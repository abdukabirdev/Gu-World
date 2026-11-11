using GuWorld.Actors;
using GuWorld.Core;
using GuWorld.Fx;
using UnityEngine;
using UnityEngine.Rendering;

namespace GuWorld.World
{
    /// <summary>
    /// The weather where the camera is: rain as falling streaks, snow as drifting flakes, mist that
    /// hangs in fog, and in a storm on the heights lightning that looks for whoever stands in the open
    /// (a red ring warns where it will strike — step out of it).
    /// </summary>
    public class WeatherFx : MonoBehaviour
    {
        ParticleSystem rain, snow, mist;
        float strikeWait = 8;

        public static WeatherFx Make(Transform parent)
        {
            var root = new GameObject("Weather");
            root.transform.SetParent(parent, false);
            var weather = root.AddComponent<WeatherFx>();
            weather.rain = weather.Fall("Rain", new Color(0.75f, 0.82f, 0.9f, 0.35f), 18, 0.05f, 0.9f, true);
            weather.snow = weather.Fall("Snow", new Color(1, 1, 1, 0.8f), 2.2f, 0.12f, 0.12f, false);
            weather.mist = weather.Mist();

            return weather;
        }

        ParticleSystem Fall(string name, Color colour, float speed, float size, float length, bool streaks)
        {
            var holder = new GameObject(name);
            holder.transform.SetParent(transform, false);
            var particles = holder.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.loop = true;
            main.startLifetime = 26 / speed;
            main.startSpeed = speed;
            main.startSize = size;
            main.startColor = colour;
            main.maxParticles = Quality.Level >= 2 ? 3000 : Quality.Level == 1 ? 1600 : 700;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0;
            var emission = particles.emission;
            emission.rateOverTime = 0;
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(44, 1, 44);
            holder.transform.localRotation = Quaternion.Euler(90, 0, 0);
            var velocity = particles.velocityOverLifetime;
            velocity.enabled = !streaks;

            if (!streaks)
            {
                // Snow drifts.
                velocity.space = ParticleSystemSimulationSpace.World;
                velocity.x = new ParticleSystem.MinMaxCurve(-0.6f, 0.6f);
                velocity.z = new ParticleSystem.MinMaxCurve(-0.6f, 0.6f);
                velocity.y = new ParticleSystem.MinMaxCurve(0, 0);
            }

            var renderer = holder.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = Models.Glow(Color.white, false);
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            if (streaks)
            {
                renderer.renderMode = ParticleSystemRenderMode.Stretch;
                renderer.velocityScale = length * 0.05f;
                renderer.lengthScale = 1;
            }

            return particles;
        }

        ParticleSystem Mist()
        {
            var holder = new GameObject("Mist");
            holder.transform.SetParent(transform, false);
            var particles = holder.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(8, 14);
            main.startSpeed = 0.3f;
            main.startSize = new ParticleSystem.MinMaxCurve(10, 22);
            main.startColor = new Color(0.8f, 0.85f, 0.9f, 0.08f);
            main.maxParticles = Quality.Level >= 1 ? 90 : 40;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = particles.emission;
            emission.rateOverTime = 0;
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(70, 4, 70);
            var renderer = holder.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = Models.Glow(Color.white, false);
            renderer.shadowCastingMode = ShadowCastingMode.Off;

            return particles;
        }

        static void Rate(ParticleSystem particles, float rate)
        {
            var emission = particles.emission;
            emission.rateOverTime = rate;

            if (rate > 0 && !particles.isPlaying)
            {
                particles.Play();
            }
            else if (rate <= 0 && particles.isPlaying)
            {
                particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        void Update()
        {
            var camera = Game.Rig != null ? Game.Rig.Camera : null;

            if (camera == null || !Game.Playing)
            {
                Rate(rain, 0);
                Rate(snow, 0);
                Rate(mist, 0);
                return;
            }

            var at = camera.transform.position;
            var hero = Game.Hero;
            var inCave = hero != null && (Atlas.PlaceAt(new MapPoint(hero.transform.position.x, hero.transform.position.z))?.Cave ?? false);
            var sky = inCave ? Sky.Clear : Atmosphere.Sky;
            var strength = Mathf.Clamp01(Atmosphere.Gloom * 1.4f);
            var scale = Quality.Level >= 2 ? 1f : Quality.Level == 1 ? 0.6f : 0.3f;
            rain.transform.position = at + Vector3.up * 16 + camera.transform.forward * 8;
            snow.transform.position = at + Vector3.up * 12 + camera.transform.forward * 6;
            mist.transform.position = new Vector3(at.x, Game.Ground(at).y + 2, at.z);
            Rate(rain, sky == Sky.Rain || sky == Sky.Storm ? (sky == Sky.Storm ? 1400 : 800) * strength * scale : 0);
            Rate(snow, sky == Sky.Snow ? 420 * strength * scale : 0);
            Rate(mist, sky == Sky.Fog || sky == Sky.Rain ? 6 * strength : 0);

            if (hero != null && hero.Alive && !Game.Frozen && !Game.Busy && Weather.LightningWeather(sky, hero.transform.position.y))
            {
                strikeWait -= Time.deltaTime;

                if (strikeWait <= 0)
                {
                    strikeWait = Random.Range(14f, 30f);
                    StartCoroutine(Strike(hero));
                }
            }
        }

        /// <summary>Lightning looking for the hero: a ring on the ground, a moment, the strike.</summary>
        System.Collections.IEnumerator Strike(Hero hero)
        {
            var spot = hero.transform.position + new Vector3(Random.Range(-1.5f, 1.5f), 0, Random.Range(-1.5f, 1.5f));
            Telegraph.Circle(spot, 3, 1.3f, new Color(0.75f, 0.7f, 1f, 1));
            yield return new WaitForSeconds(1.3f);
            Effects.Bolt(spot + Vector3.up * 40, spot, new Color(0.85f, 0.8f, 1f), 0.25f);
            Effects.Wave(spot, new Color(0.75f, 0.7f, 1f), 3, 0.4f);
            Game.Shake(0.5f);

            if (hero != null && hero.Alive && (hero.transform.position - spot).magnitude < 3 + hero.Radius && !hero.Airborne)
            {
                hero.TakeHit(Game.Session.Stats.MaxHealth * 0.18f, spot);
                Game.Session.Afflict(StatusKind.Stun, 0, 0.6f);
                Game.Say("Молния!");
            }
        }
    }
}
