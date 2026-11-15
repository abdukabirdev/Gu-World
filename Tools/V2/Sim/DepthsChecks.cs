using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using GuWorld.Core.Gen;
using GuWorld.Core.Gen.Buildings;

/// <summary>
/// The clan's underground (Core/Gen/Buildings/DepthsGen): sound meshes, every place of it there,
/// a floor to stand on under each, the blood pond some fifty metres down, the same for the same seed;
/// with `export`, written as .glb with its lights and places beside it (Logs/V2/gen/depths).
/// </summary>
static class DepthsChecks
{
    public static void Run(string repo, Action<bool, string> check, List<string> report, bool export)
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        var model = DepthsGen.Make("gu_yue", 7);
        var elapsed = watch.ElapsedMilliseconds;
        var sound = true;

        foreach (var lod in model.Lods)
        {
            foreach (var part in lod.Parts)
            {
                foreach (var v in part.Mesh.Positions)
                {
                    sound &= !float.IsNaN(v) && !float.IsInfinity(v) && Math.Abs(v) < 400;
                }

                foreach (var v in part.Mesh.Normals)
                {
                    sound &= !float.IsNaN(v);
                }
            }
        }

        check(sound, "depths: the underground's meshes are sound");
        check(model.Lods[0].Triangles > model.Lods[1].Triangles, $"depths: the near level is finer ({model.Lods[0].Triangles} / {model.Lods[1].Triangles} triangles)");
        check(model.Deepest >= 45, $"depths: the blood pond lies some fifty metres down ({model.Deepest:0} m)");

        foreach (var role in new[] { "entry", "guard", "spring", "gu_1", "gu_2", "gu_3", "forbidden", "secret_hall", "coffin" })
        {
            check(model.Spots.Exists(s => s.Role == role), $"depths: has its {role}");
        }

        // A floor under every place: a ray down from a little above it meets the solid within two metres.
        var solid = model.Collision;

        foreach (var spot in model.Spots)
        {
            var hit = Down(solid, spot.At + new V3(0, 1.2f, 0), 3.4f);
            check(hit >= 0 && hit <= 2.6f, $"depths: the floor holds at {spot.Name} ({(hit < 0 ? "nothing" : hit.ToString("0.0", CultureInfo.InvariantCulture) + " m")})");
        }

        // The way walks: along the stair, the tunnels and the shaft's turns, a floor every few metres.
        var way = new[] { new V3(0, 0, 3), new V3(0, -7, 15.2f), new V3(0, -14, 29.2f), new V3(0, -16, 40), new V3(10, -16, 52), new V3(26, -16, 52), new V3(45, -16, 52), new V3(-14, -16, 54), new V3(-30, -16, 57), new V3(-45, -16, 53), new V3(0, -16, 64), new V3(4, -18.5f, 79), new V3(0, -25, 102), new V3(0, -40.2f, 107.5f), new V3(0, -41, 113), new V3(2, -45, 124), new V3(0, -50, 140) };
        var held = 0;

        foreach (var p in way)
        {
            var hit = Down(solid, p + new V3(0, 1.5f, 0), 4);
            held += hit >= 0 && hit < 3.2f ? 1 : 0;
        }

        check(held == way.Length, $"depths: the way down has a floor all along ({held} of {way.Length})");
        var same = DepthsGen.Make("gu_yue", 7);
        check(same.Lods[0].Triangles == model.Lods[0].Triangles, "depths: the same seed digs the same underground");
        report.Add($"depths: the Gu Yue's underground in {elapsed} ms, {model.Lods[0].Triangles / 1000}k triangles near, {model.Lods[1].Triangles / 1000}k far, {model.Spots.Count} places, {model.Lights.Count} lights, down to {model.Deepest:0} m");

        if (!export)
        {
            return;
        }

        var dir = Path.Combine(GenChecks.OutDir(repo), "depths");
        Directory.CreateDirectory(dir);
        Glb.Write(Path.Combine(dir, "gu_yue_depths.glb"), BuildingChecks.Parts(model.Lods[0], "../btex"));
        var json = new StringBuilder("{\"lights\":[");

        for (var i = 0; i < model.Lights.Count; i++)
        {
            var l = model.Lights[i];
            json.Append(i > 0 ? "," : "").Append(string.Format(CultureInfo.InvariantCulture, "{{\"at\":[{0:0.##},{1:0.##},{2:0.##}],\"colour\":[{3:0.##},{4:0.##},{5:0.##}],\"range\":{6:0.#},\"intensity\":{7:0.##}}}", l.At.X, l.At.Y, l.At.Z, l.Colour.R, l.Colour.G, l.Colour.B, l.Range, l.Intensity));
        }

        json.Append("],\"spots\":[");

        for (var i = 0; i < model.Spots.Count; i++)
        {
            var s = model.Spots[i];
            json.Append(i > 0 ? "," : "").Append(string.Format(CultureInfo.InvariantCulture, "{{\"at\":[{0:0.##},{1:0.##},{2:0.##}],\"role\":\"{3}\"}}", s.At.X, s.At.Y, s.At.Z, s.Role));
        }

        File.WriteAllText(Path.Combine(dir, "gu_yue_depths.json"), json.Append("]}").ToString());
        report.Add($"depths: → {dir}");
    }

    /// <summary>How far down from a point a ray meets the mesh (−1: not within `reach`).</summary>
    static float Down(MeshData mesh, V3 from, float reach)
    {
        var best = -1f;

        for (var t = 0; t < mesh.Triangles.Count; t += 3)
        {
            V3 P(int i) => new V3(mesh.Positions[i * 3], mesh.Positions[i * 3 + 1], mesh.Positions[i * 3 + 2]);
            var a = P(mesh.Triangles[t]);
            var b = P(mesh.Triangles[t + 1]);
            var c = P(mesh.Triangles[t + 2]);
            // In the triangle seen from above?
            var d = (b.Z - c.Z) * (a.X - c.X) + (c.X - b.X) * (a.Z - c.Z);

            if (Math.Abs(d) < 1e-9f)
            {
                continue;
            }

            var l1 = ((b.Z - c.Z) * (from.X - c.X) + (c.X - b.X) * (from.Z - c.Z)) / d;
            var l2 = ((c.Z - a.Z) * (from.X - c.X) + (a.X - c.X) * (from.Z - c.Z)) / d;
            var l3 = 1 - l1 - l2;

            if (l1 < 0 || l2 < 0 || l3 < 0)
            {
                continue;
            }

            var y = l1 * a.Y + l2 * b.Y + l3 * c.Y;
            var drop = from.Y - y;

            if (drop >= 0 && drop <= reach && (best < 0 || drop < best))
            {
                best = drop;
            }
        }

        return best;
    }
}
