using System;
using System.Collections.Generic;

namespace GuWorld.Core
{
    /// <summary>
    /// What the hero has seen of the mountain (the map's fog, ROADMAP 3.2) and the marks they put on the map.
    /// The world is cut in cells of 64 m; a cell the hero has stood within sight of is seen for good. The
    /// peak's blessing (map_revealed) shows all of it.
    /// </summary>
    public partial class Session
    {
        public const float ExploreCell = 64;
        public static readonly int ExploreCells = (int)Math.Ceiling(Regions.Half * 2 / ExploreCell);
        /// <summary>How far the hero sees the land round them, metres (farther from high ground).</summary>
        public const float Sight = 150;
        public const int MostMarks = 24;

        byte[] explored;

        byte[] ExploredBits
        {
            get
            {
                if (explored != null)
                {
                    return explored;
                }

                var bytes = (ExploreCells * ExploreCells + 7) / 8;
                explored = new byte[bytes];

                if (!string.IsNullOrEmpty(State.Explored))
                {
                    try
                    {
                        var read = Convert.FromBase64String(State.Explored);
                        Array.Copy(read, explored, Math.Min(read.Length, bytes));
                    }
                    catch (FormatException)
                    {
                        // A damaged record: the fog comes back, nothing else is lost.
                    }
                }

                return explored;
            }
        }

        public static int ExploreCellOf(float coordinate)
        {
            var cell = (int)Math.Floor((coordinate + Regions.Half) / ExploreCell);

            return cell < 0 ? 0 : cell >= ExploreCells ? ExploreCells - 1 : cell;
        }

        /// <summary>Whether the whole map is seen (the peak's blessing).</summary>
        public bool MapRevealed => Has("map_revealed");

        /// <summary>Whether a cell (by its column and row) is seen.</summary>
        public bool ExploredCell(int column, int row)
        {
            if (MapRevealed)
            {
                return true;
            }

            if (column < 0 || row < 0 || column >= ExploreCells || row >= ExploreCells)
            {
                return false;
            }

            var bit = row * ExploreCells + column;

            return (ExploredBits[bit >> 3] & (1 << (bit & 7))) != 0;
        }

        /// <summary>Whether the point is seen.</summary>
        public bool IsExplored(float x, float z) => ExploredCell(ExploreCellOf(x), ExploreCellOf(z));

        /// <summary>The cells within `radius` of a point are seen now; returns how many were not before.</summary>
        public int Explore(float x, float z, float radius)
        {
            var bits = ExploredBits;
            var from = ExploreCellOf(x - radius);
            var to = ExploreCellOf(x + radius);
            var bottom = ExploreCellOf(z - radius);
            var top = ExploreCellOf(z + radius);
            var fresh = 0;

            for (var row = bottom; row <= top; row++)
            {
                for (var column = from; column <= to; column++)
                {
                    // The cell's middle within the circle.
                    var cx = -Regions.Half + (column + 0.5f) * ExploreCell - x;
                    var cz = -Regions.Half + (row + 0.5f) * ExploreCell - z;

                    if (cx * cx + cz * cz > (radius + ExploreCell * 0.5f) * (radius + ExploreCell * 0.5f))
                    {
                        continue;
                    }

                    var bit = row * ExploreCells + column;
                    var mask = (byte)(1 << (bit & 7));

                    if ((bits[bit >> 3] & mask) == 0)
                    {
                        bits[bit >> 3] |= mask;
                        fresh++;
                    }
                }
            }

            if (fresh > 0)
            {
                State.Explored = Convert.ToBase64String(bits);
            }

            return fresh;
        }

        /// <summary>The share of the mountain seen, 0–1.</summary>
        public float ExploredShare
        {
            get
            {
                if (MapRevealed)
                {
                    return 1;
                }

                var seen = 0;
                var bits = ExploredBits;

                for (var i = 0; i < ExploreCells * ExploreCells; i++)
                {
                    seen += (bits[i >> 3] >> (i & 7)) & 1;
                }

                return seen / (float)(ExploreCells * ExploreCells);
            }
        }

        /// <summary>
        /// A save from before the fog: what the hero must have seen — round where they stand and every place
        /// and settlement their codex knows — is seen.
        /// </summary>
        void ExploreOldSave()
        {
            if (!string.IsNullOrEmpty(State.Explored) || State.PlayedSeconds < 1)
            {
                return;
            }

            Explore(State.X, State.Z, Sight * 2);

            foreach (var id in State.Codex)
            {
                if (!id.StartsWith("place:", StringComparison.Ordinal))
                {
                    continue;
                }

                var key = id.Substring(6);
                var place = Atlas.Place(key);
                var settlement = Regions.Settlement(key);

                if (settlement != null)
                {
                    Explore(settlement.At.X, settlement.At.Z, Math.Max(settlement.Radius, Sight) + Sight);
                }
                else if (place != null)
                {
                    Explore(place.At.X, place.At.Z, Math.Max(place.Radius, Sight) + Sight * 0.5f);
                }
            }
        }

        // ------------------------------------------------------------------ The player's marks

        /// <summary>The mark within `reach` of a point (the nearest), or -1.</summary>
        public int MarkNear(float x, float z, float reach)
        {
            var best = -1;
            var bestDistance = reach * reach;

            for (var i = 0; i < State.Marks.Count; i++)
            {
                var dx = State.Marks[i].X - x;
                var dz = State.Marks[i].Z - z;
                var d = dx * dx + dz * dz;

                if (d <= bestDistance)
                {
                    best = i;
                    bestDistance = d;
                }
            }

            return best;
        }

        /// <summary>A mark put on the map; the oldest goes when there are too many. Returns its index.</summary>
        public int AddMark(float x, float z, string name = "")
        {
            if (State.Marks.Count >= MostMarks)
            {
                State.Marks.RemoveAt(0);
            }

            State.Marks.Add(new MapMark { X = Clamp(x, -Regions.Half, Regions.Half), Z = Clamp(z, -Regions.Half, Regions.Half), Name = name ?? "" });

            return State.Marks.Count - 1;
        }

        public bool RemoveMark(int index)
        {
            if (index < 0 || index >= State.Marks.Count)
            {
                return false;
            }

            State.Marks.RemoveAt(index);

            return true;
        }

        static float Clamp(float v, float low, float high) => v < low ? low : v > high ? high : v;
    }
}
