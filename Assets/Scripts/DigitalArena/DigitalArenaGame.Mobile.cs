using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace DigitalArena
{
    public sealed partial class DigitalArenaGame
    {
        int touchFinger = -1, suppressMouseUntilFrame = -1;
        bool touchCancelled, touchStartedOverUi, touchMoved, usedTouch;
        Vector2 touchStart;
        ArenaWorld3D.UnitView touchedUnit;
        Rect touchSafeArea;
        bool TouchControls => Application.isMobilePlatform || usedTouch;
        bool SuppressTouchMouse => touchFinger >= 0 || Time.frameCount <= suppressMouseUntilFrame;
        // Logical UI units: enough tolerance for finger jitter at any screen resolution.
        const float TouchDragThreshold = 18f;

        void OnEnable() { EnhancedTouchSupport.Enable(); }
        void OnDisable() { CancelDrag(); touchFinger = -1; EnhancedTouchSupport.Disable(); }

        void UpdateUiMetrics()
        {
            var safe = Screen.safeArea;
            if (safe.width <= 0 || safe.height <= 0) safe = new Rect(0, 0, Screen.width, Screen.height);
            uiScale = Mathf.Max(.01f, Mathf.Min(safe.width / 1440f, safe.height / 900f));
            uiOffset = new Vector2(safe.x + (safe.width - 1440 * uiScale) / 2,
                Screen.height - safe.yMax + (safe.height - 900 * uiScale) / 2);
        }

        void UpdateTouchInput()
        {
            UpdateUiMetrics();
            var touches = Touch.activeTouches;
            if (touches.Count > 0) suppressMouseUntilFrame = Time.frameCount + 2;
            if (touchFinger < 0)
            {
                // Only a fresh touch can own a gesture. Additional fingers never take over.
                foreach (var touch in touches)
                {
                    if (touch.phase != TouchPhase.Began) continue;
                    touchFinger = touch.touchId;
                    BeginTouch(UiPoint(new Vector2(touch.screenPosition.x, Screen.height - touch.screenPosition.y)));
                    return;
                }
                return;
            }
            if (Screen.safeArea != touchSafeArea) CancelDrag();
            foreach (var touch in touches)
            {
                if (touch.touchId != touchFinger) continue;
                var point = UiPoint(new Vector2(touch.screenPosition.x, Screen.height - touch.screenPosition.y));
                if (touch.phase == TouchPhase.Canceled) { CancelDrag(); touchFinger = -1; }
                else if (touch.phase == TouchPhase.Ended) { EndTouch(point); touchFinger = -1; }
                else MoveTouch(point);
                return;
            }
            CancelDrag(); touchFinger = -1;
        }

        void BeginTouch(Vector2 point)
        {
            CancelDrag();
            usedTouch = true; touchCancelled = false; touchMoved = false;
            touchStart = point; touchSafeArea = Screen.safeArea; touchedUnit = null;
            touchStartedOverUi = mainMenu || help || rules == null || rules.Health <= 0 || OverUi(point);
            if (touchStartedOverUi) return;
            touchedUnit = world.PickUnit(ScreenPoint(point));
            if (rules.Preparing && !viewingOpponent && touchedUnit != null && !touchedUnit.Enemy)
            {
                pressedId = touchedUnit.Id; mouseDown = point;
            }
        }

        void MoveTouch(Vector2 point)
        {
            if (touchCancelled) return;
            touchMoved |= Vector2.Distance(touchStart, point) > TouchDragThreshold;
            if (!touchStartedOverUi && touchMoved && pressedId >= 0)
                HandlePointer(EventType.MouseDrag, 0, 1, point);
        }

        void EndTouch(Vector2 point)
        {
            if (touchCancelled) return;
            MoveTouch(point);
            if (touchStartedOverUi)
            {
                // UI Toolkit owns the complete pointer gesture for UI controls.
                return;
            }
            if (dragId >= 0) { HandlePointer(EventType.MouseUp, 0, 1, point); return; }
            var picked = touchedUnit;
            bool tap = !touchMoved && !OverUi(point);
            CancelDrag();
            if (!tap || mainMenu || help || rules.Health <= 0) return;
            economyOpen = false;
            if (picked != null && picked.Root != null && world.Units.Contains(picked))
                selectedUnit = selectedUnit == picked ? null : picked;
            else if (picked == null)
            {
                selectedUnit = null;
                if (!viewingOpponent && playableCharacter != null && world.GroundPoint(ScreenPoint(point), out var destination))
                    playableCharacter.MoveTo(destination);
            }
        }

    }
}
