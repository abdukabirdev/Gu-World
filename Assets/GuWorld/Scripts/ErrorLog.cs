using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;

namespace GuWorld
{
    /// <summary>
    /// Errors and exceptions written to a file in the saves' folder, Logs/gu-world-YYYY-MM-DD.log (ROADMAP 9):
    /// what to send when something breaks. The same message over and over is written three times, then
    /// only counted; ten days of logs are kept. The first error of a run tells the player where the log is.
    /// </summary>
    public static class ErrorLog
    {
        public static string Folder => Path.Combine(Application.persistentDataPath, "Logs");

        /// <summary>Today's log file ("" until started).</summary>
        public static string File { get; private set; } = "";

        /// <summary>Errors seen this run.</summary>
        public static int Count => count;

        static int count;
        static int unseen;
        static bool started;
        static readonly object gate = new object();
        static readonly Dictionary<string, int> seen = new Dictionary<string, int>();

        public static void Start()
        {
            if (started)
            {
                return;
            }

            started = true;

            try
            {
                Directory.CreateDirectory(Folder);
                File = Path.Combine(Folder, $"gu-world-{DateTime.Now:yyyy-MM-dd}.log");
                Prune();
                Append($"=== {DateTime.Now:yyyy-MM-dd HH:mm:ss} · GU World {UI.Ui.Version} · Unity {Application.unityVersion} · {SystemInfo.operatingSystem} · {SystemInfo.graphicsDeviceName} · {SystemInfo.systemMemorySize} MB");
            }
            catch (Exception)
            {
                File = "";
            }

            Application.logMessageReceivedThreaded += OnLog;
        }

        /// <summary>How many errors came since the interface last asked (it asks on the main thread).</summary>
        public static int TakeUnseen() => Interlocked.Exchange(ref unseen, 0);

        static void OnLog(string text, string trace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)
            {
                return;
            }

            lock (gate)
            {
                count++;
                Interlocked.Increment(ref unseen);
                var key = text.Length > 200 ? text.Substring(0, 200) : text;
                seen.TryGetValue(key, out var times);
                seen[key] = ++times;

                // The same again and again: three times in full, then a line every hundredth time.
                if (times <= 3)
                {
                    Append($"[{DateTime.Now:HH:mm:ss}] {type}: {text}\n{trace}");
                }
                else if (times % 100 == 0)
                {
                    Append($"[{DateTime.Now:HH:mm:ss}] (×{times}) {key}");
                }
            }
        }

        static void Append(string line)
        {
            if (string.IsNullOrEmpty(File))
            {
                return;
            }

            try
            {
                System.IO.File.AppendAllText(File, line + "\n");
            }
            catch (Exception)
            {
                // A log that cannot be written must not become an error of its own.
            }
        }

        /// <summary>Keeps the ten newest logs.</summary>
        static void Prune()
        {
            var logs = new List<string>(Directory.GetFiles(Folder, "gu-world-*.log"));
            logs.Sort(StringComparer.Ordinal);

            for (var i = 0; i < logs.Count - 10; i++)
            {
                System.IO.File.Delete(logs[i]);
            }
        }

        /// <summary>Opens a folder in the system's file browser.</summary>
        public static void Open(string folder)
        {
            try
            {
                Directory.CreateDirectory(folder);
            }
            catch (Exception)
            {
            }

            Application.OpenURL("file:///" + folder.Replace('\\', '/').TrimStart('/'));
        }
    }
}
