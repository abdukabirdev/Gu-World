using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using GuWorld.Render;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GuWorld.World
{
    /// <summary>
    /// The builder (F9, the page Menus.BuilderPage): the world edited in the game, as the old strategy games' world
    /// builders do it. Three modes, each with its own tools: the things (houses, walls, props — chosen by a click or a
    /// frame, dragged, turned, scaled, raised, copied, put down from the catalog, stretched along a line), the ground
    /// (raised, lowered, levelled, smoothed, painted, given back as it was built) and the woods (felled, planted); and
    /// two views, from above and in 3D (WorldBuilderView.cs). Every change can be taken back (Ctrl+Z) and done again
    /// (Ctrl+Y), and the whole of it reset. What is done is kept (persistentDataPath/builder: world.json, terrain.bin,
    /// paint.bin) and made again at every start: the things put down always stand; the built village's things are found
    /// by their name and place as their squares stream in (ChunkStreamer.SquareArrived) and moved or taken away again,
    /// and a square whose things were carried away stays loaded while one is near them (ChunkStreamer.Held).
    /// </summary>
    public partial class WorldBuilder : MonoBehaviour
    {
        public enum Mode
        {
            Things,
            Ground,
            Woods,
        }

        public enum Tool
        {
            Select,
            Place,
            Stretch,
            Raise,
            Lower,
            Flatten,
            Smooth,
            Restore,
            Paint,
            Fell,
            Plant,
        }

        [Serializable]
        class Placed
        {
            public string Id;
            public Vector3 At;
            public Vector3 Euler;
            public Vector3 Scale = Vector3.one;
        }

        [Serializable]
        class Changed
        {
            /// <summary>The built thing: its name and where it first stood (BuilderKey.Of).</summary>
            public string Key;
            public bool Gone;
            public Vector3 At;
            public Vector3 Euler;
            public Vector3 Scale = Vector3.one;
            /// <summary>Its square's prefab under Resources (it stays loaded while one is near where the thing now stands).</summary>
            public string Square = "";
        }

        /// <summary>A piece of work on the woods, kept in the order done: the trees felled in a circle, or one planted.</summary>
        [Serializable]
        class WoodsWork
        {
            public bool Plant;
            public float X;
            public float Z;
            public float Radius;
            /// <summary>A planted tree's kind (its prefab's name, a terrain's tree prototype), size and turn.</summary>
            public string Kind;
            public float Width = 1;
            public float Height = 1;
            public float Turn;
        }

        [Serializable]
        class Saved
        {
            public int Version = 2;
            public List<Placed> Placed = new List<Placed>();
            public List<Changed> Changed = new List<Changed>();
            public List<WoodsWork> Woods = new List<WoodsWork>();
        }

        /// <summary>On a thing put down by the builder: what it is (its record).</summary>
        class PlacedMark : MonoBehaviour
        {
            public Placed Data;
        }

        /// <summary>Where a thing stands, as one change takes it from and to: there or not (put down, removed), as built (no change kept), its place, turn and size.</summary>
        struct ThingState
        {
            public bool Here;
            public bool AsBuilt;
            public Vector3 At;
            public Vector3 Euler;
            public Vector3 Scale;
        }

        /// <summary>One thing's change in a step: a thing put down (its record) or a built one (its key).</summary>
        class ThingEdit
        {
            public Placed Placed;
            public string Key;
            public string Square;
            public ThingState Before;
            public ThingState After;
        }

        /// <summary>One step that can be taken back: what it did to the things, the ground, the woods.</summary>
        class Step
        {
            public string Name;
            public float When;
            public readonly List<ThingEdit> Things = new List<ThingEdit>();
            public readonly List<Patch> Ground = new List<Patch>();
            public WoodsEdit Woods;

            public bool Empty => Things.Count == 0 && Ground.Count == 0 && (Woods == null || Woods.Empty);
        }

        public static WorldBuilder Current { get; private set; }
        /// <summary>The builder is open: the game leaves the camera to it.</summary>
        public static bool Active { get; private set; }

        /// <summary>The game's camera (moved by the builder while it is open).</summary>
        public ThirdPersonCamera Rig;

        public Tool CurrentTool { get; private set; } = Tool.Select;
        public Mode CurrentMode => ModeOf(CurrentTool);
        /// <summary>The catalog's model taken up to put down or stretch.</summary>
        public BuildCatalog.Entry Chosen;
        /// <summary>What was last done or went wrong (the page's status line).</summary>
        public string Status = "";
        /// <summary>Where the mouse points on the ground (the page's corner).</summary>
        public string Under = "";
        /// <summary>Grows when the page must be made again (the mode, the tool, the chosen things changed).</summary>
        public int PageVersion { get; private set; }

        Saved saved = new Saved();
        /// <summary>The screenshot test's run: nothing is saved.</summary>
        bool demo;
        bool loaded;
        bool unsaved;
        float nextAutosave;
        readonly Dictionary<string, Changed> changes = new Dictionary<string, Changed>();
        /// <summary>The built things the builder has keyed, as they stand now (while their squares are in the world).</summary>
        readonly Dictionary<string, GameObject> live = new Dictionary<string, GameObject>();
        readonly Dictionary<Placed, GameObject> placedLive = new Dictionary<Placed, GameObject>();
        Transform placedRoot;
        Tool groundTool = Tool.Raise;
        Tool woodsTool = Tool.Fell;
        readonly List<Step> undo = new List<Step>();
        readonly List<Step> redo = new List<Step>();
        const int UndoDepth = 300;
        Camera eye;
        Material lineMaterial;

        string Folder => Path.Combine(Application.persistentDataPath, "builder");

        public static Mode ModeOf(Tool tool) => tool <= Tool.Stretch ? Mode.Things : tool <= Tool.Paint ? Mode.Ground : Mode.Woods;

        void Awake()
        {
            Current = this;
        }

        void OnDestroy()
        {
            if (Current == this)
            {
                Current = null;
                Active = false;
                ChunkStreamer.Watch = null;
            }

            ChunkStreamer.SquareArrived -= Apply;
        }

        void Touch() => PageVersion++;

        // ── Kept and made again ─────────────────────────────────────────

        void Update()
        {
            // The world's squares and ground are there once the streamer is: the kept work is made again then.
            if (!loaded && ChunkStreamer.Current != null && BuildCatalog.Load() != null)
            {
                loaded = true;
                Load();
            }

            if (Active)
            {
                Tick();

                // Kept now and then while working, not only on the way out.
                if (unsaved && Time.unscaledTime > nextAutosave)
                {
                    Save(false);
                }
            }
        }

        void Load()
        {
            placedRoot = new GameObject("Built by the player").transform;
            var json = Path.Combine(Folder, "world.json");
            var defaults = Path.Combine(Application.streamingAssetsPath, "builder", "world.json");

            try
            {
                var file = File.Exists(json) ? json : File.Exists(defaults) ? defaults : null;

                if (file != null)
                {
                    saved = JsonUtility.FromJson<Saved>(File.ReadAllText(file)) ?? new Saved();
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Gu World builder: the saved world could not be read: {e.Message}");
                saved = new Saved();
            }

            saved.Placed ??= new List<Placed>();
            saved.Changed ??= new List<Changed>();
            saved.Woods ??= new List<WoodsWork>();

            foreach (var change in saved.Changed)
            {
                changes[change.Key] = change;
            }

            foreach (var placed in saved.Placed)
            {
                Make(placed);
            }

            LoadGround();
            LoadWoods();
            ChunkStreamer.SquareArrived += Apply;

            foreach (var square in ChunkStreamer.Current.Instances.ToList())
            {
                Apply(square);
            }

            UpdateHeld();
            UnityEngine.Physics.SyncTransforms();
        }

        /// <summary>The things of a square that the builder can take: each thing streamed on its own, and each model (its level of detail) inside one.</summary>
        static IEnumerable<GameObject> Takeable(GameObject square)
        {
            foreach (Transform unit in square.transform)
            {
                yield return unit.gameObject;
            }

            foreach (var group in square.GetComponentsInChildren<LODGroup>(true))
            {
                if (group.transform.parent != square.transform)
                {
                    yield return group.gameObject;
                }
            }
        }

        /// <summary>A square of the built world streamed in: its things moved or taken away as the builder left them.</summary>
        void Apply(GameObject square)
        {
            if (square == null || changes.Count == 0)
            {
                return;
            }

            var index = ChunkStreamer.Current != null ? ChunkStreamer.Current.SquareOf(square.transform) : -1;

            foreach (var thing in Takeable(square).ToList())
            {
                var key = BuilderKey.Of(thing);

                if (!changes.TryGetValue(key, out var change))
                {
                    continue;
                }

                BuilderKey.Keep(thing, key);
                live[key] = thing;

                if (string.IsNullOrEmpty(change.Square) && index >= 0)
                {
                    change.Square = ChunkStreamer.Current.Chunks[index].Resource;
                }

                Show(thing, change);
            }
        }

        static void Show(GameObject thing, Changed change)
        {
            if (change.Gone)
            {
                thing.SetActive(false);

                return;
            }

            thing.SetActive(true);
            thing.transform.SetPositionAndRotation(change.At, Quaternion.Euler(change.Euler));
            thing.transform.localScale = change.Scale;
        }

        /// <summary>The squares whose things were carried away are kept while one is near the things (not only near the square).</summary>
        void UpdateHeld()
        {
            var streamer = ChunkStreamer.Current;

            if (streamer == null)
            {
                return;
            }

            streamer.Held.Clear();

            foreach (var change in changes.Values)
            {
                var index = change.Gone || string.IsNullOrEmpty(change.Square) ? -1 : streamer.SquareNamed(change.Square);

                if (index < 0)
                {
                    continue;
                }

                if (!streamer.Held.TryGetValue(index, out var places))
                {
                    streamer.Held[index] = places = new List<Vector2>();
                }

                places.Add(new Vector2(change.At.x, change.At.z));
            }
        }

        GameObject Make(Placed placed)
        {
            var entry = BuildCatalog.Load().Find(placed.Id);

            if (entry == null || entry.Prefab == null)
            {
                return null;
            }

            var thing = Instantiate(entry.Prefab, placed.At, Quaternion.Euler(placed.Euler), placedRoot);
            thing.transform.localScale = placed.Scale;
            thing.AddComponent<PlacedMark>().Data = placed;
            placedLive[placed] = thing;

            return thing;
        }

        /// <summary>Everything kept: the things, the ground, the paint, the woods.</summary>
        public void Save() => Save(true);

        void Save(bool say)
        {
            unsaved = false;
            nextAutosave = Time.unscaledTime + 60;

            if (demo)
            {
                return;
            }

            try
            {
                Directory.CreateDirectory(Folder);
                saved.Changed = changes.Values.ToList();
                File.WriteAllText(Path.Combine(Folder, "world.json"), JsonUtility.ToJson(saved, true));
                SaveGround();

                if (say)
                {
                    Status = $"Сохранено: поставлено {saved.Placed.Count}, изменено построенного {saved.Changed.Count}, работ в лесу {saved.Woods.Count}";
                }
            }
            catch (Exception e)
            {
                Status = "Не удалось сохранить: " + e.Message;
            }
        }

        // ── Steps taken back and done again ─────────────────────────────

        public bool CanUndo => undo.Count > 0;
        public bool CanRedo => redo.Count > 0;

        /// <summary>A step done: kept to be taken back (the steps undone before it are forgotten).</summary>
        void Push(Step step)
        {
            if (step == null || step.Empty)
            {
                return;
            }

            step.When = Time.unscaledTime;
            undo.Add(step);

            if (undo.Count > UndoDepth)
            {
                undo.RemoveAt(0);
            }

            redo.Clear();
            unsaved = true;
        }

        /// <summary>
        /// A small change by a key (a turn, a nudge, a size): joined to the last step when it was the same change to
        /// the same things a moment ago, so that holding a key is taken back at once, not press by press.
        /// </summary>
        void PushOrJoin(Step step)
        {
            var last = undo.Count > 0 ? undo[undo.Count - 1] : null;

            if (last != null && redo.Count == 0 && last.Name == step.Name && Time.unscaledTime - last.When < 1.5f && last.Ground.Count == 0 &&
                last.Woods == null && last.Things.Count == step.Things.Count &&
                last.Things.Zip(step.Things, (a, b) => a.Placed == b.Placed && a.Key == b.Key).All(same => same))
            {
                for (var i = 0; i < step.Things.Count; i++)
                {
                    last.Things[i].After = step.Things[i].After;
                }

                last.When = Time.unscaledTime;
                unsaved = true;

                return;
            }

            Push(step);
        }

        public void Undo()
        {
            if (undo.Count == 0)
            {
                Status = "Отменять нечего";

                return;
            }

            CancelWork();
            var step = undo[undo.Count - 1];
            undo.RemoveAt(undo.Count - 1);
            Replay(step, true);
            redo.Add(step);
            Status = $"Отменено: {step.Name} (вернуть — Ctrl+Y)";
        }

        public void Redo()
        {
            if (redo.Count == 0)
            {
                Status = "Возвращать нечего";

                return;
            }

            CancelWork();
            var step = redo[redo.Count - 1];
            redo.RemoveAt(redo.Count - 1);
            Replay(step, false);
            undo.Add(step);
            Status = $"Сделано снова: {step.Name}";
        }

        /// <summary>A step taken back (`back`) or done again.</summary>
        void Replay(Step step, bool back)
        {
            var things = back ? Enumerable.Reverse(step.Things) : step.Things;

            foreach (var edit in things)
            {
                SetState(edit, back ? edit.Before : edit.After);
            }

            var patches = back ? Enumerable.Reverse(step.Ground) : step.Ground;

            foreach (var patch in patches)
            {
                PutPatch(patch, back);
            }

            FlushRefit();

            if (step.Woods != null)
            {
                ReplayWoods(step.Woods, back);
            }

            selection.RemoveAll(t => t == null || !t.activeInHierarchy);
            UpdateHeld();
            UnityEngine.Physics.SyncTransforms();
            unsaved = true;
            Touch();
        }

        ThingState StateOf(Placed placed) => new ThingState
        {
            Here = saved.Placed.Contains(placed), At = placed.At, Euler = placed.Euler, Scale = placed.Scale,
        };

        ThingState StateOf(string key) => changes.TryGetValue(key, out var change)
            ? new ThingState { Here = !change.Gone, At = change.At, Euler = change.Euler, Scale = change.Scale }
            : new ThingState { Here = true, AsBuilt = true };

        /// <summary>A thing's change from its state as kept now: begun before the change (`Before`), finished after it (Finish).</summary>
        ThingEdit StartEdit(GameObject thing)
        {
            if (thing.GetComponent<PlacedMark>() is PlacedMark mark)
            {
                return new ThingEdit { Placed = mark.Data, Before = StateOf(mark.Data) };
            }

            var key = BuilderKey.Keep(thing).Key;
            live[key] = thing;

            return new ThingEdit { Key = key, Square = SquareName(thing), Before = StateOf(key) };
        }

        void Finish(ThingEdit edit) => edit.After = edit.Placed != null ? StateOf(edit.Placed) : StateOf(edit.Key);

        static string SquareName(GameObject thing)
        {
            var streamer = ChunkStreamer.Current;
            var index = streamer != null ? streamer.SquareOf(thing.transform) : -1;

            return index >= 0 ? streamer.Chunks[index].Resource : "";
        }

        /// <summary>A thing set as a step left it: its record and, when it is in the world, the thing itself.</summary>
        void SetState(ThingEdit edit, ThingState state)
        {
            if (edit.Placed != null)
            {
                var placed = edit.Placed;
                placedLive.TryGetValue(placed, out var thing);

                if (!state.Here)
                {
                    saved.Placed.Remove(placed);
                    placedLive.Remove(placed);

                    if (thing != null)
                    {
                        selection.Remove(thing);
                        Destroy(thing);
                    }

                    return;
                }

                if (!saved.Placed.Contains(placed))
                {
                    saved.Placed.Add(placed);
                }

                placed.At = state.At;
                placed.Euler = state.Euler;
                placed.Scale = state.Scale;

                if (thing == null)
                {
                    Make(placed);
                }
                else
                {
                    thing.transform.SetPositionAndRotation(state.At, Quaternion.Euler(state.Euler));
                    thing.transform.localScale = state.Scale;
                }

                return;
            }

            if (state.AsBuilt)
            {
                changes.Remove(edit.Key);
            }
            else
            {
                changes[edit.Key] = new Changed { Key = edit.Key, Gone = !state.Here, At = state.At, Euler = state.Euler, Scale = state.Scale, Square = edit.Square ?? "" };
            }

            if (live.TryGetValue(edit.Key, out var built) && built != null)
            {
                if (state.AsBuilt)
                {
                    var key = built.GetComponent<BuilderKey>();
                    built.SetActive(true);
                    built.transform.SetPositionAndRotation(key.At, key.Turn);
                    built.transform.localScale = key.Scale;
                }
                else
                {
                    Show(built, changes[edit.Key]);
                }
            }
        }

        /// <summary>The record of a thing as it stands now (after it was moved, turned, scaled).</summary>
        void Record(GameObject thing)
        {
            var t = thing.transform;

            if (thing.GetComponent<PlacedMark>() is PlacedMark mark)
            {
                mark.Data.At = t.position;
                mark.Data.Euler = t.eulerAngles;
                mark.Data.Scale = t.localScale;

                return;
            }

            var key = BuilderKey.Keep(thing).Key;
            live[key] = thing;

            if (!changes.TryGetValue(key, out var change))
            {
                changes[key] = change = new Changed { Key = key, Square = SquareName(thing) };
            }

            change.Gone = !thing.activeSelf;
            change.At = t.position;
            change.Euler = t.eulerAngles;
            change.Scale = t.localScale;
        }

        // ── Resets ──────────────────────────────────────────────────────

        /// <summary>The built village's things all back where they were built (one step: Ctrl+Z brings the changes back).</summary>
        public void ResetVillage() => ResetAll(true, false, false, false, "Постройки деревни — на свои места");

        public void ResetPlaced() => ResetAll(false, true, false, false, "Всё поставленное — убрано");

        public void ResetGroundAll() => ResetAll(false, false, true, false, "Земля — как была построена");

        public void ResetWoods() => ResetAll(false, false, false, true, "Лес — как был");

        public void ResetEverything() => ResetAll(true, true, true, true, "Всё — как было");

        void ResetAll(bool village, bool placed, bool ground, bool woods, string name)
        {
            CancelWork();
            Select(null);
            var step = new Step { Name = name };

            if (village)
            {
                foreach (var change in changes.Values.ToList())
                {
                    var edit = new ThingEdit { Key = change.Key, Square = change.Square, Before = StateOf(change.Key), After = new ThingState { Here = true, AsBuilt = true } };
                    step.Things.Add(edit);
                    SetState(edit, edit.After);
                }
            }

            if (placed)
            {
                foreach (var thing in saved.Placed.ToList())
                {
                    var edit = new ThingEdit { Placed = thing, Before = StateOf(thing), After = new ThingState { Here = false } };
                    step.Things.Add(edit);
                    SetState(edit, edit.After);
                }
            }

            if (ground)
            {
                step.Ground.AddRange(RestoreAllGround());
            }

            if (woods)
            {
                step.Woods = UndoAllWoods();
            }

            UpdateHeld();
            UnityEngine.Physics.SyncTransforms();

            if (step.Empty)
            {
                Status = "Сбрасывать нечего: здесь ничего не менялось";

                return;
            }

            Push(step);
            Save(false);
            Status = $"{name}. Передумали — Ctrl+Z";
        }

        // ── The open builder ───────────────────────────────────────────

        public bool Ready => loaded && ChunkStreamer.Current != null;

        public void Begin()
        {
            if (Active || Rig == null)
            {
                return;
            }

            Active = true;
            Rig.Following = false;
            eye = Rig.GetComponentInChildren<Camera>();

            if (eye == null)
            {
                eye = Camera.main;
            }

            BeginView();
            Status = "Строитель: F9 — выйти (всё сохраняется само), Ctrl+Z — отменить";
            Touch();
        }

        public void End()
        {
            if (!Active)
            {
                return;
            }

            CancelWork();
            Select(null);
            HideLines();
            EndView();
            Save(false);
            Active = false;
        }

        /// <summary>Whatever the mouse was doing dropped: a stroke ended, a drag or a frame let go, a line begun forgotten, the ghost gone.</summary>
        void CancelWork()
        {
            EndStroke();
            EndWoodsStroke();
            CancelDrag();
            stretchFrom = null;
            ClearGhosts();
        }

        /// <summary>Esc: one step back — a line begun, a model taken up, the things chosen. False when there was nothing to drop (the page closes then).</summary>
        public bool Cancel()
        {
            if (dragging || framing)
            {
                CancelDrag();

                return true;
            }

            if (stretchFrom != null)
            {
                stretchFrom = null;
                ClearGhosts();

                return true;
            }

            if (CurrentTool == Tool.Place || CurrentTool == Tool.Stretch)
            {
                Choose(Tool.Select);

                return true;
            }

            if (selection.Count > 0)
            {
                Select(null);

                return true;
            }

            return false;
        }

        public void SetMode(Mode mode)
        {
            Choose(mode == Mode.Things ? Tool.Select : mode == Mode.Ground ? groundTool : woodsTool);
        }

        public void Choose(Tool tool, BuildCatalog.Entry entry = null)
        {
            CancelWork();
            CurrentTool = tool;
            Chosen = entry ?? Chosen;

            if (ModeOf(tool) == Mode.Ground)
            {
                groundTool = tool;
            }
            else if (ModeOf(tool) == Mode.Woods)
            {
                woodsTool = tool;
            }

            if (tool != Tool.Select)
            {
                Select(null);
            }

            Touch();
        }

        bool PointerOnPage => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        /// <summary>A text field (the catalog's search) has the keys: the builder's own keys wait.</summary>
        static bool Typing
        {
            get
            {
                var chosen = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;

                return chosen != null && chosen.TryGetComponent<InputField>(out var field) && field.isFocused;
            }
        }

        Ray MouseRay => eye.ScreenPointToRay(Mouse.current.position.ReadValue());

        /// <summary>What the mouse points at, the nearest: anything solid.</summary>
        bool Pick(out RaycastHit hit)
        {
            hit = default;

            return Mouse.current != null && eye != null && UnityEngine.Physics.Raycast(MouseRay, out hit, 8000, ~0, QueryTriggerInteraction.Ignore);
        }

        /// <summary>Where the mouse points on the ground itself (the terrains, through whatever stands on them).</summary>
        bool PickGround(out Vector3 point) => GroundHit(MouseRay, out point);

        static bool GroundHit(Ray ray, out Vector3 point)
        {
            point = default;
            var best = float.PositiveInfinity;

            foreach (var terrain in Terrain.activeTerrains)
            {
                if (terrain.TryGetComponent<TerrainCollider>(out var collider) && collider.enabled && collider.Raycast(ray, out var hit, 12000) && hit.distance < best)
                {
                    best = hit.distance;
                    point = hit.point;
                }
            }

            return best < float.PositiveInfinity;
        }

        /// <summary>The ground's height at a point (the terrains, not what stands on them; a village's own ground where it lies over the mountain's), or `otherwise`.</summary>
        static float GroundY(Vector2 p, float otherwise = float.NaN)
        {
            var ray = new Ray(new Vector3(p.x, 9000, p.y), Vector3.down);
            var best = float.NegativeInfinity;

            foreach (var terrain in Terrain.activeTerrains)
            {
                var o = terrain.GetPosition();
                var size = terrain.terrainData.size;

                if (p.x < o.x || p.y < o.z || p.x > o.x + size.x || p.y > o.z + size.z)
                {
                    continue;
                }

                if (terrain.TryGetComponent<TerrainCollider>(out var collider) && collider.enabled && collider.Raycast(ray, out var hit, 12000) && hit.point.y > best)
                {
                    best = hit.point.y;
                }
            }

            return float.IsNegativeInfinity(best) ? otherwise : best;
        }

        void Tick()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;

            if (keyboard == null || mouse == null || eye == null)
            {
                return;
            }

            var dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            var typing = Typing;
            var ctrl = keyboard.ctrlKey.isPressed;
            UnityEngine.Physics.SyncTransforms();

            ViewTick(keyboard, mouse, dt, typing);

            if (!typing)
            {
                if (ctrl && keyboard.zKey.wasPressedThisFrame)
                {
                    if (keyboard.shiftKey.isPressed) Redo();
                    else Undo();
                }
                else if (ctrl && keyboard.yKey.wasPressedThisFrame)
                {
                    Redo();
                }
                else if (ctrl && keyboard.sKey.wasPressedThisFrame)
                {
                    Save();
                }

                if (!ctrl && !keyboard.altKey.isPressed)
                {
                    ToolKeys(keyboard);
                }
            }

            // The chosen whose squares went away (the view flew off) are no longer chosen.
            if (selection.RemoveAll(t => t == null || !t.activeInHierarchy) > 0)
            {
                Touch();
            }

            Under = PickGround(out var under) ? string.Format(CultureInfo.InvariantCulture, "X {0:0.0} · Z {1:0.0} · высота {2:0.0} м", under.x, under.z, under.y) : "";

            if (!panning)
            {
                switch (CurrentMode)
                {
                    case Mode.Things: ThingsTick(keyboard, mouse, typing); break;
                    case Mode.Ground: GroundTick(keyboard, mouse, dt, typing); break;
                    case Mode.Woods: WoodsTick(keyboard, mouse, dt, typing); break;
                }
            }

            DrawLines();
        }

        /// <summary>The number keys: the mode's tools in the order the page shows them.</summary>
        void ToolKeys(Keyboard keyboard)
        {
            var tools = ToolsOf(CurrentMode);
            var digits = new[] { keyboard.digit1Key, keyboard.digit2Key, keyboard.digit3Key, keyboard.digit4Key, keyboard.digit5Key, keyboard.digit6Key };

            for (var i = 0; i < tools.Length && i < digits.Length; i++)
            {
                if (digits[i].wasPressedThisFrame && CurrentTool != tools[i])
                {
                    Choose(tools[i]);
                }
            }
        }

        /// <summary>A mode's tools chosen on the page (the things' placing and stretching come from the catalog).</summary>
        public static Tool[] ToolsOf(Mode mode) => mode switch
        {
            Mode.Things => new[] { Tool.Select },
            Mode.Ground => new[] { Tool.Raise, Tool.Lower, Tool.Flatten, Tool.Smooth, Tool.Restore, Tool.Paint },
            _ => new[] { Tool.Fell, Tool.Plant },
        };

        public static string NameOf(Tool tool) => tool switch
        {
            Tool.Select => "Выбор и сдвиг",
            Tool.Place => "Поставить",
            Tool.Stretch => "Протянуть",
            Tool.Raise => "Поднять",
            Tool.Lower => "Опустить",
            Tool.Flatten => "Выровнять",
            Tool.Smooth => "Сгладить",
            Tool.Restore => "Как было",
            Tool.Paint => "Покрасить",
            Tool.Fell => "Вырубить",
            _ => "Посадить",
        };

        /// <summary>What the keys and the mouse do now (the page's help line).</summary>
        public string Help => CurrentTool switch
        {
            Tool.Select => "Щелчок — выбрать · Shift — добавить · тянуть по пустому — рамка · тянуть выбранное — сдвиг · R/T — поворот · +/− — размер · PgUp/PgDn — высота · Del — убрать · Ctrl+D — копия · Ctrl+C/V",
            Tool.Place => "Щелчок — поставить (можно много раз) · R/T — повернуть · +/− — размер · PgUp/PgDn — выше/ниже · правая кнопка или Esc — хватит",
            Tool.Stretch => "Щелчок — начало, щелчок — конец · +/− — ширина · правая кнопка или Esc — отмена",
            Tool.Flatten => "Держите левую — выровнять к высоте · Shift+щелчок — взять высоту · [ ] — кисть · Alt+колесо — сила",
            Tool.Fell => "Держите левую — вырубить деревья и камни под кистью · [ ] — кисть",
            Tool.Plant => "Держите левую — сажать выбранное справа · [ ] — кисть · густота — слева",
            _ => "Держите левую кнопку — работать кистью · [ ] или Ctrl+колесо — размер кисти · Alt+колесо — сила",
        };

        /// <summary>The camera's keys (the page's second help line).</summary>
        public string ViewHelp => CurrentView == View.Above
            ? "Камера: WASD/стрелки — двигать · колесо — ближе/дальше · средняя кнопка или Пробел+левая — тащить карту · правая — повернуть · Q/E — повернуть · Tab — 3D · F — к выбранному"
            : "Камера: WASD/стрелки — двигать · колесо — ближе/дальше · правая кнопка — вращать и наклонять · средняя или Пробел+левая — тащить · Q/E — повернуть · Tab — сверху · F — к выбранному";

        /// <summary>Puts the hero on the ground under the middle of the view (and closes nothing: the page does).</summary>
        public Vector3 GroundAhead()
        {
            var at = focus;
            var y = GroundY(new Vector2(at.x, at.z));

            return float.IsNaN(y) ? at : new Vector3(at.x, y, at.z);
        }

        /// <summary>
        /// For the screenshot test (`-page builder -builddemo`): a house, a stair stretched up a slope, a wall laid along
        /// a line and a mound raised near a point — what the tools do, done once, never saved.
        /// </summary>
        public void Demo(Vector3 near)
        {
            demo = true;
            var catalog = BuildCatalog.Load();
            var ground = UnityEngine.Physics.Raycast(near + Vector3.up * 300, Vector3.down, out var hit, 600, ~0, QueryTriggerInteraction.Ignore) ? hit.point : near;

            void Put(string id, Vector3 at, float turn, float scale = 1)
            {
                if (catalog.Find(id) != null)
                {
                    var placed = new Placed { Id = id, At = at, Euler = new Vector3(0, turn, 0), Scale = Vector3.one * scale };
                    saved.Placed.Add(placed);
                    Make(placed);
                }
            }

            Debug.Log($"Gu World builder demo at ({ground.x:0.0}, {ground.y:0.0}, {ground.z:0.0})");
            Put("guyue_bamboo_house", ground + new Vector3(-12, 0, 6), 180, 0.8f);
            Put("prop_village_well", ground + new Vector3(4, 0, 2), 0);
            Put("tree_plum_8m", ground + new Vector3(14, 0, 8), 30);
            Chosen = catalog.Find("prop_stairs_medium");

            if (Chosen != null)
            {
                foreach (var p in Stretched(ground + new Vector3(0, 0, -6), ground + new Vector3(0, 3, -16)))
                {
                    saved.Placed.Add(p);
                    Make(p);
                }
            }

            Chosen = catalog.Find("fort_wall_segment");

            if (Chosen != null)
            {
                foreach (var p in Stretched(ground + new Vector3(-20, 0, -24), ground + new Vector3(20, 0, -24)))
                {
                    saved.Placed.Add(p);
                    Make(p);
                }
            }

            CurrentTool = Tool.Raise;
            Radius = 8;
            Strength = 3;

            for (var i = 0; i < 30; i++)
            {
                Brush(ground + new Vector3(24, 0, -6), 0.1f);
            }

            EndStroke();
            CurrentTool = Tool.Select;
            Chosen = null;
            focus = ground;
            viewReady = true;
            viewYaw = Rig != null ? Rig.transform.eulerAngles.y : 0;
        }
    }

    /// <summary>
    /// A built thing's key for the builder — its name and where it was built (kept once it is moved) — and where it
    /// stood as built (to put it back without waiting for its square to come again).
    /// </summary>
    public class BuilderKey : MonoBehaviour
    {
        public string Key;
        public Vector3 At;
        public Quaternion Turn;
        public Vector3 Scale;

        public static string Of(GameObject thing)
        {
            if (thing.GetComponent<BuilderKey>() is BuilderKey kept)
            {
                return kept.Key;
            }

            var p = thing.transform.position;

            return string.Format(CultureInfo.InvariantCulture, "{0}@{1:0.0},{2:0.0},{3:0.0}", thing.name, Mathf.Round(p.x * 2) / 2, Mathf.Round(p.y * 2) / 2, Mathf.Round(p.z * 2) / 2);
        }

        /// <summary>The thing keyed (as it stands now, before the builder changes it), or its key as it was.</summary>
        public static BuilderKey Keep(GameObject thing, string key = null)
        {
            if (thing.GetComponent<BuilderKey>() is BuilderKey kept)
            {
                return kept;
            }

            key ??= Of(thing);
            var made = thing.AddComponent<BuilderKey>();
            made.Key = key;
            made.At = thing.transform.position;
            made.Turn = thing.transform.rotation;
            made.Scale = thing.transform.localScale;

            return made;
        }
    }
}
