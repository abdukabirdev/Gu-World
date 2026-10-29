using GuWorld.Render;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GuWorld.World
{
    /// <summary>
    /// The builder's camera, as a strategy game's: it looks at a point on the ground (the focus) from above — straight
    /// down, north up, like a map — or around it in 3D, turned and tilted. The keys move the focus over the ground, the
    /// wheel goes nearer and farther (toward what the mouse points at), the middle button (or Space with the left) drags
    /// the ground along, the right button turns and tilts. While it is open the haze is thinned and the view goes far,
    /// and the world's squares load round the focus (ChunkStreamer.Watch), not round the hero.
    /// </summary>
    public partial class WorldBuilder
    {
        public enum View
        {
            Above,
            Around,
        }

        public View CurrentView { get; private set; } = View.Around;

        Vector3 focus;
        float viewYaw;
        float viewPitch = 52;
        float viewDistance = 90;
        bool viewReady;
        /// <summary>The ground being dragged along (the middle button, or Space with the left).</summary>
        bool panning;
        bool turning;
        Vector2 turnFrom;
        float turnTravel;
        Vector3 panGrip;
        float panPlane;
        // What the game had, given back on the way out.
        float gameFov;
        float gameFar;
        float gameNear;
        float gameFogStart;
        float gameFogEnd;
        int gameParallel;
        DaySky sky;

        const float NearestView = 4;
        const float FarthestView = 1400;

        public void SetView(View view)
        {
            if (CurrentView == view)
            {
                return;
            }

            CurrentView = view;

            // Into 3D from above: a slant that still shows the ground round the focus.
            if (view == View.Around && viewPitch > 80)
            {
                viewPitch = 52;
            }

            Touch();
        }

        void BeginView()
        {
            sky = GetComponent<DaySky>();
            gameFov = eye.fieldOfView;
            gameFar = eye.farClipPlane;
            gameNear = eye.nearClipPlane;

            if (sky != null)
            {
                gameFogStart = sky.FogStart;
                gameFogEnd = sky.FogEnd;
            }

            if (ChunkStreamer.Current != null)
            {
                gameParallel = ChunkStreamer.Current.Parallel;
                ChunkStreamer.Current.Parallel = Mathf.Max(gameParallel, 4);
            }

            // The first time: looking at what the game's camera looked at, from the same side.
            if (!viewReady)
            {
                viewReady = true;
                var forward = Rig.transform.forward;
                viewYaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
                var ray = new Ray(Rig.transform.position, forward);
                focus = GroundHit(ray, out var point) && Vector3.Distance(point, Rig.transform.position) < 400 ? point : Rig.transform.position + new Vector3(forward.x, 0, forward.z).normalized * 20;
                var y = GroundY(new Vector2(focus.x, focus.z));
                focus.y = float.IsNaN(y) ? focus.y : y;
            }

            PlaceCamera();
        }

        void EndView()
        {
            panning = false;
            turning = false;
            eye.fieldOfView = gameFov;
            eye.farClipPlane = gameFar;
            eye.nearClipPlane = gameNear;
            ChunkStreamer.Watch = null;

            if (sky != null)
            {
                sky.FogStart = gameFogStart;
                sky.FogEnd = gameFogEnd;
            }

            if (ChunkStreamer.Current != null && gameParallel > 0)
            {
                ChunkStreamer.Current.Parallel = gameParallel;
            }
        }

        /// <summary>Where the mouse points on a level plane at a height.</summary>
        bool OnPlane(float height, out Vector3 point)
        {
            var ray = MouseRay;
            point = default;

            if (Mathf.Abs(ray.direction.y) < 1e-4f)
            {
                return false;
            }

            var t = (height - ray.origin.y) / ray.direction.y;

            if (t <= 0 || t > 20000)
            {
                return false;
            }

            point = ray.GetPoint(t);

            return true;
        }

        void ViewTick(Keyboard keyboard, Mouse mouse, float dt, bool typing)
        {
            var ctrl = keyboard.ctrlKey.isPressed;
            var alt = keyboard.altKey.isPressed;
            var fast = keyboard.shiftKey.isPressed ? 3f : 1f;
            var onPage = PointerOnPage;

            // The keys: the focus over the ground (as fast as the view is far), turned with Q and E.
            if (!typing && !ctrl && !alt)
            {
                var move = Vector2.zero;
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) move.y += 1;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) move.y -= 1;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) move.x += 1;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) move.x -= 1;

                if (move != Vector2.zero)
                {
                    var speed = Mathf.Max(10, viewDistance * 0.9f) * fast;
                    focus += Quaternion.Euler(0, viewYaw, 0) * new Vector3(move.x, 0, move.y).normalized * speed * dt;
                }

                if (keyboard.qKey.isPressed) viewYaw -= 70 * fast * dt;
                if (keyboard.eKey.isPressed) viewYaw += 70 * fast * dt;

                if (keyboard.tabKey.wasPressedThisFrame)
                {
                    SetView(CurrentView == View.Above ? View.Around : View.Above);
                }

                if (keyboard.fKey.wasPressedThisFrame)
                {
                    LookAtChosen();
                }
            }

            // The wheel: nearer and farther, toward what the mouse points at (with Ctrl or Alt the wheel is the tools').
            var wheel = mouse.scroll.ReadValue().y / 120f;

            if (wheel != 0 && !ctrl && !alt && !onPage)
            {
                var was = viewDistance;
                viewDistance = Mathf.Clamp(viewDistance * Mathf.Pow(0.85f, wheel), NearestView, FarthestView);

                if (PickGround(out var toward))
                {
                    var share = 1 - viewDistance / was;
                    focus += new Vector3(toward.x - focus.x, 0, toward.z - focus.z) * share;
                }
            }

            // The middle button, or Space with the left: the ground dragged along under the mouse.
            var grab = mouse.middleButton.wasPressedThisFrame || (keyboard.spaceKey.isPressed && mouse.leftButton.wasPressedThisFrame);

            if (grab && !onPage && !typing)
            {
                panPlane = focus.y;

                if (OnPlane(panPlane, out panGrip))
                {
                    panning = true;
                }
            }

            if (panning)
            {
                if (!(mouse.middleButton.isPressed || mouse.leftButton.isPressed))
                {
                    panning = false;
                }
                else if (OnPlane(panPlane, out var now))
                {
                    var shift = panGrip - now;
                    focus.x += shift.x;
                    focus.z += shift.z;
                    // The camera moved: the grip is where it was on the ground.
                    PlaceCamera();
                }
            }

            // The right button: turned (and tilted in 3D); a click without moving drops what the tool held.
            if (mouse.rightButton.wasPressedThisFrame && !onPage)
            {
                turning = true;
                turnFrom = mouse.position.ReadValue();
                turnTravel = 0;
            }

            if (turning)
            {
                var delta = mouse.delta.ReadValue();
                turnTravel += delta.magnitude;

                if (turnTravel > 4)
                {
                    viewYaw += delta.x * 0.25f;

                    if (CurrentView == View.Around)
                    {
                        viewPitch = Mathf.Clamp(viewPitch - delta.y * 0.2f, 6, 89);
                    }
                }

                if (!mouse.rightButton.isPressed)
                {
                    turning = false;

                    if (turnTravel <= 4)
                    {
                        RightClick();
                    }
                }
            }

            // The focus stays on the ground, smoothly (it does not jump at every wall's foot).
            var ground = GroundY(new Vector2(focus.x, focus.z));

            if (!float.IsNaN(ground))
            {
                focus.y += (ground - focus.y) * (1 - Mathf.Exp(-6 * dt));
            }

            PlaceCamera();
        }

        void PlaceCamera()
        {
            var pitch = CurrentView == View.Above ? 90f : viewPitch;
            var turn = Quaternion.Euler(pitch, viewYaw, 0);
            var at = focus - turn * Vector3.forward * viewDistance;

            // Never under the ground (a low 3D view near a slope).
            var ground = GroundY(new Vector2(at.x, at.z));

            if (!float.IsNaN(ground) && at.y < ground + 1.5f)
            {
                at.y = ground + 1.5f;
                turn = Quaternion.LookRotation(focus - at, Vector3.up);
            }

            Rig.transform.SetPositionAndRotation(at, turn);
            eye.fieldOfView = CurrentView == View.Above ? 45 : 55;
            eye.farClipPlane = Mathf.Max(gameFar, viewDistance * 3 + 900);
            // Far off, a nearer limit wider too: the depth stays fine enough that the ground does not flicker.
            eye.nearClipPlane = Mathf.Clamp(viewDistance * 0.004f, gameNear, 4);
            ChunkStreamer.Watch = focus;

            // The haze thinned: from far above the ground must still be seen.
            if (sky != null)
            {
                sky.FogStart = Mathf.Max(gameFogStart, viewDistance + 200);
                sky.FogEnd = Mathf.Max(gameFogEnd, viewDistance * 2.5f + 1200);
            }
        }

        /// <summary>F: the view brought to the chosen things (or, with nothing chosen, kept where it is).</summary>
        public void LookAtChosen()
        {
            if (selection.Count == 0)
            {
                Status = "Сначала выберите что-нибудь — F покажет его";

                return;
            }

            var bounds = BoundsOf(selection);
            focus = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            viewDistance = Mathf.Clamp(Mathf.Max(bounds.size.x, bounds.size.z) * 2.2f + 12, 15, FarthestView);
        }

        /// <summary>The right button clicked (not dragged): the model taken up or the line begun put away.</summary>
        void RightClick()
        {
            if (stretchFrom != null)
            {
                stretchFrom = null;
                ClearGhosts();
            }
            else if (CurrentTool == Tool.Place || CurrentTool == Tool.Stretch)
            {
                Choose(Tool.Select);
            }
        }
    }
}
