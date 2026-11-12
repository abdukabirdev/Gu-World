using System;
using System.Collections.Generic;

namespace GuWorld.Core.Gen.Flora
{
    /// <summary>
    /// The pieces every generated plant (and body) is swept from: tubes along a path of points
    /// with a radius at each, lumpy blobs of leaves, strips, and the turning of vectors. Winding is
    /// the engine's (a face's front is the side its corners run clockwise from); normals are the
    /// smooth ones of MeshData.ComputeNormals unless a piece sets its own.
    /// </summary>
    public static class Build
    {
        public const float Tau = (float)(Math.PI * 2);

        /// <summary>`v` turned about the unit axis `k` by `radians` (Rodrigues).</summary>
        public static V3 Rotate(V3 v, V3 k, float radians)
        {
            var c = (float)Math.Cos(radians);
            var s = (float)Math.Sin(radians);

            return v * c + V3.Cross(k, v) * s + k * (V3.Dot(k, v) * (1 - c));
        }

        /// <summary>Some unit vector square to `d`.</summary>
        public static V3 Perpendicular(V3 d)
        {
            var other = Math.Abs(d.Y) < 0.9f ? V3.Up : new V3(1, 0, 0);

            return V3.Cross(d, other).Normalized;
        }

        public static float Clamp01(float x) => x < 0 ? 0 : x > 1 ? 1 : x;

        public static float Lerp(float a, float b, float t) => a + (b - a) * t;

        /// <summary>
        /// A tube swept along `points` with a radius at each, `sides` round, its ring frames carried
        /// along the path without twisting. Colour per ring by `colour(ring index, angle 0-1, point,
        /// outward normal)`. The end is closed with a point (or left open); the start is left open (it
        /// sits inside its parent or the ground).
        /// </summary>
        public static void Tube(MeshData mesh, IList<V3> points, IList<float> radii, int sides, Func<int, float, V3, V3, Tint> colour, bool closeEnd = true, float vScale = 1)
        {
            var count = points.Count;

            if (count < 2 || sides < 3)
            {
                return;
            }

            var start = mesh.VertexCount;
            var axis = (points[1] - points[0]).Normalized;
            var n1 = Perpendicular(axis);
            var travelled = 0f;

            for (var i = 0; i < count; i++)
            {
                // The axis here: the mean of the segments on each side.
                var next = i < count - 1 ? (points[i + 1] - points[i]).Normalized : (points[i] - points[i - 1]).Normalized;
                var before = i > 0 ? (points[i] - points[i - 1]).Normalized : next;
                var here = (next + before).Normalized;

                // Carry the frame: take out of n1 what now lies along the axis.
                n1 = (n1 - here * V3.Dot(n1, here)).Normalized;
                var n2 = V3.Cross(n1, here);

                if (i > 0)
                {
                    travelled += (points[i] - points[i - 1]).Length;
                }

                for (var s = 0; s <= sides; s++)
                {
                    var a = s / (float)sides * Tau;
                    var outward = n1 * (float)Math.Cos(a) + n2 * (float)Math.Sin(a);
                    var p = points[i] + outward * radii[i];
                    var c = colour(i, s / (float)sides, p, outward);
                    var v = mesh.Vertex(p.X, p.Y, p.Z, s / (float)sides, travelled * vScale, c.R, c.G, c.B);
                    mesh.SetNormal(v, outward.X, outward.Y, outward.Z);
                }
            }

            var ring = sides + 1;

            for (var i = 0; i < count - 1; i++)
            {
                for (var s = 0; s < sides; s++)
                {
                    var a = start + i * ring + s;
                    var b = start + (i + 1) * ring + s;
                    var c = start + (i + 1) * ring + s + 1;
                    var d = start + i * ring + s + 1;
                    mesh.Triangle(a, b, c);
                    mesh.Triangle(a, c, d);
                }
            }

            if (closeEnd && radii[count - 1] > 1e-4f)
            {
                var last = points[count - 1];
                var dir = (points[count - 1] - points[count - 2]).Normalized;
                var tip = last + dir * radii[count - 1] * 0.8f;
                var c = colour(count - 1, 0, tip, dir);
                var t = mesh.Vertex(tip.X, tip.Y, tip.Z, 0.5f, (travelled + radii[count - 1]) * vScale, c.R, c.G, c.B);
                mesh.SetNormal(t, dir.X, dir.Y, dir.Z);
                var baseIndex = start + (count - 1) * ring;

                for (var s = 0; s < sides; s++)
                {
                    mesh.Triangle(baseIndex + s, t, baseIndex + s + 1);
                }
            }
        }

        static readonly Dictionary<int, MeshData> spheres = new Dictionary<int, MeshData>();

        /// <summary>A unit icosphere, made once per detail (shared, never changed).</summary>
        public static MeshData Sphere(int detail)
        {
            lock (spheres)
            {
                if (!spheres.TryGetValue(detail, out var sphere))
                {
                    sphere = Shapes.Icosphere(detail);
                    spheres[detail] = sphere;
                }

                return sphere;
            }
        }

        /// <summary>
        /// A lumpy blob (an icosphere pushed in and out by noise), its three half-axes along `ax`,
        /// `ay`, `az` (any lengths), at `centre`. Colour per vertex by `colour(point, outward)`.
        /// </summary>
        public static void Blob(MeshData mesh, V3 centre, V3 ax, V3 ay, V3 az, int detail, Noise noise, float lumpiness, Func<V3, V3, Tint> colour)
        {
            var sphere = Sphere(detail);
            var start = mesh.VertexCount;
            var lx = ax.Length;
            var ly = ay.Length;
            var lz = az.Length;
            var ux = ax.Normalized;
            var uy = ay.Normalized;
            var uz = az.Normalized;
            var o = centre.X * 0.37f + centre.Y * 0.11f + centre.Z * 0.23f;

            for (var i = 0; i < sphere.VertexCount; i++)
            {
                float x = sphere.Positions[i * 3], y = sphere.Positions[i * 3 + 1], z = sphere.Positions[i * 3 + 2];
                var lump = 1 + lumpiness * noise.Perlin(x * 2.1f + o, z * 2.1f + y * 1.7f - o);
                var p = centre + ux * (x * lx * lump) + uy * (y * ly * lump) + uz * (z * lz * lump);
                // The normal of an ellipsoid: the sphere's, divided by the axes' lengths.
                var n = (ux * (x / Math.Max(lx, 1e-4f)) + uy * (y / Math.Max(ly, 1e-4f)) + uz * (z / Math.Max(lz, 1e-4f))).Normalized;
                var c = colour(p, n);
                var v = mesh.Vertex(p.X, p.Y, p.Z, x * 0.5f + 0.5f, z * 0.5f + 0.5f, c.R, c.G, c.B);
                mesh.SetNormal(v, n.X, n.Y, n.Z);
            }

            // The axes may be left-handed (a mirrored blob): then the faces turn round.
            var flip = V3.Dot(V3.Cross(ux, uy), uz) < 0;

            for (var t = 0; t < sphere.Triangles.Count; t += 3)
            {
                if (flip)
                {
                    mesh.Triangle(start + sphere.Triangles[t], start + sphere.Triangles[t + 2], start + sphere.Triangles[t + 1]);
                }
                else
                {
                    mesh.Triangle(start + sphere.Triangles[t], start + sphere.Triangles[t + 1], start + sphere.Triangles[t + 2]);
                }
            }
        }

        /// <summary>
        /// A ribbon along `points`, `widths` across, lying square to `side` (seen from both sides:
        /// each face is laid twice, back to back). For hanging willow strands, bamboo leaves, grass.
        /// </summary>
        public static void Ribbon(MeshData mesh, IList<V3> points, IList<float> widths, V3 side, Func<int, Tint> colour)
        {
            var count = points.Count;

            if (count < 2)
            {
                return;
            }

            for (var face = 0; face < 2; face++)
            {
                var start = mesh.VertexCount;

                for (var i = 0; i < count; i++)
                {
                    var along = i < count - 1 ? points[i + 1] - points[i] : points[i] - points[i - 1];
                    var normal = V3.Cross(along, side).Normalized * (face == 0 ? 1 : -1);
                    var c = colour(i);
                    var half = side * (widths[i] * 0.5f);
                    var a = points[i] - half;
                    var b = points[i] + half;
                    var va = mesh.Vertex(a.X, a.Y, a.Z, 0, i / (float)(count - 1), c.R, c.G, c.B);
                    var vb = mesh.Vertex(b.X, b.Y, b.Z, 1, i / (float)(count - 1), c.R, c.G, c.B);
                    mesh.SetNormal(va, normal.X, normal.Y, normal.Z);
                    mesh.SetNormal(vb, normal.X, normal.Y, normal.Z);
                }

                for (var i = 0; i < count - 1; i++)
                {
                    var a = start + i * 2;

                    if (face == 0)
                    {
                        mesh.Triangle(a, a + 2, a + 3);
                        mesh.Triangle(a, a + 3, a + 1);
                    }
                    else
                    {
                        mesh.Triangle(a, a + 3, a + 2);
                        mesh.Triangle(a, a + 1, a + 3);
                    }
                }
            }
        }

        /// <summary>Appends one mesh to another, moved by `offset`.</summary>
        public static void Append(MeshData into, MeshData from, V3 offset)
        {
            var start = into.VertexCount;

            for (var i = 0; i < from.VertexCount; i++)
            {
                var v = into.Vertex(from.Positions[i * 3] + offset.X, from.Positions[i * 3 + 1] + offset.Y, from.Positions[i * 3 + 2] + offset.Z,
                    from.Uvs[i * 2], from.Uvs[i * 2 + 1], from.Colours[i * 4], from.Colours[i * 4 + 1], from.Colours[i * 4 + 2], from.Colours[i * 4 + 3]);
                into.SetNormal(v, from.Normals[i * 3], from.Normals[i * 3 + 1], from.Normals[i * 3 + 2]);
            }

            foreach (var t in from.Triangles)
            {
                into.Triangles.Add(start + t);
            }
        }

        public static Tint Shade(Tint c, float k) => new Tint(Math.Min(1, Math.Max(0, c.R * k)), Math.Min(1, Math.Max(0, c.G * k)), Math.Min(1, Math.Max(0, c.B * k)));
    }
}
