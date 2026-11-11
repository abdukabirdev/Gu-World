using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GuWorld.UI
{
    /// <summary>
    /// The interface v3's own parts (UI-System.dc.html): text in its faces and spacing, the
    /// hexagon buttons, filter buttons, keycaps and the hint line, chips, toggles, thin bars,
    /// list rows, glows, the starry backdrop and the hero's pages' header with its tabs.
    /// </summary>
    public static partial class UiKit
    {
        /// <summary>A gradient over a graphic (top to bottom, or left to right when `across`).</summary>
        public static UiGradient Gradient(this Graphic graphic, Color from, Color to, bool across = false, Color? middle = null, float middleAt = 0.5f)
        {
            var gradient = graphic.GetComponent<UiGradient>() ?? graphic.gameObject.AddComponent<UiGradient>();
            gradient.Set(from, to, across, middle, middleAt);

            return gradient;
        }

        /// <summary>Letter spacing in em (CSS letter-spacing); the text stays on one line.</summary>
        public static Text Track(this Text text, float em)
        {
            var tracking = text.GetComponent<Tracking>() ?? text.gameObject.AddComponent<Tracking>();
            tracking.Em = em;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;

            return text;
        }

        /// <summary>
        /// A soft glow round a rectangle (a CSS box-shadow with no offset), `radius` wide, laid
        /// over it but clear inside it. Ignores layouts.
        /// </summary>
        public static Image Glow(Transform target, Color colour, float radius)
        {
            var glow = Image("Glow", target, colour, GlowSprite);
            glow.rectTransform.Fill(-radius);
            glow.pixelsPerUnitMultiplier = 32f / Mathf.Max(1, radius);
            glow.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

            return glow;
        }

        /// <summary>A round halo of a colour at a point (a size across), behind what comes after it.</summary>
        public static Image HaloAt(Transform parent, Color colour, Vector2 centre, float size, Vector2? anchor = null)
        {
            var halo = Image("Halo", parent, colour, Halo);
            halo.rectTransform.At(anchor ?? new Vector2(0.5f, 0.5f), centre, new Vector2(size, size));
            halo.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

            return halo;
        }

        /// <summary>Text in the display face, Cormorant Garamond 600 (names, headings, numbers).</summary>
        public static Text Disp(string text, Transform parent, int size, Color? colour = null, TextAnchor align = TextAnchor.MiddleLeft)
        {
            var label = Label(text, parent, size, colour ?? Text, align);
            label.font = Display;

            return label;
        }

        /// <summary>The design's label: small spaced capitals (11 px, 0.34 em) in the label gold.</summary>
        public static Text Lbl(string text, Transform parent, int size = 11, Color? colour = null, float em = 0.34f, TextAnchor align = TextAnchor.MiddleLeft, bool bold = false)
        {
            var label = Label(text.ToUpperInvariant(), parent, size, colour ?? LabelGold, align, bold ? FontStyle.Bold : FontStyle.Normal);
            label.Track(em);

            return label;
        }

        /// <summary>Hanzi in the seal face (Ma Shan Zheng): Gu's names, seals.</summary>
        public static Text Zh(string text, Transform parent, int size, Color? colour = null, TextAnchor align = TextAnchor.MiddleLeft)
        {
            var label = Label(text, parent, size, colour ?? Gold, align);
            label.font = SealFont;

            return label;
        }

        /// <summary>
        /// A brush stroke in the background (Zhi Mang Xing), however big: the font is drawn at
        /// most 200 and scaled up past that, so the font's texture stays small.
        /// </summary>
        public static Text Brush(string text, Transform parent, float size, Color colour)
        {
            var drawn = Mathf.Min(200, Mathf.RoundToInt(size));
            var label = Label(text, parent, drawn, colour, TextAnchor.MiddleCenter);
            label.font = BrushFont;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            var scale = size / drawn;
            label.rectTransform.localScale = new Vector3(scale, scale, 1);
            label.rectTransform.sizeDelta = new Vector2(drawn * 1.2f * text.Length, drawn * 1.3f);

            return label;
        }

        /// <summary>
        /// The design's hexagon button (.btn): `kind` is its look, `size` its words', `height`
        /// its own. Its words are spaced capitals. Returns the button (Size works on it in layouts).
        /// </summary>
        public static HexButton HexButton(string text, Transform parent, Action click, HexKind kind = HexKind.Main, int size = 14, float height = 56, string key = null)
        {
            var holder = Rect(text, parent);
            holder.sizeDelta = new Vector2(200, height);
            Image glow = null;
            var glowAlpha = 0f;

            if (kind == HexKind.Main || kind == HexKind.Solid || kind == HexKind.Bad)
            {
                glowAlpha = kind == HexKind.Bad ? 0.22f : 0.26f;
                glow = Image("Glow", holder, (kind == HexKind.Bad ? Danger : Gold).A(glowAlpha), GlowSprite);
                glow.rectTransform.Place(Vector2.zero, Vector2.one, -6, -6, -6, -6);
                glow.pixelsPerUnitMultiplier = 32f / 14;
            }

            var face = Image("Face", holder, Color.white, HexFace(kind));
            face.rectTransform.Fill();
            face.raycastTarget = true;
            var ink = kind switch
            {
                HexKind.Solid => OnGold,
                HexKind.Ghost => Text2,
                HexKind.Bad => Hex(0xFFD6CC),
                HexKind.Off => Hex(0x5E6A6C),
                _ => GoldLit,
            };
            var plain = text.IndexOf('<') < 0 && text.IndexOf('\n') < 0;
            var words = Label(plain ? text.ToUpperInvariant() : text, holder, size, ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            words.rectTransform.Fill();
            words.rectTransform.offsetMin = new Vector2(22, 0);
            words.rectTransform.offsetMax = new Vector2(-22, 0);

            if (plain)
            {
                words.Track(0.24f);
            }

            var button = holder.gameObject.AddComponent<HexButton>();
            button.Build(face, glow, words, kind == HexKind.Off ? null : click, glowAlpha);

            if (key != null)
            {
                // A keycap after the words, in the words' colour.
                words.rectTransform.offsetMax = new Vector2(-22 - 40, 0);
                var cap = Key(key, holder, ink.A(0.55f), ink);
                cap.anchorMin = cap.anchorMax = new Vector2(1, 0.5f);
                cap.pivot = new Vector2(1, 0.5f);
                cap.anchoredPosition = new Vector2(-26, 0);
            }

            holder.Size(height);

            return button;
        }

        /// <summary>Makes a hexagon button one that must be held (`seconds`, and a key that holds it too).</summary>
        public static HexButton Held(this HexButton button, float seconds = 1.2f, UnityEngine.InputSystem.Key key = UnityEngine.InputSystem.Key.None)
        {
            button.Hold = seconds;
            button.HoldKey = key;
            var fill = Image("Hold", button.transform, GoldLit.A(0.35f), button.Face.sprite);
            fill.type = UnityEngine.UI.Image.Type.Filled;
            fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.fillAmount = 0;
            fill.rectTransform.Fill();
            fill.transform.SetSiblingIndex(button.Words.transform.GetSiblingIndex());
            button.Fill = fill;

            return button;
        }

        /// <summary>A filter or tab button (.tb): 34 tall, a thin edge; chosen — gold.</summary>
        public static TabButton Tb(string text, Transform parent, bool on, Action click, int size = 12)
        {
            var back = Image(text, parent, Color.white, Plain);
            back.raycastTarget = true;
            var edge = Frame(back.transform, Color.white);
            var words = Label(text.ToUpperInvariant(), back.transform, size, Text2, TextAnchor.MiddleCenter, FontStyle.Normal);
            words.font = FontMedium;
            words.Track(0.16f);
            words.rectTransform.Fill();
            var button = back.gameObject.AddComponent<TabButton>();
            button.Back = back;
            button.Edge = edge;
            button.Words = words;
            button.On = on;
            button.Click = click;
            button.Paint();
            back.Size(34);

            return button;
        }

        /// <summary>A keycap with a key's name (.key): 22 tall, at least 24 wide. Returns its rectangle (sized; a layout element too).</summary>
        public static RectTransform Key(string key, Transform parent, Color? edge = null, Color? ink = null)
        {
            var cap = Image("Key " + key, parent, edge ?? new Color(234 / 255f, 240 / 255f, 242 / 255f, 0.32f), Keycap);
            var words = Label(key.ToUpperInvariant(), cap.transform, 10, ink ?? Text2, TextAnchor.MiddleCenter, FontStyle.Bold);
            words.Track(0.04f);
            words.rectTransform.Place(Vector2.zero, Vector2.one, 0, 1, 0, 0);
            var width = Mathf.Max(24, Mathf.Ceil(words.preferredWidth + key.Length * 0.4f + 12));
            cap.rectTransform.sizeDelta = new Vector2(width, 22);
            var element = cap.Size(22, width, 0);
            element.flexibleHeight = 0;

            return cap.rectTransform;
        }

        /// <summary>A hint: keycaps (a "SHIFT+ЛКМ" gives two, with a plus) and what they do. A row that sizes itself.</summary>
        public static RectTransform Hint(Transform parent, string keys, string what, int size = 12, Color? colour = null)
        {
            var row = Rect("Hint", parent);
            var group = row.Stack<HorizontalLayoutGroup>(8, null, TextAnchor.MiddleLeft);
            group.childForceExpandWidth = false;
            group.childControlWidth = true;
            var parts = keys.Split('+');

            for (var i = 0; i < parts.Length; i++)
            {
                if (i > 0)
                {
                    var plus = Label("+", row, size, colour ?? Muted, TextAnchor.MiddleCenter);
                    plus.Size(22, 8, 0);
                }

                Key(parts[i].Trim(), row);
            }

            if (!string.IsNullOrEmpty(what))
            {
                var label = Label(what, row, size, colour ?? Muted);
                label.Track(0.06f);
                label.Size(22);
            }

            row.Size(30);

            return row;
        }

        /// <summary>
        /// The line of hints at the foot of a screen (.foot): keys and what they do, against the
        /// right edge (or the left), `side` from the edge and `bottom` up.
        /// </summary>
        public static RectTransform Footer(Transform parent, IList<(string keys, string what)> hints, float side = 56, float bottom = 22, bool left = false, string note = null)
        {
            var foot = Rect("Footer", parent);
            foot.Place(new Vector2(0, 0), new Vector2(1, 0), side, bottom, side, -bottom - 30);
            var group = foot.Stack<HorizontalLayoutGroup>(24, null, left ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight);
            group.childForceExpandWidth = false;
            group.childControlWidth = true;

            if (note != null)
            {
                var words = Label(note, foot, 12, Muted);
                words.Track(0.06f);
                words.Size(30);
            }

            foreach (var (keys, what) in hints)
            {
                Hint(foot, keys, what);
            }

            return foot;
        }

        /// <summary>A chip (.chip): a small framed tag in spaced capitals, in a colour.</summary>
        public static RectTransform Chip(string text, Transform parent, Color? colour = null)
        {
            var ink = colour ?? Text2;
            var back = Image("Chip", parent, new Color(234 / 255f, 240 / 255f, 242 / 255f, 0.03f), Plain);
            Frame(back.transform, colour.HasValue ? ink.A(0.4f) : new Color(234 / 255f, 240 / 255f, 242 / 255f, 0.18f));
            var words = Label(text.ToUpperInvariant(), back.transform, 11, ink, TextAnchor.MiddleCenter);
            words.Track(0.2f);
            words.rectTransform.Fill();
            var width = Mathf.Ceil(words.preferredWidth + text.Length * 11 * 0.2f + 20);
            back.rectTransform.sizeDelta = new Vector2(width, 24);
            back.Size(24, width, 0);

            return back.rectTransform;
        }

        /// <summary>A toggle (.tog): a pill with a knob, gold and lit when on.</summary>
        public static RectTransform Toggle(Transform parent, bool on, Action<bool> change)
        {
            var track = Image("Toggle", parent, on ? GoldLit.A(0.18f) : new Color(234 / 255f, 240 / 255f, 242 / 255f, 0.1f), Pill);
            track.raycastTarget = true;
            var edge = Image("Edge", track.transform, on ? GoldLit.A(0.6f) : new Color(234 / 255f, 240 / 255f, 242 / 255f, 0.2f), PillLine);
            edge.rectTransform.Fill();
            var knob = Image("Knob", track.transform, on ? GoldLit : Muted, Disc);
            knob.rectTransform.At(new Vector2(on ? 1 : 0, 0.5f), new Vector2(on ? -10 : 10, 0), new Vector2(12, 12));

            if (on)
            {
                HaloAt(knob.transform, GoldLit.A(0.5f), Vector2.zero, 30);
            }

            track.rectTransform.sizeDelta = new Vector2(40, 20);
            track.Size(20, 40, 0);
            track.gameObject.AddComponent<Clickable>().Click = () => change(!on);

            return track.rectTransform;
        }

        /// <summary>The kinds of the thin bars (.bar): health, stamina, essence, the system's blue.</summary>
        public enum BarKind
        {
            Health,
            Stamina,
            Essence,
            System,
        }

        /// <summary>A thin bar (.bar, 4 tall): a faint track and a gradient fill with a glow. Returns the fill (set its anchorMax.x for the share).</summary>
        public static Image Bar(Transform parent, BarKind kind, float share, float height = 4)
        {
            var track = Image("Bar", parent, new Color(234 / 255f, 240 / 255f, 242 / 255f, 0.08f), Plain);
            track.Size(height);
            var (from, to) = kind switch
            {
                BarKind.Health => (Hex(0x7A1E16), Hex(0xF08A74)),
                BarKind.Stamina => (Hex(0x6E5028), Hex(0xFFF1CF)),
                BarKind.System => (Hex(0x24507A), Hex(0xA0D2FF)),
                _ => (Hex(0x1F6E5E), Hex(0xA8F0DC)),
            };
            var fill = Image("Fill", track.transform, Color.white, Plain);
            fill.rectTransform.Place(Vector2.zero, new Vector2(Mathf.Clamp01(share), 1));
            fill.Gradient(from, to, true);
            Glow(fill.transform, to.A(0.35f), 8);

            return fill;
        }

        /// <summary>
        /// A list row (.row): pointed at or chosen, a gold diamond at its left, a gold wash
        /// fading right, thin gold lines above and below. Returns the row (its content goes in
        /// it, 22 in from the left) and the row's own Clickable.
        /// </summary>
        public static (RectTransform row, Clickable click) ListRow(Transform parent, float height, bool on, Action click)
        {
            var back = Image("Row", parent, new Color(0, 0, 0, 0), Plain);
            back.raycastTarget = true;
            var wash = Image("Wash", back.transform, Gold.A(on ? 0.13f : 0), SoftEdge);
            wash.rectTransform.Fill();
            var top = Image("Top", back.transform, Gold.A(on ? 0.3f : 0), SoftEdge);
            top.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), 0, -1, 0, 0);
            var bottom = Image("Bottom", back.transform, Gold.A(on ? 0.15f : 0), SoftEdge);
            bottom.rectTransform.Place(new Vector2(0, 0), new Vector2(1, 0), 0, 0, 0, -1);
            var diamond = Image("Diamond", back.transform, Gold.A(on ? 1 : 0), Diamond);
            diamond.rectTransform.At(new Vector2(0, 0.5f), new Vector2(7, 0), new Vector2(11, 11));
            var halo = HaloAt(back.transform, Gold.A(on ? 0.45f : 0), new Vector2(7, 0), 30, new Vector2(0, 0.5f));

            foreach (var layer in new Graphic[] { wash, top, bottom, diamond, halo })
            {
                layer.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            }

            back.Size(height);
            var clickable = back.gameObject.AddComponent<Clickable>();
            clickable.Click = click;
            clickable.Pointed = over =>
            {
                if (on)
                {
                    return;
                }

                var a = over ? 0.6f : 0;
                wash.color = Gold.A(0.13f * a);
                top.color = Gold.A(0.3f * a);
                bottom.color = Gold.A(0.15f * a);
                diamond.color = Gold.A(a);
            };

            return (back.rectTransform, clickable);
        }

        /// <summary>A rule: one unit of gold fading out to the right (.rule), or both ways.</summary>
        public static Image Rule(Transform parent, float alpha = 0.32f, bool both = false)
        {
            var rule = Image("Rule", parent, Gold.A(alpha), both ? FadeBoth : SoftEdge);
            rule.Size(1);

            return rule;
        }

        /// <summary>
        /// The starry void behind a screen (.bg, .stars, .vig): soft clouds of teal, violet and
        /// green, faint gold high up, stars that twinkle, dark edges; a huge brush stroke if asked.
        /// </summary>
        public static RectTransform Backdrop(Transform parent, string brush = null, float brushSize = 520, Vector2? brushAt = null, bool opaque = true)
        {
            var root = Rect("Backdrop", parent);
            root.Fill();

            if (opaque)
            {
                var back = Image("Void", root, Void, Plain);
                back.rectTransform.Fill();
                back.raycastTarget = true;
            }

            var clouds = Rect("Clouds", root);
            clouds.Fill(-60);
            Cloud(clouds, new Vector2(0.36f, 0.30f), new Vector2(1280, 880), new Color(46 / 255f, 110 / 255f, 140 / 255f, 0.26f));
            Cloud(clouds, new Vector2(0.72f, 0.38f), new Vector2(1120, 800), new Color(90 / 255f, 60 / 255f, 150 / 255f, 0.2f));
            Cloud(clouds, new Vector2(0.50f, 0.92f), new Vector2(2000, 640), new Color(40 / 255f, 160 / 255f, 130 / 255f, 0.16f));
            Cloud(clouds, new Vector2(0.58f, 0.14f), new Vector2(640, 440), Gold.A(0.08f));
            var drift = clouds.gameObject.AddComponent<Bob>();
            drift.Period = 30;
            drift.Rise = -12;

            StarField(root, 0.55f);

            if (brush != null)
            {
                var stroke = Brush(brush, root, brushSize, Gold.A(0.045f));
                stroke.rectTransform.anchorMin = stroke.rectTransform.anchorMax = new Vector2(1, 1);
                stroke.rectTransform.anchoredPosition = brushAt ?? new Vector2(-40 - brushSize * 0.5f, -120 - brushSize * 0.5f);
            }

            var vignette = Image("Vignette", root, new Color(0, 0, 0, 0.72f), Vignette);
            vignette.rectTransform.Fill();

            return root;
        }

        /// <summary>A soft cloud of colour: an ellipse `size` across, its middle at a share of the parent.</summary>
        public static Image Cloud(Transform parent, Vector2 at, Vector2 size, Color colour)
        {
            var cloud = Image("Cloud", parent, colour, Radial);
            cloud.rectTransform.At(at, Vector2.zero, size);

            return cloud;
        }

        /// <summary>The repeating stars, twinkling between alphas round `alpha`.</summary>
        public static RawImage StarField(Transform parent, float alpha)
        {
            var field = Rect("Stars", parent).gameObject.AddComponent<RawImage>();
            field.texture = Stars;
            field.raycastTarget = false;
            field.rectTransform.Fill();
            field.gameObject.AddComponent<StarTiling>();
            var twinkle = field.gameObject.AddComponent<Pulse>();
            twinkle.Period = 6;
            twinkle.Low = alpha * 0.75f;
            twinkle.High = alpha * 1.3f;

            return field;
        }

        /// <summary>
        /// A page's header (.hdr): the brand — a gold hexagon with an icon (or a hanzi), the
        /// title and a line under it — tabs in the middle with Q and E at their ends, and at the
        /// right whatever `right` puts there and a close button. A gold line under it all.
        /// </summary>
        public static RectTransform Header(Transform parent, string icon, string title, string subtitle, IList<(string name, string key, bool on, Action open)> tabs,
            Action close, Action<RectTransform> right = null, string hanzi = null, float side = 56)
        {
            var header = Rect("Header", parent);
            header.Place(new Vector2(0, 1), new Vector2(1, 1), side, -84, side, 0);
            var line = Image("Line", header, Gold.A(0.28f), FadeBoth);
            line.rectTransform.Place(new Vector2(0, 0), new Vector2(1, 0), 0, 0, 0, -1);

            // The brand.
            var mark = Image("Mark", header, Color.white.A(0), HexagonUp);
            mark.rectTransform.anchorMin = mark.rectTransform.anchorMax = new Vector2(0, 0.5f);
            mark.rectTransform.pivot = new Vector2(0, 0.5f);
            mark.rectTransform.anchoredPosition = Vector2.zero;
            mark.rectTransform.sizeDelta = new Vector2(34, 38);
            var rim = Image("Rim", mark.transform, Gold, HexagonUpLine);
            rim.rectTransform.Fill();

            if (hanzi != null)
            {
                var glyph = Zh(hanzi, mark.transform, 15, Gold, TextAnchor.MiddleCenter);
                glyph.rectTransform.Fill();
            }
            else if (icon != null)
            {
                var symbol = Icon(icon, mark.transform, 16, Gold);
                symbol.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(16, 16));
            }

            var name = Disp(title.ToUpperInvariant(), header, 22, Text);
            name.Track(0.16f);
            name.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), 48, 2, -400, -24);

            if (subtitle != null)
            {
                var small = Lbl(subtitle, header, 11, Muted);
                small.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), 48, -22, -460, 2);
            }

            // The tabs.
            if (tabs != null && tabs.Count > 0)
            {
                var row = Rect("Tabs", header);
                row.anchorMin = row.anchorMax = new Vector2(0.5f, 0.5f);
                row.pivot = new Vector2(0.5f, 0.5f);
                row.anchoredPosition = Vector2.zero;
                var group = row.Stack<HorizontalLayoutGroup>(24, null, TextAnchor.MiddleCenter);
                group.childForceExpandWidth = false;
                var fitter = row.gameObject.AddComponent<ContentSizeFitter>();
                fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                row.sizeDelta = new Vector2(0, 40);
                Key("Q", row);

                foreach (var (tabName, tabKey, on, open) in tabs)
                {
                    Tab(row, tabName, tabKey, on, open);
                }

                Key("E", row);
            }

            // The right: what the page puts there, then the close button.
            var end = Rect("Right", header);
            end.Place(new Vector2(1, 0), new Vector2(1, 1), -420, 0, 0, 0);
            var endRow = end.Stack<HorizontalLayoutGroup>(18, null, TextAnchor.MiddleRight);
            endRow.childForceExpandWidth = false;
            right?.Invoke(end);

            if (close != null)
            {
                var box = Image("Close", end, new Color(0, 0, 0, 0.01f), Plain);
                box.raycastTarget = true;
                Frame(box.transform, new Color(234 / 255f, 240 / 255f, 242 / 255f, 0.2f));
                var cross = Label("×", box.transform, 22, Text2, TextAnchor.MiddleCenter);
                cross.rectTransform.Fill();
                box.Size(40, 40, 0);
                var click = box.gameObject.AddComponent<Clickable>();
                click.Click = close;
                click.Pointed = over => cross.color = over ? GoldLit : Text2;
            }

            return header;
        }

        /// <summary>One of the header's tabs: spaced capitals and its key; chosen — warm gold, a line and a diamond under it.</summary>
        static void Tab(Transform parent, string name, string key, bool on, Action open)
        {
            var tab = Rect(name, parent);
            var group = tab.Stack<HorizontalLayoutGroup>(6, null, TextAnchor.MiddleCenter);
            group.childForceExpandWidth = false;
            tab.Size(40);
            var hit = tab.gameObject.AddComponent<Image>();
            hit.color = new Color(0, 0, 0, 0);
            hit.raycastTarget = true;
            var words = Label(name.ToUpperInvariant(), tab, 12, on ? GoldText : Muted);
            words.Track(0.24f);
            words.Size(40);

            if (on)
            {
                words.Shadowed().GetComponent<Shadow>().effectColor = Gold.A(0.35f);
                var under = Image("Under", tab, Gold, FadeBoth);
                under.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                under.rectTransform.Place(new Vector2(0, 0), new Vector2(1, 0), -8, -2, -4, 1);
                var gem = Image("Gem", tab, Gold, Diamond);
                gem.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                gem.rectTransform.At(new Vector2(0.5f, 0), new Vector2(0, -2), new Vector2(8, 8));
                HaloAt(gem.transform, Gold.A(0.5f), Vector2.zero, 26);
            }

            if (key != null)
            {
                Key(key, tab);
            }

            var click = tab.gameObject.AddComponent<Clickable>();
            click.Click = open;

            if (!on)
            {
                click.Pointed = over => words.color = over ? Text : Muted;
            }
        }

        /// <summary>A constellation node (.node): a dark disc inside a light ring with a dark gap and a faint outer ring.</summary>
        public static Image Node(Transform parent, float size, Color? ring = null)
        {
            var outer = Image("Node", parent, (ring ?? new Color(200 / 255f, 240 / 255f, 255 / 255f, 1)).A(0.1f), RingLine(size + 14));
            outer.rectTransform.sizeDelta = new Vector2(size + 14, size + 14);
            var gap = Image("Gap", outer.transform, new Color(2 / 255f, 3 / 255f, 9 / 255f, 0.8f), Disc);
            gap.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size + 12, size + 12));
            var disc = Image("Disc", outer.transform, Color.white, Disc);
            disc.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size));
            disc.Gradient(new Color(20 / 255f, 34 / 255f, 44 / 255f, 0.95f), new Color(2 / 255f, 3 / 255f, 9 / 255f, 0.95f));
            var edge = Image("Edge", outer.transform, (ring ?? new Color(200 / 255f, 240 / 255f, 255 / 255f, 1)).A(0.5f), RingLine(size + 1));
            edge.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size + 1, size + 1));

            return disc;
        }

        /// <summary>A slow ring round something: a dashed or a whole circle `size` across, turning once in `period` seconds (0: still).</summary>
        public static Image Orbit(Transform parent, float size, Color colour, float period = 60, int dashGap = 6)
        {
            var ring = Image("Orbit", parent, colour, RingLine(size, dashGap));
            ring.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size));
            ring.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

            if (period != 0)
            {
                ring.gameObject.AddComponent<Spin>().Period = period;
            }

            return ring;
        }
    }

    /// <summary>Keeps a repeating texture (the stars) at its own size in canvas units, however big the rectangle.</summary>
    [RequireComponent(typeof(RawImage))]
    public class StarTiling : MonoBehaviour
    {
        Vector2 last;

        void LateUpdate()
        {
            var image = GetComponent<RawImage>();
            var size = ((RectTransform)transform).rect.size;

            if (size == last || image.texture == null)
            {
                return;
            }

            last = size;
            image.uvRect = new Rect(0, 0, size.x / image.texture.width, size.y / image.texture.height);
        }
    }
}
