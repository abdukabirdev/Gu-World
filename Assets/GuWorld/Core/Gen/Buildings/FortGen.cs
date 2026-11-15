using System;
using System.Collections.Generic;
using GuWorld.Core.Gen.Flora;
using static GuWorld.Core.Gen.Buildings.BuildingGen;

namespace GuWorld.Core.Gen.Buildings
{
    /// <summary>What a settlement's ring is built of.</summary>
    public enum RampartKind { Stone, Earth, Plaster, Palisade, Bamboo }

    /// <summary>A kind of ring round a settlement, as numbers: the wall's build and size, its towers and its gate.</summary>
    public sealed class RampartStyle
    {
        public string Id, Name;
        public RampartKind Kind;
        /// <summary>The wall's height over the ground and its thickness at the foot, metres; how much each face leans in over its height.</summary>
        public float Height = 6, Thickness = 4, Batter = 0.5f;
        /// <summary>The stone course at the foot (earth and plaster walls), metres.</summary>
        public float Footing = 0.9f;
        /// <summary>Merlons along the outer edge and a walk along the top.</summary>
        public bool Crenels, Walk;
        /// <summary>A small tiled roof along the top (earth, plaster).</summary>
        public bool Cap;
        /// <summary>The building raised on a tower, and on the gate (GenBuilding styles; "" none).</summary>
        public string TowerTop = "", GateTop = "";
        /// <summary>The usual length of one piece of wall, metres.</summary>
        public float Piece = 12;
    }

    /// <summary>What is asked of one piece of a ring.</summary>
    public struct FortParams
    {
        public float Wealth, Wear;
        public string Clan;
    }

    /// <summary>A piece of a ring as made: four levels of detail by material (as a building's), a solid, and a building to stand on it.</summary>
    public sealed class FortModel
    {
        public RampartStyle Style;
        public readonly TreeLod[] Lods = new TreeLod[4];
        public MeshData Collision;
        /// <summary>Where a building stands on it (its floor's height), the building's GenBuilding name, and the room it has (width × depth).</summary>
        public float TopY;
        public string TopModel;
        public float TopWidth, TopDepth;
        /// <summary>The walk's height over the piece's origin (0 for walls without one).</summary>
        public float WalkY;
    }

    /// <summary>The kinds of ring.</summary>
    public static class Ramparts
    {
        public static readonly RampartStyle Stone = new RampartStyle
        {
            // A clan's town wall: a battered stone base, grey brick above, merlons, a paved walk; bastions with a lookout pavilion; a gate tower over a vaulted passage.
            Id = "stone", Name = "Stone wall", Kind = RampartKind.Stone, Height = 7.5f, Thickness = 5.5f, Batter = 0.7f, Crenels = true, Walk = true,
            TowerTop = "pavilion", GateTop = "hall", Piece = 12,
        };

        public static readonly RampartStyle Earth = new RampartStyle
        {
            // A village's wall of rammed earth on a stone footing under a little tiled roof; watchtowers of its own earth.
            Id = "earth", Name = "Rammed-earth wall", Kind = RampartKind.Earth, Height = 4.2f, Thickness = 1.7f, Batter = 0.25f, Footing = 0.9f, Cap = true,
            TowerTop = "watchtower", GateTop = "gate", Piece = 12,
        };

        public static readonly RampartStyle Plaster = new RampartStyle
        {
            // A rich compound's white wall: a brick dado, lattice windows to look through, a tiled cap with turned-up ends.
            Id = "plaster", Name = "White wall", Kind = RampartKind.Plaster, Height = 3.4f, Thickness = 0.65f, Batter = 0, Footing = 0.9f, Cap = true,
            TowerTop = "pavilion", GateTop = "gate", Piece = 10,
        };

        public static readonly RampartStyle Palisade = new RampartStyle
        {
            // A hamlet's stockade of sharpened logs, rails inside; watch platforms on legs; a gate of logs between two tall posts.
            Id = "palisade", Name = "Palisade", Kind = RampartKind.Palisade, Height = 4.2f, Thickness = 0.5f, Batter = 0,
            Piece = 10,
        };

        public static readonly RampartStyle Bamboo = new RampartStyle
        {
            // A homestead's fence: bamboo panels between posts, split-bamboo rails tied across.
            Id = "bamboo", Name = "Bamboo fence", Kind = RampartKind.Bamboo, Height = 2.1f, Thickness = 0.3f, Batter = 0,
            Piece = 8,
        };

        public static readonly RampartStyle[] All = { Stone, Earth, Plaster, Palisade, Bamboo };

        public static RampartStyle Get(string id)
        {
            foreach (var style in All)
            {
                if (style.Id == id)
                {
                    return style;
                }
            }

            return Earth;
        }

        /// <summary>The ring a settlement gets: by its clan and its size.</summary>
        public static RampartStyle For(string clan, bool city, int houses)
        {
            if (city)
            {
                return Stone;
            }

            if (clan == "bai")
            {
                return Plaster;
            }

            if (houses < 10)
            {
                return Palisade;
            }

            return Earth;
        }
    }

    /// <summary>
    /// Raises the pieces of a settlement's ring: lengths of wall that follow the ground under them
    /// (their top kept level-ish over a few metres, their foot sunk into it), towers and gates —
    /// stone with merlons and a walk, rammed earth under a tiled cap, white plaster with lattice
    /// windows, a palisade of sharpened logs, a bamboo fence. A piece's own frame: x along the wall,
    /// its outer face toward -Z, the ground's height at its origin 0; `ground(x, z)` gives the
    /// ground's height relative to that.
    /// </summary>
    public static class FortGen
    {
        static Ctx Context(RampartStyle style, int seed, int lod, FortParams p, Func<float, float, float> ground)
        {
            var clan = p.Clan ?? "";
            var frame = new Tint(0.2f, 0.135f, 0.095f);
            var accent = clan == "gu_yue" ? new Tint(0.52f, 0.085f, 0.065f) : clan == "xiong" ? new Tint(0.46f, 0.27f, 0.11f) : clan == "bai" ? new Tint(0.86f, 0.86f, 0.84f) : new Tint(0.3f, 0.13f, 0.09f);
            var wall = clan == "xiong" ? new Tint(0.78f, 0.63f, 0.43f) : new Tint(0.92f, 0.9f, 0.85f);

            return new Ctx
            {
                S = Buildings.House, Seed = seed, Lod = lod, Out = new TreeLod(), Noise = new Noise(seed), Wealth = Build.Clamp01(p.Wealth), Wear = Build.Clamp01(p.Wear),
                Frame = frame, Accent = accent, Wall = wall, Roof = clan == "bai" ? new Tint(1.15f, 1.15f, 1.18f) : new Tint(1, 1, 1), Leaf = accent.Times(0.85f),
                WallTex = "plaster", DadoTex = "brick", RoofTexName = "tile", Moon = clan == "gu_yue", Floor = ground,
            };
        }

        static FortModel Finish(FortModel model, Func<int, Ctx> make, Action<Ctx> raise)
        {
            for (var lod = 0; lod < 4; lod++)
            {
                var c = make(lod);
                raise(c);
                model.Lods[lod] = c.Out;
            }

            var solid = new MeshData();

            foreach (var part in model.Lods[2].Parts)
            {
                if (part.Material.StartsWith("tex:"))
                {
                    Build.Append(solid, part.Mesh, new V3(0, 0, 0));
                }
            }

            model.Collision = solid;

            return model;
        }

        // ------------------------------------------------------------------ Walls

        /// <summary>A length of wall centred on the origin (its ends overlap their neighbours' a little, so a curving ring shows no seams).</summary>
        public static FortModel Wall(RampartStyle style, int seed, float length, Func<float, float, float> ground, FortParams p)
        {
            var model = new FortModel { Style = style };

            return Finish(model, lod => Context(style, seed, lod, p, ground), c =>
            {
                var top = Level(ground, length, style.Height);

                switch (style.Kind)
                {
                    case RampartKind.Stone: StoneWall(c, style, length, ground, top, model); break;
                    case RampartKind.Earth: EarthWall(c, style, length, ground, top, "rammed"); break;
                    case RampartKind.Plaster: EarthWall(c, style, length, ground, top, "plaster"); break;
                    case RampartKind.Palisade: Palisade(c, style, length, ground); break;
                    default: BambooFence(c, style, length, ground); break;
                }
            });
        }

        /// <summary>The wall's top along it: the ground under its middle, smoothed over a few metres, plus its height.</summary>
        static Func<float, float> Level(Func<float, float, float> ground, float length, float height)
        {
            return x =>
            {
                var sum = 0f;

                for (var k = -2; k <= 2; k++)
                {
                    sum += ground(Math.Max(-length * 0.5f - 1, Math.Min(length * 0.5f + 1, x + k * 1.6f)), 0);
                }

                return sum / 5 + height;
            };
        }

        static List<float> Stations(float length, float step)
        {
            var xs = new List<float>();
            var half = length * 0.5f + 0.25f;
            var n = Math.Max(1, (int)Math.Ceiling(2 * half / step));

            for (var i = 0; i <= n; i++)
            {
                xs.Add(-half + 2 * half * i / n);
            }

            return xs;
        }

        /// <summary>A face along the wall between two lines (bottom and top) given at each station; `side` -1 the outer face, +1 the inner.</summary>
        static void Face(Ctx c, MeshData mesh, List<float> xs, Func<float, V3> bottom, Func<float, V3> top, float side, Tint tint, string tex)
        {
            var outward = new V3(0, 0, side);

            for (var i = 0; i < xs.Count - 1; i++)
            {
                var b0 = bottom(xs[i]);
                var b1 = bottom(xs[i + 1]);
                var t0 = top(xs[i]);
                var t1 = top(xs[i + 1]);

                if (t0.Y - b0.Y < 0.01f && t1.Y - b1.Y < 0.01f)
                {
                    continue;
                }

                Quad(c, mesh, b0, b1, t1, t0, outward, tint, tex);
            }
        }

        static void Flat(Ctx c, MeshData mesh, List<float> xs, Func<float, V3> a, Func<float, V3> b, Tint tint, string tex)
        {
            for (var i = 0; i < xs.Count - 1; i++)
            {
                Quad(c, mesh, a(xs[i]), a(xs[i + 1]), b(xs[i + 1]), b(xs[i]), V3.Up, tint, tex);
            }
        }

        static void StoneWall(Ctx c, RampartStyle style, float length, Func<float, float, float> ground, Func<float, float> top, FortModel model)
        {
            var stone = c.M("stone");
            var brick = c.M("brick");
            var half = style.Thickness * 0.5f;
            var lean = style.Batter;
            var step = c.Lod == 0 ? 1.5f : c.Lod == 1 ? 3f : c.Lod == 2 ? 6f : length + 1;
            var xs = Stations(length, step);
            // Where the stone gives way to brick: a little under half way up.
            var split = style.Height * 0.42f;
            float Foot(float x, float z) => Math.Min(ground(x, z), ground(x, 0)) - 0.9f;
            float Lean(float y, float x) => lean * Build.Clamp01((y - Foot(x, -half)) / (top(x) - Foot(x, -half)));

            // The outer face: stone, then brick; the inner the same, leaning the other way.
            for (var side = -1; side <= 1; side += 2)
            {
                var s = side;
                float Z(float x, float y) => s * (half - Lean(y, x));
                V3 At(float x, float y) => new V3(x, y, Z(x, y));
                Func<float, float> mid = x => ground(x, s * half) + split;
                Face(c, stone, xs, x => At(x, Foot(x, s * half)), x => At(x, Math.Min(mid(x), top(x))), s, BuildingGen.White, "stone");
                Face(c, brick, xs, x => At(x, Math.Min(mid(x), top(x))), x => At(x, top(x)), s, BuildingGen.White, "brick");

                if (c.Lod <= 1)
                {
                    // A string course where the stone ends.
                    for (var i = 0; i < xs.Count - 1; i++)
                    {
                        var y0 = Math.Min(mid(xs[i]), top(xs[i]));
                        var y1 = Math.Min(mid(xs[i + 1]), top(xs[i + 1]));
                        Beam(c, stone, new V3(xs[i], y0, Z(xs[i], y0) + s * 0.08f), new V3(xs[i + 1], y1, Z(xs[i + 1], y1) + s * 0.08f), 0.24f, 0.2f, BuildingGen.White.Times(0.9f), true, "stone");
                    }
                }
            }

            // The walk: paved, between the parapet and the inner edge.
            var outerTop = -half + lean;
            var innerTop = half - lean;
            var parapet = 0.55f;
            model.WalkY = top(0);
            Flat(c, stone, xs, x => new V3(x, top(x), outerTop + parapet), x => new V3(x, top(x), innerTop), BuildingGen.White.Times(0.85f), "stone");

            // The outer parapet: a breast-high band of brick with merlons on it; a low one inside.
            var breast = 1.0f;
            Face(c, brick, xs, x => new V3(x, top(x) - 0.1f, outerTop), x => new V3(x, top(x) + breast, outerTop), -1, BuildingGen.White, "brick");
            Face(c, brick, xs, x => new V3(x, top(x), outerTop + parapet), x => new V3(x, top(x) + breast, outerTop + parapet), 1, BuildingGen.White, "brick");
            Flat(c, brick, xs, x => new V3(x, top(x) + breast, outerTop - 0.04f), x => new V3(x, top(x) + breast, outerTop + parapet + 0.04f), BuildingGen.White.Times(0.8f), "brick");

            if (c.Lod <= 1)
            {
                Face(c, brick, xs, x => new V3(x, top(x), innerTop - 0.32f), x => new V3(x, top(x) + 0.55f, innerTop - 0.32f), -1, BuildingGen.White, "brick");
                Face(c, brick, xs, x => new V3(x, top(x) - 0.1f, innerTop), x => new V3(x, top(x) + 0.55f, innerTop), 1, BuildingGen.White, "brick");
                Flat(c, brick, xs, x => new V3(x, top(x) + 0.55f, innerTop - 0.34f), x => new V3(x, top(x) + 0.55f, innerTop + 0.02f), BuildingGen.White.Times(0.8f), "brick");
            }

            if (c.Lod <= 2)
            {
                // Merlons: some knocked off on an old wall.
                var spacing = 2.1f;
                var count = (int)(length / spacing);

                for (var i = 0; i < count; i++)
                {
                    var x = -length * 0.5f + spacing * (i + 0.5f);

                    if (c.R(i, 41) < c.Wear * 0.35f)
                    {
                        continue;
                    }

                    var y = top(x) + breast;
                    var h = c.Lod <= 1 ? 0.8f : 0.7f;
                    Box(c, brick, new V3(x - 0.62f, y, outerTop - 0.02f), new V3(x + 0.62f, y + h, outerTop + parapet), BuildingGen.White, "brick");

                    if (c.Lod == 0)
                    {
                        // A cap, and a loophole through the merlon.
                        Box(c, brick, new V3(x - 0.68f, y + h, outerTop - 0.06f), new V3(x + 0.68f, y + h + 0.08f, outerTop + parapet + 0.04f), BuildingGen.White.Times(0.8f), "brick");
                        Quad(c, c.Plain, new V3(x - 0.08f, y + 0.18f, outerTop - 0.03f), new V3(x + 0.08f, y + 0.18f, outerTop - 0.03f), new V3(x + 0.08f, y + 0.58f, outerTop - 0.03f), new V3(x - 0.08f, y + 0.58f, outerTop - 0.03f), new V3(0, 0, -1), new Tint(0.03f, 0.03f, 0.03f), null);
                    }
                }
            }

            if (c.Lod == 0)
            {
                // Spouts that throw the walk's rain clear of the face.
                for (var x = -length * 0.5f + 3; x < length * 0.5f; x += 6)
                {
                    var y = top(x) - 0.25f;
                    Box(c, stone, new V3(x - 0.14f, y - 0.14f, outerTop - 0.6f), new V3(x + 0.14f, y + 0.08f, outerTop + 0.1f), BuildingGen.White.Times(0.85f), "stone", true);
                }
            }

            Rubble(c, length, ground, -half - 0.6f);
        }

        /// <summary>Fallen stones at an old wall's foot.</summary>
        static void Rubble(Ctx c, float length, Func<float, float, float> ground, float z)
        {
            if (c.Lod > 1 || c.Wear < 0.4f)
            {
                return;
            }

            var count = (int)(length * (c.Wear - 0.35f) * 0.8f);

            for (var i = 0; i < count; i++)
            {
                var x = -length * 0.5f + length * c.R(i, 61);
                var dz = z - 1.5f * c.R(i, 62);
                var s = 0.2f + 0.35f * c.R(i, 63);
                var y = ground(x, dz) - s * 0.3f;
                Box(c, c.M("stone"), new V3(x - s, y, dz - s * 0.8f), new V3(x + s, y + s * 0.9f, dz + s * 0.8f), BuildingGen.White.Times(0.85f), "stone");
            }
        }

        /// <summary>Rammed earth (or white plaster) on a stone footing, a tiled cap over it.</summary>
        static void EarthWall(Ctx c, RampartStyle style, float length, Func<float, float, float> ground, Func<float, float> top, string tex)
        {
            var body = c.M(tex);
            var stone = c.M("stone");
            var half = style.Thickness * 0.5f;
            var lean = style.Batter;
            var step = c.Lod == 0 ? 1.5f : c.Lod == 1 ? 3f : c.Lod == 2 ? 6f : length + 1;
            var xs = Stations(length, step);
            var plaster = tex == "plaster";
            var tint = plaster ? c.Wall : BuildingGen.White;
            float Foot(float x, float z) => ground(x, z) - 0.6f;

            for (var side = -1; side <= 1; side += 2)
            {
                var s = side;
                float Z(float x, float y) => s * (half - lean * Build.Clamp01((y - Foot(x, s * half)) / Math.Max(0.5f, top(x) - Foot(x, s * half))));
                V3 At(float x, float y) => new V3(x, y, Z(x, y));
                Func<float, float> foot = x => Math.Min(top(x), ground(x, s * half) + style.Footing);
                Face(c, plaster ? c.M("brick") : stone, xs, x => At(x, Foot(x, s * half)), x => At(x, foot(x)), s, BuildingGen.White, plaster ? "brick" : "stone");
                Face(c, body, xs, x => At(x, foot(x)), x => At(x, top(x)), s, tint, tex);

                if (c.Lod <= 1)
                {
                    for (var i = 0; i < xs.Count - 1; i++)
                    {
                        var y0 = foot(xs[i]);
                        var y1 = foot(xs[i + 1]);
                        Beam(c, plaster ? c.M("brick") : stone, new V3(xs[i], y0, Z(xs[i], y0) + s * 0.04f), new V3(xs[i + 1], y1, Z(xs[i + 1], y1) + s * 0.04f), 0.1f, 0.08f, BuildingGen.White.Times(0.85f), true, plaster ? "brick" : "stone");
                    }
                }
            }

            if (plaster && c.Lod <= 1)
            {
                // Lattice windows to look through, every few metres.
                for (var x = -length * 0.5f + 2.5f; x < length * 0.5f - 1; x += 5f)
                {
                    var y = top(x) - 1.7f;

                    if (y < ground(x, 0) + 1.3f)
                    {
                        continue;
                    }

                    for (var side = -1; side <= 1; side += 2)
                    {
                        var z = side * (half + 0.02f);
                        var o = new V3(x - 0.6f, y, z);
                        Quad(c, c.Plain, new V3(x - 0.6f, y, z * 0.98f), new V3(x + 0.6f, y, z * 0.98f), new V3(x + 0.6f, y + 1.0f, z * 0.98f), new V3(x - 0.6f, y + 1.0f, z * 0.98f), new V3(0, 0, side), new Tint(0.06f, 0.06f, 0.06f), null);
                        var tile = c.M("tile");
                        // A frame of dark tiles round it, and the tiles crossed inside it (the old pattern of coins).
                        Box(c, tile, new V3(x - 0.66f, y - 0.06f, z - 0.05f), new V3(x + 0.66f, y, z + 0.05f), c.Roof.Times(0.7f), "tile");
                        Box(c, tile, new V3(x - 0.66f, y + 1.0f, z - 0.05f), new V3(x + 0.66f, y + 1.06f, z + 0.05f), c.Roof.Times(0.7f), "tile");
                        Box(c, tile, new V3(x - 0.66f, y, z - 0.05f), new V3(x - 0.6f, y + 1.0f, z + 0.05f), c.Roof.Times(0.7f), "tile");
                        Box(c, tile, new V3(x + 0.6f, y, z - 0.05f), new V3(x + 0.66f, y + 1.0f, z + 0.05f), c.Roof.Times(0.7f), "tile");

                        if (c.Lod == 0)
                        {
                            for (var k = 0; k < 4; k++)
                            {
                                var cx = x - 0.3f + 0.6f * (k % 2);
                                var cy = y + 0.25f + 0.5f * (k / 2);

                                for (var a = 0; a < 6; a++)
                                {
                                    var a0 = Build.Tau * a / 6;
                                    var a1 = Build.Tau * (a + 1) / 6;
                                    Beam(c, tile, new V3(cx + (float)Math.Cos(a0) * 0.22f, cy + (float)Math.Sin(a0) * 0.22f, z), new V3(cx + (float)Math.Cos(a1) * 0.22f, cy + (float)Math.Sin(a1) * 0.22f, z), 0.06f, 0.04f, c.Roof.Times(0.75f), true, "tile");
                                }
                            }
                        }
                    }
                }
            }

            Cap(c, xs, top, half, plaster ? 0.36f : 0.45f, length);

            if (!plaster)
            {
                Rubble(c, length, ground, -half - 0.3f);
            }
        }

        /// <summary>A small tiled roof along a wall's top: two slopes, a ridge, the eaves' edge; the ends turned up at the piece's ends on a plaster wall.</summary>
        static void Cap(Ctx c, List<float> xs, Func<float, float> top, float half, float over, float length)
        {
            var tile = c.M("tile");
            var rise = 0.45f;
            var eave = half + over;

            for (var side = -1; side <= 1; side += 2)
            {
                var s = side;
                Face(c, tile, xs, x => new V3(x, top(x) - 0.05f, s * eave), x => new V3(x, top(x) + rise, 0), s, c.Roof, "tile");

                if (c.Lod <= 2)
                {
                    // The underside, and the board along the eave.
                    Face(c, c.M("wood"), xs, x => new V3(x, top(x) - 0.17f, s * eave), x => new V3(x, top(x) - 0.12f, s * (half - 0.02f)), -s, c.Frame, "wood");
                    Face(c, c.M("wood"), xs, x => new V3(x, top(x) - 0.17f, s * eave), x => new V3(x, top(x) - 0.04f, s * eave), s, c.Frame.Times(0.8f), "wood");
                }

                if (c.Lod == 0)
                {
                    // Rows of cover tiles down each slope.
                    for (var x = xs[0] + 0.14f; x < xs[xs.Count - 1]; x += 0.28f)
                    {
                        Beam(c, tile, new V3(x, top(x) - 0.01f, s * eave), new V3(x, top(x) + rise + 0.03f, 0), 0.13f, 0.08f, c.Roof.Times(0.9f), true, "tile");
                    }
                }
            }

            if (c.Lod <= 2)
            {
                for (var i = 0; i < xs.Count - 1; i++)
                {
                    Beam(c, tile, new V3(xs[i], top(xs[i]) + rise + 0.08f, 0), new V3(xs[i + 1], top(xs[i + 1]) + rise + 0.08f, 0), 0.3f, 0.22f, c.Roof.Times(0.75f), true, "tile");
                }
            }
        }

        static void Palisade(Ctx c, RampartStyle style, float length, Func<float, float, float> ground)
        {
            var log = c.M("log");
            var wood = c.M("wood");
            var bark = new Tint(0.95f, 0.92f, 0.88f);

            if (c.Lod >= 2)
            {
                // Far: the stockade as a band, toothed along its top.
                var xs = Stations(length, c.Lod == 2 ? 0.8f : 3f);
                Face(c, log, xs, x => new V3(x, ground(x, 0) - 0.4f, -0.2f), x => new V3(x, ground(x, 0) + style.Height * (0.92f + 0.12f * (float)Math.Abs(Math.Sin(x * 7.9f))), -0.2f), -1, bark, "log");
                Face(c, log, xs, x => new V3(x, ground(x, 0) - 0.4f, 0.2f), x => new V3(x, ground(x, 0) + style.Height * 0.95f, 0.2f), 1, bark, "log");

                return;
            }

            var sides = c.Lod == 0 ? 8 : 5;
            var x0 = -length * 0.5f - 0.15f;
            var i = 0;

            while (x0 < length * 0.5f + 0.15f)
            {
                var r = 0.15f + 0.07f * c.R(i, 71);
                var x = x0 + r;
                var g = ground(x, 0);
                var h = style.Height * (0.88f + 0.2f * c.R(i, 72));
                var lean = (c.R(i, 73) - 0.5f) * 0.06f;
                Post(c, log, x, 0, g - 0.6f, g + h, r, sides, bark.Times(0.85f + 0.2f * c.R(i, 74)));
                Point(c, log, new V3(x, g + h, 0), r, 0.55f + 0.3f * c.R(i, 75), sides, bark.Times(1.05f));
                x0 = x + r + 0.02f;
                i++;
            }

            // The rails inside that bind them, on short posts.
            var xs2 = Stations(length, c.Lod == 0 ? 2f : 4f);

            foreach (var h in new[] { 0.9f, style.Height - 1.1f })
            {
                for (var k = 0; k < xs2.Count - 1; k++)
                {
                    Beam(c, wood, new V3(xs2[k], ground(xs2[k], 0.3f) + h, 0.32f), new V3(xs2[k + 1], ground(xs2[k + 1], 0.3f) + h, 0.32f), 0.16f, 0.2f, c.Frame.Times(1.6f));
                }
            }
        }

        /// <summary>A log's sharpened end: a cone.</summary>
        static void Point(Ctx c, MeshData mesh, V3 at, float radius, float height, int sides, Tint tint)
        {
            var tip = at + V3.Up * height;

            for (var k = 0; k < sides; k++)
            {
                var a0 = Build.Tau * k / sides;
                var a1 = Build.Tau * (k + 1) / sides;
                var p0 = at + new V3((float)Math.Cos(a0) * radius, 0, (float)Math.Sin(a0) * radius);
                var p1 = at + new V3((float)Math.Cos(a1) * radius, 0, (float)Math.Sin(a1) * radius);
                var mid = new V3((float)Math.Cos((a0 + a1) * 0.5f), 0.4f, (float)Math.Sin((a0 + a1) * 0.5f));
                Quad(c, mesh, p0, p1, tip, tip, mid, tint, "wood");
            }
        }

        static void BambooFence(Ctx c, RampartStyle style, float length, Func<float, float, float> ground)
        {
            var bamboo = c.M("bamboo");
            var wood = c.M("wood");
            var xs = Stations(length, c.Lod <= 1 ? 1f : 4f);
            var h = style.Height;

            for (var side = -1; side <= 1; side += 2)
            {
                var s = side;
                Face(c, bamboo, xs, x => new V3(x, ground(x, 0) + 0.05f, s * 0.05f), x => new V3(x, ground(x, 0) + h, s * 0.05f), s, BuildingGen.White, "bamboo");
            }

            Flat(c, bamboo, xs, x => new V3(x, ground(x, 0) + h, -0.05f), x => new V3(x, ground(x, 0) + h, 0.05f), BuildingGen.White.Times(0.8f), "bamboo");

            if (c.Lod >= 2)
            {
                return;
            }

            // Posts every few metres, rails of split bamboo tied across both faces.
            for (var x = -length * 0.5f; x <= length * 0.5f + 0.01f; x += 2.4f)
            {
                var g = ground(x, 0);
                Post(c, bamboo, x, 0, g - 0.3f, g + h + 0.25f, 0.07f, c.Lod == 0 ? 8 : 5, BuildingGen.White.Times(0.9f));
            }

            foreach (var y in new[] { 0.35f, h * 0.5f, h - 0.3f })
            {
                for (var side = -1; side <= 1; side += 2)
                {
                    for (var k = 0; k < xs.Count - 1; k++)
                    {
                        Beam(c, bamboo, new V3(xs[k], ground(xs[k], 0) + y, side * 0.1f), new V3(xs[k + 1], ground(xs[k + 1], 0) + y, side * 0.1f), 0.05f, 0.06f, BuildingGen.White.Times(0.85f), true, "bamboo");
                    }
                }
            }
        }

        // ------------------------------------------------------------------ Towers

        /// <summary>A tower of the ring, centred on the origin, standing out from the wall's outer face.</summary>
        public static FortModel Tower(RampartStyle style, int seed, Func<float, float, float> ground, FortParams p)
        {
            var model = new FortModel { Style = style };

            return Finish(model, lod => Context(style, seed, lod, p, ground), c =>
            {
                switch (style.Kind)
                {
                    case RampartKind.Stone: Bastion(c, style, ground, model, p); break;
                    case RampartKind.Earth: EarthTower(c, style, ground, model, p); break;
                    case RampartKind.Palisade: Platform(c, style, ground, model); break;
                    default: Pier(c, style, ground); break;
                }
            });
        }

        static void Bastion(Ctx c, RampartStyle style, Func<float, float, float> ground, FortModel model, FortParams p)
        {
            var size = style.Thickness + 3.5f;
            var h = style.Height + 1.6f;
            var cz = -1.2f;
            var top = MaxGround(ground, size) + h;
            Block(c, ground, -size * 0.5f, size * 0.5f, cz - size * 0.5f, cz + size * 0.5f, top, style.Batter, style.Height * 0.42f);
            Crown(c, -size * 0.5f + style.Batter, size * 0.5f - style.Batter, cz - size * 0.5f + style.Batter, cz + size * 0.5f - style.Batter, top);
            model.WalkY = top;
            model.TopY = top;
            model.TopModel = style.TowerTop;
            model.TopWidth = size - 2;
            model.TopDepth = size - 2;
        }

        static float MaxGround(Func<float, float, float> ground, float size)
        {
            var most = float.MinValue;

            for (var i = -1; i <= 1; i++)
            {
                for (var j = -1; j <= 1; j++)
                {
                    most = Math.Max(most, ground(i * size * 0.5f, j * size * 0.5f));
                }
            }

            return most;
        }

        static float MinGround(Func<float, float, float> ground, float x0, float x1, float z0, float z1)
        {
            var least = float.MaxValue;

            for (var i = 0; i <= 2; i++)
            {
                for (var j = 0; j <= 2; j++)
                {
                    least = Math.Min(least, ground(x0 + (x1 - x0) * i / 2, z0 + (z1 - z0) * j / 2));
                }
            }

            return least;
        }

        /// <summary>A battered block of stone below and brick above, from below the ground to `top`, its four faces leaning in by `lean`.</summary>
        static void Block(Ctx c, Func<float, float, float> ground, float x0, float x1, float z0, float z1, float top, float lean, float split)
        {
            var foot = MinGround(ground, x0, x1, z0, z1) - 1;
            var mid = Math.Min(top, foot + 1 + split);
            var t = (mid - foot) / (top - foot);

            void Faces(MeshData mesh, float y0, float y1, float l0, float l1, string tex)
            {
                var a = new V3(x0 + l0, y0, z0 + l0);
                var b = new V3(x1 - l0, y0, z0 + l0);
                var cc = new V3(x1 - l0, y0, z1 - l0);
                var d = new V3(x0 + l0, y0, z1 - l0);
                var a2 = new V3(x0 + l1, y1, z0 + l1);
                var b2 = new V3(x1 - l1, y1, z0 + l1);
                var c2 = new V3(x1 - l1, y1, z1 - l1);
                var d2 = new V3(x0 + l1, y1, z1 - l1);
                Quad(c, mesh, a, b, b2, a2, new V3(0, 0, -1), BuildingGen.White, tex);
                Quad(c, mesh, b, cc, c2, b2, new V3(1, 0, 0), BuildingGen.White, tex);
                Quad(c, mesh, cc, d, d2, c2, new V3(0, 0, 1), BuildingGen.White, tex);
                Quad(c, mesh, d, a, a2, d2, new V3(-1, 0, 0), BuildingGen.White, tex);
            }

            Faces(c.M("stone"), foot, mid, 0, lean * t, "stone");
            Faces(c.M("brick"), mid, top, lean * t, lean, "brick");
            Quad(c, c.M("stone"), new V3(x0 + lean, top, z0 + lean), new V3(x1 - lean, top, z0 + lean), new V3(x1 - lean, top, z1 - lean), new V3(x0 + lean, top, z1 - lean), V3.Up, BuildingGen.White.Times(0.85f), "stone");

            if (c.Lod <= 1)
            {
                Box(c, c.M("stone"), new V3(x0 + lean * t - 0.08f, mid - 0.1f, z0 + lean * t - 0.08f), new V3(x1 - lean * t + 0.08f, mid + 0.1f, z1 - lean * t + 0.08f), BuildingGen.White.Times(0.9f), "stone");
            }
        }

        /// <summary>A parapet with merlons round a block's top.</summary>
        static void Crown(Ctx c, float x0, float x1, float z0, float z1, float top)
        {
            var brick = c.M("brick");
            var t = 0.5f;
            Box(c, brick, new V3(x0, top - 0.05f, z0), new V3(x1, top + 1, z0 + t), BuildingGen.White, "brick");
            Box(c, brick, new V3(x0, top - 0.05f, z1 - t), new V3(x1, top + 1, z1), BuildingGen.White, "brick");
            Box(c, brick, new V3(x0, top - 0.05f, z0), new V3(x0 + t, top + 1, z1), BuildingGen.White, "brick");
            Box(c, brick, new V3(x1 - t, top - 0.05f, z0), new V3(x1, top + 1, z1), BuildingGen.White, "brick");

            if (c.Lod > 2)
            {
                return;
            }

            void Merlons(V3 a, V3 b, V3 thick)
            {
                var length = (b - a).Length;
                var n = Math.Max(1, (int)(length / 2.1f));
                var dir = (b - a) * (1 / length);

                for (var i = 0; i < n; i++)
                {
                    var m = a + dir * (length * (i + 0.5f) / n);
                    var lo = m - dir * 0.6f;
                    var hi = m + dir * 0.6f + thick;
                    Box(c, brick, new V3(Math.Min(lo.X, hi.X), top + 1, Math.Min(lo.Z, hi.Z)), new V3(Math.Max(lo.X, hi.X), top + 1.8f, Math.Max(lo.Z, hi.Z)), BuildingGen.White, "brick");
                }
            }

            Merlons(new V3(x0, 0, z0), new V3(x1, 0, z0), new V3(0, 0, t));
            Merlons(new V3(x0, 0, z1 - t), new V3(x1, 0, z1 - t), new V3(0, 0, t));
            Merlons(new V3(x0, 0, z0), new V3(x0, 0, z1), new V3(t, 0, 0));
            Merlons(new V3(x1 - t, 0, z0), new V3(x1 - t, 0, z1), new V3(t, 0, 0));
        }

        /// <summary>A square tower of the earth wall's own build, a tiled hip roof over a room with windows.</summary>
        static void EarthTower(Ctx c, RampartStyle style, Func<float, float, float> ground, FortModel model, FortParams p)
        {
            var size = style.Thickness + 3f;
            var h = style.Height + 3.2f;
            var foot = MinGround(ground, -size * 0.5f, size * 0.5f, -size * 0.5f - 1, size * 0.5f - 1) - 0.6f;
            var top = MaxGround(ground, size) + h;
            var x0 = -size * 0.5f;
            var x1 = size * 0.5f;
            var z0 = -size * 0.5f - 1;
            var z1 = size * 0.5f - 1;
            var rammed = c.M("rammed");
            var stone = c.M("stone");
            var lean = style.Batter * 1.5f;
            var footing = foot + 0.6f + style.Footing;
            Box(c, stone, new V3(x0 - 0.05f, foot, z0 - 0.05f), new V3(x1 + 0.05f, footing, z1 + 0.05f), BuildingGen.White, "stone");
            var a = new V3(x0, footing, z0);
            var b = new V3(x1, footing, z0);
            var cc = new V3(x1, footing, z1);
            var d = new V3(x0, footing, z1);
            var up = top - footing;
            Quad(c, rammed, a, b, b + new V3(-lean, up, lean), a + new V3(lean, up, lean), new V3(0, 0, -1), BuildingGen.White, "rammed");
            Quad(c, rammed, b, cc, cc + new V3(-lean, up, -lean), b + new V3(-lean, up, lean), new V3(1, 0, 0), BuildingGen.White, "rammed");
            Quad(c, rammed, cc, d, d + new V3(lean, up, -lean), cc + new V3(-lean, up, -lean), new V3(0, 0, 1), BuildingGen.White, "rammed");
            Quad(c, rammed, d, a, a + new V3(lean, up, lean), d + new V3(lean, up, -lean), new V3(-1, 0, 0), BuildingGen.White, "rammed");

            if (c.Lod <= 1)
            {
                // Arrow slits on the outer faces.
                for (var k = -1; k <= 1; k += 2)
                {
                    var y = top - 2.4f;
                    var z = z0 + lean * ((y - footing) / up) - 0.02f;
                    Quad(c, c.Plain, new V3(k * size * 0.22f - 0.1f, y, z), new V3(k * size * 0.22f + 0.1f, y, z), new V3(k * size * 0.22f + 0.1f, y + 0.9f, z), new V3(k * size * 0.22f - 0.1f, y + 0.9f, z), new V3(0, 0, -1), new Tint(0.03f, 0.03f, 0.03f), null);
                }
            }

            Quad(c, rammed, new V3(x0 + lean, top, z0 + lean), new V3(x1 - lean, top, z0 + lean), new V3(x1 - lean, top, z1 - lean), new V3(x0 + lean, top, z1 - lean), V3.Up, BuildingGen.White, "rammed");
            model.TopY = top;
            model.TopModel = "pavilion";
            model.TopWidth = size - 2 * lean;
            model.TopDepth = size - 2 * lean;
        }

        /// <summary>A palisade's watch platform: four log legs, braces, a deck with a rail, a ladder, a thatched roof on posts.</summary>
        static void Platform(Ctx c, RampartStyle style, Func<float, float, float> ground, FortModel model)
        {
            var log = c.M("log");
            var plank = c.M("plank");
            var half = 1.8f;
            var cz = 0.8f;
            var deck = style.Height + 0.6f;
            var sides = c.Lod == 0 ? 8 : 5;
            var bark = new Tint(0.95f, 0.92f, 0.88f);
            var g0 = MaxGround(ground, 3.6f);

            for (var i = -1; i <= 1; i += 2)
            {
                for (var j = -1; j <= 1; j += 2)
                {
                    var x = i * half;
                    var z = cz + j * half;
                    var g = ground(x, z);
                    Post(c, log, x, z, g - 0.6f, g0 + deck + 2.6f, 0.2f, sides, bark);
                }
            }

            if (c.Lod <= 1)
            {
                // Braces crossed between the legs.
                for (var j = -1; j <= 1; j += 2)
                {
                    var z = cz + j * half;
                    Beam(c, log, new V3(-half, g0 + 0.5f, z), new V3(half, g0 + deck - 0.4f, z), 0.14f, 0.14f, bark, true, "log");
                    Beam(c, log, new V3(half, g0 + 0.5f, z), new V3(-half, g0 + deck - 0.4f, z), 0.14f, 0.14f, bark, true, "log");
                }
            }

            Box(c, plank, new V3(-half - 0.3f, g0 + deck - 0.2f, cz - half - 0.3f), new V3(half + 0.3f, g0 + deck, cz + half + 0.3f), c.Frame.Times(2.2f), "plank", true);

            if (c.Lod <= 1)
            {
                var r0 = new V3(-half - 0.2f, g0 + deck, cz - half - 0.2f);
                var r1 = new V3(half + 0.2f, g0 + deck, cz - half - 0.2f);
                var r2 = new V3(half + 0.2f, g0 + deck, cz + half + 0.2f);
                var r3 = new V3(-half - 0.2f, g0 + deck, cz + half + 0.2f);
                Balustrade(c, r0, r1, false);
                Balustrade(c, r1, r2, false);
                Balustrade(c, r3, r0, false);

                // The ladder up the inner side.
                var foot = ground(0.5f, cz + half + 2.6f);

                for (var k = -1; k <= 1; k += 2)
                {
                    Beam(c, log, new V3(0.5f + k * 0.35f, foot, cz + half + 2.6f), new V3(0.5f + k * 0.35f, g0 + deck + 0.9f, cz + half + 0.2f), 0.1f, 0.1f, bark, true, "log");
                }

                for (var y = 0.4f; y < g0 + deck - foot; y += 0.35f)
                {
                    var t = y / (g0 + deck + 0.9f - foot);
                    var z = cz + half + 2.6f - 2.4f * t;
                    Beam(c, c.M("wood"), new V3(0.1f, foot + y, z), new V3(0.9f, foot + y, z), 0.06f, 0.06f, c.Frame.Times(1.5f));
                }
            }

            // The roof: thatch to a point.
            var eave = g0 + deck + 2.5f;
            var apex = eave + 1.7f;
            var thatch = c.M("thatch");
            var e = half + 0.9f;
            var corners = new[] { new V3(-e, eave, cz - e), new V3(e, eave, cz - e), new V3(e, eave, cz + e), new V3(-e, eave, cz + e) };
            var tip = new V3(0, apex, cz);

            for (var k = 0; k < 4; k++)
            {
                var p0 = corners[k];
                var p1 = corners[(k + 1) % 4];
                var mid = (p0 + p1) * 0.5f - new V3(0, eave, cz);
                Quad(c, thatch, p0, p1, tip, tip, new V3(mid.X, 1.2f, mid.Z), BuildingGen.White, "thatch");
                Quad(c, thatch, p0 + new V3(0, -0.25f, 0), p1 + new V3(0, -0.25f, 0), p1, p0, new V3(mid.X, 0, mid.Z), BuildingGen.White.Times(0.8f), "thatch");
            }

            model.WalkY = g0 + deck;
        }

        /// <summary>A pier where a light wall turns a corner.</summary>
        static void Pier(Ctx c, RampartStyle style, Func<float, float, float> ground)
        {
            var size = style.Kind == RampartKind.Plaster ? 1.0f : 0.5f;
            var g = MinGround(ground, -size, size, -size, size) - 0.4f;
            var top = MaxGround(ground, size * 2) + style.Height + 0.3f;
            var tex = style.Kind == RampartKind.Plaster ? "plaster" : "bamboo";
            Box(c, c.M(tex), new V3(-size * 0.5f, g, -size * 0.5f), new V3(size * 0.5f, top, size * 0.5f), style.Kind == RampartKind.Plaster ? c.Wall : BuildingGen.White, tex);

            if (style.Kind == RampartKind.Plaster)
            {
                Box(c, c.M("tile"), new V3(-size * 0.5f - 0.2f, top, -size * 0.5f - 0.2f), new V3(size * 0.5f + 0.2f, top + 0.3f, size * 0.5f + 0.2f), c.Roof.Times(0.8f), "tile");
            }
        }

        // ------------------------------------------------------------------ Gates

        /// <summary>A gate of the ring, its passage along z through the origin; its outer face toward -Z.</summary>
        public static FortModel Gate(RampartStyle style, int seed, Func<float, float, float> ground, FortParams p)
        {
            var model = new FortModel { Style = style };

            return Finish(model, lod => Context(style, seed, lod, p, ground), c =>
            {
                switch (style.Kind)
                {
                    case RampartKind.Stone: StoneGate(c, style, ground, model); break;
                    case RampartKind.Palisade:
                    case RampartKind.Bamboo: LogGate(c, style, ground, model); break;
                    default:
                        // Earth and plaster: the gatehouse building stands in the gap on a low step.
                        Block(c, ground, -6.5f, 6.5f, -2.6f, 2.6f, MaxGround(ground, 8) + 0.35f, 0, 0.35f);
                        model.TopY = MaxGround(ground, 8) + 0.35f;
                        model.TopModel = style.GateTop;
                        model.TopWidth = 12;
                        model.TopDepth = 5;
                        break;
                }
            });
        }

        static void StoneGate(Ctx c, RampartStyle style, Func<float, float, float> ground, FortModel model)
        {
            var width = 19f;
            var depth = style.Thickness + 4;
            var top = MaxGround(ground, width) + style.Height + 1.4f;
            var pass = 2.6f;
            var arch = 5.6f;
            var x0 = -width * 0.5f;
            var x1 = width * 0.5f;
            var z0 = -depth * 0.5f;
            var z1 = depth * 0.5f;
            var lean = style.Batter;
            var floor = ground(0, 0);
            // Either side of the passage, and the mass over it.
            Block(c, ground, x0, -pass, z0, z1, top, lean, style.Height * 0.42f);
            Block(c, ground, pass, x1, z0, z1, top, lean, style.Height * 0.42f);
            var brick = c.M("brick");
            var stone = c.M("stone");
            Box(c, brick, new V3(-pass - 0.01f, floor + arch, z0 + lean), new V3(pass + 0.01f, top, z1 - lean), BuildingGen.White, "brick", true);

            if (c.Lod <= 2)
            {
                // The arch's ring of voussoirs on each face, its vault inside.
                var steps = c.Lod == 0 ? 12 : 6;

                for (var side = -1; side <= 1; side += 2)
                {
                    var z = side < 0 ? z0 + lean - 0.12f : z1 - lean + 0.12f;

                    for (var k = 0; k < steps; k++)
                    {
                        var a0 = (float)Math.PI * k / steps;
                        var a1 = (float)Math.PI * (k + 1) / steps;
                        var p0 = new V3((float)Math.Cos(a0) * pass, floor + arch - pass * 0.35f + (float)Math.Sin(a0) * pass * 0.35f, z);
                        var p1 = new V3((float)Math.Cos(a1) * pass, floor + arch - pass * 0.35f + (float)Math.Sin(a1) * pass * 0.35f, z);
                        Beam(c, stone, p0, p1, 0.5f, 0.5f, BuildingGen.White.Times(0.95f), true, "stone");
                        // The spandrel under the arch's curve fills the square head of the passage.
                        Quad(c, brick, new V3(p0.X, p0.Y, z * 0.98f), new V3(p1.X, p1.Y, z * 0.98f), new V3(p1.X, floor + arch, z * 0.98f), new V3(p0.X, floor + arch, z * 0.98f), new V3(0, 0, side), BuildingGen.White, "brick");
                    }
                }

                Quad(c, stone, new V3(-pass, floor + arch - 0.01f, z0), new V3(pass, floor + arch - 0.01f, z0), new V3(pass, floor + arch - 0.01f, z1), new V3(-pass, floor + arch - 0.01f, z1), new V3(0, -1, 0), BuildingGen.White.Times(0.7f), "stone");
                Quad(c, stone, new V3(-pass, floor + 0.02f, z0), new V3(pass, floor + 0.02f, z0), new V3(pass, floor + 0.02f, z1), new V3(-pass, floor + 0.02f, z1), V3.Up, BuildingGen.White.Times(0.8f), "stone");
            }

            // The doors: two great leaves of planks banded with iron and studded, standing open inward.
            if (c.Lod <= 2)
            {
                var plank = c.M("plank");
                var dz = z1 - lean - 0.6f;

                for (var side = -1; side <= 1; side += 2)
                {
                    var hinge = new V3(side * pass, floor, dz);
                    var open = new V3(-side * (float)Math.Sin(0.35f), 0, (float)Math.Cos(0.35f)) * (pass - 0.05f);
                    var leaf = new V3(-side, 0, 0) * (pass - 0.05f);
                    var dir = (leaf * 0.25f + open * 0.75f).Normalized * (pass - 0.05f);
                    var hh = arch - pass * 0.35f;
                    var normal = V3.Cross(V3.Up, dir).Normalized;
                    Quad(c, plank, hinge, hinge + dir, hinge + dir + V3.Up * hh, hinge + V3.Up * hh, normal * -1, c.Leaf, "plank");
                    Quad(c, plank, hinge + normal * 0.12f, hinge + dir + normal * 0.12f, hinge + dir + normal * 0.12f + V3.Up * hh, hinge + normal * 0.12f + V3.Up * hh, normal, c.Leaf, "plank");

                    if (c.Lod == 0)
                    {
                        for (var row = 0; row < 7; row++)
                        {
                            for (var k = 0; k < 6; k++)
                            {
                                var at = hinge + dir * ((k + 0.5f) / 6) + V3.Up * (0.5f + row * (hh - 1) / 6) - normal * 0.03f;
                                Box(c, c.Plain, new V3(at.X - 0.05f, at.Y - 0.05f, at.Z - 0.05f), new V3(at.X + 0.05f, at.Y + 0.05f, at.Z + 0.05f), BuildingGen.Gold.Times(0.8f), null);
                            }
                        }
                    }
                }
            }

            Crown(c, x0 + lean, x1 - lean, z0 + lean, z1 - lean, top);

            if (c.Lod <= 1)
            {
                // A board with the settlement's name over the outer arch.
                var z = z0 + lean - 0.2f;
                Box(c, c.M("wood"), new V3(-1.6f, floor + arch + 0.3f, z - 0.08f), new V3(1.6f, floor + arch + 1.2f, z), new Tint(0.06f, 0.05f, 0.05f), "wood", true);
                Box(c, c.Plain, new V3(-1.66f, floor + arch + 0.24f, z - 0.1f), new V3(1.66f, floor + arch + 0.3f, z + 0.01f), BuildingGen.Gold, null, true);
                Box(c, c.Plain, new V3(-1.66f, floor + arch + 1.2f, z - 0.1f), new V3(1.66f, floor + arch + 1.26f, z + 0.01f), BuildingGen.Gold, null, true);
            }

            model.WalkY = top;
            model.TopY = top;
            model.TopModel = style.GateTop;
            model.TopWidth = width - 5;
            model.TopDepth = depth - 3;
        }

        static void LogGate(Ctx c, RampartStyle style, Func<float, float, float> ground, FortModel model)
        {
            var log = c.M("log");
            var bark = new Tint(0.95f, 0.92f, 0.88f);
            var span = 2.4f;
            var h = style.Height + 2.2f;
            var sides = c.Lod == 0 ? 10 : c.Lod == 1 ? 6 : 4;

            for (var side = -1; side <= 1; side += 2)
            {
                var g = ground(side * span, 0);
                Post(c, log, side * span, 0, g - 0.8f, g + h, 0.32f, sides, bark);
                Point(c, log, new V3(side * span, g + h, 0), 0.32f, 0.5f, sides, bark);
            }

            var top = Math.Max(ground(-span, 0), ground(span, 0)) + h - 0.9f;
            Beam(c, log, new V3(-span - 0.7f, top, -0.1f), new V3(span + 0.7f, top, -0.1f), 0.36f, 0.36f, bark, true, "log");
            Beam(c, log, new V3(-span - 0.5f, top - 0.6f, 0.1f), new V3(span + 0.5f, top - 0.6f, 0.1f), 0.24f, 0.24f, bark, true, "log");

            if (c.Lod <= 2)
            {
                // The leaves: logs side by side on two rails, standing open inward.
                for (var side = -1; side <= 1; side += 2)
                {
                    var hinge = new V3(side * (span - 0.35f), ground(side * span, 0), 0.4f);
                    var dir = new V3(-side * 0.42f, 0, 0.9f).Normalized;
                    var n = (int)((span - 0.4f) / 0.3f);

                    for (var k = 0; k < n && c.Lod <= 1; k++)
                    {
                        var at = hinge + dir * (0.15f + k * 0.3f);
                        Post(c, log, at.X, at.Z, at.Y, at.Y + style.Height - 0.4f, 0.14f, c.Lod == 0 ? 6 : 4, bark.Times(0.9f));
                        Point(c, log, new V3(at.X, at.Y + style.Height - 0.4f, at.Z), 0.14f, 0.35f, c.Lod == 0 ? 6 : 4, bark);
                    }

                    if (c.Lod == 2)
                    {
                        Quad(c, log, hinge, hinge + dir * (span - 0.4f), hinge + dir * (span - 0.4f) + V3.Up * style.Height, hinge + V3.Up * style.Height, V3.Cross(V3.Up, dir), bark, "log");
                    }

                    foreach (var y in new[] { 0.7f, style.Height - 1.2f })
                    {
                        Beam(c, c.M("wood"), hinge + V3.Up * y + V3.Cross(V3.Up, dir) * 0.18f, hinge + dir * (span - 0.4f) + V3.Up * y + V3.Cross(V3.Up, dir) * 0.18f, 0.12f, 0.18f, c.Frame.Times(1.6f));
                    }
                }
            }

            if (c.Lod <= 1)
            {
                // Two lanterns on the lintel.
                Lamp(c, new V3(-span + 0.4f, top - 0.2f, -0.45f), 0.22f);
                Lamp(c, new V3(span - 0.4f, top - 0.2f, -0.45f), 0.22f);
            }
        }
    }
}
