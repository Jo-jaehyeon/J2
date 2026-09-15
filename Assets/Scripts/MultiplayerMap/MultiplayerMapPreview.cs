using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using DigitalArena;

namespace J2.MultiplayerMap
{
    public sealed class MultiplayerMapPreview : MonoBehaviour
    {
        public Camera ViewCamera { get; private set; }
        public Transform Player { get; private set; }
        public Transform ViewedArena => arena;
        public MultiplayerMapLayout Layout { get; private set; }

        public const int	StartArena = 6;
        const int			PreviewLayer = 29;
        bool				moving, stopped;
        int					finger = -1;

        readonly List<Camera>	suspendedCameras = new List<Camera>();
        Transform				arena;
        Animation				motion;
        Vector3					destination;
        Vector2					touchStart;
        public bool IsMoving => moving;

        public void Initialize(GameObject mapPrefab, GameObject playerPrefab, int arenaIndex = StartArena, GameObject sceneMap = null)
        {
            var map = sceneMap != null ? sceneMap : Instantiate(mapPrefab, transform);

            map.transform.SetParent(transform, true);
            Layout = map.GetComponent<MultiplayerMapLayout>();

            if (Layout == null || !Layout.TryGetView(arenaIndex, out var anchor, out var target))
            {
                throw new InvalidOperationException("Multiplayer map camera anchors are missing.");
            }

            arena = Layout.Arenas[arenaIndex];
            if (playerPrefab != null)
            {
                var player = Instantiate(playerPrefab, arena).transform;
                player.localPosition = new Vector3(0,.25f,-7.9f);
                BindPlayer(player);
            }

            foreach (var node in GetComponentsInChildren<Transform>(true))
            {
                node.gameObject.layer = PreviewLayer;
            }

            foreach (var cam in FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                if (cam.enabled)
                {
                    suspendedCameras.Add(cam);
                    cam.enabled = false;
                }
            }

            ViewCamera = new GameObject("Multiplayer Preview Camera").AddComponent<Camera>();
            ViewCamera.transform.SetParent(transform, false);
            ViewCamera.transform.SetPositionAndRotation(anchor.position, Quaternion.LookRotation(target.position - anchor.position));
            ViewCamera.cullingMask = 1 << PreviewLayer;
            ViewCamera.fieldOfView = 44;
            ViewCamera.nearClipPlane = .1f;
            ViewCamera.farClipPlane = 500;
            ViewCamera.clearFlags = CameraClearFlags.SolidColor;
            ViewCamera.backgroundColor = new Color(.60f, .80f, .84f);

            var sun = new GameObject("Multiplayer Preview Sun").AddComponent<Light>();

            sun.transform.SetParent(transform, false);
            sun.type = LightType.Directional;
            sun.transform.rotation = Quaternion.Euler(50, -35, 0);
            sun.intensity = 1.1f;
            sun.cullingMask = 1 << PreviewLayer;
            Pose(false);
        }

        public void BindPlayer(Transform player)
        {
            CancelMovement();
            Player = player;
            Player.SetParent(arena,true);
            var motor = Player.GetComponent<PlayableCharacterMotor>();
            if(motor != null) motor.enabled = false;
            motion = Player.GetComponent<Animation>();
            destination = Player.localPosition;
            Pose(false);
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

            destination = local;
            moving = (destination - Player.localPosition).sqrMagnitude > .0001f;

            return true;
        }

        static bool Inside(Vector3 local) => local.x * local.x / (10f * 10f) + local.z * local.z / (11.5f * 11.5f) <= 1f;

        void PointAt(Vector2 screen, Func<Vector2, bool> blocked)
        {
            if (blocked(screen))
            {
                return;
            }

            var ground = new Plane(Vector3.up, arena.TransformPoint(new Vector3(0, .25f, 0)));

            var ray = ViewCamera.ScreenPointToRay(screen);

            if (ground.Raycast(ray, out var distance))
            {
                MoveToWorldPoint(ray.GetPoint(distance));
            }
        }

        public void Tick(float dt, Func<Vector2, bool> blocked)
        {
            if (stopped || Player == null)
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

            Advance(dt);
        }

        public void Advance(float dt)
        {
            if (stopped || Player == null)
            {
                return;
            }

            bool walking = moving;

            if (moving)
            {
                Vector3 direction = destination - Player.localPosition;

                if (direction.sqrMagnitude > .0001f)
                {
                    Player.localRotation = Quaternion.RotateTowards(Player.localRotation, Quaternion.LookRotation(direction), 540f * dt);
                }

                Player.localPosition = Vector3.MoveTowards(Player.localPosition, destination, 4f * Mathf.Max(0, dt));

                if ((destination - Player.localPosition).sqrMagnitude < .0001f)
                {
                    moving = false;
                }
            }

            Pose(walking);
        }

        void Pose(bool walking)
        {
            string clip = walking ? "Walk" : "Idle";

            if (motion != null && motion.GetClip(clip) != null && !motion.IsPlaying(clip))
            {
                motion.CrossFade(clip, .12f);
            }
        }

        public void CancelMovement()
        {
            if (Player != null)
            {
                destination = Player.localPosition;
            }

            moving = false;
            finger = -1;
            Pose(false);
        }

        public void Shutdown()
        {
            if (stopped)
            {
                return;
            }

            stopped = true;
            CancelMovement();

            if (ViewCamera != null)
            {
                ViewCamera.enabled = false;
            }

            foreach (var camera in suspendedCameras)
            {
                if (camera != null)
                {
                    camera.enabled = true;
                }
            }

            suspendedCameras.Clear();
            gameObject.SetActive(false);
        }

        void OnApplicationFocus(bool focused)
        {
            if (!focused)
            {
                CancelMovement();
            }
        }

        void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                CancelMovement();
            }
        }

        void OnDestroy()
        {
            Shutdown();
        }
    }
}
