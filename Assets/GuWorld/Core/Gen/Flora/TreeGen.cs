using System;
using System.Collections.Generic;

namespace GuWorld.Core.Gen.Flora
{
    /// <summary>What a tree is asked to be, beyond its kind and its seed.</summary>
    public struct TreeParams
    {
        /// <summary>Its height in metres (0: anywhere in its kind's range, by the seed).</summary>
        public float Height;
        /// <summary>How far it leans, in degrees, toward +X of its own space (the wind's side, the open side, downhill).</summary>
        public float Lean;
        /// <summary>How crowded it grew: 0 alone in the open (wide, low crown), 1 in a dense wood (narrow, high crown).</summary>
        public float Crowding;
        /// <summary>0 young (smooth, slim) to 1 old (thick, gnarled, its crown flattened).</summary>
        public float Age;
        /// <summary>Added to its kind's share of turned leaves (autumn, blossom), −1 to 1.</summary>
        public float Season;
    }

    /// <summary>A grown tree: its meshes from near to far and the numbers the game places and collides it by.</summary>
    public sealed class TreeModel
    {
        public TreeSpecies Species;
        public int Seed;
        public TreeParams Asked;
        public float Height;
        /// <summary>The trunk's radius a metre up.</summary>
        public float TrunkRadius;
        /// <summary>Where the crown begins (the trunk below is a clean pole to collide with).</summary>
        public float CrownBase;
        /// <summary>The crown's radius about its centre, seen from above.</summary>
        public float CrownRadius;
        public V3 CrownCentre;
        /// <summary>Where the top of the main trunk ended up (its lean shows in X).</summary>
        public V3 Top;
        /// <summary>Levels of detail, near to far: 0 all of it, 1 lighter, 2 a few blobs on a pole.</summary>
        public MeshData[] Lods;
        public int Limbs;
        public int Clumps;
    }

    /// <summary>
    /// Grows trees from a kind (<see cref="TreeSpecies"/>), a seed and what is asked of them
    /// (<see cref="TreeParams"/>). A trunk wanders up from the ground, leaning where asked and
    /// turning back toward the light; main branches leave it in a spiral or in whorls, their length
    /// by the crown's shape; twigs leave the branches, and again; leaves are set at the twigs as
    /// clumps, pads, sprays, scales, blossom or hanging strands. Wood and leaves are coloured in
    /// their vertices: bark streaked and mossed low, leaves darker inside the crown and lighter on
    /// top. Three levels of detail are made from the same skeleton. The same kind, seed and asking
    /// always grow the same tree. Y up, metres, the base at the origin (sunk a little).
    /// </summary>
    public static class TreeGen
    {
        sealed class Limb
        {
            public readonly List<V3> Points = new List<V3>();
            public readonly List<float> Radii = new List<float>();
            public int Depth;
            public float Length;
            public bool Root;
            public bool Snapped;

            public V3 At(float t, out V3 dir, out float radius)
            {
                var f = Math.Max(0, Math.Min(Points.Count - 1.001f, t * (Points.Count - 1)));
                var i = (int)f;
                var k = f - i;
                dir = (Points[i + 1] - Points[i]).Normalized;
                radius = Build.Lerp(Radii[i], Radii[i + 1], k);

                return V3.Lerp(Points[i], Points[i + 1], k);
            }
        }

        enum ClumpKind { Blob, Strand }

        struct Clump
        {
            public ClumpKind Kind;
            public V3 Centre, Ax, Ay, Az;
            public Tint Colour;
            public float Size;
            /// <summary>For a strand: the path it hangs along.</summary>
            public V3[] Path;
        }

        public static TreeModel Grow(TreeSpecies species, int seed, TreeParams asked)
        {
            var rng = new Rng(seed * 7919 + Noise.Stable(species.Id));
            var noise = new Noise(seed * 31 + 5);
            var height = asked.Height > 0 ? asked.Height : rng.Range(species.MinHeight, species.MaxHeight);
            var age = Build.Clamp01(asked.Age);
            var crowd = Build.Clamp01(asked.Crowding);
            var limbs = new List<Limb>();
            var clumps = new List<Clump>();
            var model = new TreeModel { Species = species, Seed = seed, Asked = asked, Height = height };
            var girth = height * species.Girth * rng.Range(0.85f, 1.15f) * (1 + 0.35f * age) * (1 - 0.15f * crowd);
            var trunks = rng.Range(species.MinTrunks, species.MaxTrunks + 1);
            var asked0 = (float)Math.Tan(Math.Min(60, Math.Abs(asked.Lean)) * Math.PI / 180) * Math.Sign(asked.Lean);
            Limb main = null;

            for (var k = 0; k < trunks; k++)
            {
                // Its own lean: a little of the kind's, any way, on top of the asked lean along +X.
                var own = rng.Range(0f, species.Lean) * (Math.Abs(asked.Lean) > 1 ? 0.4f : 1) * (float)Math.PI / 180;
                var way = rng.Range(0f, Build.Tau);
                var lean = new V3((float)Math.Cos(way), 0, (float)Math.Sin(way)) * (float)Math.Tan(own);
                var at = new V3(0, -0.35f, 0);
                var share = 1f;
                var radius = girth;

                if (k > 0)
                {
                    // A second stem from the same root, leaning away from the first.
                    var off = new V3((float)Math.Cos(way), 0, (float)Math.Sin(way));
                    at = at + off * girth * 1.1f;
                    lean = lean + off * (float)Math.Tan(rng.Range(10f, 22f) * Math.PI / 180);
                    share = rng.Range(0.6f, 0.88f);
                    radius = girth * rng.Range(0.55f, 0.8f);
                }

                var snapped = rng.Chance(species.Broken);
                var length = height * share * (snapped ? rng.Range(0.45f, 0.8f) : 1);
                var trunk = Trunk(species, at, (V3.Up + lean).Normalized, length, radius, snapped, noise, ref rng, age);
                limbs.Add(trunk);
                main = main ?? trunk;
                Branches(species, trunk, height, length / (height * share), crowd, age, limbs, noise, ref rng);
            }

            // Buttress roots, sweeping out and down into the ground.
            var roots = species.Roots > 0 ? species.Roots + rng.Range(-1, 2) : 0;

            for (var i = 0; i < roots; i++)
            {
                var a = i * Build.Tau / roots + rng.Range(-0.3f, 0.3f);
                var out_ = new V3((float)Math.Cos(a), 0, (float)Math.Sin(a));
                var root = new Limb { Depth = 1, Root = true };
                var reach = girth * rng.Range(2.6f, 4.2f) * (0.7f + species.Flare);
                var p = new V3(0, girth * rng.Range(0.6f, 1.3f), 0) + out_ * girth * 0.4f;
                const int steps = 7;

                for (var s = 0; s <= steps; s++)
                {
                    var t = s / (float)steps;
                    root.Points.Add(p + out_ * (reach * t) + V3.Up * (-girth * 1.2f * t * t) + new V3(0, -0.2f * t, 0));
                    root.Radii.Add(girth * 0.5f * (1 - 0.82f * t));
                }

                root.Length = reach;
                limbs.Add(root);
            }

            Lift(limbs);
            Leaves(species, limbs, height, crowd, asked.Season, noise, ref rng, clumps);
            Fit(limbs, clumps, height);
            Shear(limbs, clumps, height, asked0);
            model.TrunkRadius = main.Radii[Math.Min(main.Radii.Count - 1, 1)];
            model.Top = main.Points[main.Points.Count - 1];
            model.Limbs = limbs.Count;
            model.Clumps = clumps.Count;
            Measure(model, main, clumps, species, crowd);
            model.Lods = new[]
            {
                Mesh(species, model, limbs, clumps, 0, noise),
                Mesh(species, model, limbs, clumps, 1, noise),
                Mesh(species, model, limbs, clumps, 2, noise),
            };

            return model;
        }

        // ------------------------------------------------------------------ The skeleton

        static Limb Trunk(TreeSpecies species, V3 at, V3 dir, float length, float radius, bool snapped, Noise noise, ref Rng rng, float age)
        {
            var trunk = new Limb { Depth = 0, Length = length, Snapped = snapped };
            var steps = Math.Max(8, (int)(length / 1.4f));
            steps = Math.Min(steps, 22);
            var step = length / steps;
            var p = at;
            var wander = species.Wander * (1 + age * 0.6f);
            // A leaning trunk bows back up toward the light over its length.
            var recover = 0.05f + species.Rise * 0.04f;
            var first = dir;

            for (var s = 0; s <= steps; s++)
            {
                var t = s / (float)steps;
                trunk.Points.Add(p);
                var taper = snapped ? 1 - 0.55f * t : (float)Math.Pow(Math.Max(0.02f, 1 - t), species.Taper);
                var flare = 1 + species.Flare * (float)Math.Pow(Math.Max(0, 1 - t * length / (radius * 6 + 0.5f)), 2);
                trunk.Radii.Add(Math.Max(0.015f, radius * taper * flare * (snapped || t < 1 ? 1 : 0.2f)));
                var wobble = new V3(noise.Perlin(t * 3.1f, 1.7f), 0, noise.Perlin(4.3f, t * 3.1f)) * wander;
                // The wander is held near the trunk's first heading, so it bends but does not drift away.
                dir = (dir * 0.8f + first * 0.2f + wobble + V3.Up * (recover * t)).Normalized;
                p = p + dir * step;
            }

            return trunk;
        }

        /// <summary>A branch's length up the crown (tc 0 at the crown's foot, 1 at its top), 0-1.</summary>
        static float Profile(CrownShape shape, float tc)
        {
            switch (shape)
            {
                case CrownShape.Cone: return 0.12f + 0.88f * (float)Math.Pow(1 - tc, 0.9f);
                case CrownShape.Column: return 0.55f + 0.45f * (float)Math.Sin(Math.PI * Math.Min(1, tc * 0.9f + 0.15f));
                case CrownShape.Umbrella: return 0.35f + 0.65f * (float)Math.Sin(Math.PI * (0.25f + 0.6f * tc));
                case CrownShape.Vase: return 0.6f + 0.4f * (float)Math.Sin(Math.PI * (0.2f + 0.6f * tc));
                default: return 0.3f + 0.7f * (float)Math.Sin(Math.PI * (0.18f + 0.72f * tc));
            }
        }

        static void Branches(TreeSpecies species, Limb trunk, float height, float reached, float crowd, float age, List<Limb> limbs, Noise noise, ref Rng rng)
        {
            var crownBase = Math.Min(0.85f, species.CrownBase + crowd * 0.22f * (species.Shape == CrownShape.Cone ? 0.4f : 1));
            // A snapped trunk keeps the branches below the break.
            var top = trunk.Snapped ? 0.98f : 0.96f;
            var count = Math.Max(2, (int)Math.Round(species.Branches * rng.Range(0.8f, 1.2f) * (trunk.Snapped ? 0.75f : 1)));
            var whorl = Math.Max(1, species.Whorl);
            var groups = (count + whorl - 1) / whorl;
            var spin = rng.Range(0f, 360f);

            for (var g = 0; g < groups; g++)
            {
                var tg = crownBase + (top - crownBase) * (g + rng.Range(0.1f, 0.9f)) / groups;
                // Through the crown in the tree's full height, so a snapped trunk keeps the low shape.
                var tc = Build.Clamp01((tg * reached - crownBase) / (1 - crownBase));

                for (var w = 0; w < whorl && g * whorl + w < count; w++)
                {
                    float azimuth;

                    if (species.Whorl > 0)
                    {
                        azimuth = spin + g * 41 + w * 360f / whorl + rng.Range(-18f, 18f);
                    }
                    else
                    {
                        azimuth = spin + (g * whorl + w) * 137.5f + rng.Range(-20f, 20f);
                    }

                    var t = species.Whorl > 0 ? tg : crownBase + (top - crownBase) * ((g * whorl + w) + rng.Range(0.2f, 0.8f)) / count;
                    var start = trunk.At(t, out var trunkDir, out var trunkRadius);
                    var profile = Profile(species.Shape, tc);
                    var length = height * species.Spread * profile * rng.Range(0.75f, 1.15f) * (1 - 0.35f * crowd) * (1 + 0.25f * age * (species.Shape == CrownShape.Umbrella ? 1 : 0));

                    if (length < height * 0.03f)
                    {
                        continue;
                    }

                    // Branches low on a cone hang lower; high ones rise.
                    var angle = species.Angle + rng.Range(-10f, 10f);

                    if (species.Shape == CrownShape.Cone)
                    {
                        angle += 14 * (1 - tc) - 10 * tc;
                    }
                    else if (species.Shape == CrownShape.Umbrella)
                    {
                        angle -= 18 * tc;
                    }

                    var dir = Away(trunkDir, azimuth, angle);
                    var radius = Math.Min(trunkRadius * rng.Range(0.42f, 0.62f), length * 0.07f);
                    var branch = Grow(species, start, dir, length, radius, 1, noise, ref rng, rise: species.Rise, sag: species.Shape == CrownShape.Weeping ? 0.35f : species.Shape == CrownShape.Cone ? 0.2f : 0.05f);
                    limbs.Add(branch);
                    Twigs(species, branch, height, limbs, noise, ref rng);
                }
            }

            // A tree whose crown begins low (a cone, a column) gets small twigs on its top too.
            if (!trunk.Snapped)
            {
                var tip = trunk.At(0.97f, out var dir, out var r);
                var leader = Grow(species, tip, dir, height * 0.06f, r, 1, noise, ref rng, 0.6f, 0);
                limbs.Add(leader);
                Twigs(species, leader, height, limbs, noise, ref rng);
            }
        }

        static void Twigs(TreeSpecies species, Limb parent, float height, List<Limb> limbs, Noise noise, ref Rng rng)
        {
            var depth = parent.Depth + 1;
            var max = species.ThirdLevel ? 3 : 2;

            if (depth > max)
            {
                return;
            }

            var count = depth == 2 ? species.Twigs : Math.Max(2, species.Twigs - 2);
            count = Math.Max(1, (int)Math.Round(count * Math.Min(1.4f, parent.Length / Math.Max(0.5f, height * species.Spread * 0.7f)) * rng.Range(0.8f, 1.2f)));
            var angle = species.TwigAngle;
            // Pines and firs fan their twigs out flat, left and right; broad trees all round.
            var flat = species.Leaves == Leafage.Pads || species.Leaves == Leafage.Sprays;

            for (var i = 0; i < count; i++)
            {
                var t = rng.Range(0.3f, 0.95f);
                var start = parent.At(t, out var dir, out var r);
                float azimuth;

                if (flat)
                {
                    azimuth = (i % 2 == 0 ? 90 : -90) + rng.Range(-25f, 25f);
                }
                else
                {
                    azimuth = i * 137.5f + rng.Range(-30f, 30f);
                }

                var twigDir = Away(dir, azimuth, angle + rng.Range(-12f, 12f), flat);
                var length = parent.Length * (depth == 2 ? 0.48f : 0.42f) * (1.1f - 0.55f * t) * rng.Range(0.8f, 1.2f);

                if (length < 0.15f)
                {
                    continue;
                }

                var twig = Grow(species, start, twigDir, length, Math.Min(r * 0.6f, length * 0.06f), depth, noise, ref rng, species.Rise * 1.2f,
                    species.Shape == CrownShape.Weeping ? 0.5f : species.Leaves == Leafage.Sprays ? 0.35f : 0.05f);
                limbs.Add(twig);
                Twigs(species, twig, height, limbs, noise, ref rng);
            }
        }

        /// <summary>A direction `angle` degrees off `axis`, toward `azimuth` degrees round it (0: the side nearest straight up). Flat: round a level frame.</summary>
        static V3 Away(V3 axis, float azimuth, float angle, bool flat = false)
        {
            // The frame round the axis: "up" is the axis's side that faces the sky.
            var up = V3.Up - axis * V3.Dot(V3.Up, axis);
            up = up.Length < 1e-3f ? Build.Perpendicular(axis) : up.Normalized;
            var side = V3.Cross(axis, up);
            var a = azimuth * (float)Math.PI / 180;
            var toward = flat ? (side * (float)Math.Sin(a) + up * 0.25f * (float)Math.Cos(a)).Normalized : (up * (float)Math.Cos(a) + side * (float)Math.Sin(a)).Normalized;
            var b = angle * (float)Math.PI / 180;

            return (axis * (float)Math.Cos(b) + toward * (float)Math.Sin(b)).Normalized;
        }

        static Limb Grow(TreeSpecies species, V3 start, V3 dir, float length, float radius, int depth, Noise noise, ref Rng rng, float rise, float sag)
        {
            var limb = new Limb { Depth = depth, Length = length };
            var steps = depth == 1 ? 7 : depth == 2 ? 5 : 4;
            var step = length / steps;
            var p = start;
            var seed = rng.Range(0f, 100f);
            var gnarl = species.Gnarl * (depth == 1 ? 0.8f : 1.2f);

            for (var s = 0; s <= steps; s++)
            {
                var t = s / (float)steps;
                limb.Points.Add(p);
                limb.Radii.Add(Math.Max(0.006f, radius * (float)Math.Pow(1 - 0.85f * t, 1.1f)));
                var twist = new V3(noise.Perlin(seed + t * 2.7f, 3.3f), noise.Perlin(seed * 0.7f, t * 2.7f + 9), noise.Perlin(t * 2.7f + 1, seed)) * gnarl;
                // Tips turn up to the light; long thin limbs sag at their ends.
                dir = (dir + twist + V3.Up * (rise * 0.22f) - V3.Up * (sag * t * 0.6f)).Normalized;
                p = p + dir * step;
            }

            return limb;
        }

        // ------------------------------------------------------------------ The leaves

        static void Leaves(TreeSpecies species, List<Limb> limbs, float height, float crowd, float season, Noise noise, ref Rng rng, List<Clump> clumps)
        {
            if (species.Leaves == Leafage.None)
            {
                return;
            }

            var deepest = 0;

            foreach (var limb in limbs)
            {
                deepest = Math.Max(deepest, limb.Root ? 0 : limb.Depth);
            }

            var size = height * species.Clump;
            var turned = Build.Clamp01(species.Turned + season);

            foreach (var limb in limbs)
            {
                if (limb.Root || limb.Depth == 0)
                {
                    continue;
                }

                // Leaves on the finest limbs, and on the tips of the coarser ones.
                var finest = limb.Depth == deepest;
                var count = finest ? Math.Max(1, (int)Math.Round(species.Density * rng.Range(0.7f, 1.4f) * (0.6f + limb.Length / Math.Max(0.5f, height * 0.12f)))) : 1;
                count = Math.Min(count, 4);

                for (var i = 0; i < count; i++)
                {
                    var t = finest ? (count == 1 ? 0.85f : 0.35f + 0.65f * (i + rng.Range(0.2f, 0.8f)) / count) : 1;
                    var at = limb.At(t, out var dir, out _);
                    var s = size * rng.Range(0.75f, 1.25f) * (1 - 0.2f * crowd);
                    var colour = Tint.Mix(species.Leaf, species.Leaf2, rng.Value());

                    if (rng.Chance(turned))
                    {
                        // A turned clump: the accent, shifted toward gold or deeper red.
                        var accent = species.Id == "maple" ? Tint.Mix(species.Accent, Tint.Hex(0xe0902a), rng.Value() * 0.8f) : species.Accent;
                        colour = Tint.Mix(colour, accent, rng.Range(0.6f, 1f));
                    }

                    var flatDir = new V3(dir.X, 0, dir.Z);
                    flatDir = flatDir.Length < 1e-3f ? new V3(1, 0, 0) : flatDir.Normalized;
                    var side = V3.Cross(V3.Up, flatDir);
                    var clump = new Clump { Kind = ClumpKind.Blob, Colour = colour, Size = s };

                    switch (species.Leaves)
                    {
                        case Leafage.Pads:
                            // A flat cloud of needles sitting on the twig.
                            clump.Centre = at + V3.Up * (s * 0.35f);
                            clump.Ax = flatDir * (s * rng.Range(1.3f, 1.8f));
                            clump.Ay = V3.Up * (s * rng.Range(0.38f, 0.5f));
                            clump.Az = side * (s * rng.Range(1.2f, 1.6f));
                            break;
                        case Leafage.Sprays:
                            // A drooping spray along the twig.
                            clump.Centre = at - dir * (s * 0.4f) - V3.Up * (s * 0.15f);
                            clump.Ax = dir * (s * 1.5f);
                            clump.Ay = V3.Up * (s * 0.4f);
                            clump.Az = side * (s * 0.95f);
                            break;
                        case Leafage.Scales:
                            clump.Centre = at;
                            clump.Ax = flatDir * (s * 1.0f);
                            clump.Ay = V3.Up * (s * 1.35f);
                            clump.Az = side * (s * 0.95f);
                            break;
                        case Leafage.Blossom:
                            s *= 0.75f;
                            clump.Centre = at;
                            clump.Ax = flatDir * s;
                            clump.Ay = V3.Up * (s * 0.8f);
                            clump.Az = side * s;
                            break;
                        default:
                            clump.Centre = at + V3.Up * (s * 0.15f);
                            clump.Ax = flatDir * (s * rng.Range(1.0f, 1.25f));
                            clump.Ay = V3.Up * (s * rng.Range(0.75f, 0.95f));
                            clump.Az = side * (s * rng.Range(1.0f, 1.2f));
                            break;
                    }

                    clump.Size = s;
                    clumps.Add(clump);

                    // A willow lets strands hang from its twigs, nearly to the ground.
                    if (species.Leaves == Leafage.Strands && finest)
                    {
                        var strands = rng.Range(2, 4);

                        for (var k = 0; k < strands; k++)
                        {
                            var drop = Math.Max(1f, Math.Min(at.Y - 0.6f, height * rng.Range(0.25f, 0.5f)));
                            var path = new V3[6];
                            var from = at + side * rng.Range(-s, s) + flatDir * rng.Range(-s * 0.5f, s * 0.5f);
                            var drift = flatDir * rng.Range(0.1f, 0.5f);

                            for (var j = 0; j < path.Length; j++)
                            {
                                var u = j / (float)(path.Length - 1);
                                path[j] = from + drift * (float)Math.Sin(u * Math.PI * 0.5f) - V3.Up * (drop * u);
                            }

                            clumps.Add(new Clump { Kind = ClumpKind.Strand, Path = path, Size = s * rng.Range(0.25f, 0.4f), Colour = Tint.Mix(species.Leaf2, species.Leaf, rng.Value()), Centre = path[2] });
                        }
                    }
                }
            }
        }

        /// <summary>Branches of a leaning or drooping tree kept off the ground (roots are meant to go in).</summary>
        static void Lift(List<Limb> limbs)
        {
            foreach (var limb in limbs)
            {
                if (limb.Root || limb.Depth == 0)
                {
                    continue;
                }

                for (var i = 0; i < limb.Points.Count; i++)
                {
                    var p = limb.Points[i];
                    var floor = 0.4f + limb.Radii[i];

                    if (p.Y < floor)
                    {
                        // Squashed up toward the floor softly, so the limb runs along above the ground.
                        limb.Points[i] = new V3(p.X, floor + (p.Y - floor) * 0.15f + (floor - p.Y) * 0.15f, p.Z);
                    }
                }
            }
        }

        /// <summary>
        /// The whole tree scaled about its foot so its top (wood or leaves) is at the asked height:
        /// rising tips and crowns piled on the leader would otherwise make it taller than asked.
        /// </summary>
        static void Fit(List<Limb> limbs, List<Clump> clumps, float height)
        {
            var top = 0f;

            foreach (var limb in limbs)
            {
                foreach (var p in limb.Points)
                {
                    top = Math.Max(top, p.Y);
                }
            }

            foreach (var c in clumps)
            {
                top = Math.Max(top, c.Centre.Y + c.Ay.Length);
            }

            if (top <= 0.5f)
            {
                return;
            }

            var k = Math.Max(0.7f, Math.Min(1.3f, height / top));

            if (Math.Abs(k - 1) < 0.01f)
            {
                return;
            }

            foreach (var limb in limbs)
            {
                for (var i = 0; i < limb.Points.Count; i++)
                {
                    limb.Points[i] = limb.Points[i] * k;
                    limb.Radii[i] *= Math.Min(1, k);
                }

                limb.Length *= k;
            }

            for (var i = 0; i < clumps.Count; i++)
            {
                var c = clumps[i];
                c.Centre = c.Centre * k;
                c.Ax = c.Ax * k;
                c.Ay = c.Ay * k;
                c.Az = c.Az * k;
                c.Size *= k;

                if (c.Path != null)
                {
                    var path = new V3[c.Path.Length];

                    for (var j = 0; j < path.Length; j++)
                    {
                        path[j] = c.Path[j] * k;
                    }

                    c.Path = path;
                }

                clumps[i] = c;
            }
        }

        /// <summary>
        /// The asked lean, laid over the grown tree as a bend along +X: most at the foot, easing
        /// toward the top as the tree turns back to the light. Leaves move with their twigs.
        /// </summary>
        static void Shear(List<Limb> limbs, List<Clump> clumps, float height, float slope)
        {
            if (Math.Abs(slope) < 1e-3f)
            {
                return;
            }

            float Push(float y)
            {
                var t = Math.Max(0, y) / height;

                return slope * height * (t - 0.22f * t * t);
            }

            foreach (var limb in limbs)
            {
                for (var i = 0; i < limb.Points.Count; i++)
                {
                    var p = limb.Points[i];
                    limb.Points[i] = new V3(p.X + Push(p.Y), p.Y, p.Z);
                }
            }

            for (var i = 0; i < clumps.Count; i++)
            {
                var c = clumps[i];
                var dx = Push(c.Centre.Y);
                c.Centre = new V3(c.Centre.X + dx, c.Centre.Y, c.Centre.Z);

                if (c.Path != null)
                {
                    var path = new V3[c.Path.Length];

                    for (var j = 0; j < path.Length; j++)
                    {
                        // A hanging strand hangs straight down from where its twig went.
                        path[j] = new V3(c.Path[j].X + Push(c.Path[0].Y), c.Path[j].Y, c.Path[j].Z);
                    }

                    c.Path = path;
                }

                clumps[i] = c;
            }
        }

        static void Measure(TreeModel model, Limb main, List<Clump> clumps, TreeSpecies species, float crowd)
        {
            if (clumps.Count == 0)
            {
                model.CrownCentre = main.Points[main.Points.Count * 2 / 3];
                model.CrownRadius = model.Height * species.Spread * 0.5f;
                model.CrownBase = model.Height * species.CrownBase;

                return;
            }

            var sum = new V3(0, 0, 0);
            var low = float.MaxValue;

            foreach (var c in clumps)
            {
                sum = sum + c.Centre;
                low = Math.Min(low, c.Centre.Y - c.Size);
            }

            var centre = sum * (1f / clumps.Count);
            var reach = 0f;

            foreach (var c in clumps)
            {
                var d = new V3(c.Centre.X - centre.X, 0, c.Centre.Z - centre.Z).Length + c.Size;
                reach = Math.Max(reach, d);
            }

            model.CrownCentre = centre;
            model.CrownRadius = reach;
            model.CrownBase = Math.Max(1.5f, low);
        }

        // ------------------------------------------------------------------ The meshes

        static MeshData Mesh(TreeSpecies species, TreeModel model, List<Limb> limbs, List<Clump> clumps, int lod, Noise noise)
        {
            var mesh = new MeshData();
            var height = model.Height;

            foreach (var limb in limbs)
            {
                var r0 = limb.Radii[0];

                // Far: only the trunk and the thickest limbs; mid: no finest twigs.
                if (lod == 2 && limb.Depth > 0 && (limb.Depth > 1 || r0 < height * 0.006f))
                {
                    continue;
                }

                if (lod == 1 && limb.Depth >= 3)
                {
                    continue;
                }

                if (lod == 1 && limb.Depth == 2 && r0 < 0.025f)
                {
                    continue;
                }

                int sides;

                if (limb.Depth == 0 || limb.Root)
                {
                    sides = lod == 0 ? Math.Max(8, Math.Min(16, (int)(r0 * 22) + 7)) : lod == 1 ? 6 : 5;
                }
                else
                {
                    sides = lod == 0 ? (r0 > 0.08f ? 7 : r0 > 0.03f ? 5 : 3) : lod == 1 ? (r0 > 0.06f ? 4 : 3) : 3;
                }

                var points = limb.Points;
                var radii = limb.Radii;

                if (lod == 2 && points.Count > 6)
                {
                    // Half the rings.
                    var p2 = new List<V3>();
                    var r2 = new List<float>();

                    for (var i = 0; i < points.Count; i += 2)
                    {
                        p2.Add(points[i]);
                        r2.Add(radii[i]);
                    }

                    if ((points.Count - 1) % 2 != 0)
                    {
                        p2.Add(points[points.Count - 1]);
                        r2.Add(radii[points.Count - 1]);
                    }

                    points = p2;
                    radii = r2;
                }

                var ringCount = points.Count;
                Build.Tube(mesh, points, radii, sides, (ring, around, p, n) => BarkColour(species, p, n, around, noise, height), !limb.Snapped || lod > 0, 0.5f);

                if (limb.Snapped && lod == 0)
                {
                    SnappedTop(mesh, points[ringCount - 1], (points[ringCount - 1] - points[ringCount - 2]).Normalized, radii[ringCount - 1], species, noise);
                }
            }

            var centre = model.CrownCentre;
            var crownR = Math.Max(0.5f, model.CrownRadius);
            var crownLow = model.CrownBase;
            var crownHigh = model.Height;

            Tint LeafColour(Tint baseColour, V3 p, V3 n)
            {
                // Darker deep in the crown and low in it, lighter on top and outside; a little speckle.
                var outside = Build.Clamp01(new V3(p.X - centre.X, 0, p.Z - centre.Z).Length / crownR);
                var up = Build.Clamp01((p.Y - crownLow) / Math.Max(0.5f, crownHigh - crownLow));
                var ao = 0.5f + 0.32f * outside + 0.25f * up;
                var light = 0.78f + 0.3f * n.Y + 0.08f * noise.Perlin(p.X * 1.9f, p.Z * 1.9f + p.Y);

                return Build.Shade(baseColour, ao * light);
            }

            if (lod == 2)
            {
                // A handful of big blobs: the clumps gathered on a coarse grid.
                var cell = Math.Max(1.5f, crownR * 0.8f);
                var groups = new Dictionary<long, (V3 sum, float min_x, float max_x, float min_y, float max_y, float min_z, float max_z, int n, float r, float g, float b)>();

                foreach (var c in clumps)
                {
                    if (c.Kind != ClumpKind.Blob)
                    {
                        continue;
                    }

                    var key = ((long)Math.Floor(c.Centre.X / cell) << 40) ^ ((long)Math.Floor(c.Centre.Y / cell) << 20) ^ (long)Math.Floor(c.Centre.Z / cell);
                    groups.TryGetValue(key, out var gr);
                    var e = Math.Max(c.Ax.Length, c.Az.Length);
                    var ey = c.Ay.Length;

                    if (gr.n == 0)
                    {
                        gr = (new V3(0, 0, 0), float.MaxValue, float.MinValue, float.MaxValue, float.MinValue, float.MaxValue, float.MinValue, 0, 0, 0, 0);
                    }

                    gr.sum = gr.sum + c.Centre;
                    gr.min_x = Math.Min(gr.min_x, c.Centre.X - e);
                    gr.max_x = Math.Max(gr.max_x, c.Centre.X + e);
                    gr.min_y = Math.Min(gr.min_y, c.Centre.Y - ey);
                    gr.max_y = Math.Max(gr.max_y, c.Centre.Y + ey);
                    gr.min_z = Math.Min(gr.min_z, c.Centre.Z - e);
                    gr.max_z = Math.Max(gr.max_z, c.Centre.Z + e);
                    gr.n++;
                    gr.r += c.Colour.R;
                    gr.g += c.Colour.G;
                    gr.b += c.Colour.B;
                    groups[key] = gr;
                }

                foreach (var gr in groups.Values)
                {
                    var mid = new V3((gr.min_x + gr.max_x) / 2, (gr.min_y + gr.max_y) / 2, (gr.min_z + gr.max_z) / 2);
                    var colour = new Tint(gr.r / gr.n, gr.g / gr.n, gr.b / gr.n);
                    var shrink = gr.n == 1 ? 1 : 0.88f;
                    Build.Blob(mesh, mid, new V3((gr.max_x - gr.min_x) / 2 * shrink, 0, 0), new V3(0, (gr.max_y - gr.min_y) / 2 * shrink, 0), new V3(0, 0, (gr.max_z - gr.min_z) / 2 * shrink),
                        1, noise, 0.18f, (p, n) => LeafColour(colour, p, n));
                }

                return mesh;
            }

            var index = 0;

            foreach (var c in clumps)
            {
                index++;

                if (c.Kind == ClumpKind.Strand)
                {
                    if (lod == 1 && index % 3 != 0)
                    {
                        continue;
                    }

                    var path = c.Path;
                    var widths = new float[path.Length];

                    for (var j = 0; j < path.Length; j++)
                    {
                        widths[j] = c.Size * (1 - 0.6f * j / (path.Length - 1f));
                    }

                    var side = Build.Perpendicular(path[path.Length - 1] - path[0]);
                    Build.Ribbon(mesh, path, widths, side, j => LeafColour(c.Colour, path[j], V3.Up));

                    continue;
                }

                var ax = c.Ax;
                var ay = c.Ay;
                var az = c.Az;

                if (lod == 1)
                {
                    // Half the clumps, the rest grown to fill.
                    if (index % 2 == 0)
                    {
                        continue;
                    }

                    ax = ax * 1.25f;
                    ay = ay * 1.15f;
                    az = az * 1.25f;
                }

                var colour = c.Colour;
                Build.Blob(mesh, c.Centre, ax, ay, az, lod == 0 ? 1 : 0, noise, lod == 0 ? 0.28f : 0.15f, (p, n) => LeafColour(colour, p, n));
            }

            return mesh;
        }

        static Tint BarkColour(TreeSpecies species, V3 p, V3 n, float around, Noise noise, float height)
        {
            // Streaks up the bark, darker low and in the cracks; moss on the faces turned up, low down.
            var streak = noise.Perlin(around * 9.3f, p.Y * 0.35f) * 0.5f + 0.5f;
            var grain = noise.Perlin(p.X * 3.1f + p.Y * 2.3f, p.Z * 3.1f) * 0.5f + 0.5f;
            var colour = Tint.Mix(species.BarkDark, species.Bark, 0.35f + 0.55f * streak * (0.7f + 0.3f * grain));

            if (species.Pale && noise.Perlin(around * 4 + p.Y * 0.9f, p.Y * 5.1f) > 0.32f)
            {
                // A birch's dark scars across its white bark.
                colour = Tint.Mix(colour, species.BarkDark, 0.75f);
            }

            var low = Build.Clamp01(1 - p.Y / Math.Max(1.5f, height * 0.35f));
            var moss = species.Moss * low * Build.Clamp01(n.Y * 1.4f + 0.45f) * (0.5f + grain);
            colour = Tint.Mix(colour, species.MossColour, Build.Clamp01(moss));

            return Build.Shade(colour, 0.85f + 0.2f * Build.Clamp01(p.Y / height + 0.1f));
        }

        /// <summary>The splintered end of a snapped trunk: a ring of jagged teeth round the break.</summary>
        static void SnappedTop(MeshData mesh, V3 at, V3 dir, float radius, TreeSpecies species, Noise noise)
        {
            var n1 = Build.Perpendicular(dir);
            var n2 = V3.Cross(n1, dir);
            const int teeth = 9;
            var centre = mesh.Vertex(at.X, at.Y, at.Z, 0.5f, 0.5f, species.BarkDark.R, species.BarkDark.G, species.BarkDark.B);
            mesh.SetNormal(centre, dir.X, dir.Y, dir.Z);
            var light = Tint.Mix(species.Bark, Tint.Hex(0xc8b89a), 0.5f);
            var ring = new int[teeth * 2];

            for (var i = 0; i < teeth * 2; i++)
            {
                var a = i / (float)(teeth * 2) * Build.Tau;
                var r = radius * (i % 2 == 0 ? 0.98f : 0.75f);
                var rise = i % 2 == 0 ? radius * (0.4f + 1.4f * (noise.Perlin(i * 1.7f, 0.3f) * 0.5f + 0.5f)) : radius * 0.1f;
                var p = at + n1 * ((float)Math.Cos(a) * r) + n2 * ((float)Math.Sin(a) * r) + dir * rise;
                ring[i] = mesh.Vertex(p.X, p.Y, p.Z, 0.5f, 0.5f, light.R, light.G, light.B);
                mesh.SetNormal(ring[i], dir.X, dir.Y, dir.Z);
            }

            for (var i = 0; i < teeth * 2; i++)
            {
                mesh.Triangle(centre, ring[(i + 1) % (teeth * 2)], ring[i]);
                mesh.Triangle(centre, ring[i], ring[(i + 1) % (teeth * 2)]);
            }
        }
    }
}
