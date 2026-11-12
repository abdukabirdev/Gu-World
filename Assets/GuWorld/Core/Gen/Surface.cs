using System;
using System.Collections.Generic;

namespace GuWorld.Core.Gen
{
    /// <summary>What grows on a piece of land.</summary>
    public enum Biome { Meadow, PineForest, Broadleaf, Bamboo, Deadwood, Rock, Marsh, Riverbank, Settled, Field, Fir, Maple }

    /// <summary>
    /// What the ground is like at a point: its biome (what grows), and the mix of the ten ground
    /// layers the terrain is painted with. The layers, in the terrain's order:
    /// 0 grass, 1 forest floor, 2 flagstones, 3 path, 4 rock, 5 gravel, 6 moss, 7 earth, 8 mud, 9 dirt.
    /// </summary>
    public sealed class Surface
    {
        public const int Layers = 10;
        public const int Grass = 0, ForestFloor = 1, Flagstone = 2, Path = 3, Rock = 4, Gravel = 5, Moss = 6, Earth = 7, Mud = 8, Dirt = 9;
        public static readonly string[] LayerNames = { "Grass", "Forest", "Flagstone", "Path", "Rock", "Gravel", "Moss", "Earth", "Mud", "Dirt" };

        readonly Land land;
        readonly WorldPlan world;
        readonly Noise patches = new Noise(6607);
        readonly SegmentIndex streets = new SegmentIndex(Regions.Half, 64, 8);
        readonly List<(PlaceDef place, Biome biome)> overrides = new List<(PlaceDef, Biome)>();
        readonly ShapeIndex<SettlementPlan> settlements = new ShapeIndex<SettlementPlan>(Regions.Half, 256);
        readonly ShapeIndex<Box> fields = new ShapeIndex<Box>(Regions.Half, 128);
        readonly List<float> streetHalf = new List<float>();
        readonly ShapeIndex<Placed> townBuildings = new ShapeIndex<Placed>(Regions.Half, 32);
        readonly List<bool> streetPaved = new List<bool>();

        public Surface(WorldPlan world)
        {
            this.world = world;
            land = world.Land;

            foreach (var plan in world.Settlements)
            {
                settlements.Add(plan.Centre, plan.Def.Radius + 60, plan);

                foreach (var street in plan.Streets)
                {
                    streets.Add(street.Points, streetHalf.Count);
                    streetHalf.Add(street.HalfWidth);
                    streetPaved.Add(street.Main && (plan.Def.Style == SettlementStyle.Bai || plan.Def.Style == SettlementStyle.GuYue));
                }

                foreach (var field in plan.Fields)
                {
                    fields.Add(field.Centre, field.Radius, field);
                }

                if (plan.Def.City)
                {
                    foreach (var building in plan.Buildings)
                    {
                        if (building.Role != "lamp")
                        {
                            townBuildings.Add(building.Footprint.Centre, building.Footprint.Radius + 4, building);
                        }
                    }
                }
            }

            // The valley's places that are woods.
            Override("forest", Biome.PineForest);
            Override("forest_edge", Biome.Broadleaf);
            Override("grove", Biome.Bamboo);
            Override("ridge", Biome.Deadwood);
            Override("knoll", Biome.Deadwood);
            Override("camp", Biome.Deadwood);
            Override("marsh", Biome.Marsh);
            Override("bear_hollow", Biome.Broadleaf);
            Override("eagle_cliffs", Biome.Rock);
            Override("great_tree", Biome.Broadleaf);
        }

        void Override(string id, Biome biome)
        {
            var place = Valley.Place(id) ?? Regions.Place(id);

            if (place != null)
            {
                overrides.Add((place, biome));
            }
        }

        /// <summary>The settlement a point is in (with a margin), or null.</summary>
        public SettlementPlan SettlementAt(Vec2 p, float margin = 0)
        {
            foreach (var plan in settlements.Near(p, 0))
            {
                if (Vec2.Distance(p, plan.Centre) <= plan.Def.Radius + margin)
                {
                    return plan;
                }
            }

            return null;
        }

        /// <summary>True inside the Gu Yue village or a levelled place of the plan (fights and things happen there: nothing grows).</summary>
        bool Cleared(Vec2 p, out float edge)
        {
            edge = float.MaxValue;
            var cleared = false;

            foreach (var flat in land.Flats)
            {
                var d = Vec2.Distance(p, flat.At) - flat.Radius;
                edge = Math.Min(edge, d);

                if (d < 0)
                {
                    cleared = true;
                }
            }

            return cleared;
        }

        public Biome BiomeAt(Vec2 p, float height, float slope, float moisture)
        {
            if (slope > 0.85f || (height > 540 && slope > 0.4f))
            {
                return Biome.Rock;
            }

            foreach (var field in fields.Near(p, 0))
            {
                if (field.Contains(p))
                {
                    return Biome.Field;
                }
            }

            var settlement = SettlementAt(p, 4);

            // A town is gardens and grass between its houses (the earth round each house is painted by Paint).
            if (settlement != null && settlement.Def.City && Vec2.Distance(p, settlement.Centre) > settlement.Def.Core + 8)
            {
                return Biome.Meadow;
            }

            if (settlement != null || Cleared(p, out _))
            {
                return Biome.Settled;
            }

            foreach (var (place, biome) in overrides)
            {
                if (Vec2.Distance(p, place.At) < place.Radius * (0.85f + 0.3f * (patches.Perlin(p.X * 0.02f, p.Z * 0.02f) * 0.5f + 0.5f)))
                {
                    return biome;
                }
            }

            if (land.Waters != null && land.Waters.Nearest(p, out var toWater, out _, out var width, out _) && toWater < width * 0.5f + 10)
            {
                return Biome.Riverbank;
            }

            if (Vec2.Distance(p, Regions.Marsh) < Regions.MarshRadius)
            {
                return Biome.Marsh;
            }

            var patch = patches.Fbm(p.X * 0.0019f, p.Z * 0.0019f, 3);
            var bamboo = patches.Fbm(p.X * 0.004f + 70, p.Z * 0.004f, 2);

            if (height < 90 && bamboo > 0.42f && moisture > 0.45f)
            {
                return Biome.Bamboo;
            }

            if (height > 420)
            {
                return patch > 0.15f ? Biome.Deadwood : patch > -0.2f ? Biome.Fir : Biome.Rock;
            }

            // A third of the way up the mountain the pines give way to dark firs, in tongues.
            var fir = patches.Fbm(p.X * 0.003f - 40, p.Z * 0.003f + 13, 2);

            if (height > 260 && fir > 0.05f - (height - 260) * 0.002f && patch > -0.3f)
            {
                return Biome.Fir;
            }

            if (height > 110)
            {
                return patch > -0.25f ? Biome.PineForest : Biome.Meadow;
            }

            // The low valleys: broad woods where it is damp (with groves of maples gone red), pines on
            // the drier knolls, meadow between.
            if (moisture > 0.45f && patch > -0.08f)
            {
                return patches.Fbm(p.X * 0.005f + 90, p.Z * 0.005f - 30, 2) > 0.28f ? Biome.Maple : Biome.Broadleaf;
            }

            return patch > 0.12f ? Biome.PineForest : Biome.Meadow;
        }

        /// <summary>The ground layers' weights at a point (they add up to 1), and the biome there.</summary>
        public Biome Paint(Vec2 p, float height, float slope, float[] weights)
        {
            Array.Clear(weights, 0, Layers);
            var moisture = land.Moisture(p);
            var biome = BiomeAt(p, height, slope, moisture);
            var grain = patches.Perlin(p.X * 0.035f, p.Z * 0.035f) * 0.5f + 0.5f;
            weights[Grass] = 1;

            switch (biome)
            {
                case Biome.PineForest:
                    Mix(weights, ForestFloor, 0.6f + 0.35f * grain);
                    Mix(weights, Moss, 0.2f * grain);
                    break;
                case Biome.Fir:
                    Mix(weights, ForestFloor, 0.7f + 0.25f * grain);
                    Mix(weights, Moss, 0.3f * grain);
                    break;
                case Biome.Maple:
                case Biome.Broadleaf:
                    Mix(weights, ForestFloor, 0.45f + 0.3f * grain);
                    Mix(weights, Moss, 0.3f * (1 - grain));
                    break;
                case Biome.Bamboo:
                    Mix(weights, Moss, 0.45f + 0.4f * grain);
                    Mix(weights, Dirt, 0.2f);
                    break;
                case Biome.Deadwood:
                    Mix(weights, Dirt, 0.5f + 0.3f * grain);
                    Mix(weights, Gravel, 0.25f * (1 - grain));
                    break;
                case Biome.Rock:
                    Mix(weights, Rock, 0.7f + 0.3f * grain);
                    Mix(weights, Gravel, 0.25f);
                    break;
                case Biome.Marsh:
                    Mix(weights, Mud, 0.55f + 0.3f * grain);
                    Mix(weights, Moss, 0.35f * (1 - grain));
                    break;
                case Biome.Riverbank:
                    Mix(weights, Gravel, 0.5f);
                    Mix(weights, Mud, 0.3f * grain);
                    break;
                case Biome.Settled:
                    Mix(weights, Earth, 0.85f);
                    break;
                case Biome.Field:
                    // Rows: earth and mud in stripes across the field.
                    foreach (var field in fields.Near(p, 0))
                    {
                        if (field.Contains(p))
                        {
                            var row = (float)Math.Abs(Math.Sin(field.Local(p).X * 1.3f));
                            Mix(weights, Earth, 0.9f);
                            Mix(weights, row > 0.5f ? Grass : Mud, 0.55f);
                            break;
                        }
                    }

                    break;
                default:
                    Mix(weights, Dirt, Noise.Smooth(0.5f, 0.85f, grain) * 0.35f);
                    break;
            }

            // A little rock wherever the land turns steep, whatever grows there.
            Mix(weights, Rock, Noise.Smooth(0.45f, 0.95f, slope));

            // The Gu Yue village: flagstones in its heart, earth round them (as the valley's plan always had it).
            var village = Valley.Place("village");
            var toVillage = Vec2.Distance(p, village.At);
            Mix(weights, Earth, (1 - Noise.Smooth(village.Radius - 12, village.Radius + 12, toVillage)) * 0.9f);
            Mix(weights, Flagstone, 1 - Noise.Smooth(52, 66, toVillage));

            // The settlements' streets and plazas.
            var plan = SettlementAt(p, 2);

            if (plan != null)
            {
                var paved = plan.Def.Style == SettlementStyle.Bai || plan.Def.Style == SettlementStyle.GuYue;
                Mix(weights, paved ? Flagstone : Dirt, 1 - Noise.Smooth(plan.PlazaRadius - 1, plan.PlazaRadius + 2, Vec2.Distance(p, plan.Centre)));
            }

            // A town's yards: beaten earth round each house, flagstones round the clan's.
            foreach (var building in townBuildings.Near(p, 0))
            {
                var d = building.Footprint.Distance(p);
                var clan = Vec2.Distance(building.Footprint.Centre, new Vec2(0, 0)) < 175 || building.Footprint.Radius > 15;
                Mix(weights, clan ? Flagstone : Earth, (1 - Noise.Smooth(0.5f, 3.5f, d)) * 0.9f);
            }

            if (streets.Nearest(p, out var toStreet, out var segment, out _))
            {
                var half = streetHalf[segment.Line];
                Mix(weights, streetPaved[segment.Line] ? Flagstone : Path, 1 - Noise.Smooth(half - 0.6f, half + 1.2f, toStreet));
            }

            // Water's edges and beds.
            if (land.Waters != null && land.Waters.Nearest(p, out var toWater, out _, out var width, out _))
            {
                Mix(weights, Mud, 1 - Noise.Smooth(width * 0.5f + 3, width * 0.5f + 10, toWater));
                Mix(weights, Gravel, 1 - Noise.Smooth(width * 0.5f - 1, width * 0.5f + 3, toWater));
            }

            // The roads, last: beaten earth over everything.
            if (land.Roads != null && land.Roads.Nearest(p, out var toRoad, out _, out var roadHalf, out _))
            {
                Mix(weights, Path, 1 - Noise.Smooth(roadHalf - 0.4f, roadHalf + 1.4f, toRoad));
            }

            return biome;
        }

        /// <summary>Lays a layer over what is there by a share: the others give way.</summary>
        static void Mix(float[] weights, int layer, float share)
        {
            share = Noise.Clamp01(share);

            if (share <= 0)
            {
                return;
            }

            for (var i = 0; i < Layers; i++)
            {
                weights[i] *= 1 - share;
            }

            weights[layer] += share;
        }
    }
}
