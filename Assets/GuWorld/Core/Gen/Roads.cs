using System;
using System.Collections.Generic;

namespace GuWorld.Core.Gen
{
    /// <summary>
    /// The roads: the valley's own (as its plan draws them) and the regions' (found here). A road is
    /// found on a grid of 16 m cells by the cheapest way between its ends, where steepness costs much
    /// (a cart cannot climb a cliff), water costs a bridge, a settlement in the way costs a detour
    /// and a road already made costs less (roads join into a network). The way is then pulled straight
    /// where it can be, rounded, given a smooth rise along its length, and graded into the land.
    /// </summary>
    public sealed class Roads
    {
        public const float Cell = 16;
        public const float Reach = 16;

        public sealed class Road
        {
            public string From;
            public string To;
            public bool Main;
            public bool Planned;
            public float HalfWidth;
            public List<Vec2> Points = new List<Vec2>();
            /// <summary>The road's height along its points (graded: no sudden rises).</summary>
            public List<float> Profile = new List<float>();
            public float Length;
        }

        public struct Bridge
        {
            public Vec2 At;
            /// <summary>The way the road runs over it.</summary>
            public Vec2 Along;
            public float Length;
            public float Width;
            public float Deck;
        }

        public readonly List<Road> All = new List<Road>();
        public readonly List<Bridge> Bridges = new List<Bridge>();
        readonly SegmentIndex index = new SegmentIndex(Regions.Half, 64, Reach);

        // The grid the ways are found on.
        readonly int size;
        readonly float[] heights;
        readonly byte[] kind;
        readonly float[] used;
        readonly float[] costs;
        readonly int[] came;
        readonly int[] stamp;
        int search;
        const byte Open = 0, Water = 1, Settled = 2, Blocked = 3;

        public Roads(Land land, Waters waters)
        {
            size = (int)(Regions.Half * 2 / Cell);
            heights = new float[size * size];
            kind = new byte[size * size];
            used = new float[size * size];
            costs = new float[size * size];
            came = new int[size * size];
            stamp = new int[size * size];

            for (var j = 0; j < size; j++)
            {
                for (var i = 0; i < size; i++)
                {
                    var p = CentreOf(i, j);
                    var h = land.Shaped(p.X, p.Z);
                    var k = Open;

                    if (waters.Wet(p, out _) || waters.Distance(p) < 8)
                    {
                        k = Water;
                    }

                    heights[j * size + i] = waters.Carve(p, h);
                    kind[j * size + i] = k;
                }
            }

            foreach (var flat in land.Flats)
            {
                Mark(flat.At, flat.Radius + (flat.Cave ? 40 : 0), flat.Cave ? Blocked : Settled);
            }

            // The valley's own roads, as drawn.
            foreach (var line in Valley.Roads)
            {
                var road = new Road { From = "valley", To = "valley", Main = true, HalfWidth = 2.6f, Points = Lines.Resample(Array.ConvertAll(line, p => (Vec2)p), 4) };
                Finish(road, land, waters);
            }

            foreach (var def in Regions.Roads)
            {
                if (!Regions.Locate(def.From, out var from) || !Regions.Locate(def.To, out var to))
                {
                    continue;
                }

                var path = Find(from, to);

                if (path == null)
                {
                    continue;
                }

                var road = new Road { From = def.From, To = def.To, Main = def.Main, Planned = true, HalfWidth = def.Main ? 2.8f : 1.8f, Points = path };
                Finish(road, land, waters);
            }

            FindBridges(waters);
        }

        Vec2 CentreOf(int i, int j) => new Vec2(-Regions.Half + (i + 0.5f) * Cell, -Regions.Half + (j + 0.5f) * Cell);

        int CellOf(float v) => Math.Max(0, Math.Min(size - 1, (int)((v + Regions.Half) / Cell)));

        void Mark(Vec2 at, float radius, byte what)
        {
            for (var j = CellOf(at.Z - radius); j <= CellOf(at.Z + radius); j++)
            {
                for (var i = CellOf(at.X - radius); i <= CellOf(at.X + radius); i++)
                {
                    if (Vec2.Distance(CentreOf(i, j), at) <= radius && kind[j * size + i] != Water)
                    {
                        kind[j * size + i] = Math.Max(kind[j * size + i], what);
                    }
                }
            }
        }

        // ------------------------------------------------------------------ Finding a way

        static readonly int[] StepX = { 1, -1, 0, 0, 1, 1, -1, -1, 2, 2, -2, -2, 1, 1, -1, -1 };
        static readonly int[] StepZ = { 0, 0, 1, -1, 1, -1, 1, -1, 1, -1, 1, -1, 2, -2, 2, -2 };

        List<Vec2> Find(Vec2 fromPoint, Vec2 toPoint)
        {
            var start = CellOf(fromPoint.Z) * size + CellOf(fromPoint.X);
            var goal = CellOf(toPoint.Z) * size + CellOf(toPoint.X);
            search++;
            stamp[start] = search;
            costs[start] = 0;
            came[start] = start;
            var open = new Heap();
            open.Push(start, 0);
            var gx = goal % size;
            var gz = goal / size;
            var startFlat = kind[start];
            var goalFlat = kind[goal];
            var visited = 0;

            while (open.Count > 0 && visited < 400000)
            {
                var current = open.Pop();
                visited++;

                if (current == goal)
                {
                    break;
                }

                var cx = current % size;
                var cz = current / size;
                var h = heights[current];
                var baseCost = costs[current];

                for (var k = 0; k < StepX.Length; k++)
                {
                    var nx = cx + StepX[k];
                    var nz = cz + StepZ[k];

                    if (nx < 1 || nz < 1 || nx >= size - 1 || nz >= size - 1)
                    {
                        continue;
                    }

                    var next = nz * size + nx;
                    var what = kind[next];
                    var centre = CentreOf(nx, nz);
                    var near = Vec2.DistanceSquared(centre, fromPoint) < 140 * 140 || Vec2.DistanceSquared(centre, toPoint) < 140 * 140;

                    if (what == Blocked && !near)
                    {
                        continue;
                    }

                    var run = Cell * (float)Math.Sqrt(StepX[k] * StepX[k] + StepZ[k] * StepZ[k]);
                    var rise = Math.Abs(heights[next] - h);
                    var grade = rise / run;

                    if (grade > 0.42f)
                    {
                        continue;
                    }

                    // Steepness costs sharply; water a bridge; the inside of a settlement other than the ends a detour.
                    var step = run * (1 + 60 * grade * grade + (grade > 0.2f ? 6 * (grade - 0.2f) : 0));

                    if (what == Water)
                    {
                        step += 160;
                    }
                    else if (what == Settled && !near)
                    {
                        step += run * 6;
                    }

                    // A way already made is cheaper: roads join.
                    step *= used[next] > 0 ? 0.55f : 1;
                    var total = baseCost + step;

                    if (stamp[next] != search || total < costs[next])
                    {
                        stamp[next] = search;
                        costs[next] = total;
                        came[next] = current;
                        var dx = nx - gx;
                        var dz = nz - gz;
                        open.Push(next, total + Cell * 0.55f * (float)Math.Sqrt(dx * dx + dz * dz));
                    }
                }
            }

            if (stamp[goal] != search)
            {
                return null;
            }

            var cells = new List<int> { goal };

            for (var at = goal; at != start; at = came[at])
            {
                cells.Add(came[at]);
            }

            cells.Reverse();
            var points = new List<Vec2> { fromPoint };

            foreach (var c in cells)
            {
                used[c] = 1;
                points.Add(CentreOf(c % size, c / size));
            }

            points.Add(toPoint);
            // Straightened where the land allows, rounded, and cut fine.
            var straight = Pull(points);

            return Lines.Resample(Lines.Chaikin(straight, 3), 4);
        }

        /// <summary>Points dropped where the way can go straight from the one before without climbing harder.</summary>
        List<Vec2> Pull(List<Vec2> points)
        {
            var result = new List<Vec2> { points[0] };
            var anchor = 0;

            while (anchor < points.Count - 1)
            {
                var far = anchor + 1;

                for (var k = points.Count - 1; k > anchor + 1; k--)
                {
                    if (Straight(points[anchor], points[k]))
                    {
                        far = k;
                        break;
                    }
                }

                result.Add(points[far]);
                anchor = far;
            }

            return result;
        }

        bool Straight(Vec2 a, Vec2 b)
        {
            var length = Vec2.Distance(a, b);

            if (length > 240)
            {
                return false;
            }

            var steps = Math.Max(2, (int)(length / (Cell * 0.5f)));
            var last = heights[CellOf(a.Z) * size + CellOf(a.X)];

            for (var s = 1; s <= steps; s++)
            {
                var p = Vec2.Lerp(a, b, s / (float)steps);
                var c = CellOf(p.Z) * size + CellOf(p.X);

                if (kind[c] == Blocked || kind[c] == Water)
                {
                    return false;
                }

                var h = heights[c];

                if (Math.Abs(h - last) / (length / steps) > 0.16f)
                {
                    return false;
                }

                last = h;
            }

            return true;
        }

        /// <summary>A small binary heap of grid cells by priority.</summary>
        sealed class Heap
        {
            readonly List<(int item, float priority)> items = new List<(int, float)>();
            public int Count => items.Count;

            public void Push(int item, float priority)
            {
                items.Add((item, priority));
                var i = items.Count - 1;

                while (i > 0)
                {
                    var parent = (i - 1) / 2;

                    if (items[parent].priority <= items[i].priority)
                    {
                        break;
                    }

                    (items[parent], items[i]) = (items[i], items[parent]);
                    i = parent;
                }
            }

            public int Pop()
            {
                var top = items[0].item;
                var last = items[items.Count - 1];
                items.RemoveAt(items.Count - 1);

                if (items.Count > 0)
                {
                    items[0] = last;
                    var i = 0;

                    while (true)
                    {
                        var left = i * 2 + 1;
                        var right = left + 1;
                        var smallest = i;

                        if (left < items.Count && items[left].priority < items[smallest].priority)
                        {
                            smallest = left;
                        }

                        if (right < items.Count && items[right].priority < items[smallest].priority)
                        {
                            smallest = right;
                        }

                        if (smallest == i)
                        {
                            break;
                        }

                        (items[smallest], items[i]) = (items[i], items[smallest]);
                        i = smallest;
                    }
                }

                return top;
            }
        }

        // ------------------------------------------------------------------ The road's rise

        void Finish(Road road, Land land, Waters waters)
        {
            var count = road.Points.Count;
            var raw = new float[count];

            for (var i = 0; i < count; i++)
            {
                var p = road.Points[i];
                raw[i] = waters.Carve(p, land.Shaped(p.X, p.Z));

                // Where it meets a road already made, it takes that road's height, so the two join without a step.
                if (Nearest(p, out var toOther, out var otherLevel, out var otherHalf, out _) && toOther < otherHalf + 3)
                {
                    raw[i] = otherLevel;
                }

                // Over a river, the deck: above the water.
                if (waters.Nearest(p, out var distance, out var level, out var width, out _) && distance < width * 0.5f + 4)
                {
                    raw[i] = Math.Max(raw[i], level + 1.4f);
                }
            }

            // Inside a levelled place the road lies at the place's level, whatever the way in: every road
            // that ends there meets the others at the same height.
            var pinned = new bool[count];

            for (var i = 0; i < count; i++)
            {
                foreach (var flat in land.Flats)
                {
                    if (Vec2.Distance(road.Points[i], flat.At) < flat.Radius - 2)
                    {
                        raw[i] = flat.Level;
                        pinned[i] = true;
                        break;
                    }
                }
            }

            // Smoothed along its length, then no steeper than one in five.
            var smooth = new float[count];

            for (var i = 0; i < count; i++)
            {
                var sum = 0f;
                var weight = 0f;

                for (var k = -5; k <= 5; k++)
                {
                    var j = Math.Max(0, Math.Min(count - 1, i + k));
                    var w = 6 - Math.Abs(k);
                    sum += raw[j] * w;
                    weight += w;
                }

                smooth[i] = pinned[i] ? raw[i] : sum / weight;
            }

            for (var pass = 0; pass < 2; pass++)
            {
                for (var i = 1; i < count; i++)
                {
                    var run = Vec2.Distance(road.Points[i], road.Points[i - 1]) * 0.2f;
                    smooth[i] = pinned[i] ? smooth[i] : Noise.Clamp(smooth[i], smooth[i - 1] - run, smooth[i - 1] + run);
                }

                for (var i = count - 2; i >= 0; i--)
                {
                    var run = Vec2.Distance(road.Points[i], road.Points[i + 1]) * 0.2f;
                    smooth[i] = pinned[i] ? smooth[i] : Noise.Clamp(smooth[i], smooth[i + 1] - run, smooth[i + 1] + run);
                }
            }

            road.Profile.AddRange(smooth);
            road.Length = Lines.Length(road.Points);
            index.Add(road.Points, All.Count);
            All.Add(road);
        }

        void FindBridges(Waters waters)
        {
            foreach (var road in All)
            {
                var onBridge = false;
                var start = 0;

                for (var i = 0; i < road.Points.Count; i++)
                {
                    var p = road.Points[i];
                    var wet = waters.Nearest(p, out var distance, out var level, out var width, out _) && distance < width * 0.5f + 2.5f;

                    if (wet && !onBridge)
                    {
                        onBridge = true;
                        start = Math.Max(0, i - 1);
                    }
                    else if (!wet && onBridge)
                    {
                        onBridge = false;
                        var a = road.Points[start];
                        var b = road.Points[i];
                        var middle = (start + i) / 2;
                        Bridges.Add(new Bridge
                        {
                            At = Vec2.Lerp(a, b, 0.5f), Along = (b - a).Normalized, Length = Vec2.Distance(a, b) + 4, Width = road.HalfWidth * 2 + 0.8f,
                            Deck = Math.Max(road.Profile[middle], (waters.LevelAt(Vec2.Lerp(a, b, 0.5f), out _) is var water && !float.IsNaN(water) ? water : 0) + 1.2f)
                        });
                    }
                }
            }
        }

        // ------------------------------------------------------------------ Asking about the roads

        public float Distance(Vec2 p) => index.Distance(p);

        /// <summary>The nearest road within reach: its distance, its profile height there, its half width.</summary>
        public bool Nearest(Vec2 p, out float distance, out float level, out float halfWidth, out Road road)
        {
            level = halfWidth = 0;
            road = null;

            if (!index.Nearest(p, out distance, out var segment, out var t))
            {
                return false;
            }

            road = All[segment.Line];
            var i = segment.Index;
            level = Noise.Lerp(road.Profile[i], road.Profile[Math.Min(i + 1, road.Profile.Count - 1)], t);
            halfWidth = road.HalfWidth;

            return true;
        }

        /// <summary>
        /// The land with the roads graded in: level along each road, easing back to the land on the
        /// shoulders. Where roads meet, their levels are blended by nearness, so a junction has no
        /// step. Not over water (the bridges span it).
        /// </summary>
        public float Grade(Vec2 p, float h, Waters waters)
        {
            var bestDistance = new Dictionary<int, (float distance, float level, float half)>();

            foreach (var segment in index.Near(p))
            {
                var d = Lines.ToSegment(p, segment.A, segment.B, out var t);
                var road = All[segment.Line];

                if (d > road.HalfWidth + 9 || (bestDistance.TryGetValue(segment.Line, out var known) && known.distance <= d))
                {
                    continue;
                }

                var i = segment.Index;
                var level = Noise.Lerp(road.Profile[i], road.Profile[Math.Min(i + 1, road.Profile.Count - 1)], t);
                bestDistance[segment.Line] = (d, level, road.HalfWidth);
            }

            if (bestDistance.Count == 0)
            {
                return h;
            }

            if (waters != null && waters.Nearest(p, out var toWater, out _, out var width, out _) && toWater < width * 0.5f + 3)
            {
                return h;
            }

            var sum = 0f;
            var weights = 0f;
            var strongest = 0f;

            foreach (var (distance, level, half) in bestDistance.Values)
            {
                var w = 1 - Noise.Smooth(half, half + 9, distance);
                // Nearer roads count for much more where several meet.
                var share = w * w / Math.Max(0.5f, distance);
                sum += level * share;
                weights += share;
                strongest = Math.Max(strongest, w);
            }

            return weights > 0 ? Noise.Lerp(h, sum / weights, strongest) : h;
        }
    }
}
