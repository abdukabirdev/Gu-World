using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using GuWorld.Core.Gen;
using GuWorld.Core.Gen.Buildings;

/// <summary>
/// The building generator's checks: every style raised with several seeds, wealths, wears and
/// clans; each judged (sound numbers, levels of detail shrinking, sizes a person fits in, a door);
/// with `export`, written as .glb for the viewer and Blender (Logs/V2/gen/buildings, textures from
/// ../btex).
/// </summary>
static class BuildingChecks
{
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    /// <summary>The showcase: seed, wealth, wear, clan.</summary>
    public static readonly (int seed, float wealth, float wear, string clan)[] Showcase =
    {
        (1, 0.5f, 0.3f, "gu_yue"),
        (2, 0.9f, 0.1f, "gu_yue"),
        (3, 0.2f, 0.8f, ""),
        (4, 0.6f, 0.4f, "xiong"),
        (5, 0.7f, 0.2f, "bai"),
    };

    public static List<Glb.Part> Parts(BuildingModel model, int lod, string tex) => Parts(model.Lods[lod], tex, $"LOD{lod}");

    public static List<Glb.Part> Parts(GuWorld.Core.Gen.Flora.TreeLod level, string tex, string prefix = "LOD0")
    {
        var parts = new List<Glb.Part>();

        foreach (var part in level.Parts)
        {
            if (part.Mesh.TriangleCount == 0)
            {
                continue;
            }

            var made = new Glb.Part { Name = $"{prefix}_{part.Material.Replace(':', '_')}", Mesh = part.Mesh };

            if (part.Material.StartsWith("tex:"))
            {
                var name = part.Material.Substring(4);
                made.Texture = $"{tex}/{name}.jpg";
                made.Normal = $"{tex}/{name}_n.png";
                made.Roughness = name == "tile" ? 0.6f : 0.88f;
            }
            else if (part.Material == "glow")
            {
                made.Emissive = new[] { 1f, 0.62f, 0.3f };
                made.TwoSided = true;
            }
            else if (part.Material == "ground")
            {
                made.Roughness = 1;
            }
            else
            {
                made.Roughness = 0.35f;
            }

            parts.Add(made);
        }

        return parts;
    }

    public static void Run(string repo, Action<bool, string> check, List<string> report, bool export)
    {
        var dir = GenChecks.OutDir(repo);
        var glbDir = Path.Combine(dir, "buildings");

        if (export)
        {
            Directory.CreateDirectory(glbDir);
        }

        var index = new StringBuilder("[");
        var watch = System.Diagnostics.Stopwatch.StartNew();
        var made = 0;
        long triangles = 0;

        foreach (var style in Buildings.All)
        {
            foreach (var (seed, wealth, wear, clan) in Showcase)
            {
                var model = BuildingGen.Make(style, seed, new BuildingParams { Wealth = wealth, Wear = wear, Clan = clan });
                made++;
                triangles += model.Lods[0].Triangles;
                Judge(model, check, $"{style.Id} #{seed}");

                if (!export)
                {
                    continue;
                }

                var name = $"{style.Id}_{seed}";

                for (var lod = 0; lod < 4; lod++)
                {
                    Glb.Write(Path.Combine(glbDir, $"{name}_lod{lod}.glb"), Parts(model, lod, "../btex"));
                }

                index.Append(index.Length > 1 ? "," : "").Append(string.Format(Inv,
                    "{{\"file\":\"buildings/{0}\",\"kind\":\"{1}\",\"name\":\"{2}\",\"seed\":{3},\"wealth\":{4},\"wear\":{5},\"clan\":\"{6}\",\"width\":{7:0.0},\"depth\":{8:0.0},\"height\":{9:0.0},\"storeys\":{10},\"bays\":\"{11}×{12}\",\"tris\":[{13},{14},{15},{16}],\"doors\":{17}}}",
                    name, style.Id, style.Name, seed, wealth, wear, clan, model.EaveWidth, model.EaveDepth, model.Height, model.Storeys, model.BaysX, model.BaysZ,
                    model.Lods[0].Triangles, model.Lods[1].Triangles, model.Lods[2].Triangles, model.Lods[3].Triangles, model.Doors.Count));
            }
        }

        var elapsed = watch.ElapsedMilliseconds;
        report.Add($"buildings: {Buildings.All.Length} styles, {made} raised in {elapsed} ms ({elapsed / Math.Max(1, made)} ms each), {triangles / made / 1000}k triangles on average near");

        if (export)
        {
            File.WriteAllText(Path.Combine(dir, "buildings.json"), index.Append("]").ToString());
        }

        // The same asking raises the same building.
        var a = BuildingGen.Make(Buildings.Hall, 7, new BuildingParams { Wealth = 0.8f, Clan = "gu_yue" });
        var b = BuildingGen.Make(Buildings.Hall, 7, new BuildingParams { Wealth = 0.8f, Clan = "gu_yue" });
        check(a.Lods[0].Triangles == b.Lods[0].Triangles && a.Height == b.Height, "buildings: the same seed raises the same building");

        // A lot asked for is filled: wider lots, more bays.
        var narrow = BuildingGen.Make(Buildings.House, 3, new BuildingParams { Width = 9 });
        var wide = BuildingGen.Make(Buildings.House, 3, new BuildingParams { Width = 20 });
        check(wide.BaysX > narrow.BaysX && Math.Abs(wide.EaveWidth - 20) < 5, $"buildings: a house fits its lot (9 m → {narrow.BaysX} bays, 20 m → {wide.BaysX} bays, eaves {wide.EaveWidth:0.0} m)");

        var great = BuildingGen.Make(Buildings.GreatHall, 1, new BuildingParams { Wealth = 1, Clan = "gu_yue" });
        report.Add(string.Format(Inv, "buildings: a great hall {0:0} × {1:0} m under its eaves, {2:0.0} m high, {3}k triangles near, {4}k far, {5} at the horizon",
            great.EaveWidth, great.EaveDepth, great.Height, great.Lods[0].Triangles / 1000, great.Lods[2].Triangles / 1000, great.Lods[3].Triangles));
    }

    static void Judge(BuildingModel model, Action<bool, string> check, string label)
    {
        var sound = true;
        var lod0 = model.Lods[0];

        foreach (var part in lod0.Parts)
        {
            foreach (var v in part.Mesh.Positions)
            {
                if (float.IsNaN(v) || float.IsInfinity(v) || Math.Abs(v) > 200)
                {
                    sound = false;
                }
            }

            foreach (var v in part.Mesh.Normals)
            {
                if (float.IsNaN(v))
                {
                    sound = false;
                }
            }
        }

        var shrinking = model.Lods[0].Triangles > model.Lods[1].Triangles && model.Lods[1].Triangles > model.Lods[2].Triangles && model.Lods[2].Triangles > model.Lods[3].Triangles;
        var room = model.Height > 2.6f && model.Width > 2;
        var door = model.Doors.Count > 0 || model.Style.Open;

        if (!sound || !shrinking || !room || !door)
        {
            check(false, $"buildings: {label} — sound {sound}, levels shrinking {shrinking} ({model.Lods[0].Triangles}/{model.Lods[1].Triangles}/{model.Lods[2].Triangles}/{model.Lods[3].Triangles}), room {room}, door {door}");
        }
    }
}
