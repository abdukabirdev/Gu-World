using System;

namespace GuWorld.Core
{
    /// <summary>
    /// The wild lands round Qing Mao (beyond 4.3 km from its middle, out to the world's edge), the ground
    /// of masters of the fourth and fifth ranks: the Frost Range to the north, the Thunder Plateau to the
    /// north-east, the Bamboo Sea to the east, the Old Battlefield to the south-east, the Red Canyon to the
    /// south-west and the Jade Lake to the west. Each has its beasts, its king, a lantern, a cache that
    /// opens when the king falls, and herbs; the wanderers' captain sends the hero to all six.
    /// </summary>
    public static class Wilds
    {
        public static readonly PlaceDef[] Places =
        {
            new PlaceDef { Id = "frost_range", Name = "Морозный хребет", At = new MapPoint(-500, 6400), Radius = 70, Flatten = true, Colour = 0xEAF6FF },
            new PlaceDef { Id = "thunder_plateau", Name = "Громовое плато", At = new MapPoint(4700, 4700), Radius = 80, Flatten = true, Colour = 0xD0C0FF },
            new PlaceDef { Id = "bamboo_sea", Name = "Бамбуковое море", At = new MapPoint(6500, -400), Radius = 90, Flatten = true, Colour = 0x9FEFA4 },
            new PlaceDef { Id = "old_battlefield", Name = "Древнее поле битвы", At = new MapPoint(4400, -5000), Radius = 90, Flatten = true, Colour = 0xC8B8A8 },
            new PlaceDef { Id = "red_canyon", Name = "Красный каньон", At = new MapPoint(-4800, -4600), Radius = 80, Flatten = true, Colour = 0xFF8050 },
            new PlaceDef { Id = "jade_lake", Name = "Нефритовое озеро", At = new MapPoint(-6300, 700), Radius = 80, Flatten = true, Colour = 0x60D0B0 },
        };

        static MapPoint P(string place, float bearing, float share)
        {
            foreach (var p in Places)
            {
                if (p.Id == place)
                {
                    var radians = bearing * Math.PI / 180;

                    return new MapPoint(p.At.X + (float)Math.Sin(radians) * p.Radius * share, p.At.Z + (float)Math.Cos(radians) * p.Radius * share);
                }
            }

            throw new ArgumentException("no wild place " + place);
        }

        // ------------------------------------------------------------------ Beasts and masters

        public static readonly EnemyDef[] Enemies =
        {
            // The Frost Range.
            new EnemyDef
            {
                Id = "frost_wolf", Name = "Морозный волк", Body = EnemyBody.Canid, Mind = EnemyMind.PackHunter, Look = "snow", Scale = 1.7f, Level = 18, Health = 1500, Damage = 46, Speed = 7f,
                AttackPeriod = 2.3f, Sight = 30, Insight = 120, Loot = "frost_wolf", Poise = 40, Armour = 0.1f, Colour = 0xD8F0FF,
                Inflicts = StatusKind.Chill, InflictChance = 0.6f, InflictPower = 0.35f, InflictTime = 3
            },
            new EnemyDef
            {
                Id = "ice_bear", Name = "Ледяной медведь", Body = EnemyBody.Canid, Mind = EnemyMind.Brawler, Look = "bear", Scale = 2.4f, Level = 19, Health = 1800, Damage = 50, Speed = 5.4f,
                AttackRange = 2.8f, AttackPeriod = 3f, Sight = 22, Insight = 150, Loot = "ice_bear", Poise = 90, Armour = 0.2f, Colour = 0xD0E8FF,
                Inflicts = StatusKind.Freeze, InflictChance = 0.2f, InflictTime = 1.5f
            },
            new EnemyDef
            {
                Id = "frost_emperor", Name = "Император снежных волков", Title = "Хозяин Морозного хребта", Body = EnemyBody.Canid, Mind = EnemyMind.Boss, Look = "snow", Scale = 3.4f, Level = 24, Health = 17000, Damage = 74, Speed = 7.4f,
                AttackRange = 4.4f, Sight = 50, Insight = 2600, Boss = true, Loot = "frost_emperor", Poise = 260, Armour = 0.2f, Colour = 0xF0FAFF,
                Moves = new[] { BossMove.Lunge, BossMove.Howl, BossMove.Rings, BossMove.Charge, BossMove.Summon, BossMove.Lunge }, Summons = "frost_wolf",
                Inflicts = StatusKind.Chill, InflictChance = 1, InflictPower = 0.4f, InflictTime = 4
            },

            // The Thunder Plateau.
            new EnemyDef
            {
                Id = "thunder_roc", Name = "Громовой рух", Body = EnemyBody.Bug, Mind = EnemyMind.Skirmisher, Look = "eagle", Scale = 2.2f, Level = 20, Health = 1600, Damage = 50, Speed = 9f,
                AttackRange = 10, AttackPeriod = 2.4f, Sight = 40, Insight = 140, Loot = "thunder_roc", Poise = 30, Colour = 0xC8B8FF,
                Inflicts = StatusKind.Stun, InflictChance = 0.15f, InflictTime = 0.8f
            },
            new EnemyDef
            {
                Id = "storm_master", Name = "Мастер грозы", Body = EnemyBody.Human, Mind = EnemyMind.Caster, Look = "scribe", Level = 21, Health = 1300, Damage = 40, Speed = 5f,
                AttackRange = 16, AttackPeriod = 2.4f, Sight = 32, Insight = 170, Loot = "storm_master", Poise = 30, Colour = 0xC8B8FF, Clan = "wanderers",
                Gu = new[] { "myriad_lightning", "thunder_crown", "storm_step" }
            },
            new EnemyDef
            {
                Id = "roc_king", Name = "Царь громовых рухов", Title = "Хозяин Громового плато", Body = EnemyBody.Bug, Mind = EnemyMind.Boss, Look = "eagle", Scale = 4.6f, Level = 26, Health = 22000, Damage = 80, Speed = 9.4f,
                AttackRange = 20, Sight = 60, Insight = 3000, Boss = true, Loot = "roc_king", Poise = 220, Colour = 0xE0D0FF,
                Moves = new[] { BossMove.Bolts, BossMove.Charge, BossMove.Leap, BossMove.Bolts, BossMove.Summon, BossMove.Rings }, Summons = "thunder_roc"
            },

            // The Bamboo Sea.
            new EnemyDef
            {
                Id = "jade_python", Name = "Нефритовый удав", Body = EnemyBody.Serpent, Mind = EnemyMind.Brawler, Look = "python", Scale = 2f, Level = 19, Health = 2000, Damage = 52, Speed = 6f,
                AttackRange = 3f, AttackPeriod = 2.8f, Sight = 18, Insight = 140, Loot = "jade_python", Poise = 60, Armour = 0.15f, Colour = 0x60D090,
                Inflicts = StatusKind.Poison, InflictChance = 0.8f, InflictPower = 18, InflictTime = 6
            },
            new EnemyDef
            {
                Id = "bamboo_spider", Name = "Бамбуковый паук", Body = EnemyBody.Bug, Mind = EnemyMind.Skirmisher, Look = "spider", Scale = 1.8f, Level = 18, Health = 1100, Damage = 40, Speed = 6.6f,
                AttackRange = 8, AttackPeriod = 2.2f, Sight = 22, Insight = 110, Loot = "bamboo_spider", Poise = 24, Colour = 0x90E080, Nocturnal = true,
                Inflicts = StatusKind.Slow, InflictChance = 0.6f, InflictPower = 0.4f, InflictTime = 3
            },
            new EnemyDef
            {
                Id = "python_queen", Name = "Царица нефритовых удавов", Title = "Хозяйка Бамбукового моря", Body = EnemyBody.Serpent, Mind = EnemyMind.Boss, Look = "python", Scale = 4f, Level = 25, Health = 19000, Damage = 72, Speed = 6.4f,
                AttackRange = 4.6f, Sight = 44, Insight = 2800, Boss = true, Loot = "python_queen", Poise = 280, Armour = 0.2f, Colour = 0x80F0B0,
                Moves = new[] { BossMove.Lunge, BossMove.Rings, BossMove.Charge, BossMove.Summon, BossMove.Lunge, BossMove.Slam }, Summons = "jade_python",
                Inflicts = StatusKind.Poison, InflictChance = 1, InflictPower = 26, InflictTime = 6
            },

            // The Old Battlefield.
            new EnemyDef
            {
                Id = "bone_soldier", Name = "Костяной воин", Body = EnemyBody.Human, Mind = EnemyMind.Brawler, Look = "shade", Level = 19, Health = 1700, Damage = 48, Speed = 4.6f,
                AttackPeriod = 2.4f, Sight = 24, Insight = 130, Loot = "bone_soldier", Poise = 50, Armour = 0.25f, Colour = 0xD8D0C0, Nocturnal = true,
                Inflicts = StatusKind.Bleed, InflictChance = 0.4f, InflictPower = 14, InflictTime = 4
            },
            new EnemyDef
            {
                Id = "bone_master", Name = "Мёртвый мастер Гу", Body = EnemyBody.Human, Mind = EnemyMind.Caster, Look = "shade", Level = 21, Health = 1400, Damage = 42, Speed = 4.8f,
                AttackRange = 15, AttackPeriod = 2.6f, Sight = 30, Insight = 170, Loot = "bone_master", Poise = 30, Colour = 0xB0A0D0,
                Gu = new[] { "bone_spear", "bone_needle", "dark_tide", "ten_thousand_needles" }
            },
            new EnemyDef
            {
                Id = "bone_general", Name = "Костяной генерал", Title = "Тот, кто держит поле", Body = EnemyBody.Human, Mind = EnemyMind.Boss, Look = "shade", Scale = 1.7f, Level = 27, Health = 19500, Damage = 82, Speed = 5.6f,
                AttackRange = 18, Sight = 50, Insight = 3200, Boss = true, Loot = "bone_general", Poise = 300, Armour = 0.25f, Colour = 0xE8E0D0,
                Moves = new[] { BossMove.Blades, BossMove.Slam, BossMove.Summon, BossMove.Shield, BossMove.Leap, BossMove.Blades, BossMove.Bolts }, Summons = "bone_soldier",
                Gu = new[] { "bone_spear", "heaven_echo", "dark_tide" }
            },

            // The Red Canyon.
            new EnemyDef
            {
                Id = "fire_ape", Name = "Огненная обезьяна", Body = EnemyBody.Canid, Mind = EnemyMind.Brawler, Look = "bear", Scale = 1.9f, Level = 20, Health = 2100, Damage = 54, Speed = 6.4f,
                AttackRange = 2.6f, AttackPeriod = 2.5f, Sight = 24, Insight = 150, Loot = "fire_ape", Poise = 60, Colour = 0xFF7040,
                Inflicts = StatusKind.Burn, InflictChance = 0.5f, InflictPower = 20, InflictTime = 4
            },
            new EnemyDef
            {
                Id = "flame_master", Name = "Мастер багрового пламени", Body = EnemyBody.Human, Mind = EnemyMind.Caster, Look = "rogue", Level = 21, Health = 1350, Damage = 44, Speed = 5f,
                AttackRange = 15, AttackPeriod = 2.4f, Sight = 30, Insight = 170, Loot = "flame_master", Poise = 30, Colour = 0xFF6040, Clan = "bandit",
                Gu = new[] { "crimson_flame", "blazing_feather", "fire_robe" }
            },
            new EnemyDef
            {
                Id = "ape_king", Name = "Багровый король обезьян", Title = "Хозяин Красного каньона", Body = EnemyBody.Canid, Mind = EnemyMind.Boss, Look = "bear", Scale = 3.6f, Level = 26, Health = 21500, Damage = 84, Speed = 6.8f,
                AttackRange = 4.4f, Sight = 44, Insight = 3000, Boss = true, Loot = "ape_king", Poise = 300, Armour = 0.15f, Colour = 0xFF5030,
                Moves = new[] { BossMove.Slam, BossMove.Charge, BossMove.Leap, BossMove.Howl, BossMove.Slam, BossMove.Summon }, Summons = "fire_ape",
                Inflicts = StatusKind.Burn, InflictChance = 1, InflictPower = 26, InflictTime = 5
            },

            // The Jade Lake.
            new EnemyDef
            {
                Id = "lake_serpent", Name = "Озёрный змей", Body = EnemyBody.Serpent, Mind = EnemyMind.Brawler, Look = "marsh", Scale = 2.2f, Level = 20, Health = 1800, Damage = 50, Speed = 6.4f,
                AttackRange = 3.2f, AttackPeriod = 2.6f, Sight = 20, Insight = 140, Loot = "lake_serpent", Poise = 50, Colour = 0x60C0D0,
                Inflicts = StatusKind.Slow, InflictChance = 0.5f, InflictPower = 0.4f, InflictTime = 3
            },
            new EnemyDef
            {
                Id = "lake_hermit", Name = "Озёрный отшельник", Body = EnemyBody.Human, Mind = EnemyMind.Caster, Look = "monk", Level = 22, Health = 1400, Damage = 44, Speed = 5f,
                AttackRange = 16, AttackPeriod = 2.5f, Sight = 30, Insight = 180, Loot = "lake_hermit", Poise = 30, Colour = 0x80E0D0, Clan = "wanderers",
                Gu = new[] { "north_wind_ice", "river_serpent", "white_cloud" }
            },
            new EnemyDef
            {
                Id = "lake_dragon", Name = "Нефритовый змей-дракон", Title = "Хозяин Нефритового озера", Body = EnemyBody.Serpent, Mind = EnemyMind.Boss, Look = "python", Scale = 5f, Level = 28, Health = 20500, Damage = 88, Speed = 7f,
                AttackRange = 18, Sight = 60, Insight = 3600, Boss = true, Loot = "lake_dragon", Poise = 340, Armour = 0.25f, Colour = 0x60F0D0,
                Moves = new[] { BossMove.Bolts, BossMove.Rings, BossMove.Lunge, BossMove.Charge, BossMove.Summon, BossMove.Bolts, BossMove.Rings }, Summons = "lake_serpent",
                Inflicts = StatusKind.Freeze, InflictChance = 0.3f, InflictTime = 1.5f
            },
        };

        public static readonly LootTable[] Tables =
        {
            new LootTable { Id = "frost_wolf", StonesMin = 30, StonesMax = 60, Items = new[] { new LootEntry { Item = "wolf_pelt", Chance = 0.6f, Max = 2 }, new LootEntry { Item = "frost_lotus", Chance = 0.35f } }, GuChance = 0.06f, GuPool = new[] { "ice_lotus", "frost_demon", "north_wind_ice" } },
            new LootTable { Id = "ice_bear", StonesMin = 40, StonesMax = 80, Items = new[] { new LootEntry { Item = "bear_claw", Chance = 0.7f, Max = 3 }, new LootEntry { Item = "bear_gall", Chance = 0.4f } }, GuChance = 0.06f, GuPool = new[] { "great_bear", "golden_bell" } },
            new LootTable { Id = "thunder_roc", StonesMin = 40, StonesMax = 70, Items = new[] { new LootEntry { Item = "eagle_feather", Chance = 0.8f, Max = 3 }, new LootEntry { Item = "storm_core", Chance = 0.6f, Max = 2 } }, GuChance = 0.06f, GuPool = new[] { "thunder_crown", "ten_thousand_li" } },
            new LootTable { Id = "storm_master", StonesMin = 60, StonesMax = 110, Items = new[] { new LootEntry { Item = "storm_core", Chance = 0.6f, Max = 2 }, new LootEntry { Item = "scroll_myriad_lightning", Chance = 0.04f } }, GuChance = 0.1f, GuPool = new[] { "myriad_lightning", "thunder_crown" } },
            new LootTable { Id = "jade_python", StonesMin = 40, StonesMax = 75, Items = new[] { new LootEntry { Item = "snake_skin", Chance = 0.8f, Max = 2 }, new LootEntry { Item = "jade_shard", Chance = 0.5f, Max = 2 } }, GuChance = 0.06f, GuPool = new[] { "ten_thousand_needles", "dark_tide" } },
            new LootTable { Id = "bamboo_spider", StonesMin = 30, StonesMax = 55, Items = new[] { new LootEntry { Item = "spider_silk", Chance = 0.8f, Max = 3 }, new LootEntry { Item = "spider_eye", Chance = 0.4f } }, GuChance = 0.05f, GuPool = new[] { "stealth_scales", "ten_thousand_needles" } },
            new LootTable { Id = "bone_soldier", StonesMin = 35, StonesMax = 65, Items = new[] { new LootEntry { Item = "bone_shard", Chance = 0.9f, Max = 3 }, new LootEntry { Item = "ancient_coin", Chance = 0.3f } }, GuChance = 0.06f, GuPool = new[] { "ancient_bone", "bone_spear" } },
            new LootTable { Id = "bone_master", StonesMin = 60, StonesMax = 110, Items = new[] { new LootEntry { Item = "ancient_coin", Chance = 0.6f, Max = 2 }, new LootEntry { Item = "ancient_relic", Chance = 0.05f } }, GuChance = 0.1f, GuPool = new[] { "immortal_shadow", "dark_tide", "ancient_bone" } },
            new LootTable { Id = "fire_ape", StonesMin = 40, StonesMax = 80, Items = new[] { new LootEntry { Item = "ember_coal", Chance = 0.9f, Max = 4 }, new LootEntry { Item = "blood_crystal", Chance = 0.2f } }, GuChance = 0.06f, GuPool = new[] { "blazing_feather", "fire_robe" } },
            new LootTable { Id = "flame_master", StonesMin = 60, StonesMax = 110, Items = new[] { new LootEntry { Item = "ember_coal", Chance = 0.8f, Max = 3 }, new LootEntry { Item = "scroll_crimson_flame", Chance = 0.04f } }, GuChance = 0.1f, GuPool = new[] { "crimson_flame", "blazing_feather" } },
            new LootTable { Id = "lake_serpent", StonesMin = 40, StonesMax = 75, Items = new[] { new LootEntry { Item = "snake_skin", Chance = 0.7f, Max = 2 }, new LootEntry { Item = "dew_gourd", Chance = 0.6f, Max = 3 } }, GuChance = 0.06f, GuPool = new[] { "river_serpent", "white_cloud" } },
            new LootTable { Id = "lake_hermit", StonesMin = 60, StonesMax = 110, Items = new[] { new LootEntry { Item = "frost_lotus", Chance = 0.5f, Max = 2 }, new LootEntry { Item = "scroll_north_wind_ice", Chance = 0.04f } }, GuChance = 0.1f, GuPool = new[] { "north_wind_ice", "ice_lotus" } },

            // The kings.
            King("frost_emperor", "relic_r4", "scroll_heaven_ice", new[] { "north_wind_ice", "frost_demon" }),
            King("roc_king", "relic_r4", "scroll_heaven_thunder", new[] { "myriad_lightning", "ten_thousand_li" }),
            King("python_queen", "relic_r4", "scroll_boundless_sea", new[] { "immortal_shadow", "ten_thousand_needles" }),
            King("bone_general", "relic_r5", "scroll_immortal_body", new[] { "golden_bell", "immortal_shadow" }),
            King("ape_king", "relic_r4", "scroll_stone_giant", new[] { "crimson_flame", "stone_giant" }),
            King("lake_dragon", "relic_r5", "scroll_full_moon", new[] { "north_wind_ice", "moon_sickle" }),

            // The caches the kings sat on.
            new LootTable { Id = "cache_wilds", StonesMin = 600, StonesMax = 900, Items = new[] { new LootEntry { Item = "heart_of_qingmao", Chance = 0.5f }, new LootEntry { Item = "relic_r4", Chance = 0.5f }, new LootEntry { Item = "king_core", Min = 1, Max = 2 }, new LootEntry { Item = "great_essence_pill", Min = 2, Max = 3 }, new LootEntry { Item = "scroll_tree_of_life", Chance = 0.2f }, new LootEntry { Item = "scroll_sky_wind", Chance = 0.2f }, new LootEntry { Item = "scroll_thousand_shadows", Chance = 0.2f }, new LootEntry { Item = "scroll_jade_bone", Chance = 0.2f }, new LootEntry { Item = "scroll_mountain", Chance = 0.2f }, new LootEntry { Item = "scroll_sun_flame", Chance = 0.2f } }, GuChance = 1, GuPool = new[] { "golden_bell", "moon_sickle", "crimson_flame", "myriad_lightning", "north_wind_ice", "stone_giant", "ten_thousand_li" }, GuMinQuality = 1 },
        };

        static LootTable King(string id, string relic, string scroll, string[] gu) => new LootTable
        {
            Id = id, StonesMin = 900, StonesMax = 1300,
            Items = new[] { new LootEntry { Item = relic }, new LootEntry { Item = "heart_of_qingmao", Chance = 0.6f }, new LootEntry { Item = "king_core", Min = 2, Max = 3 }, new LootEntry { Item = scroll, Chance = 0.5f } },
            GuChance = 1, GuPool = gu, GuMinQuality = 2
        };

        // ------------------------------------------------------------------ Where they roam

        static SpawnDef S(string id, string enemy, int count, string place, float bearing, float share, float radius = 22, float respawn = 300) =>
            new SpawnDef { Id = id, Enemy = enemy, Count = count, At = P(place, bearing, share), Radius = radius, Respawn = respawn };

        public static readonly SpawnDef[] Spawns =
        {
            S("frost_wolves_a", "frost_wolf", 4, "frost_range", 60, 1.6f), S("frost_wolves_b", "frost_wolf", 3, "frost_range", 220, 1.8f), S("ice_bears", "ice_bear", 2, "frost_range", 300, 1.4f),
            S("rocs_a", "thunder_roc", 3, "thunder_plateau", 30, 1.5f), S("rocs_b", "thunder_roc", 2, "thunder_plateau", 200, 1.7f), S("storm_masters", "storm_master", 2, "thunder_plateau", 120, 1.3f),
            S("pythons_a", "jade_python", 2, "bamboo_sea", 80, 1.4f), S("pythons_b", "jade_python", 2, "bamboo_sea", 250, 1.6f), S("bamboo_spiders", "bamboo_spider", 4, "bamboo_sea", 170, 1.5f),
            S("bone_soldiers_a", "bone_soldier", 4, "old_battlefield", 40, 1.3f), S("bone_soldiers_b", "bone_soldier", 4, "old_battlefield", 200, 1.5f), S("bone_masters", "bone_master", 2, "old_battlefield", 300, 1.2f),
            S("fire_apes_a", "fire_ape", 3, "red_canyon", 90, 1.4f), S("fire_apes_b", "fire_ape", 2, "red_canyon", 270, 1.6f), S("flame_masters", "flame_master", 2, "red_canyon", 180, 1.3f),
            S("lake_serpents_a", "lake_serpent", 3, "jade_lake", 20, 1.5f), S("lake_serpents_b", "lake_serpent", 2, "jade_lake", 160, 1.7f), S("lake_hermits", "lake_hermit", 2, "jade_lake", 280, 1.3f),
        };

        static EncounterDef E(string id, string name, string place, string shout, params (string enemy, int count)[] wave) =>
            new EncounterDef { Id = id, Name = name, At = P(place, 0, 0.1f), Radius = 46, Needs = "", Waves = new[] { new WaveDef { Shout = shout, Enemies = wave } } };

        public static readonly EncounterDef[] Encounters =
        {
            E("frost_emperor", "Император снежных волков", "frost_range", "Ветер замолкает. Снег поднимается на четыре лапы.", ("frost_emperor", 1)),
            E("roc_king", "Царь громовых рухов", "thunder_plateau", "Небо над плато темнеет крыльями.", ("roc_king", 1)),
            E("python_queen", "Царица нефритовых удавов", "bamboo_sea", "Бамбук расступается. То, что ползёт, шире дороги.", ("python_queen", 1)),
            E("bone_general", "Костяной генерал", "old_battlefield", "Кости на поле встают в строй. Над строем — генерал.", ("bone_soldier", 4), ("bone_general", 1)),
            E("ape_king", "Багровый король обезьян", "red_canyon", "С края каньона прыгает пламя размером с дом.", ("ape_king", 1)),
            E("lake_dragon", "Нефритовый змей-дракон", "jade_lake", "Озеро поднимается столбом и смотрит на вас.", ("lake_dragon", 1)),
        };

        // ------------------------------------------------------------------ Lanterns, caches, herbs

        static ThingDef Lantern(string place, string name) => new ThingDef { Id = "lantern_" + place, Name = name, Kind = ThingKind.Lantern, At = P(place, 180, 1.05f), Model = "prop_moon_lantern_post" };

        static ThingDef Cache(string place, string king, string name) =>
            new ThingDef { Id = "cache_" + place, Name = name, Kind = ThingKind.Chest, Content = "cache_wilds", At = P(place, 0, 0.35f), Needs = king + "_dead", Locked = "Хозяин этих мест не даст к нему подойти." };

        static ThingDef Herb(string place, string item, string name, float bearing) =>
            new ThingDef { Id = "herb_" + place + "_" + (int)bearing, Name = name, Kind = ThingKind.Herb, Content = item, At = P(place, bearing, 1.2f), Regrow = 600 };

        public static readonly ThingDef[] Things =
        {
            Lantern("frost_range", "Фонарь Морозного хребта"), Lantern("thunder_plateau", "Фонарь Громового плато"), Lantern("bamboo_sea", "Фонарь Бамбукового моря"),
            Lantern("old_battlefield", "Фонарь Древнего поля"), Lantern("red_canyon", "Фонарь Красного каньона"), Lantern("jade_lake", "Фонарь Нефритового озера"),
            Cache("frost_range", "frost_emperor", "Логово императора"), Cache("thunder_plateau", "roc_king", "Гнездо царя рухов"), Cache("bamboo_sea", "python_queen", "Кладка царицы"),
            Cache("old_battlefield", "bone_general", "Шатёр генерала"), Cache("red_canyon", "ape_king", "Трон короля обезьян"), Cache("jade_lake", "lake_dragon", "Жемчужина озера"),
            Herb("frost_range", "frost_lotus", "Морозный лотос", 120), Herb("frost_range", "frost_lotus", "Морозный лотос", 330),
            Herb("thunder_plateau", "spark_moss", "Искристый мох", 90), Herb("thunder_plateau", "spark_moss", "Искристый мох", 260),
            Herb("bamboo_sea", "moon_orchid", "Лунная орхидея", 40), Herb("bamboo_sea", "ginseng", "Женьшень", 300),
            Herb("old_battlefield", "night_moth", "Ночной мотылёк", 140), Herb("red_canyon", "ember_coal", "Тлеющий уголь", 30), Herb("red_canyon", "blood_vine", "Кровавая лоза", 210),
            Herb("jade_lake", "dew_gourd", "Росяная тыква", 60), Herb("jade_lake", "moon_orchid", "Лунная орхидея", 230),
        };

        // ------------------------------------------------------------------ The captain's errand

        static Line L(string who, string text) => new Line(who, text);

        public static readonly QuestDef[] Quests =
        {
            new QuestDef
            {
                Id = "q_wilds", Title = "Шесть хозяев", Chapter = 5, Giver = "trade_captain", Needs = "tomb_cleared", Clan = "wanderers",
                Summary = "Капитан торгового поста хочет знать, что за горой. Шесть земель, шесть хозяев — и ни одной карты.",
                Offer = new[] { L("trade_captain", "Ты прошёл гробницу. Значит, тебе тесно на этой горе."), L("trade_captain", "За перевалами шесть земель. У каждой свой хозяин. Принеси мне карту — и голову хотя бы троих.") },
                Steps = new[]
                {
                    new StepDef { Kind = StepKind.Reach, Target = "frost_range", Text = "Дойдите до Морозного хребта (север)", Marker = "frost_range" },
                    new StepDef { Kind = StepKind.Reach, Target = "thunder_plateau", Text = "Дойдите до Громового плато (северо-восток)", Marker = "thunder_plateau" },
                    new StepDef { Kind = StepKind.Reach, Target = "bamboo_sea", Text = "Дойдите до Бамбукового моря (восток)", Marker = "bamboo_sea" },
                    new StepDef { Kind = StepKind.Reach, Target = "old_battlefield", Text = "Дойдите до Древнего поля битвы (юго-восток)", Marker = "old_battlefield" },
                    new StepDef { Kind = StepKind.Reach, Target = "red_canyon", Text = "Дойдите до Красного каньона (юго-запад)", Marker = "red_canyon" },
                    new StepDef { Kind = StepKind.Reach, Target = "jade_lake", Text = "Дойдите до Нефритового озера (запад)", Marker = "jade_lake" },
                    new StepDef
                    {
                        Kind = StepKind.Talk, Target = "trade_captain", Text = "Вернитесь к капитану с картой",
                        Lines = new[] { L("trade_captain", "Шесть земель. Ты нарисовал их кровью, не тушью — по рукам видно."), L("trade_captain", "Теперь хозяева. Любые трое. Они сидят на том, что гора копила тысячу лет.") }
                    },
                    new StepDef { Kind = StepKind.Kill, Target = "frost_emperor|roc_king|python_queen|bone_general|ape_king|lake_dragon", Count = 3, Text = "Одолейте троих хозяев диких земель", Marker = "frost_range" },
                },
                Reward = new Reward { Insight = 6000, Stones = 2000, Items = new[] { ("relic_r4", 1), ("heart_of_qingmao", 1) }, Reputation = new[] { ("wanderers", 30) }, Flags = new[] { "wilds_mapped" } }
            },
        };
    }
}
