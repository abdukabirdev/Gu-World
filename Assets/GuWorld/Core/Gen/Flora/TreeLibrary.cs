using System;
using System.Collections.Generic;

namespace GuWorld.Core.Gen.Flora
{
    /// <summary>
    /// The trees the game actually draws: for every kind, a set of grown variants — open-grown and
    /// crowded, standing straight, leaning and leaning hard, a few seeds of each (each seed its own
    /// height, age, wetness and decay) — grown when first asked for (on a worker thread) and shared
    /// by every tree of the map. A tree of the scatter takes the variant nearest its asking and is
    /// turned so its lean points where its wind and slope push it, and scaled to its height. So a
    /// forest of tens of thousands of great trees needs some dozens of models, and no two
    /// neighbours look alike.
    /// </summary>
    public sealed class TreeLibrary
    {
        public static readonly float[] Leans = { 0, 10, 21 };
        public static readonly float[] Crowds = { 0.15f, 0.75f };
        public readonly int Seeds;
        public int VariantsPerKind => Leans.Length * Crowds.Length * Seeds;

        readonly TreeModel[,] grown;

        public TreeLibrary(int seeds)
        {
            Seeds = Math.Max(1, seeds);
            grown = new TreeModel[Trees.All.Length, VariantsPerKind];
        }

        /// <summary>A variant's asking: (crowding bucket, lean bucket, seed).</summary>
        public (int crowd, int lean, int seed) Split(int variant) => (variant / (Leans.Length * Seeds), variant / Seeds % Leans.Length, variant % Seeds);

        public int Join(int crowd, int lean, int seed) => (crowd * Leans.Length + lean) * Seeds + seed;

        /// <summary>What a variant is asked to be.</summary>
        public TreeParams Asking(int kind, int variant)
        {
            var species = Trees.All[kind];
            var (crowd, lean, seed) = Split(variant);
            var share = Seeds == 1 ? 0.6f : 0.35f + 0.5f * seed / (Seeds - 1);
            // Each seed its own tree: one old and mossed, one younger and clean, one going to ruin.
            float F(float k) => (seed * k + kind * 0.37f + crowd * 0.21f + lean * 0.13f) % 1;

            return new TreeParams
            {
                Height = species.MinHeight + (species.MaxHeight - species.MinHeight) * share,
                Lean = Leans[lean],
                Crowding = Crowds[crowd],
                Age = 0.35f + 0.6f * F(0.618f),
                Wet = -0.3f + 0.9f * F(0.414f),
                Decay = F(0.732f) > 0.7f ? 0.35f * F(0.29f) : 0,
            };
        }

        /// <summary>Whether a variant is grown already.</summary>
        public bool Has(int kind, int variant)
        {
            lock (grown)
            {
                return grown[kind, variant] != null;
            }
        }

        /// <summary>A variant, grown the first time it is asked for (safe from any thread; growing takes a tenth of a second or so for a giant).</summary>
        public TreeModel Get(int kind, int variant)
        {
            lock (grown)
            {
                if (grown[kind, variant] != null)
                {
                    return grown[kind, variant];
                }
            }

            var (crowd, lean, seed) = Split(variant);
            var model = TreeGen.Grow(Trees.All[kind], 101 + seed * 17 + kind * 7 + crowd * 3 + lean, Asking(kind, variant));

            lock (grown)
            {
                if (grown[kind, variant] == null)
                {
                    grown[kind, variant] = model;
                }

                return grown[kind, variant];
            }
        }

        /// <summary>The variant a tree of the scatter wears.</summary>
        public int Choose(in Instance tree)
        {
            var crowd = tree.Crowding > 0.45f ? 1 : 0;
            var lean = 0;
            var best = float.MaxValue;

            for (var i = 0; i < Leans.Length; i++)
            {
                var d = Math.Abs(Leans[i] - tree.Lean);

                if (d < best)
                {
                    best = d;
                    lean = i;
                }
            }

            return Join(crowd, lean, (int)((uint)tree.Seed % (uint)Seeds));
        }

        /// <summary>
        /// How a tree of the scatter stands: the yaw (degrees, the engine's turn about the vertical)
        /// that points its variant's lean (+X) toward the tree's heading, and the scale that brings
        /// a variant of `variantHeight` to the tree's height.
        /// </summary>
        public (float yaw, float scale) Place(in Instance tree, float variantHeight)
        {
            var (_, lean, _) = Split(Choose(tree));
            // The engine's turn by yaw takes +X to (cos, 0, −sin): so a heading θ needs yaw −θ.
            var yaw = lean == 0 ? tree.Yaw : -tree.LeanTo + ((uint)tree.Seed % 21) - 10;
            var scale = Math.Max(0.7f, Math.Min(1.4f, tree.Height / Math.Max(1, variantHeight)));

            return (yaw, scale);
        }
    }
}
