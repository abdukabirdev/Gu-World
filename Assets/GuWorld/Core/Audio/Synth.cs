using System;

namespace GuWorld.Core.Audio
{
    /// <summary>
    /// The parts sound is made of, in plain C#: oscillators, noise, envelopes, filters, delays and a
    /// small reverb, a plucked string (Karplus–Strong), struck bodies (sums of damped modes), and the
    /// buffers they write into. Everything is deterministic for a seed, so the same sound comes out on
    /// every machine and can be tested without an engine.
    /// </summary>
    public static class Synth
    {
        public const int Rate = 44100;
        public const float TwoPi = (float)(Math.PI * 2);

        public static int Samples(float seconds) => Math.Max(1, (int)(seconds * Rate));

        /// <summary>A note's frequency from its MIDI number (69 = A4 = 440 Hz).</summary>
        public static float Hz(float midi) => 440f * (float)Math.Pow(2, (midi - 69) / 12.0);

        public static float Sine(float phase) => (float)Math.Sin(phase * TwoPi);

        public static float Triangle(float phase)
        {
            var p = phase - (float)Math.Floor(phase);

            return p < 0.5f ? p * 4 - 1 : 3 - p * 4;
        }

        public static float Saw(float phase)
        {
            var p = phase - (float)Math.Floor(phase);

            return p * 2 - 1;
        }

        public static float Square(float phase, float width = 0.5f)
        {
            var p = phase - (float)Math.Floor(phase);

            return p < width ? 1 : -1;
        }

        /// <summary>Soft clipping (a gentle saturation that never passes ±1).</summary>
        public static float Soft(float x) => (float)Math.Tanh(x);

        public static float Lerp(float a, float b, float t) => a + (b - a) * t;

        public static float Clamp01(float x) => x < 0 ? 0 : x > 1 ? 1 : x;

        /// <summary>Decibels to a gain.</summary>
        public static float Db(float db) => (float)Math.Pow(10, db / 20);
    }

    /// <summary>A deterministic random source for sounds (xorshift).</summary>
    public struct Dice
    {
        uint state;

        public Dice(int seed)
        {
            state = (uint)(seed * 747796405 + 2891336453u);

            if (state == 0)
            {
                state = 0x9E3779B9;
            }
        }

        public uint Next()
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;

            return state;
        }

        /// <summary>0 … 1.</summary>
        public float Value() => (Next() & 0xFFFFFF) / (float)0x1000000;

        /// <summary>−1 … 1.</summary>
        public float Bipolar() => Value() * 2 - 1;

        public float Range(float min, float max) => min + (max - min) * Value();

        public int Int(int max) => max <= 0 ? 0 : (int)(Next() % (uint)max);

        public bool Chance(float p) => Value() < p;
    }

    /// <summary>White, pink and brown noise.</summary>
    public struct Noise
    {
        Dice dice;
        float b0, b1, b2, b3, b4, b5, b6;
        float brown;

        public Noise(int seed)
        {
            dice = new Dice(seed);
            b0 = b1 = b2 = b3 = b4 = b5 = b6 = 0;
            brown = 0;
        }

        public float White() => dice.Bipolar();

        /// <summary>Pink noise (Paul Kellet's filter): softer, like wind and rain.</summary>
        public float Pink()
        {
            var white = dice.Bipolar();
            b0 = 0.99886f * b0 + white * 0.0555179f;
            b1 = 0.99332f * b1 + white * 0.0750759f;
            b2 = 0.96900f * b2 + white * 0.1538520f;
            b3 = 0.86650f * b3 + white * 0.3104856f;
            b4 = 0.55000f * b4 + white * 0.5329522f;
            b5 = -0.7616f * b5 - white * 0.0168980f;
            var pink = b0 + b1 + b2 + b3 + b4 + b5 + b6 + white * 0.5362f;
            b6 = white * 0.115926f;

            return pink * 0.11f;
        }

        /// <summary>Brown noise: a rumble (a waterfall far off, the sea of the wind).</summary>
        public float Brown()
        {
            brown = (brown + 0.02f * dice.Bipolar()) / 1.02f;

            return brown * 3.5f;
        }
    }

    /// <summary>An envelope: attack, decay to sustain, release (times in seconds), shaped exponentially.</summary>
    public struct Envelope
    {
        public float Attack, Decay, Sustain, Release;

        public Envelope(float attack, float decay, float sustain, float release)
        {
            Attack = attack;
            Decay = decay;
            Sustain = sustain;
            Release = release;
        }

        /// <summary>The level at a time into the note, the note held for `held` seconds.</summary>
        public float At(float t, float held)
        {
            if (t < 0)
            {
                return 0;
            }

            float level;

            if (t < Attack)
            {
                level = Attack > 0 ? t / Attack : 1;
            }
            else if (t < Attack + Decay)
            {
                var k = (t - Attack) / Math.Max(0.0001f, Decay);
                level = Sustain + (1 - Sustain) * (float)Math.Exp(-k * 4);
            }
            else
            {
                level = Sustain;
            }

            if (t > held)
            {
                var released = (t - held) / Math.Max(0.0001f, Release);
                var atRelease = held < Attack ? held / Math.Max(0.0001f, Attack) : held < Attack + Decay ? Sustain + (1 - Sustain) * (float)Math.Exp(-(held - Attack) / Math.Max(0.0001f, Decay) * 4) : Sustain;
                level = atRelease * (float)Math.Exp(-released * 5);
            }

            return level;
        }

        /// <summary>A struck sound: an instant attack and an exponential fall over `seconds` (−60 dB).</summary>
        public static float Strike(float t, float seconds, float attack = 0.002f)
        {
            if (t < 0)
            {
                return 0;
            }

            if (t < attack)
            {
                return t / attack;
            }

            return (float)Math.Exp(-(t - attack) / Math.Max(0.0001f, seconds) * 6.9f);
        }
    }

    /// <summary>A one-pole low-pass (the cheapest smoothing there is).</summary>
    public struct OnePole
    {
        float y;
        float a;

        public OnePole(float cutoff)
        {
            y = 0;
            a = 0;
            Set(cutoff);
        }

        public void Set(float cutoff) => a = (float)Math.Exp(-Synth.TwoPi * Math.Min(cutoff, Synth.Rate * 0.49f) / Synth.Rate);

        public float Low(float x)
        {
            y = x * (1 - a) + y * a;

            return y;
        }

        public float High(float x) => x - Low(x);
    }

    public enum FilterKind { Low, High, Band, Notch, Peak }

    /// <summary>A biquad filter (RBJ's cookbook): resonant low, high, band, notch and peak.</summary>
    public struct Biquad
    {
        float b0, b1, b2, a1, a2;
        float x1, x2, y1, y2;

        public Biquad(FilterKind kind, float frequency, float q, float gainDb = 0)
        {
            b0 = b1 = b2 = a1 = a2 = 0;
            x1 = x2 = y1 = y2 = 0;
            Set(kind, frequency, q, gainDb);
        }

        public void Set(FilterKind kind, float frequency, float q, float gainDb = 0)
        {
            frequency = Math.Max(10, Math.Min(frequency, Synth.Rate * 0.45f));
            var w = Synth.TwoPi * frequency / Synth.Rate;
            var cos = (float)Math.Cos(w);
            var sin = (float)Math.Sin(w);
            var alpha = sin / (2 * Math.Max(0.05f, q));
            var amp = (float)Math.Pow(10, gainDb / 40);
            float n0, n1, n2, d0, d1, d2;

            switch (kind)
            {
                case FilterKind.High:
                    n0 = (1 + cos) / 2; n1 = -(1 + cos); n2 = (1 + cos) / 2; d0 = 1 + alpha; d1 = -2 * cos; d2 = 1 - alpha;
                    break;
                case FilterKind.Band:
                    n0 = alpha; n1 = 0; n2 = -alpha; d0 = 1 + alpha; d1 = -2 * cos; d2 = 1 - alpha;
                    break;
                case FilterKind.Notch:
                    n0 = 1; n1 = -2 * cos; n2 = 1; d0 = 1 + alpha; d1 = -2 * cos; d2 = 1 - alpha;
                    break;
                case FilterKind.Peak:
                    n0 = 1 + alpha * amp; n1 = -2 * cos; n2 = 1 - alpha * amp; d0 = 1 + alpha / amp; d1 = -2 * cos; d2 = 1 - alpha / amp;
                    break;
                default:
                    n0 = (1 - cos) / 2; n1 = 1 - cos; n2 = (1 - cos) / 2; d0 = 1 + alpha; d1 = -2 * cos; d2 = 1 - alpha;
                    break;
            }

            b0 = n0 / d0;
            b1 = n1 / d0;
            b2 = n2 / d0;
            a1 = d1 / d0;
            a2 = d2 / d0;
        }

        public float Run(float x)
        {
            var y = b0 * x + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2;
            x2 = x1;
            x1 = x;
            y2 = y1;
            y1 = y;

            // Denormals and runaway are cut off: a filter swept hard must not explode.
            if (y > 8 || y < -8 || float.IsNaN(y))
            {
                y = 0;
                y1 = y2 = 0;
            }

            return y;
        }
    }

    /// <summary>A delay line with feedback (echoes), read at a fractional time.</summary>
    public class Delay
    {
        readonly float[] line;
        int at;

        public Delay(float maxSeconds)
        {
            line = new float[Synth.Samples(maxSeconds) + 2];
        }

        public float Read(float seconds)
        {
            var back = Math.Min(line.Length - 2, Math.Max(1, seconds * Synth.Rate));
            var whole = (int)back;
            var part = back - whole;
            var a = line[(at - whole + line.Length) % line.Length];
            var b = line[(at - whole - 1 + line.Length) % line.Length];

            return a + (b - a) * part;
        }

        public void Write(float x)
        {
            line[at] = x;
            at = (at + 1) % line.Length;
        }

        /// <summary>An echo: the input plus what came back, fed back.</summary>
        public float Echo(float x, float seconds, float feedback, float mix)
        {
            var back = Read(seconds);
            Write(x + back * feedback);

            return x + back * mix;
        }
    }

    /// <summary>A small reverb (Schroeder–Moorer: four combs in parallel, two allpasses in series), for halls, caves and the open air.</summary>
    public class Reverb
    {
        readonly float[][] combs;
        readonly int[] combAt;
        readonly float[] combStore;
        readonly float[][] passes;
        readonly int[] passAt;
        public float Feedback;
        public float Damp;

        static readonly int[] CombLengths = { 1557, 1617, 1491, 1422, 1277, 1356 };
        static readonly int[] PassLengths = { 556, 441, 341 };

        /// <summary>A room of a size (0.2 a small room, 0.9 a cave) and a dampness (0 bright stone, 0.6 soft wood).</summary>
        public Reverb(float size, float damp)
        {
            combs = new float[CombLengths.Length][];
            combAt = new int[CombLengths.Length];
            combStore = new float[CombLengths.Length];

            for (var i = 0; i < combs.Length; i++)
            {
                combs[i] = new float[CombLengths[i]];
            }

            passes = new float[PassLengths.Length][];
            passAt = new int[PassLengths.Length];

            for (var i = 0; i < passes.Length; i++)
            {
                passes[i] = new float[PassLengths[i]];
            }

            Feedback = 0.7f + 0.28f * Synth.Clamp01(size);
            Damp = Synth.Clamp01(damp);
        }

        public float Run(float x)
        {
            var wet = 0f;

            for (var i = 0; i < combs.Length; i++)
            {
                var line = combs[i];
                var y = line[combAt[i]];
                combStore[i] = y * (1 - Damp) + combStore[i] * Damp;
                line[combAt[i]] = x * 0.015f + combStore[i] * Feedback;
                combAt[i] = (combAt[i] + 1) % line.Length;
                wet += y;
            }

            for (var i = 0; i < passes.Length; i++)
            {
                var line = passes[i];
                var held = line[passAt[i]];
                var y = -wet + held;
                line[passAt[i]] = wet + held * 0.5f;
                passAt[i] = (passAt[i] + 1) % line.Length;
                wet = y;
            }

            return wet;
        }
    }

    /// <summary>A plucked string (Karplus–Strong with a fractional delay and a body): the guqin, the pipa, the zheng.</summary>
    public class Pluck
    {
        readonly float[] line;
        readonly float length;
        int at;
        float last;
        readonly float decay;
        readonly float brightness;

        public Pluck(float frequency, float seconds, float brightness, int seed)
        {
            length = Synth.Rate / Math.Max(20, frequency);
            line = new float[(int)length + 2];
            var dice = new Dice(seed);
            this.brightness = Synth.Clamp01(brightness);

            // The excitation: noise, softened for a dull pluck.
            var smooth = 0f;

            for (var i = 0; i < line.Length; i++)
            {
                var noise = dice.Bipolar();
                smooth = smooth + (noise - smooth) * (0.2f + 0.8f * this.brightness);
                line[i] = smooth;
            }

            // How much is kept each round so the note falls by 60 dB in `seconds`.
            var rounds = Math.Max(1, seconds * frequency);
            decay = (float)Math.Pow(0.001, 1.0 / rounds);
        }

        int played;
        static readonly int Ramp = Synth.Samples(0.004f);

        public float Next()
        {
            var whole = (int)length;
            var part = length - whole;
            var i0 = at;
            var i1 = (at + 1) % line.Length;
            var current = line[i0] * (1 - part) + line[i1] * part;
            // The loop filter: an average (the string's loss of highs), and the decay.
            var filtered = (current + last) * 0.5f * decay;
            var y = Synth.Lerp(filtered, current * decay, brightness * 0.3f);
            last = current;
            line[at] = y;
            at = (at + 1) % whole;

            // A few milliseconds of attack, so a pluck starts without a click.
            if (played < Ramp)
            {
                return y * (played++ / (float)Ramp);
            }

            return y;
        }
    }

    /// <summary>A struck body (a bell, a gong, a wooden block, a stone): damped sine modes at inharmonic ratios.</summary>
    public static class Modes
    {
        public static readonly (float ratio, float gain, float decay)[] Bell = { (0.56f, 0.6f, 1.0f), (0.92f, 0.5f, 0.8f), (1.19f, 0.4f, 0.65f), (1.71f, 0.3f, 0.5f), (2.0f, 0.35f, 0.45f), (2.74f, 0.2f, 0.35f), (3.0f, 0.15f, 0.3f), (3.76f, 0.1f, 0.22f) };
        public static readonly (float ratio, float gain, float decay)[] Gong = { (1f, 0.7f, 1f), (1.43f, 0.45f, 0.8f), (1.86f, 0.4f, 0.9f), (2.32f, 0.3f, 0.7f), (2.76f, 0.25f, 0.6f), (3.19f, 0.2f, 0.5f), (3.81f, 0.15f, 0.45f) };
        public static readonly (float ratio, float gain, float decay)[] Wood = { (1f, 0.8f, 0.12f), (2.57f, 0.4f, 0.06f), (4.21f, 0.2f, 0.04f) };
        public static readonly (float ratio, float gain, float decay)[] Stone = { (1f, 0.7f, 0.08f), (1.73f, 0.5f, 0.05f), (2.91f, 0.3f, 0.03f), (4.4f, 0.2f, 0.02f) };
        public static readonly (float ratio, float gain, float decay)[] Jade = { (1f, 0.6f, 0.9f), (2.76f, 0.4f, 0.5f), (5.4f, 0.25f, 0.3f), (8.93f, 0.12f, 0.2f) };

        /// <summary>The body struck at a fundamental: its sound at a time, each mode decaying over `seconds` × its own share.</summary>
        public static float At((float ratio, float gain, float decay)[] body, float fundamental, float t, float seconds)
        {
            var sum = 0f;

            foreach (var (ratio, gain, decay) in body)
            {
                sum += Synth.Sine(fundamental * ratio * t) * gain * Envelope.Strike(t, seconds * decay);
            }

            return sum * 0.5f;
        }
    }

    /// <summary>Sound held in memory: mono samples at the synth's rate, with the tools to mix and finish it.</summary>
    public class Wave
    {
        public readonly float[] Data;

        public Wave(float seconds)
        {
            Data = new float[Synth.Samples(seconds)];
        }

        public Wave(int samples)
        {
            Data = new float[Math.Max(1, samples)];
        }

        public int Length => Data.Length;
        public float Seconds => Data.Length / (float)Synth.Rate;

        /// <summary>Adds another buffer into this one at a time, at a gain.</summary>
        public void Mix(Wave other, float atSeconds, float gain = 1)
        {
            var start = (int)(atSeconds * Synth.Rate);

            for (var i = 0; i < other.Length && start + i < Length; i++)
            {
                if (start + i >= 0)
                {
                    Data[start + i] += other.Data[i] * gain;
                }
            }
        }

        public float Peak()
        {
            var peak = 0f;

            foreach (var x in Data)
            {
                peak = Math.Max(peak, Math.Abs(x));
            }

            return peak;
        }

        /// <summary>Scales it so its loudest point is at a level (and softly clips what would pass it).</summary>
        public Wave Normalise(float level = 0.89f)
        {
            var peak = Peak();

            if (peak > 0.00001f)
            {
                var gain = level / peak;

                for (var i = 0; i < Length; i++)
                {
                    Data[i] *= gain;
                }
            }

            return this;
        }

        /// <summary>Short fades at both ends so it starts and stops without a click.</summary>
        public Wave Fade(float inSeconds = 0.003f, float outSeconds = 0.02f)
        {
            var fadeIn = Math.Min(Length / 2, Synth.Samples(inSeconds));
            var fadeOut = Math.Min(Length / 2, Synth.Samples(outSeconds));

            for (var i = 0; i < fadeIn; i++)
            {
                Data[i] *= i / (float)fadeIn;
            }

            for (var i = 0; i < fadeOut; i++)
            {
                Data[Length - 1 - i] *= i / (float)fadeOut;
            }

            return this;
        }

        /// <summary>Makes a loop seamless: the tail is crossfaded into the head, and the buffer shortened by the overlap.</summary>
        public Wave Loop(float overlapSeconds)
        {
            var overlap = Math.Min(Length / 3, Synth.Samples(overlapSeconds));
            var loop = new float[Length - overlap];
            Array.Copy(Data, overlap, loop, 0, loop.Length);

            for (var i = 0; i < overlap; i++)
            {
                var k = i / (float)overlap;
                // Equal-power crossfade of the tail (which runs on into the start) with the head.
                loop[loop.Length - overlap + i] = loop[loop.Length - overlap + i] * (float)Math.Cos(k * Math.PI / 2) + Data[i] * (float)Math.Sin(k * Math.PI / 2);
            }

            var result = new Wave(loop.Length);
            Array.Copy(loop, result.Data, loop.Length);

            return result;
        }

        /// <summary>True if every sample is a number and none passes ±1.</summary>
        public bool Sound()
        {
            foreach (var x in Data)
            {
                if (float.IsNaN(x) || float.IsInfinity(x) || Math.Abs(x) > 1.0001f)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>The loudness (root mean square).</summary>
        public float Rms()
        {
            var sum = 0.0;

            foreach (var x in Data)
            {
                sum += x * x;
            }

            return (float)Math.Sqrt(sum / Math.Max(1, Length));
        }

        /// <summary>Through a reverb: the dry sound and the room's tail, as long as asked.</summary>
        public Wave Room(float size, float damp, float wet, float tailSeconds = 0)
        {
            var reverb = new Reverb(size, damp);
            var result = new Wave(Length + Synth.Samples(tailSeconds));

            for (var i = 0; i < result.Length; i++)
            {
                var dry = i < Length ? Data[i] : 0;
                result.Data[i] = dry + reverb.Run(dry) * wet;
            }

            return result;
        }

        /// <summary>The 16-bit little-endian PCM WAV of it (for the tests and for listening).</summary>
        public byte[] Wav()
        {
            var bytes = new byte[44 + Length * 2];

            void Text(int at, string text)
            {
                for (var i = 0; i < text.Length; i++)
                {
                    bytes[at + i] = (byte)text[i];
                }
            }

            void Int(int at, int value)
            {
                bytes[at] = (byte)value;
                bytes[at + 1] = (byte)(value >> 8);
                bytes[at + 2] = (byte)(value >> 16);
                bytes[at + 3] = (byte)(value >> 24);
            }

            void Short(int at, int value)
            {
                bytes[at] = (byte)value;
                bytes[at + 1] = (byte)(value >> 8);
            }

            Text(0, "RIFF");
            Int(4, 36 + Length * 2);
            Text(8, "WAVE");
            Text(12, "fmt ");
            Int(16, 16);
            Short(20, 1);
            Short(22, 1);
            Int(24, Synth.Rate);
            Int(28, Synth.Rate * 2);
            Short(32, 2);
            Short(34, 16);
            Text(36, "data");
            Int(40, Length * 2);

            for (var i = 0; i < Length; i++)
            {
                var sample = (int)Math.Round(Math.Max(-1, Math.Min(1, Data[i])) * 32767);
                Short(44 + i * 2, sample);
            }

            return bytes;
        }
    }
}
