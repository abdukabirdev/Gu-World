using System.Collections;
using System.Collections.Generic;
using GuWorld.Actors;
using GuWorld.Core;
using UnityEngine;

namespace GuWorld.World
{
    /// <summary>
    /// The immortal aperture: a jade terrace floating high over the wild south-west corner of the
    /// mountain, where the hero tries Gu without consequence. Entering (from the pause menu) keeps
    /// where the hero stood; at the altar in the middle (T) the page of loans opens; training posts and
    /// any beast of the book can be called onto the terrace; falling off brings the hero back to its
    /// middle; leaving returns the hero and everything they had.
    /// </summary>
    public class Aperture : MonoBehaviour
    {
        /// <summary>Where the terrace hangs (over the wild corner, far from every story place).</summary>
        static readonly Vector2 Spot = new Vector2(-7300, -7300);
        const float Above = 420;

        public static Aperture Instance { get; private set; }

        Vector3 centre;
        Vector3 back;
        float backYaw;
        readonly List<Enemy> called = new List<Enemy>();

        public static bool Inside => Instance != null && Game.Session != null && Game.Session.InSandbox;

        /// <summary>The altar's prompt when the hero stands by it, else null.</summary>
        public static string Prompt => Inside && Game.Hero != null && Vector3.Distance(Game.Hero.transform.position, Instance.centre) < 4.5f ? "Алтарь апертуры · Гу, ранг, приёмы" : null;

        public static void Enter()
        {
            if (Game.Session == null || Game.Hero == null || Game.Session.InSandbox)
            {
                return;
            }

            Game.Run(Instance != null ? Instance.Entering() : Make().Entering());
        }

        public static void Leave()
        {
            if (Inside)
            {
                Game.Run(Instance.Leaving());
            }
        }

        static Aperture Make()
        {
            var root = new GameObject("Immortal aperture");
            var aperture = root.AddComponent<Aperture>();
            Instance = aperture;
            var ground = Game.Ground(new Vector3(Spot.x, 0, Spot.y)).y;
            aperture.centre = new Vector3(Spot.x, ground + Above, Spot.y);
            Models.Prefab(CodeProp.Name("aperture"), root.transform, aperture.centre, 0);
            Models.Prefab(CodeProp.Name("gu_altar"), root.transform, aperture.centre + new Vector3(0, 0, 3.5f), 180);

            // A light over the terrace, and a ring of soft glow round the middle.
            var light = new GameObject("Light").AddComponent<Light>();
            light.transform.SetParent(root.transform, false);
            light.transform.position = aperture.centre + Vector3.up * 14;
            light.type = LightType.Point;
            light.range = 60;
            light.intensity = 1.6f;
            light.color = new Color(0.75f, 1f, 0.88f);

            return aperture;
        }

        IEnumerator Entering()
        {
            var hero = Game.Hero;
            back = hero.transform.position;
            backYaw = hero.transform.eulerAngles.y;
            yield return Game.Ui.FadeOut(0.4f);
            Game.Session.EnterSandbox();
            hero.Teleport(centre + new Vector3(0, 0.3f, -8));
            Game.Rig.Follow(hero, 0);
            CallPosts();
            yield return null;
            yield return Game.Ui.FadeIn(0.5f);
            Game.Ui.Banner("Апертура бессмертного", "Здесь ничего не считается");
            Sound.AudioDirector.Play("lantern", null, 0.8f, 0);
        }

        IEnumerator Leaving()
        {
            yield return Game.Ui.FadeOut(0.4f);
            Dismiss();
            Game.Session.LeaveSandbox();
            Game.Hero.Teleport(back + Vector3.up * 0.2f);
            Game.Rig.Follow(Game.Hero, backYaw);
            yield return null;
            yield return Game.Ui.FadeIn(0.5f);
        }

        /// <summary>Four training posts in a square before the altar.</summary>
        public void CallPosts()
        {
            var dummy = Bestiary.Get("dummy");

            for (var i = 0; i < 4; i++)
            {
                var at = centre + new Vector3(-9 + i * 6, 0.4f, 12);
                called.Add(Enemy.Spawn(dummy, at, 180, "aperture"));
            }
        }

        /// <summary>Calls beasts of the book onto the terrace, a few steps before the hero.</summary>
        public void Call(string enemyId, int count)
        {
            var def = Bestiary.Get(enemyId);

            if (def == null || Game.Hero == null)
            {
                return;
            }

            for (var i = 0; i < count; i++)
            {
                var angle = (i - (count - 1) * 0.5f) * 0.35f;
                var forward = Quaternion.Euler(0, Game.Hero.transform.eulerAngles.y + angle * Mathf.Rad2Deg, 0) * Vector3.forward;
                var at = Game.Hero.transform.position + forward * 12;
                // Kept on the terrace.
                var flat = new Vector2(at.x - centre.x, at.z - centre.z);

                if (flat.magnitude > 24)
                {
                    flat = flat.normalized * 24;
                }

                called.Add(Enemy.Spawn(def, new Vector3(centre.x + flat.x, centre.y + 0.5f, centre.z + flat.y), Game.Hero.transform.eulerAngles.y + 180, "aperture"));
            }
        }

        /// <summary>Sends away everything called (the posts too).</summary>
        public void Dismiss()
        {
            foreach (var enemy in called)
            {
                if (enemy != null)
                {
                    Destroy(enemy.gameObject);
                }
            }

            called.Clear();
        }

        public int Living
        {
            get
            {
                var count = 0;

                foreach (var enemy in called)
                {
                    count += enemy != null && enemy.Alive ? 1 : 0;
                }

                return count;
            }
        }

        void Update()
        {
            if (!Inside || Game.Hero == null)
            {
                return;
            }

            // Off the edge: back to the middle.
            if (Game.Hero.transform.position.y < centre.y - 25)
            {
                Game.Hero.Teleport(centre + new Vector3(0, 0.3f, -8));
            }

            foreach (var enemy in called)
            {
                if (enemy != null && enemy.transform.position.y < centre.y - 25)
                {
                    enemy.Teleport(centre + new Vector3(0, 0.5f, 10));
                }
            }
        }
    }
}
