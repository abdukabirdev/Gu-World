namespace GuWorld.Core
{
    /// <summary>
    /// The Gu of the wider mountain: the Bai's ice and water, the Xiong's strength and honey, the
    /// marsh's poisons, the caves' bones and shadows, the fire and sound the wanderers carry. What
    /// they eat, which clan keeps them, and the fusions that make them.
    /// </summary>
    public static class MoreGu
    {
        public static readonly GuDef[] All =
        {
            // ------------------------------------------------------------------ Rank 1

            new GuDef
            {
                Id = "ember", Name = "Гу Уголька", Seal = "火", Rank = 1, Path = GuPath.Fire, Kind = GuKind.Active, Colour = 0xFF9050, Price = 50, Food = "ember_coal", Appetite = 2,
                Description = "Плюёт тлеющим угольком. Цель горит три секунды.",
                Skill = new SkillDef { Shape = SkillShape.Projectile, Power = 16, Cost = 10, Cooldown = 1f, Range = 13, Radius = 0.8f, Speed = 22, Status = StatusKind.Burn, StatusPower = 5, StatusTime = 3 }
            },
            new GuDef
            {
                Id = "water_shield", Name = "Гу Водяного щита", Seal = "水", Rank = 1, Path = GuPath.Water, Kind = GuKind.Active, Colour = 0x6FB8FF, Price = 50, Food = "dew_gourd", Appetite = 3, Clan = "bai",
                Description = "Плёнка воды обнимает тело: восемь секунд удары теряют треть силы. Детский Гу клана Бай.",
                Skill = new SkillDef { Shape = SkillShape.Buff, Buff = BuffKind.Shield, BuffValue = 0.32f, Duration = 8, Cost = 20, Cooldown = 15 }
            },
            new GuDef
            {
                Id = "poison_needle", Name = "Гу Ядовитой иглы", Seal = "针", Rank = 1, Path = GuPath.Poison, Kind = GuKind.Active, Colour = 0x9FE070, Price = 55, Food = "snake_gall", Appetite = 3,
                Description = "Тонкая игла, что не убивает сразу: яд точит цель пять секунд, и броня ему не помеха.",
                Skill = new SkillDef { Shape = SkillShape.Projectile, Power = 8, Cost = 8, Cooldown = 0.9f, Range = 16, Radius = 0.6f, Speed = 36, Status = StatusKind.Poison, StatusPower = 6, StatusTime = 5 }
            },
            new GuDef
            {
                Id = "echo", Name = "Гу Эха", Seal = "音", Rank = 1, Path = GuPath.Sound, Kind = GuKind.Active, Colour = 0xE0D0FF, Price = 50, Food = "echo_shell", Appetite = 3,
                Description = "Крик, что возвращается громче: всё перед вами оглушено на секунду.",
                Skill = new SkillDef { Shape = SkillShape.Cone, Power = 14, Range = 7, Radius = 60, Cost = 14, Cooldown = 6, Status = StatusKind.Stun, StatusTime = 1 }
            },
            new GuDef
            {
                Id = "light_steps", Name = "Гу Лёгких шагов", Seal = "步", Rank = 1, Path = GuPath.Wind, Kind = GuKind.Active, Colour = 0xCFF5E8, Price = 45, Food = "dew_gourd", Appetite = 3,
                Description = "Ноги лёгкие, как пух: десять секунд вы бегаете на треть быстрее.",
                Skill = new SkillDef { Shape = SkillShape.Buff, Buff = BuffKind.Haste, BuffValue = 0.35f, Duration = 10, Cost = 14, Cooldown = 16 }
            },
            new GuDef
            {
                Id = "black_boar", Name = "Гу Чёрного вепря", Seal = "猪", Rank = 1, Path = GuPath.Strength, Kind = GuKind.Passive, Colour = 0x8A7A6A, Price = 120, Food = "jerky", Appetite = 3,
                Description = "Сила одного вепря навсегда входит в тело: здоровье +30. Кому-то этого мало.",
                Passive = new[] { new StatBonus(StatKind.Health, 30) }
            },
            new GuDef
            {
                Id = "deep_breath", Name = "Гу Глубокого дыхания", Seal = "息", Rank = 1, Path = GuPath.Wood, Kind = GuKind.Passive, Colour = 0xB0F0C8, Price = 90, Food = "dew_gourd", Appetite = 4,
                Description = "Апертура дышит в такт с землёй: эссенция восстанавливается на четверть быстрее.",
                Passive = new[] { new StatBonus(StatKind.EssenceRegen, 0.25f) }
            },
            new GuDef
            {
                Id = "clinging_vine", Name = "Гу Цепкой лозы", Seal = "藤", Rank = 1, Path = GuPath.Wood, Kind = GuKind.Active, Colour = 0x7FD08A, Price = 55, Food = "dew_gourd", Appetite = 3,
                Description = "Лоза из земли хватает за ноги всех в точке прицела: три секунды им не уйти.",
                Skill = new SkillDef { Shape = SkillShape.Pull, Power = 10, Range = 12, Radius = 4.5f, Duration = 1, Cost = 16, Cooldown = 10, Status = StatusKind.Slow, StatusPower = 0.7f, StatusTime = 3 }
            },
            new GuDef
            {
                Id = "bone_needle", Name = "Гу Костяной иглы", Seal = "骨", Rank = 1, Path = GuPath.Bone, Kind = GuKind.Active, Colour = 0xF0E8D8, Price = 55, Food = "jerky", Appetite = 3,
                Description = "Игла из собственной кости хозяина. Пробивает насквозь, рана кровит.",
                Skill = new SkillDef { Shape = SkillShape.Projectile, Power = 18, Cost = 11, Cooldown = 1.1f, Range = 15, Radius = 0.6f, Speed = 34, Pierce = true, Status = StatusKind.Bleed, StatusPower = 4, StatusTime = 4 }
            },
            new GuDef
            {
                Id = "night_eye", Name = "Гу Ночного глаза", Seal = "眼", Rank = 1, Path = GuPath.Dark, Kind = GuKind.Passive, Colour = 0x8A7AC0, Price = 80, Food = "night_moth", Appetite = 4,
                Description = "Видит в темноте и дальше: дальность всех Гу +15%.",
                Passive = new[] { new StatBonus(StatKind.Range, 0.15f) }
            },
            new GuDef
            {
                Id = "frost_breath", Name = "Гу Морозного дыхания", Seal = "寒", Rank = 1, Path = GuPath.Ice, Kind = GuKind.Active, Colour = 0xBFEFFF, Price = 55, Food = "frost_lotus", Appetite = 3, Clan = "bai",
                Description = "Выдох стужи: всё перед вами мёрзнет и двигается медленнее; следующий лёд их скуёт.",
                Skill = new SkillDef { Shape = SkillShape.Cone, Power = 16, Range = 6, Radius = 55, Cost = 13, Cooldown = 4, Status = StatusKind.Chill, StatusPower = 0.3f, StatusTime = 4 }
            },
            new GuDef
            {
                Id = "honey_dew", Name = "Гу Медовой росы", Seal = "蜜", Rank = 1, Path = GuPath.Wood, Kind = GuKind.Active, Colour = 0xFFD070, Price = 60, Food = "honey", Appetite = 3, Clan = "xiong",
                Description = "Капля мёда под язык: двенадцать секунд раны затягиваются сами. Его держат пасечники Сюн.",
                Skill = new SkillDef { Shape = SkillShape.Buff, Buff = BuffKind.Regen, BuffValue = 4, Duration = 12, Cost = 18, Cooldown = 20 }
            },

            // ------------------------------------------------------------------ Rank 2

            new GuDef
            {
                Id = "flame_burst", Name = "Гу Огненного взрыва", Seal = "炎", Rank = 2, Path = GuPath.Fire, Kind = GuKind.Active, Colour = 0xFF7040, Price = 160, Food = "ember_coal", Appetite = 2,
                Description = "Тело вспыхивает: всё вокруг обожжено и горит ещё четыре секунды.",
                Skill = new SkillDef { Shape = SkillShape.Nova, Power = 44, Radius = 5.5f, Cost = 28, Cooldown = 8, Status = StatusKind.Burn, StatusPower = 9, StatusTime = 4 }
            },
            new GuDef
            {
                Id = "fire_robe", Name = "Гу Огненной одежды", Seal = "衣", Rank = 2, Path = GuPath.Fire, Kind = GuKind.Active, Colour = 0xFFA060, Price = 180, Food = "ember_coal", Appetite = 2,
                Description = "Одежда из пламени: десять секунд всё, что подходит ближе трёх шагов, горит.",
                Skill = new SkillDef { Shape = SkillShape.Buff, Buff = BuffKind.Aura, BuffValue = 12, Radius = 3.2f, Duration = 10, Cost = 30, Cooldown = 20, Status = StatusKind.Burn, StatusPower = 6, StatusTime = 2 }
            },
            new GuDef
            {
                Id = "toxic_mist", Name = "Гу Ядовитого тумана", Seal = "毒", Rank = 2, Path = GuPath.Poison, Kind = GuKind.Active, Colour = 0x8FD060, Price = 170, Food = "snake_gall", Appetite = 3,
                Description = "Зелёный туман в точке прицела. Пять секунд всё внутри дышит ядом.",
                Skill = new SkillDef { Shape = SkillShape.Storm, Power = 10, Range = 15, Radius = 6, Duration = 5, Count = 10, Cost = 30, Cooldown = 15, Status = StatusKind.Poison, StatusPower = 10, StatusTime = 4 }
            },
            new GuDef
            {
                Id = "stealth_scales", Name = "Гу Скрытой чешуи", Seal = "隐", Rank = 2, Path = GuPath.Dark, Kind = GuKind.Active, Colour = 0x9A8AD0, Price = 220, Food = "night_moth", Appetite = 3,
                Description = "Чешуя, что пьёт свет: восемь секунд вас не видят. Первый удар из тени — втрое сильнее.",
                Skill = new SkillDef { Shape = SkillShape.Buff, Buff = BuffKind.Stealth, BuffValue = 2, Duration = 8, Cost = 26, Cooldown = 24 }
            },
            new GuDef
            {
                Id = "bear_roar", Name = "Гу Медвежьего рыка", Seal = "吼", Rank = 2, Path = GuPath.Strength, Kind = GuKind.Active, Colour = 0xE09060, Price = 170, Food = "jerky", Appetite = 3, Clan = "xiong",
                Description = "Рык бурого медведя: всё перед вами оглушено на две секунды. Боевой клич Сюн.",
                Skill = new SkillDef { Shape = SkillShape.Cone, Power = 30, Range = 8, Radius = 70, Cost = 24, Cooldown = 10, Status = StatusKind.Stun, StatusTime = 2 }
            },
            new GuDef
            {
                Id = "white_boar", Name = "Гу Белого вепря", Seal = "豕", Rank = 2, Path = GuPath.Strength, Kind = GuKind.Passive, Colour = 0xE8E0D8, Price = 260, Food = "jerky", Appetite = 4, Clan = "xiong",
                Description = "Сила белого вепря: здоровье +50, удары ладонью сильнее.",
                Passive = new[] { new StatBonus(StatKind.Health, 50), new StatBonus(StatKind.Power, 0.05f) }
            },
            new GuDef
            {
                Id = "river_serpent", Name = "Гу Речного змея", Seal = "蛇", Rank = 2, Path = GuPath.Water, Kind = GuKind.Active, Colour = 0x60B0E0, Price = 190, Food = "dew_gourd", Appetite = 3, Clan = "bai",
                Description = "Водяная змея перескакивает с врага на врага, остужая каждого.",
                Skill = new SkillDef { Shape = SkillShape.Chain, Power = 30, Range = 14, Radius = 7, Count = 4, Cost = 22, Cooldown = 5, Status = StatusKind.Chill, StatusPower = 0.25f, StatusTime = 3 }
            },
            new GuDef
            {
                Id = "white_cloud", Name = "Гу Белого облака", Seal = "云", Rank = 2, Path = GuPath.Water, Kind = GuKind.Active, Colour = 0xEAF6FF, Price = 200, Food = "dew_gourd", Appetite = 3, Clan = "bai",
                Description = "Тело становится облаком и переносится на десять шагов. Там, где оно оседает, — холодная морось.",
                Skill = new SkillDef { Shape = SkillShape.Blink, Power = 20, Range = 10, Radius = 3.5f, Cost = 18, Cooldown = 7, Status = StatusKind.Chill, StatusPower = 0.3f, StatusTime = 3 }
            },
            new GuDef
            {
                Id = "moon_shadow", Name = "Гу Лунной тени", Seal = "影", Rank = 2, Path = GuPath.Moon, Kind = GuKind.Active, Colour = 0x8FA8E8, Price = 210, Food = "moon_orchid", Appetite = 3, Clan = "gu_yue",
                Description = "Вы шагаете в тень и выходите из неё за спиной цели. Первый удар после — с лунным светом.",
                Skill = new SkillDef { Shape = SkillShape.Blink, Power = 38, Range = 11, Radius = 2.5f, Cost = 20, Cooldown = 6 }
            },
            new GuDef
            {
                Id = "earth_spikes", Name = "Гу Каменных шипов", Seal = "棘", Rank = 2, Path = GuPath.Earth, Kind = GuKind.Active, Colour = 0xC8A878, Price = 180, Food = "stone_dust", Appetite = 4,
                Description = "Три секунды из земли в точке прицела бьют шипы. Кого задело — тот падает.",
                Skill = new SkillDef { Shape = SkillShape.Storm, Power = 30, Range = 15, Radius = 5, Duration = 3, Count = 6, Cost = 30, Cooldown = 13, Status = StatusKind.Stun, StatusTime = 0.8f }
            },
            new GuDef
            {
                Id = "bone_spear", Name = "Гу Костяного копья", Seal = "矛", Rank = 2, Path = GuPath.Bone, Kind = GuKind.Active, Colour = 0xF4ECDC, Price = 190, Food = "jerky", Appetite = 3,
                Description = "Копьё из кости летит сквозь строй; раны долго кровоточат.",
                Skill = new SkillDef { Shape = SkillShape.Projectile, Power = 48, Cost = 18, Cooldown = 1.6f, Range = 20, Radius = 1f, Speed = 30, Pierce = true, Status = StatusKind.Bleed, StatusPower = 8, StatusTime = 5 }
            },
            new GuDef
            {
                Id = "steel_bone", Name = "Гу Стальной кости", Seal = "钢", Rank = 2, Path = GuPath.Bone, Kind = GuKind.Passive, Colour = 0xC8D0D8, Price = 240, Food = "jerky", Appetite = 5,
                Description = "Кости как сталь: урон меньше на восемь процентов, здоровье +40.",
                Passive = new[] { new StatBonus(StatKind.DamageTaken, -0.08f), new StatBonus(StatKind.Health, 40) }
            },
            new GuDef
            {
                Id = "blood_skull", Name = "Гу Кровавого черепа", Seal = "颅", Rank = 2, Path = GuPath.Blood, Kind = GuKind.Passive, Colour = 0xD04A3A, Price = 300, Food = "blood_crystal", Appetite = 5,
                Description = "Демонический Гу: десятая часть нанесённого урона возвращается здоровьем. Праведные за такой убивают.",
                Passive = new[] { new StatBonus(StatKind.Leech, 0.1f) }
            },
            new GuDef
            {
                Id = "gold_silkworm", Name = "Золотой шелкопряд", Seal = "蚕", Rank = 2, Path = GuPath.Light, Kind = GuKind.Passive, Colour = 0xFFE8A0, Price = 280, Food = "moon_orchid", Appetite = 4,
                Description = "Прядёт эссенцию в нить: все Гу берут на десятую часть меньше и перезаряжаются быстрее.",
                Passive = new[] { new StatBonus(StatKind.Cost, -0.1f), new StatBonus(StatKind.Cooldown, -0.08f) }
            },
            new GuDef
            {
                Id = "thunder_wing", Name = "Гу Громового крыла", Seal = "翼", Rank = 2, Path = GuPath.Lightning, Kind = GuKind.Active, Colour = 0xB8A8FF, Price = 200, Food = "spark_moss", Appetite = 3,
                Description = "Веер из пяти разрядов перед собой.",
                Skill = new SkillDef { Shape = SkillShape.Fan, Power = 26, Count = 5, Cost = 22, Cooldown = 2.4f, Range = 14, Radius = 0.9f, Speed = 34 }
            },

            // ------------------------------------------------------------------ Rank 3

            new GuDef
            {
                Id = "blazing_feather", Name = "Гу Пылающего пера", Seal = "羽", Rank = 3, Path = GuPath.Fire, Kind = GuKind.Active, Colour = 0xFF8040, Price = 650, Food = "ember_coal", Appetite = 2,
                Description = "Пять огненных перьев веером. Всё, чего они коснутся, горит.",
                Skill = new SkillDef { Shape = SkillShape.Fan, Power = 52, Count = 5, Cost = 30, Cooldown = 2f, Range = 18, Radius = 1.1f, Speed = 28, Pierce = true, Status = StatusKind.Burn, StatusPower = 12, StatusTime = 3 }
            },
            new GuDef
            {
                Id = "ten_thousand_needles", Name = "Гу Десяти тысяч игл", Seal = "万", Rank = 3, Path = GuPath.Poison, Kind = GuKind.Active, Colour = 0xA0E060, Price = 620, Food = "snake_gall", Appetite = 2,
                Description = "Ливень ядовитых игл на шесть шагов вокруг. Мастера Пути Яда платят за него жизнью учеников.",
                Skill = new SkillDef { Shape = SkillShape.Nova, Power = 46, Radius = 6.5f, Cost = 36, Cooldown = 12, Status = StatusKind.Poison, StatusPower = 18, StatusTime = 5 }
            },
            new GuDef
            {
                Id = "great_bear", Name = "Гу Великого медведя", Seal = "罴", Rank = 3, Path = GuPath.Strength, Kind = GuKind.Active, Colour = 0xD08050, Price = 720, Food = "honey", Appetite = 2, Clan = "xiong",
                Description = "Тень великого медведя встаёт за спиной: двенадцать секунд ладонь бьёт вдвое сильнее, и с ног не сбить.",
                Skill = new SkillDef { Shape = SkillShape.Buff, Buff = BuffKind.MeleeUp, BuffValue = 1.2f, Duration = 12, Cost = 40, Cooldown = 26 }
            },
            new GuDef
            {
                Id = "ice_lotus", Name = "Гу Ледяного лотоса", Seal = "莲", Rank = 3, Path = GuPath.Ice, Kind = GuKind.Active, Colour = 0xD8F8FF, Price = 700, Food = "frost_lotus", Appetite = 2, Clan = "bai",
                Description = "Лотос из льда распускается вокруг вас: всё рядом замерзает на две с половиной секунды. Сокровище Бай.",
                Skill = new SkillDef { Shape = SkillShape.Nova, Power = 58, Radius = 6, Cost = 38, Cooldown = 14, Status = StatusKind.Freeze, StatusTime = 2.5f }
            },
            new GuDef
            {
                Id = "heaven_echo", Name = "Гу Небесного отзвука", Seal = "响", Rank = 3, Path = GuPath.Sound, Kind = GuKind.Active, Colour = 0xE8DCFF, Price = 600, Food = "echo_shell", Appetite = 3,
                Description = "Удар гонга, слышный на весь хребет: всё в двенадцати шагах оглушено.",
                Skill = new SkillDef { Shape = SkillShape.Nova, Power = 40, Radius = 12, Cost = 40, Cooldown = 18, Status = StatusKind.Stun, StatusTime = 2.2f }
            },
            new GuDef
            {
                Id = "ancient_bone", Name = "Гу Древней кости", Seal = "古", Rank = 3, Path = GuPath.Bone, Kind = GuKind.Passive, Colour = 0xF8F0D8, Price = 800, Food = "jerky", Appetite = 6,
                Description = "Кость мастера, умершего тысячу лет назад. Здоровье +90, урон меньше на десятую часть.",
                Passive = new[] { new StatBonus(StatKind.Health, 90), new StatBonus(StatKind.DamageTaken, -0.1f) }
            },
            new GuDef
            {
                Id = "moon_lake", Name = "Гу Лунного озера", Seal = "湖", Rank = 3, Path = GuPath.Moon, Kind = GuKind.Passive, Colour = 0xB0D0FF, Price = 750, Food = "moon_orchid", Appetite = 3, Clan = "gu_yue",
                Description = "Апертура как озеро под луной: эссенция вдвое быстрее, Гу Луны сильнее на пятую часть.",
                Passive = new[] { new StatBonus(StatKind.EssenceRegen, 1f), new StatBonus(StatKind.MoonPower, 0.2f) }
            },
            new GuDef
            {
                Id = "dark_tide", Name = "Гу Тёмного прилива", Seal = "潮", Rank = 3, Path = GuPath.Dark, Kind = GuKind.Active, Colour = 0x7060B0, Price = 680, Food = "night_moth", Appetite = 2,
                Description = "Тьма стягивает всех в точке прицела и выпивает из них жизнь — четверть урона ваша.",
                Skill = new SkillDef { Shape = SkillShape.Pull, Power = 54, Range = 16, Radius = 8, Duration = 2.5f, Cost = 36, Cooldown = 14, Status = StatusKind.Slow, StatusPower = 0.6f, StatusTime = 3, Leech = 0.25f }
            },
        };

        /// <summary>The fusions of the new Gu: the scrolls that teach them are in MoreItems.</summary>
        public static readonly RecipeDef[] Recipes =
        {
            new RecipeDef { Id = "flame_burst", Result = "flame_burst", Gu = new[] { "ember", "stone_beetle" }, Items = new[] { ("ember_coal", 4) }, Stones = 50 },
            new RecipeDef { Id = "fire_robe", Result = "fire_robe", Gu = new[] { "ember", "jade_skin" }, Items = new[] { ("ember_coal", 5), ("fox_pelt", 2) }, Stones = 60 },
            new RecipeDef { Id = "toxic_mist", Result = "toxic_mist", Gu = new[] { "poison_needle", "tailwind" }, Items = new[] { ("snake_gall", 3), ("marsh_reed", 3) }, Stones = 60 },
            new RecipeDef { Id = "stealth_scales", Result = "stealth_scales", Gu = new[] { "night_eye", "light_steps" }, Items = new[] { ("snake_skin", 3), ("bat_wing", 2) }, Stones = 80 },
            new RecipeDef { Id = "bear_roar", Result = "bear_roar", Gu = new[] { "echo", "bear_strength" }, Items = new[] { ("bear_claw", 2) }, Stones = 60 },
            new RecipeDef { Id = "white_boar", Result = "white_boar", Gu = new[] { "black_boar", "black_boar" }, Items = new[] { ("jerky", 6) }, Stones = 90 },
            new RecipeDef { Id = "river_serpent", Result = "river_serpent", Gu = new[] { "water_shield", "spark" }, Items = new[] { ("snake_skin", 2), ("dew_gourd", 3) }, Stones = 70 },
            new RecipeDef { Id = "white_cloud", Result = "white_cloud", Gu = new[] { "water_shield", "tailwind" }, Items = new[] { ("cloud_tea", 2) }, Stones = 70 },
            new RecipeDef { Id = "moon_shadow", Result = "moon_shadow", Gu = new[] { "moonlight", "night_eye" }, Items = new[] { ("moon_orchid", 4), ("night_moth", 2) }, Stones = 80 },
            new RecipeDef { Id = "earth_spikes", Result = "earth_spikes", Gu = new[] { "stone_beetle", "bone_needle" }, Items = new[] { ("iron_ore", 3) }, Stones = 60 },
            new RecipeDef { Id = "bone_spear", Result = "bone_spear", Gu = new[] { "bone_needle", "bone_needle" }, Items = new[] { ("bone_shard", 5) }, Stones = 70 },
            new RecipeDef { Id = "steel_bone", Result = "steel_bone", Gu = new[] { "bone_needle", "jade_skin" }, Items = new[] { ("iron_ore", 4), ("bone_shard", 3) }, Stones = 90 },
            new RecipeDef { Id = "blood_skull", Result = "blood_skull", Gu = new[] { "bone_needle" }, Items = new[] { ("blood_crystal", 2), ("bone_shard", 4) }, Stones = 120 },
            new RecipeDef { Id = "gold_silkworm", Result = "gold_silkworm", Gu = new[] { "little_light", "deep_breath" }, Items = new[] { ("spider_silk", 4), ("ancient_coin", 1) }, Stones = 120 },
            new RecipeDef { Id = "thunder_wing", Result = "thunder_wing", Gu = new[] { "spark", "light_steps" }, Items = new[] { ("eagle_feather", 3), ("storm_core", 1) }, Stones = 80 },
            new RecipeDef { Id = "blazing_feather", Result = "blazing_feather", Gu = new[] { "flame_burst", "thunder_wing" }, Items = new[] { ("eagle_feather", 5), ("ember_coal", 6) }, Stones = 260 },
            new RecipeDef { Id = "ten_thousand_needles", Result = "ten_thousand_needles", Gu = new[] { "toxic_mist", "poison_needle" }, Items = new[] { ("python_core", 1), ("snake_gall", 5) }, Stones = 240 },
            new RecipeDef { Id = "great_bear", Result = "great_bear", Gu = new[] { "bear_strength", "white_boar" }, Items = new[] { ("bear_king_heart", 1), ("honey", 4) }, Stones = 280 },
            new RecipeDef { Id = "ice_lotus", Result = "ice_lotus", Gu = new[] { "frost_breath", "ice_spike" }, Items = new[] { ("frost_lotus", 6), ("king_core", 1) }, Stones = 260 },
            new RecipeDef { Id = "heaven_echo", Result = "heaven_echo", Gu = new[] { "echo", "bear_roar" }, Items = new[] { ("echo_shell", 4), ("spider_queen_eye", 1) }, Stones = 240 },
            new RecipeDef { Id = "ancient_bone", Result = "ancient_bone", Gu = new[] { "steel_bone", "bone_spear" }, Items = new[] { ("ancient_relic", 1), ("bone_shard", 6) }, Stones = 300 },
            new RecipeDef { Id = "moon_lake", Result = "moon_lake", Gu = new[] { "deep_breath", "moonglow" }, Items = new[] { ("moon_orchid", 8), ("frost_lotus", 2) }, Stones = 280 },
            new RecipeDef { Id = "dark_tide", Result = "dark_tide", Gu = new[] { "stealth_scales", "whirl" }, Items = new[] { ("night_moth", 5), ("centipede_shell", 3) }, Stones = 260 },
        };
    }
}
