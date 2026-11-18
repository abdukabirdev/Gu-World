using System;
using System.Collections.Generic;
using GuWorld.Core.Gen;

namespace GuWorld.Core
{
    /// <summary>A wild Gu's spot on the mountain: where, which Gu, whether it comes out only at night.</summary>
    public class WildSpot
    {
        public string Key;
        public float X, Z;
        public string Gu;
        public bool Night;
    }

    /// <summary>What an attempt to catch a wild Gu came to.</summary>
    public class CatchResult
    {
        public bool Caught;
        public string Message;
        public GuInstance Gu;
    }

    /// <summary>
    /// Wild Gu (ROADMAP 12.7; lore: Gu live wild, and a Gu Master catches them — main's lights over a spot,
    /// improved). The mountain is cut into cells; about every other cell has a spot, the same on every
    /// machine (the cell's hash). A spot's Gu is of a path that lives in its biome — moon Gu in the meadows,
    /// at night; earth Gu on the rocks; water Gu by the rivers; poison Gu in the marsh — and of a rank
    /// that grows with the distance from the Gu Yue village (only rank one round it). Catching costs
    /// essence; the chance falls with the Gu's rank above the hero's and rises with their attainment in
    /// its path; caught or fled, the spot is empty for some days. A caught Gu is wild: it must be refined.
    /// </summary>
    public static class WildGu
    {
        /// <summary>A cell's side, metres.</summary>
        public const float Cell = 160;

        static readonly Dictionary<Biome, GuPath[]> Habitats = new Dictionary<Biome, GuPath[]>
        {
            [Biome.Meadow] = new[] { GuPath.Moon, GuPath.Light, GuPath.Wind, GuPath.Wood },
            [Biome.Field] = new[] { GuPath.Wine, GuPath.Wood, GuPath.Light },
            [Biome.Settled] = new[] { GuPath.Wine },
            [Biome.PineForest] = new[] { GuPath.Strength, GuPath.Wood, GuPath.Sound },
            [Biome.Broadleaf] = new[] { GuPath.Wood, GuPath.Strength, GuPath.Moon },
            [Biome.Maple] = new[] { GuPath.Wood, GuPath.Fire, GuPath.Sound },
            [Biome.Bamboo] = new[] { GuPath.Sound, GuPath.Wind, GuPath.Wood },
            [Biome.Fir] = new[] { GuPath.Ice, GuPath.Strength },
            [Biome.Rock] = new[] { GuPath.Earth, GuPath.Lightning, GuPath.Fire, GuPath.Wind },
            [Biome.Deadwood] = new[] { GuPath.Bone, GuPath.Blood, GuPath.Dark, GuPath.Poison },
            [Biome.Marsh] = new[] { GuPath.Poison, GuPath.Water, GuPath.Blood },
            [Biome.Riverbank] = new[] { GuPath.Water, GuPath.Ice, GuPath.Moon },
        };

        /// <summary>The paths whose wild Gu live in a biome.</summary>
        public static GuPath[] PathsOf(Biome biome) => Habitats.TryGetValue(biome, out var paths) ? paths : new[] { GuPath.Moon };

        /// <summary>The highest rank of wild Gu at a distance from the clan's village: one round it, two farther, three in the wilds.</summary>
        public static int MaxRank(float distance) => distance < 900 ? 1 : distance < 2600 ? 2 : 3;

        /// <summary>The story's own Gu, those with no price and the clans' bred ones never live wild.</summary>
        static readonly HashSet<string> NeverWild = new HashSet<string> { "thunder_crown", "sky_canopy", "blood_moon", "spring_autumn_cicada" };

        public static bool Wild(GuDef gu) => gu.Price > 0 && gu.Clan == "" && !NeverWild.Contains(gu.Id);

        static bool NightPath(GuPath path) => path == GuPath.Moon || path == GuPath.Dark;

        static uint Hash(int x, int z, uint salt)
        {
            unchecked
            {
                var h = (uint)x * 73856093u ^ (uint)z * 19349663u ^ salt * 83492791u;
                h ^= h >> 13;
                h *= 0x5bd1e995;
                h ^= h >> 15;

                return h;
            }
        }

        static float Unit(int x, int z, uint salt) => (Hash(x, z, salt) & 0xFFFFFF) / (float)0x1000000;

        /// <summary>
        /// A cell's spot, or null if it has none: its place in the cell and its Gu, chosen by the biome
        /// there (`biomeAt`) and the distance from the village (at `villageX`, `villageZ`).
        /// </summary>
        public static WildSpot Spot(int cellX, int cellZ, Func<float, float, Biome> biomeAt, float villageX = 0, float villageZ = 0)
        {
            if (Unit(cellX, cellZ, 1) > 0.5f)
            {
                return null;
            }

            var x = (cellX + 0.15f + 0.7f * Unit(cellX, cellZ, 2)) * Cell;
            var z = (cellZ + 0.15f + 0.7f * Unit(cellX, cellZ, 3)) * Cell;
            var biome = biomeAt(x, z);
            var paths = PathsOf(biome);
            var distance = (float)Math.Sqrt((x - villageX) * (x - villageX) + (z - villageZ) * (z - villageZ));
            var rank = MaxRank(distance);
            var choices = new List<GuDef>();

            foreach (var gu in GuBook.All)
            {
                if (gu.Rank <= rank && gu.Rank >= Math.Max(1, rank - 1) && Array.IndexOf(paths, gu.Path) >= 0 && Wild(gu))
                {
                    choices.Add(gu);
                }
            }

            if (choices.Count == 0)
            {
                return null;
            }

            var pick = choices[(int)(Hash(cellX, cellZ, 4) % (uint)choices.Count)];

            return new WildSpot { Key = $"{cellX}:{cellZ}", X = x, Z = z, Gu = pick.Id, Night = NightPath(pick.Path) };
        }

        /// <summary>The cells within a reach of a point (for the spots to show round the hero).</summary>
        public static IEnumerable<(int x, int z)> CellsNear(float x, float z, float reach)
        {
            var x0 = (int)Math.Floor((x - reach) / Cell);
            var x1 = (int)Math.Floor((x + reach) / Cell);
            var z0 = (int)Math.Floor((z - reach) / Cell);
            var z1 = (int)Math.Floor((z + reach) / Cell);

            for (var cz = z0; cz <= z1; cz++)
            {
                for (var cx = x0; cx <= x1; cx++)
                {
                    yield return (cx, cz);
                }
            }
        }

        /// <summary>The essence an attempt costs: more for a Gu of a higher rank.</summary>
        public static float Cost(int rank) => 8 * rank * rank;
    }

    public partial class Session
    {
        /// <summary>Whether a spot's Gu is out now: not caught or fled lately, and a night Gu only by night.</summary>
        public bool WildOut(WildSpot spot)
        {
            if (spot == null || InSandbox)
            {
                return false;
            }

            var taken = State.WildTaken?.Find(t => t.Id == spot.Key);

            if (taken != null && State.Day < taken.Count)
            {
                return false;
            }

            return !spot.Night || State.Hour < 6 || State.Hour >= 19;
        }

        /// <summary>The chance to catch a spot's Gu now (0…1): its rank against the hero's, the hero's attainment in its path.</summary>
        public float CatchChance(WildSpot spot)
        {
            var def = GuBook.Get(spot.Gu);
            var chance = 0.7f - 0.22f * (def.Rank - Rank) + 0.04f * GradeOf(def.Path);

            return Math.Max(0.08f, Math.Min(0.92f, chance));
        }

        /// <summary>
        /// Tries to catch a spot's Gu: the essence is spent at once; caught — it goes to the hero wild (to be
        /// refined), of a quality by chance; or it slips away. Either way the spot is empty for two to four days.
        /// </summary>
        public CatchResult Catch(WildSpot spot)
        {
            var def = spot == null ? null : GuBook.Get(spot.Gu);

            if (def == null || !WildOut(spot))
            {
                return new CatchResult { Message = "Здесь никого нет" };
            }

            var cost = WildGu.Cost(def.Rank);

            if (State.Essence < cost)
            {
                return new CatchResult { Message = $"Нужно эссенции: {(int)cost}" };
            }

            State.Essence -= cost;
            var chance = CatchChance(spot);
            State.WildTaken = State.WildTaken ?? new List<Tally>();
            State.WildTaken.RemoveAll(t => t.Id == spot.Key);
            State.WildTaken.Add(new Tally { Id = spot.Key, Count = State.Day + 2 + Random.Next(3) });

            if (Random.NextDouble() >= chance)
            {
                Say($"{def.Name} ускользнул");

                return new CatchResult { Message = $"{def.Name} ускользнул" };
            }

            var roll = Random.NextDouble();
            var gu = MakeGu(def.Id, roll < 0.05 ? 2 : roll < 0.3 ? 1 : 0, false);
            AddGu(gu);
            GainAttainment(def.Path, 2);
            Changed?.Invoke();

            return new CatchResult { Caught = true, Gu = gu, Message = $"Пойман дикий {def.Name} — очистите его в апертуре (K)" };
        }
    }
}
