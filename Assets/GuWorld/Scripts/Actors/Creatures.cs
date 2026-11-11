using System.Collections.Generic;
using UnityEngine;

namespace GuWorld.Actors
{
    /// <summary>
    /// The creatures made in code, for the beasts the artist's models do not cover: a snake or a
    /// centipede (a head and a body of segments that follows its path), a spider on eight legs, a
    /// toad, a bat and an eagle that beat their wings, a swarm of bees, a will-o'-the-wisp. Each moves
    /// itself from how its root moves, so the enemy's mind does not need to know what body it has.
    /// </summary>
    public static class Creatures
    {
        /// <summary>The model for a "bug" body by its look (spider, toad, bat, eagle, bee, wisp; anything else: the old beetle).</summary>
        public static GameObject Make(Transform parent, Color colour, string look, float scale)
        {
            switch (look)
            {
                case "spider":
                case "spider_queen":
                    return Spider.Make(parent, colour, scale, look == "spider_queen");
                case "toad":
                    return Toad.Make(parent, colour, scale);
                case "bat":
                    return Wings.Make(parent, colour, scale, false);
                case "eagle":
                    return Wings.Make(parent, colour, scale, true);
                case "bee":
                    return Swarm.Make(parent, colour, scale);
                case "wisp":
                    return Wisp.Make(parent, colour, scale);
                default:
                    return Models.Bug(parent, colour, scale);
            }
        }

        /// <summary>The tint of a look the canid's textures do not have (a bear is a big brown canid until it has a model of its own).</summary>
        public static bool Tint(string look, out Color colour)
        {
            switch (look)
            {
                case "bear": colour = new Color(0.38f, 0.26f, 0.17f); return true;
                case "boar": colour = new Color(0.3f, 0.25f, 0.22f); return true;
                case "snow": colour = new Color(0.9f, 0.93f, 0.96f); return true;
                default: colour = Color.white; return false;
            }
        }

        /// <summary>How far and how fast the root of a creature moved this frame (its parts follow it).</summary>
        public class Motion
        {
            Vector3 last;
            bool started;
            public float Speed;
            public Vector3 Velocity;

            public void Step(Transform root, float dt)
            {
                if (!started)
                {
                    started = true;
                    last = root.position;
                }

                Velocity = dt > 0 ? (root.position - last) / dt : Vector3.zero;
                Velocity.y = 0;
                Speed = Mathf.Lerp(Speed, Velocity.magnitude, Mathf.Clamp01(dt * 8));
                last = root.position;
            }
        }
    }

    /// <summary>A snake or a centipede: a head, and segments that lie along the path the head has taken.</summary>
    public class Serpent : MonoBehaviour
    {
        readonly List<Transform> segments = new List<Transform>();
        readonly List<Transform[]> legs = new List<Transform[]>();
        readonly List<Vector3> trail = new List<Vector3>();
        Transform head;
        float spacing;
        float phase;
        bool centipede;
        readonly Creatures.Motion motion = new Creatures.Motion();

        public static GameObject Make(Transform parent, Color colour, string look, float scale)
        {
            var root = new GameObject("Model");
            root.transform.SetParent(parent, false);
            var serpent = root.AddComponent<Serpent>();
            serpent.Build(colour, look, scale);

            return root;
        }

        void Build(Color colour, string look, float scale)
        {
            centipede = look == "centipede";
            var count = centipede ? 14 : 16;
            spacing = (centipede ? 0.42f : 0.36f) * scale;
            var skin = Models.Lit(Color.Lerp(colour, Color.black, centipede ? 0.55f : 0.45f), centipede ? 0.6f : 0.35f);
            var belly = Models.Lit(Color.Lerp(colour, Color.white, 0.25f), 0.3f);
            var leg = Models.Lit(Color.Lerp(colour, Color.black, 0.7f), 0.5f);
            head = new GameObject("Head").transform;
            head.SetParent(transform, false);
            Models.Shape("Skull", head, PrimitiveType.Sphere, skin, new Vector3(0, 0.22f, 0.12f) * scale, new Vector3(0.36f, 0.24f, 0.46f) * scale);
            Models.Shape("Jaw", head, PrimitiveType.Sphere, belly, new Vector3(0, 0.15f, 0.16f) * scale, new Vector3(0.28f, 0.12f, 0.36f) * scale);
            var eye = Models.Glow(centipede ? new Color(1f, 0.5f, 0.3f) : new Color(1f, 0.85f, 0.3f));
            Models.Shape("Eye L", head, PrimitiveType.Sphere, eye, new Vector3(-0.12f, 0.29f, 0.26f) * scale, Vector3.one * 0.06f * scale);
            Models.Shape("Eye R", head, PrimitiveType.Sphere, eye, new Vector3(0.12f, 0.29f, 0.26f) * scale, Vector3.one * 0.06f * scale);

            if (centipede)
            {
                Models.Shape("Fang L", head, PrimitiveType.Capsule, leg, new Vector3(-0.1f, 0.12f, 0.36f) * scale, new Vector3(0.04f, 0.1f, 0.04f) * scale, Quaternion.Euler(70, 0, 20));
                Models.Shape("Fang R", head, PrimitiveType.Capsule, leg, new Vector3(0.1f, 0.12f, 0.36f) * scale, new Vector3(0.04f, 0.1f, 0.04f) * scale, Quaternion.Euler(70, 0, -20));
            }

            for (var i = 0; i < count; i++)
            {
                var k = i / (float)(count - 1);
                var thick = (centipede ? 0.34f : Mathf.Lerp(0.34f, 0.08f, k * k)) * scale;
                var segment = new GameObject("Segment " + i).transform;
                segment.SetParent(transform, false);
                Models.Shape("Body", segment, PrimitiveType.Sphere, i % 2 == 0 ? skin : belly, new Vector3(0, thick * 0.55f, 0), new Vector3(thick, thick * (centipede ? 0.55f : 0.8f), spacing * 1.5f));
                segments.Add(segment);

                if (centipede && i < count - 1)
                {
                    var pair = new Transform[2];

                    for (var side = 0; side < 2; side++)
                    {
                        var hip = new GameObject(side == 0 ? "Leg L" : "Leg R").transform;
                        hip.SetParent(segment, false);
                        hip.localPosition = new Vector3((side == 0 ? -1 : 1) * thick * 0.45f, thick * 0.3f, 0);
                        Models.Shape("Leg", hip, PrimitiveType.Capsule, leg, new Vector3((side == 0 ? -1 : 1) * 0.22f * scale, -0.08f * scale, 0), new Vector3(0.04f, 0.2f, 0.04f) * scale, Quaternion.Euler(0, 0, side == 0 ? 60 : -60));
                        pair[side] = hip;
                    }

                    legs.Add(pair);
                }
            }

            for (var i = 0; i <= count + 1; i++)
            {
                trail.Add(transform.position - transform.forward * spacing * i);
            }
        }

        void LateUpdate()
        {
            var dt = Time.deltaTime;
            motion.Step(transform, dt);
            phase += dt * (2 + motion.Speed * (centipede ? 3f : 1.6f));

            // The head is where the root is; the trail remembers where it has been.
            var front = transform.position;

            if ((front - trail[0]).sqrMagnitude > spacing * spacing * 0.25f)
            {
                trail.Insert(0, front);

                if (trail.Count > segments.Count + 4)
                {
                    trail.RemoveAt(trail.Count - 1);
                }
            }
            else
            {
                trail[0] = front;
            }

            head.position = front;
            head.rotation = transform.rotation;
            var along = 0f;
            var index = 0;

            for (var i = 0; i < segments.Count; i++)
            {
                along += spacing;
                var at = PointAlong(along, ref index, out var direction);

                // A snake sways from side to side as it goes; a centipede keeps straight and runs its legs.
                if (!centipede)
                {
                    var side = Vector3.Cross(Vector3.up, direction).normalized;
                    at += side * Mathf.Sin(phase - i * 0.55f) * spacing * 0.6f * Mathf.Clamp01(motion.Speed * 0.5f + 0.15f);
                }

                at.y = Mathf.Lerp(segments[i].position.y, World.Ground.SampleHeight(at), 0.5f);
                segments[i].position = at;

                if (direction.sqrMagnitude > 0.0001f)
                {
                    segments[i].rotation = Quaternion.LookRotation(direction);
                }
            }

            for (var i = 0; i < legs.Count; i++)
            {
                var swing = Mathf.Sin(phase * 2.2f - i * 0.9f) * 35 * Mathf.Clamp01(motion.Speed);
                legs[i][0].localRotation = Quaternion.Euler(0, swing, 0);
                legs[i][1].localRotation = Quaternion.Euler(0, -swing, 0);
            }
        }

        /// <summary>The point a distance back along the trail, and the way the body faces there.</summary>
        Vector3 PointAlong(float distance, ref int index, out Vector3 direction)
        {
            var walked = 0f;

            for (var i = 0; i < trail.Count - 1; i++)
            {
                var step = Vector3.Distance(trail[i], trail[i + 1]);

                if (walked + step >= distance || i == trail.Count - 2)
                {
                    var k = step > 0.0001f ? Mathf.Clamp01((distance - walked) / step) : 0;
                    direction = trail[i] - trail[i + 1];
                    direction.y = 0;
                    index = i;

                    return Vector3.Lerp(trail[i], trail[i + 1], k);
                }

                walked += step;
            }

            direction = transform.forward;

            return trail[trail.Count - 1];
        }
    }

    /// <summary>A spider: a body, a round abdomen with a mark, eight legs of two parts that step in turn.</summary>
    public class Spider : MonoBehaviour
    {
        readonly Transform[] hips = new Transform[8];
        readonly Transform[] knees = new Transform[8];
        readonly Creatures.Motion motion = new Creatures.Motion();
        Transform body;
        float phase;

        public static GameObject Make(Transform parent, Color colour, float scale, bool queen)
        {
            var root = new GameObject("Model");
            root.transform.SetParent(parent, false);
            var spider = root.AddComponent<Spider>();
            spider.Build(colour, scale, queen);

            return root;
        }

        void Build(Color colour, float scale, bool queen)
        {
            var shell = Models.Lit(Color.Lerp(colour, Color.black, 0.82f), 0.55f);
            var hair = Models.Lit(Color.Lerp(colour, Color.black, 0.65f), 0.2f);
            var mark = Models.Glow(new Color(colour.r, colour.g, colour.b, 0.9f));
            body = new GameObject("Body").transform;
            body.SetParent(transform, false);
            body.localPosition = new Vector3(0, 0.42f * scale, 0);
            Models.Shape("Thorax", body, PrimitiveType.Sphere, shell, new Vector3(0, 0, 0.12f) * scale, new Vector3(0.42f, 0.28f, 0.46f) * scale);
            Models.Shape("Abdomen", body, PrimitiveType.Sphere, hair, new Vector3(0, 0.1f, -0.42f) * scale, new Vector3(0.62f, 0.5f, 0.74f) * scale * (queen ? 1.35f : 1));
            Models.Shape("Mark", body, PrimitiveType.Sphere, mark, new Vector3(0, 0.33f, -0.42f) * scale * (queen ? 1.2f : 1), new Vector3(0.16f, 0.05f, 0.3f) * scale);

            for (var e = 0; e < (queen ? 8 : 4); e++)
            {
                var x = (e % 4 - 1.5f) * 0.06f;
                Models.Shape("Eye", body, PrimitiveType.Sphere, Models.Glow(new Color(0.7f, 1f, 0.8f)), new Vector3(x, 0.08f + (e / 4) * 0.05f, 0.34f) * scale, Vector3.one * 0.05f * scale);
            }

            for (var i = 0; i < 8; i++)
            {
                var side = i < 4 ? -1 : 1;
                var row = i % 4;
                var hip = new GameObject("Hip " + i).transform;
                hip.SetParent(body, false);
                hip.localPosition = new Vector3(side * 0.16f, 0, 0.22f - row * 0.12f) * scale;
                hip.localRotation = Quaternion.Euler(0, side * (60 - row * 30) + (side < 0 ? 180 : 0), 0);
                Models.Shape("Upper", hip, PrimitiveType.Capsule, shell, new Vector3(0, 0.12f, 0.3f) * scale, new Vector3(0.05f, 0.24f, 0.05f) * scale, Quaternion.Euler(60, 0, 0));
                var knee = new GameObject("Knee").transform;
                knee.SetParent(hip, false);
                knee.localPosition = new Vector3(0, 0.24f, 0.58f) * scale;
                Models.Shape("Lower", knee, PrimitiveType.Capsule, hair, new Vector3(0, -0.3f, 0.12f) * scale, new Vector3(0.04f, 0.32f, 0.04f) * scale, Quaternion.Euler(-20, 0, 0));
                hips[i] = hip;
                knees[i] = knee;
            }
        }

        void LateUpdate()
        {
            var dt = Time.deltaTime;
            motion.Step(transform, dt);
            phase += dt * (1.5f + motion.Speed * 4);
            var moving = Mathf.Clamp01(motion.Speed * 0.6f);

            for (var i = 0; i < 8; i++)
            {
                // Legs step in two sets of four, crossing sides.
                var set = (i + i / 4) % 2 == 0 ? 0 : Mathf.PI;
                var lift = Mathf.Max(0, Mathf.Sin(phase + set)) * 25 * moving;
                var reach = Mathf.Cos(phase + set) * 18 * moving;
                var basis = hips[i].localEulerAngles.y;
                hips[i].localRotation = Quaternion.Euler(-lift, basis, 0) * Quaternion.Euler(0, reach * 0.02f, 0);
                knees[i].localRotation = Quaternion.Euler(lift * 0.5f, 0, 0);
            }

            body.localPosition = new Vector3(0, body.localPosition.y, 0) + Vector3.up * Mathf.Sin(phase * 2) * 0.015f * moving;
        }
    }

    /// <summary>A toad: squat, wide-eyed, its throat swelling; it hops when it moves.</summary>
    public class Toad : MonoBehaviour
    {
        Transform body, throat;
        readonly Creatures.Motion motion = new Creatures.Motion();
        float phase;

        public static GameObject Make(Transform parent, Color colour, float scale)
        {
            var root = new GameObject("Model");
            root.transform.SetParent(parent, false);
            var toad = root.AddComponent<Toad>();
            var skin = Models.Lit(Color.Lerp(colour, new Color(0.2f, 0.25f, 0.1f), 0.5f), 0.45f);
            var belly = Models.Lit(Color.Lerp(colour, new Color(0.9f, 0.85f, 0.6f), 0.5f), 0.3f);
            toad.body = new GameObject("Body").transform;
            toad.body.SetParent(root.transform, false);
            Models.Shape("Back", toad.body, PrimitiveType.Sphere, skin, new Vector3(0, 0.26f, 0) * scale, new Vector3(0.62f, 0.4f, 0.7f) * scale);
            Models.Shape("Head", toad.body, PrimitiveType.Sphere, skin, new Vector3(0, 0.3f, 0.3f) * scale, new Vector3(0.52f, 0.3f, 0.38f) * scale);
            var eye = Models.Glow(new Color(1f, 0.85f, 0.2f));

            foreach (var side in new[] { -1, 1 })
            {
                Models.Shape("Eye", toad.body, PrimitiveType.Sphere, eye, new Vector3(side * 0.16f, 0.45f, 0.38f) * scale, Vector3.one * 0.11f * scale);
                Models.Shape("Leg", toad.body, PrimitiveType.Sphere, skin, new Vector3(side * 0.32f, 0.12f, -0.15f) * scale, new Vector3(0.22f, 0.18f, 0.4f) * scale);
                Models.Shape("Arm", toad.body, PrimitiveType.Capsule, skin, new Vector3(side * 0.24f, 0.1f, 0.28f) * scale, new Vector3(0.08f, 0.12f, 0.08f) * scale);
            }

            toad.throat = Models.Shape("Throat", toad.body, PrimitiveType.Sphere, belly, new Vector3(0, 0.18f, 0.38f) * scale, new Vector3(0.3f, 0.2f, 0.2f) * scale).transform;
            toad.throatSize = toad.throat.localScale;

            return root;
        }

        void LateUpdate()
        {
            var dt = Time.deltaTime;
            motion.Step(transform, dt);
            phase += dt * (motion.Speed > 0.3f ? 7 : 2.4f);
            var hop = motion.Speed > 0.3f ? Mathf.Abs(Mathf.Sin(phase)) * 0.35f : 0;
            body.localPosition = new Vector3(0, hop, 0);
            body.localRotation = Quaternion.Euler(-hop * 30, 0, 0);
            // The throat swells and empties.
            throat.localScale = throatSize * (1 + 0.35f * Mathf.Max(0, Mathf.Sin(phase * 1.3f)));
        }

        Vector3 throatSize;
    }

    /// <summary>A bat or an eagle: a body between two wings that beat (and an eagle glides between beats).</summary>
    public class Wings : MonoBehaviour
    {
        Transform left, right, body;
        bool eagle;
        float phase;
        readonly Creatures.Motion motion = new Creatures.Motion();

        public static GameObject Make(Transform parent, Color colour, float scale, bool eagle)
        {
            var root = new GameObject("Model");
            root.transform.SetParent(parent, false);
            var wings = root.AddComponent<Wings>();
            wings.eagle = eagle;
            wings.body = new GameObject("Body").transform;
            wings.body.SetParent(root.transform, false);
            var feather = Models.Lit(eagle ? new Color(0.42f, 0.32f, 0.22f) : new Color(0.16f, 0.12f, 0.14f), 0.15f);
            var light = Models.Lit(eagle ? new Color(0.92f, 0.9f, 0.86f) : new Color(0.3f, 0.22f, 0.24f), 0.15f);
            Models.Shape("Torso", wings.body, PrimitiveType.Sphere, feather, Vector3.zero, new Vector3(0.26f, 0.24f, eagle ? 0.6f : 0.34f) * scale);
            Models.Shape("Head", wings.body, PrimitiveType.Sphere, eagle ? light : feather, new Vector3(0, 0.08f, eagle ? 0.34f : 0.2f) * scale, Vector3.one * (eagle ? 0.2f : 0.16f) * scale);

            if (eagle)
            {
                Models.Shape("Beak", wings.body, PrimitiveType.Capsule, Models.Lit(new Color(0.95f, 0.75f, 0.2f), 0.4f), new Vector3(0, 0.05f, 0.47f) * scale, new Vector3(0.05f, 0.07f, 0.05f) * scale, Quaternion.Euler(80, 0, 0));
                Models.Shape("Tail", wings.body, PrimitiveType.Cube, feather, new Vector3(0, 0, -0.42f) * scale, new Vector3(0.26f, 0.03f, 0.3f) * scale);
            }
            else
            {
                Models.Shape("Ear L", wings.body, PrimitiveType.Capsule, feather, new Vector3(-0.06f, 0.18f, 0.2f) * scale, new Vector3(0.03f, 0.07f, 0.03f) * scale);
                Models.Shape("Ear R", wings.body, PrimitiveType.Capsule, feather, new Vector3(0.06f, 0.18f, 0.2f) * scale, new Vector3(0.03f, 0.07f, 0.03f) * scale);
                Models.Shape("Eyes", wings.body, PrimitiveType.Sphere, Models.Glow(new Color(1f, 0.3f, 0.2f)), new Vector3(0, 0.1f, 0.27f) * scale, new Vector3(0.12f, 0.03f, 0.03f) * scale);
            }

            var membrane = eagle ? feather : Models.Glow(new Color(0.25f, 0.16f, 0.2f, 0.9f), false);
            var span = (eagle ? 1.1f : 0.7f) * scale;
            wings.left = new GameObject("Wing L").transform;
            wings.left.SetParent(wings.body, false);
            wings.left.localPosition = new Vector3(-0.1f * scale, 0.05f * scale, 0);
            Models.Part("Skin", wings.left, Models.Quad, membrane, new Vector3(-span * 0.5f, 0, 0), new Vector3(span, span * (eagle ? 0.42f : 0.55f), 1), Quaternion.Euler(90, 0, 0));
            wings.right = new GameObject("Wing R").transform;
            wings.right.SetParent(wings.body, false);
            wings.right.localPosition = new Vector3(0.1f * scale, 0.05f * scale, 0);
            Models.Part("Skin", wings.right, Models.Quad, membrane, new Vector3(span * 0.5f, 0, 0), new Vector3(span, span * (eagle ? 0.42f : 0.55f), 1), Quaternion.Euler(90, 0, 0));

            if (eagle)
            {
                // The wing's light underside.
                Models.Part("Under", wings.left, Models.Quad, light, new Vector3(-span * 0.45f, -0.01f, 0), new Vector3(span * 0.8f, span * 0.3f, 1), Quaternion.Euler(-90, 0, 0));
                Models.Part("Under", wings.right, Models.Quad, light, new Vector3(span * 0.45f, -0.01f, 0), new Vector3(span * 0.8f, span * 0.3f, 1), Quaternion.Euler(-90, 0, 0));
            }

            return root;
        }

        void LateUpdate()
        {
            var dt = Time.deltaTime;
            motion.Step(transform, dt);

            // An eagle beats its wings in bursts and glides between them; a bat never stops.
            var beat = eagle ? 5.5f : 13f;
            phase += dt * beat;
            var gliding = eagle && Mathf.Sin(phase * 0.15f) > 0.3f;
            var angle = gliding ? 8 : Mathf.Sin(phase) * (eagle ? 38 : 55);
            left.localRotation = Quaternion.Euler(0, 0, -angle);
            right.localRotation = Quaternion.Euler(0, 0, angle);
            var bank = Mathf.Clamp(Vector3.SignedAngle(transform.forward, motion.Velocity.sqrMagnitude > 0.1f ? motion.Velocity : transform.forward, Vector3.up), -40, 40);
            body.localRotation = Quaternion.Euler(gliding ? 4 : 0, 0, -bank * 0.6f);
            body.localPosition = new Vector3(0, gliding ? 0 : Mathf.Cos(phase) * 0.06f, 0);
        }
    }

    /// <summary>A swarm of bees: a cloud of small bodies circling a centre.</summary>
    public class Swarm : MonoBehaviour
    {
        readonly List<Transform> bees = new List<Transform>();
        readonly List<Vector3> orbits = new List<Vector3>();
        float time;

        public static GameObject Make(Transform parent, Color colour, float scale)
        {
            var root = new GameObject("Model");
            root.transform.SetParent(parent, false);
            var swarm = root.AddComponent<Swarm>();
            var stripe = Models.Lit(new Color(0.95f, 0.75f, 0.15f), 0.3f);
            var dark = Models.Lit(new Color(0.1f, 0.08f, 0.05f), 0.3f);
            var wing = Models.Glow(new Color(1, 1, 1, 0.35f));

            for (var i = 0; i < 9; i++)
            {
                var bee = new GameObject("Bee").transform;
                bee.SetParent(root.transform, false);
                Models.Shape("Body", bee, PrimitiveType.Sphere, i % 2 == 0 ? stripe : dark, Vector3.zero, new Vector3(0.07f, 0.07f, 0.12f) * scale);
                Models.Part("Wings", bee, Models.Quad, wing, new Vector3(0, 0.05f, 0) * scale, new Vector3(0.2f, 0.08f, 1) * scale, Quaternion.Euler(90, 0, 0));
                swarm.bees.Add(bee);
                swarm.orbits.Add(new Vector3(Random.Range(0.3f, 0.9f) * scale, Random.Range(2f, 5f), Random.Range(0, Mathf.PI * 2)));
            }

            Models.Part("Haze", root.transform, Models.Quad, Models.Glow(new Color(colour.r, colour.g, colour.b, 0.25f)), Vector3.zero, Vector3.one * 1.6f * scale).AddComponent<Fx.Billboard>();

            return root;
        }

        void LateUpdate()
        {
            time += Time.deltaTime;

            for (var i = 0; i < bees.Count; i++)
            {
                var (radius, speed, offset) = (orbits[i].x, orbits[i].y, orbits[i].z);
                var a = time * speed + offset;
                var at = new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a * 1.7f + offset) * radius * 0.5f, Mathf.Sin(a) * radius);
                var ahead = new Vector3(-Mathf.Sin(a), 0, Mathf.Cos(a));
                bees[i].localPosition = at;
                bees[i].localRotation = Quaternion.LookRotation(ahead);
            }
        }
    }

    /// <summary>A will-o'-the-wisp: a flame that is not a flame, flickering and drifting.</summary>
    public class Wisp : MonoBehaviour
    {
        Transform core;
        Light glow;
        float time;
        float seed;

        public static GameObject Make(Transform parent, Color colour, float scale)
        {
            var root = new GameObject("Model");
            root.transform.SetParent(parent, false);
            var wisp = root.AddComponent<Wisp>();
            wisp.seed = Random.value * 10;
            wisp.core = Models.Shape("Core", root.transform, PrimitiveType.Sphere, Models.Glow(new Color(colour.r, colour.g, colour.b, 0.95f)), Vector3.zero, Vector3.one * 0.3f * scale).transform;
            Models.Part("Flame", root.transform, Models.Quad, Models.Glow(new Color(colour.r, colour.g, colour.b, 0.5f)), new Vector3(0, 0.15f, 0) * scale, new Vector3(0.9f, 1.4f, 1) * scale).AddComponent<Fx.Billboard>();
            var light = new GameObject("Light").AddComponent<Light>();
            light.transform.SetParent(root.transform, false);
            light.type = LightType.Point;
            light.color = colour;
            light.range = 7;
            light.intensity = 1.6f;
            light.shadows = LightShadows.None;
            wisp.glow = light;

            return root;
        }

        void LateUpdate()
        {
            time += Time.deltaTime;
            var flicker = 0.85f + 0.15f * Mathf.PerlinNoise(time * 6, seed);
            core.localScale = Vector3.one * 0.3f * flicker;
            glow.intensity = 1.6f * flicker;
            core.localPosition = new Vector3(Mathf.Sin(time * 1.3f + seed) * 0.08f, Mathf.Sin(time * 2.1f) * 0.1f, 0);
        }
    }
}
