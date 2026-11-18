using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GuWorld.UI
{
    /// <summary>
    /// The conversations' words typed out (Settings → Game → text in conversations: at once, fast, slowly);
    /// the key that turns the page finishes the typing first.
    /// </summary>
    public class Typewriter : MonoBehaviour
    {
        static readonly List<Typewriter> running = new List<Typewriter>();
        Text text;
        string full;
        float shown;
        float speed;

        public static bool Busy => running.Count > 0;

        /// <summary>Types a text's words into it (or puts them at once, if the setting says so).</summary>
        public static void On(Text text, string full)
        {
            if (Prefs.TextSpeed <= 0 || string.IsNullOrEmpty(full))
            {
                text.text = full;
                return;
            }

            var typer = text.gameObject.AddComponent<Typewriter>();
            typer.text = text;
            typer.full = full;
            typer.speed = Prefs.TextSpeed == 1 ? 70 : 32;
            text.text = "";
            running.Add(typer);
        }

        public static void FinishAll()
        {
            foreach (var typer in running.ToArray())
            {
                typer.Finish();
            }
        }

        void Update()
        {
            shown += Time.unscaledDeltaTime * speed;
            var count = Mathf.Min(full.Length, (int)shown);
            text.text = full.Substring(0, count);

            if (count >= full.Length)
            {
                Finish();
            }
        }

        void Finish()
        {
            if (text != null)
            {
                text.text = full;
            }

            running.Remove(this);
            Destroy(this);
        }

        void OnDestroy() => running.Remove(this);
    }

    /// <summary>The interface's own small sounds (pointing at, choosing), unless the settings turn them off.</summary>
    public static class UiSound
    {
        static float lastHover;

        public static void Hover()
        {
            if (!Prefs.UiSounds || Time.unscaledTime - lastHover < 0.06f)
            {
                return;
            }

            lastHover = Time.unscaledTime;
            Sound.AudioDirector.Play("hover", null, 0.22f);
        }

        public static void Click()
        {
            if (Prefs.UiSounds)
            {
                Sound.AudioDirector.Play("click", null, 0.4f);
            }
        }
    }
}
