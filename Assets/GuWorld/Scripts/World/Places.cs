using System.Collections.Generic;
using GuWorld.Actors;
using GuWorld.Core;
using GuWorld.Core.Gen;
using UnityEngine;
using UnityEngine.Rendering;

namespace GuWorld.World
{
    /// <summary>
    /// The generated world's built things: the settlements' buildings, walls and gates (raised when
    /// the hero comes within sight of them, taken down when far), the rivers' water, their falls
    /// with spray, the pools, and the bridges where the roads cross the water.
    /// </summary>
    public class Places : MonoBehaviour
    {
        WorldPlan plan;
        float sight;
        float wait;
        readonly Dictionary<string, GameObject> raised = new Dictionary<string, GameObject>();
        readonly List<(Vector3 at, GameObject made)> bridges = new List<(Vector3, GameObject)>();
        Material wood;

        public static Places Make(Transform parent, WorldPlan plan)
        {
            var root = new GameObject("Places");
            root.transform.SetParent(parent, false);
            var places = root.AddComponent<Places>();
            places.plan = plan;
            places.sight = Quality.Level >= 2 ? 1100 : Quality.Level == 1 ? 750 : 480;
            places.wood = Models.VertexLit(Color.white, 0);
            places.Water();
            places.Bridges();

            // The towns keep their own houses, by cells.
            foreach (var settlement in plan.Settlements)
            {
                if (settlement.Def.City)
                {
                    Town.Make(root.transform, settlement);
                    Townsfolk.Make(root.transform, settlement);
                }
            }

            return places;
        }

        void Update()
        {
            wait -= Time.unscaledDeltaTime;

            if (wait > 0)
            {
                return;
            }

            wait = 0.5f;
            var focus = Game.Hero != null ? Game.Hero.transform.position : Game.Rig != null ? Game.Rig.transform.position : Vector3.zero;

            foreach (var settlement in plan.Settlements)
            {
                if (settlement.Def.City)
                {
                    continue;
                }

                var distance = Vector2.Distance(new Vector2(focus.x, focus.z), new Vector2(settlement.Centre.X, settlement.Centre.Z));
                var up = raised.TryGetValue(settlement.Def.Id, out var made);

                if (!up && distance < sight)
                {
                    raised[settlement.Def.Id] = Raise(settlement);
                }
                else if (up && distance > sight + 250)
                {
                    Destroy(made);
                    raised.Remove(settlement.Def.Id);
                }
            }

            // Bridges' colliders only near (their drawing is cheap).
            foreach (var (at, bridge) in bridges)
            {
                var collider = bridge.GetComponent<MeshCollider>();

                if (collider != null)
                {
                    collider.enabled = (at - focus).sqrMagnitude < 300 * 300;
                }
            }
        }

        // ------------------------------------------------------------------ Settlements

        GameObject Raise(SettlementPlan settlement)
        {
            var root = new GameObject(settlement.Def.Name);
            root.transform.SetParent(transform, false);

            foreach (var building in settlement.Buildings)
            {
                var y = Ground.HeightAt(building.At.X, building.At.Z);

                // A building stands on the lowest corner of its footprint, so no corner floats.
                foreach (var corner in building.Footprint.Corners())
                {
                    y = Mathf.Min(y, Ground.HeightAt(corner.X, corner.Z));
                }

                Models.Prefab(building.Model, root.transform, new Vector3(building.At.X, y - 0.05f, building.At.Z), building.Yaw, building.Scale);
            }

            return root;
        }

        // ------------------------------------------------------------------ Water

        void Water()
        {
            // The water's own shader (depth, waves with the current, the sky, foam); the old material if it is missing.
            var shader = Shader.Find("GuWorld/Water");
            var water = shader != null ? new Material(shader) { name = "Water" } : Resources.Load<Material>("Mats/Water");
            var falling = Resources.Load<Material>("Mats/Waterfall");
            water = water != null ? water : Models.Glow(new Color(0.2f, 0.4f, 0.5f, 0.7f), false, Texture2D.whiteTexture);
            falling = falling != null ? falling : water;
            var root = new GameObject("Water").transform;
            root.SetParent(transform, false);

            foreach (var river in plan.Waters.Rivers)
            {
                // In pieces of about 300 m, so the camera's culling has something to cull.
                const int piece = 50;

                for (var from = 0; from < river.Points.Count - 1; from += piece)
                {
                    var data = Shapes.River(river.Points, river.Levels, river.Widths, from, Mathf.Min(from + piece + 1, river.Points.Count));
                    var part = Models.Part(river.Def.Name, root, Meshes.From(data, river.Def.Id + " " + from), water, Vector3.zero, Vector3.one);
                    var renderer = part.GetComponent<MeshRenderer>();
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                }
            }

            foreach (var fall in plan.Waters.Falls)
            {
                var data = Shapes.Fall(fall.Lip, fall.Foot, fall.Top, fall.Bottom, fall.Width);
                var sheet = Models.Part("Fall", root, Meshes.From(data, "Fall " + fall.River), falling, Vector3.zero, Vector3.one);
                sheet.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                Spray(root, fall);
            }

            foreach (var pool in plan.Waters.Pools)
            {
                var part = Models.Part("Pool", root, Meshes.From(Shapes.Pool(pool.Radius), "Pool"), water, new Vector3(pool.At.X, pool.Level, pool.At.Z), Vector3.one);
                part.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
        }

        /// <summary>Spray at a waterfall's foot: soft white puffs rising and drifting, a mist that stays.</summary>
        static void Spray(Transform parent, Waters.Fall fall)
        {
            var foot = new Vector3(fall.Foot.X, fall.Bottom + 0.3f, fall.Foot.Z);
            var spray = new GameObject("Spray");
            spray.transform.SetParent(parent, false);
            spray.transform.position = foot;
            var particles = spray.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.4f, 2.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 4f);
            main.startSize = new ParticleSystem.MinMaxCurve(1.6f, 3.6f);
            main.startColor = new Color(0.92f, 0.96f, 1f, 0.32f);
            main.gravityModifier = -0.05f;
            main.maxParticles = Quality.Level >= 1 ? 160 : 60;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = particles.emission;
            emission.rateOverTime = Quality.Level >= 1 ? 40 : 16;
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(fall.Width, 0.5f, 2);
            var forward = fall.Foot - fall.Lip;
            spray.transform.rotation = Quaternion.LookRotation(new Vector3(forward.X, 0, forward.Z).normalized + Vector3.up * 0.6f);
            var colour = particles.colorOverLifetime;
            colour.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(0.35f, 0.2f), new GradientAlphaKey(0, 1) });
            colour.color = gradient;
            var size = particles.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 0.6f, 1, 1.8f));
            var renderer = spray.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = Models.Glow(Color.white, false);
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            var mist = Models.Part("Mist", parent, Models.Quad, Models.Glow(new Color(0.85f, 0.92f, 1f, 0.16f), false), foot + Vector3.up * 3, new Vector3(fall.Width * 3, 8, 1));
            mist.AddComponent<Fx.Billboard>();
        }

        // ------------------------------------------------------------------ Bridges

        void Bridges()
        {
            var root = new GameObject("Bridges").transform;
            root.SetParent(transform, false);
            var seed = 0;

            foreach (var bridge in plan.Roads.Bridges)
            {
                var data = Shapes.Bridge(bridge.Length, bridge.Width, 4, seed++);
                var mesh = Meshes.From(data, "Bridge");
                var at = new Vector3(bridge.At.X, bridge.Deck, bridge.At.Z);
                var made = Models.Part("Bridge", root, mesh, wood, at, Vector3.one, Quaternion.Euler(0, bridge.Along.Yaw, 0));
                made.AddComponent<MeshCollider>().sharedMesh = mesh;
                bridges.Add((at, made));
            }
        }
    }
}
