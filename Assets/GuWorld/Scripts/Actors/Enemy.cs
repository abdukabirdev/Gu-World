using System;
using System.Collections;
using System.Collections.Generic;
using GuWorld.Core;
using GuWorld.Fx;
using GuWorld.UI;
using UnityEngine;

namespace GuWorld.Actors
{
    /// <summary>
    /// A beast, a bandit, a boss. Each has a mind (how it fights) made of the same parts: it
    /// notices the hero, closes in its own way, warns before every blow (a glint or a red
    /// shape on the ground, so the blow can be dodged), strikes, and waits before the next.
    /// </summary>
    public partial class Enemy : Actor
    {
        public static readonly List<Enemy> Living = new List<Enemy>();

        public EnemyDef Def { get; private set; }
        public float Health { get; private set; }
        public float MaxHealth => maxHealth;
        /// <summary>The spawn group or encounter it belongs to.</summary>
        public string Group;
        public event Action<Enemy> Died;
        public override bool Alive => alive;
        public bool Shielded => shieldTime > 0;
        public bool Aware => aware;
        /// <summary>Seconds since it was last hurt (the HUD shows its bar for a while after).</summary>
        public float SinceHurt { get; private set; } = 99;

        bool alive = true;
        bool aware;
        bool busy;
        Vector3 home;
        float attackWait;
        float wanderWait;
        Vector3 wanderTo;
        float orbit;
        float slowTime, slowPower, stunTime, shieldTime, staggerTime;
        bool frozen;
        int moveIndex;
        float hover;
        float lift;
        Color colour;
        GameObject statusGlow;

        static readonly Color Warning = new Color(1f, 0.3f, 0.22f, 1);

        public static Enemy Spawn(EnemyDef def, Vector3 at, float yaw, string group)
        {
            var root = new GameObject(def.Name);
            root.transform.position = Game.Ground(at) + Vector3.up * 0.05f;
            root.transform.rotation = Quaternion.Euler(0, yaw, 0);
            var enemy = root.AddComponent<Enemy>();
            enemy.Setup(def, group);

            return enemy;
        }

        void Setup(EnemyDef def, string group)
        {
            Def = def;
            Group = group;
            // The difficulty makes them tougher or softer.
            maxHealth = def.Health * (Game.Session != null ? DifficultyRules.EnemyHealth(Game.Session.State.Settings.Difficulty) : 1);
            Health = maxHealth;
            home = transform.position;
            colour = Models.Rgb(def.Colour);
            orbit = UnityEngine.Random.value < 0.5f ? 1 : -1;
            attackWait = UnityEngine.Random.Range(0.6f, 1.8f);

            switch (def.Body)
            {
                case EnemyBody.Canid:
                    model = Models.Canid(transform, def.Look, def.Scale, out rig).transform;
                    MakeBody(0.34f * def.Scale, 0.75f * def.Scale);

                    // Looks the canid's textures do not have are tinted (a bear, a boar, a snow wolf).
                    if (Creatures.Tint(def.Look, out var tint))
                    {
                        foreach (var renderer in model.GetComponentsInChildren<Renderer>())
                        {
                            foreach (var material in renderer.materials)
                            {
                                material.color = material.color * tint;
                            }
                        }
                    }

                    break;
                case EnemyBody.Human:
                    model = Models.Human(transform, def.Look, false, false, out rig).transform;
                    model.localScale = Vector3.one * def.Scale;
                    MakeBody(0.36f * def.Scale, 1.78f * def.Scale);
                    break;
                case EnemyBody.Serpent:
                    model = Serpent.Make(transform, colour, def.Look, def.Scale).transform;
                    MakeBody(0.38f * def.Scale, 0.6f * def.Scale);
                    break;
                case EnemyBody.Bug:
                {
                    model = Creatures.Make(transform, colour, def.Look, def.Scale).transform;
                    var walks = def.Look == "spider" || def.Look == "spider_queen" || def.Look == "toad";
                    hover = walks ? 0 : def.Look == "eagle" ? 2.2f : 1.3f;
                    MakeBody(walks ? 0.45f * def.Scale : 0.3f, walks ? 0.7f * def.Scale : 0.6f);
                    Height = walks ? 0.9f * def.Scale : 2.2f;
                    break;
                }
                default:
                    model = Models.Dummy(transform).transform;
                    MakeBody(0.4f, 1.9f);
                    break;
            }

            rig?.Play("idle", 0);
            Living.Add(this);

            if (def.Mind == EnemyMind.Caster)
            {
                caster = new CasterMind(def.Gu);
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Living.Remove(this);
        }

        // ------------------------------------------------------------------ Thinking

        float SpeedNow => Def.Speed * (slowTime > 0 ? 1 - slowPower : 1) * statuses.SpeedFactor;
        Hero Hero => Game.Hero;
        bool HeroInReach(float reach) => Hero != null && Hero.Alive && FlatDistance(Hero.transform.position) <= reach + Hero.Radius;

        void Update()
        {
            if (!alive || Game.Frozen)
            {
                return;
            }

            var dt = Time.deltaTime;
            SinceHurt += dt;
            attackWait -= dt;
            slowTime -= dt;
            shieldTime -= dt;

            if (hover > 0)
            {
                model.localPosition = new Vector3(0, hover + Mathf.Sin(Time.time * 3 + orbit) * 0.15f + lift, 0);
            }
            else if (lift != 0 || model.localPosition.y != 0)
            {
                model.localPosition = new Vector3(0, lift, 0);
            }

            if (!TickStatuses(dt))
            {
                return;
            }

            if (stunTime > 0)
            {
                stunTime -= dt;
                Walk(Vector3.zero, dt);

                if (stunTime <= 0)
                {
                    ClearStatus();
                }

                return;
            }

            if (staggerTime > 0)
            {
                staggerTime -= dt;
                Walk(Vector3.zero, dt);
                return;
            }

            if (busy)
            {
                return;
            }

            if (Def.Mind == EnemyMind.Still)
            {
                return;
            }

            var hero = Hero;
            var distance = hero != null ? FlatDistance(hero.transform.position) : 999;

            // A stealthed hero is not seen until they are all but touching; the night and the weather shorten sight.
            var sight = Def.Boss ? Def.Sight : Def.Sight * Weather.SightFactor(World.Atmosphere.Sky, World.Atmosphere.Night) * (Def.Nocturnal && World.Atmosphere.Night ? 1.5f : 1);

            if (Game.Session != null && Game.Session.Stealthed)
            {
                sight = 1.6f;

                if (aware && distance > 4 && !Def.Boss)
                {
                    aware = false;
                }
            }

            if (!aware)
            {
                if (hero != null && hero.Alive && distance < sight && !Game.Talking)
                {
                    aware = true;
                    Alert();
                }
                else
                {
                    Wander(dt);
                    return;
                }
            }

            if (hero == null || !hero.Alive || (FlatDistance(home) > 75 && !Def.Boss))
            {
                // Lost them: back home, healed.
                aware = false;
                Health = Mathf.Min(MaxHealth, Health + MaxHealth * 0.2f * dt);
                Wander(dt);
                return;
            }

            switch (Def.Mind)
            {
                case EnemyMind.Skirmisher: Close(hero, distance, dt, Def.AttackRange + 1.2f, 0, Bite(3.2f, 0.42f, true)); break;
                case EnemyMind.PackHunter: Close(hero, distance, dt, 6.5f, 5f, Bite(6.5f, 0.55f, false)); break;
                case EnemyMind.Swarm: Close(hero, distance, dt, 4.5f, 3.5f, Bite(4.5f, 0.38f, true)); break;
                case EnemyMind.Shooter: KeepAway(hero, distance, dt); break;
                case EnemyMind.Brawler: Close(hero, distance, dt, Def.AttackRange + 0.5f, 0, UnityEngine.Random.value < 0.25f ? Slam(2.8f, 0.9f, 1.4f) : Swing()); break;
                case EnemyMind.Boss: Boss(hero, distance, dt); break;
                case EnemyMind.Caster: Caster(hero, distance, dt); break;
            }
        }

        void LateUpdate()
        {
            if (rig != null && !Game.Frozen)
            {
                rig.Tick(Time.deltaTime);
            }
        }

        /// <summary>The pack hears one of its own.</summary>
        void Alert()
        {
            // A beast that sees the hero calls out (not every one of a pack).
            if (UnityEngine.Random.value < (Def.Boss ? 1 : 0.4f))
            {
                Sound.AudioDirector.Play(Core.Audio.Sounds.VoiceOf(Def), transform.position, Def.Boss ? 1 : 0.7f, 0.12f);
            }

            foreach (var other in Living)
            {
                if (other != this && other.alive && !other.aware && other.Group == Group && other.FlatDistance(transform.position) < 30)
                {
                    other.aware = true;
                }
            }
        }

        void Wander(float dt)
        {
            wanderWait -= dt;

            if (wanderWait <= 0)
            {
                wanderWait = UnityEngine.Random.Range(3f, 7f);
                var around = UnityEngine.Random.insideUnitCircle * 6;
                wanderTo = home + new Vector3(around.x, 0, around.y);
            }

            var away = wanderTo - transform.position;
            away.y = 0;
            var far = FlatDistance(home) > 12;

            if (away.magnitude > 0.8f && (wanderWait > 2 || far))
            {
                Face(away, 240, dt);
                Walk(away.normalized * (far ? SpeedNow : SpeedNow * 0.35f), dt);
                Animate(far ? "run" : "walk");
            }
            else
            {
                Walk(Vector3.zero, dt);
                Animate("idle");
            }
        }

        void Animate(string clip, float speed = 1)
        {
            if (rig != null && rig.Current != clip)
            {
                rig.Play(clip, 0.18f, false, speed);
            }
        }

        /// <summary>Come to `near`; if `ring` is set, circle at that distance until it is this one's turn; then attack.</summary>
        void Close(Hero hero, float distance, float dt, float near, float ring, IEnumerator attack)
        {
            var to = hero.transform.position - transform.position;
            to.y = 0;
            Face(to, 420, dt);

            if (attackWait <= 0 && distance <= near)
            {
                StartCoroutine(Run(attack));
                return;
            }

            if (ring > 0 && distance <= ring + 1.5f)
            {
                // Circling: sideways, drifting to the ring's distance.
                var side = Vector3.Cross(Vector3.up, to.normalized) * orbit;
                var drift = to.normalized * Mathf.Clamp(distance - ring, -1, 1);
                Walk((side + drift).normalized * SpeedNow * 0.55f, dt);
                Animate("walk", 1.4f);

                if (UnityEngine.Random.value < dt * 0.15f)
                {
                    orbit = -orbit;
                }

                return;
            }

            if (distance > Mathf.Max(near - 0.6f, 1.2f))
            {
                Walk(to.normalized * SpeedNow, dt);
                Animate("run");
            }
            else
            {
                Walk(Vector3.zero, dt);
                Animate(Def.Body == EnemyBody.Human ? "guard" : "idle");
            }
        }

        void KeepAway(Hero hero, float distance, float dt)
        {
            var to = hero.transform.position - transform.position;
            to.y = 0;
            Face(to, 420, dt);
            var want = Def.AttackRange * 0.75f;

            if (attackWait <= 0 && distance <= Def.AttackRange + 2)
            {
                StartCoroutine(Run(Shoot(1, 0.75f)));
                return;
            }

            if (distance > want + 2)
            {
                Walk(to.normalized * SpeedNow, dt);
                Animate("run");
            }
            else if (distance < want - 4)
            {
                Walk(-to.normalized * SpeedNow * 0.8f, dt);
                Animate("run");
            }
            else
            {
                var side = Vector3.Cross(Vector3.up, to.normalized) * orbit;
                Walk(side * SpeedNow * 0.35f, dt);
                Animate("walk");
            }
        }

        void Boss(Hero hero, float distance, float dt)
        {
            var to = hero.transform.position - transform.position;
            to.y = 0;
            Face(to, 300, dt);
            var ranged = Def.AttackRange > 8;
            var want = ranged ? 10 : Def.AttackRange + 0.6f;

            if (attackWait <= 0)
            {
                var move = Def.Moves[moveIndex % Def.Moves.Length];
                moveIndex++;
                StartCoroutine(Run(MoveOf(move)));
                return;
            }

            if (distance > want + 1)
            {
                Walk(to.normalized * SpeedNow * 0.8f, dt);
                Animate(Def.Body == EnemyBody.Canid ? "run" : "walk", Def.Body == EnemyBody.Canid ? 0.8f : 1.3f);
            }
            else if (ranged && distance < want - 4)
            {
                Walk(-to.normalized * SpeedNow * 0.6f, dt);
                Animate("walk", 1.3f);
            }
            else
            {
                var side = Vector3.Cross(Vector3.up, to.normalized) * orbit;
                Walk(side * SpeedNow * 0.25f, dt);
                Animate(Def.Body == EnemyBody.Human ? "guard" : "walk");
            }
        }

        IEnumerator MoveOf(BossMove move)
        {
            // Below half health a boss leaves shorter gaps.
            var gap = Health < MaxHealth * 0.5f ? 0.75f : 1f;

            switch (move)
            {
                case BossMove.Lunge: return Then(Def.Body == EnemyBody.Human ? Swing(1.3f) : Bite(8f, 0.6f, false, 1.2f), 1.4f * gap);
                case BossMove.Charge: return Then(Charge(), 1.8f * gap);
                case BossMove.Howl:
                case BossMove.Summon: return Then(Summon(), 1.6f * gap);
                case BossMove.Rings: return Then(Rings(), 2f * gap);
                case BossMove.Leap: return Then(Leap(), 1.6f * gap);
                case BossMove.Bolts: return Then(Shoot(3, 0.55f), 1.6f * gap);
                case BossMove.Slam: return Then(Slam(5.5f, 0.95f, 1.5f), 1.8f * gap);
                case BossMove.Blades: return Then(Blades(), 1.5f * gap);
                case BossMove.Shield: return Then(Shield(), 1.2f * gap);
                default: return Then(BloodMoon(), 2f * gap);
            }
        }

        IEnumerator Then(IEnumerator attack, float wait)
        {
            yield return attack;
            attackWait = wait;
        }

        IEnumerator Run(IEnumerator attack)
        {
            busy = true;
            attackWait = Def.AttackPeriod * UnityEngine.Random.Range(0.8f, 1.25f);
            yield return attack;
            lift = 0;
            model.localRotation = rig != null && rig.Mirrored ? Quaternion.Euler(0, 180, 0) : Quaternion.identity;
            busy = false;
        }

        // ------------------------------------------------------------------ Blows

        void Glint(float size = 1.2f) => Effects.Burst(transform.position + Vector3.up * (Height * 0.75f + hover), Warning, size * Mathf.Max(1, Def.Scale * 0.8f), 0.3f);

        void Strike(float reach, float power = 1)
        {
            if (HeroInReach(reach))
            {
                HitHero(Def.Damage * power, transform.position);
            }
        }

        IEnumerator Still(float seconds, bool facing = true)
        {
            for (var t = 0f; t < seconds; t += Time.deltaTime)
            {
                if (facing && Hero != null)
                {
                    Face(Hero.transform.position - transform.position, 360, Time.deltaTime);
                }

                Walk(Vector3.zero, Time.deltaTime);
                yield return null;
            }
        }

        /// <summary>Moves by a flat step over a time, with a hop if `arc` is given.</summary>
        IEnumerator Dash(Vector3 by, float seconds, float arc = 0)
        {
            for (var t = 0f; t < seconds; t += Time.deltaTime)
            {
                lift = arc * Mathf.Sin(Mathf.Clamp01(t / seconds) * Mathf.PI);
                Walk(by / seconds, Time.deltaTime, arc > 0 ? 0 : 22);
                yield return null;
            }

            lift = 0;
        }

        /// <summary>A crouch, then a leap at the hero and a bite; `back` hops away after.</summary>
        IEnumerator Bite(float leap, float windup, bool back, float power = 1)
        {
            Glint();
            Animate("idle");
            model.localRotation *= Quaternion.Euler(12, 0, 0);
            yield return Still(windup);
            model.localRotation *= Quaternion.Euler(-24, 0, 0);
            var to = Hero.transform.position - transform.position;
            to.y = 0;
            var jump = Mathf.Min(leap, Mathf.Max(0.5f, to.magnitude - 0.6f));
            Animate("run", 1.6f);
            yield return Dash(to.normalized * jump, 0.24f, hover > 0 ? 0 : 0.5f * Def.Scale);
            Strike(Def.AttackRange + 0.3f, power);
            model.localRotation *= Quaternion.Euler(12, 0, 0);

            if (back)
            {
                yield return Dash(-to.normalized * 3.2f, 0.34f, hover > 0 ? 0 : 0.25f);
            }

            yield return Still(0.35f);
        }

        IEnumerator Swing(float power = 1)
        {
            Glint();
            var heavy = power > 1;
            var clip = heavy ? "attack3" : UnityEngine.Random.value < 0.5f ? "attack1" : "attack2";
            rig?.Play(clip, 0.1f, true, heavy ? 0.8f : 0.75f);
            yield return Still(heavy ? 0.42f : 0.3f);
            var to = Hero.transform.position - transform.position;
            to.y = 0;
            yield return Dash(to.normalized * Mathf.Min(1.6f, Mathf.Max(0, to.magnitude - 1.2f)), 0.14f);

            if (HeroInReach(Def.AttackRange + 0.5f) && Vector3.Angle(transform.forward, Hero.transform.position - transform.position) < 70)
            {
                HitHero(Def.Damage * power, transform.position);
            }

            yield return Still(heavy ? 0.6f : 0.45f, false);
        }

        IEnumerator Slam(float radius, float windup, float power)
        {
            var mark = Telegraph.Circle(transform.position, radius, windup);
            rig?.Play("slam", 0.12f, true, 0.9f / Mathf.Max(0.5f, windup + 0.25f));

            if (rig == null || Def.Body != EnemyBody.Human)
            {
                Glint(2);
            }

            yield return Still(windup, false);

            if (!alive)
            {
                yield break;
            }

            Effects.Wave(transform.position, colour, radius, 0.45f);
            Game.Shake(0.35f);

            if (HeroInReach(radius) && !Hero.Airborne)
            {
                HitHero(Def.Damage * power, transform.position);
            }

            yield return Still(0.55f, false);
        }

        IEnumerator Shoot(int count, float windup)
        {
            for (var i = 0; i < count; i++)
            {
                Glint(1.6f);
                rig?.Play(Def.Body == EnemyBody.Human ? "cast" : "idle", 0.1f, true);
                var aim = Hero.transform.position - transform.position;
                var line = Telegraph.Line(transform.position, aim, Mathf.Min(aim.magnitude + 3, Def.AttackRange + 6), 1.1f, windup, new Color(colour.r, colour.g, colour.b, 1));
                yield return Still(windup);
                var from = transform.position + Vector3.up * (Def.Body == EnemyBody.Human ? 1.3f * Def.Scale : 0.6f * Def.Scale + hover) + transform.forward * 0.6f;
                var target = Hero.Centre;
                Projectile.Launch(false, from, target - from, 19, Def.AttackRange + 8, 0.55f, colour, false, false, actor =>
                {
                    if (actor is Hero hero)
                    {
                        HitHero(Def.Damage, transform.position);
                    }
                });
                yield return Still(0.25f);
            }

            yield return Still(0.35f);
        }

        IEnumerator Blades()
        {
            Glint(2);
            rig?.Play("cast", 0.1f, true, 0.8f);
            yield return Still(0.55f);
            var from = transform.position + Vector3.up * 1.2f * Def.Scale + transform.forward * 0.7f;
            var blades = Health < MaxHealth * 0.5f ? 5 : 3;

            for (var i = 0; i < blades; i++)
            {
                var spread = (i - (blades - 1) * 0.5f) * 14;
                var direction = Quaternion.Euler(0, spread, 0) * transform.forward;
                Projectile.Launch(false, from, direction, 17, 34, 1f, colour, true, true, actor =>
                {
                    if (actor is Hero hero)
                    {
                        HitHero(Def.Damage, transform.position);
                    }
                });
            }

            yield return Still(0.5f, false);
        }

        IEnumerator Charge()
        {
            var to = Hero.transform.position - transform.position;
            to.y = 0;
            var direction = to.normalized;
            const float length = 20;
            Telegraph.Line(transform.position, direction, length, 2.4f * Mathf.Max(1, Def.Scale * 0.5f), 0.95f);
            Animate("idle");
            yield return Still(0.95f, false);
            Animate("run", 2);
            var struck = false;

            for (var gone = 0f; gone < length && alive;)
            {
                var step = 24 * Time.deltaTime;
                gone += step;
                Walk(direction * 24, Time.deltaTime);

                if (!struck && HeroInReach(Def.AttackRange))
                {
                    struck = true;
                    HitHero(Def.Damage * 1.5f, transform.position);
                }

                yield return null;
            }

            yield return Still(0.7f);
        }

        IEnumerator Leap()
        {
            var target = Hero.transform.position;
            var radius = 4.5f * Mathf.Max(1, Def.Scale * 0.45f);
            Telegraph.Circle(target, radius, 1.0f);
            Glint(2.4f);
            yield return Still(0.3f);
            var by = target - transform.position;
            by.y = 0;
            Animate("run", 1.5f);
            rig?.Play(Def.Body == EnemyBody.Human ? "jump" : "run", 0.1f, true);
            yield return Dash(by, 0.7f, 4.5f);
            Effects.Wave(transform.position, colour, radius, 0.5f);
            Game.Shake(0.5f);

            if (HeroInReach(radius) && !Hero.Airborne)
            {
                HitHero(Def.Damage * 1.4f, transform.position);
            }

            yield return Still(0.6f);
        }

        /// <summary>Rings of lightning running out over the ground: jump them or roll through.</summary>
        IEnumerator Rings()
        {
            Animate("idle");
            Glint(2.6f);
            yield return Still(0.7f, false);
            var rings = Health < MaxHealth * 0.5f ? 3 : 2;

            for (var i = 0; i < rings; i++)
            {
                StartCoroutine(Ring(transform.position, 18, 1.7f));
                Game.Shake(0.25f);
                yield return Still(0.75f, false);
            }

            yield return Still(0.4f);
        }

        IEnumerator Ring(Vector3 centre, float reach, float seconds)
        {
            Effects.Wave(centre, colour, reach, seconds);
            var hit = false;

            for (var t = 0f; t < seconds; t += Time.deltaTime)
            {
                // The wave's shape follows Fade's easing.
                var k = t / seconds;
                var radius = Mathf.Lerp(0.3f, reach, 1 - (1 - k) * (1 - k));
                var hero = Hero;

                if (!hit && hero != null && hero.Alive && !hero.Airborne)
                {
                    var away = hero.transform.position - centre;
                    away.y = 0;

                    if (Mathf.Abs(away.magnitude - radius) < 0.7f)
                    {
                        hit = true;
                        HitHero(Def.Damage * 0.9f, centre);
                    }
                }

                yield return null;
            }
        }

        IEnumerator Summon()
        {
            Animate("idle");
            Glint(3);
            Game.Say(Def.Body == EnemyBody.Canid ? $"{Def.Name} воет" : $"{Def.Name} зовёт марионеток");
            Effects.Wave(transform.position, colour, 9, 0.8f);
            yield return Still(1f, false);
            var mine = 0;

            foreach (var other in Living)
            {
                mine += other != this && other.Group == Group && other.alive ? 1 : 0;
            }

            var summon = Bestiary.Get(Def.Summons);

            for (var i = 0; summon != null && i < 2 && mine + i < 4; i++)
            {
                var around = Quaternion.Euler(0, UnityEngine.Random.Range(0, 360f), 0) * Vector3.forward * UnityEngine.Random.Range(5f, 8f);
                var called = Spawn(summon, transform.position + around, transform.eulerAngles.y, Group);
                called.aware = true;
                Effects.Burst(called.transform.position + Vector3.up, colour, 2.4f, 0.4f);
            }

            yield return Still(0.4f);
        }

        IEnumerator Shield()
        {
            shieldTime = 4;
            Effects.Aura(transform, colour, 4, 2.6f * Def.Scale);
            Game.Say($"{Def.Name} закрывается. Удары вязнут");
            rig?.Play("channel", 0.12f, true);
            yield return Still(0.7f);
        }

        IEnumerator BloodMoon()
        {
            rig?.Play("slam", 0.12f, true, 0.6f);
            Game.Say("Кровавая луна");
            var spots = new List<Vector3> { Hero.transform.position };

            for (var i = 0; i < 4; i++)
            {
                var around = UnityEngine.Random.insideUnitCircle * 9;
                spots.Add(Hero.transform.position + new Vector3(around.x, 0, around.y));
            }

            foreach (var spot in spots)
            {
                Telegraph.Circle(spot, 4, 1.4f);
            }

            yield return Still(1.4f, false);
            Game.Shake(0.6f);

            foreach (var spot in spots)
            {
                Effects.Pillar(spot, colour, 3, 14, 0.5f);

                if (Hero != null && Hero.Alive && (Hero.transform.position - spot).magnitude < 4 + Hero.Radius)
                {
                    HitHero(Def.Damage * 1.3f, spot);
                }
            }

            // It drinks what it spilled.
            Health = Mathf.Min(MaxHealth, Health + MaxHealth * 0.02f);
            yield return Still(0.7f);
        }

        // ------------------------------------------------------------------ Being hit

        public void TakeHit(float damage, bool critical, Vector3 from, float push = 0, StatusKind status = StatusKind.None, float statusPower = 0, float statusTime = 0, int affinity = 0)
        {
            if (!alive)
            {
                return;
            }

            if (shieldTime > 0)
            {
                damage *= 0.3f;
            }

            Health -= damage;
            SinceHurt = 0;
            Game.Ui?.DamageNumber(transform.position + Vector3.up * (Height + hover + 0.2f), damage, critical, false, affinity);

            // What the blow brings meets what is on it already: steam, shatter, quench…
            if (status != StatusKind.None && Def.Mind != EnemyMind.Still)
            {
                React(Reactions.Meet(statuses, status, statusPower, frozen && stunTime > 0), damage);
            }

            if (!aware)
            {
                aware = true;
                Alert();
            }

            if (Health <= 0)
            {
                Die(from);
                return;
            }

            if (Def.Mind == EnemyMind.Still)
            {
                return;
            }

            if (status == StatusKind.Poison || status == StatusKind.Burn || status == StatusKind.Bleed || status == StatusKind.Chill)
            {
                Afflict(status, statusPower, statusTime);
            }
            else if (status == StatusKind.Slow)
            {
                slowTime = Def.Boss ? statusTime * 0.5f : statusTime;
                slowPower = Def.Boss ? statusPower * 0.5f : statusPower;
            }
            else if ((status == StatusKind.Stun || status == StatusKind.Freeze) && !Def.Boss)
            {
                Interrupt();
                stunTime = statusTime;
                frozen = status == StatusKind.Freeze;
                ClearStatus(false);
                statusGlow = Effects.Aura(transform, frozen ? new Color(0.6f, 0.9f, 1f) : new Color(1f, 0.9f, 0.5f), statusTime, 1.6f * Mathf.Max(1, Def.Scale));
            }
            else if ((status == StatusKind.Stun || status == StatusKind.Freeze) && Def.Boss)
            {
                slowTime = statusTime;
                slowPower = 0.35f;
            }

            if (!Def.Boss && damage >= Def.Poise && stunTime <= 0)
            {
                Interrupt();
                staggerTime = 0.32f;
                rig?.Play(rig.Has("hit") ? "hit" : "idle", 0.05f, true);
                var away = transform.position - from;
                away.y = 0;

                if (push > 0 && body != null && body.enabled)
                {
                    body.Move(away.normalized * push);
                }
            }
        }

        void React(Reaction reaction, float damage)
        {
            if (!reaction.Happened)
            {
                return;
            }

            var colour = UiKit.Hex(Reactions.Colour(reaction.Kind));
            var over = transform.position + Vector3.up * (Height + hover + 0.6f);
            var extra = damage * reaction.Bonus;
            Health -= extra;

            if (extra >= 1)
            {
                Game.Ui?.DamageNumber(over, extra, true, false);
            }

            Game.Ui?.FloatWord(over + Vector3.up * 0.4f, Reactions.Name(reaction.Kind), colour);
            Effects.Burst(transform.position + Vector3.up * Height * 0.5f, colour, 1.6f * Mathf.Max(1, Def.Scale), 0.45f);
            Game.Session?.Reacted(reaction.Kind);

            if (reaction.Thawed && frozen)
            {
                stunTime = 0;
                ClearStatus();
            }

            if (reaction.Hold > 0 && !Def.Boss)
            {
                Interrupt();
                staggerTime = Mathf.Max(staggerTime, reaction.Hold);
            }
        }

        /// <summary>Its blow was parried: it reels (a boss only for a moment).</summary>
        public void Parried()
        {
            if (!Alive)
            {
                return;
            }

            Interrupt();
            staggerTime = Def.Boss ? 0.5f : 1.4f;
            attackWait = Mathf.Max(attackWait, Def.Boss ? 0.8f : 1.6f);
            rig?.Play(rig.Has("hit") ? "hit" : "idle", 0.05f, true);
        }

        void Interrupt()
        {
            StopAllCoroutines();
            busy = false;
            lift = 0;
            model.localRotation = rig != null && rig.Mirrored ? Quaternion.Euler(0, 180, 0) : Quaternion.identity;
        }

        void ClearStatus(bool all = true)
        {
            if (statusGlow != null)
            {
                Destroy(statusGlow);
            }

            if (all)
            {
                frozen = false;
            }
        }

        /// <summary>Pulled towards a point (a whirl): a step of the pull for this frame.</summary>
        public void Drag(Vector3 towards, float speed, float dt)
        {
            if (!alive || Def.Boss || Def.Mind == EnemyMind.Still || body == null || !body.enabled)
            {
                return;
            }

            var to = towards - transform.position;
            to.y = 0;

            if (to.magnitude > 0.8f)
            {
                body.Move(to.normalized * Mathf.Min(to.magnitude, speed * dt));
            }
        }

        void Die(Vector3 from)
        {
            alive = false;
            Interrupt();
            ClearStatus();
            Living.Remove(this);
            body.enabled = false;
            Died?.Invoke(this);
            Game.EnemyKilled(this);
            Sound.AudioDirector.Play("enemy_die", transform.position, Def.Boss ? 1 : 0.6f);

            if (rig != null && rig.Has("death"))
            {
                rig.Play("death", 0.1f, true);
            }

            StartCoroutine(Fall(from));
        }

        IEnumerator Fall(Vector3 from)
        {
            var away = transform.position - from;
            away.y = 0;
            var tip = rig != null && rig.Has("death") ? Quaternion.identity : Quaternion.AngleAxis(Def.Body == EnemyBody.Dummy ? 80 : 88, Vector3.Cross(Vector3.up, away.sqrMagnitude > 0.01f ? away.normalized : transform.forward));
            var start = transform.rotation;
            var ground = Game.Ground(transform.position).y;

            for (var t = 0f; t < 0.5f; t += Time.deltaTime)
            {
                transform.rotation = Quaternion.Slerp(start, tip * start, t / 0.5f);
                model.localPosition = Vector3.Lerp(model.localPosition, new Vector3(0, Def.Body == EnemyBody.Canid ? 0.12f * Def.Scale : 0, 0), t / 0.5f);
                transform.position = new Vector3(transform.position.x, Mathf.Lerp(transform.position.y, ground, t / 0.5f), transform.position.z);
                rig?.Tick(0);
                yield return null;
            }

            yield return new WaitForSeconds(Def.Boss ? 4 : 2.2f);

            // Into the ground and gone.
            for (var t = 0f; t < 1; t += Time.deltaTime)
            {
                transform.position += Vector3.down * Time.deltaTime * (Def.Scale + 0.6f);
                yield return null;
            }

            Destroy(gameObject);
        }

        /// <summary>Taken out of the world without dying (the hero went away; an encounter was reset).</summary>
        public void Remove()
        {
            alive = false;
            Living.Remove(this);
            Destroy(gameObject);
        }
    }
}
