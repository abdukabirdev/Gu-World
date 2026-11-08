using System;
using System.Collections.Generic;
using GuWorld.Body;
using GuWorld.Cultivation;
using GuWorld.Items;
using GuWorld.Systems;
using GuWorld.World;
using UnityEngine;
using UnityEngine.UI;

namespace GuWorld.UI
{
    /// <summary>What the menus ask of the game.</summary>
    public interface IMenuHost
    {
        /// <summary>A saved game to go on with, and a line about it ("Гу Юэ Лань · Ранг 1 · День 3 · 14:20").</summary>
        bool HasSave { get; }
        string SaveSummary { get; }
        Look Look { get; }
        bool CreatorFace { get; set; }
        /// <summary>The way (difficulty, talent) chosen for the new game; -1: not yet chosen (asked before the hero is made).</summary>
        int ChosenPath { get; set; }

        /// <summary>Whether this is the proving ground; going to it (from the main menu) and back; what its page does.</summary>
        bool Sandbox { get; }
        void OpenSandbox();
        void LeaveSandbox();
        int SpawnWolves(int count);
        int SpawnPack(GuWorld.Creatures.BeastKind kind, int count);
        /// <summary>Something going on that the task's card shows instead of the task (a wolf tide): its title, goal, where, how far along.</summary>
        (string title, string goal, string where, string progress)? Event { get; }
        /// <summary>A wolf tide begun now (a great one: the thunder crown wolf at its end).</summary>
        void StartTide(bool great);
        /// <summary>Gu Masters of a clan ("bai", "xiong", "gu_yue") and a rank called up near the hero, fighting on sight; how many came.</summary>
        int SpawnMasters(string clan, int rank, int count);
        void ClearWolves();
        void SetHour(float hour);
        void BackToStart();
        /// <summary>The immortal's aperture's eight cells at hand (Gu kinds), the chosen one, and a Gu put in one.</summary>
        string[] SandboxBar { get; }
        int SandboxSlot { get; set; }
        void PutInSlot(int slot, GuKind kind);
        Render.ThirdPersonCamera View { get; }

        void Continue();
        /// <summary>A new game: wipes the save and goes to making the hero.</summary>
        void NewGame();
        void Wear(Look look);
        /// <summary>The hero is made: play.</summary>
        void StartPlaying();
        void Resume();
        void BackToTitle();
        void ResetGame();
        void Quit();
        /// <summary>The conversation is over: back to playing.</summary>
        void EndTalk();
        /// <summary>Goes to one of the hero's pages (the bag, the aperture, the map) — the world waits.</summary>
        void OpenPage(Menus.Page page);
        /// <summary>Puts the hero at a point at once (y NaN: on the ground there).</summary>
        void Teleport(Vector3 at);
        /// <summary>Where the task in hand leads (its giver, its place, the nearest of what it asks for) and who or what is there; null: nowhere to show.</summary>
        (Vector3 at, string what)? QuestTarget { get; }
        /// <summary>A line of advice on the keys, until the player has done what it says once (null: none now).</summary>
        string Hint { get; }
        /// <summary>The chat's commands (null where there are none), and closing the chat.</summary>
        GuWorld.Systems.Commands Commands { get; }
        void CloseChat();

        /// <summary>The bodies the figures are made of (the body page shows the hero's).</summary>
        HumanBody.Parts Parts { get; }

        /// <summary>The hero as a Gu Master, their health, their tasks.</summary>
        Cultivator Cultivator { get; }
        /// <summary>The hero's own fists (their slot on the bar).</summary>
        GuWorld.Combat.Melee Fists { get; }
        Health Health { get; }
        /// <summary>The body's training (the status window).</summary>
        Physique Physique { get; }
        /// <summary>Whether the admin mode is on (the admin panel, the chat's cheats).</summary>
        bool Admin { get; set; }
        Quests Quests { get; }
        /// <summary>The shop the hero is at (null: none), and a deal there.</summary>
        Shop Shop { get; }
        Trade Trade { get; }
        Deal Buy(IDictionary<string, int> lots);
        int Sell(int cell, int count);
        /// <summary>Throws a pile out of the bag.</summary>
        void Drop(int cell);

        /// <summary>The location's map, where the hero is on it and which way they face (radians, 0 north).</summary>
        LocalMap Map { get; }
        /// <summary>A finer map round the hero, for the minimap (null where there is none).</summary>
        LocalMap NearMap { get; }
        Vector3 HeroAt { get; }
        float HeroFacing { get; }
        /// <summary>The place and the hour, as the HUD shows them.</summary>
        string PlaceName { get; }
        string TimeLine { get; }
        /// <summary>The game minute now.</summary>
        double Now { get; }
    }

    /// <summary>
    /// GU World's menus (uGUI, built in code, in the design's look): the main
    /// menu over the living world, the hero's making, the pause, the settings,
    /// the Gu encyclopedia, confirmations and conversations — and, while
    /// playing, the HUD (MenusHud) and the hero's pages: the bag, the aperture,
    /// the map and a shop (MenusBag, MenusAperture, MenusMap, MenusShop).
    /// Changing between the big screens fades through black. Everything
    /// animates in real time, so it works while the world is paused.
    /// </summary>
    public partial class Menus : MonoBehaviour
    {
        public enum Page
        {
            /// <summary>Playing: only the HUD.</summary>
            None,
            Title,
            Creator,
            Pause,
            Settings,
            About,
            Confirm,
            /// <summary>Talking to someone: their words below, the questions beside them; the world goes on.</summary>
            Dialogue,
            /// <summary>The aperture: the sea of essence, the ring of Gu, feeding them.</summary>
            Aperture,
            Bag,
            Map,
            Shop,
            /// <summary>The Gu encyclopedia (from the main menu).</summary>
            Codex,
            /// <summary>The body: where blows wound and how much.</summary>
            Body,
            /// <summary>The proving ground's modes and its arsenal of every Gu (P, only there).</summary>
            Sandbox,
            /// <summary>The chat over the game (T, /): the world goes on.</summary>
            Chat,
            /// <summary>Fusing Gu into new ones (U): every recipe, what it takes, what the hero has.</summary>
            Fusion,
            /// <summary>The builder (F9): the world edited — models put down, moved, removed, the ground shaped (WorldBuilder).</summary>
            Builder,
            /// <summary>The status window (O): the body's traits, health, stamina, the sea.</summary>
            Status,
            /// <summary>The admin panel (F10): cheats for testing, with the admin mode on.</summary>
            Admin,
            /// <summary>The journal (J): the tasks taken, the merit with the clan, how the clans stand.</summary>
            Journal,
        }

        enum Tab
        {
            Game,
            Controls,
            Graphics,
            Sound,
        }

        public const string Version = "0.2.0";

        IMenuHost host;
        Canvas canvas;
        RectTransform pageRoot;
        Text fpsLine;

        // The conversation on screen.
        string speaker;
        string speakerTitle;
        string spoken;
        IList<(string text, Action act)> answers = new (string, Action)[0];
        Image fade;
        float fadeAlpha;
        float fadeTarget;
        Action afterFade;
        Tab tab;

        // What the confirmation says and does, and where "back" leads from the settings and the confirmation.
        string confirmTitle;
        string confirmText;
        string confirmButton;
        Action confirmAction;
        Page confirmBack;
        Page settingsBack;
        string codexChosen;
        int codexRank;

        public Page Current { get; private set; } = (Page)(-1);

        public static Menus Create(IMenuHost host)
        {
            var menus = new GameObject("Menus").AddComponent<Menus>();
            menus.host = host;
            menus.Build();

            return menus;
        }

        void Build()
        {
            canvas = UiKit.Canvas("Menu Canvas", 10);
            canvas.transform.SetParent(transform, false);

            BuildHud();
            pageRoot = UiKit.Rect("Page", canvas.transform).Fill();

            // The counter sits above everything, menus too.
            fpsLine = UiKit.Label("", canvas.transform, 15, new Color(0.62f, 0.9f, 0.62f), TextAnchor.UpperLeft, FontStyle.Bold).Shadowed();
            fpsLine.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), 12, -30, -300, 8);

            fade = UiKit.Image("Fade", canvas.transform, Color.black);
            fade.rectTransform.Fill();
            fade.raycastTarget = false;
        }

        /// <summary>The frames-per-second counter's value (shown when the setting is on).</summary>
        public void SetFps(float fps)
        {
            fpsLine.text = GameSettings.ShowFps ? $"FPS {fps:0}" : "";
        }

        /// <summary>Shows a conversation: who speaks, what they say, and what one can answer.</summary>
        public void ShowDialogue(string name, string title, string text, IList<(string text, Action act)> choices)
        {
            speaker = name;
            speakerTitle = title;
            spoken = text;
            answers = choices;
            Show(Page.Dialogue);
        }

        bool hudHidden;

        /// <summary>The interface hidden (F1): only the world is drawn.</summary>
        public bool HudHidden
        {
            get => hudHidden;
            set
            {
                hudHidden = value;
                hud.gameObject.SetActive(!hudHidden && (Current == Page.None || Current == Page.Dialogue || Current == Page.Chat));
            }
        }

        /// <summary>Fades to black, does `then`, and fades back in.</summary>
        public void FadeThrough(Action then)
        {
            afterFade = then;
            fadeTarget = 1;
        }

        /// <summary>Starts covered in black and fades in (the game's opening).</summary>
        public void FadeIn()
        {
            fadeAlpha = 1;
            fadeTarget = 0;
        }

        void Update()
        {
            var step = Time.unscaledDeltaTime / 0.28f;
            fadeAlpha = Mathf.MoveTowards(fadeAlpha, fadeTarget, step);
            fade.color = new Color(0, 0, 0, fadeAlpha);
            fade.raycastTarget = fadeAlpha > 0.01f;

            if (fadeTarget >= 1 && fadeAlpha >= 1)
            {
                var then = afterFade;
                afterFade = null;
                fadeTarget = 0;
                then?.Invoke();
            }

            UpdateHud();
            UpdatePages();

            if (Current == Page.Chat)
            {
                UpdateChat();
            }

            if (Current == Page.Map)
            {
                UpdateMapPage();
            }
        }

        /// <summary>Shows a page (rebuilt from scratch: the pages are small).</summary>
        public void Show(Page page)
        {
            // Leaving the builder: what was done is kept, the camera given back.
            if (Current == Page.Builder && page != Page.Builder && GuWorld.World.WorldBuilder.Current != null)
            {
                GuWorld.World.WorldBuilder.Current.End();
            }

            Current = page;
            hud.gameObject.SetActive(!hudHidden && (page == Page.None || page == Page.Dialogue || page == Page.Chat));

            if (page != Page.Body)
            {
                Render.BodyStudio.Close();
            }

            for (var i = pageRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(pageRoot.GetChild(i).gameObject);
            }

            pageCells.Clear();

            switch (page)
            {
                case Page.Title: Title(); break;
                case Page.Creator: Creator(); break;
                case Page.Pause: Pause(); break;
                case Page.Settings: Settings(); break;
                case Page.About: About(); break;
                case Page.Confirm: Confirm(); break;
                case Page.Dialogue: Dialogue(); break;
                case Page.Aperture: AperturePage(); break;
                case Page.Bag: BagPage(); break;
                case Page.Map: MapPage(); break;
                case Page.Chat: ChatPage(); break;
                case Page.Shop: ShopPage(); break;
                case Page.Codex: Codex(); break;
                case Page.Body: BodyPage(); break;
                case Page.Sandbox: SandboxPage(); break;
                case Page.Fusion: FusionPage(); break;
                case Page.Status: StatusPage(); break;
                case Page.Admin: AdminPage(); break;
                case Page.Journal: JournalPage(); break;
                case Page.Builder: BuilderPage(); break;
            }
        }

        /// <summary>Esc on a page: one step back. False when there is nowhere to go back to.</summary>
        public bool Back()
        {
            switch (Current)
            {
                case Page.Settings: Show(settingsBack); return true;
                case Page.About: Show(Page.Title); return true;
                case Page.Codex: Show(Page.Title); return true;
                case Page.Confirm: Show(confirmBack); return true;
                case Page.Creator: FadeThrough(host.BackToTitle); return true;
                case Page.Pause: host.Resume(); return true;
                case Page.Builder:
                    // One step back first (the reset's panel, a line begun, a thing chosen, a tool), then out.
                    if (builderReset)
                    {
                        builderReset = false;
                        Show(Page.Builder);

                        return true;
                    }

                    if (GuWorld.World.WorldBuilder.Current != null && GuWorld.World.WorldBuilder.Current.Cancel())
                    {
                        Show(Page.Builder);

                        return true;
                    }

                    GuWorld.World.WorldBuilder.Current?.End();
                    host.Resume();

                    return true;
                case Page.Dialogue: host.EndTalk(); return true;
                case Page.Aperture:
                case Page.Bag:
                case Page.Map:
                case Page.Shop:
                case Page.Body:
                case Page.Fusion:
                case Page.Status:
                case Page.Admin:
                case Page.Journal:
                    host.Resume();
                    return true;
                default: return false;
            }
        }

        /// <summary>Asks before doing something: what, why, the button's words; with `hold` it cannot be undone, so it is held, not clicked.</summary>
        public void Ask(string title, string text, string button, Action action, Page back, bool hold = true)
        {
            confirmHold = hold;
            confirmTitle = title;
            confirmText = text;
            confirmButton = button;
            confirmAction = action;
            confirmBack = back;
            Show(Page.Confirm);
        }

        /// <summary>Opens the settings on a tab (0 game, 1 controls, 2 graphics, 3 sound).</summary>
        public void ShowSettings(int onTab, Page back)
        {
            tab = (Tab)Mathf.Clamp(onTab, 0, 3);
            OpenSettings(back);
        }

        void OpenSettings(Page back)
        {
            settingsBack = back;
            Show(Page.Settings);
        }

        /// <summary>A small heading over a group of choices.</summary>
        static void Heading(Transform parent, string text) =>
            UiKit.Lbl(text, parent, 12, UiKit.LabelGold, 0.24f, TextAnchor.LowerLeft).Size(24);

        // ── The pause ─────────────────────────────────────────────────

        /// <summary>The world dimmed behind a card.</summary>
        void Backdrop()
        {
            var dim = UiKit.Image("Dim", pageRoot, new Color(0.02f, 0.016f, 0.016f, 0.7f));
            dim.rectTransform.Fill();
            dim.raycastTarget = true;
        }

        // ── About, the encyclopedia, confirmations ────────────────────

        /// <summary>A Gu's skill in a line: what it costs and does (the price a master of its own rank pays).</summary>
        static string SkillLine(GuKind k) => k.Skill switch
        {
            SkillKind.Moonblade => $"{k.Cost:0.#}% за бросок, режет на {k.Reach:0} м, гаснет через {k.Range:0} м, сила {k.Power:0}{Afflicts(k)}",
            SkillKind.JadeSkin => $"поднять — {k.Cost:0.#}%, держать — {k.Upkeep:0.##}% в секунду; держит любой удар, каждый тоже берёт эссенцию",
            SkillKind.Skin => $"поднять — {k.Cost:0.#}%, держать — {k.Upkeep:0.##}% в секунду; гасит {k.Amount * 100:0}% удара",
            SkillKind.Bolt => $"{k.Cost:0.#}% за выстрел, бьёт на {k.Reach:0} м, сила {k.Power:0}" + (k.Radius > 0 ? $", взрыв {k.Radius:0.#} м" : "") + Afflicts(k),
            SkillKind.Burst => $"{k.Cost:0.#}% — взрыв {k.Radius:0.#} м там, куда смотрите (до {k.Range:0} м), сила {k.Power:0}{Afflicts(k)}",
            SkillKind.Cleave => $"{k.Cost:0.#}% — взмах на {k.Reach:0.#} м перед вами, сила {k.Power:0}{Afflicts(k)}",
            SkillKind.Might => $"{k.Cost:0.#}% — кулаки ×{k.Amount:0.#} на {k.Duration:0} с",
            SkillKind.Haste => $"{k.Cost:0.#}% — бег ×{k.Amount:0.#} на {k.Duration:0} с",
            SkillKind.Leap => $"{k.Cost:0.#}% — прыжок ×{k.Amount:0.#}, падение без вреда, {k.Duration:0} с",
            SkillKind.Flight => $"{k.Cost:0.#}% — взлёт вверх и вперёд, мягкое падение {k.Duration:0} с",
            SkillKind.Dash => $"{k.Cost:0.#}% — рывок вперёд на {k.Amount:0} м",
            SkillKind.Blink => $"{k.Cost:0.#}% — мгновенно туда, куда смотрите, до {k.Range:0} м",
            SkillKind.Heal => $"{k.Cost:0.#}% — +{k.Amount:0} здоровья разом",
            SkillKind.Mend => $"поднять — {k.Cost:0.#}%, держать — {k.Upkeep:0.##}% в секунду; +{k.Amount:0.#} здоровья в секунду",
            SkillKind.Hide => $"поднять — {k.Cost:0.#}%, держать — {k.Upkeep:0.##}% в секунду; " +
                              (k.Amount <= 0 ? "звери вас не видят" : $"звери видят вас лишь с {k.Amount * 100:0}% расстояния"),
            SkillKind.Sense => $"поднять — {k.Cost:0.#}%, держать — {k.Upkeep:0.##}% в секунду; слышит зверей в {k.Range:0} м",
            SkillKind.Ward => $"{k.Cost:0.#}% — сфера на {k.Duration:0} с: ничто не пройдёт, но и вы не сдвинетесь",
            SkillKind.Howl => $"{k.Cost:0.#}% — звери в {k.Radius:0} м бегут {k.Duration:0} с",
            SkillKind.Flash => $"{k.Cost:0.#}% — звери в {k.Radius:0} м слепнут на {k.Duration:0} с",
            SkillKind.Decoy => $"{k.Cost:0.#}% — соломенная кукла, на которую кинутся звери, {k.Duration:0} с",
            SkillKind.Tame => $"{k.Cost:0.#}% — выстрел: волк, в которого попали, идёт за вами",
            SkillKind.Berserk => $"{k.Cost:0.#}% — вся сила ×{k.Amount:0.#} на {k.Duration:0} с",
            SkillKind.Relic => $"один раз, из сумки: +малая ступень мастеру ранга {k.Rank}",
            SkillKind.Strength => $"один раз, из сумки: сила навсегда +{k.Amount * 100:0}%",
            SkillKind.Bones => $"один раз, из сумки: тело навсегда крепче на {k.Amount * 100:0}%",
            SkillKind.Desperate => "один раз, из сумки: море разом полно, но малая ступень теряется",
            SkillKind.Refine => $"сам, пока в апертуре: эссенция ранга {k.Rank} чище на ступень — все Гу дешевле в полтора раза",
            SkillKind.Amplify => $"сам, пока в апертуре: лунный клинок ×{k.Amount:0.#} больше и дальше",
            SkillKind.Scent => $"сам, пока в апертуре: звери чуют вас лишь с {k.Amount * 100:0}% расстояния",
            SkillKind.Surety => "сам, пока в апертуре: слияния Гу не срываются",
            _ => "—",
        };

        /// <summary>What a strike does besides wounding, as the end of a skill's line.</summary>
        static string Afflicts(GuKind k) => k.Afflict switch
        {
            GuWorld.Creatures.Affliction.Burn => $", жжёт {k.AfflictPower:0}/с ещё {k.AfflictFor:0} с",
            GuWorld.Creatures.Affliction.Slow => $", замедляет на {k.AfflictPower * 100:0}% на {k.AfflictFor:0} с",
            GuWorld.Creatures.Affliction.Root => $", держит на месте {k.AfflictFor:0.#} с",
            GuWorld.Creatures.Affliction.Sleep => $", усыпляет на {k.AfflictFor:0} с",
            GuWorld.Creatures.Affliction.Blind => $", ослепляет на {k.AfflictFor:0} с",
            GuWorld.Creatures.Affliction.Fear => $", пугает на {k.AfflictFor:0} с",
            _ => "",
        };

        /// <summary>A fusion in a line: what goes in, what comes out, its essence and chance.</summary>
        static string RecipeLine(Recipe recipe)
        {
            var parts = new List<string>();

            foreach (var id in recipe.Gu)
            {
                parts.Add(GuBook.Find(id)?.Name ?? id);
            }

            foreach (var (item, count) in recipe.Materials)
            {
                parts.Add($"{ItemBook.Find(item)?.Name.ToLowerInvariant()} ×{count}");
            }

            var result = GuBook.Find(recipe.Result);

            return $"Слияние: {string.Join(" + ", parts)} → {result?.Name} (ранг {result?.Rank}) · {recipe.Cost:0}% · шанс {recipe.Chance * 100:0}%";
        }

        /// <summary>How often a Gu eats, in words.</summary>
        static string MealLine(double minutes)
        {
            if (minutes <= 0)
            {
                return "";
            }

            var hours = minutes / 60;

            return hours >= 24 ? $"раз в {Days((int)Math.Round(hours / 24))}" : hours == 12 ? "дважды в день" : $"каждые {hours:0} ч";
        }

        static string Days(int n) => n == 1 ? "день" : ItemBook.Count(n, "день", "дня", "дней");
    }
}
