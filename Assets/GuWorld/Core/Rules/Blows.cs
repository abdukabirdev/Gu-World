using System;

namespace GuWorld.Core
{
    /// <summary>
    /// Two blows beyond the palm's three (ROADMAP 13: «удар ногой, добивание»): the finishing blow on a beast left
    /// open — reeling from a parry, or stunned — struck from close by; and the running kick, a leap with a foot
    /// into a beast that throws it back and leaves it reeling, for stamina.
    /// </summary>
    public static class Blows
    {
        // ------------------------------------------------------------------ The finishing blow

        /// <summary>How near (metres to the beast's edge) and how wide ahead (degrees) the finishing blow reaches.</summary>
        public const float FinishReach = 2.4f, FinishArc = 70;

        /// <summary>How long a parried beast is left open (a great one only for a blink).</summary>
        public static float OpenAfterParry(bool boss) => boss ? 0.5f : 1.4f;

        /// <summary>
        /// The finishing blow's worth, from the third palm's (`palm`): a common beast with half its health or less
        /// left dies of it; a fuller one loses half its health (four palms at least); a great one takes four palms.
        /// </summary>
        public static float FinishDamage(float palm, float health, float maxHealth, bool boss)
        {
            if (boss)
            {
                return palm * 4;
            }

            return health <= maxHealth * 0.5f ? health + 1 : Math.Max(palm * 4, maxHealth * 0.5f);
        }

        // ------------------------------------------------------------------ The running kick

        /// <summary>The breath a running kick takes.</summary>
        public const float KickStamina = 15;

        /// <summary>The kick's worth against the third palm's; how far it throws a beast, how long the beast reels (a great one is barely moved, not made to reel).</summary>
        public const float KickShare = 1.2f, KickPush = 2.6f, KickStagger = 0.7f;

        /// <summary>How far ahead and how wide the kick reaches.</summary>
        public const float KickReach = 2.6f, KickArc = 55;
    }
}
