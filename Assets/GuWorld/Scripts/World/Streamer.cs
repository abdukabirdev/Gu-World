using System.Collections.Generic;
using System.Threading;
using GuWorld.Core;
using GuWorld.Core.Gen;
using UnityEngine;

namespace GuWorld.World
{
    /// <summary>
    /// Qing Mao's ground near the hero, a square tile at a time: each tile's heights and ground
    /// layers are worked out on worker threads from the world's plan and handed to a Unity terrain
    /// on the main thread; tiles far behind are taken down and their terrains kept for reuse. A
    /// coarse mesh of the whole map lies under them for the horizon.
    ///
    /// The tile under the hero is always loaded before the hero may walk on it (Ready).
    /// </summary>
    public class Streamer : MonoBehaviour
    {
        public const float TileSize = 500;
        public static Streamer Instance { get; private set; }

        class Tile
        {
            public int X, Z;
            public Terrain Terrain;
            public float[,] Heights;
            public float[,,] Alphas;
            public volatile bool Computed;
            public bool Applied;
            public bool Wanted;
        }

        WorldPlan plan;
        readonly Dictionary<long, Tile> tiles = new Dictionary<long, Tile>();
        readonly Queue<Terrain> spare = new Queue<Terrain>();
        TerrainLayer[] layers;
        Material material;
        int heightRes, alphaRes, radius;
        float wait;
        GameObject far;
        readonly object work = new object();
        readonly Queue<Tile> toCompute = new Queue<Tile>();
        readonly List<Thread> workers = new List<Thread>();
        volatile bool stopping;

        static long Key(int x, int z) => ((long)x << 32) ^ (uint)z;
        static int Count => (int)(Regions.Half * 2 / TileSize);

        public static Streamer Make(Transform parent, WorldPlan plan)
        {
            var root = new GameObject("Ground");
            root.transform.SetParent(parent, false);
            var streamer = root.AddComponent<Streamer>();
            streamer.Setup(plan);
            Instance = streamer;

            return streamer;
        }

        void Setup(WorldPlan world)
        {
            plan = world;
            // Finer ground and farther tiles for the better qualities.
            heightRes = Quality.Level >= 2 ? 513 : Quality.Level == 1 ? 257 : 129;
            alphaRes = Quality.Level >= 2 ? 512 : 256;
            radius = Quality.Level >= 2 ? 2 : 1;
            layers = new TerrainLayer[Surface.Layers];

            for (var i = 0; i < layers.Length; i++)
            {
                layers[i] = Resources.Load<TerrainLayer>("Terrain/" + Surface.LayerNames[i]);

                if (layers[i] == null)
                {
                    layers[i] = new TerrainLayer { diffuseTexture = Texture2D.grayTexture, tileSize = new Vector2(6, 6) };
                }
            }

            material = Resources.Load<Material>("Terrain/Terrain");
            var threads = Mathf.Clamp(SystemInfo.processorCount - 2, 1, 4);

            for (var i = 0; i < threads; i++)
            {
                var thread = new Thread(Work) { IsBackground = true, Name = "GU World ground " + i, Priority = System.Threading.ThreadPriority.BelowNormal };
                workers.Add(thread);
                thread.Start();
            }

            MakeFar();
        }

        void OnDestroy()
        {
            stopping = true;

            lock (work)
            {
                Monitor.PulseAll(work);
            }

            if (Instance == this)
            {
                Instance = null;
            }
        }

        // ------------------------------------------------------------------ The far land

        void MakeFar()
        {
            var cells = Quality.Level >= 2 ? 200 : 128;
            var data = Shapes.FarLand(plan, cells, 4);
            var mesh = Meshes.From(data, "Far land");
            var lit = Actors.Models.VertexLit(Color.white, 0);
            far = Actors.Models.Part("Far land", transform, mesh, lit, Vector3.zero, Vector3.one);
            far.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        // ------------------------------------------------------------------ Asking

        Tile TileAt(float x, float z)
        {
            var tx = Mathf.FloorToInt((x + Regions.Half) / TileSize);
            var tz = Mathf.FloorToInt((z + Regions.Half) / TileSize);

            return tiles.TryGetValue(Key(tx, tz), out var tile) ? tile : null;
        }

        /// <summary>The ground's height as the terrain has it where a tile is up, the plan's elsewhere.</summary>
        public float Height(Vector3 at)
        {
            var tile = TileAt(at.x, at.z);

            return tile != null && tile.Applied ? tile.Terrain.SampleHeight(at) + tile.Terrain.transform.position.y : plan.Land.Height(at.x, at.z);
        }

        /// <summary>True if the ground under a point is solid (its tile is up).</summary>
        public bool Ready(Vector3 at)
        {
            var tile = TileAt(at.x, at.z);

            return tile != null && tile.Applied;
        }

        // ------------------------------------------------------------------ Every frame

        void Update()
        {
            var hero = Game.Hero;
            var focus = hero != null ? hero.transform.position : Game.Rig != null ? Game.Rig.transform.position : Vector3.zero;
            wait -= Time.unscaledDeltaTime;

            if (wait <= 0)
            {
                wait = 0.25f;
                Want(focus);
            }

            // At most one tile handed to Unity a frame (the hand-over is the costly part on the main thread).
            foreach (var tile in tiles.Values)
            {
                if (tile.Computed && !tile.Applied && tile.Wanted)
                {
                    Apply(tile);
                    break;
                }
            }
        }

        /// <summary>Marks the tiles round a point as wanted, queues those not yet made, takes down those far away.</summary>
        void Want(Vector3 focus)
        {
            var cx = Mathf.FloorToInt((focus.x + Regions.Half) / TileSize);
            var cz = Mathf.FloorToInt((focus.z + Regions.Half) / TileSize);

            foreach (var tile in tiles.Values)
            {
                tile.Wanted = false;
            }

            // Nearest first.
            var order = new List<(int x, int z, float d)>();

            for (var dz = -radius; dz <= radius; dz++)
            {
                for (var dx = -radius; dx <= radius; dx++)
                {
                    var x = cx + dx;
                    var z = cz + dz;

                    if (x < 0 || z < 0 || x >= Count || z >= Count)
                    {
                        continue;
                    }

                    var centre = new Vector2(-Regions.Half + (x + 0.5f) * TileSize, -Regions.Half + (z + 0.5f) * TileSize);
                    order.Add((x, z, (centre - new Vector2(focus.x, focus.z)).sqrMagnitude));
                }
            }

            order.Sort((a, b) => a.d.CompareTo(b.d));

            foreach (var (x, z, _) in order)
            {
                if (!tiles.TryGetValue(Key(x, z), out var tile))
                {
                    tile = new Tile { X = x, Z = z };
                    tiles[Key(x, z)] = tile;

                    lock (work)
                    {
                        toCompute.Enqueue(tile);
                        Monitor.Pulse(work);
                    }
                }

                tile.Wanted = true;
            }

            // Tiles no longer wanted: down, their terrains kept for the next.
            var gone = new List<long>();

            foreach (var pair in tiles)
            {
                var tile = pair.Value;

                if (!tile.Wanted && (tile.Computed || tile.Applied))
                {
                    if (tile.Terrain != null)
                    {
                        tile.Terrain.gameObject.SetActive(false);
                        spare.Enqueue(tile.Terrain);
                    }

                    gone.Add(pair.Key);
                }
            }

            foreach (var key in gone)
            {
                tiles.Remove(key);
            }

            if (gone.Count > 0)
            {
                Neighbours();
            }
        }

        /// <summary>Makes every wanted tile at once (behind the loading screen): the ground under the hero must be there before they stand on it.</summary>
        public void LoadNow(Vector3 focus)
        {
            Want(focus);
            var pending = new List<Tile>();

            foreach (var tile in tiles.Values)
            {
                if (tile.Wanted && !tile.Applied)
                {
                    pending.Add(tile);
                }
            }

            foreach (var tile in pending)
            {
                if (!tile.Computed)
                {
                    lock (work)
                    {
                        // Not left for the workers: made here, now.
                        var rest = new Queue<Tile>();

                        while (toCompute.Count > 0)
                        {
                            var queued = toCompute.Dequeue();

                            if (queued != tile)
                            {
                                rest.Enqueue(queued);
                            }
                        }

                        while (rest.Count > 0)
                        {
                            toCompute.Enqueue(rest.Dequeue());
                        }
                    }

                    if (!tile.Computed)
                    {
                        Compute(tile);
                    }
                }

                Apply(tile);
            }
        }

        // ------------------------------------------------------------------ Worker threads

        void Work()
        {
            while (!stopping)
            {
                Tile tile = null;

                lock (work)
                {
                    while (toCompute.Count == 0 && !stopping)
                    {
                        Monitor.Wait(work);
                    }

                    if (stopping)
                    {
                        return;
                    }

                    tile = toCompute.Dequeue();
                }

                if (!tile.Computed)
                {
                    try
                    {
                        Compute(tile);
                    }
                    catch (System.Exception error)
                    {
                        Debug.LogError($"GU World: ground tile {tile.X},{tile.Z} failed: {error}");
                    }
                }
            }
        }

        /// <summary>A tile's heights and layers from the plan (any thread).</summary>
        void Compute(Tile tile)
        {
            lock (tile)
            {
                if (tile.Computed)
                {
                    return;
                }

                var x0 = -Regions.Half + tile.X * TileSize;
                var z0 = -Regions.Half + tile.Z * TileSize;
                var heights = new float[heightRes, heightRes];
                var land = plan.Land;
                var step = TileSize / (heightRes - 1);

                for (var j = 0; j < heightRes; j++)
                {
                    for (var i = 0; i < heightRes; i++)
                    {
                        heights[j, i] = land.Height(x0 + i * step, z0 + j * step) / Land.MaxHeight;
                    }
                }

                var alphas = new float[alphaRes, alphaRes, Surface.Layers];
                var weights = new float[Surface.Layers];
                var cell = TileSize / alphaRes;

                for (var j = 0; j < alphaRes; j++)
                {
                    var z = z0 + (j + 0.5f) * cell;
                    var hj = Mathf.Clamp(Mathf.RoundToInt((j + 0.5f) / alphaRes * (heightRes - 1)), 1, heightRes - 2);

                    for (var i = 0; i < alphaRes; i++)
                    {
                        var x = x0 + (i + 0.5f) * cell;
                        var hi = Mathf.Clamp(Mathf.RoundToInt((i + 0.5f) / alphaRes * (heightRes - 1)), 1, heightRes - 2);
                        var sx = (heights[hj, hi + 1] - heights[hj, hi - 1]) * Land.MaxHeight / (2 * step);
                        var sz = (heights[hj + 1, hi] - heights[hj - 1, hi]) * Land.MaxHeight / (2 * step);
                        plan.Surface.Paint(new Vec2(x, z), heights[hj, hi] * Land.MaxHeight, Mathf.Sqrt(sx * sx + sz * sz), weights);

                        for (var k = 0; k < Surface.Layers; k++)
                        {
                            alphas[j, i, k] = weights[k];
                        }
                    }
                }

                tile.Heights = heights;
                tile.Alphas = alphas;
                tile.Computed = true;
            }
        }

        // ------------------------------------------------------------------ The main thread

        void Apply(Tile tile)
        {
            if (tile.Applied)
            {
                return;
            }

            var terrain = spare.Count > 0 ? spare.Dequeue() : null;
            TerrainData data;

            if (terrain == null)
            {
                data = new TerrainData { name = "Ground tile" };
                data.heightmapResolution = heightRes;
                data.alphamapResolution = alphaRes;
                data.baseMapResolution = 256;
                data.size = new Vector3(TileSize, Land.MaxHeight, TileSize);
                data.terrainLayers = layers;
                var made = Terrain.CreateTerrainGameObject(data);
                made.transform.SetParent(transform, false);
                terrain = made.GetComponent<Terrain>();

                if (material != null)
                {
                    terrain.materialTemplate = material;
                }

                // Instanced drawing leaves the terrain invisible in the player (Unity 6, URP 17.6, D3D11): off.
                terrain.drawInstanced = false;
                terrain.heightmapPixelError = Quality.Level >= 2 ? 4 : Quality.Level == 1 ? 7 : 12;
                terrain.basemapDistance = Quality.Level >= 2 ? 260 : 140;
                terrain.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }

            data = terrain.terrainData;
            data.SetHeights(0, 0, tile.Heights);
            data.SetAlphamaps(0, 0, tile.Alphas);
            terrain.gameObject.name = $"Ground {tile.X},{tile.Z}";
            terrain.transform.position = new Vector3(-Regions.Half + tile.X * TileSize, 0, -Regions.Half + tile.Z * TileSize);
            terrain.gameObject.SetActive(true);
            terrain.Flush();
            tile.Terrain = terrain;
            tile.Applied = true;
            // The data is in the terrain now; the arrays may go.
            tile.Heights = null;
            tile.Alphas = null;
            Neighbours();
        }

        /// <summary>Tells each terrain its neighbours, so their seams match in level of detail.</summary>
        void Neighbours()
        {
            foreach (var tile in tiles.Values)
            {
                if (!tile.Applied)
                {
                    continue;
                }

                Terrain Near(int dx, int dz) => tiles.TryGetValue(Key(tile.X + dx, tile.Z + dz), out var other) && other.Applied ? other.Terrain : null;
                tile.Terrain.SetNeighbors(Near(-1, 0), Near(0, 1), Near(1, 0), Near(0, -1));
            }
        }
    }

    /// <summary>The engine's meshes from the generator's plain arrays.</summary>
    public static class Meshes
    {
        public static Mesh From(MeshData data, string name)
        {
            var count = data.VertexCount;
            var vertices = new Vector3[count];
            var normals = new Vector3[count];
            var uvs = new Vector2[count];
            var colours = new Color[count];

            for (var i = 0; i < count; i++)
            {
                vertices[i] = new Vector3(data.Positions[i * 3], data.Positions[i * 3 + 1], data.Positions[i * 3 + 2]);
                normals[i] = new Vector3(data.Normals[i * 3], data.Normals[i * 3 + 1], data.Normals[i * 3 + 2]);
                uvs[i] = new Vector2(data.Uvs[i * 2], data.Uvs[i * 2 + 1]);
                colours[i] = new Color(data.Colours[i * 4], data.Colours[i * 4 + 1], data.Colours[i * 4 + 2], data.Colours[i * 4 + 3]);
            }

            var mesh = new Mesh { name = name };

            if (count > 65000)
            {
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            }

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.colors = colours;
            mesh.triangles = data.Triangles.ToArray();
            mesh.RecalculateBounds();

            return mesh;
        }
    }
}
