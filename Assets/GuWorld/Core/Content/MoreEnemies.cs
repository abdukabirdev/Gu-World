namespace GuWorld.Core
{
    /// <summary>
    /// The beasts and Gu masters of the wider mountain — snakes and toads of the marsh, bears of the
    /// front mountain, spiders and bats of the caves, eagles of the cliffs, the tomb's guardians,
    /// the Black Tail's masters and the clans' patrols — and what each leaves behind.
    /// </summary>
    public static class MoreEnemies
    {
        public static readonly EnemyDef[] All =
        {
            // ------------------------------------------------------------------ The marsh

            new EnemyDef
            {
                Id = "marsh_snake", Name = "Болотная гадюка", Body = EnemyBody.Serpent, Mind = EnemyMind.Skirmisher, Look = "marsh", Scale = 1f, Level = 6, Health = 240, Damage = 10, Speed = 5.2f,
                AttackRange = 2f, AttackPeriod = 2.2f, Sight = 16, Insight = 22, Loot = "marsh_snake", Poise = 10, Colour = 0x9FC070,
                Inflicts = StatusKind.Poison, InflictChance = 0.5f, InflictPower = 4, InflictTime = 5
            },
            new EnemyDef
            {
                Id = "marsh_toad", Name = "Ядовитая жаба", Body = EnemyBody.Bug, Mind = EnemyMind.Shooter, Look = "toad", Scale = 1.3f, Level = 6, Health = 210, Damage = 9, Speed = 3.6f,
                AttackRange = 12, AttackPeriod = 3.4f, Sight = 18, Insight = 20, Loot = "marsh_toad", Poise = 8, Colour = 0xA0C060,
                Inflicts = StatusKind.Poison, InflictChance = 0.8f, InflictPower = 5, InflictTime = 4
            },
            new EnemyDef
            {
                Id = "swamp_python", Name = "Болотный удав", Title = "Хозяин Змеиного болота", Body = EnemyBody.Serpent, Mind = EnemyMind.Boss, Look = "python", Scale = 2.6f, Level = 10, Health = 6200, Damage = 26, Speed = 5.6f,
                AttackRange = 3.6f, Sight = 36, Insight = 420, Boss = true, Loot = "swamp_python", Poise = 120, Colour = 0x8FD060,
                Moves = new[] { BossMove.Lunge, BossMove.Rings, BossMove.Charge, BossMove.Lunge, BossMove.Summon }, Summons = "marsh_snake",
                Inflicts = StatusKind.Poison, InflictChance = 1, InflictPower = 9, InflictTime = 6
            },
            new EnemyDef
            {
                Id = "poison_master", Name = "Мастер Пути Яда", Body = EnemyBody.Human, Mind = EnemyMind.Caster, Look = "rogue", Level = 9, Health = 520, Damage = 16, Speed = 4.8f,
                AttackRange = 14, AttackPeriod = 2.8f, Sight = 26, Insight = 60, Loot = "rogue_master", Poise = 20, Colour = 0x9FE070, Clan = "wanderers",
                Gu = new[] { "poison_needle", "toxic_mist", "poison_needle" }
            },

            // ------------------------------------------------------------------ The front mountain

            new EnemyDef
            {
                Id = "brown_bear", Name = "Бурый медведь", Body = EnemyBody.Canid, Mind = EnemyMind.Brawler, Look = "bear", Scale = 1.9f, Level = 6, Health = 520, Damage = 18, Speed = 5f,
                AttackRange = 2.6f, AttackPeriod = 2.8f, Sight = 18, Insight = 30, Loot = "brown_bear", Poise = 40, Armour = 0.05f, Colour = 0xA07850,
                Inflicts = StatusKind.Bleed, InflictChance = 0.3f, InflictPower = 4, InflictTime = 4
            },
            new EnemyDef
            {
                Id = "bear_king", Name = "Медвежий король", Title = "Владыка Медвежьего лога", Body = EnemyBody.Canid, Mind = EnemyMind.Boss, Look = "bear", Scale = 2.9f, Level = 9, Health = 5600, Damage = 30, Speed = 5.6f,
                AttackRange = 3.8f, Sight = 34, Insight = 380, Boss = true, Loot = "bear_king", Poise = 150, Armour = 0.1f, Colour = 0xC08850,
                Moves = new[] { BossMove.Slam, BossMove.Lunge, BossMove.Howl, BossMove.Charge, BossMove.Slam }, Summons = "brown_bear",
                Inflicts = StatusKind.Bleed, InflictChance = 0.5f, InflictPower = 7, InflictTime = 5
            },
            new EnemyDef { Id = "wild_boar", Name = "Дикий вепрь", Body = EnemyBody.Canid, Mind = EnemyMind.Brawler, Look = "boar", Scale = 1.3f, Level = 4, Health = 260, Damage = 13, Speed = 5.6f, AttackRange = 2f, AttackPeriod = 2.6f, Sight = 16, Insight = 18, Loot = "wild_boar", Poise = 24, Colour = 0x806050 },
            new EnemyDef
            {
                Id = "bee_swarm", Name = "Рой диких пчёл", Body = EnemyBody.Bug, Mind = EnemyMind.Swarm, Look = "bee", Scale = 0.8f, Level = 5, Health = 120, Damage = 7, Speed = 6.2f,
                AttackPeriod = 1.6f, Sight = 14, Insight = 14, Loot = "bee_swarm", Poise = 2, Colour = 0xFFD070,
                Inflicts = StatusKind.Poison, InflictChance = 0.25f, InflictPower = 3, InflictTime = 3
            },

            // ------------------------------------------------------------------ The caves

            new EnemyDef
            {
                Id = "cave_bat", Name = "Пещерный нетопырь", Body = EnemyBody.Bug, Mind = EnemyMind.Swarm, Look = "bat", Scale = 1.1f, Level = 7, Health = 160, Damage = 11, Speed = 7f,
                AttackPeriod = 1.8f, Sight = 20, Insight = 18, Loot = "cave_bat", Poise = 4, Colour = 0x806878, Nocturnal = true,
                Inflicts = StatusKind.Bleed, InflictChance = 0.35f, InflictPower = 3, InflictTime = 4
            },
            new EnemyDef
            {
                Id = "cave_spider", Name = "Пещерный паук", Body = EnemyBody.Bug, Mind = EnemyMind.Shooter, Look = "spider", Scale = 1.5f, Level = 10, Health = 380, Damage = 15, Speed = 5.4f,
                AttackRange = 13, AttackPeriod = 3f, Sight = 22, Insight = 36, Loot = "cave_spider", Poise = 14, Colour = 0x60E0A0,
                Inflicts = StatusKind.Slow, InflictChance = 0.7f, InflictPower = 0.4f, InflictTime = 3
            },
            new EnemyDef
            {
                Id = "spider_queen", Name = "Паучья мать", Title = "Ткущая во тьме", Body = EnemyBody.Bug, Mind = EnemyMind.Boss, Look = "spider_queen", Scale = 3f, Level = 12, Health = 8200, Damage = 30, Speed = 5f,
                AttackRange = 14, Sight = 40, Insight = 640, Boss = true, Loot = "spider_queen", Poise = 120, Colour = 0x60E0A0,
                Moves = new[] { BossMove.Bolts, BossMove.Summon, BossMove.Lunge, BossMove.Rings, BossMove.Bolts, BossMove.Shield }, Summons = "cave_spider",
                Inflicts = StatusKind.Poison, InflictChance = 0.6f, InflictPower = 10, InflictTime = 5
            },
            new EnemyDef
            {
                Id = "centipede", Name = "Каменная сколопендра", Body = EnemyBody.Serpent, Mind = EnemyMind.Brawler, Look = "centipede", Scale = 1.4f, Level = 9, Health = 460, Damage = 17, Speed = 6f,
                AttackRange = 2.2f, AttackPeriod = 2.2f, Sight = 18, Insight = 34, Loot = "centipede", Poise = 30, Armour = 0.15f, Colour = 0xC07040,
                Inflicts = StatusKind.Poison, InflictChance = 0.4f, InflictPower = 6, InflictTime = 4
            },
            new EnemyDef
            {
                Id = "mine_ghoul", Name = "Одичавший рудокоп", Body = EnemyBody.Human, Mind = EnemyMind.Brawler, Look = "miner", Level = 8, Health = 420, Damage = 16, Speed = 4.4f,
                AttackPeriod = 2.4f, Sight = 18, Insight = 30, Loot = "mine_ghoul", Poise = 26, Colour = 0x9A9AA0, Nocturnal = true
            },
            new EnemyDef
            {
                Id = "thousand_legs", Name = "Тысяченожка", Title = "Червь нижнего штрека", Body = EnemyBody.Serpent, Mind = EnemyMind.Boss, Look = "centipede", Scale = 3.2f, Level = 11, Health = 7400, Damage = 28, Speed = 6.2f,
                AttackRange = 3.6f, Sight = 32, Insight = 520, Boss = true, Loot = "thousand_legs", Poise = 140, Armour = 0.2f, Colour = 0xD08040,
                Moves = new[] { BossMove.Charge, BossMove.Lunge, BossMove.Slam, BossMove.Charge, BossMove.Rings },
                Inflicts = StatusKind.Poison, InflictChance = 0.7f, InflictPower = 8, InflictTime = 5
            },

            // ------------------------------------------------------------------ Cliffs and peak

            new EnemyDef
            {
                Id = "mountain_eagle", Name = "Горный орёл", Body = EnemyBody.Bug, Mind = EnemyMind.Swarm, Look = "eagle", Scale = 1.6f, Level = 9, Health = 300, Damage = 16, Speed = 8f,
                AttackPeriod = 2.4f, Sight = 30, Insight = 32, Loot = "mountain_eagle", Poise = 10, Colour = 0xE0D0B0,
                Inflicts = StatusKind.Bleed, InflictChance = 0.4f, InflictPower = 5, InflictTime = 4
            },
            new EnemyDef
            {
                Id = "storm_eagle", Name = "Грозовой орёл", Title = "Царь Орлиных скал", Body = EnemyBody.Bug, Mind = EnemyMind.Boss, Look = "eagle", Scale = 3f, Level = 12, Health = 7000, Damage = 30, Speed = 8.6f,
                AttackRange = 16, Sight = 50, Insight = 600, Boss = true, Loot = "storm_eagle", Poise = 90, Colour = 0xC0A8FF,
                Moves = new[] { BossMove.Bolts, BossMove.Charge, BossMove.Leap, BossMove.Bolts, BossMove.Summon }, Summons = "mountain_eagle"
            },
            new EnemyDef
            {
                Id = "snow_wolf", Name = "Снежный волк", Body = EnemyBody.Canid, Mind = EnemyMind.PackHunter, Look = "snow", Scale = 1.45f, Level = 11, Health = 560, Damage = 22, Speed = 6.6f,
                AttackPeriod = 2.4f, Sight = 26, Insight = 44, Loot = "snow_wolf", Poise = 20, Colour = 0xEAF6FF,
                Inflicts = StatusKind.Chill, InflictChance = 0.5f, InflictPower = 0.3f, InflictTime = 3
            },

            // ------------------------------------------------------------------ The tomb

            new EnemyDef
            {
                Id = "tomb_guardian", Name = "Терракотовый страж", Body = EnemyBody.Human, Mind = EnemyMind.Brawler, Look = "stone", Scale = 1.2f, Level = 11, Health = 760, Damage = 22, Speed = 3.8f,
                AttackRange = 2.4f, AttackPeriod = 2.8f, Sight = 18, Insight = 50, Loot = "tomb_guardian", Poise = 80, Armour = 0.3f, Colour = 0xC8A878
            },
            new EnemyDef
            {
                Id = "tomb_wisp", Name = "Блуждающий огонь", Body = EnemyBody.Bug, Mind = EnemyMind.Shooter, Look = "wisp", Scale = 0.9f, Level = 11, Health = 260, Damage = 18, Speed = 6f,
                AttackRange = 15, AttackPeriod = 2.6f, Sight = 24, Insight = 40, Loot = "tomb_wisp", Poise = 4, Colour = 0xC0A8FF, Nocturnal = true,
                Inflicts = StatusKind.Burn, InflictChance = 0.5f, InflictPower = 6, InflictTime = 3
            },
            new EnemyDef
            {
                Id = "ancient_shade", Name = "Тень Древнего мастера", Title = "Тот, кто не ушёл", Body = EnemyBody.Human, Mind = EnemyMind.Boss, Look = "shade", Scale = 1.3f, Level = 14, Health = 13000, Damage = 34, Speed = 5.4f,
                AttackRange = 16, Sight = 50, Insight = 1400, Boss = true, Loot = "ancient_shade", Poise = 200, Colour = 0xC0A8FF,
                Moves = new[] { BossMove.Blades, BossMove.Rings, BossMove.Leap, BossMove.Summon, BossMove.Bolts, BossMove.Shield, BossMove.Blades }, Summons = "tomb_guardian",
                Gu = new[] { "bone_spear", "heaven_echo", "dark_tide" }
            },

            // ------------------------------------------------------------------ The Black Tail

            new EnemyDef
            {
                Id = "black_tail_thug", Name = "Головорез Чёрного Хвоста", Body = EnemyBody.Human, Mind = EnemyMind.Brawler, Look = "bandit", Level = 8, Health = 460, Damage = 17, Speed = 4.8f,
                AttackPeriod = 2.3f, Sight = 22, Insight = 32, Loot = "black_tail", Poise = 24, Clan = "bandit",
                Inflicts = StatusKind.Bleed, InflictChance = 0.25f, InflictPower = 4, InflictTime = 4
            },
            new EnemyDef
            {
                Id = "black_tail_master", Name = "Мастер Чёрного Хвоста", Body = EnemyBody.Human, Mind = EnemyMind.Caster, Look = "bandit", Level = 9, Health = 480, Damage = 15, Speed = 4.8f,
                AttackRange = 13, AttackPeriod = 2.6f, Sight = 26, Insight = 56, Loot = "black_tail_master", Poise = 18, Colour = 0xFF9050, Clan = "bandit",
                Gu = new[] { "ember", "flame_burst", "ember" }
            },
            new EnemyDef
            {
                Id = "black_tail_archer", Name = "Стрелок Чёрного Хвоста", Body = EnemyBody.Human, Mind = EnemyMind.Shooter, Look = "bandit", Level = 8, Health = 330, Damage = 15, Speed = 4.6f,
                AttackRange = 18, AttackPeriod = 3f, Sight = 30, Insight = 30, Loot = "black_tail", Poise = 12, Clan = "bandit"
            },
            new EnemyDef
            {
                Id = "black_tail", Name = "Чёрный Хвост", Title = "Атаман западного рудника", Body = EnemyBody.Human, Mind = EnemyMind.Boss, Look = "bandit_chief", Scale = 1.2f, Level = 11, Health = 8800, Damage = 30, Speed = 5.2f,
                AttackRange = 2.6f, Sight = 40, Insight = 700, Boss = true, Loot = "black_tail_chief", Poise = 120, Colour = 0xF08A74, Clan = "bandit",
                Moves = new[] { BossMove.Lunge, BossMove.Bolts, BossMove.Summon, BossMove.Charge, BossMove.Shield, BossMove.Slam }, Summons = "black_tail_thug",
                Gu = new[] { "flame_burst", "fire_robe" }, Inflicts = StatusKind.Burn, InflictChance = 0.4f, InflictPower = 8, InflictTime = 3
            },

            // ------------------------------------------------------------------ The clans' patrols (fight them only if they hate the hero, or in a duel)

            new EnemyDef
            {
                Id = "bai_patrol", Name = "Дозорный Бай", Body = EnemyBody.Human, Mind = EnemyMind.Caster, Look = "bai", Level = 8, Health = 440, Damage = 14, Speed = 5f,
                AttackRange = 15, AttackPeriod = 2.6f, Sight = 26, Insight = 50, Loot = "clan_patrol", Poise = 18, Colour = 0x9FE8FF, Clan = "bai",
                Gu = new[] { "ice_spike", "frost_breath", "ice_spike", "water_shield" }
            },
            new EnemyDef
            {
                Id = "xiong_patrol", Name = "Дозорный Сюн", Body = EnemyBody.Human, Mind = EnemyMind.Caster, Look = "xiong", Scale = 1.08f, Level = 8, Health = 560, Damage = 18, Speed = 4.8f,
                AttackRange = 6, AttackPeriod = 2.4f, Sight = 24, Insight = 50, Loot = "clan_patrol", Poise = 40, Colour = 0xE0A070, Clan = "xiong",
                Gu = new[] { "bear_strength", "bear_roar", "stone_beetle" }
            },
            new EnemyDef
            {
                Id = "gu_yue_patrol", Name = "Дозорный Гу Юэ", Body = EnemyBody.Human, Mind = EnemyMind.Caster, Look = "student", Level = 8, Health = 450, Damage = 15, Speed = 5f,
                AttackRange = 15, AttackPeriod = 2.4f, Sight = 26, Insight = 50, Loot = "clan_patrol", Poise = 20, Colour = 0xA0D2FF, Clan = "gu_yue",
                Gu = new[] { "moonlight", "moonglow", "jade_skin" }
            },
            new EnemyDef
            {
                Id = "rogue_master", Name = "Бродячий мастер Гу", Body = EnemyBody.Human, Mind = EnemyMind.Caster, Look = "rogue", Level = 7, Health = 420, Damage = 14, Speed = 4.8f,
                AttackRange = 14, AttackPeriod = 2.6f, Sight = 24, Insight = 44, Loot = "rogue_master", Poise = 18, Colour = 0xE8C98A, Clan = "wanderers",
                Gu = new[] { "spark", "bone_needle", "jade_skin" }
            },
            new EnemyDef
            {
                Id = "bai_champion", Name = "Бай Шэн", Title = "Первый клинок Бай", Body = EnemyBody.Human, Mind = EnemyMind.Boss, Look = "bai", Scale = 1.1f, Level = 10, Health = 6800, Damage = 26, Speed = 5.6f,
                AttackRange = 15, Sight = 40, Insight = 480, Boss = true, Loot = "duelist", Poise = 80, Colour = 0x9FE8FF, Clan = "bai",
                Moves = new[] { BossMove.Bolts, BossMove.Rings, BossMove.Shield, BossMove.Leap, BossMove.Bolts }, Gu = new[] { "ice_spike", "white_cloud", "river_serpent" },
                Inflicts = StatusKind.Chill, InflictChance = 0.5f, InflictPower = 0.3f, InflictTime = 3
            },
            new EnemyDef
            {
                Id = "xiong_champion", Name = "Сюн Ба", Title = "Медвежья лапа Сюн", Body = EnemyBody.Human, Mind = EnemyMind.Boss, Look = "xiong", Scale = 1.25f, Level = 10, Health = 7400, Damage = 30, Speed = 5.2f,
                AttackRange = 2.8f, Sight = 40, Insight = 480, Boss = true, Loot = "duelist", Poise = 140, Colour = 0xE0A070, Clan = "xiong",
                Moves = new[] { BossMove.Charge, BossMove.Slam, BossMove.Howl, BossMove.Lunge, BossMove.Slam }, Gu = new[] { "bear_roar", "great_bear" }
            },
        };

        public static readonly LootTable[] Tables =
        {
            new LootTable { Id = "marsh_snake", StonesMin = 3, StonesMax = 8, Items = new[] { new LootEntry { Item = "snake_skin", Chance = 0.5f }, new LootEntry { Item = "snake_gall", Chance = 0.45f }, new LootEntry { Item = "snake_fang", Chance = 0.25f } }, GuChance = 0.06f, GuPool = new[] { "poison_needle", "clinging_vine" } },
            new LootTable { Id = "marsh_toad", StonesMin = 3, StonesMax = 7, Items = new[] { new LootEntry { Item = "toad_venom", Chance = 0.6f }, new LootEntry { Item = "marsh_reed", Chance = 0.4f, Max = 2 } }, GuChance = 0.05f, GuPool = new[] { "poison_needle", "echo" } },
            new LootTable
            {
                Id = "swamp_python", StonesMin = 160, StonesMax = 220, Items = new[] { new LootEntry { Item = "python_core" }, new LootEntry { Item = "snake_skin", Min = 4, Max = 6 }, new LootEntry { Item = "snake_gall", Min = 3, Max = 5 }, new LootEntry { Item = "scroll_ten_thousand_needles" } },
                GuChance = 1, GuPool = new[] { "toxic_mist", "poison_needle" }, GuMinQuality = 1
            },
            new LootTable { Id = "rogue_master", StonesMin = 20, StonesMax = 40, Items = new[] { new LootEntry { Item = "essence_pill", Chance = 0.4f }, new LootEntry { Item = "antidote", Chance = 0.3f }, new LootEntry { Item = "book_paths", Chance = 0.05f } }, GuChance = 0.2f, GuPool = new[] { "poison_needle", "spark", "bone_needle", "ember", "echo", "night_eye" } },

            new LootTable { Id = "brown_bear", StonesMin = 5, StonesMax = 12, Items = new[] { new LootEntry { Item = "bear_claw", Chance = 0.5f }, new LootEntry { Item = "bear_gall", Chance = 0.2f }, new LootEntry { Item = "jerky", Chance = 0.5f, Max = 3 } }, GuChance = 0.06f, GuPool = new[] { "black_boar", "bear_strength" } },
            new LootTable
            {
                Id = "bear_king", StonesMin = 150, StonesMax = 200, Items = new[] { new LootEntry { Item = "bear_king_heart" }, new LootEntry { Item = "bear_claw", Min = 4, Max = 6 }, new LootEntry { Item = "bear_gall", Min = 2, Max = 3 } },
                GuChance = 1, GuPool = new[] { "white_boar", "bear_roar" }, GuMinQuality = 1
            },
            new LootTable { Id = "wild_boar", StonesMin = 2, StonesMax = 6, Items = new[] { new LootEntry { Item = "jerky", Chance = 0.8f, Max = 2 } }, GuChance = 0.05f, GuPool = new[] { "black_boar" } },
            new LootTable { Id = "bee_swarm", StonesMin = 1, StonesMax = 4, Items = new[] { new LootEntry { Item = "honey", Chance = 0.6f } }, GuChance = 0.04f, GuPool = new[] { "honey_dew", "light_steps" } },

            new LootTable { Id = "cave_bat", StonesMin = 3, StonesMax = 8, Items = new[] { new LootEntry { Item = "bat_wing", Chance = 0.6f }, new LootEntry { Item = "night_moth", Chance = 0.3f } }, GuChance = 0.06f, GuPool = new[] { "night_eye", "echo" } },
            new LootTable { Id = "cave_spider", StonesMin = 6, StonesMax = 14, Items = new[] { new LootEntry { Item = "spider_silk", Chance = 0.6f, Max = 2 }, new LootEntry { Item = "spider_eye", Chance = 0.3f } }, GuChance = 0.07f, GuPool = new[] { "clinging_vine", "night_eye", "poison_needle" } },
            new LootTable
            {
                Id = "spider_queen", StonesMin = 260, StonesMax = 340, Items = new[] { new LootEntry { Item = "spider_queen_eye" }, new LootEntry { Item = "spider_silk", Min = 6, Max = 9 }, new LootEntry { Item = "scroll_dark_tide" } },
                GuChance = 1, GuPool = new[] { "stealth_scales", "toxic_mist" }, GuMinQuality = 2
            },
            new LootTable { Id = "centipede", StonesMin = 6, StonesMax = 14, Items = new[] { new LootEntry { Item = "centipede_shell", Chance = 0.5f }, new LootEntry { Item = "snake_gall", Chance = 0.2f } }, GuChance = 0.06f, GuPool = new[] { "poison_needle", "steel_bone", "bone_needle" } },
            new LootTable { Id = "mine_ghoul", StonesMin = 8, StonesMax = 16, Items = new[] { new LootEntry { Item = "iron_ore", Chance = 0.6f, Max = 2 }, new LootEntry { Item = "jade_shard", Chance = 0.2f }, new LootEntry { Item = "miner_tag", Chance = 0.35f } }, GuChance = 0.05f, GuPool = new[] { "stone_beetle", "earth_spikes" } },
            new LootTable
            {
                Id = "thousand_legs", StonesMin = 200, StonesMax = 260, Items = new[] { new LootEntry { Item = "centipede_shell", Min = 5, Max = 7 }, new LootEntry { Item = "jade_shard", Min = 2, Max = 4 }, new LootEntry { Item = "scroll_steel_bone" } },
                GuChance = 1, GuPool = new[] { "steel_bone", "earth_spikes" }, GuMinQuality = 1
            },

            new LootTable { Id = "mountain_eagle", StonesMin = 6, StonesMax = 12, Items = new[] { new LootEntry { Item = "eagle_feather", Chance = 0.7f, Max = 2 } }, GuChance = 0.06f, GuPool = new[] { "light_steps", "tailwind", "thunder_wing" } },
            new LootTable
            {
                Id = "storm_eagle", StonesMin = 240, StonesMax = 320, Items = new[] { new LootEntry { Item = "eagle_feather", Min = 6, Max = 8 }, new LootEntry { Item = "storm_core", Min = 3, Max = 5 }, new LootEntry { Item = "scroll_blazing_feather" } },
                GuChance = 1, GuPool = new[] { "thunder_wing", "storm_step" }, GuMinQuality = 2
            },
            new LootTable { Id = "snow_wolf", StonesMin = 10, StonesMax = 20, Items = new[] { new LootEntry { Item = "wolf_pelt", Chance = 0.5f }, new LootEntry { Item = "frost_lotus", Chance = 0.2f } }, GuChance = 0.07f, GuPool = new[] { "frost_breath", "ice_spike", "white_cloud" } },

            new LootTable { Id = "tomb_guardian", StonesMin = 14, StonesMax = 26, Items = new[] { new LootEntry { Item = "bone_shard", Chance = 0.6f, Max = 2 }, new LootEntry { Item = "ancient_coin", Chance = 0.15f } }, GuChance = 0.06f, GuPool = new[] { "steel_bone", "earth_spikes", "bone_spear" } },
            new LootTable { Id = "tomb_wisp", StonesMin = 10, StonesMax = 20, Items = new[] { new LootEntry { Item = "spirit_moss", Chance = 0.35f }, new LootEntry { Item = "ember_coal", Chance = 0.4f } }, GuChance = 0.06f, GuPool = new[] { "ember", "night_eye", "flame_burst" } },
            new LootTable
            {
                Id = "ancient_shade", StonesMin = 600, StonesMax = 800, Items = new[] { new LootEntry { Item = "ancient_relic" }, new LootEntry { Item = "ancient_coin", Min = 3, Max = 5 }, new LootEntry { Item = "scroll_ancient_bone" }, new LootEntry { Item = "book_spring_autumn" } },
                GuChance = 1, GuPool = new[] { "heaven_echo", "dark_tide", "ancient_bone" }, GuMinQuality = 2
            },

            new LootTable { Id = "black_tail", StonesMin = 12, StonesMax = 24, Items = new[] { new LootEntry { Item = "silver_ingot", Chance = 0.15f }, new LootEntry { Item = "stolen_rice", Chance = 0.2f }, new LootEntry { Item = "wine_jar", Chance = 0.25f } }, GuChance = 0.07f, GuPool = new[] { "ember", "bone_needle", "jade_skin", "spark" } },
            new LootTable { Id = "black_tail_master", StonesMin = 24, StonesMax = 44, Items = new[] { new LootEntry { Item = "silver_ingot", Chance = 0.3f }, new LootEntry { Item = "ember_coal", Chance = 0.5f, Max = 2 }, new LootEntry { Item = "scroll_flame_burst", Chance = 0.05f } }, GuChance = 0.15f, GuPool = new[] { "ember", "flame_burst", "fire_robe" } },
            new LootTable
            {
                Id = "black_tail_chief", StonesMin = 300, StonesMax = 380, Items = new[] { new LootEntry { Item = "black_tail_seal" }, new LootEntry { Item = "silver_ingot", Min = 3, Max = 5 }, new LootEntry { Item = "fort_key" }, new LootEntry { Item = "scroll_fire_robe" } },
                GuChance = 1, GuPool = new[] { "fire_robe", "flame_burst" }, GuMinQuality = 1
            },
            new LootTable { Id = "clan_patrol", StonesMin = 18, StonesMax = 30, Items = new[] { new LootEntry { Item = "essence_pill", Chance = 0.4f }, new LootEntry { Item = "heal_pill", Chance = 0.4f } }, GuChance = 0.1f, GuPool = new[] { "water_shield", "frost_breath", "bear_strength", "moonlight", "jade_skin" } },

            // Chests of the regions.
            new LootTable { Id = "chest_marsh", StonesMin = 60, StonesMax = 90, Items = new[] { new LootEntry { Item = "antidote", Min = 2, Max = 3 }, new LootEntry { Item = "snake_gall", Min = 2, Max = 3 }, new LootEntry { Item = "scroll_toxic_mist" } }, GuChance = 1, GuPool = new[] { "poison_needle", "clinging_vine" }, GuMinQuality = 1 },
            new LootTable { Id = "chest_bear", StonesMin = 50, StonesMax = 80, Items = new[] { new LootEntry { Item = "honey", Min = 3, Max = 4 }, new LootEntry { Item = "fury_pill" }, new LootEntry { Item = "scroll_bear_roar" } }, GuChance = 1, GuPool = new[] { "black_boar", "honey_dew" } },
            new LootTable { Id = "chest_mine", StonesMin = 80, StonesMax = 120, Items = new[] { new LootEntry { Item = "jade_shard", Min = 2, Max = 3 }, new LootEntry { Item = "iron_ore", Min = 3, Max = 5 }, new LootEntry { Item = "mine_key" }, new LootEntry { Item = "scroll_earth_spikes" } }, GuChance = 1, GuPool = new[] { "earth_spikes", "stone_beetle" }, GuMinQuality = 1 },
            new LootTable { Id = "chest_mine_deep", StonesMin = 160, StonesMax = 220, Items = new[] { new LootEntry { Item = "ancient_coin", Min = 1, Max = 2 }, new LootEntry { Item = "rock_3" }, new LootEntry { Item = "book_ancient" }, new LootEntry { Item = "scroll_gold_silkworm" } }, GuChance = 1, GuPool = new[] { "gold_silkworm" }, GuMinQuality = 1 },
            new LootTable { Id = "chest_spiders", StonesMin = 120, StonesMax = 180, Items = new[] { new LootEntry { Item = "spider_silk", Min = 3, Max = 4 }, new LootEntry { Item = "shadow_pill" }, new LootEntry { Item = "scroll_stealth_scales" } }, GuChance = 1, GuPool = new[] { "night_eye", "stealth_scales" }, GuMinQuality = 1 },
            new LootTable { Id = "chest_tomb", StonesMin = 220, StonesMax = 300, Items = new[] { new LootEntry { Item = "ancient_coin", Min = 2, Max = 3 }, new LootEntry { Item = "clarity_pill", Min = 1, Max = 2 }, new LootEntry { Item = "tomb_key" }, new LootEntry { Item = "scroll_bone_spear" } }, GuChance = 1, GuPool = new[] { "bone_spear", "steel_bone" }, GuMinQuality = 1 },
            new LootTable { Id = "chest_tomb_inner", StonesMin = 360, StonesMax = 460, Items = new[] { new LootEntry { Item = "great_essence_pill", Min = 2, Max = 2 }, new LootEntry { Item = "rock_3" }, new LootEntry { Item = "scroll_heaven_echo" }, new LootEntry { Item = "scroll_immortal_body" } }, GuChance = 1, GuPool = new[] { "gold_silkworm", "blood_skull" }, GuMinQuality = 2 },
            new LootTable { Id = "chest_eagle", StonesMin = 90, StonesMax = 130, Items = new[] { new LootEntry { Item = "eagle_feather", Min = 3, Max = 4 }, new LootEntry { Item = "wind_pill", Min = 1, Max = 2 }, new LootEntry { Item = "scroll_thunder_wing" } }, GuChance = 1, GuPool = new[] { "light_steps", "thunder_wing" } },
            new LootTable { Id = "chest_peak", StonesMin = 300, StonesMax = 400, Items = new[] { new LootEntry { Item = "frost_lotus", Min = 3, Max = 4 }, new LootEntry { Item = "clarity_pill", Min = 2, Max = 2 }, new LootEntry { Item = "rock_3" } }, GuChance = 1, GuPool = new[] { "ice_lotus", "moon_lake" }, GuMinQuality = 1 },
            new LootTable { Id = "chest_tree", StonesMin = 60, StonesMax = 90, Items = new[] { new LootEntry { Item = "blood_vine", Min = 1, Max = 2 }, new LootEntry { Item = "regen_pill", Min = 2, Max = 2 }, new LootEntry { Item = "ginseng", Min = 2, Max = 3 } }, GuChance = 1, GuPool = new[] { "deep_breath", "life_grass", "honey_dew" }, GuMinQuality = 1 },
            new LootTable { Id = "chest_fort", StonesMin = 260, StonesMax = 360, Items = new[] { new LootEntry { Item = "silver_ingot", Min = 4, Max = 6 }, new LootEntry { Item = "stolen_rice", Min = 2, Max = 2 }, new LootEntry { Item = "scroll_blood_skull" } }, GuChance = 1, GuPool = new[] { "fire_robe", "blood_skull" }, GuMinQuality = 1 },
            new LootTable { Id = "chest_bai_vault", StonesMin = 200, StonesMax = 260, Items = new[] { new LootEntry { Item = "frost_lotus", Min = 3, Max = 4 }, new LootEntry { Item = "scroll_ice_lotus" } }, GuChance = 1, GuPool = new[] { "white_cloud", "river_serpent" }, GuMinQuality = 2 },
            new LootTable { Id = "chest_hamlet", StonesMin = 10, StonesMax = 20, Items = new[] { new LootEntry { Item = "jerky", Min = 1, Max = 3 }, new LootEntry { Item = "bitter_root", Min = 1, Max = 2 }, new LootEntry { Item = "heal_pill", Chance = 0.5f } } },
        };
    }
}
