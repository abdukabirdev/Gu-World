using System.Collections;
using GuWorld.Core;
using GuWorld.Fx;
using GuWorld.World;
using UnityEngine;

namespace GuWorld.Actors
{
    /// <summary>
    /// What the wider mountain adds to an enemy: the statuses the hero's Gu leave on it (venom, fire,
    /// wounds, cold — damage over time and slowness), what its own blows leave on the hero, and the
    /// mind of a Gu master, who fights with the Gu it carries: bolts and fans from afar, cones and
    /// novas up close, storms on where the hero stands, shields and strength on itself.
    /// </summary>
    public partial class Enemy
    {
        float maxHealth;
        readonly EnemyStatuses statuses = new EnemyStatuses();
        CasterMind caster;
        float dotShown;
        float dotWait;
        float strongTime, strongValue;
        StatusKind shownStatus;
        GameObject statusAura;

        /// <summary>The worst status on it (the HUD tints its bar).</summary>
        public StatusKind Status => statuses.Worst;

        /// <summary>A frame of its statuses: venom and fire wear it down, cold and stuns hold it. False if it can do nothing this frame.</summary>
        bool TickStatuses(float dt)
        {
            var damage = statuses.Tick(dt, Def.Armour);
            strongTime -= dt;

            if (damage > 0)
            {
                Health -= damage;
                dotShown += damage;
                dotWait -= dt;
                SinceHurt = 0;

                // The numbers of damage over time come every half second, not every frame.
                if (dotWait <= 0 && dotShown >= 1)
                {
                    Game.Ui?.DamageNumber(transform.position + Vector3.up * (Height + hover + 0.2f), dotShown, false, false);
                    dotShown = 0;
                    dotWait = 0.5f;
                }

                if (Health <= 0)
                {
                    Die(transform.position - transform.forward);

                    return false;
                }
            }

            if (statuses.Worst != shownStatus)
            {
                shownStatus = statuses.Worst;

                if (statusAura != null)
                {
                    Destroy(statusAura);
                }

                if (shownStatus != StatusKind.None && shownStatus != StatusKind.Stun)
                {
                    statusAura = Effects.Aura(transform, StatusColour(shownStatus), 30, 1.4f * Mathf.Max(1, Def.Scale));
                }
            }

            if (statuses.Held && !Def.Boss)
            {
                Walk(Vector3.zero, dt);

                return false;
            }

            return true;
        }

        public static Color StatusColour(StatusKind kind)
        {
            switch (kind)
            {
                case StatusKind.Poison: return new Color(0.55f, 0.95f, 0.4f);
                case StatusKind.Burn: return new Color(1f, 0.55f, 0.25f);
                case StatusKind.Bleed: return new Color(0.95f, 0.2f, 0.2f);
                case StatusKind.Chill: return new Color(0.7f, 0.92f, 1f);
                case StatusKind.Freeze: return new Color(0.6f, 0.9f, 1f);
                case StatusKind.Slow: return new Color(0.6f, 0.8f, 0.7f);
                default: return Color.white;
            }
        }

        /// <summary>A Gu's status on it. Bosses shrug off half of the time; a chill on the chilled freezes.</summary>
        void Afflict(StatusKind kind, float power, float seconds)
        {
            if (Def.Mind == EnemyMind.Still)
            {
                return;
            }

            if (Def.Boss)
            {
                seconds *= 0.5f;
            }

            var wasChilled = statuses.Chilled;
            statuses.Apply(kind, power, seconds);

            if (kind == StatusKind.Chill && wasChilled && statuses.Held)
            {
                Interrupt();
                Effects.Burst(transform.position + Vector3.up, StatusColour(StatusKind.Freeze), 2, 0.4f);
                Game.Say($"{Def.Name}: заморозка");
            }
        }

        /// <summary>Its blow reaches the hero: the damage (stronger by night for the night's beasts, and while it is strengthened), and the status it leaves.</summary>
        float HitHero(float damage, Vector3 from)
        {
            var hero = Hero;

            if (hero == null)
            {
                return 0;
            }

            if (Def.Nocturnal && Atmosphere.Night)
            {
                damage *= 1.15f;
            }

            if (strongTime > 0)
            {
                damage *= 1 + strongValue;
            }

            var landed = hero.TakeHit(damage, from, this);

            if (landed > 0 && Def.Inflicts != StatusKind.None && Random.value < Def.InflictChance)
            {
                Game.Session?.Afflict(Def.Inflicts, Def.InflictPower, Def.InflictTime);
            }

            return landed;
        }

        // ------------------------------------------------------------------ A Gu master's mind

        void Caster(Hero hero, float distance, float dt)
        {
            caster.Tick(dt);
            var to = hero.transform.position - transform.position;
            to.y = 0;
            Face(to, 420, dt);

            if (attackWait <= 0)
            {
                // A killer move first when one is ready (a master's strongest), then its Gu in turn.
                var gu = caster.ChooseMove(distance, Health < MaxHealth * 0.5f) ?? caster.Choose(distance, Health < MaxHealth * 0.5f);

                if (gu != null)
                {
                    StartCoroutine(Run(Cast(gu)));
                    return;
                }

                // No Gu suits: a plain blow if the hero is in reach.
                if (distance <= Def.AttackRange + 0.6f)
                {
                    StartCoroutine(Run(Swing()));
                    return;
                }
            }

            var want = caster.PreferredDistance;

            if (distance > want + 2)
            {
                Walk(to.normalized * SpeedNow, dt);
                Animate("run");
            }
            else if (distance < want - 3 && want > 4)
            {
                Walk(-to.normalized * SpeedNow * 0.8f, dt);
                Animate("run");
            }
            else
            {
                var side = Vector3.Cross(Vector3.up, to.normalized) * orbit;
                Walk(side * SpeedNow * 0.4f, dt);
                Animate(Def.Body == EnemyBody.Human ? "guard" : "walk");

                if (Random.value < dt * 0.2f)
                {
                    orbit = -orbit;
                }
            }
        }

        /// <summary>How hard one of its Gu hits: its own blow, scaled by the Gu's power against a rank's ordinary bolt.</summary>
        float GuDamage(GuDef gu) => Def.Damage * Mathf.Clamp(gu.Skill.Power / (16f + 10f * gu.Rank), 0.4f, 1.8f);

        IEnumerator Cast(GuDef gu)
        {
            var skill = gu.Skill;
            var hue = Models.Rgb(gu.Colour);
            var hero = Hero;
            Game.Say($"{Def.Name}: {gu.Name}");
            rig?.Play("cast", 0.1f, true);

            switch (skill.Shape)
            {
                case SkillShape.Projectile:
                case SkillShape.Fan:
                {
                    var count = skill.Shape == SkillShape.Fan ? Mathf.Min(skill.Count, 5) : 1;
                    var aim = hero.transform.position - transform.position;
                    Telegraph.Line(transform.position, aim, Mathf.Min(aim.magnitude + 3, skill.Range + 4), 1.1f + 0.4f * (count - 1), 0.6f, hue);
                    yield return Still(0.6f);
                    var from = transform.position + Vector3.up * 1.3f * Def.Scale + transform.forward * 0.6f;
                    var target = Hero != null ? Hero.Centre : from + transform.forward;

                    for (var i = 0; i < count; i++)
                    {
                        var spread = count > 1 ? (i - (count - 1) * 0.5f) * 12 : 0;
                        var direction = Quaternion.Euler(0, spread, 0) * (target - from);
                        Projectile.Launch(false, from, direction, skill.Speed * 0.75f, skill.Range + 6, Mathf.Max(0.5f, skill.Radius), hue, skill.Pierce, false, actor =>
                        {
                            if (actor is Hero struck && HitHero(GuDamage(gu), transform.position) > 0)
                            {
                                AfflictHero(skill);
                            }
                        });
                    }

                    yield return Still(0.4f);
                    break;
                }

                case SkillShape.Chain:
                {
                    var aim = hero.transform.position - transform.position;
                    Telegraph.Line(transform.position, aim, aim.magnitude + 1, 0.8f, 0.7f, hue);
                    yield return Still(0.7f);

                    if (Hero != null && FlatDistance(Hero.transform.position) <= skill.Range + 2 && !Hero.Rolling)
                    {
                        Effects.Bolt(transform.position + Vector3.up * 1.4f, Hero.Centre, hue);

                        if (HitHero(GuDamage(gu), transform.position) > 0)
                        {
                            AfflictHero(skill);
                        }
                    }

                    yield return Still(0.4f);
                    break;
                }

                case SkillShape.Cone:
                case SkillShape.Nova:
                {
                    var radius = skill.Shape == SkillShape.Nova ? skill.Radius : skill.Range;
                    var centre = skill.Shape == SkillShape.Nova ? transform.position : transform.position + transform.forward * radius * 0.5f;
                    Telegraph.Circle(centre, skill.Shape == SkillShape.Nova ? radius : radius * 0.6f, 0.85f, hue);
                    yield return Still(0.85f, skill.Shape == SkillShape.Cone);
                    Effects.Wave(centre, hue, radius, 0.45f);

                    if (Hero != null && (Hero.transform.position - centre).magnitude <= (skill.Shape == SkillShape.Nova ? radius : radius * 0.6f) + Hero.Radius && !Hero.Airborne)
                    {
                        if (HitHero(GuDamage(gu), transform.position) > 0)
                        {
                            AfflictHero(skill);
                        }
                    }

                    yield return Still(0.5f);
                    break;
                }

                case SkillShape.Storm:
                case SkillShape.Pull:
                {
                    var spot = hero.transform.position;
                    Telegraph.Circle(spot, skill.Radius, 1.1f, hue);
                    yield return Still(1.1f, false);
                    var strikes = skill.Shape == SkillShape.Storm ? Mathf.Clamp(skill.Count / 2, 2, 5) : 1;

                    for (var i = 0; i < strikes && alive; i++)
                    {
                        Effects.Pillar(spot, hue, skill.Radius * 0.5f, 10, 0.35f);

                        if (Hero != null && (Hero.transform.position - spot).magnitude <= skill.Radius + Hero.Radius)
                        {
                            if (HitHero(GuDamage(gu) * (skill.Shape == SkillShape.Storm ? 0.6f : 1), spot) > 0)
                            {
                                AfflictHero(skill);
                            }
                        }

                        yield return Still(0.45f, false);
                    }

                    break;
                }

                case SkillShape.Dash:
                case SkillShape.Blink:
                {
                    var target = hero.transform.position - (hero.transform.position - transform.position).normalized * 2.2f;
                    Glint(1.6f);
                    yield return Still(0.45f);
                    Effects.Burst(transform.position + Vector3.up, hue, 2, 0.3f);
                    var by = target - transform.position;
                    by.y = 0;
                    yield return Dash(by, 0.2f);
                    Effects.Wave(transform.position, hue, skill.Radius > 0 ? skill.Radius : 2.5f, 0.35f);

                    if (HeroInReach(Mathf.Max(2.2f, skill.Radius)))
                    {
                        HitHero(GuDamage(gu), transform.position);
                    }

                    yield return Still(0.4f);
                    break;
                }

                case SkillShape.Heal:
                    Effects.Aura(transform, hue, 2, 2.2f * Def.Scale);
                    yield return Still(0.8f);
                    Health = Mathf.Min(MaxHealth, Health + MaxHealth * 0.15f);
                    break;

                case SkillShape.Buff:
                    Effects.Aura(transform, hue, skill.Duration, 2.4f * Def.Scale);

                    if (skill.Buff == BuffKind.Shield || skill.Buff == BuffKind.Unstoppable)
                    {
                        shieldTime = Mathf.Min(6, skill.Duration);
                    }
                    else if (skill.Buff == BuffKind.MeleeUp || skill.Buff == BuffKind.Aura)
                    {
                        strongTime = skill.Duration;
                        strongValue = Mathf.Min(0.8f, skill.BuffValue > 2 ? 0.4f : skill.BuffValue);
                    }
                    else if (skill.Buff == BuffKind.Regen)
                    {
                        Health = Mathf.Min(MaxHealth, Health + MaxHealth * 0.12f);
                    }

                    yield return Still(0.6f);
                    break;

                default:
                    yield return Still(0.5f);
                    break;
            }
        }

        /// <summary>The status a Gu master's Gu leaves on the hero (weaker than the hero's own Gu leave on beasts).</summary>
        static void AfflictHero(SkillDef skill)
        {
            if (skill.Status != StatusKind.None && Game.Session != null)
            {
                Game.Session.Afflict(skill.Status, skill.StatusPower * 0.6f, skill.StatusTime * 0.7f);
            }
        }
    }
}
