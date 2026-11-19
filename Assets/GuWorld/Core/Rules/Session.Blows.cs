using System;

namespace GuWorld.Core
{
    /// <summary>The finishing blow and the running kick (ROADMAP 13); the numbers are Blows'.</summary>
    public partial class Session
    {
        /// <summary>The third palm's worth against a beast, before luck (no critical, no spread).</summary>
        float PalmWorth(EnemyDef target) => HeroStats.ComboDamage[2] * Stats.Melee * (MeleeUp ? 1 + meleeValue : 1) * (1 - (target?.Armour ?? 0));

        /// <summary>The finishing blow on an open beast: its worth (always a telling one), essence for it twice a palm's, the arms grow.</summary>
        public Hit Finish(EnemyDef target, float health, float maxHealth)
        {
            var palm = PalmWorth(target);
            var damage = Blows.FinishDamage(palm, health, maxHealth, target != null && target.Boss);
            State.Essence = Math.Min(Stats.MaxEssence, State.Essence + Stats.EssencePerHit * 2);
            Train(Trait.Strength, 3);

            if (Stats.Leech > 0)
            {
                // What the blow drinks is a palm's worth or four, not the whole of a beast's life.
                Heal(Math.Min(damage, palm * 4) * Stats.Leech);
            }

            return new Hit { Damage = damage, Critical = true };
        }

        /// <summary>The breath for a running kick: false (and nothing spent) if there is not enough.</summary>
        public bool BeginKick() => Godmode || SpendStamina(Blows.KickStamina);

        /// <summary>The running kick landing on a beast: its worth; the legs and the balance grow.</summary>
        public Hit KickHit(EnemyDef target)
        {
            var damage = PalmWorth(target) * Blows.KickShare * BreakStealth() * (0.92f + 0.16f * (float)Random.NextDouble());
            State.Essence = Math.Min(Stats.MaxEssence, State.Essence + Stats.EssencePerHit);
            Train(Trait.Agility, 2);

            return new Hit { Damage = damage };
        }
    }
}
