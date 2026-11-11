using System;
using System.Collections;
using System.IO;
using System.Text;
using GuWorld.Actors;
using GuWorld.Core;
using GuWorld.UI;
using UnityEngine;

namespace GuWorld
{
    /// <summary>
    /// A self-driving smoke test: <c>GuWorld.exe -autotest [-quality N]</c>. It never touches the player's save,
    /// logs every error to Logs/autotest/errors.txt, visits the valley's places taking a screenshot and the
    /// frame rate at each, fights a wolf, opens the pages, for each quality level asked (all three by default).
    /// </summary>
    public static class AutoTest
    {
        public static bool On { get; private set; }

        static string folder;
        static StringBuilder errors = new StringBuilder();
        static StringBuilder report = new StringBuilder();

        static string Arg(string name)
        {
            var args = Environment.GetCommandLineArgs();

            for (var i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name)
                {
                    return args[i + 1];
                }
            }

            return null;
        }

        public static void Detect()
        {
            foreach (var arg in Environment.GetCommandLineArgs())
            {
                if (arg == "-autotest")
                {
                    On = true;
                }
            }
        }

        public static void Start(Game game)
        {
            var root = Application.isEditor ? Path.Combine(Application.dataPath, "..") : Path.Combine(Application.dataPath, "..", "..", "..");
            folder = Path.GetFullPath(Path.Combine(root, "Logs", "autotest"));

            if (!Directory.Exists(Path.GetDirectoryName(folder)))
            {
                folder = Path.Combine(Application.persistentDataPath, "autotest");
            }

            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "errors.txt"), "");
            Application.logMessageReceived += (text, trace, type) =>
            {
                if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                {
                    errors.AppendLine("[" + type + "] " + text);
                    errors.AppendLine(trace);
                    File.WriteAllText(Path.Combine(folder, "errors.txt"), errors.ToString());
                }
            };

            game.StartCoroutine(Run(game));
        }

        static void Note(string line)
        {
            report.AppendLine(line);
            File.WriteAllText(Path.Combine(folder, "report.txt"), report.ToString());
        }

        static IEnumerator Run(Game game)
        {
            var asked = Arg("-quality");
            var levels = asked != null ? new[] { int.Parse(asked) } : new[] { 0, 1, 2 };

            foreach (var level in levels)
            {
                Quality.Set(level, false);
                Note("== Quality " + level + " (" + Quality.Names[level] + ") " + Screen.width + "x" + Screen.height + " ==");
                game.Begin(true);

                while (!Game.Playing)
                {
                    yield return null;
                }

                yield return Frames(30);
                var places = new (string name, Vector3 at)[]
                {
                    ("village", new Vector3(0, 0, -16)),
                    ("north_gate", At("north_gate")),
                    ("grove", At("grove")),
                    ("forest_edge", At("forest_edge")),
                    ("ford", At("river")),
                    ("waterfall", At("waterfall")),
                    ("hermit_cave", Door("door_hermit")),
                    ("camp", At("camp")),
                    ("border_stone", At("border_stone")),
                    ("ridge", At("ridge")),
                    ("village_gate_south", new Vector3(0, 0, -76)),
                    ("village_gate_north", new Vector3(0, 0, 92)),
                };
                var n = 0;

                foreach (var (name, at) in places)
                {
                    n++;
                    Game.Hero.Teleport(Game.Ground(at) + Vector3.up * 0.2f);
                    Game.Rig.Follow(Game.Hero, 0);
                    yield return Frames(20);
                    var start = Time.realtimeSinceStartup;
                    yield return Frames(90);
                    var fps = 90 / (Time.realtimeSinceStartup - start);
                    Shot(level, n, name);
                    Note(string.Format("q{0} {1,-18} at ({2:0},{3:0},{4:0}) fps {5:0.0}", level, name, at.x, at.y, at.z, fps));
                    yield return Frames(2);
                }

                // A wolf beside the hero.
                Game.Hero.Teleport(Game.Ground(At("forest_edge")) + Vector3.up * 0.2f);
                Game.Rig.Follow(Game.Hero, 0);
                yield return Frames(30);
                var p = Game.Hero.transform.position;
                Enemy.Spawn(Bestiary.Get("wolf"), p + Game.Hero.transform.forward * 5f, Game.Hero.transform.eulerAngles.y + 180, "autotest");
                yield return new WaitForSecondsRealtime(0.5f);
                Shot(level, 50, "wolf_a");
                yield return new WaitForSecondsRealtime(1.5f);
                Shot(level, 51, "wolf_b");
                Game.Hero.Teleport(Game.Ground(new Vector3(0, 0, -16)) + Vector3.up * 0.2f);
                yield return Frames(10);

                var pages = new[] { Ui.Page.Aperture, Ui.Page.Bag, Ui.Page.Journal, Ui.Page.Map, Ui.Page.Shop, Ui.Page.Fusion, Ui.Page.Spring, Ui.Page.Pause, Ui.Page.Title };
                n = 60;

                foreach (var page in pages)
                {
                    Game.Ui.Show(page);
                    yield return Frames(15);
                    Shot(level, n++, "page_" + page);
                    yield return Frames(3);
                }

                Game.Ui.Show(Ui.Page.None);
                Note("q" + level + " done");
            }

            Note("ALL DONE, errors logged: " + (errors.Length > 0 ? "yes" : "none"));
            yield return Frames(5);
            Application.Quit();
        }

        static Vector3 At(string place)
        {
            foreach (var p in Valley.Places)
            {
                if (p.Id == place)
                {
                    return new Vector3(p.At.X, 0, p.At.Z);
                }
            }

            throw new Exception("no place " + place);
        }

        static Vector3 Door(string id)
        {
            var door = Valley.Thing(id);

            return new Vector3(door.To.X, 0, door.To.Z);
        }

        static IEnumerator Frames(int count)
        {
            for (var i = 0; i < count; i++)
            {
                yield return null;
            }
        }

        static void Shot(int level, int n, string name) =>
            ScreenCapture.CaptureScreenshot(Path.Combine(folder, "q" + level + "_" + n.ToString("00") + "_" + name + ".png"));
    }
}
