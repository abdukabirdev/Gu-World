using System;
using System.Collections.Generic;

namespace GuWorld.Core
{
    /// <summary>
    /// The things of the wider mountain the hero can use: lanterns to rest at and travel between,
    /// furnaces, notice boards, shrines, steles to read, herbs, chests, and the objects the regions'
    /// quests ask for. A thing with a Home stands beside a spot of that settlement; the rest are put
    /// round their place by bearing and share of its radius.
    /// </summary>
    public static class RegionThings
    {
        /// <summary>A point round a place: the bearing from its centre (0 north, 90 east) and the share of its radius.</summary>
        static MapPoint P(string place, float bearing, float share)
        {
            var def = Regions.Place(place);
            var at = def != null ? def.At : Regions.Settlement(place).At;
            var radius = def != null ? def.Radius : Regions.Settlement(place).Radius;
            var radians = bearing * Math.PI / 180;

            return new MapPoint(at.X + (float)Math.Sin(radians) * radius * share, at.Z + (float)Math.Cos(radians) * radius * share);
        }

        static ThingDef Lantern(string id, string name, string home) => new ThingDef { Id = id, Name = name, Kind = ThingKind.Lantern, Home = home, Role = "plaza", At = Regions.Settlement(home).At, Model = "prop_moon_lantern_post" };

        static ThingDef Lantern(string id, string name, string place, float bearing, float share) => new ThingDef { Id = id, Name = name, Kind = ThingKind.Lantern, At = P(place, bearing, share), Model = "prop_moon_lantern_post" };

        static ThingDef Herb(string id, string item, string name, string place, float bearing, float share, float regrow = 300) =>
            new ThingDef { Id = id, Name = name, Kind = ThingKind.Herb, Content = item, At = P(place, bearing, share), Regrow = regrow };

        static ThingDef Chest(string id, string name, string table, string place, float bearing, float share, string needs = "", string locked = "") =>
            new ThingDef { Id = id, Name = name, Kind = ThingKind.Chest, Content = table, At = P(place, bearing, share), Needs = needs, Locked = locked };

        static ThingDef Lore(string id, string name, string entry, string place, float bearing, float share, string model = "prop_clan_stele") =>
            new ThingDef { Id = id, Name = name, Kind = ThingKind.Lore, Content = entry, At = P(place, bearing, share), Model = model };

        static ThingDef Object(string id, string name, string place, float bearing, float share, string needs = "", string locked = "", string model = "") =>
            new ThingDef { Id = id, Name = name, Kind = ThingKind.QuestObject, At = P(place, bearing, share), Needs = needs, Locked = locked, Model = model };

        /// <summary>Herbs of one kind strewn round a place: count of them at even bearings from a start, at a share of its radius.</summary>
        static IEnumerable<ThingDef> Patch(string prefix, string item, string name, string place, int count, float from, float share, float regrow = 300)
        {
            for (var i = 0; i < count; i++)
            {
                var wobble = (i % 3 - 1) * 0.08f;
                yield return Herb($"{prefix}_{i + 1}", item, name, place, from + 360f / count * i, share + wobble, regrow);
            }
        }

        public static readonly ThingDef[] All = Build();

        static ThingDef[] Build()
        {
            var all = new List<ThingDef>
            {
                // ------------------------------------------------------------------ Lanterns of the settlements
                Lantern("lantern_bai", "Фонарь деревни Бай", "bai_village"),
                Lantern("lantern_xiong", "Фонарь деревни Сюн", "xiong_village"),
                Lantern("lantern_willow", "Фонарь Ивового хутора", "willow"),
                Lantern("lantern_terraces", "Фонарь Террас", "terraces"),
                Lantern("lantern_mill", "Фонарь у Мельницы", "mill"),
                Lantern("lantern_pine", "Фонарь Соснового хутора", "pine"),
                Lantern("lantern_honey", "Фонарь Медового хутора", "honey"),
                Lantern("lantern_stone_hamlet", "Фонарь Каменного хутора", "stone"),
                Lantern("lantern_misty", "Фонарь Туманного хутора", "misty"),
                Lantern("lantern_bamboo", "Фонарь Бамбукового хутора", "bamboo"),
                Lantern("lantern_ford", "Фонарь у Брода", "ford"),
                Lantern("lantern_trade", "Фонарь заставы", "trade_post"),

                // ------------------------------------------------------------------ Lanterns of the places
                Lantern("lantern_pass", "Фонарь перевала", "pass", 200, 0.7f),
                Lantern("lantern_peak", "Фонарь вершины", "peak", 180, 0.75f),
                Lantern("lantern_pool", "Фонарь у омута", "bai_pool", 100, 1.05f),
                Lantern("lantern_hollow", "Фонарь Медвежьего лога", "bear_hollow", 300, 0.9f),
                Lantern("lantern_marsh", "Фонарь на краю болота", "marsh", 60, 1.02f),
                Lantern("lantern_tomb", "Фонарь у гробницы", "tomb", 137, 1.5f),
                Lantern("lantern_mine", "Фонарь у рудника", "mine", 189, 1.5f),
                Lantern("lantern_spiders", "Фонарь у паучьих пещер", "spider_caves", 186, 1.5f),
                Lantern("lantern_tree", "Фонарь у Великого древа", "great_tree", 150, 0.85f),
                Lantern("lantern_eagle", "Фонарь Орлиных скал", "eagle_cliffs", 220, 0.8f),

                // ------------------------------------------------------------------ Furnaces, boards, shrines
                new ThingDef { Id = "furnace_bai", Name = "Печь травницы", Kind = ThingKind.Furnace, Home = "bai_village", Role = "herbalist", At = Regions.Settlement("bai_village").At, Model = "prop_bronze_ding" },
                new ThingDef { Id = "furnace_xiong", Name = "Горн Сюн Мэна", Kind = ThingKind.Furnace, Home = "xiong_village", Role = "smithy", At = Regions.Settlement("xiong_village").At, Model = "prop_bronze_ding" },
                new ThingDef { Id = "furnace_trade", Name = "Котёл алхимика", Kind = ThingKind.Furnace, Home = "trade_post", Role = "inn", At = Regions.Settlement("trade_post").At, Model = "prop_bronze_ding" },
                new ThingDef { Id = "board_bai", Name = "Доска поручений Бай", Kind = ThingKind.Board, Home = "bai_village", Role = "hall", At = Regions.Settlement("bai_village").At, Content = "bai_village" },
                new ThingDef { Id = "board_xiong", Name = "Доска поручений Сюн", Kind = ThingKind.Board, Home = "xiong_village", Role = "hall", At = Regions.Settlement("xiong_village").At, Content = "xiong_village" },
                new ThingDef { Id = "board_trade", Name = "Доска на заставе", Kind = ThingKind.Board, Home = "trade_post", Role = "plaza", At = Regions.Settlement("trade_post").At, Content = "trade_post" },
                new ThingDef { Id = "shrine_peak", Name = "Святилище вершины", Kind = ThingKind.Shrine, At = P("peak", 0, 0.2f), Content = "blessing_peak", Model = "prop_ancestral_altar" },
                new ThingDef { Id = "shrine_tree", Name = "Корни Великого древа", Kind = ThingKind.Shrine, At = P("great_tree", 0, 0.45f), Content = "blessing_tree" },
                new ThingDef { Id = "shrine_pass", Name = "Каменная ступа перевала", Kind = ThingKind.Shrine, At = P("pass", 40, 0.5f), Content = "blessing_pass", Model = "prop_clan_stele" },
                new ThingDef { Id = "shrine_pool", Name = "Камень над омутом", Kind = ThingKind.Shrine, At = P("bai_pool", 300, 1.08f), Content = "blessing_pool", Model = "prop_clan_stele" },

                // ------------------------------------------------------------------ Steles and inscriptions
                Lore("stele_tomb_1", "Стела у входа", "lore_tomb_1", "tomb", 120, 0.55f),
                Lore("stele_tomb_2", "Стела в зале", "lore_tomb_2", "tomb", 30, 0.5f),
                Lore("stele_tomb_3", "Стела у саркофага", "lore_tomb_3", "tomb", 300, 0.55f),
                Lore("stele_peak", "Надпись на вершине", "lore_peak", "peak", 90, 0.5f),
                Lore("stele_pass", "Пограничный столб", "lore_pass", "pass", 250, 0.6f),
                Lore("stele_mine", "Доска штейгера", "lore_mine", "mine", 160, 0.5f, ""),
                Lore("stele_spiders", "Кости у входа", "lore_spiders", "spider_caves", 200, 0.55f, ""),
                Lore("stele_marsh", "Затопленный алтарь", "lore_marsh", "marsh", 120, 0.6f),
                Lore("stele_eagle", "Гнездо на скале", "lore_eagles", "eagle_cliffs", 20, 0.6f, ""),

                // ------------------------------------------------------------------ Chests
                Chest("chest_marsh", "Тайник в тростнике", "chest_marsh", "marsh", 170, 0.55f),
                Chest("chest_bear", "Колода с мёдом", "chest_bear", "bear_hollow", 40, 0.6f, "bear_king_dead", "Медвежий король лежит на нём спиной."),
                Chest("chest_mine", "Сундук штейгера", "chest_mine", "mine", 60, 0.55f),
                Chest("chest_mine_deep", "Короб в нижнем штреке", "chest_mine_deep", "mine", 320, 0.6f, "item:mine_key", "Короб заперт. Ключ, должно быть, у штейгера."),
                Chest("chest_spiders", "Кокон с добром", "chest_spiders", "spider_caves", 90, 0.6f),
                Chest("chest_tomb", "Погребальный ларец", "chest_tomb", "tomb", 210, 0.55f),
                Chest("chest_tomb_inner", "Саркофаг мастера", "chest_tomb_inner", "tomb", 0, 0.25f, "ancient_shade_dead", "Тень стоит над саркофагом."),
                Chest("chest_eagle", "Сокровища гнезда", "chest_eagle", "eagle_cliffs", 60, 0.55f, "storm_eagle_dead", "Грозовой орёл не подпустит."),
                Chest("chest_peak", "Ларец на вершине", "chest_peak", "peak", 300, 0.5f, "flag:peak_blessed", "Ларец вмёрз в лёд. Святилище, может быть, растопит его."),
                Chest("chest_tree", "Дупло древа", "chest_tree", "great_tree", 260, 0.5f, "tree_healed", "Дупло затянуто корой."),
                // The mountain's heart, twice: under the ice of the peak and under the roots of the Great Tree. Only a master's hand reaches it.
                Chest("heart_peak", "Сердце под льдом", "heart_peak", "peak", 120, 0.35f, "rank>=4", "Подо льдом бьётся что-то живое. Рука слабее мастера не пробьёт этот лёд."),
                Chest("heart_tree", "Сердце под корнями", "heart_tree", "great_tree", 80, 0.3f, "rank>=4", "Под корнями светится зелёным. Корни не пускают того, кто не мастер."),
                new ThingDef { Id = "chest_fort", Name = "Погреб атамана", Kind = ThingKind.Chest, Content = "chest_fort", Home = "bandit_fort", Role = "plaza", At = Regions.Settlement("bandit_fort").At, Needs = "item:fort_key", Locked = "Погреб на замке. Ключ у атамана." },
                new ThingDef { Id = "chest_bai_vault", Name = "Хранилище Бай", Kind = ThingKind.Chest, Content = "chest_bai_vault", Home = "bai_village", Role = "nursery", At = Regions.Settlement("bai_village").At, Needs = "item:bai_vault_key", Locked = "Хранилище клана. Чужим нельзя." },
                new ThingDef { Id = "chest_gu_yue_vault", Name = "Тайное хранилище Гу Юэ", Kind = ThingKind.Chest, Content = "high_gu_yue", Home = "gu_yue", Role = "plaza", At = Regions.Settlement("gu_yue").At, Needs = "standing:gu_yue=honoured", Locked = "Хранилище старейшин. Его откроют только тому, кого клан чтит." },
                new ThingDef { Id = "chest_willow", Name = "Сундук в амбаре", Kind = ThingKind.Chest, Content = "chest_hamlet", Home = "willow", Role = "house", At = Regions.Settlement("willow").At, Needs = "willow_wolves_done", Locked = "Хозяин косится. Сначала помогите хутору." },

                // ------------------------------------------------------------------ The quests' objects
                Object("hairpin_pool", "Блеск на дне", "bai_pool", 220, 1.04f),
                Object("hive_1", "Дикий улей", "bear_hollow", 150, 1.25f, "", "", "prop_barrels"),
                Object("hive_2", "Дикий улей", "bear_hollow", 200, 1.35f, "", "", "prop_barrels"),
                Object("hive_3", "Дикий улей", "bear_hollow", 235, 1.12f, "", "", "prop_barrels"),
                Object("millstone_cart", "Телега с жерновом", "mine", 189, 1.25f),
                Object("woodcutter_body", "Тело лесоруба", "pass", 330, 0.8f),
                Object("eagle_nest", "Орлиное гнездо", "eagle_cliffs", 0, 0.75f),
                Object("tree_roots", "Иссохшие корни", "great_tree", 90, 0.5f, "item:spirit_seed", "Корням нужно семя."),
                Object("spider_egg", "Светящийся кокон", "spider_caves", 330, 0.55f, "spider_queen_dead", "Паучья мать сторожит кокон."),
                Object("tomb_gate", "Нефритовые врата", "tomb", 0, 0.45f, "item:tomb_key", "Врата запечатаны. Нужна нефритовая печать."),
                Object("fort_cell", "Клетка в погребе", "bandit_fort", 0, 0.3f, "item:fort_key", "Клетка заперта."),
                Object("python_nest", "Гнездо удава", "marsh", 330, 0.25f),
            };

            // ------------------------------------------------------------------ Herbs, round the places they grow at
            all.AddRange(Patch("lotus", "frost_lotus", "Ледяной лотос", "bai_pool", 6, 20, 1.06f));
            all.AddRange(Patch("cap", "red_cap", "Красный гриб", "marsh", 6, 10, 0.75f));
            all.AddRange(Patch("reed", "marsh_reed", "Болотный тростник", "marsh", 8, 30, 0.9f, 200));
            all.AddRange(Patch("ginseng", "ginseng", "Горный женьшень", "great_tree", 5, 0, 1.3f, 420));
            all.AddRange(Patch("vine", "blood_vine", "Кровяная лоза", "great_tree", 3, 45, 0.75f, 600));
            all.AddRange(Patch("moss", "spirit_moss", "Духовный мох", "spider_caves", 4, 20, 0.7f, 480));
            all.AddRange(Patch("tomb_moss", "spirit_moss", "Духовный мох", "tomb", 3, 80, 0.7f, 480));
            all.AddRange(Patch("garlic", "bear_garlic", "Черемша", "bear_hollow", 6, 10, 1.15f));
            all.AddRange(Patch("sun", "sun_bloom", "Солнечник", "eagle_cliffs", 5, 100, 1.3f));
            all.AddRange(Patch("peak_sun", "sun_bloom", "Солнечник", "peak", 3, 140, 1.4f));
            all.AddRange(Patch("tea", "cloud_tea", "Облачный чай", "pass", 4, 0, 1.6f, 480));
            all.AddRange(Patch("root_pass", "bitter_root", "Горький корень", "pass", 3, 120, 1.9f, 200));
            all.AddRange(Patch("root_road", "bitter_root", "Горький корень", "south_road", 5, 0, 0.9f, 200));
            all.AddRange(Patch("spark_moss", "spark_moss", "Грозовой мох", "eagle_cliffs", 4, 200, 0.9f));
            all.AddRange(Patch("ore", "iron_ore", "Жила руды", "mine", 4, 250, 0.7f, 600));

            return all.ToArray();
        }
    }
}
