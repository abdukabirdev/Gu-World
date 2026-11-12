using System;
using System.Collections.Generic;

namespace GuWorld.Core
{
    /// <summary>
    /// The codex (pages open by themselves as the hero meets places, beasts, Gu, herbs and people, and
    /// from books and steles), the tallies of kills, and the achievements they add up to.
    /// </summary>
    public partial class Session
    {
        public event Action<CodexEntry> CodexOpened;
        public event Action<AchievementDef> Achieved;

        public bool InCodex(string id) => State.Codex.Contains(id);

        /// <summary>Opens a page of the codex (once).</summary>
        public bool OpenCodex(string id)
        {
            if (string.IsNullOrEmpty(id) || State.Codex.Contains(id))
            {
                return false;
            }

            State.Codex.Add(id);
            var lore = Codex.LoreEntry(id);

            if (lore != null)
            {
                SetFlag("codex_" + id);
                Say($"Кодекс: {lore.Title}");
                CodexOpened?.Invoke(lore);
            }

            Achieve();

            return true;
        }

        /// <summary>Reads a book from the bag: its page opens in the codex (the book stays).</summary>
        public bool ReadBook(string item)
        {
            var def = ItemBook.Get(item);

            if (def == null || def.Kind != ItemKind.Book || Count(item) <= 0 || string.IsNullOrEmpty(def.Lore))
            {
                return false;
            }

            if (!OpenCodex(def.Lore))
            {
                Say("Вы это уже читали.");

                return false;
            }

            return true;
        }

        /// <summary>The pages of a kind opened, and how many there are.</summary>
        public (int open, int all) CodexCount(CodexKind kind)
        {
            var open = 0;
            var all = 0;

            foreach (var entry in Codex.Everything())
            {
                if (entry.Kind != kind)
                {
                    continue;
                }

                all++;

                if (State.Codex.Contains(entry.Id))
                {
                    open++;
                }
            }

            return (open, all);
        }

        /// <summary>The pages opened, of a kind, in the codex's order.</summary>
        public List<CodexEntry> CodexPages(CodexKind kind)
        {
            var pages = new List<CodexEntry>();

            foreach (var entry in Codex.Everything())
            {
                if (entry.Kind == kind && State.Codex.Contains(entry.Id))
                {
                    pages.Add(entry);
                }
            }

            return pages;
        }

        /// <summary>The interface reports the hero came into a place or a settlement (its page opens; quests may wait for it).</summary>
        public void Entered(string place)
        {
            if (OpenCodex("place:" + place))
            {
                var name = Atlas.Place(place)?.Name ?? Regions.Settlement(place)?.Name;

                if (name != null)
                {
                    Say($"Открыто место: {name}");
                }
            }

            Reached(place);
        }

        // ------------------------------------------------------------------ Tallies

        public int KillsOf(string enemy)
        {
            var tally = State.KillsBy.Find(t => t.Id == enemy);

            return tally?.Count ?? 0;
        }

        void Tally(string enemy)
        {
            var tally = State.KillsBy.Find(t => t.Id == enemy);

            if (tally == null)
            {
                State.KillsBy.Add(tally = new Tally { Id = enemy });
            }

            tally.Count++;
        }

        // ------------------------------------------------------------------ Achievements

        public bool HasAchievement(string id) => State.Achievements.Contains(id);

        /// <summary>The progress of an achievement towards its goal.</summary>
        public int Progress(AchievementDef achievement)
        {
            var counts = achievement.Counts;
            var colon = counts.IndexOf(':');
            var kind = colon > 0 ? counts.Substring(0, colon) : counts;
            var what = colon > 0 ? counts.Substring(colon + 1) : "";

            switch (kind)
            {
                case "kills": return what == "" ? State.Kills : KillsOf(what);
                case "bosses": return State.Bosses;
                case "gu": return State.Gu.Count;
                case "fusions": return State.Fusions;
                case "pills": return State.PillsMade;
                case "lanterns": return State.Lanterns.Count;
                case "codex": return State.Codex.Count;
                case "quests": return SideQuestsDone();
                case "flag": return Has(what) ? 1 : 0;
                case "rep": return ReputationOf(what);
                case "stones": return State.Stones;
                case "step": return State.Step;
                case "deaths": return State.Deaths;
                case "moves": return State.Moves.Count;
                case "grade": return BestGrade();
                case "elites": return State.Elites;
                case "moves_cast": return State.MovesCast;
                case "perfect": return State.Gu.FindAll(g => g.Quality >= GuBook.MaxQuality).Count;
                case "revived": return State.CicadaDay > 0 ? 1 : 0;
                default: return 0;
            }
        }

        int SideQuestsDone()
        {
            var done = 0;

            foreach (var quest in Story.All)
            {
                if (!quest.Main && !quest.Repeatable && IsDone(quest.Id))
                {
                    done++;
                }
            }

            return done;
        }

        /// <summary>Checks every achievement not yet had (cheap: a few dozen comparisons).</summary>
        public void Achieve()
        {
            foreach (var achievement in Codex.Achievements)
            {
                if (State.Achievements.Contains(achievement.Id) || Progress(achievement) < achievement.Goal)
                {
                    continue;
                }

                State.Achievements.Add(achievement.Id);
                Say($"Достижение: {achievement.Name}");
                Achieved?.Invoke(achievement);
            }
        }
    }
}
