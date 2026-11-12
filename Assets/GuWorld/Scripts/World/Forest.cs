using System.Collections.Generic;
using GuWorld.Actors;
using GuWorld.Core;
using GuWorld.Core.Gen;
using GuWorld.Core.Gen.Flora;
using UnityEngine;
using UnityEngine.Rendering;

namespace GuWorld.World
{
    /// <summary>
    /// The woods, groves and rocks of the plan's scatter, round the hero. Every tree is grown by the
    /// generator (Core/Gen/Flora): a library of variants per kind — open and crowded, straight and
    /// leaning, a few seeds of each — is grown on a worker thread at load, and each tree of the map
    /// wears the variant nearest its asking, turned so it leans where its wind and slope push it and
    /// scaled to its height. Nothing is a GameObject but the solids near the hero (a capsule round
    /// each trunk, the boulders' own shapes): everything is drawn in instanced batches by its
    /// variant and its level of detail, picked by distance (four of them, the farthest a few dozen
    /// triangles). Boulders, shrubs and reeds are shapes made in code, drawn the same way.
    ///
    /// The scatter's cells and the library are worked out on worker threads; the main thread only
    /// turns grown meshes into the engine's, places solids and draws.
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

        // The trees' variants, grown on their own thread; their meshes made on the main thread as first needed.
        TreeLibrary library;
        readonly Dictionary<int, Mesh[]> treeMeshes = new Dictionary<int, Mesh[]>();

        // Near: solids kept by cell (trunks' capsules, boulders' shapes).
        readonly Dictionary<long, List<GameObject>> placed = new Dictionary<long, List<GameObject>>();
        readonly Stack<GameObject> trunks = new Stack<GameObject>();
        readonly Dictionary<string, Stack<GameObject>> rocks = new Dictionary<string, Stack<GameObject>>();

        // Everything drawn: batches of matrices by mesh.
        class Batch
        {
            public Mesh Mesh;
            public Material Material;
            public ShadowCastingMode Shadows;
            public readonly List<Matrix4x4> Matrices = new List<Matrix4x4>();
        }

        readonly Dictionary<long, Batch> batches = new Dictionary<long, Batch>();
        readonly Dictionary<int, Mesh> shapes = new Dictionary<int, Mesh>();
        Material vertexLit, swaying, trees;
        readonly Matrix4x4[] chunk = new Matrix4x4[1023];

        public static int Drawn { get; private set; }
        public static string Count() => $"{Drawn} trees and shapes drawn; tree variants grown {(instance != null && instance.library != null ? instance.Grown() : 0)}";
        static Forest instance;

        static long Key(int x, int z) => ((long)x << 32) ^ (uint)z;

        public static Forest Make(Transform parent, WorldPlan plan)
        {
            var root = new GameObject("Forest");
            root.transform.SetParent(parent, false);
            var forest = root.AddComponent<Forest>();
            instance = forest;
            forest.plan = plan;
            forest.near = Quality.Level >= 2 ? 240 : Quality.Level == 1 ? 170 : 110;
            forest.far = Quality.Level >= 2 ? 1300 : Quality.Level == 1 ? 800 : 450;
            // Where each level of detail gives way to the next.
            forest.lodReach = Quality.Level >= 2 ? new[] { 70f, 180f, 380f } : Quality.Level == 1 ? new[] { 50f, 130f, 280f } : new[] { 32f, 90f, 190f };
            forest.vertexLit = Models.VertexLit(Color.white, 0);
            forest.swaying = Models.VertexLit(Color.white, 0.04f);
            forest.trees = Models.VertexLit(Color.white, 0.012f);
            forest.library = new TreeLibrary(Quality.Level >= 2 ? 3 : 2);
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

            if (instance == this)
            {
                instance = null;
            }
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

        /// <summary>Grows the library: the commonest kinds' first variants first, so the woods fill in soon after load.</summary>
        void GrowTrees()
        {
            try
            {
                var count = library.VariantsPerKind;

                for (var v = 0; v < count && !stopping; v++)
                {
                    for (var kind = 0; kind < Trees.All.Length && !stopping; kind++)
                    {
                        library.Get(kind, v);

                        lock (grown)
                        {
                            grown.Add(kind * 64 + v);
                        }
                    }
                }
            }
            catch (System.Exception error)
            {
                Debug.LogError($"GU World: growing the trees failed: {error}");
            }
        }

        readonly HashSet<int> grown = new HashSet<int>();

        int Grown()
        {
            lock (grown)
            {
                return grown.Count;
            }
        }

        bool IsGrown(int kind, int variant)
        {
            lock (grown)
            {
                return grown.Contains(kind * 64 + variant);
            }
        }

        // ------------------------------------------------------------------ Every frame

        void Update()
        {
            var focus = Game.Hero != null ? Game.Hero.transform.position : Game.Rig != null ? Game.Rig.transform.position : Vector3.zero;
            var cell = new Vector2Int(Mathf.FloorToInt(focus.x / Scatter.CellSize), Mathf.FloorToInt(focus.z / Scatter.CellSize));
            wait -= Time.unscaledDeltaTime;

            // Again when the cell changes, every second, or when the hero has gone far enough that the levels of detail are stale.
            if (cell != lastCell || wait <= 0 || (focus - lastFocus).sqrMagnitude > lodReach[0] * lodReach[0] * 0.09f)
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

            // Near cells: solids placed (once their cell is worked out); cells that left the near ring: back to the pools.
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

                        var lod = distance < lodReach[0] * detail ? 0 : distance < lodReach[1] * detail ? 1 : distance < lodReach[2] * detail ? 2 : 3;

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

        /// <summary>A tree into its variant's batch at a level of detail; false if its variant is not grown yet.</summary>
        bool AddTree(in Instance thing, int lod)
        {
            if (thing.Species < 0 || thing.Species >= Trees.All.Length)
            {
                return false;
            }

            var variant = library.Choose(thing);

            if (!IsGrown(thing.Species, variant))
            {
                return false;
            }

            var model = library.Get(thing.Species, variant);
            var meshes = TreeMeshes(thing.Species, variant, model);
            var (yaw, scale) = library.Place(thing, model);
            var shadows = lod == 0 || (lod == 1 && Quality.Level >= 1) || (lod == 2 && Quality.Level >= 2) ? ShadowCastingMode.On : ShadowCastingMode.Off;
            var batch = BatchOf(((long)(thing.Species * 64 + variant) << 3) | (long)lod, meshes[lod], trees, shadows);
            batch.Matrices.Add(Matrix4x4.TRS(new Vector3(thing.At.X, Ground.HeightAt(thing.At.X, thing.At.Z) - 0.1f, thing.At.Z), Quaternion.Euler(0, yaw, 0), Vector3.one * scale));

            return true;
        }

        Mesh[] TreeMeshes(int kind, int variant, TreeModel model)
        {
            var key = kind * 64 + variant;

            if (!treeMeshes.TryGetValue(key, out var meshes))
            {
                meshes = new Mesh[model.Lods.Length];

                for (var i = 0; i < meshes.Length; i++)
                {
                    meshes[i] = Meshes.From(model.Lods[i], $"{model.Species.Id} {variant} LOD{i}");
                }

                treeMeshes[key] = meshes;
            }

            return meshes;
        }

        Batch BatchOf(long key, Mesh mesh, Material material, ShadowCastingMode shadows)
        {
            if (!batches.TryGetValue(key, out var batch))
            {
                batch = new Batch { Mesh = mesh, Material = material };
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

                if (matrices.Count == 0 || batch.Mesh == null || batch.Material == null)
                {
                    continue;
                }

                for (var start = 0; start < matrices.Count; start += chunk.Length)
                {
                    var count = Mathf.Min(chunk.Length, matrices.Count - start);
                    matrices.CopyTo(start, chunk, 0, count);
                    Graphics.DrawMeshInstanced(batch.Mesh, 0, batch.Material, chunk, count, null, batch.Shadows, true);
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
                var rock = Take(rocks, name);

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

            var variant = library.Choose(thing);

            if (!IsGrown(thing.Species, variant))
            {
                complete = false;

                return null;
            }

            var model = library.Get(thing.Species, variant);
            var (yaw, scale) = library.Place(thing, model);
            var lean = TreeLibrary.Leans[library.Split(variant).lean];

            // Thin stems (bamboo, saplings) are walked through; a trunk is a capsule to its crown.
            if (model.TrunkRadius * scale < 0.07f)
            {
                return null;
            }

            var trunk = trunks.Count > 0 ? trunks.Pop() : null;

            if (trunk == null)
            {
                trunk = new GameObject("Trunk");
                trunk.transform.SetParent(transform, false);
                trunk.AddComponent<CapsuleCollider>();
            }

            var capsule = trunk.GetComponent<CapsuleCollider>();
            var height = Mathf.Clamp(model.CrownBase * scale, 2.5f, 14);
            capsule.radius = Mathf.Clamp(model.TrunkRadius * scale * 0.9f, 0.12f, 2.5f);
            capsule.height = Mathf.Max(height, capsule.radius * 2 + 0.1f);
            capsule.center = new Vector3(0, height / 2 - 0.2f, 0);
            // Tilted along the variant's lean (its +X), as the trunk is near its foot.
            trunk.transform.SetPositionAndRotation(at, Quaternion.Euler(0, yaw, 0) * Quaternion.Euler(0, 0, -lean * 0.85f));
            trunk.SetActive(true);

            return trunk;
        }

        static GameObject Take(Dictionary<string, Stack<GameObject>> pool, string name) => pool.TryGetValue(name, out var stack) && stack.Count > 0 ? stack.Pop() : null;

        void Release(GameObject made)
        {
            if (made == null)
            {
                return;
            }

            made.SetActive(false);

            if (made.name == "Trunk")
            {
                trunks.Push(made);

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
