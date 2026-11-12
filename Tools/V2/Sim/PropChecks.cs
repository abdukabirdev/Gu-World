using System;
using System.Collections.Generic;
using System.IO;
using GuWorld.Core;
using GuWorld.Core.Gen;

/// <summary>
/// The models made in code, looked at without the engine: every kind is built, kept within its
/// footprint and its budget of triangles, its normals sound; and each is drawn by a small software
/// renderer (perspective, depth buffer, sun and sky light) into a contact sheet, with faces turned the
/// wrong way painted magenta so a wrong winding shows at a glance.
/// </summary>
static class PropChecks
{
    const int Tile = 360;

    public static void Run(string repo, Action<bool, string> check, List<string> report)
    {
        var all = new List<(string name, MeshData mesh)>();

        foreach (var (kind, variants) in Props.Kinds)
        {
            for (var v = 0; v < variants; v++)
            {
                var mesh = Props.Make(kind, v);
                check(mesh != null && mesh.TriangleCount > 0, $"prop {kind}/{v} is built");

                if (mesh == null)
                {
                    continue;
                }

                check(mesh.TriangleCount < 24000, $"prop {kind}/{v}: {mesh.TriangleCount} triangles, within the budget");
                var sound = true;

                for (var i = 0; i < mesh.Normals.Count; i += 3)
                {
                    var length = Math.Sqrt(mesh.Normals[i] * mesh.Normals[i] + mesh.Normals[i + 1] * mesh.Normals[i + 1] + mesh.Normals[i + 2] * mesh.Normals[i + 2]);
                    sound &= Math.Abs(length - 1) < 0.01;
                }

                check(sound, $"prop {kind}/{v}: its normals are of unit length");
                mesh.Bounds(out var minX, out var minY, out var minZ, out var maxX, out _, out var maxZ);
                var (width, depth) = Props.Footprint(kind, v);
                check(maxX - minX <= width * 1.25f + 1.2f && maxZ - minZ <= depth * 1.25f + 1.2f, $"prop {kind}/{v}: within its footprint {width} × {depth} (is {maxX - minX:0.0} × {maxZ - minZ:0.0})");
                check(minY > -16, $"prop {kind}/{v}: does not reach far below its foot");
                all.Add(($"{kind}/{v}", mesh));
            }
        }

        // The rooms' shells (seen from inside, so a share of their faces turn away from a camera outside: not counted).
        foreach (var room in Interiors.All)
        {
            var shell = Props.Room(room.Width, room.Depth, room.Height, room.Style);
            check(shell.TriangleCount > 0 && shell.TriangleCount < 24000, $"room {room.Id}: {shell.TriangleCount} triangles");

            foreach (var (model, x, z, yaw) in room.Furniture)
            {
                check(Catalog.TryGet(model, out var footprint), $"room {room.Id}: {model} is a model");
                var turned = Math.Abs(Math.Abs(yaw) - 90) < 1;
                var across = turned ? footprint.Deep : footprint.Across;
                var deep = turned ? footprint.Across : footprint.Deep;
                check(Math.Abs(x) + across * 0.5f <= room.Width / 2 + 0.2f && Math.Abs(z) + deep * 0.5f <= room.Depth / 2 + 0.2f, $"room {room.Id}: {model} stands inside");
            }
        }

        // The rooms seen from inside, from the door: a sheet of their own.
        var roomsRgb = new byte[Tile * 2 * Tile * 2 * 3];
        var roomWrong = 0;
        var roomTotal = 0;

        for (var i = 0; i < Interiors.All.Length && i < 4; i++)
        {
            var room = Interiors.All[i];
            var shell = Props.Room(room.Width, room.Depth, room.Height, room.Style);
            var (w, t) = Draw(shell, roomsRgb, Tile * 2, (i % 2) * Tile, (i / 2) * Tile, new V3(0, 0, 1), true, new V3(room.Width * 0.3f, 1.7f, -room.Depth / 2 + 1.2f), new V3(-room.Width * 0.1f, room.Height * 0.45f, room.Depth / 2));
            roomWrong += w;
            roomTotal += t;
        }

        WorldChecks.WritePng(Path.Combine(repo, "Logs", "V2", "rooms.png"), Tile * 2, Tile * 2, roomsRgb);
        check(roomWrong < roomTotal * 0.02f, $"rooms: seen from inside, their faces turn the right way ({roomWrong} of {roomTotal})");

        // The ground's growth too (not checked against footprints: they are tufts).
        for (var kind = 0; kind < Grass.Kinds; kind++)
        {
            all.Add(($"grass/{kind}", Grass.Mesh(kind)));
        }

        // The contact sheet: four across.
        var columns = 4;
        var rows = (all.Count + columns - 1) / columns;
        var width2 = columns * Tile;
        var height2 = rows * Tile;
        var rgb = new byte[width2 * height2 * 3];
        var wrong = 0;
        var total = 0;

        for (var i = 0; i < all.Count; i++)
        {
            // Seen from the front and from behind (the second only counted, not painted).
            var (w, t) = Draw(all[i].mesh, rgb, width2, (i % columns) * Tile, (i / columns) * Tile, new V3(0.75f, 0.55f, 1.1f), true);
            var (w2, t2) = Draw(all[i].mesh, rgb, width2, 0, 0, new V3(-0.8f, 0.5f, -1.0f), false);
            wrong += w + w2;
            total += t + t2;

            if (w + w2 > (t + t2) * 0.02f)
            {
                report?.Add($"props: {all[i].name} has faces turned away ({w} from the front, {w2} from behind)");
            }
        }

        var outDir = Path.Combine(repo, "Logs", "V2");
        Directory.CreateDirectory(outDir);
        var path = Path.Combine(outDir, "props.png");
        WorldChecks.WritePng(path, width2, height2, rgb);
        report?.Add($"props: {all.Count} models, {total} pixels drawn, {wrong} of them by faces turned away ({100.0 * wrong / Math.Max(1, total):0.0}%) → {path}");
        check(wrong < total * 0.02, "props: hardly a pixel is drawn by a face turned away (windings are right)");
    }

    /// <summary>Draws a mesh into its tile; returns the pixels drawn by back faces and all pixels drawn.</summary>
    static (int wrong, int total) Draw(MeshData mesh, byte[] rgb, int stride, int ox, int oy, V3 view, bool paint, V3? eyeAt = null, V3? lookAt = null)
    {
        var depth = new float[Tile * Tile];
        var back = new bool[Tile * Tile];

        for (var y = 0; y < Tile; y++)
        {
            for (var x = 0; x < Tile; x++)
            {
                depth[y * Tile + x] = float.MaxValue;
                // The sky behind: a pale gradient.
                var k = y / (float)Tile;

                if (paint)
                    Put(rgb, stride, ox + x, oy + y, 0.62f + 0.2f * k, 0.70f + 0.16f * k, 0.78f + 0.1f * k);
            }
        }

        mesh.Bounds(out var minX, out var minY, out var minZ, out var maxX, out var maxY, out var maxZ);
        var centre = new V3((minX + maxX) / 2, (minY + maxY) / 2, (minZ + maxZ) / 2);
        var radius = Math.Max(0.5f, new V3(maxX - minX, maxY - minY, maxZ - minZ).Length / 2);
        // The camera before the model (it faces +z), to its right and above.
        var eye = eyeAt ?? centre + view.Normalized * (radius * 2.6f);
        var forward = ((lookAt ?? centre) - eye).Normalized;
        var focal = Tile * (eyeAt != null ? 0.5f : 1.05f);
        var right = V3.Cross(V3.Up, forward).Normalized;
        var up = V3.Cross(forward, right);
        var sun = new V3(0.45f, 0.8f, 0.35f).Normalized;

        (float x, float y, float z) Project(int v)
        {
            var p = new V3(mesh.Positions[v * 3], mesh.Positions[v * 3 + 1], mesh.Positions[v * 3 + 2]) - eye;
            var z = V3.Dot(p, forward);
            var x = V3.Dot(p, right);
            var y = V3.Dot(p, up);

            return (Tile / 2f + x / z * focal, Tile / 2f - y / z * focal, z);
        }

        var wrong = 0;
        var total = 0;
        var colour = new float[Tile * Tile * 3];
        var drawn = new bool[Tile * Tile];

        for (var t = 0; t < mesh.Triangles.Count; t += 3)
        {
            int ia = mesh.Triangles[t], ib = mesh.Triangles[t + 1], ic = mesh.Triangles[t + 2];
            var a = Project(ia);
            var b = Project(ib);
            var c = Project(ic);

            if (a.z <= 0.01f || b.z <= 0.01f || c.z <= 0.01f)
            {
                continue;
            }

            // Signed area on a screen whose y runs up: the engine's fronts run clockwise, so negative.
            var area = (b.x - a.x) * (-(c.y) + a.y) - (c.x - a.x) * (-(b.y) + a.y);
            var isBack = area > 0;
            var n = new V3(mesh.Normals[ia * 3], mesh.Normals[ia * 3 + 1], mesh.Normals[ia * 3 + 2]);
            var light = 0.38f + 0.62f * Math.Max(0, V3.Dot(n, sun)) + 0.1f * Math.Max(0, n.Y);
            float r = mesh.Colours[ia * 4] * light, g = mesh.Colours[ia * 4 + 1] * light, bl = mesh.Colours[ia * 4 + 2] * light;

            var x0 = (int)Math.Max(0, Math.Floor(Math.Min(a.x, Math.Min(b.x, c.x))));
            var x1 = (int)Math.Min(Tile - 1, Math.Ceiling(Math.Max(a.x, Math.Max(b.x, c.x))));
            var y0 = (int)Math.Max(0, Math.Floor(Math.Min(a.y, Math.Min(b.y, c.y))));
            var y1 = (int)Math.Min(Tile - 1, Math.Ceiling(Math.Max(a.y, Math.Max(b.y, c.y))));
            var full = (b.x - a.x) * (c.y - a.y) - (c.x - a.x) * (b.y - a.y);

            if (Math.Abs(full) < 1e-6f)
            {
                continue;
            }

            for (var y = y0; y <= y1; y++)
            {
                for (var x = x0; x <= x1; x++)
                {
                    float px = x + 0.5f, py = y + 0.5f;
                    var w0 = ((b.x - px) * (c.y - py) - (c.x - px) * (b.y - py)) / full;
                    var w1 = ((c.x - px) * (a.y - py) - (a.x - px) * (c.y - py)) / full;
                    var w2 = 1 - w0 - w1;

                    if (w0 < 0 || w1 < 0 || w2 < 0)
                    {
                        continue;
                    }

                    // A face turned away loses to a front face at the same depth (two-sided sheets are not mistakes).
                    var z = 1 / (w0 / a.z + w1 / b.z + w2 / c.z) + (isBack ? radius * 0.01f : 0);
                    var at = y * Tile + x;

                    if (z >= depth[at])
                    {
                        continue;
                    }

                    depth[at] = z;
                    back[at] = isBack;
                    drawn[at] = true;
                    // A little fog with distance, so depth reads.
                    var fog = Math.Min(0.35f, (z - radius) / (radius * 8));
                    colour[at * 3] = isBack ? 1 : r * (1 - fog) + 0.7f * fog;
                    colour[at * 3 + 1] = isBack ? 0 : g * (1 - fog) + 0.76f * fog;
                    colour[at * 3 + 2] = isBack ? 1 : bl * (1 - fog) + 0.82f * fog;
                }
            }
        }

        for (var i = 0; i < Tile * Tile; i++)
        {
            if (!drawn[i])
            {
                continue;
            }

            total++;
            wrong += back[i] ? 1 : 0;

            if (paint)
                Put(rgb, stride, ox + i % Tile, oy + i / Tile, colour[i * 3], colour[i * 3 + 1], colour[i * 3 + 2]);
        }

        // A thin frame between tiles.
        for (var i = 0; i < Tile && paint; i++)
        {
            Put(rgb, stride, ox + i, oy, 0.3f, 0.3f, 0.32f);
            Put(rgb, stride, ox, oy + i, 0.3f, 0.3f, 0.32f);
        }


        return (wrong, total);
    }

    static void Put(byte[] rgb, int stride, int x, int y, float r, float g, float b)
    {
        var i = (y * stride + x) * 3;
        rgb[i] = (byte)(Math.Clamp(r, 0, 1) * 255);
        rgb[i + 1] = (byte)(Math.Clamp(g, 0, 1) * 255);
        rgb[i + 2] = (byte)(Math.Clamp(b, 0, 1) * 255);
    }
}
