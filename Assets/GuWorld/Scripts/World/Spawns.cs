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
        }

        readonly List<Group> groups = new List<Group>();
        EncounterDef fight;
        int wave;
        float wait;
        float waveWait;

        void Awake()
        {
            foreach (var spawn in Valley.Spawns)
            {
                groups.Add(new Group { Def = spawn });
            }
        }

        public void Refresh() => wait = 0;

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
                var distance = def.At.DistanceTo(here);
                var alive = Alive(def.Id);

                if (group.Out && alive == 0)
                {
                    // Wiped out: it comes back after a while, when the hero is not looking.
                    group.Out = false;
                    group.BackAt = def.Respawn > 0 ? Time.time + def.Respawn : float.MaxValue;
                }

                if (!group.Out && alive == 0 && distance < 120 && Time.time >= group.BackAt && session.Has(def.Needs) && (def.GoneAfter == "" || !session.Has(def.GoneAfter))
                    && (distance > 30 || group.BackAt == 0 || def.Enemy == "dummy"))
                {
                    var enemy = Bestiary.Get(def.Enemy);

                    if (!Ground.Ready(new Vector3(def.At.X, 0, def.At.Z)))
                    {
                        continue;
                    }

                    for (var i = 0; i < def.Count; i++)
                    {
                        Enemy.Spawn(enemy, Around(def.At, 0, def.Radius, hero.transform.position), Random.Range(0, 360f), def.Id);
                    }

                    group.Out = true;
                }
                else if (group.Out && distance > 190)
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
                foreach (var encounter in Valley.Encounters)
                {
                    if (session.WantsEncounter(encounter.Id) && encounter.At.DistanceTo(here) < encounter.Radius && hero.Alive)
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
