using System;
using System.Collections.Generic;

namespace GuWorld.Core.Gen.Flora
{
    /// <summary>What a tree is asked to be, beyond its kind and its seed. Zeroes mean "as the kind has it".</summary>
    public struct TreeParams
    {
        /// <summary>Its height in metres (0: anywhere in its kind's range, by the seed).</summary>
        public float Height;
        /// <summary>How far it leans, in degrees, toward +X of its own space (the wind's side, the open side, downhill).</summary>
        public float Lean;
        /// <summary>How crowded it grew: 0 alone in the open (wide, low crown), 1 in a dense wood (narrow, high crown).</summary>
        public float Crowding;
        /// <summary>0 young (slim, smooth) to 1 old (thick, burled, buttressed, mossed).</summary>
        public float Age;
        /// <summary>Added to its kind's share of turned leaves (autumn, blossom), −1 to 1.</summary>
        public float Season;
        /// <summary>How wet its place is, −1 (dry: no moss, no lichen) to 1 (dripping: moss to the limbs, beards of lichen).</summary>
        public float Wet;
        /// <summary>How full its foliage is: 0 as its kind, down to −1 bare, up to 1 half again as thick.</summary>
        public float Foliage;
        /// <summary>How far gone it is: 0 sound, 1 half its limbs dead stubs, fungus up its trunk.</summary>
        public float Decay;
    }

    /// <summary>One material's share of a tree: "bark:&lt;texture&gt;", "leaf:&lt;texture&gt;", "extra" (coloured in its vertices) or "glow".</summary>
    public sealed class TreePart
    {
        public string Material;
        public readonly MeshData Mesh = new MeshData();
    }

    /// <summary>One level of detail of a tree: its parts by material.</summary>
    public sealed class TreeLod
    {
        public readonly List<TreePart> Parts = new List<TreePart>();

        public int Triangles
        {
            get
            {
                var n = 0;

                foreach (var part in Parts)
                {
                    n += part.Mesh.TriangleCount;
                }

                return n;
            }
        }

        public MeshData Part(string material)
        {
            foreach (var part in Parts)
            {
                if (part.Material == material)
                {
                    return part.Mesh;
                }
            }

            var made = new TreePart { Material = material };
            Parts.Add(made);

            return made.Mesh;
        }
    }

    /// <summary>A limb thick enough to stand on or climb along: its centre line and its radius at each point.</summary>
    public sealed class TreeLimb
    {
        public V3[] Points;
        public float[] Radii;
        /// <summary>0 the trunk, 1 a limb off it, 2 a branch off a limb.</summary>
        public int Depth;
    }

    /// <summary>A grown tree: its meshes from near to far, its solid shape, and the numbers the game places and climbs it by.</summary>
    public sealed class TreeModel
    {
        public TreeSpecies Species;
        public int Seed;
        public TreeParams Asked;
        public float Height;
        /// <summary>The trunk's radius above its flare.</summary>
        public float TrunkRadius;
        /// <summary>The foot's radius at the ground, buttresses and all.</summary>
        public float FootRadius;
        public float CrownBase;
        public float CrownRadius;
        public V3 CrownCentre;
        public V3 Top;
        /// <summary>Levels of detail, near to far: 0 all of it, 1 lighter, 2 the far woods, 3 the horizon.</summary>
        public TreeLod[] Lods;
        /// <summary>The solid shape (trunk, roots, limbs thick enough to walk): a plain mesh for the engine's collider.</summary>
        public MeshData Collision;
        /// <summary>The trunk and the limbs one can climb, for the climber.</summary>
        public List<TreeLimb> Climb = new List<TreeLimb>();
        public int Limbs, Cards;
    }

    /// <summary>
    /// Grows the mountain's great trees — up to a hundred metres and more — from a kind
    /// (<see cref="TreeSpecies"/>), a seed and what is asked of them (<see cref="TreeParams"/>).
    ///
    /// The bole stands on buttresses that run out into roots over the ground, swells with burls,
    /// wanders and leans; limbs thick enough to walk leave it through collars, in a spiral or in
    /// whorls, their length by the crown's shape; branches and twigs leave the limbs. The wood is
    /// skinned with the kind's tiling bark (a height map with it), moss laid by a weight in the
    /// vertices: over the foot, the roots and the limbs' backs, more where it is wet. The foliage
    /// is the kind's sprays painted with alpha, set on the twigs as crossed, curved cards (pads
    /// for pines, fronds for firs, curtains for willows). On it live shelves of bracket fungus,
    /// beards of lichen, vines leafed with ivy, and — on the ancient ones — glowing moss.
    ///
    /// Four levels of detail come from the one skeleton, with a solid shape for the engine and the
    /// climbable limbs for the climber. The same kind, seed and asking always grow the same tree.
    /// Y up, metres, the foot at the origin.
    /// </summary>
    public static class TreeGen
    {
        sealed class TrunkShape
        {
            public float Flare, FlareHeight, Twist;
            public float[] Angle = new float[0], Amp = new float[0], Width = new float[0];
            public readonly List<(float y, float a, float size, float amp)> Burls = new List<(float, float, float, float)>();
            /// <summary>Bamboo's joints: a ring proud of the culm every so many metres.</summary>
            public float Nodes;

            /// <summary>How far the surface stands out at a height and an angle, as a multiple of the trunk's radius there.</summary>
            public float Mul(float y, float theta, bool burls)
            {
                var f = Flare * (float)Math.Exp(-Math.Max(0, y) / Math.Max(0.1f, FlareHeight));
                var ridge = 0f;

                for (var i = 0; i < Angle.Length; i++)
                {
                    var d = Wrap(theta - Angle[i] - Twist * y);
                    // A fin with a rounded crest: sharper than a bell, so the hollows between are deep.
                    var x = Math.Abs(d) / Width[i];
                    ridge = Math.Max(ridge, Amp[i] * (float)Math.Exp(-x * x * 0.5f - x * 0.9f));
                }

                var m = 1 + f * (0.04f + 1.1f * ridge);

                if (burls)
                {
                    foreach (var (by, ba, size, amp) in Burls)
                    {
                        var dy = (y - by) / size;
                        var da = Wrap(theta - ba) / (size * 0.9f);
                        m += amp * (float)Math.Exp(-(dy * dy + da * da));
                    }
                }

                if (Nodes > 0)
                {
                    var phase = y / Nodes;
                    var near = Math.Abs(phase - (float)Math.Round(phase)) * Nodes;
                    m += 0.14f * (float)Math.Exp(-near * near / 0.0012f);
                }

                return m;
            }

            static float Wrap(float a)
            {
                while (a > Math.PI)
                {
                    a -= Build.Tau;
                }

                while (a < -Math.PI)
                {
                    a += Build.Tau;
                }

                return a;
            }
        }

        sealed class Limb
        {
            public readonly List<V3> P = new List<V3>();
            public readonly List<float> R = new List<float>();
            public int Depth;
            public float Length;
            public bool Root, Dead, Snapped;
            public TrunkShape Shape;
            public float Phase;
            float[] along;

            void Measure()
            {
                along = new float[P.Count];

                for (var i = 1; i < P.Count; i++)
                {
                    along[i] = along[i - 1] + (P[i] - P[i - 1]).Length;
                }
            }

            public void Changed() => along = null;

            /// <summary>The point a share of the way along (by length), the heading and the radius there.</summary>
            public V3 At(float t, out V3 dir, out float radius)
            {
                if (along == null)
                {
                    Measure();
                }

                var want = Build.Clamp01(t) * along[P.Count - 1];
                var i = 0;

                while (i < P.Count - 2 && along[i + 1] < want)
                {
                    i++;
                }

                var span = Math.Max(1e-5f, along[i + 1] - along[i]);
                var k = Build.Clamp01((want - along[i]) / span);
                dir = (P[i + 1] - P[i]).Normalized;
                radius = Build.Lerp(R[i], R[i + 1], k);

                return V3.Lerp(P[i], P[i + 1], k);
            }

            public float Total
            {
                get
                {
                    if (along == null)
                    {
                        Measure();
                    }

                    return along[P.Count - 1];
                }
            }

            public float AlongAt(int i)
            {
                if (along == null)
                {
                    Measure();
                }

                return along[i];
            }
        }

        enum CardKind { Spray, Pad, Frond, Hang, Scale }

        struct Anchor
        {
            public V3 At, Dir;
            public float Size;
            public uint Seed;
            public string Tex;
            public CardKind Kind;
            public Tint Tint;
        }

        /// <summary>Everything about one tree while it is grown.</summary>
        sealed class Work
        {
            public TreeSpecies Species;
            public TreeParams Asked;
            public Noise Noise;
            public float Height, R0, Age, Crowd, Wet, Decay, Foliage;
            public readonly List<Limb> Limbs = new List<Limb>();
            public readonly List<Anchor> Anchors = new List<Anchor>();
            public readonly List<(V3 at, V3 outward, float size, float turn)> Fungus = new List<(V3, V3, float, float)>();
            public readonly List<(List<V3> path, float radius)> Vines = new List<(List<V3>, float)>();
            public readonly List<(V3 at, float size)> Glow = new List<(V3, float)>();
            public Limb Main;
            public V3 CrownCentre;
            public float CrownRadius = 1, CrownLow;
        }

        public static TreeModel Grow(TreeSpecies species, int seed, TreeParams asked)
        {
            var rng = new Rng(seed * 7919 + Noise.Stable(species.Id));
            var w = new Work
            {
                Species = species, Asked = asked, Noise = new Noise(seed * 31 + 5),
                Age = Build.Clamp01(asked.Age), Crowd = Build.Clamp01(asked.Crowding), Decay = Build.Clamp01(asked.Decay),
                Wet = Math.Max(0, Math.Min(1.3f, species.Moss * (1 + asked.Wet) + Math.Max(0, asked.Wet) * 0.25f)),
                Foliage = Math.Max(0, 1 + Math.Max(-1, Math.Min(1, asked.Foliage)) * (asked.Foliage > 0 ? 0.5f : 1)),
            };
            w.Height = asked.Height > 0 ? asked.Height : rng.Range(species.MinHeight, species.MaxHeight);
            w.R0 = w.Height * species.Girth * rng.Range(0.9f, 1.12f) * (0.82f + 0.36f * w.Age) * (1 - 0.12f * w.Crowd);
            var trunks = rng.Range(species.MinTrunks, species.MaxTrunks + 1);
            var clump = species.MaxTrunks > 4;
            var slope = (float)Math.Tan(Math.Min(60, Math.Abs(asked.Lean)) * Math.PI / 180) * Math.Sign(asked.Lean);

            for (var k = 0; k < trunks; k++)
            {
                // Its own lean: a little of the kind's, any way (the asked lean is laid over the grown tree).
                var own = rng.Range(0f, species.Lean) * (Math.Abs(asked.Lean) > 1 ? 0.4f : 1) * (float)Math.PI / 180;
                var way = rng.Range(0f, Build.Tau);
                var off = new V3((float)Math.Cos(way), 0, (float)Math.Sin(way));
                var lean = off * (float)Math.Tan(own);
                var at = new V3(0, 0, 0);
                var share = 1f;
                var radius = w.R0;

                if (clump)
                {
                    // A clump (bamboo): stems scattered over a patch, arching outward.
                    var far = (0.4f + 1.1f * (1 - w.Crowd)) * (float)Math.Sqrt(rng.Value()) * (0.6f + w.Height / 20f);
                    at = off * far;
                    lean = off * (float)Math.Tan(rng.Range(2f, 11f) * Math.PI / 180);
                    share = rng.Range(0.62f, 1f);
                    radius = Math.Max(0.03f, w.R0 * rng.Range(0.75f, 1.2f));
                }
                else if (k > 0)
                {
                    // A second stem from the same root, leaning away from the first.
                    at = off * w.R0 * 1.3f;
                    lean = lean + off * (float)Math.Tan(rng.Range(9f, 20f) * Math.PI / 180);
                    share = rng.Range(0.65f, 0.9f);
                    radius = w.R0 * rng.Range(0.6f, 0.82f);
                }

                var snapped = rng.Chance(Math.Min(0.95f, species.Broken + w.Decay * 0.3f));
                var length = w.Height * share * (snapped ? rng.Range(0.4f, 0.78f) : 1);
                var trunk = Trunk(w, at, (V3.Up + lean).Normalized, length, radius, snapped, k == 0 && !clump, ref rng);
                w.Limbs.Add(trunk);
                w.Main = w.Main ?? trunk;
                Limbs(w, trunk, length / (w.Height * share), clump, ref rng);

                if (!clump && k == 0)
                {
                    Roots(w, trunk, ref rng);
                }
            }

            Lift(w);
            Fit(w);
            Shear(w, slope);
            Foliage(w, ref rng);
            Lichen(w, ref rng);
            Extras(w, ref rng);

            var model = new TreeModel
            {
                Species = species, Seed = seed, Asked = asked, Height = w.Height, Top = w.Main.P[w.Main.P.Count - 1], Limbs = w.Limbs.Count, Cards = w.Anchors.Count,
                CrownCentre = w.CrownCentre, CrownRadius = w.CrownRadius, CrownBase = w.CrownLow,
            };
            w.Main.At(Math.Min(0.5f, (w.R0 * species.ButtressHeight * 2.5f + 1) / Math.Max(1, w.Height)), out _, out model.TrunkRadius);
            model.FootRadius = w.Main.R[1] * w.Main.Shape.Mul(0, 0, false) * 1.25f;
            model.Lods = new[] { Lod(w, 0), Lod(w, 1), Lod(w, 2), Lod(w, 3) };
            model.Collision = Solid(w, model);

            return model;
        }

        // ------------------------------------------------------------------ The skeleton

        static Limb Trunk(Work w, V3 at, V3 dir, float length, float radius, bool snapped, bool main, ref Rng rng)
        {
            var species = w.Species;
            var trunk = new Limb { Depth = 0, Length = length, Snapped = snapped, Phase = rng.Range(0f, 100f) };
            var shape = new TrunkShape
            {
                Flare = species.Flare * (0.7f + 0.6f * w.Age) * (main ? 1 : 0.4f),
                FlareHeight = Math.Max(0.3f, species.ButtressHeight * radius * (0.8f + 0.5f * w.Age)),
                Twist = rng.Range(-0.04f, 0.04f),
                Nodes = species.Id == "bamboo" ? Math.Max(0.35f, length / 34f) : 0,
            };
            var ridges = main ? Math.Max(0, species.Buttresses + rng.Range(-1, 2)) : 0;
            shape.Angle = new float[ridges];
            shape.Amp = new float[ridges];
            shape.Width = new float[ridges];

            for (var i = 0; i < ridges; i++)
            {
                shape.Angle[i] = i * Build.Tau / ridges + rng.Range(-0.3f, 0.3f);
                shape.Amp[i] = rng.Range(0.65f, 1.3f);
                shape.Width[i] = rng.Range(0.15f, 0.26f) * (6f / Math.Max(4, ridges));
            }

            if (main)
            {
                var burls = (int)Math.Round(species.Burls * (0.3f + 1.1f * w.Age) * rng.Range(0.6f, 1.3f));

                for (var i = 0; i < burls; i++)
                {
                    shape.Burls.Add((rng.Range(0.03f, 0.55f) * length, rng.Range(0f, Build.Tau), rng.Range(0.45f, 1.3f) * (0.6f + radius * 0.5f), rng.Range(0.08f, 0.3f)));
                }
            }

            trunk.Shape = shape;
            // Rings close together through the foot's flare, farther apart above.
            var rings = Math.Max(14, Math.Min(70, (int)(length / 1.2f) + 12));
            var p = at + new V3(0, -1.2f, 0);
            trunk.P.Add(p);
            trunk.R.Add(radius);
            p = at;
            var wander = species.Wander * (1 + w.Age * 0.5f);
            var first = dir;
            var last = 0f;

            for (var s = 0; s <= rings; s++)
            {
                var t = (float)Math.Pow(s / (float)rings, 1.5f);
                p = p + dir * ((t - last) * length);
                last = t;
                trunk.P.Add(p);
                var taper = snapped ? 1 - 0.5f * t : (float)Math.Pow(Math.Max(0.004f, 1 - 0.985f * t), species.Taper);
                trunk.R.Add(Math.Max(0.02f, radius * taper));
                var wobble = new V3(w.Noise.Perlin(t * 3.1f + trunk.Phase, 1.7f), 0, w.Noise.Perlin(4.3f, t * 3.1f + trunk.Phase)) * wander;
                // The wander is held near the first heading; a leaning stem bows back up to the light.
                dir = (dir * 0.8f + first * 0.2f + wobble + V3.Up * (0.05f * t)).Normalized;
            }

            return trunk;
        }

        /// <summary>A limb's length up the crown (tc 0 at the crown's foot, 1 at its top), 0-1.</summary>
        static float Profile(CrownShape shape, float tc)
        {
            switch (shape)
            {
                case CrownShape.Cone: return 0.1f + 0.9f * (float)Math.Pow(1 - tc, 0.85f);
                case CrownShape.Column: return 0.55f + 0.45f * (float)Math.Sin(Math.PI * Math.Min(1, tc * 0.9f + 0.15f));
                case CrownShape.Tiers: return 0.38f + 0.62f * (float)Math.Pow(1 - tc, 0.7f);
                case CrownShape.Vase: return 0.6f + 0.4f * (float)Math.Sin(Math.PI * (0.2f + 0.6f * tc));
                default: return 0.32f + 0.68f * (float)Math.Sin(Math.PI * (0.16f + 0.74f * tc));
            }
        }

        static void Limbs(Work w, Limb trunk, float reached, bool clump, ref Rng rng)
        {
            var species = w.Species;
            var crownBase = Math.Min(0.85f, species.CrownBase + w.Crowd * 0.2f * (species.Shape == CrownShape.Cone ? 0.5f : 1));
            var top = 0.97f;
            var count = Math.Max(2, (int)Math.Round(species.Limbs * rng.Range(0.85f, 1.15f) * (trunk.Snapped ? 0.7f : 1) * (0.85f + 0.3f * w.Age)));
            var whorl = Math.Max(1, species.Whorl);
            var groups = (count + whorl - 1) / whorl;
            var spin = rng.Range(0f, 360f);

            for (var g = 0; g < groups; g++)
            {
                var tg = crownBase + (top - crownBase) * (g + rng.Range(0.15f, 0.85f)) / groups;

                for (var k = 0; k < whorl && g * whorl + k < count; k++)
                {
                    var n = g * whorl + k;
                    var azimuth = species.Whorl > 0 ? spin + g * 37 + k * 360f / whorl + rng.Range(-14f, 14f) : spin + n * 137.5f + rng.Range(-18f, 18f);
                    var t = species.Whorl > 0 ? tg : crownBase + (top - crownBase) * (n + rng.Range(0.2f, 0.8f)) / count;
                    // Through the crown in the tree's full height, so a snapped stem keeps the low shape.
                    var tc = Build.Clamp01((t * reached - crownBase) / (1 - crownBase));
                    var start = trunk.At(t, out var trunkDir, out var trunkRadius);
                    var profile = Profile(species.Shape, tc);
                    var length = w.Height * species.Spread * profile * rng.Range(0.78f, 1.15f) * (1 - 0.32f * w.Crowd);

                    if (length < Math.Max(0.6f, w.Height * 0.02f))
                    {
                        continue;
                    }

                    var angle = species.Angle + rng.Range(-9f, 9f);

                    if (species.Shape == CrownShape.Cone)
                    {
                        // Low limbs of a spire hang; high ones lift.
                        angle += 12 * (1 - tc) - 12 * tc;
                    }
                    else if (species.Shape == CrownShape.Dome || species.Shape == CrownShape.Vase)
                    {
                        angle -= 26 * tc * tc;
                    }

                    var dir = Away(trunkDir, azimuth, angle);
                    var radius = Math.Max(0.03f, Math.Min(trunkRadius * species.LimbGirth * rng.Range(0.72f, 1.1f), length * 0.085f));
                    var dead = !clump && rng.Chance(Math.Min(0.9f, species.DeadLimbs * (1 + w.Decay * 3) + w.Decay * 0.25f));

                    if (dead)
                    {
                        // A dead stub: short, bare, its end snapped.
                        var stub = Grow(w, start + dir * (trunkRadius * 0.4f), dir, length * rng.Range(0.12f, 0.4f), radius, 1, 0.1f, 0.05f, ref rng);
                        stub.Dead = true;
                        stub.Snapped = true;
                        var end = stub.R.Count - 1;

                        for (var i = 0; i <= end; i++)
                        {
                            stub.R[i] = radius * (1 - 0.35f * i / end);
                        }

                        w.Limbs.Add(stub);

                        continue;
                    }

                    var sag = species.Sag * (0.5f + length / Math.Max(1, w.Height * species.Spread));
                    var limb = Grow(w, start + dir * (trunkRadius * 0.4f), dir, length, radius, 1, species.Rise, sag, ref rng);
                    w.Limbs.Add(limb);
                    Branches(w, limb, ref rng);
                }
            }

            if (!trunk.Snapped && species.Levels > 1)
            {
                // The leader's own branches at the very top.
                var tip = trunk.At(0.975f, out var dir, out var r);
                var leader = Grow(w, tip, dir, w.Height * 0.05f, r, 1, 0.5f, 0, ref rng);
                w.Limbs.Add(leader);
                Branches(w, leader, ref rng);
            }
        }

        static void Branches(Work w, Limb parent, ref Rng rng)
        {
            var species = w.Species;
            var depth = parent.Depth + 1;

            if (depth > species.Levels)
            {
                return;
            }

            // Fewer at each level out.
            var basis = Math.Max(2, species.Twigs - (depth - 2) * 2);
            var count = Math.Max(1, (int)Math.Round(basis * Math.Min(1.3f, 0.5f + parent.Length / Math.Max(1f, w.Height * species.Spread)) * rng.Range(0.85f, 1.15f)));
            // Pines and firs fan their branches out level, left and right; broad trees all round.
            var flat = species.Leaves == LeafStyle.Pads || species.Leaves == LeafStyle.Fronds;

            for (var i = 0; i < count; i++)
            {
                var t = 0.22f + 0.74f * (i + rng.Range(0.1f, 0.9f)) / count;
                var start = parent.At(t, out var dir, out var r);
                var azimuth = flat ? (i % 2 == 0 ? 90 : -90) + rng.Range(-22f, 22f) : i * 137.5f + rng.Range(-28f, 28f);
                var twigDir = Away(dir, azimuth, species.TwigAngle + rng.Range(-10f, 10f), flat);
                var length = parent.Length * (depth == 2 ? 0.52f : 0.46f) * (1.12f - 0.6f * t) * rng.Range(0.8f, 1.2f);

                if (length < 0.35f)
                {
                    continue;
                }

                var hang = species.Shape == CrownShape.Weeping && depth == species.Levels ? 0.7f : 0;
                var twig = Grow(w, start, twigDir, length, Math.Max(0.012f, Math.Min(r * 0.62f, length * 0.06f)), depth, species.Rise * 1.1f, species.Sag * 0.7f + hang, ref rng);
                w.Limbs.Add(twig);
                Branches(w, twig, ref rng);
            }
        }

        /// <summary>A direction `angle` degrees off `axis`, toward `azimuth` degrees round it (0: the side nearest the sky). Flat: in the level plane.</summary>
        static V3 Away(V3 axis, float azimuth, float angle, bool flat = false)
        {
            var up = V3.Up - axis * V3.Dot(V3.Up, axis);
            up = up.Length < 1e-3f ? Build.Perpendicular(axis) : up.Normalized;
            var side = V3.Cross(axis, up);
            var a = azimuth * (float)Math.PI / 180;
            var toward = flat ? (side * (float)Math.Sin(a) + up * 0.22f * (float)Math.Cos(a)).Normalized : (up * (float)Math.Cos(a) + side * (float)Math.Sin(a)).Normalized;
            var b = angle * (float)Math.PI / 180;

            return (axis * (float)Math.Cos(b) + toward * (float)Math.Sin(b)).Normalized;
        }

        static Limb Grow(Work w, V3 start, V3 dir, float length, float radius, int depth, float rise, float sag, ref Rng rng)
        {
            var limb = new Limb { Depth = depth, Length = length, Phase = rng.Range(0f, 100f) };
            var steps = depth == 1 ? 14 : depth == 2 ? 9 : depth == 3 ? 6 : 4;
            var step = length / steps;
            var p = start;
            var gnarl = w.Species.Gnarl * (depth == 1 ? 0.7f : 1.15f) * (0.7f + 0.6f * w.Age);

            for (var s = 0; s <= steps; s++)
            {
                var t = s / (float)steps;
                limb.P.Add(p);
                limb.R.Add(Math.Max(0.008f, radius * (float)Math.Pow(1 - 0.86f * t, 1.05f)));
                var twist = new V3(w.Noise.Perlin(limb.Phase + t * 2.7f, 3.3f), w.Noise.Perlin(limb.Phase * 0.7f, t * 2.7f + 9), w.Noise.Perlin(t * 2.7f + 1, limb.Phase)) * gnarl;
                // Tips turn up to the light; long limbs sag under their own weight toward their ends.
                dir = (dir + twist + V3.Up * (rise * 0.2f) - V3.Up * (sag * t * 0.55f)).Normalized;
                p = p + dir * step;
            }

            return limb;
        }

        /// <summary>Roots: every buttress runs out over the ground, wandering, forking, sinking in.</summary>
        static void Roots(Work w, Limb trunk, ref Rng rng)
        {
            var shape = trunk.Shape;
            var species = w.Species;

            if (species.RootReach <= 0)
            {
                return;
            }

            for (var i = 0; i < shape.Angle.Length; i++)
            {
                var a = shape.Angle[i];
                var amp = shape.Amp[i];
                var out_ = new V3((float)Math.Cos(a), 0, (float)Math.Sin(a));
                var foot = w.R0 * shape.Mul(0, a, false);
                var reach = w.R0 * species.RootReach * rng.Range(0.6f, 1.4f) * (0.6f + 0.4f * amp) * (0.75f + 0.5f * w.Age);
                var thick = Math.Max(0.06f, w.R0 * 0.3f * amp * (0.5f + shape.Flare * 0.5f));
                Root(w, out_ * (foot * 0.62f) + new V3(0, thick * 0.9f, 0), out_, reach, thick, 2, ref rng);
            }
        }

        static void Root(Work w, V3 start, V3 dir, float length, float radius, int forks, ref Rng rng)
        {
            var root = new Limb { Depth = 1, Root = true, Length = length, Phase = rng.Range(0f, 100f) };
            const int steps = 12;
            var p = start;
            var side = new V3(-dir.Z, 0, dir.X);
            var bend = rng.Range(-0.5f, 0.5f);

            for (var s = 0; s <= steps; s++)
            {
                var t = s / (float)steps;
                var r = Math.Max(0.02f, radius * (float)Math.Pow(1 - 0.9f * t, 0.9f));
                // Half out of the ground near the foot, humping over it, gone under at the end.
                var y = r * (0.55f - 1.5f * t * t) + 0.25f * r * (float)Math.Sin(t * 9 + root.Phase);
                root.P.Add(new V3(p.X, y, p.Z));
                root.R.Add(r);
                var heading = (dir + side * (bend * t + 0.45f * w.Noise.Perlin(root.Phase + t * 3, 0.5f))).Normalized;
                p = p + heading * (length / steps);

                if (forks > 0 && s > 2 && s < 9 && rng.Chance(0.22f))
                {
                    var turn = (rng.Chance(0.5f) ? 1 : -1) * rng.Range(0.5f, 0.95f);
                    var forkDir = (heading * (float)Math.Cos(turn) + side * (float)Math.Sin(turn)).Normalized;
                    Root(w, new V3(p.X, y, p.Z), forkDir, length * (1 - t) * rng.Range(0.5f, 0.85f), r * 0.7f, forks - 1, ref rng);
                }
            }

            w.Limbs.Add(root);
        }

        /// <summary>Branches of a leaning or drooping tree kept off the ground (roots are meant to go in).</summary>
        static void Lift(Work w)
        {
            foreach (var limb in w.Limbs)
            {
                if (limb.Root || limb.Depth == 0)
                {
                    continue;
                }

                for (var i = 0; i < limb.P.Count; i++)
                {
                    var p = limb.P[i];
                    var floor = 0.5f + limb.R[i];

                    if (p.Y < floor)
                    {
                        limb.P[i] = new V3(p.X, floor + (floor - p.Y) * 0.1f, p.Z);
                    }
                }

                limb.Changed();
            }
        }

        /// <summary>The whole tree scaled about its foot so its top is at the asked height.</summary>
        static void Fit(Work w)
        {
            var top = 0f;

            foreach (var limb in w.Limbs)
            {
                foreach (var p in limb.P)
                {
                    top = Math.Max(top, p.Y);
                }
            }

            // The foliage stands a little over the highest twig.
            top += w.Species.Leaves == LeafStyle.None || w.Species.Leaves == LeafStyle.Hanging ? 0 : w.Species.LeafSize * 0.35f;

            if (top <= 0.5f)
            {
                return;
            }

            var k = Math.Max(0.6f, Math.Min(1.4f, w.Height / top));

            foreach (var limb in w.Limbs)
            {
                // The roots keep to the ground as they were grown.
                if (limb.Root)
                {
                    continue;
                }

                for (var i = 0; i < limb.P.Count; i++)
                {
                    var p = limb.P[i];
                    limb.P[i] = new V3(p.X * k, p.Y < 0 ? p.Y : p.Y * k, p.Z * k);
                }

                limb.Length *= k;
                limb.Changed();
            }
        }

        /// <summary>The asked lean, laid over the grown tree as a bend along +X: most at the foot, easing as the tree turns back to the light.</summary>
        static void Shear(Work w, float slope)
        {
            if (Math.Abs(slope) < 1e-3f)
            {
                return;
            }

            foreach (var limb in w.Limbs)
            {
                if (limb.Root)
                {
                    continue;
                }

                for (var i = 0; i < limb.P.Count; i++)
                {
                    var p = limb.P[i];
                    var t = Math.Max(0, p.Y) / w.Height;
                    limb.P[i] = new V3(p.X + slope * w.Height * (t - 0.22f * t * t), p.Y, p.Z);
                }

                limb.Changed();
            }
        }

        // ------------------------------------------------------------------ What grows on the skeleton

        static void Foliage(Work w, ref Rng rng)
        {
            var species = w.Species;
            var sum = new V3(0, 0, 0);
            var n = 0;
            var low = float.MaxValue;

            if (species.Leaves != LeafStyle.None)
            {
                var sizeK = Math.Max(0.75f, Math.Min(1.3f, w.Height / (0.5f * (species.MinHeight + species.MaxHeight))));
                var turned = Build.Clamp01(species.Turned + w.Asked.Season * (species.Leaf2 != null ? 1 : 0));
                var kind = species.Leaves == LeafStyle.Pads ? CardKind.Pad : species.Leaves == LeafStyle.Fronds ? CardKind.Frond
                    : species.Leaves == LeafStyle.Hanging ? CardKind.Hang : species.Leaves == LeafStyle.Scales ? CardKind.Scale : CardKind.Spray;

                foreach (var limb in w.Limbs)
                {
                    if (limb.Root || limb.Dead || limb.Depth == 0)
                    {
                        continue;
                    }

                    var finest = limb.Depth >= species.Levels;
                    var nearly = limb.Depth == species.Levels - 1;
                    var third = limb.Depth == species.Levels - 2 && limb.Depth >= 1;

                    if (!finest && !nearly && !third)
                    {
                        continue;
                    }

                    if (kind == CardKind.Hang && !finest)
                    {
                        continue;
                    }

                    int places;

                    if (kind == CardKind.Hang)
                    {
                        // Curtains all along the twig.
                        places = Math.Max(1, (int)(limb.Total / (species.LeafSize * 0.55f)));
                    }
                    else
                    {
                        places = finest ? Math.Max(1, (int)Math.Round(species.LeafCards)) : nearly ? 2 : 1;
                    }

                    for (var i = 0; i < places; i++)
                    {
                        if (w.Foliage < 1 && !rng.Chance(w.Foliage))
                        {
                            continue;
                        }

                        var t = kind == CardKind.Hang ? (i + rng.Range(0.2f, 0.8f)) / places : places == 1 ? 1 : (finest ? 0.3f : 0.6f) + (finest ? 0.7f : 0.4f) * i / (places - 1f);
                        var at = limb.At(t, out var dir, out var r);
                        var tex = species.Leaf2 != null && !rng.Chance(turned) ? species.Leaf2 : species.Leaf;
                        var tint = Tint.Mix(species.LeafTint, species.LeafTint2, rng.Value());
                        var size = species.LeafSize * sizeK * rng.Range(0.8f, 1.22f) * (1 - 0.12f * w.Crowd);

                        if (kind == CardKind.Hang)
                        {
                            size = Math.Min(size, at.Y - 0.4f);

                            if (size < 0.8f)
                            {
                                continue;
                            }
                        }
                        else if (at.Y < size * 0.45f + 0.3f)
                        {
                            // Nothing grows into the ground.
                            continue;
                        }

                        var repeat = w.Foliage > 1 && rng.Chance(w.Foliage - 1) ? 2 : 1;

                        for (var k = 0; k < repeat; k++)
                        {
                            w.Anchors.Add(new Anchor { At = k == 0 ? at : at - dir * (size * 0.3f), Dir = dir, Size = size, Seed = rng.Next(), Tex = tex, Kind = kind, Tint = tint });
                        }

                        sum = sum + at;
                        n++;
                        low = Math.Min(low, at.Y - (kind == CardKind.Hang ? size : size * 0.3f));
                    }
                }
            }

            if (n == 0)
            {
                // A bare tree: its crown is its limbs.
                foreach (var limb in w.Limbs)
                {
                    if (limb.Depth == 1 && !limb.Root)
                    {
                        var tip = limb.P[limb.P.Count - 1];
                        sum = sum + tip;
                        n++;
                        low = Math.Min(low, limb.P[0].Y);
                    }
                }
            }

            w.CrownCentre = n > 0 ? sum * (1f / n) : w.Main.P[w.Main.P.Count * 2 / 3];
            w.CrownLow = n > 0 ? Math.Max(1.5f, low) : w.Height * 0.4f;
            var reach = 1f;

            foreach (var limb in w.Limbs)
            {
                if (limb.Root || limb.Depth == 0)
                {
                    continue;
                }

                var tip = limb.P[limb.P.Count - 1];
                reach = Math.Max(reach, new V3(tip.X - w.CrownCentre.X, 0, tip.Z - w.CrownCentre.Z).Length);
            }

            w.CrownRadius = reach + (species.Leaves == LeafStyle.None ? 0 : species.LeafSize * 0.5f);
        }

        /// <summary>Beards of lichen under the limbs and branches: more on old, wet, dying trees.</summary>
        static void Lichen(Work w, ref Rng rng)
        {
            var amount = w.Species.Lichen * (0.5f + 0.8f * w.Age + w.Decay) * (0.6f + Math.Max(-0.6f, w.Asked.Wet));

            if (amount <= 0.02f)
            {
                return;
            }

            var count = w.Limbs.Count;

            for (var l = 0; l < count; l++)
            {
                var limb = w.Limbs[l];

                if (limb.Root || limb.Depth == 0 || limb.Depth > 2 || limb.R[0] < 0.05f)
                {
                    continue;
                }

                var beards = (int)(limb.Total / 2.2f * amount * rng.Range(0.5f, 1.5f));

                for (var i = 0; i < beards; i++)
                {
                    var at = limb.At(rng.Range(0.12f, 0.95f), out var dir, out var r);
                    var size = Math.Min(at.Y - r - 0.4f, rng.Range(1.3f, 3.6f) * (0.7f + w.Height / 120f));

                    if (size < 0.6f)
                    {
                        continue;
                    }

                    w.Anchors.Add(new Anchor { At = at - V3.Up * (r * 0.7f), Dir = dir, Size = size, Seed = rng.Next(), Tex = "hang_lichen", Kind = CardKind.Hang, Tint = new Tint(1, 1, 1) });
                }
            }
        }

        /// <summary>A point on the main trunk's skin at a height and an angle, and the level way out from it there.</summary>
        static V3 Skin(Work w, float y, float theta, out V3 outward, float proud = 0)
        {
            var trunk = w.Main;
            var i = 1;

            while (i < trunk.P.Count - 2 && trunk.P[i + 1].Y < y)
            {
                i++;
            }

            var k = Build.Clamp01((y - trunk.P[i].Y) / Math.Max(1e-4f, trunk.P[i + 1].Y - trunk.P[i].Y));
            var centre = V3.Lerp(trunk.P[i], trunk.P[i + 1], k);
            var radius = Build.Lerp(trunk.R[i], trunk.R[i + 1], k);
            outward = new V3((float)Math.Cos(theta), 0, (float)Math.Sin(theta));

            return centre + outward * (radius * trunk.Shape.Mul(y, theta, true) + proud);
        }

        static void Extras(Work w, ref Rng rng)
        {
            var species = w.Species;

            if (species.MaxTrunks > 4)
            {
                return;
            }

            var reach = Math.Min(w.Height * 0.32f, 6 + w.R0 * 6);

            // Bracket fungus: shelves in steps up the bole, more on the old and the dying.
            var clusters = (int)Math.Round(species.Fungus * (0.3f + 0.6f * w.Age + 1.5f * w.Decay) * rng.Range(0.6f, 1.3f));

            for (var c = 0; c < clusters; c++)
            {
                var y = rng.Range(0.7f, Math.Max(1.5f, reach));
                var theta = rng.Range(0f, Build.Tau);
                var shelves = rng.Range(2, 6);
                var size = rng.Range(0.16f, 0.5f) * (0.6f + Math.Min(1.4f, w.R0 * 0.5f));

                for (var s = 0; s < shelves; s++)
                {
                    var at = Skin(w, y, theta, out var outward, -size * 0.12f);
                    w.Fungus.Add((at, outward, size * rng.Range(0.6f, 1.1f), rng.Range(-0.3f, 0.3f)));
                    y += size * rng.Range(0.45f, 0.9f);
                    theta += rng.Range(-0.18f, 0.18f) / Math.Max(0.5f, w.R0);
                }
            }

            // Vines: winding up the bole, leafed with ivy.
            for (var v = 0; v < species.Vines; v++)
            {
                if (!rng.Chance(0.4f + 0.6f * w.Age))
                {
                    continue;
                }

                var theta = rng.Range(0f, Build.Tau);
                var wind = (rng.Chance(0.5f) ? 1 : -1) * rng.Range(0.12f, 0.4f) / Math.Max(0.6f, w.R0);
                var top = w.Height * rng.Range(0.18f, Math.Min(0.6f, species.CrownBase + 0.25f));
                var radius = rng.Range(0.035f, 0.085f) * (0.7f + w.R0 * 0.25f);
                var path = new List<V3>();

                for (var y = -0.2f; y < top; y += 0.45f)
                {
                    theta += wind * 0.45f + 0.05f * w.Noise.Perlin(y * 0.4f, v * 7.1f);
                    path.Add(Skin(w, y, theta, out var outward, radius * 0.7f));

                    if (y > 0.5f && rng.Chance(0.5f * w.Foliage))
                    {
                        var size = rng.Range(0.9f, 1.7f);
                        w.Anchors.Add(new Anchor { At = path[path.Count - 1], Dir = (outward * 0.8f + V3.Up * 0.6f).Normalized, Size = size, Seed = rng.Next(), Tex = "leaf_ivy", Kind = CardKind.Spray, Tint = new Tint(0.9f, 0.95f, 0.9f) });
                    }
                }

                if (path.Count > 3)
                {
                    w.Vines.Add((path, radius));
                }
            }

            // Glowing moss: beads of it in the hollows between the buttresses and up the bark.
            if (species.Glow)
            {
                var dots = rng.Range(40, 80);

                for (var i = 0; i < dots; i++)
                {
                    var y = rng.Range(0.3f, reach * 1.2f);
                    var at = Skin(w, y, rng.Range(0f, Build.Tau), out _, 0.02f);
                    w.Glow.Add((at, rng.Range(0.05f, 0.16f)));
                }
            }
        }

        // ------------------------------------------------------------------ The meshes

        static int Sides(float radius, int lod, bool trunk)
        {
            if (lod == 3)
            {
                return trunk ? 6 : 3;
            }

            if (lod == 2)
            {
                return trunk ? 10 : radius > 0.3f ? 5 : 3;
            }

            var sides = radius >= 1.2f ? (trunk ? 56 : 30) : radius >= 0.6f ? (trunk ? 40 : 24) : radius >= 0.3f ? 18 : radius >= 0.12f ? 11 : radius >= 0.05f ? 7 : radius >= 0.025f ? 5 : 3;

            return lod == 0 ? sides : Math.Max(3, sides * 3 / 5);
        }

        static TreeLod Lod(Work w, int lod)
        {
            var species = w.Species;
            var result = new TreeLod();
            var bark = result.Part("bark:" + species.Bark);
            var extra = lod <= 1 ? result.Part("extra") : null;

            foreach (var limb in w.Limbs)
            {
                var r0 = limb.R[limb.Depth == 0 ? 1 : 0];
                var trunk = limb.Depth == 0;

                // What each level keeps: all; all but the finest twigs; the trunk, roots and limbs; the trunk.
                if (lod == 1 && limb.Depth >= species.Levels && species.Levels > 1 && r0 < 0.06f)
                {
                    continue;
                }

                if (lod == 2 && !trunk && (limb.Depth > 1 || r0 < Math.Max(0.1f, w.R0 * 0.1f)))
                {
                    continue;
                }

                if (lod == 3 && !trunk)
                {
                    continue;
                }

                var step = lod == 0 ? 1 : lod == 1 ? (trunk ? 1 : 2) : lod == 2 ? 2 : 4;
                var sides = Sides(r0, lod, trunk || limb.Root);

                if (trunk && lod <= 1 && limb.Shape != null && limb.Shape.Angle.Length > 0)
                {
                    // A buttressed foot needs enough sides to carry its fins.
                    sides = Math.Max(sides, Math.Min(lod == 0 ? 112 : 64, (int)(r0 * (1 + limb.Shape.Flare) * Build.Tau / 0.42f)));
                }

                Tube(w, bark, limb, sides, step, lod, !limb.Snapped);

                if (limb.Snapped && extra != null)
                {
                    var end = limb.P.Count - 1;
                    Splinters(extra, limb.P[end], (limb.P[end] - limb.P[end - 1]).Normalized, limb.R[end], w.Noise);
                }
            }

            // The foliage: every card near; fewer, larger ones farther.
            var every = lod == 0 ? 1u : lod == 1 ? 2u : lod == 2 ? 5u : 14u;
            var grow = lod == 0 ? 1f : lod == 1 ? 1.35f : lod == 2 ? 2.1f : 3.4f;

            foreach (var anchor in w.Anchors)
            {
                if (anchor.Seed % every != 0)
                {
                    continue;
                }

                // Lichen and ivy are for near eyes only.
                if (lod >= 2 && (anchor.Tex == "hang_lichen" || anchor.Tex == "leaf_ivy"))
                {
                    continue;
                }

                Cards(w, result.Part("leaf:" + anchor.Tex), anchor, lod, grow);
            }

            if (lod <= 1)
            {
                foreach (var (at, outward, size, turn) in w.Fungus)
                {
                    Bracket(extra, at, outward, size, turn, lod == 0 ? 12 : 7);
                }
            }

            if (lod == 0)
            {
                foreach (var (path, radius) in w.Vines)
                {
                    var radii = new float[path.Count];

                    for (var i = 0; i < radii.Length; i++)
                    {
                        radii[i] = radius * (1 - 0.6f * i / radii.Length);
                    }

                    var dark = new Tint(0.13f, 0.12f, 0.06f);
                    Build.Tube(extra, path, radii, 5, (ring, around, p, n) => Build.Shade(dark, 0.8f + 0.4f * around), true);
                }
            }

            if (lod <= 1 && w.Glow.Count > 0)
            {
                var glow = result.Part("glow");
                var colour = new Tint(0.45f, 1f, 0.8f);

                foreach (var (at, size) in w.Glow)
                {
                    Build.Blob(glow, at, new V3(size, 0, 0), new V3(0, size * 0.6f, 0), new V3(0, 0, size), 0, w.Noise, 0.2f, (p, n) => colour);
                }
            }

            result.Parts.RemoveAll(part => part.Mesh.TriangleCount == 0);

            return result;
        }

        /// <summary>
        /// A limb's skin: rings swept along it, each pushed out by the trunk's shape (buttresses,
        /// burls, joints) and the bark's lumps; texture coordinates that tile the bark round and
        /// along it; in the vertices' colour, the moss's weight (red) and the shade (green).
        /// </summary>
        static void Tube(Work w, MeshData mesh, Limb limb, int sides, int step, int lod, bool cap)
        {
            var count = limb.P.Count;
            var rings = new List<int>();

            for (var i = 0; i < count; i += step)
            {
                rings.Add(i);
            }

            if (rings[rings.Count - 1] != count - 1)
            {
                rings.Add(count - 1);
            }

            var trunk = limb.Depth == 0;
            var shape = limb.Shape;
            var species = w.Species;
            var n1 = trunk ? new V3(1, 0, 0) : V3.Up;
            var grid = new V3[rings.Count, sides];
            var ridge = new float[rings.Count, sides];
            var centres = new V3[rings.Count];

            for (var k = 0; k < rings.Count; k++)
            {
                var i = rings[k];
                var next = i < count - 1 ? (limb.P[i + 1] - limb.P[i]).Normalized : (limb.P[i] - limb.P[i - 1]).Normalized;
                var before = i > 0 ? (limb.P[i] - limb.P[i - 1]).Normalized : next;
                var axis = (next + before).Normalized;

                // The trunk's frame is fixed to the world (its buttresses stand where its roots run); a limb's is carried along it.
                if (trunk)
                {
                    n1 = new V3(1, 0, 0);
                }

                n1 = n1 - axis * V3.Dot(n1, axis);
                n1 = n1.Length < 1e-4f ? Build.Perpendicular(axis) : n1.Normalized;
                var n2 = V3.Cross(n1, axis);
                var centre = limb.P[i];
                centres[k] = centre;
                var radius = limb.R[i];
                var along = limb.AlongAt(i);
                // A collar where the limb leaves its parent.
                var collar = trunk || limb.Root ? 1 : 1 + 0.5f * (float)Math.Exp(-along / Math.Max(0.05f, limb.R[0] * 1.6f));
                float min = float.MaxValue, max = float.MinValue;

                for (var s = 0; s < sides; s++)
                {
                    var theta = s / (float)sides * Build.Tau;
                    var mul = collar;

                    if (shape != null)
                    {
                        mul *= shape.Mul(centre.Y, theta, lod <= 1);
                    }

                    if (lod <= 1 && radius > 0.06f)
                    {
                        // The bark's own lumps and the wood's knots.
                        mul *= 1 + 0.045f * w.Noise.Perlin(theta * 2.2f + limb.Phase, along * 0.33f) + 0.02f * w.Noise.Perlin(theta * 6.5f, along * 1.1f + limb.Phase);
                    }

                    ridge[k, s] = mul;
                    min = Math.Min(min, mul);
                    max = Math.Max(max, mul);
                    grid[k, s] = centre + (n1 * (float)Math.Cos(theta) + n2 * (float)Math.Sin(theta)) * (radius * mul);
                }

                // How proud of the ring's hollows each vertex stands, 0-1 (for the shade between buttresses).
                for (var s = 0; s < sides; s++)
                {
                    ridge[k, s] = max - min < 0.08f ? 1 : 0.35f + 0.65f * (ridge[k, s] - min) / (max - min);
                }
            }

            // The bark's tiles: a whole number round the limb's foot, square there.
            var foot = limb.R[trunk ? 1 : 0] * (trunk && shape != null ? 1 + shape.Flare * 0.2f : 1);
            var circumference = Build.Tau * Math.Max(0.02f, foot);
            var repeats = Math.Max(1, (int)Math.Round(circumference / species.BarkTile));
            var cell = circumference / repeats;
            var start = mesh.VertexCount;

            for (var k = 0; k < rings.Count; k++)
            {
                var along = limb.AlongAt(rings[k]);

                for (var s = 0; s <= sides; s++)
                {
                    var a = s % sides;
                    var p = grid[k, a];
                    // The normal from the skin's own slopes, so buttresses and burls catch the light.
                    var tu = grid[k, (a + 1) % sides] - grid[k, (a + sides - 1) % sides];
                    var tv = grid[Math.Min(rings.Count - 1, k + 1), a] - grid[Math.Max(0, k - 1), a];
                    var n = V3.Cross(tu, tv).Normalized;

                    if (V3.Dot(n, p - centres[k]) < 0)
                    {
                        n = n * -1;
                    }

                    var colour = BarkColour(w, limb, p, n, ridge[k, a], along);
                    var v = mesh.Vertex(p.X, p.Y, p.Z, s / (float)sides * repeats + limb.Phase, along / cell, colour.moss, colour.shade, 1, 1);
                    mesh.SetNormal(v, n.X, n.Y, n.Z);
                }
            }

            var ring = sides + 1;

            for (var k = 0; k < rings.Count - 1; k++)
            {
                for (var s = 0; s < sides; s++)
                {
                    var a = start + k * ring + s;
                    var b = start + (k + 1) * ring + s;
                    mesh.Triangle(a, b, b + 1);
                    mesh.Triangle(a, b + 1, a + 1);
                }
            }

            if (cap && limb.R[count - 1] > 0.004f)
            {
                var last = rings.Count - 1;
                var dir = (limb.P[count - 1] - limb.P[count - 2]).Normalized;
                var tip = limb.P[count - 1] + dir * (limb.R[count - 1] * 0.9f);
                var colour = BarkColour(w, limb, tip, dir, 1, limb.Total);
                var t = mesh.Vertex(tip.X, tip.Y, tip.Z, 0.5f * repeats + limb.Phase, (limb.Total + limb.R[count - 1]) / cell, colour.moss, colour.shade, 1, 1);
                mesh.SetNormal(t, dir.X, dir.Y, dir.Z);
                var at = start + last * ring;

                for (var s = 0; s < sides; s++)
                {
                    mesh.Triangle(at + s, t, at + s + 1);
                }
            }
        }

        /// <summary>The moss's weight (0-1) and the shade (0-1) of the bark at a point.</summary>
        static (float moss, float shade) BarkColour(Work w, Limb limb, V3 p, V3 n, float proud, float along)
        {
            var low = (float)Math.Exp(-Math.Max(0, p.Y) / (0.09f * w.Height + 3f));
            var back = Build.Clamp01(n.Y * 1.4f - 0.15f);
            var patch = 0.5f + 0.5f * w.Noise.Perlin(p.X * 0.31f + p.Y * 0.17f, p.Z * 0.31f - p.Y * 0.11f);
            var fine = 0.5f + 0.5f * w.Noise.Perlin(p.X * 1.7f - p.Y * 0.9f, p.Z * 1.7f + p.Y * 0.6f);
            var limbs = limb.Depth == 0 ? 0.35f : limb.Root ? 1.1f : 0.85f;
            var wide = 0.5f + 0.5f * w.Noise.Fbm(p.X * 0.09f + p.Y * 0.05f + 31, p.Z * 0.09f - p.Y * 0.04f, 3);
            var wet = Math.Min(1, w.Wet);
            // How much of the bark the moss wants here: most at the foot and in the hollows between
            // the buttresses, on the roots and along the limbs' backs; then broken into patches, so
            // bark always shows through somewhere.
            var want = wet * Math.Max((0.3f + 0.7f * low) * (limb.Root ? 1.15f : limb.Depth == 0 ? 1 : 0.5f) + 0.25f * (1 - proud) * low, back * limbs * (0.55f + 0.3f * low));
            var moss = want * 0.95f + (wide - 0.5f) * 1.3f + (patch - 0.5f) * 0.7f + (fine - 0.5f) * 0.3f - 0.22f;

            if (want < 0.03f)
            {
                moss = 0;
            }

            var shade = 0.55f + 0.45f * proud;

            if (limb.Root || limb.Depth == 0)
            {
                // The ground's shadow on the foot.
                shade *= 0.72f + 0.28f * Build.Clamp01(p.Y / 1.2f);
            }
            else
            {
                // Inside the crown it is dim; and under each limb's foot.
                var inside = Build.Clamp01((p - w.CrownCentre).Length / Math.Max(1, w.CrownRadius));
                shade *= (0.72f + 0.28f * inside) * (0.8f + 0.2f * Build.Clamp01(along / Math.Max(0.3f, limb.R[0] * 3)));
            }

            return (Build.Clamp01(moss), Build.Clamp01(shade));
        }

        /// <summary>
        /// The foliage at one place on a twig: the kind's spray as cards — crossed for a spray, level
        /// and layered for a pine's pad, drooping for a fir's frond, hanging for a willow's curtain —
        /// each bent a little, lit softly by a normal turned outward from the crown, tinted and
        /// shaded in its vertices (alpha: how freely it moves in the wind).
        /// </summary>
        static void Cards(Work w, MeshData mesh, Anchor a, int lod, float grow)
        {
            var seed = a.Seed;

            float Next()
            {
                seed = seed * 1664525u + 1013904223u;

                return (seed >> 8) * (1f / 16777216f);
            }

            var size = a.Size * grow;
            var outward = (a.At - w.CrownCentre);
            outward = outward.Length < 0.01f ? V3.Up : outward.Normalized;
            var inside = Build.Clamp01((a.At - w.CrownCentre).Length / Math.Max(1, w.CrownRadius));
            var height = Build.Clamp01((a.At.Y - w.CrownLow) / Math.Max(1, w.Height - w.CrownLow));
            var shade = 0.5f + 0.3f * inside + 0.24f * height;
            var flat = new V3(a.Dir.X, 0, a.Dir.Z);
            flat = flat.Length < 1e-3f ? new V3(outward.X, 0, outward.Z + 0.001f).Normalized : flat.Normalized;
            var rows = lod == 0 ? 2 : 1;
            int cards;

            switch (a.Kind)
            {
                case CardKind.Pad: cards = lod == 0 ? 3 : lod == 1 ? 2 : 1; break;
                case CardKind.Frond: cards = lod <= 1 ? 2 : 1; break;
                case CardKind.Hang: cards = lod <= 1 ? 2 : 1; break;
                default: cards = lod == 0 ? 3 : 2; break;
            }

            for (var c = 0; c < cards; c++)
            {
                V3 origin = a.At, grows, across, curl;
                var mirror = Next() < 0.5f;
                var vFrom = 0f;
                var vTo = 1f;

                switch (a.Kind)
                {
                    case CardKind.Pad:
                    {
                        // Level pads, turned about the vertical and stacked a little: a cloud of needles.
                        var yaw = (c - (cards - 1) * 0.5f) * 0.9f + (Next() - 0.5f) * 0.5f;
                        var g = Build.Rotate(flat, V3.Up, yaw);
                        grows = (g + V3.Up * (0.1f + 0.12f * Next())).Normalized;
                        across = V3.Cross(V3.Up, g).Normalized;
                        across = Build.Rotate(across, g, (Next() - 0.5f) * 0.5f);
                        origin = a.At - g * (size * 0.18f) + V3.Up * (c * size * 0.06f);
                        curl = V3.Up * -0.12f;

                        break;
                    }
                    case CardKind.Frond:
                    {
                        // A frond lying along the twig, drooping at its end, the second rolled a little.
                        grows = (a.Dir * 0.8f + flat * 0.3f - V3.Up * 0.12f).Normalized;
                        across = V3.Cross(V3.Up, grows).Normalized;
                        across = Build.Rotate(across, grows, (c == 0 ? 0 : 0.9f) + (Next() - 0.5f) * 0.4f);
                        origin = a.At - grows * (size * 0.3f);
                        curl = V3.Up * -0.3f;

                        break;
                    }
                    case CardKind.Hang:
                    {
                        // A curtain hanging from the twig: the picture's top at the twig.
                        var yaw = c * 1.6f + Next() * Build.Tau;
                        grows = new V3(0, -1, 0);
                        across = new V3((float)Math.Cos(yaw), 0, (float)Math.Sin(yaw));
                        curl = (outward * 0.3f + new V3(Next() - 0.5f, 0, Next() - 0.5f) * 0.3f);
                        vFrom = 1;
                        vTo = 0;

                        break;
                    }
                    default:
                    {
                        // A spray standing off the twig toward the light, crossed by its fellows.
                        var lift = a.Kind == CardKind.Scale ? 0.75f : 0.4f;
                        grows = (a.Dir * 0.55f + outward * 0.35f + V3.Up * lift + new V3(Next() - 0.5f, Next() - 0.5f, Next() - 0.5f) * 0.6f).Normalized;
                        across = Build.Rotate(Build.Perpendicular(grows), grows, c * (float)Math.PI / cards + Next() * 0.6f);
                        origin = a.At - grows * (size * 0.12f);
                        curl = (V3.Up * -0.18f + outward * 0.12f);

                        break;
                    }
                }

                var face = V3.Cross(across, grows).Normalized;

                if (V3.Dot(face, outward) < 0)
                {
                    face = face * -1;
                }

                // A soft normal: mostly out from the crown, so the whole tree lights as one mass.
                var normal = (outward * 0.7f + face * 0.35f + V3.Up * 0.25f).Normalized;
                var tint = Build.Shade(a.Tint, shade * (0.9f + 0.2f * Next()));
                var first = mesh.VertexCount;

                for (var r = 0; r <= rows; r++)
                {
                    var t = r / (float)rows;
                    var centre = origin + grows * (size * t) + curl * (size * t * t);
                    var half = across * (size * 0.5f);
                    var v = vFrom + (vTo - vFrom) * t;
                    // Darker at the twig, brighter at the tips.
                    var k = 0.82f + 0.25f * t;
                    var left = centre - half;
                    var right = centre + half;
                    var i0 = mesh.Vertex(left.X, left.Y, left.Z, mirror ? 1 : 0, v, tint.R * k, tint.G * k, tint.B * k, t);
                    var i1 = mesh.Vertex(right.X, right.Y, right.Z, mirror ? 0 : 1, v, tint.R * k, tint.G * k, tint.B * k, t);
                    mesh.SetNormal(i0, normal.X, normal.Y, normal.Z);
                    mesh.SetNormal(i1, normal.X, normal.Y, normal.Z);
                }

                for (var r = 0; r < rows; r++)
                {
                    var at = first + r * 2;
                    mesh.Triangle(at, at + 2, at + 3);
                    mesh.Triangle(at, at + 3, at + 1);
                }
            }
        }

        /// <summary>A shelf of bracket fungus: a half dome standing out of the bark, banded brown above, pale beneath.</summary>
        static void Bracket(MeshData mesh, V3 at, V3 outward, float size, float turn, int around)
        {
            var side = new V3(-outward.Z, 0, outward.X);
            var out_ = Build.Rotate(outward, V3.Up, turn);
            side = Build.Rotate(side, V3.Up, turn);
            const int bands = 3;
            var start = mesh.VertexCount;
            var top = new[] { new Tint(0.30f, 0.16f, 0.07f), new Tint(0.52f, 0.30f, 0.11f), new Tint(0.70f, 0.47f, 0.20f), new Tint(0.90f, 0.82f, 0.62f) };

            // The top: bands out from the bark, doming down to the rim.
            for (var b = 0; b <= bands; b++)
            {
                var t = b / (float)bands;

                for (var s = 0; s <= around; s++)
                {
                    var a = (s / (float)around - 0.5f) * (float)Math.PI;
                    var reach = size * t * (0.85f + 0.15f * (float)Math.Cos(a * 3 + size * 40));
                    var p = at + out_ * ((float)Math.Cos(a) * reach) + side * ((float)Math.Sin(a) * reach) + V3.Up * (size * 0.3f * (1 - t * t));
                    var c = top[b];
                    var v = mesh.Vertex(p.X, p.Y, p.Z, 0.5f, 0.5f, c.R, c.G, c.B);
                    var n = (V3.Up * (1 - 0.6f * t) + (out_ * (float)Math.Cos(a) + side * (float)Math.Sin(a)) * (0.7f * t)).Normalized;
                    mesh.SetNormal(v, n.X, n.Y, n.Z);
                }
            }

            var ring = around + 1;

            for (var b = 0; b < bands; b++)
            {
                for (var s = 0; s < around; s++)
                {
                    var a = start + b * ring + s;
                    var c = start + (b + 1) * ring + s;
                    mesh.Triangle(a, a + 1, c + 1);
                    mesh.Triangle(a, c + 1, c);
                }
            }

            // The underside: a pale fan from the bark to the rim.
            var pale = new Tint(0.62f, 0.54f, 0.4f);
            var centre = mesh.Vertex(at.X, at.Y - size * 0.05f, at.Z, 0.5f, 0.5f, pale.R * 0.7f, pale.G * 0.7f, pale.B * 0.7f);
            mesh.SetNormal(centre, 0, -1, 0);
            var under = mesh.VertexCount;

            for (var s = 0; s <= around; s++)
            {
                var a = (s / (float)around - 0.5f) * (float)Math.PI;
                var reach = size * (0.85f + 0.15f * (float)Math.Cos(a * 3 + size * 40));
                var p = at + out_ * ((float)Math.Cos(a) * reach) + side * ((float)Math.Sin(a) * reach);
                var v = mesh.Vertex(p.X, p.Y, p.Z, 0.5f, 0.5f, pale.R, pale.G, pale.B);
                mesh.SetNormal(v, 0, -1, 0);
            }

            for (var s = 0; s < around; s++)
            {
                mesh.Triangle(centre, under + s + 1, under + s);
            }
        }

        /// <summary>The splintered end of a snapped stem: a ring of jagged teeth of pale wood round the break, seen from both sides.</summary>
        static void Splinters(MeshData mesh, V3 at, V3 dir, float radius, Noise noise)
        {
            var n1 = Build.Perpendicular(dir);
            var n2 = V3.Cross(n1, dir);
            var teeth = Math.Max(7, Math.Min(22, (int)(radius * 30)));
            var dark = new Tint(0.16f, 0.11f, 0.07f);
            var light = new Tint(0.62f, 0.5f, 0.34f);
            var centre = mesh.Vertex(at.X, at.Y, at.Z, 0.5f, 0.5f, dark.R, dark.G, dark.B);
            mesh.SetNormal(centre, dir.X, dir.Y, dir.Z);
            var ring = new int[teeth * 2];

            for (var i = 0; i < teeth * 2; i++)
            {
                var a = i / (float)(teeth * 2) * Build.Tau;
                var r = radius * (i % 2 == 0 ? 1.0f : 0.72f);
                var rise = i % 2 == 0 ? radius * (0.3f + 1.5f * (noise.Perlin(i * 1.7f, radius * 9) * 0.5f + 0.5f)) : radius * 0.08f;
                var p = at + n1 * ((float)Math.Cos(a) * r) + n2 * ((float)Math.Sin(a) * r) + dir * rise;
                var c = i % 2 == 0 ? light : Tint.Mix(light, dark, 0.5f);
                ring[i] = mesh.Vertex(p.X, p.Y, p.Z, 0.5f, 0.5f, c.R, c.G, c.B);
                var n = (dir + (n1 * (float)Math.Cos(a) + n2 * (float)Math.Sin(a)) * 0.6f).Normalized;
                mesh.SetNormal(ring[i], n.X, n.Y, n.Z);
            }

            for (var i = 0; i < teeth * 2; i++)
            {
                var next = ring[(i + 1) % (teeth * 2)];
                mesh.Triangle(centre, next, ring[i]);
                mesh.Triangle(centre, ring[i], next);
            }
        }

        // ------------------------------------------------------------------ The solid shape

        /// <summary>
        /// What one can stand on and climb: the trunk with its buttresses, the roots, and every limb
        /// thick enough to walk — as a plain mesh for the engine's collider, and the limbs' centre
        /// lines for the climber.
        /// </summary>
        static MeshData Solid(Work w, TreeModel model)
        {
            var mesh = new MeshData();
            var white = new Tint(1, 1, 1);

            foreach (var limb in w.Limbs)
            {
                var trunk = limb.Depth == 0;
                var r0 = limb.R[trunk ? 1 : 0];

                if (!trunk && r0 < 0.14f)
                {
                    continue;
                }

                // Only as far as it stays thick enough to bear a foot.
                var points = new List<V3>();
                var radii = new List<float>();

                for (var i = 0; i < limb.P.Count; i++)
                {
                    if (!trunk && limb.R[i] < 0.07f && points.Count >= 2)
                    {
                        break;
                    }

                    var r = limb.R[i];

                    if (trunk && limb.Shape != null)
                    {
                        // The foot's mean spread (the buttresses themselves are walked over by their roots' tubes).
                        r *= 1 + limb.Shape.Flare * 0.55f * (float)Math.Exp(-Math.Max(0, limb.P[i].Y) / Math.Max(0.1f, limb.Shape.FlareHeight));
                    }

                    points.Add(limb.P[i]);
                    radii.Add(r);
                }

                if (points.Count < 2)
                {
                    continue;
                }

                Build.Tube(mesh, points, radii, trunk ? 12 : r0 > 0.4f ? 8 : 6, (ring, around, p, n) => white, true);

                if (!limb.Root && limb.Depth <= 2)
                {
                    model.Climb.Add(new TreeLimb { Points = points.ToArray(), Radii = radii.ToArray(), Depth = limb.Depth });
                }
            }

            return mesh;
        }
    }
}
