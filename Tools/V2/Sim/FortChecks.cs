using System;
using System.Collections.Generic;
using System.IO;
using GuWorld.Core.Gen;
using GuWorld.Core.Gen.Buildings;
using GuWorld.Core.Gen.Flora;

/// <summary>
/// The ring and outskirts generators' checks: every rampart's wall, tower and gate on rolling
/// ground, every thing; judged (sound numbers, levels shrinking, a wall that meets the ground all
/// along); with `export`, a scene per rampart — an arc of the ring with its gate, towers, the
/// buildings on them and the things outside it — and a yard of all the things, as .glb
/// (Logs/V2/gen/forts).
/// </summary>
static class FortChecks
{
    /// <summary>A scene put together from pieces: their parts by material, moved and turned into place.</summary>
    sealed class Scene
    {
        public readonly TreeLod Lod = new TreeLod();

        /// <summary>Adds a level of detail at `at`, turned by `yaw` degrees (Unity's sense: local +z to (sin, cos)).</summary>
        public void Add(TreeLod lod, V3 at, float yaw)
        {
            var a = yaw * (float)Math.PI / 180;
            var cos = (float)Math.Cos(a);
            var sin = (float)Math.Sin(a);

            foreach (var part in lod.Parts)
            {
                var into = Lod.Part(part.Material);
                var from = part.Mesh;
                var start = into.VertexCount;

                for (var i = 0; i < from.VertexCount; i++)
                {
                    var x = from.Positions[i * 3];
                    var y = from.Positions[i * 3 + 1];
                    var z = from.Positions[i * 3 + 2];
                    var v = into.Vertex(at.X + x * cos + z * sin, at.Y + y, at.Z - x * sin + z * cos, from.Uvs[i * 2], from.Uvs[i * 2 + 1], from.Colours[i * 4], from.Colours[i * 4 + 1], from.Colours[i * 4 + 2], from.Colours[i * 4 + 3]);
                    var nx = from.Normals[i * 3];
                    var nz = from.Normals[i * 3 + 2];
                    into.SetNormal(v, nx * cos + nz * sin, from.Normals[i * 3 + 1], -nx * sin + nz * cos);
                }

                for (var i = 0; i < from.Triangles.Count; i += 3)
                {
                    into.Triangle(from.Triangles[i] + start, from.Triangles[i + 1] + start, from.Triangles[i + 2] + start);
                }
            }
        }

        /// <summary>The ground as a coloured sheet over a square.</summary>
        public void Ground(Func<float, float, float> height, float x0, float z0, float x1, float z1, float cell)
        {
            var mesh = Lod.Part("ground");
            var nx = (int)((x1 - x0) / cell);
            var nz = (int)((z1 - z0) / cell);
            var noise = new Noise(5);

            for (var j = 0; j <= nz; j++)
            {
                for (var i = 0; i <= nx; i++)
                {
                    var x = x0 + i * cell;
                    var z = z0 + j * cell;
                    var k = 0.85f + 0.3f * (0.5f + 0.5f * noise.Fbm(x * 0.05f, z * 0.05f, 3));
                    var v = mesh.Vertex(x, height(x, z), z, 0, 0, 0.09f * k, 0.13f * k, 0.05f * k, 1);
                    var e = 0.5f;
                    var n = new V3(height(x - e, z) - height(x + e, z), 2 * e, height(x, z - e) - height(x, z + e)).Normalized;
                    mesh.SetNormal(v, n.X, n.Y, n.Z);
                }
            }

            for (var j = 0; j < nz; j++)
            {
                for (var i = 0; i < nx; i++)
                {
                    var a = j * (nx + 1) + i;
                    // Clockwise seen from above (the engine's front faces).
                    mesh.Triangle(a, a + nx + 1, a + nx + 2);
                    mesh.Triangle(a, a + nx + 2, a + 1);
                }
            }
        }
    }

    static Func<float, float, float> Local(Func<float, float, float> world, V3 at, float yaw)
    {
        var a = yaw * (float)Math.PI / 180;
        var cos = (float)Math.Cos(a);
        var sin = (float)Math.Sin(a);

        return (x, z) => world(at.X + x * cos + z * sin, at.Z - x * sin + z * cos) - at.Y;
    }

    /// <summary>The yaw that turns a piece's outer face (-Z) toward `dx, dz`.</summary>
    static float Facing(float dx, float dz) => (float)(Math.Atan2(-dx, -dz) * 180 / Math.PI);

    static void Judge(FortModel model, Action<bool, string> check, string label)
    {
        var sound = true;

        foreach (var part in model.Lods[0].Parts)
        {
            foreach (var v in part.Mesh.Positions)
            {
                if (float.IsNaN(v) || float.IsInfinity(v) || Math.Abs(v) > 300)
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

        var t = new int[4];

        for (var i = 0; i < 4; i++)
        {
            t[i] = model.Lods[i].Triangles;
        }

        var shrinking = t[0] >= t[1] && t[1] >= t[2] && t[2] >= t[3] && t[0] > t[3] && t[3] > 0;

        if (!sound || !shrinking)
        {
            check(false, $"forts: {label} — sound {sound}, levels {t[0]}/{t[1]}/{t[2]}/{t[3]}");
        }
    }

    public static void Run(string repo, Action<bool, string> check, List<string> report, bool export)
    {
        var dir = Path.Combine(GenChecks.OutDir(repo), "forts");

        if (export)
        {
            Directory.CreateDirectory(dir);
        }

        var noise = new Noise(77);
        Func<float, float, float> hills = (x, z) => 3.2f * noise.Fbm(x * 0.012f, z * 0.012f, 3) + 1.4f * (float)Math.Sin(x * 0.035f) + 0.25f * noise.Fbm(x * 0.2f, z * 0.2f, 2);
        var watch = System.Diagnostics.Stopwatch.StartNew();
        var pieces = 0;
        long near = 0;

        foreach (var style in Ramparts.All)
        {
            var clan = style.Kind == RampartKind.Plaster ? "bai" : style.Kind == RampartKind.Earth ? "xiong" : "gu_yue";
            var p = new FortParams { Wealth = 0.6f, Wear = style.Kind == RampartKind.Earth ? 0.55f : 0.3f, Clan = clan };
            var scene = new Scene();
            var radius = style.Kind == RampartKind.Stone ? 120 : style.Kind == RampartKind.Bamboo ? 40 : 70;
            var centre = new V3(0, 0, radius);
            var arc = style.Kind == RampartKind.Bamboo ? 70f : 44f;
            var piece = style.Piece;
            var steps = (int)(radius * arc * 2 * Math.PI / 180 / piece);
            var gateHalf = style.Kind == RampartKind.Stone ? 11f : style.Kind == RampartKind.Earth || style.Kind == RampartKind.Plaster ? 7f : 3.2f;
            var lowest = float.MaxValue;
            var highest = float.MinValue;

            // The gate at the arc's middle (toward -Z), towers every so often, wall between.
            for (var i = -steps / 2; i <= steps / 2; i++)
            {
                var theta = i * piece / radius;
                var dx = (float)Math.Sin(theta);
                var dz = -(float)Math.Cos(theta);
                var at = new V3(centre.X + dx * radius, 0, centre.Z + dz * radius);
                at.Y = hills(at.X, at.Z);
                var yaw = Facing(dx, dz);
                var along = theta * radius;
                FortModel model;

                if (Math.Abs(along) < gateHalf)
                {
                    if (i != 0)
                    {
                        continue;
                    }

                    model = FortGen.Gate(style, 3, Local(hills, at, yaw), p);
                }
                else if (i % 4 == 0 && style.Kind != RampartKind.Bamboo && style.Kind != RampartKind.Plaster)
                {
                    model = FortGen.Tower(style, i + 50, Local(hills, at, yaw), p);
                    var wall = FortGen.Wall(style, i + 100, piece + 0.3f, Local(hills, at, yaw), p);
                    scene.Add(wall.Lods[0], at, yaw);
                    near += wall.Lods[0].Triangles;
                    pieces++;
                }
                else
                {
                    var length = piece + 0.3f;

                    // Trim the pieces beside the gate so they meet it.
                    if (Math.Abs(along) - piece * 0.5f < gateHalf)
                    {
                        var keep = Math.Abs(along) + piece * 0.5f - gateHalf;
                        var shift = (piece - keep) * 0.5f * Math.Sign(along);
                        var mid = theta + shift / radius;
                        dx = (float)Math.Sin(mid);
                        dz = -(float)Math.Cos(mid);
                        at = new V3(centre.X + dx * radius, 0, centre.Z + dz * radius);
                        at.Y = hills(at.X, at.Z);
                        yaw = Facing(dx, dz);
                        length = keep + 0.3f;
                    }

                    model = FortGen.Wall(style, i + 100, length, Local(hills, at, yaw), p);
                }

                Judge(model, check, $"{style.Id} piece {i}");
                scene.Add(model.Lods[0], at, yaw);
                near += model.Lods[0].Triangles;
                pieces++;
                lowest = Math.Min(lowest, at.Y);
                highest = Math.Max(highest, at.Y);

                if (!string.IsNullOrEmpty(model.TopModel))
                {
                    var top = BuildingGen.Make(Buildings.Get(model.TopModel), i + 7, new BuildingParams { Width = model.TopWidth, Depth = model.TopDepth, Clan = clan, Wealth = 0.7f, Wear = p.Wear });
                    scene.Add(top.Lods[0], new V3(at.X, at.Y + model.TopY, at.Z), yaw + 180);
                }

                if (i == 0)
                {
                    // Outside the gate: banners and lanterns either side, barricades, a shrine and a stele by the road, a signpost.
                    var outward = new V3(dx, 0, dz);
                    var right = new V3(-dz, 0, dx);
                    void Thing(string kind, float side, float out_, float turn, int seed)
                    {
                        var spot = at + right * side + outward * out_;
                        spot.Y = hills(spot.X, spot.Z);
                        var thing = ThingGen.Make(kind, seed, p);
                        Judge(thing, check, kind);
                        scene.Add(thing.Lods[0], spot, yaw + turn);
                    }

                    var w = gateHalf + 2;
                    Thing("banner_pole", -w, 6, 0, 1);
                    Thing("banner_pole", w, 6, 0, 2);
                    Thing("lantern_post", -4, 9, 90, 3);
                    Thing("lantern_post", 4, 9, -90, 4);
                    Thing("barricade", -9, 16, 10, 5);
                    Thing("barricade", 9, 16, -10, 6);
                    Thing("shrine", -13, 24, 70, 7);
                    Thing("stele", 12, 22, -60, 8);
                    Thing("signpost", 5, 30, 0, 9);
                    Thing("haystack", -24, 34, 0, 10);
                    Thing("haystack", -30, 30, 0, 11);
                    Thing("drying_rack", 26, 30, 20, 12);
                    Thing("woodpile", 18, 12, 0, 13);
                    Thing("cart", -6, 20, 30, 14);
                    Thing("wattle", 24, 40, 0, 15);
                    Thing("wattle", 30, 40, 0, 16);
                    Thing("scarecrow", 28, 46, 0, 17);
                    Thing("well", -20, 12, 0, 18);
                    Thing("grave", 40, 18, -20, 19);
                    Thing("grave", 43, 21, -25, 20);
                }
            }

            if (export)
            {
                var span = radius * 0.9f;
                scene.Ground(hills, -span - 10, -60, span + 10, radius * 0.9f, 2f);
                Glb.Write(Path.Combine(dir, $"ring_{style.Id}.glb"), BuildingChecks.Parts(scene.Lod, "../btex"));
            }
        }

        // A yard of every thing, in a row.
        var yard = new Scene();
        var n = 0;

        foreach (var kind in ThingGen.Kinds)
        {
            var thing = ThingGen.Make(kind, 3, new FortParams { Wealth = 0.6f, Wear = 0.3f, Clan = "gu_yue" });
            Judge(thing, check, kind);
            // Two rows of seven, 7 m apart, facing the camera (-Z).
            yard.Add(thing.Lods[0], new V3((n % 7) * 7f - 21, 0, (n / 7) * 8f), 0);
            n++;
        }

        if (export)
        {
            yard.Ground((a, b) => 0, -30, -14, 30, 18, 4);
            Glb.Write(Path.Combine(dir, "things.glb"), BuildingChecks.Parts(yard.Lod, "../btex"));
        }

        report.Add($"forts: {Ramparts.All.Length} ramparts, {pieces} pieces and {ThingGen.Kinds.Length} things raised in {watch.ElapsedMilliseconds} ms; {near / Math.Max(1, pieces) / 1000}k triangles a piece near");

        // A wall meets the ground all along: its foot is under the ground at every metre.
        var test = FortGen.Wall(Ramparts.Stone, 1, 12, (a, b) => 2.5f * (float)Math.Sin(a * 0.4f), new FortParams());
        var lowestFoot = float.MaxValue;

        var stones = test.Lods[0].Part("tex:stone").Positions;

        for (var i = 1; i < stones.Count; i += 3)
        {
            lowestFoot = Math.Min(lowestFoot, stones[i]);
        }

        check(lowestFoot < -2.5f, $"forts: a wall's foot goes under rolling ground ({lowestFoot:0.0} m)");
    }
}
