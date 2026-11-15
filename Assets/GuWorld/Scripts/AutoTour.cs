using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
using GuWorld.World;
using UnityEngine;
using UnityEngine.Profiling;

namespace GuWorld
{
    /// <summary>
    /// A hand-made tour for the autotest: <c>-autotest -tour file.txt</c>. Lines of the file:
    /// <c>W name x z yaw</c> — a place of the world; <c>D name x y z yaw</c> — a place of the clan's underground, relative
    /// to its origin (the room is entered first). Each stop: a screenshot, the frame rate, the memory, into Logs/autotest/tour.
    /// </summary>
    public static class AutoTour
    {
        public static string File_ => Environment.GetCommandLineArgs() is var a && Array.IndexOf(a, "-tour") is var i && i >= 0 && i < a.Length - 1 ? a[i + 1] : null;

        static float F(string s) => float.Parse(s, CultureInfo.InvariantCulture);

        public static IEnumerator Run(Game game, string file, string folder, float loadSeconds)
        {
            var report = new StringBuilder();
            var shots = Path.Combine(folder, "tour");
            Directory.CreateDirectory(shots);
            report.AppendLine("load seconds (Begin to Playing): " + loadSeconds.ToString("0.0"));
            var n = 0;
            var inDepths = false;

            foreach (var raw in File.ReadAllLines(file))
            {
                var line = raw.Trim();

                if (line.Length == 0 || line[0] == '#')
                {
                    continue;
                }

                var p = line.Split(' ');
                n++;
                System.IO.File.AppendAllText(Path.Combine(shots, "progress.txt"), Time.realtimeSinceStartup.ToString("0") + " start " + line + System.Environment.NewLine);

                if (p[0] == "W")
                {
                    if (inDepths)
                    {
                        Rooms.Leave();
                        yield return new WaitForSecondsRealtime(2);
                        inDepths = false;
                    }

                    var at = new Vector3(F(p[2]), 0, F(p[3]));
                    Streamer.Instance?.LoadNow(at);
                    Game.Hero.Teleport(Game.Ground(at) + Vector3.up * 0.2f);
                    Game.Rig.Follow(Game.Hero, F(p[4]));

                    if (p.Length > 7)
                    {
                        var yaw = Quaternion.Euler(0, F(p[4]), 0);
                        var g = Game.Ground(at);
                        CameraRig.Photo = (g + Vector3.up * F(p[5]) - yaw * Vector3.forward * F(p[6]), Quaternion.Euler(F(p[7]), F(p[4]), 0));
                    }
                    else
                    {
                        CameraRig.Photo = null;
                    }

                    yield return new WaitForSecondsRealtime(4f);
                }
                else
                {
                    if (!inDepths)
                    {
                        Rooms.Enter("gu_yue_depths", Game.Hero.transform.position, 0);
                        yield return new WaitForSecondsRealtime(3);
                        inDepths = true;
                    }

                    CameraRig.Photo = null;
                    var (ox, oy, oz) = GuWorld.Core.Interiors.Origin("gu_yue_depths");
                    Game.Hero.Teleport(new Vector3(ox + F(p[2]), oy + F(p[3]) + 0.25f, oz + F(p[4])));
                    Game.Rig.Follow(Game.Hero, F(p[5]));
                    yield return new WaitForSecondsRealtime(1.5f);
                }

                var start = Time.realtimeSinceStartup;
                var frames = 90;

                for (var i = 0; i < frames; i++)
                {
                    yield return null;
                }

                var fps = frames / (Time.realtimeSinceStartup - start);
                ScreenCapture.CaptureScreenshot(Path.Combine(shots, n.ToString("00") + "_" + p[1] + ".png"));
                yield return null;
                yield return null;
                var pos = Game.Hero.transform.position;
                var below = Physics.Raycast(pos + Vector3.up * 0.5f, Vector3.down, out var hit, 3f) ? hit.distance - 0.5f : -1f;
                report.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0,-22} fps {1,6:0.0}  hero ({2:0.0},{3:0.0},{4:0.0}) floor below {5:0.00}  mem {6} MB (mono {7} MB)",
                    p[1], fps, pos.x, pos.y, pos.z, below, Profiler.GetTotalAllocatedMemoryLong() / 1048576, GC.GetTotalMemory(false) / 1048576));
                System.IO.File.WriteAllText(Path.Combine(shots, "tour.txt"), report.ToString());
            }

            report.AppendLine("tour done");
            System.IO.File.WriteAllText(Path.Combine(shots, "tour.txt"), report.ToString());
        }
    }
}
