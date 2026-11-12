using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using GuWorld.Core;

/// <summary>
/// Checks of the game's rules and content that need no engine: every reference in the content
/// resolves, the rules do what the design says, and a bot can play the story from the first
/// quest to an ending. Exit code 0 if all is well.
/// </summary>
static partial class Program
{
    static int failures;
    static readonly List<string> notes = new List<string>();

    static void Check(bool ok, string what)
    {
        if (!ok)
        {
            failures++;
            Console.WriteLine("FAIL  " + what);
        }
    }

    static int Main(string[] args)
    {
        var repo = args.Length > 0 && Directory.Exists(args[0]) ? args[0] : FindRepo();

        // The generators alone: `gen` checks and draws them, `gen export` also writes the viewer's models.
        if (Array.IndexOf(args, "gen") >= 0)
        {
            var genReport = new List<string>();
            var export = Array.IndexOf(args, "export") >= 0;
            var only = Array.Find(args, a => a.StartsWith("only=")) ?? "only=trees,biomes,people";
            var what = only.Substring(5).Split(',');

            if (Array.IndexOf(what, "trees") >= 0)
            {
                GenChecks.Trees(repo, Check, genReport, export);
            }

            if (Array.IndexOf(what, "biomes") >= 0)
            {
                GenChecks.BiomeChecks(repo, Check, genReport);
            }

            foreach (var line in genReport)
            {
                Console.WriteLine(line);
            }

            Console.WriteLine(failures == 0 ? "\nALL GENERATOR CHECKS PASSED" : $"\n{failures} GENERATOR CHECK(S) FAILED");

            return failures == 0 ? 0 : 1;
        }

        Content(repo);
        Rules();
        MoreRules();
        HighRules();
        SaveRoundTrip();
        var report = new List<string>();

        if (Array.IndexOf(args, "audio") >= 0 || Array.IndexOf(args, "all") >= 0)
        {
            AudioChecks(repo, report);
        }

        if (Array.IndexOf(args, "props") >= 0 || Array.IndexOf(args, "world") >= 0 || Array.IndexOf(args, "all") >= 0)
        {
            PropChecks.Run(repo, Check, report);
        }

        if (Array.IndexOf(args, "world") >= 0 || Array.IndexOf(args, "all") >= 0)
        {
            WorldChecks.Run(repo, Check, report);
        }

        RegionPlay(report);
        Playthrough("ending_righteous", 11, report, true);
        Playthrough("ending_demonic", 23, null, false);
        Balance(report);

        foreach (var line in report)
        {
            Console.WriteLine(line);
        }

        Console.WriteLine(failures == 0 ? "\nALL CHECKS PASSED" : $"\n{failures} CHECK(S) FAILED");

        return failures == 0 ? 0 : 1;
    }

    static string FindRepo()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());

        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "Assets", "GuWorld")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? ".";
    }

    // ---------------------------------------------------------------------- Content

    static void Content(string repo)
    {
        var build = Path.Combine(repo, "Assets", "GuWorld", "Resources", "Build");
        bool Model(string id) => string.IsNullOrEmpty(id) || !Directory.Exists(build) || File.Exists(Path.Combine(build, id + ".prefab"));
        bool InWorld(MapPoint p) => Math.Abs(p.X) < Regions.Half - 10 && Math.Abs(p.Z) < Regions.Half - 10;

        Check(GuBook.All.Select(g => g.Id).Distinct().Count() == GuBook.All.Length, "Gu ids are unique");
        Check(ItemBook.All.Select(g => g.Id).Distinct().Count() == ItemBook.All.Length, "item ids are unique");
        Check(Bestiary.All.Select(g => g.Id).Distinct().Count() == Bestiary.All.Length, "enemy ids are unique");
        Check(Story.All.Select(g => g.Id).Distinct().Count() == Story.All.Length, "quest ids are unique");
        Check(Atlas.AllThings.Select(g => g.Id).Distinct().Count() == Atlas.AllThings.Length, "thing ids are unique");
        Check(Atlas.AllNpcs.Select(g => g.Id).Distinct().Count() == Atlas.AllNpcs.Length, "npc ids are unique");
        Check(Atlas.AllSpawns.Select(g => g.Id).Distinct().Count() == Atlas.AllSpawns.Length, "spawn ids are unique");
        Check(Atlas.AllEncounters.Select(g => g.Id).Distinct().Count() == Atlas.AllEncounters.Length, "encounter ids are unique");

        foreach (var gu in GuBook.All)
        {
            Check(gu.Kind == GuKind.Passive ? gu.Passive.Length > 0 && gu.Skill == null : gu.Skill != null, $"Gu {gu.Id}: an active Gu has a skill, a passive one has bonuses");
            Check(!string.IsNullOrEmpty(gu.Seal) && !string.IsNullOrEmpty(gu.Description), $"Gu {gu.Id} has a seal and a description");
        }

        foreach (var enemy in Bestiary.All)
        {
            Check(enemy.Loot == null || Bestiary.Table(enemy.Loot) != null, $"enemy {enemy.Id}: loot table {enemy.Loot}");
            Check(enemy.Summons == null || Bestiary.Get(enemy.Summons) != null, $"enemy {enemy.Id}: summons {enemy.Summons}");
            Check(enemy.Mind != EnemyMind.Boss || enemy.Moves.Length > 0, $"boss {enemy.Id} has moves");
        }

        foreach (var table in Bestiary.Tables)
        {
            foreach (var entry in table.Items)
            {
                Check(ItemBook.Get(entry.Item) != null, $"loot {table.Id}: item {entry.Item}");
            }

            foreach (var gu in table.GuPool)
            {
                Check(GuBook.Get(gu) != null, $"loot {table.Id}: Gu {gu}");
            }
        }

        foreach (var recipe in Recipes.All)
        {
            Check(GuBook.Get(recipe.Result) != null, $"recipe {recipe.Id}: result");
            Check(recipe.Gu.All(g => GuBook.Get(g) != null), $"recipe {recipe.Id}: Gu inputs");
            Check(recipe.Items.All(i => ItemBook.Get(i.item) != null), $"recipe {recipe.Id}: items");
            Check(recipe.Known || ItemBook.All.Any(i => i.Teaches == recipe.Id), $"recipe {recipe.Id} can be learnt from a scroll");
        }

        foreach (var item in ItemBook.All.Where(i => i.Kind == ItemKind.Scroll))
        {
            Check(Recipes.Get(item.Teaches) != null, $"scroll {item.Id} teaches a recipe");
            var obtainable = Markets.Ids.SelectMany(Markets.Get).Any(s => s.Item == item.Id) || Bestiary.Tables.Any(t => t.Items.Any(e => e.Item == item.Id)) || Story.All.Any(q => q.Reward.Items.Any(r => r.item == item.Id));
            Check(obtainable, $"scroll {item.Id} can be got somewhere");
        }

        foreach (var entry in Markets.Ids.SelectMany(Markets.Get))
        {
            Check(entry.Gu != null ? GuBook.Get(entry.Gu) != null : ItemBook.Get(entry.Item) != null, $"shop entry {entry.Gu ?? entry.Item}");
        }

        foreach (var spawn in Atlas.AllSpawns)
        {
            Check(Bestiary.Get(spawn.Enemy) != null, $"spawn {spawn.Id}: enemy");
            Check(InWorld(spawn.At), $"spawn {spawn.Id} is in the world");
        }

        foreach (var encounter in Atlas.AllEncounters)
        {
            Check(InWorld(encounter.At), $"encounter {encounter.Id} is in the world");
            Check(encounter.Waves.All(w => w.Enemies.All(e => Bestiary.Get(e.enemy) != null)), $"encounter {encounter.Id}: enemies");
        }

        foreach (var thing in Atlas.AllThings)
        {
            Check(InWorld(thing.At), $"thing {thing.Id} is in the world");
            Check(Model(thing.Model), $"thing {thing.Id}: model {thing.Model}");

            if (thing.Kind == ThingKind.Chest)
            {
                Check(Bestiary.Table(thing.Content) != null, $"chest {thing.Id}: loot table");
            }
            else if (thing.Kind == ThingKind.Herb)
            {
                Check(ItemBook.Get(thing.Content) != null, $"herb {thing.Id}: item");
            }
            else if (thing.Kind == ThingKind.Door)
            {
                // A door into a room leads to that room; any other to a place of the world.
                Check(thing.Content.StartsWith("interior:") ? Interiors.Get(thing.Content.Substring(9)) != null : InWorld(thing.To) && thing.To.DistanceTo(thing.At) > 30, $"door {thing.Id} leads somewhere");
            }
        }

        foreach (var building in Valley.Buildings)
        {
            Check(Model(building.Model), $"building model {building.Model}");
            Check(InWorld(building.At), $"building {building.Model} at {building.At} is in the world");
        }

        foreach (var scatter in Valley.Scatter)
        {
            Check(Model(scatter.Model), $"scatter model {scatter.Model}");
        }

        foreach (var npc in Atlas.AllNpcs)
        {
            Check(InWorld(npc.At), $"npc {npc.Id} is in the world");

            foreach (var building in Valley.Buildings.Where(b => Atlas.InValley(npc) && !b.Model.StartsWith("prop_b") && !b.Model.StartsWith("tree") && b.Model != "prop_market_goods"))
            {
                Check(building.At.DistanceTo(npc.At) > 2.5f, $"npc {npc.Id} does not stand inside {building.Model} at {building.At}");
            }
        }

        // The story: every target is something that exists, every marker can be found, the chain is whole.
        var seen = new HashSet<string>();

        for (var id = Story.First; id != null; id = Story.Get(id).Next)
        {
            Check(seen.Add(id), $"main chain does not loop at {id}");
            Check(Story.Get(id) != null && Story.Get(id).Main, $"main chain: {id}");
        }

        Check(seen.Count == Story.All.Count(q => q.Main), "every main quest is in the chain");

        foreach (var quest in Story.All)
        {
            Check(quest.Steps.Length > 0, $"quest {quest.Id} has steps");
            Check(quest.Main || Atlas.Npc(quest.Giver) != null, $"quest {quest.Id}: giver");
            Check(quest.Main || quest.Offer.Length > 0, $"quest {quest.Id}: the giver says something");
            Check(quest.Reward.Items.All(i => ItemBook.Get(i.item) != null) && quest.Reward.Gu.All(g => GuBook.Get(g) != null), $"quest {quest.Id}: reward");

            foreach (var step in quest.Steps)
            {
                var target = step.Target.TrimEnd('*');
                var wild = step.Target.EndsWith("*");
                // "a|b|c": any of several targets, each must be real.
                bool Any<T>(IEnumerable<T> all, Func<T, string> key) => step.Target.Contains('|')
                    ? step.Target.Split('|').All(one => all.Any(x => key(x) == one))
                    : all.Any(x => wild ? key(x).StartsWith(target) : key(x) == target);
                var ok = step.Kind switch
                {
                    StepKind.Talk => Atlas.Npc(target) != null && step.Lines.Length > 0,
                    StepKind.Kill => Any(Bestiary.All, e => e.Id),
                    StepKind.Have => ItemBook.Get(target) != null,
                    StepKind.Reach => Atlas.Place(target) != null || Regions.Settlement(target) != null,
                    StepKind.Use => Any(Atlas.AllThings, t => t.Id) && (!wild || Atlas.AllThings.Count(t => t.Id.StartsWith(target)) >= step.Count),
                    StepKind.Equip => target == "" || GuBook.Get(target) != null,
                    StepKind.Cultivate => step.Count >= 1 && step.Count <= Cultivation.MaxStep,
                    StepKind.Encounter => Atlas.Encounter(target) != null,
                    StepKind.Choose => step.Choices.Length >= 2 && step.Choices.All(c => Story.Endings.ContainsKey(c.flag)),
                    _ => true
                };
                Check(ok, $"quest {quest.Id}: step «{step.Text}» has a real target ({step.Kind} {step.Target})");
                var marker = string.IsNullOrEmpty(step.Marker) ? step.Target : step.Marker;
                Check(step.Kind == StepKind.Equip || step.Kind == StepKind.Fuse || Atlas.Locate(marker, out _), $"quest {quest.Id}: step «{step.Text}» has a marker ({marker})");

                foreach (var gift in step.Gives)
                {
                    Check(gift.StartsWith("stones:") || GuBook.Get(gift) != null || ItemBook.Get(gift) != null, $"quest {quest.Id}: gives {gift}");
                }

                foreach (var line in step.Lines)
                {
                    Check(line.Who == "" || line.Who == "hero" || Atlas.Npc(line.Who) != null, $"quest {quest.Id}: speaker {line.Who}");
                }
            }
        }

        foreach (var thing in Atlas.AllThings.Where(t => t.Needs != ""))
        {
            var needs = thing.Needs.StartsWith("flag:") ? thing.Needs.Substring(5) : thing.Needs;

            if (needs.StartsWith("item:"))
            {
                var item = needs.Substring(5);
                var got = Bestiary.Tables.Any(t => t.Items.Any(i => i.Item == item)) || Story.All.Any(q => q.Gives.Contains(item) || q.Steps.Any(st => st.Gives.Contains(item)) || q.Reward.Items.Any(i => i.item == item));
                Check(ItemBook.Get(item) != null && got, $"thing {thing.Id}: the key {item} can be found");
                continue;
            }

            // A condition on the hero's rank or standing (not a flag) is met by playing on.
            if (needs.Contains(">=") || needs.StartsWith("standing:"))
            {
                Check(new Session(Session.NewGame("Тест"), 1).Explain(needs) != "", $"thing {thing.Id}: its condition {needs} is understood");
                continue;
            }

            var set = Story.All.Any(q => q.Reward.Flags.Contains(needs) || q.Id + "_done" == needs) || Bestiary.All.Any(e => e.Boss && e.Id + "_dead" == needs)
                || needs == "peak_blessed" || Atlas.AllEncounters.Any(e => e.Id + "_won" == needs);
            Check(set, $"thing {thing.Id}: something sets the flag {needs}");
        }

        MoreContent();
    }

    // ---------------------------------------------------------------------- Rules

    static void Rules()
    {
        var s = new Session(Session.NewGame("Тест"), 7);
        Check(s.MainQuest != null && s.MainQuest.Quest == "m01", "a new game starts the first quest");
        Check(Math.Abs(s.Stats.MaxEssence - 44) < 0.01f, "talent C fills 44 of a rank 1 aperture");
        Check(s.Stats.ActiveSlots == 2 && s.Stats.PassiveSlots == 1, "two active slots and one passive at the start");

        // A wild Gu must be refined before it goes in a slot.
        var moon = s.AddGu(s.MakeGu("moonlight", 0, false), true);
        Check(!s.Equip(moon.Uid, false, 0), "a wild Gu cannot be slotted");
        Check(s.Refine(moon.Uid) && s.Equip(moon.Uid, false, 0), "a refined Gu can");
        s.Restore();
        var before = s.State.Essence;
        var cast = s.CastSlot(0);
        Check(cast != null && s.State.Essence < before && s.CooldownLeft(0) > 0, "casting takes essence and starts the cooldown");
        Check(s.CastSlot(0) == null, "a Gu on cooldown cannot be cast");
        s.Tick(5);
        Check(s.CooldownLeft(0) <= 0, "cooldowns run out");

        // Palm strikes fill the aperture.
        s.State.Essence = 0;
        s.MeleeHit(0, Bestiary.Get("fox"));
        Check(s.State.Essence > 4, "a palm strike gives essence");

        // A rank 2 Gu is too much for a rank 1 hero.
        var ice = s.AddGu(s.MakeGu("ice_spike", 0, false), true);
        Check(s.WhyNoRefine(ice) != null, "a rank 2 Gu cannot be refined at rank 1 with talent C");
        ice.Refined = true;
        Check(!s.Equip(ice.Uid, false, 1), "a rank 2 Gu cannot be slotted at rank 1");

        // Shield and ward.
        var jade = s.AddGu(s.MakeGu("jade_skin", 0, true), true);
        s.Equip(jade.Uid, false, 1);
        s.Restore();
        s.CastSlot(1);
        var hurt = s.TakeDamage(20);
        Check(Math.Abs(hurt - 12) < 0.01f, "the jade skin takes 40% off a blow");

        // Stages come by themselves; a rank waits for the spring, the story and the stones.
        s.GainInsight(100 + 180 + 300 + 600);
        Check(s.State.Step == 3, "insight carries the hero to the peak of rank 1 and no further");
        Check(s.WhyNoBreakthrough() != null, "the rank's gate is shut until the story opens it");
        s.SetFlag("ch3");
        s.State.Stones = 49;
        Check(!s.Breakthrough(), "a breakthrough needs its stones");
        s.State.Stones = 60;
        Check(s.Breakthrough() && s.Rank == 2 && s.State.Stones == 10, "the breakthrough takes 50 stones and gives rank 2");
        Check(s.Stats.ActiveSlots == 4 && s.Stats.PassiveSlots == 2, "rank 2 opens the fourth slot and the second passive");
        Check(s.Equip(ice.Uid, false, 2), "now the rank 2 Gu fits");

        // Fusion: the result takes its parent's slot.
        var light = s.AddGu(s.MakeGu("little_light", 1, true), true);
        s.AddItem("moon_orchid", 3, true);
        s.State.Stones = 100;
        Check(s.FusionInputs(Recipes.Get("moonglow"), out var why) == null && why == "Рецепт не изучен", "an unknown recipe cannot be fused");
        s.AddItem("scroll_moonglow", 1, true);
        s.ReadScroll("scroll_moonglow");
        var glow = s.Fuse("moonglow");
        Check(glow != null && glow.Quality >= 1 && s.State.Active[0] == glow.Uid, "fusion keeps the best quality and the slot");
        Check(s.FindGu(moon.Uid) == null && s.FindGu(light.Uid) == null && s.Count("moon_orchid") == 0 && s.State.Stones == 70, "fusion takes its inputs");

        // Passives and affixes add up.
        var worm = s.AddGu(s.MakeGu("liquor_worm", 0, true), true);
        var regen = s.Stats.EssenceRegen;
        s.Equip(worm.Uid, true, 0);
        Check(Math.Abs(s.Stats.EssenceRegen / regen - 1.5f) < 0.01f, "the liquor worm adds half again to the regeneration");

        // Death costs a tenth of the stones.
        s.State.Stones = 200;
        s.TakeDamage(99999);
        Check(s.Dead && s.Die() == 20 && s.State.Stones == 180 && !s.Dead, "death takes a tenth of the stones and the hero wakes whole");

        // Loot: quality odds, bosses' floors.
        var counts = new int[3];

        for (var i = 0; i < 4000; i++)
        {
            foreach (var gu in s.RollLoot("cave_gu").Gu)
            {
                counts[gu.Quality]++;
                Check(gu.Affixes.Count == gu.Quality, "a Gu has as many affixes as its quality");
            }
        }

        var total = counts.Sum();
        Check(total > 400 && total < 720, $"the cave's Gu drop about 14% of the time ({total} of 4000)");
        Check(counts[0] > counts[1] && counts[1] > counts[2] && counts[2] > 0, "ordinary Gu are commoner than rare, rare than superb");
        Check(Enumerable.Range(0, 50).All(_ => s.RollLoot("thunder_crown_wolf").Gu.Single().Quality == 2), "the Thunder Crown always drops a superb Gu");

        // Rocks pay back less than they cost, on average, but not nothing.
        foreach (var rock in new[] { "rock_1", "rock_2", "rock_3" })
        {
            var g = new Session(Session.NewGame("Игрок"), 3);
            g.State.Step = 8;
            g.Recompute();
            var worth = 0f;
            const int n = 3000;

            for (var i = 0; i < n; i++)
            {
                g.AddItem(rock, 1, true);
                var loot = g.CrackRock(rock);
                worth += loot.Stones + loot.Gu.Sum(x => g.SellPrice(x));
            }

            var price = ItemBook.Get(rock).Price;
            var back = worth / n / price;
            notes.Add($"  {rock}: returns {back:P0} of its price when everything is sold");
            Check(back > 0.3f && back < 1.0f, $"{rock} returns between 30% and 100% of its price ({back:P0})");
        }
    }

    static void SaveRoundTrip()
    {
        var s = new Session(Session.NewGame("Лань"), 5);
        s.AddGu(s.MakeGu("moonlight", 2, true), true);
        s.AddItem("heal_pill", 3, true);
        s.SetFlag("x");
        var options = new JsonSerializerOptions { IncludeFields = true };
        var json = JsonSerializer.Serialize(s.State, options);
        var back = JsonSerializer.Deserialize<SaveData>(json, options);
        Check(back.Gu.Count == 1 && back.Gu[0].Affixes.Count == 2 && back.Items[0].Count == 3 && back.Flags.Contains("x") && back.Quests.Count == 1, "a save survives being written and read");
    }

    // ---------------------------------------------------------------------- A bot plays the story

    static void Playthrough(string ending, int seed, List<string> report, bool verbose)
    {
        var s = new Session(Session.NewGame("Бот"), seed);
        var kills = new Dictionary<string, int>();
        var grind = 0;
        var quests = 0;
        s.QuestDone += _ => quests++;

        void Kill(string enemy)
        {
            s.Collect(s.Killed(enemy));
            kills[enemy] = kills.TryGetValue(enemy, out var n) ? n + 1 : 1;
        }

        // The beasts the hero can farm at this point of the story.
        string Prey() => s.Chapter >= 5 ? "blood_wolf" : s.Chapter >= 4 ? "storm_wolf" : s.Chapter >= 3 ? "bandit" : s.Chapter >= 2 ? "wolf" : "fox";

        void Farm(Func<bool> until, string why)
        {
            var guard = 0;

            while (!until() && guard++ < 5000)
            {
                Kill(Prey());
                grind++;

                // Stones go to the spring, keeping what the next gate costs.
                var keep = s.State.Step <= 3 ? 50 : s.State.Step <= 7 ? 200 : 0;

                if (s.State.Stones > keep)
                {
                    s.Meditate(s.State.Stones - keep);
                }

                if (s.WhyNoBreakthrough() == null)
                {
                    s.Breakthrough();
                }
            }

            Check(guard < 5000, $"the bot could not: {why}");
        }

        var steps = 0;

        while (s.MainQuest != null && steps++ < 400)
        {
            var quest = s.MainQuest;
            var def = Story.Get(quest.Quest);
            var step = s.StepOf(quest);
            var startStep = s.State.Step;
            var startGrind = grind;

            switch (step.Kind)
            {
                case StepKind.Talk:
                    var talk = s.TalkTo(step.Target);
                    Check(talk != null && talk.Finish != null && talk.Lines.Length > 0, $"{def.Id}: {step.Target} has the quest's words");
                    talk?.Finish?.Invoke();
                    break;

                case StepKind.Kill:
                    var prey = Bestiary.All.First(e => step.Target.EndsWith("*") ? e.Id.StartsWith(step.Target.TrimEnd('*')) : e.Id == step.Target).Id;

                    for (var i = quest.Progress; i < step.Count; i++)
                    {
                        Kill(prey);
                    }

                    break;

                case StepKind.Have:
                    var herb = Valley.Things.FirstOrDefault(t => t.Kind == ThingKind.Herb && t.Content == step.Target);
                    var dropper = Bestiary.All.FirstOrDefault(e => e.Loot != null && Bestiary.Table(e.Loot).Items.Any(i => i.Item == step.Target));
                    Check(herb != null || dropper != null, $"{def.Id}: {step.Target} can be gathered or dropped");
                    var tries = 0;

                    while (s.Count(step.Target) < step.Count && s.MainQuest == quest && s.StepOf(quest) == step && tries++ < 400)
                    {
                        if (herb != null)
                        {
                            s.UseThing(herb.Id);
                        }
                        else if (dropper != null)
                        {
                            Kill(dropper.Id);
                        }
                    }

                    break;

                case StepKind.Reach:
                    // Through a door, if the place is behind one.
                    var door = Valley.Things.FirstOrDefault(t => t.Kind == ThingKind.Door && Valley.PlaceAt(t.To)?.Id == step.Target);

                    if (door != null)
                    {
                        var use = s.UseThing(door.Id);
                        Check(use.Ok, $"{def.Id}: the door {door.Id} opens ({use.Message})");
                    }

                    s.Reached(step.Target);
                    break;

                case StepKind.Use:
                    foreach (var thing in Valley.Things.Where(t => step.Target.EndsWith("*") ? t.Id.StartsWith(step.Target.TrimEnd('*')) : t.Id == step.Target))
                    {
                        if (s.MainQuest != quest || s.StepOf(quest) != step)
                        {
                            break;
                        }

                        var use = s.UseThing(thing.Id);
                        Check(use.Ok && use.Finish != null, $"{def.Id}: {thing.Id} can be used ({use.Message})");
                        use.Finish?.Invoke();
                    }

                    break;

                case StepKind.Equip:
                    var gu = s.State.Gu.First(g => g.Def == step.Target);
                    s.Restore();
                    Check(gu.Refined || s.Refine(gu.Uid), $"{def.Id}: the Gu can be refined");
                    Check(s.EquipAuto(gu.Uid), $"{def.Id}: the Gu can be slotted");
                    break;

                case StepKind.Cultivate:
                    Farm(() => s.State.Step >= step.Count, $"reach step {step.Count} in {def.Id}");
                    break;

                case StepKind.Encounter:
                    Check(s.WantsEncounter(step.Target), $"{def.Id}: the encounter is wanted");

                    foreach (var wave in Valley.Encounter(step.Target).Waves)
                    {
                        foreach (var (enemy, count) in wave.Enemies)
                        {
                            for (var i = 0; i < count; i++)
                            {
                                Kill(enemy);
                            }
                        }
                    }

                    s.EncounterWon(step.Target);
                    break;

                case StepKind.Choose:
                    var bowl = s.UseThing("blood_gu");
                    Check(bowl.Ok && bowl.Service == "choose" && s.PendingChoice() == step, "the bowl offers the choice");
                    s.Choose(ending);
                    break;
            }

            // Keep the bot's Gu in order: refine and slot what it finds, as a player would.
            s.Restore();

            // Between quests, half the spare stones go to the spring (the rest is for the shop and fusion).
            if (s.MainQuest != quest && s.State.Step >= 1)
            {
                var keep = s.State.Step <= 3 ? 50 : s.State.Step <= 7 ? 200 : 0;
                s.Meditate(Math.Max(0, (s.State.Stones - keep) / 2));
                s.State.Stones -= Math.Max(0, (s.State.Stones - keep) / 2);

                if (s.WhyNoBreakthrough() == null)
                {
                    s.Breakthrough();
                }
            }

            foreach (var gu in s.State.Gu.ToArray())
            {
                if (!gu.Refined && s.WhyNoRefine(gu) == null)
                {
                    s.Refine(gu.Uid);
                    s.Restore();
                }
            }

            if (verbose && s.MainQuest != quest)
            {
                report.Add($"  {def.Id} {def.Title,-26} → {Cultivation.Name(s.State.Step),-28} камни {s.State.Stones,4}  добор {grind - startGrind,3}  Гу {s.State.Gu.Count}");
            }
        }

        Check(s.MainQuest == null && s.Has("finished") && s.Has(ending), $"the story can be played to {ending}");
        Check(quests == Story.All.Count(q => q.Main), $"every main quest was done ({quests})");
        Check(ending != "ending_demonic" || s.State.Gu.Any(g => g.Def == "blood_moon"), "the demonic ending gives the Blood Moon Gu");
        Check(ending != "ending_righteous" || s.State.Talent >= 1, "the righteous ending gives talent S");
        Check(s.Rank == 3, "the hero ends at rank 3");

        if (verbose)
        {
            report.Insert(0, $"\nPLAYTHROUGH ({ending}): {kills.Values.Sum()} kills, of them {grind} farmed for the gates; final {Cultivation.Name(s.State.Step)}, talent {Cultivation.TalentName(s.State.Talent)}");
            report.Add("  kills: " + string.Join(", ", kills.OrderByDescending(k => k.Value).Select(k => $"{k.Key} {k.Value}")));
            Check(grind < 140, $"the story does not ask for much farming ({grind} extra kills)");

            // Side quests can all be taken and done by the end.
            // (Those that open with the story itself; the chains of the wider world are walked by the regions' playthrough.)
            foreach (var side in Story.All.Where(q => !q.Main && (q.Needs == "" || System.Text.RegularExpressions.Regex.IsMatch(q.Needs, "^m\\d+_done$")) && (q.Clan == "" || s.ReputationOf(q.Clan) >= q.NeedsReputation)))
            {
                Check(s.Offers(side.Giver).Contains(side), $"side quest {side.Id} is on offer by the end");
            }
        }
    }

    // ---------------------------------------------------------------------- Balance

    /// <summary>How long fights take with what the hero has when the story sends them there.</summary>
    static void Balance(List<string> report)
    {
        report.Add("\nBALANCE (the hero at the step the story expects, one attack Gu of the rank, palm strikes between casts)");
        report.Add("  enemy                 hp   hero step  time to kill  blows to die");
        // The step the hero is at when the story first sends them against each enemy.
        var expected = new (string enemy, int step, string gu)[]
        {
            ("fox", 0, "moonlight"), ("wolf", 1, "moonlight"), ("ragged_ear", 2, "moonlight"), ("cave_gu", 2, "moonlight"), ("stone_guard", 3, "moonlight"),
            ("bandit", 4, "moonglow"), ("crooked_fang", 4, "moonglow"), ("xiong_fighter", 5, "moonglow"), ("bai_fighter", 5, "moonglow"),
            ("storm_wolf", 6, "moonglow"), ("hundred_king", 6, "moonglow"), ("thunder_crown_wolf", 7, "moonglow"),
            ("blood_puppet", 8, "golden_moon"), ("blood_wolf", 8, "golden_moon"), ("elder", 8, "golden_moon"),
            // The wild lands: masters of the fourth and fifth ranks.
            ("frost_wolf", 12, "moon_sickle"), ("ice_bear", 12, "moon_sickle"), ("thunder_roc", 13, "moon_sickle"), ("bone_soldier", 13, "moon_sickle"),
            ("fire_ape", 14, "moon_sickle"), ("lake_hermit", 14, "moon_sickle"), ("frost_emperor", 14, "moon_sickle"), ("python_queen", 15, "moon_sickle"),
            ("roc_king", 16, "moon_sickle"), ("ape_king", 16, "moon_sickle"), ("bone_general", 17, "moon_sickle"), ("lake_dragon", 18, "moon_sickle"),
        };

        foreach (var (id, step, guId) in expected)
        {
            var enemy = Bestiary.Get(id);
            var s = new Session(Session.NewGame("Весы"), 1);
            s.State.Step = step;
            s.State.Talent = step >= 7 ? 0.82f : step >= 3 ? 0.62f : 0.44f;
            s.State.Settings.Hunger = HungerRule.Off;
            s.Recompute();
            var gu = s.AddGu(s.MakeGu(guId, 0, true), true);
            gu.MasteryXp = step >= 6 ? 200 : step >= 2 ? 90 : 0;
            s.Equip(gu.Uid, false, 0);
            s.Restore();

            // A simple fight: cast when able, otherwise three palm strikes every 1.6 s.
            var health = enemy.Health;
            var time = 0f;
            var combo = 0;
            var next = 0f;
            const float dt = 0.1f;
            // Hits a cast lands on one foe: two bolts of a fan, about half the strikes of a storm.
            var skill = GuBook.Get(guId).Skill;
            var fan = skill.Shape == SkillShape.Fan ? 2 : skill.Shape == SkillShape.Storm ? Math.Max(1, skill.Count / 2) : 1;

            while (health > 0 && time < 600)
            {
                s.Tick(dt);
                time += dt;

                if (s.WhyNot(0) == null)
                {
                    var cast = s.CastSlot(0);

                    for (var i = 0; i < fan; i++)
                    {
                        health -= s.SkillHit(cast, enemy).Damage;
                    }
                }
                else if (time >= next)
                {
                    health -= s.MeleeHit(combo, enemy).Damage;
                    combo = (combo + 1) % 3;
                    next = time + (combo == 0 ? 0.9f : 0.45f);
                }
            }

            // A player lands about half of what a bot that never moves or misses does.
            time *= 2;
            var blows = s.Stats.MaxHealth / (enemy.Damage * s.Stats.DamageTaken);
            report.Add($"  {enemy.Name,-20} {enemy.Health,5:0}   {step,2}        {time,6:0.0} s      {blows,5:0.0}");
            var low = enemy.Boss ? 40 : 2f;
            var high = enemy.Boss ? 170 : 16;
            Check(time >= low && time <= high, $"{id}: a fight of {low}–{high} s (it takes {time:0.0} s)");
            Check(blows >= (enemy.Boss ? 5 : 6), $"{id}: the hero stands at least {(enemy.Boss ? 5 : 6)} blows ({blows:0.0})");
        }

        report.AddRange(notes);
    }
}
