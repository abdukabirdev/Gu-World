using GuWorld.Cultivation;
using UnityEngine;
using UnityEngine.UI;

namespace GuWorld.UI
{
    /// <summary>
    /// A Gu's slot on the HUD's bar (UI-HUD.dc.html's .slot): an octagon 64 across, a dark
    /// round glow inside a thin edge; its key at the top left, what one use costs at the foot
    /// right, the rank's strip under it, its mark in the middle; a shade with a light top
    /// edge rising from the foot while it rests; its name in small spaced capitals under it,
    /// and under that a hairline of how tired it is (amber, red, pulsing red once using it
    /// means forcing it). A held Gu and the chosen slot glow; the chosen one is framed in light.
    /// </summary>
    public class GuSlot
    {
        public const float Width = 64;
        public const float Height = 64;

        public RectTransform Root;
        Image glow, back, frame, icon, shade, shadeEdge, strip, tiredFill;
        RectTransform tiredBar;
        Text key, cost, name;
        float size;

        static readonly Color Tiring = UiKit.Hex(0xF0C868);
        static readonly Color Spent = UiKit.Hex(0xF08A74);

        public static GuSlot Build(Transform parent, string keyName, float size = Width)
        {
            var slot = new GuSlot { size = size };
            var root = UiKit.Rect("Gu Slot " + keyName, parent);
            root.sizeDelta = new Vector2(size, size);
            slot.Root = root;

            slot.glow = UiKit.Image("Glow", root, Color.clear, UiKit.GlowSprite);
            slot.glow.rectTransform.Fill(-14);
            slot.glow.pixelsPerUnitMultiplier = 32f / 14;
            slot.back = UiKit.Image("Back", root, Color.white, UiKit.Octagon);
            slot.back.rectTransform.Fill();
            slot.icon = UiKit.Icon(null, root, size * 0.62f, Color.white);
            slot.icon.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one * size * 0.62f);
            slot.shade = UiKit.Image("Rest", root, new Color(3 / 255f, 4 / 255f, 10 / 255f, 0.7f), UiKit.Octagon);
            slot.shade.rectTransform.Place(Vector2.zero, new Vector2(1, 0));
            slot.shade.type = Image.Type.Filled;
            slot.shade.fillMethod = Image.FillMethod.Vertical;
            slot.shade.fillOrigin = (int)Image.OriginVertical.Bottom;
            slot.shade.rectTransform.Fill();
            slot.shade.fillAmount = 0;
            slot.shadeEdge = UiKit.Image("Rest Edge", root, UiKit.GoldLit.A(0.5f), UiKit.Plain);
            slot.shadeEdge.rectTransform.Place(new Vector2(0, 0), new Vector2(1, 0), 2, 0, 2, -1);
            slot.shadeEdge.enabled = false;
            slot.frame = UiKit.Image("Edge", root, UiKit.Line, UiKit.OctagonLine);
            slot.frame.rectTransform.Fill();
            slot.strip = UiKit.Image("Rank", root, Color.clear, UiKit.Plain);
            slot.strip.rectTransform.Place(new Vector2(0, 0), new Vector2(1, 0), 9, 0, 9, -2);

            slot.key = UiKit.Label(keyName, root, 11, UiKit.Text2, TextAnchor.UpperLeft, FontStyle.Bold).Shadowed();
            slot.key.rectTransform.Place(Vector2.zero, Vector2.one, 6, 0, 0, 3);
            slot.cost = UiKit.Label("", root, 10, UiKit.EssenceLit, TextAnchor.LowerRight).Shadowed();
            slot.cost.rectTransform.Place(Vector2.zero, Vector2.one, 0, 3, 6, 0);
            slot.name = UiKit.Label("", root, 9, UiKit.Muted, TextAnchor.UpperCenter).Shadowed();
            slot.name.Track(0.14f);
            slot.name.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), -60, -18, -60, 4);

            var bar = UiKit.Image("Tired", root, new Color(3 / 255f, 4 / 255f, 10 / 255f, 0.65f), UiKit.Plain);
            bar.rectTransform.Place(new Vector2(0, 0), new Vector2(1, 0), 8, -22, 8, 20);
            slot.tiredBar = bar.rectTransform;
            slot.tiredFill = UiKit.Image("Fill", bar.transform, Tiring, UiKit.Plain);
            slot.tiredFill.rectTransform.Place(Vector2.zero, new Vector2(0, 1));

            return slot;
        }

        /// <summary>A slot with nothing in it: its key, and a word.</summary>
        public void Empty(string word, bool chosen)
        {
            Raise(chosen);
            back.color = new Color(1, 1, 1, chosen ? 0.95f : 0.75f);
            frame.color = chosen ? UiKit.GoldLit.A(0.6f) : new Color(200 / 255f, 230 / 255f, 240 / 255f, 0.1f);
            glow.color = chosen ? UiKit.GoldLit.A(0.25f) : Color.clear;
            icon.enabled = false;
            shade.fillAmount = 0;
            shadeEdge.enabled = false;
            strip.color = Color.clear;
            cost.text = "";
            name.text = word.ToUpperInvariant();
            name.color = UiKit.Hex(0x4A585B);
            tiredBar.gameObject.SetActive(false);
        }

        /// <summary>
        /// A Gu in the slot: `rest` how far through its own short rest (0 ready), `tired` how tired (of 100), `price` what a
        /// use costs this master (below 0: "сам", it works by itself), `affordable` whether the sea holds it, `held` whether
        /// it is up, `chosen` the slot picked (on the immortal's aperture, or the Gu in hand), `label` its name, shortened.
        /// </summary>
        public void Show(GuKind kind, float rest, float tired, float price, bool affordable, bool held, bool chosen, string label)
        {
            Raise(chosen);
            var rankColour = Aperture.RankColour(kind.Rank);
            back.color = Color.white;
            frame.color = chosen ? UiKit.GoldLit.A(0.6f) : held ? UiKit.EssenceLit.A(0.7f) : new Color(200 / 255f, 230 / 255f, 240 / 255f, 0.16f);
            strip.color = rankColour;
            icon.enabled = true;
            icon.sprite = Icons.Get(kind.Icon);
            icon.color = affordable || held ? Icons.Tint(kind.Icon) : UiKit.Muted;
            icon.canvasRenderer.SetAlpha(rest > 0 ? 0.7f : 1);
            shade.fillAmount = Mathf.Clamp01(rest);
            shadeEdge.enabled = rest > 0;

            if (rest > 0)
            {
                shadeEdge.rectTransform.anchorMin = new Vector2(0, Mathf.Clamp01(rest));
                shadeEdge.rectTransform.anchorMax = new Vector2(1, Mathf.Clamp01(rest));
                shadeEdge.rectTransform.offsetMin = new Vector2(2, -0.5f);
                shadeEdge.rectTransform.offsetMax = new Vector2(-2, 0.5f);
            }

            cost.text = price >= 0 ? $"{price:0.#}%" : price > -1.5f ? "сам" : "";
            cost.color = affordable ? UiKit.EssenceLit : UiKit.Danger;
            name.text = (held ? "● " : "") + label.ToUpperInvariant();
            name.color = held ? UiKit.EssenceLit : chosen ? UiKit.GoldLit : UiKit.Muted;

            // The pulse of a Gu held up, or forced past its strength.
            var pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6);
            glow.color = held ? UiKit.EssenceLit.A(0.3f + 0.15f * pulse)
                : tired >= 100 ? Spent.A(0.25f + 0.25f * pulse)
                : chosen ? UiKit.GoldLit.A(0.25f) : Color.clear;

            tiredBar.gameObject.SetActive(!kind.Passive && !kind.OneUse && tired >= 1);
            tiredFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(tired / 100), 1);
            tiredFill.color = tired >= 100 ? Color.Lerp(Spent, Color.white, 0.35f * pulse) : tired >= 80 ? Spent : tired >= 50 ? Tiring : UiKit.Essence;
        }

        void Raise(bool chosen)
        {
            Root.localScale = Vector3.one * (chosen ? 1.06f : 1);
        }

        /// <summary>A Gu's name for its slot: without the word "Гу", and short.</summary>
        public static string Short(string name)
        {
            var shortName = name.StartsWith("Гу ") ? name.Substring(3) : name;

            return shortName.Length > 14 ? shortName.Substring(0, 13) + "." : shortName;
        }
    }
}
