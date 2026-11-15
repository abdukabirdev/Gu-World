using System;
using System.Collections.Generic;
using System.IO;
using GuWorld.Core.Gen;
using GuWorld.Core.Gen.Buildings;
using GuWorld.Core.Gen.Flora;

/// <summary>A hillside of village houses (each its own seed, standing down to the slope), to judge them in Blender: Logs/V2/gen/towns/slope.glb.</summary>
static class SlopeScene
{
    public static void Run(string repo, List<string> report)
    {
        // A slope rising to +Z (uphill, away from the camera), rolling a little, with a bench every few metres.
        var noise = new Noise(9);
        Func<float, float, float> ground = (x, z) => 0.34f * z + 1.5f * noise.Fbm(x * 0.03f, z * 0.03f, 2) + 0.8f * (float)Math.Sin(z * 0.25f);
        var scene = new TreeLod();
        var kinds = new[] { "house", "cottage", "hut", "stilt", "townhouse", "cottage", "house", "hut", "stilt", "courtyard", "house", "cottage", "smithy", "townhouse", "hut", "house" };
        var clans = new[] { "gu_yue", "", "xiong", "gu_yue", "bai", "" };
        var count = 0;

        for (var row = 0; row < 4; row++)
        {
            var x = -40f + row * 5;
            var z = -22f + row * 16;

            for (var k = 0; k < 4; k++)
            {
                var kind = kinds[row * 4 + k];
                var style = Buildings.Get(kind);
                var w = kind == "courtyard" ? 20 : kind == "hut" ? 8 : kind == "stilt" ? 9 : 12;
                var d = kind == "courtyard" ? 22 : kind == "hut" ? 7.5f : 10;
                var at = new V3(x + w * 0.5f, 0, z + 2 * (float)Math.Sin(k * 1.7f));
                at.Y = ground(at.X, at.Z);
                // Facing down the slope (toward -Z), turned a little.
                var yaw = (k * 37 % 20) - 10f;
                var a = yaw * (float)Math.PI / 180;
                var cos = (float)Math.Cos(a);
                var sin = (float)Math.Sin(a);
                var origin = at;
                var b = BuildingGen.Make(style, 100 + row * 7 + k * 13, new BuildingParams { Width = w, Depth = d, Wealth = (k + row) % 5 / 5f, Wear = 0.4f, Clan = clans[(row + k) % clans.Length], Ground = (lx, lz) => ground(origin.X + lx * cos + lz * sin, origin.Z - lx * sin + lz * cos) - origin.Y });
                Compose.Add(scene, b.Lods[0], at, yaw);
                x += w + 4 + 3 * (float)Math.Abs(Math.Sin(row * 3 + k));
                count++;
            }
        }

        var mesh = scene.Part("ground");
        var n = 60;

        for (var j = 0; j <= n; j++)
        {
            for (var i = 0; i <= n; i++)
            {
                var gx = -60 + 120f * i / n;
                var gz = -40 + 110f * j / n;
                var v = mesh.Vertex(gx, ground(gx, gz), gz, 0, 0, 0.09f, 0.13f, 0.05f, 1);
                var e = 0.5f;
                var nn = new V3(ground(gx - e, gz) - ground(gx + e, gz), 2 * e, ground(gx, gz - e) - ground(gx, gz + e)).Normalized;
                mesh.SetNormal(v, nn.X, nn.Y, nn.Z);
            }
        }

        for (var j = 0; j < n; j++)
        {
            for (var i = 0; i < n; i++)
            {
                var a = j * (n + 1) + i;
                mesh.Triangle(a, a + n + 1, a + n + 2);
                mesh.Triangle(a, a + n + 2, a + 1);
            }
        }

        var dir = Path.Combine(GenChecks.OutDir(repo), "towns");
        Directory.CreateDirectory(dir);
        Glb.Write(Path.Combine(dir, "slope.glb"), BuildingChecks.Parts(scene, "../btex"));
        report.Add($"slope: {count} houses on a hillside → towns/slope.glb");
    }
}
