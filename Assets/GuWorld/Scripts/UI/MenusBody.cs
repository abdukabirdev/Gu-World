using System;
using System.Collections.Generic;
using GuWorld.Combat;
using GuWorld.Render;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GuWorld.UI
{
    /// <summary>Passes a drag over a picture on (the body page turns its figure by it).</summary>
    public class Dragger : MonoBehaviour, IDragHandler
    {
        public Action<Vector2> Dragged;

        public void OnDrag(PointerEventData data) => Dragged?.Invoke(data.delta);
    }

    /// <summary>
    /// The body page (B, UI-Body.dc.html): the hero's figure in a slow gold ring with the
    /// body's parts in colour — where a blow is deadly and where it only hurts — turned by
    /// dragging or the three views; at the right every part with the share of health a blow
    /// there takes, the vital ones marked, and how the strength of a blow against the
    /// toughness of the body changes it all.
    /// </summary>
    public partial class Menus
    {
        BodyPart? bodyChosen;
        float bodyYaw;

        void BodyPage()
        {
            var body = BookFrame("body", "Тело", Page.Body);

            // The figure.
            var figure = UiKit.Rect("Figure", body);
            figure.TopLeft(84, -12, 520, 720);
            var studio = BodyStudio.Open(host.Parts, host.Look);
            studio.Turn(bodyYaw);
            studio.Choose(bodyChosen);
            var views = UiKit.Rect("Views", figure);
            views.Place(new Vector2(0, 1), Vector2.one, 0, -34, 0, 0);
            var viewRow = views.Stack<HorizontalLayoutGroup>(6, null, TextAnchor.MiddleCenter);
            viewRow.childForceExpandWidth = false;

            foreach (var (label, yaw) in new[] { ("Спереди", 0f), ("Сбоку", 90f), ("Сзади", 180f) })
            {
                var y = yaw;
                UiKit.Tb(label, views, Mathf.Abs(Mathf.DeltaAngle(bodyYaw, yaw)) < 1, () =>
                {
                    bodyYaw = y;
                    Show(Page.Body);
                }).Size(34, 110, 0);
            }

            var ring = UiKit.Rect("Ring", figure);
            ring.TopLeft(60, 90, 400, 400);
            UiKit.Orbit(ring, 400, UiKit.Gold.A(0.12f), 160, 0);
            var pad = UiKit.Image("Pad", figure, UiKit.Essence.A(0.15f), UiKit.Radial);
            pad.rectTransform.TopLeft(260 - 150, 640, 300, 40);
            var frame = UiKit.Rect("Frame", figure);
            frame.TopLeft(0, 56, 520, 610);
            var picture = UiKit.Rect("Picture", frame).gameObject.AddComponent<RawImage>();
            picture.texture = studio.Texture;
            picture.rectTransform.Fill();
            var fit = picture.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fit.aspectRatio = 0.75f;
            picture.gameObject.AddComponent<Dragger>().Dragged = delta =>
            {
                bodyYaw -= delta.x * 0.5f;
                studio.Turn(bodyYaw);
            };

            var legend = UiKit.Rect("Legend", figure);
            legend.Place(Vector2.zero, new Vector2(1, 0), 0, 0, 0, -20);
            var legendRow = legend.Stack<HorizontalLayoutGroup>(22, null, TextAnchor.MiddleCenter);
            legendRow.childForceExpandWidth = false;
            LegendChip(legend, UiKit.Hex(0xF0604E), "Уязвимое место");
            LegendChip(legend, UiKit.Hex(0xF0C868, 0.6f), "Обычная рана");

            // The parts.
            var right = UiKit.Rect("Parts", body);
            right.Place(new Vector2(1, 0), Vector2.one, -560, 0, 0, 0);
            right.Stack<VerticalLayoutGroup>(20);
            var list = UiKit.Rect("List", right);
            list.Stack<VerticalLayoutGroup>(0);
            UiKit.Lbl("Доля здоровья за удар равной силы", list).Size(24);
            var seen = new HashSet<string>();
            var rows = 0;

            foreach (var info in Wounds.Parts)
            {
                if (seen.Add(info.Name))
                {
                    PartRow(list, info);
                    rows++;
                }
            }

            list.Size(24 + rows * 40);

            // Strength against toughness.
            var power = GuWorld.Cultivation.GuBook.Moonlight.Power * (1 + 0.6f * (host.Cultivator.Aperture.Rank - 1));
            var toughness = Mathf.Max(1, host.Health.Toughness);
            var panel = UiKit.Image("Strength", right, Color.white, UiKit.Plain);
            panel.Gradient(new Color(12 / 255f, 16 / 255f, 24 / 255f, 0.7f), new Color(5 / 255f, 7 / 255f, 12 / 255f, 0.82f));
            UiKit.Frame(panel.transform, UiKit.Gold.A(0.2f));
            panel.Size(-1).flexibleHeight = 1;
            var inside = panel.rectTransform.Stack<VerticalLayoutGroup>(8, new RectOffset(20, 20, 16, 14));
            inside.childControlHeight = true;
            var head = UiKit.Rect("Head", panel.transform);
            head.Size(32);
            UiKit.Lbl("Сила против крепости", head).rectTransform.Fill();
            UiKit.Disp($"×{Wounds.Scale(1, 1):0.00}", head, 30, UiKit.GoldLit, TextAnchor.MiddleRight).rectTransform.Fill();

            // The scale: green where blows do not bite, gold at one, red where wounds go deeper; marks for the table's ratios.
            var scale = UiKit.Rect("Scale", panel.transform);
            scale.Size(30);
            var line = UiKit.Image("Line", scale, Color.white, UiKit.Plain);
            line.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(1, 0.5f), 0, -1, 0, -1);
            line.Gradient(UiKit.Essence, UiKit.Danger, true, UiKit.Hex(0xF0C868), 0.5f);

            foreach (var ratio in new[] { 0.25f, 0.5f, 1f, 2f, 4f })
            {
                var at = Mathf.InverseLerp(Mathf.Log(0.25f), Mathf.Log(4), Mathf.Log(ratio));
                var tick = UiKit.Image("Tick", scale, ratio == 1 ? UiKit.GoldLit : new Color(234 / 255f, 240 / 255f, 242 / 255f, 0.5f), ratio == 1 ? UiKit.Diamond : UiKit.Plain);
                tick.rectTransform.At(new Vector2(at, 0.5f), Vector2.zero, ratio == 1 ? new Vector2(14, 14) : new Vector2(1, 10));
                var value = Wounds.Scale(ratio, 1);
                var word = UiKit.Label(value <= 0 ? "мимо" : $"×{ratio:0.##} → ×{value:0.0}", scale, 10, UiKit.Muted, TextAnchor.UpperCenter);
                word.rectTransform.At(new Vector2(at, 0), new Vector2(0, -2), new Vector2(90, 14));
            }

            var about = UiKit.Label("Доли выше — для удара, равного крепости тела. Слабый удар по крепкому телу теряет почти всё, а ниже трети его " +
                                    "крепости не оставляет и царапины; удар много сильнее берёт больше своей доли.", panel.transform, 13, UiKit.Hex(0xA8B6B8), TextAnchor.UpperLeft);
            about.lineSpacing = 1.1f;
            about.Size(56);
            UiKit.Label($"Ваша крепость <color=#FFF1CF>{toughness:0}</color> · лунный клинок — сила <color=#FFF1CF>{power:0}</color>, дальше {GuWorld.Cultivation.GuBook.Moonlight.Reach:0} м вдвое слабее.\n" +
                        "Простые люди — около 45, ученики — 70, стражи — 140, наставники — 220, старейшины — 300.", panel.transform, 12, UiKit.Muted, TextAnchor.UpperLeft).Size(40);

            if (bodyChosen.HasValue)
            {
                var info = Wounds.Of(bodyChosen.Value);
                UiKit.Label($"<color=#{ColorUtility.ToHtmlStringRGB(info.Colour)}>{info.Name}</color> — {info.About}", panel.transform, 13, UiKit.Text, TextAnchor.UpperLeft).Size(36);
            }

            UiKit.Footer(bookStage, new[] { ("ЛКМ", "Повернуть фигуру"), ("Q+E", "Страница"), ("Esc", "Закрыть") });
        }

        static void LegendChip(Transform parent, Color colour, string name)
        {
            var item = UiKit.Rect(name, parent);
            var row = item.Stack<HorizontalLayoutGroup>(8, null, TextAnchor.MiddleLeft);
            row.childForceExpandWidth = false;
            item.Size(20);
            UiKit.Image("Chip", item, colour, UiKit.Plain).Size(10, 10, 0);
            UiKit.Label(name, item, 12, UiKit.Hex(0xA8B6B8)).Size(20);
        }

        /// <summary>A part's row: its colour, its name (and "уязвимое"), a bar of its share, the share in numbers; a click shows it on the figure.</summary>
        void PartRow(Transform parent, PartInfo info)
        {
            var part = info.Part;
            var chosen = bodyChosen.HasValue && Wounds.Of(bodyChosen.Value).Name == info.Name;
            var (row, _) = UiKit.ListRow(parent, 40, chosen, () =>
            {
                bodyChosen = chosen ? (BodyPart?)null : part;
                Show(Page.Body);
            });
            var line = row.Stack<HorizontalLayoutGroup>(14, new RectOffset(22, 6, 0, 0), TextAnchor.MiddleLeft);
            line.childForceExpandWidth = false;
            var rule = UiKit.Image("Rule", row, new Color(234 / 255f, 240 / 255f, 242 / 255f, 0.06f), UiKit.Plain);
            rule.rectTransform.Place(Vector2.zero, new Vector2(1, 0), 0, 0, 0, -1);
            rule.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var chip = UiKit.Image("Colour", row, info.Colour, UiKit.Plain);
            chip.Size(10, 12, 0);
            var name = UiKit.Label(info.Name + (info.Critical ? "   <size=10><color=#F08A74>У Я З В И М О Е</color></size>" : ""), row, 14, UiKit.Text);
            name.Size(40, -1, 1);
            var track = UiKit.Image("Track", row, new Color(234 / 255f, 240 / 255f, 242 / 255f, 0.08f), UiKit.Plain);
            track.Size(3, 120, 0);
            track.GetComponent<LayoutElement>().flexibleHeight = 0;
            var fill = UiKit.Image("Fill", track.transform, info.Colour, UiKit.Plain);
            fill.rectTransform.Place(Vector2.zero, new Vector2(Mathf.Clamp01(info.Max), 1));
            var share = UiKit.Disp($"{info.Min * 100:0}–{info.Max * 100:0}%", row, 20, info.Critical ? UiKit.Danger : UiKit.Text, TextAnchor.MiddleRight);
            share.Size(40, 80, 0);
        }
    }
}
