using System.Collections.Generic;
using System.IO;
using GuWorld.Actors;
using GuWorld.Core.Gen;
using GuWorld.Core.Gen.Buildings;
using GuWorld.Core.Gen.Flora;
using GuWorld.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace GuWorld.Editor
{
    /// <summary>
    /// Bakes what the generators make into ordinary assets of the project, so it can be seen,
    /// placed and edited in the editor: prefabs (with their meshes and materials as assets) of
    /// buildings, trees, the rings' pieces and the outskirts' things, in Assets/GuWorld/Generated,
    /// and a showcase scene with all of them in rows.
    ///
    /// The game itself does not need any of this: it raises everything from the generators while it
    /// runs. The baked files are made on each machine from the menu (Gu World → Generators → …) and
    /// are not kept in git (they are large and can always be made again).
    /// </summary>
    public static class GeneratedBaker
    {
        const string Root = "Assets/GuWorld/Generated";
        static readonly Dictionary<Material, Material> savedMaterials = new Dictionary<Material, Material>();

        [MenuItem("Gu World/Generators/Bake everything and open the showcase", priority = 60)]
        public static void BakeAll()
        {
            try
            {
                Buildings_();
                Trees();
                Rings();
                Things();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            Showcase();
        }

        [MenuItem("Gu World/Generators/Bake buildings", priority = 61)]
        public static void BakeBuildings() => Run(Buildings_);

        [MenuItem("Gu World/Generators/Bake trees", priority = 62)]
        public static void BakeTrees() => Run(Trees);

        [MenuItem("Gu World/Generators/Bake walls, towers and gates", priority = 63)]
        public static void BakeRings() => Run(Rings);

        [MenuItem("Gu World/Generators/Bake the outskirts' things", priority = 64)]
        public static void BakeThings() => Run(Things);

        [MenuItem("Gu World/Generators/Open the showcase scene", priority = 70)]
        public static void Showcase()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(120, 1, 120);
            ground.transform.position = new Vector3(400, 0, 300);
            var z = 0f;

            foreach (var group in new[] { "Buildings", "Rings", "Things", "Trees" })
            {
                var folder = $"{Root}/{group}";

                if (!AssetDatabase.IsValidFolder(folder))
                {
                    continue;
                }

                var parent = new GameObject(group).transform;
                var x = 0f;
                var row = 0f;

                foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { folder }))
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                    var made = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                    var size = Size(made);

                    if (x > 700)
                    {
                        x = 0;
                        z += row + 8;
                        row = 0;
                    }

                    made.transform.position = new Vector3(x + size.x * 0.5f, 0, z + size.z * 0.5f);
                    // Their fronts (-Z) toward the rows' start, where the camera looks from.
                    x += size.x + 6;
                    row = Mathf.Max(row, size.z);
                }

                z += row + 30;
            }

            Directory.CreateDirectory(Root);
            EditorSceneManager.SaveScene(scene, $"{Root}/Showcase.unity");
        }

        [MenuItem("Gu World/Generators/Delete the baked files", priority = 80)]
        public static void Delete()
        {
            if (AssetDatabase.IsValidFolder(Root))
            {
                AssetDatabase.DeleteAsset(Root);
            }
        }

        static void Run(System.Action bake)
        {
            try
            {
                bake();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        static Vector3 Size(GameObject made)
        {
            var bounds = new Bounds(made.transform.position, Vector3.one);

            foreach (var renderer in made.GetComponentsInChildren<Renderer>())
            {
                bounds.Encapsulate(renderer.bounds);
            }

            return bounds.size;
        }

        // ------------------------------------------------------------------ What is baked

        static void Buildings_()
        {
            var clans = new[] { "gu_yue", "xiong", "bai" };
            var all = Core.Gen.Buildings.Buildings.All;

            for (var i = 0; i < all.Length; i++)
            {
                for (var seed = 1; seed <= 3; seed++)
                {
                    var style = all[i];
                    var clan = clans[seed - 1];
                    Progress("Buildings", $"{style.Name} {seed}", (i * 3 + seed) / (float)(all.Length * 3));
                    var made = GenBuilding.Make(GenBuilding.Name(style.Id, seed, clan, 3 + 2 * seed, seed), null, Vector3.zero, 0);
                    Save(made, "Buildings", $"{style.Id}_{seed}_{clan}");
                }
            }
        }

        static void Trees()
        {
            var all = Core.Gen.Flora.Trees.All;

            for (var i = 0; i < all.Length; i++)
            {
                for (var seed = 1; seed <= 3; seed++)
                {
                    var species = all[i];
                    Progress("Trees", $"{species.Name} {seed}", (i * 3 + seed) / (float)(all.Length * 3));
                    var share = seed == 1 ? 0.35f : seed == 2 ? 0.7f : 1f;
                    var tree = TreeGen.Grow(species, seed, new TreeParams { Height = species.MinHeight + (species.MaxHeight - species.MinHeight) * share, Lean = seed == 3 ? 12 : 0, Crowding = 0.3f, Age = 0.4f + 0.25f * seed, Wet = 0.3f });
                    var root = new GameObject($"{species.Id}_{seed}");
                    var lods = new LOD[tree.Lods.Length];
                    var heights = new[] { 0.3f, 0.1f, 0.03f, 0.004f };

                    for (var lod = 0; lod < tree.Lods.Length; lod++)
                    {
                        var mesh = Forest.MeshOf(tree.Lods[lod], $"{species.Id}_{seed} LOD{lod}", out var parts);
                        var mats = new Material[parts.Count];

                        for (var k = 0; k < parts.Count; k++)
                        {
                            mats[k] = Forest.MaterialFor(species, parts[k]);
                        }

                        var child = new GameObject("LOD" + lod);
                        child.transform.SetParent(root.transform, false);
                        child.AddComponent<MeshFilter>().sharedMesh = mesh;
                        var renderer = child.AddComponent<MeshRenderer>();
                        renderer.sharedMaterials = mats;
                        renderer.shadowCastingMode = lod == 3 ? ShadowCastingMode.Off : ShadowCastingMode.On;
                        lods[lod] = new LOD(heights[Mathf.Min(lod, 3)], new Renderer[] { renderer });
                    }

                    root.AddComponent<LODGroup>().SetLODs(lods);
                    root.AddComponent<MeshCollider>().sharedMesh = GenBuilding.Plain(tree.Collision, $"{species.Id}_{seed} solid");
                    var solid = root.AddComponent<TreeSolid>();
                    solid.Species = species;
                    solid.Height = tree.Height;
                    Save(root, "Trees", $"{species.Id}_{seed}_{tree.Height:0}m");
                }
            }
        }

        static void Rings()
        {
            var n = 0;

            foreach (var style in Ramparts.All)
            {
                var clan = style.Kind == RampartKind.Plaster ? "bai" : style.Kind == RampartKind.Earth ? "xiong" : "gu_yue";

                foreach (var what in new[] { "wall", "tower", "gate" })
                {
                    if (what == "tower" && (style.Kind == RampartKind.Bamboo || style.Kind == RampartKind.Plaster))
                    {
                        continue;
                    }

                    Progress("Walls, towers and gates", $"{style.Name} {what}", n++ / 13f);
                    var p = new FortParams { Clan = clan, Wealth = 0.6f, Wear = 0.3f };
                    var model = what == "wall" ? FortGen.Wall(style, 1, style.Piece, (x, z) => 0, p) : what == "tower" ? FortGen.Tower(style, 1, (x, z) => 0, p) : FortGen.Gate(style, 1, (x, z) => 0, p);
                    var root = Raise(model, $"{style.Id}_{what}");

                    if (!string.IsNullOrEmpty(model.TopModel))
                    {
                        var top = GenBuilding.Make($"{GenBuilding.Prefix}{model.TopModel}:1:{clan}:6:3:{model.TopWidth.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)}:{model.TopDepth.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)}", root.transform, Vector3.zero, 0);
                        top.transform.localPosition = new Vector3(0, model.TopY, 0);
                        top.transform.localRotation = Quaternion.Euler(0, 180, 0);
                    }

                    Save(root, "Rings", $"{style.Id}_{what}");
                }
            }
        }

        static void Things()
        {
            var kinds = ThingGen.Kinds;

            for (var i = 0; i < kinds.Length; i++)
            {
                for (var seed = 1; seed <= 2; seed++)
                {
                    Progress("Things", kinds[i], (i * 2 + seed) / (float)(kinds.Length * 2));
                    var model = ThingGen.Make(kinds[i], seed, new FortParams { Clan = seed == 1 ? "gu_yue" : "xiong", Wealth = 0.6f, Wear = 0.3f });
                    Save(Raise(model, $"{kinds[i]}_{seed}"), "Things", $"{kinds[i]}_{seed}");
                }
            }
        }

        /// <summary>A ring's piece or a thing as a GameObject: four levels of detail and a collider.</summary>
        static GameObject Raise(FortModel model, string name)
        {
            var root = new GameObject(name);
            var lods = new LOD[4];
            var heights = new[] { 0.25f, 0.08f, 0.02f, 0.003f };

            for (var i = 0; i < 4; i++)
            {
                var child = new GameObject("LOD" + i);
                child.transform.SetParent(root.transform, false);
                child.AddComponent<MeshFilter>().sharedMesh = GenBuilding.MeshOf(model.Lods[i], name + " LOD" + i, out var mats);
                var renderer = child.AddComponent<MeshRenderer>();
                renderer.sharedMaterials = mats;
                lods[i] = new LOD(heights[i], new Renderer[] { renderer });
            }

            root.AddComponent<LODGroup>().SetLODs(lods);

            if (model.Collision.VertexCount > 0)
            {
                root.AddComponent<MeshCollider>().sharedMesh = GenBuilding.Plain(model.Collision, name + " solid");
            }

            return root;
        }

        // ------------------------------------------------------------------ Saving

        static void Progress(string what, string item, float share) => EditorUtility.DisplayProgressBar("Gu World: baking " + what, item, share);

        static string Folder(string group)
        {
            foreach (var path in new[] { "Assets/GuWorld", Root, $"{Root}/{group}", $"{Root}/{group}/Meshes", $"{Root}/Materials" })
            {
                if (!AssetDatabase.IsValidFolder(path))
                {
                    var parent = Path.GetDirectoryName(path).Replace('\\', '/');
                    AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
                }
            }

            return $"{Root}/{group}";
        }

        static string Clean(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }

            return name.Replace(':', '_').Replace(' ', '_');
        }

        /// <summary>Saves a made GameObject as a prefab: its meshes and materials first as assets of their own, then it; then destroys it.</summary>
        static void Save(GameObject made, string group, string name)
        {
            var folder = Folder(group);
            name = Clean(name);
            var count = 0;

            foreach (var filter in made.GetComponentsInChildren<MeshFilter>(true))
            {
                filter.sharedMesh = Keep(filter.sharedMesh, $"{folder}/Meshes/{name}_{count++}.asset");
            }

            foreach (var collider in made.GetComponentsInChildren<MeshCollider>(true))
            {
                collider.sharedMesh = Keep(collider.sharedMesh, $"{folder}/Meshes/{name}_{count++}_solid.asset");
            }

            foreach (var renderer in made.GetComponentsInChildren<Renderer>(true))
            {
                var mats = renderer.sharedMaterials;

                for (var i = 0; i < mats.Length; i++)
                {
                    mats[i] = KeepMaterial(mats[i]);
                }

                renderer.sharedMaterials = mats;
            }

            made.name = name;
            PrefabUtility.SaveAsPrefabAsset(made, $"{folder}/{name}.prefab");
            Object.DestroyImmediate(made);
        }

        static Mesh Keep(Mesh mesh, string path)
        {
            if (mesh == null || AssetDatabase.Contains(mesh))
            {
                return mesh;
            }

            if (AssetDatabase.LoadAssetAtPath<Mesh>(path) != null)
            {
                AssetDatabase.DeleteAsset(path);
            }

            AssetDatabase.CreateAsset(mesh, path);

            return mesh;
        }

        static Material KeepMaterial(Material material)
        {
            if (material == null || AssetDatabase.Contains(material))
            {
                return material;
            }

            if (savedMaterials.TryGetValue(material, out var saved) && saved != null)
            {
                return saved;
            }

            var path = $"{Root}/Materials/{Clean(material.name)}_{material.GetInstanceID() & 0xFFFF:X4}.mat";
            AssetDatabase.CreateAsset(material, path);
            savedMaterials[material] = material;

            return material;
        }
    }
}
