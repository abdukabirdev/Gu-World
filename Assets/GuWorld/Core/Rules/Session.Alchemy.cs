using System;
using System.Collections.Generic;

namespace GuWorld.Core
{
    /// <summary>What a batch at the furnace came to.</summary>
    public class Batch
    {
        public bool Ok;
        public string Why = "";
        public string Result;
        public int Count;
        public bool Bonus;
        public bool SkillUp;
    }

    /// <summary>
    /// Refining pills at a furnace: recipes are known from the start, bought from teachers or read in
    /// books; a batch takes its herbs and stones; the refiner's skill decides whether it comes out and
    /// grows with every batch.
    /// </summary>
    public partial class Session
    {
        public bool KnowsPill(string id)
        {
            var recipe = Alchemy.Get(id);

            return recipe != null && (recipe.Known || State.Pills.Contains(id) || Has("learnt_" + id));
        }

        public void LearnPill(string id)
        {
            var recipe = Alchemy.Get(id);

            if (recipe == null || KnowsPill(id) && !recipe.Known && State.Pills.Contains(id))
            {
                return;
            }

            if (!State.Pills.Contains(id))
            {
                State.Pills.Add(id);
            }

            SetFlag("learnt_" + id);
            Say($"Рецепт пилюли: {ItemBook.Get(recipe.Result)?.Name ?? id}");
            Touch();
        }

        /// <summary>The recipes the hero knows, in the book's order.</summary>
        public List<PillRecipeDef> KnownPills()
        {
            var known = new List<PillRecipeDef>();

            foreach (var recipe in Alchemy.All)
            {
                if (KnowsPill(recipe.Id))
                {
                    known.Add(recipe);
                }
            }

            return known;
        }

        /// <summary>Why a batch cannot be made now, or null if it can.</summary>
        public string WhyNoBatch(PillRecipeDef recipe)
        {
            if (recipe == null)
            {
                return "Нет такого рецепта.";
            }

            if (!KnowsPill(recipe.Id))
            {
                return "Рецепт неизвестен.";
            }

            foreach (var (item, count) in recipe.Items)
            {
                if (Count(item) < count)
                {
                    return $"Не хватает: {ItemBook.Get(item)?.Name ?? item} ({Count(item)} / {count})";
                }
            }

            if (State.Stones < recipe.Stones)
            {
                return $"Нужно {recipe.Stones} камней.";
            }

            return null;
        }

        public float BatchChance(PillRecipeDef recipe) => Alchemy.SuccessChance(State.Refining, recipe.Difficulty);

        /// <summary>Refines one batch: takes what it needs; the pills come out (or soot).</summary>
        public Batch Brew(string recipeId)
        {
            var recipe = Alchemy.Get(recipeId);
            var why = WhyNoBatch(recipe);

            if (why != null)
            {
                return new Batch { Why = why };
            }

            foreach (var (item, count) in recipe.Items)
            {
                RemoveItem(item, count);
            }

            State.Stones -= recipe.Stones;
            var batch = new Batch { Result = recipe.Result };
            batch.Ok = Random.NextDouble() < BatchChance(recipe);

            if (batch.Ok)
            {
                batch.Count = recipe.Count;

                if (Random.NextDouble() < Alchemy.BonusChance(State.Refining, recipe.Difficulty))
                {
                    batch.Count++;
                    batch.Bonus = true;
                }

                AddItem(recipe.Result, batch.Count);
                State.PillsMade += batch.Count;
            }
            else
            {
                batch.Why = "Котёл зачадил. Травы сгорели.";
                Say(batch.Why);
            }

            // A failed batch teaches as well — a little less.
            batch.SkillUp = Practise(batch.Ok ? recipe.Difficulty : Math.Max(1, recipe.Difficulty - 1));
            Achieve();
            Touch();

            return batch;
        }

        /// <summary>Practice at the furnace: harder recipes teach more. True if the skill rose.</summary>
        bool Practise(int amount)
        {
            if (State.Refining >= Alchemy.MaxSkill)
            {
                return false;
            }

            State.Refined += amount;
            var need = Alchemy.SkillNeeds[State.Refining];

            if (State.Refined < need)
            {
                return false;
            }

            State.Refined -= need;
            State.Refining++;
            Say($"Мастерство алхимии: {State.Refining}");

            return true;
        }

        /// <summary>The skill's progress to the next level, 0–1.</summary>
        public float RefiningShare => State.Refining >= Alchemy.MaxSkill ? 1 : (float)State.Refined / Alchemy.SkillNeeds[State.Refining];

        /// <summary>How many batches of a recipe the bag and the purse allow.</summary>
        public int BatchesPossible(PillRecipeDef recipe)
        {
            if (recipe == null || !KnowsPill(recipe.Id))
            {
                return 0;
            }

            var most = recipe.Stones > 0 ? State.Stones / recipe.Stones : int.MaxValue;

            foreach (var (item, count) in recipe.Items)
            {
                most = Math.Min(most, Count(item) / count);
            }

            return most == int.MaxValue ? 0 : most;
        }
    }
}
