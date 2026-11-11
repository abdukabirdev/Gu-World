namespace GuWorld.Core
{
    /// <summary>One of the artist's models (Resources/Build/&lt;Id&gt;): its name, its footprint across (its own X) and deep (its own Z, its front),
    /// its height, and where the footprint's middle lies from the model's origin. The front faces the model's +Z.</summary>
    public readonly struct Footprint
    {
        public readonly string Id;
        public readonly string Label;
        public readonly float Across;
        public readonly float Deep;
        public readonly float Height;
        public readonly float MiddleX;
        public readonly float MiddleZ;

        public Footprint(string id, string label, float across, float deep, float height, float middleX, float middleZ)
        {
            Id = id;
            Label = label;
            Across = across;
            Deep = deep;
            Height = height;
            MiddleX = middleX;
            MiddleZ = middleZ;
        }
    }

    /// <summary>The footprints of every model in Resources/Build (measured from the models when the builder's catalogue was made).</summary>
    public static class Catalog
    {
        public static readonly Footprint[] All =
        {
            new Footprint("kit_stilt_bamboo", "Бамбуковый дом на сваях", 7.49f, 8.03f, 6.36f, 0.00f, 1.37f),
            new Footprint("kit_hut_mud", "Глинобитная хижина", 5.16f, 4.59f, 4.32f, 0.11f, 0.00f),
            new Footprint("kit_l_kitchen", "Г-образный дом с кухней", 12.44f, 6.62f, 5.70f, -1.62f, 0.66f),
            new Footprint("kit_l_kitchen_grey", "Г-образный дом, серый", 12.44f, 6.62f, 5.70f, -1.62f, 0.66f),
            new Footprint("kit_two_storey", "Двухэтажный дом", 8.49f, 7.29f, 8.81f, 0.00f, 0.50f),
            new Footprint("kit_two_storey_stone", "Двухэтажный дом на камне", 8.49f, 7.29f, 8.81f, 0.00f, 0.50f),
            new Footprint("kit_slope_terrace", "Дом на краю террасы", 10.02f, 8.98f, 6.76f, 0.00f, -0.52f),
            new Footprint("kit_artisan", "Дом ремесленника", 8.59f, 6.77f, 5.15f, -0.05f, 0.59f),
            new Footprint("kit_artisan_ochre", "Дом ремесленника в охре", 8.59f, 6.77f, 5.15f, -0.05f, 0.59f),
            new Footprint("kit_courtyard", "Дом с двором", 12.29f, 12.45f, 5.64f, 0.00f, -1.57f),
            new Footprint("kit_courtyard_blue", "Дом с двором, синий лак", 12.29f, 12.45f, 5.64f, 0.00f, -1.57f),
            new Footprint("kit_shop_house", "Дом с лавкой", 7.49f, 8.92f, 8.87f, 0.00f, 0.82f),
            new Footprint("kit_shop_house_plaster", "Дом с лавкой, белёный", 7.49f, 8.92f, 8.87f, 0.00f, 0.82f),
            new Footprint("kit_stilt_plank", "Дощатый дом на сваях", 7.49f, 8.03f, 6.36f, 0.00f, 1.37f),
            new Footprint("kit_hut_mud_ochre", "Хижина в охре", 5.16f, 4.59f, 4.32f, 0.11f, 0.00f),
            new Footprint("prop_granary", "Амбар", 10.64f, 9.98f, 7.66f, 0.01f, 0.74f),
            new Footprint("guyue_bamboo_house", "Бамбуковый дом", 13.31f, 10.53f, 13.79f, -0.82f, 0.18f),
            new Footprint("prop_poor_house", "Бедный дом", 7.70f, 7.76f, 4.57f, -0.05f, 0.52f),
            new Footprint("prop_gambling_tent", "Игорный шатёр", 14.08f, 13.90f, 7.23f, 0.00f, -0.08f),
            new Footprint("prop_smithy", "Кузница", 10.85f, 9.24f, 7.58f, 0.00f, 0.00f),
            new Footprint("prop_shop", "Лавка", 7.63f, 9.87f, 7.88f, 0.00f, 0.57f),
            new Footprint("prop_herbalist", "Лавка травника", 8.64f, 10.22f, 6.63f, 0.00f, 0.96f),
            new Footprint("prop_shop_small_a", "Лавочка А", 5.44f, 6.26f, 5.14f, 0.00f, 0.00f),
            new Footprint("prop_shop_small_b", "Лавочка Б", 5.44f, 6.77f, 5.14f, 0.00f, 0.25f),
            new Footprint("prop_shop_small_c", "Лавочка В", 5.26f, 5.84f, 4.42f, 0.08f, 0.00f),
            new Footprint("prop_gu_nursery", "Питомник Гу", 14.02f, 11.34f, 8.03f, -0.01f, 0.18f),
            new Footprint("prop_inn", "Постоялый двор", 16.54f, 11.72f, 9.82f, -1.15f, -0.01f),
            new Footprint("prop_tavern", "Таверна", 12.24f, 12.41f, 9.55f, -0.00f, 0.45f),
            new Footprint("prop_eatery", "Харчевня", 13.63f, 11.12f, 8.43f, 0.00f, 0.00f),
            new Footprint("prop_teahouse", "Чайная", 11.31f, 12.43f, 11.10f, -0.01f, 0.56f),
            new Footprint("prop_academy_great", "Академия", 57.59f, 66.78f, 21.80f, 0.00f, -2.33f),
            new Footprint("prop_tavern_great", "Большая таверна", 44.22f, 42.92f, 22.69f, -0.01f, -9.96f),
            new Footprint("prop_teahouse_octagon", "Восьмигранный чайный дом", 19.79f, 21.90f, 18.90f, 0.00f, 1.05f),
            new Footprint("prop_clan_main_hall", "Главный зал клана", 83.00f, 42.65f, 27.96f, 0.01f, 6.18f),
            new Footprint("prop_inn_courtyard", "Гостиный двор", 37.38f, 35.90f, 16.58f, -0.03f, -0.76f),
            new Footprint("prop_ancestral_hall", "Зал предков", 13.63f, 13.67f, 12.42f, -0.01f, 1.15f),
            new Footprint("prop_council_hall", "Зал совета", 15.89f, 12.04f, 9.09f, 0.00f, 0.49f),
            new Footprint("guyue_pavilion_LOD0", "Лунный павильон", 24.30f, 26.15f, 36.46f, 0.00f, 2.43f),
            new Footprint("prop_academy", "Малая академия", 19.91f, 18.43f, 9.19f, 0.00f, 3.27f),
            new Footprint("prop_academy_dorm", "Малое общежитие", 17.62f, 9.76f, 9.02f, -0.09f, 0.00f),
            new Footprint("prop_dorm_great", "Общежитие", 68.65f, 17.53f, 21.27f, -0.00f, 0.94f),
            new Footprint("prop_market_row", "Торговый ряд", 54.60f, 12.65f, 13.73f, 0.00f, 0.00f),
            new Footprint("prop_refectory", "Трапезная", 42.24f, 30.14f, 15.89f, 0.00f, -5.66f),
            new Footprint("prop_gu_vault", "Хранилище Гу", 33.12f, 120.60f, 3.22f, -1.25f, -57.18f),
            new Footprint("prop_bamboo_fence", "Бамбуковый забор", 7.25f, 0.75f, 1.99f, -1.28f, 0.00f),
            new Footprint("fort_battle_gate", "Боевые ворота", 15.09f, 14.49f, 17.74f, -0.40f, -1.24f),
            new Footprint("fort_wall_segment", "Стена крепости", 7.24f, 8.78f, 9.95f, 0.00f, 0.85f),
            new Footprint("fort_watchtower", "Сторожевая башня", 9.32f, 9.30f, 17.39f, -0.00f, 1.12f),
            new Footprint("prop_stairs_grand", "Большая лестница", 8.08f, 23.32f, 9.02f, 0.00f, -1.46f),
            new Footprint("prop_stairs_medium", "Лестница", 5.38f, 7.77f, 4.33f, 0.00f, -0.09f),
            new Footprint("prop_paving_tiles", "Мощение", 10.84f, 3.44f, 0.15f, 0.00f, 0.00f),
            new Footprint("prop_wooden_ladder", "Приставная лестница", 0.59f, 0.95f, 3.31f, 0.00f, -0.39f),
            new Footprint("prop_steps_small", "Ступени", 2.50f, 2.94f, 0.70f, 0.00f, 0.15f),
            new Footprint("prop_paths", "Тропинка из камней", 26.81f, 6.74f, 1.19f, -0.30f, -0.77f),
            new Footprint("prop_bonsai_pine", "Бонсай", 1.05f, 0.53f, 1.35f, -0.00f, -0.01f),
            new Footprint("prop_barrels", "Бочки", 4.90f, 5.12f, 1.43f, 0.02f, -0.29f),
            new Footprint("prop_bronze_ding", "Бронзовый дин", 3.10f, 3.10f, 3.04f, -0.00f, 0.00f),
            new Footprint("prop_bench_wood", "Деревянная скамья", 1.90f, 0.72f, 1.01f, 0.00f, 0.00f),
            new Footprint("prop_firewood_stack", "Дрова", 4.03f, 2.06f, 1.62f, -0.51f, 0.53f),
            new Footprint("prop_bench_stone", "Каменная скамья", 3.41f, 1.31f, 0.62f, -0.76f, 0.20f),
            new Footprint("prop_village_well", "Колодец", 3.67f, 2.80f, 3.61f, 0.00f, 0.00f),
            new Footprint("prop_moon_gate", "Лунные ворота", 7.80f, 1.95f, 4.66f, 0.00f, 0.00f),
            new Footprint("prop_clan_stele", "Стела клана", 3.80f, 5.32f, 5.88f, 0.00f, 0.46f),
            new Footprint("prop_weapon_rack", "Стойка с оружием", 2.66f, 0.75f, 2.74f, 0.00f, 0.00f),
            new Footprint("prop_go_table", "Стол для го", 1.91f, 1.91f, 0.89f, 0.00f, 0.00f),
            new Footprint("prop_drying_rack", "Сушилка", 3.45f, 1.62f, 2.25f, -0.27f, 0.38f),
            new Footprint("prop_market_goods", "Товары", 3.73f, 1.96f, 1.28f, 0.10f, 0.04f),
            new Footprint("prop_moon_lantern_post", "Фонарь", 2.39f, 1.35f, 5.01f, 0.00f, 0.00f),
            new Footprint("prop_lotus_vat", "Чаша с лотосами", 1.44f, 1.44f, 1.39f, 0.00f, 0.00f),
            new Footprint("prop_bamboo_grove", "Бамбуковая роща", 5.99f, 6.24f, 9.15f, 0.06f, 0.00f),
            new Footprint("tree_ancient_100m", "Древнее дерево (100 м)", 104.20f, 95.46f, 108.77f, 1.08f, -0.15f),
            new Footprint("tree_camphor_70m", "Камфора (70 м)", 59.29f, 64.43f, 74.91f, 0.65f, 1.70f),
            new Footprint("tree_plum_8m", "Слива (8 м)", 9.91f, 10.86f, 9.28f, -0.04f, 0.19f),
            new Footprint("tree_pine_40m", "Сосна (40 м)", 30.46f, 28.99f, 40.60f, -1.18f, 0.61f),
            new Footprint("tree_deadwood_25m", "Сухое дерево (25 м)", 16.62f, 18.56f, 27.08f, 0.17f, 1.41f),
            new Footprint("prop_ancestral_altar", "Алтарь предков", 2.53f, 1.66f, 2.71f, 0.00f, 0.40f),
            new Footprint("prop_medicine_cabinet", "Аптечный шкаф", 1.43f, 0.68f, 2.46f, 0.00f, 0.01f),
            new Footprint("prop_canopy_bed", "Кровать с балдахином", 2.52f, 2.55f, 2.33f, 0.00f, 0.32f),
            new Footprint("prop_luohan_bed", "Кушетка", 2.15f, 1.00f, 1.09f, 0.00f, 0.00f),
            new Footprint("prop_writing_desk", "Письменный стол", 1.64f, 1.52f, 1.48f, 0.00f, 0.40f),
            new Footprint("prop_gu_jar_shelf", "Полка с кувшинами Гу", 2.70f, 0.83f, 2.82f, 0.00f, 0.00f),
            new Footprint("prop_curio_shelf", "Полка с редкостями", 1.81f, 0.49f, 2.57f, 0.00f, 0.00f),
            new Footprint("prop_folding_screen", "Ширма", 3.18f, 0.53f, 2.02f, 0.00f, 0.17f),
            new Footprint("prop_liquor_worm", "Винный Червь", 0.13f, 0.13f, 0.03f, 0.00f, 0.00f),
            new Footprint("prop_white_jade_gu", "Гу Белого Нефрита", 0.11f, 0.09f, 0.05f, 0.00f, 0.00f),
            new Footprint("prop_moonlight_gu", "Гу Лунного Света", 0.11f, 0.11f, 0.11f, 0.00f, 0.00f),
            new Footprint("prop_spring_autumn_cicada", "Нефритовая цикада", 0.11f, 0.15f, 0.06f, 0.00f, -0.00f),
            new Footprint("prop_stairs_tall", "prop_stairs_tall", 8.08f, 81.96f, 26.96f, 0.00f, 0.00f),
        };

        static System.Collections.Generic.Dictionary<string, Footprint> byId;

        public static bool TryGet(string id, out Footprint footprint)
        {
            if (byId == null)
            {
                byId = new System.Collections.Generic.Dictionary<string, Footprint>();

                foreach (var one in All)
                {
                    byId[one.Id] = one;
                }
            }

            return byId.TryGetValue(id ?? "", out footprint);
        }

        public static Footprint Get(string id) => TryGet(id, out var footprint) ? footprint : new Footprint(id, id, 4, 4, 4, 0, 0);
    }
}
