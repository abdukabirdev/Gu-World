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
            // Nothing of the immortal aperture is kept: the save waits until the hero is back.
            if (AutoTest.On || Session == null || !Playing || Session.Dead || Session.InSandbox)
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
            Session.Dawn += day => Ui.Toast($"Рассвет · день {day}");
            Session.Achieved += achievement => Ui.Banner("Достижение", achievement.Name);
            Session.StandingChanged += (clan, standing) => spawns?.Refresh();
            Session.AttainmentRose += (path, grade) =>
            {
                Ui.Banner(GuBook.PathName(path), Attainment.Grades[grade]);
                Sound.AudioDirector.Play("breakthrough", null, 0.6f, 0);
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
                var making = System.Threading.Tasks.Task.Run(() => Core.Gen.WorldPlan.Get());

                while (!making.IsCompleted)
                {
                    yield return null;
                }

                if (making.Exception != null)
                {
                    Debug.LogError("GU World: the world could not be made, falling back to the valley: " + making.Exception);
                    WorldMode.Big = false;
                }
                else
                {
                    var plan = making.Result;
                    Debug.Log("GU World: " + plan.Report());
                    World.Ground.Plan = plan;
                    Streamer.Make(world.transform, plan);
                    Forest.Make(world.transform, plan);
                    Places.Make(world.transform, plan);
                    Population.Make(world.transform);
                }
            }

            if (!WorldMode.Big)
            {
                World.Ground.Plan = null;
                World.Ground.Build(world.transform);
            }

            yield return null;
            Scenery.Build(world.transform);
            WeatherFx.Make(world.transform);
            yield return null;
            spawns = world.AddComponent<Spawns>();
            Atmosphere.Hour = Session.State.Hour;

            var lantern = Atlas.Thing(Session.State.Lantern) ?? Valley.Thing("lantern_village");
            var at = fresh || state == null ? new Vector3(0, 0, -16) : new Vector3(lantern.At.X, 0, lantern.At.Z - 2.5f);
            // The ground under the hero must be there before they stand on it.
            Streamer.Instance?.LoadNow(at);
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

            // The rules keep the clock, the sky and whether the hero is moving.
            Session.SetClock(Atmosphere.Hour);
            Session.SetSky(Atmosphere.Sky);
            Session.SetMoving(Hero.Moving);
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
            // The altar of the immortal aperture.
            if (World.Aperture.Prompt != null)
            {
                nearby = World.Aperture.Instance;
                Prompt = World.Aperture.Prompt;
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
