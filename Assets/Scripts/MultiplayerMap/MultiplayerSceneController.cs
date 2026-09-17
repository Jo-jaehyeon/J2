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
        public J2.Creatures.PlayerController PlayerController { get; private set; }
        public MultiplayerEntitySpawner Spawner { get; private set; }

        NetworkManager		spawnNetwork;
        MultiplayerGameUi	ui;

        int?	localEntityId;
        public MultiplayerGameUi Hud => ui;

        MultiplayerPointerInput	pointer;

        float	scale = 1;

        Font	font;
        Vector2	offset;

        void Awake()
        {
            World = gameObject.AddComponent<MultiplayerMapPreview>();
            World.Initialize(Resources.Load<GameObject>("Map/DragonEyeMultiplayer"), GameSceneFlow.LocalArenaIndex, sceneMap != null ? sceneMap.gameObject : null);
            spawnNetwork = NetworkManager.Instance;
            Spawner = gameObject.AddComponent<MultiplayerEntitySpawner>();
            Spawner.Initialize(World.ViewedArena);
            PlayerController = gameObject.AddComponent<J2.Creatures.PlayerController>();
            PlayerController.Initialize(World.ViewCamera);
            font = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Arial" }, 18);
            ui = new MultiplayerGameUi(transform, font, this);
            pointer = new MultiplayerPointerInput(this);
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

            if (!network.SendPacket(PacketId.CSpawn, packet))
            {
                Debug.LogWarning("스폰 요청 전송 실패");
            }
        }

        void Update()
        {
            if (GameSceneFlow.IsLoading)
            {
                return;
            }

            if (spawnNetwork != null)
            {
                // EnterGame identifies ownership; spawn broadcasts do not imply ownership.
                int ownedEntityId = spawnNetwork.LocalEntityId;

                if (ownedEntityId > 0 && localEntityId != ownedEntityId)
                {
                    SetLocalEntityId(ownedEntityId);
                }

                while (spawnNetwork.TryDequeueSpawn(out var packet))
                {
                    HandleSpawn(packet);
                }
            }

            if (Keyboard.current?.escapeKey.wasPressedThisFrame == true)
            {
                ReturnToMenu();

                return;
            }

            bool consumed = pointer.Tick(scale, offset);

            PlayerController.Tick(screen =>
            {
                var point = (new Vector2(screen.x, Screen.height - screen.y) - offset) / scale;

                return consumed || ui.OverUi(point);
            });
        }

        public bool HandleSpawn(S_Spawn packet)
        {
            if (!Spawner.Spawn(packet))
            {
                return false;
            }

            if (localEntityId == packet.EntityId)
            {
                BindLocalEntity();
            }

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
            if (localEntityId.HasValue && Spawner.TryGetCreature(localEntityId.Value, out var creature) && creature is J2.Creatures.Player player && PlayerController.Player != player)
            {
                PlayerController.BindPlayer(player);
            }
        }

        void LateUpdate()
        {
            var safe = Screen.safeArea;

            if (safe.width <= 0 || safe.height <= 0)
            {
                safe = new Rect(0, 0, Screen.width, Screen.height);
            }

            scale = Mathf.Max(.01f, Mathf.Min(safe.width / 1440f, safe.height / 900f));
            offset = new Vector2(safe.x + (safe.width - 1440 * scale) / 2, Screen.height - safe.yMax + (safe.height - 900 * scale) / 2);
            ui.Draw(scale, offset, Time.deltaTime);
        }

        void OnApplicationFocus(bool focused)
        {
            if (!focused)
            {
                pointer?.Cancel();
            }
        }

        void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                pointer?.Cancel();
            }
        }

        public void ReturnToMenu()
        {
            PlayerController.CancelMovement();
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
