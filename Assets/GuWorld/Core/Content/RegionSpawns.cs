using System;
using System.Collections.Generic;

namespace GuWorld.Core
{
    /// <summary>
    /// Who roams the wider mountain and where: packs and herds round the places, the Black Tail at
    /// the mine and its fort, the clans' patrols when they hate the hero; and the staged fights —
    /// the bosses of each place, the fort's assault, the duels with the clans' champions.
    /// </summary>
    public static class RegionSpawns
    {
        static MapPoint P(string place, float bearing, float share)
        {
            var def = Regions.Place(place);
            var at = def != null ? def.At : Regions.Settlement(place).At;
            var radius = def != null ? def.Radius : Regions.Settlement(place).Radius;
            var radians = bearing * Math.PI / 180;

            return new MapPoint(at.X + (float)Math.Sin(radians) * radius * share, at.Z + (float)Math.Cos(radians) * radius * share);
        }

        static SpawnDef S(string id, string enemy, int count, string place, float bearing, float share, float radius = 16, float respawn = 240, string needs = "", string gone = "") =>
            new SpawnDef { Id = id, Enemy = enemy, Count = count, At = P(place, bearing, share), Radius = radius, Respawn = respawn, Needs = needs, GoneAfter = gone };

        static SpawnDef A(string id, string enemy, int count, float x, float z, float radius = 18, float respawn = 240, string needs = "") =>
            new SpawnDef { Id = id, Enemy = enemy, Count = count, At = new MapPoint(x, z), Radius = radius, Respawn = respawn, Needs = needs };

        public static readonly SpawnDef[] Mountain =
        {
            // ------------------------------------------------------------------ The roads out of the valley
            A("road_north_wolves", "wolf", 3, -600, 1350, 20),
            A("road_north_rogue", "rogue_master", 1, -900, 1580, 8, 360),
            A("road_west_foxes", "fox", 4, -1000, -350, 22),
            A("road_west_wolves", "wolf", 3, -1500, -1100, 20),
            A("road_east_boars", "wild_boar", 3, 800, -700, 22),
            A("road_south_rogues", "rogue_master", 2, 450, -2400, 14, 300),
            A("road_south_bandits", "bandit", 3, 200, -2900, 18, 240),
            A("road_stone_wolves", "wolf", 4, 1700, 300, 22),
            A("road_misty_wolves", "snow_wolf", 3, 200, 2500, 20, 300),

            // ------------------------------------------------------------------ Round the hamlets
            A("willow_wolves", "wolf", 4, -1820, -640, 20),
            A("willow_wolves_b", "wolf", 3, -1500, -900, 18),
            A("terraces_boars", "wild_boar", 3, -1250, -2280, 20),
            A("mill_foxes", "fox", 4, -2500, 560, 20),
            A("pine_wolves", "wolf", 3, -700, 1350, 18),
            A("honey_bees_a", "bee_swarm", 3, 2700, -3100, 16),
            A("honey_bees_b", "bee_swarm", 3, 3000, -2800, 16),
            A("stone_wolves", "wolf", 3, 3050, 420, 20),
            A("misty_eagles", "mountain_eagle", 2, 600, 2800, 20, 300),
            A("bamboo_snakes_a", "marsh_snake", 4, -2700, -2400, 18),
            A("bamboo_snakes_b", "marsh_snake", 3, -2950, -2300, 16),
            A("ford_rogues", "rogue_master", 2, 1000, -1550, 14, 300),

            // ------------------------------------------------------------------ The Xiong's front mountain
            S("hollow_bears_a", "brown_bear", 2, "bear_hollow", 0, 0.5f, 16),
            S("hollow_bears_b", "brown_bear", 2, "bear_hollow", 120, 0.6f, 16),
            S("hollow_bears_c", "brown_bear", 2, "bear_hollow", 240, 0.55f, 16),
            S("hollow_bees", "bee_swarm", 2, "bear_hollow", 200, 1.3f, 14),
            A("xiong_boars", "wild_boar", 4, 2200, -1900, 24),
            A("xiong_bears", "brown_bear", 2, 1650, -2050, 18, 300),

            // ------------------------------------------------------------------ The marsh
            S("marsh_snakes_a", "marsh_snake", 4, "marsh", 0, 0.5f, 22),
            S("marsh_snakes_b", "marsh_snake", 4, "marsh", 100, 0.6f, 22),
            S("marsh_snakes_c", "marsh_snake", 3, "marsh", 200, 0.7f, 20),
            S("marsh_toads_a", "marsh_toad", 3, "marsh", 60, 0.35f, 18),
            S("marsh_toads_b", "marsh_toad", 3, "marsh", 260, 0.45f, 18),
            S("marsh_master", "poison_master", 1, "marsh", 160, 0.3f, 8, 480),

            // ------------------------------------------------------------------ Caves
            S("mine_mouth", "black_tail_thug", 3, "mine", 189, 1.1f, 12, 300, "", "black_tail_dead"),
            S("mine_ghouls_a", "mine_ghoul", 3, "mine", 60, 0.45f, 12),
            S("mine_ghouls_b", "mine_ghoul", 2, "mine", 300, 0.5f, 10),
            S("mine_centipedes", "centipede", 2, "mine", 0, 0.55f, 10),
            S("mine_bats", "cave_bat", 4, "mine", 140, 0.4f, 12),
            S("spider_mouth", "cave_spider", 2, "spider_caves", 186, 0.8f, 10),
            S("spiders_a", "cave_spider", 3, "spider_caves", 90, 0.5f, 12),
            S("spiders_b", "cave_spider", 3, "spider_caves", 270, 0.5f, 12),
            S("spider_bats", "cave_bat", 4, "spider_caves", 0, 0.35f, 12),
            S("spider_centipedes", "centipede", 2, "spider_caves", 30, 0.7f, 10),
            S("tomb_guards_a", "tomb_guardian", 2, "tomb", 90, 0.5f, 8),
            S("tomb_guards_b", "tomb_guardian", 2, "tomb", 270, 0.5f, 8),
            S("tomb_wisps", "tomb_wisp", 4, "tomb", 180, 0.4f, 12),

            // ------------------------------------------------------------------ Heights
            S("eagles_a", "mountain_eagle", 3, "eagle_cliffs", 90, 1.2f, 20),
            S("eagles_b", "mountain_eagle", 2, "eagle_cliffs", 270, 1.2f, 20),
            S("peak_wolves_a", "snow_wolf", 3, "peak", 60, 2.2f, 22),
            S("peak_wolves_b", "snow_wolf", 3, "peak", 240, 2.4f, 22),
            S("pass_wolves", "wolf", 4, "pass", 90, 2.5f, 22),
            S("pass_rogue", "rogue_master", 1, "pass", 300, 2.6f, 8, 360),

            // ------------------------------------------------------------------ The Black Tail's fort
            S("fort_thugs_a", "black_tail_thug", 3, "bandit_fort", 0, 0.4f, 12, 360, "", "black_tail_dead"),
            S("fort_thugs_b", "black_tail_thug", 2, "bandit_fort", 180, 0.5f, 10, 360, "", "black_tail_dead"),
            S("fort_archers", "black_tail_archer", 3, "bandit_fort", 90, 0.6f, 10, 360, "", "black_tail_dead"),
            S("fort_masters", "black_tail_master", 2, "bandit_fort", 270, 0.5f, 8, 360, "", "black_tail_dead"),

            // ------------------------------------------------------------------ The clans' patrols, when they hate the hero
            A("patrol_bai_gate", "bai_patrol", 3, -1050, 3180, 14, 300, "rep:bai<=-50"),
            A("patrol_bai_road", "bai_patrol", 2, -1150, 2600, 14, 300, "rep:bai<=-50"),
            A("patrol_xiong_gate", "xiong_patrol", 3, 1780, -1580, 14, 300, "rep:xiong<=-50"),
            A("patrol_xiong_road", "xiong_patrol", 2, 1200, -1250, 14, 300, "rep:xiong<=-50"),
            A("patrol_gu_yue", "gu_yue_patrol", 3, 0, 520, 14, 300, "rep:gu_yue<=-50"),
        };

        static EncounterDef E(string id, string name, MapPoint at, float radius, string needs, params WaveDef[] waves) => new EncounterDef { Id = id, Name = name, At = at, Radius = radius, Needs = needs, Waves = waves };

        static WaveDef W(string shout, params (string enemy, int count)[] enemies) => new WaveDef { Shout = shout, Enemies = enemies };

        public static readonly EncounterDef[] MountainEncounters =
        {
            // The wolf tide (see Session.Tide): at the old north gate of the village, on its nights.
            E("wolf_tide", "Волчий прилив", new MapPoint(0, 110), 150, "wolf_tide",
                W("Вой со всех склонов сразу. Стая выходит из темноты к воротам.", ("wolf", 6)),
                W("За первыми — грозовые волки.", ("wolf", 4), ("storm_wolf", 3)),
                W("И вожак. Сотенный.", ("storm_wolf", 2), ("hundred_king", 1))),
            E("bear_king", "Медвежий король", P("bear_hollow", 0, 0.1f), 40, "",
                W("Земля дрожит. Из лога поднимается медведь размером с дом.", ("bear_king", 1))),
            E("swamp_python", "Хозяин болота", P("marsh", 330, 0.2f), 40, "",
                W("Вода вздувается холмом. Холм открывает глаза.", ("marsh_snake", 3)),
                W("", ("swamp_python", 1))),
            E("thousand_legs", "Нижний штрек", P("mine", 0, 0.2f), 30, "",
                W("Стук. Тысяча стуков. Из штрека ползёт она.", ("thousand_legs", 1))),
            E("spider_queen", "Гнездо Паучьей матери", P("spider_caves", 0, 0.15f), 32, "",
                W("Нити дрожат. Все сразу.", ("cave_spider", 4)),
                W("Сверху спускается Паучья мать.", ("spider_queen", 1))),
            E("storm_eagle", "Царь Орлиных скал", P("eagle_cliffs", 0, 0.2f), 40, "",
                W("Небо темнеет крыльями.", ("mountain_eagle", 3)),
                W("Гром. Над скалами кружит грозовой орёл.", ("storm_eagle", 1))),
            E("ancient_shade", "Саркофаг Древнего мастера", P("tomb", 0, 0.3f), 30, "tomb_gate_used",
                W("Нефритовые врата открываются. Внутри шевелится терракота.", ("tomb_guardian", 3)),
                W("Над саркофагом поднимается тень. Она помнит своё имя.", ("ancient_shade", 1))),
            E("fort_assault", "Штурм логова", P("bandit_fort", 0, 0.1f), 50, "",
                W("Чужак у ворот! К оружию!", ("black_tail_thug", 4), ("black_tail_archer", 2)),
                W("Мастера, сюда!", ("black_tail_master", 2), ("black_tail_thug", 2)),
                W("— Кто посмел? — На площадь выходит Чёрный Хвост.", ("black_tail", 1))),
            E("duel_bai", "Поединок с Бай Шэном", P("bai_village", 0, 0.05f), 24, "",
                W("Бай Шэн кланяется и отступает на три шага. Воздух холодеет.", ("bai_champion", 1))),
            E("duel_xiong", "Поединок с Сюн Ба", P("xiong_village", 0, 0.05f), 24, "",
                W("Сюн Ба сбрасывает куртку. — Ну, Гу Юэ!", ("xiong_champion", 1))),
            E("road_ambush", "Засада на тракте", new MapPoint(300, -3000), 40, "",
                W("Из-за камней — бродячие мастера.", ("rogue_master", 3)),
                W("И ещё!", ("rogue_master", 2), ("bandit_thrower", 2))),
            E("peak_storm", "Снежная стая", P("peak", 180, 1.6f), 40, "",
                W("Вой с трёх сторон.", ("snow_wolf", 4)),
                W("Вожак снежной стаи.", ("snow_wolf", 3), ("storm_wolf", 1))),
        };

        /// <summary>The mountain's and the wild lands' roamers and staged fights.</summary>
        public static readonly SpawnDef[] All = Lists.Join(Mountain, Wilds.Spawns);
        public static readonly EncounterDef[] Encounters = Lists.Join(MountainEncounters, Wilds.Encounters);
    }
}
