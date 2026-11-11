using System;
using System.Collections.Generic;

namespace GuWorld.Core
{
    public partial class Session
    {
        // ------------------------------------------------------------------ Items

        public int Count(string item)
        {
            foreach (var stack in State.Items)
            {
                if (stack.Item == item)
                {
                    return stack.Count;
                }
            }

            return 0;
        }

        public void AddItem(string item, int count = 1, bool quiet = false)
        {
            if (count <= 0 || ItemBook.Get(item) == null)
            {
                return;
            }

            var stack = State.Items.Find(s => s.Item == item);

            if (stack == null)
            {
                State.Items.Add(new ItemStack { Item = item, Count = count });
            }
            else
            {
                stack.Count += count;
            }

            if (!quiet)
            {
                Say($"+{count} {ItemBook.Get(item).Name}");
            }

            RefreshQuests();
            Touch();
        }

        public bool RemoveItem(string item, int count = 1)
        {
            var stack = State.Items.Find(s => s.Item == item);

            if (stack == null || stack.Count < count)
            {
                return false;
            }

            stack.Count -= count;

            if (stack.Count <= 0)
            {
                State.Items.Remove(stack);
            }

            Touch();

            return true;
        }

        public void AddStones(int count, bool quiet = false)
        {
            if (count <= 0)
            {
                return;
            }

            State.Stones += count;

            if (!quiet)
            {
                Say($"+{count} первобытных камней");
            }

            Touch();
        }

        /// <summary>Swallows a pill. False if there is none or it would do nothing.</summary>
        public bool UseConsumable(string item)
        {
            var def = ItemBook.Get(item);

            if (def == null || def.Kind != ItemKind.Consumable || Count(item) <= 0 || Dead)
            {
                return false;
            }

            var useful = (def.Heal > 0 && State.Health < Stats.MaxHealth - 0.5f) || (def.Essence > 0 && State.Essence < Stats.MaxEssence - 0.5f);

            if (!useful)
            {
                return false;
            }

            RemoveItem(item);
            Heal(def.Heal);
            State.Essence = Math.Min(Stats.MaxEssence, State.Essence + def.Essence * Stats.MaxEssence);

            return true;
        }

        /// <summary>Draws a primeval stone's essence into the aperture: a quarter of it per stone.</summary>
        public bool AbsorbStone()
        {
            if (State.Stones <= 0 || State.Essence >= Stats.MaxEssence - 0.5f)
            {
                return false;
            }

            State.Stones--;
            State.Essence = Math.Min(Stats.MaxEssence, State.Essence + Stats.MaxEssence * 0.25f);
            Touch();

            return true;
        }

        public bool KnowsRecipe(string id)
        {
            var recipe = Recipes.Get(id);

            return recipe != null && (recipe.Known || State.Recipes.Contains(id));
        }

        public bool ReadScroll(string item)
        {
            var def = ItemBook.Get(item);

            if (def == null || def.Kind != ItemKind.Scroll || Count(item) <= 0)
            {
                return false;
            }

            RemoveItem(item);

            if (!State.Recipes.Contains(def.Teaches))
            {
                State.Recipes.Add(def.Teaches);
                Say($"Изучен рецепт: {GuBook.Get(Recipes.Get(def.Teaches).Result).Name}");
            }
            else
            {
                Say("Этот рецепт вы уже знаете");
            }

            Touch();

            return true;
        }

        // ------------------------------------------------------------------ Gu

        public GuInstance FindGu(int uid) => uid == 0 ? null : State.Gu.Find(g => g.Uid == uid);

        /// <summary>A new Gu of a kind, with affixes rolled for its quality (not yet the hero's).</summary>
        public GuInstance MakeGu(string defId, int quality, bool refined)
        {
            var def = GuBook.Get(defId);
            var gu = new GuInstance { Def = defId, Quality = Math.Max(0, Math.Min(2, quality)), Refined = refined };
            var pool = def.Kind == GuKind.Passive
                ? new List<AffixKind> { AffixKind.Sturdy, AffixKind.Wellspring }
                : def.Skill.Power > 0 && def.Skill.Shape != SkillShape.Heal
                    ? new List<AffixKind> { AffixKind.Sharp, AffixKind.Thrifty, AffixKind.Quick, AffixKind.Far, AffixKind.Leech, AffixKind.Sturdy, AffixKind.Wellspring }
                    : new List<AffixKind> { AffixKind.Thrifty, AffixKind.Quick, AffixKind.Sturdy, AffixKind.Wellspring };

            for (var i = 0; i < gu.Quality && pool.Count > 0; i++)
            {
                var kind = pool[Random.Next(pool.Count)];
                pool.Remove(kind);
                var roll = (float)Random.NextDouble();
                var value = kind == AffixKind.Sturdy ? (float)Math.Round(10 + 15 * roll)
                    : kind == AffixKind.Far ? 0.10f + 0.15f * roll
                    : kind == AffixKind.Leech ? 0.03f + 0.03f * roll
                    : kind == AffixKind.Wellspring ? 0.05f + 0.10f * roll
                    : 0.08f + 0.10f * roll;
                gu.Affixes.Add(new Affix { Kind = kind, Value = (float)Math.Round(value, 2) });
            }

            return gu;
        }

        public GuInstance AddGu(GuInstance gu, bool quiet = false)
        {
            gu.Uid = State.NextUid++;
            State.Gu.Add(gu);

            if (!quiet)
            {
                var def = GuBook.Get(gu.Def);
                Say($"Получен {def.Name}" + (gu.Quality > 0 ? $" ({GuBook.QualityNames[gu.Quality]})" : "") + (gu.Refined ? "" : " — дикий, очистите его"));
            }

            Touch();

            return gu;
        }

        public float RefineCost(GuInstance gu)
        {
            var rank = GuBook.Get(gu.Def).Rank;

            return 30 * rank * rank;
        }

        /// <summary>Why a wild Gu cannot be refined now, or null if it can.</summary>
        public string WhyNoRefine(GuInstance gu)
        {
            if (gu == null || gu.Refined)
            {
                return "Этот Гу уже ваш";
            }

            var cost = RefineCost(gu);

            if (cost > Stats.MaxEssence)
            {
                return "Апертура мала для этого Гу. Нужен ранг выше";
            }

            return State.Essence < cost ? $"Нужно эссенции: {(int)cost}" : null;
        }

        public bool Refine(int uid)
        {
            var gu = FindGu(uid);

            if (WhyNoRefine(gu) != null)
            {
                return false;
            }

            State.Essence -= RefineCost(gu);
            gu.Refined = true;
            Say($"{GuBook.Get(gu.Def).Name} очищен и слушается вас");
            RefreshQuests();
            Touch();

            return true;
        }

        public bool IsSlotted(int uid) => uid != 0 && (Array.IndexOf(State.Active, uid) >= 0 || Array.IndexOf(State.Passive, uid) >= 0);

        /// <summary>Why a Gu cannot go in a slot, or null if it can.</summary>
        public string WhyNoEquip(GuInstance gu, bool passive, int slot)
        {
            if (gu == null)
            {
                return "Нет такого Гу";
            }

            var def = GuBook.Get(gu.Def);

            if (!gu.Refined)
            {
                return "Сначала очистите Гу";
            }

            if ((def.Kind == GuKind.Passive) != passive)
            {
                return passive ? "Это Гу для активного слота" : "Это пассивный Гу";
            }

            if (def.Rank > Rank)
            {
                return $"Нужен ранг {GuBook.RankNumerals[def.Rank]}";
            }

            var count = passive ? Stats.PassiveSlots : Stats.ActiveSlots;

            return slot < 0 || slot >= count ? "Этот слот ещё закрыт" : null;
        }

        public bool Equip(int uid, bool passive, int slot)
        {
            var gu = FindGu(uid);

            if (WhyNoEquip(gu, passive, slot) != null)
            {
                return false;
            }

            Unslot(uid);
            var slots = passive ? State.Passive : State.Active;
            slots[slot] = uid;
            Recompute();
            RefreshQuests();
            Touch();

            return true;
        }

        /// <summary>Puts a Gu in the first free slot of its kind (or the first slot if all are taken).</summary>
        public bool EquipAuto(int uid)
        {
            var gu = FindGu(uid);

            if (gu == null)
            {
                return false;
            }

            var passive = GuBook.Get(gu.Def).Kind == GuKind.Passive;
            var slots = passive ? State.Passive : State.Active;
            var count = passive ? Stats.PassiveSlots : Stats.ActiveSlots;
            var slot = 0;

            for (var i = 0; i < count; i++)
            {
                if (slots[i] == 0)
                {
                    slot = i;
                    break;
                }
            }

            return Equip(uid, passive, slot);
        }

        public void Unslot(int uid)
        {
            for (var i = 0; i < State.Active.Length; i++)
            {
                if (State.Active[i] == uid)
                {
                    State.Active[i] = 0;
                }
            }

            for (var i = 0; i < State.Passive.Length; i++)
            {
                if (State.Passive[i] == uid)
                {
                    State.Passive[i] = 0;
                }
            }

            Recompute();
            Touch();
        }

        void RemoveGu(GuInstance gu)
        {
            Unslot(gu.Uid);
            State.Gu.Remove(gu);
        }

        // ------------------------------------------------------------------ Loot

        public Loot RollLoot(string tableId)
        {
            var loot = new Loot();
            var table = Bestiary.Table(tableId);

            if (table == null)
            {
                return loot;
            }

            loot.Stones = table.StonesMax > 0 ? Random.Next(table.StonesMin, table.StonesMax + 1) : 0;

            foreach (var entry in table.Items)
            {
                if (Random.NextDouble() < entry.Chance)
                {
                    loot.Items.Add(new ItemStack { Item = entry.Item, Count = Random.Next(entry.Min, entry.Max + 1) });
                }
            }

            if (table.GuPool.Length > 0 && Random.NextDouble() < table.GuChance)
            {
                var roll = Random.NextDouble();
                var quality = roll < 0.05 ? 2 : roll < 0.30 ? 1 : 0;
                loot.Gu.Add(MakeGu(table.GuPool[Random.Next(table.GuPool.Length)], Math.Max(quality, table.GuMinQuality), false));
            }

            return loot;
        }

        public void Collect(Loot loot)
        {
            AddStones(loot.Stones);

            foreach (var stack in loot.Items)
            {
                AddItem(stack.Item, stack.Count);
            }

            foreach (var gu in loot.Gu)
            {
                AddGu(gu);
            }
        }

        /// <summary>Cracks a gambling rock from the bag open.</summary>
        public Loot CrackRock(string item)
        {
            var def = ItemBook.Get(item);

            if (def == null || def.Kind != ItemKind.Rock || !RemoveItem(item))
            {
                return null;
            }

            var loot = new Loot();
            var roll = Random.NextDouble();
            var tier = def.Tier;
            var empty = tier == 1 ? 0.42 : tier == 2 ? 0.34 : 0.28;
            var stones = empty + 0.30;

            if (roll < empty)
            {
                Say("Пустая порода");
            }
            else if (roll < stones)
            {
                loot.Stones = tier == 1 ? Random.Next(10, 46) : tier == 2 ? Random.Next(40, 121) : Random.Next(150, 481);
            }
            else
            {
                var high = Random.NextDouble() < (tier == 1 ? 0.15 : tier == 2 ? 0.1 : 0.3);
                var rank = Math.Min(3, tier + (high ? 1 : 0));

                if (tier == 3 && !high)
                {
                    rank = 2;
                }

                var pool = new List<string>();

                foreach (var gu in GuBook.All)
                {
                    if (gu.Rank == rank && gu.Path != GuPath.Blood && gu.Id != "liquor_worm" && gu.Id != "four_wines" && gu.Id != "moonlight")
                    {
                        pool.Add(gu.Id);
                    }
                }

                var quality = Random.NextDouble() < 0.25 + 0.15 * tier ? (Random.NextDouble() < 0.2 ? 2 : 1) : 0;
                loot.Gu.Add(MakeGu(pool[Random.Next(pool.Count)], quality, false));
            }

            Collect(loot);

            return loot;
        }

        // ------------------------------------------------------------------ Fusion

        /// <summary>The Gu a recipe would take (the best of each kind), or null with the reason it cannot be done.</summary>
        public List<GuInstance> FusionInputs(RecipeDef recipe, out string why)
        {
            why = null;
            var picked = new List<GuInstance>();

            if (!KnowsRecipe(recipe.Id))
            {
                why = "Рецепт не изучен";
                return null;
            }

            if (GuBook.Get(recipe.Result).Rank > Rank)
            {
                why = $"Нужен ранг {GuBook.RankNumerals[GuBook.Get(recipe.Result).Rank]}";
                return null;
            }

            foreach (var need in recipe.Gu)
            {
                GuInstance best = null;

                foreach (var gu in State.Gu)
                {
                    if (gu.Def == need && gu.Refined && !picked.Contains(gu) && (best == null || gu.Quality > best.Quality || (gu.Quality == best.Quality && gu.MasteryXp > best.MasteryXp)))
                    {
                        best = gu;
                    }
                }

                if (best == null)
                {
                    why = $"Нет очищенного: {GuBook.Get(need).Name}";
                    return null;
                }

                picked.Add(best);
            }

            foreach (var (item, count) in recipe.Items)
            {
                if (Count(item) < count)
                {
                    why = $"Не хватает: {ItemBook.Get(item).Name} ({Count(item)} / {count})";
                    return null;
                }
            }

            if (State.Stones < recipe.Stones)
            {
                why = $"Нужно камней: {recipe.Stones}";
                return null;
            }

            return picked;
        }

        public GuInstance Fuse(string recipeId)
        {
            var recipe = Recipes.Get(recipeId);

            if (recipe == null)
            {
                return null;
            }

            var inputs = FusionInputs(recipe, out _);

            if (inputs == null)
            {
                return null;
            }

            var quality = 0;
            var xp = 0;
            var activeSlot = -1;
            var passiveSlot = -1;

            foreach (var gu in inputs)
            {
                quality = Math.Max(quality, gu.Quality);
                xp = Math.Max(xp, gu.MasteryXp);
                var a = Array.IndexOf(State.Active, gu.Uid);
                var p = Array.IndexOf(State.Passive, gu.Uid);
                activeSlot = a >= 0 && activeSlot < 0 ? a : activeSlot;
                passiveSlot = p >= 0 && passiveSlot < 0 ? p : passiveSlot;
                RemoveGu(gu);
            }

            foreach (var (item, count) in recipe.Items)
            {
                RemoveItem(item, count);
            }

            State.Stones -= recipe.Stones;

            if (quality < 2 && Random.NextDouble() < 0.25)
            {
                quality++;
            }

            var result = MakeGu(recipe.Result, quality, true);
            result.MasteryXp = xp / 2;
            AddGu(result, true);
            Say($"Слияние удалось: {GuBook.Get(recipe.Result).Name}" + (quality > 0 ? $" ({GuBook.QualityNames[quality]})" : ""));

            // It takes the place of the Gu it was made from, if it fits there.
            var passive = GuBook.Get(recipe.Result).Kind == GuKind.Passive;
            var slot = passive ? passiveSlot : activeSlot;

            if (slot >= 0)
            {
                Equip(result.Uid, passive, slot);
            }

            Notify(StepKind.Fuse, recipe.Result);
            Touch();

            return result;
        }

        // ------------------------------------------------------------------ Trade

        public List<ShopEntry> Shelf(ShopEntry[] shop)
        {
            var shelf = new List<ShopEntry>();

            foreach (var entry in shop)
            {
                if (entry.Chapter <= Chapter)
                {
                    shelf.Add(entry);
                }
            }

            return shelf;
        }

        public bool Buy(ShopEntry entry)
        {
            if (entry == null || State.Stones < entry.Price)
            {
                return false;
            }

            State.Stones -= entry.Price;

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

        public int SellPrice(string item)
        {
            var def = ItemBook.Get(item);

            return def == null || def.Kind == ItemKind.Quest ? 0 : Math.Max(1, (int)(def.Price * 0.4f));
        }

        public int SellPrice(GuInstance gu) => (int)(GuBook.Get(gu.Def).Price * (1 + 0.5f * gu.Quality) * 0.5f);

        public bool Sell(string item, int count = 1)
        {
            var price = SellPrice(item);

            if (price <= 0 || !RemoveItem(item, count))
            {
                return false;
            }

            AddStones(price * count, true);

            return true;
        }

        public bool SellGu(int uid)
        {
            var gu = FindGu(uid);

            if (gu == null || GuBook.Get(gu.Def).Price <= 0)
            {
                return false;
            }

            var price = SellPrice(gu);
            RemoveGu(gu);
            AddStones(price, true);

            return true;
        }
    }
}
