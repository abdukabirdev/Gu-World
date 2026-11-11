using System;

namespace GuWorld.Core
{
    /// <summary>The ladder of cultivation: twelve steps, four to a rank.</summary>
    public static class Cultivation
    {
        public const int MaxStep = 11;
        /// <summary>Insight to go from step i to step i + 1.</summary>
        public static readonly int[] Needs = { 100, 180, 300, 500, 700, 1000, 1400, 2000, 2600, 3400, 4400 };
        /// <summary>Primeval stones a breakthrough to the next rank burns.</summary>
        public static readonly int[] StoneCost = { 0, 0, 0, 50, 0, 0, 0, 200, 0, 0, 0 };
        public static readonly string[] StageNames = { "начальная ступень", "средняя ступень", "высшая ступень", "пиковая ступень" };
        /// <summary>Insight one primeval stone gives in meditation.</summary>
        public const float StoneInsight = 3;

        public static int Rank(int step) => Math.Min(3, step / 4 + 1);
        public static int Stage(int step) => step % 4;
        public static string Name(int step) => $"Ранг {GuBook.RankNumerals[Rank(step)]} · {StageNames[Stage(step)]}";
        public static bool IsRankUp(int fromStep) => fromStep == 3 || fromStep == 7;

        /// <summary>The story flag a rank's gate waits for.</summary>
        public static string Gate(int fromStep) => fromStep == 3 ? "ch3" : fromStep == 7 ? "ch5" : null;

        public static float EssenceScale(int rank) => rank <= 1 ? 100 : rank == 2 ? 220 : 480;
        public static int ActiveSlots(int step) => step >= 4 ? 4 : step >= 2 ? 3 : 2;
        public static int PassiveSlots(int step) => step >= 8 ? 3 : step >= 4 ? 2 : 1;
        public static string TalentName(float talent) => talent >= 0.99f ? "S" : talent >= 0.8f ? "A" : talent >= 0.6f ? "B" : talent >= 0.4f ? "C" : "D";

        public static readonly int[] MasteryNeeds = { 0, 30, 90, 200, 400 };

        public static int MasteryLevel(int xp)
        {
            var level = 1;

            for (var i = 1; i < MasteryNeeds.Length; i++)
            {
                if (xp >= MasteryNeeds[i])
                {
                    level = i + 1;
                }
            }

            return level;
        }

        /// <summary>How far to the next mastery level, 0–1 (1 at the top).</summary>
        public static float MasteryShare(int xp)
        {
            var level = MasteryLevel(xp);

            if (level >= MasteryNeeds.Length)
            {
                return 1;
            }

            var from = MasteryNeeds[level - 1];
            var to = MasteryNeeds[level];

            return (xp - from) / (float)(to - from);
        }
    }

    /// <summary>What the hero is worth right now: the step, the Gu in the slots and their affixes, all added up.</summary>
    public class HeroStats
    {
        public float MaxHealth;
        public float MaxEssence;
        /// <summary>Essence per second.</summary>
        public float EssenceRegen;
        public float Power = 1;
        public float MoonPower = 1;
        public float Cost = 1;
        public float Cooldown = 1;
        public float Range = 1;
        public float DamageTaken = 1;
        public float Leech;
        public float WardPeriod;
        public float Melee = 1;
        public int ActiveSlots;
        public int PassiveSlots;

        public static readonly float[] ComboDamage = { 9, 9, 17 };
        public const float MaxStamina = 100;
        public const float RollStamina = 25;
        public const float SprintStamina = 10;
        public const float StaminaRegen = 32;

        /// <summary>Essence a palm strike that lands gives back.</summary>
        public float EssencePerHit => 3 + MaxEssence * 0.05f;
    }
}
