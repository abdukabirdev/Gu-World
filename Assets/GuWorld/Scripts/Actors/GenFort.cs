using System.Collections.Generic;
using GuWorld.Core.Gen;
using GuWorld.Core.Gen.Buildings;
using GuWorld.World;
using UnityEngine;
using UnityEngine.Rendering;

namespace GuWorld.Actors
{
    /// <summary>
    /// A settlement's ring and the things round it, in the engine (Core/Gen/Buildings: FortGen,
    /// ThingGen). "fort:&lt;rampart&gt;:&lt;wall|tower|gate&gt;:&lt;seed&gt;:&lt;clan&gt;:&lt;length&gt;" is a piece of
    /// the ring: raised where it stands, against the ground under it (so each piece is its own
    /// mesh), with the building on a tower or a gate (GenBuilding) on top.
    /// "thing:&lt;kind&gt;:&lt;seed&gt;:&lt;clan&gt;" is a thing of the outskirts, shared by name. Both
    /// face -Z (the ring's pieces: outward), feet at their origin.
    /// </summary>
    public static class GenFort
    {
        public static bool Is(string model) => model != null && (model.StartsWith("fort:", System.StringComparison.Ordinal) || model.StartsWith("thing:", System.StringComparison.Ordinal));

        public static string Wall(string rampart, string what, int seed, string clan, float length = 0) => $"fort:{rampart}:{what}:{seed}:{clan}:{length.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)}";

        public static string Thing(string kind, int seed, string clan) => $"thing:{kind}:{seed}:{clan}";

        static readonly Dictionary<string, FortModel> flat = new Dictionary<string, FortModel>();
        static readonly Dictionary<string, (Mesh[] lods, Material[][] mats, Mesh solid)> things = new Dictionary<string, (Mesh[], Material[][], Mesh)>();

        static FortModel Raise(string model, System.Func<float, float, float> ground)
        {
            var bits = model.Split(':');
            var p = new FortParams { Clan = bits.Length > 4 ? bits[4] : bits.Length > 3 ? bits[3] : "", Wealth = 0.6f, Wear = 0.35f };

            if (bits[0] == "thing")
            {
                p.Clan = bits.Length > 3 ? bits[3] : "";

                return ThingGen.Make(bits[1], bits.Length > 2 && int.TryParse(bits[2], out var t) ? t : 1, p);
            }

            var style = Ramparts.Get(bits[1]);
            var seed = bits.Length > 3 && int.TryParse(bits[3], out var s) ? s : 1;
            var length = bits.Length > 5 && float.TryParse(bits[5], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var l) && l > 0 ? l : style.Piece;
            p.Wear = style.Kind == RampartKind.Earth ? 0.5f : 0.3f;

            switch (bits[2])
            {
                case "tower": return FortGen.Tower(style, seed, ground, p);
                case "gate": return FortGen.Gate(style, seed, ground, p);
                default: return FortGen.Wall(style, seed, length, ground, p);
            }
        }

        /// <summary>The model on flat ground, for drawing from afar (and for the things, which do not follow the ground).</summary>
        static (Mesh[] lods, Material[][] mats, Mesh solid) Shared(string model)
        {
            if (things.TryGetValue(model, out var got))
            {
                return got;
            }

            var made = Raise(model, (x, z) => 0);
            var lods = new Mesh[4];
            var mats = new Material[4][];

            for (var i = 0; i < 4; i++)
            {
                lods[i] = GenBuilding.MeshOf(made.Lods[i], model + " LOD" + i, out mats[i]);
            }

            got = (lods, mats, GenBuilding.Plain(made.Collision, model + " solid"));
            things[model] = got;
            flat[model] = made;

            return got;
        }

        public static Mesh Far(string model, out Material[] mats)
        {
            var shared = Shared(model);
            mats = shared.mats[2];

            return shared.lods[2];
        }

        public static GameObject Make(string model, Transform parent, Vector3 at, float yaw, float scale = 1)
        {
            var root = new GameObject(model);
            root.transform.SetParent(parent, false);
            root.transform.SetPositionAndRotation(at, Quaternion.Euler(0, yaw, 0));
            Mesh[] lods;
            Material[][] mats;
            Mesh solid;
            FortModel made;

            if (model.StartsWith("thing:", System.StringComparison.Ordinal))
            {
                (lods, mats, solid) = Shared(model);
                made = flat[model];
            }
            else
            {
                // A piece of the ring: against the ground where it stands.
                var t = root.transform;
                var foot = at.y;
                made = Raise(model, (x, z) =>
                {
                    var w = t.TransformPoint(new Vector3(x, 0, z));

                    return Ground.HeightAt(w.x, w.z) - foot;
                });
                lods = new Mesh[4];
                mats = new Material[4][];

                for (var i = 0; i < 4; i++)
                {
                    lods[i] = GenBuilding.MeshOf(made.Lods[i], model + " LOD" + i, out mats[i]);
                }

                solid = GenBuilding.Plain(made.Collision, model + " solid");
            }

            var group = new LOD[4];
            var heights = new[] { 0.25f, 0.08f, 0.02f, 0.003f };

            for (var i = 0; i < 4; i++)
            {
                var child = new GameObject("LOD" + i);
                child.transform.SetParent(root.transform, false);
                child.AddComponent<MeshFilter>().sharedMesh = lods[i];
                var renderer = child.AddComponent<MeshRenderer>();
                renderer.sharedMaterials = mats[i];
                renderer.shadowCastingMode = i == 3 ? ShadowCastingMode.Off : ShadowCastingMode.On;
                group[i] = new LOD(heights[i], new Renderer[] { renderer });
            }

            var lodGroup = root.AddComponent<LODGroup>();
            lodGroup.SetLODs(group);
            lodGroup.RecalculateBounds();

            if (solid.vertexCount > 0)
            {
                root.AddComponent<MeshCollider>().sharedMesh = solid;
            }

            if (!string.IsNullOrEmpty(made.TopModel))
            {
                // The building on a tower or over a gate, facing into the settlement (+Z of the piece) as the piece faces out.
                var bits = model.Split(':');
                var clan = bits.Length > 4 ? bits[4] : "";
                var top = $"{GenBuilding.Prefix}{made.TopModel}:{(bits.Length > 3 ? bits[3] : "1")}:{clan}:6:3:{made.TopWidth.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)}:{made.TopDepth.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)}";
                var building = GenBuilding.Make(top, root.transform, Vector3.zero, 0);
                building.transform.localPosition = new Vector3(0, made.TopY, 0);
                building.transform.localRotation = Quaternion.identity;
            }

            return root;
        }
    }
}
