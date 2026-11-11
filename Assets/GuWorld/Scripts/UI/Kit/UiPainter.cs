using System.Collections.Generic;
using UnityEngine;

namespace GuWorld.UI
{
    /// <summary>
    /// Paints still pictures for the menus into a texture, as the design's SVG scenes do:
    /// gradients, polygons with a gradient top to bottom, rectangles and soft ellipses
    /// (a blurred SVG ellipse), laid one over another. Coordinates are the design's, y down.
    /// </summary>
    public class UiPainter
    {
        readonly int width;
        readonly int height;
        readonly float scale;
        readonly Color[] pixels;

        /// <summary>A canvas of the design's `w` × `h`, painted at `scale` pixels per design pixel.</summary>
        public UiPainter(int w, int h, float scale = 1)
        {
            this.scale = scale;
            width = Mathf.RoundToInt(w * scale);
            height = Mathf.RoundToInt(h * scale);
            pixels = new Color[width * height];
        }

        void Blend(int x, int y, Color colour, float cover)
        {
            if (x < 0 || y < 0 || x >= width || y >= height || cover <= 0)
            {
                return;
            }

            var i = (height - 1 - y) * width + x;
            var a = colour.a * Mathf.Clamp01(cover);
            var under = pixels[i];
            var outA = a + under.a * (1 - a);

            if (outA <= 0)
            {
                return;
            }

            var rgb = ((Vector4)colour * a + (Vector4)under * under.a * (1 - a)) / outA;
            pixels[i] = new Color(rgb.x, rgb.y, rgb.z, outA);
        }

        /// <summary>The whole canvas, a gradient from top to bottom (with a middle colour at a share).</summary>
        public void Sky(Color top, Color bottom, Color? middle = null, float middleAt = 0.5f)
        {
            for (var y = 0; y < height; y++)
            {
                var t = y / (height - 1f);
                var c = middle is Color m
                    ? t < middleAt ? Color.Lerp(top, m, t / middleAt) : Color.Lerp(m, bottom, (t - middleAt) / (1 - middleAt))
                    : Color.Lerp(top, bottom, t);

                for (var x = 0; x < width; x++)
                {
                    Blend(x, y, c, 1);
                }
            }
        }

        /// <summary>A CSS radial-gradient ellipse: the colour at its middle, clear at `fade` of its radii.</summary>
        public void Glow(Vector2 centre, Vector2 radii, Color colour, float fade = 0.7f)
        {
            Ellipse(centre, radii, colour, d => Mathf.Clamp01(1 - d / fade));
        }

        /// <summary>A blurred ellipse (an SVG ellipse under a Gaussian blur of `blur`).</summary>
        public void Soft(Vector2 centre, Vector2 radii, Color colour, float blur)
        {
            var c = centre * scale;
            var r = radii * scale;
            var sigma = blur * scale;
            var reach = r + Vector2.one * sigma * 2.5f;

            for (var y = Mathf.Max(0, Mathf.FloorToInt(c.y - reach.y)); y < Mathf.Min(height, Mathf.CeilToInt(c.y + reach.y)); y++)
            {
                for (var x = Mathf.Max(0, Mathf.FloorToInt(c.x - reach.x)); x < Mathf.Min(width, Mathf.CeilToInt(c.x + reach.x)); x++)
                {
                    var o = new Vector2(x + 0.5f - c.x, y + 0.5f - c.y);
                    var dn = Mathf.Sqrt(o.x * o.x / (r.x * r.x) + o.y * o.y / (r.y * r.y));
                    // How far past the ellipse's edge, along the line from its middle.
                    var past = dn > 0 ? o.magnitude * (1 - 1 / dn) : -r.magnitude;
                    Blend(x, y, colour, Mathf.Clamp01(0.5f - past / (2.5f * sigma)));
                }
            }
        }

        void Ellipse(Vector2 centre, Vector2 radii, Color colour, System.Func<float, float> cover)
        {
            var c = centre * scale;
            var r = radii * scale;

            for (var y = Mathf.Max(0, Mathf.FloorToInt(c.y - r.y)); y < Mathf.Min(height, Mathf.CeilToInt(c.y + r.y)); y++)
            {
                for (var x = Mathf.Max(0, Mathf.FloorToInt(c.x - r.x)); x < Mathf.Min(width, Mathf.CeilToInt(c.x + r.x)); x++)
                {
                    var dx = (x + 0.5f - c.x) / r.x;
                    var dy = (y + 0.5f - c.y) / r.y;
                    var d = Mathf.Sqrt(dx * dx + dy * dy);

                    if (d < 1)
                    {
                        Blend(x, y, colour, cover(d));
                    }
                }
            }
        }

        /// <summary>A polygon (any shape, even-odd), one colour or a gradient from its top to its bottom.</summary>
        public void Polygon(IList<Vector2> points, Color top, Color? bottom = null)
        {
            var minY = float.MaxValue;
            var maxY = float.MinValue;

            foreach (var p in points)
            {
                minY = Mathf.Min(minY, p.y * scale);
                maxY = Mathf.Max(maxY, p.y * scale);
            }

            var crossings = new List<float>();
            const int samples = 3;

            for (var y = Mathf.Max(0, Mathf.FloorToInt(minY)); y < Mathf.Min(height, Mathf.CeilToInt(maxY)); y++)
            {
                var t = maxY > minY ? (y + 0.5f - minY) / (maxY - minY) : 0;
                var colour = bottom is Color b ? Color.Lerp(top, b, t) : top;
                var row = new float[width + 1];

                for (var s = 0; s < samples; s++)
                {
                    var sy = y + (s + 0.5f) / samples;
                    crossings.Clear();

                    for (var i = 0; i < points.Count; i++)
                    {
                        var a = points[i] * scale;
                        var c = points[(i + 1) % points.Count] * scale;

                        if ((a.y <= sy && c.y > sy) || (c.y <= sy && a.y > sy))
                        {
                            crossings.Add(a.x + (sy - a.y) / (c.y - a.y) * (c.x - a.x));
                        }
                    }

                    crossings.Sort();

                    for (var k = 0; k + 1 < crossings.Count; k += 2)
                    {
                        var x0 = Mathf.Clamp(crossings[k], 0, width);
                        var x1 = Mathf.Clamp(crossings[k + 1], 0, width);

                        for (var x = Mathf.FloorToInt(x0); x < Mathf.CeilToInt(x1) && x < width; x++)
                        {
                            var cover = Mathf.Min(x + 1, x1) - Mathf.Max(x, x0);
                            row[x] += Mathf.Max(0, cover) / samples;
                        }
                    }
                }

                for (var x = 0; x < width; x++)
                {
                    if (row[x] > 0)
                    {
                        Blend(x, y, colour, row[x]);
                    }
                }
            }
        }

        public void Rect(float x, float y, float w, float h, Color colour) =>
            Polygon(new[] { new Vector2(x, y), new Vector2(x + w, y), new Vector2(x + w, y + h), new Vector2(x, y + h) }, colour);

        /// <summary>The painting as a texture.</summary>
        public Texture2D Texture()
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
            texture.SetPixels(pixels);
            texture.Apply(false, true);

            return texture;
        }

        /// <summary>A path of points from "x y x y …" (the design's SVG numbers).</summary>
        public static Vector2[] Path(params float[] xy)
        {
            var points = new Vector2[xy.Length / 2];

            for (var i = 0; i < points.Length; i++)
            {
                points[i] = new Vector2(xy[i * 2], xy[i * 2 + 1]);
            }

            return points;
        }

        /// <summary>A quadratic curve's points from a to b round c (SVG Q), `steps` of them, without a.</summary>
        public static IEnumerable<Vector2> Curve(Vector2 a, Vector2 c, Vector2 b, int steps = 8)
        {
            for (var i = 1; i <= steps; i++)
            {
                var t = i / (float)steps;
                yield return (1 - t) * (1 - t) * a + 2 * (1 - t) * t * c + t * t * b;
            }
        }

        /// <summary>A cubic curve's points (SVG C), without a.</summary>
        public static IEnumerable<Vector2> Cubic(Vector2 a, Vector2 c1, Vector2 c2, Vector2 b, int steps = 16)
        {
            for (var i = 1; i <= steps; i++)
            {
                var t = i / (float)steps;
                var u = 1 - t;
                yield return u * u * u * a + 3 * u * u * t * c1 + 3 * u * t * t * c2 + t * t * t * b;
            }
        }
    }
}
