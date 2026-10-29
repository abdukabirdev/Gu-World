using System.Collections.Generic;
using System.Linq;
using GuWorld.Render;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GuWorld.World
{
    /// <summary>
    /// The builder's woods: the trees and rocks of the terrains felled with a brush, or planted — the kind chosen on the
    /// page, no nearer to another than the spacing, sized as the wild ones grow. Kept as works in the order done
    /// (world.json) and done again at every start; each step remembers the terrains' trees it took out and put in, to be
    /// taken back. The terrains keep their trees for their trunks (solid); the drawn ones are TreeInstancer's.
    /// </summary>
    public partial class WorldBuilder
    {
        /// <summary>What a piece of work on the woods did: the terrains' trees it took out or put in (to take it back).</summary>
        class WoodsDone
        {
            public readonly List<(Terrain terrain, TreeInstance tree)> Felled = new List<(Terrain, TreeInstance)>();
            public readonly List<(Terrain terrain, TreeInstance tree)> Planted = new List<(Terrain, TreeInstance)>();
        }

        /// <summary>A step's works on the woods: done again in order, taken back in reverse (a reset's doing is taking all of them back).</summary>
        class WoodsEdit
        {
            public List<WoodsWork> Works = new List<WoodsWork>();
            public bool Removes;

            public bool Empty => Works.Count == 0;
        }

        /// <summary>Planting: no nearer to another tree than this, m; the trees' size against the wild ones; the kind (a terrain's tree prototype's name).</summary>
        public float Spacing = 8;
        public float PlantSize = 1;
        public string PlantKind = "Red Pine";
        readonly Dictionary<WoodsWork, WoodsDone> woodsDone = new Dictionary<WoodsWork, WoodsDone>();
        WoodsEdit woodsStroke;
        bool woodsStroking;
        Vector2 lastFelled;
        readonly List<Vector2> strokePlanted = new List<Vector2>();

        /// <summary>The kinds that can be planted (the first terrain's tree prototypes).</summary>
        public string[] PlantKinds
        {
            get
            {
                var terrain = Terrain.activeTerrains.FirstOrDefault(t => t.terrainData != null && t.terrainData.treePrototypes.Length > 0);

                return terrain == null ? new string[0] : terrain.terrainData.treePrototypes.Where(p => p.prefab != null).Select(p => p.prefab.name).Distinct().ToArray();
            }
        }

        public static string KindLabel(string name)
        {
            var n = name.ToLowerInvariant();

            return n.Contains("red pine") ? "Красная сосна" : n.Contains("fir") ? "Ель" : n.Contains("camphor") ? "Камфора" : n.Contains("cypress") ? "Кипарис" :
                n.Contains("bamboo") ? "Бамбук" : n.Contains("dead") ? "Сухое дерево" : n.Contains("shrub") ? "Куст" : n.Contains("boulder") ? "Валун" :
                n.Contains("crag") ? "Скала" : n.Contains("slab") ? "Каменная плита" : name;
        }

        /// <summary>How big a planted kind is against its shape (as the wild ones grow: trees four to five times, bamboo twice).</summary>
        static float KindScale(string name)
        {
            var n = name.ToLowerInvariant();

            return n.Contains("shrub") ? 1.6f : n.Contains("bamboo") ? 2.2f : n.Contains("dead") ? 3.6f :
                n.Contains("boulder") || n.Contains("crag") || n.Contains("slab") ? 1.8f : 4.4f;
        }

        /// <summary>The terrain whose ground is under a point (a village's own over the mountain's) and the ground's height there.</summary>
        static Terrain TerrainUnder(Vector2 p, out float y)
        {
            var ray = new Ray(new Vector3(p.x, 9000, p.y), Vector3.down);
            Terrain best = null;
            var nearest = float.PositiveInfinity;
            y = float.NaN;

            foreach (var terrain in Terrain.activeTerrains)
            {
                if (terrain.TryGetComponent<TerrainCollider>(out var collider) && collider.enabled && collider.Raycast(ray, out var hit, 12000) && hit.distance < nearest)
                {
                    nearest = hit.distance;
                    best = terrain;
                    y = hit.point.y;
                }
            }

            return best;
        }

        static int PrototypeOf(TerrainData data, string kind)
        {
            var prototypes = data.treePrototypes;

            for (var i = 0; i < prototypes.Length; i++)
            {
                if (prototypes[i].prefab != null && prototypes[i].prefab.name == kind)
                {
                    return i;
                }
            }

            return -1;
        }

        static Vector3 WorldOf(Terrain terrain, TreeInstance tree) => terrain.GetPosition() + Vector3.Scale(tree.position, terrain.terrainData.size);

        static Matrix4x4 Drawn(Terrain terrain, TreeInstance tree) =>
            Matrix4x4.TRS(WorldOf(terrain, tree), Quaternion.Euler(0, tree.rotation * Mathf.Rad2Deg, 0), new Vector3(tree.widthScale, tree.heightScale, tree.widthScale));

        static GameObject PrefabOf(Terrain terrain, TreeInstance tree)
        {
            var prototypes = terrain.terrainData.treePrototypes;

            return tree.prototypeIndex >= 0 && tree.prototypeIndex < prototypes.Length ? prototypes[tree.prototypeIndex].prefab : null;
        }

        static bool Overlaps(Terrain terrain, Vector2 centre, float radius)
        {
            var o = terrain.GetPosition();
            var size = terrain.terrainData.size;

            return centre.x + radius >= o.x && centre.x - radius <= o.x + size.x && centre.y + radius >= o.z && centre.y - radius <= o.z + size.z;
        }

        /// <summary>
        /// Works done to the terrains' trees, in order, each terrain's trees set once at the end (and, unless the stroke
        /// already did it as it went — `drawn` — to the drawn trees too); each work remembers what it did.
        /// </summary>
        void ApplyWorks(IList<WoodsWork> works, bool drawn)
        {
            var lists = new Dictionary<Terrain, List<TreeInstance>>();
            var instancer = TreeInstancer.Current;

            List<TreeInstance> TreesOf(Terrain terrain)
            {
                if (!lists.TryGetValue(terrain, out var list))
                {
                    lists[terrain] = list = new List<TreeInstance>(terrain.terrainData.treeInstances);
                }

                return list;
            }

            foreach (var work in works)
            {
                var done = new WoodsDone();
                woodsDone[work] = done;

                if (work.Plant)
                {
                    var terrain = TerrainUnder(new Vector2(work.X, work.Z), out var y);
                    var index = terrain != null ? PrototypeOf(terrain.terrainData, work.Kind) : -1;

                    if (index < 0)
                    {
                        continue;
                    }

                    var o = terrain.GetPosition();
                    var size = terrain.terrainData.size;
                    var tree = new TreeInstance
                    {
                        position = new Vector3((work.X - o.x) / size.x, (y - o.y) / size.y, (work.Z - o.z) / size.z),
                        widthScale = work.Width,
                        heightScale = work.Height,
                        rotation = work.Turn,
                        color = new Color32(225, 235, 225, 255),
                        lightmapColor = Color.white,
                        prototypeIndex = index,
                    };
                    TreesOf(terrain).Add(tree);
                    done.Planted.Add((terrain, tree));

                    if (!drawn && instancer != null)
                    {
                        instancer.Add(terrain.terrainData.treePrototypes[index].prefab, Drawn(terrain, tree));
                    }

                    continue;
                }

                var centre = new Vector2(work.X, work.Z);
                var r2 = work.Radius * work.Radius;

                if (!drawn && instancer != null)
                {
                    instancer.Cut(centre, work.Radius);
                }

                foreach (var terrain in Terrain.activeTerrains)
                {
                    if (!Overlaps(terrain, centre, work.Radius) || (terrain.terrainData.treeInstanceCount == 0 && !lists.ContainsKey(terrain)))
                    {
                        continue;
                    }

                    var o = terrain.GetPosition();
                    var size = terrain.terrainData.size;
                    var here = terrain;
                    TreesOf(terrain).RemoveAll(tree =>
                    {
                        var p = new Vector2(o.x + tree.position.x * size.x, o.z + tree.position.z * size.z);

                        if ((p - centre).sqrMagnitude > r2)
                        {
                            return false;
                        }

                        done.Felled.Add((here, tree));

                        return true;
                    });
                }
            }

            foreach (var pair in lists)
            {
                pair.Key.terrainData.SetTreeInstances(pair.Value.ToArray(), true);
            }
        }

        /// <summary>Works taken back, the last first: what they felled put back, what they planted taken out.</summary>
        void UnApplyWorks(IList<WoodsWork> works)
        {
            var lists = new Dictionary<Terrain, List<TreeInstance>>();
            var instancer = TreeInstancer.Current;

            List<TreeInstance> TreesOf(Terrain terrain)
            {
                if (!lists.TryGetValue(terrain, out var list))
                {
                    lists[terrain] = list = new List<TreeInstance>(terrain.terrainData.treeInstances);
                }

                return list;
            }

            for (var i = works.Count - 1; i >= 0; i--)
            {
                if (!woodsDone.TryGetValue(works[i], out var done))
                {
                    continue;
                }

                woodsDone.Remove(works[i]);

                foreach (var (terrain, tree) in done.Planted)
                {
                    if (terrain == null)
                    {
                        continue;
                    }

                    var list = TreesOf(terrain);
                    var at = list.FindIndex(t => t.prototypeIndex == tree.prototypeIndex && Mathf.Abs(t.position.x - tree.position.x) < 1e-6f && Mathf.Abs(t.position.z - tree.position.z) < 1e-6f);

                    if (at >= 0)
                    {
                        list.RemoveAt(at);
                    }

                    var p = WorldOf(terrain, tree);
                    instancer?.Cut(new Vector2(p.x, p.z), 0.05f);
                }

                foreach (var (terrain, tree) in done.Felled)
                {
                    if (terrain == null)
                    {
                        continue;
                    }

                    TreesOf(terrain).Add(tree);
                    instancer?.Add(PrefabOf(terrain, tree), Drawn(terrain, tree));
                }
            }

            foreach (var pair in lists)
            {
                pair.Key.terrainData.SetTreeInstances(pair.Value.ToArray(), true);
            }
        }

        void ReplayWoods(WoodsEdit edit, bool back)
        {
            // A step's own doing (done again, or a reset taken back) puts its works in; the other way takes them out.
            if (back == edit.Removes)
            {
                saved.Woods.AddRange(edit.Works);
                ApplyWorks(edit.Works, false);
            }
            else
            {
                UnApplyWorks(edit.Works);

                foreach (var work in edit.Works)
                {
                    saved.Woods.Remove(work);
                }
            }
        }

        /// <summary>The woods as they grew: every work taken back; returns the reset's step part (null when nothing was done).</summary>
        WoodsEdit UndoAllWoods()
        {
            EndWoodsStroke();

            if (saved.Woods.Count == 0)
            {
                return null;
            }

            var edit = new WoodsEdit { Works = saved.Woods.ToList(), Removes = true };
            UnApplyWorks(edit.Works);
            saved.Woods.Clear();

            return edit;
        }

        void LoadWoods()
        {
            if (saved.Woods.Count > 0)
            {
                ApplyWorks(saved.Woods, false);
            }
        }

        void WoodsTick(Keyboard keyboard, Mouse mouse, float dt, bool typing)
        {
            BrushKeys(keyboard, mouse, typing);
            var left = mouse.leftButton;

            if (!PickGround(out var point) || (PointerOnPage && !woodsStroking))
            {
                if (ring != null) ring.enabled = false;
                if (woodsStroking && !left.isPressed) EndWoodsStroke();

                return;
            }

            DrawRing(point, CurrentTool == Tool.Fell ? new Color(0.95f, 0.45f, 0.35f) : new Color(0.5f, 0.9f, 0.45f));

            if (left.wasPressedThisFrame && !PointerOnPage)
            {
                woodsStroking = true;
                woodsStroke = new WoodsEdit();
                lastFelled = new Vector2(float.MaxValue, float.MaxValue);
                strokePlanted.Clear();
            }

            if (!woodsStroking)
            {
                return;
            }

            if (!left.isPressed)
            {
                EndWoodsStroke();

                return;
            }

            var centre = new Vector2(point.x, point.z);

            if (CurrentTool == Tool.Fell)
            {
                // A circle every third of the brush along the way: the stroke's trees go as it passes.
                if ((centre - lastFelled).magnitude >= Radius * 0.3f)
                {
                    lastFelled = centre;
                    woodsStroke.Works.Add(new WoodsWork { X = centre.x, Z = centre.y, Radius = Radius });
                    TreeInstancer.Current?.Cut(centre, Radius);
                }
            }
            else
            {
                Plant(centre);
            }
        }

        /// <summary>A few tries a frame: a spot under the brush with no tree nearer than the spacing gets one.</summary>
        void Plant(Vector2 centre)
        {
            for (var i = 0; i < 3; i++)
            {
                var p = centre + Random.insideUnitCircle * Radius;
                var instancer = TreeInstancer.Current;

                if ((instancer != null && instancer.AnyNear(p, Spacing)) || strokePlanted.Any(q => (q - p).sqrMagnitude < Spacing * Spacing))
                {
                    continue;
                }

                var terrain = TerrainUnder(p, out var y);
                var index = terrain != null ? PrototypeOf(terrain.terrainData, PlantKind) : -1;

                if (index < 0)
                {
                    continue;
                }

                var scale = KindScale(PlantKind) * PlantSize * Random.Range(0.85f, 1.15f);
                var work = new WoodsWork { Plant = true, X = p.x, Z = p.y, Kind = PlantKind, Width = scale * Random.Range(0.9f, 1.1f), Height = scale, Turn = Random.value * Mathf.PI * 2 };
                woodsStroke.Works.Add(work);
                strokePlanted.Add(p);
                instancer?.Add(terrain.terrainData.treePrototypes[index].prefab,
                    Matrix4x4.TRS(new Vector3(p.x, y, p.y), Quaternion.Euler(0, work.Turn * Mathf.Rad2Deg, 0), new Vector3(work.Width, work.Height, work.Width)));
            }
        }

        /// <summary>A stroke on the woods finished: the terrains' trees changed at once, kept as one step.</summary>
        void EndWoodsStroke()
        {
            if (!woodsStroking)
            {
                return;
            }

            woodsStroking = false;
            var edit = woodsStroke;
            woodsStroke = null;
            strokePlanted.Clear();

            if (edit == null || edit.Empty)
            {
                return;
            }

            ApplyWorks(edit.Works, true);
            // Works that did nothing (a circle with no trees) are not kept.
            edit.Works.RemoveAll(w => woodsDone.TryGetValue(w, out var d) && d.Felled.Count == 0 && d.Planted.Count == 0);

            if (edit.Empty)
            {
                Status = CurrentTool == Tool.Fell ? "Здесь нечего рубить" : "Здесь не посадить: тесно или не та земля";

                return;
            }

            saved.Woods.AddRange(edit.Works);
            var felled = edit.Works.Sum(w => woodsDone.TryGetValue(w, out var d) ? d.Felled.Count : 0);
            var planted = edit.Works.Sum(w => woodsDone.TryGetValue(w, out var d) ? d.Planted.Count : 0);
            Push(new Step { Name = CurrentTool == Tool.Fell ? "Вырубить" : "Посадить", Woods = edit });
            Status = CurrentTool == Tool.Fell ? $"Вырублено: {felled}" : $"Посажено: {planted}";
        }
    }
}
