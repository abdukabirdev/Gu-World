using System;
using System.Collections.Generic;

namespace GuWorld.Core
{
    /// <summary>
    /// The body (ROADMAP 12.4; main's status and body windows, improved). Four traits grow from doing:
    /// strength from palm strikes that land, agility from rolls, endurance from running hard, toughness
    /// from blows taken — each 0–10, each giving something. Heavy blows leave wounds on a part of the body
    /// (bruises, cuts, burns, fractures; 1–3 bad) that weigh on what that part does — arms on strikes,
    /// legs on running, the torso on health and breath, the head on the essence's flow — and heal with the
    /// hours of the game (faster asleep, at once in part with a healing pill). The path decides how often
    /// and how badly.
    /// </summary>
    public static class Body
    {
        public static readonly Trait[] Traits = { Trait.Strength, Trait.Agility, Trait.Endurance, Trait.Toughness };
        public static readonly BodyPart[] Parts = { BodyPart.Head, BodyPart.Torso, BodyPart.LeftArm, BodyPart.RightArm, BodyPart.LeftLeg, BodyPart.RightLeg };

        /// <summary>The experience each level takes, all told (level 1 at 20 … level 10 at 1250).</summary>
        public static readonly float[] Needs = { 0, 20, 50, 100, 170, 260, 380, 530, 720, 950, 1250 };
        public const int MaxLevel = 10;

        public static string Name(Trait trait)
        {
            switch (trait)
            {
                case Trait.Strength: return "Сила";
                case Trait.Agility: return "Ловкость";
                case Trait.Endurance: return "Выносливость";
                default: return "Крепость";
            }
        }

        /// <summary>What a trait gives at a level, in words.</summary>
        public static string Gives(Trait trait, int level)
        {
            switch (trait)
            {
                case Trait.Strength: return $"удар ладонью +{level * 3}%";
                case Trait.Agility: return $"перекат дешевле на {level * 3}%";
                case Trait.Endurance: return $"выносливость +{level * 6}";
                default: return $"здоровье +{level * 6}, урон по вам −{level}%";
            }
        }

        public static string TrainedBy(Trait trait)
        {
            switch (trait)
            {
                case Trait.Strength: return "удары ладонью, что попали";
                case Trait.Agility: return "перекаты";
                case Trait.Endurance: return "бег во весь дух";
                default: return "удары, что вы выдержали";
            }
        }

        public static int Level(float xp)
        {
            var level = 0;

            while (level < MaxLevel && xp >= Needs[level + 1])
            {
                level++;
            }

            return level;
        }

        /// <summary>The body's title by the sum of its levels (0–40).</summary>
        public static string Title(int sum) =>
            sum >= 32 ? "Тело из нефрита" : sum >= 22 ? "Тело из бронзы" : sum >= 13 ? "Крепкий мастер" : sum >= 5 ? "Закалённый юноша" : "Хилый ученик";

        public static string Name(BodyPart part)
        {
            switch (part)
            {
                case BodyPart.Head: return "голова";
                case BodyPart.Torso: return "тело";
                case BodyPart.LeftArm: return "левая рука";
                case BodyPart.RightArm: return "правая рука";
                case BodyPart.LeftLeg: return "левая нога";
                default: return "правая нога";
            }
        }

        public static string Name(WoundKind kind, int severity)
        {
            var bad = severity >= 3 ? "тяжёлый " : severity == 2 ? "сильный " : "";

            switch (kind)
            {
                case WoundKind.Cut: return bad + "порез";
                case WoundKind.Burn: return bad + "ожог";
                case WoundKind.Fracture: return severity >= 3 ? "открытый перелом" : "перелом";
                default: return bad + "ушиб";
            }
        }

        /// <summary>Hours a wound takes to heal, by its kind and how bad it is.</summary>
        public static float HealHours(WoundKind kind, int severity)
        {
            var per = kind == WoundKind.Fracture ? 36 : kind == WoundKind.Burn ? 14 : kind == WoundKind.Cut ? 10 : 6;

            return per * Math.Max(1, severity);
        }

        /// <summary>What a wound on a part weighs on, in words.</summary>
        public static string Weighs(BodyPart part)
        {
            switch (part)
            {
                case BodyPart.Head: return "эссенция течёт медленнее";
                case BodyPart.Torso: return "меньше здоровья и дыхания";
                case BodyPart.LeftArm:
                case BodyPart.RightArm: return "удар ладонью слабее";
                default: return "бег медленнее и тяжелее";
            }
        }

        /// <summary>How often blows wound on a path (0 never … more), and how bad a wound may get.</summary>
        public static float WoundChance(Difficulty path)
        {
            switch (path)
            {
                case Difficulty.Easy: return 0.4f;
                case Difficulty.Normal: return 0.75f;
                case Difficulty.Unbearable: return 1.2f;
                case Difficulty.Impossible: return 1.4f;
                default: return 1;
            }
        }

        public static int WorstWound(Difficulty path) => path == Difficulty.Easy ? 1 : 3;

        public static bool IsArm(BodyPart part) => part == BodyPart.LeftArm || part == BodyPart.RightArm;
        public static bool IsLeg(BodyPart part) => part == BodyPart.LeftLeg || part == BodyPart.RightLeg;
    }

    public partial class Session
    {
        /// <summary>A trait went up a level.</summary>
        public event Action<Trait, int> TraitRose;

        public float TraitXpOf(Trait trait)
        {
            State.Physique = State.Physique ?? new List<TraitXp>();
            var entry = State.Physique.Find(t => t.Trait == trait);

            return entry?.Xp ?? 0;
        }

        public int TraitLevel(Trait trait) => Body.Level(TraitXpOf(trait));

        /// <summary>The sum of the body's levels (0–40): its title's measure.</summary>
        public int BodyLevel
        {
            get
            {
                var sum = 0;

                foreach (var trait in Body.Traits)
                {
                    sum += TraitLevel(trait);
                }

                return sum;
            }
        }

        /// <summary>Trains a trait (not in the immortal aperture: nothing counts there).</summary>
        public void Train(Trait trait, float xp)
        {
            if (xp <= 0 || InSandbox)
            {
                return;
            }

            State.Physique = State.Physique ?? new List<TraitXp>();
            var entry = State.Physique.Find(t => t.Trait == trait);

            if (entry == null)
            {
                entry = new TraitXp { Trait = trait };
                State.Physique.Add(entry);
            }

            var before = Body.Level(entry.Xp);
            entry.Xp = Math.Min(Body.Needs[Body.MaxLevel], entry.Xp + xp);
            var after = Body.Level(entry.Xp);

            if (after > before)
            {
                Recompute();
                Say($"{Body.Name(trait)}: {after} — {Body.Gives(trait, after)}");
                TraitRose?.Invoke(trait, after);
                Changed?.Invoke();
            }
        }

        /// <summary>A roll: its stamina (less with agility), and agility trained. False if there is not enough stamina.</summary>
        public bool Roll()
        {
            if (!SpendStamina(RollCost))
            {
                return false;
            }

            Train(Trait.Agility, 1);

            return true;
        }

        public float RollCost => HeroStats.RollStamina * (1 - 0.03f * TraitLevel(Trait.Agility));

        /// <summary>The stamina a second of running hard costs: more with a leg wounded.</summary>
        public float SprintCost => HeroStats.SprintStamina * (1 + 0.15f * WoundsOn(Body.IsLeg));

        // ------------------------------------------------------------------ Wounds

        public IReadOnlyList<Wound> Wounds => State.Wounds ?? (State.Wounds = new List<Wound>());

        /// <summary>How bad the wounds are on the parts that answer (their severities summed).</summary>
        int WoundsOn(Func<BodyPart, bool> which)
        {
            var sum = 0;

            foreach (var wound in Wounds)
            {
                if (which(wound.Part))
                {
                    sum += wound.Severity;
                }
            }

            return sum;
        }

        public Wound WoundOn(BodyPart part) => State.Wounds?.Find(w => w.Part == part);

        /// <summary>The traits' gifts and the wounds' weight on the numbers (from Recompute).</summary>
        void BodyStats(HeroStats stats, ref float regen)
        {
            var strength = TraitLevel(Trait.Strength);
            var endurance = TraitLevel(Trait.Endurance);
            var toughness = TraitLevel(Trait.Toughness);
            stats.Melee *= 1 + 0.03f * strength;
            stats.StaminaMax = HeroStats.MaxStamina + 6 * endurance;
            stats.MaxHealth += 6 * toughness;
            stats.DamageTaken -= 0.01f * toughness;

            stats.Melee *= 1 - Math.Min(0.4f, 0.08f * WoundsOn(Body.IsArm));
            var torso = WoundsOn(p => p == BodyPart.Torso);
            stats.MaxHealth *= 1 - Math.Min(0.25f, 0.05f * torso);
            stats.StaminaMax *= 1 - Math.Min(0.3f, 0.06f * torso);
            regen *= 1 - Math.Min(0.4f, 0.1f * WoundsOn(p => p == BodyPart.Head));
        }

        /// <summary>The share of speed the legs' wounds leave (1 whole).</summary>
        public float LegFactor => 1 - Math.Min(0.35f, 0.06f * WoundsOn(Body.IsLeg));

        /// <summary>
        /// A blow landed (after the ward and the shield): a heavy one may leave a wound — where, by chance
        /// (the torso most, the head least); what, by the blow (fire burns; a very heavy one may break a
        /// limb); a part already wounded is wounded worse. None in the aperture, in a bout, under /god.
        /// </summary>
        void MaybeWound(float damage, StatusKind status)
        {
            if (InSandbox || Sparring || Godmode || Stats.MaxHealth <= 0)
            {
                return;
            }

            var share = damage / Stats.MaxHealth;

            if (share < 0.08f)
            {
                return;
            }

            var path = State.Settings.Difficulty;
            var chance = Math.Min(0.6f, share * 2.2f) * Body.WoundChance(path);

            if (Random.NextDouble() >= chance)
            {
                return;
            }

            var roll = Random.Next(100);
            var part = roll < 40 ? BodyPart.Torso : roll < 52 ? BodyPart.LeftArm : roll < 64 ? BodyPart.RightArm : roll < 77 ? BodyPart.LeftLeg : roll < 90 ? BodyPart.RightLeg : BodyPart.Head;
            var severity = Math.Min(Body.WorstWound(path), share >= 0.45f ? 3 : share >= 0.3f ? 2 : 1);
            var kind = status == StatusKind.Burn ? WoundKind.Burn
                : severity >= 2 && part != BodyPart.Torso && part != BodyPart.Head && Random.NextDouble() < 0.35 ? WoundKind.Fracture
                : Random.NextDouble() < 0.5 ? WoundKind.Cut : WoundKind.Bruise;
            AddWound(part, kind, severity);
        }

        /// <summary>A wound on a part: a part already wounded is wounded worse (its worse kind kept, its severity summed up to 3).</summary>
        public void AddWound(BodyPart part, WoundKind kind, int severity)
        {
            State.Wounds = State.Wounds ?? new List<Wound>();
            var wound = WoundOn(part);

            if (wound == null)
            {
                wound = new Wound { Part = part, Kind = kind, Severity = Math.Max(1, Math.Min(3, severity)) };
                State.Wounds.Add(wound);
            }
            else
            {
                wound.Kind = (WoundKind)Math.Max((int)wound.Kind, (int)kind);
                wound.Severity = Math.Min(Body.WorstWound(State.Settings.Difficulty) == 1 ? 1 : 3, wound.Severity + severity);
            }

            wound.Hours = Math.Max(wound.Hours, Body.HealHours(wound.Kind, wound.Severity));
            Recompute();
            Say($"Рана: {Body.Name(wound.Kind, wound.Severity)} — {Body.Name(wound.Part)}, {Body.Weighs(wound.Part)}");
            Changed?.Invoke();
        }

        /// <summary>Hours pass: the wounds heal (faster on the easy path).</summary>
        public void HealWounds(float hours)
        {
            if (hours <= 0 || State.Wounds == null || State.Wounds.Count == 0)
            {
                return;
            }

            var rate = State.Settings.Difficulty == Difficulty.Easy ? 2f : 1f;
            var healed = State.Wounds.RemoveAll(w => (w.Hours -= hours * rate) <= 0);

            // A bad wound eases as it heals: its severity follows the hours left.
            foreach (var wound in State.Wounds)
            {
                var per = Body.HealHours(wound.Kind, 1);
                wound.Severity = Math.Max(1, Math.Min(wound.Severity, (int)Math.Ceiling(wound.Hours / per)));
            }

            if (healed > 0)
            {
                Say(healed == 1 ? "Рана зажила" : $"Зажило ран: {healed}");
            }

            Recompute();
            Changed?.Invoke();
        }

        /// <summary>A healing pill's touch: the lightest wound loses a day of healing (a bruise or a cut usually closes).</summary>
        void TreatWound()
        {
            if (State.Wounds == null || State.Wounds.Count == 0)
            {
                return;
            }

            var lightest = State.Wounds[0];

            foreach (var wound in State.Wounds)
            {
                if (wound.Hours < lightest.Hours)
                {
                    lightest = wound;
                }
            }

            lightest.Hours -= 24;
            HealWounds(0.001f);
        }
    }
}
