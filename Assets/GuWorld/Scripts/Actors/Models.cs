using System.Collections.Generic;
using GuWorld.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace GuWorld.Actors
{
    /// <summary>
    /// Where every body and thing in the world gets its shape: the hero's and the people's
    /// models (Resources/Models) dyed by a look (Resources/Looks), the canids, and the small
    /// things made in code (a training dummy, a wild Gu, a chest, a herb).
    /// </summary>
    public static class Models
    {
        public const float CanidScale = 0.0062f;

        static readonly Dictionary<string, Material> lit = new Dictionary<string, Material>();
        static readonly Dictionary<string, Material> glow = new Dictionary<string, Material>();
        static readonly Dictionary<string, Texture2D> looks = new Dictionary<string, Texture2D>();
        static Mesh quad, disc, ring, crescent;
        static Texture2D soft;

        public static Color Rgb(int rgb, float alpha = 1) => new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, alpha);

        // ------------------------------------------------------------------ Materials

        /// <summary>An opaque lit material of one colour (URP Lit, cloned from a material the build keeps).</summary>
        public static Material Lit(Color colour, float smoothness = 0.1f)
        {
            var key = ColorUtility.ToHtmlStringRGBA(colour) + smoothness;

            if (!lit.TryGetValue(key, out var material))
            {
                var source = Resources.Load<Material>("Mats/Boulder");
                material = source != null ? new Material(source) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
                material.name = "Lit " + key;
                SetTexture(material, null);
                SetColour(material, colour);

                if (material.HasProperty("_Smoothness"))
                {
                    material.SetFloat("_Smoothness", smoothness);
                }

                lit[key] = material;
            }

            return material;
        }

        static readonly Dictionary<string, Material> vertexLit = new Dictionary<string, Material>();

        /// <summary>A lit material that takes its colour from the mesh's vertices (shapes made in code), swaying in the wind by `sway`.</summary>
        public static Material VertexLit(Color tint, float sway)
        {
            var key = ColorUtility.ToHtmlStringRGBA(tint) + sway;

            if (!vertexLit.TryGetValue(key, out var material))
            {
                var shader = Resources.Load<Shader>("Shaders/VertexLit");
                material = shader != null ? new Material(shader) : Lit(tint);
                material.name = "Vertex lit " + key;
                material.enableInstancing = true;

                if (material.HasProperty("_Sway"))
                {
                    material.SetFloat("_Sway", sway);
                    material.SetColor("_BaseColor", tint);
                }

                vertexLit[key] = material;
            }

            return material;
        }

        /// <summary>An unlit see-through material for light: additive (a glow) or blended (a veil).</summary>
        public static Material Glow(Color colour, bool additive = true, Texture texture = null)
        {
            var key = ColorUtility.ToHtmlStringRGBA(colour) + additive + Engine.Key(texture);

            if (!glow.TryGetValue(key, out var material))
            {
                var shader = Resources.Load<Shader>("Shaders/Sky");
                material = new Material(shader != null ? shader : Shader.Find("Universal Render Pipeline/Unlit")) { name = "Glow " + key };
                material.SetColor("_BaseColor", colour);
                material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)(additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
                material.SetTexture("_BaseMap", texture != null ? texture : Soft);
                material.renderQueue = 3100;
                glow[key] = material;
            }

            return material;
        }

        /// <summary>A soft round spot: bright in the middle, gone at the edge.</summary>
        public static Texture2D Soft
        {
            get
            {
                if (soft == null)
                {
                    const int size = 64;
                    soft = new Texture2D(size, size, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, name = "Soft" };
                    var pixels = new Color32[size * size];

                    for (var y = 0; y < size; y++)
                    {
                        for (var x = 0; x < size; x++)
                        {
                            var d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size, size) * 0.5f) / (size * 0.5f);
                            var a = Mathf.Clamp01(1 - d);
                            pixels[y * size + x] = new Color(1, 1, 1, a * a);
                        }
                    }

                    soft.SetPixels32(pixels);
                    soft.Apply();
                }

                return soft;
            }
        }

        public static void SetColour(Material material, Color colour)
        {
            foreach (var name in new[] { "_BaseColor", "baseColorFactor", "_Color" })
            {
                if (material.HasProperty(name))
                {
                    material.SetColor(name, colour);
                }
            }
        }

        public static void SetTexture(Material material, Texture texture)
        {
            foreach (var name in new[] { "_BaseMap", "baseColorTexture", "_MainTex" })
            {
                if (material.HasProperty(name))
                {
                    material.SetTexture(name, texture);
                }
            }
        }

        static Texture2D Look(string name)
        {
            if (!looks.TryGetValue(name, out var texture))
            {
                texture = Resources.Load<Texture2D>("Looks/" + name);
                looks[name] = texture;
            }

            return texture;
        }

        // ------------------------------------------------------------------ Meshes made once

        public static Mesh Quad => quad != null ? quad : quad = MakeQuad();

        /// <summary>A flat disc on the ground (XZ), a metre across.</summary>
        public static Mesh Disc => disc != null ? disc : disc = MakeDisc(0, 0.5f, 40, "Disc");
        /// <summary>A flat ring on the ground: its outer edge a metre across, a tenth of that wide.</summary>
        public static Mesh Ring => ring != null ? ring : ring = MakeDisc(0.42f, 0.5f, 48, "Ring");
        public static Mesh Crescent => crescent != null ? crescent : crescent = MakeCrescent();

        static Mesh MakeQuad()
        {
            var mesh = new Mesh { name = "Quad" };
            mesh.vertices = new[] { new Vector3(-0.5f, -0.5f, 0), new Vector3(0.5f, -0.5f, 0), new Vector3(0.5f, 0.5f, 0), new Vector3(-0.5f, 0.5f, 0) };
            mesh.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
            mesh.colors = new[] { Color.white, Color.white, Color.white, Color.white };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateBounds();

            return mesh;
        }

        static Mesh MakeDisc(float inner, float outer, int steps, string name)
        {
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var colours = new List<Color>();
            var triangles = new List<int>();

            for (var i = 0; i <= steps; i++)
            {
                var angle = i * Mathf.PI * 2 / steps;
                var direction = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle));
                vertices.Add(direction * inner);
                vertices.Add(direction * outer);
                // The texture's middle is its brightest: both rims sample it, so the shape is solid.
                uvs.Add(new Vector2(0.5f, 0.5f));
                uvs.Add(new Vector2(0.5f, 0.5f));
                colours.Add(new Color(1, 1, 1, inner > 0 ? 0.25f : 0.55f));
                colours.Add(Color.white);

                if (i < steps)
                {
                    var at = i * 2;
                    triangles.AddRange(new[] { at, at + 1, at + 3, at, at + 3, at + 2 });
                }
            }

            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetColors(colours);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();

            return mesh;
        }

        /// <summary>A moonblade: a crescent lying flat, its horns back, a metre from horn to horn, flying along +Z.</summary>
        static Mesh MakeCrescent()
        {
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var colours = new List<Color>();
            var triangles = new List<int>();
            const int steps = 20;

            for (var i = 0; i <= steps; i++)
            {
                var t = i / (float)steps;
                var angle = Mathf.Lerp(-80, 80, t) * Mathf.Deg2Rad;
                var thick = Mathf.Sin(t * Mathf.PI);
                var outer = new Vector3(Mathf.Sin(angle) * 0.5f, 0, Mathf.Cos(angle) * 0.5f - 0.25f);
                var inner = new Vector3(Mathf.Sin(angle) * 0.5f, 0, Mathf.Cos(angle) * 0.5f - 0.25f - 0.22f * thick);
                vertices.Add(outer);
                vertices.Add(inner);
                uvs.Add(new Vector2(0.5f, 0.5f));
                uvs.Add(new Vector2(0.5f, 0.5f));
                colours.Add(Color.white);
                colours.Add(new Color(1, 1, 1, 0.1f));

                if (i < steps)
                {
                    var at = i * 2;
                    triangles.AddRange(new[] { at, at + 3, at + 1, at, at + 2, at + 3 });
                }
            }

            var mesh = new Mesh { name = "Crescent" };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetColors(colours);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();

            return mesh;
        }

        // ------------------------------------------------------------------ Small helpers

        public static GameObject Part(string name, Transform parent, Mesh mesh, Material material, Vector3 at, Vector3 scale, Quaternion? turn = null)
        {
            var part = new GameObject(name);
            part.transform.SetParent(parent, false);
            part.transform.localPosition = at;
            part.transform.localScale = scale;
            part.transform.localRotation = turn ?? Quaternion.identity;
            part.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = part.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = material.renderQueue >= 3000 ? ShadowCastingMode.Off : ShadowCastingMode.On;
            renderer.receiveShadows = material.renderQueue < 3000;

            return part;
        }

        static readonly Dictionary<PrimitiveType, Mesh> primitives = new Dictionary<PrimitiveType, Mesh>();

        public static Mesh Primitive(PrimitiveType type)
        {
            if (!primitives.TryGetValue(type, out var mesh))
            {
                var made = GameObject.CreatePrimitive(type);
                mesh = made.GetComponent<MeshFilter>().sharedMesh;
                Object.Destroy(made);
                primitives[type] = mesh;
            }

            return mesh;
        }

        public static GameObject Shape(string name, Transform parent, PrimitiveType type, Material material, Vector3 at, Vector3 scale, Quaternion? turn = null) =>
            Part(name, parent, Primitive(type), material, at, scale, turn);

        /// <summary>A flat patch of light lying on the ground.</summary>
        public static GameObject GroundGlow(string name, Transform parent, Color colour, float size, bool asRing = false)
        {
            var part = Part(name, parent, asRing ? Ring : Disc, Glow(colour), new Vector3(0, 0.06f, 0), Vector3.one * size);

            return part;
        }

        // ------------------------------------------------------------------ Bodies

        /// <summary>A person: the hero's model (full or game detail) or the lighter one, dyed by a look. Returns the model under `parent`.</summary>
        public static GameObject Human(Transform parent, string look, bool hero, bool fullDetail, out RigAnimator animator)
        {
            var path = hero ? (fullDetail ? "Models/gu-hero" : "Models/gu-hero-game") : "Models/gu-npc";
            var prefab = Resources.Load<GameObject>(path);

            if (prefab == null && hero)
            {
                prefab = Resources.Load<GameObject>("Models/gu-hero");
            }

            GameObject model;

            if (prefab == null)
            {
                Debug.LogError($"GU World: no model at Resources/{path}");
                model = Shape("Body", parent, PrimitiveType.Capsule, Lit(new Color(0.2f, 0.22f, 0.28f)), new Vector3(0, 0.9f, 0), new Vector3(0.5f, 0.9f, 0.5f));
                animator = null;

                return model;
            }

            model = Object.Instantiate(prefab, parent, false);
            model.name = "Model";
            model.transform.localPosition = Vector3.zero;
            animator = new RigAnimator(model.transform, "human");

            if (animator.Mirrored)
            {
                model.transform.localRotation = Quaternion.Euler(0, 180, 0);
            }

            // A look of the wider mountain borrows a texture and dyes it.
            var (textureName, dye) = Wardrobe.Dress(look);
            var texture = string.IsNullOrEmpty(look) || look == "hero" ? null : Look(textureName);

            foreach (var renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                renderer.updateWhenOffscreen = false;
                // Bounds that hold every pose, so nothing is culled mid-roll.
                renderer.localBounds = new Bounds(new Vector3(0, 0.4f, 0), new Vector3(3.6f, 3.6f, 3.6f));

                if (renderer.name.Contains("Body") && texture != null)
                {
                    SetTexture(renderer.material, texture);

                    if (dye != 0xFFFFFF)
                    {
                        SetColour(renderer.material, Rgb(dye));
                    }
                }
                else if (renderer.name.Contains("Hands") && (look == "stone" || look == "blood" || look == "bai"))
                {
                    SetColour(renderer.material, look == "stone" ? new Color(0.5f, 0.5f, 0.52f) : look == "blood" ? new Color(0.62f, 0.4f, 0.38f) : new Color(0.78f, 0.62f, 0.56f));
                }
                else if (renderer.name.Contains("Face") && look == "stone")
                {
                    renderer.enabled = false;
                }
            }

            return model;
        }

        /// <summary>A fox or a wolf: the canid model at a scale, dyed.</summary>
        public static GameObject Canid(Transform parent, string look, float scale, out RigAnimator animator)
        {
            var prefab = Resources.Load<GameObject>("Models/canid");

            if (prefab == null)
            {
                Debug.LogError("GU World: no model at Resources/Models/canid");
                animator = null;

                return Shape("Body", parent, PrimitiveType.Capsule, Lit(new Color(0.5f, 0.5f, 0.5f)), new Vector3(0, 0.4f * scale, 0), new Vector3(0.4f, 0.5f, 0.4f) * scale, Quaternion.Euler(90, 0, 0));
            }

            var model = Object.Instantiate(prefab, parent, false);
            model.name = "Model";
            model.transform.localScale = Vector3.one * CanidScale * scale;
            animator = new RigAnimator(model.transform, "canid");

            if (animator.Mirrored)
            {
                model.transform.localRotation = Quaternion.Euler(0, 180, 0);
            }

            var texture = Look("canid_" + (string.IsNullOrEmpty(look) ? "fox" : look));

            foreach (var renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                renderer.updateWhenOffscreen = false;
                renderer.localBounds = new Bounds(new Vector3(0, 40, 0), new Vector3(260, 200, 260));

                if (texture != null)
                {
                    SetTexture(renderer.material, texture);
                }
            }

            return model;
        }

        /// <summary>A wild Gu: a glowing beetle that hovers.</summary>
        public static GameObject Bug(Transform parent, Color colour, float scale)
        {
            var root = new GameObject("Model");
            root.transform.SetParent(parent, false);
            var shell = Lit(Color.Lerp(colour, Color.black, 0.75f), 0.7f);
            Shape("Body", root.transform, PrimitiveType.Sphere, shell, new Vector3(0, 0, 0), new Vector3(0.34f, 0.26f, 0.5f) * scale);
            Shape("Head", root.transform, PrimitiveType.Sphere, shell, new Vector3(0, 0.02f, 0.26f) * scale, Vector3.one * 0.2f * scale);
            var wings = Glow(new Color(colour.r, colour.g, colour.b, 0.55f));
            Part("Wing L", root.transform, Quad, wings, new Vector3(-0.3f, 0.12f, -0.05f) * scale, new Vector3(0.6f, 0.3f, 1) * scale, Quaternion.Euler(70, -20, 0));
            Part("Wing R", root.transform, Quad, wings, new Vector3(0.3f, 0.12f, -0.05f) * scale, new Vector3(0.6f, 0.3f, 1) * scale, Quaternion.Euler(70, 20, 0));
            Part("Glow", root.transform, Quad, Glow(new Color(colour.r, colour.g, colour.b, 0.5f)), Vector3.zero, Vector3.one * 1.6f * scale).AddComponent<Fx.Billboard>();

            return root;
        }

        /// <summary>A training dummy: a post with arms and a straw head.</summary>
        public static GameObject Dummy(Transform parent)
        {
            var root = new GameObject("Model");
            root.transform.SetParent(parent, false);
            var wood = Lit(new Color(0.36f, 0.25f, 0.16f));
            var straw = Lit(new Color(0.72f, 0.6f, 0.34f));
            Shape("Post", root.transform, PrimitiveType.Cylinder, wood, new Vector3(0, 0.8f, 0), new Vector3(0.22f, 0.8f, 0.22f));
            Shape("Arms", root.transform, PrimitiveType.Cylinder, wood, new Vector3(0, 1.3f, 0), new Vector3(0.1f, 0.6f, 0.1f), Quaternion.Euler(0, 0, 90));
            Shape("Chest", root.transform, PrimitiveType.Cylinder, straw, new Vector3(0, 1.1f, 0), new Vector3(0.46f, 0.3f, 0.46f));
            Shape("Head", root.transform, PrimitiveType.Sphere, straw, new Vector3(0, 1.78f, 0), Vector3.one * 0.36f);

            return root;
        }

        /// <summary>One of the artist's models (Resources/Build), with its colliders and its levels of detail.</summary>
        public static GameObject Prefab(string model, Transform parent, Vector3 at, float yaw, float scale = 1)
        {
            if (CodeProp.Is(model))
            {
                return CodeProp.Make(model, parent, at, yaw, scale);
            }

            var prefab = Resources.Load<GameObject>("Build/" + model);

            if (prefab == null)
            {
                Debug.LogWarning($"GU World: no model at Resources/Build/{model}");

                return null;
            }

            var made = Object.Instantiate(prefab, at, Quaternion.Euler(0, yaw, 0), parent);
            made.name = model;
            made.transform.localScale = Vector3.one * scale;

            return made;
        }
    }
}
