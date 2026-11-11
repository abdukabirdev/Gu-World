using System;
using System.Collections.Generic;

namespace GuWorld.Core.Audio
{
    /// <summary>
    /// The beds of sound that lie under the world, each a seamless loop of some seconds: wind of three
    /// strengths, the forest by day (birds over leaves), the night (crickets, an owl), a river, a
    /// waterfall, rain and a storm's rain, a cave (drips over a low hum), the marsh (frogs), a
    /// settlement (wood knocks, a far chime). The engine plays several at once and fades them by place.
    /// </summary>
    public static class Ambience
    {
        static readonly Dictionary<string, Func<Wave>> beds = new Dictionary<string, Func<Wave>>
        {
            ["wind_soft"] = () => Wind(1, 0.35f),
            ["wind"] = () => Wind(2, 0.6f),
            ["wind_high"] = () => Wind(3, 1f),
            ["forest_day"] = ForestDay,
            ["night"] = Night,
            ["river"] = () => Water(4, 0.45f, 900),
            ["waterfall"] = () => Water(5, 1f, 500),
            ["rain"] = () => Rain(6, 0.6f),
            ["storm"] = () => Rain(7, 1f),
            ["cave"] = Cave,
            ["marsh"] = Marsh,
            ["settlement"] = Settlement,
        };

        public static IEnumerable<string> Ids => beds.Keys;

        /// <summary>How long each bed's loop is.</summary>
        public const float Seconds = 10;

        public static Wave Make(string id)
        {
            if (!beds.TryGetValue(id, out var make))
            {
                return null;
            }

            return make().Normalise(0.7f).Loop(1.5f);
        }

        static Wave Bed() => new Wave(Seconds + 1.5f);

        static Wave Wind(int seed, float strength)
        {
            var b = Bed();
            var noise = new Noise(3000 + seed);
            var band = new Biquad(FilterKind.Band, 400, 1.5f);
            var whistle = new Biquad(FilterKind.Band, 1200, 18);
            var gust = new OnePole(0.3f);
            var dice = new Dice(3001 + seed);
            var target = 0.5f;

            for (var i = 0; i < b.Length; i++)
            {
                if (i % 4410 == 0)
                {
                    target = dice.Range(0.25f, 1);
                }

                var level = gust.Low(target);
                band.Set(FilterKind.Band, 250 + 900 * level * strength, 1.2f);
                var body = band.Run(noise.Pink() * 3) * (0.4f + 0.6f * level);
                var sing = strength > 0.8f ? whistle.Run(noise.White()) * level * level * 0.6f : 0;
                b.Data[i] = (body + sing) * strength;
            }

            return b;
        }

        static void Chirp(Wave b, float at, float pitch, float length, int seed)
        {
            var one = new Wave(length);
            var dice = new Dice(seed);
            var notes = 2 + dice.Int(4);

            for (var i = 0; i < one.Length; i++)
            {
                var t = i / (float)Synth.Rate;
                var note = (int)(t / length * notes);
                var local = t - note * length / notes;
                var sweep = pitch * (1 + 0.25f * (float)Math.Sin(note * 2.1f) + 0.4f * local / (length / notes));
                one.Data[i] = Synth.Sine(sweep * t) * Envelope.Strike(local, length / notes * 0.8f, 0.003f);
            }

            b.Mix(one, at, 0.18f);
        }

        static Wave ForestDay()
        {
            var b = Wind(8, 0.3f);
            var dice = new Dice(3100);
            var leaves = new Noise(3101);
            var high = new Biquad(FilterKind.High, 3500, 0.7f);

            for (var i = 0; i < b.Length; i++)
            {
                b.Data[i] += high.Run(leaves.White()) * 0.04f * (0.5f + 0.5f * Synth.Sine(i / (float)Synth.Rate * 0.3f));
            }

            for (var k = 0; k < 18; k++)
            {
                Chirp(b, dice.Range(0, Seconds), dice.Range(2400, 5200), dice.Range(0.15f, 0.5f), 3200 + k);
            }

            return b;
        }

        static Wave Night()
        {
            var b = Wind(9, 0.2f);
            var dice = new Dice(3300);

            // Crickets: bursts of a high tone, in a rhythm of their own.
            for (var cricket = 0; cricket < 5; cricket++)
            {
                var pitch = dice.Range(4200, 5600);
                var period = dice.Range(0.45f, 0.9f);
                var offset = dice.Range(0, period);

                for (var at = offset; at < Seconds; at += period)
                {
                    var one = new Wave(0.12f);

                    for (var i = 0; i < one.Length; i++)
                    {
                        var t = i / (float)Synth.Rate;
                        var chirp = Synth.Square(60 * t, 0.5f) * 0.5f + 0.5f;
                        one.Data[i] = Synth.Sine(pitch * t) * chirp * Envelope.Strike(t, 0.1f, 0.005f);
                    }

                    b.Mix(one, at, 0.06f);
                }
            }

            // An owl, once.
            var owl = new Wave(1.2f);

            for (var i = 0; i < owl.Length; i++)
            {
                var t = i / (float)Synth.Rate;
                var hoot = t < 0.35f ? Envelope.Strike(t, 0.3f, 0.05f) : t > 0.55f ? Envelope.Strike(t - 0.55f, 0.6f, 0.05f) : 0;
                owl.Data[i] = Synth.Sine(380 * (1 - 0.05f * t) * t) * hoot;
            }

            b.Mix(owl.Room(0.8f, 0.4f, 0.5f, 0.5f), dice.Range(2, 7), 0.25f);

            return b;
        }

        static Wave Water(int seed, float strength, float colour)
        {
            var b = Bed();
            var noise = new Noise(3400 + seed);
            var low = new Biquad(FilterKind.Low, colour, 0.7f);
            var band = new Biquad(FilterKind.Band, colour * 2.5f, 1);
            var dice = new Dice(3401 + seed);

            for (var i = 0; i < b.Length; i++)
            {
                var rush = low.Run(noise.Brown() * 1.5f + noise.Pink()) * strength;
                var spray = band.Run(noise.White()) * 0.25f * strength * strength;
                b.Data[i] = rush + spray;
            }

            // Bubbles and the slap of the current on the stones.
            for (var k = 0; k < (int)(40 * (1.2f - strength)); k++)
            {
                var one = new Wave(0.06f);
                var pitch = dice.Range(500, 1600);

                for (var i = 0; i < one.Length; i++)
                {
                    var t = i / (float)Synth.Rate;
                    one.Data[i] = Synth.Sine(pitch * (1 + t * 12) * t) * Envelope.Strike(t, 0.05f);
                }

                b.Mix(one, dice.Range(0, Seconds), 0.12f);
            }

            return b;
        }

        static Wave Rain(int seed, float strength)
        {
            var b = Bed();
            var noise = new Noise(3500 + seed);
            var band = new Biquad(FilterKind.Band, 2500, 0.5f);
            var dice = new Dice(3501 + seed);

            for (var i = 0; i < b.Length; i++)
            {
                b.Data[i] = band.Run(noise.Pink() * 2) * (0.5f + 0.5f * strength);
            }

            // Drops on leaves and roofs.
            for (var k = 0; k < (int)(260 * strength); k++)
            {
                var one = new Wave(0.02f);
                var pitch = dice.Range(1800, 6000);

                for (var i = 0; i < one.Length; i++)
                {
                    var t = i / (float)Synth.Rate;
                    one.Data[i] = Synth.Sine(pitch * t) * Envelope.Strike(t, 0.012f);
                }

                b.Mix(one, dice.Range(0, Seconds), 0.12f);
            }

            if (strength > 0.9f)
            {
                var rumble = new Noise(3509);
                var low = new Biquad(FilterKind.Low, 90, 0.7f);

                for (var i = 0; i < b.Length; i++)
                {
                    b.Data[i] += low.Run(rumble.Brown()) * 0.8f * (0.5f + 0.5f * Synth.Sine(i / (float)Synth.Rate * 0.11f));
                }
            }

            return b;
        }

        static Wave Cave()
        {
            var b = Bed();
            var noise = new Noise(3600);
            var low = new Biquad(FilterKind.Low, 120, 2);

            for (var i = 0; i < b.Length; i++)
            {
                var t = i / (float)Synth.Rate;
                b.Data[i] = low.Run(noise.Brown() + Synth.Sine(55 * t) * 0.1f) * 0.6f;
            }

            var dice = new Dice(3601);

            for (var k = 0; k < 9; k++)
            {
                var drip = new Wave(0.4f);
                var pitch = dice.Range(900, 2200);

                for (var i = 0; i < drip.Length; i++)
                {
                    var t = i / (float)Synth.Rate;
                    drip.Data[i] = Synth.Sine(pitch * (1 + 2 * Envelope.Strike(t, 0.03f)) * t) * Envelope.Strike(t, 0.15f);
                }

                b.Mix(drip.Room(0.9f, 0.2f, 0.8f, 0.3f), dice.Range(0, Seconds - 0.5f), 0.25f);
            }

            return b;
        }

        static Wave Marsh()
        {
            var b = Water(10, 0.15f, 400);
            var dice = new Dice(3700);

            for (var frog = 0; frog < 4; frog++)
            {
                var pitch = dice.Range(180, 420);
                var period = dice.Range(0.8f, 2.2f);

                for (var at = dice.Range(0, period); at < Seconds; at += period * dice.Range(0.8f, 1.3f))
                {
                    var one = new Wave(0.25f);
                    var formant = new Biquad(FilterKind.Band, pitch * 2.5f, 3);

                    for (var i = 0; i < one.Length; i++)
                    {
                        var t = i / (float)Synth.Rate;
                        var rattle = Synth.Square((18 + frog * 3) * t, 0.3f) * 0.5f + 0.5f;
                        one.Data[i] = formant.Run(Synth.Saw(pitch * t)) * rattle * Envelope.Strike(t, 0.22f, 0.02f);
                    }

                    b.Mix(one, at, 0.2f);
                }
            }

            return b;
        }

        static Wave Settlement()
        {
            var b = Wind(11, 0.2f);
            var dice = new Dice(3800);

            // Wood knocks (someone working), a far chime, a hen.
            for (var k = 0; k < 10; k++)
            {
                var knock = new Wave(0.2f);
                var pitch = dice.Range(300, 700);

                for (var i = 0; i < knock.Length; i++)
                {
                    var t = i / (float)Synth.Rate;
                    knock.Data[i] = Modes.At(Modes.Wood, pitch, t, 0.15f);
                }

                b.Mix(knock.Room(0.5f, 0.5f, 0.3f, 0.2f), dice.Range(0, Seconds - 0.3f), 0.18f);
            }

            var chime = new Wave(2.5f);

            for (var i = 0; i < chime.Length; i++)
            {
                var t = i / (float)Synth.Rate;
                chime.Data[i] = Modes.At(Modes.Bell, 880, t, 2.2f);
            }

            b.Mix(chime, dice.Range(1, 6), 0.08f);

            return b;
        }
    }
}
