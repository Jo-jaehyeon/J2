# Creature 상속 구조

## 객체와 책임

- `Assets/Scripts/Creatures/Creature.cs`: 공통 부모. EntityId, SpawnTypeId, 월드 좌표 Destination을 보관한다. 공통 보간 이동·회전 및 Idle/Walk/Attack/Death 애니메이션 재생을 제공한다.
- `Player.cs`: Creature 상속. Tick에서 현재 위치와 목적지의 차이를 확인하고 일정 속도로 목적지까지 이동한다.
- `Entity.cs`: Creature 상속. 준비 상태에서 SetDestination 호출 시 즉시 배치한다. 전투 상태에서는 Tick으로 목적지까지 이동한다. 기본 상태는 Preparation이다.
- `Creature.Update`가 매 프레임 Tick을 한 번 호출한다. 맵의 입력 Tick에서는 이동을 중복 갱신하지 않는다.
- `MultiplayerEntitySpawner`가 실제 스폰 객체에 Player 또는 Entity를 연결하고 Initialize로 ID를 설정한다. 기존 EntityId → GameObject 조회 API는 UI와의 호환을 위해 유지한다. TryGetCreature로 공통 부모를 조회할 수 있다.
- `PlayerController`가 내 Player 참조·우클릭/터치·바닥 좌표 계산·전장 범위 검사·포커스 해제 시 이동 취소를 담당하고 TryGetMoveWorldPoint로 월드 좌표까지만 계산하며, PointAt에는 이동 패킷 전송 TODO를 남긴다. Input Action 에셋 전환은 하지 않았다.

## 패킷 적용 지점

Unity 메인 스레드에서 호출한다. 현재 이동/전투 상태 패킷 수신 핸들러 자체를 새로 연결하지는 않았다.

```csharp
// 월드 좌표를 사용한다. 기존 스폰 패킷 X/Y는 전장 로컬 좌표이므로 혼동하지 않는다.
spawner.SetDestination(entityId, worldPosition);

// 서버 라운드 상태가 도착했을 때 명시적으로 전환한다.
spawner.SetBattleState(EntityBattleState.Combat);
spawner.SetBattleState(EntityBattleState.Preparation);

// 개별 객체에도 적용 가능하다.
if (spawner.TryGetCreature(entityId, out var creature))
{
    creature.SetDestination(worldPosition);
    creature.PlayAttackAnimation();
    creature.PlayDeathAnimation();
}
```

Preparation으로 바뀌면 진행 중 목적지에 즉시 배치한다. Combat 상태에 생성되는 새 기물도 해당 상태를 이어받는다. 중복 스폰은 목적지나 객체를 초기화하지 않는다. 사망 후 목적지 갱신과 이동은 무시하며, Initialize에서 초기화한다. 공격 재생은 현재 이동을 멈춘다. 공격·사망 재생은 클라이언트 전투 판정이 아니다.

## 애니메이션 확장

자식 클래스에서 `PlayIdleAnimation`, `PlayMoveAnimation`, `PlayAttackAnimation`, `PlayDeathAnimation`, `PlayAnimation`을 override할 수 있다. 클립 이름은 protected SerializeField이므로 자식 및 인스펙터에서 설정 가능하다. 현재 프리팹의 Legacy Animation을 사용하고 자식 모델에 있는 Animation도 찾는다. Animator를 사용하는 자식은 PlayAnimation을 재정의할 수 있다. 모델에 없는 클립은 재생하지 않는다.

## 검증

Unity에서 `Tools/Verification/VerifyCreatures.cs`, `VerifyPreservedMovement.cs`, `VerifyServerSpawn.cs` 통과. 실제 프리팹 스폰의 상속 타입·ID, 플레이어 점진 이동·도착, 기물 순간이동/보간, 목적지 변경, 준비/전투 전환, 전투 중 생성, 중복 스폰, 사망 이동 차단, 애니메이션 override 가능 여부, 기존 바닥 입력/UI 차단/카메라 전환/패킷 큐를 검증했다.

## 중복 기능 정리

- MultiplayerMapPreview에서 플레이어 생성·참조·입력·이동 위임·취소를 제거했다. 맵과 카메라만 관리한다.
- MultiplayerSceneController는 스폰된 Player를 PlayerController에 연결하고 UI 차단 정보를 전달한다. 캐릭터 이동을 직접 실행하지 않는다.
- PlayableCharacterMotor와 ArenaUnitAnimation을 삭제했다. 사용 중인 리소스 프리팹 26개를 Unity PrefabUtility로 수정하고 에셋 빌더의 생성·검증 참조도 정리했다. Source~ 아래 과거 백업은 Unity가 로드하지 않는 이력으로 보존했다.
- 기물 모델 프리팹은 외형과 Animation을 제공하며, 런타임 루트 Entity가 이를 제어한다. 자식 모델에 Entity를 중복 부착하지 않는다.
- VerifyCreatureCleanup에서 리소스 프리팹 29개에 Missing Script가 없음을 확인했다. VerifyCreatures, VerifyPreservedMovement, VerifyServerSpawn도 통과했다.
## PlayerController 입력 통합

- 기물 선택·조회, 드래그 배치/판매, 더블클릭 자동배치, 터치 및 ESC를 PlayerController로 통합했다. MultiplayerPointerInput은 삭제했다.
- 씬은 PlayerController.Tick(scale, offset)을 호출하며 직접 장치 입력을 읽지 않는다. UI는 기존 요청 콜백 및 버튼 이벤트를 유지한다.
- 내 EntityId로 스폰된 Player에 BindPlayer를 호출한다. 다른 플레이어의 스폰은 조작 대상을 바꾸지 않는다. 클릭은 해당 Player 기준의 월드 좌표까지만 계산한다. 로컬 목적지·위치를 변경하지 않으며 이동 패킷 전송은 TODO다.
- 포커스 상실·일시정지·비활성화·바인딩 변경 시 이동과 드래그 상태를 함께 취소한다.
- Input Action 에셋 도입이나 이동 패킷 연결은 추가하지 않았다. 현재 Input System 장치를 읽는 위치를 PlayerController로 모은 작업이다.

클릭 이동은 서버 연동 대기 상태다. 목적지를 수신하여 Player.SetDestination을 호출하는 코드는 별도로 연결해야 한다.
