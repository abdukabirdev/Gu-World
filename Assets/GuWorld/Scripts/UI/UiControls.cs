using GuWorld.Actors;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GuWorld.UI
{
    /// <summary>The keys page (from the pause menu): every action with its key; click one, press a new key.</summary>
    public partial class Ui
    {
        string rebinding;

        void ControlsPage()
        {
            var body = Frame("键", "Клавиши", rebinding != null ? "Нажмите новую клавишу (Esc — отмена)" : "Нажмите на действие, чтобы сменить клавишу", false, () => Show(Page.Pause), null, new[] { ("Esc", "Назад") });
            var left = Column(body, 0.12f, 0.5f, 4);
            var right = Column(body, 0.54f, 0.88f, 4);
            var half = (Bindings.All.Length + 1) / 2;

            for (var i = 0; i < Bindings.All.Length; i++)
            {
                var action = Bindings.All[i];
                var id = action.Id;
                var column = i < half ? left : right;
                var waiting = rebinding == id;
                ListLine(column, waiting ? "…" : Bindings.Label(action.Key), action.Name, action.Key != action.Default ? "изменено" : "", waiting ? UiKit.GoldText : UiKit.Text, waiting, () => { rebinding = id; Redraw(); });
            }

            Gap(right, 12);
            UiKit.HexButton("Вернуть как было", right, () => { Bindings.Reset(); rebinding = null; Redraw(); }, UiKit.HexKind.Ghost, 12, 40);
            Para("ЛКМ — удар, ПКМ — перекат, колесо мыши — держать цель, WASD — идти: они не меняются.", right, 13, UiKit.Muted);
        }

        /// <summary>The keys page's own keys: a new key while waiting for one, Esc back.</summary>
        void ControlsKeys()
        {
            if (rebinding != null)
            {
                var key = Bindings.Pressed();

                if (key == Key.Escape)
                {
                    rebinding = null;
                    Redraw();
                }
                else if (key != Key.None)
                {
                    Bindings.Bind(rebinding, key);
                    rebinding = null;
                    Redraw();
                }

                return;
            }

            if (Keys.Down(Key.Escape))
            {
                Show(Page.Pause);
            }
        }
    }
}
