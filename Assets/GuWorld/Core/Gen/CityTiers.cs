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
        sealed class Tiers
        {
            public readonly ClanTown Town;
            public readonly Vec2 Centre;
            public readonly float Radius, Core;
            /// <summary>The outer edge of each tier, metres from the middle.</summary>
            public readonly float[] Edges;
            /// <summary>The faction holding each 2° of bearing in the factions' ring (-1: none).</summary>
            public readonly int[] Sector = new int[180];
            public float PavilionBearing;
            readonly IGround ground;
            readonly Dictionary<(int, int), bool> beyond = new Dictionary<(int, int), bool>();

            public Tiers(ClanTown town, SettlementDef def, IGround ground)
            {
                Town = town;
                Centre = def.At;
                Radius = def.Radius;
                Core = def.Core;
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
                        good += r < Radius - 5 && !ground.Blocked(p, 4) && !Beyond(p) ? 1 : 0;
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

            for (var bearing = 0f; bearing < 360; bearing += 5)
            {
                var nearest = def.Core + 6 + squareD + main.Depth * 0.5f;

                for (var r = nearest; r < Math.Max(nearest + 30, tiers.Edges[0] - main.Depth * 0.5f - 4); r += 7)
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
                        round += dq < def.Core || dq < def.Radius - 10 && !ground.Blocked(q, 4) && !tiers.Beyond(q) ? 1 : 0;
                    }

                    candidates.Add((p, bearing, ground.Height(p.X, p.Z) - (r - def.Core) * 0.12f - (16 - round) * 3));
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
                var clear = !Crowded(index, square);

                foreach (var corner in square.Corners())
                {
                    var d = Vec2.Distance(corner, plan.Centre);
                    clear &= d > def.Core + 1 && d < def.Radius - 4 && !ground.Blocked(corner, 2);
                    var h = ground.Height(corner.X, corner.Z);
                    low = Math.Min(low, h);
                    high = Math.Max(high, h);
                }

                if (!clear || high - low > 7)
                {
                    continue;
                }

                var model = Generated(true, main.Style, 1 + rng.Range(0, 24), town.Clan, main.Wealth, main.Wear, main.Width, main.Depth);

                // The clan's terrace may be cut deep into the slope.
                if (!Place(plan, placed, index, avenues, ground, waters, model, p, yaw, main.Role, 1.5f, true, false, 8))
                {
                    continue;
                }

                var pavilion = plan.Buildings[plan.Buildings.Count - 1];
                pavilion.Name = main.Name;
                pavilion.District = heartId;

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
                    var lo = def.Core + 6 + landmark.Depth * 0.5f;
                    var hi = Math.Max(lo + 25, tiers.Edges[0] - 3 - landmark.Depth * 0.5f);

                    for (var attempt = 0; attempt < 300; attempt++)
                    {
                        // (Where the heart has no room left for it, just outside it.)
                        var bearing = near && found ? tiers.PavilionBearing + rng.Range(-1f, 1f) * (25 + attempt * 0.8f) : rng.Range(0f, 360f);
                        var p = plan.Centre + Vec2.FromYaw(bearing) * rng.Range(lo, hi + Math.Max(0, attempt - 150) * 0.4f);
                        // Facing the middle, or (a long yard that will not take the slope that way) turned along it.
                        var yaw = (float)Math.Round((plan.Centre - p).Yaw / 5) * 5 + (attempt % 3 == 0 ? 0 : attempt % 3 == 1 ? 90 : -90);
                        var tag = landmark.Faction.Length > 0 ? town.Clan + "." + landmark.Faction : town.Clan;
                        var model = Generated(landmark.Pad, landmark.Style, 1 + rng.Range(0, 24), tag, landmark.Wealth, landmark.Wear, landmark.Width, landmark.Depth);

                        if (tiers.Beyond(p) || !Place(plan, placed, index, avenues, ground, waters, model, p, yaw, landmark.Role, 2f, landmark.Pad, false, landmark.Pad ? 6 : 10))
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

                    for (var attempt = 0; attempt < 400; attempt++)
                    {
                        // The manor in the middle of the bearings near the heart (a smaller one where none will go); the rest about it.
                        var scale = manor ? 1 - 0.13f * (attempt / 100) : 1;
                        var t = manor ? 0.5f + rng.Range(-0.5f, 0.5f) * Math.Min(1, 0.15f + (attempt % 100) * 0.012f) : rng.Range(0.08f, 0.92f);
                        var bearing = from + span * t;
                        var r = manor ? tiers.Edges[0] + 4 + landmark.Depth * 0.5f * scale + (attempt % 100) * 0.009f * (tiers.Edges[1] - tiers.Edges[0]) : rng.Range(tiers.Edges[0] + 6, tiers.Edges[1] - 8);
                        var p = plan.Centre + Vec2.FromYaw(bearing) * r;

                        if (hasSeat && !manor && Vec2.Distance(p, seat) > 30 + attempt * 0.5f)
                        {
                            continue;
                        }

                        if (tiers.TierOf(p) != 1 || tiers.FactionOf(p) != f)
                        {
                            continue;
                        }

                        var yaw = (float)Math.Round((plan.Centre - p).Yaw / 5) * 5 + (attempt % 3 == 0 ? 0 : attempt % 3 == 1 ? 90 : -90);
                        var model = thing
                            ? $"thing:training:{def.Seed * 7 + f}:{faction.Tag}"
                            : Generated(landmark.Pad, landmark.Style, 1 + rng.Range(0, 24), faction.Tag, landmark.Wealth, landmark.Wear, (float)Math.Round(landmark.Width * scale), (float)Math.Round(landmark.Depth * scale));

                        if (!Place(plan, placed, index, avenues, ground, waters, model, p, yaw, landmark.Role, 2f, landmark.Pad || thing, false, thing ? 5 : landmark.Pad ? 6 : 10))
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
                                any |= Place(plan, placed, index, streets, ground, waters, $"thing:{kind}:{rng.Range(1, 9)}:{faction.Tag}", at, yaw, "prop", 0.4f, kind == "herbs");

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
