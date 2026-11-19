using System;
using System.Collections.Generic;
using GuWorld.Actors;
using GuWorld.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GuWorld.UI
{
    /// <summary>
    /// The settings (ROADMAP 2.7, main's UI-Settings.dc.html): six tabs — the game, the controls, the
    /// picture, the sound, the interface, accessibility — Q and E go through them; each setting a row
    /// with its name and a hint at the left and its control at the right; what the tab is about and
    /// "back to how it was" at the right. The same page from the main menu and from the pause; this
    /// game's own rules (in its save) only from the pause. Each setting takes effect at once.
    /// </summary>
    public partial class Ui
    {
        enum SettingsTab { Game, Controls, Picture, Sound, Interface, Access }

        static readonly (SettingsTab tab, string name, string about)[] SettingsTabs =
        {
            (SettingsTab.Game, "Игра", "Подсказки, числа и метки над миром, автосохранение. Из паузы — ещё и правила этой игры: голод Гу, кормление, переходы. Путь и цену смерти выбирают один раз, в новой игре."),
            (SettingsTab.Controls, "Управление", "Мышь, камера и клавиши. Нажмите на клавишу действия, потом новую — клавишу, кнопку мыши или колесо; у каждого действия их две. Занятая другим действием переходит к нему в обмен."),
            (SettingsTab.Picture, "Графика", "Пять уровней качества — или свои числа. Всё меняется сразу, кроме «детализации мира» (рельеф и дальность леса): она — когда мир строится заново."),
            (SettingsTab.Sound, "Звук", "Громкость по слоям: музыка, удары и Гу, окружение. И что делать со звуком, когда окно игры не впереди."),
            (SettingsTab.Interface, "Интерфейс", "Размер и прозрачность HUD, компас, миникарта, часы."),
            (SettingsTab.Access, "Доступность", "Меньше вспышек и тряски, цвета для дальтоников, крупный текст. Моно — во вкладке «Звук»."),
        };

        SettingsTab settingsTab = SettingsTab.Game;
        Page settingsBack = Page.Title;
        string rebinding;
        /// <summary>Which of the action's two places waits for an input: 0 the main one, 1 the second.</summary>
        int rebindSlot;
        int rebindEnded = -1;

        void OpenSettings(Page back)
        {
            settingsBack = back;
            rebinding = null;
            Show(Page.Settings);
        }

        void LeaveSettings() => Show(settingsBack == Page.Pause && Game.Playing ? Page.Pause : Page.Title);

        bool KeysFree => rebinding == null && Time.frameCount != rebindEnded;

        void SettingsPage()
        {
            var stage = UiKit.Stage(pageRoot);
            UiKit.Backdrop(stage, "设");
            var tabs = new List<(string, string, bool, Action)>();

            foreach (var (tab, name, _) in SettingsTabs)
            {
                var t = tab;
                tabs.Add((name, null, tab == settingsTab, () => { settingsTab = t; rebinding = null; Redraw(); }));
            }

            UiKit.Header(stage, null, "Настройки", InGame ? "Этот компьютер · и правила этой игры" : "Хранятся на этом компьютере", tabs, LeaveSettings, null, "设");
            UiKit.Footer(stage, new[] { ("↑↓", "Строка"), ("← →", "Значение"), ("Enter", "Взять"), ("Q E", "Раздел"), ("Esc", "Назад") });
            var body = UiKit.Rect("Body", stage);
            body.Place(Vector2.zero, Vector2.one, 56, 70, 56, 108);

            // The rows at the left (they scroll), what the tab is about at the right.
            var listArea = UiKit.Rect("List", body);
            listArea.Place(Vector2.zero, new Vector2(0.66f, 1));
            var list = Scroll(listArea);
            var aside = UiKit.Rect("Aside", body);
            aside.Place(new Vector2(0.7f, 0), Vector2.one);
            aside.Stack<VerticalLayoutGroup>(10);
            var about = Array.Find(SettingsTabs, s => s.tab == settingsTab);
            UiKit.Lbl(about.name, aside, 11, UiKit.LabelGold).Size(16);
            Para(about.about, aside, 14, UiKit.Text2);
            Gap(aside, 6);
            Para("Каждая настройка действует сразу.", aside, 13, UiKit.Muted);
            Gap(aside, 10);
            // A tab's own settings all go back at once: held, so a stray click undoes nothing (ROADMAP 2.12).
            UiKit.HexButton("Удерживать — вернуть как было", aside, ResetTab, UiKit.HexKind.Ghost, 12, 44).Held(1.2f).Size(44, 320, 0);

            switch (settingsTab)
            {
                case SettingsTab.Game: GameTab(list); break;
                case SettingsTab.Controls: ControlsTab(list); break;
                case SettingsTab.Picture: PictureTab(list); break;
                case SettingsTab.Sound: SoundTab(list); break;
                case SettingsTab.Interface: InterfaceTab(list); break;
                case SettingsTab.Access: AccessTab(list); break;
            }

            var keys = stage.gameObject.AddComponent<PageKeys>();
            keys.On(Key.Escape, () => { if (KeysFree) { LeaveSettings(); } });
            keys.On(Key.Q, () => { if (KeysFree) { TurnTab(-1); } });
            keys.On(Key.E, () => { if (KeysFree) { TurnTab(1); } });

            if (rebinding != null)
            {
                // While an input is awaited, a click is that input, not a press of a button under it: a veil over the page takes the clicks and the wheel.
                var veil = UiKit.Image("Waiting", stage, new Color(0, 0, 0, 0.35f), UiKit.Plain);
                veil.rectTransform.Fill();
                veil.raycastTarget = true;
                var asked = UiKit.Label($"{Array.Find(Bindings.All, a => a.Id == rebinding)?.Name} · {(rebindSlot == 0 ? "основная" : "вторая")}: нажмите клавишу, кнопку мыши или поверните колесо · Backspace — убрать · Esc — отмена",
                    veil.transform, 16, UiKit.GoldLit, TextAnchor.MiddleCenter);
                asked.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(1, 0.5f), 56, -30, 56, -30);
                stage.gameObject.AddComponent<KeyCatcher>().Caught = KeyCaught;
            }
        }

        /// <summary>A game is being played (not the main menu, not the guest proving ground): its own rules can be shown.</summary>
        bool InGame => Game.Playing && Game.Session != null && !Game.SandboxOnly;

        void TurnTab(int step)
        {
            var count = SettingsTabs.Length;
            settingsTab = SettingsTabs[((int)settingsTab + step + count) % count].tab;
            Redraw();
        }

        void ResetTab()
        {
            switch (settingsTab)
            {
                case SettingsTab.Game: Prefs.ResetGame(); break;
                case SettingsTab.Controls: Prefs.ResetControls(); Game.Rig?.Dress(); break;
                case SettingsTab.Picture: Prefs.ResetPicture(); break;
                case SettingsTab.Sound: Prefs.ResetSound(); break;
                default: Prefs.ResetInterface(); break;
            }

            Redraw();
        }

        // ------------------------------------------------------------------ The tabs

        void GameTab(RectTransform list)
        {
            if (InGame)
            {
                var settings = S.State.Settings;
                Section(list, "Эта игра");
                Fixed(list, "Путь", $"{DifficultyRules.Name(settings.Born)} · талант при рождении {Cultivation.TalentName(Words.BornTalent(S.State))}", "Выбирают один раз, в новой игре");
                Fixed(list, "Цена смерти", DeathRules.Name(settings.Death), DeathRules.Text(settings.Death));
                Choice(list, "Голод Гу", settings.Hunger == HungerRule.Off ? "Гу не нужно кормить" : settings.Hunger == HungerRule.Soft ? "Голодный Гу слабее на треть, но не умирает" : $"Голодный Гу умирает через {Session.StarveDays} дня, как в каноне",
                    new[] { "Нет", "Мягкий", "Канон" }, (int)settings.Hunger, i => { settings.Hunger = (HungerRule)i; S.Recompute(); });
                Switch(list, "Гу сами едят из сумки", "На рассвете, если корм есть", settings.AutoFeed, on => settings.AutoFeed = on);
                Switch(list, "Переходы стоят камней", "Между лунными фонарями", settings.TravelCosts, on => settings.TravelCosts = on);
                Section(list, "Всегда");
            }

            Switch(list, "Подсказки", "Строка совета о клавишах, пока вы их не освоили", Prefs.Hints, on => Prefs.Hints = on);
            Choice(list, "Числа урона", "Над тем, по кому пришёлся удар", new[] { "Все", "По врагам", "Нет" }, Prefs.DamageNumbers, i => Prefs.DamageNumbers = i);
            Switch(list, "Слова реакций", "«Пар», «Раскол» над врагом", Prefs.ReactionWords, on => Prefs.ReactionWords = on);
            Choice(list, "Метка задания", "Ромб над местом задания и стрелка у края экрана", new[] { "В мире", "Компас", "Нет" }, Prefs.QuestMark, i => Prefs.QuestMark = i);
            Switch(list, "Режим разработчика", "F3 — строки отладки, F7 — хитбоксы, / — консоль команд, F10 — панель. Игра помечает, что команды были", Prefs.DevMode, on => Prefs.DevMode = on);
            Choice(list, "Текст в разговорах", "Сразу целиком — или печатается; Пробел допечатывает", new[] { "Сразу", "Быстро", "Медленно" }, Prefs.TextSpeed, i => Prefs.TextSpeed = i);
            Choice(list, "Автосохранение", "Игра сохраняется сама, при паузе и при выходе", new[] { "30 с", "2 мин", "5 мин" },
                Math.Max(0, Array.IndexOf(Prefs.AutosaveChoices, Prefs.Autosave)), i => Prefs.Autosave = Prefs.AutosaveChoices[i]);

            Section(list, "Файлы");
            var saves = Row(list, "Сохранения и настройки", Application.persistentDataPath);
            UiKit.HexButton("Открыть папку", saves, () => ErrorLog.Open(Application.persistentDataPath), UiKit.HexKind.Ghost, 11, 40).Size(40, 200, 0);
            var log = Row(list, "Журнал ошибок", ErrorLog.Count == 0 ? "В этом запуске ошибок нет. Если что-то сломается — пришлите файл из этой папки" : $"В этом запуске ошибок: {ErrorLog.Count}. Пришлите файл {System.IO.Path.GetFileName(ErrorLog.File)} из этой папки");
            UiKit.HexButton("Открыть папку", log, () => ErrorLog.Open(ErrorLog.Folder), UiKit.HexKind.Ghost, 11, 40).Size(40, 200, 0);

            if (InGame)
            {
                Section(list, "Опасно");
                var row = Row(list, "Бросить эту игру", "Сохранение в этой ячейке удалится. Настройки останутся.");
                UiKit.HexButton("Удерживать — бросить", row, () =>
                {
                    Game.DeleteSlot(Game.Slot);
                    Game.Instance.ToTitle(false);
                }, UiKit.HexKind.Bad, 11, 40).Held(1.2f).Size(40, 240, 0);
            }
        }

        void ControlsTab(RectTransform list)
        {
            Slide(list, "Мышь по горизонтали", "Скорость поворота камеры", 0.2f, 3, Prefs.SensitivityX, v => $"{v:0.0}", v => Prefs.SensitivityX = v);
            Slide(list, "Мышь по вертикали", "Скорость наклона камеры", 0.2f, 3, Prefs.SensitivityY, v => $"{v:0.0}", v => Prefs.SensitivityY = v);
            Switch(list, "Инверсия по вертикали", "Мышь вверх — камера вниз", Prefs.InvertY, on => Prefs.InvertY = on);
            Slide(list, "Поле зрения", "Шире — больше видно, сильнее искажение по краям", 50, 100, Prefs.Fov, v => $"{v:0}°", v => { Prefs.Fov = Mathf.Round(v); Game.Rig?.Dress(); });
            Slide(list, "Поле зрения глазами героя", "Вид от первого лица (F5)", 55, 110, Prefs.FovFirst, v => $"{v:0}°", v => { Prefs.FovFirst = Mathf.Round(v); Game.Rig?.Dress(); });
            Slide(list, "Тряска камеры", "От ударов и взрывов", 0, 1, Prefs.CameraShake, v => $"{v * 100:0}%", v => Prefs.CameraShake = v);
            Slide(list, "Покачивание головы", "Вид глазами героя (F5): шаги качают взгляд", 0, 1, Prefs.HeadBob, v => $"{v * 100:0}%", v => Prefs.HeadBob = v);
            Slide(list, "Дистанция камеры", "С какой начинать; в игре — колесо", 2.4f, 11, Prefs.CameraDistance, v => $"{v:0.0} м", v => Prefs.CameraDistance = Mathf.Round(v * 10) / 10);
            Slide(list, "Сглаживание камеры", "Плотнее — камера держится вплотную; мягче — догоняет плавно", 0, 1, Prefs.CameraSmooth, v => v < 0.25f ? "плотно" : v > 0.75f ? "мягко" : "средне", v => Prefs.CameraSmooth = v);
            Switch(list, "Камера сама за спину", "Идёте, не трогая мышь, — взгляд плавно встаёт за спиной героя", Prefs.CameraRecenter, on => Prefs.CameraRecenter = on);
            Choice(list, "Бег во весь дух", "Удерживать клавишу — или нажать раз: бег до остановки", new[] { "Удерживать", "Переключать" }, Prefs.SprintToggle ? 1 : 0, i => Prefs.SprintToggle = i == 1);
            Choice(list, "Захват цели", "Нажать — взять и отпустить цель; или держать, пока клавиша нажата", new[] { "Нажатием", "Удержанием" }, Prefs.TargetHold ? 1 : 0, i => Prefs.TargetHold = i == 1);

            Section(list, "Клавиши");

            foreach (var action in Bindings.All)
            {
                var id = action.Id;
                // A key the game uses for something fixed (walking, a page): both would happen at once.
                var clash = Bindings.ClashOf(action.Inputs[0]);
                var clash2 = Bindings.ClashOf(action.Inputs[1]);
                var hint = action.Inputs[0] == 0 ? $"<color=#{ColorUtility.ToHtmlStringRGB(Accessible.Harm)}>не назначено — без клавиши действие недоступно</color>"
                    : clash != "" || clash2 != "" ? $"<color=#{ColorUtility.ToHtmlStringRGB(Accessible.Harm)}>занята: {(clash != "" ? clash : clash2)} — выберите другую</color>"
                    : action.Changed ? $"изменено · было {Bindings.Label(action.Defaults[0])}{(action.Defaults[1] != 0 ? " · " + Bindings.Label(action.Defaults[1]) : "")}" : null;
                var row = Row(list, action.Name, hint);

                for (var slot = 0; slot < 2; slot++)
                {
                    var at = slot;
                    var waiting = rebinding == id && rebindSlot == slot;
                    var input = action.Inputs[slot];
                    var bad = (slot == 0 ? clash : clash2) != "";
                    var words = waiting ? "…нажмите" : input == 0 ? (slot == 0 ? "—" : "+ вторая") : Bindings.Label(input);
                    var button = UiKit.HexButton(words, row, () => { rebinding = id; rebindSlot = at; Redraw(); },
                        waiting ? UiKit.HexKind.Solid : bad ? UiKit.HexKind.Bad : slot == 1 && input == 0 ? UiKit.HexKind.Off : UiKit.HexKind.Ghost, 12, 40);
                    button.Size(40, 150, 0);
                }
            }

            Para("У каждого действия две клавиши: основная (её показывают HUD и подсказки) и вторая. Годятся клавиши, кнопки мыши (ЛКМ, ПКМ, СКМ, боковые) и колесо вверх или вниз. Клавиша, уже занятая другим действием, переходит к нему в обмен. WASD, буквы страниц (K U P I J M B L O) и F-клавиши пока не меняются.", list, 13, UiKit.Muted);
        }

        /// <summary>The input caught for the place that waits: Esc cancels, Backspace or Delete empties the place.</summary>
        void KeyCaught(int input)
        {
            if (rebinding != null)
            {
                if (input == (int)Key.Backspace || input == (int)Key.Delete)
                {
                    Bindings.Clear(rebinding, rebindSlot);
                }
                else if (input != (int)Key.Escape)
                {
                    Bindings.Bind(rebinding, rebindSlot, input);
                }
            }

            rebinding = null;
            rebindEnded = Time.frameCount;
            Redraw();
        }

        void PictureTab(RectTransform list)
        {
            Choice(list, "Режим экрана", "Окно, весь экран без рамки или весь экран", new[] { "В окне", "Без рамки", "Весь экран" }, Prefs.WindowMode, i => Prefs.WindowMode = i);
            var sizes = Prefs.Resolutions();
            sizes.Insert(0, "");
            var at = Math.Max(0, sizes.IndexOf(Prefs.Resolution));
            Turn(list, "Разрешение", "Размер картинки на экране", sizes, at, size => size == "" ? "Как у экрана" : size.Replace("x", " × "), i => Prefs.Resolution = sizes[i]);
            Switch(list, "Вертикальная синхронизация", "Без разрывов кадра", Prefs.VSync, on => Prefs.VSync = on);
            Choice(list, "Предел кадров", Prefs.VSync ? "С синхронизацией — не выше частоты экрана" : "Сколько кадров в секунду рисовать",
                new[] { "Нет", "30", "60", "90", "120", "144" }, Math.Max(0, Array.IndexOf(Prefs.FpsChoices, Prefs.FpsCap)), i => Prefs.FpsCap = Prefs.FpsChoices[i]);

            Section(list, "Качество");
            var presets = new List<string>(Quality.Names);
            var chosen = Quality.Preset;

            if (chosen == Quality.Custom)
            {
                presets.Add("Свои");
                chosen = presets.Count - 1;
            }

            Choice(list, "Уровень", "Пять уровней от слабой встроенной графики до мощной видеокарты", presets, chosen, i =>
            {
                if (i < Quality.Names.Length)
                {
                    Quality.Set(i);
                }
            });

            var now = Quality.Now;
            Slide(list, "Масштаб картинки", "Меньше — быстрее и мягче; картинка растягивается до экрана", 0.5f, 1, now.Scale, v => $"{v * 100:0}%",
                v => { var n = Quality.Now; n.Scale = Mathf.Round(v * 20) / 20; Quality.Change(n); });
            var smooth = now.Smaa ? 3 : now.Msaa >= 4 ? 2 : now.Msaa >= 2 ? 1 : 0;
            Choice(list, "Сглаживание", "Края моделей без лесенки", new[] { "Нет", "MSAA ×2", "MSAA ×4", "×4 + SMAA" }, smooth, i =>
            {
                var n = Quality.Now;
                n.Msaa = i == 0 ? 1 : i == 1 ? 2 : 4;
                n.Smaa = i == 3;
                Quality.Change(n);
            });
            var shadowSteps = new[] { 0f, 30, 60, 130, 220 };
            var shadowAt = 0;

            for (var s = 0; s < shadowSteps.Length; s++)
            {
                if (now.Shadows >= shadowSteps[s] - 0.5f)
                {
                    shadowAt = s;
                }
            }

            Choice(list, "Тени", "Как далеко от камеры", new[] { "Нет", "30 м", "60 м", "130 м", "220 м" }, shadowAt, i => { var n = Quality.Now; n.Shadows = shadowSteps[i]; Quality.Change(n); });
            Slide(list, "Деревья", "Сколько дальних деревьев видно · сразу", 0.3f, 1, now.Trees, v => $"{v * 100:0}%",
                v => { var n = Quality.Now; n.Trees = Mathf.Round(v * 20) / 20; Quality.Change(n); });
            Slide(list, "Трава", "Густота и дальность травы · сразу", 0.3f, 1.6f, Quality.Grass, v => $"{v * 100:0}%",
                v => { var n = Quality.Now; n.Grass = Mathf.Round(v * 20) / 20; Quality.Change(n); });
            Slide(list, "Люди", "Сколько горожан видно и как далеко · сразу", 0.5f, 1.5f, Quality.People, v => $"{v * 100:0}%",
                v => { var n = Quality.Now; n.People = Mathf.Round(v * 20) / 20; Quality.Change(n); });
            Choice(list, "Модель героя", "Полная — 626 тыс. треугольников, средняя — 70 тыс. · сразу", new[] { "По уровню", "Полная", "Средняя" }, Mathf.Clamp(now.Hero, 0, 2),
                i => { var n = Quality.Now; n.Hero = i; Quality.Change(n); });
            Choice(list, "Детализация мира", InGame ? "Лес, трава, люди, рельеф — перестроятся при загрузке" : "Лес, трава, люди, рельеф", new[] { "Лёгкая", "Обычная", "Высокая" }, now.Tier, i =>
            {
                var n = Quality.Now;
                n.Tier = i;
                Quality.Change(n);
            });

            if (InGame)
            {
                var row = Row(list, "Перестроить мир сейчас", "Сохранить и загрузить снова — с новой детализацией");
                UiKit.HexButton("Перестроить", row, () => Game.Instance.Load(Game.Slot), UiKit.HexKind.Ghost, 12, 40).Size(40, 200, 0);
            }

            Switch(list, "Авто-детализация", "Трава, горожане и тени убавляются сами, если кадров мало", World.Governor.On, on => World.Governor.On = on);
            Slide(list, "Яркость", Quality.Level >= 1 ? "Экспозиция картинки" : "Работает от «Среднего» уровня", 0.6f, 1.4f, Prefs.Brightness, v => $"{v * 100:0}%", v => Prefs.Brightness = v);
            Slide(list, "Свечение", Quality.Level >= 1 ? "Ореол света Гу, огней и луны" : "Работает от «Среднего» уровня", 0, 1.5f, Prefs.Bloom, v => v < 0.02f ? "нет" : $"{v * 100:0}%", v => Prefs.Bloom = v);
            Switch(list, "Показывать FPS", "Кадры в секунду в углу экрана", Prefs.ShowFps, on => Prefs.ShowFps = on);
        }

        void SoundTab(RectTransform list)
        {
            Volume(list, "Общая громкость", "Всё сразу · отпустите — прозвучит проба", Sound.AudioDirector.Master,
                v => Sound.AudioDirector.SetVolumes(v, Sound.AudioDirector.Effects, Sound.AudioDirector.MusicVolume, Sound.AudioDirector.Beds), "palm");
            Slide(list, "Музыка", "Деревня, бой, меню", 0, 1, Sound.AudioDirector.MusicVolume, v => $"{v * 100:0}%",
                v => Sound.AudioDirector.SetVolumes(Sound.AudioDirector.Master, Sound.AudioDirector.Effects, v, Sound.AudioDirector.Beds));
            Volume(list, "Удары и Гу", "Бой, приёмы, интерфейс · отпустите — прозвучит проба", Sound.AudioDirector.Effects,
                v => Sound.AudioDirector.SetVolumes(Sound.AudioDirector.Master, v, Sound.AudioDirector.MusicVolume, Sound.AudioDirector.Beds), "cast_moon");
            Slide(list, "Окружение", "Ветер, вода, лес, рынок", 0, 1, Sound.AudioDirector.Beds, v => $"{v * 100:0}%",
                v => Sound.AudioDirector.SetVolumes(Sound.AudioDirector.Master, Sound.AudioDirector.Effects, Sound.AudioDirector.MusicVolume, v));
            Choice(list, "Звук в фоне", "Когда окно игры не впереди", new[] { "Как есть", "Тише", "Нет" }, Prefs.Background, i => Prefs.Background = i);
            Switch(list, "Моно", "Звук одинаков в обоих ушах: ничего не слышно только с одной стороны", Prefs.Mono, on => Prefs.Mono = on);
            Switch(list, "Звуки интерфейса", "Наведение и выбор в меню и на страницах", Prefs.UiSounds, on => Prefs.UiSounds = on);
            var ui = Slide(list, "Громкость интерфейса", "Наведение и щелчки · отпустите — прозвучит проба", 0, 1, Prefs.UiVolume, v => $"{v * 100:0}%", v => Prefs.UiVolume = v);
            ui.gameObject.AddComponent<OnRelease>().Released = UiSound.Click;
        }

        void InterfaceTab(RectTransform list)
        {
            Slide(list, "Размер HUD", "Полосы, слоты, задание", 0.8f, 1.3f, Prefs.HudScale, v => $"{v * 100:0}%", v => Prefs.HudScale = Mathf.Round(v * 20) / 20);
            Slide(list, "Прозрачность HUD", "Насколько он плотный", 0.4f, 1, Prefs.HudOpacity, v => $"{v * 100:0}%", v => Prefs.HudOpacity = v);
            Switch(list, "Компас", "Полоса сторон света вверху экрана: задание, фонари, деревни, набеги", Prefs.Compass, on => Prefs.Compass = on);
            Switch(list, "Миникарта", "Круг местности в углу: враги рядом, фонари, задание, набеги", Prefs.Minimap, on => Prefs.Minimap = on);
            Switch(list, "Часы и погода", "Над миникартой: знак неба, час, погода, день", Prefs.Clock, on => Prefs.Clock = on);
        }

        void AccessTab(RectTransform list)
        {
            Switch(list, "Без вспышек", "Красная вспышка раны тише", Prefs.NoFlashes, on => Prefs.NoFlashes = on);
            Slide(list, "Тряска камеры", "От ударов и взрывов", 0, 1, Prefs.CameraShake, v => $"{v * 100:0}%", v => Prefs.CameraShake = v);
            Slide(list, "Покачивание головы", "Если от вида глазами героя укачивает — уберите до нуля", 0, 1, Prefs.HeadBob, v => $"{v * 100:0}%", v => Prefs.HeadBob = v);
            Switch(list, "Цвета для дальтоников", "Добро — голубое, вред — оранжевый; значки эффектов различаются ещё и формой рамки", Prefs.ColourBlind, on => Prefs.ColourBlind = on);
            Switch(list, "Крупный текст", "Реплики, описания на страницах, строки событий и подсказка на HUD крупнее (HUD — с новой загрузки)", Prefs.LargeText, on => { Prefs.LargeText = on; Redraw(); });
        }

        // ------------------------------------------------------------------ The rows

        static void Section(RectTransform list, string name)
        {
            Gap(list, 10);
            UiKit.Lbl(name, list, 11, UiKit.LabelGold).Size(22);
        }

        /// <summary>A setting's row: its name and a hint at the left; returns the right half, where its control goes.</summary>
        static RectTransform Row(RectTransform list, string name, string hint)
        {
            var row = UiKit.Rect(name, list);
            row.Size(64);
            UiKit.Image("Line", row, new Color(234 / 255f, 240 / 255f, 242 / 255f, 0.06f), UiKit.Plain).rectTransform.Place(Vector2.zero, new Vector2(1, 0), 0, 0, 0, -1);
            var words = UiKit.Rect("Words", row);
            words.Place(Vector2.zero, new Vector2(0.48f, 1), 14, 6, 0, 6);
            var title = UiKit.Label(name, words, 17, UiKit.Text, hint != null ? TextAnchor.UpperLeft : TextAnchor.MiddleLeft);
            title.rectTransform.Place(Vector2.zero, Vector2.one, 0, hint != null ? 22 : 0, 0, hint != null ? 4 : 0);

            if (hint != null)
            {
                var small = UiKit.Label(hint, words, 12, UiKit.Muted, TextAnchor.UpperLeft);
                small.horizontalOverflow = HorizontalWrapMode.Wrap;
                small.rectTransform.Place(Vector2.zero, Vector2.one, 0, 0, 0, 30);
            }

            var control = UiKit.Rect("Control", row);
            control.Place(new Vector2(0.5f, 0), Vector2.one, 0, 8, 10, 8);
            var line = control.Stack<HorizontalLayoutGroup>(8, null, TextAnchor.MiddleRight);
            line.childForceExpandWidth = false;

            return control;
        }

        static void Fixed(RectTransform list, string name, string value, string hint)
        {
            var row = Row(list, name, hint);
            UiKit.Label(value, row, 15, UiKit.GoldText, TextAnchor.MiddleRight).Size(48, -1, 1);
        }

        static void Switch(RectTransform list, string name, string hint, bool on, Action<bool> change)
        {
            var row = Row(list, name, hint);
            UiKit.Toggle(row, on, value => { change(value); Game.Ui.Redraw(); });
        }

        static void Choice(RectTransform list, string name, string hint, IList<string> options, int chosen, Action<int> pick)
        {
            var row = Row(list, name, hint);
            var holder = UiKit.Rect("Choices", row);
            holder.Size(48, Mathf.Min(460, 96 * options.Count), 0);
            UiKit.Choices(holder, options, chosen, i => { pick(i); Game.Ui.Redraw(); }, 14);
        }

        /// <summary>A slider (its number beside it changes as it moves; the page is not built again).</summary>
        static Slider Slide(RectTransform list, string name, string hint, float min, float max, float value, Func<float, string> show, Action<float> change)
        {
            var row = Row(list, name, hint);
            var holder = UiKit.Rect("Slider", row);
            holder.Size(48, 360, 0);

            return UiKit.Slider(holder, min, max, Mathf.Clamp(value, min, max), show, change);
        }

        /// <summary>A volume slider: when it is let go, a sound at the new loudness (ROADMAP 2.7.4).</summary>
        static void Volume(RectTransform list, string name, string hint, float value, Action<float> change, string sample)
        {
            var slider = Slide(list, name, hint, 0, 1, value, v => $"{v * 100:0}%", change);
            slider.gameObject.AddComponent<OnRelease>().Released = () => Sound.AudioDirector.Play(sample, null, 0.8f, 0);
        }

        /// <summary>A choice turned through with ‹ and ›: the value in gold between them.</summary>
        static void Turn(RectTransform list, string name, string hint, List<string> options, int at, Func<string, string> show, Action<int> pick)
        {
            var row = Row(list, name, hint);
            var count = options.Count;
            UiKit.HexButton("‹", row, () => { pick((at - 1 + count) % count); Game.Ui.Redraw(); }, UiKit.HexKind.Ghost, 14, 40).Size(40, 56, 0);
            UiKit.Label(show(options[at]), row, 16, UiKit.GoldText, TextAnchor.MiddleCenter).Size(40, 220, 0);
            UiKit.HexButton("›", row, () => { pick((at + 1) % count); Game.Ui.Redraw(); }, UiKit.HexKind.Ghost, 14, 40).Size(40, 56, 0);
        }
    }

    /// <summary>Calls back when the pointer lets go of the thing (a slider let go).</summary>
    public class OnRelease : MonoBehaviour, UnityEngine.EventSystems.IPointerUpHandler
    {
        public Action Released;

        public void OnPointerUp(UnityEngine.EventSystems.PointerEventData data) => Released?.Invoke();
    }

    /// <summary>Waits for the next input — a key, a mouse button, a wheel notch — and hands it on once (Esc too: the page cancels on it).</summary>
    public class KeyCatcher : MonoBehaviour
    {
        public Action<int> Caught;
        int born;

        void Awake() => born = Time.frameCount;

        void Update()
        {
            if (Time.frameCount == born || Caught == null)
            {
                return;
            }

            var input = Bindings.PressedInput();

            if (input != 0)
            {
                var caught = Caught;
                Caught = null;
                caught(input);
            }
        }
    }
}
