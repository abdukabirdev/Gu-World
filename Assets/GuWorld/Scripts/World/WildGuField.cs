using System.Collections.Generic;
using GuWorld.Actors;
using GuWorld.Core;
using GuWorld.Core.Gen;
using GuWorld.Fx;
using UnityEngine;

namespace GuWorld.World
{
    /// <summary>
    /// The wild Gu round the hero (ROADMAP 12.7; the rules in Core/Rules/WildGu.cs): every spot within
    /// reach whose Gu is out shows as a small light of its colour circling and bobbing over its spot —
    /// a brighter core, a soft halo, a faint trail — a second or so above the grass. Beside one, T tries
    /// to catch it (Game.Interact): caught, it flies into the hero; fled, it darts off and is gone.
    /// </summary>
    public class WildGuField : MonoBehaviour
    {
        public static WildGuField Instance { get; private set; }

        const float Reach = 150;
        const float CatchReach = 2.6f;

        readonly Dictionary<string, (WildSpot spot, Transform light)> shown = new Dictionary<string, (WildSpot, Transform)>();
        readonly float[] weights = new float[Core.Gen.Surface.Layers];
        float wait;

        public static void Make(Transform parent)
        {
            var root = new GameObject("Wild Gu");
            root.transform.SetParent(parent, false);
            Instance = root.AddComponent<WildGuField>();
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        Biome BiomeAt(float x, float z)
        {
            var plan = Ground.Plan;

            if (plan == null)
            {
                return Biome.Meadow;
            }

            var height = Ground.HeightAt(x, z);
            var slope = Mathf.Abs(Ground.HeightAt(x + 2, z) - height) / 2 + Mathf.Abs(Ground.HeightAt(x, z + 2) - height) / 2;

            if (Ground.RiverDistance(x, z) < 14)
            {
                return Biome.Riverbank;
            }

            return plan.Surface.Paint(new Core.Gen.Vec2(x, z), height, slope, weights);
        }

        void Update()
        {
            var hero = Game.Hero;
            var session = Game.Session;

            if (hero == null || session == null)
            {
                return;
            }

            wait -= Time.deltaTime;

            if (wait > 0)
            {
                return;
            }

            wait = 0.5f;
            var at = hero.transform.position;
            var wanted = new HashSet<string>();

            foreach (var (cx, cz) in WildGu.CellsNear(at.x, at.z, Reach))
            {
                var key = $"{cx}:{cz}";

                if (shown.TryGetValue(key, out var have))
                {
                    if (session.WildOut(have.spot) && (new Vector2(have.spot.X - at.x, have.spot.Z - at.z)).sqrMagnitude < Reach * Reach)
                    {
                        wanted.Add(key);
                    }

                    continue;
                }

                var spot = WildGu.Spot(cx, cz, BiomeAt);

                if (spot == null || !session.WildOut(spot) || (new Vector2(spot.X - at.x, spot.Z - at.z)).sqrMagnitude > Reach * Reach)
                {
                    continue;
                }

                // Not in a settlement's streets nor under water.
                if (Ground.Plan != null && Ground.Plan.Surface.SettlementAt(new Core.Gen.Vec2(spot.X, spot.Z), 10) != null)
                {
                    continue;
                }

                shown[key] = (spot, Light(spot));
                wanted.Add(key);
            }

            foreach (var key in new List<string>(shown.Keys))
            {
                if (!wanted.Contains(key))
                {
                    if (shown[key].light != null)
                    {
                        Destroy(shown[key].light.gameObject);
                    }

                    shown.Remove(key);
                }
            }
        }

        Transform Light(WildSpot spot)
        {
            var def = GuBook.Get(spot.Gu);
            var colour = Models.Rgb(def.Colour);
            var root = new GameObject("Wild " + def.Id).transform;
            root.SetParent(transform, false);
            root.position = Game.Ground(new Vector3(spot.X, 0, spot.Z)) + Vector3.up * 1.1f;
            var mote = new GameObject("Mote").transform;
            mote.SetParent(root, false);
            Models.Part("Halo", mote, Models.Quad, Models.Glow(new Color(colour.r, colour.g, colour.b, 0.55f)), Vector3.zero, Vector3.one * 0.9f).AddComponent<Billboard>();
            Models.Part("Core", mote, Models.Quad, Models.Glow(Color.Lerp(colour, Color.white, 0.6f)), Vector3.zero, Vector3.one * 0.2f).AddComponent<Billboard>();
            Models.Part("Trail", mote, Models.Quad, Models.Glow(new Color(colour.r, colour.g, colour.b, 0.25f)), new Vector3(0, 0, -0.25f), Vector3.one * 0.45f).AddComponent<Billboard>();
            var flight = root.gameObject.AddComponent<WildFlight>();
            flight.Seed = Mathf.Abs(spot.Key.GetHashCode() % 1000) / 1000f;

            return root;
        }

        /// <summary>The wild Gu shown now: where each flies and its colour (for the minimap).</summary>
        public IEnumerable<(Vector3 at, Color colour)> Lights
        {
            get
            {
                foreach (var (spot, light) in shown.Values)
                {
                    if (light != null)
                    {
                        yield return (light.position, Models.Rgb(GuBook.Get(spot.Gu).Colour));
                    }
                }
            }
        }

        /// <summary>The spot whose Gu is close enough to catch, or null.</summary>
        public WildSpot Nearest(Vector3 at)
        {
            WildSpot best = null;
            var reach = CatchReach * CatchReach;

            foreach (var (spot, light) in shown.Values)
            {
                if (light == null)
                {
                    continue;
                }

                var d = new Vector2(light.position.x - at.x, light.position.z - at.z).sqrMagnitude;

                if (d < reach)
                {
                    reach = d;
                    best = spot;
                }
            }

            return best;
        }

        /// <summary>The prompt for a spot beside the hero: the Gu, the chance, the essence it costs.</summary>
        public static string PromptFor(WildSpot spot)
        {
            var def = GuBook.Get(spot.Gu);

            return $"Поймать · {def.Name} · {Mathf.RoundToInt(Game.Session.CatchChance(spot) * 100)}% · {WildGu.Cost(def.Rank):0} эссенции";
        }

        /// <summary>T beside a wild Gu: the attempt, and what is seen of it.</summary>
        public void TryCatch(WildSpot spot)
        {
            if (spot == null || !shown.TryGetValue(spot.Key, out var entry))
            {
                return;
            }

            var result = Game.Session.Catch(spot);
            var light = entry.light;

            if (Game.Session.WildOut(spot))
            {
                // Not even tried (no essence): only the words.
                Game.Say(result.Message);
                Sound.AudioDirector.Play("error", null, 0.5f);

                return;
            }

            shown.Remove(spot.Key);
            var colour = Models.Rgb(GuBook.Get(spot.Gu).Colour);

            if (result.Caught)
            {
                Effects.Pillar(light.position, colour, 0.8f, 3, 0.8f);
                LootMote.Spawn(light.position - Vector3.up * 0.6f, colour, null);
                Sound.AudioDirector.Play("loot", null, 0.8f);
                Game.Ui.Banner("Дикий Гу пойман", GuBook.Get(spot.Gu).Name);
                Destroy(light.gameObject);
            }
            else
            {
                light.GetComponent<WildFlight>().Flee(Game.Hero.transform.position);
                Sound.AudioDirector.Play("whoosh", null, 0.6f);
            }

            Game.Say(result.Message);
        }
    }

    /// <summary>A wild Gu's flight over its spot: a slow circle, a bob, a quiver; fleeing — off and up, then gone.</summary>
    public class WildFlight : MonoBehaviour
    {
        public float Seed;
        Transform mote;
        Vector3 away;
        float fled = -1;

        void Start() => mote = transform.GetChild(0);

        public void Flee(Vector3 from)
        {
            away = (transform.position - from).normalized;
            away.y = 0.6f;
            fled = 0;
        }

        void Update()
        {
            var t = Time.time + Seed * 50;

            if (fled >= 0)
            {
                fled += Time.deltaTime;
                transform.position += away * (14 * Time.deltaTime);
                mote.localScale = Vector3.one * Mathf.Max(0, 1 - fled);

                if (fled > 1)
                {
                    Destroy(gameObject);
                }

                return;
            }

            mote.localPosition = new Vector3(Mathf.Cos(t * 0.9f) * 0.6f, Mathf.Sin(t * 2.3f) * 0.18f + Mathf.Sin(t * 13) * 0.02f, Mathf.Sin(t * 0.9f) * 0.6f);
        }
    }
}
