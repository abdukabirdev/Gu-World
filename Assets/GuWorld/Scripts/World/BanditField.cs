using System.Collections.Generic;
using GuWorld.Actors;
using GuWorld.Core;
using UnityEngine;

namespace GuWorld.World
{
    /// <summary>
    /// The bandits' ambushes round the hero (ROADMAP 12.9; the rules in Core/Rules/Bandits.cs): a band that
    /// lies in wait within reach comes out of sight (never on top of the hero), stays while the hero is near,
    /// and goes when they are far; a band killed to the last man is beaten and rests some days.
    /// </summary>
    public class BanditField : MonoBehaviour
    {
        const float Reach = 200;
        const float Leave = 280;
        /// <summary>Closer than this a band does not appear (it is there already, or it is not).</summary>
        const float NotInSight = 70;

        readonly Dictionary<string, BanditSpot> out_ = new Dictionary<string, BanditSpot>();
        readonly Dictionary<string, int> manned = new Dictionary<string, int>();
        float wait;

        public static void Make(Transform parent)
        {
            var root = new GameObject("Bandits");
            root.transform.SetParent(parent, false);
            root.AddComponent<BanditField>();
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

        void Update()
        {
            var hero = Game.Hero;
            var session = Game.Session;

            if (hero == null || session == null || !Ground.Big || Game.Frozen)
            {
                return;
            }

            wait -= Time.deltaTime;

            if (wait > 0)
            {
                return;
            }

            wait = 0.5f;
            var at = hero.transform.position;
            var here = new MapPoint(at.x, at.z);

            // Those out: beaten to the last man, or left far behind.
            foreach (var key in new List<string>(out_.Keys))
            {
                var spot = out_[key];
                var alive = Alive(key);
                var far = new MapPoint(spot.X, spot.Z).DistanceTo(here);

                if (alive == 0)
                {
                    out_.Remove(key);

                    if (manned.TryGetValue(key, out var men) && men > 0)
                    {
                        session.BanditsBeaten(spot);
                    }

                    manned.Remove(key);
                }
                else if (far > Leave)
                {
                    foreach (var enemy in new List<Enemy>(Enemy.Living))
                    {
                        if (enemy.Group == key)
                        {
                            enemy.Remove();
                        }
                    }

                    out_.Remove(key);
                    manned.Remove(key);
                }
            }

            // New ones within reach.
            var blackTailDead = session.Has("black_tail_beaten");

            foreach (var (cx, cz) in Bandits.CellsNear(at.x, at.z, Reach))
            {
                var spot = Bandits.Spot(cx, cz, blackTailDead);

                if (spot == null || out_.ContainsKey(spot.Key) || !session.BanditsOut(spot))
                {
                    continue;
                }

                var far = new MapPoint(spot.X, spot.Z).DistanceTo(here);

                if (far > Reach || far < NotInSight || !Ground.Ready(new Vector3(spot.X, 0, spot.Z)))
                {
                    continue;
                }

                var count = 0;

                for (var i = 0; i < spot.Members.Length; i++)
                {
                    var def = Bestiary.Get(spot.Members[i]);

                    if (def == null)
                    {
                        continue;
                    }

                    var one = Elites.Make(def, Elites.Roll(def, session.Rank, session.Random));
                    var angle = i * 2.4f;
                    var place = new Vector3(spot.X + Mathf.Sin(angle) * 3.5f, 0, spot.Z + Mathf.Cos(angle) * 3.5f);
                    var made = Enemy.Spawn(one, place, Random.Range(0, 360f), spot.Key);

                    if (made != null)
                    {
                        count++;

                        if (one.Elite != "")
                        {
                            Fx.Effects.Aura(made.transform, Models.Rgb(one.Colour), 9999, 1.4f);
                        }
                    }
                }

                out_[spot.Key] = spot;
                manned[spot.Key] = count;
            }
        }
    }
}
