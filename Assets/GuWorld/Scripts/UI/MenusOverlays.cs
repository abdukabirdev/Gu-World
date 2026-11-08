using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GuWorld.UI
{
    /// <summary>
    /// The windows over everything (UI-Overlays.dc.html): a confirmation — for what cannot be
    /// undone a wide panel with a warning sign whose red button must be held, for the rest a
    /// small one with "no" and "yes" — and "About the game" with the brush mark 蛊.
    /// </summary>
    public partial class Menus
    {
        /// <summary>Whether the confirmation's action cannot be undone (then it is held, not clicked).</summary>
        bool confirmHold = true;

        void Confirm()
        {
            var stage = UiKit.Stage(pageRoot);
            var dim = UiKit.Image("Dim", stage, new Color(3 / 255f, 4 / 255f, 10 / 255f, 0.82f), UiKit.Plain);
            dim.rectTransform.Fill();
            dim.raycastTarget = true;
            UiKit.StarField(stage, 0.4f);
            var width = confirmHold ? 600f : 380f;
            var card = UiKit.Panel("Confirm", stage);
            card.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(width, confirmHold ? 270 : 210));
            card.gameObject.AddComponent<Appear>();
            UiKit.Lbl(confirmHold ? "Подтверждение · необратимое" : "Подтверждение", card.transform, 10, UiKit.Hex(0x4E5E61), 0.3f)
                .rectTransform.Place(new Vector2(0, 1), Vector2.one, 0, 4, 0, -18);
            var column = UiKit.Rect("Column", card.transform).Fill();
            column.Stack<VerticalLayoutGroup>(14, confirmHold ? new RectOffset(34, 34, 32, 30) : new RectOffset(26, 26, 26, 24));

            var head = UiKit.Rect("Head", column);
            var headRow = head.Stack<HorizontalLayoutGroup>(14, null, TextAnchor.MiddleLeft);
            headRow.childForceExpandWidth = false;
            head.Size(38);

            if (confirmHold)
            {
                var warn = UiKit.Image("Warn", head, UiKit.Danger, UiKit.RingLine(30));
                warn.Size(30, 30, 0);
                UiKit.Disp("!", warn.transform, 22, UiKit.Danger, TextAnchor.MiddleCenter).rectTransform.Fill();
            }

            UiKit.Disp(confirmTitle, head, confirmHold ? 34 : 28).Size(38);
            var text = UiKit.Label(confirmText, column, confirmHold ? 15 : 14, UiKit.Hex(0xA8B6B8), TextAnchor.UpperLeft);
            text.lineSpacing = 1.15f;
            text.Size(-1).flexibleHeight = 1;

            var buttons = UiKit.Rect("Buttons", column);
            var row = buttons.Stack<HorizontalLayoutGroup>(12, null, TextAnchor.MiddleLeft);
            row.childForceExpandWidth = false;
            buttons.Size(48);

            if (confirmHold)
            {
                UiKit.HexButton("Отмена", buttons, () => Show(confirmBack), UiKit.HexKind.Ghost, 12, 48, "Esc").Size(48, 190, 0);
                UiKit.HexButton($"Удерживайте — {confirmButton}", buttons, () => confirmAction?.Invoke(), UiKit.HexKind.Bad, 12, 48)
                    .Held(1.2f, Key.Space).Size(48, -1, 1);
            }
            else
            {
                UiKit.HexButton("Нет", buttons, () => Show(confirmBack), UiKit.HexKind.Ghost, 12, 42).Size(42, 120, 0);
                UiKit.HexButton("Да", buttons, () => confirmAction?.Invoke(), UiKit.HexKind.Solid, 12, 42).Size(42, 120, 0);
                var keys = stage.gameObject.AddComponent<PageKeys>();
                keys.On(Key.Enter, () => confirmAction?.Invoke());
            }

            UiKit.Footer(stage, confirmHold ? new[] { ("Пробел", "Удерживать"), ("Esc", "Отмена") } : new[] { ("Enter", "Да"), ("Esc", "Нет") });
        }

        void About()
        {
            var stage = UiKit.Stage(pageRoot);
            UiKit.Backdrop(stage);
            var card = UiKit.Panel("About", stage);
            card.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700, 380));
            card.gameObject.AddComponent<Appear>();
            var mark = UiKit.Brush("蛊", card.transform, 96, UiKit.Gold);
            mark.rectTransform.anchorMin = mark.rectTransform.anchorMax = new Vector2(0, 1);
            mark.rectTransform.anchoredPosition = new Vector2(28 + 48, -24 - 52);
            var column = UiKit.Rect("Column", card.transform);
            column.Place(Vector2.zero, Vector2.one, 150, 24, 28, 24);
            column.Stack<VerticalLayoutGroup>(8);
            var name = UiKit.Disp("GU WORLD", column, 30);
            name.Track(0.1f);
            name.Size(32);
            UiKit.Lbl($"Версия {Version}", column, 12, UiKit.Muted, 0.2f).Size(16);
            UiKit.Label("Игра о мире Гу по мотивам романа «Преподобный Гу» (Reverend Insanity). Первая арка — гора Цинмао на Южной " +
                        "границе: деревня клана Гу Юэ, академия, первые Гу. Слова и герои — свои.", column, 13, UiKit.Hex(0xA8B6B8), TextAnchor.UpperLeft).Size(66);
            UiKit.Lbl("Авторы и источники", column).Size(22);
            UiKit.Label("Люди — из базовой модели MakeHuman (CC0). Шрифты — Cormorant Garamond, Inter Tight, Ma Shan Zheng, Zhi Mang Xing " +
                        "(Google Fonts, OFL). Движок — Unity 6, Universal Render Pipeline.", column, 13, UiKit.Hex(0xA8B6B8), TextAnchor.UpperLeft).Size(66);
            var buttons = UiKit.Rect("Buttons", column);
            var row = buttons.Stack<HorizontalLayoutGroup>(10, null, TextAnchor.MiddleRight);
            row.childForceExpandWidth = false;
            buttons.Size(44);
            UiKit.HexButton("Назад", buttons, () => Show(Page.Title), UiKit.HexKind.Ghost, 12, 42).Size(42, 150, 0);
            UiKit.Footer(stage, new[] { ("Esc", "Назад") });
        }
    }
}
