using System;
using System.Collections.Generic;
using System.Linq;
using GuWorld.Core;

/// <summary>
/// Checks of the wider mountain's content (every id it names exists, every conversation can be
/// walked, every condition and effect is one the rules know) and of the rules added with it:
/// reputation, conversations, alchemy, Gu hunger, statuses and effects, the codex and achievements,
/// the boards' errands, travel between lanterns, the weather, the casters' minds.
/// </summary>
static partial class Program
{
    static readonly string[] ConditionKinds = { "flag", "quest", "done", "gu", "visited", "codex", "pill", "rep", "item", "kills", "standing" };
    static readonly string[] EffectKinds = { "flag", "unflag", "give", "take", "stones", "rep", "quest", "learn", "insight", "gu", "codex", "heal", "sleep", "shop", "gamble", "board", "fight", "alchemy", "travel" };

    static bool Known(string id) => ItemBook.Get(id) != null || GuBook.Get(id) != null;

    /// <summary>A condition names only things that exist.</summary>
    static string BadCondition(string condition)
    {
        if (string.IsNullOrEmpty(condition))
        {
            return null;
        }

        var c = condition.TrimStart('!');
        var colon = c.IndexOf(':');

        if (colon < 0)
        {
            return null;
        }

        var kind = c.Substring(0, colon);
        var rest = c.Substring(colon + 1);
        var name = new string(rest.TakeWhile(ch => ch != '>' && ch != '<' && ch != '=' && ch != '!').ToArray());

        if (!ConditionKinds.Contains(kind))
        {
            return $"unknown condition {kind}";
        }

        switch (kind)
        {
            case "quest":
            case "done": return Story.Get(name) == null ? $"no quest {name}" : null;
            case "item": return ItemBook.Get(name) == null ? $"no item {name}" : null;
            case "gu": return GuBook.Get(name) == null ? $"no Gu {name}" : null;
            case "rep": return Clans.Get(name) == null ? $"no clan {name}" : null;
            case "visited": return Atlas.Thing(name) == null ? $"no lantern {name}" : null;
            case "pill": return Alchemy.Get(name) == null ? $"no pill {name}" : null;
            case "kills": return Bestiary.Get(name) == null ? $"no enemy {name}" : null;
            default: return null;
        }
    }

    static string BadEffect(string effect)
    {
        var parts = effect.Split(':');

        if (!EffectKinds.Contains(parts[0]))
        {
            return $"unknown effect {parts[0]}";
        }

        string Part(int i) => parts.Length > i ? parts[i] : "";

        switch (parts[0])
        {
            case "give":
            case "take": return Known(Part(1)) ? null : $"nothing called {Part(1)}";
            case "rep": return Clans.Get(Part(1)) == null ? $"no clan {Part(1)}" : null;
            case "quest": return Story.Get(Part(1)) == null ? $"no quest {Part(1)}" : null;
            case "learn": return Part(1) == "pill" ? (Alchemy.Get(Part(2)) == null ? $"no pill {Part(2)}" : null) : Recipes.Get(Part(2)) == null ? $"no fusion {Part(2)}" : null;
            case "gu": return GuBook.Get(Part(1)) == null ? $"no Gu {Part(1)}" : null;
            case "codex": return Codex.Everything().Any(e => e.Id == Part(1)) ? null : $"no codex page {Part(1)}";
            case "fight": return Atlas.Encounter(Part(1)) == null ? $"no encounter {Part(1)}" : null;
            default: return null;
        }
    }

    static void MoreContent()
    {
        // Gu: what they eat exists, the clan that keeps them exists, every new active one has a skill that does something.
        foreach (var gu in GuBook.All)
        {
            Check(gu.Food == "" || ItemBook.Get(gu.Food) != null, $"Gu {gu.Id}: food {gu.Food}");
            Check(gu.Clan == "" || Clans.Get(gu.Clan) != null, $"Gu {gu.Id}: clan {gu.Clan}");
            Check(gu.Appetite >= 1, $"Gu {gu.Id}: appetite");

            if (gu.Skill != null && gu.Skill.Shape == SkillShape.Buff)
            {
                Check(gu.Skill.Buff != BuffKind.None && gu.Skill.Duration > 0, $"Gu {gu.Id}: a buff that lasts");
            }

            if (gu.Skill != null && gu.Skill.Status != StatusKind.None)
            {
                Check(gu.Skill.StatusTime > 0, $"Gu {gu.Id}: its status lasts");
            }
        }

        // Every new Gu can be had: a fusion, a shop, a loot table or a quest.
        foreach (var gu in MoreGu.All)
        {
            var had = Recipes.All.Any(r => r.Result == gu.Id) || Markets.Ids.SelectMany(Markets.Get).Any(e => e.Gu == gu.Id)
                      || Bestiary.Tables.Any(t => t.GuPool.Contains(gu.Id)) || Story.All.Any(q => q.Reward.Gu.Contains(gu.Id));
            Check(had, $"Gu {gu.Id} can be got somewhere");
        }

        foreach (var item in MoreItems.All)
        {
            Check(item.Kind != ItemKind.Book || Codex.LoreEntry(item.Lore) != null, $"book {item.Id}: codex page {item.Lore}");
            Check(item.Kind != ItemKind.Scroll || Recipes.Get(item.Teaches) != null, $"scroll {item.Id}: recipe {item.Teaches}");
            Check(item.Kind != ItemKind.Consumable || item.Heal > 0 || item.Essence > 0 || item.Cures != StatusKind.None || item.Buff != BuffKind.None || item.BoostSeconds > 0 || item.Insight > 0, $"pill {item.Id} does something");
        }

        // Every herb grows somewhere or can be bought; every food can be had.
        foreach (var item in ItemBook.All.Where(i => i.Kind == ItemKind.Herb || i.Kind == ItemKind.Food))
        {
            var had = Atlas.AllThings.Any(t => t.Kind == ThingKind.Herb && t.Content == item.Id) || Markets.Ids.SelectMany(Markets.Get).Any(e => e.Item == item.Id)
                      || Bestiary.Tables.Any(t => t.Items.Any(i => i.Item == item.Id)) || Alchemy.All.Any(r => r.Result == item.Id);
            Check(had, $"{item.Kind} {item.Id} can be got somewhere");
        }

        foreach (var enemy in Bestiary.All)
        {
            Check(enemy.Gu.All(g => GuBook.Get(g) != null && GuBook.Get(g).Skill != null), $"enemy {enemy.Id}: its Gu are active Gu");
            Check(enemy.Mind != EnemyMind.Caster || enemy.Gu.Length > 0, $"caster {enemy.Id} carries Gu");
            Check(enemy.Clan == "" || Clans.Get(enemy.Clan) != null, $"enemy {enemy.Id}: clan {enemy.Clan}");
            Check(enemy.Inflicts == StatusKind.None || enemy.InflictTime > 0 && enemy.InflictChance > 0, $"enemy {enemy.Id}: what it inflicts lasts");
        }

        foreach (var recipe in Alchemy.All)
        {
            Check(ItemBook.Get(recipe.Result) != null, $"pill recipe {recipe.Id}: result");
            Check(recipe.Items.All(i => ItemBook.Get(i.item) != null), $"pill recipe {recipe.Id}: herbs");
            var taught = recipe.Known || Atlas.AllNpcs.Any(n => n.Teaches.Contains(recipe.Id)) || Dialogues.All.Any(d => d.Nodes.Any(n => n.Options.Any(o => o.Effects.Contains("learn:pill:" + recipe.Id))))
                         || Story.All.Any(q => q.Reward.Flags.Contains("learnt_" + recipe.Id));
            Check(taught, $"pill recipe {recipe.Id} can be learnt");
        }

        foreach (var npc in Atlas.AllNpcs)
        {
            Check(Clans.Get(npc.Clan) != null, $"npc {npc.Id}: clan {npc.Clan}");
            Check(npc.Home == "" || Regions.Settlement(npc.Home) != null, $"npc {npc.Id}: home {npc.Home}");
            Check(npc.Dialogue == "" || Dialogues.Get(npc.Dialogue) != null, $"npc {npc.Id}: dialogue {npc.Dialogue}");
            Check(npc.Shop == "" || Markets.Ids.Contains(npc.Shop), $"npc {npc.Id}: shop {npc.Shop}");
            Check(npc.Teaches.All(t => Alchemy.Get(t) != null), $"npc {npc.Id}: teaches");
            Check(npc.Service != "shop" || npc.Shop != "" || Atlas.InValley(npc), $"shopkeeper {npc.Id} has a shelf");
            Check(npc.Dialogue != "" || npc.Barks.Length > 0, $"npc {npc.Id} says something");
        }

        foreach (var thing in Atlas.AllThings)
        {
            Check(thing.Home == "" || Regions.Settlement(thing.Home) != null, $"thing {thing.Id}: home {thing.Home}");
            Check(thing.Kind != ThingKind.Lore || Codex.LoreEntry(thing.Content) != null, $"stele {thing.Id}: page {thing.Content}");
            Check(thing.Kind != ThingKind.Shrine || Codex.Blessing(thing.Content) != null, $"shrine {thing.Id}: blessing {thing.Content}");
            Check(thing.Kind != ThingKind.Board || Session.Board(thing.Content) != null, $"board {thing.Id}: errands of {thing.Content}");
            Check(BadCondition(thing.Needs) == null, $"thing {thing.Id}: {BadCondition(thing.Needs)}");
        }

        foreach (var spawn in Atlas.AllSpawns)
        {
            Check(BadCondition(spawn.Needs) == null, $"spawn {spawn.Id}: {BadCondition(spawn.Needs)}");
        }

        foreach (var quest in Story.All)
        {
            Check(quest.Clan == "" || Clans.Get(quest.Clan) != null, $"quest {quest.Id}: clan");
            Check(quest.Reward.Reputation.All(r => Clans.Get(r.clan) != null), $"quest {quest.Id}: reputation of clans that exist");
            Check(BadCondition(quest.Needs) == null, $"quest {quest.Id}: needs {quest.Needs}");

            foreach (var step in quest.Steps)
            {
                Check(BadCondition(step.Needs) == null, $"quest {quest.Id}: step needs {step.Needs}");
            }
        }

        // Conversations: every node reachable from the start, every link goes somewhere, every rule is a known one.
        foreach (var dialogue in Dialogues.All)
        {
            Check(dialogue.Node("start") != null, $"dialogue {dialogue.Id} has a start");
            var reached = new HashSet<string>();
            var queue = new Queue<string>();
            queue.Enqueue("start");

            while (queue.Count > 0)
            {
                var id = queue.Dequeue();

                if (!reached.Add(id) || dialogue.Node(id) == null)
                {
                    continue;
                }

                foreach (var option in dialogue.Node(id).Options)
                {
                    if (option.Next != "")
                    {
                        Check(dialogue.Node(option.Next) != null, $"dialogue {dialogue.Id}: {id} → {option.Next}");
                        queue.Enqueue(option.Next);
                    }

                    foreach (var need in option.Needs)
                    {
                        Check(BadCondition(need) == null, $"dialogue {dialogue.Id}: {BadCondition(need)}");
                    }

                    foreach (var effect in option.Effects)
                    {
                        Check(BadEffect(effect) == null, $"dialogue {dialogue.Id}: {BadEffect(effect)}");
                    }
                }

                foreach (var line in dialogue.Node(id).Lines)
                {
                    Check(line.Who == "" || line.Who == "hero" || Atlas.Npc(line.Who) != null, $"dialogue {dialogue.Id}: speaker {line.Who}");
                }
            }

            foreach (var node in dialogue.Nodes)
            {
                Check(reached.Contains(node.Id), $"dialogue {dialogue.Id}: node {node.Id} can be reached");
                Check(node.Options.Length > 0, $"dialogue {dialogue.Id}: node {node.Id} has a way on");
            }
        }

        foreach (var board in Session.Boards)
        {
            Check(Regions.Settlement(board.Settlement.Replace("_masters", "")) != null && Clans.Get(board.Clan) != null, $"board {board.Settlement}");
            Check(board.Hunts.All(h => Bestiary.Get(h.enemy) != null && h.min <= h.max) && board.Gathers.All(g => ItemBook.Get(g.item) != null && g.min <= g.max), $"board {board.Settlement}: errands");
            Check(Atlas.AllThings.Any(t => t.Kind == ThingKind.Board && t.Content == board.Settlement), $"board {board.Settlement} stands somewhere");
        }

        foreach (var achievement in Codex.Achievements)
        {
            Check(achievement.Goal >= 1 && achievement.Counts != "", $"achievement {achievement.Id}");
        }

        Check(Codex.Everything().Select(e => e.Id).Distinct().Count() == Codex.Everything().Count(), "codex ids are unique");
        Check(Atlas.AllThings.Count(t => t.Kind == ThingKind.Lantern) >= 30, "lanterns all over the mountain");
    }

    // ---------------------------------------------------------------------- Rules of the wider world

    static void MoreRules()
    {
        // Reputation: gains with a clan cost a little with its rivals; standings by band; prices follow.
        var s = new Session(Session.NewGame("Тест"), 5);
        Check(s.ReputationOf("bai") == Clans.Get("bai").Start, "a clan starts where its def says");
        s.ChangeReputation("bai", 30, true);
        Check(s.ReputationOf("bai") == 20 && s.StandingWith("bai") == Standing.Friendly, "thirty up from −10 is friendly");
        Check(s.ReputationOf("gu_yue") < Clans.Get("gu_yue").Start, "a gain with the Bai is a small loss with Gu Yue");
        var entry = new ShopEntry { Item = "heal_pill", Price = 100 };
        Check(s.PriceFor(entry, "bai") < 100 && s.PriceFor(entry, "bandit") > 100, "friends sell cheaper, enemies dearer");
        s.ChangeReputation("bai", -500, true);
        Check(s.ReputationOf("bai") == Clans.Min && s.Hostile("bai"), "reputation stops at its least; the hated are attacked");
        Check(s.Hostile("bandit") && !s.Hostile("mortal"), "bandits are hostile, mortals never");

        // Conditions and effects.
        s = new Session(Session.NewGame("Тест"), 6);
        s.State.Stones = 100;
        Check(s.Meets("stones>=100") && !s.Meets("stones>100") && s.Meets("!flag:none") && s.Meets(""), "comparisons and negation");
        s.Apply("give:jerky:3");
        Check(s.Count("jerky") == 3 && s.Meets("item:jerky>=3") && !s.Meets("item:jerky>3"), "give and item conditions");
        s.Apply("stones:-40");
        Check(s.State.Stones == 60, "stones can be taken by an effect");
        s.Apply("rep:xiong:+25");
        Check(s.Meets("rep:xiong>=20") && s.Meets("standing:xiong=friendly"), "reputation by an effect");
        var requested = "";
        s.Requested += (what, arg) => requested = what + "/" + arg;
        s.Apply("fight:duel_bai");
        Check(requested == "fight/duel_bai", "an effect the interface carries out is requested");

        // A conversation: walk Bai Lin's, learn a recipe, the once-only answers and the greyed ones.
        s = new Session(Session.NewGame("Тест"), 7);
        s.State.Stones = 500;
        s.ChangeReputation("bai", 15, true);
        var run = s.Converse("bai_lin");
        Check(run != null && run.Lines.Length > 0 && run.Answers.Count >= 2, "Bai Lin holds a conversation");
        var teach = run.Answers.FindIndex(a => a.Text.StartsWith("Научите"));
        Check(teach >= 0 && run.Choose(teach) && run.Node.Id == "teach", "an answer leads to its node");
        var bandage = run.Answers.FindIndex(a => a.Text.StartsWith("Повязка"));
        var locked = run.Answers.Find(a => a.Text.StartsWith("Пилюля бережливости"));
        Check(locked != null && !locked.Enabled && locked.Why != "", "an answer out of reach is shown greyed with its reason");
        Check(bandage >= 0 && run.Choose(bandage) && s.KnowsPill("bandage") && s.State.Stones == 480, "a recipe bought in a conversation");
        Check(run.Node.Id == "learnt" && !run.Over, "the conversation goes on");
        run.Choose(0);
        Check(run.Node.Id == "start", "and back");
        var bye = run.Answers.Count - 1;
        run.Choose(bye);
        Check(run.Over, "and ends");
        run = s.Converse("xiong_jian");
        var clan = run.Answers.FindIndex(a => a.Text.StartsWith("Расскажите"));
        run.Choose(clan);
        run.Choose(0);
        var before = s.ReputationOf("xiong");
        run.Choose(run.Answers.FindIndex(a => a.Text.StartsWith("Расскажите")));
        Check(run.Answers.All(a => a.Text != "Ха!"), "a once-only answer is not offered twice");
        Check(s.ReputationOf("xiong") == before, "and gives nothing twice");

        // Alchemy: a known recipe, the herbs and stones taken, the skill rises with practice.
        s = new Session(Session.NewGame("Тест"), 8);
        Check(s.KnowsPill("heal_pill") && !s.KnowsPill("clarity_pill"), "the first recipes are known");
        Check(s.WhyNoBatch(Alchemy.Get("heal_pill")) != null, "no batch without herbs");
        s.AddItem("ginseng", 20, true);
        s.AddItem("bitter_root", 20, true);
        s.AddStones(100, true);
        var batch = s.Brew("heal_pill");
        Check(batch.Ok && s.Count("heal_pill") >= 2 && s.Count("ginseng") == 19, "a batch gives its pills and takes its herbs");
        var stonesBefore = s.State.Stones;

        for (var i = 0; i < 12; i++)
        {
            s.Brew("heal_pill");
        }

        Check(s.State.Refining >= 2, "the refining skill rises with practice");
        Check(s.State.Stones == stonesBefore - 24, "every batch takes its stones");
        Check(Alchemy.SuccessChance(1, 4) < 0.2f && Alchemy.SuccessChance(4, 4) == 1, "a hard recipe fails in green hands");

        // Pills of the wider world: cures, buffs, boosts.
        s = new Session(Session.NewGame("Тест"), 9);
        s.Afflict(StatusKind.Poison, 5, 10);
        Check(s.Suffers(StatusKind.Poison), "venom takes hold");
        var health = s.State.Health;
        s.Tick(2);
        Check(s.State.Health < health - 9, "and works over time");
        s.AddItem("antidote", 1, true);
        Check(s.UseConsumable("antidote") && !s.Suffers(StatusKind.Poison), "an antidote ends it");
        Check(!s.UseConsumable("antidote"), "no antidote left");
        s.AddItem("focus_pill", 1, true);
        var power = s.Stats.Power;
        Check(s.UseConsumable("focus_pill") && Math.Abs(s.Stats.Power - power - 0.2f) < 0.001f, "a focus pill strengthens the Gu");
        s.Tick(121);
        Check(Math.Abs(s.Stats.Power - power) < 0.001f && s.Effects.Count == 0, "and wears off");
        s.AddItem("wind_pill", 1, true);
        s.UseConsumable("wind_pill");
        Check(s.Hasted && s.SpeedFactor > 1.2f, "a wind pill hastens");
        s.Afflict(StatusKind.Chill, 0.3f, 4);
        s.Afflict(StatusKind.Chill, 0.3f, 4);
        Check(s.Held && s.Suffers(StatusKind.Freeze), "cold on the cold freezes");
        s.Tick(2);
        Check(!s.Held, "and thaws");
        s.ApplyBuff(BuffKind.Unstoppable, 0.5f, 5);
        s.Afflict(StatusKind.Stun, 0, 2);
        Check(!s.Held, "the unstoppable cannot be stunned");
        s.ApplyBuff(BuffKind.Stealth, 2, 8);
        Check(s.Stealthed && Math.Abs(s.BreakStealth() - 3) < 0.001f && !s.Stealthed, "a blow from stealth is three times stronger and ends it");

        // Gu hunger: the soft rule weakens, the canon's kills, the bag feeds.
        s = new Session(Session.NewGame("Тест"), 10);
        var moon = s.AddGu(s.MakeGu("moonlight", 0, true), true);
        s.Equip(moon.Uid, false, 0);
        var full = s.PowerOf(moon);

        for (var day = 0; day < 4; day++)
        {
            s.NewDay();
        }

        Check(s.IsStarving(moon) && s.PowerOf(moon) < full * 0.75f, "a starving Gu is weaker (soft rule)");
        s.AddItem("moon_orchid", 1, true);
        Check(s.Feed(moon.Uid) && !s.IsHungry(moon) && Math.Abs(s.PowerOf(moon) - full) < 0.01f, "fed, it is whole again");
        s.AddItem("moon_orchid", 5, true);

        for (var day = 0; day < 12; day++)
        {
            s.NewDay();
        }

        Check(s.FindGu(moon.Uid) != null && s.Count("moon_orchid") < 5, "the bag feeds the Gu at dawn");
        s.State.Settings.Hunger = HungerRule.Canon;
        s.RemoveItem("moon_orchid", s.Count("moon_orchid"));

        for (var day = 0; day < 10; day++)
        {
            s.NewDay();
        }

        Check(s.FindGu(moon.Uid) == null && s.State.Active[0] == 0, "under the canon's rule a starved Gu dies and leaves its slot");
        s.State.Settings.Hunger = HungerRule.Off;
        var spark = s.AddGu(s.MakeGu("spark", 0, true), true);

        for (var day = 0; day < 20; day++)
        {
            s.NewDay();
        }

        Check(s.FindGu(spark.Uid) != null && !s.IsHungry(spark), "with hunger off, nothing starves");

        // The clock: midnight makes a new day; sleep moves it on.
        s = new Session(Session.NewGame("Тест"), 11);
        var today = s.State.Day;
        s.SetClock(23.9f);
        s.SetClock(0.1f);
        Check(s.State.Day == today + 1, "past midnight is a new day");
        s.SetClock(20);
        s.SleepTillMorning();
        Check(s.State.Day == today + 2 && Math.Abs(s.State.Hour - 6) < 0.01f, "sleep till morning");

        // Boards: the same errands for the same day; taken, done, handed in.
        var board = Session.Board("xiong_village");
        var a = Session.MakeBounty(board, 7, 0);
        var b = Session.MakeBounty(board, 7, 0);
        Check(a.Kind == b.Kind && a.Target == b.Target && a.Count == b.Count && a.Stones == b.Stones, "a day's errands are the same every time");
        Check(Enumerable.Range(1, 30).Select(d => Session.MakeBounty(board, d, 0).Target).Distinct().Count() > 2, "and differ from day to day");
        s = new Session(Session.NewGame("Тест"), 12);
        var errands = s.BoardToday("trade_post");
        Check(errands.Count == 3, "a board posts three errands");
        var hunt = errands.First(e => e.Kind == "kill");
        Check(s.TakeBounty(hunt) && s.BountiesRunning == 1, "an errand is taken");

        for (var i = 0; i < hunt.Count; i++)
        {
            s.Killed(hunt.Target);
        }

        var stonesNow = s.State.Stones;
        Check(s.BountyReady(hunt) && s.CompleteBounty(hunt) && s.State.Stones == stonesNow + hunt.Stones, "a hunt done is paid");
        s.NewDay();
        Check(s.BountiesRunning == 0 && s.BoardToday("trade_post").All(e => e.Day == s.State.Day), "a new day, new errands");

        // Travel between lanterns.
        s = new Session(Session.NewGame("Тест"), 13);
        s.LightLantern("lantern_village");
        s.LightLantern("lantern_river");
        s.AddStones(100, true);
        var river = Atlas.Thing("lantern_river");
        var from = new MapPoint(0, -10);
        Check(s.WhyNoTravel(from, river, true) != null, "no travel mid-fight");
        var cost = s.TravelCost(from, river);
        var purse = s.State.Stones;
        Check(s.Travel(from, "lantern_river") && s.State.Stones == purse - cost && s.State.Lantern == "lantern_river", "travel pays and moves the waking lantern");
        Check(!s.Travel(from, "lantern_bai"), "no travel to an unlit lantern");

        // The codex and achievements.
        s = new Session(Session.NewGame("Тест"), 14);
        s.AddItem("book_alchemy", 1, true);
        Check(s.ReadBook("book_alchemy") && s.InCodex("lore_alchemy") && !s.ReadBook("book_alchemy"), "a book opens its page once");
        s.Killed("fox");
        Check(s.InCodex("beast:fox") && s.HasAchievement("first_blood") && s.KillsOf("fox") == 1, "a kill opens the beast's page and counts");
        s.Killed("black_tail_thug");
        Check(s.ReputationOf("bandit") < Clans.Get("bandit").Start && s.ReputationOf("mortal") > 0, "killing bandits angers them and pleases the hamlets");

        // Shrines.
        s = new Session(Session.NewGame("Тест"), 15);
        var shrine = Atlas.Thing("shrine_pass");
        var cooldown = s.Stats.Cooldown;
        Check(s.Pray(shrine).StartsWith("Благословение") && s.Stats.Cooldown < cooldown, "a shrine blesses");
        Check(s.Pray(shrine).StartsWith("Сегодня"), "once a day");
        s.NewDay();
        Check(!s.PrayedToday("shrine_pass"), "and again tomorrow");

        // Weather and paths.
        Check(Weather.PathFactor(GuPath.Moon, Sky.Clear, true) > 1.1f && Weather.PathFactor(GuPath.Fire, Sky.Rain, false) < 0.9f, "the moon is stronger at night, fire weaker in the rain");
        Check(Enumerable.Range(1, 60).Select(d => Weather.At(d, 10)).Distinct().Count() >= 4, "the weather changes over the days");
        Check(Weather.At(5, 15) == Weather.At(5, 15), "the same day has the same sky");
        Check(Enumerable.Range(1, 200).All(d => Weather.At(d, 14, 600) != Sky.Rain), "no rain above the snow line");

        // Enemy statuses.
        var statuses = new EnemyStatuses();
        statuses.Apply(StatusKind.Poison, 10, 3);
        statuses.Apply(StatusKind.Chill, 0, 3);
        Check(statuses.SpeedFactor < 1 && !statuses.Held, "a chilled enemy is slower");
        statuses.Apply(StatusKind.Chill, 0, 3);
        Check(statuses.Held && statuses.Worst == StatusKind.Freeze, "and frozen by a second chill");
        var dot = statuses.Tick(1, 0.5f);
        Check(Math.Abs(dot - 10) < 0.01f, "venom goes past armour");

        // A caster's mind.
        var mind = new CasterMind(new[] { "ice_spike", "frost_breath", "water_shield" });
        Check(mind.Gu.Length == 3 && mind.PreferredDistance > 8, "a Bai patrol keeps its distance");
        var far = mind.Choose(15, false);
        Check(far != null && far.Id == "ice_spike", "a bolt from afar");
        Check(mind.Choose(15, false) == null, "then waits for it");
        var near = mind.Choose(4, true);
        Check(near != null && (near.Id == "frost_breath" || near.Id == "water_shield"), "close up, a cone or a shield");

        // Difficulty.
        s = new Session(Session.NewGame("Тест"), 16);
        s.State.Settings.Difficulty = Difficulty.Story;
        var soft = s.TakeDamage(10);
        s.State.Settings.Difficulty = Difficulty.Demonic;
        var hard = s.TakeDamage(10);
        Check(soft < 7 && hard > 16, "the difficulty scales the blows");

        // Quest offers wait on reputation.
        s = new Session(Session.NewGame("Тест"), 17);
        s.SetFlag("q_bai_gate_done");
        Check(!s.Offers("bai_yun").Any(q => q.Id == "q_bai_wolves"), "a clan's quest waits for its trust");
        s.ChangeReputation("bai", 40, true);
        Check(s.Offers("bai_yun").Any(q => q.Id == "q_bai_wolves"), "and is offered once trusted");
        var talk = s.TalkTo("bai_yun");
        Check(talk != null && talk.Lines.Length > 0, "the elder offers it");
        talk.Finish?.Invoke();
        Check(s.IsActive("q_bai_wolves"), "and it is taken");
        s.ChangeReputation("xiong", -200, true);
        var cold = s.TalkTo("xiong_dan");
        Check(cold.Service == "" && cold.Lines[0].Text.StartsWith("Убирайся"), "the hated get nothing from a clan's shop");
    }
}
