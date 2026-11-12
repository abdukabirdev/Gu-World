using System.Collections.Generic;
using GuWorld.Actors;
using GuWorld.Core;
using GuWorld.Core.Gen;
using UnityEngine;
using UnityEngine.Rendering;

namespace GuWorld.World
{
    /// <summary>
    /// A town of a thousand houses, kept by cells of the map: near the hero each house is the artist's
    /// prefab with its colliders and levels of detail (raised a few a frame, so walking in does not
    /// stutter), farther out the houses are drawn in instanced batches of their far meshes, with no
    /// objects at all. Each house stands on its pad (the land is levelled under it by the plan).
    /// </summary>
    public class Town : MonoBehaviour
    {
        const float CellSize = 80;

        SettlementPlan plan;
        float near, far;
        float wait;
        Vector2Int lastCell = new Vector2Int(int.MinValue, int.MinValue);

        readonly Dictionary<long, List<Placed>> cells = new Dictionary<long, List<Placed>>();
        readonly Dictionary<long, List<GameObject>> raised = new Dictionary<long, List<GameObject>>();
        readonly Queue<(long cell, Placed building)> toRaise = new Queue<(long, Placed)>();
        readonly HashSet<long> wanted = new HashSet<long>();

        class Batch
        {
            public Mesh Mesh;
            public int Submesh;
            public Material Material;
            public Matrix4x4 Local;
            public readonly List<Matrix4x4> Matrices = new List<Matrix4x4>();
        }

        readonly Dictionary<string, List<Batch>> farParts = new Dictionary<string, List<Batch>>();
        readonly Dictionary<Material, Material> instanced = new Dictionary<Material, Material>();
        readonly Matrix4x4[] chunk = new Matrix4x4[1023];

        static long Key(int x, int z) => ((long)x << 32) ^ (uint)z;

        public static Town Make(Transform parent, SettlementPlan plan)
        {
            var root = new GameObject(plan.Def.Name);
            root.transform.SetParent(parent, false);
            var town = root.AddComponent<Town>();
            town.plan = plan;
            town.near = Quality.Level >= 2 ? 300 : Quality.Level == 1 ? 210 : 150;
            town.far = Quality.Level >= 2 ? 1300 : Quality.Level == 1 ? 850 : 520;

            foreach (var building in plan.Buildings)
            {
                var key = Key(Mathf.FloorToInt(building.At.X / CellSize), Mathf.FloorToInt(building.At.Z / CellSize));

                if (!town.cells.TryGetValue(key, out var list))
                {
                    town.cells[key] = list = new List<Placed>();
                }

                list.Add(building);
            }

            return town;
        }

        Vector3 Focus => Game.Hero != null ? Game.Hero.transform.position : Game.Rig != null ? Game.Rig.transform.position : Vector3.zero;

        void Update()
        {
            // A few houses raised a frame (more on a strong machine).
            for (var n = 0; n < (Quality.Level >= 2 ? 6 : 3) && toRaise.Count > 0; n++)
            {
                var (cell, building) = toRaise.Dequeue();

                if (!wanted.Contains(cell) || !Ground.Ready(new Vector3(building.At.X, 0, building.At.Z)))
                {
                    if (wanted.Contains(cell))
                    {
                        toRaise.Enqueue((cell, building));
                    }

                    break;
                }

                var made = Models.Prefab(building.Model, transform, Foot(building), building.Yaw, building.Scale);

                if (made != null)
                {
                    raised[cell].Add(made);
                }
            }

            wait -= Time.unscaledDeltaTime;
            var focus = Focus;
            var here = new Vector2Int(Mathf.FloorToInt(focus.x / CellSize), Mathf.FloorToInt(focus.z / CellSize));

            if (wait <= 0 || here != lastCell)
            {
                wait = 1;
                lastCell = here;
                Refresh(focus);
            }

            Draw();
        }

        /// <summary>Where a house's origin stands: on its pad.</summary>
        static Vector3 Foot(Placed building) => new Vector3(building.At.X, Ground.HeightAt(building.Footprint.Centre.X, building.Footprint.Centre.Z) - 0.05f, building.At.Z);

        void Refresh(Vector3 focus)
        {
            var reach = Mathf.CeilToInt(far / CellSize);
            var centre = new Vector2(focus.x, focus.z);

            foreach (var batches in farParts.Values)
            {
                foreach (var batch in batches)
                {
                    batch.Matrices.Clear();
                }
            }

            var nearNow = new HashSet<long>();

            for (var dz = -reach; dz <= reach; dz++)
            {
                for (var dx = -reach; dx <= reach; dx++)
                {
                    var cx = lastCell.x + dx;
                    var cz = lastCell.y + dz;
                    var key = Key(cx, cz);

                    if (!cells.TryGetValue(key, out var buildings))
                    {
                        continue;
                    }

                    var cellCentre = new Vector2((cx + 0.5f) * CellSize, (cz + 0.5f) * CellSize);
                    var distance = Vector2.Distance(cellCentre, centre);

                    if (distance > far + CellSize)
                    {
                        continue;
                    }

                    if (distance < near)
                    {
                        nearNow.Add(key);

                        if (!raised.ContainsKey(key))
                        {
                            raised[key] = new List<GameObject>();
                            wanted.Add(key);

                            foreach (var building in buildings)
                            {
                                toRaise.Enqueue((key, building));
                            }
                        }

                        // Until its houses are all up, a near cell is drawn far as well (nothing pops out).
                        if (raised[key].Count >= buildings.Count)
                        {
                            continue;
                        }
                    }

                    foreach (var building in buildings)
                    {
                        var foot = Foot(building);
                        var matrix = Matrix4x4.TRS(foot, Quaternion.Euler(0, building.Yaw, 0), Vector3.one * building.Scale);

                        foreach (var part in Parts(building.Model))
                        {
                            part.Matrices.Add(matrix * part.Local);
                        }
                    }
                }
            }

            // Cells left behind (with a margin, so walking along an edge does not raise and drop them).
            var drop = new List<long>();

            foreach (var key in raised.Keys)
            {
                if (!nearNow.Contains(key))
                {
                    var x = (int)(key >> 32);
                    var z = (int)(uint)key;
                    var cellCentre = new Vector2((x + 0.5f) * CellSize, (z + 0.5f) * CellSize);

                    if (Vector2.Distance(cellCentre, centre) > near + 60)
                    {
                        drop.Add(key);
                    }
                }
            }

            foreach (var key in drop)
            {
                foreach (var made in raised[key])
                {
                    Destroy(made);
                }

                raised.Remove(key);
                wanted.Remove(key);
            }
        }

        void Draw()
        {
            var shadows = Quality.Level >= 2 ? ShadowCastingMode.On : ShadowCastingMode.Off;

            foreach (var batches in farParts.Values)
            {
                foreach (var batch in batches)
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
                        Graphics.DrawMeshInstanced(batch.Mesh, batch.Submesh, batch.Material, chunk, count, null, shadows, true);
                    }
                }
            }
        }

        /// <summary>A model's far meshes: its last level of detail's renderers, each a batch per submesh.</summary>
        List<Batch> Parts(string model)
        {
            if (farParts.TryGetValue(model, out var parts))
            {
                return parts;
            }

            parts = new List<Batch>();
            farParts[model] = parts;
            var prefab = Resources.Load<GameObject>("Build/" + model);

            if (prefab == null)
            {
                return parts;
            }

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

            return parts;
        }

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

        /// <summary>How many houses stand as prefabs and how many are drawn from afar (for the autotest's log).</summary>
        public string Count()
        {
            var up = 0;
            var drawn = 0;

            foreach (var list in raised.Values)
            {
                up += list.Count;
            }

            foreach (var batches in farParts.Values)
            {
                if (batches.Count > 0)
                {
                    drawn += batches[0].Matrices.Count;
                }
            }

            return $"{plan.Def.Name}: {up} raised, {drawn} drawn far";
        }
    }
}
