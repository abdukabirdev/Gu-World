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

        // ------------------------------------------------------------------ Tempering

        var t = new Session(Session.NewGame("Кузнец"), 12);
        var blade = t.AddGu(t.MakeGu("moonglow", 0, true), true);
        Check(t.WhyNoTemper(blade) != null, "tempering takes its materials");
        t.State.Stones = 100000;
        t.AddItem("jade_shard", 200, true);
        t.AddItem("blood_crystal", 200, true);
        t.AddItem("king_core", 200, true);
        var tries = 0;

        while (blade.Quality < GuBook.MaxQuality && tries < 60)
        {
            var was = blade.Quality;
            var result = t.Temper(blade.Uid);
            tries++;
            Check(result.Tried, "a tempering is tried when it can be");
            Check(result.Held ? blade.Quality == was + 1 && blade.Tempered == 0 : blade.Quality == was && blade.Tempered > 0, "a tempering holds (a quality up) or is remembered");
        }

        Check(blade.Quality == GuBook.MaxQuality && blade.Affixes.Count == 3, "tempered to perfect, with an affix a quality");
        Check(blade.Affixes.Select(a => a.Kind).Distinct().Count() == blade.Affixes.Count, "no affix twice");
        Check(t.WhyNoTemper(blade) == "Совершеннее не бывает", "perfect is the top");
        Check(t.TemperCostOf(t.MakeGu("moonglow", 0, true)).Chance < t.TemperCostOf(new GuInstance { Def = "moonglow", Tempered = 3 }).Chance, "each failure makes the next tempering surer");

        var kinds = blade.Affixes.Select(a => a.Kind).ToList();
        var made = t.Reforge(blade.Uid, 0);
        Check(made != null && blade.Affixes[0] == made && blade.Affixes.Select(a => a.Kind).Distinct().Count() == 3, "a reforged affix stays unique");

        var twin = t.AddGu(t.MakeGu("moonglow", 2, true), true);
        var xp = blade.MasteryXp;
        var gain = Session.TwinMastery(twin);
        Check(t.TwinsOf(blade).Count == 1 && t.FeedTwin(blade.Uid) && blade.MasteryXp == xp + gain && t.FindGu(twin.Uid) == null, "a Gu takes in its twin as mastery");
        Check(!t.FeedTwin(blade.Uid), "no twin left to take in");

        // ------------------------------------------------------------------ Attainment on the paths

        var p = new Session(Session.NewGame("Путь"), 31);
        var moon = p.AddGu(p.MakeGu("moonlight", 0, true), true);
        p.Equip(moon.Uid, false, 0);
        var plain = p.PowerOf(moon);
        var plainCost = p.CostOf(moon);
        p.GainAttainment(GuPath.Moon, Attainment.Needs[2]);
        Check(p.GradeOf(GuPath.Moon) == 2 && p.GradeOf(GuPath.Fire) == 0, "attainment is of one path");
        Check(p.PowerOf(moon) > plain * 1.09f && p.CostOf(moon) < plainCost, "a master's Gu of the path are stronger and cheaper");
        Check(p.DeviseCost(KillerMoves.Get("moon_on_wind")) < KillerMoves.Get("moon_on_wind").Insight, "a master comprehends the path's moves for less");
        Check(p.State.Achievements.Contains("path_master"), "becoming a master is an achievement");
        var before2 = p.AttainmentOf(GuPath.Moon);
        p.State.Essence = 500;
        p.Tick(10);
        var c3 = p.CastSlot(0);
        Check(c3 != null, "a Gu is cast");
        p.SkillHit(c3, Bestiary.Get("wolf"));
        Check(p.AttainmentOf(GuPath.Moon) >= before2, "using the path's Gu adds to it");
        Check(Attainment.Needs.Length == Attainment.Grades.Length && Attainment.Power.Length == Attainment.Grades.Length && Attainment.Thrift.Length == Attainment.Grades.Length, "every grade has its numbers");

        // ------------------------------------------------------------------ Relic Gu

        var r = new Session(Session.NewGame("Реликвия"), 41);
        r.AddItem("relic_r1", 4, true);
        r.AddItem("relic_r2", 1, true);
        Check(r.UseConsumable("relic_r1") && r.State.Step == 1, "a relic lifts a stage");
        Check(!r.UseConsumable("relic_r2") && r.Count("relic_r2") == 1, "a relic of another rank does nothing");
        r.UseConsumable("relic_r1");
        r.UseConsumable("relic_r1");
        Check(r.State.Step == 3 && !r.UseConsumable("relic_r1") && r.Count("relic_r1") == 1 && r.WhyNoRelic(ItemBook.Get("relic_r1")) != null, "a relic does not carry across a rank's gate");

        foreach (var rank in Enumerable.Range(1, Cultivation.MaxRank))
        {
            var id = $"relic_r{rank}";
            Check(ItemBook.Get(id)?.Relic == rank, $"a relic of rank {rank} exists");
            Check(Bestiary.Tables.Any(t => t.Items.Any(e => e.Item == id)) || Markets.Ids.SelectMany(Markets.Get).Any(e => e.Item == id), $"the relic of rank {rank} can be found");
        }

        // ------------------------------------------------------------------ The immortal aperture

        var a = new Session(Session.NewGame("Полигон"), 21);
        var own = a.AddGu(a.MakeGu("moonlight", 0, true), true);
        a.Equip(own.Uid, false, 0);
        var stepBefore = a.State.Step;
        var guBefore = a.State.Gu.Count;
        var killsBefore = a.State.Kills;
        var insightBefore = a.State.Insight;
        a.EnterSandbox();
        Check(a.InSandbox, "the hero is in the aperture");
        a.SandboxStep(16);
        Check(a.Rank == 5 && a.Stats.ActiveSlots == 6, "in the aperture the hero can be of the fifth rank");
        var lent = a.SandboxGive("full_moon", 3);
        Check(lent != null && lent.Borrowed && lent.Quality == 3 && a.IsSlotted(lent.Uid), "a Gu of the book is lent, at any quality, and slotted");
        Check(a.SandboxTryMove("moonfall") && a.SelectedMove()?.Id == "moonfall", "a killer move can be tried at once (its Gu lent and slotted)");
        var cast2 = a.CastMove(KillerMoves.Get("moonfall"));
        Check(cast2 != null && a.State.Essence >= a.Stats.MaxEssence - 0.01f || cast2 != null, "a move cast in the aperture");
        a.Tick(0.1f);
        Check(a.State.Essence >= a.Stats.MaxEssence - 0.01f, "essence without end in the aperture");
        a.Killed("wolf");
        a.GainInsight(500);
        Check(a.State.Kills == killsBefore && Math.Abs(a.State.Insight - insightBefore) < 0.01f, "nothing in the aperture counts");
        a.TakeDamage(1e6f);
        Check(!a.Dead, "no death in the aperture");
        a.LeaveSandbox();
        Check(!a.InSandbox && a.State.Step == stepBefore && a.State.Gu.Count == guBefore && a.State.Gu.TrueForAll(g => !g.Borrowed), "leaving gives everything back and takes the loans");
        Check(a.ActiveGu(0) == own && !a.KnowsMove("moonfall"), "the slots and the moves as they were");

        // A game saved by mistake inside the aperture loads without its loans.
        var b2 = new Session(Session.NewGame("Сбой"), 22);
        b2.EnterSandbox();
        b2.SandboxGive("heaven_ice", 1);
        var copy = System.Text.Json.JsonSerializer.Deserialize<SaveData>(System.Text.Json.JsonSerializer.Serialize(b2.State, new System.Text.Json.JsonSerializerOptions { IncludeFields = true }), new System.Text.Json.JsonSerializerOptions { IncludeFields = true });
        var reloaded = new Session(copy, 23);
        Check(!reloaded.State.Gu.Exists(g => g.Borrowed), "a save made inside the aperture loads without the loans");

        notes.Add($"high: {HighGu.All.Length} Gu of ranks 4–5, {HighGu.Recipes.Length} fusions, {KillerMoves.All.Length} killer moves");
    }
}
