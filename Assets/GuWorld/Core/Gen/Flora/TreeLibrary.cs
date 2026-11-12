using System;
using System.Collections.Generic;

namespace GuWorld.Core.Gen.Flora
{
    /// <summary>
    /// The trees the game actually draws: for every kind, a set of grown variants — open-grown and
    /// crowded, standing straight, leaning and leaning hard, a few seeds of each — made once (on a
    /// worker thread) and shared by every tree of the map. A tree of the scatter takes the variant
    /// nearest its asking and is turned so its lean points where its wind and slope push it, and
    /// scaled to its height. So a forest of tens of thousands of trees needs a few hundred meshes,
    /// and no two neighbours look alike.
    /// </summary>
    public sealed class TreeLibrary
    {
        public static readonly float[] Leans = { 0, 11, 23 };
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

        /// <summary>A variant, grown the first time it is asked for (safe from any thread).</summary>
        public TreeModel Get(int kind, int variant)
        {
            lock (grown)
            {
                if (grown[kind, variant] != null)
                {
                    return grown[kind, variant];
                }
            }

            var species = Trees.All[kind];
            var (crowd, lean, seed) = Split(variant);
            var share = Seeds == 1 ? 0.55f : 0.3f + 0.5f * seed / (Seeds - 1);
            var model = TreeGen.Grow(species, 101 + seed * 17 + kind * 7 + crowd * 3 + lean, new TreeParams
            {
                Height = species.MinHeight + (species.MaxHeight - species.MinHeight) * share,
                Lean = Leans[lean],
                Crowding = Crowds[crowd],
                Age = 0.3f + 0.5f * ((seed * 0.618f) % 1),
            });

            lock (grown)
            {
                if (grown[kind, variant] == null)
                {
                    grown[kind, variant] = model;
                }

                return grown[kind, variant];
            }
        }

        /// <summary>Grows every variant now (the game does it on a worker thread at load).</summary>
        public void GrowAll(Func<bool> stop = null)
        {
            for (var kind = 0; kind < Trees.All.Length; kind++)
            {
                for (var v = 0; v < VariantsPerKind; v++)
                {
                    if (stop != null && stop())
                    {
                        return;
                    }

                    Get(kind, v);
                }
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
        /// Where a tree of the scatter stands, turned and scaled: the yaw (degrees, the engine's turn
        /// about the vertical) that points its variant's lean (+X) toward the tree's heading, and the
        /// scale that brings the variant to the tree's height.
        /// </summary>
        public (float yaw, float scale) Place(in Instance tree, TreeModel model)
        {
            var (_, lean, _) = Split(Choose(tree));
            // The engine's turn by yaw takes +X to (cos, 0, −sin): so a heading θ needs yaw −θ.
            var yaw = lean == 0 ? tree.Yaw : -tree.LeanTo + ((uint)tree.Seed % 21) - 10;
            var scale = Math.Max(0.7f, Math.Min(1.45f, tree.Height / Math.Max(1, model.Height)));

            return (yaw, scale);
        }
    }
}
