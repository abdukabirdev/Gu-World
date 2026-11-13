using System.Collections.Generic;
using GuWorld.Actors;
using GuWorld.Core;
using GuWorld.Core.Gen;
using GuWorld.Core.Gen.Flora;
using UnityEngine;
using UnityEngine.Rendering;

namespace GuWorld.World
{
    /// <summary>A grown tree's solid shape in the world: what the hero stands on and climbs (see Hero's climbing).</summary>
    public class TreeSolid : MonoBehaviour
    {
        public TreeSpecies Species;
        public float Height;
    }

    /// <summary>
    /// The woods, groves and rocks of the plan's scatter, round the hero. Every tree is grown by the
    /// generator (Core/Gen/Flora): variants of each kind — open and crowded, straight and leaning,
    /// a few seeds — are grown on a worker thread as the hero comes near woods that need them, and
    /// each tree of the map wears the variant nearest its asking, turned so it leans where its wind
    /// and slope push it and scaled to its height.
    ///
    /// A variant is several materials: its bark (with moss), its foliage (cut out), what lives on
    /// it. Each level of detail is one mesh with a sub-mesh per material, drawn in instanced
    /// batches; the level is picked by distance. Near the hero every tree also has a solid — its
    /// trunk, roots and thick limbs as a mesh collider — to walk on and climb. Boulders, shrubs
    /// and reeds are shapes made in code, drawn the same way.
    /// </summary>
    public class Forest : MonoBehaviour
    {
        WorldPlan plan;
        float near, far;
        float[] lodReach;
        float wait;
        Vector2Int lastCell = new Vector2Int(int.MinValue, int.MinValue);
        Vector3 lastFocus;

        // The plan's cells as worked out (shared with the worker thread).
        readonly Dictionary<long, List<Instance>> cells = new Dictionary<long, List<Instance>>();
        readonly HashSet<long> asked = new HashSet<long>();
        readonly Queue<(int x, int z)> toWork = new Queue<(int, int)>();
        System.Threading.Thread worker, grower;
        volatile bool stopping;

        // The trees' variants: asked for by the main thread, grown on their own thread, then turned into the engine's meshes a level a frame.
        TreeLibrary library;
        readonly Queue<int> toGrow = new Queue<int>();
        readonly HashSet<int> wanted = new HashSet<int>();
        readonly Queue<(int key, TreeModel model)> grownNow = new Queue<(int, TreeModel)>();

        sealed class Variant
        {
            public TreeModel Model;
            public Mesh[] Lods;
            public Material[][] Materials;
            public Mesh Solid;
            public int Uploaded;
            public float Height, TrunkRadius;
            public bool Ready => Uploaded >= 4;
        }

        readonly Dictionary<int, Variant> variants = new Dictionary<int, Variant>();
        readonly List<Variant> uploading = new List<Variant>();

        // Near: solids kept by cell.
        readonly Dictionary<long, List<GameObject>> placed = new Dictionary<long, List<GameObject>>();
        readonly Dictionary<int, Stack<GameObject>> solids = new Dictionary<int, Stack<GameObject>>();
        readonly Dictionary<string, Stack<GameObject>> rocks = new Dictionary<string, Stack<GameObject>>();

        // Everything drawn: batches of matrices by mesh.
        class Batch
        {
            public Mesh Mesh;
            public Material[] Materials;
            public ShadowCastingMode Shadows;
            public readonly List<Matrix4x4> Matrices = new List<Matrix4x4>();
        }

        readonly Dictionary<long, Batch> batches = new Dictionary<long, Batch>();
        readonly Dictionary<int, Mesh> shapes = new Dictionary<int, Mesh>();
        readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
        Material vertexLit, swaying;
        readonly Matrix4x4[] chunk = new Matrix4x4[1023];

        public static int Drawn { get; private set; }
        public static string Count() => $"{Drawn} trees and shapes drawn; tree variants ready {(instance != null ? instance.ReadyCount() : 0)}";
        static Forest instance;

        static long Key(int x, int z) => ((long)x << 32) ^ (uint)z;

        public static Forest Make(Transform parent, WorldPlan plan)
        {
            var root = new GameObject("Forest");
            root.transform.SetParent(parent, false);
            var forest = root.AddComponent<Forest>();
            instance = forest;
            forest.plan = plan;
            forest.near = Quality.Level >= 2 ? 260 : Quality.Level == 1 ? 180 : 120;
            forest.far = Quality.Level >= 2 ? 1500 : Quality.Level == 1 ? 900 : 500;
            // Where each level of detail gives way to the next.
            forest.lodReach = Quality.Level >= 2 ? new[] { 130f, 320f, 700f } : Quality.Level == 1 ? new[] { 80f, 200f, 420f } : new[] { 45f, 120f, 260f };
            forest.vertexLit = Models.VertexLit(Color.white, 0);
            forest.swaying = Models.VertexLit(Color.white, 0.04f);
            forest.library = new TreeLibrary(Quality.Level >= 2 ? 3 : Quality.Level == 1 ? 2 : 1);
            forest.worker = new System.Threading.Thread(forest.Work) { IsBackground = true, Name = "GU World forest" };
            forest.worker.Start();
            forest.grower = new System.Threading.Thread(forest.GrowTrees) { IsBackground = true, Name = "GU World trees", Priority = System.Threading.ThreadPriority.BelowNormal };
            forest.grower.Start();

            return forest;
        }

        void OnDestroy()
        {
            stopping = true;

            lock (toWork)
            {
                System.Threading.Monitor.PulseAll(toWork);
            }

            lock (toGrow)
            {
                System.Threading.Monitor.PulseAll(toGrow);
            }

            if (instance == this)
            {
                instance = null;
            }
        }

        int ReadyCount()
        {
            var n = 0;

            foreach (var variant in variants.Values)
            {
                n += variant.Ready ? 1 : 0;
            }

            return n;
        }

        // ------------------------------------------------------------------ The workers

        void Work()
        {
            while (!stopping)
            {
                (int x, int z) next;

                lock (toWork)
                {
                    while (toWork.Count == 0 && !stopping)
                    {
                        System.Threading.Monitor.Wait(toWork);
                    }

                    if (stopping)
                    {
                        return;
                    }

                    next = toWork.Dequeue();
                }

                List<Instance> made;

                try
                {
                    made = plan.Scatter.Cell(next.x, next.z);
                }
                catch (System.Exception error)
                {
                    Debug.LogError($"GU World: forest cell {next.x},{next.z} failed: {error}");
                    made = new List<Instance>();
                }

                lock (cells)
                {
                    cells[Key(next.x, next.z)] = made;
                }
            }
        }

        /// <summary>Grows the variants the main thread asks for, in the order asked (nearest woods first).</summary>
        void GrowTrees()
        {
            while (!stopping)
            {
                int key;

                lock (toGrow)
                {
                    while (toGrow.Count == 0 && !stopping)
                    {
                        System.Threading.Monitor.Wait(toGrow);
                    }

                    if (stopping)
                    {
                        return;
                    }

                    key = toGrow.Dequeue();
                }

                try
                {
                    var model = library.Get(key / 64, key % 64);

                    lock (grownNow)
                    {
                        grownNow.Enqueue((key, model));
                    }
                }
                catch (System.Exception error)
                {
                    Debug.LogError($"GU World: growing tree {Trees.All[key / 64].Id} variant {key % 64} failed: {error}");
                }
            }
        }

        /// <summary>Asks for a variant (once); null until it is grown and its meshes are made.</summary>
        Variant VariantOf(int kind, int variant)
        {
            var key = kind * 64 + variant;

            if (variants.TryGetValue(key, out var found))
            {
                return found.Ready ? found : null;
            }

            if (wanted.Add(key))
            {
                lock (toGrow)
                {
                    toGrow.Enqueue(key);
                    System.Threading.Monitor.Pulse(toGrow);
                }
            }

            return null;
        }

        /// <summary>Takes what the grower has grown and turns it into the engine's meshes: one level of one variant a frame, so no frame stalls.</summary>
        void Upload()
        {
            lock (grownNow)
            {
                while (grownNow.Count > 0)
                {
                    var (key, model) = grownNow.Dequeue();
                    var variant = new Variant { Model = model, Lods = new Mesh[4], Materials = new Material[4][], Height = model.Height, TrunkRadius = model.TrunkRadius };
                    variants[key] = variant;
                    uploading.Add(variant);
                }
            }

            if (uploading.Count == 0)
            {
                return;
            }

            var next = uploading[0];
            var model0 = next.Model;
            // The far levels first: a wood shows as soon as its distant shape is ready.
            var lod = 3 - next.Uploaded;

            if (lod >= 0)
            {
                next.Lods[lod] = MeshOf(model0.Lods[lod], $"{model0.Species.Id} LOD{lod}", out var parts);
                next.Materials[lod] = new Material[parts.Count];

                for (var i = 0; i < parts.Count; i++)
                {
                    next.Materials[lod][i] = MaterialFor(model0.Species, parts[i]);
                }

                // The generator's copy of this level is not needed again.
                model0.Lods[lod] = null;
                next.Uploaded++;
            }

            if (next.Uploaded >= 4)
            {
                next.Solid = Meshes.From(model0.Collision, model0.Species.Id + " solid");
                model0.Collision = null;
                uploading.RemoveAt(0);
            }
        }

        /// <summary>A level of detail as one mesh, a sub-mesh per material.</summary>
        static Mesh MeshOf(TreeLod lod, string name, out List<string> parts)
        {
            parts = new List<string>();
            var total = 0;

            foreach (var part in lod.Parts)
            {
                total += part.Mesh.VertexCount;
            }

            var vertices = new Vector3[total];
            var normals = new Vector3[total];
            var uvs = new Vector2[total];
            var colours = new Color32[total];
            var mesh = new Mesh { name = name };

            if (total > 65000)
            {
                mesh.indexFormat = IndexFormat.UInt32;
            }

            var at = 0;
            var triangles = new List<int[]>();

            foreach (var part in lod.Parts)
            {
                var data = part.Mesh;
                var count = data.VertexCount;

                for (var i = 0; i < count; i++)
                {
                    vertices[at + i] = new Vector3(data.Positions[i * 3], data.Positions[i * 3 + 1], data.Positions[i * 3 + 2]);
                    normals[at + i] = new Vector3(data.Normals[i * 3], data.Normals[i * 3 + 1], data.Normals[i * 3 + 2]);
                    uvs[at + i] = new Vector2(data.Uvs[i * 2], data.Uvs[i * 2 + 1]);
                    colours[at + i] = new Color32((byte)(Mathf.Clamp01(data.Colours[i * 4]) * 255), (byte)(Mathf.Clamp01(data.Colours[i * 4 + 1]) * 255),
                        (byte)(Mathf.Clamp01(data.Colours[i * 4 + 2]) * 255), (byte)(Mathf.Clamp01(data.Colours[i * 4 + 3]) * 255));
                }

                var indices = new int[data.Triangles.Count];

                for (var i = 0; i < indices.Length; i++)
                {
                    indices[i] = data.Triangles[i] + at;
                }

                triangles.Add(indices);
                parts.Add(part.Material);
                at += count;
            }

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.colors32 = colours;
            mesh.subMeshCount = triangles.Count;

            for (var i = 0; i < triangles.Count; i++)
            {
                mesh.SetTriangles(triangles[i], i, false);
            }

            mesh.RecalculateBounds();
            // The foliage sways past the mesh's own bounds.
            var bounds = mesh.bounds;
            bounds.Expand(2);
            mesh.bounds = bounds;

            return mesh;
        }

        /// <summary>The material of one part of a kind of tree: its bark (with the moss), its foliage, the rest.</summary>
        Material MaterialFor(TreeSpecies species, string part)
        {
            var key = part.StartsWith("bark:") ? part + "|" + species.Id : part;

            if (materials.TryGetValue(key, out var material))
            {
                return material;
            }

            if (part.StartsWith("bark:"))
            {
                var name = part.Substring(5);
                var shader = Resources.Load<Shader>("Shaders/TreeBark");
                var texture = Resources.Load<Texture2D>("Trees/" + name);

                if (shader != null && texture != null)
                {
                    material = new Material(shader) { name = "Bark " + name + " " + species.Id };
                    material.SetTexture("_BaseMap", texture);
                    material.SetTexture("_HeightMap", Resources.Load<Texture2D>("Trees/" + name + "_h"));
                    material.SetTexture("_MossMap", Resources.Load<Texture2D>("Trees/moss"));
                    material.SetTexture("_MossHeight", Resources.Load<Texture2D>("Trees/moss_h"));
                    material.SetColor("_BaseColor", new Color(species.BarkTint.R, species.BarkTint.G, species.BarkTint.B, 1));
                }
                else
                {
                    Debug.LogWarning($"GU World: no bark shader or texture for {name}; the tree is drawn plain");
                    material = new Material(vertexLit) { name = "Bark (plain) " + name };
                    material.SetColor("_BaseColor", new Color(0.36f, 0.3f, 0.25f, 1));
                }
            }
            else if (part.StartsWith("leaf:"))
            {
                var name = part.Substring(5);
                var shader = Resources.Load<Shader>("Shaders/TreeLeaf");
                var texture = Resources.Load<Texture2D>("Trees/" + name);

                if (shader != null && texture != null)
                {
                    material = new Material(shader) { name = "Foliage " + name };
                    texture.wrapMode = TextureWrapMode.Clamp;
                    material.SetTexture("_BaseMap", texture);
                    // Curtains and beards swing more than sprays.
                    material.SetFloat("_Sway", name.StartsWith("hang_") ? 0.35f : 0.16f);
                }
                else
                {
                    Debug.LogWarning($"GU World: no foliage shader or texture for {name}; the tree is drawn plain");
                    material = new Material(vertexLit) { name = "Foliage (plain) " + name };
                    material.SetColor("_BaseColor", new Color(0.25f, 0.45f, 0.2f, 1));
                }
            }
            else if (part == "glow")
            {
                var shader = Resources.Load<Shader>("Shaders/TreeGlow");
                material = shader != null ? new Material(shader) { name = "Glowing moss" } : new Material(vertexLit);
            }
            else
            {
                material = vertexLit;
            }

            material.enableInstancing = true;
            materials[key] = material;

            return material;
        }

        // ------------------------------------------------------------------ Every frame

        void Update()
        {
            var focus = Game.Hero != null ? Game.Hero.transform.position : Game.Rig != null ? Game.Rig.transform.position : Vector3.zero;
            var cell = new Vector2Int(Mathf.FloorToInt(focus.x / Scatter.CellSize), Mathf.FloorToInt(focus.z / Scatter.CellSize));
            wait -= Time.unscaledDeltaTime;
            Upload();

            // Again when the cell changes, every second, or when the hero has gone far enough that the levels of detail are stale.
            if (cell != lastCell || wait <= 0 || (focus - lastFocus).sqrMagnitude > lodReach[0] * lodReach[0] * 0.04f)
            {
                wait = 1;
                lastCell = cell;
                lastFocus = focus;
                Refresh(focus);
            }

            Draw();
        }

        /// <summary>Asks for the cells within the far radius, places the near solids, rebuilds the batches.</summary>
        void Refresh(Vector3 focus)
        {
            var reach = Mathf.CeilToInt(far / Scatter.CellSize);
            var cx = Mathf.FloorToInt(focus.x / Scatter.CellSize);
            var cz = Mathf.FloorToInt(focus.z / Scatter.CellSize);
            var wantNear = new HashSet<long>();
            var order = new List<(int x, int z, float d)>();

            for (var dz = -reach; dz <= reach; dz++)
            {
                for (var dx = -reach; dx <= reach; dx++)
                {
                    var centre = new Vector2((cx + dx + 0.5f) * Scatter.CellSize, (cz + dz + 0.5f) * Scatter.CellSize);
                    var d = (centre - new Vector2(focus.x, focus.z)).magnitude;

                    if (d <= far + Scatter.CellSize)
                    {
                        order.Add((cx + dx, cz + dz, d));
                    }
                }
            }

            order.Sort((a, b) => a.d.CompareTo(b.d));

            foreach (var (x, z, d) in order)
            {
                var key = Key(x, z);

                if (!asked.Contains(key))
                {
                    asked.Add(key);

                    lock (toWork)
                    {
                        toWork.Enqueue((x, z));
                        System.Threading.Monitor.Pulse(toWork);
                    }
                }

                if (d <= near + Scatter.CellSize * 0.7f)
                {
                    wantNear.Add(key);
                }
            }

            // Near cells: solids placed (once their cell is worked out and their trees grown); cells that left the near ring: back to the pools.
            foreach (var key in new List<long>(placed.Keys))
            {
                if (!wantNear.Contains(key))
                {
                    foreach (var made in placed[key])
                    {
                        Release(made);
                    }

                    placed.Remove(key);
                }
            }

            foreach (var key in wantNear)
            {
                if (placed.ContainsKey(key))
                {
                    continue;
                }

                List<Instance> list;

                lock (cells)
                {
                    cells.TryGetValue(key, out list);
                }

                if (list == null)
                {
                    continue;
                }

                var made = new List<GameObject>();
                var complete = true;

                foreach (var thing in list)
                {
                    var one = Solid(thing, ref complete);

                    if (one != null)
                    {
                        made.Add(one);
                    }
                }

                if (complete)
                {
                    placed[key] = made;
                }
                else
                {
                    // Some of its trees are not grown yet: try the cell again on the next refresh.
                    foreach (var one in made)
                    {
                        Release(one);
                    }
                }
            }

            // Everything into the batches, by its level of detail.
            foreach (var batch in batches.Values)
            {
                batch.Matrices.Clear();
            }

            var drawn = 0;
            var here = new Vector2(focus.x, focus.z);
            var detail = Mathf.Lerp(0.6f, 1f, Governor.Detail);

            foreach (var (x, z, d) in order)
            {
                List<Instance> list;

                lock (cells)
                {
                    cells.TryGetValue(Key(x, z), out list);
                }

                if (list == null)
                {
                    continue;
                }

                foreach (var thing in list)
                {
                    var distance = Vector2.Distance(here, new Vector2(thing.At.X, thing.At.Z));

                    if (distance > far)
                    {
                        continue;
                    }

                    if (thing.Kind == Strewn.Tree)
                    {
                        // The far woods thinned on lighter quality: a tree drops out by its own number.
                        if (distance > lodReach[2] && ((uint)thing.Seed % 100) / 100f > Quality.Trees)
                        {
                            continue;
                        }

                        // A giant holds its detail farther than a sapling.
                        var size = Mathf.Clamp(thing.Height / 40f, 0.6f, 2.2f);
                        var lod = distance < lodReach[0] * detail * size ? 0 : distance < lodReach[1] * detail * size ? 1 : distance < lodReach[2] * detail * size ? 2 : 3;

                        if (AddTree(thing, lod))
                        {
                            drawn++;
                        }

                        continue;
                    }

                    // Shrubs and reeds only in the middle distance.
                    if ((thing.Kind == Strewn.Shrub || thing.Kind == Strewn.Reeds) && distance > far * 0.4f)
                    {
                        continue;
                    }

                    if (thing.Kind == Strewn.Boulder || thing.Kind == Strewn.Shrub || thing.Kind == Strewn.Reeds)
                    {
                        var shape = Shape(thing);
                        var batch = BatchOf(1L << 40 | (long)((int)thing.Kind * 100 + thing.Seed % 6), shape, thing.Kind == Strewn.Boulder ? vertexLit : swaying,
                            thing.Kind == Strewn.Boulder && distance < lodReach[1] && Quality.Level >= 1 ? ShadowCastingMode.On : ShadowCastingMode.Off);
                        batch.Matrices.Add(Matrix4x4.TRS(new Vector3(thing.At.X, Ground.HeightAt(thing.At.X, thing.At.Z) - 0.05f, thing.At.Z), Quaternion.Euler(0, thing.Yaw, 0), Vector3.one * thing.Scale));
                        drawn++;
                    }
                }
            }

            Drawn = drawn;
        }

        /// <summary>A tree into its variant's batch at a level of detail; false if its variant is not ready yet.</summary>
        bool AddTree(in Instance thing, int lod)
        {
            if (thing.Species < 0 || thing.Species >= Trees.All.Length)
            {
                return false;
            }

            var index = library.Choose(thing);
            var variant = VariantOf(thing.Species, index);

            if (variant == null)
            {
                return false;
            }

            var (yaw, scale) = library.Place(thing, variant.Height);
            var shadows = lod == 0 || (lod == 1 && Quality.Level >= 1) || (lod == 2 && Quality.Level >= 2) ? ShadowCastingMode.On : ShadowCastingMode.Off;
            var key = ((long)(thing.Species * 64 + index) << 3) | (long)lod;

            if (!batches.TryGetValue(key, out var batch))
            {
                batch = new Batch { Mesh = variant.Lods[lod], Materials = variant.Materials[lod] };
                batches[key] = batch;
            }

            batch.Shadows = shadows;
            batch.Matrices.Add(Matrix4x4.TRS(new Vector3(thing.At.X, Ground.HeightAt(thing.At.X, thing.At.Z) - 0.1f, thing.At.Z), Quaternion.Euler(0, yaw, 0), Vector3.one * scale));

            return true;
        }

        Batch BatchOf(long key, Mesh mesh, Material material, ShadowCastingMode shadows)
        {
            if (!batches.TryGetValue(key, out var batch))
            {
                batch = new Batch { Mesh = mesh, Materials = new[] { material } };
                batches[key] = batch;
            }

            batch.Shadows = shadows;

            return batch;
        }

        Mesh Shape(in Instance thing)
        {
            // Shapes made in code: a handful of variants each, chosen by the seed.
            var variant = thing.Seed % 6;
            var key = (int)thing.Kind * 100 + variant;

            if (!shapes.TryGetValue(key, out var mesh))
            {
                var seed = (int)thing.Kind * 1000 + variant;
                var data = thing.Kind == Strewn.Boulder ? Shapes.Boulder(seed, 2) : thing.Kind == Strewn.Shrub ? Shapes.Shrub(seed) : Shapes.Reeds(seed);
                mesh = Meshes.From(data, thing.Kind + " " + variant);
                shapes[key] = mesh;
            }

            return mesh;
        }

        void Draw()
        {
            foreach (var batch in batches.Values)
            {
                var matrices = batch.Matrices;

                if (matrices.Count == 0 || batch.Mesh == null || batch.Materials == null)
                {
                    continue;
                }

                for (var start = 0; start < matrices.Count; start += chunk.Length)
                {
                    var count = Mathf.Min(chunk.Length, matrices.Count - start);
                    matrices.CopyTo(start, chunk, 0, count);

                    for (var s = 0; s < batch.Materials.Length && s < batch.Mesh.subMeshCount; s++)
                    {
                        if (batch.Materials[s] != null)
                        {
                            Graphics.DrawMeshInstanced(batch.Mesh, s, batch.Materials[s], chunk, count, null, batch.Shadows, true);
                        }
                    }
                }
            }
        }

        // ------------------------------------------------------------------ The solids near the hero

        GameObject Solid(in Instance thing, ref bool complete)
        {
            var at = new Vector3(thing.At.X, Ground.HeightAt(thing.At.X, thing.At.Z), thing.At.Z);

            if (thing.Kind == Strewn.Boulder)
            {
                var mesh = Shape(thing);
                var name = "boulder " + thing.Seed % 6;
                var rock = rocks.TryGetValue(name, out var stack) && stack.Count > 0 ? stack.Pop() : null;

                if (rock == null)
                {
                    rock = new GameObject(name);
                    rock.transform.SetParent(transform, false);
                    rock.AddComponent<MeshCollider>().sharedMesh = mesh;
                }

                rock.transform.SetPositionAndRotation(at - new Vector3(0, 0.05f, 0), Quaternion.Euler(0, thing.Yaw, 0));
                rock.transform.localScale = Vector3.one * thing.Scale;
                rock.SetActive(true);

                return rock;
            }

            if (thing.Kind != Strewn.Tree || thing.Species < 0)
            {
                return null;
            }

            var index = library.Choose(thing);
            var variant = VariantOf(thing.Species, index);

            if (variant == null)
            {
                complete = false;

                return null;
            }

            // Thin stems (bamboo, saplings) are walked through.
            var (yaw, scale) = library.Place(thing, variant.Height);

            if (variant.TrunkRadius * scale < 0.07f || variant.Solid == null)
            {
                return null;
            }

            var key = thing.Species * 64 + index;
            var tree = solids.TryGetValue(key, out var pool) && pool.Count > 0 ? pool.Pop() : null;

            if (tree == null)
            {
                tree = new GameObject("Tree " + key);
                tree.transform.SetParent(transform, false);
                tree.AddComponent<MeshCollider>().sharedMesh = variant.Solid;
                var solid = tree.AddComponent<TreeSolid>();
                solid.Species = Trees.All[thing.Species];
            }

            tree.GetComponent<TreeSolid>().Height = variant.Height * scale;
            tree.transform.SetPositionAndRotation(at - new Vector3(0, 0.1f, 0), Quaternion.Euler(0, yaw, 0));
            tree.transform.localScale = Vector3.one * scale;
            tree.SetActive(true);

            return tree;
        }

        void Release(GameObject made)
        {
            if (made == null)
            {
                return;
            }

            made.SetActive(false);

            if (made.name.StartsWith("Tree "))
            {
                var key = int.Parse(made.name.Substring(5));

                if (!solids.TryGetValue(key, out var pool))
                {
                    solids[key] = pool = new Stack<GameObject>();
                }

                pool.Push(made);

                return;
            }

            if (!rocks.TryGetValue(made.name, out var stack))
            {
                stack = new Stack<GameObject>();
                rocks[made.name] = stack;
            }

            stack.Push(made);
        }
    }
}
