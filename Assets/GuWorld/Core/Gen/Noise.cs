using System;

namespace GuWorld.Core.Gen
{
    /// <summary>
    /// Deterministic noise for the world's generation, in plain C# so it runs off the main thread
    /// and in the checks without the engine. Every function is pure: the same seed and point give
    /// the same value on every machine.
    /// </summary>
    public sealed class Noise
    {
        readonly int[] perm = new int[512];
        readonly float[] gradX = new float[256];
        readonly float[] gradZ = new float[256];
        public readonly int Seed;

        public Noise(int seed)
        {
            Seed = seed;
            var random = new Random(seed);
            var p = new int[256];

            for (var i = 0; i < 256; i++)
            {
                p[i] = i;
                var angle = random.NextDouble() * Math.PI * 2;
                gradX[i] = (float)Math.Cos(angle);
                gradZ[i] = (float)Math.Sin(angle);
            }

            for (var i = 255; i > 0; i--)
            {
                var j = random.Next(i + 1);
                var t = p[i];
                p[i] = p[j];
                p[j] = t;
            }

            for (var i = 0; i < 512; i++)
            {
                perm[i] = p[i & 255];
            }
        }

        static float Fade(float t) => t * t * t * (t * (t * 6 - 15) + 10);

        static int Floor(float x) => x >= 0 ? (int)x : (int)x - 1;

        /// <summary>Gradient noise, about −1…1 (rarely beyond ±0.9).</summary>
        public float Perlin(float x, float z)
        {
            var xi = Floor(x);
            var zi = Floor(z);
            var xf = x - xi;
            var zf = z - zi;
            xi &= 255;
            zi &= 255;
            var aa = perm[perm[xi] + zi];
            var ab = perm[perm[xi] + zi + 1];
            var ba = perm[perm[xi + 1] + zi];
            var bb = perm[perm[xi + 1] + zi + 1];
            var u = Fade(xf);
            var v = Fade(zf);
            var x1 = Lerp(gradX[aa] * xf + gradZ[aa] * zf, gradX[ba] * (xf - 1) + gradZ[ba] * zf, u);
            var x2 = Lerp(gradX[ab] * xf + gradZ[ab] * (zf - 1), gradX[bb] * (xf - 1) + gradZ[bb] * (zf - 1), u);

            return Lerp(x1, x2, v) * 1.41421356f;
        }

        /// <summary>Fractal sum of octaves of gradient noise, normalised to about −1…1.</summary>
        public float Fbm(float x, float z, int octaves, float lacunarity = 2.03f, float gain = 0.5f)
        {
            var sum = 0f;
            var amplitude = 1f;
            var total = 0f;

            for (var i = 0; i < octaves; i++)
            {
                sum += Perlin(x, z) * amplitude;
                total += amplitude;
                x = x * lacunarity + 17.3f;
                z = z * lacunarity - 9.1f;
                amplitude *= gain;
            }

            return sum / total;
        }

        /// <summary>Ridged noise, 0…1: sharp crests where the gradient noise crosses zero (mountain spines, gullies).</summary>
        public float Ridged(float x, float z, int octaves, float lacunarity = 2.1f, float gain = 0.5f)
        {
            var sum = 0f;
            var amplitude = 1f;
            var total = 0f;
            var weight = 1f;

            for (var i = 0; i < octaves; i++)
            {
                var n = 1 - Math.Abs(Perlin(x, z));
                n *= n;
                n *= weight;
                weight = Clamp01(n * 1.6f);
                sum += n * amplitude;
                total += amplitude;
                x = x * lacunarity + 31.7f;
                z = z * lacunarity + 5.3f;
                amplitude *= gain;
            }

            return sum / total;
        }

        /// <summary>Billowy noise, 0…1: soft rounded lumps (rolling hills, clouds).</summary>
        public float Billow(float x, float z, int octaves) => Clamp01(Math.Abs(Fbm(x, z, octaves)) * 1.6f);

        /// <summary>A point pushed about by noise, so straight shapes meander.</summary>
        public void Warp(ref float x, ref float z, float scale, float strength)
        {
            var dx = Fbm(x * scale + 3.1f, z * scale - 7.7f, 3);
            var dz = Fbm(x * scale - 11.3f, z * scale + 2.9f, 3);
            x += dx * strength;
            z += dz * strength;
        }

        /// <summary>Cellular noise: the distance to the nearest of points scattered one per cell, and to the next nearest (cells of `size`).</summary>
        public void Cells(float x, float z, float size, out float nearest, out float second, out int cell)
        {
            var cx = Floor(x / size);
            var cz = Floor(z / size);
            nearest = second = float.MaxValue;
            cell = 0;

            for (var j = -1; j <= 1; j++)
            {
                for (var i = -1; i <= 1; i++)
                {
                    var hx = cx + i;
                    var hz = cz + j;
                    var h = Hash(hx, hz, Seed);
                    var px = (hx + (h & 1023) / 1023f) * size;
                    var pz = (hz + ((h >> 10) & 1023) / 1023f) * size;
                    var d = (float)Math.Sqrt((px - x) * (px - x) + (pz - z) * (pz - z));

                    if (d < nearest)
                    {
                        second = nearest;
                        nearest = d;
                        cell = h;
                    }
                    else if (d < second)
                    {
                        second = d;
                    }
                }
            }
        }

        public static float Lerp(float a, float b, float t) => a + (b - a) * t;

        public static float Clamp01(float x) => x < 0 ? 0 : x > 1 ? 1 : x;

        public static float Clamp(float x, float min, float max) => x < min ? min : x > max ? max : x;

        public static float InverseLerp(float a, float b, float x) => a == b ? 0 : Clamp01((x - a) / (b - a));

        /// <summary>0 below `from`, 1 above `to`, a smooth S between (either way round).</summary>
        public static float Smooth(float from, float to, float x)
        {
            var t = from == to ? (x >= to ? 1 : 0) : Clamp01((x - from) / (to - from));

            return t * t * (3 - 2 * t);
        }

        /// <summary>A smoother S (zero slope and curvature at both ends).</summary>
        public static float Smoother(float from, float to, float x)
        {
            var t = from == to ? (x >= to ? 1 : 0) : Clamp01((x - from) / (to - from));

            return t * t * t * (t * (t * 6 - 15) + 10);
        }

        /// <summary>A smooth minimum: like Math.Min, but rounded over `k` where the two meet.</summary>
        public static float SmoothMin(float a, float b, float k)
        {
            if (k <= 0)
            {
                return Math.Min(a, b);
            }

            var h = Clamp01(0.5f + 0.5f * (b - a) / k);

            return Lerp(b, a, h) - k * h * (1 - h);
        }

        public static float SmoothMax(float a, float b, float k) => -SmoothMin(-a, -b, k);

        /// <summary>A well-mixed 32-bit hash of two integers and a seed.</summary>
        public static int Hash(int x, int z, int seed)
        {
            unchecked
            {
                var h = (uint)seed * 0x9E3779B1u;
                h ^= (uint)x * 0x85EBCA77u;
                h = (h << 13) | (h >> 19);
                h ^= (uint)z * 0xC2B2AE3Du;
                h *= 0x27D4EB2Fu;
                h ^= h >> 15;
                h *= 0x85EBCA77u;
                h ^= h >> 13;

                return (int)(h & 0x7FFFFFFF);
            }
        }

        public static float Hash01(int x, int z, int seed) => Hash(x, z, seed) / (float)int.MaxValue;
    }

    /// <summary>A small fast deterministic random for the generators (xorshift), independent of System.Random's version.</summary>
    public struct Rng
    {
        uint state;

        public Rng(int seed)
        {
            state = (uint)seed * 2654435761u + 0x6D2B79F5u;

            if (state == 0)
            {
                state = 0x9E3779B9u;
            }

            Next();
            Next();
        }

        public Rng(int x, int z, int seed) : this(Noise.Hash(x, z, seed)) { }

        public uint Next()
        {
            var x = state;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            state = x;

            return x;
        }

        /// <summary>0 ≤ value &lt; 1.</summary>
        public float Value() => (Next() >> 8) * (1f / 16777216f);

        public float Range(float min, float max) => min + (max - min) * Value();

        /// <summary>min ≤ value &lt; max.</summary>
        public int Range(int min, int max) => max <= min ? min : min + (int)(Next() % (uint)(max - min));

        public bool Chance(float p) => Value() < p;

        public T Pick<T>(T[] items) => items[Range(0, items.Length)];

        /// <summary>An index chosen by weights (all ≥ 0; −1 if they add to nothing).</summary>
        public int Weighted(float[] weights)
        {
            var total = 0f;

            foreach (var w in weights)
            {
                total += Math.Max(0, w);
            }

            if (total <= 0)
            {
                return -1;
            }

            var roll = Value() * total;

            for (var i = 0; i < weights.Length; i++)
            {
                roll -= Math.Max(0, weights[i]);

                if (roll < 0)
                {
                    return i;
                }
            }

            return weights.Length - 1;
        }

        /// <summary>A point in the unit disc, uniformly.</summary>
        public Vec2 InDisc()
        {
            var angle = Value() * (float)Math.PI * 2;
            var r = (float)Math.Sqrt(Value());

            return new Vec2((float)Math.Cos(angle) * r, (float)Math.Sin(angle) * r);
        }
    }
}
