using System;
using System.Collections.Generic;
using GuWorld.Core.Gen;

/// <summary>
/// A small software renderer for looking at generated models without the engine: perspective,
/// a depth buffer, vertex colours interpolated across faces, a sun with half-Lambert wrap and a
/// sky light, distance fog, an optional ground. Faces seen from behind are counted (and can be
/// painted magenta) so a wrong winding shows. The engine's axes: x right, y up, z forward.
/// </summary>
sealed class Raster
{
    public readonly int Width, Height;
    public readonly byte[] Rgb;
    readonly float[] depth;
    readonly float[] colour;
    readonly bool[] drawn;
    readonly bool[] backAt;
    readonly bool[] counted;
    V3 eye, forward, right, up;
    float focal;
    public V3 Sun = new V3(0.45f, 0.8f, -0.35f).Normalized;
    public bool PaintBackFaces = true;
    bool counting = true;

    /// <summary>Pixels showing a model now, and those of them showing a face from behind (what is seen, not what was drawn over).</summary>
    public long Pixels
    {
        get
        {
            long n = 0;

            for (var i = 0; i < drawn.Length; i++)
            {
                n += drawn[i] && counted[i] ? 1 : 0;
            }

            return n;
        }
    }

    public long BackPixels
    {
        get
        {
            long n = 0;

            for (var i = 0; i < backAt.Length; i++)
            {
                n += backAt[i] ? 1 : 0;
            }

            return n;
        }
    }
    public float FogDistance = 0;

    public Raster(int width, int height)
    {
        Width = width;
        Height = height;
        Rgb = new byte[width * height * 3];
        depth = new float[width * height];
        colour = new float[width * height * 3];
        drawn = new bool[width * height];
        backAt = new bool[width * height];
        counted = new bool[width * height];
        Clear();
    }

    public void Clear()
    {
        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                var i = y * Width + x;
                depth[i] = float.MaxValue;
                drawn[i] = false;
                backAt[i] = false;
                counted[i] = false;
                var k = y / (float)Height;
                colour[i * 3] = 0.58f + 0.22f * k;
                colour[i * 3 + 1] = 0.66f + 0.18f * k;
                colour[i * 3 + 2] = 0.76f + 0.1f * k;
            }
        }
    }

    public void Camera(V3 from, V3 at, float fovDegrees = 40)
    {
        eye = from;
        forward = (at - from).Normalized;
        right = V3.Cross(V3.Up, forward).Normalized;
        up = V3.Cross(forward, right);
        focal = Height * 0.5f / (float)Math.Tan(fovDegrees * Math.PI / 360);
    }

    /// <summary>A camera that frames a box from a direction (`view` from the box toward the eye).</summary>
    public void Frame(V3 min, V3 max, V3 view, float fovDegrees = 30, float margin = 1.15f)
    {
        var centre = (min + max) * 0.5f;
        var radius = Math.Max(0.5f, (max - min).Length * 0.5f);
        var distance = radius * margin / (float)Math.Sin(fovDegrees * Math.PI / 360);
        Camera(centre + view.Normalized * distance, centre, fovDegrees);
    }

    (float x, float y, float z) Project(V3 p)
    {
        var d = p - eye;
        var z = V3.Dot(d, forward);

        return (Width / 2f + V3.Dot(d, right) / z * focal, Height / 2f - V3.Dot(d, up) / z * focal, z);
    }

    /// <summary>A flat ground at y, a checker of two greens out to `size` metres round the origin.</summary>
    public void Ground(float y, float size, V3 centre)
    {
        var mesh = new MeshData();
        const int n = 24;

        for (var j = 0; j < n; j++)
        {
            for (var i = 0; i < n; i++)
            {
                var x0 = centre.X - size + 2 * size * i / n;
                var z0 = centre.Z - size + 2 * size * j / n;
                var x1 = centre.X - size + 2 * size * (i + 1) / n;
                var z1 = centre.Z - size + 2 * size * (j + 1) / n;
                var g = (i + j) % 2 == 0 ? 0.36f : 0.33f;
                var a = mesh.Vertex(x0, y, z0, 0, 0, g * 0.95f, g * 1.15f, g * 0.7f);
                var b = mesh.Vertex(x0, y, z1, 0, 0, g * 0.95f, g * 1.15f, g * 0.7f);
                var c = mesh.Vertex(x1, y, z1, 0, 0, g * 0.95f, g * 1.15f, g * 0.7f);
                var d = mesh.Vertex(x1, y, z0, 0, 0, g * 0.95f, g * 1.15f, g * 0.7f);
                mesh.Quad(a, b, c, d);
            }
        }

        counting = false;
        Draw(mesh, 0, 0, 0, 1);
        counting = true;
    }

    /// <summary>Draws a mesh placed at (x, y, z), turned `yaw` degrees about the vertical, scaled.</summary>
    public void Draw(MeshData mesh, float px, float py, float pz, float scale, float yaw = 0)
    {
        var count = mesh.VertexCount;
        var screen = new (float x, float y, float z)[count];
        var world = new V3[count];
        var normal = new V3[count];
        var cos = (float)Math.Cos(yaw * Math.PI / 180);
        var sin = (float)Math.Sin(yaw * Math.PI / 180);

        for (var i = 0; i < count; i++)
        {
            float x = mesh.Positions[i * 3], y = mesh.Positions[i * 3 + 1], z = mesh.Positions[i * 3 + 2];
            // Unity's turn about Y: +X goes to (cos, 0, −sin).
            world[i] = new V3(px + (x * cos + z * sin) * scale, py + y * scale, pz + (-x * sin + z * cos) * scale);
            float nx = mesh.Normals[i * 3], ny = mesh.Normals[i * 3 + 1], nz = mesh.Normals[i * 3 + 2];
            normal[i] = new V3(nx * cos + nz * sin, ny, -nx * sin + nz * cos);
            screen[i] = Project(world[i]);
        }

        for (var t = 0; t < mesh.Triangles.Count; t += 3)
        {
            int ia = mesh.Triangles[t], ib = mesh.Triangles[t + 1], ic = mesh.Triangles[t + 2];
            var a = screen[ia];
            var b = screen[ib];
            var c = screen[ic];

            if (a.z <= 0.05f || b.z <= 0.05f || c.z <= 0.05f)
            {
                continue;
            }

            var full = (b.x - a.x) * (c.y - a.y) - (c.x - a.x) * (b.y - a.y);

            if (Math.Abs(full) < 1e-7f)
            {
                continue;
            }

            // On a screen whose y runs down, the engine's clockwise fronts have a positive area.
            var isBack = full < 0;
            var x0 = (int)Math.Max(0, Math.Floor(Math.Min(a.x, Math.Min(b.x, c.x))));
            var x1 = (int)Math.Min(Width - 1, Math.Ceiling(Math.Max(a.x, Math.Max(b.x, c.x))));
            var y0 = (int)Math.Max(0, Math.Floor(Math.Min(a.y, Math.Min(b.y, c.y))));
            var y1 = (int)Math.Min(Height - 1, Math.Ceiling(Math.Max(a.y, Math.Max(b.y, c.y))));

            if (x1 < x0 || y1 < y0)
            {
                continue;
            }

            var la = Light(normal[ia]);
            var lb = Light(normal[ib]);
            var lc = Light(normal[ic]);

            for (var y = y0; y <= y1; y++)
            {
                for (var x = x0; x <= x1; x++)
                {
                    float qx = x + 0.5f, qy = y + 0.5f;
                    var w0 = ((b.x - qx) * (c.y - qy) - (c.x - qx) * (b.y - qy)) / full;
                    var w1 = ((c.x - qx) * (a.y - qy) - (a.x - qx) * (c.y - qy)) / full;
                    var w2 = 1 - w0 - w1;

                    if (w0 < 0 || w1 < 0 || w2 < 0)
                    {
                        continue;
                    }

                    // Perspective-correct weights.
                    var iz = w0 / a.z + w1 / b.z + w2 / c.z;
                    var z = 1 / iz;
                    var pz0 = w0 / a.z * z;
                    var pz1 = w1 / b.z * z;
                    var pz2 = w2 / c.z * z;
                    var at = y * Width + x;
                    // A face seen from behind loses to a front face at the same depth (two-sided leaves are fine).
                    var zz = z + (isBack ? z * 0.002f : 0);

                    if (zz >= depth[at])
                    {
                        continue;
                    }

                    depth[at] = zz;
                    drawn[at] = true;
                    var light = la * pz0 + lb * pz1 + lc * pz2;
                    float r = 0, g = 0, bl = 0;

                    if (isBack && PaintBackFaces)
                    {
                        r = 1;
                        bl = 1;
                    }
                    else if (isBack)
                    {
                        // Seen from inside (a sleeve, a hem): the cloth's own colour, in shadow.
                        r = (mesh.Colours[ia * 4] * pz0 + mesh.Colours[ib * 4] * pz1 + mesh.Colours[ic * 4] * pz2) * 0.4f;
                        g = (mesh.Colours[ia * 4 + 1] * pz0 + mesh.Colours[ib * 4 + 1] * pz1 + mesh.Colours[ic * 4 + 1] * pz2) * 0.4f;
                        bl = (mesh.Colours[ia * 4 + 2] * pz0 + mesh.Colours[ib * 4 + 2] * pz1 + mesh.Colours[ic * 4 + 2] * pz2) * 0.4f;
                    }
                    else
                    {
                        r = (mesh.Colours[ia * 4] * pz0 + mesh.Colours[ib * 4] * pz1 + mesh.Colours[ic * 4] * pz2) * light;
                        g = (mesh.Colours[ia * 4 + 1] * pz0 + mesh.Colours[ib * 4 + 1] * pz1 + mesh.Colours[ic * 4 + 1] * pz2) * light;
                        bl = (mesh.Colours[ia * 4 + 2] * pz0 + mesh.Colours[ib * 4 + 2] * pz1 + mesh.Colours[ic * 4 + 2] * pz2) * light;
                    }

                    if (FogDistance > 0)
                    {
                        var fog = Math.Min(0.75f, z / FogDistance);
                        r = r * (1 - fog) + 0.66f * fog;
                        g = g * (1 - fog) + 0.72f * fog;
                        bl = bl * (1 - fog) + 0.8f * fog;
                    }

                    colour[at * 3] = r;
                    colour[at * 3 + 1] = g;
                    colour[at * 3 + 2] = bl;
                    backAt[at] = isBack && counting;
                    counted[at] = counting;
                }
            }
        }
    }

    float Light(V3 n)
    {
        var lambert = V3.Dot(n, Sun);
        var wrapped = Math.Max(0, (lambert + 0.35f) / 1.35f);

        return 0.42f + 0.72f * wrapped + 0.12f * Math.Max(0, n.Y);
    }

    /// <summary>Copies the picture into a bigger sheet at (ox, oy), with a thin frame.</summary>
    public void Into(byte[] sheet, int sheetWidth, int ox, int oy)
    {
        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                var i = y * Width + x;
                var o = ((oy + y) * sheetWidth + ox + x) * 3;
                var edge = x == 0 || y == 0;
                sheet[o] = (byte)(Math.Clamp(edge ? 0.3f : colour[i * 3], 0, 1) * 255);
                sheet[o + 1] = (byte)(Math.Clamp(edge ? 0.3f : colour[i * 3 + 1], 0, 1) * 255);
                sheet[o + 2] = (byte)(Math.Clamp(edge ? 0.32f : colour[i * 3 + 2], 0, 1) * 255);
            }
        }
    }

    /// <summary>Writes a label in a tiny 3×5 font at (x, y) of a sheet.</summary>
    public static void Label(byte[] sheet, int sheetWidth, int x, int y, string text, int scale = 2)
    {
        const string glyphs = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789.-:/ ";
        // Each glyph: 5 rows of 3 bits.
        int[] rows =
        {
            0b010101111101101, 0b110101110101110, 0b011100100100011, 0b110101101101110, 0b111100110100111, 0b111100110100100,
            0b011100101101011, 0b101101111101101, 0b111010010010111, 0b001001001101010, 0b101101110101101, 0b100100100100111,
            0b101111111101101, 0b110101101101101, 0b010101101101010, 0b110101110100100, 0b010101101110011, 0b110101110101101,
            0b011100010001110, 0b111010010010010, 0b101101101101111, 0b101101101101010, 0b101101111111101, 0b101101010101101,
            0b101101010010010, 0b111001010100111, 0b111101101101111, 0b010110010010111, 0b110001010100111, 0b110001010001110,
            0b101101111001001, 0b111100110001110, 0b011100110101010, 0b111001010010010, 0b111101111101111, 0b111101111001110,
            0b000000000000010, 0b000000111000000, 0b000010000010000, 0b001001010100100, 0b000000000000000,
        };
        var cx = x;

        foreach (var ch in text.ToUpperInvariant())
        {
            var g = glyphs.IndexOf(ch);

            if (g < 0)
            {
                g = glyphs.Length - 1;
            }

            for (var r = 0; r < 5; r++)
            {
                for (var c = 0; c < 3; c++)
                {
                    if ((rows[g] >> (14 - (r * 3 + c)) & 1) == 0)
                    {
                        continue;
                    }

                    for (var sy = 0; sy < scale; sy++)
                    {
                        for (var sx = 0; sx < scale; sx++)
                        {
                            var px = cx + c * scale + sx;
                            var py = y + r * scale + sy;

                            if (px < 0 || py < 0 || px >= sheetWidth || (py * sheetWidth + px) * 3 + 2 >= sheet.Length)
                            {
                                continue;
                            }

                            var o = (py * sheetWidth + px) * 3;
                            sheet[o] = sheet[o + 1] = sheet[o + 2] = 245;
                        }
                    }
                }
            }

            cx += 4 * scale;
        }
    }
}
