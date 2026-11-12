using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using GuWorld.Core.Gen;
using UnityEngine;
using UnityEngine.Rendering;

namespace GuWorld.World
{
    /// <summary>
    /// The grass, flowers, ferns and reeds round the hero, planted by Core (<see cref="Grass"/>) in cells
    /// of 24 m on worker threads as the hero walks, and drawn in instanced batches (no objects at all),
    /// swaying in the wind. Thinner farther out; how far and how thick by the quality.
    /// </summary>
    public class GrassField : MonoBehaviour
    {
        const float CellSize = 24;

        sealed class Cell
        {
            public readonly List<Matrix4x4>[] ByKind = new List<Matrix4x4>[Grass.Kinds];
            public bool Ready;
        }

        WorldPlan plan;
        float radius;
        float baseRadius;
        float detailUsed = 1;
        float density;
        Mesh[] meshes;
        Material material;
        readonly Dictionary<long, Cell> cells = new Dictionary<long, Cell>();
        readonly ConcurrentQueue<(long key, List<Tuft> tufts)> planted = new ConcurrentQueue<(long, List<Tuft>)>();
        readonly Matrix4x4[] chunk = new Matrix4x4[1023];
        Vector2Int lastCell = new Vector2Int(int.MinValue, int.MinValue);

        static long Key(int x, int z) => ((long)x << 32) ^ (uint)z;

        public static GrassField Make(Transform parent, WorldPlan plan)
        {
            var root = new GameObject("Grass");
            root.transform.SetParent(parent, false);
            var field = root.AddComponent<GrassField>();
            field.plan = plan;
            field.radius = Quality.Level >= 2 ? 70 : Quality.Level == 1 ? 46 : 28;
            field.baseRadius = field.radius;
            field.density = Quality.Level >= 2 ? 4f : Quality.Level == 1 ? 2.6f : 1.5f;
            field.meshes = new Mesh[Grass.Kinds];

            for (var kind = 0; kind < Grass.Kinds; kind++)
            {
                field.meshes[kind] = Meshes.From(Grass.Mesh(kind), "Grass " + kind);
            }

            field.material = Actors.Models.VertexLit(Color.white, 0.12f);

            return field;
        }

        void Update()
        {
            var hero = Game.Hero;

            if (hero == null)
            {
                return;
            }

            // What the workers have planted becomes matrices here (the engine's types belong to this thread).
            while (planted.TryDequeue(out var done))
            {
                if (!cells.TryGetValue(done.key, out var cell))
                {
                    continue;
                }

                for (var kind = 0; kind < Grass.Kinds; kind++)
                {
                    cell.ByKind[kind] = new List<Matrix4x4>();
                }

                foreach (var t in done.tufts)
                {
                    cell.ByKind[t.Kind].Add(Matrix4x4.TRS(new Vector3(t.X, t.Y - 0.02f, t.Z), Quaternion.Euler(0, t.Yaw, 0), Vector3.one * t.Scale));
                }

                cell.Ready = true;
            }

            var p = hero.transform.position;
            var here = new Vector2Int(Mathf.FloorToInt(p.x / CellSize), Mathf.FloorToInt(p.z / CellSize));

            // The governor's detail shortens the reach (and the cells beyond it go).
            if (Mathf.Abs(Governor.Detail - detailUsed) > 0.01f)
            {
                detailUsed = Governor.Detail;
                radius = baseRadius * Mathf.Lerp(0.55f, 1, (detailUsed - 0.4f) / 0.6f);
                lastCell = new Vector2Int(int.MinValue, int.MinValue);
            }

            if (here != lastCell)
            {
                lastCell = here;
                Refresh(p);
            }

            Draw();
        }

        void Refresh(Vector3 focus)
        {
            var reach = Mathf.CeilToInt(radius / CellSize);
            var keep = new HashSet<long>();

            for (var dz = -reach; dz <= reach; dz++)
            {
                for (var dx = -reach; dx <= reach; dx++)
                {
                    var cx = lastCell.x + dx;
                    var cz = lastCell.y + dz;
                    var centre = new Vector2((cx + 0.5f) * CellSize, (cz + 0.5f) * CellSize);
                    var distance = Vector2.Distance(centre, new Vector2(focus.x, focus.z));

                    if (distance > radius + CellSize * 0.7f)
                    {
                        continue;
                    }

                    var key = Key(cx, cz);
                    keep.Add(key);

                    if (cells.ContainsKey(key))
                    {
                        continue;
                    }

                    cells[key] = new Cell();
                    // Thinner beyond half the reach.
                    var thick = distance < radius * 0.5f ? density : density * 0.45f;
                    var world = plan;
                    var x0 = cx * CellSize;
                    var z0 = cz * CellSize;
                    Task.Run(() =>
                    {
                        var tufts = new List<Tuft>();

                        try
                        {
                            Grass.Plant(world, x0, z0, CellSize, thick, tufts);
                        }
                        catch (System.Exception)
                        {
                            tufts.Clear();
                        }

                        planted.Enqueue((key, tufts));
                    });
                }
            }

            var drop = new List<long>();

            foreach (var key in cells.Keys)
            {
                if (!keep.Contains(key))
                {
                    drop.Add(key);
                }
            }

            foreach (var key in drop)
            {
                cells.Remove(key);
            }
        }

        void Draw()
        {
            foreach (var cell in cells.Values)
            {
                if (!cell.Ready)
                {
                    continue;
                }

                for (var kind = 0; kind < Grass.Kinds; kind++)
                {
                    var list = cell.ByKind[kind];

                    for (var start = 0; start < list.Count; start += chunk.Length)
                    {
                        var count = Mathf.Min(chunk.Length, list.Count - start);
                        list.CopyTo(start, chunk, 0, count);
                        Graphics.DrawMeshInstanced(meshes[kind], 0, material, chunk, count, null, ShadowCastingMode.Off, true);
                    }
                }
            }
        }

        /// <summary>How many tufts are drawn now (for the autotest's log).</summary>
        public string Count()
        {
            var total = 0;

            foreach (var cell in cells.Values)
            {
                if (cell.Ready)
                {
                    foreach (var list in cell.ByKind)
                    {
                        total += list.Count;
                    }
                }
            }

            return $"grass: {total} tufts in {cells.Count} cells";
        }
    }
}
