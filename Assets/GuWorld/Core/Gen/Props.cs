using System;
using System.Collections.Generic;

namespace GuWorld.Core.Gen
{
    /// <summary>
    /// The town's things made in code, in the style of the mountain's clans: memorial archways over the
    /// avenues, whitewashed courtyard walls with tiled copings, market stalls under cloth, hand carts,
    /// sampans, humped stone bridges, strings of red lanterns across the lanes, pagodas, drum towers,
    /// and the jade terrace of the immortal aperture. Each is a mesh of a few hundred to a few thousand
    /// triangles with its colours in its vertices, its origin at the middle of its foot, facing +z.
    /// </summary>
    public static class Props
    {
        // The palette: the clan's lacquer, stone, wood, tiles, plaster, cloth.
        public static readonly Tint Lacquer = Tint.Hex(0x9A2A22);
        public static readonly Tint LacquerDark = Tint.Hex(0x5E1A16);
        public static readonly Tint Gold = Tint.Hex(0xD8B060);
        public static readonly Tint Stone = Tint.Hex(0xA8A49A);
        public static readonly Tint StoneDark = Tint.Hex(0x7A766E);
        public static readonly Tint Wood = Tint.Hex(0x7A5636);
        public static readonly Tint WoodLight = Tint.Hex(0xA88058);
        public static readonly Tint Tiles = Tint.Hex(0x4A5560);
        public static readonly Tint TilesGreen = Tint.Hex(0x3E6A58);
        public static readonly Tint Plaster = Tint.Hex(0xE8E2D4);
        public static readonly Tint Jade = Tint.Hex(0x8FD0B0);
        public static readonly Tint JadeDeep = Tint.Hex(0x4E9A80);
        public static readonly Tint Paper = Tint.Hex(0xE85A3A);
        public static readonly Tint Rope = Tint.Hex(0x5A4630);
        public static readonly Tint[] Cloths = { Tint.Hex(0x3A6A9A), Tint.Hex(0xB04A3A), Tint.Hex(0xD8B060), Tint.Hex(0x4E8A5A), Tint.Hex(0x8A5AA0), Tint.Hex(0xE8E2D4) };

        /// <summary>The kinds the town asks for by name ("code:paifang"), with how many variants each has.</summary>
        public static readonly (string id, int variants)[] Kinds =
        {
            ("paifang", 2), ("wall", 1), ("stall", 6), ("cart", 3), ("boat", 3), ("arch_bridge", 2),
            ("lanterns", 3), ("pagoda", 2), ("drum_tower", 1), ("aperture", 1), ("dummy_post", 1), ("gu_altar", 1),
            ("town_wall", 1), ("town_gate", 1), ("corner_tower", 1), ("banner", 3), ("stone_lion", 2), ("training_yard", 1),
        };

        /// <summary>A model by its kind and variant (null if there is no such kind).</summary>
        public static MeshData Make(string kind, int variant = 0)
        {
            switch (kind)
            {
                case "paifang": return Paifang(variant == 0 ? 3 : 1);
                case "wall": return Wall(8);
                case "stall": return Stall(variant);
                case "cart": return Cart(variant);
                case "boat": return Boat(variant);
                case "arch_bridge": return ArchBridge(variant == 0 ? 14 : 22, variant == 0 ? 4 : 6);
                case "lanterns": return Lanterns(variant == 0 ? 8 : variant == 1 ? 12 : 16, variant + 5);
                case "pagoda": return Pagoda(variant == 0 ? 5 : 7);
                case "drum_tower": return DrumTower();
                case "aperture": return Aperture();
                case "dummy_post": return DummyPost();
                case "gu_altar": return GuAltar();
                case "town_wall": return TownWall(10);
                case "town_gate": return TownGate();
                case "corner_tower": return CornerTower();
                case "banner": return Banner(variant);
                case "stone_lion": return StoneLion(variant == 1);
                case "training_yard": return TrainingYard();
                default: return null;
            }
        }

        /// <summary>The ground a kind takes (width along x, depth along z), for the planner to keep it clear.</summary>
        public static (float width, float depth) Footprint(string kind, int variant = 0)
        {
            switch (kind)
            {
                case "paifang": return variant == 0 ? (13f, 2.6f) : (6f, 2f);
                case "wall": return (8f, 0.8f);
                case "stall": return (3.4f, 2.6f);
                case "cart": return (1.8f, 3.6f);
                case "boat": return (2.2f, 7.5f);
                case "arch_bridge": return variant == 0 ? (4.6f, 18f) : (6.6f, 28f);
                case "lanterns": return (0.6f, variant == 0 ? 8f : variant == 1 ? 12f : 16f);
                case "pagoda": return variant == 0 ? (9f, 9f) : (12f, 12f);
                case "drum_tower": return (9f, 9f);
                case "aperture": return (64f, 64f);
                case "dummy_post": return (1f, 1f);
                case "gu_altar": return (4f, 4f);
                case "town_wall": return (10f, 3.2f);
                case "town_gate": return (18f, 10f);
                case "corner_tower": return (7f, 7f);
                case "banner": return (1.6f, 0.6f);
                case "stone_lion": return (1.4f, 2.2f);
                case "training_yard": return (16f, 12f);
                default: return (1f, 1f);
            }
        }

        /// <summary>How tall a kind stands (for the planner and the far view).</summary>
        public static float Height(string kind, int variant = 0)
        {
            switch (kind)
            {
                case "paifang": return variant == 0 ? 8.4f : 6.8f;
                case "wall": return 3.2f;
                case "stall": return 2.9f;
                case "cart": return 1.4f;
                case "boat": return 4.2f;
                case "arch_bridge": return variant == 0 ? 6f : 8.5f;
                case "lanterns": return 4.6f;
                case "pagoda": return variant == 0 ? 23f : 30f;
                case "drum_tower": return 12.5f;
                case "aperture": return 9.5f;
                case "dummy_post": return 2.2f;
                case "gu_altar": return 1.8f;
                case "town_wall": return 6.6f;
                case "town_gate": return 15f;
                case "corner_tower": return 13f;
                case "banner": return 7.5f;
                case "stone_lion": return 2.6f;
                case "training_yard": return 2.6f;
                default: return 2f;
            }
        }

        // ------------------------------------------------------------------ Memorial archway

        /// <summary>
        /// A paifang: four (or two) lacquered columns on stone drums, a beam and a gilded plaque across,
        /// and over each bay its own little curved roof, the middle one higher. `bays` 3 or 1.
        /// </summary>
        public static MeshData Paifang(int bays)
        {
            var k = new Kit();
            var bay = bays == 3 ? new[] { 3.4f, 4.8f, 3.4f } : new[] { 4.6f };
            var total = 0f;

            foreach (var b in bay)
            {
                total += b;
            }

            var x = -total / 2;
            var columns = new List<float> { x };

            foreach (var b in bay)
            {
                x += b;
                columns.Add(x);
            }

            var height = bays == 3 ? 7.2f : 5.6f;

            foreach (var c in columns)
            {
                // A drum of stone at the foot, held by two clasping stones front and back.
                k.Drum(c, 0, 0, 0.55f, 0.5f, 10, Stone);
                k.Box(c, 0, 0.6f, 0.5f, 0.9f, 0.5f, StoneDark);
                k.Box(c, 0, -0.6f, 0.5f, 0.9f, 0.5f, StoneDark);
                k.Post(c, 0.5f, 0, 0.27f, 0.24f, height - 0.5f, 10, Lacquer);
            }

            for (var i = 0; i < bay.Length; i++)
            {
                var left = columns[i];
                var right = columns[i + 1];
                var middle = (left + right) / 2;
                var main = bays == 1 || i == 1;
                var top = main ? height : height - 1.1f;

                // Two beams, the plaque between them, brackets above.
                k.Beam(new V3(left - 0.2f, top - 1.9f, 0), new V3(right + 0.2f, top - 1.9f, 0), 0.32f, 0.36f, LacquerDark);
                k.Beam(new V3(left - 0.3f, top - 0.6f, 0), new V3(right + 0.3f, top - 0.6f, 0), 0.38f, 0.42f, Lacquer);

                if (main)
                {
                    k.Box(middle, top - 1.65f, 0, (right - left) * 0.55f, 0.85f, 0.12f, LacquerDark);
                    k.Box(middle, top - 1.58f, 0.07f, (right - left) * 0.48f, 0.7f, 0.04f, Gold);
                }

                for (var b = 0; b < 4; b++)
                {
                    var bx = left + (right - left) * (b + 0.5f) / 4;
                    k.Box(bx, top - 0.38f, 0, 0.3f, 0.25f, 0.55f, Gold.Times(0.8f));
                }

                k.Push(new V3(middle, top - 0.12f, 0));
                k.Roof(right - left + 0.6f, 1.0f, main ? 1.0f : 0.8f, 0.55f, main ? 0.55f : 0.45f, false, TilesGreen, Tiles.Times(0.8f));
                k.Pop();
            }

            return k.Mesh;
        }

        // ------------------------------------------------------------------ Walls

        /// <summary>A courtyard wall `length` long: a stone footing, whitewashed body, a timber band, a tiled coping.</summary>
        public static MeshData Wall(float length)
        {
            var k = new Kit();
            k.Box(0, 0, 0, length, 0.5f, 0.7f, StoneDark);
            k.Box(0, 0.5f, 0, length, 2.1f, 0.5f, Plaster);
            k.Box(0, 2.45f, 0, length, 0.15f, 0.56f, Wood);
            k.Coping(new V3(-length / 2 - 0.15f, 2.6f, 0), new V3(length / 2 + 0.15f, 2.6f, 0), 1.2f, Tiles);

            // A lattice window in the middle, as the halls' walls have.
            if (length >= 6)
            {
                k.Box(0, 1.25f, 0, 1.2f, 0.9f, 0.56f, Wood.Times(0.7f));

                for (var i = -2; i <= 2; i++)
                {
                    k.Box(i * 0.22f, 1.28f, 0, 0.05f, 0.84f, 0.6f, WoodLight);
                    k.Box(0, 1.7f + i * 0.17f - 0.0f, 0, 1.14f, 0.05f, 0.6f, WoodLight);
                }
            }

            return k.Mesh;
        }

        // ------------------------------------------------------------------ Market

        /// <summary>A market stall: a counter of planks, four posts, a sloping cloth awning of its colour, goods on the counter.</summary>
        public static MeshData Stall(int variant)
        {
            var k = new Kit();
            var rng = new Rng(9100 + variant);
            var cloth = Cloths[variant % Cloths.Length];
            const float w = 3.2f, d = 2.2f;

            foreach (var (px, pz, h) in new[] { (-w / 2, d / 2, 2.4f), (w / 2, d / 2, 2.4f), (-w / 2, -d / 2, 2.0f), (w / 2, -d / 2, 2.0f) })
            {
                k.Box(px, 0, pz, 0.12f, h, 0.12f, Wood);
            }

            // The counter at the front and a back shelf.
            k.Box(0, 0, d / 2 - 0.35f, w - 0.1f, 0.9f, 0.6f, WoodLight.Times(0.85f));
            k.Box(0, 0.9f, d / 2 - 0.35f, w + 0.1f, 0.06f, 0.7f, WoodLight);
            k.Box(0, 1.1f, -d / 2 + 0.25f, w - 0.2f, 0.05f, 0.4f, Wood);

            // The awning: a cloth sheet from the high front to the low back, with a scalloped front valance.
            var front = new List<V3>();
            var back = new List<V3>();

            for (var i = 0; i <= 6; i++)
            {
                var x = -w / 2 - 0.25f + (w + 0.5f) * i / 6;
                var sag = 0.08f * (float)Math.Sin(i * Math.PI / 6);
                front.Add(new V3(x, 2.5f - sag, d / 2 + 0.5f));
                back.Add(new V3(x, 2.05f - sag, -d / 2 - 0.2f));
            }

            k.Sheet(back.ToArray(), front.ToArray(), cloth, true);

            for (var i = 0; i < 6; i++)
            {
                var a = front[i];
                var b = front[i + 1];
                var stripe = i % 2 == 0 ? cloth : Plaster;
                k.Quad(a + new V3(0, -0.35f, 0), a, b, b + new V3(0, -0.35f, 0), stripe);
                k.Quad(b + new V3(0, -0.35f, 0), b, a, a + new V3(0, -0.35f, 0), stripe.Times(0.7f));
            }

            // Goods: baskets, jars, bolts of cloth, crates — by the variant.
            for (var i = 0; i < 5; i++)
            {
                var gx = -w / 2 + 0.45f + i * (w - 0.9f) / 4;
                var gz = d / 2 - 0.35f + rng.Range(-0.12f, 0.12f);

                switch ((variant + i) % 4)
                {
                    case 0:
                        k.Post(gx, 0.96f, gz, 0.18f, 0.24f, 0.22f, 8, Tint.Hex(0xB89060));
                        k.Ball(new V3(gx, 1.2f, gz), 0.16f, 0.6f, 8, 4, Tint.Hex(0xC84830));
                        break;
                    case 1:
                        k.Post(gx, 0.96f, gz, 0.14f, 0.1f, 0.34f, 8, Tint.Hex(0x7A5A40));
                        break;
                    case 2:
                        k.Box(gx, 0.96f, gz, 0.4f, 0.16f, 0.5f, Cloths[(variant + i) % Cloths.Length]);
                        k.Box(gx, 1.12f, gz, 0.36f, 0.14f, 0.46f, Cloths[(variant + i + 2) % Cloths.Length]);
                        break;
                    default:
                        k.Box(gx, 0.96f, gz, 0.42f, 0.3f, 0.42f, WoodLight.Times(0.9f));
                        break;
                }
            }

            // A sign on a pole at one side.
            k.Box(w / 2 + 0.25f, 0, d / 2, 0.08f, 2.9f, 0.08f, Wood);
            k.Box(w / 2 + 0.25f, 1.7f, d / 2 + 0.02f, 0.36f, 1.0f, 0.04f, cloth.Times(0.9f));

            return k.Mesh;
        }

        // ------------------------------------------------------------------ Carts and boats

        /// <summary>A hand cart: a plank bed with low rails, two spoked wheels, two shafts, a load by the variant.</summary>
        public static MeshData Cart(int variant)
        {
            var k = new Kit();
            k.Box(0, 0.62f, 0.2f, 1.3f, 0.08f, 2.0f, WoodLight);

            foreach (var side in new[] { -1, 1 })
            {
                k.Box(side * 0.62f, 0.7f, 0.2f, 0.06f, 0.3f, 2.0f, Wood);
                k.Push(new V3(side * 0.78f, 0.55f, 0.1f), 90);
                k.Wheel(new V3(0, 0, 0), 0.55f, 0.1f, 8, Wood.Times(0.8f), WoodLight);
                k.Pop();
                k.Beam(new V3(side * 0.5f, 0.62f, -0.8f), new V3(side * 0.45f, 0.75f, -2.1f), 0.08f, 0.08f, Wood);
            }

            k.Box(0, 0.7f, 1.18f, 1.3f, 0.3f, 0.06f, Wood);
            k.Beam(new V3(-0.46f, 0.75f, -2.0f), new V3(0.46f, 0.75f, -2.0f), 0.06f, 0.06f, Wood);

            switch (variant % 3)
            {
                case 0:
                    // Sacks of rice.
                    for (var i = 0; i < 4; i++)
                    {
                        k.Ball(new V3(-0.3f + 0.6f * (i % 2), 0.88f + 0.18f * (i / 2), -0.25f + 0.55f * (i % 3)), 0.3f, 0.55f, 8, 4, Tint.Hex(0xD8C8A0));
                    }

                    break;
                case 1:
                    // Firewood.
                    for (var i = 0; i < 9; i++)
                    {
                        var x = -0.45f + 0.3f * (i % 4);
                        var y = 0.75f + 0.14f * (i / 4);
                        k.Beam(new V3(x, y, -0.6f), new V3(x + 0.05f, y, 1.0f), 0.13f, 0.13f, Tint.Hex(0x8A6A48));
                    }

                    break;
                default:
                    // Jars of wine.
                    for (var i = 0; i < 4; i++)
                    {
                        var x = -0.3f + 0.6f * (i % 2);
                        var z = -0.2f + 0.7f * (i / 2);
                        k.Ball(new V3(x, 0.98f, z), 0.26f, 1.1f, 8, 5, Tint.Hex(0x6A4A32));
                        k.Post(x, 1.24f, z, 0.08f, 0.08f, 0.1f, 6, Tint.Hex(0xB04A3A));
                    }

                    break;
            }

            return k.Mesh;
        }

        /// <summary>A sampan: a lofted hull (narrow, upturned at the ends), thwarts, a hooped canopy of matting, a pole.</summary>
        public static MeshData Boat(int variant)
        {
            var k = new Kit();
            const int stations = 9;
            const float length = 7f;
            var hull = Tint.Hex(0x6A4E34);
            var gunwaleL = new V3[stations];
            var gunwaleR = new V3[stations];
            var keel = new V3[stations];
            var chineL = new V3[stations];
            var chineR = new V3[stations];

            for (var i = 0; i < stations; i++)
            {
                var t = i / (float)(stations - 1);
                var z = -length / 2 + length * t;
                var u = 2 * t - 1;
                var beam = 1.05f * (1 - u * u * 0.85f);
                var sheer = 0.6f + 0.45f * u * u * u * u;
                var bottom = 0.05f + 0.25f * u * u;
                gunwaleL[i] = new V3(-beam, sheer, z);
                gunwaleR[i] = new V3(beam, sheer, z);
                chineL[i] = new V3(-beam * 0.75f, bottom + 0.12f, z);
                chineR[i] = new V3(beam * 0.75f, bottom + 0.12f, z);
                keel[i] = new V3(0, bottom, z);
            }

            k.Sheet(chineL, gunwaleL, hull, true);
            k.Sheet(gunwaleR, chineR, hull, true);
            k.Sheet(keel, chineL, hull.Times(0.85f), true);
            k.Sheet(chineR, keel, hull.Times(0.85f), true);

            // The stem and the stern closed (both faces, as the hull's sheets are).
            foreach (var i in new[] { 0, stations - 1 })
            {
                foreach (var flip in new[] { false, true })
                {
                    if (flip)
                    {
                        k.Quad(gunwaleR[i], chineR[i], chineL[i], gunwaleL[i], hull.Times(0.8f));
                        k.Triangle(chineR[i], keel[i], chineL[i], hull.Times(0.8f));
                    }
                    else
                    {
                        k.Quad(gunwaleL[i], chineL[i], chineR[i], gunwaleR[i], hull.Times(0.8f));
                        k.Triangle(chineL[i], keel[i], chineR[i], hull.Times(0.8f));
                    }
                }
            }

            for (var i = 1; i < stations - 1; i += 2)
            {
                k.Beam(gunwaleL[i] + new V3(0.05f, -0.05f, 0), gunwaleR[i] + new V3(-0.05f, -0.05f, 0), 0.18f, 0.06f, WoodLight);
            }

            // The canopy over the middle: hoops of bamboo and matting between them.
            if (variant != 2)
            {
                var matting = Tint.Hex(variant == 0 ? 0xA89060 : 0x4A4A44);
                var hoops = 4;
                V3[] previous = null;

                for (var h = 0; h < hoops; h++)
                {
                    var z = -1.2f + 2.2f * h / (hoops - 1);
                    var arc = new V3[7];

                    for (var a = 0; a < 7; a++)
                    {
                        var angle = Math.PI * a / 6;
                        arc[a] = new V3(-(float)Math.Cos(angle) * 0.9f, 0.6f + (float)Math.Sin(angle) * 0.85f, z);
                    }

                    if (previous != null)
                    {
                        k.Sheet(previous, arc, matting, true);
                    }

                    previous = arc;
                }
            }

            k.Beam(new V3(0.5f, 0.7f, 2.2f), new V3(0.9f, 4.2f, 3.6f), 0.06f, 0.06f, Tint.Hex(0xC8B070));

            return k.Mesh;
        }

        // ------------------------------------------------------------------ Bridges

        /// <summary>A humped stone bridge of one arch `span` long and `width` wide: the arch's barrel, the deck rising in steps, parapets with posts.</summary>
        public static MeshData ArchBridge(float span, float width)
        {
            var k = new Kit();
            var rise = span * 0.32f;
            var segments = 14;
            var deck = new V3[segments + 1];
            var under = new V3[segments + 1];

            // The arch's profile along z, the deck over it.
            for (var i = 0; i <= segments; i++)
            {
                var t = i / (float)segments;
                var z = -span / 2 - 2 + (span + 4) * t;
                var u = (z / (span / 2 + 2));
                var top = rise * (1 - u * u) + 0.4f;
                deck[i] = new V3(0, top, z);
                var a = Math.Abs(z) < span / 2 ? (float)Math.Sqrt(1 - (z / (span / 2)) * (z / (span / 2))) : 0;
                under[i] = new V3(0, Math.Max(-0.4f, rise * 0.85f * a - 0.4f), z);
            }

            for (var i = 0; i < segments; i++)
            {
                var a = deck[i];
                var b = deck[i + 1];
                var ua = under[i];
                var ub = under[i + 1];
                var half = new V3(width / 2, 0, 0);
                // The deck (stone slabs, every other a shade).
                var slab = i % 2 == 0 ? Stone : Stone.Times(0.94f);
                k.Quad(b - half, b + half, a + half, a - half, slab);
                // The arch's soffit (seen from under) and the two faces of the spandrel.
                k.Quad(ua - half, ua + half, ub + half, ub - half, StoneDark);
                k.Quad(b + half, ub + half, ua + half, a + half, StoneDark.Times(1.1f));
                k.Quad(a - half, ua - half, ub - half, b - half, StoneDark.Times(1.1f));

                // The parapets.
                foreach (var side in new[] { -1, 1 })
                {
                    var off = new V3(side * (width / 2 - 0.15f), 0, 0);
                    var rail = new V3(0, 0.75f, 0);
                    var thick = new V3(side * 0.15f, 0, 0);

                    // The parapet's inner and outer faces (which way each looks depends on the side, so the inner face is turned by it).
                    var inner0 = a + off - thick;
                    var inner1 = b + off - thick;
                    var outer0 = a + off + thick;
                    var outer1 = b + off + thick;

                    if (side > 0)
                    {
                        k.Quad(inner1, inner1 + rail, inner0 + rail, inner0, Stone);
                        k.Quad(outer0, outer0 + rail, outer1 + rail, outer1, Stone.Times(1.05f));
                    }
                    else
                    {
                        k.Quad(inner0, inner0 + rail, inner1 + rail, inner1, Stone);
                        k.Quad(outer1, outer1 + rail, outer0 + rail, outer0, Stone.Times(1.05f));
                    }
                    if (side > 0)
                    {
                        k.Quad(b + off - thick + rail, b + off + thick + rail, a + off + thick + rail, a + off - thick + rail, Stone.Times(1.1f));
                    }
                    else
                    {
                        k.Quad(a + off - thick + rail, a + off + thick + rail, b + off + thick + rail, b + off - thick + rail, Stone.Times(1.1f));
                    }

                    if (i % 2 == 0)
                    {
                        k.Box(a.X + off.X, a.Y, a.Z, 0.36f, 1.05f, 0.36f, Stone.Times(1.08f));
                        k.Ball(new V3(a.X + off.X, a.Y + 1.15f, a.Z), 0.14f, 1, 6, 3, Stone.Times(1.1f));
                    }
                }
            }

            // The abutments where the deck meets the banks.
            foreach (var end in new[] { deck[0], deck[segments] })
            {
                k.Box(0, -0.6f, end.Z, width + 0.1f, end.Y + 0.6f, 0.8f, StoneDark);
            }

            // The arch's ring of voussoirs on each face.
            for (var i = 0; i < 12; i++)
            {
                var a0 = Math.PI * i / 12;
                var a1 = Math.PI * (i + 1) / 12;

                foreach (var side in new[] { -1, 1 })
                {
                    var x = side * (width / 2 + 0.02f);
                    var p0 = new V3(x, rise * 0.85f * (float)Math.Sin(a0) - 0.4f, -(float)Math.Cos(a0) * span / 2);
                    var p1 = new V3(x, rise * 0.85f * (float)Math.Sin(a1) - 0.4f, -(float)Math.Cos(a1) * span / 2);
                    k.Beam(p0, p1, 0.08f, 0.5f, i % 2 == 0 ? Stone.Times(1.1f) : Stone);
                }
            }

            return k.Mesh;
        }

        // ------------------------------------------------------------------ Lanterns

        /// <summary>Where the lanterns of a string of a variant hang (their bodies' middles), for the light they give at night.</summary>
        public static List<V3> LanternPoints(int variant)
        {
            var span = variant == 0 ? 8f : variant == 1 ? 12f : 16f;
            var count = variant + 5;
            var sag = span * 0.08f;
            var points = new List<V3>();

            for (var i = 0; i < count; i++)
            {
                var t = (i + 0.5f) / count;
                points.Add(new V3(0, 4.6f - 0.15f - sag * 4 * t * (1 - t) - 0.58f, -span / 2 + span * t));
            }

            return points;
        }

        /// <summary>A string of red paper lanterns across a lane `span` wide: two poles, a sagging rope, the lanterns hanging from it.</summary>
        public static MeshData Lanterns(float span, int count)
        {
            var k = new Kit();
            const float height = 4.6f;
            k.Post(0, 0, -span / 2, 0.08f, 0.07f, height, 6, Wood);
            k.Post(0, 0, span / 2, 0.08f, 0.07f, height, 6, Wood);
            var sag = span * 0.08f;
            var steps = count * 2;
            V3 At(float t) => new V3(0, height - 0.15f - sag * 4 * t * (1 - t), -span / 2 + span * t);

            for (var i = 0; i < steps; i++)
            {
                k.Beam(At(i / (float)steps), At((i + 1) / (float)steps), 0.025f, 0.025f, Rope);
            }

            for (var i = 0; i < count; i++)
            {
                var t = (i + 0.5f) / count;
                var p = At(t);
                k.Beam(p, p + new V3(0, -0.25f, 0), 0.02f, 0.02f, Rope);
                k.Ball(p + new V3(0, -0.58f, 0), 0.26f, 1.2f, 10, 6, i % 5 == 2 ? Gold : Paper);
                k.Drum(p.X, p.Y - 0.92f, p.Z, 0.12f, 0.06f, 8, Gold.Times(0.8f));
                k.Drum(p.X, p.Y - 0.3f, p.Z, 0.12f, 0.05f, 8, Gold.Times(0.8f));
                k.Beam(p + new V3(0, -0.92f, 0), p + new V3(0, -1.15f, 0), 0.04f, 0.04f, Paper.Times(0.8f));
            }

            return k.Mesh;
        }

        // ------------------------------------------------------------------ Towers

        /// <summary>A pagoda of `tiers` storeys: a stone base, octagonal bodies narrowing upward, each under its own lifted eaves, a spire.</summary>
        public static MeshData Pagoda(int tiers)
        {
            var k = new Kit();
            k.Drum(0, 0, 0, 4.4f, 0.9f, 8, StoneDark);
            k.Drum(0, 0.9f, 0, 3.9f, 0.3f, 8, Stone);
            var y = 1.2f;
            var radius = 3.0f;

            for (var t = 0; t < tiers; t++)
            {
                var h = t == 0 ? 3.4f : 2.5f - t * 0.08f;
                k.Post(0, y, 0, radius, radius * 0.97f, h, 8, t % 2 == 0 ? Plaster : Plaster.Times(0.95f), false);

                // Columns at the corners and a door on each face of the first storey.
                for (var c = 0; c < 8; c++)
                {
                    var a = (c + 0.5) * Math.PI / 4;
                    k.Post((float)Math.Sin(a) * radius * 1.02f, y, (float)Math.Cos(a) * radius * 1.02f, 0.14f, 0.14f, h, 6, Lacquer, false);
                }

                if (t == 0)
                {
                    for (var f = 0; f < 4; f++)
                    {
                        k.Push(new V3(0, y, 0), f * 90);
                        k.Box(0, 0, radius * 0.93f, 1.2f, 2.2f, 0.2f, LacquerDark);
                        k.Pop();
                    }
                }

                y += h;
                // Eaves: an octagonal skirt of tiles lifting at its eight corners.
                var eave = radius + 1.2f;
                var inner = radius * 0.85f;
                var tierRise = 0.9f;

                for (var c = 0; c < 8; c++)
                {
                    var a0 = c * Math.PI / 4;
                    var a1 = (c + 1) * Math.PI / 4;
                    var lift0 = 0.45f;
                    var o0 = new V3((float)Math.Sin(a0) * eave, y + lift0, (float)Math.Cos(a0) * eave);
                    var o1 = new V3((float)Math.Sin(a1) * eave, y + lift0, (float)Math.Cos(a1) * eave);
                    var om = new V3((float)Math.Sin((a0 + a1) / 2) * eave * 0.96f, y + 0.05f, (float)Math.Cos((a0 + a1) / 2) * eave * 0.96f);
                    var i0 = new V3((float)Math.Sin(a0) * inner, y + tierRise, (float)Math.Cos(a0) * inner);
                    var i1 = new V3((float)Math.Sin(a1) * inner, y + tierRise, (float)Math.Cos(a1) * inner);
                    k.Triangle(i0, o0, om, Tiles);
                    k.Triangle(i0, om, i1, Tiles.Times(0.95f));
                    k.Triangle(i1, om, o1, Tiles);
                    k.Triangle(om, o0, i0, Tiles.Times(0.55f));
                    k.Triangle(i1, om, i0, Tiles.Times(0.5f));
                    k.Triangle(o1, om, i1, Tiles.Times(0.55f));
                    k.Ball(o0 + new V3(0, 0.1f, 0), 0.12f, 1, 6, 3, Gold);
                }

                y += tierRise;
                radius *= 0.86f;
            }

            k.Post(0, y, 0, 0.5f, 0.05f, 3.2f, 8, Gold.Times(0.85f));

            for (var r = 0; r < 4; r++)
            {
                k.Ring(0, y + 0.6f + r * 0.55f, 0, 0, 0.42f - r * 0.07f, 10, Gold);
            }

            return k.Mesh;
        }

        /// <summary>A drum tower: a stone platform with a gate tunnel, a lacquered pavilion on it holding the great drum, a hipped roof.</summary>
        public static MeshData DrumTower()
        {
            var k = new Kit();
            // The platform with its tunnel (two blocks and a lintel).
            k.Box(-3.0f, 0, 0, 3.0f, 5.0f, 8.4f, StoneDark);
            k.Box(3.0f, 0, 0, 3.0f, 5.0f, 8.4f, StoneDark);
            k.Box(0, 3.4f, 0, 3.0f, 1.6f, 8.4f, StoneDark.Times(0.95f));
            k.Box(0, 5.0f, 0, 9.4f, 0.4f, 8.8f, Stone);

            foreach (var x in new[] { -4.4f, -1.5f, 1.5f, 4.4f })
            {
                foreach (var z in new[] { -3.6f, 3.6f })
                {
                    k.Post(x, 5.4f, z, 0.22f, 0.2f, 4.0f, 8, Lacquer);
                }
            }

            k.Box(0, 5.4f, 3.7f, 9.0f, 0.9f, 0.1f, LacquerDark);
            k.Box(0, 5.4f, -3.7f, 9.0f, 0.9f, 0.1f, LacquerDark);
            // The drum on its stand.
            k.Push(new V3(0, 7.0f, 0), 90);
            k.Wheel(new V3(0, 0, 0), 1.2f, 1.2f, 0, Lacquer, Gold);
            k.Pop();
            k.Box(0, 5.4f, 0, 0.3f, 0.6f, 1.6f, Wood);
            k.Push(new V3(0, 9.4f, 0));
            k.Roof(9.6f, 7.8f, 2.4f, 1.1f, 0.7f, true, Tiles, Tiles.Times(0.75f));
            k.Pop();

            return k.Mesh;
        }

        // ------------------------------------------------------------------ The clan's signs

        /// <summary>A clan banner: a lacquered pole on a stone base, a long cloth hanging from a crossbar (moon blue, red, or gold), a moon disc on it.</summary>
        public static MeshData Banner(int variant)
        {
            var k = new Kit();
            var cloth = variant == 0 ? Tint.Hex(0x2E4E8A) : variant == 1 ? Tint.Hex(0x9A2A22) : Tint.Hex(0xC8A040);
            k.Drum(0, 0, 0, 0.5f, 0.35f, 8, StoneDark);
            k.Post(0, 0.35f, 0, 0.09f, 0.07f, 7f, 8, Lacquer);
            k.Ball(new V3(0, 7.45f, 0), 0.14f, 1.4f, 8, 4, Gold);
            k.Beam(new V3(0, 6.8f, -0.05f), new V3(1.4f, 6.8f, -0.05f), 0.06f, 0.06f, Lacquer);

            // The cloth: a few rows that sway out a little toward the bottom (the shader sways what is high; the cut gives it a wind).
            var rows = 8;
            var top = new V3[3];
            var previous = new[] { new V3(0.1f, 6.75f, 0), new V3(0.75f, 6.75f, 0), new V3(1.35f, 6.75f, 0) };

            for (var r = 1; r <= rows; r++)
            {
                var y = 6.75f - r * 0.48f;
                var bow = 0.12f * (float)Math.Sin(r * 0.7f);
                var row = new[] { new V3(0.1f, y, bow), new V3(0.75f, y, bow * 1.4f), new V3(1.35f - 0.02f * r, y, bow) };
                k.Sheet(previous, row, cloth.Times(r % 2 == 0 ? 1 : 0.94f), true);
                previous = row;
            }

            // The tails.
            k.Triangle(previous[0], previous[1], previous[1] + new V3(-0.3f, -0.6f, 0), cloth.Times(0.9f));
            k.Triangle(previous[1] + new V3(-0.3f, -0.6f, 0), previous[1], previous[0], cloth.Times(0.8f));
            // The moon on the cloth (both faces).
            k.Push(new V3(0.72f, 5.3f, 0.24f));
            Disc(k, 0.36f, Tint.Hex(0xE8EEF2));
            k.Pop();
            k.Push(new V3(0.72f, 5.3f, 0.0f), 180);
            Disc(k, 0.36f, Tint.Hex(0xE8EEF2));
            k.Pop();

            return k.Mesh;
        }

        /// <summary>A disc standing up, facing +z.</summary>
        static void Disc(Kit k, float radius, Tint tint)
        {
            const int sides = 16;

            for (var i = 0; i < sides; i++)
            {
                var a0 = i * 2 * Math.PI / sides;
                var a1 = (i + 1) * 2 * Math.PI / sides;
                k.Triangle(new V3(0, 0, 0), new V3((float)Math.Sin(a1) * radius, (float)Math.Cos(a1) * radius, 0), new V3((float)Math.Sin(a0) * radius, (float)Math.Cos(a0) * radius, 0), tint);
            }
        }

        /// <summary>
        /// A guardian stone lion on its plinth, sitting up, facing +z: a body and haunches of blocks, a
        /// maned head with a ball of curls, a paw on a ball (the male) or on a cub (the female).
        /// </summary>
        public static MeshData StoneLion(bool female)
        {
            var k = new Kit();
            var stone = Tint.Hex(0x9A968C);
            var dark = stone.Times(0.8f);
            k.Box(0, 0, 0, 1.3f, 0.6f, 2.0f, StoneDark);
            k.Box(0, 0.6f, 0, 1.1f, 0.12f, 1.8f, Stone);
            // Haunches and body, the chest raised.
            k.Box(0, 0.72f, -0.45f, 0.95f, 0.55f, 0.8f, stone);
            k.Beam(new V3(0, 0.95f, -0.3f), new V3(0, 1.55f, 0.25f), 0.7f, 0.6f, stone);
            // Forelegs.
            k.Box(-0.28f, 0.72f, 0.45f, 0.22f, 0.75f, 0.25f, stone);
            k.Box(0.28f, 0.72f, 0.45f, 0.22f, 0.75f, 0.25f, stone);
            // The mane: a ring of curls; the head; the face.
            k.Ball(new V3(0, 1.75f, 0.35f), 0.48f, 1, 10, 6, dark);

            for (var c = 0; c < 9; c++)
            {
                var a = c * Math.PI * 2 / 9;
                k.Ball(new V3((float)Math.Sin(a) * 0.42f, 1.75f + (float)Math.Cos(a) * 0.42f, 0.3f), 0.12f, 1, 6, 3, dark.Times(0.9f));
            }

            k.Box(0, 1.55f, 0.72f, 0.42f, 0.35f, 0.25f, stone);
            k.Ball(new V3(-0.14f, 1.9f, 0.76f), 0.06f, 1, 6, 3, StoneDark);
            k.Ball(new V3(0.14f, 1.9f, 0.76f), 0.06f, 1, 6, 3, StoneDark);
            k.Box(0, 1.5f, 0.86f, 0.3f, 0.08f, 0.05f, StoneDark);

            // Under its paw: an embroidered ball, or a cub.
            if (female)
            {
                k.Ball(new V3(0.45f, 0.95f, 0.55f), 0.2f, 1, 8, 4, stone);
                k.Ball(new V3(0.45f, 1.18f, 0.62f), 0.13f, 1, 8, 4, dark);
            }
            else
            {
                k.Ball(new V3(-0.45f, 0.92f, 0.55f), 0.2f, 1, 10, 5, dark);
            }

            return k.Mesh;
        }

        /// <summary>The academy's training yard: sand behind a low bamboo fence, posts to strike, racks, a drum, a bench for the teacher.</summary>
        public static MeshData TrainingYard()
        {
            var k = new Kit();
            const float w = 8, d = 6;
            k.Tiled(0, 0, 0, w * 2, 0.06f, d * 2, 4, Tint.Hex(0xD8C8A0));

            // The fence: posts and two rails, open at the front.
            for (var x = -w; x <= w + 0.01f; x += 2)
            {
                k.Post(x, 0, d, 0.06f, 0.06f, 1.0f, 6, Tint.Hex(0x8AA060));
                k.Post(x, 0, -d, 0.06f, 0.06f, 1.0f, 6, Tint.Hex(0x8AA060));
            }

            for (var z = -d; z <= d + 0.01f; z += 2)
            {
                k.Post(-w, 0, z, 0.06f, 0.06f, 1.0f, 6, Tint.Hex(0x8AA060));
                k.Post(w, 0, z, 0.06f, 0.06f, 1.0f, 6, Tint.Hex(0x8AA060));
            }

            foreach (var y in new[] { 0.45f, 0.85f })
            {
                k.Beam(new V3(-w, y, d), new V3(w, y, d), 0.05f, 0.05f, Tint.Hex(0x9AB070));
                k.Beam(new V3(-w, y, -d), new V3(-2, y, -d), 0.05f, 0.05f, Tint.Hex(0x9AB070));
                k.Beam(new V3(2, y, -d), new V3(w, y, -d), 0.05f, 0.05f, Tint.Hex(0x9AB070));
                k.Beam(new V3(-w, y, -d), new V3(-w, y, d), 0.05f, 0.05f, Tint.Hex(0x9AB070));
                k.Beam(new V3(w, y, -d), new V3(w, y, d), 0.05f, 0.05f, Tint.Hex(0x9AB070));
            }

            // Posts to strike, in a row.
            for (var i = 0; i < 4; i++)
            {
                k.Push(new V3(-4.5f + i * 3f, 0, 2.5f));
                var post = DummyPost();
                Append(k, post);
                k.Pop();
            }

            // A drum on its stand and the teacher's bench.
            k.Push(new V3(6.5f, 0.9f, -4f), 90);
            k.Wheel(new V3(0, 0, 0), 0.6f, 0.5f, 0, Lacquer, Gold);
            k.Pop();
            k.Box(6.5f, 0, -4f, 0.2f, 0.4f, 0.8f, Wood);
            k.Box(-6f, 0, -4.5f, 1.8f, 0.45f, 0.5f, Wood);

            return k.Mesh;
        }

        /// <summary>Copies another mesh into the kit at its present frame.</summary>
        static void Append(Kit k, MeshData other)
        {
            for (var t = 0; t < other.Triangles.Count; t += 3)
            {
                V3 P(int i) => new V3(other.Positions[i * 3], other.Positions[i * 3 + 1], other.Positions[i * 3 + 2]);
                var a = other.Triangles[t];
                k.Triangle(P(a), P(other.Triangles[t + 1]), P(other.Triangles[t + 2]), new Tint(other.Colours[a * 4], other.Colours[a * 4 + 1], other.Colours[a * 4 + 2]));
            }
        }

        // ------------------------------------------------------------------ Rooms

        /// <summary>
        /// A room's shell seen from inside: a floor of planks (flagstones in a hall), walls of plaster
        /// framed in timber (lacquer in a hall), lattice windows glowing with the day, pillars along the
        /// walls, beams across, a ceiling, and a door in the middle of the −z wall. Its origin is the
        /// floor's middle.
        /// </summary>
        public static MeshData Room(float width, float depth, float height, string style)
        {
            var k = new Kit();
            var hall = style == "hall";
            var floor = hall ? Stone.Times(0.85f) : WoodLight.Times(0.85f);
            var frame = hall ? Lacquer : Wood;
            var wall = style == "nursery" ? Tint.Hex(0xD8E4D8) : Plaster.Times(0.93f);
            var w = width / 2;
            var d = depth / 2;

            // The floor in boards (or slabs), each a shade from the next.
            var boards = Math.Max(4, (int)(width / (hall ? 2f : 0.8f)));

            for (var i = 0; i < boards; i++)
            {
                var x = -w + (i + 0.5f) * width / boards;
                k.Tiled(x, -0.3f, 0, width / boards - 0.02f, 0.3f, depth, 2.5f, floor.Times(0.92f + 0.08f * (i % 3) / 2));
            }

            // The walls: thin boxes (seen from both sides), the front one with a door.
            const float thick = 0.3f;
            k.Tiled(0, 0, d + thick / 2, width + thick * 2, height, thick, 2.5f, wall);
            k.Tiled(-w - thick / 2, 0, 0, thick, height, depth, 2.5f, wall);
            k.Tiled(w + thick / 2, 0, 0, thick, height, depth, 2.5f, wall);
            var door = 1.6f;
            k.Box(-(w + door) / 2 - thick / 2, 0, -d - thick / 2, w - door + thick, height, thick, wall);
            k.Box((w + door) / 2 + thick / 2, 0, -d - thick / 2, w - door + thick, height, thick, wall);
            k.Box(0, 2.6f, -d - thick / 2, door * 2, height - 2.6f, thick, wall);
            // The door's frame and its two leaves, shut (the way out is by the step before them).
            k.Box(-door, 0, -d, 0.2f, 2.7f, 0.4f, frame);
            k.Box(door, 0, -d, 0.2f, 2.7f, 0.4f, frame);
            k.Box(0, 2.6f, -d, door * 2 + 0.4f, 0.25f, 0.4f, frame);
            k.Box(-door / 2, 0, -d - 0.05f, door - 0.05f, 2.55f, 0.1f, frame.Times(0.7f));
            k.Box(door / 2, 0, -d - 0.05f, door - 0.05f, 2.55f, 0.1f, frame.Times(0.7f));

            // Timber framing on the walls: a skirting, a rail, posts every few metres.
            k.Box(0, 0, d - 0.02f, width, 0.35f, 0.12f, frame);
            k.Box(0, height - 0.5f, d - 0.02f, width, 0.3f, 0.12f, frame);
            k.Box(-w + 0.02f, 0, 0, 0.12f, 0.35f, depth, frame);
            k.Box(w - 0.02f, 0, 0, 0.12f, 0.35f, depth, frame);
            var bays = Math.Max(2, (int)(depth / 4));

            for (var i = 0; i <= bays; i++)
            {
                var z = -d + i * depth / bays;

                foreach (var side in new[] { -1, 1 })
                {
                    var x = side * (w - 0.25f);
                    k.Post(x, 0, z, hall ? 0.32f : 0.16f, hall ? 0.3f : 0.15f, height, 8, frame, false);

                    // Lattice windows between the posts, lit by the day outside.
                    if (i < bays && (i % 2 == 1 || !hall))
                    {
                        var mid = z + depth / bays / 2;
                        var light = Tint.Hex(0xF4E8C8);
                        k.Box(side * (w - 0.02f), 1.4f, mid, 0.06f, 1.6f, 1.8f, light);

                        for (var b = -2; b <= 2; b++)
                        {
                            k.Box(side * (w - 0.08f), 1.4f, mid + b * 0.36f, 0.06f, 1.6f, 0.05f, frame.Times(0.8f));
                            k.Box(side * (w - 0.08f), 2.2f + b * 0.32f, mid, 0.06f, 0.05f, 1.8f, frame.Times(0.8f));
                        }
                    }
                }

                // A beam across at each bay.
                k.Beam(new V3(-w, height - 0.3f, z), new V3(w, height - 0.3f, z), 0.28f, 0.34f, frame.Times(0.85f));
            }

            // The ceiling: boards, and in a hall a raised middle with a painted panel.
            k.Tiled(0, height, 0, width + thick * 2, 0.2f, depth + thick * 2, 3f, Wood.Times(0.6f));

            if (hall)
            {
                k.Box(0, height - 0.1f, 0, width * 0.5f, 0.1f, depth * 0.5f, Tint.Hex(0x2E4E5A));
                k.Ring(0, height - 0.16f, 0, width * 0.12f, width * 0.15f, 24, Gold);
            }

            return k.Mesh;
        }

        // ------------------------------------------------------------------ The town's wall

        /// <summary>
        /// A stretch of the town's wall `length` long: a battered stone footing, a body of rammed earth faced
        /// with plaster, a walk on top behind crenellations on the outer side (the wall faces −z outward),
        /// a tiled coping on the inner parapet. Long enough pieces are set end to end round the town.
        /// </summary>
        public static MeshData TownWall(float length)
        {
            var k = new Kit();
            var half = length / 2;
            // The footing, a little wider, and the body.
            // (The footing reaches deep, so a piece on a slope never shows its underside.)
            k.Box(0, -4.0f, 0, length + 0.05f, 5.0f, 3.2f, StoneDark);
            k.Box(0, 1.0f, 0, length, 4.0f, 2.6f, Tint.Hex(0xC8B898));
            k.Box(0, 1.0f, -1.31f, length, 0.25f, 0.02f, StoneDark);
            // The walk.
            k.Box(0, 5.0f, 0, length, 0.12f, 2.6f, Stone);
            // The battlements outside: merlons with gaps.
            var merlons = Math.Max(2, (int)(length / 2.2f));

            for (var i = 0; i < merlons; i++)
            {
                var x = -half + (i + 0.5f) * length / merlons;
                k.Box(x, 5.12f, -1.1f, length / merlons * 0.6f, 1.1f, 0.4f, Stone.Times(0.95f));
                k.Box(x, 6.22f, -1.1f, length / merlons * 0.66f, 0.12f, 0.5f, StoneDark);
            }

            // The low parapet inside, under its tiles.
            k.Box(0, 5.12f, 1.12f, length, 0.6f, 0.36f, Plaster);
            k.Coping(new V3(-half, 5.72f, 1.12f), new V3(half, 5.72f, 1.12f), 0.8f, Tiles);

            return k.Mesh;
        }

        /// <summary>The town's gate: two stone bastions with a tunnel between, red doors, and a two-tiered gate tower over them.</summary>
        public static MeshData TownGate()
        {
            var k = new Kit();
            k.Box(-6.0f, -1.2f, 0, 6.0f, 7.4f, 10f, StoneDark);
            k.Box(6.0f, -1.2f, 0, 6.0f, 7.4f, 10f, StoneDark);
            k.Box(0, 4.6f, 0, 6.0f, 1.6f, 10f, StoneDark.Times(0.95f));
            // The arch of the tunnel as a frame, the doors swung open against its sides.
            k.Box(-2.9f, 0, -2.0f, 0.3f, 4.6f, 3.2f, LacquerDark);
            k.Box(2.9f, 0, -2.0f, 0.3f, 4.6f, 3.2f, LacquerDark);

            for (var r = 0; r < 4; r++)
            {
                k.Ball(new V3(-2.72f, 1.0f + r * 1.0f, -2.0f), 0.08f, 1, 6, 3, Gold);
                k.Ball(new V3(2.72f, 1.0f + r * 1.0f, -2.0f), 0.08f, 1, 6, 3, Gold);
            }

            k.Box(0, 6.2f, 0, 18.4f, 0.3f, 10.4f, Stone);
            // The tower: a lacquered hall, its gallery, two roofs.
            foreach (var x in new[] { -7.2f, -2.4f, 2.4f, 7.2f })
            {
                foreach (var z in new[] { -3.6f, 3.6f })
                {
                    k.Post(x, 6.5f, z, 0.3f, 0.26f, 3.6f, 8, Lacquer);
                }
            }

            k.Box(0, 6.5f, 0, 14.0f, 3.0f, 6.4f, Plaster.Times(0.95f));
            k.Box(0, 7.0f, -3.25f, 4f, 1.3f, 0.1f, LacquerDark);
            k.Box(0, 7.1f, -3.31f, 3.4f, 1.0f, 0.04f, Gold);
            k.Push(new V3(0, 10.1f, 0));
            k.Roof(15.6f, 8.4f, 1.6f, 1.2f, 0.55f, true, Tiles, Tiles.Times(0.75f));
            k.Pop();
            k.Box(0, 11.5f, 0, 9.0f, 1.6f, 4.4f, Plaster);
            k.Push(new V3(0, 13.0f, 0));
            k.Roof(10.0f, 5.0f, 1.9f, 1.0f, 0.65f, true, Tiles, Tiles.Times(0.75f));
            k.Pop();

            return k.Mesh;
        }

        /// <summary>A corner tower of the wall: a square stone tower a storey above the walk, battlements, a small hipped roof on posts.</summary>
        public static MeshData CornerTower()
        {
            var k = new Kit();
            k.Box(0, -4.0f, 0, 7.0f, 12.0f, 7.0f, StoneDark);
            k.Box(0, 8.0f, 0, 7.2f, 0.14f, 7.2f, Stone);

            for (var side = 0; side < 4; side++)
            {
                k.Push(new V3(0, 0, 0), side * 90);

                for (var i = 0; i < 3; i++)
                {
                    k.Box(-2.4f + i * 2.4f, 8.14f, -3.3f, 1.2f, 1.0f, 0.4f, Stone.Times(0.95f));
                }

                k.Pop();
            }

            foreach (var (x, z) in new[] { (-2.6f, -2.6f), (2.6f, -2.6f), (-2.6f, 2.6f), (2.6f, 2.6f) })
            {
                k.Post(x, 8.14f, z, 0.18f, 0.16f, 2.6f, 6, Lacquer);
            }

            k.Push(new V3(0, 10.7f, 0));
            k.Roof(6.0f, 6.0f, 1.8f, 0.9f, 0.6f, true, Tiles, Tiles.Times(0.75f));
            k.Pop();

            return k.Mesh;
        }

        // ------------------------------------------------------------------ The immortal aperture

        /// <summary>
        /// The jade terrace of the immortal aperture: a great disc in three steps, a ring of jade pillars
        /// with gold caps, a balustrade at the edge with four openings, a moon of white jade inlaid at the
        /// centre. 64 m across.
        /// </summary>
        public static MeshData Aperture()
        {
            var k = new Kit();
            k.Drum(0, -6, 0, 32, 4.2f, 48, JadeDeep);
            k.Post(0, -14, 0, 4, 31, 8, 48, JadeDeep.Times(0.7f), false);
            k.Drum(0, -1.8f, 0, 30, 1.2f, 48, Jade.Times(0.9f));
            k.Drum(0, -0.6f, 0, 28.5f, 0.6f, 48, Jade);
            k.Ring(0, 0.01f, 0, 6, 6.6f, 48, Gold);
            k.Ring(0, 0.01f, 0, 0, 5.8f, 48, Plaster);
            k.Ring(0, 0.01f, 0, 13, 13.4f, 48, JadeDeep);
            k.Ring(0, 0.01f, 0, 21, 21.3f, 48, JadeDeep);

            // Twelve pillars, a pillar for each of the twelve hours.
            for (var i = 0; i < 12; i++)
            {
                var a = i * Math.PI / 6;
                var x = (float)Math.Sin(a) * 24;
                var z = (float)Math.Cos(a) * 24;
                k.Drum(x, 0, z, 1.0f, 0.5f, 10, JadeDeep);
                k.Post(x, 0.5f, z, 0.6f, 0.5f, 7.5f, 10, Jade.Times(1.05f));
                k.Drum(x, 8.0f, z, 0.85f, 0.4f, 10, Gold);
                k.Ball(new V3(x, 8.8f, z), 0.5f, 1, 10, 5, Plaster);
            }

            // The balustrade, broken by four stairs (north, east, south, west).
            const int posts = 96;

            for (var i = 0; i < posts; i++)
            {
                var a0 = i * 2 * Math.PI / posts;
                var a1 = (i + 1) * 2 * Math.PI / posts;
                var gap = Math.Abs(Math.Sin(a0 * 2)) < 0.12 && Math.Cos(a0 * 4) > 0;

                if (gap)
                {
                    continue;
                }

                var p0 = new V3((float)Math.Sin(a0) * 28, 0, (float)Math.Cos(a0) * 28);
                var p1 = new V3((float)Math.Sin(a1) * 28, 0, (float)Math.Cos(a1) * 28);
                k.Beam(p0 + new V3(0, 0.9f, 0), p1 + new V3(0, 0.9f, 0), 0.18f, 0.16f, Plaster);
                k.Beam(p0 + new V3(0, 0.3f, 0), p1 + new V3(0, 0.3f, 0), 0.12f, 0.1f, Plaster.Times(0.95f));

                if (i % 3 == 0)
                {
                    k.Box(p0.X, 0, p0.Z, 0.24f, 1.15f, 0.24f, Plaster);
                }
            }

            return k.Mesh;
        }

        /// <summary>A training post: a pole on a stone foot with two arms and a straw-wrapped body.</summary>
        public static MeshData DummyPost()
        {
            var k = new Kit();
            k.Drum(0, 0, 0, 0.45f, 0.25f, 8, StoneDark);
            k.Post(0, 0.25f, 0, 0.1f, 0.09f, 1.9f, 8, Wood);
            k.Post(0, 0.9f, 0, 0.26f, 0.24f, 0.8f, 10, Tint.Hex(0xC8B070));
            k.Beam(new V3(-0.55f, 1.45f, 0), new V3(0.55f, 1.45f, 0), 0.09f, 0.09f, Wood);
            k.Ball(new V3(0, 2.0f, 0), 0.2f, 1.1f, 8, 5, Tint.Hex(0xC8B070));

            return k.Mesh;
        }

        /// <summary>The altar of the aperture where Gu are lent for trying: a jade plinth, a bronze ding on it, four lamps.</summary>
        public static MeshData GuAltar()
        {
            var k = new Kit();
            k.Box(0, 0, 0, 3.4f, 0.4f, 3.4f, JadeDeep);
            k.Box(0, 0.4f, 0, 2.6f, 0.7f, 2.6f, Jade);
            k.Post(0, 1.1f, 0, 0.75f, 0.9f, 0.8f, 12, Tint.Hex(0x6A5A3A));
            k.Ring(0, 1.91f, 0, 0.6f, 0.92f, 12, Gold);

            // Its two handles.
            k.Box(-0.95f, 1.75f, 0, 0.12f, 0.45f, 0.4f, Tint.Hex(0x6A5A3A));
            k.Box(0.95f, 1.75f, 0, 0.12f, 0.45f, 0.4f, Tint.Hex(0x6A5A3A));

            foreach (var (x, z) in new[] { (-1.4f, -1.4f), (1.4f, -1.4f), (-1.4f, 1.4f), (1.4f, 1.4f) })
            {
                k.Post(x, 0.4f, z, 0.08f, 0.08f, 1.0f, 6, Gold.Times(0.8f));
                k.Ball(new V3(x, 1.55f, z), 0.18f, 1.2f, 8, 4, Paper);
            }

            return k.Mesh;
        }
    }
}
