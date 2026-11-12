using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using GuWorld.Core;
using GuWorld.Core.Gen;

/// <summary>
/// Checks of the generated world, and pictures of it: the map coloured by height, biome, water and
/// roads with the settlements' buildings on it (Logs/V2/world.png), and each settlement's plan close
/// up — the only way to look at the world without the engine.
/// </summary>
static class WorldChecks
{
    public static void Run(string repo, Action<bool, string> check, List<string> report)
    {
        var plan = WorldPlan.Get();
        report.Add("\nWORLD " + plan.Report());
        var land = plan.Land;
        var outDir = Path.Combine(repo, "Logs", "V2");
        Directory.CreateDirectory(outDir);

        GrassChecks(plan, check, report);

        // The valley's places keep their levels.
        foreach (var place in Valley.Places)
        {
            if (!float.IsNaN(place.Level) && !place.Cave)
            {
                var h = land.Height(place.At.X, place.At.Z);
                check(Math.Abs(h - place.Level) < 1.5f, $"world: {place.Id} stands at its level {place.Level} ({h:0.0})");
            }
        }

        // The rivers: never uphill, wet where they run, the valley's fall where the plan has it.
        foreach (var river in plan.Waters.Rivers)
        {
            for (var i = 1; i < river.Levels.Count; i++)
            {
                if (river.Levels[i] > river.Levels[i - 1] + 0.01f)
                {
                    check(false, $"world: river {river.Def.Id} runs uphill at {river.Points[i]}");
                    break;
                }
            }

            var dry = 0;

            for (var i = 0; i < river.Points.Count; i += 5)
            {
                var p = river.Points[i];
                dry += land.Height(p) > river.Levels[i] - 0.2f ? 1 : 0;
            }

            check(dry <= river.Points.Count / 5 / 20, $"world: river {river.Def.Id} has water over its bed ({dry} dry samples)");
        }

        check(plan.Waters.Falls.Any(f => f.River == "qing" && Vec2.Distance(f.Lip, new Vec2(276, 150)) < 40), "world: the Qing falls from the valley's plateau");
        check(plan.Waters.Falls.Any(f => f.River == "bai" && f.Top - f.Bottom > 30), "world: the back mountain's waterfall falls beside the Bai");
        var ford = new Vec2(244, 66);
        check(plan.Waters.Wet(ford, out var fordLevel) && fordLevel - land.Height(ford) < 1.1f, "world: the ford can be waded");

        // The roads: every one found, none steeper than a cart can climb, all of them joined.
        check(plan.Roads.All.Count(r => r.Planned) == Regions.Roads.Length, $"world: every road was found ({plan.Roads.All.Count(r => r.Planned)} of {Regions.Roads.Length})");
        var steepest = 0f;
        var where = default(Vec2);

        foreach (var road in plan.Roads.All.Where(r => r.Planned))
        {
            for (var i = 1; i < road.Points.Count; i++)
            {
                var a = road.Points[i - 1];
                var b = road.Points[i];
                var grade = Math.Abs(land.Height(b) - land.Height(a)) / Math.Max(0.5f, Vec2.Distance(a, b));

                var onBridge = plan.Roads.Bridges.Any(bridge => Vec2.Distance(bridge.At, b) < bridge.Length * 0.5f + 3);

                if (grade > steepest && !onBridge && !plan.Waters.Wet(b, out _) && !plan.Waters.Wet(a, out _))
                {
                    steepest = grade;
                    where = b;
                }
            }
        }

        report.Add($"  steepest road: {Math.Atan(steepest) * 180 / Math.PI:0}° at {where}");
        check(steepest < 0.45f, $"world: no road steeper than 24° ({Math.Atan(steepest) * 180 / Math.PI:0}° at {where})");

        // Settlements: their buildings do not overlap or stand in the street, every one reachable from the plaza.
        foreach (var settlement in plan.Settlements)
        {
            var houses = settlement.Buildings.Count(b => b.Role == "house");
            var solid = settlement.Buildings.Where(b => b.Role != "wall" && b.Role != "fence" && b.Role != "gate" && b.Role != "tower" && b.Role != "lamp").ToList();
            var overlaps = 0;

            for (var i = 0; i < solid.Count; i++)
            {
                for (var j = i + 1; j < solid.Count; j++)
                {
                    overlaps += Box.Overlap(solid[i].Footprint, solid[j].Footprint) ? 1 : 0;
                }
            }

            check(overlaps == 0, $"world: {settlement.Def.Id} has no buildings on one another ({overlaps})");
            check(houses >= settlement.Def.Houses * 0.6f, $"world: {settlement.Def.Id} has its houses ({houses} of {settlement.Def.Houses})");

            foreach (var must in settlement.Def.Must)
            {
                check(settlement.Buildings.Any(b => b.Model == must), $"world: {settlement.Def.Id} has its {must}");
            }

            if (settlement.Def.City)
            {
                // A town: each house stands on its own pad, whatever the slope round it.
                var worst = 0f;

                foreach (var (box, level) in settlement.Pads)
                {
                    foreach (var p in box.Corners().Append(box.Centre))
                    {
                        worst = Math.Max(worst, Math.Abs(land.Height(p) - level));
                    }
                }

                check(worst < 0.6f, $"world: {settlement.Def.Id}'s houses stand on level pads (off by {worst:0.0} m)");
                check(settlement.Population >= 3000 && settlement.Population <= 6500, $"world: {settlement.Def.Id} houses 3–6 thousand people ({settlement.Population})");
                check(settlement.Buildings.Select(b => b.Model).Distinct().Count() >= 30, $"world: {settlement.Def.Id} is built of many models ({settlement.Buildings.Select(b => b.Model).Distinct().Count()})");
                report.Add($"  town {settlement.Def.Id}: {settlement.Buildings.Count} buildings ({houses} houses), {settlement.Population} people, {settlement.Streets.Count} streets, {settlement.Fields.Count} fields, " +
                           string.Join(", ", settlement.Buildings.Where(b => b.Footprint.Radius > 15 && !b.Model.StartsWith("code:")).Select(b => b.Model).Distinct()));
                var made = settlement.Buildings.Where(b => b.Model.StartsWith("code:")).GroupBy(b => b.Model.Split(':')[1]).OrderBy(g => g.Key).Select(g => $"{g.Key} {g.Count()}");
                report.Add($"  town {settlement.Def.Id}, made in code: " + string.Join(", ", made));
                check(settlement.Stalls >= 20, $"world: {settlement.Def.Id} has a market ({settlement.Stalls} stalls)");
                check(settlement.WallPieces >= 150 && settlement.Gates.Count >= 2, $"world: {settlement.Def.Id} is walled ({settlement.WallPieces} pieces, {settlement.Gates.Count} gates)");
                report.Add($"  town {settlement.Def.Id}, its wall: {settlement.WallPieces} pieces, {settlement.Gates.Count} gates");
                CrowdChecks(settlement, check, report);
                check(settlement.Buildings.Count(b => b.Model.StartsWith("code:paifang")) >= 2, $"world: {settlement.Def.Id} has archways over its avenues");
                continue;
            }

            var h0 = land.Height(settlement.Centre);
            var flatness = solid.Max(b => Math.Abs(land.Height(b.Footprint.Centre) - h0));
            check(flatness < 2.5f, $"world: {settlement.Def.Id} is level under its buildings (off by {flatness:0.0} m)");
            report.Add($"  {settlement.Def.Name,-26} {houses,2} houses, {settlement.Buildings.Count,3} pieces, {settlement.Streets.Count} streets, gates {settlement.Gates.Count}, level {settlement.Level:0}");
        }

        // Scatter: deterministic, and never on a road or in the water.
        var a1 = plan.Scatter.Cell(3, 4);
        var a2 = plan.Scatter.Cell(3, 4);
        check(a1.Count == a2.Count && a1.Select(i => i.At).SequenceEqual(a2.Select(i => i.At)), "world: a cell is strewn the same every time");
        var total = 0;
        var trees = 0;
        var bad = 0;

        for (var cz = -60; cz < 60; cz += 6)
        {
            for (var cx = -60; cx < 60; cx += 6)
            {
                foreach (var instance in plan.Scatter.Cell(cx, cz))
                {
                    total++;
                    trees += instance.IsTree ? 1 : 0;

                    if (plan.Roads.Nearest(instance.At, out var toRoad, out _, out var half, out _) && toRoad < half || plan.Waters.Wet(instance.At, out _))
                    {
                        bad++;
                    }
                }
            }
        }

        report.Add($"  strewn in a 1/36 sample of the map: {total} things ({trees} trees) → about {total * 36 / 1000}k on the whole map");
        check(bad == 0, $"world: nothing strewn on a road or in the water ({bad})");
        check(trees > 300, "world: the mountain has woods");

        // The shapes made in code face outwards (their normals point away from their middle) and are the same for the same seed.
        foreach (var (name, shape) in new[] { ("boulder", Shapes.Boulder(7, 2)), ("shrub", Shapes.Shrub(3)), ("sphere", Shapes.Icosphere(2)) })
        {
            float sx = 0, sy = 0, sz = 0;

            for (var i = 0; i < shape.VertexCount; i++)
            {
                sx += shape.Positions[i * 3];
                sy += shape.Positions[i * 3 + 1];
                sz += shape.Positions[i * 3 + 2];
            }

            sx /= shape.VertexCount;
            sy /= shape.VertexCount;
            sz /= shape.VertexCount;
            var outward = 0;

            for (var i = 0; i < shape.VertexCount; i++)
            {
                var dot = (shape.Positions[i * 3] - sx) * shape.Normals[i * 3] + (shape.Positions[i * 3 + 1] - sy) * shape.Normals[i * 3 + 1] + (shape.Positions[i * 3 + 2] - sz) * shape.Normals[i * 3 + 2];
                outward += dot > 0 ? 1 : 0;
            }

            check(outward > shape.VertexCount * 0.85f, $"world: the {name}'s faces point outwards ({outward} of {shape.VertexCount})");
        }

        check(Shapes.Boulder(5, 2).Positions.SequenceEqual(Shapes.Boulder(5, 2).Positions), "world: a boulder is the same for the same seed");
        var deck = Shapes.Bridge(12, 5, 4, 1);
        check(deck.TriangleCount > 100, "world: a bridge has planks, rails and piers");
        var farLand = Shapes.FarLand(plan, 32, 4);
        check(farLand.VertexCount == 33 * 33 && Enumerable.Range(0, farLand.VertexCount).All(i => farLand.Normals[i * 3 + 1] > 0), "world: the far land faces up");

        Atlas(plan, check, report);
        Picture(plan, Path.Combine(outDir, "world.png"), 1024, -Regions.Half, -Regions.Half, Regions.Half * 2, true);
        Picture(plan, Path.Combine(outDir, "valley.png"), 768, -520, -520, 1040, true);

        Picture(plan, Path.Combine(outDir, "gu_yue_town.png"), 1024, -520, -560, 1040, false);

        foreach (var settlement in plan.Settlements.Where(s => s.Def.Radius >= 90 && !s.Def.City))
        {
            var r = settlement.Def.Radius + 40;
            Picture(plan, Path.Combine(outDir, settlement.Def.Id + ".png"), 640, settlement.Centre.X - r, settlement.Centre.Z - r, r * 2, false);
        }

        report.Add($"  pictures in {outDir}");
    }

    /// <summary>The wider world's people and things are placed where they can be reached: on dry, walkable ground, out of the buildings.</summary>
    static void Atlas(WorldPlan plan, Action<bool, string> check, List<string> report)
    {
        check(GuWorld.Core.Atlas.Big, "world: the atlas is resolved once the world is laid out");
        var land = plan.Land;

        bool InBuilding(SettlementPlan settlement, Vec2 p) => settlement.Buildings.Any(b => b.Role != "lamp" && b.Footprint.Contains(p));

        foreach (var npc in RegionPeople.All.Where(n => n.Home != ""))
        {
            var settlement = plan.Settlement(npc.Home);
            check(Vec2.Distance(npc.At, settlement.Centre) <= settlement.Def.Radius + 12, $"world: {npc.Id} lives in {npc.Home}");
            check(!InBuilding(settlement, npc.At), $"world: {npc.Id} does not stand inside a building ({string.Join(",", settlement.Buildings.Where(b => b.Footprint.Contains(npc.At)).Select(b => b.Model + "/" + b.Role))})");
        }

        var stacked = RegionPeople.All.Where(n => n.Home != "").GroupBy(n => (Math.Round(n.At.X), Math.Round(n.At.Z))).Where(g => g.Count() > 1).ToList();
        check(stacked.Count == 0, $"world: no two people stand on one spot ({string.Join(", ", stacked.Select(g => string.Join("+", g.Select(n => n.Id))))})");

        foreach (var thing in RegionThings.All)
        {
            var at = (Vec2)thing.At;
            // Herbs may grow in the shallows (reeds), knee-deep at most; nothing else stands in water.
            check(!plan.Waters.Wet(at, out var level) || level - land.Height(at) < (thing.Kind == ThingKind.Herb ? 1f : 0.4f), $"world: {thing.Id} is not under water");

            if (thing.Home != "")
            {
                var settlement = plan.Settlement(thing.Home);
                check(!InBuilding(settlement, at), $"world: {thing.Id} does not stand inside a building");
            }
            else if (thing.Kind != ThingKind.Herb)
            {
                check(land.Slope(at.X, at.Z) < 0.75f, $"world: {thing.Id} stands on ground that can be walked ({land.Slope(at.X, at.Z):0.00})");
            }
        }

        foreach (var spawn in RegionSpawns.All)
        {
            var at = (Vec2)spawn.At;
            check(Math.Abs(at.X) < Regions.Half - 60 && Math.Abs(at.Z) < Regions.Half - 60, $"world: {spawn.Id} is inside the walls");
            check(!plan.Waters.Wet(at, out var level) || level - land.Height(at) < 0.6f, $"world: {spawn.Id} is not in deep water");
        }

        // A day's schedule moves people between real spots.
        var moved = 0;

        foreach (var npc in RegionPeople.All.Where(n => n.Day.Length > 0))
        {
            var morning = GuWorld.Core.Atlas.WhereAt(npc, 7, out _);
            var night = GuWorld.Core.Atlas.WhereAt(npc, 23, out _);
            moved += morning.DistanceTo(night) > 3 ? 1 : 0;
        }

        check(moved >= 8, $"world: people go about their day ({moved} move between morning and night)");
        report.Add($"  atlas: {RegionPeople.All.Length} people, {RegionThings.All.Length} things, {RegionSpawns.All.Length} spawns of the regions placed; {moved} with a day of their own");
    }

    static readonly int[] LayerColours = { 0x5A7A3C, 0x2F4A2A, 0x8A8A86, 0xA08A5C, 0x6E6A66, 0x9A9488, 0x4A6A3A, 0x7A5E40, 0x4A3E30, 0x8A7050 };

    /// <summary>A shaded relief map of a square of the world: ground layers lit from the north-west, water, roads, buildings, settlements' names.</summary>
    static void Picture(WorldPlan plan, string path, int size, float x0, float z0, float span, bool roads)
    {
        var land = plan.Land;
        var rgb = new byte[size * size * 3];
        var weights = new float[Surface.Layers];
        var step = span / size;

        for (var j = 0; j < size; j++)
        {
            var z = z0 + span * (1 - (j + 0.5f) / size);

            for (var i = 0; i < size; i++)
            {
                var x = x0 + span * (i + 0.5f) / size;
                var p = new Vec2(x, z);
                var h = land.Height(p);
                var dx = land.Height(x + step, z) - h;
                var dz = land.Height(x, z + step) - h;
                var slope = (float)Math.Sqrt(dx * dx + dz * dz) / step;
                plan.Surface.Paint(p, h, slope, weights);
                float r = 0, g = 0, b = 0;

                for (var k = 0; k < Surface.Layers; k++)
                {
                    r += ((LayerColours[k] >> 16) & 255) * weights[k];
                    g += ((LayerColours[k] >> 8) & 255) * weights[k];
                    b += (LayerColours[k] & 255) * weights[k];
                }

                // Light from the north-west, and a little brighter the higher.
                var light = Noise.Clamp(1 + (-dx + dz) / step * 0.9f, 0.45f, 1.5f) * (0.82f + Math.Min(0.35f, h / 1500));

                if (plan.Waters.Wet(p, out var level) && level > h)
                {
                    var deep = Noise.Clamp01((level - h) / 3);
                    r = Noise.Lerp(70, 30, deep);
                    g = Noise.Lerp(110, 70, deep);
                    b = Noise.Lerp(140, 120, deep);
                    light = 1;
                }

                var o = (j * size + i) * 3;
                rgb[o] = (byte)Noise.Clamp(r * light, 0, 255);
                rgb[o + 1] = (byte)Noise.Clamp(g * light, 0, 255);
                rgb[o + 2] = (byte)Noise.Clamp(b * light, 0, 255);
            }
        }

        void Dot(float x, float z, int colour, int radius)
        {
            var i = (int)((x - x0) / span * size);
            var j = (int)((1 - (z - z0) / span) * size);

            for (var dj = -radius; dj <= radius; dj++)
            {
                for (var di = -radius; di <= radius; di++)
                {
                    var u = i + di;
                    var v = j + dj;

                    if (u >= 0 && v >= 0 && u < size && v < size && di * di + dj * dj <= radius * radius)
                    {
                        var o = (v * size + u) * 3;
                        rgb[o] = (byte)(colour >> 16);
                        rgb[o + 1] = (byte)(colour >> 8);
                        rgb[o + 2] = (byte)colour;
                    }
                }
            }
        }

        // Buildings as their footprints, filled.
        foreach (var settlement in plan.Settlements)
        {
            foreach (var building in settlement.Buildings)
            {
                var box = building.Footprint;
                var colour = building.Role == "house" ? 0xD8C8A8 : building.Role == "wall" || building.Role == "fence" ? 0x4A3A2A : building.Role == "prop" || building.Role == "lamp" ? 0xB08060 : 0xF0D070;

                for (var u = -box.Half.X; u <= box.Half.X; u += step * 0.7f)
                {
                    for (var v = -box.Half.Z; v <= box.Half.Z; v += step * 0.7f)
                    {
                        var w = box.World(new Vec2(u, v));
                        Dot(w.X, w.Z, colour, 0);
                    }
                }
            }
        }

        foreach (var building in Valley.Buildings)
        {
            if (Catalog.TryGet(building.Model, out var footprint) && !building.Model.StartsWith("tree"))
            {
                var origin = (Vec2)building.At;
                var box = new Box(origin + Vec2.FromYaw(building.Facing + 90) * footprint.MiddleX + Vec2.FromYaw(building.Facing) * footprint.MiddleZ,
                    new Vec2(footprint.Across * 0.5f, footprint.Deep * 0.5f), building.Facing);

                for (var u = -box.Half.X; u <= box.Half.X; u += step * 0.7f)
                {
                    for (var v = -box.Half.Z; v <= box.Half.Z; v += step * 0.7f)
                    {
                        var w = box.World(new Vec2(u, v));
                        Dot(w.X, w.Z, 0xC8B090, 0);
                    }
                }
            }
        }

        // Trees, sparsely, as dark dots (a picture of a small area only: the whole map would take long).
        if (span <= 1200)
        {
            var c0 = (int)Math.Floor(x0 / Scatter.CellSize);
            var c1 = (int)Math.Floor((x0 + span) / Scatter.CellSize);
            var r0 = (int)Math.Floor(z0 / Scatter.CellSize);
            var r1 = (int)Math.Floor((z0 + span) / Scatter.CellSize);

            for (var cz = r0; cz <= r1; cz++)
            {
                for (var cx = c0; cx <= c1; cx++)
                {
                    foreach (var instance in plan.Scatter.Cell(cx, cz))
                    {
                        var colour = instance.Kind == Strewn.Boulder ? 0x8A8680 : instance.Kind == Strewn.Bamboo ? 0x6AA040 : instance.Kind == Strewn.Reeds ? 0x9AA060
                            : instance.Kind == Strewn.Shrub ? 0x3A5A2A : instance.Kind == Strewn.Deadwood ? 0x5A4A3A : 0x1A3A1A;
                        Dot(instance.At.X, instance.At.Z, colour, instance.IsTree ? Math.Max(1, (int)(instance.Scale * 3 / step)) : 0);
                    }
                }
            }
        }

        if (roads)
        {
            foreach (var bridge in plan.Roads.Bridges)
            {
                Dot(bridge.At.X, bridge.At.Z, 0xFFFFFF, 2);
            }

            foreach (var fall in plan.Waters.Falls)
            {
                Dot(fall.Lip.X, fall.Lip.Z, 0xA0E0FF, 4);
            }

            foreach (var place in Valley.Places.Concat(Regions.Places))
            {
                Dot(place.At.X, place.At.Z, place.Colour, 3);
            }
        }

        WritePng(path, size, size, rgb);
    }

    /// <summary>The ground's small growth: thick on meadows, none on roads and in the deep, ferns under trees, quick enough to plant as the hero walks.</summary>
    static void GrassChecks(WorldPlan plan, Action<bool, string> check, List<string> report)
    {
        var tufts = new List<Tuft>();
        var watch = System.Diagnostics.Stopwatch.StartNew();
        var cells = 0;
        var byKind = new int[Grass.Kinds];
        var onRoad = 0;
        var inDeep = 0;

        // A walk of cells across the mountain's middle: the town, meadows, woods, the river.
        for (var x = -1200f; x < 1200f; x += 300)
        {
            for (var z = -1200f; z < 1200f; z += 300)
            {
                tufts.Clear();
                Grass.Plant(plan, x, z, 24, 5, tufts);
                cells++;

                foreach (var t in tufts)
                {
                    byKind[t.Kind]++;
                    var p = new Vec2(t.X, t.Z);

                    if (plan.Roads.Nearest(p, out var toRoad, out _, out var half, out _) && toRoad < half)
                    {
                        onRoad++;
                    }

                    if (plan.Waters.Wet(p, out var level) && level - t.Y > 0.7f)
                    {
                        inDeep++;
                    }
                }
            }
        }

        var each = watch.Elapsed.TotalMilliseconds / cells;
        var total = byKind.Sum();
        check(total > cells * 300, $"grass: the ground is not bare ({total} tufts in {cells} cells)");
        check(onRoad == 0 && inDeep == 0, $"grass: none on the roads or in deep water ({onRoad}, {inDeep})");
        check(byKind[Grass.Fern] > 0 && byKind[Grass.FlowersWhite] + byKind[Grass.FlowersRed] + byKind[Grass.FlowersYellow] > 0, "grass: ferns in the woods and flowers in the meadows");
        check(each < 60, $"grass: a cell is planted quickly ({each:0.0} ms)");

        for (var kind = 0; kind < Grass.Kinds; kind++)
        {
            var mesh = Grass.Mesh(kind);
            check(mesh.TriangleCount > 0 && mesh.TriangleCount < 400, $"grass: kind {kind} has a light mesh ({mesh.TriangleCount} triangles)");
        }

        report.Add($"grass: {total} tufts in {cells} cells of 24 m ({each:0.0} ms a cell): " + string.Join(", ", byKind.Select((n, k) => $"{k}:{n}")));
    }

    /// <summary>The town's walkers: enough of them, their ways along the streets, out by day and in by night.</summary>
    static void CrowdChecks(SettlementPlan town, Action<bool, string> check, List<string> report)
    {
        var walkers = town.Walkers;
        check(walkers.Count >= 300, $"world: {town.Def.Id} has a crowd ({walkers.Count} walkers)");

        if (walkers.Count == 0)
        {
            return;
        }

        int Out(float hour) => walkers.Count(w => !Crowd.At(w, hour).Indoors);
        int Walking(float hour) => walkers.Count(w => Crowd.At(w, hour).Walking);
        var noon = Out(10);
        var midnight = Out(2);
        check(noon > walkers.Count * 0.8f, $"world: by day the town is out ({noon} of {walkers.Count} at ten)");
        check(midnight < walkers.Count * 0.15f, $"world: by night the town is in ({midnight} of {walkers.Count} at two)");
        check(new[] { 7f, 9f, 12f, 13f, 18f, 21f }.All(h => Walking(h) > walkers.Count * 0.02f), "world: at every hour of the day someone is on the way");

        // Every walk keeps to a sane pace and every leg follows the one before.
        var badPace = 0;
        var gaps = 0;
        var throughStreets = 0;
        var walks = 0;

        foreach (var walker in walkers)
        {
            for (var i = 0; i < walker.Day.Count; i++)
            {
                var leg = walker.Day[i];

                if (i > 0 && Math.Abs(walker.Day[i - 1].To - leg.From) > 1e-3f)
                {
                    gaps++;
                }

                if (leg.Path.Length >= 2)
                {
                    walks++;
                    throughStreets += leg.Path.Length > 2 || leg.Length < 12 ? 1 : 0;
                    var speed = leg.Length / Math.Max(1e-4f, (leg.To - leg.From) * 3600);
                    badPace += speed > 3.2f ? 1 : 0;
                }
            }
        }

        check(gaps == 0, $"world: the walkers' days have no gaps ({gaps})");
        check(badPace < walks * 0.03f, $"world: the walkers walk, not fly ({badPace} of {walks} walks too fast)");
        check(throughStreets > walks * 0.9f, $"world: the walks go along the streets ({throughStreets} of {walks})");

        // They talk: a name and a title each, a line for every trade, a stall's goods for its trader.
        var traders = walkers.Count(w => w.Stall >= 0);
        check(traders >= town.Stalls * 0.8f, $"world: the stalls have traders ({traders} of {town.Stalls})");
        check(walkers.All(w => Chatter.Who(w).name.Length > 2 && Chatter.Who(w).name == Chatter.Who(w).name), "world: every townsperson has a name");
        check(walkers.Take(80).All(w => Chatter.Say(w, 10, 2, Standing.Neutral, false, 1).Length >= 1), "world: every townsperson has something to say");
        check(walkers.Where(w => w.Stall >= 0).All(w => Markets.Get(Chatter.ShelfOf(w.Stall)).Length > 0), "world: every stall has goods");
        report.Add($"  town {town.Def.Id}, a passer-by: {Chatter.Who(walkers[3]).name} ({Chatter.Who(walkers[3]).title}): «{Chatter.Say(walkers[3], 10, 4, Standing.Friendly, false, 2)[0].Text}»");

        var parts = new Crowd.Graph(town).Components();
        report.Add($"  town {town.Def.Id}, its street graph: {parts.Sum()} nodes in {parts.Count} parts (the biggest {parts[0]}, then {string.Join(", ", parts.Skip(1).Take(6))})");

        // The town at three hours, by trade.
        var at = new[] { 7f, 12f, 15f, 19f, 23f }.Select(h => $"{h:0}h {Out(h)} out/{Walking(h)} walking");
        report.Add($"  town {town.Def.Id}, its crowd: {walkers.Count} walkers ({string.Join(", ", walkers.GroupBy(w => w.Trade).Select(g => $"{g.Key} {g.Count()}"))}); " + string.Join(", ", at));
    }

    /// <summary>A plain RGB PNG (zlib from the framework, CRCs by hand).</summary>
    public static void WritePng(string path, int width, int height, byte[] rgb)
    {
        using (var file = File.Create(path))
        {
            file.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, 0, 8);
            var header = new byte[13];
            BigEndian(header, 0, width);
            BigEndian(header, 4, height);
            header[8] = 8;
            header[9] = 2;
            Chunk(file, "IHDR", header);
            var raw = new byte[height * (width * 3 + 1)];

            for (var y = 0; y < height; y++)
            {
                raw[y * (width * 3 + 1)] = 0;
                Buffer.BlockCopy(rgb, y * width * 3, raw, y * (width * 3 + 1) + 1, width * 3);
            }

            using (var packed = new MemoryStream())
            {
                using (var zlib = new ZLibStream(packed, CompressionLevel.Optimal, true))
                {
                    zlib.Write(raw, 0, raw.Length);
                }

                Chunk(file, "IDAT", packed.ToArray());
            }

            Chunk(file, "IEND", new byte[0]);
        }
    }

    static void BigEndian(byte[] buffer, int at, int value)
    {
        buffer[at] = (byte)(value >> 24);
        buffer[at + 1] = (byte)(value >> 16);
        buffer[at + 2] = (byte)(value >> 8);
        buffer[at + 3] = (byte)value;
    }

    static readonly uint[] Crcs = Enumerable.Range(0, 256).Select(n =>
    {
        var c = (uint)n;

        for (var k = 0; k < 8; k++)
        {
            c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
        }

        return c;
    }).ToArray();

    static void Chunk(Stream stream, string type, byte[] data)
    {
        var length = new byte[4];
        BigEndian(length, 0, data.Length);
        stream.Write(length, 0, 4);
        var typed = System.Text.Encoding.ASCII.GetBytes(type);
        stream.Write(typed, 0, 4);
        stream.Write(data, 0, data.Length);
        var crc = 0xFFFFFFFFu;

        foreach (var b in typed.Concat(data))
        {
            crc = Crcs[(crc ^ b) & 0xFF] ^ (crc >> 8);
        }

        crc ^= 0xFFFFFFFFu;
        var tail = new byte[4];
        BigEndian(tail, 0, (int)crc);
        stream.Write(tail, 0, 4);
    }
}
