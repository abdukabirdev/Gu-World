using System;
using System.Collections.Generic;

namespace GuWorld.Core.Audio
{
    /// <summary>
    /// Every sound effect of the game, made from the synth: blows and casts (one voice for each path
    /// of Gu), steps on each kind of ground, the voices of beasts, the interface, the world's events.
    /// A sound has variants (a seed each), so twenty footsteps do not sound like one.
    /// </summary>
    public static class Sounds
    {
        /// <summary>How many variants each sound has.</summary>
        public const int Variants = 4;

        static readonly Dictionary<string, Func<int, Wave>> recipes = new Dictionary<string, Func<int, Wave>>
        {
            // ---------------------------------------------------------------- Fighting
            ["palm"] = v => Palm(v, 1),
            ["palm_heavy"] = v => Palm(v, 1.8f),
            ["whoosh"] = Whoosh,
            ["roll"] = v => Whoosh(v + 17).Normalise(0.5f),
            ["hurt"] = Hurt,
            ["block"] = v => Struck(Modes.Stone, 380 + 40 * v, 0.25f, v, 0.35f),
            ["ward"] = v => Struck(Modes.Jade, 900 + 60 * v, 1.2f, v, 0.2f).Room(0.5f, 0.3f, 0.6f, 0.5f).Normalise(0.6f),
            ["death"] = Death,
            ["enemy_die"] = v => Thud(v, 70, 0.5f),
            ["loot"] = Loot,
            ["coin"] = Coin,

            // ---------------------------------------------------------------- The paths of Gu, cast
            ["cast_moon"] = v => Shimmer(v, 72 + v, 0.7f, 1.0f),
            ["cast_light"] = v => Shimmer(v, 79 + v, 0.5f, 0.7f),
            ["cast_blood"] = v => Shimmer(v, 60 - v, 0.9f, 1.1f, true),
            ["cast_fire"] = Fire,
            ["cast_ice"] = Ice,
            ["cast_water"] = Water,
            ["cast_wind"] = Wind,
            ["cast_lightning"] = Zap,
            ["cast_earth"] = v => Thud(v, 55, 0.7f, true),
            ["cast_strength"] = v => Thud(v, 48, 0.6f),
            ["cast_wood"] = Rustle,
            ["cast_poison"] = Hiss,
            ["cast_sound"] = v => Struck(Modes.Gong, 110 + 15 * v, 2.4f, v, 0.4f).Room(0.6f, 0.2f, 0.5f, 1).Normalise(0.8f),
            ["cast_dark"] = Dark,
            ["cast_bone"] = Crack,
            ["cast_wine"] = Glug,
            ["heal"] = v => Chord(v, new[] { 67f, 71, 74, 79 }, 1.6f, 0.08f),
            ["buff"] = v => Chord(v, new[] { 62f, 69, 74 }, 1.0f, 0.03f),

            // ---------------------------------------------------------------- Steps
            ["step_grass"] = v => Step(v, 1800, 0.6f, 0.07f),
            ["step_stone"] = v => Step(v, 3200, 0.35f, 0.04f, Modes.Stone),
            ["step_wood"] = v => Step(v, 1200, 0.4f, 0.06f, Modes.Wood),
            ["step_gravel"] = v => Step(v, 4200, 1f, 0.1f),
            ["step_mud"] = v => Step(v, 600, 0.8f, 0.12f),
            ["step_water"] = Splash,
            ["land"] = v => Thud(v, 60, 0.25f),

            // ---------------------------------------------------------------- Beasts
            ["howl"] = Howl,
            ["growl"] = Growl,
            ["roar"] = Roar,
            ["hiss"] = v => Hiss(v + 31),
            ["squeak"] = Squeak,
            ["screech"] = Screech,
            ["skitter"] = Skitter,
            ["buzz"] = Buzz,
            ["croak"] = Croak,
            ["wisp"] = v => Shimmer(v, 84, 0.3f, 0.6f),

            // ---------------------------------------------------------------- The interface and the world's events
            ["click"] = v => Struck(Modes.Wood, 1800 + 80 * v, 0.05f, v, 0.05f).Normalise(0.4f),
            ["open"] = v => Struck(Modes.Wood, 700 + 50 * v, 0.12f, v, 0.1f).Normalise(0.45f),
            ["close"] = v => Struck(Modes.Wood, 520 + 40 * v, 0.1f, v, 0.08f).Normalise(0.4f),
            ["error"] = v => Struck(Modes.Wood, 220, 0.12f, v, 0.1f).Normalise(0.5f),
            ["quest"] = v => Struck(Modes.Gong, 98, 3f, v, 0.5f).Room(0.7f, 0.25f, 0.6f, 1.5f).Normalise(0.85f),
            ["quest_done"] = v => Chord(v, new[] { 55f, 62, 67, 74, 79 }, 2.4f, 0.12f),
            ["breakthrough"] = Breakthrough,
            ["lantern"] = v => Struck(Modes.Bell, 620, 2.8f, v, 0.4f).Room(0.6f, 0.3f, 0.5f, 1.2f).Normalise(0.7f),
            ["door"] = v => Thud(v, 40, 1.2f, true),
            ["chest"] = v => Struck(Modes.Wood, 300, 0.4f, v, 0.2f).Normalise(0.6f),
            ["pick"] = v => Rustle(v + 7).Normalise(0.4f),
            ["thunder"] = Thunder,
            ["brew"] = Brew,
            ["pray"] = v => Struck(Modes.Bell, 440, 4f, v, 0.3f).Room(0.8f, 0.3f, 0.6f, 2).Normalise(0.7f),
        };

        public static IEnumerable<string> Ids => recipes.Keys;

        public static bool Has(string id) => recipes.ContainsKey(id);

        /// <summary>Makes a sound (a variant of it); null for an unknown id.</summary>
        public static Wave Make(string id, int variant = 0)
        {
            if (!recipes.TryGetValue(id, out var make))
            {
                return null;
            }

            var sound = make(Math.Abs(variant) % Variants);

            return sound.Fade().Normalise(Math.Min(0.89f, sound.Peak()));
        }

        /// <summary>The sound a Gu of a path makes when cast.</summary>
        public static string CastOf(GuPath path)
        {
            switch (path)
            {
                case GuPath.Moon: return "cast_moon";
                case GuPath.Light: return "cast_light";
                case GuPath.Blood: return "cast_blood";
                case GuPath.Fire: return "cast_fire";
                case GuPath.Ice: return "cast_ice";
                case GuPath.Water: return "cast_water";
                case GuPath.Wind: return "cast_wind";
                case GuPath.Lightning: return "cast_lightning";
                case GuPath.Earth: return "cast_earth";
                case GuPath.Strength: return "cast_strength";
                case GuPath.Wood: return "cast_wood";
                case GuPath.Poison: return "cast_poison";
                case GuPath.Sound: return "cast_sound";
                case GuPath.Dark: return "cast_dark";
                case GuPath.Bone: return "cast_bone";
                default: return "cast_wine";
            }
        }

        /// <summary>The voice of a beast by its body and look.</summary>
        public static string VoiceOf(EnemyDef enemy)
        {
            switch (enemy.Body)
            {
                case EnemyBody.Canid: return enemy.Look == "bear" ? "roar" : enemy.Look == "boar" ? "growl" : "howl";
                case EnemyBody.Serpent: return "hiss";
                case EnemyBody.Bug:
                    switch (enemy.Look)
                    {
                        case "bat": return "squeak";
                        case "eagle": return "screech";
                        case "spider":
                        case "spider_queen": return "skitter";
                        case "bee": return "buzz";
                        case "toad": return "croak";
                        case "wisp": return "wisp";
                        default: return "buzz";
                    }
                default: return "growl";
            }
        }

        // ================================================================== Recipes

        /// <summary>A palm strike: a thump of the body and the slap of the hand, harder for heavy blows.</summary>
        static Wave Palm(int v, float weight)
        {
            var b = new Wave(0.25f);
            var noise = new Noise(100 + v);
            var low = new Biquad(FilterKind.Low, 900 + 300 * v, 0.7f);
            var band = new Biquad(FilterKind.Band, 2400 + 200 * v, 1.2f);

            for (var i = 0; i < b.Length; i++)
            {
                var t = i / (float)Synth.Rate;
                var body = Synth.Sine((90 + 30 / weight) * t * (1 - t * 2)) * Envelope.Strike(t, 0.09f * weight);
                var slap = band.Run(noise.White()) * Envelope.Strike(t, 0.035f);
                b.Data[i] = low.Run(body * weight + slap * 1.2f);
            }

            return b;
        }

        static Wave Whoosh(int v)
        {
            var b = new Wave(0.32f);
            var noise = new Noise(200 + v);
            var band = new Biquad(FilterKind.Band, 400, 2);

            for (var i = 0; i < b.Length; i++)
            {
                var t = i / (float)b.Length;
                band.Set(FilterKind.Band, 300 + 2400 * (float)Math.Sin(t * Math.PI) * (0.8f + 0.1f * v), 2.5f);
                b.Data[i] = band.Run(noise.Pink() * 4) * (float)Math.Sin(t * Math.PI);
            }

            return b;
        }

        static Wave Hurt(int v)
        {
            var b = new Wave(0.3f);
            var noise = new Noise(300 + v);
            var low = new Biquad(FilterKind.Low, 1400, 1);

            for (var i = 0; i < b.Length; i++)
            {
                var t = i / (float)Synth.Rate;
                var hit = low.Run(noise.White()) * Envelope.Strike(t, 0.06f);
                var grunt = Synth.Soft(Synth.Saw((140 - 40 * t - 8 * v) * t) * 2) * Envelope.Strike(t, 0.18f, 0.01f) * 0.35f;
                b.Data[i] = hit + grunt;
            }

            return b;
        }

        static Wave Death(int v)
        {
            var b = new Wave(1.6f);

            for (var i = 0; i < b.Length; i++)
            {
                var t = i / (float)Synth.Rate;
                var fall = Synth.Sine(Synth.Lerp(220, 55, Synth.Clamp01(t / 1.4f)) * t) * Envelope.Strike(t, 1.4f, 0.05f);
                b.Data[i] = fall * 0.6f + Modes.At(Modes.Gong, 73, t, 1.5f) * 0.4f;
            }

            return b.Room(0.8f, 0.4f, 0.6f, 1);
        }

        static Wave Thud(int v, float pitch, float seconds, bool rumble = false)
        {
            var b = new Wave(seconds + 0.1f);
            var noise = new Noise(400 + v);
            var low = new Biquad(FilterKind.Low, 280 + 30 * v, 0.9f);

            for (var i = 0; i < b.Length; i++)
            {
                var t = i / (float)Synth.Rate;
                var drop = Synth.Sine(pitch * (1 + 1.5f * (float)Math.Exp(-t * 30)) * t) * Envelope.Strike(t, seconds * 0.6f);
                var dirt = low.Run(noise.Brown() + (rumble ? noise.White() * 0.3f : 0)) * Envelope.Strike(t, seconds, 0.005f);
                b.Data[i] = Synth.Soft((drop + dirt * 1.4f) * 1.4f);
            }

            return b;
        }

        static Wave Loot(int v)
        {
            var b = new Wave(0.6f);

            foreach (var (note, at) in new[] { (84f + v, 0f), (88f + v, 0.07f), (91f + v, 0.14f) })
            {
                var one = new Wave(0.45f);

                for (var i = 0; i < one.Length; i++)
                {
                    var t = i / (float)Synth.Rate;
                    one.Data[i] = Modes.At(Modes.Jade, Synth.Hz(note), t, 0.35f);
                }

                b.Mix(one, at, 0.5f);
            }

            return b;
        }

        static Wave Coin(int v)
        {
            var b = new Wave(0.4f);

            for (var i = 0; i < b.Length; i++)
            {
                var t = i / (float)Synth.Rate;
                b.Data[i] = Modes.At(Modes.Bell, 2400 + 150 * v, t, 0.3f) + (t > 0.06f ? Modes.At(Modes.Bell, 2900 + 150 * v, t - 0.06f, 0.25f) * 0.6f : 0);
            }

            return b;
        }

        /// <summary>A shimmer: a cluster of detuned sines that bloom and fade, with a rising sweep (the moon's light).</summary>
        static Wave Shimmer(int v, float midi, float seconds, float brightness, bool dark = false)
        {
            var b = new Wave(seconds + 0.4f);
            var dice = new Dice(500 + v);
            var detunes = new float[6];

            for (var k = 0; k < detunes.Length; k++)
            {
                detunes[k] = dice.Range(-0.6f, 0.6f) + (k % 3) * 12 * (k < 3 ? 1 : 0.5f);
            }

            var noise = new Noise(510 + v);
            var high = new Biquad(FilterKind.High, 3000, 0.7f);

            for (var i = 0; i < b.Length; i++)
            {
                var t = i / (float)Synth.Rate;
                var bloom = Envelope.Strike(t, seconds, 0.04f);
                var sum = 0f;

                foreach (var detune in detunes)
                {
                    var f = Synth.Hz(midi + detune + 2 * Synth.Clamp01(t / seconds));
                    sum += (dark ? Synth.Triangle(f * t) : Synth.Sine(f * t)) / detunes.Length;
                }

                var sparkle = high.Run(noise.White()) * brightness * 0.3f * Envelope.Strike(t, seconds * 0.4f, 0.01f);
                b.Data[i] = (sum + sparkle) * bloom;
            }

            return b.Room(0.6f, 0.2f, 0.45f, 0.5f);
        }

        static Wave Fire(int v)
        {
            var b = new Wave(0.9f);
            var noise = new Noise(600 + v);
            var low = new Biquad(FilterKind.Low, 900, 0.8f);
            var dice = new Dice(601 + v);
            var crackle = 0f;

            for (var i = 0; i < b.Length; i++)
            {
                var t = i / (float)Synth.Rate;
                low.Set(FilterKind.Low, 500 + 2500 * Envelope.Strike(t, 0.4f), 0.8f);
                var roar = low.Run(noise.Pink() * 3) * Envelope.Strike(t, 0.8f, 0.05f);

                if (dice.Chance(0.0016f))
                {
                    crackle = 1;
                }

                crackle *= 0.985f;
                b.Data[i] = roar + noise.White() * crackle * 0.4f;
            }

            return b;
        }

        static Wave Ice(int v)
        {
            var b = new Wave(0.7f);
            var dice = new Dice(700 + v);
            var noise = new Noise(701 + v);
            var high = new Biquad(FilterKind.High, 5000, 1);

            for (var shard = 0; shard < 7; shard++)
            {
                var at = dice.Range(0, 0.25f);
                var pitch = dice.Range(2500, 6000);
                var one = new Wave(0.4f);

                for (var i = 0; i < one.Length; i++)
                {
                    var t = i / (float)Synth.Rate;
                    one.Data[i] = Modes.At(Modes.Jade, pitch, t, 0.2f);
                }

                b.Mix(one, at, 0.3f);
            }

            for (var i = 0; i < b.Length; i++)
            {
                var t = i / (float)Synth.Rate;
                b.Data[i] += high.Run(noise.White()) * Envelope.Strike(t, 0.3f, 0.01f) * 0.5f;
            }

            return b;
        }

        static Wave Water(int v)
        {
            var b = Splash(v + 13);
            var bubble = new Wave(0.5f);
            var dice = new Dice(800 + v);

            for (var k = 0; k < 5; k++)
            {
                var at = dice.Range(0.05f, 0.35f);
                var pitch = dice.Range(400, 1100);
                var one = new Wave(0.12f);

                for (var i = 0; i < one.Length; i++)
                {
                    var t = i / (float)Synth.Rate;
                    one.Data[i] = Synth.Sine(pitch * (1 + t * 6) * t) * Envelope.Strike(t, 0.08f);
                }

                bubble.Mix(one, at, 0.3f);
            }

            b.Mix(bubble, 0.05f);

            return b;
        }

        static Wave Wind(int v)
        {
            var b = new Wave(0.8f);
            var noise = new Noise(900 + v);
            var band = new Biquad(FilterKind.Band, 600, 4);

            for (var i = 0; i < b.Length; i++)
            {
                var t = i / (float)b.Length;
                band.Set(FilterKind.Band, 500 + 1800 * t * (1 - t) * 4 + 100 * v, 5);
                b.Data[i] = band.Run(noise.Pink() * 6) * (float)Math.Sin(t * Math.PI);
            }

            return b;
        }

        static Wave Zap(int v)
        {
            var b = new Wave(0.5f);
            var dice = new Dice(1000 + v);
            var noise = new Noise(1001 + v);
            var band = new Biquad(FilterKind.Band, 3000, 0.8f);
            var level = 0f;

            for (var i = 0; i < b.Length; i++)
            {
                var t = i / (float)Synth.Rate;

                // Lightning is jagged: random jumps of level, a buzz under them.
                if (dice.Chance(0.004f))
                {
                    level = dice.Range(0.4f, 1);
                }

                level *= 0.9995f;
                var buzz = Synth.Square(120 * t) * 0.3f + Synth.Saw((900 + 400 * v) * t) * 0.2f;
                b.Data[i] = Synth.Soft((band.Run(noise.White()) * level * 2 + buzz * level) * 1.5f) * Envelope.Strike(t, 0.45f, 0.001f);
            }

            return b;
        }

        static Wave Rustle(int v)
        {
            var b = new Wave(0.5f);
            var noise = new Noise(1100 + v);
            var high = new Biquad(FilterKind.High, 2500, 0.7f);
            var dice = new Dice(1101 + v);
            var gate = 0f;

            for (var i = 0; i < b.Length; i++)
            {
                var t = i / (float)Synth.Rate;

                if (dice.Chance(0.003f))
                {
                    gate = 1;
                }

                gate *= 0.997f;
                b.Data[i] = high.Run(noise.White()) * gate * Envelope.Strike(t, 0.45f, 0.02f);
            }

            return b;
        }

        static Wave Hiss(int v)
        {
            var b = new Wave(0.6f);
            var noise = new Noise(1200 + v);
            var band = new Biquad(FilterKind.Band, 5500, 2);

            for (var i = 0; i < b.Length; i++)
            {
                var t = i / (float)Synth.Rate;
                var shape = Synth.Clamp01(t / 0.05f) * Envelope.Strike(t, 0.55f, 0.04f);
                b.Data[i] = band.Run(noise.White() * 2) * shape;
            }

            return b;
        }

        static Wave Dark(int v)
        {
            var b = new Wave(1.1f);
            var noise = new Noise(1300 + v);
            var low = new Biquad(FilterKind.Low, 400, 3);

            for (var i = 0; i < b.Length; i++)
            {
                var t = i / (float)Synth.Rate;
                low.Set(FilterKind.Low, 200 + 600 * (float)Math.Sin(t * 6), 4);
                var drone = Synth.Saw(41 * t) * 0.4f + Synth.Saw(41.6f * t) * 0.4f;
                b.Data[i] = low.Run(drone + noise.Brown() * 0.5f) * Envelope.Strike(t, 1, 0.15f);
            }

            return b.Room(0.8f, 0.5f, 0.5f, 0.5f);
        }

        static Wave Crack(int v)
        {
            var b = new Wave(0.3f);
            var noise = new Noise(1400 + v);
            var band = new Biquad(FilterKind.Band, 1800 + 300 * v, 3);

            for (var i = 0; i < b.Length; i++)
            {
                var t = i / (float)Synth.Rate;
                b.Data[i] = band.Run(noise.White() * 3) * Envelope.Strike(t, 0.05f) + Modes.At(Modes.Wood, 900 + 100 * v, t, 0.15f) * 0.6f;
            }

            return b;
        }

        static Wave Glug(int v)
        {
            var b = new Wave(0.6f);

            for (var k = 0; k < 4; k++)
            {
                var one = new Wave(0.12f);

                for (var i = 0; i < one.Length; i++)
                {
                    var t = i / (float)Synth.Rate;
                    one.Data[i] = Synth.Sine((180 + 60 * k + 20 * v) * (1 + t * 8) * t) * Envelope.Strike(t, 0.1f);
                }

                b.Mix(one, k * 0.12f, 0.6f);
            }

            return b;
        }

        /// <summary>A chord of soft plucks rolled upwards (healing, quests done).</summary>
        static Wave Chord(int v, float[] notes, float seconds, float roll)
        {
            var b = new Wave(seconds + roll * notes.Length + 0.2f);

            for (var k = 0; k < notes.Length; k++)
            {
                var string_ = new Pluck(Synth.Hz(notes[k] + (v % 2 == 0 ? 0 : 2)), seconds, 0.5f, 1500 + v * 7 + k);
                var one = new Wave(seconds);

                for (var i = 0; i < one.Length; i++)
                {
                    one.Data[i] = string_.Next();
                }

                b.Mix(one, k * roll, 0.5f);
            }

            return b.Room(0.6f, 0.3f, 0.35f, 0.6f);
        }

        static Wave Struck((float ratio, float gain, float decay)[] body, float fundamental, float seconds, int v, float noiseShare)
        {
            var b = new Wave(seconds + 0.05f);
            var noise = new Noise(1600 + v);
            var low = new OnePole(fundamental * 4);

            for (var i = 0; i < b.Length; i++)
            {
                var t = i / (float)Synth.Rate;
                b.Data[i] = Modes.At(body, fundamental, t, seconds) + low.Low(noise.White()) * noiseShare * Envelope.Strike(t, 0.02f);
            }

            return b;
        }

        static Wave Step(int v, float colour, float grit, float seconds, (float ratio, float gain, float decay)[] body = null)
        {
            var b = new Wave(seconds + 0.05f);
            var noise = new Noise(1700 + v);
            var band = new Biquad(FilterKind.Band, colour * (0.9f + 0.07f * v), 1.1f);
            var low = new Biquad(FilterKind.Low, 180, 0.8f);

            for (var i = 0; i < b.Length; i++)
            {
                var t = i / (float)Synth.Rate;
                var scuff = band.Run(noise.White()) * grit * Envelope.Strike(t, seconds, 0.004f);
                var heel = low.Run(noise.White()) * Envelope.Strike(t, 0.03f) * 2;
                var knock = body != null ? Modes.At(body, colour * 0.25f, t, seconds) * 0.6f : 0;
                b.Data[i] = scuff + heel + knock;
            }

            return b;
        }

        static Wave Splash(int v)
        {
            var b = new Wave(0.45f);
            var noise = new Noise(1800 + v);
            var band = new Biquad(FilterKind.Band, 1200, 0.9f);

            for (var i = 0; i < b.Length; i++)
            {
                var t = i / (float)Synth.Rate;
                band.Set(FilterKind.Band, 900 + 1800 * Envelope.Strike(t, 0.15f), 1);
                b.Data[i] = band.Run(noise.White() * 2) * Envelope.Strike(t, 0.35f, 0.006f);
            }

            return b;
        }

        /// <summary>A wolf's howl: a voice (a saw through two vowel formants) that rises, holds and falls.</summary>
        static Wave Howl(int v)
        {
            var b = new Wave(2.2f);
            var formant1 = new Biquad(FilterKind.Band, 700, 6);
            var formant2 = new Biquad(FilterKind.Band, 1150, 8);
            var phase = 0f;

            for (var i = 0; i < b.Length; i++)
            {
                var t = i / (float)Synth.Rate;
                var k = t / 2.2f;
                var pitch = 330 + 160 * (float)Math.Sin(k * Math.PI) - 40 * k + 10 * (float)Math.Sin(t * 30) + 20 * v;
                phase += pitch / Synth.Rate;
                var source = Synth.Saw(phase);
                var voice = formant1.Run(source) + formant2.Run(source) * 0.6f;
                b.Data[i] = voice * Synth.Clamp01(t / 0.3f) * Synth.Clamp01((2.2f - t) / 0.6f);
            }

            return b.Room(0.85f, 0.3f, 0.5f, 1.2f);
        }

        static Wave Growl(int v)
        {
            var b = new Wave(0.9f);
            var noise = new Noise(1900 + v);
            var low = new Biquad(FilterKind.Low, 600, 2);
            var phase = 0f;

            for (var i = 0; i < b.Length; i++)
            {
                var t = i / (float)Synth.Rate;
                // A growl is a low voice shaken by a slow tremble.
                var shake = 0.6f + 0.4f * Synth.Sine(28 * t + v);
                phase += (85 + 10 * v + 15 * (float)Math.Sin(t * 4)) / Synth.Rate;
                b.Data[i] = low.Run(Synth.Saw(phase) * shake + noise.Pink() * 0.6f) * Synth.Clamp01(t / 0.1f) * Synth.Clamp01((0.9f - t) / 0.3f);
            }

            return b;
        }

        static Wave Roar(int v)
        {
            var b = new Wave(1.4f);
            var noise = new Noise(2000 + v);
            var formant = new Biquad(FilterKind.Band, 500, 2.5f);
            var phase = 0f;

            for (var i = 0; i < b.Length; i++)
            {
                var t = i / (float)Synth.Rate;
                phase += (95 + 50 * Synth.Clamp01(t * 3) - 30 * t + 8 * v) / Synth.Rate;
                var source = Synth.Soft(Synth.Saw(phase) * 3 + noise.White() * 0.8f);
                b.Data[i] = formant.Run(source) * Synth.Clamp01(t / 0.15f) * Synth.Clamp01((1.4f - t) / 0.5f) * 1.5f;
            }

            return b.Room(0.7f, 0.4f, 0.35f, 0.6f);
        }

        static Wave Squeak(int v)
        {
            var b = new Wave(0.25f);

            for (var i = 0; i < b.Length; i++)
            {
                var t = i / (float)Synth.Rate;
                b.Data[i] = Synth.Sine((5200 + 900 * Synth.Sine(40 * t) + 300 * v) * t) * Envelope.Strike(t, 0.1f, 0.01f) * 0.5f;
            }

            return b;
        }

        static Wave Screech(int v)
        {
            var b = new Wave(1f);
            var phase = 0f;
            var noise = new Noise(2100 + v);
            var band = new Biquad(FilterKind.Band, 2800, 3);

            for (var i = 0; i < b.Length; i++)
            {
                var t = i / (float)Synth.Rate;
                phase += (2200 - 900 * t + 60 * Synth.Sine(12 * t) + 100 * v) / Synth.Rate;
                b.Data[i] = (band.Run(Synth.Saw(phase)) + noise.White() * 0.1f) * Synth.Clamp01(t / 0.05f) * Envelope.Strike(t, 0.9f, 0.05f);
            }

            return b.Room(0.9f, 0.2f, 0.4f, 1);
        }

        static Wave Skitter(int v)
        {
            var b = new Wave(0.6f);
            var dice = new Dice(2200 + v);

            for (var tap = 0; tap < 14; tap++)
            {
                var at = tap * 0.04f + dice.Range(0, 0.015f);
                var one = new Wave(0.03f);

                for (var i = 0; i < one.Length; i++)
                {
                    var t = i / (float)Synth.Rate;
                    one.Data[i] = Modes.At(Modes.Wood, dice.Range(2500, 4000), t, 0.02f);
                }

                b.Mix(one, at, 0.5f);
            }

            return b;
        }

        static Wave Buzz(int v)
        {
            var b = new Wave(1.2f);
            var low = new Biquad(FilterKind.Band, 400, 1.5f);

            for (var i = 0; i < b.Length; i++)
            {
                var t = i / (float)Synth.Rate;
                var wobble = 1 + 0.05f * Synth.Sine(5 * t) + 0.02f * Synth.Sine(13 * t + v);
                var wings = Synth.Saw(210 * wobble * t + 0.3f * v) * 0.5f + Synth.Saw(223 * wobble * t) * 0.5f;
                b.Data[i] = low.Run(wings) * Synth.Clamp01(t / 0.2f) * Synth.Clamp01((1.2f - t) / 0.2f);
            }

            return b;
        }

        static Wave Croak(int v)
        {
            var b = new Wave(0.5f);
            var low = new Biquad(FilterKind.Band, 350, 3);

            for (var i = 0; i < b.Length; i++)
            {
                var t = i / (float)Synth.Rate;
                // A croak is a rattle: a low pulse train under a closing vowel.
                var pulse = Synth.Square((22 + 3 * v) * t, 0.2f) * 0.5f + 0.5f;
                b.Data[i] = low.Run(Synth.Saw((110 + 10 * v) * t) * pulse) * Envelope.Strike(t, 0.45f, 0.03f) * 1.5f;
            }

            return b;
        }

        static Wave Breakthrough(int v)
        {
            var b = new Wave(4.5f);
            var gong = new Wave(4);

            for (var i = 0; i < gong.Length; i++)
            {
                var t = i / (float)Synth.Rate;
                gong.Data[i] = Modes.At(Modes.Gong, 73.4f, t, 4);
            }

            b.Mix(gong, 0, 0.8f);
            b.Mix(Chord(v, new[] { 62f, 67, 69, 74, 79, 81, 86 }, 3, 0.09f), 0.4f, 0.6f);

            return b.Room(0.9f, 0.25f, 0.5f, 2);
        }

        static Wave Thunder(int v)
        {
            var b = new Wave(4f);
            var noise = new Noise(2300 + v);
            var low = new Biquad(FilterKind.Low, 200, 0.7f);
            var dice = new Dice(2301 + v);
            var level = 1f;

            for (var i = 0; i < b.Length; i++)
            {
                var t = i / (float)Synth.Rate;

                if (dice.Chance(0.0003f))
                {
                    level = Math.Max(level, dice.Range(0.4f, 0.9f));
                }

                level *= 0.99995f;
                var crack = t < 0.25f ? noise.White() * Envelope.Strike(t, 0.2f) : 0;
                b.Data[i] = low.Run(noise.Brown() * 2) * level * Synth.Clamp01(t / 0.05f) + crack * 0.5f;
            }

            return b.Room(0.95f, 0.5f, 0.4f, 1);
        }

        static Wave Brew(int v)
        {
            var b = new Wave(1.2f);
            var dice = new Dice(2400 + v);

            for (var k = 0; k < 16; k++)
            {
                var at = dice.Range(0, 1.0f);
                var pitch = dice.Range(300, 900);
                var one = new Wave(0.08f);

                for (var i = 0; i < one.Length; i++)
                {
                    var t = i / (float)Synth.Rate;
                    one.Data[i] = Synth.Sine(pitch * (1 + t * 10) * t) * Envelope.Strike(t, 0.06f);
                }

                b.Mix(one, at, 0.25f);
            }

            var fire = Fire(v);
            b.Mix(fire, 0, 0.25f);

            return b;
        }
    }
}
