using System.Collections.Generic;
using GuWorld.Actors;
using GuWorld.Core;
using GuWorld.UI;
using UnityEngine;

namespace GuWorld.World
{
    /// <summary>One of the valley's people, standing where the plan puts them: turns to the hero, talks, and shows a mark when they have something to say.</summary>
    public class Npc : MonoBehaviour
    {
        public static readonly List<Npc> All = new List<Npc>();

        public NpcDef Def { get; private set; }
        RigAnimator rig;
        GameObject mark;
        Quaternion home;
        float wait;

        public static Npc Make(NpcDef def, Transform parent)
        {
            var root = new GameObject(def.Name);
            root.transform.SetParent(parent, false);
            root.transform.position = Game.Ground(new Vector3(def.At.X, 0, def.At.Z));
            root.transform.rotation = Quaternion.Euler(0, def.Facing, 0);
            var npc = root.AddComponent<Npc>();
            npc.Def = def;
            npc.home = root.transform.rotation;
            Models.Human(root.transform, def.Look, false, false, out npc.rig);
            npc.rig?.Play("idle", 0);
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

            // Turns to whoever comes close; back to their work when left alone.
            var to = hero.transform.position - transform.position;
            to.y = 0;
            var want = to.magnitude < 7 && to.sqrMagnitude > 0.1f ? Quaternion.LookRotation(to) : home;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, want, 200 * Time.deltaTime);
        }

        void LateUpdate()
        {
            if (!Game.Frozen)
            {
                rig?.Tick(Time.deltaTime);
            }
        }

        public void Talk()
        {
            var talk = Game.Session.TalkTo(Def.Id);

            if (talk == null)
            {
                return;
            }

            var service = talk.Service;
            Game.Ui.Talk(talk.Lines, () =>
            {
                talk.Finish?.Invoke();

                switch (service)
                {
                    case "shop": Game.Ui.Show(Ui.Page.Shop); break;
                    case "gamble": Game.Ui.Show(Ui.Page.Gamble); break;
                    case "board": Game.Ui.Show(Ui.Page.Board); break;
                }
            });
        }
    }
}
