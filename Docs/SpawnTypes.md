# 스폰 ID 테이블

## 편집

Unity 메뉴 **Digital Arena > Spawn ID Table**을 열면 `Assets/Resources/SpawnTypeTable.asset`이 Inspector에 표시된다. Entries에서 행 추가·삭제, ID, 등록 키, 표시 이름, 분류, 기물 데이터 ID, 프리팹을 수정할 수 있다. 유효한 변경은 자동 저장·내보내기된다. 상태 메시지에서 동기화 성공 여부를 확인한다. `검증 / 서버로 내보내기` 버튼으로 다시 실행할 수 있다.

- ID: 서버 패킷 `ObjectId`에 쓰는 종류 ID. 배열 순서가 바뀌어도 유지된다.
- Key: 영문·숫자·밑줄로 구성된 고유 등록 키. 기존 항목의 키를 유지한다. 미등록 기물 가져오기에서 항목 식별에 사용한다.
- Kind: Player / Ally / Creep.
- Catalog ID: 기존 디지몬 데이터 테이블의 문자열 ID. 플레이어는 비워 둔다.
- Prefab: Unity 프리팹 직접 참조. 기물에서 비어 있으면 Catalog ID로 기존 코드 모델 생성 경로를 사용한다.
- `기존 기물 중 미등록 항목 추가`: 새 디지몬 데이터 행을 가져온다. 이미 등록된 키의 숫자 ID와 프리팹 설정은 덮어쓰지 않는다.

초기 등록: 신태일 0, 아군 1001~1020, 크립 2001~2006. 앞으로 추가한 값도 한 번 배포한 뒤에는 가급적 유지한다. 실행 중인 게임은 재시작하여 변경을 반영한다.

## 원본과 서버 동기화

원본은 Unity의 SpawnTypeTable.asset 한 곳이다. 서버 파일은 직접 수정하지 않는다.

- 서버 `Common/Generated/SpawnTypes.json`: ID·키·이름·분류·기물 데이터 ID·Unity 에셋 경로.
- 서버 `Common/Generated/SpawnTypes.h`: 같은 ID 목록, C++ 상수와 ID 조회 함수.
- Unity `Docs/Generated/SpawnTypes.json`: 서버 JSON과 동일한 검토용 사본.

기본 서버 위치는 J2 프로젝트에서 `../../CPP_Server/J2_Server`이다. 경로가 바뀌면 SpawnTypeTableEditorData.Export의 경로를 조정한다. 자동 내보내기는 이 PC의 서버 저장소 파일을 갱신하며, 실행 중인 서버를 재시작하거나 원격 서버에 배포하지 않는다. 헤더를 사용하는 서버는 다시 빌드해야 한다. 서버가 JSON을 런타임에 읽으려면 별도 로더가 필요하다.

```cpp
#include "Common/Generated/SpawnTypes.h"

const auto playerTypeId = J2SpawnTypes::Id::Type_player_shintaeyil;
const auto* type = J2SpawnTypes::Find(packet.object_id());
```

## 클라이언트 ID 조회와 생성

```csharp
var table = Resources.Load<SpawnTypeTable>("SpawnTypeTable");
var catalog = JsonUtility.FromJson<DigimonCatalog>(Resources.Load<TextAsset>("DigimonCatalog").text);

// Unity 메인 스레드에서 실행. 위치는 부모 Transform 기준이다.
var spawned = table.Spawn(packet.ObjectId, arenaTransform, localPosition, catalog);
```

Spawn은 등록 프리팹 또는 기존 기물 모델을 생성하며, 패킷 수신 스레드에서는 호출하면 안 된다. 멀티맵 C_Spawn 요청은 등록 키 player_shintaeyil의 ID를 참조한다.

S_Spawn 수신은 ServerSession 큐에 보관하고 MultiplayerSceneController.Update에서 메인 스레드로 처리한다. SpawnTypeId로 테이블을 조회하여 객체를 생성하고 EntityId로 중복 생성을 방지한다. 패킷의 X/Y는 현재 카메라가 보고 있는 전장 기준 로컬 X/Z 좌표로 적용한다. (0,0)은 해당 전장 중앙이다. 플레이어 높이는 0.25, 기물은 0으로 적용한다. 연결 종료와 멀티 씬 종료 시 대기 큐를 비운다.

S_Spawn은 같은 게임의 모든 클라이언트에게 전달된다. 현재 소유자 필드가 없어 SpawnTypeId나 수신 순서로 내 캐릭터를 추측하지 않는다. 자동으로 생성하던 임시 플레이어는 제거했다. S_EnterGame.ObjectId를 ServerSession.LocalEntityId에 보관한다. 멀티 컨트롤러가 메인 스레드에서 이 값을 읽어 SetLocalEntityId를 호출하고 동일한 S_Spawn.EntityId 객체에 기존 우클릭·터치 조작을 자동 연결한다. 입장 응답과 스폰의 처리 순서가 달라도 연결된다. 연결 종료 시 소유 ID를 초기화한다.

### EntityId로 객체 찾기

`MultiplayerSceneController.Spawner`가 멀티 씬 수명 동안 `EntityId -> GameObject`를 관리한다. 같은 SpawnTypeId라도 EntityId가 다르면 별도 객체다. 중복 S_Spawn은 기존 객체를 유지한다. 이후 S_Move 처리 시 `Spawner.TryGetEntity(packet.EntityId, out var entity)`로 대상을 찾는다. 등록 목록은 읽기 전용 `Spawner.Entities`로 확인할 수 있다. S_Move의 위치 적용은 아직 연결하지 않았다.
