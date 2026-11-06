using System.Collections.Generic;
using UnityEngine;

namespace GuWorld.Body
{
    /// <summary>
    /// The hero's own model (Resources/Models/gu-hero.glb, made in the Gu Rig editor:
    /// a long-haired Gu Yue youth in a dark hanfu, its texture, face and hands baked in)
    /// worn over the Mannequin's joints, the way HumanBody wears the made bodies.
    ///
    /// Its 209 bones come from the editor's rig: hips, spine, spine1, spine2, chest,
    /// neck, neck1, head; shoulder/upperArm/forearm/hand and thigh/shin/foot per side
    /// (_L, _R); fingers index/middle/ring/pinky 0…3 and thumb 1…3. The body moves
    /// with the main ones; the twist and helper bones (upperArmTwist, elbowHelper,
    /// forearmTwist1/2, hipHelper, thighTwist, kneeHelper, shinTwist) take a share of
    /// their neighbour's turn so the joints keep their volume, as in the editor. The
    /// face, hair, sleeve and skirt bones hold their rest pose (the robe is weighted
    /// to the legs).
    ///
    /// Everything about comparing the turns in the figure's space is HumanBody's.
    /// </summary>
    public class GuHeroBody
    {
        public const string ResourcePath = "Models/gu-hero";

        /// <summary>Bone → the Mannequin joint that turns it, and the share of the joint's own turn it takes.</summary>
        static readonly Dictionary<string, (string joint, float share)> Drivers = new Dictionary<string, (string, float)>
        {
            ["hips"] = ("hips", 1),
            // The back's bend spread over its vertebrae, the head's turn partly in the neck.
            ["spine"] = ("spine", 0.5f),
            ["spine1"] = ("spine", 1),
            ["spine2"] = ("chest", 0.5f),
            ["chest"] = ("chest", 1),
            ["neck"] = ("head", 0.3f),
            ["neck1"] = ("head", 0.6f),
            ["head"] = ("head", 1),
            ["shoulder_L"] = ("shoulderL", 0.2f),
            ["upperArm_L"] = ("shoulderL", 1),
            ["forearm_L"] = ("elbowL", 1),
            ["hand_L"] = ("wristL", 1),
            ["shoulder_R"] = ("shoulderR", 0.2f),
            ["upperArm_R"] = ("shoulderR", 1),
            ["forearm_R"] = ("elbowR", 1),
            ["hand_R"] = ("wristR", 1),
            ["thigh_L"] = ("hipL", 1),
            ["shin_L"] = ("kneeL", 1),
            ["foot_L"] = ("ankleL", 1),
            ["thigh_R"] = ("hipR", 1),
            ["shin_R"] = ("kneeR", 1),
            ["foot_R"] = ("ankleR", 1),
        };

        /// <summary>
        /// The editor's drivers (rigdef.js DRIVERS): a helper bends by a share of its neighbour's
        /// bend ("half"), a twist bone turns by a share of a bone's roll about its length ("roll").
        /// </summary>
        static readonly (string bone, string source, bool roll, float share)[] Helpers =
        {
            ("upperArmTwist", "upperArm", true, -0.5f),
            ("elbowHelper", "forearm", false, 0.5f),
            ("forearmTwist1", "hand", true, 0.33f),
            ("forearmTwist2", "hand", true, 0.66f),
            ("hipHelper", "thigh", false, 0.5f),
            ("thighTwist", "thigh", true, -0.5f),
            ("kneeHelper", "shin", false, 0.5f),
            ("shinTwist", "foot", true, 0.5f),
        };

        /// <summary>The robe's wide sleeves: the arms hang a little further out than the made bodies' (radians).</summary>
        const float ArmOut = 0.14f;
        const float CurlRest = 0.3f;
        const float FistCurl = 1.25f;
        static readonly string[] Fingers = { "index", "middle", "ring", "pinky" };
        /// <summary>How much of the curl each knuckle (1…3, from the palm out) takes.</summary>
        static readonly float[] Knuckles = { 0.8f, 1, 0.75f };

        class Driven
        {
            public Transform bone;
            public Transform joint;
            public float share;
            public Quaternion rest;
            public Driven parent;
            public Quaternion target;
        }

        class Finger
        {
            public Transform bone;
            public Quaternion rest;
            public Vector3 axis;
            public float amount;
            public Vector3 across;
            public float over;
        }

        class Helper
        {
            public Transform bone;
            public Transform source;
            public Quaternion rest;
            public Quaternion sourceRest;
            public bool roll;
            public float share;
        }

        /// <summary>Holds the model: scaled to the Mannequin, feet on the ground, facing +Z.</summary>
        public readonly Transform Holder;

        readonly Transform root;
        readonly Mannequin rig;
        readonly Dictionary<string, Transform> bones = new Dictionary<string, Transform>();
        readonly List<Driven> driven = new List<Driven>();
        readonly List<Finger> fingers = new List<Finger>();
        readonly List<Helper> helpers = new List<Helper>();
        readonly Vector3 hipsDriverRest;
        readonly Quaternion pelvisParent;
        readonly Vector3 pelvisRest;
        readonly Transform pelvis;
        readonly Transform model;

        /// <summary>Materials made for the model once (toon, anime), shared by every copy.</summary>
        static GameObject template;

        /// <summary>The model, loaded once; null when it is not in the build.</summary>
        public static GameObject Load()
        {
            return Resources.Load<GameObject>(ResourcePath);
        }

        public GuHeroBody(GameObject source, Mannequin rig)
        {
            this.rig = rig;
            root = rig.Root;

            // Measured in the rest pose, with the figure upright.
            rig.ApplyRest();

            Holder = new GameObject("GU Hero").transform;
            Holder.SetParent(root, false);
            var instance = Object.Instantiate(Template(source), Holder, false);
            instance.name = source.name;
            instance.SetActive(true);
            model = instance.transform;

            foreach (var t in instance.GetComponentsInChildren<Transform>(true))
            {
                bones[t.name] = t;
            }

            foreach (var renderer in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                renderer.shadowCastingMode = renderer.sharedMaterial != null && renderer.sharedMaterial.renderQueue >= 2900
                    ? UnityEngine.Rendering.ShadowCastingMode.Off
                    : UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = true;
                renderer.updateWhenOffscreen = true;
            }

            Fit(Mannequin.Hips - Mannequin.HipJoint);
            Calibrate();

            var joints = new Dictionary<string, Transform>
            {
                ["hips"] = rig.HipsJoint, ["spine"] = rig.Spine, ["chest"] = rig.Chest, ["head"] = rig.Head,
                ["shoulderL"] = rig.ShoulderL, ["shoulderR"] = rig.ShoulderR,
                ["elbowL"] = rig.ElbowL, ["elbowR"] = rig.ElbowR,
                ["wristL"] = rig.WristL, ["wristR"] = rig.WristR,
                ["hipL"] = rig.HipL, ["hipR"] = rig.HipR,
                ["kneeL"] = rig.KneeL, ["kneeR"] = rig.KneeR,
                ["ankleL"] = rig.AnkleL, ["ankleR"] = rig.AnkleR,
            };
            var byBone = new Dictionary<Transform, Driven>();

            // Parents before children (GetComponentsInChildren goes depth first), so each parent's target is ready first.
            foreach (var t in instance.GetComponentsInChildren<Transform>(true))
            {
                if (!Drivers.TryGetValue(t.name, out var drive))
                {
                    continue;
                }

                var entry = new Driven
                {
                    bone = t,
                    joint = joints[drive.joint],
                    share = drive.share,
                    rest = InFigure(t),
                    parent = t.parent != null && byBone.TryGetValue(t.parent, out var parent) ? parent : null,
                };
                driven.Add(entry);
                byBone[t] = entry;
            }

            pelvis = Bone("hips");
            pelvisParent = InFigure(pelvis.parent);
            pelvisRest = PositionInFigure(pelvis);
            hipsDriverRest = PositionInFigure(rig.HipsJoint);
            FindFingers();
            FindHelpers();
        }

        /// <summary>
        /// The model with the game's look (cel shading and an ink outline, as every body has),
        /// made once on a hidden shelf. The face's portrait is see-through at its edges and
        /// keeps its own material.
        /// </summary>
        static GameObject Template(GameObject source)
        {
            if (template != null)
            {
                return template;
            }

            var shelf = new GameObject("GU Hero Template");
            shelf.SetActive(false);
            Object.DontDestroyOnLoad(shelf);
            template = Object.Instantiate(source, shelf.transform, false);
            template.name = source.name;
            Toon.Apply(template);
            Anime.Apply(template);

            return template;
        }

        /// <summary>Copies the joints' turns onto the bones. Call after the joints move.</summary>
        public void Update()
        {
            var rootInverse = Quaternion.Inverse(root.rotation);

            foreach (var entry in driven)
            {
                Quaternion target;

                if (entry.share < 1)
                {
                    target = rootInverse * entry.joint.parent.rotation *
                             Quaternion.Slerp(Quaternion.identity, entry.joint.localRotation, entry.share);
                }
                else
                {
                    target = rootInverse * entry.joint.rotation;
                }

                target *= entry.rest;
                entry.target = target;

                var parentTarget = entry.parent != null ? entry.parent.target : pelvisParent;
                entry.bone.localRotation = Quaternion.Inverse(parentTarget) * target;
            }

            var curl = Mathf.Max(0.04f, CurlRest + rig.Grip * FistCurl);
            var fist = Mathf.Clamp01(rig.Grip);

            foreach (var finger in fingers)
            {
                var turn = finger.rest * Quaternion.AngleAxis(curl * finger.amount * Mathf.Rad2Deg, finger.axis);
                finger.bone.localRotation = finger.over > 0 ? turn * Quaternion.AngleAxis(fist * finger.over * Mathf.Rad2Deg, finger.across) : turn;
            }

            foreach (var helper in helpers)
            {
                if (helper.roll)
                {
                    // The source's roll about its own length (its Y), a share of it about the helper's length.
                    var delta = Quaternion.Inverse(helper.sourceRest) * helper.source.localRotation;
                    var twist = new Quaternion(0, delta.y, 0, delta.w);
                    var size = Mathf.Sqrt(twist.y * twist.y + twist.w * twist.w);
                    twist = size > 1e-5f ? new Quaternion(0, twist.y / size, 0, twist.w / size) : Quaternion.identity;
                    helper.bone.localRotation = helper.rest * Quaternion.SlerpUnclamped(Quaternion.identity, twist, helper.share);
                }
                else
                {
                    // A share of the source's bend, in their common parent's space (the helper sits on the same joint).
                    var bend = helper.source.localRotation * Quaternion.Inverse(helper.sourceRest);
                    helper.bone.localRotation = Quaternion.Slerp(Quaternion.identity, bend, helper.share) * helper.rest;
                }
            }

            // The hips also carry the body's shift (crouching, lying).
            var shift = PositionInFigure(rig.HipsJoint) - hipsDriverRest + pelvisRest;
            pelvis.position = root.TransformPoint(shift);
        }

        Transform Bone(string name)
        {
            if (!bones.TryGetValue(name, out var bone))
            {
                throw new KeyNotFoundException($"The GU hero model has no {name} bone");
            }

            return bone;
        }

        Quaternion InFigure(Transform t) => Quaternion.Inverse(root.rotation) * t.rotation;

        Vector3 PositionInFigure(Transform t) => root.InverseTransformPoint(t.position);

        /// <summary>Scales the model so its hip joints are the Mannequin's, feet on the ground (the model's origin), facing +Z.</summary>
        void Fit(float hipJoint)
        {
            Vector3 At(string name) => PositionInFigure(Bone(name));

            // The figure's left is -X here (glTFast mirrors X on the way in).
            if (At("upperArm_L").x > At("upperArm_R").x)
            {
                Holder.localRotation = Quaternion.Euler(0, 180, 0);
            }

            float Sole() => PositionInFigure(model).y;
            var hips = (At("thigh_L").y + At("thigh_R").y) / 2;
            var scale = hipJoint / Mathf.Max(0.1f, hips - Sole());

            Holder.localScale = Vector3.one * scale;

            var centre = At("hips");
            Holder.localPosition = new Vector3(-centre.x, -Sole(), -centre.z);
        }

        /// <summary>Hangs the arms and legs straight down: the Mannequin's rest pose.</summary>
        void Calibrate()
        {
            Vector3 Down(float side) => new Vector3(-side * Mathf.Sin(ArmOut), -Mathf.Cos(ArmOut), 0);
            var straight = Vector3.down;
            var aims = new (string bone, string child, Vector3 direction)[]
            {
                ("upperArm_L", "forearm_L", Down(1)),
                ("forearm_L", "hand_L", Down(1)),
                ("hand_L", "middle1_L", Down(1)),
                ("upperArm_R", "forearm_R", Down(-1)),
                ("forearm_R", "hand_R", Down(-1)),
                ("hand_R", "middle1_R", Down(-1)),
                ("thigh_L", "shin_L", straight),
                ("shin_L", "foot_L", straight),
                ("thigh_R", "shin_R", straight),
                ("shin_R", "foot_R", straight),
            };

            foreach (var (name, childName, direction) in aims)
            {
                if (!bones.TryGetValue(name, out var bone) || !bones.TryGetValue(childName, out var child))
                {
                    continue;
                }

                var from = root.TransformDirection(PositionInFigure(child) - PositionInFigure(bone));
                var turn = Quaternion.FromToRotation(from.normalized, root.TransformDirection(direction));
                bone.rotation = turn * bone.rotation;
            }
        }

        /// <summary>Per hand, which way the fingers curl: toward the palm, the side the thumb is on.</summary>
        void FindFingers()
        {
            foreach (var s in new[] { "L", "R" })
            {
                Vector3 At(string name) => PositionInFigure(Bone(name));
                var wrist = At($"hand_{s}");
                var along = (At($"middle1_{s}") - wrist).normalized;
                var across = At($"pinky1_{s}") - At($"index1_{s}");
                var palm = Vector3.Cross(along, across).normalized;

                if (Vector3.Dot(palm, At($"thumb3_{s}") - wrist) < 0)
                {
                    palm = -palm;
                }

                var axis = Vector3.Cross(along, palm).normalized;
                var thumb = (At($"thumb3_{s}") - At($"thumb1_{s}")).normalized;
                var over = Vector3.Dot(Vector3.Cross(palm, thumb), across) > 0 ? palm : -palm;

                void Add(string name, float amount, float swing = 0)
                {
                    if (!bones.TryGetValue(name, out var bone))
                    {
                        return;
                    }

                    fingers.Add(new Finger
                    {
                        bone = bone,
                        rest = bone.localRotation,
                        axis = Quaternion.Inverse(InFigure(bone)) * axis,
                        amount = amount,
                        across = Quaternion.Inverse(InFigure(bone)) * over,
                        over = swing,
                    });
                }

                foreach (var finger in Fingers)
                {
                    for (var k = 0; k < Knuckles.Length; k++)
                    {
                        Add($"{finger}{k + 1}_{s}", Knuckles[k]);
                    }
                }

                Add($"thumb1_{s}", 0.1f, 1.05f);
                Add($"thumb2_{s}", 0.45f);
                Add($"thumb3_{s}", 0.55f);
            }
        }

        void FindHelpers()
        {
            foreach (var s in new[] { "L", "R" })
            {
                foreach (var (name, sourceName, roll, share) in Helpers)
                {
                    if (bones.TryGetValue($"{name}_{s}", out var bone) && bones.TryGetValue($"{sourceName}_{s}", out var source))
                    {
                        helpers.Add(new Helper
                        {
                            bone = bone,
                            source = source,
                            rest = bone.localRotation,
                            sourceRest = source.localRotation,
                            roll = roll,
                            share = share,
                        });
                    }
                }
            }
        }

        public void Destroy()
        {
            if (Holder != null)
            {
                Object.Destroy(Holder.gameObject);
            }
        }
    }
}
