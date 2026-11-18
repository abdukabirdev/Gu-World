using System;
using System.Linq;
using System.Text.Json;
using GuWorld.Core;

/// <summary>
/// Checks of the new game's choices (ROADMAP 2.4, 2.5): the five paths and the talent each is born
/// with, the Ten Extreme Physique's roll, which paths a game may switch to, old saves moved to five
/// paths, the hero's name, and the story's words filled for every hero.
/// </summary>
static partial class Program
{
    static void PathRules()
    {
        // Five paths, easiest first, each its own.
        Check(DifficultyRules.All.Length == 5 && Beginnings.All.Length == 5, "five paths");
        Check(Beginnings.All.Select(b => b.Path).SequenceEqual(DifficultyRules.All), "the paths are offered easiest first");
        Check(DifficultyRules.All.Select(DifficultyRules.Name).Distinct().Count() == 5, "every path has its own name");

        for (var i = 1; i < Beginnings.All.Length; i++)
        {
            var easier = Beginnings.All[i - 1];
            var harder = Beginnings.All[i];
            Check(harder.Talent < easier.Talent, $"{harder.Name}: a smaller talent than {easier.Name}");
            Check(harder.Dots == easier.Dots + 1, $"{harder.Name}: one dot more");
            Check(DifficultyRules.DamageTaken(harder.Path) > DifficultyRules.DamageTaken(easier.Path), $"{harder.Name}: blows hurt more");
            Check(DifficultyRules.EnemyHealth(harder.Path) > DifficultyRules.EnemyHealth(easier.Path), $"{harder.Name}: enemies last longer");
            Check(DifficultyRules.DeathLoss(harder.Path) > DifficultyRules.DeathLoss(easier.Path), $"{harder.Name}: a fall costs more");
        }

        // The reference path is the numbers the game was balanced on.
        var reference = Beginnings.Of(Beginnings.Reference);
        Check(Math.Abs(reference.Talent - 0.44f) < 1e-4f && DifficultyRules.DamageTaken(Beginnings.Reference) == 1 && DifficultyRules.EnemyHealth(Beginnings.Reference) == 1,
            "the hard path is the balanced one: C 44%, every multiplier 1");
        Check(Math.Abs(Session.NewGame("x").Talent - 0.44f) < 1e-4f && Session.NewGame("x").Settings.Difficulty == Beginnings.Reference, "NewGame(name) plays the reference path");

        // Every path can refine its first Gu: a rank one aperture holds 30 essence at least.
        foreach (var way in Beginnings.All)
        {
            var s = new Session(Session.NewGame("Путник", way.Path, null, 5), 5);
            Check(s.State.Settings.Difficulty == way.Path && s.State.Settings.Born == way.Path, $"{way.Name}: the game is born on its path");
            Check(Math.Abs(s.State.Talent - way.Talent) < 1e-4f || s.State.ExtremePhysique, $"{way.Name}: the hero is born with its talent");
            Check(s.Stats.MaxEssence >= 30 - 1e-3f, $"{way.Name}: the first Gu can be refined ({s.Stats.MaxEssence:0} essence)");
            Check(Math.Abs(s.State.Essence - s.Stats.MaxEssence) < 0.5f, $"{way.Name}: the sea starts full");
        }

        // The Ten Extreme Physique: about one in ten on the easy path, never on the others, and the same for the same seed.
        var extreme = Enumerable.Range(1, 2000).Count(seed => Session.NewGame("x", Difficulty.Easy, null, seed).ExtremePhysique);
        Check(extreme > 140 && extreme < 260, $"the Ten Extreme Physique: about one easy hero in ten ({extreme} of 2000)");
        Check(Enumerable.Range(1, 500).All(seed => !Session.NewGame("x", Difficulty.Normal, null, seed).ExtremePhysique), "no extreme physique off the easy path");
        var lucky = Enumerable.Range(1, 200).First(seed => Session.NewGame("x", Difficulty.Easy, null, seed).ExtremePhysique);
        var born = Session.NewGame("x", Difficulty.Easy, null, lucky);
        Check(born.Talent >= 1 && Session.NewGame("x", Difficulty.Easy, null, lucky).ExtremePhysique, "the extreme physique gives talent 100%, the same for the same seed");

        // The player's own rules come with the new game; the path is set over them.
        var rules = new Settings { Hunger = HungerRule.Canon, AutoFeed = false, TravelCosts = false, Difficulty = Difficulty.Easy };
        var ruled = Session.NewGame("x", Difficulty.Impossible, rules, 3);
        Check(ruled.Settings.Hunger == HungerRule.Canon && !ruled.Settings.AutoFeed && !ruled.Settings.TravelCosts && ruled.Settings.Difficulty == Difficulty.Impossible,
            "a new game keeps the player's rules and its path");

        // A game may go easier than it was born, never harder.
        Check(DifficultyRules.CanSwitch(Difficulty.Hard, Difficulty.Easy) && DifficultyRules.CanSwitch(Difficulty.Hard, Difficulty.Hard), "a game may be made easier or kept");
        Check(!DifficultyRules.CanSwitch(Difficulty.Hard, Difficulty.Unbearable) && !DifficultyRules.CanSwitch(Difficulty.Easy, Difficulty.Normal), "a game may not be made harder than it was born");

        // Old saves: four difficulties, the demonic one (3) becomes the impossible path; born on what they play.
        var options = new JsonSerializerOptions { IncludeFields = true };
        var old = Session.NewGame("Старый");
        old.Version = 2;
        old.Settings.Difficulty = (Difficulty)3;
        var moved = new Session(JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(old, options), options), 1);
        Check(moved.State.Settings.Difficulty == Difficulty.Impossible && moved.State.Settings.Born == Difficulty.Impossible && moved.State.Version >= 3,
            "an old demonic save plays the impossible path");
        var oldNormal = Session.NewGame("Старый");
        oldNormal.Version = 2;
        oldNormal.Settings.Difficulty = Difficulty.Normal;
        var kept = new Session(oldNormal, 1);
        Check(kept.State.Settings.Difficulty == Difficulty.Normal && kept.State.Settings.Born == Difficulty.Normal, "an old normal save stays normal");

        // The hero's name.
        Check(Look.CleanName(null) == "Лань" && Look.CleanName("   ") == "Лань", "no name: Лань");
        Check(Look.CleanName("  фан   юань ") == "Фан юань", "a name is trimmed, its spaces squeezed, its first letter capital");
        Check(Look.CleanName("<b>Бай</b>!") == "BБайb", "a name keeps letters only (no markup)");
        Check(Look.CleanName("Оченьдлинноеимягероя").Length == Look.NameLength, "a name is cut to its length");
        Check(Session.NewGame("  мо бэй ").HeroName == "Мо бэй", "the new game takes the name cleaned");

        // The story's words: every token filled for every hero; the ceremony reads the talent the hero was born with.
        var texts = Story.All.SelectMany(q => q.Steps.SelectMany(st => st.Lines ?? new Line[0]).Concat(q.Offer ?? new Line[0]))
            .Select(l => l.Text)
            .Concat(Story.Endings.Values.SelectMany(v => v))
            .Concat(Atlas.AllNpcs.SelectMany(n => n.Barks ?? new string[0]))
            .ToList();
        Check(texts.Any(t => t.Contains("{")), "the story has words to fill");

        foreach (var way in Beginnings.All)
        {
            var state = Session.NewGame("Мо", way.Path, null, 9);
            var left = texts.Select(t => Words.Fill(t, state)).Where(t => t.Contains("{") || t.Contains("}")).ToList();
            Check(left.Count == 0, $"{way.Name}: every token is filled{(left.Count > 0 ? ": " + left[0] : "")}");
        }

        var ceremony = Story.All.First(q => q.Id == "m01").Steps[0].Lines;
        var cHero = Session.NewGame("Мо", Difficulty.Hard, null, 1);
        Check(Words.Fill(ceremony[0].Text, cHero).StartsWith("Мо.") && Words.Fill(ceremony[2].Text, cHero).StartsWith("Сорок четыре доли. Талант C."),
            "the ceremony names the hero and reads C 44% on the hard path");
        var aHero = Session.NewGame("Мо", Difficulty.Normal, null, 1);
        Check(Words.Fill(ceremony[2].Text, aHero).StartsWith("Шестьдесят две доли. Талант B."), "and B 62% on the normal path");
        Check(Words.Fill(ceremony[2].Text, born).Contains("Десяти Крайностей"), "and the extreme physique when the hero has it");
        Check(Words.Shares(30) == "тридцать долей" && Words.Shares(21) == "двадцать одна доля" && Words.Shares(100) == "сто долей" && Words.Shares(13) == "тринадцать долей",
            "shares in words agree with their number");

        // The story raises the talent by the same amount on every path (C 44 → B 62 → A 82 on the reference path, as before).
        var gains = Story.All.Where(q => q.Reward != null && q.Reward.TalentGain > 0).ToList();
        Check(gains.Count == 2 && Math.Abs(gains.Sum(q => q.Reward.TalentGain) - 0.38f) < 1e-4f, "the story raises the talent twice, by 38 points in all");
    }
}
