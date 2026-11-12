using System;
using System.Collections.Generic;

namespace GuWorld.Core
{
    /// <summary>One thing the clan's treasury gives for merit, from a clan rank on.</summary>
    public sealed class TreasuryEntry
    {
        public string Item;
        public string Gu;
        public int Quality;
        public int Merit;
        public int ClanRank;
    }

    /// <summary>
    /// Merit with the Gu Yue clan: earned by its errands, by holding the wolf tide, by felling the great
    /// beasts and by the clan's own quests; it raises the hero's standing inside the clan (outer disciple
    /// to elder) and is spent at the clan's treasury in the main hall, which keeps what no shop sells.
    /// </summary>
    public partial class Session
    {
        public static readonly string[] ClanRanks = { "Внешний ученик", "Ученик клана", "Член клана", "Старший клана", "Старейшина" };
        /// <summary>Merit earned (in all) to reach each clan rank.</summary>
        public static readonly int[] ClanRankNeeds = { 0, 150, 600, 1800, 4500 };

        public static readonly TreasuryEntry[] Treasury =
        {
            new TreasuryEntry { Item = "great_essence_pill", Merit = 20 },
            new TreasuryEntry { Item = "jade_shard", Merit = 25 },
            new TreasuryEntry { Item = "relic_r1", Merit = 120, ClanRank = 1 },
            new TreasuryEntry { Gu = "moon_shadow", Quality = 1, Merit = 180, ClanRank = 1 },
            new TreasuryEntry { Item = "relic_r2", Merit = 300, ClanRank = 2 },
            new TreasuryEntry { Gu = "golden_moon", Quality = 1, Merit = 420, ClanRank = 2 },
            new TreasuryEntry { Gu = "moon_lake", Quality = 1, Merit = 500, ClanRank = 2 },
            new TreasuryEntry { Item = "blood_crystal", Merit = 60, ClanRank = 2 },
            new TreasuryEntry { Item = "relic_r3", Merit = 700, ClanRank = 3 },
            new TreasuryEntry { Item = "manual_masters", Merit = 500, ClanRank = 3 },
            new TreasuryEntry { Item = "manual_sages", Merit = 1400, ClanRank = 4 },
            new TreasuryEntry { Item = "scroll_moon_sickle", Merit = 900, ClanRank = 3 },
            new TreasuryEntry { Item = "king_core", Merit = 250, ClanRank = 3 },
            new TreasuryEntry { Item = "relic_r4", Merit = 1600, ClanRank = 4 },
            new TreasuryEntry { Item = "heart_of_qingmao", Merit = 1800, ClanRank = 4 },
            new TreasuryEntry { Item = "scroll_full_moon", Merit = 2600, ClanRank = 4 },
        };

        public int ClanRank
        {
            get
            {
                var rank = 0;

                for (var i = 1; i < ClanRankNeeds.Length; i++)
                {
                    if (State.MeritEarned >= ClanRankNeeds[i])
                    {
                        rank = i;
                    }
                }

                return rank;
            }
        }

        public event Action<int> ClanRankRose;

        public void GainMerit(int merit)
        {
            if (merit <= 0 || InSandbox)
            {
                return;
            }

            var before = ClanRank;
            State.Merit += merit;
            State.MeritEarned += merit;

            if (ClanRank > before)
            {
                Say($"Клан Гу Юэ: вы теперь {ClanRanks[ClanRank].ToLowerInvariant()}");
                ClanRankRose?.Invoke(ClanRank);
                Achieve();
            }

            Touch();
        }

        public string WhyNoTreasury(TreasuryEntry entry)
        {
            if (ClanRank < entry.ClanRank)
            {
                return "Нужно быть: " + ClanRanks[entry.ClanRank].ToLowerInvariant();
            }

            return State.Merit < entry.Merit ? $"Нужно {entry.Merit} заслуг" : null;
        }

        public bool TakeFromTreasury(TreasuryEntry entry)
        {
            if (WhyNoTreasury(entry) != null)
            {
                return false;
            }

            State.Merit -= entry.Merit;

            if (entry.Gu != null)
            {
                AddGu(MakeGu(entry.Gu, entry.Quality, true));
            }
            else
            {
                AddItem(entry.Item);
            }

            Touch();

            return true;
        }
    }
}
