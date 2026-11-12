using System.Collections.Generic;
using GuWorld.Core.Gen;
using UnityEngine;

namespace GuWorld.Actors
{
    /// <summary>
    /// The things made in code (Core's <see cref="Props"/>) as the engine's meshes: named "code:kind:variant"
    /// wherever a model's name is asked for, built once each and shared, raised with a collider near the
    /// hero and drawn in instanced batches far (the town asks <see cref="Mesh"/> for its far parts).
    /// </summary>
    public static class CodeProp
    {
        const string Prefix = "code:";
        static readonly Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();

        public static bool Is(string model) => model != null && model.StartsWith(Prefix, System.StringComparison.Ordinal);

        public static string Name(string kind, int variant = 0) => Prefix + kind + ":" + variant;

        static void Split(string model, out string kind, out int variant)
        {
            var parts = model.Substring(Prefix.Length).Split(':');
            kind = parts[0];
            variant = parts.Length > 1 && int.TryParse(parts[1], out var v) ? v : 0;
        }

        /// <summary>The shared mesh of a code model (null if there is no such kind).</summary>
        public static Mesh Mesh(string model)
        {
            if (meshes.TryGetValue(model, out var mesh))
            {
                return mesh;
            }

            Split(model, out var kind, out var variant);
            var data = Props.Make(kind, variant);
            mesh = data != null ? World.Meshes.From(data, model) : null;
            meshes[model] = mesh;

            return mesh;
        }

        public static Material Material => Models.VertexLit(Color.white, 0);

        /// <summary>A code model standing in the world, with a collider (the mesh itself, or a box for the big flat ones).</summary>
        public static GameObject Make(string model, Transform parent, Vector3 at, float yaw, float scale = 1)
        {
            var mesh = Mesh(model);

            if (mesh == null)
            {
                Debug.LogWarning($"GU World: no code model {model}");

                return null;
            }

            var made = new GameObject(model);
            made.transform.SetParent(parent, false);
            made.transform.SetPositionAndRotation(at, Quaternion.Euler(0, yaw, 0));
            made.transform.localScale = Vector3.one * scale;
            made.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = made.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = Material;
            renderer.shadowCastingMode = Quality.Level >= 1 ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
            Split(model, out var kind, out _);

            // Strings of lanterns are walked under, not into; at night they glow.
            if (kind != "lanterns")
            {
                made.AddComponent<MeshCollider>().sharedMesh = mesh;
            }
            else
            {
                Split(model, out _, out var variant);
                var glow = new GameObject("Glow");
                glow.transform.SetParent(made.transform, false);

                foreach (var p in Props.LanternPoints(variant))
                {
                    Models.Part("Light", glow.transform, Models.Quad, Models.Glow(new Color(1f, 0.55f, 0.3f, 0.55f)), new Vector3(p.X, p.Y, p.Z), Vector3.one * 1.1f).AddComponent<Fx.Billboard>();
                }

                made.AddComponent<NightGlow>().Glow = glow;
            }

            return made;
        }
    }

    /// <summary>Shows its glow only at night (and at dusk), checked now and then.</summary>
    public class NightGlow : MonoBehaviour
    {
        public GameObject Glow;
        float wait;

        void Update()
        {
            wait -= Time.deltaTime;

            if (wait > 0 || Glow == null)
            {
                return;
            }

            wait = 2 + Random.value;
            var hour = World.Atmosphere.Hour;
            Glow.SetActive(hour < 6f || hour > 18.6f);
        }
    }
}
