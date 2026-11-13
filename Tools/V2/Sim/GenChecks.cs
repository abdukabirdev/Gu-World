using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using GuWorld.Core;
using GuWorld.Core.Gen;
using GuWorld.Core.Gen.Flora;
using GuWorld.Core.Gen.People;

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

    /// <summary>The variants every kind is shown in: (seed, height share of its range, lean, crowding, age, wetness, decay).</summary>
    public static readonly (int seed, float height, float lean, float crowd, float age, float wet, float decay)[] Showcase =
    {
        (1, 0.55f, 0, 0.1f, 0.6f, 0, 0), (2, 1f, 0, 0.7f, 0.9f, 0.6f, 0.1f), (3, 0.2f, 10, 0.3f, 0.2f, -0.5f, 0), (4, 0.7f, 22, 0, 1f, 0.3f, 0.5f),
    };

    /// <summary>The parts of a level of detail as the .glb's: textures by path, the vertices' colours as they are (`raw`: moss weight and shade, for Blender's own bark material) or baked to tints (for the viewer).</summary>
    public static List<Glb.Part> TreeParts(TreeModel tree, int lod, string tex, bool raw)
    {
        var parts = new List<Glb.Part>();
        var species = tree.Species;

        foreach (var part in tree.Lods[lod].Parts)
        {
            var mesh = part.Mesh;
            var made = new Glb.Part { Name = $"LOD{lod}_{part.Material.Replace(':', '_')}", Mesh = mesh };

            if (part.Material.StartsWith("bark:"))
            {
                var name = part.Material.Substring(5);
                made.Texture = $"{tex}/{name}.jpg";
                made.Normal = $"{tex}/{name}_n.png";
                made.Roughness = 0.95f;

                if (!raw)
                {
                    // Moss as a green cast, the shade as darkness, the kind's tint.
                    made.Tints = new float[mesh.VertexCount * 3];

                    for (var i = 0; i < mesh.VertexCount; i++)
                    {
                        var moss = Math.Clamp(mesh.Colours[i * 4] * 1.4f - 0.2f, 0, 1);
                        var shade = mesh.Colours[i * 4 + 1];
                        made.Tints[i * 3] = (species.BarkTint.R * (1 - moss) + 0.42f * moss) * shade;
                        made.Tints[i * 3 + 1] = (species.BarkTint.G * (1 - moss) + 0.78f * moss) * shade;
                        made.Tints[i * 3 + 2] = (species.BarkTint.B * (1 - moss) + 0.3f * moss) * shade;
                    }
                }
            }
            else if (part.Material.StartsWith("leaf:"))
            {
                made.Texture = $"{tex}/{part.Material.Substring(5)}.png";
                made.Mask = true;
                made.TwoSided = true;
                made.Roughness = 0.7f;
            }
            else if (part.Material == "glow")
            {
                made.Emissive = new[] { 0.25f, 1f, 0.7f };
            }

            parts.Add(made);
        }

        return parts;
    }

    public static void Trees(string repo, Action<bool, string> check, List<string> report, bool export)
    {
        var dir = OutDir(repo);
        var glbDir = Path.Combine(dir, "trees");

        if (export)
        {
            Directory.CreateDirectory(glbDir);
        }

        var index = new StringBuilder("[");
        var watch = System.Diagnostics.Stopwatch.StartNew();
        var grown = 0;
        long triangles = 0;

        foreach (var species in Trees_All)
        {
            foreach (var (seed, share, lean, crowd, age, wet, decay) in Showcase)
            {
                var asked = new TreeParams { Height = species.MinHeight + (species.MaxHeight - species.MinHeight) * share, Lean = lean, Crowding = crowd, Age = age, Wet = wet, Decay = decay };
                var tree = TreeGen.Grow(species, seed, asked);
                grown++;
                triangles += tree.Lods[0].Triangles;
                Judge(tree, check, report, $"{species.Id} #{seed}");

                if (!export)
                {
                    continue;
                }

                var name = $"{species.Id}_{seed}";

                for (var lod = 0; lod < tree.Lods.Length; lod++)
                {
                    // The near model once with the raw weights (for Blender's bark material), and all levels baked for the viewer.
                    Glb.Write(Path.Combine(glbDir, $"{name}_lod{lod}.glb"), TreeParts(tree, lod, "../tex", false));
                }

                Glb.Write(Path.Combine(glbDir, $"{name}_raw.glb"), TreeParts(tree, 0, "../tex", true));
                index.Append(index.Length > 1 ? "," : "").Append(string.Format(Inv,
                    "{{\"file\":\"trees/{0}\",\"kind\":\"{1}\",\"name\":\"{2}\",\"seed\":{3},\"height\":{4:0.0},\"lean\":{5},\"crowding\":{6},\"age\":{7},\"wet\":{8},\"decay\":{9},\"tris\":[{10},{11},{12},{13}],\"crown\":{14:0.0},\"trunk\":{15:0.00},\"foot\":{16:0.0},\"cards\":{17},\"limbs\":{18},\"climb\":{19}}}",
                    name, species.Id, species.Name, seed, tree.Height, lean, crowd, age, wet, decay, tree.Lods[0].Triangles, tree.Lods[1].Triangles, tree.Lods[2].Triangles, tree.Lods[3].Triangles,
                    tree.CrownRadius, tree.TrunkRadius, tree.FootRadius, tree.Cards, tree.Limbs, tree.Climb.Count));
            }
        }

        var elapsed = watch.ElapsedMilliseconds;
        report.Add($"trees: {Trees_All.Length} kinds, {grown} grown in {elapsed} ms ({elapsed / Math.Max(1, grown)} ms each), {triangles / grown / 1000}k triangles on average near");

        if (export)
        {
            File.WriteAllText(Path.Combine(dir, "trees.json"), index.Append("]").ToString());
        }

        // The same asking grows the same tree.
        var a = TreeGen.Grow(Flora_Trees.Camphor, 77, new TreeParams { Height = 50, Lean = 5 });
        var b = TreeGen.Grow(Flora_Trees.Camphor, 77, new TreeParams { Height = 50, Lean = 5 });
        check(a.Lods[0].Triangles == b.Lods[0].Triangles && Same(a.Lods[0].Parts[0].Mesh, b.Lods[0].Parts[0].Mesh), "trees: the same kind, seed and asking grow the same tree");

        // The asking is heard: wetter is mossier, older is thicker, decay kills limbs, foliage thins.
        var dry = TreeGen.Grow(Flora_Trees.Camphor, 5, new TreeParams { Height = 50, Wet = -0.8f });
        var soaked = TreeGen.Grow(Flora_Trees.Camphor, 5, new TreeParams { Height = 50, Wet = 0.8f });
        check(Moss(soaked) > Moss(dry) * 1.5f + 0.01f, $"trees: a wet place grows moss ({Moss(dry):0.000} dry, {Moss(soaked):0.000} wet)");
        var young = TreeGen.Grow(Flora_Trees.Camphor, 5, new TreeParams { Height = 50, Age = 0 });
        var old = TreeGen.Grow(Flora_Trees.Camphor, 5, new TreeParams { Height = 50, Age = 1 });
        check(old.TrunkRadius > young.TrunkRadius * 1.2f, $"trees: an old tree is thicker ({young.TrunkRadius:0.00} m young, {old.TrunkRadius:0.00} m old)");
        var bare = TreeGen.Grow(Flora_Trees.Camphor, 5, new TreeParams { Height = 50, Foliage = -0.7f });
        check(bare.Cards < young.Cards * 0.6f, $"trees: foliage thins when asked ({young.Cards} to {bare.Cards} places)");
        var giant = TreeGen.Grow(GuWorld.Core.Gen.Flora.Trees.Ancient, 9, new TreeParams { Height = 105, Age = 1 });
        report.Add(string.Format(Inv, "trees: an ancient tree of {0:0} m — trunk {1:0.0} m thick above its foot, foot {2:0.0} m across, crown {3:0} m across, {4} limbs to climb, {5}k triangles near, {6} far, {7} at the horizon",
            giant.Height, giant.TrunkRadius * 2, giant.FootRadius * 2, giant.CrownRadius * 2, giant.Climb.Count, giant.Lods[0].Triangles / 1000, giant.Lods[2].Triangles, giant.Lods[3].Triangles));
        check(giant.Climb.Count > 20 && giant.Collision.TriangleCount > 500, "trees: a giant has limbs to climb and a solid shape");
    }

    static float Moss(TreeModel tree)
    {
        double sum = 0;
        var n = 0;

        foreach (var part in tree.Lods[0].Parts)
        {
            if (!part.Material.StartsWith("bark:"))
            {
                continue;
            }

            for (var i = 0; i < part.Mesh.VertexCount; i++)
            {
                sum += part.Mesh.Colours[i * 4];
                n++;
            }
        }

        return n == 0 ? 0 : (float)(sum / n);
    }

    static TreeSpecies[] Trees_All => Flora_Trees.All;

    static class Flora_Trees
    {
        public static TreeSpecies[] All => GuWorld.Core.Gen.Flora.Trees.All;
        public static TreeSpecies Camphor => GuWorld.Core.Gen.Flora.Trees.Camphor;
    }

    static bool Same(MeshData a, MeshData b)
    {
        if (a.Positions.Count != b.Positions.Count)
        {
            return false;
        }

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
        float maxY = float.MinValue, minY = float.MaxValue;
        var sound = true;

        foreach (var lod in tree.Lods)
        {
            foreach (var part in lod.Parts)
            {
                var mesh = part.Mesh;

                for (var i = 0; i < mesh.Normals.Count; i += 3)
                {
                    var length = Math.Sqrt(mesh.Normals[i] * mesh.Normals[i] + mesh.Normals[i + 1] * mesh.Normals[i + 1] + mesh.Normals[i + 2] * mesh.Normals[i + 2]);
                    sound &= Math.Abs(length - 1) < 0.02 && !double.IsNaN(length);
                }

                foreach (var p in mesh.Positions)
                {
                    sound &= !float.IsNaN(p) && !float.IsInfinity(p);
                }

                foreach (var t in mesh.Triangles)
                {
                    sound &= t >= 0 && t < mesh.VertexCount;
                }

                if (lod == tree.Lods[0])
                {
                    mesh.Bounds(out _, out var low, out _, out _, out var high, out _);
                    maxY = Math.Max(maxY, high);
                    minY = Math.Min(minY, low);
                }
            }
        }

        check(sound, $"{tag}: positions, normals and triangles sound");
        // A snapped tree is shorter than asked: that is its point.
        var snapped = maxY < tree.Height * 0.85f && tree.Species.Broken > 0;
        check(snapped || Math.Abs(maxY - tree.Height) < tree.Height * 0.2f + 1.5f, $"{tag}: as tall as asked ({maxY:0.0} m for {tree.Height:0.0})");
        check(minY > -Math.Max(3.5f, tree.FootRadius * 0.5f), $"{tag}: nothing far under the ground ({minY:0.0})");
        check(tree.Lods[0].Triangles < 450000, $"{tag}: near model within budget ({tree.Lods[0].Triangles})");
        check(tree.Lods[1].Triangles < tree.Lods[0].Triangles * 0.75f + 100, $"{tag}: middle model lighter ({tree.Lods[1].Triangles} < {tree.Lods[0].Triangles})");
        check(tree.Lods[2].Triangles < 30000, $"{tag}: far model within budget ({tree.Lods[2].Triangles})");
        check(tree.Lods[3].Triangles < 3500, $"{tag}: horizon model within budget ({tree.Lods[3].Triangles})");
        check(tree.Collision != null && tree.Collision.TriangleCount > 0 && tree.Climb.Count > 0, $"{tag}: has a solid shape and something to climb");

        if (tree.Asked.Lean >= 10 && tree.Species.MaxTrunks == 1)
        {
            check(tree.Top.X > 0, $"{tag}: leans the way asked (top at x {tree.Top.X:0.0})");
        }
    }

    /// <summary>A level of detail drawn by the software renderer, its bark and leaves from small copies of the textures when they are at hand.</summary>
    public static void DrawTree(Raster raster, TreeModel tree, int lod, float x, float y, float z, float scale, float yaw)
    {
        foreach (var part in tree.Lods[lod].Parts)
        {
            var colon = part.Material.IndexOf(':');
            raster.Draw(part.Mesh, x, y, z, scale, yaw, colon > 0 ? part.Material.Substring(colon + 1) : null, part.Material.StartsWith("bark:"));
        }
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
        Raster.LoadTextures(Path.Combine(dir, "tex128"));
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
            const float span = 280;
            var groundMesh = Patch(plan, at, span, 5);
            var h0 = land.Height(at);
            var eyeAt = new Vec2(at.X - span * 0.55f, at.Z - span * 0.55f);
            var top = h0;

            for (var t = 0; t <= 10; t++)
            {
                // The camera clears the land between it and the patch.
                var q = new Vec2(Lerp(eyeAt.X, at.X, t / 10f), Lerp(eyeAt.Z, at.Z, t / 10f));
                top = Math.Max(top, land.Height(q));
            }

            raster.Camera(new V3(eyeAt.X, Math.Max(h0 + span * 0.36f, top + 70), eyeAt.Z), new V3(at.X + span * 0.08f, h0 + 18, at.Z + span * 0.08f), 50);
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
                            var (yaw, scale) = library.Place(thing, model.Height);
                            var near = Math.Abs(thing.At.X - at.X) + Math.Abs(thing.At.Z - at.Z) < span * 0.5f;
                            DrawTree(raster, model, near ? 1 : 2, thing.At.X, y - 0.1f, thing.At.Z, scale, yaw);
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

    // ---------------------------------------------------------------------- People

    static readonly string[] Roles = { "villager", "farmer", "porter", "trader", "merchant", "guard", "student", "elder", "child", "cook", "herbalist", "smith", "scribe", "bandit", "gambler", "judge", "watch", "keeper" };

    /// <summary>
    /// A crowd made by the people generator: every trade, both sexes, every age, the clans; each
    /// checked (weights, bones, budget, height, sound numbers), drawn from the front and from the
    /// back, and some of them posed through their skin (an arm raised, an elbow and a knee bent) to
    /// see that the weights hold; written as .glb for the viewer.
    /// </summary>
    public static void PeopleChecks(string repo, Action<bool, string> check, List<string> report, bool export)
    {
        var dir = OutDir(repo);
        var glbDir = Path.Combine(dir, "people");

        if (export)
        {
            Directory.CreateDirectory(glbDir);
        }

        const int tw = 180, th = 300, columns = 12;
        var people = new List<PersonModel>();
        var watch = System.Diagnostics.Stopwatch.StartNew();
        var clans = new[] { "gu_yue", "gu_yue", "bai", "xiong" };

        for (var i = 0; i < 72; i++)
        {
            var role = Roles[i % Roles.Length];
            var spec = GuWorld.Core.Gen.People.PersonSpec.FromLook(role, 1000 + i * 37, clans[(i / Roles.Length) % clans.Length]);
            people.Add(GuWorld.Core.Gen.People.PersonGen.Make(spec));
        }

        var each = watch.ElapsedMilliseconds / (double)people.Count;
        var rows = people.Count / columns + 2;
        var sheetW = tw * columns;
        var sheet = new byte[sheetW * th * rows * 3];
        long pixels = 0, back = 0;
        var tris = 0;
        var index = new StringBuilder("[");
        var bones = GuWorld.Core.Gen.People.Skeleton.Bones.Length;

        for (var n = 0; n < people.Count; n++)
        {
            var person = people[n];
            var mesh = person.Mesh;
            var tag = $"person {n} ({person.Spec})";
            tris += mesh.TriangleCount;
            check(person.Joints.Length == mesh.VertexCount * 4 && person.Weights.Length == mesh.VertexCount * 4, $"{tag}: a skin for every vertex");
            var weightsOk = true;

            for (var v = 0; v < mesh.VertexCount; v++)
            {
                var sum = person.Weights[v * 4] + person.Weights[v * 4 + 1] + person.Weights[v * 4 + 2] + person.Weights[v * 4 + 3];
                weightsOk &= Math.Abs(sum - 1) < 0.01f;

                for (var j = 0; j < 4; j++)
                {
                    weightsOk &= person.Joints[v * 4 + j] >= 0 && person.Joints[v * 4 + j] < bones;
                }
            }

            check(weightsOk, $"{tag}: weights sum to one on real bones");
            check(mesh.TriangleCount < 16000, $"{tag}: within budget ({mesh.TriangleCount})");
            mesh.Bounds(out var minX, out var minY, out var minZ, out var maxX, out var maxY, out var maxZ);
            check(minY > -0.12f && maxY < person.Height + 0.35f && maxY > person.Height * 0.9f, $"{tag}: stands on the ground at its height ({minY:0.00}..{maxY:0.00} for {person.Height:0.00})");
            var sound = true;

            foreach (var x in mesh.Positions)
            {
                sound &= !float.IsNaN(x) && !float.IsInfinity(x);
            }

            check(sound, $"{tag}: sound positions");

            if (n < columns * (rows - 2))
            {
                var raster = new Raster(tw, th) { PaintBackFaces = false };
                raster.Frame(new V3(-0.5f, 0, -0.4f), new V3(0.5f, 1.9f, 0.4f), new V3(0.35f, 0.1f, 1), 28, 1.0f);
                raster.Ground(0, 0.8f, new V3(0, 0, 0));
                raster.Draw(mesh, 0, 0, 0, 1);
                pixels += raster.Pixels;
                back += raster.BackPixels;
                raster.Into(sheet, sheetW, (n % columns) * tw, (n / columns) * th);
                Raster.Label(sheet, sheetW, (n % columns) * tw + 4, (n / columns) * th + 4, person.Spec.Role, 1);
                Raster.Label(sheet, sheetW, (n % columns) * tw + 4, (n / columns) * th + 12, $"{person.Spec.Sex.ToString()[0]} {person.Spec.Age} {person.Spec.Clan}", 1);
            }

            if (export)
            {
                var skin = new Glb.Skin { Names = new string[bones], Parents = new int[bones], Heads = person.BoneAt, Joints = person.Joints, Weights = person.Weights };

                for (var b = 0; b < bones; b++)
                {
                    skin.Names[b] = GuWorld.Core.Gen.People.Skeleton.Bones[b].Name;
                    skin.Parents[b] = GuWorld.Core.Gen.People.Skeleton.Bones[b].Parent;
                }

                Glb.Write(Path.Combine(glbDir, $"person_{n}.glb"), new[] { new Glb.Part { Name = "Person", Mesh = mesh, Skin = skin } });
                index.Append(index.Length > 1 ? "," : "").Append(string.Format(Inv, "{{\"file\":\"people/person_{0}.glb\",\"role\":\"{1}\",\"sex\":\"{2}\",\"age\":\"{3}\",\"clan\":\"{4}\",\"wealth\":{5:0.00},\"seed\":{6},\"height\":{7:0.00},\"tris\":{8},\"about\":\"{9}\"}}",
                    n, person.Spec.Role, person.Spec.Sex, person.Spec.Age, person.Spec.Clan, person.Spec.Wealth, person.Spec.Seed, person.Height, mesh.TriangleCount, person.Description.Replace("\"", "'")));
            }
        }

        // The last two rows: the first twelve from behind, and posed through their skins.
        for (var n = 0; n < columns; n++)
        {
            var person = people[n];
            var raster = new Raster(tw, th) { PaintBackFaces = false };
            raster.Frame(new V3(-0.5f, 0, -0.4f), new V3(0.5f, 1.9f, 0.4f), new V3(-0.4f, 0.1f, -1), 28, 1.0f);
            raster.Ground(0, 0.8f, new V3(0, 0, 0));
            raster.Draw(person.Mesh, 0, 0, 0, 1);
            raster.Into(sheet, sheetW, n * tw, (rows - 2) * th);
            Raster.Label(sheet, sheetW, n * tw + 4, (rows - 2) * th + 4, "BACK", 1);

            var posed = Pose(person);
            var pr = new Raster(tw, th) { PaintBackFaces = false };
            pr.Frame(new V3(-0.7f, 0, -0.5f), new V3(0.7f, 1.9f, 0.5f), new V3(0.5f, 0.15f, 1), 30, 1.0f);
            pr.Ground(0, 0.9f, new V3(0, 0, 0));
            pr.Draw(posed, 0, 0, 0, 1);
            pr.Into(sheet, sheetW, n * tw, (rows - 1) * th);
            Raster.Label(sheet, sheetW, n * tw + 4, (rows - 1) * th + 4, "POSED", 1);
        }

        WorldChecks.WritePng(Path.Combine(dir, "people.png"), sheetW, th * rows, sheet);
        report.Add($"people: {people.Count} made, {each:0.0} ms each, {tris / people.Count} triangles on average, back-face pixels {100.0 * back / Math.Max(1, pixels):0.00}% → {Path.Combine(dir, "people.png")}");
        check(each < 60, "people: one is made in well under a tenth of a second");
        check(back < pixels * 0.04f, "people: hardly a pixel shows the inside of a garment (sleeves and hems are drawn two-sided in the game)");

        // Faces, close: the first 24 heads.
        const int fw = 150, fh = 170;
        var faces = new byte[fw * 12 * fh * 2 * 3];

        for (var n = 0; n < 24; n++)
        {
            var person = people[n];
            var head = person.BoneAt[GuWorld.Core.Gen.People.PersonGen.Bone("head")];
            var raster = new Raster(fw, fh) { PaintBackFaces = false };
            raster.Camera(head + new V3(0.18f, 0.13f, 0.55f), head + new V3(0, 0.08f, 0), 30);
            raster.Draw(person.Mesh, 0, 0, 0, 1);
            raster.Into(faces, fw * 12, (n % 12) * fw, (n / 12) * fh);
            Raster.Label(faces, fw * 12, (n % 12) * fw + 3, (n / 12) * fh + 3, person.Spec.Role, 1);
        }

        WorldChecks.WritePng(Path.Combine(dir, "faces.png"), fw * 12, fh * 2, faces);

        var again = GuWorld.Core.Gen.People.PersonGen.Make(people[5].Spec);
        check(again.Mesh.VertexCount == people[5].Mesh.VertexCount && Same(again.Mesh, people[5].Mesh), "people: the same spec makes the same person");

        // How different they are: the distinct outfits, hair and colours among the crowd.
        var looks = new HashSet<string>();

        foreach (var person in people)
        {
            looks.Add(person.Description.Substring(person.Description.IndexOf(';') + 1));
        }

        report.Add($"people: {looks.Count} different outfits and hairstyles among {people.Count}");
        check(looks.Count > people.Count * 0.7f, "people: hardly two dressed and coiffed alike");

        if (export)
        {
            File.WriteAllText(Path.Combine(dir, "people.json"), index.Append("]").ToString());
        }
    }

    /// <summary>
    /// The person posed through their skin, as the engine would: the left arm raised out to the
    /// side with the elbow bent, the right knee lifted and bent, the head turned — each bone turned
    /// about its own joint, its children carried with it, every vertex moved by its four bones.
    /// </summary>
    static MeshData Pose(PersonModel person)
    {
        var bones = GuWorld.Core.Gen.People.Skeleton.Bones;
        var turns = new Dictionary<string, (V3 axis, float degrees)>
        {
            ["upperArm_L"] = (new V3(0, 0, 1), -75),
            ["forearm_L"] = (new V3(1, 0, 0), -70),
            ["thigh_R"] = (new V3(1, 0, 0), -55),
            ["shin_R"] = (new V3(1, 0, 0), 75),
            ["head"] = (new V3(0, 1, 0), 30),
        };
        // Each bone's world turn and where its joint has gone.
        var rot = new (V3 axis, float angle)[bones.Length][];
        var origin = new V3[bones.Length];
        var at = person.BoneAt;
        var chain = new List<(V3 pivot, V3 axis, float angle)>[bones.Length];

        for (var i = 0; i < bones.Length; i++)
        {
            chain[i] = bones[i].Parent >= 0 ? new List<(V3, V3, float)>(chain[bones[i].Parent]) : new List<(V3, V3, float)>();

            if (turns.TryGetValue(bones[i].Name, out var turn))
            {
                chain[i].Add((at[i], turn.axis.Normalized, turn.degrees * (float)Math.PI / 180));
            }
        }

        V3 Move(int bone, V3 p)
        {
            // Innermost turn last: apply the bone's own first, then its parents' (each about its rest pivot).
            var list = chain[bone];

            for (var t = list.Count - 1; t >= 0; t--)
            {
                var (pivot, axis, angle) = list[t];
                p = pivot + Build.Rotate(p - pivot, axis, angle);
            }

            return p;
        }

        var mesh = person.Mesh;
        var posed = new MeshData();

        for (var v = 0; v < mesh.VertexCount; v++)
        {
            var p = new V3(mesh.Positions[v * 3], mesh.Positions[v * 3 + 1], mesh.Positions[v * 3 + 2]);
            var sum = new V3(0, 0, 0);

            for (var j = 0; j < 4; j++)
            {
                var w = person.Weights[v * 4 + j];

                if (w > 0)
                {
                    sum = sum + Move(person.Joints[v * 4 + j], p) * w;
                }
            }

            posed.Vertex(sum.X, sum.Y, sum.Z, 0, 0, mesh.Colours[v * 4], mesh.Colours[v * 4 + 1], mesh.Colours[v * 4 + 2]);
        }

        posed.Triangles.AddRange(mesh.Triangles);
        posed.ComputeNormals();

        return posed;
    }
}
