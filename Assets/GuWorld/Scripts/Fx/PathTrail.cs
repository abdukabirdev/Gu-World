using GuWorld.Actors;
using GuWorld.Core;
using UnityEngine;

namespace GuWorld.Fx
{
    /// <summary>
    /// What a Gu's bolt leaves behind it, by its path: embers that rise from fire, shards that fall
    /// from ice, a green mist from poison, sparks that jump from lightning, leaves from wood, smoke from
    /// darkness, drops from blood, chips from bone, rings from sound, spray from water, swirls from wind,
    /// motes from light and moon, grit from earth. Each mote is a small glowing quad that drifts and fades.
    /// </summary>
    public class PathTrail : MonoBehaviour
    {
        /// <summary>The path of the next bolt launched (the caster sets it just before; it is used once).</summary>
        public static GuPath? Next;

        GuPath path;
        Color colour;
        float wait;

        public static void Attach(GameObject bolt, Color colour)
        {
            if (Next == null)
            {
                return;
            }

            var trail = bolt.AddComponent<PathTrail>();
            trail.path = Next.Value;
            trail.colour = colour;
            Next = null;
        }

        void Update()
        {
            wait -= Time.deltaTime;

            if (wait > 0)
            {
                return;
            }

            wait = 0.035f;
            var (tint, size, drift, life) = Style();
            var mote = Models.Part("Mote", null, Models.Quad, Models.Glow(tint), transform.position + Random.insideUnitSphere * 0.25f, Vector3.one * size);
            mote.AddComponent<Billboard>();
            var fade = mote.AddComponent<Fade>();
            fade.Life = life;
            fade.From = Vector3.one * size;
            fade.To = Vector3.one * size * (path == GuPath.Poison || path == GuPath.Dark || path == GuPath.Sound ? 2.4f : 0.3f);
            fade.Colour = tint;
            mote.AddComponent<Drift>().Velocity = drift + Random.insideUnitSphere * 0.4f;
        }

        (Color tint, float size, Vector3 drift, float life) Style()
        {
            switch (path)
            {
                case GuPath.Fire: return (new Color(1f, 0.55f, 0.2f, 0.9f), 0.22f, Vector3.up * 1.6f, 0.5f);
                case GuPath.Ice: return (new Color(0.8f, 0.95f, 1f, 0.9f), 0.16f, Vector3.down * 1.2f, 0.6f);
                case GuPath.Poison: return (new Color(0.55f, 0.9f, 0.3f, 0.35f), 0.4f, Vector3.up * 0.2f, 0.9f);
                case GuPath.Lightning: return (new Color(0.85f, 0.8f, 1f, 1f), 0.12f, Random.insideUnitSphere * 4, 0.15f);
                case GuPath.Wood: return (new Color(0.5f, 0.85f, 0.4f, 0.8f), 0.15f, Vector3.down * 0.6f, 0.8f);
                case GuPath.Dark: return (new Color(0.35f, 0.3f, 0.5f, 0.4f), 0.45f, Vector3.up * 0.3f, 0.8f);
                case GuPath.Blood: return (new Color(0.9f, 0.15f, 0.2f, 0.9f), 0.12f, Vector3.down * 2.5f, 0.5f);
                case GuPath.Bone: return (new Color(0.95f, 0.92f, 0.85f, 0.9f), 0.1f, Vector3.down * 1.5f, 0.4f);
                case GuPath.Sound: return (new Color(colour.r, colour.g, colour.b, 0.3f), 0.3f, Vector3.zero, 0.4f);
                case GuPath.Water: return (new Color(0.5f, 0.8f, 1f, 0.7f), 0.12f, Vector3.down * 2f + Random.insideUnitSphere, 0.5f);
                case GuPath.Wind: return (new Color(0.85f, 1f, 0.95f, 0.4f), 0.2f, Random.onUnitSphere * 1.5f, 0.4f);
                case GuPath.Earth: return (new Color(0.75f, 0.62f, 0.45f, 0.9f), 0.12f, Vector3.down * 3f, 0.5f);
                default: return (new Color(colour.r, colour.g, colour.b, 0.7f), 0.14f, Vector3.zero, 0.45f);
            }
        }
    }

    /// <summary>Moves a mote on, slowing.</summary>
    public class Drift : MonoBehaviour
    {
        public Vector3 Velocity;

        void Update()
        {
            transform.position += Velocity * Time.deltaTime;
            Velocity *= 1 - 1.5f * Time.deltaTime;
        }
    }
}
