using System;
using System.Collections.Generic;

namespace GuWorld.Core.Gen
{
    /// <summary>
    /// The height of the ground anywhere on the 8 × 8 km map, built up in layers:
    ///
    /// 1. <see cref="Base"/> — Qing Mao itself: plains, the main ridge and its spurs, the front
    ///    mountain, the back mountain's cliff behind the Bai, the marsh, the walls at the map's edge,
    ///    and the Gu Yue valley's own plateau.
    /// 2. <see cref="Shaped"/> — places levelled for villages, fights and caves (pits behind rock).
    /// 3. <see cref="Height"/> — the rivers' beds cut in, and the roads graded.
    ///
    /// Each layer reads the one under it, so the rivers can find their level on the shaped land and
    /// the roads their way over it. Pure C#, safe to call from any thread once built.
    /// </summary>
    public sealed class Land
    {
        public const float Half = Regions.Half;
        public const float MaxHeight = 900;

        readonly Noise hills = new Noise(1101);
        readonly Noise rock = new Noise(2203);
        readonly Noise detail = new Noise(3307);
        readonly Noise moisture = new Noise(4409);

        /// <summary>A place levelled flat: the valley's places, the regions' places and the settlements.</summary>
        public sealed class Flat
        {
            public string Id;
            public Vec2 At;
            public float Radius;
            public float Level;
            public float Blend;
            public bool Cave;
            public bool Settlement;
            /// <summary>A cave's open side (yaw), NaN if it is shut all round.</summary>
            public float Mouth = float.NaN;
        }

        readonly List<Flat> flats = new List<Flat>();
        readonly ShapeIndex<Flat> flatIndex = new ShapeIndex<Flat>(Half, 128);
        readonly Vec2[] ridge;
        readonly float[] crest;
        readonly Vec2[] cliff;

        public IReadOnlyList<Flat> Flats => flats;
        public Waters Waters { get; private set; }
        public Roads Roads { get; private set; }

        public Land()
        {
            ridge = new Vec2[Regions.Ridge.Length];
            crest = new float[Regions.Ridge.Length];

            for (var i = 0; i < ridge.Length; i++)
            {
                ridge[i] = Regions.Ridge[i].at;
                crest[i] = Regions.Ridge[i].crest;
            }

            cliff = Array.ConvertAll(Regions.BaiCliff, p => (Vec2)p);
            AddFlats();
        }

        /// <summary>The rivers, once found (their levels come from the shaped land).</summary>
        public void SetWaters(Waters waters) => Waters = waters;

        /// <summary>The roads, once found (they are graded into the ground).</summary>
        public void SetRoads(Roads roads) => Roads = roads;

        /// <summary>The pads a town's houses stand on: the ground under each is levelled to it, easing back over a few metres.</summary>
        readonly ShapeIndex<(Box box, float level)> pads = new ShapeIndex<(Box, float)>(Half, 32);
        public const float PadBlend = 4.5f;

        public void SetPads(IEnumerable<(Box box, float level)> list)
        {
            foreach (var pad in list)
            {
                pads.Add(pad.box.Centre, pad.box.Radius + PadBlend, pad);
            }
        }

        /// <summary>The ground eased to the pads round a point (the strongest pad wins).</summary>
        float Padded(Vec2 p, float h)
        {
            if (pads.Count == 0)
            {
                return h;
            }

            var weight = 0f;
            var level = h;

            foreach (var (box, padLevel) in pads.Near(p, 0))
            {
                var d = box.Distance(p);
                var w = 1 - Noise.Smoother(0, PadBlend, d);

                if (w > weight)
                {
                    weight = w;
                    level = padLevel;
                }
            }

            return h + (level - h) * weight;
        }

        // ------------------------------------------------------------------ Layer 1: the mountain

        /// <summary>The main ridge's pull at a point: its height, and how far the point is from its line.</summary>
        float Ridge(Vec2 p, out float distance, out bool north)
        {
            distance = Lines.ToPolyline(p, ridge, out var segment, out var t);
            var top = Noise.Lerp(crest[segment], crest[segment + 1], t);
            var a = ridge[segment];
            var b = ridge[segment + 1];
            north = Vec2.Cross(b - a, p - a) > 0;
            var reach = north ? Regions.RidgeNorthReach : Regions.RidgeSouthReach;
            // The reach breathes along the ridge, so the foot is not a straight line.
            reach *= 0.85f + 0.3f * Noise.Clamp01(hills.Perlin(p.X * 0.0009f + 40, p.Z * 0.0009f) * 0.5f + 0.5f);
            var u = distance / reach;
            var shape = (float)Math.Exp(-3 * u * u);

            return top * shape;
        }

        /// <summary>The ground of the mountain itself, before anything is levelled or cut.</summary>
        public float Base(float x, float z)
        {
            var p = new Vec2(x, z);
            // The plains: low rolling land.
            var h = 12 + 9 * hills.Fbm(x * 0.0055f, z * 0.0055f, 3) + 3.5f * hills.Fbm(x * 0.021f + 50, z * 0.021f, 2) + 0.6f * detail.Perlin(x * 0.09f, z * 0.09f);
            // Southward the land sinks a little towards the old trade road.
            h -= Noise.Smooth(-150, -1500, z) * 5;

            // The main ridge, with spurs running down its flanks and gullies between them.
            var ridgeHeight = Ridge(p, out var fromRidge, out var north);
            var spurs = rock.Ridged(x * 0.0021f, z * 0.0021f, 4);
            h += ridgeHeight * (0.82f + 0.36f * spurs);

            // The front mountain.
            var front = Vec2.Distance(p, Regions.FrontPeak) / Regions.FrontReach;
            h += Regions.FrontCrest * (float)Math.Exp(-2.6f * front * front) * (0.85f + 0.3f * rock.Ridged(x * 0.0026f + 9, z * 0.0026f, 3));

            // Behind the ridge, the back mountain's shelf, falling at the cliff to the Bai's basin.
            if (z > 1900)
            {
                h += Noise.Smooth(1950, 2500, z) * 60;
                var toCliff = Lines.ToPolyline(p, cliff, out var segment, out var along);
                var a = cliff[segment];
                var b = cliff[segment + 1];
                var side = Vec2.Cross(b - a, p - a) > 0 ? 1 : -1;
                // How far along the cliff line (its ends fade into the hills).
                var lengthAlong = Lines.DistanceAlong(cliff, segment, along);
                var total = Lines.Length(cliff);
                var ends = Noise.Smooth(0, 220, lengthAlong) * Noise.Smooth(total, total - 220, lengthAlong) * Noise.Smooth(320, 120, toCliff);
                h += Regions.BaiCliffDrop * Noise.Smooth(14, -14, side * toCliff) * ends;
            }

            // Rock: the high slopes are rougher.
            var high = Noise.Smooth(70, 320, h);
            h += high * (rock.Ridged(x * 0.011f, z * 0.011f, 3) - 0.45f) * 26;

            // The marsh: low and flat.
            var marsh = Vec2.Distance(p, Regions.Marsh) / (Regions.MarshRadius * (0.8f + 0.4f * (hills.Perlin(x * 0.004f + 20, z * 0.004f) * 0.5f + 0.5f)));

            if (marsh < 1)
            {
                h = Noise.Lerp(h, 3.2f + 0.8f * detail.Perlin(x * 0.03f, z * 0.03f), Noise.Smooth(1, 0.6f, marsh));
            }

            // The Gu Yue valley's plateau north-east of the village, where the Qing falls.
            h += Noise.Smooth(Valley.PlateauZ - 4, Valley.PlateauZ + 4, z) * Noise.Smooth(Valley.PlateauX - 16, Valley.PlateauX + 16, x)
                * Noise.Smooth(620, 470, x) * Noise.Smooth(1150, 650, z) * Valley.PlateauRise;

            // The wild lands beyond the mountain of the story (16 × 16 km): hills that grow into ranges towards the edge.
            var outward = Math.Max(Math.Abs(x), Math.Abs(z));
            var wild = Noise.Smooth(4300, 6400, outward);

            if (wild > 0)
            {
                h += wild * (35 + 210 * rock.Ridged(x * 0.00045f + 31.7f, z * 0.00045f - 12.3f, 4) * Noise.Smooth(0.25f, 0.75f, rock.Perlin(x * 0.00018f, z * 0.00018f) * 0.5f + 0.5f));
            }

            // The walls of mountains at the map's edge.
            var edge = Math.Max(Math.Abs(x), Math.Abs(z));
            h += Noise.Smooth(Half - 320, Half - 20, edge) * (240 + 60 * rock.Fbm(x * 0.004f, z * 0.004f, 3));

            return Noise.Clamp(h, 0.5f, MaxHeight - 1);
        }

        // ------------------------------------------------------------------ Layer 2: levelled places

        void AddFlats()
        {
            foreach (var place in Valley.Places)
            {
                if (!float.IsNaN(place.Level))
                {
                    AddFlat(place.Id, place.At, place.Radius, place.Level, place.Blend, place.Cave, false);
                }
                else if (place.Flatten)
                {
                    AddFlat(place.Id, place.At, place.Radius, AutoLevel(place.At, place.Radius), place.Blend, place.Cave, false);
                }
            }

            foreach (var place in Regions.Places)
            {
                var level = !float.IsNaN(place.Level) ? place.Level : place.Cave ? AutoLevel(place.At, place.Radius) - 7 : place.Flatten ? AutoLevel(place.At, place.Radius) : float.NaN;

                if (!float.IsNaN(level))
                {
                    AddFlat(place.Id, place.At, place.Radius, level, 40, place.Cave, false).Mouth = place.Mouth;
                }
            }

            foreach (var settlement in Regions.Settlements)
            {
                // A town is not levelled as one: each of its houses stands on a pad of its own (Pads).
                if (settlement.City)
                {
                    continue;
                }

                var level = AutoLevel(settlement.At, settlement.Radius);
                AddFlat(settlement.Id, settlement.At, settlement.Radius + 8, level, 50 + settlement.Radius * 0.4f, false, true);
            }
        }

        Flat AddFlat(string id, Vec2 at, float radius, float level, float blend, bool cave, bool settlement)
        {
            var flat = new Flat { Id = id, At = at, Radius = radius, Level = level, Blend = blend, Cave = cave, Settlement = settlement };
            flats.Add(flat);
            flatIndex.Add(at, radius + (cave ? 70 : blend), flat);

            return flat;
        }

        /// <summary>The level a place is cut to by itself: the mean of the mountain round its middle, a little lower than its highest side.</summary>
        float AutoLevel(Vec2 at, float radius)
        {
            var sum = Base(at.X, at.Z) * 2;
            var count = 2;

            for (var i = 0; i < 8; i++)
            {
                var d = Vec2.FromYaw(i * 45) * radius * 0.6f;
                sum += Base(at.X + d.X, at.Z + d.Z);
                count++;
            }

            return sum / count;
        }

        public Flat FlatOf(string id) => flats.Find(f => f.Id == id);

        /// <summary>The ground with its places levelled and its caves sunk (no rivers, no roads yet).</summary>
        public float Shaped(float x, float z)
        {
            var h = Base(x, z);
            var p = new Vec2(x, z);

            foreach (var flat in flatIndex.Near(p, 0))
            {
                var d = Vec2.Distance(p, flat.At);

                if (flat.Cave)
                {
                    // A pit behind cliffs: flat inside, a ring of rock round it.
                    var inside = 1 - Noise.Smooth(flat.Radius - 2, flat.Radius + 8, d);
                    var cut = Noise.Lerp(h, flat.Level, inside);
                    var wall = Noise.Smooth(flat.Radius - 1, flat.Radius + 12, d) * (1 - Noise.Smooth(flat.Radius + 26, flat.Radius + 60, d));

                    if (!float.IsNaN(flat.Mouth))
                    {
                        // The mouth: no rock on that side, and a ramp from the land outside down to the floor.
                        var off = Math.Abs(((p - flat.At).Yaw - flat.Mouth + 540) % 360 - 180);
                        var open = 1 - Noise.Smooth(12, 24, off);
                        wall *= 1 - open;
                        var ramp = Noise.Lerp(flat.Level, h, Noise.Smooth(flat.Radius - 4, flat.Radius + 64, d));
                        cut = Noise.Lerp(cut, ramp, open);
                    }

                    h = Math.Max(cut, cut + wall * (46 + 6 * rock.Fbm(x * 0.05f, z * 0.05f, 2)));
                }
                else if (d < flat.Radius + flat.Blend)
                {
                    h = Noise.Lerp(h, flat.Level, 1 - Noise.Smooth(flat.Radius * 0.9f, flat.Radius + flat.Blend, d));
                }
            }

            return h;
        }

        // ------------------------------------------------------------------ Layer 3: water and roads

        /// <summary>The ground as the game has it: places levelled, rivers cut, roads graded.</summary>
        public float Height(float x, float z)
        {
            var h = Shaped(x, z);
            var p = new Vec2(x, z);

            if (Waters != null)
            {
                h = Waters.Carve(p, h);
            }

            if (Roads != null)
            {
                h = Roads.Grade(p, h, Waters);
            }

            h = Padded(p, h);

            return Noise.Clamp(h, 0.2f, MaxHeight - 1);
        }

        public float Height(Vec2 p) => Height(p.X, p.Z);

        /// <summary>The steepness at a point, as a rise over a run (0 flat, 1 is 45°).</summary>
        public float Slope(float x, float z, float step = 2)
        {
            var dx = Height(x + step, z) - Height(x - step, z);
            var dz = Height(x, z + step) - Height(x, z - step);

            return (float)Math.Sqrt(dx * dx + dz * dz) / (2 * step);
        }

        /// <summary>How wet the land is, 0–1: near water, in the marsh, and in the noise's damp patches.</summary>
        public float Moisture(Vec2 p)
        {
            var wet = moisture.Fbm(p.X * 0.0016f, p.Z * 0.0016f, 3) * 0.5f + 0.5f;

            if (Waters != null)
            {
                wet = Math.Max(wet, 1 - Noise.Smooth(10, 90, Waters.Distance(p)));
            }

            wet = Math.Max(wet, 1 - Noise.Smooth(Regions.MarshRadius * 0.7f, Regions.MarshRadius * 1.2f, Vec2.Distance(p, Regions.Marsh)));

            return Noise.Clamp01(wet);
        }

        public float Detail(float x, float z, float scale) => detail.Perlin(x * scale, z * scale);
    }
}
