using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using GuWorld.Core;
using GuWorld.Core.Gen;
using GuWorld.Core.Gen.Flora;

/// <summary>
/// The generators, looked at without the engine (Sim `gen`): every kind of tree grown across
/// its range of heights, leans, crowding and age, held to its budgets and its asking (height,
/// lean, sound normals, no faces turned away); drawn into contact sheets in Logs/V2/gen/ and
/// written as .glb for the viewer page (Tools/V2/viewer).
/// </summary>
static class GenChecks
{
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static string OutDir(string repo)
    {
        var dir = Path.Combine(repo, "Logs", "V2", "gen");
        Directory.CreateDirectory(dir);

        return dir;
    }

    /// <summary>The variants every kind is shown in: (seed, height share of its range, lean, crowding, age).</summary>
    public static readonly (int seed, float height, float lean, float crowd, float age)[] Showcase =
    {
        (1, 0.5f, 0, 0, 0.4f), (2, 0.9f, 0, 0.7f, 0.6f), (3, 0.3f, 12, 0.2f, 0.2f), (4, 0.7f, 24, 0, 1f), (5, 1f, 0, 0.95f, 0.5f), (6, 0.15f, 6, 0.4f, 0f),
    };

    public static void Trees(string repo, Action<bool, string> check, List<string> report, bool export)
    {
        var dir = OutDir(repo);
        var glbDir = Path.Combine(dir, "trees");

        if (export)
        {
            Directory.CreateDirectory(glbDir);
        }

        const int tw = 240, th = 320;
        var columns = Showcase.Length + 3;
        var rows = Trees_All.Length;
        var sheetW = columns * tw;
        var sheetH = rows * th;
        var sheet = new byte[sheetW * sheetH * 3];
        var index = new StringBuilder("[");
        var watch = System.Diagnostics.Stopwatch.StartNew();
        var grown = 0;
        long backPixels = 0, pixels = 0;

        for (var row = 0; row < rows; row++)
        {
            var species = Trees_All[row];

            for (var col = 0; col < Showcase.Length; col++)
            {
                var (seed, share, lean, crowd, age) = Showcase[col];
                var asked = new TreeParams { Height = species.MinHeight + (species.MaxHeight - species.MinHeight) * share, Lean = lean, Crowding = crowd, Age = age };
                var tree = TreeGen.Grow(species, seed, asked);
                grown++;
                var tag = $"{species.Id} #{seed}";
                Judge(tree, check, report, tag);
                var raster = new Raster(tw, th);
                var size = Math.Max(species.MaxHeight, 8);
                raster.Frame(new V3(-size * 0.45f, 0, -size * 0.45f), new V3(size * 0.45f, size * 1.02f, size * 0.45f), new V3(0.35f, 0.18f, -1), 30, 1.0f);
                raster.Ground(0, size * 0.6f, new V3(0, 0, 0));
                raster.Draw(tree.Lods[0], 0, 0, 0, 1);
                pixels += raster.Pixels;
                backPixels += raster.BackPixels;
                raster.Into(sheet, sheetW, col * tw, row * th);
                Raster.Label(sheet, sheetW, col * tw + 6, row * th + 6, $"{species.Id} {seed}");
                Raster.Label(sheet, sheetW, col * tw + 6, row * th + 20, string.Format(Inv, "{0:0.#}M L{1:0} C{2:0.0}", tree.Height, lean, crowd));
                Raster.Label(sheet, sheetW, col * tw + 6, row * th + th - 16, $"{tree.Lods[0].TriangleCount / 1000.0:0.0}K TRIS", 2);

                if (export)
                {
                    var name = $"{species.Id}_{seed}";
                    var parts = new List<Glb.Part>();

                    for (var lod = 0; lod < tree.Lods.Length; lod++)
                    {
                        parts.Add(new Glb.Part { Name = "LOD" + lod, Mesh = tree.Lods[lod] });
                    }

                    Glb.Write(Path.Combine(glbDir, name + ".glb"), parts);
                    index.Append(index.Length > 1 ? "," : "").Append(string.Format(Inv,
                        "{{\"file\":\"trees/{0}.glb\",\"kind\":\"{1}\",\"name\":\"{2}\",\"seed\":{3},\"height\":{4:0.0},\"lean\":{5},\"crowding\":{6},\"age\":{7},\"tris\":[{8},{9},{10},{12}],\"crown\":{11:0.0}}}",
                        name, species.Id, species.Name, seed, tree.Height, lean, crowd, age, tree.Lods[0].TriangleCount, tree.Lods[1].TriangleCount, tree.Lods[2].TriangleCount, tree.CrownRadius, tree.Lods[3].TriangleCount));
                }
            }

            // The last tree again at its three levels of detail, and seen from above.
            var last = TreeGen.Grow(species, 4, new TreeParams { Height = (species.MinHeight + species.MaxHeight) / 2, Lean = 10, Crowding = 0.3f, Age = 0.6f });

            for (var lod = 1; lod < 4; lod++)
            {
                var raster = new Raster(tw, th);
                var size = Math.Max(species.MaxHeight, 8);
                var view = lod == 3 ? new V3(0.2f, 1.4f, -1) : new V3(0.35f, 0.18f, -1);
                raster.Frame(new V3(-size * 0.45f, 0, -size * 0.45f), new V3(size * 0.45f, size * 1.02f, size * 0.45f), view, 30, 1.0f);
                raster.Ground(0, size * 0.6f, new V3(0, 0, 0));
                raster.Draw(last.Lods[lod], 0, 0, 0, 1);
                raster.Into(sheet, sheetW, (Showcase.Length + lod - 1) * tw, row * th);
                Raster.Label(sheet, sheetW, (Showcase.Length + lod - 1) * tw + 6, row * th + 6, $"LOD{lod} {last.Lods[lod].TriangleCount} TRIS");
            }
        }

        var elapsed = watch.ElapsedMilliseconds;
        WorldChecks.WritePng(Path.Combine(dir, "trees.png"), sheetW, sheetH, sheet);
        report.Add($"trees: {Trees_All.Length} kinds, {grown} trees grown in {elapsed} ms ({elapsed / Math.Max(1, grown)} ms each with the sheet), back-face pixels {100.0 * backPixels / Math.Max(1, pixels):0.00}% → {Path.Combine(dir, "trees.png")}");
        check(backPixels < pixels * 0.03, "trees: hardly a pixel is drawn by a face turned away");

        if (export)
        {
            File.WriteAllText(Path.Combine(dir, "trees.json"), index.Append("]").ToString());
        }

        // Determinism and speed: the same asking grows the same tree; a variant takes milliseconds.
        var a = TreeGen.Grow(Flora_Trees.Camphor, 77, new TreeParams { Height = 20, Lean = 5 });
        var b = TreeGen.Grow(Flora_Trees.Camphor, 77, new TreeParams { Height = 20, Lean = 5 });
        check(a.Lods[0].VertexCount == b.Lods[0].VertexCount && Same(a.Lods[0], b.Lods[0]), "trees: the same kind, seed and asking grow the same tree");
        watch.Restart();

        for (var i = 0; i < 40; i++)
        {
            TreeGen.Grow(Trees_All[i % Trees_All.Length], 1000 + i, new TreeParams { Crowding = 0.4f, Age = 0.5f });
        }

        var each = watch.ElapsedMilliseconds / 40.0;
        report.Add($"trees: {each:0.0} ms to grow one (all three levels)");
        check(each < 120, "trees: one grows in well under a tenth of a second");
    }

    static TreeSpecies[] Trees_All => Flora_Trees.All;

    static class Flora_Trees
    {
        public static TreeSpecies[] All => GuWorld.Core.Gen.Flora.Trees.All;
        public static TreeSpecies Camphor => GuWorld.Core.Gen.Flora.Trees.Camphor;
    }

    static bool Same(MeshData a, MeshData b)
    {
        for (var i = 0; i < a.Positions.Count; i += 97)
        {
            if (a.Positions[i] != b.Positions[i])
            {
                return false;
            }
        }

        return true;
    }

    static void Judge(TreeModel tree, Action<bool, string> check, List<string> report, string tag)
    {
        var lod0 = tree.Lods[0];
        lod0.Bounds(out var minX, out var minY, out var minZ, out var maxX, out var maxY, out var maxZ);
        var tall = maxY;
        // A snapped tree is shorter than asked: that is its point.
        var snapped = tall < tree.Height * 0.85f && tree.Species.Broken > 0;
        check(snapped || Math.Abs(tall - tree.Height) < tree.Height * 0.22f + 1.2f, $"{tag}: as tall as asked ({tall:0.0} m for {tree.Height:0.0})");
        check(minY > -Math.Max(1.6f, tree.TrunkRadius * 2.2f), $"{tag}: nothing far under the ground ({minY:0.0})");
        check(lod0.TriangleCount < 48000, $"{tag}: near model within budget ({lod0.TriangleCount})");
        check(tree.Lods[1].TriangleCount < lod0.TriangleCount * 0.75f + 50, $"{tag}: middle model lighter ({tree.Lods[1].TriangleCount} < {lod0.TriangleCount})");
        check(tree.Lods[2].TriangleCount < 4000, $"{tag}: far model within budget ({tree.Lods[2].TriangleCount})");
        check(tree.Lods[3].TriangleCount < 500, $"{tag}: farthest model within budget ({tree.Lods[3].TriangleCount})");

        if (tree.Asked.Lean >= 10 && tree.Species.MaxTrunks == 1)
        {
            check(tree.Top.X > 0, $"{tag}: leans the way asked (top at x {tree.Top.X:0.0})");
        }

        var sound = true;

        foreach (var lod in tree.Lods)
        {
            for (var i = 0; i < lod.Normals.Count; i += 3)
            {
                var length = Math.Sqrt(lod.Normals[i] * lod.Normals[i] + lod.Normals[i + 1] * lod.Normals[i + 1] + lod.Normals[i + 2] * lod.Normals[i + 2]);
                sound &= Math.Abs(length - 1) < 0.02 && !double.IsNaN(length);
            }

            foreach (var p in lod.Positions)
            {
                sound &= !float.IsNaN(p) && !float.IsInfinity(p);
            }
        }

        check(sound, $"{tag}: positions and normals sound");
    }

    // ---------------------------------------------------------------------- Biomes

    static readonly Dictionary<Biome, int> BiomeColours = new Dictionary<Biome, int>
    {
        [Biome.Meadow] = 0x8AA65A, [Biome.PineForest] = 0x2E5A30, [Biome.Fir] = 0x163A2A, [Biome.Broadleaf] = 0x4E7A34, [Biome.Maple] = 0xB0482A,
        [Biome.Bamboo] = 0x7AB04A, [Biome.Deadwood] = 0x7A6A5A, [Biome.Rock] = 0x8A8884, [Biome.Marsh] = 0x5A6A4A, [Biome.Riverbank] = 0x9AAA7A,
        [Biome.Settled] = 0xB09A70, [Biome.Field] = 0xC0B060,
    };

    /// <summary>
    /// The biomes over the whole map (and the grandeur field: where the giants grow), their
    /// trees counted on a sample of cells, and a patch of each drawn in 3D with its land, trees and
    /// undergrowth (Logs/V2/gen/biomes.png, biome-map.png).
    /// </summary>
    public static void BiomeChecks(string repo, Action<bool, string> check, List<string> report)
    {
        var dir = OutDir(repo);
        var watch = System.Diagnostics.Stopwatch.StartNew();
        var plan = WorldPlan.Get();
        report.Add($"biomes: world plan in {watch.ElapsedMilliseconds} ms");
        var land = plan.Land;
        var surface = plan.Surface;
        var half = Regions.Half;

        // The map: biome colours, with the grandeur field shading the woods (lighter = taller).
        const int size = 640;
        var map = new byte[size * size * 2 * 3];
        var found = new Dictionary<Biome, List<Vec2>>();
        var cells = new Dictionary<Biome, int>();
        var woods = plan.Scatter.Woods;

        for (var j = 0; j < size; j++)
        {
            for (var i = 0; i < size; i++)
            {
                var p = new Vec2(-half + 2 * half * (i + 0.5f) / size, half - 2 * half * (j + 0.5f) / size);
                var h = land.Height(p);
                var slope = land.Slope(p.X, p.Z, 4);
                var biome = surface.BiomeAt(p, h, slope, land.Moisture(p));
                cells[biome] = cells.TryGetValue(biome, out var n) ? n + 1 : 1;

                if (!found.TryGetValue(biome, out var list))
                {
                    found[biome] = list = new List<Vec2>();
                }

                if (list.Count < 400 && (i * 7 + j * 3) % 5 == 0)
                {
                    list.Add(p);
                }

                var c = BiomeColours.TryGetValue(biome, out var bc) ? bc : 0xFF00FF;
                var shade = 0.75f + 0.25f * Math.Min(1.5f, h / 500f);
                Put(map, size * 2, i, j, c, shade);
                // The right half: grandeur (dark small, bright giants), only where trees grow.
                var g = woods.Grandeur(p) * Woods.TreeLine(h);
                var k = Math.Clamp((g - 0.6f) / 1.0f, 0, 1);
                var grey = Biomes.Of(biome) != null && Biomes.Of(biome).Density > 0.5f ? (int)(40 + 210 * k) : 24;
                Put(map, size * 2, size + i, j, (grey << 16) | (grey << 8) | (int)(grey * 0.7f), 1);
            }
        }

        WorldChecks.WritePng(Path.Combine(dir, "biome-map.png"), size * 2, size, map);
        var legend = new StringBuilder("biomes: share of the map —");

        foreach (var (biome, n) in cells)
        {
            legend.Append($" {biome} {100.0 * n / (size * size):0.0}%");
        }

        report.Add(legend.ToString());

        foreach (var biome in new[] { Biome.PineForest, Biome.Fir, Biome.Broadleaf, Biome.Maple, Biome.Bamboo, Biome.Deadwood, Biome.Marsh, Biome.Riverbank, Biome.Meadow })
        {
            check(found.ContainsKey(biome), $"biomes: the map has {biome}");
        }

        // Every biome's trees, counted on cells round its sample points.
        var library = new TreeLibrary(2);
        var pictures = new List<(Biome biome, Vec2 at)>();

        foreach (var biome in new[] { Biome.PineForest, Biome.Fir, Biome.Broadleaf, Biome.Maple, Biome.Bamboo, Biome.Deadwood, Biome.Marsh, Biome.Riverbank, Biome.Meadow, Biome.Rock })
        {
            if (!found.TryGetValue(biome, out var points) || points.Count == 0)
            {
                continue;
            }

            // A point deep in the biome: the sample with most of its neighbours the same.
            Vec2 best = points[0];
            var bestScore = -1;

            for (var t = 0; t < points.Count; t += Math.Max(1, points.Count / 60))
            {
                var p = points[t];
                var score = 0;

                for (var a = 0; a < 8; a++)
                {
                    var q = p + new Vec2((float)Math.Cos(a * 0.785) * 60, (float)Math.Sin(a * 0.785) * 60);
                    score += surface.BiomeAt(q, land.Height(q), land.Slope(q.X, q.Z, 4), land.Moisture(q)) == biome ? 1 : 0;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    best = p;
                }
            }

            pictures.Add((biome, best));
            var kinds = new Dictionary<string, int>();
            float tall = 0, lean = 0;
            var count = 0;
            var cx = (int)Math.Floor(best.X / Scatter.CellSize);
            var cz = (int)Math.Floor(best.Z / Scatter.CellSize);

            for (var dz = -1; dz <= 1; dz++)
            {
                for (var dx = -1; dx <= 1; dx++)
                {
                    foreach (var tree in plan.Scatter.Cell(cx + dx, cz + dz))
                    {
                        if (tree.Kind != Strewn.Tree)
                        {
                            continue;
                        }

                        var id = Flora_Trees.All[tree.Species].Id;
                        kinds[id] = kinds.TryGetValue(id, out var k) ? k + 1 : 1;
                        tall += tree.Height;
                        lean += tree.Lean;
                        count++;
                    }
                }
            }

            var mix = new StringBuilder();

            foreach (var (id, k) in kinds)
            {
                mix.Append($" {id} {k}");
            }

            report.Add(string.Format(Inv, "  {0,-11} {1,4} trees on 3×3 cells, mean height {2:0.0} m, mean lean {3:0.0}°, at ({4:0}, {5:0}):{6}",
                biome, count, count > 0 ? tall / count : 0, count > 0 ? lean / count : 0, best.X, best.Z, mix));
        }

        // A patch of each, in 3D: the land, its trees (the library's variants, turned and scaled as the game does) and undergrowth.
        const int tw = 480, th = 300;
        var columns = 2;
        var rows = (pictures.Count + 1) / 2;
        var sheetW = tw * columns;
        var sheet = new byte[sheetW * th * rows * 3];
        var shapes = new Dictionary<int, MeshData>();

        for (var n = 0; n < pictures.Count; n++)
        {
            var (biome, at) = pictures[n];
            var raster = new Raster(tw, th) { FogDistance = 900 };
            const float span = 150;
            var groundMesh = Patch(plan, at, span, 3);
            var h0 = land.Height(at);
            var eyeAt = new Vec2(at.X - span * 0.55f, at.Z - span * 0.55f);
            var top = h0;

            for (var t = 0; t <= 10; t++)
            {
                // The camera clears the land between it and the patch.
                var q = new Vec2(Lerp(eyeAt.X, at.X, t / 10f), Lerp(eyeAt.Z, at.Z, t / 10f));
                top = Math.Max(top, land.Height(q));
            }

            raster.Camera(new V3(eyeAt.X, Math.Max(h0 + span * 0.32f, top + 25), eyeAt.Z), new V3(at.X + span * 0.08f, h0 + 2, at.Z + span * 0.08f), 50);
            raster.Draw(groundMesh, 0, 0, 0, 1);
            var drawn = 0;
            var cx0 = (int)Math.Floor((at.X - span / 2) / Scatter.CellSize);
            var cx1 = (int)Math.Floor((at.X + span / 2) / Scatter.CellSize);
            var cz0 = (int)Math.Floor((at.Z - span / 2) / Scatter.CellSize);
            var cz1 = (int)Math.Floor((at.Z + span / 2) / Scatter.CellSize);

            for (var cz = cz0; cz <= cz1; cz++)
            {
                for (var cx = cx0; cx <= cx1; cx++)
                {
                    foreach (var thing in plan.Scatter.Cell(cx, cz))
                    {
                        if (Math.Abs(thing.At.X - at.X) > span / 2 || Math.Abs(thing.At.Z - at.Z) > span / 2)
                        {
                            continue;
                        }

                        var y = land.Height(thing.At);

                        if (thing.Kind == Strewn.Tree)
                        {
                            var variant = library.Choose(thing);
                            var model = library.Get(thing.Species, variant);
                            var (yaw, scale) = library.Place(thing, model);
                            var near = Math.Abs(thing.At.X - at.X) + Math.Abs(thing.At.Z - at.Z) < span * 0.5f;
                            raster.Draw(model.Lods[near ? 0 : 1], thing.At.X, y - 0.15f, thing.At.Z, scale, yaw);
                            drawn++;
                        }
                        else if (thing.Kind == Strewn.Boulder || thing.Kind == Strewn.Shrub || thing.Kind == Strewn.Reeds)
                        {
                            var key = (int)thing.Kind * 100 + thing.Seed % 6;

                            if (!shapes.TryGetValue(key, out var mesh))
                            {
                                var seed = (int)thing.Kind * 1000 + thing.Seed % 6;
                                shapes[key] = mesh = thing.Kind == Strewn.Boulder ? Shapes.Boulder(seed, 2) : thing.Kind == Strewn.Shrub ? Shapes.Shrub(seed) : Shapes.Reeds(seed);
                            }

                            raster.Draw(mesh, thing.At.X, y - 0.05f, thing.At.Z, thing.Scale, thing.Yaw);
                        }
                    }
                }
            }

            raster.Into(sheet, sheetW, (n % columns) * tw, (n / columns) * th);
            Raster.Label(sheet, sheetW, (n % columns) * tw + 8, (n / columns) * th + 8, $"{biome} {drawn} TREES", 2);
            Raster.Label(sheet, sheetW, (n % columns) * tw + 8, (n / columns) * th + 22, string.Format(Inv, "AT {0:0} {1:0} H{2:0}M G{3:0.00}", at.X, at.Z, h0, woods.Grandeur(at)), 2);
        }

        WorldChecks.WritePng(Path.Combine(dir, "biomes.png"), sheetW, th * rows, sheet);
        report.Add($"biomes: {pictures.Count} patches drawn in {watch.ElapsedMilliseconds} ms → {Path.Combine(dir, "biomes.png")}");

        // Grandeur does what it says: the same kind stands taller in a grove of giants than in a stunted stand.
        var rng = new Rng(5);
        var pine = GuWorld.Core.Gen.Flora.Trees.Pine;
        var profile = Biomes.Of(Biome.PineForest);
        float giants = 0, stunted = 0;
        Vec2 high = new Vec2(0, 0), low = new Vec2(0, 0);
        float gHigh = 0, gLow = 9;

        for (var t = 0; t < 4000; t++)
        {
            var p = new Vec2(-half + 2 * half * ((t * 0.6180339f) % 1), -half + 2 * half * ((t * 0.7548777f) % 1));
            var g = woods.Grandeur(p);

            if (g > gHigh)
            {
                gHigh = g;
                high = p;
            }

            if (g < gLow)
            {
                gLow = g;
                low = p;
            }
        }

        for (var t = 0; t < 50; t++)
        {
            giants += woods.Ask(profile, pine, high, 150, 0.1f, new Vec2(0, 0), ref rng).height;
            stunted += woods.Ask(profile, pine, low, 150, 0.1f, new Vec2(0, 0), ref rng).height;
        }

        report.Add(string.Format(Inv, "biomes: pines in the grandest grove {0:0.0} m on average (grandeur {1:0.00}), in the most stunted stand {2:0.0} m ({3:0.00})", giants / 50, gHigh, stunted / 50, gLow));
        check(giants > stunted * 1.3f, "biomes: trees grow markedly taller in groves of giants");
        var windLow = woods.Ask(profile, pine, high, 100, 0, new Vec2(0, 0), ref rng).lean;
        float leanHigh = 0, leanLow = 0;

        for (var t = 0; t < 50; t++)
        {
            leanHigh += woods.Ask(Biomes.Of(Biome.Rock), pine, high, 600, 0.6f, new Vec2(1, 0), ref rng).lean;
            leanLow += woods.Ask(Biomes.Of(Biome.Broadleaf), pine, high, 60, 0.05f, new Vec2(1, 0), ref rng).lean;
        }

        report.Add(string.Format(Inv, "biomes: trees lean {0:0.0}° on average on high rock, {1:0.0}° in a low broad wood", leanHigh / 50, leanLow / 50));
        check(leanHigh > leanLow * 2, "biomes: the wind bends trees far more up high and in the open");
    }

    /// <summary>The land round a point as a mesh, coloured by its ground layers and lit.</summary>
    static MeshData Patch(WorldPlan plan, Vec2 at, float span, float step)
    {
        var mesh = new MeshData();
        var n = (int)(span / step);
        var weights = new float[Surface.Layers];

        for (var j = 0; j <= n; j++)
        {
            for (var i = 0; i <= n; i++)
            {
                var x = at.X - span / 2 + i * step;
                var z = at.Z - span / 2 + j * step;
                var p = new Vec2(x, z);
                var h = plan.Land.Height(p);
                var slope = plan.Land.Slope(x, z, 2);
                plan.Surface.Paint(p, h, slope, weights);
                float r = 0, g = 0, b = 0;

                for (var k = 0; k < Surface.Layers; k++)
                {
                    var c = WorldChecks.LayerColours[k];
                    r += ((c >> 16) & 255) / 255f * weights[k];
                    g += ((c >> 8) & 255) / 255f * weights[k];
                    b += (c & 255) / 255f * weights[k];
                }

                var wet = plan.Waters != null && plan.Waters.Wet(p, out var level);

                if (wet)
                {
                    r = 0.25f;
                    g = 0.38f;
                    b = 0.45f;
                }

                mesh.Vertex(x, h, z, 0, 0, r, g, b);
            }
        }

        for (var j = 0; j < n; j++)
        {
            for (var i = 0; i < n; i++)
            {
                var a = j * (n + 1) + i;
                mesh.Quad(a, a + n + 1, a + n + 2, a + 1);
            }
        }

        mesh.ComputeNormals();

        return mesh;
    }

    static float Lerp(float a, float b, float t) => a + (b - a) * t;

    static void Put(byte[] rgb, int width, int x, int y, int colour, float shade)
    {
        var o = (y * width + x) * 3;
        rgb[o] = (byte)Math.Min(255, ((colour >> 16) & 255) * shade);
        rgb[o + 1] = (byte)Math.Min(255, ((colour >> 8) & 255) * shade);
        rgb[o + 2] = (byte)Math.Min(255, (colour & 255) * shade);
    }
}
