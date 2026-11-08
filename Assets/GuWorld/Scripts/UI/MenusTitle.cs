using System;
using System.Collections.Generic;
using GuWorld.Cultivation;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GuWorld.UI
{
    /// <summary>
    /// The main menu (UI-Title.dc.html): night over Qing Mao — a crescent moon in its gold
    /// rings, two ranges of mountains in mist with a village's lit windows, motes of essence
    /// drifting up; the 蛊 brush mark and the name at the top left, the menu under it, the
    /// mountain's name at the bottom right, the keys at the foot.
    /// </summary>
    public partial class Menus
    {
        static Texture2D titleLand;
        static Sprite titleMoon;

        void Title()
        {
            var stage = UiKit.Stage(pageRoot);

            // The sky: a gradient and two clouds of light.
            var sky = UiKit.Image("Sky", stage, Color.white, UiKit.Plain);
            sky.rectTransform.Fill();
            sky.raycastTarget = true;
            sky.Gradient(UiKit.Hex(0x060A14), UiKit.Void, false, UiKit.Void, 0.7f);
            UiKit.Cloud(stage, new Vector2(0.74f, 0.70f), new Vector2(1800, 1200), new Color(60 / 255f, 100 / 255f, 160 / 255f, 0.32f));
            UiKit.Cloud(stage, new Vector2(0.5f, 0), new Vector2(2400, 800), new Color(40 / 255f, 160 / 255f, 130 / 255f, 0.22f));
            UiKit.StarField(stage, 0.6f);

            // The moon: its breathing glow, the crescent, two gold rings round it.
            var moon = UiKit.Rect("Moon", stage);
            moon.TopRight(320 - 260, 300 - 260, 520, 520);
            // CSS's circle reaches the box's corners: clear at 70% of that, about the box's own half.
            var haze = UiKit.Image("Haze", moon, new Color(200 / 255f, 220 / 255f, 245 / 255f, 0.32f), UiKit.Radial);
            haze.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(734, 734));
            var breath = haze.gameObject.AddComponent<Pulse>();
            breath.Period = 9;
            breath.Low = 0.6f;
            breath.High = 1;
            breath.Grow = 0.05f;
            UiKit.HaloAt(moon, new Color(1, 1, 1, 0.22f), Vector2.zero, 250);
            var crescent = UiKit.Image("Crescent", moon, Color.white, TitleMoon());
            crescent.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(280, 280));
            UiKit.Orbit(moon, 280, UiKit.Gold.A(0.22f), 0, 0);
            UiKit.Orbit(moon, 380, UiKit.Gold.A(0.12f), 160, 10);

            // The land: the mountains, the village, the mist (one painting).
            var land = UiKit.Rect("Land", stage).gameObject.AddComponent<RawImage>();
            land.texture = TitleLand();
            land.raycastTarget = false;
            land.rectTransform.Place(Vector2.zero, new Vector2(1, 0), 0, 0, 0, -480);

            // A band of fog drifting to and fro, and the motes rising.
            var fog = UiKit.Image("Fog", stage, new Color(150 / 255f, 175 / 255f, 210 / 255f, 0.12f), UiKit.Radial);
            fog.rectTransform.Place(new Vector2(-0.1f, 0), new Vector2(1.1f, 0), -43, 120, 43, -320);
            var sway = fog.gameObject.AddComponent<Bob>();
            sway.Period = 26;
            sway.Rise = 86;
            sway.Way = Vector2.right;

            for (var m = 0; m < 26; m++)
            {
                var x = 500 + Fract(Mathf.Sin(m * 12.9898f) * 43758.5453f) * 900;
                var y = 300 + Fract(Mathf.Sin(m * 78.233f) * 12345.678f) * 500;
                var size = 1 + m % 3 * 0.8f;
                var colour = m % 4 == 0 ? new Color(232 / 255f, 201 / 255f, 138 / 255f, 0.9f) : new Color(170 / 255f, 240 / 255f, 220 / 255f, 0.8f);
                var mote = UiKit.Image("Mote", stage, colour, UiKit.Halo);
                mote.rectTransform.TopLeft(x - size * 2, y - size * 2, size * 5, size * 5);
                var drift = mote.gameObject.AddComponent<Drift>();
                drift.Period = 9 + m % 5 * 2;
                drift.Delay = m * 0.9f;
            }

            var vignette = UiKit.Image("Vignette", stage, new Color(0, 0, 0, 0.78f), UiKit.Vignette);
            vignette.rectTransform.Fill();

            // The brush mark and the name.
            var mark = UiKit.Brush("蛊", stage, 92, UiKit.Gold);
            mark.rectTransform.anchorMin = mark.rectTransform.anchorMax = new Vector2(0, 1);
            mark.rectTransform.anchoredPosition = new Vector2(72 + 46, -96 - 46);
            UiKit.HaloAt(stage, UiKit.Gold.A(0.16f), new Vector2(72 + 46, -96 - 46), 180, new Vector2(0, 1)).transform.SetSiblingIndex(mark.transform.GetSiblingIndex());
            var name = UiKit.Disp("GU WORLD", stage, 76, Color.white);
            name.Track(0.14f);
            name.Gradient(Color.white, UiKit.Hex(0xBFAE8A), false, UiKit.Hex(0xF2EADB));
            name.rectTransform.TopLeft(182, 92, 700, 80);
            var motto = UiKit.Lbl("Мир Гу · путь мастера", stage, 12, UiKit.LabelGold, 0.5f);
            motto.rectTransform.TopLeft(184, 168, 600, 18);

            // The menu.
            var menu = UiKit.Rect("Menu", stage);
            menu.TopLeft(72, 330, 420, 520);
            menu.Stack<VerticalLayoutGroup>(2, null, TextAnchor.UpperLeft);
            var keys = menu.gameObject.AddComponent<MenuKeys>();

            if (host.HasSave)
            {
                var resume = UiKit.Entry("Продолжить", host.SaveSummary, menu, () => FadeThrough(host.Continue));
                resume.Restyle(30, 78);
                keys.Add(resume);
            }

            keys.Add(UiKit.Entry("Новый путь", null, menu, () =>
            {
                if (host.HasSave)
                {
                    Ask("Начать новый путь?", "Сохранённая игра будет удалена: вы заново создадите героя и начнёте с первого утра.",
                        "Начать заново", () => FadeThrough(host.NewGame), Page.Title);
                }
                else
                {
                    FadeThrough(host.NewGame);
                }
            }).Restyle(24, 54));
            keys.Add(UiKit.Entry("Апертура бессмертного", null, menu, () => FadeThrough(host.OpenSandbox)).Restyle(24, 54, "Полигон"));
            keys.Add(UiKit.Entry("Кодекс Гу", null, menu, () => Show(Page.Codex)).Restyle(24, 54, $"{GuBook.Count} Гу"));
            keys.Add(UiKit.Entry("Настройки", null, menu, () => OpenSettings(Page.Title)).Restyle(24, 54));
            keys.Add(UiKit.Entry("Об игре", null, menu, () => Show(Page.About)).Restyle(24, 54));
            var gap = UiKit.Rect("Gap", menu);
            gap.Size(21);
            var rule = UiKit.Image("Rule", gap, UiKit.Gold.A(0.3f), UiKit.SoftEdge);
            rule.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(1, 0.5f), 26, 0, 0, -1);
            keys.Add(UiKit.Entry("Выйти из игры", null, menu, host.Quit).Quiet(48));

            // The mountain's name.
            var place = UiKit.Rect("Place", stage);
            place.BottomRight(72, 96, 420, 44);
            var where = UiKit.Lbl("Гора Цинмао · Южная граница", place, 11, UiKit.LabelGold, 0.34f, TextAnchor.UpperRight);
            where.rectTransform.Place(new Vector2(0, 1), Vector2.one, 0, -16, 0, 0);
            var arc = UiKit.Label("Клан Гу Юэ · арка первая", place, 13, UiKit.Muted, TextAnchor.LowerRight);
            arc.rectTransform.Place(Vector2.zero, new Vector2(1, 0), 0, 0, 0, -20);

            UiKit.Footer(stage, new[] { ("↑↓", "Выбор"), ("Enter", "Открыть") }, 72, 28, false, null);
            var version = UiKit.Label($"Версия {Version}", stage, 12, UiKit.Muted);
            version.Track(0.06f);
            version.rectTransform.BottomLeft(72, 28, 300, 30);
        }

        static float Fract(float v) => ((v % 1) + 1) % 1;

        /// <summary>The moon of the main menu: a pale disc with a dark one over it, a crescent (280 across, its middle the moon's).</summary>
        static Sprite TitleMoon()
        {
            if (titleMoon != null)
            {
                return titleMoon;
            }

            const int size = 280;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
            var pixels = new Color[size * size];
            var light = new Vector2(140 - 96 + 0.35f * 192, 140 - 96 + 0.35f * 192);

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    // y down, the moon's middle at 140, 140; the dark disc 36 right and 22 up.
                    var p = new Vector2(x + 0.5f, size - y - 0.5f);
                    var moon = Mathf.Clamp01(96.5f - Vector2.Distance(p, new Vector2(140, 140)));
                    var dark = Mathf.Clamp01(88.5f - Vector2.Distance(p, new Vector2(176, 118)));
                    var t = Mathf.Clamp01(Vector2.Distance(p, light) / (0.7f * 192));
                    var lit = t < 0.6f ? Color.Lerp(Color.white, UiKit.Hex(0xE4ECF6), t / 0.6f) : Color.Lerp(UiKit.Hex(0xE4ECF6), UiKit.Hex(0x9FB4CC), (t - 0.6f) / 0.4f);
                    var colour = Color.Lerp(lit, UiKit.Hex(0x071022), dark);
                    colour.a = Mathf.Max(moon, dark);
                    pixels[y * size + x] = colour;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            return titleMoon = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        /// <summary>The main menu's land (the design's 1440 wide, from 420 down to the foot): two ranges, snow, a village, mist.</summary>
        static Texture2D TitleLand()
        {
            if (titleLand != null)
            {
                return titleLand;
            }

            var paint = new UiPainter(1440, 480);
            Vector2[] Shift(IEnumerable<Vector2> points)
            {
                var list = new List<Vector2>();

                foreach (var p in points)
                {
                    list.Add(new Vector2(p.x, p.y - 420));
                }

                return list.ToArray();
            }

            paint.Polygon(Shift(UiPainter.Path(0, 640, 120, 560, 210, 600, 330, 500, 420, 548, 540, 470, 640, 530, 760, 430, 860, 500, 980, 420,
                1100, 520, 1210, 470, 1330, 540, 1440, 500, 1440, 900, 0, 900)), UiKit.Hex(0x16263A), UiKit.Hex(0x07090F));
            paint.Polygon(Shift(UiPainter.Path(760, 430, 800, 470, 776, 466, 760, 446, 744, 470, 724, 470)), new Color(200 / 255f, 220 / 255f, 240 / 255f, 0.25f));
            paint.Polygon(Shift(UiPainter.Path(980, 420, 1010, 450, 992, 448, 980, 436, 966, 452)), new Color(200 / 255f, 220 / 255f, 240 / 255f, 0.2f));
            paint.Soft(new Vector2(760, 600 - 420), new Vector2(700, 50), UiKit.Hex(0x8FB0D0, 0.1f), 22);
            paint.Polygon(Shift(UiPainter.Path(0, 720, 180, 640, 300, 690, 460, 620, 600, 700, 760, 640, 900, 710, 1060, 650, 1200, 720, 1320, 680,
                1440, 710, 1440, 900, 0, 900)), UiKit.Hex(0x0C1622), UiKit.Hex(0x040508));

            // The village: three houses with curved roofs, three lit windows.
            var house = UiKit.Hex(0x05070C);
            paint.Rect(930, 662 - 420, 40, 30, house);
            paint.Polygon(Shift(Roof(912, 666, 950, 650, 988, 666, 976, 660, 970, 652, 930, 652, 924, 660)), house);
            paint.Rect(938, 636 - 420, 24, 18, house);
            paint.Polygon(Shift(Roof(924, 640, 950, 628, 976, 640, 968, 634, 964, 628, 936, 628, 932, 634)), house);
            paint.Rect(1010, 676 - 420, 46, 24, house);
            paint.Polygon(Shift(Roof(992, 680, 1033, 666, 1074, 680, 1062, 674, 1056, 668, 1010, 668, 1004, 674)), house);
            var window = UiKit.Hex(0xF0C878);
            paint.Rect(942, 672 - 420, 4, 7, window.A(0.8f));
            paint.Rect(956, 672 - 420, 4, 7, window.A(0.6f));
            paint.Rect(1026, 684 - 420, 4, 7, window.A(0.7f));

            paint.Soft(new Vector2(720, 760 - 420), new Vector2(800, 60), UiKit.Hex(0x7FE0C8, 0.07f), 22);
            var ground = new List<Vector2> { new Vector2(0, 800) };
            ground.AddRange(UiPainter.Cubic(new Vector2(0, 800), new Vector2(240, 760), new Vector2(480, 790), new Vector2(720, 770)));
            ground.AddRange(UiPainter.Cubic(new Vector2(720, 770), new Vector2(960, 750), new Vector2(1200, 790), new Vector2(1440, 770)));
            ground.Add(new Vector2(1440, 900));
            ground.Add(new Vector2(0, 900));
            paint.Polygon(Shift(ground), UiKit.Hex(0x020306));

            return titleLand = paint.Texture();
        }

        /// <summary>A roof as the design draws it: M a Q b c Q d e L f L g Q h a — two curves and the eaves' line.</summary>
        static List<Vector2> Roof(float ax, float ay, float bx, float by, float cx, float cy, float dx, float dy, float ex, float ey, float fx, float fy, float gx, float gy)
        {
            var a = new Vector2(ax, ay);
            var points = new List<Vector2> { a };
            points.AddRange(UiPainter.Curve(a, new Vector2(bx, by), new Vector2(cx, cy)));
            points.AddRange(UiPainter.Curve(new Vector2(cx, cy), new Vector2(dx, dy), new Vector2(ex, ey)));
            points.Add(new Vector2(fx, fy));
            points.AddRange(UiPainter.Curve(new Vector2(fx, fy), new Vector2(gx, gy), a));

            return points;
        }
    }

    /// <summary>
    /// A menu the keys can work: ↑ and ↓ choose an entry (the pointer chooses too), Enter
    /// opens it. The chosen entry is lit as the design's chosen row.
    /// </summary>
    public class MenuKeys : MonoBehaviour
    {
        readonly List<MenuEntry> entries = new List<MenuEntry>();
        int chosen;

        public void Add(MenuEntry entry)
        {
            var index = entries.Count;
            entries.Add(entry);
            entry.Keyed = true;
            entry.Entered = () => Choose(index);
            Choose(chosen);
        }

        void Choose(int index)
        {
            chosen = Mathf.Clamp(index, 0, entries.Count - 1);

            for (var i = 0; i < entries.Count; i++)
            {
                entries[i].Chosen = i == chosen;
            }
        }

        void Update()
        {
            var keyboard = Keyboard.current;

            if (keyboard == null || entries.Count == 0)
            {
                return;
            }

            if (keyboard.downArrowKey.wasPressedThisFrame)
            {
                Choose((chosen + 1) % entries.Count);
            }
            else if (keyboard.upArrowKey.wasPressedThisFrame)
            {
                Choose((chosen + entries.Count - 1) % entries.Count);
            }
            else if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
            {
                entries[chosen].Open();
            }
        }
    }
}
