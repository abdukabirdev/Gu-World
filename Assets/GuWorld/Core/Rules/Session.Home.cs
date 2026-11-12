using System;
using System.Collections.Generic;

namespace GuWorld.Core
{
    /// <summary>
    /// A house of the hero's own: bought from the clan (cheaper to the honoured), with a bed (sleep till
    /// morning, whole again), a store that keeps what the bag cannot, and a nursery where Gu wait out of
    /// the aperture and are fed from the store each day.
    /// </summary>
    public partial class Session
    {
        /// <summary>What a house costs: by the clan's regard (an honoured member pays two thirds).</summary>
        public int HomePrice(ThingDef home)
        {
            var price = home != null && int.TryParse(home.Content, out var p) ? p : 800;
            var standing = home != null && !string.IsNullOrEmpty(home.Home) ? StandingWith(Regions.Settlement(home.Home)?.Clan ?? "gu_yue") : Standing.Neutral;

            return standing >= Standing.Honoured ? price * 2 / 3 : standing >= Standing.Friendly ? price * 5 / 6 : price;
        }

        public bool OwnsHome(string id) => State.Homes.Contains(id);

        public bool OwnsAnyHome => State.Homes.Count > 0;

        public string WhyNoHome(ThingDef home)
        {
            if (home == null || home.Kind != ThingKind.Home)
            {
                return "Это не дом";
            }

            if (OwnsHome(home.Id))
            {
                return "Дом уже ваш";
            }

            var clan = Regions.Settlement(home.Home)?.Clan ?? "";

            if (clan != "" && StandingWith(clan) < Standing.Neutral)
            {
                return "Клан не продаст дом чужаку";
            }

            return State.Stones < HomePrice(home) ? $"Нужно {HomePrice(home)} камней" : null;
        }

        public bool BuyHome(ThingDef home)
        {
            if (WhyNoHome(home) != null)
            {
                return false;
            }

            State.Stones -= HomePrice(home);
            State.Homes.Add(home.Id);
            Say($"{home.Name} теперь ваш");
            Achieve();
            Touch();

            return true;
        }

        // ------------------------------------------------------------------ The store

        public int StashCount(string item)
        {
            var stack = State.Stash.Find(s => s.Item == item);

            return stack != null ? stack.Count : 0;
        }

        /// <summary>Puts items from the bag into the store.</summary>
        public bool StashPut(string item, int count)
        {
            count = Math.Min(count, Count(item));

            if (!OwnsAnyHome || count <= 0)
            {
                return false;
            }

            RemoveItem(item, count);
            var stack = State.Stash.Find(s => s.Item == item);

            if (stack == null)
            {
                State.Stash.Add(new ItemStack { Item = item, Count = count });
            }
            else
            {
                stack.Count += count;
            }

            Touch();

            return true;
        }

        /// <summary>Takes items out of the store (into the bag, unless `toBag` is false: then they are used up).</summary>
        public bool StashTake(string item, int count, bool toBag = true)
        {
            var stack = State.Stash.Find(s => s.Item == item);

            if (stack == null || count <= 0)
            {
                return false;
            }

            count = Math.Min(count, stack.Count);
            stack.Count -= count;

            if (stack.Count <= 0)
            {
                State.Stash.Remove(stack);
            }

            if (toBag)
            {
                AddItem(item, count, true);
            }

            Touch();

            return true;
        }

        // ------------------------------------------------------------------ The nursery

        public bool InNursery(int uid) => State.Nursery.Contains(uid);

        /// <summary>Takes a Gu out of the aperture into the nursery (it leaves its slot).</summary>
        public bool NurseryPut(int uid)
        {
            var gu = FindGu(uid);

            if (!OwnsAnyHome || gu == null || InNursery(uid) || State.Nursery.Count >= NurserySize)
            {
                return false;
            }

            Unslot(uid);
            State.Nursery.Add(uid);
            Recompute();
            Touch();

            return true;
        }

        public bool NurseryTake(int uid)
        {
            if (!State.Nursery.Remove(uid))
            {
                return false;
            }

            Touch();

            return true;
        }

        /// <summary>How many Gu a nursery holds: more for each house.</summary>
        public int NurserySize => 6 * Math.Max(1, State.Homes.Count);

        /// <summary>Sleeps in the house till morning: whole again, and rested (a while of quicker essence).</summary>
        public void SleepAtHome()
        {
            if (!OwnsAnyHome)
            {
                return;
            }

            SleepTillMorning();
            Restore();
            CureAll();
            AddEffect(new Effect { Source = "rested", Name = "Отдохнувший", IsStat = true, Stat = StatKind.EssenceRegen, Value = 0.3f, Seconds = 600 });
            Touch();
        }
    }
}
