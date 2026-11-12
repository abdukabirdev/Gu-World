using System;

namespace GuWorld.Core
{
    /// <summary>The ladder of cultivation: twenty steps, four to a rank, five mortal ranks (the sixth is an immortal's).</summary>
    public static class Cultivation
    {
        public const int MaxStep = 19;
        public const int MaxRank = 5;
        /// <summary>Insight to go from step i to step i + 1.</summary>
        public static readonly int[] Needs = { 100, 180, 300, 500, 700, 1000, 1400, 2000, 2600, 3400, 4400, 5600, 7000, 8600, 10500, 12800, 15500, 18500, 22000 };
        /// <summary>Primeval stones a breakthrough to the next rank burns.</summary>
        public static readonly int[] StoneCost = { 0, 0, 0, 50, 0, 0, 0, 200, 0, 0, 0, 600, 0, 0, 0, 1500, 0, 0, 0 };
        public static readonly string[] StageNames = { "начальная ступень", "средняя ступень", "высшая ступень", "пиковая ступень" };
        /// <summary>Insight one primeval stone gives in meditation.</summary>
        public const float StoneInsight = 3;

        public static int Rank(int step) => Math.Min(MaxRank, step / 4 + 1);
        public static int Stage(int step) => step % 4;
        public static string Name(int step) => $"Ранг {GuBook.RankNumerals[Rank(step)]} · {StageNames[Stage(step)]}";
        public static bool IsRankUp(int fromStep) => fromStep % 4 == 3 && fromStep < MaxStep;

        /// <summary>The story flag a rank's gate waits for: the third rank after the valley's fourth chapter, the fourth after its end, the fifth after the Ancient Master's tomb.</summary>
        public static string Gate(int fromStep) => fromStep == 3 ? "ch3" : fromStep == 7 ? "ch5" : fromStep == 11 ? "finished" : fromStep == 15 ? "tomb_cleared" : null;

        public static float EssenceScale(int rank) => rank <= 1 ? 100 : rank == 2 ? 220 : rank == 3 ? 480 : rank == 4 ? 1000 : 2000;
        public static int ActiveSlots(int step) => step >= 16 ? 6 : step >= 12 ? 5 : step >= 4 ? 4 : step >= 2 ? 3 : 2;
        public static int PassiveSlots(int step) => step >= 16 ? 5 : step >= 12 ? 4 : step >= 8 ? 3 : step >= 4 ? 2 : 1;

        /// <summary>The colour of a rank's primeval essence (the canon's: green copper, red steel, white silver, yellow gold, purple crystal).</summary>
        public static int EssenceColour(int rank) => rank <= 1 ? 0x5FB88A : rank == 2 ? 0xD0584A : rank == 3 ? 0xE8EEF2 : rank == 4 ? 0xF2C850 : 0xB07AE8;

        public static string EssenceName(int rank) => rank <= 1 ? "Зелёная медь" : rank == 2 ? "Красная сталь" : rank == 3 ? "Белое серебро" : rank == 4 ? "Жёлтое золото" : "Пурпурный кристалл";
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
