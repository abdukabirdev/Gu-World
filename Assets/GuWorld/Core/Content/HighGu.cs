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
                Description = "Полная луна встаёт над точкой прицела, и шесть секунд с неё падает свет, режущий камень. Каждые десять секунд.",
                Skill = new SkillDef { Shape = SkillShape.Storm, Power = 150, Range = 26, Radius = 10, Duration = 6, Count = 14, Cost = 150, Cooldown = 10 }
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

            // ------------------------------------------------------------------ The paths' high Gu (the second batch)

            new GuDef
            {
                Id = "spring_life", Name = "Гу Весенней жизни", Seal = "春", Rank = 4, Path = GuPath.Wood, Kind = GuKind.Active, Colour = 0x9FEF9F, Price = 1800, Food = "ginseng", Appetite = 3,
                Description = "Весна внутри тела: за четыре секунды возвращает много здоровья, а потом ещё полминуты тело само затягивает раны.",
                Skill = new SkillDef { Shape = SkillShape.Heal, Power = 420, Duration = 4, Cost = 90, Cooldown = 24 }
            },
            new GuDef
            {
                Id = "tree_of_life", Name = "Гу Древа жизни", Seal = "樹", Rank = 5, Path = GuPath.Wood, Kind = GuKind.Passive, Colour = 0x7FD07F, Price = 6400, Food = "ginseng", Appetite = 3,
                Description = "Корни в апертуре: здоровье +350, эссенция восполняется вдвое быстрее.",
                Passive = new[] { new StatBonus(StatKind.Health, 350), new StatBonus(StatKind.EssenceRegen, 1f) }
            },
            new GuDef
            {
                Id = "sky_wind", Name = "Гу Небесного ветра", Seal = "颶", Rank = 5, Path = GuPath.Wind, Kind = GuKind.Active, Colour = 0xE0FFF4, Price = 6000, Food = "dew_gourd", Appetite = 2,
                Description = "Ураган сходит с неба на место прицела и семь секунд режет и тянет всё в себя.",
                Skill = new SkillDef { Shape = SkillShape.Storm, Power = 140, Range = 26, Radius = 11, Duration = 7, Count = 16, Cost = 150, Cooldown = 12, Status = StatusKind.Slow, StatusPower = 0.5f, StatusTime = 2 }
            },
            new GuDef
            {
                Id = "venom_dragon", Name = "Гу Ядовитого дракона", Seal = "蛟", Rank = 4, Path = GuPath.Poison, Kind = GuKind.Active, Colour = 0x90E040, Price = 1900, Food = "snake_gall", Appetite = 2,
                Description = "Дракон из ядовитого тумана проходит сквозь врагов по прямой. Яд держится восемь секунд.",
                Skill = new SkillDef { Shape = SkillShape.Projectile, Power = 120, Range = 28, Radius = 2.2f, Speed = 22, Pierce = true, Cost = 70, Cooldown = 6, Status = StatusKind.Poison, StatusPower = 30, StatusTime = 8 }
            },
            new GuDef
            {
                Id = "thunder_roar", Name = "Гу Громового рыка", Seal = "吼", Rank = 4, Path = GuPath.Sound, Kind = GuKind.Active, Colour = 0xF0D890, Price = 1800, Food = "echo_shell", Appetite = 3,
                Description = "Рык, от которого дрожит камень: всё перед вами оглушено на две секунды.",
                Skill = new SkillDef { Shape = SkillShape.Cone, Power = 110, Range = 14, Radius = 70, Cost = 70, Cooldown = 12, Status = StatusKind.Stun, StatusTime = 2 }
            },
            new GuDef
            {
                Id = "radiant_lotus", Name = "Гу Сияющего лотоса", Seal = "輝", Rank = 4, Path = GuPath.Light, Kind = GuKind.Passive, Colour = 0xFFF4C0, Price = 2200, Food = "moon_orchid", Appetite = 4,
                Description = "Лотос света в апертуре: все Гу сильнее на десятую часть, перезарядка короче.",
                Passive = new[] { new StatBonus(StatKind.Power, 0.1f), new StatBonus(StatKind.Cooldown, -0.08f) }
            },
            new GuDef
            {
                Id = "thousand_shadows", Name = "Гу Тысячи теней", Seal = "影", Rank = 5, Path = GuPath.Dark, Kind = GuKind.Active, Colour = 0x7060B0, Price = 6200, Food = "night_moth", Appetite = 2,
                Description = "Вы распадаетесь на тысячу теней: двадцать секунд вас не видят, а первый удар из тени страшен.",
                Skill = new SkillDef { Shape = SkillShape.Buff, Buff = BuffKind.Stealth, BuffValue = 1, Duration = 20, Cost = 120, Cooldown = 40 }
            },
            new GuDef
            {
                Id = "pure_wine", Name = "Гу Чистого вина", Seal = "醇", Rank = 4, Path = GuPath.Wine, Kind = GuKind.Passive, Colour = 0xF0E0A0, Price = 2000, Food = "wine_jar", Appetite = 5,
                Description = "Вино, что очищает эссенцию: её втрое больше прибывает, Гу берут на десятую часть меньше.",
                Passive = new[] { new StatBonus(StatKind.EssenceRegen, 1.2f), new StatBonus(StatKind.Cost, -0.1f) }
            },
            new GuDef
            {
                Id = "jade_bone", Name = "Гу Нефритовой кости", Seal = "髓", Rank = 5, Path = GuPath.Bone, Kind = GuKind.Active, Colour = 0xE8F0E0, Price = 6000, Food = "bone_shard", Appetite = 3,
                Description = "Из земли вокруг вырастают нефритовые кости — копьями во все стороны.",
                Skill = new SkillDef { Shape = SkillShape.Nova, Power = 230, Radius = 11, Cost = 150, Cooldown = 14, Status = StatusKind.Bleed, StatusPower = 30, StatusTime = 5 }
            },
            new GuDef
            {
                Id = "blood_lotus", Name = "Гу Кровавого лотоса", Seal = "蓮", Rank = 4, Path = GuPath.Blood, Kind = GuKind.Active, Colour = 0xE04050, Price = 2300, Food = "blood_vine", Appetite = 2,
                Description = "Лотос из крови раскрывается вокруг: режет всё рядом и возвращает треть урона здоровьем.",
                Skill = new SkillDef { Shape = SkillShape.Nova, Power = 120, Radius = 8, Cost = 75, Cooldown = 10, Leech = 0.33f }
            },
            new GuDef
            {
                Id = "mountain", Name = "Гу Горы", Seal = "嶽", Rank = 5, Path = GuPath.Earth, Kind = GuKind.Active, Colour = 0xC0A880, Price = 6400, Food = "stone_dust", Appetite = 4,
                Description = "Гора падает на место прицела. Всё под ней — оглушено, если выжило.",
                Skill = new SkillDef { Shape = SkillShape.Storm, Power = 260, Range = 22, Radius = 8, Duration = 2, Count = 4, Cost = 160, Cooldown = 16, Status = StatusKind.Stun, StatusTime = 2.5f }
            },
            new GuDef
            {
                Id = "sun_flame", Name = "Гу Солнечного пламени", Seal = "曜", Rank = 5, Path = GuPath.Fire, Kind = GuKind.Active, Colour = 0xFFB040, Price = 6600, Food = "ember_coal", Appetite = 1,
                Description = "Маленькое солнце летит в цель и взрывается — всё в восьми шагах горит.",
                Skill = new SkillDef { Shape = SkillShape.Projectile, Power = 300, Range = 30, Radius = 8, Speed = 18, Cost = 160, Cooldown = 9, Status = StatusKind.Burn, StatusPower = 40, StatusTime = 6 }
            },
            new GuDef
            {
                Id = "water_dragon", Name = "Гу Водяного дракона", Seal = "龍", Rank = 4, Path = GuPath.Water, Kind = GuKind.Active, Colour = 0x60B8F0, Price = 1900, Food = "dew_gourd", Appetite = 3,
                Description = "Водяной дракон бросается к цели, сшибая всех на пути, и тянет их за собой.",
                Skill = new SkillDef { Shape = SkillShape.Dash, Power = 110, Range = 18, Radius = 3, Cost = 65, Cooldown = 8, Status = StatusKind.Slow, StatusPower = 0.5f, StatusTime = 3 }
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
            new RecipeDef { Id = "spring_life", Result = "spring_life", Gu = new[] { "life_grass", "honey_dew" }, Items = new[] { ("ginseng", 10), ("bear_king_heart", 1) }, Stones = 900 },
            new RecipeDef { Id = "tree_of_life", Result = "tree_of_life", Gu = new[] { "spring_life", "deep_breath" }, Items = new[] { ("heart_of_qingmao", 1), ("ginseng", 20) }, Stones = 3000 },
            new RecipeDef { Id = "sky_wind", Result = "sky_wind", Gu = new[] { "ten_thousand_li", "whirl" }, Items = new[] { ("heart_of_qingmao", 1), ("eagle_feather", 20) }, Stones = 3000 },
            new RecipeDef { Id = "venom_dragon", Result = "venom_dragon", Gu = new[] { "ten_thousand_needles", "toxic_mist" }, Items = new[] { ("python_core", 1), ("toad_venom", 10) }, Stones = 950 },
            new RecipeDef { Id = "thunder_roar", Result = "thunder_roar", Gu = new[] { "heaven_echo", "bear_roar" }, Items = new[] { ("crown_core", 1), ("echo_shell", 10) }, Stones = 950 },
            new RecipeDef { Id = "radiant_lotus", Result = "radiant_lotus", Gu = new[] { "gold_silkworm", "ice_lotus" }, Items = new[] { ("ancient_relic", 1), ("moon_orchid", 15) }, Stones = 1100 },
            new RecipeDef { Id = "thousand_shadows", Result = "thousand_shadows", Gu = new[] { "immortal_shadow", "stealth_scales" }, Items = new[] { ("heart_of_qingmao", 1), ("night_moth", 20) }, Stones = 3200 },
            new RecipeDef { Id = "pure_wine", Result = "pure_wine", Gu = new[] { "four_wines", "liquor_worm" }, Items = new[] { ("wine_jar", 10), ("king_core", 1) }, Stones = 1000 },
            new RecipeDef { Id = "jade_bone", Result = "jade_bone", Gu = new[] { "ancient_bone", "bone_spear" }, Items = new[] { ("heart_of_qingmao", 1), ("jade_shard", 20) }, Stones = 3000 },
            new RecipeDef { Id = "blood_lotus", Result = "blood_lotus", Gu = new[] { "blood_moon", "blood_skull" }, Items = new[] { ("blood_crystal", 6), ("blood_vine", 10) }, Stones = 1100 },
            new RecipeDef { Id = "mountain", Result = "mountain", Gu = new[] { "golden_bell", "earth_spikes" }, Items = new[] { ("heart_of_qingmao", 1), ("iron_ore", 30) }, Stones = 3200 },
            new RecipeDef { Id = "sun_flame", Result = "sun_flame", Gu = new[] { "crimson_flame", "blazing_feather" }, Items = new[] { ("heart_of_qingmao", 1), ("ember_coal", 40) }, Stones = 3400 },
            new RecipeDef { Id = "water_dragon", Result = "water_dragon", Gu = new[] { "river_serpent", "white_cloud" }, Items = new[] { ("python_core", 1), ("dew_gourd", 20) }, Stones = 900 },
        };

        public static readonly ItemDef[] Items =
        {
            // The relic Gu: one use each, a stage of cultivation at once (the canon's green copper, red steel, white silver, yellow gold, purple crystal).
            new ItemDef { Id = "relic_r1", Name = "Реликвия Зелёной меди", Seal = "铜", Kind = ItemKind.Consumable, Relic = 1, Price = 300, Colour = 0x5FB88A, Description = "Гу-реликвия: мастер первого ранга, проглотив её, поднимается на ступень. Через ворота ранга не переносит." },
            new ItemDef { Id = "relic_r2", Name = "Реликвия Красной стали", Seal = "钢", Kind = ItemKind.Consumable, Relic = 2, Price = 800, Colour = 0xD0584A, Description = "Гу-реликвия второго ранга: ступень вверх, но не новый ранг." },
            new ItemDef { Id = "relic_r3", Name = "Реликвия Белого серебра", Seal = "银", Kind = ItemKind.Consumable, Relic = 3, Price = 1800, Colour = 0xE8EEF2, Description = "Гу-реликвия третьего ранга: ступень вверх." },
            new ItemDef { Id = "relic_r4", Name = "Реликвия Жёлтого золота", Seal = "金", Kind = ItemKind.Consumable, Relic = 4, Price = 4000, Colour = 0xF2C850, Description = "Гу-реликвия четвёртого ранга: ступень вверх." },
            new ItemDef { Id = "relic_r5", Name = "Реликвия Пурпурного кристалла", Seal = "晶", Kind = ItemKind.Consumable, Relic = 5, Price = 9000, Colour = 0xB07AE8, Description = "Гу-реликвия пятого ранга: ступень вверх. Говорят, их видели только в сокровищницах бессмертных." },
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
            new ItemDef { Id = "scroll_spring_life", Name = "Свиток: Гу Весенней жизни", Seal = "卷", Kind = ItemKind.Scroll, Teaches = "spring_life", Price = 1200, Description = "Рецепт четвёртого ранга." },
            new ItemDef { Id = "scroll_tree_of_life", Name = "Свиток: Гу Древа жизни", Seal = "卷", Kind = ItemKind.Scroll, Teaches = "tree_of_life", Price = 4200, Description = "Рецепт пятого ранга." },
            new ItemDef { Id = "scroll_sky_wind", Name = "Свиток: Гу Небесного ветра", Seal = "卷", Kind = ItemKind.Scroll, Teaches = "sky_wind", Price = 4000, Description = "Рецепт пятого ранга." },
            new ItemDef { Id = "scroll_venom_dragon", Name = "Свиток: Гу Ядовитого дракона", Seal = "卷", Kind = ItemKind.Scroll, Teaches = "venom_dragon", Price = 1250, Description = "Рецепт четвёртого ранга." },
            new ItemDef { Id = "scroll_thunder_roar", Name = "Свиток: Гу Громового рыка", Seal = "卷", Kind = ItemKind.Scroll, Teaches = "thunder_roar", Price = 1200, Description = "Рецепт четвёртого ранга." },
            new ItemDef { Id = "scroll_radiant_lotus", Name = "Свиток: Гу Сияющего лотоса", Seal = "卷", Kind = ItemKind.Scroll, Teaches = "radiant_lotus", Price = 1400, Description = "Рецепт четвёртого ранга." },
            new ItemDef { Id = "scroll_thousand_shadows", Name = "Свиток: Гу Тысячи теней", Seal = "卷", Kind = ItemKind.Scroll, Teaches = "thousand_shadows", Price = 4300, Description = "Рецепт пятого ранга." },
            new ItemDef { Id = "scroll_pure_wine", Name = "Свиток: Гу Чистого вина", Seal = "卷", Kind = ItemKind.Scroll, Teaches = "pure_wine", Price = 1300, Description = "Рецепт четвёртого ранга." },
            new ItemDef { Id = "scroll_jade_bone", Name = "Свиток: Гу Нефритовой кости", Seal = "卷", Kind = ItemKind.Scroll, Teaches = "jade_bone", Price = 4000, Description = "Рецепт пятого ранга." },
            new ItemDef { Id = "scroll_blood_lotus", Name = "Свиток: Гу Кровавого лотоса", Seal = "卷", Kind = ItemKind.Scroll, Teaches = "blood_lotus", Price = 1400, Description = "Рецепт четвёртого ранга." },
            new ItemDef { Id = "scroll_mountain", Name = "Свиток: Гу Горы", Seal = "卷", Kind = ItemKind.Scroll, Teaches = "mountain", Price = 4300, Description = "Рецепт пятого ранга." },
            new ItemDef { Id = "scroll_sun_flame", Name = "Свиток: Гу Солнечного пламени", Seal = "卷", Kind = ItemKind.Scroll, Teaches = "sun_flame", Price = 4500, Description = "Рецепт пятого ранга." },
            new ItemDef { Id = "scroll_water_dragon", Name = "Свиток: Гу Водяного дракона", Seal = "卷", Kind = ItemKind.Scroll, Teaches = "water_dragon", Price = 1200, Description = "Рецепт четвёртого ранга." },
        };

        /// <summary>Where the high scrolls and the mountain's heart are found (added to the tables of the great beasts and the deepest chests).</summary>
        public static readonly LootTable[] Tables =
        {
            new LootTable { Id = "high_gu_yue", Items = new[] { new LootEntry { Item = "relic_r4", Chance = 0.6f }, new LootEntry { Item = "scroll_moon_sickle" }, new LootEntry { Item = "scroll_full_moon", Chance = 0.5f } } },
            new LootTable { Id = "heart_peak", Items = new[] { new LootEntry { Item = "relic_r4" }, new LootEntry { Item = "heart_of_qingmao" }, new LootEntry { Item = "scroll_heaven_thunder" } }, StonesMin = 400, StonesMax = 600 },
            new LootTable { Id = "heart_tree", Items = new[] { new LootEntry { Item = "relic_r5", Chance = 0.6f }, new LootEntry { Item = "heart_of_qingmao" }, new LootEntry { Item = "scroll_boundless_sea" } }, StonesMin = 400, StonesMax = 600 },
        };
    }
}
