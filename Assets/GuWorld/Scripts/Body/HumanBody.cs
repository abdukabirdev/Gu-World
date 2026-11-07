using System.Collections.Generic;
using GuWorld.Engine;
using UnityEngine;

namespace GuWorld.Body
{
    /// <summary>
    /// A person's body: one of GU World's own people (Resources/People — a young man, a young
    /// woman and their grown-up selves, made in Blender from MakeHuman's base mesh by
    /// Tools/GuPeople/make_people.py) worn over the Mannequin's joints.
    ///
    /// Everything a person wears is a separate skinned mesh on the same skeleton: the clothes
    /// (Cloth_Under, Cloth_Trousers, Cloth_Wraps, Cloth_Shoes, Cloth_Robe or Cloth_Tunic,
    /// Cloth_Belt, Cloth_Mark), the hair (Hair_Short … Hair_Long) and the beards (Beard_…). A
    /// look is a choice of these pieces — the hairstyle, the beard, the long robe of a hanfu or
    /// a village tunic, the clan's crescent — the cloth dyed by the build's dye (Dyes) and the
    /// hair and beard by its hair colour. Each look is put together once, on a hidden shelf,
    /// and copied for everyone who has it.
    ///
    /// The Mannequin keeps doing all the moving on its own (invisible) joints; every frame each
    /// bone copies the turn of the joint that drives it. Rotations are compared in the figure's
    /// own space, so the bones' local axes don't matter: at load the model is brought into the
    /// Mannequin's rest pose (arms and legs hanging down), and from then on a bone's orientation
    /// is its joint's orientation times that rest one. Some bones take only a share of their
    /// joint's turn, which the real body spreads out: the bend of the back over its vertebrae,
    /// a turn of the head partly in the neck, a raised arm partly in the collarbone. The
    /// fingers rest loosely curled and close into a fist with the grip.
    /// </summary>
    public class HumanBody
    {
        /// <summary>Bone → the joint that turns it, and the share of the joint's own turn it takes.</summary>
        static readonly Dictionary<string, (string joint, float share)> Drivers = new Dictionary<string, (string, float)>
        {
            ["pelvis"] = ("hips", 1),
            // The lower back takes the spine's turn over its two vertebrae; the chest's vertebra takes the chest's (on top).
            ["spine_01"] = ("spine", 0.5f),
            ["spine_02"] = ("spine", 1),
            ["spine_03"] = ("chest", 1),
            ["neck_01"] = ("head", 0.45f),
            ["head"] = ("head", 1),
            ["clavicle_l"] = ("shoulderL", 0.2f),
            ["upperarm_l"] = ("shoulderL", 1),
            ["lowerarm_l"] = ("elbowL", 1),
            ["hand_l"] = ("wristL", 1),
            ["clavicle_r"] = ("shoulderR", 0.2f),
            ["upperarm_r"] = ("shoulderR", 1),
            ["lowerarm_r"] = ("elbowR", 1),
            ["hand_r"] = ("wristR", 1),
            ["thigh_l"] = ("hipL", 1),
            ["calf_l"] = ("kneeL", 1),
            ["foot_l"] = ("ankleL", 1),
            ["thigh_r"] = ("hipR", 1),
            ["calf_r"] = ("kneeR", 1),
            ["foot_r"] = ("ankleR", 1),
        };

        /// <summary>Arms hang this far out from straight down (radians) at rest: the robes' wide sleeves are between.</summary>
        const float ArmOut = 0.12f;
        /// <summary>Finger curl per joint (radians), relaxed, and how much more a fist adds.</summary>
        const float CurlRest = 0.3f;
        const float FistCurl = 1.25f;
        static readonly string[] Fingers = { "index", "middle", "ring", "pinky" };
        /// <summary>How much of the curl each knuckle takes, from the palm out.</summary>
        static readonly float[] Knuckles = { 0.8f, 1, 0.75f };

        /// <summary>Where the people are (Resources), one folder per kit: People/&lt;kit&gt;/&lt;kit&gt;.gltf.</summary>
        public const string Folder = "People";

        class Driven
        {
            public Transform bone;
            public Transform joint;
            public float share;
            /// <summary>The bone's orientation in figure space at the Mannequin's rest pose.</summary>
            public Quaternion rest;
            public Driven parent;
            /// <summary>This frame's orientation in figure space (for the children).</summary>
            public Quaternion target;
        }

        class Finger
        {
            public Transform bone;
            public Quaternion rest;
            /// <summary>The curl's axis, in the bone's own space.</summary>
            public Vector3 axis;
            public float amount;
            /// <summary>The thumb's swing over the palm: its axis (bone space) and how far a full fist takes it.</summary>
            public Vector3 across;
            public float over;
        }

        public readonly Transform Holder;

        readonly Transform root;
        readonly Mannequin rig;
        readonly Transform model;
        readonly Dictionary<string, Transform> bones = new Dictionary<string, Transform>();
        readonly List<Driven> driven = new List<Driven>();
        readonly List<Finger> fingers = new List<Finger>();
        readonly Vector3 hipsDriverRest;
        /// <summary>The pelvis' parent in figure space (it never moves).</summary>
        readonly Quaternion pelvisParent;
        readonly Vector3 pelvisRest;
        readonly Transform pelvis;

        /// <summary>What bodies are made from: the four kits as glTFast imports them.</summary>
        public struct Parts
        {
            public GameObject maleTeen;
            public GameObject maleAdult;
            public GameObject femaleTeen;
            public GameObject femaleAdult;

            /// <summary>The kits from Resources/People (any that is missing stays null).</summary>
            public static Parts Load() => new Parts
            {
                maleTeen = Kit("male_teen"),
                maleAdult = Kit("male_adult"),
                femaleTeen = Kit("female_teen"),
                femaleAdult = Kit("female_adult"),
            };

            static GameObject Kit(string name) => Resources.Load<GameObject>($"{Folder}/{name}/{name}");

            /// <summary>Both teenagers' kits are there (the hero, the children and the students are drawn on them).</summary>
            public bool HasTeens => maleTeen != null && femaleTeen != null;

            /// <summary>A gender's kit, the grown-up's first; null when there is none.</summary>
            public GameObject Model(Gender gender) => gender == Gender.Female
                ? (femaleAdult != null ? femaleAdult : femaleTeen)
                : (maleAdult != null ? maleAdult : maleTeen);

            /// <summary>The kit for a gender at an age: the teenager's from TeenFrom up, the grown-up's below.</summary>
            public GameObject ModelFor(Gender gender, float youth)
            {
                var teen = gender == Gender.Female ? femaleTeen : maleTeen;
                var adult = gender == Gender.Female ? femaleAdult : maleAdult;

                return youth >= TeenFrom ? (teen != null ? teen : adult) : (adult != null ? adult : teen);
            }
        }

        /// <summary>From this youth up a figure is made from the teenager's kit (the hero, children, students).</summary>
        public const float TeenFrom = 0.8f;

        /// <summary>How a body is grown beyond its look: its age, its hair's colour, its clothes' dye.</summary>
        public struct Build
        {
            /// <summary>1 fifteen (the teenager's kit), 0 a grown-up.</summary>
            public float youth;
            public Color hair;
            /// <summary>Which dye the clothes are (Dyes.For).</summary>
            public int dye;
            /// <summary>The clan's mark on the clothes.</summary>
            public Emblem emblem;
            /// <summary>The skin's brightness (Look.SkinBrightness, 146 tanned … 204 fair; 0: fair).</summary>
            public float skin;

            /// <summary>The hero: fifteen, black-haired, a Gu Yue in a dark hanfu.</summary>
            public static Build Hero => ForHero(Look.Default(Gender.Male));

            /// <summary>The hero in their look's hanfu, with the clan's crescent.</summary>
            public static Build ForHero(Look look) => new Build { youth = 1, hair = Look.HairBlack, dye = look.Dye, emblem = Emblem.Crescent, skin = look.SkinBrightness };

            public string Key => $"{(youth >= TeenFrom ? "teen" : "adult")}|{ColorUtility.ToHtmlStringRGB(hair)}|{dye}|{emblem}{(skin > 0 ? $"|{skin:0}" : "")}";
        }

        /// <summary>Bodies put together once per look and kept (hidden), copied for each who wears them.</summary>
        static readonly Dictionary<string, GameObject> templates = new Dictionary<string, GameObject>();
        /// <summary>The kits' materials dyed a colour, shared by every look that dyes them so.</summary>
        static readonly Dictionary<(Material, Color), Material> dyed = new Dictionary<(Material, Color), Material>();
        static Transform shelf;

        /// <summary>Ages in steps of a fifth (0, 0.2 … 1): kept for the callers that group people by age.</summary>
        public static float YouthStep(float youth) => Mathf.Round(Mathf.Clamp01(youth) * 5) / 5;

        public HumanBody(Parts parts, Look look, Mannequin rig) : this(parts, look, Build.Hero, rig)
        {
        }

        public HumanBody(Parts parts, Look look, Build build, Mannequin rig)
        {
            this.rig = rig;
            root = rig.Root;

            // Measured in the rest pose, with the figure upright.
            rig.ApplyRest();

            var template = Template(parts, look, build);
            using var cost = Costs.Time("body copied");
            Holder = new GameObject("Body").transform;
            Holder.SetParent(root, false);
            var instance = Object.Instantiate(template, Holder, false);
            instance.name = template.name;
            instance.SetActive(true);
            model = instance.transform;

            foreach (var t in instance.GetComponentsInChildren<Transform>(true))
            {
                bones[t.name] = t;
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

            // Parents before children (GetComponentsInChildren goes depth first), so each bone's parent target is ready first.
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

            pelvis = Bone("pelvis");
            pelvisParent = InFigure(pelvis.parent);
            pelvisRest = PositionInFigure(pelvis);
            hipsDriverRest = PositionInFigure(rig.HipsJoint);
            FindFingers();
        }

        /// <summary>The pieces a look wears (the skinned meshes' names in the kit); the rest are taken off.</summary>
        public static HashSet<string> Pieces(Look look, Build build)
        {
            var pieces = new HashSet<string>
            {
                "Body", "Eyes", "Eyebrows", "Eyelashes",
                "Cloth_Trousers", "Cloth_Wraps", "Cloth_Shoes", "Cloth_Belt",
                Dyes.Long(build.dye) ? "Cloth_Robe" : "Cloth_Tunic",
            };

            if (look.Hair != HairStyle.Bald)
            {
                pieces.Add("Hair_" + look.Hair);
            }

            if (look.Gender == Gender.Male && look.BeardStyle != Beard.None)
            {
                pieces.Add("Beard_" + look.BeardStyle);
            }

            if (build.emblem == Emblem.Crescent)
            {
                pieces.Add("Cloth_Mark");
            }

            return pieces;
        }

        /// <summary>The skin's colour for its brightness: fair (204) as the texture is, warmer and darker to tanned (146).</summary>
        public static Color SkinColour(float brightness)
        {
            var fair = brightness > 0 ? Mathf.InverseLerp(146, 204, brightness) : 1;

            return Color.Lerp(new Color(0.8f, 0.66f, 0.55f), new Color(1, 0.98f, 0.96f), fair);
        }

        /// <summary>
        /// The body for a look, put together once and kept on a hidden shelf: the kit for the gender
        /// and age, only the pieces the look wears left on, the cloth dyed, the hair coloured, then
        /// drawn as the art direction asks (cel-shaded, outlined). Its meshes and materials are shared by every copy.
        /// </summary>
        static GameObject Template(Parts parts, Look look, Build build)
        {
            var hairColour = build.hair.a > 0 ? build.hair : Look.HairBlack;
            build.hair = hairColour;
            var key = $"{look.Gender}/{look.Hair}/{look.BeardStyle}|{build.Key}";

            if (templates.TryGetValue(key, out var done) && done != null)
            {
                return done;
            }

            using var cost = Costs.Time("look built");

            if (shelf == null)
            {
                var go = new GameObject("Body Templates");
                go.SetActive(false);
                Object.DontDestroyOnLoad(go);
                shelf = go.transform;
            }

            var model = parts.ModelFor(look.Gender, build.youth);
            var instance = Object.Instantiate(model, shelf, false);
            instance.name = model.name;
            var wear = Pieces(look, build);
            var palette = Dyes.For(look.Gender, build.dye);
            var skin = SkinColour(build.skin);

            Color? ColourOf(string piece, string material)
            {
                if (piece.StartsWith("Hair_"))
                {
                    return material.StartsWith("Pin") ? (Color?)null : hairColour;
                }

                if (piece.StartsWith("Beard_"))
                {
                    return material.StartsWith("Stubble") ? Color.Lerp(skin * 0.8f, hairColour, 0.55f) : hairColour;
                }

                if (material.StartsWith("Skin")) return skin;
                if (material.StartsWith("Robe")) return palette.tunic;
                if (material.StartsWith("Trim")) return palette.trim;
                if (material.StartsWith("Belt")) return palette.sash;
                if (material.StartsWith("Trousers")) return palette.trousers;
                if (material.StartsWith("Wraps")) return palette.wraps;
                if (material.StartsWith("Shoes")) return palette.shoes;
                if (material.StartsWith("Mark")) return Dyes.MarkColour(build.dye);

                return null;
            }

            foreach (var renderer in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (!wear.Contains(renderer.name))
                {
                    Object.DestroyImmediate(renderer.gameObject);
                    continue;
                }

                var face = renderer.name == "Eyes" || renderer.name == "Eyebrows" || renderer.name == "Eyelashes";
                renderer.shadowCastingMode = face ? UnityEngine.Rendering.ShadowCastingMode.Off : UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = true;
                // Skinned meshes move away from their bounds.
                renderer.updateWhenOffscreen = true;

                var materials = renderer.sharedMaterials;

                for (var i = 0; i < materials.Length; i++)
                {
                    if (materials[i] != null && ColourOf(renderer.name, materials[i].name) is Color colour)
                    {
                        materials[i] = Dyed(materials[i], colour);
                    }
                }

                renderer.sharedMaterials = materials;
            }

            // Drawn as the art direction asks: cel-shaded, outlined.
            Toon.Apply(instance);
            Anime.Apply(instance);

            templates[key] = instance;

            return instance;
        }

        /// <summary>A kit's material in a colour (multiplied into its texture, when it has one), made once.</summary>
        static Material Dyed(Material source, Color colour)
        {
            colour.a = 1;

            if (dyed.TryGetValue((source, colour), out var done) && done != null)
            {
                return done;
            }

            var copy = new Material(source) { name = source.name };

            // glTFast's shader takes the colour as baseColorFactor; others as _BaseColor or _Color.
            foreach (var name in new[] { "baseColorFactor", "_BaseColor", "_Color" })
            {
                if (copy.HasProperty(name))
                {
                    copy.SetColor(name, colour);
                }
            }

            dyed[(source, colour)] = copy;

            return copy;
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
                    // The joint's parent turned, plus a share of the joint's own turn.
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

            // The fingers: loosely curled at rest, closed into a fist as the Mannequin grips (the thumb laid over them),
            // straight when it opens the hand.
            var curl = Mathf.Max(0.04f, CurlRest + rig.Grip * FistCurl);
            var fist = Mathf.Clamp01(rig.Grip);

            foreach (var finger in fingers)
            {
                var turn = finger.rest * Quaternion.AngleAxis(curl * finger.amount * Mathf.Rad2Deg, finger.axis);
                finger.bone.localRotation = finger.over > 0 ? turn * Quaternion.AngleAxis(fist * finger.over * Mathf.Rad2Deg, finger.across) : turn;
            }

            // The hips also carry the body's shift (crouching, lying).
            var shift = PositionInFigure(rig.HipsJoint) - hipsDriverRest + pelvisRest;
            pelvis.position = root.TransformPoint(shift);
        }

        Transform Bone(string name)
        {
            if (!bones.TryGetValue(name, out var bone))
            {
                throw new KeyNotFoundException($"The body has no {name} bone");
            }

            return bone;
        }

        Quaternion InFigure(Transform t) => Quaternion.Inverse(root.rotation) * t.rotation;

        Vector3 PositionInFigure(Transform t) => root.InverseTransformPoint(t.position);

        /// <summary>Scales the model so its hip joints are the Mannequin's, feet on the ground (the kit's origin), facing +Z.</summary>
        void Fit(float hipJoint)
        {
            Vector3 At(string name) => PositionInFigure(Bone(name));

            // The figure's left is -X here.
            if (At("upperarm_l").x > At("upperarm_r").x)
            {
                Holder.localRotation = Quaternion.Euler(0, 180, 0);
            }

            float Sole() => PositionInFigure(model).y;
            var hips = (At("thigh_l").y + At("thigh_r").y) / 2;
            var scale = hipJoint / Mathf.Max(0.1f, hips - Sole());

            Holder.localScale = Vector3.one * scale;

            var centre = At("pelvis");
            Holder.localPosition = new Vector3(-centre.x, -Sole(), -centre.z);
        }

        /// <summary>Hangs the arms and legs straight down: the Mannequin's rest pose.</summary>
        void Calibrate()
        {
            // Left is -X here.
            Vector3 Down(float side) => new Vector3(-side * Mathf.Sin(ArmOut), -Mathf.Cos(ArmOut), 0);
            var straight = Vector3.down;
            var aims = new (string bone, string child, Vector3 direction)[]
            {
                ("upperarm_l", "lowerarm_l", Down(1)),
                ("lowerarm_l", "hand_l", Down(1)),
                ("hand_l", "middle_01_l", Down(1)),
                ("upperarm_r", "lowerarm_r", Down(-1)),
                ("lowerarm_r", "hand_r", Down(-1)),
                ("hand_r", "middle_01_r", Down(-1)),
                ("thigh_l", "calf_l", straight),
                ("calf_l", "foot_l", straight),
                ("thigh_r", "calf_r", straight),
                ("calf_r", "foot_r", straight),
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

        /// <summary>Works out, per hand, which way the fingers curl: toward the palm, the side the thumb is on.</summary>
        void FindFingers()
        {
            foreach (var s in new[] { "l", "r" })
            {
                Vector3 At(string name) => PositionInFigure(Bone(name));
                var wrist = At($"hand_{s}");
                var along = (At($"middle_01_{s}") - wrist).normalized;
                var across = At($"pinky_01_{s}") - At($"index_01_{s}");
                var palm = Vector3.Cross(along, across).normalized;

                if (Vector3.Dot(palm, At($"thumb_03_{s}") - wrist) < 0)
                {
                    palm = -palm;
                }

                var axis = Vector3.Cross(along, palm).normalized;
                // The thumb swings over the palm toward the little finger: about the palm's normal, whichever way takes its tip there.
                var thumb = (At($"thumb_03_{s}") - At($"thumb_01_{s}")).normalized;
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
                        Add($"{finger}_0{k + 1}_{s}", Knuckles[k]);
                    }
                }

                Add($"thumb_01_{s}", 0.1f, 1.05f);
                Add($"thumb_02_{s}", 0.45f);
                Add($"thumb_03_{s}", 0.55f);
            }
        }

        /// <summary>
        /// For the checks: the finger joints found, the curl now, and for each hand how far its index and middle
        /// fingers have come round toward the palm (1 straight out from the wrist, -1 folded back to it) and to
        /// which side of the palm's plane; and where the thumb's last joint lies.
        /// </summary>
        public string HandReport()
        {
            var text = $"fingers {fingers.Count}, grip {rig.Grip:0.00}, curl {Mathf.Max(0.04f, CurlRest + rig.Grip * FistCurl):0.00}";

            foreach (var s in new[] { "l", "r" })
            {
                Vector3 At(string name) => PositionInFigure(Bone(name));
                var wrist = At($"hand_{s}");
                var along = (At($"middle_01_{s}") - wrist).normalized;
                var palm = Vector3.Cross(along, At($"pinky_01_{s}") - At($"index_01_{s}")).normalized;

                if (Vector3.Dot(palm, At($"thumb_03_{s}") - wrist) < 0)
                {
                    palm = -palm;
                }

                foreach (var finger in new[] { "index", "middle" })
                {
                    var tip = (At($"{finger}_03_{s}") - At($"{finger}_01_{s}")).normalized;
                    text += $"; {finger}_{s}: along {Vector3.Dot(tip, along):0.00}, palm side {Vector3.Dot(tip, palm):0.00}";
                }

                var acrossPalm = (At($"pinky_01_{s}") - At($"index_01_{s}")).normalized;
                var thumbTip = At($"thumb_03_{s}") - At($"index_01_{s}");
                text += $"; thumb_{s}: toward the little finger {Vector3.Dot(thumbTip, acrossPalm) * 100:0.0} cm, before the palm {Vector3.Dot(thumbTip, palm) * 100:0.0} cm";
            }

            return text;
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
