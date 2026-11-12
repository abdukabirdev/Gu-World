using System;
using System.Collections.Generic;

namespace GuWorld.Core
{
    /// <summary>A settlement's board: who it asks to be hunted and what it wants gathered.</summary>
    public class BoardDef
    {
        public string Settlement;
        public string Clan;
        public (string enemy, int min, int max)[] Hunts = new (string, int, int)[0];
        public (string item, int min, int max)[] Gathers = new (string, int, int)[0];
        public int Chapter = 3;
    }

    /// <summary>
    /// The errands of the settlements' boards. Each day every board posts three, made from the
    /// settlement and the day alone (the same day gives the same errands); one taken stays three days.
    /// </summary>
    public partial class Session
    {
        public static readonly BoardDef[] Boards =
        {
            new BoardDef
            {
                Settlement = "bai_village", Clan = "bai",
                Hunts = new[] { ("snow_wolf", 3, 6), ("mountain_eagle", 2, 4), ("cave_bat", 4, 8), ("rogue_master", 1, 3), ("wolf", 4, 8) },
                Gathers = new[] { ("frost_lotus", 2, 4), ("cloud_tea", 1, 3), ("spirit_moss", 1, 2), ("eagle_feather", 2, 4) },
            },
            new BoardDef
            {
                Settlement = "xiong_village", Clan = "xiong",
                Hunts = new[] { ("brown_bear", 2, 4), ("wild_boar", 3, 6), ("bee_swarm", 3, 5), ("black_tail_thug", 2, 4), ("wolf", 4, 8) },
                Gathers = new[] { ("bear_garlic", 3, 6), ("honey", 2, 4), ("iron_ore", 3, 6), ("bear_claw", 2, 3) },
            },
            new BoardDef
            {
                Settlement = "trade_post", Clan = "wanderers",
                Hunts = new[] { ("rogue_master", 2, 3), ("bandit", 3, 6), ("black_tail_thug", 2, 5), ("marsh_snake", 3, 6), ("centipede", 2, 3), ("marsh_toad", 3, 5) },
                Gathers = new[] { ("bitter_root", 3, 6), ("red_cap", 2, 4), ("snake_gall", 2, 4), ("ginseng", 1, 3), ("spider_silk", 2, 4) },
            },
            new BoardDef
            {
                Settlement = "gu_yue", Clan = "gu_yue",
                Hunts = new[] { ("wolf", 4, 8), ("fox", 4, 8), ("wild_boar", 3, 6), ("bandit", 2, 5), ("storm_wolf", 2, 4) },
                Gathers = new[] { ("moon_orchid", 3, 6), ("bitter_root", 3, 6), ("wolf_pelt", 2, 4), ("ginseng", 1, 2) },
            },
            // The masters' board in the Gu Yue town: the wild lands' beasts, for those of the fourth rank.
            new BoardDef
            {
                Settlement = "gu_yue_masters", Clan = "gu_yue",
                Hunts = new[] { ("frost_wolf", 3, 6), ("thunder_roc", 2, 4), ("jade_python", 2, 3), ("bone_soldier", 3, 6), ("fire_ape", 2, 4), ("lake_serpent", 2, 4), ("ice_bear", 1, 3) },
                Gathers = new[] { ("frost_lotus", 3, 6), ("spark_moss", 3, 6), ("king_core", 1, 2), ("storm_core", 3, 5) },
            },
        };

        public static BoardDef Board(string settlement) => Array.Find(Boards, b => b.Settlement == settlement);

        /// <summary>How long a taken errand waits for the hero, days.</summary>
        public const int BountyDays = 3;

        /// <summary>The errands a board shows today: those taken (still running) and today's new ones.</summary>
        public List<BountyState> BoardToday(string settlement)
        {
            var board = Board(settlement);
            var list = new List<BountyState>();

            if (board == null)
            {
                return list;
            }

            for (var n = 0; n < 3; n++)
            {
                var id = $"bounty:{settlement}:{State.Day}:{n}";
                var state = State.Bounties.Find(b => b.Id == id) ?? MakeBounty(board, State.Day, n);
                list.Add(state);
            }

            foreach (var taken in State.Bounties)
            {
                if (taken.Settlement == settlement && taken.Taken && !taken.Done && taken.Day != State.Day && !list.Exists(b => b.Id == taken.Id))
                {
                    list.Add(taken);
                }
            }

            return list;
        }

        /// <summary>The errand a board posts as the n-th of a day: the same for the same settlement, day and n.</summary>
        public static BountyState MakeBounty(BoardDef board, int day, int n)
        {
            var seed = Mix(board.Settlement.GetHashCodeStable() ^ (day * 7919) ^ (n * 104729));
            var hunt = n < 2 || board.Gathers.Length == 0;
            var bounty = new BountyState { Id = $"bounty:{board.Settlement}:{day}:{n}", Settlement = board.Settlement, Day = day };

            if (hunt)
            {
                var (enemy, min, max) = board.Hunts[seed % board.Hunts.Length];
                var def = Bestiary.Get(enemy);
                bounty.Kind = "kill";
                bounty.Target = enemy;
                bounty.Count = min + (seed / 7) % (max - min + 1);
                var level = def?.Level ?? 3;
                bounty.Stones = 4 + bounty.Count * (2 + level * 2);
                bounty.Insight = bounty.Count * (6 + level * 4);
            }
            else
            {
                var (item, min, max) = board.Gathers[seed % board.Gathers.Length];
                var def = ItemBook.Get(item);
                bounty.Kind = "gather";
                bounty.Target = item;
                bounty.Count = min + (seed / 7) % (max - min + 1);
                bounty.Stones = 6 + (int)(bounty.Count * (def?.Price ?? 8) * 1.4f);
                bounty.Insight = bounty.Count * 12;
            }

            bounty.Reputation = 3 + seed % 4;

            return bounty;
        }

        static int Mix(int n)
        {
            unchecked
            {
                var h = (uint)n;
                h ^= h >> 16;
                h *= 0x7feb352d;
                h ^= h >> 15;
                h *= 0x846ca68b;
                h ^= h >> 16;

                return (int)(h & 0x7FFFFFFF);
            }
        }

        public string BountyText(BountyState bounty)
        {
            if (bounty.Kind == "kill")
            {
                var name = Bestiary.Get(bounty.Target)?.Name ?? bounty.Target;

                return $"{name}: победить {bounty.Count}" + (bounty.Taken ? $" · {Math.Min(bounty.Progress, bounty.Count)} / {bounty.Count}" : "");
            }

            var item = ItemBook.Get(bounty.Target)?.Name ?? bounty.Target;

            return $"{item}: принести {bounty.Count}" + (bounty.Taken ? $" · {Math.Min(Count(bounty.Target), bounty.Count)} / {bounty.Count}" : "");
        }

        public bool TakeBounty(BountyState bounty)
        {
            if (bounty == null || bounty.Taken || bounty.Done)
            {
                return false;
            }

            var board = Board(bounty.Settlement);

            if (board != null && Hostile(board.Clan))
            {
                Say("Писарь отворачивается: клан не доверяет вам.");

                return false;
            }

            if (State.Bounties.FindAll(b => b.Taken && !b.Done).Count >= 6)
            {
                Say("Не больше шести поручений сразу.");

                return false;
            }

            bounty.Taken = true;
            State.Bounties.RemoveAll(b => b.Id == bounty.Id);
            State.Bounties.Add(bounty);
            Say($"Поручение взято: {BountyText(bounty)}");
            Touch();

            return true;
        }

        public bool BountyReady(BountyState bounty) => bounty.Taken && !bounty.Done && (bounty.Kind == "kill" ? bounty.Progress >= bounty.Count : Count(bounty.Target) >= bounty.Count);

        /// <summary>Hands in an errand at its board.</summary>
        public bool CompleteBounty(BountyState bounty)
        {
            if (bounty == null || !BountyReady(bounty))
            {
                return false;
            }

            if (bounty.Kind == "gather")
            {
                RemoveItem(bounty.Target, bounty.Count);
            }

            bounty.Done = true;
            AddStones(bounty.Stones);
            GainInsight(bounty.Insight);
            var board = Board(bounty.Settlement);

            if (board != null)
            {
                ChangeReputation(board.Clan, bounty.Reputation, false);
            }

            SetFlag("bounties_done");
            Touch();

            return true;
        }

        /// <summary>A kill counts for the errands that want it.</summary>
        void BountyKill(string enemy)
        {
            foreach (var bounty in State.Bounties)
            {
                if (bounty.Taken && !bounty.Done && bounty.Kind == "kill" && Matches(bounty.Target, enemy))
                {
                    bounty.Progress++;

                    if (bounty.Progress == bounty.Count)
                    {
                        Say($"Поручение готово: {BountyText(bounty)}");
                    }
                }
            }
        }

        /// <summary>At dawn: errands past their days are dropped; done ones are forgotten.</summary>
        void Bounties()
        {
            State.Bounties.RemoveAll(b => b.Done || (b.Taken && State.Day - b.Day > BountyDays) || !b.Taken);
        }

        public int BountiesRunning => State.Bounties.FindAll(b => b.Taken && !b.Done).Count;
    }

    static class StableHash
    {
        /// <summary>A string's hash that is the same in every run (string.GetHashCode is not, in .NET Core).</summary>
        public static int GetHashCodeStable(this string text)
        {
            unchecked
            {
                var hash = 2166136261u;

                foreach (var c in text)
                {
                    hash = (hash ^ c) * 16777619u;
                }

                return (int)(hash & 0x7FFFFFFF);
            }
        }
    }
}
