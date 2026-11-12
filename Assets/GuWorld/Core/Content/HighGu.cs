namespace GuWorld.Core
{
    /// <summary>
    /// The Gu of the fourth and fifth ranks, the masters' (fused from the third rank's with the hearts
    /// and cores of the mountain's great beasts), and the one Gu that is not of any rank a master can
    /// reach: the Spring Autumn Cicada.
    /// </summary>
    public static class HighGu
    {
        public static readonly GuDef[] All =
        {
            // ------------------------------------------------------------------ Rank 4

            new GuDef
            {
                Id = "moon_sickle", Name = "Гу Лунного серпа", Seal = "镰", Rank = 4, Path = GuPath.Moon, Kind = GuKind.Active, Colour = 0xD8E8FF, Price = 1800, Food = "moon_orchid", Appetite = 2, Clan = "gu_yue",
                Description = "Пять серпов лунного света, каждый — в рост человека. Проходят сквозь всё.",
                Skill = new SkillDef { Shape = SkillShape.Fan, Power = 96, Count = 5, Cost = 60, Cooldown = 1.6f, Range = 24, Radius = 1.6f, Speed = 34, Pierce = true }
            },
            new GuDef
            {
                Id = "north_wind_ice", Name = "Гу Северного ледяного ветра", Seal = "朔", Rank = 4, Path = GuPath.Ice, Kind = GuKind.Active, Colour = 0xCFF6FF, Price = 1700, Food = "frost_lotus", Appetite = 2, Clan = "bai",
                Description = "Буря стужи в точке прицела: всё внутри медленнеет, а кто стоит до конца — замерзает.",
                Skill = new SkillDef { Shape = SkillShape.Storm, Power = 52, Range = 20, Radius = 8, Duration = 5, Count = 10, Cost = 70, Cooldown = 16, Status = StatusKind.Chill, StatusPower = 0.4f, StatusTime = 3 }
            },
            new GuDef
            {
                Id = "golden_bell", Name = "Гу Золотого колокола", Seal = "钟", Rank = 4, Path = GuPath.Earth, Kind = GuKind.Passive, Colour = 0xF2C850, Price = 2200, Food = "stone_dust", Appetite = 5,
                Description = "Тело под золотым колоколом: урон меньше на пятую часть, здоровье +160.",
                Passive = new[] { new StatBonus(StatKind.DamageTaken, -0.2f), new StatBonus(StatKind.Health, 160) }
            },
            new GuDef
            {
                Id = "myriad_lightning", Name = "Гу Мириад молний", Seal = "霆", Rank = 4, Path = GuPath.Lightning, Kind = GuKind.Active, Colour = 0xC8B8FF, Price = 1900, Food = "spark_moss", Appetite = 2,
                Description = "Молния, что прыгает с врага на врага восемь раз и не теряет силы.",
                Skill = new SkillDef { Shape = SkillShape.Chain, Power = 70, Range = 20, Radius = 9, Count = 8, Cost = 55, Cooldown = 4 }
            },
            new GuDef
            {
                Id = "crimson_flame", Name = "Гу Багрового пламени", Seal = "焱", Rank = 4, Path = GuPath.Fire, Kind = GuKind.Active, Colour = 0xFF5030, Price = 1800, Food = "ember_coal", Appetite = 2,
                Description = "Багровое пламя вырывается из тела на восемь шагов. Что выжило — горит.",
                Skill = new SkillDef { Shape = SkillShape.Nova, Power = 110, Radius = 8, Cost = 65, Cooldown = 11, Status = StatusKind.Burn, StatusPower = 22, StatusTime = 5 }
            },
            new GuDef
            {
                Id = "stone_giant", Name = "Гу Каменного великана", Seal = "巨", Rank = 4, Path = GuPath.Strength, Kind = GuKind.Active, Colour = 0xC8A878, Price = 2000, Food = "jerky", Appetite = 2, Clan = "xiong",
                Description = "Пятнадцать секунд вы — великан из камня: удары вдвое сильнее, вас не сдвинуть, урон вполовину.",
                Skill = new SkillDef { Shape = SkillShape.Buff, Buff = BuffKind.Unstoppable, BuffValue = 0.5f, Duration = 15, Cost = 80, Cooldown = 32 }
            },
            new GuDef
            {
                Id = "immortal_shadow", Name = "Гу Бессмертной тени", Seal = "魅", Rank = 4, Path = GuPath.Dark, Kind = GuKind.Passive, Colour = 0x8070C0, Price = 2400, Food = "night_moth", Appetite = 4,
                Description = "Тень пьёт за вас: седьмая часть урона возвращается здоровьем, дальность Гу больше.",
                Passive = new[] { new StatBonus(StatKind.Leech, 0.14f), new StatBonus(StatKind.Range, 0.15f) }
            },
            new GuDef
            {
                Id = "ten_thousand_li", Name = "Гу Десяти тысяч ли", Seal = "里", Rank = 4, Path = GuPath.Wind, Kind = GuKind.Active, Colour = 0xD8FFF0, Price = 1700, Food = "dew_gourd", Appetite = 3,
                Description = "Шаг в двадцать шагов. Ветер, что остаётся позади, режет.",
                Skill = new SkillDef { Shape = SkillShape.Dash, Power = 60, Range = 20, Radius = 2.5f, Cost = 40, Cooldown = 5 }
            },

            // ------------------------------------------------------------------ Rank 5

            new GuDef
            {
                Id = "full_moon", Name = "Гу Полной луны", Seal = "望", Rank = 5, Path = GuPath.Moon, Kind = GuKind.Active, Colour = 0xF0F6FF, Price = 6000, Food = "moon_orchid", Appetite = 1, Clan = "gu_yue",
                Description = "Полная луна встаёт над точкой прицела, и шесть секунд с неё падает свет, режущий камень.",
                Skill = new SkillDef { Shape = SkillShape.Storm, Power = 120, Range = 26, Radius = 10, Duration = 6, Count = 14, Cost = 160, Cooldown = 24 }
            },
            new GuDef
            {
                Id = "heaven_ice", Name = "Гу Небесного льда", Seal = "凌", Rank = 5, Path = GuPath.Ice, Kind = GuKind.Active, Colour = 0xE8FFFF, Price = 5800, Food = "frost_lotus", Appetite = 1, Clan = "bai",
                Description = "Небо роняет лёд перед вами: всё в двенадцати шагах замерзает на четыре секунды.",
                Skill = new SkillDef { Shape = SkillShape.Cone, Power = 180, Range = 12, Radius = 70, Cost = 150, Cooldown = 20, Status = StatusKind.Freeze, StatusTime = 4 }
            },
            new GuDef
            {
                Id = "immortal_body", Name = "Гу Бессмертного тела", Seal = "身", Rank = 5, Path = GuPath.Strength, Kind = GuKind.Passive, Colour = 0xF8E0C0, Price = 7000, Food = "jerky", Appetite = 3,
                Description = "Тело, которое не берёт ни меч, ни яд: урон меньше на треть, здоровье +400.",
                Passive = new[] { new StatBonus(StatKind.DamageTaken, -0.3f), new StatBonus(StatKind.Health, 400) }
            },
            new GuDef
            {
                Id = "heaven_thunder", Name = "Гу Небесного грома", Seal = "震", Rank = 5, Path = GuPath.Lightning, Kind = GuKind.Active, Colour = 0xE0D0FF, Price = 6200, Food = "spark_moss", Appetite = 1,
                Description = "Гром небес бьёт всё в десяти шагах от вас — и оглушает.",
                Skill = new SkillDef { Shape = SkillShape.Nova, Power = 200, Radius = 10, Cost = 170, Cooldown = 18, Status = StatusKind.Stun, StatusTime = 2.5f }
            },
            new GuDef
            {
                Id = "boundless_sea", Name = "Гу Безбрежного моря", Seal = "海", Rank = 5, Path = GuPath.Water, Kind = GuKind.Passive, Colour = 0x60B8F0, Price = 6500, Food = "dew_gourd", Appetite = 3,
                Description = "Апертура как море: эссенция втрое быстрее, Гу берут на пятую часть меньше.",
                Passive = new[] { new StatBonus(StatKind.EssenceRegen, 2f), new StatBonus(StatKind.Cost, -0.2f) }
            },

            // ------------------------------------------------------------------ The Cicada

            new GuDef
            {
                Id = "spring_autumn_cicada", Name = "Цикада Весны и Осени", Seal = "蝉", Rank = 1, Path = GuPath.Light, Kind = GuKind.Passive, Colour = 0xA8E0B0, Price = 0, Food = "", Clan = "",
                Description = "Гу шестого ранга, что дремлет в апертуре и не просит еды. Раз в три дня возвращает хозяина из смерти — целым, как весна после осени. И чуть быстрее наполняет апертуру.",
                Passive = new[] { new StatBonus(StatKind.EssenceRegen, 0.1f) }
            },
        };

        /// <summary>The Gu that brings its master back from death.</summary>
        public const string Cicada = "spring_autumn_cicada";
        /// <summary>Days the Cicada sleeps after it has saved its master.</summary>
        public const int CicadaSleep = 3;

        public static readonly RecipeDef[] Recipes =
        {
            new RecipeDef { Id = "moon_sickle", Result = "moon_sickle", Gu = new[] { "golden_moon", "moon_shadow" }, Items = new[] { ("ancient_relic", 1), ("moon_orchid", 10) }, Stones = 900 },
            new RecipeDef { Id = "north_wind_ice", Result = "north_wind_ice", Gu = new[] { "ice_lotus", "whirl" }, Items = new[] { ("frost_lotus", 10), ("spider_queen_eye", 1) }, Stones = 900 },
            new RecipeDef { Id = "golden_bell", Result = "golden_bell", Gu = new[] { "sky_canopy", "rock_skin" }, Items = new[] { ("bear_king_heart", 1), ("jade_shard", 6) }, Stones = 1000 },
            new RecipeDef { Id = "myriad_lightning", Result = "myriad_lightning", Gu = new[] { "thunder_crown", "river_serpent" }, Items = new[] { ("crown_core", 1), ("eagle_feather", 8) }, Stones = 950 },
            new RecipeDef { Id = "crimson_flame", Result = "crimson_flame", Gu = new[] { "blazing_feather", "flame_burst" }, Items = new[] { ("ember_coal", 15), ("python_core", 1) }, Stones = 900 },
            new RecipeDef { Id = "stone_giant", Result = "stone_giant", Gu = new[] { "great_bear", "rock_skin" }, Items = new[] { ("bear_king_heart", 1), ("iron_ore", 12) }, Stones = 950 },
            new RecipeDef { Id = "immortal_shadow", Result = "immortal_shadow", Gu = new[] { "dark_tide", "blood_skull" }, Items = new[] { ("spider_queen_eye", 1), ("night_moth", 10) }, Stones = 1100 },
            new RecipeDef { Id = "ten_thousand_li", Result = "ten_thousand_li", Gu = new[] { "thunder_wing", "storm_step" }, Items = new[] { ("eagle_feather", 12), ("storm_core", 4) }, Stones = 850 },
            new RecipeDef { Id = "full_moon", Result = "full_moon", Gu = new[] { "moon_sickle", "moon_lake" }, Items = new[] { ("heart_of_qingmao", 1), ("moon_orchid", 20) }, Stones = 3000 },
            new RecipeDef { Id = "heaven_ice", Result = "heaven_ice", Gu = new[] { "north_wind_ice", "frost_demon" }, Items = new[] { ("heart_of_qingmao", 1), ("frost_lotus", 20) }, Stones = 3000 },
            new RecipeDef { Id = "immortal_body", Result = "immortal_body", Gu = new[] { "golden_bell", "ancient_bone" }, Items = new[] { ("heart_of_qingmao", 1), ("ancient_relic", 1) }, Stones = 3500 },
            new RecipeDef { Id = "heaven_thunder", Result = "heaven_thunder", Gu = new[] { "myriad_lightning", "thunder_crown" }, Items = new[] { ("heart_of_qingmao", 1), ("storm_core", 10) }, Stones = 3200 },
            new RecipeDef { Id = "boundless_sea", Result = "boundless_sea", Gu = new[] { "four_wines", "deep_breath" }, Items = new[] { ("heart_of_qingmao", 1), ("dew_gourd", 30) }, Stones = 3000 },
        };

        public static readonly ItemDef[] Items =
        {
            new ItemDef { Id = "heart_of_qingmao", Name = "Сердце Цинмао", Seal = "髓", Kind = ItemKind.Material, Price = 2000, Colour = 0x9FEFC4, Description = "Камень из-под корней Великого древа и льда вершины: в нём бьётся сама гора. Без него не сплавить Гу пятого ранга." },
            new ItemDef { Id = "scroll_moon_sickle", Name = "Свиток: Гу Лунного серпа", Seal = "卷", Kind = ItemKind.Scroll, Teaches = "moon_sickle", Price = 1200, Description = "Тайный рецепт четвёртого ранга клана Гу Юэ." },
            new ItemDef { Id = "scroll_north_wind_ice", Name = "Свиток: Гу Северного ледяного ветра", Seal = "卷", Kind = ItemKind.Scroll, Teaches = "north_wind_ice", Price = 1200, Description = "Тайный рецепт четвёртого ранга клана Бай." },
            new ItemDef { Id = "scroll_golden_bell", Name = "Свиток: Гу Золотого колокола", Seal = "卷", Kind = ItemKind.Scroll, Teaches = "golden_bell", Price = 1300, Description = "Рецепт четвёртого ранга, выбитый на бронзе." },
            new ItemDef { Id = "scroll_myriad_lightning", Name = "Свиток: Гу Мириад молний", Seal = "卷", Kind = ItemKind.Scroll, Teaches = "myriad_lightning", Price = 1250, Description = "Рецепт четвёртого ранга." },
            new ItemDef { Id = "scroll_crimson_flame", Name = "Свиток: Гу Багрового пламени", Seal = "卷", Kind = ItemKind.Scroll, Teaches = "crimson_flame", Price = 1200, Description = "Рецепт четвёртого ранга, опалённый по краям." },
            new ItemDef { Id = "scroll_stone_giant", Name = "Свиток: Гу Каменного великана", Seal = "卷", Kind = ItemKind.Scroll, Teaches = "stone_giant", Price = 1250, Description = "Тайный рецепт четвёртого ранга клана Сюн." },
            new ItemDef { Id = "scroll_immortal_shadow", Name = "Свиток: Гу Бессмертной тени", Seal = "卷", Kind = ItemKind.Scroll, Teaches = "immortal_shadow", Price = 1400, Description = "Рецепт демонического пути четвёртого ранга." },
            new ItemDef { Id = "scroll_ten_thousand_li", Name = "Свиток: Гу Десяти тысяч ли", Seal = "卷", Kind = ItemKind.Scroll, Teaches = "ten_thousand_li", Price = 1150, Description = "Рецепт четвёртого ранга." },
            new ItemDef { Id = "scroll_full_moon", Name = "Свиток: Гу Полной луны", Seal = "卷", Kind = ItemKind.Scroll, Teaches = "full_moon", Price = 4000, Description = "Рецепт пятого ранга. Основатель Гу Юэ сплавил его один раз за жизнь." },
            new ItemDef { Id = "scroll_heaven_ice", Name = "Свиток: Гу Небесного льда", Seal = "卷", Kind = ItemKind.Scroll, Teaches = "heaven_ice", Price = 4000, Description = "Рецепт пятого ранга клана Бай." },
            new ItemDef { Id = "scroll_immortal_body", Name = "Свиток: Гу Бессмертного тела", Seal = "卷", Kind = ItemKind.Scroll, Teaches = "immortal_body", Price = 4500, Description = "Рецепт пятого ранга из саркофага Древнего мастера." },
            new ItemDef { Id = "scroll_heaven_thunder", Name = "Свиток: Гу Небесного грома", Seal = "卷", Kind = ItemKind.Scroll, Teaches = "heaven_thunder", Price = 4200, Description = "Рецепт пятого ранга." },
            new ItemDef { Id = "scroll_boundless_sea", Name = "Свиток: Гу Безбрежного моря", Seal = "卷", Kind = ItemKind.Scroll, Teaches = "boundless_sea", Price = 4000, Description = "Рецепт пятого ранга." },
        };

        /// <summary>Where the high scrolls and the mountain's heart are found (added to the tables of the great beasts and the deepest chests).</summary>
        public static readonly LootTable[] Tables =
        {
            new LootTable { Id = "high_gu_yue", Items = new[] { new LootEntry { Item = "scroll_moon_sickle" }, new LootEntry { Item = "scroll_full_moon", Chance = 0.5f } } },
            new LootTable { Id = "heart_peak", Items = new[] { new LootEntry { Item = "heart_of_qingmao" }, new LootEntry { Item = "scroll_heaven_thunder" } }, StonesMin = 400, StonesMax = 600 },
            new LootTable { Id = "heart_tree", Items = new[] { new LootEntry { Item = "heart_of_qingmao" }, new LootEntry { Item = "scroll_boundless_sea" } }, StonesMin = 400, StonesMax = 600 },
        };
    }
}
