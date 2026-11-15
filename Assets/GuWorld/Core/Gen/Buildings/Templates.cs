using System;
using System.Collections.Generic;
using System.Globalization;

namespace GuWorld.Core.Gen.Buildings
{
    /// <summary>One kind of lot a settlement fills: what is built on it, how often, how wide (along the street) and deep, how rich.</summary>
    public struct LotKind
    {
        public string Style;
        public float Weight;
        public float Width, Depth;
        public int Poorest, Richest;
        /// <summary>A shop or a market row: its role in the plan is "shop".</summary>
        public bool Shop;

        public LotKind(string style, float weight, float width, float depth, int poorest, int richest, bool shop = false)
        {
            Style = style;
            Weight = weight;
            Width = width;
            Depth = depth;
            Poorest = poorest;
            Richest = richest;
            Shop = shop;
        }
    }

    /// <summary>
    /// How a settlement is built up, as numbers the planners read: the lots of each district (by
    /// distance from the middle, as a share of the radius) and of the main street, the gap between
    /// lots, how far back from the street they stand. A clan's template gives its look: the Gu Yue's
    /// courtyards and two-storey houses, the Bai's white courtyards, the Xiong's cottages and huts,
    /// a hamlet's huts and stilt houses, a camp's shacks.
    /// </summary>
    public sealed class TownTemplate
    {
        public string Id;
        /// <summary>The districts from the middle out: (up to this share of the radius, its lots).</summary>
        public (float upTo, LotKind[] lots)[] Districts;
        /// <summary>The lots along the main street near the middle (shops, inns), up to this share of the radius.</summary>
        public LotKind[] MainStreet;
        public float MainStreetUpTo = 0.55f;
        /// <summary>Metres between neighbouring lots along a street; from the street's edge to a lot's front.</summary>
        public float Gap = 1.2f, Setback = 1.2f;
        /// <summary>The share of a district's possible lots built on (the rest gardens, gaps, trees).</summary>
        public float Fill = 0.9f;
        /// <summary>The chance of a vegetable garden behind a house (not a courtyard or a shop).</summary>
        public float Gardens = 0.4f;

        public static readonly TownTemplate GuYue = new TownTemplate
        {
            Id = "gu_yue",
            Districts = new[]
            {
                (0.42f, new[] { new LotKind("courtyard", 6, 22, 25, 6, 9), new LotKind("townhouse", 2, 12, 12, 5, 8), new LotKind("courtyard", 2, 26, 30, 8, 9) }),
                (0.72f, new[] { new LotKind("courtyard", 4, 19, 22, 3, 6), new LotKind("townhouse", 3, 11, 12, 4, 7), new LotKind("house", 3, 12, 10, 3, 6), new LotKind("shop", 1, 11, 11, 3, 6, true) }),
                (1.01f, new[] { new LotKind("house", 3, 11, 9, 1, 4), new LotKind("cottage", 3, 10, 10, 1, 4), new LotKind("hut", 2, 7.5f, 7, 0, 2), new LotKind("stilt", 2, 9, 9, 1, 4), new LotKind("courtyard", 1, 17, 19, 1, 3) }),
            },
            MainStreet = new[] { new LotKind("shop", 4, 11, 11, 4, 8, true), new LotKind("market_row", 1, 26, 12, 4, 7, true), new LotKind("townhouse", 2, 12, 12, 5, 8) },
            Gap = 1.0f, Setback = 1.2f, Fill = 0.92f, Gardens = 0.25f,
        };

        public static readonly TownTemplate Bai = new TownTemplate
        {
            Id = "bai",
            Districts = new[]
            {
                (0.6f, new[] { new LotKind("courtyard", 5, 19, 22, 4, 8), new LotKind("townhouse", 2, 11, 12, 4, 7) }),
                (1.01f, new[] { new LotKind("courtyard", 2, 17, 19, 2, 5), new LotKind("house", 3, 11, 10, 2, 5), new LotKind("cottage", 1, 10, 10, 1, 4) }),
            },
            MainStreet = new[] { new LotKind("shop", 3, 11, 11, 4, 7, true), new LotKind("townhouse", 1, 12, 12, 5, 7) },
            Gap = 1.4f, Setback = 1.4f, Fill = 0.85f,
        };

        public static readonly TownTemplate Xiong = new TownTemplate
        {
            Id = "xiong",
            Districts = new[]
            {
                (0.55f, new[] { new LotKind("courtyard", 3, 18, 20, 3, 6), new LotKind("cottage", 3, 11, 10, 2, 5), new LotKind("house", 1, 12, 10, 3, 5) }),
                (1.01f, new[] { new LotKind("cottage", 3, 10, 10, 1, 4), new LotKind("hut", 3, 8, 7.5f, 0, 3), new LotKind("stilt", 1, 9, 9, 1, 3) }),
            },
            MainStreet = new[] { new LotKind("shop", 2, 11, 11, 2, 5, true), new LotKind("cottage", 2, 11, 10, 2, 5) },
            Gap = 2.2f, Setback = 1.8f, Fill = 0.8f, Gardens = 0.6f,
        };

        public static readonly TownTemplate Hamlet = new TownTemplate
        {
            Id = "hamlet",
            Districts = new[] { (1.01f, new[] { new LotKind("hut", 3, 8, 7.5f, 0, 3), new LotKind("cottage", 3, 10, 10, 1, 4), new LotKind("stilt", 2, 9, 9, 1, 3), new LotKind("courtyard", 1, 16, 17, 1, 3) }) },
            MainStreet = new[] { new LotKind("cottage", 2, 10, 10, 2, 4), new LotKind("shop", 1, 10, 10, 2, 4, true) },
            Gap = 3.5f, Setback = 2.4f, Fill = 0.75f, Gardens = 0.8f,
        };

        public static readonly TownTemplate Camp = new TownTemplate
        {
            Id = "camp",
            Districts = new[] { (1.01f, new[] { new LotKind("hut", 3, 8, 7.5f, 0, 2), new LotKind("stilt", 2, 9, 9, 0, 2), new LotKind("cottage", 1, 10, 10, 0, 2) }) },
            MainStreet = new[] { new LotKind("cottage", 1, 10, 10, 1, 3) },
            Gap = 3f, Setback = 2f, Fill = 0.85f, Gardens = 0,
        };

        public static TownTemplate For(string clan, string style)
        {
            switch (clan)
            {
                case "gu_yue": return GuYue;
                case "bai": return Bai;
                case "xiong": return Xiong;
            }

            return style == "Camp" ? Camp : Hamlet;
        }

        /// <summary>The lots that fit a place: by its share of the radius out, on the main street or not.</summary>
        public LotKind[] At(float share, bool main)
        {
            if (main && share < MainStreetUpTo && MainStreet != null)
            {
                return MainStreet;
            }

            foreach (var (upTo, lots) in Districts)
            {
                if (share <= upTo)
                {
                    return lots;
                }
            }

            return Districts[Districts.Length - 1].lots;
        }

        public static LotKind Pick(LotKind[] lots, ref Rng rng)
        {
            var total = 0f;

            foreach (var lot in lots)
            {
                total += lot.Weight;
            }

            var r = rng.Range(0f, total);

            foreach (var lot in lots)
            {
                r -= lot.Weight;

                if (r <= 0)
                {
                    return lot;
                }
            }

            return lots[lots.Length - 1];
        }

        /// <summary>The generated building's name for a lot of a kind: one of a few dozen variants, its wealth and wear by the variant.</summary>
        public static string Name(LotKind lot, string clan, ref Rng rng)
        {
            var variant = rng.Range(1, GenHouses.Variants + 1);
            var wealth = lot.Poorest + variant % (lot.Richest - lot.Poorest + 1);
            var wear = 2 + variant * 7 % 4;

            return string.Format(CultureInfo.InvariantCulture, "gen:{0}:{1}:{2}:{3}:{4}:{5:0.0}:{6:0.0}", lot.Style, variant, clan ?? "", wealth, wear, lot.Width, lot.Depth);
        }
    }

    /// <summary>
    /// Fills the streets of a settlement with lots, the way its template says: lot after lot along
    /// each side of each street, fronts to the street, nearest the middle first (so a settlement short
    /// of houses is dense in its heart and thin at its edge, not sprinkled everywhere).
    /// </summary>
    public static class Lots
    {
        /// <summary>
        /// `place(model, centre, yaw, role)` tries a building on the plan and says whether it went in.
        /// Returns how many were placed.
        /// </summary>
        public static int Fill(SettlementPlan plan, TownTemplate template, int most, string clan, Func<string, Vec2, float, string, bool> place, ref Rng rng, float keepFromMiddle)
        {
            var radius = plan.Def.Radius;
            var candidates = new List<(Vec2 point, Vec2 normal, float halfWidth, float distance, LotKind lot)>();

            foreach (var street in plan.Streets)
            {
                var length = Lines.Length(street.Points);

                foreach (var side in new[] { -1, 1 })
                {
                    var along = rng.Range(1f, 4f);

                    while (along < length - 2)
                    {
                        var point = Lines.At(street.Points, along, out var direction);
                        var distance = Vec2.Distance(point, plan.Centre);
                        var lot = TownTemplate.Pick(template.At(distance / radius, street.Main), ref rng);
                        var normal = side < 0 ? direction.Left : direction.Right;
                        // The lot's middle along the street is half its width on.
                        var middle = Lines.At(street.Points, Math.Min(length - 1, along + lot.Width * 0.5f), out _);
                        candidates.Add((middle, normal, street.HalfWidth, distance, lot));
                        along += lot.Width + template.Gap;
                    }
                }
            }

            candidates.Sort((a, b) => a.distance.CompareTo(b.distance));
            var placed = 0;
            var done = new bool[candidates.Count];

            // A first pass leaves gaps (gardens, trees) as the template says; a second fills them if the settlement is still short of houses.
            for (var pass = 0; pass < 2 && placed < most; pass++)
            {
                for (var i = 0; i < candidates.Count && placed < most; i++)
                {
                    var (point, normal, halfWidth, distance, lot) = candidates[i];

                    if (done[i] || distance < keepFromMiddle || pass == 0 && !rng.Chance(template.Fill))
                    {
                        continue;
                    }

                    var kind = lot;

                    for (var attempt = 0; attempt < 2; attempt++)
                    {
                        var model = TownTemplate.Name(kind, clan, ref rng);
                        var centre = point + normal * (halfWidth + template.Setback + kind.Depth * 0.5f);

                        if (place(model, centre, (-normal).Yaw, kind.Shop ? "shop" : "house"))
                        {
                            placed++;
                            done[i] = true;

                            // A vegetable garden behind it.
                            if (!kind.Shop && kind.Style != "courtyard" && rng.Chance(template.Gardens))
                            {
                                var garden = centre + normal * (kind.Depth * 0.5f + 4f);
                                place($"thing:garden:{rng.Range(1, 9)}:{clan}", garden, (-normal).Yaw, "prop");
                            }

                            break;
                        }

                        // A smaller lot of the district's where the big one did not fit.
                        var smallest = kind;

                        foreach (var other in template.At(distance / radius, false))
                        {
                            if (other.Width * other.Depth < smallest.Width * smallest.Depth)
                            {
                                smallest = other;
                            }
                        }

                        if (smallest.Style == kind.Style && smallest.Width == kind.Width)
                        {
                            break;
                        }

                        kind = smallest;
                    }
                }
            }

            return placed;
        }
    }
}
