using System;
using System.Collections.Generic;
using UnityEngine;

namespace GuWorld.UI
{
    /// <summary>
    /// The shapes the interface is drawn with, made in code once: white on clear (tinted by
    /// whatever shows them) unless their colours are part of the shape (the hexagon buttons,
    /// the stars). Most are sliced, so they stretch to any size and keep their edges.
    /// </summary>
    public static partial class UiKit
    {
        static Sprite plain, rounded, softEdge, chamfer, chamferFrame, ring, disc, diamond, arrow;
        static Sprite outline, octagon, octagonLine, hexagonUp, hexagonUpLine, glow, radial, vignette, fadeBoth, keycap, pill, pillLine, halo;
        static Texture2D stars;
        static readonly Dictionary<HexKind, Sprite> hexes = new Dictionary<HexKind, Sprite>();

        /// <summary>A plain white square.</summary>
        public static Sprite Plain => plain != null ? plain : plain = Make(4, 4, 1, (x, y) => 1, Vector4.zero);

        /// <summary>A rounded rectangle, sliced so it stretches to any size.</summary>
        public static Sprite Rounded => rounded != null ? rounded : rounded = MakeShape(64, 14, Shape.Round, 0);

        /// <summary>A rectangle with its corners cut off, and its frame (the older panels).</summary>
        public static Sprite Chamfer => chamfer != null ? chamfer : chamfer = MakeShape(64, 10, Shape.Cut, 0);
        public static Sprite ChamferFrame => chamferFrame != null ? chamferFrame : chamferFrame = MakeShape(64, 10, Shape.Cut, 1.6f);

        /// <summary>White fading to clear from left to right (a side of the screen darkened, a rule fading out).</summary>
        public static Sprite SoftEdge => softEdge != null ? softEdge : softEdge = MakeFade();

        /// <summary>Clear, white across the middle three fifths, clear again (the header's line).</summary>
        public static Sprite FadeBoth => fadeBoth != null ? fadeBoth : fadeBoth = Make(256, 1, 1, (x, y) =>
        {
            var t = x / 255f;

            return Mathf.Clamp01(Mathf.Min(t, 1 - t) / 0.2f);
        }, Vector4.zero);

        /// <summary>A ring (for radial fills: the essence round the portrait, the sea), a disc, a diamond, an arrow.</summary>
        public static Sprite Ring => ring != null ? ring : ring = MakeRound(256, 0.78f);
        public static Sprite Disc => disc != null ? disc : disc = MakeRound(128, 0);
        public static Sprite Diamond => diamond != null ? diamond : diamond = MakeDiamond(128);
        public static Sprite Arrow => arrow != null ? arrow : arrow = MakeArrow(64);

        static readonly Dictionary<(int, int), Sprite> rings = new Dictionary<(int, int), Sprite>();

        /// <summary>A circle's outline `size` across, about a unit thick, whole or in dashes of 2 with `gap` between (the design's rings).</summary>
        public static Sprite RingLine(float size, int gap = 0)
        {
            var across = Mathf.Max(8, Mathf.RoundToInt(size));

            if (rings.TryGetValue((across, gap), out var made))
            {
                return made;
            }

            // Two pixels a unit; the line's middle half a unit in from the edge.
            var w = across * 2 + 4;
            var radius = across - 1f;
            var period = 2 + gap;
            var count = gap > 0 ? Mathf.Max(8, Mathf.RoundToInt(Mathf.PI * across / period)) : 0;

            return rings[(across, gap)] = Make(w, w, 2, (x, y) =>
            {
                var p = new Vector2(x + 0.5f - w * 0.5f, y + 0.5f - w * 0.5f);
                var line = Mathf.Clamp01(1.6f - Mathf.Abs(p.magnitude - radius));

                if (count > 0)
                {
                    var turn = (Mathf.Atan2(p.y, p.x) / (2 * Mathf.PI) + 1) % 1 * count;
                    line *= turn % 1 < 2f / period ? 1 : 0;
                }

                return line;
            }, Vector4.zero);
        }

        static readonly Dictionary<(int, int), Sprite> arcs = new Dictionary<(int, int), Sprite>();

        /// <summary>A ring `size` across whose stroke is `width` thick at its outer edge (for arcs: set the image to Filled, Radial360).</summary>
        public static Sprite ArcRing(float size, float width)
        {
            var across = Mathf.Max(8, Mathf.RoundToInt(size));
            var thick = Mathf.Max(1, Mathf.RoundToInt(width * 2));

            if (arcs.TryGetValue((across, thick), out var made))
            {
                return made;
            }

            var w = across * 2;
            var outer = across - 0.5f;
            var inner = outer - thick;

            return arcs[(across, thick)] = Make(w, w, 2, (x, y) =>
            {
                var d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(across, across));

                return Mathf.Clamp01(outer - d + 0.5f) * Mathf.Clamp01(d - inner + 0.5f);
            }, Vector4.zero);
        }

        /// <summary>A one-unit square outline, sliced (the panels' and fields' frames).</summary>
        public static Sprite Outline => outline != null ? outline : outline = Make(16, 16, 1, (x, y) =>
            x == 0 || y == 0 || x == 15 || y == 15 ? 1 : 0, new Vector4(2, 2, 2, 2));

        /// <summary>The design's octagon (corners cut 10 at 72 across), filled with its dark round glow, and its edge.</summary>
        public static Sprite Octagon => octagon != null ? octagon : octagon = Make(72, 72, 1, (x, y) =>
        {
            var d = OctagonDistance(x + 0.5f, y + 0.5f, 72, 10);
            var inside = Mathf.Clamp01(d + 0.5f);

            return inside;
        }, new Vector4(12, 12, 12, 12), (x, y) =>
        {
            // radial-gradient(circle at 50% 40%, rgba(30,40,56,.6), rgba(4,6,10,.88) 75%)
            var t = Mathf.Clamp01(Vector2.Distance(new Vector2(x, 71 - y), new Vector2(36, 29)) / 54f);

            return Color.Lerp(new Color(30 / 255f, 40 / 255f, 56 / 255f, 0.6f), new Color(4 / 255f, 6 / 255f, 10 / 255f, 0.88f), t);
        });

        public static Sprite OctagonLine => octagonLine != null ? octagonLine : octagonLine = Make(72, 72, 1, (x, y) =>
        {
            var d = OctagonDistance(x + 0.5f, y + 0.5f, 72, 10);

            return Mathf.Clamp01(d + 0.5f) * Mathf.Clamp01(1.5f - d);
        }, new Vector4(12, 12, 12, 12));

        static float OctagonDistance(float px, float py, float size, float cut)
        {
            var inside = Mathf.Min(Mathf.Min(px, size - px), Mathf.Min(py, size - py));
            var corner = (Mathf.Min(px, size - px) + Mathf.Min(py, size - py) - cut) / 1.4142f;

            return Mathf.Min(inside, corner);
        }

        /// <summary>The brand's hexagon (pointed top and bottom, 34 × 38), filled and outlined.</summary>
        public static Sprite HexagonUp => hexagonUp != null ? hexagonUp : hexagonUp = Make(136, 152, 4, (x, y) =>
            Mathf.Clamp01(HexUpDistance(x + 0.5f, y + 0.5f) + 0.5f), Vector4.zero);

        public static Sprite HexagonUpLine => hexagonUpLine != null ? hexagonUpLine : hexagonUpLine = Make(136, 152, 4, (x, y) =>
        {
            var d = HexUpDistance(x + 0.5f, y + 0.5f);

            return Mathf.Clamp01(d + 0.5f) * Mathf.Clamp01(5.3f - d);
        }, Vector4.zero);

        /// <summary>Distance inside the path M17 1 L33 10 V28 L17 37 L1 28 V10Z at ×4.</summary>
        static float HexUpDistance(float x, float y)
        {
            var points = new[] { new Vector2(68, 4), new Vector2(132, 40), new Vector2(132, 112), new Vector2(68, 148), new Vector2(4, 112), new Vector2(4, 40) };

            return Convex(new Vector2(x, 152 - y), points);
        }

        /// <summary>Signed distance inside a convex polygon whose points go clockwise on screen (y down).</summary>
        static float Convex(Vector2 p, IList<Vector2> points)
        {
            var d = float.MaxValue;

            for (var i = 0; i < points.Count; i++)
            {
                var a = points[i];
                var b = points[(i + 1) % points.Count];
                var edge = (b - a).normalized;
                var normal = new Vector2(-edge.y, edge.x);
                d = Mathf.Min(d, Vector2.Dot(p - a, normal));
            }

            return d;
        }

        /// <summary>
        /// A soft halo: clear in the middle, glowing at the inner edge of its border and fading
        /// out (a CSS box-shadow round what it is laid over). Sliced: its border is 32.
        /// </summary>
        public static Sprite GlowSprite => glow != null ? glow : glow = Make(80, 80, 1, (x, y) =>
        {
            var dx = Mathf.Max(0, Mathf.Max(32 - (x + 0.5f), x + 0.5f - 48));
            var dy = Mathf.Max(0, Mathf.Max(32 - (y + 0.5f), y + 0.5f - 48));
            var d = Mathf.Sqrt(dx * dx + dy * dy) / 32f;

            if (dx <= 0 && dy <= 0)
            {
                return 0;
            }

            var t = Mathf.Clamp01(1 - d);

            return t * t * (3 - 2 * t);
        }, new Vector4(32, 32, 32, 32));

        /// <summary>A round soft glow: full at the middle, clear at 70% of its radius (the design's radial gradients).</summary>
        public static Sprite Radial => radial != null ? radial : radial = Make(128, 128, 1, (x, y) =>
        {
            var d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(64, 64)) / 64f;

            return Mathf.Clamp01(1 - d / 0.7f);
        }, Vector4.zero);

        /// <summary>A round halo fading from the middle to its edge, smoothly (a glow round a dot or a node).</summary>
        public static Sprite Halo => halo != null ? halo : halo = Make(128, 128, 1, (x, y) =>
        {
            var d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(64, 64)) / 64f;
            var t = Mathf.Clamp01(1 - d);

            return t * t;
        }, Vector4.zero);

        /// <summary>The screen's dark edges: clear inside 55% of the way to the corners, then black to .72 alpha.</summary>
        public static Sprite Vignette => vignette != null ? vignette : vignette = Make(128, 128, 1, (x, y) =>
        {
            var dx = (x + 0.5f - 64) / 64f;
            var dy = (y + 0.5f - 64) / 64f;
            var d = Mathf.Sqrt(dx * dx + dy * dy) / 1.4142f;
            var t = Mathf.Clamp01((d - 0.5f) / 0.5f);

            return t;
        }, Vector4.zero, (x, y) => Color.black);

        /// <summary>A keycap: a box with rounded corners, a one-unit edge and a two-unit foot.</summary>
        public static Sprite Keycap => keycap != null ? keycap : keycap = Make(32, 32, 2, (x, y) =>
        {
            var px = x + 0.5f;
            var py = y + 0.5f;
            var cx = Mathf.Max(0, Mathf.Max(8 - px, px - 24));
            var cy = Mathf.Max(0, Mathf.Max(8 - py, py - 24));
            var outer = 8 - Mathf.Sqrt(cx * cx + cy * cy);
            outer = cx > 0 && cy > 0 ? outer : Mathf.Min(Mathf.Min(px, 32 - px), Mathf.Min(py, 32 - py));
            var foot = py < 5 ? 4 : 2;
            var edge = Mathf.Clamp01(outer + 0.5f) * Mathf.Clamp01(foot + 0.5f - outer);

            return edge;
        }, new Vector4(10, 10, 10, 10));

        /// <summary>A pill (the toggle's track), filled and its edge.</summary>
        public static Sprite Pill => pill != null ? pill : pill = Make(80, 40, 2, (x, y) => Mathf.Clamp01(PillDistance(x, y) + 0.5f), new Vector4(20, 0, 20, 0));
        public static Sprite PillLine => pillLine != null ? pillLine : pillLine = Make(80, 40, 2, (x, y) =>
        {
            var d = PillDistance(x, y);

            return Mathf.Clamp01(d + 0.5f) * Mathf.Clamp01(2.5f - d);
        }, new Vector4(20, 0, 20, 0));

        static float PillDistance(int x, int y)
        {
            var px = Mathf.Clamp(x + 0.5f, 20, 60);

            return 20 - Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(px, 20));
        }

        /// <summary>The kinds of the hexagon buttons (UI-System: .btn, .solid, .ghost, .bad, .off).</summary>
        public enum HexKind
        {
            Main,
            Solid,
            Ghost,
            Bad,
            Off,
        }

        /// <summary>
        /// A hexagon button's whole face, its edge and its body with their gradients, 56 tall,
        /// sliced 21 in from each end: stretched to any height, its points stay 20 deep as the
        /// design's clip-path keeps them.
        /// </summary>
        public static Sprite HexFace(HexKind kind)
        {
            if (hexes.TryGetValue(kind, out var made))
            {
                return made;
            }

            const int scale = 2;
            const int w = 128 * scale;
            const int h = 56 * scale;
            Color[] edge, body;
            float edgeMid, bodyMid;

            switch (kind)
            {
                case HexKind.Solid:
                    edge = new[] { Hex(0xFFF1CF), Hex(0xC9A060), Hex(0x6E5028) };
                    edgeMid = 0.5f;
                    body = new[] { Hex(0xFFF1CF), Hex(0xE2C080), Hex(0xC29E5C) };
                    bodyMid = 0.55f;
                    break;
                case HexKind.Ghost:
                    edge = new[] { new Color(234 / 255f, 240 / 255f, 242 / 255f, 0.24f), new Color(234 / 255f, 240 / 255f, 242 / 255f, 0.22f), new Color(234 / 255f, 240 / 255f, 242 / 255f, 0.18f) };
                    edgeMid = 0.5f;
                    body = new[] { new Color(14 / 255f, 18 / 255f, 26 / 255f, 0.96f), new Color(10 / 255f, 13 / 255f, 19 / 255f, 0.96f), new Color(6 / 255f, 8 / 255f, 12 / 255f, 0.96f) };
                    bodyMid = 0.5f;
                    break;
                case HexKind.Bad:
                    edge = new[] { Hex(0xFFC2B0), Hex(0xB84A3C), Hex(0x5A1A12) };
                    edgeMid = 0.55f;
                    body = new[] { Hex(0x2A0E0A), Hex(0x1F0A08), Hex(0x140605) };
                    bodyMid = 0.5f;
                    break;
                case HexKind.Off:
                    edge = new[] { new Color(234 / 255f, 240 / 255f, 242 / 255f, 0.14f), new Color(234 / 255f, 240 / 255f, 242 / 255f, 0.14f), new Color(234 / 255f, 240 / 255f, 242 / 255f, 0.14f) };
                    edgeMid = 0.5f;
                    body = new[] { Hex(0x08090C), Hex(0x08090C), Hex(0x08090C) };
                    bodyMid = 0.5f;
                    break;
                default:
                    edge = new[] { Hex(0xFFF1CF), Hex(0xC9A060), Hex(0x6E5028) };
                    edgeMid = 0.5f;
                    body = new[] { Hex(0x2A1A10), Hex(0x1E130B), Hex(0x120A06) };
                    bodyMid = 0.5f;
                    break;
            }

            Color Stops(Color[] c, float mid, float t) => t < mid ? Color.Lerp(c[0], c[1], t / mid) : Color.Lerp(c[1], c[2], (t - mid) / (1 - mid));

            var outer = new[] { new Vector2(20, 0), new Vector2(108, 0), new Vector2(128, 28), new Vector2(108, 56), new Vector2(20, 56), new Vector2(0, 28) };
            var inner = new[] { new Vector2(20, 1), new Vector2(108, 1), new Vector2(127, 28), new Vector2(108, 55), new Vector2(20, 55), new Vector2(1, 28) };
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
            var pixels = new Color[w * h];

            for (var y = 0; y < h; y++)
            {
                for (var x = 0; x < w; x++)
                {
                    var p = new Vector2((x + 0.5f) / scale, (h - y - 0.5f) / scale);
                    var o = Mathf.Clamp01(Convex(p, outer) * scale + 0.5f);
                    var i = Mathf.Clamp01(Convex(p, inner) * scale + 0.5f);
                    var t = p.y / 56f;
                    var colour = Color.Lerp(Stops(edge, edgeMid, t), Stops(body, bodyMid, Mathf.InverseLerp(1, 55, p.y)), i);
                    colour.a *= o;
                    pixels[y * w + x] = colour;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            return hexes[kind] = Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100 * scale, 0, SpriteMeshType.FullRect,
                new Vector4(21 * scale, 0, 21 * scale, 0));
        }

        /// <summary>The stars over the void: a 420 × 300 tile of ten small soft dots (the design's .stars), to repeat.</summary>
        public static Texture2D Stars
        {
            get
            {
                if (stars != null)
                {
                    return stars;
                }

                const int w = 420;
                const int h = 300;
                var dots = new (float x, float y, float r, Color c)[]
                {
                    (0.07f, 0.12f, 1.2f, Color.white), (0.23f, 0.67f, 1f, Hex(0xDDFFFF)), (0.41f, 0.31f, 1.4f, Color.white), (0.58f, 0.82f, 1f, Hex(0xCCDDFF)),
                    (0.73f, 0.18f, 1.2f, Color.white), (0.88f, 0.54f, 1f, Hex(0xEEFFFF)), (0.15f, 0.44f, 0.8f, Color.white), (0.66f, 0.47f, 0.8f, Color.white),
                    (0.34f, 0.90f, 0.8f, Color.white), (0.94f, 0.86f, 0.8f, Color.white),
                };
                stars = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, hideFlags = HideFlags.DontSave, filterMode = FilterMode.Bilinear };
                var pixels = new Color[w * h];

                foreach (var (dx, dy, r, c) in dots)
                {
                    var cx = dx * w;
                    var cy = (1 - dy) * h;

                    for (var y = Mathf.FloorToInt(cy - 3); y <= Mathf.CeilToInt(cy + 3); y++)
                    {
                        for (var x = Mathf.FloorToInt(cx - 3); x <= Mathf.CeilToInt(cx + 3); x++)
                        {
                            var d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                            var a = Mathf.Clamp01(r - d + 0.6f);

                            if (a > 0)
                            {
                                var i = (y + h) % h * w + (x + w) % w;
                                pixels[i] = new Color(c.r, c.g, c.b, Mathf.Max(pixels[i].a, a));
                            }
                        }
                    }
                }

                stars.SetPixels(pixels);
                stars.Apply();

                return stars;
            }
        }

        /// <summary>Makes a sprite of a shape given as coverage (0…1) per pixel, white or coloured, at `ppuScale` pixels per unit.</summary>
        static Sprite Make(int w, int h, int ppuScale, Func<int, int, float> alpha, Vector4 border, Func<int, int, Color> colour = null)
        {
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
            var pixels = new Color[w * h];

            for (var y = 0; y < h; y++)
            {
                for (var x = 0; x < w; x++)
                {
                    var c = colour != null ? colour(x, y) : Color.white;
                    c.a *= alpha(x, y);
                    pixels[y * w + x] = c;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100 * ppuScale, 0, SpriteMeshType.FullRect, border);
        }

        enum Shape
        {
            Round,
            Cut,
        }

        /// <summary>A sliced shape: rounded or corner-cut, filled or (line &gt; 0) just its outline.</summary>
        static Sprite MakeShape(int size, int corner, Shape shape, float line)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
            var pixels = new Color32[size * size];

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    // Signed distance to the edge (positive inside).
                    var px = x + 0.5f;
                    var py = y + 0.5f;
                    var inside = Mathf.Min(Mathf.Min(px, size - px), Mathf.Min(py, size - py));
                    var cx = Mathf.Max(0, Mathf.Max(corner - px, px - (size - corner)));
                    var cy = Mathf.Max(0, Mathf.Max(corner - py, py - (size - corner)));
                    float d;

                    if (shape == Shape.Round)
                    {
                        d = cx > 0 && cy > 0 ? corner - Mathf.Sqrt(cx * cx + cy * cy) : inside;
                    }
                    else
                    {
                        // The cut: a 45° line across each corner.
                        d = Mathf.Min(inside, (Mathf.Min(px, size - px) + Mathf.Min(py, size - py) - corner) / 1.4142f);
                    }

                    var alpha = line > 0 ? Mathf.Clamp01(d + 0.5f) * Mathf.Clamp01(line + 0.5f - d) : Mathf.Clamp01(d + 0.5f);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            var border = corner + 3;

            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        }

        /// <summary>A disc, or (hole &gt; 0) a ring whose hole is that share of its radius.</summary>
        static Sprite MakeRound(int size, float hole)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
            var pixels = new Color32[size * size];
            var r = size * 0.5f;

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                    var alpha = Mathf.Clamp01(r - 0.5f - d + 0.5f);

                    if (hole > 0)
                    {
                        alpha *= Mathf.Clamp01(d - r * hole + 0.5f);
                    }

                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        static Sprite MakeDiamond(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
            var pixels = new Color32[size * size];
            var r = size * 0.5f;

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var d = r - (Mathf.Abs(x + 0.5f - r) + Mathf.Abs(y + 0.5f - r));
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(d / 1.4142f + 0.5f) * 255));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        /// <summary>An arrowhead pointing up (the hero on the maps).</summary>
        static Sprite MakeArrow(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
            var pixels = new Color32[size * size];
            Vector2 tip = new Vector2(size * 0.5f, size * 0.95f), left = new Vector2(size * 0.12f, size * 0.08f), notch = new Vector2(size * 0.5f, size * 0.3f), right = new Vector2(size * 0.88f, size * 0.08f);

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f);
                    var inside = InTriangle(p, tip, left, notch) || InTriangle(p, tip, notch, right);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(inside ? 255 : 0));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        static bool InTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float Side(Vector2 u, Vector2 v, Vector2 w) => (u.x - w.x) * (v.y - w.y) - (v.x - w.x) * (u.y - w.y);
            var d1 = Side(p, a, b);
            var d2 = Side(p, b, c);
            var d3 = Side(p, c, a);

            return !((d1 < 0 || d2 < 0 || d3 < 0) && (d1 > 0 || d2 > 0 || d3 > 0));
        }

        static Sprite MakeFade()
        {
            const int width = 256;
            var texture = new Texture2D(width, 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };

            for (var x = 0; x < width; x++)
            {
                var t = x / (width - 1f);
                texture.SetPixel(x, 0, new Color(1, 1, 1, 1 - t * t * (3 - 2 * t)));
            }

            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, width, 1), new Vector2(0.5f, 0.5f));
        }
    }
}
