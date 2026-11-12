using System;
using System.Collections.Generic;

namespace GuWorld.Core.Gen
{
    /// <summary>
    /// The rivers as they run on the land: each drawn course made to wander (not inside the Gu Yue
    /// valley, whose plan is fixed), sampled every few metres with its water level and width. The
    /// level is found from the land itself — water never runs uphill, so it is the lowest the land
    /// has been from the source down — with the plan's pins kept exactly. Where it drops sharply,
    /// there is a waterfall.
    /// </summary>
    public sealed class Waters
    {
        public const float Step = 6;
        public const float Reach = 80;

        public sealed class River
        {
            public RiverDef Def;
            public List<Vec2> Points = new List<Vec2>();
            public List<float> Levels = new List<float>();
            public List<float> Widths = new List<float>();
            public float Length;
        }

        public struct Fall
        {
            public string River;
            public Vec2 Lip;
            public Vec2 Foot;
            public float Top;
            public float Bottom;
            public float Width;
        }

        public struct Pool
        {
            public Vec2 At;
            public float Radius;
            public float Level;
        }

        public readonly List<River> Rivers = new List<River>();
        public readonly List<Fall> Falls = new List<Fall>();
        public readonly List<Pool> Pools = new List<Pool>();
        readonly SegmentIndex index = new SegmentIndex(Regions.Half, 64, Reach);

        public Waters(Land land)
        {
            var noise = new Noise(5501);

            // Rivers that flow into another come after it, so they can meet its water.
            var order = new List<RiverDef>(Regions.Rivers);
            order.Sort((a, b) => (a.Joins == "" ? 0 : 1).CompareTo(b.Joins == "" ? 0 : 1));

            foreach (var def in order)
            {
                var river = Build(def, land, noise);
                index.Add(river.Points, Rivers.Count);
                Rivers.Add(river);
            }

            var pool = new Pool { At = Regions.BaiPool, Radius = Regions.BaiPoolRadius, Level = LevelAt(Regions.BaiPool, out _) };
            Pools.Add(pool);
        }

        River Build(RiverDef def, Land land, Noise noise)
        {
            var river = new River { Def = def };
            var course = Array.ConvertAll(def.Course, p => (Vec2)p);

            // The course wanders, except in the valley (its plan is fixed) and at pinned points.
            var points = Lines.Resample(course, Step);
            var total = Lines.Length(points);
            var gone = 0f;
            var pinned = new List<Vec2>();

            foreach (var (point, _) in def.Pins)
            {
                pinned.Add(course[point]);
            }

            for (var i = 0; i < points.Count; i++)
            {
                if (i > 0)
                {
                    gone += Vec2.Distance(points[i - 1], points[i]);
                }

                var forward = (points[Math.Min(i + 1, points.Count - 1)] - points[Math.Max(i - 1, 0)]).Normalized;
                var free = Noise.Smooth(560, 900, points[i].Length) * Noise.Smooth(0, 160, gone) * Noise.Smooth(total, total - 160, gone);

                foreach (var pin in pinned)
                {
                    free *= Noise.Smooth(60, 260, Vec2.Distance(pin, points[i]));
                }

                var push = noise.Fbm(gone / 260f, Noise.Stable(def.Id) % 997 * 0.37f, 3) * def.Meander * 2.2f * free;
                river.Points.Add(points[i] + forward.Right * push);
            }

            river.Length = Lines.Length(river.Points);
            var count = river.Points.Count;

            // The level: the lowest the shaped land has been from the source down to here, a little under it.
            var running = float.MaxValue;
            var raw = new float[count];

            for (var i = 0; i < count; i++)
            {
                var p = river.Points[i];
                // The lowest of the bed's middle and its two sides, so a river on a slope keeps to its low side.
                var side = (river.Points[Math.Min(i + 1, count - 1)] - river.Points[Math.Max(i - 1, 0)]).Normalized.Right * 6;
                var here = Math.Min(land.Shaped(p.X, p.Z), Math.Min(land.Shaped(p.X + side.X, p.Z + side.Z), land.Shaped(p.X - side.X, p.Z - side.Z))) - 1.2f;
                running = Math.Min(running, here);
                raw[i] = running;
            }

            // A river that joins another meets its water, not below it.
            if (def.Joins != "")
            {
                var parent = Rivers.Find(r => r.Def.Id == def.Joins);

                if (parent != null)
                {
                    var mouth = LevelOn(parent, river.Points[count - 1]);

                    for (var i = 0; i < count; i++)
                    {
                        raw[i] = Math.Max(raw[i], mouth);
                    }
                }
            }

            // The pins: fixed levels at chosen points; between two pins the water steps (a waterfall)
            // if they differ by more than a few metres, otherwise it slopes.
            if (def.Pins.Length > 0)
            {
                var pinIndex = new List<(int sample, float level)>();

                foreach (var (point, level) in def.Pins)
                {
                    pinIndex.Add((Nearest(river.Points, course[point]), level));
                }

                pinIndex.Sort((a, b) => a.sample.CompareTo(b.sample));

                for (var k = 0; k < pinIndex.Count; k++)
                {
                    var (sample, level) = pinIndex[k];

                    for (var i = 0; i < count; i++)
                    {
                        raw[i] = i <= sample ? Math.Max(raw[i], level) : Math.Min(raw[i], level);
                    }

                    if (k + 1 < pinIndex.Count)
                    {
                        var (next, nextLevel) = pinIndex[k + 1];
                        var step = level - nextLevel > 3;

                        for (var i = sample; i <= next; i++)
                        {
                            var t = next == sample ? 1 : (i - sample) / (float)(next - sample);
                            raw[i] = step ? (t < 0.5f ? level : nextLevel) : Noise.Lerp(level, nextLevel, t);
                        }
                    }
                }
            }

            // Never uphill, and never below the sea of the plains (the land's floor).
            for (var i = 1; i < count; i++)
            {
                raw[i] = Math.Min(raw[i], raw[i - 1]);
            }

            for (var i = 0; i < count; i++)
            {
                raw[i] = Math.Max(raw[i], 1.4f);
            }

            for (var i = 0; i < count; i++)
            {
                river.Levels.Add(raw[i]);
                river.Widths.Add(Noise.Lerp(def.WidthFrom, def.WidthTo, i / (float)Math.Max(1, count - 1)));
            }

            // Waterfalls: where the level drops by more than ten metres within two samples, much faster than the stream round it runs.
            for (var i = 0; i + 2 < count; i++)
            {
                var drop = river.Levels[i] - river.Levels[i + 2];
                var before = Math.Max(0, i - 12);
                var after = Math.Min(count - 1, i + 14);
                var around = (river.Levels[before] - river.Levels[after] - drop) / Math.Max(1, after - before - 2) * 2;

                if (drop > 10 && drop > around * 4 && (Falls.Count == 0 || Vec2.Distance(Falls[Falls.Count - 1].Lip, river.Points[i]) > 60))
                {
                    var foot = i + 2;

                    while (foot + 1 < count && river.Levels[foot] - river.Levels[foot + 1] > 0.5f)
                    {
                        foot++;
                    }

                    Falls.Add(new Fall
                    {
                        River = def.Id, Lip = river.Points[i], Foot = river.Points[foot], Top = river.Levels[i], Bottom = river.Levels[foot], Width = river.Widths[i] * 1.2f
                    });
                }
            }

            return river;
        }

        static int Nearest(List<Vec2> points, Vec2 to)
        {
            var best = 0;
            var distance = float.MaxValue;

            for (var i = 0; i < points.Count; i++)
            {
                var d = Vec2.DistanceSquared(points[i], to);

                if (d < distance)
                {
                    distance = d;
                    best = i;
                }
            }

            return best;
        }

        static float LevelOn(River river, Vec2 p)
        {
            Lines.ToPolyline(p, river.Points, out var segment, out var t);

            return Noise.Lerp(river.Levels[segment], river.Levels[Math.Min(segment + 1, river.Levels.Count - 1)], t);
        }

        // ------------------------------------------------------------------ Asking about the water

        /// <summary>The distance to the nearest river's middle line (very large if none is near).</summary>
        public float Distance(Vec2 p) => index.Distance(p);

        /// <summary>The water level and the bed's width of the river nearest a point, and how far its middle is (false if none is within reach).</summary>
        public bool Nearest(Vec2 p, out float distance, out float level, out float width, out int river)
        {
            level = width = 0;
            river = -1;

            if (!index.Nearest(p, out distance, out var segment, out var t))
            {
                return false;
            }

            var r = Rivers[segment.Line];
            var i = segment.Index;
            level = Noise.Lerp(r.Levels[i], r.Levels[Math.Min(i + 1, r.Levels.Count - 1)], t);
            width = Noise.Lerp(r.Widths[i], r.Widths[Math.Min(i + 1, r.Widths.Count - 1)], t);
            river = segment.Line;

            return true;
        }

        public float LevelAt(Vec2 p, out float distance) => Nearest(p, out distance, out var level, out _, out _) ? level : float.NaN;

        /// <summary>True if a point is under water (in a river's bed or a pool), with the water's level.</summary>
        public bool Wet(Vec2 p, out float level)
        {
            foreach (var pool in Pools)
            {
                if (Vec2.Distance(p, pool.At) < pool.Radius)
                {
                    level = pool.Level;
                    return true;
                }
            }

            if (Nearest(p, out var distance, out level, out var width, out _) && distance < width * 0.5f + 0.5f)
            {
                return true;
            }

            level = float.NaN;
            return false;
        }

        /// <summary>The ground with the beds cut: wadeable beds, banks easing up, low land beside the water lifted so it holds.</summary>
        public float Carve(Vec2 p, float h)
        {
            if (Nearest(p, out var distance, out var level, out var width, out _))
            {
                var half = width * 0.5f;
                var bed = level - 0.55f - width * 0.02f;
                // Low land beside the water is lifted a little above it (a levee), so the water stays in its bed.
                h += Math.Max(0, level + 0.9f - h) * (1 - Noise.Smooth(half + 18, half + 64, distance));
                var depth = Math.Max(0, h - level);
                var bank = Math.Max(16, depth * 1.6f);
                h = Noise.Lerp(bed, h, Noise.Smooth(half, half + bank, distance));
            }

            foreach (var pool in Pools)
            {
                var d = Vec2.Distance(p, pool.At);

                if (d < pool.Radius + 40)
                {
                    var bed = pool.Level - 2.2f * (1 - Noise.Smooth(0, pool.Radius, d)) - 0.5f;
                    var rim = h + Math.Max(0, pool.Level + 0.8f - h) * (1 - Noise.Smooth(pool.Radius + 14, pool.Radius + 40, d));
                    h = Noise.Lerp(Math.Min(h, bed), rim, Noise.Smooth(pool.Radius - 2, pool.Radius + 12, d));
                }
            }

            return h;
        }
    }
}
