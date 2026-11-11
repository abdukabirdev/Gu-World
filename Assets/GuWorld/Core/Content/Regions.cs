using System.Collections.Generic;

namespace GuWorld.Core
{
    /// <summary>How a settlement is built: the models its houses are drawn from, its walls, its feel.</summary>
    public enum SettlementStyle
    {
        /// <summary>Gu Yue: dark wood, crescents, stilts on the slope.</summary>
        GuYue,
        /// <summary>Bai: white plaster and blue awnings by the water.</summary>
        Bai,
        /// <summary>Xiong: logs, ochre clay, thatch; honey and bears.</summary>
        Xiong,
        /// <summary>Mortals at the foot of the mountain: mud huts, stilts, fields.</summary>
        Mortal,
        /// <summary>A camp: tents and lean-tos behind fences.</summary>
        Camp,
    }

    /// <summary>A settlement the world's generator lays out: its streets, lots and houses are made, not drawn by hand.</summary>
    public class SettlementDef
    {
        public string Id;
        public string Name;
        public MapPoint At;
        public float Radius;
        public SettlementStyle Style;
        /// <summary>The clan it belongs to ("gu_yue", "bai", "xiong", "mortal", "bandit").</summary>
        public string Clan;
        /// <summary>Its main street's direction, degrees (0 north).</summary>
        public float Axis;
        /// <summary>How many houses it should hold, roughly.</summary>
        public int Houses = 12;
        /// <summary>Buildings that must stand in it (a clan hall, a shop), from the catalogue.</summary>
        public string[] Must = new string[0];
        /// <summary>A wall (true) or a fence round it.</summary>
        public bool Walled;
        /// <summary>Fields round it: rows of crops outside the fence.</summary>
        public bool Fields;
        public int Seed;
        public int Colour = 0xC9D3D5;
    }

    /// <summary>A river of the world: its course from source to mouth (the generator makes it wander and finds its water level).</summary>
    public class RiverDef
    {
        public string Id;
        public string Name;
        public MapPoint[] Course;
        /// <summary>The bed's width at the source and at the mouth, metres.</summary>
        public float WidthFrom = 5;
        public float WidthTo = 12;
        /// <summary>How far it may wander from the course drawn.</summary>
        public float Meander = 30;
        /// <summary>The river it flows into (its last point is on that river), or "".</summary>
        public string Joins = "";
        /// <summary>Points of the course whose water level is fixed (a waterfall's lip and pool), as (index into Course, level).</summary>
        public (int point, float level)[] Pins = new (int, float)[0];
    }

    /// <summary>A road the world's generator must find a way for, between two places (ids of places, settlements, things, or "x,z").</summary>
    public class RoadDef
    {
        public string From;
        public string To;
        /// <summary>A main road (wider, paved near villages) or a trail.</summary>
        public bool Main;

        public RoadDef(string from, string to, bool main = false)
        {
            From = from;
            To = to;
            Main = main;
        }
    }

    /// <summary>
    /// The whole of the world beyond the Gu Yue valley: Qing Mao mountain 8 × 8 km — its ridge and
    /// peak, the front mountain with the Xiong, the Bai behind the ridge at the back mountain's
    /// waterfall, the hamlets of mortals at its foot, the rivers and the roads between them.
    /// Coordinates: X east, Z north, metres from the Gu Yue well (the valley's own plan is in Valley).
    /// </summary>
    public static class Regions
    {
        public const float Half = 4000;

        // ---------------------------------------------------------------- The mountain's shape

        /// <summary>The main ridge from west to east, and the height of its crest at each point (the peak is the highest).</summary>
        public static readonly (MapPoint at, float crest)[] Ridge =
        {
            (new MapPoint(-3750, 1250), 230),
            (new MapPoint(-2500, 1600), 400),
            (new MapPoint(-1450, 1720), 470),
            (new MapPoint(-1150, 1760), 360),
            (new MapPoint(-800, 1820), 480),
            (new MapPoint(300, 1950), 660),
            (new MapPoint(1400, 1850), 560),
            (new MapPoint(2600, 1550), 430),
            (new MapPoint(3750, 1150), 260),
        };

        /// <summary>How far the ridge's slopes reach to the south (towards the Gu Yue) and to the north (towards the Bai).</summary>
        public const float RidgeSouthReach = 1500;
        public const float RidgeNorthReach = 1350;

        /// <summary>The front mountain: a smaller peak to the south-east, the Xiong on its western shoulder.</summary>
        public static readonly MapPoint FrontPeak = new MapPoint(2450, -2350);
        public const float FrontCrest = 430;
        public const float FrontReach = 1550;

        /// <summary>The cliff belt behind the Bai: land to the south of this line stands higher by the drop.</summary>
        public static readonly MapPoint[] BaiCliff = { new MapPoint(-1500, 3000), new MapPoint(-1050, 3075), new MapPoint(-650, 3085), new MapPoint(-250, 3030) };
        public const float BaiCliffDrop = 52;

        /// <summary>The low wet land in the south-west, where the western stream spreads into a marsh.</summary>
        public static readonly MapPoint Marsh = new MapPoint(-2650, -1550);
        public const float MarshRadius = 520;

        // ---------------------------------------------------------------- Rivers

        public static readonly RiverDef[] Rivers =
        {
            new RiverDef
            {
                Id = "qing", Name = "Река Цин", WidthFrom = 4, WidthTo = 14, Meander = 26,
                Course = new[]
                {
                    new MapPoint(520, 1720), new MapPoint(460, 1250), new MapPoint(380, 820), new MapPoint(330, 470), new MapPoint(300, 300),
                    new MapPoint(276, 150), new MapPoint(262, 120), new MapPoint(244, 66), new MapPoint(238, 0), new MapPoint(262, -90),
                    new MapPoint(296, -200), new MapPoint(330, -330), new MapPoint(350, -470), new MapPoint(420, -900), new MapPoint(560, -1400),
                    new MapPoint(420, -2100), new MapPoint(260, -2700), new MapPoint(420, -3300), new MapPoint(500, -3990),
                },
                // The plateau's lip and the pool below the fall, as the valley's plan has them.
                Pins = new[] { (4, 28.6f), (5, 28.4f), (6, 11.4f) }
            },
            new RiverDef
            {
                Id = "bai", Name = "Ручей Бай", WidthFrom = 3, WidthTo = 10, Meander = 22,
                Course = new[]
                {
                    new MapPoint(-300, 2250), new MapPoint(-560, 2700), new MapPoint(-760, 3030), new MapPoint(-790, 3110), new MapPoint(-830, 3180),
                    new MapPoint(-640, 3420), new MapPoint(-480, 3700), new MapPoint(-300, 3990),
                }
            },
            new RiverDef
            {
                Id = "bear", Name = "Медвежий ручей", WidthFrom = 3, WidthTo = 7, Meander = 18, Joins = "qing",
                Course = new[] { new MapPoint(2350, -1700), new MapPoint(1900, -2050), new MapPoint(1300, -2400), new MapPoint(700, -2450), new MapPoint(330, -2420) }
            },
            new RiverDef
            {
                Id = "west", Name = "Западный ручей", WidthFrom = 3, WidthTo = 9, Meander = 30,
                Course = new[]
                {
                    new MapPoint(-1700, 1450), new MapPoint(-1900, 700), new MapPoint(-2150, -200), new MapPoint(-2450, -1100), new MapPoint(-2700, -1650),
                    new MapPoint(-3200, -2300), new MapPoint(-3990, -2600),
                }
            },
        };

        /// <summary>The pool the back mountain's waterfall falls into, beside the Bai.</summary>
        public static readonly MapPoint BaiPool = new MapPoint(-815, 3170);
        public const float BaiPoolRadius = 42;

        // ---------------------------------------------------------------- Settlements

        public static readonly SettlementDef[] Settlements =
        {
            new SettlementDef
            {
                Id = "bai_village", Name = "Деревня Бай", At = new MapPoint(-1120, 3330), Radius = 125, Style = SettlementStyle.Bai, Clan = "bai", Axis = 120, Houses = 34,
                Must = new[] { "prop_council_hall", "prop_shop", "prop_teahouse_octagon", "prop_herbalist", "prop_gu_nursery", "prop_inn" }, Walled = true, Seed = 11, Colour = 0x9FE8FF
            },
            new SettlementDef
            {
                Id = "xiong_village", Name = "Деревня Сюн", At = new MapPoint(1900, -1700), Radius = 120, Style = SettlementStyle.Xiong, Clan = "xiong", Axis = 300, Houses = 32,
                Must = new[] { "prop_council_hall", "prop_shop", "prop_tavern", "prop_smithy", "prop_granary", "prop_gu_nursery" }, Walled = true, Seed = 23, Colour = 0xE0A070
            },
            new SettlementDef { Id = "willow", Name = "Ивовый хутор", At = new MapPoint(-1650, -760), Radius = 55, Style = SettlementStyle.Mortal, Clan = "mortal", Axis = 40, Houses = 9, Fields = true, Seed = 31 },
            new SettlementDef { Id = "terraces", Name = "Хутор Террасы", At = new MapPoint(-1100, -2100), Radius = 60, Style = SettlementStyle.Mortal, Clan = "mortal", Axis = 10, Houses = 10, Fields = true, Seed = 37 },
            new SettlementDef { Id = "mill", Name = "Хутор у Мельницы", At = new MapPoint(-2350, 420), Radius = 50, Style = SettlementStyle.Mortal, Clan = "mortal", Axis = 95, Houses = 8, Fields = true, Seed = 41, Must = new[] { "prop_granary" } },
            new SettlementDef { Id = "pine", Name = "Сосновый хутор", At = new MapPoint(-520, 1180), Radius = 45, Style = SettlementStyle.Mortal, Clan = "mortal", Axis = 160, Houses = 7, Seed = 43 },
            new SettlementDef { Id = "honey", Name = "Медовый хутор", At = new MapPoint(2850, -2950), Radius = 50, Style = SettlementStyle.Mortal, Clan = "mortal", Axis = 220, Houses = 8, Fields = true, Seed = 47, Must = new[] { "prop_drying_rack" } },
            new SettlementDef { Id = "stone", Name = "Каменный хутор", At = new MapPoint(2900, 260), Radius = 50, Style = SettlementStyle.Mortal, Clan = "mortal", Axis = 270, Houses = 8, Seed = 53 },
            new SettlementDef { Id = "misty", Name = "Туманный хутор", At = new MapPoint(420, 2950), Radius = 48, Style = SettlementStyle.Mortal, Clan = "mortal", Axis = 200, Houses = 7, Seed = 59 },
            new SettlementDef { Id = "bamboo", Name = "Бамбуковый хутор", At = new MapPoint(-2850, -2650), Radius = 50, Style = SettlementStyle.Mortal, Clan = "mortal", Axis = 60, Houses = 8, Fields = true, Seed = 61 },
            new SettlementDef { Id = "ford", Name = "Хутор у Брода", At = new MapPoint(820, -1380), Radius = 50, Style = SettlementStyle.Mortal, Clan = "mortal", Axis = 0, Houses = 9, Fields = true, Seed = 67 },
            new SettlementDef { Id = "trade_post", Name = "Застава Южного тракта", At = new MapPoint(150, -3550), Radius = 55, Style = SettlementStyle.Camp, Clan = "mortal", Axis = 0, Houses = 7, Seed = 71, Must = new[] { "prop_inn", "prop_shop_small_a" } },
            new SettlementDef { Id = "bandit_fort", Name = "Логово Чёрного Хвоста", At = new MapPoint(-3050, 2350), Radius = 55, Style = SettlementStyle.Camp, Clan = "bandit", Axis = 135, Houses = 7, Walled = true, Seed = 73, Colour = 0xF08A74 },
        };

        // ---------------------------------------------------------------- Places beyond the valley

        public static readonly PlaceDef[] Places =
        {
            new PlaceDef { Id = "pass", Name = "Перевал Цинмао", At = new MapPoint(-1150, 1760), Radius = 26, Flatten = true, Colour = 0xC9D3D5 },
            new PlaceDef { Id = "peak", Name = "Вершина Цинмао", At = new MapPoint(300, 1950), Radius = 30, Flatten = true, Colour = 0xFFF1CF },
            new PlaceDef { Id = "bai_pool", Name = "Омут Бай", At = new MapPoint(-815, 3170), Radius = 50, Colour = 0x6FB8FF },
            new PlaceDef { Id = "bear_hollow", Name = "Медвежий лог", At = new MapPoint(1450, -2350), Radius = 70, Flatten = true, Colour = 0xE0A070 },
            new PlaceDef { Id = "marsh", Name = "Змеиное болото", At = new MapPoint(-2650, -1550), Radius = 260, Colour = 0x7FE0C8 },
            new PlaceDef { Id = "tomb", Name = "Гробница Древнего мастера", At = new MapPoint(2250, 950), Radius = 40, Cave = true, Mouth = 137, Colour = 0xC0A8FF },
            new PlaceDef { Id = "mine", Name = "Заброшенный рудник", At = new MapPoint(-2050, 2250), Radius = 38, Cave = true, Mouth = 189, Colour = 0xD0B080 },
            new PlaceDef { Id = "spider_caves", Name = "Паучьи пещеры", At = new MapPoint(3150, 2450), Radius = 44, Cave = true, Mouth = 186, Colour = 0x9FEFC4 },
            new PlaceDef { Id = "great_tree", Name = "Великое древо", At = new MapPoint(-350, -1900), Radius = 36, Flatten = true, Colour = 0x9FEFC4 },
            new PlaceDef { Id = "eagle_cliffs", Name = "Орлиные скалы", At = new MapPoint(1700, 1400), Radius = 40, Flatten = true, Colour = 0xC9D3D5 },
            new PlaceDef { Id = "south_road", Name = "Южный тракт", At = new MapPoint(150, -3300), Radius = 60, Colour = 0xE8C98A },
        };

        // ---------------------------------------------------------------- Roads

        /// <summary>The roads the generator finds a way for (the valley's own roads stay as drawn).</summary>
        public static readonly RoadDef[] Roads =
        {
            // From the valley's road ends outwards.
            new RoadDef("0,352", "pass", true),
            new RoadDef("pass", "bai_village", true),
            new RoadDef("236,-178", "xiong_village", true),
            new RoadDef("40,-240", "ford", true),
            new RoadDef("ford", "trade_post", true),
            new RoadDef("ford", "xiong_village"),
            new RoadDef("-195,30", "willow", true),
            new RoadDef("willow", "terraces"),
            new RoadDef("willow", "mill"),
            new RoadDef("terraces", "bamboo"),
            new RoadDef("terraces", "great_tree"),
            new RoadDef("great_tree", "ford"),
            new RoadDef("bamboo", "-2450,-1820"),
            new RoadDef("mill", "pine"),
            new RoadDef("pine", "pass"),
            new RoadDef("mill", "mine"),
            new RoadDef("mine", "bandit_fort"),
            new RoadDef("xiong_village", "honey"),
            new RoadDef("xiong_village", "bear_hollow"),
            new RoadDef("268,124", "stone"),
            new RoadDef("stone", "tomb"),
            new RoadDef("tomb", "eagle_cliffs"),
            new RoadDef("eagle_cliffs", "peak"),
            new RoadDef("stone", "spider_caves"),
            new RoadDef("bai_village", "misty"),
            new RoadDef("misty", "peak"),
        };

        // ---------------------------------------------------------------- Lookups

        static Dictionary<string, SettlementDef> settlements;
        static Dictionary<string, PlaceDef> places;

        public static SettlementDef Settlement(string id)
        {
            if (settlements == null)
            {
                settlements = new Dictionary<string, SettlementDef>();

                foreach (var one in Settlements)
                {
                    settlements[one.Id] = one;
                }
            }

            return id != null && settlements.TryGetValue(id, out var found) ? found : null;
        }

        public static PlaceDef Place(string id)
        {
            if (places == null)
            {
                places = new Dictionary<string, PlaceDef>();

                foreach (var one in Places)
                {
                    places[one.Id] = one;
                }
            }

            return id != null && places.TryGetValue(id, out var found) ? found : null;
        }

        /// <summary>Where an id is: a settlement, a place of the regions or of the valley, a thing, an NPC — or "x,z".</summary>
        public static bool Locate(string id, out MapPoint at)
        {
            at = default;

            if (string.IsNullOrEmpty(id))
            {
                return false;
            }

            var comma = id.IndexOf(',');

            if (comma > 0 && float.TryParse(id.Substring(0, comma), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var x)
                && float.TryParse(id.Substring(comma + 1), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var z))
            {
                at = new MapPoint(x, z);
                return true;
            }

            var settlement = Settlement(id);

            if (settlement != null)
            {
                at = settlement.At;
                return true;
            }

            var place = Place(id);

            if (place != null)
            {
                at = place.At;
                return true;
            }

            return Valley.Locate(id, out at);
        }
    }
}
