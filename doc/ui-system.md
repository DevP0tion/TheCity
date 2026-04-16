# UI 시스템

## 상태: ✅ Sin 수직 스택 뷰어로 교체 완료

## 개요
플레이어가 카드를 드래그/선택하는 동안, 그 카드 바로 위에 **7대죄(Sin) 수직 스택**을 띄워 현재 보유 자원을 시인성 있게 보여준다.
데이터 계층(`SharedResourceManager`)은 유지하고 UI 계층만 Sin 전용으로 교체됐다.
Resource ↔ UI 의존 방향: UI → Resource (단방향). Resource는 UI를 모르고 이벤트만 발행.

## 파일
- `src/UI/SinDisplay.cs` — Sin 1개를 표시하는 줄 (`HBoxContainer`: 색상 원 + 값 라벨, 이름은 툴팁)
- `src/UI/SinStackPanel.cs` — 7개 `SinDisplay` 를 담는 수직 스택 (`PanelContainer` + `VBoxContainer`, 카드 좌표 추적)
- `src/UI/SinTrackerPatch.cs` — Harmony 3개 Postfix (스택 주입 + 드래그 바인딩)

## 구조

### SinDisplay
- 생성자 인자 `Sin` 으로 색상 팔레트 바인딩(고정).
- `SetValue(int)` 로 값 갱신 시 흰색 → Sin색 펄스 애니메이션 (`_Process` 에서 lerp).
- Sin 이름은 `TooltipText` (수직 스택 폭 절약).
- `MouseFilter = Ignore` — 카드 드래그 입력을 가로채지 않음.

### SinStackPanel
- `TopLevel = true` — 부모 레이아웃 무시하고 `GlobalPosition` 직접 제어.
- `_Ready` 에서 `Sin` 7종 `SinDisplay` 를 `VBoxContainer` 에 선생성.
- `SharedResourceManager.ValueChanged` / `CleanedUp` 구독 → 각각 `RefreshValues` / `Unbind` 를 `CallDeferred` 로 메인 스레드 마샬링 (네트워크 스레드 수신 대비).
- `Bind(CardModel)` / `Unbind()` — 드래그 시작/종료 훅에서 호출.
- `RefreshValues()` — 값 0 Sin은 `Visible=false`, non-zero 는 `SetValue(v)` + `Visible=true`. VBox 가 자동 축소.
- `_Process` — 바인딩된 카드의 `NCombatRoom.Instance.Ui.Hand.GetCardHolder(model).CardNode.GetGlobalRect()` 를 읽어 카드 상단 중앙 + `VerticalGap` 위에 자기 위치 갱신. 카드가 사라지면 자동 `Unbind()`.

### SinTrackerPatch
| 패치 | 타겟 | 역할 |
|---|---|---|
| `SinStackInjectPatch` | `NCombatRoom._Ready` Postfix | `__instance.Ui.AddChild(new SinStackPanel())` (z-order 보장) |
| `SinStackSelectPatch` | `HoveredModelTracker.OnLocalCardSelected` Postfix | `Instance.Bind(cardModel)` |
| `SinStackDeselectPatch` | `HoveredModelTracker.OnLocalCardDeselected` Postfix | `Instance.Unbind()` |

중복 생성 방지: `SinStackPanel.Instance != null` 체크.

## 색상 팔레트
Limbus Company 7대죄 테마 근사치. 팔레트는 `SinDisplay.Palette` 정적 딕셔너리로 UI 계층 내에 격리(Resource 계층 오염 방지).
| Sin | Color | 의도 |
|---|---|---|
| Wrath | #D12E2E | 빨강 |
| Lust | #E67E22 | 주황 |
| Sloth | #F2C40F | 노랑 |
| Gluttony | #9B59B6 | 보라 |
| Gloom | #1ABC9C | 청록 |
| Pride | #4A78BF | 남색 |
| Envy | #27AE60 | 녹색 |

## 동작 흐름
```
전투 진입
 └─ NCombatRoom._Ready → SinStackPanel 주입 (Visible=false)
 └─ CombatManager.SetUpCombat → SharedResourceManager.Initialize → Sin 전부 0

카드 드래그 시작
 └─ HoveredModelTracker.OnLocalCardSelected(card)
     └─ SinStackPanel.Bind(card) → Visible=true, 0 제외 스택 구성

매 프레임
 └─ SinStackPanel._Process → 카드 GlobalPosition 추적, 자기 위치 갱신

카드 드래그 종료 (취소/플레이)
 └─ HoveredModelTracker.OnLocalCardDeselected
     └─ SinStackPanel.Unbind → Visible=false

Sin 값 변경 (예: 카드 효과로 Wrath +2)
 └─ SharedResourceManager.ValueChanged
     └─ SinStackPanel.OnValueChanged → CallDeferred(RefreshValues)
     └─ 0↔non-zero 전환 시 스택 재구성 + 펄스 애니메이션
```

## 커스터마이즈 포인트
- 카드 위 여백: `SinStackPanel.VerticalGap` (기본 12px)
- 아이콘 크기: `SinDisplay.IconSize` (기본 14px)
- 색상 팔레트: `SinDisplay.Palette`
- Sin 순서: `Sin` enum 선언 순서 그대로 VBox에 쌓임

## 향후 개선
- 카드 효과가 요구하는 Sin만 강조(나머지는 저대비)
- Sin 간 상호작용(공명) 시각 효과
- 값이 양/음으로 변할 때 색 플래시 구분
