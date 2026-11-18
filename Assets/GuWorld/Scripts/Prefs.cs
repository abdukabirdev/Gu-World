using System.Collections.Generic;
using UnityEngine;

namespace GuWorld
{
    /// <summary>
    /// The settings of this computer (ROADMAP 2.7), kept in PlayerPrefs apart from any save: a new game
    /// or a reset leaves them as they are. The picture's own numbers are Quality's, the volumes the
    /// AudioDirector's, the keys Bindings'. Each takes effect the moment it changes.
    /// </summary>
    public static class Prefs
    {
        // ------------------------------------------------------------------ Game

        /// <summary>The advice lines while playing.</summary>
        public static bool Hints { get => Bool("gw2.hints", true); set => Bool("gw2.hints", value, true); }
        /// <summary>Damage numbers: 0 every one, 1 only on enemies, 2 none.</summary>
        public static int DamageNumbers { get => Int("gw2.numbers", 0); set => Int("gw2.numbers", value, true); }
        /// <summary>The reactions' words over enemies ("Пар", "Раскол").</summary>
        public static bool ReactionWords { get => Bool("gw2.reactions", true); set => Bool("gw2.reactions", value, true); }
        /// <summary>The quest's mark: 0 over the place in the world and on the compass, 1 the compass only, 2 none.</summary>
        public static int QuestMark { get => Int("gw2.questmark", 0); set => Int("gw2.questmark", value, true); }

        public static readonly int[] AutosaveChoices = { 30, 120, 300 };
        /// <summary>Seconds between saves while playing.</summary>
        public static int Autosave { get => Int("gw2.autosave", 30); set => Int("gw2.autosave", value, true); }

        /// <summary>The developer's mode: the debug overlay (F3), hitboxes (F7) and the panel (F10) — later (ROADMAP 12.1).</summary>
        public static bool DevMode { get => Bool("gw2.dev", false); set => Bool("gw2.dev", value, true); }

        // ------------------------------------------------------------------ Controls

        public static float SensitivityX { get => Float("gw2.sensx", 1); set => Float("gw2.sensx", value, true); }
        public static float SensitivityY { get => Float("gw2.sensy", 1); set => Float("gw2.sensy", value, true); }
        public static bool InvertY { get => Bool("gw2.inverty", false); set => Bool("gw2.inverty", value, true); }
        /// <summary>Running hard: false — while the key is held; true — the key turns it on (it ends when the hero stops or tires).</summary>
        public static bool SprintToggle { get => Bool("gw2.sprinttoggle", false); set => Bool("gw2.sprinttoggle", value, true); }
        /// <summary>Holding a target: false — the key takes and drops it; true — only while the key is held.</summary>
        public static bool TargetHold { get => Bool("gw2.targethold", false); set => Bool("gw2.targethold", value, true); }
        /// <summary>The conversations' words: 0 at once, 1 typed fast, 2 typed slowly.</summary>
        public static int TextSpeed { get => Int("gw2.textspeed", 1); set => Int("gw2.textspeed", value, true); }
        /// <summary>The interface's own sounds (pointing, choosing).</summary>
        public static bool UiSounds { get => Bool("gw2.uisounds", true); set => Bool("gw2.uisounds", value, true); }

        /// <summary>The camera's field of view, degrees.</summary>
        public static float Fov { get => Float("gw2.fov", 58); set => Float("gw2.fov", value, true); }
        /// <summary>How much blows shake the camera, 0…1.</summary>
        public static float CameraShake { get => Float("gw2.shake", 1); set => Float("gw2.shake", value, true); }

        // ------------------------------------------------------------------ Picture

        /// <summary>0 a window, 1 the whole screen without a frame, 2 the whole screen (exclusive).</summary>
        public static int WindowMode { get => Int("gw2.window", 1); set { Int("gw2.window", value, true); ApplyScreen(); } }
        /// <summary>The size chosen, "1920x1080", or "" for the screen's own.</summary>
        public static string Resolution { get => PlayerPrefs.GetString("gw2.resolution", ""); set { PlayerPrefs.SetString("gw2.resolution", value); PlayerPrefs.Save(); ApplyScreen(); } }
        public static bool VSync { get => Bool("gw2.vsync", true); set { Bool("gw2.vsync", value, true); ApplyScreen(); } }

        public static readonly int[] FpsChoices = { 0, 30, 60, 90, 120, 144 };
        /// <summary>The frame cap, 0 none (with vsync on, the screen's own rate).</summary>
        public static int FpsCap { get => Int("gw2.fpscap", 0); set { Int("gw2.fpscap", value, true); ApplyScreen(); } }
        public static bool ShowFps { get => Bool("gw2.showfps", false); set => Bool("gw2.showfps", value, true); }
        /// <summary>Brightness, 0.6…1.4 (the post-processing's exposure; from the medium preset up).</summary>
        public static float Brightness { get => Float("gw2.brightness", 1); set { Float("gw2.brightness", value, true); Game.Rig?.Dress(); } }

        // ------------------------------------------------------------------ Sound

        /// <summary>The sound while the window is not in front: 0 as it is, 1 quieter, 2 none.</summary>
        public static int Background { get => Int("gw2.background", 1); set => Int("gw2.background", value, true); }

        // ------------------------------------------------------------------ Interface

        /// <summary>How big the HUD is, 0.8…1.3.</summary>
        public static float HudScale { get => Float("gw2.hudscale", 1); set => Float("gw2.hudscale", value, true); }
        /// <summary>How solid the HUD is, 0.4…1.</summary>
        public static float HudOpacity { get => Float("gw2.hudalpha", 1); set => Float("gw2.hudalpha", value, true); }
        public static bool Compass { get => Bool("gw2.compass", true); set => Bool("gw2.compass", value, true); }
        public static bool Minimap { get => Bool("gw2.minimap", true); set => Bool("gw2.minimap", value, true); }
        /// <summary>The conversations' words bigger.</summary>
        public static bool LargeText { get => Bool("gw2.largetext", false); set => Bool("gw2.largetext", value, true); }

        // ------------------------------------------------------------------ Accessibility

        /// <summary>No flashes: the hurt flash, lightning and the bright bursts are toned down.</summary>
        public static bool NoFlashes { get => Bool("gw2.noflash", false); set => Bool("gw2.noflash", value, true); }

        /// <summary>Colours for the colour-blind: gifts blue, harms orange, and the effects told apart by shape too.</summary>
        public static bool ColourBlind { get => Bool("gw2.colourblind", false); set => Bool("gw2.colourblind", value, true); }

        // ------------------------------------------------------------------ Every section back to how it was

        public static void ResetGame()
        {
            Delete("gw2.hints", "gw2.numbers", "gw2.reactions", "gw2.questmark", "gw2.autosave", "gw2.dev", "gw2.textspeed");
        }

        public static void ResetControls()
        {
            Delete("gw2.sensx", "gw2.sensy", "gw2.inverty", "gw2.fov", "gw2.shake", "gw2.sprinttoggle", "gw2.targethold");
            Actors.Bindings.Reset();
        }

        public static void ResetPicture()
        {
            Delete("gw2.window", "gw2.resolution", "gw2.vsync", "gw2.fpscap", "gw2.showfps", "gw2.brightness");
            Quality.Set(SystemInfo.graphicsMemorySize >= 3500 ? 3 : 2);
            ApplyScreen();
        }

        public static void ResetSound()
        {
            Delete("gw2.background", "gw2.uisounds");
            Sound.AudioDirector.SetVolumes(0.9f, 0.9f, 0.55f, 0.7f);
        }

        public static void ResetInterface()
        {
            Delete("gw2.hudscale", "gw2.hudalpha", "gw2.compass", "gw2.minimap", "gw2.largetext", "gw2.noflash", "gw2.colourblind");
        }

        // ------------------------------------------------------------------ The screen

        /// <summary>The sizes the screen offers, biggest first, each once.</summary>
        public static List<string> Resolutions()
        {
            var sizes = new List<string>();

            foreach (var mode in Screen.resolutions)
            {
                var size = $"{mode.width}x{mode.height}";

                if (!sizes.Contains(size))
                {
                    sizes.Insert(0, size);
                }
            }

            return sizes;
        }

        /// <summary>The window, its size, vsync and the frame cap as chosen.</summary>
        public static void ApplyScreen()
        {
            var mode = WindowMode == 0 ? FullScreenMode.Windowed : WindowMode == 2 ? FullScreenMode.ExclusiveFullScreen : FullScreenMode.FullScreenWindow;
            int width = Display.main.systemWidth, height = Display.main.systemHeight;
            var size = Resolution.Split('x');

            if (size.Length == 2 && int.TryParse(size[0], out var w) && int.TryParse(size[1], out var h) && w > 0 && h > 0)
            {
                width = w;
                height = h;
            }
            else if (mode == FullScreenMode.Windowed)
            {
                width = Mathf.RoundToInt(width * 0.8f);
                height = Mathf.RoundToInt(height * 0.8f);
            }

            if (!Application.isEditor && (Screen.width != width || Screen.height != height || Screen.fullScreenMode != mode))
            {
                Screen.SetResolution(width, height, mode);
            }

            QualitySettings.vSyncCount = VSync ? 1 : 0;
            Application.targetFrameRate = FpsCap > 0 ? FpsCap : -1;
        }

        // ------------------------------------------------------------------ PlayerPrefs

        static bool Bool(string key, bool fallback) => PlayerPrefs.GetInt(key, fallback ? 1 : 0) == 1;
        static void Bool(string key, bool value, bool _) => Store(() => PlayerPrefs.SetInt(key, value ? 1 : 0));
        static int Int(string key, int fallback) => PlayerPrefs.GetInt(key, fallback);
        static void Int(string key, int value, bool _) => Store(() => PlayerPrefs.SetInt(key, value));
        static float Float(string key, float fallback) => PlayerPrefs.GetFloat(key, fallback);
        static void Float(string key, float value, bool _) => Store(() => PlayerPrefs.SetFloat(key, value));

        static void Store(System.Action write)
        {
            write();
            PlayerPrefs.Save();
        }

        static void Delete(params string[] keys)
        {
            foreach (var key in keys)
            {
                PlayerPrefs.DeleteKey(key);
            }

            PlayerPrefs.Save();
        }
    }
}
