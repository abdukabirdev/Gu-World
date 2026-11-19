using System;
using System.Collections.Generic;
using GuWorld.Core;
using GuWorld.Core.Gen.People;
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
    /// the name after the clan's and the look (sex, height, skin, hair, the hanfu) at the left, the hero
    /// as a paper cut-out in the middle (until the model itself stands there), the path chosen, the price
    /// of a fall and what lies ahead at the right, Begin.
    /// </summary>
    public partial class Ui
    {
        Difficulty newPath = Beginnings.Default;
        string newName = "";
        Core.Settings newRules = new Core.Settings();
        Appearance newLook = new Appearance();

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
            newLook = new Appearance();
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
                "Имя после имени клана, облик и цена смерти. Облик — на всё прохождение: рост, кожа, причёска и цвет ханьфу; девушка придёт вместе со своей моделью.");
            newLook = Appearances.Clamp(newLook);

            LookPanel(stage);

            // The hero in the middle, where the stage is wide enough for three columns (16:10 and wider).
            if (Screen.height > 0 && Screen.width * 900f / Screen.height >= 1400)
            {
                var middle = UiKit.Rect("Figure", stage);
                middle.At(new Vector2(0.5f, 0), new Vector2(-10, 410), new Vector2(380, 560));
                LookFigure(middle, newLook);
            }

            WayColumn(stage);

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

        /// <summary>The left of the hero's page: the name, then the look, each choice a row under its label (the label tells what is chosen).</summary>
        void LookPanel(RectTransform stage)
        {
            var left = UiKit.Rect("Look", stage);
            left.Place(new Vector2(0, 0), new Vector2(0, 1), 56, 128, -540, 186);
            left.Stack<VerticalLayoutGroup>(6);

            LookLabel(left, "Имя", null);
            var nameRow = UiKit.Rect("Name", left);
            nameRow.Size(48);
            var across = nameRow.Stack<HorizontalLayoutGroup>(12, null, TextAnchor.MiddleLeft);
            across.childForceExpandWidth = false;
            UiKit.Disp("Гу Юэ", nameRow, 24, UiKit.Gold).Size(48, 84, 0);
            var fieldHolder = UiKit.Rect("Field", nameRow);
            fieldHolder.Size(48, 250, 0);
            var field = UiKit.Field(fieldHolder, newName, Look.DefaultName, Look.NameLength, text => newName = text);
            ((RectTransform)field.transform).Fill();
            UiKit.HexButton("Случайно", nameRow, () => { newName = Look.RandomName(new System.Random()); Redraw(); }, UiKit.HexKind.Ghost, 11, 40).Size(40, 128, 0);

            Gap(left, 4);
            LookLabel(left, "Пол", "девушка — когда будет её модель");
            var sexes = UiKit.Rect("Sex", left);
            sexes.Size(36);
            sexes.Stack<HorizontalLayoutGroup>(6, null, TextAnchor.MiddleLeft).childForceExpandWidth = true;

            for (var i = 0; i < Appearances.Sexes.Length; i++)
            {
                var sex = i;
                var ready = Appearances.SexReady(i);
                var button = UiKit.Tb(ready ? Appearances.Sexes[i] : Appearances.Sexes[i] + " · скоро", sexes, newLook.Sex == i, () =>
                {
                    if (ready)
                    {
                        newLook.Sex = sex;
                        Redraw();
                    }
                }, 11);
                button.Size(36, -1, 1);

                if (!ready)
                {
                    button.Words.color = UiKit.Muted;
                    button.enabled = false;
                }
            }

            LookLabel(left, "Рост", $"{Appearances.Centimetres(newLook.Height)} см");
            LookChoices(left, Appearances.StatureNames, newLook.Height, 3, i => newLook.Height = i);

            LookLabel(left, "Кожа", Appearances.Skins[newLook.Skin].Name);
            var tones = new Color[Appearances.Skins.Length];

            for (var i = 0; i < tones.Length; i++)
            {
                tones[i] = UiKit.Hex(Appearances.Skins[i].Swatch);
            }

            Swatches(left, tones, newLook.Skin, i => newLook.Skin = i);

            LookLabel(left, "Причёска", Appearances.Hairs[newLook.Hair]);
            LookChoices(left, Appearances.Hairs, newLook.Hair, 2, i => newLook.Hair = i);

            LookLabel(left, "Ханьфу", Appearances.Robes[newLook.Robe].Name);
            var dyes = new Color[Appearances.Robes.Length];

            for (var i = 0; i < dyes.Length; i++)
            {
                dyes[i] = UiKit.Hex(Appearances.Robes[i].Swatch);
            }

            Swatches(left, dyes, newLook.Robe, i => newLook.Robe = i);

            Gap(left, 4);
            var chance = UiKit.Rect("Chance", left);
            chance.Size(40);
            var chanceRow = chance.Stack<HorizontalLayoutGroup>(12, null, TextAnchor.MiddleLeft);
            chanceRow.childForceExpandWidth = false;
            UiKit.HexButton("Случайный облик", chance, () => { newLook = Appearances.Random(new System.Random(), newLook); Redraw(); }, UiKit.HexKind.Ghost, 11, 40).Size(40, 220, 0);
            UiKit.HexButton("Как в модели", chance, () => { newLook = new Appearance { Sex = newLook.Sex }; Redraw(); }, UiKit.HexKind.Ghost, 11, 40).Size(40, 180, 0);
        }

        /// <summary>A choice's label (gold, spaced), what is chosen at its right.</summary>
        static void LookLabel(Transform parent, string name, string chosen)
        {
            var row = UiKit.Rect(name, parent);
            row.Size(18);
            UiKit.Lbl(name, row, 11, UiKit.LabelGold).rectTransform.Fill();

            if (!string.IsNullOrEmpty(chosen))
            {
                UiKit.Label(chosen, row, 13, UiKit.Text2, TextAnchor.MiddleRight).rectTransform.Fill();
            }
        }

        /// <summary>Filter buttons, `across` in a row, the chosen one gold; a pick redraws the page.</summary>
        void LookChoices(Transform parent, string[] names, int chosen, int across, Action<int> pick)
        {
            for (var i = 0; i < names.Length; i += across)
            {
                var row = UiKit.Rect("Choices", parent);
                row.Size(36);
                row.Stack<HorizontalLayoutGroup>(6, null, TextAnchor.MiddleLeft).childForceExpandWidth = true;

                for (var k = i; k < i + across; k++)
                {
                    if (k >= names.Length)
                    {
                        // Keep the last row's buttons as wide as the others.
                        UiKit.Rect("Empty", row).Size(36, -1, 1);
                        continue;
                    }

                    var index = k;
                    UiKit.Tb(names[k], row, k == chosen, () => { pick(index); Redraw(); }, 11).Size(36, -1, 1);
                }
            }
        }

        /// <summary>Round swatches, 36 across; the chosen one ringed in light; a pick redraws the page.</summary>
        void Swatches(Transform parent, Color[] colours, int chosen, Action<int> pick)
        {
            var row = UiKit.Rect("Swatches", parent);
            row.Size(44);
            var group = row.Stack<HorizontalLayoutGroup>(12, new RectOffset(4, 0, 0, 0), TextAnchor.MiddleLeft);
            group.childForceExpandWidth = false;

            for (var i = 0; i < colours.Length; i++)
            {
                var index = i;
                var holder = UiKit.Rect("Swatch", row);
                holder.Size(44, 40, 0);

                if (i == chosen)
                {
                    UiKit.HaloAt(holder, UiKit.GoldLit.A(0.35f), Vector2.zero, 60);
                    UiKit.Image("Ring", holder, UiKit.GoldLit, UiKit.RingLine(44)).rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(44, 44));
                }
                else
                {
                    UiKit.Image("Ring", holder, new Color(234 / 255f, 240 / 255f, 242 / 255f, 0.2f), UiKit.RingLine(40)).rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(40, 40));
                }

                var dot = UiKit.Image("Colour", holder, colours[i], UiKit.Disc);
                dot.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(34, 34));
                dot.raycastTarget = true;
                dot.gameObject.AddComponent<Clickable>().Click = () => { pick(index); Redraw(); };
            }
        }

        /// <summary>
        /// The hero as a paper cut-out on the pad in the rings: boots, the robe in its dye flaring to the hem
        /// with the sash and the white of the under-robe at the throat, short sleeves with pale cuffs and dark
        /// bracers, the skin's tone, the loose hair and its style; taller or shorter with the height. Stands in
        /// for the model until the model itself is shown here.
        /// </summary>
        void LookFigure(RectTransform box, Appearance look)
        {
            UiKit.Orbit(box, 360, UiKit.Gold.A(0.15f), 140, 0).rectTransform.anchoredPosition = new Vector2(0, 60);
            UiKit.Orbit(box, 290, UiKit.Gold.A(0.12f), 0, 4).rectTransform.anchoredPosition = new Vector2(0, 60);
            var pad = UiKit.Image("Pad", box, UiKit.Essence.A(0.4f), UiKit.Radial);
            pad.rectTransform.At(new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(380, 70));
            var padRing = UiKit.Image("Pad ring", box, new Color(160 / 255f, 240 / 255f, 220 / 255f, 0.35f), UiKit.RingLine(240));
            padRing.rectTransform.At(new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(240, 40));

            if (figure != null)
            {
                Destroy(figure);
            }

            figure = PaintFigure(look);
            var picture = UiKit.Rect("Hero", box).gameObject.AddComponent<RawImage>();
            picture.texture = figure;
            picture.raycastTarget = false;
            picture.rectTransform.At(new Vector2(0.5f, 0), new Vector2(0, 280), new Vector2(300, 520));

            var words = UiKit.Lbl(Appearances.Describe(look), box, 10, UiKit.Hex(0x8A9A9D), 0.12f, TextAnchor.MiddleCenter);
            words.rectTransform.At(new Vector2(0.5f, 0), new Vector2(0, 8), new Vector2(380, 16));
        }

        Texture2D figure;

        /// <summary>The cut-out itself, painted (300 × 520, the feet at 500): flat shapes in the look's colours over a soft glow, so black hair reads on the dark page.</summary>
        static Texture2D PaintFigure(Appearance look)
        {
            var s = 1 + (Appearances.Stature(look.Height) - 1) * 2.2f;
            var across = 0.97f + 0.03f * s;
            var paint = new UiPainter(300, 520, 2);
            var robe = UiKit.Hex(Appearances.Robes[look.Robe].Swatch);
            var shade = new Color(robe.r * 0.78f, robe.g * 0.78f, robe.b * 0.78f);
            var skin = UiKit.Hex(Appearances.Skins[look.Skin].Swatch);
            var hair = UiKit.Hex(HeroHair.HairColour);
            var dark = UiKit.Hex(0x1E2024);
            var white = UiKit.Hex(0xE6E4DE);
            var ribbon = UiKit.Hex(0xE6ECF2);

            // Taller or shorter about the feet.
            Vector2 At(float x, float y) => new Vector2(150 + (x - 150) * across, 500 - (500 - y) * s);

            Vector2[] Shape(bool mirrored, params float[] xy)
            {
                var points = new Vector2[xy.Length / 2];

                for (var i = 0; i < points.Length; i++)
                {
                    points[i] = At(mirrored ? 300 - xy[i * 2] : xy[i * 2], xy[i * 2 + 1]);
                }

                return points;
            }

            void Fill(Color colour, params float[] xy) => paint.Polygon(Shape(false, xy), colour);
            void Both(Color colour, params float[] xy)
            {
                paint.Polygon(Shape(false, xy), colour);
                paint.Polygon(Shape(true, xy), colour);
            }

            void Oval(float x, float y, float rx, float ry, Color colour) => paint.Soft(At(x, y), new Vector2(rx * across, ry * s), colour, 0.6f);

            void Line(float x0, float y0, float x1, float y1, float width, Color colour)
            {
                var a = At(x0, y0);
                var b = At(x1, y1);
                var side = new Vector2(-(b - a).y, (b - a).x).normalized * (width * 0.5f);
                paint.Polygon(new[] { a + side, b + side, b - side, a - side }, colour);
            }

            paint.Soft(new Vector2(150, 255), new Vector2(110, 215), new Color(60 / 255f, 140 / 255f, 140 / 255f, 0.35f), 40);

            if (look.Hair == 3)
            {
                Line(147, 84, 140, 176, 5, ribbon);
                Line(153, 84, 161, 176, 5, ribbon);
            }

            // The loose hair behind the shoulders, the boots.
            Fill(hair, 120, 100, 180, 100, 190, 140, 188, 196, 112, 196, 110, 140);
            Fill(dark, 126, 446, 146, 446, 147, 500, 118, 500, 122, 470);
            Fill(dark, 154, 446, 174, 446, 178, 470, 182, 500, 153, 500);

            // The sleeves to the elbow, their pale cuffs, the bracers, the hands.
            Both(robe, 114, 172, 84, 192, 70, 262, 102, 268, 114, 214);
            Both(white, 70, 262, 102, 268, 101, 280, 70, 274);
            Both(dark, 74, 274, 100, 280, 97, 334, 80, 334);
            Oval(89, 346, 10, 13, skin);
            Oval(211, 346, 10, 13, skin);

            // The robe flaring to the hem, its lower half and its front fold in shade; the sash and its ends.
            Fill(robe, 114, 170, 186, 170, 184, 262, 208, 450, 92, 450, 116, 262);
            Fill(shade, 116, 330, 184, 330, 208, 450, 92, 450);
            Fill(shade, 150, 262, 160, 262, 176, 452, 168, 452);
            Fill(dark, 115, 252, 185, 252, 186, 280, 114, 280);
            Fill(dark, 158, 280, 168, 280, 166, 334, 157, 330);

            // The neck, the white under-robe at the throat, the head, the hair framing the face and its two locks.
            Fill(skin, 140, 148, 160, 148, 160, 176, 140, 176);
            Fill(white, 132, 170, 168, 170, 150, 212);
            Oval(150, 122, 23, 29, skin);
            Fill(hair, 124, 130, 124, 102, 134, 88, 150, 82, 166, 88, 176, 102, 176, 130, 170, 110, 160, 100, 150, 98, 140, 100, 130, 110);
            Both(hair, 124, 108, 132, 110, 130, 186, 119, 190);

            if (look.Hair > 0)
            {
                Oval(150, 77, 15, 12, hair);
            }

            switch (look.Hair)
            {
                case 1:
                    Line(124, 76, 176, 70, 3, UiKit.Hex(0x7A5034));
                    Oval(176, 70, 3, 3, UiKit.Hex(0x7A5034));
                    break;
                case 2:
                    Oval(150, 72, 15, 7, UiKit.Hex(0xB9BFC7));
                    Fill(UiKit.Hex(0xB9BFC7), 141, 70, 159, 70, 156, 62, 144, 62);
                    Line(122, 76, 178, 72, 3, UiKit.Hex(0x6FB592));
                    break;
                case 3:
                    Fill(ribbon, 137, 80, 163, 80, 163, 86, 137, 86);
                    break;
            }

            return paint.Texture();
        }

        /// <summary>The right of the hero's page: the path chosen, what a fall costs, what lies ahead, and a warning if the slot is taken.</summary>
        void WayColumn(RectTransform stage)
        {
            var way = Beginnings.Of(newPath);
            var colour = PathColours[DifficultyRules.Order(newPath)];
            var right = UiKit.Rect("Way", stage);
            right.Place(new Vector2(1, 0), new Vector2(1, 1), -500, 128, 56, 186);
            right.Stack<VerticalLayoutGroup>(8);

            var card = UiKit.Image("Path", right, Color.white, UiKit.Plain);
            card.Gradient(new Color(12 / 255f, 16 / 255f, 24 / 255f, 0.72f), new Color(5 / 255f, 7 / 255f, 12 / 255f, 0.84f));
            UiKit.Frame(card.transform, new Color(200 / 255f, 230 / 255f, 240 / 255f, 0.1f));
            card.Size(206);
            var inside = UiKit.Rect("Inside", card.transform).Fill();
            inside.Stack<VerticalLayoutGroup>(6, new RectOffset(22, 22, 16, 14));
            UiKit.Lbl("Ваш путь", inside, 11, UiKit.LabelGold).Size(16);
            UiKit.Disp($"{way.Name} · {way.Grade} · {way.Talent * 100:0}%", inside, 24, colour).Size(30);
            Para(way.About, inside, 13, UiKit.Text2).Size(-1).flexibleHeight = 1;
            var hunger = newRules.Hunger == HungerRule.Off ? "нет" : newRules.Hunger == HungerRule.Soft ? "мягкий" : "как в каноне";
            Para($"Голод Гу: {hunger} · Гу едят сами: {(newRules.AutoFeed ? "да" : "нет")} · переходы платные: {(newRules.TravelCosts ? "да" : "нет")}", inside, 12, UiKit.Muted).Size(18);
            UiKit.HexButton("Изменить путь", inside, () => Show(Page.NewPath), UiKit.HexKind.Ghost, 11, 36).Size(36, 200, 0);

            // What a fall costs: chosen once, for the whole playthrough.
            Gap(right, 6);
            UiKit.Lbl("Цена смерти · на всё прохождение", right, 11, UiKit.LabelGold).Size(16);
            var deathRow = UiKit.Rect("Death", right);
            deathRow.Size(40);
            var deaths = new List<string>();

            foreach (var rule in DeathRules.All)
            {
                deaths.Add(DeathRules.Name(rule));
            }

            UiKit.Choices(deathRow, deaths, Array.IndexOf(DeathRules.All, newRules.Death), i => { newRules.Death = DeathRules.All[i]; Redraw(); }, 14);
            Para(DeathRules.Text(newRules.Death), right, 13, newRules.Death == DeathRule.Hardcore ? UiKit.Danger : UiKit.Text2).Size(40);

            Gap(right, 6);
            UiKit.Lbl("Что ждёт", right, 11, UiKit.LabelGold).Size(16);
            Para("Вам пятнадцать. Завтра — обряд пробуждения апертуры в академии клана Гу Юэ. Камень предков покажет, сколько света в вашей чаше.", right, 14, UiKit.Text2).Size(60);

            var save = Game.ReadSave();

            if (save != null)
            {
                Gap(right, 4);
                UiKit.Lbl("Ячейка занята", right, 11, UiKit.Danger).Size(16);
                Para($"В ячейке {Game.Slot + 1} — Гу Юэ {save.HeroName}, {Cultivation.Name(save.Step)}, {Mathf.RoundToInt(save.PlayedSeconds / 60)} мин. " +
                     "Новый путь заменит это сохранение.", right, 13, UiKit.Text2).Size(36);
            }
        }

        void BeginNewGame() => Game.Instance.BeginNew(newName, newPath, newRules, Appearances.Copy(newLook));
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
