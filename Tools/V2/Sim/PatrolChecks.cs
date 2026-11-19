using System;
using System.Collections.Generic;
using System.Linq;
using GuWorld.Core;

/// <summary>Checks of the clans' patrols, the raids foretold (ROADMAP 12.9) and the hit zones (12.4).</summary>
static partial class Program
{
    static void PatrolRules()
    {
        // What a patrol does comes from the clan's regard.
        Check(Patrols.StanceOf("bai", Standing.Hated) == PatrolStance.Fight, "a clan that hates the hero attacks");
        Check(Patrols.StanceOf("bai", Standing.Disliked) == PatrolStance.Warn && Patrols.StanceOf("bai", Standing.Neutral) == PatrolStance.Warn, "the disliked and the indifferent are told to leave");
        Check(Patrols.StanceOf("bai", Standing.Friendly) == PatrolStance.Pass && Patrols.StanceOf("xiong", Standing.Kin) == PatrolStance.Pass, "friends are let by");
        Check(Patrols.StanceOf("gu_yue", Standing.Neutral) == PatrolStance.Pass, "the hero's own clan lets them by while it is not against them");
        Check(Patrols.StanceOf("gu_yue", Standing.Disliked) == PatrolStance.Warn && Patrols.StanceOf("gu_yue", Standing.Hated) == PatrolStance.Fight, "…and turns on them when it is");
        Check(Patrols.WarnSeconds(PatrolStance.Warn) > Patrols.WarnSeconds(PatrolStance.Fight) && Patrols.WarnSeconds(PatrolStance.Pass) == 0, "a warning gives time to go, an attack only a shout");
        Check(Patrols.Line(PatrolStance.Warn, "Клан Бай").Contains("клана Бай") && !Patrols.Line(PatrolStance.Warn, "Клан Бай").Contains("Клан Бай"), "the warning names the clan once");

        var s = new Session(Session.NewGame("Путник"), 3);
        Check(s.PatrolStanceOf("gu_yue") == PatrolStance.Pass, "at the start Gu Yue lets its own by");
        Check(s.PatrolStanceOf("bai") == PatrolStance.Warn && s.PatrolStanceOf("xiong") == PatrolStance.Warn, "at the start Bai and Xiong warn");
        s.ChangeReputation("bai", -60, true);
        Check(s.PatrolStanceOf("bai") == PatrolStance.Fight, "Bai hated: its patrols attack");
        s.ChangeReputation("xiong", 40, true);
        Check(s.PatrolStanceOf("xiong") == PatrolStance.Pass, "Xiong friendly: its patrols let the hero by");
        var before = s.ReputationOf("xiong");
        s.StruckFriendlyPatrol("xiong");
        Check(s.ReputationOf("xiong") == before - Patrols.StruckFriendCost, "striking a friendly patrol costs regard");

        // The posts and patrols are always out (no more only when the clan hates the hero), and the patrols walk roads.
        var patrols = Atlas.AllSpawns.Where(d => d.Enemy.EndsWith("_patrol")).ToList();
        Check(patrols.Count >= 5 && patrols.All(d => d.Needs == ""), "the clans' posts and patrols are always out");
        Check(patrols.Count(d => d.Route != null && d.Route.Length >= 2) >= 3, "some of them walk a road");
        Check(patrols.Where(d => d.Route != null).All(d => Patrols.Length(d.Route) > 150), "a patrol's road is long enough to walk");
        Check(patrols.All(d => Bestiary.Get(d.Enemy) != null && Clans.Get(Bestiary.Get(d.Enemy).Clan) != null), "every patrol belongs to a clan");

        // A road walked.
        var road = new[] { new MapPoint(0, 0), new MapPoint(0, 100), new MapPoint(100, 100) };
        Check(Math.Abs(Patrols.Length(road) - 200) < 0.01f, "a road's length");
        var along = Patrols.Closest(road, new MapPoint(10, 50), out var off);
        Check(Math.Abs(along - 50) < 0.01f && Math.Abs(off - 10) < 0.01f, "the nearest point of a road and how far it is");
        var at = Patrols.At(road, 150);
        Check(Math.Abs(at.X - 50) < 0.01f && Math.Abs(at.Z - 100) < 0.01f, "the point so far along a road");
        Check(Patrols.At(road, 999).X == 100 && Patrols.At(road, -5).Z == 0, "past the ends: the ends");
        var come = Patrols.ComeOutAt(road, new MapPoint(2, 50), 40);
        Check(Patrols.At(road, come).DistanceTo(new MapPoint(2, 50)) >= 39.9f, "a patrol comes out on the road, not on top of the hero");
        var end = Patrols.ComeOutAt(road, new MapPoint(100, 100), 40);
        Check(Patrols.At(road, end).DistanceTo(new MapPoint(100, 100)) >= 39.9f, "…at the road's end it comes out behind");

        // Raids and tides are foretold hours before, once a day, and shown where they come.
        var r = new Session(Session.NewGame("Дозорный"), 4);
        var told = new List<string>();
        r.Forewarned += (title, text) => told.Add(title);
        r.State.Day = 13; // 13 % 9 == 4: a raid's day
        r.SetClock(7.5f);
        Check(told.Count == 1 && r.RaidSoon(7.5f) && !r.RaidUp, "the raid's smoke is seen in the morning before it");
        Check(r.Threats().Any(t => t.Name == Atlas.Encounter(Session.Raid).Name && !t.Now), "a raid foretold shows where it comes");
        r.SetClock(8.5f);
        Check(told.Count == 1, "it is foretold once");
        r.SetClock(11);
        Check(r.RaidUp && r.Threats().Any(t => t.Now), "the raid on: shown as on");
        r.SetClock(18);
        Check(!r.RaidUp && r.Threats().Count == 0, "after it nothing is shown");

        var t0 = new Session(Session.NewGame("Слушатель"), 5);
        var heard = 0;
        t0.Forewarned += (title, text) => heard++;
        t0.State.Day = 14; // 14 % 7 == 0
        t0.SetClock(18.5f);
        Check(heard == 0, "a hero below the second rank hears no tide (it does not come for them)");
        Commands.Run(t0, "/rank 2 1");
        t0.SetClock(19);
        Check(heard == 1 && t0.Threats().Any(t => t.Name == Atlas.Encounter(Session.Tide).Name), "from the second rank the evening's howl foretells the tide");

        // Hit zones.
        Check(HitZones.Applies(EnemyBody.Human) && !HitZones.Applies(EnemyBody.Canid) && !HitZones.Applies(EnemyBody.Bug), "only men have a head and legs to aim at");
        Check(HitZones.Of(0.95f) == HitZone.Head && HitZones.Of(0.6f) == HitZone.Body && HitZones.Of(0.2f) == HitZone.Legs, "the zone by the height of the blow");
        Check(HitZones.Damage(HitZone.Head) > 1 && HitZones.Damage(HitZone.Body) == 1 && HitZones.Damage(HitZone.Legs) < 1, "the head hurts more, the legs less");
        Check(HitZones.Critical(HitZone.Head) && !HitZones.Critical(HitZone.Legs), "a head hit counts as critical");
    }
}

static partial class Program
{
    /// <summary>The first picture level for a new machine (ROADMAP 2.7.3).</summary>
    static void MachineRules()
    {
        Check(Machine.Integrated("Intel(R) Iris(R) Xe Graphics") && Machine.Integrated("Intel(R) UHD Graphics 620") && Machine.Integrated("AMD Radeon(TM) Graphics"), "integrated cards are known");
        Check(!Machine.Integrated("NVIDIA GeForce RTX 3060 Laptop GPU") && !Machine.Integrated("AMD Radeon RX 6600"), "discrete cards are not taken for integrated");
        Check(Machine.Preset("Intel(R) Iris(R) Xe Graphics", 128, 16000, 8) == 2, "Iris Xe with 16 GB: medium (the autotest held 30+ frames there)");
        Check(Machine.Preset("Intel(R) UHD Graphics 620", 128, 8000, 4) == 1, "an older integrated card: low");
        Check(Machine.Preset("NVIDIA GeForce GTX 1050", 2048, 8000, 4) == 2 && Machine.Preset("NVIDIA GeForce RTX 3060", 6144, 16000, 6) == 3, "discrete cards by their memory");
        Check(Machine.Preset("NVIDIA GeForce RTX 4080", 16384, 32000, 16) == 4, "a strong machine: ultra");
        Check(Machine.Preset("NVIDIA GeForce RTX 4080", 16384, 4000, 16) == 0 && Machine.Preset("Anything", 4096, 16000, 2) == 0, "too little memory or too few cores: the minimum");
    }
}
