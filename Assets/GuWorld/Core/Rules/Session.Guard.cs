using System;

namespace GuWorld.Core
{
    /// <summary>What a raised guard made of a blow.</summary>
    public struct GuardResult
    {
        /// <summary>The share of the blow that still lands (1: not guarded).</summary>
        public float Share;
        /// <summary>Caught in the first instant: nothing lands, the attacker reels.</summary>
        public bool Parried;
        /// <summary>The guard gave way (no stamina left): the hero staggers.</summary>
        public bool Broken;
    }

    /// <summary>
    /// The guard: hands raised (and the aperture's essence braced) against blows from in front. A blow
    /// caught in the first fifth of a second after raising is parried — nothing lands and the attacker
    /// reels; later ones are blocked — a third lands, and stamina pays for the rest; a blow from behind,
    /// or with no stamina left, breaks through. Strength path attainment makes the guard firmer.
    /// </summary>
    public partial class Session
    {
        public const float ParryWindow = 0.22f;
        public const float GuardShare = 0.35f;

        /// <summary>Stamina a blocked blow costs (less for a master of the Strength path).</summary>
        public float GuardCost(float raw) => (8 + raw * 0.35f) * (1 - 0.08f * GradeOf(GuPath.Strength));

        /// <summary>Works out a blow on a raised guard; `sinceRaised` is how long the guard has been up, `facing` whether the blow comes from in front.</summary>
        public GuardResult Guard(float raw, float sinceRaised, bool facing)
        {
            if (!facing || raw <= 0)
            {
                return new GuardResult { Share = 1 };
            }

            if (sinceRaised <= ParryWindow)
            {
                State.Parries++;
                GainAttainment(GuPath.Strength, 2);

                if (State.Parries % 10 == 0)
                {
                    Achieve();
                }

                return new GuardResult { Share = 0, Parried = true };
            }

            var cost = GuardCost(raw);

            if (!SpendStamina(cost))
            {
                Stamina = 0;

                return new GuardResult { Share = 0.8f, Broken = true };
            }

            return new GuardResult { Share = GuardShare * (1 - 0.04f * GradeOf(GuPath.Strength)) };
        }
    }
}
