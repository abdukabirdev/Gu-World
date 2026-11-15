using System;
using GuWorld.Core.Gen.Flora;
using static GuWorld.Core.Gen.Buildings.BuildingGen;

namespace GuWorld.Core.Gen.Buildings
{
    /// <summary>
    /// The things round a village, outside its houses: haystacks and drying racks by the fields,
    /// scarecrows and wattle fences in them, a roadside shrine and a stele by the road, a signpost
    /// where roads part, lantern posts and banner poles by the gate, barricades of stakes before it,
    /// a well, a woodpile, a cart, graves on the hillside. Each from a seed, with four levels of
    /// detail and a solid; standing on its origin, its front toward -Z.
    /// </summary>
    public static class ThingGen
    {
        public static readonly string[] Kinds =
        {
            "haystack", "drying_rack", "scarecrow", "wattle", "shrine", "stele", "signpost", "lantern_post",
            "banner_pole", "barricade", "well", "woodpile", "cart", "grave",
            "jars", "washing", "hide_rack", "weapon_rack", "dummy", "stall", "table", "barrels", "garden",
            "training", "herbs", "hives", "censer", "plaza",
        };

        /// <summary>The room a thing takes on the ground (width × depth, metres).</summary>
        public static (float width, float depth) Footprint(string kind)
        {
            switch (kind)
            {
                case "haystack": return (4.4f, 4.4f);
                case "drying_rack": return (5.5f, 1.6f);
                case "scarecrow": return (1.6f, 0.6f);
                case "wattle": return (6f, 0.4f);
                case "shrine": return (2.4f, 2.0f);
                case "stele": return (2.2f, 1.6f);
                case "signpost": return (1.6f, 1.6f);
                case "lantern_post": return (1.2f, 1.0f);
                case "banner_pole": return (1.2f, 1.2f);
                case "barricade": return (5f, 2.2f);
                case "well": return (3.4f, 3.4f);
                case "woodpile": return (3.2f, 1.4f);
                case "cart": return (2f, 4.4f);
                case "jars": return (2.2f, 1.4f);
                case "washing": return (5f, 0.8f);
                case "hide_rack": return (2.6f, 1.2f);
                case "weapon_rack": return (3f, 0.9f);
                case "dummy": return (1f, 1f);
                case "stall": return (3.4f, 2.6f);
                case "table": return (2.4f, 2.4f);
                case "barrels": return (2f, 1.6f);
                case "garden": return (8f, 6f);
                case "training": return (22f, 16f);
                case "herbs": return (8f, 6f);
                case "hives": return (6.4f, 1.8f);
                case "censer": return (2.6f, 2.6f);
                case "plaza": return (36f, 24f);
                default: return (2.4f, 3.2f);
            }
        }

        public static FortModel Make(string kind, int seed, FortParams p)
        {
            var model = new FortModel();
            var rng = new Rng(seed * 92821 + kind.Length);
            var size = rng.Range(0.85f, 1.15f);

            for (var lod = 0; lod < 4; lod++)
            {
                var clan = Factions.Clan(p.Clan);
                var accent = Factions.Accent(p.Clan);
                var c = new Ctx
                {
                    S = Buildings.House, Seed = seed, Lod = lod, Out = new TreeLod(), Noise = new Noise(seed), Wealth = Build.Clamp01(p.Wealth), Wear = Build.Clamp01(p.Wear),
                    Frame = new Tint(0.2f, 0.135f, 0.095f), Accent = accent, Wall = new Tint(0.92f, 0.9f, 0.85f), Roof = new Tint(1, 1, 1), Leaf = accent.Times(0.85f),
                    WallTex = "plaster", DadoTex = "brick", RoofTexName = "tile", Moon = clan == "gu_yue", Paper = Factions.Lantern(p.Clan),
                };

                switch (kind)
                {
                    case "haystack": Haystack(c, size); break;
                    case "drying_rack": DryingRack(c, size); break;
                    case "scarecrow": Scarecrow(c); break;
                    case "wattle": Wattle(c); break;
                    case "shrine": Shrine(c, size); break;
                    case "stele": Stele(c, size); break;
                    case "signpost": Signpost(c); break;
                    case "lantern_post": LanternPost(c); break;
                    case "banner_pole": BannerPole(c, size); break;
                    case "barricade": Barricade(c); break;
                    case "well": Well(c); break;
                    case "woodpile": Woodpile(c); break;
                    case "cart": Cart(c); break;
                    case "jars": Jars(c); break;
                    case "washing": Washing(c); break;
                    case "hide_rack": HideRack(c); break;
                    case "weapon_rack": WeaponRack(c); break;
                    case "dummy": Dummy(c); break;
                    case "stall": Stall(c, size); break;
                    case "table": Table(c); break;
                    case "barrels": Barrels(c); break;
                    case "garden": Garden(c, false); break;
                    case "herbs": Garden(c, true); break;
                    case "training": Training(c, seed, p); break;
                    case "hives": Hives(c); break;
                    case "censer": Censer(c); break;
                    case "plaza": Plaza(c); break;
                    default: Grave(c, size); break;
                }

                model.Lods[lod] = c.Out;
            }

            var solid = new MeshData();

            foreach (var part in model.Lods[3].Parts)
            {
                Build.Append(solid, part.Mesh, new V3(0, 0, 0));
            }

            model.Collision = solid;

            return model;
        }

        static int Sides(Ctx c, int near) => c.Lod == 0 ? near : c.Lod == 1 ? Math.Max(4, near / 2) : Math.Max(3, near / 3);

        /// <summary>A round solid turned about the vertical: `profile` gives (height, radius) pairs from the bottom up.</summary>
        static void Lathe(Ctx c, MeshData mesh, V3 at, (float y, float r)[] profile, int sides, Tint tint, string tex, Func<int, int, float> wobble = null)
        {
            for (var k = 0; k < profile.Length - 1; k++)
            {
                for (var i = 0; i < sides; i++)
                {
                    var a0 = Build.Tau * i / sides;
                    var a1 = Build.Tau * (i + 1) / sides;
                    float W(int ring, int around) => wobble == null ? 1 : wobble(ring, around % sides);
                    V3 P(int ring, int around, float a) => at + new V3((float)Math.Cos(a) * profile[ring].r * W(ring, around), profile[ring].y, (float)Math.Sin(a) * profile[ring].r * W(ring, around));
                    var mid = (a0 + a1) * 0.5f;
                    var slope = (profile[k].r - profile[k + 1].r) / Math.Max(0.01f, profile[k + 1].y - profile[k].y);
                    var n = new V3((float)Math.Cos(mid), slope, (float)Math.Sin(mid));
                    Quad(c, mesh, P(k, i, a0), P(k, i + 1, a1), P(k + 1, i + 1, a1), P(k + 1, i, a0), n, tint, tex);
                }
            }
        }

        /// <summary>A round log lying from a to b.</summary>
        static void Log(Ctx c, MeshData mesh, V3 a, V3 b, float r, int sides, Tint tint, bool ends = true)
        {
            var axis = (b - a).Normalized;
            var side = Math.Abs(axis.Y) > 0.95f ? new V3(1, 0, 0) : V3.Cross(V3.Up, axis).Normalized;
            var up = V3.Cross(axis, side);

            for (var i = 0; i < sides; i++)
            {
                var a0 = Build.Tau * i / sides;
                var a1 = Build.Tau * (i + 1) / sides;
                var d0 = side * ((float)Math.Cos(a0) * r) + up * ((float)Math.Sin(a0) * r);
                var d1 = side * ((float)Math.Cos(a1) * r) + up * ((float)Math.Sin(a1) * r);
                Quad(c, mesh, a + d0, b + d0, b + d1, a + d1, (d0 + d1) * 0.5f, tint, "log");

                if (ends && c.Lod <= 1)
                {
                    // The cut ends: pale wood.
                    Quad(c, c.M("wood"), a, a + d1, a + d0, a, axis * -1, new Tint(0.75f, 0.6f, 0.42f), "wood");
                    Quad(c, c.M("wood"), b, b + d0, b + d1, b, axis, new Tint(0.75f, 0.6f, 0.42f), "wood");
                }
            }
        }

        static readonly Tint Bark = new Tint(0.95f, 0.92f, 0.88f);
        static readonly Tint Straw = new Tint(1, 1, 1);

        static void Haystack(Ctx c, float size)
        {
            // A rick of straw round a pole: a fat drum under a rounded cone, combed by the wind.
            var sides = Sides(c, 18);
            var r = 1.9f * size;
            var profile = new[] { (-0.1f, r * 0.82f), (0.6f, r), (1.5f, r * 0.98f), (2.3f, r * 0.82f), (3.0f, r * 0.55f), (3.5f, r * 0.25f), (3.75f, 0.12f) };
            Lathe(c, c.M("thatch"), new V3(0, 0, 0), profile, sides, Straw, "thatch", (ring, a) => 1 + 0.06f * (c.R(ring * 31 + a, 7) - 0.5f));

            if (c.Lod <= 2)
            {
                Post(c, c.M("log"), 0, 0, 3.5f, 4.5f * size, 0.06f, 5, Bark);
            }

            if (c.Lod <= 1)
            {
                // A rope round its shoulder, a tarp of reeds on its crown.
                Lathe(c, c.M("wood"), new V3(0, 0, 0), new[] { (2.26f, r * 0.84f), (2.36f, r * 0.82f) }, sides, new Tint(0.5f, 0.4f, 0.3f), "wood");
            }
        }

        static void DryingRack(Ctx c, float size)
        {
            // A rack of bamboo poles: two trestles and three rails, sheaves of grain hung over them.
            var bamboo = c.M("bamboo");
            var w = 2.6f * size;

            for (var side = -1; side <= 1; side += 2)
            {
                Beam(c, bamboo, new V3(side * w, 0, -0.6f), new V3(side * w, 2.4f, 0), 0.07f, 0.07f, Straw, true, "bamboo");
                Beam(c, bamboo, new V3(side * w, 0, 0.6f), new V3(side * w, 2.4f, 0), 0.07f, 0.07f, Straw, true, "bamboo");
            }

            foreach (var (y, z) in new[] { (2.3f, 0f), (1.6f, -0.18f), (1.6f, 0.18f) })
            {
                Beam(c, bamboo, new V3(-w - 0.3f, y, z), new V3(w + 0.3f, y, z), 0.06f, 0.06f, Straw.Times(0.9f), true, "bamboo");
            }

            if (c.Lod <= 2)
            {
                var thatch = c.M("thatch");
                var count = (int)(w * 2 / 0.32f);

                for (var i = 0; i < count; i++)
                {
                    var x = -w + 0.16f + i * 0.32f;
                    var drop = 1.1f + 0.3f * c.R(i, 3);
                    var tint = Straw.Times(0.85f + 0.3f * c.R(i, 4));

                    for (var side = -1; side <= 1; side += 2)
                    {
                        // A sheaf over the top rail, hanging down both sides.
                        Quad(c, thatch, new V3(x - 0.13f, 2.35f, 0), new V3(x + 0.13f, 2.35f, 0), new V3(x + 0.16f, 2.35f - drop, side * 0.22f), new V3(x - 0.16f, 2.35f - drop, side * 0.22f), new V3(0, 0.3f, side), tint, "thatch");
                    }
                }
            }
        }

        static void Scarecrow(Ctx c)
        {
            var log = c.M("log");
            Post(c, log, 0, 0, -0.4f, 1.9f, 0.05f, 5, Bark);
            Beam(c, log, new V3(-0.75f, 1.45f, 0), new V3(0.75f, 1.45f, 0), 0.06f, 0.06f, Bark, true, "log");
            var thatch = c.M("thatch");
            // A straw head under a wide hat, a ragged coat on the cross.
            Lathe(c, thatch, new V3(0, 1.62f, 0), new[] { (0f, 0.12f), (0.15f, 0.16f), (0.32f, 0.08f) }, Sides(c, 8), Straw, "thatch");
            Lathe(c, thatch, new V3(0, 1.88f, 0), new[] { (0f, 0.5f), (0.06f, 0.48f), (0.28f, 0.03f) }, Sides(c, 12), Straw.Times(0.9f), "thatch");
            var cloth = c.Plain;
            var coat = new Tint(0.22f, 0.25f, 0.3f);
            Quad(c, cloth, new V3(-0.7f, 1.5f, -0.04f), new V3(0.7f, 1.5f, -0.04f), new V3(0.3f, 0.7f, -0.1f), new V3(-0.32f, 0.72f, -0.1f), new V3(0, 0, -1), coat, null);
            Quad(c, cloth, new V3(0.7f, 1.5f, 0.04f), new V3(-0.7f, 1.5f, 0.04f), new V3(-0.32f, 0.72f, 0.1f), new V3(0.3f, 0.7f, 0.1f), new V3(0, 0, 1), coat.Times(0.8f), null);
        }

        static void Wattle(Ctx c)
        {
            // A wattle fence: stakes, and withies woven in and out between them.
            var log = c.M("log");
            var wood = c.M("wood");
            var length = 6f;
            var stakes = 9;

            for (var i = 0; i < stakes; i++)
            {
                var x = -length * 0.5f + length * i / (stakes - 1);
                Post(c, log, x, 0, -0.3f, 1.25f + 0.1f * c.R(i, 1), 0.045f, Sides(c, 6), Bark);
            }

            if (c.Lod >= 2)
            {
                Quad(c, c.M("thatch"), new V3(-length * 0.5f, 0.1f, 0), new V3(length * 0.5f, 0.1f, 0), new V3(length * 0.5f, 1.1f, 0), new V3(-length * 0.5f, 1.1f, 0), new V3(0, 0, -1), new Tint(0.6f, 0.5f, 0.4f), "thatch");

                return;
            }

            for (var row = 0; row < (c.Lod == 0 ? 12 : 6); row++)
            {
                var y = 0.15f + row * (c.Lod == 0 ? 0.08f : 0.16f);

                for (var i = 0; i < stakes - 1; i++)
                {
                    var x0 = -length * 0.5f + length * i / (stakes - 1);
                    var x1 = -length * 0.5f + length * (i + 1) / (stakes - 1);
                    var z = ((i + row) % 2 == 0 ? 1 : -1) * 0.05f;
                    Beam(c, wood, new V3(x0, y, z), new V3((x0 + x1) * 0.5f, y + 0.01f, -z), 0.03f, 0.035f, new Tint(0.55f, 0.43f, 0.32f), false);
                    Beam(c, wood, new V3((x0 + x1) * 0.5f, y + 0.01f, -z), new V3(x1, y, z), 0.03f, 0.035f, new Tint(0.55f, 0.43f, 0.32f), false);
                }
            }
        }

        /// <summary>A small hip roof of tiles over a rectangle, its corners turned up, a ridge along x when it is longer that way.</summary>
        static void LittleRoof(Ctx c, float hw, float hd, float y, float rise, float lift)
        {
            var tile = c.M("tile");
            var corners = new[] { new V3(-hw, y + lift, -hd), new V3(hw, y + lift, -hd), new V3(hw, y + lift, hd), new V3(-hw, y + lift, hd) };
            var ridge = Math.Max(0, hw - hd);
            var t0 = new V3(-ridge, y + rise, 0);
            var t1 = new V3(ridge, y + rise, 0);
            var tops = new[] { (t0, t1), (t1, t1), (t1, t0), (t0, t0) };

            for (var k = 0; k < 4; k++)
            {
                var a = corners[k];
                var b = corners[(k + 1) % 4];
                var (ta, tb) = tops[k];
                var m = (a + b) * 0.5f - V3.Up * lift;
                var tm = (ta + tb) * 0.5f;
                var outward = new V3(m.X, 0, m.Z).Normalized + V3.Up;
                Quad(c, tile, a, m, tm, ta, outward, c.Roof, "tile");
                Quad(c, tile, m, b, tb, tm, outward, c.Roof, "tile");
                var drop = V3.Up * -0.1f;
                Quad(c, c.M("wood"), a + drop, m + drop, m, a, new V3(m.X, 0, m.Z), c.Frame, "wood");
                Quad(c, c.M("wood"), m + drop, b + drop, b, m, new V3(m.X, 0, m.Z), c.Frame, "wood");

                if (c.Lod <= 1)
                {
                    // The hip's ridge, ending in a little horn at the corner.
                    Beam(c, tile, a + V3.Up * 0.06f, Lerp(a, ta, 0.999f) + V3.Up * 0.06f, 0.1f, 0.1f, c.Roof.Times(0.7f), true, "tile");
                    var outCorner = new V3(a.X, 0, a.Z).Normalized;
                    Beam(c, tile, a + V3.Up * 0.06f, a + outCorner * 0.18f + V3.Up * 0.24f, 0.08f, 0.08f, c.Roof.Times(0.7f), true, "tile");
                }
            }

            if (ridge > 0.05f)
            {
                Beam(c, tile, t0 + V3.Up * 0.06f, t1 + V3.Up * 0.06f, 0.16f, 0.16f, c.Roof.Times(0.7f), true, "tile");
            }
        }

        static V3 Lerp(V3 a, V3 b, float t) => a + (b - a) * t;

        static void Shrine(Ctx c, float size)
        {
            // A roadside shrine to the land's god: a stone plinth, a little house of stone and lacquered posts under tiles, a figure and an incense burner, a lamp lit inside.
            var stone = c.M("stone");
            var w = 0.9f * size;
            Box(c, stone, new V3(-w - 0.25f, 0, -w * 0.8f - 0.25f), new V3(w + 0.25f, 0.55f, w * 0.8f + 0.25f), White, "stone");
            Box(c, stone, new V3(-w, 0.55f, -w * 0.8f + 0.1f), new V3(w, 0.65f, w * 0.8f), White.Times(0.9f), "stone");
            // The back and sides of stone, the front open between two red posts.
            Box(c, stone, new V3(-w, 0.65f, w * 0.8f - 0.18f), new V3(w, 1.9f, w * 0.8f), White, "stone");
            Box(c, stone, new V3(-w, 0.65f, -w * 0.5f), new V3(-w + 0.16f, 1.9f, w * 0.8f), White, "stone");
            Box(c, stone, new V3(w - 0.16f, 0.65f, -w * 0.5f), new V3(w, 1.9f, w * 0.8f), White, "stone");
            Post(c, c.M("wood"), -w + 0.1f, -w * 0.6f, 0.65f, 1.9f, 0.07f, Sides(c, 8), c.Accent);
            Post(c, c.M("wood"), w - 0.1f, -w * 0.6f, 0.65f, 1.9f, 0.07f, Sides(c, 8), c.Accent);
            Beam(c, c.M("wood"), new V3(-w - 0.05f, 1.86f, -w * 0.6f), new V3(w + 0.05f, 1.86f, -w * 0.6f), 0.12f, 0.12f, c.Accent);
            LittleRoof(c, w + 0.45f, w * 0.8f + 0.4f, 1.95f, 0.75f, 0.18f);

            if (c.Lod <= 1)
            {
                // The god: a seated figure of painted wood; the burner before it; a lamp.
                Lathe(c, c.Plain, new V3(0, 0.65f, 0.15f), new[] { (0f, 0.22f), (0.25f, 0.2f), (0.55f, 0.14f), (0.62f, 0.09f), (0.78f, 0.1f), (0.86f, 0.01f) }, Sides(c, 10), new Tint(0.55f, 0.3f, 0.12f), null);
                Lathe(c, c.Plain, new V3(0, 0.55f, -w * 0.8f - 0.05f), new[] { (0.1f, 0.08f), (0.22f, 0.16f), (0.32f, 0.15f), (0.33f, 0.1f) }, Sides(c, 10), new Tint(0.28f, 0.24f, 0.18f), null);
                Lamp(c, new V3(0.45f * w, 1.75f, -w * 0.2f), 0.1f);
            }

            if (c.Lod == 0)
            {
                // Sticks of incense in the burner, their tips glowing.
                for (var i = 0; i < 3; i++)
                {
                    var x = -0.05f + 0.05f * i;
                    Box(c, c.Plain, new V3(x - 0.006f, 0.85f, -w * 0.8f - 0.06f), new V3(x + 0.006f, 1.1f, -w * 0.8f - 0.05f), new Tint(0.6f, 0.15f, 0.1f), null);
                    Box(c, c.Glow, new V3(x - 0.01f, 1.1f, -w * 0.8f - 0.065f), new V3(x + 0.01f, 1.12f, -w * 0.8f - 0.045f), new Tint(1, 0.5f, 0.2f), null);
                }
            }
        }

        static void Stele(Ctx c, float size)
        {
            // A stone tablet on a tortoise-backed plinth, a carved cap, the inscription's lines cut in.
            var stone = c.M("stone");
            var w = 0.65f * size;
            var h = 2.4f * size;
            Lathe(c, stone, new V3(0, 0, 0), new[] { (-0.1f, 0.95f), (0.25f, 0.9f), (0.5f, 0.7f), (0.62f, 0.4f) }, Sides(c, 12), White.Times(0.85f), "stone", (ring, a) => 1 + (a % 4 == 0 ? 0.04f : 0));
            Box(c, stone, new V3(-w, 0.5f, -0.16f), new V3(w, 0.5f + h, 0.16f), White.Times(1.05f), "stone");
            Box(c, stone, new V3(-w - 0.08f, 0.5f + h, -0.2f), new V3(w + 0.08f, 0.5f + h + 0.5f, 0.2f), White.Times(0.95f), "stone");

            if (c.Lod <= 1)
            {
                // The rounded crown of the cap and the lines of characters.
                Lathe(c, stone, new V3(0, 0.5f + h + 0.5f, 0), new[] { (0f, 0.22f), (0.15f, 0.18f), (0.25f, 0.02f) }, 6, White.Times(0.9f), "stone");

                for (var line = 0; line < 5; line++)
                {
                    var x = -w + 0.18f + line * (2 * w - 0.36f) / 4;
                    var length = h - 0.6f - 0.4f * c.R(line, 9);

                    for (var side = -1; side <= 1; side += 2)
                    {
                        var z = side * 0.165f;
                        Quad(c, c.Plain, new V3(x - 0.03f, 0.5f + h - 0.3f - length, z), new V3(x + 0.03f, 0.5f + h - 0.3f - length, z), new V3(x + 0.03f, 0.5f + h - 0.3f, z), new V3(x - 0.03f, 0.5f + h - 0.3f, z), new V3(0, 0, side), new Tint(0.12f, 0.12f, 0.12f), null);
                    }
                }
            }
        }

        static void Signpost(Ctx c)
        {
            var wood = c.M("wood");
            Post(c, c.M("log"), 0, 0, -0.5f, 2.6f, 0.08f, Sides(c, 8), Bark);
            var arms = 2 + (int)(c.R(1, 1) * 2);

            for (var i = 0; i < arms; i++)
            {
                var yaw = (float)(c.R(i, 2) * Math.PI * 2);
                var dir = new V3((float)Math.Cos(yaw), 0, (float)Math.Sin(yaw));
                var y = 2.3f - i * 0.36f;
                var tint = c.Frame.Times(2.6f);
                Beam(c, wood, new V3(0, y, 0) + dir * 0.06f, new V3(0, y, 0) + dir * 1.0f, 0.04f, 0.24f, tint);

                if (c.Lod <= 1)
                {
                    // Its pointed end, and the place's name painted on.
                    Beam(c, wood, new V3(0, y, 0) + dir * 1.0f, new V3(0, y, 0) + dir * 1.18f, 0.04f, 0.1f, tint);
                    var side = V3.Cross(V3.Up, dir) * 0.025f;

                    for (var k = 0; k < 4; k++)
                    {
                        var at = new V3(0, y, 0) + dir * (0.25f + k * 0.17f);
                        Quad(c, c.Plain, at + side - dir * 0.05f + V3.Up * -0.06f, at + side + dir * 0.05f + V3.Up * -0.06f, at + side + dir * 0.05f + V3.Up * 0.06f, at + side - dir * 0.05f + V3.Up * 0.06f, side, new Tint(0.06f, 0.05f, 0.05f), null);
                    }
                }
            }
        }

        static void LanternPost(Ctx c)
        {
            // A post with an arm, a paper lantern hanging from it; a stone foot.
            Box(c, c.M("stone"), new V3(-0.3f, 0, -0.3f), new V3(0.3f, 0.35f, 0.3f), White.Times(0.9f), "stone");
            Post(c, c.M("wood"), 0, 0, 0.35f, 3.2f, 0.07f, Sides(c, 8), c.Accent);
            Beam(c, c.M("wood"), new V3(0, 3.0f, 0), new V3(0, 3.0f, -0.8f), 0.07f, 0.1f, c.Accent);
            Beam(c, c.M("wood"), new V3(0, 2.6f, 0), new V3(0, 3.0f, -0.5f), 0.05f, 0.05f, c.Accent);
            Lamp(c, new V3(0, 2.97f, -0.72f), 0.26f);
        }

        static void BannerPole(Ctx c, float size)
        {
            // A tall pole on a stone foot, a long banner of the clan's colour on a crosspiece, a gilt finial.
            var h = 7.5f * size;
            Lathe(c, c.M("stone"), new V3(0, 0, 0), new[] { (-0.1f, 0.5f), (0.4f, 0.45f), (0.5f, 0.3f) }, Sides(c, 8), White.Times(0.85f), "stone");
            Post(c, c.M("wood"), 0, 0, 0.4f, h, 0.09f, Sides(c, 8), c.Frame.Times(1.3f));
            Lathe(c, c.Plain, new V3(0, h, 0), new[] { (0f, 0.1f), (0.2f, 0.12f), (0.45f, 0.01f) }, Sides(c, 6), Gold, null);
            Beam(c, c.M("wood"), new V3(0, h - 0.5f, 0), new V3(0, h - 0.5f, -1.0f), 0.06f, 0.06f, c.Frame.Times(1.3f));
            var cloth = c.Plain;
            var colour = c.Accent.Times(1.1f);
            var drop = 3.6f * size;
            var sway = c.Lod <= 1 ? 6 : 2;

            for (var k = 0; k < sway; k++)
            {
                var t0 = k / (float)sway;
                var t1 = (k + 1) / (float)sway;
                float Bend(float t) => 0.12f * (float)Math.Sin(t * 5 + c.R(1, 5) * 6) * t;
                var a = new V3(Bend(t0), h - 0.5f - drop * t0, -0.05f);
                var b = new V3(Bend(t0), h - 0.5f - drop * t0, -0.95f);
                var cc = new V3(Bend(t1), h - 0.5f - drop * t1, -0.95f);
                var d = new V3(Bend(t1), h - 0.5f - drop * t1, -0.05f);
                Quad(c, cloth, a, b, cc, d, new V3(-1, 0, 0), colour, null);
                Quad(c, cloth, b, a, d, cc, new V3(1, 0, 0), colour.Times(0.85f), null);
            }

            if (c.Moon && c.Lod <= 1)
            {
                // The clan's crescent on the banner, in gold, both sides.
                for (var side = -1; side <= 1; side += 2)
                {
                    var x = side * 0.03f + 0.05f;
                    var centre = new V3(x, h - 1.5f, -0.5f);

                    for (var i = 0; i < 8; i++)
                    {
                        var a0 = (float)Math.PI * (0.62f + 1.76f * i / 8f);
                        var a1 = (float)Math.PI * (0.62f + 1.76f * (i + 1) / 8f);
                        var p0 = centre + new V3(0, (float)Math.Sin(a0) * 0.3f, (float)Math.Cos(a0) * 0.3f);
                        var p1 = centre + new V3(0, (float)Math.Sin(a1) * 0.3f, (float)Math.Cos(a1) * 0.3f);
                        Beam(c, c.Plain, p0, p1, 0.02f, 0.05f + 0.05f * (float)Math.Sin(Math.PI * (i + 0.5f) / 8), Gold);
                    }
                }
            }
        }

        static void Barricade(Ctx c)
        {
            // A beam on the ground bristling with sharpened stakes crossed through it, leaning outward (toward -Z).
            var log = c.M("log");
            var length = 4.6f;
            Log(c, log, new V3(-length * 0.5f, 0.45f, 0), new V3(length * 0.5f, 0.45f, 0), 0.18f, Sides(c, 8), Bark);
            var n = c.Lod <= 1 ? 7 : 4;

            for (var i = 0; i < n; i++)
            {
                var x = -length * 0.5f + 0.3f + (length - 0.6f) * i / (n - 1);

                for (var k = -1; k <= 1; k += 2)
                {
                    var a = new V3(x, 0.45f - 0.9f * 0.55f, k * 0.9f * 0.45f + 0.35f);
                    var b = new V3(x, 0.45f + 1.4f * 0.55f, -k * 1.4f * 0.45f - 0.35f);
                    var dir = (b - a).Normalized;
                    Log(c, log, a, b, 0.07f, Sides(c, 6), Bark.Times(0.9f), false);
                    // The points.
                    Beam(c, c.M("wood"), b, b + dir * 0.28f, 0.07f, 0.02f, new Tint(0.75f, 0.62f, 0.45f));
                    Beam(c, c.M("wood"), a, a - dir * 0.28f, 0.07f, 0.02f, new Tint(0.75f, 0.62f, 0.45f));
                }
            }
        }

        static void Well(Ctx c)
        {
            // A ring of stone round dark water, a windlass on two posts under a little roof, a bucket on its rope.
            var stone = c.M("stone");
            var sides = Sides(c, 16);
            Lathe(c, stone, new V3(0, 0, 0), new[] { (-0.1f, 1.15f), (0.75f, 1.12f), (0.8f, 1.2f), (0.9f, 1.2f), (0.92f, 0.85f) }, sides, White, "stone");
            Lathe(c, stone, new V3(0, 0, 0), new[] { (0.92f, 0.85f), (0.2f, 0.85f) }, sides, White.Times(0.6f), "stone");
            Lathe(c, c.Plain, new V3(0, 0, 0), new[] { (0.25f, 0.85f), (0.26f, 0.01f) }, sides, new Tint(0.03f, 0.05f, 0.06f), null);
            var wood = c.M("wood");

            for (var side = -1; side <= 1; side += 2)
            {
                Post(c, c.M("log"), side * 1.05f, 0, 0.2f, 2.6f, 0.08f, Sides(c, 8), Bark);
            }

            Log(c, c.M("log"), new V3(-1.1f, 1.6f, 0), new V3(1.1f, 1.6f, 0), 0.1f, Sides(c, 8), Bark);
            LittleRoof(c, 1.55f, 1.0f, 2.55f, 0.75f, 0.12f);

            if (c.Lod <= 1)
            {
                Beam(c, wood, new V3(1.1f, 1.6f, 0), new V3(1.4f, 1.6f, 0), 0.04f, 0.04f, c.Frame);
                Beam(c, wood, new V3(1.4f, 1.6f, 0), new V3(1.4f, 1.3f, 0), 0.04f, 0.04f, c.Frame);
                Box(c, c.Plain, new V3(-0.015f, 0.95f, -0.015f), new V3(0.015f, 1.55f, 0.015f), new Tint(0.45f, 0.38f, 0.28f), null);
                Lathe(c, wood, new V3(0, 0.7f, 0), new[] { (0f, 0.15f), (0.28f, 0.18f) }, Sides(c, 10), c.Frame.Times(1.8f), "wood");
            }
        }

        static void Woodpile(Ctx c)
        {
            // Split logs stacked between stakes, a bark slab over them.
            var log = c.M("log");
            var rows = 5;
            var length = 1.1f;

            for (var row = 0; row < rows; row++)
            {
                var n = 9 - row;

                for (var i = 0; i < n; i++)
                {
                    var x = -1.3f + (row % 2) * 0.13f + i * 0.28f;
                    var y = 0.14f + row * 0.25f;
                    var jitter = (c.R(row * 13 + i, 2) - 0.5f) * 0.15f;
                    Log(c, log, new V3(x, y, -length * 0.5f + jitter), new V3(x, y, length * 0.5f + jitter), 0.12f + 0.03f * c.R(row * 13 + i, 3), Sides(c, 6), Bark.Times(0.85f + 0.25f * c.R(row * 13 + i, 4)));
                }
            }

            for (var side = -1; side <= 1; side += 2)
            {
                Post(c, log, side * 1.45f, 0, -0.3f, 1.5f, 0.05f, Sides(c, 6), Bark);
            }

            Quad(c, log, new V3(-1.6f, 1.45f, -0.75f), new V3(1.6f, 1.45f, -0.75f), new V3(1.6f, 1.6f, 0.75f), new V3(-1.6f, 1.6f, 0.75f), V3.Up, Bark.Times(0.8f), "log");
        }

        static void Cart(Ctx c)
        {
            // A two-wheeled cart, its shafts resting on the ground, a few sacks in it.
            var plank = c.M("plank");
            var wood = c.M("wood");
            var tilt = 0.35f;
            V3 T(float x, float y, float z) => new V3(x, 0.75f + y + (z - 0.4f) * -(float)Math.Sin(tilt) * 0 + (0.8f - z) * 0.18f, z);
            Quad(c, plank, T(-0.85f, 0, -0.9f), T(0.85f, 0, -0.9f), T(0.85f, 0, 1.4f), T(-0.85f, 0, 1.4f), V3.Up, c.Frame.Times(2.2f), "plank");

            for (var side = -1; side <= 1; side += 2)
            {
                Quad(c, plank, T(side * 0.85f, 0, -0.9f), T(side * 0.85f, 0, 1.4f), T(side * 0.85f, 0.4f, 1.4f), T(side * 0.85f, 0.4f, -0.9f), new V3(side, 0, 0), c.Frame.Times(2f), "plank");
                // The shafts run forward to the ground.
                Beam(c, wood, T(side * 0.6f, -0.05f, -0.9f), new V3(side * 0.55f, 0.08f, -2.2f), 0.08f, 0.1f, c.Frame.Times(1.5f));
                // A wheel: rim, hub, spokes.
                var centre = new V3(side * 1.0f, 0.62f, 0.3f);
                var spokes = c.Lod == 0 ? 12 : 6;

                for (var k = 0; k < spokes; k++)
                {
                    var a0 = Build.Tau * k / spokes;
                    var a1 = Build.Tau * (k + 1) / spokes;
                    var p0 = centre + new V3(0, (float)Math.Sin(a0) * 0.62f, (float)Math.Cos(a0) * 0.62f);
                    var p1 = centre + new V3(0, (float)Math.Sin(a1) * 0.62f, (float)Math.Cos(a1) * 0.62f);
                    Beam(c, wood, p0, p1, 0.08f, 0.09f, c.Frame.Times(1.4f));

                    if (c.Lod <= 1)
                    {
                        Beam(c, wood, centre, p0, 0.04f, 0.04f, c.Frame.Times(1.4f), false);
                    }
                }

                Log(c, wood, centre - new V3(0.12f, 0, 0), centre + new V3(0.12f, 0, 0), 0.1f, 6, c.Frame.Times(1.2f), false);
            }

            if (c.Lod <= 1)
            {
                for (var i = 0; i < 3; i++)
                {
                    var at = T(-0.4f + 0.4f * i, 0, 0.2f + 0.5f * (i % 2));
                    Lathe(c, c.M("thatch"), at, new[] { (0f, 0.25f), (0.25f, 0.3f), (0.5f, 0.22f), (0.58f, 0.08f) }, Sides(c, 8), new Tint(0.75f, 0.68f, 0.58f), "thatch");
                }
            }
        }

        static void Jars(Ctx c)
        {
            // Big glazed jars for pickles and wine, lidded, in a row; a small one in front.
            var n = 2 + (int)(c.R(1, 1) * 2);

            for (var i = 0; i < n; i++)
            {
                var x = -0.75f + i * 0.62f;
                var s = 0.85f + 0.3f * c.R(i, 2);
                var glaze = c.R(i, 3) < 0.5f ? new Tint(0.16f, 0.12f, 0.09f) : new Tint(0.3f, 0.2f, 0.12f);
                Lathe(c, c.Plain, new V3(x, 0, 0), new[] { (0f, 0.16f * s), (0.1f, 0.24f * s), (0.38f, 0.3f * s), (0.62f, 0.24f * s), (0.72f, 0.14f * s), (0.76f, 0.15f * s) }, Sides(c, 12), glaze, null);
                Lathe(c, c.M("wood"), new V3(x, 0.76f * s, 0), new[] { (0f, 0.17f * s), (0.04f, 0.17f * s), (0.06f, 0.05f) }, Sides(c, 10), c.Frame.Times(1.6f), "wood");
            }

            Lathe(c, c.Plain, new V3(0.1f, 0, -0.5f), new[] { (0f, 0.08f), (0.12f, 0.13f), (0.24f, 0.1f), (0.3f, 0.06f) }, Sides(c, 8), new Tint(0.55f, 0.35f, 0.2f), null);
        }

        static void Washing(Ctx c)
        {
            // A line between two poles, cloths and a robe hung on it.
            var log = c.M("log");
            Post(c, log, -2.3f, 0, -0.3f, 2.1f, 0.05f, Sides(c, 6), Bark);
            Post(c, log, 2.3f, 0, -0.3f, 2.1f, 0.05f, Sides(c, 6), Bark);
            Box(c, c.Plain, new V3(-2.3f, 1.98f, -0.01f), new V3(2.3f, 2.0f, 0.01f), new Tint(0.5f, 0.45f, 0.35f), null);
            var colours = new[] { new Tint(0.75f, 0.73f, 0.68f), new Tint(0.25f, 0.33f, 0.45f), new Tint(0.55f, 0.2f, 0.15f), new Tint(0.4f, 0.42f, 0.3f) };
            var x = -1.9f;
            var i = 0;

            while (x < 1.9f)
            {
                var w = 0.4f + 0.5f * c.R(i, 4);
                var h = 0.6f + 0.6f * c.R(i, 5);
                var tint = colours[(int)(c.R(i, 6) * colours.Length) % colours.Length];
                var sway = 0.05f * (c.R(i, 7) - 0.5f);
                Quad(c, c.Plain, new V3(x, 1.99f, 0), new V3(x + w, 1.99f, 0), new V3(x + w, 1.99f - h, sway), new V3(x, 1.99f - h, sway), new V3(0, 0, -1), tint, null);
                Quad(c, c.Plain, new V3(x + w, 1.99f, 0.005f), new V3(x, 1.99f, 0.005f), new V3(x, 1.99f - h, sway + 0.005f), new V3(x + w, 1.99f - h, sway + 0.005f), new V3(0, 0, 1), tint.Times(0.85f), null);
                x += w + 0.15f + 0.2f * c.R(i, 8);
                i++;
            }
        }

        static void HideRack(Ctx c)
        {
            // A frame of poles with a bear's hide stretched in it to dry (the Xiong).
            var log = c.M("log");
            Beam(c, log, new V3(-1.1f, 0, 0), new V3(-1.0f, 2.3f, 0), 0.09f, 0.09f, Bark, true, "log");
            Beam(c, log, new V3(1.1f, 0, 0), new V3(1.0f, 2.3f, 0), 0.09f, 0.09f, Bark, true, "log");
            Beam(c, log, new V3(-1.2f, 2.15f, 0), new V3(1.2f, 2.15f, 0), 0.08f, 0.08f, Bark, true, "log");
            Beam(c, log, new V3(-1.2f, 0.35f, 0), new V3(1.2f, 0.35f, 0), 0.08f, 0.08f, Bark, true, "log");
            var hide = new Tint(0.2f, 0.13f, 0.08f);
            var pts = new[] { new V3(-0.55f, 0.55f, -0.04f), new V3(0.55f, 0.55f, -0.04f), new V3(0.8f, 1.2f, -0.04f), new V3(0.6f, 1.95f, -0.04f), new V3(-0.6f, 1.95f, -0.04f), new V3(-0.8f, 1.2f, -0.04f) };

            for (var side = -1; side <= 1; side += 2)
            {
                var off = new V3(0, 0, side * 0.004f);
                var n = new V3(0, 0, side * -1);
                Quad(c, c.Plain, pts[0] + off, pts[1] + off, pts[2] + off, pts[5] + off, n, hide, null);
                Quad(c, c.Plain, pts[5] + off, pts[2] + off, pts[3] + off, pts[4] + off, n, hide.Times(0.9f), null);
            }

            if (c.Lod <= 1)
            {
                // Thongs from its edges to the frame.
                foreach (var (a, b) in new[] { (pts[0], new V3(-1.0f, 0.35f, 0)), (pts[1], new V3(1.0f, 0.35f, 0)), (pts[3], new V3(1.0f, 2.15f, 0)), (pts[4], new V3(-1.0f, 2.15f, 0)), (pts[2], new V3(1.05f, 1.2f, 0)), (pts[5], new V3(-1.05f, 1.2f, 0)) })
                {
                    Beam(c, c.Plain, a, b, 0.015f, 0.015f, new Tint(0.4f, 0.32f, 0.22f), false, null);
                }
            }
        }

        static void WeaponRack(Ctx c)
        {
            // A lacquered rack of spears, halberds and staves.
            var wood = c.M("wood");
            Box(c, wood, new V3(-1.4f, 0, -0.35f), new V3(1.4f, 0.15f, 0.35f), c.Accent.Times(0.8f), "wood");

            for (var side = -1; side <= 1; side += 2)
            {
                Beam(c, wood, new V3(side * 1.35f, 0.15f, 0), new V3(side * 1.35f, 2.2f, 0), 0.1f, 0.1f, c.Accent);
            }

            Beam(c, wood, new V3(-1.45f, 1.7f, 0), new V3(1.45f, 1.7f, 0), 0.08f, 0.12f, c.Accent);

            for (var i = 0; i < 7; i++)
            {
                var x = -1.05f + i * 0.35f;
                var h = 2.5f + 0.5f * c.R(i, 1);
                Beam(c, wood, new V3(x, 0.15f, -0.05f), new V3(x, h, 0.02f), 0.04f, 0.04f, c.Frame.Times(1.5f), true);

                if (c.Lod <= 1)
                {
                    var steel = new Tint(0.55f, 0.57f, 0.6f);
                    Beam(c, c.Plain, new V3(x, h, 0.02f), new V3(x, h + 0.35f, 0.02f), 0.02f, 0.07f, steel, true, null);

                    if (i % 3 == 1)
                    {
                        // A halberd's blade and a red tassel.
                        Beam(c, c.Plain, new V3(x, h - 0.15f, 0.02f), new V3(x + 0.2f, h + 0.05f, 0.02f), 0.015f, 0.18f, steel, true, null);
                        Box(c, c.Plain, new V3(x - 0.04f, h - 0.4f, -0.02f), new V3(x + 0.04f, h - 0.15f, 0.06f), new Tint(0.6f, 0.1f, 0.08f), null);
                    }
                }
            }
        }

        static void Dummy(Ctx c)
        {
            // A wooden training dummy: a post with three arms and a leg, on a stand.
            var wood = c.M("wood");
            var tint = c.Frame.Times(1.8f);
            Box(c, wood, new V3(-0.45f, 0, -0.45f), new V3(0.45f, 0.12f, 0.45f), tint.Times(0.8f), "wood");
            Post(c, wood, 0, 0, 0.12f, 1.75f, 0.14f, Sides(c, 10), tint);
            Beam(c, wood, new V3(-0.05f, 1.35f, -0.1f), new V3(-0.25f, 1.4f, -0.5f), 0.05f, 0.05f, tint.Times(0.9f));
            Beam(c, wood, new V3(0.05f, 1.35f, -0.1f), new V3(0.25f, 1.4f, -0.5f), 0.05f, 0.05f, tint.Times(0.9f));
            Beam(c, wood, new V3(0, 1.05f, -0.1f), new V3(0, 1.0f, -0.48f), 0.05f, 0.05f, tint.Times(0.9f));
            Beam(c, wood, new V3(0, 0.55f, -0.1f), new V3(0, 0.3f, -0.45f), 0.06f, 0.06f, tint.Times(0.9f));
        }

        static void Stall(Ctx c, float size)
        {
            // A market stall: a counter, four posts, a cloth awning sloping to the front, goods laid out.
            var wood = c.M("wood");
            var plank = c.M("plank");
            var w = 1.5f * size;
            var cloth = c.Plain;
            var colours = new[] { new Tint(0.6f, 0.15f, 0.1f), new Tint(0.2f, 0.3f, 0.45f), new Tint(0.75f, 0.65f, 0.4f), new Tint(0.3f, 0.4f, 0.25f) };
            var awning = colours[(int)(c.R(1, 1) * colours.Length) % colours.Length];

            foreach (var (x, z, h) in new[] { (-w, -1.1f, 2.3f), (w, -1.1f, 2.3f), (-w, 1.0f, 2.7f), (w, 1.0f, 2.7f) })
            {
                Post(c, wood, x, z, 0, h, 0.05f, Sides(c, 6), c.Frame.Times(1.6f));
            }

            Box(c, plank, new V3(-w + 0.1f, 0, -0.9f), new V3(w - 0.1f, 0.9f, -0.4f), c.Frame.Times(2.2f), "plank");
            Box(c, plank, new V3(-w + 0.05f, 0.9f, -0.95f), new V3(w - 0.05f, 0.96f, -0.35f), c.Frame.Times(2f), "plank");
            var sag = c.Lod <= 1 ? 4 : 1;

            for (var k = 0; k < sag; k++)
            {
                var t0 = k / (float)sag;
                var t1 = (k + 1) / (float)sag;
                float Y(float t) => 2.7f - 0.5f * t - 0.06f * (float)Math.Sin(Math.PI * t);
                float Z(float t) => 1.05f - 2.4f * t;
                Quad(c, cloth, new V3(-w - 0.1f, Y(t0), Z(t0)), new V3(w + 0.1f, Y(t0), Z(t0)), new V3(w + 0.1f, Y(t1), Z(t1)), new V3(-w - 0.1f, Y(t1), Z(t1)), new V3(0, 1, -0.2f), awning, null);
                Quad(c, cloth, new V3(w + 0.1f, Y(t0) - 0.01f, Z(t0)), new V3(-w - 0.1f, Y(t0) - 0.01f, Z(t0)), new V3(-w - 0.1f, Y(t1) - 0.01f, Z(t1)), new V3(w + 0.1f, Y(t1) - 0.01f, Z(t1)), new V3(0, -1, 0), awning.Times(0.7f), null);
            }

            if (c.Lod <= 1)
            {
                // A valance along the front edge; goods on the counter: baskets, bundles, jars.
                Quad(c, cloth, new V3(-w - 0.1f, 2.2f, -1.36f), new V3(w + 0.1f, 2.2f, -1.36f), new V3(w + 0.1f, 1.95f, -1.38f), new V3(-w - 0.1f, 1.95f, -1.38f), new V3(0, 0, -1), awning.Times(0.85f), null);

                for (var i = 0; i < 5; i++)
                {
                    var x = -w + 0.35f + i * (2 * w - 0.7f) / 4;
                    var kind = (int)(c.R(i, 9) * 3);
                    var goods = new[] { new Tint(0.7f, 0.55f, 0.3f), new Tint(0.45f, 0.6f, 0.3f), new Tint(0.65f, 0.25f, 0.15f), new Tint(0.8f, 0.78f, 0.7f) }[(int)(c.R(i, 10) * 4) % 4];

                    if (kind == 0)
                    {
                        Lathe(c, c.M("thatch"), new V3(x, 0.96f, -0.65f), new[] { (0f, 0.14f), (0.12f, 0.2f), (0.14f, 0.18f) }, Sides(c, 8), new Tint(0.8f, 0.7f, 0.5f), "thatch");
                        Lathe(c, c.Plain, new V3(x, 1.05f, -0.65f), new[] { (0f, 0.16f), (0.06f, 0.12f), (0.1f, 0.01f) }, Sides(c, 8), goods, null);
                    }
                    else if (kind == 1)
                    {
                        Box(c, c.Plain, new V3(x - 0.15f, 0.96f, -0.8f), new V3(x + 0.15f, 1.1f, -0.5f), goods, null);
                    }
                    else
                    {
                        Lathe(c, c.Plain, new V3(x, 0.96f, -0.65f), new[] { (0f, 0.06f), (0.08f, 0.1f), (0.2f, 0.08f), (0.24f, 0.04f) }, Sides(c, 8), goods.Times(0.6f), null);
                    }
                }
            }
        }

        static void Table(Ctx c)
        {
            // A square table with four stools (a teahouse's or a noodle stall's, set out in the street).
            var wood = c.M("wood");
            var tint = c.Frame.Times(1.7f);
            Box(c, wood, new V3(-0.5f, 0.72f, -0.5f), new V3(0.5f, 0.78f, 0.5f), tint, "wood", true);

            foreach (var (x, z) in new[] { (-0.42f, -0.42f), (0.42f, -0.42f), (0.42f, 0.42f), (-0.42f, 0.42f) })
            {
                Box(c, wood, new V3(x - 0.04f, 0, z - 0.04f), new V3(x + 0.04f, 0.72f, z + 0.04f), tint.Times(0.85f), "wood");
            }

            foreach (var (x, z) in new[] { (0f, -0.9f), (0.9f, 0f), (0f, 0.9f), (-0.9f, 0f) })
            {
                Lathe(c, wood, new V3(x, 0, z), new[] { (0f, 0.16f), (0.42f, 0.14f), (0.45f, 0.18f) }, Sides(c, 8), tint.Times(0.9f), "wood");
            }

            if (c.Lod <= 1)
            {
                // A teapot and cups.
                Lathe(c, c.Plain, new V3(0, 0.78f, 0), new[] { (0f, 0.06f), (0.06f, 0.09f), (0.12f, 0.06f), (0.15f, 0.02f) }, 8, new Tint(0.35f, 0.25f, 0.18f), null);

                for (var i = 0; i < 3; i++)
                {
                    var a = i * 2.1f;
                    Lathe(c, c.Plain, new V3((float)Math.Cos(a) * 0.25f, 0.78f, (float)Math.Sin(a) * 0.25f), new[] { (0f, 0.025f), (0.05f, 0.035f) }, 6, new Tint(0.85f, 0.85f, 0.8f), null);
                }
            }
        }

        static void Barrels(Ctx c)
        {
            // Barrels of wine or oil, hooped, one lying on its side.
            var wood = c.M("wood");
            var tint = c.Frame.Times(2f);
            var profile = new[] { (0f, 0.3f), (0.2f, 0.36f), (0.45f, 0.38f), (0.7f, 0.36f), (0.9f, 0.3f) };
            Lathe(c, wood, new V3(-0.45f, 0, 0.1f), profile, Sides(c, 12), tint, "wood");
            Lathe(c, wood, new V3(0.35f, 0, 0.25f), profile, Sides(c, 12), tint.Times(0.9f), "wood");

            if (c.Lod <= 1)
            {
                foreach (var x in new[] { -0.45f, 0.35f })
                {
                    var z = x < 0 ? 0.1f : 0.25f;
                    Lathe(c, c.Plain, new V3(x, 0.89f, z), new[] { (0f, 0.3f), (0.01f, 0.01f) }, Sides(c, 12), tint.Times(0.7f), null);

                    foreach (var y in new[] { 0.12f, 0.75f })
                    {
                        Lathe(c, c.Plain, new V3(x, y, z), new[] { (0f, 0.35f + 0.02f * (y < 0.4f ? 0 : 1)), (0.05f, 0.36f + 0.02f * (y < 0.4f ? 0 : 1)) }, Sides(c, 12), new Tint(0.15f, 0.14f, 0.13f), null);
                    }
                }
            }

            Log(c, wood, new V3(-0.4f, 0.32f, -0.55f), new V3(0.5f, 0.32f, -0.55f), 0.32f, Sides(c, 10), tint.Times(0.95f));
        }

        static void Garden(Ctx c, bool herbs)
        {
            // A vegetable plot: dug earth in ridges, rows of greens (cabbages, leeks, beans on poles), a wattle edge.
            // The medicine faction's beds of herbs: tufts in rows of their own colours (lavender, gold flowers, dark leaves), a stake naming each.
            var mud = c.M("mud");
            var w = 3.8f;
            var d = 2.8f;
            Quad(c, mud, new V3(-w, 0.04f, -d), new V3(w, 0.04f, -d), new V3(w, 0.04f, d), new V3(-w, 0.04f, d), V3.Up, new Tint(0.55f, 0.45f, 0.35f), "mud");
            var rows = herbs ? 6 : 7;

            for (var r = 0; r < rows; r++)
            {
                var z = -d + 0.4f + r * (2 * d - 0.8f) / (rows - 1);

                if (herbs)
                {
                    var colours = new[] { new Tint(0.48f, 0.4f, 0.62f), new Tint(0.75f, 0.62f, 0.18f), new Tint(0.16f, 0.32f, 0.18f), new Tint(0.7f, 0.72f, 0.75f), new Tint(0.62f, 0.22f, 0.2f) };
                    var herb = colours[(int)(c.R(r, 9) * colours.Length) % colours.Length];
                    Beam(c, mud, new V3(-w + 0.3f, 0.06f, z), new V3(w - 0.3f, 0.06f, z), 0.5f, 0.14f, new Tint(0.45f, 0.36f, 0.28f), true, "mud");

                    for (var x = -w + 0.45f; x < w - 0.4f; x += c.Lod == 0 ? 0.32f : c.Lod == 1 ? 0.6f : 2 * w)
                    {
                        var k = 0.75f + 0.5f * c.R((int)(x * 10), r + 40);
                        var leaf = new Tint(0.22f, 0.42f, 0.2f).Times(0.85f + 0.3f * c.R(r, (int)(x * 9)));

                        if (c.Lod >= 2)
                        {
                            Quad(c, c.Plain, new V3(-w + 0.3f, 0.16f, z - 0.2f), new V3(w - 0.3f, 0.16f, z - 0.2f), new V3(w - 0.3f, 0.16f, z + 0.2f), new V3(-w + 0.3f, 0.16f, z + 0.2f), V3.Up, herb.Times(0.8f), null);
                            break;
                        }

                        Lathe(c, c.Plain, new V3(x, 0.12f, z), new[] { (0f, 0.05f * k), (0.22f * k, 0.16f * k), (0.38f * k, 0.12f * k), (0.46f * k, 0.03f) }, c.Lod == 0 ? 6 : 4, leaf, null);
                        Lathe(c, c.Plain, new V3(x, 0.12f + 0.36f * k, z), new[] { (0f, 0.1f * k), (0.1f * k, 0.12f * k), (0.18f * k, 0.02f) }, c.Lod == 0 ? 6 : 4, herb, null);
                    }

                    if (c.Lod <= 1)
                    {
                        // A stake with a little board naming the row.
                        Post(c, c.M("wood"), -w + 0.15f, z, 0, 0.7f, 0.025f, 4, c.Frame.Times(1.8f));
                        Box(c, c.M("wood"), new V3(-w + 0.05f, 0.5f, z - 0.12f), new V3(-w + 0.08f, 0.68f, z + 0.12f), new Tint(0.85f, 0.8f, 0.7f), "wood");
                    }

                    continue;
                }

                var crop = (int)(c.R(r, 1) * 3);
                var green = new[] { new Tint(0.3f, 0.5f, 0.2f), new Tint(0.4f, 0.55f, 0.25f), new Tint(0.25f, 0.42f, 0.2f) }[crop];
                // The ridge of earth the row grows on.
                Beam(c, mud, new V3(-w + 0.3f, 0.06f, z), new V3(w - 0.3f, 0.06f, z), 0.45f, 0.12f, new Tint(0.5f, 0.4f, 0.3f), true, "mud");

                if (c.Lod >= 2)
                {
                    Quad(c, c.Plain, new V3(-w + 0.3f, 0.14f, z - 0.18f), new V3(w - 0.3f, 0.14f, z - 0.18f), new V3(w - 0.3f, 0.14f, z + 0.18f), new V3(-w + 0.3f, 0.14f, z + 0.18f), V3.Up, green, null);
                    continue;
                }

                for (var x = -w + 0.5f; x < w - 0.4f; x += crop == 1 ? 0.25f : 0.5f)
                {
                    var s = 0.8f + 0.4f * c.R((int)(x * 10), r);

                    if (crop == 0)
                    {
                        Lathe(c, c.Plain, new V3(x, 0.1f, z), new[] { (0f, 0.1f * s), (0.1f, 0.2f * s), (0.2f, 0.12f * s), (0.24f, 0.02f) }, c.Lod == 0 ? 7 : 4, green.Times(0.9f + 0.2f * c.R(r, (int)(x * 7))), null);
                    }
                    else if (crop == 1)
                    {
                        Beam(c, c.Plain, new V3(x, 0.1f, z), new V3(x + 0.03f, 0.55f * s, z + 0.04f), 0.04f, 0.02f, green, false, null);
                    }
                    else if (c.Lod == 0 || ((int)(x * 2)) % 2 == 0)
                    {
                        // Beans on poles leaning together.
                        Beam(c, c.M("bamboo"), new V3(x, 0, z - 0.15f), new V3(x, 1.6f, z), 0.025f, 0.025f, White.Times(0.85f), false, "bamboo");
                        Lathe(c, c.Plain, new V3(x, 0.2f, z - 0.07f), new[] { (0f, 0.12f), (0.6f, 0.1f), (1.3f, 0.03f) }, 5, green.Times(0.85f), null);
                    }
                }
            }

            if (c.Lod <= 1)
            {
                // A low wattle on the open sides.
                foreach (var (a, b) in new[] { (new V3(-w, 0, -d), new V3(-w, 0, d)), (new V3(w, 0, -d), new V3(w, 0, d)), (new V3(-w, 0, d), new V3(w, 0, d)) })
                {
                    var length = (b - a).Length;

                    for (var k = 0; k <= (int)(length / 0.9f); k++)
                    {
                        var p = V3.Lerp(a, b, k / Math.Max(1f, (int)(length / 0.9f)));
                        Post(c, c.M("log"), p.X, p.Z, -0.2f, 0.75f, 0.03f, 5, Bark);
                    }

                    Beam(c, c.M("wood"), a + V3.Up * 0.5f, b + V3.Up * 0.5f, 0.04f, 0.05f, new Tint(0.55f, 0.43f, 0.32f), false);
                    Beam(c, c.M("wood"), a + V3.Up * 0.3f, b + V3.Up * 0.3f, 0.04f, 0.05f, new Tint(0.55f, 0.43f, 0.32f), false);
                }
            }
        }

        /// <summary>
        /// A training ground: beaten earth inside a rail of logs, open at its front; posts to strike in a
        /// row, racks of weapons, stone weights, a banner in the corner. The Xiong's is a wrestling ring:
        /// a round of earth edged with stones, the weights heavier.
        /// </summary>
        static void Training(Ctx c, int seed, FortParams p)
        {
            var mud = c.M("mud");
            var w = 10.6f;
            var d = 7.6f;
            var xiong = Factions.Clan(p.Clan) == "xiong";
            Quad(c, mud, new V3(-w, 0.04f, -d), new V3(w, 0.04f, -d), new V3(w, 0.04f, d), new V3(-w, 0.04f, d), V3.Up, new Tint(0.62f, 0.52f, 0.4f), "mud");

            void Add(string kind, V3 at, float yaw, int k)
            {
                var thing = Make(kind, seed * 31 + k, p);
                Compose.Add(c.Out, thing.Lods[c.Lod], at, yaw);
            }

            if (xiong)
            {
                // The ring: stones round a circle of trodden earth.
                var r = 5.2f;
                var n = c.Lod == 0 ? 28 : c.Lod == 1 ? 16 : 8;

                for (var i = 0; i < n; i++)
                {
                    var a = Build.Tau * i / n;
                    var x = (float)Math.Cos(a) * r;
                    var z = (float)Math.Sin(a) * r;
                    Lathe(c, c.M("stone"), new V3(x, 0, z), new[] { (-0.1f, 0.32f), (0.15f, 0.3f), (0.3f, 0.12f) }, 5, White.Times(0.85f + 0.2f * c.R(i, 2)), "stone");
                }

                Lathe(c, mud, new V3(0, 0, 0), new[] { (0.04f, r - 0.2f), (0.08f, r - 0.4f), (0.09f, 0.1f) }, Sides(c, 24), new Tint(0.55f, 0.42f, 0.3f), "mud");
            }
            else if (c.Lod <= 2)
            {
                // The rail: posts every two metres, a log along them; the front left open in its middle.
                foreach (var (a, b, front) in new[] { (new V3(-w, 0, -d), new V3(-2.5f, 0, -d), true), (new V3(2.5f, 0, -d), new V3(w, 0, -d), true), (new V3(w, 0, -d), new V3(w, 0, d), false), (new V3(w, 0, d), new V3(-w, 0, d), false), (new V3(-w, 0, d), new V3(-w, 0, -d), false) })
                {
                    var length = (b - a).Length;
                    var posts = Math.Max(1, (int)(length / 2.2f));

                    for (var k = 0; k <= posts; k++)
                    {
                        var q = V3.Lerp(a, b, k / (float)posts);
                        Post(c, c.M("log"), q.X, q.Z, -0.2f, 1.05f, 0.07f, Sides(c, 6), Bark.Times(0.85f));
                    }

                    Beam(c, c.M("log"), a + V3.Up * 0.95f, b + V3.Up * 0.95f, 0.08f, 0.08f, Bark.Times(0.8f), true, "log");
                }
            }

            if (c.Lod <= 1)
            {
                var dummies = xiong ? 2 : 5;

                for (var i = 0; i < dummies; i++)
                {
                    Add("dummy", new V3(xiong ? -w + 2 : -6 + i * 3, 0, xiong ? -3 + i * 6 : d - 2.2f), xiong ? 90 : 180, i);
                }

                Add("weapon_rack", new V3(-w + 1.1f, 0, xiong ? 4.5f : -2), -90, 10);

                if (!xiong)
                {
                    Add("weapon_rack", new V3(w - 1.1f, 0, -2), 90, 11);
                }

                // Stone weights to lift: lathed stones with a grip, the Xiong's twice as big.
                for (var i = 0; i < (xiong ? 5 : 3); i++)
                {
                    var k = (xiong ? 1.5f : 1) * (0.8f + 0.4f * c.R(i, 6));
                    var at = new V3(w - 1.6f - (i % 2) * 0.9f, 0, d - 1.6f - i * 1.1f);
                    Lathe(c, c.M("stone"), at, new[] { (0f, 0.22f * k), (0.1f * k, 0.3f * k), (0.32f * k, 0.28f * k), (0.42f * k, 0.12f * k), (0.48f * k, 0.12f * k), (0.5f * k, 0.02f) }, Sides(c, 10), White.Times(0.8f), "stone");
                }

                Add("banner_pole", new V3(-w + 1, 0, -d + 1), 0, 12);
            }
        }

        /// <summary>Bee hives of straw on a bench under a lean-to of shingles (the Xiong's honey for their Gu).</summary>
        static void Hives(Ctx c)
        {
            var wood = c.M("wood");
            Box(c, c.M("plank"), new V3(-3.0f, 0.55f, -0.45f), new V3(3.0f, 0.65f, 0.45f), c.Frame.Times(2f), "plank");

            foreach (var x in new[] { -2.8f, 0f, 2.8f })
            {
                Post(c, wood, x, -0.3f, 0, 0.55f, 0.06f, Sides(c, 6), c.Frame.Times(1.6f));
                Post(c, wood, x, 0.3f, 0, 0.55f, 0.06f, Sides(c, 6), c.Frame.Times(1.6f));
            }

            for (var i = 0; i < 5; i++)
            {
                var x = -2.4f + i * 1.2f;
                var k = 0.9f + 0.2f * c.R(i, 3);
                Lathe(c, c.M("thatch"), new V3(x, 0.65f, 0), new[] { (0f, 0.42f * k), (0.3f * k, 0.4f * k), (0.55f * k, 0.3f * k), (0.72f * k, 0.12f * k), (0.76f * k, 0.01f) }, Sides(c, 12), new Tint(1.1f, 1.0f, 0.75f), "thatch");

                if (c.Lod <= 1)
                {
                    Box(c, c.Plain, new V3(x - 0.08f, 0.66f, -0.43f * k), new V3(x + 0.08f, 0.74f, -0.4f * k), new Tint(0.05f, 0.04f, 0.03f), null);
                }
            }

            if (c.Lod <= 1)
            {
                // A shingled lean-to over them, on two posts at the back.
                foreach (var x in new[] { -3.1f, 3.1f })
                {
                    Post(c, wood, x, 0.8f, 0, 2.2f, 0.06f, Sides(c, 6), c.Frame.Times(1.5f));
                    Post(c, wood, x, -0.8f, 0, 1.75f, 0.05f, Sides(c, 6), c.Frame.Times(1.5f));
                }

                Quad(c, c.M("shingle"), new V3(-3.3f, 1.75f, -1.0f), new V3(3.3f, 1.75f, -1.0f), new V3(3.3f, 2.25f, 0.95f), new V3(-3.3f, 2.25f, 0.95f), new V3(0, 1, -0.25f), new Tint(0.9f, 0.85f, 0.78f), "shingle");
                Quad(c, c.M("shingle"), new V3(3.3f, 1.72f, -1.0f), new V3(-3.3f, 1.72f, -1.0f), new V3(-3.3f, 2.22f, 0.95f), new V3(3.3f, 2.22f, 0.95f), new V3(0, -1, 0.25f), new Tint(0.7f, 0.65f, 0.6f), "shingle");
            }
        }

        /// <summary>The paving of a square (before the clan's pavilion, on its levelled terrace): flags in rows, a kerb round it, a path of long slabs down its middle.</summary>
        static void Plaza(Ctx c)
        {
            var stone = c.M("stone");
            var w = 18f;
            var d = 12f;

            if (c.Lod >= 2)
            {
                Quad(c, stone, new V3(-w, 0.06f, -d), new V3(w, 0.06f, -d), new V3(w, 0.06f, d), new V3(-w, 0.06f, d), V3.Up, White.Times(0.85f), "stone");
                return;
            }

            var size = 2.4f;

            for (var x = -w; x < w - 0.01f; x += size)
            {
                for (var z = -d; z < d - 0.01f; z += size)
                {
                    var middle = Math.Abs(x + size * 0.5f) < size;
                    var tone = (middle ? 0.95f : 0.78f) + 0.12f * c.R((int)(x * 3), (int)(z * 3));
                    var y = 0.05f + 0.02f * c.R((int)(x * 5), (int)(z * 5));
                    var gap = c.Lod == 0 ? 0.03f : 0;
                    Quad(c, stone, new V3(x + gap, y, z + gap), new V3(x + size - gap, y, z + gap), new V3(x + size - gap, y, z + size - gap), new V3(x + gap, y, z + size - gap), V3.Up, White.Times(tone), "stone");
                }
            }

            if (c.Lod == 0)
            {
                // The joints: dark earth showing between the flags.
                Quad(c, c.M("mud"), new V3(-w, 0.03f, -d), new V3(w, 0.03f, -d), new V3(w, 0.03f, d), new V3(-w, 0.03f, d), V3.Up, new Tint(0.4f, 0.36f, 0.3f), "mud");
            }

            // The kerb.
            foreach (var (a, b) in new[] { (new V3(-w, 0, -d), new V3(w, 0, -d)), (new V3(w, 0, -d), new V3(w, 0, d)), (new V3(w, 0, d), new V3(-w, 0, d)), (new V3(-w, 0, d), new V3(-w, 0, -d)) })
            {
                Beam(c, stone, a + V3.Up * 0.02f, b + V3.Up * 0.02f, 0.35f, 0.22f, White.Times(0.7f), true, "stone");
            }
        }

        /// <summary>A bronze cauldron for incense before the clan's hall: three legs, two loop handles, on a stone step.</summary>
        static void Censer(Ctx c)
        {
            var bronze = new Tint(0.32f, 0.27f, 0.17f);
            Box(c, c.M("stone"), new V3(-1.2f, 0, -1.2f), new V3(1.2f, 0.3f, 1.2f), White.Times(0.9f), "stone");

            for (var i = 0; i < 3; i++)
            {
                var a = Build.Tau * i / 3 + 0.5f;
                Beam(c, c.Plain, new V3((float)Math.Cos(a) * 0.55f, 0.3f, (float)Math.Sin(a) * 0.55f), new V3((float)Math.Cos(a) * 0.6f, 0.85f, (float)Math.Sin(a) * 0.6f), 0.1f, 0.1f, bronze.Times(0.9f), true, null);
            }

            Lathe(c, c.Plain, new V3(0, 0.75f, 0), new[] { (0f, 0.3f), (0.15f, 0.62f), (0.5f, 0.72f), (0.75f, 0.68f), (0.8f, 0.76f), (0.86f, 0.74f) }, Sides(c, 16), bronze, null);
            Quad(c, c.Plain, new V3(-0.6f, 1.55f, -0.6f), new V3(0.6f, 1.55f, -0.6f), new V3(0.6f, 1.55f, 0.6f), new V3(-0.6f, 1.55f, 0.6f), V3.Up, new Tint(0.35f, 0.33f, 0.3f), null);

            foreach (var side in new[] { -1, 1 })
            {
                Beam(c, c.Plain, new V3(side * 0.7f, 1.58f, -0.2f), new V3(side * 0.7f, 1.95f, -0.15f), 0.07f, 0.07f, bronze, true, null);
                Beam(c, c.Plain, new V3(side * 0.7f, 1.95f, -0.15f), new V3(side * 0.7f, 1.95f, 0.15f), 0.07f, 0.07f, bronze, true, null);
                Beam(c, c.Plain, new V3(side * 0.7f, 1.95f, 0.15f), new V3(side * 0.7f, 1.58f, 0.2f), 0.07f, 0.07f, bronze, true, null);
            }

            if (c.Lod <= 1)
            {
                // Sticks of incense standing in the ash.
                for (var i = 0; i < 5; i++)
                {
                    var x = -0.3f + 0.15f * i;
                    Beam(c, c.Plain, new V3(x, 1.55f, 0.05f * (i % 2)), new V3(x, 2.0f, 0.05f * (i % 2)), 0.015f, 0.015f, new Tint(0.6f, 0.2f, 0.12f), false, null);
                }
            }
        }

        static void Grave(Ctx c, float size)
        {
            // An earth mound, a headstone before it, a stone table for offerings.
            var r = 1.0f * size;
            Lathe(c, c.M("mud"), new V3(0, 0, 0.6f), new[] { (-0.1f, r * 1.05f), (0.35f, r), (0.75f, r * 0.75f), (1.0f, r * 0.4f), (1.1f, 0.05f) }, Sides(c, 14), new Tint(0.7f, 0.75f, 0.55f), "mud", (ring, a) => 1 + 0.05f * (c.R(ring * 17 + a, 5) - 0.5f));
            var stone = c.M("stone");
            Box(c, stone, new V3(-0.42f, 0, -0.62f), new V3(0.42f, 1.15f, -0.48f), White, "stone");
            Lathe(c, stone, new V3(0, 1.15f, -0.55f), new[] { (0f, 0.42f), (0.12f, 0.3f), (0.2f, 0.02f) }, 4, White.Times(0.9f), "stone");
            Box(c, stone, new V3(-0.5f, 0, -1.25f), new V3(0.5f, 0.35f, -0.8f), White.Times(0.85f), "stone");

            if (c.Lod <= 1)
            {
                Quad(c, c.Plain, new V3(-0.05f, 0.3f, -0.625f), new V3(0.05f, 0.3f, -0.625f), new V3(0.05f, 1.0f, -0.625f), new V3(-0.05f, 1.0f, -0.625f), new V3(0, 0, -1), new Tint(0.1f, 0.1f, 0.1f), null);
                Lathe(c, c.Plain, new V3(-0.2f, 0.35f, -1.02f), new[] { (0f, 0.06f), (0.08f, 0.1f), (0.1f, 0.08f) }, 8, new Tint(0.75f, 0.72f, 0.65f), null);
                Lathe(c, c.Plain, new V3(0.2f, 0.35f, -1.02f), new[] { (0f, 0.05f), (0.18f, 0.06f), (0.2f, 0.03f) }, 8, new Tint(0.3f, 0.25f, 0.2f), null);
            }
        }
    }
}
