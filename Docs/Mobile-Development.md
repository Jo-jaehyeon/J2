# 모바일 개발

## 조작

- 아군·적 기물 탭: 정보 패널 열기/닫기. 전투 중에도 조회 가능.
- 준비 단계에서 내 기물 드래그: 전장 배치, 교환, 대기석 이동.
- 기물을 화면 하단 판매 영역으로 끌고 손을 떼기: 판매.
- 빈 바닥 탭: 테이머 이동. 상대 시점에서는 이동 불가.
- 상점, 구매, 플레이어 목록, 정보 닫기, 가이드: 탭.
- PC의 우클릭 조회/이동, 좌클릭 드래그, 더블 클릭 자동배치는 유지.

Input System Enhanced Touch를 사용한다. UI에서 시작한 터치는 전장으로 넘어가지 않고, 처음 잡은 손가락만 제스처를 제어한다. 취소, 앱 포커스 상실/일시정지, 화면 안전 영역 변경, 라운드 전환 시 드래그는 취소된다. 짧은 손떨림을 탭으로 허용하며 드래그가 판매/배치와 동시에 이동을 발생시키지 않는다. UI 버튼은 시작점과 끝점이 같은 버튼 안에 있는 탭만 한 번 처리한다.

## 설정과 빌드

2026-09-14: Unity 6000.6.0f1에 iOS Build Support 설치 완료. 실행 중인 Editor에서도 iOS 빌드 지원이 인식되는 것을 확인했다. iPhone·iPad, 최소 iOS 15.0, IL2CPP 설정이다. Xcode 프로젝트 내보내기 및 실기기 설치는 아직 검증하지 않았다.

`Digital Arena > Mobile` 메뉴:

- `Apply Mobile Settings`: 양방향 가로 회전, Android ARM64/IL2CPP 및 프레임 페이싱, iOS IL2CPP.
- `Build Android Development APK`: `Builds/Android/DigiTactics.apk` 생성.
- `Export iOS Development Project`: `Builds/iOS`에 Xcode 프로젝트 생성. iOS Build Support 필요; 기기 설치와 서명은 macOS/Xcode에서 진행.

기존 Input System 설정을 유지한다. 런타임은 60 FPS를 목표로 하며 UI는 `Screen.safeArea` 안에 배치된다. 현재 UI는 기존 1440×900 기준 레이아웃을 축소하므로 작은 휴대폰에서 글자와 버튼 크기는 실기기 점검이 필요하다. 한국어 폰트는 현재 OS 폰트를 사용하므로 Android/iOS에서 글리프 표시를 확인해야 한다.

## 검증

Unity Play 모드에서 프로젝트 루트 기준:

```powershell
unity command eval_file --file Tools/Verification/VerifyMobile.cs
unity command eval_file --file Tools/Verification/VerifyMobileDevice.cs
```

첫 검증은 테스트용 런을 생성하고 기물 탭/정보 닫기, 배치, 판매, 이동, UI 차단, 일시정지·전투 전환 취소를 확인한다. 마지막 상점 탭은 Game View가 한 프레임 그려진 후 `shopOpen=true`, `pendingUiTap=false`인지 확인한다. 두 번째 검증은 임시 Touchscreen 장치에서 실제 Input System 이벤트로 손가락 소유권, 여러 손가락 입력, 취소, 새 탭, 마우스 중복 입력 억제를 확인하고 장치를 제거한다.

실기기 최종 점검: 두 가로 방향과 노치, 구매/닫기 버튼, 작은 기물 선택, 전투 중 정보 확인, 두 손가락을 교대로 떼기, 드래그 도중 홈 화면 전환/복귀, 한국어 표시 및 프레임 속도. APK 빌드·설치와 기기 성능 검증은 별도 수행한다.
