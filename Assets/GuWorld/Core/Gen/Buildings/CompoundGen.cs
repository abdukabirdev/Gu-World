using System;
using System.Collections.Generic;
using GuWorld.Core.Gen.Flora;

namespace GuWorld.Core.Gen.Buildings
{
    /// <summary>Puts levels of detail together: each added part moved and turned into place (Unity's sense of yaw: local +Z to (sin, cos)).</summary>
    public static class Compose
    {
        public static void Add(TreeLod into, TreeLod lod, V3 at, float yaw)
        {
            var a = yaw * (float)Math.PI / 180;
            var cos = (float)Math.Cos(a);
            var sin = (float)Math.Sin(a);

            foreach (var part in lod.Parts)
            {
                Add(into.Part(part.Material), part.Mesh, at, cos, sin);
            }
        }

        public static void Add(MeshData into, MeshData from, V3 at, float yaw)
        {
            var a = yaw * (float)Math.PI / 180;
            Add(into, from, at, (float)Math.Cos(a), (float)Math.Sin(a));
        }

        static void Add(MeshData into, MeshData from, V3 at, float cos, float sin)
        {
            var start = into.VertexCount;
            var hasNormals = from.Normals.Count == from.Positions.Count;
            var hasUvs = from.Uvs.Count * 3 == from.Positions.Count * 2;
            var hasColours = from.Colours.Count * 3 == from.Positions.Count * 4;

            for (var i = 0; i < from.VertexCount; i++)
            {
                var x = from.Positions[i * 3];
                var y = from.Positions[i * 3 + 1];
                var z = from.Positions[i * 3 + 2];
                var v = into.Vertex(at.X + x * cos + z * sin, at.Y + y, at.Z - x * sin + z * cos,
                    hasUvs ? from.Uvs[i * 2] : 0, hasUvs ? from.Uvs[i * 2 + 1] : 0,
                    hasColours ? from.Colours[i * 4] : 1, hasColours ? from.Colours[i * 4 + 1] : 1, hasColours ? from.Colours[i * 4 + 2] : 1, hasColours ? from.Colours[i * 4 + 3] : 1);

                if (hasNormals)
                {
                    var nx = from.Normals[i * 3];
                    var nz = from.Normals[i * 3 + 2];
                    into.SetNormal(v, nx * cos + nz * sin, from.Normals[i * 3 + 1], -nx * sin + nz * cos);
                }
            }

            for (var i = 0; i < from.Triangles.Count; i += 3)
            {
                into.Triangle(from.Triangles[i] + start, from.Triangles[i + 1] + start, from.Triangles[i + 2] + start);
            }
        }

        /// <summary>A point of a piece's frame in the whole's.</summary>
        public static V3 Place(V3 local, V3 at, float yaw)
        {
            var a = yaw * (float)Math.PI / 180;
            var cos = (float)Math.Cos(a);
            var sin = (float)Math.Sin(a);

            return new V3(at.X + local.X * cos + local.Z * sin, at.Y + local.Y, at.Z - local.X * sin + local.Z * cos);
        }
    }

    /// <summary>
    /// A walled yard of buildings, raised as one model: a family's courtyard house (the gate in the
    /// front wall off to one side, a screen wall behind it, the main house across the back facing the
    /// gate, wings down the sides facing each other, a front row along the street for the rich; a
    /// paved yard with a well or a tree, jars, a woodpile, washing), or the clan's academy (a great
    /// yard for bouts, the lecture hall at its head on a high plinth, dormitories down its sides,
    /// a gatehouse, weapon racks), or a faction's manor (its hall at the head, the branch's houses down
    /// the sides, kitchen and storehouse by the gate, a yard to train in under its banners). Its front
    /// toward -Z, its lot `Width` × `Depth`.
    /// </summary>
    public static class CompoundGen
    {
        public static BuildingModel Make(BuildingStyle style, int seed, BuildingParams asked)
        {
            var rng = new Rng(seed * 4099 + 17);
            var academy = style.Compound == 2;
            var manor = style.Compound == 3;
            var wealth = Build.Clamp01(asked.Wealth);
            var width = asked.Width > 0 ? asked.Width : academy ? 56 : manor ? 40 : rng.Range(17f, 24f) + 8 * wealth;
            var depth = asked.Depth > 0 ? asked.Depth : academy ? 64 : manor ? 46 : rng.Range(19f, 26f) + 8 * wealth;
            width = Math.Max(academy ? 30 : manor ? 28 : 12, width);
            depth = Math.Max(academy ? 34 : manor ? 32 : 14, depth);
            // The tag carries a faction ("gu_yue.chi"): passed on whole to the buildings and things, the clan alone decides the build.
            var tag = asked.Clan ?? "";
            var clan = Factions.Clan(tag);
            var model = new BuildingModel { Style = style, Seed = seed, Asked = asked, Width = width, Depth = depth, EaveWidth = width, EaveDepth = depth, Storeys = 1 };
            var hw = width * 0.5f;
            var hd = depth * 0.5f;
            var fortParams = new FortParams { Clan = tag, Wealth = wealth, Wear = asked.Wear };
            var yardWall = clan == "bai" || clan == "gu_yue" && wealth > 0.35f || academy || manor && clan != "xiong" ? Ramparts.YardPlaster : Ramparts.YardEarth;
            var pieces = new List<(TreeLod[] lods, V3 at, float yaw, MeshData solid)>();
            Func<float, float, float> flat = (x, z) => 0;
            var gateWidth = academy ? 9f : manor ? 5.5f : 3.4f;
            // The gate: in the middle for the academy and a manor, off to the east (the right, seen from the street) for a house.
            var gateX = academy || manor ? 0 : hw * (rng.Chance(0.5f) ? 0.45f : -0.45f);

            void Wall(float x0, float x1, float z, float yaw)
            {
                var length = x1 - x0;

                if (length < 0.6f)
                {
                    return;
                }

                var n = Math.Max(1, (int)Math.Round(length / 9f));

                for (var i = 0; i < n; i++)
                {
                    var a = x0 + length * i / n;
                    var b = x0 + length * (i + 1) / n;
                    var piece = FortGen.Wall(yardWall, seed * 31 + pieces.Count, b - a + 0.05f, flat, fortParams);
                    var mid = (a + b) * 0.5f;
                    var at = yaw == 0 ? new V3(mid, 0, z) : yaw == 180 ? new V3(-mid, 0, z) : yaw == 90 ? new V3(z, 0, -mid) : new V3(z, 0, mid);
                    pieces.Add((piece.Lods, at, yaw, piece.Collision));
                }
            }

            // The enclosing wall: front (with the gate's gap), back, sides. A side piece's outer face turned outward.
            var t = yardWall.Thickness * 0.5f;
            Wall(-hw, gateX - gateWidth * 0.5f, -hd, 0);
            Wall(gateX + gateWidth * 0.5f, hw, -hd, 0);
            Wall(-hw, hw, hd, 180);
            Wall(-hd + t, hd - t, -hw, 90);
            Wall(-hd + t, hd - t, hw, 270);

            void Building(BuildingStyle kind, float w, float d, V3 at, float yaw, int k, float storeysWealth = -1)
            {
                var made = BuildingGen.Make(kind, seed * 13 + k, new BuildingParams { Width = w, Depth = d, Wealth = storeysWealth >= 0 ? storeysWealth : wealth, Wear = asked.Wear, Clan = tag, Storeys = asked.Storeys > 0 && kind != Buildings.Gate ? asked.Storeys : 0 });
                pieces.Add((made.Lods, at, yaw, made.Collision));
                model.Height = Math.Max(model.Height, made.Height);

                foreach (var door in made.Doors)
                {
                    model.Doors.Add(new BuildingDoor { At = Compose.Place(door.At, at, yaw), Facing = Compose.Place(door.Facing, new V3(0, 0, 0), yaw) });
                }
            }

            // The gatehouse in the gap: a deep roof over doors.
            var gate = BuildingGen.Make(Buildings.Gate, seed * 7 + 1, new BuildingParams { Width = gateWidth + (academy ? 4 : manor ? 2.5f : 1.2f), Depth = academy || manor ? 6 : 3.2f, Wealth = wealth, Wear = asked.Wear, Clan = tag });
            pieces.Add((gate.Lods, new V3(gateX, 0, -hd), 0, gate.Collision));
            model.Doors.Add(new BuildingDoor { At = new V3(gateX, 0, -hd - 1), Facing = new V3(0, 0, -1) });

            var inner = 0.9f;
            var houseStyle = academy ? Buildings.Hall : wealth > 0.7f && clan == "gu_yue" ? Buildings.Townhouse : clan == "xiong" ? Buildings.Cottage : Buildings.House;

            if (manor)
            {
                // The branch's hall across the head of the yard on its plinth; the branch's houses down both
                // sides facing in; the kitchen and the storehouse along the front wall either side of the gate;
                // the yard between them to train in.
                var hallDepth = Math.Min(13, depth * 0.27f);
                var hallWidth = width * 0.5f;
                Building(clan == "xiong" ? Buildings.GreatHall : Buildings.Hall, hallWidth, hallDepth, new V3(0, 0, hd - hallDepth * 0.5f - inner), 0, 1);
                var houseDepth = Math.Min(8, width * 0.2f);
                var houseLength = Math.Min(14, depth * 0.3f);
                var houseZ = hd - hallDepth - inner - houseLength * 0.5f - 0.5f;
                var side = clan == "xiong" ? Buildings.Cottage : Buildings.Townhouse;
                Building(side, houseLength, houseDepth, new V3(-hw + houseDepth * 0.5f + inner, 0, houseZ), -90, 2, wealth * 0.85f);
                Building(side, houseLength, houseDepth, new V3(hw - houseDepth * 0.5f - inner, 0, houseZ), 90, 3, wealth * 0.85f);
                var rowLength = hw - gateWidth * 0.5f - 2.2f - inner;
                var rowDepth = 6.5f;
                Building(Buildings.Kitchen, rowLength, rowDepth, new V3(-hw + inner + rowLength * 0.5f, 0, -hd + rowDepth * 0.5f + inner), 180, 4, wealth * 0.6f);
                Building(Buildings.Granary, rowLength, rowDepth, new V3(hw - inner - rowLength * 0.5f, 0, -hd + rowDepth * 0.5f + inner), 180, 5, wealth * 0.7f);
            }
            else if (academy)
            {
                // The lecture hall across the head of the yard; dormitories down both sides; the yard open for bouts.
                var hallDepth = Math.Min(18, depth * 0.3f);
                Building(Buildings.Hall, width * 0.62f, hallDepth, new V3(0, 0, hd - hallDepth * 0.5f - inner - 1), 0, 1);
                var dormLength = depth - hallDepth - 14;
                Building(Buildings.Dorm, dormLength, 9, new V3(-hw + 4.5f + inner, 0, -hd + 9 + dormLength * 0.5f), -90, 2);
                Building(Buildings.Dorm, dormLength, 9, new V3(hw - 4.5f - inner, 0, -hd + 9 + dormLength * 0.5f), 90, 3);
            }
            else
            {
                // The main house across the back; wings down the sides; a front row for the rich.
                var mainDepth = Math.Min(9.5f, depth * 0.36f);
                Building(houseStyle, width - 2 * inner - 1, mainDepth, new V3(0, 0, hd - mainDepth * 0.5f - inner), 0, 1);
                var wingDepth = Math.Min(6.5f, width * 0.27f);
                var wingLength = depth - mainDepth - 2 * inner - (wealth > 0.6f ? 7 : 3.5f);
                var wingZ = hd - mainDepth - inner - wingLength * 0.5f - 0.6f;
                var wingStyle = clan == "xiong" ? Buildings.Hut : Buildings.House;

                if (wingLength > 5)
                {
                    Building(wingStyle, wingLength, wingDepth, new V3(-hw + wingDepth * 0.5f + inner, 0, wingZ), -90, 2, wealth * 0.7f);

                    if (wealth > 0.3f || rng.Chance(0.5f))
                    {
                        Building(wingStyle, wingLength, wingDepth, new V3(hw - wingDepth * 0.5f - inner, 0, wingZ), 90, 3, wealth * 0.7f);
                    }
                }

                if (wealth > 0.65f && depth > 22)
                {
                    // The front row along the street wall, beside the gate, facing into the yard.
                    var rowLength = hw - Math.Abs(gateX) + hw * 0.5f - gateWidth;
                    var rowX = gateX > 0 ? -hw + inner + rowLength * 0.5f : hw - inner - rowLength * 0.5f;
                    Building(Buildings.House, rowLength, 5.5f, new V3(rowX, 0, -hd + 2.75f + inner), 180, 4, wealth * 0.6f);
                }

                if (wealth > 0.45f)
                {
                    // The screen wall facing the gate inside, so no one looks straight in.
                    var screen = FortGen.Wall(Ramparts.YardPlaster, seed * 3, 3.6f, flat, fortParams);
                    pieces.Add((screen.Lods, new V3(gateX, 0, -hd + 3.2f), 0, screen.Collision));
                }
            }

            // The yard: paved, its things.
            var yard = new TreeLod[4];
            var things = new List<(string kind, V3 at, float yaw)>();

            if (manor)
            {
                // The branch's banners either side of the gate inside; its training yard: posts to strike, racks, (for the Xiong) hides.
                var front = -hd + 6.5f + inner + 2.5f;
                things.Add(("banner_pole", new V3(-gateWidth * 0.5f - 0.8f, 0, front - 1.2f), 0));
                things.Add(("banner_pole", new V3(gateWidth * 0.5f + 0.8f, 0, front - 1.2f), 0));
                things.Add(("dummy", new V3(-4.5f, 0, front + 4), 180));
                things.Add(("dummy", new V3(-1.5f, 0, front + 4), 180));
                things.Add(("dummy", new V3(1.5f, 0, front + 4), 180));
                things.Add(("dummy", new V3(4.5f, 0, front + 4), 180));
                things.Add(("weapon_rack", new V3(-hw + 9.5f, 0, front + 1), -90));
                things.Add(("weapon_rack", new V3(hw - 9.5f, 0, front + 1), 90));
                things.Add(("lantern_post", new V3(-3.2f, 0, hd - 13 - 3), 0));
                things.Add(("lantern_post", new V3(3.2f, 0, hd - 13 - 3), 0));
                things.Add(("jars", new V3(-hw + 3, 0, front - 0.5f), 0));
                things.Add(("well", new V3(-hw + 4.5f, 0, front + 5.5f), 0));

                if (clan == "xiong")
                {
                    things.Add(("hide_rack", new V3(hw - 4.5f, 0, front + 6), 90));
                }
            }
            else if (academy)
            {
                things.Add(("banner_pole", new V3(-6, 0, -hd + 7), 0));
                things.Add(("banner_pole", new V3(6, 0, -hd + 7), 0));
                things.Add(("weapon_rack", new V3(-hw + 11, 0, 0), -90));
                things.Add(("weapon_rack", new V3(hw - 11, 0, 4), 90));
                things.Add(("dummy", new V3(-6, 0, 2), 180));
                things.Add(("dummy", new V3(-2, 0, 2), 180));
                things.Add(("dummy", new V3(2, 0, 2), 180));
                things.Add(("dummy", new V3(6, 0, 2), 180));
                things.Add(("lantern_post", new V3(-4, 0, hd - 22), 0));
                things.Add(("lantern_post", new V3(4, 0, hd - 22), 0));
            }
            else
            {
                var cx = -gateX * 0.4f;
                things.Add((rng.Chance(0.5f) || clan == "xiong" ? "well" : "jars", new V3(cx, 0, 0.5f), rng.Range(0f, 90f)));
                things.Add(("jars", new V3(hw - 2.6f, 0, hd - 9.5f - rng.Range(0f, 2f)), 90));
                things.Add(("woodpile", new V3(-hw + 3.4f, 0, -hd + 2.2f), 180));

                if (wealth < 0.6f)
                {
                    things.Add(("washing", new V3(cx + 3.5f, 0, -1.5f), 90));
                }

                if (clan == "xiong")
                {
                    things.Add(("hide_rack", new V3(cx - 4, 0, -2.5f), 20));
                }
            }

            for (var lod = 0; lod < 4; lod++)
            {
                yard[lod] = new TreeLod();
                var stone = yard[lod].Part("tex:stone");
                var paving = academy || manor || wealth > 0.4f;

                // The yard's floor: flags for the rich and the academy, beaten earth otherwise.
                var mesh = paving ? stone : yard[lod].Part("tex:mud");
                var tint = paving ? new Tint(0.85f, 0.85f, 0.85f) : new Tint(0.9f, 0.85f, 0.75f);
                var y = 0.03f;
                var start = mesh.VertexCount;
                var size = paving ? 2.4f : 2.6f;

                foreach (var (x, z) in new[] { (-hw + 0.3f, -hd + 0.3f), (hw - 0.3f, -hd + 0.3f), (hw - 0.3f, hd - 0.3f), (-hw + 0.3f, hd - 0.3f) })
                {
                    var v = mesh.Vertex(x, y, z, x / size, z / size, tint.R, tint.G, tint.B, 1);
                    mesh.SetNormal(v, 0, 1, 0);
                }

                mesh.Triangle(start, start + 3, start + 2);
                mesh.Triangle(start, start + 2, start + 1);

                if (lod <= 1)
                {
                    foreach (var (kind, at, yaw) in things)
                    {
                        var thing = ThingGen.Make(kind, seed + things.IndexOf((kind, at, yaw)), fortParams);
                        Compose.Add(yard[lod], thing.Lods[lod], at, yaw);
                    }
                }
            }

            // A tree in a corner of the yard: a plum, a camphor for the academy, a maple for the Xiong.
            {
                var species = academy ? Trees.Camphor : clan == "xiong" ? Trees.Maple : clan == "bai" ? Trees.Bamboo : Trees.Plum;
                var tree = TreeGen.Grow(species, seed, new TreeParams { Height = academy ? 22 : manor ? 12 : clan == "xiong" ? 11 : clan == "bai" ? 9 : 7.5f, Crowding = 0.2f, Age = 0.6f, Wet = 0.2f });
                var spot = academy ? new V3(-hw + 9, 0, hd - 20) : manor ? new V3(hw - 4.5f, 0, -hd + 6.5f + 4.5f) : new V3(gateX > 0 ? -hw + 3.5f : hw - 3.5f, 0, -hd + 4.5f);

                for (var lod = 0; lod < 4; lod++)
                {
                    Compose.Add(yard[lod], tree.Lods[lod], spot, rng.Range(0f, 360f));
                }
            }

            pieces.Add((yard, new V3(0, 0, 0), 0, null));

            // Put it all together, on its terrace if the ground falls away.
            var lift = Math.Max(0, asked.Terrace);
            var bottom = -0.3f;

            if (asked.Ground != null)
            {
                // On the mountain: the whole yard on a stone terrace a little over the highest ground of its lot.
                var high = float.MinValue;
                var low = float.MaxValue;

                for (var i = 0; i <= 6; i++)
                {
                    for (var j = 0; j <= 6; j++)
                    {
                        var g = asked.Ground(-hw + width * i / 6, -hd + depth * j / 6);
                        high = Math.Max(high, g);
                        low = Math.Min(low, g);
                    }
                }

                lift = high + 0.25f;
                bottom = low - 0.5f;
            }
            var solid = new MeshData();

            for (var lod = 0; lod < 4; lod++)
            {
                var into = new TreeLod();

                foreach (var (lods, at, yaw, _) in pieces)
                {
                    Compose.Add(into, lods[lod], new V3(at.X, at.Y + lift, at.Z), yaw);
                }

                if (lift - bottom > 0.4f)
                {
                    // The terrace: a stone platform under the whole yard, steps up to the gate.
                    var terrace = BuildingGen.Platform(seed, lod, hw + 0.4f, hd + 0.4f, lift, gateX, gateWidth, bottom);
                    Compose.Add(into, terrace, new V3(0, 0, 0), 0);
                }

                model.Lods[lod] = into;
            }

            foreach (var (_, at, yaw, piece) in pieces)
            {
                if (piece != null)
                {
                    Compose.Add(solid, piece, new V3(at.X, at.Y + lift, at.Z), yaw);
                }
            }

            model.Collision = solid;
            model.Height += lift;

            return model;
        }

    }
}
