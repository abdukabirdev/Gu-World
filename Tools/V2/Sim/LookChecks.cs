using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using GuWorld.Core;
using GuWorld.Core.Gen.People;

/// <summary>
/// The hero's looks (ROADMAP 2.5): the tables, a look by chance, the new game and the save keeping it,
/// the dye on the body's texture, the hair's pieces. `looks` (a mode, like `gen`) dyes a texture given
/// as raw bytes and writes the hair styles as .glb — what Tools/V2/models/hero.py previews.
/// </summary>
static partial class Program
{
    static void LookRules()
    {
        // The tables.
        Check(Appearances.Statures.Length == Appearances.StatureNames.Length && Appearances.Statures.Length == 3, "looks: three heights, each named");
        Check(Appearances.Centimetres(0) < Appearances.Centimetres(1) && Appearances.Centimetres(1) < Appearances.Centimetres(2), "looks: the heights grow");
        Check(Appearances.Centimetres(1) == 173 && Appearances.Centimetres(0) >= 160 && Appearances.Centimetres(2) <= 185, $"looks: a middling hero is the model as the game draws it, 173 cm ({Appearances.Centimetres(0)}–{Appearances.Centimetres(2)})");
        var own = Appearances.Tint(0);
        Check(Math.Abs(own.r - 1) < 1e-4f && Math.Abs(own.g - 1) < 1e-4f && Math.Abs(own.b - 1) < 1e-4f, "looks: the first skin is the model's own (no tint)");

        for (var i = 1; i < Appearances.Skins.Length; i++)
        {
            var (r, g, b) = Appearances.Tint(i);
            var (r0, g0, b0) = Appearances.Tint(i - 1);
            Check(r + g + b < r0 + g0 + b0 && r > 0 && g > 0 && b > 0, $"looks: skin {Appearances.Skins[i].Name} is darker than the one before it");
        }

        Check(Appearances.Robes[0].Own && Appearances.Robes.Skip(1).All(d => !d.Own && d.Gain > 0 && d.Hue >= 0 && d.Hue < 1), "looks: the first hanfu is the model's own, the rest are dyes");
        Check(Appearances.Robes.Select(d => d.Name).Distinct().Count() == Appearances.Robes.Length && Appearances.Skins.Select(t => t.Name).Distinct().Count() == Appearances.Skins.Length, "looks: names are unique");
        Check(Appearances.Hairs.Length == 4 && Appearances.SexReady(0) && !Appearances.SexReady(1), "looks: four ways to wear the hair; a girl waits for her model");

        // A look out of the tables is brought back into them; a girl is a youth until her model comes.
        var wild = Appearances.Clamp(new Appearance { Sex = 1, Height = 9, Skin = -3, Hair = 99, Robe = 70 });
        Check(wild.Sex == 0 && wild.Height == 2 && wild.Skin == 0 && wild.Hair == Appearances.Hairs.Length - 1 && wild.Robe == Appearances.Robes.Length - 1, "looks: a look out of the tables is brought into them");
        Check(Appearances.Clamp(null) != null && Appearances.OwnTexture(Appearances.Clamp(null)) && Appearances.Clamp(null).Height == 1, "looks: no look is the model's own, middling");

        // By chance: every choice of every table comes up; the sex is kept.
        var random = new Random(31);
        var seen = new HashSet<string>();

        for (var i = 0; i < 800; i++)
        {
            var look = Appearances.Random(random, new Appearance());
            seen.Add("h" + look.Height);
            seen.Add("s" + look.Skin);
            seen.Add("r" + look.Robe);
            seen.Add("c" + look.Hair);
            Check(look.Sex == 0, "looks: chance keeps the sex");
        }

        Check(seen.Count == Appearances.Statures.Length + Appearances.Skins.Length + Appearances.Robes.Length + Appearances.Hairs.Length, $"looks: chance reaches every choice ({seen.Count})");

        // The new game keeps its own copy of the look; the save keeps it; an old save wears the model's own.
        var chosen = new Appearance { Height = 2, Skin = 3, Hair = 2, Robe = 4 };
        var state = Session.NewGame("Мин", Difficulty.Normal, null, 7, chosen);
        chosen.Robe = 1;
        Check(state.Appearance.Height == 2 && state.Appearance.Skin == 3 && state.Appearance.Hair == 2 && state.Appearance.Robe == 4, "looks: a new game keeps the look chosen (its own copy)");
        Check(Session.NewGame("Мин").Appearance.Height == 1 && Appearances.OwnTexture(Session.NewGame("Мин").Appearance), "looks: a new game without a look has the model's own");
        var options = new JsonSerializerOptions { IncludeFields = true };
        var json = JsonSerializer.Serialize(state, options);
        var back = JsonSerializer.Deserialize<SaveData>(json, options);
        Check(back.Appearance.Robe == 4 && back.Appearance.Hair == 2, "looks: the look survives the save");
        var old = JsonSerializer.Deserialize<SaveData>(json.Replace("\"Appearance\":", "\"Appearance_\":"), options);
        Check(new Session(old, 3).State.Appearance.Height == 1 && Appearances.OwnTexture(old.Appearance), "looks: a save from before the looks wears the model's own");
        var broken = Session.NewGame("Мин");
        broken.Appearance = null;
        Check(new Session(broken, 3).State.Appearance != null, "looks: a save with no look gets one");
        Check(Appearances.Describe(state.Appearance).StartsWith("180 см") && Appearances.Describe(state.Appearance).Contains("гуань") && Appearances.Describe(state.Appearance).Contains("синее"), "looks: the look in a line: " + Appearances.Describe(state.Appearance));
        Check(Appearances.TextureKey(new Appearance { Skin = 2, Robe = 3, Hair = 1 }) == Appearances.TextureKey(new Appearance { Skin = 2, Robe = 3, Hair = 0, Height = 0 }), "looks: the dyed texture depends on the skin and the robe only");

        // The dye, on a texture of eight texels: hair (in the hair's zone), the dark robe, pale cloth, skin (in the skin's
        // zone), skin-coloured embroidery (outside it), the robe again, its deepest shadow, the sash (kept dark). Zones:
        // red the hair's, green the skin's, blue what keeps its own colour.
        byte[] Texture() => new byte[]
        {
            20, 20, 22, 255,    40, 44, 52, 255,    200, 200, 196, 255,   180, 140, 120, 255,
            178, 138, 118, 255, 30, 34, 40, 255,    12, 13, 15, 255,      60, 64, 70, 255,
        };

        var zones = new byte[]
        {
            255, 0, 0, 255,   0, 0, 0, 255,   0, 0, 0, 255,   0, 255, 0, 255,
            0, 0, 0, 255,     0, 0, 0, 255,   0, 0, 0, 255,   0, 0, 255, 255,
        };

        byte[] Dyed(Appearance look)
        {
            var pixels = Texture();
            Appearances.DyeBody(pixels, 4, 2, zones, 4, 2, look);

            return pixels;
        }

        bool Same(byte[] a, int texel) => a.Skip(texel * 4).Take(4).SequenceEqual(Texture().Skip(texel * 4).Take(4));

        var plain = Texture();
        Check(Appearances.DyeBody(plain, 4, 2, zones, 4, 2, new Appearance()) == 0 && plain.SequenceEqual(Texture()), "looks: the model's own look leaves the texture as it is");
        var blue = Dyed(new Appearance { Robe = 4 });
        Check(Same(blue, 0), "looks: the hair takes no dye");
        Check(Same(blue, 2), "looks: pale cloth takes no dye");
        Check(Same(blue, 3), "looks: a dye alone leaves the skin as it is");
        Check(Same(blue, 4), "looks: embroidery of a colour of its own keeps it under a dye");
        Check(blue[6] > blue[4] + 30 && blue[6] > 52, $"looks: the dark robe turns blue and lighter ({blue[4]},{blue[5]},{blue[6]})");
        Check(blue[26] > blue[24] && blue[26] > 15, $"looks: the robe's deepest shadow is dyed too ({blue[24]},{blue[25]},{blue[26]})");
        Check(Same(blue, 7), "looks: the sash, the boots and the bracers keep their own colour");
        var white = Dyed(new Appearance { Robe = 2 });
        Check(white[4] > 120 && Math.Abs(white[4] - white[6]) < 30, $"looks: a white robe is light and colourless ({white[4]},{white[5]},{white[6]})");
        var dark = Dyed(new Appearance { Skin = 5 });
        Check(dark[12] < 120 && dark[14] < 60, $"looks: dark skin darkens the skin in its zone ({dark[12]},{dark[13]},{dark[14]})");
        Check(Same(dark, 4), "looks: skin-coloured embroidery away from the head and the feet keeps its colour");
        Check(Same(dark, 1) && Same(dark, 0), "looks: the skin alone does not dye the robe or the hair");
        var coarse = Texture();
        var changed = Appearances.DyeBody(coarse, 4, 2, new byte[] { 0, 0, 0, 255, 0, 0, 0, 255 }, 2, 1, new Appearance { Robe = 3 });
        Check(changed >= 5 && coarse[8] == 200 && coarse[28] != 60, $"looks: zones of another size than the texture's serve ({changed} texels dyed)");

        // The hair's pieces: none for the loose hair; the rest on the crown, round the head's middle, whole meshes.
        Check(HeroHair.Make(0).Count == 0, "looks: loose hair needs no pieces");

        for (var style = 1; style < Appearances.Hairs.Length; style++)
        {
            var pieces = HeroHair.Make(style);
            Check(pieces.Count >= 2 && pieces.All(p => p.Mesh.TriangleCount > 20), $"looks: hair style {Appearances.Hairs[style]} has its pieces");

            foreach (var piece in pieces)
            {
                var mesh = piece.Mesh;
                var inBox = true;
                var unit = true;

                for (var v = 0; v < mesh.VertexCount; v++)
                {
                    float x = mesh.Positions[v * 3], y = mesh.Positions[v * 3 + 1], z = mesh.Positions[v * 3 + 2];
                    inBox &= Math.Abs(x - HeroHair.MidX) < 0.1f && y > 1.5f && y < 1.86f && z > -0.2f && z < 0.08f;
                    var n = Math.Sqrt(mesh.Normals[v * 3] * mesh.Normals[v * 3] + mesh.Normals[v * 3 + 1] * mesh.Normals[v * 3 + 1] + mesh.Normals[v * 3 + 2] * mesh.Normals[v * 3 + 2]);
                    unit &= Math.Abs(n - 1) < 0.01;
                }

                Check(inBox, $"looks: {Appearances.Hairs[style]} · {piece.Name} stays on the head");
                Check(unit, $"looks: {Appearances.Hairs[style]} · {piece.Name} has unit normals");
                Check(mesh.Triangles.All(t => t >= 0 && t < mesh.VertexCount) && mesh.Triangles.Count % 3 == 0, $"looks: {Appearances.Hairs[style]} · {piece.Name} has whole triangles");
            }

            var knot = pieces.First(p => p.Name == "Knot").Mesh;
            var lowest = Enumerable.Range(0, knot.VertexCount).Min(v => knot.Positions[v * 3 + 1]);
            var highest = Enumerable.Range(0, knot.VertexCount).Max(v => knot.Positions[v * 3 + 1]);
            Check(lowest < 1.765f && highest > 1.8f, $"looks: the knot sits in the crown's hair and rises over it ({lowest:0.000}–{highest:0.000} m)");
        }
    }

    /// <summary>
    /// `looks body.rgba W H zones.rgba ZW ZH out/`: the body's texture dyed for every skin with the own robe and
    /// for every robe with the own skin (raw RGBA files beside each other), and the hair styles as .glb.
    /// </summary>
    static int Looks(string[] args)
    {
        var at = Array.IndexOf(args, "looks");

        if (args.Length < at + 8)
        {
            Console.WriteLine("usage: looks body.rgba width height zones.rgba width height outdir");

            return 1;
        }

        var body = File.ReadAllBytes(args[at + 1]);
        int width = int.Parse(args[at + 2]), height = int.Parse(args[at + 3]);
        var zones = File.ReadAllBytes(args[at + 4]);
        int zoneWidth = int.Parse(args[at + 5]), zoneHeight = int.Parse(args[at + 6]);
        var outDir = args[at + 7];
        Directory.CreateDirectory(outDir);
        var looks = new List<Appearance>();

        for (var skin = 1; skin < Appearances.Skins.Length; skin++)
        {
            looks.Add(new Appearance { Skin = skin });
        }

        for (var robe = 1; robe < Appearances.Robes.Length; robe++)
        {
            looks.Add(new Appearance { Robe = robe });
        }

        looks.Add(new Appearance { Skin = 2, Robe = 2 });
        looks.Add(new Appearance { Skin = 4, Robe = 6 });

        foreach (var look in looks)
        {
            var pixels = (byte[])body.Clone();
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var changed = Appearances.DyeBody(pixels, width, height, zones, zoneWidth, zoneHeight, look);
            var name = $"skin{look.Skin}_robe{look.Robe}";
            File.WriteAllBytes(Path.Combine(outDir, name + ".rgba"), pixels);
            Console.WriteLine($"{name}: {changed} texels in {watch.ElapsedMilliseconds} ms");
        }

        for (var style = 1; style < Appearances.Hairs.Length; style++)
        {
            var parts = new List<Glb.Part>();

            foreach (var piece in HeroHair.Make(style))
            {
                var tints = new float[piece.Mesh.VertexCount * 3];

                for (var v = 0; v < piece.Mesh.VertexCount; v++)
                {
                    tints[v * 3] = ((piece.Colour >> 16) & 255) / 255f;
                    tints[v * 3 + 1] = ((piece.Colour >> 8) & 255) / 255f;
                    tints[v * 3 + 2] = (piece.Colour & 255) / 255f;
                }

                parts.Add(new Glb.Part { Name = piece.Name, Mesh = piece.Mesh, Tints = tints, Roughness = 1 - piece.Gloss * 0.6f });
            }

            Glb.Write(Path.Combine(outDir, $"hair{style}.glb"), parts);
        }

        return 0;
    }
}
