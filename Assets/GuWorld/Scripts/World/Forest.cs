using System.Collections.Generic;
using GuWorld.Actors;
using GuWorld.Core;
using GuWorld.Core.Gen;
using UnityEngine;
using UnityEngine.Rendering;

namespace GuWorld.World
{
    /// <summary>
    /// The woods, groves and rocks of the plan's scatter, round the hero. Near (a couple of hundred
    /// metres) each tree is the artist's prefab with its colliders and levels of detail; farther,
    /// out to the fog, trees are drawn in batches from their far meshes, with no objects at all —
    /// that is what keeps the frame rate on a laptop. Boulders, shrubs and reeds are shapes made in
    /// code, drawn in batches too (boulders near the hero also get a collider).
    ///
    /// The scatter's cells are worked out on a worker thread; the main thread only places and draws.
    /// </summary>
    public class Forest : MonoBehaviour
    {
        WorldPlan plan;
        float near, far;
        float wait;
        Vector2Int lastCell = new Vector2Int(int.MinValue, int.MinValue);

        // The plan's cells as worked out (shared with the worker thread).
        readonly Dictionary<long, List<Instance>> cells = new Dictionary<long, List<Instance>>();
        readonly HashSet<long> asked = new HashSet<long>();
        readonly Queue<(int x, int z)> toWork = new Queue<(int, int)>();
        System.Threading.Thread worker;
        volatile bool stopping;

        // Near: placed prefabs, kept by cell.
        readonly Dictionary<long, List<GameObject>> placed = new Dictionary<long, List<GameObject>>();
        readonly Dictionary<string, Stack<GameObject>> pool = new Dictionary<string, Stack<GameObject>>();

        // Far: batches of matrices by what is drawn.
        class Batch
        {
            public Mesh Mesh;
            public int Submesh;
            public Material Material;
            public Matrix4x4 Local = Matrix4x4.identity;
            public readonly List<Matrix4x4> Matrices = new List<Matrix4x4>();
        }

        readonly Dictionary<Strewn, List<Batch>> farParts = new Dictionary<Strewn, List<Batch>>();
        readonly Dictionary<int, Mesh> shapes = new Dictionary<int, Mesh>();
        Material vertexLit, swaying;
        readonly Matrix4x4[] chunk = new Matrix4x4[1023];

        static long Key(int x, int z) => ((long)x << 32) ^ (uint)z;

        public static Forest Make(Transform parent, WorldPlan plan)
        {
            var root = new GameObject("Forest");
            root.transform.SetParent(parent, false);
            var forest = root.AddComponent<Forest>();
            forest.plan = plan;
            forest.near = Quality.Level >= 2 ? 240 : Quality.Level == 1 ? 170 : 110;
            forest.far = Quality.Level >= 2 ? 1300 : Quality.Level == 1 ? 800 : 450;
            forest.vertexLit = Models.VertexLit(Color.white, 0);
            forest.swaying = Models.VertexLit(Color.white, 0.04f);
            forest.worker = new System.Threading.Thread(forest.Work) { IsBackground = true, Name = "GU World forest" };
            forest.worker.Start();

            return forest;
        }

        void OnDestroy()
        {
            stopping = true;

            lock (toWork)
            {
                System.Threading.Monitor.PulseAll(toWork);
            }
        }

        // ------------------------------------------------------------------ The worker

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

        // ------------------------------------------------------------------ Every frame

        void Update()
        {
            var focus = Game.Hero != null ? Game.Hero.transform.position : Game.Rig != null ? Game.Rig.transform.position : Vector3.zero;
            var cell = new Vector2Int(Mathf.FloorToInt(focus.x / Scatter.CellSize), Mathf.FloorToInt(focus.z / Scatter.CellSize));
            wait -= Time.unscaledDeltaTime;

            if (cell != lastCell || wait <= 0)
            {
                wait = 1;
                lastCell = cell;
                Refresh(focus);
            }

            Draw();
        }

        /// <summary>Asks for the cells within the far radius, places the near ones, rebuilds the batches.</summary>
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

            // Near cells: prefabs placed (once their cell is worked out); cells that left the near ring: back to the pool.
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

                foreach (var instance in list)
                {
                    var one = Place(instance);

                    if (one != null)
                    {
                        made.Add(one);
                    }
                }

                placed[key] = made;
            }

            // Far cells: into the batches.
            foreach (var parts in farParts.Values)
            {
                foreach (var batch in parts)
                {
                    batch.Matrices.Clear();
                }
            }

            foreach (var (x, z, d) in order)
            {
                var key = Key(x, z);
                List<Instance> list;

                lock (cells)
                {
                    cells.TryGetValue(key, out list);
                }

                if (list == null)
                {
                    continue;
                }

                var isNear = placed.ContainsKey(key);

                foreach (var instance in list)
                {
                    // Near cells have their trees as objects; their small things are drawn in batches anyway.
                    if (isNear && instance.IsTree)
                    {
                        continue;
                    }

                    // Shrubs and reeds only in the middle distance.
                    if ((instance.Kind == Strewn.Shrub || instance.Kind == Strewn.Reeds) && d > far * 0.4f)
                    {
                        continue;
                    }

                    foreach (var batch in Parts(instance))
                    {
                        batch.Matrices.Add(Matrix(instance) * batch.Local);
                    }
                }
            }
        }

        static Matrix4x4 Matrix(Instance instance)
        {
            var y = Ground.HeightAt(instance.At.X, instance.At.Z) - (instance.IsTree ? 0.15f : 0.05f);

            return Matrix4x4.TRS(new Vector3(instance.At.X, y, instance.At.Z), Quaternion.Euler(0, instance.Yaw, 0), Vector3.one * instance.Scale);
        }

        void Draw()
        {
            foreach (var parts in farParts.Values)
            {
                foreach (var batch in parts)
                {
                    var matrices = batch.Matrices;

                    if (matrices.Count == 0 || batch.Mesh == null || batch.Material == null)
                    {
                        continue;
                    }

                    var shadows = Quality.Level >= 2 ? ShadowCastingMode.On : ShadowCastingMode.Off;

                    for (var start = 0; start < matrices.Count; start += chunk.Length)
                    {
                        var count = Mathf.Min(chunk.Length, matrices.Count - start);
                        matrices.CopyTo(start, chunk, 0, count);
                        Graphics.DrawMeshInstanced(batch.Mesh, batch.Submesh, batch.Material, chunk, count, null, shadows, true);
                    }
                }
            }
        }

        // ------------------------------------------------------------------ What is drawn far

        /// <summary>The meshes a strewn thing is drawn with from afar: a tree's lowest level of detail, or the shape made in code.</summary>
        List<Batch> Parts(Instance instance)
        {
            if (!instance.IsTree && instance.Kind != Strewn.Bonsai)
            {
                // Shapes made in code: a handful of variants each, chosen by the seed.
                var variant = instance.Seed % 6;
                var key = (int)instance.Kind * 100 + variant;

                if (!farParts.TryGetValue(instance.Kind, out var variants))
                {
                    variants = new List<Batch>();
                    farParts[instance.Kind] = variants;
                }

                while (variants.Count <= variant)
                {
                    var seed = (int)instance.Kind * 1000 + variants.Count;
                    var data = instance.Kind == Strewn.Boulder ? Shapes.Boulder(seed, 2) : instance.Kind == Strewn.Shrub ? Shapes.Shrub(seed) : Shapes.Reeds(seed);
                    variants.Add(new Batch
                    {
                        Mesh = Meshes.From(data, instance.Kind + " " + variants.Count),
                        Material = instance.Kind == Strewn.Boulder ? vertexLit : swaying,
                    });
                }

                single.Clear();
                single.Add(variants[variant]);

                return single;
            }

            if (!farParts.TryGetValue(instance.Kind, out var parts))
            {
                parts = new List<Batch>();
                farParts[instance.Kind] = parts;
                var prefab = Resources.Load<GameObject>("Build/" + Scatter.Models[instance.Kind]);

                if (prefab != null)
                {
                    // The last level of detail's renderers, each a batch per submesh, with its place in the prefab.
                    var group = prefab.GetComponentInChildren<LODGroup>();
                    var renderers = group != null && group.lodCount > 0 ? group.GetLODs()[group.lodCount - 1].renderers : prefab.GetComponentsInChildren<Renderer>();

                    foreach (var renderer in renderers)
                    {
                        var filter = renderer != null ? renderer.GetComponent<MeshFilter>() : null;

                        if (filter == null || filter.sharedMesh == null)
                        {
                            continue;
                        }

                        var local = prefab.transform.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
                        var materials = renderer.sharedMaterials;

                        for (var s = 0; s < filter.sharedMesh.subMeshCount && s < materials.Length; s++)
                        {
                            parts.Add(new Batch { Mesh = filter.sharedMesh, Submesh = s, Material = Instanced(materials[s]), Local = local });
                        }
                    }
                }
            }

            return parts;
        }

        readonly List<Batch> single = new List<Batch>(1);
        readonly Dictionary<Material, Material> instanced = new Dictionary<Material, Material>();

        Material Instanced(Material source)
        {
            if (source == null)
            {
                return null;
            }

            if (!instanced.TryGetValue(source, out var copy))
            {
                copy = new Material(source) { name = source.name + " (instanced)", enableInstancing = true };
                instanced[source] = copy;
            }

            return copy;
        }

        // ------------------------------------------------------------------ What is placed near

        GameObject Place(Instance instance)
        {
            if (instance.Kind == Strewn.Shrub || instance.Kind == Strewn.Reeds)
            {
                return null;
            }

            var at = new Vector3(instance.At.X, Ground.HeightAt(instance.At.X, instance.At.Z) - (instance.IsTree ? 0.15f : 0.05f), instance.At.Z);
            var turn = Quaternion.Euler(0, instance.Yaw, 0);

            if (instance.Kind == Strewn.Boulder)
            {
                // A boulder near the hero is solid.
                var variant = instance.Seed % 6;
                var mesh = Parts(instance)[0].Mesh;
                var rock = Take("boulder " + variant);

                if (rock == null)
                {
                    rock = new GameObject("Boulder");
                    rock.AddComponent<MeshFilter>().sharedMesh = mesh;
                    rock.AddComponent<MeshRenderer>().sharedMaterial = vertexLit;
                    rock.AddComponent<MeshCollider>().sharedMesh = mesh;
                    rock.name = "boulder " + variant;
                }

                rock.transform.SetParent(transform, false);
                rock.transform.SetPositionAndRotation(at, turn);
                rock.transform.localScale = Vector3.one * instance.Scale;
                rock.SetActive(true);
                // Drawn by the batches already: the object only holds the collider.
                rock.GetComponent<MeshRenderer>().enabled = false;

                return rock;
            }

            var model = Scatter.Models[instance.Kind];
            var tree = Take(model);

            if (tree == null)
            {
                tree = Models.Prefab(model, transform, at, instance.Yaw, instance.Scale);

                if (tree == null)
                {
                    return null;
                }

                // The limbs' own solids are for climbing, which this game has none of: the trunk is enough.
                foreach (var box in tree.GetComponentsInChildren<BoxCollider>())
                {
                    Destroy(box);
                }
            }
            else
            {
                tree.transform.SetPositionAndRotation(at, turn);
                tree.transform.localScale = Vector3.one * instance.Scale;
                tree.SetActive(true);
            }

            tree.name = model;

            return tree;
        }

        GameObject Take(string name) => pool.TryGetValue(name, out var stack) && stack.Count > 0 ? stack.Pop() : null;

        void Release(GameObject made)
        {
            if (made == null)
            {
                return;
            }

            made.SetActive(false);

            if (!pool.TryGetValue(made.name, out var stack))
            {
                stack = new Stack<GameObject>();
                pool[made.name] = stack;
            }

            stack.Push(made);
        }
    }
}
