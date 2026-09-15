using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace J2.MultiplayerMap
{
    /// <summary>Visual-only movement for the one shared train. A server distance can replace local preview movement.</summary>
    public sealed class MultiplayerMapTrain : MonoBehaviour
    {

        public float	Speed = 3f;
        public bool		PreviewMovement = true;
        [SerializeField]
        float			distance;
        float[]			lengths;
        float			totalLength;

        public Transform	Train;
        public Vector3[]	Route;
        public float RouteLength
        {
            get
            {
                EnsureRoute();

                return totalLength;
            }
        }

        void Awake()
        {
            EnsureRoute();
            ApplyPose();
        }

        void Update()
        {
            if (!PreviewMovement)
            {
                return;
            }

            EnsureRoute();

            if (totalLength <= 0)
            {
                return;
            }

            distance = Mathf.Repeat(distance + Speed * Time.deltaTime, totalLength);
            ApplyPose();
        }

        public void SetAuthoritativeDistance(float meters)
        {
            if (float.IsNaN(meters) || float.IsInfinity(meters))
            {
                return;
            }

            PreviewMovement = false;
            EnsureRoute();
            distance = totalLength > 0 ? Mathf.Repeat(meters, totalLength) : 0;
            ApplyPose();
        }

        public void RebuildRoute()
        {
            lengths = null;
            EnsureRoute();
            ApplyPose();
        }

        void EnsureRoute()
        {
            if (Route == null || Route.Length < 2)
            {
                lengths = null;
                totalLength = 0;

                return;
            }

            if (lengths != null && lengths.Length == Route.Length)
            {
                return;
            }

            lengths = new float[Route.Length];
            totalLength = 0;

            for (int i = 0; i < Route.Length; i++)
            {
                lengths[i] = Vector3.Distance(Route[i], Route[(i + 1) % Route.Length]);
                totalLength += lengths[i];
            }
        }

        void ApplyPose()
        {
            if (Train == null || lengths == null || totalLength <= 0)
            {
                return;
            }

            float remaining = Mathf.Repeat(distance, totalLength);

            for (int i = 0; i < lengths.Length; i++)
            {
                if (lengths[i] <= .00001f)
                {
                    continue;
                }

                if (remaining > lengths[i])
                {
                    remaining -= lengths[i];

                    continue;
                }

                Vector3 a = Route[i], b = Route[(i + 1) % Route.Length];

                Train.position = transform.TransformPoint(Vector3.Lerp(a, b, remaining / lengths[i]));

                Vector3 direction = transform.TransformDirection(b - a).normalized;

                Train.rotation = Quaternion.LookRotation(Vector3.Cross(direction, transform.up), transform.up);

                return;
            }
        }
    }
}
