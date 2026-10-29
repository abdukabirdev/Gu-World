using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GuWorld.Render;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GuWorld.World
{
    /// <summary>
    /// The builder's ground: a brush held down raises, lowers, levels (to the height where the stroke began, or one
    /// taken with Shift), smooths, paints one of the ground's layers (grass, rock, flagstones…) or gives the ground back
    /// as it was built. Every block of a terrain (32 heights) is kept as built the first time it is touched, and as it
    /// was before each stroke: a stroke is taken back block by block, and "as it was" knows what to go back to. The
    /// trees on the ground changed are set on it again (the terrains' own, for their trunks, and the drawn ones).
    /// </summary>
    public partial class WorldBuilder
    {
        /// <summary>A block of a terrain's heights (or paint) as it was before a step and after it.</summary>
        class Patch
        {
            public TerrainData Data;
            public bool Paint;
            public RectInt Area;
            public float[,] Before;
            public float[,] After;
            public float[,,] PaintBefore;
            public float[,,] PaintAfter;
        }

        /// <summary>The brush: its size (m), strength, and how soft its edge is (0: hard, 1: soft from the middle).</summary>
        public float Radius = 8;
        public float Strength = 0.6f;
        public float Softness = 0.5f;
        /// <summary>The height levelled to (taken with Shift and a click); NaN — the height where each stroke begins.</summary>
        public float FlattenTo = float.NaN;
        /// <summary>The ground's layer painted (its name, as the terrains know it).</summary>
        public string PaintLayer = "Flagstone";
        LineRenderer ring;

        const int Block = 32;
        /// <summary>Each block of a terrain as built (taken the first time it is touched, before the kept work is made again).</summary>
        readonly Dictionary<(TerrainData, int, int), float[,]> builtHeights = new Dictionary<(TerrainData, int, int), float[,]>();
        readonly Dictionary<(TerrainData, int, int), float[,,]> builtPaint = new Dictionary<(TerrainData, int, int), float[,,]>();
        /// <summary>Each block the stroke touched, as it was before the stroke.</summary>
        readonly Dictionary<(TerrainData, int, int), float[,]> strokeHeights = new Dictionary<(TerrainData, int, int), float[,]>();
        readonly Dictionary<(TerrainData, int, int), float[,,]> strokePaint = new Dictionary<(TerrainData, int, int), float[,,]>();
        readonly HashSet<TerrainData> stroke = new HashSet<TerrainData>();
        bool stroking;
        float strokeTarget;
        /// <summary>The ground's heights (and paint) changed, per terrain: the square of its map they lie in (kept in terrain.bin, paint.bin).</summary>
        readonly Dictionary<TerrainData, RectInt> dirty = new Dictionary<TerrainData, RectInt>();
        readonly Dictionary<TerrainData, RectInt> dirtyPaint = new Dictionary<TerrainData, RectInt>();
        /// <summary>Where the trees are to be set on the ground again (the terrains and the area, X and Z).</summary>
        readonly HashSet<TerrainData> refitTerrains = new HashSet<TerrainData>();
        Rect refitArea;
        bool refitWanted;

        static Terrain TerrainNamed(string name) => Terrain.activeTerrains.FirstOrDefault(t => t.terrainData != null && t.terrainData.name == name);

        static Terrain TerrainOf(TerrainData data) => Terrain.activeTerrains.FirstOrDefault(t => t.terrainData == data);

        static RectInt BlockArea(int resolution, int bx, int bz)
        {
            var x = bx * Block;
            var z = bz * Block;

            return new RectInt(x, z, Mathf.Min(Block, resolution - x), Mathf.Min(Block, resolution - z));
        }

        static void Dirty(Dictionary<TerrainData, RectInt> map, TerrainData data, RectInt r)
        {
            if (map.TryGetValue(data, out var was))
            {
                var xMin = Mathf.Min(was.xMin, r.xMin);
                var yMin = Mathf.Min(was.yMin, r.yMin);
                r = new RectInt(xMin, yMin, Mathf.Max(was.xMax, r.xMax) - xMin, Mathf.Max(was.yMax, r.yMax) - yMin);
            }

            map[data] = r;
        }

        /// <summary>The blocks of an area kept before they change: as built (the first time ever) and, for a stroke, as before it.</summary>
        void KeepHeights(TerrainData data, RectInt r, bool forStroke)
        {
            var resolution = data.heightmapResolution;

            for (var bz = r.yMin / Block; bz <= (r.yMax - 1) / Block; bz++)
            {
                for (var bx = r.xMin / Block; bx <= (r.xMax - 1) / Block; bx++)
                {
                    var key = (data, bx, bz);
                    var forThis = forStroke && !strokeHeights.ContainsKey(key);
                    var first = !builtHeights.ContainsKey(key);

                    if (forThis || first)
                    {
                        var a = BlockArea(resolution, bx, bz);
                        var heights = data.GetHeights(a.x, a.y, a.width, a.height);
                        if (forThis) strokeHeights[key] = heights;
                        if (first) builtHeights[key] = heights;
                    }
                }
            }
        }

        void KeepPaint(TerrainData data, RectInt r, bool forStroke)
        {
            var resolution = data.alphamapResolution;

            for (var bz = r.yMin / Block; bz <= (r.yMax - 1) / Block; bz++)
            {
                for (var bx = r.xMin / Block; bx <= (r.xMax - 1) / Block; bx++)
                {
                    var key = (data, bx, bz);
                    var forThis = forStroke && !strokePaint.ContainsKey(key);
                    var first = !builtPaint.ContainsKey(key);

                    if (forThis || first)
                    {
                        var a = BlockArea(resolution, bx, bz);
                        var maps = data.GetAlphamaps(a.x, a.y, a.width, a.height);
                        if (forThis) strokePaint[key] = maps;
                        if (first) builtPaint[key] = maps;
                    }
                }
            }
        }

        /// <summary>The ground's layers that can be painted (the first terrain's, by their names).</summary>
        public string[] PaintLayers
        {
            get
            {
                var terrain = Terrain.activeTerrains.FirstOrDefault(t => t.terrainData != null && t.terrainData.terrainLayers.Length > 0);

                return terrain != null ? terrain.terrainData.terrainLayers.Where(l => l != null).Select(l => l.name).ToArray() : new string[0];
            }
        }

        public static string LayerLabel(string name)
        {
            var n = name.ToLowerInvariant();

            return n.Contains("grass") ? "Трава" : n.Contains("forest") ? "Лесная земля" : n.Contains("rock") ? "Скала" : n.Contains("gravel") ? "Гравий" :
                n.Contains("dirt") ? "Земля" : n.Contains("mud") ? "Грязь" : n.Contains("flag") ? "Плиты" : n.Contains("moss") ? "Мох" : name;
        }

        static int LayerIndex(TerrainData data, string name)
        {
            var layers = data.terrainLayers;

            for (var i = 0; i < layers.Length; i++)
            {
                if (layers[i] != null && layers[i].name == name)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>The brush's strength at a distance from its middle: full inside, easing to nothing at its edge.</summary>
        float Falloff(float d)
        {
            if (d >= Radius)
            {
                return 0;
            }

            var inner = Radius * (1 - Softness);

            if (d <= inner)
            {
                return 1;
            }

            var f = 1 - (d - inner) / Mathf.Max(0.001f, Radius - inner);

            return f * f * (3 - 2 * f);
        }

        bool BuiltHeight(TerrainData data, int x, int z, out float height)
        {
            if (builtHeights.TryGetValue((data, x / Block, z / Block), out var block))
            {
                height = block[z % Block, x % Block];

                return true;
            }

            height = 0;

            return false;
        }

        /// <summary>The brush on every terrain under it (where a village's own ground lies over the mountain's, both: their edges stay together).</summary>
        void Brush(Vector3 centre, float dt)
        {
            foreach (var terrain in Terrain.activeTerrains)
            {
                var data = terrain.terrainData;
                var origin = terrain.GetPosition();
                var size = data.size;

                if (centre.x + Radius < origin.x || centre.x - Radius > origin.x + size.x || centre.z + Radius < origin.z || centre.z - Radius > origin.z + size.z)
                {
                    continue;
                }

                if (CurrentTool == Tool.Paint || CurrentTool == Tool.Restore)
                {
                    PaintOn(terrain, centre, dt);

                    if (CurrentTool == Tool.Paint)
                    {
                        continue;
                    }
                }

                var res = data.heightmapResolution;
                var x0 = Mathf.Clamp(Mathf.FloorToInt((centre.x - Radius - origin.x) / size.x * (res - 1)) - 1, 0, res - 1);
                var z0 = Mathf.Clamp(Mathf.FloorToInt((centre.z - Radius - origin.z) / size.z * (res - 1)) - 1, 0, res - 1);
                var x1 = Mathf.Clamp(Mathf.CeilToInt((centre.x + Radius - origin.x) / size.x * (res - 1)) + 1, 0, res - 1);
                var z1 = Mathf.Clamp(Mathf.CeilToInt((centre.z + Radius - origin.z) / size.z * (res - 1)) + 1, 0, res - 1);

                if (x1 <= x0 || z1 <= z0)
                {
                    continue;
                }

                var w = x1 - x0 + 1;
                var h = z1 - z0 + 1;
                var area = new RectInt(x0, z0, w, h);
                KeepHeights(data, area, true);
                var heights = data.GetHeights(x0, z0, w, h);
                var before = CurrentTool == Tool.Smooth ? (float[,])heights.Clone() : null;
                var step = Strength * dt;
                var target = (strokeTarget - origin.y) / size.y;
                var changed = false;

                for (var z = 0; z < h; z++)
                {
                    for (var x = 0; x < w; x++)
                    {
                        var px = origin.x + (x0 + x) * size.x / (res - 1);
                        var pz = origin.z + (z0 + z) * size.z / (res - 1);
                        var f = Falloff(Mathf.Sqrt((px - centre.x) * (px - centre.x) + (pz - centre.z) * (pz - centre.z)));

                        if (f <= 0)
                        {
                            continue;
                        }

                        var was = heights[z, x];

                        switch (CurrentTool)
                        {
                            case Tool.Raise: heights[z, x] += step * 6 / size.y * f; break;
                            case Tool.Lower: heights[z, x] -= step * 6 / size.y * f; break;
                            case Tool.Flatten: heights[z, x] = Mathf.Lerp(heights[z, x], target, Mathf.Clamp01(step * 8 * f)); break;
                            case Tool.Smooth:
                                if (x > 0 && z > 0 && x < w - 1 && z < h - 1)
                                {
                                    var mean = 0f;

                                    for (var dz = -1; dz <= 1; dz++)
                                    {
                                        for (var dx = -1; dx <= 1; dx++)
                                        {
                                            mean += before[z + dz, x + dx];
                                        }
                                    }

                                    heights[z, x] = Mathf.Lerp(heights[z, x], mean / 9, Mathf.Clamp01(step * 8 * f));
                                }

                                break;
                            case Tool.Restore:
                                if (BuiltHeight(data, x0 + x, z0 + z, out var built))
                                {
                                    heights[z, x] = Mathf.Lerp(heights[z, x], built, Mathf.Clamp01(step * 8 * f));
                                }

                                break;
                        }

                        heights[z, x] = Mathf.Clamp01(heights[z, x]);
                        changed |= heights[z, x] != was;
                    }
                }

                if (!changed)
                {
                    continue;
                }

                data.SetHeightsDelayLOD(x0, z0, heights);
                stroke.Add(data);
                Dirty(dirty, data, area);
                MarkRefit(data, new Rect(centre.x - Radius, centre.z - Radius, Radius * 2, Radius * 2));
            }
        }

        /// <summary>The paint brush (or "as it was" giving the paint back) on one terrain.</summary>
        void PaintOn(Terrain terrain, Vector3 centre, float dt)
        {
            var data = terrain.terrainData;
            var layer = LayerIndex(data, PaintLayer);
            var layers = data.alphamapLayers;

            if (layers == 0 || (CurrentTool == Tool.Paint && layer < 0))
            {
                return;
            }

            var origin = terrain.GetPosition();
            var size = data.size;
            var res = data.alphamapResolution;
            var x0 = Mathf.Clamp(Mathf.FloorToInt((centre.x - Radius - origin.x) / size.x * res) - 1, 0, res - 1);
            var z0 = Mathf.Clamp(Mathf.FloorToInt((centre.z - Radius - origin.z) / size.z * res) - 1, 0, res - 1);
            var x1 = Mathf.Clamp(Mathf.CeilToInt((centre.x + Radius - origin.x) / size.x * res) + 1, 0, res - 1);
            var z1 = Mathf.Clamp(Mathf.CeilToInt((centre.z + Radius - origin.z) / size.z * res) + 1, 0, res - 1);

            if (x1 <= x0 || z1 <= z0)
            {
                return;
            }

            var w = x1 - x0 + 1;
            var h = z1 - z0 + 1;
            var area = new RectInt(x0, z0, w, h);
            KeepPaint(data, area, true);
            var maps = data.GetAlphamaps(x0, z0, w, h);
            var changed = false;

            for (var z = 0; z < h; z++)
            {
                for (var x = 0; x < w; x++)
                {
                    var px = origin.x + (x0 + x + 0.5f) * size.x / res;
                    var pz = origin.z + (z0 + z + 0.5f) * size.z / res;
                    var k = Mathf.Clamp01(Strength * dt * 4 * Falloff(Mathf.Sqrt((px - centre.x) * (px - centre.x) + (pz - centre.z) * (pz - centre.z))));

                    if (k <= 0)
                    {
                        continue;
                    }

                    if (CurrentTool == Tool.Paint)
                    {
                        for (var l = 0; l < layers; l++)
                        {
                            maps[z, x, l] = l == layer ? maps[z, x, l] + (1 - maps[z, x, l]) * k : maps[z, x, l] * (1 - k);
                        }

                        changed = true;
                    }
                    else if (builtPaint.TryGetValue((data, (x0 + x) / Block, (z0 + z) / Block), out var block))
                    {
                        for (var l = 0; l < layers; l++)
                        {
                            maps[z, x, l] = Mathf.Lerp(maps[z, x, l], block[(z0 + z) % Block, (x0 + x) % Block, l], k);
                        }

                        changed = true;
                    }
                }
            }

            if (changed)
            {
                data.SetAlphamaps(x0, z0, maps);
                Dirty(dirtyPaint, data, area);
            }
        }

        void MarkRefit(TerrainData data, Rect area)
        {
            refitTerrains.Add(data);
            refitArea = refitWanted ? Rect.MinMaxRect(Mathf.Min(refitArea.xMin, area.xMin), Mathf.Min(refitArea.yMin, area.yMin), Mathf.Max(refitArea.xMax, area.xMax), Mathf.Max(refitArea.yMax, area.yMax)) : area;
            refitWanted = true;
        }

        /// <summary>The trees on the ground changed set on it again: the terrains' own (their trunks are solid) and the drawn ones.</summary>
        void FlushRefit()
        {
            if (!refitWanted)
            {
                return;
            }

            refitWanted = false;

            foreach (var data in refitTerrains)
            {
                if (data != null && data.treeInstanceCount > 0)
                {
                    data.SetTreeInstances(data.treeInstances, true);
                }
            }

            refitTerrains.Clear();
            TreeInstancer.Current?.Refit(refitArea, p => GroundY(p, 0));
        }

        /// <summary>A stroke finished: its blocks kept as one step (taken back with Ctrl+Z), the trees set on the ground.</summary>
        void EndStroke()
        {
            stroking = false;

            foreach (var data in stroke)
            {
                data.SyncHeightmap();
            }

            stroke.Clear();

            if (strokeHeights.Count == 0 && strokePaint.Count == 0)
            {
                return;
            }

            var step = new Step { Name = "Земля: " + NameOf(CurrentTool).ToLowerInvariant() };

            foreach (var pair in strokeHeights)
            {
                var (data, bx, bz) = pair.Key;
                var a = BlockArea(data.heightmapResolution, bx, bz);
                step.Ground.Add(new Patch { Data = data, Area = a, Before = pair.Value, After = data.GetHeights(a.x, a.y, a.width, a.height) });
            }

            foreach (var pair in strokePaint)
            {
                var (data, bx, bz) = pair.Key;
                var a = BlockArea(data.alphamapResolution, bx, bz);
                step.Ground.Add(new Patch { Data = data, Paint = true, Area = a, PaintBefore = pair.Value, PaintAfter = data.GetAlphamaps(a.x, a.y, a.width, a.height) });
            }

            strokeHeights.Clear();
            strokePaint.Clear();
            FlushRefit();
            Push(step);
        }

        /// <summary>A block put as a step left it (taken back: as it was before).</summary>
        void PutPatch(Patch patch, bool back)
        {
            if (patch.Data == null)
            {
                return;
            }

            if (patch.Paint)
            {
                patch.Data.SetAlphamaps(patch.Area.x, patch.Area.y, back ? patch.PaintBefore : patch.PaintAfter);
                Dirty(dirtyPaint, patch.Data, patch.Area);

                return;
            }

            patch.Data.SetHeights(patch.Area.x, patch.Area.y, back ? patch.Before : patch.After);
            Dirty(dirty, patch.Data, patch.Area);
            var terrain = TerrainOf(patch.Data);

            if (terrain != null)
            {
                var o = terrain.GetPosition();
                var cell = patch.Data.size.x / (patch.Data.heightmapResolution - 1);
                MarkRefit(patch.Data, new Rect(o.x + patch.Area.x * cell, o.z + patch.Area.y * cell, patch.Area.width * cell, patch.Area.height * cell));
            }
        }

        static bool Same(float[,] a, float[,] b)
        {
            for (var z = 0; z < a.GetLength(0); z++)
            {
                for (var x = 0; x < a.GetLength(1); x++)
                {
                    if (Mathf.Abs(a[z, x] - b[z, x]) > 1e-7f)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        static bool Same(float[,,] a, float[,,] b)
        {
            for (var z = 0; z < a.GetLength(0); z++)
            {
                for (var x = 0; x < a.GetLength(1); x++)
                {
                    for (var l = 0; l < a.GetLength(2); l++)
                    {
                        if (Mathf.Abs(a[z, x, l] - b[z, x, l]) > 1e-5f)
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        /// <summary>All the ground given back as built (every block ever touched); returns the blocks changed, to be taken back.</summary>
        List<Patch> RestoreAllGround()
        {
            EndStroke();
            var patches = new List<Patch>();

            foreach (var pair in builtHeights)
            {
                var (data, bx, bz) = pair.Key;
                var a = BlockArea(data.heightmapResolution, bx, bz);
                var now = data.GetHeights(a.x, a.y, a.width, a.height);

                if (!Same(now, pair.Value))
                {
                    var patch = new Patch { Data = data, Area = a, Before = now, After = pair.Value };
                    PutPatch(patch, false);
                    patches.Add(patch);
                }
            }

            foreach (var pair in builtPaint)
            {
                var (data, bx, bz) = pair.Key;
                var a = BlockArea(data.alphamapResolution, bx, bz);
                var now = data.GetAlphamaps(a.x, a.y, a.width, a.height);

                if (!Same(now, pair.Value))
                {
                    var patch = new Patch { Data = data, Paint = true, Area = a, PaintBefore = now, PaintAfter = pair.Value };
                    PutPatch(patch, false);
                    patches.Add(patch);
                }
            }

            dirty.Clear();
            dirtyPaint.Clear();
            FlushRefit();

            return patches;
        }

        void GroundTick(Keyboard keyboard, Mouse mouse, float dt, bool typing)
        {
            BrushKeys(keyboard, mouse, typing);
            var left = mouse.leftButton;

            if (!PickGround(out var point) || (PointerOnPage && !stroking))
            {
                if (ring != null) ring.enabled = false;
                if (stroking && !left.isPressed) EndStroke();

                return;
            }

            DrawRing(point, new Color(0.95f, 0.8f, 0.4f));

            if (left.wasPressedThisFrame && !PointerOnPage)
            {
                if (CurrentTool == Tool.Flatten && keyboard.shiftKey.isPressed)
                {
                    FlattenTo = point.y;
                    Status = $"Высота для выравнивания: {FlattenTo:0.0} м";
                    Touch();

                    return;
                }

                stroking = true;
                strokeTarget = float.IsNaN(FlattenTo) ? point.y : FlattenTo;
            }

            if (stroking)
            {
                if (left.isPressed)
                {
                    Brush(point, dt);
                }
                else
                {
                    EndStroke();
                }
            }
        }

        /// <summary>The brush's keys: [ and ] (or Ctrl with the wheel) its size, Alt with the wheel its strength.</summary>
        void BrushKeys(Keyboard keyboard, Mouse mouse, bool typing)
        {
            var wheel = PointerOnPage ? 0 : mouse.scroll.ReadValue().y / 120f;
            var was = (Radius, Strength);

            if (!typing)
            {
                if (keyboard.leftBracketKey.wasPressedThisFrame) Radius = Mathf.Max(1, Radius / 1.2f);
                if (keyboard.rightBracketKey.wasPressedThisFrame) Radius = Mathf.Min(120, Radius * 1.2f);
            }

            if (keyboard.ctrlKey.isPressed && wheel != 0) Radius = Mathf.Clamp(Radius * Mathf.Pow(1.1f, wheel), 1, 120);
            if (keyboard.altKey.isPressed && wheel != 0) Strength = Mathf.Clamp(Strength * Mathf.Pow(1.1f, wheel), 0.05f, 4);

            if (was != (Radius, Strength))
            {
                Status = $"Кисть {Radius:0} м · сила {Strength:0.00}";
            }
        }

        void DrawRing(Vector3 point, Color colour)
        {
            if (ring == null)
            {
                ring = NewLine("Brush", 65, colour);
            }

            ring.startColor = ring.endColor = colour;
            ring.enabled = true;
            ring.positionCount = 65;
            ring.widthMultiplier = Mathf.Clamp(Vector3.Distance(eye.transform.position, point) * 0.004f, 0.06f, 6);

            for (var i = 0; i < 65; i++)
            {
                var a = i * Mathf.PI * 2 / 64;
                var p = point + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * Radius;
                var y = GroundY(new Vector2(p.x, p.z));
                p.y = (float.IsNaN(y) ? point.y : y) + 0.2f;
                ring.SetPosition(i, p);
            }
        }

        // ── Kept ───────────────────────────────────────────────────────

        /// <summary>terrain.bin and paint.bin: for every terrain changed, the square of its heights (or paint) that changed (name, x, z, width, depth, then the values in 16 bits, or the layers' weights in 8).</summary>
        void SaveGround()
        {
            using (var writer = new BinaryWriter(File.Create(Path.Combine(Folder, "terrain.bin"))))
            {
                writer.Write(1);
                writer.Write(dirty.Count);

                foreach (var pair in dirty)
                {
                    var r = pair.Value;
                    var heights = pair.Key.GetHeights(r.x, r.y, r.width, r.height);
                    writer.Write(pair.Key.name);
                    writer.Write(r.x);
                    writer.Write(r.y);
                    writer.Write(r.width);
                    writer.Write(r.height);

                    for (var z = 0; z < r.height; z++)
                    {
                        for (var x = 0; x < r.width; x++)
                        {
                            writer.Write((ushort)Mathf.RoundToInt(Mathf.Clamp01(heights[z, x]) * 65535));
                        }
                    }
                }
            }

            using (var writer = new BinaryWriter(File.Create(Path.Combine(Folder, "paint.bin"))))
            {
                writer.Write(1);
                writer.Write(dirtyPaint.Count);

                foreach (var pair in dirtyPaint)
                {
                    var r = pair.Value;
                    var maps = pair.Key.GetAlphamaps(r.x, r.y, r.width, r.height);
                    var layers = maps.GetLength(2);
                    writer.Write(pair.Key.name);
                    writer.Write(r.x);
                    writer.Write(r.y);
                    writer.Write(r.width);
                    writer.Write(r.height);
                    writer.Write(layers);

                    for (var z = 0; z < r.height; z++)
                    {
                        for (var x = 0; x < r.width; x++)
                        {
                            for (var l = 0; l < layers; l++)
                            {
                                writer.Write((byte)Mathf.RoundToInt(Mathf.Clamp01(maps[z, x, l]) * 255));
                            }
                        }
                    }
                }
            }
        }

        string Kept(string file)
        {
            var path = Path.Combine(Folder, file);
            var defaults = Path.Combine(Application.streamingAssetsPath, "builder", file);

            return File.Exists(path) ? path : File.Exists(defaults) ? defaults : null;
        }

        void LoadGround()
        {
            var file = Kept("terrain.bin");

            if (file != null)
            {
                try
                {
                    using var reader = new BinaryReader(File.OpenRead(file));
                    reader.ReadInt32();
                    var count = reader.ReadInt32();

                    for (var i = 0; i < count; i++)
                    {
                        var name = reader.ReadString();
                        var r = new RectInt(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
                        var heights = new float[r.height, r.width];

                        for (var z = 0; z < r.height; z++)
                        {
                            for (var x = 0; x < r.width; x++)
                            {
                                heights[z, x] = reader.ReadUInt16() / 65535f;
                            }
                        }

                        if (TerrainNamed(name) is Terrain terrain)
                        {
                            // As built first (to give it back), then as the builder left it.
                            KeepHeights(terrain.terrainData, r, false);
                            terrain.terrainData.SetHeights(r.x, r.y, heights);
                            Dirty(dirty, terrain.terrainData, r);
                            var o = terrain.GetPosition();
                            var cell = terrain.terrainData.size.x / (terrain.terrainData.heightmapResolution - 1);
                            MarkRefit(terrain.terrainData, new Rect(o.x + r.x * cell, o.z + r.y * cell, r.width * cell, r.height * cell));
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Gu World builder: the saved ground could not be read: {e.Message}");
                }
            }

            file = Kept("paint.bin");

            if (file != null)
            {
                try
                {
                    using var reader = new BinaryReader(File.OpenRead(file));
                    reader.ReadInt32();
                    var count = reader.ReadInt32();

                    for (var i = 0; i < count; i++)
                    {
                        var name = reader.ReadString();
                        var r = new RectInt(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
                        var layers = reader.ReadInt32();
                        var maps = new float[r.height, r.width, layers];

                        for (var z = 0; z < r.height; z++)
                        {
                            for (var x = 0; x < r.width; x++)
                            {
                                for (var l = 0; l < layers; l++)
                                {
                                    maps[z, x, l] = reader.ReadByte() / 255f;
                                }
                            }
                        }

                        if (TerrainNamed(name) is Terrain terrain && terrain.terrainData.alphamapLayers == layers)
                        {
                            KeepPaint(terrain.terrainData, r, false);
                            terrain.terrainData.SetAlphamaps(r.x, r.y, maps);
                            Dirty(dirtyPaint, terrain.terrainData, r);
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Gu World builder: the saved paint could not be read: {e.Message}");
                }
            }

            FlushRefit();
        }
    }
}
