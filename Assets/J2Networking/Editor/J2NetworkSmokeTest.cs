using System;
using System.Threading;
using System.Threading.Tasks;
using J2.Protocol;
using UnityEditor;
using UnityEngine;

namespace J2.Networking.Editor
{
    public static class J2NetworkSmokeTest
    {
        [MenuItem("J2/Network/Check server connection")]
        public static void Run()
        {
            // Client continuations never require Unity's main thread.
            Task.Run(CheckAsync).GetAwaiter().GetResult();
            Debug.Log("[J2] NETWORK_SMOKE_OK: Unity Protobuf handshake, ping and Unicode chat roundtrip passed.");
        }
        private static async Task CheckAsync()
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            using var client = new GameClient();
            var pong = new TaskCompletionSource<S_Pong>(TaskCreationOptions.RunContinuationsAsynchronously);
            var chat = new TaskCompletionSource<S_Chat>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var registration = timeout.Token.Register(() => { pong.TrySetCanceled(); chat.TrySetCanceled(); });
            client.Received += (id, message) =>
            {
                if (message is S_Pong p) pong.TrySetResult(p);
                if (message is S_Chat c) chat.TrySetResult(c);
            };
            var welcome = await client.ConnectAsync("127.0.0.1", 9900, "Unity smoke test", timeout.Token);
            if (welcome.SessionId <= 0) throw new Exception("Invalid session ID.");
            const long timestamp = 123456789;
            const string text = "J2 연결 확인 — 유니티 🎮";
            await client.SendAsync(PacketId.CPing, new C_Ping { Timestamp = timestamp }, timeout.Token);
            if ((await pong.Task).Timestamp != timestamp) throw new Exception("Ping mismatch.");
            await client.SendAsync(PacketId.CChat, new C_Chat { Text = text }, timeout.Token);
            var received = await chat.Task;
            if (received.PlayerId != welcome.SessionId || received.Text != text) throw new Exception("Chat mismatch.");
            client.Dispose();
            await client.Completion;
        }
    }
}
