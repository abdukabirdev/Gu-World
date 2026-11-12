using System;
using System.Collections.Generic;

namespace GuWorld.Core.Gen
{
    /// <summary>A mesh as plain arrays (the engine turns it into its own): positions, normals, texture coordinates, colours, triangles.</summary>
    public sealed class MeshData
    {
        public readonly List<float> Positions = new List<float>();
        public readonly List<float> Normals = new List<float>();
        public readonly List<float> Uvs = new List<float>();
        public readonly List<float> Colours = new List<float>();
        public readonly List<int> Triangles = new List<int>();

        public int VertexCount => Positions.Count / 3;
        public int TriangleCount => Triangles.Count / 3;

        public int Vertex(float x, float y, float z, float u = 0, float v = 0, float r = 1, float g = 1, float b = 1, float a = 1)
        {
            Positions.Add(x);
            Positions.Add(y);
            Positions.Add(z);
            Normals.Add(0);
            Normals.Add(1);
            Normals.Add(0);
            Uvs.Add(u);
            Uvs.Add(v);
            Colours.Add(r);
            Colours.Add(g);
            Colours.Add(b);
            Colours.Add(a);

            return VertexCount - 1;
        }

        public void Triangle(int a, int b, int c)
        {
            Triangles.Add(a);
            Triangles.Add(b);
            Triangles.Add(c);
        }

        public void Quad(int a, int b, int c, int d)
        {
            Triangle(a, b, c);
            Triangle(a, c, d);
        }

        public void SetNormal(int vertex, float x, float y, float z)
        {
            Normals[vertex * 3] = x;
            Normals[vertex * 3 + 1] = y;
            Normals[vertex * 3 + 2] = z;
        }

        /// <summary>Smooth normals from the triangles (each vertex's is the sum of its faces').</summary>
        public void ComputeNormals()
        {
            var n = new float[Positions.Count];

            for (var t = 0; t < Triangles.Count; t += 3)
            {
                int a = Triangles[t] * 3, b = Triangles[t + 1] * 3, c = Triangles[t + 2] * 3;
                float ux = Positions[b] - Positions[a], uy = Positions[b + 1] - Positions[a + 1], uz = Positions[b + 2] - Positions[a + 2];
                float vx = Positions[c] - Positions[a], vy = Positions[c + 1] - Positions[a + 1], vz = Positions[c + 2] - Positions[a + 2];
                // The engine's front is the side the vertices run clockwise from; its normal is u × v.
                var nx = uy * vz - uz * vy;
                var ny = uz * vx - ux * vz;
                var nz = ux * vy - uy * vx;

                foreach (var i in new[] { a, b, c })
                {
                    n[i] += nx;
                    n[i + 1] += ny;
                    n[i + 2] += nz;
                }
            }

            for (var i = 0; i < n.Length; i += 3)
            {
                var length = (float)Math.Sqrt(n[i] * n[i] + n[i + 1] * n[i + 1] + n[i + 2] * n[i + 2]);

                if (length > 1e-6f)
                {
                    Normals[i] = n[i] / length;
                    Normals[i + 1] = n[i + 1] / length;
                    Normals[i + 2] = n[i + 2] / length;
                }
            }
        }

        public void Bounds(out float minX, out float minY, out float minZ, out float maxX, out float maxY, out float maxZ)
        {
            minX = minY = minZ = float.MaxValue;
            maxX = maxY = maxZ = float.MinValue;

            for (var i = 0; i < Positions.Count; i += 3)
            {
                minX = Math.Min(minX, Positions[i]);
                minY = Math.Min(minY, Positions[i + 1]);
                minZ = Math.Min(minZ, Positions[i + 2]);
                maxX = Math.Max(maxX, Positions[i]);
                maxY = Math.Max(maxY, Positions[i + 1]);
                maxZ = Math.Max(maxZ, Positions[i + 2]);
            }
        }
    }

    /// <summary>
    /// The shapes made in code for the world: boulders, shrubs, reeds, bridges, the water's
    /// surfaces and falls. Every shape is the same for the same seed. Positions are local, Y up,
    /// metres; the winding is the engine's (clockwise seen from the front).
    /// </summary>
    public static class Shapes
    {
        // ------------------------------------------------------------------ Boulders

        /// <summary>A boulder about a metre across: a sphere pushed in and out by noise, flattened at the base, with a few flat facets cut like weathered rock.</summary>
        public static MeshData Boulder(int seed, int detail = 2)
        {
            var noise = new Noise(seed);
            var mesh = Icosphere(detail);
            var rng = new Rng(seed);
            var squash = rng.Range(0.55f, 0.85f);
            var stretch = rng.Range(0.85f, 1.25f);
            var cuts = new List<(float x, float y, float z, float d)>();

            for (var i = 0; i < 4; i++)
            {
                var dir = RandomDirection(ref rng);
                cuts.Add((dir.x, dir.y, dir.z, rng.Range(0.55f, 0.8f)));
            }

            for (var i = 0; i < mesh.Positions.Count; i += 3)
            {
                float x = mesh.Positions[i], y = mesh.Positions[i + 1], z = mesh.Positions[i + 2];
                var lump = 1 + 0.22f * noise.Fbm(x * 1.7f + 3, z * 1.7f + y * 1.3f, 3) + 0.06f * noise.Perlin(x * 6, z * 6 + y * 5);
                x *= lump * stretch;
                y *= lump * squash;
                z *= lump;

                // Facets: a plane cuts the shape where it pokes out past it.
                foreach (var (cx, cy, cz, d) in cuts)
                {
                    var along = x * cx + y * cy + z * cz;

                    if (along > d)
                    {
                        var push = along - d;
                        x -= cx * push;
                        y -= cy * push;
                        z -= cz * push;
                    }
                }

                // Sunk into the ground a little, flat underneath.
                y = Math.Max(y, -0.18f * squash);
                mesh.Positions[i] = x * 0.5f;
                mesh.Positions[i + 1] = y * 0.5f + 0.08f;
                mesh.Positions[i + 2] = z * 0.5f;
                // Moss on top: the colour's green rises with the facing up.
                var top = Noise.Clamp01(y / squash * 1.4f);
                var vertex = i / 3;
                mesh.Colours[vertex * 4] = 0.62f - 0.18f * top;
                mesh.Colours[vertex * 4 + 1] = 0.6f + 0.06f * top;
                mesh.Colours[vertex * 4 + 2] = 0.56f - 0.2f * top;
            }

            mesh.ComputeNormals();

            return mesh;
        }

        static (float x, float y, float z) RandomDirection(ref Rng rng)
        {
            var y = rng.Range(-0.3f, 1f);
            var angle = rng.Range(0f, (float)Math.PI * 2);
            var r = (float)Math.Sqrt(Math.Max(0, 1 - y * y));

            return ((float)Math.Cos(angle) * r, y, (float)Math.Sin(angle) * r);
        }

        /// <summary>A unit sphere of triangles, subdivided `detail` times from an icosahedron.</summary>
        public static MeshData Icosphere(int detail)
        {
            var t = (1 + (float)Math.Sqrt(5)) / 2;
            var points = new List<(float x, float y, float z)>
            {
                (-1, t, 0), (1, t, 0), (-1, -t, 0), (1, -t, 0), (0, -1, t), (0, 1, t), (0, -1, -t), (0, 1, -t), (t, 0, -1), (t, 0, 1), (-t, 0, -1), (-t, 0, 1),
            };
            var faces = new List<(int a, int b, int c)>
            {
                (0, 11, 5), (0, 5, 1), (0, 1, 7), (0, 7, 10), (0, 10, 11), (1, 5, 9), (5, 11, 4), (11, 10, 2), (10, 7, 6), (7, 1, 8),
                (3, 9, 4), (3, 4, 2), (3, 2, 6), (3, 6, 8), (3, 8, 9), (4, 9, 5), (2, 4, 11), (6, 2, 10), (8, 6, 7), (9, 8, 1),
            };

            for (var i = 0; i < points.Count; i++)
            {
                points[i] = Unit(points[i]);
            }

            for (var level = 0; level < detail; level++)
            {
                var middles = new Dictionary<long, int>();
                var next = new List<(int, int, int)>();

                int Middle(int a, int b)
                {
                    var key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;

                    if (middles.TryGetValue(key, out var index))
                    {
                        return index;
                    }

                    var pa = points[a];
                    var pb = points[b];
                    points.Add(Unit(((pa.x + pb.x) / 2, (pa.y + pb.y) / 2, (pa.z + pb.z) / 2)));
                    middles[key] = points.Count - 1;

                    return points.Count - 1;
                }

                foreach (var (a, b, c) in faces)
                {
                    var ab = Middle(a, b);
                    var bc = Middle(b, c);
                    var ca = Middle(c, a);
                    next.Add((a, ab, ca));
                    next.Add((b, bc, ab));
                    next.Add((c, ca, bc));
                    next.Add((ab, bc, ca));
                }

                faces = next;
            }

            var mesh = new MeshData();

            foreach (var (x, y, z) in points)
            {
                mesh.Vertex(x, y, z, x * 0.5f + 0.5f, z * 0.5f + 0.5f);
            }

            foreach (var (a, b, c) in faces)
            {
                // Seen from outside in the engine's left-handed axes these run clockwise: their front is outward.
                mesh.Triangle(a, b, c);
            }

            mesh.ComputeNormals();

            return mesh;
        }

        static (float, float, float) Unit((float x, float y, float z) p)
        {
            var length = (float)Math.Sqrt(p.x * p.x + p.y * p.y + p.z * p.z);

            return (p.x / length, p.y / length, p.z / length);
        }

        // ------------------------------------------------------------------ Plants

        /// <summary>A shrub about a metre high: several lumpy blobs of leaves round a centre, darker inside.</summary>
        public static MeshData Shrub(int seed)
        {
            var rng = new Rng(seed);
            var mesh = new MeshData();
            var blobs = rng.Range(3, 6);
            var hue = rng.Range(-0.05f, 0.05f);

            for (var b = 0; b < blobs; b++)
            {
                var angle = b * Math.PI * 2 / blobs + rng.Range(-0.4f, 0.4f);
                var far = b == 0 ? 0 : rng.Range(0.18f, 0.38f);
                var cx = (float)Math.Cos(angle) * far;
                var cz = (float)Math.Sin(angle) * far;
                var size = rng.Range(0.32f, 0.5f) * (b == 0 ? 1.25f : 1);
                var cy = size * 0.8f + rng.Range(0f, 0.15f);
                var sphere = Icosphere(1);
                var noise = new Noise(seed + b);
                var start = mesh.VertexCount;

                for (var i = 0; i < sphere.VertexCount; i++)
                {
                    float x = sphere.Positions[i * 3], y = sphere.Positions[i * 3 + 1], z = sphere.Positions[i * 3 + 2];
                    var lump = 1 + 0.25f * noise.Perlin(x * 2.3f + b, z * 2.3f + y * 2);
                    var shade = 0.55f + 0.45f * Noise.Clamp01(y * 0.5f + 0.5f);
                    mesh.Vertex(cx + x * size * lump, cy + y * size * lump * 0.85f, cz + z * size * lump, 0.5f, 0.5f,
                        (0.24f + hue) * shade, (0.38f + hue * 0.5f) * shade, 0.16f * shade);
                }

                for (var t = 0; t < sphere.Triangles.Count; t += 3)
                {
                    mesh.Triangle(start + sphere.Triangles[t], start + sphere.Triangles[t + 1], start + sphere.Triangles[t + 2]);
                }
            }

            mesh.ComputeNormals();

            return mesh;
        }

        /// <summary>A stand of reeds: thin tapering blades leaning out from a clump, each a pair of crossed strips.</summary>
        public static MeshData Reeds(int seed)
        {
            var rng = new Rng(seed);
            var mesh = new MeshData();
            var blades = rng.Range(9, 16);

            for (var i = 0; i < blades; i++)
            {
                var angle = rng.Range(0f, (float)Math.PI * 2);
                var far = rng.Range(0f, 0.35f);
                var bx = (float)Math.Cos(angle) * far;
                var bz = (float)Math.Sin(angle) * far;
                var height = rng.Range(0.9f, 1.8f);
                var lean = rng.Range(0.05f, 0.35f);
                var lx = (float)Math.Cos(angle) * lean;
                var lz = (float)Math.Sin(angle) * lean;
                var width = rng.Range(0.03f, 0.06f);
                var green = rng.Range(0.35f, 0.55f);

                for (var cross = 0; cross < 2; cross++)
                {
                    var turn = angle + cross * Math.PI / 2;
                    var sx = (float)Math.Cos(turn) * width;
                    var sz = (float)Math.Sin(turn) * width;
                    var a = mesh.Vertex(bx - sx, 0, bz - sz, 0, 0, 0.3f, green * 0.8f, 0.15f);
                    var b = mesh.Vertex(bx + sx, 0, bz + sz, 1, 0, 0.3f, green * 0.8f, 0.15f);
                    var c = mesh.Vertex(bx + lx + sx * 0.3f, height, bz + lz + sz * 0.3f, 1, 1, 0.55f, green, 0.25f);
                    var d = mesh.Vertex(bx + lx - sx * 0.3f, height, bz + lz - sz * 0.3f, 0, 1, 0.55f, green, 0.25f);
                    // Both faces: a blade is seen from either side.
                    mesh.Quad(a, d, c, b);
                    mesh.Quad(a, b, c, d);
                }
            }

            mesh.ComputeNormals();

            return mesh;
        }

        // ------------------------------------------------------------------ Bridges

        /// <summary>
        /// A plank bridge along +Z, `length` long and `width` wide, its deck at Y 0: planks across
        /// with small gaps, two beams under them, posts and rails on both sides, and two piers down
        /// to `drop` below. Colours: weathered wood.
        /// </summary>
        public static MeshData Bridge(float length, float width, float drop, int seed)
        {
            var rng = new Rng(seed);
            var mesh = new MeshData();
            var half = width * 0.5f;
            var planks = Math.Max(2, (int)(length / 0.42f));
            var plank = length / planks;

            for (var i = 0; i < planks; i++)
            {
                var z0 = -length * 0.5f + i * plank + 0.02f;
                var shade = rng.Range(0.75f, 1f);
                var sag = rng.Range(-0.015f, 0.015f);
                Block(mesh, -half - rng.Range(0f, 0.08f), -0.09f + sag, z0, half + rng.Range(0f, 0.08f), sag, z0 + plank - 0.04f, 0.46f * shade, 0.34f * shade, 0.22f * shade);
            }

            foreach (var side in new[] { -1, 1 })
            {
                var x = side * (half - 0.25f);
                // The beam under the planks.
                Block(mesh, x - 0.12f, -0.38f, -length * 0.5f, x + 0.12f, -0.09f, length * 0.5f, 0.32f, 0.23f, 0.15f);
                // Posts and the rail.
                var posts = Math.Max(2, (int)(length / 2.4f) + 1);

                for (var p = 0; p < posts; p++)
                {
                    var z = -length * 0.5f + p * length / (posts - 1);
                    var px = side * (half - 0.05f);
                    Block(mesh, px - 0.07f, 0, z - 0.07f, px + 0.07f, 1.05f, z + 0.07f, 0.36f, 0.26f, 0.17f);
                }

                var rx = side * (half - 0.05f);
                Block(mesh, rx - 0.05f, 0.95f, -length * 0.5f, rx + 0.05f, 1.05f, length * 0.5f, 0.42f, 0.31f, 0.2f);
                Block(mesh, rx - 0.04f, 0.5f, -length * 0.5f, rx + 0.04f, 0.56f, length * 0.5f, 0.38f, 0.28f, 0.18f);
            }

            // Piers of stone at each end, down into the bank.
            foreach (var end in new[] { -1, 1 })
            {
                var z = end * (length * 0.5f - 0.6f);
                Block(mesh, -half - 0.2f, -drop, z - 0.6f, half + 0.2f, -0.38f, z + 0.6f, 0.5f, 0.49f, 0.46f);
            }

            mesh.ComputeNormals();

            return mesh;
        }

        /// <summary>An axis-aligned block with its own flat faces (separate vertices per face, so its edges stay sharp).</summary>
        public static void Block(MeshData mesh, float x0, float y0, float z0, float x1, float y1, float z1, float r, float g, float b)
        {
            void Face(float ax, float ay, float az, float bx, float by, float bz, float cx, float cy, float cz, float dx, float dy, float dz, float nx, float ny, float nz)
            {
                var a = mesh.Vertex(ax, ay, az, 0, 0, r, g, b);
                var bb = mesh.Vertex(bx, by, bz, 1, 0, r, g, b);
                var c = mesh.Vertex(cx, cy, cz, 1, 1, r, g, b);
                var d = mesh.Vertex(dx, dy, dz, 0, 1, r, g, b);

                foreach (var v in new[] { a, bb, c, d })
                {
                    mesh.SetNormal(v, nx, ny, nz);
                }

                mesh.Quad(a, bb, c, d);
            }

            Face(x0, y1, z0, x0, y1, z1, x1, y1, z1, x1, y1, z0, 0, 1, 0);
            Face(x0, y0, z1, x0, y0, z0, x1, y0, z0, x1, y0, z1, 0, -1, 0);
            Face(x0, y0, z0, x0, y1, z0, x1, y1, z0, x1, y0, z0, 0, 0, -1);
            Face(x1, y0, z1, x1, y1, z1, x0, y1, z1, x0, y0, z1, 0, 0, 1);
            Face(x0, y0, z1, x0, y1, z1, x0, y1, z0, x0, y0, z0, -1, 0, 0);
            Face(x1, y0, z0, x1, y1, z0, x1, y1, z1, x1, y0, z1, 1, 0, 0);
        }

        // ------------------------------------------------------------------ Water

        /// <summary>
        /// A river's surface from sample `from` to `to` (exclusive): a ribbon at the water's level, as
        /// wide as the bed plus the banks' wet edge, its texture running along the flow (u across,
        /// v metres along / width), its colour's alpha the flow's speed (steeper is faster).
        /// </summary>
        public static MeshData River(IList<Vec2> points, IList<float> levels, IList<float> widths, int from, int to)
        {
            // Five columns across: the water's flow (rg), how far from the bank (b) and how fast (a) ride in the vertex colour
            // for the water's shader; the edges reach into the banks, where the shader thins the water out by depth.
            const int across = 5;
            var mesh = new MeshData();
            var along = 0f;
            to = Math.Min(to, points.Count);

            for (var i = from; i < to; i++)
            {
                var forward = (points[Math.Min(i + 1, points.Count - 1)] - points[Math.Max(i - 1, 0)]).Normalized;
                var half = widths[i] * 0.5f + 2.5f;

                if (i > from)
                {
                    along += Vec2.Distance(points[i], points[i - 1]);
                }

                var fall = i + 1 < levels.Count ? Math.Max(0, levels[i] - levels[i + 1]) : 0;
                var speed = Noise.Clamp01(0.25f + fall * 0.6f);

                for (var c = 0; c < across; c++)
                {
                    var k = c / (float)(across - 1);
                    var offset = forward.Right * ((k * 2 - 1) * half);
                    var middle = 1 - Math.Abs(k * 2 - 1);
                    mesh.Vertex(points[i].X + offset.X, levels[i], points[i].Z + offset.Z, k, along / 8f, forward.X * 0.5f + 0.5f, forward.Z * 0.5f + 0.5f, middle, speed);
                }

                if (i > from)
                {
                    var row = (i - from) * across;

                    for (var c = 0; c < across - 1; c++)
                    {
                        // Clockwise seen from above.
                        mesh.Quad(row - across + c, row + c, row + c + 1, row - across + c + 1);
                    }
                }
            }

            for (var v = 0; v < mesh.VertexCount; v++)
            {
                mesh.SetNormal(v, 0, 1, 0);
            }

            return mesh;
        }

        /// <summary>A waterfall's sheet from the lip down to the foot: it leaves the lip level, bends over and falls, a little apart from the rock.</summary>
        public static MeshData Fall(Vec2 lip, Vec2 foot, float top, float bottom, float width)
        {
            var mesh = new MeshData();
            var forward = (foot - lip).Normalized;
            var side = forward.Right * width * 0.5f;
            var drop = top - bottom;
            const int rows = 10;

            for (var r = 0; r <= rows; r++)
            {
                var t = r / (float)rows;
                // Out from the lip in a curve, then straight down.
                var outward = Math.Min(1.6f, 0.4f + t * 3) + t * Vec2.Distance(lip, foot) * 0.6f;
                var centre = lip + forward * outward;
                var y = top - drop * t * t * (3 - 2 * t) * 0.15f - drop * t * 0.85f;
                var a = mesh.Vertex(centre.X - side.X, y, centre.Z - side.Z, 0, t * drop / 6, 1, 1, 1, 1);
                var b = mesh.Vertex(centre.X + side.X, y, centre.Z + side.Z, 1, t * drop / 6, 1, 1, 1, 1);

                if (r > 0)
                {
                    mesh.Quad(a - 2, b - 2, b, a);
                    mesh.Quad(a - 2, a, b, b - 2);
                }
            }

            mesh.ComputeNormals();

            return mesh;
        }

        /// <summary>A flat disc of water (a pool), `segments` round.</summary>
        public static MeshData Pool(float radius, int segments = 40)
        {
            var mesh = new MeshData();
            // A pool barely moves: no flow (rg 0.5), deep in the middle (b 1), slow (a).
            var centre = mesh.Vertex(0, 0, 0, 0.5f, 0.5f, 0.5f, 0.5f, 1, 0.1f);

            for (var i = 0; i <= segments; i++)
            {
                var angle = i * Math.PI * 2 / segments;
                var x = (float)Math.Sin(angle) * radius;
                var z = (float)Math.Cos(angle) * radius;
                mesh.Vertex(x, 0, z, x / radius * 0.5f + 0.5f, z / radius * 0.5f + 0.5f, 0.5f, 0.5f, 0, 0.1f);

                if (i > 0)
                {
                    mesh.Triangle(centre, i, i + 1);
                }
            }

            for (var v = 0; v < mesh.VertexCount; v++)
            {
                mesh.SetNormal(v, 0, 1, 0);
            }

            return mesh;
        }

        // ------------------------------------------------------------------ The far land

        /// <summary>
        /// The whole map as one coarse mesh, `cells` squares a side, coloured by what the ground is
        /// (for the distance, under the detailed terrain near the hero): the horizon's hills.
        /// </summary>
        public static MeshData FarLand(WorldPlan world, int cells, float sink)
        {
            var mesh = new MeshData();
            var size = Regions.Half * 2;
            var step = size / cells;
            var weights = new float[Surface.Layers];

            for (var j = 0; j <= cells; j++)
            {
                for (var i = 0; i <= cells; i++)
                {
                    var x = -Regions.Half + i * step;
                    var z = -Regions.Half + j * step;
                    var p = new Vec2(x, z);
                    var h = world.Land.Height(p);
                    var slope = Math.Abs(world.Land.Height(x + step, z) - h) / step + Math.Abs(world.Land.Height(x, z + step) - h) / step;
                    world.Surface.Paint(p, h, slope, weights);
                    float r = 0, g = 0, b = 0;

                    for (var k = 0; k < Surface.Layers; k++)
                    {
                        r += FarColours[k * 3] * weights[k];
                        g += FarColours[k * 3 + 1] * weights[k];
                        b += FarColours[k * 3 + 2] * weights[k];
                    }

                    // Woods look darker from afar than their floor.
                    var biome = world.Surface.BiomeAt(p, h, slope, world.Land.Moisture(p));

                    if (biome == Biome.PineForest || biome == Biome.Broadleaf || biome == Biome.Bamboo)
                    {
                        r *= 0.62f;
                        g *= 0.7f;
                        b *= 0.62f;
                    }

                    mesh.Vertex(x, h - sink, z, i / (float)cells, j / (float)cells, r, g, b);
                }
            }

            for (var j = 0; j < cells; j++)
            {
                for (var i = 0; i < cells; i++)
                {
                    var a = j * (cells + 1) + i;
                    var b = a + 1;
                    var c = a + cells + 1;
                    var d = c + 1;
                    mesh.Quad(a, c, d, b);
                }
            }

            mesh.ComputeNormals();

            return mesh;
        }

        /// <summary>The ground layers' colours from afar (r, g, b per layer, 0–1).</summary>
        public static readonly float[] FarColours =
        {
            0.33f, 0.44f, 0.24f, 0.19f, 0.27f, 0.16f, 0.52f, 0.52f, 0.5f, 0.6f, 0.52f, 0.37f, 0.44f, 0.42f, 0.4f,
            0.58f, 0.55f, 0.5f, 0.28f, 0.4f, 0.22f, 0.46f, 0.37f, 0.26f, 0.28f, 0.24f, 0.18f, 0.52f, 0.43f, 0.31f,
        };
    }
}
