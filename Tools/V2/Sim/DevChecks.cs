using System.Linq;
using GuWorld.Core;

/// <summary>Checks of the developer's commands (ROADMAP 12.1): each does what it says, a bad line answers instead of throwing.</summary>
static partial class Program
{
    static void DevRules()
    {
        var s = new Session(Session.NewGame("Разработчик"), 3);
        var gu = GuBook.All.First(g => g.Rank == 1).Id;
        var before = s.State.Gu.Count;
        Check(Commands.Run(s, $"/give {gu} 2").StartsWith("Гу") && s.State.Gu.Count == before + 2 && s.State.Gu.Last().Refined, "/give gives refined Gu");
        var item = ItemBook.All[0].Id;
        var had = s.Count(item);
        Commands.Run(s, $"/give {item} 5");
        Check(s.Count(item) == had + 5, "/give gives things");
        Check(Commands.Run(s, "/give nothing_like_it").StartsWith("Нет"), "/give of what is not there says so");
        var stones = s.State.Stones;
        Commands.Run(s, "/stones 100");
        Check(s.State.Stones == stones + 100, "/stones adds");
        Commands.Run(s, "/stones -100000");
        Check(s.State.Stones == 0, "/stones takes, not below zero");
        Commands.Run(s, "/rank 3 2");
        Check(s.Rank == 3 && s.State.Step == 9, "/rank sets the rank and the stage");
        Commands.Run(s, "/talent 88");
        Check(System.Math.Abs(s.State.Talent - 0.88f) < 1e-4f, "/talent sets the talent");
        Check(Commands.Run(s, "/nonsense").StartsWith("Нет команды"), "an unknown command says so");
        Check(Commands.Run(s, "/rank").StartsWith("Ошибка"), "a command short of words answers how to write it");
        Check(Commands.Run(null, "/help").Contains("/give") && Commands.Run(null, "/give x").StartsWith("Команда работает только в игре"), "help works anywhere, the rest only in a game");
        Check(Commands.Run(s, "/find лун").Contains("moonlight"), "/find finds by the name");
        Commands.Run(s, "/codex");
        Check(s.State.Codex.Count == Codex.Everything().Count(), "/codex opens every page");
        Commands.Run(s, "/god");
        s.TakeDamage(100000);
        Check(s.Godmode && !s.Dead, "/god: no blow kills");
        Check(s.Has("dev_used"), "a command used marks the save");
        Check(Commands.Matching("/gi").Any(c => c.Name == "give") && Commands.Matching("").Count == Commands.All.Count, "the console's hints match what is typed");
    }
}
