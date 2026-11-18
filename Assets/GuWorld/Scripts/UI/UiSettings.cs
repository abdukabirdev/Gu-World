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
            (SettingsTab.Controls, "Управление", "Мышь, камера и клавиши. Нажмите на действие, потом новую клавишу; клавиша, занятая другим действием, переходит к нему в обмен. ЛКМ, ПКМ, колесо и WASD пока не меняются."),
            (SettingsTab.Picture, "Графика", "Пять уровней качества — или свои числа. Масштаб, сглаживание, тени и яркость меняются сразу; лес, трава и люди («детализация мира») — когда мир строится заново."),
            (SettingsTab.Sound, "Звук", "Громкость по слоям: музыка, удары и Гу, окружение. И что делать со звуком, когда окно игры не впереди."),
            (SettingsTab.Interface, "Интерфейс", "Размер и прозрачность HUD, компас и миникарта, крупный текст в разговорах."),
            (SettingsTab.Access, "Доступность", "Меньше вспышек и тряски. Цвета для дальтоников — следующим шагом."),
        };

        SettingsTab settingsTab = SettingsTab.Game;
        Page settingsBack = Page.Title;
        string rebinding;
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
            UiKit.Footer(stage, new[] { ("Q E", "Раздел"), ("Esc", "Назад") });
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
            UiKit.HexButton("Вернуть как было", aside, ResetTab, UiKit.HexKind.Ghost, 12, 44).Size(44, 260, 0);

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
            Choice(list, "Автосохранение", "Игра сохраняется сама, при паузе и при выходе", new[] { "30 с", "2 мин", "5 мин" },
                Math.Max(0, Array.IndexOf(Prefs.AutosaveChoices, Prefs.Autosave)), i => Prefs.Autosave = Prefs.AutosaveChoices[i]);

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
            Slide(list, "Тряска камеры", "От ударов и взрывов", 0, 1, Prefs.CameraShake, v => $"{v * 100:0}%", v => Prefs.CameraShake = v);

            Section(list, "Клавиши");

            foreach (var action in Bindings.All)
            {
                var id = action.Id;
                var waiting = rebinding == id;
                var row = Row(list, action.Name, action.Key != action.Default ? $"изменено · было {Bindings.Label(action.Default)}" : null);
                var key = UiKit.HexButton(waiting ? "…нажмите" : Bindings.Label(action.Key), row, () => { rebinding = id; Redraw(); },
                    waiting ? UiKit.HexKind.Solid : UiKit.HexKind.Ghost, 12, 40);
                key.Size(40, 200, 0);
            }

            Para("ЛКМ — удар, ПКМ — перекат, колесо — дистанция и цель, WASD — идти: эти пока не меняются. Esc отменяет ожидание клавиши.", list, 13, UiKit.Muted);
        }

        void KeyCaught(Key key)
        {
            if (rebinding != null && key != Key.Escape)
            {
                Bindings.Bind(rebinding, key);
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
            Slide(list, "Деревья", "Сколько деревьев плана сажать", 0.3f, 1, now.Trees, v => $"{v * 100:0}%",
                v => { var n = Quality.Now; n.Trees = Mathf.Round(v * 20) / 20; Quality.Change(n); });
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
            Switch(list, "Показывать FPS", "Кадры в секунду в углу экрана", Prefs.ShowFps, on => Prefs.ShowFps = on);
        }

        void SoundTab(RectTransform list)
        {
            Slide(list, "Общая громкость", "Всё сразу", 0, 1, Sound.AudioDirector.Master, v => $"{v * 100:0}%",
                v => Sound.AudioDirector.SetVolumes(v, Sound.AudioDirector.Effects, Sound.AudioDirector.MusicVolume, Sound.AudioDirector.Beds));
            Slide(list, "Музыка", "Деревня, бой, меню", 0, 1, Sound.AudioDirector.MusicVolume, v => $"{v * 100:0}%",
                v => Sound.AudioDirector.SetVolumes(Sound.AudioDirector.Master, Sound.AudioDirector.Effects, v, Sound.AudioDirector.Beds));
            Slide(list, "Удары и Гу", "Бой, приёмы, интерфейс", 0, 1, Sound.AudioDirector.Effects, v => $"{v * 100:0}%",
                v => Sound.AudioDirector.SetVolumes(Sound.AudioDirector.Master, v, Sound.AudioDirector.MusicVolume, Sound.AudioDirector.Beds));
            Slide(list, "Окружение", "Ветер, вода, лес, рынок", 0, 1, Sound.AudioDirector.Beds, v => $"{v * 100:0}%",
                v => Sound.AudioDirector.SetVolumes(Sound.AudioDirector.Master, Sound.AudioDirector.Effects, Sound.AudioDirector.MusicVolume, v));
            Choice(list, "Звук в фоне", "Когда окно игры не впереди", new[] { "Как есть", "Тише", "Нет" }, Prefs.Background, i => Prefs.Background = i);
        }

        void InterfaceTab(RectTransform list)
        {
            Slide(list, "Размер HUD", "Полосы, слоты, задание", 0.8f, 1.3f, Prefs.HudScale, v => $"{v * 100:0}%", v => Prefs.HudScale = Mathf.Round(v * 20) / 20);
            Slide(list, "Прозрачность HUD", "Насколько он плотный", 0.4f, 1, Prefs.HudOpacity, v => $"{v * 100:0}%", v => Prefs.HudOpacity = v);
            Switch(list, "Компас", "Полоса сторон света вверху экрана · появится в следующем шаге", Prefs.Compass, on => Prefs.Compass = on);
            Switch(list, "Миникарта", "Круг местности в углу · появится в следующем шаге", Prefs.Minimap, on => Prefs.Minimap = on);
            Switch(list, "Крупный текст в разговорах", "Реплики и ответы крупнее", Prefs.LargeText, on => Prefs.LargeText = on);
        }

        void AccessTab(RectTransform list)
        {
            Switch(list, "Без вспышек", "Красная вспышка раны тише", Prefs.NoFlashes, on => Prefs.NoFlashes = on);
            Slide(list, "Тряска камеры", "От ударов и взрывов", 0, 1, Prefs.CameraShake, v => $"{v * 100:0}%", v => Prefs.CameraShake = v);
            Para("Цвета для дальтоников (статусы, сродство, ранги) — в плане, раздел 2.7.6.", list, 13, UiKit.Muted);
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
        static void Slide(RectTransform list, string name, string hint, float min, float max, float value, Func<float, string> show, Action<float> change)
        {
            var row = Row(list, name, hint);
            var holder = UiKit.Rect("Slider", row);
            holder.Size(48, 360, 0);
            UiKit.Slider(holder, min, max, Mathf.Clamp(value, min, max), show, change);
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

    /// <summary>Waits for the next key pressed (a key being bound) and hands it on; Esc hands on Escape (cancel).</summary>
    public class KeyCatcher : MonoBehaviour
    {
        public Action<Key> Caught;
        int born;

        void Awake() => born = Time.frameCount;

        void Update()
        {
            if (Time.frameCount == born || Caught == null)
            {
                return;
            }

            var key = Bindings.Pressed();

            if (key != Key.None)
            {
                var caught = Caught;
                Caught = null;
                caught(key);
            }
        }
    }
}
