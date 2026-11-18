using System.Collections.Generic;
using GuWorld.Core;
using GuWorld.Core.Gen;
using UnityEngine;

namespace GuWorld.World
{
    /// <summary>
    /// The herb patches round the hero (ROADMAP 12.8; the rules in Core/Rules/Forage.cs): every patch
    /// within reach that has grown back stands as a herb thing (a few leaves and a glowing bloom of its
    /// colour), so the prompt and T are the herbs' own; those gathered, or left behind, go.
    /// </summary>
    public class ForageField : MonoBehaviour
    {
        public static ForageField Instance { get; private set; }

        const float Reach = 90;

        readonly Dictionary<string, Thing> shown = new Dictionary<string, Thing>();
        readonly Dictionary<(int, int), ThingDef> patches = new Dictionary<(int, int), ThingDef>();
        readonly HashSet<(int, int)> bare = new HashSet<(int, int)>();
        readonly float[] weights = new float[Core.Gen.Surface.Layers];
        float wait;

        public static void Make(Transform parent)
        {
            var root = new GameObject("Herb patches");
            root.transform.SetParent(parent, false);
            Instance = root.AddComponent<ForageField>();
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

            if (plan.Surface.SettlementAt(new Core.Gen.Vec2(x, z), 6) != null)
            {
                return Biome.Settled;
            }

            var height = Ground.HeightAt(x, z);
            var slope = Mathf.Abs(Ground.HeightAt(x + 2, z) - height) / 2 + Mathf.Abs(Ground.HeightAt(x, z + 2) - height) / 2;

            if (Ground.RiverDistance(x, z) < 12)
            {
                return Ground.RiverDistance(x, z) < 3 ? Biome.Settled : Biome.Riverbank;
            }

            return plan.Surface.Paint(new Core.Gen.Vec2(x, z), height, slope, weights);
        }

        void Update()
        {
            var hero = Game.Hero;
            var session = Game.Session;

            if (hero == null || session == null || session.InSandbox)
            {
                return;
            }

            wait -= Time.deltaTime;

            if (wait > 0)
            {
                return;
            }

            wait = 0.6f;
            var at = hero.transform.position;
            var wanted = new HashSet<string>();

            foreach (var cell in Forage.CellsNear(at.x, at.z, Reach))
            {
                if (bare.Contains(cell))
                {
                    continue;
                }

                if (!patches.TryGetValue(cell, out var patch))
                {
                    patch = Forage.Patch(cell.Item1, cell.Item2, BiomeAt);

                    if (patch == null)
                    {
                        bare.Add(cell);
                        continue;
                    }

                    patches[cell] = patch;
                }

                if (!session.ForageOut(patch.Id) || new Vector2(patch.At.X - at.x, patch.At.Z - at.z).sqrMagnitude > Reach * Reach)
                {
                    continue;
                }

                wanted.Add(patch.Id);

                if (!shown.ContainsKey(patch.Id))
                {
                    shown[patch.Id] = Thing.Make(patch, transform);
                }
            }

            foreach (var id in new List<string>(shown.Keys))
            {
                if (!wanted.Contains(id))
                {
                    if (shown[id] != null)
                    {
                        Destroy(shown[id].gameObject);
                    }

                    shown.Remove(id);
                }
            }

            // The cells' patches are kept while near; far ones are forgotten (they are made again the same).
            if (patches.Count > 600)
            {
                patches.Clear();
                bare.Clear();
            }
        }
    }
}
