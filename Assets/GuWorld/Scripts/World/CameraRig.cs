using GuWorld.Actors;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace GuWorld.World
{
    /// <summary>
    /// The camera behind the hero's shoulder: the mouse turns it, the wheel moves it in and
    /// out, walls and hills push it in, a held target pulls it round, blows shake it.
    /// </summary>
    public class CameraRig : MonoBehaviour
    {
        Camera view;
        Hero hero;
        float yaw, pitch = 14, distance = 5.2f, shown = 5.2f;
        float shake;
        Vector3 focus;
        Volume volume;
        public Camera Camera => view;

        public static CameraRig Make()
        {
            var root = new GameObject("Camera") { tag = "MainCamera" };
            var rig = root.AddComponent<CameraRig>();
            rig.view = root.AddComponent<Camera>();
            rig.view.nearClipPlane = 0.15f;
            rig.view.farClipPlane = 1400;
            rig.view.fieldOfView = 58;
            rig.view.clearFlags = CameraClearFlags.SolidColor;
            root.AddComponent<AudioListener>();
            root.transform.position = new Vector3(0, 60, -120);
            root.transform.rotation = Quaternion.Euler(18, 0, 0);
            rig.Dress();

            return rig;
        }

        /// <summary>Post-processing for the quality chosen: bloom makes the Gu's light glow.</summary>
        public void Dress()
        {
            var data = view.GetUniversalAdditionalCameraData();

            if (data != null)
            {
                data.renderPostProcessing = Quality.Level >= 1;
                data.antialiasing = Quality.Level >= 2 ? AntialiasingMode.SubpixelMorphologicalAntiAliasing : AntialiasingMode.None;
            }

            if (volume == null)
            {
                volume = gameObject.AddComponent<Volume>();
                volume.isGlobal = true;
                volume.priority = 10;
                var profile = ScriptableObject.CreateInstance<VolumeProfile>();
                var bloom = profile.Add<Bloom>(true);
                bloom.threshold.Override(0.95f);
                bloom.intensity.Override(0.9f);
                bloom.scatter.Override(0.6f);
                var vignette = profile.Add<Vignette>(true);
                vignette.intensity.Override(0.28f);
                vignette.smoothness.Override(0.4f);
                var colours = profile.Add<ColorAdjustments>(true);
                colours.contrast.Override(8);
                colours.saturation.Override(6);
                volume.profile = profile;
            }

            volume.enabled = Quality.Level >= 1;
        }

        public void Follow(Hero whom, float facing)
        {
            hero = whom;
            yaw = facing;
            pitch = 14;
            focus = whom.transform.position + Vector3.up * 1.5f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void Shake(float amount) => shake = Mathf.Max(shake, amount);

        void LateUpdate()
        {
            if (hero == null)
            {
                // The title: a slow turn over where the village will be.
                transform.rotation = Quaternion.Euler(16, Time.unscaledTime * 2.5f, 0);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                return;
            }

            var free = !Game.Busy && !Game.Frozen;
            Cursor.lockState = free ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !free;
            var dt = Time.unscaledDeltaTime;

            if (free)
            {
                var look = Keys.Look;
                yaw += look.x * 0.12f;
                pitch = Mathf.Clamp(pitch - look.y * 0.1f, -28, 68);
                var wheel = Keys.Scroll;

                if (Mathf.Abs(wheel) > 0.01f)
                {
                    distance = Mathf.Clamp(distance - Mathf.Sign(wheel) * 0.7f, 2.4f, 11);
                }

                // A held target draws the camera round behind the hero.
                if (hero.Target != null)
                {
                    var to = hero.Target.transform.position - hero.transform.position;
                    to.y = 0;

                    if (to.sqrMagnitude > 1)
                    {
                        var want = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
                        yaw = Mathf.MoveTowardsAngle(yaw, want, Mathf.Abs(Mathf.DeltaAngle(yaw, want)) * 3 * dt);
                    }
                }
            }

            var head = hero.transform.position + Vector3.up * (hero.Sitting ? 1.0f : 1.55f);
            focus = Vector3.Lerp(focus, head, 1 - Mathf.Exp(-18 * dt));
            var turn = Quaternion.Euler(pitch, yaw, 0);
            var back = turn * Vector3.back;
            var want2 = distance;

            // Walls and hills between the hero and the camera push it in.
            if (Physics.SphereCast(focus, 0.25f, back, out var hit, distance, ~0, QueryTriggerInteraction.Ignore) && hit.collider.GetComponentInParent<Actor>() == null)
            {
                want2 = Mathf.Max(0.8f, hit.distance - 0.1f);
            }

            shown = want2 < shown ? want2 : Mathf.Lerp(shown, want2, 1 - Mathf.Exp(-6 * dt));
            var at = focus + back * shown + turn * Vector3.right * 0.45f;
            var floor = Ground.SampleHeight(at) + 0.3f;

            if (at.y < floor)
            {
                at.y = floor;
            }

            if (shake > 0.001f)
            {
                at += Random.insideUnitSphere * shake * 0.35f;
                shake = Mathf.Lerp(shake, 0, 1 - Mathf.Exp(-9 * dt));
            }

            transform.position = at;
            transform.rotation = Quaternion.LookRotation(focus + turn * Vector3.right * 0.45f - at + Vector3.up * 0.1f);
        }
    }
}
