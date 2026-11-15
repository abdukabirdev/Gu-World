using System.Collections.Generic;
using GuWorld.Core.Gen;
using GuWorld.Core.Gen.Buildings;
using GuWorld.Core.Gen.Flora;
using GuWorld.World;
using UnityEngine;
using UnityEngine.Rendering;

namespace GuWorld.Actors
{
    /// <summary>
    /// Generated buildings in the engine (Core/Gen/Buildings): a model named
    /// "gen:&lt;style&gt;:&lt;seed&gt;[:&lt;clan&gt;[:&lt;wealth 0-9&gt;[:&lt;wear 0-9&gt;[:&lt;width m&gt;[:&lt;depth m&gt;]]]]]" (as Models.Prefab
    /// is asked for) is raised once, its four levels of detail kept as meshes, and placed with a
    /// LODGroup and a collider of its far level. Its front faces +Z (as the game's places want; the generator's
    /// -Z front is turned inside), its foot at its origin.
    /// Textures: Resources/Buildings/&lt;name&gt; (Tools/V2/models/buildtex.py), drawn with VertexLit
    /// (the texture times the vertices' colour); lit paper and lanterns with TreeGlow (unlit).
    /// </summary>
    public static class GenBuilding
    {
        public const string Prefix = "gen:";

        public static bool Is(string model) => model != null && (model.StartsWith(Prefix, System.StringComparison.Ordinal) || model.StartsWith(Hill, System.StringComparison.Ordinal));

        /// <summary>A house of the mountain: as "gen:" but raised where it stands, down to the ground under it (each its own meshes); far, the level one stands in.</summary>
        public const string Hill = "hill:";

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

        static BuildingModel Raise(string model, System.Func<float, float, float> ground = null)
        {
            var bits = model.Substring(model.IndexOf(':') + 1).Split(':');
            var style = Buildings.Get(bits[0]);
            var seed = bits.Length > 1 && int.TryParse(bits[1], out var s) ? s : 1;
            var clan = bits.Length > 2 ? bits[2] : "gu_yue";
            var wealth = bits.Length > 3 && int.TryParse(bits[3], out var w) ? w / 9f : 0.5f;
            var wear = bits.Length > 4 && int.TryParse(bits[4], out var r) ? r / 9f : 0.3f;

            var width = bits.Length > 5 && float.TryParse(bits[5], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var wd) ? wd : 0;
            var depth = bits.Length > 6 && float.TryParse(bits[6], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var dp) ? dp : 0;

            return BuildingGen.Make(style, seed, new BuildingParams { Clan = clan, Wealth = wealth, Wear = wear, Width = width, Depth = depth, Ground = ground });
        }

        static Made Get(string model)
        {
            // The level stand-in of a house of the mountain (for far batches and caches).
            if (model.StartsWith(Hill, System.StringComparison.Ordinal))
            {
                model = Prefix + model.Substring(Hill.Length);
            }

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

        /// <summary>The far level of a model (for drawing it in batches from afar): its mesh and a material per sub-mesh.</summary>
        public static Mesh Far(string model, out Material[] mats)
        {
            var m = Get(model);
            mats = m.Materials[2];

            return m.Lods[2];
        }

        public static GameObject Make(string model, Transform parent, Vector3 at, float yaw, float scale = 1)
        {
            var root = new GameObject(model);
            root.transform.SetParent(parent, false);
            root.transform.SetPositionAndRotation(at, Quaternion.Euler(0, yaw, 0));
            root.transform.localScale = Vector3.one * scale;
            // The generator makes a building's front toward -Z; the game's places (as its hand-made
            // models did) want it toward +Z: the model is turned about inside its root.
            var inner = new GameObject("Model").transform;
            inner.SetParent(root.transform, false);
            inner.localRotation = Quaternion.Euler(0, 180, 0);
            Made m;

            if (model.StartsWith(Hill, System.StringComparison.Ordinal))
            {
                // Raised against the ground where it stands (its own meshes, not shared).
                var foot = at.y;
                var building = Raise(model, (x, z) =>
                {
                    var w = inner.TransformPoint(new Vector3(x, 0, z));

                    return GuWorld.World.Ground.HeightAt(w.x, w.z) - foot;
                });
                m = new Made();

                for (var lod = 0; lod < 4; lod++)
                {
                    m.Lods[lod] = MeshOf(building.Lods[lod], model + " LOD" + lod, out m.Materials[lod]);
                }

                m.Solid = Plain(building.Collision, model + " solid");
            }
            else
            {
                m = Get(model);
            }
            var lods = new LOD[4];
            // The share of the screen's height below which each level gives way to the next.
            var heights = new[] { 0.32f, 0.12f, 0.035f, 0.004f };

            for (var lod = 0; lod < 4; lod++)
            {
                var child = new GameObject("LOD" + lod);
                child.transform.SetParent(inner, false);
                child.AddComponent<MeshFilter>().sharedMesh = m.Lods[lod];
                var renderer = child.AddComponent<MeshRenderer>();
                renderer.sharedMaterials = m.Materials[lod];
                renderer.shadowCastingMode = lod == 3 ? ShadowCastingMode.Off : ShadowCastingMode.On;
                lods[lod] = new LOD(heights[lod], new Renderer[] { renderer });
            }

            var group = root.AddComponent<LODGroup>();
            group.SetLODs(lods);
            group.RecalculateBounds();
            inner.gameObject.AddComponent<MeshCollider>().sharedMesh = m.Solid;

            return root;
        }

        /// <summary>The turn inside a generated building's root (see Make), for drawing its far level in batches.</summary>
        public static readonly Matrix4x4 Turn = Matrix4x4.Rotate(Quaternion.Euler(0, 180, 0));

        public static Mesh MeshOf(TreeLod lod, string name, out Material[] mats)
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

        public static Mesh Plain(MeshData data, string name)
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

            if (part.StartsWith("bark:") || part.StartsWith("leaf:") || part == "extra")
            {
                // A tree in a yard: drawn as the forest draws its kind.
                var name = part.Contains(":") ? part.Substring(5) : "";
                var species = Trees.Plum;

                foreach (var kind in Trees.All)
                {
                    if (kind.Bark == name || kind.Leaf == name || kind.Leaf2 == name)
                    {
                        species = kind;
                        break;
                    }
                }

                material = Forest.MaterialFor(species, part);
                materials[part] = material;

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
