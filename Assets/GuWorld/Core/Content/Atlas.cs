using System;
using System.Collections.Generic;
using GuWorld.Core.Gen;

namespace GuWorld.Core
{
    /// <summary>
    /// Everything in the world in one index: the valley's hand-placed content and the wider mountain's.
    /// The regions' people and things that live in a settlement get their place from its plan
    /// (Resolve, once the world is laid out); until then — and in the small world, which has only the
    /// valley — the "present" lists hold only what stands in the valley.
    /// </summary>
    public static class Atlas
    {
        public static readonly NpcDef[] AllNpcs = Lists.Join(Valley.Npcs, RegionPeople.All);
        public static readonly ThingDef[] AllThings = Lists.Join(Valley.Things, RegionThings.All);
        public static readonly SpawnDef[] AllSpawns = Lists.Join(Valley.Spawns, RegionSpawns.All);
        public static readonly EncounterDef[] AllEncounters = Lists.Join(Valley.Encounters, RegionSpawns.Encounters);
        public static readonly PlaceDef[] Places = Lists.Join(Valley.Places, Regions.Places);

        static readonly HashSet<string> valleyNpcs = Ids(Valley.Npcs, n => n.Id);
        static readonly HashSet<string> valleyThings = Ids(Valley.Things, t => t.Id);
        static readonly HashSet<string> valleySpawns = Ids(Valley.Spawns, s => s.Id);
        static readonly HashSet<string> valleyEncounters = Ids(Valley.Encounters, e => e.Id);

        static Dictionary<string, NpcDef> npcs;
        static Dictionary<string, ThingDef> things;
        static Dictionary<string, EncounterDef> encounters;
        static Dictionary<string, PlaceDef> places;

        /// <summary>The spots of each settlement, by role, once laid out.</summary>
        static readonly Dictionary<string, Dictionary<string, List<Spot>>> spots = new Dictionary<string, Dictionary<string, List<Spot>>>();
        /// <summary>Which spot of its role each person takes (several share a role: the second takes the second spot).</summary>
        static readonly Dictionary<string, int> spotIndex = new Dictionary<string, int>();

        /// <summary>The whole mountain is in the game (the world has been laid out and the regions' content placed).</summary>
        public static bool Big { get; private set; }

        public static NpcDef[] Npcs => Big ? AllNpcs : Valley.Npcs;
        public static ThingDef[] Things => Big ? AllThings : Valley.Things;
        public static SpawnDef[] Spawns => Big ? AllSpawns : Valley.Spawns;
        public static EncounterDef[] Encounters => Big ? AllEncounters : Valley.Encounters;

        public static bool InValley(NpcDef npc) => valleyNpcs.Contains(npc.Id);
        public static bool InValley(ThingDef thing) => valleyThings.Contains(thing.Id);
        public static bool InValley(SpawnDef spawn) => valleySpawns.Contains(spawn.Id);
        public static bool InValley(EncounterDef encounter) => valleyEncounters.Contains(encounter.Id);

        static HashSet<string> Ids<T>(IEnumerable<T> items, Func<T, string> id)
        {
            var set = new HashSet<string>();

            foreach (var item in items)
            {
                set.Add(id(item));
            }

            return set;
        }

        public static NpcDef Npc(string id)
        {
            npcs = npcs ?? Lists.Index(AllNpcs, n => n.Id);

            return id != null && npcs.TryGetValue(id, out var found) ? found : null;
        }

        public static ThingDef Thing(string id)
        {
            things = things ?? Lists.Index(AllThings, t => t.Id);

            return id != null && things.TryGetValue(id, out var found) ? found : null;
        }

        public static EncounterDef Encounter(string id)
        {
            encounters = encounters ?? Lists.Index(AllEncounters, e => e.Id);

            return id != null && encounters.TryGetValue(id, out var found) ? found : null;
        }

        public static PlaceDef Place(string id)
        {
            places = places ?? Lists.Index(Places, p => p.Id);

            return id != null && places.TryGetValue(id, out var found) ? found : null;
        }

        /// <summary>Where an id is: "x,z", a settlement, a place, a person, a thing or an encounter.</summary>
        public static bool Locate(string id, out MapPoint at)
        {
            if (Regions.Locate(id, out at))
            {
                return true;
            }

            var npc = Npc(id);

            if (npc != null)
            {
                at = npc.At;
                return true;
            }

            var thing = Thing(id);

            if (thing != null)
            {
                at = thing.At;
                return true;
            }

            var encounter = Encounter(id);

            if (encounter != null)
            {
                at = encounter.At;
                return true;
            }

            return false;
        }

        /// <summary>The place a point is in (the smallest that holds it), or null in the wilds.</summary>
        public static PlaceDef PlaceAt(MapPoint at)
        {
            PlaceDef best = null;

            foreach (var place in Places)
            {
                if ((Big || Valley.Place(place.Id) != null) && place.At.DistanceTo(at) <= place.Radius && (best == null || place.Radius < best.Radius))
                {
                    best = place;
                }
            }

            return best;
        }

        /// <summary>The settlement a point is in, or null.</summary>
        public static SettlementDef SettlementAt(MapPoint at)
        {
            if (!Big)
            {
                return null;
            }

            foreach (var settlement in Regions.Settlements)
            {
                if (settlement.At.DistanceTo(at) <= settlement.Radius + 10)
                {
                    return settlement;
                }
            }

            return null;
        }

        /// <summary>The name of where a point is, for the interface: a settlement, a place, or "".</summary>
        public static string NameAt(MapPoint at)
        {
            var settlement = SettlementAt(at);

            if (settlement != null)
            {
                return settlement.Name;
            }

            return PlaceAt(at)?.Name ?? "";
        }

        // ------------------------------------------------------------------ Placing the regions' people and things

        /// <summary>Puts the people and things of the settlements at their spots in the laid-out world. Safe to call more than once.</summary>
        public static void Resolve(WorldPlan plan)
        {
            lock (spots)
            {
                spots.Clear();
                spotIndex.Clear();

                foreach (var settlement in plan.Settlements)
                {
                    var byRole = new Dictionary<string, List<Spot>>();

                    foreach (var spot in settlement.Spots)
                    {
                        if (!byRole.TryGetValue(spot.Role, out var list))
                        {
                            byRole[spot.Role] = list = new List<Spot>();
                        }

                        list.Add(spot);
                    }

                    spots[settlement.Def.Id] = byRole;
                }

                var taken = new Dictionary<string, int>();

                foreach (var npc in RegionPeople.All)
                {
                    if (npc.Home == "")
                    {
                        continue;
                    }

                    var key = npc.Home + "/" + npc.Role;
                    taken.TryGetValue(key, out var index);
                    taken[key] = index + 1;
                    spotIndex[npc.Id] = index;

                    if (SpotOf(npc.Home, npc.Role, index, out var spot))
                    {
                        npc.At = (MapPoint)spot.At;
                        npc.Facing = spot.Facing;
                    }
                }

                foreach (var thing in RegionThings.All)
                {
                    if (thing.Home == "")
                    {
                        continue;
                    }

                    var key = thing.Home + "/" + thing.Role + "/thing";
                    taken.TryGetValue(key, out var index);
                    taken[key] = index + 1;

                    if (SpotOf(thing.Home, thing.Role, index, out var spot))
                    {
                        // Beside the spot, not on it (the person of that spot stands there), and clear of the buildings.
                        var settlement = plan.Settlement(thing.Home);
                        var forward = Vec2.FromYaw(spot.Facing);
                        var side = 3.2f + 2.4f * index;
                        Vec2[] tries =
                        {
                            spot.At + forward.Right * side + forward * 0.8f, spot.At - forward.Right * side + forward * 0.8f,
                            spot.At + forward * 3.5f, spot.At + forward.Right * side + forward * 3, spot.At - forward.Right * side + forward * 3,
                            spot.At + forward * 6, spot.At + forward.Right * (side + 3), spot.At - forward.Right * (side + 3),
                        };
                        var at = tries[0];

                        foreach (var candidate in tries)
                        {
                            if (settlement.Clear(candidate, 1.2f) && !TooClose(candidate))
                            {
                                at = candidate;
                                break;
                            }
                        }

                        thing.At = (MapPoint)at;
                        thing.Facing = spot.Facing;
                    }
                }

                Big = true;
            }
        }

        /// <summary>True if a point is within two metres of someone of the regions (a thing should not stand on a person).</summary>
        static bool TooClose(Vec2 at)
        {
            foreach (var npc in RegionPeople.All)
            {
                if (npc.Home != "" && Vec2.Distance(npc.At, at) < 2)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>A spot of a role in a settlement (the index-th, wrapping), falling back to the plaza, then any spot.</summary>
        public static bool SpotOf(string settlement, string role, int index, out Spot spot)
        {
            spot = default;

            if (!spots.TryGetValue(settlement, out var byRole))
            {
                return false;
            }

            if (!byRole.TryGetValue(role, out var list) || list.Count == 0)
            {
                if (!byRole.TryGetValue("plaza", out list) || list.Count == 0)
                {
                    list = null;

                    foreach (var any in byRole.Values)
                    {
                        if (any.Count > 0)
                        {
                            list = any;
                            break;
                        }
                    }
                }
            }

            if (list == null)
            {
                return false;
            }

            spot = list[Math.Abs(index) % list.Count];

            return true;
        }

        /// <summary>
        /// Where someone is at an hour of the day: their schedule's spot (the last entry whose hour has
        /// come, wrapping round midnight), or where they always stand.
        /// </summary>
        public static MapPoint WhereAt(NpcDef npc, float hour, out float facing)
        {
            facing = npc.Facing;

            if (npc.Day.Length == 0 || npc.Home == "" || !Big)
            {
                return npc.At;
            }

            var role = npc.Day[npc.Day.Length - 1].role;

            foreach (var (from, there) in npc.Day)
            {
                if (hour >= from)
                {
                    role = there;
                }
            }

            spotIndex.TryGetValue(npc.Id, out var index);

            if (role == npc.Role || !SpotOf(npc.Home, role, index + Hash(npc.Id), out var spot))
            {
                return npc.At;
            }

            facing = spot.Facing;

            // Off their own spot, they stand a little aside, so two people do not stand in each other.
            var aside = Vec2.FromYaw(spot.Facing).Right * (((Hash(npc.Id) % 5) - 2) * 1.3f);

            return (MapPoint)(spot.At + aside);
        }

        static int Hash(string id)
        {
            var hash = 17;

            foreach (var c in id)
            {
                hash = hash * 31 + c;
            }

            return Math.Abs(hash);
        }
    }
}
