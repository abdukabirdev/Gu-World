using System;
using UnityEngine;

namespace GuWorld.World
{
    /// <summary>Which clan a settlement belongs to (the mortal hamlets at the foot serve them all).</summary>
    public enum Clan
    {
        GuYue,
        Bai,
        Xiong,
        Mortal,
    }

    /// <summary>A settlement on the mountain, as the world's plan has it.</summary>
    [Serializable]
    public struct Settlement
    {
        public string Id;
        public string Name;
        public Clan Clan;
        /// <summary>Its middle (X, Z) and roughly how far it reaches, m.</summary>
        public Vector2 Centre;
        public float Radius;
        /// <summary>Downhill there, unit length (X, Z): which way the village faces.</summary>
        public Vector2 Downhill;
        /// <summary>Its own fine-grained ground (a terrain of its own, the mountain's cut away under it): its south-west corner and size, m; 0 for none.</summary>
        public Vector2 TileCorner;
        public float TileSize;
        public bool Walled;
        /// <summary>
        /// Where it is a rectangle in its own frame (Gu Yue's terraced town, 2026-10-25): the rectangle's middle (along
        /// the slope, uphill, from Centre) and half its size, m; zero: a circle of Radius.
        /// </summary>
        public Vector2 RectMiddle;
        public Vector2 RectHalf;

        /// <summary>How far outside its footprint a point is, m (negative inside).</summary>
        public float Outside(Vector2 p)
        {
            if (RectHalf == Vector2.zero)
            {
                return Vector2.Distance(p, Centre) - Radius;
            }

            var d = p - Centre;
            var along = new Vector2(-Downhill.y, Downhill.x);
            var local = new Vector2(Vector2.Dot(d, along), -Vector2.Dot(d, Downhill)) - RectMiddle;
            var q = new Vector2(Mathf.Abs(local.x) - RectHalf.x, Mathf.Abs(local.y) - RectHalf.y);
            var outside = new Vector2(Mathf.Max(q.x, 0), Mathf.Max(q.y, 0)).magnitude;

            return outside > 0 ? outside : Mathf.Max(q.x, q.y);
        }

        /// <summary>A point `beyond` m outside its footprint, out from its middle at an angle (radians, from +X).</summary>
        public Vector2 Around(float angle, float beyond)
        {
            var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            if (RectHalf == Vector2.zero)
            {
                return Centre + direction * (Radius + beyond);
            }

            var along = new Vector2(-Downhill.y, Downhill.x);
            var middle = Centre + along * RectMiddle.x - Downhill * RectMiddle.y;
            var reach = Mathf.Min(RectHalf.x / Mathf.Max(1e-4f, Mathf.Abs(Vector2.Dot(direction, along))), RectHalf.y / Mathf.Max(1e-4f, Mathf.Abs(Vector2.Dot(direction, Downhill))));

            return middle + direction * (reach + beyond);
        }
    }

    /// <summary>
    /// Qing Mao Mountain (青茅山, lore: loc_qing_mao_mountain) — the first arc's
    /// world: one great mountain, 20 km across, and the three clans' villages
    /// at three far-apart places on it (lore ch. 65): Gu Yue at the mountain's
    /// waist on its south-western flank, the Xiong village on the fore mountain
    /// — a lower peak in front, to the south-east — and the Bai village behind,
    /// beside the deep pool under the back mountain's waterfall (ch. 14). The
    /// clans are rivals; a day's walk lies between them. Mortal hamlets sit at
    /// the foot. Every measure here is the game's own assumption
    /// (DATA_CONTRACT §7), to be tuned.
    ///
    /// The map is 20 km square, centred on the origin, Y up, X east and Z north
    /// in the game's numbers. The shape of the ground (MountainShape, in the
    /// editor) and everything built on it come from these places; what the
    /// builder worked out — the trails, the streams, the villages' plans — is
    /// kept in the scene (WorldPlan, VillageLayout).
    /// </summary>
    public static class QingMao
    {
        /// <summary>The location's id ("qing_mao_2": the 20 km mountain; saves on the old 1.6 km map start again at the village).</summary>
        public const string Id = "qing_mao_2";
        public const string Title = "Гора Цинмао";
        public const float Size = 20000;
        public const float Half = Size / 2;
        /// <summary>The terrain's height range, m (the peak is well under it).</summary>
        public const float MaxHeight = 2600;

        public static readonly Vector2 Peak = new Vector2(300, 1800);
        /// <summary>The fore mountain: a lower peak in front of the great one.</summary>
        public static readonly Vector2 ForePeak = new Vector2(3900, -4300);

        public static readonly Vector2 GuYue = new Vector2(-2300, -1700);
        /// <summary>
        /// Where the brook above the village rises (the women wash there). The clan's spirit spring itself is
        /// under the clan pavilion, down a stone stair from inside it (lore ch. 4), not here.
        /// </summary>
        public static readonly Vector2 Spring = new Vector2(-2230, -1440);
        /// <summary>
        /// The Flower Wine Monk's secret cave: a crevice worn by a trickle of falling water in a boulder against a
        /// rock wall, reached from the spear bamboo grove (lore ch. 14) — uphill of the grove, outside the village.
        /// </summary>
        public static readonly Vector2 SecretCave = new Vector2(-1349, -1782);
        /// <summary>Qing Mao's spear bamboo, a grove east of Gu Yue (outside its east wall since the town grew, 2026-10-25).</summary>
        public static readonly Vector2 Grove = new Vector2(-1460, -1972);
        public const float GroveRadius = 170;

        public static readonly Vector2 Xiong = new Vector2(3150, -4750);
        /// <summary>The back mountain's waterfall: the lip of the cliff, and the deep pool under it.</summary>
        public static readonly Vector2 FallTop = new Vector2(-1720, 6110);
        public static readonly Vector2 Pool = new Vector2(-1720, 6240);
        public const float PoolRadius = 62;
        public static readonly Vector2 Bai = new Vector2(-1960, 6440);

        public static readonly Settlement[] Settlements =
        {
            // A terraced town on three levels in a rectangle 1080 by 500 m (VillagePlanTiers.cs, 2026-10-25): its frame's
            // downhill as MountainShape.DownhillAt has it there.
            new Settlement { Id = "gu_yue", Name = "Деревня Гу Юэ", Clan = Clan.GuYue, Centre = GuYue, Radius = 300, Walled = true, TileSize = 1600,
                Downhill = new Vector2(-0.4655f, -0.8850f), RectMiddle = new Vector2(40, 60), RectHalf = new Vector2(540, 250) },
            new Settlement { Id = "xiong", Name = "Деревня Сюн", Clan = Clan.Xiong, Centre = Xiong, Radius = 150, Walled = true, TileSize = 625 },
            new Settlement { Id = "bai", Name = "Деревня Бай", Clan = Clan.Bai, Centre = Bai, Radius = 155, Walled = true, TileSize = 625 },
            new Settlement { Id = "hamlet_willow", Name = "Хутор Ивовый", Clan = Clan.Mortal, Centre = new Vector2(-5150, -6150), Radius = 55 },
            new Settlement { Id = "hamlet_ford", Name = "Хутор у Брода", Clan = Clan.Mortal, Centre = new Vector2(-900, -7300), Radius = 50 },
            new Settlement { Id = "hamlet_terraces", Name = "Хутор Террасы", Clan = Clan.Mortal, Centre = new Vector2(5400, -7400), Radius = 50 },
            new Settlement { Id = "hamlet_west", Name = "Западный хутор", Clan = Clan.Mortal, Centre = new Vector2(-7300, 1300), Radius = 45 },
            new Settlement { Id = "hamlet_north", Name = "Северный хутор", Clan = Clan.Mortal, Centre = new Vector2(600, 8350), Radius = 45 },
            // "Tens of tiny mortal villages" at the foot (ch. 65): a dozen here, round the mountain on the way at its foot.
            new Settlement { Id = "hamlet_pine", Name = "Хутор Сосновый", Clan = Clan.Mortal, Centre = new Vector2(-7600, -3400), Radius = 40 },
            new Settlement { Id = "hamlet_mill", Name = "Хутор у Мельницы", Clan = Clan.Mortal, Centre = new Vector2(2400, -8600), Radius = 45 },
            new Settlement { Id = "hamlet_east", Name = "Восточный хутор", Clan = Clan.Mortal, Centre = new Vector2(8300, -3200), Radius = 40 },
            // Honey is Bear Strength Gu's food, and the Xiong want a lot of it (ch. 65): beekeepers below the fore mountain.
            new Settlement { Id = "hamlet_honey", Name = "Хутор Медовый", Clan = Clan.Mortal, Centre = new Vector2(7900, 2600), Radius = 42 },
            new Settlement { Id = "hamlet_stone", Name = "Хутор Каменный", Clan = Clan.Mortal, Centre = new Vector2(5200, 7600), Radius = 40 },
            new Settlement { Id = "hamlet_mist", Name = "Хутор Туманный", Clan = Clan.Mortal, Centre = new Vector2(-5600, 6900), Radius = 40 },
            new Settlement { Id = "hamlet_bamboo", Name = "Хутор Бамбуковый", Clan = Clan.Mortal, Centre = new Vector2(-8400, -1000), Radius = 38 },
        };

        public static Settlement Find(string id) => Array.Find(Settlements, s => s.Id == id);

        /// <summary>The location's settings: the box the hero may be in, and where a new game begins (the builder knows the ground's height there).</summary>
        public static LocationSettings Settings(Vector3 spawn, float spawnYaw) => LocationSettings.FromMinMax(
            Id,
            Title,
            new Vector3(-Half + 20, -60, -Half + 20),
            new Vector3(Half - 20, MaxHeight, Half - 20),
            spawn,
            spawnYaw);

        public static float ToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            var t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(1e-4f, ab.sqrMagnitude));

            return Vector2.Distance(p, a + ab * t);
        }

        /// <summary>The ground's height under a point: the finest terrain there (a village's own, else the mountain's).</summary>
        public static float GroundAt(Vector3 at)
        {
            // The terrains and their squares, looked up once a frame in the game (Terrain.activeTerrains makes a new array each time
            // it is read); every time in the editor, where the builder makes terrains between two looks.
            if (!Application.isPlaying || groundFrame != Time.frameCount)
            {
                groundFrame = Time.frameCount;
                grounds.Clear();

                foreach (var terrain in Terrain.activeTerrains)
                {
                    if (terrain != null && terrain.terrainData != null)
                    {
                        grounds.Add((terrain, terrain.GetPosition(), terrain.terrainData.size));
                    }
                }
            }

            var best = -1;
            var size = float.PositiveInfinity;

            for (var i = 0; i < grounds.Count; i++)
            {
                var (_, corner, extent) = grounds[i];

                if (at.x >= corner.x && at.x <= corner.x + extent.x && at.z >= corner.z && at.z <= corner.z + extent.z && extent.x < size)
                {
                    best = i;
                    size = extent.x;
                }
            }

            return best >= 0 && grounds[best].terrain != null ? grounds[best].terrain.SampleHeight(at) + grounds[best].corner.y : at.y;
        }

        static int groundFrame = -1;
        static readonly System.Collections.Generic.List<(Terrain terrain, Vector3 corner, Vector3 extent)> grounds = new System.Collections.Generic.List<(Terrain, Vector3, Vector3)>();
    }
}
