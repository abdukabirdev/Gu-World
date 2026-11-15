using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using GuWorld.Core.Gen;
using GuWorld.Core.Gen.Buildings;
using GuWorld.Core.Gen.Flora;

/// <summary>
/// A piece of a planned settlement as a 3D scene, to judge it by eye without the engine: every
/// building, piece of the ring and thing in a square round a point raised as the game raises it
/// (generated buildings turned to face +Z, as Town places them), on the land's own ground, as one
/// .glb (Logs/V2/gen/towns/&lt;settlement&gt;_&lt;x&gt;_&lt;z&gt;.glb).
///   dotnet run -c Release -- gen export only=town [town=gu_yue] [at=x,z] [size=metres] [lod=1]
/// </summary>
static class TownScene
{
    public static void Run(string repo, Action<bool, string> check, List<string> report, string[] args)
    {
        string Arg(string key, string fallback) => Array.Find(args, a => a.StartsWith(key + "="))?.Substring(key.Length + 1) ?? fallback;
        var id = Arg("town", "gu_yue");
        var size = float.Parse(Arg("size", "180"), CultureInfo.InvariantCulture);
        var lod = int.Parse(Arg("lod", "1"), CultureInfo.InvariantCulture);
        var plan = WorldPlan.Get();
        var settlement = plan.Settlements.Find(s => s.Def.Id == id);

        if (settlement == null)
        {
            check(false, $"town scene: no settlement {id}");
            return;
        }

        var at = settlement.Centre;
        var given = Arg("at", "");

        if (given.Length > 0)
        {
            var xz = given.Split(',');
            at = new Vec2(float.Parse(xz[0], CultureInfo.InvariantCulture), float.Parse(xz[1], CultureInfo.InvariantCulture));
        }

        var land = plan.Land;
        var half = size * 0.5f;

        var probe = Arg("probe", "");

        if (probe.Length > 0)
        {
            var xz = probe.Split(',');
            var px = float.Parse(xz[0], CultureInfo.InvariantCulture);
            var pz = float.Parse(xz[1], CultureInfo.InvariantCulture);

            for (var k = -4; k <= 4; k++)
            {
                foreach (var (ox, oz) in new[] { (k * 2f, 0f), (0f, k * 2f) })
                {
                    report.Add($"probe ({px + ox:0}, {pz + oz:0}): base {land.Base(px + ox, pz + oz):0.0} shaped {land.Shaped(px + ox, pz + oz):0.0} game {land.Height(px + ox, pz + oz):0.0}");
                }
            }

            foreach (var f in land.Flats)
            {
                if (Vec2.Distance(f.At, new Vec2(px, pz)) < f.Radius + f.Blend + 20)
                {
                    report.Add($"probe flat {f.Id} at ({f.At.X:0}, {f.At.Z:0}) r {f.Radius:0} blend {f.Blend:0} level {f.Level:0.0}");
                }
            }

            foreach (var r in plan.Roads.All)
            {
                for (var i = 0; i < r.Points.Count; i++)
                {
                    if (Vec2.Distance(r.Points[i], new Vec2(px, pz)) < 6)
                    {
                        report.Add($"probe road {r.From}→{r.To} point {i}/{r.Points.Count}: profile {r.Profile[i]:0.0}, land {land.Shaped(r.Points[i].X, r.Points[i].Z):0.0}");
                    }
                }
            }

            return;
        }

        if (Array.Exists(args, a => a == "relief"))
        {
            // The ground's fall over each settlement (the unlevelled land and as the game has it).
            foreach (var s in plan.Settlements)
            {
                float lo = float.MaxValue, hi = float.MinValue, nlo = float.MaxValue, nhi = float.MinValue;

                for (var a = 0; a < 24; a++)
                {
                    for (var r = 0.1f; r <= 1; r += 0.15f)
                    {
                        var p = s.Centre + Vec2.FromYaw(a * 15) * (s.Def.Radius * r);
                        var h = land.Height(p.X, p.Z);
                        var nb = land.Base(p.X, p.Z);
                        lo = Math.Min(lo, h); hi = Math.Max(hi, h); nlo = Math.Min(nlo, nb); nhi = Math.Max(nhi, nb);
                    }
                }

                report.Add($"relief {s.Def.Id,-14} radius {s.Def.Radius,4:0}: game {lo:0}..{hi:0} m ({hi - lo:0} m), the mountain itself {nlo:0}..{nhi:0} m ({nhi - nlo:0} m)");
            }

            return;
        }
        var level = land.Height(at.X, at.Z);
        var scene = new TreeLod();
        var made = new Dictionary<string, TreeLod>();
        var count = 0;
        var watch = System.Diagnostics.Stopwatch.StartNew();

        foreach (var building in settlement.Buildings)
        {
            var d = building.Footprint.Centre - at;

            if (Math.Abs(d.X) > half || Math.Abs(d.Z) > half)
            {
                continue;
            }

            var model = building.Model;
            var foot = land.Height(building.Footprint.Centre.X, building.Footprint.Centre.Z) - 0.05f;
            var place = new V3(building.At.X - at.X, foot - level, building.At.Z - at.Z);
            TreeLod lods;

            if (model.StartsWith("hill:"))
            {
                // Stands down to the slope where it is: raised for its place.
                var bits = model.Split(':');
                float.TryParse(bits.Length > 6 ? bits[6] : "0", NumberStyles.Float, CultureInfo.InvariantCulture, out var w);
                float.TryParse(bits.Length > 7 ? bits[7] : "0", NumberStyles.Float, CultureInfo.InvariantCulture, out var dd);
                // The building's own frame (front -Z) inside the place's (front +Z): turned about.
                var a = (building.Yaw + 180) * (float)Math.PI / 180;
                var cos = (float)Math.Cos(a);
                var sin = (float)Math.Sin(a);
                var ox = building.At.X;
                var oz = building.At.Z;
                var b = BuildingGen.Make(Buildings.Get(bits[1]), int.Parse(bits[2]), new BuildingParams { Clan = bits[3], Wealth = int.Parse(bits[4]) / 9f, Wear = int.Parse(bits[5]) / 9f, Width = w, Depth = dd, Ground = (lx, lz) => land.Height(ox + lx * cos + lz * sin, oz - lx * sin + lz * cos) - foot });
                Compose.Add(scene, b.Lods[lod], place, building.Yaw + 180);
                made[model] = b.Lods[lod];
            }
            else if (model.StartsWith("gen:"))
            {
                if (!made.TryGetValue(model, out lods))
                {
                    var bits = model.Split(':');
                    float.TryParse(bits.Length > 6 ? bits[6] : "0", NumberStyles.Float, CultureInfo.InvariantCulture, out var w);
                    float.TryParse(bits.Length > 7 ? bits[7] : "0", NumberStyles.Float, CultureInfo.InvariantCulture, out var dd);
                    var b = BuildingGen.Make(Buildings.Get(bits[1]), int.Parse(bits[2]), new BuildingParams { Clan = bits[3], Wealth = int.Parse(bits[4]) / 9f, Wear = int.Parse(bits[5]) / 9f, Width = w, Depth = dd });
                    lods = b.Lods[lod];
                    made[model] = lods;
                }

                // Turned to face +Z, as the game's places want.
                Compose.Add(scene, lods, place, building.Yaw + 180);
            }
            else if (model.StartsWith("fort:") || model.StartsWith("thing:"))
            {
                var bits = model.Split(':');
                var p = new FortParams { Clan = model.StartsWith("thing:") ? (bits.Length > 3 ? bits[3] : "") : (bits.Length > 4 ? bits[4] : ""), Wealth = 0.6f, Wear = 0.35f };
                var yaw = building.Yaw;
                var origin = new V3(building.At.X, foot, building.At.Z);
                var a = yaw * (float)Math.PI / 180;
                var cos = (float)Math.Cos(a);
                var sin = (float)Math.Sin(a);
                Func<float, float, float> ground = (x, z) => land.Height(origin.X + x * cos + z * sin, origin.Z - x * sin + z * cos) - foot;
                FortModel piece;

                if (model.StartsWith("thing:"))
                {
                    piece = ThingGen.Make(bits[1], int.Parse(bits[2]), p);
                }
                else
                {
                    var style = Ramparts.Get(bits[1]);
                    float.TryParse(bits.Length > 5 ? bits[5] : "0", NumberStyles.Float, CultureInfo.InvariantCulture, out var length);
                    piece = bits[2] == "tower" ? FortGen.Tower(style, int.Parse(bits[3]), ground, p) : bits[2] == "gate" ? FortGen.Gate(style, int.Parse(bits[3]), ground, p) : FortGen.Wall(style, int.Parse(bits[3]), length > 0 ? length : style.Piece, ground, p);

                    if (!string.IsNullOrEmpty(piece.TopModel))
                    {
                        var top = BuildingGen.Make(Buildings.Get(piece.TopModel), int.Parse(bits[3]), new BuildingParams { Clan = p.Clan, Wealth = 0.6f, Width = piece.TopWidth, Depth = piece.TopDepth });
                        Compose.Add(scene, top.Lods[lod], Compose.Place(new V3(0, piece.TopY, 0), place, yaw), yaw + 180);
                    }
                }

                Compose.Add(scene, piece.Lods[lod], place, yaw);
            }
            else
            {
                continue;
            }

            count++;
        }

        // The ground: the land's heights, coloured green, a little darker where streets run.
        var mesh = scene.Part("ground");
        var cell = 2f;
        var n = (int)(size / cell);

        for (var j = 0; j <= n; j++)
        {
            for (var i = 0; i <= n; i++)
            {
                var x = -half + i * cell;
                var z = -half + j * cell;
                var wx = at.X + x;
                var wz = at.Z + z;
                var street = false;

                foreach (var s in settlement.Streets)
                {
                    if (Lines.ToPolyline(new Vec2(wx, wz), s.Points) < s.HalfWidth)
                    {
                        street = true;
                        break;
                    }
                }

                var k = street ? 1.6f : 1f;
                var v = mesh.Vertex(x, land.Height(wx, wz) - level, z, 0, 0, (street ? 0.2f : 0.09f) * k, (street ? 0.17f : 0.13f) * k, (street ? 0.13f : 0.05f) * k, 1);
                var e = 0.5f;
                var nn = new V3(land.Height(wx - e, wz) - land.Height(wx + e, wz), 2 * e, land.Height(wx, wz - e) - land.Height(wx, wz + e)).Normalized;
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
        var name = $"{id}_{(int)at.X}_{(int)at.Z}";
        Glb.Write(Path.Combine(dir, name + ".glb"), BuildingChecks.Parts(scene, "../btex"));
        report.Add($"town scene: {id} round ({at.X:0}, {at.Z:0}), {size:0} m: {count} things ({made.Count} kinds of building) raised in {watch.ElapsedMilliseconds} ms, {scene.Triangles / 1000}k triangles → towns/{name}.glb");
    }
}
