using UnityEngine;

namespace GuWorld.UI
{
    /// <summary>
    /// The colour-blind setting (ROADMAP 2.7.6): with it on, what tells good from bad is not red against
    /// green — gifts go sky blue, harms orange — and the effect icons' rings take a shape of their own
    /// for each kind (round, hexagon, octagon, chamfered), so even without colour they are told apart.
    /// </summary>
    public static class Accessible
    {
        public static Color Harm => Prefs.ColourBlind ? UiKit.Hex(0xFF9F1C) : UiKit.Danger;
        public static Color Gift => Prefs.ColourBlind ? UiKit.Hex(0x5AB4FF) : UiKit.Essence;

        /// <summary>A ring of its own shape for each kind of effect (0 gift, 1 harm, 2 wound, 3 hunger, 4 the rest).</summary>
        public static Sprite RingFor(int sort)
        {
            switch (sort)
            {
                case 1: return UiKit.HexagonUpLine;
                case 2: return UiKit.OctagonLine;
                case 3: return UiKit.ChamferFrame;
                default: return UiKit.Ring;
            }
        }
    }
}
