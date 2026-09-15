using System.Collections;
using System.Collections.Generic;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace J2.Networking.Editor
{
    public static class J2NetworkSmokeTest
    {
        [MenuItem("J2/Network/Check TCP connection")]
        public static void Run()
        {
            Task.Run(() => CheckAsync(9900)).GetAwaiter().GetResult();
            Debug.Log("[J2] TCP_CONNECTION_OK");
        }

        [MenuItem("J2/Network/Check TCP locally")]
        public static void RunLoopback()
        {
            Task.Run(async () =>
            {
                var listener = new TcpListener(IPAddress.Loopback, 0);

                listener.Start();

                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));

                using var registration = timeout.Token.Register(listener.Stop);

                try
                {
                    int port = ((IPEndPoint)listener.LocalEndpoint).Port;

                    await Task.WhenAll(CheckNoPacketsAsync(listener, timeout.Token), CheckAsync(port));
                }
                finally
                {
                    listener.Stop();
                }
            }).GetAwaiter().GetResult();
            Debug.Log("[J2] TCP_ONLY_OK: connected/disconnected without sending application packets.");
        }

        [MenuItem("J2/Network/Check FindMatch locally")]
        public static void RunFindMatch()
        {
            Task.Run(async () =>
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));

                var listener = new TcpListener(IPAddress.Loopback, 0);

                listener.Start();

                using var registration = timeout.Token.Register(listener.Stop);

                using var connector = new Connector();

                var session = new ProbeSession();

                void Check(bool ok, string description)
                {
                    if (!ok)
                    {
                        throw new Exception(description);
                    }
                }

                try
                {
                    var request = new J2.Protocol.C_FindMatch
                    {
                        SessionId = 42,
                        MatchType = 0,
                        MMR = 1200
                    };

                    var requestId = J2.Protocol.PacketCodec.GetId(request);

                    Check(!session.SendPacket(requestId, request), "unconnected request accepted");

                    var accepting = listener.AcceptTcpClientAsync();

                    connector.Connect((IPEndPoint)listener.LocalEndpoint, () => session);

                    using var server = await accepting;

                    await session.Connected.Task;
                    Check(session.SessionId == 0, "session ID should initially be unassigned");

                    var entered = new J2.Protocol.S_EnterGame
                    {
                        SessionId = 42
                    };

                    session.OnRecvPacket(new ArraySegment<byte>(J2.Protocol.PacketCodec.Encode(J2.Protocol.PacketCodec.GetId(entered), entered)));
                    Check(session.SendPacket(requestId, request), "valid request rejected");
                    Check(!session.SendPacket((J2.Protocol.PacketId)999, request), "mismatched packet ID accepted");
                    Check(!session.SendPacket(requestId, null), "null packet accepted");

                    var bytes = await J2.Protocol.PacketCodec.ReadAsync(server.GetStream(), timeout.Token);

                    Check((int)J2.Protocol.PacketCodec.GetId(new ArraySegment<byte>(bytes)) == 104, "wrong packet ID");

                    var packet = J2.Protocol.PacketCodec.Parse(J2.Protocol.C_FindMatch.Parser, new ArraySegment<byte>(bytes));

                    Check(packet.SessionId == 42 && packet.MatchType == 0 && packet.MMR == 1200, "wrong packet fields");

                    var found = new J2.Protocol.S_FindMatch
                    {
                        GameId = 88
                    };

                    session.OnRecvPacket(new ArraySegment<byte>(J2.Protocol.PacketCodec.Encode(J2.Protocol.PacketCodec.GetId(found), found)));
                    Check(session.MatchedGameId == 88, "match response not recorded");
                    Check(!session.SendPacket(J2.Protocol.PacketCodec.GetId(found), found), "server packet accepted for outbound send");

                    var ping = new J2.Protocol.C_Ping
                    {
                        Timestamp = 123456
                    };

                    Check(session.SendPacket(J2.Protocol.PacketCodec.GetId(ping), ping), "second packet type rejected");

                    var pingBytes = await J2.Protocol.PacketCodec.ReadAsync(server.GetStream(), timeout.Token);

                    Check(J2.Protocol.PacketCodec.Parse(J2.Protocol.C_Ping.Parser, new ArraySegment<byte>(pingBytes)).Timestamp == 123456, "ping serialization failed");
                    session.Disconnect();
                    Check(!session.SendPacket(requestId, request), "disconnected request accepted");

                    var remainder = await J2.Protocol.PacketCodec.ReadAsync(server.GetStream(), timeout.Token);

                    Check(remainder == null, "unexpected duplicate packet");
                }
                finally
                {
                    session.Disconnect();
                    listener.Stop();
                }
            }).GetAwaiter().GetResult();
            Debug.Log("[J2] SEND_PACKET_OK: 13 checks, FindMatch and Ping TCP serialization verified.");
        }

        private static async Task CheckNoPacketsAsync(TcpListener listener, CancellationToken token)
        {
            using var socket = await listener.AcceptTcpClientAsync();

            int received = await socket.GetStream().ReadAsync(new byte[1], 0, 1, token);

            if (received != 0)
            {
                throw new Exception("Unexpected automatic packet after connection.");
            }
        }

        private static async Task CheckAsync(int port)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            using var connector = new Connector();

            var session = new ProbeSession();

            using var registration = timeout.Token.Register(() =>
            {
                session.Disconnect();
                session.Connected.TrySetCanceled();
            });

            try
            {
                connector.Connect(new IPEndPoint(IPAddress.Loopback, port), () => session);
                await session.Connected.Task;
                await Task.Delay(100, timeout.Token);
            }
            finally
            {
                session.Disconnect();
            }
        }

        private sealed class ProbeSession : ServerSession
        {

            public readonly TaskCompletionSource<bool>	Connected = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            public override void OnConnected(EndPoint endPoint)
            {
                base.OnConnected(endPoint);
                Connected.TrySetResult(true);
            }
        }
    }
}
