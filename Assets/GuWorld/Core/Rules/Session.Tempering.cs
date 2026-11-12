using System;
using System.Collections.Generic;

namespace GuWorld.Core
{
    /// <summary>
    /// Tempering a Gu up the qualities (ordinary → rare → superb → perfect) with jade, blood crystal and
    /// beast-king cores; reforging one of its affixes; and feeding it a twin of its own kind, which it
    /// takes in as mastery. Tempering can fail: then the materials are spent, but the Gu remembers, and
    /// the next try is surer.
    /// </summary>
    public partial class Session
    {
        /// <summary>What lifting a Gu one quality up takes.</summary>
        public struct TemperCost
        {
            public int Stones;
            public string Item;
            public int Count;
            /// <summary>The chance it holds, 0–1.</summary>
            public float Chance;
        }

        static readonly string[] TemperMaterials = { "jade_shard", "blood_crystal", "king_core" };
        static readonly float[] TemperChances = { 0.85f, 0.6f, 0.35f };

        public TemperCost TemperCostOf(GuInstance gu)
        {
            var def = GuBook.Get(gu.Def);
            var quality = Math.Min(gu.Quality, TemperMaterials.Length - 1);
            var rank = Math.Max(1, def.Rank);
            var count = quality == 0 ? rank * 2 : quality == 1 ? rank : Math.Max(1, rank - 1);
            // Each failure at this quality adds a tenth; the refiner's skill adds a twentieth a level.
            var chance = TemperChances[quality] + 0.1f * gu.Tempered + 0.05f * (State.Refining - 1);

            return new TemperCost
            {
                Stones = 40 * rank * (quality + 1) * (quality + 1),
                Item = TemperMaterials[quality],
                Count = count,
                Chance = Math.Min(0.95f, chance)
            };
        }

        /// <summary>Why a Gu cannot be tempered now, or null.</summary>
        public string WhyNoTemper(GuInstance gu)
        {
            if (gu == null)
            {
                return "Нет такого Гу";
            }

            if (!gu.Refined)
            {
                return "Сначала очистите Гу";
            }

            if (gu.Quality >= GuBook.MaxQuality)
            {
                return "Совершеннее не бывает";
            }

            if (InSandbox || gu.Borrowed)
            {
                return "В апертуре бессмертного не закаляют";
            }

            var cost = TemperCostOf(gu);

            if (Count(cost.Item) < cost.Count)
            {
                return $"Нужно: {ItemBook.Get(cost.Item)?.Name ?? cost.Item} × {cost.Count}";
            }

            return State.Stones < cost.Stones ? $"Нужно {cost.Stones} камней" : null;
        }

        /// <summary>The outcome of a tempering: held (a quality up and a new affix) or not.</summary>
        public struct TemperResult
        {
            public bool Tried;
            public bool Held;
            public Affix Gained;
        }

        public TemperResult Temper(int uid)
        {
            var gu = FindGu(uid);

            if (WhyNoTemper(gu) != null)
            {
                return default;
            }

            var cost = TemperCostOf(gu);
            State.Stones -= cost.Stones;
            RemoveItem(cost.Item, cost.Count);
            var result = new TemperResult { Tried = true };
            var def = GuBook.Get(gu.Def);

            if (Random.NextDouble() < cost.Chance)
            {
                gu.Quality++;
                gu.Tempered = 0;
                result.Held = true;

                // A new affix of a kind it does not have yet (tempered affixes roll higher); a Gu with all its kinds has its weakest raised.
                var pool = AffixPool(def);
                pool.RemoveAll(k => gu.Affixes.Exists(a => a.Kind == k));

                if (pool.Count > 0)
                {
                    result.Gained = RollAffix(pool[Random.Next(pool.Count)], 1.3f);
                    gu.Affixes.Add(result.Gained);
                }
                else if (gu.Affixes.Count > 0)
                {
                    var weakest = gu.Affixes[0];

                    foreach (var affix in gu.Affixes)
                    {
                        if (AffixShare(affix) < AffixShare(weakest))
                        {
                            weakest = affix;
                        }
                    }

                    var better = RollAffix(weakest.Kind, 1.6f);
                    weakest.Value = Math.Max(weakest.Value, better.Value);
                    result.Gained = weakest;
                }

                Say($"Закалка удалась: {def.Name} — {GuBook.QualityNames[gu.Quality]}");
            }
            else
            {
                gu.Tempered++;
                Say($"Закалка не удалась. {def.Name} помнит её: в следующий раз выйдет вернее.");
            }

            Recompute();
            Achieve();
            Touch();

            return result;
        }

        /// <summary>Where an affix's value sits in the range of its kind (0 the least, 1 the most).</summary>
        public static float AffixShare(Affix affix)
        {
            switch (affix.Kind)
            {
                case AffixKind.Sturdy: return (affix.Value - 10) / 15;
                case AffixKind.Far: return (affix.Value - 0.10f) / 0.15f;
                case AffixKind.Leech: return (affix.Value - 0.03f) / 0.03f;
                case AffixKind.Wellspring: return (affix.Value - 0.05f) / 0.10f;
                default: return (affix.Value - 0.08f) / 0.10f;
            }
        }

        // ------------------------------------------------------------------ Reforging

        public int ReforgeStones(GuInstance gu) => 25 * Math.Max(1, GuBook.Get(gu.Def).Rank) * (1 + gu.Quality);
        public int ReforgeJade(GuInstance gu) => Math.Max(1, GuBook.Get(gu.Def).Rank);

        public string WhyNoReforge(GuInstance gu, int affix)
        {
            if (gu == null || affix < 0 || affix >= gu.Affixes.Count)
            {
                return "Нечего перековывать";
            }

            if (!gu.Refined)
            {
                return "Сначала очистите Гу";
            }

            if (InSandbox || gu.Borrowed)
            {
                return "В апертуре бессмертного не перековывают";
            }

            if (Count("jade_shard") < ReforgeJade(gu))
            {
                return $"Нужно: {ItemBook.Get("jade_shard")?.Name} × {ReforgeJade(gu)}";
            }

            return State.Stones < ReforgeStones(gu) ? $"Нужно {ReforgeStones(gu)} камней" : null;
        }

        /// <summary>Rolls one affix again: another kind it does not have (or the same kind if none is left), a new value.</summary>
        public Affix Reforge(int uid, int affix)
        {
            var gu = FindGu(uid);

            if (WhyNoReforge(gu, affix) != null)
            {
                return null;
            }

            State.Stones -= ReforgeStones(gu);
            RemoveItem("jade_shard", ReforgeJade(gu));
            var old = gu.Affixes[affix];
            var pool = AffixPool(GuBook.Get(gu.Def));
            pool.RemoveAll(k => k != old.Kind && gu.Affixes.Exists(a => a.Kind == k));
            var kind = pool.Count > 1 ? pool[Random.Next(pool.Count)] : old.Kind;
            var made = RollAffix(kind, 1 + 0.1f * gu.Quality);
            gu.Affixes[affix] = made;
            Say($"Перековка: {GuBook.AffixText(made)}");
            Recompute();
            Touch();

            return made;
        }

        // ------------------------------------------------------------------ Feeding a twin

        /// <summary>The Gu of the same kind as this one that it could take in (not slotted, refined or wild).</summary>
        public List<GuInstance> TwinsOf(GuInstance gu)
        {
            var twins = new List<GuInstance>();

            if (gu == null)
            {
                return twins;
            }

            foreach (var other in State.Gu)
            {
                if (other != gu && other.Def == gu.Def && !IsSlotted(other.Uid) && !other.Borrowed && !InSandbox)
                {
                    twins.Add(other);
                }
            }

            twins.Sort((a, b) => a.Quality != b.Quality ? a.Quality.CompareTo(b.Quality) : a.MasteryXp.CompareTo(b.MasteryXp));

            return twins;
        }

        /// <summary>Mastery a Gu gains from taking in a twin.</summary>
        public static int TwinMastery(GuInstance twin) => twin.MasteryXp + 40 * Math.Max(1, GuBook.Get(twin.Def).Rank) * (1 + twin.Quality);

        /// <summary>Feeds the weakest twin to a Gu: the twin is gone, its mastery joins, and a twin of as good a quality makes the next tempering surer.</summary>
        public bool FeedTwin(int uid)
        {
            var gu = FindGu(uid);
            var twins = TwinsOf(gu);

            if (gu == null || !gu.Refined || twins.Count == 0)
            {
                return false;
            }

            var twin = twins[0];
            var xp = TwinMastery(twin);
            RemoveGu(twin);
            gu.MasteryXp += xp;

            if (twin.Quality >= gu.Quality)
            {
                gu.Tempered++;
            }

            Say($"{GuBook.Get(gu.Def).Name} поглотил двойника: мастерство +{xp}");
            Achieve();
            Touch();

            return true;
        }
    }
}
