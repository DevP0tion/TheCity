# UI 시스템

## 상태: ✅ 구현 완료

## 개요
SharedResourceManager의 이벤트를 구독하여 자원 UI를 자동 생성/업데이트하는 시스템.
Resource ↔ UI 의존 방향: UI → Resource (단방향). Resource는 UI를 모르고 이벤트만 발행.

## 파일
- `src/UI/ResourceDisplay.cs` — 개별 자원 표시 요소 (HBoxContainer)
- `src/UI/ResourcePanel.cs` — 자원 패널 컨테이너 (PanelContainer, 이벤트 구독)
- `src/UI/ResourcePanelPatch.cs` — NCombatRoom에 패널 주입 Harmony 패치

## 구조

### ResourceDisplay
- 개별 자원의 이름 + 값을 표시하는 단위 UI
- 값 변경 시 색상 펄스 애니메이션 (흰색 → 기본색)
- `ResourcePanel`이 생성/관리

### ResourcePanel
- `SharedResourceManager`의 4개 이벤트를 구독:
  - `ValueChanged` → 해당 ResourceDisplay 업데이트
  - `ResourceRegistered` → 새 ResourceDisplay 생성
  - `Initialized` → 패널 표시, 값 리셋
  - `CleanedUp` → 패널 숨김
- 표시 이름 매핑: `ResourcePanel.DisplayNames["faith"] = "신앙";`

### ResourcePanelPatch
- `NCombatRoom._Ready()` Postfix로 패널 주입
- 위치: 좌상단 (유물 바 하단, OffsetLeft=20, OffsetTop=90)
- 중복 생성 방지: `ResourcePanel.Instance != null` 체크

## UI 위치 변경 방법
`ResourcePanelPatch.cs`의 OffsetLeft/OffsetTop 값 수정.
실제 유물 바 위치는 게임 실행 후 확인하여 조정 필요.

## 향후 개선 사항
- TheCityConfig.EnableResourceUI와 연동하여 UI 표시/숨김 제어
- 아이콘 추가 (현재 텍스트만)
- 애니메이션 강화 (증가/감소 시 색상 구분 등)
