using System.Collections;
using System.Collections.Generic;
using DigitalArena;
using J2.Networking;
using J2.Protocol;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace J2.MultiplayerMap
{
    public sealed class MultiplayerSceneController : MonoBehaviour
    {

        [SerializeField]
        MultiplayerMapLayout	sceneMap;
        public MultiplayerMapPreview World { get; private set; }

        public MultiplayerEntitySpawner Spawner { get; private set; }
        int? localEntityId;
        NetworkManager spawnNetwork;
        ArenaGameUi	ui;

        float	scale = 1;

        Font	font;
        Vector2	offset;

        void Awake()
        {
            World = gameObject.AddComponent<MultiplayerMapPreview>();
            World.Initialize(Resources.Load<GameObject>("Map/DragonEyeMultiplayer"), null, GameSceneFlow.LocalArenaIndex, sceneMap != null ? sceneMap.gameObject : null);
            spawnNetwork = NetworkManager.Instance;
            Spawner = gameObject.AddComponent<MultiplayerEntitySpawner>();
            Spawner.Initialize(World.ViewedArena);
            font = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Arial" }, 18);
            ui = new ArenaGameUi(transform, font);
        }

        void Start()
        {
            // 멀티 씬 초기화 완료 → 서버에 스폰 요청
            var network = spawnNetwork;
            if (network == null || !network.IsConnected || network.SessionId <= 0)
            {
                Debug.LogWarning("스폰 요청을 보낼 서버 세션이 없습니다.");
                return;
            }

            var table = Resources.Load<J2.Spawning.SpawnTypeTable>("SpawnTypeTable");

            var playerType = table?.entries.Find(row => row.key == "player_shintaeyil");

            if (playerType == null)
            {
                Debug.LogError("플레이어 스폰 ID가 테이블에 없습니다.");

                return;
            }

            var packet = new C_Spawn
            {
                SessionId = network.SessionId,
                SpawnTypeId = playerType.id
            };

            if (!network.SendPacket(PacketId.CSpawn, packet)) Debug.LogWarning("스폰 요청 전송 실패");
        }

        void Update()
        {
            if (GameSceneFlow.IsLoading)
            {
                return;
            }

            if (spawnNetwork != null)
            {
                while (spawnNetwork.TryDequeueSpawn(out var packet)) HandleSpawn(packet);
            }

            if (Keyboard.current?.escapeKey.wasPressedThisFrame == true)
            {
                ReturnToMenu();

                return;
            }

            World.Tick(Time.deltaTime, screen =>
            {
                var point = (new Vector2(screen.x, Screen.height - screen.y) - offset) / scale;

                return point.y < 110 || point.y > 790;
            });
        }

        public bool HandleSpawn(S_Spawn packet)
        {
            if (!Spawner.Spawn(packet)) return false;
            if (localEntityId == packet.EntityId) BindLocalEntity();
            return true;
        }

        // Call on the Unity main thread once the server identifies this client's entity.
        public void SetLocalEntityId(int entityId)
        {
            localEntityId = entityId;
            BindLocalEntity();
        }
        void BindLocalEntity()
        {
            if (localEntityId.HasValue && Spawner.TryGetEntity(localEntityId.Value,out var entity)
                && World.Player != entity.transform) World.BindPlayer(entity.transform);
        }

        void LateUpdate()
        {
            scale = Mathf.Min(Screen.width / 1440f, Screen.height / 900f);
            offset = new Vector2((Screen.width - 1440 * scale) / 2, (Screen.height - 900 * scale) / 2);
            ui.Begin(scale, offset);
            ui.Text(new Rect(32, 28, 500, 40), "내 전장", 23, Color.white, true, TextAnchor.MiddleLeft);
            ui.Button("returnMainMenu", new Rect(1216, 24, 200, 48), "메인 화면으로", new Color(.09f, .16f, .18f), ReturnToMenu, !GameSceneFlow.IsLoading);
            ui.Text(new Rect(260, 829, 920, 31), "바닥 우클릭 / 터치 이동  ·  Esc 돌아가기", 17, Color.white, false, TextAnchor.MiddleCenter);
            ui.End();
        }

        void ReturnToMenu()
        {
            World.CancelMovement();
            spawnNetwork?.ClearSpawns();
            GameSceneFlow.Load(GameSceneFlow.MainMenu);
        }

        void OnDestroy()
        {
            spawnNetwork?.ClearSpawns();
            ui?.Dispose();

            if (font != null)
            {
                Destroy(font);
            }
        }
    }
}
