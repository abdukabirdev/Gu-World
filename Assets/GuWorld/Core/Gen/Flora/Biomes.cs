using System;
using System.Collections.Generic;

namespace GuWorld.Core.Gen.Flora
{
    /// <summary>
    /// What a biome grows and how: its trees (kinds and shares), how many and how big, how crowded
    /// they stand, how hard the wind bends them, how old they are, and what grows under them. The
    /// numbers are the biome's own; the world's fields (<see cref="Woods"/>) then push them about
    /// from place to place — a grove of giants here, a wind-bent ridge there.
    /// </summary>
    public sealed class BiomeProfile
    {
        public Biome Biome;
        public string Name;
        /// <summary>The kinds of tree and their shares.</summary>
        public (string kind, float share)[] Trees = new (string, float)[0];
        /// <summary>Trees per 1000 m² where the biome is thickest.</summary>
        public float Density;
        /// <summary>Where in each kind's range of heights its trees stand (0 its smallest, 1 its tallest), and how widely they spread about that.</summary>
        public float Stature = 0.5f, StatureSpread = 0.3f;
        /// <summary>How crowded the trees grew (0 open, 1 a dense wood): narrow crowns, high branches.</summary>
        public float Crowding = 0.4f;
        /// <summary>How much the wind leans its trees, in degrees, where it blows hardest.</summary>
        public float Wind = 6;
        /// <summary>How old its trees are (0-1).</summary>
        public float Age = 0.5f;
        /// <summary>How patchy: 0 even cover, 1 thickets and clearings.</summary>
        public float Patchiness = 0.5f;
        /// <summary>Added to every tree's turned leaves (a grove in full autumn, blossom time).</summary>
        public float Season;
        /// <summary>What grows under the trees (shrubs, boulders, reeds), per 1000 m², with sizes.</summary>
        public (Strewn kind, float perThousand, float min, float max)[] Undergrowth = new (Strewn, float, float, float)[0];
        /// <summary>The colour the far land is tinted (r, g, b multipliers) where this biome grows.</summary>
        public Tint FarTint = new Tint(1, 1, 1);
    }

    /// <summary>The biomes' profiles, and the fields across the world that bend them.</summary>
    public static class Biomes
    {
        public static readonly BiomeProfile[] All =
        {
            new BiomeProfile
            {
                Biome = Biome.PineForest, Name = "Pine forest", Density = 2.6f, Stature = 0.55f, Crowding = 0.6f, Wind = 6, Age = 0.5f, Patchiness = 0.6f,
                Trees = new[] { ("pine", 0.78f), ("fir", 0.1f), ("camphor", 0.06f), ("deadwood", 0.06f) },
                Undergrowth = new[] { (Strewn.Boulder, 0.25f, 0.8f, 2.4f), (Strewn.Shrub, 0.8f, 0.7f, 1.3f) },
            },
            new BiomeProfile
            {
                Biome = Biome.Fir, Name = "Fir forest", Density = 3.2f, Stature = 0.6f, StatureSpread = 0.35f, Crowding = 0.8f, Wind = 4, Age = 0.6f, Patchiness = 0.35f,
                Trees = new[] { ("fir", 0.8f), ("pine", 0.08f), ("birch", 0.07f), ("deadwood", 0.05f) },
                Undergrowth = new[] { (Strewn.Boulder, 0.4f, 0.8f, 2.6f), (Strewn.Shrub, 0.5f, 0.6f, 1.1f) },
            },
            new BiomeProfile
            {
                Biome = Biome.Broadleaf, Name = "Broadleaf wood", Density = 2.3f, Stature = 0.5f, Crowding = 0.45f, Wind = 5, Age = 0.5f, Patchiness = 0.55f,
                Trees = new[] { ("camphor", 0.4f), ("plum", 0.22f), ("maple", 0.14f), ("pine", 0.14f), ("birch", 0.1f) },
                Undergrowth = new[] { (Strewn.Shrub, 1.6f, 0.7f, 1.4f), (Strewn.Boulder, 0.1f, 0.6f, 1.6f) },
            },
            new BiomeProfile
            {
                Biome = Biome.Maple, Name = "Maple grove", Density = 2.4f, Stature = 0.55f, Crowding = 0.5f, Wind = 4, Age = 0.6f, Patchiness = 0.4f, Season = 0.1f,
                Trees = new[] { ("maple", 0.75f), ("camphor", 0.15f), ("birch", 0.1f) },
                Undergrowth = new[] { (Strewn.Shrub, 1.2f, 0.6f, 1.2f), (Strewn.Boulder, 0.15f, 0.6f, 1.8f) },
            },
            new BiomeProfile
            {
                Biome = Biome.Bamboo, Name = "Bamboo grove", Density = 3.4f, Stature = 0.6f, Crowding = 0.7f, Wind = 5, Age = 0.4f, Patchiness = 0.3f,
                Trees = new[] { ("bamboo", 1f) },
                Undergrowth = new[] { (Strewn.Shrub, 0.4f, 0.6f, 1.1f) },
            },
            new BiomeProfile
            {
                Biome = Biome.Deadwood, Name = "Dead slopes", Density = 1f, Stature = 0.4f, StatureSpread = 0.4f, Crowding = 0.2f, Wind = 8, Age = 0.9f, Patchiness = 0.7f,
                Trees = new[] { ("deadwood", 0.65f), ("pine", 0.25f), ("birch", 0.1f) },
                Undergrowth = new[] { (Strewn.Boulder, 0.6f, 0.8f, 3f) },
            },
            new BiomeProfile
            {
                Biome = Biome.Rock, Name = "Rock", Density = 0.14f, Stature = 0.25f, Crowding = 0, Wind = 8, Age = 0.8f, Patchiness = 0.8f,
                Trees = new[] { ("pine", 0.8f), ("deadwood", 0.2f) },
                Undergrowth = new[] { (Strewn.Boulder, 1.4f, 1f, 4.5f) },
            },
            new BiomeProfile
            {
                Biome = Biome.Marsh, Name = "Marsh", Density = 0.5f, Stature = 0.35f, Crowding = 0.1f, Wind = 6, Age = 0.6f, Patchiness = 0.7f,
                Trees = new[] { ("willow", 0.55f), ("deadwood", 0.45f) },
                Undergrowth = new[] { (Strewn.Reeds, 4f, 0.7f, 1.4f), (Strewn.Shrub, 0.5f, 0.6f, 1f) },
            },
            new BiomeProfile
            {
                Biome = Biome.Riverbank, Name = "Riverbank", Density = 0.45f, Stature = 0.55f, Crowding = 0.1f, Wind = 4, Age = 0.6f, Patchiness = 0.6f,
                Trees = new[] { ("willow", 0.7f), ("plum", 0.15f), ("birch", 0.15f) },
                Undergrowth = new[] { (Strewn.Reeds, 2.4f, 0.6f, 1.2f), (Strewn.Boulder, 0.6f, 0.6f, 1.8f) },
            },
            new BiomeProfile
            {
                Biome = Biome.Meadow, Name = "Meadow", Density = 0.24f, Stature = 0.5f, Crowding = 0, Wind = 7, Age = 0.6f, Patchiness = 0.8f,
                Trees = new[] { ("plum", 0.35f), ("pine", 0.3f), ("camphor", 0.15f), ("maple", 0.1f), ("birch", 0.1f) },
                Undergrowth = new[] { (Strewn.Shrub, 0.6f, 0.6f, 1.2f), (Strewn.Boulder, 0.12f, 0.8f, 2f) },
            },
        };

        static readonly Dictionary<Biome, BiomeProfile> byBiome = new Dictionary<Biome, BiomeProfile>();

        static Biomes()
        {
            foreach (var profile in All)
            {
                byBiome[profile.Biome] = profile;
            }
        }

        public static BiomeProfile Of(Biome biome) => byBiome.TryGetValue(biome, out var profile) ? profile : null;

        /// <summary>The index of a kind of tree in <see cref="Trees.All"/> (−1 if none).</summary>
        public static int KindIndex(string id)
        {
            for (var i = 0; i < Flora.Trees.All.Length; i++)
            {
                if (Flora.Trees.All[i].Id == id)
                {
                    return i;
                }
            }

            return -1;
        }
    }

    /// <summary>
    /// The fields laid over the whole map that make one wood unlike the next:
    /// <list type="bullet">
    /// <item>grandeur — groves where everything grows taller and older (up to half again), and thin stunted stands;</item>
    /// <item>the wind — its heading drifts slowly across the map, and it blows harder up high and on open ground;</item>
    /// <item>the tree line — above it trees shrink, bend and die back;</item>
    /// <item>the slope — trees on a hillside lean a little out over it.</item>
    /// </list>
    /// Everything is a pure function of the point, so every cell asks the same and gets the same.
    /// </summary>
    public sealed class Woods
    {
        readonly Noise grandeur = new Noise(4409);
        readonly Noise wind = new Noise(4421);
        readonly Noise age = new Noise(4433);

        /// <summary>The prevailing wind's heading in degrees (0 toward +X, 90 toward +Z) across the map.</summary>
        public float WindHeading(Vec2 p) => 20 + 70 * wind.Fbm(p.X * 0.00025f, p.Z * 0.00025f, 2);

        /// <summary>0.75 (a stunted stand) to 1.5 (a grove of giants): how tall things grow here.</summary>
        public float Grandeur(Vec2 p)
        {
            var g = grandeur.Fbm(p.X * 0.0016f + 11, p.Z * 0.0016f - 7, 3);
            // Rare pockets of giants: the field's high crests, sharpened.
            var giants = Noise.Smooth(0.35f, 0.62f, g);

            return 0.82f + 0.3f * Noise.Clamp01(g * 0.5f + 0.5f) + 0.4f * giants;
        }

        /// <summary>How much older than the biome's usual the trees are here (−0.3 to 0.3).</summary>
        public float AgeShift(Vec2 p) => 0.3f * age.Fbm(p.X * 0.003f, p.Z * 0.003f, 2);

        /// <summary>The tree line's effect at a height: 1 well below it, falling to 0.45 above it.</summary>
        public static float TreeLine(float height) => 1 - 0.55f * Noise.Smooth(360, 560, height);

        /// <summary>How hard the wind blows at a height (open, high ground more), 0.6-1.8.</summary>
        public static float Exposure(float height, float slope) => 0.6f + 0.8f * Noise.Smooth(150, 520, height) + 0.4f * Noise.Clamp01(slope * 1.2f);

        /// <summary>
        /// Everything a tree of a profile is asked to be at a point: its height and the way it leans
        /// (degrees, and the world heading it leans toward), how crowded and how old.
        /// </summary>
        public (float height, float lean, float heading, float crowd, float age) Ask(BiomeProfile profile, TreeSpecies kind, Vec2 p, float height, float slope, Vec2 downhill, ref Rng rng)
        {
            var share = Noise.Clamp01(profile.Stature + rng.Range(-profile.StatureSpread, profile.StatureSpread));
            var line = TreeLine(height);
            var grand = Grandeur(p);
            var tall = (kind.MinHeight + (kind.MaxHeight - kind.MinHeight) * share) * grand * line;
            tall = Math.Max(kind.MinHeight * 0.6f, Math.Min(kind.MaxHeight * 1.5f, tall));

            // The wind's lean, more where exposed and above the tree line; the slope's, out over it.
            var exposure = Exposure(height, slope) * (1 + (1 - line) * 1.5f);
            var windLean = profile.Wind * exposure * rng.Range(0.4f, 1.2f);
            var heading = WindHeading(p) * (float)Math.PI / 180;
            var lx = (float)Math.Cos(heading) * windLean;
            var lz = (float)Math.Sin(heading) * windLean;
            var slopeLean = Math.Min(14, slope * 22) * rng.Range(0.3f, 1f);
            lx += downhill.X * slopeLean;
            lz += downhill.Z * slopeLean;
            // Willows reach for the water: the riverbank's tree leans hard, any way.
            if (kind.Id == "willow")
            {
                var a = rng.Range(0f, (float)Math.PI * 2);
                lx += (float)Math.Cos(a) * 12;
                lz += (float)Math.Sin(a) * 12;
            }

            var lean = (float)Math.Sqrt(lx * lx + lz * lz);
            var to = (float)(Math.Atan2(lz, lx) * 180 / Math.PI);
            var crowd = Noise.Clamp01(profile.Crowding + rng.Range(-0.2f, 0.2f));
            var old = Noise.Clamp01(profile.Age + AgeShift(p) + (grand - 1) * 0.6f);

            return (tall, Math.Min(28, lean), to, crowd, old);
        }
    }
}
