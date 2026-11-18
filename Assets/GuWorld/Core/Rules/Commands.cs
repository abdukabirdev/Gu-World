using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace GuWorld.Core
{
    /// <summary>A command of the developer's console: its name, how it is written, what it does, and the doing.</summary>
    public class CommandDef
    {
        public string Name;
        public string Usage;
        public string About;
        /// <summary>Runs with the session and the words after the name; returns what to say.</summary>
        public Func<Session, string[], string> Run;
    }

    /// <summary>
    /// The developer's commands (ROADMAP 12.1, main's chat commands improved): one table the console,
    /// the F10 panel and the checks all run. The rules' own commands are here; the engine adds its own
    /// (teleport, time, spawning) with Add. A command used marks the save ("dev_used") so honest
    /// achievements can tell.
    /// </summary>
    public static class Commands
    {
        static readonly List<CommandDef> all = new List<CommandDef>();

        public static IReadOnlyList<CommandDef> All
        {
            get
            {
                Fill();

                return all;
            }
        }

        /// <summary>Adds a command (the engine's own); one of the same name is replaced.</summary>
        public static void Add(CommandDef command)
        {
            Fill();
            all.RemoveAll(c => c.Name == command.Name);
            all.Add(command);
        }

        public static CommandDef Find(string name)
        {
            Fill();

            return all.Find(c => c.Name == name);
        }

        /// <summary>The commands whose names start with what is typed (after the slash), for the console's hints.</summary>
        public static List<CommandDef> Matching(string typed)
        {
            Fill();
            var word = (typed ?? "").TrimStart('/').Split(' ')[0].ToLowerInvariant();

            return all.Where(c => c.Name.StartsWith(word)).OrderBy(c => c.Name).ToList();
        }

        /// <summary>Runs a line ("/give moonlight 2"); what to say back. Never throws.</summary>
        public static string Run(Session session, string line)
        {
            var words = (line ?? "").Trim().TrimStart('/').Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (words.Length == 0)
            {
                return "";
            }

            var command = Find(words[0].ToLowerInvariant());

            if (command == null)
            {
                return $"Нет команды «{words[0]}». /help — все команды.";
            }

            if (session == null && command.Name != "help" && command.Name != "find")
            {
                return "Команда работает только в игре.";
            }

            try
            {
                var said = command.Run(session, words.Skip(1).ToArray());

                if (session != null && command.Name != "help" && command.Name != "find" && command.Name != "stats")
                {
                    session.SetFlag("dev_used");
                }

                return said;
            }
            catch (Exception error)
            {
                return $"Ошибка: {error.Message}. Как писать: {command.Usage}";
            }
        }

        // ------------------------------------------------------------------ The rules' own commands

        static void Fill()
        {
            if (all.Count > 0)
            {
                return;
            }

            all.Add(new CommandDef { Name = "help", Usage = "/help [команда]", About = "Все команды или одна подробно", Run = Help });
            all.Add(new CommandDef { Name = "find", Usage = "/find <слово>", About = "Найти Гу, вещь, врага, место или задание по имени — их id", Run = FindIds });
            all.Add(new CommandDef { Name = "give", Usage = "/give <id> [сколько] [качество 0–3]", About = "Дать Гу (очищенного) или вещь", Run = Give });
            all.Add(new CommandDef { Name = "stones", Usage = "/stones <сколько>", About = "Прибавить первобытных камней (можно отнять)", Run = Stones });
            all.Add(new CommandDef { Name = "rank", Usage = "/rank <1–5> [ступень 1–4]", About = "Поставить ранг и ступень", Run = Rank });
            all.Add(new CommandDef { Name = "talent", Usage = "/talent <проценты>", About = "Поставить талант", Run = Talent });
            all.Add(new CommandDef { Name = "heal", Usage = "/heal", About = "Здоровье и эссенция — полные", Run = (s, a) => { s.Restore(); return "Здоровье и эссенция полные"; } });
            all.Add(new CommandDef { Name = "god", Usage = "/god", About = "Бессмертие: вкл / выкл", Run = (s, a) => { s.Godmode = !s.Godmode; return s.Godmode ? "Бессмертие включено" : "Бессмертие выключено"; } });
            all.Add(new CommandDef { Name = "quest", Usage = "/quest <id>", About = "Начать задание", Run = Quest });
            all.Add(new CommandDef { Name = "flag", Usage = "/flag <id>", About = "Поставить флаг сюжета", Run = (s, a) => { s.SetFlag(Need(a, 0)); s.Recompute(); return $"Флаг {a[0]} поставлен"; } });
            all.Add(new CommandDef { Name = "unflag", Usage = "/unflag <id>", About = "Снять флаг сюжета", Run = (s, a) => { s.State.Flags.Remove(Need(a, 0)); s.Recompute(); return $"Флаг {a[0]} снят"; } });
            all.Add(new CommandDef { Name = "rep", Usage = "/rep <клан> <сколько>", About = "Изменить отношение клана", Run = (s, a) => { s.ChangeReputation(Need(a, 0), Int(a, 1), false); return $"Клан {a[0]}: {s.ReputationOf(a[0])}"; } });
            all.Add(new CommandDef { Name = "codex", Usage = "/codex", About = "Открыть весь кодекс", Run = Codex });
        }

        static string Help(Session session, string[] words)
        {
            if (words.Length > 0)
            {
                var command = Find(words[0].TrimStart('/'));

                return command == null ? $"Нет команды «{words[0]}»" : $"{command.Usage} — {command.About}";
            }

            return string.Join("\n", all.OrderBy(c => c.Name).Select(c => $"{c.Usage} — {c.About}"));
        }

        static string FindIds(Session session, string[] words)
        {
            var word = string.Join(" ", words).ToLowerInvariant();

            if (word.Length < 2)
            {
                return "Нужно слово хотя бы из двух букв";
            }

            bool Fits(string id, string name) => id.ToLowerInvariant().Contains(word) || (name ?? "").ToLowerInvariant().Contains(word);

            var found = new List<string>();
            found.AddRange(GuBook.All.Where(g => Fits(g.Id, g.Name)).Select(g => $"Гу {g.Id} — {g.Name}"));
            found.AddRange(ItemBook.All.Where(i => Fits(i.Id, i.Name)).Select(i => $"вещь {i.Id} — {i.Name}"));
            found.AddRange(Bestiary.All.Where(e => Fits(e.Id, e.Name)).Select(e => $"враг {e.Id} — {e.Name}"));
            found.AddRange(Atlas.Places.Where(p => Fits(p.Id, p.Name)).Select(p => $"место {p.Id} — {p.Name}"));
            found.AddRange(Story.All.Where(q => Fits(q.Id, q.Title)).Select(q => $"задание {q.Id} — {q.Title}"));

            return found.Count == 0 ? $"Ничего по «{word}»" : string.Join("\n", found.Take(14)) + (found.Count > 14 ? $"\n…ещё {found.Count - 14}" : "");
        }

        static string Give(Session session, string[] words)
        {
            var id = Need(words, 0);
            var count = Math.Max(1, words.Length > 1 ? Int(words, 1) : 1);
            var quality = Math.Max(0, Math.Min(GuBook.MaxQuality, words.Length > 2 ? Int(words, 2) : 0));

            if (GuBook.Get(id) != null)
            {
                for (var i = 0; i < Math.Min(count, 20); i++)
                {
                    session.AddGu(session.MakeGu(id, quality, true), true);
                }

                session.Recompute();

                return $"Гу {GuBook.Get(id).Name} ×{Math.Min(count, 20)}";
            }

            if (ItemBook.Get(id) != null)
            {
                session.AddItem(id, count, true);

                return $"{ItemBook.Get(id).Name} ×{count}";
            }

            return $"Нет Гу или вещи «{id}». /find — найти по имени.";
        }

        static string Stones(Session session, string[] words)
        {
            var count = Int(words, 0);

            if (count >= 0)
            {
                session.AddStones(count, true);
            }
            else
            {
                session.State.Stones = Math.Max(0, session.State.Stones + count);
            }

            return $"Камней: {session.State.Stones}";
        }

        static string Rank(Session session, string[] words)
        {
            var rank = Math.Max(1, Math.Min(5, Int(words, 0)));
            var stage = Math.Max(1, Math.Min(4, words.Length > 1 ? Int(words, 1) : 1));
            session.State.Step = Math.Min(Cultivation.MaxStep, (rank - 1) * 4 + stage - 1);
            session.State.Insight = 0;
            session.Recompute();
            session.Restore();

            return Cultivation.Name(session.State.Step);
        }

        static string Talent(Session session, string[] words)
        {
            var percent = float.Parse(Need(words, 0).TrimEnd('%'), CultureInfo.InvariantCulture);
            session.State.Talent = Math.Max(0.05f, Math.Min(1f, percent / 100f));
            session.Recompute();

            return $"Талант {Cultivation.TalentName(session.State.Talent)} · {session.State.Talent * 100:0}%";
        }

        static string Quest(Session session, string[] words)
        {
            var id = Need(words, 0);

            if (Story.Get(id) == null)
            {
                return $"Нет задания «{id}». /find — найти по имени.";
            }

            session.StartQuest(id);

            return $"Задание: {Story.Get(id).Title}";
        }

        static string Codex(Session session, string[] words)
        {
            var added = 0;

            foreach (var entry in GuWorld.Core.Codex.Everything())
            {
                if (!session.State.Codex.Contains(entry.Id))
                {
                    session.State.Codex.Add(entry.Id);
                    added++;
                }
            }

            return $"Кодекс открыт: +{added} страниц";
        }

        static string Need(string[] words, int at)
        {
            if (words.Length <= at || string.IsNullOrWhiteSpace(words[at]))
            {
                throw new ArgumentException("не хватает слов");
            }

            return words[at];
        }

        static int Int(string[] words, int at) => int.Parse(Need(words, at), CultureInfo.InvariantCulture);
    }
}
