using System;
using System.Collections.Generic;
using GuWorld.Core.Gen.Flora;

namespace GuWorld.Core.Gen.People
{
    public static partial class PersonGen
    {
        static readonly string[] SkirtBones = Skirts();

        static string[] Skirts()
        {
            var list = new List<string> { "hips", "spine" };

            foreach (var bone in Skeleton.Bones)
            {
                if (bone.Name.StartsWith("skirt"))
                {
                    list.Add(bone.Name);
                }
            }

            return list.ToArray();
        }

        /// <summary>The cloth's colour at a point: its weave, its pattern, the dirt toward the hem.</summary>
        static Tint Cloth(Outfit o, Tint colour, V3 p, float hemY, Noise noise, bool dirty = true)
        {
            var weave = 0.93f + 0.07f * noise.Perlin(p.X * 70 + p.Z * 50, p.Y * 70);
            var c = Build.Shade(colour, weave);

            switch (o.Pattern)
            {
                case 1:
                    if (Math.Sin((p.X * 0.4f + p.Z * 0.3f) * 150) > 0.55f)
                    {
                        c = Build.Shade(c, 0.78f);
                    }

                    break;
                case 2:
                    // Embroidery: a pair of thin bands of the trim's colour round the skirt above the hem,
                    // and a sparse scatter of small cloud medallions.
                    var above = p.Y - hemY;

                    if (above > 0.07f && above < 0.085f || above > 0.1f && above < 0.108f)
                    {
                        c = Tint.Mix(c, o.Trim, 0.8f);
                    }
                    else if (noise.Perlin(p.X * 16 + 40, p.Y * 16 + p.Z * 13) > 0.56f)
                    {
                        c = Tint.Mix(c, o.Trim, 0.4f);
                    }

                    break;
                case 3:
                    // Patches: squares of another cloth, lighter or darker.
                    var patch = noise.Perlin((float)Math.Floor(p.X * 9) * 1.7f + 3, (float)Math.Floor(p.Y * 7) * 1.3f + (float)Math.Floor(p.Z * 9));

                    if (patch > 0.38f)
                    {
                        c = Build.Shade(Tint.Mix(c, Tint.Hex(0x8a7a60), 0.5f), patch > 0.5f ? 1.2f : 0.8f);
                    }

                    break;
            }

            if (dirty && o.Dirt > 0)
            {
                var d = o.Dirt * Noise.Clamp01(1 - (p.Y - hemY) / 0.35f) * (0.6f + 0.4f * (noise.Perlin(p.X * 12, p.Z * 12 + p.Y * 3) * 0.5f + 0.5f));
                c = Tint.Mix(c, Tint.Hex(0x4a3c2c), Noise.Clamp01(d) * 0.7f);
            }

            return c;
        }

        static void Clothes(Skinned s, Frame f, Body b, Outfit o, Noise noise, ref Rng rng)
        {
            var k = f.K;
            var keys = TorsoKeys(f, b);
            var neckTop = keys[keys.Count - 1].y;
            var waist = f.J("spine").Y + 0.01f * k;
            var knee = f.J("shin_L").Y;
            var ankle = f.J("foot_L").Y + 0.04f * k;
            var robe = o.Upper == "robe" || o.Upper == "tunic";
            var hemY = robe ? Build.Lerp(ankle + 0.02f * k, knee - 0.05f * k, o.Hem) : f.J("hips").Y - 0.07f * k;
            var lowest = o.Trousers ? ankle : hemY;
            const float inner = 0.014f;

            // ---- Trousers: two legs and the seat, under whatever goes over them.
            if (o.Trousers)
            {
                var legColour = o.Legs;
                var (centres, a, c) = TorsoRings(f, b, keys[0].y - 0.01f * k, waist + 0.02f * k, inner * k, 2);
                var seat = s.Mesh.VertexCount;
                Loft(s.Mesh, centres, a, c, new V3(1, 0, 0), 16, (ring, t, col, p) => Cloth(o, col, p, lowest, noise), legColour, true, false);
                s.Skin(seat, TorsoBones);

                foreach (var side in new[] { "_L", "_R" })
                {
                    var hip = f.J("thigh" + side);
                    var kneeAt = f.J("shin" + side);
                    var ankleAt = f.J("foot" + side);
                    var thick = 1 + 0.2f * b.Muscle + 0.3f * b.Girth;
                    var hem = o.Wraps ? 0.044f : 0.066f;
                    var from = s.Mesh.VertexCount;
                    var kneeY = kneeAt.Y;
                    Limb(s.Mesh, new[] { hip + V3.Up * (0.05f * k), V3.Lerp(hip, kneeAt, 0.5f), kneeAt, V3.Lerp(kneeAt, ankleAt, 0.45f), ankleAt + V3.Up * (0.025f * k) },
                        new[] { (0.1f * b.HipK * thick + 0.012f) * k, (0.085f * thick + 0.012f) * k, (0.07f + 0.008f) * k, (0.068f * thick + 0.004f) * k, hem * k }, 0.95f, new V3(1, 0, 0), 12, 2,
                        (ring, t, col, p) =>
                        {
                            var cloth = Cloth(o, col, p, lowest, noise);

                            // Leg wraps: bands of a lighter cloth wound round the shin.
                            if (o.Wraps && p.Y < kneeY - 0.03f * k)
                            {
                                var band = Math.Sin((p.Y + t * 0.02f) * 170) > 0 ? 1.15f : 0.95f;
                                cloth = Build.Shade(Tint.Mix(cloth, Tint.Hex(0xb8ac90), 0.55f), band);
                            }

                            return cloth;
                        }, legColour, false, true);
                    s.Skin(from, "hips", "thigh" + side, "shin" + side, "foot" + side);
                }
            }

            // ---- The upper garment: the body of it, its collar crossed left over right, its sleeves.
            var outer = (o.Armour ? 0.024f : 0.02f) * k;
            var bottom = robe ? waist - 0.04f * k : hemY;
            var top = neckTop - 0.005f * k;
            {
                var (centres, a, c) = TorsoRings(f, b, bottom, top, outer, 3, 1.25f);
                var vest = o.Upper == "vest";
                var collarDepth = (top - f.J("chest").Y) + 0.07f * k;
                var garment = s.Mesh.VertexCount;
                Loft(s.Mesh, centres, a, c, new V3(1, 0, 0), 30, (ring, t, col, p) =>
                {
                    var centre = centres[ring];
                    var halfWidth = a[ring];
                    var u = (p.X - f.Cx) / Math.Max(0.01f, halfWidth);
                    var front = p.Z > centre.Z;
                    var v = (top - p.Y) / Math.Max(0.01f, collarDepth);
                    var cloth = Cloth(o, col, p, hemY, noise);

                    if (!front || v > 1.2f)
                    {
                        // The back, or below the collar: the side seam where the front overlaps (left over right).
                        return front && Math.Abs(u + 0.18f + 0.1f * v) < 0.06f && !vest ? Tint.Mix(cloth, o.Trim, 0.7f) : cloth;
                    }

                    // The V of the crossed collar: the inner robe showing, the trim along its edges.
                    var shifted = u + 0.2f * v;
                    var open = 0.4f * (1 - v / 1.0f);

                    if (Math.Abs(shifted) < open)
                    {
                        return Build.Shade(o.Inner, 0.95f + 0.05f * v);
                    }

                    if (Math.Abs(shifted) < open + 0.13f)
                    {
                        return Cloth(o, o.Trim, p, hemY, noise, false);
                    }

                    return Math.Abs(u + 0.18f + 0.1f * v) < 0.06f && v > 0.8f && !vest ? Tint.Mix(cloth, o.Trim, 0.7f) : cloth;
                }, o.Main, false, false);
                s.Skin(garment, TorsoBones);

                // A jacket's hem (or a vest's) is a band of trim.
                if (!robe)
                {
                    var hemBand = s.Mesh.VertexCount;
                    var (hc, ha, hcz) = TorsoRings(f, b, hemY - 0.005f * k, hemY + 0.035f * k, outer + 0.004f * k, 1);
                    Loft(s.Mesh, hc, ha, hcz, new V3(1, 0, 0), 18, (ring, t, col, p) => Cloth(o, col, p, hemY, noise, false), o.Trim, false, false);
                    s.Skin(hemBand, TorsoBones);
                }

                if (!vest)
                {
                    Sleeves(s, f, b, o, noise, hemY);
                }
            }

            // ---- The robe's skirt (or the tunic's), hanging from the waist on the skirt's bones.
            if (robe)
            {
                var centres = new List<V3>();
                var a = new List<float>();
                var c = new List<float>();
                var hipKey = keys[1];
                var flare = (o.Upper == "tunic" ? 0.03f : 0.06f + 0.05f * (1 - o.Hem)) * k * (b.Bust > 0 ? 1.3f : 1);
                var zHem = (f.J("foot_L").Z + f.J("hips").Z) * 0.5f + 0.03f * k;
                const int rings = 9;

                for (var i = 0; i <= rings; i++)
                {
                    var t = i / (float)rings;
                    var y = Build.Lerp(waist + 0.02f * k, hemY, t);
                    float ra, rc, z;

                    if (y > hipKey.y)
                    {
                        var w = Noise.Clamp01((waist - y) / Math.Max(0.01f, waist - hipKey.y));
                        ra = Build.Lerp(keys[2].a, hipKey.a, w);
                        rc = Build.Lerp(keys[2].c, hipKey.c, w);
                        z = Build.Lerp(keys[2].z, hipKey.z, w);
                    }
                    else
                    {
                        var w = Noise.Clamp01((hipKey.y - y) / Math.Max(0.01f, hipKey.y - hemY));
                        // Wide enough below the hips to hold both legs, then flaring to the hem.
                        ra = Build.Lerp(hipKey.a, hipKey.a + 0.03f * k + flare, (float)Math.Sqrt(w));
                        rc = Build.Lerp(hipKey.c, hipKey.c + 0.045f * k + flare * 0.8f, (float)Math.Sqrt(w));
                        z = Build.Lerp(hipKey.z, zHem, w);
                    }

                    centres.Add(new V3(f.Cx, y, z));
                    a.Add(ra + outer + 0.006f * k);
                    c.Add(rc + outer + 0.006f * k);
                }

                var skirt = s.Mesh.VertexCount;
                Loft(s.Mesh, centres, a, c, new V3(1, 0, 0), 22, (ring, t, col, p) =>
                {
                    var cloth = Cloth(o, col, p, hemY, noise);
                    var centre = centres[ring];

                    // The hem's band, and the front edge of the outer panel, crossed left over right.
                    if (p.Y < hemY + 0.045f * k)
                    {
                        return Cloth(o, o.Trim, p, hemY, noise);
                    }

                    var u = (p.X - f.Cx) / Math.Max(0.01f, a[ring]);

                    return p.Z > centre.Z && Math.Abs(u + 0.32f) < 0.06f ? Tint.Mix(cloth, o.Trim, 0.75f) : cloth;
                }, o.Main, false, false);
                s.Skin(skirt, SkirtBones);
            }

            // ---- The sash at the waist, its ends hanging at the front left.
            {
                var (centres, a, c) = TorsoRings(f, b, waist - 0.035f * k, waist + 0.03f * k, outer + 0.012f * k, 2);
                var sash = s.Mesh.VertexCount;
                var sashColour = o.Sash;
                Loft(s.Mesh, centres, a, c, new V3(1, 0, 0), 18, (ring, t, col, p) => Cloth(o, col, p, -9, noise, false), sashColour, false, false);
                var front = centres[0] + new V3(-a[0] * 0.45f, 0, c[0] * 0.92f);
                var length = rng.Range(0.18f, 0.32f) * k;
                Build.Ribbon(s.Mesh, new[] { front, front + new V3(-0.005f, -length * 0.5f, 0.012f), front + new V3(-0.01f, -length, 0.02f) }, new[] { 0.05f * k, 0.045f * k, 0.04f * k }, new V3(1, 0, 0),
                    j => Build.Shade(sashColour, 0.95f));
                s.Skin(sash, "hips", "spine", "skirtF_1", "skirtF_2", "skirt1_1_L", "skirt1_2_L");

                if (o.Pouch)
                {
                    var pouch = s.Mesh.VertexCount;
                    var at = centres[0] + new V3(a[0] * 0.75f, -0.06f * k, c[0] * 0.5f);
                    var leather = Tint.Hex(0x5a3e28);
                    Build.Blob(s.Mesh, at, new V3(0.035f * k, 0, 0), new V3(0, 0.045f * k, 0), new V3(0, 0, 0.022f * k), 1, noise, 0.1f, (p, n) => leather);
                    s.Skin(pouch, "hips", "spine");
                }
            }

            // ---- Over it: an apron, armour, a basket on the back.
            if (o.Apron)
            {
                var apron = s.Mesh.VertexCount;
                var hips = keys[1];
                var zFront = hips.z + hips.c + outer + 0.03f * k;
                var bottomY = Build.Lerp(knee, hemY, 0.3f) - 0.05f * k;
                var path = new[] { new V3(f.Cx, waist + 0.02f * k, keys[2].z + keys[2].c + outer + 0.02f * k), new V3(f.Cx, hips.y - 0.08f * k, zFront), new V3(f.Cx, bottomY, zFront + 0.02f * k) };
                var colour = o.ApronColour;
                Build.Ribbon(s.Mesh, path, new[] { 0.3f * k, 0.34f * k, 0.36f * k }, new V3(1, 0, 0), j => Build.Shade(colour, 0.95f + 0.05f * j));
                s.Skin(apron, "hips", "spine", "skirtF_1", "skirtF_2", "skirtF_3", "skirtF_4");
            }

            if (o.Armour)
            {
                var armour = s.Mesh.VertexCount;
                var colour = o.ArmourColour;
                var (centres, a, c) = TorsoRings(f, b, waist - 0.02f * k, f.J("chest").Y + 0.06f * k, outer + 0.022f * k, 2);
                Loft(s.Mesh, centres, a, c, new V3(1, 0, 0), 18, (ring, t, col, p) =>
                {
                    // Lamellae: rows of small plates, each row edged darker, laced with the clan's colour.
                    var row = (float)Math.Abs(Math.Sin(p.Y * 90));
                    var plate = (float)Math.Abs(Math.Sin((p.X + p.Z) * 120));
                    var shade = row < 0.25f ? 0.6f : plate < 0.15f ? 0.75f : 1f;

                    return Build.Shade(col, shade * (0.95f + 0.1f * noise.Perlin(p.X * 30, p.Y * 30)));
                }, colour, false, false, null, 0.25f);
                s.Skin(armour, TorsoBones);

                foreach (var side in new[] { "_L", "_R" })
                {
                    var pad = s.Mesh.VertexCount;
                    var at = f.J("upperArm" + side) + new V3(0, 0.015f * k, 0);
                    var outward = new V3(side == "_L" ? -1 : 1, 0, 0);
                    Build.Blob(s.Mesh, at + outward * (0.02f * k), outward * (0.075f * k), V3.Up * (0.03f * k), new V3(0, 0, 0.075f * k), 1, noise, 0.05f, (p, n) => Build.Shade(colour, 0.9f + 0.2f * Noise.Clamp01(n.Y)));
                    s.Skin(pad, "shoulder" + side, "upperArm" + side);
                }
            }

            if (o.Basket)
            {
                var basket = s.Mesh.VertexCount;
                var chest = f.J("chest");
                var back = new V3(f.Cx, chest.Y - 0.12f * k, chest.Z - 0.12f * k - 0.17f * k);
                var weave = Tint.Hex(0xa08850);
                Loft(s.Mesh, new[] { back + V3.Up * (-0.22f * k), back + V3.Up * (0.02f * k), back + V3.Up * (0.22f * k) }, new[] { 0.12f * k, 0.15f * k, 0.17f * k }, new[] { 0.11f * k, 0.14f * k, 0.15f * k },
                    new V3(1, 0, 0), 12, (ring, t, col, p) => Build.Shade(col, (Math.Sin(p.Y * 140) > 0 ? 1.1f : 0.85f) * ((int)(t * 24) % 2 == 0 ? 1 : 0.92f)), weave, true, false);
                s.Skin(basket, "chest", "spine2");
            }

            Hat(s, f, b, o, noise);
        }

        static void Sleeves(Skinned s, Frame f, Body b, Outfit o, Noise noise, float hemY)
        {
            var k = f.K;

            foreach (var side in new[] { "_L", "_R" })
            {
                var shoulder = f.J("upperArm" + side);
                var elbow = f.J("forearm" + side);
                var wrist = f.J("hand" + side);
                var thick = 1 + 0.25f * b.Muscle + 0.2f * b.Girth;
                var from = s.Mesh.VertexCount;
                var sleeveColour = o.Main;
                var trim = o.Trim;
                List<V3> joints;
                List<float> radii;

                if (o.ShortSleeves)
                {
                    joints = new List<V3> { shoulder + V3.Up * (0.025f * k), V3.Lerp(shoulder, elbow, 0.55f), V3.Lerp(shoulder, elbow, 0.85f) };
                    radii = new List<float> { 0.066f * k * thick, 0.06f * k * thick, 0.062f * k * thick };
                }
                else
                {
                    // A long sleeve, widening to its cuff; a wide one hangs past the wrist.
                    var cuff = Math.Max(o.Cuff * k, 0.042f * k);
                    var past = wrist + (wrist - elbow).Normalized * (Math.Max(0, cuff - 0.06f * k) * 0.8f + 0.01f * k);
                    joints = new List<V3> { shoulder + V3.Up * (0.025f * k), V3.Lerp(shoulder, elbow, 0.5f), elbow, V3.Lerp(elbow, wrist, 0.6f), past };
                    radii = new List<float> { 0.066f * k * thick, 0.058f * k * thick, Math.Max(0.05f * k * thick, cuff * 0.6f), Math.Max(0.045f * k, cuff * 0.85f), cuff };
                }

                var count = joints.Count;
                Limb(s.Mesh, joints, radii, 0.92f, new V3(1, 0, 0), 12, 3, (ring, t, col, p) =>
                {
                    // The cuff's band of trim on the last rings.
                    var rings = (count - 1) * 3;

                    return ring >= rings - 1 ? Cloth(o, trim, p, hemY, noise, false) : Cloth(o, col, p, hemY, noise, false);
                }, sleeveColour, false, false);
                s.Skin(from, "shoulder" + side, "upperArm" + side, "forearm" + side, "hand" + side, "chest");
            }
        }

        static void Hat(Skinned s, Frame f, Body b, Outfit o, Noise noise)
        {
            var hk = f.K * b.HeadK;
            var from = s.Mesh.VertexCount;
            var crown = f.HeadCentre + new V3(0, f.Ry, 0);
            var colour = o.HatColour;

            switch (o.Hat)
            {
                case "straw":
                {
                    // A wide cone of woven straw, its rings showing.
                    var brimY = f.HeadCentre.Y + f.Ry * 0.55f;
                    var c0 = new V3(f.HeadCentre.X, brimY, f.HeadCentre.Z);
                    var r = 0.25f * hk;
                    Loft(s.Mesh, new[] { c0, c0 + V3.Up * (0.05f * hk), c0 + V3.Up * (0.1f * hk), c0 + V3.Up * (0.14f * hk) }, new[] { r, r * 0.62f, r * 0.28f, 0.01f * hk }, new[] { r, r * 0.62f, r * 0.28f, 0.01f * hk },
                        new V3(1, 0, 0), 20, (ring, t, col, p) => Build.Shade(col, ((int)(new V3(p.X - c0.X, 0, p.Z - c0.Z).Length * 90) % 2 == 0 ? 1.05f : 0.88f)), colour, true, true);
                    break;
                }
                case "cap":
                {
                    // A scholar's cap: a small stiff crown of black gauze, squarish.
                    var y0 = f.HeadCentre.Y + f.Ry * 0.45f;
                    var c0 = new V3(f.HeadCentre.X, y0, f.HeadCentre.Z - 0.1f * f.Rz);
                    Loft(s.Mesh, new[] { c0, c0 + V3.Up * (0.045f * hk), c0 + V3.Up * (0.075f * hk) }, new[] { f.Rx * 1.08f, f.Rx * 0.95f, f.Rx * 0.75f }, new[] { f.Rz * 1.05f, f.Rz * 0.92f, f.Rz * 0.7f },
                        new V3(1, 0, 0), 16, (ring, t, col, p) => Build.Shade(col, 0.9f + 0.15f * noise.Perlin(p.X * 80, p.Y * 80)), colour, false, true, null, 0.6f);
                    break;
                }
                case "crown":
                {
                    // An elder's small guan over the topknot, with its gold pin.
                    var c0 = crown + new V3(0, -0.01f * hk, -0.1f * f.Rz);
                    Loft(s.Mesh, new[] { c0, c0 + V3.Up * (0.045f * hk) }, new[] { 0.032f * hk, 0.03f * hk }, new[] { 0.042f * hk, 0.04f * hk }, new V3(1, 0, 0), 12, Plain, colour, false, true);
                    var gold = Tint.Hex(0xc8a040);
                    Build.Blob(s.Mesh, c0 + V3.Up * (0.03f * hk), new V3(0.08f * hk, 0, 0), new V3(0, 0.004f * hk, 0), new V3(0, 0, 0.004f * hk), 0, noise, 0, (p, n) => gold);
                    break;
                }
                case "headband":
                {
                    var band = Tint.Mix(o.Trim, o.Main, 0.3f);
                    var y = f.HeadCentre.Y + f.Ry * 0.3f;
                    var c0 = new V3(f.HeadCentre.X, y, f.HeadCentre.Z - 0.02f * f.Rz);
                    Loft(s.Mesh, new[] { c0, c0 + V3.Up * (0.022f * hk) }, new[] { f.Rx * 1.09f, f.Rx * 1.08f }, new[] { f.Rz * 1.04f, f.Rz * 1.03f }, new V3(1, 0, 0), 18, Plain, band, false, false);
                    var knot = new V3(f.HeadCentre.X, y + 0.01f * hk, f.HeadCentre.Z - f.Rz * 1.05f);
                    Build.Ribbon(s.Mesh, new[] { knot, knot + new V3(0.01f, -0.06f * hk, -0.01f), knot + new V3(0.02f, -0.12f * hk, -0.015f) }, new[] { 0.02f * hk, 0.018f * hk, 0.015f * hk }, new V3(1, 0, 0), j => band);
                    break;
                }
                case "scarf":
                {
                    // A cloth over the hair, knotted at the nape.
                    var sphere = Build.Sphere(2);
                    var keep = new int[sphere.VertexCount];

                    for (var i = 0; i < sphere.VertexCount; i++)
                    {
                        float x = sphere.Positions[i * 3], y = sphere.Positions[i * 3 + 1], z = sphere.Positions[i * 3 + 2];

                        if (!(z > 0.3f ? y > 0.25f : y > -0.5f))
                        {
                            keep[i] = -1;
                            continue;
                        }

                        var p = f.HeadCentre + new V3(x * f.Rx * 1.12f, y * f.Ry * 1.08f + 0.006f * hk, z * f.Rz * 1.12f);
                        var c = Build.Shade(colour, 0.92f + 0.1f * noise.Perlin(p.X * 60, p.Y * 60));
                        keep[i] = s.Mesh.Vertex(p.X, p.Y, p.Z, 0.5f, 0.5f, c.R, c.G, c.B);
                    }

                    for (var t = 0; t < sphere.Triangles.Count; t += 3)
                    {
                        int a = keep[sphere.Triangles[t]], b2 = keep[sphere.Triangles[t + 1]], c2 = keep[sphere.Triangles[t + 2]];

                        if (a >= 0 && b2 >= 0 && c2 >= 0)
                        {
                            s.Mesh.Triangle(a, b2, c2);
                        }
                    }

                    Build.Blob(s.Mesh, f.HeadCentre + new V3(0, -0.35f * f.Ry, -f.Rz * 1.1f), new V3(0.03f * hk, 0, 0), new V3(0, 0.022f * hk, 0), new V3(0, 0, 0.02f * hk), 1, noise, 0.1f, (p, n) => colour);
                    break;
                }
                case "helmet":
                {
                    var metal = Tint.Mix(o.ArmourColour.R > 0 ? o.ArmourColour : Tint.Hex(0x4a4a50), Tint.Hex(0x6a6a70), 0.4f);
                    var y0 = f.HeadCentre.Y + f.Ry * 0.12f;
                    var c0 = new V3(f.HeadCentre.X, y0, f.HeadCentre.Z - 0.05f * f.Rz);
                    Loft(s.Mesh, new[] { c0, c0 + V3.Up * (f.Ry * 0.5f), c0 + V3.Up * (f.Ry * 0.85f), c0 + V3.Up * (f.Ry * 1.05f) }, new[] { f.Rx * 1.18f, f.Rx * 1.12f, f.Rx * 0.8f, 0.012f * hk }, new[] { f.Rz * 1.15f, f.Rz * 1.1f, f.Rz * 0.78f, 0.012f * hk },
                        new V3(1, 0, 0), 16, (ring, t, col, p) => Build.Shade(col, 0.85f + 0.3f * Noise.Clamp01((p.Y - y0) / f.Ry)), metal, false, true);
                    var red = Tint.Hex(0x8a2a24);
                    Build.Blob(s.Mesh, c0 + V3.Up * (f.Ry * 1.12f), new V3(0.012f * hk, 0, 0), new V3(0, 0.03f * hk, 0), new V3(0, 0, 0.012f * hk), 1, noise, 0.2f, (p, n) => red);
                    break;
                }
            }

            if (s.Mesh.VertexCount > from)
            {
                s.Skin(from, "head");
            }
        }
    }
}
