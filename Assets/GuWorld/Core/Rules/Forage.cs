using System;
using System.Collections.Generic;
using GuWorld.Core.Gen;

namespace GuWorld.Core
{
    /// <summary>
    /// Herbs over the whole mountain (ROADMAP 12.8; main's moon-orchid beds, improved): the land in 70 m
    /// cells, a little over half with a patch, the same on every machine; what grows there is the
    /// biome's (sun-bloom and bitter root in the meadows, mushrooms and wild garlic under broadleaves,
    /// bamboo shoots in the groves, reeds and blood vine in the marsh, spark moss on the rocks…), the
    /// rare ones (ginseng, cloud tea, spirit moss, frost lotus, blood vine) only a kilometre and more
    /// from the village. Gathered — one to three of it — a patch grows back in a day or two of the game.
    /// Patches are things of the herb kind ("forage:x:z:item"), so gathering is the herbs' own way.
    /// </summary>
    public static class Forage
    {
        public const float Cell = 70;
        public const string Prefix = "forage:";

        static readonly Dictionary<Biome, (string item, bool rare)[]> Grows = new Dictionary<Biome, (string, bool)[]>
        {
            [Biome.Meadow] = new[] { ("sun_bloom", false), ("bitter_root", false), ("dew_gourd", false), ("ginseng", true) },
            [Biome.Field] = new[] { ("dew_gourd", false), ("bear_garlic", false) },
            [Biome.PineForest] = new[] { ("red_cap", false), ("bitter_root", false), ("ginseng", true) },
            [Biome.Broadleaf] = new[] { ("red_cap", false), ("bear_garlic", false), ("ginseng", true) },
            [Biome.Maple] = new[] { ("red_cap", false), ("sun_bloom", false), ("spirit_moss", true) },
            [Biome.Bamboo] = new[] { ("bamboo_shoot", false), ("bitter_root", false), ("cloud_tea", true) },
            [Biome.Fir] = new[] { ("bitter_root", false), ("spirit_moss", true) },
            [Biome.Rock] = new[] { ("spark_moss", false), ("stone_dust", false), ("cloud_tea", true) },
            [Biome.Deadwood] = new[] { ("red_cap", false), ("bitter_root", false), ("blood_vine", true) },
            [Biome.Marsh] = new[] { ("marsh_reed", false), ("red_cap", false), ("blood_vine", true) },
            [Biome.Riverbank] = new[] { ("marsh_reed", false), ("dew_gourd", false), ("frost_lotus", true) },
        };

        /// <summary>The rare herbs (worth more, grow far from the village).</summary>
        public static bool Rare(string item)
        {
            foreach (var pair in Grows)
            {
                foreach (var (grown, rare) in pair.Value)
                {
                    if (grown == item)
                    {
                        return rare;
                    }
                }
            }

            return false;
        }

        /// <summary>How far round a clan's village its land reaches past the village itself, metres.</summary>
        public const float ClanLand = 400;

        /// <summary>
        /// The clan whose land a point is on (ROADMAP 12.8: «грядки клана — с разрешения»): within 400 m of a
        /// clan's village (not the hero's own Gu Yue, not the free hamlets); "" if no one's.
        /// </summary>
        public static string OwnerAt(float x, float z)
        {
            var p = new MapPoint(x, z);

            foreach (var settlement in Regions.Settlements)
            {
                var clan = Clans.Get(settlement.Clan);

                if (clan != null && clan.Id != Patrols.HomeClan && clan.Guards && settlement.At.DistanceTo(p) < settlement.Radius + ClanLand)
                {
                    return clan.Id;
                }
            }

            return "";
        }

        /// <summary>What grows in a biome (none in the settlements' streets).</summary>
        public static (string item, bool rare)[] GrowsIn(Biome biome) => Grows.TryGetValue(biome, out var list) ? list : new (string, bool)[0];

        static uint Hash(int x, int z, uint salt)
        {
            unchecked
            {
                var h = (uint)x * 2654435761u ^ (uint)z * 2246822519u ^ salt * 3266489917u;
                h ^= h >> 15;
                h *= 0x2c1b3c6d;
                h ^= h >> 12;

                return h;
            }
        }

        static float Unit(int x, int z, uint salt) => (Hash(x, z, salt) & 0xFFFFFF) / (float)0x1000000;

        static MapPoint Where(int cellX, int cellZ) =>
            new MapPoint((cellX + 0.1f + 0.8f * Unit(cellX, cellZ, 12)) * Cell, (cellZ + 0.1f + 0.8f * Unit(cellX, cellZ, 13)) * Cell);

        /// <summary>A cell's patch, or null: where in the cell, and what grows by its biome and its distance from the village.</summary>
        public static ThingDef Patch(int cellX, int cellZ, Func<float, float, Biome> biomeAt, float villageX = 0, float villageZ = 0)
        {
            if (Unit(cellX, cellZ, 11) > 0.55f)
            {
                return null;
            }

            var at = Where(cellX, cellZ);
            var grows = GrowsIn(biomeAt(at.X, at.Z));

            if (grows.Length == 0)
            {
                return null;
            }

            var far = Math.Sqrt((at.X - villageX) * (at.X - villageX) + (at.Z - villageZ) * (at.Z - villageZ)) > 1000;
            var pick = grows[(int)(Hash(cellX, cellZ, 14) % (uint)grows.Length)];

            // A rare herb only far from the village, and only one patch in four of those it would grow in.
            if (pick.rare && (!far || Unit(cellX, cellZ, 15) > 0.25f))
            {
                pick = grows[0];
            }

            return Make(cellX, cellZ, pick.item);
        }

        static ThingDef Make(int cellX, int cellZ, string item)
        {
            var def = ItemBook.Get(item);

            return def == null ? null : new ThingDef
            {
                Id = $"{Prefix}{cellX}:{cellZ}:{item}",
                Name = def.Name,
                Kind = ThingKind.Herb,
                Content = item,
                At = Where(cellX, cellZ),
                Regrow = 99999,
            };
        }

        /// <summary>The patch a key names ("forage:x:z:item"), or null.</summary>
        public static ThingDef FromId(string id)
        {
            if (id == null || !id.StartsWith(Prefix))
            {
                return null;
            }

            var parts = id.Substring(Prefix.Length).Split(':');

            return parts.Length == 3 && int.TryParse(parts[0], out var x) && int.TryParse(parts[1], out var z) ? Make(x, z, parts[2]) : null;
        }

        /// <summary>The cells within a reach of a point.</summary>
        public static IEnumerable<(int x, int z)> CellsNear(float x, float z, float reach)
        {
            for (var cz = (int)Math.Floor((z - reach) / Cell); cz <= (int)Math.Floor((z + reach) / Cell); cz++)
            {
                for (var cx = (int)Math.Floor((x - reach) / Cell); cx <= (int)Math.Floor((x + reach) / Cell); cx++)
                {
                    yield return (cx, cz);
                }
            }
        }
    }

    public partial class Session
    {
        /// <summary>Whether a patch has grown back (or was never gathered).</summary>
        public bool ForageOut(string id)
        {
            var taken = State.ForageTaken?.Find(t => t.Id == id);

            return taken == null || State.Day >= taken.Count;
        }

        /// <summary>Gathers a patch as the hero would (for the tests).</summary>
        public Use GatherForTest(ThingDef patch) => GatherPatch(patch);

        /// <summary>A patch gathered: one to three of it; it grows back in a day or two.</summary>
        Use GatherPatch(ThingDef patch)
        {
            if (!ForageOut(patch.Id))
            {
                return new Use { Message = "Здесь уже собрано" };
            }

            var count = 1 + Random.Next(3);

            // The rare ones give more by night, when they open (ROADMAP 12.8).
            if (Night && Forage.Rare(patch.Content))
            {
                count++;
            }

            AddItem(patch.Content, count);

            // Gathered on a clan's land by one it does not count a friend: theft, and the clan hears of it.
            var owner = Forage.OwnerAt(patch.At.X, patch.At.Z);

            if (owner != "" && StandingWith(owner) < Standing.Friendly)
            {
                ChangeReputation(owner, -2, false);
                Say($"Это земля клана {Clans.Get(owner)?.Name.Replace("Клан ", "")} — сбор без спроса им не понравится");
            }
            State.ForageTaken = State.ForageTaken ?? new List<Tally>();
            State.ForageTaken.RemoveAll(t => t.Id == patch.Id);
            State.ForageTaken.Add(new Tally { Id = patch.Id, Count = State.Day + 1 + Random.Next(2) });

            // The list stays small: patches long grown back are forgotten.
            State.ForageTaken.RemoveAll(t => t.Count < State.Day - 3);
            Notify(StepKind.Use, patch.Id);

            return new Use { Ok = true };
        }
    }
}
