using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace GuWorld.World
{
    /// <summary>
    /// The builder's things: chosen by a click (Shift adds), by a frame dragged over empty ground, or by the catalog
    /// to put down. The chosen are dragged over a level plane through the point grabbed (they follow the mouse exactly,
    /// and with "on the ground" ride the ground under them at the height they stood above it), turned about their
    /// middle, scaled, raised, copied, removed, put back where they were built; a model from the catalog follows the
    /// mouse until a click puts it down; stairs, walls, fences and paths are stretched between two clicks.
    /// </summary>
    public partial class WorldBuilder
    {
        readonly List<GameObject> selection = new List<GameObject>();
        GameObject hovered;

        // The left button's press: where, on what, and whether it became a drag or a frame.
        bool pressing;
        Vector2 pressAt;
        Vector3 pressPoint;
        GameObject pressThing;
        bool pressShift;
        bool dragging;
        bool framing;

        /// <summary>What is dragged: each thing, where it began, its middle's start, its height above the ground there, and its change.</summary>
        class Dragged
        {
            public GameObject Thing;
            public Vector3 At;
            public Vector3 Middle;
            public float Lift;
            public ThingEdit Edit;
        }

        readonly List<Dragged> dragged = new List<Dragged>();
        Vector3 dragGrip;

        // Putting down and stretching.
        GameObject ghost;
        string ghostId;
        float ghostYaw;
        float heightOffset;
        Vector3? stretchFrom;
        readonly List<GameObject> stretchGhosts = new List<GameObject>();

        /// <summary>The size a model is put down at (the catalog's), and stretched pieces' width.</summary>
        public float ScaleChoice = 1;
        /// <summary>The grid things snap to, m (0: none).</summary>
        public float GridStep;
        /// <summary>Dragged things ride the ground under them (else they keep their height).</summary>
        public bool FollowGround = true;
        /// <summary>Each model put down turned at random (trees, rocks, crowds of props).</summary>
        public bool RandomTurn;

        /// <summary>Copied things: their model, place from the copy's middle on the ground, turn and size.</summary>
        readonly List<(string id, Vector3 offset, Vector3 euler, Vector3 scale)> clipboard = new List<(string, Vector3, Vector3, Vector3)>();

        // The lines drawn: a box round each chosen thing and the one under the mouse.
        readonly List<LineRenderer> boxes = new List<LineRenderer>();
        LineRenderer hoverBox;
        static readonly Color ChosenColour = new Color(0.45f, 0.85f, 1f, 1f);
        static readonly Color HoverColour = new Color(1f, 0.95f, 0.75f, 0.7f);

        // ── What the page shows ────────────────────────────────────────

        public int ChosenCount => selection.Count;

        public string ChosenName => selection.Count == 0 ? "" : selection.Count == 1 ? Describe(selection[0]) : $"Выбрано: {selection.Count}";

        /// <summary>The chosen thing's place, turn and size (one), or the chosen things' middle (many).</summary>
        public string ChosenInfo
        {
            get
            {
                if (selection.Count == 0 || selection[0] == null)
                {
                    return "";
                }

                if (selection.Count > 1)
                {
                    var b = BoundsOf(selection);

                    return string.Format(CultureInfo.InvariantCulture, "Середина: X {0:0.0} · Z {1:0.0}\nВместе {2:0} × {3:0} м", b.center.x, b.center.z, b.size.x, b.size.z);
                }

                var t = selection[0].transform;
                var size = BoundsOf(selection[0]).size;
                var built = selection[0].GetComponent<PlacedMark>() == null;

                return string.Format(CultureInfo.InvariantCulture, "X {0:0.0} · Y {1:0.0} · Z {2:0.0}\nПоворот {3:0}° · размер ×{4:0.00} · {5:0}×{6:0}×{7:0} м\n{8}",
                    t.position.x, t.position.y, t.position.z, t.eulerAngles.y, SizeOf(selection[0]), size.x, size.z, size.y,
                    built ? "Постройка деревни" : "Поставлено вами");
            }
        }

        /// <summary>The first chosen thing's size (its scale, averaged over the axes): the page's size slider.</summary>
        public float ChosenSize => selection.Count > 0 && selection[0] != null ? SizeOf(selection[0]) : 1;

        public bool ChosenHasBuilt => selection.Any(t => t != null && t.GetComponent<PlacedMark>() == null);

        static float SizeOf(GameObject thing)
        {
            var s = thing.transform.localScale;

            return (Mathf.Abs(s.x) + Mathf.Abs(s.y) + Mathf.Abs(s.z)) / 3;
        }

        static string Describe(GameObject thing)
        {
            var catalog = BuildCatalog.Load();
            var id = CatalogId(thing);
            var entry = id != null ? catalog.Find(id) : null;

            return entry != null ? entry.Label : thing.name;
        }

        /// <summary>The catalog's model a thing is (put down by the builder, or a built house of the artist's models), or null.</summary>
        static string CatalogId(GameObject thing)
        {
            if (thing.GetComponent<PlacedMark>() is PlacedMark mark)
            {
                return mark.Data.Id;
            }

            var catalog = BuildCatalog.Load();

            if (catalog.Find(thing.name) != null)
            {
                return thing.name;
            }

            foreach (Transform child in thing.transform)
            {
                if (catalog.Find(child.name) != null)
                {
                    return child.name;
                }
            }

            return null;
        }

        // ── Finding things ─────────────────────────────────────────────

        /// <summary>The thing a collider belongs to that the builder can take: a thing put down, else a built model (its level of detail) or the thing streamed on its own; not the ground, not what is too big to be a thing (over 200 m).</summary>
        GameObject Thing(Collider collider)
        {
            if (collider == null || collider is TerrainCollider)
            {
                return null;
            }

            var mark = collider.GetComponentInParent<PlacedMark>();

            if (mark != null)
            {
                return mark.gameObject;
            }

            var unit = UnitOf(collider.transform);

            if (unit == null)
            {
                return null;
            }

            var group = collider.GetComponentInParent<LODGroup>();
            var thing = group != null && group.transform.IsChildOf(unit.transform) ? group.gameObject : unit;

            return Takes(thing) ? thing : null;
        }

        static bool Takes(GameObject thing)
        {
            var size = BoundsOf(thing).size;

            return size.x <= 200 && size.z <= 200;
        }

        /// <summary>The thing streamed on its own (a child of a square) that holds a part, or null (the hero, a beast, the sky).</summary>
        static GameObject UnitOf(Transform part)
        {
            var streamer = ChunkStreamer.Current != null ? ChunkStreamer.Current.transform : null;

            if (streamer == null)
            {
                return null;
            }

            while (part != null && part.parent != null && part.parent.parent != streamer)
            {
                part = part.parent;
            }

            return part != null && part.parent != null && part.parent.parent == streamer ? part.gameObject : null;
        }

        /// <summary>Every thing in the world the builder can take now: the squares' things (not the holders of several models) and the things put down.</summary>
        IEnumerable<GameObject> AllTakeable()
        {
            if (ChunkStreamer.Current != null)
            {
                foreach (var square in ChunkStreamer.Current.Instances)
                {
                    foreach (var thing in Takeable(square))
                    {
                        if (thing.activeInHierarchy && (thing.GetComponent<LODGroup>() != null || thing.GetComponentInChildren<LODGroup>() == null))
                        {
                            yield return thing;
                        }
                    }
                }
            }

            if (placedRoot != null)
            {
                foreach (Transform placed in placedRoot)
                {
                    yield return placed.gameObject;
                }
            }
        }

        /// <summary>
        /// The thing under the mouse (and the point on it): the nearest solid one before the ground; with `deep`, also a
        /// small one without a solid shape whose box the mouse is in (a lantern, a jar).
        /// </summary>
        GameObject PickThing(out Vector3 point, bool deep)
        {
            point = default;

            if (eye == null || Mouse.current == null)
            {
                return null;
            }

            var ray = MouseRay;
            var hits = UnityEngine.Physics.RaycastAll(ray, 8000, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            var groundAt = float.PositiveInfinity;

            foreach (var hit in hits)
            {
                if (hit.collider is TerrainCollider)
                {
                    groundAt = hit.distance;
                    point = hit.point;

                    break;
                }

                var thing = Thing(hit.collider);

                if (thing != null)
                {
                    point = hit.point;

                    return thing;
                }
            }

            if (!deep)
            {
                return null;
            }

            GameObject best = null;
            var bestAt = groundAt;

            foreach (var thing in AllTakeable())
            {
                var b = BoundsOf(thing);

                if (b.size.x < 40 && b.size.z < 40 && b.IntersectRay(ray, out var d) && d < bestAt)
                {
                    best = thing;
                    bestAt = d;
                }
            }

            if (best != null)
            {
                point = ray.GetPoint(bestAt);
            }

            return best;
        }

        static Bounds BoundsOf(GameObject thing)
        {
            var has = false;
            var bounds = new Bounds(thing.transform.position, Vector3.zero);

            foreach (var renderer in thing.GetComponentsInChildren<Renderer>())
            {
                if (renderer is LineRenderer || renderer is ParticleSystemRenderer)
                {
                    continue;
                }

                if (has)
                {
                    bounds.Encapsulate(renderer.bounds);
                }
                else
                {
                    bounds = renderer.bounds;
                    has = true;
                }
            }

            if (!has)
            {
                foreach (var collider in thing.GetComponentsInChildren<Collider>())
                {
                    if (has)
                    {
                        bounds.Encapsulate(collider.bounds);
                    }
                    else
                    {
                        bounds = collider.bounds;
                        has = true;
                    }
                }
            }

            return bounds;
        }

        static Bounds BoundsOf(IEnumerable<GameObject> things)
        {
            var has = false;
            var bounds = new Bounds();

            foreach (var thing in things)
            {
                if (thing == null)
                {
                    continue;
                }

                var b = BoundsOf(thing);

                if (has)
                {
                    bounds.Encapsulate(b);
                }
                else
                {
                    bounds = b;
                    has = true;
                }
            }

            return bounds;
        }

        // ── Choosing ───────────────────────────────────────────────────

        void Select(GameObject thing)
        {
            selection.Clear();

            if (thing != null)
            {
                selection.Add(thing);
            }

            heightOffset = 0;
            Touch();
        }

        /// <summary>A thing added to the chosen (or taken out of them); a holder and what it holds are never chosen together.</summary>
        void Toggle(GameObject thing)
        {
            if (selection.Remove(thing))
            {
                Touch();

                return;
            }

            selection.RemoveAll(t => t == null || t.transform.IsChildOf(thing.transform) || thing.transform.IsChildOf(t.transform));
            selection.Add(thing);
            Touch();
        }

        Vector3 Snapped(Vector3 p) => GridStep > 0 ? new Vector3(Mathf.Round(p.x / GridStep) * GridStep, p.y, Mathf.Round(p.z / GridStep) * GridStep) : p;

        void ThingsTick(Keyboard keyboard, Mouse mouse, bool typing)
        {
            switch (CurrentTool)
            {
                case Tool.Place: PlaceTick(keyboard, mouse, typing); break;
                case Tool.Stretch: StretchTick(keyboard, mouse, typing); break;
                default: SelectTick(keyboard, mouse, typing); break;
            }
        }

        void SelectTick(Keyboard keyboard, Mouse mouse, bool typing)
        {
            var left = mouse.leftButton;
            var at = mouse.position.ReadValue();

            if (left.wasPressedThisFrame && !PointerOnPage)
            {
                pressing = true;
                pressAt = at;
                pressShift = keyboard.shiftKey.isPressed;
                pressThing = PickThing(out pressPoint, true);

                // A thing not chosen yet is chosen at once: dragging it moves it.
                if (pressThing != null && !pressShift && !selection.Contains(pressThing))
                {
                    Select(pressThing);
                }
            }

            if (pressing && left.isPressed && !dragging && !framing && (at - pressAt).magnitude > 6)
            {
                if (pressThing != null && !pressShift)
                {
                    StartDrag();
                }
                else
                {
                    framing = true;
                }
            }

            if (dragging)
            {
                DragTick();
            }

            if (pressing && !left.isPressed)
            {
                if (dragging)
                {
                    FinishDrag();
                }
                else if (framing)
                {
                    FrameSelect(pressAt, at, pressShift);
                }
                else if (pressThing != null)
                {
                    if (pressShift) Toggle(pressThing);
                    else Select(pressThing);
                }
                else if (!pressShift)
                {
                    Select(null);
                }

                pressing = false;
                framing = false;
                pressThing = null;
            }

            hovered = !pressing && !PointerOnPage ? PickThing(out _, false) : null;

            if (!typing)
            {
                ChosenKeys(keyboard, mouse);
            }
        }

        void StartDrag()
        {
            dragging = true;
            dragged.Clear();
            dragGrip = pressPoint;

            foreach (var thing in selection)
            {
                if (thing == null)
                {
                    continue;
                }

                var middle = BoundsOf(thing).center;
                var ground = GroundY(new Vector2(middle.x, middle.z));
                dragged.Add(new Dragged
                {
                    Thing = thing, At = thing.transform.position, Middle = middle,
                    Lift = float.IsNaN(ground) ? float.NaN : thing.transform.position.y - ground, Edit = StartEdit(thing),
                });
            }
        }

        void DragTick()
        {
            if (!OnPlane(dragGrip.y, out var now) || Vector3.Distance(now, eye.transform.position) > 4000 || dragged.Count == 0)
            {
                return;
            }

            var shift = now - dragGrip;
            shift.y = 0;

            if (GridStep > 0)
            {
                var lead = Snapped(dragged[0].At + shift);
                shift = new Vector3(lead.x - dragged[0].At.x, 0, lead.z - dragged[0].At.z);
            }

            foreach (var d in dragged)
            {
                if (d.Thing == null)
                {
                    continue;
                }

                var to = d.At + shift;

                if (FollowGround && !float.IsNaN(d.Lift))
                {
                    var ground = GroundY(new Vector2(d.Middle.x + shift.x, d.Middle.z + shift.z));

                    if (!float.IsNaN(ground))
                    {
                        to.y = ground + d.Lift;
                    }
                }

                d.Thing.transform.position = to;
            }

            Status = string.Format(CultureInfo.InvariantCulture, "Сдвиг: {0:0.0} м", shift.magnitude);
        }

        void FinishDrag()
        {
            dragging = false;
            var step = new Step { Name = "Сдвинуть" };

            foreach (var d in dragged)
            {
                if (d.Thing == null)
                {
                    continue;
                }

                Record(d.Thing);
                Finish(d.Edit);
                step.Things.Add(d.Edit);
            }

            dragged.Clear();
            Push(step);
            UpdateHeld();
            UnityEngine.Physics.SyncTransforms();
        }

        void CancelDrag()
        {
            if (dragging)
            {
                foreach (var d in dragged)
                {
                    if (d.Thing != null)
                    {
                        d.Thing.transform.position = d.At;
                    }
                }
            }

            dragged.Clear();
            dragging = false;
            framing = false;
            pressing = false;
            pressThing = null;
        }

        /// <summary>The things whose middles are inside the frame dragged on the screen chosen (with Shift, added to the chosen).</summary>
        void FrameSelect(Vector2 a, Vector2 b, bool add)
        {
            var frame = Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));

            if (!add)
            {
                selection.Clear();
            }

            foreach (var thing in AllTakeable())
            {
                if (selection.Contains(thing))
                {
                    continue;
                }

                var bounds = BoundsOf(thing);

                if (bounds.size.x > 200 || bounds.size.z > 200)
                {
                    continue;
                }

                var screen = eye.WorldToScreenPoint(bounds.center);

                if (screen.z > 0 && frame.Contains(new Vector2(screen.x, screen.y)))
                {
                    selection.Add(thing);
                }
            }

            Status = selection.Count > 0 ? $"Выбрано рамкой: {selection.Count}" : "В рамку ничего не попало";
            Touch();
        }

        /// <summary>The keys on the chosen things.</summary>
        void ChosenKeys(Keyboard keyboard, Mouse mouse)
        {
            var ctrl = keyboard.ctrlKey.isPressed;
            var shift = keyboard.shiftKey.isPressed;
            var wheel = PointerOnPage ? 0 : mouse.scroll.ReadValue().y / 120f;

            if (ctrl && keyboard.vKey.wasPressedThisFrame)
            {
                Paste();

                return;
            }

            if (selection.Count == 0 || dragging)
            {
                return;
            }

            if (keyboard.rKey.wasPressedThisFrame) Turn(shift ? 90 : 15);
            if (keyboard.tKey.wasPressedThisFrame) Turn(shift ? -90 : -15);
            if (keyboard.altKey.isPressed && wheel != 0) Turn(wheel * 5);
            if (keyboard.equalsKey.wasPressedThisFrame || keyboard.numpadPlusKey.wasPressedThisFrame) ScaleBy(1.1f);
            if (keyboard.minusKey.wasPressedThisFrame || keyboard.numpadMinusKey.wasPressedThisFrame) ScaleBy(1 / 1.1f);
            if (ctrl && wheel != 0) ScaleBy(Mathf.Pow(1.05f, wheel));
            if (keyboard.pageUpKey.wasPressedThisFrame) Lift(shift ? 1 : 0.1f);
            if (keyboard.pageDownKey.wasPressedThisFrame) Lift(shift ? -1 : -0.1f);

            if (keyboard.deleteKey.wasPressedThisFrame || keyboard.backspaceKey.wasPressedThisFrame)
            {
                RemoveChosen();
            }
            else if (ctrl && keyboard.dKey.wasPressedThisFrame)
            {
                Duplicate();
            }
            else if (ctrl && keyboard.cKey.wasPressedThisFrame)
            {
                Copy();
            }
        }

        // ── Changing the chosen ────────────────────────────────────────

        /// <summary>A change to every chosen thing, kept as one step (joined to the last when it is the same change a moment later).</summary>
        void Change(string name, System.Action<GameObject> change, bool join = true)
        {
            selection.RemoveAll(t => t == null);

            if (selection.Count == 0)
            {
                return;
            }

            var step = new Step { Name = name };

            foreach (var thing in selection)
            {
                var edit = StartEdit(thing);
                change(thing);
                Record(thing);
                Finish(edit);
                step.Things.Add(edit);
            }

            if (join) PushOrJoin(step);
            else Push(step);

            UpdateHeld();
            UnityEngine.Physics.SyncTransforms();
        }

        /// <summary>The chosen things turned together about their middle (one thing: about its own middle, not its origin).</summary>
        public void Turn(float degrees)
        {
            if (CurrentTool == Tool.Place)
            {
                ghostYaw += degrees;

                return;
            }

            var middle = BoundsOf(selection).center;
            var turn = Quaternion.Euler(0, degrees, 0);
            Change("Повернуть", thing =>
            {
                var t = thing.transform;
                t.SetPositionAndRotation(middle + turn * (t.position - middle), turn * t.rotation);
            });
            Status = $"Повёрнуто на {degrees:0}°";
        }

        /// <summary>Each chosen thing scaled about its own middle (it stays where it stands).</summary>
        public void ScaleBy(float factor)
        {
            if (CurrentTool == Tool.Place)
            {
                ScaleChoice = Mathf.Clamp(ScaleChoice * factor, 0.1f, 10);

                return;
            }

            Change("Размер", thing =>
            {
                var t = thing.transform;
                var s = t.localScale * factor;

                if (Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z)) > 30 || Mathf.Min(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z)) < 0.03f)
                {
                    return;
                }

                var middle = BoundsOf(thing).center;
                var p = t.position;
                t.position = new Vector3(middle.x + (p.x - middle.x) * factor, p.y, middle.z + (p.z - middle.z) * factor);
                t.localScale = s;
            });
            Status = string.Format(CultureInfo.InvariantCulture, "Размер ×{0:0.00}", ChosenSize);
        }

        /// <summary>The page's size slider: the first chosen thing to this size, the others in proportion.</summary>
        public void SetChosenSize(float size)
        {
            var now = ChosenSize;

            if (now > 0.001f && Mathf.Abs(size - now) > 0.001f)
            {
                ScaleBy(size / now);
            }
        }

        public void Lift(float metres)
        {
            if (CurrentTool == Tool.Place)
            {
                heightOffset += metres;

                return;
            }

            Change("Выше/ниже", thing => thing.transform.position += Vector3.up * metres);
            Status = string.Format(CultureInfo.InvariantCulture, "Высота: {0:+0.0;-0.0} м", metres);
        }

        /// <summary>The chosen things set down on the ground under their middles.</summary>
        public void DropToGround()
        {
            Change("На землю", thing =>
            {
                var middle = BoundsOf(thing).center;
                var ground = GroundY(new Vector2(middle.x, middle.z));

                if (!float.IsNaN(ground))
                {
                    var p = thing.transform.position;
                    thing.transform.position = new Vector3(p.x, ground, p.z);
                }
            }, false);
            Status = "Поставлено на землю";
        }

        public void RemoveChosen()
        {
            selection.RemoveAll(t => t == null);

            if (selection.Count == 0)
            {
                return;
            }

            var step = new Step { Name = "Убрать" };

            foreach (var thing in selection.ToList())
            {
                var edit = StartEdit(thing);

                if (edit.Placed != null)
                {
                    edit.After = new ThingState { Here = false };
                    SetState(edit, edit.After);
                }
                else
                {
                    thing.SetActive(false);
                    Record(thing);
                    Finish(edit);
                }

                step.Things.Add(edit);
            }

            Status = $"Убрано: {step.Things.Count} (вернуть — Ctrl+Z)";
            Select(null);
            Push(step);
            UpdateHeld();
        }

        /// <summary>The chosen built things back where they were built.</summary>
        public void PutBack()
        {
            var step = new Step { Name = "На своё место" };

            foreach (var thing in selection.ToList())
            {
                if (thing == null || thing.GetComponent<PlacedMark>() != null)
                {
                    continue;
                }

                var edit = StartEdit(thing);
                edit.After = new ThingState { Here = true, AsBuilt = true };
                SetState(edit, edit.After);
                step.Things.Add(edit);
            }

            Status = step.Things.Count > 0 ? "Возвращено на своё место" : "Это поставлено вами — у него нет «своего места»; его можно убрать (Del)";
            Push(step);
            UpdateHeld();
            UnityEngine.Physics.SyncTransforms();
        }

        /// <summary>New things put down as one step (each a record, made in the world); they become the chosen.</summary>
        void PutDown(string name, IEnumerable<Placed> pieces, bool choose)
        {
            var step = new Step { Name = name };
            var made = new List<GameObject>();

            foreach (var placed in pieces)
            {
                var edit = new ThingEdit { Placed = placed, Before = new ThingState { Here = false } };
                saved.Placed.Add(placed);
                var thing = Make(placed);
                Finish(edit);
                step.Things.Add(edit);

                if (thing != null)
                {
                    made.Add(thing);
                }
            }

            Push(step);

            if (choose)
            {
                selection.Clear();
                selection.AddRange(made);
                Touch();
            }

            UnityEngine.Physics.SyncTransforms();
        }

        /// <summary>Ctrl+D: the chosen things copied beside them (the artist's models only; the copies are chosen, to be dragged).</summary>
        public void Duplicate()
        {
            var bounds = BoundsOf(selection);
            var aside = Quaternion.Euler(0, viewYaw, 0) * Vector3.right * Mathf.Clamp(bounds.size.x + 2, 2, 60);
            var pieces = new List<Placed>();
            var skipped = 0;

            foreach (var thing in selection)
            {
                var id = thing != null ? CatalogId(thing) : null;

                if (id == null)
                {
                    skipped++;
                    continue;
                }

                var t = thing.transform;
                var at = t.position + aside;
                var middle = BoundsOf(thing).center;
                var groundThere = GroundY(new Vector2(middle.x, middle.z));
                var groundHere = GroundY(new Vector2(middle.x + aside.x, middle.z + aside.z));

                if (FollowGround && !float.IsNaN(groundThere) && !float.IsNaN(groundHere))
                {
                    at.y += groundHere - groundThere;
                }

                pieces.Add(new Placed { Id = id, At = at, Euler = t.eulerAngles, Scale = t.localScale });
            }

            if (pieces.Count == 0)
            {
                Status = "Это нельзя скопировать: оно не из моделей каталога";

                return;
            }

            PutDown("Копия", pieces, true);
            Status = skipped > 0 ? $"Скопировано {pieces.Count}, не копируется {skipped} (не из каталога)" : $"Копия: {pieces.Count} — тащите её куда нужно";
        }

        public void Copy()
        {
            clipboard.Clear();
            var bounds = BoundsOf(selection);
            var ground = GroundY(new Vector2(bounds.center.x, bounds.center.z), bounds.min.y);
            var middle = new Vector3(bounds.center.x, ground, bounds.center.z);

            foreach (var thing in selection)
            {
                var id = thing != null ? CatalogId(thing) : null;

                if (id != null)
                {
                    clipboard.Add((id, thing.transform.position - middle, thing.transform.eulerAngles, thing.transform.localScale));
                }
            }

            Status = clipboard.Count > 0 ? $"Скопировано: {clipboard.Count} — Ctrl+V вставит под мышью" : "Это нельзя скопировать: оно не из моделей каталога";
        }

        public void Paste()
        {
            if (clipboard.Count == 0)
            {
                Status = "Нечего вставлять: сначала выберите и скопируйте (Ctrl+C)";

                return;
            }

            if (PointerOnPage || !PickGround(out var at))
            {
                Status = "Наведите мышь на землю, куда вставить";

                return;
            }

            var middle = Snapped(at);
            middle.y = at.y;
            PutDown("Вставить", clipboard.Select(c => new Placed { Id = c.id, At = middle + c.offset, Euler = c.euler, Scale = c.scale }).ToList(), true);
            Status = $"Вставлено: {clipboard.Count}";
        }

        // ── Putting down ───────────────────────────────────────────────

        static GameObject Ghost(BuildCatalog.Entry entry)
        {
            var g = Instantiate(entry.Prefab);
            g.name = "Ghost";

            foreach (var c in g.GetComponentsInChildren<Collider>())
            {
                c.enabled = false;
            }

            foreach (var b in g.GetComponentsInChildren<MonoBehaviour>())
            {
                b.enabled = false;
            }

            return g;
        }

        void ClearGhosts()
        {
            if (ghost != null) Destroy(ghost);
            ghost = null;
            ghostId = null;

            foreach (var g in stretchGhosts)
            {
                if (g != null) Destroy(g);
            }

            stretchGhosts.Clear();
        }

        /// <summary>The keys while a model is held: turned, scaled, raised.</summary>
        void HeldKeys(Keyboard keyboard, Mouse mouse)
        {
            var shift = keyboard.shiftKey.isPressed;
            var wheel = PointerOnPage ? 0 : mouse.scroll.ReadValue().y / 120f;
            if (keyboard.rKey.wasPressedThisFrame) ghostYaw += shift ? 90 : 15;
            if (keyboard.tKey.wasPressedThisFrame) ghostYaw -= shift ? 90 : 15;
            if (keyboard.altKey.isPressed && wheel != 0) ghostYaw += wheel * 5;
            if (keyboard.equalsKey.wasPressedThisFrame || keyboard.numpadPlusKey.wasPressedThisFrame) ScaleChoice = Mathf.Min(10, ScaleChoice * 1.1f);
            if (keyboard.minusKey.wasPressedThisFrame || keyboard.numpadMinusKey.wasPressedThisFrame) ScaleChoice = Mathf.Max(0.1f, ScaleChoice / 1.1f);
            if (keyboard.ctrlKey.isPressed && wheel != 0) ScaleChoice = Mathf.Clamp(ScaleChoice * Mathf.Pow(1.05f, wheel), 0.1f, 10);
            if (keyboard.pageUpKey.wasPressedThisFrame) heightOffset += shift ? 1 : 0.1f;
            if (keyboard.pageDownKey.wasPressedThisFrame) heightOffset -= shift ? 1 : 0.1f;
        }

        // Putting a model down: it follows the mouse on whatever is under it; a click puts it there, again and again.
        void PlaceTick(Keyboard keyboard, Mouse mouse, bool typing)
        {
            if (Chosen == null)
            {
                Choose(Tool.Select);

                return;
            }

            if (ghost == null || ghostId != Chosen.Id)
            {
                ClearGhosts();
                ghost = Ghost(Chosen);
                ghostId = Chosen.Id;
            }

            if (!typing)
            {
                HeldKeys(keyboard, mouse);
            }

            if (PointerOnPage || !Pick(out var hit))
            {
                ghost.SetActive(false);

                return;
            }

            ghost.SetActive(true);
            var turn = Quaternion.Euler(0, ghostYaw, 0);
            var middle = turn * new Vector3(Chosen.Middle.x, 0, Chosen.Middle.y) * ScaleChoice;
            var at = Snapped(hit.point) - middle;
            at.y = hit.point.y + heightOffset;
            ghost.transform.SetPositionAndRotation(at, turn);
            ghost.transform.localScale = Vector3.one * ScaleChoice;
            Status = string.Format(CultureInfo.InvariantCulture, "{0}: размер ×{1:0.00} · поворот {2:0}° · над землёй {3:0.0} м", Chosen.Label, ScaleChoice, Mathf.Repeat(ghostYaw, 360), heightOffset);

            if (mouse.leftButton.wasPressedThisFrame)
            {
                PutDown("Поставить", new[] { new Placed { Id = Chosen.Id, At = at, Euler = new Vector3(0, ghostYaw, 0), Scale = Vector3.one * ScaleChoice } }, false);

                if (RandomTurn)
                {
                    ghostYaw = Random.value * 360;
                }
            }
        }

        // Stretching: a stair from its foot to its top, a wall, a fence or a path laid along the line between two clicks.
        void StretchTick(Keyboard keyboard, Mouse mouse, bool typing)
        {
            if (Chosen == null || !(Chosen.Stretch || Chosen.Repeat))
            {
                Choose(Tool.Select);

                return;
            }

            if (!typing)
            {
                HeldKeys(keyboard, mouse);
            }

            if (PointerOnPage || !Pick(out var hit))
            {
                return;
            }

            var point = Snapped(hit.point);
            point.y = hit.point.y;

            if (stretchFrom == null)
            {
                Status = Chosen.Stretch ? $"{Chosen.Label}: щёлкните, где её низ (или верх), потом другой конец · ширина +/− (×{ScaleChoice:0.00})"
                    : $"{Chosen.Label}: щёлкните начало, потом конец · ширина +/− (×{ScaleChoice:0.00})";

                if (mouse.leftButton.wasPressedThisFrame)
                {
                    stretchFrom = point;
                }

                return;
            }

            var pieces = Stretched(stretchFrom.Value, point);
            ClearGhosts();

            foreach (var p in pieces)
            {
                var g = Ghost(Chosen);
                g.transform.SetPositionAndRotation(p.At, Quaternion.Euler(p.Euler));
                g.transform.localScale = p.Scale;
                stretchGhosts.Add(g);
            }

            Status = string.Format(CultureInfo.InvariantCulture, "{0}: {1} шт., {2:0.0} м — щелчок закончить, правая кнопка или Esc — отмена", Chosen.Label, pieces.Count, Vector3.Distance(stretchFrom.Value, point));

            if (mouse.leftButton.wasPressedThisFrame)
            {
                ClearGhosts();
                PutDown("Протянуть", pieces, false);
                stretchFrom = null;
            }
        }

        /// <summary>The pieces between two points: one stair scaled to the run and the rise, or copies along the line, each on the ground under it.</summary>
        List<Placed> Stretched(Vector3 a, Vector3 b)
        {
            var pieces = new List<Placed>();

            if (Chosen.Stretch)
            {
                var foot = a.y <= b.y ? a : b;
                var top = a.y <= b.y ? b : a;
                var flat = new Vector3(foot.x - top.x, 0, foot.z - top.z);
                var run = Mathf.Max(0.3f, flat.magnitude);
                var rise = Mathf.Max(0.2f, top.y - foot.y);
                var stairYaw = Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg;
                var scale = new Vector3(ScaleChoice, rise / Mathf.Max(0.1f, Chosen.Height), run / Mathf.Max(0.1f, Chosen.Size.y));
                // Its foot (the front edge's middle) on the lower point: the origin back from there.
                var frontEdge = new Vector3(Chosen.Middle.x * scale.x, 0, (Chosen.Middle.y + Chosen.Size.y / 2) * scale.z);
                var at = foot - Quaternion.Euler(0, stairYaw, 0) * frontEdge;
                at.y = foot.y;
                pieces.Add(new Placed { Id = Chosen.Id, At = at, Euler = new Vector3(0, stairYaw, 0), Scale = scale });

                return pieces;
            }

            var line = new Vector3(b.x - a.x, 0, b.z - a.z);
            var length = line.magnitude;
            var piece = Mathf.Max(0.5f, Chosen.Size.x * ScaleChoice);
            var count = Mathf.Max(1, Mathf.RoundToInt(length / piece));
            var each = Mathf.Max(0.5f, length / count);
            var dir = length > 0.01f ? line / length : Vector3.right;
            // The model's own X along the line.
            var lineYaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg - 90;
            var turnLine = Quaternion.Euler(0, lineYaw, 0);

            for (var i = 0; i < count && i < 200; i++)
            {
                var centre = a + dir * each * (i + 0.5f);
                var ground = GroundY(new Vector2(centre.x, centre.z), centre.y);
                var scale = new Vector3(each / Mathf.Max(0.1f, Chosen.Size.x), ScaleChoice, ScaleChoice);
                var at = centre - turnLine * new Vector3(Chosen.Middle.x * scale.x, 0, Chosen.Middle.y * scale.z);
                at.y = ground;
                pieces.Add(new Placed { Id = Chosen.Id, At = at, Euler = new Vector3(0, lineYaw, 0), Scale = scale });
            }

            return pieces;
        }

        // ── Lines ──────────────────────────────────────────────────────

        Material LineMaterial => lineMaterial != null ? lineMaterial : lineMaterial = new Material(Shader.Find("GuWorld/Lines") ?? Shader.Find("Sprites/Default")) { name = "Builder Lines" };

        LineRenderer NewLine(string name, int points, Color colour)
        {
            var line = new GameObject(name).AddComponent<LineRenderer>();
            line.positionCount = points;
            line.useWorldSpace = true;
            line.sharedMaterial = LineMaterial;
            line.startColor = line.endColor = colour;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.enabled = false;

            return line;
        }

        void DrawLines()
        {
            var shown = CurrentMode == Mode.Things ? selection : null;
            var count = shown?.Count ?? 0;

            while (boxes.Count < count)
            {
                boxes.Add(NewLine("Chosen", 16, ChosenColour));
            }

            for (var i = 0; i < boxes.Count; i++)
            {
                if (i < count && shown[i] != null)
                {
                    DrawBox(boxes[i], BoundsOf(shown[i]));
                }
                else
                {
                    boxes[i].enabled = false;
                }
            }

            if (hoverBox == null)
            {
                hoverBox = NewLine("Under the mouse", 16, HoverColour);
            }

            if (CurrentMode == Mode.Things && CurrentTool == Tool.Select && hovered != null && !selection.Contains(hovered))
            {
                DrawBox(hoverBox, BoundsOf(hovered));
            }
            else
            {
                hoverBox.enabled = false;
            }

            if (ring != null && CurrentMode == Mode.Things)
            {
                ring.enabled = false;
            }
        }

        void HideLines()
        {
            foreach (var line in boxes)
            {
                line.enabled = false;
            }

            if (hoverBox != null) hoverBox.enabled = false;
            if (ring != null) ring.enabled = false;
            hovered = null;
        }

        /// <summary>A box drawn round bounds, its lines as thick as its distance asks (seen from far above too).</summary>
        void DrawBox(LineRenderer line, Bounds b)
        {
            var lo = b.min;
            var hi = b.max;
            var c = new[]
            {
                new Vector3(lo.x, lo.y, lo.z), new Vector3(hi.x, lo.y, lo.z), new Vector3(hi.x, lo.y, hi.z), new Vector3(lo.x, lo.y, hi.z),
                new Vector3(lo.x, hi.y, lo.z), new Vector3(hi.x, hi.y, lo.z), new Vector3(hi.x, hi.y, hi.z), new Vector3(lo.x, hi.y, hi.z),
            };
            var order = new[] { 0, 1, 2, 3, 0, 4, 5, 1, 5, 6, 2, 6, 7, 3, 7, 4 };
            line.enabled = true;
            line.positionCount = order.Length;
            line.widthMultiplier = Mathf.Clamp(Vector3.Distance(eye.transform.position, b.center) * 0.004f, 0.05f, 6);

            for (var i = 0; i < order.Length; i++)
            {
                line.SetPosition(i, c[order[i]]);
            }
        }

        /// <summary>The frame being dragged, drawn over the view.</summary>
        void OnGUI()
        {
            if (!Active || !framing || Mouse.current == null)
            {
                return;
            }

            var m = Mouse.current.position.ReadValue();
            var r = Rect.MinMaxRect(Mathf.Min(pressAt.x, m.x), Mathf.Min(pressAt.y, m.y), Mathf.Max(pressAt.x, m.x), Mathf.Max(pressAt.y, m.y));
            var gui = new Rect(r.xMin, Screen.height - r.yMax, r.width, r.height);
            var was = GUI.color;
            GUI.color = new Color(0.45f, 0.85f, 1f, 0.15f);
            GUI.DrawTexture(gui, Texture2D.whiteTexture);
            GUI.color = new Color(0.45f, 0.85f, 1f, 0.9f);
            GUI.DrawTexture(new Rect(gui.xMin, gui.yMin, gui.width, 2), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(gui.xMin, gui.yMax - 2, gui.width, 2), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(gui.xMin, gui.yMin, 2, gui.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(gui.xMax - 2, gui.yMin, 2, gui.height), Texture2D.whiteTexture);
            GUI.color = was;
        }
    }
}
