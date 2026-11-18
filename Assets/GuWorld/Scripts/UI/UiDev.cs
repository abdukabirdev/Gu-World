using System;
using System.Collections.Generic;
using System.Globalization;
using GuWorld.Actors;
using GuWorld.Core;
using GuWorld.World;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GuWorld.UI
{
    /// <summary>
    /// The developer's mode (ROADMAP 2.11, 12.1; main's admin panel and chat, improved). On only with
    /// Settings → Игра → «Режим разработчика» or the -dev key at launch. F3: the debug lines in the
    /// corner (frames, where, what is about, memory). / or ~: the console — a log over a line to type
    /// in, the commands that fit listed as one types, Tab completes, ↑↓ go through the lines sent,
    /// Enter runs; the world goes on. F10: the panel — the same commands as buttons, by tabs.
    /// </summary>
    public partial class Ui
    {
        static readonly bool DevArg = Array.IndexOf(Environment.GetCommandLineArgs(), "-dev") >= 0;
        public static bool DevOn => Prefs.DevMode || DevArg;

        bool devOverlay;
        Text devLine;
        float devWait;
        readonly List<string> consoleLog = new List<string> { "Консоль разработчика. /help — все команды, /find слово — найти id." };
        readonly List<string> consoleSent = new List<string>();
        int consoleBack = -1;
        string consoleDraft = "/";
        Text consoleHints;
        InputField consoleField;

        enum DevTab { Hero, Gu, Things, Enemies, Places, World }

        DevTab devTab = DevTab.Hero;

        /// <summary>What happened in the game, into the console's log (at most 200 lines kept), with the hour.</summary>
        void LogEvent(string line)
        {
            if (!DevOn)
            {
                return;
            }

            var hour = Atmosphere.Hour;
            consoleLog.Add($"<color=#7E9093>{(int)hour:00}:{(int)(hour % 1 * 60):00}</color> {line}");

            if (consoleLog.Count > 200)
            {
                consoleLog.RemoveRange(0, consoleLog.Count - 200);
            }
        }

        // ------------------------------------------------------------------ The engine's commands

        void AddEngineCommands()
        {
            Commands.Add(new CommandDef { Name = "tp", Usage = "/tp <место | x z>", About = "Перенести героя: id или имя места, деревни, фонаря — или координаты", Run = Teleport });
            Commands.Add(new CommandDef { Name = "time", Usage = "/time <час>", About = "Поставить время суток (0–24)", Run = (s, a) =>
            {
                var hour = Mathf.Repeat(float.Parse(a[0], CultureInfo.InvariantCulture), 24);
                Atmosphere.Hour = hour;
                s.SetClock(hour);

                return $"{(int)hour:00}:{(int)(hour % 1 * 60):00}";
            } });
            Commands.Add(new CommandDef { Name = "spawn", Usage = "/spawn <враг> [сколько]", About = "Позвать врагов перед героем", Run = Spawn });
            Commands.Add(new CommandDef { Name = "kill", Usage = "/kill [радиус]", About = "Убить врагов вокруг (по умолчанию 30 м)", Run = Kill });
            Commands.Add(new CommandDef { Name = "save", Usage = "/save", About = "Сохранить сейчас", Run = (s, a) => { Game.Save(); return "Сохранено"; } });
            Commands.Add(new CommandDef { Name = "aperture", Usage = "/aperture", About = "В апертуру бессмертного", Run = (s, a) => { Close(); Aperture.Enter(); return "Апертура"; } });
            Commands.Add(new CommandDef { Name = "fps", Usage = "/fps", About = "Счётчик кадров: вкл / выкл", Run = (s, a) => { Prefs.ShowFps = !Prefs.ShowFps; return Prefs.ShowFps ? "FPS показан" : "FPS скрыт"; } });
            Commands.Add(new CommandDef { Name = "stats", Usage = "/stats", About = "Что сейчас в мире: враги, люди, память", Run = (s, a) => Stats() });
        }

        static string Teleport(Session session, string[] words)
        {
            Vector2 to;

            if (words.Length >= 2 && float.TryParse(words[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x) && float.TryParse(words[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var z))
            {
                to = new Vector2(x, z);
            }
            else
            {
                var word = string.Join(" ", words).ToLowerInvariant();
                bool Fits(string id, string name) => id.ToLowerInvariant() == word || (word.Length >= 3 && (name ?? "").ToLowerInvariant().Contains(word));
                var place = Array.Find(Atlas.Places, p => Fits(p.Id, p.Name));
                var settlement = Array.Find(Regions.Settlements, p => Fits(p.Id, p.Name));
                var thing = Array.Find(Atlas.Things, t => t.Kind == ThingKind.Lantern && Fits(t.Id, t.Name));

                if (place != null)
                {
                    to = new Vector2(place.At.X, place.At.Z);
                }
                else if (settlement != null)
                {
                    to = new Vector2(settlement.At.X, settlement.At.Z);
                }
                else if (thing != null)
                {
                    to = new Vector2(thing.At.X, thing.At.Z - 2.5f);
                }
                else
                {
                    return $"Нет места «{word}». /find — найти по имени.";
                }
            }

            var at = Game.Ground(new Vector3(to.x, 0, to.y)) + Vector3.up * 0.3f;
            Streamer.Instance?.LoadNow(at);
            Game.Hero.Teleport(at);

            return $"Герой в {to.x:0}, {to.y:0}";
        }

        static string Spawn(Session session, string[] words)
        {
            var def = Bestiary.Get(words[0]);

            if (def == null)
            {
                return $"Нет врага «{words[0]}». /find — найти по имени.";
            }

            var count = Mathf.Clamp(words.Length > 1 ? int.Parse(words[1], CultureInfo.InvariantCulture) : 1, 1, 12);
            var hero = Game.Hero.transform;

            for (var i = 0; i < count; i++)
            {
                var angle = (i - (count - 1) * 0.5f) * 20;
                var forward = Quaternion.Euler(0, hero.eulerAngles.y + angle, 0) * Vector3.forward;
                Enemy.Spawn(def, Game.Ground(hero.position + forward * 10) + Vector3.up * 0.3f, hero.eulerAngles.y + 180, "dev");
            }

            return $"{def.Name} ×{count}";
        }

        static string Kill(Session session, string[] words)
        {
            var reach = words.Length > 0 ? float.Parse(words[0], CultureInfo.InvariantCulture) : 30;
            var hero = Game.Hero.transform.position;
            var killed = 0;

            foreach (var enemy in new List<Enemy>(Enemy.Living))
            {
                if (enemy.Alive && (enemy.transform.position - hero).sqrMagnitude < reach * reach)
                {
                    enemy.TakeHit(1e7f, false, hero);
                    killed++;
                }
            }

            return $"Убито: {killed}";
        }

        static string Stats()
        {
            var memory = GC.GetTotalMemory(false) / (1024f * 1024f);

            return $"Враги {Enemy.Living.Count} · тела {Actor.All.Count} · качество {Quality.Name} (мир {Quality.Level}) · авто-детализация {Governor.Detail:0.00} · память C# {memory:0} МБ";
        }

        // ------------------------------------------------------------------ Keys and the corner lines

        void DevTick()
        {
            if (!DevOn)
            {
                if (devLine != null)
                {
                    devLine.enabled = false;
                }

                return;
            }

            if (devLine == null)
            {
                devLine = UiKit.Label("", topCanvas.transform, 13, UiKit.Text2, TextAnchor.UpperLeft).Shadowed();
                devLine.rectTransform.TopLeft(14, 34, 900, 140);
                devLine.raycastTarget = false;
                devLine.horizontalOverflow = HorizontalWrapMode.Overflow;
            }

            if (Keys.Down(Key.F3))
            {
                devOverlay = !devOverlay;
            }

            if (Game.Playing && Open == Page.None)
            {
                if (Keys.Down(Key.Slash) || Keys.Down(Key.Backquote))
                {
                    consoleDraft = "/";
                    Show(Page.Console);
                }
                else if (Keys.Down(Key.F10))
                {
                    Show(Page.Dev);
                }
            }
            else if (Game.Playing && Open == Page.Dev && Keys.Down(Key.F10))
            {
                Close();
            }

            devLine.enabled = devOverlay;
            devWait -= Time.unscaledDeltaTime;

            if (!devOverlay || devWait > 0)
            {
                return;
            }

            devWait = 0.25f;
            var lines = $"GU World {Version} · {1f / Mathf.Max(0.0001f, Time.smoothDeltaTime):0} FPS · {Time.smoothDeltaTime * 1000:0.0} мс · {Screen.width}×{Screen.height} · {Quality.Name}";

            if (Game.Hero != null && Game.Session != null)
            {
                var p = Game.Hero.transform.position;
                var place = Atlas.PlaceAt(new MapPoint(p.x, p.z));
                lines += $"\nx {p.x:0.0} · y {p.y:0.0} · z {p.z:0.0} · земля {Ground.HeightAt(p.x, p.z):0.0} · {(place != null ? place.Name : "—")}";
                lines += $"\nдень {Game.Session.State.Day} · {Atmosphere.Hour:0.00} ч · {Weather.Name(Atmosphere.Sky)} · глава {Game.Session.Chapter} · {(Game.Session.Godmode ? "бессмертие" : "")}";
            }

            lines += "\n" + Stats();
            lines += "\nF3 — эти строки · / — консоль · F10 — панель";
            devLine.text = lines;
        }

        // ------------------------------------------------------------------ The console

        void ConsolePage()
        {
            var stage = UiKit.Stage(pageRoot);
            var shade = UiKit.Image("Shade", stage, Color.white, UiKit.Plain);
            shade.rectTransform.Fill();
            shade.Gradient(new Color(3 / 255f, 4 / 255f, 10 / 255f, 0), new Color(3 / 255f, 4 / 255f, 10 / 255f, 0.92f), false, new Color(3 / 255f, 4 / 255f, 10 / 255f, 0.2f), 0.45f);

            var box = UiKit.Rect("Console", stage);
            box.Place(new Vector2(0, 0), new Vector2(0, 0), 40, 60, -940, -600);
            box.Stack<VerticalLayoutGroup>(4, null, TextAnchor.LowerLeft);
            UiKit.Lbl("Консоль разработчика", box, 11, UiKit.LabelGold).Size(16);
            var log = UiKit.Label(string.Join("\n", consoleLog.GetRange(Mathf.Max(0, consoleLog.Count - 18), Mathf.Min(18, consoleLog.Count))), box, 14, UiKit.Text2, TextAnchor.LowerLeft);
            log.horizontalOverflow = HorizontalWrapMode.Wrap;
            log.verticalOverflow = VerticalWrapMode.Truncate;
            log.Size(400);
            consoleHints = UiKit.Label("", box, 13, UiKit.GoldText, TextAnchor.LowerLeft);
            consoleHints.Size(110);
            var line = UiKit.Rect("Line", box);
            line.Size(44);
            consoleField = UiKit.Field(line, consoleDraft, "/команда", 200, text => { consoleDraft = text; ShowHints(); });
            ((RectTransform)consoleField.transform).Fill();
            // Focused a frame later, so the key that opened the console is not typed into it.
            var draft = consoleDraft;
            consoleField.gameObject.AddComponent<FocusLater>().Text = draft;
            ShowHints();

            UiKit.Footer(stage, new[] { ("Enter", "Выполнить"), ("Tab", "Дополнить"), ("↑↓", "Прошлые"), ("Esc", "Закрыть") });
            var keys = stage.gameObject.AddComponent<PageKeys>();
            keys.On(Key.Enter, RunConsole);
            keys.On(Key.NumpadEnter, RunConsole);
            keys.On(Key.Escape, Close);
            keys.On(Key.Tab, () =>
            {
                var fits = Commands.Matching(consoleDraft);

                if (fits.Count > 0)
                {
                    consoleDraft = "/" + fits[0].Name + " ";
                    Redraw();
                }
            });
            keys.On(Key.UpArrow, () => Recall(1));
            keys.On(Key.DownArrow, () => Recall(-1));
        }

        void ShowHints()
        {
            if (consoleHints == null)
            {
                return;
            }

            var fits = Commands.Matching(consoleDraft);
            var hints = new List<string>();

            for (var i = 0; i < Mathf.Min(5, fits.Count); i++)
            {
                hints.Add($"{fits[i].Usage}  <color=#7E9093>— {fits[i].About}</color>");
            }

            consoleHints.text = string.Join("\n", hints);
        }

        void Recall(int step)
        {
            if (consoleSent.Count == 0)
            {
                return;
            }

            consoleBack = Mathf.Clamp(consoleBack + step, 0, consoleSent.Count - 1);
            consoleDraft = consoleSent[consoleSent.Count - 1 - consoleBack];
            Redraw();
        }

        void RunConsole()
        {
            var line = consoleDraft.Trim();

            if (line.Length <= 1)
            {
                return;
            }

            consoleSent.Add(line);
            consoleBack = -1;
            consoleLog.Add("> " + line);

            foreach (var said in Commands.Run(Game.Session, line).Split('\n'))
            {
                consoleLog.Add("  " + said);
            }

            consoleDraft = "/";

            if (Open == Page.Console)
            {
                Redraw();
            }
        }

        // ------------------------------------------------------------------ The panel (F10)

        void DevRun(string command)
        {
            var said = Commands.Run(Game.Session, command);
            consoleLog.Add("> " + command);
            consoleLog.Add("  " + said);
            Toast(said.Split('\n')[0]);

            if (Open == Page.Dev)
            {
                Redraw();
            }
        }

        void DevPage()
        {
            var session = S;
            var body = Frame("开", "Разработчик", $"{Commands.All.Count} команд · всё то же — в консоли (/)", false, Close, null, new[] { ("F10", "Закрыть"), ("Esc", "Закрыть") });
            var top = UiKit.Rect("Tabs", body);
            top.Place(new Vector2(0, 1), new Vector2(1, 1), 0, -48, 0, 0);
            var names = new[] { "Герой", "Гу", "Вещи", "Враги", "Места", "Мир" };
            UiKit.Choices(top, names, (int)devTab, i => { devTab = (DevTab)i; Redraw(); }, 14);
            var area = UiKit.Rect("Area", body);
            area.Place(Vector2.zero, Vector2.one, 0, 0, 0, 60);
            var list = Scroll(area);

            switch (devTab)
            {
                case DevTab.Hero:
                    DevRow(list, "Здоровье и эссенция", ("Полные", "/heal"), (session.Godmode ? "Бессмертие: вкл" : "Бессмертие: выкл", "/god"));
                    DevRow(list, "Камни", ("+100", "/stones 100"), ("+1000", "/stones 1000"), ("+10000", "/stones 10000"));
                    DevRow(list, "Ранг", ("I", "/rank 1"), ("II", "/rank 2"), ("III", "/rank 3"), ("IV", "/rank 4"), ("V", "/rank 5"));
                    DevRow(list, "Талант", ("D 30", "/talent 30"), ("C 44", "/talent 44"), ("B 62", "/talent 62"), ("A 88", "/talent 88"), ("S 100", "/talent 100"));
                    DevRow(list, "Кодекс", ("Открыть весь", "/codex"));
                    break;
                case DevTab.Gu:
                    foreach (var gu in GuBook.All)
                    {
                        DevRow(list, $"{GuBook.RankNumerals[gu.Rank]} · {gu.Name}", ("Дать", $"/give {gu.Id}"), ("Отменный", $"/give {gu.Id} 1 2"), ("Совершенный", $"/give {gu.Id} 1 3"));
                    }

                    break;
                case DevTab.Things:
                    foreach (var item in ItemBook.All)
                    {
                        DevRow(list, item.Name, ("×1", $"/give {item.Id}"), ("×10", $"/give {item.Id} 10"));
                    }

                    break;
                case DevTab.Enemies:
                    DevRow(list, "Вокруг", ("Убить всех в 30 м", "/kill 30"), ("в 100 м", "/kill 100"));

                    foreach (var enemy in Bestiary.All)
                    {
                        DevRow(list, $"{enemy.Name} · ур. {enemy.Level}", ("Позвать", $"/spawn {enemy.Id}"), ("×3", $"/spawn {enemy.Id} 3"));
                    }

                    break;
                case DevTab.Places:
                    foreach (var settlement in Regions.Settlements)
                    {
                        DevRow(list, "Деревня · " + settlement.Name, ("Туда", $"/tp {settlement.At.X.ToString(CultureInfo.InvariantCulture)} {settlement.At.Z.ToString(CultureInfo.InvariantCulture)}"));
                    }

                    foreach (var place in Atlas.Places)
                    {
                        DevRow(list, place.Name, ("Туда", $"/tp {place.Id}"));
                    }

                    break;
                case DevTab.World:
                    DevRow(list, "Время", ("Рассвет", "/time 6"), ("Полдень", "/time 12"), ("Закат", "/time 18.5"), ("Полночь", "/time 0"));
                    DevRow(list, "Игра", ("Сохранить", "/save"), ("Апертура", "/aperture"), ("FPS", "/fps"));
                    DevRow(list, "Сейчас", ("Статистика", "/stats"));
                    break;
            }
        }

        /// <summary>A row of the panel: a name at the left, buttons running commands at the right.</summary>
        void DevRow(RectTransform list, string name, params (string label, string command)[] buttons)
        {
            var row = UiKit.Rect(name, list);
            row.Size(48);
            var line = row.Stack<HorizontalLayoutGroup>(8, new RectOffset(12, 12, 4, 4), TextAnchor.MiddleLeft);
            line.childForceExpandWidth = false;
            UiKit.Label(name, row, 15, UiKit.Text).Size(40, -1, 1);

            foreach (var (label, command) in buttons)
            {
                var run = command;
                UiKit.HexButton(label, row, () => DevRun(run), UiKit.HexKind.Ghost, 11, 38).Size(38, 150, 0);
            }
        }
    }

    /// <summary>Focuses a text field on the frame after it is made, with its text and the caret at its end.</summary>
    public class FocusLater : MonoBehaviour
    {
        public string Text;
        int born;

        void Awake() => born = Time.frameCount;

        void Update()
        {
            if (Time.frameCount == born)
            {
                return;
            }

            var field = GetComponent<InputField>();
            field.text = Text;
            field.ActivateInputField();
            field.MoveTextEnd(false);
            Destroy(this);
        }
    }
}
