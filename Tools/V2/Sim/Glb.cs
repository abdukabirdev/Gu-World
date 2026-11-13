using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using GuWorld.Core.Gen;

/// <summary>
/// Writes generated models as binary glTF (.glb) for the viewer page and for Blender: positions,
/// normals, texture coordinates and vertex colours, a material per part (a texture by its file's
/// path beside the .glb, alpha cut-out, two-sided, glowing), and for bodies a skin too. The
/// engine's left-handed axes are turned to glTF's right-handed ones by mirroring X (as Unity's
/// importer undoes), so a model looks the same in both; the triangles' winding is turned with it,
/// and V is flipped (the engine's V runs up the picture, glTF's down).
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
        /// <summary>The colour texture's path relative to the .glb (null: none), and its normal map's.</summary>
        public string Texture, Normal;
        /// <summary>Alpha cut out at a half; drawn from both sides.</summary>
        public bool Mask, TwoSided;
        /// <summary>Whether the vertices' colours are written (they multiply the texture).</summary>
        public bool Colours = true;
        /// <summary>A glow of this colour (null: none).</summary>
        public float[] Emissive;
        public float Roughness = 0.9f;
        /// <summary>The vertices' colours, in place of the mesh's own (r, g, b per vertex, 0-1, as the eye sees them).</summary>
        public float[] Tints;
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
        var materials = new List<string>();
        var materialOf = new Dictionary<string, int>();
        var images = new List<string>();
        var textures = new List<string>();
        var textureOf = new Dictionary<string, int>();
        var inv = System.Globalization.CultureInfo.InvariantCulture;

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

        int Accessor(int view, int componentType, int count, string type, string extra = "")
        {
            accessors.Add($"{{\"bufferView\":{view},\"componentType\":{componentType},\"count\":{count},\"type\":\"{type}\"{extra}}}");

            return accessors.Count - 1;
        }

        byte[] Floats(float[] values)
        {
            var bytes = new byte[values.Length * 4];
            Buffer.BlockCopy(values, 0, bytes, 0, bytes.Length);

            return bytes;
        }

        int Texture(string uri)
        {
            if (!textureOf.TryGetValue(uri, out var index))
            {
                images.Add($"{{\"uri\":\"{uri}\"}}");
                textures.Add($"{{\"source\":{images.Count - 1},\"sampler\":0}}");
                index = textures.Count - 1;
                textureOf[uri] = index;
            }

            return index;
        }

        int Material(Part part)
        {
            var key = $"{part.Texture}|{part.Normal}|{part.Mask}|{part.TwoSided}|{(part.Emissive != null ? string.Join(",", part.Emissive) : "")}|{part.Roughness}";

            if (materialOf.TryGetValue(key, out var index))
            {
                return index;
            }

            var json = new StringBuilder("{\"name\":\"").Append(part.Name).Append("\",\"pbrMetallicRoughness\":{\"baseColorFactor\":[1,1,1,1],\"metallicFactor\":0,\"roughnessFactor\":")
                .Append(part.Roughness.ToString(inv));

            if (part.Texture != null)
            {
                json.Append(",\"baseColorTexture\":{\"index\":").Append(Texture(part.Texture)).Append('}');
            }

            json.Append('}');

            if (part.Normal != null)
            {
                json.Append(",\"normalTexture\":{\"index\":").Append(Texture(part.Normal)).Append('}');
            }

            if (part.Mask)
            {
                json.Append(",\"alphaMode\":\"MASK\",\"alphaCutoff\":0.45");
            }

            if (part.Emissive != null)
            {
                json.Append(string.Format(inv, ",\"emissiveFactor\":[{0},{1},{2}]", part.Emissive[0], part.Emissive[1], part.Emissive[2]));
            }

            json.Append(",\"doubleSided\":").Append(part.TwoSided ? "true" : "false").Append('}');
            materials.Add(json.ToString());
            materialOf[key] = materials.Count - 1;

            return materials.Count - 1;
        }

        foreach (var part in parts)
        {
            var mesh = part.Mesh;
            var n = mesh.VertexCount;

            if (n == 0 || mesh.Triangles.Count == 0)
            {
                continue;
            }

            var positions = new float[n * 3];
            var normals = new float[n * 3];
            var uvs = new float[n * 2];
            var colours = new byte[n * 4];
            float minX = float.MaxValue, minY = float.MaxValue, minZ = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue, maxZ = float.MinValue;

            for (var i = 0; i < n; i++)
            {
                positions[i * 3] = -mesh.Positions[i * 3];
                positions[i * 3 + 1] = mesh.Positions[i * 3 + 1];
                positions[i * 3 + 2] = mesh.Positions[i * 3 + 2];
                normals[i * 3] = -mesh.Normals[i * 3];
                normals[i * 3 + 1] = mesh.Normals[i * 3 + 1];
                normals[i * 3 + 2] = mesh.Normals[i * 3 + 2];
                uvs[i * 2] = mesh.Uvs[i * 2];
                uvs[i * 2 + 1] = 1 - mesh.Uvs[i * 2 + 1];
                minX = Math.Min(minX, positions[i * 3]);
                minY = Math.Min(minY, positions[i * 3 + 1]);
                minZ = Math.Min(minZ, positions[i * 3 + 2]);
                maxX = Math.Max(maxX, positions[i * 3]);
                maxY = Math.Max(maxY, positions[i * 3 + 1]);
                maxZ = Math.Max(maxZ, positions[i * 3 + 2]);

                for (var k = 0; k < 4; k++)
                {
                    // Linear, as glTF's vertex colours are; a byte each, normalised.
                    var c = k == 3 ? 1 : part.Tints != null ? part.Tints[i * 3 + k] : mesh.Colours[i * 4 + k];
                    colours[i * 4 + k] = (byte)Math.Round(255 * (k == 3 ? 1 : Math.Pow(Math.Clamp(c, 0, 1), 2.2)));
                }
            }

            var indices = new List<byte>(mesh.Triangles.Count * 4);
            var small = n < 65536;

            void Index(int i)
            {
                if (small)
                {
                    indices.AddRange(BitConverter.GetBytes((ushort)i));
                }
                else
                {
                    indices.AddRange(BitConverter.GetBytes((uint)i));
                }
            }

            for (var t = 0; t < mesh.Triangles.Count; t += 3)
            {
                // Mirrored: the winding turns.
                Index(mesh.Triangles[t]);
                Index(mesh.Triangles[t + 2]);
                Index(mesh.Triangles[t + 1]);
            }

            var pa = Accessor(View(Floats(positions), 34962), 5126, n, "VEC3", string.Format(inv, ",\"min\":[{0},{1},{2}],\"max\":[{3},{4},{5}]", minX, minY, minZ, maxX, maxY, maxZ));
            var na = Accessor(View(Floats(normals), 34962), 5126, n, "VEC3");
            var attributes = $"\"POSITION\":{pa},\"NORMAL\":{na}";

            if (part.Texture != null)
            {
                attributes += $",\"TEXCOORD_0\":{Accessor(View(Floats(uvs), 34962), 5126, n, "VEC2")}";
            }

            if (part.Colours)
            {
                attributes += $",\"COLOR_0\":{Accessor(View(colours, 34962), 5121, n, "VEC4", ",\"normalized\":true")}";
            }

            var ia = Accessor(View(indices.ToArray(), 34963), small ? 5123 : 5125, mesh.Triangles.Count, "SCALAR");

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

            meshes.Add($"{{\"name\":\"{part.Name}\",\"primitives\":[{{\"attributes\":{{{attributes}}},\"indices\":{ia},\"material\":{Material(part)}}}]}}");

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

            var ib = Accessor(View(Floats(inverse.ToArray()), 0), 5126, skin.Names.Length, "MAT4");
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
            $",\"materials\":[{string.Join(",", materials)}]" +
            (textures.Count > 0 ? $",\"textures\":[{string.Join(",", textures)}],\"images\":[{string.Join(",", images)}],\"samplers\":[{{\"magFilter\":9729,\"minFilter\":9987,\"wrapS\":10497,\"wrapT\":10497}}]" : "") +
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
