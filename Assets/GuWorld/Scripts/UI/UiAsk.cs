using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GuWorld.UI
{
    /// <summary>The topmost question, while one is open: the keys of what lies under it wait (MenuKeys, PageKeys, PageNav, the Ui's own).</summary>
    public static class Modal
    {
        public static Transform Top;

        /// <summary>The frame a question was answered in: the key that answered it is not taken again by what lay under it.</summary>
        public static int ClosedAt = -1;

        public static bool Open => Top != null || ClosedAt == Time.frameCount;

        /// <summary>Whether a question stands over this part of the interface (or was answered this very frame).</summary>
        public static bool Blocks(Transform under) => (Top != null && under != null && !under.IsChildOf(Top)) || ClosedAt == Time.frameCount;
    }

    /// <summary>A question's own keys: Esc — no; Enter — yes (an ordinary question only: what cannot be undone is held).</summary>
    public class AskKeys : MonoBehaviour
    {
        public Action No, Yes;
        int born;

        void Awake() => born = Time.frameCount;

        void Update()
        {
            var keyboard = Keyboard.current;

            if (keyboard == null || Time.frameCount == born)
            {
                return;
            }

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                No?.Invoke();
            }
            else if (Yes != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame))
            {
                Yes();
            }
        }
    }

    public partial class Ui
    {
        RectTransform asking;

        /// <summary>
        /// A question over what is open (UI-Overlays.dc.html): its kind, a title, what it means, and two answers —
        /// «Нет» (Esc) and the yes (Enter); for what cannot be undone, «Отмена» (Esc) and a yes held for 1.2 s.
        /// What lies under it waits for the answer.
        /// </summary>
        public void Ask(string title, string text, string yes, Action onYes, bool irreversible = false)
        {
            Unask();
            var stage = UiKit.Stage(pageRoot);
            asking = stage;
            Modal.Top = stage;
            var dim = UiKit.Image("Dim", stage, new Color(3 / 255f, 4 / 255f, 10 / 255f, 0.72f), UiKit.Plain);
            dim.rectTransform.Fill();
            dim.raycastTarget = true;
            var card = UiKit.Panel("Question", stage).rectTransform;
            card.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(640, 300));
            var inside = UiKit.Rect("Inside", card);
            inside.Fill();
            inside.Stack<VerticalLayoutGroup>(10, new RectOffset(44, 44, 34, 30), TextAnchor.UpperCenter);
            UiKit.Lbl(irreversible ? "Подтверждение · необратимое" : "Подтверждение", inside, 11, irreversible ? UiKit.Danger : UiKit.LabelGold, 0.3f, TextAnchor.MiddleCenter).Size(16);
            UiKit.Disp(title, inside, 30, UiKit.Text, TextAnchor.MiddleCenter).Size(40);
            var words = UiKit.Label(text, inside, Read(15), UiKit.Text2, TextAnchor.UpperCenter);
            words.horizontalOverflow = HorizontalWrapMode.Wrap;
            words.Size(64);
            var row = UiKit.Rect("Answers", inside);
            row.Size(56);
            var answers = row.Stack<HorizontalLayoutGroup>(18, null, TextAnchor.MiddleCenter);
            answers.childForceExpandWidth = true;
            Action no = Unask;
            Action agreed = () =>
            {
                Unask();
                onYes?.Invoke();
            };
            UiKit.HexButton(irreversible ? "Отмена" : "Нет", row, no, UiKit.HexKind.Ghost, 13, 52, "Esc");
            var button = UiKit.HexButton(irreversible ? "Удерживайте — " + yes.ToLowerInvariant() : yes, row, agreed, irreversible ? UiKit.HexKind.Solid : UiKit.HexKind.Main, 13, 52, irreversible ? null : "Enter");

            if (irreversible)
            {
                button.Held(1.2f, Key.Enter);
            }

            var keys = stage.gameObject.AddComponent<AskKeys>();
            keys.No = no;
            keys.Yes = irreversible ? null : agreed;
            UiSound.Hover();
        }

        /// <summary>The question goes (answered, or its page redrawn under it).</summary>
        public void Unask()
        {
            if (asking != null)
            {
                Destroy(asking.gameObject);
                Modal.ClosedAt = Time.frameCount;
            }

            asking = null;
            Modal.Top = null;
        }
    }
}
