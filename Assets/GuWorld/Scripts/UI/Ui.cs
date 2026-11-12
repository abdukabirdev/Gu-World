using System;
using System.Collections;
using System.Collections.Generic;
using GuWorld.Actors;
using GuWorld.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GuWorld.UI
{
    /// <summary>
    /// The interface: the HUD over the world and one page at a time over it (the title, a
    /// talk, the aperture, the bag, the journal, the map, a shop, fusion, the spring, pause).
    /// Built from the kit (UI/Kit) in the design's "Rite × Aperture" language.
    /// </summary>
    public partial class Ui : MonoBehaviour
    {
        public enum Page { None, Title, Loading, Talk, Aperture, Bag, Journal, Map, Shop, Gamble, Board, Fusion, Spring, Pause, Help, Death, Ending, Choice, Dialogue, Alchemy, Bounties, Codex, Clans, Travel, Inn, Settings, Moves, Sandbox, Paths, Home }

        public Page Open { get; private set; } = Page.Title;

        Canvas hudCanvas, pageCanvas, topCanvas;
        RectTransform pageRoot;
        Image fade, hurt;
        int born;
        string loadingText = "";
        int lostStones;
        Session S => Game.Session;

        static readonly Page[] HeroPages = { Page.Aperture, Page.Moves, Page.Paths, Page.Bag, Page.Journal, Page.Map, Page.Codex, Page.Clans };
        static readonly (string name, string key)[] HeroTabs = { ("Апертура", "K"), ("Приёмы", "U"), ("Пути", "P"), ("Сумка", "I"), ("Журнал", "J"), ("Карта", "M"), ("Кодекс", "B"), ("Кланы", "L") };

        void Awake()
        {
            hudCanvas = UiKit.Canvas("HUD", 0);
            pageCanvas = UiKit.Canvas("Page", 10);
            topCanvas = UiKit.Canvas("Top", 20);

            foreach (var canvas in new[] { hudCanvas, pageCanvas, topCanvas })
            {
                canvas.transform.SetParent(transform, false);
            }

            pageRoot = UiKit.Rect("Root", pageCanvas.transform);
            pageRoot.Fill();
            hurt = UiKit.Image("Hurt", topCanvas.transform, new Color(0.7f, 0.08f, 0.05f, 0), UiKit.Vignette);
            hurt.rectTransform.Fill();
            fade = UiKit.Image("Fade", topCanvas.transform, new Color(0, 0, 0, 0), UiKit.Plain);
            fade.rectTransform.Fill();
            BuildHud();
        }

        // ------------------------------------------------------------------ Pages

        static bool Stops(Page page) => page != Page.None && page != Page.Talk && page != Page.Dialogue && page != Page.Spring && page != Page.Title && page != Page.Loading;

        public void Show(Page page)
        {
            foreach (Transform child in pageRoot)
            {
                Destroy(child.gameObject);
            }

            var was = Open;
            Open = page;

            if (Game.Playing && page != Page.Talk && page != Page.Dialogue)
            {
                if (was == Page.None && page != Page.None)
                {
                    Sound.AudioDirector.Play("open", null, 0.5f);
                }
                else if (was != Page.None && page == Page.None && was != Page.Talk && was != Page.Dialogue)
                {
                    Sound.AudioDirector.Play("close", null, 0.45f);
                }
            }
            born = Time.frameCount;

            if (Game.Playing || page == Page.None)
            {
                Time.timeScale = Stops(page) ? 0 : 1;
            }

            if (Game.Hero != null && (was == Page.Spring || page == Page.Spring))
            {
                Game.Hero.Sit(page == Page.Spring);
            }

            hudRoot.gameObject.SetActive(Game.Playing && (page == Page.None || page == Page.Talk || page == Page.Dialogue || page == Page.Spring));

            switch (page)
            {
                case Page.Title: Title(); break;
                case Page.Loading: LoadingPage(); break;
                case Page.Talk: TalkPage(); break;
                case Page.Aperture: AperturePage(); break;
                case Page.Bag: BagPage(); break;
                case Page.Journal: JournalPage(); break;
                case Page.Map: MapPage(); break;
                case Page.Shop: ShopPage(Markets.Get(shopShelf), shopTitle, "商", Page.Shop); break;
                case Page.Gamble: ShopPage(Shops.Gamble, "Игорный шатёр", "运", Page.Gamble); break;
                case Page.Dialogue: DialoguePage(); break;
                case Page.Alchemy: AlchemyPage(); break;
                case Page.Bounties: BountyPage(); break;
                case Page.Codex: CodexPage(); break;
                case Page.Moves: MovesPage(); break;
                case Page.Paths: PathsPage(); break;
                case Page.Home: HomePage(); break;
                case Page.Sandbox: SandboxPage(); break;
                case Page.Clans: ClansPage(); break;
                case Page.Travel: TravelPage(); break;
                case Page.Inn: InnPage(); break;
                case Page.Settings: SettingsPage(); break;
                case Page.Board: BoardPage(); break;
                case Page.Fusion: FusionPage(); break;
                case Page.Spring: SpringPage(); break;
                case Page.Pause: PausePage(); break;
                case Page.Help: HelpPage(); break;
                case Page.Death: DeathPage(); break;
                case Page.Ending: EndingPage(); break;
                case Page.Choice: ChoicePage(); break;
            }
        }

        /// <summary>Builds the open page again (something on it changed), keeping what was chosen.</summary>
        void Redraw() => Show(Open);

        void Close() => Show(Page.None);

        public void Loading(string text)
        {
            loadingText = text;
            Show(Page.Loading);
        }

        public void Death(int lost)
        {
            lostStones = lost;
            Show(Page.Death);
        }

        void Update()
        {
            TickHud();

            if (!Game.Playing || Time.frameCount == born)
            {
                return;
            }

            var hero = Array.IndexOf(HeroPages, Open);

            if (Open == Page.None)
            {
                if (Game.Hero != null && !Game.Hero.Alive)
                {
                    return;
                }

                if (Keys.Down(Key.K)) { Show(Page.Aperture); }
                else if (Keys.Down(Key.U)) { Show(Page.Moves); }
                else if (Keys.Down(Key.P)) { Show(Page.Paths); }
                else if (Keys.Down(Key.I)) { Show(Page.Bag); }
                else if (Keys.Down(Key.J)) { Show(Page.Journal); }
                else if (Keys.Down(Key.M)) { Show(Page.Map); }
                else if (Keys.Down(Key.B)) { Show(Page.Codex); }
                else if (Keys.Down(Key.L)) { Show(Page.Clans); }
                else if (Keys.Down(Key.Escape)) { Show(Page.Pause); }
            }
            else if (hero >= 0)
            {
                var own = new[] { Key.K, Key.U, Key.P, Key.I, Key.J, Key.M, Key.B, Key.L };

                if (Keys.Down(Key.Escape) || Keys.Down(own[hero]))
                {
                    Close();
                }
                else if (Keys.Down(Key.Q))
                {
                    Show(HeroPages[(hero + HeroPages.Length - 1) % HeroPages.Length]);
                }
                else if (Keys.Down(Key.E))
                {
                    Show(HeroPages[(hero + 1) % HeroPages.Length]);
                }
                else
                {
                    for (var i = 0; i < own.Length; i++)
                    {
                        if (Keys.Down(own[i]))
                        {
                            Show(HeroPages[i]);
                        }
                    }
                }
            }
            else if (Open == Page.Talk)
            {
                if (Keys.Down(Key.Space) || Keys.Down(Key.Enter) || Keys.Down(Key.T) || Keys.LeftDown)
                {
                    NextLine();
                }
                else if (Keys.Down(Key.Escape))
                {
                    EndTalk();
                }
            }
            else if (Open == Page.Dialogue)
            {
                DialogueKeys();
            }
            else if (Open == Page.Settings)
            {
                if (Keys.Down(Key.Escape))
                {
                    Show(Page.Pause);
                }
            }
            else if (Open == Page.Help)
            {
                if (Keys.Down(Key.Escape))
                {
                    Show(Page.Pause);
                }
            }
            else if (Open == Page.Death)
            {
                if (Keys.Down(Key.Enter) || Keys.Down(Key.Space))
                {
                    Game.Wake();
                }
            }
            else if (Open != Page.Ending && Open != Page.Choice && Open != Page.Loading && Open != Page.Title)
            {
                if (Keys.Down(Key.Escape) || (Open == Page.Spring && Keys.Down(Key.T)))
                {
                    Close();
                }
            }
        }

        // ------------------------------------------------------------------ Parts the pages share

        /// <summary>A full page: the starry backdrop, the header with the hero's tabs (or a title of its own), a footer of hints. Returns where the page puts its own things.</summary>
        RectTransform Frame(string hanzi, string title, string subtitle, bool heroTabs, Action close = null, Action<RectTransform> right = null, IList<(string, string)> hints = null)
        {
            var stage = UiKit.Stage(pageRoot);
            UiKit.Backdrop(stage, hanzi);
            List<(string, string, bool, Action)> tabs = null;

            if (heroTabs)
            {
                tabs = new List<(string, string, bool, Action)>();

                for (var i = 0; i < HeroPages.Length; i++)
                {
                    var page = HeroPages[i];
                    tabs.Add((HeroTabs[i].name, HeroTabs[i].key, page == Open, () => Show(page)));
                }
            }

            UiKit.Header(stage, null, title, subtitle, tabs, close ?? Close, right, hanzi);
            UiKit.Footer(stage, hints ?? new[] { ("Esc", "Закрыть") });
            var body = UiKit.Rect("Body", stage);
            body.Place(Vector2.zero, Vector2.one, 56, 70, 56, 108);

            return body;
        }

        /// <summary>The stones in the purse, for a header's right side.</summary>
        void Purse(RectTransform right)
        {
            UiKit.Zh("元", right, 20, UiKit.Gold, TextAnchor.MiddleRight).Size(40, 26, 0);
            UiKit.Disp($"{S.State.Stones}", right, 24, UiKit.GoldText, TextAnchor.MiddleLeft).Size(40, 70, 0);
        }

        /// <summary>A column of the body between two shares of its width, its children stacked.</summary>
        static RectTransform Column(RectTransform body, float from, float to, float spacing = 10)
        {
            var column = UiKit.Rect("Column", body);
            column.Place(new Vector2(from, 0), new Vector2(to, 1));
            column.Stack<VerticalLayoutGroup>(spacing);

            return column;
        }

        /// <summary>A list that scrolls: returns what the rows go into.</summary>
        static RectTransform Scroll(RectTransform area)
        {
            var view = UiKit.Rect("View", area);
            view.Fill();
            view.gameObject.AddComponent<RectMask2D>();
            var catcher = view.gameObject.AddComponent<Image>();
            catcher.color = new Color(0, 0, 0, 0);
            catcher.raycastTarget = true;
            var content = UiKit.Rect("Content", view);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            content.Stack<VerticalLayoutGroup>(2);
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = area.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = view;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 36;

            return content;
        }

        /// <summary>A part of a column that takes what height is left (for a list).</summary>
        static RectTransform Rest(RectTransform column)
        {
            var rest = UiKit.Rect("Rest", column);
            rest.Size(-1).flexibleHeight = 1;

            return rest;
        }

        /// <summary>A row of a list: a seal, a name, and something at its right.</summary>
        static void ListLine(RectTransform list, string seal, string name, string right, Color colour, bool on, Action click, Color? rightColour = null)
        {
            var (row, _) = UiKit.ListRow(list, 46, on, click);
            var line = row.Stack<HorizontalLayoutGroup>(12, new RectOffset(26, 14, 0, 0), TextAnchor.MiddleLeft);
            line.childForceExpandWidth = false;
            UiKit.Zh(seal ?? "", row, 22, colour, TextAnchor.MiddleCenter).Size(46, 30, 0);
            UiKit.Label(name, row, 16, colour).Size(46, -1, 1);

            if (!string.IsNullOrEmpty(right))
            {
                UiKit.Label(right, row, 14, rightColour ?? UiKit.Muted, TextAnchor.MiddleRight).Size(46, 120, 0);
            }
        }

        /// <summary>Words that wrap, as tall as they need.</summary>
        static Text Para(string text, Transform parent, int size = 15, Color? colour = null)
        {
            var words = UiKit.Label(text, parent, size, colour ?? UiKit.Text2, TextAnchor.UpperLeft);
            words.horizontalOverflow = HorizontalWrapMode.Wrap;
            words.verticalOverflow = VerticalWrapMode.Overflow;
            words.lineSpacing = 1.15f;

            return words;
        }

        /// <summary>A line of a detail panel: what it is on the left, its value on the right.</summary>
        static void Fact(Transform parent, string name, string value, Color? colour = null)
        {
            var row = UiKit.Rect(name, parent);
            row.Size(24);
            UiKit.Label(name, row, 14, UiKit.Muted).rectTransform.Fill();
            UiKit.Label(value, row, 15, colour ?? UiKit.Text, TextAnchor.MiddleRight).rectTransform.Fill();
        }

        static void Gap(Transform parent, float height = 8) => UiKit.Rect("Gap", parent).Size(height);

        static Color QualityColour(int quality) => UiKit.Hex(GuBook.QualityColours[Mathf.Clamp(quality, 0, 2)]);

        // ------------------------------------------------------------------ Talk

        Line[] talkLines = new Line[0];
        int talkAt;
        Action talkDone;

        public void Talk(Line[] lines, Action done)
        {
            if (lines == null || lines.Length == 0)
            {
                done?.Invoke();
                return;
            }

            talkLines = lines;
            talkAt = 0;
            talkDone = done;
            Show(Page.Talk);
        }

        void NextLine()
        {
            talkAt++;

            if (talkAt >= talkLines.Length)
            {
                EndTalk();
            }
            else
            {
                Show(Page.Talk);
            }
        }

        void EndTalk()
        {
            var done = talkDone;
            talkDone = null;
            Close();
            done?.Invoke();
        }

        void TalkPage()
        {
            var line = talkLines[Mathf.Clamp(talkAt, 0, talkLines.Length - 1)];
            var stage = UiKit.Stage(pageRoot);
            var shade = UiKit.Image("Shade", stage, Color.white, UiKit.Plain);
            shade.rectTransform.Fill();
            shade.Gradient(new Color(3 / 255f, 4 / 255f, 10 / 255f, 0), new Color(3 / 255f, 4 / 255f, 10 / 255f, 0.96f), false, new Color(3 / 255f, 4 / 255f, 10 / 255f, 0.1f), 0.45f);
            var npc = Atlas.Npc(line.Who);
            // A passer-by of the town speaks as "~Name|Title".
            var passer = line.Who != null && line.Who.StartsWith("~") ? line.Who.Substring(1).Split('|') : null;
            var name = line.Who == "hero" ? "Гу Юэ " + S.State.HeroName : npc != null ? npc.Name : passer?[0];
            var title = line.Who == "hero" ? "Вы" : npc != null ? npc.Title : passer != null && passer.Length > 1 ? passer[1] : "";
            var words = UiKit.Rect("Words", stage);
            words.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), -460, 70, -460, -300);
            words.Stack<VerticalLayoutGroup>(12);

            if (name != null)
            {
                var who = UiKit.Rect("Who", words);
                var row = who.Stack<HorizontalLayoutGroup>(14, null, TextAnchor.MiddleLeft);
                row.childForceExpandWidth = false;
                who.Size(36);
                UiKit.Disp(name, who, 30, UiKit.GoldText).Size(36);
                UiKit.Lbl(title, who, 11, UiKit.Muted, 0.3f).Size(36);
                UiKit.Rule(words);
                var said = UiKit.Disp($"«{line.Text}»", words, 26, UiKit.Hex(0xF2EADB), TextAnchor.UpperLeft);
                said.horizontalOverflow = HorizontalWrapMode.Wrap;
                said.lineSpacing = 1.05f;
                said.Size(-1).flexibleHeight = 1;
            }
            else
            {
                // The narrator: no name, quieter.
                var told = UiKit.Disp(line.Text, words, 24, UiKit.Hex(0xC9D3D5), TextAnchor.UpperLeft);
                told.fontStyle = FontStyle.Italic;
                told.horizontalOverflow = HorizontalWrapMode.Wrap;
                told.lineSpacing = 1.08f;
                told.Size(-1).flexibleHeight = 1;
            }

            UiKit.Footer(stage, new[] { ("Пробел", talkAt + 1 < talkLines.Length ? "Дальше" : "Закончить"), ("Esc", "Пропустить") }, 56, 22, false, $"{talkAt + 1} / {talkLines.Length}");
        }

        // ------------------------------------------------------------------ Fades

        public IEnumerator FadeOut(float seconds) => FadeTo(1, seconds);
        public IEnumerator FadeIn(float seconds) => FadeTo(0, seconds);

        IEnumerator FadeTo(float alpha, float seconds)
        {
            var from = fade.color.a;

            for (var t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                fade.color = new Color(0, 0, 0, Mathf.Lerp(from, alpha, t / seconds));
                yield return null;
            }

            fade.color = new Color(0, 0, 0, alpha);
        }

        public void Hurt() => hurt.color = new Color(0.7f, 0.08f, 0.05f, 0.55f);
    }
}
