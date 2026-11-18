using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GuWorld.UI
{
    /// <summary>
    /// The first thing seen (ROADMAP 2.2): the void, the 蛊 brush mark coming out of it in gold, then the
    /// name, then "press any key" breathing; any key or click (or eight seconds) goes on to the main menu.
    /// And the small "saving…" in the corner whenever the game is saved (ROADMAP 2.6).
    /// </summary>
    public partial class Ui
    {
        void SplashPage()
        {
            var stage = UiKit.Stage(pageRoot);
            var back = UiKit.Image("Void", stage, UiKit.Void, UiKit.Plain);
            back.rectTransform.Fill();
            back.raycastTarget = true;
            UiKit.Cloud(stage, new Vector2(0.5f, 0.55f), new Vector2(1400, 900), UiKit.Gold.A(0.06f));
            UiKit.StarField(stage, 0.35f);

            var mark = UiKit.Brush("蛊", stage, 220, UiKit.Gold);
            mark.rectTransform.anchorMin = mark.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            mark.rectTransform.anchoredPosition = new Vector2(0, 90);
            UiKit.HaloAt(stage, UiKit.Gold.A(0.14f), new Vector2(0, 90), 420, new Vector2(0.5f, 0.5f)).transform.SetSiblingIndex(mark.transform.GetSiblingIndex());
            Appear(mark.gameObject, 0.2f, 1.4f);
            UiKit.Orbit(stage, 360, UiKit.Gold.A(0.16f), 90, 8).rectTransform.anchoredPosition = new Vector2(0, 90);

            var name = UiKit.Disp("GU WORLD", stage, 64, Color.white, TextAnchor.MiddleCenter);
            name.Track(0.3f);
            name.rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(0, -90), new Vector2(900, 80));
            Appear(name.gameObject, 1.3f, 1.2f);
            var motto = UiKit.Lbl("Лунная тень Цинмао", stage, 12, UiKit.LabelGold, 0.5f, TextAnchor.MiddleCenter);
            motto.rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(0, -140), new Vector2(900, 20));
            Appear(motto.gameObject, 1.9f, 1.2f);

            var press = UiKit.Lbl("Нажмите любую клавишу", stage, 12, UiKit.Muted, 0.4f, TextAnchor.MiddleCenter);
            press.rectTransform.At(new Vector2(0.5f, 0), new Vector2(0, 90), new Vector2(900, 20));
            var holder = UiKit.Rect("Press", stage);
            press.transform.SetParent(holder, true);
            holder.gameObject.AddComponent<CanvasGroup>().alpha = 0;
            var breathe = holder.gameObject.AddComponent<Pulse>();
            breathe.Period = 2.6f;
            breathe.Low = 0.25f;
            breathe.High = 0.9f;
            Appear(holder.gameObject, 2.8f, 0.6f);

            stage.gameObject.AddComponent<SplashWait>().Done = () => Show(Page.Title);
        }

        /// <summary>Fades a part in after a delay (its own CanvasGroup).</summary>
        static void Appear(GameObject part, float delay, float seconds)
        {
            var group = part.GetComponent<CanvasGroup>();

            if (group == null)
            {
                group = part.AddComponent<CanvasGroup>();
            }

            group.alpha = 0;
            var fade = part.AddComponent<FadeInAfter>();
            fade.Delay = delay;
            fade.Seconds = seconds;
        }

        // ------------------------------------------------------------------ Saving…

        CanvasGroup savingMark;
        float savingAge = 99;

        /// <summary>The game was saved: "saving…" shows in the corner for a moment.</summary>
        public void Saved()
        {
            if (savingMark == null)
            {
                var root = UiKit.Rect("Saving", topCanvas.transform);
                root.anchorMin = root.anchorMax = new Vector2(1, 1);
                root.pivot = new Vector2(1, 1);
                root.anchoredPosition = new Vector2(-24, -20);
                root.sizeDelta = new Vector2(200, 28);
                savingMark = root.gameObject.AddComponent<CanvasGroup>();
                savingMark.blocksRaycasts = false;
                var ring = UiKit.Orbit(root, 18, UiKit.Gold.A(0.8f), 1.2f, 4);
                ring.rectTransform.anchorMin = ring.rectTransform.anchorMax = new Vector2(1, 0.5f);
                ring.rectTransform.anchoredPosition = new Vector2(-12, 0);
                var words = UiKit.Lbl("Сохранение", root, 11, UiKit.LabelGold, 0.3f, TextAnchor.MiddleRight).Shadowed();
                words.rectTransform.Place(Vector2.zero, Vector2.one, 0, 0, 30, 0);
            }

            savingAge = 0;
        }

        void TickSaving()
        {
            if (savingMark == null)
            {
                return;
            }

            savingAge += Time.unscaledDeltaTime;
            savingMark.alpha = Mathf.Clamp01(Mathf.Min(savingAge / 0.2f, (1.4f - savingAge) / 0.5f));
        }
    }

    /// <summary>Waits on the splash for any key or click (or eight seconds), then goes on.</summary>
    public class SplashWait : MonoBehaviour
    {
        public System.Action Done;
        float age;

        void Update()
        {
            age += Time.unscaledDeltaTime;
            var pressed = (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);

            if ((pressed && age > 0.3f) || age > 8)
            {
                var done = Done;
                Done = null;
                done?.Invoke();
            }
        }
    }

    /// <summary>Fades its CanvasGroup from nothing to whole after a delay (a Pulse on it waits till then).</summary>
    public class FadeInAfter : MonoBehaviour
    {
        public float Delay;
        public float Seconds = 1;
        CanvasGroup group;
        Pulse pulse;
        float age;

        void Start()
        {
            group = GetComponent<CanvasGroup>();
            pulse = GetComponent<Pulse>();

            if (pulse != null)
            {
                pulse.enabled = false;
            }
        }

        void Update()
        {
            age += Time.unscaledDeltaTime;
            var share = Mathf.Clamp01((age - Delay) / Mathf.Max(0.01f, Seconds));

            if (group != null)
            {
                group.alpha = share;
            }

            if (share >= 1)
            {
                if (pulse != null)
                {
                    pulse.enabled = true;
                }

                Destroy(this);
            }
        }
    }
}
