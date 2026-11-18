using System;
using System.Collections.Generic;
using System.Linq;
using GuWorld.Core;
using GuWorld.Core.Gen;

/// <summary>Checks of the wild Gu (ROADMAP 12.7): spots the same everywhere, Gu that fit their biome and distance, catching and the spot's rest.</summary>
static partial class Program
{
    static void WildRules()
    {
        Biome Meadow(float x, float z) => Biome.Meadow;
        Biome ByBand(float x, float z) => x < 0 ? Biome.Marsh : Biome.Rock;
        var spots = new List<WildSpot>();

        for (var cz = -30; cz <= 30; cz++)
        {
            for (var cx = -30; cx <= 30; cx++)
            {
                var spot = WildGu.Spot(cx, cz, ByBand);

                if (spot != null)
                {
                    spots.Add(spot);
                    var again = WildGu.Spot(cx, cz, ByBand);
                    Check(again.Gu == spot.Gu && again.X == spot.X && again.Key == spot.Key, "a cell's spot is the same every time");
                }
            }
        }

        var share = spots.Count / (61f * 61f);
        Check(share > 0.4f && share < 0.6f, $"about every other cell has a spot ({share:P0})");
        Check(spots.All(s => WildGu.PathsOf(ByBand(s.X, s.Z)).Contains(GuBook.Get(s.Gu).Path)), "every wild Gu lives in its biome");
        Check(spots.All(s => GuBook.Get(s.Gu).Rank <= WildGu.MaxRank((float)Math.Sqrt(s.X * s.X + s.Z * s.Z))), "no wild Gu above its distance's rank");
        Check(spots.Where(s => Math.Sqrt(s.X * s.X + s.Z * s.Z) < 900).All(s => GuBook.Get(s.Gu).Rank == 1), "round the village only rank one");
        Check(spots.Any(s => GuBook.Get(s.Gu).Rank == 3), "rank three in the wilds");
        Check(spots.All(s => WildGu.Wild(GuBook.Get(s.Gu))), "no story or clan Gu in the wild");

        // Catching.
        var s0 = new Session(Session.NewGame("Ловец"), 5);
        var near = Enumerable.Range(0, 200).Select(i => WildGu.Spot(i % 5 - 2, i / 5 - 2, Meadow)).First(s => s != null && !s.Night);
        Check(s0.WildOut(near), "a day Gu is out by day");
        var night = Enumerable.Range(0, 2000).Select(i => WildGu.Spot(i % 40 - 20, i / 40 - 25, Meadow)).FirstOrDefault(s => s != null && s.Night);

        if (night != null)
        {
            s0.SetClock(12);
            Check(!s0.WildOut(night), "a moon Gu hides by day");
        }

        s0.State.Essence = 0;
        Check(!s0.Catch(near).Caught && s0.WildOut(near), "no essence, no attempt (the spot is untouched)");
        s0.Restore();
        var gu = s0.State.Gu.Count;
        var result = s0.Catch(near);
        Check(!s0.WildOut(near), "caught or fled, the spot is empty");
        Check(!result.Caught || (s0.State.Gu.Count == gu + 1 && !result.Gu.Refined), "a caught Gu is wild: it has to be refined");
        s0.State.Day += 5;
        Check(s0.WildOut(near), "another settles there in some days");

        // Over many tries the chance holds: rank one at rank one about seven in ten.
        var caught = 0;

        for (var i = 0; i < 400; i++)
        {
            var t = new Session(Session.NewGame("Ловец"), 100 + i);

            if (t.Catch(near).Caught)
            {
                caught++;
            }
        }

        Check(caught > 230 && caught < 330, $"a rank one Gu is caught about seven times in ten at rank one ({caught} of 400)");
    }
}
