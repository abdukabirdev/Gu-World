using System;
using System.Collections.Generic;
using GuWorld.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GuWorld.UI
{
    /// <summary>
    /// A new game in two steps (ROADMAP 2.3–2.5, from main's UI-Path.dc.html and UI-Creator.dc.html).
    /// Step 1, the path: five cards side by side, each a difficulty that is also the talent one is born
    /// with — its sea drawn full to the talent's share, what it does to the mountain, how hard it is in
    /// dots; the chosen card stands taller in gold; the player's own rules under them. Step 2, the hero:
    /// the name after the clan's, the path chosen, what lies ahead, Begin.
    /// </summary>
    public partial class Ui
    {
        Difficulty newPath = Beginnings.Default;
        string newName = "";
        Core.Settings newRules = new Core.Settings();

        /// <summary>The paths' colours, easiest first: jade, essence, gold, amber, coral.</summary>
        static readonly Color[] PathColours =
        {
            UiKit.Hex(0x9FEFC4), UiKit.Hex(0x7FE0C8), UiKit.Hex(0xF0C868), UiKit.Hex(0xF0A070), UiKit.Hex(0xF08A74),
        };

        /// <summary>The main menu's "New game": the choices start over at the path page.</summary>
        void StartNewGame()
        {
            newPath = Beginnings.Default;
            newName = "";
            newRules = new Core.Settings();
            Show(Page.NewPath);
        }

        // ------------------------------------------------------------------ Step 1: the path

        void NewPathPage()
        {
            var stage = UiKit.Stage(pageRoot);
            UiKit.Backdrop(stage, "道", 560);
            NewGameHeader(stage, 1, "Выбор пути",
                "Путь — это трудность игры и талант, с которым вы родились. Талант решает, сколько эссенции вмещает апертура: как часто " +
                "можно звать Гу и каких Гу удастся очистить. Сюжет дважды поднимет талант — на любом пути.");

            // The cards, their feet on one line; the chosen one taller.
            var cards = UiKit.Rect("Cards", stage);
            cards.Place(new Vector2(0, 1), new Vector2(1, 1), 56, -690, 56, 180);
            var row = cards.Stack<HorizontalLayoutGroup>(16, null, TextAnchor.LowerCenter);
            row.childControlHeight = true;
            row.childForceExpandHeight = false;

            for (var i = 0; i < Beginnings.All.Length; i++)
            {
                PathCard(cards, i, Beginnings.All[i].Path == newPath);
            }

            // The player's own rules.
            var rules = UiKit.Rect("Rules", stage);
            rules.Place(new Vector2(0, 1), new Vector2(1, 1), 56, -782, 56, 704);
            UiKit.Lbl("Свои правила · их можно сменить и потом, в паузе (путь — нельзя)", rules, 11, UiKit.LabelGold).rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), 0, -16, 0, 0);
            var line = UiKit.Rect("Line", rules);
            line.Place(Vector2.zero, new Vector2(1, 1), 0, 0, 0, 24);
            var across = line.Stack<HorizontalLayoutGroup>(28, null, TextAnchor.MiddleLeft);
            across.childForceExpandWidth = false;

            var hunger = UiKit.Rect("Hunger", line);
            hunger.Size(52, 520, 0);
            var hungerRow = hunger.Stack<HorizontalLayoutGroup>(12, null, TextAnchor.MiddleLeft);
            hungerRow.childForceExpandWidth = false;
            UiKit.Label("Голод Гу", hunger, 15, UiKit.Text).Size(52, 90, 0);
            var hungerChoices = UiKit.Rect("Choices", hunger);
            hungerChoices.Size(52, 400, 0);
            UiKit.Choices(hungerChoices, new[] { "Нет", "Мягкий", "Как в каноне" }, (int)newRules.Hunger, i => { newRules.Hunger = (HungerRule)i; Redraw(); }, 15);

            RuleToggle(line, "Гу сами едят из сумки", newRules.AutoFeed, on => newRules.AutoFeed = on);
            RuleToggle(line, "Переходы между фонарями стоят камней", newRules.TravelCosts, on => newRules.TravelCosts = on);

            // Back, what is chosen, on.
            var chosen = Beginnings.Of(newPath);
            var foot = UiKit.Rect("Choice", stage);
            foot.Place(new Vector2(0, 0), new Vector2(1, 0), 56, 62, 56, -114);
            var buttons = foot.Stack<HorizontalLayoutGroup>(20, null, TextAnchor.MiddleLeft);
            buttons.childForceExpandWidth = false;
            UiKit.HexButton("Назад", foot, () => Show(Page.Title), UiKit.HexKind.Ghost, 13, 52, "Esc").Size(52, 190, 0);
            UiKit.Label($"Выбрано: <color=#FFF1CF>{chosen.Name}</color> · талант {chosen.Grade} · {chosen.Talent * 100:0}%", foot, 15, UiKit.Muted).Size(52, -1, 1);
            UiKit.HexButton("К герою", foot, () => Show(Page.NewHero), UiKit.HexKind.Solid, 14, 52, "Enter").Size(52, 300, 0);

            UiKit.Footer(stage, new[] { ("← →", "Путь"), ("Enter", "Дальше"), ("Esc", "Назад") });

            var keys = stage.gameObject.AddComponent<PageKeys>();
            keys.On(Key.LeftArrow, () => PickPath(-1));
            keys.On(Key.A, () => PickPath(-1));
            keys.On(Key.RightArrow, () => PickPath(1));
            keys.On(Key.D, () => PickPath(1));
            keys.On(Key.Enter, () => Show(Page.NewHero));
            keys.On(Key.NumpadEnter, () => Show(Page.NewHero));
            keys.On(Key.Escape, () => Show(Page.Title));
        }

        void PickPath(int step)
        {
            var at = DifficultyRules.Order(newPath);
            var next = Mathf.Clamp(at + step, 0, DifficultyRules.All.Length - 1);

            if (next != at)
            {
                newPath = DifficultyRules.All[next];
                Sound.AudioDirector.Play("hover", null, 0.35f);
                Redraw();
            }
        }

        /// <summary>The top of both steps: which step, the title, a line under it; two marks at the right for the two steps.</summary>
        static void NewGameHeader(RectTransform stage, int step, string title, string intro)
        {
            var header = UiKit.Rect("Header", stage);
            header.Place(new Vector2(0, 1), new Vector2(1, 1), 56, -168, 56, 40);
            UiKit.Lbl(step > 0 ? $"Новая игра · шаг {step} из 2" : "GU World", header, 11, UiKit.LabelGold).rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), 0, -16, 0, 0);
            var name = UiKit.Disp(title.ToUpperInvariant(), header, 40, UiKit.Text);
            name.Track(0.14f);
            name.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), 0, -70, 0, 22);
            var words = UiKit.Label(intro, header, 15, UiKit.Hex(0xA8B6B8), TextAnchor.UpperLeft);
            words.horizontalOverflow = HorizontalWrapMode.Wrap;
            words.lineSpacing = 1.15f;
            words.rectTransform.Place(new Vector2(0, 0), new Vector2(0, 1), 0, 0, -1150, 78);

            for (var i = 0; i < (step > 0 ? 2 : 0); i++)
            {
                var lit = i < step;
                var bar = UiKit.Image("Step", header, lit ? UiKit.GoldLit : new Color(234 / 255f, 240 / 255f, 242 / 255f, 0.15f), UiKit.Plain);
                bar.rectTransform.At(new Vector2(1, 1), new Vector2(-14 - (1 - i) * 40, -8), new Vector2(30, 2));

                if (lit)
                {
                    UiKit.Glow(bar.transform, UiKit.GoldLit.A(0.5f), 8);
                }
            }

            UiKit.Image("Line", header, UiKit.Gold.A(0.28f), UiKit.FadeBoth).rectTransform.Place(Vector2.zero, new Vector2(1, 0), 0, -1, 0, 0);
        }

        static void RuleToggle(RectTransform parent, string name, bool on, Action<bool> change)
        {
            var holder = UiKit.Rect(name, parent);
            holder.Size(52, 330, 0);
            var row = holder.Stack<HorizontalLayoutGroup>(12, null, TextAnchor.MiddleLeft);
            row.childForceExpandWidth = false;
            row.childControlWidth = false;
            row.childControlHeight = false;
            var toggle = UiKit.Toggle(holder, on, change);
            toggle.sizeDelta = new Vector2(44, 24);
            var label = UiKit.Label(name, holder, 15, UiKit.Text);
            label.rectTransform.sizeDelta = new Vector2(270, 52);
        }

        /// <summary>A path's card (main's PathCard, with the mountain's numbers v2 knows).</summary>
        void PathCard(Transform parent, int index, bool on)
        {
            var way = Beginnings.All[index];
            var colour = PathColours[Mathf.Min(index, PathColours.Length - 1)];
            var card = UiKit.Image(way.Name, parent, Color.white, UiKit.Plain);
            card.raycastTarget = true;
            card.Size(on ? 510 : 474, -1, 1);

            if (on)
            {
                card.Gradient(new Color(40 / 255f, 34 / 255f, 24 / 255f, 0.86f), new Color(10 / 255f, 10 / 255f, 14 / 255f, 0.94f));
                UiKit.Glow(card.transform, UiKit.Gold.A(0.12f), 40);
                UiKit.Frame(card.transform, UiKit.GoldLit.A(0.6f));
                UiKit.GoldCorners(card.transform, 16);
            }
            else
            {
                card.Gradient(new Color(12 / 255f, 16 / 255f, 24 / 255f, 0.72f), new Color(5 / 255f, 7 / 255f, 12 / 255f, 0.84f));
                UiKit.Frame(card.transform, new Color(200 / 255f, 230 / 255f, 240 / 255f, 0.1f));
            }

            var click = card.gameObject.AddComponent<Clickable>();
            click.Click = () =>
            {
                if (on)
                {
                    Show(Page.NewHero);
                }
                else
                {
                    newPath = way.Path;
                    Redraw();
                }
            };

            // The grade, huge and faint, cut by the card's edge.
            var clip = UiKit.Rect("Clip", card.transform).Fill();
            clip.gameObject.AddComponent<RectMask2D>();
            var ghost = UiKit.Label(way.Grade, clip, 190, on ? UiKit.Gold.A(0.12f) : new Color(234 / 255f, 240 / 255f, 242 / 255f, 0.04f), TextAnchor.UpperRight);
            ghost.font = UiKit.Display;
            ghost.rectTransform.TopRight(-6, -28, 220, 220);

            var column = UiKit.Rect("Column", card.transform).Fill();
            column.Stack<VerticalLayoutGroup>(0, new RectOffset(20, 20, 22, 20));
            UiKit.Lbl($"Талант {way.Grade}", column, 11, colour, 0.3f).Size(14);
            Gap(column, 6);
            UiKit.Disp(way.Name, column, 30, on ? Color.white : UiKit.Text2).Size(34);
            Gap(column, 12);
            var seaRow = UiKit.Rect("Sea", column);
            seaRow.Size(120);
            Sea(seaRow, way.Talent, colour, on);
            Gap(column, 12);
            UiKit.Image("Rule", column, new Color(234 / 255f, 240 / 255f, 242 / 255f, 0.1f), UiKit.Plain).Size(1);
            Gap(column, 6);
            Pair(column, "Урон по вам", Times(DifficultyRules.DamageTaken(way.Path)));
            Pair(column, "Здоровье врагов", Times(DifficultyRules.EnemyHealth(way.Path)));
            Pair(column, "Смерть забирает", $"{DifficultyRules.DeathLoss(way.Path) * 100:0}% камней");

            if (Math.Abs(DifficultyRules.Insight(way.Path) - 1) > 0.001f)
            {
                Pair(column, "Прорыв", Times(DifficultyRules.Insight(way.Path)));
            }

            if (Math.Abs(DifficultyRules.Prices(way.Path) - 1) > 0.001f)
            {
                Pair(column, "Цены", Times(DifficultyRules.Prices(way.Path)));
            }

            var hard = Pair(column, "Трудность", null);
            var pips = UiKit.Rect("Pips", hard);
            pips.Place(new Vector2(1, 0), new Vector2(1, 1), -80, 0, 0, 0);

            for (var k = 0; k < 5; k++)
            {
                var pip = UiKit.Image("Pip", pips, k < way.Dots ? colour : new Color(234 / 255f, 240 / 255f, 242 / 255f, 0.14f), UiKit.Diamond);
                pip.rectTransform.At(new Vector2(1, 0.5f), new Vector2(-5 - (4 - k) * 14, 0), new Vector2(10, 10));
            }

            Gap(column, 10);
            var about = UiKit.Label(way.About, column, 13, UiKit.Hex(0xA8B6B8), TextAnchor.UpperLeft);
            about.horizontalOverflow = HorizontalWrapMode.Wrap;
            about.lineSpacing = 1.1f;
            about.Size(-1).flexibleHeight = 1;
        }

        static string Times(float value) => Math.Abs(value - 1) < 0.001f ? "как задумано" : $"×{value:0.##}";

        /// <summary>A line of a card: a quiet name at the left, a value at the right.</summary>
        static RectTransform Pair(Transform parent, string name, string value)
        {
            var row = UiKit.Rect(name, parent);
            row.Size(19);
            UiKit.Label(name, row, 12, UiKit.Muted).rectTransform.Fill();

            if (value != null)
            {
                UiKit.Label(value, row, 12, UiKit.Text2, TextAnchor.MiddleRight).rectTransform.Fill();
            }

            return row;
        }

        /// <summary>
        /// A sea of essence in a ring, 130 across: dark water filled from the foot to the talent's
        /// share, a wave running on its top, the path's ring and a slow dashed gold one; the share over it.
        /// </summary>
        static void Sea(RectTransform holder, float talent, Color colour, bool on)
        {
            const float inner = 100;
            var box = UiKit.Rect("Ring", holder);
            box.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(130, 130));
            var deep = UiKit.Image("Deep", box, new Color(2 / 255f, 6 / 255f, 10 / 255f, 0.9f), UiKit.Disc);
            deep.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(inner, inner));
            var share = Mathf.Clamp01(talent);
            var water = UiKit.Image("Water", box, new Color(127 / 255f, 224 / 255f, 200 / 255f, on ? 0.75f : 0.45f), UiKit.Disc);
            water.type = Image.Type.Filled;
            water.fillMethod = Image.FillMethod.Vertical;
            water.fillOrigin = (int)Image.OriginVertical.Bottom;
            water.fillAmount = share;
            water.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(inner, inner));

            // The wave: a line as wide as the water's top.
            var level = -inner * 0.5f + inner * share;
            var half = Mathf.Sqrt(Mathf.Max(0, inner * inner * 0.25f - level * level));

            if (half > 4)
            {
                var wave = UiKit.Rect("Wave", box).gameObject.AddComponent<RawImage>();
                wave.texture = WaveTexture();
                wave.color = new Color(230 / 255f, 1, 248 / 255f, 0.6f);
                wave.raycastTarget = false;
                wave.rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(0, level), new Vector2(half * 2, 8));
                var run = wave.gameObject.AddComponent<WaveRun>();
                run.Period = 4;
                run.Across = half * 2 / 60f;
            }

            UiKit.Orbit(box, 110, colour.A(0.7f), 0, 0);
            UiKit.Orbit(box, 126, UiKit.Gold.A(0.2f), 90, 6);
            var number = UiKit.Disp($"{talent * 100:0}%", box, 36, Color.white, TextAnchor.MiddleCenter);
            number.Shadowed();
            number.rectTransform.Place(Vector2.zero, Vector2.one, 0, 10, 0, 0);
            var word = UiKit.Lbl("Море", box, 10, UiKit.Hex(0xCFE8E2), 0.24f, TextAnchor.MiddleCenter);
            word.rectTransform.Place(Vector2.zero, Vector2.one, 0, 0, 0, 0);
            word.rectTransform.anchoredPosition = new Vector2(0, -20);
        }

        static Texture2D wave;

        /// <summary>A wave's line, one period of 60 across, to repeat.</summary>
        static Texture2D WaveTexture()
        {
            if (wave != null)
            {
                return wave;
            }

            const int w = 120;
            const int h = 16;
            wave = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, hideFlags = HideFlags.DontSave };
            var pixels = new Color[w * h];

            for (var x = 0; x < w; x++)
            {
                // Two humps of 30 each, 3 high.
                var t = x / (float)w * 2 % 1;
                var y = h * 0.5f + 3 * (1 - (2 * t - 1) * (2 * t - 1)) * (x < w / 2 ? 1 : -1);

                for (var py = 0; py < h; py++)
                {
                    pixels[py * w + x] = new Color(1, 1, 1, Mathf.Clamp01(1.4f - Mathf.Abs(py + 0.5f - y)));
                }
            }

            wave.SetPixels(pixels);
            wave.Apply();

            return wave;
        }

        // ------------------------------------------------------------------ Step 2: the hero

        void NewHeroPage()
        {
            var stage = UiKit.Stage(pageRoot);
            UiKit.Backdrop(stage, "人", 560);
            NewGameHeader(stage, 2, "Герой",
                "Имя после имени клана и цена смерти. Внешность — пол, рост, кожа, причёска и цвет ханьфу — появится здесь вместе со второй моделью и перекрасками.");
            var way = Beginnings.Of(newPath);
            var colour = PathColours[DifficultyRules.Order(newPath)];

            // The name.
            var left = UiKit.Rect("Left", stage);
            left.Place(new Vector2(0, 0), new Vector2(0.5f, 1), 56, 150, 24, 200);
            left.Stack<VerticalLayoutGroup>(10);
            UiKit.Lbl("Имя", left, 11, UiKit.LabelGold).Size(16);
            var nameRow = UiKit.Rect("Name", left);
            nameRow.Size(56);
            var across = nameRow.Stack<HorizontalLayoutGroup>(14, null, TextAnchor.MiddleLeft);
            across.childForceExpandWidth = false;
            UiKit.Disp("Гу Юэ", nameRow, 26, UiKit.Gold).Size(56, 100, 0);
            var fieldHolder = UiKit.Rect("Field", nameRow);
            fieldHolder.Size(56, 300, 0);
            var field = UiKit.Field(fieldHolder, newName, Look.DefaultName, Look.NameLength, text => newName = text);
            ((RectTransform)field.transform).Fill();
            UiKit.HexButton("Случайно", nameRow, () => { newName = Look.RandomName(new System.Random()); Redraw(); }, UiKit.HexKind.Ghost, 12, 44).Size(44, 150, 0);
            Para($"Своё имя — после имени клана, до {Look.NameLength} знаков. Пусто — «{Look.DefaultName}».", left, 13, UiKit.Muted);

            // What a fall costs: chosen once, for the whole playthrough.
            Gap(left, 14);
            UiKit.Lbl("Цена смерти · на всё прохождение", left, 11, UiKit.LabelGold).Size(16);
            var deathRow = UiKit.Rect("Death", left);
            deathRow.Size(48);
            var deaths = new List<string>();

            foreach (var rule in DeathRules.All)
            {
                deaths.Add(DeathRules.Name(rule));
            }

            UiKit.Choices(deathRow, deaths, Array.IndexOf(DeathRules.All, newRules.Death), i => { newRules.Death = DeathRules.All[i]; Redraw(); }, 15);
            Para(DeathRules.Text(newRules.Death), left, 14, newRules.Death == DeathRule.Hardcore ? UiKit.Danger : UiKit.Text2);

            Gap(left, 14);
            UiKit.Lbl("Что ждёт", left, 11, UiKit.LabelGold).Size(16);
            Para("Вам пятнадцать. Завтра — обряд пробуждения апертуры в академии клана Гу Юэ. Камень предков покажет, сколько света в вашей чаше.", left, 16, UiKit.Text2);

            var save = Game.ReadSave();

            if (save != null)
            {
                Gap(left, 18);
                UiKit.Lbl("Ячейка занята", left, 11, UiKit.Danger).Size(16);
                Para($"В ячейке {Game.Slot + 1} — Гу Юэ {save.HeroName}, {Cultivation.Name(save.Step)}, {Mathf.RoundToInt(save.PlayedSeconds / 60)} мин. " +
                     "Новый путь заменит это сохранение.", left, 14, UiKit.Text2);
            }

            // The path chosen.
            var right = UiKit.Image("Path", stage, Color.white, UiKit.Plain);
            right.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(1, 1), 24, 150, 56, 200);
            right.Gradient(new Color(12 / 255f, 16 / 255f, 24 / 255f, 0.72f), new Color(5 / 255f, 7 / 255f, 12 / 255f, 0.84f));
            UiKit.Frame(right.transform, new Color(200 / 255f, 230 / 255f, 240 / 255f, 0.1f));
            var inside = UiKit.Rect("Inside", right.transform).Fill();
            inside.Stack<VerticalLayoutGroup>(8, new RectOffset(28, 28, 24, 24));
            UiKit.Lbl("Ваш путь", inside, 11, UiKit.LabelGold).Size(16);
            UiKit.Disp($"{way.Name} · талант {way.Grade} · {way.Talent * 100:0}%", inside, 28, colour).Size(36);
            Para(way.About, inside, 14, UiKit.Text2);
            Para(DifficultyRules.Text(newPath), inside, 14, UiKit.Muted);
            Gap(inside, 6);
            var hunger = newRules.Hunger == HungerRule.Off ? "нет" : newRules.Hunger == HungerRule.Soft ? "мягкий" : "как в каноне";
            Para($"Смерть: {DeathRules.Name(newRules.Death).ToLowerInvariant()} · голод Гу: {hunger} · Гу едят сами: {(newRules.AutoFeed ? "да" : "нет")} · переходы платные: {(newRules.TravelCosts ? "да" : "нет")}", inside, 13, UiKit.Muted);
            Gap(inside, 4);
            UiKit.HexButton("Изменить путь", inside, () => Show(Page.NewPath), UiKit.HexKind.Ghost, 12, 44).Size(44, 220, 0);

            // Back and Begin.
            var foot = UiKit.Rect("Begin", stage);
            foot.Place(new Vector2(0, 0), new Vector2(1, 0), 56, 62, 56, -114);
            var buttons = foot.Stack<HorizontalLayoutGroup>(20, null, TextAnchor.MiddleRight);
            buttons.childForceExpandWidth = false;
            UiKit.HexButton("Назад", foot, () => Show(Page.NewPath), UiKit.HexKind.Ghost, 13, 52, "Esc").Size(52, 190, 0);
            UiKit.HexButton("Начать путь", foot, BeginNewGame, UiKit.HexKind.Solid, 14, 52, "Enter").Size(52, 300, 0);

            UiKit.Footer(stage, new[] { ("Enter", "Начать"), ("Esc", "Назад") });

            var keys = stage.gameObject.AddComponent<PageKeys>();
            keys.On(Key.Enter, BeginNewGame);
            keys.On(Key.NumpadEnter, BeginNewGame);
            keys.On(Key.Escape, () => Show(Page.NewPath));
        }

        void BeginNewGame() => Game.Instance.BeginNew(newName, newPath, newRules);
    }

    /// <summary>Runs a repeating texture sideways, one tile in `Period` seconds (the sea's wave).</summary>
    [RequireComponent(typeof(RawImage))]
    public class WaveRun : MonoBehaviour
    {
        public float Period = 4;
        /// <summary>How many tiles across the rectangle shows.</summary>
        public float Across = 1;

        void Update()
        {
            var image = GetComponent<RawImage>();
            image.uvRect = new Rect(Time.unscaledTime / Period % 1, 0, Across, 1);
        }
    }
}
