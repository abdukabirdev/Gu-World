using System.Collections.Generic;
using GuWorld.Core;
using UnityEngine;

namespace GuWorld.World
{
    /// <summary>
    /// The people and things of the wider mountain, brought into the world as the hero comes near
    /// and taken out as they go: a few dozen people and over a hundred things are spread over eight
    /// kilometres, and only those within sight need a body. (The valley's own stay, as before.)
    /// A herb picked and left keeps its regrowth here, so walking away does not grow it back.
    /// </summary>
    public class Population : MonoBehaviour
    {
        readonly Dictionary<string, Npc> people = new Dictionary<string, Npc>();
        readonly Dictionary<string, Thing> things = new Dictionary<string, Thing>();
        /// <summary>When a herb picked out of sight grows back (by Time.time).</summary>
        static readonly Dictionary<string, float> regrowth = new Dictionary<string, float>();
        Transform peopleRoot, thingsRoot;
        float near, far;
        float wait;

        public static Population Make(Transform parent)
        {
            var root = new GameObject("Population");
            root.transform.SetParent(parent, false);
            var population = root.AddComponent<Population>();
            population.peopleRoot = new GameObject("People").transform;
            population.peopleRoot.SetParent(root.transform, false);
            population.thingsRoot = new GameObject("Things").transform;
            population.thingsRoot.SetParent(root.transform, false);
            population.near = Quality.Level >= 2 ? 320 : Quality.Level == 1 ? 240 : 170;
            population.far = population.near + 80;

            return population;
        }

        /// <summary>A herb was picked: remember when it grows back, for when it is raised again.</summary>
        public static void Picked(string herb, float seconds) => regrowth[herb] = Time.time + seconds;

        public static float RegrowLeft(string herb) => regrowth.TryGetValue(herb, out var at) ? Mathf.Max(0, at - Time.time) : 0;

        void Update()
        {
            wait -= Time.unscaledDeltaTime;

            if (wait > 0 || Game.Session == null)
            {
                return;
            }

            wait = 0.6f;
            var focus = Game.Hero != null ? Game.Hero.transform.position : Vector3.zero;
            var here = new MapPoint(focus.x, focus.z);

            foreach (var npc in Atlas.Npcs)
            {
                if (Atlas.InValley(npc) || Game.Session.IsGone(npc))
                {
                    continue;
                }

                var distance = npc.At.DistanceTo(here);
                var up = people.TryGetValue(npc.Id, out var made);

                if (!up && distance < near && Ground.Ready(new Vector3(npc.At.X, 0, npc.At.Z)))
                {
                    people[npc.Id] = Npc.Make(npc, peopleRoot);
                }
                else if (up && (distance > far || made == null))
                {
                    if (made != null)
                    {
                        Destroy(made.gameObject);
                    }

                    people.Remove(npc.Id);
                }
            }

            foreach (var thing in Atlas.Things)
            {
                if (Atlas.InValley(thing))
                {
                    continue;
                }

                var distance = thing.At.DistanceTo(here);
                var up = things.TryGetValue(thing.Id, out var made);

                if (!up && distance < near && Ground.Ready(new Vector3(thing.At.X, 0, thing.At.Z)))
                {
                    things[thing.Id] = Thing.Make(thing, thingsRoot);
                }
                else if (up && (distance > far || made == null))
                {
                    if (made != null)
                    {
                        Destroy(made.gameObject);
                    }

                    things.Remove(thing.Id);
                }
            }
        }

        /// <summary>How many of the regions' people and things have bodies now (for the autotest's log).</summary>
        public string Count() => $"{people.Count} people, {things.Count} things raised";
    }
}
