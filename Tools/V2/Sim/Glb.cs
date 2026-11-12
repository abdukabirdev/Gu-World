using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using GuWorld.Core.Gen;

/// <summary>
/// Writes generated models as binary glTF (.glb) for the viewer page: positions, normals and
/// vertex colours, and for bodies a skin (joints, weights, the skeleton's nodes) too. The engine's
/// left-handed axes are turned to glTF's right-handed ones by mirroring X (as Unity's importer
/// undoes), so a model looks the same in both; the triangles' winding is turned with it.
/// </summary>
static class Glb
{
    public sealed class Skin
    {
        public string[] Names;
        public int[] Parents;
        /// <summary>Each joint's place in the model at rest (engine axes), no turns.</summary>
        public V3[] Heads;
        /// <summary>Per vertex: four joints and four weights.</summary>
        public int[] Joints;
        public float[] Weights;
    }

    public sealed class Part
    {
        public string Name;
        public MeshData Mesh;
        public Skin Skin;
    }

    public static void Write(string path, IList<Part> parts, string extrasJson = null)
    {
        var bin = new MemoryStream();
        var views = new List<string>();
        var accessors = new List<string>();
        var meshes = new List<string>();
        var nodes = new List<string>();
        var skins = new List<string>();
        var sceneNodes = new List<int>();

        int View(byte[] data, int target)
        {
            while (bin.Length % 4 != 0)
            {
                bin.WriteByte(0);
            }

            var offset = (int)bin.Length;
            bin.Write(data, 0, data.Length);
            views.Add($"{{\"buffer\":0,\"byteOffset\":{offset},\"byteLength\":{data.Length}{(target > 0 ? $",\"target\":{target}" : "")}}}");

            return views.Count - 1;
        }

        int Accessor(int view, int componentType, int count, string type, string minMax = "")
        {
            accessors.Add($"{{\"bufferView\":{view},\"componentType\":{componentType},\"count\":{count},\"type\":\"{type}\"{minMax}}}");

            return accessors.Count - 1;
        }

        byte[] Floats(IEnumerable<float> values)
        {
            var list = new List<byte>();

            foreach (var v in values)
            {
                list.AddRange(BitConverter.GetBytes(v));
            }

            return list.ToArray();
        }

        foreach (var part in parts)
        {
            var mesh = part.Mesh;
            var n = mesh.VertexCount;
            var positions = new float[n * 3];
            var normals = new float[n * 3];
            var colours = new float[n * 4];
            float minX = float.MaxValue, minY = float.MaxValue, minZ = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue, maxZ = float.MinValue;

            for (var i = 0; i < n; i++)
            {
                positions[i * 3] = -mesh.Positions[i * 3];
                positions[i * 3 + 1] = mesh.Positions[i * 3 + 1];
                positions[i * 3 + 2] = mesh.Positions[i * 3 + 2];
                normals[i * 3] = -mesh.Normals[i * 3];
                normals[i * 3 + 1] = mesh.Normals[i * 3 + 1];
                normals[i * 3 + 2] = mesh.Normals[i * 3 + 2];
                minX = Math.Min(minX, positions[i * 3]);
                minY = Math.Min(minY, positions[i * 3 + 1]);
                minZ = Math.Min(minZ, positions[i * 3 + 2]);
                maxX = Math.Max(maxX, positions[i * 3]);
                maxY = Math.Max(maxY, positions[i * 3 + 1]);
                maxZ = Math.Max(maxZ, positions[i * 3 + 2]);

                for (var k = 0; k < 4; k++)
                {
                    colours[i * 4 + k] = k == 3 ? 1 : (float)Math.Pow(Math.Clamp(mesh.Colours[i * 4 + k], 0, 1), 2.2);
                }
            }

            var indices = new List<byte>();

            for (var t = 0; t < mesh.Triangles.Count; t += 3)
            {
                // Mirrored: the winding turns.
                indices.AddRange(BitConverter.GetBytes((uint)mesh.Triangles[t]));
                indices.AddRange(BitConverter.GetBytes((uint)mesh.Triangles[t + 2]));
                indices.AddRange(BitConverter.GetBytes((uint)mesh.Triangles[t + 1]));
            }

            var inv = System.Globalization.CultureInfo.InvariantCulture;
            var pa = Accessor(View(Floats(positions), 34962), 5126, n, "VEC3", string.Format(inv, ",\"min\":[{0},{1},{2}],\"max\":[{3},{4},{5}]", minX, minY, minZ, maxX, maxY, maxZ));
            var na = Accessor(View(Floats(normals), 34962), 5126, n, "VEC3");
            var ca = Accessor(View(Floats(colours), 34962), 5126, n, "VEC4");
            var ia = Accessor(View(indices.ToArray(), 34963), 5125, mesh.Triangles.Count, "SCALAR");
            var attributes = $"\"POSITION\":{pa},\"NORMAL\":{na},\"COLOR_0\":{ca}";

            if (part.Skin != null)
            {
                var joints = new byte[n * 8];

                for (var i = 0; i < n * 4; i++)
                {
                    BitConverter.GetBytes((ushort)part.Skin.Joints[i]).CopyTo(joints, i * 2);
                }

                var ja = Accessor(View(joints, 34962), 5123, n, "VEC4");
                var wa = Accessor(View(Floats(part.Skin.Weights), 34962), 5126, n, "VEC4");
                attributes += $",\"JOINTS_0\":{ja},\"WEIGHTS_0\":{wa}";
            }

            meshes.Add($"{{\"name\":\"{part.Name}\",\"primitives\":[{{\"attributes\":{{{attributes}}},\"indices\":{ia},\"material\":0}}]}}");

            if (part.Skin == null)
            {
                nodes.Add($"{{\"name\":\"{part.Name}\",\"mesh\":{meshes.Count - 1}}}");
                sceneNodes.Add(nodes.Count - 1);
                continue;
            }

            // The skeleton: a node per joint, placed by its offset from its parent (no turns at rest).
            var skin = part.Skin;
            var first = nodes.Count;
            var children = new List<int>[skin.Names.Length];

            for (var j = 0; j < skin.Names.Length; j++)
            {
                children[j] = new List<int>();
            }

            for (var j = 0; j < skin.Names.Length; j++)
            {
                if (skin.Parents[j] >= 0)
                {
                    children[skin.Parents[j]].Add(j);
                }
            }

            var inverse = new List<float>();

            for (var j = 0; j < skin.Names.Length; j++)
            {
                var head = skin.Heads[j];
                var parentHead = skin.Parents[j] >= 0 ? skin.Heads[skin.Parents[j]] : new V3(0, 0, 0);
                var local = head - parentHead;
                var kids = children[j].Count > 0 ? $",\"children\":[{string.Join(",", children[j].ConvertAll(c => (c + first).ToString()))}]" : "";
                nodes.Add(string.Format(inv, "{{\"name\":\"{0}\",\"translation\":[{1},{2},{3}]{4}}}", skin.Names[j], -local.X, local.Y, local.Z, kids));
                inverse.AddRange(new float[] { 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, head.X, -head.Y, -head.Z, 1 });
            }

            var ib = Accessor(View(Floats(inverse), 0), 5126, skin.Names.Length, "MAT4");
            var jointList = new List<string>();

            for (var j = 0; j < skin.Names.Length; j++)
            {
                jointList.Add((first + j).ToString());
            }

            skins.Add($"{{\"inverseBindMatrices\":{ib},\"joints\":[{string.Join(",", jointList)}],\"skeleton\":{first}}}");

            for (var j = 0; j < skin.Names.Length; j++)
            {
                if (skin.Parents[j] < 0)
                {
                    sceneNodes.Add(first + j);
                }
            }

            nodes.Add($"{{\"name\":\"{part.Name}\",\"mesh\":{meshes.Count - 1},\"skin\":{skins.Count - 1}}}");
            sceneNodes.Add(nodes.Count - 1);
        }

        while (bin.Length % 4 != 0)
        {
            bin.WriteByte(0);
        }

        var json = "{\"asset\":{\"version\":\"2.0\",\"generator\":\"GU World Sim\"}" +
            $",\"scene\":0,\"scenes\":[{{\"nodes\":[{string.Join(",", sceneNodes)}]{(extrasJson != null ? ",\"extras\":" + extrasJson : "")}}}]" +
            $",\"nodes\":[{string.Join(",", nodes)}],\"meshes\":[{string.Join(",", meshes)}]" +
            (skins.Count > 0 ? $",\"skins\":[{string.Join(",", skins)}]" : "") +
            ",\"materials\":[{\"pbrMetallicRoughness\":{\"baseColorFactor\":[1,1,1,1],\"metallicFactor\":0,\"roughnessFactor\":0.9},\"doubleSided\":false}]" +
            $",\"accessors\":[{string.Join(",", accessors)}],\"bufferViews\":[{string.Join(",", views)}],\"buffers\":[{{\"byteLength\":{bin.Length}}}]}}";
        var jsonBytes = Encoding.UTF8.GetBytes(json);
        var pad = (4 - jsonBytes.Length % 4) % 4;
        var binBytes = bin.ToArray();

        using (var file = new BinaryWriter(File.Create(path)))
        {
            file.Write(0x46546C67u);
            file.Write(2u);
            file.Write((uint)(12 + 8 + jsonBytes.Length + pad + 8 + binBytes.Length));
            file.Write((uint)(jsonBytes.Length + pad));
            file.Write(0x4E4F534Au);
            file.Write(jsonBytes);

            for (var i = 0; i < pad; i++)
            {
                file.Write((byte)0x20);
            }

            file.Write((uint)binBytes.Length);
            file.Write(0x004E4942u);
            file.Write(binBytes);
        }
    }
}
