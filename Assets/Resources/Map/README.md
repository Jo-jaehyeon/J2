# 용의 눈 호수 · 멀티플레이 맵 에셋

8개 전장(3×3 중앙 제외), 전장 중앙을 통과하는 공용 정사각형 철로, 기차 한 대, 호수를 둘러싼 육지·숲과 먼 산·폭포로 구성한다. 기존 싱글플레이 씬과 ArenaWorld3D는 수정하지 않았다.

## 에셋

- `DragonEyeMultiplayer.prefab`: 전체 멀티플레이 맵. `Resources.Load<GameObject>("Map/DragonEyeMultiplayer")`로 로드한다.
- `Battlefield.prefab`: 일반 전장. 진영별 8×4 셀, 대기석 각 10칸, 카메라 위치·주시점.
- `CornerBattlefield.prefab`: 코너 전장. 셀 수는 유지하고 좌우 4열을 각각 3.25유닛 바깥으로 이동하여 기차 회전 공간을 확보한다. 코너 전장 배치 좌표는 포함된 Cell/Slot Transform을 사용해야 한다.
- `Battlefield.fbx`, `CornerBattlefield.fbx`, `LakeEnvironment.fbx`, `SquareRailway.fbx`, `LakeTram.fbx`: Blender 모델.
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
| 4 | 3 | -40, 0 | 90° |
| 5 | 4 | 40, 0 | 90° |
| 6 | 5 | -40, -40 | 0° |
| 7 | 6 | 0, -40 | 0° |
| 8 | 7 | 40, -40 | 0° |

중앙 (0,0)에는 전장이 없다. `MultiplayerMapLayout.TryGetView(arenaId, out anchor, out target)`으로 카메라 기준점을 얻는다. 4·5번의 셀/벤치/카메라도 전장과 함께 회전한다.

## 철로와 기차

철로는 8개 전장의 중앙을 관통한다. X/Z ±40의 정사각형이며 코너 반경은 0.65, 전체 경로는 약 318.88유닛이다. 이전 가장자리 승강장 연결 방식은 폐기했으며 연결 데크는 제거했다. 코너 1·3·6·8번은 L자 철로와 기차 회전 여유를 위한 전용 전장이다.

전체 프리팹에는 SharedTrain 하나만 존재한다. `MultiplayerMapTrain`은 3유닛/초 시각적 순환 미리보기를 제공한다. `SetAuthoritativeDistance(meters)`로 서버 경로상의 거리를 적용하면 로컬 자동 이동이 꺼진다. 서버 동기화·전투 장애물 판정·매칭/수락/로딩 UI·플레이어 목록은 이번 에셋 작업에 포함하지 않는다.

## 경관

전장 나무는 휘어진 줄기, 뿌리, 가지와 개별 잎 메시를 갖는다. 불규칙한 연속 모래 물가와 숲 지형이 호수를 둘러싸며 북쪽에 원경 산 능선과 두 곳의 다단 폭포를 배치했다. 폭포는 정적 메시이며 효과 애니메이션은 별도다. 모델은 Blender에서 제작 후 FBX로 내보내 Unity에서 재질을 연결했다.

## 검증 / 재제작

8개 슬롯, 빈 중앙, 셀·벤치·카메라 기준점, FBX 축과 크기, 모든 전장 중앙 관통, 4·5번 회전, 단일 기차와 순환 이음매를 확인했다. 기차 지붕 투영 영역을 경로 4,538개 자세에서 512개 셀과 대조해 겹침이 없음을 검증했다. 결과는 `Source~/unity-validation.txt`.

제작: `Tools/Map/build_multiplayer_map.py`, `natural_trees.py`, `lake_landscape.py`. Unity 프리팹 생성: `Tools/Map/build_prefabs.cs`. 검증: `Tools/Map/validate_map.cs`. 렌더: `Tools/Map/render_map.py`. MCP 연결: 기존 `Tools/Blender/mcp_execute.py`, `Tools/Map/unity_mcp.py`.
