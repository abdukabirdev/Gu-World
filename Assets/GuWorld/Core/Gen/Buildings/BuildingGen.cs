using System;
using System.Collections.Generic;
using GuWorld.Core.Gen.Flora;

namespace GuWorld.Core.Gen.Buildings
{
    /// <summary>
    /// Raises a building from a style, a seed and what is asked of it: a stone plinth with its steps
    /// (or a deck on stilts), a frame of round posts on stone feet with beams and bracket sets, walls
    /// between the posts (plaster, planks, brick, mud, bamboo) with lattice windows and doors whose
    /// paper is lit from inside, verandas, balconies with balustrades, skirt roofs between the
    /// storeys, and a curved roof — gabled, hipped, the halls' hip-and-gable, or pointed — with rows
    /// of tiles, ridges with horns, rafters under the eaves, a finial. Four levels of detail and a
    /// solid for feet. The model stands at the origin, its front toward -Z, 1 unit = 1 m.
    /// </summary>
    public static class BuildingGen
    {
        const float TileW = 0.28f;
        const float RoofTex = TileW * 8;

        enum Panel { None, Solid, Window, Door, LatticeDoor, Counter, Rail }

        internal sealed class Ctx
        {
            public BuildingStyle S;
            public int Seed, Lod;
            public TreeLod Out;
            public Noise Noise;
            public float Wealth, Wear;
            public Tint Frame, Accent, Wall, Roof, Leaf;
            public string WallTex, DadoTex, RoofTexName;
            public bool Moon;
            /// <summary>The ground's height under a point (for the dirt at a wall's foot on sloping ground); null: the model's own zero.</summary>
            public Func<float, float, float> Floor;

            public MeshData M(string tex) => Out.Part("tex:" + tex);
            public MeshData Plain => Out.Part("plain");
            public MeshData Glow => Out.Part("glow");
            /// <summary>A stable number 0-1 for a place in the building (the same at every level of detail).</summary>
            public float R(int a, int b) => (uint)Noise.Hash(a, b, Seed) % 10007 / 10007f;
        }

        internal static readonly Tint Gold = new Tint(0.83f, 0.62f, 0.22f);
        internal static readonly Tint Paper = new Tint(1f, 0.74f, 0.38f);
        internal static readonly Tint Lantern = new Tint(1f, 0.26f, 0.1f);
        internal static readonly Tint White = new Tint(1, 1, 1);

        static string Tex(WallKind kind)
        {
            switch (kind)
            {
                case WallKind.Mud: return "mud";
                case WallKind.Plank: return "plank";
                case WallKind.Brick: return "brick";
                case WallKind.Stone: return "stone";
                case WallKind.Bamboo: return "bamboo";
                default: return "plaster";
            }
        }

        /// <summary>How many metres one repeat of a texture covers.</summary>
        internal static float Size(string tex)
        {
            switch (tex)
            {
                case "wood": return 1.4f;
                case "plank": return 2.2f;
                case "plaster": return 3.2f;
                case "mud": return 2.6f;
                case "stone": return 2.4f;
                case "brick": return 1.6f;
                case "bamboo": return 1.5f;
                case "rammed": return 3.0f;
                case "log": return 1.6f;
                default: return RoofTex;
            }
        }

        public static BuildingModel Make(BuildingStyle style, int seed, BuildingParams asked)
        {
            var rng = new Rng(seed * 7919 + 13);
            var model = new BuildingModel { Style = style, Seed = seed, Asked = asked };
            var wealth = Build.Clamp01(asked.Wealth);
            var bay = style.Bay * rng.Range(0.94f, 1.06f);
            var baysX = rng.Range(style.MinBaysX, style.MaxBaysX + 1);
            var baysZ = rng.Range(style.MinBaysZ, style.MaxBaysZ + 1);

            if (asked.Width > 0)
            {
                baysX = Math.Max(1, Math.Min(11, (int)Math.Round((asked.Width - 2 * style.Overhang * 0.6f) / bay)));
            }

            if (asked.Depth > 0)
            {
                baysZ = Math.Max(1, Math.Min(7, (int)Math.Round((asked.Depth - style.Veranda - 2 * style.Overhang * 0.6f) / bay)));
            }

            if (style.Roof == RoofKind.Pyramid)
            {
                baysZ = baysX;
            }

            var storeys = asked.Storeys > 0 ? asked.Storeys : rng.Range(style.MinStoreys, style.MaxStoreys + 1);
            var clan = asked.Clan ?? "";
            var frame = new Tint(0.2f, 0.135f, 0.095f);
            var accent = new Tint(0.3f, 0.13f, 0.09f);
            var wall = new Tint(0.9f, 0.87f, 0.8f);
            var roof = new Tint(1, 1, 1);

            if (clan == "gu_yue")
            {
                accent = new Tint(0.52f, 0.085f, 0.065f);
            }
            else if (clan == "xiong")
            {
                frame = new Tint(0.27f, 0.18f, 0.115f);
                accent = new Tint(0.46f, 0.27f, 0.11f);
                wall = new Tint(0.78f, 0.63f, 0.43f);
                roof = new Tint(0.95f, 0.84f, 0.74f);
            }
            else if (clan == "bai")
            {
                frame = new Tint(0.6f, 0.58f, 0.55f);
                accent = new Tint(0.86f, 0.86f, 0.84f);
                wall = new Tint(1, 1, 0.98f);
                roof = new Tint(1.2f, 1.2f, 1.22f);
            }

            var tone = rng.Range(0.9f, 1.08f);
            wall = wall.Times(tone);
            roof = new Tint(roof.R * rng.Range(0.9f, 1.05f), roof.G * rng.Range(0.92f, 1.05f), roof.B * rng.Range(0.92f, 1.08f));

            for (var lod = 0; lod < 4; lod++)
            {
                var c = new Ctx
                {
                    S = style, Seed = seed, Lod = lod, Out = new TreeLod(), Noise = new Noise(seed), Wealth = wealth, Wear = Build.Clamp01(asked.Wear),
                    Frame = style.Lacquer ? accent : frame, Accent = accent, Wall = wall, Roof = roof, Leaf = style.Lacquer ? accent.Times(0.8f) : frame.Times(1.15f),
                    WallTex = Tex(style.Walls), DadoTex = Tex(style.Dado), RoofTexName = style.Cover == RoofCover.Tile ? "tile" : style.Cover == RoofCover.Thatch ? "thatch" : "shingle",
                    Moon = clan == "gu_yue",
                };

                if (!style.Lacquer)
                {
                    c.Frame = frame;
                }

                Raise(c, model, bay, baysX, baysZ, storeys);
                model.Lods[lod] = c.Out;
            }

            // The solid: the far level's walls and roofs (no paper, no gilding).
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

        // ------------------------------------------------------------------ The whole

        static void Raise(Ctx c, BuildingModel model, float bay, int baysX, int baysZ, int storeys)
        {
            var s = c.S;
            // The posts' lines: the middle bay of the front a little wider.
            var widths = new float[baysX];

            for (var i = 0; i < baysX; i++)
            {
                widths[i] = bay * (baysX >= 3 && baysX % 2 == 1 && i == baysX / 2 ? 1.18f : 1);
            }

            var xs = new float[baysX + 1];
            var total = 0f;

            foreach (var w in widths)
            {
                total += w;
            }

            xs[0] = -total * 0.5f;

            for (var i = 0; i < baysX; i++)
            {
                xs[i + 1] = xs[i] + widths[i];
            }

            var veranda = s.Open ? 0 : s.Veranda;
            var depth = baysZ * bay * (s.Roof == RoofKind.Pyramid ? total / (baysX * bay) : 1) + veranda;
            var fw = total * 0.5f;
            var fd = depth * 0.5f;
            var plinth = s.Plinth * (s.Stilts ? 1 : 0.75f + 0.5f * c.Wealth);
            model.Width = total;
            model.Depth = depth;
            model.BaysX = baysX;
            model.BaysZ = baysZ;
            model.Storeys = storeys;

            if (s.Stilts)
            {
                Stilts(c, xs, fw, fd, plinth);
            }
            else
            {
                Plinth(c, fw, fd, plinth, widths[baysX / 2]);
            }

            var y = plinth;
            var post = 0.13f + 0.04f * c.Wealth + bay * 0.012f;

            for (var k = 0; k < storeys; k++)
            {
                var top = k == storeys - 1;
                var shrink = k * s.Shrink;
                var scale = (fw - shrink) / fw;
                var kw = fw - shrink;
                var kd = fd - shrink;
                var height = s.StoreyHeight * (k == 0 ? 1 : 0.9f);
                var front = -kd + (k == 0 ? veranda : 0);
                var kxs = new float[xs.Length];

                for (var i = 0; i < xs.Length; i++)
                {
                    kxs[i] = xs[i] * scale;
                }

                var kzs = new float[baysZ + 1];

                for (var j = 0; j <= baysZ; j++)
                {
                    kzs[j] = front + (kd - front) * j / baysZ;
                }

                var balcony = k > 0 && c.R(900 + k, 3) < s.Balcony;

                if (balcony)
                {
                    Balcony(c, kw, kd, y, Math.Min(1.15f, s.Shrink + 0.55f));
                }

                if (k < s.StoneBase)
                {
                    StoneShaft(c, kw, kd, y, height, k == 0, model);
                }
                else
                {
                    Storey(c, model, k, kxs, kzs, kw, kd, front, y, height, post * (k == 0 ? 1 : 0.9f), top);
                }

                y += height;

                if (!top)
                {
                    // A skirt roof round the storey's head, running up to the next storey's wall.
                    var over = s.Overhang * 0.85f;
                    var cut = over + s.Shrink + 0.12f;
                    var rise = cut * s.Pitch * 0.72f;
                    var nextBalcony = c.R(900 + k + 1, 3) < s.Balcony;
                    Roof(c, new RoofShape
                    {
                        Hw = kw + over, Hd = kd + over, E = Math.Min(kw, kd) + over, Rise = (Math.Min(kw, kd) + over) * s.Pitch * 0.72f, Curve = s.Curve * 0.6f, Upturn = s.Upturn * 0.7f,
                        Y0 = y, Cut = cut, Th = Thickness(s), Kind = RoofKind.Hip, Skirt = true, Over = over,
                    });
                    y += rise + (nextBalcony ? 0.12f : -rise * 0.35f);
                }
                else
                {
                    var over = s.Overhang;
                    var hw = kw + over;
                    var hd = kd + over;
                    var turn = hd > hw + 0.01f;
                    var shape = new RoofShape { Y0 = y, Curve = s.Curve, Upturn = s.Upturn, Th = Thickness(s), Kind = s.Roof, Turn = turn, Over = over };
                    shape.Hw = turn ? hd : hw;
                    shape.Hd = turn ? hw : hd;

                    if (s.Roof == RoofKind.Pyramid || s.Roof == RoofKind.Hip)
                    {
                        shape.E = shape.Hd;
                        shape.Rise = shape.Hd * s.Pitch * (s.Roof == RoofKind.Pyramid ? 1.25f : 1);
                    }
                    else
                    {
                        shape.E = s.Roof == RoofKind.Gable ? 0 : Math.Min(shape.Hd * 0.55f, over + bay * 0.75f);
                        shape.Rise = shape.Hd * s.Pitch;
                    }

                    if (s.Roof == RoofKind.Gable && !s.Open)
                    {
                        GableWalls(c, shape, turn ? kd : kw, turn ? kw : kd, turn);
                    }

                    Roof(c, shape);
                    model.Height = y + shape.Rise + 0.5f;
                    model.EaveWidth = hw * 2;
                    model.EaveDepth = hd * 2;
                }
            }
        }

        static float Thickness(BuildingStyle s) => s.Cover == RoofCover.Thatch ? 0.36f : s.Cover == RoofCover.Shingle ? 0.1f : 0.15f;

        // ------------------------------------------------------------------ Ground

        static void Plinth(Ctx c, float fw, float fd, float height, float doorBay)
        {
            var stone = c.M("stone");
            var margin = 0.45f + 0.35f * c.Wealth;
            Box(c, stone, new V3(-fw - margin, 0, -fd - margin), new V3(fw + margin, height, fd + margin), White, "stone");

            if (c.Lod <= 1)
            {
                // A foot course, proud of the plinth.
                Box(c, stone, new V3(-fw - margin - 0.08f, 0, -fd - margin - 0.08f), new V3(fw + margin + 0.08f, Math.Min(0.16f, height * 0.4f), fd + margin + 0.08f), White.Times(0.9f), "stone");
            }

            // Steps to the door.
            var steps = Math.Max(1, (int)Math.Ceiling(height / 0.17f));
            var width = doorBay * 0.5f + 0.5f + 0.6f * c.Wealth;
            var z0 = -fd - margin;

            if (c.Lod <= 2)
            {
                for (var i = 0; i < steps; i++)
                {
                    Box(c, stone, new V3(-width, 0, z0 - (steps - i) * 0.3f), new V3(width, height * (i + 1) / steps - 0.01f, z0 + 0.02f), White.Times(0.96f), "stone");
                }
            }

            if (height > 1 && c.Lod <= 1)
            {
                // A high plinth: cheeks beside the steps, a balustrade round its edge.
                var run = steps * 0.3f;
                Box(c, stone, new V3(-width - 0.4f, 0, z0 - run), new V3(-width, height * 0.5f, z0), White.Times(0.9f), "stone");
                Box(c, stone, new V3(width, 0, z0 - run), new V3(width + 0.4f, height * 0.5f, z0), White.Times(0.9f), "stone");
                var e = margin - 0.15f;
                var ax = fw + e;
                var az = fd + e;
                Balustrade(c, new V3(-ax, height, -az), new V3(-width - 0.2f, height, -az), true);
                Balustrade(c, new V3(width + 0.2f, height, -az), new V3(ax, height, -az), true);
                Balustrade(c, new V3(ax, height, -az), new V3(ax, height, az), true);
                Balustrade(c, new V3(ax, height, az), new V3(-ax, height, az), true);
                Balustrade(c, new V3(-ax, height, az), new V3(-ax, height, -az), true);
            }
        }

        static void Stilts(Ctx c, float[] xs, float fw, float fd, float height)
        {
            var wood = c.M("wood");
            var plank = c.M("plank");
            Box(c, plank, new V3(-fw - 0.35f, height - 0.2f, -fd - 0.35f), new V3(fw + 0.35f, height, fd + 0.35f), c.Frame.Times(2.2f), "plank", true);

            if (c.Lod <= 2)
            {
                foreach (var x in xs)
                {
                    for (var j = 0; j < 3; j++)
                    {
                        var z = -fd + fd * j;
                        Post(c, wood, x, z, -0.1f, height - 0.2f, 0.14f, c.Lod == 0 ? 10 : 6, c.Frame.Times(1.3f));

                        if (c.Lod <= 1)
                        {
                            Box(c, c.M("stone"), new V3(x - 0.24f, 0, z - 0.24f), new V3(x + 0.24f, 0.22f, z + 0.24f), White.Times(0.9f), "stone");
                        }
                    }
                }

                // A ladder stair to the gallery.
                var run = height * 0.95f;
                var z0 = -fd - 0.35f;

                for (var side = -1; side <= 1; side += 2)
                {
                    Beam(c, wood, new V3(side * 0.62f, 0, z0 - run), new V3(side * 0.62f, height - 0.05f, z0), 0.08f, 0.26f, c.Frame.Times(1.3f));
                }

                var treads = Math.Max(3, (int)(height / 0.24f));

                for (var i = 1; i <= treads && c.Lod <= 1; i++)
                {
                    var t = i / (treads + 1f);
                    Box(c, plank, new V3(-0.6f, height * t - 0.03f, z0 - run * (1 - t) - 0.14f), new V3(0.6f, height * t + 0.02f, z0 - run * (1 - t) + 0.14f), c.Frame.Times(2f), "plank", true);
                }
            }
        }

        // ------------------------------------------------------------------ Storeys

        static Panel Decide(Ctx c, int k, int side, int i, int n)
        {
            var s = c.S;
            var centre = i == (n - 1) / 2;

            if (s.Id == "gate")
            {
                return side < 2 ? (centre ? Panel.None : Panel.Solid) : Panel.Solid;
            }

            if (s.Open)
            {
                return side < 2 && centre ? Panel.None : Panel.Rail;
            }

            var balcony = k > 0 && c.R(900 + k, 3) < s.Balcony;

            if (side == 0 || s.Roof == RoofKind.Pyramid)
            {
                if (s.OpenFront && k == 0 && side == 0)
                {
                    return centre ? Panel.None : Panel.Counter;
                }

                if (s.LatticeFront)
                {
                    return side == 0 || centre ? Panel.LatticeDoor : Panel.Window;
                }

                if (centre)
                {
                    return k == 0 ? (side == 0 ? Panel.Door : Panel.Window) : balcony ? Panel.LatticeDoor : Panel.Window;
                }

                return Panel.Window;
            }

            return c.R(k * 8 + side, i) < s.Windows ? Panel.Window : Panel.Solid;
        }

        static void Storey(Ctx c, BuildingModel model, int k, float[] xs, float[] zs, float kw, float kd, float front, float y, float height, float post, bool top)
        {
            var s = c.S;
            var wood = c.M("wood");
            var nx = xs.Length - 1;
            var nz = zs.Length - 1;
            var veranda = front > -kd + 0.01f;
            var beam = 0.26f + 0.08f * c.Wealth;
            var head = y + height;

            // The posts: round, on stone feet; those of the veranda stand free.
            var places = new List<(float x, float z, bool free)>();

            for (var i = 0; i <= nx; i++)
            {
                places.Add((xs[i], front, false));
                places.Add((xs[i], kd, false));

                if (veranda)
                {
                    places.Add((xs[i], -kd, true));
                }
            }

            for (var j = 1; j < nz; j++)
            {
                places.Add((-kw, zs[j], false));
                places.Add((kw, zs[j], false));
            }

            if (c.Lod <= 2)
            {
                var sides = c.Lod == 0 ? 14 : c.Lod == 1 ? 8 : 4;

                foreach (var (x, z, free) in places)
                {
                    if (c.Lod == 2 && !free && !s.Open)
                    {
                        continue;
                    }

                    Post(c, wood, x, z, y, head, post, sides, c.Frame);

                    if (c.Lod <= 1 && k == 0)
                    {
                        Drum(c, c.M("stone"), x, z, y, y + 0.16f, post * 1.55f, c.Lod == 0 ? 12 : 8, White.Times(0.92f));
                    }
                }
            }

            // The beams round the posts' heads (and the veranda's own).
            if (c.Lod <= 2)
            {
                var by = head - beam * 0.5f;
                var z0 = veranda ? -kd : front;
                Beam(c, wood, new V3(-kw - 0.25f, by, z0), new V3(kw + 0.25f, by, z0), 0.2f, beam, c.Frame);
                Beam(c, wood, new V3(-kw - 0.25f, by, kd), new V3(kw + 0.25f, by, kd), 0.2f, beam, c.Frame);
                Beam(c, wood, new V3(-kw, by, z0 - 0.25f), new V3(-kw, by, kd + 0.25f), 0.2f, beam, c.Frame);
                Beam(c, wood, new V3(kw, by, z0 - 0.25f), new V3(kw, by, kd + 0.25f), 0.2f, beam, c.Frame);

                if (veranda)
                {
                    Beam(c, wood, new V3(-kw, by, front), new V3(kw, by, front), 0.2f, beam, c.Frame);
                }

                if (c.Lod <= 1 && (s.Brackets > 0 || c.Wealth > 0.6f))
                {
                    // A second, lighter tie under the front beam.
                    Beam(c, wood, new V3(-kw, by - beam - 0.22f, z0), new V3(kw, by - beam - 0.22f, z0), 0.12f, 0.18f, c.Frame);
                }
            }

            // Bracket sets on the outer posts' heads.
            if (c.Lod <= 1 && s.Brackets > 0)
            {
                var zOut = veranda ? -kd : front;

                for (var i = 0; i <= nx; i++)
                {
                    Bracket(c, new V3(xs[i], head, zOut), new V3(0, 0, -1));
                    Bracket(c, new V3(xs[i], head, kd), new V3(0, 0, 1));
                }

                for (var j = 1; j < nz; j++)
                {
                    Bracket(c, new V3(-kw, head, zs[j]), new V3(-1, 0, 0));
                    Bracket(c, new V3(kw, head, zs[j]), new V3(1, 0, 0));
                }
            }

            // The floor of an open building (seen between its posts), and the veranda's.
            if ((s.Open || veranda || s.OpenFront) && k == 0 && c.Lod <= 2)
            {
                Quad(c, c.M(s.Stilts ? "plank" : "stone"), new V3(-kw, y + 0.012f, -kd), new V3(kw, y + 0.012f, -kd), new V3(kw, y + 0.012f, kd), new V3(-kw, y + 0.012f, kd), V3.Up, s.Stilts ? c.Frame.Times(2.2f) : White.Times(0.85f), s.Stilts ? "plank" : "stone");
            }

            // The walls, bay by bay.
            var wallHeight = height - beam;

            for (var i = 0; i < nx; i++)
            {
                Bay(c, model, Decide(c, k, 0, i, nx), new V3(xs[i], y, front), new V3(xs[i + 1], y, front), new V3(0, 0, -1), wallHeight, k, i);
                Bay(c, model, Decide(c, k, 1, i, nx), new V3(xs[i + 1], y, kd), new V3(xs[i], y, kd), new V3(0, 0, 1), wallHeight, k, 20 + i);
            }

            for (var j = 0; j < nz; j++)
            {
                Bay(c, model, Decide(c, k, 2, j, nz), new V3(-kw, y, zs[j + 1]), new V3(-kw, y, zs[j]), new V3(-1, 0, 0), wallHeight, k, 40 + j);
                Bay(c, model, Decide(c, k, 3, j, nz), new V3(kw, y, zs[j]), new V3(kw, y, zs[j + 1]), new V3(1, 0, 0), wallHeight, k, 60 + j);
            }

            // The veranda's sides: a low rail.
            if (veranda && c.Lod <= 1 && !s.OpenFront)
            {
                Balustrade(c, new V3(-kw, y, -kd), new V3(-kw, y, front), false);
                Balustrade(c, new V3(kw, y, front), new V3(kw, y, -kd), false);
            }

            // Lanterns by the door, and a board over it.
            if (k == 0)
            {
                var zHang = (veranda ? -kd : front) - 0.42f;
                var hung = 0;

                for (var ring = 0; ring <= nx / 2 && hung < s.Lanterns && c.Lod <= 2; ring++)
                {
                    var left = (nx - 1) / 2 - ring;
                    var right = (nx - 1) / 2 + 1 + ring;

                    if (left < 0)
                    {
                        break;
                    }

                    Lamp(c, new V3(xs[left], head - beam - 0.15f, zHang), 0.2f + 0.08f * c.Wealth);
                    Lamp(c, new V3(xs[right], head - beam - 0.15f, zHang), 0.2f + 0.08f * c.Wealth);
                    hung += 2;
                }

                if (s.Sign && c.Lod <= 1)
                {
                    var mid = (xs[(nx - 1) / 2] + xs[(nx - 1) / 2 + 1]) * 0.5f;
                    var z = (veranda ? -kd : front) - 0.18f;
                    var w = Math.Min(1.3f, (xs[(nx - 1) / 2 + 1] - xs[(nx - 1) / 2]) * 0.4f);
                    var sy = head - beam - 0.42f;
                    Box(c, c.M("wood"), new V3(mid - w, sy - 0.3f, z - 0.07f), new V3(mid + w, sy + 0.3f, z), new Tint(0.07f, 0.06f, 0.06f), "wood", true);
                    var plain = c.Plain;
                    Box(c, plain, new V3(mid - w - 0.04f, sy + 0.3f, z - 0.09f), new V3(mid + w + 0.04f, sy + 0.34f, z + 0.01f), Gold, null, true);
                    Box(c, plain, new V3(mid - w - 0.04f, sy - 0.34f, z - 0.09f), new V3(mid + w + 0.04f, sy - 0.3f, z + 0.01f), Gold, null, true);
                    Box(c, plain, new V3(mid - w - 0.04f, sy - 0.3f, z - 0.09f), new V3(mid - w, sy + 0.3f, z + 0.01f), Gold, null, true);
                    Box(c, plain, new V3(mid + w, sy - 0.3f, z - 0.09f), new V3(mid + w + 0.04f, sy + 0.3f, z + 0.01f), Gold, null, true);
                }
            }
        }

        /// <summary>A watchtower's blind stone shaft, a door at its foot.</summary>
        static void StoneShaft(Ctx c, float kw, float kd, float y, float height, bool door, BuildingModel model)
        {
            var stone = c.M("stone");
            Box(c, stone, new V3(-kw, y, -kd), new V3(kw, y + height, kd), White, "stone", true);

            if (c.Lod <= 1)
            {
                // A string course at the storey's head, and corner piers.
                Box(c, stone, new V3(-kw - 0.1f, y + height - 0.25f, -kd - 0.1f), new V3(kw + 0.1f, y + height, kd + 0.1f), White.Times(0.85f), "stone", true);

                for (var sx = -1; sx <= 1; sx += 2)
                {
                    for (var sz = -1; sz <= 1; sz += 2)
                    {
                        Box(c, stone, new V3(sx * kw - 0.3f, y, sz * kd - 0.3f), new V3(sx * kw + 0.3f, y + height, sz * kd + 0.3f), White.Times(0.92f), "stone", true);
                    }
                }
            }

            if (door && c.Lod <= 2)
            {
                var plank = c.M("plank");
                Quad(c, plank, new V3(-0.8f, y, -kd - 0.03f), new V3(0.8f, y, -kd - 0.03f), new V3(0.8f, y + 2.3f, -kd - 0.03f), new V3(-0.8f, y + 2.3f, -kd - 0.03f), new V3(0, 0, -1), c.Leaf, "plank");
                Beam(c, c.M("wood"), new V3(-1f, y + 2.42f, -kd - 0.06f), new V3(1f, y + 2.42f, -kd - 0.06f), 0.2f, 0.24f, c.Frame);

                if (c.Lod == 0)
                {
                    model.Doors.Add(new BuildingDoor { At = new V3(0, y, -kd), Facing = new V3(0, 0, -1) });
                }
            }
            else if (c.Lod <= 1)
            {
                // Slits.
                for (var side = -1; side <= 1; side += 2)
                {
                    Quad(c, c.Plain, new V3(side * kw * 0.4f - 0.09f, y + 1.2f, -kd - 0.02f), new V3(side * kw * 0.4f + 0.09f, y + 1.2f, -kd - 0.02f), new V3(side * kw * 0.4f + 0.09f, y + 2.1f, -kd - 0.02f), new V3(side * kw * 0.4f - 0.09f, y + 2.1f, -kd - 0.02f), new V3(0, 0, -1), new Tint(0.03f, 0.03f, 0.03f), null);
                }
            }
        }

        // ------------------------------------------------------------------ A bay of wall

        /// <summary>One bay between two posts, seen from outside: a (left) → b (right) at the floor, `outward` its face.</summary>
        static void Bay(Ctx c, BuildingModel model, Panel kind, V3 a, V3 b, V3 outward, float height, int k, int index)
        {
            if (kind == Panel.None)
            {
                // An open bay: the way in.
                if (k == 0 && c.Lod == 0 && outward.Z < -0.5f)
                {
                    model.Doors.Add(new BuildingDoor { At = V3.Lerp(a, b, 0.5f), Facing = outward });
                }

                return;
            }

            var s = c.S;
            var length = (b - a).Length;
            var t = (b - a) * (1 / length);
            V3 P(float u, float v, float off) => a + t * u + V3.Up * v + outward * off;
            var wall = c.M(c.WallTex);
            var wood = c.M("wood");
            var dado = k == 0 && c.DadoTex != c.WallTex ? 0.95f : 0;

            void Wall(float u0, float u1, float v0, float v1)
            {
                if (u1 - u0 < 0.01f || v1 - v0 < 0.01f)
                {
                    return;
                }

                var tint = c.WallTex == "plaster" || c.WallTex == "plank" ? (c.WallTex == "plank" ? c.Frame.Times(2.4f) : c.Wall) : White;

                if (dado > 0 && v0 < dado)
                {
                    Quad(c, c.M(c.DadoTex), P(u0, v0, 0.03f), P(u1, v0, 0.03f), P(u1, Math.Min(v1, dado), 0.03f), P(u0, Math.Min(v1, dado), 0.03f), outward, White, c.DadoTex);
                    v0 = dado;

                    if (v1 <= v0)
                    {
                        return;
                    }
                }

                Quad(c, wall, P(u0, v0, 0), P(u1, v0, 0), P(u1, v1, 0), P(u0, v1, 0), outward, tint, c.WallTex);
            }

            if (kind == Panel.Rail)
            {
                if (c.Lod <= 1)
                {
                    Balustrade(c, a, b, false);
                }

                return;
            }

            if (kind == Panel.Counter)
            {
                if (c.Lod <= 2)
                {
                    // A shop's counter across the bay, and a valance under the beam.
                    var plank = c.M("plank");
                    BoxOn(c, plank, P(0.25f, 0, -0.5f), t, outward, length - 0.5f, 0.95f, 0.5f, c.Frame.Times(2f), "plank");
                    BoxOn(c, wood, P(0.2f, 0.95f, -0.58f), t, outward, length - 0.4f, 0.06f, 0.66f, c.Frame.Times(1.2f), "wood");
                    BoxOn(c, wood, P(0.1f, height - 0.34f, -0.04f), t, outward, length - 0.2f, 0.34f, 0.05f, c.Accent, "wood");
                }

                return;
            }

            if (c.Lod >= 2)
            {
                // Far: the bay as one face, its opening a lit or a dark patch.
                Wall(0, length, 0, height);

                if (kind != Panel.Solid && c.Lod == 2)
                {
                    var lit = kind != Panel.Door;
                    var w = kind == Panel.LatticeDoor ? length - 0.5f : Math.Min(length - 0.7f, 1.7f);
                    var v0 = kind == Panel.Window ? 0.95f : 0.1f;
                    var v1 = kind == Panel.Window ? Math.Min(height - 0.3f, 2.3f) : Math.Min(height - 0.25f, 2.6f);
                    var u0 = (length - w) * 0.5f;
                    Quad(c, lit ? c.Glow : c.M("plank"), P(u0, v0, 0.03f), P(u0 + w, v0, 0.03f), P(u0 + w, v1, 0.03f), P(u0, v1, 0.03f), outward, lit ? Paper.Times(0.6f) : c.Leaf, lit ? null : "plank");
                }

                return;
            }

            var pattern = (int)(c.R(77, 5) * 3);

            if (kind == Panel.Solid)
            {
                Wall(0, length, 0, height);

                if (dado > 0)
                {
                    BoxOn(c, wood, P(0, dado - 0.04f, 0), t, outward, length, 0.08f, 0.07f, c.Frame, "wood");
                }
                else if (c.WallTex == "plaster" || c.WallTex == "mud")
                {
                    // A rail across the panel, as a timber frame has.
                    BoxOn(c, wood, P(0, height * 0.45f, 0), t, outward, length, 0.1f, 0.04f, c.Frame, "wood");
                }

                return;
            }

            if (kind == Panel.Window)
            {
                var w = Math.Min(length - 0.7f, 1.5f + 0.5f * c.Wealth);
                var v0 = 0.95f;
                var v1 = Math.Min(height - 0.3f, 2.2f + 0.3f * c.Wealth);

                if (c.WallTex == "mud")
                {
                    w = Math.Min(w, 0.8f);
                    v0 = 1.2f;
                    v1 = Math.Min(v1, 1.9f);
                }

                var u0 = (length - w) * 0.5f;
                Wall(0, length, 0, v0);
                Wall(0, length, v1, height);
                Wall(0, u0, v0, v1);
                Wall(u0 + w, length, v0, v1);
                Opening(c, P(u0, v0, 0), t, outward, w, v1 - v0, pattern, 0.09f);
                // The sill.
                BoxOn(c, wood, P(u0 - 0.12f, v0 - 0.07f, 0), t, outward, w + 0.24f, 0.07f, 0.1f, c.Frame, "wood");

                if (dado > 0)
                {
                    BoxOn(c, wood, P(0, dado - 0.04f, 0), t, outward, length, 0.08f, 0.07f, c.Frame, "wood");
                }

                return;
            }

            if (kind == Panel.Door)
            {
                var w = Math.Min(length - 0.6f, 1.7f + 0.5f * c.Wealth);
                var v1 = Math.Min(height - 0.45f, 2.35f + 0.3f * c.Wealth);
                var u0 = (length - w) * 0.5f;
                Wall(0, u0, 0, height);
                Wall(u0 + w, length, 0, height);
                Wall(u0, u0 + w, v1 + 0.12f, height);
                var plank = c.M("plank");

                for (var leaf = 0; leaf < 2; leaf++)
                {
                    var l0 = u0 + leaf * w * 0.5f + 0.01f;
                    var lw = w * 0.5f - 0.02f;
                    Quad(c, plank, P(l0, 0.1f, -0.06f), P(l0 + lw, 0.1f, -0.06f), P(l0 + lw, v1, -0.06f), P(l0, v1, -0.06f), outward, c.Leaf, "plank");
                    BoxOn(c, wood, P(l0, 0.45f, -0.06f), t, outward, lw, 0.09f, 0.035f, c.Leaf.Times(0.8f), "wood");
                    BoxOn(c, wood, P(l0, v1 - 0.55f, -0.06f), t, outward, lw, 0.09f, 0.035f, c.Leaf.Times(0.8f), "wood");

                    if (c.Lod == 0)
                    {
                        // A ring handle.
                        var hx = leaf == 0 ? l0 + lw - 0.16f : l0 + 0.1f;
                        BoxOn(c, c.Plain, P(hx, 1.05f, -0.06f), t, outward, 0.06f, 0.1f, 0.05f, Gold, null);
                    }
                }

                // The frame and the threshold.
                BoxOn(c, wood, P(u0 - 0.1f, 0, -0.02f), t, outward, 0.1f, v1 + 0.12f, 0.12f, c.Frame, "wood");
                BoxOn(c, wood, P(u0 + w, 0, -0.02f), t, outward, 0.1f, v1 + 0.12f, 0.12f, c.Frame, "wood");
                BoxOn(c, wood, P(u0 - 0.1f, v1, -0.02f), t, outward, w + 0.2f, 0.12f, 0.14f, c.Frame, "wood");
                BoxOn(c, wood, P(u0, 0, -0.04f), t, outward, w, 0.12f, 0.12f, c.Frame.Times(0.8f), "wood");

                if (c.Lod == 0)
                {
                    model.Doors.Add(new BuildingDoor { At = P(length * 0.5f, 0, 0), Facing = outward });
                }

                return;
            }

            // Lattice doors from post to post: four leaves, solid below, paper above, a fixed light over them.
            {
                var leaves = length > 3.4f ? 4 : length > 2.2f ? 3 : 2;
                var u0 = 0.16f;
                var w = length - 0.32f;
                var v1 = Math.Min(height - 0.5f, 3.1f);
                var lw = w / leaves;

                for (var leaf = 0; leaf < leaves; leaf++)
                {
                    var l0 = u0 + leaf * lw;
                    Quad(c, c.M("plank"), P(l0 + 0.03f, 0.1f, -0.05f), P(l0 + lw - 0.03f, 0.1f, -0.05f), P(l0 + lw - 0.03f, 0.9f, -0.05f), P(l0 + 0.03f, 0.9f, -0.05f), outward, c.Leaf, "plank");
                    BoxOn(c, wood, P(l0 + 0.03f, 0.1f, -0.03f), t, outward, lw - 0.06f, 0.07f, 0.04f, c.Frame, "wood");
                    Opening(c, P(l0 + 0.03f, 0.9f, 0), t, outward, lw - 0.06f, v1 - 0.9f, pattern, 0.06f);
                }

                BoxOn(c, wood, P(0, 0, -0.02f), t, outward, length, 0.1f, 0.1f, c.Frame.Times(0.8f), "wood");

                if (height - v1 > 0.3f)
                {
                    Opening(c, P(u0, v1 + 0.06f, 0), t, outward, w, height - v1 - 0.1f, (pattern + 1) % 3, 0.07f);
                }
                else
                {
                    Wall(0, length, v1, height);
                }

                if (k == 0 && index == (model.BaysX - 1) / 2 && c.Lod == 0)
                {
                    model.Doors.Add(new BuildingDoor { At = P(length * 0.5f, 0, 0), Facing = outward });
                }
            }
        }

        /// <summary>A window's or a door-leaf's opening: a frame, lit paper set back in it, a lattice of thin bars before the paper.</summary>
        internal static void Opening(Ctx c, V3 o, V3 t, V3 outward, float w, float h, int pattern, float frame)
        {
            if (w < 0.15f || h < 0.15f)
            {
                return;
            }

            var wood = c.M("wood");
            V3 P(float u, float v, float off) => o + t * u + V3.Up * v + outward * off;
            // The paper: brighter in some windows than in others, as lamps stand behind them.
            var lit = 0.55f + 0.45f * c.R((int)(o.X * 7 + o.Z * 13), (int)(o.Y * 5));
            Quad(c, c.Glow, P(0, 0, -0.07f), P(w, 0, -0.07f), P(w, h, -0.07f), P(0, h, -0.07f), outward, Paper.Times(lit), null);
            BoxOn(c, wood, P(0, 0, -0.03f), t, outward, w, frame, 0.07f, c.Frame, "wood");
            BoxOn(c, wood, P(0, h - frame, -0.03f), t, outward, w, frame, 0.07f, c.Frame, "wood");
            BoxOn(c, wood, P(0, frame, -0.03f), t, outward, frame, h - 2 * frame, 0.07f, c.Frame, "wood");
            BoxOn(c, wood, P(w - frame, frame, -0.03f), t, outward, frame, h - 2 * frame, 0.07f, c.Frame, "wood");

            var bar = 0.028f;
            var step = c.Lod == 0 ? 0.17f : 0.36f;
            var tint = c.Frame.Times(0.9f);
            var iw = w - 2 * frame;
            var ih = h - 2 * frame;

            void Bar(float u0, float v0, float u1, float v1)
            {
                Beam(c, wood, P(frame + u0, frame + v0, -0.045f), P(frame + u1, frame + v1, -0.045f), bar, bar, tint, false);
            }

            if (pattern == 2 && c.Lod == 0)
            {
                // Diamonds: bars both ways on the slant, cut to the opening.
                var d = step * 1.5f;

                for (var k = -ih; k < iw; k += d)
                {
                    var u0 = Math.Max(0, k);
                    var u1 = Math.Min(iw, k + ih);

                    if (u1 > u0)
                    {
                        Bar(u0, u0 - k, u1, u1 - k);
                        Bar(u0, ih - (u0 - k), u1, ih - (u1 - k));
                    }
                }

                return;
            }

            var inset = pattern == 1 ? Math.Min(0.14f, Math.Min(iw, ih) * 0.2f) : 0;

            if (inset > 0 && c.Lod == 0)
            {
                // A border within the frame, then the grid inside it.
                Bar(inset, inset, iw - inset, inset);
                Bar(inset, ih - inset, iw - inset, ih - inset);
                Bar(inset, inset, inset, ih - inset);
                Bar(iw - inset, inset, iw - inset, ih - inset);
            }

            var nu = Math.Max(1, (int)Math.Round(iw / step));
            var nv = Math.Max(1, (int)Math.Round(ih / step));

            for (var i = 1; i < nu; i++)
            {
                Bar(iw * i / nu, 0, iw * i / nu, ih);
            }

            for (var j = 1; j < nv; j++)
            {
                Bar(0, ih * j / nv, iw, ih * j / nv);
            }
        }

        // ------------------------------------------------------------------ Galleries, brackets, lamps

        static void Balcony(Ctx c, float kw, float kd, float y, float reach)
        {
            var wood = c.M("wood");
            var ax = kw + reach;
            var az = kd + reach;

            if (c.Lod <= 2)
            {
                Box(c, c.M("plank"), new V3(-ax, y - 0.16f, -az), new V3(ax, y, az), c.Frame.Times(2f), "plank", true);
                Box(c, wood, new V3(-ax - 0.03f, y - 0.3f, -az - 0.03f), new V3(ax + 0.03f, y - 0.14f, az + 0.03f), c.Frame, "wood", true);
            }

            if (c.Lod <= 1)
            {
                Balustrade(c, new V3(-ax, y, -az), new V3(ax, y, -az), false);
                Balustrade(c, new V3(ax, y, -az), new V3(ax, y, az), false);
                Balustrade(c, new V3(ax, y, az), new V3(-ax, y, az), false);
                Balustrade(c, new V3(-ax, y, az), new V3(-ax, y, -az), false);
            }
            else if (c.Lod == 2)
            {
                // Far: the rail as a band.
                Box(c, wood, new V3(-ax, y + 0.5f, -az), new V3(ax, y + 0.95f, az), c.Frame, "wood", true);
            }
        }

        /// <summary>A balustrade from a to b standing on their height: posts with caps, three rails, balusters between (stone: a solid panel).</summary>
        internal static void Balustrade(Ctx c, V3 a, V3 b, bool stone)
        {
            var length = (b - a).Length;

            if (length < 0.3f)
            {
                return;
            }

            var t = (b - a) * (1 / length);
            var mesh = c.M(stone ? "stone" : "wood");
            var tint = stone ? White.Times(0.95f) : c.Frame;
            var tex = stone ? "stone" : "wood";
            var posts = Math.Max(1, (int)Math.Round(length / 1.5f));
            var h = stone ? 1.0f : 0.95f;
            var normal = V3.Cross(V3.Up, t);

            for (var i = 0; i <= posts; i++)
            {
                var p = a + t * (length * i / posts);
                Box(c, mesh, new V3(p.X - 0.07f, p.Y, p.Z - 0.07f), new V3(p.X + 0.07f, p.Y + h + 0.12f, p.Z + 0.07f), tint, tex, true);

                if (c.Lod == 0)
                {
                    Box(c, mesh, new V3(p.X - 0.1f, p.Y + h + 0.12f, p.Z - 0.1f), new V3(p.X + 0.1f, p.Y + h + 0.18f, p.Z + 0.1f), tint, tex, true);
                }
            }

            Beam(c, mesh, a + V3.Up * h, b + V3.Up * h, 0.09f, 0.08f, tint);
            Beam(c, mesh, a + V3.Up * 0.14f, b + V3.Up * 0.14f, 0.06f, 0.07f, tint);

            if (stone)
            {
                Quad(c, mesh, a + V3.Up * 0.14f + normal * 0.03f, b + V3.Up * 0.14f + normal * 0.03f, b + V3.Up * (h - 0.25f) + normal * 0.03f, a + V3.Up * (h - 0.25f) + normal * 0.03f, normal, tint.Times(0.9f), tex);
                Quad(c, mesh, a + V3.Up * 0.14f - normal * 0.03f, b + V3.Up * 0.14f - normal * 0.03f, b + V3.Up * (h - 0.25f) - normal * 0.03f, a + V3.Up * (h - 0.25f) - normal * 0.03f, normal * -1, tint.Times(0.9f), tex);
                Beam(c, mesh, a + V3.Up * (h - 0.25f), b + V3.Up * (h - 0.25f), 0.07f, 0.06f, tint);

                return;
            }

            Beam(c, mesh, a + V3.Up * (h - 0.3f), b + V3.Up * (h - 0.3f), 0.05f, 0.05f, tint);

            if (c.Lod == 0)
            {
                var count = Math.Max(1, (int)(length / 0.15f));

                for (var i = 1; i < count; i++)
                {
                    var p = a + t * (length * i / count);
                    Beam(c, mesh, p + V3.Up * 0.17f, p + V3.Up * (h - 0.32f), 0.03f, 0.03f, tint.Times(0.92f), false);
                }
            }
        }

        /// <summary>A bracket set on a post's head: blocks and arms stepping out under the eave.</summary>
        static void Bracket(Ctx c, V3 head, V3 outward)
        {
            var wood = c.M("wood");
            var t = V3.Cross(V3.Up, outward);
            var tiers = c.Lod == 0 ? c.S.Brackets : 1;
            var tint = c.Accent;

            for (var tier = 0; tier < tiers; tier++)
            {
                var y = head.Y - 0.02f + tier * 0.2f;
                var reach = 0.42f + tier * 0.36f;
                Beam(c, wood, head + V3.Up * (y - head.Y + 0.09f) - outward * 0.15f, head + V3.Up * (y - head.Y + 0.09f) + outward * reach, 0.13f, 0.15f, tint);
                Beam(c, wood, head + V3.Up * (y - head.Y + 0.09f) + outward * (reach - 0.08f) - t * (0.32f + tier * 0.12f), head + V3.Up * (y - head.Y + 0.09f) + outward * (reach - 0.08f) + t * (0.32f + tier * 0.12f), 0.12f, 0.13f, tint);
            }
        }

        /// <summary>A paper lantern hung on a cord: a lit drum between two dark caps, a tassel.</summary>
        internal static void Lamp(Ctx c, V3 top, float radius)
        {
            var glow = c.Glow;
            var sides = c.Lod == 0 ? 10 : c.Lod == 1 ? 6 : 4;
            var cy = top.Y - 0.25f - radius;
            var rings = new[] { (-1f, 0.55f), (-0.5f, 0.95f), (0f, 1.05f), (0.5f, 0.95f), (1f, 0.55f) };

            for (var r = 0; r < rings.Length - 1; r++)
            {
                for (var i = 0; i < sides; i++)
                {
                    var a0 = Build.Tau * i / sides;
                    var a1 = Build.Tau * (i + 1) / sides;
                    V3 At((float y, float w) ring, float a) => new V3(top.X + (float)Math.Cos(a) * radius * ring.w, cy + ring.y * radius * 1.15f, top.Z + (float)Math.Sin(a) * radius * ring.w);
                    var mid = new V3((float)Math.Cos((a0 + a1) * 0.5f), 0, (float)Math.Sin((a0 + a1) * 0.5f));
                    Quad(c, glow, At(rings[r], a0), At(rings[r], a1), At(rings[r + 1], a1), At(rings[r + 1], a0), mid, Lantern, null);
                }
            }

            if (c.Lod <= 1)
            {
                var plain = c.Plain;
                var dark = new Tint(0.08f, 0.06f, 0.05f);
                Box(c, plain, new V3(top.X - radius * 0.5f, cy + radius * 1.15f, top.Z - radius * 0.5f), new V3(top.X + radius * 0.5f, cy + radius * 1.3f, top.Z + radius * 0.5f), dark, null);
                Box(c, plain, new V3(top.X - radius * 0.5f, cy - radius * 1.3f, top.Z - radius * 0.5f), new V3(top.X + radius * 0.5f, cy - radius * 1.15f, top.Z + radius * 0.5f), dark, null);
                Box(c, plain, new V3(top.X - 0.012f, cy + radius * 1.3f, top.Z - 0.012f), new V3(top.X + 0.012f, top.Y, top.Z + 0.012f), dark, null, true);
                Box(c, plain, new V3(top.X - 0.03f, cy - radius * 1.3f - 0.3f, top.Z - 0.03f), new V3(top.X + 0.03f, cy - radius * 1.3f, top.Z + 0.03f), Gold.Times(0.9f), null, true);
            }
        }

        // ------------------------------------------------------------------ Roofs

        sealed class RoofShape
        {
            /// <summary>Half the eaves' length along the ridge and across it; how deep the end slopes run (0: gables); the rise; the hollow; the corners' lift.</summary>
            public float Hw, Hd, E, Rise, Curve, Upturn, Y0, Th, Over;
            /// <summary>A skirt roof stops this far in from its eave (at the wall above).</summary>
            public float Cut;
            public RoofKind Kind;
            public bool Skirt, Turn;
        }

        /// <summary>A point of a roof's upper face: `face` 0 front, 1 back, 2 and 3 the ends; `a` metres along the eave from its middle, `d` metres in from the eave.</summary>
        static V3 RoofAt(RoofShape r, int face, float a, float d)
        {
            var t = Build.Clamp01(d / r.Hd);
            var y = r.Y0 + r.Rise * (float)Math.Pow(t, 1 + r.Curve);
            var span = face < 2 ? r.Hw - Math.Min(d, r.E) : r.Hd - d;
            var corner = span - Math.Abs(a);
            var reach = Math.Max(1.5f, Math.Min(span, r.Hd * 0.9f));
            var along = Build.Clamp01(1 - corner / reach);
            var inward = Build.Clamp01(1 - d / Math.Max(0.8f, r.Hd * 0.7f));
            y += r.Upturn * along * along * along * inward * inward;
            V3 p;

            switch (face)
            {
                case 0: p = new V3(a, y, -(r.Hd - d)); break;
                case 1: p = new V3(a, y, r.Hd - d); break;
                case 2: p = new V3(-(r.Hw - d), y, a); break;
                default: p = new V3(r.Hw - d, y, a); break;
            }

            return r.Turn ? new V3(p.Z, p.Y, p.X) : p;
        }

        static float Span(RoofShape r, int face, float d) => face < 2 ? r.Hw - Math.Min(d, r.E) : r.Hd - d;

        static V3 Outward(RoofShape r, int face)
        {
            var n = face == 0 ? new V3(0, 0, -1) : face == 1 ? new V3(0, 0, 1) : face == 2 ? new V3(-1, 0, 0) : new V3(1, 0, 0);

            return r.Turn ? new V3(n.Z, 0, n.X) : n;
        }

        static void Roof(Ctx c, RoofShape r)
        {
            var s = c.S;
            var cover = c.M(c.RoofTexName);
            var wood = c.M("wood");
            var tile = s.Cover == RoofCover.Tile;
            var faces = r.E > 0.01f ? 4 : 2;
            var rowsWanted = c.Lod == 0 ? 10 : c.Lod == 1 ? 7 : c.Lod == 2 ? 4 : 2;
            var slope = (float)Math.Sqrt(1 + (r.Rise / r.Hd) * (r.Rise / r.Hd));
            var under = Math.Min(r.Over + 0.3f, r.Skirt ? r.Cut : r.Hd);

            for (var face = 0; face < faces; face++)
            {
                var deep = r.Skirt ? r.Cut : face < 2 ? r.Hd : r.E;
                var rows = new List<float>();

                for (var i = 0; i <= rowsWanted; i++)
                {
                    // Closer rows toward the eave, where the corners turn up.
                    var f = i / (float)rowsWanted;
                    rows.Add(deep * (0.35f * f + 0.65f * f * f));
                }

                if (face < 2 && r.E > 0.01f && r.E < deep - 0.01f)
                {
                    rows.Add(r.E);
                    rows.Sort();
                }

                var widest = Span(r, face, 0);
                var cell = c.Lod == 0 ? 0.8f : c.Lod == 1 ? 1.4f : c.Lod == 2 ? 3f : 7f;
                var columns = Math.Max(c.Lod <= 1 ? 8 : c.Lod == 2 ? 4 : 2, (int)(widest * 2 / cell));
                var outward = Outward(r, face);
                var start = cover.VertexCount;

                for (var i = 0; i < rows.Count; i++)
                {
                    var d = rows[i];
                    var span = Span(r, face, d);

                    for (var j = 0; j <= columns; j++)
                    {
                        // Columns crowd toward the corners too.
                        var f = j / (float)columns * 2 - 1;
                        f = Math.Sign(f) * (0.45f * Math.Abs(f) + 0.55f * (1 - (1 - Math.Abs(f)) * (1 - Math.Abs(f))));
                        var a = span * f;
                        var p = RoofAt(r, face, a, d);
                        var e = 0.06f;
                        var pa = RoofAt(r, face, Math.Min(span, a + e), d) - RoofAt(r, face, Math.Max(-span, a - e), d);
                        var pd = RoofAt(r, face, a, Math.Min(deep, d + e)) - RoofAt(r, face, a, Math.Max(0, d - e));
                        var n = V3.Cross(pa, pd).Normalized;

                        if (n.Y < 0)
                        {
                            n = n * -1;
                        }

                        var tint = RoofTint(c, p, r.Skirt && d > deep - 0.5f ? 0.8f : 1);
                        var v = cover.Vertex(p.X, p.Y, p.Z, a / RoofTex, d * slope / RoofTex, tint.R, tint.G, tint.B, 1);
                        cover.SetNormal(v, n.X, n.Y, n.Z);
                    }
                }

                for (var i = 0; i < rows.Count - 1; i++)
                {
                    for (var j = 0; j < columns; j++)
                    {
                        var v0 = start + i * (columns + 1) + j;
                        var v1 = v0 + 1;
                        var v2 = v1 + columns + 1;
                        var v3 = v0 + columns + 1;
                        Wind(cover, v0, v1, v2, v3, V3.Up);
                    }
                }

                if (c.Lod <= 2)
                {
                    // The underside of the eave, and the board along its edge.
                    // The underside follows the slope's own rows as far in as it shows, so the turned-up corners keep it under them.
                    var lower = new List<float>();

                    foreach (var d in rows)
                    {
                        if (d < under - 0.05f || r.Kind == RoofKind.Gable && !r.Skirt)
                        {
                            lower.Add(d);
                        }
                    }

                    if (!(r.Kind == RoofKind.Gable && !r.Skirt))
                    {
                        lower.Add(under);
                    }

                    var underMesh = tile ? wood : cover;
                    var underTex = tile ? "wood" : c.RoofTexName;
                    var underTint = tile ? c.Frame.Times(0.9f) : White.Times(0.6f);
                    var cols = Math.Max(2, columns / 2);

                    for (var i = 0; i < lower.Count - 1; i++)
                    {
                        for (var j = 0; j < cols; j++)
                        {
                            float A(int jj, float d) => Span(r, face, d) * (jj / (float)cols * 2 - 1);
                            var down = new V3(0, -r.Th, 0);
                            Quad(c, underMesh, RoofAt(r, face, A(j, lower[i]), lower[i]) + down, RoofAt(r, face, A(j + 1, lower[i]), lower[i]) + down, RoofAt(r, face, A(j + 1, lower[i + 1]), lower[i + 1]) + down, RoofAt(r, face, A(j, lower[i + 1]), lower[i + 1]) + down, new V3(0, -1, 0), underTint, underTex);
                        }
                    }

                    for (var j = 0; j < columns; j++)
                    {
                        float F(int jj)
                        {
                            var f = jj / (float)columns * 2 - 1;

                            return widest * Math.Sign(f) * (0.45f * Math.Abs(f) + 0.55f * (1 - (1 - Math.Abs(f)) * (1 - Math.Abs(f))));
                        }

                        var p0 = RoofAt(r, face, F(j), 0);
                        var p1 = RoofAt(r, face, F(j + 1), 0);
                        Quad(c, underMesh, p0 + new V3(0, -r.Th, 0), p1 + new V3(0, -r.Th, 0), p1, p0, outward, tile ? c.Frame.Times(0.8f) : White.Times(0.75f), underTex);
                    }
                }

                if (c.Lod == 0)
                {
                    // Rafters under the eave.
                    var count = (int)(widest * 2 / 0.5f);

                    for (var i = 0; i <= count; i++)
                    {
                        var a = -widest + 0.15f + (widest * 2 - 0.3f) * i / Math.Max(1, count);

                        if (Math.Abs(a) > Span(r, face, under) - 0.05f)
                        {
                            continue;
                        }

                        // In pieces that follow the slope's curve (a straight rafter would cut up through the turned-up corner).
                        var drop = new V3(0, -r.Th - 0.08f, 0);
                        var pieces = 3;

                        for (var p = 0; p < pieces; p++)
                        {
                            var d0 = 0.04f + (under - 0.04f) * p / pieces;
                            var d1 = 0.04f + (under - 0.04f) * (p + 1) / pieces;
                            Beam(c, wood, RoofAt(r, face, a, d0) + drop, RoofAt(r, face, a, d1) + drop, 0.09f, 0.11f, c.Accent, p == 0);
                        }
                    }
                }

                if (c.Lod == 0 && tile)
                {
                    // The rows of cover tiles running down the slope, each ending in a round face at the eave.
                    var count = (int)(widest / TileW);

                    for (var k = -count; k <= count; k++)
                    {
                        var a = k * TileW;
                        var end = face < 2 ? (r.Hw - Math.Abs(a) >= r.E ? deep : Math.Min(deep, r.Hw - Math.Abs(a))) : Math.Min(deep, r.Hd - Math.Abs(a));

                        if (end < 0.3f || Math.Abs(a) > widest - 0.12f)
                        {
                            continue;
                        }

                        TileRow(c, cover, r, face, a, end - 0.05f, slope);
                    }
                }
            }

            Ridges(c, r);
        }

        static Tint RoofTint(Ctx c, V3 p, float k)
        {
            var patch = 0.5f + 0.5f * c.Noise.Fbm(p.X * 0.35f + 7, p.Z * 0.35f + p.Y * 0.2f, 3);
            var tint = c.Roof.Times(k * (0.86f + 0.24f * patch));
            var moss = Build.Clamp01((c.Wear * 1.3f - 0.25f) * Noise.Smooth(0.45f, 0.8f, 0.5f + 0.5f * c.Noise.Fbm(p.X * 0.21f - 3, p.Z * 0.21f + 11, 3)));

            return Tint.Mix(tint, new Tint(tint.R * 0.62f, tint.G * 0.95f, tint.B * 0.5f), moss * 0.7f);
        }

        static void TileRow(Ctx c, MeshData mesh, RoofShape r, int face, float a, float end, float slope)
        {
            var segments = Math.Max(3, (int)(end / 0.7f));
            var across = Outward(r, face);
            var side = r.Turn ? (face < 2 ? new V3(0, 0, 1) : new V3(1, 0, 0)) : (face < 2 ? new V3(1, 0, 0) : new V3(0, 0, 1));
            var start = mesh.VertexCount;
            var half = TileW * 0.27f;
            var high = 0.06f;

            for (var i = 0; i <= segments; i++)
            {
                var d = end * i / segments;
                var p = RoofAt(r, face, a, d);
                var tint = RoofTint(c, p, 1.1f);
                var v = d * slope / RoofTex;
                var offsets = new[] { (-half, 0f, -0.9f), (-half * 0.6f, high, -0.4f), (half * 0.6f, high, 0.4f), (half, 0f, 0.9f) };

                foreach (var (o, h, nx) in offsets)
                {
                    var q = p + side * o + new V3(0, h, 0);
                    var index = mesh.Vertex(q.X, q.Y, q.Z, (a + o) / RoofTex, v, tint.R, tint.G, tint.B, 1);
                    var n = (side * nx + new V3(0, 0.6f, 0)).Normalized;
                    mesh.SetNormal(index, n.X, n.Y, n.Z);
                }
            }

            for (var i = 0; i < segments; i++)
            {
                for (var j = 0; j < 3; j++)
                {
                    var v0 = start + i * 4 + j;
                    Wind(mesh, v0, v0 + 1, v0 + 5, v0 + 4, V3.Up);
                }
            }

            // The round end at the eave.
            var e = RoofAt(r, face, a, 0);
            var front = across * 0.012f;
            Quad(c, mesh, e - side * half + new V3(0, -0.07f, 0) + front, e + side * half + new V3(0, -0.07f, 0) + front, e + side * half + new V3(0, high, 0) + front, e - side * half + new V3(0, high, 0) + front, across, c.Roof.Times(0.8f), "tile");
        }

        static void Ridges(Ctx c, RoofShape r)
        {
            if (c.Lod == 3)
            {
                return;
            }

            var s = c.S;
            var tile = s.Cover == RoofCover.Tile;
            var mesh = c.M(tile ? "tile" : s.Cover == RoofCover.Thatch ? "thatch" : "wood");
            var tint = tile ? c.Roof.Times(0.72f) : s.Cover == RoofCover.Thatch ? White.Times(0.85f) : c.Frame.Times(1.4f);
            var wide = tile ? 0.24f + 0.08f * s.Ornament : s.Cover == RoofCover.Thatch ? 0.5f : 0.18f;
            var high = tile ? 0.26f + 0.14f * s.Ornament : s.Cover == RoofCover.Thatch ? 0.3f : 0.1f;
            var horn = tile ? s.Ornament * (0.5f + r.Upturn * 0.5f) * (r.Skirt ? 0.7f : 1) : 0;
            var top = r.Skirt ? r.Cut : r.Hd;

            List<V3> Line(int face, Func<float, float> a, float d0, float d1)
            {
                var points = new List<V3>();
                var steps = c.Lod == 0 ? 8 : c.Lod == 1 ? 5 : 3;

                for (var i = 0; i <= steps; i++)
                {
                    var d = d0 + (d1 - d0) * i / steps;
                    points.Add(RoofAt(r, face, a(d), d));
                }

                return points;
            }

            // The hips (and a gabled roof's verges), from the top down to each corner.
            for (var face = 0; face < 2; face++)
            {
                for (var side = -1; side <= 1; side += 2)
                {
                    var sign = side;

                    if (r.E > 0.01f)
                    {
                        RidgeLine(c, mesh, Line(face, d => sign * Span(r, face, d), Math.Min(top, r.E), 0), wide, high, tint, horn);
                    }

                    if (!r.Skirt && r.Kind != RoofKind.Pyramid && r.Kind != RoofKind.Hip)
                    {
                        // The verge ridge down the main slope beside the gable.
                        var inset = r.Hw - r.E - (r.E > 0.01f ? 0.12f : 0.16f);
                        RidgeLine(c, mesh, Line(face, d => sign * inset, r.Hd, r.E > 0.01f ? r.E : 0), wide * 0.9f, high * 0.9f, tint, r.E > 0.01f ? 0 : horn * 0.6f);
                    }
                }
            }

            if (r.Skirt)
            {
                return;
            }

            var apex = r.Y0 + r.Rise;
            V3 Place(float x, float y, float z) => r.Turn ? new V3(z, y, x) : new V3(x, y, z);
            var half = r.Hw - Math.Min(r.Hd, r.E);

            if (half > 0.2f)
            {
                // The main ridge: sagging a little with age, a horn at each end, the clan's moon at its middle.
                var points = new List<V3>();
                var steps = c.Lod <= 1 ? 8 : 2;

                for (var i = 0; i <= steps; i++)
                {
                    var f = i / (float)steps * 2 - 1;
                    points.Add(Place(half * f, apex - c.Wear * 0.12f * (1 - f * f) + (tile ? 0.1f * s.Ornament * f * f : 0), 0));
                }

                for (var i = 0; i < points.Count - 1; i++)
                {
                    Beam(c, mesh, points[i] + V3.Up * (high * 0.6f), points[i + 1] + V3.Up * (high * 0.6f), wide * 1.2f, high * 1.5f, tint);
                }

                if (tile && c.Lod <= 1 && s.Ornament > 0.2f)
                {
                    for (var side = -1; side <= 1; side += 2)
                    {
                        var end = points[side < 0 ? 0 : points.Count - 1] + V3.Up * (high * 1.2f);
                        var inward = Place(-side, 0, 0);
                        var size = 0.35f + 0.6f * s.Ornament;
                        Beam(c, mesh, end, end + V3.Up * size + inward * (-0.1f * size), wide * 1.3f, wide * 1.5f, tint);
                        Beam(c, mesh, end + V3.Up * size + inward * (-0.1f * size), end + V3.Up * (size * 1.5f) + inward * (0.35f * size), wide * 0.9f, wide, tint);
                    }
                }

                if (c.Moon && s.Ornament > 0.8f && c.Lod <= 1)
                {
                    // A crescent of gilt bronze.
                    var centre = Place(0, apex + high * 1.5f + 0.75f, 0);
                    var along = Place(1, 0, 0);
                    V3 last = default;

                    for (var i = 0; i <= 8; i++)
                    {
                        var angle = (float)Math.PI * (0.62f + 1.76f * i / 8f);
                        var p = centre + along * ((float)Math.Cos(angle) * 0.55f) + V3.Up * ((float)Math.Sin(angle) * 0.55f);

                        if (i > 0)
                        {
                            var thick = 0.05f + 0.1f * (float)Math.Sin(Math.PI * (i - 0.5f) / 8f);
                            Beam(c, c.Plain, last, p, 0.07f, thick, Gold);
                        }

                        last = p;
                    }

                    Beam(c, c.Plain, centre - V3.Up * 0.75f, centre - V3.Up * 0.52f, 0.08f, 0.08f, Gold);
                }
            }
            else if (c.Lod <= 2)
            {
                // A pointed roof's finial: a drum, two gourds and a spike of gilt bronze.
                var plain = c.Plain;
                var size = 0.5f + 0.5f * s.Ornament;
                var y = apex - 0.1f;
                Drum(c, c.M(tile ? "tile" : "wood"), 0, 0, y, y + 0.35f * size, 0.5f * size, 8, tint);
                Drum(c, plain, 0, 0, y + 0.35f * size, y + 0.85f * size, 0.3f * size, 8, Gold);
                Drum(c, plain, 0, 0, y + 0.85f * size, y + 1.2f * size, 0.2f * size, 8, Gold.Times(0.9f));
                Drum(c, plain, 0, 0, y + 1.2f * size, y + 2.1f * size, 0.05f * size, 6, Gold);
            }

            if (r.Kind == RoofKind.HipGable && r.E > 0.01f && c.Lod <= 2)
            {
                // The little gables above the end slopes: a wall, a board along its slopes, a gilt drop at its point.
                var x = r.Hw - r.E - 0.3f;
                var foot = RoofAt(r, 0, 0, r.E).Y - 0.05f;
                var reach = r.Hd - r.E;
                var columns = c.Lod == 0 ? 8 : 4;
                var wall = c.M("plaster");

                for (var side = -1; side <= 1; side += 2)
                {
                    for (var j = 0; j < columns; j++)
                    {
                        var z0 = -reach + 2 * reach * j / columns;
                        var z1 = -reach + 2 * reach * (j + 1) / columns;
                        var y0 = RoofAt(r, 0, 0, r.Hd - Math.Abs(z0)).Y - 0.1f;
                        var y1 = RoofAt(r, 0, 0, r.Hd - Math.Abs(z1)).Y - 0.1f;
                        Quad(c, wall, Place(side * x, foot, z0), Place(side * x, foot, z1), Place(side * x, Math.Max(foot, y1), z1), Place(side * x, Math.Max(foot, y0), z0), Place(side, 0, 0), s.Lacquer ? c.Accent.Times(1.5f) : c.Wall, "plaster");

                        if (c.Lod <= 1)
                        {
                            Beam(c, c.M("wood"), Place(side * (x + 0.06f), Math.Max(foot, y0) - 0.1f, z0), Place(side * (x + 0.06f), Math.Max(foot, y1) - 0.1f, z1), 0.07f, 0.3f, c.Frame);
                        }
                    }

                    if (c.Lod <= 1 && s.Ornament > 0.4f)
                    {
                        Beam(c, c.Plain, Place(side * (x + 0.12f), apex - 0.5f, 0), Place(side * (x + 0.12f), apex - 1.3f - 0.6f * s.Ornament, 0), 0.05f, 0.34f, Gold);
                    }
                }
            }
        }

        /// <summary>A ridge laid along points of the roof (from the top down), ending in a horn that sweeps out and up.</summary>
        static void RidgeLine(Ctx c, MeshData mesh, List<V3> points, float wide, float high, Tint tint, float horn)
        {
            for (var i = 0; i < points.Count - 1; i++)
            {
                Beam(c, mesh, points[i] + V3.Up * (high * 0.4f), points[i + 1] + V3.Up * (high * 0.4f), wide, high, tint);
            }

            if (horn <= 0.05f || c.Lod > 1)
            {
                return;
            }

            var last = points[points.Count - 1];
            var dir = last - points[points.Count - 2];
            dir = new V3(dir.X, 0, dir.Z).Normalized;
            var p = last + V3.Up * (high * 0.4f);
            var lifts = new[] { 0.12f, 0.32f, 0.6f };

            for (var i = 0; i < 3; i++)
            {
                var q = p + dir * (0.42f * horn) + V3.Up * (lifts[i] * horn);
                Beam(c, mesh, p, q, wide * (1 - 0.25f * (i + 1)), high * (1 - 0.25f * (i + 1)), tint);
                p = q;
            }

            if (c.Lod == 0 && c.S.Ornament > 0.5f)
            {
                // The little beasts that walk down the ridge toward the corner.
                var count = 2 + (int)(c.S.Ornament * 3);

                for (var i = 0; i < count && points.Count > 2; i++)
                {
                    var at = V3.Lerp(points[points.Count - 2], last, 0.25f + 0.7f * i / count) + V3.Up * (high * 0.9f);
                    Box(c, c.Plain, new V3(at.X - 0.06f, at.Y, at.Z - 0.06f), new V3(at.X + 0.06f, at.Y + 0.16f + 0.03f * i, at.Z + 0.06f), new Tint(0.16f, 0.17f, 0.2f), null, true);
                }
            }
        }

        /// <summary>The triangles of wall under a gabled roof's ends, with the timbers that show there.</summary>
        static void GableWalls(Ctx c, RoofShape r, float kw, float kd, bool turn)
        {
            var wall = c.M(c.WallTex);
            var tint = c.WallTex == "plaster" ? c.Wall : c.WallTex == "plank" ? c.Frame.Times(2.4f) : White;
            var columns = c.Lod <= 1 ? 8 : 2;
            V3 Place(float x, float y, float z) => turn ? new V3(z, y, x) : new V3(x, y, z);
            float Top(float z) => r.Y0 + r.Rise * (float)Math.Pow(Build.Clamp01((r.Hd - Math.Abs(z)) / r.Hd), 1 + r.Curve) - r.Th;

            for (var side = -1; side <= 1; side += 2)
            {
                for (var j = 0; j < columns; j++)
                {
                    var z0 = -kd + 2 * kd * j / columns;
                    var z1 = -kd + 2 * kd * (j + 1) / columns;
                    Quad(c, wall, Place(side * kw, r.Y0 - 0.3f, z0), Place(side * kw, r.Y0 - 0.3f, z1), Place(side * kw, Top(z1), z1), Place(side * kw, Top(z0), z0), Place(side, 0, 0), tint, c.WallTex);
                }

                if (c.Lod <= 1)
                {
                    var wood = c.M("wood");
                    var x = side * (kw + 0.05f);
                    Beam(c, wood, Place(x, r.Y0 + 0.02f, -kd), Place(x, r.Y0 + 0.02f, kd), 0.1f, 0.2f, c.Frame);
                    Beam(c, wood, Place(x, r.Y0, 0), Place(x, Top(0), 0), 0.1f, 0.18f, c.Frame);
                    Beam(c, wood, Place(x, r.Y0, -kd * 0.5f), Place(x, Top(kd * 0.5f), -kd * 0.5f), 0.1f, 0.14f, c.Frame);
                    Beam(c, wood, Place(x, r.Y0, kd * 0.5f), Place(x, Top(kd * 0.5f), kd * 0.5f), 0.1f, 0.14f, c.Frame);
                }
            }
        }

        // ------------------------------------------------------------------ Pieces

        internal static void Shade(Ctx c, V3 p, Tint tint, out float r, out float g, out float b)
        {
            // Stains where the weather has run, and the ground's dirt at the foot.
            var stain = 1 - (0.08f + 0.22f * c.Wear) * (0.5f + 0.5f * c.Noise.Fbm(p.X * 0.6f + p.Y * 0.35f, p.Z * 0.6f - p.Y * 0.2f, 2));
            var above = c.Floor != null ? p.Y - c.Floor(p.X, p.Z) : p.Y;
            var dirt = 1 - (0.1f + 0.22f * c.Wear) * (float)Math.Exp(-Math.Max(0, above) / 0.45f);
            var k = stain * dirt;
            r = Math.Min(1, tint.R * k);
            g = Math.Min(1, tint.G * k);
            b = Math.Min(1, tint.B * k);
        }

        /// <summary>Two triangles for a quad whose corners run round it, facing `outward`.</summary>
        internal static void Wind(MeshData mesh, int a, int b, int cc, int d, V3 outward)
        {
            V3 At(int i) => new V3(mesh.Positions[i * 3], mesh.Positions[i * 3 + 1], mesh.Positions[i * 3 + 2]);
            var n = V3.Cross(At(b) - At(a), At(cc) - At(a));

            if (n.Length < 1e-9f)
            {
                n = V3.Cross(At(cc) - At(a), At(d) - At(a));
            }

            if (V3.Dot(n, outward) >= 0)
            {
                mesh.Triangle(a, b, cc);
                mesh.Triangle(a, cc, d);
            }
            else
            {
                mesh.Triangle(a, cc, b);
                mesh.Triangle(a, d, cc);
            }
        }

        /// <summary>A flat quad a → b → cc → d facing `outward`, its texture laid by the world's metres (tex null: untextured).</summary>
        internal static void Quad(Ctx c, MeshData mesh, V3 a, V3 b, V3 cc, V3 d, V3 outward, Tint tint, string tex)
        {
            var n = V3.Cross(b - a, cc - a);

            if (n.Length < 1e-9f)
            {
                n = V3.Cross(cc - a, d - a);
            }

            n = n.Normalized;

            if (V3.Dot(n, outward) < 0)
            {
                n = n * -1;
            }

            var size = tex == null ? 1 : Size(tex);
            var tu = Math.Abs(n.Y) > 0.7f ? new V3(1, 0, 0) : V3.Cross(V3.Up, n).Normalized;
            var tv = Math.Abs(n.Y) > 0.7f ? new V3(0, 0, 1) : V3.Up;
            var glow = tex == null;
            var start = mesh.VertexCount;

            foreach (var p in new[] { a, b, cc, d })
            {
                float r, g, bl;

                if (glow)
                {
                    r = tint.R;
                    g = tint.G;
                    bl = tint.B;
                }
                else
                {
                    Shade(c, p, tint, out r, out g, out bl);
                }

                var v = mesh.Vertex(p.X, p.Y, p.Z, V3.Dot(p, tu) / size, V3.Dot(p, tv) / size, r, g, bl, 1);
                mesh.SetNormal(v, n.X, n.Y, n.Z);
            }

            Wind(mesh, start, start + 1, start + 2, start + 3, n);
        }

        /// <summary>A box between two corners (its underside left out unless asked).</summary>
        internal static void Box(Ctx c, MeshData mesh, V3 min, V3 max, Tint tint, string tex, bool bottom = false)
        {
            var x0 = min.X;
            var x1 = max.X;
            var y0 = min.Y;
            var y1 = max.Y;
            var z0 = min.Z;
            var z1 = max.Z;
            Quad(c, mesh, new V3(x0, y0, z0), new V3(x1, y0, z0), new V3(x1, y1, z0), new V3(x0, y1, z0), new V3(0, 0, -1), tint, tex);
            Quad(c, mesh, new V3(x1, y0, z1), new V3(x0, y0, z1), new V3(x0, y1, z1), new V3(x1, y1, z1), new V3(0, 0, 1), tint, tex);
            Quad(c, mesh, new V3(x0, y0, z1), new V3(x0, y0, z0), new V3(x0, y1, z0), new V3(x0, y1, z1), new V3(-1, 0, 0), tint, tex);
            Quad(c, mesh, new V3(x1, y0, z0), new V3(x1, y0, z1), new V3(x1, y1, z1), new V3(x1, y1, z0), new V3(1, 0, 0), tint, tex);
            Quad(c, mesh, new V3(x0, y1, z0), new V3(x1, y1, z0), new V3(x1, y1, z1), new V3(x0, y1, z1), V3.Up, tint, tex);

            if (bottom)
            {
                Quad(c, mesh, new V3(x0, y0, z0), new V3(x1, y0, z0), new V3(x1, y0, z1), new V3(x0, y0, z1), new V3(0, -1, 0), tint, tex);
            }
        }

        /// <summary>A box on a wall's face: from `o` (its lower left, seen from outside) `w` along the wall, `h` up, standing `d` proud of it.</summary>
        internal static void BoxOn(Ctx c, MeshData mesh, V3 o, V3 t, V3 outward, float w, float h, float d, Tint tint, string tex)
        {
            if (w <= 0 || h <= 0)
            {
                return;
            }

            var a = o + V3.Up * (h * 0.5f) + outward * (d * 0.5f);
            Beam(c, mesh, a, a + t * w, d, h, tint, true, tex);
        }

        /// <summary>A beam of section `w` (across) × `h` (up) from a to b, the wood's grain along it.</summary>
        internal static void Beam(Ctx c, MeshData mesh, V3 a, V3 b, float w, float h, Tint tint, bool caps = true, string tex = "wood")
        {
            var axis = b - a;
            var length = axis.Length;

            if (length < 1e-4f)
            {
                return;
            }

            axis = axis * (1 / length);
            var side = Math.Abs(axis.Y) > 0.95f ? new V3(1, 0, 0) : V3.Cross(V3.Up, axis).Normalized;
            var up = V3.Cross(axis, side).Normalized;
            var hs = side * (w * 0.5f);
            var hu = up * (h * 0.5f);
            var size = tex == null ? 1 : Size(tex);
            var shift = a.X * 0.37f + a.Z * 0.71f + a.Y * 0.13f;

            void Face(V3 p0, V3 p1, V3 p2, V3 p3, V3 n, float across)
            {
                var start = mesh.VertexCount;
                var corners = new[] { p0, p1, p2, p3 };
                var us = new[] { 0f, length, length, 0f };
                var vs = new[] { 0f, 0f, across, across };

                for (var i = 0; i < 4; i++)
                {
                    float r, g, bl;

                    if (tex == null)
                    {
                        r = tint.R;
                        g = tint.G;
                        bl = tint.B;
                    }
                    else
                    {
                        Shade(c, corners[i], tint, out r, out g, out bl);
                    }

                    var v = mesh.Vertex(corners[i].X, corners[i].Y, corners[i].Z, us[i] / size + shift, vs[i] / size + shift * 0.5f, r, g, bl, 1);
                    mesh.SetNormal(v, n.X, n.Y, n.Z);
                }

                Wind(mesh, start, start + 1, start + 2, start + 3, n);
            }

            Face(a + hs - hu, b + hs - hu, b + hs + hu, a + hs + hu, side, h);
            Face(a - hs + hu, b - hs + hu, b - hs - hu, a - hs - hu, side * -1, h);
            Face(a - hs + hu, b - hs + hu, b + hs + hu, a + hs + hu, up, w);
            Face(a + hs - hu, b + hs - hu, b - hs - hu, a - hs - hu, up * -1, w);

            if (caps)
            {
                Quad(c, mesh, a - hs - hu, a + hs - hu, a + hs + hu, a - hs + hu, axis * -1, tex == null ? tint : tint.Times(0.85f), tex);
                Quad(c, mesh, b - hs - hu, b + hs - hu, b + hs + hu, b - hs + hu, axis, tex == null ? tint : tint.Times(0.85f), tex);
            }
        }

        /// <summary>A round post from y0 to y1, the grain running up it.</summary>
        internal static void Post(Ctx c, MeshData mesh, float x, float z, float y0, float y1, float radius, int sides, Tint tint)
        {
            var start = mesh.VertexCount;
            var size = Size("wood");
            var shift = x * 0.31f + z * 0.57f;

            for (var i = 0; i <= sides; i++)
            {
                var a = Build.Tau * i / sides;
                var nx = (float)Math.Cos(a);
                var nz = (float)Math.Sin(a);

                for (var end = 0; end < 2; end++)
                {
                    var p = new V3(x + nx * radius, end == 0 ? y0 : y1, z + nz * radius);
                    Shade(c, p, tint, out var r, out var g, out var b);
                    var v = mesh.Vertex(p.X, p.Y, p.Z, p.Y / size + shift, a * radius / size, r, g, b, 1);
                    mesh.SetNormal(v, nx, 0, nz);
                }
            }

            for (var i = 0; i < sides; i++)
            {
                var v0 = start + i * 2;
                var mid = Build.Tau * (i + 0.5f) / sides;
                Wind(mesh, v0, v0 + 2, v0 + 3, v0 + 1, new V3((float)Math.Cos(mid), 0, (float)Math.Sin(mid)));
            }
        }

        /// <summary>A short round solid with a flat top (a post's stone foot, a finial's stage).</summary>
        internal static void Drum(Ctx c, MeshData mesh, float x, float z, float y0, float y1, float radius, int sides, Tint tint)
        {
            var plain = ReferenceEquals(mesh, c.Plain);

            for (var i = 0; i < sides; i++)
            {
                var a0 = Build.Tau * i / sides;
                var a1 = Build.Tau * (i + 1) / sides;
                var p0 = new V3(x + (float)Math.Cos(a0) * radius, 0, z + (float)Math.Sin(a0) * radius);
                var p1 = new V3(x + (float)Math.Cos(a1) * radius, 0, z + (float)Math.Sin(a1) * radius);
                var mid = new V3((float)Math.Cos((a0 + a1) * 0.5f), 0, (float)Math.Sin((a0 + a1) * 0.5f));
                Quad(c, mesh, p0 + V3.Up * y0, p1 + V3.Up * y0, p1 + V3.Up * y1, p0 + V3.Up * y1, mid, tint, plain ? null : "stone");
                Quad(c, mesh, new V3(x, y1, z), p0 + V3.Up * y1, p1 + V3.Up * y1, new V3(x, y1, z), V3.Up, tint, plain ? null : "stone");
            }
        }
    }
}
