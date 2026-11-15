using System.Collections.Generic;
using GuWorld.Core.Gen;
using GuWorld.Core.Gen.Buildings;
using GuWorld.Core.Gen.Flora;
using UnityEngine;
using UnityEngine.Rendering;

namespace GuWorld.Actors
{
    /// <summary>
    /// Generated buildings in the engine (Core/Gen/Buildings): a model named
    /// "gen:&lt;style&gt;:&lt;seed&gt;[:&lt;clan&gt;[:&lt;wealth 0-9&gt;[:&lt;wear 0-9&gt;]]]" (as Models.Prefab
    /// is asked for) is raised once, its four levels of detail kept as meshes, and placed with a
    /// LODGroup and a collider of its far level. The model's front faces -Z, its foot at its origin.
    /// Textures: Resources/Buildings/&lt;name&gt; (Tools/V2/models/buildtex.py), drawn with VertexLit
    /// (the texture times the vertices' colour); lit paper and lanterns with TreeGlow (unlit).
    /// </summary>
    public static class GenBuilding
    {
        public const string Prefix = "gen:";

        public static bool Is(string model) => model != null && model.StartsWith(Prefix, System.StringComparison.Ordinal);

        public static string Name(string style, int seed, string clan = "", int wealth = 5, int wear = 3) => $"{Prefix}{style}:{seed}:{clan}:{wealth}:{wear}";

        sealed class Made
        {
            public Mesh[] Lods = new Mesh[4];
            public Material[][] Materials = new Material[4][];
            public Mesh Solid;
            public float Size;
        }

        static readonly Dictionary<string, Made> made = new Dictionary<string, Made>();
        static readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();

        static BuildingModel Raise(string model)
        {
            var bits = model.Substring(Prefix.Length).Split(':');
            var style = Buildings.Get(bits[0]);
            var seed = bits.Length > 1 && int.TryParse(bits[1], out var s) ? s : 1;
            var clan = bits.Length > 2 ? bits[2] : "gu_yue";
            var wealth = bits.Length > 3 && int.TryParse(bits[3], out var w) ? w / 9f : 0.5f;
            var wear = bits.Length > 4 && int.TryParse(bits[4], out var r) ? r / 9f : 0.3f;

            return BuildingGen.Make(style, seed, new BuildingParams { Clan = clan, Wealth = wealth, Wear = wear });
        }

        static Made Get(string model)
        {
            if (made.TryGetValue(model, out var m))
            {
                return m;
            }

            var building = Raise(model);
            m = new Made { Size = Mathf.Max(building.EaveWidth, building.EaveDepth, building.Height) };

            for (var lod = 0; lod < 4; lod++)
            {
                m.Lods[lod] = MeshOf(building.Lods[lod], model + " LOD" + lod, out m.Materials[lod]);
            }

            m.Solid = Plain(building.Collision, model + " solid");
            made[model] = m;

            return m;
        }

        public static GameObject Make(string model, Transform parent, Vector3 at, float yaw, float scale = 1)
        {
            var m = Get(model);
            var root = new GameObject(model);
            root.transform.SetParent(parent, false);
            root.transform.SetPositionAndRotation(at, Quaternion.Euler(0, yaw, 0));
            root.transform.localScale = Vector3.one * scale;
            var lods = new LOD[4];
            // The share of the screen's height below which each level gives way to the next.
            var heights = new[] { 0.32f, 0.12f, 0.035f, 0.004f };

            for (var lod = 0; lod < 4; lod++)
            {
                var child = new GameObject("LOD" + lod);
                child.transform.SetParent(root.transform, false);
                child.AddComponent<MeshFilter>().sharedMesh = m.Lods[lod];
                var renderer = child.AddComponent<MeshRenderer>();
                renderer.sharedMaterials = m.Materials[lod];
                renderer.shadowCastingMode = lod == 3 ? ShadowCastingMode.Off : ShadowCastingMode.On;
                lods[lod] = new LOD(heights[lod], new Renderer[] { renderer });
            }

            var group = root.AddComponent<LODGroup>();
            group.SetLODs(lods);
            group.RecalculateBounds();
            var solid = root.AddComponent<MeshCollider>();
            solid.sharedMesh = m.Solid;

            return root;
        }

        static Mesh MeshOf(TreeLod lod, string name, out Material[] mats)
        {
            var positions = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var colours = new List<Color32>();
            var subs = new List<int[]>();
            var list = new List<Material>();

            foreach (var part in lod.Parts)
            {
                var data = part.Mesh;

                if (data.TriangleCount == 0)
                {
                    continue;
                }

                var start = positions.Count;

                for (var i = 0; i < data.VertexCount; i++)
                {
                    positions.Add(new Vector3(data.Positions[i * 3], data.Positions[i * 3 + 1], data.Positions[i * 3 + 2]));
                    normals.Add(new Vector3(data.Normals[i * 3], data.Normals[i * 3 + 1], data.Normals[i * 3 + 2]));
                    uvs.Add(new Vector2(data.Uvs[i * 2], data.Uvs[i * 2 + 1]));
                    colours.Add(new Color(data.Colours[i * 4], data.Colours[i * 4 + 1], data.Colours[i * 4 + 2], 1));
                }

                var tris = new int[data.Triangles.Count];

                for (var i = 0; i < tris.Length; i++)
                {
                    tris[i] = data.Triangles[i] + start;
                }

                subs.Add(tris);
                list.Add(MaterialFor(part.Material));
            }

            var mesh = new Mesh { name = name };

            if (positions.Count > 65000)
            {
                mesh.indexFormat = IndexFormat.UInt32;
            }

            mesh.SetVertices(positions);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetColors(colours);
            mesh.subMeshCount = subs.Count;

            for (var i = 0; i < subs.Count; i++)
            {
                mesh.SetTriangles(subs[i], i);
            }

            mesh.RecalculateBounds();
            mats = list.ToArray();

            return mesh;
        }

        static Mesh Plain(MeshData data, string name)
        {
            var mesh = new Mesh { name = name };
            var vertices = new Vector3[data.VertexCount];

            for (var i = 0; i < vertices.Length; i++)
            {
                vertices[i] = new Vector3(data.Positions[i * 3], data.Positions[i * 3 + 1], data.Positions[i * 3 + 2]);
            }

            if (vertices.Length > 65000)
            {
                mesh.indexFormat = IndexFormat.UInt32;
            }

            mesh.vertices = vertices;
            mesh.triangles = data.Triangles.ToArray();
            mesh.RecalculateBounds();

            return mesh;
        }

        static Material MaterialFor(string part)
        {
            if (materials.TryGetValue(part, out var material))
            {
                return material;
            }

            if (part == "glow")
            {
                var shader = Resources.Load<Shader>("Shaders/TreeGlow");
                material = shader != null ? new Material(shader) : new Material(Models.VertexLit(Color.white, 0));

                if (material.HasProperty("_Breath"))
                {
                    material.SetFloat("_Breath", 0.05f);
                    material.SetColor("_BaseColor", new Color(1.4f, 1.3f, 1.2f, 1));
                }
            }
            else
            {
                material = new Material(Models.VertexLit(Color.white, 0));

                if (part.StartsWith("tex:"))
                {
                    var texture = Resources.Load<Texture2D>("Buildings/" + part.Substring(4));

                    if (texture != null)
                    {
                        material.SetTexture("_BaseMap", texture);
                    }
                }
            }

            material.name = "Building " + part;
            material.enableInstancing = true;
            materials[part] = material;

            return material;
        }
    }
}
