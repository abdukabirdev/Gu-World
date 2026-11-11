using System;
using System.Collections.Generic;

namespace GuWorld.Core
{
    /// <summary>
    /// The days of the game: the clock, dawn (Gu grow hungry and eat from the bag, the boards post new
    /// errands, the shrines bless again), sleeping at an inn, and the hunger of Gu under the rule the
    /// player chose — none, soft (a hungry Gu is weaker) or the canon's (a starved Gu dies).
    /// </summary>
    public partial class Session
    {
        /// <summary>A new day began (its number).</summary>
        public event Action<int> Dawn;
        /// <summary>The clock was moved by the rules (sleep): the interface's sky should follow.</summary>
        public event Action<float> ClockSet;

        /// <summary>How many days past its appetite a Gu lives unfed under the canon's rule.</summary>
        public const int StarveDays = 3;

        /// <summary>The interface's clock moved on: a new day if the hour went round midnight.</summary>
        public void SetClock(float hour)
        {
            hour = ((hour % 24) + 24) % 24;

            if (hour + 12 < State.Hour)
            {
                State.Hour = hour;
                NewDay();
            }
            else
            {
                State.Hour = hour;
            }
        }

        /// <summary>Sleeps some hours (an inn's bed): whole again, and as many dawns as pass.</summary>
        public void Sleep(int hours)
        {
            hours = Math.Max(1, Math.Min(24, hours));
            var hour = State.Hour + hours;

            while (hour >= 24)
            {
                hour -= 24;
                State.Hour = 0;
                NewDay();
            }

            State.Hour = hour;
            Restore();
            ClockSet?.Invoke(State.Hour);
            Say($"Вы проспали {hours} ч.");
            Touch();
        }

        /// <summary>Sleeps until the next morning (six o'clock).</summary>
        public void SleepTillMorning()
        {
            var hours = (int)Math.Ceiling((24 - State.Hour + 6) % 24);
            Sleep(hours == 0 ? 24 : hours);
        }

        public void NewDay()
        {
            State.Day++;
            State.Prayed.Clear();
            Hunger();
            Bounties();
            Dawn?.Invoke(State.Day);
            Touch();
        }

        // ------------------------------------------------------------------ Hunger

        /// <summary>A Gu needs feeding: its days unfed have reached its appetite.</summary>
        public bool IsHungry(GuInstance gu)
        {
            var def = GuBook.Get(gu.Def);

            return State.Settings.Hunger != HungerRule.Off && def != null && def.Food != "" && gu.Hunger >= def.Appetite;
        }

        /// <summary>A Gu has gone unfed past its appetite and is weak from it.</summary>
        public bool IsStarving(GuInstance gu)
        {
            var def = GuBook.Get(gu.Def);

            return State.Settings.Hunger != HungerRule.Off && def != null && def.Food != "" && gu.Hunger > def.Appetite;
        }

        /// <summary>A starving Gu's power, as a share (a fed one: 1).</summary>
        public float HungerFactor(GuInstance gu) => IsStarving(gu) ? 0.7f : 1;

        /// <summary>Days a Gu has left before it dies of hunger (canon rule), or −1 if it cannot die of it.</summary>
        public int DaysToStarve(GuInstance gu)
        {
            var def = GuBook.Get(gu.Def);

            if (State.Settings.Hunger != HungerRule.Canon || def == null || def.Food == "")
            {
                return -1;
            }

            return Math.Max(0, def.Appetite + StarveDays - gu.Hunger);
        }

        /// <summary>Feeds a Gu from the bag. False if it needs nothing or there is no food for it.</summary>
        public bool Feed(int uid)
        {
            var gu = FindGu(uid);
            var def = gu != null ? GuBook.Get(gu.Def) : null;

            if (def == null || def.Food == "" || gu.Hunger == 0 || Count(def.Food) <= 0)
            {
                return false;
            }

            RemoveItem(def.Food);
            gu.Hunger = 0;
            Recompute();
            Touch();

            return true;
        }

        /// <summary>Feeds every Gu that is hungry and has food in the bag. How many ate.</summary>
        public int FeedAll(bool onlyHungry = true)
        {
            var fed = 0;

            foreach (var gu in new List<GuInstance>(State.Gu))
            {
                if ((!onlyHungry || IsHungry(gu)) && Feed(gu.Uid))
                {
                    fed++;
                }
            }

            return fed;
        }

        /// <summary>What every Gu owned eats in a day of play, on average: (food, portions a day).</summary>
        public Dictionary<string, float> DailyFood()
        {
            var food = new Dictionary<string, float>();

            foreach (var gu in State.Gu)
            {
                var def = GuBook.Get(gu.Def);

                if (def == null || def.Food == "")
                {
                    continue;
                }

                food.TryGetValue(def.Food, out var already);
                food[def.Food] = already + 1f / Math.Max(1, def.Appetite);
            }

            return food;
        }

        void Hunger()
        {
            if (State.Settings.Hunger == HungerRule.Off)
            {
                return;
            }

            var starved = new List<GuInstance>();

            foreach (var gu in State.Gu)
            {
                var def = GuBook.Get(gu.Def);

                if (def == null || def.Food == "")
                {
                    continue;
                }

                gu.Hunger++;

                if (State.Settings.AutoFeed && gu.Hunger >= def.Appetite && Count(def.Food) > 0)
                {
                    RemoveItem(def.Food);
                    gu.Hunger = 0;
                    continue;
                }

                if (State.Settings.Hunger == HungerRule.Canon && gu.Hunger > def.Appetite + StarveDays)
                {
                    starved.Add(gu);
                }
                else if (gu.Hunger == def.Appetite + 1)
                {
                    Say($"{def.Name} голоден: нужен корм — {ItemBook.Get(def.Food)?.Name ?? def.Food}");
                }
            }

            foreach (var gu in starved)
            {
                // The Blood Moon cannot die: it eats its master instead.
                if (gu.Def == "blood_moon")
                {
                    State.Health = Math.Max(1, State.Health - Stats.MaxHealth * 0.3f);
                    gu.Hunger = 0;
                    Say("Гу Кровавой луны насытился вашей кровью.");
                    continue;
                }

                Unslot(gu.Uid);
                State.Gu.Remove(gu);
                Say($"{GuBook.Get(gu.Def).Name} умер от голода.");
            }

            Recompute();
        }

        // ------------------------------------------------------------------ Shrines

        public bool PrayedToday(string shrine) => State.Prayed.Contains(shrine);

        /// <summary>Prays at a shrine: its blessing for some minutes, once a day.</summary>
        public string Pray(ThingDef shrine)
        {
            if (PrayedToday(shrine.Id))
            {
                return "Сегодня вы уже молились здесь.";
            }

            var blessing = Codex.Blessing(shrine.Content);

            if (blessing == null)
            {
                return "";
            }

            State.Prayed.Add(shrine.Id);
            AddEffect(new Effect { Source = blessing.Id, Name = blessing.Name, IsStat = true, Stat = blessing.Stat, Value = blessing.Value, Seconds = blessing.Minutes * 60 });

            if (shrine.Id == "shrine_peak")
            {
                SetFlag("peak_blessed");
            }

            return $"{blessing.Name}: {blessing.Text}";
        }
    }
}
