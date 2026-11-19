using System;

namespace GuWorld.Core
{
    /// <summary>
    /// The hero's looks (ROADMAP 2.5, from main's creator and UI-Creator.dc.html): sex, height, skin,
    /// hair and the hanfu's dye. One model wears all of them (gu-hero.glb): the height scales it, the
    /// skin tints its face and hands and the skin in the body's texture, the dye recolours the body's
    /// texture where it is the outer robe, the hair is worn up over the model's own loose hair (its
    /// pieces are Gen.People.HeroHair). Every table only grows at its end: the save keeps places in them.
    /// </summary>
    public static class Appearances
    {
        // ------------------------------------------------------------------ Sex

        public static readonly string[] Sexes = { "Юноша", "Девушка" };

        /// <summary>Whether a sex can be chosen yet: a girl needs a model of her own (ROADMAP §6).</summary>
        public static bool SexReady(int sex) => sex == 0;

        // ------------------------------------------------------------------ Height

        /// <summary>The model from its soles to its crown at rest (gu-hero.glb), and the scale the game draws it at.</summary>
        public const float ModelHeight = 1.78f;
        public const float ModelScale = 0.97f;

        /// <summary>Shorter, middling, taller among the clan's fifteen-year-olds: the model's scale against the middling one's.</summary>
        public static readonly float[] Statures = { 0.955f, 1f, 1.045f };
        public static readonly string[] StatureNames = { "Пониже", "Средний", "Повыше" };

        public static float Stature(int height) => Statures[Index(height, Statures.Length)];

        /// <summary>The hero's height in centimetres.</summary>
        public static int Centimetres(int height) => (int)Math.Round(ModelHeight * ModelScale * Stature(height) * 100);

        // ------------------------------------------------------------------ Skin

        public sealed class Tone
        {
            public string Name;
            /// <summary>The swatch the page shows (0xRRGGBB, as the eye sees it).</summary>
            public int Swatch;
        }

        /// <summary>The design's six tones; the first is the model's own (its face, its hands).</summary>
        public static readonly Tone[] Skins =
        {
            new Tone { Name = "Фарфор", Swatch = 0xF4E4D6 },
            new Tone { Name = "Светлая", Swatch = 0xEBD2BA },
            new Tone { Name = "Медовая", Swatch = 0xD9B28E },
            new Tone { Name = "Тёплая", Swatch = 0xC08E66 },
            new Tone { Name = "Смуглая", Swatch = 0x9A6A48 },
            new Tone { Name = "Тёмная", Swatch = 0x6E4A32 },
        };

        /// <summary>
        /// What a tone multiplies the model's skin by (red, green, blue, as the eye sees them): the
        /// tone's swatch against the first one's — so the face keeps its own shading and only moves
        /// as far as the swatches do from each other.
        /// </summary>
        public static (float r, float g, float b) Tint(int skin)
        {
            var own = Skins[0].Swatch;
            var tone = Skins[Index(skin, Skins.Length)].Swatch;

            return (Channel(tone, 16) / Channel(own, 16), Channel(tone, 8) / Channel(own, 8), Channel(tone, 0) / Channel(own, 0));
        }

        static float Channel(int rgb, int shift) => Math.Max(1, (rgb >> shift) & 255) / 255f;

        // ------------------------------------------------------------------ Hair

        /// <summary>
        /// How the hair is worn. The model's long loose hair is cast in its mesh (no bone of the hair
        /// carries any of it), so a style is what is put up over it: a knot on the crown with a wooden
        /// pin, a small silver guan over the knot, a moon-white ribbon round the knot (Gen.People.HeroHair).
        /// </summary>
        public static readonly string[] Hairs = { "Распущенные", "Пучок", "Гуань", "Лента" };

        // ------------------------------------------------------------------ The hanfu

        public sealed class Dye
        {
            public string Name;
            /// <summary>The swatch the page shows (0xRRGGBB): the dyed robe as it looks lit (Tools/V2/models/hero.py looks).</summary>
            public int Swatch;
            /// <summary>Keep the model's own robe (no dye).</summary>
            public bool Own;
            /// <summary>The dye's hue (0–1) and saturation.</summary>
            public float Hue, Saturation;
            /// <summary>How the robe's own light becomes the dyed cloth's: lift + own × gain (the robe is near black, its shadows deep).</summary>
            public float Lift, Gain;
        }

        public static readonly Dye[] Robes =
        {
            new Dye { Name = "Тёмное", Swatch = 0x323A44, Own = true },
            new Dye { Name = "Серое", Swatch = 0x9A9EA2, Hue = 0.58f, Saturation = 0.06f, Lift = 0.16f, Gain = 2.1f },
            new Dye { Name = "Белое", Swatch = 0xE2E0DA, Hue = 0.11f, Saturation = 0.05f, Lift = 0.42f, Gain = 2.6f },
            new Dye { Name = "Зелёное", Swatch = 0x5E9468, Hue = 0.37f, Saturation = 0.42f, Lift = 0.08f, Gain = 2.0f },
            new Dye { Name = "Синее", Swatch = 0x4A6A9A, Hue = 0.60f, Saturation = 0.58f, Lift = 0.08f, Gain = 2.1f },
            new Dye { Name = "Бурое", Swatch = 0x8A6A52, Hue = 0.07f, Saturation = 0.45f, Lift = 0.08f, Gain = 1.9f },
            new Dye { Name = "Багряное", Swatch = 0x9A4048, Hue = 0.99f, Saturation = 0.62f, Lift = 0.07f, Gain = 1.9f },
        };

        // ------------------------------------------------------------------ The whole look

        static int Index(int value, int count) => value < 0 ? 0 : value >= count ? count - 1 : value;

        /// <summary>A look within the tables (null: the model's own); a sex not ready yet is a youth.</summary>
        public static Appearance Clamp(Appearance look)
        {
            look = look ?? new Appearance();
            look.Sex = SexReady(Index(look.Sex, Sexes.Length)) ? Index(look.Sex, Sexes.Length) : 0;
            look.Height = Index(look.Height, Statures.Length);
            look.Skin = Index(look.Skin, Skins.Length);
            look.Hair = Index(look.Hair, Hairs.Length);
            look.Robe = Index(look.Robe, Robes.Length);

            return look;
        }

        public static Appearance Copy(Appearance look) => look == null ? null : new Appearance { Sex = look.Sex, Height = look.Height, Skin = look.Skin, Hair = look.Hair, Robe = look.Robe };

        /// <summary>A look by chance — the height, the skin, the hair, the hanfu; the sex kept.</summary>
        public static Appearance Random(Random random, Appearance keep = null)
        {
            return Clamp(new Appearance
            {
                Sex = keep?.Sex ?? 0,
                Height = random.Next(Statures.Length),
                Skin = random.Next(Skins.Length),
                Hair = random.Next(Hairs.Length),
                Robe = random.Next(Robes.Length),
            });
        }

        /// <summary>Whether the body's own texture serves as it is (the model's own skin and robe).</summary>
        public static bool OwnTexture(Appearance look) => look == null || (Index(look.Skin, Skins.Length) == 0 && Robes[Index(look.Robe, Robes.Length)].Own);

        /// <summary>What tells two looks' dyed textures apart (the same key: the same texture).</summary>
        public static string TextureKey(Appearance look) => $"skin{Index(look.Skin, Skins.Length)}/robe{Index(look.Robe, Robes.Length)}";

        /// <summary>The look in a line: "173 см · кожа медовая · пучок · ханьфу синее".</summary>
        public static string Describe(Appearance look)
        {
            look = Clamp(Copy(look) ?? new Appearance());

            return $"{Centimetres(look.Height)} см · кожа {Skins[look.Skin].Name.ToLowerInvariant()} · {Hairs[look.Hair].ToLowerInvariant()} · ханьфу {Robes[look.Robe].Name.ToLowerInvariant()}";
        }

        // ------------------------------------------------------------------ The body's texture

        static float Smooth(float from, float to, float x)
        {
            var t = (x - from) / (to - from);
            t = t < 0 ? 0 : t > 1 ? 1 : t;

            return t * t * (3 - 2 * t);
        }

        /// <summary>How much a texel's colour is skin: warm, red over green over blue, not dark (main's and hero.py's test, softened).</summary>
        public static float Warm(float r, float g, float b)
        {
            var high = Math.Max(r, Math.Max(g, b));
            var low = Math.Min(r, Math.Min(g, b));

            return Smooth(0, 0.04f, r - g) * Smooth(0, 0.03f, g - b) * Smooth(0.28f, 0.34f, high) * Smooth(0.06f, 0.10f, high - low) * Smooth(0.08f, 0.12f, r - b);
        }

        /// <summary>
        /// How much a texel keeps its own colour under a dye: light colourless cloth (the white under-robe
        /// at the throat, the pale cuffs) and anything of a colour of its own (the embroidered flowers on the
        /// sleeves, the trims) — the robe itself is near black and near grey.
        /// </summary>
        public static float Pale(float r, float g, float b)
        {
            var high = Math.Max(r, Math.Max(g, b));
            var low = Math.Min(r, Math.Min(g, b));
            var light = Smooth(0.30f, 0.45f, high) * (1 - Smooth(0.05f, 0.10f, high - low));
            var coloured = Smooth(0.10f, 0.16f, high - low);

            return Math.Max(light, coloured);
        }

        static (float r, float g, float b) FromHue(float hue, float saturation)
        {
            hue = (hue % 1 + 1) % 1 * 6;
            var sector = (int)hue;
            var f = hue - sector;
            var p = 1 - saturation;
            var q = 1 - saturation * f;
            var t = 1 - saturation * (1 - f);

            switch (sector)
            {
                case 0: return (1, t, p);
                case 1: return (q, 1, p);
                case 2: return (p, 1, t);
                case 3: return (p, q, 1);
                case 4: return (t, p, 1);
                default: return (1, p, q);
            }
        }

        /// <summary>
        /// Puts a look on the body's texture, in place: `pixels` are its texels (red, green, blue, alpha;
        /// `width` × `height`, as the eye sees them), `zones` the texels of Looks/hero_zones (red: where the
        /// model's hair is; green: where its skin may show — the head and the feet; blue: what keeps its
        /// own dark colour whatever the robe — the boots, the sash, the bracers; `zoneWidth` × `zoneHeight`,
        /// rows the same way round as the texture's, any size). Skin there is tinted to the tone; the dark
        /// outer robe, wherever it is none of those, takes the dye — its own light lifted and scaled, a
        /// little of its own colour kept so seams and trims still read; pale cloth keeps its colour.
        /// Returns the texels changed.
        /// </summary>
        public static int DyeBody(byte[] pixels, int width, int height, byte[] zones, int zoneWidth, int zoneHeight, Appearance look)
        {
            look = Clamp(Copy(look) ?? new Appearance());
            var robe = Robes[look.Robe];
            var (kr, kg, kb) = Tint(look.Skin);
            var tinted = look.Skin != 0;
            var dyed = !robe.Own;

            if (!tinted && !dyed || pixels == null || zones == null || zoneWidth <= 0 || zoneHeight <= 0)
            {
                return 0;
            }

            var (hr, hg, hb) = FromHue(robe.Hue, robe.Saturation);
            var keep = Math.Min(1f, robe.Gain) * 0.18f;
            var changed = 0;

            for (var y = 0; y < height; y++)
            {
                var zy = Math.Min(zoneHeight - 1, y * zoneHeight / height);

                for (var x = 0; x < width; x++)
                {
                    var at = (y * width + x) * 4;
                    var zx = Math.Min(zoneWidth - 1, x * zoneWidth / width);
                    var zone = (zy * zoneWidth + zx) * 4;
                    var hairZone = zones[zone] / 255f;
                    var skinZone = zones[zone + 1] / 255f;
                    var keepZone = zones[zone + 2] / 255f;
                    float r = pixels[at] / 255f, g = pixels[at + 1] / 255f, b = pixels[at + 2] / 255f;
                    // Skin is found whether or not it is tinted: a dye must not take it for cloth.
                    var skin = skinZone > 0 ? skinZone * Warm(r, g, b) : 0;
                    var hair = hairZone * (1 - skin);
                    var cloth = dyed ? Math.Max(0, 1 - hair - skin) * (1 - keepZone) * (1 - Pale(r, g, b)) : 0;
                    var tone = tinted ? skin : 0;

                    if (tone <= 0.001f && cloth <= 0.001f)
                    {
                        continue;
                    }

                    var own = 1 - tone - cloth;
                    float nr = r * own, ng = g * own, nb = b * own;

                    if (tone > 0)
                    {
                        nr += Math.Min(1, r * kr) * tone;
                        ng += Math.Min(1, g * kg) * tone;
                        nb += Math.Min(1, b * kb) * tone;
                    }

                    if (cloth > 0)
                    {
                        var light = Math.Min(1, robe.Lift + Math.Max(r, Math.Max(g, b)) * robe.Gain);
                        nr += Math.Min(1, hr * light * 0.82f + r * keep + robe.Lift * 0.1f) * cloth;
                        ng += Math.Min(1, hg * light * 0.82f + g * keep + robe.Lift * 0.1f) * cloth;
                        nb += Math.Min(1, hb * light * 0.82f + b * keep + robe.Lift * 0.1f) * cloth;
                    }

                    pixels[at] = Byte(nr);
                    pixels[at + 1] = Byte(ng);
                    pixels[at + 2] = Byte(nb);
                    changed++;
                }
            }

            return changed;
        }

        static byte Byte(float v) => (byte)(v <= 0 ? 0 : v >= 1 ? 255 : (int)(v * 255 + 0.5f));
    }
}
