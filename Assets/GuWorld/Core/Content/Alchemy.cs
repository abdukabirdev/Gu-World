using System.Collections.Generic;

namespace GuWorld.Core
{
    /// <summary>A pill refined at a furnace: what goes in, how hard it is, what comes out.</summary>
    public class PillRecipeDef
    {
        public string Id;
        public string Result;
        public int Count = 1;
        public (string item, int count)[] Items = new (string, int)[0];
        public int Stones;
        /// <summary>The refiner's skill it wants: below it a batch may fail, above it a batch may give one more.</summary>
        public int Difficulty = 1;
        /// <summary>Known from the start; otherwise learnt from a book or a teacher (Teacher: an NPC id).</summary>
        public bool Known;
        public string Teacher = "";
    }

    /// <summary>The refiner's recipes and the rules of the furnace.</summary>
    public static class Alchemy
    {
        public static readonly PillRecipeDef[] All =
        {
            new PillRecipeDef { Id = "heal_pill", Result = "heal_pill", Count = 2, Items = new[] { ("ginseng", 1), ("bitter_root", 1) }, Stones = 2, Difficulty = 1, Known = true },
            new PillRecipeDef { Id = "essence_pill", Result = "essence_pill", Count = 2, Items = new[] { ("moon_orchid", 1), ("bitter_root", 1) }, Stones = 3, Difficulty = 1, Known = true },
            new PillRecipeDef { Id = "antidote", Result = "antidote", Count = 2, Items = new[] { ("red_cap", 1), ("marsh_reed", 1) }, Stones = 2, Difficulty = 1, Known = true },
            new PillRecipeDef { Id = "bandage", Result = "bandage", Count = 2, Items = new[] { ("bitter_root", 1), ("spider_silk", 1) }, Stones = 1, Difficulty = 1, Teacher = "bai_lin" },
            new PillRecipeDef { Id = "burn_salve", Result = "burn_salve", Count = 2, Items = new[] { ("frost_lotus", 1), ("bitter_root", 1) }, Stones = 2, Difficulty = 2, Teacher = "bai_lin" },
            new PillRecipeDef { Id = "warming_pill", Result = "warming_pill", Count = 2, Items = new[] { ("sun_bloom", 1), ("ember_coal", 1) }, Stones = 2, Difficulty = 2, Teacher = "wandering_alchemist" },
            new PillRecipeDef { Id = "big_heal_pill", Result = "big_heal_pill", Items = new[] { ("ginseng", 2), ("blood_vine", 1), ("bitter_root", 1) }, Stones = 10, Difficulty = 3, Teacher = "bai_lin" },
            new PillRecipeDef { Id = "great_essence_pill", Result = "great_essence_pill", Items = new[] { ("moon_orchid", 2), ("spirit_moss", 1), ("bitter_root", 1) }, Stones = 12, Difficulty = 3, Teacher = "wandering_alchemist" },
            new PillRecipeDef { Id = "clarity_pill", Result = "clarity_pill", Items = new[] { ("spirit_moss", 2), ("cloud_tea", 1) }, Stones = 25, Difficulty = 4, Teacher = "wandering_alchemist" },
            new PillRecipeDef { Id = "iron_pill", Result = "iron_pill", Items = new[] { ("iron_ore", 2), ("ginseng", 1) }, Stones = 8, Difficulty = 2, Teacher = "xiong_meng" },
            new PillRecipeDef { Id = "fury_pill", Result = "fury_pill", Items = new[] { ("bear_gall", 1), ("bear_garlic", 2) }, Stones = 8, Difficulty = 2, Teacher = "xiong_hua" },
            new PillRecipeDef { Id = "wind_pill", Result = "wind_pill", Items = new[] { ("eagle_feather", 1), ("dew_gourd", 1) }, Stones = 6, Difficulty = 2, Teacher = "wandering_alchemist" },
            new PillRecipeDef { Id = "shadow_pill", Result = "shadow_pill", Items = new[] { ("night_moth", 2), ("bat_wing", 1) }, Stones = 15, Difficulty = 3, Teacher = "wandering_alchemist" },
            new PillRecipeDef { Id = "regen_pill", Result = "regen_pill", Items = new[] { ("honey", 1), ("ginseng", 1) }, Stones = 6, Difficulty = 2, Teacher = "xiong_hua" },
            new PillRecipeDef { Id = "focus_pill", Result = "focus_pill", Items = new[] { ("spirit_moss", 1), ("spark_moss", 1), ("bitter_root", 1) }, Stones = 15, Difficulty = 3, Teacher = "wandering_alchemist" },
            new PillRecipeDef { Id = "thrift_pill", Result = "thrift_pill", Items = new[] { ("cloud_tea", 1), ("moon_orchid", 1), ("bitter_root", 1) }, Stones = 12, Difficulty = 3, Teacher = "bai_lin" },
            new PillRecipeDef { Id = "jerky", Result = "jerky", Count = 3, Items = new[] { ("wolf_pelt", 1) }, Stones = 0, Difficulty = 1, Known = true },
            new PillRecipeDef { Id = "stone_dust", Result = "stone_dust", Count = 4, Items = new[] { ("iron_ore", 1) }, Stones = 0, Difficulty = 1, Known = true },
        };

        /// <summary>The highest refining skill.</summary>
        public const int MaxSkill = 5;

        /// <summary>Refines a batch needs to rise a level of skill (from level 1, 2, 3, 4).</summary>
        public static readonly int[] SkillNeeds = { 0, 6, 14, 26, 42 };

        static Dictionary<string, PillRecipeDef> byId;

        public static PillRecipeDef Get(string id)
        {
            byId = byId ?? Lists.Index(All, r => r.Id);

            return id != null && byId.TryGetValue(id, out var found) ? found : null;
        }

        /// <summary>The chance a batch comes out, from the refiner's skill against the recipe's difficulty.</summary>
        public static float SuccessChance(int skill, int difficulty)
        {
            var gap = skill - difficulty;

            if (gap >= 0)
            {
                return 1;
            }

            return gap == -1 ? 0.6f : gap == -2 ? 0.3f : 0.1f;
        }

        /// <summary>The chance a batch gives one pill more than it should.</summary>
        public static float BonusChance(int skill, int difficulty) => skill > difficulty ? 0.15f * (skill - difficulty) : 0;
    }
}
