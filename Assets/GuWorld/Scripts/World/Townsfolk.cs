using System.Collections.Generic;
using GuWorld.Actors;
using GuWorld.Core;
using GuWorld.Core.Gen;
using UnityEngine;

namespace GuWorld.World
{
    /// <summary>
    /// The town's crowd in the engine. The day of every townsperson is planned in Core (<see cref="Crowd"/>);
    /// here only the ones near the hero get a body. A body follows its person's day at a walking pace in
    /// real seconds (the clock of the game runs sixty times faster, so the plan says where they are going,
    /// and the body walks there along the plan's streets); those out of sight are simply where the plan
    /// puts them. Bodies come from a pool (by look) and go back to it when their person walks out of
    /// reach or goes indoors.
    /// </summary>
    public class Townsfolk : MonoBehaviour
    {
        sealed class Body
        {
            public GameObject Root;
            public RigAnimator Rig;
            public string Look;
            public Walker Walker;
            public int Leg = -1;
            public float Distance;
            public bool Walking;
            public Vector3 Goal;
            public float Facing;
        }

        readonly List<Walker> walkers = new List<Walker>();
        readonly List<Body> active = new List<Body>();
        readonly Dictionary<string, Stack<Body>> pool = new Dictionary<string, Stack<Body>>();
        readonly Dictionary<Walker, Body> bodyOf = new Dictionary<Walker, Body>();
        readonly List<(Walker walker, float distance)> wanted = new List<(Walker, float)>();
        float wait;
        float reach;
        int most;
        int made;

        public static Townsfolk Make(Transform parent, SettlementPlan plan)
        {
            var root = new GameObject("Townsfolk of " + plan.Def.Name);
            root.transform.SetParent(parent, false);
            var folk = root.AddComponent<Townsfolk>();
            folk.walkers.AddRange(plan.Walkers);
            folk.reach = Quality.Level >= 2 ? 95 : Quality.Level == 1 ? 70 : 45;
            folk.most = Quality.Level >= 2 ? 48 : Quality.Level == 1 ? 28 : 14;

            return folk;
        }

        static readonly List<Townsfolk> all = new List<Townsfolk>();

        void OnEnable() => all.Add(this);
        void OnDisable() => all.Remove(this);

        /// <summary>The townsperson with a body nearest a point (within `reach`), and its root; null if none.</summary>
        public static (Townsfolk folk, Walker walker, Transform body) Nearest(Vector3 at, float reach)
        {
            (Townsfolk, Walker, Transform) best = (null, null, null);
            var bestDistance = reach * reach;

            foreach (var folk in all)
            {
                foreach (var body in folk.active)
                {
                    var d = (body.Root.transform.position - at).sqrMagnitude;

                    if (d < bestDistance)
                    {
                        bestDistance = d;
                        best = (folk, body.Walker, body.Root.transform);
                    }
                }
            }

            return best;
        }

        /// <summary>Whether a townsperson is behind their stall now (and not walking to or from it).</summary>
        public bool AtStall(Walker walker) => bodyOf.TryGetValue(walker, out var body) && !body.Walking && walker.Stall >= 0 && Crowd.At(walker, Atmosphere.Hour).Doing == "stall";

        /// <summary>A townsperson turns to the one speaking to them.</summary>
        public void TurnTo(Walker walker, Vector3 to)
        {
            if (bodyOf.TryGetValue(walker, out var body) && !body.Walking)
            {
                var away = to - body.Root.transform.position;
                away.y = 0;

                if (away.sqrMagnitude > 0.01f)
                {
                    body.Facing = Quaternion.LookRotation(away).eulerAngles.y;
                }
            }
        }

        /// <summary>How many have bodies now and how many exist (for the autotest's log).</summary>
        public string Count() => $"{active.Count} townsfolk shown of {walkers.Count} ({made} bodies made)";

        void Update()
        {
            var hero = Game.Hero;

            if (hero == null || walkers.Count == 0)
            {
                return;
            }

            var focus = hero.transform.position;
            wait -= Time.deltaTime;

            if (wait <= 0)
            {
                wait = 0.4f;
                Choose(focus);
            }

            var dt = Time.deltaTime;

            foreach (var body in active)
            {
                Move(body, dt);
            }
        }

        /// <summary>Who should have a body: the nearest of those out of doors within reach.</summary>
        void Choose(Vector3 focus)
        {
            wanted.Clear();
            var hour = Atmosphere.Hour;
            // On a wolf tide's night everyone is behind their doors.
            var tide = Game.Session != null && Game.Session.TideUp;
            var here = new Vec2(focus.x, focus.z);

            foreach (var walker in walkers)
            {
                // Those with a body are where their body is; the rest where the plan puts them.
                Vec2 at;

                if (bodyOf.TryGetValue(walker, out var has))
                {
                    at = new Vec2(has.Root.transform.position.x, has.Root.transform.position.z);
                }
                else
                {
                    var pose = Crowd.At(walker, hour);

                    if (pose.Indoors)
                    {
                        continue;
                    }

                    at = pose.At;
                }

                var distance = Vec2.Distance(at, here);

                if (distance < reach && !(tide && walker.Clan == "gu_yue"))
                {
                    wanted.Add((walker, distance));
                }
            }

            wanted.Sort((a, b) => a.distance.CompareTo(b.distance));
            var keep = new HashSet<Walker>();
            var allowed = Mathf.Max(6, Mathf.RoundToInt(most * Governor.Detail));

            for (var i = 0; i < wanted.Count && i < allowed; i++)
            {
                keep.Add(wanted[i].walker);
            }

            // Bodies whose person is no longer wanted (or has gone in and stands at home) go back.
            for (var i = active.Count - 1; i >= 0; i--)
            {
                var body = active[i];
                var home = !body.Walking && Crowd.At(body.Walker, hour).Indoors;

                if (!keep.Contains(body.Walker) || home)
                {
                    Release(body);
                    active.RemoveAt(i);
                }
            }

            var fresh = 0;

            foreach (var walker in keep)
            {
                if (!bodyOf.ContainsKey(walker))
                {
                    // A generated person takes a few milliseconds to make: a handful a round, so the frame holds.
                    if (People.Enabled && !(pool.TryGetValue("w" + walker.Id, out var waiting) && waiting.Count > 0) && fresh++ >= 6)
                    {
                        continue;
                    }

                    var body = Take(walker);

                    if (body != null)
                    {
                        Place(body, hour);
                        active.Add(body);
                    }
                }
            }
        }

        Body Take(Walker walker)
        {
            // Generated people are each their own: a body goes back to its own person's pool. Watchmen
            // keep bodies of their own (they carry a lantern).
            var kind = People.Enabled ? "w" + walker.Id : walker.Trade == Trade.Watch ? walker.Look + "#watch" : walker.Look;

            if (!pool.TryGetValue(kind, out var stack))
            {
                pool[kind] = stack = new Stack<Body>();
            }

            Body body;

            if (stack.Count > 0)
            {
                body = stack.Pop();
                body.Root.SetActive(true);
                resting.Remove(body);
            }
            else
            {
                var root = new GameObject("Townsperson");
                root.transform.SetParent(transform, false);
                // Each townsperson is their own person: their body, face, hair and clothes from their number.
                Models.Human(root.transform, TradeLook(walker), false, false, out var rig, 104729 + walker.Id * 7 + (walker.Clan ?? "").Length, walker.Clan);
                body = new Body { Root = root, Rig = rig, Look = kind };

                // A watchman's lantern: a warm glow at the hand, lit at night.
                if (walker.Trade == Trade.Watch)
                {
                    var glow = new GameObject("Lantern");
                    glow.transform.SetParent(root.transform, false);
                    Models.Part("Light", glow.transform, Models.Quad, Models.Glow(new Color(1f, 0.6f, 0.3f, 0.7f)), new Vector3(0.35f, 1.0f, 0.3f), Vector3.one * 0.9f).AddComponent<Fx.Billboard>();
                    root.AddComponent<NightGlow>().Glow = glow;
                }
                made++;
            }

            body.Walker = walker;
            body.Leg = -1;
            bodyOf[walker] = body;

            return body;
        }

        readonly LinkedList<Body> resting = new LinkedList<Body>();

        void Release(Body body)
        {
            bodyOf.Remove(body.Walker);
            body.Walker = null;
            body.Root.SetActive(false);
            pool[body.Look].Push(body);

            if (!People.Enabled)
            {
                return;
            }

            // Kept for when their person comes back; the longest unused go when too many wait.
            resting.AddLast(body);

            while (resting.Count > 40)
            {
                var old = resting.First.Value;
                resting.RemoveFirst();

                if (pool.TryGetValue(old.Look, out var stack) && stack.Count > 0 && stack.Peek() == old)
                {
                    stack.Pop();

                    foreach (var renderer in old.Root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    {
                        Destroy(renderer.sharedMesh);
                    }

                    Destroy(old.Root);
                }
            }
        }

        /// <summary>The look a townsperson is made from: their trade, in the clan's colours.</summary>
        static string TradeLook(Walker walker)
        {
            switch (walker.Trade)
            {
                case Trade.Trader: return "trader";
                case Trade.Porter: return "porter";
                case Trade.Farmer: return "farmer";
                case Trade.Student: return "student";
                case Trade.Guard: return "guard";
                case Trade.Elder: return "elder";
                case Trade.Child: return "child";
                case Trade.Watch: return "watch";
                default: return walker.Look == "villager" || string.IsNullOrEmpty(walker.Look) ? "villager" : walker.Look;
            }
        }

        /// <summary>A new body appears where its person is by the plan, part way along a walk if they are walking.</summary>
        void Place(Body body, float hour)
        {
            var walker = body.Walker;
            var index = Crowd.LegAt(walker, hour);
            var pose = Crowd.At(walker, hour);
            body.Root.transform.position = Game.Ground(new Vector3(pose.At.X, 0, pose.At.Z));
            body.Root.transform.rotation = Quaternion.Euler(0, pose.Facing, 0);
            body.Leg = index;

            if (index >= 0 && walker.Day[index].Path.Length >= 2)
            {
                var leg = walker.Day[index];
                var h = hour < leg.From ? hour + 24 : hour;
                body.Distance = leg.Length * Mathf.Clamp01((h - leg.From) / Mathf.Max(1e-4f, leg.To - leg.From));
                body.Walking = true;
                body.Rig?.Play("walk", 0, false, walker.Pace / 1.3f);
            }
            else
            {
                body.Walking = false;
                body.Facing = pose.Facing;
                body.Rig?.Play("idle", 0);
            }
        }

        /// <summary>A frame of a body: along its walk at its pace, or standing turned to its work; a new leg of the plan starts when it is free.</summary>
        void Move(Body body, float dt)
        {
            var walker = body.Walker;
            var transformOf = body.Root.transform;

            if (body.Walking)
            {
                var leg = walker.Day[body.Leg];
                body.Distance += walker.Pace * dt;
                var (at, heading) = Crowd.Along(leg.Path, Mathf.Min(body.Distance, leg.Length));
                var next = Game.Ground(new Vector3(at.X, 0, at.Z));
                transformOf.position = Vector3.Lerp(transformOf.position, next, 0.5f);
                transformOf.rotation = Quaternion.RotateTowards(transformOf.rotation, Quaternion.Euler(0, heading, 0), 300 * dt);

                if (body.Distance >= leg.Length)
                {
                    // Arrived: stand as the leg's end has it until the plan moves on.
                    body.Walking = false;
                    body.Facing = leg.Facing;
                    body.Rig?.Play("idle", 0.3f);
                }
            }
            else
            {
                transformOf.rotation = Quaternion.RotateTowards(transformOf.rotation, Quaternion.Euler(0, body.Facing, 0), 200 * dt);
                var index = Crowd.LegAt(walker, Atmosphere.Hour);

                // The plan has moved on to a new walk: set off along it from wherever the body stands.
                if (index != body.Leg)
                {
                    body.Leg = index;
                    var leg = index >= 0 ? walker.Day[index] : null;

                    if (leg != null && leg.Path.Length >= 2)
                    {
                        body.Distance = 0;
                        body.Walking = true;
                        body.Rig?.Play("walk", 0.25f, false, walker.Pace / 1.3f);
                    }
                    else if (leg != null)
                    {
                        body.Facing = leg.Facing;
                    }
                }
            }

            if (!Game.Frozen)
            {
                body.Rig?.Tick(dt);
            }
        }
    }
}
