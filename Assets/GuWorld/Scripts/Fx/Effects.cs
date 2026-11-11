using System;
using System.Collections.Generic;
using GuWorld.Actors;
using UnityEngine;

namespace GuWorld.Fx
{
    /// <summary>Turns to face the camera every frame.</summary>
    public class Billboard : MonoBehaviour
    {
        void LateUpdate()
        {
            var camera = Camera.main;

            if (camera != null)
            {
                transform.rotation = camera.transform.rotation;
            }
        }
    }

    /// <summary>A light that grows (or shrinks) and fades out over its life, then is gone.</summary>
    public class Fade : MonoBehaviour
    {
        public float Life = 0.4f;
        public Vector3 From = Vector3.one, To = Vector3.one;
        public Color Colour = Color.white;
        /// <summary>The share of its life it stays at full brightness before fading.</summary>
        public float Hold;
        public bool Unscaled;
        float age;
        Renderer[] renderers;
        MaterialPropertyBlock block;

        void Start()
        {
            renderers = GetComponentsInChildren<Renderer>();
            block = new MaterialPropertyBlock();
            Apply(0);
        }

        void Update()
        {
            age += Unscaled ? Time.unscaledDeltaTime : Time.deltaTime;

            if (age >= Life)
            {
                Destroy(gameObject);
                return;
            }

            Apply(age / Life);
        }

        void Apply(float t)
        {
            transform.localScale = Vector3.LerpUnclamped(From, To, 1 - (1 - t) * (1 - t));
            var alpha = t <= Hold ? 1 : 1 - (t - Hold) / Mathf.Max(0.001f, 1 - Hold);
            block.SetColor("_BaseColor", new Color(Colour.r, Colour.g, Colour.b, Colour.a * alpha));

            foreach (var one in renderers)
            {
                one.SetPropertyBlock(block);
            }
        }
    }

    /// <summary>Bobs and spins in place (a herb's light, a chest's mark, a wild Gu).</summary>
    public class Hover : MonoBehaviour
    {
        public float Rise = 0.12f, Period = 2.2f, Spin;
        Vector3 start;
        float seed;

        void Start()
        {
            start = transform.localPosition;
            seed = UnityEngine.Random.value * 10;
        }

        void Update()
        {
            transform.localPosition = start + Vector3.up * Mathf.Sin((Time.time + seed) * Mathf.PI * 2 / Period) * Rise;

            if (Spin != 0)
            {
                transform.Rotate(0, Spin * Time.deltaTime, 0, Space.Self);
            }
        }
    }

    /// <summary>
    /// A warning on the ground before an enemy's blow lands: a red shape whose inner fill
    /// grows until the blow, so the player can see how long is left.
    /// </summary>
    public class Telegraph : MonoBehaviour
    {
        float life, age;
        Transform fill;
        Vector3 full;

        public static Telegraph Circle(Vector3 at, float radius, float seconds, Color? colour = null)
        {
            var tint = colour ?? new Color(1f, 0.25f, 0.2f, 1);
            var root = new GameObject("Telegraph");
            root.transform.position = Game.Ground(at) + Vector3.up * 0.08f;
            Models.Part("Rim", root.transform, Models.Ring, Models.Glow(new Color(tint.r, tint.g, tint.b, 0.9f)), Vector3.zero, Vector3.one * radius * 2);
            Models.Part("Area", root.transform, Models.Disc, Models.Glow(new Color(tint.r, tint.g, tint.b, 0.16f), false), Vector3.zero, Vector3.one * radius * 2);
            var telegraph = root.AddComponent<Telegraph>();
            telegraph.fill = Models.Part("Fill", root.transform, Models.Disc, Models.Glow(new Color(tint.r, tint.g, tint.b, 0.4f), false), new Vector3(0, 0.01f, 0), Vector3.zero).transform;
            telegraph.full = Vector3.one * radius * 2;
            telegraph.life = seconds;

            return telegraph;
        }

        public static Telegraph Line(Vector3 from, Vector3 direction, float length, float width, float seconds, Color? colour = null)
        {
            var tint = colour ?? new Color(1f, 0.25f, 0.2f, 1);
            direction.y = 0;
            direction = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.forward;
            var root = new GameObject("Telegraph");
            root.transform.position = Game.Ground(from) + Vector3.up * 0.08f;
            root.transform.rotation = Quaternion.LookRotation(direction);
            var flat = Quaternion.Euler(90, 0, 0);
            Models.Part("Area", root.transform, Models.Quad, Models.Glow(new Color(tint.r, tint.g, tint.b, 0.2f), false, Texture2D.whiteTexture), new Vector3(0, 0, length * 0.5f), new Vector3(width, length, 1), flat);
            var telegraph = root.AddComponent<Telegraph>();
            var fill = new GameObject("Fill");
            fill.transform.SetParent(root.transform, false);
            Models.Part("Bar", fill.transform, Models.Quad, Models.Glow(new Color(tint.r, tint.g, tint.b, 0.4f), false, Texture2D.whiteTexture), new Vector3(0, 0.01f, length * 0.5f), new Vector3(width, length, 1), flat);
            fill.transform.localScale = new Vector3(1, 1, 0);
            telegraph.fill = fill.transform;
            telegraph.full = Vector3.one;
            telegraph.life = seconds;

            return telegraph;
        }

        void Update()
        {
            age += Time.deltaTime;
            var t = Mathf.Clamp01(age / Mathf.Max(0.01f, life));
            fill.localScale = full.x == 1 && full.z == 1 && full.y == 1 ? new Vector3(1, 1, t) : full * t;

            if (age >= life + 0.05f)
            {
                Destroy(gameObject);
            }
        }

        public void Cancel() => Destroy(gameObject);
    }

    /// <summary>A bolt in flight: a moonblade, an ice spike, a wolf's lightning.</summary>
    public class Projectile : MonoBehaviour
    {
        bool fromHero;
        Vector3 velocity;
        float left, radius;
        bool pierce;
        Color colour;
        Action<Actor> hit;
        readonly HashSet<Actor> struck = new HashSet<Actor>();

        public static Projectile Launch(bool fromHero, Vector3 from, Vector3 direction, float speed, float range, float radius, Color colour, bool pierce, bool crescent, Action<Actor> hit)
        {
            var root = new GameObject("Projectile");
            root.transform.position = from;
            root.transform.rotation = Quaternion.LookRotation(direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.forward);
            var bright = new Color(colour.r, colour.g, colour.b, 0.95f);

            if (crescent)
            {
                var size = Mathf.Max(1.2f, radius * 2.2f);
                Models.Part("Blade", root.transform, Models.Crescent, Models.Glow(bright), Vector3.zero, Vector3.one * size);
                Models.Part("Blade 2", root.transform, Models.Crescent, Models.Glow(Color.white), new Vector3(0, 0.02f, 0), Vector3.one * size * 0.8f);
            }
            else
            {
                Models.Shape("Core", root.transform, PrimitiveType.Sphere, Models.Glow(Color.white), Vector3.zero, new Vector3(0.16f, 0.16f, 0.7f) * Mathf.Max(1, radius * 1.4f));
            }

            Models.Part("Glow", root.transform, Models.Quad, Models.Glow(new Color(colour.r, colour.g, colour.b, 0.5f)), Vector3.zero, Vector3.one * Mathf.Max(1.2f, radius * 3)).AddComponent<Billboard>();
            var light = root.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = colour;
            light.range = 6;
            light.intensity = 2.2f;
            light.shadows = LightShadows.None;
            var projectile = root.AddComponent<Projectile>();
            projectile.fromHero = fromHero;
            projectile.velocity = root.transform.forward * speed;
            projectile.left = range;
            projectile.radius = radius;
            projectile.pierce = pierce;
            projectile.colour = colour;
            projectile.hit = hit;

            return projectile;
        }

        void Update()
        {
            var step = velocity * Time.deltaTime;
            var from = transform.position;
            var to = from + step;
            left -= step.magnitude;

            // Walls and trees stop it; bodies are handled below.
            if (Physics.Raycast(from, step.normalized, out var wall, step.magnitude + 0.2f, ~0, QueryTriggerInteraction.Ignore) && wall.collider.GetComponentInParent<Actor>() == null && !(wall.collider is TerrainCollider && pierce && false))
            {
                Effects.Burst(wall.point, colour, 1.4f, 0.25f);
                Destroy(gameObject);
                return;
            }

            transform.position = to;

            foreach (var actor in Actor.All)
            {
                if (!actor.Alive || actor.IsHero == fromHero || struck.Contains(actor))
                {
                    continue;
                }

                var centre = actor.Centre;
                var flat = new Vector2(centre.x - to.x, centre.z - to.z).magnitude;

                if (flat <= radius + actor.Radius && Mathf.Abs(centre.y - to.y) <= actor.Height * 0.5f + radius + 0.4f)
                {
                    struck.Add(actor);
                    hit?.Invoke(actor);
                    Effects.Burst(to, colour, 1.6f, 0.25f);

                    if (!pierce)
                    {
                        Destroy(gameObject);
                        return;
                    }
                }
            }

            if (left <= 0)
            {
                Effects.Burst(to, colour, 1f, 0.2f);
                Destroy(gameObject);
            }
        }
    }

    /// <summary>A mote of loot that leaps from a body and flies to the hero.</summary>
    public class LootMote : MonoBehaviour
    {
        static readonly List<LootMote> flying = new List<LootMote>();
        Vector3 velocity;
        float age;
        Action arrive;

        void OnEnable() => flying.Add(this);
        void OnDisable() => flying.Remove(this);

        /// <summary>Everything still in the air lands at once (before a save, so no loot is lost with the game).</summary>
        public static void Flush()
        {
            foreach (var mote in new List<LootMote>(flying))
            {
                var land = mote.arrive;
                mote.arrive = null;
                land?.Invoke();
                Destroy(mote.gameObject);
            }
        }

        public static void Spawn(Vector3 from, Color colour, Action arrive)
        {
            var root = new GameObject("Loot");
            root.transform.position = from + Vector3.up * 0.6f;
            Models.Part("Glow", root.transform, Models.Quad, Models.Glow(new Color(colour.r, colour.g, colour.b, 0.9f)), Vector3.zero, Vector3.one * 0.5f).AddComponent<Billboard>();
            var mote = root.AddComponent<LootMote>();
            var around = UnityEngine.Random.insideUnitCircle * 3;
            mote.velocity = new Vector3(around.x, 5 + UnityEngine.Random.value * 2, around.y);
            mote.arrive = arrive;
        }

        void Update()
        {
            age += Time.deltaTime;
            var hero = Game.Hero;

            if (hero == null)
            {
                return;
            }

            if (age < 0.45f)
            {
                velocity += Vector3.down * 14 * Time.deltaTime;
                transform.position += velocity * Time.deltaTime;
                var ground = Game.Ground(transform.position).y + 0.3f;

                if (transform.position.y < ground)
                {
                    transform.position = new Vector3(transform.position.x, ground, transform.position.z);
                }

                return;
            }

            var target = hero.Centre;
            transform.position = Vector3.MoveTowards(transform.position, target, (8 + age * 30) * Time.deltaTime);

            if ((transform.position - target).sqrMagnitude < 0.3f)
            {
                arrive?.Invoke();
                Destroy(gameObject);
            }
        }
    }

    public static class Effects
    {
        /// <summary>A flash of light that swells and fades.</summary>
        public static void Burst(Vector3 at, Color colour, float size, float life)
        {
            var root = new GameObject("Burst");
            root.transform.position = at;
            Models.Part("Glow", root.transform, Models.Quad, Models.Glow(colour), Vector3.zero, Vector3.one).AddComponent<Billboard>();
            var fade = root.AddComponent<Fade>();
            fade.Life = life;
            fade.From = Vector3.one * size * 0.4f;
            fade.To = Vector3.one * size * 1.6f;
            fade.Colour = colour;
        }

        /// <summary>A ring that runs out over the ground to a radius.</summary>
        public static void Wave(Vector3 at, Color colour, float radius, float life)
        {
            var root = new GameObject("Wave");
            root.transform.position = Game.Ground(at) + Vector3.up * 0.1f;
            Models.Part("Ring", root.transform, Models.Ring, Models.Glow(colour), Vector3.zero, Vector3.one);
            var fade = root.AddComponent<Fade>();
            fade.Life = life;
            fade.From = Vector3.one * 0.6f;
            fade.To = Vector3.one * radius * 2;
            fade.Colour = colour;
            fade.Hold = 0.3f;
        }

        /// <summary>A pillar of light standing on the ground for a moment (a lightning strike, a breakthrough).</summary>
        public static void Pillar(Vector3 at, Color colour, float width, float height, float life)
        {
            var root = new GameObject("Pillar");
            root.transform.position = Game.Ground(at);
            var beam = Models.Part("Beam", root.transform, Models.Quad, Models.Glow(colour), new Vector3(0, height * 0.5f, 0), new Vector3(width, height, 1));
            beam.AddComponent<UprightBillboard>();
            var fade = root.AddComponent<Fade>();
            fade.Life = life;
            fade.From = new Vector3(1.3f, 1, 1.3f);
            fade.To = new Vector3(0.3f, 1, 0.3f);
            fade.Colour = colour;
            Wave(at, colour, width * 2.2f, life);
        }

        /// <summary>A jagged line of lightning between two points.</summary>
        public static void Bolt(Vector3 from, Vector3 to, Color colour, float life = 0.18f)
        {
            var root = new GameObject("Bolt");
            var line = root.AddComponent<LineRenderer>();
            line.sharedMaterial = Models.Glow(Color.white, true, Texture2D.whiteTexture);
            line.startColor = line.endColor = colour;
            line.startWidth = 0.16f;
            line.endWidth = 0.1f;
            const int points = 8;
            line.positionCount = points;
            var across = Vector3.Cross((to - from).normalized, Vector3.up);

            for (var i = 0; i < points; i++)
            {
                var t = i / (float)(points - 1);
                var jitter = i == 0 || i == points - 1 ? Vector3.zero : (across * UnityEngine.Random.Range(-0.5f, 0.5f) + Vector3.up * UnityEngine.Random.Range(-0.35f, 0.35f));
                line.SetPosition(i, Vector3.Lerp(from, to, t) + jitter);
            }

            UnityEngine.Object.Destroy(root, life);
            Burst(to, colour, 1.4f, 0.22f);
        }

        /// <summary>A soft light clinging to a body for a while (a shield, a buff).</summary>
        public static GameObject Aura(Transform on, Color colour, float seconds, float size = 2.2f)
        {
            var root = new GameObject("Aura");
            root.transform.SetParent(on, false);
            root.transform.localPosition = new Vector3(0, 1, 0);
            Models.Part("Glow", root.transform, Models.Quad, Models.Glow(new Color(colour.r, colour.g, colour.b, 0.35f)), Vector3.zero, Vector3.one * size).AddComponent<Billboard>();
            var ring = Models.Part("Ring", root.transform, Models.Ring, Models.Glow(colour), new Vector3(0, -0.92f, 0), Vector3.one * 1.6f);
            ring.AddComponent<Hover>().Spin = 90;
            UnityEngine.Object.Destroy(root, seconds);

            return root;
        }
    }

    /// <summary>Faces the camera but stays upright (a pillar of light).</summary>
    public class UprightBillboard : MonoBehaviour
    {
        void LateUpdate()
        {
            var camera = Camera.main;

            if (camera != null)
            {
                var away = transform.position - camera.transform.position;
                away.y = 0;

                if (away.sqrMagnitude > 0.001f)
                {
                    transform.rotation = Quaternion.LookRotation(away);
                }
            }
        }
    }
}
