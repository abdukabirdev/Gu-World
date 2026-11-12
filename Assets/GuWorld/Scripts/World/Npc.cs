using System.Collections.Generic;
using GuWorld.Actors;
using GuWorld.Core;
using GuWorld.UI;
using UnityEngine;

namespace GuWorld.World
{
    /// <summary>
    /// One of the mountain's people, standing where the plan puts them: turns to the hero, talks, and
    /// shows a mark when they have something to say. Those with a day of their own walk between the
    /// spots of their settlement as the hours pass (and step there at once when no one is watching).
    /// </summary>
    public class Npc : MonoBehaviour
    {
        public static readonly List<Npc> All = new List<Npc>();

        public NpcDef Def { get; private set; }
        RigAnimator rig;
        GameObject mark;
        Quaternion home;
        float wait;
        float dayWait;
        Vector3 goal;
        bool walking;

        public static Npc Make(NpcDef def, Transform parent)
        {
            var root = new GameObject(def.Name);
            root.transform.SetParent(parent, false);
            root.transform.position = Game.Ground(new Vector3(def.At.X, 0, def.At.Z));
            root.transform.rotation = Quaternion.Euler(0, def.Facing, 0);
            var npc = root.AddComponent<Npc>();
            npc.Def = def;
            npc.home = root.transform.rotation;
            npc.goal = root.transform.position;
            var model = Models.Human(root.transform, def.Look, false, false, out npc.rig, People.Seed(def.Id));
            npc.rig?.Play("idle", 0);
            npc.springs = Quality.Level >= 1 ? new SpringBones(model.transform) : null;
            var solid = root.AddComponent<CapsuleCollider>();
            solid.radius = 0.35f;
            solid.height = 1.8f;
            solid.center = new Vector3(0, 0.9f, 0);
            npc.mark = new GameObject("Mark");
            npc.mark.transform.SetParent(root.transform, false);
            npc.mark.transform.localPosition = new Vector3(0, 2.35f, 0);
            Models.Part("Gem", npc.mark.transform, Models.Quad, Models.Glow(new Color(1f, 0.82f, 0.4f, 1f)), Vector3.zero, new Vector3(0.28f, 0.5f, 1)).AddComponent<Fx.Billboard>();
            Models.Part("Glow", npc.mark.transform, Models.Quad, Models.Glow(new Color(1f, 0.82f, 0.4f, 0.45f)), Vector3.zero, Vector3.one * 1.2f).AddComponent<Fx.Billboard>();
            npc.mark.AddComponent<Fx.Hover>();

            return npc;
        }

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        void Update()
        {
            var hero = Game.Hero;
            var session = Game.Session;

            if (hero == null || session == null)
            {
                return;
            }

            wait -= Time.deltaTime;

            if (wait <= 0)
            {
                wait = 0.5f;

                if (session.IsGone(Def))
                {
                    gameObject.SetActive(false);
                    return;
                }

                mark.SetActive(session.HasBusiness(Def.Id));
            }

            if (Def.Day.Length > 0)
            {
                GoAboutTheDay(hero);
            }

            if (walking)
            {
                return;
            }

            // Turns to whoever comes close; back to their work when left alone.
            var to = hero.transform.position - transform.position;
            to.y = 0;
            var want = to.magnitude < 7 && to.sqrMagnitude > 0.1f ? Quaternion.LookRotation(to) : home;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, want, 200 * Time.deltaTime);
        }

        /// <summary>Walks to where their day has them now; stops to talk when the hero is close.</summary>
        void GoAboutTheDay(Hero hero)
        {
            dayWait -= Time.deltaTime;

            if (dayWait <= 0)
            {
                dayWait = 2;
                var at = Atlas.WhereAt(Def, Atmosphere.Hour, out var facing);
                goal = Game.Ground(new Vector3(at.X, 0, at.Z));
                home = Quaternion.Euler(0, facing, 0);

                // Far from the hero's eyes they are simply there.
                if ((goal - transform.position).sqrMagnitude > 4 && (hero.transform.position - transform.position).sqrMagnitude > 70 * 70)
                {
                    transform.position = goal;
                    transform.rotation = home;
                }
            }

            var away = goal - transform.position;
            away.y = 0;
            var near = (hero.transform.position - transform.position).sqrMagnitude < 9;

            if (away.magnitude > 0.6f && !near && !Game.Busy)
            {
                walking = true;
                var step = away.normalized * Mathf.Min(away.magnitude, 1.7f * Time.deltaTime);
                var next = transform.position + step;
                transform.position = new Vector3(next.x, Ground.SampleHeight(next), next.z);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(away), 360 * Time.deltaTime);
                rig?.Play("walk", 0.2f);
            }
            else if (walking)
            {
                walking = false;
                rig?.Play("idle", 0.3f);
            }
        }

        SpringBones springs;

        void LateUpdate()
        {
            if (!Game.Frozen)
            {
                rig?.Tick(Time.deltaTime);

                // Only those near enough to be looked at swing their sleeves.
                if (springs != null && Game.Hero != null && (Game.Hero.transform.position - transform.position).sqrMagnitude < 30 * 30)
                {
                    springs.Tick(Time.deltaTime, SpringBones.Wind);
                }
            }
        }

        public void Talk()
        {
            var talk = Game.Session.TalkTo(Def.Id);

            if (talk == null)
            {
                return;
            }

            if (talk.Dialogue != null)
            {
                Game.Ui.Converse(talk.Dialogue, null);
                return;
            }

            var service = talk.Service;
            Game.Ui.Talk(talk.Lines, () =>
            {
                talk.Finish?.Invoke();

                switch (service)
                {
                    case "shop": Game.Ui.OpenShop(talk.Shop, Def.Title, talk.Clan); break;
                    case "gamble": Game.Ui.Show(Ui.Page.Gamble); break;
                    // The valley's scribe keeps the clan's errands; a settlement's scribe its board.
                    case "board":
                        if (Def.Home != "")
                        {
                            Game.Ui.OpenBoard(Def.Home);
                        }
                        else
                        {
                            Game.Ui.Show(Ui.Page.Board);
                        }

                        break;
                    case "inn": Game.Ui.OpenInn(Def); break;
                }
            });
        }
    }
}
