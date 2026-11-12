using System;
using System.Collections.Generic;

namespace GuWorld.Core.Gen
{
    /// <summary>A building the planner placed: a model of the catalogue, where its origin stands, its turn, its footprint on the ground.</summary>
    public sealed class Placed
    {
        public string Model;
        public Vec2 At;
        public float Yaw;
        public float Scale = 1;
        public Box Footprint;
        /// <summary>What it is for in the settlement: "house", "hall", "shop", "inn", "smithy", "herbalist", "nursery", "prop", "wall", "gate", "fence", "lamp".</summary>
        public string Role = "house";

        /// <summary>The point before its door, a step out from its front.</summary>
        public Vec2 Door => Footprint.World(new Vec2(0, Footprint.Half.Z + 1.6f));
    }

    /// <summary>A street of a settlement: its line and half width.</summary>
    public sealed class Street
    {
        public List<Vec2> Points = new List<Vec2>();
        public float HalfWidth;
        public bool Main;
    }

    /// <summary>A spot a person of the settlement stands at, and what they do there.</summary>
    public struct Spot
    {
        public Vec2 At;
        public float Facing;
        public string Role;
    }

    /// <summary>A settlement as laid out: its streets, plaza, buildings, props, wall and gates, fields, and where its people stand.</summary>
    public sealed class SettlementPlan
    {
        public SettlementDef Def;
        public float Level;
        public Vec2 Centre;
        public float PlazaRadius;
        public readonly List<Street> Streets = new List<Street>();
        public readonly List<Placed> Buildings = new List<Placed>();
        public readonly List<Box> Fields = new List<Box>();
        public readonly List<Spot> Spots = new List<Spot>();
        /// <summary>Market stalls made in code (each a spot with the role "stall" for a trader).</summary>
        public int Stalls;
        public readonly List<(Vec2 at, float yaw)> Gates = new List<(Vec2, float)>();

        /// <summary>The ground under a town's houses, each levelled to its own height (none for a village, which is levelled whole).</summary>
        public readonly List<(Box box, float level)> Pads = new List<(Box, float)>();
        /// <summary>How many live there (a town counts its houses' people).</summary>
        public int Population;
        public int Lanes;

        public Placed Find(string role) => Buildings.Find(b => b.Role == role);

        /// <summary>True if a point is at least this far from every building's footprint (people and lanterns stand there).</summary>
        public bool Clear(Vec2 at, float margin)
        {
            foreach (var building in Buildings)
            {
                if (building.Role != "lamp" && building.Footprint.Distance(at) < margin)
                {
                    return false;
                }
            }

            return true;
        }
    }

    /// <summary>
    /// Lays out a settlement: a main street through it the way its roads come in, cross streets for
    /// the bigger ones, a plaza in the middle with the clan's buildings round it, houses on lots along
    /// the streets with their fronts to the street, props between them, a wall or a fence with gaps
    /// for the gates, and fields outside. Deterministic: the same settlement is laid out the same.
    /// </summary>
    public static class Planner
    {
        static readonly Dictionary<SettlementStyle, string[]> Houses = new Dictionary<SettlementStyle, string[]>
        {
            [SettlementStyle.GuYue] = new[] { "kit_two_storey", "kit_courtyard", "kit_artisan", "kit_stilt_bamboo", "kit_shop_house", "prop_poor_house" },
            [SettlementStyle.Bai] = new[] { "kit_courtyard_blue", "kit_shop_house_plaster", "kit_two_storey_stone", "kit_l_kitchen_grey", "kit_slope_terrace", "kit_courtyard_blue", "kit_shop_house_plaster" },
            [SettlementStyle.Xiong] = new[] { "kit_hut_mud_ochre", "kit_artisan_ochre", "kit_stilt_plank", "kit_hut_mud", "kit_l_kitchen", "kit_artisan_ochre" },
            [SettlementStyle.Mortal] = new[] { "kit_hut_mud", "kit_hut_mud_ochre", "kit_stilt_bamboo", "kit_stilt_plank", "prop_poor_house", "kit_hut_mud" },
            [SettlementStyle.Camp] = new[] { "kit_stilt_plank", "prop_poor_house", "kit_hut_mud", "kit_stilt_bamboo" },
        };

        static readonly Dictionary<string, string> Roles = new Dictionary<string, string>
        {
            ["prop_council_hall"] = "hall", ["prop_clan_main_hall"] = "hall", ["prop_ancestral_hall"] = "hall",
            ["prop_shop"] = "shop", ["prop_shop_small_a"] = "shop", ["prop_shop_small_b"] = "shop", ["prop_shop_small_c"] = "shop",
            ["prop_inn"] = "inn", ["prop_tavern"] = "inn", ["prop_eatery"] = "inn", ["prop_teahouse"] = "inn", ["prop_teahouse_octagon"] = "inn",
            ["prop_smithy"] = "smithy", ["prop_herbalist"] = "herbalist", ["prop_gu_nursery"] = "nursery", ["prop_granary"] = "granary",
            ["prop_drying_rack"] = "prop",
        };

        static readonly string[] Clutter = { "prop_barrels", "prop_firewood_stack", "prop_drying_rack", "prop_bench_wood", "prop_market_goods" };

        public static SettlementPlan Plan(SettlementDef def, float level, IList<float> roadsIn, Waters waters)
        {
            var plan = new SettlementPlan { Def = def, Level = level, Centre = def.At };
            var rng = new Rng(def.Seed * 7919 + 13);
            var big = def.Radius >= 90;
            plan.PlazaRadius = big ? 14 : def.Style == SettlementStyle.Camp ? 9 : 7;
            var axis = roadsIn.Count > 0 ? roadsIn[0] : def.Axis;

            // The streets.
            var mainHalf = big ? 3.2f : 2.4f;
            plan.Streets.Add(Bent(plan.Centre, axis, def.Radius * 1.02f, mainHalf, true, ref rng));
            var crosses = new List<float>();

            if (big)
            {
                foreach (var along in new[] { -0.5f, 0.48f })
                {
                    var at = plan.Centre + Vec2.FromYaw(axis) * def.Radius * along;
                    var reach = (float)Math.Sqrt(Math.Max(0, def.Radius * def.Radius - (def.Radius * along) * (def.Radius * along))) * 0.92f;
                    plan.Streets.Add(Bent(at, axis + 90, reach, 2.2f, false, ref rng));
                }

                plan.Streets.Add(Bent(plan.Centre, axis + 90, def.Radius * 0.9f, 2.6f, false, ref rng));
            }
            else if (def.Radius >= 50)
            {
                var at = plan.Centre + Vec2.FromYaw(axis) * def.Radius * 0.25f;
                plan.Streets.Add(Bent(at, axis + 90, def.Radius * 0.7f, 1.8f, false, ref rng));
            }

            // Other roads that come in have a street to the plaza too.
            for (var i = 1; i < roadsIn.Count; i++)
            {
                var diff = Math.Abs(Mod(roadsIn[i] - axis + 180, 360) - 180);

                if (diff > 25 && diff < 155)
                {
                    var line = new List<Vec2> { plan.Centre, plan.Centre + Vec2.FromYaw(roadsIn[i]) * def.Radius * 1.02f };
                    plan.Streets.Add(new Street { Points = Lines.Resample(line, 4), HalfWidth = 2.2f });
                }
            }

            var placed = new List<Box>();

            // The clan's buildings round the plaza, facing it.
            var mustAngle = axis + 45;

            foreach (var model in def.Must)
            {
                if (!Catalog.TryGet(model, out var footprint))
                {
                    continue;
                }

                var role = Roles.TryGetValue(model, out var known) ? known : "house";

                for (var attempt = 0; attempt < 24; attempt++)
                {
                    var angle = mustAngle + attempt * 37 + rng.Range(-8f, 8f);
                    var far = plan.PlazaRadius + 3 + footprint.Deep * 0.5f + attempt * 1.5f;
                    var at = plan.Centre + Vec2.FromYaw(angle) * far;
                    var yaw = Mod(angle + 180, 360);

                    if (TryPlace(plan, placed, model, at, yaw, role, waters, 1.5f))
                    {
                        mustAngle = angle + 70;
                        break;
                    }
                }
            }

            // Houses on lots along the streets, fronts to the street. Every lot of every street is a
            // candidate; they are tried in a shuffled order, so the houses spread over the whole place.
            var styleHouses = Houses[def.Style];
            var houses = 0;
            var lots = new List<(Street street, int side, float along)>();

            foreach (var street in plan.Streets)
            {
                var length = Lines.Length(street.Points);

                foreach (var side in new[] { -1, 1 })
                {
                    for (var along = rng.Range(3f, 7f); along < length - 3; along += 8.5f)
                    {
                        lots.Add((street, side, along));
                    }
                }
            }

            for (var i = lots.Count - 1; i > 0; i--)
            {
                var j = rng.Range(0, i + 1);
                (lots[i], lots[j]) = (lots[j], lots[i]);
            }

            foreach (var (street, side, along) in lots)
            {
                if (houses >= def.Houses)
                {
                    break;
                }

                var point = Lines.At(street.Points, along, out var direction);

                if (Vec2.Distance(point, plan.Centre) < plan.PlazaRadius + 4)
                {
                    continue;
                }

                for (var attempt = 0; attempt < 2; attempt++)
                {
                    var model = rng.Pick(styleHouses);
                    var footprint = Catalog.Get(model);
                    var normal = side < 0 ? direction.Left : direction.Right;
                    var setback = street.HalfWidth + 1.2f + rng.Range(0f, 1.6f);
                    // The front faces the street: the house stands on the normal's side, its +Z towards the street.
                    var yaw = (-normal).Yaw;
                    var centre = point + normal * (setback + footprint.Deep * 0.5f);
                    var origin = centre - (Vec2.FromYaw(yaw + 90) * footprint.MiddleX + Vec2.FromYaw(yaw) * footprint.MiddleZ);

                    if (!TryPlace(plan, placed, model, origin, yaw, "house", waters, 1.6f))
                    {
                        continue;
                    }

                    houses++;

                    // Clutter beside some houses.
                    if (rng.Chance(0.35f))
                    {
                        var prop = rng.Pick(Clutter);
                        var beside = centre + Vec2.FromYaw(yaw + 90) * (footprint.Across * 0.5f + 1.8f) * (rng.Chance(0.5f) ? 1 : -1);
                        TryPlace(plan, placed, prop, beside, yaw, "prop", waters, 0.4f);
                    }

                    break;
                }
            }

            // The plaza: a well (or a fire for a camp), goods, benches.
            if (def.Style == SettlementStyle.Camp)
            {
                TryPlace(plan, placed, "prop_firewood_stack", plan.Centre, axis, "prop", waters, 0, true);
            }
            else
            {
                TryPlace(plan, placed, "prop_village_well", plan.Centre, axis, "prop", waters, 0, true);
            }

            for (var i = 0; i < (big ? 4 : 2); i++)
            {
                var angle = axis + 90 * i + 45;
                var at = plan.Centre + Vec2.FromYaw(angle) * (plan.PlazaRadius - 2.5f);
                TryPlace(plan, placed, i % 2 == 0 ? "prop_market_goods" : "prop_bench_wood", at, Mod(angle + 180, 360), "prop", waters, 0.3f, true);
            }

            // Lamps along the main street.
            var main = plan.Streets[0];
            var mainLength = Lines.Length(main.Points);

            for (var along = 10f; along < mainLength - 6; along += big ? 22 : 30)
            {
                var point = Lines.At(main.Points, along, out var direction);
                var side = ((int)(along / 22) % 2 == 0 ? direction.Left : direction.Right) * (main.HalfWidth + 0.8f);
                TryPlace(plan, placed, "prop_moon_lantern_post", point + side, direction.Yaw, "lamp", waters, 0.2f, true);
            }

            Ring(plan, placed, roadsIn, axis, waters, ref rng);

            if (def.Fields)
            {
                Fields(plan, roadsIn, axis, ref rng);
            }

            // Where people stand: before the clan's buildings, before some doors, on the plaza.
            foreach (var building in plan.Buildings)
            {
                if (building.Role != "house" && building.Role != "prop" && building.Role != "lamp" && building.Role != "wall" && building.Role != "fence")
                {
                    plan.Spots.Add(new Spot { At = building.Door, Facing = building.Yaw, Role = building.Role });
                }
            }

            var doors = 0;

            foreach (var building in plan.Buildings)
            {
                if (building.Role == "house" && doors++ % 2 == 0)
                {
                    plan.Spots.Add(new Spot { At = building.Door, Facing = building.Yaw, Role = "house" });
                }
            }

            // Three spots on the plaza, each turned a little until it is clear of the well and the stalls.
            for (var i = 0; i < 3; i++)
            {
                var angle = axis + 120 * i + 60;

                for (var turn = 0; turn < 12; turn++)
                {
                    var tried = angle + (turn % 2 == 0 ? 1 : -1) * 12 * ((turn + 1) / 2);
                    var at = plan.Centre + Vec2.FromYaw(tried) * (plan.PlazaRadius * 0.6f);

                    if (plan.Clear(at, 0.7f) || turn == 11)
                    {
                        plan.Spots.Add(new Spot { At = at, Facing = Mod(tried + 180, 360), Role = "plaza" });
                        break;
                    }
                }
            }

            return plan;
        }



        static float Mod(float a, float m) => ((a % m) + m) % m;

        /// <summary>A street through a point along a direction, gently bent.</summary>
        static Street Bent(Vec2 through, float yaw, float reach, float half, bool main, ref Rng rng)
        {
            var direction = Vec2.FromYaw(yaw);
            var bend = rng.Range(-0.08f, 0.08f) * reach;
            var points = new List<Vec2>();

            for (var i = 0; i <= 8; i++)
            {
                var t = i / 8f * 2 - 1;
                points.Add(through + direction * (t * reach) + direction.Right * (bend * (1 - t * t)));
            }

            return new Street { Points = Lines.Resample(points, 4), HalfWidth = half, Main = main };
        }

        /// <summary>Places a model if its footprint stays inside the settlement, off the streets, off the water, clear of the others by `gap`.</summary>
        static bool TryPlace(SettlementPlan plan, List<Box> placed, string model, Vec2 origin, float yaw, string role, Waters waters, float gap, bool onStreet = false)
        {
            var footprint = Catalog.Get(model);
            var axisX = Vec2.FromYaw(yaw + 90);
            var axisZ = Vec2.FromYaw(yaw);
            var centre = origin + axisX * footprint.MiddleX + axisZ * footprint.MiddleZ;
            var box = new Box(centre, new Vec2(footprint.Across * 0.5f, footprint.Deep * 0.5f), yaw);
            var radius = plan.Def.Radius;

            foreach (var corner in box.Corners())
            {
                if (Vec2.Distance(corner, plan.Centre) > radius - 1.5f)
                {
                    return false;
                }

                if (waters != null && waters.Nearest(corner, out var toWater, out _, out var width, out _) && toWater < width * 0.5f + 5)
                {
                    return false;
                }
            }

            var grown = box.Grown(gap);

            foreach (var other in placed)
            {
                if (Box.Overlap(grown, other))
                {
                    return false;
                }
            }

            if (!onStreet)
            {
                foreach (var street in plan.Streets)
                {
                    foreach (var corner in grown.Corners())
                    {
                        if (Lines.ToPolyline(corner, street.Points) < street.HalfWidth + 0.4f)
                        {
                            return false;
                        }
                    }

                    // The street must not pass through the box between its corners either.
                    for (var i = 0; i < street.Points.Count; i += 2)
                    {
                        if (grown.Contains(street.Points[i]))
                        {
                            return false;
                        }
                    }
                }

                if (Vec2.Distance(box.Centre, plan.Centre) < plan.PlazaRadius + box.Radius * 0.6f)
                {
                    return false;
                }
            }

            placed.Add(box);
            plan.Buildings.Add(new Placed { Model = model, At = origin, Yaw = yaw, Footprint = box, Role = role });

            return true;
        }

        /// <summary>The wall (or fence) round the settlement, with gaps where the streets leave it, and a gate at the main one.</summary>
        static void Ring(SettlementPlan plan, List<Box> placed, IList<float> roadsIn, float axis, Waters waters, ref Rng rng)
        {
            var def = plan.Def;
            var walled = def.Walled;
            var model = walled ? "fort_wall_segment" : "prop_bamboo_fence";
            var piece = Catalog.Get(model).Across;
            var radius = def.Radius - (walled ? 3 : 1.5f);
            var circumference = 2 * (float)Math.PI * radius;
            var pieces = (int)(circumference / piece);
            var openings = new List<float>();

            foreach (var street in plan.Streets)
            {
                foreach (var end in new[] { street.Points[0], street.Points[street.Points.Count - 1] })
                {
                    if (Vec2.Distance(end, plan.Centre) > radius - 6)
                    {
                        openings.Add((end - plan.Centre).Yaw);
                    }
                }
            }

            foreach (var road in roadsIn)
            {
                openings.Add(road);
            }

            var gateDone = false;

            for (var i = 0; i < pieces; i++)
            {
                var angle = i * 360f / pieces;
                var open = false;

                foreach (var opening in openings)
                {
                    var diff = Math.Abs(Mod(angle - opening + 180, 360) - 180);
                    var half = (float)(Math.Atan2(walled ? 9 : 4, radius) * 180 / Math.PI);

                    if (diff < half + 360f / pieces * 0.5f)
                    {
                        open = true;

                        if (walled && !gateDone && Math.Abs(Mod(opening - axis + 180, 360) - 180) < 30)
                        {
                            gateDone = true;
                            var gateAt = plan.Centre + Vec2.FromYaw(opening) * (radius + 1);
                            plan.Gates.Add((gateAt, opening));
                            plan.Buildings.Add(new Placed
                            {
                                Model = "fort_battle_gate", At = gateAt, Yaw = opening, Role = "gate",
                                Footprint = new Box(gateAt, new Vec2(Catalog.Get("fort_battle_gate").Across * 0.5f, 3), opening)
                            });
                        }
                    }
                }

                // A fence has gaps of its own here and there.
                if (open || (!walled && rng.Chance(0.12f)))
                {
                    continue;
                }

                var at = plan.Centre + Vec2.FromYaw(angle) * radius;

                if (waters != null && waters.Distance(at) < 12)
                {
                    continue;
                }

                var yaw = Mod(angle + 90, 360);
                var box = new Box(at, new Vec2(piece * 0.5f, walled ? 1.5f : 0.4f), yaw);
                var clear = true;

                foreach (var other in placed)
                {
                    if (Box.Overlap(box, other))
                    {
                        clear = false;
                        break;
                    }
                }

                if (clear)
                {
                    plan.Buildings.Add(new Placed { Model = model, At = at, Yaw = yaw, Footprint = box, Role = walled ? "wall" : "fence" });
                }
            }

            if (walled && plan.Gates.Count > 0)
            {
                // Watchtowers at the gate's two sides.
                var (gate, facing) = plan.Gates[0];

                foreach (var side in new[] { -1, 1 })
                {
                    var at = gate + Vec2.FromYaw(facing + 90) * side * 12 - Vec2.FromYaw(facing) * 2;
                    plan.Buildings.Add(new Placed
                    {
                        Model = "fort_watchtower", At = at, Yaw = facing, Role = "tower", Footprint = new Box(at, new Vec2(4.6f, 4.6f), facing)
                    });
                }
            }
        }

        /// <summary>Fields outside the fence, in the sectors no road runs through: rows of strips.</summary>
        static void Fields(SettlementPlan plan, IList<float> roadsIn, float axis, ref Rng rng)
        {
            var def = plan.Def;

            for (var sector = 0; sector < 6; sector++)
            {
                var angle = axis + 30 + sector * 60;
                var blocked = false;

                foreach (var road in roadsIn)
                {
                    if (Math.Abs(Mod(angle - road + 180, 360) - 180) < 35)
                    {
                        blocked = true;
                    }
                }

                if (blocked || rng.Chance(0.3f))
                {
                    continue;
                }

                var centre = plan.Centre + Vec2.FromYaw(angle) * (def.Radius + 34);
                plan.Fields.Add(new Box(centre, new Vec2(rng.Range(18f, 28f), rng.Range(12f, 18f)), Mod(angle + rng.Range(-15f, 15f), 360)));
            }
        }
    }
}
