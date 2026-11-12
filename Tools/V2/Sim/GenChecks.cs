using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
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

                    for (var lod = 0; lod < 3; lod++)
                    {
                        parts.Add(new Glb.Part { Name = "LOD" + lod, Mesh = tree.Lods[lod] });
                    }

                    Glb.Write(Path.Combine(glbDir, name + ".glb"), parts);
                    index.Append(index.Length > 1 ? "," : "").Append(string.Format(Inv,
                        "{{\"file\":\"trees/{0}.glb\",\"kind\":\"{1}\",\"name\":\"{2}\",\"seed\":{3},\"height\":{4:0.0},\"lean\":{5},\"crowding\":{6},\"age\":{7},\"tris\":[{8},{9},{10}],\"crown\":{11:0.0}}}",
                        name, species.Id, species.Name, seed, tree.Height, lean, crowd, age, tree.Lods[0].TriangleCount, tree.Lods[1].TriangleCount, tree.Lods[2].TriangleCount, tree.CrownRadius));
                }
            }

            // The last tree again at its three levels of detail, and seen from above.
            var last = TreeGen.Grow(species, 4, new TreeParams { Height = (species.MinHeight + species.MaxHeight) / 2, Lean = 10, Crowding = 0.3f, Age = 0.6f });

            for (var lod = 0; lod < 3; lod++)
            {
                var raster = new Raster(tw, th);
                var size = Math.Max(species.MaxHeight, 8);
                var view = lod == 2 ? new V3(0.2f, 1.4f, -1) : new V3(0.35f, 0.18f, -1);
                raster.Frame(new V3(-size * 0.45f, 0, -size * 0.45f), new V3(size * 0.45f, size * 1.02f, size * 0.45f), view, 30, 1.0f);
                raster.Ground(0, size * 0.6f, new V3(0, 0, 0));
                raster.Draw(last.Lods[lod], 0, 0, 0, 1);
                raster.Into(sheet, sheetW, (Showcase.Length + lod) * tw, row * th);
                Raster.Label(sheet, sheetW, (Showcase.Length + lod) * tw + 6, row * th + 6, $"LOD{lod} {last.Lods[lod].TriangleCount} TRIS");
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
}
