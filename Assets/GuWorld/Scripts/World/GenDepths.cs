using GuWorld.Actors;
using GuWorld.Core.Gen.Buildings;
using UnityEngine;
using UnityEngine.Rendering;

namespace GuWorld.World
{
    /// <summary>
    /// The clan's underground in the engine (Core/Gen/Buildings/DepthsGen): its near level as one mesh of
    /// parts by material (the rock, the dressed stone, the wood, the glow of the spring, the orchids, the
    /// jade plates and the blood lake), a collider of its far level's rock and stone, its lights, and the
    /// Dark Hall's guards at their post by the spring.
    /// </summary>
    public static class GenDepths
    {
        public static void Build(Transform root, string clan, int seed)
        {
            var model = DepthsGen.Make(clan, seed);
            var body = new GameObject("Depths");
            body.transform.SetParent(root, false);
            body.AddComponent<MeshFilter>().sharedMesh = GenBuilding.MeshOf(model.Lods[0], "depths " + clan, out var materials);
            var renderer = body.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = materials;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            body.AddComponent<MeshCollider>().sharedMesh = GenBuilding.Plain(model.Collision, "depths solid");

            foreach (var l in model.Lights)
            {
                var light = new GameObject("Light").AddComponent<Light>();
                light.transform.SetParent(root, false);
                light.transform.localPosition = new Vector3(l.At.X, l.At.Y, l.At.Z);
                light.type = LightType.Point;
                light.range = l.Range;
                light.intensity = l.Intensity;
                light.color = new Color(l.Colour.R, l.Colour.G, l.Colour.B);
            }

            foreach (var spot in model.Spots)
            {
                if (spot.Role != "guard")
                {
                    continue;
                }

                var guard = new GameObject(spot.Name);
                guard.transform.SetParent(root, false);
                guard.transform.localPosition = new Vector3(spot.At.X, spot.At.Y + 0.02f, spot.At.Z);
                guard.transform.localRotation = Quaternion.Euler(0, spot.Facing, 0);
                Models.Human(guard.transform, "guard", false, false, out var rig, People.Seed(spot.Name + spot.At.X));
                rig?.Play("idle", 0);
                guard.AddComponent<RoomPerson>().Set(rig, spot.Name, "Тёмный зал", "Дальше — источник клана. Иди тихо и ничего не трогай.");
                var solid = guard.AddComponent<CapsuleCollider>();
                solid.radius = 0.35f;
                solid.height = 1.8f;
                solid.center = new Vector3(0, 0.9f, 0);
            }
        }
    }
}
