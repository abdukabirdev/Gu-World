using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace GuWorld.Core.Gen
{
    /// <summary>
    /// The whole world as generated: the land, its rivers, its roads, its settlements laid out, what
    /// grows where. Made once (it takes a few seconds: the roads' search is the long part) and then
    /// only read, from any thread. Everything in it follows from the content's plan and fixed seeds.
    /// </summary>
    public sealed class WorldPlan
    {
        /// <summary>Changes whenever the generator's results would change (a save of generated data made by another version is not trusted).</summary>
        public const string Version = "gw2-world-1";

        public readonly Land Land;
        public readonly Waters Waters;
        public readonly Roads Roads;
        public readonly List<SettlementPlan> Settlements = new List<SettlementPlan>();
        public readonly Surface Surface;
        public readonly Scatter Scatter;
        /// <summary>How long each stage took, for the log.</summary>
        public readonly List<(string stage, double seconds)> Timings = new List<(string, double)>();

        static WorldPlan instance;
        static readonly object gate = new object();

        /// <summary>The world, made on first use (call it from a worker thread while the game shows its loading screen).</summary>
        public static WorldPlan Get()
        {
            lock (gate)
            {
                return instance ?? (instance = new WorldPlan());
            }
        }

        public static bool Ready
        {
            get
            {
                lock (gate)
                {
                    return instance != null;
                }
            }
        }

        WorldPlan()
        {
            var clock = Stopwatch.StartNew();

            void Stage(string name)
            {
                Timings.Add((name, clock.Elapsed.TotalSeconds));
                clock.Restart();
            }

            Land = new Land();
            Stage("land");
            Waters = new Waters(Land);
            Land.SetWaters(Waters);
            Stage("waters");
            Roads = new Roads(Land, Waters);
            Land.SetRoads(Roads);
            Stage("roads");

            foreach (var def in Regions.Settlements)
            {
                var flat = Land.FlatOf(def.Id);
                Settlements.Add(Planner.Plan(def, flat != null ? flat.Level : Land.Shaped(def.At.X, def.At.Z), RoadsInto(def), Waters));
            }

            Stage("settlements");
            Surface = new Surface(this);
            Scatter = new Scatter(this);
            Stage("surface");
            Atlas.Resolve(this);
            Stage("atlas");
        }

        /// <summary>The directions (yaw) the roads come into a settlement from, the main road's first.</summary>
        List<float> RoadsInto(SettlementDef def)
        {
            var result = new List<(float yaw, bool main)>();

            foreach (var road in Roads.All)
            {
                if (road.From != def.Id && road.To != def.Id)
                {
                    continue;
                }

                // Where the road crosses the settlement's edge.
                var points = road.Points;

                if (road.To == def.Id)
                {
                    points = new List<Vec2>(points);
                    points.Reverse();
                }

                var cross = points[points.Count - 1];

                foreach (var p in points)
                {
                    if (Vec2.Distance(p, def.At) > def.Radius)
                    {
                        cross = p;
                        break;
                    }
                }

                result.Add(((cross - (Vec2)def.At).Yaw, road.Main));
            }

            result.Sort((a, b) => b.main.CompareTo(a.main));

            return result.ConvertAll(r => r.yaw);
        }

        public SettlementPlan Settlement(string id) => Settlements.Find(s => s.Def.Id == id);

        /// <summary>A short report of what was made, for the log and the checks.</summary>
        public string Report()
        {
            var buildings = 0;

            foreach (var settlement in Settlements)
            {
                buildings += settlement.Buildings.Count;
            }

            var length = 0f;

            foreach (var road in Roads.All)
            {
                length += road.Length;
            }

            var timing = string.Join(", ", Timings.ConvertAll(t => $"{t.stage} {t.seconds:0.00}s"));

            return $"{Version}: {Waters.Rivers.Count} rivers, {Waters.Falls.Count} falls, {Roads.All.Count} roads ({length / 1000:0.0} km), {Roads.Bridges.Count} bridges, " +
                   $"{Settlements.Count} settlements ({buildings} buildings); {timing}";
        }
    }
}
