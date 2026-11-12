using System;

namespace GuWorld.Core
{
    /// <summary>A kind of elite: what it adds to a beast or a master, and what it is called.</summary>
    public sealed class EliteDef
    {
        public string Id;
        /// <summary>The word before the name ("Свирепый волк").</summary>
        public string Prefix;
        public string PrefixFeminine;
        public float Health = 1;
        public float Damage = 1;
        public float Speed = 1;
        public float Armour;
        public StatusKind Inflicts;
        public float InflictPower;
        public float InflictTime;
        public int Colour;
        public string Description;
    }

    /// <summary>
    /// Elites: now and then a beast or a master of a pack comes out stronger than the rest — fiercer,
    /// stonier, quicker, colder, venomous, burning or blood-drinking — tinted and named so, worth twice
    /// the insight and stones and an extra roll of its loot. Never a boss, never in the immortal aperture.
    /// </summary>
    public static class Elites
    {
        public static readonly EliteDef[] All =
        {
            new EliteDef { Id = "fierce", Prefix = "Свирепый", PrefixFeminine = "Свирепая", Damage = 1.45f, Health = 1.4f, Colour = 0xFF8060, Description = "бьёт вдвое злее" },
            new EliteDef { Id = "stone", Prefix = "Каменный", PrefixFeminine = "Каменная", Health = 2.2f, Armour = 0.25f, Speed = 0.85f, Colour = 0xB0A890, Description = "камнем покрыт" },
            new EliteDef { Id = "swift", Prefix = "Стремительный", PrefixFeminine = "Стремительная", Speed = 1.35f, Damage = 1.15f, Health = 1.3f, Colour = 0xA0F0D0, Description = "быстр как ветер" },
            new EliteDef { Id = "frost", Prefix = "Ледяной", PrefixFeminine = "Ледяная", Health = 1.6f, Inflicts = StatusKind.Chill, InflictPower = 0.35f, InflictTime = 3, Colour = 0xC8F0FF, Description = "холодит каждым ударом" },
            new EliteDef { Id = "venom", Prefix = "Ядовитый", PrefixFeminine = "Ядовитая", Health = 1.5f, Inflicts = StatusKind.Poison, InflictPower = 0.12f, InflictTime = 6, Colour = 0xA0E060, Description = "отравляет" },
            new EliteDef { Id = "burning", Prefix = "Пылающий", PrefixFeminine = "Пылающая", Health = 1.5f, Damage = 1.15f, Inflicts = StatusKind.Burn, InflictPower = 0.12f, InflictTime = 4, Colour = 0xFF6030, Description = "горит и поджигает" },
            new EliteDef { Id = "blood", Prefix = "Кровавый", PrefixFeminine = "Кровавая", Health = 1.7f, Damage = 1.2f, Inflicts = StatusKind.Bleed, InflictPower = 0.1f, InflictTime = 5, Colour = 0xD03040, Description = "пускает кровь" },
        };

        /// <summary>The chance a beast of a pack is an elite (more as the hero climbs: elites matter to masters).</summary>
        public static float Chance(int heroRank) => 0.04f + 0.02f * heroRank;

        public static EliteDef Get(string id)
        {
            foreach (var elite in All)
            {
                if (elite.Id == id)
                {
                    return elite;
                }
            }

            return null;
        }

        /// <summary>Rolls whether an enemy comes out elite, and which (null if not, and never for a boss or a still thing).</summary>
        public static EliteDef Roll(EnemyDef def, int heroRank, Random random)
        {
            if (def == null || def.Boss || def.Mind == EnemyMind.Still || random.NextDouble() >= Chance(heroRank))
            {
                return null;
            }

            return All[random.Next(All.Length)];
        }

        /// <summary>
        /// An elite of an enemy: a copy of its def with the elite's numbers, name and tint, and its Elite id
        /// set (the rules pay it double). A status the beast inflicts already is kept; an elite's comes on top
        /// only when it had none (its power is a share of the beast's damage, so it scales with it).
        /// </summary>
        public static EnemyDef Make(EnemyDef def, EliteDef elite)
        {
            if (def == null || elite == null)
            {
                return def;
            }

            var made = def.Clone();
            made.Elite = elite.Id;
            made.Health = def.Health * elite.Health;
            made.Damage = def.Damage * elite.Damage;
            made.Speed = def.Speed * elite.Speed;
            made.Armour = Math.Min(0.6f, def.Armour + elite.Armour);
            made.Poise = def.Poise * 1.5f;
            made.Scale = def.Scale * 1.15f;
            made.Colour = elite.Colour;
            made.Name = Named(def.Name, elite);

            if (elite.Inflicts != StatusKind.None && def.Inflicts == StatusKind.None)
            {
                made.Inflicts = elite.Inflicts;
                made.InflictChance = 0.5f;
                made.InflictPower = elite.Inflicts == StatusKind.Chill ? elite.InflictPower : Math.Max(2, def.Damage * elite.InflictPower);
                made.InflictTime = elite.InflictTime;
            }

            return made;
        }

        /// <summary>The elite's name: its word before the beast's (feminine words for feminine names).</summary>
        public static string Named(string name, EliteDef elite)
        {
            if (elite.Id == "blood")
            {
                return name + " — кровопийца";
            }

            var first = name.Split(' ')[0];
            var feminine = first.EndsWith("а") || first.EndsWith("я") || first.EndsWith("ь") && !first.EndsWith("ль");

            return (feminine ? elite.PrefixFeminine : elite.Prefix) + " " + Lower(name);
        }

        static string Lower(string name)
        {
            // "Серый волк" → "серый волк"; keep the capital of a name that is a person's ("Мастер Чёрного Хвоста" keeps "Чёрного Хвоста").
            return name.Length > 0 ? char.ToLowerInvariant(name[0]) + name.Substring(1) : name;
        }
    }
}
