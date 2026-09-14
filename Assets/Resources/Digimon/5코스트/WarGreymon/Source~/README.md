# 워그레이몬

사용자가 제공한 정면·측면·후면 이미지를 참고해 연결된 Blender 5.2.1 LTS에서 Blender MCP로 제작했습니다.

- 원본: `WarGreymon.blend`
- 재생성 코드: `build_wargreymon.py` (Blender 내부에서 실행, 새 작업 씬 생성)
- 게임 모델: 상위 폴더의 `WarGreymon_Model.fbx`
- 게임 프리팹: 상위 폴더의 `WarGreymon.prefab`
- 프리팹 Resources 경로: `Digimon/워그레이몬/WarGreymon`
- 메시: 9,592삼각형, 16개 본, 단일 스킨 메시, 13개 공용 재질
- 기준 높이: Unity 2.15 유닛, 발밑 원점, +Z 정면

## 애니메이션

30fps의 Blender 타임라인을 Unity에서 네 개의 Legacy AnimationClip으로 분리했습니다.

- Idle: 프레임 1–49, 1.6초, 반복
- Walk: 프레임 60–84, 0.8초, 제자리 걷기, 반복
- Attack: 프레임 90–108, 0.6초, 발톱 공격
- Death: 프레임 120–150, 1초, 앞으로 쓰러진 뒤 마지막 자세 유지

`ArenaUnitAnimation`이 게임 전투 상태에 따라 클립을 전환합니다. Special이 없을 때 Attack을 사용하며, Hit가 없을 때 현재 동작을 유지합니다. 사망 표시 시간은 Death 클립 길이를 따릅니다.

## Unity에 다시 적용

FBX를 수정한 후 `Digital Arena > Import WarGreymon From Blender`를 실행합니다. 이 명령은 해당 모델의 재질·애니메이션·프리팹을 갱신하고 이름이 워그레이몬인 기물에 연결합니다. 사용자 정의 수정이 있는 경우 먼저 원본을 보관하세요. FBX는 프리팹과 이름이 겹치지 않아야 Resources에서 올바르게 로드됩니다.

`Source~`는 Unity가 가져오지 않는 원본 보관 폴더입니다. .blend가 중복 임포트되는 것을 방지합니다. 모델은 메시 색상 기반이며 외부 텍스처가 필요하지 않습니다.

## 검증

- Unity 실제 임포트와 코드 컴파일 완료
- 네 클립의 관절 움직임 및 Idle/Walk 시작·끝 반복 연결 통과
- Unity 스킨 행렬로 평가한 실제 높이 2.15 검증 통과
- Unity 프리팹 렌더링으로 방향, 재질 및 면 방향 확인
- 기존 게임 규칙 검사 39,517개 통과
- Play 모드 전환 요청이 승인되지 않아 실행 중 상태 전환 검사는 미실행

`Preview.png`, `Back.png`는 최종 Blender 렌더링입니다. Unity의 게임용 셰이더는 Blender 스튜디오 조명보다 단순한 명암을 사용합니다.
