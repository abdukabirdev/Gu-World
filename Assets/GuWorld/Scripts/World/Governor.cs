using UnityEngine;

namespace GuWorld.World
{
    /// <summary>
    /// Keeps the frame rate up on its own: it watches the frames over a few seconds and, when they run
    /// slow, turns the detail down a notch (the grass's reach and thickness, how many townsfolk get a
    /// body, how near houses are raised as prefabs, the shadows' distance); when they have run fast for a
    /// while it turns it back up. <see cref="Detail"/> runs from 0.4 (thinnest) to 1 (as the quality
    /// asks). It can be switched off in the settings ("gw2.governor").
    /// </summary>
    public class Governor : MonoBehaviour
    {
        /// <summary>The share of the detail the quality asks for that is used now.</summary>
        public static float Detail { get; private set; } = 1;

        public static bool On
        {
            get => PlayerPrefs.GetInt("gw2.governor", 1) == 1;
            set
            {
                PlayerPrefs.SetInt("gw2.governor", value ? 1 : 0);
                PlayerPrefs.Save();

                if (!value)
                {
                    Detail = 1;
                }
            }
        }

        const float Slow = 1 / 27f;
        const float Fast = 1 / 50f;
        float sum;
        int frames;
        float window;
        float fastFor;
        float baseShadows = -1;

        public static void Make(Transform parent)
        {
            var go = new GameObject("Governor");
            go.transform.SetParent(parent, false);
            go.AddComponent<Governor>();
            Detail = 1;
        }

        void Update()
        {
            if (!On || !Game.Playing || Game.Busy)
            {
                return;
            }

            sum += Time.unscaledDeltaTime;
            frames++;
            window += Time.unscaledDeltaTime;

            if (window < 3)
            {
                return;
            }

            var average = sum / Mathf.Max(1, frames);
            sum = 0;
            frames = 0;
            window = 0;

            if (baseShadows < 0)
            {
                baseShadows = QualitySettings.shadowDistance;
            }

            if (average > Slow && Detail > 0.4f)
            {
                Detail = Mathf.Max(0.4f, Detail - 0.15f);
                fastFor = 0;
                Apply();
            }
            else if (average < Fast && Detail < 1)
            {
                fastFor += 3;

                if (fastFor >= 12)
                {
                    Detail = Mathf.Min(1, Detail + 0.1f);
                    fastFor = 0;
                    Apply();
                }
            }
            else
            {
                fastFor = 0;
            }
        }

        void Apply()
        {
            QualitySettings.shadowDistance = baseShadows * Mathf.Lerp(0.5f, 1, (Detail - 0.4f) / 0.6f);
        }
    }
}
