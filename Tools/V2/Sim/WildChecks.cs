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

        // Herbs over the mountain.
        foreach (Biome biome in Enum.GetValues(typeof(Biome)))
        {
            foreach (var (item, _) in Forage.GrowsIn(biome))
            {
                Check(ItemBook.Get(item) != null, $"forage: {item} is a thing that exists");
            }
        }

        var patches = new List<ThingDef>();

        for (var cz = -60; cz <= 60; cz++)
        {
            for (var cx = -60; cx <= 60; cx++)
            {
                var patch = Forage.Patch(cx, cz, ByBand);

                if (patch != null)
                {
                    patches.Add(patch);
                }
            }
        }

        var rare = new HashSet<string> { "ginseng", "cloud_tea", "spirit_moss", "frost_lotus", "blood_vine" };
        Check(patches.Count > 121 * 121 * 0.45f && patches.Count < 121 * 121 * 0.65f, $"a little over half the cells have a herb patch ({patches.Count})");
        Check(patches.Where(p => Math.Sqrt(p.At.X * p.At.X + p.At.Z * p.At.Z) < 1000).All(p => !rare.Contains(p.Content)), "no rare herbs near the village");
        Check(patches.Any(p => rare.Contains(p.Content)), "rare herbs far from it");
        var one = patches[0];
        var back = Forage.FromId(one.Id);
        Check(back != null && back.Content == one.Content && back.At.X == one.At.X && back.Kind == ThingKind.Herb, "a patch is found again by its key");
        var g = new Session(Session.NewGame("Травник"), 9);
        var had = g.Count(one.Content);
        Check(g.UseThing(one.Id).Ok && g.Count(one.Content) > had && g.Count(one.Content) <= had + 3, "gathering a patch gives one to three");
        Check(!g.UseThing(one.Id).Ok && !g.ForageOut(one.Id), "a patch gathered is bare");
        g.State.Day += 3;
        Check(g.ForageOut(one.Id), "and grows back in a day or two");
    }
}
