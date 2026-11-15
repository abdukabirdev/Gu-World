using System;
using System.Collections.Generic;
using GuWorld.Core.Gen.Flora;
using static GuWorld.Core.Gen.Buildings.BuildingGen;

namespace GuWorld.Core.Gen.Buildings
{
    /// <summary>A place in the clan's underground: what it is, where, and what the game finds there.</summary>
    public struct DepthsSpot
    {
        public V3 At;
        public float Facing;
        /// <summary>"entry", "guard", "spring", "gu_1", "gu_2", "gu_3", "forbidden", "secret_hall", "coffin".</summary>
        public string Role;
        public string Name;
    }

    /// <summary>A light of the underground: the spring's pale glow, the lanterns of the rooms, the blood lake's red.</summary>
    public struct DepthsLight
    {
        public V3 At;
        public Tint Colour;
        public float Range, Intensity;
    }

    public sealed class DepthsModel
    {
        /// <summary>Levels of detail (0 near, 1 the rest), each parts by material as a building's.</summary>
        public readonly TreeLod[] Lods = new TreeLod[2];
        public MeshData Collision;
        public readonly List<DepthsSpot> Spots = new List<DepthsSpot>();
        public readonly List<DepthsLight> Lights = new List<DepthsLight>();
        /// <summary>The deepest floor, metres below the entry.</summary>
        public float Deepest;
    }

    /// <summary>
    /// The clan's underground under its great building (Docs/V2/PLAN-TOWNS.md; for the Gu Yue, ch. 4–5,
    /// 15, 156–158, 178, 182–186): a stone stair down from the pavilion's cellar; the spirit spring's
    /// cave with its pool of primeval stone and the sea of moon orchids round it, the Dark Hall's post
    /// at its mouth; a corridor east to the clan's Gu rooms — the first rank's, the second's (fewer,
    /// better), the third's — and at its end the forbidden room behind a sealed stone door; a dark
    /// tunnel west to a stone door and the Secret Hall's rows of stone tables with jade plates of
    /// recipes, lit by water-light Gu on its walls; and north, down a winding tunnel and a fifteen
    /// metre shaft, the blood pond graveyard: a cave of red earth, hundreds of holes in its walls
    /// running with water, a lake of blood and in its middle, over a slow whirlpool, the crimson
    /// coffin of the clan's first head, some fifty metres under the village.
    /// The entry (the top of the stair) at the origin, the way in toward +Z, 1 unit = 1 m.
    /// </summary>
    public static class DepthsGen
    {
        sealed class Hollow
        {
            public V3 Centre;
            public float Rx, Ry, Rz, Floor;
            public Tint Tint;
        }

        sealed class Tube
        {
            public List<V3> Points;
            public float Radius;
        }

        /// <summary>A room cut square in the rock: its floor's corners and height.</summary>
        sealed class Cut
        {
            public V3 Min, Max;
        }

        sealed class Plan
        {
            public readonly List<Hollow> Hollows = new List<Hollow>();
            public readonly List<Tube> Tubes = new List<Tube>();
            public readonly List<Cut> Cuts = new List<Cut>();

            public bool InHollow(V3 p, Hollow except, float k = 0.94f)
            {
                foreach (var h in Hollows)
                {
                    if (h == except)
                    {
                        continue;
                    }

                    var d = p - h.Centre;

                    if (p.Y > h.Floor - 0.6f && d.X * d.X / (h.Rx * h.Rx) + d.Y * d.Y / (h.Ry * h.Ry) + d.Z * d.Z / (h.Rz * h.Rz) < k)
                    {
                        return true;
                    }
                }

                return false;
            }

            public bool InTube(V3 p, Tube except, float k = 0.92f)
            {
                foreach (var t in Tubes)
                {
                    if (t == except)
                    {
                        continue;
                    }

                    for (var i = 1; i < t.Points.Count; i++)
                    {
                        if (ToSegment(p, t.Points[i - 1], t.Points[i]) < t.Radius * k)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }

            public bool InCut(V3 p, Cut except, float margin = 0.15f)
            {
                foreach (var c in Cuts)
                {
                    if (c == except)
                    {
                        continue;
                    }

                    if (p.X > c.Min.X + margin && p.X < c.Max.X - margin && p.Y > c.Min.Y - 0.4f && p.Y < c.Max.Y - margin && p.Z > c.Min.Z + margin && p.Z < c.Max.Z - margin)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        static float ToSegment(V3 p, V3 a, V3 b)
        {
            var ab = b - a;
            var t = Math.Max(0, Math.Min(1, V3.Dot(p - a, ab) / Math.Max(1e-6f, V3.Dot(ab, ab))));

            return (a + ab * t - p).Length;
        }

        static readonly Tint Rock = new Tint(0.78f, 0.76f, 0.72f);
        static readonly Tint RedEarth = new Tint(0.78f, 0.36f, 0.28f);
        static readonly Tint SpringLight = new Tint(0.55f, 0.82f, 0.95f);
        static readonly Tint Orchid = new Tint(0.5f, 0.66f, 1f);
        static readonly Tint Blood = new Tint(0.42f, 0.03f, 0.04f);

        public static DepthsModel Make(string clan, int seed)
        {
            var model = new DepthsModel();
            var plan = new Plan();

            // ---------------------------------------------------------------- The layout

            // The stair from the cellar: straight down into the mountain, 16 m in 32.
            var stair = new Cut { Min = new V3(-1.8f, -16, 0), Max = new V3(1.8f, 3.6f, 33) };
            // The spring's cave.
            var spring = new Hollow { Centre = new V3(0, -13, 52), Rx = 24, Ry = 11, Rz = 20, Floor = -16, Tint = Rock };
            // The Gu rooms' corridor east, its rooms north of it, the forbidden room at its end.
            var corridor = new Cut { Min = new V3(20, -16, 50.5f), Max = new V3(64, -12.4f, 53.5f) };
            var rooms = new[]
            {
                (cut: new Cut { Min = new V3(26, -16, 53.5f), Max = new V3(38, -12, 63.5f) }, door: 32f, rank: 1),
                (cut: new Cut { Min = new V3(41, -16, 53.5f), Max = new V3(50, -12.2f, 61.5f) }, door: 45.5f, rank: 2),
                (cut: new Cut { Min = new V3(53, -16, 53.5f), Max = new V3(59, -12.4f, 59.5f) }, door: 56f, rank: 3),
            };
            var forbidden = new Cut { Min = new V3(64, -16, 46), Max = new V3(74, -11.5f, 58) };
            // The dark way west to the Secret Hall.
            var dark = new Tube { Radius = 1.7f, Points = new List<V3> { new V3(-19, -14.9f, 54), new V3(-30, -14.9f, 58), new V3(-41, -14.9f, 52), new V3(-52.6f, -14.9f, 56) } };
            var secret = new Cut { Min = new V3(-70, -16, 49), Max = new V3(-53, -11.5f, 63) };
            // The way down: a winding tunnel north, a shaft, a short tunnel into the graveyard.
            var down = new Tube { Radius = 2.2f, Points = new List<V3> { new V3(0, -14.6f, 68), new V3(5, -17, 79), new V3(-3, -21, 90), new V3(0, -23.6f, 100), new V3(0, -23.6f, 103) } };
            var shaft = new Tube { Radius = 3.2f, Points = new List<V3> { new V3(0, -21.5f, 105.5f), new V3(0, -41.5f, 105.5f) } };
            var under = new Tube { Radius = 2.4f, Points = new List<V3> { new V3(0, -38.6f, 105.5f), new V3(0, -39.6f, 113), new V3(3, -43.6f, 122), new V3(2, -48.4f, 131) } };
            var grave = new Hollow { Centre = new V3(0, -44, 160), Rx = 44, Ry = 17, Rz = 34, Floor = -50, Tint = RedEarth };

            plan.Hollows.Add(spring);
            plan.Hollows.Add(grave);
            plan.Tubes.Add(dark);
            plan.Tubes.Add(down);
            plan.Tubes.Add(shaft);
            plan.Tubes.Add(under);
            plan.Cuts.Add(stair);
            plan.Cuts.Add(corridor);
            plan.Cuts.Add(forbidden);
            plan.Cuts.Add(secret);

            foreach (var room in rooms)
            {
                plan.Cuts.Add(room.cut);
            }

            model.Deepest = -grave.Floor;

            for (var lod = 0; lod < 2; lod++)
            {
                var c = new Ctx
                {
                    S = Buildings.House, Seed = seed, Lod = lod, Out = new TreeLod(), Noise = new Noise(seed), Wealth = 0.8f, Wear = 0.5f,
                    Frame = new Tint(0.2f, 0.135f, 0.095f), Accent = Factions.Accent(clan), Wall = new Tint(0.9f, 0.87f, 0.8f), Roof = White, Leaf = White,
                    WallTex = "stone", DadoTex = "stone", RoofTexName = "tile", Moon = Factions.Clan(clan) == "gu_yue", Paper = Factions.Lantern(clan + ".dark"),
                };
                var rng = new Rng(seed * 811 + 7);

                // ------------------------------------------------------------ Rock
                Shell(c, plan, spring);
                Shell(c, plan, grave);
                TubeShell(c, plan, dark, Rock.Times(0.8f));
                TubeShell(c, plan, down, Rock);
                TubeShell(c, plan, shaft, Rock.Times(0.9f));
                TubeShell(c, plan, under, Lerp(Rock, RedEarth, 0.6f));

                // ------------------------------------------------------------ The stair down from the cellar
                Room(c, plan, stair, new[] { (2, 0f, 3.6f, 3.4f) }, true, "stone");
                Steps(c, new V3(0, 0, 1.2f), new V3(0, -16, 32.4f), 3.2f);
                c.Paper = Factions.Lantern(clan);

                for (var k = 0; k < 4 && lod == 0; k++)
                {
                    var z = 4 + k * 8.5f;
                    var y = -z * 0.5f + 2.8f;
                    Lamp(c, new V3(1.55f, y + 0.4f, z), 0.2f);
                    Beam(c, c.M("wood"), new V3(1.8f, y + 0.4f, z), new V3(1.55f, y + 0.4f, z), 0.05f, 0.05f, c.Frame, true);
                }

                // ------------------------------------------------------------ The spring's cave
                Spring(c, spring, ref rng);
                c.Paper = Factions.Lantern(clan + ".dark");
                DarkPost(c, clan, seed);

                // ------------------------------------------------------------ The Gu rooms and the forbidden room
                Room(c, plan, corridor, new[] { (3, 52f, 3f, 3.4f), (1, 52f, 3f, 3.4f) }.Concat(rooms, r => (2, r.door, 1.6f, 2.4f)), false, "stone");

                foreach (var (cut, door, rank) in rooms)
                {
                    Room(c, plan, cut, new[] { (0, door, 1.6f, 2.4f) }, false, "stone");
                    GuRoom(c, cut, rank, ref rng);
                }

                Room(c, plan, forbidden, new[] { (3, 52f, 2.4f, 3.2f) }, false, "stone");
                Forbidden(c, forbidden, ref rng);

                // ------------------------------------------------------------ The Secret Hall
                Room(c, plan, secret, new[] { (1, 56f, 2.4f, 3f) }, false, "stone");
                SecretHall(c, secret, ref rng);

                // ------------------------------------------------------------ The way down and the blood pond
                SpiralStair(c, new V3(0, -40.2f, 105.5f), 15.2f, 2.9f);
                BloodPond(c, grave, ref rng);

                model.Lods[lod] = c.Out;
            }

            // The solid: the coarse level's rock, stone and wood (not the water's glow, not the flowers).
            var solid = new MeshData();

            foreach (var part in model.Lods[1].Parts)
            {
                if (part.Material.StartsWith("tex:"))
                {
                    Build.Append(solid, part.Mesh, new V3(0, 0, 0));
                }
            }

            model.Collision = solid;

            // ---------------------------------------------------------------- Where things are, the lights
            void Spot(V3 at, float facing, string role, string name) => model.Spots.Add(new DepthsSpot { At = at, Facing = facing, Role = role, Name = name });
            void Light(V3 at, Tint colour, float range, float intensity) => model.Lights.Add(new DepthsLight { At = at, Colour = colour, Range = range, Intensity = intensity });

            Spot(new V3(0, 0, 1.6f), 0, "entry", "Лестница из подвала павильона");
            Spot(new V3(-1.6f, -16, 35.2f), 180, "guard", "Страж Тёмного зала");
            Spot(new V3(1.6f, -16, 35.2f), 180, "guard", "Страж Тёмного зала");
            Spot(new V3(0, -16, 44), 0, "spring", "Духовный источник клана");
            Spot(new V3(32, -16, 58.5f), 0, "gu_1", "Комната Гу первого ранга");
            Spot(new V3(45.5f, -16, 57.5f), 0, "gu_2", "Комната Гу второго ранга");
            Spot(new V3(56, -16, 56.5f), 0, "gu_3", "Комната редких Гу");
            Spot(new V3(69, -16, 52), 90, "forbidden", "Запретная комната");
            Spot(new V3(-61.5f, -16, 56), -90, "secret_hall", "Тайный зал");
            Spot(new V3(0, -50, 132), 0, "coffin", "Кровавый пруд-кладбище");

            for (var k = 0; k < 4; k++)
            {
                Light(new V3(1.2f, -k * 4.25f - 1.4f, 4 + k * 8.5f), new Tint(1f, 0.75f, 0.5f), 8, 1.1f);
            }

            Light(new V3(0, -15, 37), Factions.Lantern(clan + ".dark"), 9, 1.2f);
            Light(new V3(0, -11, 54), SpringLight, 30, 2.2f);
            Light(new V3(-13, -13, 54), Orchid, 16, 0.9f);
            Light(new V3(13, -13, 54), Orchid, 16, 0.9f);
            Light(new V3(42, -13, 52), new Tint(1f, 0.75f, 0.5f), 14, 1f);

            foreach (var (cut, _, _) in rooms)
            {
                Light((cut.Min + cut.Max) * 0.5f + new V3(0, 1.2f, 0), new Tint(1f, 0.75f, 0.5f), 9, 1f);
            }

            Light(new V3(69, -13, 52), new Tint(1f, 0.6f, 0.25f), 10, 1.2f);
            Light(new V3(-61.5f, -12.5f, 56), new Tint(0.45f, 0.95f, 0.85f), 16, 1.4f);
            Light(new V3(0, -32, 105.5f), new Tint(1f, 0.7f, 0.45f), 14, 1.4f);
            Light(new V3(0, -36, 160), new Tint(1f, 0.25f, 0.2f), 60, 2.4f);
            Light(new V3(0, -46, 164), new Tint(1f, 0.3f, 0.25f), 16, 1.6f);

            return model;
        }

        static Tint Lerp(Tint a, Tint b, float t) => new Tint(a.R + (b.R - a.R) * t, a.G + (b.G - a.G) * t, a.B + (b.B - a.B) * t);

        static IEnumerable<T> Concat<T, U>(this T[] first, U[] more, Func<U, T> map)
        {
            foreach (var x in first)
            {
                yield return x;
            }

            foreach (var x in more)
            {
                yield return map(x);
            }
        }

        // ------------------------------------------------------------------ Rock shells

        /// <summary>
        /// A cave: a dome of rock over a floor, its wall bulging and hollowed by the noise, open where a
        /// tunnel or a room cuts into it; smooth-shaded, facing in. Darker the deeper and the higher up.
        /// </summary>
        static void Shell(Ctx c, Plan plan, Hollow h)
        {
            var rock = c.M("rock");
            var around = c.Lod == 0 ? 72 : 32;
            var up = c.Lod == 0 ? 18 : 8;
            var rings = c.Lod == 0 ? 10 : 5;
            var start = (float)Math.Asin(Math.Max(-0.99f, Math.Min(0.99f, (h.Floor - h.Centre.Y) / h.Ry)));
            var size = Size("rock");
            var grid = new int[around + 1, up + 1];
            var points = new V3[around + 1, up + 1];

            for (var i = 0; i <= around; i++)
            {
                var phi = Build.Tau * i / around;

                for (var j = 0; j <= up; j++)
                {
                    var theta = start + ((float)Math.PI / 2 - start) * (float)Math.Pow(j / (float)up, 0.85f);
                    var cos = (float)Math.Cos(theta);
                    var bulge = 1 + 0.16f * c.Noise.Fbm((float)Math.Cos(phi) * 2.2f + 7, (float)Math.Sin(phi) * 2.2f + theta * 1.8f, 3) + 0.05f * c.Noise.Perlin(phi * 9, theta * 9);
                    var p = h.Centre + new V3(h.Rx * cos * (float)Math.Cos(phi) * bulge, h.Ry * (float)Math.Sin(theta) * (j == 0 ? 1 : bulge * 0.9f + 0.1f), h.Rz * cos * (float)Math.Sin(phi) * bulge);

                    if (j == 0)
                    {
                        p.Y = h.Floor;
                    }

                    points[i % around == 0 && i > 0 ? 0 : i, j] = p;
                }
            }

            for (var j = 0; j <= up; j++)
            {
                points[around, j] = points[0, j];
            }

            for (var i = 0; i <= around; i++)
            {
                for (var j = 0; j <= up; j++)
                {
                    var p = points[i, j];
                    // The normal: across the grid, turned toward the cave's middle.
                    var a = points[(i + 1) % (around + 1) == 0 ? 1 : (i + 1) % (around + 1), j] - points[i == 0 ? around - 1 : i - 1, j];
                    var b = points[i, Math.Min(up, j + 1)] - points[i, Math.Max(0, j - 1)];
                    var n = V3.Cross(a, b).Normalized;

                    if (V3.Dot(n, h.Centre + new V3(0, 0, 0) - p) < 0)
                    {
                        n = n * -1;
                    }

                    var dark = 0.55f + 0.45f * (float)Math.Exp(-(p.Y - h.Floor) / (h.Ry * 0.9f));
                    var tone = (0.85f + 0.3f * c.Noise.Perlin(p.X * 0.13f, p.Z * 0.13f + p.Y * 0.1f)) * dark;
                    var u = Build.Tau * i / around * (h.Rx + h.Rz) * 0.5f / size;
                    var v = (p.Y - h.Floor) / size;
                    var index = rock.Vertex(p.X, p.Y, p.Z, u, v, h.Tint.R * tone, h.Tint.G * tone, h.Tint.B * tone, 1);
                    rock.SetNormal(index, n.X, n.Y, n.Z);
                    grid[i, j] = index;
                }
            }

            for (var i = 0; i < around; i++)
            {
                for (var j = 0; j < up; j++)
                {
                    var mid = (points[i, j] + points[i + 1, j] + points[i, j + 1] + points[i + 1, j + 1]) * 0.25f;

                    if (plan.InTube(mid, null) || plan.InCut(mid, null, -0.4f) || plan.InHollow(mid, h))
                    {
                        continue;
                    }

                    Wind(rock, grid[i, j], grid[i + 1, j], grid[i + 1, j + 1], grid[i, j + 1], h.Centre - mid);
                }
            }

            // The floor: from the middle out to the dome's foot, lumpy in the middle, meeting the wall exactly.
            var floor = c.M("rock");
            var centre = floor.Vertex(h.Centre.X, h.Floor, h.Centre.Z, h.Centre.X / size, h.Centre.Z / size, h.Tint.R * 0.8f, h.Tint.G * 0.8f, h.Tint.B * 0.8f, 1);
            floor.SetNormal(centre, 0, 1, 0);
            var ring = new int[rings + 1, around + 1];

            for (var r = 1; r <= rings; r++)
            {
                var t = r / (float)rings;

                for (var i = 0; i <= around; i++)
                {
                    var edge = points[i, 0];
                    var p = h.Centre + (edge - h.Centre) * t;
                    p.Y = h.Floor + (r < rings ? 0.35f * c.Noise.Fbm(p.X * 0.12f, p.Z * 0.12f, 2) * (1 - t * t) : 0);
                    var tone = 0.7f + 0.2f * c.Noise.Perlin(p.X * 0.2f, p.Z * 0.2f);
                    var index = floor.Vertex(p.X, p.Y, p.Z, p.X / size, p.Z / size, h.Tint.R * tone, h.Tint.G * tone, h.Tint.B * tone, 1);
                    floor.SetNormal(index, 0, 1, 0);
                    ring[r, i] = index;
                }
            }

            for (var i = 0; i < around; i++)
            {
                floor.Triangle(centre, ring[1, i + 1], ring[1, i]);

                for (var r = 1; r < rings; r++)
                {
                    floor.Triangle(ring[r, i], ring[r, i + 1], ring[r + 1, i + 1]);
                    floor.Triangle(ring[r, i], ring[r + 1, i + 1], ring[r + 1, i]);
                }
            }
        }

        /// <summary>A tunnel along a line: a round bore with a flat floor, facing in, open where it meets a cave or a room.</summary>
        static void TubeShell(Ctx c, Plan plan, Tube t, Tint tint)
        {
            var rock = c.M("rock");
            var sides = c.Lod == 0 ? 14 : 8;
            var size = Size("rock");
            var line = Lines3(t.Points, c.Lod == 0 ? 1.5f : 3f);
            var grid = new int[line.Count, sides + 1];
            var points = new V3[line.Count, sides + 1];
            var vertical = Math.Abs(t.Points[t.Points.Count - 1].Y - t.Points[0].Y) > (t.Points[t.Points.Count - 1] - t.Points[0]).Length * 0.9f;
            var along = 0f;

            for (var k = 0; k < line.Count; k++)
            {
                var forward = (line[Math.Min(line.Count - 1, k + 1)] - line[Math.Max(0, k - 1)]).Normalized;
                var side = vertical ? new V3(1, 0, 0) : V3.Cross(V3.Up, forward).Normalized;
                var upward = V3.Cross(forward, side).Normalized;
                along += k > 0 ? (line[k] - line[k - 1]).Length : 0;

                for (var i = 0; i <= sides; i++)
                {
                    var a = Build.Tau * i / sides;
                    var wobble = 1 + 0.14f * c.Noise.Perlin(along * 0.35f + i * 0.7f, k * 0.2f + 3);
                    var x = (float)Math.Cos(a) * t.Radius * wobble;
                    var y = (float)Math.Sin(a) * t.Radius * wobble;

                    // A walked tunnel's floor is flat.
                    if (!vertical)
                    {
                        y = Math.Max(y, -t.Radius * 0.65f);
                    }

                    points[k, i] = line[k] + side * x + upward * y;
                }
            }

            for (var k = 0; k < line.Count; k++)
            {
                for (var i = 0; i <= sides; i++)
                {
                    var p = points[k, i];
                    var axis = line[k];
                    var n = (axis - p).Normalized;
                    var tone = 0.75f + 0.25f * c.Noise.Perlin(p.X * 0.3f, p.Z * 0.3f + p.Y * 0.2f);
                    var index = rock.Vertex(p.X, p.Y, p.Z, i * Build.Tau * t.Radius / sides / size, (k == 0 ? 0 : along * k / line.Count) / size, tint.R * tone, tint.G * tone, tint.B * tone, 1);
                    rock.SetNormal(index, n.X, n.Y, n.Z);
                    grid[k, i] = index;
                }
            }

            for (var k = 0; k < line.Count - 1; k++)
            {
                for (var i = 0; i < sides; i++)
                {
                    var mid = (points[k, i] + points[k + 1, i] + points[k, i + 1] + points[k + 1, i + 1]) * 0.25f;

                    if (plan.InHollow(mid, null) || plan.InCut(mid, null, -0.3f) || plan.InTube(mid, t, 0.97f))
                    {
                        continue;
                    }

                    Wind(rock, grid[k, i], grid[k + 1, i], grid[k + 1, i + 1], grid[k, i + 1], (line[k] + line[k + 1]) * 0.5f - mid);
                }
            }
        }

        /// <summary>A line resampled every `step` metres.</summary>
        static List<V3> Lines3(List<V3> points, float step)
        {
            var result = new List<V3> { points[0] };

            for (var i = 1; i < points.Count; i++)
            {
                var a = points[i - 1];
                var b = points[i];
                var n = Math.Max(1, (int)Math.Ceiling((b - a).Length / step));

                for (var k = 1; k <= n; k++)
                {
                    result.Add(V3.Lerp(a, b, k / (float)n));
                }
            }

            return result;
        }

        // ------------------------------------------------------------------ Rooms cut square

        /// <summary>
        /// A room cut square in the rock: dressed stone walls on a stone floor, a ceiling of beams and
        /// planks, timber posts in its corners. `openings`: (side 0 south −Z, 1 east +X, 2 north +Z, 3 west −X,
        /// the opening's middle along that side in world metres, its width, its height). A sloping room (the stair) slopes its floor and ceiling.
        /// </summary>
        static void Room(Ctx c, Plan plan, Cut room, IEnumerable<(int side, float at, float width, float height)> openings, bool sloping, string tex)
        {
            var walls = c.M(tex);
            var min = room.Min;
            var max = room.Max;
            var list = new List<(int side, float at, float width, float height)>(openings);
            float FloorAt(float z) => sloping ? Math.Max(min.Y, Math.Min(0, -(z - 1.2f) * 0.5f)) : min.Y;
            float Ceil(float z) => FloorAt(z) + (sloping ? 3.6f : max.Y - min.Y);
            var corners = new[] { new V3(min.X, 0, min.Z), new V3(max.X, 0, min.Z), new V3(max.X, 0, max.Z), new V3(min.X, 0, max.Z) };
            var inward = new[] { new V3(0, 0, 1), new V3(-1, 0, 0), new V3(0, 0, -1), new V3(1, 0, 0) };

            for (var side = 0; side < 4; side++)
            {
                var a = corners[side];
                var b = corners[(side + 1) % 4];
                var length = (b - a).Length;
                var dir = (b - a) * (1 / length);
                // The wall in pieces between its openings: a piece every couple of metres (so a sloping one follows the floor).
                var gaps = list.FindAll(o => o.side == side);
                var cuts = new List<(float from, float to, float top)>();

                foreach (var o in gaps)
                {
                    var t = side % 2 == 0 ? o.at - min.X : o.at - min.Z;

                    if (side >= 2)
                    {
                        t = length - t;
                    }

                    cuts.Add((t - o.width * 0.5f, t + o.width * 0.5f, o.height));
                }

                var step = sloping ? 2f : length;

                for (var s = 0f; s < length - 0.01f; s += step)
                {
                    var s1 = Math.Min(length, s + step);
                    var pieces = new List<(float from, float to)> { (s, s1) };

                    foreach (var (from, to, top) in cuts)
                    {
                        var next = new List<(float, float)>();

                        foreach (var (p0, p1) in pieces)
                        {
                            if (to <= p0 || from >= p1)
                            {
                                next.Add((p0, p1));
                                continue;
                            }

                            if (from > p0)
                            {
                                next.Add((p0, from));
                            }

                            if (to < p1)
                            {
                                next.Add((to, p1));
                            }

                            // Over the opening: a lintel of wall from its top up.
                            var l0 = Math.Max(p0, from);
                            var l1 = Math.Min(p1, to);
                            var q0 = a + dir * l0;
                            var q1 = a + dir * l1;
                            var y0 = FloorAt(q0.Z);
                            var y1 = FloorAt(q1.Z);
                            Quad(c, walls, new V3(q0.X, y0 + top, q0.Z), new V3(q1.X, y1 + top, q1.Z), new V3(q1.X, Ceil(q1.Z), q1.Z), new V3(q0.X, Ceil(q0.Z), q0.Z), inward[side], White.Times(0.8f), tex);

                            if (c.Lod == 0)
                            {
                                // The opening's frame: two posts and a lintel beam.
                                var w = c.M("wood");
                                Beam(c, w, new V3(q0.X, y0, q0.Z) + inward[side] * 0.12f, new V3(q0.X, y0 + top, q0.Z) + inward[side] * 0.12f, 0.2f, 0.2f, c.Frame.Times(1.2f));
                                Beam(c, w, new V3(q1.X, y1, q1.Z) + inward[side] * 0.12f, new V3(q1.X, y1 + top, q1.Z) + inward[side] * 0.12f, 0.2f, 0.2f, c.Frame.Times(1.2f));
                                Beam(c, w, new V3(q0.X, y0 + top, q0.Z) + inward[side] * 0.12f, new V3(q1.X, y1 + top, q1.Z) + inward[side] * 0.12f, 0.24f, 0.24f, c.Frame.Times(1.2f));
                            }
                        }

                        pieces = next;
                    }

                    foreach (var (p0, p1) in pieces)
                    {
                        if (p1 - p0 < 0.02f)
                        {
                            continue;
                        }

                        var q0 = a + dir * p0;
                        var q1 = a + dir * p1;
                        Quad(c, walls, new V3(q0.X, FloorAt(q0.Z) - 0.05f, q0.Z), new V3(q1.X, FloorAt(q1.Z) - 0.05f, q1.Z), new V3(q1.X, Ceil(q1.Z), q1.Z), new V3(q0.X, Ceil(q0.Z), q0.Z), inward[side], White.Times(0.82f), tex);
                    }
                }
            }

            // The floor (a stair has its steps) and the ceiling.
            if (!sloping)
            {
                Quad(c, c.M("stone"), new V3(min.X, min.Y, min.Z), new V3(max.X, min.Y, min.Z), new V3(max.X, min.Y, max.Z), new V3(min.X, min.Y, max.Z), V3.Up, White.Times(0.7f), "stone");
            }

            var plank = c.M("plank");

            for (var z = min.Z; z < max.Z - 0.01f; z += sloping ? 2 : max.Z - min.Z)
            {
                var z1 = Math.Min(max.Z, z + (sloping ? 2 : max.Z - min.Z));
                Quad(c, plank, new V3(min.X, Ceil(z), z), new V3(max.X, Ceil(z), z), new V3(max.X, Ceil(z1), z1), new V3(min.X, Ceil(z1), z1), new V3(0, -1, 0), c.Frame.Times(1.6f), "plank");
            }

            if (c.Lod == 0)
            {
                // Beams across the ceiling, posts in the corners.
                var w = c.M("wood");
                var spanX = max.X - min.X < max.Z - min.Z;

                for (var t = 1.2f; t < (spanX ? max.Z - min.Z : max.X - min.X); t += 2.4f)
                {
                    if (spanX)
                    {
                        var z = min.Z + t;
                        Beam(c, w, new V3(min.X, Ceil(z) - 0.15f, z), new V3(max.X, Ceil(z) - 0.15f, z), 0.22f, 0.26f, c.Frame);
                    }
                    else
                    {
                        var x = min.X + t;
                        Beam(c, w, new V3(x, Ceil(min.Z) - 0.15f, min.Z), new V3(x, Ceil(max.Z) - 0.15f, max.Z), 0.22f, 0.26f, c.Frame);
                    }
                }

                if (!sloping)
                {
                    foreach (var p in corners)
                    {
                        var x = p.X + (p.X < (min.X + max.X) * 0.5f ? 0.15f : -0.15f);
                        var z = p.Z + (p.Z < (min.Z + max.Z) * 0.5f ? 0.15f : -0.15f);
                        Post(c, w, x, z, min.Y, max.Y, 0.16f, 8, c.Frame.Times(1.1f));
                    }
                }
            }
        }

        /// <summary>Stone steps from one landing down to another.</summary>
        static void Steps(Ctx c, V3 top, V3 bottom, float width)
        {
            var stone = c.M("stone");
            var drop = top.Y - bottom.Y;
            var n = Math.Max(1, (int)Math.Round(drop / (c.Lod == 0 ? 0.2f : 0.8f)));
            var run = (bottom.Z - top.Z) / n;

            for (var i = 0; i < n; i++)
            {
                var y = top.Y - drop * (i + 1) / n;
                var z = top.Z + run * i;
                Box(c, stone, new V3(-width * 0.5f, y - 0.25f, z), new V3(width * 0.5f, y + 0.001f, z + run + 0.02f), White.Times(0.75f + 0.1f * (i % 3) / 2), "stone");
            }

            // The landing at the top, under the cellar's trapdoor.
            Box(c, stone, new V3(-width * 0.5f - 0.3f, top.Y - 0.3f, -0.1f), new V3(width * 0.5f + 0.3f, top.Y, top.Z), White.Times(0.8f), "stone");
        }

        /// <summary>A stair turning round a post down a shaft: plank treads on brackets, a rope rail.</summary>
        static void SpiralStair(Ctx c, V3 bottom, float height, float radius)
        {
            var wood = c.M("wood");
            var plank = c.M("plank");
            Post(c, c.M("log"), bottom.X, bottom.Z, bottom.Y - 0.5f, bottom.Y + height + 0.5f, 0.32f, c.Lod == 0 ? 12 : 6, new Tint(0.85f, 0.8f, 0.74f));
            var steps = (int)(height / 0.24f);

            for (var i = 0; i < steps; i++)
            {
                var a = i * 0.42f;
                var y = bottom.Y + height - (i + 1) * height / steps;
                var dir = new V3((float)Math.Cos(a), 0, (float)Math.Sin(a));
                var side = new V3(-dir.Z, 0, dir.X);
                var inner = bottom + new V3(0, y - bottom.Y, 0) + dir * 0.3f;
                var outer = bottom + new V3(0, y - bottom.Y, 0) + dir * (radius - 0.4f);
                Quad(c, plank, inner - side * 0.25f, outer - side * 0.55f, outer + side * 0.55f, inner + side * 0.25f, V3.Up, c.Frame.Times(2f), "plank");

                if (c.Lod == 0 && i % 3 == 0)
                {
                    Beam(c, wood, outer, outer + new V3(0, 1.0f, 0), 0.05f, 0.05f, c.Frame);
                }
            }

            // The landing at the foot.
            Box(c, c.M("stone"), bottom + new V3(-radius, -0.4f, -radius), bottom + new V3(radius, 0, radius), White.Times(0.7f), "stone");
        }

        // ------------------------------------------------------------------ The spring

        /// <summary>
        /// The spirit spring: a pool ringed with stones, its water pale and glowing, clusters of primeval
        /// stone growing up round it like crystal; round it on low terraces the sea of moon orchids,
        /// their pale blue flowers glowing in the dark (the Awakening Ceremony's flower sea).
        /// </summary>
        static void Spring(Ctx c, Hollow h, ref Rng rng)
        {
            var at = new V3(h.Centre.X, h.Floor, h.Centre.Z + 2);
            var sides = c.Lod == 0 ? 28 : 12;

            // The rim: a ring of rough stones; the water inside it.
            for (var i = 0; i < sides; i++)
            {
                var a0 = Build.Tau * i / sides;
                var a1 = Build.Tau * (i + 1) / sides;
                var r0 = 5.2f + 0.3f * c.Noise.Perlin(i * 0.7f, 1);
                var p0 = at + new V3((float)Math.Cos(a0) * r0, 0, (float)Math.Sin(a0) * r0);
                var p1 = at + new V3((float)Math.Cos(a1) * r0, 0, (float)Math.Sin(a1) * r0);
                Beam(c, c.M("stone"), p0 + V3.Up * 0.15f, p1 + V3.Up * 0.15f, 0.7f, 0.45f, White.Times(0.7f), true);
                // The water: a fan from the middle, bright at the spring's eye.
                Quad(c, c.Glow, at + V3.Up * 0.12f, at + new V3((float)Math.Cos(a0) * (r0 - 0.2f), 0.12f, (float)Math.Sin(a0) * (r0 - 0.2f)), at + new V3((float)Math.Cos(a1) * (r0 - 0.2f), 0.12f, (float)Math.Sin(a1) * (r0 - 0.2f)), at + V3.Up * 0.12f, V3.Up, SpringLight.Times(0.75f), null);
            }

            // Primeval stone: clusters of pale crystals about the pool and in it.
            var crystals = c.Lod == 0 ? 46 : 14;

            for (var i = 0; i < crystals; i++)
            {
                var a = rng.Range(0f, Build.Tau);
                var r = i < crystals / 4 ? rng.Range(0.5f, 3.5f) : rng.Range(5.8f, 9f);
                var foot = at + new V3((float)Math.Cos(a) * r, 0, (float)Math.Sin(a) * r);
                var tall = rng.Range(0.4f, 1.6f) * (i < crystals / 4 ? 0.7f : 1);
                var lean = new V3(rng.Range(-0.3f, 0.3f), 1, rng.Range(-0.3f, 0.3f)).Normalized;
                Crystal(c, foot, lean, tall, tall * 0.22f, new Tint(0.82f, 0.95f, 1f).Times(rng.Range(0.8f, 1.05f)));
            }

            // The moon orchids: on low terraces in a band round the pool, thick near the wall.
            var terraces = new[] { 11f, 15f };

            foreach (var r in terraces)
            {
                var n = c.Lod == 0 ? 60 : 24;

                for (var i = 0; i < n; i++)
                {
                    var a0 = Build.Tau * i / n;
                    var a1 = Build.Tau * (i + 1) / n;
                    var e = new V3((float)Math.Cos(a0) * h.Rx / 24 * r, 0, (float)Math.Sin(a0) * h.Rz / 20 * r);
                    var f = new V3((float)Math.Cos(a1) * h.Rx / 24 * r, 0, (float)Math.Sin(a1) * h.Rz / 20 * r);

                    if (Math.Abs(Math.Sin(a0 + 0.1f) + 1) < 0.15f)
                    {
                        // A gap toward the stair, the way in.
                        continue;
                    }

                    Beam(c, c.M("stone"), new V3(h.Centre.X, h.Floor + 0.2f, h.Centre.Z) + e, new V3(h.Centre.X, h.Floor + 0.2f, h.Centre.Z) + f, 0.3f, 0.4f, White.Times(0.6f), true);
                }
            }

            var flowers = c.Lod == 0 ? 900 : 160;

            for (var i = 0; i < flowers; i++)
            {
                var a = rng.Range(0f, Build.Tau);

                // Not across the way in from the stair (south, −Z).
                if (Math.Abs(Mod(a * 180 / (float)Math.PI - 270, 360) - 180) > 168)
                {
                    continue;
                }

                var r = rng.Range(7.5f, 19f);
                var foot = new V3(h.Centre.X + (float)Math.Cos(a) * r * h.Rx / 24, h.Floor + (r > 15 ? 0.4f : r > 11 ? 0.2f : 0.02f), h.Centre.Z + (float)Math.Sin(a) * r * h.Rz / 20);
                Flower(c, foot, rng.Range(0.35f, 0.7f), rng.Range(0f, Build.Tau), Orchid.Times(rng.Range(0.45f, 0.75f)));
            }
        }

        static float Mod(float a, float m) => ((a % m) + m) % m;

        /// <summary>A crystal of primeval stone: a long pointed prism of six faces, glowing faintly.</summary>
        static void Crystal(Ctx c, V3 foot, V3 axis, float length, float radius, Tint tint)
        {
            var side = V3.Cross(axis, Math.Abs(axis.Y) > 0.9f ? new V3(1, 0, 0) : V3.Up).Normalized;
            var other = V3.Cross(axis, side);
            var n = c.Lod == 0 ? 6 : 4;
            var tip = foot + axis * length;
            var top = foot + axis * (length * 0.78f);

            for (var i = 0; i < n; i++)
            {
                var a0 = Build.Tau * i / n;
                var a1 = Build.Tau * (i + 1) / n;
                var d0 = side * (float)Math.Cos(a0) + other * (float)Math.Sin(a0);
                var d1 = side * (float)Math.Cos(a1) + other * (float)Math.Sin(a1);
                var out0 = (d0 + d1).Normalized;
                // Its body like milky glass, its point glowing faintly with the essence in it.
                Quad(c, c.Plain, foot + d0 * radius - axis * 0.15f, foot + d1 * radius - axis * 0.15f, top + d1 * radius, top + d0 * radius, out0, tint.Times(0.9f - 0.2f * (i % 2)), null);
                Quad(c, c.Glow, top + d0 * radius, top + d1 * radius, tip, tip, out0 + axis * 0.5f, tint.Times(0.45f), null);
            }
        }

        /// <summary>A moon orchid: a stem, two leaves, five petals of pale blue round a white heart.</summary>
        static void Flower(Ctx c, V3 foot, float tall, float turn, Tint tint)
        {
            var head = foot + new V3(0, tall, 0);
            var leaf = new Tint(0.12f, 0.3f, 0.2f);
            Quad(c, c.Plain, foot + new V3(-0.015f, 0, 0), foot + new V3(0.015f, 0, 0), head + new V3(0.01f, 0, 0), head + new V3(-0.01f, 0, 0), new V3((float)Math.Sin(turn), 0, (float)Math.Cos(turn)), leaf, null);

            if (c.Lod == 0)
            {
                for (var k = 0; k < 2; k++)
                {
                    var a = turn + k * 3.1f;
                    var d = new V3((float)Math.Cos(a), 0, (float)Math.Sin(a));
                    Quad(c, c.Plain, foot + d * 0.02f, foot + d * 0.05f + V3.Up * 0.02f, foot + d * 0.32f + V3.Up * 0.22f, foot + d * 0.24f + V3.Up * 0.26f, V3.Up, leaf.Times(1.2f), null);
                }
            }

            var petals = c.Lod == 0 ? 5 : 3;

            for (var k = 0; k < petals; k++)
            {
                var a = turn + Build.Tau * k / petals;
                var d = new V3((float)Math.Cos(a), 0, (float)Math.Sin(a));
                var s = new V3(-d.Z, 0, d.X);
                var tipAt = head + d * 0.12f + V3.Up * 0.02f;
                Quad(c, c.Glow, head, head + d * 0.05f + s * 0.04f, tipAt, head + d * 0.05f - s * 0.04f, V3.Up, tint, null);
            }

            Quad(c, c.Glow, head + new V3(-0.02f, 0.025f, -0.02f), head + new V3(0.02f, 0.025f, -0.02f), head + new V3(0.02f, 0.025f, 0.02f), head + new V3(-0.02f, 0.025f, 0.02f), V3.Up, new Tint(1f, 1f, 0.92f), null);
        }

        /// <summary>The Dark Hall's post at the cave's mouth: a fence of black posts across the way, its gate, two lanterns, a bench and a rack.</summary>
        static void DarkPost(Ctx c, string clan, int seed)
        {
            var wood = c.M("wood");
            var black = Factions.Accent(clan + ".dark");
            var z = 36.5f;

            foreach (var x in new[] { -7f, -5f, -3.2f, 3.2f, 5f, 7f })
            {
                Post(c, wood, x, z, -16, -13.4f, 0.12f, c.Lod == 0 ? 8 : 5, black.Times(1.4f));
            }

            Beam(c, wood, new V3(-7.2f, -14.4f, z), new V3(-3.2f, -14.4f, z), 0.1f, 0.14f, black.Times(1.4f));
            Beam(c, wood, new V3(3.2f, -14.4f, z), new V3(7.2f, -14.4f, z), 0.1f, 0.14f, black.Times(1.4f));
            // The gate's frame over the way.
            Beam(c, wood, new V3(-3.2f, -16, z), new V3(-3.2f, -12.6f, z), 0.22f, 0.22f, black.Times(1.4f));
            Beam(c, wood, new V3(3.2f, -16, z), new V3(3.2f, -12.6f, z), 0.22f, 0.22f, black.Times(1.4f));
            Beam(c, wood, new V3(-3.7f, -12.8f, z), new V3(3.7f, -12.8f, z), 0.28f, 0.3f, black.Times(1.4f));

            if (c.Lod == 0)
            {
                Lamp(c, new V3(-3.2f, -13.1f, z - 0.4f), 0.24f);
                Lamp(c, new V3(3.2f, -13.1f, z - 0.4f), 0.24f);
                var p = new FortParams { Clan = clan + ".dark", Wealth = 0.7f, Wear = 0.3f };
                Compose.Add(c.Out, ThingGen.Make("weapon_rack", seed + 3, p).Lods[0], new V3(-5.5f, -16, z + 1.2f), 0);
                Compose.Add(c.Out, ThingGen.Make("table", seed + 4, p).Lods[0], new V3(5.5f, -16, z + 1.8f), 0);
            }
        }

        // ------------------------------------------------------------------ The Gu rooms, the forbidden room

        /// <summary>
        /// A Gu room: shelves along its walls with jars of Gu, the first rank's crowded, the second's
        /// fewer in lacquered cabinets, the rarest few on stands of their own; a label over each.
        /// </summary>
        static void GuRoom(Ctx c, Cut room, int rank, ref Rng rng)
        {
            var wood = c.M("wood");
            var plank = c.M("plank");
            var min = room.Min;
            var max = room.Max;
            var lacquer = rank >= 2;
            var shelfTint = lacquer ? c.Accent.Times(1.1f) : c.Frame.Times(2f);
            var glazes = rank == 1 ? new[] { new Tint(0.16f, 0.12f, 0.09f), new Tint(0.3f, 0.2f, 0.12f), new Tint(0.4f, 0.36f, 0.3f) } :
                rank == 2 ? new[] { new Tint(0.55f, 0.1f, 0.08f), new Tint(0.15f, 0.15f, 0.18f), new Tint(0.6f, 0.5f, 0.2f) } :
                new[] { new Tint(0.85f, 0.85f, 0.82f), new Tint(0.5f, 0.75f, 0.6f) };

            if (rank <= 2)
            {
                // Shelves on the walls but the door's: three or four boards high, jars on each.
                var boards = rank == 1 ? 4 : 3;
                var walls = new[] { (a: new V3(min.X + 0.5f, 0, max.Z - 0.5f), b: new V3(max.X - 0.5f, 0, max.Z - 0.5f)), (a: new V3(min.X + 0.5f, 0, min.Z + 1.2f), b: new V3(min.X + 0.5f, 0, max.Z - 1.0f)), (a: new V3(max.X - 0.5f, 0, min.Z + 1.2f), b: new V3(max.X - 0.5f, 0, max.Z - 1.0f)) };

                foreach (var (a, b) in walls)
                {
                    var length = (b - a).Length;
                    var dir = (b - a) * (1 / length);
                    var inward = V3.Cross(V3.Up, dir) * (V3.Dot(V3.Cross(V3.Up, dir), (min + max) * 0.5f - a) > 0 ? 1 : -1);
                    inward = new V3(inward.X, 0, inward.Z).Normalized;

                    for (var k = 0; k < boards; k++)
                    {
                        var y = min.Y + 0.4f + k * 0.62f;
                        Quad(c, plank, a + new V3(0, y, 0) - inward * 0.2f, b + new V3(0, y, 0) - inward * 0.2f, b + new V3(0, y, 0) + inward * 0.25f, a + new V3(0, y, 0) + inward * 0.25f, V3.Up, shelfTint, "plank");

                        if (c.Lod > 0)
                        {
                            continue;
                        }

                        for (var t = 0.25f; t < length - 0.2f; t += rank == 1 ? 0.32f : 0.62f)
                        {
                            if (rng.Chance(rank == 1 ? 0.12f : 0.3f))
                            {
                                continue;
                            }

                            var s = rank == 1 ? rng.Range(0.7f, 1f) : rng.Range(0.9f, 1.25f);
                            var foot = a + dir * t + new V3(0, y, 0) + inward * 0.02f;
                            Jar(c, foot, s * 0.22f, glazes[rng.Range(0, glazes.Length)], rank);
                        }
                    }

                    // The shelves' uprights.
                    if (c.Lod == 0)
                    {
                        for (var t = 0f; t <= length + 0.01f; t += length / Math.Max(1, (int)(length / 2.2f)))
                        {
                            var p = a + dir * t;
                            Beam(c, wood, p + new V3(0, min.Y, 0) - inward * 0.2f, p + new V3(0, min.Y + boards * 0.62f + 0.3f, 0) - inward * 0.2f, 0.07f, 0.07f, shelfTint.Times(0.9f));
                        }
                    }
                }

                // A table down the middle to sort them on.
                Box(c, plank, new V3((min.X + max.X) * 0.5f - 1.4f, min.Y + 0.78f, (min.Z + max.Z) * 0.5f - 0.5f), new V3((min.X + max.X) * 0.5f + 1.4f, min.Y + 0.86f, (min.Z + max.Z) * 0.5f + 0.5f), shelfTint, "plank");

                foreach (var (dx, dz) in new[] { (-1.3f, -0.4f), (1.3f, -0.4f), (-1.3f, 0.4f), (1.3f, 0.4f) })
                {
                    Beam(c, wood, new V3((min.X + max.X) * 0.5f + dx, min.Y, (min.Z + max.Z) * 0.5f + dz), new V3((min.X + max.X) * 0.5f + dx, min.Y + 0.78f, (min.Z + max.Z) * 0.5f + dz), 0.07f, 0.07f, shelfTint.Times(0.9f));
                }
            }
            else
            {
                // The rarest: a few on stands of their own, each under a glow of its own.
                var mid = (min + max) * 0.5f;

                for (var k = 0; k < 5; k++)
                {
                    var a = Build.Tau * k / 5 + 0.3f;
                    var at = new V3(mid.X + (float)Math.Cos(a) * 1.8f, min.Y, mid.Z + (float)Math.Sin(a) * 1.8f);
                    Lathe(c, c.M("stone"), at, new[] { (0f, 0.3f), (0.15f, 0.22f), (0.85f, 0.16f), (0.95f, 0.26f), (1.0f, 0.26f) }, White.Times(0.8f));
                    Jar(c, at + new V3(0, 1.0f, 0), 0.24f, glazes[k % glazes.Length], 3);
                    Quad(c, c.Glow, at + new V3(-0.22f, 1.55f, 0), at + new V3(0.22f, 1.55f, 0), at + new V3(0.22f, 1.95f, 0), at + new V3(-0.22f, 1.95f, 0), new V3(0, 0, -1), new Tint(0.5f, 0.8f, 0.7f).Times(0.4f), null);
                }
            }
        }

        /// <summary>A jar of Gu: glazed, lidded, a paper label with its name; the higher the rank, a ring of gold at its neck.</summary>
        static void Jar(Ctx c, V3 foot, float s, Tint glaze, int rank)
        {
            Lathe(c, c.Plain, foot, new[] { (0f, 0.55f * s), (0.35f * s, 0.95f * s), (0.9f * s, 1f * s), (1.35f * s, 0.6f * s), (1.5f * s, 0.62f * s) }, glaze);
            Lathe(c, c.M("wood"), foot + new V3(0, 1.5f * s, 0), new[] { (0f, 0.66f * s), (0.12f * s, 0.66f * s), (0.2f * s, 0.15f * s) }, c.Frame.Times(1.6f));
            Quad(c, c.Plain, foot + new V3(-0.4f * s, 0.55f * s, -1.01f * s), foot + new V3(0.4f * s, 0.55f * s, -1.01f * s), foot + new V3(0.4f * s, 1.05f * s, -0.99f * s), foot + new V3(-0.4f * s, 1.05f * s, -0.99f * s), new V3(0, 0, -1), new Tint(0.9f, 0.85f, 0.72f), null);

            if (rank >= 2)
            {
                Lathe(c, c.Plain, foot + new V3(0, 1.3f * s, 0), new[] { (0f, 0.66f * s), (0.06f * s, 0.66f * s) }, Gold);
            }
        }

        static void Lathe(Ctx c, MeshData mesh, V3 at, (float y, float r)[] profile, Tint tint, string tex = "stone")
        {
            var sides = c.Lod == 0 ? 10 : 6;

            for (var k = 0; k < profile.Length - 1; k++)
            {
                for (var i = 0; i < sides; i++)
                {
                    var a0 = Build.Tau * i / sides;
                    var a1 = Build.Tau * (i + 1) / sides;
                    V3 P(int ring, float a) => at + new V3((float)Math.Cos(a) * profile[ring].r, profile[ring].y, (float)Math.Sin(a) * profile[ring].r);
                    var mid = (a0 + a1) * 0.5f;
                    Quad(c, mesh, P(k, a0), P(k, a1), P(k + 1, a1), P(k + 1, a0), new V3((float)Math.Cos(mid), 0.2f, (float)Math.Sin(mid)), tint, mesh == c.Plain || mesh == c.Glow ? null : tex);
                }
            }
        }

        /// <summary>
        /// The forbidden room at the corridor's end: its way shut by a slab of stone pasted over with
        /// talismans and chained; inside, a pedestal with a casket glowing orange (a Gu of the fourth
        /// rank), chests, a scroll on the wall.
        /// </summary>
        static void Forbidden(Ctx c, Cut room, ref Rng rng)
        {
            var stone = c.M("stone");
            var x = room.Min.X;
            // The slab in the doorway (from the corridor's side), the talismans and the chains over it.
            Box(c, stone, new V3(x - 0.35f, room.Min.Y, 50.8f), new V3(x - 0.05f, room.Min.Y + 3.2f, 53.2f), White.Times(0.6f), "stone");

            if (c.Lod == 0)
            {
                for (var k = 0; k < 5; k++)
                {
                    var z = 51.1f + k * 0.42f;
                    var y = room.Min.Y + 1.3f + rng.Range(-0.2f, 0.6f);
                    Quad(c, c.Plain, new V3(x - 0.36f, y, z), new V3(x - 0.36f, y, z + 0.18f), new V3(x - 0.36f, y + 0.62f, z + 0.18f), new V3(x - 0.36f, y + 0.62f, z), new V3(-1, 0, 0), new Tint(0.85f, 0.75f, 0.35f), null);
                    Quad(c, c.Plain, new V3(x - 0.365f, y + 0.12f, z + 0.05f), new V3(x - 0.365f, y + 0.12f, z + 0.13f), new V3(x - 0.365f, y + 0.5f, z + 0.13f), new V3(x - 0.365f, y + 0.5f, z + 0.05f), new V3(-1, 0, 0), new Tint(0.6f, 0.08f, 0.06f), null);
                }

                foreach (var (y0, y1) in new[] { (0.8f, 2.6f), (2.6f, 0.8f) })
                {
                    Beam(c, c.Plain, new V3(x - 0.42f, room.Min.Y + y0, 50.9f), new V3(x - 0.42f, room.Min.Y + y1, 53.1f), 0.06f, 0.06f, new Tint(0.25f, 0.25f, 0.27f), true, null);
                }
            }

            // Inside: the pedestal and the casket, chests, a scroll.
            var mid = (room.Min + room.Max) * 0.5f;
            Box(c, stone, new V3(mid.X - 0.8f, room.Min.Y, mid.Z - 0.8f), new V3(mid.X + 0.8f, room.Min.Y + 1.0f, mid.Z + 0.8f), White.Times(0.85f), "stone");
            Box(c, c.Plain, new V3(mid.X - 0.35f, room.Min.Y + 1.0f, mid.Z - 0.25f), new V3(mid.X + 0.35f, room.Min.Y + 1.38f, mid.Z + 0.25f), c.Accent.Times(1.2f), null);
            Box(c, c.Glow, new V3(mid.X - 0.2f, room.Min.Y + 1.385f, mid.Z - 0.12f), new V3(mid.X + 0.2f, room.Min.Y + 1.4f, mid.Z + 0.12f), new Tint(1f, 0.55f, 0.15f), null);

            for (var k = 0; k < 4; k++)
            {
                var at = new V3(room.Max.X - 1.2f, room.Min.Y, room.Min.Z + 1.5f + k * 2.6f);
                Box(c, c.M("wood"), at + new V3(-0.6f, 0, -0.45f), at + new V3(0.6f, 0.75f, 0.45f), c.Accent.Times(0.9f), "wood");
                Box(c, c.Plain, at + new V3(-0.62f, 0.55f, -0.47f), at + new V3(0.62f, 0.62f, 0.47f), Gold.Times(0.8f), null);
            }

            Quad(c, c.Plain, new V3(mid.X - 0.6f, room.Min.Y + 1.4f, room.Max.Z - 0.05f), new V3(mid.X + 0.6f, room.Min.Y + 1.4f, room.Max.Z - 0.05f), new V3(mid.X + 0.6f, room.Min.Y + 3.4f, room.Max.Z - 0.05f), new V3(mid.X - 0.6f, room.Min.Y + 3.4f, room.Max.Z - 0.05f), new V3(0, 0, -1), new Tint(0.85f, 0.8f, 0.66f), null);
        }

        // ------------------------------------------------------------------ The Secret Hall

        /// <summary>
        /// The Secret Hall (ch. 156–157): rows of stone tables, on each jade plates and the recipe Gu
        /// lying on them coloured by rank — green the first, red-black the second, white the third,
        /// orange the fourth, purple the fifth; the walls hung with water-light Gu, a pale green-blue glow.
        /// </summary>
        static void SecretHall(Ctx c, Cut room, ref Rng rng)
        {
            var stone = c.M("stone");
            var ranks = new[] { new Tint(0.3f, 0.85f, 0.4f), new Tint(0.55f, 0.08f, 0.08f), new Tint(0.95f, 0.95f, 0.92f), new Tint(1f, 0.55f, 0.15f), new Tint(0.6f, 0.3f, 0.9f) };

            for (var row = 0; row < 3; row++)
            {
                for (var col = 0; col < 5; col++)
                {
                    var at = new V3(room.Min.X + 2.4f + col * 2.9f, room.Min.Y, room.Min.Z + 3 + row * 4);
                    Box(c, stone, at + new V3(-1f, 0, -0.5f), at + new V3(1f, 0.8f, 0.5f), White.Times(0.72f), "stone");
                    // Jade plates on it: white jade, the recipe Gu glowing in its rank's colour.
                    var plates = c.Lod == 0 ? 3 : 1;

                    for (var k = 0; k < plates; k++)
                    {
                        var x = at.X - 0.6f + k * 0.6f;
                        Box(c, c.Plain, new V3(x - 0.22f, at.Y + 0.8f, at.Z - 0.18f), new V3(x + 0.22f, at.Y + 0.84f, at.Z + 0.18f), new Tint(0.85f, 0.9f, 0.82f), null);
                        var rank = ranks[Math.Min(ranks.Length - 1, (int)(rng.Value() * rng.Value() * ranks.Length))];
                        Box(c, c.Glow, new V3(x - 0.07f, at.Y + 0.84f, at.Z - 0.05f), new V3(x + 0.07f, at.Y + 0.9f, at.Z + 0.05f), rank, null);
                    }
                }
            }

            // Water-light Gu on the walls: soft patches of green-blue.
            if (c.Lod == 0)
            {
                for (var k = 0; k < 26; k++)
                {
                    var side = k % 3;
                    var y = room.Min.Y + rng.Range(1.8f, 3.6f);
                    var s = rng.Range(0.12f, 0.3f);
                    var colour = new Tint(0.35f, 0.9f, 0.85f).Times(rng.Range(0.5f, 0.9f));

                    if (side == 0)
                    {
                        var x = rng.Range(room.Min.X + 1, room.Max.X - 1);
                        Quad(c, c.Glow, new V3(x - s, y - s, room.Max.Z - 0.04f), new V3(x + s, y - s, room.Max.Z - 0.04f), new V3(x + s, y + s, room.Max.Z - 0.04f), new V3(x - s, y + s, room.Max.Z - 0.04f), new V3(0, 0, -1), colour, null);
                    }
                    else
                    {
                        var z = rng.Range(room.Min.Z + 1, room.Max.Z - 1);
                        var x = side == 1 ? room.Min.X + 0.04f : room.Max.X - 0.04f;

                        if (side == 2 && Math.Abs(z - 56) < 2)
                        {
                            continue;
                        }

                        Quad(c, c.Glow, new V3(x, y - s, z - s), new V3(x, y - s, z + s), new V3(x, y + s, z + s), new V3(x, y + s, z - s), new V3(side == 1 ? 1 : -1, 0, 0), colour, null);
                    }
                }
            }

            // The stone door into it, standing open against the wall.
            Box(c, stone, new V3(room.Max.X - 0.3f, room.Min.Y, 57.2f), new V3(room.Max.X - 0.05f, room.Min.Y + 2.9f, 59.6f), White.Times(0.55f), "stone");
        }

        // ------------------------------------------------------------------ The blood pond

        /// <summary>
        /// The blood pond graveyard (ch. 182–186): the cave's red earth; the lake of blood over most of its
        /// floor, a bank by the way in; hundreds of holes in its walls, from many water running down into
        /// the lake; in its middle a slow whirlpool and on a rock in it the crimson coffin of the clan's first head.
        /// </summary>
        static void BloodPond(Ctx c, Hollow h, ref Rng rng)
        {
            var lake = new V3(h.Centre.X, h.Floor + 0.35f, h.Centre.Z + 4);
            var rx = h.Rx * 0.72f;
            var rz = h.Rz * 0.66f;
            var sides = c.Lod == 0 ? 64 : 24;

            for (var i = 0; i < sides; i++)
            {
                var a0 = Build.Tau * i / sides;
                var a1 = Build.Tau * (i + 1) / sides;
                var w0 = 1 + 0.1f * c.Noise.Perlin(i * 0.4f, 5);
                var w1 = 1 + 0.1f * c.Noise.Perlin((i + 1) * 0.4f, 5);
                var p0 = lake + new V3((float)Math.Cos(a0) * rx * w0, 0, (float)Math.Sin(a0) * rz * w0);
                var p1 = lake + new V3((float)Math.Cos(a1) * rx * w1, 0, (float)Math.Sin(a1) * rz * w1);
                Quad(c, c.Glow, lake, p0, p1, lake, V3.Up, Blood.Times(0.6f), null);
            }

            // The whirlpool: rings round the coffin, lighter as they turn in.
            var rings = c.Lod == 0 ? 7 : 3;

            for (var k = 0; k < rings; k++)
            {
                var r = 2.4f + k * 1.3f;
                var n = c.Lod == 0 ? 40 : 16;

                for (var i = 0; i < n; i += 2)
                {
                    var a0 = Build.Tau * i / n + k * 0.4f;
                    var a1 = Build.Tau * (i + 1) / n + k * 0.4f;
                    var r1 = r - 0.5f;
                    var y = lake.Y + 0.02f + 0.004f * k;
                    Quad(c, c.Glow, lake + new V3((float)Math.Cos(a0) * r, y - lake.Y, (float)Math.Sin(a0) * r), lake + new V3((float)Math.Cos(a1) * r1, y - lake.Y, (float)Math.Sin(a1) * r1),
                        lake + new V3((float)Math.Cos(a1) * (r1 - 0.25f), y - lake.Y, (float)Math.Sin(a1) * (r1 - 0.25f)), lake + new V3((float)Math.Cos(a0) * (r - 0.25f), y - lake.Y, (float)Math.Sin(a0) * (r - 0.25f)), V3.Up, Blood.Times(1.2f - k * 0.08f), null);
                }
            }

            // The rock in the middle and the coffin on it.
            var rock = c.M("rock");
            Lathe(c, rock, lake + new V3(0, -0.5f, 0), new[] { (0f, 2.2f), (0.5f, 2.0f), (0.85f, 1.5f), (0.95f, 0.2f) }, RedEarth.Times(0.6f), "rock");
            var coffin = lake + new V3(0, 0.45f, 0);
            var lacquer = new Tint(0.5f, 0.04f, 0.05f);
            Box(c, c.Plain, coffin + new V3(-0.55f, 0, -1.3f), coffin + new V3(0.55f, 0.75f, 1.3f), lacquer, null);
            Box(c, c.Plain, coffin + new V3(-0.65f, 0.75f, -1.4f), coffin + new V3(0.65f, 0.95f, 1.45f), lacquer.Times(1.15f), null);

            if (c.Lod == 0)
            {
                Box(c, c.Plain, coffin + new V3(-0.3f, 0.95f, -0.9f), coffin + new V3(0.3f, 1.02f, 0.9f), Gold.Times(0.7f), null);
            }

            // The holes in the walls: dark mouths over the dome, water running from some down to the lake.
            var holes = c.Lod == 0 ? 180 : 60;

            for (var k = 0; k < holes; k++)
            {
                var phi = rng.Range(0f, Build.Tau);
                var theta = rng.Range(0.15f, 0.95f);
                var dir = new V3((float)(Math.Cos(theta) * Math.Cos(phi)), (float)Math.Sin(theta), (float)(Math.Cos(theta) * Math.Sin(phi)));
                var bulge = 1 + 0.16f * c.Noise.Fbm((float)Math.Cos(phi) * 2.2f + 7, (float)Math.Sin(phi) * 2.2f + theta * 1.8f, 3);
                var p = h.Centre + new V3(dir.X * h.Rx * bulge, dir.Y * h.Ry * (bulge * 0.9f + 0.1f), dir.Z * h.Rz * bulge) * 0.985f;
                var n = (h.Centre - p).Normalized;
                var side = V3.Cross(n, V3.Up).Normalized;
                var up = V3.Cross(side, n).Normalized;
                var r = rng.Range(0.25f, 0.6f);
                var corners = new[] { p - side * r - up * r, p + side * r - up * r, p + side * r + up * r, p - side * r + up * r };
                Quad(c, c.Plain, corners[0], corners[1], corners[2], corners[3], n, new Tint(0.04f, 0.02f, 0.02f), null);

                if (k % 6 == 0)
                {
                    // A thread of water down the wall to the lake.
                    var foot = new V3(p.X, lake.Y, p.Z) + n * 1.5f;
                    var mid = (p + foot) * 0.5f + n * 0.8f;
                    Ribbon(c, new[] { p - up * r * 0.8f, mid, foot }, 0.06f, new Tint(0.32f, 0.07f, 0.06f));
                }
            }

            // The bank by the way in: a beach of red earth and stones.
            for (var k = 0; k < (c.Lod == 0 ? 40 : 10); k++)
            {
                var at = new V3(h.Centre.X + rng.Range(-14f, 14f), h.Floor, h.Centre.Z - h.Rz * 0.82f + rng.Range(-3f, 4f));
                var s = rng.Range(0.25f, 0.9f);
                Lathe(c, rock, at, new[] { (-0.1f, s), (s * 0.5f, s * 0.85f), (s * 0.8f, s * 0.3f) }, RedEarth.Times(rng.Range(0.5f, 0.8f)), "rock");
            }
        }

        /// <summary>A flat ribbon along points, turned to face the way it is seen from the cave (water running down a wall).</summary>
        static void Ribbon(Ctx c, V3[] points, float width, Tint tint)
        {
            for (var i = 0; i < points.Length - 1; i++)
            {
                var a = points[i];
                var b = points[i + 1];
                var along = (b - a).Normalized;
                var side = V3.Cross(along, V3.Up).Length > 1e-3f ? V3.Cross(along, V3.Up).Normalized : new V3(1, 0, 0);
                var face = V3.Cross(side, along);
                Quad(c, c.Glow, a - side * width, a + side * width, b + side * width, b - side * width, face, tint, null);
            }
        }
    }
}
