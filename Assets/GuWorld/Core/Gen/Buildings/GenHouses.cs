using System;
using System.Collections.Generic;
using System.Globalization;

namespace GuWorld.Core.Gen.Buildings
{
    /// <summary>
    /// Puts generated buildings in the settlements' places: once a settlement is planned, every
    /// hand-made house and hall it chose ("kit_*", "prop_*") becomes a building of the generator
    /// ("gen:…") of the same kind, sized to the same lot, in the settlement's clan's colours, its
    /// wealth and wear chosen by its place, one of a few dozen variants. Roles, doors and spots stay
    /// as they were, so the game finds its halls, inns and shops as before. The old model's id is
    /// kept in Placed.Origin. Off (Enabled = false), the hand-made models stay.
    /// </summary>
    public static class GenHouses
    {
        public static bool Enabled = true;

        /// <summary>
        /// The settlements are of the mountain: the town on the slope's benches, the villages not
        /// levelled; their houses ("hill:…" in place of "gen:…") stand down to the ground each on
        /// its own posts and stone, raised where they stand.
        /// </summary>
        public static bool Mountain = true;

        /// <summary>How many variants of each kind of building a settlement draws from (more: more variety, more meshes).</summary>
        public static int Variants = 24;

        static readonly Dictionary<string, (string style, int poorest, int richest)> Kinds = new Dictionary<string, (string, int, int)>
        {
            ["kit_hut_mud"] = ("hut", 0, 2), ["kit_hut_mud_ochre"] = ("hut", 0, 3), ["prop_poor_house"] = ("hut", 0, 2),
            ["kit_stilt_bamboo"] = ("stilt", 1, 4), ["kit_stilt_plank"] = ("stilt", 1, 4), ["guyue_bamboo_house"] = ("stilt", 3, 6),
            ["kit_artisan"] = ("cottage", 2, 5), ["kit_artisan_ochre"] = ("cottage", 2, 5), ["kit_l_kitchen"] = ("house", 2, 5), ["kit_l_kitchen_grey"] = ("house", 2, 5),
            ["kit_slope_terrace"] = ("house", 3, 6),
            ["kit_two_storey"] = ("townhouse", 4, 7), ["kit_two_storey_stone"] = ("townhouse", 5, 8),
            ["kit_shop_house"] = ("shop", 3, 7), ["kit_shop_house_plaster"] = ("shop", 4, 8),
            ["prop_shop"] = ("shop", 5, 8), ["prop_shop_small_a"] = ("shop", 2, 6), ["prop_shop_small_b"] = ("shop", 2, 6), ["prop_shop_small_c"] = ("shop", 2, 6),
            ["kit_courtyard"] = ("courtyard", 4, 8), ["kit_courtyard_blue"] = ("courtyard", 5, 9),
            ["prop_clan_main_hall"] = ("great_hall", 9, 9), ["prop_academy_great"] = ("academy", 8, 9), ["prop_academy"] = ("academy", 7, 8),
            ["prop_academy_dorm"] = ("dorm", 6, 7), ["prop_dorm_great"] = ("dorm", 6, 8),
            ["prop_refectory"] = ("inn", 6, 7), ["prop_inn"] = ("inn", 5, 7), ["prop_inn_courtyard"] = ("inn", 6, 8), ["prop_tavern"] = ("inn", 4, 6),
            ["prop_tavern_great"] = ("inn", 7, 9), ["prop_eatery"] = ("shop", 4, 6), ["prop_teahouse"] = ("townhouse", 5, 7), ["prop_teahouse_octagon"] = ("pavilion", 7, 9),
            ["prop_market_row"] = ("market_row", 4, 7), ["prop_gu_nursery"] = ("nursery", 6, 8), ["prop_herbalist"] = ("shop", 5, 7),
            ["prop_smithy"] = ("smithy", 3, 5), ["prop_granary"] = ("granary", 3, 6), ["prop_council_hall"] = ("hall", 7, 9),
            ["prop_ancestral_hall"] = ("temple", 8, 9), ["guyue_pavilion_LOD0"] = ("pavilion", 8, 9),
        };

        public static bool Has(string model) => model != null && Kinds.ContainsKey(model);

        /// <summary>The name of the generated building for an old model on a lot, its clan's.</summary>
        public static string For(string old, Box lot, string clan, int salt)
        {
            var (style, poorest, richest) = Kinds[old];
            var h = (uint)Noise.Hash((int)(lot.Centre.X * 3), (int)(lot.Centre.Z * 3), salt);
            var variant = (int)(h % (uint)Math.Max(1, Variants)) + 1;
            // Wealth and wear follow the variant, so a settlement draws on at most `Variants` meshes of each kind of lot.
            var wealth = poorest + variant % (richest - poorest + 1);
            var wear = 2 + variant * 7 % 4;
            // The lot's width and depth (a little smaller than the old model's, so eaves stay inside it).
            var w = (float)Math.Round(lot.Half.X * 2 * 0.95f, 1);
            var d = (float)Math.Round(lot.Half.Z * 2 * 0.95f, 1);

            return string.Format(CultureInfo.InvariantCulture, "gen:{0}:{1}:{2}:{3}:{4}:{5:0.0}:{6:0.0}", style, variant, clan ?? "", wealth, wear, w, d);
        }

        public static void Convert(SettlementPlan plan)
        {
            if (!Enabled)
            {
                return;
            }

            var clan = plan.Def.Clan == "gu_yue" || plan.Def.Clan == "bai" || plan.Def.Clan == "xiong" ? plan.Def.Clan : "";

            foreach (var building in plan.Buildings)
            {
                if (!Has(building.Model))
                {
                    continue;
                }

                building.Origin = building.Model;
                building.Model = For(building.Model, building.Footprint, clan, plan.Def.Seed);

                // A village is not levelled: its halls too stand down to the slope (the town's great buildings have pads).
                if (Mountain && !plan.Def.City)
                {
                    building.Model = "hill:" + building.Model.Substring(4);
                }
                // A generated building stands on its lot's middle (the old models had their own pivots).
                building.At = building.Footprint.Centre;
            }
        }

        /// <summary>How many people live in a generated building.</summary>
        public static int Occupants(string model)
        {
            var bits = model.Split(':');

            switch (bits.Length > 1 ? bits[1] : "")
            {
                case "courtyard": return 9;
                case "townhouse": return 6;
                case "house": return 5;
                case "cottage": return 4;
                case "stilt": return 4;
                case "hut": return 3;
                case "shop": return 3;
                case "dorm": return 24;
                // A Gu Yue's bamboo house: a family over its stakes, two for the better off; the manor: a branch's head, its elders, servants.
                case "bamboo": return 6;
                case "lodging": return 10;
                case "manor": return 30;
                case "tavern": return 3;
                default: return 0;
            }
        }
    }
}
