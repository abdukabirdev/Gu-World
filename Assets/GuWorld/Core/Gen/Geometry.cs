using System;
using System.Collections.Generic;

namespace GuWorld.Core.Gen
{
    /// <summary>A point or direction on the map: X east, Z north, metres.</summary>
    public struct Vec2 : IEquatable<Vec2>
    {
        public float X;
        public float Z;

        public Vec2(float x, float z)
        {
            X = x;
            Z = z;
        }

        public static implicit operator Vec2(MapPoint p) => new Vec2(p.X, p.Z);
        public static implicit operator MapPoint(Vec2 v) => new MapPoint(v.X, v.Z);

        public static Vec2 operator +(Vec2 a, Vec2 b) => new Vec2(a.X + b.X, a.Z + b.Z);
        public static Vec2 operator -(Vec2 a, Vec2 b) => new Vec2(a.X - b.X, a.Z - b.Z);
        public static Vec2 operator -(Vec2 a) => new Vec2(-a.X, -a.Z);
        public static Vec2 operator *(Vec2 a, float k) => new Vec2(a.X * k, a.Z * k);
        public static Vec2 operator *(float k, Vec2 a) => new Vec2(a.X * k, a.Z * k);
        public static Vec2 operator /(Vec2 a, float k) => new Vec2(a.X / k, a.Z / k);

        public float Length => (float)Math.Sqrt(X * X + Z * Z);
        public float LengthSquared => X * X + Z * Z;

        public Vec2 Normalized
        {
            get
            {
                var length = Length;

                return length > 1e-6f ? new Vec2(X / length, Z / length) : new Vec2(0, 1);
            }
        }

        /// <summary>Turned a quarter to the right (clockwise seen from above): for (0, 1) north, (1, 0) east.</summary>
        public Vec2 Right => new Vec2(Z, -X);
        public Vec2 Left => new Vec2(-Z, X);

        public static float Dot(Vec2 a, Vec2 b) => a.X * b.X + a.Z * b.Z;
        public static float Cross(Vec2 a, Vec2 b) => a.X * b.Z - a.Z * b.X;
        public static float Distance(Vec2 a, Vec2 b) => (a - b).Length;
        public static float DistanceSquared(Vec2 a, Vec2 b) => (a - b).LengthSquared;
        public static Vec2 Lerp(Vec2 a, Vec2 b, float t) => new Vec2(a.X + (b.X - a.X) * t, a.Z + (b.Z - a.Z) * t);

        /// <summary>A direction from a yaw in degrees, the engine's way: 0 north (+Z), 90 east (+X).</summary>
        public static Vec2 FromYaw(float degrees)
        {
            var r = degrees * (float)Math.PI / 180;

            return new Vec2((float)Math.Sin(r), (float)Math.Cos(r));
        }

        /// <summary>The yaw, in degrees 0–360, a direction points to (0 north, 90 east).</summary>
        public float Yaw
        {
            get
            {
                var degrees = (float)(Math.Atan2(X, Z) * 180 / Math.PI);

                return degrees < 0 ? degrees + 360 : degrees;
            }
        }

        /// <summary>Turned by a yaw in degrees (clockwise seen from above, as the engine turns).</summary>
        public Vec2 Rotated(float degrees)
        {
            var r = degrees * (float)Math.PI / 180;
            var c = (float)Math.Cos(r);
            var s = (float)Math.Sin(r);

            return new Vec2(X * c + Z * s, -X * s + Z * c);
        }

        public bool Equals(Vec2 other) => X == other.X && Z == other.Z;
        public override bool Equals(object obj) => obj is Vec2 v && Equals(v);
        public override int GetHashCode() => X.GetHashCode() * 397 ^ Z.GetHashCode();
        public override string ToString() => $"({X:0.#}, {Z:0.#})";
    }

    /// <summary>A box on the map, turned by a yaw: a building's footprint, a lot.</summary>
    public struct Box
    {
        public Vec2 Centre;
        /// <summary>Half its size across (along its own X) and deep (along its own Z, its front).</summary>
        public Vec2 Half;
        public float Yaw;

        public Box(Vec2 centre, Vec2 half, float yaw)
        {
            Centre = centre;
            Half = half;
            Yaw = yaw;
        }

        public Vec2 AxisX => Vec2.FromYaw(Yaw + 90);
        public Vec2 AxisZ => Vec2.FromYaw(Yaw);

        public Box Grown(float margin) => new Box(Centre, new Vec2(Half.X + margin, Half.Z + margin), Yaw);

        /// <summary>The point in the box's own frame (X across, Z to its front).</summary>
        public Vec2 Local(Vec2 world)
        {
            var d = world - Centre;

            return new Vec2(Vec2.Dot(d, AxisX), Vec2.Dot(d, AxisZ));
        }

        public Vec2 World(Vec2 local) => Centre + AxisX * local.X + AxisZ * local.Z;

        public bool Contains(Vec2 p)
        {
            var local = Local(p);

            return Math.Abs(local.X) <= Half.X && Math.Abs(local.Z) <= Half.Z;
        }

        /// <summary>The distance from a point to the box (0 inside).</summary>
        public float Distance(Vec2 p)
        {
            var local = Local(p);
            var dx = Math.Max(0, Math.Abs(local.X) - Half.X);
            var dz = Math.Max(0, Math.Abs(local.Z) - Half.Z);

            return (float)Math.Sqrt(dx * dx + dz * dz);
        }

        public Vec2[] Corners()
        {
            var x = AxisX * Half.X;
            var z = AxisZ * Half.Z;

            return new[] { Centre - x - z, Centre + x - z, Centre + x + z, Centre - x + z };
        }

        public float Radius => Half.Length;

        /// <summary>Whether two turned boxes overlap (separating axes).</summary>
        public static bool Overlap(Box a, Box b)
        {
            if (Vec2.Distance(a.Centre, b.Centre) > a.Radius + b.Radius)
            {
                return false;
            }

            foreach (var axis in new[] { a.AxisX, a.AxisZ, b.AxisX, b.AxisZ })
            {
                Project(a, axis, out var minA, out var maxA);
                Project(b, axis, out var minB, out var maxB);

                if (maxA < minB || maxB < minA)
                {
                    return false;
                }
            }

            return true;
        }

        static void Project(Box box, Vec2 axis, out float min, out float max)
        {
            var c = Vec2.Dot(box.Centre, axis);
            var r = Math.Abs(Vec2.Dot(box.AxisX, axis)) * box.Half.X + Math.Abs(Vec2.Dot(box.AxisZ, axis)) * box.Half.Z;
            min = c - r;
            max = c + r;
        }

        public override string ToString() => $"box {Centre} {Half.X * 2:0.#}×{Half.Z * 2:0.#} yaw {Yaw:0}";
    }

    /// <summary>Lines on the map: distances to them, walking along them, smoothing them.</summary>
    public static class Lines
    {
        /// <summary>The distance from a point to a segment, and how far along it the nearest point is (0–1).</summary>
        public static float ToSegment(Vec2 p, Vec2 a, Vec2 b, out float t)
        {
            var ab = b - a;
            var lengthSquared = ab.LengthSquared;
            t = lengthSquared > 1e-9f ? Noise.Clamp01(Vec2.Dot(p - a, ab) / lengthSquared) : 0;

            return Vec2.Distance(p, a + ab * t);
        }

        /// <summary>The distance from a point to a polyline; the segment it is nearest and how far along that segment.</summary>
        public static float ToPolyline(Vec2 p, IList<Vec2> line, out int segment, out float t)
        {
            var best = float.MaxValue;
            segment = 0;
            t = 0;

            for (var i = 0; i < line.Count - 1; i++)
            {
                var d = ToSegment(p, line[i], line[i + 1], out var along);

                if (d < best)
                {
                    best = d;
                    segment = i;
                    t = along;
                }
            }

            return best;
        }

        public static float ToPolyline(Vec2 p, IList<Vec2> line) => ToPolyline(p, line, out _, out _);

        public static float Length(IList<Vec2> line)
        {
            var length = 0f;

            for (var i = 0; i < line.Count - 1; i++)
            {
                length += Vec2.Distance(line[i], line[i + 1]);
            }

            return length;
        }

        /// <summary>The distance along a polyline to a point on a segment of it.</summary>
        public static float DistanceAlong(IList<Vec2> line, int segment, float t)
        {
            var length = 0f;

            for (var i = 0; i < segment && i < line.Count - 1; i++)
            {
                length += Vec2.Distance(line[i], line[i + 1]);
            }

            return segment < line.Count - 1 ? length + Vec2.Distance(line[segment], line[segment + 1]) * t : length;
        }

        /// <summary>The point at a distance along a polyline (clamped to its ends), and the direction there.</summary>
        public static Vec2 At(IList<Vec2> line, float distance, out Vec2 direction)
        {
            direction = line.Count > 1 ? (line[1] - line[0]).Normalized : new Vec2(0, 1);

            if (distance <= 0 || line.Count < 2)
            {
                return line[0];
            }

            for (var i = 0; i < line.Count - 1; i++)
            {
                var step = Vec2.Distance(line[i], line[i + 1]);

                if (distance <= step)
                {
                    direction = (line[i + 1] - line[i]).Normalized;

                    return Vec2.Lerp(line[i], line[i + 1], step > 0 ? distance / step : 0);
                }

                distance -= step;
            }

            direction = (line[line.Count - 1] - line[line.Count - 2]).Normalized;

            return line[line.Count - 1];
        }

        /// <summary>The polyline cut into pieces no longer than `step`.</summary>
        public static List<Vec2> Resample(IList<Vec2> line, float step)
        {
            var result = new List<Vec2>();

            if (line.Count == 0)
            {
                return result;
            }

            result.Add(line[0]);

            for (var i = 0; i < line.Count - 1; i++)
            {
                var a = line[i];
                var b = line[i + 1];
                var pieces = Math.Max(1, (int)Math.Ceiling(Vec2.Distance(a, b) / step));

                for (var k = 1; k <= pieces; k++)
                {
                    result.Add(Vec2.Lerp(a, b, k / (float)pieces));
                }
            }

            return result;
        }

        /// <summary>Corners cut off (Chaikin), `rounds` times; the ends stay where they are.</summary>
        public static List<Vec2> Chaikin(IList<Vec2> line, int rounds)
        {
            var current = new List<Vec2>(line);

            for (var round = 0; round < rounds && current.Count > 2; round++)
            {
                var next = new List<Vec2> { current[0] };

                for (var i = 0; i < current.Count - 1; i++)
                {
                    var a = current[i];
                    var b = current[i + 1];

                    if (i > 0)
                    {
                        next.Add(Vec2.Lerp(a, b, 0.25f));
                    }

                    if (i < current.Count - 2)
                    {
                        next.Add(Vec2.Lerp(a, b, 0.75f));
                    }
                }

                next.Add(current[current.Count - 1]);
                current = next;
            }

            return current;
        }

        /// <summary>A line made to wander: each point pushed sideways by noise, more in the middle than at the ends.</summary>
        public static List<Vec2> Meander(IList<Vec2> line, Noise noise, float amplitude, float wavelength, float step)
        {
            var points = Resample(line, step);
            var total = Length(points);
            var result = new List<Vec2>(points.Count);
            var gone = 0f;

            for (var i = 0; i < points.Count; i++)
            {
                if (i > 0)
                {
                    gone += Vec2.Distance(points[i - 1], points[i]);
                }

                var forward = (points[Math.Min(i + 1, points.Count - 1)] - points[Math.Max(i - 1, 0)]).Normalized;
                var ends = Noise.Smooth(0, total * 0.12f, gone) * Noise.Smooth(total, total * 0.88f, gone);
                var push = noise.Fbm(gone / wavelength, noise.Seed * 0.013f, 3) * amplitude * ends;
                result.Add(points[i] + forward.Right * push);
            }

            return result;
        }

        /// <summary>Points dropped that lie almost on the line between their neighbours (within `tolerance`).</summary>
        public static List<Vec2> Simplify(IList<Vec2> line, float tolerance)
        {
            if (line.Count < 3)
            {
                return new List<Vec2>(line);
            }

            var keep = new bool[line.Count];
            keep[0] = keep[line.Count - 1] = true;
            var stack = new Stack<(int, int)>();
            stack.Push((0, line.Count - 1));

            while (stack.Count > 0)
            {
                var (first, last) = stack.Pop();
                var worst = -1f;
                var index = -1;

                for (var i = first + 1; i < last; i++)
                {
                    var d = ToSegment(line[i], line[first], line[last], out _);

                    if (d > worst)
                    {
                        worst = d;
                        index = i;
                    }
                }

                if (index >= 0 && worst > tolerance)
                {
                    keep[index] = true;
                    stack.Push((first, index));
                    stack.Push((index, last));
                }
            }

            var result = new List<Vec2>();

            for (var i = 0; i < line.Count; i++)
            {
                if (keep[i])
                {
                    result.Add(line[i]);
                }
            }

            return result;
        }

        /// <summary>Where two segments cross (if they do): the point and how far along each.</summary>
        public static bool Cross(Vec2 a, Vec2 b, Vec2 c, Vec2 d, out Vec2 at, out float t, out float u)
        {
            var r = b - a;
            var s = d - c;
            var denominator = Vec2.Cross(r, s);
            at = default;
            t = u = 0;

            if (Math.Abs(denominator) < 1e-9f)
            {
                return false;
            }

            t = Vec2.Cross(c - a, s) / denominator;
            u = Vec2.Cross(c - a, r) / denominator;

            if (t < 0 || t > 1 || u < 0 || u > 1)
            {
                return false;
            }

            at = a + r * t;

            return true;
        }
    }

    /// <summary>
    /// A grid over the map that knows which segments of a set of lines pass near each of its cells,
    /// so the distance to the nearest of hundreds of segments costs a handful of checks.
    /// </summary>
    public sealed class SegmentIndex
    {
        public struct Segment
        {
            public Vec2 A;
            public Vec2 B;
            /// <summary>Which line it belongs to, and which segment of that line.</summary>
            public int Line;
            public int Index;
        }

        readonly float cell;
        readonly float reach;
        readonly float min;
        readonly int size;
        readonly List<int>[] cells;
        public readonly List<Segment> Segments = new List<Segment>();

        /// <param name="half">The map's half size: the grid covers −half…half.</param>
        /// <param name="cellSize">A cell's side.</param>
        /// <param name="reach">How far from a segment a query must still find it.</param>
        public SegmentIndex(float half, float cellSize, float reach)
        {
            cell = cellSize;
            this.reach = reach;
            min = -half;
            size = Math.Max(1, (int)Math.Ceiling(half * 2 / cellSize));
            cells = new List<int>[size * size];
        }

        public void Add(IList<Vec2> line, int lineId)
        {
            for (var i = 0; i < line.Count - 1; i++)
            {
                var segment = new Segment { A = line[i], B = line[i + 1], Line = lineId, Index = i };
                var id = Segments.Count;
                Segments.Add(segment);
                var x0 = Cell(Math.Min(segment.A.X, segment.B.X) - reach);
                var x1 = Cell(Math.Max(segment.A.X, segment.B.X) + reach);
                var z0 = Cell(Math.Min(segment.A.Z, segment.B.Z) - reach);
                var z1 = Cell(Math.Max(segment.A.Z, segment.B.Z) + reach);

                for (var z = z0; z <= z1; z++)
                {
                    for (var x = x0; x <= x1; x++)
                    {
                        // Only cells the segment's reach truly touches.
                        var centre = new Vec2(min + (x + 0.5f) * cell, min + (z + 0.5f) * cell);

                        if (Lines.ToSegment(centre, segment.A, segment.B, out _) > reach + cell * 0.7072f)
                        {
                            continue;
                        }

                        var list = cells[z * size + x] ?? (cells[z * size + x] = new List<int>(4));
                        list.Add(id);
                    }
                }
            }
        }

        int Cell(float v) => Math.Max(0, Math.Min(size - 1, (int)((v - min) / cell)));

        /// <summary>The nearest segment within the reach (false if none is that close).</summary>
        public bool Nearest(Vec2 p, out float distance, out Segment segment, out float t)
        {
            distance = float.MaxValue;
            segment = default;
            t = 0;
            var list = cells[Cell(p.Z) * size + Cell(p.X)];

            if (list == null)
            {
                return false;
            }

            foreach (var id in list)
            {
                var s = Segments[id];
                var d = Lines.ToSegment(p, s.A, s.B, out var along);

                if (d < distance)
                {
                    distance = d;
                    segment = s;
                    t = along;
                }
            }

            return distance <= reach;
        }

        public float Distance(Vec2 p) => Nearest(p, out var d, out _, out _) ? d : float.MaxValue;

        /// <summary>Every segment near a point (within the reach), for a caller that wants them all.</summary>
        public IEnumerable<Segment> Near(Vec2 p)
        {
            var list = cells[Cell(p.Z) * size + Cell(p.X)];

            if (list == null)
            {
                yield break;
            }

            foreach (var id in list)
            {
                yield return Segments[id];
            }
        }
    }

    /// <summary>A grid over the map that knows which boxes and circles are near each of its cells.</summary>
    public sealed class ShapeIndex<T>
    {
        readonly float cell;
        readonly float min;
        readonly int size;
        readonly List<(Vec2 centre, float radius, T item)>[] cells;
        public int Count { get; private set; }

        public ShapeIndex(float half, float cellSize)
        {
            cell = cellSize;
            min = -half;
            size = Math.Max(1, (int)Math.Ceiling(half * 2 / cellSize));
            cells = new List<(Vec2, float, T)>[size * size];
        }

        int Cell(float v) => Math.Max(0, Math.Min(size - 1, (int)((v - min) / cell)));

        public void Add(Vec2 centre, float radius, T item)
        {
            Count++;

            for (var z = Cell(centre.Z - radius); z <= Cell(centre.Z + radius); z++)
            {
                for (var x = Cell(centre.X - radius); x <= Cell(centre.X + radius); x++)
                {
                    var list = cells[z * size + x] ?? (cells[z * size + x] = new List<(Vec2, float, T)>(4));
                    list.Add((centre, radius, item));
                }
            }
        }

        /// <summary>Everything whose circle reaches within `within` of a point.</summary>
        public IEnumerable<T> Near(Vec2 p, float within)
        {
            for (var z = Cell(p.Z - within); z <= Cell(p.Z + within); z++)
            {
                for (var x = Cell(p.X - within); x <= Cell(p.X + within); x++)
                {
                    var list = cells[z * size + x];

                    if (list == null)
                    {
                        continue;
                    }

                    foreach (var (centre, radius, item) in list)
                    {
                        if (Vec2.Distance(centre, p) <= radius + within)
                        {
                            yield return item;
                        }
                    }
                }
            }
        }
    }
}
