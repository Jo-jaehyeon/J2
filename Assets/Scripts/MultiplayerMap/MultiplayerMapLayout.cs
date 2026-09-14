using UnityEngine;

namespace J2.MultiplayerMap
{
    /// <summary>Stable arena slots; player assignment belongs to server data, never list order.</summary>
    public sealed class MultiplayerMapLayout : MonoBehaviour
    {
        public const int Capacity = 8;
        public Transform[] Arenas = new Transform[Capacity];
        public Transform[] CameraAnchors = new Transform[Capacity];
        public Transform[] CameraTargets = new Transform[Capacity];
        public MultiplayerMapTrain SharedTrain;
        public bool TryGetView(int arenaId, out Transform anchor, out Transform target)
        {
            anchor = null; target = null;
            if (arenaId < 0 || arenaId >= Capacity || CameraAnchors == null || CameraTargets == null ||
                arenaId >= CameraAnchors.Length || arenaId >= CameraTargets.Length) return false;
            anchor = CameraAnchors[arenaId]; target = CameraTargets[arenaId];
            return anchor != null && target != null;
        }
    }
}
