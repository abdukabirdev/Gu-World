using GuWorld.Actors;
using GuWorld.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace GuWorld.World
{
    /// <summary>
    /// The light of the valley: a sun that goes round in twenty-four minutes, a blue moon at
    /// night, a sky that is a dome of two colours, fog that takes the horizon's colour — and
    /// the dark of the two caves, each with its own tint, when the hero is in one.
    /// </summary>
    public class Atmosphere : MonoBehaviour
    {
        /// <summary>The hour of the day, 0–24.</summary>
        public static float Hour = 9;
        const float SecondsPerHour = 60;
        /// <summary>The sky over the hero now (from the day, the hour and the hero's height: the same on every machine).</summary>
        public static Sky Sky { get; private set; } = Sky.Clear;
        /// <summary>How much of the weather is in the picture, 0 clear – 1 full (it eases from one sky to the next).</summary>
        public static float Gloom { get; private set; }
        float flash;

        Light sun;
        Mesh dome;
        Transform domeRoot, moon;
        Color[] domeColours;
        Vector3[] domeVertices;
        Material moonMaterial;
        float cave;

        struct Key
        {
            public float Hour;
            public Color Sun, Top, Horizon, Ambient;
            public float Strength;
        }

        static Color C(int rgb) => Models.Rgb(rgb);

        // The art direction is dark and cold: even noon is overcast-blue, and night belongs to the moon.
        static readonly Key[] Keys =
        {
            new Key { Hour = 0, Sun = C(0x86A8E8), Top = C(0x05070F), Horizon = C(0x16233A), Ambient = C(0x2A3550), Strength = 0.55f },
            new Key { Hour = 5, Sun = C(0x86A8E8), Top = C(0x0A1020), Horizon = C(0x2A3550), Ambient = C(0x303C58), Strength = 0.5f },
            new Key { Hour = 6.5f, Sun = C(0xFFB080), Top = C(0x2A3C62), Horizon = C(0xC88C6C), Ambient = C(0x5A5860), Strength = 0.8f },
            new Key { Hour = 9, Sun = C(0xFFE8CC), Top = C(0x34587E), Horizon = C(0x9FB4C4), Ambient = C(0x6C7888), Strength = 1.25f },
            new Key { Hour = 13, Sun = C(0xFFF4E4), Top = C(0x2E5684), Horizon = C(0xA8BCCC), Ambient = C(0x74808E), Strength = 1.4f },
            new Key { Hour = 17, Sun = C(0xFFD8A8), Top = C(0x30486E), Horizon = C(0xC8A888), Ambient = C(0x6A6C74), Strength = 1.1f },
            new Key { Hour = 19, Sun = C(0xFF8850), Top = C(0x1C2444), Horizon = C(0xB86848), Ambient = C(0x4A4458), Strength = 0.6f },
            new Key { Hour = 20.5f, Sun = C(0x86A8E8), Top = C(0x080C18), Horizon = C(0x1C2A44), Ambient = C(0x2C3852), Strength = 0.5f },
            new Key { Hour = 24, Sun = C(0x86A8E8), Top = C(0x05070F), Horizon = C(0x16233A), Ambient = C(0x2A3550), Strength = 0.55f },
        };

        public static void Make(Transform parent)
        {
            var root = new GameObject("Atmosphere");
            root.transform.SetParent(parent, false);
            root.AddComponent<Atmosphere>().Build();
        }

        void Build()
        {
            var lamp = new GameObject("Sun");
            lamp.transform.SetParent(transform, false);
            sun = lamp.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.85f;
            RenderSettings.sun = sun;
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;

            // The sky: a dome that goes with the camera, coloured from the horizon up.
            const int rings = 10, steps = 24;
            domeVertices = new Vector3[(rings + 1) * (steps + 1)];
            domeColours = new Color[domeVertices.Length];
            var triangles = new int[rings * steps * 6];

            for (var r = 0; r <= rings; r++)
            {
                var up = Mathf.Lerp(-0.25f, 1, r / (float)rings) * Mathf.PI * 0.5f;

                for (var s = 0; s <= steps; s++)
                {
                    var round = s * Mathf.PI * 2 / steps;
                    domeVertices[r * (steps + 1) + s] = new Vector3(Mathf.Sin(round) * Mathf.Cos(up), Mathf.Sin(up), Mathf.Cos(round) * Mathf.Cos(up));
                }
            }

            var t = 0;

            for (var r = 0; r < rings; r++)
            {
                for (var s = 0; s < steps; s++)
                {
                    var a = r * (steps + 1) + s;
                    var b = a + steps + 1;
                    triangles[t++] = a; triangles[t++] = a + 1; triangles[t++] = b;
                    triangles[t++] = a + 1; triangles[t++] = b + 1; triangles[t++] = b;
                }
            }

            dome = new Mesh { name = "Sky" };
            dome.vertices = domeVertices;
            dome.uv = new Vector2[domeVertices.Length];
            dome.colors = domeColours;
            dome.triangles = triangles;
            dome.bounds = new Bounds(Vector3.zero, Vector3.one * 4000);
            var skyMaterial = new Material(Models.Glow(Color.white, false, Texture2D.whiteTexture)) { name = "Sky", renderQueue = 2400 };
            domeRoot = Models.Part("Sky", transform, dome, skyMaterial, Vector3.zero, Vector3.one * 1200).transform;
            moonMaterial = new Material(Models.Glow(new Color(0.75f, 0.86f, 1f, 1f))) { name = "Moon", renderQueue = 2410 };
            moon = Models.Part("Moon", transform, Models.Quad, moonMaterial, Vector3.zero, Vector3.one * 150).transform;

            foreach (var drawn in new[] { domeRoot, moon })
            {
                var renderer = drawn.GetComponent<MeshRenderer>();
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
            Apply();
        }

        static Key At(float hour)
        {
            hour = Mathf.Repeat(hour, 24);

            for (var i = 0; i < Keys.Length - 1; i++)
            {
                if (hour <= Keys[i + 1].Hour)
                {
                    var a = Keys[i];
                    var b = Keys[i + 1];
                    var k = Mathf.InverseLerp(a.Hour, b.Hour, hour);

                    return new Key
                    {
                        Sun = Color.Lerp(a.Sun, b.Sun, k), Top = Color.Lerp(a.Top, b.Top, k), Horizon = Color.Lerp(a.Horizon, b.Horizon, k),
                        Ambient = Color.Lerp(a.Ambient, b.Ambient, k), Strength = Mathf.Lerp(a.Strength, b.Strength, k)
                    };
                }
            }

            return Keys[0];
        }

        public static bool Night => Hour < 5.5f || Hour > 19.8f;

        void Update()
        {
            if (Game.Playing && !Game.Frozen)
            {
                Hour = Mathf.Repeat(Hour + Time.deltaTime / SecondsPerHour, 24);
            }

            Apply();
        }

        void Apply()
        {
            var key = At(Hour);
            var night = Night;

            // In a cave the sky's light does not reach: the place's own colour instead.
            var hero = Game.Hero;
            var place = hero != null ? Atlas.PlaceAt(new MapPoint(hero.transform.position.x, hero.transform.position.z)) : null;

            if (Game.Session != null && hero != null)
            {
                Sky = Weather.At(Game.Session.State.Day, Hour, hero.transform.position.y);
            }

            var gloomWanted = Sky == Sky.Storm ? 1f : Sky == Sky.Rain || Sky == Sky.Snow ? 0.7f : Sky == Sky.Fog ? 0.6f : Sky == Sky.Cloudy ? 0.35f : 0f;
            Gloom = Mathf.MoveTowards(Gloom, gloomWanted, Time.unscaledDeltaTime * 0.05f);

            // Lightning now and then in a storm: a flash of the whole sky.
            if (Sky == Sky.Storm && Game.Playing && !Game.Frozen && Random.value < Time.deltaTime * 0.08f)
            {
                flash = 1;
            }

            flash = Mathf.MoveTowards(flash, 0, Time.unscaledDeltaTime * 3.5f);
            var inCave = place != null && place.Cave;
            cave = Mathf.MoveTowards(cave, inCave ? 1 : 0, Time.unscaledDeltaTime * 1.5f);
            var tint = inCave ? Models.Rgb(place.Colour) : Color.black;

            // By day the sun; by night the same lamp is the moon, on the other side of the sky.
            var angle = (Hour - 6) / 12 * 180;
            var height = night ? Mathf.Repeat(angle - 180, 360) : angle;
            sun.transform.rotation = Quaternion.Euler(Mathf.Clamp(Mathf.Sin(height * Mathf.Deg2Rad) * 62, 9, 62), 115 + (night ? 180 : 0) + height * 0.35f, 0);
            sun.color = Color.Lerp(key.Sun, tint * 0.8f + Color.white * 0.1f, cave * 0.7f);
            sun.intensity = Mathf.Lerp(key.Strength * (1 - 0.5f * Gloom), 0.35f, cave) + flash * 2.2f * (1 - cave);
            var ambient = Color.Lerp(key.Ambient, Color.Lerp(new Color(0.05f, 0.06f, 0.08f), tint, 0.3f), cave);
            RenderSettings.ambientLight = ambient * 1.15f;
            var horizon = Color.Lerp(key.Horizon, Color.Lerp(new Color(0.01f, 0.012f, 0.02f), tint, 0.12f), cave);
            var top = Color.Lerp(key.Top, new Color(0.004f, 0.005f, 0.01f), cave);
            // Weather greys the horizon; fog brings it close.
            horizon = Color.Lerp(horizon, new Color(0.42f, 0.46f, 0.5f) * (key.Strength * 0.6f + 0.1f), Gloom * 0.55f) + new Color(0.5f, 0.55f, 0.7f) * flash * 0.3f;
            RenderSettings.fogColor = horizon;
            // On the whole mountain the fog lies farther (the far land and woods are drawn out to it).
            var fogEnd = Ground.Big ? (Quality.Level == 0 ? 480 : Quality.Level == 1 ? 880 : 1400) : (Quality.Level == 0 ? 420 : 620);
            RenderSettings.fogStartDistance = Mathf.Lerp(Quality.Level == 0 ? 50 : 90, 14, cave);
            var weatherFog = Sky == Sky.Fog ? 0.22f : Sky == Sky.Rain || Sky == Sky.Snow ? 0.55f : Sky == Sky.Storm ? 0.45f : 1f;
            RenderSettings.fogEndDistance = Mathf.Lerp(fogEnd * Mathf.Lerp(1, weatherFog, Gloom / Mathf.Max(0.01f, gloomWanted == 0 ? 1 : gloomWanted)), 90, cave);
            var camera = Game.Rig != null ? Game.Rig.Camera : null;

            if (camera != null)
            {
                camera.backgroundColor = horizon;
                domeRoot.position = camera.transform.position;
                moon.position = camera.transform.position - sun.transform.forward * 1000;
                moon.rotation = Quaternion.LookRotation(moon.position - camera.transform.position);
            }

            for (var i = 0; i < domeVertices.Length; i++)
            {
                var up = Mathf.Clamp01(domeVertices[i].y);
                domeColours[i] = Color.Lerp(horizon, top, Mathf.Pow(up, 0.55f));
            }

            dome.colors = domeColours;
            moonMaterial.SetColor("_BaseColor", night ? new Color(0.75f, 0.86f, 1f, 1 - cave) : new Color(1f, 0.95f, 0.8f, (1 - cave) * 0.9f));
            moon.localScale = Vector3.one * (night ? 150 : 110);
        }
    }
}
