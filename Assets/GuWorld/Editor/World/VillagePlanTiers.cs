using System.Collections.Generic;
using System.Linq;
using GuWorld.World;
using UnityEngine;

namespace GuWorld.Editor
{
    /// <summary>
    /// Gu Yue as a mountain town of many low terraces (v4, 2026-10-25 evening; the user, over his screenshots: the 27 m
    /// tiers far too high, 7 m each; the houses too even in their rows and too alike, a village that grew instead; four
    /// gates, the great hall in the middle; two agents studied the textures and the buildings first:
    /// Claude/plans/guyue-2026-10-25-v4.md).
    ///
    /// The rectangle 1080 by 500 m across the slope (its fort's wall unchanged) is cut into terraces 7 m apart that
    /// follow the mountain: a plane through its ground, bent by about half its hollows and by a slow wander, rounded
    /// to whole terraces. So their edges curve, bay and run on; each is a stone retaining wall a terrace high with a
    /// parapet, the ground stepping hidden in its body. Level pads under the great buildings and the squares, along
    /// the fort's lower and upper walls and down the main way bend the terraces round them.
    ///
    /// In the middle the clan head's court: the great hall facing down the main way, the elders' and the inner
    /// affairs' halls at its sides, the ancestors' and the punishments' behind, the square with its stalls before it.
    /// The academy west of it, the healers east; the Mo and the Chi families' fenced quarters in the upper corners; the
    /// market, the inn and the great tavern by the main gate below; the mortals to the south-west round the lotus
    /// pond, the crafts to the south-east. Four gates: the main one below, one above, one each side.
    ///
    /// The streets: the main way straight up the middle from the lower gate to the court and on to the upper gate, a
    /// double stair at each terrace; along every terrace its own winding lane (where the ground is a little above its
    /// level: a contour, so it bends as the terrace does); stairs up the walls between the lanes here and there. The
    /// houses: of many kinds mixed by quarter, set along the lanes at uneven distances, turned a little, now and then
    /// side on, and more in the room behind them, facing the nearest lane.
    ///
    /// The water: the spring's channel down the west of the court, the east channel from the upper pond, each falling
    /// over every retaining wall into a basin and ending in a pond above the lower wall.
    ///
    /// The village's own frame: along the slope (Along, "east") and uphill (-Down, "north"), from the settlement's middle.
    /// </summary>
    public partial class VillagePlan
    {
        // The rectangle the fort's wall stands on, m (the earthworks reach out round it).
        const float West = -500, East = 580, South = -190, North = 310;
        /// <summary>The main way's line: the lower gate, the court, the great hall, the upper gate.</summary>
        const float AxisAlong = 40;
        /// <summary>A terrace's rise over the one below (the user, 2026-10-25: 7 m each).</summary>
        const float Rise = 7;
        /// <summary>The channels' lines (along): the spring's on the west of the court, the east one's.</summary>
        const float WestWater = -59, EastWater = 270;
        /// <summary>The level strip kept outside the wall before the ground goes to the mountain's own, m.</summary>
        const float Berm = 6;
        /// <summary>How far inside the wall the guards of a gate stand, m.</summary>
        const float RingInset = 11;
        /// <summary>The strip inside the wall's line kept for it (its own depth, its gates', its towers'), m.</summary>
        const float WallStrip = 7.5f;
        /// <summary>The steepest a gate's road may ramp to the mountain (rise over run).</summary>
        const float RampGrade = 0.15f;
        /// <summary>How far a house's front stands back from its street's edge at the least, m.</summary>
        const float Setback = 1.2f;
        /// <summary>A retaining wall's depth behind its face, and where under it the ground steps (hidden in it), m.</summary>
        const float WallBody = 2f, WallTuck = 1f;
        /// <summary>How high the parapets stand over their walls' tops, m.</summary>
        const float ParapetHeight = 0.9f;
        /// <summary>The terraces' height field: its cell, how far it reaches round the rectangle, m.</summary>
        const float FieldCell = 2, FieldMargin = 90;
        /// <summary>How much of the mountain's own hollows and rises the terraces follow (the rest a plane through them).</summary>
        const float Follow = 0.45f;
        /// <summary>How far round a pad its level reaches out before the ground is the terraces' own again, m.</summary>
        const float PadBlend = 30;
        /// <summary>A terrace's lane runs where the field is this much over its level (a little uphill of its middle).</summary>
        const float LaneAbove = 0.5f;
        /// <summary>
        /// The stair up a terrace's wall: the artist's grand one. Its top landing is 6.0 m up (its 9.02 m are the posts
        /// over it; 2026-10-25: scaled by those, it stopped 1.6 m short of the wall's top), so it is raised 7/6 to the
        /// rise, narrowed and shortened to 0.85 (6.9 by 19.8 m); where a terrace is too shallow for that run, shortened
        /// to 0.62 (14.5 m, steeper).
        /// </summary>
        const string StairModel = "prop_stairs_grand";
        const float StairLanding = 6.0f, StairAcross = 0.85f, StairRun = 0.85f, SteepRun = 0.62f;

        /// <summary>How far up its rise the grand stair's treads are, a share along its run (measured on the model's middle line: a landing a little past halfway, the top landing its last sixth).</summary>
        static float StairProfile(float t) =>
            t < 0.094f ? Mathf.Lerp(0, 0.035f, t / 0.094f) :
            t < 0.437f ? Mathf.Lerp(0.035f, 0.5f, (t - 0.094f) / 0.343f) :
            t < 0.523f ? Mathf.Lerp(0.5f, 0.535f, (t - 0.437f) / 0.086f) :
            t < 0.832f ? Mathf.Lerp(0.535f, 1, (t - 0.523f) / 0.309f) : 1;

        /// <summary>Whether this is the terraced town (Gu Yue's): its wall is built by its rectangle (QingMaoBuilder.BuildFortRect).</summary>
        public bool LevelGround => Glb;

        /// <summary>How far outside the boundary the town's earthworks reach: no giant tree nearer.</summary>
        public float Margin => Glb ? 160 : 22;

        /// <summary>Where the fort's walls step a terrace: the point on the wall's line, the levels below and above.</summary>
        public readonly List<(Vector2 at, float low, float high)> WallSteps = new List<(Vector2, float, float)>();

        // The terraces' height field over the rectangle and round it (the village's frame), and the level of terrace 0.
        int fieldNa, fieldNu;
        float[] field;
        float levelBase;
        // The stair's footprint scaled to a terrace (width, run).
        Vector2 stairSize;

        /// <summary>A gate's road out to the mountain: from the gate, straight out, level over the berm, then ramping to the ground at its end.</summary>
        class GateRoad
        {
            public Vector2 At;
            public Vector2 Out;
            public Vector2 Across;
            public float Length;
            public float End;
            public float Level;
        }

        readonly List<GateRoad> gateRoads = new List<GateRoad>();

        /// <summary>The streets houses stand along (their points in the world), half their width, when (the main ones first), whether main.</summary>
        readonly List<(List<Vector2> line, float half, int order, bool main)> lotStreets = new List<(List<Vector2>, float, int, bool)>();

        /// <summary>A channel of water in stone: its line, half its width, its water's and bed's levels, the ground's beside it.</summary>
        public class Channel
        {
            public List<Vector2> Points = new List<Vector2>();
            public float Half;
            public float Water;
            public float Bed;
            public float Top;
        }

        /// <summary>A pond (a rectangle, or round: Half.x its radius) in a stone rim.</summary>
        public class Pool
        {
            public Vector2 Centre;
            public Vector2 Half;
            public float Yaw;
            public float Water;
            public float Bed;
            public float Top;
            public bool Round;
        }

        /// <summary>A fall of water down a wall's face: where it leaves the face, out, from and to which level, how wide.</summary>
        public class Fall
        {
            public Vector2 At;
            public Vector2 Out;
            public float Top;
            public float Bottom;
            public float Width;
        }

        /// <summary>A stone bridge over a channel: its middle, half its size (across the water, along the way), turn, its deck's level.</summary>
        public class Bridge
        {
            public Vector2 Centre;
            public Vector2 Half;
            public float Yaw;
            public float Top;
        }

        public readonly List<Channel> Channels = new List<Channel>();
        public readonly List<Pool> Pools = new List<Pool>();
        public readonly List<Fall> Falls = new List<Fall>();
        public readonly List<Bridge> Bridges = new List<Bridge>();

        // The water cut into the ground, in the village's frame: channels (a line, half width, bed), ponds (a box, bed), basins (a disc, bed).
        readonly List<(Vector2 a, Vector2 b, float half, float bed)> cuts = new List<(Vector2, Vector2, float, float)>();
        readonly List<(Vector2 min, Vector2 max, float bed)> pits = new List<(Vector2, Vector2, float)>();
        readonly List<(Vector2 centre, float radius, float bed)> bowls = new List<(Vector2, float, float)>();

        /// <summary>The stairs up the walls, in the world: the foot's middle, uphill, half their width, their run, the levels at foot and top.</summary>
        readonly List<(Vector2 foot, Vector2 up, float half, float run, float low, float high)> flights = new List<(Vector2, Vector2, float, float, float, float)>();

        /// <summary>The terraces' retaining walls (the village's frame): the terrace below, the line where the ground steps, its face, uphill at each point.</summary>
        readonly List<(int low, List<Vector2> line, List<Vector2> face, List<Vector2> up, Wall wall)> terraceWalls = new List<(int, List<Vector2>, List<Vector2>, List<Vector2>, Wall)>();

        /// <summary>Each terrace's lanes (the village's frame).</summary>
        readonly List<(int terrace, List<Vector2> line)> lanes = new List<(int, List<Vector2>)>();

        /// <summary>Where a parapet leaves a gap (a stair's head, a fall): the point on the face (world), half the gap.</summary>
        readonly List<(Vector2 at, float half)> parapetGaps = new List<(Vector2, float)>();

        /// <summary>A level pad of the height field (the village's frame): its middle, half its size, how far its level holds past it, its terrace.</summary>
        class PadSpec
        {
            public Vector2 Centre;
            public Vector2 Half;
            public float Margin;
        }

        readonly List<PadSpec> padSpecs = new List<PadSpec>();

        /// <summary>One of the great buildings: its place's name (or none), the model, its scale, its middle (the village's frame), the way it faces.</summary>
        class GreatSpec
        {
            public string Place;
            public string Model;
            public float Scale;
            public Vector2 At;
            public Vector2 Facing;
            public BuildingKind Kind;
            public string Label;

            /// <summary>Half its footprint in the village's frame (turned when it faces along).</summary>
            public Vector2 LocalHalf
            {
                get
                {
                    var size = QingMaoBuilder.VillageGlb[Model].Size * Scale / 2;

                    return Mathf.Abs(Facing.x) > 0.5f ? new Vector2(size.y, size.x) : size;
                }
            }
        }

        static readonly Vector2 East_ = Vector2.right, West_ = Vector2.left, North_ = Vector2.up, South_ = Vector2.down;

        Vector2 W(float along, float up) => World(along, up);

        Vector2 W(Vector2 local) => World(local.x, local.y);

        /// <summary>A way given in the village's frame, in the world's (unit length).</summary>
        Vector2 WorldDir(Vector2 local) => (Along * local.x - Down * local.y).normalized;

        /// <summary>The turn that faces a way given in the village's frame (x along, y uphill).</summary>
        float Facing(Vector2 local) => YawFacing(Along * local.x - Down * local.y);

        /// <summary>A point a turned thing's own (x across its front, z out of its front) puts at, from its middle.</summary>
        static Vector2 Offset(Vector2 centre, float yaw, float x, float z)
        {
            var v = Quaternion.Euler(0, yaw, 0) * new Vector3(x, 0, z);

            return centre + new Vector2(v.x, v.z);
        }

        // ── The terraces' field ───────────────────────────────────────

        /// <summary>The field's height at a point of the village's frame (bilinear).</summary>
        float FieldAt(Vector2 local)
        {
            var ga = Mathf.Clamp((local.x - (West - FieldMargin)) / FieldCell, 0, fieldNa - 1.001f);
            var gu = Mathf.Clamp((local.y - (South - FieldMargin)) / FieldCell, 0, fieldNu - 1.001f);
            var i = (int)ga;
            var j = (int)gu;
            var fa = ga - i;
            var fu = gu - j;
            float F(int x, int y) => field[y * fieldNa + x];

            return Mathf.Lerp(Mathf.Lerp(F(i, j), F(i + 1, j), fa), Mathf.Lerp(F(i, j + 1), F(i + 1, j + 1), fa), fu);
        }

        /// <summary>Which terrace a point of the village's frame is on (0: the main gate's).</summary>
        int TerraceAt(Vector2 local) => Mathf.RoundToInt((FieldAt(local) - levelBase) / Rise);

        float TerraceLevel(int terrace) => levelBase + Rise * terrace;

        /// <summary>Uphill on the field at a point (the village's frame, unit length): across the terraces' edges.</summary>
        Vector2 FieldUphill(Vector2 local)
        {
            var g = new Vector2(FieldAt(local + new Vector2(1.5f, 0)) - FieldAt(local - new Vector2(1.5f, 0)), FieldAt(local + new Vector2(0, 1.5f)) - FieldAt(local - new Vector2(0, 1.5f)));

            return g.sqrMagnitude > 1e-8f ? g.normalized : Vector2.up;
        }

        /// <summary>The level a thing standing at a point stands on (its terrace's).</summary>
        public float TierLevelAt(Vector2 p) => Glb ? TerraceLevel(TerraceAt(Local(p))) : Level0;

        /// <summary>Whether a turned rectangle (grown a metre) lies on one terrace.</summary>
        bool OneTerrace(Vector2 centre, Vector2 half, float yaw)
        {
            var terrace = TerraceAt(Local(centre));

            foreach (var (x, z) in new[] { (-1f, -1f), (1f, -1f), (-1f, 1f), (1f, 1f), (0f, -1f), (0f, 1f), (-1f, 0f), (1f, 0f) })
            {
                if (TerraceAt(Local(Offset(centre, yaw, x * (half.x + 1), z * (half.y + 1)))) != terrace)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>A box blur over a grid, `passes` times (near a gaussian), the edges held.</summary>
        static float[] BoxBlur(float[] source, int nx, int ny, int radius, int passes)
        {
            var a = (float[])source.Clone();
            var b = new float[a.Length];
            var width = 2 * radius + 1;

            for (var pass = 0; pass < passes; pass++)
            {
                for (var j = 0; j < ny; j++)
                {
                    for (var i = 0; i < nx; i++)
                    {
                        var sum = 0f;

                        for (var d = -radius; d <= radius; d++)
                        {
                            sum += a[j * nx + Mathf.Clamp(i + d, 0, nx - 1)];
                        }

                        b[j * nx + i] = sum / width;
                    }
                }

                for (var j = 0; j < ny; j++)
                {
                    for (var i = 0; i < nx; i++)
                    {
                        var sum = 0f;

                        for (var d = -radius; d <= radius; d++)
                        {
                            sum += b[Mathf.Clamp(j + d, 0, ny - 1) * nx + i];
                        }

                        a[j * nx + i] = sum / width;
                    }
                }
            }

            return a;
        }

        /// <summary>
        /// The terraces' field: the mountain's ground blurred (its bumps gone), a plane fitted through it over the
        /// rectangle, the field the plane bent by `Follow` of the blurred ground's difference from it and a slow wander;
        /// down the main way's corridor the same across (the walls cross it square); then each pad held at its terrace's
        /// level and blended out round it.
        /// </summary>
        void MakeField(List<GreatSpec> great)
        {
            var a0 = West - FieldMargin;
            var u0 = South - FieldMargin;
            fieldNa = Mathf.CeilToInt((East - West + 2 * FieldMargin) / FieldCell) + 1;
            fieldNu = Mathf.CeilToInt((North - South + 2 * FieldMargin) / FieldCell) + 1;
            var raw = new float[fieldNa * fieldNu];

            for (var j = 0; j < fieldNu; j++)
            {
                for (var i = 0; i < fieldNa; i++)
                {
                    var p = W(a0 + i * FieldCell, u0 + j * FieldCell);
                    raw[j * fieldNa + i] = natural(p.x, p.y);
                }
            }

            var smooth = BoxBlur(raw, fieldNa, fieldNu, 14, 3);

            // The plane through the rectangle's blurred ground (least squares, about the means).
            double n = 0, sa = 0, su = 0, sh = 0, saa = 0, suu = 0, sau = 0, sah = 0, suh = 0;

            for (var j = 0; j < fieldNu; j++)
            {
                for (var i = 0; i < fieldNa; i++)
                {
                    double a = a0 + i * FieldCell, u = u0 + j * FieldCell, h = smooth[j * fieldNa + i];

                    if (a < West || a > East || u < South || u > North)
                    {
                        continue;
                    }

                    n++;
                    sa += a;
                    su += u;
                    sh += h;
                    saa += a * a;
                    suu += u * u;
                    sau += a * u;
                    sah += a * h;
                    suh += u * h;
                }
            }

            double ma = sa / n, mu = su / n, mh = sh / n;
            double caa = saa / n - ma * ma, cuu = suu / n - mu * mu, cau = sau / n - ma * mu, cah = sah / n - ma * mh, cuh = suh / n - mu * mh;
            var det = caa * cuu - cau * cau;
            var ka = (cah * cuu - cuh * cau) / det;
            var ku = (cuh * caa - cah * cau) / det;
            field = new float[raw.Length];

            for (var j = 0; j < fieldNu; j++)
            {
                for (var i = 0; i < fieldNa; i++)
                {
                    var a = a0 + i * FieldCell;
                    var u = u0 + j * FieldCell;
                    var plane = (float)(mh + ka * (a - ma) + ku * (u - mu));
                    var wander = MountainShape.Fbm(a * 0.006f + 3.1f, u * 0.006f + 7.7f, 3) * 7f + MountainShape.Fbm(a * 0.018f + 1.3f, u * 0.018f + 5.2f, 2) * 2.2f;
                    field[j * fieldNa + i] = plane + Follow * (smooth[j * fieldNa + i] - plane) + wander;
                }
            }

            BuildLog.Write($"plan of {Place.Id}: the terraces' plane rises {ku * 100:0.0} m in 100 m uphill, {ka * 100:+0.0;-0.0} m along");

            // The main way's corridor: the field across it the same as on its line, blended out over 30 m each side.
            for (var j = 0; j < fieldNu; j++)
            {
                var sum = 0f;
                var count = 0;

                for (var i = 0; i < fieldNa; i++)
                {
                    if (Mathf.Abs(a0 + i * FieldCell - AxisAlong) <= 20)
                    {
                        sum += field[j * fieldNa + i];
                        count++;
                    }
                }

                var axis = sum / count;

                for (var i = 0; i < fieldNa; i++)
                {
                    var w = 1 - Smooth01(18, 50, Mathf.Abs(a0 + i * FieldCell - AxisAlong));

                    if (w > 0)
                    {
                        field[j * fieldNa + i] = Mathf.Lerp(field[j * fieldNa + i], axis, w);
                    }
                }
            }

            // Terrace 0 is the main gate's.
            levelBase = FieldAt(new Vector2(AxisAlong, South + 10));
            var held = new bool[field.Length];

            // The pads: the strips along the lower and the upper wall (the fort's wall level there, stepping a terrace
            // only between two lengths, square to it), the gates' courts, the court, the families' quarters, the
            // academy's and the healers' grounds, every great building's own, the squares and the ponds.
            for (var a = West; a < East - 1; a += 108)
            {
                Hold(new Vector2(a + 54, South + 9), new Vector2(54, 9), 0);
                Hold(new Vector2(a + 54, North - 9), new Vector2(54, 9), 0);
            }

            Hold(new Vector2(AxisAlong, -166), new Vector2(34, 16), 3);
            Hold(new Vector2(AxisAlong, 294), new Vector2(22, 14), 3);
            // The court in two: the square and the forecourt below, the great hall's court a terrace above them (one
            // wall between, the main way's stair up it).
            var court = Hold(new Vector2(AxisAlong, 24), new Vector2(80, 27), 0);
            Hold(new Vector2(AxisAlong, 82), new Vector2(84, 31), 0, court + 1);
            Hold(new Vector2(-320, 262), new Vector2(95, 40), 2);
            Hold(new Vector2(400, 262), new Vector2(95, 40), 2);
            Hold(new Vector2(-172, 92), new Vector2(70, 38), 3);
            Hold(new Vector2(-165, 25), new Vector2(68, 10), 3);
            Hold(new Vector2(215, 88), new Vector2(48, 30), 3);
            Hold(new Vector2(215, 25), new Vector2(45, 10), 3);

            foreach (var g in great)
            {
                Hold(g.At, g.LocalHalf, 5);
            }

            Hold(new Vector2(-200, -150), new Vector2(36, 18), 2);
            Hold(new Vector2(WestWater, -166), new Vector2(15, 10), 2);
            Hold(new Vector2(EastWater, -166), new Vector2(15, 10), 2);
            Hold(new Vector2(EastWater, 272), new Vector2(17, 11), 2);
            Hold(new Vector2(-330, -128), new Vector2(46, 17), 2);

            // The main way's corridor once more, now the pads are down: between two stretches of its line that a pad
            // holds, the field rising evenly from the one's level to the other's (its walls evenly spaced, each with
            // room for the way's stair below it), blended out across as before; no pad's ground changed.
            var line = Mathf.RoundToInt((AxisAlong - a0) / FieldCell);
            var profile = new float[fieldNu];
            var last = -1;

            for (var j = 0; j < fieldNu; j++)
            {
                profile[j] = field[j * fieldNa + line];

                if (!held[j * fieldNa + line])
                {
                    continue;
                }

                for (var k = last + 1; last >= 0 && k < j; k++)
                {
                    profile[k] = Mathf.Lerp(profile[last], profile[j], (float)(k - last) / (j - last));
                }

                last = j;
            }

            for (var j = 0; j < fieldNu; j++)
            {
                for (var i = 0; i < fieldNa; i++)
                {
                    var w = 1 - Smooth01(18, 50, Mathf.Abs(a0 + i * FieldCell - AxisAlong));

                    if (w > 0 && !held[j * fieldNa + i])
                    {
                        field[j * fieldNa + i] = Mathf.Lerp(field[j * fieldNa + i], profile[j], w);
                    }
                }
            }

            // Each pad level to its margin (later pads over earlier ones there), blended out round it where no pad holds
            // the ground already (the bigger the pad, the farther: its edges stand further from its level).
            int Hold(Vector2 centre, Vector2 half, float margin, int terrace = int.MinValue)
            {
                var spec = new PadSpec { Centre = centre, Half = half, Margin = margin };
                padSpecs.Add(spec);
                terrace = terrace == int.MinValue ? TerraceAt(centre) : terrace;
                var level = TerraceLevel(terrace);
                var reach = margin + Mathf.Max(PadBlend, Mathf.Max(half.x, half.y) * 0.9f);
                var i0 = Mathf.Max(0, Mathf.FloorToInt((centre.x - half.x - reach - a0) / FieldCell));
                var i1 = Mathf.Min(fieldNa - 1, Mathf.CeilToInt((centre.x + half.x + reach - a0) / FieldCell));
                var j0 = Mathf.Max(0, Mathf.FloorToInt((centre.y - half.y - reach - u0) / FieldCell));
                var j1 = Mathf.Min(fieldNu - 1, Mathf.CeilToInt((centre.y + half.y + reach - u0) / FieldCell));

                for (var j = j0; j <= j1; j++)
                {
                    for (var i = i0; i <= i1; i++)
                    {
                        var dx = Mathf.Max(0, Mathf.Abs(a0 + i * FieldCell - centre.x) - half.x);
                        var du = Mathf.Max(0, Mathf.Abs(u0 + j * FieldCell - centre.y) - half.y);
                        var d = Mathf.Sqrt(dx * dx + du * du);
                        var k = j * fieldNa + i;

                        if (d <= margin)
                        {
                            field[k] = level;
                            held[k] = true;
                        }
                        else if (!held[k])
                        {
                            // Evenly (a smooth step would crowd the walls in its steep middle).
                            field[k] = Mathf.Lerp(field[k], level, 1 - Mathf.Clamp01((d - margin) / (reach - margin)));
                        }
                    }
                }

                return terrace;
            }
        }

        /// <summary>The lines where the field crosses `level` (the village's frame), in the cells `keep` keeps, joined into polylines (marching squares).</summary>
        List<List<Vector2>> FieldContours(float level, System.Func<Vector2, bool> keep)
        {
            var a0 = West - FieldMargin;
            var u0 = South - FieldMargin;
            var segments = new List<(Vector2, Vector2)>();
            var at = new Vector2[4];
            var v = new float[4];
            var crossings = new List<Vector2>(4);

            for (var j = 0; j + 1 < fieldNu; j++)
            {
                for (var i = 0; i + 1 < fieldNa; i++)
                {
                    v[0] = field[j * fieldNa + i];
                    v[1] = field[j * fieldNa + i + 1];
                    v[2] = field[(j + 1) * fieldNa + i + 1];
                    v[3] = field[(j + 1) * fieldNa + i];
                    var below = (v[0] < level ? 1 : 0) + (v[1] < level ? 1 : 0) + (v[2] < level ? 1 : 0) + (v[3] < level ? 1 : 0);

                    if (below == 0 || below == 4)
                    {
                        continue;
                    }

                    at[0] = new Vector2(a0 + i * FieldCell, u0 + j * FieldCell);
                    at[1] = at[0] + new Vector2(FieldCell, 0);
                    at[2] = at[0] + new Vector2(FieldCell, FieldCell);
                    at[3] = at[0] + new Vector2(0, FieldCell);

                    if (!keep(at[0] + Vector2.one * (FieldCell / 2)))
                    {
                        continue;
                    }

                    crossings.Clear();

                    for (var e = 0; e < 4; e++)
                    {
                        var a = v[e];
                        var b = v[(e + 1) % 4];

                        if ((a < level) != (b < level))
                        {
                            crossings.Add(Vector2.Lerp(at[e], at[(e + 1) % 4], (level - a) / (b - a)));
                        }
                    }

                    if (crossings.Count == 2)
                    {
                        segments.Add((crossings[0], crossings[1]));
                    }
                    else if (crossings.Count == 4)
                    {
                        segments.Add((crossings[0], crossings[1]));
                        segments.Add((crossings[2], crossings[3]));
                    }
                }
            }

            return Chain(segments);
        }

        void LayOutTiers()
        {
            stairSize = Vector2.Scale(QingMaoBuilder.VillageGlb[StairModel].Size, new Vector2(StairAcross, StairRun));
            var great = GreatList();
            MakeField(great);
            Level0 = levelBase;
            Boundary.Add(W(West, South));
            Boundary.Add(W(East, South));
            Boundary.Add(W(East, North));
            Boundary.Add(W(West, North));
            void Phase(string what) => BuildLog.Mark($"plan of {Place.Id}: {what}");
            Phase("the terraces' field");
            Rasters();
            TerraceWalls();
            FortSteps();
            TerraceLanes();
            TierGates();
            Phase("terraces, walls, lanes and gates");
            TierWater();
            TierGreatBuildings(great);
            TierPads();
            TierStairs();
            TierStreets();
            TierBridges();
            Phase("water, great buildings, stairs, squares, streets");
            TierSquares();
            TierFences();
            TierLanterns();
            TierHouses();
            Phase("houses");
            TierDecor();
            TierGardens();
            TierWallHeights();
            Phase("props");
            BuildLog.Write($"plan of {Place.Id}: terraces {TerraceAt(new Vector2(AxisAlong, South + 10))}…{TerraceAt(new Vector2(AxisAlong, North - 10))} up the main way, level 0 at {levelBase:0.0} m; " +
                $"{terraceWalls.Count} retaining walls, {lanes.Count} lanes, {flights.Count} stairs, {WallSteps.Count} steps of the fort's wall");
        }

        // ── The ground ────────────────────────────────────────────────

        /// <summary>How far outside the rectangle a point of the village's frame is (negative inside).</summary>
        static float RectOutside(float a, float u)
        {
            var ox = Mathf.Max(West - a, a - East);
            var oy = Mathf.Max(South - u, u - North);

            return ox > 0 && oy > 0 ? Mathf.Sqrt(ox * ox + oy * oy) : Mathf.Max(ox, oy);
        }

        /// <summary>
        /// The ground's height: each terrace's level (stepping up hidden in its retaining wall's body) to the fort's
        /// wall and over the berm outside it; round that, a rock cut up to the mountain's ground or an earth bank down
        /// to it; the gates' roads; the channels, ponds and basins cut in.
        /// </summary>
        float TierHeight(Vector2 p, float naturalHeight)
        {
            var local = Local(p);
            var level = TerraceLevel(TerraceAt(local));
            var outside = RectOutside(local.x, local.y);
            var height = outside <= Berm ? level : Bank(outside - Berm, naturalHeight, level);

            foreach (var road in gateRoads)
            {
                var d = p - road.At;
                var along = Vector2.Dot(d, road.Out);
                var across = Mathf.Abs(Vector2.Dot(d, road.Across));
                var end = Berm + road.Length;

                if (along < 0 || along > end + 14 || across > 60)
                {
                    continue;
                }

                var onRoad = along <= end ? Mathf.Lerp(road.Level, road.End, Mathf.Clamp01((along - Berm) / road.Length)) : naturalHeight;
                var side = 3 + 1.6f * Mathf.Abs(onRoad - height);
                var weight = (1 - Smooth01(5, 5 + side, across)) * (1 - Smooth01(end, end + 12, along));
                height = Mathf.Lerp(height, onRoad, weight);
            }

            return Carve(local, height);
        }

        /// <summary>The ground `d` m past the berm: a rock cut (4 up for 3 across) up to the mountain's ground above the level, an earth bank down to it below.</summary>
        static float Bank(float d, float naturalHeight, float level)
        {
            var rise_ = naturalHeight - level;

            return rise_ >= 0 ? level + Mathf.Min(rise_, d * 1.3f) : level + rise_ * Smooth01(0, 10 + 2.2f * Mathf.Abs(rise_), d);
        }

        /// <summary>The water's beds cut into the ground (in the village's frame).</summary>
        float Carve(Vector2 local, float height)
        {
            foreach (var (a, b, half, bed) in cuts)
            {
                if (QingMao.ToSegment(local, a, b) <= half)
                {
                    height = Mathf.Min(height, bed);
                }
            }

            foreach (var (min, max, bed) in pits)
            {
                if (local.x >= min.x && local.x <= max.x && local.y >= min.y && local.y <= max.y)
                {
                    height = Mathf.Min(height, bed);
                }
            }

            foreach (var (centre, radius, bed) in bowls)
            {
                if (Vector2.Distance(local, centre) <= radius)
                {
                    height = Mathf.Min(height, bed);
                }
            }

            return height;
        }

        /// <summary>The height one walks at: the ground, but up the stairs their treads' (the villagers' ways follow it).</summary>
        public float WalkHeight(Vector2 p)
        {
            if (Glb)
            {
                foreach (var (foot, up, half, run, low, high) in flights)
                {
                    var d = p - foot;
                    var t = Vector2.Dot(d, up);

                    if (t >= -1 && t <= run + 0.6f && Mathf.Abs(d.x * up.y - d.y * up.x) <= half)
                    {
                        return Mathf.Lerp(low, high, StairProfile(Mathf.Clamp01(t / run)));
                    }
                }
            }

            return GroundAt(p);
        }

        void PaintTiers(float x, float z, float steep, float[] weights)
        {
            var p = new Vector2(x, z);
            var outside = -InsideDistance(p);

            if (outside > 200)
            {
                return;
            }

            var noise = Mathf.PerlinNoise(x * 0.35f, z * 0.35f);
            var blotch = Mathf.PerlinNoise(x * 0.06f + 3, z * 0.06f + 8);
            var local = Local(p);
            var wet = Carve(local, float.MaxValue) < float.MaxValue;

            if (wet)
            {
                weights[QingMaoBuilder.LayerGravelIndex] += 6;
                weights[QingMaoBuilder.LayerMudIndex] += 3;

                return;
            }

            switch (KindAt(p))
            {
                case KLane:
                    weights[QingMaoBuilder.LayerDirtIndex] += 6;
                    weights[QingMaoBuilder.LayerMudIndex] += 1.5f * blotch;
                    weights[QingMaoBuilder.LayerGravelIndex] += noise > 0.6f ? 2 : 0.6f;
                    break;
                case KPaved:
                    weights[QingMaoBuilder.LayerFlagstoneIndex] += 10;
                    weights[QingMaoBuilder.LayerMossIndex] += blotch > 0.75f ? 1.2f : 0;
                    break;
                case KFloor:
                    weights[QingMaoBuilder.LayerDirtIndex] += 5;
                    break;
                case KYard:
                    weights[QingMaoBuilder.LayerDirtIndex] += 5;
                    weights[QingMaoBuilder.LayerGravelIndex] += 1.5f;
                    break;
                case KGarden:
                    weights[QingMaoBuilder.LayerMudIndex] += 7;
                    break;
                case KGround:
                    // Trodden grass, bare earth worn in patches, moss in the corners.
                    weights[QingMaoBuilder.LayerDirtIndex] += blotch > 0.6f ? 1.6f : 0.3f;
                    weights[QingMaoBuilder.LayerMossIndex] += noise > 0.65f ? 0.6f : 0;
                    break;
                default:
                    // Outside: the trodden berm and the roads, then the banks — earth, bare rock where steep.
                    if (outside < Berm + 1.5f)
                    {
                        weights[QingMaoBuilder.LayerDirtIndex] += 4;
                        weights[QingMaoBuilder.LayerGravelIndex] += 1.5f;
                    }

                    break;
            }

            if (outside > 0 && steep > 22)
            {
                weights[QingMaoBuilder.LayerRockIndex] += Smooth01(24, 36, steep) * 4;
                weights[QingMaoBuilder.LayerDirtIndex] += Smooth01(22, 30, steep) * (1 - Smooth01(30, 38, steep)) * 1.5f;
            }
        }

        float GrassTiers(float x, float z)
        {
            var p = new Vector2(x, z);
            var cell = CellOf(p);

            if (Carve(Local(p), float.MaxValue) < float.MaxValue)
            {
                return 0;
            }

            switch (KindAt(p))
            {
                case KLane:
                case KPaved:
                case KFloor:
                case KGarden:
                    return 0;
                case KYard:
                    return 0.1f;
                case KGround:
                    return cell >= 0 && taken[cell] ? 0.2f : 0.35f + 0.35f * Mathf.PerlinNoise(x * 0.08f, z * 0.08f);
                default:
                    var outside = -InsideDistance(p);

                    return outside < Berm + 1 ? 0.15f : outside < Berm + 30 ? 0.7f : 1;
            }
        }

        /// <summary>Wild trees: not on the town, its earthworks near the wall or the gates' roads.</summary>
        bool WildTiers(Vector2 p)
        {
            if (-InsideDistance(p) < Berm + 30 || KindAt(p) != KWild)
            {
                return false;
            }

            foreach (var road in gateRoads)
            {
                var d = p - road.At;
                var along = Vector2.Dot(d, road.Out);

                if (along > -10 && along < Berm + road.Length + 20 && Mathf.Abs(Vector2.Dot(d, road.Across)) < 18)
                {
                    return false;
                }
            }

            return true;
        }

        // ── The retaining walls and the fort's steps ──────────────────

        /// <summary>
        /// A retaining wall wherever two terraces meet, inside the town and out over its earthworks until their two
        /// grounds come together: the contour halfway between their levels, where the ground steps; its face a metre
        /// downhill of that, its body two metres deep (the step hidden in it). Its heights are filled in last
        /// (TierWallHeights), when the gates' roads and the water have been cut. Its body is kept from houses.
        /// </summary>
        void TerraceWalls()
        {
            var low = Mathf.FloorToInt((field.Min() - levelBase) / Rise) - 1;
            var high = Mathf.CeilToInt((field.Max() - levelBase) / Rise) + 1;

            for (var k = low; k <= high; k++)
            {
                foreach (var raw in FieldContours(TerraceLevel(k) + Rise / 2, c => RectOutside(c.x, c.y) < Berm + 60))
                {
                    if (Length(raw) < 4)
                    {
                        continue;
                    }

                    var line = Resample(raw, 3);
                    var face = new List<Vector2>();
                    var up = new List<Vector2>();
                    var wall = new Wall { Face = true, Thick = WallBody, Backs = new List<Vector2>() };

                    foreach (var p in line)
                    {
                        var u = FieldUphill(p);
                        face.Add(p - u * WallTuck);
                        up.Add(u);
                        wall.Points.Add(W(p - u * WallTuck));
                        wall.Backs.Add(WorldDir(u));
                    }

                    terraceWalls.Add((k, line, face, up, wall));
                    Walls.Add(wall);

                    for (var i = 0; i + 1 < face.Count; i++)
                    {
                        var a = W(face[i]);
                        var b = W(face[i + 1]);
                        var back = (wall.Backs[i] + wall.Backs[i + 1]).normalized;
                        Inside((a + b) / 2 + back * (WallBody / 2), new Vector2(WallBody / 2 + 0.7f, Vector2.Distance(a, b) / 2 + 0.3f), YawFacing(b - a), 0, cell => taken[cell] = true);
                    }
                }
            }
        }

        /// <summary>The retaining walls' heights from the ground either side (and the parapets'), once all is cut.</summary>
        void TierWallHeights()
        {
            foreach (var (_, line, face, up, wall) in terraceWalls)
            {
                for (var i = 0; i < face.Count; i++)
                {
                    var below = face[i] - up[i] * 0.5f;
                    var above = line[i] + up[i] * (WallBody - WallTuck + 0.5f);
                    wall.Low.Add(TierHeight(W(below), Natural(below)));
                    wall.High.Add(TierHeight(W(above), Natural(above)));
                }
            }
        }

        /// <summary>Where the fort's walls cross from one terrace to another: a tower on a footing there (BuildFortRect).</summary>
        void FortSteps()
        {
            var sides = new[] { (new Vector2(West, South), new Vector2(East, South)), (new Vector2(East, South), new Vector2(East, North)), (new Vector2(West, North), new Vector2(East, North)), (new Vector2(West, South), new Vector2(West, North)) };

            foreach (var (from, to) in sides)
            {
                var length = Vector2.Distance(from, to);
                var last = TerraceAt(from);

                for (var s = 0.5f; s <= length; s += 0.5f)
                {
                    var at = Vector2.Lerp(from, to, s / length);
                    var terrace = TerraceAt(at);

                    if (terrace != last)
                    {
                        WallSteps.Add((W(at), TerraceLevel(Mathf.Min(terrace, last)), TerraceLevel(Mathf.Max(terrace, last))));
                        last = terrace;
                    }
                }
            }
        }

        /// <summary>The lane along each terrace: the contour a little over its level, inside the wall's strip, long enough to walk.</summary>
        void TerraceLanes()
        {
            var low = Mathf.FloorToInt((field.Min() - levelBase) / Rise) - 1;
            var high = Mathf.CeilToInt((field.Max() - levelBase) / Rise) + 1;

            for (var k = low; k <= high; k++)
            {
                foreach (var raw in FieldContours(TerraceLevel(k) + LaneAbove, c => RectOutside(c.x, c.y) < -(WallStrip + 5)))
                {
                    if (Length(raw) >= 30)
                    {
                        lanes.Add((k, Resample(raw, 1.4f)));
                    }
                }
            }
        }

        /// <summary>The nearest point of a terrace's lanes to a point (the village's frame), within `reach`; whether one is.</summary>
        bool NearestLane(Vector2 local, int terrace, float reach, out Vector2 nearest)
        {
            nearest = local;
            var best = reach;

            foreach (var (k, line) in lanes)
            {
                if (k != terrace)
                {
                    continue;
                }

                foreach (var p in line)
                {
                    var d = Vector2.Distance(p, local);

                    if (d < best)
                    {
                        best = d;
                        nearest = p;
                    }
                }
            }

            return best < reach;
        }

        /// <summary>The mountain's own ground at a point of the village's frame.</summary>
        float Natural(Vector2 local)
        {
            var p = W(local);

            return natural(p.x, p.y);
        }

        // ── The gates and their roads ─────────────────────────────────

        void TierGates()
        {
            void AddGate(string name, float along, float up, Vector2 outward)
            {
                var at = W(along, up);
                var out_ = WorldDir(outward);
                var level = TerraceLevel(TerraceAt(new Vector2(along, up)));
                var road = new GateRoad { At = at, Out = out_, Across = new Vector2(-out_.y, out_.x), Level = level };
                // As short as the steepest grade allowed lets it be; where the mountain itself falls or rises faster
                // than that (straight down or up the slope), the gentlest it can be within 60 m.
                var length = 30f;
                var gentlest = float.MaxValue;

                for (var l = 14f; l <= 60; l += 2)
                {
                    var end = at + out_ * (Berm + l);
                    var grade = Mathf.Abs(natural(end.x, end.y) - level) / l;

                    if (grade <= RampGrade)
                    {
                        length = l;
                        break;
                    }

                    if (grade < gentlest - 0.005f)
                    {
                        gentlest = grade;
                        length = l;
                    }
                }

                var last = at + out_ * (Berm + length);
                road.Length = length;
                road.End = natural(last.x, last.y);
                gateRoads.Add(road);
                Gates.Add(new Gate { At = at, Out = out_, Name = name, Reach = Berm + length + 4 });
                // The guards' place: inside, looking out.
                Places.Add((name, at - out_ * RingInset, YawFacing(out_)));
                MarkRoad(at + out_ * ((Berm + length) / 2), (Berm + length) / 2 + 2, out_);
                BuildLog.Write($"plan of {Place.Id}: {name}: its road {length:0} m to the mountain at {road.End - level:+0.0;-0.0} m");
            }

            // The side gates: each where the terrace it opens on runs longest round the middle of its side.
            float SideGate(float along)
            {
                var best = 60f;
                var score = float.MaxValue;

                for (var u = South + 40; u <= North - 40; u += 1)
                {
                    var terrace = TerraceAt(new Vector2(along, u));
                    var clear = true;

                    for (var d = -16; d <= 16 && clear; d += 2)
                    {
                        clear = TerraceAt(new Vector2(along, u + d)) == terrace && TerraceAt(new Vector2(along + Mathf.Sign(AxisAlong - along) * 20, u + d)) == terrace;
                    }

                    if (clear && Mathf.Abs(u - 60) < score)
                    {
                        score = Mathf.Abs(u - 60);
                        best = u;
                    }
                }

                return best;
            }

            AddGate("gate south", AxisAlong, South, South_);
            AddGate("gate north", AxisAlong, North, North_);
            AddGate("gate west", West, SideGate(West), West_);
            AddGate("gate east", East, SideGate(East), East_);

            // The strip inside the wall kept for the wall, its towers and gates: nothing stands or is laid there.
            var width = East - West;
            var depth = North - South;
            void Keep(Vector2 centre, Vector2 half, float yaw) => Inside(centre, half, yaw, 0, cell => taken[cell] = true);
            Keep(W((West + East) / 2, South + WallStrip / 2), new Vector2(width / 2, WallStrip / 2), Facing(North_));
            Keep(W((West + East) / 2, North - WallStrip / 2), new Vector2(width / 2, WallStrip / 2), Facing(North_));
            Keep(W(West + WallStrip / 2, (South + North) / 2), new Vector2(depth / 2, WallStrip / 2), Facing(East_));
            Keep(W(East - WallStrip / 2, (South + North) / 2), new Vector2(depth / 2, WallStrip / 2), Facing(East_));

            // The gates' passages through it, paved.
            foreach (var gate in Gates)
            {
                Inside(gate.At - gate.Out * (WallStrip / 2), new Vector2(4, WallStrip / 2 + 0.5f), YawFacing(gate.Out), 0, cell => kind[cell] = KPaved);
            }
        }

        /// <summary>A road's strip outside the wall: trodden earth (no wild tree on it).</summary>
        void MarkRoad(Vector2 middle, float halfLength, Vector2 direction)
        {
            Inside(middle, new Vector2(5, halfLength), YawFacing(direction), 0, cell =>
            {
                if (kind[cell] == KWild)
                {
                    kind[cell] = KLane;
                }
            });
        }

        // ── The water ─────────────────────────────────────────────────

        /// <summary>A channel along a line (the village's frame) on a terrace: half its width, how deep its bed lies under the terrace (the water `waterDown` under it).</summary>
        void Canal(int terrace, float half, float depth, float waterDown, params Vector2[] line)
        {
            var level = TerraceLevel(terrace);
            var channel = new Channel { Half = half, Water = level - waterDown, Bed = level - depth, Top = level };

            for (var i = 0; i < line.Length; i++)
            {
                channel.Points.Add(W(line[i]));

                if (i > 0)
                {
                    cuts.Add((line[i - 1], line[i], half, level - depth));
                    var from = W(line[i - 1]);
                    var to = W(line[i]);
                    Inside((from + to) / 2, new Vector2(half + 0.8f, Vector2.Distance(from, to) / 2 + 0.4f), YawFacing(to - from), 0, cell =>
                    {
                        taken[cell] = true;

                        if (kind[cell] != KWild)
                        {
                            kind[cell] = KYard;
                        }
                    });
                }
            }

            Channels.Add(channel);
        }

        /// <summary>A pond (a box in the village's frame) on its terrace, its water a metre under the terrace.</summary>
        void Pond(Vector2 centre, Vector2 half, float depth = 2.5f)
        {
            var level = TerraceLevel(TerraceAt(centre));
            pits.Add((centre - half, centre + half, level - depth));
            Pools.Add(new Pool { Centre = W(centre), Half = half, Yaw = Facing(North_), Water = level - 1, Bed = level - depth, Top = level });
            Inside(W(centre), half + Vector2.one * 0.8f, Facing(North_), 0, cell =>
            {
                taken[cell] = true;

                if (kind[cell] != KWild)
                {
                    kind[cell] = KYard;
                }
            });
        }

        /// <summary>A round stone basin a fall drops into.</summary>
        void Basin(Vector2 centre, float radius)
        {
            var level = TerraceLevel(TerraceAt(centre));
            bowls.Add((centre, radius, level - 3));
            Pools.Add(new Pool { Centre = W(centre), Half = new Vector2(radius, radius), Water = level - 1, Bed = level - 3, Top = level, Round = true });
            Around(W(centre), radius + 0.8f, (cell, _) =>
            {
                taken[cell] = true;

                if (kind[cell] != KWild)
                {
                    kind[cell] = KYard;
                }
            });
        }

        /// <summary>
        /// Water down a straight line (the village's frame) from `from` to `to`: a channel on each terrace it runs over,
        /// and where it comes to a terrace's edge, a fall from a spout in the wall's face into a stone basin below, the
        /// channel going on past the basin.
        /// </summary>
        void Stream(Vector2 from, Vector2 to, float half)
        {
            var length = Vector2.Distance(from, to);
            var dir = (to - from) / length;
            var start = 0f;
            var terrace = TerraceAt(from);

            for (var s = 0.5f; s <= length; s += 0.5f)
            {
                var p = from + dir * s;
                var next = TerraceAt(p);

                if (next == terrace)
                {
                    continue;
                }

                var up = FieldUphill(p);
                var end = Mathf.Max(start, s - 1.4f);

                if (end - start > 2)
                {
                    Canal(terrace, half, 2.2f, 1, from + dir * start, from + dir * end);
                }

                if (next < terrace)
                {
                    var face = p - up * WallTuck;
                    Falls.Add(new Fall { At = W(face), Out = WorldDir(-up), Top = TerraceLevel(terrace) - 1.2f, Bottom = TerraceLevel(next) - 1, Width = half * 2 - 0.6f });
                    parapetGaps.Add((W(face), half + 0.8f));
                    Basin(from + dir * (s + 7.5f), 4.5f);
                    start = s + 12.6f;
                    s = start;
                }
                else
                {
                    start = s + 1.4f;
                }

                terrace = TerraceAt(from + dir * start);
            }

            if (length - start > 2)
            {
                Canal(terrace, half, 2.2f, 1, from + dir * start, to);
            }
        }

        void TierWater()
        {
            // The spring under its pavilion: down the west of the court to the lower west pond; the east channel from
            // the upper pond to the lower east one; the lotus pond among the mortals' houses.
            var spring = Local(QingMao.Spring);
            Stream(new Vector2(WestWater, spring.y + 1), new Vector2(WestWater, -166 + 10.5f), 2);
            Pond(new Vector2(WestWater, -166), new Vector2(14, 9));
            Pond(new Vector2(EastWater, 272), new Vector2(16, 10));
            Stream(new Vector2(EastWater, 261.5f), new Vector2(EastWater, -166 + 10.5f), 1.6f);
            Pond(new Vector2(EastWater, -166), new Vector2(14, 9));
            Pond(new Vector2(-330, -128), new Vector2(44, 15));
            // The washers' place: on the channel's bank below the spring's pavilion.
            Places.Add(("spring", W(WestWater + 5, spring.y - 14), Facing(West_)));
        }

        // ── The great buildings and the pads ──────────────────────────

        static GreatSpec G(string place, string model, float scale, float along, float up, Vector2 facing, BuildingKind kind, string label) =>
            new GreatSpec { Place = place, Model = model, Scale = scale, At = new Vector2(along, up), Facing = facing, Kind = kind, Label = label };

        /// <summary>The great buildings, where each stands (the village's frame) and the way it faces.</summary>
        List<GreatSpec> GreatList()
        {
            var spring = Local(QingMao.Spring);
            var list = new List<GreatSpec>
            {
                // The middle: the head's court — the great hall facing down the main way, the elders' and the inner
                // affairs' halls at its sides, the ancestors' and the punishments' halls behind it.
                G("pavilion", "prop_clan_main_hall", 1, AxisAlong, 82, South_, BuildingKind.Pavilion, "Павильон клана"),
                G("council", "prop_council_hall", 1, -22, 84, East_, BuildingKind.Hall, "Зал старейшин"),
                G(null, "prop_council_hall", 1, 102, 84, West_, BuildingKind.Hall, "Зал внутренних дел"),
                G("shrine", "prop_ancestral_hall", 1, 6, 128, South_, BuildingKind.Ancestral, "Зал предков"),
                G(null, "prop_council_hall", 1, 76, 128, South_, BuildingKind.Hall, "Зал наказаний"),
                G(null, "prop_teahouse_octagon", 0.6f, WestWater, spring.y + 10, South_, BuildingKind.Hall, "Павильон источника"),
                // The academy west of the court: its great hall over its yard, the hostel and the refectory at the yard's
                // sides, the classrooms and its Gu nursery below.
                G("academy", "prop_academy_great", 0.6f, -172, 104, South_, BuildingKind.Academy, "Академия"),
                G("hostel", "prop_dorm_great", 0.55f, -232, 104, East_, BuildingKind.Hostel, "Общежитие академии"),
                G("refectory", "prop_refectory", 0.55f, -116, 104, West_, BuildingKind.Hall, "Трапезная академии"),
                G(null, "prop_academy", 0.8f, -215, 25, South_, BuildingKind.Academy, "Класс академии"),
                G(null, "prop_academy", 0.8f, -165, 25, South_, BuildingKind.Academy, "Класс академии"),
                G(null, "prop_gu_nursery", 0.85f, -120, 25, South_, BuildingKind.GuFarm, "Питомник Гу академии"),
                // The healers east of it: their hall, two herbalists, their nursery and store of herbs (their beds: TierGardens).
                G(null, "prop_academy", 0.9f, 215, 104, South_, BuildingKind.Hall, "Медицинский зал"),
                G(null, "prop_herbalist", 0.9f, 180, 104, South_, BuildingKind.Herbalist, "Лекарня"),
                G(null, "prop_herbalist", 0.9f, 250, 104, South_, BuildingKind.Herbalist, "Травяная лавка лекарей"),
                G(null, "prop_gu_nursery", 0.85f, 180, 25, South_, BuildingKind.GuFarm, "Питомник лечебных Гу"),
                G(null, "prop_granary", 0.85f, 250, 25, South_, BuildingKind.Granary, "Склад трав"),
                G("teahouse", "prop_teahouse_octagon", 0.75f, 150, -10, West_, BuildingKind.Teahouse, "Чайный дом «Восемь ветров»"),
                // By the main gate: the guards' barracks and the outer affairs' hall at the gate square's sides, the
                // great tavern and the inn above it, the market rows along the lower wall, the gambling house.
                G(null, "prop_dorm_great", 0.55f, -28, -148, East_, BuildingKind.Hostel, "Казарма стражи"),
                G(null, "prop_council_hall", 1, 108, -150, West_, BuildingKind.Hall, "Зал внешних дел"),
                G("tavern great", "prop_tavern_great", 0.5f, -12, -105, South_, BuildingKind.Tavern, "Таверна у площади"),
                G(null, "prop_inn_courtyard", 0.8f, 96, -105, South_, BuildingKind.Inn, "Постоялый двор"),
                G("market", "prop_market_row", 0.7f, -95, -150, North_, BuildingKind.Shop, "Торговый ряд"),
                G(null, "prop_market_row", 0.7f, 175, -150, North_, BuildingKind.Shop, "Торговый ряд"),
                G(null, "prop_gambling_tent", 0.8f, 300, -110, South_, BuildingKind.Gambling, "Игорный дом"),
            };

            // The Mo family's quarter (the upper west corner) and the Chi family's (the upper east): each its manor round
            // a courtyard, its own hall, a pavilion by its pond, its Gu nursery and granary.
            foreach (var (family, manor, hall, garden, granary) in new[] { ("Мо", -300f, -352f, -245f, -400f), ("Чи", 380f, 432f, 325f, 480f) })
            {
                list.Add(G(null, "prop_inn_courtyard", 1, manor, 252, South_, BuildingKind.Hall, $"Усадьба семьи {family}"));
                list.Add(G(null, "prop_council_hall", 1, hall, 262, South_, BuildingKind.Hall, $"Родовой зал семьи {family}"));
                list.Add(G(null, "prop_teahouse_octagon", 0.45f, garden, 284, South_, BuildingKind.Teahouse, $"Беседка семьи {family}"));
                list.Add(G(null, "prop_gu_nursery", 0.9f, garden, 234, South_, BuildingKind.GuFarm, $"Питомник Гу семьи {family}"));
                list.Add(G(null, "prop_granary", 0.9f, granary, 266, South_, BuildingKind.Granary, $"Амбар семьи {family}"));
            }

            return list;
        }

        /// <summary>One of the great buildings on its pad, its place before its front.</summary>
        void Great(GreatSpec g)
        {
            var size = QingMaoBuilder.VillageGlb[g.Model].Size * g.Scale;
            var centre = W(g.At);
            var yaw = Facing(g.Facing);

            if (!Free(centre, size / 2, yaw, 0.3f) || !OneTerrace(centre, size / 2 - Vector2.one, yaw))
            {
                BuildLog.Write($"plan of {Place.Id}: {g.Label} overlaps what stands there before it, or a terrace's edge");
            }

            var b = new Building { Kind = g.Kind, Centre = centre, Size = size, Yaw = yaw, Floor = TierLevelAt(centre), Name = g.Label, Lit = true, Model = g.Model, Scale = g.Scale, Pad = true, Seed = random.Next() };
            Buildings.Add(b);
            Take(centre, size / 2, yaw);

            if (g.Place != null)
            {
                Places.Add((g.Place, Offset(centre, yaw, 0, size.y / 2 + 2.5f), yaw + 180));
            }
        }

        void TierGreatBuildings(List<GreatSpec> great)
        {
            foreach (var g in great)
            {
                Great(g);
            }

            // The families' ponds by their pavilions.
            Pond(new Vector2(-245, 262), new Vector2(14, 7));
            Pond(new Vector2(325, 262), new Vector2(14, 7));
        }

        void TierPads()
        {
            var down = Facing(South_);
            // The square before the great hall, the forecourt between, the gate square inside the main gate, the
            // academy's yard (its training ground), the caravans' field.
            AddPad("square", W(AxisAlong, 13), new Vector2(34, 14), down, true);
            Square = Pads[Pads.Count - 1];
            Places.Add(("square", W(AxisAlong, 13), down));
            AddPad("forecourt", W(AxisAlong, 40), new Vector2(62, 9), down, true);
            AddPad("gate square", W(AxisAlong, -166), new Vector2(34, 15), down, true);
            AddPad("academy yard", W(-172, 66), new Vector2(40, 13), down, false);
            Inside(W(-172, 66), new Vector2(40, 13), down, 0, cell =>
            {
                if (!taken[cell] && kind[cell] != KWild)
                {
                    kind[cell] = KYard;
                }
            });
            Places.Add(("academy yard", W(-172, 66), down));
            AddPad("caravans", W(-200, -150), new Vector2(34, 16), down, false);
            Inside(W(-200, -150), new Vector2(34, 16), down, 0, cell =>
            {
                if (!taken[cell] && kind[cell] != KWild)
                {
                    kind[cell] = KYard;
                }
            });
        }

        // ── The stairs ────────────────────────────────────────────────

        /// <summary>
        /// A stair up a retaining wall: its head at the wall's face (`face`, the village's frame), climbing `up`, `across`
        /// m to the side; on the terrace below, its whole run on it and free. Its walkers' way, its parapet gap. Whether it went in.
        /// </summary>
        /// <summary>Why the last stair did not go in (the build's log).</summary>
        string stairWhy;

        bool AddStair(Vector2 face, Vector2 up, float across, int low, string name, bool steep = false)
        {
            var scale = new Vector3(StairAcross, Rise / StairLanding, steep ? SteepRun : StairRun);
            var stairSize = Vector2.Scale(QingMaoBuilder.VillageGlb[StairModel].Size, new Vector2(scale.x, scale.z));
            var side = new Vector2(up.y, -up.x);
            var head = face + side * across;
            var centreLocal = head - up * (stairSize.y / 2);
            var centre = W(centreLocal);
            var yaw = Facing(-up);
            var half = stairSize / 2;

            // Free: nothing standing, the water's cut, nor off the town (it may come down onto a pad or a street) — its
            // run, 3.5 m before its foot to walk up to it, 4 m over its head on the terrace above to step out on.
            var free = TerraceAt(head + up * (WallBody + 1.5f)) == low + 1 && TerraceAt(head - up * (stairSize.y + 3.5f)) == low;
            var approach = W(head - up * (stairSize.y + 1.75f));
            var landing = W(head + up * (WallBody + 2.9f));
            Inside(W(head - up * (stairSize.y / 2 + 0.7f)), half - new Vector2(0.4f, 0.7f), yaw, 0, cell => free &= !taken[cell] && kind[cell] != KWild);
            Inside(approach, new Vector2(half.x - 0.4f, 1.75f), yaw, 0, cell => free &= !taken[cell] && kind[cell] != KWild);
            Inside(landing, new Vector2(half.x - 0.4f, 2), yaw, 0, cell => free &= !taken[cell] && kind[cell] != KWild);

            if (!free)
            {
                stairWhy = TerraceAt(head + up * (WallBody + 1.5f)) == low + 1 ? "taken" : "no terrace above";

                return false;
            }

            foreach (var (x, z) in new[] { (-1f, -0.9f), (1f, -0.9f), (-1f, 0.3f), (1f, 0.3f), (0f, -1f), (0f, 0f), (0f, 1f) })
            {
                if (TerraceAt(Local(Offset(centre, yaw, x * half.x, z * half.y))) != low)
                {
                    stairWhy = $"off its terrace at ({x}, {z})";

                    return false;
                }
            }

            ModelStairs.Add((StairModel, centre, yaw, TerraceLevel(low), scale));
            var footWorld = W(head - up * stairSize.y);
            var upWorld = WorldDir(up);
            flights.Add((footWorld, upWorld, half.x - 0.5f, stairSize.y, TerraceLevel(low), TerraceLevel(low + 1)));
            Inside(centre, half + new Vector2(0, 0.4f), yaw, 0, cell =>
            {
                taken[cell] = true;

                if (kind[cell] != KWild)
                {
                    kind[cell] = KPaved;
                }
            });
            Around(centre, half.magnitude + 5, (cell, d) => stairNear[cell] = Mathf.Min(stairNear[cell], d));

            // The ground before its foot and over its head kept open (no house, no well there).
            foreach (var (at, h) in new[] { (approach, 1.75f), (landing, 2f) })
            {
                Inside(at, new Vector2(half.x, h), yaw, 0, cell =>
                {
                    if (!taken[cell] && kind[cell] != KWild)
                    {
                        kind[cell] = KPaved;
                    }
                });
            }
            parapetGaps.Add((W(head), half.x + 0.5f));
            Streets.Add(new Street { Name = name, Points = Resample(new List<Vector2> { footWorld - upWorld * 3, W(head + up * (WallBody + 2)) }, 1.4f), Half = half.x - 0.6f, Main = false });

            return true;
        }

        void TierStairs()
        {
            // The main way: a double stair at each terrace it climbs, from the main gate to the court, and from the
            // court to the upper gate.
            foreach (var (from, to) in new[] { (South + 2, North - 2) })
            {
                var last = TerraceAt(new Vector2(AxisAlong, from));

                for (var u = from; u <= to; u += 0.5f)
                {
                    var terrace = TerraceAt(new Vector2(AxisAlong, u));

                    if (terrace == last)
                    {
                        continue;
                    }

                    var p = new Vector2(AxisAlong, u);
                    var up = FieldUphill(p);
                    var face = p - up * WallTuck;
                    var low = Mathf.Min(last, terrace);
                    var placed = AddStair(face, up, -stairSize.x / 2, low, "Главная лестница") & AddStair(face, up, stairSize.x / 2, low, "Главная лестница");

                    // A terrace too shallow for the grand stair's run: one steep stair up the middle of the way.
                    if (!placed && !flights.Any(f => Vector2.Distance(f.foot + f.up * f.run, W(face)) < 8))
                    {
                        placed = AddStair(face, up, 0, low, "Главная лестница", true);
                    }

                    if (!placed)
                    {
                        BuildLog.Write($"plan of {Place.Id}: no room for the main way's stair at {u:0} ({stairWhy}; {flights.Count(f => Vector2.Distance(f.foot + f.up * f.run, W(face)) < 10)} there)");
                    }

                    last = terrace;
                }
            }

            // Up every other wall here and there: a stair every 55–130 m along it, where its run fits on the terrace
            // below and nothing stands; none within 45 m of another.
            foreach (var (low, _, face, up, _) in terraceWalls)
            {
                var length = Length(face);
                var s = 20 + R() * 50;

                while (s < length - 10)
                {
                    var tries = 0;
                    var placed = false;

                    for (; tries < 8 && !placed; tries++, s += 5)
                    {
                        var at = PointAt(face, s, out _);
                        var i = Mathf.Clamp(Mathf.RoundToInt(s / Mathf.Max(0.1f, length) * (face.Count - 1)), 0, face.Count - 1);

                        if (RectOutside(at.x, at.y) > -(WallStrip + 8) || flights.Any(f => Vector2.Distance(f.foot + f.up * f.run, W(at)) < 45))
                        {
                            continue;
                        }

                        placed = AddStair(at, up[i], 0, low, "Лестница") || AddStair(at, up[i], 0, low, "Лестница", true);
                    }

                    s += 55 + R() * 75;
                }
            }

            TierParapets();
        }

        /// <summary>The parapets along every retaining wall's top inside the town, open where a stair comes up or water falls over.</summary>
        void TierParapets()
        {
            foreach (var (low, _, face, up, _) in terraceWalls)
            {
                var top = TerraceLevel(low + 1);
                var run = new List<int>();

                for (var i = 0; i <= face.Count; i++)
                {
                    var open = i == face.Count;

                    if (!open)
                    {
                        var world = W(face[i]);
                        open = RectOutside(face[i].x, face[i].y) > -WallStrip || parapetGaps.Any(g => Vector2.Distance(g.at, world) < g.half);
                    }

                    if (!open)
                    {
                        run.Add(i);
                        continue;
                    }

                    if (run.Count >= 2)
                    {
                        var parapet = new Wall { Face = true, Thick = 0.55f, Backs = new List<Vector2>() };

                        foreach (var k in run)
                        {
                            parapet.Points.Add(W(face[k] + up[k] * 0.02f));
                            parapet.Backs.Add(WorldDir(up[k]));
                            parapet.Low.Add(top + 0.45f);
                            parapet.High.Add(top + ParapetHeight - 0.22f);
                        }

                        Walls.Add(parapet);
                    }

                    run.Clear();
                }
            }
        }

        // ── The streets ───────────────────────────────────────────────

        /// <summary>A street along a polyline (the village's frame), laid in the ground (paved or trodden); `order`: when houses are set along it (none if below 0).</summary>
        void PolyWay(string name, List<Vector2> local, float half, bool paved, bool main, int order)
        {
            var points = local.Select(p => W(p)).ToList();

            if (points.Count < 2)
            {
                return;
            }

            Streets.Add(new Street { Name = name, Points = points, Half = half, Main = main });

            for (var i = 0; i + 1 < points.Count; i++)
            {
                var a = points[i];
                var b = points[i + 1];
                Inside((a + b) / 2, new Vector2(half, Vector2.Distance(a, b) / 2 + 0.3f), YawFacing(b - a), 0, cell =>
                {
                    if (kind[cell] != KWild && !taken[cell])
                    {
                        kind[cell] = paved ? KPaved : KLane;
                    }
                });
            }

            if (order >= 0)
            {
                lotStreets.Add((points, half, order, main));
            }
        }

        /// <summary>A straight street from one point to another (the village's frame).</summary>
        void Way(string name, Vector2 from, Vector2 to, float half, bool paved, bool main, int order) =>
            PolyWay(name, Resample(new List<Vector2> { from, to }, 1.4f), half, paved, main, order);

        /// <summary>The terraces' lanes' names, from the lowest up.</summary>
        static readonly string[] LaneNames =
        {
            "Нижняя улица", "Рыночная улица", "Улица фонарей", "Каменная улица", "Улица мастеров", "Ясеневая улица",
            "Средняя улица", "Улица у источника", "Сосновая улица", "Бамбуковая улица", "Лунная улица", "Верхняя улица",
            "Улица усадеб", "Северная улица", "Улица облаков", "Улица у скал",
        };

        void TierStreets()
        {
            // The main way: from the main gate up the middle to the square, from the court on to the upper gate.
            Way("Главная дорога", new Vector2(AxisAlong, South), new Vector2(AxisAlong, 0), 6, true, true, 0);
            Way("Дорога к павильону", new Vector2(AxisAlong, 0), new Vector2(AxisAlong, 58), 6, true, true, -1);
            Way("Северная дорога", new Vector2(AxisAlong, 106), new Vector2(AxisAlong, North), 5, true, true, 0);

            // Each terrace's lane along it.
            var lowest = lanes.Count > 0 ? lanes.Min(l => l.terrace) : 0;

            foreach (var (terrace, line) in lanes)
            {
                PolyWay(LaneNames[Mathf.Clamp(terrace - lowest, 0, LaneNames.Length - 1)], line, 3, false, false, 1);
            }

            // The side gates' ways in to their terrace's lane.
            foreach (var gate in Gates.Where(g => g.Name == "gate west" || g.Name == "gate east"))
            {
                var local = Local(gate.At - gate.Out * (WallStrip + 1));

                if (NearestLane(local, TerraceAt(local), 120, out var lane))
                {
                    Way(gate.Name == "gate west" ? "Западная дорога" : "Восточная дорога", Local(gate.At), lane, 4, true, true, 2);
                }
            }

            // From each stair's foot and head to the lanes of the terraces below and above it.
            foreach (var (foot, up, _, run, low, _) in flights.ToList())
            {
                var below = Local(foot - up * 2);
                var above = Local(foot + up * (run + WallBody + 1.5f));
                var lowTerrace = Mathf.RoundToInt((low - levelBase) / Rise);

                if (NearestLane(below, lowTerrace, 40, out var a) && Vector2.Distance(a, below) > 1.5f)
                {
                    Way("Переулок", below, a, 2.5f, false, false, 3);
                }

                if (NearestLane(above, lowTerrace + 1, 45, out var b) && Vector2.Distance(b, above) > 1.5f)
                {
                    Way("Переулок", above, b, 2.5f, false, false, 3);
                }
            }
        }

        /// <summary>Where a street crosses a channel, a stone bridge (the street's width, the channel's span and more).</summary>
        void TierBridges()
        {
            foreach (var street in Streets)
            {
                for (var s = 0; s + 1 < street.Points.Count; s++)
                {
                    var a = Local(street.Points[s]);
                    var b = Local(street.Points[s + 1]);

                    foreach (var channel in Channels)
                    {
                        for (var i = 0; i + 1 < channel.Points.Count; i++)
                        {
                            var c = Local(channel.Points[i]);
                            var d = Local(channel.Points[i + 1]);

                            if (!Crossing(a, b, c, d, out var at))
                            {
                                continue;
                            }

                            var way = (b - a).normalized;
                            var world = W(at);

                            if (Bridges.Any(x => Vector2.Distance(x.Centre, world) < 4))
                            {
                                continue;
                            }

                            Bridges.Add(new Bridge { Centre = world, Half = new Vector2(street.Half + 0.4f, channel.Half + 1.4f), Yaw = Facing(way), Top = channel.Top + 0.25f });
                        }
                    }
                }
            }

            BuildLog.Write($"plan of {Place.Id}: {Channels.Count} channels, {Pools.Count} ponds and basins, {Falls.Count} falls, {Bridges.Count} bridges");
        }

        static bool Crossing(Vector2 a, Vector2 b, Vector2 c, Vector2 d, out Vector2 at)
        {
            at = Vector2.zero;
            var r = b - a;
            var s = d - c;
            var cross = r.x * s.y - r.y * s.x;

            if (Mathf.Abs(cross) < 1e-4f)
            {
                return false;
            }

            var t = ((c.x - a.x) * s.y - (c.y - a.y) * s.x) / cross;
            var v = ((c.x - a.x) * r.y - (c.y - a.y) * r.x) / cross;

            if (t < 0 || t > 1 || v < 0 || v > 1)
            {
                return false;
            }

            at = a + r * t;

            return true;
        }

        // ── What stands on the squares, at the gates, along the ways ──

        /// <summary>A thing of the procedural kit (PropKit) where nothing stands within `radius` (and off the streets unless `onStreet`).</summary>
        bool Thing(string what, Vector2 at, float yaw, float radius = 0.6f, bool onStreet = false, float scale = 1)
        {
            var free = CellOf(at) >= 0;
            Around(at, radius, (cell, _) => free &= !taken[cell] && kind[cell] != KWild && (onStreet || (kind[cell] != KLane && kind[cell] != KPaved)));

            if (!free)
            {
                return false;
            }

            Props.Add(new Prop { Kind = what, At = Flat(at), Yaw = yaw, Scale = scale, Seed = random.Next() });
            Around(at, radius, (cell, _) => taken[cell] = true);

            return true;
        }

        void TierSquares()
        {
            var east = Facing(East_);
            var west = Facing(West_);
            var south = Facing(South_);
            var north = Facing(North_);
            var s = Local(Square.Centre);

            // The square: three stalls (the noodles, the rice, Baoshan's Gu) facing the way across it, their goods beside them.
            var stalls = new[] { (W(s.x - 19, s.y - 7), east), (W(s.x - 19, s.y + 5), east), (W(s.x + 19, s.y - 7), west) };

            for (var i = 0; i < stalls.Length; i++)
            {
                var (at, yaw) = stalls[i];
                Thing("stall", at, yaw, 1.7f, true);
                Places.Add(($"stall {i}", Offset(at, yaw, 0, -1.5f), yaw));
            }

            Decor("prop_market_goods", W(s.x - 23, s.y - 1), east, 0.8f, true, true);
            Decor("prop_market_goods", W(s.x + 23, s.y - 1), west, 0.8f, true, true);
            // The square's well, the old camphor with its ring of stones to sit on, stone benches, the notice board (the
            // merit board of the tide) and the drum, stone lanterns at the corners.
            Decor("prop_village_well", W(s.x + 19, s.y + 7), west, 1, true, true);
            Places.Add(("well", W(s.x + 16.4f, s.y + 7), east));
            var tree = W(s.x - 15, s.y + 10);
            Thing("old tree", tree, R() * 360, 3.4f, true);

            for (var i = 0; i < 4; i++)
            {
                var seat = tree + (Along * Mathf.Cos(i * Mathf.PI / 2) - Down * Mathf.Sin(i * Mathf.PI / 2)) * 3.2f;
                Places.Add(($"bench {i}", seat, YawFacing(tree - seat) + 180));
            }

            Decor("prop_bench_stone", W(s.x + 11, s.y + 12), south, 1, true, true);
            Decor("prop_bench_stone", W(s.x + 23, s.y + 12), south, 1, true, true);
            Thing("notice board", W(s.x - 10, s.y - 12), south, 0.8f, true);
            Thing("drum", W(s.x + 10, s.y - 12), south, 0.9f, true);

            foreach (var (a, u) in new[] { (-31f, -12f), (31f, -12f), (-31f, 12f), (31f, 12f) })
            {
                Thing("stone lantern", W(s.x + a, s.y + u), south, 0.6f, true);
            }

            // The gate square: goods, a stele of the clan facing the gate.
            var g = Local(Pads.First(p => p.Name == "gate square").Centre);
            Decor("prop_clan_stele", W(g.x, g.y + 10), south, 1, true, true);
            Places.Add(("market", W(g.x + 20, g.y), west));

            for (var i = 0; i < 6; i++)
            {
                Decor("prop_market_goods", W(g.x - 30 + i * 12, g.y - 6), north, 0.8f, true, true);
            }

            // The forecourt before the great hall: bronze dings and steles either side of the way up, lotus vats and
            // pines before the hall, banners.
            var f = Local(Pads.First(p => p.Name == "forecourt").Centre);

            foreach (var side in new[] { -1, 1 })
            {
                Decor("prop_bronze_ding", W(AxisAlong + side * 18, f.y - 4), south, 1, true, true);
                Decor("prop_clan_stele", W(AxisAlong + side * 30, f.y + 1), south, 1, true, true);
                Decor("prop_lotus_vat", W(AxisAlong + side * 11, f.y + 4), south, 1, true, true);
                Decor("prop_bonsai_pine", W(AxisAlong + side * 14, f.y + 4), south, 1, false, true);
                Decor("prop_lotus_vat", W(AxisAlong + side * 46, f.y + 4), south, 1, true, true);
                Thing("banner", W(AxisAlong + side * 40, f.y + 5), south, 0.5f, true);
            }

            // The academy's yard: weapon racks along its lower side, go tables by the hall, room in the middle.
            var y = Local(Pads.First(p => p.Name == "academy yard").Centre);

            foreach (var a in new[] { -32f, -22f, 22f, 32f })
            {
                Decor("prop_weapon_rack", W(y.x + a, y.y - 10), north, 1, true, true);
            }

            Decor("prop_go_table", W(y.x - 30, y.y + 9), south, 1, true, true);
            Decor("prop_go_table", W(y.x + 30, y.y + 9), south, 1, true, true);

            // Inside each gate: a weapon rack and the spears by one side of the way, a brazier by the other.
            foreach (var gate in Gates)
            {
                var inward = -gate.Out;
                var side = new Vector2(-inward.y, inward.x);
                var half = gate.Name == "gate south" ? 7 : 6;
                Decor("prop_weapon_rack", gate.At + inward * 17 + side * (half + 3), YawFacing(-side), 1, true);
                Thing("spears", gate.At + inward * 20 + side * (half + 2.5f), YawFacing(-side), 0.8f);
                Thing("brazier", gate.At + inward * 17 - side * (half + 2.5f), 0, 0.8f);
            }
        }

        /// <summary>The families' quarters behind bamboo fences (not along the fort's wall): a moon gate in the middle of the lower side and wherever a lane goes through.</summary>
        void TierFences()
        {
            var length = QingMaoBuilder.VillageGlb["prop_bamboo_fence"].Size.x;

            foreach (var centre in new[] { new Vector2(-320, 262), new Vector2(400, 262) })
            {
                var min = centre - new Vector2(93, 38);
                var max = centre + new Vector2(93, 34);
                var sides = new[] { (new Vector2(min.x, min.y), new Vector2(max.x, min.y)), (new Vector2(min.x, min.y), new Vector2(min.x, max.y)), (new Vector2(max.x, min.y), new Vector2(max.x, max.y)) };

                foreach (var (from, to) in sides)
                {
                    var span = Vector2.Distance(from, to);
                    var dir = (to - from) / span;
                    var yaw = Facing(new Vector2(dir.y, -dir.x));

                    for (var s = length / 2; s < span - length / 2; s += length)
                    {
                        var local = from + dir * s;
                        var at = W(local);
                        var k = KindAt(at);

                        if (k == KLane || k == KPaved || (from.y == to.y && Mathf.Abs(local.x - centre.x) < length))
                        {
                            Decor("prop_moon_gate", at, yaw, 0.9f, true, true);
                            continue;
                        }

                        Decor("prop_bamboo_fence", at, yaw, 1, true);
                    }
                }
            }
        }

        /// <summary>Lanterns (the artist's posts) along the main way both sides, along the lanes now and then, by each stair's foot and head.</summary>
        void TierLanterns()
        {
            void Post(Vector2 at, float yaw)
            {
                // Not on another's foot (the double stairs' inner posts fall together).
                if (!ModelProps.Any(m => m.model == "prop_moon_lantern_post" && Vector2.Distance(m.at, at) < 2.6f))
                {
                    Decor("prop_moon_lantern_post", at, yaw, 1, true, true);
                }
            }

            foreach (var (from, to) in new[] { (South + 20f, -3f), (108f, North - 16f) })
            {
                for (var u = from; u < to; u += 14)
                {
                    foreach (var side in new[] { -1, 1 })
                    {
                        var at = W(AxisAlong + side * 7.2f, u);
                        var cell = CellOf(at);

                        if (cell >= 0 && stairNear[cell] > 4)
                        {
                            Post(at, Facing(side > 0 ? West_ : East_));
                        }
                    }
                }
            }

            foreach (var (_, line) in lanes)
            {
                var points = line.Select(p => W(p)).ToList();
                var length = Length(points);

                for (float s = 10 + R() * 20, side = 1; s < length - 10; s += 28 + R() * 22, side = -side)
                {
                    var p = PointAt(points, s, out var tangent);
                    var normal = new Vector2(-tangent.y, tangent.x) * side;
                    var cell = CellOf(p + normal * 3.6f);

                    // Not before a stair's foot or over its head.
                    if (cell >= 0 && stairNear[cell] > 14)
                    {
                        Post(p + normal * 3.6f, YawFacing(-normal));
                    }
                }
            }

            foreach (var (foot, up, half, run, _, _) in flights)
            {
                var across = new Vector2(up.y, -up.x);

                foreach (var side in new[] { -1, 1 })
                {
                    Post(foot - up * 1.5f + across * side * (half + 1.6f), YawFacing(-up));
                    Post(foot + up * (run + WallBody + 1.5f) + across * side * (half + 1.6f), YawFacing(-up));
                }
            }
        }

        // ── Houses ────────────────────────────────────────────────────

        void TierWishes()
        {
            void Want(BuildingKind kind, string name, string model, float scale, float along, float up, float radius = 45) =>
                wishes.Add(new Wish { Kind = kind, Name = name, Model = model, Scale = scale, Target = W(along, up), Radius = radius });

            // Round the court and the gate square: the trades people walk to.
            Want(BuildingKind.Tavern, "Харчевня", "prop_eatery", 0.85f, -75, 5);
            Want(BuildingKind.Teahouse, "Чайная", "prop_teahouse", 0.85f, 150, -45);
            Want(BuildingKind.Shop, "Лавка", "prop_shop", 0.9f, -40, -128);
            Want(BuildingKind.Shop, "Лавка", "prop_shop", 0.9f, 125, -128);
            Want(BuildingKind.Shop, "Лавка", "prop_shop", 0.9f, -80, -50);
            Want(BuildingKind.Herbalist, "Лавка травника", "prop_herbalist", 0.9f, -330, -90);
            // Three taverns, as the clan has (the bamboo wine is the inn's alone): one among the masters, one by the crafts.
            Want(BuildingKind.Tavern, "Таверна «Рисовое вино»", "prop_tavern", 0.85f, 330, 160, 50);
            Want(BuildingKind.Tavern, "Таверна «Мутное вино»", "prop_tavern", 0.85f, 380, -100, 50);
            // The crafts: smithies, the clan's granaries along the east wall; the poor's eatery.
            Want(BuildingKind.Smithy, "Кузница", "prop_smithy", 0.9f, 340, -50, 45);
            Want(BuildingKind.Smithy, "Кузница", "prop_smithy", 0.9f, 450, -30, 45);
            Want(BuildingKind.Smithy, "Оружейная кузница", "prop_smithy", 0.9f, 520, -130, 45);

            foreach (var u in new[] { -150f, -95f, -40f })
            {
                Want(BuildingKind.Granary, "Амбар клана", "prop_granary", 0.9f, 545, u, 45);
            }

            Want(BuildingKind.Tavern, "Харчевня для бедных", "prop_eatery", 0.8f, -430, -100, 50);
            Want(BuildingKind.Shop, "Лавка", "prop_shop", 0.9f, -320, 150, 50);
            Want(BuildingKind.Shop, "Лавка", "prop_shop", 0.9f, 350, 60, 50);
            Want(BuildingKind.Teahouse, "Чайная", "prop_teahouse", 0.85f, -430, 150, 50);
        }

        /// <summary>The quarter a spot (the village's frame) is in; their edges wander.</summary>
        enum Quarter { Head, Family, Academy, Healers, Masters, Market, Mortals, Crafts }

        static Quarter QuarterAt(Vector2 local)
        {
            var a = local.x + MountainShape.Fbm(local.x * 0.012f + 9, local.y * 0.012f + 4, 2) * 50;
            var u = local.y + MountainShape.Fbm(local.x * 0.012f + 2, local.y * 0.012f + 13, 2) * 36;

            if (a > -60 && a < 140 && u > -25 && u < 140)
            {
                return Quarter.Head;
            }

            if (u > 205 && (a < -200 || a > 290))
            {
                return Quarter.Family;
            }

            if (a > -270 && a < -60 && u > -25 && u < 150)
            {
                return Quarter.Academy;
            }

            if (a > 140 && a < 300 && u > -25 && u < 150)
            {
                return Quarter.Healers;
            }

            if (u < -60)
            {
                return a < -130 ? Quarter.Mortals : a > 220 ? Quarter.Crafts : Quarter.Market;
            }

            return u < 30 && a < -280 ? Quarter.Mortals : u < 30 && a > 320 ? Quarter.Crafts : Quarter.Masters;
        }

        /// <summary>What may stand on a lot: the artist's model (none: a mortal's hut of the procedural kit), its scale and stretch, its kind, the trade it is, its footprint; plain: never turned side on or mirrored (a sign on its front).</summary>
        class Lot
        {
            public string Model;
            public float Scale = 1;
            public Vector3 Stretch = Vector3.one;
            public BuildingKind Kind;
            public Wish Wish;
            public Vector2 Size;
            public bool Plain;
        }

        /// <summary>A model on a lot: stretched a little each way, mirrored half the time (unless plain).</summary>
        Lot Pick(string model, float scale, BuildingKind kind, bool plain = false)
        {
            var stretch = plain ? Vector3.one : new Vector3((0.9f + R() * 0.22f) * (R() < 0.5f ? -1 : 1), 0.93f + R() * 0.15f, 0.9f + R() * 0.22f);
            var size = QingMaoBuilder.VillageGlb[model].Size * scale;

            return new Lot { Model = model, Scale = scale, Stretch = stretch, Kind = kind, Size = new Vector2(size.x * Mathf.Abs(stretch.x), size.y * stretch.z), Plain = plain };
        }

        /// <summary>A mortal's hut of the procedural kit (thatched, crooked, as in the hamlets).</summary>
        Lot Hut() => new Lot { Model = null, Kind = BuildingKind.Poor, Size = new Vector2(5.6f + R() * 3, 4.8f + R() * 2.4f) };

        /// <summary>One of several lots by their weights.</summary>
        Lot OneOf(params (float weight, System.Func<Lot> make)[] options)
        {
            var pick = R() * options.Sum(o => o.weight);

            foreach (var (weight, make) in options)
            {
                if ((pick -= weight) <= 0)
                {
                    return make();
                }
            }

            return options[options.Length - 1].make();
        }

        /// <summary>
        /// What may stand on a lot here, the first that fits: a wanted trade near its spot; else one by the quarter's
        /// mix (the agent's table, 2026-10-25) — the head's and the families' people in bamboo houses, long houses,
        /// plastered shop-houses; the masters' a mix of all with their workshops; the market's shops; the mortals' and
        /// the crafts' poor houses, huts, sheds, stores and smithies; else something smaller.
        /// </summary>
        IEnumerable<Lot> LotChoices(Vector2 local, bool main)
        {
            var world = W(local);
            var wish = wishes.Where(w => !w.Done && Vector2.Distance(w.Target, world) < w.Radius).OrderBy(w => Vector2.Distance(w.Target, world)).FirstOrDefault();

            if (wish != null)
            {
                var size = QingMaoBuilder.VillageGlb[wish.Model].Size * wish.Scale;

                yield return new Lot { Model = wish.Model, Scale = wish.Scale, Kind = wish.Kind, Wish = wish, Size = size, Plain = true };
            }

            Lot Small() => Pick(SmallShops[random.Next(SmallShops.Length)], 1, BuildingKind.Shop, true);
            var quarter = QuarterAt(local);

            // Three tries of the quarter's mix (a big one may not fit where a smaller would), then something small.
            for (var i = 0; i < 3; i++)
            {
                yield return Mix();
            }

            yield return OneOf((40, Hut), (30, () => Pick("prop_poor_house", 0.68f + R() * 0.08f, BuildingKind.Poor)), (30, Small));
            yield return Hut();

            Lot Mix()
            {
                switch (quarter)
                {
                case Quarter.Head:
                case Quarter.Family:
                    return OneOf(
                        (50, () => Pick("guyue_bamboo_house", 0.8f + R() * 0.15f, BuildingKind.Family)),
                        (15, () => Pick("prop_poor_house", 0.85f + R() * 0.15f, BuildingKind.Family)),
                        (12, () => Pick("prop_academy_dorm", 0.58f + R() * 0.08f, BuildingKind.Family)),
                        (10, () => Pick("prop_shop_small_a", 1, BuildingKind.Family)),
                        (8, () => Pick("prop_eatery", 0.65f, BuildingKind.Family, true)),
                        (5, () => Pick("prop_granary", 0.65f, BuildingKind.Shed)));
                case Quarter.Market:
                    return OneOf(
                        (main ? 35 : 20, () => Pick("prop_shop", 0.9f, BuildingKind.Shop, true)),
                        (35, Small),
                        (15, () => Pick("guyue_bamboo_house", 0.75f, BuildingKind.Family)),
                        (10, () => Pick("prop_poor_house", 0.9f, BuildingKind.Poor)),
                        (5, () => Pick("prop_herbalist", 0.85f, BuildingKind.Herbalist, true)),
                        (5, () => Pick("prop_eatery", 0.7f, BuildingKind.Family, true)));
                case Quarter.Mortals:
                    return OneOf(
                        (40, () => Pick("prop_poor_house", 0.75f + R() * 0.25f, BuildingKind.Poor)),
                        (30, Hut),
                        (10, () => Pick("prop_shop_small_c", 1, BuildingKind.Shop, true)),
                        (12, () => Pick("prop_poor_house", 0.5f + R() * 0.1f, BuildingKind.Shed)),
                        (8, () => Pick("prop_academy_dorm", 0.5f, BuildingKind.Poor)));
                case Quarter.Crafts:
                    return OneOf(
                        (30, () => Pick("prop_poor_house", 0.8f + R() * 0.2f, BuildingKind.Poor)),
                        (20, Hut),
                        (12, () => Pick("prop_granary", 0.7f, BuildingKind.Shed)),
                        (10, () => Pick("prop_academy_dorm", 0.6f, BuildingKind.Poor)),
                        (10, () => Pick("prop_shop_small_c", 1, BuildingKind.Shop, true)),
                        (8, () => Pick("prop_smithy", 0.7f, BuildingKind.Shed, true)),
                        (10, () => Pick("prop_poor_house", 0.5f + R() * 0.1f, BuildingKind.Shed)));
                default:
                    return OneOf(
                        (25, () => Pick("guyue_bamboo_house", 0.75f + R() * 0.12f, BuildingKind.Family)),
                        (main ? 25 : 18, Small),
                        (15, () => Pick("prop_poor_house", 0.85f + R() * 0.15f, BuildingKind.Family)),
                        (10, () => Pick("prop_shop", 0.85f, BuildingKind.Shop, true)),
                        (10, () => Pick("prop_academy_dorm", 0.6f, BuildingKind.Family)),
                        (5, () => Pick("prop_granary", 0.7f, BuildingKind.Shed)),
                        (5, () => Pick("prop_eatery", 0.65f, BuildingKind.Family, true)),
                        (5, () => Pick("prop_smithy", 0.6f, BuildingKind.Shed, true)));
                }
            }
        }

        void TierHouses()
        {
            TierWishes();

            foreach (var (line, half, _, main) in lotStreets.OrderBy(s => s.order))
            {
                Lots(line, half, 1, main);
                Lots(line, half, -1, main);
            }

            FillHouses();
            AssignTrades();
            BuildLog.Write($"plan of {Place.Id}: {Buildings.Count} buildings: " + string.Join(", ", Buildings.GroupBy(b => b.Model ?? "hut").OrderByDescending(g => g.Count()).Select(g => $"{g.Count()} {g.Key}")));
        }

        /// <summary>
        /// Houses along one side of a street (`side`: left or right of its way), as a village grows them: one after
        /// another, each turned to the street give or take a few degrees, now and then side on, set back by more or
        /// less, with gaps of a step or of a whole yard between; where one does not fit, a smaller, else on a little.
        /// </summary>
        void Lots(List<Vector2> line, float half, int side, bool main)
        {
            var length = Length(line);
            var s = R() * 4;

            while (s < length - 3)
            {
                var placed = 0f;
                var p = PointAt(line, s, out var t);

                foreach (var lot in LotChoices(Local(p + new Vector2(-t.y, t.x) * side * (half + 6)), main))
                {
                    var turned = !lot.Plain && lot.Wish == null && R() < 0.18f;
                    var front = turned ? lot.Size.y : lot.Size.x;
                    var deep = turned ? lot.Size.x : lot.Size.y;

                    if (s + front > length)
                    {
                        continue;
                    }

                    var mid = PointAt(line, s + front / 2, out var tangent);
                    var normal = new Vector2(-tangent.y, tangent.x) * side;
                    var centre = mid + normal * (half + Setback + R() * (main ? 1.5f : 3.5f) + deep / 2);
                    var jitter = (R() - 0.5f) * (lot.Model == null || lot.Kind == BuildingKind.Poor || lot.Kind == BuildingKind.Shed ? 26 : lot.Plain ? 6 : 14);
                    var yaw = YawFacing(-normal) + (turned ? (R() < 0.5f ? 90 : -90) : 0) + jitter;

                    if (!OneTerrace(centre, lot.Size / 2, yaw) || !Free(centre, lot.Size / 2, yaw, 0.9f))
                    {
                        continue;
                    }

                    AddHouse(centre, lot, yaw);
                    placed = front;
                    break;
                }

                if (placed <= 0)
                {
                    s += 1.5f;
                    continue;
                }

                var gap = 1.2f + R() * 3.5f;

                // Now and then a plot left empty: a yard, a garden, a tree.
                if (R() < 0.18f)
                {
                    gap += 6 + R() * 10;
                }

                s += placed + gap;
            }
        }

        /// <summary>More houses in the room the streets' rows leave, each facing the nearest lane (else down the mountain), turned as it likes.</summary>
        void FillHouses()
        {
            var count = 0;

            for (var tries = 0; tries < 16000 && count < 260; tries++)
            {
                var local = AnyWhere(16);
                var at = W(local);

                if (KindAt(at) != KGround || taken[CellOf(at)])
                {
                    continue;
                }

                var nearest = float.MaxValue;
                var towards = Vector2.zero;
                Around(at, 26, (cell, d) =>
                {
                    if ((kind[cell] == KLane || kind[cell] == KPaved) && d < nearest)
                    {
                        nearest = d;
                        towards = new Vector2(Area.x + cell % Resolution * spacing, Area.y + cell / Resolution * spacing) - at;
                    }
                });
                var facing = nearest < float.MaxValue ? YawFacing(towards) + (R() - 0.5f) * 40 : YawFacing(Down) + (R() - 0.5f) * 60;

                foreach (var lot in LotChoices(local, false).Where(l => l.Wish == null))
                {
                    if (OneTerrace(at, lot.Size / 2, facing) && Free(at, lot.Size / 2, facing, 1.8f))
                    {
                        AddHouse(at, lot, facing);
                        count++;
                        break;
                    }
                }
            }

            BuildLog.Write($"plan of {Place.Id}: {count} houses in the room behind the rows");
        }

        void AddHouse(Vector2 centre, Lot lot, float yaw)
        {
            var b = new Building
            {
                Kind = lot.Kind, Centre = centre, Size = lot.Size, Yaw = yaw, Floor = TierLevelAt(centre), Seed = random.Next(), Lit = lot.Wish != null || R() < 0.55f,
                Model = lot.Model, Scale = lot.Scale, Stretch = lot.Stretch, Mortal = lot.Model == null, Pad = lot.Model != null,
            };

            if (lot.Wish != null)
            {
                lot.Wish.Done = true;
                b.Name = lot.Wish.Name;
            }

            Buildings.Add(b);
            Take(centre, lot.Size / 2, yaw);

            if (lot.Kind == BuildingKind.Family || lot.Kind == BuildingKind.Poor)
            {
                Doorsteps.Add((Offset(centre, yaw, 0, lot.Size.y / 2 + 1.1f), yaw));
            }
        }

        // ── What lies about ───────────────────────────────────────────

        /// <summary>One of the artist's props off the streets, where nothing stands within `margin` of it.</summary>
        bool Model(string model, Vector2 at, float yaw, float scale = 1, bool solid = true, float margin = 0.6f)
        {
            var half = QingMaoBuilder.VillageGlb[model].Size * scale / 2;

            if (!Free(at, half, yaw, margin) || !OneTerrace(at, half, yaw))
            {
                return false;
            }

            ModelProps.Add((model, at, yaw, scale, solid));
            Inside(at, half, yaw, 0, cell => taken[cell] = true);

            return true;
        }

        /// <summary>A random spot inside the wall (the village's frame), `inset` m in from it.</summary>
        Vector2 AnyWhere(float inset) => new Vector2(Mathf.Lerp(West + inset, East - inset, R()), Mathf.Lerp(South + inset, North - inset, R()));

        void TierDecor()
        {
            // By each building's front and sides, what its trade or its household keeps there (never on the street);
            // by some houses a fence, a shed, firewood or a rack.
            foreach (var b in Buildings.ToList())
            {
                var hx = b.Size.x / 2;
                var front = b.Size.y / 2 + 0.6f;
                Vector2 At(float x, float z) => Offset(b.Centre, b.Yaw, x, z);
                var aside = R() < 0.5f ? 1 : -1;

                switch (b.Kind)
                {
                    case BuildingKind.Family:
                    case BuildingKind.Poor:
                    case BuildingKind.Shed:
                        for (var i = 0; i < random.Next(3); i++)
                        {
                            Thing(DoorThings[random.Next(DoorThings.Length)], At((R() < 0.5f ? -1 : 1) * (1.2f + R() * (hx - 1.4f)), front), b.Yaw + (R() - 0.5f) * 60, 0.4f);
                        }

                        var pick = R();

                        if (pick < (b.Kind == BuildingKind.Family ? 0.3f : 0.6f))
                        {
                            var thing = new[] { "prop_firewood_stack", "prop_drying_rack", "prop_barrels" }[random.Next(3)];
                            var scale = thing == "prop_barrels" ? 0.55f : 1;
                            var size = QingMaoBuilder.VillageGlb[thing].Size * scale;
                            Model(thing, At(aside * (hx + size.y / 2 + 0.6f), 0), b.Yaw + 90 * aside, scale, true, 0.4f);
                        }
                        else if (pick < 0.75f && b.Kind != BuildingKind.Shed)
                        {
                            // A shed beside it (the poor house small), or a fence along its side.
                            if (R() < 0.5f)
                            {
                                Model("prop_poor_house", At(aside * (hx + 2.8f), -b.Size.y * 0.15f), b.Yaw + (R() - 0.5f) * 30, 0.5f, true, 0.4f);
                            }
                            else
                            {
                                Model("prop_bamboo_fence", At(aside * (hx + 0.9f), -0.5f), b.Yaw + 90, 1, true, 0.3f);
                            }
                        }
                        else if (R() < 0.4f)
                        {
                            Thing(R() < 0.5f ? "firewood" : "water jar", At(aside * (hx + 0.7f), (R() - 0.5f) * b.Size.y * 0.6f), b.Yaw + 90, 0.6f);
                        }

                        break;
                    case BuildingKind.Shop:
                        Thing("crates", At(-hx + 0.8f, front), b.Yaw + R() * 20, 0.6f);
                        Thing("goods basket", At(hx - 0.8f, front), b.Yaw, 0.5f);
                        Thing("sacks", At(hx + 0.8f, 0), b.Yaw, 0.6f);
                        break;
                    case BuildingKind.Tavern:
                    case BuildingKind.Teahouse:
                        Thing(b.Kind == BuildingKind.Tavern ? "wine jars" : "water jar", At(hx + 0.8f, front - 1), b.Yaw, 0.6f);
                        Thing("bench", At(-hx + 1.2f, front), b.Yaw + 180, 0.7f);
                        Thing(b.Kind == BuildingKind.Tavern ? "barrel" : "flower pots", At(-hx - 0.8f, front - 1.2f), b.Yaw, 0.5f);
                        break;
                    case BuildingKind.Smithy:
                        Thing("anvil", At(hx + 1.2f, front - 1.5f), b.Yaw + 15, 0.6f);
                        Thing("charcoal", At(-hx - 1, 0), b.Yaw, 0.7f);
                        Model("prop_weapon_rack", At(hx + 1.6f, -1), b.Yaw - 90, 1, true, 0.4f);
                        break;
                    case BuildingKind.Herbalist:
                        Model("prop_drying_rack", At(hx + 1.4f, 0), b.Yaw + 90, 1, true, 0.4f);
                        Thing("jars", At(-hx + 0.6f, front), b.Yaw, 0.5f);
                        break;
                    case BuildingKind.Granary:
                        Thing("sacks", At(-hx - 0.8f, front - 1), b.Yaw, 0.6f);
                        Thing("cart", At(hx + 1.8f, 0), b.Yaw + 90, 1.6f);
                        break;
                    case BuildingKind.GuFarm:
                        for (var i = 0; i < 4; i++)
                        {
                            Thing("gu pots", At(hx + 1.2f, -b.Size.y / 2 + 1.5f + i * 2.2f), b.Yaw, 0.6f);
                        }

                        break;
                    case BuildingKind.Gambling:
                        Thing("red lantern pole", At(hx + 0.8f, front - 0.6f), b.Yaw, 0.4f);
                        break;
                    case BuildingKind.Inn:
                        Thing("cart", At(hx + 1.8f, 0), b.Yaw + 90, 1.6f);
                        Thing("hay", At(-hx - 1.4f, 0), b.Yaw, 1.1f);
                        break;
                }
            }

            // Behind the houses their yards: a vegetable bed the house's width (fenced low), or a tree, or bamboo.
            foreach (var b in Buildings.Where(b => b.Kind == BuildingKind.Family || b.Kind == BuildingKind.Poor).ToList())
            {
                var pick = R();
                var back = b.Size.y / 2 + 1.2f;

                if (pick < 0.35f)
                {
                    foreach (var depth in new[] { 2.2f, 1.6f, 1.1f })
                    {
                        var half = new Vector2(b.Size.x / 2 - 1.2f, depth);
                        var at = Offset(b.Centre, b.Yaw, 0, -(back + depth));

                        if (half.x > 1 && Free(at, half, b.Yaw, 0.6f) && OneTerrace(at, half, b.Yaw))
                        {
                            Gardens.Add((at, half, b.Yaw));
                            Take(at, half, b.Yaw, KGarden);
                            break;
                        }
                    }
                }
                else if (pick < 0.65f)
                {
                    Thing(R() < 0.3f ? "pine tree" : "tree", Offset(b.Centre, b.Yaw, (R() - 0.5f) * b.Size.x * 0.5f, -(back + 1.4f)), R() * 360, 1.4f, false, 0.85f + R() * 0.3f);
                }
                else if (pick < 0.75f)
                {
                    Model("prop_bamboo_grove", Offset(b.Centre, b.Yaw, 0, -(back + 3.4f)), b.Yaw, 0.75f, false, 0.6f);
                }
            }

            // A well for every few hundred houses' streets, bamboo along the walls where room is left, trees.
            var wells = new List<Vector2>();

            for (var tries = 0; tries < 3000 && wells.Count < 22; tries++)
            {
                var at = W(AnyWhere(20));

                if (wells.Any(w => Vector2.Distance(w, at) < 80) || Vector2.Distance(at, Square.Centre) < 60)
                {
                    continue;
                }

                if (Model("prop_village_well", at, Facing(South_), 1, true, 1.2f))
                {
                    wells.Add(at);
                }
            }

            for (int tries = 0, groves = 0; tries < 6000 && groves < 90; tries++)
            {
                var at = W(AnyWhere(14));

                if (InsideDistance(at) < 45 && Model("prop_bamboo_grove", at, Facing(South_), 0.8f, false, 0.8f))
                {
                    groves++;
                }
            }

            for (int tries = 0, trees = 0; tries < 4000 && trees < 110; tries++)
            {
                if (Thing(R() < 0.3f ? "pine tree" : "tree", W(AnyWhere(14)), R() * 360, 2, false, 0.85f + R() * 0.4f))
                {
                    trees++;
                }
            }

            // Trees along the tops of the retaining walls now and then (a view down over the town); plums round the court.
            foreach (var (_, line, _, up, _) in terraceWalls)
            {
                var length = Length(line);

                for (var s = 8 + R() * 20; s < length - 4; s += 16 + R() * 26)
                {
                    var at = PointAt(line, s, out _);
                    var i = Mathf.Clamp(Mathf.RoundToInt(s / Mathf.Max(0.1f, length) * (line.Count - 1)), 0, line.Count - 1);
                    var spot = at + up[i] * (WallBody - WallTuck + 1.6f);

                    if (RectOutside(spot.x, spot.y) < -(WallStrip + 3))
                    {
                        Thing(QuarterAt(spot) == Quarter.Head ? "tree" : R() < 0.4f ? "pine tree" : "tree", W(spot), R() * 360, 1.0f, false, 0.8f + R() * 0.3f);
                    }
                }
            }

            // Fallen leaves about the streets and yards.
            for (var i = 0; i < 2200; i++)
            {
                var at = W(AnyWhere(8));
                var cell = CellOf(at);

                if (cell >= 0 && !taken[cell] && kind[cell] != KWild && kind[cell] != KPaved)
                {
                    Props.Add(new Prop { Kind = "leaves", At = Flat(at), Yaw = R() * 360, Scale = 0.6f + R() * 0.8f, Seed = random.Next() });
                }
            }
        }

        /// <summary>Vegetable gardens in the room the houses leave, fenced low; the healers' herb beds in rows.</summary>
        void TierGardens()
        {
            // The healers' beds between their hall and their nursery.
            for (var a = 178f; a < 255; a += 12)
            {
                for (var u = 52f; u < 88; u += 7)
                {
                    var at = W(a, u);
                    var half = new Vector2(4.6f, 2.2f);

                    if (Free(at, half, Facing(North_), 0.6f) && OneTerrace(at, half, Facing(North_)))
                    {
                        Gardens.Add((at, half, Facing(North_)));
                        Take(at, half, Facing(North_), KGarden);
                    }
                }
            }

            for (var tries = 0; tries < 6000 && Gardens.Count < 520; tries++)
            {
                var p = W(AnyWhere(14));
                var half = new Vector2(2.4f + R() * 1.6f, 1.4f + R() * 0.8f);
                var yaw = Facing(R() < 0.5f ? South_ : East_) + (R() - 0.5f) * 30;

                if (KindAt(p) != KGround || !Free(p, half, yaw, 0.8f) || !OneTerrace(p, half, yaw))
                {
                    continue;
                }

                Gardens.Add((p, half, yaw));
                Take(p, half, yaw, KGarden);
            }
        }
    }
}
