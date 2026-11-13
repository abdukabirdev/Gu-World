using System.Collections.Generic;

namespace GuWorld.Core.Gen.Flora
{
    /// <summary>How the crown is laid out: the length of the limbs up the trunk.</summary>
    public enum CrownShape
    {
        /// <summary>Longest at the bottom (firs).</summary>
        Cone,
        /// <summary>Longest a third of the way up (camphor, maple, the ancient trees).</summary>
        Dome,
        /// <summary>Short all the way up (cypress).</summary>
        Column,
        /// <summary>Tiers of level limbs, long low and shorter above (the old red pines).</summary>
        Tiers,
        /// <summary>A dome whose twigs hang (willow).</summary>
        Weeping,
        /// <summary>Spreading from low down, wider upwards (plum, the dead trees).</summary>
        Vase,
    }

    /// <summary>How the foliage's cards are set on the twigs.</summary>
    public enum LeafStyle
    {
        /// <summary>Sprays of broad leaves standing off the twig, crossed for volume.</summary>
        Sprays,
        /// <summary>Flat pads lying level on the twig, in layers (pine).</summary>
        Pads,
        /// <summary>Flat fronds drooping along the twig (fir).</summary>
        Fronds,
        /// <summary>Curtains hanging straight down from the twig (willow).</summary>
        Hanging,
        /// <summary>Dense upright sprays hugging the limbs (cypress).</summary>
        Scales,
        /// <summary>None: a dead tree.</summary>
        None,
    }

    /// <summary>
    /// A kind of tree as the numbers the generator grows it from. Every tree of the kind differs (its
    /// seed, its height, its lean, how crowded and how old it grew, how wet its place is), but all read
    /// as the kind. Metres and degrees; "per metre of height" where said.
    /// </summary>
    public sealed class TreeSpecies
    {
        public string Id;
        public string Name;

        // ---- The size.
        public float MinHeight = 20, MaxHeight = 40;

        // ---- The trunk.
        /// <summary>The trunk's radius above its flare, per metre of height (a 40 m pine at 0.03 is 1.2 m: 2.4 m through).</summary>
        public float Girth = 0.03f;
        /// <summary>How the trunk thins upwards (below 1 it stays thick high up; above 1 it thins early).</summary>
        public float Taper = 0.8f;
        /// <summary>How far the foot spreads into the ground (0 a pole; 1 the foot is over twice the trunk).</summary>
        public float Flare = 0.8f;
        /// <summary>Buttresses: ridges round the foot that run out into roots.</summary>
        public int Buttresses = 6;
        /// <summary>How high the buttresses climb the trunk, in trunk radii.</summary>
        public float ButtressHeight = 2.2f;
        /// <summary>How far the roots run over the ground, in trunk radii.</summary>
        public float RootReach = 4;
        /// <summary>How much the trunk wanders (0 a mast).</summary>
        public float Wander = 0.04f;
        /// <summary>How twisted and knotted the limbs are.</summary>
        public float Gnarl = 0.15f;
        /// <summary>Burls and knots swelling the trunk (for an old tree; fewer on young).</summary>
        public int Burls = 4;
        /// <summary>The most a trunk leans of itself, before the wind and the slope.</summary>
        public float Lean = 4;
        /// <summary>Stems from one root.</summary>
        public int MinTrunks = 1, MaxTrunks = 1;
        /// <summary>The chance the top has snapped off, and the share of limbs that are dead stubs.</summary>
        public float Broken, DeadLimbs = 0.06f;

        // ---- The crown.
        /// <summary>Where the limbs begin, as a share of the height, for a tree grown in the open.</summary>
        public float CrownBase = 0.3f;
        public CrownShape Shape = CrownShape.Dome;
        /// <summary>The longest limb's length per metre of height.</summary>
        public float Spread = 0.32f;
        /// <summary>Limbs on the trunk; in whorls of this many at one height (0: one at a time, in a spiral).</summary>
        public int Limbs = 14, Whorl;
        /// <summary>The limbs' angle from the trunk (90 is level).</summary>
        public float Angle = 60;
        /// <summary>A limb's thickness where it leaves the trunk, as a share of the trunk's there (thick limbs are walked on).</summary>
        public float LimbGirth = 0.4f;
        /// <summary>Levels of branching past the trunk (3: limb, branch, twig).</summary>
        public int Levels = 3;
        /// <summary>Branches on each limb (and twigs on each branch), and their angle.</summary>
        public int Twigs = 6;
        public float TwigAngle = 50;
        /// <summary>How the tips turn up to the light (positive) or hang (negative), and how much long limbs sag.</summary>
        public float Rise = 0.25f, Sag = 0.1f;

        // ---- The foliage.
        public LeafStyle Leaves = LeafStyle.Sprays;
        /// <summary>The foliage's texture (Resources/Trees), and a second one mixed in by the season (a maple's green among its red).</summary>
        public string Leaf = "leaf_broad", Leaf2;
        /// <summary>The share of the foliage that wears the first texture when two are given (autumn, blossom).</summary>
        public float Turned = 1;
        /// <summary>A spray's size in metres (the card's side), and sprays at each place on a twig.</summary>
        public float LeafSize = 3, LeafCards = 3;
        /// <summary>The foliage's tint, from this to that, card by card.</summary>
        public Tint LeafTint = new Tint(1, 1, 1), LeafTint2 = new Tint(0.82f, 0.9f, 0.78f);

        // ---- The bark and what lives on it.
        public string Bark = "bark_broad";
        /// <summary>How many metres one tile of the bark's texture covers on the trunk.</summary>
        public float BarkTile = 3;
        public Tint BarkTint = new Tint(1, 1, 1);
        /// <summary>Moss over the foot, the buttresses and the limbs' backs (0 none, 1 thick), before the place's wetness.</summary>
        public float Moss = 0.4f;
        /// <summary>Beards of lichen hanging under the limbs (0 none, 1 many).</summary>
        public float Lichen = 0.2f;
        /// <summary>Shelves of bracket fungus on the trunk.</summary>
        public int Fungus = 3;
        /// <summary>Vines climbing the trunk, leafed with ivy.</summary>
        public int Vines;
        /// <summary>Glowing moss in the bark's hollows (the ancient trees).</summary>
        public bool Glow;
    }

    /// <summary>The kinds of tree of the mountain, by id.</summary>
    public static class Trees
    {
        public static readonly TreeSpecies Pine = new TreeSpecies
        {
            // The old red pine: a mast of plated red bark on spreading buttresses, tiers of level limbs,
            // each carrying flat clouds of needles.
            Id = "pine", Name = "Old red pine", MinHeight = 28, MaxHeight = 62, Girth = 0.04f, Taper = 0.75f, Flare = 0.9f, Buttresses = 6, ButtressHeight = 2.4f,
            RootReach = 4, Wander = 0.03f, Gnarl = 0.22f, Burls = 3, Lean = 5, CrownBase = 0.3f, Shape = CrownShape.Tiers, Spread = 0.3f, Limbs = 17, Angle = 84,
            LimbGirth = 0.36f, Levels = 3, Twigs = 7, TwigAngle = 55, Rise = 0.28f, Sag = 0.04f, Leaves = LeafStyle.Pads, Leaf = "needle_pine", LeafSize = 4.2f, LeafCards = 3,
            LeafTint2 = new Tint(0.8f, 0.9f, 0.85f), Bark = "bark_pine", BarkTile = 2.0f, Moss = 0.35f, Lichen = 0.12f, Fungus = 3,
        };

        public static readonly TreeSpecies Fir = new TreeSpecies
        {
            // A dark spire of the high slopes: whorl on whorl of drooping limbs to the ground, bearded with lichen.
            Id = "fir", Name = "Fir", MinHeight = 32, MaxHeight = 75, Girth = 0.026f, Taper = 0.95f, Flare = 0.55f, Buttresses = 5, ButtressHeight = 1.8f, RootReach = 3,
            Wander = 0.012f, Gnarl = 0.08f, Burls = 1, Lean = 2, CrownBase = 0.12f, Shape = CrownShape.Cone, Spread = 0.2f, Limbs = 52, Angle = 84,
            LimbGirth = 0.2f, Levels = 3, Twigs = 8, TwigAngle = 58, Rise = -0.08f, Sag = 0.22f, Leaves = LeafStyle.Fronds, Leaf = "needle_fir", LeafSize = 3.4f, LeafCards = 3,
            Bark = "bark_broad", BarkTile = 2.6f, BarkTint = new Tint(0.8f, 0.8f, 0.86f), Moss = 0.5f, Lichen = 0.55f, Fungus = 2,
        };

        public static readonly TreeSpecies Cypress = new TreeSpecies
        {
            // A dark flame of a tree, by halls and graves: fibrous red bark, the foliage close about the stem.
            Id = "cypress", Name = "Cypress", MinHeight = 22, MaxHeight = 50, Girth = 0.031f, Taper = 0.9f, Flare = 0.7f, Buttresses = 7, ButtressHeight = 2, RootReach = 2.5f,
            Wander = 0.02f, Gnarl = 0.15f, Burls = 2, Lean = 2, CrownBase = 0.1f, Shape = CrownShape.Column, Spread = 0.085f, Limbs = 30, Angle = 34,
            LimbGirth = 0.24f, Levels = 2, Twigs = 6, TwigAngle = 32, Rise = 0.6f, Sag = 0, Leaves = LeafStyle.Scales, Leaf = "scale_cypress", LeafSize = 2.8f, LeafCards = 3,
            Bark = "bark_broad", BarkTile = 2.2f, BarkTint = new Tint(1.15f, 0.86f, 0.74f), Moss = 0.2f, Lichen = 0.05f, Fungus = 1,
        };

        public static readonly TreeSpecies Camphor = new TreeSpecies
        {
            // The great shade tree of the valleys: a vast furrowed bole on high buttresses, limbs thick as
            // lesser trees held nearly level, a dome of glossy leaves, vines up its sides.
            Id = "camphor", Name = "Camphor", MinHeight = 40, MaxHeight = 80, Girth = 0.044f, Taper = 0.7f, Flare = 1.1f, Buttresses = 7, ButtressHeight = 2.6f, RootReach = 4.5f,
            Wander = 0.05f, Gnarl = 0.24f, Burls = 6, Lean = 5, CrownBase = 0.26f, Shape = CrownShape.Dome, Spread = 0.44f, Limbs = 17, Angle = 68,
            LimbGirth = 0.46f, Levels = 4, Twigs = 6, TwigAngle = 48, Rise = 0.22f, Sag = 0.08f, Leaves = LeafStyle.Sprays, Leaf = "leaf_camphor", LeafSize = 4.8f, LeafCards = 3,
            Bark = "bark_broad", BarkTile = 3.2f, Moss = 0.6f, Lichen = 0.3f, Fungus = 5, Vines = 2,
        };

        public static readonly TreeSpecies Maple = new TreeSpecies
        {
            // A broad maple, turned red and gold: the colour of the mountain's autumn.
            Id = "maple", Name = "Maple", MinHeight = 18, MaxHeight = 40, Girth = 0.036f, Taper = 0.85f, Flare = 0.6f, Buttresses = 5, ButtressHeight = 1.6f, RootReach = 3.5f,
            Wander = 0.07f, Gnarl = 0.2f, Burls = 3, Lean = 6, CrownBase = 0.26f, Shape = CrownShape.Dome, Spread = 0.4f, Limbs = 10, Angle = 55,
            LimbGirth = 0.46f, Levels = 4, Twigs = 6, TwigAngle = 48, Rise = 0.25f, Sag = 0.06f, Leaves = LeafStyle.Sprays, Leaf = "leaf_maple_red", Leaf2 = "leaf_maple_green", Turned = 0.85f,
            LeafSize = 3.2f, LeafCards = 3, LeafTint2 = new Tint(0.9f, 0.86f, 0.8f), Bark = "bark_smooth", BarkTile = 1.8f, Moss = 0.3f, Lichen = 0.1f, Fungus = 2,
        };

        public static readonly TreeSpecies Plum = new TreeSpecies
        {
            // A crooked old plum of the gardens and the lower slopes, in blossom on bare black wood.
            Id = "plum", Name = "Plum", MinHeight = 6, MaxHeight = 14, Girth = 0.034f, Taper = 0.9f, Flare = 0.35f, Buttresses = 3, ButtressHeight = 1.2f, RootReach = 3,
            Wander = 0.2f, Gnarl = 0.5f, Burls = 3, Lean = 12, MinTrunks = 1, MaxTrunks = 2, CrownBase = 0.26f, Shape = CrownShape.Vase, Spread = 0.5f, Limbs = 7, Angle = 48,
            LimbGirth = 0.5f, Levels = 3, Twigs = 6, TwigAngle = 52, Rise = 0.3f, Sag = 0.04f, Leaves = LeafStyle.Sprays, Leaf = "blossom_plum", LeafSize = 1.9f, LeafCards = 3,
            LeafTint2 = new Tint(0.95f, 0.9f, 0.92f), Bark = "bark_smooth", BarkTile = 1.1f, BarkTint = new Tint(0.8f, 0.76f, 0.78f), Moss = 0.3f, Lichen = 0.05f, Fungus = 1,
        };

        public static readonly TreeSpecies Willow = new TreeSpecies
        {
            // A great willow over the water: a short leaning bole, limbs flung wide, curtains to the ground.
            Id = "willow", Name = "Willow", MinHeight = 16, MaxHeight = 34, Girth = 0.042f, Taper = 0.85f, Flare = 0.7f, Buttresses = 5, ButtressHeight = 1.6f, RootReach = 4,
            Wander = 0.12f, Gnarl = 0.32f, Burls = 5, Lean = 14, CrownBase = 0.24f, Shape = CrownShape.Weeping, Spread = 0.42f, Limbs = 9, Angle = 50,
            LimbGirth = 0.5f, Levels = 3, Twigs = 7, TwigAngle = 40, Rise = 0.35f, Sag = 0.25f, Leaves = LeafStyle.Hanging, Leaf = "hang_willow", LeafSize = 4.6f, LeafCards = 2,
            Bark = "bark_broad", BarkTile = 2.4f, BarkTint = new Tint(0.95f, 0.92f, 0.8f), Moss = 0.5f, Lichen = 0.1f, Fungus = 4,
        };

        public static readonly TreeSpecies Birch = new TreeSpecies
        {
            // A tall white birch of the cold slopes, light-crowned, its twigs drooping.
            Id = "birch", Name = "Birch", MinHeight = 20, MaxHeight = 42, Girth = 0.016f, Taper = 0.95f, Flare = 0.3f, Buttresses = 3, ButtressHeight = 1.4f, RootReach = 3,
            Wander = 0.06f, Gnarl = 0.12f, Burls = 1, Lean = 6, MinTrunks = 1, MaxTrunks = 3, CrownBase = 0.34f, Shape = CrownShape.Dome, Spread = 0.24f, Limbs = 18, Angle = 40,
            LimbGirth = 0.3f, Levels = 3, Twigs = 5, TwigAngle = 44, Rise = 0.1f, Sag = 0.38f, Leaves = LeafStyle.Sprays, Leaf = "leaf_birch", LeafSize = 1.9f, LeafCards = 2,
            Bark = "bark_birch", BarkTile = 1.6f, Moss = 0.08f, Lichen = 0.05f, Fungus = 2,
        };

        public static readonly TreeSpecies Deadwood = new TreeSpecies
        {
            // A dead giant, silvered and charred: often snapped, its limbs bare stubs, bearded with lichen, shelved with fungus.
            Id = "deadwood", Name = "Deadwood", MinHeight = 15, MaxHeight = 45, Girth = 0.042f, Taper = 0.8f, Flare = 0.8f, Buttresses = 6, ButtressHeight = 2, RootReach = 3.5f,
            Wander = 0.06f, Gnarl = 0.4f, Burls = 5, Lean = 8, Broken = 0.6f, DeadLimbs = 0.45f, CrownBase = 0.22f, Shape = CrownShape.Vase, Spread = 0.26f, Limbs = 14, Angle = 62,
            LimbGirth = 0.34f, Levels = 2, Twigs = 4, TwigAngle = 50, Rise = 0.15f, Sag = 0.05f, Leaves = LeafStyle.None, Bark = "bark_dead", BarkTile = 2.6f, Moss = 0.2f, Lichen = 0.6f, Fungus = 7,
        };

        public static readonly TreeSpecies Ancient = new TreeSpecies
        {
            // A tree older than the clans: a hundred metres of it, a bole like a tower on roots like walls, its
            // limbs a forest in the air; moss, vines, fungus, and a faint glow in its hollows.
            Id = "ancient", Name = "Ancient tree", MinHeight = 85, MaxHeight = 110, Girth = 0.041f, Taper = 0.68f, Flare = 1.15f, Buttresses = 9, ButtressHeight = 3, RootReach = 5.5f,
            Wander = 0.04f, Gnarl = 0.3f, Burls = 10, Lean = 3, CrownBase = 0.24f, Shape = CrownShape.Dome, Spread = 0.42f, Limbs = 21, Angle = 70,
            LimbGirth = 0.48f, Levels = 4, Twigs = 7, TwigAngle = 48, Rise = 0.25f, Sag = 0.08f, Leaves = LeafStyle.Sprays, Leaf = "leaf_broad", LeafSize = 5.8f, LeafCards = 3,
            LeafTint2 = new Tint(0.75f, 0.86f, 0.74f), Bark = "bark_broad", BarkTile = 3.6f, BarkTint = new Tint(0.9f, 0.88f, 0.84f), Moss = 0.8f, Lichen = 0.6f, Fungus = 9, Vines = 4, Glow = true,
        };

        public static readonly TreeSpecies Bamboo = new TreeSpecies
        {
            // A clump of giant bamboo: jointed green culms arching out from one root, fans of blades at their nodes.
            Id = "bamboo", Name = "Giant bamboo", MinHeight = 10, MaxHeight = 24, Girth = 0.0065f, Taper = 0.5f, Flare = 0, Buttresses = 0, RootReach = 0, Wander = 0.02f, Gnarl = 0.02f,
            Burls = 0, Lean = 7, MinTrunks = 8, MaxTrunks = 16, DeadLimbs = 0, CrownBase = 0.38f, Shape = CrownShape.Column, Spread = 0.1f, Limbs = 12, Angle = 55,
            LimbGirth = 0.3f, Levels = 1, Twigs = 0, Rise = 0, Sag = 0.3f, Leaves = LeafStyle.Sprays, Leaf = "leaf_bamboo", LeafSize = 2, LeafCards = 2,
            Bark = "bark_bamboo", BarkTile = 0.8f, Moss = 0, Lichen = 0, Fungus = 0,
        };

        /// <summary>Every kind of tree, in a fixed order (variants are numbered by it).</summary>
        public static readonly TreeSpecies[] All = { Pine, Fir, Cypress, Camphor, Maple, Plum, Willow, Birch, Deadwood, Ancient, Bamboo };

        static Dictionary<string, TreeSpecies> byId;

        public static TreeSpecies Get(string id)
        {
            if (byId == null)
            {
                var map = new Dictionary<string, TreeSpecies>();

                foreach (var species in All)
                {
                    map[species.Id] = species;
                }

                byId = map;
            }

            return byId.TryGetValue(id, out var found) ? found : null;
        }
    }
}
