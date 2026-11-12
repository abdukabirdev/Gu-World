using System;
using System.Collections.Generic;

namespace GuWorld.Core.Gen
{
    /// <summary>One tuft of the ground's small growth: where, how turned, how big, and of which kind.</summary>
    public struct Tuft
    {
        public float X, Y, Z;
        public float Yaw;
        public float Scale;
        public int Kind;
    }

    /// <summary>
    /// The grass and the small things of the ground, planted by cells near the hero: tufts where the
    /// ground is grass (by the surface's own paint, so it matches the ground's texture), tall grass in the
    /// meadows' damp hollows, flowers in patches, ferns on the forest floor, reeds at the water — none on
    /// roads, streets, in water, on rock or under houses. Deterministic per cell; the meshes are made
    /// here too (two-sided blades whose colour darkens to the root).
    /// </summary>
    public static class Grass
    {
        public const int Tufts = 0, Tall = 1, FlowersWhite = 2, FlowersRed = 3, FlowersYellow = 4, Fern = 5, Reed = 6;
        public const int Kinds = 7;

        static readonly Noise patches = new Noise(8831);

        /// <summary>The tufts of one square cell (`size` metres from its corner), at `density` tufts a square metre where the ground is all grass.</summary>
        public static void Plant(WorldPlan world, float x0, float z0, float size, float density, List<Tuft> into)
        {
            var land = world.Land;
            var weights = new float[Surface.Layers];
            var step = 1 / (float)Math.Sqrt(Math.Max(0.01f, density));
            var columns = Math.Max(1, (int)(size / step));
            var rng = new Rng(Noise.Hash((int)Math.Floor(x0 / size), (int)Math.Floor(z0 / size), 4409));

            for (var j = 0; j < columns; j++)
            {
                for (var i = 0; i < columns; i++)
                {
                    var x = x0 + (i + rng.Value()) * step;
                    var z = z0 + (j + rng.Value()) * step;
                    var p = new Vec2(x, z);
                    var height = land.Height(x, z);
                    var slope = land.Slope(x, z);

                    if (slope > 0.9f)
                    {
                        continue;
                    }

                    var wet = world.Waters.Wet(p, out var level);
                    var nearWater = world.Waters.Nearest(p, out var toWater, out _, out var width, out _) && toWater < width * 0.5f + 3;

                    // Reeds stand in the shallows and at the water's edge; nothing else grows in water.
                    if (wet)
                    {
                        if (level - height < 0.6f && rng.Chance(0.5f))
                        {
                            into.Add(new Tuft { X = x, Y = height, Z = z, Yaw = rng.Range(0f, 360f), Scale = rng.Range(0.8f, 1.3f), Kind = Reed });
                        }

                        continue;
                    }

                    if (world.Roads.Nearest(p, out var toRoad, out _, out var half, out _) && toRoad < half + 0.6f)
                    {
                        continue;
                    }

                    // The surface's paint already keeps grass off streets, yards, rock and the forest floor.
                    world.Surface.Paint(p, height, slope, weights);

                    var grass = weights[Surface.Grass];
                    var forest = weights[Surface.ForestFloor] + weights[Surface.Moss];

                    if (nearWater && rng.Chance(0.25f))
                    {
                        into.Add(new Tuft { X = x, Y = height, Z = z, Yaw = rng.Range(0f, 360f), Scale = rng.Range(0.7f, 1.1f), Kind = Reed });
                        continue;
                    }

                    if (forest > 0.5f)
                    {
                        if (rng.Chance(0.12f * forest))
                        {
                            into.Add(new Tuft { X = x, Y = height, Z = z, Yaw = rng.Range(0f, 360f), Scale = rng.Range(0.7f, 1.3f), Kind = Fern });
                        }

                        continue;
                    }

                    if (!rng.Chance(grass * grass))
                    {
                        continue;
                    }

                    // Patches: flowers where the patch noise is high, tall grass where it is low (and damp).
                    var patch = patches.Perlin(x * 0.06f, z * 0.06f);
                    var kind = Tufts;

                    if (patch > 0.38f && rng.Chance(0.35f))
                    {
                        kind = FlowersWhite + (int)(Math.Abs(patches.Perlin(x * 0.013f + 7, z * 0.013f)) * 7) % 3;
                    }
                    else if (patch < -0.25f && land.Moisture(p) > 0.45f)
                    {
                        kind = Tall;
                    }

                    into.Add(new Tuft { X = x, Y = height, Z = z, Yaw = rng.Range(0f, 360f), Scale = rng.Range(0.75f, 1.25f), Kind = kind });
                }
            }
        }

        // ------------------------------------------------------------------ Meshes

        /// <summary>The mesh of a kind of tuft (its origin at its root, ~0.3–1.6 m tall).</summary>
        public static MeshData Mesh(int kind)
        {
            var k = new Kit();
            var rng = new Rng(77 + kind);

            switch (kind)
            {
                case Tufts:
                    Blades(k, ref rng, 9, 0.28f, 0.45f, 0.22f, Tint.Hex(0x5E8A3A), Tint.Hex(0xA8C870));
                    break;
                case Tall:
                    Blades(k, ref rng, 12, 0.7f, 1.1f, 0.3f, Tint.Hex(0x4E7A36), Tint.Hex(0xC8C880));
                    break;
                case FlowersWhite:
                case FlowersRed:
                case FlowersYellow:
                {
                    Blades(k, ref rng, 6, 0.22f, 0.35f, 0.2f, Tint.Hex(0x5E8A3A), Tint.Hex(0x9AC070));
                    var petal = kind == FlowersWhite ? Tint.Hex(0xF4F0F0) : kind == FlowersRed ? Tint.Hex(0xD84040) : Tint.Hex(0xF0C840);

                    for (var f = 0; f < 4; f++)
                    {
                        var x = rng.Range(-0.15f, 0.15f);
                        var z = rng.Range(-0.15f, 0.15f);
                        var h = rng.Range(0.35f, 0.5f);
                        Blade(k, new V3(x, 0, z), new V3(x + 0.02f, h, z), 0.012f, Tint.Hex(0x4E7A36), Tint.Hex(0x6E9A46));
                        k.Ball(new V3(x + 0.02f, h + 0.03f, z), 0.05f, 0.6f, 6, 3, petal);
                    }

                    break;
                }
                case Fern:
                    for (var f = 0; f < 7; f++)
                    {
                        var yaw = f * 360f / 7 + rng.Range(-15f, 15f);
                        // An arched frond: up from the root, over, and down to its tip.
                        var tip = new V3((float)Math.Sin(yaw * Math.PI / 180) * 0.7f, 0.12f, (float)Math.Cos(yaw * Math.PI / 180) * 0.7f);
                        var mid = new V3(tip.X * 0.45f, 0.42f, tip.Z * 0.45f);
                        var across = V3.Cross(tip, V3.Up).Normalized * 0.07f;
                        Leaf(k, new V3(0, 0.02f, 0), mid, tip, across, Tint.Hex(0x3E6A2E), Tint.Hex(0x7AA850));
                    }

                    break;
                default:
                    Blades(k, ref rng, 8, 1.0f, 1.6f, 0.18f, Tint.Hex(0x5A6E3A), Tint.Hex(0xB8A868));

                    for (var c = 0; c < 2; c++)
                    {
                        var x = rng.Range(-0.1f, 0.1f);
                        var z = rng.Range(-0.1f, 0.1f);
                        k.Post(x, 1.1f + c * 0.15f, z, 0.03f, 0.025f, 0.22f, 5, Tint.Hex(0x6A4A2A));
                    }

                    break;
            }

            return k.Mesh;
        }

        static void Blades(Kit k, ref Rng rng, int count, float low, float high, float spread, Tint root, Tint tip)
        {
            for (var b = 0; b < count; b++)
            {
                var foot = new V3(rng.Range(-spread, spread) * 0.4f, 0, rng.Range(-spread, spread) * 0.4f);
                var lean = new V3(rng.Range(-spread, spread), 0, rng.Range(-spread, spread));
                var top = foot + lean + new V3(0, rng.Range(low, high), 0);
                Blade(k, foot, top, rng.Range(0.025f, 0.045f), root, tip);
            }
        }

        /// <summary>A blade: a tapering two-sided strip from a root to a tip, bent a little in the middle.</summary>
        static void Blade(Kit k, V3 root, V3 tip, float width, Tint low, Tint high)
        {
            var along = tip - root;
            var side = V3.Cross(along, V3.Up);
            side = side.Length > 1e-4f ? side.Normalized * width : new V3(width, 0, 0);
            var mid = root + along * 0.55f + new V3(along.X * 0.15f, 0, along.Z * 0.15f);
            var a = root - side;
            var b = root + side;
            var c = mid + side * 0.6f;
            var d = mid - side * 0.6f;
            var midColour = Tint.Mix(low, high, 0.55f);
            k.Quad(a, d, c, b, midColour);
            k.Quad(b, c, d, a, midColour.Times(0.85f));
            k.Triangle(d, tip, c, high);
            k.Triangle(c, tip, d, high.Times(0.85f));
        }

        static void Leaf(Kit k, V3 root, V3 mid, V3 tip, V3 across, Tint low, Tint high)
        {
            k.Quad(root, mid - across, tip, mid + across, Tint.Mix(low, high, 0.5f));
            k.Quad(mid + across, tip, mid - across, root, Tint.Mix(low, high, 0.4f));
        }
    }
}
