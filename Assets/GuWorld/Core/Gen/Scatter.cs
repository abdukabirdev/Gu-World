using System;
using System.Collections.Generic;

namespace GuWorld.Core.Gen
{
    /// <summary>What can be strewn over the land.</summary>
    public enum Strewn
    {
        Pine, Camphor, Plum, Deadwood, Ancient, Bamboo,
        /// <summary>A boulder (made in code, `Size` across).</summary>
        Boulder,
        /// <summary>A low shrub (made in code).</summary>
        Shrub,
        /// <summary>A stand of reeds by water (made in code).</summary>
        Reeds,
        /// <summary>A small pine in a pot, by a door (a model).</summary>
        Bonsai,
    }

    /// <summary>One thing strewn: what, where, turned how, how big.</summary>
    public struct Instance
    {
        public Strewn Kind;
        public Vec2 At;
        public float Yaw;
        public float Scale;
        /// <summary>A number of its own (a boulder's shape, a shrub's colour).</summary>
        public int Seed;

        public bool IsTree => Kind <= Strewn.Bamboo;
    }

    /// <summary>
    /// The woods, groves, rocks and shrubs of the whole map, made a cell at a time (64 m) so the game
    /// can ask for the cells round the hero only, and the same cell always holds the same things.
    /// Within a cell the candidates sit on a jittered grid; each is kept by the biome's density and
    /// a patchy noise, and only where nothing forbids it — a road, the water, a settlement, a
    /// levelled place, a cliff, a thing of the plan.
    /// </summary>
    public sealed class Scatter
    {
        public const float CellSize = 64;
        const float Spacing = 6;

        public static readonly Dictionary<Strewn, string> Models = new Dictionary<Strewn, string>
        {
            [Strewn.Pine] = "tree_pine_40m", [Strewn.Camphor] = "tree_camphor_70m", [Strewn.Plum] = "tree_plum_8m", [Strewn.Deadwood] = "tree_deadwood_25m",
            [Strewn.Ancient] = "tree_ancient_100m", [Strewn.Bamboo] = "prop_bamboo_grove", [Strewn.Bonsai] = "prop_bonsai_pine",
        };

        readonly WorldPlan world;
        readonly Noise density = new Noise(7703);
        readonly List<Vec2> forbidden = new List<Vec2>();
        readonly ShapeIndex<Vec2> keepClear = new ShapeIndex<Vec2>(Regions.Half, 128);

        public Scatter(WorldPlan world)
        {
            this.world = world;

            foreach (var thing in Valley.Things)
            {
                keepClear.Add(thing.At, 5, thing.At);
            }

            foreach (var npc in Valley.Npcs)
            {
                keepClear.Add(npc.At, 4, npc.At);
            }

            foreach (var spawn in Valley.Spawns)
            {
                keepClear.Add(spawn.At, spawn.Radius * 0.5f, spawn.At);
            }
        }

        /// <summary>The trees of one kind per 1000 m² a biome holds at its densest, by kind.</summary>
        static (Strewn kind, float perThousand, float minScale, float maxScale)[] Mix(Biome biome)
        {
            switch (biome)
            {
                case Biome.PineForest: return new[] { (Strewn.Pine, 2.3f, 0.45f, 0.82f), (Strewn.Camphor, 0.12f, 0.38f, 0.55f), (Strewn.Boulder, 0.25f, 0.8f, 2.4f), (Strewn.Shrub, 0.8f, 0.7f, 1.3f) };
                case Biome.Broadleaf: return new[] { (Strewn.Camphor, 0.45f, 0.38f, 0.58f), (Strewn.Plum, 1.4f, 0.8f, 1.35f), (Strewn.Pine, 0.4f, 0.45f, 0.7f), (Strewn.Shrub, 1.6f, 0.7f, 1.4f) };
                case Biome.Bamboo: return new[] { (Strewn.Bamboo, 3.2f, 0.9f, 1.55f), (Strewn.Shrub, 0.4f, 0.6f, 1.1f) };
                case Biome.Deadwood: return new[] { (Strewn.Deadwood, 0.9f, 0.6f, 1.05f), (Strewn.Boulder, 0.6f, 0.8f, 3f), (Strewn.Pine, 0.25f, 0.4f, 0.6f) };
                case Biome.Rock: return new[] { (Strewn.Boulder, 1.4f, 1f, 4.5f), (Strewn.Pine, 0.12f, 0.35f, 0.55f) };
                case Biome.Marsh: return new[] { (Strewn.Reeds, 4f, 0.7f, 1.4f), (Strewn.Deadwood, 0.25f, 0.5f, 0.8f), (Strewn.Shrub, 0.5f, 0.6f, 1f) };
                case Biome.Riverbank: return new[] { (Strewn.Reeds, 2.4f, 0.6f, 1.2f), (Strewn.Plum, 0.35f, 0.8f, 1.2f), (Strewn.Boulder, 0.6f, 0.6f, 1.8f) };
                case Biome.Meadow: return new[] { (Strewn.Plum, 0.14f, 0.8f, 1.3f), (Strewn.Pine, 0.1f, 0.45f, 0.7f), (Strewn.Shrub, 0.6f, 0.6f, 1.2f), (Strewn.Boulder, 0.12f, 0.8f, 2f) };
                default: return new (Strewn, float, float, float)[0];
            }
        }

        /// <summary>How close to another tree a tree of a kind may stand, at scale 1.</summary>
        static float Room(Strewn kind)
        {
            switch (kind)
            {
                case Strewn.Pine: return 11;
                case Strewn.Camphor: return 22;
                case Strewn.Ancient: return 40;
                case Strewn.Deadwood: return 8;
                case Strewn.Bamboo: return 5;
                case Strewn.Plum: return 6;
                default: return 2.5f;
            }
        }

        /// <summary>Everything strewn in the cell (cx, cz) of 64 m.</summary>
        public List<Instance> Cell(int cx, int cz)
        {
            var result = new List<Instance>();
            var rng = new Rng(cx, cz, 9901);
            var origin = new Vec2(cx * CellSize, cz * CellSize);
            var land = world.Land;
            var surface = world.Surface;
            var trees = new List<(Vec2 at, float room)>();
            var steps = (int)(CellSize / Spacing);

            for (var j = 0; j < steps; j++)
            {
                for (var i = 0; i < steps; i++)
                {
                    var p = origin + new Vec2((i + rng.Value()) * Spacing, (j + rng.Value()) * Spacing);

                    if (Math.Abs(p.X) > Regions.Half - 60 || Math.Abs(p.Z) > Regions.Half - 60)
                    {
                        continue;
                    }

                    var height = land.Height(p);
                    var slope = land.Slope(p.X, p.Z, 2.5f);
                    var biome = surface.BiomeAt(p, height, slope, land.Moisture(p));
                    var mix = Mix(biome);

                    if (mix.Length == 0)
                    {
                        continue;
                    }

                    // Patchy: clearings and thickets.
                    var patch = Noise.Smooth(-0.35f, 0.45f, density.Fbm(p.X * 0.006f, p.Z * 0.006f, 3));
                    var weights = new float[mix.Length];
                    var any = 0f;

                    for (var k = 0; k < mix.Length; k++)
                    {
                        weights[k] = mix[k].perThousand * Spacing * Spacing / 1000 * (mix[k].kind <= Strewn.Bamboo ? 0.3f + 1.2f * patch : 1);
                        any += weights[k];
                    }

                    if (!rng.Chance(Math.Min(1, any)))
                    {
                        continue;
                    }

                    var pick = rng.Weighted(weights);
                    var (kind, _, minScale, maxScale) = mix[pick];
                    var scale = rng.Range(minScale, maxScale);
                    var tree = kind <= Strewn.Bamboo;

                    if (tree && slope > 0.75f || !Allowed(p, tree ? 4 : 2))
                    {
                        continue;
                    }

                    if (tree)
                    {
                        var room = Room(kind) * scale;
                        var crowded = false;

                        foreach (var (at, other) in trees)
                        {
                            if (Vec2.Distance(at, p) < (room + other) * 0.5f)
                            {
                                crowded = true;
                                break;
                            }
                        }

                        if (crowded)
                        {
                            continue;
                        }

                        trees.Add((p, room));
                    }

                    result.Add(new Instance { Kind = kind, At = p, Yaw = rng.Range(0f, 360f), Scale = scale, Seed = (int)(rng.Next() & 0xFFFFFF) });
                }
            }

            // A rare ancient tree in the deep woods.
            if (rng.Chance(0.006f))
            {
                var p = origin + new Vec2(CellSize * 0.5f, CellSize * 0.5f);
                var biome = surface.BiomeAt(p, land.Height(p), land.Slope(p.X, p.Z), land.Moisture(p));

                if ((biome == Biome.PineForest || biome == Biome.Broadleaf) && Allowed(p, 30))
                {
                    result.RemoveAll(other => other.IsTree && Vec2.Distance(other.At, p) < 26);
                    result.Add(new Instance { Kind = Strewn.Ancient, At = p, Yaw = rng.Range(0f, 360f), Scale = rng.Range(0.42f, 0.5f), Seed = 1 });
                }
            }

            return result;
        }

        /// <summary>Whether something may stand at a point, `margin` clear of roads, water, settlements, places and the plan's things.</summary>
        public bool Allowed(Vec2 p, float margin)
        {
            var land = world.Land;

            if (land.Roads != null && land.Roads.Nearest(p, out var toRoad, out _, out var half, out _) && toRoad < half + margin + 1)
            {
                return false;
            }

            if (land.Waters != null)
            {
                if (land.Waters.Nearest(p, out var toWater, out _, out var width, out _) && toWater < width * 0.5f + margin)
                {
                    return false;
                }

                foreach (var pool in land.Waters.Pools)
                {
                    if (Vec2.Distance(p, pool.At) < pool.Radius + margin)
                    {
                        return false;
                    }
                }
            }

            if (world.Surface.SettlementAt(p, margin + 6) != null)
            {
                return false;
            }

            foreach (var flat in land.Flats)
            {
                if (Vec2.Distance(p, flat.At) < flat.Radius + (flat.Cave ? 70 : margin))
                {
                    return false;
                }
            }

            foreach (var _ in keepClear.Near(p, margin))
            {
                return false;
            }

            return true;
        }
    }
}
