using System.Collections.Generic;
using GuWorld.Actors;
using GuWorld.Core;
using UnityEngine;

namespace GuWorld.World
{
    /// <summary>
    /// Keeps the valley's beasts where the plan has them — a group appears when the hero is
    /// near and comes back some time after it is wiped out — and stages the story's fights:
    /// waves that come one after another once the hero stands where the story sends them.
    /// </summary>
    public class Spawns : MonoBehaviour
    {
        class Group
        {
            public SpawnDef Def;
            public bool Out;
            public float BackAt;
            /// <summary>For a clan's post or patrol: the game's hour (days × 24 + hour) its fallen are replaced.</summary>
            public double BackAtHour;
        }

        /// <summary>The game's time in hours since the first day.</summary>
        static double Hours(Session session) => session.State.Day * 24.0 + session.State.Hour;

        static bool IsPatrol(SpawnDef def) => def.Enemy.EndsWith("_patrol");

        /// <summary>How far the hero is from a group: from its road for a patrol that is not out, from its nearest living man when out.</summary>
        static float DistanceTo(SpawnDef def, MapPoint here, bool isOut)
        {
            if (def.Route == null)
            {
                return def.At.DistanceTo(here);
            }

            if (isOut)
            {
                var best = float.MaxValue;

                foreach (var enemy in Enemy.Living)
                {
                    if (enemy.Group == def.Id)
                    {
                        best = Mathf.Min(best, new MapPoint(enemy.transform.position.x, enemy.transform.position.z).DistanceTo(here));
                    }
                }

                if (best < float.MaxValue)
                {
                    return best;
                }
            }

            Patrols.Closest(def.Route, here, out var off);

            return off;
        }

        readonly List<Group> groups = new List<Group>();
        EncounterDef fight;
        int wave;
        float wait;
        float waveWait;

        void Awake()
        {
            foreach (var spawn in Atlas.Spawns)
            {
                groups.Add(new Group { Def = spawn });
            }
        }

        public void Refresh() => wait = 0;

        /// <summary>Begins a staged fight at once (a conversation asked for it: a duel).</summary>
        public void Begin(string id)
        {
            var encounter = Atlas.Encounter(id);

            if (encounter == null || fight != null)
            {
                return;
            }

            fight = encounter;
            wave = -1;
            waveWait = 0.8f;
        }

        /// <summary>Whether a group may be out now: its conditions, and night for the beasts of the night (in caves it is always night).</summary>
        static bool Allowed(Session session, SpawnDef def)
        {
            if (!session.Meets(def.Needs) || (def.GoneAfter != "" && session.Meets(def.GoneAfter)))
            {
                return false;
            }

            var enemy = Bestiary.Get(def.Enemy);

            if (enemy != null && enemy.Nocturnal && !session.Night)
            {
                var place = Atlas.PlaceAt(def.At);

                return place != null && place.Cave;
            }

            return true;
        }

        static int Alive(string group)
        {
            var count = 0;

            foreach (var enemy in Enemy.Living)
            {
                count += enemy.Group == group ? 1 : 0;
            }

            return count;
        }

        static void Remove(string group)
        {
            foreach (var enemy in new List<Enemy>(Enemy.Living))
            {
                if (enemy.Group == group)
                {
                    enemy.Remove();
                }
            }
        }

        static Vector3 Around(MapPoint centre, float min, float max, Vector3 awayFrom)
        {
            for (var attempt = 0; attempt < 16; attempt++)
            {
                var angle = Random.value * Mathf.PI * 2;
                var far = Random.Range(min, max);
                var at = new Vector3(centre.X + Mathf.Sin(angle) * far, 0, centre.Z + Mathf.Cos(angle) * far);

                if ((at - new Vector3(awayFrom.x, 0, awayFrom.z)).magnitude > 6)
                {
                    return at;
                }
            }

            return new Vector3(centre.X, 0, centre.Z);
        }

        void Update()
        {
            var hero = Game.Hero;
            var session = Game.Session;

            if (hero == null || session == null || Game.Frozen)
            {
                return;
            }

            wait -= Time.deltaTime;

            if (wait > 0)
            {
                return;
            }

            wait = 0.4f;
            var here = new MapPoint(hero.transform.position.x, hero.transform.position.z);

            foreach (var group in groups)
            {
                var def = group.Def;
                var alive = Alive(def.Id);
                var distance = DistanceTo(def, here, group.Out && alive > 0);

                if (group.Out && alive == 0)
                {
                    // Wiped out: it comes back after a while, when the hero is not looking (a clan's post
                    // after some hours of the game's time: Patrols.ReturnHours).
                    group.Out = false;
                    group.BackAt = def.Respawn > 0 ? Time.time + def.Respawn : float.MaxValue;
                    group.BackAtHour = IsPatrol(def) ? Hours(session) + Patrols.ReturnHours : 0;
                }

                if (!group.Out && alive == 0 && distance < 120 && Time.time >= group.BackAt && Hours(session) >= group.BackAtHour && Allowed(session, def)
                    && (distance > 30 || group.BackAt == 0 || def.Enemy == "dummy" || def.Route != null))
                {
                    var enemy = Bestiary.Get(def.Enemy);
                    // A patrol comes out on its road near the hero, but not on top of them.
                    var start = def.Route != null ? Patrols.ComeOutAt(def.Route, here, 45) : 0;
                    var first = def.Route != null ? Patrols.At(def.Route, start) : def.At;

                    if (!Ground.Ready(new Vector3(first.X, 0, first.Z)))
                    {
                        continue;
                    }

                    for (var i = 0; i < def.Count; i++)
                    {
                        // Now and then one of a pack is an elite.
                        var one = Elites.Make(enemy, Elites.Roll(enemy, Game.Session.Rank, Game.Session.Random));
                        var side = (i - (def.Count - 1) * 0.5f) * 1.6f;
                        var at = def.Route != null ? Patrols.At(def.Route, start - i * 1.5f) : default;
                        var made = def.Route != null
                            ? Enemy.Spawn(one, new Vector3(at.X + side, 0, at.Z), 0, def.Id)
                            : Enemy.Spawn(one, Around(def.At, 0, def.Radius, hero.transform.position), Random.Range(0, 360f), def.Id);

                        if (def.Route != null && made != null)
                        {
                            made.WalkRoute(def.Route, start, side);
                        }

                        if (one.Elite != "" && made != null)
                        {
                            Fx.Effects.Aura(made.transform, Models.Rgb(one.Colour), 9999, 1.4f);
                        }
                    }

                    group.Out = true;
                }
                else if (group.Out && (distance > 190 || (!Allowed(session, def) && distance > 40)))
                {
                    Remove(def.Id);
                    group.Out = false;
                }
            }

            Fights(hero, session, here);
        }

        void Fights(Hero hero, Session session, MapPoint here)
        {
            if (fight == null)
            {
                foreach (var encounter in Atlas.Encounters)
                {
                    if (session.WantsEncounter(encounter.Id) && session.Meets(encounter.Needs) && encounter.At.DistanceTo(here) < encounter.Radius && hero.Alive)
                    {
                        fight = encounter;
                        wave = -1;
                        waveWait = 0;
                        break;
                    }
                }

                return;
            }

            var group = "fight:" + fight.Id;

            if (Alive(group) > 0)
            {
                return;
            }

            waveWait -= 0.4f;

            if (waveWait > 0)
            {
                return;
            }

            wave++;

            if (wave >= fight.Waves.Length)
            {
                var won = fight;
                fight = null;
                Game.Ui.Banner("Победа", won.Name);
                session.EncounterWon(won.Id);
                Game.Save();
                return;
            }

            var next = fight.Waves[wave];
            Game.Ui.Banner(fight.Waves.Length > 1 ? $"{fight.Name} · {wave + 1} / {fight.Waves.Length}" : fight.Name, next.Shout);

            foreach (var (id, count) in next.Enemies)
            {
                var enemy = Bestiary.Get(id);

                for (var i = 0; i < count; i++)
                {
                    var at = enemy.Boss ? Around(fight.At, 0, 3, hero.transform.position) : Around(fight.At, 6, Mathf.Min(16, fight.Radius * 0.7f), hero.transform.position);
                    var to = hero.transform.position - at;
                    Enemy.Spawn(enemy, at, Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg, group);
                    Fx.Effects.Burst(Game.Ground(at) + Vector3.up, Models.Rgb(enemy.Colour), 2.5f, 0.4f);
                }
            }

            waveWait = 1.6f;
        }

        /// <summary>The hero fell: a staged fight is taken down, to begin again when they come back.</summary>
        public void HeroFell()
        {
            if (fight != null)
            {
                Remove("fight:" + fight.Id);
                fight = null;
            }
        }
    }
}
