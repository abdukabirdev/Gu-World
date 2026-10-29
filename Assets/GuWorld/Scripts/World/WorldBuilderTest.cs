using System;
using System.Linq;
using UnityEngine;

namespace GuWorld.World
{
    /// <summary>
    /// The builder's own check (`-page builder -buildtest` with a screenshot run): near the view a built house is moved,
    /// turned, scaled, removed and copied, the ground raised and painted, the woods felled and planted, everything reset
    /// — each taken back and done again — and what came of it written to the log ("Gu World builder test: …"). Nothing
    /// is saved.
    /// </summary>
    public partial class WorldBuilder
    {
        public void SelfTest()
        {
            demo = true;
            var failed = 0;

            void Check(string what, bool ok)
            {
                Debug.Log($"Gu World builder test: {(ok ? "ok" : "FAILED")} — {what}");

                if (!ok)
                {
                    failed++;
                }
            }

            static bool Near(Vector3 a, Vector3 b) => (a - b).magnitude < 0.02f;
            int Trees() => Terrain.activeTerrains.Sum(t => t.terrainData.treeInstanceCount);

            try
            {
                UnityEngine.Physics.SyncTransforms();
                var house = AllTakeable().Where(t => t.GetComponent<LODGroup>() != null && t.GetComponent<PlacedMark>() == null && Takes(t))
                    .OrderBy(t => (t.transform.position - focus).sqrMagnitude).FirstOrDefault();
                Check($"a built thing near the view: {(house != null ? house.name : "none")}", house != null);

                if (house != null)
                {
                    var start = house.transform.position;
                    var turn = house.transform.rotation;
                    var scale = house.transform.localScale;
                    var shift = new Vector3(10, 0, 0);

                    Select(house);
                    pressPoint = BoundsOf(house).center;
                    StartDrag();

                    foreach (var d in dragged)
                    {
                        d.Thing.transform.position = d.At + shift;
                    }

                    FinishDrag();
                    Check("dragged 10 m", Near(house.transform.position, start + shift));
                    Check("its square held near its new place", ChunkStreamer.Current.Held.Count > 0);
                    Undo();
                    Check("Ctrl+Z puts it back", Near(house.transform.position, start));
                    Redo();
                    Check("Ctrl+Y moves it again", Near(house.transform.position, start + shift));
                    Undo();

                    Select(house);
                    Turn(90);
                    Check("turned 90°", Quaternion.Angle(house.transform.rotation, turn) > 89);
                    Undo();
                    Check("turn taken back", Quaternion.Angle(house.transform.rotation, turn) < 0.5f && Near(house.transform.position, start));

                    Select(house);
                    SetChosenSize(SizeOf(house) * 1.5f);
                    Check($"scaled ×1.5 ({house.transform.localScale.x:0.00})", Mathf.Abs(house.transform.localScale.x - scale.x * 1.5f) < 0.01f);
                    Undo();
                    Check("size taken back", Near(house.transform.localScale, scale) && Near(house.transform.position, start));

                    Select(house);
                    RemoveChosen();
                    Check("removed", !house.activeSelf);
                    Undo();
                    Check("removal taken back", house.activeSelf && Near(house.transform.position, start));

                    var placed = saved.Placed.Count;
                    Select(house);
                    Duplicate();
                    Check($"copied ({CatalogId(house) ?? "no model"})", CatalogId(house) == null || (saved.Placed.Count == placed + 1 && selection.Count == 1 && selection[0].GetComponent<PlacedMark>() != null));
                    Undo();
                    Check("copy taken back", saved.Placed.Count == placed);

                    // Moved, then everything reset, then the reset taken back.
                    Select(house);
                    StartDrag();

                    foreach (var d in dragged)
                    {
                        d.Thing.transform.position = d.At + shift;
                    }

                    FinishDrag();
                    ResetEverything();
                    Check("reset puts the house back", Near(house.transform.position, start) && changes.Count == 0);
                    Undo();
                    Check("reset taken back", Near(house.transform.position, start + shift));
                    Undo();
                    Check("and the move taken back", Near(house.transform.position, start));
                }

                // The ground.
                var p = new Vector2(focus.x + 30, focus.z + 30);
                var before = GroundY(p);
                Check($"ground found at {p}", !float.IsNaN(before));

                if (!float.IsNaN(before))
                {
                    CurrentTool = Tool.Raise;
                    Radius = 6;
                    Strength = 2;
                    stroking = true;
                    strokeTarget = before;

                    for (var i = 0; i < 10; i++)
                    {
                        Brush(new Vector3(p.x, before, p.y), 0.1f);
                    }

                    EndStroke();
                    var raised = GroundY(p);
                    Check($"raised {before:0.00} → {raised:0.00}", raised > before + 0.5f);
                    Undo();
                    Check($"raise taken back ({GroundY(p):0.00})", Mathf.Abs(GroundY(p) - before) < 0.05f);
                    Redo();
                    Check("raise done again", GroundY(p) > before + 0.5f);
                    ResetGroundAll();
                    Check("ground reset", Mathf.Abs(GroundY(p) - before) < 0.05f);
                    Undo();
                    Check("ground reset taken back", GroundY(p) > before + 0.5f);
                    Undo();
                    Undo();
                    Check("all back", Mathf.Abs(GroundY(p) - before) < 0.05f);

                    var layers = PaintLayers;
                    Check($"layers to paint: {layers.Length}", layers.Length > 0);

                    if (layers.Length > 0)
                    {
                        CurrentTool = Tool.Paint;
                        PaintLayer = layers[layers.Length - 1];

                        for (var i = 0; i < 5; i++)
                        {
                            Brush(new Vector3(p.x, before, p.y), 0.2f);
                        }

                        EndStroke();
                        Check("painted (a step kept)", undo.Count > 0 && undo[undo.Count - 1].Ground.Any(g => g.Paint));
                        Undo();
                    }
                }

                // The woods.
                var trees = Trees();
                // Felled round the nearest wild tree to the view (the village itself has none).
                var nearest = new Vector2(focus.x, focus.z);
                var best = float.PositiveInfinity;

                foreach (var terrain in Terrain.activeTerrains)
                {
                    if (!Overlaps(terrain, new Vector2(focus.x, focus.z), 1500))
                    {
                        continue;
                    }

                    foreach (var tree in terrain.terrainData.treeInstances)
                    {
                        var at = WorldOf(terrain, tree);
                        var d = (new Vector2(at.x, at.z) - new Vector2(focus.x, focus.z)).sqrMagnitude;

                        if (d < best)
                        {
                            best = d;
                            nearest = new Vector2(at.x, at.z);
                        }
                    }
                }

                var fell = new WoodsEdit();
                fell.Works.Add(new WoodsWork { X = nearest.x, Z = nearest.y, Radius = 25 });
                ApplyWorks(fell.Works, false);
                saved.Woods.AddRange(fell.Works);
                Push(new Step { Name = "Вырубить", Woods = fell });
                var felled = woodsDone[fell.Works[0]].Felled.Count;
                Check($"felled {felled} of {trees} round {nearest}", felled > 0 && Trees() == trees - felled);
                Undo();
                Check($"felling taken back ({Trees()})", Trees() == trees);

                CurrentTool = Tool.Plant;
                Spacing = 3;
                PlantKind = PlantKinds.FirstOrDefault() ?? PlantKind;
                woodsStroking = true;
                woodsStroke = new WoodsEdit();

                for (var i = 0; i < 40; i++)
                {
                    Plant(new Vector2(focus.x + 60, focus.z + 60));
                }

                EndWoodsStroke();
                Check($"planted {Trees() - trees} ({KindLabel(PlantKind)})", Trees() > trees);
                ResetWoods();
                Check("woods reset", Trees() == trees);
                Undo();
                Check("woods reset taken back", Trees() > trees);
                Undo();
                Check("planting taken back", Trees() == trees);
                CurrentTool = Tool.Select;
            }
            catch (Exception e)
            {
                failed++;
                Debug.LogError($"Gu World builder test: FAILED — {e}");
            }

            Debug.Log($"Gu World builder test: done, {failed} failed");
        }
    }
}
