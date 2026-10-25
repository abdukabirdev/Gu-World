using System.Collections.Generic;
using System.Linq;
using GuWorld.World;
using UnityEngine;

namespace GuWorld.Editor
{
    /// <summary>
    /// Gu Yue as a terraced town (2026-10-25; the user: six times bigger, two or three tiers on the mountain's side, the
    /// stairs we made put to use, streams and the rain led off, each faction of the clan its own quarter; planned by
    /// two agents: Claude/plans/guyue-2026-10-25*.md). A rectangle 1080 by 500 m across the slope, cut and banked into
    /// three level tiers 27.06 m apart (one tall stair of the artist's, three flights, climbs each), held by stone
    /// retaining walls with a parapet; a stone plinth under the lowest tier's front, a rock cut behind the highest.
    /// Inside the fort's wall (a tower on each corner, every ~70 m and where the side walls step up a tier), seven
    /// gates: the main one below, its forecourt on the plinth with a ramp down along the plinth each side; two on each
    /// side, one per tier, each on the line where the tier meets the mountain's own ground.
    ///
    /// The tiers, from the top: the clan head's court in the middle (the great hall over the axis, the spring's pavilion,
    /// the elders' and the ancestors' halls), the Mo family's quarter west of it and the Chi family's east, behind
    /// bamboo fences. The middle tier: the middle square at the stairs' head, the academy and its yard west of it, the
    /// healers' hall and their beds east of it, the masters' quarters beyond. The lowest: the gate square and the
    /// market by the main gate, the mortals' and servants' quarter to the west round the lotus pond, the crafts and the
    /// clan's granaries to the east.
    ///
    /// Water: the spring rises under its pavilion on the top tier and runs down the west channel, falling over each
    /// retaining wall into a stone basin, to the lotus pond and out of the west water gate; the golden channel crosses
    /// the head's forecourt to the east channel, which falls the same way to the east water gate. Gutters at the foot
    /// of every retaining wall and of the rock cut take the rain to the channels.
    ///
    /// The village's own frame: along the slope (Along, "east") and uphill (-Down, "north"), from the settlement's middle.
    /// </summary>
    public partial class VillagePlan
    {
        // The rectangle the fort's wall stands on, m (the earthworks reach out round it).
        const float West = -500, East = 580, South = -190, North = 310;
        /// <summary>The main way's line: the gate, the stairs, the great hall.</summary>
        const float AxisAlong = 40;
        /// <summary>The retaining walls' faces (uphill): tier k lies between the k-1'th and the k'th.</summary>
        static readonly float[] TierEdge = { -20, 145 };
        /// <summary>Each tier's street from its west gate to its east one.</summary>
        static readonly float[] TierStreet = { -120, 45, 190 };
        /// <summary>The channels' lines (along): the spring's on the west, the golden one's on the east.</summary>
        const float WestWater = -59, EastWater = 270;
        /// <summary>Where the lotus pond's water leaves the town (along), through the west water gate.</summary>
        const float OutflowWest = -265;
        /// <summary>The level strip kept outside the wall before the ground goes to the mountain's own, m.</summary>
        const float Berm = 6;
        /// <summary>The lane round the inside of the wall: its middle's distance from the wall's line, its half width.</summary>
        const float RingInset = 11, RingHalf = 3;
        /// <summary>The strip inside the wall's line kept for it (its own depth, its gates', its towers'), m.</summary>
        const float WallStrip = 7.5f;
        /// <summary>The steepest a side gate's road may ramp to the mountain, and the main gate's ramps (rise over run).</summary>
        const float RampGrade = 0.15f, LongRamp = 0.12f;
        /// <summary>How far a house's front stands back from its street's edge, m.</summary>
        const float Setback = 1.5f;
        /// <summary>A retaining wall's depth behind its face, and where under it the ground steps (hidden in it), m.</summary>
        const float WallBody = 1.6f, WallTuck = 0.8f;
        /// <summary>The main gate's forecourt on the plinth: half its width, its depth out from the plinth's face.</summary>
        const float CourtHalf = 15, CourtDepth = 24;
        /// <summary>The main gate's ramps: their width along the plinth's face.</summary>
        const float RampWidth = 7;
        /// <summary>How high the parapets stand over their walls' tops, m.</summary>
        const float ParapetHeight = 0.9f;

        /// <summary>The tiers' levels, lowest first, and the rise between two (one tall stair).</summary>
        readonly float[] levels = new float[3];
        float rise;

        /// <summary>Whether this is the terraced town (Gu Yue's): its wall is built by its rectangle (QingMaoBuilder.BuildFortRect).</summary>
        public bool LevelGround => Glb;

        /// <summary>How far outside the boundary the town's earthworks reach: no giant tree nearer.</summary>
        public float Margin => Glb ? 160 : 22;

        /// <summary>Where the fort's side walls step up a tier: the point on the wall's line, the levels below and above.</summary>
        public readonly List<(Vector2 at, float low, float high)> WallSteps = new List<(Vector2, float, float)>();

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

        /// <summary>The streets houses stand along, in the village's frame (from, to, half width), the main ones first.</summary>
        readonly List<(Vector2 from, Vector2 to, float half, int order)> lotStreets = new List<(Vector2, Vector2, float, int)>();

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

        /// <summary>The tall stairs, in the village's frame: the middle of the line up them, half their width, their foot's and top's `up`, the levels.</summary>
        readonly List<(float along, float half, float foot, float top, float low)> flights = new List<(float, float, float, float, float)>();

        static readonly Vector2 East_ = Vector2.right, West_ = Vector2.left, North_ = Vector2.up, South_ = Vector2.down;

        Vector2 W(float along, float up) => World(along, up);

        Vector2 W(Vector2 local) => World(local.x, local.y);

        /// <summary>The turn that faces a way given in the village's frame (x along, y uphill).</summary>
        float Facing(Vector2 local) => YawFacing(Along * local.x - Down * local.y);

        /// <summary>A point a turned thing's own (x across its front, z out of its front) puts at, from its middle.</summary>
        static Vector2 Offset(Vector2 centre, float yaw, float x, float z)
        {
            var v = Quaternion.Euler(0, yaw, 0) * new Vector3(x, 0, z);

            return centre + new Vector2(v.x, v.z);
        }

        /// <summary>Which tier a spot uphill `u` is on (where things stand: the retaining walls' faces divide them).</summary>
        static int TierAt(float u) => u < TierEdge[0] ? 0 : u < TierEdge[1] ? 1 : 2;

        /// <summary>Which tier the ground at `u` is of: it steps up under each retaining wall, hidden in its body.</summary>
        static int GroundTier(float u) => u < TierEdge[0] + WallTuck ? 0 : u < TierEdge[1] + WallTuck ? 1 : 2;

        /// <summary>The level a thing standing at a point stands on (its tier's).</summary>
        public float TierLevelAt(Vector2 p) => Glb ? levels[TierAt(Local(p).y)] : Level0;

        void LayOutTiers()
        {
            rise = QingMaoBuilder.VillageGlb["prop_stairs_tall"].Height;
            // The lowest level such that what is dug out of all three tiers fills what is banked up (the tiers 27 m apart).
            var sum = 0f;
            var count = 0;

            for (var a = West; a <= East; a += 10)
            {
                for (var u = South; u <= North; u += 10)
                {
                    var p = W(a, u);
                    sum += natural(p.x, p.y) - TierAt(u) * rise;
                    count++;
                }
            }

            for (var k = 0; k < 3; k++)
            {
                levels[k] = sum / count + k * rise;
            }

            Level0 = levels[0];
            Boundary.Add(W(West, South));
            Boundary.Add(W(East, South));
            Boundary.Add(W(East, North));
            Boundary.Add(W(West, North));

            foreach (var side in new[] { West, East })
            {
                for (var k = 0; k < 2; k++)
                {
                    WallSteps.Add((W(side, TierEdge[k] + WallTuck), levels[k], levels[k + 1]));
                }
            }

            BuildLog.Write($"plan of {Place.Id}: tiers at {levels[0]:0.0}, {levels[1]:0.0}, {levels[2]:0.0} m");
            void Phase(string what) => BuildLog.Mark($"plan of {Place.Id}: {what}");
            Rasters();
            TierGates();
            TierWalls();
            Phase("tiers, walls and gates");
            TierWater();
            TierStairs();
            TierGreatBuildings();
            TierPads();
            TierStreets();
            TierBridges();
            Phase("water, stairs, great buildings, squares, streets");
            TierSquares();
            TierFences();
            TierLanterns();
            TierHouses();
            Phase("houses");
            TierDecor();
            TierGardens();
            Phase("props");
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
        /// The ground's height: each tier level to its retaining wall (stepping up hidden in the wall's body) and over
        /// the berm outside the fort's wall; under the front, the plinth (the mountain's own ground before its face, the
        /// main gate's forecourt and ramps on it); round the rest, a rock cut up to the mountain's ground or an earth bank
        /// down to it; the gates' roads; the channels, ponds, basins and gutters cut in.
        /// </summary>
        float TierHeight(Vector2 p, float naturalHeight)
        {
            var local = Local(p);
            var a = local.x;
            var u = local.y;
            var level = levels[GroundTier(u)];
            var face = South - Berm;
            float height;

            if (u < face + WallTuck && a > West - Berm && a < East + Berm)
            {
                height = PlinthGround(a, u, naturalHeight);
            }
            else
            {
                var outside = RectOutside(a, u);
                height = outside <= Berm ? level : Bank(outside - Berm, naturalHeight, level);

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
            }

            return Carve(local, height);
        }

        /// <summary>The ground before the plinth's face: the mountain's, but the main gate's forecourt (the lowest level) and its two ramps down along the face.</summary>
        float PlinthGround(float a, float u, float naturalHeight)
        {
            var face = South - Berm;

            if (a > AxisAlong - CourtHalf + WallTuck && a < AxisAlong + CourtHalf - WallTuck && u > face - CourtDepth + WallTuck)
            {
                return levels[0];
            }

            if (u > face - RampWidth + WallTuck)
            {
                var off = a < AxisAlong ? AxisAlong - CourtHalf - a : a - AxisAlong - CourtHalf;
                var ramp = levels[0] - LongRamp * Mathf.Max(0, off);

                return Mathf.Max(ramp, naturalHeight);
            }

            return naturalHeight;
        }

        /// <summary>The ground `d` m past the berm: a rock cut (2 up for 1 across) up to the mountain's ground above the level, an earth bank down to it below.</summary>
        static float Bank(float d, float naturalHeight, float level)
        {
            var rise_ = naturalHeight - level;

            return rise_ >= 0 ? level + Mathf.Min(rise_, d * 2) : level + rise_ * Smooth01(0, 10 + 2.2f * Mathf.Abs(rise_), d);
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

        /// <summary>The height one walks at: the ground, but up the tall stairs their treads' (the villagers' ways follow it).</summary>
        public float WalkHeight(Vector2 p)
        {
            if (Glb)
            {
                var local = Local(p);

                foreach (var (along, half, foot, top, low) in flights)
                {
                    if (Mathf.Abs(local.x - along) <= half && local.y >= foot - 1 && local.y <= top)
                    {
                        // Three flights of 21.67 m and 9.02 m each, a landing of 7.65 m between two.
                        var t = Mathf.Max(0, local.y - foot);
                        var k = Mathf.Min(2, Mathf.FloorToInt(t / 29.32f));
                        var r = t - k * 29.32f;

                        return low + Mathf.Min(rise, (k + Mathf.Clamp01(r / 21.67f)) * rise / 3);
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
                    // Outside: the trodden berm and the ramps, then the banks — earth, bare rock where steep.
                    if (outside < Berm + 1.5f || local.y < South - Berm + 1)
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

        /// <summary>Wild trees: not on the town, its earthworks near the wall, the plinth's ramps or the gates' roads.</summary>
        bool WildTiers(Vector2 p)
        {
            var local = Local(p);

            if (-InsideDistance(p) < Berm + 30 || KindAt(p) != KWild || (local.y < South && local.y > South - Berm - RampWidth - 30 && local.x > West - 40 && local.x < East + 40))
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

        // ── The gates and their roads ─────────────────────────────────

        void TierGates()
        {
            void AddGate(string name, float along, float up, Vector2 outward)
            {
                var at = W(along, up);
                var out_ = (Along * outward.x - Down * outward.y).normalized;
                var level = levels[TierAt(up)];
                var road = new GateRoad { At = at, Out = out_, Across = new Vector2(-out_.y, out_.x), Level = level };
                var length = 20f;

                // As long as its rise asks at the steepest allowed; the ground it meets moves as it grows (found again).
                for (var i = 0; i < 24; i++)
                {
                    var end = at + out_ * (Berm + length);
                    road.End = natural(end.x, end.y);
                    length = Mathf.Max(14, Mathf.Abs(road.End - level) / RampGrade);
                }

                var last = at + out_ * (Berm + length);
                road.Length = length;
                road.End = natural(last.x, last.y);
                gateRoads.Add(road);
                Gates.Add(new Gate { At = at, Out = out_, Name = name, Reach = Berm + length + 4 });
                // The guards' place: inside, on the lane round the wall, looking out.
                Places.Add((name, at - out_ * RingInset, YawFacing(out_)));
                MarkRoad(at + out_ * ((Berm + length) / 2), (Berm + length) / 2 + 2, out_);
                BuildLog.Write($"plan of {Place.Id}: {name}: its road {length:0} m to the mountain at {road.End - level:+0.0;-0.0} m");
            }

            // The side gates, one each side on every tier, where that tier meets the mountain's ground (no long ramps).
            AddGate("gate west", West, TierStreet[0], West_);
            AddGate("gate east lower", East, TierStreet[0], East_);
            AddGate("gate west middle", West, TierStreet[1], West_);
            AddGate("gate east", East, TierStreet[1], East_);
            AddGate("gate north", West, TierStreet[2], West_);
            AddGate("gate east upper", East, TierStreet[2], East_);

            // The main gate: out onto its forecourt on the plinth; the ramps down either side along the plinth's face, each
            // as long as it takes to meet the mountain's ground (the trails begin at the east one's end, towards the Xiong).
            var main = W(AxisAlong, South);
            var face = South - Berm;
            var ends = new List<Vector2>();

            foreach (var side in new[] { -1, 1 })
            {
                var off = 0f;

                for (; off < 600; off += 2)
                {
                    var a = AxisAlong + side * (CourtHalf + off);
                    var at = W(a, face - RampWidth / 2);

                    if (levels[0] - LongRamp * off <= natural(at.x, at.y) + 0.3f)
                    {
                        break;
                    }
                }

                var endA = AxisAlong + side * (CourtHalf + off);
                ends.Add(W(endA, face - RampWidth / 2));
                MarkRoad(W((AxisAlong + side * CourtHalf + endA) / 2, face - RampWidth / 2), Mathf.Abs(endA - AxisAlong) / 2, Along);
                BuildLog.Write($"plan of {Place.Id}: the main gate's {(side < 0 ? "west" : "east")} ramp: {off:0} m along the plinth");
            }

            Gates.Add(new Gate { At = main, Out = Down, Name = "gate south", Reach = Berm + CourtDepth, End = ends[1] });
            Places.Add(("gate south", main + Down * -RingInset, YawFacing(Down)));
            MarkRoad(W(AxisAlong, face - CourtDepth / 2), CourtDepth / 2 + Berm, Down);

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

        // ── The retaining walls, the plinth, their parapets ───────────

        /// <summary>A stone wall along a line in the village's frame, its face on the line, its body towards `back` (the higher side); its low and high ends worked out at each point.</summary>
        void FaceWall(Vector2 from, Vector2 to, Vector2 back, float thick, float step, System.Func<Vector2, (float low, float high)> ends)
        {
            var wall = new Wall { Face = true, Back = (Along * back.x - Down * back.y).normalized, Thick = thick };
            var length = Vector2.Distance(from, to);
            var n = Mathf.Max(1, Mathf.CeilToInt(length / step));

            for (var i = 0; i <= n; i++)
            {
                var local = Vector2.Lerp(from, to, (float)i / n);
                var (low, high) = ends(local);
                wall.Points.Add(W(local));
                wall.Low.Add(low);
                wall.High.Add(high);
            }

            Walls.Add(wall);
        }

        /// <summary>A parapet on a wall's top edge, with gaps (along, half width) where stairs and falls come over it.</summary>
        void Parapet(float u, float a0, float a1, float level, Vector2 back, IEnumerable<(float along, float half)> gaps)
        {
            var cuts_ = gaps.OrderBy(g => g.along).ToList();
            var from = a0;

            foreach (var (along, half) in cuts_.Append((a1 + 1000, 0f)))
            {
                var to = Mathf.Min(a1, along - half);

                if (to - from > 1)
                {
                    FaceWall(new Vector2(from, u), new Vector2(to, u), back, 0.55f, 8, _ => (level + 0.45f, level + ParapetHeight - 0.22f));
                }

                from = Mathf.Max(from, along + half);
            }
        }

        void TierWalls()
        {
            float Ground(Vector2 local) => TierHeight(W(local), Natural(local));

            // The retaining walls between the tiers: inside, from the west wall to the east one, a tier high; outside,
            // on into the banks each side as long as the two tiers' banks still differ there.
            for (var k = 0; k < 2; k++)
            {
                var u = TierEdge[k];
                var low = levels[k];
                var high = levels[k + 1];
                FaceWall(new Vector2(West - 1, u), new Vector2(East + 1, u), North_, WallBody, 8, _ => (low, high));
                Inside(W((West + East) / 2, u + WallBody / 2), new Vector2((East - West) / 2, WallBody / 2 + 0.6f), Facing(North_), 0, cell => taken[cell] = true);

                foreach (var side in new[] { -1, 1 })
                {
                    var start = side < 0 ? West - 1 : East + 1;
                    var reach = 0f;

                    for (; reach < 160; reach += 2)
                    {
                        var a = start + side * reach;

                        if (Ground(new Vector2(a, u + WallBody)) - Ground(new Vector2(a, u - 0.3f)) < 0.3f)
                        {
                            break;
                        }
                    }

                    if (reach > 1)
                    {
                        FaceWall(new Vector2(start, u), new Vector2(start + side * reach, u), North_, WallBody, 3, l => (Ground(l + new Vector2(0, -0.3f)), Ground(l + new Vector2(0, WallBody))));
                    }
                }
            }

            // The plinth under the lowest tier's front: its face before the berm, round the main gate's forecourt; the
            // ramps' outer walls.
            var face = South - Berm;
            float Front(Vector2 l) => Ground(l + new Vector2(0, -0.3f));
            FaceWall(new Vector2(West - Berm, face), new Vector2(AxisAlong - CourtHalf, face), North_, WallBody, 4, l => (Front(l), levels[0]));
            FaceWall(new Vector2(AxisAlong + CourtHalf, face), new Vector2(East + Berm, face), North_, WallBody, 4, l => (Front(l), levels[0]));
            FaceWall(new Vector2(AxisAlong - CourtHalf, face - CourtDepth), new Vector2(AxisAlong + CourtHalf, face - CourtDepth), North_, WallBody, 4, l => (Front(l), levels[0]));

            foreach (var side in new[] { -1, 1 })
            {
                var edge = AxisAlong + side * CourtHalf;
                FaceWall(new Vector2(edge, face - CourtDepth), new Vector2(edge, face - RampWidth), new Vector2(-side, 0), WallBody, 4, l => (Ground(l + new Vector2(side * 0.3f, 0)), levels[0]));
                var length = 0f;

                for (; length < 600; length += 2)
                {
                    var a = edge + side * length;

                    if (Ground(new Vector2(a, face - RampWidth + WallTuck + 0.3f)) - Ground(new Vector2(a, face - RampWidth - 0.3f)) < 0.3f)
                    {
                        break;
                    }
                }

                FaceWall(new Vector2(edge, face - RampWidth), new Vector2(edge + side * length, face - RampWidth), North_, WallBody, 3, l => (Ground(l + new Vector2(0, -0.3f)), Ground(l + new Vector2(0, WallBody))));
            }
        }

        /// <summary>The mountain's own ground at a point of the village's frame.</summary>
        float Natural(Vector2 local)
        {
            var p = W(local);

            return natural(p.x, p.y);
        }

        /// <summary>The parapets, once the stairs and the falls are known (their gaps).</summary>
        void TierParapets()
        {
            for (var k = 0; k < 2; k++)
            {
                var gaps = flights.Where(f => Mathf.Abs(f.top - TierEdge[k]) < 1).Select(f => (f.along, f.half + 0.6f))
                    .Concat(Falls.Where(f => Mathf.Abs(Local(f.At).y - TierEdge[k]) < 2).Select(f => (Local(f.At).x, f.Width / 2 + 0.6f)));
                Parapet(TierEdge[k] + 0.05f, West + 1, East - 1, levels[k + 1], North_, gaps);
            }

            // Along the plinth's top, before the berm (the fort's wall stands behind it), and round the forecourt.
            var face = South - Berm + 0.05f;
            Parapet(face, West - Berm, East + Berm, levels[0], North_, new[] { (AxisAlong, CourtHalf - 0.5f), (OutflowWest, 3.5f), (EastWater, 3.5f) });
            Parapet(face - CourtDepth + 0.05f, AxisAlong - CourtHalf, AxisAlong + CourtHalf, levels[0], North_, new (float, float)[0]);
        }

        // ── The water ─────────────────────────────────────────────────

        /// <summary>A channel along a line (the village's frame) on a tier: half its width, how deep its bed lies under the tier (the water a metre under it).</summary>
        void Canal(int tier, float half, float depth, float waterDown, params Vector2[] line)
        {
            var level = levels[tier];
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

        /// <summary>A pond (a box in the village's frame) on a tier, its water a metre under the tier.</summary>
        void Pond(int tier, Vector2 min, Vector2 max, float depth = 2.5f)
        {
            var level = levels[tier];
            pits.Add((min, max, level - depth));
            var centre = W((min + max) / 2);
            var half = (max - min) / 2;
            Pools.Add(new Pool { Centre = centre, Half = half, Yaw = Facing(North_), Water = level - 1, Bed = level - depth, Top = level });
            Inside(centre, half + Vector2.one * 0.8f, Facing(North_), 0, cell =>
            {
                taken[cell] = true;

                if (kind[cell] != KWild)
                {
                    kind[cell] = KYard;
                }
            });
        }

        /// <summary>A round stone basin a fall drops into.</summary>
        void Basin(int tier, Vector2 centre, float radius)
        {
            var level = levels[tier];
            bowls.Add((centre, radius, level - 3.5f));
            Pools.Add(new Pool { Centre = W(centre), Half = new Vector2(radius, radius), Water = level - 1, Bed = level - 3.5f, Top = level, Round = true });
            Around(W(centre), radius + 0.8f, (cell, _) =>
            {
                taken[cell] = true;

                if (kind[cell] != KWild)
                {
                    kind[cell] = KYard;
                }
            });
        }

        void TierWater()
        {
            // The spring under its pavilion on the top tier: down the west channel, over the two retaining walls into
            // basins, west along the lowest tier to the lotus pond, out of the west water gate and down the plinth.
            var spring = Local(QingMao.Spring);
            var lotusMin = new Vector2(-310, -165);
            var lotusMax = new Vector2(-200, -125);
            Canal(2, 2, 2.2f, 1, new Vector2(WestWater, spring.y + 1), new Vector2(WestWater, TierEdge[1] + WallBody + 0.2f));
            FallOver(1, WestWater, 3.4f);
            Canal(1, 2, 2.2f, 1, new Vector2(WestWater, TierEdge[1] - 16), new Vector2(WestWater, TierEdge[0] + WallBody + 0.2f));
            FallOver(0, WestWater, 3.4f);
            Canal(0, 2, 2.2f, 1, new Vector2(WestWater, TierEdge[0] - 16), new Vector2(WestWater, -150), new Vector2(lotusMax.x, -150));
            Pond(0, lotusMin, lotusMax);
            Canal(0, 1.6f, 2.2f, 1, new Vector2(OutflowWest, lotusMin.y), new Vector2(OutflowWest, South - Berm + WallBody));
            OutFall(OutflowWest);

            // The golden channel across the head's forecourt, from the spring's channel to the east one; the upper pond
            // above it; the east channel down the tiers the same way to the east water gate.
            var golden = 168f;
            var upperMin = new Vector2(150, 255);
            var upperMax = new Vector2(215, 292);
            Pond(2, upperMin, upperMax);
            Canal(2, 1.6f, 2.2f, 1, new Vector2(WestWater, golden), new Vector2(EastWater, golden));
            Canal(2, 1.6f, 2.2f, 1, new Vector2(upperMax.x, 262), new Vector2(EastWater, 262), new Vector2(EastWater, TierEdge[1] + WallBody + 0.2f));
            FallOver(1, EastWater, 3);
            Canal(1, 1.6f, 2.2f, 1, new Vector2(EastWater, TierEdge[1] - 16), new Vector2(EastWater, TierEdge[0] + WallBody + 0.2f));
            FallOver(0, EastWater, 3);
            Canal(0, 1.6f, 2.2f, 1, new Vector2(EastWater, TierEdge[0] - 16), new Vector2(EastWater, South - Berm + WallBody));
            OutFall(EastWater);

            // The gutters: at the foot of each retaining wall (from its middle to the channels each side), and at the
            // foot of the rock cut behind the top tier, outside the wall.
            for (var k = 0; k < 2; k++)
            {
                var u = TierEdge[k] - 0.9f;
                Canal(k, 0.55f, 0.6f, 0.4f, new Vector2(West + WallStrip + 1, u), new Vector2(East - WallStrip - 1, u));
            }

            Canal(2, 0.55f, 0.6f, 0.4f, new Vector2(West - Berm + 1, North + Berm - 0.9f), new Vector2(East + Berm - 1, North + Berm - 0.9f));
            // The washers' place: on the channel's bank below the spring's pavilion.
            Places.Add(("spring", W(WestWater + 5, spring.y - 14), Facing(West_)));

            void FallOver(int tier, float along, float width)
            {
                var face = TierEdge[tier];
                Falls.Add(new Fall { At = W(along, face), Out = Down, Top = levels[tier + 1] - 1.2f, Bottom = levels[tier] - 1, Width = width });
                Basin(tier, new Vector2(along, face - 9), 8);
            }

            void OutFall(float along)
            {
                var face = South - Berm;
                var foot = Natural(new Vector2(along, face - 4));
                Falls.Add(new Fall { At = W(along, face), Out = Down, Top = levels[0] - 1.2f, Bottom = foot - 0.5f, Width = 2.6f });
                bowls.Add((new Vector2(along, face - 4), 3.5f, foot - 1.5f));
                Pools.Add(new Pool { Centre = W(along, face - 4), Half = new Vector2(3.5f, 3.5f), Water = foot - 0.5f, Bed = foot - 1.5f, Top = foot, Round = true });
            }
        }

        // ── The stairs ────────────────────────────────────────────────

        /// <summary>A tall stair up a retaining wall's face, its top at the face: where along, its foot's level; and the walkers' way up it.</summary>
        void TallStair(float along, int tier, string name)
        {
            var size = QingMaoBuilder.VillageGlb["prop_stairs_tall"].Size;
            var top = TierEdge[tier];
            var centre = W(along, top - size.y / 2);
            ModelStairs.Add(("prop_stairs_tall", centre, Facing(South_), levels[tier], 1));
            Inside(centre, size / 2 + new Vector2(0.4f, 0.4f), Facing(South_), 0, cell =>
            {
                taken[cell] = true;

                if (kind[cell] != KWild)
                {
                    kind[cell] = KPaved;
                }
            });
            // The stone foot is 1.65 m in from its front (the flags stand before it).
            var foot = top - size.y + 1.65f;
            flights.Add((along, size.x / 2 - 0.6f, foot, top, levels[tier]));
            Streets.Add(new Street { Name = name, Points = Resample(new List<Vector2> { W(along, foot - 4), W(along, top + WallBody + 2) }, 1.4f), Half = 3, Main = false });
        }

        void TierStairs()
        {
            for (var k = 0; k < 2; k++)
            {
                // On the axis two side by side; one in each wing between the quarters.
                var width = QingMaoBuilder.VillageGlb["prop_stairs_tall"].Size.x;
                TallStair(AxisAlong - width / 2, k, "Главная лестница");
                TallStair(AxisAlong + width / 2, k, "Главная лестница");
                TallStair(-300, k, "Западная лестница");
                TallStair(400, k, "Восточная лестница");
            }

            TierParapets();
        }

        // ── The great buildings, the squares, the streets ─────────────

        /// <summary>One of the great buildings, its footprint's middle at (along, up), facing a way; its place before its front.</summary>
        Building Great(string place, string model, float scale, float along, float up, Vector2 facing, BuildingKind kind, string label)
        {
            var size = QingMaoBuilder.VillageGlb[model].Size * scale;
            var centre = W(along, up);
            var yaw = Facing(facing);

            if (!Free(centre, size / 2, yaw, 0.3f))
            {
                BuildLog.Write($"plan of {Place.Id}: {label} overlaps what stands there before it");
            }

            var b = new Building { Kind = kind, Centre = centre, Size = size, Yaw = yaw, Floor = TierLevelAt(centre), Name = label, Lit = true, Model = model, Scale = scale, Pad = true, Seed = random.Next() };
            Buildings.Add(b);
            Take(centre, size / 2, yaw);

            if (place != null)
            {
                Places.Add((place, Offset(centre, yaw, 0, size.y / 2 + 2.5f), yaw + 180));
            }

            return b;
        }

        void TierGreatBuildings()
        {
            // The top tier. The head's court: the great hall on the axis facing down it, the spring's pavilion over the
            // spring, the elders' and the inner affairs' halls at the hall's sides, the ancestors' hall and the hall of
            // punishments behind.
            Great("pavilion", "prop_clan_main_hall", 1, AxisAlong, 228, South_, BuildingKind.Pavilion, "Павильон клана");
            var spring = Local(QingMao.Spring);
            Great(null, "prop_teahouse_octagon", 0.6f, WestWater, spring.y + 10, South_, BuildingKind.Hall, "Павильон источника");
            Great("council", "prop_council_hall", 1, -32, 226, East_, BuildingKind.Hall, "Зал старейшин");
            Great(null, "prop_council_hall", 1, 112, 226, West_, BuildingKind.Hall, "Зал внутренних дел");
            Great("shrine", "prop_ancestral_hall", 1, AxisAlong, 280, South_, BuildingKind.Ancestral, "Зал предков");
            Great(null, "prop_council_hall", 1, 110, 282, South_, BuildingKind.Hall, "Зал наказаний");

            // The Mo family's quarter (west) and the Chi family's (east): each its manor round a courtyard, its own
            // hall, a pavilion by its pond, its Gu nursery and granary.
            foreach (var (family, manor, hall, garden, granary) in new[] { ("Мо", -285f, -355f, -130f, -405f), ("Чи", 415f, 485f, 298f, 535f) })
            {
                Great(null, "prop_inn_courtyard", 1, manor, 250, South_, BuildingKind.Hall, $"Усадьба семьи {family}");
                Great(null, "prop_council_hall", 1, hall, 252, South_, BuildingKind.Hall, $"Родовой зал семьи {family}");
                Great(null, "prop_teahouse_octagon", 0.45f, garden, 280, South_, BuildingKind.Teahouse, $"Беседка семьи {family}");
                Pond(2, new Vector2(garden - 15, 255), new Vector2(garden + 15, 270));
                Great(null, "prop_gu_nursery", 0.9f, garden, 225, South_, BuildingKind.GuFarm, $"Питомник Гу семьи {family}");
                Great(null, "prop_granary", 0.9f, granary, 282, South_, BuildingKind.Granary, $"Амбар семьи {family}");
            }

            // The middle tier. The academy west of the square: its great hall above its yard, the hostel and the
            // refectory at the yard's sides, the classrooms and its Gu nursery below the street.
            Great("academy", "prop_academy_great", 0.6f, -95, 115, South_, BuildingKind.Academy, "Академия");
            Great("hostel", "prop_dorm_great", 0.55f, -158, 112, East_, BuildingKind.Hostel, "Общежитие академии");
            Great("refectory", "prop_refectory", 0.55f, -32, 118, West_, BuildingKind.Hall, "Трапезная академии");
            Great(null, "prop_academy", 0.8f, -140, 17, South_, BuildingKind.Academy, "Класс академии");
            Great(null, "prop_academy", 0.8f, -90, 17, South_, BuildingKind.Academy, "Класс академии");
            Great(null, "prop_gu_nursery", 0.85f, -32, 17, South_, BuildingKind.GuFarm, "Питомник Гу академии");
            // The healers east of the square: their hall, two herbalists, their nursery and store of herbs (their beds: TierGardens).
            Great(null, "prop_academy", 0.9f, 172, 118, South_, BuildingKind.Hall, "Медицинский зал");
            Great(null, "prop_herbalist", 0.9f, 128, 118, South_, BuildingKind.Herbalist, "Лекарня");
            Great(null, "prop_herbalist", 0.9f, 216, 118, South_, BuildingKind.Herbalist, "Травяная лавка лекарей");
            Great(null, "prop_gu_nursery", 0.85f, 130, 17, South_, BuildingKind.GuFarm, "Питомник лечебных Гу");
            Great(null, "prop_granary", 0.85f, 200, 17, South_, BuildingKind.Granary, "Склад трав");
            Great("teahouse", "prop_teahouse_octagon", 0.75f, 95, 22, West_, BuildingKind.Teahouse, "Чайный дом «Восемь ветров»");

            // The lowest tier. By the gate square: the guards' barracks and the outer affairs' hall; up the axis, the
            // great tavern and the inn either side of the stairs' foot; the market rows along the street.
            Great(null, "prop_dorm_great", 0.55f, -45, -152, East_, BuildingKind.Hostel, "Казарма стражи");
            Great(null, "prop_council_hall", 1, 120, -160, West_, BuildingKind.Hall, "Зал внешних дел");
            Great("tavern great", "prop_tavern_great", 0.5f, -12, -96, South_, BuildingKind.Tavern, "Таверна у площади");
            Great(null, "prop_inn_courtyard", 0.8f, 95, -96, South_, BuildingKind.Inn, "Постоялый двор");
            Great("market", "prop_market_row", 0.7f, -86, -133, North_, BuildingKind.Shop, "Торговый ряд");
            Great(null, "prop_market_row", 0.7f, 175, -133, North_, BuildingKind.Shop, "Торговый ряд");
            Great(null, "prop_gambling_tent", 0.8f, 300, -95, South_, BuildingKind.Gambling, "Игорный дом");
        }

        void TierPads()
        {
            var down = Facing(South_);
            // The middle square at the main stairs' head, the gate square inside the main gate, the forecourt of the
            // great hall, the academy's yard (its training ground), the caravans' field.
            AddPad("square", W(AxisAlong, 15), new Vector2(35, 23), down, true);
            Square = Pads[Pads.Count - 1];
            Places.Add(("square", W(AxisAlong, 15), down));
            AddPad("gate square", W(AxisAlong, -158), new Vector2(38, 20), down, true);
            AddPad("forecourt", W(AxisAlong, 178), new Vector2(60, 25), down, true);
            AddPad("academy yard", W(-95, 74), new Vector2(45, 20), down, false);
            Inside(W(-95, 74), new Vector2(45, 20), down, 0, cell =>
            {
                if (!taken[cell] && kind[cell] != KWild)
                {
                    kind[cell] = KYard;
                }
            });
            Places.Add(("academy yard", W(-95, 74), down));
            AddPad("caravans", W(-150, -152), new Vector2(45, 22), down, false);
            Inside(W(-150, -152), new Vector2(45, 22), down, 0, cell =>
            {
                if (!taken[cell] && kind[cell] != KWild)
                {
                    kind[cell] = KYard;
                }
            });
        }

        /// <summary>A straight street (in the village's frame), laid in the ground (paved or trodden); `order`: when houses are set along it.</summary>
        void Way(string name, float a0, float u0, float a1, float u1, float half, bool paved, bool main, int order)
        {
            var from = W(a0, u0);
            var to = W(a1, u1);
            Streets.Add(new Street { Name = name, Points = Resample(new List<Vector2> { from, to }, 1.4f), Half = half, Main = main });
            Inside((from + to) / 2, new Vector2(half, Vector2.Distance(from, to) / 2), YawFacing(to - from), 0, cell =>
            {
                if (kind[cell] != KWild && !taken[cell])
                {
                    kind[cell] = paved ? KPaved : KLane;
                }
            });

            if (order >= 0)
            {
                lotStreets.Add((new Vector2(a0, u0), new Vector2(a1, u1), half, order));
            }
        }

        /// <summary>Each tier's lanes: (its lowest lane, its street, a lane between, its highest lane) uphill.</summary>
        static readonly float[][] TierLanes =
        {
            new[] { South + RingInset, TierStreet[0], -73f, TierEdge[0] - 6 },
            new[] { TierEdge[0] + 8, TierStreet[1], 92f, TierEdge[1] - 6 },
            new[] { TierEdge[1] + 8, TierStreet[2], 245f, North - RingInset },
        };

        static readonly string[][] TierLaneNames =
        {
            new[] { "Улица у южной стены", "Нижняя улица", "Рыночный переулок", "Улица под стеной" },
            new[] { "Средняя набережная", "Средняя улица", "Улица мастеров", "Улица под верхней стеной" },
            new[] { "Верхняя набережная", "Верхняя улица", "Улица усадеб", "Улица у северной стены" },
        };

        /// <summary>The lanes across each tier, from its lowest lane to its highest, along the slope (not by the axis or a channel).</summary>
        static readonly float[] CrossLanes = { -440, -370, -230, -160, -100, 120, 190, 320, 470, 525 };

        void TierStreets()
        {
            var west = West + RingInset;
            var east = East - RingInset;

            // The main ways first (their lots go first): each tier's street from gate to gate, the axis.
            for (var k = 0; k < 3; k++)
            {
                Way(TierLaneNames[k][1], West, TierStreet[k], East, TierStreet[k], k == 2 ? 5 : 6, true, true, 0);
            }

            Way("Главная дорога", AxisAlong, South, AxisAlong, TierEdge[0] - 82, 7, true, true, 0);
            Way("Дорога через площадь", AxisAlong, TierEdge[0] + WallBody, AxisAlong, TierEdge[1] - 82, 6, true, true, -1);
            Way("Дорога к павильону", AxisAlong, TierEdge[1] + WallBody, AxisAlong, 207, 6, true, true, -1);

            for (var k = 0; k < 3; k++)
            {
                var lanes = TierLanes[k];
                var low = lanes[0];
                var high = lanes[3];
                // Along: the lowest lane (the promenade over the wall below, or the lane by the south wall), the one between
                // (not through the academy's and the healers' grounds, nor the head's court), the highest.
                Lane(TierLaneNames[k][0], west - RingHalf, east + RingHalf, low, k == 0 ? RingHalf : 3.5f, k > 0, 1);
                Lane(TierLaneNames[k][2], west, east, lanes[2], 3.5f, false, 1, k == 1 ? (-185f, 260f) : k == 2 ? (WestWater, EastWater) : (0f, 0f));
                Lane(TierLaneNames[k][3], west - RingHalf, east + RingHalf, high, RingHalf, false, 2);
                // Up the tier: the lanes by the side walls, the cross lanes.
                Way("Улица у стены", west, low, west, high, RingHalf, false, false, 3);
                Way("Улица у стены", east, low, east, high, RingHalf, false, false, 3);

                foreach (var a in CrossLanes)
                {
                    // Not through the head's court, nor the academy's or the healers' grounds.
                    if ((k == 2 && a > WestWater && a < EastWater) || (k == 1 && a > -185 && a < 260))
                    {
                        continue;
                    }

                    Way("Переулок", a, low, a, high, 3, false, false, 2);
                }
            }

            // Lanes by the channels (their banks walked), and round the head's court.
            for (var k = 0; k < 3; k++)
            {
                var low = TierLanes[k][0];
                var high = TierLanes[k][3];
                Way("Улица у канала", WestWater + 5, low, WestWater + 5, high, 2.5f, false, false, 3);
                Way("Улица у канала", EastWater - 5, low, EastWater - 5, high, 2.5f, false, false, 3);
            }
        }

        /// <summary>A lane along the slope at `u`, in pieces round what it must not run through: the tall stairs, and the stretches given (along, from, to).</summary>
        void Lane(string name, float a0, float a1, float u, float half, bool main, int order, params (float from, float to)[] blocked)
        {
            var cuts_ = blocked.Where(b => b.to > b.from).ToList();

            foreach (var (along, halfWidth, foot, top, _) in flights)
            {
                if (u + half > foot - 4 && u - half < top)
                {
                    cuts_.Add((along - halfWidth - 1.6f, along + halfWidth + 1.6f));
                }
            }

            var from = a0;

            foreach (var (b0, b1) in cuts_.OrderBy(c => c.from).Append((a1 + 1000, a1 + 1000)))
            {
                var to = Mathf.Min(a1, b0);

                if (to - from > 6)
                {
                    Way(name, from, u, to, u, half, false, main, order);
                }

                from = Mathf.Max(from, b1);
            }
        }

        /// <summary>Where a street crosses a channel, a stone bridge (the street's width, the channel's span and more).</summary>
        void TierBridges()
        {
            foreach (var street in Streets)
            {
                var a = Local(street.Points[0]);
                var b = Local(street.Points[street.Points.Count - 1]);

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

                        if (Bridges.Any(x => Vector2.Distance(x.Centre, world) < 3))
                        {
                            continue;
                        }

                        Bridges.Add(new Bridge { Centre = world, Half = new Vector2(street.Half + 0.4f, channel.Half + 1.4f), Yaw = Facing(way), Top = channel.Top + 0.25f });
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

            // The middle square: three stalls (the noodles, the rice, Baoshan's Gu) facing the way across it, their goods beside them.
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
            var tree = W(s.x - 15, s.y + 11);
            Thing("old tree", tree, R() * 360, 3.4f, true);

            for (var i = 0; i < 4; i++)
            {
                var seat = tree + (Along * Mathf.Cos(i * Mathf.PI / 2) - Down * Mathf.Sin(i * Mathf.PI / 2)) * 3.2f;
                Places.Add(($"bench {i}", seat, YawFacing(tree - seat) + 180));
            }

            Decor("prop_bench_stone", W(s.x + 11, s.y + 16), south, 1, true, true);
            Decor("prop_bench_stone", W(s.x + 23, s.y + 16), south, 1, true, true);
            Thing("notice board", W(s.x - 10, s.y - 18), south, 0.8f, true);
            Thing("drum", W(s.x + 10, s.y - 18), south, 0.9f, true);

            foreach (var (a, u) in new[] { (-30f, -20f), (30f, -20f), (-30f, 20f), (30f, 20f) })
            {
                Thing("stone lantern", W(s.x + a, s.y + u), south, 0.6f, true);
            }

            // The gate square: goods, stalls, carts by the barracks; a stele of the clan facing the gate.
            var g = Local(Pads.First(p => p.Name == "gate square").Centre);
            Decor("prop_clan_stele", W(g.x, g.y + 12), south, 1, true, true);
            Places.Add(("market", W(g.x + 20, g.y), west));

            for (var i = 0; i < 6; i++)
            {
                Decor("prop_market_goods", W(g.x - 30 + i * 12, g.y - 8), north, 0.8f, true, true);
            }

            // The forecourt: bronze dings and steles either side of the way up, lotus vats and pines before the hall.
            var f = Local(Pads.First(p => p.Name == "forecourt").Centre);

            foreach (var side in new[] { -1, 1 })
            {
                Decor("prop_bronze_ding", W(AxisAlong + side * 16, f.y - 2), south, 1, true, true);
                Decor("prop_clan_stele", W(AxisAlong + side * 16, f.y + 14), south, 1, true, true);
                Decor("prop_lotus_vat", W(AxisAlong + side * 11, f.y + 20), south, 1, true, true);
                Decor("prop_bonsai_pine", W(AxisAlong + side * 14, f.y + 20), south, 1, false, true);
                Decor("prop_lotus_vat", W(AxisAlong + side * 40, f.y + 20), south, 1, true, true);
                Thing("banner", W(AxisAlong + side * 30, f.y + 21), south, 0.5f, true);
                Thing("banner", W(AxisAlong + side * 10, TierEdge[1] + 4), south, 0.5f, true);
                Thing("banner", W(AxisAlong + side * 10, TierEdge[0] + 4), south, 0.5f, true);
            }

            // The academy's yard: weapon racks along its lower side, go tables, straw dummies' room in the middle.
            foreach (var a in new[] { -130f, -120f, -70f, -60f })
            {
                Decor("prop_weapon_rack", W(a, 56), north, 1, true, true);
            }

            Decor("prop_go_table", W(-130, 90), south, 1, true, true);
            Decor("prop_go_table", W(-60, 90), south, 1, true, true);

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

        /// <summary>The families' quarters behind bamboo fences: between them and the head's court, along the channels' outer banks (gaps where the lanes go through, a moon gate on each lane).</summary>
        void TierFences()
        {
            foreach (var along in new[] { WestWater - 8, EastWater + 8 })
            {
                var length = QingMaoBuilder.VillageGlb["prop_bamboo_fence"].Size.x;
                var yaw = Facing(East_);

                for (var u = TierEdge[1] + 3 + length / 2; u < North - WallStrip - length / 2; u += length)
                {
                    var at = W(along, u);
                    var k = KindAt(at);

                    if (k == KLane || k == KPaved)
                    {
                        // A lane through the fence: a moon gate over it, or just the gap on the street itself.
                        if (Mathf.Abs(u - TierStreet[2]) > 8)
                        {
                            Decor("prop_moon_gate", at, yaw, 0.9f, true, true);
                        }

                        continue;
                    }

                    Decor("prop_bamboo_fence", at, yaw, 1, true);
                }
            }
        }

        /// <summary>Lanterns (the artist's posts) along the main ways and each tier's street, both sides, not at a crossing; round the forecourt, at the stairs' feet and heads.</summary>
        void TierLanterns()
        {
            bool Street(float a, float u)
            {
                var k = KindAt(W(a, u));

                return k == KLane || k == KPaved;
            }

            void Post(float a, float u, Vector2 facing) => Decor("prop_moon_lantern_post", W(a, u), Facing(facing), 1, true, true);

            for (var u = South + 22; u < TierEdge[0] - 84; u += 14)
            {
                foreach (var side in new[] { -1, 1 })
                {
                    var edge = AxisAlong + side * 10;

                    if (!Street(edge, u) && !Street(edge, u - 5) && !Street(edge, u + 5))
                    {
                        Post(AxisAlong + side * 6.2f, u, side > 0 ? West_ : East_);
                    }
                }
            }

            for (var k = 0; k < 3; k++)
            {
                var half = k == 2 ? 5 : 6;

                for (var a = West + 24; a < East - 20; a += 24)
                {
                    foreach (var side in new[] { -1, 1 })
                    {
                        var edge = TierStreet[k] + side * (half + 3);

                        if (!Street(a, edge) && !Street(a - 5, edge) && !Street(a + 5, edge))
                        {
                            Post(a, TierStreet[k] + side * (half - 0.8f), side > 0 ? South_ : North_);
                        }
                    }
                }

                // Either side of each stair's foot and head.
                if (k < 2)
                {
                    foreach (var a in new[] { AxisAlong - 12, AxisAlong + 12, -310f, -290f, 390f, 410f })
                    {
                        Post(a, TierEdge[k] - 86, North_);
                        Post(a, TierEdge[k] + 4, South_);
                    }
                }
            }
        }

        // ── Houses ────────────────────────────────────────────────────

        void TierWishes()
        {
            void Want(BuildingKind kind, string name, string model, float scale, float along, float up, float radius = 40) =>
                wishes.Add(new Wish { Kind = kind, Name = name, Model = model, Scale = scale, Target = W(along, up), Radius = radius });

            // Round the middle square and the gate square: the trades people walk to.
            Want(BuildingKind.Tavern, "Харчевня", "prop_eatery", 0.85f, -20, 52);
            Want(BuildingKind.Teahouse, "Чайная", "prop_teahouse", 0.85f, 100, 52);
            Want(BuildingKind.Shop, "Лавка", "prop_shop", 0.9f, 0, -112);
            Want(BuildingKind.Shop, "Лавка", "prop_shop", 0.9f, 80, -112);
            Want(BuildingKind.Shop, "Лавка", "prop_shop", 0.9f, 140, 38);
            Want(BuildingKind.Herbalist, "Лавка травника", "prop_herbalist", 0.9f, -330, -112);
            // Three taverns, as the clan has (the bamboo wine is the inn's alone): one among the masters, one by the crafts.
            Want(BuildingKind.Tavern, "Таверна «Рисовое вино»", "prop_tavern", 0.85f, 350, 52, 50);
            Want(BuildingKind.Tavern, "Таверна «Мутное вино»", "prop_tavern", 0.85f, 380, -112, 50);
            // The crafts: smithies, the clan's granaries along the east wall; the poor's eatery.
            Want(BuildingKind.Smithy, "Кузница", "prop_smithy", 0.9f, 330, -60, 45);
            Want(BuildingKind.Smithy, "Кузница", "prop_smithy", 0.9f, 450, -60, 45);
            Want(BuildingKind.Smithy, "Оружейная кузница", "prop_smithy", 0.9f, 520, -112, 45);

            foreach (var u in new[] { -160f, -112f, -60f })
            {
                Want(BuildingKind.Granary, "Амбар клана", "prop_granary", 0.9f, 540, u, 45);
            }

            Want(BuildingKind.Tavern, "Харчевня для бедных", "prop_eatery", 0.8f, -420, -112, 50);
            Want(BuildingKind.Shop, "Лавка", "prop_shop", 0.9f, -300, 52, 50);
            Want(BuildingKind.Shop, "Лавка", "prop_shop", 0.9f, 330, 52, 50);
            Want(BuildingKind.Teahouse, "Чайная", "prop_teahouse", 0.85f, -420, 52, 50);
        }

        /// <summary>The quarter a spot (the village's frame) is in.</summary>
        enum Quarter { Head, Family, Academy, Healers, Masters, Market, Mortals, Crafts }

        static Quarter QuarterAt(Vector2 local)
        {
            var a = local.x;

            switch (TierAt(local.y))
            {
                case 2:
                    return a > WestWater && a < EastWater ? Quarter.Head : Quarter.Family;
                case 1:
                    return a > -185 && a < 5 ? Quarter.Academy : a > 75 && a < 260 ? Quarter.Healers : Quarter.Masters;
                default:
                    return a < -150 ? Quarter.Mortals : a > 230 ? Quarter.Crafts : Quarter.Market;
            }
        }

        /// <summary>
        /// What may stand on a lot here, the first that fits: a wanted trade near its spot; else by the quarter — the
        /// families' and the head's people in big bamboo houses, the masters in bamboo houses with a shop now and then,
        /// the market's shops, the mortals' poor houses, the crafts' poor houses and sheds — the poor in the rows by
        /// the wall everywhere but the top; else a smaller one.
        /// </summary>
        IEnumerable<(string model, float scale, BuildingKind kind, Wish wish)> LotChoices(Vector2 local, bool main)
        {
            var world = W(local);
            var wish = wishes.Where(w => !w.Done && Vector2.Distance(w.Target, world) < w.Radius).OrderBy(w => Vector2.Distance(w.Target, world)).FirstOrDefault();

            if (wish != null)
            {
                yield return (wish.Model, wish.Scale, wish.Kind, wish);
            }

            var small = (SmallShops[random.Next(SmallShops.Length)], 1f, BuildingKind.Shop, (Wish)null);
            var quarter = QuarterAt(local);

            if (InsideDistance(world) < 30 && TierAt(local.y) < 2)
            {
                yield return ("prop_poor_house", 0.85f + R() * 0.1f, BuildingKind.Poor, null);
                yield return ("prop_poor_house", 0.75f, BuildingKind.Poor, null);
                yield break;
            }

            switch (quarter)
            {
                case Quarter.Head:
                case Quarter.Family:
                    yield return ("guyue_bamboo_house", 0.88f + R() * 0.06f, BuildingKind.Family, null);
                    yield return ("guyue_bamboo_house", 0.78f, BuildingKind.Family, null);
                    break;
                case Quarter.Market:
                    var pick = R();
                    yield return main ? (pick < 0.4f ? small : pick < 0.75f ? ("prop_shop", 0.9f, BuildingKind.Shop, null) : ("guyue_bamboo_house", 0.75f, BuildingKind.Family, null))
                        : (pick < 0.2f ? small : ("guyue_bamboo_house", 0.75f, BuildingKind.Family, null));
                    yield return small;
                    break;
                case Quarter.Mortals:
                    yield return R() < 0.08f ? small : ("prop_poor_house", 0.85f + R() * 0.12f, BuildingKind.Poor, null);
                    yield return ("prop_poor_house", 0.75f, BuildingKind.Poor, null);
                    break;
                case Quarter.Crafts:
                    var c = R();
                    yield return c < 0.12f ? small : c < 0.3f ? ("prop_poor_house", 0.75f, BuildingKind.Shed, null) : ("prop_poor_house", 0.9f, BuildingKind.Poor, null);
                    yield return ("prop_poor_house", 0.75f, BuildingKind.Poor, null);
                    break;
                default:
                    yield return (main && R() < 0.12f) || R() < 0.04f ? small : ("guyue_bamboo_house", 0.8f, BuildingKind.Family, null);
                    yield return ("guyue_bamboo_house", 0.7f, BuildingKind.Family, null);
                    yield return ("prop_poor_house", 0.9f, BuildingKind.Poor, null);
                    break;
            }
        }

        void TierHouses()
        {
            TierWishes();

            foreach (var (from, to, half, order) in lotStreets.OrderBy(s => s.order))
            {
                Lots(from, to, half, 1, order == 0);
                Lots(from, to, half, -1, order == 0);
            }

            AssignTrades();
        }

        /// <summary>
        /// Houses along one side of a straight street (`side`: left or right of its way): one after another, each
        /// square to it, its front `Setback` from its edge, a gap between, an alley now and then; where one does not fit
        /// (a crossing, what stands there) a smaller one is tried, else the next metre.
        /// </summary>
        void Lots(Vector2 from, Vector2 to, float half, int side, bool main)
        {
            var dir = (to - from).normalized;
            var normal = new Vector2(-dir.y, dir.x) * side;
            var length = Vector2.Distance(from, to);
            var yaw = Facing(-normal);
            var s = 0f;
            var since = 0;

            while (s < length - 4)
            {
                var placed = 0f;

                foreach (var (model, scale, kind_, wish) in LotChoices(from + dir * s + normal * (half + 6), main))
                {
                    var size = QingMaoBuilder.VillageGlb[model].Size * scale;

                    if (s + size.x > length)
                    {
                        continue;
                    }

                    var local = from + dir * (s + size.x / 2) + normal * (half + Setback + size.y / 2);
                    var centre = W(local);

                    // Whole on one tier, clear of the retaining walls' feet and tops.
                    if (TierAt(local.y - size.magnitude / 2) != TierAt(local.y + size.magnitude / 2) || !Free(centre, size / 2, yaw, 0.7f))
                    {
                        continue;
                    }

                    AddHouse(centre, size, yaw, kind_, model, scale, wish);
                    placed = size.x;
                    break;
                }

                if (placed <= 0)
                {
                    s += 1;
                    continue;
                }

                since++;
                var gap = 1.6f + R() * 0.8f;

                if (since >= 3 + random.Next(3))
                {
                    gap = 4.5f;
                    since = 0;
                }

                s += placed + gap;
            }
        }

        void AddHouse(Vector2 centre, Vector2 size, float yaw, BuildingKind kind_, string model, float scale, Wish wish)
        {
            var b = new Building { Kind = kind_, Centre = centre, Size = size, Yaw = yaw, Floor = TierLevelAt(centre), Seed = random.Next(), Lit = wish != null || R() < 0.55f, Model = model, Scale = scale, Pad = true };

            if (wish != null)
            {
                wish.Done = true;
                b.Name = wish.Name;
            }

            Buildings.Add(b);
            Take(centre, size / 2, yaw);

            if (kind_ == BuildingKind.Family || kind_ == BuildingKind.Poor)
            {
                Doorsteps.Add((Offset(centre, yaw, 0, size.y / 2 + 1.1f), yaw));
            }
        }

        // ── What lies about ───────────────────────────────────────────

        /// <summary>One of the artist's props off the streets, where nothing stands within `margin` of it.</summary>
        bool Model(string model, Vector2 at, float yaw, float scale = 1, bool solid = true, float margin = 0.6f)
        {
            var half = QingMaoBuilder.VillageGlb[model].Size * scale / 2;

            if (!Free(at, half, yaw, margin))
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
            // By each building's front and sides, what its trade or its household keeps there (never on the street).
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

                        if (b.Kind != BuildingKind.Family && R() < 0.6f)
                        {
                            var thing = new[] { "prop_firewood_stack", "prop_drying_rack", "prop_barrels" }[random.Next(3)];
                            var scale = thing == "prop_barrels" ? 0.55f : 1;
                            var size = QingMaoBuilder.VillageGlb[thing].Size * scale;
                            Model(thing, At(aside * (hx + size.y / 2 + 0.6f), 0), b.Yaw + 90 * aside, scale, true, 0.4f);
                        }
                        else if (R() < 0.3f)
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

                        if (Free(at, half, b.Yaw, 0.6f))
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

            // Trees along the promenades over the walls (a view down over the town), plums in the head's court.
            for (var k = 1; k < 3; k++)
            {
                for (var a = West + 30; a < East - 30; a += 18)
                {
                    Thing(k == 2 && a > WestWater && a < EastWater ? "tree" : R() < 0.4f ? "pine tree" : "tree", W(a + (R() - 0.5f) * 4, TierEdge[k - 1] + 3.3f), R() * 360, 1.0f, false, 0.8f + R() * 0.3f);
                }
            }

            // Behind the great hall, a row of bamboo.
            foreach (var a in new[] { 0f, 14f, 66f, 80f })
            {
                Model("prop_bamboo_grove", W(a, 258), Facing(South_), 0.7f, false, 0.3f);
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

        /// <summary>Vegetable gardens in the room the rows leave, fenced low; the healers' herb beds in rows; the mortals' plots by the lotus pond.</summary>
        void TierGardens()
        {
            // The healers' beds between their hall and the street below.
            for (var a = 100f; a < 245; a += 12)
            {
                for (var u = 62f; u < 100; u += 7)
                {
                    var at = W(a, u);
                    var half = new Vector2(4.6f, 2.2f);

                    if (Free(at, half, Facing(North_), 0.6f))
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
                var yaw = Facing(R() < 0.5f ? South_ : East_);

                if (KindAt(p) != KGround || !Free(p, half, yaw, 0.8f))
                {
                    continue;
                }

                Gardens.Add((p, half, yaw));
                Take(p, half, yaw, KGarden);
            }
        }
    }
}
