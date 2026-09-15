using System.Collections;
using System.Collections.Generic;
using System;
using System.Net;
using J2.Protocol;
using Google.Protobuf;

namespace J2.Networking
{
    public class ServerSession : PacketSession
    {

        private readonly object	stateLock = new object ();
        private bool			connected;
        private int				sessionId, matchedGameId;
        private bool			matchResultPending;
        private int				matchResultSessionId;
        private readonly System.Collections.Generic.Queue<S_Spawn> pendingSpawns = new System.Collections.Generic.Queue<S_Spawn>();
        public void EnqueueSpawn(S_Spawn packet)
        {
            if (packet == null) return;
            lock (stateLock) pendingSpawns.Enqueue(packet.Clone());
        }
        public bool TryDequeueSpawn(out S_Spawn packet)
        {
            lock (stateLock)
            {
                packet = pendingSpawns.Count > 0 ? pendingSpawns.Dequeue() : null;
                return packet != null;
            }
        }
        public void ClearSpawns() { lock (stateLock) pendingSpawns.Clear(); }

        public int MatchedGameId
        {
            get
            {
                lock (stateLock)
                {
                    return matchedGameId;
                }
            }
        }

        public void SetSessionId(int value)
        {
            lock (stateLock)
            {
                sessionId = value;
            }
        }

        public void SetMatchedGame(int value)
        {
            lock (stateLock)
            {
                matchedGameId = value;
                matchResultSessionId = sessionId;
                matchResultPending = true;
            }
        }

        public void ClearMatchResult()
        {
            lock (stateLock)
            {
                matchResultPending = false;
            }
        }

        public bool TryConsumeMatchResult(out int responseSessionId, out int gameId)
        {
            lock (stateLock)
            {
                responseSessionId = matchResultSessionId;
                gameId = matchedGameId;

                if (!matchResultPending)
                {
                    return false;
                }

                matchResultPending = false;

                return true;
            }
        }

        public int SessionId
        {
            get
            {
                lock (stateLock)
                {
                    return sessionId;
                }
            }
        }

        public bool IsConnected
        {
            get
            {
                lock (stateLock)
                {
                    return connected && !IsDisconnected;
                }
            }
        }

        // Accepts an unencoded protobuf message. True means queued, not server-acknowledged.
        public bool SendPacket(PacketId packetId, IMessage packet)
        {
            if (packet == null || !IsConnected)
            {
                return false;
            }

            if (!packet.Descriptor.Name.StartsWith("C_", StringComparison.Ordinal) || PacketCodec.GetId(packet) != packetId)
            {
                return false;
            }

            var buffer = PacketCodec.Encode(packetId, packet);

            if (!IsConnected)
            {
                return false;
            }

            Send(new ArraySegment<byte>(buffer));

            return true;
        }

        public override void OnConnected(EndPoint endPoint)
        {
            lock (stateLock)
            {
                connected = true;
                sessionId = 0;
                matchedGameId = 0;
                pendingSpawns.Clear();
                matchResultPending = false;
                matchResultSessionId = 0;
            }

            Console.WriteLine($"OnConnected: {endPoint}");
        }

        public override void OnDisconnected(EndPoint endPoint)
        {
            lock (stateLock)
            {
                connected = false;
                sessionId = 0;
                matchedGameId = 0;
                pendingSpawns.Clear();
                matchResultPending = false;
                matchResultSessionId = 0;
            }

            Console.WriteLine($"OnDisconnected: {endPoint}");
        }

        public override void OnRecvPacket(ArraySegment<byte> buffer) => PacketManager.Instance.OnRecvPacket(this, buffer);

        public override void OnSend(int numOfBytes)
        {
        }
    }
}
