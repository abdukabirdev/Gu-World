using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using GuWorld.Core;
using GuWorld.Core.Audio;

/// <summary>
/// Checks of the sound made in code: every effect, bed and piece comes out as numbers that are sound
/// (no NaN, no clipping), is not silent, loops without a click where it loops, is the same for the
/// same seed; and a few are written as WAV files to listen to.
/// </summary>
static partial class Program
{
    static void AudioChecks(string repo, List<string> report)
    {
        var outDir = Path.Combine(repo, "Logs", "V2", "audio");
        Directory.CreateDirectory(outDir);
        var clock = Stopwatch.StartNew();
        var count = 0;

        foreach (var id in Sounds.Ids)
        {
            for (var v = 0; v < Sounds.Variants; v++)
            {
                var sound = Sounds.Make(id, v);
                Check(sound != null && sound.Sound(), $"sound {id}/{v} is sound");
                Check(sound.Rms() > 0.005f, $"sound {id}/{v} is not silent ({sound.Rms():0.0000})");
                Check(sound.Seconds < 7, $"sound {id}/{v} is short ({sound.Seconds:0.0} s)");
                count++;
            }

            File.WriteAllBytes(Path.Combine(outDir, id + ".wav"), Sounds.Make(id, 0).Wav());
        }

        Check(Sounds.Make("palm", 1).Data.SequenceEqual(Sounds.Make("palm", 1).Data), "a sound is the same for the same variant");
        Check(!Sounds.Make("palm", 1).Data.SequenceEqual(Sounds.Make("palm", 2).Data), "and differs between variants");

        foreach (GuPath path in Enum.GetValues(typeof(GuPath)))
        {
            Check(Sounds.Has(Sounds.CastOf(path)), $"the path {path} has a sound when cast");
        }

        foreach (var enemy in Bestiary.All.Where(e => e.Body != EnemyBody.Dummy))
        {
            Check(Sounds.Has(Sounds.VoiceOf(enemy)), $"enemy {enemy.Id} has a voice");
        }

        var effects = clock.Elapsed.TotalSeconds;
        clock.Restart();

        foreach (var id in Ambience.Ids)
        {
            var bed = Ambience.Make(id);
            Check(bed != null && bed.Sound() && bed.Rms() > 0.01f, $"ambience {id} is sound and not silent");
            Check(Seam(bed) < 6, $"ambience {id} loops without a click ({Seam(bed):0.0} × an ordinary step)");
            File.WriteAllBytes(Path.Combine(outDir, "bed_" + id + ".wav"), bed.Wav());
        }

        var beds = clock.Elapsed.TotalSeconds;
        clock.Restart();

        foreach (Mood mood in Enum.GetValues(typeof(Mood)))
        {
            var score = Music.Compose(mood, 1);
            Check(score.Melody.Count > 4 || !score.Style.Flute && !score.Style.Bowed && score.Melody.Count > 0, $"music {mood}: a melody");
            Check(score.Melody.All(n => n.Midi > 30 && n.Midi < 100), $"music {mood}: notes in reach");
            var tones = score.Melody.Select(n => ((n.Midi - score.Style.Root) % 12 + 12) % 12).Distinct().Count();
            Check(tones <= 5, $"music {mood}: pentatonic ({tones} tones)");
            var piece = Music.Render(score);
            Check(piece.Sound() && piece.Rms() > 0.01f, $"music {mood} is sound and not silent");
            Check(Math.Abs(piece.Seconds - score.Seconds) < 0.05f, $"music {mood} is as long as its score");
            Check(Seam(piece) < 6, $"music {mood} loops without a click ({Seam(piece):0.0} × an ordinary step)");

            if (mood == Mood.Valley || mood == Mood.Combat || mood == Mood.Bai || mood == Mood.Cave)
            {
                File.WriteAllBytes(Path.Combine(outDir, "music_" + mood.ToString().ToLowerInvariant() + ".wav"), piece.Wav());
            }
        }

        Check(Music.Compose(Mood.Night, 5).Melody.Select(n => n.Midi).SequenceEqual(Music.Compose(Mood.Night, 5).Melody.Select(n => n.Midi)), "the same mood and seed write the same piece");
        Check(!Music.Compose(Mood.Night, 5).Melody.Select(n => n.Midi).SequenceEqual(Music.Compose(Mood.Night, 6).Melody.Select(n => n.Midi)), "another seed writes another");
        Check(Music.MoodOf("bai", false, false, false, false, false, false) == Mood.Bai && Music.MoodOf("", false, true, true, false, false, false) == Mood.Combat, "the mood follows the place and the fight");

        var music = clock.Elapsed.TotalSeconds;
        report.Add($"\nAUDIO: {count} effects in {effects:0.0} s, {Ambience.Ids.Count()} beds in {beds:0.0} s, {Enum.GetValues(typeof(Mood)).Length} pieces in {music:0.0} s; WAVs in {outDir}");
    }

    /// <summary>How big the jump from a loop's last sample to its first is against the jumps between its samples elsewhere (≈ 1 for a seam that cannot be heard; a click is many times more).</summary>
    static float Seam(Wave loop)
    {
        var jump = Math.Abs(loop.Data[loop.Length - 1] - loop.Data[0]);
        var sum = 0.0;

        for (var i = 1; i < loop.Length; i++)
        {
            sum += Math.Abs(loop.Data[i] - loop.Data[i - 1]);
        }

        var typical = (float)(sum / (loop.Length - 1));

        return jump / Math.Max(0.00001f, typical);
    }
}
