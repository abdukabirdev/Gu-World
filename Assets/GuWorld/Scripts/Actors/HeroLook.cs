using System.Collections.Generic;
using GuWorld.Core;
using GuWorld.Core.Gen.People;
using UnityEngine;
using UnityEngine.Rendering;

namespace GuWorld.Actors
{
    /// <summary>
    /// Puts the hero's look (Core.Appearances, ROADMAP 2.5) on the hero's model, fresh from the file and
    /// still at rest: the skin's tone on the face and the hands, the body's texture dyed for the skin and
    /// the hanfu (made once per look and kept), the hair's pieces hung on the head's bone. The height is
    /// the model's scale (Hero.MakeModel).
    /// </summary>
    public static class HeroLook
    {
        static readonly Dictionary<string, Texture2D> dyed = new Dictionary<string, Texture2D>();
        static readonly Dictionary<int, List<(Mesh mesh, Material material, string name)>> hair = new Dictionary<int, List<(Mesh, Material, string)>>();
        static byte[] zones;
        static int zoneWidth, zoneHeight;
        static bool zonesTried;

        /// <summary>Dresses the model in the look. `mirrored`: the importer turned the file's Z, not its X (RigAnimator.Mirrored).</summary>
        public static void Apply(Transform model, Appearance look, bool mirrored)
        {
            if (model == null || look == null)
            {
                return;
            }

            look = Appearances.Clamp(Appearances.Copy(look));
            var (kr, kg, kb) = Appearances.Tint(look.Skin);

            foreach (var renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (renderer.name.Contains("Face") || renderer.name.Contains("Hands"))
                {
                    if (look.Skin != 0)
                    {
                        var material = renderer.material;
                        var colour = Colour(material);
                        Models.SetColour(material, new Color(colour.r * kr, colour.g * kg, colour.b * kb, colour.a));
                    }
                }
                else if (renderer.name.Contains("Body") && !Appearances.OwnTexture(look))
                {
                    var material = renderer.material;
                    var texture = Dyed(Texture(material), look);

                    if (texture != null)
                    {
                        Models.SetTexture(material, texture);
                    }
                }
            }

            if (look.Hair > 0)
            {
                Hair(model, look.Hair, mirrored);
            }
        }

        static Color Colour(Material material)
        {
            foreach (var name in new[] { "_BaseColor", "baseColorFactor", "_Color" })
            {
                if (material.HasProperty(name))
                {
                    return material.GetColor(name);
                }
            }

            return Color.white;
        }

        static Texture Texture(Material material)
        {
            foreach (var name in new[] { "_BaseMap", "baseColorTexture", "_MainTex" })
            {
                if (material.HasProperty(name) && material.GetTexture(name) != null)
                {
                    return material.GetTexture(name);
                }
            }

            return null;
        }

        // ------------------------------------------------------------------ The body's texture

        /// <summary>Looks/hero_zones: where the model's hair, skin and dark accessories are (made by Tools/V2/models/hero.py zones).</summary>
        static bool LoadZones()
        {
            if (zonesTried)
            {
                return zones != null;
            }

            zonesTried = true;
            var texture = Resources.Load<Texture2D>("Looks/hero_zones");

            if (texture == null || !texture.isReadable)
            {
                Debug.LogWarning("GU World: Looks/hero_zones is missing or not readable — the hero keeps the model's own robe and skin");

                return false;
            }

            var pixels = texture.GetPixels32();
            zoneWidth = texture.width;
            zoneHeight = texture.height;
            zones = Bytes(pixels);

            return true;
        }

        static byte[] Bytes(Color32[] pixels)
        {
            var bytes = new byte[pixels.Length * 4];

            for (var i = 0; i < pixels.Length; i++)
            {
                bytes[i * 4] = pixels[i].r;
                bytes[i * 4 + 1] = pixels[i].g;
                bytes[i * 4 + 2] = pixels[i].b;
                bytes[i * 4 + 3] = pixels[i].a;
            }

            return bytes;
        }

        /// <summary>
        /// The body's texture dyed for the look, made once per look and size. The model's own texture is
        /// not readable: it is drawn into a render texture (sRGB, so the colours come back as they were) and
        /// read from there; the dye is Core's; the result is compressed where the card can.
        /// </summary>
        static Texture2D Dyed(Texture source, Appearance look)
        {
            if (source == null || !LoadZones())
            {
                return null;
            }

            // The lightest quality keeps the dyed texture at half the model's (as the townsfolk's looks are).
            var size = Mathf.Clamp(Quality.Level == 0 ? Mathf.Min(1024, source.width) : source.width, 256, 2048);
            var key = Appearances.TextureKey(look) + "@" + size;

            if (dyed.TryGetValue(key, out var made) && made != null)
            {
                return made;
            }

            var watch = System.Diagnostics.Stopwatch.StartNew();
            var target = RenderTexture.GetTemporary(size, size, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var was = RenderTexture.active;
            Graphics.Blit(source, target);
            RenderTexture.active = target;
            var copy = new Texture2D(size, size, TextureFormat.RGBA32, false, false);
            copy.ReadPixels(new Rect(0, 0, size, size), 0, 0, false);
            RenderTexture.active = was;
            RenderTexture.ReleaseTemporary(target);
            var pixels = copy.GetPixels32();
            Object.Destroy(copy);

            var bytes = Bytes(pixels);
            Appearances.DyeBody(bytes, size, size, zones, zoneWidth, zoneHeight, look);

            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color32(bytes[i * 4], bytes[i * 4 + 1], bytes[i * 4 + 2], 255);
            }

            var texture = new Texture2D(size, size, TextureFormat.RGB24, true, false)
            {
                name = "Hero " + key,
                wrapMode = source.wrapMode,
                filterMode = FilterMode.Trilinear,
                anisoLevel = 4,
            };

            texture.SetPixels32(pixels);
            texture.Apply(true, false);

            if (SystemInfo.SupportsTextureFormat(TextureFormat.DXT1))
            {
                texture.Compress(false);
            }

            texture.Apply(false, true);
            dyed[key] = texture;
            Debug.Log($"GU World: the hero's texture dyed for {key} in {watch.ElapsedMilliseconds} ms");

            return texture;
        }

        // ------------------------------------------------------------------ The hair

        /// <summary>The pieces of a hair style (made once), hung on the head's bone so they turn with it.</summary>
        static void Hair(Transform model, int style, bool mirrored)
        {
            var head = Find(model, "head");

            if (head == null)
            {
                Debug.LogWarning("GU World: the hero's model has no bone 'head' — the hair is worn loose");

                return;
            }

            if (!hair.TryGetValue(style, out var pieces))
            {
                pieces = new List<(Mesh, Material, string)>();

                foreach (var piece in HeroHair.Make(style))
                {
                    var mesh = World.Meshes.From(piece.Mesh, "Hero hair " + piece.Name);
                    pieces.Add((mesh, Models.Lit(Models.Rgb(piece.Colour), piece.Gloss), piece.Name));
                }

                hair[style] = pieces;
            }

            // Made in the model's own space at rest (the file's X mirrored, as the importer does it): put under the
            // model as it is, turned round if the importer mirrored Z instead, then handed to the head keeping its place.
            var root = new GameObject("Hair " + style);
            root.transform.SetParent(model, false);
            root.transform.localRotation = mirrored ? Quaternion.Euler(0, 180, 0) : Quaternion.identity;

            foreach (var (mesh, material, name) in pieces)
            {
                var part = new GameObject(name);
                part.transform.SetParent(root.transform, false);
                part.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = part.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.On;
            }

            root.transform.SetParent(head, true);
        }

        static Transform Find(Transform under, string name)
        {
            foreach (var child in under.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name)
                {
                    return child;
                }
            }

            return null;
        }
    }
}
