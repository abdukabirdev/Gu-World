using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GuWorld.UI
{
    /// <summary>
    /// The keyboard on a page (ROADMAP 2.1): the arrows go to the nearest control that way (Tab and Shift+Tab
    /// in reading order), Enter or Space presses it — a held button is held by Enter as long as it is down —,
    /// ← → move a slider; a text field takes the keys once Enter goes into it. The mouse pointing at a control
    /// chooses it too. A gold frame shows the chosen one once the keys have been used on the page. Keys the
    /// page answers itself (PageKeys) stay the page's. The choice outlives the page being built again (a
    /// choice made redraws it) by its place in reading order.
    /// </summary>
    public class PageNav : MonoBehaviour
    {
        /// <summary>A control: what it is and how it is pressed.</summary>
        sealed class Control
        {
            public RectTransform Rect;
            public HexButton Hex;
            public TabButton Tab;
            public Clickable Click;
            public Slider Slider;
            public InputField Field;
        }

        readonly List<Control> controls = new List<Control>();
        PageKeys pageKeys;
        RectTransform ring;
        int chosen = -1;
        int born;
        bool keyed;
        Vector2 lastMouse;
        HexButton holding;

        static Ui.Page rememberedPage = Ui.Page.None;
        static int rememberedIndex = -1;
        static bool rememberedKeyed;

        public Ui.Page Page;

        void Start()
        {
            born = Time.frameCount;
            pageKeys = transform.parent != null ? transform.parent.GetComponentInChildren<PageKeys>() : null;

            if (rememberedPage == Page && rememberedIndex >= 0)
            {
                chosen = rememberedIndex;
                keyed = rememberedKeyed;
            }

            lastMouse = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
        }

        void OnDestroy()
        {
            if (holding != null)
            {
                holding.HoldKey = Key.None;
            }

            rememberedPage = Page;
            rememberedIndex = chosen;
            rememberedKeyed = keyed;
        }

        /// <summary>The controls on the page now, in reading order (top to bottom, then left to right).</summary>
        void Gather()
        {
            controls.Clear();
            var root = transform.parent;

            if (root == null)
            {
                return;
            }

            foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(false))
            {
                Control control = null;

                switch (behaviour)
                {
                    case HexButton hex when hex.Click != null:
                        control = new Control { Hex = hex };
                        break;
                    case TabButton tab when tab.enabled && tab.Click != null:
                        control = new Control { Tab = tab };
                        break;
                    case Clickable click when click.Click != null:
                        control = new Control { Click = click };
                        break;
                    case Slider slider when slider.interactable:
                        control = new Control { Slider = slider };
                        break;
                    case InputField field when field.interactable:
                        control = new Control { Field = field };
                        break;
                }

                if (control != null && behaviour.transform is RectTransform rect && Visible(rect))
                {
                    control.Rect = rect;
                    controls.Add(control);
                }
            }

            controls.Sort((a, b) =>
            {
                var pa = Centre(a.Rect);
                var pb = Centre(b.Rect);

                // Rows first (a few pixels' slack), then across.
                return Mathf.Abs(pa.y - pb.y) > 8 ? pb.y.CompareTo(pa.y) : pa.x.CompareTo(pb.x);
            });
        }

        static bool Visible(RectTransform rect) => rect.gameObject.activeInHierarchy && rect.rect.width > 2 && rect.rect.height > 2;

        static readonly Vector3[] corners = new Vector3[4];

        static Vector2 Centre(RectTransform rect)
        {
            rect.GetWorldCorners(corners);

            return (corners[0] + corners[2]) * 0.5f;
        }

        static Rect ScreenRect(RectTransform rect)
        {
            rect.GetWorldCorners(corners);

            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }

        bool Own(Key key) => pageKeys != null && pageKeys.Has(key);

        void Update()
        {
            var keyboard = Keyboard.current;

            // Not the press that opened the page; nothing while a key is being bound (the settings' catcher takes it).
            if (keyboard == null || Time.frameCount == born || Modal.Blocks(transform) || (transform.parent != null && transform.parent.GetComponentInChildren<KeyCatcher>() != null))
            {
                return;
            }

            // A text field being typed in keeps every key; Enter or Esc leaves it (the field's own way).
            var focusedField = chosen >= 0 && chosen < controls.Count ? controls[chosen].Field : null;

            if (focusedField != null && focusedField.isFocused)
            {
                return;
            }

            Pointing();

            var step = Vector2.zero;
            var tab = keyboard.tabKey.wasPressedThisFrame;

            if (keyboard.upArrowKey.wasPressedThisFrame && !Own(Key.UpArrow))
            {
                step = Vector2.up;
            }
            else if (keyboard.downArrowKey.wasPressedThisFrame && !Own(Key.DownArrow))
            {
                step = Vector2.down;
            }
            else if (keyboard.leftArrowKey.wasPressedThisFrame && !Own(Key.LeftArrow))
            {
                step = Vector2.left;
            }
            else if (keyboard.rightArrowKey.wasPressedThisFrame && !Own(Key.RightArrow))
            {
                step = Vector2.right;
            }

            if (step != Vector2.zero || (tab && !Own(Key.Tab)))
            {
                Gather();

                if (controls.Count == 0)
                {
                    return;
                }

                var current = chosen >= 0 && chosen < controls.Count ? controls[chosen] : null;

                // ← → move a slider instead of leaving it.
                if (current?.Slider != null && step.y == 0 && step.x != 0 && keyed)
                {
                    var slider = current.Slider;
                    var by = slider.wholeNumbers ? 1 : (slider.maxValue - slider.minValue) / 20f;
                    slider.value = Mathf.Clamp(slider.value + by * step.x, slider.minValue, slider.maxValue);
                    UiSound.Hover();

                    return;
                }

                var first = !keyed;
                keyed = true;

                if (first || current == null)
                {
                    // The first press only shows where the keys are.
                    Choose(current == null ? 0 : chosen, true);
                }
                else if (tab)
                {
                    var back = keyboard.shiftKey.isPressed;
                    Choose((chosen + (back ? controls.Count - 1 : 1)) % controls.Count, true);
                }
                else
                {
                    Choose(Nearest(chosen, step), true);
                }

                Ring();

                return;
            }

            var enter = (keyboard.enterKey.wasPressedThisFrame && !Own(Key.Enter)) || (keyboard.numpadEnterKey.wasPressedThisFrame && !Own(Key.NumpadEnter)) ||
                        (keyboard.spaceKey.wasPressedThisFrame && !Own(Key.Space));

            if (enter && keyed && chosen >= 0)
            {
                Gather();

                if (chosen < controls.Count)
                {
                    Press(controls[chosen], keyboard.spaceKey.wasPressedThisFrame ? Key.Space : Key.Enter);
                }
            }

            if (keyed)
            {
                Ring();
            }
        }

        /// <summary>The mouse pointing at a control chooses it (quietly: the frame shows only once keys were used).</summary>
        void Pointing()
        {
            var mouse = Mouse.current;

            if (mouse == null)
            {
                return;
            }

            var at = mouse.position.ReadValue();

            if ((at - lastMouse).sqrMagnitude < 4)
            {
                return;
            }

            lastMouse = at;
            Gather();

            for (var i = controls.Count - 1; i >= 0; i--)
            {
                if (ScreenRect(controls[i].Rect).Contains(at))
                {
                    if (i != chosen)
                    {
                        Choose(i, false);
                    }

                    return;
                }
            }
        }

        /// <summary>The control nearest the chosen one in a direction: along the way, and what is off to the side counts double.</summary>
        int Nearest(int from, Vector2 way)
        {
            var origin = Centre(controls[from].Rect);
            var best = from;
            var bestScore = float.MaxValue;

            for (var i = 0; i < controls.Count; i++)
            {
                if (i == from)
                {
                    continue;
                }

                var offset = Centre(controls[i].Rect) - origin;
                var along = Vector2.Dot(offset, way);

                if (along < 4)
                {
                    continue;
                }

                var side = Mathf.Abs(Vector2.Dot(offset, new Vector2(-way.y, way.x)));
                var score = along + side * 2;

                if (score < bestScore)
                {
                    bestScore = score;
                    best = i;
                }
            }

            // Nothing that way: round to the other end of the reading order.
            if (best == from && way.y != 0)
            {
                best = way.y < 0 ? 0 : controls.Count - 1;
            }

            return best;
        }

        void Choose(int index, bool byKeys)
        {
            if (holding != null)
            {
                holding.HoldKey = Key.None;
                holding = null;
            }

            chosen = Mathf.Clamp(index, 0, controls.Count - 1);
            var control = controls[chosen];

            // A held button is held by Enter while it is the chosen one.
            if (control.Hex != null && control.Hex.Hold > 0)
            {
                holding = control.Hex;
                holding.HoldKey = Key.Enter;
            }

            if (byKeys)
            {
                UiSound.Hover();
                Reveal(control.Rect);
            }
        }

        void Press(Control control, Key with)
        {
            if (control.Hex != null)
            {
                // A held one fills while Enter is down (its own HoldKey); a plain one acts now.
                if (control.Hex.Hold <= 0)
                {
                    UiSound.Click();
                    control.Hex.Click?.Invoke();
                }
                else if (with == Key.Space)
                {
                    control.Hex.HoldKey = Key.Space;
                    holding = control.Hex;
                }
            }
            else if (control.Tab != null)
            {
                UiSound.Click();
                control.Tab.Click?.Invoke();
            }
            else if (control.Click != null)
            {
                UiSound.Click();
                control.Click.Click?.Invoke();
            }
            else if (control.Field != null)
            {
                control.Field.ActivateInputField();
            }
        }

        /// <summary>The gold frame on the chosen control.</summary>
        void Ring()
        {
            if (chosen < 0 || chosen >= controls.Count || controls[chosen].Rect == null)
            {
                Gather();

                if (chosen < 0 || chosen >= controls.Count)
                {
                    return;
                }
            }

            var target = controls[chosen].Rect;

            if (ring == null)
            {
                ring = UiKit.Rect("Focus", target);
                ring.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                UiKit.Frame(ring, UiKit.GoldLit.A(0.9f));
                UiKit.Glow(ring, UiKit.GoldLit.A(0.35f), 10);
            }

            if (ring.parent != target)
            {
                ring.SetParent(target, false);
            }

            ring.SetAsLastSibling();
            ring.Fill(-4);
        }

        /// <summary>A control in a scrolled list is scrolled into view.</summary>
        static void Reveal(RectTransform rect)
        {
            var scroll = rect.GetComponentInParent<ScrollRect>();

            if (scroll == null || scroll.content == null || scroll.viewport == null)
            {
                return;
            }

            var view = ScreenRect(scroll.viewport);
            var box = ScreenRect(rect);
            var scale = scroll.content.lossyScale.y > 0 ? scroll.content.lossyScale.y : 1;
            var shift = 0f;

            if (box.yMax > view.yMax)
            {
                shift = box.yMax - view.yMax + 12;
            }
            else if (box.yMin < view.yMin)
            {
                shift = box.yMin - view.yMin - 12;
            }

            if (Mathf.Abs(shift) > 0.5f)
            {
                var position = scroll.content.anchoredPosition;
                scroll.content.anchoredPosition = new Vector2(position.x, position.y - shift / scale);
            }
        }
    }
}
