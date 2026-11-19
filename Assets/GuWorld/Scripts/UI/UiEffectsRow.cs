using System.Collections.Generic;
using GuWorld.Core;
using UnityEngine;
using UnityEngine.UI;

namespace GuWorld.UI
{
    /// <summary>
    /// What lasts on the hero as icons under the bars (ROADMAP 3.1, in place of a line of words): a dark
    /// disc ringed by its kind's colour — gifts (Gu and pills) in jade and gold, harms in coral, wounds in
    /// amber, hunger in orange — its seal in the middle, the time left as a shadow sweeping round it and
    /// the seconds under it. With the colour-blind setting each kind also has its own shape of ring.
    /// </summary>
    public partial class Ui
    {
        const int EffectCells = 12;

        class EffectCell
        {
            public RectTransform Root;
            public Image Ring, Sweep;
            public Text Seal, Left;
        }

        readonly List<EffectCell> effectCells = new List<EffectCell>();
        readonly Dictionary<string, float> effectLongest = new Dictionary<string, float>();

        enum EffectSort { Gift, Harm, Wound, Hunger, Stat }

        RectTransform effectsRow;

        void BuildEffectsRow(RectTransform hero)
        {
            var row = UiKit.Rect("Effects", hero);
            effectsRow = row;
            row.Size(46, 440, 0);
            var line = row.Stack<HorizontalLayoutGroup>(6, null, TextAnchor.MiddleLeft);
            line.childForceExpandWidth = false;
            line.childControlWidth = false;
            line.childControlHeight = false;

            for (var i = 0; i < EffectCells; i++)
            {
                var root = UiKit.Rect("Effect", row);
                root.sizeDelta = new Vector2(30, 44);
                var back = UiKit.Image("Back", root, new Color(4 / 255f, 6 / 255f, 10 / 255f, 0.75f), UiKit.Disc);
                back.rectTransform.At(new Vector2(0.5f, 1), new Vector2(0, -15), new Vector2(30, 30));
                var ring = UiKit.Image("Ring", root, Color.white, UiKit.Ring);
                ring.rectTransform.At(new Vector2(0.5f, 1), new Vector2(0, -15), new Vector2(30, 30));
                var seal = UiKit.Zh("", root, 17, Color.white, TextAnchor.MiddleCenter);
                seal.rectTransform.At(new Vector2(0.5f, 1), new Vector2(0, -15), new Vector2(30, 30));
                var sweep = UiKit.Image("Sweep", root, new Color(0, 0, 0, 0.55f), UiKit.Disc);
                sweep.type = Image.Type.Filled;
                sweep.fillMethod = Image.FillMethod.Radial360;
                sweep.fillOrigin = 2;
                sweep.fillClockwise = false;
                sweep.rectTransform.At(new Vector2(0.5f, 1), new Vector2(0, -15), new Vector2(28, 28));
                var left = UiKit.Label("", root, 10, UiKit.Text2, TextAnchor.LowerCenter).Shadowed();
                left.rectTransform.Fill();
                root.gameObject.SetActive(false);
                effectCells.Add(new EffectCell { Root = root, Ring = ring, Sweep = sweep, Seal = seal, Left = left });
            }
        }

        static Color SortColour(EffectSort sort) =>
            sort == EffectSort.Gift ? Accessible.Gift : sort == EffectSort.Harm ? Accessible.Harm : sort == EffectSort.Wound ? UiKit.Hex(0xF0C868) : sort == EffectSort.Hunger ? UiKit.Hex(0xF0A070) : UiKit.Moon;

        static string BuffSeal(BuffKind kind)
        {
            switch (kind)
            {
                case BuffKind.Shield: return "盾";
                case BuffKind.MeleeUp: return "力";
                case BuffKind.Unstoppable: return "身";
                case BuffKind.Stealth: return "隐";
                case BuffKind.Aura: return "气";
                case BuffKind.Haste: return "速";
                default: return "药";
            }
        }

        static string StatusSeal(StatusKind kind)
        {
            switch (kind)
            {
                case StatusKind.Slow: return "缓";
                case StatusKind.Stun: return "雷";
                case StatusKind.Freeze: return "冰";
                case StatusKind.Poison: return "毒";
                case StatusKind.Burn: return "火";
                case StatusKind.Bleed: return "血";
                default: return "寒";
            }
        }

        /// <summary>The icons as things stand (called with the HUD's words, ten times a second).</summary>
        void TickEffectsRow(Session session)
        {
            // Turned off in the settings (Interface): the row is not shown at all.
            if (effectsRow != null && effectsRow.gameObject.activeSelf != Prefs.EffectsRow)
            {
                effectsRow.gameObject.SetActive(Prefs.EffectsRow);
            }

            if (!Prefs.EffectsRow)
            {
                return;
            }

            var now = new List<(string key, string seal, EffectSort sort, float left)>();

            foreach (BuffKind kind in System.Enum.GetValues(typeof(BuffKind)))
            {
                var left = session.BuffLeft(kind);

                if (kind != BuffKind.None && left > 0)
                {
                    now.Add(("b" + kind, BuffSeal(kind), EffectSort.Gift, left));
                }
            }

            if (session.WardReady)
            {
                now.Add(("ward", "护", EffectSort.Gift, -1));
            }

            foreach (var status in session.Statuses)
            {
                now.Add(("s" + status.Kind, StatusSeal(status.Kind), EffectSort.Harm, status.Seconds));
            }

            foreach (var effect in session.Effects)
            {
                if (effect.IsStat)
                {
                    now.Add(("e" + effect.Source, "药", EffectSort.Stat, effect.Seconds));
                }
            }

            foreach (var wound in session.Wounds)
            {
                now.Add(("w" + wound.Part, "伤", EffectSort.Wound, wound.Hours * 3600));
            }

            var hungry = 0;

            foreach (var gu in session.Slotted())
            {
                hungry += session.IsStarving(gu) ? 1 : 0;
            }

            if (hungry > 0)
            {
                now.Add(("hunger", "虫", EffectSort.Hunger, -1));
            }

            for (var i = 0; i < effectCells.Count; i++)
            {
                var cell = effectCells[i];

                if (i >= now.Count)
                {
                    cell.Root.gameObject.SetActive(false);
                    continue;
                }

                var (key, seal, sort, left) = now[i];
                var colour = SortColour(sort);
                cell.Root.gameObject.SetActive(true);
                cell.Seal.text = seal;
                cell.Seal.color = colour;
                cell.Ring.color = colour.A(0.85f);
                cell.Ring.sprite = Prefs.ColourBlind ? Accessible.RingFor((int)sort) : UiKit.Ring;

                if (left < 0)
                {
                    cell.Sweep.fillAmount = 0;
                    cell.Left.text = sort == EffectSort.Hunger ? $"{hungry}" : "";
                    continue;
                }

                effectLongest[key] = Mathf.Max(effectLongest.TryGetValue(key, out var most) ? most : 0, left);
                cell.Sweep.fillAmount = 1 - Mathf.Clamp01(left / Mathf.Max(0.01f, effectLongest[key]));
                cell.Left.text = left >= 3600 ? $"{Mathf.CeilToInt(left / 3600)}ч" : left >= 60 ? $"{Mathf.CeilToInt(left / 60)}м" : $"{Mathf.CeilToInt(left)}с";
            }

            // What has ended is forgotten (its longest time starts afresh next time).
            var keys = new List<string>(effectLongest.Keys);

            foreach (var key in keys)
            {
                if (!now.Exists(e => e.key == key))
                {
                    effectLongest.Remove(key);
                }
            }
        }
    }
}
