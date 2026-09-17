using System;
using System.Collections;
using System.Collections.Generic;
using J2.MultiplayerMap;
using UnityEngine;
using UnityEngine.InputSystem;

namespace J2.Creatures
{
    // Local input only. Creature.Update owns movement and animation ticking.
    public sealed class PlayerController : MonoBehaviour
    {

        private MultiplayerEntitySpawner	spawner;
        private MultiplayerGameUi			hud;
        private Action						returnToMenu;

        private int?	pressed;
        private bool	dragging, touchGesture;
        private int		finger = -1;

        private Func<Transform>	viewedArena;
        private Vector2			start;
        private Transform		arena;
        private Camera			viewCamera;
        private Vector2			touchStart;
        public Player Player { get; private set; }

        public void Initialize(Camera camera)
        {
            viewCamera = camera;
        }

        public void ConfigureInteraction(MultiplayerEntitySpawner entitySpawner, MultiplayerGameUi gameUi, Func<Transform> arenaProvider, Action exitAction)
        {
            spawner = entitySpawner;
            hud = gameUi;
            viewedArena = arenaProvider;
            returnToMenu = exitAction;
        }

        public void Tick(float scale, Vector2 offset)
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            if (Keyboard.current?.escapeKey.wasPressedThisFrame == true)
            {
                CancelMovement();
                returnToMenu?.Invoke();

                return;
            }

            if (hud == null || spawner == null || viewCamera == null)
            {
                return;
            }

            bool consumed = TickPointer(scale, offset);

            TickMovement(screen => consumed || OverUi(screen, scale, offset));
        }

        public void BindPlayer(Player player)
        {
            CancelMovement();
            Player = player;
            arena = player != null ? player.transform.parent : null;
        }

        public bool TryGetMoveWorldPoint(Vector2 screen, out Vector3 worldPosition)
        {
            worldPosition = default;

            if (!isActiveAndEnabled || Player == null || arena == null || viewCamera == null)
            {
                return false;
            }

            var ground = new Plane(Vector3.up, arena.TransformPoint(new Vector3(0, .25f, 0)));
            var ray = viewCamera.ScreenPointToRay(screen);

            if (!ground.Raycast(ray, out var distance))
            {
                return false;
            }

            var local = arena.InverseTransformPoint(ray.GetPoint(distance));
            local.y = .25f;

            if (local.x * local.x / 100f + local.z * local.z / (11.5f * 11.5f) > 1f)
            {
                return false;
            }

            worldPosition = arena.TransformPoint(local);
            return true;
        }

        public void PointAt(Vector2 screen, Func<Vector2, bool> blocked)
        {
            if (blocked(screen) || !TryGetMoveWorldPoint(screen, out var worldPosition))
            {
                return;
            }

            // TODO: Player.EntityId와 worldPosition을 사용하여 서버로 이동 요청 패킷을 전송한다.
            // 목적지는 추후 서버 수신 처리에서 설정한다. 클릭 시 로컬 목적지/위치를 변경하지 않는다.
        }

        private void TickMovement(Func<Vector2, bool> blocked)
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
            CancelGesture();
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

        private void CancelGesture()
        {
            pressed = null;
            dragging = false;
            touchGesture = false;

            if (hud != null)
            {
                hud.DragEntityId = null;
            }
        }

        private bool TickPointer(float scale, Vector2 offset)
        {
            var touch = Touchscreen.current?.primaryTouch;

            if (touch != null && touch.press.wasPressedThisFrame)
            {
                touchGesture = true;
                Begin(touch.position.ReadValue(), scale, offset);
            }

            if (touchGesture)
            {
                if (touch != null && touch.press.isPressed)
                {
                    Drag(touch.position.ReadValue());
                }

                if (touch != null && touch.press.wasReleasedThisFrame)
                {
                    bool used = pressed.HasValue;

                    if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Canceled)
                    {
                        CancelGesture();
                    }
                    else
                    {
                        End(touch.position.ReadValue(), scale, offset, true);
                    }

                    return used;
                }

                return pressed.HasValue;
            }

            var mouse = Mouse.current;

            if (mouse == null)
            {
                return false;
            }

            var point = mouse.position.ReadValue();

            if (mouse.rightButton.wasPressedThisFrame && !OverUi(point, scale, offset))
            {
                var id = Pick(point);

                if (id.HasValue)
                {
                    hud.RequestDetails(id.Value);

                    return true;
                }
            }

            if (mouse.leftButton.wasPressedThisFrame)
            {
                Begin(point, scale, offset);
            }

            if (mouse.leftButton.isPressed)
            {
                Drag(point);
            }

            bool consumed = pressed.HasValue;

            if (mouse.leftButton.wasReleasedThisFrame)
            {
                End(point, scale, offset, false, mouse.clickCount.ReadValue());
            }

            return consumed;
        }

        bool OverUi(Vector2 p, float scale, Vector2 offset) => hud.OverUi((new Vector2(p.x, Screen.height - p.y) - offset) / scale);

        int? Pick(Vector2 screen)
        {
            // Renderer bounds work for models without gameplay colliders.
            var ray = viewCamera.ScreenPointToRay(screen);

            float nearest = float.MaxValue;

            int? found = null;

            foreach (var pair in spawner.Entities)
            {
                if (pair.Value == null || !pair.Value.activeInHierarchy)
                {
                    continue;
                }

                foreach (var renderer in pair.Value.GetComponentsInChildren<Renderer>())
                {
                    if (renderer.enabled && renderer.bounds.IntersectRay(ray, out var distance) && distance < nearest)
                    {
                        nearest = distance;
                        found = pair.Key;
                    }
                }
            }

            return found;
        }

        void Begin(Vector2 p, float scale, Vector2 offset)
        {
            pressed = OverUi(p, scale, offset) ? null : Pick(p);
            start = p;
            dragging = false;
        }

        void Drag(Vector2 p)
        {
            if (pressed.HasValue && Vector2.Distance(p, start) > 18)
            {
                dragging = true;
                hud.DragEntityId = pressed;
            }
        }

        void End(Vector2 p, float scale, Vector2 offset, bool touch, int clickCount = 0)
        {
            if (pressed.HasValue)
            {
                var logical = (new Vector2(p.x, Screen.height - p.y) - offset) / scale;

                if (dragging)
                {
                    if (logical.y >= 790)
                    {
                        hud.RequestSell(pressed.Value);
                    }
                    else if (!hud.OverUi(logical))
                    {
                        var plane = new Plane(Vector3.up, viewedArena().position);

                        var ray = viewCamera.ScreenPointToRay(p);

                        if (plane.Raycast(ray, out var distance))
                        {
                            hud.RequestPlacement(pressed.Value, ray.GetPoint(distance));
                        }
                    }
                }
                else if (touch)
                {
                    hud.RequestDetails(pressed.Value);
                }
                else if (clickCount >= 2)
                {
                    hud.RequestAutoDeploy(pressed.Value);
                }
            }

            CancelGesture();
        }
    }
}
