using System;
using System.Linq;
using System.Text.Json;
using GuWorld.Core;

/// <summary>The map's fog and the player's marks (ROADMAP 3.2).</summary>
static partial class Program
{
    static void MapRules()
    {
        var s = new Session(Session.NewGame("Мин"), 3);
        Check(Session.ExploreCells * Session.ExploreCell >= Regions.Half * 2, $"map: the cells cover the mountain ({Session.ExploreCells}²)");
        Check(!s.IsExplored(0, -12) && s.ExploredShare == 0, "map: a new game has seen nothing yet");
        var fresh = s.Explore(0, -12, Session.Sight);
        Check(fresh > 10 && s.IsExplored(0, -12) && s.IsExplored(100, -12) && !s.IsExplored(400, -12), $"map: standing somewhere shows the land in sight ({fresh} cells), not beyond");
        Check(s.Explore(0, -12, Session.Sight) == 0, "map: seeing the same land again shows nothing new");
        Check(s.IsExplored(-Regions.Half - 50, 0) == false && s.IsExplored(Regions.Half + 50, Regions.Half + 50) == false, "map: points off the mountain are not seen (and do not throw)");
        s.Explore(Regions.Half - 1, Regions.Half - 1, 300);
        Check(s.IsExplored(Regions.Half - 10, Regions.Half - 10), "map: the far corner can be seen");

        // The record survives the save; a damaged one only brings the fog back.
        var options = new JsonSerializerOptions { IncludeFields = true };
        var back = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(s.State, options), options);
        var again = new Session(back, 3);
        Check(again.IsExplored(0, -12) && again.IsExplored(Regions.Half - 10, Regions.Half - 10) && !again.IsExplored(2000, 2000), "map: what was seen survives the save");
        Check(s.State.Explored.Length < 12000, $"map: the record is small ({s.State.Explored.Length} characters)");
        back.Explored = "not base64!";
        Check(!new Session(back, 3).IsExplored(0, -12), "map: a damaged record brings the fog back, nothing worse");

        // The peak's blessing shows the whole mountain.
        s.SetFlag("map_revealed");
        Check(s.IsExplored(3000, -3000) && s.ExploredShare == 1, "map: the peak's blessing shows it all");

        // A save from before the fog: round the hero and the places in the codex are seen.
        var old = Session.NewGame("Мин");
        old.PlayedSeconds = 3600;
        old.X = 0;
        old.Z = -12;
        var town = Regions.Settlements.First(t => Math.Abs(t.At.X) + Math.Abs(t.At.Z) > 1500);
        old.Codex.Add("place:" + town.Id);
        old.Explored = null;
        var migrated = new Session(old, 3);
        Check(migrated.IsExplored(0, -12) && migrated.IsExplored(town.At.X, town.At.Z) && !migrated.IsExplored(-town.At.X * 0.5f + 3000, 7000), $"map: an old save has seen where it stands and {town.Name} (in its codex)");

        // Marks.
        var m = new Session(Session.NewGame("Мин"), 3);
        var first = m.AddMark(100, 200, "тайник");
        Check(first == 0 && m.MarkNear(110, 195, 30) == 0 && m.MarkNear(400, 400, 30) == -1, "map: a mark is put and found near where it is");

        for (var i = 0; i < Session.MostMarks + 5; i++)
        {
            m.AddMark(i * 10, 0);
        }

        Check(m.State.Marks.Count == Session.MostMarks && m.MarkNear(100, 200, 5) == -1, "map: too many marks — the oldest go");
        Check(m.RemoveMark(0) && m.State.Marks.Count == Session.MostMarks - 1 && !m.RemoveMark(99), "map: a mark is taken away");
        m.AddMark(99999, -99999);
        Check(Math.Abs(m.State.Marks.Last().X) <= Regions.Half && Math.Abs(m.State.Marks.Last().Z) <= Regions.Half, "map: a mark stays on the mountain");

        // The journal's errands (ROADMAP 3.3): where to go, days left, leaving one.
        var b = new Session(Session.NewGame("Мин"), 5);
        var boards = Session.Boards.Select(x => x.Settlement).ToList();
        var taken = 0;

        foreach (var board in boards)
        {
            foreach (var errand in b.BoardToday(board))
            {
                if (taken < 4 && b.TakeBounty(errand))
                {
                    taken++;
                }
            }
        }

        Check(taken > 0 && b.BountiesTaken().Count == taken, $"journal: errands taken show in the journal ({taken})");
        // A hunt whose beast has a den on the map (some come only with the wild lands' packs and the bandits' ambushes).
        var anyKill = b.BountiesTaken().FirstOrDefault(x => x.Kind == "kill" && Atlas.Spawns.Any(sp => sp.Enemy == x.Target));
        var anyGather = b.BountiesTaken().FirstOrDefault(x => x.Kind != "kill");

        anyKill = anyKill ?? new BountyState { Kind = "kill", Target = Atlas.Spawns.First(sp => sp.Needs == "" && sp.GoneAfter == "").Enemy, Count = 3, Taken = true };
        var (where, at) = b.BountyWhere(anyKill, new MapPoint(0, 0));
        Check(at != null && where.Contains("км"), $"journal: a hunt says where its beast lives ({anyKill.Target}: {where})");

        if (anyGather != null)
        {
            var (grows, _) = b.BountyWhere(anyGather, new MapPoint(0, 0));
            Check(grows.Length > 10, $"journal: a gathering says where it grows ({grows})");
        }

        var first2 = b.BountiesTaken()[0];
        Check(b.BountyDaysLeft(first2) == Session.BountyDays, "journal: a fresh errand has all its days");
        Check(b.AbandonBounty(first2) && !b.BountiesTaken().Contains(first2) && !b.AbandonBounty(first2), "journal: an errand left frees its place, once");
    }
}
