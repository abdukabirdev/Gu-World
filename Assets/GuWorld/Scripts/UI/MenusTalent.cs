using System;
using GuWorld.Cultivation;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GuWorld.UI
{
    /// <summary>
    /// The way to begin (UI-Path.dc.html, the first step of a new game, before the hero is
    /// made): five cards side by side, each a difficulty that is the talent one is born with —
    /// from an A talent of 99% (with a chance of the Ten Extreme Physique) to a D talent of
    /// 30% — with its sea drawn full to the talent's share, how soon it refills and how hard
    /// it is. The chosen card stands taller in gold; ← → choose, Enter goes on.
    /// </summary>
    public partial class Menus
    {
        /// <summary>The way picked on the page (not yet taken).</summary>
        int pathPick = 1;

        static readonly Color[] PathColours =
        {
            UiKit.Hex(0x9FEFC4), UiKit.Hex(0x7FE0C8), UiKit.Hex(0xF0C868), UiKit.Hex(0xF0A070), UiKit.Hex(0xF08A74),
        };

        void TalentChoice()
        {
            var stage = UiKit.Stage(pageRoot);
            UiKit.Backdrop(stage, null);
            var brush = UiKit.Brush("道", stage, 560, UiKit.Gold.A(0.04f));
            brush.rectTransform.anchorMin = brush.rectTransform.anchorMax = new Vector2(0.5f, 1);
            brush.rectTransform.anchoredPosition = new Vector2(0, -60 - 280);
            var count = Talents.Paths.Length;
            pathPick = Mathf.Clamp(pathPick, 0, count - 1);

            // The header: the title, the step, two marks for the two steps.
            var header = UiKit.Rect("Header", stage);
            header.Place(new Vector2(0, 1), new Vector2(1, 1), 56, -84, 56, 0);
            UiKit.Image("Line", header, UiKit.Gold.A(0.28f), UiKit.FadeBoth).rectTransform.Place(Vector2.zero, new Vector2(1, 0), 0, 0, 0, -1);
            var title = UiKit.Disp("ВЫБОР ПУТИ", header, 26);
            title.Track(0.16f);
            title.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), 0, 2, -600, -28);
            UiKit.Lbl("Шаг 1 из 2 · затем создание героя", header, 11, UiKit.Muted).rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), 0, -24, -600, 6);
            Steps(header, 1);

            var intro = UiKit.Label("Путь — это трудность игры и талант, с которым вы родились. Талант решает, сколько эссенции вмещает море " +
                                    "апертуры и как быстро оно наполняется.", stage, 15, UiKit.Hex(0xA8B6B8), TextAnchor.UpperLeft);
            intro.lineSpacing = 1.2f;
            intro.rectTransform.TopLeft(56, 108, 720, 60);

            // The cards, their feet on one line.
            var cards = UiKit.Rect("Cards", stage);
            cards.Place(new Vector2(0, 1), new Vector2(1, 1), 56, -736, 56, 176);
            var row = cards.Stack<HorizontalLayoutGroup>(18, null, TextAnchor.LowerCenter);
            row.childForceExpandWidth = true;
            row.childControlHeight = true;

            for (var i = 0; i < count; i++)
            {
                PathCard(cards, i, i == pathPick);
            }

            // Back, what is chosen, on.
            var chosen = Talents.Paths[pathPick];
            var foot = UiKit.Rect("Choice", stage);
            foot.Place(new Vector2(0, 1), new Vector2(1, 1), 56, -826, 56, 770);
            var line = foot.Stack<HorizontalLayoutGroup>(16, null, TextAnchor.MiddleLeft);
            line.childForceExpandWidth = false;
            UiKit.HexButton("Назад", foot, () => FadeThrough(host.BackToTitle), UiKit.HexKind.Ghost).Size(56, 170, 0);
            UiKit.Label($"Выбрано: <color=#FFF1CF>{chosen.Name}</color> · талант {chosen.Grade} · {chosen.Talent:0}%", foot, 13, UiKit.Muted).Size(56, -1, 1);
            UiKit.HexButton("К созданию героя", foot, TakePath, UiKit.HexKind.Solid).Size(56, 300, 0);

            UiKit.Footer(stage, new[] { ("← →", "Путь"), ("Enter", "Дальше"), ("Esc", "Назад") });

            var keys = stage.gameObject.AddComponent<PageKeys>();
            keys.On(Key.LeftArrow, () => PickPath(pathPick - 1));
            keys.On(Key.RightArrow, () => PickPath(pathPick + 1));
            keys.On(Key.Enter, TakePath);
            keys.On(Key.NumpadEnter, TakePath);
        }

        void PickPath(int index)
        {
            pathPick = Mathf.Clamp(index, 0, Talents.Paths.Length - 1);
            Show(Page.Creator);
        }

        void TakePath()
        {
            host.ChosenPath = pathPick;
            Show(Page.Creator);
        }

        /// <summary>The two steps of a new game as two short bars at the header's right, `lit` of them lit.</summary>
        static void Steps(RectTransform header, int lit)
        {
            for (var i = 0; i < 2; i++)
            {
                var bar = UiKit.Image("Step", header, i < lit ? UiKit.GoldLit : new Color(234 / 255f, 240 / 255f, 242 / 255f, 0.15f), UiKit.Plain);
                bar.rectTransform.anchorMin = bar.rectTransform.anchorMax = new Vector2(1, 0.5f);
                bar.rectTransform.pivot = new Vector2(1, 0.5f);
                bar.rectTransform.anchoredPosition = new Vector2(-(1 - i) * 36, 0);
                bar.rectTransform.sizeDelta = new Vector2(28, 2);

                if (i < lit)
                {
                    UiKit.Glow(bar.transform, UiKit.GoldLit.A(0.5f), 8);
                }
            }
        }

        void PathCard(Transform parent, int index, bool on)
        {
            var path = Talents.Paths[index];
            var colour = PathColours[Mathf.Min(index, PathColours.Length - 1)];
            var card = UiKit.Image(path.Name, parent, Color.white, UiKit.Plain);
            card.raycastTarget = true;
            card.Size(on ? 560 : 520, -1, 1);

            if (on)
            {
                card.Gradient(new Color(40 / 255f, 34 / 255f, 24 / 255f, 0.85f), new Color(10 / 255f, 10 / 255f, 14 / 255f, 0.92f));
                UiKit.Glow(card.transform, UiKit.Gold.A(0.12f), 40);
                UiKit.Frame(card.transform, UiKit.GoldLit.A(0.6f));
                UiKit.GoldCorners(card.transform, 16);
            }
            else
            {
                card.Gradient(new Color(12 / 255f, 16 / 255f, 24 / 255f, 0.7f), new Color(5 / 255f, 7 / 255f, 12 / 255f, 0.82f));
                UiKit.Frame(card.transform, new Color(200 / 255f, 230 / 255f, 240 / 255f, 0.1f));
            }

            var click = card.gameObject.AddComponent<Clickable>();
            click.Click = () =>
            {
                if (on)
                {
                    TakePath();
                }
                else
                {
                    PickPath(index);
                }
            };

            // The grade, huge and faint, cut by the card's edge.
            var clip = UiKit.Rect("Clip", card.transform).Fill();
            clip.gameObject.AddComponent<RectMask2D>();
            var ghost = UiKit.Label(path.Grade.ToString(), clip, 200, on ? UiKit.Gold.A(0.12f) : new Color(234 / 255f, 240 / 255f, 242 / 255f, 0.04f), TextAnchor.UpperRight);
            ghost.font = UiKit.Display;
            ghost.rectTransform.TopRight(-8, -30, 240, 240);
            ghost.rectTransform.localScale = new Vector3(1.1f, 1.1f, 1);

            var column = UiKit.Rect("Column", card.transform).Fill();
            column.Stack<VerticalLayoutGroup>(0, new RectOffset(22, 22, 26, 24));
            UiKit.Lbl($"Талант {path.Grade}", column, 11, colour, 0.3f).Size(14);
            UiKit.Rect("Gap", column).Size(8);
            UiKit.Disp(path.Name, column, 34, on ? Color.white : UiKit.Text2).Size(36);
            UiKit.Rect("Gap", column).Size(26);
            var seaRow = UiKit.Rect("Sea", column);
            seaRow.Size(150);
            Sea(seaRow, path.Talent, colour, on);
            UiKit.Rect("Gap", column).Size(24);
            UiKit.Image("Rule", column, new Color(234 / 255f, 240 / 255f, 242 / 255f, 0.1f), UiKit.Plain).Size(1);
            UiKit.Rect("Gap", column).Size(12);
            Pair(column, "Наполнение", Talents.Span(Aperture.RefillHours(path.Talent)));
            UiKit.Rect("Gap", column).Size(8);
            var hard = Pair(column, "Трудность", null);
            var pips = UiKit.Rect("Pips", hard);
            pips.Place(new Vector2(1, 0), new Vector2(1, 1), -80, 0, 0, 0);

            for (var k = 0; k < 5; k++)
            {
                var pip = UiKit.Image("Pip", pips, k <= index ? colour : new Color(234 / 255f, 240 / 255f, 242 / 255f, 0.14f), UiKit.Diamond);
                pip.rectTransform.At(new Vector2(1, 0.5f), new Vector2(-5 - (4 - k) * 14, 0), new Vector2(10, 10));
            }

            UiKit.Rect("Gap", column).Size(16);
            var about = UiKit.Label(path.About + (path.ExtremeChance > 0 ? $" Шанс {path.ExtremeChance * 100:0}% на Телосложение Десяти Крайностей." : ""),
                column, 13, UiKit.Hex(0xA8B6B8), TextAnchor.UpperLeft);
            about.lineSpacing = 1.1f;
            about.Size(-1).flexibleHeight = 1;
        }

        /// <summary>A line of a card: a quiet name at the left, a value at the right.</summary>
        static RectTransform Pair(Transform parent, string name, string value)
        {
            var row = UiKit.Rect(name, parent);
            row.Size(18);
            UiKit.Label(name, row, 12, UiKit.Muted).rectTransform.Fill();

            if (value != null)
            {
                UiKit.Label(value, row, 12, UiKit.Text2, TextAnchor.MiddleRight).rectTransform.Fill();
            }

            return row;
        }

        /// <summary>
        /// A sea of essence in a ring, 150 across: dark water space filled from the foot to
        /// the talent's share, a wave running on its top, the path's ring and a dashed gold one;
        /// the share written over it.
        /// </summary>
        static void Sea(RectTransform holder, float talent, Color colour, bool on)
        {
            var box = UiKit.Rect("Ring", holder);
            box.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150, 150));
            var deep = UiKit.Image("Deep", box, new Color(2 / 255f, 6 / 255f, 10 / 255f, 0.9f), UiKit.Disc);
            deep.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(116, 116));
            var share = Mathf.Clamp01(talent / 100f);
            var water = UiKit.Image("Water", box, new Color(127 / 255f, 224 / 255f, 200 / 255f, on ? 0.75f : 0.45f), UiKit.Disc);
            water.type = Image.Type.Filled;
            water.fillMethod = Image.FillMethod.Vertical;
            water.fillOrigin = (int)Image.OriginVertical.Bottom;
            water.fillAmount = share;
            water.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(116, 116));

            // The wave: a line as wide as the water's top.
            var level = -58 + 116 * share;
            var half = Mathf.Sqrt(Mathf.Max(0, 58 * 58 - level * level));

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

            UiKit.Orbit(box, 124, colour.A(0.7f), 0, 0);
            UiKit.Orbit(box, 140, UiKit.Gold.A(0.2f), 90, 6);
            var number = UiKit.Disp($"{talent:0}%", box, 42, Color.white, TextAnchor.MiddleCenter);
            number.Shadowed();
            number.rectTransform.Place(Vector2.zero, Vector2.one, 0, 10, 0, 0);
            var word = UiKit.Lbl("Море", box, 10, UiKit.Hex(0xCFE8E2), 0.24f, TextAnchor.MiddleCenter);
            word.rectTransform.Place(Vector2.zero, Vector2.one, 0, 0, 0, 0);
            word.rectTransform.anchoredPosition = new Vector2(0, -22);
        }

        static Texture2D wave;

        /// <summary>A wave's line, one period of 60 across (the design's "q15 -6 30 0 t30 0"), to repeat.</summary>
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
                // Two humps of 30 each, 3 high (a quadratic's peak is half its control's height).
                var t = x / (float)w * 2 % 1;
                var y = h * 0.5f + 3 * 2 * (1 - (2 * t - 1) * (2 * t - 1)) * (x < w / 2 ? 1 : -1) * 0.5f;

                for (var py = 0; py < h; py++)
                {
                    pixels[py * w + x] = new Color(1, 1, 1, Mathf.Clamp01(1.4f - Mathf.Abs(py + 0.5f - y)));
                }
            }

            wave.SetPixels(pixels);
            wave.Apply();

            return wave;
        }
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

    /// <summary>Keys a page answers to while it is shown (each once per press).</summary>
    public class PageKeys : MonoBehaviour
    {
        readonly System.Collections.Generic.List<(Key key, Action act)> keys = new System.Collections.Generic.List<(Key, Action)>();
        int born;

        void Awake() => born = Time.frameCount;

        public void On(Key key, Action act) => keys.Add((key, act));

        void Update()
        {
            var keyboard = Keyboard.current;

            // Not the press that opened the page.
            if (keyboard == null || Time.frameCount == born)
            {
                return;
            }

            foreach (var (key, act) in keys)
            {
                if (keyboard[key].wasPressedThisFrame)
                {
                    act();

                    return;
                }
            }
        }
    }
}
