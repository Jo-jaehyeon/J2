using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using Google.Protobuf;
using J2.Protocol;

namespace J2.Networking
{
    // Access Instance from Unity's main thread. Socket callbacks use their existing session.
    public class NetworkManager : MonoBehaviour
    {

        private static NetworkManager	instance;

        private static bool	quitting;
        public static NetworkManager Instance
        {
            get
            {
                if (quitting || !Application.isPlaying)
                {
                    return null;
                }

                if (instance == null)
                {
                    instance = FindAnyObjectByType<NetworkManager>();

                    if (instance == null)
                    {
                        instance = new GameObject(nameof(NetworkManager)).AddComponent<NetworkManager>();
                    }
                }

                return instance;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
            quitting = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            _ = Instance;
        }

        private readonly ServerSession	_session = new ServerSession();
        private readonly Connector		_connector = new Connector();

        [SerializeField]
        private string	host = "127.0.0.1";
        [SerializeField]
        private int		port = 9900;
        private bool	connectionStarted;
        public int SessionId => _session.SessionId;
        public bool IsConnected => _session.IsConnected;

        public void ClearMatchResult() => _session.ClearMatchResult();

        public bool TryConsumeMatchResult(out int responseSessionId, out int gameId) => _session.TryConsumeMatchResult(out responseSessionId, out gameId);

        public bool TryDequeueSpawn(out S_Spawn packet) => _session.TryDequeueSpawn(out packet);
        public void ClearSpawns() => _session.ClearSpawns();

        public bool SendPacket(PacketId packetId, IMessage packet) => _session.SendPacket(packetId, packet);

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                enabled = false;
                Destroy(this);

                return;
            }

            instance = this;
            // A persistent component must live on a root object.

            if (transform.parent != null)
            {
                transform.SetParent(null);
            }

            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (instance != this || connectionStarted)
            {
                return;
            }

            connectionStarted = true;

            var endPoint = new IPEndPoint(IPAddress.Parse(host), port);

            _connector.Connect(endPoint, () => _session);
        }

        private void OnApplicationQuit()
        {
            if (instance == this)
            {
                quitting = true;
            }
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }

            _connector.Dispose();
            _session.Disconnect();
        }
    }
}
