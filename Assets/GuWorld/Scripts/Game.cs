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
    /// <summary>How heavy the picture is: 0 light (weak integrated graphics), 1 ordinary, 2 high (the hero's full model, far shadows, bloom).</summary>
    public static class Quality
    {
        public static readonly string[] Names = { "Лёгкое", "Обычное", "Высокое" };
        public static int Level { get; private set; } = 1;
        public static bool FullHero => Level >= 2;
        /// <summary>The share of the plan's trees that are planted.</summary>
        public static float Trees => Level == 0 ? 0.45f : Level == 1 ? 0.75f : 1f;

        public static void Load() => Set(PlayerPrefs.GetInt("gw2.quality", SystemInfo.graphicsMemorySize >= 3500 ? 2 : 1), false);

        public static void Set(int level, bool save = true)
        {
            Level = Mathf.Clamp(level, 0, 2);

            if (save)
            {
                PlayerPrefs.SetInt("gw2.quality", Level);
                PlayerPrefs.Save();
            }

            QualitySettings.lodBias = Level == 0 ? 0.55f : Level == 1 ? 0.9f : 1.6f;
            QualitySettings.shadowDistance = Level == 0 ? 30 : Level == 1 ? 60 : 130;
            QualitySettings.vSyncCount = 1;
            Application.targetFrameRate = -1;
            var urp = UnityEngine.Rendering.Universal.UniversalRenderPipeline.asset;

            if (urp != null)
            {
                urp.renderScale = Level == 0 ? 0.75f : 1f;
                urp.shadowDistance = QualitySettings.shadowDistance;
                urp.msaaSampleCount = Level == 0 ? 1 : Level == 1 ? 2 : 4;
            }
        }
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
        public static string Prompt { get; private set; }

        static string SavePath => Path.Combine(Application.persistentDataPath, "gu-world-2.json");
        public static bool HasSave => File.Exists(SavePath);

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
            runner = new GameObject("Runner").AddComponent<Runner>();
            runner.transform.SetParent(transform, false);
            Rig = CameraRig.Make();
            DontDestroyOnLoad(Rig.gameObject);
            Atmosphere.Make(transform);
            Ui = new GameObject("Interface").AddComponent<Ui>();
            Ui.transform.SetParent(transform, false);
            Ui.Show(Ui.Page.Title);

            if (AutoTest.On)
            {
                AutoTest.Start(this);
            }
        }

        // ------------------------------------------------------------------ Beginning and ending a game

        public static SaveData ReadSave()
        {
            try
            {
                return HasSave && !AutoTest.On ? JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath)) : null;
            }
            catch (System.Exception error)
            {
                Debug.LogWarning("GU World: the save could not be read: " + error.Message);

                return null;
            }
        }

        public static void Save()
        {
            if (AutoTest.On || Session == null || !Playing || Session.Dead)
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
                File.WriteAllText(SavePath, JsonUtility.ToJson(Session.State));
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

        IEnumerator Starting(bool fresh)
        {
            Ui.Loading("Долина просыпается…");
            yield return null;
            yield return null;
            Leave();
            var state = fresh ? null : ReadSave();
            Session = new Session(state ?? Session.NewGame("Лань"));
            Session.Toast += line => Ui.Toast(line);
            Session.QuestStarted += quest => Ui.Banner(quest.Main ? "Новое задание" : "Поручение", quest.Title);
            Session.QuestDone += quest =>
            {
                Ui.Banner("Задание выполнено", quest.Title);
                Save();
            };
            Session.Advanced += step =>
            {
                Ui.Banner("Прорыв", Cultivation.Name(step));

                if (Hero != null)
                {
                    Effects.Pillar(Hero.transform.position, new Color(1f, 0.9f, 0.6f), 2.4f, 22, 1.2f);
                }
            };
            Session.FlagSet += flag => spawns?.Refresh();

            world = new GameObject("World");
            World.Ground.Build(world.transform);
            yield return null;
            Scenery.Build(world.transform);
            yield return null;
            spawns = world.AddComponent<Spawns>();
            Atmosphere.Hour = Session.State.Hour;

            var lantern = Valley.Thing(Session.State.Lantern) ?? Valley.Thing("lantern_village");
            var at = fresh || state == null ? new Vector3(0, 0, -16) : new Vector3(lantern.At.X, 0, lantern.At.Z - 2.5f);
            Hero = Hero.Spawn(at, 0);
            Rig.Follow(Hero, 0);
            Playing = true;
            Time.timeScale = 1;
            yield return null;
            Ui.Show(Ui.Page.None);

            if (fresh || state == null)
            {
                Ui.Banner("Глава I", Story.ChapterNames[1]);
                Ui.Toast("WASD — идти · мышь — смотреть · T — говорить · J — журнал");
            }
        }

        /// <summary>Takes the world down (to the title, or before another game).</summary>
        public void Leave()
        {
            Save();
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

            foreach (var loose in FindObjectsByType<Fade>())
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

        public void ToTitle()
        {
            Leave();
            Ui.Show(Ui.Page.Title);
        }

        public static void Quit()
        {
            Save();
            Application.Quit();
        }

        void OnApplicationQuit() => Save();

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

            saveWait -= Time.deltaTime;

            if (saveWait <= 0)
            {
                saveWait = 30;
                Save();
            }

            nearbyWait -= Time.deltaTime;

            if (nearbyWait <= 0)
            {
                nearbyWait = 0.12f;
                FindNearby();
            }

            // The edge of the world.
            var p = Hero.transform.position;
            var limit = Valley.Half - 8;

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

            var loot = Session.Killed(enemy.Def.Id);
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
            yield return new WaitForSeconds(1.6f);

            if (Session == null)
            {
                yield break;
            }

            var lost = Session.Die();
            spawns?.HeroFell();
            Ui.Death(lost);
        }

        /// <summary>After death: the hero wakes at their lantern.</summary>
        public static void Wake()
        {
            var lantern = Valley.Thing(Session.State.Lantern) ?? Valley.Thing("lantern_village");
            Hero.Revive(new Vector3(lantern.At.X, 0, lantern.At.Z - 2.5f));
            Rig.Follow(Hero, 0);
            Ui.Show(Ui.Page.None);
            Save();
        }

        /// <summary>Through a door: the screen goes dark, the hero is elsewhere.</summary>
        public static void Travel(MapPoint to)
        {
            Instance.runner.StartCoroutine(Instance.Travelling(to));
        }

        IEnumerator Travelling(MapPoint to)
        {
            yield return Ui.FadeOut(0.35f);
            Hero.Teleport(Ground(new Vector3(to.X, 0, to.Z)) + Vector3.up * 0.15f);
            Rig.Follow(Hero, Hero.transform.eulerAngles.y);
            spawns?.Refresh();
            yield return null;
            yield return Ui.FadeIn(0.45f);
        }
    }
}
