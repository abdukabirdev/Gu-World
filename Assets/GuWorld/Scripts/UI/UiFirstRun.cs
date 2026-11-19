using GuWorld.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GuWorld.UI
{
    /// <summary>
    /// The first run (ROADMAP 2.1: no settings yet): after the splash, one page before the menu — the picture
    /// level the machine was given (Core.Machine) and the brightness, set by eye on three moon seals on black:
    /// the left one barely seen, the right one plainly. The interface is not under the picture's exposure, so
    /// the seals are drawn as bright as the exposure would make them.
    /// </summary>
    public partial class Ui
    {
        static readonly float[] SealGreys = { 0.035f, 0.075f, 0.16f };
        Image[] brightSeals;

        void FirstRunPage()
        {
            var stage = UiKit.Stage(pageRoot);
            UiKit.Backdrop(stage, "月", 560);
            NewGameHeader(stage, 0, "Перед первым шагом", "Уровень графики подобран под ваш компьютер. Осталась яркость: её проще поставить сейчас, на глаз.");
            var body = UiKit.Rect("Body", stage);
            body.Place(Vector2.zero, Vector2.one, 56, 120, 56, 200);
            var left = Column(body, 0, 0.44f, 8);
            var right = Column(body, 0.5f, 1, 10);

            UiKit.Lbl("Ваш компьютер", left, 11, UiKit.LabelGold).Size(16);
            Fact(left, "Видеокарта", SystemInfo.graphicsDeviceName);
            Fact(left, "Память", $"{SystemInfo.systemMemorySize / 1024f:0.#} ГБ · видео {SystemInfo.graphicsMemorySize / 1024f:0.#} ГБ");
            Fact(left, "Уровень графики", Quality.Name, UiKit.GoldText);
            Para("Его всегда можно сменить: Настройки → Изображение. Если кадров будет мало, игра сама подскажет уровень легче.", left, 13, UiKit.Muted);

            UiKit.Lbl("Яркость", right, 11, UiKit.LabelGold).Size(16);
            Para("Подвиньте ползунок так, чтобы левый знак был едва виден, а правый — хорошо.", right, 15, UiKit.Text2);
            var panel = UiKit.Image("Sample", right, Color.black, UiKit.Rounded);
            panel.Size(150);
            var row = UiKit.Rect("Seals", panel.transform);
            row.Fill();
            row.Stack<HorizontalLayoutGroup>(0, null, TextAnchor.MiddleCenter);
            brightSeals = new Image[SealGreys.Length];

            for (var i = 0; i < SealGreys.Length; i++)
            {
                var cell = UiKit.Rect("Seal", row);
                cell.Size(150, 160, 0);
                var disc = UiKit.Image("Moon", cell, Color.white, UiKit.Disc);
                disc.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(84, 84));
                brightSeals[i] = disc;
            }

            ShadeSeals();
            var holder = UiKit.Rect("Slider", right);
            holder.Size(48);
            UiKit.Slider(holder, 0.6f, 1.4f, Mathf.Clamp(Prefs.Brightness, 0.6f, 1.4f), v => $"{v * 100:0}%", v => { Prefs.Brightness = v; ShadeSeals(); });
            Para(Quality.Level >= 1 ? "Яркость — экспозиция картинки в мире." : "На этом уровне графики яркость не меняется (работает от «Среднего»).", right, 12, UiKit.Muted);

            var foot = UiKit.Rect("Foot", stage);
            foot.Place(new Vector2(0, 0), new Vector2(1, 0), 56, 50, 56, -100);
            foot.Stack<HorizontalLayoutGroup>(20, null, TextAnchor.MiddleLeft).childForceExpandWidth = false;
            UiKit.HexButton("Готово", foot, FinishFirstRun, UiKit.HexKind.Solid, 13, 50, "Enter").Size(50, 220, 0);
            UiKit.Footer(stage, new[] { ("Enter", "Готово"), ("Esc", "Пропустить") });
            var keys = stage.gameObject.AddComponent<PageKeys>();
            keys.On(Key.Enter, FinishFirstRun);
            keys.On(Key.Escape, FinishFirstRun);
        }

        /// <summary>The seals as bright as the exposure would make them (the interface itself is not under it).</summary>
        void ShadeSeals()
        {
            if (brightSeals == null)
            {
                return;
            }

            var exposure = Quality.Level >= 1 ? Mathf.Clamp(Prefs.Brightness, 0.6f, 1.4f) : 1;

            for (var i = 0; i < brightSeals.Length; i++)
            {
                if (brightSeals[i] != null)
                {
                    var grey = Mathf.Clamp01(SealGreys[i] * exposure);
                    brightSeals[i].color = new Color(grey, grey, grey * 1.05f, 1);
                }
            }
        }

        void FinishFirstRun()
        {
            Prefs.FirstRunDone = true;
            brightSeals = null;
            Show(Page.Title);
        }
    }
}
