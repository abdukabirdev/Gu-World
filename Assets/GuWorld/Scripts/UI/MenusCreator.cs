using System;
using GuWorld.Body;
using GuWorld.Cultivation;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GuWorld.UI
{
    /// <summary>
    /// The hero's making (UI-Creator.dc.html, the second step of a new game): the look on a
    /// panel at the left — the name after the clan's, gender, height, skin, hair, the
    /// hanfu's colour — the hero himself in the middle on a glowing pad inside slow gold
    /// rings (the real figure, turned by dragging, the wheel bringing the face near), the
    /// view and the way chosen at the right, Back and Begin at the foot.
    /// </summary>
    public partial class Menus
    {
        /// <summary>The making's middle in screen pixels, between the panels: the camera is dragged only there.</summary>
        public (float left, float right) CreatorDragArea
        {
            get
            {
                var scale = Screen.height / DesignStage.Height;

                return (436 * scale, Screen.width - 416 * scale);
            }
        }

        static readonly Color[] SkinSwatches = { UiKit.Hex(0xEBD2BA), UiKit.Hex(0xD9B28E), UiKit.Hex(0xC08E66) };

        void Creator()
        {
            // A new game asks the way first.
            if (host.ChosenPath < 0)
            {
                TalentChoice();

                return;
            }

            var look = host.Look;
            var stage = UiKit.Stage(pageRoot);

            // Only the edges are dark: the hero stands in the world, in the middle.
            var vignette = UiKit.Image("Vignette", stage, new Color(0, 0, 0, 0.6f), UiKit.Vignette);
            vignette.rectTransform.Fill();
            UiKit.Image("Left", stage, new Color(3 / 255f, 4 / 255f, 10 / 255f, 0.55f), UiKit.SoftEdge).rectTransform.Place(Vector2.zero, new Vector2(0, 1), 0, 0, -620, 0);
            var right = UiKit.Image("Right", stage, new Color(3 / 255f, 4 / 255f, 10 / 255f, 0.55f), UiKit.SoftEdge);
            right.rectTransform.Place(new Vector2(1, 0), Vector2.one, -620, 0, 0, 0);
            right.rectTransform.localScale = new Vector3(-1, 1, 1);

            var header = UiKit.Rect("Header", stage);
            header.Place(new Vector2(0, 1), new Vector2(1, 1), 56, -84, 56, 0);
            UiKit.Image("Line", header, UiKit.Gold.A(0.28f), UiKit.FadeBoth).rectTransform.Place(Vector2.zero, new Vector2(1, 0), 0, 0, 0, -1);
            var title = UiKit.Disp("СОЗДАНИЕ ГЕРОЯ", header, 26);
            title.Track(0.16f);
            title.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), 0, 2, -600, -28);
            UiKit.Lbl("Шаг 2 из 2 · клан Гу Юэ", header, 11, UiKit.Muted).rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), 0, -24, -600, 6);
            Steps(header, 2);

            // The look.
            var panel = UiKit.Panel("Look", stage);
            panel.rectTransform.Place(new Vector2(0, 0), new Vector2(0, 1), 56, 150, -436, 116);
            panel.raycastTarget = true;
            var column = UiKit.Rect("Column", panel.transform).Fill();
            column.Stack<VerticalLayoutGroup>(20, new RectOffset(26, 26, 24, 22));

            LookSection(column, "Имя");
            var nameBox = UiKit.Image("Name", column, new Color(4 / 255f, 6 / 255f, 10 / 255f, 0.8f), UiKit.Plain);
            UiKit.Frame(nameBox.transform, new Color(234 / 255f, 240 / 255f, 242 / 255f, 0.16f));
            nameBox.Size(46);
            var clan = UiKit.Disp("Гу Юэ", nameBox.transform, 19, UiKit.Gold);
            clan.rectTransform.Place(Vector2.zero, new Vector2(0, 1), 14, 0, -80, 0);
            var field = UiKit.Field(nameBox.transform, look.name, "своё имя", 16, text => look.name = Look.CleanName(text));
            field.GetComponent<Image>().color = new Color(0, 0, 0, 0);
            field.transform.Find("Frame").gameObject.SetActive(false);
            ((RectTransform)field.transform).Place(Vector2.zero, Vector2.one, 72, 1, 1, 1);
            var hint = UiKit.Rect("Hint", column);
            hint.Size(16);
            UiKit.Label("Своё имя — после имени клана", hint, 12, UiKit.Muted).rectTransform.Fill();
            var random = UiKit.Label("<color=#E8C98A>СЛУЧАЙНО</color>", hint, 11, UiKit.Gold, TextAnchor.MiddleRight);
            random.Track(0.16f);
            random.rectTransform.Fill();
            random.raycastTarget = true;
            random.gameObject.AddComponent<Clickable>().Click = () =>
            {
                // A whole look by chance (the name and the gender kept).
                host.Wear(look.Random(new System.Random()));
                Show(Page.Creator);
            };

            LookSection(column, "Пол");
            LookOptions(column, new[] { "Юноша", "Девушка" }, look.Gender == Gender.Male ? 0 : 1, 1, i =>
            {
                var gender = i == 0 ? Gender.Male : Gender.Female;

                if (gender != look.Gender)
                {
                    host.Wear(look.With(gender: gender, hair: Look.DefaultHair(gender)));
                    Show(Page.Creator);
                }
            });

            // The height: a slider of three steps, the height in centimetres.
            var heightRow = LookSection(column, "Рост");
            var cm = UiKit.Disp($"{Render.HeroView.HeightOf(look) * 100:0} см", heightRow, 18, UiKit.Text, TextAnchor.MiddleRight);
            cm.rectTransform.Fill();
            var statures = (Stature[])Enum.GetValues(typeof(Stature));
            var slider = UiKit.Slider(column, 0, statures.Length - 1, Array.IndexOf(statures, look.Height), v => Look.Name(statures[Mathf.RoundToInt(v)]), v =>
            {
                var s = statures[Mathf.RoundToInt(v)];

                if (s != look.Height)
                {
                    host.Wear(look.With(stature: s));
                    cm.text = $"{Render.HeroView.HeightOf(host.Look) * 100:0} см";
                }
            });
            slider.wholeNumbers = true;
            slider.Size(30);

            LookSection(column, "Кожа");
            var skins = (Skin[])Enum.GetValues(typeof(Skin));
            Swatches(column, skins.Length, i => SkinSwatches[Mathf.Min(i, SkinSwatches.Length - 1)], i => skins[i] == look.SkinTone, i =>
            {
                host.Wear(look.With(skin: skins[i]));
                Show(Page.Creator);
            });

            LookSection(column, "Ханьфу");
            var robes = (Robe[])Enum.GetValues(typeof(Robe));
            Swatches(column, robes.Length, i => Dyes.For(look.Gender, 8 + (int)robes[i]).tunic, i => robes[i] == look.RobeColour, i =>
            {
                host.Wear(look.With(robe: robes[i]));
                Show(Page.Creator);
            });

            LookSection(column, "Причёска");
            var styles = (HairStyle[])Enum.GetValues(typeof(HairStyle));
            LookOptions(column, Array.ConvertAll(styles, Look.Name), Array.IndexOf(styles, look.Hair), 3, i =>
            {
                host.Wear(look.With(hair: styles[i]));
                Show(Page.Creator);
            });

            // The pad and the rings the hero stands in.
            var stand = UiKit.Rect("Stand", stage);
            stand.anchorMin = stand.anchorMax = new Vector2(0.5f, 1);
            stand.pivot = new Vector2(0.5f, 1);
            stand.anchoredPosition = new Vector2(0, -110);
            stand.sizeDelta = new Vector2(500, 640);
            var rings = UiKit.Rect("Rings", stand);
            rings.TopLeft(50, 40, 400, 400);
            UiKit.Orbit(rings, 400, UiKit.Gold.A(0.15f), 140, 0);
            UiKit.Orbit(rings, 320, UiKit.Gold.A(0.12f), 0, 4);
            var pad = UiKit.Image("Pad", stand, UiKit.Essence.A(0.45f), UiKit.Radial);
            pad.rectTransform.TopLeft(250 - 270, 570 - 49, 540, 97);
            var padRing = UiKit.Image("Pad Ring", stand, new Color(160 / 255f, 240 / 255f, 220 / 255f, 0.4f), UiKit.RingLine(300));
            padRing.rectTransform.TopLeft(100, 570 - 24, 300, 48);
            var padDash = UiKit.Image("Pad Dash", stand, UiKit.Gold.A(0.35f), UiKit.RingLine(220, 6));
            padDash.rectTransform.TopLeft(140, 570 - 16, 220, 32);
            var turn = UiKit.Lbl("Потяните мышью — повернуть · колесо — ближе", stand, 11, UiKit.Hex(0x6E7E81), 0.3f, TextAnchor.MiddleCenter);
            turn.rectTransform.Place(Vector2.zero, new Vector2(1, 0), 0, 0, 0, -16);

            // The view and the way.
            var side = UiKit.Rect("Side", stage);
            side.TopRight(56, 116, 360, 560);
            side.Stack<VerticalLayoutGroup>(22);
            var view = UiKit.Rect("View", side);
            view.Stack<VerticalLayoutGroup>(10);
            view.Size(110);
            LookSection(view, "Вид");
            LookOptions(view, new[] { "Весь рост", "Лицо" }, host.CreatorFace ? 1 : 0, 2, i =>
            {
                host.CreatorFace = i == 1;
                Show(Page.Creator);
            });
            UiKit.Label("Колесо мыши приближает · ЛКМ — повернуть", view, 12, UiKit.Muted).Size(16);

            var way = Talents.Paths[Mathf.Clamp(host.ChosenPath, 0, Talents.Paths.Length - 1)];
            var card = UiKit.Panel("Way", side);
            card.Size(176);
            var inside = UiKit.Rect("Inside", card.transform).Fill();
            inside.Stack<VerticalLayoutGroup>(10, new RectOffset(22, 22, 20, 18));
            UiKit.Lbl("Ваш путь", inside).Size(14);
            var names = UiKit.Rect("Names", inside);
            names.Size(32);
            UiKit.Disp(way.Name, names, 30).rectTransform.Fill();
            UiKit.Disp($"{way.Grade} · {way.Talent:0}%", names, 30, PathColours[Mathf.Clamp(host.ChosenPath, 0, PathColours.Length - 1)], TextAnchor.MiddleRight).rectTransform.Fill();
            var about = UiKit.Label($"Море наполняется за {Talents.Span(Aperture.RefillHours(way.Talent))}.", inside, 13, UiKit.Hex(0xA8B6B8), TextAnchor.UpperLeft);
            about.Size(36);
            var change = UiKit.Lbl("Изменить путь", inside, 12, UiKit.Gold, 0.16f);
            change.raycastTarget = true;
            change.gameObject.AddComponent<Clickable>().Click = () =>
            {
                pathPick = host.ChosenPath;
                host.ChosenPath = -1;
                Show(Page.Creator);
            };
            change.Size(16);

            var ahead = UiKit.Rect("Ahead", side);
            ahead.Stack<VerticalLayoutGroup>(8, new RectOffset(4, 0, 0, 0));
            ahead.Size(80);
            UiKit.Lbl("Что ждёт", ahead).Size(14);
            UiKit.Label("Вам пятнадцать. Завтра — обряд пробуждения апертуры в академии клана Гу Юэ.", ahead, 13, UiKit.Hex(0xA8B6B8), TextAnchor.UpperLeft).Size(44);

            // Back and Begin.
            var buttons = UiKit.Rect("Buttons", stage);
            buttons.Place(Vector2.zero, new Vector2(1, 0), 56, 70, 56, -126);
            var row = buttons.Stack<HorizontalLayoutGroup>(16, null, TextAnchor.MiddleLeft);
            row.childForceExpandWidth = false;
            UiKit.HexButton("Назад", buttons, () =>
            {
                pathPick = host.ChosenPath;
                host.ChosenPath = -1;
                Show(Page.Creator);
            }, UiKit.HexKind.Ghost).Size(56, 170, 0);
            UiKit.Rect("Space", buttons).Size(56, -1, 1);
            UiKit.HexButton("Начать путь", buttons, () => FadeThrough(host.StartPlaying), UiKit.HexKind.Solid).Size(56, 280, 0);

            UiKit.Footer(stage, new[] { ("ЛКМ", "Повернуть"), ("Колесо", "Приблизить"), ("Enter", "Начать"), ("Esc", "Назад") });
            var keys = stage.gameObject.AddComponent<PageKeys>();
            keys.On(Key.Enter, () =>
            {
                if (field == null || !field.isFocused)
                {
                    FadeThrough(host.StartPlaying);
                }
            });
        }

        /// <summary>A section's label (the design's .lbl); returns its row (something can sit at its right).</summary>
        static RectTransform LookSection(Transform parent, string name)
        {
            var row = UiKit.Rect(name, parent);
            row.Size(16);
            UiKit.Lbl(name, row).rectTransform.Fill();

            return row;
        }

        /// <summary>Filter buttons in rows of `across`, the chosen one gold.</summary>
        static void LookOptions(Transform parent, string[] names, int chosen, int across, Action<int> pick)
        {
            across = Mathf.Max(1, across == 1 ? names.Length : across);

            for (var i = 0; i < names.Length; i += across)
            {
                var row = UiKit.Rect("Options", parent);
                var group = row.Stack<HorizontalLayoutGroup>(6);
                group.childForceExpandWidth = true;
                row.Size(40);

                for (var k = i; k < Mathf.Min(i + across, names.Length); k++)
                {
                    var index = k;
                    UiKit.Tb(names[k], row, k == chosen, () => pick(index)).Size(40, -1, 1);
                }

                // Keep the last row's buttons as wide as the others.
                for (var k = names.Length; k < i + across && i + across > names.Length; k++)
                {
                    UiKit.Rect("Empty", row).Size(40, -1, 1);
                }
            }
        }

        /// <summary>Round swatches of colour, 40 across; the chosen one ringed in light.</summary>
        static void Swatches(Transform parent, int count, Func<int, Color> colour, Func<int, bool> chosen, Action<int> pick)
        {
            var row = UiKit.Rect("Swatches", parent);
            var group = row.Stack<HorizontalLayoutGroup>(10, null, TextAnchor.MiddleLeft);
            group.childForceExpandWidth = false;
            row.Size(40);

            for (var i = 0; i < count; i++)
            {
                var index = i;
                var on = chosen(i);
                var holder = UiKit.Rect("Swatch", row);
                holder.Size(40, 40, 0);

                if (on)
                {
                    UiKit.HaloAt(holder, UiKit.GoldLit.A(0.4f), Vector2.zero, 64);
                    var ring = UiKit.Image("Ring", holder, UiKit.GoldLit, UiKit.RingLine(46));
                    ring.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(46, 46));
                }
                else
                {
                    var ring = UiKit.Image("Ring", holder, new Color(234 / 255f, 240 / 255f, 242 / 255f, 0.2f), UiKit.RingLine(42));
                    ring.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(42, 42));
                }

                var dot = UiKit.Image("Colour", holder, colour(i), UiKit.Disc);
                dot.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(40, 40));
                dot.raycastTarget = true;
                dot.gameObject.AddComponent<Clickable>().Click = () => pick(index);
            }
        }
    }
}
