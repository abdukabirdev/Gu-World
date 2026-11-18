using System;
using System.Linq;
using GuWorld.Core;

/// <summary>Checks of the body (ROADMAP 12.4): traits grow from doing and give what they say; wounds come from heavy blows, weigh on their part and heal.</summary>
static partial class Program
{
    static void BodyRules()
    {
        var s = new Session(Session.NewGame("Тело"), 7);
        var melee = s.Stats.Melee;
        Check(s.BodyLevel == 0 && Body.Title(0) == "Хилый ученик", "a new hero's body is level 0");

        for (var i = 0; i < 20; i++)
        {
            s.MeleeHit(0, null);
        }

        Check(s.TraitLevel(Trait.Strength) == 1 && s.Stats.Melee > melee * 1.02f, "twenty palm strikes: strength 1, strikes 3% stronger");
        var roll = s.RollCost;
        s.State.Physique.Add(new TraitXp { Trait = Trait.Agility, Xp = Body.Needs[5] });
        s.Recompute();
        Check(s.RollCost < roll * 0.9f, "agility 5: a roll costs 15% less");
        s.Train(Trait.Endurance, Body.Needs[10]);
        Check(s.TraitLevel(Trait.Endurance) == Body.MaxLevel && Math.Abs(s.Stats.StaminaMax - (HeroStats.MaxStamina + 60)) < 0.01f, "endurance 10: +60 stamina");
        s.Train(Trait.Toughness, 100000);
        Check(s.TraitLevel(Trait.Toughness) == 10 && s.Stats.MaxHealth >= 160 - 0.01f, "toughness 10: +60 health (no more than level 10)");
        Check(Enumerable.Range(0, 41).All(n => Body.Title(n).Length > 0) && Body.Level(Body.Needs[3]) == 3 && Body.Level(Body.Needs[3] - 0.1f) == 2, "levels and titles follow the experience");

        // Wounds: heavy blows wound, light ones never.
        var w = new Session(Session.NewGame("Раны", Difficulty.Hard, null, 1), 11);

        for (var i = 0; i < 200; i++)
        {
            w.Restore();
            w.TakeDamage(w.Stats.MaxHealth * 0.05f);
        }

        Check(w.Wounds.Count == 0, "light blows never wound");

        for (var i = 0; i < 60 && w.Wounds.Count == 0; i++)
        {
            w.Restore();
            w.TakeDamage(w.Stats.MaxHealth * 0.35f);
        }

        Check(w.Wounds.Count > 0, "heavy blows wound");
        w.State.Wounds.Clear();
        w.Recompute();
        var before = w.Stats.MaxHealth;
        w.AddWound(BodyPart.LeftLeg, WoundKind.Fracture, 2);
        Check(w.LegFactor < 0.9f && w.SpeedFactor < 0.9f && w.SprintCost > HeroStats.SprintStamina, "a broken leg: slower, running harder");
        w.AddWound(BodyPart.RightArm, WoundKind.Cut, 1);
        var cutMelee = w.Stats.Melee;
        w.AddWound(BodyPart.Torso, WoundKind.Bruise, 2);
        Check(w.Stats.MaxHealth < before, "a wounded torso: less health");
        w.AddWound(BodyPart.RightArm, WoundKind.Bruise, 1);
        Check(w.WoundOn(BodyPart.RightArm).Severity == 2 && w.WoundOn(BodyPart.RightArm).Kind == WoundKind.Cut && w.Stats.Melee < cutMelee, "a wounded part wounded again is worse, its worse kind kept");

        // Healing: hours, sleep, a pill, the lantern.
        var leg = w.WoundOn(BodyPart.LeftLeg).Hours;
        w.HealWounds(10);
        Check(Math.Abs(w.WoundOn(BodyPart.LeftLeg).Hours - (leg - 10)) < 0.01f, "wounds heal with the hours");
        w.HealWounds(100);
        Check(w.WoundOn(BodyPart.LeftLeg) == null && w.WoundOn(BodyPart.Torso) == null, "and close when their hours are done");
        w.AddWound(BodyPart.Head, WoundKind.Cut, 1);
        w.AddItem("heal_pill", 1, true);
        Check(w.UseConsumable("heal_pill") && w.WoundOn(BodyPart.Head) == null, "a healing pill closes a cut, even at full health");
        w.AddWound(BodyPart.Torso, WoundKind.Bruise, 1);
        w.AddWound(BodyPart.LeftLeg, WoundKind.Fracture, 2);
        w.Die();
        Check(w.WoundOn(BodyPart.Torso) == null && w.WoundOn(BodyPart.LeftLeg) != null, "the lantern closes bruises and cuts, not broken bones");

        // The easy path wounds less and never badly; /god never.
        var easy = new Session(Session.NewGame("Легко", Difficulty.Easy, null, 1), 13);
        easy.AddWound(BodyPart.LeftArm, WoundKind.Bruise, 3);
        easy.AddWound(BodyPart.LeftArm, WoundKind.Bruise, 3);
        Check(easy.WoundOn(BodyPart.LeftArm).Severity == 1, "on the easy path a wound is never worse than light");
        var god = new Session(Session.NewGame("Бог"), 3);
        god.Godmode = true;

        for (var i = 0; i < 50; i++)
        {
            god.TakeDamage(god.Stats.MaxHealth * 0.5f);
        }

        Check(god.Wounds.Count == 0, "no wounds under /god");
    }
}
