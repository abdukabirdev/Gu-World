using System;
using System.Collections.Generic;

namespace GuWorld.Core
{
    /// <summary>What casting a Gu's skill amounts to, with the hero's bonuses in.</summary>
    public class Cast
    {
        public GuInstance Gu;
        public GuDef Def;
        public SkillDef Skill;
        /// <summary>Damage (or health) of one hit, before the target's armour and the roll for a critical.</summary>
        public float Power;
        public float Range;
        public float Leech;
    }

    public struct Hit
    {
        public float Damage;
        public bool Critical;
    }

    /// <summary>
    /// One game in play: the saved state and every rule that changes it. The engine layer asks
    /// it what happens (a cast, a blow, a kill, a talk) and shows the answer; nothing here
    /// knows about the engine.
    /// </summary>
    public partial class Session
    {
        public readonly SaveData State;
        public readonly Random Random;
        public HeroStats Stats { get; private set; } = new HeroStats();

        /// <summary>A line for the player: "+3 лисьи шкуры", "Гу очищен".</summary>
        public event Action<string> Toast;
        /// <summary>Something the interface shows has changed (the bag, the slots, the quests).</summary>
        public event Action Changed;
        public event Action<QuestDef> QuestStarted;
        public event Action<QuestDef> QuestDone;
        /// <summary>The hero rose a step (the new step).</summary>
        public event Action<int> Advanced;
        public event Action<string> FlagSet;

        // What does not outlive the play session.
        public float Stamina = HeroStats.MaxStamina;
        float staminaWait;
        readonly Dictionary<int, float> cooldowns = new Dictionary<int, float>();
        float shieldTime, shieldValue;
        float meleeTime, meleeValue;
        float unstoppableTime;
        float healLeft, healTime;
        float wardWait;

        public Session(SaveData state, int seed = 0)
        {
            State = state ?? new SaveData();
            Random = seed == 0 ? new Random() : new Random(seed);
            Recompute();

            if (State.Quests.Count == 0 && !Has("finished"))
            {
                StartQuest(Story.First);
            }
        }

        public static SaveData NewGame(string heroName)
        {
            var state = new SaveData { HeroName = string.IsNullOrWhiteSpace(heroName) ? "Лань" : heroName.Trim() };
            state.Health = 100;
            state.Essence = Cultivation.EssenceScale(1) * state.Talent;

            return state;
        }

        // ------------------------------------------------------------------ The hero's numbers

        public int Rank => Cultivation.Rank(State.Step);
        public bool Shielded => shieldTime > 0;
        public bool Unstoppable => unstoppableTime > 0;
        public bool MeleeUp => meleeTime > 0;
        public float ShieldLeft => shieldTime;
        public float MeleeLeft => meleeTime;
        public bool WardReady => Stats.WardPeriod > 0 && wardWait <= 0;
        public int Chapter { get; private set; } = 1;

        public void Recompute()
        {
            var stats = new HeroStats();
            var step = State.Step;
            stats.MaxHealth = 100 + 20 * step;
            stats.MaxEssence = Cultivation.EssenceScale(Cultivation.Rank(step)) * State.Talent;
            var regen = 1f;
            stats.Power = 1 + 0.04f * step;
            stats.Melee = 1 + 0.06f * step;
            stats.ActiveSlots = Cultivation.ActiveSlots(step);
            stats.PassiveSlots = Cultivation.PassiveSlots(step);
            var ward = 0f;

            foreach (var gu in Slotted())
            {
                var def = GuBook.Get(gu.Def);

                if (def.Kind == GuKind.Passive)
                {
                    foreach (var bonus in def.Passive)
                    {
                        // A passive Gu of better quality gives a little more.
                        var value = bonus.Value * (1 + 0.1f * gu.Quality);

                        switch (bonus.Stat)
                        {
                            case StatKind.EssenceRegen: regen += value; break;
                            case StatKind.Cost: stats.Cost += value; break;
                            case StatKind.Cooldown: stats.Cooldown += value; break;
                            case StatKind.Power: stats.Power += value; break;
                            case StatKind.MoonPower: stats.MoonPower += value; break;
                            case StatKind.DamageTaken: stats.DamageTaken += value; break;
                            case StatKind.Health: stats.MaxHealth += value; break;
                            case StatKind.Range: stats.Range += value; break;
                            case StatKind.Leech: stats.Leech += value; break;
                            case StatKind.WardPeriod: ward = ward <= 0 ? bonus.Value : Math.Min(ward, bonus.Value); break;
                        }
                    }
                }

                foreach (var affix in gu.Affixes)
                {
                    if (affix.Kind == AffixKind.Sturdy)
                    {
                        stats.MaxHealth += affix.Value;
                    }
                    else if (affix.Kind == AffixKind.Wellspring)
                    {
                        regen += affix.Value;
                    }
                }
            }

            stats.WardPeriod = ward;
            stats.Cost = Math.Max(0.4f, stats.Cost);
            stats.Cooldown = Math.Max(0.4f, stats.Cooldown);
            stats.DamageTaken = Math.Max(0.3f, stats.DamageTaken);
            stats.EssenceRegen = stats.MaxEssence * 0.03f * regen;
            Stats = stats;
            State.Health = Math.Min(State.Health, stats.MaxHealth);
            State.Essence = Math.Min(State.Essence, stats.MaxEssence);

            var chapter = 1;

            foreach (var quest in State.Quests)
            {
                var def = Story.Get(quest.Quest);

                if (def != null && def.Main)
                {
                    chapter = Math.Max(chapter, def.Chapter);
                }
            }

            Chapter = Has("finished") ? 5 : chapter;
        }

        /// <summary>The Gu in the slots the hero has (a slot beyond the count does not work).</summary>
        public IEnumerable<GuInstance> Slotted()
        {
            var active = Cultivation.ActiveSlots(State.Step);
            var passive = Cultivation.PassiveSlots(State.Step);

            for (var i = 0; i < State.Active.Length && i < active; i++)
            {
                var gu = FindGu(State.Active[i]);

                if (gu != null)
                {
                    yield return gu;
                }
            }

            for (var i = 0; i < State.Passive.Length && i < passive; i++)
            {
                var gu = FindGu(State.Passive[i]);

                if (gu != null)
                {
                    yield return gu;
                }
            }
        }

        // ------------------------------------------------------------------ Time

        /// <summary>A frame's worth of time: essence and stamina come back, effects run out.</summary>
        public void Tick(float dt, bool sprinting = false)
        {
            State.PlayedSeconds += dt;
            State.Essence = Math.Min(Stats.MaxEssence, State.Essence + Stats.EssenceRegen * dt);

            if (sprinting)
            {
                Stamina = Math.Max(0, Stamina - HeroStats.SprintStamina * dt);
                staminaWait = 0.6f;
            }
            else if (staminaWait > 0)
            {
                staminaWait -= dt;
            }
            else
            {
                Stamina = Math.Min(HeroStats.MaxStamina, Stamina + HeroStats.StaminaRegen * dt);
            }

            shieldTime = Math.Max(0, shieldTime - dt);
            meleeTime = Math.Max(0, meleeTime - dt);
            unstoppableTime = Math.Max(0, unstoppableTime - dt);
            wardWait = Math.Max(0, wardWait - dt);

            if (healTime > 0)
            {
                var share = Math.Min(1, dt / healTime);
                Heal(healLeft * share);
                healLeft -= healLeft * share;
                healTime -= dt;
            }

            if (cooldowns.Count > 0)
            {
                var keys = new List<int>(cooldowns.Keys);

                foreach (var key in keys)
                {
                    var left = cooldowns[key] - dt;

                    if (left <= 0)
                    {
                        cooldowns.Remove(key);
                    }
                    else
                    {
                        cooldowns[key] = left;
                    }
                }
            }
        }

        public bool SpendStamina(float amount)
        {
            if (Stamina < amount)
            {
                return false;
            }

            Stamina -= amount;
            staminaWait = 0.8f;

            return true;
        }

        // ------------------------------------------------------------------ Casting

        public GuInstance ActiveGu(int slot) =>
            slot >= 0 && slot < Stats.ActiveSlots && slot < State.Active.Length ? FindGu(State.Active[slot]) : null;

        public float CooldownLeft(int slot)
        {
            var gu = ActiveGu(slot);

            return gu != null && cooldowns.TryGetValue(gu.Uid, out var left) ? left : 0;
        }

        public float CooldownOf(int slot)
        {
            var gu = ActiveGu(slot);

            return gu == null ? 0 : CooldownOf(gu);
        }

        float CooldownOf(GuInstance gu) => GuBook.Get(gu.Def).Skill.Cooldown * Stats.Cooldown * (1 - AffixOf(gu, AffixKind.Quick));

        public float CostOf(GuInstance gu) => GuBook.Get(gu.Def).Skill.Cost * Stats.Cost * (1 - AffixOf(gu, AffixKind.Thrifty));

        public static float AffixOf(GuInstance gu, AffixKind kind)
        {
            var sum = 0f;

            foreach (var affix in gu.Affixes)
            {
                if (affix.Kind == kind)
                {
                    sum += affix.Value;
                }
            }

            return sum;
        }

        /// <summary>The power of one hit of a Gu's skill with everything the hero has added in.</summary>
        public float PowerOf(GuInstance gu)
        {
            var def = GuBook.Get(gu.Def);

            if (def.Skill == null)
            {
                return 0;
            }

            var power = def.Skill.Power * Stats.Power;
            power *= 1 + 0.12f * (Cultivation.MasteryLevel(gu.MasteryXp) - 1);
            power *= 1 + 0.08f * gu.Quality;
            power *= 1 + AffixOf(gu, AffixKind.Sharp);

            if (def.Path == GuPath.Moon || def.Path == GuPath.Blood)
            {
                power *= Stats.MoonPower;
            }

            return power;
        }

        /// <summary>Why the Gu in a slot cannot be cast now, or null if it can.</summary>
        public string WhyNot(int slot)
        {
            var gu = ActiveGu(slot);

            if (gu == null)
            {
                return "Слот пуст";
            }

            if (cooldowns.ContainsKey(gu.Uid))
            {
                return "Гу ещё не готов";
            }

            return State.Essence < CostOf(gu) ? "Не хватает эссенции" : null;
        }

        /// <summary>Casts the Gu in a slot: takes the essence, starts the cooldown, applies what falls on the hero. Null if it cannot be cast.</summary>
        public Cast CastSlot(int slot)
        {
            if (WhyNot(slot) != null)
            {
                return null;
            }

            var gu = ActiveGu(slot);
            var def = GuBook.Get(gu.Def);
            var skill = def.Skill;
            State.Essence -= CostOf(gu);
            cooldowns[gu.Uid] = CooldownOf(gu);
            var cast = new Cast
            {
                Gu = gu, Def = def, Skill = skill, Power = PowerOf(gu),
                Range = skill.Range * Stats.Range * (1 + AffixOf(gu, AffixKind.Far)),
                Leech = skill.Leech + Stats.Leech + AffixOf(gu, AffixKind.Leech)
            };

            if (skill.Shape == SkillShape.Buff)
            {
                var time = skill.Duration * (1 + 0.1f * (Cultivation.MasteryLevel(gu.MasteryXp) - 1));

                switch (skill.Buff)
                {
                    case BuffKind.Shield:
                        shieldTime = time;
                        shieldValue = skill.BuffValue;
                        break;
                    case BuffKind.Unstoppable:
                        shieldTime = time;
                        shieldValue = skill.BuffValue;
                        unstoppableTime = time;
                        break;
                    case BuffKind.MeleeUp:
                        meleeTime = time;
                        meleeValue = skill.BuffValue;
                        break;
                }

                GainMastery(gu, 3);
            }
            else if (skill.Shape == SkillShape.Heal)
            {
                healLeft += cast.Power;
                healTime = Math.Max(0.1f, skill.Duration);
                GainMastery(gu, 3);
            }

            return cast;
        }

        /// <summary>A skill's hit on an enemy: the damage it does (and what it gives back to the hero).</summary>
        public Hit SkillHit(Cast cast, EnemyDef target)
        {
            var critical = Random.NextDouble() < 0.1;
            var damage = cast.Power * (critical ? 1.5f : 1) * (1 - (target?.Armour ?? 0)) * (0.92f + 0.16f * (float)Random.NextDouble());

            if (cast.Leech > 0)
            {
                Heal(damage * cast.Leech);
            }

            GainMastery(cast.Gu, 1);

            return new Hit { Damage = damage, Critical = critical };
        }

        /// <summary>A palm strike that lands (the combo's step, 0–2): its damage; the hero's essence rises.</summary>
        public Hit MeleeHit(int combo, EnemyDef target)
        {
            var critical = Random.NextDouble() < 0.1;
            var damage = HeroStats.ComboDamage[Math.Max(0, Math.Min(2, combo))] * Stats.Melee * (MeleeUp ? 1 + meleeValue : 1);
            damage *= (critical ? 1.5f : 1) * (1 - (target?.Armour ?? 0)) * (0.92f + 0.16f * (float)Random.NextDouble());
            State.Essence = Math.Min(Stats.MaxEssence, State.Essence + Stats.EssencePerHit);

            if (Stats.Leech > 0)
            {
                Heal(damage * Stats.Leech);
            }

            return new Hit { Damage = damage, Critical = critical };
        }

        void GainMastery(GuInstance gu, int xp)
        {
            var before = Cultivation.MasteryLevel(gu.MasteryXp);
            gu.MasteryXp += xp;
            var after = Cultivation.MasteryLevel(gu.MasteryXp);

            if (after > before)
            {
                Toast?.Invoke($"{GuBook.Get(gu.Def).Name}: мастерство {after}");
                Changed?.Invoke();
            }
        }

        // ------------------------------------------------------------------ Being hit

        /// <summary>A blow on the hero: what is left of it after the ward, the shield and the passives. 0 if the ward swallowed it.</summary>
        public float TakeDamage(float raw)
        {
            if (raw <= 0 || State.Health <= 0)
            {
                return 0;
            }

            if (WardReady)
            {
                wardWait = Stats.WardPeriod;
                Toast?.Invoke("Небесный покров поглотил удар");

                return 0;
            }

            var damage = raw * Stats.DamageTaken * (shieldTime > 0 ? 1 - shieldValue : 1);
            State.Health = Math.Max(0, State.Health - damage);

            return damage;
        }

        public void Heal(float amount)
        {
            if (State.Health > 0)
            {
                State.Health = Math.Min(Stats.MaxHealth, State.Health + amount);
            }
        }

        public bool Dead => State.Health <= 0;

        /// <summary>The hero fell: a tenth of the stones is lost, and they wake whole at their lantern. The stones lost.</summary>
        public int Die()
        {
            var lost = State.Stones / 10;
            State.Stones -= lost;
            State.Deaths++;
            Restore();
            Changed?.Invoke();

            return lost;
        }

        /// <summary>Full health and essence (a lantern's touch).</summary>
        public void Restore()
        {
            State.Health = Stats.MaxHealth;
            State.Essence = Stats.MaxEssence;
            Stamina = HeroStats.MaxStamina;
            healLeft = healTime = 0;
        }

        // ------------------------------------------------------------------ Cultivation

        public int InsightNeeded => State.Step >= Cultivation.MaxStep ? 0 : Cultivation.Needs[State.Step];
        public float InsightShare => State.Step >= Cultivation.MaxStep ? 1 : Math.Min(1, State.Insight / Cultivation.Needs[State.Step]);

        public void GainInsight(float amount)
        {
            if (amount <= 0)
            {
                return;
            }

            State.Insight += amount;
            AdvanceStages();
        }

        /// <summary>Stages within a rank come by themselves; a new rank waits for the spring.</summary>
        void AdvanceStages()
        {
            while (State.Step < Cultivation.MaxStep && !Cultivation.IsRankUp(State.Step) && State.Insight >= Cultivation.Needs[State.Step])
            {
                Advance();
            }
        }

        void Advance()
        {
            State.Insight -= Cultivation.Needs[State.Step];
            State.Step++;
            Recompute();
            Restore();
            Toast?.Invoke($"Прорыв: {Cultivation.Name(State.Step)}");
            Advanced?.Invoke(State.Step);
            RefreshQuests();
            Changed?.Invoke();
        }

        /// <summary>Burns stones at the spring for insight. The stones burnt.</summary>
        public int Meditate(int stones)
        {
            stones = Math.Min(stones, State.Stones);

            if (State.Step >= Cultivation.MaxStep)
            {
                stones = 0;
            }

            if (stones <= 0)
            {
                return 0;
            }

            State.Stones -= stones;
            GainInsight(stones * Cultivation.StoneInsight);
            Changed?.Invoke();

            return stones;
        }

        /// <summary>Why the hero cannot break through to the next rank now, or null if they can.</summary>
        public string WhyNoBreakthrough()
        {
            if (!Cultivation.IsRankUp(State.Step))
            {
                return "Сначала дойдите до пиковой ступени";
            }

            if (State.Insight < Cultivation.Needs[State.Step])
            {
                return $"Не хватает прорыва: {(int)State.Insight} / {Cultivation.Needs[State.Step]}";
            }

            var gate = Cultivation.Gate(State.Step);

            if (gate != null && !Has(gate))
            {
                return "Апертура не готова. Продвиньтесь по сюжету";
            }

            var cost = Cultivation.StoneCost[State.Step];

            return State.Stones < cost ? $"Нужно камней: {cost}" : null;
        }

        public bool Breakthrough()
        {
            if (WhyNoBreakthrough() != null)
            {
                return false;
            }

            State.Stones -= Cultivation.StoneCost[State.Step];
            Advance();
            AdvanceStages();

            return true;
        }

        // ------------------------------------------------------------------ Flags

        public bool Has(string flag) => string.IsNullOrEmpty(flag) || State.Flags.Contains(flag);

        public void SetFlag(string flag)
        {
            if (string.IsNullOrEmpty(flag) || State.Flags.Contains(flag))
            {
                return;
            }

            State.Flags.Add(flag);
            FlagSet?.Invoke(flag);
        }

        void Say(string line) => Toast?.Invoke(line);
        void Touch() => Changed?.Invoke();
    }
}
