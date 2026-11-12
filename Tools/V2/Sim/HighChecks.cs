using System;
using System.Collections.Generic;
using System.Linq;
using GuWorld.Core;

/// <summary>
/// Checks of the master's side of the game: the fourth and fifth ranks' Gu and where they are had,
/// killer moves (every one can be wielded at its rank with the slots it has, comprehended and cast),
/// the Spring Autumn Cicada's return from death, shelves that open by rank and standing.
/// </summary>
static partial class Program
{
    static void HighRules()
    {
        // ------------------------------------------------------------------ The ladder

        Check(Cultivation.Needs.Length == Cultivation.MaxStep, "a need for every step but the last");
        Check(Cultivation.StoneCost.Length == Cultivation.MaxStep, "a stone cost for every step but the last");
        Check(Cultivation.Rank(Cultivation.MaxStep) == Cultivation.MaxRank, "the last step is of the last rank");

        for (var step = 1; step < Cultivation.MaxStep; step++)
        {
            Check(Cultivation.Needs[step] > Cultivation.Needs[step - 1], $"step {step} needs more insight than the one before");
            Check(Cultivation.ActiveSlots(step) >= Cultivation.ActiveSlots(step - 1) && Cultivation.PassiveSlots(step) >= Cultivation.PassiveSlots(step - 1), $"step {step} keeps its slots");
        }

        for (var rank = 1; rank <= Cultivation.MaxRank; rank++)
        {
            var first = (rank - 1) * 4;
            Check(Cultivation.Rank(first) == rank, $"rank {rank} starts at step {first}");
            Check(rank == 1 || Cultivation.IsRankUp(first - 1), $"rank {rank} is reached by a breakthrough");
            Check(rank == 1 || Cultivation.StoneCost[first - 1] > 0, $"the breakthrough to rank {rank} burns stones");
        }

        // ------------------------------------------------------------------ The high Gu: every one is had somewhere

        foreach (var gu in HighGu.All)
        {
            var had = Recipes.All.Any(r => r.Result == gu.Id) || Markets.Ids.SelectMany(Markets.Get).Any(e => e.Gu == gu.Id)
                      || Bestiary.Tables.Any(t => t.GuPool.Contains(gu.Id)) || Story.All.Any(q => q.Reward.Gu.Contains(gu.Id));
            Check(had, $"high Gu {gu.Id} can be got somewhere");
            Check(gu.Skill != null || gu.Passive.Length > 0, $"high Gu {gu.Id} does something");
        }

        foreach (var recipe in HighGu.Recipes)
        {
            var result = GuBook.Get(recipe.Result);

            foreach (var input in recipe.Gu)
            {
                Check(GuBook.Get(input) != null && GuBook.Get(input).Rank < result.Rank, $"fusion {recipe.Id}: {input} is of a lower rank than what it makes");
            }
        }

        // Every loot table is somewhere: an enemy's, a chest's, or another's.
        foreach (var table in Bestiary.Tables)
        {
            var used = Bestiary.All.Any(e => e.Loot == table.Id) || Atlas.AllThings.Any(t => t.Kind == ThingKind.Chest && t.Content == table.Id);
            Check(used, $"loot table {table.Id} drops from something");
        }

        // A master's shelf: what needs a rank is not on it before.
        var shop = new Session(Session.NewGame("Тест"), 3);
        var bai = Markets.Get("bai_gu");
        Check(!shop.Shelf(bai).Any(e => e.Item == "scroll_north_wind_ice"), "a rank 4 scroll is not sold to a novice");
        shop.State.Step = 12;
        shop.Recompute();
        shop.ChangeReputation("bai", 60, true);
        Check(shop.Shelf(bai).Any(e => e.Item == "scroll_north_wind_ice"), "a rank 4 master friendly with the Bai is sold it");
        Check(!shop.Shelf(bai).Any(e => e.Item == "scroll_heaven_ice"), "the rank 5 scroll waits for rank 5");
        Check(shop.Explain("standing:bai=honoured").Contains("Бай") || shop.Meets("standing:bai=honoured"), "a standing condition is explained");

        // ------------------------------------------------------------------ Killer moves

        var ids = new HashSet<string>();

        foreach (var move in KillerMoves.All)
        {
            Check(ids.Add(move.Id), $"move {move.Id} is named once");
            var core = GuBook.Get(move.Core);
            Check(core != null && core.Kind == GuKind.Active && core.Skill != null, $"move {move.Id}: its core {move.Core} is an active Gu");
            Check(move.Supports.All(g => GuBook.Get(g) != null), $"move {move.Id}: its Gu exist");
            Check(move.Supports.Distinct().Count() == move.Supports.Length && !move.Supports.Contains(move.Core), $"move {move.Id}: no Gu twice");
            Check(move.Skill != null && move.Skill.Shape != SkillShape.None && move.Skill.Cost > 0 && move.Skill.Cooldown > 0, $"move {move.Id}: a skill that costs and waits");
            Check(move.Skill.Status == StatusKind.None || move.Skill.StatusTime > 0, $"move {move.Id}: its status lasts");
            Check(move.Insight > 0, $"move {move.Id} can be comprehended");

            if (core == null || move.Supports.Any(g => GuBook.Get(g) == null))
            {
                continue;
            }

            var all = KillerMoves.GuOf(move).Select(GuBook.Get).ToList();
            Check(all.All(g => g.Rank <= move.Rank), $"move {move.Id}: no Gu above its rank {move.Rank}");
            Check(all.Max(g => g.Rank) == move.Rank || move.Rank <= 2, $"move {move.Id}: of the rank of its highest Gu");

            // The slots at the first step of its rank hold it.
            var step = (move.Rank - 1) * 4;
            var actives = all.Count(g => g.Kind == GuKind.Active);
            var passives = all.Count(g => g.Kind == GuKind.Passive);
            Check(actives <= Cultivation.ActiveSlots(step) && passives <= Cultivation.PassiveSlots(step), $"move {move.Id}: fits the slots of rank {move.Rank} ({actives} active, {passives} passive)");

            // Stronger than its core alone, per essence it is worth it.
            // (A fan's bolts spread: no more than two of them find one foe.)
            float Weight(SkillDef k) => k.Power * (k.Shape == SkillShape.Fan ? Math.Min(2, k.Count) : Math.Max(1, k.Count));
            var lone = Weight(core.Skill);
            var together = Weight(move.Skill);
            Check(together > lone, $"move {move.Id} hits harder than {move.Core} alone");

            // Each of its Gu can be had.
            foreach (var gu in all)
            {
                var had = Recipes.All.Any(r => r.Result == gu.Id) || Markets.Ids.SelectMany(Markets.Get).Any(e => e.Gu == gu.Id)
                          || Bestiary.Tables.Any(t => t.GuPool.Contains(gu.Id)) || Story.All.Any(q => q.Reward.Gu.Contains(gu.Id))
                          || Session.NewGame("x").Gu.Any(g => g.Def == gu.Id)
                          || gu.Id == "blood_moon"; // the demonic ending's

                Check(had, $"move {move.Id}: its Gu {gu.Id} can be got");
            }
        }

        Check(Enumerable.Range(1, Cultivation.MaxRank).All(r => KillerMoves.All.Count(m => m.Rank == r) >= 3), "three moves or more at every rank");

        // Comprehending one and casting it.
        var s = new Session(Session.NewGame("Мастер"), 9);
        var frost = KillerMoves.Get("frost_moon");
        s.State.Step = 4;
        s.Recompute();
        Check(s.WhyNoDevise(frost) != null && s.WhyNoDevise(frost).Contains("Вставьте"), "a move is not comprehended without its Gu slotted");
        var glow = s.AddGu(s.MakeGu("moonglow", 0, true), true);
        var spike = s.AddGu(s.MakeGu("ice_spike", 0, true), true);
        Check(s.Equip(glow.Uid, false, 0) && s.Equip(spike.Uid, false, 1), "the move's Gu go in");
        s.State.Insight = 0;
        Check(s.WhyNoDevise(frost) != null && s.WhyNoDevise(frost).Contains("прозрения"), "comprehension takes insight");
        s.State.Insight = frost.Insight + 5;
        Check(s.Devise("frost_moon") && s.KnowsMove("frost_moon") && Math.Abs(s.State.Insight - 5) < 0.01f, "a move comprehended spends its insight");
        Check(s.SelectedMove() == frost, "the first move is the one on V");
        s.State.Essence = 1000;
        var lonePower = s.PowerOf(glow);
        var cast = s.CastMove(frost);
        Check(cast != null && cast.Move == frost && cast.Skill == frost.Skill && cast.Def.Id == "moonglow", "a move is cast through its core");
        Check(cast != null && cast.Power > lonePower, "a move is stronger than its core alone");
        Check(s.WhyNoMove(frost) == "Приём ещё не готов" && s.CooldownLeft(0) > 0, "a move waits, and so does its core");
        s.Tick(frost.Skill.Cooldown + 0.1f);
        Check(s.WhyNoMove(frost) == null, "a move is ready again after its cooldown");
        s.Unslot(spike.Uid);
        Check(s.WhyNoMove(frost) != null && s.SelectedMove() == null, "a move falls apart when a Gu of it leaves the aperture");

        // Taught by an effect, and Shift + V turns through the ready ones.
        s.Apply("learn:move:moon_on_wind");
        Check(s.KnowsMove("moon_on_wind"), "a move can be taught by an effect");
        s.Equip(spike.Uid, false, 1);
        var wind = s.AddGu(s.MakeGu("tailwind", 0, true), true);
        var light = s.AddGu(s.MakeGu("moonlight", 0, true), true);
        s.Equip(wind.Uid, false, 2);
        s.Equip(light.Uid, false, 3);
        Check(s.ReadyMoves().Count == 2, "two moves ready together");
        var before = s.SelectedMove();
        var after = s.NextMove();
        Check(after != null && after != before && s.SelectedMove() == after, "the next move is chosen");

        // ------------------------------------------------------------------ The Cicada

        var c = new Session(Session.NewGame("Цикада"), 4);
        var cicada = c.AddGu(c.MakeGu(HighGu.Cicada, 0, true), true);
        Check(c.Equip(cicada.Uid, true, 0), "the Cicada goes in a passive slot (any rank)");
        Check(c.CicadaReady, "the Cicada is awake");
        c.TakeDamage(1e6f);
        Check(!c.Dead && c.State.Health >= c.Stats.MaxHealth - 0.01f, "the Cicada brings its master back whole");
        Check(!c.CicadaReady && c.CicadaSleepLeft == HighGu.CicadaSleep, "then it sleeps three days");
        c.TakeDamage(1e6f);
        Check(c.Dead, "a sleeping Cicada does not save");
        c.Die();
        c.NewDay();
        c.NewDay();
        c.NewDay();
        Check(c.CicadaReady, "after three days it is awake again");

        notes.Add($"high: {HighGu.All.Length} Gu of ranks 4–5, {HighGu.Recipes.Length} fusions, {KillerMoves.All.Length} killer moves");
    }
}
