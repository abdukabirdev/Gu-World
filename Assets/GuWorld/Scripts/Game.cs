using System.Collections;
using System.Collections.Generic;
using System.IO;
using GuWorld.Actors;
using GuWorld.Core;
using GuWorld.Fx;
using GuWorld.UI;
using GuWorld.World;
using UnityEngine;

namespace GuWorld
{
    /// <summary>
    /// How heavy the picture is: five presets (ROADMAP 2.7.3 — main's six without "cinema"), each a
    /// set of numbers that take effect at once (the picture's scale, smoothing, shadows, level of
    /// detail) and a tier the world's builders read when they make the world (0 light: weak
    /// integrated graphics, 1 ordinary, 2 high — the hero's full model, far trees and people). A
    /// number moved by hand makes the preset "custom".
    /// </summary>
    public static class Quality
    {
        public static readonly string[] Names = { "Минимум", "Низкое", "Среднее", "Высокое", "Ультра" };
        public const int Custom = -1;

        /// <summary>The numbers of a preset.</summary>
        public struct Numbers
        {
            public int Tier;
            public float Scale;
            public int Msaa;
            public float Shadows;
            public float LodBias;
            public float Trees;
            public bool Smaa;
        }

        public static readonly Numbers[] Presets =
        {
            new Numbers { Tier = 0, Scale = 0.6f, Msaa = 1, Shadows = 0, LodBias = 0.4f, Trees = 0.35f },
            new Numbers { Tier = 0, Scale = 0.75f, Msaa = 1, Shadows = 30, LodBias = 0.55f, Trees = 0.45f },
            new Numbers { Tier = 1, Scale = 0.9f, Msaa = 2, Shadows = 60, LodBias = 0.9f, Trees = 0.75f },
            new Numbers { Tier = 2, Scale = 1f, Msaa = 4, Shadows = 130, LodBias = 1.6f, Trees = 1f, Smaa = true },
            new Numbers { Tier = 2, Scale = 1f, Msaa = 4, Shadows = 220, LodBias = 2.4f, Trees = 1f, Smaa = true },
        };

        /// <summary>The preset chosen (0 minimum … 4 ultra), or Custom.</summary>
        public static int Preset { get; private set; } = 2;
        /// <summary>The numbers in effect.</summary>
        public static Numbers Now { get; private set; } = Presets[2];
        /// <summary>The tier the world was (or will be) built for: 0 light, 1 ordinary, 2 high.</summary>
        public static int Level => Now.Tier;
        public static bool FullHero => Level >= 2;
        /// <summary>The share of the plan's trees that are planted.</summary>
        public static float Trees => Now.Trees;
        public static string Name => Preset == Custom ? "Свои" : Names[Preset];

        public static void Load()
        {
            // The old three levels (gw2.quality: light, ordinary, high) are the presets low, medium, high.
            var guess = SystemInfo.graphicsMemorySize >= 3500 ? 3 : 2;
            var old = PlayerPrefs.GetInt("gw2.quality", -1);
            var preset = PlayerPrefs.GetInt("gw2.preset", old >= 0 ? Mathf.Clamp(old, 0, 2) + 1 : guess);

            if (preset == Custom)
            {
                Now = new Numbers
                {
                    Tier = PlayerPrefs.GetInt("gw2.q.tier", 1),
                    Scale = PlayerPrefs.GetFloat("gw2.q.scale", 0.9f),
                    Msaa = PlayerPrefs.GetInt("gw2.q.msaa", 2),
                    Shadows = PlayerPrefs.GetFloat("gw2.q.shadows", 60),
                    LodBias = PlayerPrefs.GetFloat("gw2.q.lod", 0.9f),
                    Trees = PlayerPrefs.GetFloat("gw2.q.trees", 0.75f),
                    Smaa = PlayerPrefs.GetInt("gw2.q.smaa", 0) == 1,
                };
                Preset = Custom;
                Apply();
            }
            else
            {
                Set(preset, false);
            }
        }

        /// <summary>A preset (0 … 4).</summary>
        public static void Set(int preset, bool save = true)
        {
            Preset = Mathf.Clamp(preset, 0, Presets.Length - 1);
            Now = Presets[Preset];

            if (save)
            {
                PlayerPrefs.SetInt("gw2.preset", Preset);
                PlayerPrefs.Save();
            }

            Apply();
        }

        /// <summary>The old three levels (the autotest's -quality): light, ordinary, high.</summary>
        public static void SetTier(int tier, bool save = true) => Set(Mathf.Clamp(tier, 0, 2) + 1, save);

        /// <summary>One number changed by hand: the preset becomes "custom".</summary>
        public static void Change(Numbers numbers)
        {
            Now = numbers;
            Preset = Custom;
            PlayerPrefs.SetInt("gw2.preset", Custom);
            PlayerPrefs.SetInt("gw2.q.tier", numbers.Tier);
            PlayerPrefs.SetFloat("gw2.q.scale", numbers.Scale);
            PlayerPrefs.SetInt("gw2.q.msaa", numbers.Msaa);
            PlayerPrefs.SetFloat("gw2.q.shadows", numbers.Shadows);
            PlayerPrefs.SetFloat("gw2.q.lod", numbers.LodBias);
            PlayerPrefs.SetFloat("gw2.q.trees", numbers.Trees);
            PlayerPrefs.SetInt("gw2.q.smaa", numbers.Smaa ? 1 : 0);
            PlayerPrefs.Save();
            Apply();
        }

        /// <summary>The numbers that take effect at once (the tier's own wait for the world to be built again).</summary>
        static void Apply()
        {
            QualitySettings.lodBias = Now.LodBias;
            QualitySettings.shadowDistance = Now.Shadows;
            QualitySettings.shadows = Now.Shadows > 0 ? ShadowQuality.All : ShadowQuality.Disable;
            var urp = UnityEngine.Rendering.Universal.UniversalRenderPipeline.asset;

            if (urp != null)
            {
                urp.renderScale = Now.Scale;
                urp.shadowDistance = Now.Shadows;
                urp.msaaSampleCount = Now.Msaa;
            }

            if (Game.Rig != null)
            {
                Game.Rig.Dress();
            }
        }
    }

    /// <summary>
    /// Which world the game runs on: the whole mountain (8 × 8 km, generated, streamed round the
    /// hero) or the small valley of the first rewrite (one terrain, everything loaded). The big one
    /// is the game; the small one stays as a fallback (`-smallworld` on the command line).
    /// </summary>
    public static class WorldMode
    {
        public static bool Big = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-smallworld") < 0;
    }

    /// <summary>
    /// The game itself: it starts with the application (no scene objects are needed), shows the
    /// title, builds the valley when a game begins, and holds what everything else asks for —
    /// the session (the rules), the hero, the camera, the interface.
    /// </summary>
    public class Game : MonoBehaviour
    {
        public static Game Instance { get; private set; }
        public static Session Session { get; private set; }
        public static Hero Hero { get; private set; }
        public static CameraRig Rig { get; private set; }
        public static Ui Ui { get; private set; }
        public static bool Playing { get; private set; }

        /// <summary>The world stands still (a menu is open).</summary>
        public static bool Frozen => Time.timeScale == 0;
        /// <summary>The hero does not answer the keys (a page or a talk is open) and cannot be hurt.</summary>
        public static bool Busy => Ui != null && Ui.Open != Ui.Page.None;
        public static bool Talking => Busy;

        /// <summary>Runs the world's coroutines, so they can all be stopped when the world is taken down.</summary>
        class Runner : MonoBehaviour { }

        Runner runner;
        GameObject world;
        Spawns spawns;
        float saveWait = 30;
        float nearbyWait;
        Component nearby;
        float hintWait = 8;
        Core.Gen.Walker nearbyWalker;
        public static string Prompt { get; private set; }

        /// <summary>The save slots (the first is the old single save, so a game saved before slots is kept).</summary>
        public const int Slots = 3;

        /// <summary>The slot played now (kept between runs).</summary>
        public static int Slot
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt("gw2.slot", 0), 0, Slots - 1);
            set
            {
                PlayerPrefs.SetInt("gw2.slot", Mathf.Clamp(value, 0, Slots - 1));
                PlayerPrefs.Save();
            }
        }

        static string PathOf(int slot) => Path.Combine(Application.persistentDataPath, slot == 0 ? "gu-world-2.json" : $"gu-world-2-slot{slot + 1}.json");

        /// <summary>The start of an arc (a chapter) kept for the rebirth death rule: the slot's file with ".arcN" before ".json".</summary>
        static string ArcPath(int slot, int arc) => Path.ChangeExtension(PathOf(slot), $".arc{arc}.json");

        /// <summary>When a slot was last saved (null: never).</summary>
        public static System.DateTime? SavedAt(int slot)
        {
            var path = PathOf(slot);

            return File.Exists(path) ? File.GetLastWriteTime(path) : File.Exists(path + ".bak") ? File.GetLastWriteTime(path + ".bak") : (System.DateTime?)null;
        }

        /// <summary>Whether a slot's own save could not be read and its backup is what is there.</summary>
        public static bool OnBackup(int slot)
        {
            var path = PathOf(slot);

            try
            {
                return File.Exists(path + ".bak") && (!File.Exists(path) || JsonUtility.FromJson<SaveData>(File.ReadAllText(path)) == null);
            }
            catch (System.Exception)
            {
                return true;
            }
        }

        /// <summary>Removes a slot's save, its backup and its arcs' starts (a slot emptied, or a hardcore life ended).</summary>
        public static void DeleteSlot(int slot)
        {
            var folder = Application.persistentDataPath;
            var stem = Path.GetFileNameWithoutExtension(PathOf(slot));

            foreach (var path in new[] { PathOf(slot), PathOf(slot) + ".bak", PathOf(slot) + ".tmp" })
            {
                TryDelete(path);
            }

            foreach (var path in Directory.GetFiles(folder, stem + ".arc*.json"))
            {
                TryDelete(path);
            }
        }

        static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (System.Exception error)
            {
                Debug.LogWarning($"GU World: {path} could not be removed: {error.Message}");
            }
        }

        /// <summary>The proving ground opened from the main menu: a guest hero, nothing kept, out to the menu again.</summary>
        public static bool SandboxOnly { get; private set; }
        static string SavePath => PathOf(Slot);
        public static bool HasSave => File.Exists(SavePath) || File.Exists(SavePath + ".bak");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Instance != null)
            {
                return;
            }

            var root = new GameObject("GU World");
            DontDestroyOnLoad(root);
            Instance = root.AddComponent<Game>();
        }

        void Awake()
        {
            AutoTest.Detect();
            Quality.Load();
            Prefs.ApplyScreen();
            runner = new GameObject("Runner").AddComponent<Runner>();
            runner.transform.SetParent(transform, false);
            Rig = CameraRig.Make();
            DontDestroyOnLoad(Rig.gameObject);
            Atmosphere.Make(transform);
            Sound.AudioDirector.Make(transform);
            Ui = new GameObject("Interface").AddComponent<Ui>();
            Ui.transform.SetParent(transform, false);
            Ui.Show(Ui.Page.Title);

            if (AutoTest.On)
            {
                AutoTest.Start(this);
            }
        }

        // ------------------------------------------------------------------ Beginning and ending a game

        public static SaveData ReadSave() => AutoTest.On ? null : ReadSlot(Slot);

        /// <summary>A slot's save, or its backup if the save itself is missing or broken (null if neither is there).</summary>
        public static SaveData ReadSlot(int slot)
        {
            foreach (var path in new[] { PathOf(slot), PathOf(slot) + ".bak" })
            {
                try
                {
                    if (File.Exists(path))
                    {
                        var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));

                        if (data != null && !string.IsNullOrEmpty(data.HeroName))
                        {
                            return data;
                        }
                    }
                }
                catch (System.Exception error)
                {
                    Debug.LogWarning($"GU World: {path} could not be read: {error.Message}");
                }
            }

            return null;
        }

        public static void Save()
        {
            // Nothing of the immortal aperture is kept: the save waits until the hero is back.
            if (AutoTest.On || Session == null || !Playing || Session.Dead || Session.InSandbox || World.Rooms.Inside || SandboxOnly)
            {
                return;
            }

            LootMote.Flush();

            if (Hero != null)
            {
                Session.State.X = Hero.transform.position.x;
                Session.State.Z = Hero.transform.position.z;
            }

            Session.State.Hour = Atmosphere.Hour;

            try
            {
                // Written beside, then moved in, the last good save kept as a backup: a crash mid-write loses nothing.
                var path = SavePath;
                var fresh = path + ".tmp";
                File.WriteAllText(fresh, JsonUtility.ToJson(Session.State));

                if (File.Exists(path))
                {
                    File.Copy(path, path + ".bak", true);
                    File.Delete(path);
                }

                File.Move(fresh, path);
            }
            catch (System.Exception error)
            {
                Debug.LogWarning("GU World: the game could not be saved: " + error.Message);
            }
        }

        public void Begin(bool fresh)
        {
            StartCoroutine(Starting(fresh));
        }

        /// <summary>The new game the new game pages chose (the name, the path, the player's rules), started on the next Begin(true).</summary>
        SaveData chosenNew;

        /// <summary>Begins a new game as the new game pages chose it: the hero is born on the path now (the one roll — the extreme physique — made here).</summary>
        public void BeginNew(string name, Difficulty path, Core.Settings rules)
        {
            // A new life in this slot: whatever was there (its backup, its arcs' starts) goes.
            SandboxOnly = false;
            DeleteSlot(Slot);
            chosenNew = Session.NewGame(name, path, rules, 0);
            Begin(true);
        }

        /// <summary>The immortal aperture from the main menu: a guest of the clan, any Gu, nothing saved.</summary>
        public void BeginSandbox()
        {
            SandboxOnly = true;
            chosenNew = Session.NewGame("Гость", Beginnings.Reference, null, 1);
            Begin(true);
        }

        /// <summary>Continues the game in a slot (the load page).</summary>
        public void Load(int slot)
        {
            SandboxOnly = false;
            Slot = slot;
            Begin(false);
        }

        IEnumerator Starting(bool fresh)
        {
            Ui.Loading(SandboxOnly ? "Апертура раскрывается…" : "Долина просыпается…");
            Ui.LoadingProgress(0.04f, "Сохранение");
            yield return null;
            yield return null;
            Leave();
            var state = fresh ? chosenNew : ReadSave();
            chosenNew = null;
            Session = new Session(state ?? Session.NewGame(Look.DefaultName));
            Session.Toast += line => Ui.Toast(line);
            Session.QuestStarted += quest =>
            {
                // The rebirth rule keeps the start of every arc.
                if (quest.Main && quest.Chapter > 1)
                {
                    KeepArc(quest.Chapter);
                }
            };
            Session.QuestStarted += quest =>
            {
                Ui.Banner(quest.Main ? "Новое задание" : "Поручение", quest.Title);
                Sound.AudioDirector.Play("quest", null, 0.7f, 0);
            };
            Session.QuestDone += quest =>
            {
                Ui.Banner("Задание выполнено", quest.Title);
                Sound.AudioDirector.Play("quest_done", null, 0.8f, 0);
                Save();
            };
            Session.Advanced += step =>
            {
                Ui.Banner("Прорыв", Cultivation.Name(step));
                Sound.AudioDirector.Play("breakthrough", null, 1, 0);

                if (Hero != null)
                {
                    Effects.Pillar(Hero.transform.position, new Color(1f, 0.9f, 0.6f), 2.4f, 22, 1.2f);
                }
            };
            Session.FlagSet += flag => spawns?.Refresh();
            Session.Requested += Requested;
            Session.ClockSet += hour => Atmosphere.Hour = hour;
            Session.Dawn += day =>
            {
                Ui.Toast($"Рассвет · день {day}");

                if (Session.FairDay)
                {
                    Ui.Banner("Ярмарка в городе Гу Юэ", "До заката у лотков — редкие Гу и реликвии");
                }
            };
            Session.Achieved += achievement => Ui.Banner("Достижение", achievement.Name);
            Session.StandingChanged += (clan, standing) => spawns?.Refresh();
            Session.AttainmentRose += (path, grade) =>
            {
                Ui.Banner(GuBook.PathName(path), Attainment.Grades[grade]);
                Sound.AudioDirector.Play("breakthrough", null, 0.6f, 0);
            };
            Session.ClanRankRose += rank => Ui.Banner("Клан Гу Юэ", Session.ClanRanks[rank]);
            Session.RaidChanged += up =>
            {
                if (up)
                {
                    Ui.Banner("Набег на Ивовый хутор", "Разбойники Чёрного Хвоста грабят крестьян");
                    spawns?.Refresh();
                }
            };
            Session.TideChanged += up =>
            {
                if (up)
                {
                    Ui.Banner("Волчий прилив", "Стаи идут к северным воротам деревни");
                    Sound.AudioDirector.Play("howl", null, 1, 0);
                    spawns?.Refresh();
                }
            };
            Session.MoveLearnt += move =>
            {
                Ui.Banner("Убийственный приём", move.Name);
                Sound.AudioDirector.Play("killer_move", null, 0.8f, 0);
            };
            Session.Revived += () =>
            {
                Ui.Banner("Цикада Весны и Осени", "Время повернулось вспять");
                Sound.AudioDirector.Play("revive", null, 1, 0);

                if (Hero != null)
                {
                    Effects.Pillar(Hero.transform.position, new Color(0.66f, 0.88f, 0.69f), 2.2f, 16, 1.6f);
                }
            };

            world = new GameObject("World");

            if (WorldMode.Big)
            {
                // The mountain's plan is made on a worker thread (a few seconds the first time); the loading screen waits.
                Debug.Log("GU World load: plan task starting");
                var making = System.Threading.Tasks.Task.Run(() => Core.Gen.WorldPlan.Get());
                var waited = 0f;

                // The plan takes some seconds the first time (cached after): the bar creeps toward half meanwhile.
                while (!making.IsCompleted)
                {
                    waited += Time.unscaledDeltaTime;
                    Ui.LoadingProgress(0.08f + 0.42f * (1 - Mathf.Exp(-waited / 4f)), waited > 20 ? $"Гора, реки, дороги, деревни — {waited:0} с" : "Гора, реки, дороги, деревни");
                    yield return null;
                }

                if (making.Exception != null)
                {
                    Debug.LogError("GU World: the world could not be made, falling back to the valley: " + making.Exception);
                    WorldMode.Big = false;
                }
                else
                {
                    Debug.Log("GU World load: plan made");
                    var plan = making.Result;
                    Debug.Log("GU World: " + plan.Report());
                    World.Ground.Plan = plan;
                    Ui.LoadingProgress(0.55f, "Земля");
                    yield return null;
                    Streamer.Make(world.transform, plan);
                    Debug.Log("GU World load: Streamer made");
                    Ui.LoadingProgress(0.64f, "Лес");
                    yield return null;
                    Forest.Make(world.transform, plan);
                    Debug.Log("GU World load: Forest made");
                    Ui.LoadingProgress(0.72f, "Места и деревни");
                    yield return null;
                    Places.Make(world.transform, plan);
                    Debug.Log("GU World load: Places made");
                    Ui.LoadingProgress(0.8f, "Трава и цветы");
                    yield return null;
                    GrassField.Make(world.transform, plan);
                    Debug.Log("GU World load: GrassField made");
                    Governor.Make(world.transform);
                    Debug.Log("GU World load: Governor made");
                    Ui.LoadingProgress(0.86f, "Люди");
                    yield return null;
                    Population.Make(world.transform);
                    Debug.Log("GU World load: Population made");
                }
            }

            if (!WorldMode.Big)
            {
                World.Ground.Plan = null;
                World.Ground.Build(world.transform);
            }

            Ui.LoadingProgress(0.9f, "Дома, вещи, погода");
            yield return null;
            Scenery.Build(world.transform);
            WeatherFx.Make(world.transform);
            Ui.LoadingProgress(0.96f, "Звери и герой");
            yield return null;
            spawns = world.AddComponent<Spawns>();
            Atmosphere.Hour = Session.State.Hour;

            var lantern = Atlas.Thing(Session.State.Lantern) ?? Valley.Thing("lantern_village");
            var at = fresh || state == null ? new Vector3(0, 0, -16) : new Vector3(lantern.At.X, 0, lantern.At.Z - 2.5f);
            // The ground under the hero must be there before they stand on it.
            Streamer.Instance?.LoadNow(at);
            Hero = Hero.Spawn(at, 0);
            Rig.Follow(Hero, 0);
            Ui.LoadingProgress(1, "Готово");
            Playing = true;
            Time.timeScale = 1;
            yield return null;
            Ui.Show(Ui.Page.None);

            if (SandboxOnly)
            {
                World.Aperture.Enter();
            }
            else if (fresh || state == null)
            {
                KeepArc(1);
                Ui.Banner("Глава I", Story.ChapterNames[1]);
                Ui.Toast("WASD — идти · мышь — смотреть · T — говорить · J — журнал");
            }
        }

        /// <summary>Writes the start of an arc for the rebirth rule (once: the arc's first start is the one kept).</summary>
        static void KeepArc(int arc)
        {
            if (Session == null || AutoTest.On || SandboxOnly || Session.State.Settings.Death != DeathRule.Rebirth || File.Exists(ArcPath(Slot, arc)))
            {
                return;
            }

            try
            {
                if (Hero != null)
                {
                    Session.State.X = Hero.transform.position.x;
                    Session.State.Z = Hero.transform.position.z;
                }

                File.WriteAllText(ArcPath(Slot, arc), JsonUtility.ToJson(Session.State));
            }
            catch (System.Exception error)
            {
                Debug.LogWarning("GU World: the arc's start could not be kept: " + error.Message);
            }
        }

        /// <summary>Takes the world down (to the title, or before another game); `save` false when the game is thrown away.</summary>
        public void Leave(bool save = true)
        {
            if (save)
            {
                Save();
            }

            Playing = false;
            StopGameCoroutines();

            foreach (var actor in new List<Actor>(Actor.All))
            {
                Destroy(actor.gameObject);
            }

            if (world != null)
            {
                Destroy(world);
            }

            foreach (var loose in Engine.FindAll<Fade>())
            {
                Destroy(loose.gameObject);
            }

            Hero = null;
            Session = null;
            spawns = null;
            Prompt = null;
            Time.timeScale = 1;
        }

        void StopGameCoroutines() => runner.StopAllCoroutines();

        public void ToTitle(bool save = true)
        {
            Leave(save);
            SandboxOnly = false;
            Ui.Show(Ui.Page.Title);
        }

        public static void Quit()
        {
            Save();
            Application.Quit();
        }

        void OnApplicationQuit() => Save();

        /// <summary>The sound while the window is behind others: as it is, quieter or none (the settings').</summary>
        void OnApplicationFocus(bool focused) => AudioListener.volume = focused || Prefs.Background == 0 ? 1 : Prefs.Background == 1 ? 0.3f : 0;

        // ------------------------------------------------------------------ Every frame

        void Update()
        {
            if (!Playing || Session == null || Hero == null)
            {
                return;
            }

            if (Frozen)
            {
                return;
            }

            // The rules keep the clock, the sky and whether the hero is moving.
            Session.SetClock(Atmosphere.Hour);
            Session.SetSky(Atmosphere.Sky);
            Session.SetMoving(Hero.Moving);
            saveWait -= Time.deltaTime;

            if (saveWait <= 0)
            {
                saveWait = Mathf.Max(15, Prefs.Autosave);
                Save();
            }

            // A hint now and then, each once, when it first matters.
            hintWait -= Time.deltaTime;

            if (hintWait <= 0 && !Busy && Prefs.Hints)
            {
                hintWait = 25;
                var hint = Session.NextHint();

                if (hint != null)
                {
                    Ui.Banner("Подсказка", hint.Text);
                }
            }

            nearbyWait -= Time.deltaTime;

            if (nearbyWait <= 0)
            {
                nearbyWait = 0.12f;
                FindNearby();
            }

            // The edge of the world.
            var p = Hero.transform.position;
            var limit = World.Ground.Half - 8;

            if (Mathf.Abs(p.x) > limit || Mathf.Abs(p.z) > limit || p.y < -20)
            {
                Hero.Teleport(Ground(new Vector3(Mathf.Clamp(p.x, -limit, limit), 0, Mathf.Clamp(p.z, -limit, limit))) + Vector3.up * 0.2f);
            }
        }

        void FindNearby()
        {
            nearby = null;
            Prompt = null;

            if (Busy || !Hero.Alive)
            {
                return;
            }

            var best = 3.4f;

            foreach (var npc in Npc.All)
            {
                var distance = Hero.FlatDistance(npc.transform.position);

                if (distance < best && npc.isActiveAndEnabled)
                {
                    best = distance;
                    nearby = npc;
                    Prompt = "Говорить · " + npc.Def.Name;
                }
            }

            foreach (var thing in Thing.All)
            {
                var distance = Hero.FlatDistance(thing.transform.position) - thing.Reach;

                if (distance < best && thing.Usable)
                {
                    best = distance;
                    nearby = thing;
                    Prompt = thing.Verb + " · " + thing.Def.Name;
                }
            }
            // A townsperson close by (only when no one of the story is closer).
            if (nearby == null)
            {
                var (folk, walker, body) = Townsfolk.Nearest(Hero.transform.position, 2.6f);

                if (walker != null)
                {
                    nearby = folk;
                    nearbyWalker = walker;
                    var (name, title) = Core.Chatter.Who(walker);
                    Prompt = (folk.AtStall(walker) ? "Торговать · " : "Говорить · ") + name;
                }
            }

            // The altar of the immortal aperture.
            if (World.Aperture.Prompt != null)
            {
                nearby = World.Aperture.Instance;
                Prompt = World.Aperture.Prompt;
            }

            // The way out of a room, and the room's people.
            var person = World.Rooms.NearestPerson();

            if (person != null)
            {
                nearby = person;
                Prompt = "Говорить · " + person.Name;
            }
            else if (World.Rooms.Prompt != null)
            {
                nearby = Ui;
                Prompt = World.Rooms.Prompt;
            }
        }

        public static void Interact()
        {
            Instance.FindNearby();

            if (Instance.nearby is Npc npc)
            {
                npc.Talk();
            }
            else if (Instance.nearby is Thing thing)
            {
                thing.Use();
            }
            else if (Instance.nearby is Townsfolk folk && Instance.nearbyWalker != null)
            {
                var walker = Instance.nearbyWalker;
                folk.TurnTo(walker, Hero.transform.position);
                var lines = Core.Chatter.Say(walker, Atmosphere.Hour, Session.Rank, Session.StandingWith(walker.Clan), Atmosphere.Gloom > 0.5f, Session.State.Day + (int)Time.time, Session.FairDay && walker.Clan == "gu_yue");
                var stall = folk.AtStall(walker) ? (walker.Clan == "gu_yue" ? Session.StallShelf(walker.Stall, Atmosphere.Hour) : Core.Chatter.ShelfOf(walker.Stall)) : null;
                var (name, _) = Core.Chatter.Who(walker);
                Ui.Talk(lines, stall == null ? (System.Action)null : () => Ui.OpenShop(stall, "Лоток · " + name, walker.Clan));
            }
            else if (Instance.nearby is World.RoomPerson person)
            {
                person.Talk();
            }
            else if (World.Rooms.Prompt != null)
            {
                World.Rooms.Leave();
            }
            else if (Instance.nearby is World.Aperture)
            {
                Ui.Show(UI.Ui.Page.Sandbox);
            }
        }

        // ------------------------------------------------------------------ What the world asks of the game

        /// <summary>The point of the ground under (or over) a point.</summary>
        public static Vector3 Ground(Vector3 at) => new Vector3(at.x, World.Ground.SampleHeight(at), at.z);

        public static void Shake(float amount) => Rig?.Shake(amount);
        public static void Say(string line) => Ui?.Toast(line);
        public static Coroutine Run(IEnumerator routine) => Instance.runner.StartCoroutine(routine);

        public static void EnemyKilled(Enemy enemy)
        {
            if (Session == null)
            {
                return;
            }

            var loot = Session.Killed(enemy.Def.Id, enemy.Def.Elite);
            var from = enemy.transform.position;

            if (loot.Stones > 0)
            {
                var stones = loot.Stones;
                LootMote.Spawn(from, new Color(0.91f, 0.79f, 0.54f), () => Session?.AddStones(stones));
            }

            foreach (var stack in loot.Items)
            {
                var item = stack;
                LootMote.Spawn(from, Models.Rgb(ItemBook.Get(item.Item).Colour), () => Session?.AddItem(item.Item, item.Count));
            }

            foreach (var gu in loot.Gu)
            {
                var found = gu;
                Effects.Pillar(from, Models.Rgb(GuBook.QualityColours[found.Quality]), 1.2f, 9, 1f);
                LootMote.Spawn(from, Models.Rgb(GuBook.Get(found.Def).Colour), () => Session?.AddGu(found));
            }
        }

        public static void HeroDied()
        {
            Instance.runner.StartCoroutine(Instance.Dying());
        }

        IEnumerator Dying()
        {
            var rule = Session?.State.Settings.Death ?? DeathRule.Lantern;

            // One life: the save goes at once (quitting now saves nothing — the hero is dead).
            if (rule == DeathRule.Hardcore && !SandboxOnly && Session != null && !Session.InSandbox)
            {
                Session.State.Deaths++;
                DeleteSlot(Slot);
            }

            yield return new WaitForSeconds(1.6f);

            if (Session == null)
            {
                yield break;
            }

            spawns?.HeroFell();

            // Rebirth and hardcore leave the hero where they fell, dead (no save is written): the page decides what comes.
            Ui.Death(rule == DeathRule.Lantern ? Session.Die() : 0);
        }

        /// <summary>The rebirth rule: born again at the start of the arc before this one, with only memories (DeathRules.Reborn).</summary>
        public void Reborn()
        {
            if (Session == null)
            {
                return;
            }

            var arc = DeathRules.RebirthArc(Session.Chapter);
            SaveData start = null;

            for (var a = arc; a >= 1 && start == null; a--)
            {
                try
                {
                    if (File.Exists(ArcPath(Slot, a)))
                    {
                        start = JsonUtility.FromJson<SaveData>(File.ReadAllText(ArcPath(Slot, a)));
                        arc = a;
                    }
                }
                catch (System.Exception error)
                {
                    Debug.LogWarning("GU World: an arc's start could not be read: " + error.Message);
                }
            }

            if (start == null)
            {
                // No arc kept (a save from before the rule): the lantern instead.
                Ui.Death(Session.Die());
                Wake();
                return;
            }

            var reborn = DeathRules.Reborn(start, Session.State);

            // The arcs after the one gone back to are lived again, and kept again.
            for (var a = arc + 1; a <= 6; a++)
            {
                TryDelete(ArcPath(Slot, a));
            }

            try
            {
                File.WriteAllText(PathOf(Slot), JsonUtility.ToJson(reborn));
            }
            catch (System.Exception error)
            {
                Debug.LogWarning("GU World: the rebirth could not be written: " + error.Message);
            }

            Begin(false);
        }

        /// <summary>After death: the hero wakes at their lantern.</summary>
        public static void Wake()
        {
            var lantern = Atlas.Thing(Session.State.Lantern) ?? Valley.Thing("lantern_village");
            Streamer.Instance?.LoadNow(new Vector3(lantern.At.X, 0, lantern.At.Z - 2.5f));
            Hero.Revive(new Vector3(lantern.At.X, 0, lantern.At.Z - 2.5f));
            Rig.Follow(Hero, 0);
            Ui.Show(Ui.Page.None);
            Save();
        }

        /// <summary>True while an enemy close by is after the hero (no travel, no sleep).</summary>
        public static bool Fighting
        {
            get
            {
                if (Hero == null)
                {
                    return false;
                }

                foreach (var enemy in Enemy.Living)
                {
                    if (enemy.Aware && enemy.Def.Mind != EnemyMind.Still && (enemy.transform.position - Hero.transform.position).sqrMagnitude < 30 * 30)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>To a lantern the rules let the hero travel to: dark, then there.</summary>
        public void TravelTo(ThingDef lantern)
        {
            Travel(new MapPoint(lantern.At.X, lantern.At.Z - 2.5f));
        }

        /// <summary>What a conversation's effect asks of the game: a shop, a fight, a page.</summary>
        void Requested(string what, string argument)
        {
            var npc = Atlas.Npc(argument);

            switch (what)
            {
                case "shop":
                case "gamble":
                    if (npc != null)
                    {
                        Ui.OpenShop(what == "gamble" ? "gamble" : npc.Shop, npc.Title, npc.Clan);
                    }

                    break;
                case "board":
                    Ui.OpenBoard(npc != null ? npc.Home : argument);
                    break;
                case "alchemy":
                    Ui.Show(Ui.Page.Alchemy);
                    break;
                case "travel":
                    Ui.Show(Ui.Page.Travel);
                    break;
                case "inn":
                    Ui.OpenInn(npc);
                    break;
                case "fight":
                    spawns?.Begin(argument);
                    break;
            }
        }

        /// <summary>Through a door: the screen goes dark, the hero is elsewhere.</summary>
        public static void Travel(MapPoint to)
        {
            Instance.runner.StartCoroutine(Instance.Travelling(to));
        }

        IEnumerator Travelling(MapPoint to)
        {
            yield return Ui.FadeOut(0.35f);
            Streamer.Instance?.LoadNow(new Vector3(to.X, 0, to.Z));
            Hero.Teleport(Ground(new Vector3(to.X, 0, to.Z)) + Vector3.up * 0.15f);
            Rig.Follow(Hero, Hero.transform.eulerAngles.y);
            spawns?.Refresh();
            yield return null;
            yield return Ui.FadeIn(0.45f);
        }
    }
}
