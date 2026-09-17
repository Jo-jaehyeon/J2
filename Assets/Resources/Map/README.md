# 용의 눈 호수 · 멀티플레이 맵 에셋

동일한 형태의 8개 전장(위 3개 / 중간 좌우 2개 / 아래 3개, 중앙 비움), 전장 중앙을 통과하는 공용 직사각형 철로, 기차 한 대, 호수를 둘러싼 육지·숲과 먼 산·폭포로 구성한다.

## 에셋

- `DragonEyeMultiplayer.prefab`: 전체 멀티플레이 맵. `Resources.Load<GameObject>("Map/DragonEyeMultiplayer")`로 로드한다.
- `Battlefield.prefab`: 일반 전장. 진영별 8×4 셀, 대기석 각 10칸, 카메라 위치·주시점.
- `CornerBattlefield.prefab`: 이전 참조의 호환용 이름만 유지하며 형태는 일반 전장과 동일하게 복원했다. 전체 맵의 8개 전장은 모두 `Battlefield.prefab`을 사용한다.
- `Battlefield.fbx`, `CornerBattlefield.fbx`, `LakeEnvironment.fbx`, `RectangularRailway.fbx`, `LakeTram.fbx`: Blender 모델.
- `Materials/`: Unity Built-in Standard 재질.
- `Source~/DragonEyeMultiplayer.blend`: 최종 Blender 원본. Source~는 Unity 임포트에서 제외한다.
- `Source~/Overview.png`, `LakeScenery.png`, `Battlefield.png`, `UnityOverview.png`, `UnityLakeScenery.png`: 검수 이미지.
- `Source~/References/`: 사용자가 제공한 호수 기획·메인 화면 참고 이미지.

## 배치와 카메라

표시 번호는 위에서 아래, 왼쪽에서 오른쪽의 1~8번이다. 서버 연결용 전장 ID는 0~7이며 플레이어 목록 순서와 동일시하지 않는다.

| 표시 번호 | 전장 ID | 월드 X/Z | Y 회전 |
|---|---|---|---|
| 1 | 0 | -40, 40 | 0° |
| 2 | 1 | 0, 40 | 0° |
| 3 | 2 | 40, 40 | 0° |
| 4 | 3 | -62, 0 | 90° |
| 5 | 4 | 62, 0 | 90° |
| 6 | 5 | -40, -40 | 0° |
| 7 | 6 | 0, -40 | 0° |
| 8 | 7 | 40, -40 | 0° |

중앙 (0,0)에는 전장이 없다. 철로가 전장 밖에서 회전하도록 좌우 4·5번 전장만 X ±62로 벌렸다. 위·아래 줄은 X -40/0/40을 유지하며 모든 전장의 크기·셀 간격·벤치·섬 형태가 같다. `MultiplayerMapLayout.TryGetView(arenaId, out anchor, out target)`으로 카메라 기준점을 얻는다. 4·5번의 셀/벤치/카메라도 전장과 함께 회전한다.

## 철로와 기차

철로는 8개 전장의 중앙을 직선으로 관통한다. X ±62 / Z ±40의 124×80 직사각형이며 바깥 코너 반경은 4, 전체 경로는 약 401.11유닛이다. 곡선은 전장 밖에 놓이고 1·3·6·8번도 일반 전장과 동일하다. 이전 코너 전장의 분리된 좌우 칸 배치와 넓어진 섬 형태는 사용하지 않는다. 이전 `SquareRailway.fbx`는 과거 에셋이며 현재 맵에서는 참조하지 않는다.

전체 프리팹에는 SharedTrain 하나만 존재한다. `MultiplayerMapTrain`은 3유닛/초 시각적 순환 미리보기를 제공한다. `SetAuthoritativeDistance(meters)`로 서버 경로상의 거리를 적용하면 로컬 자동 이동이 꺼진다. 서버 동기화·전투 장애물 판정·매칭/수락/로딩 UI·플레이어 목록은 이번 에셋 작업에 포함하지 않는다.

## 경관

전장 나무는 휘어진 줄기, 뿌리, 가지와 개별 잎 메시를 갖는다. 불규칙한 연속 모래 물가와 숲 지형이 호수를 둘러싸며 북쪽에 원경 산 능선과 두 곳의 다단 폭포를 배치했다. 폭포는 정적 메시이며 효과 애니메이션은 별도다. 모델은 Blender에서 제작 후 FBX로 내보내 Unity에서 재질을 연결했다.

## 검증 / 재제작

동일 원본 프리팹 8개와 동일 셀/대기석 좌표·스케일, 빈 중앙, 카메라 기준점, FBX 축과 크기, 모든 전장 중앙 관통, 4·5번 회전, 단일 기차와 순환 이음매를 확인했다. 기차 지붕 투영 영역을 경로 5,606개 자세에서 512개 셀과 대조해 겹침이 없음을 검증했다. 결과는 `Source~/unity-validation.txt`.

제작: `Tools/Map/build_multiplayer_map.py`, `natural_trees.py`, `lake_landscape.py`. Unity 프리팹 생성: `Tools/Map/build_prefabs.cs`. 검증: `Tools/Map/validate_map.cs`. 렌더: `Tools/Map/render_map.py`. MCP 연결: 기존 `Tools/Blender/mcp_execute.py`, `Tools/Map/unity_mcp.py`.

## 현재 실행 방법

빌드 시작 씬은 `Assets/Scenes/MainMenu.unity`다. 로그인, 닉네임, 게임 시작 버튼을 처리하며, 계정과 네트워크 세션은 씬 전환 후에도 유지한다.

- 멀티 버튼은 매칭 요청·응답 대기 없이 `DragonEyeLake` 씬을 비동기로 로드한다. 씬 진입 후 서버 스폰 처리는 유지한다.
- `Assets/Scenes/DragonEyeLake.unity`는 멀티플레이 전용이며 맵 프리팹과 `MultiplayerSceneController`를 포함한다. 메인 화면 컨트롤러와 싱글플레이 라운드는 생성하지 않는다.
- 씬 진입 시 `GameSceneFlow.LocalArenaIndex` 위치에 카메라를 초기화한다. 캐릭터와 기물은 `S_Spawn` 수신 시 생성한다. 서버 프로토콜에 전장 할당 필드가 아직 없어 기본값은 기존 7번 전장이다. 서버 연동 시 씬 전환 전에 `GameSceneFlow.SetLocalArenaAssignment(index)`를 호출한다. index는 0~7이다.
- 우클릭·터치 탭으로 전장 안을 이동한다. WASD·방향키 이동은 없다. Esc 또는 메인 화면 버튼은 `MainMenu` 씬으로 돌아간다.

현재 검증은 Tools/Verification/VerifyMultiplayerHud.cs, VerifyServerSpawn.cs, VerifyPreservedMovement.cs를 사용한다.

## 개발 범위 결정 (2026-09-15)

사용자 승인에 따라 싱글 맵과 로컬 전투·라운드·상점 추첨·실제 구매 로직을 삭제했다. 메인 화면과 멀티 씬만 유지한다. UI는 서버 상태 표시와 요청 콜백으로 이식했다. 상세 내용은 Docs/MultiplayerOnlyMigration.md를 참고한다.

## 서버 스폰

S_Spawn의 SpawnTypeId를 SpawnTypeTable로 조회해 생성하고 EntityId로 객체를 관리한다. X/Y는 현재 카메라 전장 기준 로컬 X/Z 좌표다. (0,0)은 전장 중앙이다. 임시 캐릭터의 자동 생성은 제거했다. 전체 플레이어에게 스폰이 전송되므로 S_EnterGame.ObjectId를 내 EntityId로 보관한다. 멀티 컨트롤러가 동일 EntityId의 스폰 객체에 우클릭·터치 조작을 자동 연결한다. 세부 사항은 Docs/SpawnTypes.md를 참고한다.
