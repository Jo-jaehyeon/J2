using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace J2.Creatures
{
    // Local input only. Creature.Update owns movement and animation ticking.
    public sealed class PlayerController : MonoBehaviour
    {

        private int	finger = -1;

        private Transform	arena;
        private Camera		viewCamera;
        private Vector2		touchStart;
        public Player Player { get; private set; }

        public void Initialize(Camera camera)
        {
            viewCamera = camera;
        }

        public void BindPlayer(Player player)
        {
            CancelMovement();
            Player = player;
            arena = player != null ? player.transform.parent : null;
        }

        public bool MoveToWorldPoint(Vector3 world)
        {
            if (arena == null || Player == null)
            {
                return false;
            }

            var local = arena.InverseTransformPoint(world);

            local.y = .25f;

            if (!Inside(local))
            {
                return false;
            }

            Player.SetDestination(arena.TransformPoint(local));

            return true;
        }

        static bool Inside(Vector3 local) => local.x * local.x / (10f * 10f) + local.z * local.z / (11.5f * 11.5f) <= 1f;

        public void PointAt(Vector2 screen, Func<Vector2, bool> blocked)
        {
            if (!enabled || Player == null || arena == null || viewCamera == null || blocked(screen))
            {
                return;
            }

            var ground = new Plane(Vector3.up, arena.TransformPoint(new Vector3(0, .25f, 0)));

            var ray = viewCamera.ScreenPointToRay(screen);

            if (ground.Raycast(ray, out var distance))
            {
                MoveToWorldPoint(ray.GetPoint(distance));
            }
        }

        public void Tick(Func<Vector2, bool> blocked)
        {
            if (!enabled || Player == null)
            {
                return;
            }

            var touch = Touchscreen.current?.primaryTouch;

            bool touching = touch != null && touch.press.isPressed;

            if (touch != null && touch.press.wasPressedThisFrame)
            {
                touchStart = touch.position.ReadValue();
                finger = blocked(touchStart) ? -1 : touch.touchId.ReadValue();
            }

            if (touch != null && touch.press.wasReleasedThisFrame)
            {
                if (finger >= 0 && touch.phase.ReadValue() != UnityEngine.InputSystem.TouchPhase.Canceled && Vector2.Distance(touchStart, touch.position.ReadValue()) < 24)
                {
                    PointAt(touch.position.ReadValue(), blocked);
                }

                finger = -1;
            }

            var mouse = Mouse.current;

            if (!touching && finger < 0 && mouse != null && mouse.rightButton.wasPressedThisFrame)
            {
                PointAt(mouse.position.ReadValue(), blocked);
            }
        }

        public void CancelMovement()
        {
            Player?.StopMovement();
            finger = -1;
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused)
            {
                CancelMovement();
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                CancelMovement();
            }
        }

        private void OnDisable()
        {
            CancelMovement();
        }
    }
}
