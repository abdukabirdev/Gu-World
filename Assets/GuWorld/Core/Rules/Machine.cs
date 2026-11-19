namespace GuWorld.Core
{
    /// <summary>
    /// The first picture level for a machine never seen before (ROADMAP 2.7.3): from its graphics card's
    /// name and memory, the system's memory and the cores. An integrated card gets the low level (Iris Xe
    /// with enough memory the medium one, where the autotest held 30+ frames); a discrete one by its memory.
    /// Presets: 0 minimum, 1 low, 2 medium, 3 high, 4 ultra.
    /// </summary>
    public static class Machine
    {
        public static bool Integrated(string gpu)
        {
            var name = (gpu ?? "").ToLowerInvariant();

            if (name.Contains("rtx") || name.Contains("gtx") || name.Contains("radeon rx") || name.Contains(" arc") || name.Contains("quadro") || name.Contains("radeon pro"))
            {
                return false;
            }

            return name.Contains("intel") || name.Contains("uhd") || name.Contains("iris") || name.Contains("vega") || name.Contains("radeon graphics")
                || name.Contains("radeon(tm) graphics") || name.Contains("adreno") || name.Contains("mali") || name.Contains("apple m");
        }

        public static int Preset(string gpu, int videoMb, int systemMb, int cores)
        {
            var name = (gpu ?? "").ToLowerInvariant();

            if (systemMb > 0 && systemMb < 6000 || cores > 0 && cores < 4)
            {
                return 0;
            }

            if (Integrated(gpu))
            {
                return name.Contains("iris") && systemMb >= 15000 ? 2 : 1;
            }

            if (videoMb < 2000)
            {
                return 1;
            }

            if (videoMb < 3500)
            {
                return 2;
            }

            return videoMb >= 8000 && systemMb >= 15000 && cores >= 8 ? 4 : 3;
        }

        /// <summary>Frames a second below which the level is too heavy for the machine (the game warns once).</summary>
        public const float TooFewFrames = 24;
    }
}
