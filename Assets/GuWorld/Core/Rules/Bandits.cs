using System;
using System.Collections.Generic;

namespace GuWorld.Core
{
    /// <summary>A band lying in wait somewhere in the wild: where, and who.</summary>
    public class BanditSpot
    {
        public string Key;
        public float X;
        public float Z;
        public string[] Members;
        /// <summary>How deep in the wild (0 nearest the valley … 3 the wild lands).</summary>
        public int Band;
    }

    /// <summary>
    /// Bandits who wait for loners away from the villages (ROADMAP 12.9): a few spots on a 300 m grid, the
    /// same every time, none in the Gu Yue valley, in a village or at a named place. The deeper into the
    /// wild, the stronger the band — thieves near the valley, the Black Tail's men past the second ridge,
    /// masters of crimson flame in the wild lands. A band beaten rests some days; with the Black Tail dead
    /// the nearer bands thin out; a hero the bandits do not hate is let be.
    /// </summary>
    public static class Bandits
    {
        public const float Cell = 300;
        public const string GroupPrefix = "bandits:";

        /// <summary>Where each band begins, metres from the middle of the mountain.</summary>
        public static readonly float[] Bands = { 1100, 2200, 3300, 4300 };

        static readonly string[][] Crews =
        {
            new[] { "bandit", "bandit", "bandit_thrower" },
            new[] { "bandit", "bandit", "bandit_thrower", "rogue_master" },
            new[] { "black_tail_thug", "black_tail_thug", "black_tail_archer", "black_tail_master" },
            new[] { "flame_master", "black_tail_thug", "black_tail_thug", "black_tail_archer" },
        };

        /// <summary>The band at a distance from the middle (-1 inside the valley).</summary>
        public static int BandAt(float distance)
        {
            for (var i = Bands.Length - 1; i >= 0; i--)
            {
                if (distance >= Bands[i])
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>The share of cells with a band, by how deep.</summary>
        public static float Chance(int band, bool blackTailDead) => band < 0 ? 0 : (band <= 1 && blackTailDead ? 0.04f : band == 0 ? 0.07f : 0.1f);

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

        /// <summary>Whether a point is clear of the villages and the named places (their own people are there).</summary>
        public static bool Clear(float x, float z)
        {
            var p = new MapPoint(x, z);

            foreach (var settlement in Regions.Settlements)
            {
                if (settlement.At.DistanceTo(p) < settlement.Radius + 250)
                {
                    return false;
                }
            }

            foreach (var place in Regions.Places)
            {
                if (place.At.DistanceTo(p) < place.Radius + 80)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>The band of a cell, if it has one (the same every time).</summary>
        public static BanditSpot Spot(int cx, int cz, bool blackTailDead)
        {
            var x = (cx + 0.2f + 0.6f * Unit(cx, cz, 11)) * Cell;
            var z = (cz + 0.2f + 0.6f * Unit(cx, cz, 12)) * Cell;
            var band = BandAt((float)Math.Sqrt(x * x + z * z));

            if (band < 0 || Unit(cx, cz, 13) >= Chance(band, blackTailDead) || !Clear(x, z))
            {
                return null;
            }

            return new BanditSpot { Key = $"bandits:{cx}:{cz}", X = x, Z = z, Members = Crews[band], Band = band };
        }

        /// <summary>The cells within `reach` metres of a point.</summary>
        public static IEnumerable<(int cx, int cz)> CellsNear(float x, float z, float reach)
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
    }

    public partial class Session
    {
        /// <summary>Whether a band lies in wait there now: the bandits hate the hero, it is not resting after a beating, not in the sandbox.</summary>
        public bool BanditsOut(BanditSpot spot)
        {
            if (spot == null || InSandbox || !Hostile("bandit"))
            {
                return false;
            }

            var rest = State.BanditsBeaten?.Find(t => t.Id == spot.Key);

            return rest == null || State.Day >= rest.Count;
        }

        /// <summary>A band beaten: it rests three to five days; the bandits take it ill, the hamlets are glad.</summary>
        public void BanditsBeaten(BanditSpot spot)
        {
            if (spot == null)
            {
                return;
            }

            State.BanditsBeaten = State.BanditsBeaten ?? new List<Tally>();
            State.BanditsBeaten.RemoveAll(t => t.Id == spot.Key);
            State.BanditsBeaten.Add(new Tally { Id = spot.Key, Count = State.Day + 3 + Random.Next(3) });
            ChangeReputation("mortal", 2, true);
            Say("Засада разбита. На этой дороге какое-то время будет тихо.");
            Touch();
        }

        /// <summary>The spot a band's id ("bandits:x:z") stands for, or null.</summary>
        public BanditSpot BanditSpotOf(string key)
        {
            if (key == null || !key.StartsWith(Bandits.GroupPrefix))
            {
                return null;
            }

            var parts = key.Split(':');

            return parts.Length == 3 && int.TryParse(parts[1], out var cx) && int.TryParse(parts[2], out var cz) ? Bandits.Spot(cx, cz, Has("black_tail_beaten")) : null;
        }
    }
}
