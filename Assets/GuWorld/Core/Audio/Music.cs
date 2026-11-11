using System;
using System.Collections.Generic;

namespace GuWorld.Core.Audio
{
    /// <summary>What the music is for: the place, the hour, the danger.</summary>
    public enum Mood { Title, Valley, Night, Forest, Bai, Xiong, Hamlet, Cave, Combat, Boss, Sacred, Sorrow }

    /// <summary>
    /// Music made from rules, in the manner of the old Chinese instruments: a pentatonic mode for each
    /// mood, a guqin or a zheng that plays patterns, a dizi or a xiao that sings phrases (a question,
    /// then an answer), drums for the fights, a drone under the caves. A piece is some bars long and
    /// loops; the same mood and seed make the same piece.
    /// </summary>
    public static class Music
    {
        /// <summary>The five tones of each mode, in semitones from its root.</summary>
        static readonly Dictionary<string, int[]> Modes = new Dictionary<string, int[]>
        {
            ["gong"] = new[] { 0, 2, 4, 7, 9 },
            ["shang"] = new[] { 0, 2, 5, 7, 10 },
            ["jue"] = new[] { 0, 3, 5, 8, 10 },
            ["zhi"] = new[] { 0, 2, 5, 7, 9 },
            ["yu"] = new[] { 0, 3, 5, 7, 10 },
        };

        /// <summary>How each mood is played.</summary>
        public class Style
        {
            public string Mode;
            /// <summary>The root (MIDI).</summary>
            public int Root;
            public float Tempo;
            public int Bars = 8;
            /// <summary>Beats in a bar.</summary>
            public int Beats = 4;
            public bool Strings = true;
            public bool Flute;
            /// <summary>The flute is a xiao (low, breathy) rather than a dizi.</summary>
            public bool Low;
            public bool Drums;
            public bool Drone;
            public bool Bells;
            public bool Bowed;
            /// <summary>How much of the time the melody rests, 0–1.</summary>
            public float Space = 0.35f;
            /// <summary>The room it plays in: size and how much of it is heard.</summary>
            public float Room = 0.6f;
            public float Wet = 0.35f;
        }

        public static Style StyleOf(Mood mood)
        {
            switch (mood)
            {
                case Mood.Title: return new Style { Mode = "yu", Root = 50, Tempo = 58, Bars = 8, Flute = true, Low = true, Drone = true, Space = 0.45f, Room = 0.85f, Wet = 0.5f };
                case Mood.Valley: return new Style { Mode = "zhi", Root = 55, Tempo = 72, Bars = 8, Flute = true, Space = 0.4f };
                case Mood.Night: return new Style { Mode = "yu", Root = 52, Tempo = 54, Bars = 8, Flute = true, Low = true, Bells = true, Space = 0.6f, Room = 0.8f, Wet = 0.5f };
                case Mood.Forest: return new Style { Mode = "shang", Root = 53, Tempo = 66, Bars = 8, Flute = true, Space = 0.5f, Room = 0.7f };
                case Mood.Bai: return new Style { Mode = "jue", Root = 57, Tempo = 64, Bars = 8, Flute = true, Bells = true, Space = 0.45f, Room = 0.75f, Wet = 0.45f };
                case Mood.Xiong: return new Style { Mode = "gong", Root = 50, Tempo = 84, Bars = 8, Drums = true, Bowed = true, Space = 0.3f, Room = 0.5f };
                case Mood.Hamlet: return new Style { Mode = "gong", Root = 55, Tempo = 76, Bars = 8, Flute = true, Space = 0.35f, Room = 0.45f, Wet = 0.25f };
                case Mood.Cave: return new Style { Mode = "yu", Root = 45, Tempo = 50, Bars = 8, Strings = false, Drone = true, Bells = true, Space = 0.7f, Room = 0.95f, Wet = 0.6f };
                case Mood.Combat: return new Style { Mode = "shang", Root = 50, Tempo = 118, Bars = 8, Drums = true, Bowed = true, Space = 0.15f, Room = 0.45f, Wet = 0.25f };
                case Mood.Boss: return new Style { Mode = "jue", Root = 45, Tempo = 126, Bars = 8, Drums = true, Bowed = true, Drone = true, Space = 0.1f, Room = 0.6f, Wet = 0.3f };
                case Mood.Sacred: return new Style { Mode = "gong", Root = 48, Tempo = 46, Bars = 8, Strings = false, Drone = true, Bells = true, Flute = true, Low = true, Space = 0.6f, Room = 0.95f, Wet = 0.6f };
                default: return new Style { Mode = "yu", Root = 50, Tempo = 52, Bars = 8, Flute = true, Low = true, Space = 0.5f, Room = 0.8f, Wet = 0.45f };
            }
        }

        /// <summary>The note of a degree of the mode (degrees past five go up an octave).</summary>
        static int Note(Style style, int degree)
        {
            var tones = Modes[style.Mode];
            var octave = (int)Math.Floor(degree / 5.0);
            var step = ((degree % 5) + 5) % 5;

            return style.Root + octave * 12 + tones[step];
        }

        /// <summary>A note to play: when (beats), how long (beats), which (MIDI), how loud.</summary>
        public struct Event
        {
            public float Beat;
            public float Length;
            public int Midi;
            public float Velocity;
        }

        /// <summary>The score of a piece: the melody, the strings' pattern, the drums, the bells.</summary>
        public class Score
        {
            public Style Style;
            public readonly List<Event> Melody = new List<Event>();
            public readonly List<Event> Strings = new List<Event>();
            public readonly List<Event> Drums = new List<Event>();
            public readonly List<Event> Bells = new List<Event>();
            public readonly List<int> Chords = new List<int>();

            public float BeatSeconds => 60f / Style.Tempo;
            public float Beats => Style.Bars * Style.Beats;
            public float Seconds => Beats * BeatSeconds;
        }

        /// <summary>Writes the score of a piece for a mood: the same mood and seed give the same piece.</summary>
        public static Score Compose(Mood mood, int seed)
        {
            var style = StyleOf(mood);
            var score = new Score { Style = style };
            var dice = new Dice(seed * 7 + (int)mood * 131 + 1);

            // The harmony: a degree of the mode under each bar, moving by small steps, home at the end.
            var chord = 0;

            for (var bar = 0; bar < style.Bars; bar++)
            {
                if (bar == style.Bars - 1)
                {
                    chord = 0;
                }
                else if (bar > 0)
                {
                    chord = ((chord + new[] { 1, 2, 3, -1, -2 }[dice.Int(5)]) % 5 + 5) % 5;
                }

                score.Chords.Add(chord);
            }

            Melody(score, ref dice);
            Strings(score, ref dice);

            if (style.Drums)
            {
                Drums(score, ref dice, mood == Mood.Boss);
            }

            if (style.Bells)
            {
                for (var bar = 0; bar < style.Bars; bar += 2)
                {
                    score.Bells.Add(new Event { Beat = bar * style.Beats + dice.Int(style.Beats), Length = 4, Midi = Note(style, score.Chords[bar] + 10 + dice.Int(3)), Velocity = 0.35f });
                }
            }

            return score;
        }

        /// <summary>The melody: phrases of two bars; it moves mostly by steps, lands its long notes on the chord, rests where the style breathes.</summary>
        static void Melody(Score score, ref Dice dice)
        {
            var style = score.Style;
            var degree = 5 + dice.Int(3);
            var rhythms = style.Tempo > 100
                ? new[] { new[] { 1f, 0.5f, 0.5f, 1, 1 }, new[] { 0.5f, 0.5f, 0.5f, 0.5f, 2 }, new[] { 1.5f, 0.5f, 1, 1 } }
                : new[] { new[] { 2f, 1, 1 }, new[] { 1f, 1, 2 }, new[] { 1.5f, 0.5f, 2 }, new[] { 3f, 1 }, new[] { 1f, 0.5f, 0.5f, 2 } };

            for (var phrase = 0; phrase < style.Bars; phrase += 2)
            {
                for (var bar = 0; bar < 2; bar++)
                {
                    var rhythm = rhythms[dice.Int(rhythms.Length)];
                    var beat = (float)(phrase + bar) * style.Beats;

                    foreach (var length in rhythm)
                    {
                        // Steps more than leaps; leaps come back.
                        var move = dice.Chance(0.7f) ? (dice.Chance(0.5f) ? 1 : -1) : (dice.Chance(0.5f) ? 2 : -2) * (dice.Chance(0.3f) ? 2 : 1);
                        degree = Math.Max(3, Math.Min(12, degree + move));

                        // Long notes land on the bar's chord tones.
                        if (length >= 2)
                        {
                            var chordTone = score.Chords[phrase + bar] + 5 + (dice.Chance(0.5f) ? 0 : 2);

                            if (Math.Abs(chordTone - degree) <= 2)
                            {
                                degree = chordTone;
                            }
                        }

                        var rest = dice.Chance(style.Space) && length < 2;

                        if (!rest)
                        {
                            score.Melody.Add(new Event { Beat = beat, Length = length * 0.95f, Midi = Note(style, degree) + 12, Velocity = dice.Range(0.55f, 0.85f) });
                        }

                        beat += length;
                    }
                }
            }

            // The last note goes home.
            if (score.Melody.Count > 0)
            {
                var last = score.Melody[score.Melody.Count - 1];
                last.Midi = Note(style, 5) + 12;
                last.Length = Math.Max(last.Length, 2);
                score.Melody[score.Melody.Count - 1] = last;
            }
        }

        /// <summary>The strings: a pattern over the chord of each bar (an arpeggio for calm, an ostinato for fights).</summary>
        static void Strings(Score score, ref Dice dice)
        {
            var style = score.Style;

            if (!style.Strings && !style.Drone)
            {
                return;
            }

            var fight = style.Tempo > 100;
            var pattern = fight ? new[] { 0, 0, 2, 0, 3, 0, 2, 0 } : dice.Chance(0.5f) ? new[] { 0, 2, 4, 2 } : new[] { 0, 4, 2, 5 };
            var step = fight ? 0.5f : 1f;

            for (var bar = 0; bar < style.Bars; bar++)
            {
                var root = score.Chords[bar];

                if (style.Drone)
                {
                    score.Strings.Add(new Event { Beat = bar * style.Beats, Length = style.Beats, Midi = Note(style, root) - 12, Velocity = -1 });
                }

                if (!style.Strings)
                {
                    continue;
                }

                for (var k = 0; k * step < style.Beats; k++)
                {
                    var degree = root + pattern[k % pattern.Length];
                    score.Strings.Add(new Event { Beat = bar * style.Beats + k * step, Length = step * 3, Midi = Note(style, degree), Velocity = k == 0 ? 0.8f : dice.Range(0.4f, 0.6f) });
                }
            }
        }

        /// <summary>The drums: a big drum on the strong beats, a small one between, a run into each fourth bar.</summary>
        static void Drums(Score score, ref Dice dice, bool boss)
        {
            var style = score.Style;

            for (var bar = 0; bar < style.Bars; bar++)
            {
                for (var beat = 0; beat < style.Beats; beat++)
                {
                    var at = bar * style.Beats + beat;

                    if (beat == 0 || (boss && beat == 2))
                    {
                        score.Drums.Add(new Event { Beat = at, Midi = 0, Velocity = 1 });
                    }
                    else
                    {
                        score.Drums.Add(new Event { Beat = at, Midi = 1, Velocity = dice.Range(0.4f, 0.7f) });
                    }

                    if (dice.Chance(0.3f))
                    {
                        score.Drums.Add(new Event { Beat = at + 0.5f, Midi = 1, Velocity = 0.35f });
                    }
                }

                if (bar % 4 == 3)
                {
                    for (var k = 0; k < 4; k++)
                    {
                        score.Drums.Add(new Event { Beat = bar * style.Beats + style.Beats - 1 + k * 0.25f, Midi = 1, Velocity = 0.5f + k * 0.12f });
                    }
                }
            }
        }

        // ================================================================== Playing a score

        /// <summary>Plays a score into a buffer that loops seamlessly.</summary>
        public static Wave Render(Score score)
        {
            var style = score.Style;
            var beat = score.BeatSeconds;
            // A tail past the end, folded back onto the start: what rings over the loop's seam is heard at its beginning.
            var tail = 3f;
            var mix = new Wave(score.Seconds + tail);

            foreach (var note in score.Strings)
            {
                var sound = note.Velocity < 0 ? Drone(note.Midi, note.Length * beat) : Zheng(note.Midi, note.Length * beat, note.Velocity, note.Midi * 31 + (int)(note.Beat * 7));
                mix.Mix(sound, note.Beat * beat, note.Velocity < 0 ? 0.22f : 0.3f * Math.Abs(note.Velocity));
            }

            foreach (var note in score.Melody)
            {
                var sound = style.Bowed ? Erhu(note.Midi, note.Length * beat, note.Velocity) : style.Flute ? Flute(note.Midi, note.Length * beat, note.Velocity, style.Low) : Guqin(note.Midi, note.Length * beat, note.Velocity, note.Midi);
                mix.Mix(sound, note.Beat * beat, 0.4f);
            }

            foreach (var hit in score.Drums)
            {
                mix.Mix(Drum(hit.Midi == 0, hit.Velocity, (int)(hit.Beat * 4)), hit.Beat * beat, 0.5f);
            }

            foreach (var bell in score.Bells)
            {
                mix.Mix(Bell(bell.Midi), bell.Beat * beat, 0.2f);
            }

            var roomed = mix.Room(style.Room, 0.35f, style.Wet, 0);
            var loop = new Wave(Synth.Samples(score.Seconds));

            for (var i = 0; i < roomed.Length; i++)
            {
                loop.Data[i % loop.Length] += roomed.Data[i];
            }

            return loop.Normalise(0.75f);
        }

        public static Wave Make(Mood mood, int seed) => Render(Compose(mood, seed));

        // ================================================================== The instruments

        /// <summary>The guqin: a low plucked string with a long, woody decay and a slight slide into the note.</summary>
        static Wave Guqin(int midi, float seconds, float velocity, int seed)
        {
            var length = Math.Max(1.2f, seconds + 1.5f);
            var b = new Wave(length);
            var string_ = new Pluck(Synth.Hz(midi - 12), length * 1.3f, 0.35f + 0.3f * velocity, seed);
            var body = new Biquad(FilterKind.Peak, 220, 1.5f, 6);

            for (var i = 0; i < b.Length; i++)
            {
                b.Data[i] = body.Run(string_.Next()) * velocity;
            }

            return b;
        }

        /// <summary>The zheng: a brighter pluck, shorter.</summary>
        static Wave Zheng(int midi, float seconds, float velocity, int seed)
        {
            var length = Math.Max(0.6f, seconds + 0.8f);
            var b = new Wave(length);
            var string_ = new Pluck(Synth.Hz(midi), length, 0.7f, seed);
            var body = new Biquad(FilterKind.Peak, 900, 1.2f, 4);

            for (var i = 0; i < b.Length; i++)
            {
                b.Data[i] = body.Run(string_.Next()) * velocity;
            }

            return b;
        }

        /// <summary>The dizi (bright) or the xiao (low, breathy): a soft tone with breath, vibrato that comes in late.</summary>
        static Wave Flute(int midi, float seconds, float velocity, bool low)
        {
            var length = seconds + 0.25f;
            var b = new Wave(length);
            var frequency = Synth.Hz(low ? midi - 12 : midi);
            var noise = new Noise(midi * 13 + (int)(seconds * 100));
            var breathFilter = new Biquad(FilterKind.Band, frequency * 2, 3);
            var tone = new OnePole(frequency * (low ? 3 : 6));
            var envelope = new Envelope(low ? 0.12f : 0.06f, 0.2f, 0.75f, 0.2f);
            var phase = 0f;

            for (var i = 0; i < b.Length; i++)
            {
                var t = i / (float)Synth.Rate;
                var vibrato = 1 + 0.006f * Synth.Clamp01((t - 0.35f) * 2) * Synth.Sine(5.2f * t);
                phase += frequency * vibrato / Synth.Rate;
                var body = Synth.Sine(phase) * 0.8f + Synth.Triangle(phase * 2) * (low ? 0.05f : 0.15f);
                var breath = breathFilter.Run(noise.White()) * (low ? 0.35f : 0.18f);
                b.Data[i] = tone.Low(body + breath) * envelope.At(t, seconds) * velocity;
            }

            return b;
        }

        /// <summary>The erhu: a bowed string (a saw through the body's formants) that swells and slides.</summary>
        static Wave Erhu(int midi, float seconds, float velocity)
        {
            var length = seconds + 0.2f;
            var b = new Wave(length);
            var frequency = Synth.Hz(midi);
            var f1 = new Biquad(FilterKind.Band, 800, 2);
            var f2 = new Biquad(FilterKind.Band, 1800, 3);
            var envelope = new Envelope(0.08f, 0.1f, 0.85f, 0.15f);
            var phase = 0f;

            for (var i = 0; i < b.Length; i++)
            {
                var t = i / (float)Synth.Rate;
                var slide = 1 - 0.03f * (float)Math.Exp(-t * 25);
                var vibrato = 1 + 0.01f * Synth.Clamp01(t * 2 - 0.3f) * Synth.Sine(6 * t);
                phase += frequency * slide * vibrato / Synth.Rate;
                var source = Synth.Saw(phase);
                b.Data[i] = (f1.Run(source) + f2.Run(source) * 0.6f) * envelope.At(t, seconds) * velocity * 1.4f;
            }

            return b;
        }

        /// <summary>A low drone of two detuned saws, darkened.</summary>
        static Wave Drone(int midi, float seconds)
        {
            var b = new Wave(seconds + 0.6f);
            var frequency = Synth.Hz(midi);
            var low = new Biquad(FilterKind.Low, frequency * 4, 0.8f);
            var envelope = new Envelope(0.6f, 0.2f, 1, 0.6f);

            for (var i = 0; i < b.Length; i++)
            {
                var t = i / (float)Synth.Rate;
                var sound = Synth.Saw(frequency * t) + Synth.Saw(frequency * 1.004f * t) + Synth.Sine(frequency * 0.5f * t);
                b.Data[i] = low.Run(sound * 0.3f) * envelope.At(t, seconds);
            }

            return b;
        }

        /// <summary>The big drum (a membrane: a falling low tone and a thud) or the small one.</summary>
        static Wave Drum(bool big, float velocity, int seed)
        {
            var b = new Wave(big ? 0.9f : 0.25f);
            var noise = new Noise(4000 + seed);
            var low = new Biquad(FilterKind.Low, big ? 400 : 2200, 0.8f);

            for (var i = 0; i < b.Length; i++)
            {
                var t = i / (float)Synth.Rate;
                var pitch = big ? 62 + 40 * (float)Math.Exp(-t * 18) : 180 + 80 * (float)Math.Exp(-t * 30);
                var skin = Synth.Sine(pitch * t) * Envelope.Strike(t, big ? 0.7f : 0.12f);
                var slap = low.Run(noise.White()) * Envelope.Strike(t, big ? 0.06f : 0.04f);
                b.Data[i] = Synth.Soft((skin + slap * 0.8f) * 1.6f) * velocity;
            }

            return b;
        }

        static Wave Bell(int midi)
        {
            var b = new Wave(3);
            var frequency = Synth.Hz(midi);

            for (var i = 0; i < b.Length; i++)
            {
                var t = i / (float)Synth.Rate;
                b.Data[i] = Audio.Modes.At(Audio.Modes.Bell, frequency, t, 2.6f);
            }

            return b;
        }

        /// <summary>The mood for where the hero is and what is happening.</summary>
        public static Mood MoodOf(string settlementClan, bool cave, bool night, bool fighting, bool boss, bool sacred, bool forest)
        {
            if (boss)
            {
                return Mood.Boss;
            }

            if (fighting)
            {
                return Mood.Combat;
            }

            if (sacred)
            {
                return Mood.Sacred;
            }

            if (cave)
            {
                return Mood.Cave;
            }

            switch (settlementClan)
            {
                case "bai": return Mood.Bai;
                case "xiong": return Mood.Xiong;
                case "mortal":
                case "wanderers": return Mood.Hamlet;
            }

            if (night)
            {
                return Mood.Night;
            }

            return forest ? Mood.Forest : Mood.Valley;
        }
    }
}
