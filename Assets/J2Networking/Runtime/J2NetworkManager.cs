using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using J2.Protocol;
using UnityEngine;

namespace J2.Networking
{
    public sealed class J2NetworkManager : MonoBehaviour
    {
        public static J2NetworkManager Instance { get; private set; }
        [SerializeField] private string host = "127.0.0.1";
        [SerializeField] private int port = 9900;
        public int SessionId { get; private set; }
        public bool IsConnected => SessionId != 0;
        public event Action<S_Chat> ChatReceived;
        public event Action<S_Pong> PongReceived;
        private GameClient _client;
        private CancellationTokenSource _lifetime;
        private readonly ConcurrentQueue<Action> _mainThread = new ConcurrentQueue<Action>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null || FindFirstObjectByType<J2NetworkManager>() != null) return;
            new GameObject("J2 Network").AddComponent<J2NetworkManager>();
        }
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        private void OnEnable()
        {
            if (Instance != this) return;
            _lifetime = new CancellationTokenSource();
            _ = RunAsync(_lifetime.Token);
        }
        private async Task RunAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                using (var client = new GameClient())
                {
                    _client = client;
                    client.Received += (id, message) => _mainThread.Enqueue(() =>
                    {
                        if (token.IsCancellationRequested) return;
                        if (message is S_Chat chat) ChatReceived?.Invoke(chat);
                        if (message is S_Pong pong) PongReceived?.Invoke(pong);
                    });
                    try
                    {
                        using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(token))
                        {
                            timeout.CancelAfter(TimeSpan.FromSeconds(5));
                            var welcome = await client.ConnectAsync(host, port, "J2 Unity", timeout.Token);
                            token.ThrowIfCancellationRequested();
                            SessionId = welcome.SessionId;
                            Debug.Log($"[J2] Connected: session {SessionId}, Protobuf v{welcome.ProtocolVersion}");
                        }
                        await client.SendAsync(PacketId.CPing, new C_Ping { Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }, token);
                        await client.Completion;
                    }
                    catch (Exception e) when (!token.IsCancellationRequested)
                    { Debug.LogWarning($"[J2] {e.GetType().Name}: server {host}:{port} unavailable; retrying in 3 seconds."); }
                    catch (Exception) when (token.IsCancellationRequested) { }
                    finally { SessionId = 0; if (_client == client) _client = null; }
                }
                try { await Task.Delay(3000, token); }
                catch (OperationCanceledException) { break; }
            }
        }
        public Task SendChatAsync(string text)
        {
            if (!IsConnected || _client == null) throw new InvalidOperationException("J2 server is not connected.");
            if (text == null || text.Length > 1024) throw new ArgumentException("Chat must be 0-1024 characters.", nameof(text));
            return _client.SendAsync(PacketId.CChat, new C_Chat { Text = text }, _lifetime.Token);
        }
        private void Update()
        {
            for (int i = 0; i < 100 && _mainThread.TryDequeue(out var action); i++) action();
        }
        private void OnDisable()
        {
            _lifetime?.Cancel();
            _client?.Dispose();
            SessionId = 0;
        }
        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
