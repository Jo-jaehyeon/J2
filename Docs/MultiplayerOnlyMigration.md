# 멀티플레이 전용 전환

## 사용자 요청

싱글 맵, 클라이언트 기물 전투, 라운드별 기물 갱신 및 상점 추첨·실제 구매 로직을 제거한다. 마우스 이동과 UI는 멀티로 이식한다. 구매 UI는 서버의 5개 항목을 표시하고 클릭 함수/요청 콜백만 실행한다.

## 현재 적용 및 검증 완료

- `MultiplayerUiState`: 서버가 전달하는 상점 5칸, 플레이어 목록, 라운드 표시, 경제, 기물 정보·체력, 결과 화면 데이터.
- `MultiplayerGameUi`: 기존 주요 UI 영역을 멀티 씬에 이식. `ApplyServerState(state)` / `ApplyShopOffers(offers)`로 표시 갱신. 입력 상태는 복사해 보관한다.
- `Buy(slot)`: `PurchaseRequested(slot, offerId)`만 호출한다. 골드 차감, 기물 추가, 추첨, 합성은 하지 않는다.
- `RerollRequested`, `BuyXpRequested`, `SellRequested`, `AutoDeployRequested`, `PlacementRequested`, `UnitDetailsRequested`, `PlayerViewRequested`, `RestartRequested`를 네트워크 패킷 송신에 연결할 수 있다. 대응 프로토콜이 아직 없어 새 패킷을 임의로 만들지 않았다.
- `MultiplayerPointerInput`: 기물 우클릭/터치 조회, 드래그 배치·판매 요청, 더블클릭 자동배치 요청. 로컬 기물 상태를 변경하지 않는다.
- 기존 빈 바닥 우클릭·터치 캐릭터 이동은 `MultiplayerMapPreview`에 유지. `S_EnterGame.ObjectId`를 저장하고 같은 `S_Spawn.EntityId` 객체에 자동 연결한다.
- `UnitModelView`: 기존 기물 표시용 모델 생성만 추출. `SpawnTypeTable.Spawn`은 이 클래스를 사용하므로 싱글 맵 생성 없이 모델을 생성한다.
- 플레이어 목록은 서버의 arenaIndex로 카메라만 전환한다. 캐릭터 배속 전장은 바꾸지 않는다.

검증: `VerifyMultiplayerHud.cs`의 실제 구매 버튼 5개, 콜백 ID, 상태 불변, 5칸 검증 통과. `VerifyServerSpawn.cs`의 모델 생성·좌표·EntityId·중복 검증 통과. `VerifyPreservedMovement.cs`의 바닥 좌표 이동·UI 차단·전장 카메라 전환 검증 통과.

## 싱글 제거 완료

사용자의 진행 승인 후 다음 정리를 완료했다.

- `DigitalArenaGame`은 로그인·메인 화면·멀티 진입만 담당한다. 싱글 버튼과 로컬 전투/라운드/구매/판매/배치 처리를 제거했다.
- 싱글 마우스 partial은 삭제했다. 모바일 파일에는 메인 UI의 안전 영역 계산만 남겼다. 멀티의 마우스·터치 이동과 기물 입력 요청은 별도 코드로 보존했다.
- `SinglePlayer.unity`를 삭제하고 빌드 씬을 `MainMenu`, `DragonEyeLake`로 통일했다.
- `ArenaRules`, `ArenaBattle`, `ArenaTrain`, `ArenaWorld3D`, `ArenaSmokeTest`, `DigitalArenaValidation`을 삭제했다.
- `ArenaUnitAnimation`은 서버 표시 명령으로 애니메이션을 재생하는 역할만 유지한다. 타깃 선택·피해·생사 판정은 없다.
- 기물 카탈로그·프리팹·계정·서버 통신을 보존했다. 데이터 편집기의 로컬 구매·판매 가격 계산은 제거했다. ArenaBalance는 편집용 정적 테이블이며, 런타임 상점 추첨에는 사용하지 않는다.
- PC/모바일 빌드는 MainMenu에서 시작해 DragonEyeLake로 전환한다.

삭제 후 검증: UI 구매 버튼 5개와 콜백/상태 불변, 모델 스폰/EntityId, 마우스 목적지 이동/UI 차단/카메라 전환 통과. 실제 Play에서 싱글 버튼 부재와 멀티 씬 진입·서버 데이터 UI 표시를 확인했다. `Assets`와 `ProjectSettings`에서 삭제 타입/씬 참조가 없음을 확인했다.

## 서버 연동 예시

```csharp
// Unity 메인 스레드에서 수신 데이터를 UI에 적용한다.
controller.Hud.ApplyServerState(serverState);
controller.Hud.ApplyShopOffers(fiveOffers); // 정확히 5칸, 빈 칸은 null
controller.Hud.PurchaseRequested += (slot, offerId) => {
    // 정의될 구매 요청 패킷 전송
};
```

구매·리롤·경험치·배치·판매 UI 요청 콜백은 보존되어 있다. 해당 패킷 정의가 제공되면 송신을 연결한다. 콜백만으로 골드/기물/상점 상태를 바꾸지 않는다. `S_EnterGame.ObjectId`를 `ServerSession.LocalEntityId`에 저장한다. 멀티 컨트롤러는 Unity 메인 스레드에서 이 값을 읽고 `SetLocalEntityId`로 스폰 객체와 이동 입력을 연결한다. 입장/스폰의 처리 순서가 달라도 연결되며 다른 플레이어 스폰으로 조작 대상이 바뀌지 않는다.

기존 Tools의 싱글·씬 분리 이전 검증 스크립트는 과거 작업 기록이며 현재 검증 대상이 아니다. 현재 검증은 `VerifyMultiplayerHud.cs`, `VerifyServerSpawn.cs`, `VerifyPreservedMovement.cs`를 사용한다.
