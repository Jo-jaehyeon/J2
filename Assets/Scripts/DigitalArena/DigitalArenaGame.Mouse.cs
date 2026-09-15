using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DigitalArena
{
    public sealed partial class DigitalArenaGame
    {

        float	lastBoardClickTime = -1;

        Vector2	lastBoardClick;

        void UpdateMouseInput()
        {
            if (mainMenu || help || rules == null || rules.Health <= 0)
            {
                return;
            }

            if (Keyboard.current?.escapeKey.wasPressedThisFrame == true)
            {
                CancelDrag();
                shopOpen = false;

                return;
            }

            var mouse = Mouse.current;

            if (mouse == null || SuppressTouchMouse)
            {
                return;
            }

            var point = mouse.position.ReadValue();

            point = UiPoint(new Vector2(point.x, Screen.height - point.y));

            if (mouse.rightButton.wasPressedThisFrame)
            {
                HandlePointer(EventType.MouseDown, 1, 1, point);
            }

            if (mouse.leftButton.wasPressedThisFrame)
            {
                bool board = !OverUi(point);

                bool doubleClick = board && Time.unscaledTime - lastBoardClickTime <= .3f && Vector2.Distance(point, lastBoardClick) < 6;

                HandlePointer(EventType.MouseDown, 0, doubleClick ? 2 : 1, point);
                lastBoardClickTime = board && !doubleClick ? Time.unscaledTime : -1;
                lastBoardClick = point;
            }

            if (mouse.leftButton.isPressed && mouse.delta.ReadValue().sqrMagnitude > 0)
            {
                HandlePointer(EventType.MouseDrag, 0, 1, point);
            }

            if (mouse.leftButton.wasReleasedThisFrame)
            {
                HandlePointer(EventType.MouseUp, 0, 1, point);
            }
        }
    }
}
