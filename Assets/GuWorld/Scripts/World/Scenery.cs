using System.Collections.Generic;
using GuWorld.Actors;
using GuWorld.Core;
using UnityEngine;

namespace GuWorld.World
{
    /// <summary>
    /// What stands on the ground: the plan's buildings (the artist's models), the groves and
    /// woods strewn by the plan's patches, the river's water and its fall, the people, the
    /// things to use, the caves' lights and the beacon over where the story goes next.
    /// </summary>
    public static class Scenery
    {
        public static void Build(Transform parent)
        {
            var built = new GameObject("Buildings").transform;
            built.SetParent(parent, false);

            foreach (var building in Valley.Buildings)
            {
                Models.Prefab(building.Model, built, Game.Ground(new Vector3(building.At.X, 0, building.At.Z)), building.Facing, building.Scale);
            }

            // On the big world the woods, the water and the bridges are the plan's (Forest, Places).
            if (!Ground.Big)
            {
                Strew(parent);
                Water(parent);
            }

            var things = new GameObject("Things").transform;
            things.SetParent(parent, false);

            foreach (var thing in Valley.Things)
            {
                Thing.Make(thing, things);
            }

            var people = new GameObject("People").transform;
            people.SetParent(parent, false);

            foreach (var npc in Valley.Npcs)
            {
                Npc.Make(npc, people);
            }

            foreach (var place in Valley.Places)
            {
                if (place.Cave)
                {
                    CaveLights(place, parent);
                }
            }

            new GameObject("Beacon").AddComponent<Beacon>().transform.SetParent(parent, false);
        }

        // ------------------------------------------------------------------ Trees and bamboo

        static bool Clear(float x, float z, float margin)
        {
            if (Mathf.Abs(x) > Ground.Half - 30 || Mathf.Abs(z) > Ground.Half - 30 || Ground.RoadDistance(x, z) < margin + 2 || Ground.DistanceTo(Valley.River, x, z, out _) < margin + 9)
            {
                return false;
            }

            var point = new MapPoint(x, z);

            foreach (var place in Valley.Places)
            {
                // Levelled places are kept open (the fights are there); the woods grow round them.
                if (!float.IsNaN(place.Level) && place.At.DistanceTo(point) < place.Radius + (place.Cave ? 70 : margin))
                {
                    return false;
                }
            }

            foreach (var thing in Valley.Things)
            {
                if (thing.At.DistanceTo(point) < margin + 2)
                {
                    return false;
                }
            }

            // Not on a cliff.
            var here = Ground.HeightAt(x, z);

            return Mathf.Abs(Ground.HeightAt(x + 3, z) - here) < 2.4f && Mathf.Abs(Ground.HeightAt(x, z + 3) - here) < 2.4f;
        }

        static void Strew(Transform parent)
        {
            var root = new GameObject("Woods").transform;
            root.SetParent(parent, false);
            var random = new System.Random(20261111);
            var placed = new List<Vector3>();

            foreach (var patch in Valley.Scatter)
            {
                var tree = patch.Model.StartsWith("tree_");
                var count = Mathf.Max(1, Mathf.RoundToInt(patch.Count * (patch.Count > 2 ? Quality.Trees : 1)));

                for (var i = 0; i < count; i++)
                {
                    for (var attempt = 0; attempt < 12; attempt++)
                    {
                        var angle = (float)random.NextDouble() * Mathf.PI * 2;
                        var far = Mathf.Sqrt((float)random.NextDouble()) * patch.Radius;
                        var x = patch.At.X + Mathf.Sin(angle) * far;
                        var z = patch.At.Z + Mathf.Cos(angle) * far;
                        var scale = Mathf.Lerp(patch.MinScale, patch.MaxScale, (float)random.NextDouble());
                        var gap = tree ? 7 * scale + 3 : 4.5f;

                        if (!Clear(x, z, patch.Clear) || placed.Exists(p => (p.x - x) * (p.x - x) + (p.z - z) * (p.z - z) < gap * gap))
                        {
                            continue;
                        }

                        var at = new Vector3(x, Ground.HeightAt(x, z) - 0.15f, z);
                        var made = Models.Prefab(patch.Model, root, at, (float)random.NextDouble() * 360, scale);
                        placed.Add(at);

                        if (made != null && tree)
                        {
                            // The limbs' own solids are for climbing, which this game has none of: the trunk is enough.
                            foreach (var box in made.GetComponentsInChildren<BoxCollider>())
                            {
                                Object.Destroy(box);
                            }
                        }

                        break;
                    }
                }
            }
        }

        // ------------------------------------------------------------------ The river

        static void Water(Transform parent)
        {
            var root = new GameObject("River").transform;
            root.SetParent(parent, false);
            var water = Resources.Load<Material>("Mats/Water");
            var falling = Resources.Load<Material>("Mats/Waterfall");
            water = water != null ? water : Models.Glow(new Color(0.2f, 0.4f, 0.5f, 0.7f), false, Texture2D.whiteTexture);
            falling = falling != null ? falling : water;

            // The plan's line, finely cut, and split where it goes over the cliff.
            var upper = new List<Vector3>();
            var lower = new List<Vector3>();
            var fall = Vector3.zero;

            for (var i = 0; i < Valley.River.Length - 1; i++)
            {
                var a = Valley.River[i];
                var b = Valley.River[i + 1];
                var steps = Mathf.CeilToInt(a.DistanceTo(b) / 10);

                for (var k = 0; k <= steps; k++)
                {
                    var t = k / (float)steps;
                    var x = Mathf.Lerp(a.X, b.X, t);
                    var z = Mathf.Lerp(a.Z, b.Z, t);

                    if (z > Valley.PlateauZ + 3)
                    {
                        upper.Add(new Vector3(x, Valley.WaterfallTop, z));
                        fall = new Vector3(x, 0, Valley.PlateauZ + 3);
                    }
                    else if (z < Valley.PlateauZ - 3)
                    {
                        lower.Add(new Vector3(x, Valley.WaterfallFoot, z));
                    }
                }
            }

            Ribbon("Upper", root, upper, 13, water);
            Ribbon("Lower", root, lower, 13, water);

            // The fall: a sheet from the plateau's lip to the pool, a little out from the rock.
            var sheet = new Mesh { name = "Fall" };
            const float half = 6.5f;
            var top = Valley.WaterfallTop + 0.1f;
            var foot = Valley.WaterfallFoot - 0.2f;
            sheet.vertices = new[]
            {
                new Vector3(fall.x - half, top, fall.z + 1), new Vector3(fall.x + half, top, fall.z + 1),
                new Vector3(fall.x - half, top - 1, fall.z - 5), new Vector3(fall.x + half, top - 1, fall.z - 5),
                new Vector3(fall.x - half, foot, fall.z - 9), new Vector3(fall.x + half, foot, fall.z - 9),
            };
            sheet.uv = new[] { new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 0.9f), new Vector2(1, 0.9f), new Vector2(0, 0), new Vector2(1, 0) };
            sheet.triangles = new[] { 0, 1, 2, 1, 3, 2, 2, 3, 4, 3, 5, 4, 0, 2, 1, 1, 2, 3, 2, 4, 3, 3, 4, 5 };
            sheet.RecalculateNormals();
            sheet.RecalculateBounds();
            Models.Part("Fall", root, sheet, falling, Vector3.zero, Vector3.one).GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var mist = Models.Part("Mist", root, Models.Quad, Models.Glow(new Color(0.8f, 0.9f, 1f, 0.22f), false), new Vector3(fall.x, foot + 3, fall.z - 9), new Vector3(22, 9, 1));
            mist.AddComponent<Fx.Billboard>();
        }

        static void Ribbon(string name, Transform parent, List<Vector3> line, float width, Material material)
        {
            if (line.Count < 2)
            {
                return;
            }

            var vertices = new Vector3[line.Count * 2];
            var uvs = new Vector2[vertices.Length];
            var triangles = new int[(line.Count - 1) * 6];
            var along = 0f;

            for (var i = 0; i < line.Count; i++)
            {
                var forward = (line[Mathf.Min(i + 1, line.Count - 1)] - line[Mathf.Max(i - 1, 0)]).normalized;
                var side = Vector3.Cross(Vector3.up, forward) * width * 0.5f;
                along += i > 0 ? (line[i] - line[i - 1]).magnitude : 0;
                vertices[i * 2] = line[i] - side;
                vertices[i * 2 + 1] = line[i] + side;
                uvs[i * 2] = new Vector2(0, along / width);
                uvs[i * 2 + 1] = new Vector2(1, along / width);

                if (i < line.Count - 1)
                {
                    var t = i * 6;
                    var v = i * 2;
                    triangles[t] = v; triangles[t + 1] = v + 2; triangles[t + 2] = v + 1;
                    triangles[t + 3] = v + 1; triangles[t + 4] = v + 2; triangles[t + 5] = v + 3;
                }
            }

            var mesh = new Mesh { name = name };
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            var part = Models.Part(name, parent, mesh, material, Vector3.zero, Vector3.one);
            part.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        // ------------------------------------------------------------------ The caves

        static void CaveLights(PlaceDef place, Transform parent)
        {
            var colour = Models.Rgb(place.Colour);
            var count = Quality.Level == 0 ? 3 : 5;

            for (var i = 0; i < count; i++)
            {
                var angle = i * Mathf.PI * 2 / count;
                var far = i == 0 ? 0 : place.Radius * 0.55f;
                var at = Game.Ground(new Vector3(place.At.X + Mathf.Sin(angle) * far, 0, place.At.Z + Mathf.Cos(angle) * far));
                var lamp = new GameObject("Cave light");
                lamp.transform.SetParent(parent, false);
                lamp.transform.position = at + Vector3.up * 5;
                var light = lamp.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = colour;
                light.range = 34;
                light.intensity = 3.2f;
                light.shadows = LightShadows.None;
                Models.Part("Glow", lamp.transform, Models.Quad, Models.Glow(new Color(colour.r, colour.g, colour.b, 0.5f)), Vector3.zero, Vector3.one * 2.5f).AddComponent<Fx.Billboard>();
                lamp.AddComponent<Fx.Hover>().Rise = 0.5f;
            }
        }
    }

    /// <summary>A column of gold light over the place the story's next step is at (gone when the hero is close).</summary>
    public class Beacon : MonoBehaviour
    {
        Transform beam;
        float wait;

        void Start()
        {
            beam = Models.Part("Beam", transform, Models.Quad, Models.Glow(new Color(1f, 0.82f, 0.45f, 0.5f)), new Vector3(0, 40, 0), new Vector3(1.6f, 80, 1)).transform;
            beam.gameObject.AddComponent<Fx.UprightBillboard>();
            Models.GroundGlow("Mark", transform, new Color(1f, 0.82f, 0.45f, 0.8f), 3, true).AddComponent<Fx.Hover>().Spin = 40;
        }

        void Update()
        {
            wait -= Time.deltaTime;

            if (wait > 0 || Game.Session == null || Game.Hero == null)
            {
                return;
            }

            wait = 0.4f;
            var quest = Game.Session.MainQuest;
            var shown = quest != null && Game.Session.MarkerOf(quest, out var at);

            if (shown)
            {
                Game.Session.MarkerOf(quest, out var point);
                transform.position = Game.Ground(new Vector3(point.X, 0, point.Z));
                shown = Game.Hero.FlatDistance(transform.position) > 7;
            }

            foreach (Transform child in transform)
            {
                child.gameObject.SetActive(shown);
            }
        }
    }
}
