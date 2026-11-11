using GuWorld.Core;
using UnityEngine;

namespace GuWorld.World
{
    /// <summary>
    /// The valley's ground: rolling hills that climb to the Wolf Ridge in the north and to a
    /// wall of mountains all round, levelled where the plan (Core/Valley) puts a place, cut by
    /// the river, with the two caves sunk in pits behind cliffs. Made at start as one Unity
    /// terrain with its layers painted from the same plan: flagstones in the village, beaten
    /// earth on the roads, rock on the steep.
    /// </summary>
    public static class Ground
    {
        public const float Size = Valley.Half * 2;
        public const float MaxHeight = 160;
        const int Heights = 513;
        const int Alphas = 512;

        public static Terrain Terrain { get; private set; }

        /// <summary>The generated 8 × 8 km world, when the game runs on it (null on the small valley).</summary>
        public static Core.Gen.WorldPlan Plan { get; set; }
        public static bool Big => Plan != null;

        static float Smooth(float from, float to, float x) => Mathf.SmoothStep(0, 1, Mathf.InverseLerp(from, to, x));

        static float Noise(float x, float z, float scale, float seed) => Mathf.PerlinNoise(x * scale + seed, z * scale + seed * 1.7f) * 2 - 1;

        public static float DistanceTo(MapPoint[] line, float x, float z, out int segment)
        {
            var best = float.MaxValue;
            segment = 0;

            for (var i = 0; i < line.Length - 1; i++)
            {
                var ax = line[i].X;
                var az = line[i].Z;
                var bx = line[i + 1].X - ax;
                var bz = line[i + 1].Z - az;
                var t = Mathf.Clamp01(((x - ax) * bx + (z - az) * bz) / Mathf.Max(0.0001f, bx * bx + bz * bz));
                var dx = x - (ax + bx * t);
                var dz = z - (az + bz * t);
                var d = dx * dx + dz * dz;

                if (d < best)
                {
                    best = d;
                    segment = i;
                }
            }

            return Mathf.Sqrt(best);
        }

        public static float RoadDistance(float x, float z)
        {
            if (Plan != null)
            {
                return Plan.Roads.Distance(new Core.Gen.Vec2(x, z));
            }

            var best = float.MaxValue;

            foreach (var road in Valley.Roads)
            {
                best = Mathf.Min(best, DistanceTo(road, x, z, out _));
            }

            return best;
        }

        /// <summary>The ground's height at a point of the map, metres.</summary>
        public static float HeightAt(float x, float z)
        {
            if (Plan != null)
            {
                return Plan.Land.Height(x, z);
            }

            var h = 15 + 9 * Noise(x, z, 0.0055f, 11) + 3.5f * Noise(x, z, 0.021f, 37) + 0.6f * Noise(x, z, 0.09f, 71);
            // The land climbs to the ridge in the north and dips a little to the south.
            h += Smooth(180, 430, z) * 44 - Smooth(-150, -400, z) * 4;
            // The mountains that shut the valley in.
            var edge = Mathf.Max(Mathf.Abs(x), Mathf.Abs(z));
            h += Smooth(Valley.Half - 70, Valley.Half - 4, edge) * (70 + 14 * Noise(x, z, 0.03f, 3));

            // The places: levelled, easing back into the hills round them.
            foreach (var place in Valley.Places)
            {
                if (float.IsNaN(place.Level))
                {
                    continue;
                }

                var d = Mathf.Sqrt((x - place.At.X) * (x - place.At.X) + (z - place.At.Z) * (z - place.At.Z));

                if (place.Cave)
                {
                    // A pit behind cliffs: flat inside, a ring of rock round it.
                    var inside = 1 - Smooth(place.Radius - 2, place.Radius + 8, d);
                    h = Mathf.Lerp(h, place.Level, inside);
                    var wall = Smooth(place.Radius - 1, place.Radius + 12, d) * (1 - Smooth(place.Radius + 26, place.Radius + 60, d));
                    h += wall * (46 + 6 * Noise(x, z, 0.05f, 5));
                }
                else if (d < place.Radius + place.Blend)
                {
                    h = Mathf.Lerp(h, place.Level, 1 - Smooth(place.Radius * 0.9f, place.Radius + place.Blend, d));
                }
            }

            // The north-east plateau: a cliff along its southern edge, where the river falls.
            h += Smooth(Valley.PlateauZ - 4, Valley.PlateauZ + 4, z) * Smooth(Valley.PlateauX - 16, Valley.PlateauX + 16, x) * Valley.PlateauRise;

            // The river: shallow enough to wade, its banks lifted where the land lies lower than its water.
            var river = DistanceTo(Valley.River, x, z, out var segment);

            if (river < 70)
            {
                var level = WaterLevelAt(z);
                var bed = level - 0.55f;
                h += Mathf.Max(0, level + 0.9f - h) * (1 - Smooth(24, 70, river));
                h = Mathf.Lerp(bed, h, Smooth(5, 24, river));
            }

            return Mathf.Clamp(h, 0.5f, MaxHeight - 1);
        }

        /// <summary>The river's water level at a latitude: high on the plateau, low below the fall.</summary>
        public static float WaterLevelAt(float z) => z > Valley.PlateauZ ? Valley.WaterfallTop : Valley.WaterfallFoot;

        public static float SampleHeight(Vector3 at)
        {
            if (Plan != null)
            {
                return Streamer.Instance != null ? Streamer.Instance.Height(at) : Plan.Land.Height(at.x, at.z);
            }

            return Terrain != null ? Terrain.SampleHeight(at) + Terrain.transform.position.y : HeightAt(at.x, at.z);
        }

        /// <summary>The distance to the nearest river's middle (the big world's rivers, or the valley's one).</summary>
        public static float RiverDistance(float x, float z) => Plan != null ? Plan.Waters.Distance(new Core.Gen.Vec2(x, z)) : DistanceTo(Valley.River, x, z, out _);

        static readonly float[] stepWeights = new float[Core.Gen.Surface.Layers];

        /// <summary>The sound of a step at a point: water, a bridge's planks, paving and paths, gravel and rock, mud, or grass.</summary>
        public static string StepSound(Vector3 at)
        {
            if (Plan == null)
            {
                return RoadDistance(at.x, at.z) < 2.2f ? "step_stone" : "step_grass";
            }

            var p = new Core.Gen.Vec2(at.x, at.z);

            if (Plan.Waters.Wet(p, out var level) && level > at.y - 0.2f)
            {
                return "step_water";
            }

            // On a bridge: higher than the ground under it, near the road over water.
            if (at.y > HeightAt(at.x, at.z) + 0.6f)
            {
                return "step_wood";
            }

            var height = HeightAt(at.x, at.z);
            System.Array.Clear(stepWeights, 0, stepWeights.Length);
            Plan.Surface.Paint(p, height, Plan.Land.Slope(at.x, at.z), stepWeights);
            var best = 0;

            for (var i = 1; i < stepWeights.Length; i++)
            {
                if (stepWeights[i] > stepWeights[best])
                {
                    best = i;
                }
            }

            switch (Core.Gen.Surface.LayerNames[best])
            {
                case "Flagstone":
                case "Path": return "step_stone";
                case "Rock":
                case "Gravel": return "step_gravel";
                case "Mud": return "step_mud";
                case "Dirt":
                case "Earth": return "step_gravel";
                default: return "step_grass";
            }
        }

        /// <summary>How far a point is from the nearest waterfall's foot (the valley's own fall on the small world).</summary>
        public static float FallDistance(float x, float z)
        {
            var best = float.MaxValue;

            if (Plan != null)
            {
                foreach (var fall in Plan.Waters.Falls)
                {
                    best = Mathf.Min(best, Vector2.Distance(new Vector2(x, z), new Vector2(fall.Foot.X, fall.Foot.Z)));
                }
            }
            else
            {
                var place = Valley.Place("waterfall");

                if (place != null)
                {
                    best = Vector2.Distance(new Vector2(x, z), new Vector2(place.At.X, place.At.Z));
                }
            }

            return best;
        }

        /// <summary>True if the solid ground under a point is there (on the big world, its tile is loaded).</summary>
        public static bool Ready(Vector3 at) => Plan == null || (Streamer.Instance != null && Streamer.Instance.Ready(at));

        /// <summary>The map's half size: the valley's or the whole mountain's.</summary>
        public static float Half => Plan != null ? Core.Regions.Half : Valley.Half;

        public static Terrain Build(Transform parent)
        {
            var heights = new float[Heights, Heights];

            for (var j = 0; j < Heights; j++)
            {
                var z = -Valley.Half + Size * j / (Heights - 1);

                for (var i = 0; i < Heights; i++)
                {
                    var x = -Valley.Half + Size * i / (Heights - 1);
                    heights[j, i] = HeightAt(x, z) / MaxHeight;
                }
            }

            var data = new TerrainData { name = "Valley" };
            data.heightmapResolution = Heights;
            data.size = new Vector3(Size, MaxHeight, Size);
            data.alphamapResolution = Alphas;
            data.baseMapResolution = 512;
            data.SetHeights(0, 0, heights);
            Paint(data, heights);

            var made = Terrain.CreateTerrainGameObject(data);
            made.name = "Valley";
            made.transform.SetParent(parent, false);
            made.transform.position = new Vector3(-Valley.Half, 0, -Valley.Half);
            Terrain = made.GetComponent<Terrain>();
            var material = Resources.Load<Material>("Terrain/Terrain");

            if (material != null)
            {
                Terrain.materialTemplate = material;
            }

            Terrain.heightmapPixelError = Quality.Level >= 2 ? 5 : 9;
            Terrain.basemapDistance = Quality.Level >= 2 ? 180 : 90;
            Terrain.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            // Instanced drawing leaves the terrain invisible in the player (Unity 6 / URP 17.6 on D3D11), so it is off.
            Terrain.drawInstanced = false;

            return Terrain;
        }

        static readonly string[] LayerNames = { "Grass", "Forest", "Flagstone", "Path", "Rock", "Gravel", "Moss", "Earth", "Mud", "Dirt" };

        static void Paint(TerrainData data, float[,] heights)
        {
            var layers = new TerrainLayer[LayerNames.Length];

            for (var i = 0; i < layers.Length; i++)
            {
                layers[i] = Resources.Load<TerrainLayer>("Terrain/" + LayerNames[i]);

                if (layers[i] == null)
                {
                    Debug.LogWarning($"GU World: no terrain layer at Resources/Terrain/{LayerNames[i]}");
                    layers[i] = new TerrainLayer { diffuseTexture = Texture2D.grayTexture, tileSize = new Vector2(6, 6) };
                }
            }

            data.terrainLayers = layers;
            var map = new float[Alphas, Alphas, layers.Length];
            var weights = new float[layers.Length];
            var village = Valley.Place("village");
            var grove = Valley.Place("grove");
            var forest = Valley.Place("forest");
            var ridge = Valley.Place("ridge");
            var camp = Valley.Place("camp");
            var stone = Valley.Place("border_stone");
            var gate = Valley.Place("north_gate");
            const float step = Size / (Heights - 1);

            for (var j = 0; j < Alphas; j++)
            {
                var z = -Valley.Half + Size * (j + 0.5f) / Alphas;
                var hj = Mathf.Clamp(Mathf.RoundToInt((j + 0.5f) / Alphas * (Heights - 1)), 1, Heights - 2);

                for (var i = 0; i < Alphas; i++)
                {
                    var x = -Valley.Half + Size * (i + 0.5f) / Alphas;
                    var hi = Mathf.Clamp(Mathf.RoundToInt((i + 0.5f) / Alphas * (Heights - 1)), 1, Heights - 2);
                    var slopeX = (heights[hj, hi + 1] - heights[hj, hi - 1]) * MaxHeight / (2 * step);
                    var slopeZ = (heights[hj + 1, hi] - heights[hj - 1, hi]) * MaxHeight / (2 * step);
                    var slope = Mathf.Sqrt(slopeX * slopeX + slopeZ * slopeZ);
                    var patch = Noise(x, z, 0.035f, 19) * 0.5f + 0.5f;
                    System.Array.Clear(weights, 0, weights.Length);
                    var point = new MapPoint(x, z);
                    weights[0] = 1;

                    float In(PlaceDef place, float soft = 14) => 1 - Smooth(place.Radius - soft, place.Radius + soft, place.At.DistanceTo(point));

                    Mix(weights, 1, Mathf.Max(In(forest, 30), In(ridge, 30)) * (0.55f + 0.45f * patch));
                    Mix(weights, 6, In(grove, 24) * (0.4f + 0.5f * patch));
                    Mix(weights, 9, Smooth(0.45f, 0.8f, patch) * 0.35f);
                    Mix(weights, 7, Mathf.Max(In(village, 12), In(gate, 8)) * 0.9f);
                    Mix(weights, 2, 1 - Smooth(52, 66, village.At.DistanceTo(point)));
                    Mix(weights, 2, In(stone, 6) * 0.9f);
                    Mix(weights, 9, In(camp, 12) * 0.85f);

                    foreach (var place in Valley.Places)
                    {
                        if (place.Cave)
                        {
                            Mix(weights, 5, 1 - Smooth(place.Radius - 4, place.Radius + 4, place.At.DistanceTo(point)));
                            Mix(weights, 4, Smooth(place.Radius - 2, place.Radius + 6, place.At.DistanceTo(point)) * (1 - Smooth(place.Radius + 50, place.Radius + 70, place.At.DistanceTo(point))));
                        }
                    }

                    var river = DistanceTo(Valley.River, x, z, out _);
                    Mix(weights, 8, 1 - Smooth(8, 15, river));
                    Mix(weights, 5, 1 - Smooth(3, 9, river));
                    Mix(weights, 3, 1 - Smooth(1.6f, 3.4f, RoadDistance(x, z)));
                    Mix(weights, 4, Smooth(0.55f, 0.95f, slope));

                    for (var layer = 0; layer < weights.Length; layer++)
                    {
                        map[j, i, layer] = weights[layer];
                    }
                }
            }

            data.SetAlphamaps(0, 0, map);
        }

        /// <summary>Lays a layer over what is there by a share: the others give way.</summary>
        static void Mix(float[] weights, int layer, float share)
        {
            share = Mathf.Clamp01(share);

            if (share <= 0)
            {
                return;
            }

            for (var i = 0; i < weights.Length; i++)
            {
                weights[i] *= 1 - share;
            }

            weights[layer] += share;
        }
    }
}
