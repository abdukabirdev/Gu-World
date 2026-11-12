using System;
using System.Collections.Generic;

namespace GuWorld.Core.Gen
{
    /// <summary>What a townsperson is: their trade decides where their day takes them.</summary>
    public enum Trade { Trader, Porter, Farmer, Student, Guard, Elder, Child, Idler, Watch }

    /// <summary>One stretch of a townsperson's day: from an hour to an hour, walking a path or standing at its end.</summary>
    public sealed class Leg
    {
        public float From;
        public float To;
        /// <summary>The way walked (empty: standing at <see cref="Stand"/>).</summary>
        public Vec2[] Path = new Vec2[0];
        public float Length;
        public Vec2 Stand;
        public float Facing;
        /// <summary>What they do while standing: "stall", "talk", "sit", "work", "watch", "home" (indoors: not seen).</summary>
        public string Doing = "";
    }

    /// <summary>A townsperson: a look, a trade, a home, and the legs of their day (the hours wrap at 24).</summary>
    public sealed class Walker
    {
        public int Id;
        public string Look;
        public Trade Trade;
        public Vec2 Home;
        public readonly List<Leg> Day = new List<Leg>();
        /// <summary>The clan of the settlement (its students, guards and elders bear its name).</summary>
        public string Clan = "gu_yue";
        /// <summary>The variant of the stall a trader keeps (-1: none).</summary>
        public int Stall = -1;
        /// <summary>Walking pace, metres a second (an elder slower, a child quicker).</summary>
        public float Pace = 1.3f;
    }

    /// <summary>Where a townsperson is at an hour and what they are doing.</summary>
    public struct WalkerPose
    {
        public Vec2 At;
        public float Facing;
        public bool Walking;
        public bool Indoors;
        public string Doing;
    }

    /// <summary>
    /// The town's life: a few hundred of its people, each with a day — out of the house in the morning,
    /// to the stall, the field, the hall or the gate, a meal at noon at an inn or the square, back to
    /// work, an evening on the square or in a tavern, home at dark — walked along the town's streets
    /// (paths found on the street graph). Everything is worked out at load on a worker thread and
    /// deterministic; the engine only asks where each one is now and dresses the nearest.
    /// </summary>
    public static class Crowd
    {
        /// <summary>The street graph of a town: nodes on the streets' points, joined along each street and where streets meet.</summary>
        public sealed class Graph
        {
            public readonly List<Vec2> Nodes = new List<Vec2>();
            public readonly List<List<(int to, float cost)>> Edges = new List<List<(int, float)>>();
            readonly ShapeIndex<int> index;

            public Graph(SettlementPlan plan)
            {
                index = new ShapeIndex<int>(Regions.Half, 16);
                var ends = new List<int>();
                var streetOf = new List<int>();

                for (var s = 0; s < plan.Streets.Count; s++)
                {
                    var street = plan.Streets[s];
                    var first = -1;

                    for (var i = 0; i < street.Points.Count; i++)
                    {
                        var node = Add(street.Points[i]);
                        streetOf.Add(s);

                        if (i == 0 || i == street.Points.Count - 1)
                        {
                            ends.Add(node);
                        }

                        if (first >= 0)
                        {
                            Join(node - 1, node);
                        }

                        first = node;
                    }
                }

                // Where streets cross, join their nearest points (each street to each other street it meets, once a few metres).
                for (var node = 0; node < Nodes.Count; node++)
                {
                    foreach (var other in index.Near(Nodes[node], 3))
                    {
                        if (other > node && streetOf[other] != streetOf[node] && Vec2.Distance(Nodes[node], Nodes[other]) < 3 && !Linked(node, other))
                        {
                            Join(node, other);
                        }
                    }
                }

                // Where a street's end stops short of another street, join it across the gap.
                foreach (var end in ends)
                {
                    var p = Nodes[end];
                    var best = -1;
                    var bestDistance = 16f;

                    foreach (var other in index.Near(p, 16))
                    {
                        if (streetOf[other] == streetOf[end])
                        {
                            continue;
                        }

                        var d = Vec2.Distance(p, Nodes[other]);

                        if (d < bestDistance && !Linked(end, other))
                        {
                            bestDistance = d;
                            best = other;
                        }
                    }

                    if (best >= 0)
                    {
                        Join(end, best);
                    }
                }

                // Parts left apart (a lane cut off by a great hall's grounds) are joined to the rest by
                // their nearest pair of points, as the townsfolk would cut across a yard.
                for (var round = 0; round < 40; round++)
                {
                    var label = Label(out var count);

                    if (count <= 1)
                    {
                        break;
                    }

                    var joined = false;

                    for (var part = 1; part < count; part++)
                    {
                        var best = (-1, -1);
                        var bestDistance = 60f;

                        for (var node = 0; node < Nodes.Count; node++)
                        {
                            if (label[node] != part)
                            {
                                continue;
                            }

                            foreach (var other in index.Near(Nodes[node], bestDistance))
                            {
                                if (label[other] == part)
                                {
                                    continue;
                                }

                                var d = Vec2.Distance(Nodes[node], Nodes[other]);

                                if (d < bestDistance)
                                {
                                    bestDistance = d;
                                    best = (node, other);
                                }
                            }
                        }

                        if (best.Item1 >= 0)
                        {
                            Join(best.Item1, best.Item2);
                            joined = true;
                        }
                    }

                    if (!joined)
                    {
                        break;
                    }
                }
            }

            /// <summary>Each node's part (0 the biggest), and how many parts there are.</summary>
            int[] Label(out int count)
            {
                var label = new int[Nodes.Count];
                var sizes = new List<(int part, int size)>();

                for (var i = 0; i < label.Length; i++)
                {
                    label[i] = -1;
                }

                var parts = 0;

                for (var start = 0; start < Nodes.Count; start++)
                {
                    if (label[start] >= 0)
                    {
                        continue;
                    }

                    var size = 0;
                    var stack = new Stack<int>();
                    stack.Push(start);
                    label[start] = parts;

                    while (stack.Count > 0)
                    {
                        var at = stack.Pop();
                        size++;

                        foreach (var (next, _) in Edges[at])
                        {
                            if (label[next] < 0)
                            {
                                label[next] = parts;
                                stack.Push(next);
                            }
                        }
                    }

                    sizes.Add((parts, size));
                    parts++;
                }

                // Renumber so the biggest part is 0.
                sizes.Sort((a, b) => b.size.CompareTo(a.size));
                var order = new int[parts];

                for (var i = 0; i < sizes.Count; i++)
                {
                    order[sizes[i].part] = i;
                }

                for (var i = 0; i < label.Length; i++)
                {
                    label[i] = order[label[i]];
                }

                count = parts;

                return label;
            }

            int Add(Vec2 p)
            {
                Nodes.Add(p);
                Edges.Add(new List<(int, float)>());
                index.Add(p, 0.1f, Nodes.Count - 1);

                return Nodes.Count - 1;
            }

            void Join(int a, int b)
            {
                var cost = Vec2.Distance(Nodes[a], Nodes[b]);
                Edges[a].Add((b, cost));
                Edges[b].Add((a, cost));
            }

            bool Linked(int a, int b) => Edges[a].Exists(e => e.to == b);

            /// <summary>The graph's parts that are not joined to one another (one, in a well-made town), the biggest first.</summary>
            public List<int> Components()
            {
                var seen = new int[Nodes.Count];
                var sizes = new List<int>();

                for (var start = 0; start < Nodes.Count; start++)
                {
                    if (seen[start] != 0)
                    {
                        continue;
                    }

                    var size = 0;
                    var stack = new Stack<int>();
                    stack.Push(start);
                    seen[start] = 1;

                    while (stack.Count > 0)
                    {
                        var at = stack.Pop();
                        size++;

                        foreach (var (next, _) in Edges[at])
                        {
                            if (seen[next] == 0)
                            {
                                seen[next] = 1;
                                stack.Push(next);
                            }
                        }
                    }

                    sizes.Add(size);
                }

                sizes.Sort((a, b) => b.CompareTo(a));

                return sizes;
            }

            /// <summary>The node nearest a point (within `reach`), or -1.</summary>
            public int Nearest(Vec2 p, float reach = 60)
            {
                var best = -1;
                var bestDistance = reach;

                foreach (var node in index.Near(p, reach))
                {
                    var d = Vec2.Distance(p, Nodes[node]);

                    if (d < bestDistance)
                    {
                        bestDistance = d;
                        best = node;
                    }
                }

                return best;
            }

            /// <summary>The shortest way along the streets between two nodes (A*), as points; null if there is none.</summary>
            public List<Vec2> Path(int from, int to)
            {
                if (from < 0 || to < 0)
                {
                    return null;
                }

                if (from == to)
                {
                    return new List<Vec2> { Nodes[from] };
                }

                var cost = new Dictionary<int, float> { [from] = 0 };
                var came = new Dictionary<int, int>();
                var open = new MinHeap();
                open.Push(from, Vec2.Distance(Nodes[from], Nodes[to]));
                var closed = new HashSet<int>();

                while (open.Count > 0)
                {
                    var at = open.Pop();

                    if (at == to)
                    {
                        var path = new List<Vec2>();

                        for (var n = to; ; n = came[n])
                        {
                            path.Add(Nodes[n]);

                            if (n == from)
                            {
                                break;
                            }
                        }

                        path.Reverse();

                        return path;
                    }

                    if (!closed.Add(at))
                    {
                        continue;
                    }

                    foreach (var (next, step) in Edges[at])
                    {
                        var c = cost[at] + step;

                        if (!cost.TryGetValue(next, out var known) || c < known)
                        {
                            cost[next] = c;
                            came[next] = at;
                            open.Push(next, c + Vec2.Distance(Nodes[next], Nodes[to]));
                        }
                    }
                }

                return null;
            }
        }

        /// <summary>A small binary heap of nodes by priority.</summary>
        sealed class MinHeap
        {
            readonly List<(int node, float key)> items = new List<(int, float)>();

            public int Count => items.Count;

            public void Push(int node, float key)
            {
                items.Add((node, key));
                var i = items.Count - 1;

                while (i > 0)
                {
                    var parent = (i - 1) / 2;

                    if (items[parent].key <= items[i].key)
                    {
                        break;
                    }

                    (items[parent], items[i]) = (items[i], items[parent]);
                    i = parent;
                }
            }

            public int Pop()
            {
                var top = items[0].node;
                var last = items[items.Count - 1];
                items.RemoveAt(items.Count - 1);

                if (items.Count > 0)
                {
                    items[0] = last;
                    var i = 0;

                    while (true)
                    {
                        var l = i * 2 + 1;
                        var r = l + 1;
                        var least = i;

                        if (l < items.Count && items[l].key < items[least].key)
                        {
                            least = l;
                        }

                        if (r < items.Count && items[r].key < items[least].key)
                        {
                            least = r;
                        }

                        if (least == i)
                        {
                            break;
                        }

                        (items[least], items[i]) = (items[i], items[least]);
                        i = least;
                    }
                }

                return top;
            }
        }

        static readonly string[] TownLooks = { "villager", "farmer", "merchant", "cook", "scribe", "smith", "keeper", "herbalist", "student", "monk", "gambler", "child", "elder", "guard" };

        /// <summary>The townsfolk of a town plan: `count` of them (a few hundred is a crowd; the rest are indoors).</summary>
        public static List<Walker> Plan(SettlementPlan plan, int count, int seed)
        {
            var walkers = new List<Walker>();
            var graph = new Graph(plan);

            if (graph.Nodes.Count < 2)
            {
                return walkers;
            }

            var rng = new Rng(seed * 7919 + 17);
            var homes = Spots(plan, "house");
            var stalls = Spots(plan, "stall");
            var inns = Spots(plan, "inn");
            var plazas = Spots(plan, "plaza");
            var halls = new List<Spot>();
            halls.AddRange(Spots(plan, "hall"));
            halls.AddRange(Spots(plan, "academy"));
            halls.AddRange(Spots(plan, "landmark"));
            halls.AddRange(Spots(plan, "yard"));
            var works = new List<Spot>();
            works.AddRange(Spots(plan, "smithy"));
            works.AddRange(Spots(plan, "granary"));
            works.AddRange(Spots(plan, "shop"));
            works.AddRange(Spots(plan, "herbalist"));
            works.AddRange(Spots(plan, "nursery"));

            if (homes.Count == 0)
            {
                return walkers;
            }

            // The square's ring and the gates: where people idle and guards stand.
            var square = new List<Spot>(plazas);

            for (var i = 0; i < 16; i++)
            {
                var at = plan.Centre + Vec2.FromYaw(i * 22.5f) * (plan.PlazaRadius * 0.7f);
                square.Add(new Spot { At = at, Facing = (plan.Centre - at).Yaw, Role = "plaza" });
            }

            var stallTaken = new bool[stalls.Count];

            for (var n = 0; n < count; n++)
            {
                var trade = (Trade)rng.Weighted(new[] { 1.4f, 1.2f, 1.0f, 0.9f, 0.5f, 0.4f, 0.7f, 1.0f });
                var home = homes[rng.Range(0, homes.Count)];
                var walker = new Walker
                {
                    Id = n,
                    Trade = trade,
                    Home = home.At,
                    Look = LookOf(trade, ref rng),
                    Pace = trade == Trade.Elder ? 0.9f : trade == Trade.Child ? 1.7f : trade == Trade.Porter ? 1.2f : rng.Range(1.15f, 1.45f),
                };

                // Where their work is.
                Spot work;

                switch (trade)
                {
                    case Trade.Trader:
                    {
                        var free = Array.IndexOf(stallTaken, false);

                        if (free >= 0 && stalls.Count > 0)
                        {
                            stallTaken[free] = true;
                            // Behind the counter (the spot is the customer's, before the stall), facing out.
                            work = stalls[free];
                            walker.Stall = StallVariant(plan, work.At);
                            work.At = work.At - Vec2.FromYaw(work.Facing) * 3f;
                        }
                        else
                        {
                            work = Pick(works.Count > 0 ? works : square, ref rng);
                        }

                        break;
                    }
                    case Trade.Farmer:
                        work = FieldSpot(plan, ref rng);
                        break;
                    case Trade.Student:
                    case Trade.Elder:
                        work = Pick(halls.Count > 0 ? halls : square, ref rng);
                        break;
                    case Trade.Guard:
                        work = GateSpot(plan, ref rng);
                        break;
                    case Trade.Porter:
                        work = Pick(works.Count > 0 ? works : stalls.Count > 0 ? stalls : square, ref rng);
                        break;
                    default:
                        work = Pick(square, ref rng);
                        break;
                }

                // The day: the hours shift a little from one person to the next.
                var wake = 6 + rng.Range(0f, 1.5f);
                var noon = 11.5f + rng.Range(0f, 1.2f);
                var back = noon + 1 + rng.Range(0f, 0.6f);
                var evening = 17.5f + rng.Range(0f, 1.5f);
                var night = 20.5f + rng.Range(0f, 2f);
                var meal = rng.Chance(0.5f) && inns.Count > 0 ? Pick(inns, ref rng) : Pick(square, ref rng);
                var leisure = rng.Chance(0.4f) && inns.Count > 0 ? Pick(inns, ref rng) : Pick(square, ref rng);
                var doing = trade == Trade.Trader ? "stall" : trade == Trade.Guard ? "watch" : trade == Trade.Farmer || trade == Trade.Porter ? "work" : trade == Trade.Child ? "play" : "talk";

                // Children and idlers wander between the square and the stalls instead of working.
                if (trade == Trade.Child || trade == Trade.Idler)
                {
                    var stops = new List<Spot> { Pick(square, ref rng), stalls.Count > 0 ? Pick(stalls, ref rng) : Pick(square, ref rng), Pick(square, ref rng) };
                    var hour = wake;
                    var from = home;

                    for (var i = 0; i < stops.Count; i++)
                    {
                        var until = i == stops.Count - 1 ? evening : Math.Min(evening - 1, hour + rng.Range(1.2f, 2.6f));
                        hour = Walk(walker, graph, from, stops[i], hour, until, trade == Trade.Child ? "play" : "talk");
                        from = stops[i];
                    }

                    hour = Walk(walker, graph, from, leisure, hour, night, "talk");
                    Walk(walker, graph, leisure, home, night, wake + 24, "home");
                }
                else if (trade == Trade.Porter && stalls.Count > 0)
                {
                    // A porter carries all day: between their work and the stalls, there and back.
                    var hour = Walk(walker, graph, home, work, wake, wake + 0.5f, doing);
                    var at = work;

                    foreach (var (until, then) in new[] { (noon, meal), (evening, leisure) })
                    {
                        while (hour + 1.2f < until)
                        {
                            var next = at.Role == "stall" ? work : Pick(stalls, ref rng);
                            hour = Walk(walker, graph, at, next, hour, Math.Min(until - 0.6f, hour + rng.Range(0.4f, 0.9f)), "work");
                            at = next;
                        }

                        hour = Walk(walker, graph, at, then, hour, until == noon ? back : night, "sit");
                        at = then;
                    }

                    Walk(walker, graph, at, home, hour, wake + 24, "home");
                }
                else
                {
                    var hour = Walk(walker, graph, home, work, wake, noon, doing);
                    hour = Walk(walker, graph, work, meal, hour, back, "sit");
                    hour = Walk(walker, graph, meal, work, hour, evening, doing);
                    hour = Walk(walker, graph, work, leisure, hour, night, leisure.Role == "inn" ? "sit" : "talk");
                    Walk(walker, graph, leisure, home, hour, wake + 24, "home");
                }

                walkers.Add(walker);
            }

            // The night watch: they sleep by day and walk the streets with lanterns from dusk to dawn,
            // from crossing to crossing, calling the hours.
            var crossings = new List<Spot>();

            foreach (var street in plan.Streets)
            {
                crossings.Add(new Spot { At = street.Points[0], Role = "crossing" });
                crossings.Add(new Spot { At = street.Points[street.Points.Count - 1], Role = "crossing" });
            }

            var watch = Math.Max(4, count / 30);

            for (var n = 0; n < watch && crossings.Count > 1; n++)
            {
                var home = homes[rng.Range(0, homes.Count)];
                var walker = new Walker { Id = count + n, Trade = Trade.Watch, Home = home.At, Look = "guard", Pace = 1.1f };
                var hour = 19.5f + rng.Range(0f, 0.6f);
                var dawn = 29.5f + rng.Range(0f, 0.5f);
                // A walk from home to the first crossing, then round, a short stand at each.
                var at = home;
                var first = true;

                while (hour < dawn - 1)
                {
                    var next = crossings[rng.Range(0, crossings.Count)];

                    if (Vec2.Distance(next.At, at.At) < 30)
                    {
                        continue;
                    }

                    var until = Math.Min(dawn - 0.6f, hour + rng.Range(0.6f, 1.1f));
                    hour = Walk(walker, graph, at, next, hour, until, "watch");
                    at = next;
                    first = false;
                }

                if (!first)
                {
                    // Home by dawn, asleep through the day (round to the next dusk).
                    Walk(walker, graph, at, home, hour, walker.Day[0].From + 24, "home");
                    walkers.Add(walker);
                }
            }

            return walkers;
        }

        /// <summary>The variant of the stall whose customer's spot this is.</summary>
        static int StallVariant(SettlementPlan plan, Vec2 spot)
        {
            foreach (var building in plan.Buildings)
            {
                if (building.Role == "stall" && Vec2.Distance(building.Door, spot) < 0.5f)
                {
                    var parts = building.Model.Split(':');

                    return parts.Length > 2 && int.TryParse(parts[2], out var v) ? v : 0;
                }
            }

            return 0;
        }

        static List<Spot> Spots(SettlementPlan plan, string role) => plan.Spots.FindAll(s => s.Role == role);

        static Spot Pick(List<Spot> spots, ref Rng rng) => spots[rng.Range(0, spots.Count)];

        static string LookOf(Trade trade, ref Rng rng)
        {
            switch (trade)
            {
                case Trade.Trader: return rng.Pick(new[] { "merchant", "cook", "villager", "herbalist" });
                case Trade.Porter: return rng.Pick(new[] { "villager", "smith", "farmer" });
                case Trade.Farmer: return "farmer";
                case Trade.Student: return "student";
                case Trade.Guard:
                case Trade.Watch: return "guard";
                case Trade.Elder: return rng.Pick(new[] { "elder", "scribe", "monk" });
                case Trade.Child: return "child";
                default: return rng.Pick(TownLooks);
            }
        }

        static Spot FieldSpot(SettlementPlan plan, ref Rng rng)
        {
            if (plan.Fields.Count == 0)
            {
                return new Spot { At = plan.Centre + Vec2.FromYaw(rng.Range(0f, 360f)) * plan.Def.Radius * 0.9f, Role = "field" };
            }

            var field = plan.Fields[rng.Range(0, plan.Fields.Count)];

            return new Spot { At = field.Centre + new Vec2(rng.Range(-field.Half.X, field.Half.X), rng.Range(-field.Half.Z, field.Half.Z)) * 0.6f, Facing = rng.Range(0f, 360f), Role = "field" };
        }

        static Spot GateSpot(SettlementPlan plan, ref Rng rng)
        {
            // Inside one of the town's gates, to one side of the way.
            if (plan.Gates.Count > 0)
            {
                var (gate, outward) = plan.Gates[rng.Range(0, plan.Gates.Count)];
                var out_ = Vec2.FromYaw(outward);

                return new Spot { At = gate - out_ * 8 + out_.Right * 4 * (rng.Chance(0.5f) ? 1 : -1), Facing = outward, Role = "gate" };
            }

            // Where an avenue leaves the town.
            var avenues = plan.Streets.FindAll(s => s.Main);

            if (avenues.Count == 0)
            {
                return new Spot { At = plan.Centre, Role = "gate" };
            }

            var street = avenues[rng.Range(0, avenues.Count)];
            var end = Vec2.Distance(street.Points[0], plan.Centre) > Vec2.Distance(street.Points[street.Points.Count - 1], plan.Centre) ? 0 : street.Points.Count - 1;
            var p = street.Points[end];
            var inward = (plan.Centre - p).Normalized;

            return new Spot { At = p + inward.Right * (street.HalfWidth + 1) * (rng.Chance(0.5f) ? 1 : -1), Facing = (-inward).Yaw, Role = "gate" };
        }

        /// <summary>
        /// Adds a walk from one spot to another starting at `start`, then a stand at the second until `until`;
        /// returns the hour the stand ends. The walk takes the street graph (from the street nearest each spot).
        /// </summary>
        static float Walk(Walker walker, Graph graph, Spot from, Spot to, float start, float until, string doing)
        {
            var path = Vec2.Distance(from.At, to.At) < 12 ? null : graph.Path(graph.Nearest(from.At), graph.Nearest(to.At));
            var points = new List<Vec2> { from.At };

            if (path != null)
            {
                points.AddRange(path);
            }

            points.Add(to.At);
            var length = 0f;

            for (var i = 1; i < points.Count; i++)
            {
                length += Vec2.Distance(points[i - 1], points[i]);
            }

            var hours = length / walker.Pace / 3600f;
            var arrive = start + hours;

            if (arrive > until - 0.05f)
            {
                // Too far for the time: they walk faster (a town is never so big that this matters much).
                arrive = start + (until - start) * 0.8f;
            }

            walker.Day.Add(new Leg { From = start, To = arrive, Path = points.ToArray(), Length = length, Stand = to.At, Facing = to.Facing });
            walker.Day.Add(new Leg { From = arrive, To = until, Stand = to.At, Facing = to.Facing, Doing = doing });

            return until;
        }

        /// <summary>Where a walker is at an hour of the day (0–24).</summary>
        public static WalkerPose At(Walker walker, float hour)
        {
            var day = walker.Day;

            if (day.Count == 0)
            {
                return new WalkerPose { At = walker.Home, Indoors = true, Doing = "home" };
            }

            // The day runs from the first leg's start round to the same hour next day.
            var start = day[0].From;
            var h = hour;

            while (h < start)
            {
                h += 24;
            }

            while (h >= start + 24)
            {
                h -= 24;
            }

            foreach (var leg in day)
            {
                if (h < leg.From || h >= leg.To)
                {
                    continue;
                }

                if (leg.Path.Length >= 2)
                {
                    var share = (h - leg.From) / Math.Max(1e-5f, leg.To - leg.From);
                    var (at, heading) = Along(leg.Path, leg.Length * share);

                    return new WalkerPose { At = at, Facing = heading, Walking = true, Doing = "walk" };
                }

                return new WalkerPose { At = leg.Stand, Facing = leg.Facing, Indoors = leg.Doing == "home", Doing = leg.Doing };
            }

            return new WalkerPose { At = walker.Home, Indoors = true, Doing = "home" };
        }

        /// <summary>The index of the leg of a walker's day at an hour.</summary>
        public static int LegAt(Walker walker, float hour)
        {
            var day = walker.Day;

            if (day.Count == 0)
            {
                return -1;
            }

            var h = hour;

            while (h < day[0].From)
            {
                h += 24;
            }

            while (h >= day[0].From + 24)
            {
                h -= 24;
            }

            for (var i = 0; i < day.Count; i++)
            {
                if (h >= day[i].From && h < day[i].To)
                {
                    return i;
                }
            }

            return day.Count - 1;
        }

        /// <summary>A point `distance` metres along a path, and the way it heads there.</summary>
        public static (Vec2 at, float heading) Along(Vec2[] path, float distance)
        {
            for (var i = 1; i < path.Length; i++)
            {
                var step = Vec2.Distance(path[i - 1], path[i]);

                if (distance <= step || i == path.Length - 1)
                {
                    var t = step > 1e-4f ? Math.Min(1, distance / step) : 1;

                    return (path[i - 1] + (path[i] - path[i - 1]) * t, (path[i] - path[i - 1]).Yaw);
                }

                distance -= step;
            }

            return (path[path.Length - 1], 0);
        }
    }
}
