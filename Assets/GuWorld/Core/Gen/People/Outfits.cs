using System;
using System.Text;
using GuWorld.Core.Gen.Flora;

namespace GuWorld.Core.Gen.People
{
    /// <summary>What a person wears and how their hair is done, chosen from their spec.</summary>
    public sealed class Outfit
    {
        /// <summary>robe (to the ankle or the calf), tunic (to the knee, over trousers), jacket (to the hip, over trousers), vest (a sleeveless jacket).</summary>
        public string Upper = "robe";
        public bool Trousers;
        /// <summary>Where the hem falls: 0 at the ankle, 1 at the knee (robes and tunics).</summary>
        public float Hem;
        /// <summary>The sleeves' opening at the wrist (metres of radius): 0.045 tight, 0.16 a scholar's wide sleeve.</summary>
        public float Cuff = 0.06f;
        public bool ShortSleeves;
        public Tint Main, Inner, Trim, Sash, Legs, Shoes;
        /// <summary>0 plain, 1 stripes, 2 embroidery, 3 patched.</summary>
        public int Pattern;
        /// <summary>The dirt of the road and the field at the hem and on the legs (0-1).</summary>
        public float Dirt;
        /// <summary>short, topknot, ponytail, long, bun, twinbuns, bald, balding, bob, braid, tied.</summary>
        public string Hair = "short";
        /// <summary>none, straw, cap, headband, scarf, helmet, crown (an elder's small guan).</summary>
        public string Hat = "none";
        /// <summary>none, stubble, moustache, goatee, long.</summary>
        public string Beard = "none";
        public bool Apron, Armour, Basket, Pouch, Sandals, Barefoot, Wraps, Pin;
        public Tint ApronColour, ArmourColour, HatColour, PinColour;
    }

    public static class Outfits
    {
        // Clans' colours: (main, second, trim).
        static (int main, int second, int trim) Clan(string clan, ref Rng rng)
        {
            switch (clan)
            {
                case "bai": return rng.Pick(new[] { (0xd4d4cc, 0x9aaec4, 0x8a96a4), (0xc8ccd0, 0x6a7e98, 0xd8d8d0), (0xe0dcd0, 0x7a8aa0, 0x5a6a80) });
                case "xiong": return rng.Pick(new[] { (0x6a4428, 0xa06a2a, 0x3a2a20), (0x8a3a20, 0x5a3420, 0xb08a3a), (0x7a5a30, 0x4a3020, 0xa04a24) });
                case "none": return rng.Pick(new[] { (0x3a3430, 0x5a2a28, 0x2a2420), (0x4a3a2a, 0x2a2a28, 0x6a4a2a) });
                default: return rng.Pick(new[] { (0x2e3a4c, 0x1e2228, 0xc8c8c0), (0x2a4a4a, 0x22282c, 0xb08a3a), (0x34405a, 0x1c2026, 0xa8b0b8), (0x3a3a48, 0x262a30, 0xc0b89a) });
            }
        }

        static readonly int[] Hemp = { 0xb8a888, 0x8a7a64, 0x7a6a58, 0x6a5a48, 0x5a6a80, 0x34405a, 0x6a5040, 0x5a5a3a, 0xc8bea6, 0x4a4438, 0x7a6a4a };
        static readonly int[] Silk = { 0x8a2a2a, 0x3a7a5a, 0x5a3a6a, 0xa07a30, 0x4a6a96, 0x2a5a6a, 0x7a2a4a, 0x2a2a3a };
        static readonly int[] Bright = { 0xb04a3a, 0x3a7aa0, 0x5a9a4a, 0xc89a3a, 0x9a5aa0, 0xd07a5a };

        static Tint Hex(int c) => Tint.Hex(c);

        static Tint Vary(Tint c, ref Rng rng, float amount = 0.08f) => new Tint(
            Noise.Clamp01(c.R * rng.Range(1 - amount, 1 + amount)), Noise.Clamp01(c.G * rng.Range(1 - amount, 1 + amount)), Noise.Clamp01(c.B * rng.Range(1 - amount, 1 + amount)));

        public static Outfit Choose(PersonSpec spec, ref Rng rng, StringBuilder words)
        {
            var o = new Outfit();
            var female = spec.Sex == Sex.Female;
            var rich = spec.Wealth;
            var (cMain, cSecond, cTrim) = Clan(spec.Clan, ref rng);
            var clanMain = Hex(cMain);
            var clanSecond = Hex(cSecond);
            var clanTrim = Hex(cTrim);
            var hemp = Hex(rng.Pick(Hemp));
            var hemp2 = Hex(rng.Pick(Hemp));
            var silk = Hex(rng.Pick(Silk));
            o.Inner = Tint.Mix(Hex(0xd8d0bc), hemp, rich < 0.3f ? 0.6f : 0.1f);
            o.Shoes = Hex(rng.Pick(new[] { 0x1e1c1a, 0x2e2620, 0x3a3028, 0x4a3a2a }));
            o.Legs = Tint.Mix(hemp2, Hex(0x2a2a2c), rng.Value() * 0.5f);
            o.Sash = rng.Chance(0.5f) ? clanSecond : Hex(rng.Pick(Hemp));
            o.Main = rich > 0.6f ? Tint.Mix(silk, clanMain, 0.5f) : rich < 0.3f ? hemp : Tint.Mix(hemp, clanMain, rng.Range(0.3f, 0.8f));
            o.Trim = rich > 0.5f ? clanTrim : Tint.Mix(o.Main, Hex(0x1a1816), 0.45f);
            o.Pattern = rich > 0.75f && rng.Chance(0.6f) ? 2 : rich < 0.2f && rng.Chance(0.6f) ? 3 : rng.Chance(0.15f) ? 1 : 0;
            o.Cuff = rng.Range(0.05f, 0.075f);
            o.Hem = rng.Range(0f, 0.3f);
            o.HatColour = Hex(0xc8a868);
            o.PinColour = rich > 0.5f ? Hex(0xc8a040) : Hex(0x6a5a48);

            // Hair, by sex and age; the trade may tie it up.
            if (female)
            {
                o.Hair = spec.Age == AgeGroup.Child ? rng.Pick(new[] { "twinbuns", "bob", "braid", "ponytail" })
                    : spec.Age == AgeGroup.Teen ? rng.Pick(new[] { "ponytail", "long", "twinbuns", "braid", "bun", "tied" })
                    : rng.Pick(new[] { "bun", "bun", "tied", "long", "braid" });
                o.Pin = (o.Hair == "bun" || o.Hair == "tied") && rng.Chance(0.6f + 0.3f * rich);
            }
            else
            {
                o.Hair = spec.Age == AgeGroup.Child ? rng.Pick(new[] { "bob", "short", "tied", "bald" })
                    : spec.Age == AgeGroup.Elder ? rng.Pick(new[] { "topknot", "balding", "long", "topknot" })
                    : rng.Pick(new[] { "topknot", "topknot", "ponytail", "short", "long", "tied" });
                o.Pin = o.Hair == "topknot" && rng.Chance(0.5f);

                if (spec.Age == AgeGroup.Adult)
                {
                    o.Beard = rng.Pick(new[] { "none", "none", "stubble", "moustache", "goatee" });
                }
                else if (spec.Age == AgeGroup.Elder)
                {
                    o.Beard = rng.Pick(new[] { "long", "goatee", "long", "moustache", "none" });
                }
            }

            // The trade.
            switch (spec.Role)
            {
                case "farmer":
                    o.Upper = rng.Chance(0.7f) ? "jacket" : "tunic";
                    o.Trousers = true;
                    o.ShortSleeves = rng.Chance(0.35f);
                    o.Main = hemp;
                    o.Dirt = rng.Range(0.5f, 1f);
                    o.Hat = rng.Chance(0.6f) ? "straw" : rng.Chance(0.4f) ? "scarf" : "none";
                    o.Wraps = rng.Chance(0.5f);
                    o.Sandals = rng.Chance(0.6f);
                    o.Barefoot = !o.Sandals && rng.Chance(0.4f);
                    o.Basket = rng.Chance(0.25f);
                    break;
                case "porter":
                    o.Upper = rng.Chance(0.6f) ? "jacket" : "vest";
                    o.Trousers = true;
                    o.ShortSleeves = rng.Chance(0.5f);
                    o.Main = hemp;
                    o.Dirt = rng.Range(0.3f, 0.8f);
                    o.Hat = rng.Chance(0.4f) ? "headband" : rng.Chance(0.3f) ? "straw" : "none";
                    o.Wraps = true;
                    o.Sandals = rng.Chance(0.7f);
                    o.Basket = rng.Chance(0.6f);
                    break;
                case "guard":
                case "watch":
                    o.Upper = "tunic";
                    o.Trousers = true;
                    o.Hem = rng.Range(0.6f, 0.9f);
                    o.Main = Tint.Mix(clanMain, Hex(0x5a2a24), spec.Clan == "gu_yue" ? 0.35f : 0.2f);
                    o.Armour = rng.Chance(spec.Role == "guard" ? 0.85f : 0.3f);
                    o.ArmourColour = Tint.Mix(Hex(0x3a3a3e), clanMain, 0.3f);
                    o.Hat = rng.Chance(0.5f) ? "helmet" : "headband";
                    o.Wraps = true;
                    o.Hair = female ? "bun" : "topknot";
                    break;
                case "bandit":
                    o.Upper = rng.Chance(0.5f) ? "jacket" : "vest";
                    o.Trousers = true;
                    o.ShortSleeves = rng.Chance(0.5f);
                    o.Main = Tint.Mix(hemp, Hex(0x2a2420), 0.5f);
                    o.Pattern = 3;
                    o.Dirt = rng.Range(0.4f, 0.9f);
                    o.Hat = rng.Chance(0.7f) ? "headband" : "none";
                    o.Wraps = true;
                    o.Armour = rng.Chance(0.25f);
                    o.ArmourColour = Hex(0x4a3a2a);
                    o.Hair = rng.Pick(new[] { "tied", "long", "ponytail", "short" });
                    break;
                case "merchant":
                case "trader":
                    o.Upper = rng.Chance(0.6f) ? "robe" : "tunic";
                    o.Trousers = o.Upper == "tunic";
                    o.Main = rich > 0.55f ? silk : Tint.Mix(hemp, silk, 0.4f);
                    o.Hat = rng.Chance(0.4f) ? "cap" : "none";
                    o.HatColour = Hex(0x1e1c1c);
                    o.Apron = spec.Role == "trader" && rng.Chance(0.3f);
                    o.ApronColour = Hex(0x8a7a5a);
                    o.Pouch = true;
                    o.Cuff = rng.Range(0.06f, 0.1f);
                    break;
                case "cook":
                    o.Upper = "jacket";
                    o.Trousers = true;
                    o.Main = Tint.Mix(hemp, Hex(0xd0c8b8), 0.5f);
                    o.Apron = true;
                    o.ApronColour = Hex(0xd8d2c4);
                    o.Hat = "scarf";
                    o.HatColour = Hex(0xd8d2c4);
                    break;
                case "smith":
                    o.Upper = "vest";
                    o.Trousers = true;
                    o.Main = Hex(0x3a3028);
                    o.Apron = true;
                    o.ApronColour = Hex(0x5a3e28);
                    o.Hat = "headband";
                    o.Dirt = 0.6f;
                    break;
                case "herbalist":
                    o.Upper = "robe";
                    o.Main = Tint.Mix(Hex(0x4a6a4a), hemp, 0.4f);
                    o.Pouch = true;
                    o.Basket = rng.Chance(0.4f);
                    o.Hat = rng.Chance(0.4f) ? "straw" : "none";
                    o.Cuff = 0.07f;
                    break;
                case "scribe":
                case "keeper":
                    o.Upper = "robe";
                    o.Main = Tint.Mix(clanMain, Hex(0x5a6a7a), 0.4f);
                    o.Hat = rng.Chance(0.7f) ? "cap" : "none";
                    o.HatColour = Hex(0x1a1a1e);
                    o.Cuff = rng.Range(0.09f, 0.14f);
                    break;
                case "judge":
                case "elder":
                    o.Upper = "robe";
                    o.Main = Tint.Mix(clanSecond, clanMain, 0.4f);
                    o.Trim = Hex(0xb08a3a);
                    o.Pattern = rng.Chance(0.6f) ? 2 : 0;
                    o.Hat = rng.Chance(0.5f) ? "crown" : "cap";
                    o.HatColour = Hex(0x1a1a1e);
                    o.Cuff = rng.Range(0.12f, 0.17f);
                    o.Hem = 0;
                    break;
                case "student":
                    o.Upper = "robe";
                    o.Main = Tint.Mix(clanMain, Hex(0x6a7a8a), 0.25f);
                    o.Trim = clanTrim;
                    o.Hem = rng.Range(0.15f, 0.35f);
                    o.Hat = rng.Chance(0.4f) ? "headband" : "none";
                    o.Hair = female ? rng.Pick(new[] { "ponytail", "bun", "tied" }) : rng.Pick(new[] { "topknot", "ponytail", "tied" });
                    break;
                case "gambler":
                    o.Upper = "robe";
                    o.Main = Hex(rng.Pick(new[] { 0x5a3a6a, 0x7a2a4a, 0x2a5a6a }));
                    o.Hem = 0.2f;
                    o.Pattern = 1;
                    o.Pouch = true;
                    break;
                case "child":
                    o.Upper = rng.Chance(0.6f) ? "jacket" : "tunic";
                    o.Trousers = true;
                    o.Main = Hex(rng.Pick(Bright));
                    o.Barefoot = rich < 0.2f && rng.Chance(0.5f);
                    o.Dirt = rng.Range(0f, 0.5f);
                    break;
                case "idler":
                case "villager":
                default:
                    o.Upper = rng.Pick(new[] { "robe", "tunic", "jacket", "robe" });
                    o.Trousers = o.Upper != "robe";
                    o.Dirt = rng.Range(0f, 0.3f);
                    o.Hat = rng.Chance(0.15f) ? "straw" : rng.Chance(0.1f) ? "scarf" : "none";
                    o.Basket = rng.Chance(0.08f);
                    break;
            }

            if (female && o.Upper == "robe")
            {
                o.Hem = Math.Min(o.Hem, 0.15f);
            }

            if (o.Upper == "tunic")
            {
                o.Hem = Math.Max(o.Hem, 0.55f);
            }

            o.Main = Vary(o.Main, ref rng);
            o.Trim = Vary(o.Trim, ref rng, 0.05f);
            words.Append($"{o.Upper}{(o.Trousers ? " + trousers" : "")}, hair {o.Hair}{(o.Beard != "none" ? ", beard " + o.Beard : "")}{(o.Hat != "none" ? ", hat " + o.Hat : "")}");
            words.Append(o.Armour ? ", armour" : "").Append(o.Apron ? ", apron" : "").Append(o.Basket ? ", basket" : "").Append(o.Pattern == 2 ? ", embroidered" : o.Pattern == 3 ? ", patched" : o.Pattern == 1 ? ", striped" : "");

            return o;
        }
    }
}
