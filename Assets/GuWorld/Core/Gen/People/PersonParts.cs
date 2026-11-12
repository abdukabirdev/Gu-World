using System;
using System.Collections.Generic;
using System.Text;
using GuWorld.Core.Gen.Flora;

namespace GuWorld.Core.Gen.People
{
    public static partial class PersonGen
    {
        /// <summary>Makes a person from a spec.</summary>
        public static PersonModel Make(PersonSpec spec)
        {
            var rng = new Rng(spec.Seed * 7 + 3 + (int)spec.Sex * 101 + (int)spec.Age * 1009);
            var words = new StringBuilder();
            var body = ChooseBody(spec, ref rng, words);
            var outfit = Outfits.Choose(spec, ref rng, words);
            var at = Proportion(body);
            var s = new Skinned { At = at };
            var noise = new Noise(spec.Seed * 13 + 7);
            var f = new Frame(at, body);

            Torso(s, f, body, outfit);
            Neck(s, f, body);
            Head(s, f, body, outfit, noise);
            Hair(s, f, body, outfit, noise, ref rng);
            Arms(s, f, body, outfit, noise);
            Legs(s, f, body, outfit, noise);
            Clothes(s, f, body, outfit, noise, ref rng);
            s.Mesh.ComputeNormals();

            return new PersonModel
            {
                Spec = spec, Mesh = s.Mesh, Joints = s.Joints.ToArray(), Weights = s.Weights.ToArray(), BoneAt = at, Height = body.Height, Description = words.ToString(),
            };
        }

        /// <summary>The joints of one body, and the measures its parts are built to.</summary>
        sealed class Frame
        {
            public readonly V3[] At;
            public readonly float K;
            public readonly float Cx;
            public V3 HeadCentre;
            public float Rx, Ry, Rz;

            public Frame(V3[] at, Body body)
            {
                At = at;
                K = body.Height / HeroHeight;
                Cx = J("hips").X;
                var hk = K * body.HeadK;
                HeadCentre = J("head") + new V3(0, 0.085f, 0.022f) * hk;
                Rx = 0.076f * hk * body.FaceWidth;
                Ry = 0.106f * hk;
                Rz = 0.094f * hk;
            }

            public V3 J(string name) => At[Bone(name)];

            /// <summary>A point on the head's surface by its unit coordinates (x across, y up, z forward), pushed out by `lift`.</summary>
            public V3 OnHead(float xu, float yu, float lift = 0)
            {
                var zu = FaceZ(xu, yu);

                return HeadCentre + new V3(xu * Rx, yu * Ry, zu * Rz + lift);
            }

            public static float FaceZ(float xu, float yu)
            {
                var z = (float)Math.Sqrt(Math.Max(0, 1 - xu * xu - yu * yu));

                return z > 0.6f ? 0.6f + (z - 0.6f) * 0.6f : z;
            }
        }

        // ------------------------------------------------------------------ The trunk

        /// <summary>The torso's rings from the crotch to the neck: (height, half-width, half-depth, centre z).</summary>
        static List<(float y, float a, float c, float z)> TorsoKeys(Frame f, Body b)
        {
            var k = f.K;
            var arm = f.J("upperArm_L");
            var shoulderA = Math.Abs(arm.X - f.Cx) + 0.03f * k;
            var depth = b.DepthK;

            return new List<(float, float, float, float)>
            {
                (f.J("thigh_L").Y - 0.075f * k, 0.11f * k * b.HipK, 0.085f * k * depth, f.J("hips").Z + 0.005f * k),
                (f.J("hips").Y, 0.162f * k * b.HipK, 0.104f * k * depth, f.J("hips").Z + 0.01f * k),
                (f.J("spine").Y, 0.15f * k * Build.Lerp(b.HipK, b.WaistK, 0.6f), 0.1f * k * depth * (1 + 0.2f * b.Girth), f.J("spine").Z + 0.015f * k),
                (f.J("spine1").Y, 0.142f * k * b.WaistK, 0.098f * k * depth * (1 + 0.28f * b.Girth), f.J("spine1").Z + 0.02f * k),
                (f.J("spine2").Y, 0.152f * k * b.ChestK, 0.108f * k * depth, f.J("spine2").Z + 0.02f * k),
                (f.J("chest").Y, 0.164f * k * b.ChestK, 0.116f * k * depth, f.J("chest").Z + 0.015f * k),
                (arm.Y + 0.035f * k, shoulderA, 0.092f * k * depth, (f.J("chest").Z + arm.Z) * 0.5f),
                (f.J("neck").Y + 0.012f * k, 0.062f * k * (1 + 0.2f * b.Girth), 0.058f * k, f.J("neck").Z + 0.01f * k),
            };
        }

        /// <summary>The torso's rings between two heights, `margin` out from the skin, subdivided for smoothness.</summary>
        static (List<V3> centres, List<float> a, List<float> c) TorsoRings(Frame f, Body b, float from, float to, float margin, int steps = 3, float shoulderCut = 1)
        {
            var keys = TorsoKeys(f, b);
            var centres = new List<V3>();
            var a = new List<float>();
            var c = new List<float>();

            void Add(float y)
            {
                // Between the keys either side, smoothly.
                var i = 0;

                while (i < keys.Count - 2 && y > keys[i + 1].y)
                {
                    i++;
                }

                var k0 = keys[i];
                var k1 = keys[i + 1];
                var t = Noise.Clamp01((y - k0.y) / Math.Max(1e-4f, k1.y - k0.y));
                var smooth = t * t * (3 - 2 * t);
                var ra = Build.Lerp(k0.a, k1.a, smooth);
                var rc = Build.Lerp(k0.c, k1.c, smooth);

                if (i == keys.Count - 2)
                {
                    // Over the shoulders to the neck: the width falls away fast (sloping shoulders).
                    ra = Build.Lerp(k0.a, k1.a, (float)Math.Pow(t, 0.55f * shoulderCut));
                }

                centres.Add(new V3(f.Cx, y, Build.Lerp(k0.z, k1.z, smooth)));
                a.Add(ra + margin);
                c.Add(rc + margin);
            }

            var heights = new List<float> { from };

            foreach (var key in keys)
            {
                if (key.y > from + 0.01f && key.y < to - 0.01f)
                {
                    heights.Add(key.y);
                }
            }

            heights.Add(to);

            for (var h = 0; h < heights.Count - 1; h++)
            {
                for (var s = 0; s < steps; s++)
                {
                    Add(Build.Lerp(heights[h], heights[h + 1], s / (float)steps));
                }
            }

            Add(to);

            return (centres, a, c);
        }

        static readonly string[] TorsoBones = { "hips", "spine", "spine1", "spine2", "chest", "neck", "shoulder_L", "shoulder_R", "thigh_L", "thigh_R" };

        static void Torso(Skinned s, Frame f, Body b, Outfit o)
        {
            var keys = TorsoKeys(f, b);
            var (centres, a, c) = TorsoRings(f, b, keys[0].y, keys[keys.Count - 1].y, 0, 3);
            var chestY = f.J("chest").Y - 0.03f * f.K;
            var bust = b.Bust * 0.035f * f.K;
            var from = s.Mesh.VertexCount;
            Loft(s.Mesh, centres, a, c, new V3(1, 0, 0), 16, Plain, b.Skin, true, false, (ring, t) =>
            {
                if (bust <= 0)
                {
                    return 0;
                }

                // The bust: two swellings on the front, about the chest's height (t 0.25 is the front).
                var dy = (centres[ring].Y - chestY) / (0.07f * f.K);
                var side = Math.Min(Math.Abs(t - 0.18f), Math.Abs(t - 0.32f));

                return bust * (float)Math.Exp(-dy * dy) * (float)Math.Exp(-side * side / 0.004f);
            });
            s.Skin(from, TorsoBones);
        }

        static void Neck(Skinned s, Frame f, Body b)
        {
            var from = s.Mesh.VertexCount;
            var r = (b.Bust > 0 || b.HeadK > 1.1f ? 0.041f : 0.047f) * f.K * (1 + 0.3f * b.Girth);
            Limb(s.Mesh, new[] { f.J("neck") - V3.Up * (0.02f * f.K), f.J("neck1"), f.J("head") + V3.Up * (0.03f * f.K) }, new[] { r * 1.1f, r, r * 0.95f }, 0.95f, new V3(1, 0, 0), 10, 2, Plain, b.Skin, false, false);
            s.Skin(from, "chest", "neck", "neck1", "head");
        }

        // ------------------------------------------------------------------ The head and face

        static void Head(Skinned s, Frame f, Body b, Outfit o, Noise noise)
        {
            var sphere = Build.Sphere(3);
            var from = s.Mesh.VertexCount;
            var stubble = o.Beard == "stubble" || o.Beard == "moustache" || (o.Beard != "none" && b.Hair.R < 0.3f);

            for (var i = 0; i < sphere.VertexCount; i++)
            {
                float x = sphere.Positions[i * 3], y = sphere.Positions[i * 3 + 1], z = sphere.Positions[i * 3 + 2];
                // The jaw narrows below; the chin juts or recedes; the face is flatter than a ball; a dent under the brow for each eye.
                var low = Noise.Clamp01(-y);
                x *= 1 - (0.3f - 0.18f * (b.Jaw - 1)) * low * low * 1.4f;
                x *= y > -0.2f && y < 0.25f ? 1 + 0.05f * (b.Cheek - 1) : 1;

                if (z > 0)
                {
                    z = z > 0.6f ? 0.6f + (z - 0.6f) * 0.6f : z;

                    if (y < -0.55f)
                    {
                        z += 0.12f * (b.Chin - 1) * Noise.Clamp01((-y - 0.55f) * 3);
                    }

                    var eye = Math.Min(Sq(x - 0.36f * b.EyeGap) + Sq(y - 0.05f) * 1.6f, Sq(x + 0.36f * b.EyeGap) + Sq(y - 0.05f) * 1.6f);
                    z -= 0.06f * (float)Math.Exp(-eye / 0.012f) * Noise.Clamp01(z * 2);
                }
                else if (y > -0.3f)
                {
                    z *= 1.06f;
                }

                var p = f.HeadCentre + new V3(x * f.Rx, y * f.Ry, z * f.Rz);
                var skin = b.Skin;
                var cheek = Math.Min(Sq(x - 0.5f) + Sq(y + 0.25f), Sq(x + 0.5f) + Sq(y + 0.25f));
                skin = Tint.Mix(skin, Tint.Hex(0xd07a6a), 0.18f * (float)Math.Exp(-cheek / 0.03f) * (z > 0.3f ? 1 : 0));

                if (Math.Abs(x) < 0.2f * b.Mouth && y > -0.53f && y < -0.43f && z > 0.55f)
                {
                    skin = b.Lips;
                }

                if (stubble && y < -0.25f && z > 0.1f && !(Math.Abs(x) < 0.2f && y > -0.55f && y < -0.4f))
                {
                    skin = Tint.Mix(skin, b.Hair, 0.35f);
                }

                skin = Build.Shade(skin, 0.97f + 0.05f * noise.Perlin(x * 9, y * 9 + z * 5));
                s.Mesh.Vertex(p.X, p.Y, p.Z, 0.5f + x * 0.5f, 0.5f + y * 0.5f, skin.R, skin.G, skin.B);
            }

            for (var t = 0; t < sphere.Triangles.Count; t++)
            {
                s.Mesh.Triangles.Add(from + sphere.Triangles[t]);
            }

            var hk = f.K * b.HeadK;

            // Eyes: white, the iris and the pupil painted on the ball's front.
            foreach (var side in new[] { -1f, 1f })
            {
                var centre = f.OnHead(0.36f * b.EyeGap * side, 0.05f, -0.011f * hk);
                var r = 0.0145f * hk * b.EyeSize;
                var eyes = b.Eyes;
                Build.Blob(s.Mesh, centre, new V3(r, 0, 0), new V3(0, r * 0.82f, 0), new V3(0, 0, r), 1, noise, 0, (p, n) =>
                    n.Z > 0.88f ? Tint.Hex(0x0a0808) : n.Z > 0.55f ? eyes : Tint.Hex(0xe8e4dc));
                // The brow over it, tilted.
                var tilt = b.BrowTilt * side * (float)Math.PI / 180;
                var along = new V3((float)Math.Cos(tilt), (float)Math.Sin(tilt), 0);
                var hair = b.Hair;
                Build.Blob(s.Mesh, f.OnHead(0.36f * b.EyeGap * side, 0.24f, 0.002f * hk), along * (0.024f * hk), new V3(0, 0.0045f * hk * b.BrowThick, 0), new V3(0, 0, 0.006f * hk), 1, noise, 0, (p, n) => hair);
                // An ear.
                var ear = f.HeadCentre + new V3(side * f.Rx * 0.96f, -0.02f * hk, -0.01f * hk);
                var skin = Build.Shade(b.Skin, 0.95f);
                Build.Blob(s.Mesh, ear, new V3(0.008f * hk, 0, 0), new V3(0, 0.028f * hk * b.Ears, 0), new V3(0, 0, 0.017f * hk), 1, noise, 0.1f, (p, n) => skin);
            }

            // Nose and lips.
            var noseSkin = Tint.Mix(b.Skin, Tint.Hex(0xc87a6a), 0.12f);
            Build.Blob(s.Mesh, f.OnHead(0, -0.15f, -0.002f * hk), new V3(0.0105f * hk * b.NoseWidth, 0, 0), new V3(0, 0.017f * hk * b.Nose, 0.003f * hk), new V3(0, -0.004f * hk, 0.011f * hk * b.Nose), 1, noise, 0.05f, (p, n) => noseSkin);
            var lips = b.Lips;
            Build.Blob(s.Mesh, f.OnHead(0, -0.48f, -0.001f * hk), new V3(0.021f * hk * b.Mouth, 0, 0), new V3(0, 0.0055f * hk, 0), new V3(0, 0, 0.006f * hk), 1, noise, 0, (p, n) => lips);
            s.Skin(from, "head", "neck1");
        }

        static float Sq(float x) => x * x;

        // ------------------------------------------------------------------ Hair

        static void Hair(Skinned s, Frame f, Body b, Outfit o, Noise noise, ref Rng rng)
        {
            var hk = f.K * b.HeadK;
            var hair = b.Hair;
            Tint HairColour(V3 p) => Build.Shade(hair, 0.82f + 0.3f * (noise.Perlin(p.X * 90, p.Y * 12) * 0.5f + 0.5f));
            var style = o.Hair;
            var hatCovers = o.Hat == "scarf" || o.Hat == "helmet";

            if (style == "bald" || hatCovers && style == "short")
            {
                return;
            }

            // The cap of hair over the skull, its edge by the style: high on the forehead, low at the nape.
            var front = style == "bob" ? 0.12f : style == "balding" ? 2f : 0.42f;
            var back = style == "short" || style == "topknot" || style == "bun" ? -0.42f : -0.62f;
            var puff = style == "bob" ? 1.13f : style == "long" || style == "braid" ? 1.08f : 1.06f;
            var from = s.Mesh.VertexCount;
            var sphere = Build.Sphere(2);
            var keep = new int[sphere.VertexCount];

            for (var i = 0; i < sphere.VertexCount; i++)
            {
                float x = sphere.Positions[i * 3], y = sphere.Positions[i * 3 + 1], z = sphere.Positions[i * 3 + 2];
                bool kept;

                if (style == "balding")
                {
                    kept = z < 0.35f && y < 0.5f && y > -0.45f;
                }
                else
                {
                    // The hairline round the head: high over the brow, down to above the ear at the
                    // sides (a sideburn before it), low at the nape.
                    var around = (float)Math.Abs(Math.Atan2(x, z)) / (float)Math.PI;
                    float line;

                    if (around < 0.5f)
                    {
                        var t = around / 0.5f;
                        line = Build.Lerp(front, -0.12f, (float)Math.Pow(t, 0.7f));
                    }
                    else
                    {
                        var t = (around - 0.5f) / 0.5f;
                        line = Build.Lerp(-0.12f, back, t * t * (3 - 2 * t));
                    }

                    kept = y > line;
                }

                if (!kept)
                {
                    keep[i] = -1;
                    continue;
                }

                var low = Noise.Clamp01(-y);
                var xx = x * (1 - 0.18f * low);
                var p = f.HeadCentre + new V3(xx * f.Rx * puff, y * f.Ry * (puff * 0.98f) + 0.004f * hk, z * f.Rz * puff + (z < 0 ? -0.004f * hk : 0));
                var c = HairColour(p);
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

            s.Skin(from, "head", "neck1");
            var extras = s.Mesh.VertexCount;
            var skinned = false;
            var top = f.HeadCentre + new V3(0, f.Ry * 1.02f, -0.1f * f.Rz);
            var backOfHead = f.HeadCentre + new V3(0, 0.2f * f.Ry, -f.Rz * 1.02f);
            var pin = o.PinColour;

            switch (style)
            {
                case "topknot":
                    Build.Blob(s.Mesh, top + V3.Up * (0.022f * hk), new V3(0.03f * hk, 0, 0), new V3(0, 0.034f * hk, 0), new V3(0, 0, 0.03f * hk), 1, noise, 0.1f, (p, n) => HairColour(p));

                    if (o.Pin)
                    {
                        Build.Blob(s.Mesh, top + V3.Up * (0.026f * hk), new V3(0.075f * hk, 0, 0), new V3(0, 0.004f * hk, 0), new V3(0, 0, 0.004f * hk), 0, noise, 0, (p, n) => pin);
                    }
                    else
                    {
                        // A cloth band round the knot.
                        var band = o.Trim;
                        Build.Blob(s.Mesh, top + V3.Up * (0.012f * hk), new V3(0.033f * hk, 0, 0), new V3(0, 0.012f * hk, 0), new V3(0, 0, 0.033f * hk), 1, noise, 0, (p, n) => band);
                    }

                    break;
                case "bun":
                {
                    var at = f.HeadCentre + new V3(0, 0.35f * f.Ry, -f.Rz * 1.05f);
                    Build.Blob(s.Mesh, at, new V3(0.042f * hk, 0, 0), new V3(0, 0.036f * hk, 0), new V3(0, 0, 0.034f * hk), 1, noise, 0.12f, (p, n) => HairColour(p));

                    if (o.Pin)
                    {
                        Build.Blob(s.Mesh, at + new V3(0, 0.01f * hk, -0.01f * hk), new V3(0.085f * hk, 0, 0), new V3(0, 0.004f * hk, 0.002f * hk), new V3(0, 0, 0.004f * hk), 0, noise, 0, (p, n) => pin);
                    }

                    break;
                }
                case "twinbuns":
                    foreach (var side in new[] { -1f, 1f })
                    {
                        Build.Blob(s.Mesh, f.HeadCentre + new V3(side * f.Rx * 0.75f, f.Ry * 0.78f, -0.15f * f.Rz), new V3(0.03f * hk, 0, 0), new V3(0, 0.03f * hk, 0), new V3(0, 0, 0.03f * hk), 1, noise, 0.1f, (p, n) => HairColour(p));
                    }

                    break;
                case "ponytail":
                case "tied":
                case "braid":
                {
                    var start = style == "tied" ? backOfHead - V3.Up * (0.06f * hk) : f.HeadCentre + new V3(0, 0.55f * f.Ry, -f.Rz * 0.95f);
                    var length = style == "tied" ? rng.Range(0.12f, 0.22f) : rng.Range(0.24f, 0.42f) * (b.Height / 1.7f);
                    var neck = f.J("neck");
                    var chestBack = f.J("chest").Z - 0.13f * f.K;
                    var points = new[] { start, start + new V3(0, -0.06f, -0.03f) * hk, new V3(start.X, start.Y - length * 0.55f, chestBack - 0.02f), new V3(start.X, start.Y - length, chestBack - 0.01f) };

                    // Past the head the tail lies on the back, not in it.
                    points[2] = new V3(points[2].X, points[2].Y, Math.Min(points[2].Z, neck.Z - 0.08f * f.K));
                    var radii = style == "braid" ? new[] { 0.024f, 0.022f, 0.018f, 0.007f } : new[] { 0.028f, 0.03f, 0.022f, 0.006f };

                    for (var r = 0; r < radii.Length; r++)
                    {
                        radii[r] *= hk;
                    }

                    var start0 = s.Mesh.VertexCount;
                    Limb(s.Mesh, points, radii, 0.8f, new V3(1, 0, 0), 8, 3, (ring, t, col, p) => style == "braid" && ring % 2 == 0 ? Build.Shade(HairColour(p), 0.75f) : HairColour(p), hair, false, true);
                    s.Skin(start0, "head", "neck1", "neck", "chest", "spine2");
                    skinned = true;

                    break;
                }
                case "long":
                {
                    // A fall of hair down the back to the shoulder blades or lower.
                    var start = f.HeadCentre + new V3(0, -0.1f * f.Ry, -f.Rz * 0.92f);
                    var bottom = f.J("chest").Y - rng.Range(0.02f, 0.2f) * f.K;
                    var backZ = f.J("chest").Z - 0.13f * f.K;
                    var centres = new List<V3>();
                    var a = new List<float>();
                    var c = new List<float>();

                    for (var i = 0; i <= 6; i++)
                    {
                        var t = i / 6f;
                        var y = Build.Lerp(start.Y, bottom, t);
                        var z = Build.Lerp(start.Z, backZ - 0.012f * f.K, (float)Math.Sqrt(t));
                        centres.Add(new V3(start.X, y, z));
                        a.Add(f.Rx * Build.Lerp(0.95f, 0.85f, t));
                        c.Add(0.022f * hk * (1 - 0.4f * t));
                    }

                    var start0 = s.Mesh.VertexCount;
                    Loft(s.Mesh, centres, a, c, new V3(1, 0, 0), 10, (ring, t, col, p) => HairColour(p), hair, false, true);
                    s.Skin(start0, "head", "neck1", "neck", "chest", "spine2");
                    skinned = true;

                    break;
                }
            }

            if (!skinned)
            {
                s.Skin(extras, "head");
            }

            // Beards.
            var beardFrom = s.Mesh.VertexCount;

            if (o.Beard == "moustache" || o.Beard == "goatee" || o.Beard == "long")
            {
                Build.Blob(s.Mesh, f.OnHead(0, -0.38f, 0.003f * hk), new V3(0.026f * hk, -0.004f * hk, 0), new V3(0, 0.005f * hk, 0), new V3(0, 0, 0.007f * hk), 1, noise, 0.1f, (p, n) => HairColour(p));
            }

            if (o.Beard == "goatee")
            {
                Build.Blob(s.Mesh, f.OnHead(0, -0.82f, -0.004f * hk), new V3(0.016f * hk, 0, 0), new V3(0, 0.026f * hk, 0), new V3(0, 0, 0.012f * hk), 1, noise, 0.1f, (p, n) => HairColour(p));
            }

            if (o.Beard == "long")
            {
                var chin = f.OnHead(0, -0.78f, -0.01f * hk);
                var length = rng.Range(0.1f, 0.24f) * f.K;
                Limb(s.Mesh, new[] { chin + new V3(0, 0.01f, -0.01f), chin + new V3(0, -length * 0.45f, 0.012f), chin + new V3(0, -length, 0.0f) }, new[] { 0.032f * hk, 0.026f * hk, 0.006f * hk }, 0.55f,
                    new V3(1, 0, 0), 8, 3, (ring, t, col, p) => HairColour(p), hair, false, true);
            }

            s.Skin(beardFrom, "head");
        }

        // ------------------------------------------------------------------ Limbs

        static void Arms(Skinned s, Frame f, Body b, Outfit o, Noise noise)
        {
            var k = f.K;

            foreach (var side in new[] { "_L", "_R" })
            {
                var shoulder = f.J("upperArm" + side);
                var elbow = f.J("forearm" + side);
                var wrist = f.J("hand" + side);
                var finger = f.J("middle1" + side);
                var from = s.Mesh.VertexCount;
                var thick = 1 + 0.25f * b.Muscle + 0.2f * b.Girth;
                Limb(s.Mesh, new[] { shoulder + V3.Up * (0.01f * k), V3.Lerp(shoulder, elbow, 0.5f), elbow, V3.Lerp(elbow, wrist, 0.45f), wrist },
                    new[] { 0.05f * k * thick, 0.045f * k * thick, 0.036f * k, 0.038f * k * thick, 0.027f * k }, 0.88f, new V3(1, 0, 0), 10, 2, Plain, b.Skin, true, true);
                Build.Blob(s.Mesh, shoulder + V3.Up * (0.008f * k), new V3(0.055f * k * thick, 0, 0), new V3(0, 0.05f * k, 0), new V3(0, 0, 0.052f * k), 1, noise, 0, (p, n) => b.Skin);
                s.Skin(from, "shoulder" + side, "upperArm" + side, "forearm" + side, "hand" + side, "chest");

                // The hand: a palm with fingers together, and a thumb before it.
                var hand = s.Mesh.VertexCount;
                var along = (finger - wrist).Normalized;
                var outward = new V3(side == "_L" ? -1 : 1, 0, 0);
                var front = V3.Cross(along, outward).Normalized;

                if (front.Z < 0)
                {
                    front = front * -1;
                }

                var palm = wrist + along * (0.05f * k);
                var skin = b.Skin;
                Build.Blob(s.Mesh, palm, along * (0.05f * k), front * (0.036f * k), outward * (0.015f * k), 1, noise, 0.05f, (p, n) => skin);
                Build.Blob(s.Mesh, palm + along * (0.06f * k), along * (0.035f * k), front * (0.032f * k), outward * (0.012f * k), 1, noise, 0.05f, (p, n) => skin);
                Build.Blob(s.Mesh, wrist + along * (0.035f * k) + front * (0.03f * k) - outward * (0.008f * k), (along + front * 0.5f).Normalized * (0.03f * k), front * (0.011f * k), outward * (0.011f * k), 1, noise, 0, (p, n) => skin);
                s.Skin(hand, "hand" + side, "middle1" + side, "middle2" + side, "forearm" + side);
            }
        }

        static void Legs(Skinned s, Frame f, Body b, Outfit o, Noise noise)
        {
            var k = f.K;

            foreach (var side in new[] { "_L", "_R" })
            {
                var hip = f.J("thigh" + side);
                var knee = f.J("shin" + side);
                var ankle = f.J("foot" + side);
                var from = s.Mesh.VertexCount;
                var thick = 1 + 0.2f * b.Muscle + 0.3f * b.Girth;
                // Only the part a garment leaves bare needs to be good; the rest is covered.
                Limb(s.Mesh, new[] { hip + V3.Up * (0.04f * k), V3.Lerp(hip, knee, 0.5f), knee, V3.Lerp(knee, ankle, 0.35f), ankle },
                    new[] { 0.085f * k * thick * b.HipK, 0.07f * k * thick, 0.05f * k, 0.056f * k * thick, 0.036f * k }, 0.95f, new V3(1, 0, 0), 10, 2, Plain, b.Skin, false, false);
                s.Skin(from, "hips", "thigh" + side, "shin" + side, "foot" + side);

                // The foot, under its shoe or bare.
                var foot = s.Mesh.VertexCount;
                var colour = o.Barefoot || o.Sandals ? b.Skin : o.Shoes;
                var swell = o.Barefoot || o.Sandals ? 1 : 1.12f;
                Build.Blob(s.Mesh, ankle + new V3(0, -0.055f * k, 0.065f * k), new V3(0.04f * k * swell, 0, 0), new V3(0, 0.034f * k * swell, 0), new V3(0, -0.01f * k, 0.12f * k * swell), 1, noise, 0.04f, (p, n) => colour);

                if (o.Sandals)
                {
                    var sole = Tint.Hex(0xa08a5a);
                    Build.Blob(s.Mesh, ankle + new V3(0, -0.085f * k, 0.065f * k), new V3(0.046f * k, 0, 0), new V3(0, 0.008f * k, 0), new V3(0, 0, 0.13f * k), 1, noise, 0, (p, n) => sole);
                }

                s.Skin(foot, "foot" + side, "shin" + side);
            }
        }
    }
}
