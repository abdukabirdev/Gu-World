using System;
using System.Collections.Generic;
using System.Text;
using GuWorld.Core.Gen.Flora;

namespace GuWorld.Core.Gen.People
{
    /// <summary>
    /// Makes people. A few bases (a man, a woman; a child, a youth, a grown-up, an elder) are built
    /// on the hero's own skeleton, so the hero's animations move them all; from the seed come the
    /// body's proportions (height, legs, arms, shoulders, hips, girth), the face (its width, jaw,
    /// chin, nose, eyes, brows, mouth, ears, the skin's tone), the hair (eleven styles, its colour,
    /// a beard) and the clothes — a robe or a jacket and trousers or a short tunic, sleeves narrow or
    /// wide, a collar crossed left over right with its trim, a sash, shoes or sandals or bare feet, a
    /// hat or a cap or a headband, an apron, armour, a basket — in colours taken from the clan's,
    /// the trade's and the purse's palettes, and woven into the vertices: the weave's grain,
    /// stripes, embroidery, patches, the dirt of the fields at the hem. Every part is skinned to the
    /// bones near it. The same spec always makes the same person.
    /// </summary>
    public static partial class PersonGen
    {
        static readonly Dictionary<string, int> boneIndex = new Dictionary<string, int>();
        static readonly int[] mainChild;

        static PersonGen()
        {
            var bones = Skeleton.Bones;

            for (var i = 0; i < bones.Length; i++)
            {
                boneIndex[bones[i].Name] = i;
            }

            mainChild = new int[bones.Length];

            for (var i = 0; i < bones.Length; i++)
            {
                mainChild[i] = -1;
            }

            // The child a bone points to: the one named next in its chain, else its only child.
            string Next(string name)
            {
                switch (name)
                {
                    case "hips": return "spine";
                    case "spine": return "spine1";
                    case "spine1": return "spine2";
                    case "spine2": return "chest";
                    case "chest": return "neck";
                    case "neck": return "neck1";
                    case "neck1": return "head";
                }

                foreach (var side in new[] { "_L", "_R" })
                {
                    if (!name.EndsWith(side))
                    {
                        continue;
                    }

                    var stem = name.Substring(0, name.Length - 2);

                    switch (stem)
                    {
                        case "shoulder": return "upperArm" + side;
                        case "upperArm": return "forearm" + side;
                        case "forearm": return "hand" + side;
                        case "hand": return "middle1" + side;
                        case "thigh": return "shin" + side;
                        case "shin": return "foot" + side;
                    }
                }

                return null;
            }

            for (var i = 0; i < bones.Length; i++)
            {
                var next = Next(bones[i].Name);

                if (next != null && boneIndex.TryGetValue(next, out var child))
                {
                    mainChild[i] = child;
                    continue;
                }

                var only = -1;
                var count = 0;

                for (var j = 0; j < bones.Length; j++)
                {
                    if (bones[j].Parent == i)
                    {
                        only = j;
                        count++;
                    }
                }

                if (count == 1)
                {
                    mainChild[i] = only;
                }
            }
        }

        public static int Bone(string name) => boneIndex.TryGetValue(name, out var i) ? i : -1;

        // ------------------------------------------------------------------ The body's numbers

        sealed class Body
        {
            public float Height;       // metres
            public float LegK = 1, ArmK = 1, TorsoK = 1;
            public float ShoulderK = 1, HipK = 1, WaistK = 1, ChestK = 1, DepthK = 1;
            public float HeadK = 1;
            public float Girth;        // 0 thin .. 1 heavy
            public float Muscle;       // 0 .. 1
            public float Bust;         // 0 .. 1 (women)
            public float Stoop;        // an elder's rounded back, in the mesh
            public Tint Skin;
            public Tint Lips;
            public Tint Hair;
            public Tint Eyes;
            // The face, each about 1 for the average.
            public float FaceWidth = 1, Jaw = 1, Chin = 1, Nose = 1, NoseWidth = 1, EyeSize = 1, EyeGap = 1, BrowTilt, BrowThick = 1, Mouth = 1, Ears = 1, Cheek = 1;
        }

        const float HeroHeight = 1.765f;

        static Body ChooseBody(PersonSpec spec, ref Rng rng, StringBuilder words)
        {
            var b = new Body();
            var female = spec.Sex == Sex.Female;

            switch (spec.Age)
            {
                case AgeGroup.Child:
                    b.Height = rng.Range(1.05f, 1.38f);
                    b.HeadK = 1.32f - (b.Height - 1.05f) * 0.5f;
                    b.LegK = 0.92f;
                    b.ShoulderK = 0.86f;
                    b.HipK = 0.9f;
                    break;
                case AgeGroup.Teen:
                    b.Height = female ? rng.Range(1.5f, 1.64f) : rng.Range(1.58f, 1.76f);
                    b.HeadK = 1.04f;
                    b.ShoulderK = female ? 0.84f : 0.93f;
                    break;
                case AgeGroup.Elder:
                    b.Height = female ? rng.Range(1.45f, 1.6f) : rng.Range(1.56f, 1.74f);
                    b.Stoop = rng.Range(0.3f, 1f);
                    b.ShoulderK = female ? 0.86f : 0.95f;
                    break;
                default:
                    b.Height = female ? rng.Range(1.5f, 1.68f) : rng.Range(1.62f, 1.84f);
                    b.ShoulderK = female ? 0.86f : rng.Range(0.96f, 1.1f);
                    break;
            }

            b.Girth = Noise.Clamp01(rng.Range(-0.2f, 0.8f) * (spec.Wealth > 0.6f ? 1.3f : 1) + (spec.Age == AgeGroup.Elder ? 0.1f : 0));
            b.Muscle = spec.Role == "guard" || spec.Role == "smith" || spec.Role == "porter" || spec.Role == "bandit" ? rng.Range(0.5f, 1f) : rng.Range(0f, 0.5f);

            if (spec.Age == AgeGroup.Child)
            {
                b.Girth *= 0.5f;
                b.Muscle = 0;
            }

            b.LegK *= rng.Range(0.95f, 1.05f);
            b.ArmK = rng.Range(0.95f, 1.05f);
            b.TorsoK = rng.Range(0.96f, 1.04f);
            b.HipK = b.HipK * (female ? rng.Range(1.06f, 1.18f) : rng.Range(0.94f, 1.04f)) * (1 + 0.12f * b.Girth);
            b.WaistK = (female ? rng.Range(0.8f, 0.9f) : rng.Range(0.92f, 1.0f)) * (1 + 0.45f * b.Girth);
            b.ChestK = (female ? 0.92f : 1) * (1 + 0.12f * b.Muscle + 0.12f * b.Girth);
            b.ShoulderK *= 1 + 0.08f * b.Muscle;
            b.DepthK = 1 + 0.25f * b.Girth;
            b.Bust = female && spec.Age >= AgeGroup.Teen ? rng.Range(0.3f, 1f) * (spec.Age == AgeGroup.Teen ? 0.6f : 1) : 0;

            // The face.
            b.FaceWidth = rng.Range(0.9f, 1.1f) * (female ? 0.95f : 1);
            b.Jaw = female || spec.Age == AgeGroup.Child ? rng.Range(0.75f, 0.95f) : rng.Range(0.9f, 1.15f);
            b.Chin = rng.Range(0.8f, 1.2f);
            b.Nose = rng.Range(0.8f, 1.2f) * (spec.Age == AgeGroup.Child ? 0.75f : 1);
            b.NoseWidth = rng.Range(0.85f, 1.2f);
            b.EyeSize = rng.Range(0.85f, 1.15f) * (spec.Age == AgeGroup.Child ? 1.2f : 1);
            b.EyeGap = rng.Range(0.92f, 1.08f);
            b.BrowTilt = rng.Range(-12f, 14f);
            b.BrowThick = female ? rng.Range(0.6f, 0.9f) : rng.Range(0.9f, 1.4f);
            b.Mouth = rng.Range(0.85f, 1.15f);
            b.Ears = rng.Range(0.85f, 1.2f);
            b.Cheek = rng.Range(0.9f, 1.15f);

            // Colours: skin from fair to sun-dark (those who work outside darker), lips, eyes, hair.
            var outdoors = spec.Role == "farmer" || spec.Role == "porter" || spec.Role == "guard" || spec.Role == "bandit" || spec.Role == "watch" ? 0.35f : 0;
            var tan = Noise.Clamp01(rng.Range(0f, 0.55f) + outdoors);
            b.Skin = Tint.Mix(Tint.Hex(0xecccb0), Tint.Hex(0xa87a58), tan);
            b.Skin = Build.Shade(b.Skin, rng.Range(0.94f, 1.04f));
            b.Lips = Tint.Mix(b.Skin, Tint.Hex(0x9a4a44), female ? rng.Range(0.45f, 0.75f) : rng.Range(0.25f, 0.4f));
            b.Eyes = Tint.Mix(Tint.Hex(0x2a1a12), Tint.Hex(0x5a3a22), rng.Value());

            var grey = spec.Age == AgeGroup.Elder ? rng.Range(0.55f, 1f) : spec.Age == AgeGroup.Adult && rng.Chance(0.12f) ? rng.Range(0.15f, 0.4f) : 0;
            var hair = Tint.Mix(Tint.Hex(0x0e0c0c), Tint.Hex(0x3a2618), rng.Value() * (spec.Clan == "xiong" ? 1 : 0.6f));

            if (spec.Clan == "bai" && rng.Chance(0.25f))
            {
                // A few of the Bai go white young, as the clan's name says.
                grey = Math.Max(grey, rng.Range(0.6f, 0.95f));
            }

            b.Hair = Tint.Mix(hair, Tint.Hex(0xd8d6d2), grey);
            words.Append($"{b.Height:0.00} m, girth {b.Girth:0.0}, muscle {b.Muscle:0.0}; ");

            return b;
        }

        // ------------------------------------------------------------------ The skeleton, proportioned

        static V3[] Proportion(Body b)
        {
            var bones = Skeleton.Bones;
            var at = new V3[bones.Length];
            var hips = bones[Bone("hips")].At;
            var cx = hips.X;
            var k = b.Height / HeroHeight / ((b.LegK * hips.Y + (HeroHeight - hips.Y) * b.TorsoK * (0.82f + 0.18f * b.HeadK)) / HeroHeight);
            var upperArm = new[] { Bone("upperArm_L"), Bone("upperArm_R") };
            var head = Bone("head");

            bool Under(int i, int top)
            {
                for (var p = bones[i].Parent; p >= 0; p = bones[p].Parent)
                {
                    if (p == top)
                    {
                        return true;
                    }
                }

                return false;
            }

            for (var i = 0; i < bones.Length; i++)
            {
                var p = bones[i].At;
                var name = bones[i].Name;
                var side = name.EndsWith("_L") || name.EndsWith("_R");
                var leg = name.StartsWith("thigh") || name.StartsWith("shin") || name.StartsWith("foot") || name.StartsWith("knee") || name.StartsWith("toe") || name.StartsWith("hipHelper") || name.StartsWith("skirt");
                var arm = side && !leg;

                if (arm && (Under(i, upperArm[0]) || Under(i, upperArm[1])))
                {
                    // Down the arm: each joint from its parent's new place, the arm's own length.
                    var parent = bones[i].Parent;
                    at[i] = at[parent] + (p - bones[parent].At) * (k * b.ArmK);
                    continue;
                }

                if (Under(i, head))
                {
                    at[i] = at[head] + (p - bones[head].At) * (k * b.HeadK);
                    continue;
                }

                var y = p.Y < hips.Y ? p.Y * b.LegK : hips.Y * b.LegK + (p.Y - hips.Y) * b.TorsoK;
                var width = arm ? b.ShoulderK : leg ? b.HipK : 1;
                at[i] = new V3(cx + (p.X - cx) * k * width, y * k, p.Z * k * (leg || arm ? 1 : b.DepthK));
            }

            return at;
        }

        /// <summary>Where the end of a bone is: its main child, or a stretch past it for the ends of chains.</summary>
        static V3 Tail(V3[] at, int i)
        {
            if (mainChild[i] >= 0)
            {
                return at[mainChild[i]];
            }

            var name = Skeleton.Bones[i].Name;
            var parent = Skeleton.Bones[i].Parent;
            var dir = parent >= 0 ? (at[i] - at[parent]).Normalized : V3.Up;

            if (name == "head")
            {
                return at[i] + V3.Up * 0.2f;
            }

            if (name.StartsWith("foot"))
            {
                return at[i] + new V3(0, -0.07f, 0.15f);
            }

            return at[i] + dir * 0.05f;
        }

        // ------------------------------------------------------------------ The skin

        /// <summary>A mesh with its skin weights, built part by part.</summary>
        sealed class Skinned
        {
            public readonly MeshData Mesh = new MeshData();
            public readonly List<int> Joints = new List<int>();
            public readonly List<float> Weights = new List<float>();
            public V3[] At;

            /// <summary>Weights for the vertices made since `from`: each by its nearness to the listed bones (the nearest four).</summary>
            public void Skin(int from, params string[] bones)
            {
                var ids = new List<int>();

                foreach (var name in bones)
                {
                    var i = Bone(name);

                    if (i >= 0)
                    {
                        ids.Add(i);
                    }
                }

                var heads = new V3[ids.Count];
                var tails = new V3[ids.Count];

                for (var k = 0; k < ids.Count; k++)
                {
                    heads[k] = At[ids[k]];
                    tails[k] = Tail(At, ids[k]);
                }

                var w = new float[ids.Count];

                for (var v = from; v < Mesh.VertexCount; v++)
                {
                    var p = new V3(Mesh.Positions[v * 3], Mesh.Positions[v * 3 + 1], Mesh.Positions[v * 3 + 2]);

                    for (var k = 0; k < ids.Count; k++)
                    {
                        var d = SegmentDistance(p, heads[k], tails[k]);
                        var dd = d * d + 0.0004f;
                        w[k] = 1 / (dd * dd);
                    }

                    Keep4(w, ids);
                }
            }

            /// <summary>The vertices made since `from` all on one bone.</summary>
            public void Rigid(int from, string bone)
            {
                var i = Math.Max(0, Bone(bone));

                for (var v = from; v < Mesh.VertexCount; v++)
                {
                    Joints.Add(i);
                    Joints.Add(0);
                    Joints.Add(0);
                    Joints.Add(0);
                    Weights.Add(1);
                    Weights.Add(0);
                    Weights.Add(0);
                    Weights.Add(0);
                }
            }

            void Keep4(float[] w, List<int> ids)
            {
                var best = new[] { -1, -1, -1, -1 };

                for (var slot = 0; slot < 4; slot++)
                {
                    var top = -1;

                    for (var k = 0; k < w.Length; k++)
                    {
                        if (Array.IndexOf(best, k) >= 0)
                        {
                            continue;
                        }

                        if (top < 0 || w[k] > w[top])
                        {
                            top = k;
                        }
                    }

                    best[slot] = top;
                }

                var sum = 0f;

                for (var slot = 0; slot < 4; slot++)
                {
                    sum += best[slot] >= 0 ? w[best[slot]] : 0;
                }

                for (var slot = 0; slot < 4; slot++)
                {
                    var b = best[slot];
                    var weight = b >= 0 && sum > 0 ? w[b] / sum : 0;

                    // Slivers of weight are dropped (and the rest stays summed to one below).
                    Joints.Add(b >= 0 ? ids[b] : 0);
                    Weights.Add(weight < 0.02f ? 0 : weight);
                }

                var at = Weights.Count - 4;
                var total = Weights[at] + Weights[at + 1] + Weights[at + 2] + Weights[at + 3];

                for (var slot = 0; slot < 4; slot++)
                {
                    Weights[at + slot] = total > 0 ? Weights[at + slot] / total : slot == 0 ? 1 : 0;
                }
            }
        }

        static float SegmentDistance(V3 p, V3 a, V3 b)
        {
            var ab = b - a;
            var t = Noise.Clamp01(V3.Dot(p - a, ab) / Math.Max(1e-6f, V3.Dot(ab, ab)));

            return (p - (a + ab * t)).Length;
        }

        // ------------------------------------------------------------------ Lofted surfaces

        /// <summary>
        /// A surface swept through rings (each a centre, two half-widths along `side` and the
        /// ring's other cross axis, and a share 0-1 of a squarer section), `sides` round, its ends
        /// closed if asked. `shape(ring, angle 0-1)` can push the radius out (a bust, a nose of
        /// cloth); `colour(ring, angle 0-1, point)` paints it.
        /// </summary>
        static void Loft(MeshData mesh, IList<V3> centres, IList<float> a, IList<float> c, V3 side, int sides, Func<int, float, Tint, V3, Tint> colour, Tint baseColour,
            bool capStart = false, bool capEnd = false, Func<int, float, float> shape = null, float square = 0)
        {
            var count = centres.Count;
            var start = mesh.VertexCount;

            for (var i = 0; i < count; i++)
            {
                var next = i < count - 1 ? centres[i + 1] - centres[i] : centres[i] - centres[i - 1];
                var before = i > 0 ? centres[i] - centres[i - 1] : next;
                var axis = (next.Normalized + before.Normalized).Normalized;
                var u = (side - axis * V3.Dot(side, axis)).Normalized;
                var v = V3.Cross(u, axis);

                for (var s = 0; s <= sides; s++)
                {
                    var t = s / (float)sides;
                    var angle = t * Build.Tau;
                    var cos = (float)Math.Cos(angle);
                    var sin = (float)Math.Sin(angle);

                    if (square > 0)
                    {
                        // Towards a rounded square: the corners pushed out.
                        var k = 1 + square * 0.3f * (float)Math.Abs(Math.Sin(2 * angle));
                        cos *= k;
                        sin *= k;
                    }

                    var push = shape != null ? shape(i, t) : 0;
                    var p = centres[i] + u * (cos * (a[i] + push)) + v * (sin * (c[i] + push));
                    var col = colour(i, t, baseColour, p);
                    mesh.Vertex(p.X, p.Y, p.Z, t, i / (float)Math.Max(1, count - 1), col.R, col.G, col.B);
                }
            }

            var ring = sides + 1;

            for (var i = 0; i < count - 1; i++)
            {
                for (var s = 0; s < sides; s++)
                {
                    var p0 = start + i * ring + s;
                    var p1 = start + (i + 1) * ring + s;
                    mesh.Triangle(p0, p1, p1 + 1);
                    mesh.Triangle(p0, p1 + 1, p0 + 1);
                }
            }

            void Cap(int i, bool end)
            {
                var c0 = centres[i];
                var col = colour(i, 0, baseColour, c0);
                var centre = mesh.Vertex(c0.X, c0.Y, c0.Z, 0.5f, 0.5f, col.R, col.G, col.B);

                for (var s = 0; s < sides; s++)
                {
                    var p0 = start + i * ring + s;

                    if (end)
                    {
                        mesh.Triangle(p0, centre, p0 + 1);
                    }
                    else
                    {
                        mesh.Triangle(p0, p0 + 1, centre);
                    }
                }
            }

            if (capStart)
            {
                Cap(0, false);
            }

            if (capEnd)
            {
                Cap(count - 1, true);
            }
        }

        /// <summary>A loft along a chain of joints with radii at each, subdivided `steps` times between them.</summary>
        static void Limb(MeshData mesh, IList<V3> joints, IList<float> radii, float flat, V3 side, int sides, int steps, Func<int, float, Tint, V3, Tint> colour, Tint baseColour,
            bool capStart = false, bool capEnd = true, IList<V3> sag = null)
        {
            var centres = new List<V3>();
            var a = new List<float>();
            var c = new List<float>();

            for (var j = 0; j < joints.Count - 1; j++)
            {
                for (var s = 0; s < steps; s++)
                {
                    var t = s / (float)steps;
                    var smooth = t * t * (3 - 2 * t);
                    var r = Build.Lerp(radii[j], radii[j + 1], smooth);
                    var drop = sag != null ? V3.Lerp(sag[j], sag[j + 1], t) : new V3(0, 0, 0);
                    centres.Add(V3.Lerp(joints[j], joints[j + 1], t) + drop);
                    a.Add(r);
                    c.Add(r * flat);
                }
            }

            centres.Add(joints[joints.Count - 1] + (sag != null ? sag[sag.Count - 1] : new V3(0, 0, 0)));
            a.Add(radii[radii.Count - 1]);
            c.Add(radii[radii.Count - 1] * flat);
            Loft(mesh, centres, a, c, side, sides, colour, baseColour, capStart, capEnd);
        }

        static Tint Plain(int ring, float angle, Tint colour, V3 p) => colour;
    }
}
