using System.Collections.Generic;

namespace GuWorld.Core
{
    /// <summary>
    /// The valley under Qing Mao: its places, who stands where, what can be used, where the
    /// beasts are, and what is built. X east, Z north, metres from the village well.
    /// </summary>
    public static class Valley
    {
        /// <summary>Half the valley's side: the world is a square from −Half to +Half.</summary>
        public const float Half = 480;
        public const float VillageLevel = 20;

        public static readonly PlaceDef[] Places =
        {
            new PlaceDef { Id = "village", Name = "Деревня Гу Юэ", At = new MapPoint(0, 0), Radius = 84, Level = VillageLevel, Colour = 0xE8C98A },
            new PlaceDef { Id = "north_gate", Name = "Северные ворота", At = new MapPoint(0, 112), Radius = 34, Level = 17, Colour = 0xE8C98A },
            new PlaceDef { Id = "yard", Name = "Двор академии", At = new MapPoint(-56, 6), Radius = 14, Level = VillageLevel, Colour = 0xE8C98A },
            new PlaceDef { Id = "grove", Name = "Бамбуковая роща", At = new MapPoint(-195, 30), Radius = 75, Colour = 0x9FEFC4 },
            new PlaceDef { Id = "forest_edge", Name = "Опушка леса", At = new MapPoint(10, 160), Radius = 34, Colour = 0xA0D2FF },
            new PlaceDef { Id = "forest", Name = "Лес Цинмао", At = new MapPoint(5, 240), Radius = 100, Colour = 0x7E9093 },
            new PlaceDef { Id = "knoll", Name = "Волчий холм", At = new MapPoint(70, 300), Radius = 24, Level = 34, Colour = 0xF08A74 },
            new PlaceDef { Id = "river", Name = "Речной брод", At = new MapPoint(236, 70), Radius = 36, Colour = 0x6FB8FF },
            new PlaceDef { Id = "waterfall", Name = "Водопад", At = new MapPoint(268, 128), Radius = 18, Level = 12, Blend = 5, Colour = 0x6FB8FF },
            new PlaceDef { Id = "hermit_cave", Name = "Пещера Отшельника", At = new MapPoint(395, 398), Radius = 46, Level = 6, Cave = true, Colour = 0x7FE0C8 },
            new PlaceDef { Id = "camp", Name = "Разбойничий стан", At = new MapPoint(40, -268), Radius = 52, Level = 9, Colour = 0xF08A74 },
            new PlaceDef { Id = "border_stone", Name = "Пограничный камень", At = new MapPoint(236, -178), Radius = 28, Level = 14, Colour = 0xE8C98A },
            new PlaceDef { Id = "ridge", Name = "Волчий гребень", At = new MapPoint(0, 352), Radius = 62, Colour = 0xC0A8FF },
            new PlaceDef { Id = "den", Name = "Логово Громовой Короны", At = new MapPoint(0, 404), Radius = 26, Level = 58, Colour = 0xD0B8FF },
            new PlaceDef { Id = "blood_cave", Name = "Кровавая пещера", At = new MapPoint(-395, 398), Radius = 48, Level = 4, Cave = true, Colour = 0xF05A4A },
        };

        public static readonly NpcDef[] Npcs =
        {
            new NpcDef
            {
                Id = "elder", Name = "Гу Юэ Чанмин", Title = "Старейшина академии", Look = "elder", At = new MapPoint(0, 36), Facing = 180, GoneAfter = "elder_gone",
                Barks = new[] { "Талант — это глубина колодца. Но воду из него носят руками.", "Клан помнит тех, кто полезен. Будь полезен, Лань.", "Источник мелеет. Старики говорят — к долгой зиме." }
            },
            new NpcDef
            {
                Id = "keeper", Name = "Гу Юэ Синь", Title = "Смотрительница питомника", Look = "keeper", At = new MapPoint(-40, -22), Facing = 90,
                Barks = new[] { "Гу — как дети: накорми, не обижай, и не давай кусаться.", "Дикого Гу сперва очисти. Иначе он съест твою эссенцию и уползёт." }
            },
            new NpcDef
            {
                Id = "instructor", Name = "Гу Юэ Тэ", Title = "Наставник боя", Look = "guard", At = new MapPoint(-46, 8), Facing = 250,
                Barks = new[] { "Ладонь копит эссенцию, Гу её тратит. Кто только бросает клинки — тот скоро пуст.", "Видишь красный замах — уходи перекатом. Потом бей." }
            },
            new NpcDef
            {
                Id = "shopkeeper", Name = "Баошань", Title = "Лавочник", Look = "merchant", At = new MapPoint(31, 6), Facing = 270, Service = "shop",
                Barks = new[] { "Всё честно, всё по весу. Камни вперёд." }
            },
            new NpcDef
            {
                Id = "hua", Name = "Тётушка Хуа", Title = "Лапшичная", Look = "cook", At = new MapPoint(28, -26), Facing = 270,
                Barks = new[] { "Худой какой. Сядь, поешь. Потом спасёшь мир.", "Лапша без побегов бамбука — это суп с тестом." }
            },
            new NpcDef
            {
                Id = "herbalist", Name = "Гу Юэ Яо", Title = "Лекарка", Look = "herbalist", At = new MapPoint(37, 32), Facing = 270,
                Barks = new[] { "Пилюли у Баошаня. Он берёт у меня и продаёт вдвое.", "Орхидеи у опушки рви с корнем — тогда вырастут снова." }
            },
            new NpcDef
            {
                Id = "smith", Name = "Гу Юэ Ган", Title = "Кузнец", Look = "smith", At = new MapPoint(16, -43), Facing = 0,
                Barks = new[] { "Гу-мастеру меч ни к чему. А вот страже — клыки на наконечники." }
            },
            new NpcDef
            {
                Id = "gambler", Name = "Старый Цянь", Title = "Игорный шатёр", Look = "gambler", At = new MapPoint(-20, -43), Facing = 0, Service = "gamble",
                Barks = new[] { "В каждом камне спит Гу. Или не спит. Или не Гу." }
            },
            new NpcDef
            {
                Id = "scribe", Name = "Гу Юэ Вэнь", Title = "Писарь клана", Look = "scribe", At = new MapPoint(-9, -30), Facing = 20, Service = "board",
                Barks = new[] { "Поручения — на доске. Плата — по исполнении." }
            },
            new NpcDef
            {
                Id = "captain", Name = "Гу Юэ Цзян", Title = "Начальник стражи", Look = "guard", At = new MapPoint(7, 84), Facing = 180,
                Barks = new[] { "За ворота после заката — только с Гу в апертуре.", "Волки в этом году наглые. Подходят к самой стене." }
            },
            new NpcDef
            {
                Id = "widow", Name = "Гу Юэ Лин", Title = "Жена дозорного", Look = "villager", At = new MapPoint(45, -42), Facing = 300,
                Barks = new[] { "Тянь ушёл к реке три дня назад. Он всегда возвращался к ужину." }
            },
            new NpcDef
            {
                Id = "judge", Name = "Те Мяо", Title = "Распорядитель турнира", Look = "judge", At = new MapPoint(228, -168), Facing = 120,
                Barks = new[] { "Три клана, один камень, никаких смертей. Таков уговор." }
            },
        };

        public static readonly ThingDef[] Things =
        {
            new ThingDef { Id = "lantern_village", Name = "Лунный фонарь деревни", Kind = ThingKind.Lantern, At = new MapPoint(6, -14), Model = "prop_moon_lantern_post" },
            new ThingDef { Id = "lantern_grove", Name = "Лунный фонарь рощи", Kind = ThingKind.Lantern, At = new MapPoint(-128, 27), Model = "prop_moon_lantern_post" },
            new ThingDef { Id = "lantern_forest", Name = "Лунный фонарь опушки", Kind = ThingKind.Lantern, At = new MapPoint(18, 142), Model = "prop_moon_lantern_post" },
            new ThingDef { Id = "lantern_river", Name = "Лунный фонарь брода", Kind = ThingKind.Lantern, At = new MapPoint(214, 79), Model = "prop_moon_lantern_post" },
            new ThingDef { Id = "lantern_camp", Name = "Лунный фонарь тракта", Kind = ThingKind.Lantern, At = new MapPoint(31, -198), Model = "prop_moon_lantern_post" },
            new ThingDef { Id = "lantern_stone", Name = "Лунный фонарь у камня", Kind = ThingKind.Lantern, At = new MapPoint(214, -160), Model = "prop_moon_lantern_post" },
            new ThingDef { Id = "lantern_ridge", Name = "Лунный фонарь гребня", Kind = ThingKind.Lantern, At = new MapPoint(17, 300), Model = "prop_moon_lantern_post" },
            new ThingDef { Id = "lantern_hermit", Name = "Лунный фонарь пещеры", Kind = ThingKind.Lantern, At = new MapPoint(401, 366), Model = "prop_moon_lantern_post" },
            new ThingDef { Id = "lantern_blood", Name = "Погасший фонарь", Kind = ThingKind.Lantern, At = new MapPoint(-401, 364), Model = "prop_moon_lantern_post" },

            new ThingDef { Id = "spring", Name = "Духовный источник", Kind = ThingKind.Spring, At = new MapPoint(-15, 24) },
            new ThingDef { Id = "altar", Name = "Алтарь слияния", Kind = ThingKind.FusionAltar, At = new MapPoint(15, 24), Model = "prop_bronze_ding" },

            new ThingDef { Id = "shoot_1", Name = "Побег бамбука", Kind = ThingKind.Herb, At = new MapPoint(-150, 44), Content = "bamboo_shoot", Regrow = 240 },
            new ThingDef { Id = "shoot_2", Name = "Побег бамбука", Kind = ThingKind.Herb, At = new MapPoint(-172, 12), Content = "bamboo_shoot", Regrow = 240 },
            new ThingDef { Id = "shoot_3", Name = "Побег бамбука", Kind = ThingKind.Herb, At = new MapPoint(-196, 58), Content = "bamboo_shoot", Regrow = 240 },
            new ThingDef { Id = "shoot_4", Name = "Побег бамбука", Kind = ThingKind.Herb, At = new MapPoint(-214, 22), Content = "bamboo_shoot", Regrow = 240 },
            new ThingDef { Id = "shoot_5", Name = "Побег бамбука", Kind = ThingKind.Herb, At = new MapPoint(-232, 48), Content = "bamboo_shoot", Regrow = 240 },
            new ThingDef { Id = "shoot_6", Name = "Побег бамбука", Kind = ThingKind.Herb, At = new MapPoint(-186, -14), Content = "bamboo_shoot", Regrow = 240 },

            new ThingDef { Id = "orchid_1", Name = "Лунная орхидея", Kind = ThingKind.Herb, At = new MapPoint(-14, 152), Content = "moon_orchid", Regrow = 240 },
            new ThingDef { Id = "orchid_2", Name = "Лунная орхидея", Kind = ThingKind.Herb, At = new MapPoint(6, 168), Content = "moon_orchid", Regrow = 240 },
            new ThingDef { Id = "orchid_3", Name = "Лунная орхидея", Kind = ThingKind.Herb, At = new MapPoint(30, 158), Content = "moon_orchid", Regrow = 240 },
            new ThingDef { Id = "orchid_4", Name = "Лунная орхидея", Kind = ThingKind.Herb, At = new MapPoint(-30, 176), Content = "moon_orchid", Regrow = 240 },
            new ThingDef { Id = "orchid_5", Name = "Лунная орхидея", Kind = ThingKind.Herb, At = new MapPoint(40, 182), Content = "moon_orchid", Regrow = 240 },
            new ThingDef { Id = "orchid_6", Name = "Лунная орхидея", Kind = ThingKind.Herb, At = new MapPoint(14, 190), Content = "moon_orchid", Regrow = 240 },
            new ThingDef { Id = "orchid_7", Name = "Лунная орхидея", Kind = ThingKind.Herb, At = new MapPoint(-8, 204), Content = "moon_orchid", Regrow = 240 },
            new ThingDef { Id = "orchid_8", Name = "Лунная орхидея", Kind = ThingKind.Herb, At = new MapPoint(52, 204), Content = "moon_orchid", Regrow = 240 },

            new ThingDef { Id = "chest_grove", Name = "Тайник в роще", Kind = ThingKind.Chest, At = new MapPoint(-246, 66), Content = "chest_grove" },
            new ThingDef { Id = "chest_forest", Name = "Тайник охотника", Kind = ThingKind.Chest, At = new MapPoint(-74, 272), Content = "chest_forest" },
            new ThingDef { Id = "chest_river", Name = "Сундук на отмели", Kind = ThingKind.Chest, At = new MapPoint(262, 16), Content = "chest_river" },
            new ThingDef { Id = "chest_hermit", Name = "Ларец Отшельника", Kind = ThingKind.Chest, At = new MapPoint(416, 418), Content = "chest_hermit" },
            new ThingDef { Id = "chest_camp", Name = "Сундук атамана", Kind = ThingKind.Chest, At = new MapPoint(52, -306), Content = "chest_camp", Needs = "crooked_fang_dead", Locked = "Сундук окован. Ключ, должно быть, у атамана." },
            new ThingDef { Id = "chest_ridge", Name = "Тайник на гребне", Kind = ThingKind.Chest, At = new MapPoint(-52, 388), Content = "chest_ridge" },
            new ThingDef { Id = "chest_blood", Name = "Ларец старейшины", Kind = ThingKind.Chest, At = new MapPoint(-424, 420), Content = "chest_blood" },

            new ThingDef { Id = "scout_body", Name = "Тело дозорного", Kind = ThingKind.QuestObject, At = new MapPoint(244, 104) },
            new ThingDef
            {
                Id = "door_hermit", Name = "Проход за водопадом", Kind = ThingKind.Door, At = new MapPoint(272, 140), To = new MapPoint(395, 362), Needs = "hermit_open",
                Locked = "За водой — камень, а на камне лунная печать. Она не открывается тому, кто слаб."
            },
            new ThingDef { Id = "door_hermit_out", Name = "Выход к водопаду", Kind = ThingKind.Door, At = new MapPoint(389, 358), To = new MapPoint(266, 122) },
            new ThingDef { Id = "hermit_remains", Name = "Останки Отшельника", Kind = ThingKind.QuestObject, At = new MapPoint(395, 432), Needs = "stone_guard_dead", Locked = "Каменный страж ещё стоит.", Model = "prop_ancestral_altar" },

            new ThingDef { Id = "blood_altar_1", Name = "Кровавый алтарь", Kind = ThingKind.QuestObject, At = new MapPoint(-66, 348), Model = "prop_bronze_ding" },
            new ThingDef { Id = "blood_altar_2", Name = "Кровавый алтарь", Kind = ThingKind.QuestObject, At = new MapPoint(62, 356), Model = "prop_bronze_ding" },
            new ThingDef { Id = "blood_altar_3", Name = "Кровавый алтарь", Kind = ThingKind.QuestObject, At = new MapPoint(-6, 334), Model = "prop_bronze_ding" },
            new ThingDef { Id = "den_lure", Name = "Приманка в логове", Kind = ThingKind.QuestObject, At = new MapPoint(0, 422), Needs = "thunder_crown_wolf_dead", Locked = "Громовая Корона ещё жива." },

            new ThingDef
            {
                Id = "door_blood", Name = "Плита за залом предков", Kind = ThingKind.Door, At = new MapPoint(0, 66), To = new MapPoint(-395, 360), Needs = "blood_open",
                Locked = "Каменная плита с лунной печатью. Она заперта изнутри."
            },
            new ThingDef { Id = "door_blood_out", Name = "Лестница наверх", Kind = ThingKind.Door, At = new MapPoint(-389, 356), To = new MapPoint(0, 72) },
            new ThingDef { Id = "blood_gu", Name = "Гу Кровавой луны", Kind = ThingKind.QuestObject, At = new MapPoint(-395, 438), Needs = "elder_dead", Locked = "Старейшина стоит между вами и чашей.", Model = "prop_bronze_ding" },
        };

        public static readonly SpawnDef[] Spawns =
        {
            new SpawnDef { Id = "dummies", Enemy = "dummy", Count = 3, At = new MapPoint(-62, 4), Radius = 4, Respawn = 12 },

            new SpawnDef { Id = "grove_a", Enemy = "fox", Count = 4, At = new MapPoint(-165, 40), Radius = 22, Respawn = 120 },
            new SpawnDef { Id = "grove_b", Enemy = "fox", Count = 4, At = new MapPoint(-212, 6), Radius = 22, Respawn = 120 },
            new SpawnDef { Id = "grove_c", Enemy = "fox", Count = 3, At = new MapPoint(-222, 70), Radius = 18, Respawn = 120 },

            new SpawnDef { Id = "forest_a", Enemy = "wolf", Count = 3, At = new MapPoint(-22, 196), Radius = 18, Respawn = 150 },
            new SpawnDef { Id = "forest_b", Enemy = "wolf", Count = 4, At = new MapPoint(46, 226), Radius = 20, Respawn = 150 },
            new SpawnDef { Id = "forest_c", Enemy = "wolf", Count = 4, At = new MapPoint(-52, 254), Radius = 22, Respawn = 150 },
            new SpawnDef { Id = "forest_d", Enemy = "wolf", Count = 3, At = new MapPoint(14, 276), Radius = 18, Respawn = 150 },

            new SpawnDef { Id = "river_a", Enemy = "fox", Count = 3, At = new MapPoint(206, 26), Radius = 18, Respawn = 150 },
            new SpawnDef { Id = "river_b", Enemy = "wolf", Count = 3, At = new MapPoint(250, 176), Radius = 16, Respawn = 150 },

            new SpawnDef { Id = "hermit_a", Enemy = "cave_gu", Count = 5, At = new MapPoint(395, 390), Radius = 7, Respawn = 240 },
            new SpawnDef { Id = "hermit_b", Enemy = "cave_gu", Count = 6, At = new MapPoint(380, 404), Radius = 9, Respawn = 240 },
            new SpawnDef { Id = "hermit_c", Enemy = "cave_gu", Count = 4, At = new MapPoint(412, 402), Radius = 8, Respawn = 240 },

            new SpawnDef { Id = "camp_a", Enemy = "bandit", Count = 3, At = new MapPoint(28, -240), Radius = 10, Respawn = 240 },
            new SpawnDef { Id = "camp_b", Enemy = "bandit_thrower", Count = 2, At = new MapPoint(58, -252), Radius = 9, Respawn = 240 },
            new SpawnDef { Id = "camp_c", Enemy = "bandit", Count = 3, At = new MapPoint(24, -280), Radius = 10, Respawn = 240 },
            new SpawnDef { Id = "camp_d", Enemy = "bandit_thrower", Count = 2, At = new MapPoint(66, -284), Radius = 9, Respawn = 240 },

            new SpawnDef { Id = "ridge_a", Enemy = "storm_wolf", Count = 3, At = new MapPoint(-64, 352), Radius = 12, Respawn = 200 },
            new SpawnDef { Id = "ridge_b", Enemy = "storm_wolf", Count = 3, At = new MapPoint(60, 360), Radius = 12, Respawn = 200 },
            new SpawnDef { Id = "ridge_c", Enemy = "wolf", Count = 4, At = new MapPoint(-4, 338), Radius = 14, Respawn = 200 },
            new SpawnDef { Id = "ridge_d", Enemy = "storm_wolf", Count = 2, At = new MapPoint(-44, 372), Radius = 10, Respawn = 200 },

            new SpawnDef { Id = "blood_a", Enemy = "blood_puppet", Count = 3, At = new MapPoint(-390, 392), Radius = 6, Respawn = 300 },
            new SpawnDef { Id = "blood_b", Enemy = "blood_wolf", Count = 4, At = new MapPoint(-414, 400), Radius = 10, Respawn = 300 },
            new SpawnDef { Id = "blood_c", Enemy = "blood_puppet", Count = 3, At = new MapPoint(-376, 408), Radius = 9, Respawn = 300 },
        };

        public static readonly EncounterDef[] Encounters =
        {
            new EncounterDef { Id = "ragged_ear", Name = "Рваное Ухо", At = new MapPoint(70, 300), Radius = 26, Waves = new[] { new WaveDef { Enemies = new[] { ("ragged_ear", 1), ("wolf", 2) }, Shout = "Вожак выходит из-за камней." } } },
            new EncounterDef { Id = "stone_guard", Name = "Каменный страж", At = new MapPoint(395, 420), Radius = 20, Waves = new[] { new WaveDef { Enemies = new[] { ("stone_guard", 1) }, Shout = "Статуя у останков открывает глаза." } } },
            new EncounterDef { Id = "crooked_fang", Name = "Атаман Кривой Клык", At = new MapPoint(40, -298), Radius = 22, Waves = new[] { new WaveDef { Enemies = new[] { ("crooked_fang", 1), ("bandit", 2) }, Shout = "— Ученик Гу Юэ? Один? Вот потеха." } } },
            new EncounterDef
            {
                Id = "tournament", Name = "Турнир трёх кланов", At = new MapPoint(236, -178), Radius = 24, Waves = new[]
                {
                    new WaveDef { Enemies = new[] { ("xiong_fighter", 1) }, Shout = "Первый бой: Сюн Ли, клан Сюн!" },
                    new WaveDef { Enemies = new[] { ("bai_fighter", 1) }, Shout = "Второй бой: Бай Нин, клан Бай!" },
                }
            },
            new EncounterDef
            {
                Id = "tide", Name = "Волчий прилив", At = new MapPoint(0, 128), Radius = 40, Waves = new[]
                {
                    new WaveDef { Enemies = new[] { ("wolf", 6) }, Shout = "Первая волна! Серые идут от опушки." },
                    new WaveDef { Enemies = new[] { ("wolf", 5), ("storm_wolf", 3) }, Shout = "Вторая волна! Молниевые за серыми." },
                    new WaveDef { Enemies = new[] { ("hundred_king", 1), ("wolf", 4) }, Shout = "Сотенный вожак! К воротам!" },
                }
            },
            new EncounterDef
            {
                Id = "den", Name = "Логово", At = new MapPoint(0, 404), Radius = 24, Waves = new[]
                {
                    new WaveDef { Enemies = new[] { ("storm_wolf", 4) }, Shout = "Стража логова." },
                    new WaveDef { Enemies = new[] { ("thunder_crown_wolf", 1) }, Shout = "Над логовом гремит. Громовая Корона встаёт." },
                }
            },
            new EncounterDef { Id = "elder", Name = "Старейшина", At = new MapPoint(-395, 426), Radius = 24, Waves = new[] { new WaveDef { Enemies = new[] { ("elder", 1) }, Shout = "— Ты дошёл. Я знал, что дойдёшь. Потому и выбрал тебя." } } },
        };

        public static readonly BuildingDef[] Buildings =
        {
            // The square and the clan's halls.
            new BuildingDef("prop_ancestral_hall", 0, 52, 180),
            new BuildingDef("prop_ancestral_altar", 0, 43, 180),
            new BuildingDef("prop_clan_stele", 0, 26, 180),
            new BuildingDef("prop_village_well", 0, 0, 0),
            new BuildingDef("prop_academy", -50, 36, 90),
            new BuildingDef("prop_academy_dorm", -26, 68, 180),
            new BuildingDef("prop_teahouse", 28, 66, 180),
            new BuildingDef("prop_gu_nursery", -52, -22, 90),
            new BuildingDef("prop_shop", 40, 6, 270),
            new BuildingDef("prop_eatery", 39, -26, 270),
            new BuildingDef("prop_herbalist", 47, 32, 270),
            new BuildingDef("prop_smithy", 16, -53, 0),
            new BuildingDef("prop_gambling_tent", -20, -56, 0),
            new BuildingDef("prop_poor_house", 54, -48, 315),

            // Houses round the square.
            new BuildingDef("kit_courtyard", -74, -2, 90),
            new BuildingDef("kit_two_storey", -64, -46, 45),
            new BuildingDef("kit_artisan", -44, -66, 0),
            new BuildingDef("kit_l_kitchen", -26, -80, 0),
            new BuildingDef("kit_shop_house", 40, -66, 0),
            new BuildingDef("kit_two_storey_stone", 68, -30, 270),
            new BuildingDef("kit_courtyard_blue", 72, 2, 270),
            new BuildingDef("kit_artisan_ochre", 68, 34, 270),
            new BuildingDef("kit_hut_mud", 56, 58, 225),
            new BuildingDef("kit_hut_mud_ochre", -74, 40, 90),
            new BuildingDef("kit_shop_house_plaster", -66, 62, 135),

            // The square's small things.
            new BuildingDef("prop_market_goods", 12, -10, 0),
            new BuildingDef("prop_market_goods", -12, -10, 180),
            new BuildingDef("prop_barrels", 30, 14, 0),
            new BuildingDef("prop_bench_wood", 8, -6, 135),
            new BuildingDef("prop_bench_wood", -7, 7, 315),
            new BuildingDef("prop_go_table", 22, 54, 0),
            new BuildingDef("prop_weapon_rack", -66, 12, 90),
            new BuildingDef("prop_weapon_rack", -66, 0, 90),
            new BuildingDef("prop_firewood_stack", 26, -50, 0),
            new BuildingDef("prop_drying_rack", 48, 44, 0),
            new BuildingDef("prop_bonsai_pine", 6, 40, 0, 1.4f),
            new BuildingDef("prop_bonsai_pine", -6, 40, 0, 1.4f),
            new BuildingDef("prop_lotus_vat", -19, 28, 0),
            new BuildingDef("prop_lotus_vat", -11, 29, 0),
            new BuildingDef("prop_lotus_vat", -20, 20, 0),
            new BuildingDef("prop_moon_gate", 0, -80, 0),
            new BuildingDef("tree_plum_8m", -28, 14, 0),
            new BuildingDef("tree_plum_8m", 24, 24, 80),
            new BuildingDef("tree_plum_8m", -30, -40, 200),
            new BuildingDef("tree_plum_8m", 58, 14, 140),

            // The north gate and its wall.
            new BuildingDef("fort_battle_gate", 0, 96, 0),
            new BuildingDef("fort_wall_segment", -11, 96, 0),
            new BuildingDef("fort_wall_segment", -18, 96, 0),
            new BuildingDef("fort_wall_segment", -25, 96, 0),
            new BuildingDef("fort_wall_segment", 11, 96, 0),
            new BuildingDef("fort_wall_segment", 18, 96, 0),
            new BuildingDef("fort_wall_segment", 25, 96, 0),
            new BuildingDef("fort_watchtower", -34, 96, 0),
            new BuildingDef("fort_watchtower", 34, 96, 0),

            // The bandits' camp.
            new BuildingDef("prop_gambling_tent", 40, -312, 0),
            new BuildingDef("kit_stilt_plank", 14, -262, 90),
            new BuildingDef("kit_stilt_bamboo", 68, -268, 270),
            new BuildingDef("prop_firewood_stack", 44, -262, 30),
            new BuildingDef("prop_barrels", 30, -292, 0),
            new BuildingDef("prop_drying_rack", 54, -240, 60),
            new BuildingDef("prop_bamboo_fence", 22, -224, 0),
            new BuildingDef("prop_bamboo_fence", 30, -224, 0),
            new BuildingDef("prop_bamboo_fence", 52, -224, 0),
            new BuildingDef("prop_bamboo_fence", 60, -224, 0),
            new BuildingDef("prop_weapon_rack", 50, -290, 300),

            // The border stone.
            new BuildingDef("prop_clan_stele", 236, -152, 180, 1.4f),
            new BuildingDef("prop_bench_stone", 214, -178, 90),
            new BuildingDef("prop_bench_stone", 258, -178, 270),
            new BuildingDef("prop_weapon_rack", 222, -156, 0),
            new BuildingDef("prop_weapon_rack", 250, -156, 0),

            // The hermit's cave.
            new BuildingDef("prop_writing_desk", 410, 430, 220),
            new BuildingDef("prop_gu_jar_shelf", 380, 432, 150),
            new BuildingDef("prop_luohan_bed", 418, 408, 270),
            new BuildingDef("prop_curio_shelf", 372, 416, 110),
            new BuildingDef("prop_folding_screen", 402, 436, 190),
            new BuildingDef("prop_lotus_vat", 388, 428, 0),

            // The blood cave.
            new BuildingDef("prop_ancestral_altar", -395, 442, 180, 1.3f),
            new BuildingDef("prop_gu_jar_shelf", -416, 432, 140),
            new BuildingDef("prop_gu_jar_shelf", -374, 432, 220),
            new BuildingDef("prop_medicine_cabinet", -428, 404, 90),
            new BuildingDef("prop_canopy_bed", -366, 392, 270),
        };

        public static readonly ScatterDef[] Scatter =
        {
            new ScatterDef("prop_bamboo_grove", -195, 30, 78, 70, 0.9f, 1.5f),
            new ScatterDef("prop_bamboo_grove", -110, -40, 30, 10, 0.8f, 1.2f),
            new ScatterDef("tree_pine_40m", 5, 240, 105, 46, 0.5f, 0.85f, 6),
            new ScatterDef("tree_pine_40m", -150, 190, 70, 18, 0.5f, 0.8f, 6),
            new ScatterDef("tree_pine_40m", 150, 230, 70, 16, 0.5f, 0.8f, 6),
            new ScatterDef("tree_camphor_70m", 0, 250, 110, 7, 0.45f, 0.6f, 10),
            new ScatterDef("tree_ancient_100m", -90, 300, 10, 1, 0.5f, 0.5f, 10),
            new ScatterDef("tree_ancient_100m", 150, -60, 10, 1, 0.45f, 0.45f, 10),
            new ScatterDef("tree_deadwood_25m", 0, 370, 80, 16, 0.6f, 1f, 5),
            new ScatterDef("tree_deadwood_25m", 40, -268, 60, 6, 0.6f, 0.9f, 5),
            new ScatterDef("tree_plum_8m", 160, 60, 60, 12, 0.8f, 1.3f, 4),
            new ScatterDef("tree_plum_8m", 120, -150, 80, 14, 0.8f, 1.3f, 4),
            new ScatterDef("tree_pine_40m", -160, -180, 110, 22, 0.45f, 0.75f, 6),
            new ScatterDef("tree_pine_40m", 300, -40, 80, 12, 0.45f, 0.75f, 6),
            new ScatterDef("tree_camphor_70m", -230, -240, 60, 3, 0.4f, 0.55f, 10),
        };

        /// <summary>The river, from the heights in the north-east down past the ford to the south-east.</summary>
        public static readonly MapPoint[] River =
        {
            new MapPoint(330, 470), new MapPoint(300, 300), new MapPoint(276, 150), new MapPoint(262, 120), new MapPoint(244, 66),
            new MapPoint(238, 0), new MapPoint(262, -90), new MapPoint(296, -200), new MapPoint(330, -330), new MapPoint(350, -470),
        };

        /// <summary>The water's level above the fall (on the north-east plateau) and below it.</summary>
        public const float WaterfallTop = 28;
        public const float WaterfallFoot = 11.4f;
        /// <summary>The plateau's southern edge (the cliff the river falls from) and its western edge.</summary>
        public const float PlateauZ = 152;
        public const float PlateauX = 212;
        public const float PlateauRise = 17;

        /// <summary>The roads: beaten earth from the village to everywhere the story goes.</summary>
        public static readonly MapPoint[][] Roads =
        {
            new[] { new MapPoint(0, 0), new MapPoint(10, 12), new MapPoint(11, 44), new MapPoint(12, 74), new MapPoint(0, 92), new MapPoint(0, 100), new MapPoint(6, 130), new MapPoint(12, 160), new MapPoint(8, 230), new MapPoint(30, 280), new MapPoint(8, 304), new MapPoint(0, 352) },
            new[] { new MapPoint(30, 280), new MapPoint(70, 300) },
            new[] { new MapPoint(0, 0), new MapPoint(-84, 12), new MapPoint(-128, 20), new MapPoint(-195, 30) },
            new[] { new MapPoint(0, 0), new MapPoint(40, -8), new MapPoint(72, -15), new MapPoint(100, -6), new MapPoint(150, 50), new MapPoint(214, 72), new MapPoint(244, 66), new MapPoint(258, 100), new MapPoint(268, 124) },
            new[] { new MapPoint(0, 0), new MapPoint(0, -84), new MapPoint(14, -150), new MapPoint(24, -196), new MapPoint(40, -240) },
            // The south road on, through the town, to where the camp now is.
            new[] { new MapPoint(40, -240), new MapPoint(48, -320), new MapPoint(52, -400), new MapPoint(50, -455) },
            new[] { new MapPoint(24, -196), new MapPoint(110, -190), new MapPoint(180, -170), new MapPoint(214, -164), new MapPoint(236, -178) },
        };

        /// <summary>
        /// Places moved from where the valley's plan first had them (the village grew into a town and
        /// pushed the bandits' camp down the south road): each takes along everything within its reach.
        /// </summary>
        static readonly (string place, MapPoint to)[] Moves =
        {
            ("camp", new MapPoint(50, -500)),
        };

        static Valley()
        {
            foreach (var (id, to) in Moves)
            {
                var place = Place(id);

                if (place == null)
                {
                    continue;
                }

                var from = place.At;
                var reach = place.Radius + 40;
                var dx = to.X - from.X;
                var dz = to.Z - from.Z;

                MapPoint Move(MapPoint p) => p.DistanceTo(from) <= reach ? new MapPoint(p.X + dx, p.Z + dz) : p;

                foreach (var npc in Npcs) npc.At = Move(npc.At);
                foreach (var thing in Things) thing.At = Move(thing.At);
                foreach (var spawn in Spawns) spawn.At = Move(spawn.At);
                foreach (var encounter in Encounters) encounter.At = Move(encounter.At);
                foreach (var building in Buildings) building.At = Move(building.At);
                foreach (var patch in Scatter) patch.At = Move(patch.At);
                place.At = to;
                // The camp's ground is levelled where it now stands, to the land's own height.
                place.Level = float.NaN;
                place.Flatten = true;
            }
        }

        public static PlaceDef Place(string id) => Find(Places, p => p.Id == id);
        public static NpcDef Npc(string id) => Find(Npcs, n => n.Id == id);
        public static ThingDef Thing(string id) => Find(Things, t => t.Id == id);
        public static EncounterDef Encounter(string id) => Find(Encounters, e => e.Id == id);

        static T Find<T>(T[] all, System.Predicate<T> match) where T : class
        {
            foreach (var one in all)
            {
                if (match(one))
                {
                    return one;
                }
            }

            return null;
        }

        /// <summary>Where on the map an id is: a place, an NPC, a thing or an encounter.</summary>
        public static bool Locate(string id, out MapPoint at)
        {
            at = default;

            if (string.IsNullOrEmpty(id))
            {
                return false;
            }

            var place = Place(id);

            if (place != null)
            {
                at = place.At;
                return true;
            }

            var npc = Npc(id);

            if (npc != null)
            {
                at = npc.At;
                return true;
            }

            var thing = Thing(id);

            if (thing != null)
            {
                at = thing.At;
                return true;
            }

            var encounter = Encounter(id);

            if (encounter != null)
            {
                at = encounter.At;
                return true;
            }

            return false;
        }

        /// <summary>The place a point is in (the smallest that holds it), or null in the wilds.</summary>
        public static PlaceDef PlaceAt(MapPoint at)
        {
            PlaceDef best = null;

            foreach (var place in Places)
            {
                if (place.At.DistanceTo(at) <= place.Radius && (best == null || place.Radius < best.Radius))
                {
                    best = place;
                }
            }

            return best;
        }
    }
}
