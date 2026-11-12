using System.Collections.Generic;
using UnityEngine;

namespace GuWorld.Actors
{
    /// <summary>
    /// The soft parts of a person — the long hair and its locks, the bangs, the sleeves, the robe's
    /// skirt panels, the belt's ties — swinging after the body moves. Each chain of bones follows the
    /// animated pose like a spring (stiffness), keeps its momentum (less with drag), falls (gravity),
    /// feels the wind, and is pushed out of the legs and the body so the skirt does not pass through the
    /// thighs. The method is the one avatar systems use (VRM's): each bone's tail is a particle held
    /// at the bone's length from its head.
    /// </summary>
    public class SpringBones
    {
        /// <summary>The wind the soft parts feel now: a breeze from the south-west, stronger in bad weather, gusting.</summary>
        public static Vector3 Wind
        {
            get
            {
                var strength = 0.6f + 2.4f * World.Atmosphere.Gloom;
                var gust = 0.6f + 0.4f * Mathf.PerlinNoise(Time.time * 0.3f, 0.7f);

                return new Vector3(0.6f, 0, 0.8f) * strength * gust;
            }
        }

        /// <summary>A family of bones by their names' start, and how it moves.</summary>
        struct Kind
        {
            public string Prefix;
            public float Stiffness;
            public float Drag;
            public float Gravity;
            public float Radius;
            public float Wind;

            public Kind(string prefix, float stiffness, float drag, float gravity, float radius, float wind)
            {
                Prefix = prefix;
                Stiffness = stiffness;
                Drag = drag;
                Gravity = gravity;
                Radius = radius;
                Wind = wind;
            }
        }

        static readonly Kind[] Kinds =
        {
            new Kind("hairBack", 0.9f, 0.35f, 0.6f, 0.05f, 1f),
            new Kind("lock", 0.8f, 0.35f, 0.5f, 0.03f, 1f),
            new Kind("bang", 2.4f, 0.5f, 0.2f, 0.015f, 0.4f),
            new Kind("sleeve", 1.2f, 0.4f, 0.8f, 0.04f, 0.6f),
            new Kind("skirt", 1.6f, 0.45f, 0.5f, 0.05f, 0.5f),
            new Kind("tie", 0.6f, 0.3f, 0.9f, 0.02f, 1.2f),
        };

        class Joint
        {
            public Transform Bone;
            public Transform Child;
            public Vector3 Axis;
            public float Length;
            public Vector3 Tail, Previous;
            public Quaternion Rest;
            public Quaternion LastSet;
            public bool Set;
            public Kind Kind;
        }

        /// <summary>A part of the body the soft bones may not pass into: a sphere, or a capsule from one bone to another.</summary>
        struct Collider
        {
            public Transform From;
            public Transform To;
            public float Radius;
        }

        readonly List<List<Joint>> chains = new List<List<Joint>>();
        readonly List<Collider> colliders = new List<Collider>();
        readonly Transform root;
        Vector3 lastRoot;
        bool started;

        public int Count { get; private set; }

        public SpringBones(Transform model)
        {
            root = model;
            var bones = new Dictionary<string, Transform>();

            foreach (var bone in model.GetComponentsInChildren<Transform>(true))
            {
                bones[bone.name] = bone;
            }

            foreach (var bone in bones.Values)
            {
                var kind = KindOf(bone.name, out var found);

                // A chain starts at a bone of a soft family whose parent is not of it.
                if (!found || bone.parent == null || StartsWith(bone.parent.name, kind.Prefix))
                {
                    continue;
                }

                var chain = new List<Joint>();

                for (var at = bone; at != null; at = SoftChild(at, kind.Prefix))
                {
                    var child = SoftChild(at, kind.Prefix);
                    var joint = new Joint { Bone = at, Child = child, Kind = kind, Rest = at.localRotation };

                    if (child != null)
                    {
                        joint.Axis = child.localPosition.normalized;
                        joint.Length = child.localPosition.magnitude;
                    }
                    else if (chain.Count > 0)
                    {
                        // The last bone's tail goes on the way the chain was going.
                        var previous = chain[chain.Count - 1];
                        joint.Axis = Quaternion.Inverse(at.localRotation) * previous.Axis;
                        joint.Length = previous.Length * 0.8f;
                    }
                    else
                    {
                        joint.Axis = Vector3.down;
                        joint.Length = 0.05f;
                    }

                    chain.Add(joint);
                }

                if (chain.Count > 0)
                {
                    chains.Add(chain);
                    Count += chain.Count;
                }
            }

            // The legs and the body.
            void Capsule(string from, string to, float radius)
            {
                if (bones.TryGetValue(from, out var a) && bones.TryGetValue(to, out var b))
                {
                    colliders.Add(new Collider { From = a, To = b, Radius = radius });
                }
            }

            Capsule("thigh_L", "shin_L", 0.085f);
            Capsule("thigh_R", "shin_R", 0.085f);
            Capsule("shin_L", "foot_L", 0.06f);
            Capsule("shin_R", "foot_R", 0.06f);
            Capsule("spine", "chest", 0.13f);
            Capsule("chest", "neck", 0.12f);
            Capsule("hips", "spine", 0.14f);
            Capsule("upperArm_L", "forearm_L", 0.05f);
            Capsule("upperArm_R", "forearm_R", 0.05f);
        }

        static bool StartsWith(string name, string prefix) => name.StartsWith(prefix, System.StringComparison.Ordinal);

        static Kind KindOf(string name, out bool found)
        {
            foreach (var kind in Kinds)
            {
                if (StartsWith(name, kind.Prefix))
                {
                    found = true;

                    return kind;
                }
            }

            found = false;

            return default;
        }

        static Transform SoftChild(Transform bone, string prefix)
        {
            for (var i = 0; i < bone.childCount; i++)
            {
                var child = bone.GetChild(i);

                if (StartsWith(child.name, prefix))
                {
                    return child;
                }
            }

            return null;
        }

        /// <summary>A frame of the springs, after the animation has posed the body.</summary>
        public void Tick(float dt, Vector3 wind)
        {
            if (dt <= 0 || chains.Count == 0)
            {
                return;
            }

            dt = Mathf.Min(dt, 1 / 30f);
            var scale = root.lossyScale.y;

            // A jump of the whole body (a teleport, a door): the springs start again from the pose.
            var reset = !started || (root.position - lastRoot).sqrMagnitude > 4;
            lastRoot = root.position;
            started = true;

            foreach (var chain in chains)
            {
                foreach (var joint in chain)
                {
                    var bone = joint.Bone;
                    var head = bone.position;
                    // The rotation the animation gave (the spring pulls toward it). A bone the animation
                    // does not move still holds what the spring set last frame: then its rest is the pose.
                    var local = joint.Set && Quaternion.Angle(bone.localRotation, joint.LastSet) < 0.01f ? joint.Rest : bone.localRotation;
                    var animated = bone.parent.rotation * local;
                    var length = joint.Length * scale;
                    var aim = animated * joint.Axis;

                    if (reset)
                    {
                        joint.Tail = head + aim * length;
                        joint.Previous = joint.Tail;
                        bone.localRotation = local;
                        joint.LastSet = local;
                        joint.Set = true;
                        continue;
                    }

                    var kind = joint.Kind;
                    var inertia = (joint.Tail - joint.Previous) * (1 - kind.Drag);
                    var pull = aim * (kind.Stiffness * dt * length * 4);
                    var fall = Vector3.down * (kind.Gravity * dt * length * 2);
                    var blow = wind * (kind.Wind * dt * length * 0.25f);
                    var next = joint.Tail + inertia + pull + fall + blow;
                    next = head + (next - head).normalized * length;
                    next = Collide(next, kind.Radius * scale);
                    next = head + (next - head).normalized * length;
                    joint.Previous = joint.Tail;
                    joint.Tail = next;

                    // Turn the bone from where the animation points it to where the spring has it.
                    var turn = Quaternion.FromToRotation(aim, next - head);
                    bone.rotation = turn * animated;
                    joint.LastSet = bone.localRotation;
                    joint.Set = true;
                }
            }
        }

        Vector3 Collide(Vector3 point, float radius)
        {
            foreach (var collider in colliders)
            {
                var a = collider.From.position;
                var b = collider.To.position;
                var ab = b - a;
                var k = Mathf.Clamp01(Vector3.Dot(point - a, ab) / Mathf.Max(0.0001f, ab.sqrMagnitude));
                var closest = a + ab * k;
                var away = point - closest;
                var reach = collider.Radius * root.lossyScale.y + radius;

                if (away.sqrMagnitude < reach * reach)
                {
                    point = closest + (away.sqrMagnitude > 0.000001f ? away.normalized : Vector3.up) * reach;
                }
            }

            return point;
        }
    }
}
