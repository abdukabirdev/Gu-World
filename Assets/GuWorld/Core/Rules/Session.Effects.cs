using System;
using System.Collections.Generic;

namespace GuWorld.Core
{
    /// <summary>Something that weighs on the hero for a while: venom, fire, a wound, cold, a stun.</summary>
    public class HeroStatus
    {
        public StatusKind Kind;
        /// <summary>Damage a second (poison, burn, bleed) or the share of speed taken (slow, chill).</summary>
        public float Power;
        public float Seconds;
    }

    /// <summary>
    /// What lasts on the hero: statuses enemies leave (damage over time, slowness, stuns — the Gu of
    /// stone shrug off stuns), the buffs of Gu and pills (haste, stealth, regeneration, a burning
    /// aura), and the timed changes to the hero's numbers that pills and blessings give.
    /// </summary>
    public partial class Session
    {
        readonly List<HeroStatus> statuses = new List<HeroStatus>();
        float hasteTime, hasteValue;
        float stealthTime, stealthValue;
        float regenTime, regenValue;
        float auraTime, auraValue, auraRadius, auraStatusPower;
        StatusKind auraStatus;
        float effectWait;

        public IReadOnlyList<HeroStatus> Statuses => statuses;
        public bool Stealthed => stealthTime > 0;
        public bool Hasted => hasteTime > 0;
        public bool Regenerating => regenTime > 0;
        public bool AuraActive => auraTime > 0;
        public float AuraDamage => auraValue;
        public float AuraRadius => auraRadius;
        public StatusKind AuraStatus => auraStatus;
        public float AuraStatusPower => auraStatusPower;
        public float BuffLeft(BuffKind kind)
        {
            switch (kind)
            {
                case BuffKind.Shield: return shieldTime;
                case BuffKind.MeleeUp: return meleeTime;
                case BuffKind.Unstoppable: return unstoppableTime;
                case BuffKind.Haste: return hasteTime;
                case BuffKind.Stealth: return stealthTime;
                case BuffKind.Regen: return regenTime;
                case BuffKind.Aura: return auraTime;
                default: return 0;
            }
        }

        /// <summary>The hero cannot act (stunned or frozen).</summary>
        public bool Held => statuses.Exists(s => s.Kind == StatusKind.Stun || s.Kind == StatusKind.Freeze);

        public bool Suffers(StatusKind kind) => statuses.Exists(s => s.Kind == kind);

        /// <summary>How fast the hero moves, as a share: slowed by cold and slowness, hastened by wind.</summary>
        public float SpeedFactor
        {
            get
            {
                var factor = 1f;

                foreach (var status in statuses)
                {
                    if (status.Kind == StatusKind.Slow || status.Kind == StatusKind.Chill)
                    {
                        factor = Math.Min(factor, 1 - Math.Min(0.8f, status.Power));
                    }
                }

                if (hasteTime > 0)
                {
                    factor *= 1 + hasteValue;
                }

                return factor;
            }
        }

        /// <summary>An enemy's blow left something on the hero. Stuns do not hold the unstoppable; cold freezes the already chilled.</summary>
        public void Afflict(StatusKind kind, float power, float seconds)
        {
            if (kind == StatusKind.None || seconds <= 0 || Dead)
            {
                return;
            }

            if ((kind == StatusKind.Stun || kind == StatusKind.Freeze) && unstoppableTime > 0)
            {
                return;
            }

            if (kind == StatusKind.Chill && Suffers(StatusKind.Chill))
            {
                // A second chill while still cold: frozen for a moment.
                statuses.RemoveAll(s => s.Kind == StatusKind.Chill);
                kind = StatusKind.Freeze;
                seconds = 1.2f;
            }

            var existing = statuses.Find(s => s.Kind == kind);

            if (existing != null)
            {
                // The same status again: the stronger power, the longer time.
                existing.Power = Math.Max(existing.Power, power);
                existing.Seconds = Math.Max(existing.Seconds, seconds);
            }
            else
            {
                statuses.Add(new HeroStatus { Kind = kind, Power = power, Seconds = seconds });
                Say(StatusName(kind));
            }

            Touch();
        }

        public bool Cure(StatusKind kind)
        {
            var removed = statuses.RemoveAll(s => s.Kind == kind || (kind == StatusKind.Chill && s.Kind == StatusKind.Freeze)) > 0;

            if (removed)
            {
                Touch();
            }

            return removed;
        }

        public void CureAll()
        {
            statuses.Clear();
            Touch();
        }

        public static string StatusName(StatusKind kind)
        {
            switch (kind)
            {
                case StatusKind.Slow: return "Замедление";
                case StatusKind.Stun: return "Оглушение";
                case StatusKind.Freeze: return "Заморозка";
                case StatusKind.Poison: return "Отравление";
                case StatusKind.Burn: return "Горение";
                case StatusKind.Bleed: return "Кровотечение";
                case StatusKind.Chill: return "Озноб";
                default: return "";
            }
        }

        public static string BuffName(BuffKind kind)
        {
            switch (kind)
            {
                case BuffKind.Shield: return "Щит";
                case BuffKind.MeleeUp: return "Сила удара";
                case BuffKind.Unstoppable: return "Несокрушимость";
                case BuffKind.Stealth: return "Скрытность";
                case BuffKind.Aura: return "Аура";
                case BuffKind.Haste: return "Ускорение";
                case BuffKind.Regen: return "Восстановление";
                default: return "";
            }
        }

        /// <summary>Puts a buff on the hero (from a Gu or a pill).</summary>
        public void ApplyBuff(BuffKind kind, float value, float seconds, float radius = 0, StatusKind status = StatusKind.None, float statusPower = 0)
        {
            switch (kind)
            {
                case BuffKind.Shield:
                    shieldTime = Math.Max(shieldTime, seconds);
                    shieldValue = Math.Max(shieldTime > seconds ? shieldValue : 0, value);
                    break;
                case BuffKind.Unstoppable:
                    shieldTime = seconds;
                    shieldValue = value;
                    unstoppableTime = seconds;
                    statuses.RemoveAll(s => s.Kind == StatusKind.Stun || s.Kind == StatusKind.Freeze);
                    break;
                case BuffKind.MeleeUp:
                    meleeTime = Math.Max(meleeTime, seconds);
                    meleeValue = Math.Max(meleeValue * (meleeTime > seconds ? 1 : 0), value);
                    break;
                case BuffKind.Haste:
                    hasteTime = Math.Max(hasteTime, seconds);
                    hasteValue = Math.Max(hasteValue * (hasteTime > seconds ? 1 : 0), value);
                    break;
                case BuffKind.Stealth:
                    stealthTime = seconds;
                    stealthValue = value;
                    break;
                case BuffKind.Regen:
                    regenTime = Math.Max(regenTime, seconds);
                    regenValue = Math.Max(regenValue * (regenTime > seconds ? 1 : 0), value);
                    break;
                case BuffKind.Aura:
                    auraTime = seconds;
                    auraValue = value;
                    auraRadius = radius > 0 ? radius : 3;
                    auraStatus = status;
                    auraStatusPower = statusPower;
                    break;
            }

            Touch();
        }

        /// <summary>The hero strikes from stealth: the stealth ends, and the blow is this many times stronger (1 if not stealthed).</summary>
        public float BreakStealth()
        {
            if (stealthTime <= 0)
            {
                return 1;
            }

            stealthTime = 0;
            Touch();

            return 1 + stealthValue;
        }

        // ------------------------------------------------------------------ Timed effects of pills and blessings

        public IReadOnlyList<Effect> Effects => State.Effects;

        public void AddEffect(Effect effect)
        {
            var same = State.Effects.Find(e => e.Source == effect.Source);

            if (same != null)
            {
                same.Seconds = Math.Max(same.Seconds, effect.Seconds);
                same.Value = effect.Value;
            }
            else
            {
                State.Effects.Add(effect);
            }

            if (!effect.IsStat)
            {
                ApplyBuff(effect.Buff, effect.Value, effect.Seconds);
            }

            Recompute();
            Touch();
        }

        /// <summary>What the timed effects add to the hero's numbers (called while the numbers are worked out).</summary>
        void EffectStats(HeroStats stats, ref float regen)
        {
            foreach (var effect in State.Effects)
            {
                if (!effect.IsStat)
                {
                    continue;
                }

                switch (effect.Stat)
                {
                    case StatKind.EssenceRegen: regen += effect.Value; break;
                    case StatKind.Cost: stats.Cost += effect.Value; break;
                    case StatKind.Cooldown: stats.Cooldown += effect.Value; break;
                    case StatKind.Power: stats.Power += effect.Value; break;
                    case StatKind.MoonPower: stats.MoonPower += effect.Value; break;
                    case StatKind.DamageTaken: stats.DamageTaken += effect.Value; break;
                    case StatKind.Health: stats.MaxHealth += effect.Value; break;
                    case StatKind.Range: stats.Range += effect.Value; break;
                    case StatKind.Leech: stats.Leech += effect.Value; break;
                }
            }
        }

        /// <summary>A frame of the lasting things: damage over time, regeneration, buffs and effects running out. The damage the hero took from statuses.</summary>
        float TickEffects(float dt)
        {
            var taken = 0f;

            for (var i = statuses.Count - 1; i >= 0; i--)
            {
                var status = statuses[i];

                switch (status.Kind)
                {
                    case StatusKind.Poison:
                        // Venom goes past armour and shields.
                        taken += status.Power * dt;
                        break;
                    case StatusKind.Burn:
                        taken += status.Power * dt * Stats.DamageTaken;
                        break;
                    case StatusKind.Bleed:
                        taken += status.Power * dt * Stats.DamageTaken * (moving ? 1.6f : 1f);
                        break;
                }

                status.Seconds -= dt;

                if (status.Seconds <= 0)
                {
                    statuses.RemoveAt(i);
                    Touch();
                }
            }

            if (taken > 0 && State.Health > 0)
            {
                // Statuses wear the hero down but do not finish them on the difficulties below Hard.
                var floor = State.Settings.Difficulty <= Difficulty.Normal ? 1 : 0;
                State.Health = Math.Max(floor, State.Health - taken * DifficultyRules.DamageTaken(State.Settings.Difficulty));
            }

            if (regenTime > 0)
            {
                Heal(regenValue * dt);
            }

            hasteTime = Math.Max(0, hasteTime - dt);
            stealthTime = Math.Max(0, stealthTime - dt);
            regenTime = Math.Max(0, regenTime - dt);
            auraTime = Math.Max(0, auraTime - dt);

            if (State.Effects.Count > 0)
            {
                var expired = false;

                foreach (var effect in State.Effects)
                {
                    effect.Seconds -= dt;
                    expired |= effect.Seconds <= 0;
                }

                if (expired)
                {
                    foreach (var effect in State.Effects.FindAll(e => e.Seconds <= 0))
                    {
                        Say($"Закончилось: {effect.Name}");
                    }

                    State.Effects.RemoveAll(e => e.Seconds <= 0);
                    Recompute();
                    Touch();
                }
            }

            effectWait -= dt;

            return taken;
        }

        bool moving;

        /// <summary>The interface says whether the hero is moving (a bleeding wound bleeds more).</summary>
        public void SetMoving(bool value) => moving = value;

        /// <summary>Swallows a pill of the wider world's kinds: cures, buffs, boosts, insight. False if it would do nothing.</summary>
        bool UsePill(ItemDef def)
        {
            var useful = false;

            if (def.Cures != StatusKind.None && (Suffers(def.Cures) || (def.Cures == StatusKind.Chill && Suffers(StatusKind.Freeze))))
            {
                useful = true;
            }

            if (def.Buff != BuffKind.None || def.BoostSeconds > 0 || def.Insight > 0)
            {
                useful = true;
            }

            if ((def.Heal > 0 && State.Health < Stats.MaxHealth - 0.5f) || (def.Essence > 0 && State.Essence < Stats.MaxEssence - 0.5f))
            {
                useful = true;
            }

            if (!useful)
            {
                return false;
            }

            RemoveItem(def.Id);

            if (def.Cures != StatusKind.None)
            {
                Cure(def.Cures);
            }

            Heal(def.Heal);
            State.Essence = Math.Min(Stats.MaxEssence, State.Essence + def.Essence * Stats.MaxEssence);

            if (def.Buff != BuffKind.None)
            {
                AddEffect(new Effect { Source = def.Id, Name = def.Name, Buff = def.Buff, Value = def.BoostValue, Seconds = def.BoostSeconds });
            }
            else if (def.BoostSeconds > 0)
            {
                AddEffect(new Effect { Source = def.Id, Name = def.Name, IsStat = true, Stat = def.BoostStat, Value = def.BoostValue, Seconds = def.BoostSeconds });
            }

            if (def.Insight > 0)
            {
                GainInsight(def.Insight);
            }

            Touch();

            return true;
        }
    }
}
