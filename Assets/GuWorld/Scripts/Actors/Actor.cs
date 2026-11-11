using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GuWorld.Actors
{
    /// <summary>The keys and the mouse, read in one place (null-safe: a device may be missing).</summary>
    public static class Keys
    {
        public static bool Down(Key key) => Keyboard.current != null && Keyboard.current[key].wasPressedThisFrame;
        public static bool Held(Key key) => Keyboard.current != null && Keyboard.current[key].isPressed;
        public static bool LeftDown => Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        public static bool LeftHeld => Mouse.current != null && Mouse.current.leftButton.isPressed;
        public static bool RightDown => Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
        public static bool MiddleDown => Mouse.current != null && Mouse.current.middleButton.wasPressedThisFrame;
        public static Vector2 Look => Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
        public static float Scroll => Mouse.current != null ? Mouse.current.scroll.ReadValue().y : 0;

        public static Vector2 Move
        {
            get
            {
                var move = Vector2.zero;
                move.y += Held(Key.W) ? 1 : 0;
                move.y -= Held(Key.S) ? 1 : 0;
                move.x += Held(Key.D) ? 1 : 0;
                move.x -= Held(Key.A) ? 1 : 0;

                return move.sqrMagnitude > 1 ? move.normalized : move;
            }
        }
    }

    /// <summary>Anything with a body that can be struck: the hero, a beast, a bandit.</summary>
    public abstract class Actor : MonoBehaviour
    {
        public static readonly List<Actor> All = new List<Actor>();

        public bool IsHero;
        public float Radius = 0.4f;
        public float Height = 1.8f;
        public abstract bool Alive { get; }
        public Vector3 Centre => transform.position + Vector3.up * Height * 0.5f;

        protected CharacterController body;
        protected RigAnimator rig;
        protected Transform model;
        protected float fallSpeed;

        protected virtual void OnEnable() => All.Add(this);
        protected virtual void OnDisable() => All.Remove(this);

        protected void MakeBody(float radius, float height)
        {
            Radius = radius;
            Height = height;
            body = gameObject.AddComponent<CharacterController>();
            body.radius = radius;
            body.height = Mathf.Max(height, radius * 2);
            body.center = new Vector3(0, body.height * 0.5f + 0.02f, 0);
            body.stepOffset = Mathf.Min(0.45f, body.height * 0.3f);
            body.slopeLimit = 55;
            body.skinWidth = 0.04f;
            body.minMoveDistance = 0;
        }

        public bool Grounded => body != null && body.isGrounded;

        /// <summary>Walks the body by a flat velocity for a frame; gravity is added here.</summary>
        protected void Walk(Vector3 velocity, float dt, float gravity = 22)
        {
            if (body == null || !body.enabled)
            {
                return;
            }

            fallSpeed = body.isGrounded && fallSpeed < 0 ? -3 : fallSpeed - gravity * dt;
            velocity.y = fallSpeed;
            body.Move(velocity * dt);
        }

        protected void Face(Vector3 direction, float degreesPerSecond, float dt)
        {
            direction.y = 0;

            if (direction.sqrMagnitude < 0.0004f)
            {
                return;
            }

            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction), degreesPerSecond * dt);
        }

        /// <summary>Puts the body somewhere at once (a door, a respawn, a blink).</summary>
        public void Teleport(Vector3 to)
        {
            if (body != null)
            {
                body.enabled = false;
            }

            transform.position = to;
            fallSpeed = 0;

            if (body != null)
            {
                body.enabled = true;
            }
        }

        public float FlatDistance(Vector3 to)
        {
            var away = to - transform.position;
            away.y = 0;

            return away.magnitude;
        }
    }
}
