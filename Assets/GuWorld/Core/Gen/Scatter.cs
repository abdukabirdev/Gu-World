using System;
using System.Collections.Generic;
using GuWorld.Core.Gen.Flora;

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
        /// <summary>A generated tree: its kind (<see cref="Instance.Species"/>) and what it was asked to be are in the instance.</summary>
        Tree,
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

        // A generated tree's asking (Kind == Tree): its kind's index in Flora.Trees.All, its height
        // in metres, its lean in degrees and the world heading it leans toward (0 +X, 90 +Z), how
        // crowded and how old it grew.
        public int Species;
        public float Height;
        public float Lean;
        public float LeanTo;
        public float Crowding;
        public float Age;

        public bool IsTree => Kind == Strewn.Tree || Kind <= Strewn.Bamboo;
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

            // Everything placed by hand (the settlements' own people and things are inside them, and the settlements are kept clear anyway).
            foreach (var thing in Atlas.AllThings)
            {
                if (thing.Home == "")
                {
                    keepClear.Add(thing.At, 5, thing.At);
                }
            }

            foreach (var npc in Atlas.AllNpcs)
            {
                if (npc.Home == "")
                {
                    keepClear.Add(npc.At, 4, npc.At);
                }
            }

            foreach (var spawn in Atlas.AllSpawns)
            {
                keepClear.Add(spawn.At, spawn.Radius * 0.5f, spawn.At);
            }
        }

        /// <summary>The woods' fields across the map (grandeur, wind, the tree line).</summary>
        public readonly Woods Woods = new Woods();

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
            var kinds = Flora.Trees.All;

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
                    var profile = Biomes.Of(biome);

                    if (profile == null)
                    {
                        continue;
                    }

                    // Patchy: clearings and thickets, as patchy as the biome is; fewer trees up at the tree line.
                    var patch = Noise.Smooth(-0.35f, 0.45f, density.Fbm(p.X * 0.006f, p.Z * 0.006f, 3));
                    var cover = (1 - profile.Patchiness) + profile.Patchiness * (0.25f + 1.2f * patch);
                    var line = Woods.TreeLine(height);
                    var treeChance = profile.Density * Spacing * Spacing / 1000 * cover * (0.4f + 0.6f * line);
                    var under = profile.Undergrowth;
                    var weights = new float[under.Length + 1];
                    weights[0] = treeChance;
                    var any = treeChance;

                    for (var k = 0; k < under.Length; k++)
                    {
                        weights[k + 1] = under[k].perThousand * Spacing * Spacing / 1000;
                        any += weights[k + 1];
                    }

                    if (!rng.Chance(Math.Min(1, any)))
                    {
                        continue;
                    }

                    var pick = rng.Weighted(weights);

                    if (pick > 0)
                    {
                        var (kind, _, minScale, maxScale) = under[pick - 1];

                        if (!Allowed(p, 2))
                        {
                            continue;
                        }

                        result.Add(new Instance { Kind = kind, At = p, Yaw = rng.Range(0f, 360f), Scale = rng.Range(minScale, maxScale), Seed = (int)(rng.Next() & 0xFFFFFF), Species = -1 });

                        continue;
                    }

                    if (slope > 0.75f || !Allowed(p, 4))
                    {
                        continue;
                    }

                    // Which kind: by the biome's shares.
                    var shares = new float[profile.Trees.Length];

                    for (var k = 0; k < shares.Length; k++)
                    {
                        shares[k] = profile.Trees[k].share;
                    }

                    var species = Flora.Trees.Get(profile.Trees[rng.Weighted(shares)].kind);

                    if (species == null)
                    {
                        continue;
                    }

                    var downhill = Downhill(p);
                    var asked = Woods.Ask(profile, species, p, height, slope, downhill, ref rng);
                    var room = Room(species, asked.height, asked.crowd);
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
                    result.Add(new Instance
                    {
                        Kind = Strewn.Tree, At = p, Yaw = rng.Range(0f, 360f), Scale = 1, Seed = (int)(rng.Next() & 0xFFFFFF),
                        Species = Biomes.KindIndex(species.Id), Height = asked.height, Lean = asked.lean, LeanTo = asked.heading, Crowding = asked.crowd, Age = asked.age,
                    });
                }
            }

            // A rare ancient tree in the deep woods.
            if (rng.Chance(0.006f))
            {
                var p = origin + new Vec2(CellSize * 0.5f, CellSize * 0.5f);
                var biome = surface.BiomeAt(p, land.Height(p), land.Slope(p.X, p.Z), land.Moisture(p));

                if ((biome == Biome.PineForest || biome == Biome.Broadleaf || biome == Biome.Maple) && Allowed(p, 30))
                {
                    var ancient = Flora.Trees.Ancient;
                    result.RemoveAll(other => other.IsTree && Vec2.Distance(other.At, p) < 26);
                    result.Add(new Instance
                    {
                        Kind = Strewn.Tree, At = p, Yaw = rng.Range(0f, 360f), Scale = 1, Seed = (int)(rng.Next() & 0xFFFFFF), Species = Biomes.KindIndex(ancient.Id),
                        Height = rng.Range(ancient.MinHeight, ancient.MaxHeight), Lean = rng.Range(0f, 5f), LeanTo = rng.Range(0f, 360f), Crowding = 0.1f, Age = 1,
                    });
                }
            }

            return result;
        }

        /// <summary>How far apart trees stand, centre to centre: about their crowns' width, less where crowded.</summary>
        static float Room(TreeSpecies species, float height, float crowd)
        {
            var crown = height * species.Spread * (species.Leaves == Leafage.Bamboo ? 4 : 1);

            return Math.Max(2.5f, crown * (1.1f - 0.5f * crowd));
        }

        /// <summary>The level way down the slope at a point (zero on the flat).</summary>
        Vec2 Downhill(Vec2 p)
        {
            var land = world.Land;
            var dx = land.Height(p.X + 2, p.Z) - land.Height(p.X - 2, p.Z);
            var dz = land.Height(p.X, p.Z + 2) - land.Height(p.X, p.Z - 2);
            var length = (float)Math.Sqrt(dx * dx + dz * dz);

            return length < 1e-4f ? new Vec2(0, 0) : new Vec2(-dx / length, -dz / length);
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
