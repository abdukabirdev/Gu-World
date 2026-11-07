using UnityEngine;

namespace GuWorld.Body
{
    /// <summary>
    /// The cloth's colours: what the people of the mountain dye their clothes. A build's dye
    /// (HumanBody.Build.dye) picks a palette; HumanBody paints the clothes' pieces by their
    /// materials' names with it (Robe, Trim, Belt, Trousers, Wraps, Shoes, Mark).
    /// </summary>
    public static class Dyes
    {
        /// <summary>One dye's colours: the robe or tunic, its trim, the sash, the trousers, the leg wraps, the shoes.</summary>
        public struct Palette
        {
            public Color tunic, trim, sash, trousers, wraps, shoes;
        }

        /// <summary>How many dyes there are (0 … Palettes - 1).</summary>
        public const int Palettes = 14;

        /// <summary>
        /// The dyes a village has (for the villagers): 0 is the plain one for each gender;
        /// then hemp brown, indigo grey, undyed, the guard's dark with red, the elder's plum
        /// with gold, sage, rust; from 8 the hanfu (the hero's look: Look.Dye).
        /// </summary>
        public static Palette For(Gender gender, int index = 0)
        {
            Palette P(int tunic, int trim, int sash, int trousers) => new Palette
            {
                tunic = Hex(tunic), trim = Hex(trim), sash = Hex(sash),
                trousers = Hex(trousers), wraps = Hex(0xb8ae98), shoes = Hex(0x1f1c1a),
            };

            switch (index)
            {
                case 1: return P(0x6b5a45, 0x2e2620, 0x3f4a5a, 0x3a3631);
                case 2: return P(0x4a5568, 0x1f2430, 0x8a6a3a, 0x33322f);
                case 3: return P(0x857660, 0x3a3026, 0x5a3020, 0x45403a);
                case 4: return P(0x3a3f3a, 0x7a2a22, 0x2a2a2a, 0x2c2b29);
                case 5: return P(0x5a4a66, 0xb89a5a, 0x3a2e40, 0x3a3436);
                case 6: return P(0x6a7d6a, 0x2c3a2c, 0x7a5a3a, 0x3c3a34);
                case 7: return P(0x8a5a45, 0x2e1e18, 0x4a4a3a, 0x3a3430);
                // The hero's hanfu (the art direction's "Hero 2.0"): slate blue with a dark red
                // trim and sash, earth-brown trousers, linen wraps; then grey, then white.
                case 8: return Hanfu(0x2b2f3a, 0x7a2526, 0x6e1f22, 0x4a4238, 0xd2c6b1);
                case 9: return Hanfu(0x6d7078, 0x25272b, 0x6e1f22, 0x3a3c40, 0xd2c6b1);
                case 10: return Hanfu(0xd8d5cc, 0x2c2f35, 0x4a4f58, 0x9c9a94);
                // The clan's dark bamboo green (lore: the Gu Yue's green bamboo) with a bronze trim; indigo with silver;
                // earth brown with a dark trim.
                case 11: return Hanfu(0x26352b, 0x8a6a34, 0x5e1c1e, 0x3a3a32, 0xc8bea8);
                case 12: return Hanfu(0x222a42, 0x8e9ab4, 0x3a2a48, 0x30323a, 0xccc6b6);
                case 13: return Hanfu(0x4a3526, 0x231a14, 0x7a5a2a, 0x3a332c, 0xcfc2aa);
            }

            return gender == Gender.Female
                ? P(0x7d5f5a, 0x2d2422, 0x4a5a4e, 0x3f3a36)
                : P(0x56686a, 0x262a2c, 0x7a4e2d, 0x3d3a35);
        }

        /// <summary>Whether a dye is a hanfu's (the long robe, to the shin), not a village tunic's (to the thigh or the knee).</summary>
        public static bool Long(int dye) => dye == 5 || dye >= 8;

        /// <summary>The clan mark's colour on a dye: silver on dark cloth, slate on white.</summary>
        public static Color MarkColour(int dye) => dye == 10 ? Hex(0x34405a) : Hex(0xc9ccd4);

        static Palette Hanfu(int tunic, int trim, int sash, int trousers, int wraps = -1) => new Palette
        {
            tunic = Hex(tunic), trim = Hex(trim), sash = Hex(sash),
            trousers = Hex(trousers), wraps = wraps >= 0 ? Hex(wraps) : Hex(trousers) * 1.15f, shoes = Hex(0x141317),
        };

        public static Color Hex(int rgb) => new Color(((rgb >> 16) & 0xff) / 255f, ((rgb >> 8) & 0xff) / 255f, (rgb & 0xff) / 255f);
    }
}
