using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GuWorld.UI
{
    /// <summary>
    /// The settings (UI-Settings.dc.html): four tabs at the top (Q and E go through them) —
    /// the game, the controls, the picture, the sound; each setting a row with its name and
    /// a hint at the left and its control at the right (a slider, a toggle, a choice turned
    /// with ‹ ›, keycaps, a red button); what the tab is about at the right.
    /// </summary>
    public partial class Menus
    {
        static readonly (string name, string title, string about)[] SettingsTabs =
        {
            ("Игра", "Как ведёт себя игра", "Поле зрения и мышь подстраивают камеру. Хитбоксы — для проверки ударов: границы тел и ударов рисуются линиями."),
            ("Управление", "Клавиши", "Клавиши показаны рядом с действием — в HUD, в окнах и в подсказках внизу экрана."),
            ("Графика", "Картинка", "Тени, трава, деревья и люди видны тем дальше, чем выше качество. На слабых ноутбуках начните с низкого."),
            ("Звук", "Громкость", "Звуков в игре пока нет — громкость пригодится, когда они появятся."),
        };

        static readonly (string what, string keys)[] ControlRows =
        {
            ("Ходьба · бег", "W A S D · Shift"),
            ("Прыжок · на уступ · ухватиться за стену", "Пробел"),
            ("Присесть · ползти", "C · Z"),
            ("Гу: держать — прицел, отпустить — удар", "1 2 3 4"),
            ("Кулаки: серия · блок (вовремя — парирование) · удары", "F · ЛКМ · ПКМ · Q · R"),
            ("Перекат: в начале его ничто не задевает", "Alt"),
            ("Поглотить первобытный камень · говорить, собрать", "V · E"),
            ("Апертура · сумка · слияние", "K · I · U"),
            ("Карта · журнал · статус · тело", "M · J · O · B"),
            ("Сесть · встать (сидя — медитация)", "X"),
            ("Вид от первого лица · чат", "F5 · T"),
            ("Отладка · хитбоксы · пауза", "F3 · F7 · Esc"),
        };

        void Settings()
        {
            var stage = UiKit.Stage(pageRoot);
            UiKit.Backdrop(stage, "律", 460, new Vector2(-60 - 230, -160 - 230));
            var view = host.View;

            // The header: the title, the tabs between Q and E, the version.
            var header = UiKit.Rect("Header", stage);
            header.Place(new Vector2(0, 1), new Vector2(1, 1), 56, -84, 56, 0);
            UiKit.Image("Line", header, UiKit.Gold.A(0.28f), UiKit.FadeBoth).rectTransform.Place(Vector2.zero, new Vector2(1, 0), 0, 0, 0, -1);
            var title = UiKit.Disp("НАСТРОЙКИ", header, 26);
            title.Track(0.16f);
            title.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), 0, 2, -600, -28);
            UiKit.Lbl("Хранятся на этом компьютере", header, 11, UiKit.Muted).rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), 0, -24, -600, 6);
            var version = UiKit.Label($"Версия {Version}", header, 12, UiKit.Muted, TextAnchor.MiddleRight);
            version.rectTransform.Place(new Vector2(1, 0), Vector2.one, -300, 0, 0, 0);
            var tabRow = UiKit.Rect("Tabs", header);
            tabRow.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(10, 40));
            var tabGroup = tabRow.Stack<HorizontalLayoutGroup>(30, null, TextAnchor.MiddleCenter);
            tabGroup.childForceExpandWidth = false;
            tabRow.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            UiKit.Key("Q", tabRow);

            for (var i = 0; i < SettingsTabs.Length; i++)
            {
                var index = i;
                SettingsTab(tabRow, SettingsTabs[i].name, i == (int)tab, () =>
                {
                    tab = (Tab)index;
                    Show(Page.Settings);
                });
            }

            UiKit.Key("E", tabRow);

            // The rows.
            var list = UiKit.Rect("Rows", stage);
            list.TopLeft(56, 120, 820, 720);
            list.Stack<VerticalLayoutGroup>(0);

            switch (tab)
            {
                case Tab.Game:
                {
                    var fov = SettingRow(list, "Поле зрения", "Шире — больше видно, сильнее искажение");
                    UiKit.Slider(fov, 55, 90, GameSettings.FieldOfView, v => $"{v:0}°", v =>
                    {
                        GameSettings.FieldOfView = v;
                        GameSettings.Apply(view);
                    }).Size(60, 300, 0);
                    var mouse = SettingRow(list, "Чувствительность мыши", "Скорость поворота камеры");
                    UiKit.Slider(mouse, 0.25f, 3, GameSettings.Sensitivity, v => v.ToString("0.00"), v =>
                    {
                        GameSettings.Sensitivity = v;
                        GameSettings.Apply(view);
                    }).Size(60, 300, 0);
                    var invert = SettingRow(list, "Инверсия по вертикали", "Мышь вверх — камера вниз");
                    UiKit.Toggle(invert, GameSettings.InvertY, on =>
                    {
                        GameSettings.InvertY = on;
                        GameSettings.Apply(view);
                        Show(Page.Settings);
                    });
                    var boxes = SettingRow(list, "Хитбоксы · F7", "Границы тел и ударов линиями");
                    UiKit.Toggle(boxes, GameSettings.ShowHitboxes, on =>
                    {
                        GameSettings.ShowHitboxes = on;
                        Render.HitboxView.On = on;
                        GameSettings.Apply(view);
                        Show(Page.Settings);
                    });
                    var reset = SettingRow(list, "Сбросить игру", "Сохранение удалится. Настройки останутся.");
                    UiKit.HexButton("Сбросить…", reset, () =>
                        Ask("Сбросить игру?", "Сохранение будет удалено. Игра начнётся заново — с выбора пути и создания героя. Настройки останутся.",
                            "Да, сбросить", host.ResetGame, Page.Settings), UiKit.HexKind.Bad, 12, 40).Size(40, 170, 0);
                    var file = UiKit.Label($"Игра сохраняется сама каждые 30 секунд, при паузе и при выходе. Файл: {State.SaveGame.FilePath}", list, 12, UiKit.Muted, TextAnchor.UpperLeft);
                    file.Size(52);
                    file.rectTransform.GetComponent<LayoutElement>().minWidth = 0;
                    break;
                }
                case Tab.Controls:
                {
                    foreach (var (what, keys) in ControlRows)
                    {
                        var row = SettingRow(list, what, null, 46);
                        var caps = UiKit.Rect("Keys", row);
                        var capRow = caps.Stack<HorizontalLayoutGroup>(6, null, TextAnchor.MiddleRight);
                        capRow.childForceExpandWidth = false;
                        caps.Size(46, 300, 0);

                        foreach (var part in keys.Split(' '))
                        {
                            if (part == "·")
                            {
                                UiKit.Label("·", caps, 12, UiKit.Muted, TextAnchor.MiddleCenter).Size(28, 8, 0);
                            }
                            else
                            {
                                UiKit.Key(part, caps);
                            }
                        }
                    }

                    break;
                }
                case Tab.Graphics:
                {
                    var window = SettingRow(list, "Режим экрана", "Окно или весь экран");
                    Turn(window, new[] { "В окне", "Во весь экран" }, GameSettings.Fullscreen ? 1 : 0, i =>
                    {
                        GameSettings.Fullscreen = i == 1;
                        GameSettings.Apply(view);
                    });
                    var level = GameSettings.Current;
                    var quality = SettingRow(list, "Качество", $"Тени до {level.Shadows:0} м · трава до {level.GrassDistance:0} м · деревья до {level.TreeDistance / 1000:0.#} км · люди до {level.PeopleSight:0} м");
                    Turn(quality, Array.ConvertAll(GameSettings.Levels, l => l.Name), GameSettings.Quality, i =>
                    {
                        GameSettings.Quality = i;
                        GameSettings.Apply(view);
                    });
                    var vsync = SettingRow(list, "Вертикальная синхронизация", "Без разрывов кадра");
                    UiKit.Toggle(vsync, GameSettings.VSync, on =>
                    {
                        GameSettings.VSync = on;
                        GameSettings.Apply(view);
                        Show(Page.Settings);
                    });
                    var fps = SettingRow(list, "Показывать FPS", "Кадры в секунду в углу экрана");
                    UiKit.Toggle(fps, GameSettings.ShowFps, on =>
                    {
                        GameSettings.ShowFps = on;
                        GameSettings.Apply(view);
                        Show(Page.Settings);
                    });
                    break;
                }
                case Tab.Sound:
                {
                    var volume = SettingRow(list, "Общая громкость", "Всё сразу");
                    UiKit.Slider(volume, 0, 1, GameSettings.Volume, v => $"{v * 100:0}", v =>
                    {
                        GameSettings.Volume = v;
                        GameSettings.Apply(view);
                    }).Size(60, 300, 0);
                    break;
                }
            }

            // What the tab is about.
            var (name, about, words) = SettingsTabs[(int)tab];
            var aside = UiKit.Rect("Aside", stage);
            aside.TopRight(56, 120, 420, 400);
            aside.Stack<VerticalLayoutGroup>(14);
            UiKit.Lbl(name, aside).Size(14);
            UiKit.Disp(about, aside, 34).Size(40);
            var text = UiKit.Label(words, aside, 14, UiKit.Hex(0xA8B6B8), TextAnchor.UpperLeft);
            text.lineSpacing = 1.15f;
            text.Size(70);
            UiKit.Image("Rule", aside, UiKit.Gold.A(0.4f), UiKit.SoftEdge).Size(1, 120, 0);
            UiKit.Label("Каждая настройка действует сразу. Сброс игры не трогает настройки.", aside, 13, UiKit.Muted, TextAnchor.UpperLeft).Size(44);

            UiKit.Footer(stage, new[] { ("Q+E", "Раздел"), ("Esc", "Назад") });
            var keysOf = stage.gameObject.AddComponent<PageKeys>();
            keysOf.On(Key.Q, () =>
            {
                tab = (Tab)(((int)tab + SettingsTabs.Length - 1) % SettingsTabs.Length);
                Show(Page.Settings);
            });
            keysOf.On(Key.E, () =>
            {
                tab = (Tab)(((int)tab + 1) % SettingsTabs.Length);
                Show(Page.Settings);
            });
        }

        /// <summary>A settings tab in the header: spaced capitals; chosen — warm gold, a gold line and a diamond under it.</summary>
        static void SettingsTab(Transform parent, string name, bool on, Action open)
        {
            var tab = UiKit.Rect(name, parent);
            tab.Size(40);
            var hit = tab.gameObject.AddComponent<Image>();
            hit.color = new Color(0, 0, 0, 0);
            var words = UiKit.Label(name.ToUpperInvariant(), tab, 12, on ? UiKit.GoldText : UiKit.Muted, TextAnchor.MiddleCenter);
            words.font = UiKit.FontMedium;
            words.Track(0.28f);
            words.rectTransform.Fill();
            var fit = tab.gameObject.AddComponent<LayoutElement>();
            fit.preferredWidth = words.preferredWidth + name.Length * 12 * 0.28f + 4;

            if (on)
            {
                UiKit.Image("Under", tab, UiKit.Gold, UiKit.FadeBoth).rectTransform.Place(Vector2.zero, new Vector2(1, 0), -8, 0, -4, -1);
                var gem = UiKit.Image("Gem", tab, UiKit.Gold, UiKit.Diamond);
                gem.rectTransform.At(new Vector2(0.5f, 0), new Vector2(0, -1), new Vector2(8, 8));
            }

            var click = tab.gameObject.AddComponent<Clickable>();
            click.Click = open;

            if (!on)
            {
                click.Pointed = over => words.color = over ? UiKit.Text : UiKit.Muted;
            }
        }

        /// <summary>A setting's row: its name and a hint at the left, its control at the right (what goes in the returned row).</summary>
        static RectTransform SettingRow(Transform parent, string name, string hint, float height = 60)
        {
            var (row, click) = UiKit.ListRow(parent, height, false, null);
            var line = row.Stack<HorizontalLayoutGroup>(20, new RectOffset(26, 20, 0, 0), TextAnchor.MiddleLeft);
            line.childForceExpandWidth = false;
            var rule = UiKit.Image("Rule", row, new Color(234 / 255f, 240 / 255f, 242 / 255f, 0.06f), UiKit.Plain);
            rule.rectTransform.Place(Vector2.zero, new Vector2(1, 0), 0, 0, 0, -1);
            rule.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var words = UiKit.Rect("Words", row);
            words.Size(height, -1, 1);
            var title = UiKit.Label(name, words, 15, UiKit.Text);

            if (hint != null)
            {
                title.rectTransform.Place(new Vector2(0, 0.5f), Vector2.one, 0, 1, 0, 0);
                title.alignment = TextAnchor.LowerLeft;
                var note = UiKit.Label(hint, words, 12, UiKit.Muted, TextAnchor.UpperLeft);
                note.rectTransform.Place(Vector2.zero, new Vector2(1, 0.5f), 0, 0, 0, 3);
            }
            else
            {
                title.rectTransform.Fill();
            }

            return row;
        }

        /// <summary>A choice turned through with ‹ and ›: the value in gold between them.</summary>
        void Turn(Transform parent, string[] options, int chosen, Action<int> pick)
        {
            var holder = UiKit.Rect("Choice", parent);
            var group = holder.Stack<HorizontalLayoutGroup>(14, null, TextAnchor.MiddleCenter);
            group.childForceExpandWidth = false;
            holder.Size(60, 240, 0);
            Arrow(holder, "‹", () =>
            {
                pick((chosen + options.Length - 1) % options.Length);
                Show(Page.Settings);
            });
            UiKit.Disp(options[Mathf.Clamp(chosen, 0, options.Length - 1)], holder, 20, UiKit.GoldLit, TextAnchor.MiddleCenter).Size(60, 150, 0);
            Arrow(holder, "›", () =>
            {
                pick((chosen + 1) % options.Length);
                Show(Page.Settings);
            });
        }

        static void Arrow(Transform parent, string mark, Action click)
        {
            var box = UiKit.Image("Arrow", parent, new Color(0, 0, 0, 0.01f), UiKit.Plain);
            box.raycastTarget = true;
            UiKit.Frame(box.transform, new Color(234 / 255f, 240 / 255f, 242 / 255f, 0.16f));
            var words = UiKit.Label(mark, box.transform, 16, UiKit.Text2, TextAnchor.MiddleCenter);
            words.rectTransform.Fill();
            box.Size(30, 30, 0);
            var clickable = box.gameObject.AddComponent<Clickable>();
            clickable.Click = click;
            clickable.Pointed = over => words.color = over ? UiKit.GoldLit : UiKit.Text2;
        }
    }
}
