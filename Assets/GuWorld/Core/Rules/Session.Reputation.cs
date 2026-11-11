using System;

namespace GuWorld.Core
{
    /// <summary>
    /// What the clans think of the hero. A gain with one clan is a small loss with its rivals (the
    /// mountain is not big enough for everyone's friend); killing a clan's people is a great loss with
    /// it and a little gain with those who hate it; shops price by it and guards act on it.
    /// </summary>
    public partial class Session
    {
        public event Action<string, Standing> StandingChanged;

        public int ReputationOf(string clan)
        {
            var entry = State.Reputation.Find(r => r.Clan == clan);

            if (entry != null)
            {
                return entry.Value;
            }

            return Clans.Get(clan)?.Start ?? 0;
        }

        public Standing StandingWith(string clan) => Clans.StandingOf(ReputationOf(clan));

        /// <summary>Changes what a clan thinks (rivals feel a fifth of a gain the other way, unless quiet).</summary>
        public void ChangeReputation(string clan, int change, bool quiet)
        {
            var def = Clans.Get(clan);

            if (def == null || change == 0)
            {
                return;
            }

            Set(clan, ReputationOf(clan) + change, quiet);

            // A friend of my rival is no friend of mine — a little.
            if (change > 0)
            {
                foreach (var rival in def.Rivals)
                {
                    var loss = Math.Max(1, change / 5);
                    var other = Clans.Get(rival);

                    // Clans that are already friends of the hero do not grudge each other as much.
                    if (other != null && StandingWith(rival) < Standing.Honoured)
                    {
                        Set(rival, ReputationOf(rival) - loss, true);
                    }
                }
            }

            Touch();
        }

        void Set(string clan, int value, bool quiet)
        {
            value = Math.Max(Clans.Min, Math.Min(Clans.Max, value));
            var before = ReputationOf(clan);
            var entry = State.Reputation.Find(r => r.Clan == clan);

            if (entry == null)
            {
                State.Reputation.Add(entry = new Reputation { Clan = clan });
            }

            entry.Value = value;
            var was = Clans.StandingOf(before);
            var now = Clans.StandingOf(value);

            if (!quiet && value != before)
            {
                var name = Clans.Get(clan)?.Name ?? clan;
                Say($"{name}: {(value > before ? "+" : "")}{value - before}");
            }

            if (was != now)
            {
                Say($"{Clans.Get(clan)?.Name ?? clan}: {Clans.StandingName(now)}");
                StandingChanged?.Invoke(clan, now);
            }

            Achieve();
        }

        /// <summary>The hero killed someone of a clan: a loss with it, and a gain with those that hate it.</summary>
        void KilledOf(string clan, bool boss)
        {
            var def = Clans.Get(clan);

            if (def == null)
            {
                return;
            }

            var loss = boss ? 15 : clan == "bandit" ? 2 : 6;
            Set(clan, ReputationOf(clan) - loss, true);

            foreach (var other in Clans.All)
            {
                // Those who count the clan their rival, and those the clan counts its rival, are glad.
                if (other.Id != clan && (Array.IndexOf(other.Rivals, clan) >= 0 || Array.IndexOf(def.Rivals, other.Id) >= 0))
                {
                    Set(other.Id, ReputationOf(other.Id) + (clan == "bandit" ? 1 : 2), true);
                }
            }
        }

        /// <summary>Whether a clan's patrols and guards attack the hero.</summary>
        public bool Hostile(string clan) => !string.IsNullOrEmpty(clan) && (clan == "bandit" ? ReputationOf(clan) < 0 : StandingWith(clan) == Standing.Hated);

        /// <summary>What a shop of a clan asks for an entry.</summary>
        public int PriceFor(ShopEntry entry, string clan)
        {
            var factor = string.IsNullOrEmpty(clan) ? 1 : Clans.PriceFactor(ReputationOf(clan));

            // The difficulty: dearer on harder ones.
            factor *= DifficultyRules.Prices(State.Settings.Difficulty);

            return Math.Max(1, (int)Math.Round(entry.Price * factor));
        }

        /// <summary>Whether someone will trade with the hero at all.</summary>
        public bool WillTrade(NpcDef npc) => npc == null || Clans.Trades(ReputationOf(npc.Clan));

        /// <summary>Buys from a clan's shop at its price for the hero.</summary>
        public bool Buy(ShopEntry entry, string clan)
        {
            if (entry == null)
            {
                return false;
            }

            var price = PriceFor(entry, clan);

            if (State.Stones < price)
            {
                return false;
            }

            State.Stones -= price;

            if (entry.Gu != null)
            {
                AddGu(MakeGu(entry.Gu, 0, true));
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
