using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace GuWorld.Render
{
    /// <summary>
    /// The terrains' trees drawn in bulk: Unity draws each tree with levels of detail one by
    /// one (thousands of them cost a third of a frame), so the terrains keep their trees only
    /// to be solid (their trunks) and this draws them — gathered into 64 m squares, each square
    /// drawn with the GPU's instancing, its level of detail by its distance, squares out of
    /// view skipped. How far: Distance (the graphics level's).
    /// </summary>
    public class TreeInstancer : MonoBehaviour
    {
        const float CellSize = 64;
        const int Batch = 1023;

        public static TreeInstancer Current { get; private set; }

        /// <summary>How far trees are drawn, m (0: none).</summary>
        public static float Distance = 800;
        /// <summary>Within these the full tree and the middle one are drawn (beyond, the plainest).</summary>
        public static float FullUpTo = 120;
        public static float MiddleUpTo = 420;
        public static float ShadowsUpTo = 110;

        class Kind
        {
            public GameObject Prefab;
            public Mesh[] Meshes = new Mesh[3];
            public Material[][] Materials = new Material[3][];
        }

        class Cell
        {
            public Bounds Bounds;
            public readonly Dictionary<Kind, List<Matrix4x4>> Trees = new Dictionary<Kind, List<Matrix4x4>>();
            public readonly Dictionary<Kind, Matrix4x4[]> Ready = new Dictionary<Kind, Matrix4x4[]>();
        }

        readonly Dictionary<Vector2Int, Cell> cells = new Dictionary<Vector2Int, Cell>();
        readonly Dictionary<GameObject, Kind> kinds = new Dictionary<GameObject, Kind>();
        readonly Plane[] planes = new Plane[6];
        bool gathered;

        void Awake() => Current = this;

        void OnDestroy()
        {
            if (Current == this)
            {
                Current = null;
            }
        }

        Kind KindOf(GameObject prefab)
        {
            if (kinds.TryGetValue(prefab, out var kind))
            {
                return kind;
            }

            kind = null;
            var group = prefab.GetComponent<LODGroup>();
            var lods = group != null ? group.GetLODs() : null;

            if (lods != null && lods.Length > 0)
            {
                kind = new Kind { Prefab = prefab };

                for (var level = 0; level < 3; level++)
                {
                    var lod = lods[Mathf.Min(level, lods.Length - 1)];
                    var renderer = lod.renderers.Length > 0 ? lod.renderers[0] : null;
                    var filter = renderer != null ? renderer.GetComponent<MeshFilter>() : null;
                    kind.Meshes[level] = filter != null ? filter.sharedMesh : null;
                    kind.Materials[level] = renderer != null ? renderer.sharedMaterials : new Material[0];
                }
            }

            kinds[prefab] = kind;

            return kind;
        }

        /// <summary>Takes the terrains' trees over: gathered into squares, and the terrains no longer draw them.</summary>
        void Gather()
        {
            gathered = true;

            foreach (var terrain in Terrain.activeTerrains)
            {
                var data = terrain.terrainData;
                var corner = terrain.GetPosition();
                var size = data.size;
                var prototypes = data.treePrototypes;

                foreach (var tree in data.treeInstances)
                {
                    if (tree.prototypeIndex >= prototypes.Length || prototypes[tree.prototypeIndex].prefab == null)
                    {
                        continue;
                    }

                    var kind = KindOf(prototypes[tree.prototypeIndex].prefab);

                    if (kind == null)
                    {
                        continue;
                    }

                    var at = corner + Vector3.Scale(tree.position, size);
                    var key = new Vector2Int(Mathf.FloorToInt(at.x / CellSize), Mathf.FloorToInt(at.z / CellSize));

                    if (!cells.TryGetValue(key, out var cell))
                    {
                        cells[key] = cell = new Cell { Bounds = new Bounds(at, Vector3.zero) };
                    }

                    if (!cell.Trees.TryGetValue(kind, out var list))
                    {
                        cell.Trees[kind] = list = new List<Matrix4x4>();
                    }

                    list.Add(Matrix4x4.TRS(at, Quaternion.Euler(0, tree.rotation * Mathf.Rad2Deg, 0), new Vector3(tree.widthScale, tree.heightScale, tree.widthScale)));
                    // The tree's own size (trunk and crown grow with it): tall ones are seen over the hill before their foot is.
                    cell.Bounds.Encapsulate(new Bounds(at + Vector3.up * 10 * tree.heightScale, new Vector3(12 * tree.widthScale, 20 * tree.heightScale, 12 * tree.widthScale)));
                }

                terrain.treeDistance = 0;
            }

            foreach (var cell in cells.Values)
            {
                foreach (var pair in cell.Trees)
                {
                    cell.Ready[pair.Key] = pair.Value.ToArray();
                }

                cell.Trees.Clear();
            }
        }

        /// <summary>The cell a point lies in (made when there is none: a tree planted where none grew).</summary>
        Cell CellAt(Vector3 at)
        {
            var key = new Vector2Int(Mathf.FloorToInt(at.x / CellSize), Mathf.FloorToInt(at.z / CellSize));

            if (!cells.TryGetValue(key, out var cell))
            {
                cells[key] = cell = new Cell { Bounds = new Bounds(at, Vector3.zero) };
            }

            return cell;
        }

        IEnumerable<Cell> CellsOver(Rect area)
        {
            for (var z = Mathf.FloorToInt(area.yMin / CellSize); z <= Mathf.FloorToInt(area.yMax / CellSize); z++)
            {
                for (var x = Mathf.FloorToInt(area.xMin / CellSize); x <= Mathf.FloorToInt(area.xMax / CellSize); x++)
                {
                    if (cells.TryGetValue(new Vector2Int(x, z), out var cell))
                    {
                        yield return cell;
                    }
                }
            }
        }

        /// <summary>
        /// The builder raised or lowered the ground: the trees standing on it within the area (X and Z) set again on
        /// the ground under them (`ground`: its height at a point).
        /// </summary>
        public void Refit(Rect area, System.Func<Vector2, float> ground)
        {
            if (!gathered)
            {
                return;
            }

            foreach (var cell in CellsOver(area))
            {
                foreach (var matrices in cell.Ready.Values)
                {
                    for (var i = 0; i < matrices.Length; i++)
                    {
                        var at = new Vector2(matrices[i].m03, matrices[i].m23);

                        if (area.Contains(at))
                        {
                            var y = ground(at);
                            cell.Bounds.Encapsulate(new Vector3(at.x, y, at.y));
                            matrices[i].m13 = y;
                        }
                    }
                }
            }
        }

        /// <summary>The builder felled the trees within a circle (X and Z): they are drawn no more. Returns them (their kind's prefab and place) to be put back.</summary>
        public List<(GameObject prefab, Matrix4x4 at)> Cut(Vector2 centre, float radius)
        {
            var cut = new List<(GameObject, Matrix4x4)>();

            if (!gathered)
            {
                return cut;
            }

            foreach (var cell in CellsOver(new Rect(centre.x - radius, centre.y - radius, radius * 2, radius * 2)))
            {
                foreach (var kind in cell.Ready.Keys.ToList())
                {
                    var matrices = cell.Ready[kind];
                    var kept = new List<Matrix4x4>(matrices.Length);

                    foreach (var m in matrices)
                    {
                        if ((new Vector2(m.m03, m.m23) - centre).sqrMagnitude <= radius * radius)
                        {
                            cut.Add((kind.Prefab, m));
                        }
                        else
                        {
                            kept.Add(m);
                        }
                    }

                    if (kept.Count != matrices.Length)
                    {
                        cell.Ready[kind] = kept.ToArray();
                    }
                }
            }

            return cut;
        }

        /// <summary>A tree drawn (planted by the builder, or a felled one put back): its kind's prefab (a terrain's tree prototype) and place.</summary>
        public void Add(GameObject prefab, Matrix4x4 at)
        {
            var kind = prefab != null && gathered ? KindOf(prefab) : null;

            if (kind == null)
            {
                return;
            }

            var position = new Vector3(at.m03, at.m13, at.m23);
            var cell = CellAt(position);
            var scale = at.lossyScale;
            cell.Bounds.Encapsulate(new Bounds(position + Vector3.up * 10 * scale.y, new Vector3(12 * scale.x, 20 * scale.y, 12 * scale.x)));
            cell.Ready[kind] = cell.Ready.TryGetValue(kind, out var matrices) ? matrices.Append(at).ToArray() : new[] { at };
        }

        /// <summary>Whether a tree is drawn nearer than a distance to a point (X and Z): the builder plants no closer.</summary>
        public bool AnyNear(Vector2 at, float distance)
        {
            if (!gathered)
            {
                return false;
            }

            foreach (var cell in CellsOver(new Rect(at.x - distance, at.y - distance, distance * 2, distance * 2)))
            {
                foreach (var matrices in cell.Ready.Values)
                {
                    foreach (var m in matrices)
                    {
                        if ((new Vector2(m.m03, m.m23) - at).sqrMagnitude < distance * distance)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>Whether the trees are drawn by this yet (else the terrains' own trees are what there is).</summary>
        public bool Gathered => gathered;

        void LateUpdate()
        {
            if (!gathered)
            {
                if (Terrain.activeTerrains.Length == 0)
                {
                    return;
                }

                using (Engine.Costs.Time("trees gathered"))
                {
                    Gather();
                }
            }

            var camera = Camera.main;

            if (camera == null || Distance <= 0)
            {
                return;
            }

            GeometryUtility.CalculateFrustumPlanes(camera, planes);
            var at = camera.transform.position;
            var reach = Mathf.CeilToInt(Distance / CellSize);
            var centre = new Vector2Int(Mathf.FloorToInt(at.x / CellSize), Mathf.FloorToInt(at.z / CellSize));
            var bias = QualitySettings.lodBias;

            for (var dz = -reach; dz <= reach; dz++)
            {
                for (var dx = -reach; dx <= reach; dx++)
                {
                    if (!cells.TryGetValue(new Vector2Int(centre.x + dx, centre.y + dz), out var cell))
                    {
                        continue;
                    }

                    var distance = Mathf.Sqrt(cell.Bounds.SqrDistance(at));

                    if (distance > Distance || !GeometryUtility.TestPlanesAABB(planes, cell.Bounds))
                    {
                        continue;
                    }

                    var level = distance < FullUpTo * bias ? 0 : distance < MiddleUpTo * bias ? 1 : 2;
                    var shadows = distance < ShadowsUpTo ? ShadowCastingMode.On : ShadowCastingMode.Off;

                    foreach (var pair in cell.Ready)
                    {
                        var mesh = pair.Key.Meshes[level];
                        var materials = pair.Key.Materials[level];

                        if (mesh == null)
                        {
                            continue;
                        }

                        var matrices = pair.Value;

                        for (var sub = 0; sub < mesh.subMeshCount && sub < materials.Length; sub++)
                        {
                            if (materials[sub] == null)
                            {
                                continue;
                            }

                            var parameters = new RenderParams(materials[sub]) { shadowCastingMode = shadows, receiveShadows = true, worldBounds = cell.Bounds };

                            for (var start = 0; start < matrices.Length; start += Batch)
                            {
                                Graphics.RenderMeshInstanced(parameters, mesh, sub, matrices, Mathf.Min(Batch, matrices.Length - start), start);
                            }
                        }
                    }
                }
            }
        }
    }
}
