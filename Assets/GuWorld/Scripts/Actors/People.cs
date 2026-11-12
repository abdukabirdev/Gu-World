using System.Collections.Generic;
using GuWorld.Core.Gen.People;
using UnityEngine;
using UnityEngine.Rendering;

namespace GuWorld.Actors
{
    /// <summary>
    /// Generated people in the engine (Core/Gen/People): a person's mesh, coloured in its
    /// vertices, skinned to a copy of the hero's skeleton laid out in the person's proportions —
    /// so the hero's animations (human.bytes, played by RigAnimator) move every one of them. The
    /// bones keep the file's rest turns; only their places differ, which the animations do not
    /// touch (the hips' movement is scaled by the player to the body's size).
    /// </summary>
    public static class People
    {
        /// <summary>Whether the game's people are generated (on by default; off falls back to the one hand-made model, dyed).</summary>
        public static bool Enabled = true;

        static Material material;
        static int hipsIndex = -1;

        /// <summary>The material people are drawn with: vertex colours, lit, both faces (the insides of sleeves and hems show).</summary>
        static Material Material
        {
            get
            {
                if (material == null)
                {
                    var shader = Resources.Load<Shader>("Shaders/VertexLit");
                    material = shader != null ? new Material(shader) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    material.name = "People";

                    if (material.HasProperty("_Cull"))
                    {
                        material.SetFloat("_Cull", (float)CullMode.Off);
                    }

                    if (material.HasProperty("_Sway"))
                    {
                        material.SetFloat("_Sway", 0);
                    }
                }

                return material;
            }
        }

        /// <summary>Builds a person under `parent` (facing +Z, feet at its origin); the animator plays the human clips on them.</summary>
        public static GameObject Make(Transform parent, PersonSpec spec, out RigAnimator animator)
        {
            var person = PersonGen.Make(spec);
            var bones = Skeleton.Bones;
            var root = new GameObject("Model");
            root.transform.SetParent(parent, false);
            var transforms = new Transform[bones.Length];

            // The skeleton: each bone under its parent with the file's rest turn (glTF's axes mirrored in X, as the importer does)...
            for (var i = 0; i < bones.Length; i++)
            {
                var bone = bones[i];
                var go = new GameObject(bone.Name);
                go.transform.SetParent(bone.Parent >= 0 ? transforms[bone.Parent] : root.transform, false);
                go.transform.localRotation = new Quaternion(bone.Qx, -bone.Qy, -bone.Qz, bone.Qw);
                transforms[i] = go.transform;
            }

            // ...then each put where this body has it (parents first, so children follow).
            for (var i = 0; i < bones.Length; i++)
            {
                var at = person.BoneAt[i];
                transforms[i].position = root.transform.TransformPoint(new Vector3(at.X, at.Y, at.Z));
            }

            var mesh = MeshOf(person);
            var bindposes = new Matrix4x4[bones.Length];

            for (var i = 0; i < bones.Length; i++)
            {
                bindposes[i] = transforms[i].worldToLocalMatrix * root.transform.localToWorldMatrix;
            }

            mesh.bindposes = bindposes;
            var body = new GameObject("Body");
            body.transform.SetParent(root.transform, false);
            var skinned = body.AddComponent<SkinnedMeshRenderer>();
            skinned.sharedMesh = mesh;
            skinned.bones = transforms;

            if (hipsIndex < 0)
            {
                hipsIndex = Mathf.Max(0, PersonGen.Bone("hips"));
            }

            skinned.rootBone = transforms[hipsIndex];
            skinned.sharedMaterial = Material;
            skinned.updateWhenOffscreen = false;
            skinned.quality = SkinQuality.Bone4;
            skinned.shadowCastingMode = ShadowCastingMode.On;
            // Bounds about the hips that hold every pose (a roll, a fall), so nothing is culled mid-move.
            skinned.localBounds = new Bounds(Vector3.zero, Vector3.one * 3.6f);
            animator = new RigAnimator(root.transform, "human");

            return root;
        }

        static Mesh MeshOf(PersonModel person)
        {
            var data = person.Mesh;
            var count = data.VertexCount;
            var vertices = new Vector3[count];
            var normals = new Vector3[count];
            var colours = new Color[count];
            var weights = new BoneWeight[count];

            for (var i = 0; i < count; i++)
            {
                vertices[i] = new Vector3(data.Positions[i * 3], data.Positions[i * 3 + 1], data.Positions[i * 3 + 2]);
                normals[i] = new Vector3(data.Normals[i * 3], data.Normals[i * 3 + 1], data.Normals[i * 3 + 2]);
                colours[i] = new Color(data.Colours[i * 4], data.Colours[i * 4 + 1], data.Colours[i * 4 + 2], 1);
                weights[i] = new BoneWeight
                {
                    boneIndex0 = person.Joints[i * 4], weight0 = person.Weights[i * 4],
                    boneIndex1 = person.Joints[i * 4 + 1], weight1 = person.Weights[i * 4 + 1],
                    boneIndex2 = person.Joints[i * 4 + 2], weight2 = person.Weights[i * 4 + 2],
                    boneIndex3 = person.Joints[i * 4 + 3], weight3 = person.Weights[i * 4 + 3],
                };
            }

            var mesh = new Mesh { name = "Person " + person.Spec.Seed };

            if (count > 65000)
            {
                mesh.indexFormat = IndexFormat.UInt32;
            }

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.colors = colours;
            mesh.boneWeights = weights;
            mesh.triangles = data.Triangles.ToArray();
            mesh.RecalculateBounds();

            return mesh;
        }

        /// <summary>A stable number from a name (for people whose seed is their name).</summary>
        public static int Seed(string text)
        {
            unchecked
            {
                var h = 23;

                foreach (var c in text ?? "")
                {
                    h = h * 31 + c;
                }

                return h & 0x7FFFFFFF;
            }
        }

        static int next = 1;

        /// <summary>A new seed each time (for the nameless: bandits, fighters).</summary>
        public static int Fresh() => 7919 * next++;

        /// <summary>The looks that stay on the hand-made model: the hero, and the stone statue.</summary>
        public static bool HandMade(string look) => string.IsNullOrEmpty(look) || look == "hero" || look == "stone";

        /// <summary>The clan a look belongs to, for its colours.</summary>
        public static string ClanOf(string look) => look == "bai" ? "bai" : look == "xiong" ? "xiong" : look == "bandit" || look == "blood" ? "none" : "gu_yue";
    }
}
