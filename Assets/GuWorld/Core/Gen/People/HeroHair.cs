using System;
using System.Collections.Generic;
using GuWorld.Core.Gen.Flora;

namespace GuWorld.Core.Gen.People
{
    /// <summary>One piece of a hair style: its mesh, its colour (0xRRGGBB, as the eye sees it) and how glossy it is (0–1).</summary>
    public sealed class HairPiece
    {
        public string Name;
        public MeshData Mesh;
        public int Colour;
        public float Gloss;
    }

    /// <summary>
    /// The hero's hair styles (Appearances.Hairs, ROADMAP 2.5). The model's long loose hair is cast in
    /// its mesh — no bone of the hair carries any of it — so a style is worn up over it, as a youth of the
    /// clan wears it half up: a knot on the crown with a wooden pin through it; the knot in a small silver
    /// guan with a jade pin; the knot bound with a moon-white ribbon whose two tails lie down the back of
    /// the head. The pieces are made in the hero model's own space at rest — the engine's axes, +Y up,
    /// +Z the face, X mirrored from the file's — where gu-hero.glb was measured: the crown 1.78 m up, its
    /// top a little before the head's middle, the head's middle 2.8 cm to the model's −X side. The game
    /// hangs them on the head's bone, so they turn with it.
    /// </summary>
    public static class HeroHair
    {
        /// <summary>The head's middle across (the model is not centred on its own axis).</summary>
        public const float MidX = -0.028f;

        /// <summary>The knot: where it sits on the crown, a little behind the top, and how far it leans back with the skull there.</summary>
        public static readonly V3 Knot = new V3(MidX, 1.777f, -0.03f);
        public const float KnotLean = 0.36f;
        public const float KnotX = 0.038f, KnotY = 0.031f, KnotZ = 0.036f;

        /// <summary>
        /// The back of the head down its middle (height, how far behind), measured off the model with the
        /// hair on: what a ribbon's tails lie along.
        /// </summary>
        static readonly (float y, float z)[] BackOfHead =
        {
            (1.756f, -0.040f), (1.729f, -0.060f), (1.700f, -0.087f), (1.660f, -0.097f), (1.620f, -0.113f), (1.580f, -0.124f), (1.560f, -0.131f),
        };

        public const int HairColour = 0x141316;
        const int Wood = 0x4A2E1C;
        const int Jade = 0x5FA585;
        const int Silver = 0xA9AFB7;
        const int Ribbon = 0xE6ECF2;

        /// <summary>The pieces of a style (none for the loose hair, 0).</summary>
        public static List<HairPiece> Make(int style)
        {
            var pieces = new List<HairPiece>();

            if (style <= 0 || style >= Appearances.Hairs.Length)
            {
                return pieces;
            }

            var noise = new Noise(4517);
            var knot = new MeshData();
            // The knot: a lumpy ball of hair, wider than high, its strands wound (the noise runs round it).
            Build.Blob(knot, new V3(0, 0, 0), new V3(KnotX, 0, 0), new V3(0, KnotY, 0), new V3(0, 0, KnotZ), 2, noise, 0.07f, (p, n) => new Tint(1, 1, 1));
            pieces.Add(new HairPiece { Name = "Knot", Mesh = Placed(knot), Colour = HairColour, Gloss = 0.45f });

            switch (style)
            {
                case 1:
                    pieces.Add(new HairPiece { Name = "Pin", Mesh = Placed(Pin(0.125f, 0.0034f, 0.0068f)), Colour = Wood, Gloss = 0.3f });
                    break;
                case 2:
                {
                    // The guan: a small silver cup over the top of the knot, a rim round its foot, the jade pin through both.
                    var guan = new MeshData();
                    Cap(guan, KnotX + 0.004f, KnotZ + 0.004f, -KnotY * 0.1f, KnotY * 0.8f);
                    var rim = Shrunk(-KnotY * 0.1f);
                    Band(guan, rim.x + 0.004f, rim.z + 0.004f, -KnotY * 0.2f, rim.x + 0.009f, rim.z + 0.009f, -KnotY * 0.02f);
                    pieces.Add(new HairPiece { Name = "Guan", Mesh = Placed(guan), Colour = Silver, Gloss = 0.75f });
                    pieces.Add(new HairPiece { Name = "Pin", Mesh = Placed(Pin(0.14f, 0.0032f, 0.0075f)), Colour = Jade, Gloss = 0.7f });
                    break;
                }
                case 3:
                {
                    // The ribbon: a band round the knot's foot, and its two tails down the back of the head.
                    var band = new MeshData();
                    var foot = Shrunk(-KnotY * 0.72f);
                    var waist = Shrunk(-KnotY * 0.3f);
                    Band(band, foot.x + 0.003f, foot.z + 0.003f, -KnotY * 0.72f, waist.x + 0.003f, waist.z + 0.003f, -KnotY * 0.3f);
                    var ribbon = Placed(band);

                    foreach (var side in new[] { -1f, 1f })
                    {
                        var points = new List<V3>();
                        var widths = new List<float>();

                        for (var i = 0; i < BackOfHead.Length; i++)
                        {
                            var t = i / (float)(BackOfHead.Length - 1);
                            // A finger's breadth off the hair, the two tails parting a little as they fall.
                            var (y, z) = BackOfHead[i];
                            points.Add(new V3(MidX + side * (0.009f + 0.012f * t), y, z - 0.011f - 0.004f * t));
                            widths.Add(0.022f - 0.004f * t);
                        }

                        Build.Ribbon(ribbon, points, widths, new V3(1, 0, 0), i => new Tint(1, 1, 1));
                    }

                    pieces.Add(new HairPiece { Name = "Ribbon", Mesh = ribbon, Colour = Ribbon, Gloss = 0.2f });
                    break;
                }
            }

            return pieces;
        }

        /// <summary>A pin through the knot from side to side, a bead at its head (made about the knot's middle).</summary>
        static MeshData Pin(float length, float radius, float bead)
        {
            var pin = new MeshData();
            var half = length * 0.5f;
            var y = KnotY * 0.12f;
            Build.Tube(pin, new[] { new V3(half, y, 0.004f), new V3(-half * 0.2f, y, 0.006f), new V3(-half, y, 0.008f) }, new[] { radius, radius, radius * 0.4f }, 8, (r, a, p, n) => new Tint(1, 1, 1));
            var noise = new Noise(1);
            Build.Blob(pin, new V3(half + bead * 0.6f, y, 0.004f), new V3(bead, 0, 0), new V3(0, bead, 0), new V3(0, 0, bead), 1, noise, 0, (p, n) => new Tint(1, 1, 1));

            return pin;
        }

        /// <summary>The knot's half-widths across and front to back at a height of its own (an ellipsoid's).</summary>
        static (float x, float z) Shrunk(float y)
        {
            var k = (float)Math.Sqrt(Math.Max(0, 1 - y * y / (KnotY * KnotY)));

            return (KnotX * k, KnotZ * k);
        }

        /// <summary>An upright band round the knot (ellipses rx0 × rz0 at y0 and rx1 × rz1 at y1), seen from both sides.</summary>
        static void Band(MeshData mesh, float rx0, float rz0, float y0, float rx1, float rz1, float y1)
        {
            const int sides = 28;

            for (var face = 0; face < 2; face++)
            {
                var start = mesh.VertexCount;

                for (var i = 0; i <= sides; i++)
                {
                    var a = i / (float)sides * Build.Tau;
                    var c = (float)Math.Cos(a);
                    var s = (float)Math.Sin(a);
                    var outward = new V3(c / Math.Max(rx0, 1e-4f), 0, s / Math.Max(rz0, 1e-4f)).Normalized * (face == 0 ? 1 : -1);
                    var bottom = mesh.Vertex(c * rx0, y0, s * rz0, i / (float)sides, 0);
                    var top = mesh.Vertex(c * rx1, y1, s * rz1, i / (float)sides, 1);
                    mesh.SetNormal(bottom, outward.X, outward.Y, outward.Z);
                    mesh.SetNormal(top, outward.X, outward.Y, outward.Z);
                }

                for (var i = 0; i < sides; i++)
                {
                    var a = start + i * 2;

                    // Outward faces run clockwise seen from outside (the engine's front); the inner copy the other way.
                    if (face == 0)
                    {
                        mesh.Triangle(a, a + 1, a + 3);
                        mesh.Triangle(a, a + 3, a + 2);
                    }
                    else
                    {
                        mesh.Triangle(a, a + 3, a + 1);
                        mesh.Triangle(a, a + 2, a + 3);
                    }
                }
            }
        }

        /// <summary>A dome over the knot: an ellipse rx × rz at y0 rising `height` to a closed top (seen from outside).</summary>
        static void Cap(MeshData mesh, float rx, float rz, float y0, float height)
        {
            const int sides = 28;
            const int rings = 7;
            var start = mesh.VertexCount;

            for (var j = 0; j < rings; j++)
            {
                var phi = j / (float)rings * (float)Math.PI * 0.5f;
                var k = (float)Math.Cos(phi);
                var y = y0 + height * (float)Math.Sin(phi);

                for (var i = 0; i <= sides; i++)
                {
                    var a = i / (float)sides * Build.Tau;
                    var c = (float)Math.Cos(a);
                    var s = (float)Math.Sin(a);
                    // An ellipsoid's normal: the point over its axes' squares.
                    var n = new V3(c * k / rx, (float)Math.Sin(phi) / height, s * k / rz).Normalized;
                    var v = mesh.Vertex(c * rx * k, y, s * rz * k, i / (float)sides, j / (float)rings);
                    mesh.SetNormal(v, n.X, n.Y, n.Z);
                }
            }

            var top = mesh.Vertex(0, y0 + height, 0, 0.5f, 1);
            mesh.SetNormal(top, 0, 1, 0);
            var ring = sides + 1;

            for (var j = 0; j < rings - 1; j++)
            {
                for (var i = 0; i < sides; i++)
                {
                    var a = start + j * ring + i;
                    // Seen from outside: a below-left, a + ring above-left, a + ring + 1 above-right, a + 1 below-right — clockwise.
                    mesh.Triangle(a, a + ring, a + ring + 1);
                    mesh.Triangle(a, a + ring + 1, a + 1);
                }
            }

            for (var i = 0; i < sides; i++)
            {
                var a = start + (rings - 1) * ring + i;
                mesh.Triangle(a, top, a + 1);
            }
        }

        /// <summary>A piece made about the knot's middle, upright, put on the crown: leant back with the skull and moved to the knot.</summary>
        static MeshData Placed(MeshData local)
        {
            var placed = new MeshData();
            var axis = new V3(1, 0, 0);

            for (var i = 0; i < local.VertexCount; i++)
            {
                var p = Build.Rotate(new V3(local.Positions[i * 3], local.Positions[i * 3 + 1], local.Positions[i * 3 + 2]), axis, -KnotLean) + Knot;
                var n = Build.Rotate(new V3(local.Normals[i * 3], local.Normals[i * 3 + 1], local.Normals[i * 3 + 2]), axis, -KnotLean);
                var v = placed.Vertex(p.X, p.Y, p.Z, local.Uvs[i * 2], local.Uvs[i * 2 + 1]);
                placed.SetNormal(v, n.X, n.Y, n.Z);
            }

            placed.Triangles.AddRange(local.Triangles);

            return placed;
        }
    }
}
