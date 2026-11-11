using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace GuWorld.UI
{
    /// <summary>
    /// The parts GU World's menus are built from, in code (uGUI), in the look of the
    /// interface v3, «Обряд × Апертура» (Reference/Design/UI-v3): glass panels over a
    /// starry void with a thin cold frame and two gold corners, Cormorant Garamond for
    /// names and big numbers, Inter Tight for everything else, gold for what is chosen,
    /// the essence's sea-green, the moon's blue, a coral red for danger, the ranks'
    /// colours for Gu. A canvas scaled from 1920×1080 (the pages made after the v3 design
    /// put a Stage on it, 900 units tall, the design's own pixels), panels, text, menu
    /// entries, the long hexagon buttons, rows of choices, item cells and the shapes the
    /// HUD needs (rings, discs, diamonds). The shapes are in UiShapes, the effects
    /// (gradients, letter spacing, motion) in UiEffects, the v3's own parts in UiParts.
    /// </summary>
    public static partial class UiKit
    {
        // The design's tokens (README «Токены»). The older names are kept, pointed at the new colours.
        /// <summary>The void behind everything.</summary>
        public static readonly Color Void = Hex(0x03040A);
        public static readonly Color Ink = Hex(0x05070C, 0.9f);
        public static readonly Color Panel1 = Hex(0x0A0E15, 0.86f);
        public static readonly Color PanelTop = Hex(0x0C1018, 0.9f);
        /// <summary>The panels' thin cold frame and the rules.</summary>
        public static readonly Color Line = new Color(200 / 255f, 230 / 255f, 240 / 255f, 0.12f);
        public static readonly Color LineLit = new Color(234 / 255f, 240 / 255f, 242 / 255f, 0.3f);
        public static readonly Color Text = Hex(0xEAF0F2);
        /// <summary>Quieter text (the design's --t2).</summary>
        public static readonly Color Text2 = Hex(0xC9D3D5);
        /// <summary>Captions and hints (--mu).</summary>
        public static readonly Color Muted = Hex(0x7E9093);
        /// <summary>The small spaced headings (--lb).</summary>
        public static readonly Color LabelGold = Hex(0xB8A47C);
        public static readonly Color Gold = Hex(0xE8C98A);
        public static readonly Color GoldLit = Hex(0xFFF1CF);
        public static readonly Color GoldDeep = Hex(0x8C6A3A);
        /// <summary>The chosen tab's text.</summary>
        public static readonly Color GoldText = Hex(0xF7E7C2);
        public static readonly Color Cinnabar = Hex(0xB84A3C);
        public static readonly Color CinnabarLit = Hex(0xF08A74);
        public static readonly Color Seal = Hex(0x0C1018, 0.95f);
        /// <summary>Threat, a failure, a deletion.</summary>
        public static readonly Color Danger = Hex(0xF08A74);
        public static readonly Color Essence = Hex(0x7FE0C8);
        public static readonly Color EssenceLit = Hex(0xA8F0DC);
        /// <summary>The moon; the system window's blue.</summary>
        public static readonly Color Moon = Hex(0xA0D2FF);
        public static readonly Color SystemBlue = Hex(0x6FB8FF);
        /// <summary>Enough, ready.</summary>
        public static readonly Color Ok = Hex(0x9FEFC4);
        public static readonly Color Hunger = Hex(0xF0B060);
        public static readonly Color Life = Hex(0xF08A74);
        /// <summary>A chosen tab or filter: a faint gold.</summary>
        public static readonly Color Chosen = new Color(1, 241 / 255f, 207 / 255f, 0.1f);
        public static readonly Color InkLight = new Color(14 / 255f, 18 / 255f, 26 / 255f, 0.9f);
        public static readonly Color InkHover = new Color(26 / 255f, 32 / 255f, 44 / 255f, 0.95f);
        /// <summary>Text on gold.</summary>
        public static readonly Color OnGold = Hex(0x1A1008);

        public static Color Hex(int rgb, float alpha = 1) =>
            new Color(((rgb >> 16) & 0xff) / 255f, ((rgb >> 8) & 0xff) / 255f, (rgb & 0xff) / 255f, alpha);

        /// <summary>A colour with another alpha.</summary>
        public static Color A(this Color colour, float alpha) => new Color(colour.r, colour.g, colour.b, alpha);

        static Font font, fontMedium, fontBold, display, sealFont, brushFont;

        static Font LoadFont(string name, ref Font cache)
        {
            if (cache == null)
            {
                cache = Resources.Load<Font>("Fonts/" + name);

                if (cache == null)
                {
                    cache = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }
            }

            return cache;
        }

        /// <summary>The interface's text: Inter Tight 400, 500 and 600 (Google Fonts, OFL).</summary>
        public static Font Font => LoadFont("InterTight-Regular", ref font);
        public static Font FontMedium => LoadFont("InterTight-Medium", ref fontMedium);
        public static Font FontBold => LoadFont("InterTight-SemiBold", ref fontBold);
        /// <summary>Headings, names and big numbers: Cormorant Garamond 600.</summary>
        public static Font Display => LoadFont("CormorantGaramond-SemiBold", ref display);
        /// <summary>Gu's names and seals in hanzi: Ma Shan Zheng (a subset).</summary>
        public static Font SealFont => LoadFont("MaShanZheng-Regular", ref sealFont);
        /// <summary>The brush strokes in the background: Zhi Mang Xing (a subset).</summary>
        public static Font BrushFont => LoadFont("ZhiMangXing-Regular", ref brushFont);

        /// <summary>A screen-wide canvas (and the event system the pointer needs).</summary>
        public static Canvas Canvas(string name, int order)
        {
            var root = new GameObject(name, typeof(RectTransform));
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = order;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            root.AddComponent<GraphicRaycaster>();

            if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() == null)
            {
                var events = new GameObject("Event System", typeof(EventSystem), typeof(InputSystemUIInputModule));
                UnityEngine.Object.DontDestroyOnLoad(events);
            }

            return canvas;
        }

        /// <summary>
        /// A stage over its parent in the design's own units: always 900 tall (the design's
        /// 1440 × 900 frame), as wide as the screen's shape makes it (1600 on 16:9).
        /// </summary>
        public static RectTransform Stage(Transform parent)
        {
            var rect = Rect("Stage", parent);
            rect.gameObject.AddComponent<DesignStage>().Fit();

            return rect;
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);

            return rect;
        }

        /// <summary>Anchors a rectangle: min and max anchors, then offsets from them (left, bottom, right, top).</summary>
        public static RectTransform Place(this RectTransform rect, Vector2 min, Vector2 max, float left = 0, float bottom = 0, float right = 0, float top = 0)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);

            return rect;
        }

        /// <summary>Puts a rectangle of a size with its centre at a point of its parent (from the parent's anchor point).</summary>
        public static RectTransform At(this RectTransform rect, Vector2 anchor, Vector2 centre, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = centre;
            rect.sizeDelta = size;

            return rect;
        }

        /// <summary>
        /// Puts a rectangle as the design does, by its top-left corner and size, measured
        /// from the parent's top-left (y down), the way CSS's `left`/`top` read.
        /// </summary>
        public static RectTransform TopLeft(this RectTransform rect, float left, float top, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(left, -top);
            rect.sizeDelta = new Vector2(width, height);

            return rect;
        }

        /// <summary>The same from the parent's top-right corner (CSS `right`/`top`).</summary>
        public static RectTransform TopRight(this RectTransform rect, float right, float top, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(1, 1);
            rect.anchoredPosition = new Vector2(-right, -top);
            rect.sizeDelta = new Vector2(width, height);

            return rect;
        }

        /// <summary>The same from the parent's bottom-left corner (CSS `left`/`bottom`).</summary>
        public static RectTransform BottomLeft(this RectTransform rect, float left, float bottom, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0, 0);
            rect.pivot = new Vector2(0, 0);
            rect.anchoredPosition = new Vector2(left, bottom);
            rect.sizeDelta = new Vector2(width, height);

            return rect;
        }

        /// <summary>The same from the parent's bottom-right corner (CSS `right`/`bottom`).</summary>
        public static RectTransform BottomRight(this RectTransform rect, float right, float bottom, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(1, 0);
            rect.pivot = new Vector2(1, 0);
            rect.anchoredPosition = new Vector2(-right, bottom);
            rect.sizeDelta = new Vector2(width, height);

            return rect;
        }

        /// <summary>Fills its parent (with an inset).</summary>
        public static RectTransform Fill(this RectTransform rect, float inset = 0) =>
            rect.Place(Vector2.zero, Vector2.one, inset, inset, inset, inset);

        public static Image Image(string name, Transform parent, Color colour, Sprite sprite = null)
        {
            var image = Rect(name, parent).gameObject.AddComponent<Image>();
            image.color = colour;
            image.sprite = sprite;
            image.type = sprite != null && sprite.border != Vector4.zero ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
            image.raycastTarget = false;

            return image;
        }

        /// <summary>
        /// A panel: glass over the void — a dark gradient, a thin cold frame and two gold
        /// corners (top left, bottom right), a deep shadow under it. A colour given only
        /// sets how see-through it is; a frame given replaces the cold one.
        /// </summary>
        public static Image Panel(string name, Transform parent, Color? colour = null, Color? frame = null, bool corners = true)
        {
            var panel = Image(name, parent, new Color(1, 1, 1, Mathf.Clamp01((colour?.a ?? 0.97f) / 0.97f)), Plain);
            panel.Gradient(new Color(12 / 255f, 16 / 255f, 24 / 255f, 0.78f), new Color(5 / 255f, 7 / 255f, 12 / 255f, 0.9f));
            Frame(panel.transform, frame ?? new Color(200 / 255f, 230 / 255f, 240 / 255f, 0.1f));

            if (corners)
            {
                GoldCorners(panel.transform, 14);
            }

            return panel;
        }

        /// <summary>A thin square outline over a shape (ignoring layouts).</summary>
        public static Image Frame(Transform parent, Color colour)
        {
            var frame = Image("Frame", parent, colour, Outline);
            frame.rectTransform.Fill();
            frame.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

            return frame;
        }

        /// <summary>The two gold corners of a panel: top left and bottom right, `size` long, 1 thick, just outside the frame.</summary>
        public static void GoldCorners(Transform parent, float size, Color? colour = null)
        {
            var gold = colour ?? Gold;
            Corner(parent, gold, new Vector2(0, 1), new Vector2(-1, 1), new Vector2(size, 1));
            Corner(parent, gold, new Vector2(0, 1), new Vector2(-1, 1), new Vector2(1, size));
            Corner(parent, gold, new Vector2(1, 0), new Vector2(1, -1), new Vector2(size, 1));
            Corner(parent, gold, new Vector2(1, 0), new Vector2(1, -1), new Vector2(1, size));
        }

        static void Corner(Transform parent, Color colour, Vector2 anchor, Vector2 offset, Vector2 size)
        {
            var line = Image("Corner", parent, colour);
            line.rectTransform.anchorMin = line.rectTransform.anchorMax = anchor;
            line.rectTransform.pivot = anchor;
            line.rectTransform.anchoredPosition = offset;
            line.rectTransform.sizeDelta = size;
            line.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        }

        /// <summary>
        /// Text. Bold is Inter Tight's semibold face, not a made-up bold; `Display` text
        /// uses Cormorant Garamond.
        /// </summary>
        public static Text Label(string text, Transform parent, int size, Color? colour = null, TextAnchor align = TextAnchor.MiddleLeft, FontStyle style = FontStyle.Normal)
        {
            var label = Rect("Text", parent).gameObject.AddComponent<Text>();
            label.font = style == FontStyle.Bold || style == FontStyle.BoldAndItalic ? FontBold : Font;
            label.text = text;
            label.fontSize = size;
            label.fontStyle = style == FontStyle.BoldAndItalic ? FontStyle.Italic : style == FontStyle.Bold ? FontStyle.Normal : style;
            label.color = colour ?? Text;
            label.alignment = align;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            label.supportRichText = true;

            return label;
        }

        /// <summary>An icon (Icons), square, tinted.</summary>
        public static Image Icon(string name, Transform parent, float size, Color colour)
        {
            var icon = Image(name ?? "Icon", parent, colour, Icons.Get(name));
            icon.preserveAspect = true;
            icon.rectTransform.sizeDelta = new Vector2(size, size);
            icon.enabled = icon.sprite != null;

            return icon;
        }

        /// <summary>Text set as a mark (a rank's numeral in a seal): centred, in the display face.</summary>
        public static Text Mark(string text, Transform parent, int size, Color colour)
        {
            var mark = Label(text, parent, size, colour, TextAnchor.MiddleCenter);
            mark.font = Display;

            return mark;
        }

        /// <summary>A small heading in spaced capitals ("ЗАДАНИЕ"), the design's label gold.</summary>
        public static Text Caps(string text, Transform parent, int size = 15, Color? colour = null)
        {
            var caps = Label(text.ToUpperInvariant(), parent, size, colour ?? LabelGold, TextAnchor.MiddleLeft, FontStyle.Bold);
            caps.Track(0.3f);

            return caps;
        }

        /// <summary>Capitals with a thin space between letters, for text made into a string before it is shown.</summary>
        public static string Spaced(string text) => string.Join(" ", text.ToUpperInvariant().ToCharArray());

        /// <summary>A seal: the design's brand mark, a hexagon outlined in gold, with an icon in it.</summary>
        public static Image SealMark(string icon, Transform parent, float size)
        {
            var seal = Image("Seal", parent, Seal, HexagonUp);
            seal.rectTransform.sizeDelta = new Vector2(size, size * 38 / 34f);
            var rim = Image("Rim", seal.transform, Gold, HexagonUpLine);
            rim.rectTransform.Fill();
            var mark = Icon(icon, seal.transform, size * 0.5f, Gold);
            mark.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one * size * 0.5f);

            return seal;
        }

        /// <summary>The same seal with a numeral or a word in it (a rank, a grade).</summary>
        public static Image SealText(string text, Transform parent, float size)
        {
            var seal = Image("Seal", parent, Seal, HexagonUp);
            seal.rectTransform.sizeDelta = new Vector2(size, size * 38 / 34f);
            var rim = Image("Rim", seal.transform, Gold, HexagonUpLine);
            rim.rectTransform.Fill();
            var mark = Mark(text, seal.transform, Mathf.RoundToInt(size * (text.Length > 2 ? 0.36f : 0.52f)), GoldLit);
            mark.rectTransform.Fill();

            return seal;
        }

        /// <summary>A soft dark shadow under text that sits on the world.</summary>
        public static T Shadowed<T>(this T graphic) where T : Graphic
        {
            var shadow = graphic.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.75f);
            shadow.effectDistance = new Vector2(1.5f, -1.5f);

            return graphic;
        }

        /// <summary>Lays children out in a column (or a row), with spacing and padding.</summary>
        public static T Stack<T>(this RectTransform rect, float spacing, RectOffset padding = null, TextAnchor align = TextAnchor.UpperLeft)
            where T : HorizontalOrVerticalLayoutGroup
        {
            var group = rect.gameObject.AddComponent<T>();
            group.spacing = spacing;
            group.padding = padding ?? new RectOffset(0, 0, 0, 0);
            group.childAlignment = align;
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = false;

            return group;
        }

        public static LayoutElement Size(this Component part, float height = -1, float width = -1, float flexible = -1)
        {
            var element = part.GetComponent<LayoutElement>() ?? part.gameObject.AddComponent<LayoutElement>();

            if (height >= 0) element.preferredHeight = element.minHeight = height;
            if (width >= 0) element.preferredWidth = element.minWidth = width;
            if (flexible >= 0) element.flexibleWidth = flexible;

            return element;
        }

        /// <summary>A big entry of a menu: a name in the display face that the chosen row's gold lights when pointed at; a note under it or beside it.</summary>
        public static MenuEntry Entry(string text, string detail, Transform parent, Action click, string icon = null)
        {
            var rect = Rect(text, parent);
            var entry = rect.gameObject.AddComponent<MenuEntry>();
            entry.Build(text, detail, click, icon);
            rect.Size(detail != null ? 74 : 58);

            return entry;
        }

        /// <summary>
        /// A button, the design's long hexagon: plain (a quiet grey edge), gold — the main
        /// action (a gold edge round a dark body), danger (coral), or a chosen tab (a solid gold
        /// body). Its words are set in spaced capitals.
        /// </summary>
        public static HexButton Button(string text, Transform parent, Action click, ButtonKind kind = ButtonKind.Plain, int size = 20)
        {
            var look = kind switch
            {
                ButtonKind.Gold => HexKind.Main,
                ButtonKind.Danger => HexKind.Bad,
                ButtonKind.Tab => HexKind.Solid,
                ButtonKind.Solid => HexKind.Solid,
                ButtonKind.Off => HexKind.Off,
                _ => HexKind.Ghost,
            };

            return HexButton(text, parent, click, look, Mathf.RoundToInt(size * 0.8f), 52);
        }

        /// <summary>A row: a name on the left, and whatever `fill` puts on the right.</summary>
        public static RectTransform Row(string name, Transform parent, float height = 56)
        {
            var row = Rect(name, parent);
            row.Stack<HorizontalLayoutGroup>(16, null, TextAnchor.MiddleLeft);
            row.Size(height);
            var label = Label(name, row, 19, Text);
            label.Size(-1, 300, 0);

            return row;
        }

        /// <summary>A row of choices (the design's filter buttons), the chosen one lit gold; `pick` gets the index chosen.</summary>
        public static void Choices(Transform parent, IList<string> options, int chosen, Action<int> pick, int size = 18)
        {
            var row = Rect("Choices", parent);
            row.Stack<HorizontalLayoutGroup>(6, null, TextAnchor.MiddleLeft).childForceExpandWidth = true;
            row.Size(48, -1, 1);

            for (var i = 0; i < options.Count; i++)
            {
                var index = i;
                var button = Tb(options[i], row, i == chosen, () => pick(index), Mathf.RoundToInt(size * 0.8f));
                button.Size(44, -1, 1);
            }
        }

        /// <summary>A slider from min to max (the design's: a hairline, a gold fill, a diamond to drag), its value beside it.</summary>
        public static Slider Slider(Transform parent, float min, float max, float value, Func<float, string> show, Action<float> change)
        {
            var holder = Rect("Slider", parent);
            holder.Size(48, -1, 1);
            var hit = Image("Hit", holder, new Color(0, 0, 0, 0));
            hit.rectTransform.Place(new Vector2(0, 0), new Vector2(1, 1), 0, 0, 90, 0);
            hit.raycastTarget = true;
            var track = Image("Track", holder, new Color(234 / 255f, 240 / 255f, 242 / 255f, 0.12f));
            track.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(1, 0.5f), 0, -1, 90, -1);
            var fillArea = Rect("Fill Area", holder).Place(new Vector2(0, 0.5f), new Vector2(1, 0.5f), 0, -1, 90, -1);
            var fill = Image("Fill", fillArea, Color.white);
            fill.Gradient(GoldDeep, GoldLit, true);
            fill.rectTransform.Fill();
            var handleArea = Rect("Handle Area", holder).Place(new Vector2(0, 0), new Vector2(1, 1), 0, 0, 90, 0);
            var handle = Image("Handle", handleArea, GoldLit, Diamond);
            handle.rectTransform.sizeDelta = new Vector2(20, 0);
            handle.rectTransform.anchorMin = new Vector2(0, 0.5f);
            handle.rectTransform.anchorMax = new Vector2(0, 0.5f);
            handle.rectTransform.sizeDelta = new Vector2(20, 20);
            handle.raycastTarget = true;
            Glow(handle.transform, GoldLit.A(0.5f), 10);
            var number = Label(show(value), holder, 22, Text, TextAnchor.MiddleRight);
            number.font = Display;
            number.rectTransform.Place(new Vector2(1, 0), new Vector2(1, 1), -80, 0, 0, 0);

            var slider = holder.gameObject.AddComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.transition = Selectable.Transition.None;
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = value;
            slider.onValueChanged.AddListener(v =>
            {
                number.text = show(v);
                change(v);
            });

            return slider;
        }

        /// <summary>A one-line text field (the hero's name), with a hint while empty.</summary>
        public static InputField Field(Transform parent, string value, string hint, int limit, Action<string> change)
        {
            var box = Image("Field", parent, new Color(4 / 255f, 6 / 255f, 10 / 255f, 0.8f), Plain);
            box.raycastTarget = true;
            Frame(box.transform, new Color(234 / 255f, 240 / 255f, 242 / 255f, 0.16f));
            box.Size(44, -1, 1);
            var text = Label("", box.transform, 16, Text);
            text.rectTransform.Fill(14);
            text.supportRichText = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            var placeholder = Label(hint, box.transform, 15, Muted.A(0.8f));
            placeholder.rectTransform.Fill(14);

            var field = box.gameObject.AddComponent<InputField>();
            field.textComponent = text;
            field.placeholder = placeholder;
            field.characterLimit = limit;
            field.lineType = InputField.LineType.SingleLine;
            field.targetGraphic = box;
            field.transition = Selectable.Transition.None;
            field.caretColor = GoldLit;
            field.selectionColor = Gold.A(0.35f);
            field.text = value ?? "";
            field.onValueChanged.AddListener(v => change(v));

            return field;
        }

        /// <summary>
        /// An item cell (the design's octagon): a dark round glow inside a thin cold edge, an
        /// icon in the middle, a count at the foot, the rank's strip under it. Clicking it calls `click`.
        /// </summary>
        public static Cell Cell(Transform parent, float size, Action click = null)
        {
            var back = Image("Cell", parent, Color.white, Octagon);
            back.raycastTarget = click != null;
            back.rectTransform.sizeDelta = new Vector2(size, size);
            var cell = back.gameObject.AddComponent<Cell>();
            cell.Build(back, size, click);

            return cell;
        }
    }

    public enum ButtonKind
    {
        /// <summary>A quiet one (the design's ghost).</summary>
        Plain,
        /// <summary>The main action (a gold edge, a dark body).</summary>
        Gold,
        Danger,
        /// <summary>A chosen tab or filter.</summary>
        Tab,
        /// <summary>A solid gold body (the action the page is for).</summary>
        Solid,
        /// <summary>Out of reach: greyed.</summary>
        Off,
    }

    /// <summary>A clickable image that eases between its resting and lit colours (in real time, so it works while paused).</summary>
    public class Hover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        Image image;
        Color rest;
        Color lit;
        Action click;
        bool over;

        /// <summary>Whether the pointer is on it.</summary>
        public bool Over => over;

        public void Set(Image target, Color resting, Color lighted, Action onClick)
        {
            image = target;
            rest = resting;
            lit = lighted;
            click = onClick;
            image.color = rest;
        }

        public void OnPointerEnter(PointerEventData _) => over = true;
        public void OnPointerExit(PointerEventData _) => over = false;

        public void OnPointerClick(PointerEventData data)
        {
            if (data.button == PointerEventData.InputButton.Left)
            {
                click?.Invoke();
            }
        }

        void Update()
        {
            if (image != null)
            {
                image.color = Color.Lerp(image.color, over ? lit : rest, 1 - Mathf.Exp(-18 * Time.unscaledDeltaTime));
            }
        }
    }

    /// <summary>An item cell: rim, icon, count, a corner mark (rank), a badge (the vital Gu's heart), a dot (hunger), a shade (rest after use), the rank's strip.</summary>
    public class Cell : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public Image Back;
        public Image Rim;
        public Image Mark;
        public Text Count;
        public Text Corner;
        public Text Key;
        public Image Badge;
        public Image Dot;
        public Image Shade;
        /// <summary>The strip in the rank's colour under the cell (hidden until SetRank).</summary>
        public Image Strip;
        Action click;
        bool over;
        Color rim;

        public void Build(Image back, float size, Action onClick)
        {
            Back = back;
            click = onClick;
            Rim = UiKit.Image("Rim", back.transform, UiKit.Line, UiKit.OctagonLine);
            Rim.rectTransform.Fill();
            Shade = UiKit.Image("Rest", back.transform, new Color(0, 0, 0, 0.62f), UiKit.Octagon);
            Shade.rectTransform.Place(Vector2.zero, new Vector2(1, 0));
            Mark = UiKit.Icon(null, back.transform, size * 0.5f, UiKit.Text);
            Mark.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one * size * 0.5f);
            Count = UiKit.Label("", back.transform, Mathf.RoundToInt(Mathf.Clamp(size * 0.18f, 11, 16)), UiKit.Text2, TextAnchor.LowerRight, FontStyle.Bold).Shadowed();
            Count.rectTransform.Fill(size * 0.09f);
            Corner = UiKit.Label("", back.transform, Mathf.RoundToInt(Mathf.Clamp(size * 0.2f, 11, 16)), UiKit.Essence, TextAnchor.UpperRight);
            Corner.font = UiKit.Display;
            Corner.rectTransform.Fill(size * 0.1f);
            var badge = Mathf.Clamp(size * 0.2f, 10, 16);
            Badge = UiKit.Icon("heart", back.transform, badge, UiKit.Danger);
            Badge.rectTransform.At(Vector2.zero, new Vector2(badge * 0.5f + size * 0.1f, badge * 0.5f + size * 0.08f), Vector2.one * badge);
            Badge.enabled = false;
            Key = UiKit.Label("", back.transform, Mathf.RoundToInt(Mathf.Clamp(size * 0.17f, 10, 14)), UiKit.Muted, TextAnchor.UpperLeft, FontStyle.Bold);
            Key.rectTransform.Fill(size * 0.1f);
            Dot = UiKit.Image("Dot", back.transform, UiKit.Hunger, UiKit.Disc);
            Dot.rectTransform.At(new Vector2(1, 1), new Vector2(-size * 0.14f, -size * 0.14f), new Vector2(8, 8));
            Dot.enabled = false;
            Strip = UiKit.Image("Rank", back.transform, Color.clear);
            Strip.rectTransform.Place(new Vector2(0, 0), new Vector2(1, 0), size * 0.14f, 0, size * 0.14f, -2);
            Strip.enabled = false;
            SetRim(UiKit.Line);
        }

        public void SetRim(Color colour)
        {
            rim = colour;
            Rim.color = colour;
        }

        /// <summary>The strip under the cell in a rank's colour (0: none).</summary>
        public void SetRank(int rank)
        {
            Strip.enabled = rank > 0;

            if (rank > 0)
            {
                Strip.color = UiKit.RankColour(rank);
            }
        }

        /// <summary>The icon in the middle (null: none).</summary>
        public void SetIcon(string name)
        {
            Mark.sprite = Icons.Get(name);
            Mark.enabled = Mark.sprite != null;
        }

        /// <summary>How far through its rest it still is (0 ready … 1 just used): a shade rising from the foot.</summary>
        public void SetRest(float share) => Shade.rectTransform.anchorMax = new Vector2(1, Mathf.Clamp01(share));

        public void OnPointerClick(PointerEventData data)
        {
            if (data.button == PointerEventData.InputButton.Left)
            {
                click?.Invoke();
            }
        }

        /// <summary>Told when the pointer comes over the cell (a page showing what is under it).</summary>
        public Action Hovered;

        public void OnPointerEnter(PointerEventData _)
        {
            over = click != null;
            Hovered?.Invoke();
        }

        public void OnPointerExit(PointerEventData _) => over = false;

        void Update()
        {
            if (Rim != null && click != null)
            {
                Rim.color = over ? UiKit.GoldLit : rim;
            }
        }
    }

    /// <summary>
    /// An entry of a menu, the design's list row: a name in the display face; pointed at
    /// (or chosen), a gold diamond shows at its left, a gold wash fades to the right and
    /// thin gold lines run above and below it. A note can sit under the name or after it.
    /// </summary>
    public class MenuEntry : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        Text title;
        Text note;
        Text aside;
        Image mark;
        Image wash;
        Image top;
        Image bottom;
        Image diamond;
        Image halo;
        Image diamond2;
        Image halo2;
        Action click;
        float lit;
        bool over;
        Color rest = UiKit.Text2;
        Color bright = Color.white;

        /// <summary>Lit as chosen even when the pointer is elsewhere.</summary>
        public bool Chosen;
        /// <summary>Worked by keys (MenuKeys): only being chosen lights it; the pointer chooses it.</summary>
        public bool Keyed;
        /// <summary>Told when the pointer comes onto it.</summary>
        public Action Entered;

        public void Build(string text, string detail, Action onClick, string icon, int size = 26)
        {
            click = onClick;
            var rect = (RectTransform)transform;
            wash = UiKit.Image("Wash", rect, new Color(1, 1, 1, 0), UiKit.SoftEdge);
            wash.rectTransform.Fill();
            wash.raycastTarget = true;
            top = UiKit.Image("Top", rect, new Color(1, 1, 1, 0), UiKit.SoftEdge);
            top.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), 0, -1, 0, 0);
            bottom = UiKit.Image("Bottom", rect, new Color(1, 1, 1, 0), UiKit.SoftEdge);
            bottom.rectTransform.Place(new Vector2(0, 0), new Vector2(1, 0), 0, 0, 0, -1);
            halo = UiKit.Image("Halo", rect, new Color(1, 1, 1, 0), UiKit.Halo);
            halo.rectTransform.At(new Vector2(0, 0.5f), new Vector2(8, 0), new Vector2(32, 32));
            diamond = UiKit.Image("Diamond", rect, new Color(1, 1, 1, 0), UiKit.Diamond);
            diamond.rectTransform.At(new Vector2(0, 0.5f), new Vector2(8, 0), new Vector2(11, 11));
            title = UiKit.Label(text, rect, size, rest, TextAnchor.MiddleLeft);
            title.font = UiKit.Display;
            title.horizontalOverflow = HorizontalWrapMode.Overflow;
            title.rectTransform.Place(new Vector2(0, detail != null ? 0.42f : 0), Vector2.one, 26, 0, 60, 0);

            if (detail != null)
            {
                note = UiKit.Label(detail, rect, 12, UiKit.Hex(0xA8B6B8));
                note.Track(0.08f);
                note.rectTransform.Place(Vector2.zero, new Vector2(1, 0.42f), 27, 4, 20, 0);
            }

            if (icon != null)
            {
                mark = UiKit.Icon(icon, rect, 22, UiKit.Muted.A(0.5f));
                mark.rectTransform.At(new Vector2(1, 0.5f), new Vector2(-24, 0), new Vector2(22, 22));
            }
        }

        /// <summary>The name's size, the row's height and a note after the name (spaced capitals); returns the entry.</summary>
        public MenuEntry Restyle(int size, float height, string after = null)
        {
            title.fontSize = size;
            this.Size(height);

            if (after != null)
            {
                aside = UiKit.Lbl(after, transform, 11, UiKit.Muted, 0.2f);
                aside.rectTransform.Place(new Vector2(0, 0), new Vector2(0, 1), 26 + title.preferredWidth + 14, 0, -400, 0);
            }

            return this;
        }

        /// <summary>A quiet entry (leaving the game): small spaced capitals, grey.</summary>
        public MenuEntry Quiet(float height)
        {
            title.font = UiKit.Font;
            title.fontSize = 13;
            title.text = title.text.ToUpperInvariant();
            title.Track(0.24f);
            rest = UiKit.Muted;
            bright = UiKit.Text;
            title.color = rest;
            this.Size(height);

            return this;
        }

        /// <summary>A centred entry (the pause): the name in the middle, the wash fading both ways, a diamond at each side and no lines.</summary>
        public MenuEntry Centre(float inset = 60)
        {
            title.alignment = TextAnchor.MiddleCenter;
            title.rectTransform.offsetMin = new Vector2(0, title.rectTransform.offsetMin.y);
            title.rectTransform.offsetMax = new Vector2(0, title.rectTransform.offsetMax.y);
            wash.sprite = UiKit.FadeBoth;
            top.enabled = false;
            bottom.enabled = false;
            diamond.rectTransform.anchoredPosition = new Vector2(inset + 4, 0);
            halo.rectTransform.anchoredPosition = new Vector2(inset + 4, 0);
            halo2 = UiKit.Image("Halo", transform, new Color(1, 1, 1, 0), UiKit.Halo);
            halo2.rectTransform.At(new Vector2(1, 0.5f), new Vector2(-inset - 4, 0), new Vector2(32, 32));
            diamond2 = UiKit.Image("Diamond", transform, new Color(1, 1, 1, 0), UiKit.Diamond);
            diamond2.rectTransform.At(new Vector2(1, 0.5f), new Vector2(-inset - 4, 0), new Vector2(10, 10));

            return this;
        }

        /// <summary>The name's resting colour (a quieter or warning entry).</summary>
        public MenuEntry Tint(Color colour)
        {
            rest = colour;
            title.color = colour;

            return this;
        }

        /// <summary>Does what the entry is for.</summary>
        public void Open() => click?.Invoke();

        public void OnPointerEnter(PointerEventData _)
        {
            over = true;
            Entered?.Invoke();
        }

        public void OnPointerExit(PointerEventData _) => over = false;

        public void OnPointerClick(PointerEventData data)
        {
            if (data.button == PointerEventData.InputButton.Left)
            {
                click?.Invoke();
            }
        }

        void Update()
        {
            lit = Mathf.MoveTowards(lit, (over && !Keyed) || Chosen ? 1 : 0, Time.unscaledDeltaTime * 7);
            var ease = lit * lit * (3 - 2 * lit);
            wash.color = UiKit.Gold.A(0.14f * ease);
            top.color = UiKit.Gold.A(0.3f * ease);
            bottom.color = UiKit.Gold.A(0.15f * ease);
            diamond.color = UiKit.Gold.A(ease);
            halo.color = UiKit.Gold.A(0.45f * ease);

            if (diamond2 != null)
            {
                diamond2.color = diamond.color;
                halo2.color = halo.color;
            }
            title.color = Color.Lerp(rest, bright, ease);

            if (mark != null)
            {
                mark.color = Color.Lerp(UiKit.Muted.A(0.5f), UiKit.Gold, ease);
            }
        }
    }
}
