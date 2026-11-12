using System.Collections.Generic;

namespace GuWorld.Core.Gen.Flora
{
    /// <summary>How the crown is laid out: the length of the branches up the trunk.</summary>
    public enum CrownShape
    {
        /// <summary>Longest at the bottom (firs, young pines).</summary>
        Cone,
        /// <summary>Longest a third of the way up (camphor, maple).</summary>
        Dome,
        /// <summary>Short all the way up (cypress).</summary>
        Column,
        /// <summary>Longest at the top: a flat crown on a bare trunk (old mountain pines).</summary>
        Umbrella,
        /// <summary>A dome whose twigs hang (willow).</summary>
        Weeping,
        /// <summary>Spreading from low down, wider upwards (plum, old deadwood).</summary>
        Vase,
    }

    /// <summary>What the leaves are made as.</summary>
    public enum Leafage
    {
        /// <summary>Lumpy clumps of broad leaves.</summary>
        Broad,
        /// <summary>Flat cloud-like pads of needles on top of the branches (the mountain pines of paintings).</summary>
        Pads,
        /// <summary>Drooping sprays along the branches (firs).</summary>
        Sprays,
        /// <summary>Tight scale-leaf clumps round the trunk (cypress).</summary>
        Scales,
        /// <summary>Small clumps of blossom and a few leaves (plum).</summary>
        Blossom,
        /// <summary>Hanging strands and soft clumps (willow).</summary>
        Strands,
        /// <summary>None: a dead tree.</summary>
        None,
    }

    /// <summary>
    /// A kind of tree as a set of numbers the generator grows from. Every tree of the kind is
    /// different (its seed, its height, its lean, how crowded it grew), but all of them read as the
    /// kind. Heights in metres; angles in degrees; ratios of the tree's height unless said.
    /// </summary>
    public sealed class TreeSpecies
    {
        public string Id;
        public string Name;
        public float MinHeight = 8, MaxHeight = 20;
        /// <summary>The trunk's radius at the base, per metre of height.</summary>
        public float Girth = 0.018f;
        /// <summary>How the trunk thins upwards (1: evenly to a point; more: thick low, thin high).</summary>
        public float Taper = 1.1f;
        /// <summary>How much the base swells into the ground (0 none, 1 twice as wide).</summary>
        public float Flare = 0.4f;
        /// <summary>How much the trunk wanders (0 straight as a mast).</summary>
        public float Wander = 0.06f;
        /// <summary>How twisted and knotted the limbs are (0 smooth).</summary>
        public float Gnarl = 0.1f;
        /// <summary>Where the branches begin, as a share of the height, for a tree grown in the open.</summary>
        public float CrownBase = 0.3f;
        public CrownShape Shape = CrownShape.Dome;
        /// <summary>The longest branch's length per metre of height.</summary>
        public float Spread = 0.35f;
        /// <summary>Main branches on the trunk (for a tree of middle height).</summary>
        public int Branches = 14;
        /// <summary>Branches set in whorls of this many at one height (0: one at a time, in a spiral).</summary>
        public int Whorl;
        /// <summary>The angle of the main branches from the trunk.</summary>
        public float Angle = 55;
        /// <summary>Twigs on each main branch and their angle.</summary>
        public int Twigs = 5;
        public float TwigAngle = 40;
        /// <summary>Whether twigs branch once more (bigger, finer trees).</summary>
        public bool ThirdLevel = true;
        /// <summary>How the tips turn up to the light (positive) or sag (negative).</summary>
        public float Rise = 0.25f;
        /// <summary>The most a trunk leans, before the wind and the slope add theirs.</summary>
        public float Lean = 6;
        /// <summary>Trunks from one root (plums and bamboo stand in clumps).</summary>
        public int MinTrunks = 1, MaxTrunks = 1;
        /// <summary>Buttress roots at the base (big old trees).</summary>
        public int Roots;
        /// <summary>The chance the top has snapped off.</summary>
        public float Broken;

        public Leafage Leaves = Leafage.Broad;
        /// <summary>A leaf clump's radius per metre of height.</summary>
        public float Clump = 0.07f;
        /// <summary>Leaf clumps per twig.</summary>
        public float Density = 1.2f;

        public Tint Bark = Tint.Hex(0x5a4a3c);
        public Tint BarkDark = Tint.Hex(0x2e2620);
        public Tint Leaf = Tint.Hex(0x3e5a2a);
        public Tint Leaf2 = Tint.Hex(0x58702e);
        /// <summary>The leaves in autumn (maples) or the blossom (plums).</summary>
        public Tint Accent = Tint.Hex(0xb03a24);
        /// <summary>How far the leaves have turned to the accent (0 summer green, 1 all turned).</summary>
        public float Turned;
        /// <summary>Moss on the trunk's upper faces low down (0 none).</summary>
        public float Moss = 0.2f;
        public Tint MossColour = Tint.Hex(0x4c6a2c);
        /// <summary>Pale bark with dark scars (birch).</summary>
        public bool Pale;
    }

    /// <summary>The kinds of tree of the mountain, by id.</summary>
    public static class Trees
    {
        public static readonly TreeSpecies Pine = new TreeSpecies
        {
            // The mountain's pine of the old paintings: a straight-ish trunk with orange-brown plates,
            // branches nearly level and turned up at the tips, flat cloud-pads of needles on top.
            Id = "pine", Name = "Mountain pine", MinHeight = 12, MaxHeight = 30, Girth = 0.02f, Taper = 1.2f, Flare = 0.35f,
            Wander = 0.07f, Gnarl = 0.18f, CrownBase = 0.42f, Shape = CrownShape.Umbrella, Spread = 0.3f, Branches = 13, Whorl = 3,
            Angle = 78, Twigs = 4, TwigAngle = 45, ThirdLevel = false, Rise = 0.35f, Lean = 9, Leaves = Leafage.Pads, Clump = 0.06f, Density = 1.4f,
            Bark = Tint.Hex(0x7a4a30), BarkDark = Tint.Hex(0x3a2418), Leaf = Tint.Hex(0x23402a), Leaf2 = Tint.Hex(0x355a32), Moss = 0.15f,
        };

        public static readonly TreeSpecies Fir = new TreeSpecies
        {
            // A dark spire of the high slopes: whorls of drooping branches, longest at the bottom.
            Id = "fir", Name = "Fir", MinHeight = 14, MaxHeight = 34, Girth = 0.016f, Taper = 1f, Flare = 0.3f, Wander = 0.02f, Gnarl = 0.05f,
            CrownBase = 0.06f, Shape = CrownShape.Cone, Spread = 0.25f, Branches = 36, Whorl = 4, Angle = 84, Twigs = 4, TwigAngle = 50,
            ThirdLevel = false, Rise = -0.1f, Lean = 3, Leaves = Leafage.Sprays, Clump = 0.06f, Density = 2f,
            Bark = Tint.Hex(0x4a3e36), BarkDark = Tint.Hex(0x221c18), Leaf = Tint.Hex(0x1a3424), Leaf2 = Tint.Hex(0x274632), Moss = 0.3f,
        };

        public static readonly TreeSpecies Cypress = new TreeSpecies
        {
            // A dark flame of a tree, planted by halls and graves.
            Id = "cypress", Name = "Cypress", MinHeight = 9, MaxHeight = 22, Girth = 0.017f, Taper = 1f, Flare = 0.25f, Wander = 0.03f,
            Gnarl = 0.12f, CrownBase = 0.06f, Shape = CrownShape.Column, Spread = 0.09f, Branches = 22, Angle = 32, Twigs = 2, TwigAngle = 30,
            ThirdLevel = false, Rise = 0.6f, Lean = 3, Leaves = Leafage.Scales, Clump = 0.06f, Density = 1.6f,
            Bark = Tint.Hex(0x6a4a3a), BarkDark = Tint.Hex(0x34241c), Leaf = Tint.Hex(0x1f3a26), Leaf2 = Tint.Hex(0x2c4a2c), Moss = 0.1f,
        };

        public static readonly TreeSpecies Camphor = new TreeSpecies
        {
            // The great shade tree of the valley: a short thick trunk that forks into a huge dome.
            Id = "camphor", Name = "Camphor", MinHeight = 14, MaxHeight = 30, Girth = 0.03f, Taper = 1.4f, Flare = 0.6f, Wander = 0.1f,
            Gnarl = 0.2f, CrownBase = 0.25f, Shape = CrownShape.Dome, Spread = 0.42f, Branches = 9, Angle = 48, Twigs = 6, TwigAngle = 42,
            ThirdLevel = true, Rise = 0.25f, Lean = 8, Roots = 5, Leaves = Leafage.Broad, Clump = 0.07f, Density = 1.1f,
            Bark = Tint.Hex(0x5a4c40), BarkDark = Tint.Hex(0x2a221c), Leaf = Tint.Hex(0x355626), Leaf2 = Tint.Hex(0x58782e), Moss = 0.35f,
        };

        public static readonly TreeSpecies Maple = new TreeSpecies
        {
            // A rounded maple turning red: the colour of the mountain's autumn.
            Id = "maple", Name = "Maple", MinHeight = 8, MaxHeight = 18, Girth = 0.022f, Taper = 1.2f, Flare = 0.35f, Wander = 0.1f,
            Gnarl = 0.15f, CrownBase = 0.3f, Shape = CrownShape.Dome, Spread = 0.4f, Branches = 9, Angle = 50, Twigs = 6, TwigAngle = 45,
            ThirdLevel = true, Rise = 0.2f, Lean = 8, Leaves = Leafage.Broad, Clump = 0.08f, Density = 1.1f,
            Bark = Tint.Hex(0x585048), BarkDark = Tint.Hex(0x2c2824), Leaf = Tint.Hex(0x4a6a26), Leaf2 = Tint.Hex(0x7a8a2a),
            Accent = Tint.Hex(0xb8341e), Turned = 0.8f, Moss = 0.2f,
        };

        public static readonly TreeSpecies Plum = new TreeSpecies
        {
            // A small crooked plum of the gardens and the lower slopes, in blossom.
            Id = "plum", Name = "Plum", MinHeight = 3.5f, MaxHeight = 8, Girth = 0.03f, Taper = 1.3f, Flare = 0.4f, Wander = 0.22f,
            Gnarl = 0.45f, CrownBase = 0.32f, Shape = CrownShape.Vase, Spread = 0.45f, Branches = 6, Angle = 45, Twigs = 5, TwigAngle = 50,
            ThirdLevel = true, Rise = 0.3f, Lean = 14, MinTrunks = 1, MaxTrunks = 2, Leaves = Leafage.Blossom, Clump = 0.07f, Density = 1.1f,
            Bark = Tint.Hex(0x3e3230), BarkDark = Tint.Hex(0x1e1818), Leaf = Tint.Hex(0x5a7a34), Leaf2 = Tint.Hex(0x6e8a3e),
            Accent = Tint.Hex(0xf0c4cc), Turned = 0.75f, Moss = 0.25f,
        };

        public static readonly TreeSpecies Willow = new TreeSpecies
        {
            // A weeping willow leaning over the water.
            Id = "willow", Name = "Willow", MinHeight = 8, MaxHeight = 16, Girth = 0.03f, Taper = 1.3f, Flare = 0.45f, Wander = 0.14f,
            Gnarl = 0.3f, CrownBase = 0.32f, Shape = CrownShape.Weeping, Spread = 0.38f, Branches = 8, Angle = 45, Twigs = 5, TwigAngle = 35,
            ThirdLevel = false, Rise = 0.4f, Lean = 16, Leaves = Leafage.Strands, Clump = 0.06f, Density = 1.2f,
            Bark = Tint.Hex(0x4e4638), BarkDark = Tint.Hex(0x26221c), Leaf = Tint.Hex(0x6a8a3a), Leaf2 = Tint.Hex(0x8aa04a), Moss = 0.3f,
        };

        public static readonly TreeSpecies Birch = new TreeSpecies
        {
            // A pale birch of the cold slopes, thin and light-crowned.
            Id = "birch", Name = "Birch", MinHeight = 10, MaxHeight = 22, Girth = 0.012f, Taper = 1f, Flare = 0.2f, Wander = 0.08f,
            Gnarl = 0.1f, CrownBase = 0.35f, Shape = CrownShape.Dome, Spread = 0.22f, Branches = 12, Angle = 40, Twigs = 5, TwigAngle = 40,
            ThirdLevel = false, Rise = 0.1f, Lean = 7, MinTrunks = 1, MaxTrunks = 3, Leaves = Leafage.Broad, Clump = 0.055f, Density = 1f,
            Bark = Tint.Hex(0xd8d4cc), BarkDark = Tint.Hex(0x2a2826), Leaf = Tint.Hex(0x5a7e30), Leaf2 = Tint.Hex(0x7e9a38),
            Accent = Tint.Hex(0xd8a630), Turned = 0.12f, Moss = 0f, Pale = true,
        };

        public static readonly TreeSpecies Deadwood = new TreeSpecies
        {
            // A dead, silvered snag: often broken, its limbs bare and twisted.
            Id = "deadwood", Name = "Deadwood", MinHeight = 6, MaxHeight = 20, Girth = 0.034f, Taper = 1.2f, Flare = 0.5f, Wander = 0.15f,
            Gnarl = 0.5f, CrownBase = 0.28f, Shape = CrownShape.Vase, Spread = 0.34f, Branches = 10, Angle = 50, Twigs = 4, TwigAngle = 45,
            ThirdLevel = true, Rise = 0.15f, Lean = 12, Broken = 0.5f, Roots = 3, Leaves = Leafage.None,
            Bark = Tint.Hex(0x8a8478), BarkDark = Tint.Hex(0x3a3630), Moss = 0.15f,
        };

        public static readonly TreeSpecies Ancient = new TreeSpecies
        {
            // A tree older than the clan: a vast trunk on buttress roots, its crown a forest of its own.
            Id = "ancient", Name = "Ancient tree", MinHeight = 38, MaxHeight = 55, Girth = 0.045f, Taper = 1.5f, Flare = 0.9f, Wander = 0.1f,
            Gnarl = 0.35f, CrownBase = 0.3f, Shape = CrownShape.Dome, Spread = 0.5f, Branches = 11, Angle = 52, Twigs = 7, TwigAngle = 45,
            ThirdLevel = true, Rise = 0.3f, Lean = 4, Roots = 9, Leaves = Leafage.Broad, Clump = 0.05f, Density = 1.1f,
            Bark = Tint.Hex(0x5c5044), BarkDark = Tint.Hex(0x241e18), Leaf = Tint.Hex(0x2e4c24), Leaf2 = Tint.Hex(0x4a6a2a), Moss = 0.5f,
        };

        /// <summary>Every kind of tree, in a fixed order (variants are numbered by it).</summary>
        public static readonly TreeSpecies[] All = { Pine, Fir, Cypress, Camphor, Maple, Plum, Willow, Birch, Deadwood, Ancient };

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
