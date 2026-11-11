using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GuWorld.UI
{
    /// <summary>
    /// Keeps a rectangle over its parent in the design's units: 900 tall, as wide as the
    /// parent's shape makes it, scaled to fit. What is put on it can be placed by the
    /// design's own numbers (UiKit.TopLeft and the like).
    /// </summary>
    public class DesignStage : MonoBehaviour
    {
        public const float Height = 900;
        Vector2 fitted;

        public void Fit()
        {
            var rect = (RectTransform)transform;
            var parent = rect.parent as RectTransform;

            if (parent == null)
            {
                return;
            }

            var size = parent.rect.size;

            if (size.y <= 0)
            {
                return;
            }

            fitted = size;
            var scale = size.y / Height;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(size.x / scale, Height);
            rect.localScale = new Vector3(scale, scale, 1);
        }

        void LateUpdate()
        {
            if (transform.parent is RectTransform parent && parent.rect.size != fitted)
            {
                Fit();
            }
        }
    }

    /// <summary>
    /// A gradient over a graphic (a CSS linear-gradient): each vertex takes the colour of
    /// where it lies, top to bottom or left to right, two colours or three (with the middle
    /// one's place). Multiplies the graphic's own colour.
    /// </summary>
    public class UiGradient : BaseMeshEffect
    {
        public Color From = Color.white;
        public Color To = Color.white;
        public Color? Middle;
        public float MiddleAt = 0.5f;
        public bool Across;

        public void Set(Color from, Color to, bool across = false, Color? middle = null, float middleAt = 0.5f)
        {
            From = from;
            To = to;
            Across = across;
            Middle = middle;
            MiddleAt = middleAt;

            if (graphic != null)
            {
                graphic.SetVerticesDirty();
            }
        }

        Color At(float t)
        {
            if (Middle is Color middle)
            {
                return t < MiddleAt ? Color.Lerp(From, middle, t / MiddleAt) : Color.Lerp(middle, To, (t - MiddleAt) / (1 - MiddleAt));
            }

            return Color.Lerp(From, To, t);
        }

        public override void ModifyMesh(VertexHelper helper)
        {
            if (!IsActive() || helper.currentVertCount == 0)
            {
                return;
            }

            var rect = graphic.rectTransform.rect;
            var vertex = new UIVertex();

            for (var i = 0; i < helper.currentVertCount; i++)
            {
                helper.PopulateUIVertex(ref vertex, i);
                var t = Across ? Mathf.InverseLerp(rect.xMin, rect.xMax, vertex.position.x) : Mathf.InverseLerp(rect.yMax, rect.yMin, vertex.position.y);
                vertex.color = (Color32)((Color)vertex.color * At(t));
                helper.SetUIVertex(vertex, i);
            }
        }
    }

    /// <summary>
    /// Letter spacing for legacy text (CSS letter-spacing, in em): spreads each line's
    /// glyphs apart and keeps the line where its alignment puts it. It also tells layouts the
    /// wider width. Unity no longer makes quads for spaces, so a wide gap between two glyphs
    /// counts as a space and gets its share too.
    /// </summary>
    [RequireComponent(typeof(Text))]
    public class Tracking : BaseMeshEffect, ILayoutElement
    {
        public float Em = 0.3f;
        Text text;
        static readonly List<UIVertex> stream = new List<UIVertex>();
        static readonly Regex tags = new Regex("<[^>]+>");

        Text Text => text != null ? text : text = GetComponent<Text>();

        public void Set(float em)
        {
            Em = em;
            graphic.SetVerticesDirty();
            LayoutRebuilder.MarkLayoutForRebuild((RectTransform)transform);
        }

        public override void ModifyMesh(VertexHelper helper)
        {
            if (!IsActive() || Em == 0 || helper.currentVertCount == 0)
            {
                return;
            }

            var label = Text;
            var step = Em * label.fontSize;
            stream.Clear();
            helper.GetUIVertexStream(stream);
            var quads = stream.Count / 6;

            if (quads == 0)
            {
                return;
            }

            // Lines: a quad whose middle is a line lower than the last one's starts a new line.
            var lineStart = 0;
            var lastY = float.NaN;
            var shift = 0f;
            var shifts = new float[quads];
            var lineOf = new int[quads];
            var lines = new List<(int start, int end, float spread)>();
            var lastRight = float.NaN;

            for (var q = 0; q < quads; q++)
            {
                float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;

                for (var k = 0; k < 6; k++)
                {
                    var p = stream[q * 6 + k].position;
                    minX = Mathf.Min(minX, p.x);
                    maxX = Mathf.Max(maxX, p.x);
                    minY = Mathf.Min(minY, p.y);
                    maxY = Mathf.Max(maxY, p.y);
                }

                var middle = (minY + maxY) * 0.5f;

                if (!float.IsNaN(lastY) && lastY - middle > label.fontSize * 0.6f)
                {
                    lines.Add((lineStart, q, shift));
                    lineStart = q;
                    shift = 0;
                    lastRight = float.NaN;
                }
                else if (q > lineStart)
                {
                    shift += step;

                    // A gap as wide as a space: one more step for it.
                    if (!float.IsNaN(lastRight) && minX - lastRight > label.fontSize * 0.18f)
                    {
                        shift += step;
                    }
                }

                lastY = float.IsNaN(lastY) || q == lineStart ? middle : lastY;
                lastRight = maxX;
                shifts[q] = shift;
            }

            lines.Add((lineStart, quads, shift));

            var align = label.alignment;
            var centre = align == TextAnchor.UpperCenter || align == TextAnchor.MiddleCenter || align == TextAnchor.LowerCenter;
            var right = align == TextAnchor.UpperRight || align == TextAnchor.MiddleRight || align == TextAnchor.LowerRight;

            foreach (var (start, end, spread) in lines)
            {
                var back = centre ? spread * 0.5f : right ? spread : 0;

                for (var q = start; q < end; q++)
                {
                    var dx = shifts[q] - back;

                    for (var k = 0; k < 6; k++)
                    {
                        var v = stream[q * 6 + k];
                        v.position.x += dx;
                        stream[q * 6 + k] = v;
                    }
                }
            }

            helper.Clear();
            helper.AddUIVertexTriangleStream(stream);
        }

        /// <summary>How much wider the tracking makes the text, as one line.</summary>
        float Extra()
        {
            var label = Text;

            if (label == null || string.IsNullOrEmpty(label.text))
            {
                return 0;
            }

            var plain = label.supportRichText ? tags.Replace(label.text, "") : label.text;
            plain = plain.Replace("\n", "");

            return Mathf.Max(0, plain.Length - 1) * Em * label.fontSize;
        }

        public void CalculateLayoutInputHorizontal()
        {
        }

        public void CalculateLayoutInputVertical()
        {
        }

        public float minWidth => -1;
        public float preferredWidth => Text != null ? Text.preferredWidth + Extra() : -1;
        public float flexibleWidth => -1;
        public float minHeight => -1;
        public float preferredHeight => -1;
        public float flexibleHeight => -1;
        public float maxWidth => -1;
        public float maxHeight => -1;
        public int layoutPriority => 2;
    }

    /// <summary>Turns round and round, a whole turn in `Period` seconds (the design's slow rings: 30–160 s).</summary>
    public class Spin : MonoBehaviour
    {
        public float Period = 60;

        void Update() => transform.Rotate(0, 0, -360f / Period * Time.unscaledDeltaTime);
    }

    /// <summary>Breathes: its alpha (and, if asked, its size) rises and falls once in `Period` seconds.</summary>
    public class Pulse : MonoBehaviour
    {
        public float Period = 5;
        public float Low = 0.5f;
        public float High = 0.95f;
        public float Grow;
        public float Offset;
        Graphic graphic;
        CanvasGroup group;

        void Awake()
        {
            graphic = GetComponent<Graphic>();
            group = GetComponent<CanvasGroup>();
        }

        void Update()
        {
            var t = 0.5f - 0.5f * Mathf.Cos((Time.unscaledTime + Offset) / Period * 2 * Mathf.PI);
            var alpha = Mathf.Lerp(Low, High, t);

            if (group != null)
            {
                group.alpha = alpha;
            }
            else if (graphic != null)
            {
                graphic.canvasRenderer.SetAlpha(alpha);
            }

            if (Grow != 0)
            {
                var s = 1 + Grow * t;
                transform.localScale = new Vector3(s, s, 1);
            }
        }
    }

    /// <summary>Floats up and down by `Rise` units once in `Period` seconds (the design's 6 s hover).</summary>
    public class Bob : MonoBehaviour
    {
        public float Period = 6;
        public float Rise = 10;
        /// <summary>Which way it rises (up, or sideways for a drifting band of fog).</summary>
        public Vector2 Way = Vector2.up;
        Vector2 home;
        bool homed;

        void Update()
        {
            var rect = (RectTransform)transform;

            if (!homed)
            {
                home = rect.anchoredPosition;
                homed = true;
            }

            rect.anchoredPosition = home + Way * (Rise * (0.5f - 0.5f * Mathf.Cos(Time.unscaledTime / Period * 2 * Mathf.PI)));
        }
    }

    /// <summary>A mote drifting up and away, fading in and out, again and again.</summary>
    public class Drift : MonoBehaviour
    {
        public float Period = 10;
        public float Delay;
        public Vector2 Way = new Vector2(-60, 140);
        Vector2 home;
        Graphic graphic;

        void Start()
        {
            home = ((RectTransform)transform).anchoredPosition;
            graphic = GetComponent<Graphic>();
        }

        void Update()
        {
            var t = Mathf.Repeat((Time.unscaledTime + Delay) / Period, 1);
            ((RectTransform)transform).anchoredPosition = home + Way * t;

            if (graphic != null)
            {
                graphic.canvasRenderer.SetAlpha(t < 0.15f ? t / 0.15f : 1 - (t - 0.15f) / 0.85f);
            }
        }
    }

    /// <summary>A window coming in: from nothing and 0.92 of its size to whole, in 0.4 s.</summary>
    public class Appear : MonoBehaviour
    {
        public float Time = 0.4f;
        public float From = 0.92f;
        float t;
        CanvasGroup group;

        void Awake()
        {
            group = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
            Step(0);
        }

        void Update()
        {
            if (t >= 1)
            {
                return;
            }

            Step(UnityEngine.Time.unscaledDeltaTime / Time);
        }

        void Step(float dt)
        {
            t = Mathf.Clamp01(t + dt);
            var ease = 1 - (1 - t) * (1 - t) * (1 - t);
            group.alpha = ease;
            var s = Mathf.Lerp(From, 1, ease);
            transform.localScale = new Vector3(s, s, 1);
        }
    }

    /// <summary>
    /// A hexagon button: its face lightens and its glow grows when pointed at, it can be
    /// turned off (greyed, not clickable), and — for what cannot be undone — it can ask to be
    /// held: then it fills from left to right while held (the mouse, or a key) and acts when full.
    /// </summary>
    public class HexButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
    {
        public Image Face;
        public Image Glow;
        public Image Fill;
        public Text Words;
        public Action Click;
        /// <summary>Seconds to hold (0: a click acts at once).</summary>
        public float Hold;
        /// <summary>A key that also holds it (none: only the mouse).</summary>
        public Key HoldKey = Key.None;
        float lit;
        float held;
        bool over;
        bool down;
        float glowRest;

        public void Build(Image face, Image glow, Text words, Action click, float glowAlpha)
        {
            Face = face;
            Glow = glow;
            Words = words;
            Click = click;
            glowRest = glowAlpha;
        }

        public void OnPointerEnter(PointerEventData _) => over = true;

        public void OnPointerExit(PointerEventData _)
        {
            over = false;
            down = false;
        }

        public void OnPointerDown(PointerEventData data) => down = data.button == PointerEventData.InputButton.Left;
        public void OnPointerUp(PointerEventData _) => down = false;

        public void OnPointerClick(PointerEventData data)
        {
            if (Hold <= 0 && data.button == PointerEventData.InputButton.Left)
            {
                Click?.Invoke();
            }
        }

        void Update()
        {
            lit = Mathf.MoveTowards(lit, over ? 1 : 0, UnityEngine.Time.unscaledDeltaTime * 8);
            var tint = Mathf.Lerp(0.88f, 1.08f, lit);
            Face.color = new Color(tint, tint, tint, 1);

            if (Glow != null)
            {
                Glow.color = new Color(Glow.color.r, Glow.color.g, Glow.color.b, glowRest * (1 + 0.8f * lit));
            }

            if (Hold > 0)
            {
                var holding = down || (HoldKey != Key.None && Keyboard.current != null && Keyboard.current[HoldKey].isPressed);
                held = holding ? held + UnityEngine.Time.unscaledDeltaTime / Hold : Mathf.MoveTowards(held, 0, UnityEngine.Time.unscaledDeltaTime * 3);

                if (Fill != null)
                {
                    Fill.fillAmount = Mathf.Clamp01(held);
                }

                if (held >= 1)
                {
                    held = 0;
                    down = false;
                    Click?.Invoke();
                }
            }
        }
    }

    /// <summary>
    /// A filter or tab button (the design's .tb): a thin edge and faint glass; chosen, the
    /// edge and the words turn gold. Lights a little when pointed at.
    /// </summary>
    public class TabButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public Image Back;
        public Image Edge;
        public Text Words;
        public bool On;
        public Action Click;
        bool over;

        public void OnPointerEnter(PointerEventData _) => over = true;
        public void OnPointerExit(PointerEventData _) => over = false;

        public void OnPointerClick(PointerEventData data)
        {
            if (data.button == PointerEventData.InputButton.Left)
            {
                Click?.Invoke();
            }
        }

        public void Paint()
        {
            Back.color = On ? UiKit.GoldLit.A(over ? 0.16f : 0.1f) : new Color(234 / 255f, 240 / 255f, 242 / 255f, over ? 0.07f : 0.03f);
            Edge.color = On ? UiKit.GoldLit.A(0.6f) : new Color(234 / 255f, 240 / 255f, 242 / 255f, over ? 0.28f : 0.14f);
            Words.color = On ? UiKit.GoldLit : over ? UiKit.Text : UiKit.Text2;
        }

        bool painted;
        bool wasOver;

        void Update()
        {
            if (!painted || over != wasOver)
            {
                painted = true;
                wasOver = over;
                Paint();
            }
        }
    }

    /// <summary>A whole clickable area (a list row, a card) that tells when it is pointed at.</summary>
    public class Clickable : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public Action Click;
        public Action RightClick;
        public Action<bool> Pointed;

        public void OnPointerEnter(PointerEventData _) => Pointed?.Invoke(true);
        public void OnPointerExit(PointerEventData _) => Pointed?.Invoke(false);

        public void OnPointerClick(PointerEventData data)
        {
            if (data.button == PointerEventData.InputButton.Left)
            {
                Click?.Invoke();
            }
            else if (data.button == PointerEventData.InputButton.Right)
            {
                RightClick?.Invoke();
            }
        }
    }
}
