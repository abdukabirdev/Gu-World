using System;
using System.Collections.Generic;
using System.Globalization;
using GuWorld.Core.Gen.Buildings;

namespace GuWorld.Core.Gen
{
    /// <summary>
    /// A clan's town in rings by how protected they are (Docs/V2/PLAN-TOWNS.md): the heart round the
    /// valley's square with the clan's pavilion over its square and the halls, the academy, the
    /// storehouse; the factions' districts, each its own bearing of the ring with its manor, kitchen,
    /// storehouse, training ground and fields in its colours; the Gu Masters' houses; the commoners
    /// with the taverns and the inn; the poor at the edge with the lodging let by the day. Where the
    /// ground breaks off under a cliff between a place and the heart (the riverside below the town),
    /// the place counts as farther out.
    /// </summary>
    public static partial class CityPlanner
    {
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        /// <summary>A clan town's rings and its factions' bearings, and what they say of a point.</summary>
        internal sealed class Tiers
        {
            public readonly ClanTown Town;
            public readonly Vec2 Centre;
            public readonly float Radius, Core;
            /// <summary>The outer edge of each tier, metres from the middle.</summary>
            public readonly float[] Edges;
            /// <summary>The faction holding each 2° of bearing in the factions' ring (-1: none).</summary>
            public readonly int[] Sector = new int[180];
            public float PavilionBearing;
            /// <summary>A village round its plaza (not levelled: everything stands down to the slope), not a town round a hand-made middle.</summary>
            public bool Village;
            readonly IGround ground;
            readonly Dictionary<(int, int), bool> beyond = new Dictionary<(int, int), bool>();

            public Tiers(ClanTown town, SettlementDef def, IGround ground, float core = -1)
            {
                Town = town;
                Centre = def.At;
                Radius = def.Radius;
                Core = core >= 0 ? core : def.Core;
                this.ground = ground;
                Edges = new float[town.Tiers.Length];

                // The rings hold shares of the land that can be built on (not of the radius): where the valley's
                // places, the water or a cliff take a bearing, the ring reaches farther out on the others.
                var reach = new List<float>();

                for (var x = -Radius; x <= Radius; x += 12)
                {
                    for (var z = -Radius; z <= Radius; z += 12)
                    {
                        var p = Centre + new Vec2(x, z);
                        var d = Vec2.Distance(p, Centre);

                        if (d > Core + 4 && d < Radius - 4 && !ground.Blocked(p, 3))
                        {
                            reach.Add(d + (Beyond(p) ? Radius * 0.3f : 0));
                        }
                    }
                }

                reach.Sort();

                for (var i = 0; i < Edges.Length; i++)
                {
                    var share = Math.Min(1, town.Tiers[i].UpTo);
                    Edges[i] = reach.Count == 0 ? share * Radius : i == Edges.Length - 1 ? float.MaxValue : reach[Math.Min(reach.Count - 1, (int)(share * reach.Count))];
                }

                for (var i = 0; i < Sector.Length; i++)
                {
                    Sector[i] = -1;
                }
            }

            /// <summary>Whether the point lies under a cliff on its way to the middle: the ground climbing more than 20 m in 18 towards the middle (the town's cliff over the riverside).</summary>
            public bool Beyond(Vec2 p)
            {
                var key = ((int)Math.Floor(p.X / 8), (int)Math.Floor(p.Z / 8));

                if (beyond.TryGetValue(key, out var known))
                {
                    return known;
                }

                var from = new Vec2(key.Item1 * 8 + 4, key.Item2 * 8 + 4);
                var d = Vec2.Distance(from, Centre);
                var steps = (int)(d / 6);
                var heights = new List<float> { ground.Height(from.X, from.Z) };
                var result = false;

                for (var i = 1; i <= steps && !result; i++)
                {
                    var q = from + (Centre - from) * (i / (float)Math.Max(1, steps));

                    if (Vec2.Distance(q, Centre) < Core)
                    {
                        break;
                    }

                    heights.Add(ground.Height(q.X, q.Z));
                    var n = heights.Count;
                    result = n > 3 && heights[n - 1] - heights[n - 4] > 20;
                }

                beyond[key] = result;

                return result;
            }

            public int TierOf(Vec2 p)
            {
                var d = Vec2.Distance(p, Centre) + (Beyond(p) ? Radius * 0.3f : 0);

                for (var i = 0; i < Edges.Length; i++)
                {
                    if (d <= Edges[i])
                    {
                        return i;
                    }
                }

                return Edges.Length - 1;
            }

            public static int Bin(float bearing) => ((int)Math.Floor(Mod(bearing, 360) / 2)) % 180;

            public int FactionOf(Vec2 p) => Sector[Bin((p - Centre).Yaw)];

            public LotPlace At(Vec2 p, bool main)
            {
                var tier = TierOf(p);
                var t = Town.Tiers[tier];
                var tag = Town.Clan;

                if (tier == 1 && FactionOf(p) is var f && f >= 0)
                {
                    tag = Town.Factions[f].Tag;
                }

                return new LotPlace { Lots = main && t.Main != null ? t.Main : t.Lots, Tag = tag, Tier = tier };
            }

            /// <summary>
            /// Shares the factions' ring out by bearing: only where it can be built on (not a place of the
            /// valley, not under the cliff), by the factions' weights, the clan head's own centred on the pavilion.
            /// </summary>
            public void Divide(float pavilionBearing)
            {
                PavilionBearing = pavilionBearing;
                var open = new List<int>();
                var start = Bin(pavilionBearing);

                for (var k = 0; k < 180; k++)
                {
                    var b = (start + k) % 180;
                    var bearing = b * 2 + 1;
                    int good = 0, all = 0;

                    for (var r = Edges[0] + 10; r < Edges[1]; r += 14)
                    {
                        all++;
                        var p = Centre + Vec2.FromYaw(bearing) * r;
                        var ok = r < Radius - 5 && !ground.Blocked(p, 4) && !Beyond(p);

                        // (A village's factions keep to ground a yard can stand on: not where it falls half its length.)
                        if (ok && Village)
                        {
                            var across = Vec2.FromYaw(bearing + 90) * 10;
                            var outward = Vec2.FromYaw(bearing) * 10;
                            ok = Math.Abs(ground.Height((p + across).X, (p + across).Z) - ground.Height((p - across).X, (p - across).Z)) < 10 &&
                                 Math.Abs(ground.Height((p + outward).X, (p + outward).Z) - ground.Height((p - outward).X, (p - outward).Z)) < 10;
                        }

                        good += ok ? 1 : 0;
                    }

                    if (good * 2 >= all)
                    {
                        open.Add(b);
                    }
                }

                if (open.Count == 0)
                {
                    return;
                }

                var total = 0f;

                foreach (var f in Town.Factions)
                {
                    total += f.Weight;
                }

                // Turned so the head's share is centred on the pavilion's bearing.
                var head = (int)Math.Round(Town.Factions[0].Weight / total * open.Count);
                var shift = open.Count - head / 2;
                var at = 0f;

                for (var i = 0; i < Town.Factions.Length; i++)
                {
                    var from = (int)Math.Round(at / total * open.Count);
                    at += Town.Factions[i].Weight;
                    var to = (int)Math.Round(at / total * open.Count);

                    for (var k = from; k < to; k++)
                    {
                        Sector[open[(k + shift) % open.Count]] = i;
                    }
                }
            }

            /// <summary>A faction's bearings as runs (from, to in degrees, clockwise), the longest first.</summary>
            public List<(float from, float to)> Runs(int faction)
            {
                var runs = new List<(float, float)>();
                var first = -1;

                // Start where the faction does not hold, so no run is cut at 0°.
                var origin = Array.FindIndex(Sector, s => s != faction);

                if (origin < 0)
                {
                    runs.Add((0, 360));
                    return runs;
                }

                for (var k = 0; k <= 180; k++)
                {
                    var b = (origin + k) % 180;
                    var holds = k < 180 && Sector[b] == faction;

                    if (holds && first < 0)
                    {
                        first = origin + k;
                    }
                    else if (!holds && first >= 0)
                    {
                        runs.Add((Mod(first * 2, 360), Mod((origin + k) * 2, 360)));
                        first = -1;
                    }
                }

                runs.Sort((a, b) => Span(b.Item1, b.Item2).CompareTo(Span(a.Item1, a.Item2)));

                return runs;
            }
        }

        static float Span(float from, float to) => Mod(to - from, 360);

        /// <summary>
        /// Lanes along the slope at every bench of its height (the contours, so they bend as the mountain
        /// does) and stairs straight down the slope between them every so often, across a disc of `radius`
        /// outside `inner`; cut where `taken`. The slope's way found from the ground over the disc. Returns the lanes made.
        /// </summary>
        internal static int ContourLanes(SettlementPlan plan, float radius, float inner, IGround ground, Func<Vec2, float, bool> taken, float bench, float laneHalf, float stairsFrom, float stairsTo, ref Rng rng)
        {
            var centre = plan.Centre;
            var lowest = float.MaxValue;
            var highest = float.MinValue;
            var gx = 0f;
            var gz = 0f;

            for (var a = 0; a < 36; a++)
            {
                for (var r = Math.Max(inner, radius * 0.1f); r <= radius; r += radius / 12)
                {
                    var p = centre + Vec2.FromYaw(a * 10) * r;
                    var h = ground.Height(p.X, p.Z);
                    lowest = Math.Min(lowest, h);
                    highest = Math.Max(highest, h);
                    gx += (p.X - centre.X) * h;
                    gz += (p.Z - centre.Z) * h;
                }
            }

            // Uphill and along the slope.
            var up = new Vec2(gx, gz).Length > 1e-3f ? new Vec2(gx, gz) * (1 / new Vec2(gx, gz).Length) : new Vec2(0, 1);
            var along = up.Right;
            var lanes = 0;

            for (var level = (float)Math.Ceiling(lowest / bench) * bench + bench * 0.5f; level < highest; level += bench)
            {
                // Trace the contour across, at each step along the slope the point up or down it where the ground is at this level.
                var lane = new List<Vec2>();

                for (var t = -radius; t <= radius; t += 4)
                {
                    var half = (float)Math.Sqrt(Math.Max(0, radius * radius - t * t)) - 8;
                    var found = false;
                    var p = default(Vec2);

                    if (half > 10)
                    {
                        Vec2 At(float s) => centre + along * t + up * s;
                        var lo = -half;
                        var hi = half;
                        var hlo = ground.Height(At(lo).X, At(lo).Z) - level;
                        var hhi = ground.Height(At(hi).X, At(hi).Z) - level;

                        if (hlo * hhi < 0)
                        {
                            for (var k = 0; k < 18; k++)
                            {
                                var mid = (lo + hi) * 0.5f;
                                var hm = ground.Height(At(mid).X, At(mid).Z) - level;

                                if (hm * hlo > 0)
                                {
                                    lo = mid;
                                    hlo = hm;
                                }
                                else
                                {
                                    hi = mid;
                                }
                            }

                            p = At((lo + hi) * 0.5f);
                            found = Vec2.Distance(p, centre) > inner + 6 && !taken(p, 5) && (lane.Count == 0 || Vec2.Distance(p, lane[lane.Count - 1]) < 12);
                        }
                    }

                    if (found)
                    {
                        lane.Add(p);
                    }
                    else
                    {
                        lanes += AddSmoothed(plan, lane, laneHalf);

                        lane = new List<Vec2>();

                        if (half > 10 && Vec2.Distance(p, centre) > inner + 6 && t != 0 && !taken(p, 5))
                        {
                            lane.Add(p);
                        }
                    }
                }

                lanes += AddSmoothed(plan, lane, laneHalf);
            }

            // Stairs down the slope: straight down its fall from bench to bench, every so often across.
            for (var t = -radius + rng.Range(15f, 35f); t < radius - 15; t += rng.Range(stairsFrom, stairsTo))
            {
                var stair = new List<Vec2>();
                var half = (float)Math.Sqrt(Math.Max(0, radius * radius - t * t)) - 8;

                for (var s = -half; s <= half; s += 4)
                {
                    var p = centre + along * (t + 6 * (float)Math.Sin(s * 0.02f + t)) + up * s;

                    if (Vec2.Distance(p, centre) < inner + 6 || taken(p, 4))
                    {
                        AddPiece(plan, stair, 1.5f);
                        stair = new List<Vec2>();
                    }
                    else
                    {
                        stair.Add(p);
                    }
                }

                AddPiece(plan, stair, 1.5f);
            }

            return lanes;
        }

        static string Generated(bool pad, string style, int variant, string tag, int wealth, int wear, float w, float d) =>
            string.Format(Inv, "{0}:{1}:{2}:{3}:{4}:{5}:{6:0.0}:{7:0.0}", pad ? "gen" : "hill", style, variant, tag, Math.Max(0, Math.Min(9, wealth)), Math.Max(0, Math.Min(9, wear)), w, d);

        /// <summary>Puts a thing on the plan as it is, unchecked (things of a square that is itself kept clear).</summary>
        static void Put(SettlementPlan plan, string model, Vec2 centre, float yaw, string role, string district)
        {
            if (!Catalog.TryGet(model, out var footprint))
            {
                return;
            }

            var box = new Box(centre, new Vec2(footprint.Across * 0.5f, footprint.Deep * 0.5f), yaw);
            plan.Buildings.Add(new Placed { Model = model, At = centre, Yaw = yaw, Footprint = box, Role = role, District = district });
        }

        /// <summary>
        /// The heart: the clan's great building with a square before it facing the middle, on the
        /// highest ground of the ring that will take it; the halls about it; the rest of the heart's
        /// buildings anywhere in the ring. Returns the squares (kept clear of streets and houses).
        /// </summary>
        static List<Box> Heart(SettlementPlan plan, Tiers tiers, List<Box> placed, ShapeIndex<Box> index, SegmentIndex avenues, IGround ground, Waters waters, ref Rng rng)
        {
            var town = tiers.Town;
            var def = plan.Def;
            var squares = new List<Box>();
            var heartId = town.Clan + ":tier0";
            var main = town.Main;
            var (squareW, squareD) = ThingGen.Footprint("plaza");
            var candidates = new List<(Vec2 at, float bearing, float score)>();

            var core = tiers.Core;

            for (var bearing = 0f; bearing < 360; bearing += 5)
            {
                // A town's great building stands back from its hand-made middle behind its own square; a village's faces its plaza.
                var nearest = tiers.Village ? core + 3 + main.Depth * 0.5f : core + 6 + squareD + main.Depth * 0.5f;

                for (var r = nearest; r < Math.Max(nearest + (tiers.Village ? 40 : 30), tiers.Village ? 0 : tiers.Edges[0] - main.Depth * 0.5f - 4); r += tiers.Village ? 4 : 7)
                {
                    var p = plan.Centre + Vec2.FromYaw(bearing) * r;

                    if (ground.Blocked(p, 6) || tiers.Beyond(p))
                    {
                        continue;
                    }

                    // High ground, near the middle, the village all round it (not at the edge of a wood): the most protected and the most seen.
                    var round = 0;

                    for (var k = 0; k < 16; k++)
                    {
                        var q = p + Vec2.FromYaw(k * 22.5f) * 70;
                        var dq = Vec2.Distance(q, plan.Centre);
                        round += dq < core || dq < def.Radius - 10 && !ground.Blocked(q, 4) && !tiers.Beyond(q) ? 1 : 0;
                    }

                    candidates.Add((p, bearing, ground.Height(p.X, p.Z) - (r - core) * (tiers.Village ? 0.5f : 0.12f) - (16 - round) * 3));
                }
            }

            candidates.Sort((a, b) => b.score.CompareTo(a.score));
            var found = false;

            foreach (var (p, bearing, _) in candidates)
            {
                var yaw = (plan.Centre - p).Yaw;
                var forward = Vec2.FromYaw(yaw);
                var square = new Box(p + forward * (main.Depth * 0.5f + squareD * 0.5f + 1), new Vec2(squareW * 0.5f, squareD * 0.5f), yaw);
                float low = float.MaxValue, high = float.MinValue;
                var clear = tiers.Village || !Crowded(index, square);

                foreach (var corner in square.Corners())
                {
                    var d = Vec2.Distance(corner, plan.Centre);
                    clear &= tiers.Village || d > core + 1 && d < def.Radius - 4 && !ground.Blocked(corner, 2);
                    var h = ground.Height(corner.X, corner.Z);
                    low = Math.Min(low, h);
                    high = Math.Max(high, h);
                }

                if (!clear || !tiers.Village && high - low > 7)
                {
                    continue;
                }

                var model = Generated(!tiers.Village, main.Style, 1 + rng.Range(0, 24), town.Clan, main.Wealth, main.Wear, main.Width, main.Depth);

                // The clan's terrace may be cut deep into the slope (a village's hall stands down to it on its podium).
                if (!Place(plan, placed, index, avenues, ground, waters, model, p, yaw, main.Role, 1.5f, !tiers.Village, false, tiers.Village ? 10 : 8))
                {
                    continue;
                }

                var pavilion = plan.Buildings[plan.Buildings.Count - 1];
                pavilion.Name = main.Name;
                pavilion.District = heartId;

                if (tiers.Village)
                {
                    Beside(plan, placed, index, avenues, ground, waters, pavilion, main.Things, town.Clan, ref rng);
                    tiers.Divide(bearing);
                    found = true;
                    break;
                }

                // The square: levelled, paved by the ground's own stone layer, kept clear; a censer in its middle, lanterns and banners.
                plan.Pads.Add((square, (low + high) * 0.5f));
                index.Add(square.Centre, square.Radius, square);
                squares.Add(square);
                Put(plan, $"thing:plaza:{def.Seed}:{town.Clan}", square.Centre, yaw, "ground", heartId);
                Put(plan, $"thing:censer:{def.Seed}:{town.Clan}", square.Centre, yaw + 180, "prop", heartId);

                foreach (var side in new[] { -1, 1 })
                {
                    foreach (var along in new[] { -0.75f, 0.3f })
                    {
                        Put(plan, $"thing:lantern_post:{def.Seed + (side > 0 ? 3 : 4)}:{town.Clan}", square.World(new Vec2(side * (squareW * 0.5f - 1.2f), squareD * 0.5f * along)), yaw + 180, "prop", heartId);
                    }

                    Put(plan, $"thing:banner_pole:{def.Seed + 5}:{town.Clan}", square.World(new Vec2(side * (squareW * 0.5f - 1.4f), -squareD * 0.5f + 1.6f)), yaw + 180, "prop", heartId);
                    plan.Spots.Add(new Spot { At = square.World(new Vec2(side * 6, -2)), Facing = yaw + 180, Role = "plaza" });
                }

                plan.Spots.Add(new Spot { At = square.World(new Vec2(0, squareD * 0.5f - 3)), Facing = yaw, Role = "plaza" });
                // Stone lions at the foot of the pavilion's steps, the male on the left, the female on the right; guards of the Dark Hall by them.
                Put(plan, "code:stone_lion:0", square.World(new Vec2(-5, squareD * 0.5f - 2.2f)), yaw, "prop", heartId);
                Put(plan, "code:stone_lion:1", square.World(new Vec2(5, squareD * 0.5f - 2.2f)), yaw, "prop", heartId);
                // The way down to the spring, by the pavilion's steps (the door thing of the regions stands beside it).
                plan.Spots.Add(new Spot { At = square.World(new Vec2(-8, squareD * 0.5f - 1.5f)), Facing = yaw + 180, Role = "depths" });
                plan.Spots.Add(new Spot { At = square.World(new Vec2(-3, squareD * 0.5f - 1.2f)), Facing = yaw + 180, Role = "guard" });
                plan.Spots.Add(new Spot { At = square.World(new Vec2(3, squareD * 0.5f - 1.2f)), Facing = yaw + 180, Role = "guard" });
                tiers.Divide(bearing);
                found = true;
                break;
            }

            if (!found)
            {
                tiers.Divide(0);
            }

            // The rest of the heart, the biggest first: the halls near the pavilion, the others anywhere in the ring, each facing the middle.
            var heart = new List<Landmark>(Array.FindAll(town.Landmarks, l => l.Tier == 0));
            heart.Sort((a, b) => (b.Width * b.Depth).CompareTo(a.Width * a.Depth));

            foreach (var landmark in heart)
            {

                for (var n = 0; n < landmark.Count; n++)
                {
                    var near = landmark.Role == "hall" || landmark.Role == "herbalist";
                    var lo = core + 6 + landmark.Depth * 0.5f;
                    var hi = Math.Max(lo + 25, tiers.Edges[0] - 3 - landmark.Depth * 0.5f);

                    for (var attempt = 0; attempt < 300; attempt++)
                    {
                        // (Where the heart has no room left for it, just outside it.)
                        var bearing = near && found ? tiers.PavilionBearing + rng.Range(-1f, 1f) * (25 + attempt * 0.8f) : rng.Range(0f, 360f);
                        var p = plan.Centre + Vec2.FromYaw(bearing) * rng.Range(lo, hi + Math.Max(0, attempt - 150) * 0.4f);
                        // Facing the middle, or (a long yard that will not take the slope that way) turned along it.
                        var yaw = (float)Math.Round((plan.Centre - p).Yaw / 5) * 5 + (attempt % 3 == 0 ? 0 : attempt % 3 == 1 ? 90 : -90);
                        var tag = landmark.Faction.Length > 0 ? town.Clan + "." + landmark.Faction : town.Clan;
                        var pad = landmark.Pad && !tiers.Village;
                        var model = Generated(pad, landmark.Style, 1 + rng.Range(0, 24), tag, landmark.Wealth, landmark.Wear, landmark.Width, landmark.Depth);

                        if (tiers.Beyond(p) || !Place(plan, placed, index, avenues, ground, waters, model, p, yaw, landmark.Role, 2f, pad, false, pad ? 6 : 10))
                        {
                            continue;
                        }

                        var building = plan.Buildings[plan.Buildings.Count - 1];
                        building.Name = landmark.Name;
                        building.District = heartId;
                        Beside(plan, placed, index, avenues, ground, waters, building, landmark.Things, tag, ref rng);
                        break;
                    }
                }
            }

            return squares;
        }

        /// <summary>Things by a building's sides and front corners (jars, racks, a banner).</summary>
        static void Beside(SettlementPlan plan, List<Box> placed, ShapeIndex<Box> index, SegmentIndex streets, IGround ground, Waters waters, Placed building, string[] things, string tag, ref Rng rng)
        {
            var k = 0;

            foreach (var thing in things)
            {
                var box = building.Footprint;
                var side = k % 2 == 0 ? 1 : -1;
                var at = box.World(new Vec2(side * (box.Half.X + 2.2f), box.Half.Z - 2 - k / 2 * 3));
                Place(plan, placed, index, streets, ground, waters, $"thing:{thing}:{rng.Range(1, 9)}:{tag}", at, building.Yaw, "prop", 0.3f, false);
                k++;
            }
        }

        /// <summary>
        /// The factions' districts: each faction's manor on the inner edge of its bearings facing the
        /// heart, its kitchen, storehouse and training ground near it. Returns where each manor stands.
        /// </summary>
        static void FactionSeats(SettlementPlan plan, Tiers tiers, List<Box> placed, ShapeIndex<Box> index, SegmentIndex avenues, IGround ground, Waters waters, ref Rng rng)
        {
            var town = tiers.Town;
            var def = plan.Def;

            for (var f = 0; f < town.Factions.Length; f++)
            {
                var faction = town.Factions[f];
                var runs = tiers.Runs(f);

                if (runs.Count == 0 || Span(runs[0].from, runs[0].to) < 4)
                {
                    continue;
                }

                var (from, to) = runs[0];
                var span = Span(from, to);
                var district = new District
                {
                    Id = faction.Tag, Name = faction.Name, Tag = faction.Tag, Tier = 1, Inner = tiers.Edges[0], Outer = tiers.Edges[1], From = from, To = to,
                };
                plan.Districts.Add(district);
                Vec2 seat = default;
                var hasSeat = false;

                foreach (var landmark in town.District)
                {
                    var thing = landmark.Style == "training";
                    var manor = landmark.Style == "manor";

                    for (var attempt = 0; attempt < 700; attempt++)
                    {
                        // The manor in the middle of the bearings near the heart (a smaller one where none will go); the rest about it.
                        // Where the faction's own ground will not take it (steep, crowded), beside it, a ring farther out.
                        var astray = attempt >= 400;
                        var scale = manor ? 1 - 0.13f * (Math.Min(399, attempt) / 100) : 1;
                        var t = astray ? rng.Range(-0.4f, 1.4f) : manor ? 0.5f + rng.Range(-0.5f, 0.5f) * Math.Min(1, 0.15f + (attempt % 100) * 0.012f) : rng.Range(0.08f, 0.92f);
                        var bearing = from + span * t;
                        var r = astray ? rng.Range(tiers.Edges[0] + 4, tiers.Edges[Math.Min(2, tiers.Edges.Length - 1)]) :
                            manor ? tiers.Edges[0] + 4 + landmark.Depth * 0.5f * scale + (attempt % 100) * 0.009f * (tiers.Edges[1] - tiers.Edges[0]) : rng.Range(tiers.Edges[0] + 6, tiers.Edges[1] - 8);
                        var p = plan.Centre + Vec2.FromYaw(bearing) * r;

                        if (hasSeat && !manor && Vec2.Distance(p, seat) > 30 + attempt * 0.5f)
                        {
                            continue;
                        }

                        if (!astray && (tiers.TierOf(p) != 1 || tiers.FactionOf(p) != f) || astray && tiers.TierOf(p) == 0)
                        {
                            continue;
                        }

                        var yaw = (float)Math.Round((plan.Centre - p).Yaw / 5) * 5 + (attempt % 3 == 0 ? 0 : attempt % 3 == 1 ? 90 : -90);
                        var pad = landmark.Pad && !tiers.Village;
                        var model = thing
                            ? $"thing:{(town.Clan == "xiong" ? "ring" : "training")}:{def.Seed * 7 + f}:{faction.Tag}"
                            : Generated(pad, landmark.Style, 1 + rng.Range(0, 24), faction.Tag, landmark.Wealth, landmark.Wear, (float)Math.Round(landmark.Width * scale), (float)Math.Round(landmark.Depth * scale));

                        // (A training ground is levelled into the slope.)
                        if (!Place(plan, placed, index, avenues, ground, waters, model, p, yaw, landmark.Role, 2f, pad || thing, false, thing ? (tiers.Village ? 7 : 6) : pad ? 6 : tiers.Village ? 12 : 10))
                        {
                            continue;
                        }

                        var building = plan.Buildings[plan.Buildings.Count - 1];
                        building.Name = landmark.Name + " — " + faction.Name;
                        building.District = faction.Tag;
                        Beside(plan, placed, index, avenues, ground, waters, building, landmark.Things, faction.Tag, ref rng);

                        if (manor)
                        {
                            seat = building.Footprint.Centre;
                            hasSeat = true;
                            district.Seat = building.Door;
                            district.HasSeat = true;
                        }

                        break;
                    }
                }
            }
        }

        /// <summary>
        /// The factions' fields at the outer edge of their bearings, between the streets: rice and
        /// vegetables (painted on the ground), the medicine faction's beds of herbs, the Xiong's hives
        /// and hide racks; and their banners along the lanes of their districts.
        /// </summary>
        static void FactionGrounds(SettlementPlan plan, Tiers tiers, List<Box> placed, ShapeIndex<Box> index, SegmentIndex streets, IGround ground, Waters waters, ref Rng rng)
        {
            var town = tiers.Town;
            var def = plan.Def;

            foreach (var district in plan.Districts)
            {
                var f = Array.FindIndex(town.Factions, x => x.Tag == district.Tag);

                if (f < 0)
                {
                    continue;
                }

                var faction = town.Factions[f];
                var span = Span(district.From, district.To);
                var made = 0;

                for (var attempt = 0; attempt < 120 && made < 3; attempt++)
                {
                    var bearing = district.From + span * rng.Range(0.05f, 0.95f);
                    var r = rng.Range(district.Outer - 34, district.Outer - 6);
                    var p = plan.Centre + Vec2.FromYaw(bearing) * r;

                    if (tiers.TierOf(p) != 1 || tiers.FactionOf(p) != f)
                    {
                        continue;
                    }

                    var yaw = (plan.Centre - p).Yaw;

                    switch (faction.Fields)
                    {
                        case "herbs":
                        case "bees":
                        case "hides":
                        {
                            // Rows of beds, hives or racks side by side.
                            var kind = faction.Fields == "herbs" ? "herbs" : faction.Fields == "bees" ? "hives" : "hide_rack";
                            var (w, _) = ThingGen.Footprint(kind);
                            var any = false;

                            for (var k = -1; k <= 1; k++)
                            {
                                var at = p + Vec2.FromYaw(yaw + 90) * (k * (w + 1.2f));
                                any |= Place(plan, placed, index, streets, ground, waters, $"thing:{kind}:{rng.Range(1, 9)}:{faction.Tag}", at, yaw, "prop", 0.4f, kind == "herbs", false, 3);

                                if (any)
                                {
                                    plan.Buildings[plan.Buildings.Count - 1].District = faction.Tag;
                                }
                            }

                            made += any ? 1 : 0;
                            break;
                        }
                        default:
                        {
                            // A paddy or a plot, painted on the ground: clear of houses and streets, on ground that does not fall much.
                            var box = new Box(p, new Vec2(rng.Range(6f, 9f), rng.Range(4f, 6f)), yaw);
                            float low = float.MaxValue, high = float.MinValue;
                            var clear = !Crowded(index, box.Grown(1)) && !OnStreet(plan, streets, box.Grown(1));

                            foreach (var corner in box.Corners())
                            {
                                clear &= !ground.Blocked(corner, 2) && Vec2.Distance(corner, plan.Centre) < def.Radius - 3;
                                var h = ground.Height(corner.X, corner.Z);
                                low = Math.Min(low, h);
                                high = Math.Max(high, h);
                            }

                            if (clear && high - low < 4)
                            {
                                plan.Fields.Add(box);
                                index.Add(box.Centre, box.Radius, box);
                                made++;
                            }

                            break;
                        }
                    }
                }
            }
        }

        /// <summary>Each faction's banners along the lanes of its district, in the gaps the houses left.</summary>
        static void FactionBanners(SettlementPlan plan, Tiers tiers, List<Box> placed, ShapeIndex<Box> index, SegmentIndex streets, IGround ground, Waters waters, ref Rng rng)
        {
            var town = tiers.Town;

            for (var f = 0; f < town.Factions.Length; f++)
            {
                var faction = town.Factions[f];
                var banners = 0;

                foreach (var street in plan.Streets)
                {
                    var length = Lines.Length(street.Points);

                    for (var along = 8f; along < length - 8 && banners < 16; along += 13)
                    {
                        var p = Lines.At(street.Points, along, out var direction);

                        if (tiers.TierOf(p) != 1 || tiers.FactionOf(p) != f)
                        {
                            continue;
                        }

                        var side = ((int)(along / 13) % 2 == 0 ? 1 : -1);
                        var at = p + direction.Right * (street.HalfWidth + 0.9f) * side;

                        if (Place(plan, placed, index, streets, ground, waters, $"thing:banner_pole:{rng.Range(1, 9)}:{faction.Tag}", at, direction.Yaw + (side > 0 ? 90 : -90), "prop", 0.1f, false, true))
                        {
                            plan.Buildings[plan.Buildings.Count - 1].District = faction.Tag;
                            banners++;
                            along += 13;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// The heart's own wall: a white wall round the ring (the clan's precinct), a gatehouse where each
        /// avenue passes it, open where a lane does, broken off where the ground or a building will not take it.
        /// </summary>
        static void HeartWall(SettlementPlan plan, Tiers tiers, ShapeIndex<Box> index, SegmentIndex streets, IGround ground)
        {
            var town = tiers.Town;

            if (!town.HeartWall)
            {
                return;
            }

            var style = Ramparts.Get(town.HeartRampart);
            var radius = tiers.Edges[0] + 2;
            var clan = town.Clan;
            var gates = new List<float>();

            foreach (var street in plan.Streets)
            {
                if (!street.Main)
                {
                    continue;
                }

                for (var i = 1; i < street.Points.Count; i++)
                {
                    var a = Vec2.Distance(street.Points[i - 1], plan.Centre) - radius;
                    var b = Vec2.Distance(street.Points[i], plan.Centre) - radius;

                    if (a * b <= 0 && Math.Abs(a - b) > 1e-3f)
                    {
                        var at = street.Points[i - 1] + (street.Points[i] - street.Points[i - 1]) * (a / (a - b));
                        var bearing = (at - plan.Centre).Yaw;

                        if (!gates.Exists(g => Math.Abs(Mod(g - bearing + 180, 360) - 180) < 8))
                        {
                            gates.Add(bearing);
                        }
                    }
                }
            }

            var gateModel = $"fort:{style.Id}:gate:{plan.Def.Seed + 1}:{clan}:0";
            var gateHalf = Catalog.TryGet(gateModel, out var gateFoot) ? (float)(Math.Atan2(gateFoot.Across * 0.5f, radius) * 180 / Math.PI) : 2;

            foreach (var bearing in gates)
            {
                var at = plan.Centre + Vec2.FromYaw(bearing) * radius;
                var box = new Box(at, new Vec2(gateFoot.Across * 0.5f, gateFoot.Deep * 0.5f), bearing + 180);

                if (Crowded(index, box))
                {
                    continue;
                }

                plan.Buildings.Add(new Placed { Model = gateModel, At = at, Yaw = bearing + 180, Footprint = box, Role = "wall", Name = "Ворота сердца клана", District = clan + ":tier0" });
                index.Add(box.Centre, box.Radius, box);
            }

            var pieces = (int)(2 * Math.PI * radius / style.Piece);
            var run = new List<Placed>();

            void EndRun()
            {
                // A wall of fewer than three pieces is no wall: left out.
                if (run.Count >= 3)
                {
                    foreach (var piece in run)
                    {
                        plan.Buildings.Add(piece);
                        index.Add(piece.Footprint.Centre, piece.Footprint.Radius, piece.Footprint);
                    }
                }

                run.Clear();
            }

            for (var i = 0; i < pieces; i++)
            {
                var bearing = (i + 0.5f) * 360f / pieces;

                if (gates.Exists(g => Math.Abs(Mod(g - bearing + 180, 360) - 180) < gateHalf + 1))
                {
                    // A gate does not end a run: the pieces either side of it are one wall with it.
                    continue;
                }

                var at = plan.Centre + Vec2.FromYaw(bearing) * radius;
                var yaw = bearing + 180;
                var model = string.Format(Inv, "fort:{0}:wall:{1}:{2}:{3:0.0}", style.Id, 500 + i, clan, style.Piece + 0.3f);

                if (!Catalog.TryGet(model, out var foot))
                {
                    EndRun();
                    continue;
                }

                var piece = new Box(at, new Vec2(foot.Across * 0.5f, foot.Deep * 0.5f), yaw);
                var corners = piece.Corners();
                var blocked = false;

                foreach (var p in corners)
                {
                    blocked |= ground.Blocked(p, 1) || tiers.Beyond(p);
                }

                var fall = Math.Abs(ground.Height(corners[0].X, corners[0].Z) - ground.Height(corners[1].X, corners[1].Z));

                if (blocked || fall > 5 || Crowded(index, piece) || OnStreet(plan, streets, piece.Grown(1.2f)))
                {
                    EndRun();
                    continue;
                }

                run.Add(new Placed { Model = model, At = at, Yaw = yaw, Footprint = piece, Role = "wall", District = clan + ":tier0" });
            }

            EndRun();
        }

        /// <summary>The rings themselves as districts (the heart, the Gu Masters, the commoners, the poor); the factions' ring is theirs.</summary>
        static void Rings(SettlementPlan plan, Tiers tiers)
        {
            for (var i = 0; i < tiers.Edges.Length; i++)
            {
                if (i == 1)
                {
                    continue;
                }

                var tier = tiers.Town.Tiers[i];
                plan.Districts.Add(new District
                {
                    Id = tiers.Town.Clan + ":tier" + i, Name = tier.Name, Tag = tiers.Town.Clan, Tier = i,
                    Inner = i == 0 ? plan.Def.Core : tiers.Edges[i - 1], Outer = Math.Min(tiers.Edges[i], plan.Def.Radius),
                });
            }

            foreach (var building in plan.Buildings)
            {
                if (building.District != null || building.Role == "wall" || building.Role == "gate")
                {
                    continue;
                }

                var p = building.Footprint.Centre;
                var tier = tiers.TierOf(p);
                var f = tier == 1 ? tiers.FactionOf(p) : -1;
                building.District = f >= 0 ? tiers.Town.Factions[f].Tag : tiers.Town.Clan + ":tier" + tier;
            }
        }

        /// <summary>
        /// A traced lane eased (each point the mean of its neighbours) and cut where it still turns
        /// sharply (rough ground folds a contour back on itself): the pieces long enough are added.
        /// </summary>
        static int AddSmoothed(SettlementPlan plan, List<Vec2> lane, float half)
        {
            if (lane.Count < 5)
            {
                return 0;
            }

            var eased = new List<Vec2>(lane);

            for (var pass = 0; pass < 2; pass++)
            {
                var next = new List<Vec2>(eased);

                for (var i = 2; i < eased.Count - 2; i++)
                {
                    next[i] = (eased[i - 2] + eased[i - 1] + eased[i] + eased[i + 1] + eased[i + 2]) * 0.2f;
                }

                eased = next;
            }

            var made = 0;
            var piece = new List<Vec2> { eased[0] };

            for (var i = 1; i < eased.Count; i++)
            {
                if (piece.Count >= 2)
                {
                    var a = eased[i - 1] - eased[i - 2 < 0 ? 0 : i - 2];
                    var b = eased[i] - eased[i - 1];
                    var turn = Math.Abs(Mod(b.Yaw - a.Yaw + 180, 360) - 180);

                    if (turn > 45)
                    {
                        made += AddPiece(plan, piece, half) ? 1 : 0;
                        piece = new List<Vec2> { eased[i - 1] };
                    }
                }

                piece.Add(eased[i]);
            }

            made += AddPiece(plan, piece, half) ? 1 : 0;

            return made;
        }

        /// <summary>
        /// A mountain village's lanes as its people wore them: from the main street every so often,
        /// both ways, a lane that walks along the slope (across its fall, turning as the ground turns,
        /// never sharply) until the village's edge, a building or another lane; from the lanes, every
        /// so often, a stair straight up the fall to the lane above. Rough ground gives winding lanes,
        /// never a tangle. Returns the lanes made.
        /// </summary>
        internal static int ContourBranches(SettlementPlan plan, float radius, float inner, IGround ground, Func<Vec2, float, bool> taken, float spacing, float half, ref Rng rng)
        {
            var centre = plan.Centre;
            var made = 0;
            var lanes = new SegmentIndex(Regions.Half, 32, 16);
            var count = 0;

            foreach (var street in plan.Streets)
            {
                lanes.Add(street.Points, count++);
            }

            Vec2 Fall(Vec2 p)
            {
                var e = 3f;
                return new Vec2(ground.Height(p.X + e, p.Z) - ground.Height(p.X - e, p.Z), ground.Height(p.X, p.Z + e) - ground.Height(p.X, p.Z - e)) * (1 / (2 * e));
            }

            bool Inside(Vec2 p) => Vec2.Distance(p, centre) < radius && Vec2.Distance(p, centre) > inner;

            // Walks from a point in a direction, along the slope or up it; stops at the edge, a building, a lane met (joining it).
            List<Vec2> Walk(Vec2 from, Vec2 heading, bool up, float most)
            {
                var line = new List<Vec2> { from };
                var p = from;
                var dir = heading;

                for (var step = 0; step < most / 4; step++)
                {
                    var g = Fall(p);
                    var want = dir;

                    if (g.Length > 0.02f)
                    {
                        var n = g * (1 / g.Length);
                        want = up ? n : Vec2.Dot(n.Right, dir) >= 0 ? n.Right : n.Left;
                    }

                    var turned = dir * 0.65f + want * 0.35f;
                    dir = turned * (1 / Math.Max(1e-3f, turned.Length));
                    p += dir * 4;

                    if (!Inside(p) || taken(p, 4))
                    {
                        break;
                    }

                    line.Add(p);

                    // Met another lane (not the one it left): joins it there.
                    if (line.Count > 4 && lanes.Nearest(p, out var d, out _, out _) && d < 6)
                    {
                        break;
                    }
                }

                return line;
            }

            var mains = plan.Streets.FindAll(x => x.Main);
            var branches = new List<List<Vec2>>();

            foreach (var street in mains)
            {
                var length = Lines.Length(street.Points);

                for (var along = rng.Range(8f, 16f); along < length - 8; along += spacing * rng.Range(0.8f, 1.2f))
                {
                    var p = Lines.At(street.Points, along, out var direction);

                    if (!Inside(p))
                    {
                        continue;
                    }

                    foreach (var side in new[] { -1, 1 })
                    {
                        var start = p + direction.Right * side * (street.HalfWidth + 1);
                        var line = Walk(start, direction.Right * side, false, radius * 1.4f);

                        if (line.Count >= 5)
                        {
                            line.Insert(0, p);
                            AddPiece(plan, line, half);
                            lanes.Add(line, count++);
                            branches.Add(line);
                            made++;
                        }
                    }
                }
            }

            // Stairs up the fall from the lanes, to the lane above (or as far as the slope goes).
            foreach (var line in branches)
            {
                var length = Lines.Length(line);

                for (var along = spacing * 0.6f; along < length - 6; along += spacing * rng.Range(1.1f, 1.6f))
                {
                    var p = Lines.At(line, along, out _);
                    var stair = Walk(p, Fall(p), true, spacing * 1.6f);

                    if (AddPiece(plan, stair, 1.4f))
                    {
                        lanes.Add(stair, count++);
                    }
                }
            }

            return made;
        }

        /// <summary>A clan village being laid out by its tiers: what its two steps share.</summary>
        internal sealed class VillageLayout
        {
            internal Tiers Tiers;
            internal ShapeIndex<Box> Index;
            internal int Known;
        }

        static SegmentIndex StreetIndex(SettlementPlan plan)
        {
            var segments = new SegmentIndex(Regions.Half, 32, 12);

            for (var i = 0; i < plan.Streets.Count; i++)
            {
                segments.Add(plan.Streets[i].Points, i);
            }

            return segments;
        }

        /// <summary>
        /// A clan's village of the mountain by its tiers, first step (once its main street is laid, before
        /// anything else stands): the clan's hall facing the plaza from the high side, the heart's other
        /// buildings, the factions' seats.
        /// </summary>
        internal static VillageLayout VillageHeart(SettlementPlan plan, ClanTown town, List<Box> placed, IGround ground, Waters waters, ref Rng rng)
        {
            var layout = new VillageLayout { Index = new ShapeIndex<Box>(Regions.Half, 32), Tiers = new Tiers(town, plan.Def, ground, plan.PlazaRadius + 2) { Village = true } };

            foreach (var box in placed)
            {
                layout.Index.Add(box.Centre, box.Radius, box);
            }

            var main = StreetIndex(plan);
            Heart(plan, layout.Tiers, placed, layout.Index, main, ground, waters, ref rng);
            layout.Known = placed.Count;

            return layout;
        }

        /// <summary>
        /// The second step, once the clan's buildings stand round the plaza: the factions' seats; lanes
        /// along the slope round them all; the factions' grounds, the lots ring by ring with the named
        /// ones first, the banners. Returns the houses made.
        /// </summary>
        internal static int VillageRest(SettlementPlan plan, VillageLayout layout, List<Box> placed, IGround ground, Waters waters, ref Rng rng)
        {
            var def = plan.Def;
            var tiers = layout.Tiers;
            var index = layout.Index;

            for (var i = layout.Known; i < placed.Count; i++)
            {
                index.Add(placed[i].Centre, placed[i].Radius, placed[i]);
            }

            FactionSeats(plan, tiers, placed, index, StreetIndex(plan), ground, waters, ref rng);

            // Lanes along the slope, round the clan's buildings; then lanes from the main street to where those did not reach.
            var great = new List<Box>();

            foreach (var box in placed)
            {
                great.Add(box.Grown(3));
            }

            bool Taken(Vec2 p, float margin)
            {
                if (ground.Blocked(p, margin))
                {
                    return true;
                }

                foreach (var box in great)
                {
                    if (box.Distance(p) < margin)
                    {
                        return true;
                    }
                }

                return false;
            }

            plan.Lanes = ContourLanes(plan, def.Radius - 10, plan.PlazaRadius + 2, ground, Taken, 8f, 1.8f, 26f, 40f, ref rng);
            plan.Lanes += ContourBranches(plan, def.Radius - 10, plan.PlazaRadius + 2, ground, Taken, 34f, 1.8f, ref rng);
            var streets = StreetIndex(plan);
            FactionGrounds(plan, tiers, placed, index, streets, ground, waters, ref rng);
            var houses = Lots.Fill(plan, def.Houses, tiers.At, 1.8f, 1.4f, 0.85f, 0.5f, (model, centre, yaw, role) =>
                Place(plan, placed, index, streets, ground, waters, model, centre, yaw, role, 0.6f, false), ref rng, plan.PlazaRadius + 4, Specials(tiers.Town));
            FactionBanners(plan, tiers, placed, index, streets, ground, waters, ref rng);
            Rings(plan, tiers);

            return houses;
        }

        /// <summary>The buildings of the outer tiers placed by name on lots (the taverns, the inn, the market rows, the lodging).</summary>
        static List<SpecialLot> Specials(ClanTown town)
        {
            var list = new List<SpecialLot>();

            foreach (var landmark in town.Landmarks)
            {
                if (landmark.Tier == 0)
                {
                    continue;
                }

                for (var n = 0; n < landmark.Count; n++)
                {
                    list.Add(new SpecialLot
                    {
                        Lot = new LotKind(landmark.Style, 1, landmark.Width, landmark.Depth, landmark.Wealth, landmark.Wealth, landmark.Role == "shop", landmark.Wear),
                        Role = landmark.Role, Name = landmark.Name, Tier = landmark.Tier, Main = landmark.Style == "market_row",
                        Tag = landmark.Faction.Length > 0 ? town.Clan + "." + landmark.Faction : null, Things = landmark.Things,
                    });
                }
            }

            return list;
        }
    }
}
