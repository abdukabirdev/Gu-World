using System.Collections.Generic;
using GuWorld.Core;
using GuWorld.Fx;
using UnityEngine;

namespace GuWorld.Actors
{
    /// <summary>
    /// Two blows beyond the palm's three (ROADMAP 13): the finishing blow on a beast left open (reeling from a
    /// parry, or stunned) — a step in and both palms through it; and the running kick — a leap with a foot that
    /// throws a beast back and leaves it reeling. The numbers are Core's (Blows, Session.Finish / KickHit).
    /// </summary>
    public partial class Hero
    {
        Enemy finishing;
        bool finishLanded, kickLanded;
        Vector3 kickDirection;

        /// <summary>The nearest beast ahead left open to a finishing blow, within its reach.</summary>
        Enemy OpenFoe()
        {
            Enemy best = null;
            var bestDistance = Blows.FinishReach;
            var ahead = AimDirection();

            foreach (var enemy in Enemy.Living)
            {
                if (!enemy.Open || enemy.Peaceful)
                {
                    continue;
                }

                var to = enemy.transform.position - transform.position;
                to.y = 0;
                var distance = to.magnitude - enemy.Radius;

                if (distance < bestDistance && (distance < 0.5f || Vector3.Angle(ahead, to) <= Blows.FinishArc))
                {
                    best = enemy;
                    bestDistance = distance;
                }
            }

            return best;
        }

        void BeginFinisher(Enemy foe)
        {
            finishing = foe;
            finishLanded = false;
            var to = foe.transform.position - transform.position;
            to.y = 0;

            if (to.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(to);
            }

            // Not to be broken off by another beast's blow halfway.
            invulnerable = Mathf.Max(invulnerable, 0.5f);
            SetMode(Mode.Finishing);
            Animate("finisher", 0.06f, 1, true);
            Game.Ui?.FloatWord(foe.transform.position + Vector3.up * (foe.Height + 0.8f), "Добивание", UI.UiKit.GoldLit);
        }

        void Finishing(float dt)
        {
            var length = ClipLength("finisher", 0.8f);
            var t = modeTime / length;
            var foe = finishing;

            // In to arm's length before the palms go.
            if (foe != null && foe.Alive && t < 0.5f)
            {
                var to = foe.transform.position - transform.position;
                to.y = 0;
                var gap = to.magnitude - foe.Radius - 0.85f;
                var left = Mathf.Max(0.05f, 0.5f * length - modeTime);
                Walk(gap > 0.05f ? to.normalized * Mathf.Min(7, gap / left) : Vector3.zero, dt);
                Face(to, 900, dt);
            }
            else
            {
                Walk(Vector3.zero, dt);
            }

            if (!finishLanded && t >= 0.5f)
            {
                finishLanded = true;

                if (foe != null && foe.Alive)
                {
                    var hit = Session.Finish(foe.Def, foe.Health, foe.MaxHealth);
                    foe.Finished();
                    foe.TakeHit(hit.Damage, true, transform.position, 1.6f);
                    Effects.Burst(foe.Centre, new Color(1f, 0.86f, 0.5f), 2.6f, 0.35f);
                    Effects.Wave(foe.transform.position, new Color(1f, 0.9f, 0.6f, 0.7f), 2.6f, 0.35f);
                    Sound.AudioDirector.Play("palm_heavy", transform.position + transform.forward, 1, 0);
                    Game.Shake(0.45f);
                    Game.HitStop(0.12f);
                    SinceFight = 0;
                    sinceStrike = 0;
                }
            }

            if (t >= 1)
            {
                finishing = null;
                SetMode(Mode.Free);
            }
        }

        /// <summary>Running, the strike key: a kick in a leap at the beast ahead (or where the hero runs), for stamina; false if out of breath.</summary>
        bool TryKick(Vector3 wish)
        {
            if (!Session.BeginKick())
            {
                return false;
            }

            kickDirection = wish.sqrMagnitude > 0.01f ? wish.normalized : transform.forward;
            var mark = Target != null && FlatDistance(Target.transform.position) < Blows.KickReach + 2 ? Target : Nearest(Blows.KickReach + 1.5f, Blows.KickArc);

            if (mark != null)
            {
                var to = mark.transform.position - transform.position;
                to.y = 0;

                if (to.sqrMagnitude > 0.01f)
                {
                    kickDirection = to.normalized;
                }
            }

            transform.rotation = Quaternion.LookRotation(kickDirection);
            kickLanded = false;
            fallSpeed = 3.2f;
            Sprinting = false;
            SetMode(Mode.Kicking);
            Animate("kick", 0.05f, 1, true);
            Sound.AudioDirector.Play("whoosh", transform.position, 0.6f, 0.1f);

            return true;
        }

        void Kicking(float dt)
        {
            var length = ClipLength("kick", 0.55f);
            var t = modeTime / length;
            Walk(kickDirection * Mathf.Lerp(SprintSpeed * 1.05f, 1.5f, t), dt);

            if (!kickLanded && t >= 0.4f)
            {
                kickLanded = true;
                var any = false;

                foreach (var enemy in new List<Enemy>(Enemy.Living))
                {
                    if (enemy.Peaceful)
                    {
                        continue;
                    }

                    var to = enemy.transform.position - transform.position;
                    to.y = 0;

                    if (to.magnitude > Blows.KickReach + enemy.Radius || (to.magnitude > 0.6f && Vector3.Angle(transform.forward, to) > Blows.KickArc))
                    {
                        continue;
                    }

                    var hit = Session.KickHit(enemy.Def);
                    var zone = ZoneOn(enemy);
                    enemy.TakeHit(hit.Damage * HitZones.Damage(zone), hit.Critical || HitZones.Critical(zone), transform.position, 0);
                    enemy.Knock(transform.position, Blows.KickPush, Blows.KickStagger);
                    Landed(enemy, zone);
                    Effects.Burst(enemy.Centre - to.normalized * enemy.Radius, new Color(1f, 0.95f, 0.8f), 1.6f, 0.2f);
                    any = true;
                }

                if (any)
                {
                    Sound.AudioDirector.Play("palm_heavy", transform.position + transform.forward, 0.9f);
                    Game.Shake(0.25f);
                    Game.HitStop(0.06f);
                    SinceFight = 0;
                    sinceStrike = 0;
                }
            }

            if (t >= 1)
            {
                SetMode(Mode.Free);
            }
        }
    }
}
