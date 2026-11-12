using System;
using System.Collections.Generic;

namespace GuWorld.Core
{
    /// <summary>What comes of walking up to someone and pressing the key.</summary>
    public class Talk
    {
        public NpcDef Npc;
        public Line[] Lines = new Line[0];
        /// <summary>"shop", "gamble", "board", "inn" to open once the lines are said, or "".</summary>
        public string Service = "";
        /// <summary>The shelf a shop opens (Markets), and the clan whose prices it asks.</summary>
        public string Shop = "";
        public string Clan = "";
        /// <summary>A conversation to hold after the lines (or instead of them), or null.</summary>
        public DialogueRun Dialogue;
        /// <summary>Called when the last line has been read.</summary>
        public Action Finish;
    }

    /// <summary>What comes of using a thing in the world.</summary>
    public class Use
    {
        public bool Ok;
        /// <summary>Why not, or what happened.</summary>
        public string Message = "";
        public Line[] Lines = new Line[0];
        public Loot Loot;
        /// <summary>"spring", "fusion", "door", "lantern", "choose", "alchemy", "board", "travel", or "".</summary>
        public string Service = "";
        public Action Finish;
    }

    public partial class Session
    {
        // ------------------------------------------------------------------ The log

        public QuestState StateOf(string quest) => State.Quests.Find(q => q.Quest == quest);

        public bool IsActive(string quest)
        {
            var state = StateOf(quest);

            return state != null && !state.Done;
        }

        public bool IsDone(string quest) => Has(quest + "_done");

        public IEnumerable<QuestState> ActiveQuests()
        {
            foreach (var quest in State.Quests)
            {
                if (!quest.Done)
                {
                    yield return quest;
                }
            }
        }

        /// <summary>The main quest in hand (null once the story is told).</summary>
        public QuestState MainQuest
        {
            get
            {
                foreach (var quest in State.Quests)
                {
                    if (!quest.Done && Story.Get(quest.Quest).Main)
                    {
                        return quest;
                    }
                }

                return null;
            }
        }

        public StepDef StepOf(QuestState quest)
        {
            var def = Story.Get(quest.Quest);

            return def != null && quest.Step < def.Steps.Length ? def.Steps[quest.Step] : null;
        }

        /// <summary>The step's line with its count: "Убейте серых волков 3 / 6".</summary>
        public string StepText(QuestState quest)
        {
            var step = StepOf(quest);

            if (step == null)
            {
                return "";
            }

            if (step.Kind == StepKind.Have)
            {
                return step.Count > 1 ? $"{step.Text} · {Math.Min(Count(step.Target), step.Count)} / {step.Count}" : step.Text;
            }

            return step.Count > 1 && step.Kind != StepKind.Cultivate ? $"{step.Text} · {quest.Progress} / {step.Count}" : step.Text;
        }

        /// <summary>Where the compass should point for a quest's step.</summary>
        public bool MarkerOf(QuestState quest, out MapPoint at)
        {
            var step = StepOf(quest);
            at = default;

            if (step == null)
            {
                return false;
            }

            var marker = string.IsNullOrEmpty(step.Marker) ? step.Target : step.Marker;

            return Atlas.Locate(marker, out at);
        }

        public void StartQuest(string id)
        {
            var def = Story.Get(id);

            if (def == null || IsActive(id))
            {
                return;
            }

            State.Quests.RemoveAll(q => q.Quest == id);
            State.Quests.Add(new QuestState { Quest = id });

            foreach (var gift in def.Gives)
            {
                Give(gift, true);
            }
            Recompute();
            QuestStarted?.Invoke(def);
            RefreshQuests();
            Touch();
        }

        static bool Matches(string pattern, string id)
        {
            if (string.IsNullOrEmpty(pattern))
            {
                return true;
            }

            // Several targets, any of them: "a|b|c".
            if (pattern.IndexOf('|') >= 0)
            {
                foreach (var one in pattern.Split('|'))
                {
                    if (Matches(one, id))
                    {
                        return true;
                    }
                }

                return false;
            }

            return pattern.EndsWith("*") ? id != null && id.StartsWith(pattern.Substring(0, pattern.Length - 1)) : pattern == id;
        }

        /// <summary>Something happened that a quest may be waiting for.</summary>
        public void Notify(StepKind kind, string id, int count = 1)
        {
            var changed = false;

            foreach (var quest in new List<QuestState>(State.Quests))
            {
                if (quest.Done)
                {
                    continue;
                }

                var step = StepOf(quest);

                if (step == null || step.Kind != kind || !Matches(step.Target, id) || !Meets(step.Needs))
                {
                    continue;
                }

                quest.Progress += count;
                changed = true;

                if (quest.Progress >= step.Count)
                {
                    CompleteStep(quest);
                }
            }

            if (changed)
            {
                // The step that follows may already be met (the pelts are in the bag).
                RefreshQuests();
                Touch();
            }
        }

        /// <summary>Steps that wait on a state of things (the bag, the slots, the step) rather than on an event.</summary>
        public void RefreshQuests()
        {
            for (var guard = 0; guard < 40; guard++)
            {
                var again = false;

                foreach (var quest in new List<QuestState>(State.Quests))
                {
                    if (quest.Done)
                    {
                        continue;
                    }

                    var step = StepOf(quest);

                    if (step == null)
                    {
                        continue;
                    }

                    var met = false;

                    switch (step.Kind)
                    {
                        case StepKind.Have:
                            met = Count(step.Target) >= step.Count;
                            break;
                        case StepKind.Equip:
                            foreach (var gu in Slotted())
                            {
                                met |= Matches(step.Target, gu.Def);
                            }

                            break;
                        case StepKind.Cultivate:
                            met = State.Step >= step.Count;
                            break;
                    }

                    if (met)
                    {
                        CompleteStep(quest);
                        again = true;
                    }
                }

                if (!again)
                {
                    break;
                }
            }
        }

        void CompleteStep(QuestState quest)
        {
            var def = Story.Get(quest.Quest);
            var step = def.Steps[quest.Step];

            if (step.Kind == StepKind.Have && step.Takes)
            {
                RemoveItem(step.Target, step.Count);
            }

            foreach (var gift in step.Gives)
            {
                Give(gift, false);
            }

            quest.Step++;
            quest.Progress = 0;

            if (quest.Step >= def.Steps.Length)
            {
                CompleteQuest(quest, def);
            }

            Touch();
        }

        void Give(string what, bool refined)
        {
            if (what.StartsWith("stones:"))
            {
                AddStones(int.Parse(what.Substring(7)));
            }
            else if (GuBook.Get(what) != null)
            {
                AddGu(MakeGu(what, 0, refined));
            }
            else
            {
                AddItem(what);
            }
        }

        void CompleteQuest(QuestState quest, QuestDef def)
        {
            quest.Done = true;
            var reward = def.Reward;
            Say($"Задание выполнено: {def.Title}");
            AddStones(reward.Stones);

            foreach (var (item, count) in reward.Items)
            {
                AddItem(item, count);
            }

            foreach (var gu in reward.Gu)
            {
                Give(gu, true);
            }

            foreach (var (clan, change) in reward.Reputation)
            {
                ChangeReputation(clan, change, false);
            }

            if (reward.Talent > State.Talent)
            {
                State.Talent = reward.Talent;
                Say($"Талант вырос: {Cultivation.TalentName(State.Talent)} ({(int)Math.Round(State.Talent * 100)}%)");
            }

            SetFlag(def.Id + "_done");

            foreach (var flag in reward.Flags)
            {
                SetFlag(flag);
            }

            if (def.Id == "m21")
            {
                if (Has("ending_demonic"))
                {
                    AddGu(MakeGu("blood_moon", 2, true));
                }
                else
                {
                    State.Talent = 1f;
                    AddStones(500);
                    AddItem("scroll_sky_canopy");
                    Say("Источник одарил вас: талант S (100%)");
                }
            }

            if (def.Repeatable)
            {
                State.Quests.Remove(quest);
            }

            Recompute();
            QuestDone?.Invoke(def);
            GainInsight(reward.Insight);
            Achieve();

            if (def.Next != null)
            {
                StartQuest(def.Next);
            }
        }

        // ------------------------------------------------------------------ People

        public bool IsGone(NpcDef npc) => !string.IsNullOrEmpty(npc.GoneAfter) && Has(npc.GoneAfter);

        /// <summary>The side quests someone has to offer now.</summary>
        public List<QuestDef> Offers(string npc)
        {
            var offers = new List<QuestDef>();

            foreach (var quest in Story.All)
            {
                if (quest.Giver == npc && Meets(quest.Needs) && !IsActive(quest.Id) && (quest.Repeatable || !IsDone(quest.Id))
                    && (quest.Clan == "" || ReputationOf(quest.Clan) >= quest.NeedsReputation))
                {
                    offers.Add(quest);
                }
            }

            return offers;
        }

        /// <summary>True if talking to someone would move a quest on or start one (a mark over their head).</summary>
        public bool HasBusiness(string npc)
        {
            foreach (var quest in ActiveQuests())
            {
                var step = StepOf(quest);

                if (step != null && step.Kind == StepKind.Talk && step.Target == npc && Meets(step.Needs))
                {
                    return true;
                }
            }

            var def = Atlas.Npc(npc);

            return def != null && def.Service != "board" && Offers(npc).Count > 0;
        }

        public Talk TalkTo(string npcId)
        {
            var npc = Atlas.Npc(npcId);

            if (npc == null || IsGone(npc))
            {
                return null;
            }

            OpenCodex("person:" + npc.Id);

            // Those whose clan hates the hero say nothing useful.
            if (Hostile(npc.Clan))
            {
                return new Talk { Npc = npc, Lines = new[] { new Line(npcId, "Убирайся, пока цел.") } };
            }

            // A quest step that waits for this talk.
            foreach (var quest in ActiveQuests())
            {
                var step = StepOf(quest);

                if (step != null && step.Kind == StepKind.Talk && step.Target == npcId && Meets(step.Needs))
                {
                    return new Talk { Npc = npc, Lines = step.Lines, Finish = () => Notify(StepKind.Talk, npcId) };
                }
            }

            // A side quest to offer (the board's errands are taken from its own page).
            if (npc.Service != "board")
            {
                var offers = Offers(npcId);

                if (offers.Count > 0)
                {
                    var quest = offers[0];

                    return new Talk { Npc = npc, Lines = quest.Offer, Finish = () => StartQuest(quest.Id) };
                }
            }

            // A conversation of their own.
            var dialogue = Converse(npcId);

            if (dialogue != null)
            {
                return new Talk { Npc = npc, Dialogue = dialogue, Service = "", Shop = npc.Shop, Clan = npc.Clan };
            }

            var bark = npc.Barks.Length > 0 ? npc.Barks[Random.Next(npc.Barks.Length)] : "…";
            var service = npc.Service;

            if ((service == "shop" || service == "gamble") && !WillTrade(npc))
            {
                service = "";
                bark = "Тебе я ничего не продам.";
            }

            return new Talk { Npc = npc, Lines = new[] { new Line(npcId, bark) }, Service = service, Shop = npc.Shop, Clan = npc.Clan };
        }

        // ------------------------------------------------------------------ Things

        public bool IsUsed(string thing) => State.Used.Contains(thing);

        public Use UseThing(string id)
        {
            var thing = Atlas.Thing(id);

            if (thing == null)
            {
                return new Use { Message = "" };
            }

            if (!Meets(thing.Needs))
            {
                return new Use { Message = thing.Locked != "" ? thing.Locked : Explain(thing.Needs) };
            }

            switch (thing.Kind)
            {
                case ThingKind.Lantern:
                    LightLantern(thing.Id);
                    Restore();
                    CureAll();
                    Touch();
                    return new Use { Ok = true, Service = "lantern", Message = "Лунный фонарь запомнил вас. Здоровье и эссенция восстановлены." };

                case ThingKind.Furnace:
                    return new Use { Ok = true, Service = "alchemy", Message = thing.Name };

                case ThingKind.Board:
                    return new Use { Ok = true, Service = "board", Message = thing.Content };

                case ThingKind.Lore:
                {
                    var entry = Codex.LoreEntry(thing.Content);
                    var opened = OpenCodex(thing.Content);
                    var quest = UseQuestObject(thing);

                    if (quest.Ok)
                    {
                        quest.Lines = entry != null ? new[] { new Line("", entry.Text) } : quest.Lines;

                        return quest;
                    }

                    return new Use { Ok = true, Lines = entry != null ? new[] { new Line("", entry.Text) } : new Line[0], Message = opened ? "" : "Вы перечитали надпись." };
                }

                case ThingKind.Shrine:
                {
                    var message = Pray(thing);
                    var quest = UseQuestObject(thing);

                    if (quest.Ok)
                    {
                        quest.Message = message;

                        return quest;
                    }

                    return new Use { Ok = message != "" && !message.StartsWith("Сегодня"), Message = message };
                }

                case ThingKind.Spring:
                    return new Use { Ok = true, Service = "spring" };

                case ThingKind.FusionAltar:
                    return new Use { Ok = true, Service = "fusion" };

                case ThingKind.Door:
                    return new Use { Ok = true, Service = "door" };

                case ThingKind.Chest:
                    if (IsUsed(id))
                    {
                        return new Use { Message = "Пусто." };
                    }

                    State.Used.Add(id);
                    var loot = RollLoot(thing.Content);
                    Collect(loot);
                    Notify(StepKind.Use, id);
                    return new Use { Ok = true, Loot = loot, Message = thing.Name };

                case ThingKind.Herb:
                    AddItem(thing.Content);
                    Notify(StepKind.Use, id);
                    return new Use { Ok = true };

                default:
                    return UseQuestObject(thing);
            }
        }

        Use UseQuestObject(ThingDef thing)
        {
            foreach (var quest in ActiveQuests())
            {
                var step = StepOf(quest);

                if (step == null || !Matches(step.Target, thing.Id))
                {
                    continue;
                }

                if (step.Kind == StepKind.Use)
                {
                    if (step.Count > 1)
                    {
                        if (IsUsed(thing.Id))
                        {
                            continue;
                        }

                        State.Used.Add(thing.Id);
                    }

                    var id = thing.Id;

                    // A used thing is remembered (an opened gate lets a fight begin: "<id>_used").
                    return new Use { Ok = true, Lines = step.Lines, Finish = () => { SetFlag(id + "_used"); Notify(StepKind.Use, id); } };
                }
            }

            // The last choice is made at the bowl.
            foreach (var quest in ActiveQuests())
            {
                var step = StepOf(quest);

                if (step != null && step.Kind == StepKind.Choose && thing.Id == "blood_gu")
                {
                    return new Use { Ok = true, Service = "choose" };
                }
            }

            return new Use { Message = IsUsed(thing.Id) ? "Здесь больше нечего делать." : "Сейчас это ни к чему." };
        }

        /// <summary>The answers of the step that waits for a choice (null if none does).</summary>
        public StepDef PendingChoice()
        {
            foreach (var quest in ActiveQuests())
            {
                var step = StepOf(quest);

                if (step != null && step.Kind == StepKind.Choose)
                {
                    return step;
                }
            }

            return null;
        }

        public void Choose(string flag)
        {
            var step = PendingChoice();

            if (step == null)
            {
                return;
            }

            SetFlag(flag);
            Notify(StepKind.Choose, step.Target);
        }

        /// <summary>True if a thing can be used for something right now (it glows).</summary>
        public bool IsWanted(ThingDef thing)
        {
            switch (thing.Kind)
            {
                case ThingKind.Chest: return !IsUsed(thing.Id);
                case ThingKind.Lore: return !InCodex(thing.Content) || WantsUse(thing.Id);
                case ThingKind.Shrine: return !PrayedToday(thing.Id);
                case ThingKind.Lantern: return !Lit(thing.Id);
                case ThingKind.QuestObject: break;
                default: return true;
            }

            foreach (var quest in ActiveQuests())
            {
                var step = StepOf(quest);

                if (step != null && (step.Kind == StepKind.Use || step.Kind == StepKind.Choose) && (Matches(step.Target, thing.Id) || (step.Kind == StepKind.Choose && thing.Id == "blood_gu")))
                {
                    return step.Count <= 1 || !IsUsed(thing.Id);
                }
            }

            return false;
        }

        // ------------------------------------------------------------------ Fights

        /// <summary>An enemy died by the hero's hand: insight, the quests' counts, the loot it leaves.</summary>
        public Loot Killed(string enemyId) => Killed(enemyId, "");

        /// <summary>An enemy fell (an elite, if `elite` names one: twice the insight and the stones, and its loot rolled twice).</summary>
        public Loot Killed(string enemyId, string elite)
        {
            var def = Bestiary.Get(enemyId);

            // Nothing in the immortal aperture counts.
            if (def == null || InSandbox)
            {
                return new Loot();
            }

            State.Kills++;
            Tally(enemyId);
            OpenCodex("beast:" + enemyId);

            if (def.Boss)
            {
                SetFlag(enemyId + "_dead");
                State.Bosses++;
            }

            if (!string.IsNullOrEmpty(def.Clan))
            {
                KilledOf(def.Clan, def.Boss);
            }

            BountyKill(enemyId);
            Notify(StepKind.Kill, enemyId);
            var isElite = !string.IsNullOrEmpty(elite) && Elites.Get(elite) != null && !def.Boss;
            GainInsight(def.Insight * (isElite ? 2 : 1));

            if (isElite)
            {
                State.Elites++;
            }

            Achieve();
            var loot = RollLoot(def.Loot);

            if (isElite)
            {
                var more = RollLoot(def.Loot);
                loot.Stones = loot.Stones * 2 + more.Stones;
                loot.Items.AddRange(more.Items);
                loot.Gu.AddRange(more.Gu);
            }

            return loot;
        }

        /// <summary>The encounter whose turn it is in the story, if the hero is to fight it now.</summary>
        public bool WantsEncounter(string id)
        {
            // The wider world's bosses can be fought whenever the hero comes, once (duels only when asked for).
            var def = Atlas.Encounter(id);

            if (def != null && !Atlas.InValley(def) && !id.StartsWith("duel_") && !Has(id + "_won") && Meets(def.Needs))
            {
                return true;
            }

            foreach (var quest in ActiveQuests())
            {
                var step = StepOf(quest);

                if (step != null && step.Kind == StepKind.Encounter && step.Target == id)
                {
                    return true;
                }
            }

            return false;
        }

        public void EncounterWon(string id)
        {
            SetFlag(id + "_won");
            Notify(StepKind.Encounter, id);
        }

        public void Reached(string place) => Notify(StepKind.Reach, place);

        /// <summary>True if a quest's step waits for this thing to be used.</summary>
        bool WantsUse(string thing)
        {
            foreach (var quest in ActiveQuests())
            {
                var step = StepOf(quest);

                if (step != null && step.Kind == StepKind.Use && Matches(step.Target, thing) && (step.Count <= 1 || !IsUsed(thing)))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
