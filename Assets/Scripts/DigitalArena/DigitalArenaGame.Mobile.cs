using UnityEngine;
namespace DigitalArena { public sealed partial class DigitalArenaGame {
        void UpdateUiMetrics()
        {
            var safe = Screen.safeArea;

            if (safe.width <= 0 || safe.height <= 0)
            {
                safe = new Rect(0, 0, Screen.width, Screen.height);
            }

            uiScale = Mathf.Max(.01f, Mathf.Min(safe.width / 1440f, safe.height / 900f));
            uiOffset = new Vector2(safe.x + (safe.width - 1440 * uiScale) / 2, Screen.height - safe.yMax + (safe.height - 900 * uiScale) / 2);
        }
}}
