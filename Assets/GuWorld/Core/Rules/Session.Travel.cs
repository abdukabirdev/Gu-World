using System;
using System.Collections.Generic;

namespace GuWorld.Core
{
    /// <summary>
    /// The moon lanterns: lighting one remembers it (the hero wakes there after a fall), and between lit
    /// lanterns the hero can travel — for a few stones a kilometre, unless the player turned the cost off.
    /// </summary>
    public partial class Session
    {
        public bool Lit(string lantern) => State.Lanterns.Contains(lantern);

        /// <summary>Lights a lantern (the hero touched it).</summary>
        public void LightLantern(string id)
        {
            if (!State.Lanterns.Contains(id))
            {
                State.Lanterns.Add(id);
                var name = Atlas.Thing(id)?.Name ?? id;
                Say($"Фонарь зажжён: {name}");
                Achieve();
            }

            State.Lantern = id;
        }

        /// <summary>The lanterns the hero can travel to, nearest first from a point.</summary>
        public List<ThingDef> TravelTargets(MapPoint from)
        {
            var targets = new List<ThingDef>();

            foreach (var id in State.Lanterns)
            {
                var thing = Atlas.Thing(id);

                if (thing != null && thing.Kind == ThingKind.Lantern && Array.IndexOf(Atlas.Things, thing) >= 0)
                {
                    targets.Add(thing);
                }
            }

            targets.Sort((a, b) => a.At.DistanceTo(from).CompareTo(b.At.DistanceTo(from)));

            return targets;
        }

        /// <summary>What travelling to a lantern costs from a point: two stones and three for each kilometre (0 when travel is free).</summary>
        public int TravelCost(MapPoint from, ThingDef lantern)
        {
            if (!State.Settings.TravelCosts || lantern == null)
            {
                return 0;
            }

            return 2 + (int)Math.Round(from.DistanceTo(lantern.At) / 1000f * 3);
        }

        /// <summary>Why the hero cannot travel now, or null.</summary>
        public string WhyNoTravel(MapPoint from, ThingDef lantern, bool fighting)
        {
            if (lantern == null || !Lit(lantern.Id))
            {
                return "Этот фонарь не зажжён.";
            }

            if (fighting)
            {
                return "Нельзя уйти посреди боя.";
            }

            if (Held)
            {
                return "Вы не можете пошевелиться.";
            }

            if (lantern.At.DistanceTo(from) < 25)
            {
                return "Вы и так здесь.";
            }

            var cost = TravelCost(from, lantern);

            return State.Stones < cost ? $"Нужно {cost} камней." : null;
        }

        /// <summary>Travels to a lit lantern: pays, and it becomes the lantern the hero wakes at. The interface moves the hero.</summary>
        public bool Travel(MapPoint from, string lanternId, bool fighting = false)
        {
            var lantern = Atlas.Thing(lanternId);

            if (WhyNoTravel(from, lantern, fighting) != null)
            {
                return false;
            }

            State.Stones -= TravelCost(from, lantern);
            State.Lantern = lantern.Id;
            // The road takes time: an hour for every two kilometres.
            var hours = from.DistanceTo(lantern.At) / 2000f;
            var hour = State.Hour + hours;

            if (hour >= 24)
            {
                State.Hour = hour - 24;
                NewDay();
            }
            else
            {
                State.Hour = hour;
            }

            ClockSet?.Invoke(State.Hour);
            Touch();

            return true;
        }
    }
}
