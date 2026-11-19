using GuWorld.Actors;
using GuWorld.Core;
using UnityEngine;

namespace GuWorld.Fx
{
    /// <summary>
    /// The smoke over a raided place (ROADMAP 12.9: «отряды видны заранее — дым»): from the morning the raid
    /// is foretold until it is over, a dark column rises over the Willow hamlet, seen from far off — the way
    /// the compass points. Puffs of a blended veil that rise, drift with the wind, swell and fade.
    /// </summary>
    public class SmokeSignal : MonoBehaviour
    {
        const float SeenFrom = 2600;
        const int MaxPuffs = 34;

        float wait;
        Vector3 at;
        bool placed;

        public static void Make(Transform parent)
        {
            var root = new GameObject("Smoke signal");
            root.transform.SetParent(parent, false);
            root.AddComponent<SmokeSignal>();
        }

        void Update()
        {
            var session = Game.Session;
            var hero = Game.Hero;

            if (session == null || hero == null || Game.Frozen)
            {
                return;
            }

            wait -= Time.deltaTime;

            if (wait > 0)
            {
                return;
            }

            wait = 0.4f;
            var raid = Atlas.Encounter(Session.Raid);

            if (raid == null || !(session.RaidUp || session.RaidSoon(session.State.Hour)))
            {
                return;
            }

            if (!placed)
            {
                at = new Vector3(raid.At.X, 0, raid.At.Z);
                placed = true;
            }

            var flat = hero.transform.position - at;
            flat.y = 0;

            if (flat.magnitude > SeenFrom || transform.childCount >= MaxPuffs)
            {
                return;
            }

            // A puff from among the houses, rising.
            var ground = Game.Ground(at + new Vector3(Random.Range(-12f, 12f), 0, Random.Range(-12f, 12f)));
            var puff = new GameObject("Puff");
            puff.transform.SetParent(transform, false);
            puff.transform.position = ground + Vector3.up * 4;
            var grey = Random.Range(0.12f, 0.22f);
            var colour = new Color(grey, grey * 0.97f, grey * 0.94f, 0.55f);
            Models.Part("Veil", puff.transform, Models.Quad, Models.Glow(colour, false), Vector3.zero, Vector3.one).AddComponent<Billboard>();
            var fade = puff.AddComponent<Fade>();
            fade.Life = Random.Range(10f, 13f);
            fade.From = Vector3.one * Random.Range(6f, 9f);
            fade.To = Vector3.one * Random.Range(22f, 30f);
            fade.Colour = colour;
            fade.Hold = 0.35f;
            var rise = puff.AddComponent<Rise>();
            rise.Up = Random.Range(3.2f, 4.4f);
            rise.Wind = new Vector3(1.4f, 0, 0.6f) * Random.Range(0.6f, 1.2f);
        }
    }

    /// <summary>Moves a thing up and along the wind (smoke).</summary>
    public class Rise : MonoBehaviour
    {
        public float Up = 3;
        public Vector3 Wind;

        void Update() => transform.position += (Vector3.up * Up + Wind) * Time.deltaTime;
    }
}
