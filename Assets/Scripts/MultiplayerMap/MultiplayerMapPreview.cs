using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace J2.MultiplayerMap
{
    public sealed class MultiplayerMapPreview : MonoBehaviour
    {
        public Camera ViewCamera { get; private set; }
        public Transform ViewedArena => viewedArena;
        public MultiplayerMapLayout Layout { get; private set; }

        public const int	StartArena = 6;
        const int			PreviewLayer = 29;
        bool				stopped;

        readonly List<Camera>	suspendedCameras = new List<Camera>();
        Transform				viewedArena;

        public void Initialize(GameObject mapPrefab, int arenaIndex = StartArena, GameObject sceneMap = null)
        {
            var map = sceneMap != null ? sceneMap : Instantiate(mapPrefab, transform);

            map.transform.SetParent(transform, true);
            Layout = map.GetComponent<MultiplayerMapLayout>();

            if (Layout == null || !Layout.TryGetView(arenaIndex, out var anchor, out var target))
            {
                throw new InvalidOperationException("Multiplayer map camera anchors are missing.");
            }

            viewedArena = Layout.Arenas[arenaIndex];

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
        }

        public bool SetViewedArena(int index)
        {
            if (!Layout.TryGetView(index, out var anchor, out var target))
            {
                return false;
            }

            viewedArena = Layout.Arenas[index];
            ViewCamera.transform.SetPositionAndRotation(anchor.position, Quaternion.LookRotation(target.position - anchor.position));

            return true;
        }

        public void Shutdown()
        {
            if (stopped)
            {
                return;
            }

            stopped = true;

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

        void OnDestroy()
        {
            Shutdown();
        }
    }
}
