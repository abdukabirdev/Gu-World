using System;
using System.Collections.Generic;

namespace GuWorld.Core.Gen
{
    /// <summary>
    /// Lays out a town of thousands round a square made by hand: avenues where the roads come in,
    /// ring streets that follow the slope, lanes between them; districts by distance from the middle
    /// (the clan's quarter of halls and courtyards, the town of shops and two-storey houses, the
    /// outskirts of huts and stilt houses and terraces on the slopes, fields beyond); the great
    /// buildings of the clan placed first; every house on a pad levelled to its own ground; wells,
    /// lamps and stalls where streets meet. It keeps clear of the valley's places, the rivers and
    /// cliffs. Deterministic: the same town comes out every time.
    /// </summary>
    public static class CityPlanner
    {
        /// <summary>The districts, by distance from the middle.</summary>
        enum District { Clan, Town, Outskirts }

        static readonly string[] ClanHouses = { "kit_courtyard", "kit_courtyard_blue", "kit_two_storey_stone", "kit_shop_house_plaster", "kit_courtyard", "guyue_bamboo_house" };
        static readonly string[] TownHouses = { "kit_two_storey", "kit_shop_house", "kit_artisan", "kit_artisan_ochre", "kit_l_kitchen", "kit_l_kitchen_grey", "guyue_bamboo_house", "kit_two_storey_stone", "kit_shop_house_plaster", "kit_courtyard" };
        static readonly string[] OutskirtHouses = { "kit_hut_mud", "kit_hut_mud_ochre", "prop_poor_house", "kit_stilt_bamboo", "kit_stilt_plank", "kit_l_kitchen_grey", "kit_artisan", "kit_hut_mud", "prop_poor_house" };
        static readonly string[] Shops = { "prop_shop_small_a", "prop_shop_small_b", "prop_shop_small_c", "prop_shop", "kit_shop_house" };

        /// <summary>The clan's great buildings, placed before the houses (each once), nearest the middle first.</summary>
        static readonly (string model, string role, float from, float to)[] Landmarks =
        {
            ("prop_clan_main_hall", "hall", 110, 190),
            ("prop_academy_great", "academy", 120, 230),
            ("prop_dorm_great", "house", 140, 240),
            ("prop_refectory", "inn", 120, 230),
            ("guyue_pavilion_LOD0", "prop", 100, 200),
            ("prop_teahouse_octagon", "inn", 110, 220),
            ("prop_inn_courtyard", "inn", 150, 280),
            ("prop_tavern_great", "inn", 170, 300),
            ("prop_market_row", "shop", 110, 260),
            ("prop_market_row", "shop", 150, 300),
            ("prop_market_row", "shop", 180, 320),
            ("prop_gu_nursery", "nursery", 110, 220),
            ("prop_herbalist", "herbalist", 120, 260),
            ("prop_smithy", "smithy", 200, 330),
            ("prop_smithy", "smithy", 250, 380),
            ("prop_inn", "inn", 220, 360),
            ("prop_tavern", "inn", 180, 340),
            ("prop_eatery", "inn", 130, 300),
            ("prop_teahouse", "inn", 140, 300),
            ("prop_granary", "granary", 300, 420),
            ("prop_granary", "granary", 300, 420),
            ("prop_granary", "granary", 320, 420),
            ("prop_council_hall", "hall", 160, 300),
            ("prop_ancestral_hall", "hall", 200, 360),
        };

        /// <summary>How many people live in a building of a model.</summary>
        public static int Occupants(string model)
        {
            switch (model)
            {
                case "kit_courtyard":
                case "kit_courtyard_blue": return 9;
                case "guyue_bamboo_house": return 8;
                case "kit_two_storey":
                case "kit_two_storey_stone":
                case "kit_l_kitchen":
                case "kit_l_kitchen_grey": return 6;
                case "kit_shop_house":
                case "kit_shop_house_plaster":
                case "kit_artisan":
                case "kit_artisan_ochre":
                case "kit_slope_terrace": return 5;
                case "kit_stilt_bamboo":
                case "kit_stilt_plank":
                case "prop_poor_house": return 4;
                case "kit_hut_mud":
                case "kit_hut_mud_ochre": return 3;
                case "prop_dorm_great": return 120;
                case "prop_academy_dorm": return 30;
                default: return 0;
            }
        }

        /// <summary>The ground for the planner: the height of the land (before roads), whether a point may be built on.</summary>
        public interface IGround
        {
            float Height(float x, float z);
            /// <summary>Why a point may not be built on (a place of the valley, water, a cliff), or null.</summary>
            bool Blocked(Vec2 p, float margin);
        }

        public static SettlementPlan Plan(SettlementDef def, IGround ground, IList<List<Vec2>> roads, Waters waters)
        {
            var plan = new SettlementPlan { Def = def, Level = ground.Height(def.At.X, def.At.Z), Centre = def.At, PlazaRadius = def.Core };
            var rng = new Rng(def.Seed * 6007 + 101);
            var placed = new List<Box>();
            var index = new ShapeIndex<Box>(Regions.Half, 32);

            // ---------------------------------------------------------------- Streets

            // The roads that cross the town are its avenues.
            foreach (var road in roads)
            {
                var inside = new List<Vec2>();

                foreach (var p in road)
                {
                    var d = Vec2.Distance(p, plan.Centre);

                    if (d < def.Radius + 10 && d > def.Core - 6)
                    {
                        inside.Add(p);
                    }
                    else if (inside.Count > 1)
                    {
                        plan.Streets.Add(new Street { Points = Lines.Resample(inside, 4), HalfWidth = 3.4f, Main = true });
                        inside = new List<Vec2>();
                    }
                    else
                    {
                        inside.Clear();
                    }
                }

                if (inside.Count > 1)
                {
                    plan.Streets.Add(new Street { Points = Lines.Resample(inside, 4), HalfWidth = 3.4f, Main = true });
                }
            }

            // ---------------------------------------------------------------- The clan's great buildings

            var avenues = new SegmentIndex(Regions.Half, 32, 12);

            for (var i = 0; i < plan.Streets.Count; i++)
            {
                avenues.Add(plan.Streets[i].Points, i);
            }

            foreach (var (model, role, from, to) in Landmarks)
            {
                if (!Catalog.TryGet(model, out var footprint))
                {
                    continue;
                }

                for (var attempt = 0; attempt < 90; attempt++)
                {
                    var angle = rng.Range(0f, 360f);
                    var distance = rng.Range(from, to);
                    var centre = plan.Centre + Vec2.FromYaw(angle) * distance;

                    // It faces the nearest street, or the middle.
                    var yaw = avenues.Nearest(centre, out var toStreet, out var segment, out _) ? (segment.A + (segment.B - segment.A) * 0.5f - centre).Yaw : (plan.Centre - centre).Yaw;
                    yaw = (float)Math.Round(yaw / 15) * 15;

                    if (Place(plan, placed, index, avenues, ground, waters, model, centre, yaw, role, 2.5f))
                    {
                        break;
                    }
                }
            }

            // The streets go round the great buildings.
            var great = new List<Box>();

            foreach (var building in plan.Buildings)
            {
                great.Add(building.Footprint.Grown(4));
            }

            bool Taken(Vec2 p, float margin)
            {
                if (ground.Blocked(p, margin))
                {
                    return true;
                }

                foreach (var box in great)
                {
                    if (box.Distance(p) < margin)
                    {
                        return true;
                    }
                }

                return false;
            }

            // Ring streets, bent by the land: each is cut where it would pass through a place of the valley, water or a cliff.
            var rings = new List<float>();

            for (var r = def.Core + 26; r < def.Radius - 10; r += r < 200 ? 48 : 58)
            {
                rings.Add(r);
            }

            var wobble = new Noise(def.Seed * 13 + 5);

            foreach (var radius in rings)
            {
                var ring = new List<Vec2>();
                var steps = (int)(2 * Math.PI * radius / 6);

                for (var i = 0; i <= steps; i++)
                {
                    var angle = i * 360f / steps;
                    var bend = wobble.Fbm(angle * 0.02f, radius * 0.01f, 2) * 14;
                    var p = plan.Centre + Vec2.FromYaw(angle) * (radius + bend);

                    if (Taken(p, 6))
                    {
                        AddPiece(plan, ring, 2.6f);
                        ring = new List<Vec2>();
                    }
                    else
                    {
                        ring.Add(p);
                    }
                }

                AddPiece(plan, ring, 2.6f);
            }

            // Lanes between the rings, staggered from ring to ring so the blocks are not one long grid.
            var lanes = 0;

            for (var k = 0; k < rings.Count; k++)
            {
                var inner = k == 0 ? def.Core + 4 : rings[k - 1];
                var outer = k + 1 < rings.Count ? rings[k + 1] : def.Radius - 6;
                var count = (int)(2 * Math.PI * rings[k] / (k < 2 ? 70 : 85));
                var offset = rng.Range(0f, 360f / count);

                for (var i = 0; i < count; i++)
                {
                    var angle = offset + i * 360f / count + rng.Range(-4f, 4f);
                    var lane = new List<Vec2>();

                    for (var r = inner; r <= outer; r += 5)
                    {
                        var p = plan.Centre + Vec2.FromYaw(angle) * r;

                        if (Taken(p, 5))
                        {
                            AddPiece(plan, lane, 1.8f);
                            lane = new List<Vec2>();
                        }
                        else
                        {
                            lane.Add(p);
                        }
                    }

                    if (AddPiece(plan, lane, 1.8f))
                    {
                        lanes++;
                    }
                }
            }

            var streets = new SegmentIndex(Regions.Half, 32, 12);

            for (var i = 0; i < plan.Streets.Count; i++)
            {
                streets.Add(plan.Streets[i].Points, i);
            }

            // ---------------------------------------------------------------- Houses along every street

            var lots = new List<(Street street, int side, float along)>();

            foreach (var street in plan.Streets)
            {
                var length = Lines.Length(street.Points);

                foreach (var side in new[] { -1, 1 })
                {
                    for (var along = rng.Range(2f, 6f); along < length - 2; along += rng.Range(9f, 13f))
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

            var houses = 0;

            foreach (var (street, side, along) in lots)
            {
                if (houses >= def.Houses)
                {
                    break;
                }

                var point = Lines.At(street.Points, along, out var direction);
                var fromMiddle = Vec2.Distance(point, plan.Centre);
                var district = fromMiddle < 175 ? District.Clan : fromMiddle < 300 ? District.Town : District.Outskirts;
                var normal = side < 0 ? direction.Left : direction.Right;

                for (var attempt = 0; attempt < 3; attempt++)
                {
                    var model = Pick(district, street.Main, ground, point, ref rng);
                    var footprint = Catalog.Get(model);
                    var setback = street.HalfWidth + 1.4f + rng.Range(0f, district == District.Outskirts ? 3f : 1.2f);
                    var yaw = (-normal).Yaw;
                    var centre = point + normal * (setback + footprint.Deep * 0.5f);

                    if (!Place(plan, placed, index, streets, ground, waters, model, centre, yaw, Occupants(model) > 0 ? "house" : "shop", 1.4f))
                    {
                        continue;
                    }

                    houses++;

                    // A yard's clutter beside some of them.
                    if (rng.Chance(district == District.Outskirts ? 0.45f : 0.25f))
                    {
                        var prop = rng.Pick(district == District.Outskirts ? new[] { "prop_firewood_stack", "prop_drying_rack", "prop_barrels" } : new[] { "prop_barrels", "prop_bench_wood", "prop_lotus_vat", "prop_bonsai_pine" });
                        var beside = centre + Vec2.FromYaw(yaw + 90) * (footprint.Across * 0.5f + 1.8f) * (rng.Chance(0.5f) ? 1 : -1);
                        Place(plan, placed, index, streets, ground, waters, prop, beside, yaw, "prop", 0.3f, false);
                    }

                    break;
                }
            }

            // ---------------------------------------------------------------- Where streets meet: wells, lamps, stalls

            var squares = 0;

            for (var i = 0; i < plan.Streets.Count && squares < 60; i++)
            {
                var a = plan.Streets[i];

                foreach (var end in new[] { a.Points[0], a.Points[a.Points.Count - 1] })
                {
                    var fromMiddle = Vec2.Distance(end, plan.Centre);

                    if (fromMiddle < def.Core + 20 || fromMiddle > def.Radius - 20 || !rng.Chance(0.35f))
                    {
                        continue;
                    }

                    var around = end + Vec2.FromYaw(rng.Range(0f, 360f)) * 6;

                    if (Place(plan, placed, index, streets, ground, waters, rng.Chance(0.5f) ? "prop_village_well" : "prop_moon_lantern_post", around, rng.Range(0f, 360f), "prop", 0.6f, false))
                    {
                        squares++;
                        plan.Spots.Add(new Spot { At = around + Vec2.FromYaw(rng.Range(0f, 360f)) * 3, Facing = rng.Range(0f, 360f), Role = "plaza" });

                        if (fromMiddle < 300 && rng.Chance(0.6f))
                        {
                            Place(plan, placed, index, streets, ground, waters, "prop_market_goods", around + Vec2.FromYaw(rng.Range(0f, 360f)) * 5, rng.Range(0f, 360f), "prop", 0.3f, false);
                        }
                    }
                }
            }

            // Lamps along the avenues.
            foreach (var street in plan.Streets)
            {
                if (!street.Main)
                {
                    continue;
                }

                var length = Lines.Length(street.Points);

                for (var along = 12f; along < length - 6; along += 34)
                {
                    var p = Lines.At(street.Points, along, out var direction);
                    var side = ((int)(along / 34) % 2 == 0 ? direction.Left : direction.Right) * (street.HalfWidth + 0.9f);
                    Place(plan, placed, index, streets, ground, waters, "prop_moon_lantern_post", p + side, direction.Yaw, "lamp", 0.1f, false, true);
                }
            }

            // ---------------------------------------------------------------- Things made in code

            CodeThings(plan, placed, index, streets, ground, waters, ref rng);

            // ---------------------------------------------------------------- Fields beyond the outskirts

            for (var i = 0; i < 90; i++)
            {
                var angle = rng.Range(0f, 360f);
                var at = plan.Centre + Vec2.FromYaw(angle) * rng.Range(def.Radius - 60, def.Radius + 40);
                var box = new Box(at, new Vec2(rng.Range(12f, 22f), rng.Range(8f, 14f)), angle + rng.Range(-10f, 10f));

                if (ground.Blocked(at, 20) || Crowded(index, box.Grown(3)) || streets.Distance(at) < box.Radius + 3)
                {
                    continue;
                }

                plan.Fields.Add(box);
                index.Add(box.Centre, box.Radius, box);
            }

            // ---------------------------------------------------------------- The wall round it all

            Wall(plan, index, roads, ground, waters);

            // ---------------------------------------------------------------- People and their spots

            var doors = 0;

            foreach (var building in plan.Buildings)
            {
                plan.Population += Occupants(building.Model);

                if (building.Role != "house" && building.Role != "prop" && building.Role != "lamp")
                {
                    plan.Spots.Add(new Spot { At = building.Door, Facing = building.Yaw, Role = building.Role });
                }
                else if (building.Role == "house" && doors++ % 5 == 0)
                {
                    plan.Spots.Add(new Spot { At = building.Door, Facing = building.Yaw, Role = "house" });
                }
            }

            plan.Lanes = lanes;

            return plan;
        }

        /// <summary>
        /// The town's furniture made in code: memorial archways where the avenues enter the clan's middle and
        /// leave the town, a ring of market stalls round the square and more at the crossings (each a spot
        /// for a trader), strings of lanterns across the lanes of the inner districts, hand carts by the
        /// granaries and the shops, a pagoda and a drum tower among the halls.
        /// </summary>
        static void CodeThings(SettlementPlan plan, List<Box> placed, ShapeIndex<Box> index, SegmentIndex streets, IGround ground, Waters waters, ref Rng rng)
        {
            var def = plan.Def;

            // Archways over the avenues: one where each enters the middle, one where it leaves the town.
            foreach (var street in plan.Streets)
            {
                if (!street.Main)
                {
                    continue;
                }

                var length = Lines.Length(street.Points);

                foreach (var wanted in new[] { def.Core + 16, def.Radius - 40 })
                {
                    // Walk the avenue for the point nearest the wanted distance from the middle.
                    var best = -1f;
                    var bestError = float.MaxValue;

                    for (var along = 4f; along < length - 4; along += 4)
                    {
                        var error = Math.Abs(Vec2.Distance(Lines.At(street.Points, along, out _), plan.Centre) - wanted);

                        if (error < bestError)
                        {
                            bestError = error;
                            best = along;
                        }
                    }

                    if (best < 0 || bestError > 12)
                    {
                        continue;
                    }

                    var p = Lines.At(street.Points, best, out var direction);
                    var big = wanted < def.Radius * 0.5f;
                    Place(plan, placed, index, streets, ground, waters, Code(big ? "paifang:0" : "paifang:1"), p, direction.Yaw, "prop", 0.2f, false, true);
                }
            }

            // Stalls in a ring round the square, facing in.
            var ringRadius = def.Core + 11;
            var stalls = 0;

            for (var i = 0; i < 48; i++)
            {
                var angle = i * 7.5f + rng.Range(-1.5f, 1.5f);
                var at = plan.Centre + Vec2.FromYaw(angle) * ringRadius;
                var yaw = (plan.Centre - at).Yaw;

                if (Place(plan, placed, index, streets, ground, waters, Code("stall:" + rng.Range(0, 6)), at, yaw, "stall", 0.4f, false))
                {
                    stalls++;
                }
            }

            // Stalls at the crossings of the town (two or three round each, facing the crossing).
            foreach (var street in plan.Streets)
            {
                foreach (var end in new[] { street.Points[0], street.Points[street.Points.Count - 1] })
                {
                    var fromMiddle = Vec2.Distance(end, plan.Centre);

                    if (fromMiddle < def.Core + 30 || fromMiddle > 300 || !rng.Chance(0.3f))
                    {
                        continue;
                    }

                    for (var k = 0; k < 3; k++)
                    {
                        var at = end + Vec2.FromYaw(rng.Range(0f, 360f)) * rng.Range(7f, 10f);

                        if (Place(plan, placed, index, streets, ground, waters, Code("stall:" + rng.Range(0, 6)), at, (end - at).Yaw, "stall", 0.4f, false))
                        {
                            stalls++;
                        }
                    }
                }
            }

            // Lanterns across the lanes of the clan's and the town's districts.
            foreach (var street in plan.Streets)
            {
                if (street.Main)
                {
                    continue;
                }

                var length = Lines.Length(street.Points);

                for (var along = 10f; along < length - 10; along += rng.Range(26f, 44f))
                {
                    var p = Lines.At(street.Points, along, out var direction);

                    if (Vec2.Distance(p, plan.Centre) > 300)
                    {
                        continue;
                    }

                    var variant = street.HalfWidth > 3 ? 1 : 0;
                    Place(plan, placed, index, streets, ground, waters, Code("lanterns:" + variant), p, direction.Yaw + 90, "prop", 0.05f, false, true);
                }
            }

            // Carts by the granaries and some shops.
            foreach (var building in plan.Buildings.ToArray())
            {
                var granary = building.Model == "prop_granary";
                var shop = building.Role == "shop";

                if (!(granary || shop && rng.Chance(0.25f)))
                {
                    continue;
                }

                var side = Vec2.FromYaw(building.Yaw + 90) * (building.Footprint.Half.X + 2.2f) * (rng.Chance(0.5f) ? 1 : -1);
                Place(plan, placed, index, streets, ground, waters, Code("cart:" + rng.Range(0, 3)), building.Footprint.Centre + side, building.Yaw + rng.Range(-20f, 20f), "prop", 0.3f, false);
            }

            // A pagoda and a drum tower among the halls.
            foreach (var (model, from, to) in new[] { ("pagoda:1", def.Core + 60, def.Core + 160f), ("drum_tower:0", def.Core + 25, def.Core + 70f) })
            {
                for (var attempt = 0; attempt < 120; attempt++)
                {
                    var at = plan.Centre + Vec2.FromYaw(rng.Range(0f, 360f)) * rng.Range(from, to);

                    if (Place(plan, placed, index, streets, ground, waters, Code(model), at, (plan.Centre - at).Yaw, "landmark", 1.5f))
                    {
                        break;
                    }
                }
            }

            // Banners of the clan along the avenues near the middle, both sides, blue and red by turns.
            foreach (var street in plan.Streets)
            {
                if (!street.Main)
                {
                    continue;
                }

                var length = Lines.Length(street.Points);

                for (var along = 6f; along < length - 6; along += 22)
                {
                    var p = Lines.At(street.Points, along, out var direction);
                    var fromMiddle = Vec2.Distance(p, plan.Centre);

                    if (fromMiddle < def.Core + 8 || fromMiddle > def.Core + 110)
                    {
                        continue;
                    }

                    foreach (var side in new[] { -1, 1 })
                    {
                        var at = p + direction.Right * (street.HalfWidth + 1.2f) * side;
                        Place(plan, placed, index, streets, ground, waters, Code("banner:" + ((int)(along / 22) % 2)), at, direction.Yaw + (side > 0 ? 90 : -90), "prop", 0.1f, false, true);
                    }
                }
            }

            // Stone lions before the great halls' doors: the male on the left, the female on the right.
            foreach (var building in plan.Buildings.ToArray())
            {
                if (building.Role != "hall" && building.Role != "academy")
                {
                    continue;
                }

                var forward = Vec2.FromYaw(building.Yaw);
                var right = forward.Right;
                var door = building.Footprint.Centre + forward * (building.Footprint.Half.Z + 3.5f);
                Place(plan, placed, index, streets, ground, waters, Code("stone_lion:0"), door - right * 4.5f, building.Yaw, "prop", 0.1f, false, true);
                Place(plan, placed, index, streets, ground, waters, Code("stone_lion:1"), door + right * 4.5f, building.Yaw, "prop", 0.1f, false, true);

                // The academy's training yard beside it.
                if (building.Role == "academy")
                {
                    for (var attempt = 0; attempt < 8; attempt++)
                    {
                        var side = attempt % 2 == 0 ? 1 : -1;
                        var at = building.Footprint.Centre + right * side * (building.Footprint.Half.X + 11 + attempt * 2);

                        if (Place(plan, placed, index, streets, ground, waters, Code("training_yard:0"), at, building.Yaw, "yard", 1f))
                        {
                            break;
                        }
                    }
                }
            }

            plan.Stalls = stalls;
        }

        /// <summary>
        /// The town's wall: pieces of rammed earth on stone set round the fields, a gate tower where each road
        /// crosses the ring, a corner tower every so often, and gaps where the ground will not take a wall
        /// (water, a cliff, a place of the valley).
        /// </summary>
        static void Wall(SettlementPlan plan, ShapeIndex<Box> index, IList<List<Vec2>> roads, IGround ground, Waters waters)
        {
            var radius = plan.Def.Radius + 66;
            var piece = Props.Footprint("town_wall").width;
            var pieces = (int)(2 * Math.PI * radius / piece);
            var gates = new List<float>();

            // Where the roads cross the ring.
            foreach (var road in roads)
            {
                for (var i = 1; i < road.Count; i++)
                {
                    var a = Vec2.Distance(road[i - 1], plan.Centre) - radius;
                    var b = Vec2.Distance(road[i], plan.Centre) - radius;

                    if (a * b <= 0 && Math.Abs(a - b) > 1e-3f)
                    {
                        var t = a / (a - b);
                        var at = road[i - 1] + (road[i] - road[i - 1]) * t;
                        var angle = (at - plan.Centre).Yaw;

                        if (!gates.Exists(g => Math.Abs(Mod(g - angle + 180, 360) - 180) < 6))
                        {
                            gates.Add(angle);
                        }
                    }
                }
            }

            foreach (var angle in gates)
            {
                var at = plan.Centre + Vec2.FromYaw(angle) * radius;
                var (width, depth) = Props.Footprint("town_gate");
                // The gate faces out along the road (its +z outward).
                plan.Buildings.Add(new Placed { Model = "code:town_gate:0", At = at, Yaw = angle, Footprint = new Box(at, new Vec2(width * 0.5f, depth * 0.5f), angle), Role = "gate" });
                plan.Gates.Add((at, angle));
            }

            var built = new List<Box>();
            var gateHalf = (float)(Math.Atan2(Props.Footprint("town_gate").width * 0.5f + 1, radius) * 180 / Math.PI);

            for (var i = 0; i < pieces; i++)
            {
                var angle = (i + 0.5f) * 360f / pieces;

                if (gates.Exists(g => Math.Abs(Mod(g - angle + 180, 360) - 180) < gateHalf))
                {
                    continue;
                }

                var at = plan.Centre + Vec2.FromYaw(angle) * radius;
                // Its length along the ring (its own x), its outer face (−z) outward: it faces the middle.
                var yaw = angle + 180;
                var corner = i % 24 == 0;
                var model = corner ? "code:corner_tower:0" : "code:town_wall:0";
                var (w, d) = Props.Footprint(corner ? "corner_tower" : "town_wall");
                var box = new Box(at, new Vec2(w * 0.5f, d * 0.5f), yaw);
                var blocked = false;

                foreach (var p in box.Corners())
                {
                    if (ground.Blocked(p, 1) || waters != null && waters.Nearest(p, out var toWater, out _, out var width, out _) && toWater < width * 0.5f + 2)
                    {
                        blocked = true;
                        break;
                    }
                }

                // A wall does not climb a cliff: more than a few metres of fall along it and it stops.
                var fall = Math.Abs(ground.Height(box.Corners()[0].X, box.Corners()[0].Z) - ground.Height(box.Corners()[2].X, box.Corners()[2].Z));

                if (blocked || fall > 6f || Crowded(index, box))
                {
                    continue;
                }

                // (The pieces touch at their ends round the curve; they are kept from the rest, not from each other.)
                plan.Buildings.Add(new Placed { Model = model, At = at, Yaw = yaw, Footprint = box, Role = "wall" });
                built.Add(box);
                plan.WallPieces++;
            }

            foreach (var box in built)
            {
                index.Add(box.Centre, box.Radius, box);
            }
        }

        static float Mod(float a, float m) => ((a % m) + m) % m;

        /// <summary>The name of a model made in code ("code:kind:variant").</summary>
        static string Code(string kindAndVariant) => "code:" + kindAndVariant;

        /// <summary>True if a street passes under a box: its corners, the middles of its sides, its middle and (for a big one) a grid inside.</summary>
        static bool OnStreet(SettlementPlan plan, SegmentIndex streets, Box box)
        {
            var points = new List<Vec2>(box.Corners()) { box.Centre };
            var corners = box.Corners();

            for (var i = 0; i < corners.Length; i++)
            {
                points.Add((corners[i] + corners[(i + 1) % corners.Length]) * 0.5f);
            }

            if (box.Radius > 14)
            {
                var axisX = Vec2.FromYaw(box.Yaw + 90);
                var axisZ = Vec2.FromYaw(box.Yaw);

                for (var x = -box.Half.X; x <= box.Half.X; x += 7)
                {
                    for (var z = -box.Half.Z; z <= box.Half.Z; z += 7)
                    {
                        points.Add(box.Centre + axisX * x + axisZ * z);
                    }
                }
            }

            foreach (var p in points)
            {
                if (streets.Nearest(p, out var distance, out var segment, out _) && distance < plan.Streets[segment.Line].HalfWidth + 0.4f)
                {
                    return true;
                }
            }

            return false;
        }

        static bool AddPiece(SettlementPlan plan, List<Vec2> points, float half)
        {
            if (points.Count < 5)
            {
                return false;
            }

            plan.Streets.Add(new Street { Points = Lines.Resample(points, 4), HalfWidth = half });

            return true;
        }

        /// <summary>A house for a lot: by district, a shop on the avenues, a terrace house where the ground falls away.</summary>
        static string Pick(District district, bool avenue, IGround ground, Vec2 at, ref Rng rng)
        {
            if (avenue && district != District.Outskirts && rng.Chance(0.3f))
            {
                return rng.Pick(Shops);
            }

            if (district == District.Outskirts)
            {
                var fall = Math.Abs(ground.Height(at.X + 6, at.Z) - ground.Height(at.X - 6, at.Z)) + Math.Abs(ground.Height(at.X, at.Z + 6) - ground.Height(at.X, at.Z - 6));

                if (fall > 3.5f && rng.Chance(0.6f))
                {
                    return "kit_slope_terrace";
                }
            }

            return rng.Pick(district == District.Clan ? ClanHouses : district == District.Town ? TownHouses : OutskirtHouses);
        }

        static bool Crowded(ShapeIndex<Box> index, Box box)
        {
            foreach (var other in index.Near(box.Centre, box.Radius))
            {
                if (Box.Overlap(box, other))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Places a model whose footprint's middle is at `centre`: inside the town and outside its hand-made middle,
        /// clear of the others, the streets, the water and the valley's places, on ground that does not fall more
        /// than a storey under it. Adds the pad the house stands on.
        /// </summary>
        static bool Place(SettlementPlan plan, List<Box> placed, ShapeIndex<Box> index, SegmentIndex streets, IGround ground, Waters waters,
            string model, Vec2 centre, float yaw, string role, float gap, bool pad = true, bool onStreet = false)
        {
            if (!Catalog.TryGet(model, out var footprint))
            {
                return false;
            }

            var box = new Box(centre, new Vec2(footprint.Across * 0.5f, footprint.Deep * 0.5f), yaw);
            var def = plan.Def;
            var low = float.MaxValue;
            var high = float.MinValue;

            foreach (var corner in box.Corners())
            {
                var d = Vec2.Distance(corner, plan.Centre);

                if (d > def.Radius - 2 || d < def.Core + 2 || ground.Blocked(corner, 3))
                {
                    return false;
                }

                if (waters != null && waters.Nearest(corner, out var toWater, out _, out var width, out _) && toWater < width * 0.5f + 6)
                {
                    return false;
                }

                var h = ground.Height(corner.X, corner.Z);
                low = Math.Min(low, h);
                high = Math.Max(high, h);
            }

            var middle = ground.Height(centre.X, centre.Z);
            low = Math.Min(low, middle);
            high = Math.Max(high, middle);

            // A house on a slope gets a pad cut in; more than a storey of fall is too much.
            if (pad && high - low > (footprint.Across > 30 ? 6 : 4.2f))
            {
                return false;
            }

            var grown = box.Grown(gap);

            if (Crowded(index, grown))
            {
                return false;
            }

            if (!onStreet && OnStreet(plan, streets, grown))
            {
                return false;
            }

            placed.Add(box);
            index.Add(box.Centre, box.Radius, box);
            var origin = centre - (Vec2.FromYaw(yaw + 90) * footprint.MiddleX + Vec2.FromYaw(yaw) * footprint.MiddleZ);
            plan.Buildings.Add(new Placed { Model = model, At = origin, Yaw = yaw, Footprint = box, Role = role });

            if (pad)
            {
                // The pad is cut to the middle of its rise: half the fall dug in, half built up.
                plan.Pads.Add((box, (low + high) * 0.5f));
            }

            return true;
        }
    }
}
