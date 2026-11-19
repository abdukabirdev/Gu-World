using System;
using System.Linq;
using System.Text.Json;
using GuWorld.Core;
using GuWorld.Core.Gen;
using GuWorld.Core.Gen.Flora;

/// <summary>The hero's body beyond the fight (ROADMAP 12.3, 12.6): postures, falls, climbing, ledges, limbs, the tops of great trees.</summary>
static partial class Program
{
    static void MoveRules()
    {
        // Crouching and crawling: slower, lower, quieter, seen later; only upright one sprints.
        var postures = new[] { Posture.Stand, Posture.Crouch, Posture.Crawl };

        for (var i = 1; i < postures.Length; i++)
        {
            var up = postures[i - 1];
            var down = postures[i];
            Check(Movement.Speed(down) < Movement.Speed(up) && Movement.Height(down) < Movement.Height(up) && Movement.Eyes(down) < Movement.Eyes(up)
                && Movement.Steps(down) < Movement.Steps(up) && Movement.Seen(down) < Movement.Seen(up), $"moves: {down} is slower, lower, quieter and seen later than {up}");
        }

        Check(Movement.Speed(Posture.Crawl) > 0 && Movement.Height(Posture.Crawl) >= 0.64f, "moves: a crawl still moves, and its body is no lower than its width (0.32 m round)");
        Check(Movement.Upright(Posture.Stand) && !Movement.Upright(Posture.Crouch) && !Movement.Upright(Posture.Crawl), "moves: only an upright hero sprints, guards and jumps");

        // Falls.
        Check(Movement.FallShare(Movement.SafeFall) == 0 && Movement.FallShare(3) == 0, "falls: a drop of a few metres does not hurt");
        var nine = Movement.FallShare(9);
        Check(nine > 0.05f && nine < 0.15f, $"falls: from 9 m about a tenth of the health ({nine:0.00})");
        Check(Movement.FallShare(30) >= 1, "falls: from 30 m the fall kills");
        var rising = true;

        for (var d = 5f; d < 40; d += 0.5f)
        {
            rising &= Movement.FallShare(d + 0.5f) >= Movement.FallShare(d);
        }

        Check(rising, "falls: the higher, the worse");
        Check(Movement.FallWound(6, Difficulty.Normal) == null, "falls: below 7 m the legs are not hurt");
        Check(Movement.FallWound(8, Difficulty.Normal) == (WoundKind.Bruise, 1) && Movement.FallWound(13, Difficulty.Normal) == (WoundKind.Bruise, 2)
            && Movement.FallWound(18, Difficulty.Normal) == (WoundKind.Fracture, 2) && Movement.FallWound(26, Difficulty.Normal) == (WoundKind.Fracture, 3), "falls: a bruise, a worse one, a break, a bad break");
        Check(Movement.FallWound(26, Difficulty.Easy) == (WoundKind.Bruise, 1), "falls: on the easy path, only a bruise");

        var s = new Session(Session.NewGame("Мин"), 5);
        var health = s.State.Health;
        Check(s.Fall(3) == 0 && s.State.Health == health && s.Wounds.Count == 0, "falls: a short drop leaves the hero as they were");
        var hurt = s.Fall(19);
        Check(hurt > 0 && s.State.Health > 0 && s.Wounds.Any(w => (w.Part == BodyPart.LeftLeg || w.Part == BodyPart.RightLeg) && w.Kind == WoundKind.Fracture),
            $"falls: from 19 m — {hurt:0} damage and a broken leg");
        var dead = new Session(Session.NewGame("Мин"), 5);
        dead.Fall(45);
        Check(dead.Dead, "falls: from 45 m the hero dies");

        // Climbing: stamina by the second, less holding still, more scrambling; spent — the hands let go.
        var c = new Session(Session.NewGame("Мин"), 5);
        c.Stamina = 100;
        Check(c.Climb(1, true, false) && Math.Abs(c.Stamina - (100 - Movement.ClimbCost)) < 0.01f, $"climbing: a second up the bark costs {Movement.ClimbCost} stamina");
        var before = c.Stamina;
        c.Climb(1, false, false);
        var hold = before - c.Stamina;
        before = c.Stamina;
        c.Climb(1, true, true);
        var scramble = before - c.Stamina;
        Check(hold < Movement.ClimbCost && scramble > Movement.ClimbCost, $"climbing: holding still costs less ({hold:0.0}), scrambling more ({scramble:0.0})");
        var seconds = 0f;

        while (c.Climb(0.1f, true, false) && seconds < 60)
        {
            seconds += 0.1f;
        }

        Check(seconds < 60 && c.Stamina == 0, $"climbing: the hands let go when the stamina is spent ({seconds:0.0} s more)");
        c.Tick(0.3f);
        Check(c.Stamina == 0, "climbing: no stamina comes back while still on the bark");
        c.Godmode = true;
        Check(c.Climb(5, true, true), "climbing: godmode never lets go");
        var tallest = Movement.ClimbSpeed * HeroStats.MaxStamina / Movement.ClimbCost;
        Check(tallest > 30 && tallest < 80, $"climbing: a full stamina climbs {tallest:0} m of bare trunk (rest on the limbs for more)");

        // Ledges and walls.
        Check(Movement.Over(0.3f, true, false) == LedgeMove.None && Movement.Over(1f, true, false) == LedgeMove.Vault && Movement.Over(1f, true, true) == LedgeMove.StepUp
            && Movement.Over(1.8f, true, true) == LedgeMove.PullUp && Movement.Over(2.8f, true, true) == LedgeMove.TooHigh, "ledges: stepped, vaulted, stepped onto, pulled up onto, too high — by the height");
        Check(Movement.Over(1.8f, false, false) == LedgeMove.Grab && Movement.Over(2.8f, false, false) == LedgeMove.None && Movement.Over(0.9f, false, false) == LedgeMove.None,
            "ledges: in the air, the hands catch what is within their reach");
        var jump = 7.2f * 7.2f / (2 * 22);
        Check(Movement.PullUpHigh < Movement.GrabHigh + jump && Movement.PullUpHigh + 1 < Movement.GrabHigh + Movement.WallRunRise,
            $"ledges: a jump catches what is too high standing (up to {Movement.GrabHigh + jump:0.0} m), a run up the wall still higher ({Movement.GrabHigh + Movement.WallRunRise:0.0} m)");
        Check(Movement.PullUp(0) == (0, 0) && Movement.PullUp(1) == (1, 1) && Movement.PullUp(0.4f).inward == 0, "ledges: a pull up rises first, then goes in over the edge");
        var steady = true;

        for (var t = 0f; t < 1; t += 0.01f)
        {
            steady &= Movement.PullUp(t + 0.01f).rise >= Movement.PullUp(t).rise - 1e-5f && Movement.PullUp(t + 0.01f).inward >= Movement.PullUp(t).inward - 1e-5f;
        }

        Check(steady, "ledges: a pull up never sinks back or steps back");
        Check(Movement.PullUpStart(Movement.HangDrop) < 0.01f && Movement.PullUpStart(0) > 0.99f, "ledges: from a hang the pull up starts at its beginning; from the top's level, at its end");
        var mid = Movement.PullUpStart(1.2f);
        Check(Math.Abs(Movement.PullUp(mid).rise * Movement.HangDrop - (Movement.HangDrop - 1.2f)) < 0.01f, $"ledges: from a 1.2 m ledge the pull up starts where the hands are on its top ({mid:0.00})");
        Check(Movement.Vault(0).rise == 0 && Movement.Vault(0.55f).rise == 1 && Movement.Vault(1) == (0, 1), "ledges: a vault goes up to the top and down beyond");

        // The limbs of a tree.
        Check(Movement.Bears(0.2f) && !Movement.Bears(0.09f), "limbs: a thick limb bears the hero, a thin one does not");
        Check(Math.Abs(Movement.CreakSeconds(Movement.ThinnestLimb) - 0.5f) < 1e-4f && Math.Abs(Movement.CreakSeconds(Movement.BearRadius) - 2) < 1e-4f
            && Movement.CreakSeconds(0.09f) > Movement.CreakSeconds(0.08f), "limbs: the thinner, the sooner it breaks (½–2 s)");
        Check(Movement.Sway(0.4f) == 0 && Movement.Sway(0.07f) == 1, "limbs: a thick limb is still, the thinnest sways the most");

        var tree = TreeGen.Grow(GuWorld.Core.Gen.Flora.Trees.Camphor, 77, new TreeParams { Height = 50, Lean = 5 });
        var trunk = tree.Climb.FirstOrDefault(l => l.Depth == 0);
        var limbs = tree.Climb.Where(l => l.Depth > 0).ToList();
        Check(trunk != null && limbs.Count >= 5, $"limbs: a great camphor has its trunk and limbs to climb ({limbs.Count} limbs)");
        Check(limbs.All(l => l.Radii[0] >= 0.14f && l.Radii.Skip(2).All(r => r >= Movement.ThinnestLimb)), "limbs: the solid keeps no limb thinner than a foot can stand on");

        if (trunk != null)
        {
            var at = trunk.Points[trunk.Points.Length / 2];
            Movement.NearestLimb(tree.Climb, at, out var r0, out var d0, out var gap0);
            Check(d0 == 0 && Math.Abs(gap0 + r0) < 1e-3f, $"limbs: inside the trunk, the trunk is nearest (radius {r0:0.00} m)");
            var off = at + new V3(100, 0, 0);
            Movement.NearestLimb(tree.Climb, off, out _, out _, out var far);
            Check(far > 50, "limbs: far from the tree, no limb is near");
        }

        var limb = limbs.FirstOrDefault(l => l.Points.Length >= 4);

        if (limb != null)
        {
            var i = limb.Points.Length / 2;
            var on = limb.Points[i] + V3.Up * (limb.Radii[i] + 0.05f);
            Movement.NearestLimb(tree.Climb, on, out var r1, out var d1, out var gap1);
            Check(gap1 <= 0.06f && gap1 > -0.2f && r1 > 0 && d1 >= 0, $"limbs: standing on a limb, it is underfoot (radius {r1:0.00} m, {gap1:0.00} m off its bark)");
        }

        // The beasts below a tree.
        Check(Bestiary.All.Any(e => !Movement.ReachesUp(e)) && Bestiary.All.Where(e => e.Mind == EnemyMind.Swarm || e.Boss).All(Movement.ReachesUp),
            "trees: beasts that do not fly, shoot or cast lose a hero up a tree; fliers and the great ones do not");

        // The top of a great tree: once a tree, the view and the nest.
        var t1 = new Session(Session.NewGame("Мин"), 5);
        var key = Movement.TreeKey(812.4f, -301.6f);
        Check(key == "812:-302", $"trees: a tree's key is its place to the metre ({key})");
        Check(t1.Treetop(key, 812, -302, 22) == null, "trees: an ordinary tree has no top worth the climb");
        var stones = t1.State.Stones;
        var shareBefore = t1.ExploredShare;
        var words = t1.Treetop(key, 812, -302, 60);
        Check(words != null && t1.State.Stones > stones && t1.ExploredShare > shareBefore && t1.IsExplored(812 + 500, -302), $"trees: a great tree's top shows the land far round and gives its nest ({words})");
        Check(t1.Treetop(key, 812, -302, 60) == null, "trees: a top gives its nest once");
        var options = new JsonSerializerOptions { IncludeFields = true };
        var back = new Session(JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(t1.State, options), options), 5);
        Check(back.Treetop(key, 812, -302, 60) == null, "trees: the tops reached survive the save");

        for (var n = 0; n < Session.MostTreetops + 20; n++)
        {
            t1.Treetop($"{n}:0", n, 0, 40);
        }

        Check(t1.State.Treetops.Count <= Session.MostTreetops, $"trees: the record of tops stays small ({t1.State.Treetops.Count})");
        var random = new Random(3);
        var items = Enumerable.Range(0, 400).SelectMany(_ => Movement.Nest(random, 90)).Select(f => f.item).Distinct().ToList();
        Check(items.All(id => id == "stones" || ItemBook.Get(id) != null) && items.Count >= 3, $"trees: a nest holds things that exist ({string.Join(", ", items)})");
    }
}

static partial class Program
{
    /// <summary>The finishing blow and the running kick (ROADMAP 13).</summary>
    static void BlowRules()
    {
        var s = new Session(Session.NewGame("Мин"), 5);
        var wolf = Bestiary.All.First(e => !e.Boss && e.Health >= 60 && e.Mind != EnemyMind.Still);
        var boss = Bestiary.All.First(e => e.Boss);
        var palm = HeroStats.ComboDamage[2] * s.Stats.Melee * (1 - wolf.Armour);
        var half = s.Finish(wolf, wolf.Health * 0.4f, wolf.Health);
        Check(half.Damage > wolf.Health * 0.4f && half.Critical, $"blows: a finishing blow kills a {wolf.Name} with less than half its health left");
        var full = s.Finish(wolf, wolf.Health, wolf.Health);
        Check(full.Damage < wolf.Health && full.Damage >= wolf.Health * 0.5f - 0.01f && full.Damage >= palm * 4 - 0.01f, $"blows: a fresh {wolf.Name} loses half its health to it ({full.Damage:0} of {wolf.Health:0})");
        var great = s.Finish(boss, boss.Health, boss.Health);
        Check(Math.Abs(great.Damage - HeroStats.ComboDamage[2] * s.Stats.Melee * (1 - boss.Armour) * 4) < 0.01f && great.Damage < boss.Health, $"blows: a great beast ({boss.Name}) takes four palms, no more");
        Check(Blows.OpenAfterParry(true) < Blows.OpenAfterParry(false), "blows: a great beast is left open for less time");

        s.Stamina = 100;
        Check(s.BeginKick() && Math.Abs(s.Stamina - (100 - Blows.KickStamina)) < 0.01f, $"blows: a running kick takes {Blows.KickStamina} stamina");
        var kick = s.KickHit(wolf);
        Check(kick.Damage > palm * 1.05f && kick.Damage < palm * 1.4f, $"blows: a kick is worth a little more than the third palm ({kick.Damage:0} against {palm:0})");
        s.Stamina = 5;
        Check(!s.BeginKick() && Math.Abs(s.Stamina - 5) < 0.01f, "blows: out of breath, no kick (and nothing spent)");
    }
}

static partial class Program
{
    /// <summary>The bag's «новое» marks (ROADMAP 3.2).</summary>
    static void FreshRules()
    {
        var s = new Session(Session.NewGame("Мин"), 5);
        s.SeenBag();
        s.AddItem("honey", 2, true);
        Check(s.IsFresh("honey") && !s.IsFresh("heal_pill"), "bag: a thing just come in is new, the others are not");
        s.AddItem("honey", 1, true);
        Check(s.State.Fresh.Count(i => i == "honey") == 1, "bag: a thing is marked new once");
        var options = new JsonSerializerOptions { IncludeFields = true };
        var back = new Session(JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(s.State, options), options), 5);
        Check(back.IsFresh("honey"), "bag: the new marks survive the save");
        s.SeenBag();
        Check(!s.IsFresh("honey"), "bag: looked at, nothing is new");
        foreach (var item in ItemBook.All.Take(Session.MostFresh + 10))
        {
            s.AddItem(item.Id, 1, true);
        }

        Check(s.State.Fresh.Count <= Session.MostFresh, "bag: the marks stay few");
    }
}
