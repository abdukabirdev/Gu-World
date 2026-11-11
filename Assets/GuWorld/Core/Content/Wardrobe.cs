namespace GuWorld.Core
{
    /// <summary>
    /// The looks of the wider mountain's people, dressed from the looks the artist made: each new look
    /// takes an existing texture and a dye over it (a Bai elder is an elder in pale blue, a Xiong
    /// hunter a guard in ochre), until it has a texture of its own.
    /// </summary>
    public static class Wardrobe
    {
        /// <summary>The textures there are (Resources/Looks).</summary>
        public static readonly string[] Made = { "bai", "bandit", "blood", "cook", "elder", "gambler", "guard", "herbalist", "judge", "keeper", "merchant", "scribe", "smith", "stone", "villager", "xiong" };

        static readonly (string look, string texture, int dye)[] Borrowed =
        {
            ("bai_elder", "elder", 0xD8ECFF),
            ("bai_villager", "bai", 0xFFFFFF),
            ("bai_guard", "guard", 0xC8E4FF),
            ("xiong_elder", "elder", 0xF0C890),
            ("child", "villager", 0xF4E8D8),
            ("farmer", "villager", 0xD8C8A0),
            ("hunter", "guard", 0xC8B088),
            ("witch", "herbalist", 0xB8A8C8),
            ("alchemist", "herbalist", 0xF0D0A0),
            ("monk", "scribe", 0xE8B060),
            ("miner", "smith", 0xA8A098),
            ("rogue", "bandit", 0x8890A0),
            ("bandit_chief", "bandit", 0xD08070),
            ("student", "guard", 0xB8D4F0),
            ("shade", "elder", 0xA090D0),
        };

        /// <summary>The canid's textures there are; other canid looks (bear, boar, snow) are dyed in the engine.</summary>
        public static readonly string[] CanidMade = { "fox", "grey", "storm", "blood" };

        public static bool CanidHasTexture(string look) => System.Array.IndexOf(CanidMade, string.IsNullOrEmpty(look) ? "fox" : look) >= 0;

        /// <summary>The texture to dress a look in, and the dye over it (0xFFFFFF: none).</summary>
        public static (string texture, int dye) Dress(string look)
        {
            if (string.IsNullOrEmpty(look))
            {
                return ("", 0xFFFFFF);
            }

            foreach (var made in Made)
            {
                if (made == look)
                {
                    return (look, 0xFFFFFF);
                }
            }

            foreach (var (name, texture, dye) in Borrowed)
            {
                if (name == look)
                {
                    return (texture, dye);
                }
            }

            return ("villager", 0xFFFFFF);
        }
    }
}
