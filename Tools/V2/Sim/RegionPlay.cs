using System;
using System.Collections.Generic;
using System.Linq;
using GuWorld.Core;

/// <summary>
/// A bot that walks every quest of the wider mountain from its giver to its reward, doing what each
/// step asks the way a player would (talking, killing, gathering, reaching, using), and only what
/// the world offers: an item a step wants must drop from something or grow somewhere, a thing it
/// wants used must be usable then. A quest the bot cannot finish is a quest a player cannot.
/// </summary>
static partial class Program
{
    static void RegionPlay(List<string> report)
    {
        var s = new Session(Session.NewGame("Бот"), 31);
        // The story so far: the wider world's quests open after the valley's chapters.
        foreach (var main in Story.All.Where(q => q.Main))
        {
            s.SetFlag(main.Id + "_done");
        }

        s.State.Step = 8;
        s.Recompute();
        var done = new List<string>();
        var stuck = new Dictionary<string, string>();
        var log = new List<string>();

        for (var round = 0; round < 12; round++)
        {
            var progress = false;

            foreach (var quest in RegionQuests.All)
            {
                if (s.IsDone(quest.Id) || stuck.ContainsKey(quest.Id) && round > 10)
                {
                    continue;
                }

                if (!s.IsActive(quest.Id))
                {
                    // Earn the trust a quest asks for the way a player would: the boards' errands.
                    if (quest.Clan != "" && s.ReputationOf(quest.Clan) < quest.NeedsReputation && s.Meets(quest.Needs))
                    {
                        EarnTrust(s, quest.Clan, quest.NeedsReputation);
                    }

                    if (!s.Offers(quest.Giver).Contains(quest))
                    {
                        continue;
                    }

                    var talk = s.TalkTo(quest.Giver);

                    if (talk == null || talk.Finish == null)
                    {
                        stuck[quest.Id] = "the giver has nothing to say";
                        continue;
                    }

                    talk.Finish();
                }

                var why = Walk(s, quest, log);

                if (why == null)
                {
                    done.Add(quest.Id);
                    stuck.Remove(quest.Id);
                    progress = true;
                }
                else
                {
                    stuck[quest.Id] = why;
                }
            }

            if (!progress)
            {
                break;
            }
        }

        foreach (var quest in RegionQuests.All)
        {
            Check(s.IsDone(quest.Id), $"region quest {quest.Id} can be finished ({(stuck.TryGetValue(quest.Id, out var why) ? why : "never offered")})");
        }

        Check(s.Has("treaty_signed"), "the treaty is signed in the end");
        Check(s.StandingWith("bai") >= Standing.Friendly && s.StandingWith("xiong") >= Standing.Friendly, "both clans end as friends");
        report.Add($"\nREGIONS: {done.Count} of {RegionQuests.All.Length} quests walked; reputation " +
                   string.Join(", ", Clans.All.Select(c => $"{c.Id} {s.ReputationOf(c.Id)}")) + $"; {s.State.Codex.Count} codex pages, {s.State.Achievements.Count} achievements");
    }

    /// <summary>Hands in the boards' errands of the clan until it trusts the hero enough.</summary>
    static void EarnTrust(Session s, string clan, int need)
    {
        var board = Session.Boards.FirstOrDefault(b => b.Clan == clan);

        for (var day = 0; day < 40 && s.ReputationOf(clan) < need; day++)
        {
            if (board == null)
            {
                // A clan without a board: its quests' rewards are the way; nothing to grind.
                return;
            }

            foreach (var bounty in s.BoardToday(board.Settlement))
            {
                if (!s.TakeBounty(bounty))
                {
                    continue;
                }

                if (bounty.Kind == "kill")
                {
                    for (var i = 0; i < bounty.Count; i++)
                    {
                        s.Killed(bounty.Target);
                    }
                }
                else
                {
                    s.AddItem(bounty.Target, bounty.Count, true);
                }

                s.CompleteBounty(bounty);
            }

            s.NewDay();
        }
    }

    /// <summary>Does every step of a quest taken; null when done, or why the bot could not go on.</summary>
    static string Walk(Session s, QuestDef quest, List<string> log)
    {
        for (var guard = 0; guard < 40 && s.IsActive(quest.Id); guard++)
        {
            var state = s.StateOf(quest.Id);
            var step = s.StepOf(state);

            if (step == null)
            {
                return "no step";
            }

            var before = (state.Step, state.Progress);

            switch (step.Kind)
            {
                case StepKind.Talk:
                {
                    if (!s.Meets(step.Needs))
                    {
                        var colon = step.Needs.IndexOf(':');

                        if (step.Needs.StartsWith("rep:"))
                        {
                            var clan = step.Needs.Substring(4, step.Needs.IndexOf('>') - 4);
                            EarnTrust(s, clan, 25);
                        }

                        if (!s.Meets(step.Needs))
                        {
                            return $"the talk with {step.Target} waits for {step.Needs}";
                        }
                    }

                    var talk = s.TalkTo(step.Target);

                    if (talk?.Finish == null)
                    {
                        return $"{step.Target} does not take the step";
                    }

                    talk.Finish();
                    break;
                }

                case StepKind.Kill:
                {
                    var enemy = Bestiary.All.FirstOrDefault(e => step.Target.EndsWith("*") ? e.Id.StartsWith(step.Target.TrimEnd('*')) : e.Id == step.Target);

                    if (enemy == null)
                    {
                        return "nothing to kill";
                    }

                    if (!Atlas.AllSpawns.Any(sp => sp.Enemy == enemy.Id) && !Atlas.AllEncounters.Any(e => e.Waves.Any(w => w.Enemies.Any(x => x.enemy == enemy.Id))))
                    {
                        return $"{enemy.Id} lives nowhere";
                    }

                    for (var i = state.Progress; i < step.Count; i++)
                    {
                        Collect(s, s.Killed(enemy.Id));
                    }

                    break;
                }

                case StepKind.Have:
                {
                    var missing = step.Count - s.Count(step.Target);

                    if (missing > 0)
                    {
                        if (!Obtain(s, step.Target, missing))
                        {
                            return $"{step.Target} cannot be got";
                        }
                    }

                    s.RefreshQuests();
                    break;
                }

                case StepKind.Reach:
                    s.Entered(step.Target);
                    break;

                case StepKind.Use:
                {
                    var targets = Atlas.AllThings.Where(t => step.Target.EndsWith("*") ? t.Id.StartsWith(step.Target.TrimEnd('*')) : t.Id == step.Target).ToList();

                    foreach (var thing in targets)
                    {
                        if (!s.IsActive(quest.Id) || s.StepOf(s.StateOf(quest.Id)) != step)
                        {
                            break;
                        }

                        if (!s.Meets(thing.Needs) && !Unlock(s, thing.Needs))
                        {
                            return $"{thing.Id} stays locked ({thing.Needs})";
                        }

                        var use = s.UseThing(thing.Id);
                        use.Finish?.Invoke();
                    }

                    break;
                }

                case StepKind.Encounter:
                {
                    var encounter = Atlas.Encounter(step.Target);

                    if (encounter == null || !s.Meets(encounter.Needs))
                    {
                        return $"the fight {step.Target} cannot begin ({encounter?.Needs})";
                    }

                    // The waves die by the hero's hand (their loot too), then the fight is won.
                    foreach (var wave in encounter.Waves)
                    {
                        foreach (var (enemy, count) in wave.Enemies)
                        {
                            for (var i = 0; i < count; i++)
                            {
                                Collect(s, s.Killed(enemy));
                            }
                        }
                    }

                    s.EncounterWon(step.Target);
                    break;
                }

                default:
                    return $"the bot does not know how to {step.Kind}";
            }

            var after = s.StateOf(quest.Id);

            if (s.IsActive(quest.Id) && after.Step == before.Step && after.Progress == before.Progress)
            {
                return $"step «{step.Text}» did not move";
            }
        }

        return s.IsDone(quest.Id) ? null : "too many steps";
    }

    static void Collect(Session s, Loot loot)
    {
        s.Collect(loot);
    }

    /// <summary>Gets an item the way the world offers it: a herb to pick, a beast to hunt, a chest to open, a shop.</summary>
    static bool Obtain(Session s, string item, int count)
    {
        var herb = Atlas.AllThings.FirstOrDefault(t => t.Kind == ThingKind.Herb && t.Content == item);

        if (herb != null)
        {
            s.AddItem(item, count, true);
            return true;
        }

        // A beast that drops it: hunted until there are enough (a cap: the drop must be likely enough to farm).
        var table = Bestiary.Tables.FirstOrDefault(t => t.Items.Any(i => i.Item == item) && Bestiary.All.Any(e => e.Loot == t.Id && !e.Boss));

        if (table != null)
        {
            var enemy = Bestiary.All.First(e => e.Loot == table.Id && !e.Boss);

            for (var i = 0; i < 80 && s.Count(item) < count; i++)
            {
                s.Collect(s.Killed(enemy.Id));

                if (s.Count(item) >= count)
                {
                    break;
                }
            }

            return s.Count(item) >= count;
        }

        // A chest that holds it, open now.
        var chest = Atlas.AllThings.FirstOrDefault(t => t.Kind == ThingKind.Chest && !s.IsUsed(t.Id) && Bestiary.Table(t.Content)?.Items.Any(i => i.Item == item && i.Chance >= 1) == true);

        if (chest != null && (s.Meets(chest.Needs) || Unlock(s, chest.Needs)))
        {
            s.UseThing(chest.Id);

            if (s.Count(item) >= count)
            {
                return true;
            }
        }

        var shelf = Markets.Ids.SelectMany(Markets.Get).FirstOrDefault(e => e.Item == item);

        if (shelf != null)
        {
            s.AddStones(shelf.Price * count * 2, true);

            for (var i = 0; i < count; i++)
            {
                s.Buy(shelf, "");
            }

            return true;
        }

        return false;
    }

    /// <summary>Meets what a locked thing waits for: a key from its chest, a boss to kill, a quest's flag.</summary>
    static bool Unlock(Session s, string needs)
    {
        if (needs.StartsWith("item:"))
        {
            var key = needs.Substring(5);
            var chest = Atlas.AllThings.FirstOrDefault(t => t.Kind == ThingKind.Chest && Bestiary.Table(t.Content)?.Items.Any(i => i.Item == key) == true);

            if (chest != null && s.Meets(chest.Needs))
            {
                s.UseThing(chest.Id);
            }

            var boss = Bestiary.All.FirstOrDefault(e => Bestiary.Table(e.Loot)?.Items.Any(i => i.Item == key && i.Chance >= 1) == true);

            if (s.Count(key) == 0 && boss != null)
            {
                s.Collect(s.Killed(boss.Id));
            }

            return s.Count(key) > 0;
        }

        if (needs.EndsWith("_dead"))
        {
            s.Killed(needs.Substring(0, needs.Length - 5));
            return s.Meets(needs);
        }

        return s.Meets(needs);
    }
}
