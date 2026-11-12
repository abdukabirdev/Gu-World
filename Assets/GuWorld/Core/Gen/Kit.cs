using System;

namespace GuWorld.Core.Gen
{
    /// <summary>A point or a direction in the model's space (x right, y up, z forward).</summary>
    public struct V3
    {
        public float X, Y, Z;

        public V3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static V3 operator +(V3 a, V3 b) => new V3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static V3 operator -(V3 a, V3 b) => new V3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static V3 operator *(V3 a, float k) => new V3(a.X * k, a.Y * k, a.Z * k);
        public static V3 operator *(float k, V3 a) => new V3(a.X * k, a.Y * k, a.Z * k);
        public static V3 Cross(V3 a, V3 b) => new V3(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);
        public static float Dot(V3 a, V3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;
        public static V3 Lerp(V3 a, V3 b, float t) => a + (b - a) * t;
        public float Length => (float)Math.Sqrt(X * X + Y * Y + Z * Z);
        public V3 Normalized => Length > 1e-6f ? this * (1 / Length) : new V3(0, 1, 0);
        public static readonly V3 Up = new V3(0, 1, 0);
    }

    /// <summary>A colour of a model's part, 0–1 each.</summary>
    public struct Tint
    {
        public float R, G, B;

        public Tint(float r, float g, float b)
        {
            R = r;
            G = g;
            B = b;
        }

        public static Tint Hex(int rgb) => new Tint(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f);

        /// <summary>Lighter (above 1) or darker (below 1), kept in range.</summary>
        public Tint Times(float k) => new Tint(Math.Min(1, R * k), Math.Min(1, G * k), Math.Min(1, B * k));

        public static Tint Mix(Tint a, Tint b, float t) => new Tint(a.R + (b.R - a.R) * t, a.G + (b.G - a.G) * t, a.B + (b.B - a.B) * t);
    }

    /// <summary>
    /// A builder of models made in code: boxes, beams between two points, prisms and cylinders, discs,
    /// lofted hulls and the curved tiled roofs of the mountain's halls, each with flat faces (sharp
    /// edges) and a vertex colour. Parts are placed through a frame (an origin, a turn about the
    /// vertical, a scale) that can be pushed and popped, so a model is put together like a carpenter
    /// would: a post here, a beam across, a roof over them.
    /// </summary>
    public sealed class Kit
    {
        public readonly MeshData Mesh = new MeshData();

        V3 origin;
        float yaw;
        float cos = 1, sin;
        float scale = 1;
        readonly System.Collections.Generic.Stack<(V3, float, float)> frames = new System.Collections.Generic.Stack<(V3, float, float)>();

        public Tint Colour = new Tint(0.8f, 0.8f, 0.8f);

        /// <summary>Moves into a frame at `at` (in the present frame), turned by `degrees` about the vertical, scaled.</summary>
        public void Push(V3 at, float degrees = 0, float scaleBy = 1)
        {
            frames.Push((origin, yaw, scale));
            origin = Point(at);
            SetYaw(yaw + degrees);
            scale *= scaleBy;
        }

        public void Pop()
        {
            var (o, y, s) = frames.Pop();
            origin = o;
            SetYaw(y);
            scale = s;
        }

        void SetYaw(float degrees)
        {
            yaw = degrees;
            var radians = degrees * Math.PI / 180;
            cos = (float)Math.Cos(radians);
            sin = (float)Math.Sin(radians);
        }

        /// <summary>A point of the present frame in the model's space.</summary>
        public V3 Point(V3 p) => new V3(origin.X + (p.X * cos + p.Z * sin) * scale, origin.Y + p.Y * scale, origin.Z + (-p.X * sin + p.Z * cos) * scale);

        // ------------------------------------------------------------------ Faces

        /// <summary>A flat quad a → b → c → d (clockwise seen from its front), its normal from its corners.</summary>
        public void Quad(V3 a, V3 b, V3 c, V3 d, Tint? tint = null)
        {
            a = Point(a);
            b = Point(b);
            c = Point(c);
            d = Point(d);
            QuadWorld(a, b, c, d, tint ?? Colour);
        }

        public void Triangle(V3 a, V3 b, V3 c, Tint? tint = null)
        {
            a = Point(a);
            b = Point(b);
            c = Point(c);
            var t = tint ?? Colour;
            var n = V3.Cross(b - a, c - a).Normalized;
            var ia = Mesh.Vertex(a.X, a.Y, a.Z, 0, 0, t.R, t.G, t.B);
            var ib = Mesh.Vertex(b.X, b.Y, b.Z, 1, 0, t.R, t.G, t.B);
            var ic = Mesh.Vertex(c.X, c.Y, c.Z, 1, 1, t.R, t.G, t.B);
            Mesh.SetNormal(ia, n.X, n.Y, n.Z);
            Mesh.SetNormal(ib, n.X, n.Y, n.Z);
            Mesh.SetNormal(ic, n.X, n.Y, n.Z);
            Mesh.Triangle(ia, ib, ic);
        }

        void QuadWorld(V3 a, V3 b, V3 c, V3 d, Tint t)
        {
            var n = V3.Cross(b - a, c - a);

            if (n.Length < 1e-9f)
            {
                n = V3.Cross(c - a, d - a);
            }

            n = n.Normalized;
            var ia = Mesh.Vertex(a.X, a.Y, a.Z, 0, 0, t.R, t.G, t.B);
            var ib = Mesh.Vertex(b.X, b.Y, b.Z, 1, 0, t.R, t.G, t.B);
            var ic = Mesh.Vertex(c.X, c.Y, c.Z, 1, 1, t.R, t.G, t.B);
            var id = Mesh.Vertex(d.X, d.Y, d.Z, 0, 1, t.R, t.G, t.B);

            foreach (var v in new[] { ia, ib, ic, id })
            {
                Mesh.SetNormal(v, n.X, n.Y, n.Z);
            }

            Mesh.Quad(ia, ib, ic, id);
        }

        // ------------------------------------------------------------------ Solids

        /// <summary>A box from its centre at the foot (x, z the middle, y the bottom) with its size.</summary>
        public void Box(float x, float y, float z, float width, float height, float depth, Tint? tint = null)
        {
            var t = tint ?? Colour;
            float x0 = x - width / 2, x1 = x + width / 2, y0 = y, y1 = y + height, z0 = z - depth / 2, z1 = z + depth / 2;
            var top = t;
            var side = t.Times(0.92f);
            Quad(new V3(x0, y1, z0), new V3(x0, y1, z1), new V3(x1, y1, z1), new V3(x1, y1, z0), top);
            Quad(new V3(x0, y0, z1), new V3(x0, y0, z0), new V3(x1, y0, z0), new V3(x1, y0, z1), t.Times(0.7f));
            Quad(new V3(x0, y0, z0), new V3(x0, y1, z0), new V3(x1, y1, z0), new V3(x1, y0, z0), side);
            Quad(new V3(x1, y0, z1), new V3(x1, y1, z1), new V3(x0, y1, z1), new V3(x0, y0, z1), side);
            Quad(new V3(x0, y0, z1), new V3(x0, y1, z1), new V3(x0, y1, z0), new V3(x0, y0, z0), side);
            Quad(new V3(x1, y0, z0), new V3(x1, y1, z0), new V3(x1, y1, z1), new V3(x1, y0, z1), side);
        }

        /// <summary>
        /// A box cut into tiles of about `tile` metres along its two long sides (for a big surface lit by
        /// lights near it: the light is worked out at the corners, so a wall of one piece would be dark).
        /// </summary>
        public void Tiled(float x, float y, float z, float width, float height, float depth, float tile, Tint? tint = null)
        {
            var nx = Math.Max(1, (int)Math.Round(width / tile));
            var ny = Math.Max(1, (int)Math.Round(height / tile));
            var nz = Math.Max(1, (int)Math.Round(depth / tile));

            // Only along the dimensions that are long (a wall is thin one way).
            if (width < tile * 0.5f) nx = 1;
            if (depth < tile * 0.5f) nz = 1;

            for (var i = 0; i < nx; i++)
            {
                for (var j = 0; j < ny; j++)
                {
                    for (var l = 0; l < nz; l++)
                    {
                        Box(x - width / 2 + (i + 0.5f) * width / nx, y + j * height / ny, z - depth / 2 + (l + 0.5f) * depth / nz, width / nx, height / ny, depth / nz, tint);
                    }
                }
            }
        }

        /// <summary>A beam of a square section `thick` × `deep` from one point to another (its "up" kept as near the vertical as it can be).</summary>
        public void Beam(V3 from, V3 to, float thick, float deep, Tint? tint = null)
        {
            var t = tint ?? Colour;
            var along = (to - from).Normalized;
            var side = V3.Cross(Math.Abs(along.Y) > 0.95f ? new V3(0, 0, 1) : V3.Up, along).Normalized * (thick / 2);
            var up = V3.Cross(along, side).Normalized * (deep / 2);
            V3 C(V3 p, float s, float u) => p + side * s + up * u;
            var a0 = C(from, -1, -1); var a1 = C(from, 1, -1); var a2 = C(from, 1, 1); var a3 = C(from, -1, 1);
            var b0 = C(to, -1, -1); var b1 = C(to, 1, -1); var b2 = C(to, 1, 1); var b3 = C(to, -1, 1);
            Quad(a3, b3, b2, a2, t);
            Quad(a1, b1, b0, a0, t.Times(0.75f));
            Quad(a2, b2, b1, a1, t.Times(0.9f));
            Quad(a0, b0, b3, a3, t.Times(0.9f));
            Quad(a0, a3, a2, a1, t.Times(0.85f));
            Quad(b1, b2, b3, b0, t.Times(0.85f));
        }

        /// <summary>A prism (a round post with `sides` faces) standing at (x, y, z), tapering from `bottom` to `top` radius.</summary>
        public void Post(float x, float y, float z, float bottom, float top, float height, int sides = 8, Tint? tint = null, bool caps = true)
        {
            var t = tint ?? Colour;

            for (var i = 0; i < sides; i++)
            {
                var a0 = Angle(i, sides);
                var a1 = Angle(i + 1, sides);
                var p0 = new V3(x + (float)Math.Sin(a0) * bottom, y, z + (float)Math.Cos(a0) * bottom);
                var p1 = new V3(x + (float)Math.Sin(a1) * bottom, y, z + (float)Math.Cos(a1) * bottom);
                var q0 = new V3(x + (float)Math.Sin(a0) * top, y + height, z + (float)Math.Cos(a0) * top);
                var q1 = new V3(x + (float)Math.Sin(a1) * top, y + height, z + (float)Math.Cos(a1) * top);
                Quad(p1, q1, q0, p0, t.Times(0.85f + 0.15f * (float)Math.Cos(a0 - 0.6)));

                if (caps)
                {
                    Triangle(new V3(x, y + height, z), q0, q1, t);
                    Triangle(new V3(x, y, z), p1, p0, t.Times(0.7f));
                }
            }
        }

        static double Angle(int i, int sides) => i * 2 * Math.PI / sides;

        /// <summary>A flat ring (or a disc, with `inner` 0) lying at height y, facing up.</summary>
        public void Ring(float x, float y, float z, float inner, float outer, int sides, Tint? tint = null)
        {
            var t = tint ?? Colour;

            for (var i = 0; i < sides; i++)
            {
                var a0 = Angle(i, sides);
                var a1 = Angle(i + 1, sides);
                var o0 = new V3(x + (float)Math.Sin(a0) * outer, y, z + (float)Math.Cos(a0) * outer);
                var o1 = new V3(x + (float)Math.Sin(a1) * outer, y, z + (float)Math.Cos(a1) * outer);

                if (inner <= 0)
                {
                    Triangle(new V3(x, y, z), o0, o1, t);
                }
                else
                {
                    var i0 = new V3(x + (float)Math.Sin(a0) * inner, y, z + (float)Math.Cos(a0) * inner);
                    var i1 = new V3(x + (float)Math.Sin(a1) * inner, y, z + (float)Math.Cos(a1) * inner);
                    Quad(i0, o0, o1, i1, t);
                }
            }
        }

        /// <summary>A drum: a short round solid (a step, a platform, a wheel lying down).</summary>
        public void Drum(float x, float y, float z, float radius, float height, int sides, Tint? tint = null)
        {
            Post(x, y, z, radius, radius, height, sides, tint, true);
        }

        /// <summary>A round thing seen from the side: a wheel of `radius` and `width` turned to face along x (its axle on z).</summary>
        public void Wheel(V3 hub, float radius, float width, int spokes, Tint rim, Tint spoke)
        {
            var sides = 16;

            for (var i = 0; i < sides; i++)
            {
                var a0 = Angle(i, sides);
                var a1 = Angle(i + 1, sides);
                var p0 = hub + new V3((float)Math.Sin(a0) * radius, (float)Math.Cos(a0) * radius, 0);
                var p1 = hub + new V3((float)Math.Sin(a1) * radius, (float)Math.Cos(a1) * radius, 0);
                var q0 = hub + new V3((float)Math.Sin(a0) * radius * 0.85f, (float)Math.Cos(a0) * radius * 0.85f, 0);
                var q1 = hub + new V3((float)Math.Sin(a1) * radius * 0.85f, (float)Math.Cos(a1) * radius * 0.85f, 0);
                var w = new V3(0, 0, width / 2);
                Quad(p0 + w, p1 + w, p1 - w, p0 - w, rim);
                Quad(q0 - w, q1 - w, q1 + w, q0 + w, rim.Times(0.8f));
                Quad(p0 - w, p1 - w, q1 - w, q0 - w, rim.Times(0.9f));
                Quad(q0 + w, q1 + w, p1 + w, p0 + w, rim.Times(0.9f));
            }

            for (var i = 0; i < spokes; i++)
            {
                var a = Angle(i, spokes);
                Beam(hub, hub + new V3((float)Math.Sin(a) * radius * 0.86f, (float)Math.Cos(a) * radius * 0.86f, 0), width * 0.35f, width * 0.35f, spoke);
            }

            Push(hub);
            Beam(new V3(0, 0, -width), new V3(0, 0, width), radius * 0.22f, radius * 0.22f, spoke.Times(0.8f));
            Pop();
        }

        /// <summary>A ball made of rings (a lantern, a gourd, a pearl on a roof).</summary>
        public void Ball(V3 centre, float radius, float squash = 1, int sides = 10, int rings = 6, Tint? tint = null)
        {
            var t = tint ?? Colour;

            for (var r = 0; r < rings; r++)
            {
                var b0 = Math.PI * r / rings;
                var b1 = Math.PI * (r + 1) / rings;

                for (var i = 0; i < sides; i++)
                {
                    var a0 = Angle(i, sides);
                    var a1 = Angle(i + 1, sides);
                    V3 P(double a, double b) => centre + new V3((float)(Math.Sin(b) * Math.Sin(a)) * radius, (float)Math.Cos(b) * radius * squash, (float)(Math.Sin(b) * Math.Cos(a)) * radius);
                    Quad(P(a0, b1), P(a1, b1), P(a1, b0), P(a0, b0), t.Times(0.8f + 0.2f * (float)Math.Cos(b0)));
                }
            }
        }

        /// <summary>A strip of quads through two rows of points (a hull's side, a canopy, a curved roof's slope).</summary>
        public void Sheet(V3[] near, V3[] far, Tint? tint = null, bool twoSided = false)
        {
            var t = tint ?? Colour;

            for (var i = 0; i + 1 < near.Length; i++)
            {
                Quad(near[i], far[i], far[i + 1], near[i + 1], t);

                if (twoSided)
                {
                    Quad(near[i + 1], far[i + 1], far[i], near[i], t.Times(0.8f));
                }
            }
        }

        // ------------------------------------------------------------------ Roofs

        /// <summary>
        /// The curved roof of a hall, its ridge along x: two slopes that sag in the middle and lift at
        /// the eaves (the more `lift`, the more the corners turn up, the hall's style), grey tiles in
        /// rows, a ridge with its two upturned ends and, if `hip`, the hipped ends too. The roof sits on
        /// (0, eave height, 0): `width` along the ridge, `depth` across, `rise` from eave to ridge, the
        /// eaves reaching `overhang` past the walls.
        /// </summary>
        public void Roof(float width, float depth, float rise, float overhang, float lift, bool hip, Tint tiles, Tint ridge)
        {
            var w = width / 2 + overhang;
            var d = depth / 2 + overhang;
            var rows = 6;
            var columns = 8;
            var ridgeHalf = hip ? Math.Max(0.2f, w - d) : w;

            // A point of the slope: s from the eave (0) to the ridge (1), u from one end (−1) to the other (1).
            V3 Slope(float s, float u, float side)
            {
                // The slope sags (concave) and its eave lifts toward the corners.
                var sag = (float)Math.Pow(s, 1.6);
                var y = rise * sag;
                var corner = (float)Math.Pow(Math.Abs(u), 3) * lift * (1 - s);
                var z = side * (d - (d - 0.05f) * s);
                var half = ridgeHalf + (w - ridgeHalf) * (1 - s);

                return new V3(u * half, y + corner, z);
            }

            for (var side = -1; side <= 1; side += 2)
            {
                for (var r = 0; r < rows; r++)
                {
                    float s0 = r / (float)rows, s1 = (r + 1) / (float)rows;

                    for (var c = 0; c < columns; c++)
                    {
                        float u0 = -1 + 2f * c / columns, u1 = -1 + 2f * (c + 1) / columns;
                        // Rows of tiles: each row a shade from the next.
                        var shade = tiles.Times(0.88f + 0.12f * ((r + c) % 2));
                        var a = Slope(s0, u0, side);
                        var b = Slope(s1, u0, side);
                        var cc = Slope(s1, u1, side);
                        var dd = Slope(s0, u1, side);

                        if (side > 0)
                        {
                            Quad(a, b, cc, dd, shade);
                            Quad(dd, cc, b, a, shade.Times(0.6f));
                        }
                        else
                        {
                            Quad(dd, cc, b, a, shade);
                            Quad(a, b, cc, dd, shade.Times(0.6f));
                        }
                    }
                }
            }

            // The hipped ends: triangles of slope from the eave corners to the ridge's ends.
            if (hip)
            {
                for (var end = -1; end <= 1; end += 2)
                {
                    for (var r = 0; r < rows; r++)
                    {
                        float s0 = r / (float)rows, s1 = (r + 1) / (float)rows;
                        var a = Slope(s0, end, -1);
                        var b = Slope(s1, end, -1);
                        var c = Slope(s1, end, 1);
                        var dd = Slope(s0, end, 1);
                        var shade = tiles.Times(0.85f + 0.1f * (r % 2));

                        if (end > 0)
                        {
                            Quad(a, b, c, dd, shade);
                            Quad(dd, c, b, a, shade.Times(0.6f));
                        }
                        else
                        {
                            Quad(dd, c, b, a, shade);
                            Quad(a, b, c, dd, shade.Times(0.6f));
                        }
                    }
                }
            }
            else
            {
                // A gable's triangle of wall under each end.
                for (var end = -1; end <= 1; end += 2)
                {
                    var x = end * (width / 2);
                    var tip = new V3(x, rise * 0.95f, 0);
                    var l = new V3(x, 0, -depth / 2);
                    var rr = new V3(x, 0, depth / 2);

                    if (end > 0)
                    {
                        Triangle(l, tip, rr, ridge.Times(1.3f));
                    }
                    else
                    {
                        Triangle(rr, tip, l, ridge.Times(1.3f));
                    }
                }
            }

            // The ridge with its ends turned up like a swallow's tail.
            var top = rise + 0.05f;
            Beam(new V3(-ridgeHalf - 0.1f, top, 0), new V3(ridgeHalf + 0.1f, top, 0), 0.32f, 0.28f, ridge);

            for (var end = -1; end <= 1; end += 2)
            {
                var foot = new V3(end * (ridgeHalf + 0.1f), top, 0);
                var tail = new V3(end * (ridgeHalf + 0.1f + rise * 0.22f), top + rise * 0.28f, 0);
                Beam(foot, tail, 0.22f, 0.22f, ridge);
            }

            // The eaves' edge: a dark board under the lifted lip.
            for (var side = -1; side <= 1; side += 2)
            {
                for (var c = 0; c < columns; c++)
                {
                    float u0 = -1 + 2f * c / columns, u1 = -1 + 2f * (c + 1) / columns;
                    var a = Slope(0, u0, side);
                    var b = Slope(0, u1, side);
                    Beam(a + new V3(0, -0.06f, 0), b + new V3(0, -0.06f, 0), 0.12f, 0.12f, ridge.Times(0.7f));
                }
            }
        }

        /// <summary>A cap of tiles along a wall's top from one point to another: a small two-sided roof.</summary>
        public void Coping(V3 from, V3 to, float width, Tint tiles)
        {
            var along = to - from;
            var length = along.Length;
            var mid = (from + to) * 0.5f;
            var degrees = (float)(Math.Atan2(-along.Z, along.X) * 180 / Math.PI);
            Push(mid, degrees);
            Roof(length, width * 0.4f, width * 0.35f, width * 0.3f, 0.08f, false, tiles, tiles.Times(0.8f));
            Pop();
        }
    }
}
