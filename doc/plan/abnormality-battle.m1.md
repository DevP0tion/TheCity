# Plan M1: abnormality-battle (구현 직전 사양서)

**Feature:** abnormality-battle
**상태:** M1 정적 조사 완료 / 구현 미시작 / 사용자 결재 진행 중 — (c) 채택 (2026-04-25), (a)/(b)/(d-1 배경)/(d-2 보스) 답 대기. 보강 #11/#12 (2차) 반영 후 (d) 가 배경/보스 분리, Mon-NONE 위험 명시.
**작성:** 2026-04-25
**관련 문서:**
- `abnormality-battle.plan.md` (Plan v1.1 — 본 사양서가 그 위에 차분 적용)
- `abnormality-battle.verification.md` (2026-04-22 검증 리포트)
- `abnormality-map-node.md` (선행 맵 노드 시스템 — 이미 구현됨)

> 본 문서는 `Plan v1.1` 본문을 덮어쓰지 않는 별도 사양서. Plan v1.1 의 ⚠ 블록을 한 단계 더 구체화한 "구현 직전 사양". `abnormality-map-node.md` 의 M1–M4 진화 패턴을 따른다 — Plan 본문은 맥락 보존을 위해 그대로 두고, 구현은 본 M1 문서를 따른다.

> ⚠ **이 세션에서는 m1.md 작성 + 사용자 결재 보고까지가 끝.** 구현 단계 (mod-implementer 호출 / `src/Abnormality/` 코드 작성 / `dotnet build` / `assets/` 신규 자산 추가) 는 사용자 결재 + 후속 세션에서 진행한다.

---

## Executive Summary

| 관점 | 요약 |
|------|------|
| **Problem** | Plan v1.1 의 게임 API 가정 8건 (M1-1~M1-8) 이 정적 미검증 상태였음. 구현 진입 시 컴파일·런타임 에러 위험. |
| **Solution** | sts-game-analyst 의 게임 코드 정적 조사 7건 + web-researcher 의 BaseLib 외부 조사 1건 으로 8건 모두 결론 도달. Plan v1.1 의 ⚠ 블록을 한 단계 더 구체화한 본 사양서 작성. |
| **Outcome** | 구현 시 사용할 정확한 시그니처·경로·메커니즘 모두 코드 인용으로 확정. v1 PoC 진입 직전 사용자 결재 4건 (a/b/c/d) 만 받으면 §7 구현 단계로 이행 가능. |
| **Residual Risk** | 정적 한계 7건은 Wave 2 runtime-qa 영역으로 이관. 단 v1 권장 결정 (배경 v2 이관 + Custom*Model 트리오 + extraRewards) 채택 시 runtime-qa 의존성 거의 zero. |

---

## 사용자 결재 항목 (4건)

> **이 4건이 결재 받기 전엔 §7 구현 단계 진입 금지.** 각 항목의 권고안은 본 M1 조사로 도출된 가장 단순·안전한 경로. 사용자가 다른 옵션을 선택하면 m1.md 갱신 후 재보고.

### (a) 클래스명 prefix 규약

| 옵션 | 결과 `Id.Entry` | 평가 |
|---|---|---|
| **권장 ★** A1: `EbonyQueenAppleEncounter` (BaseLib `Custom*Model` 트리오 사용, 클래스명 prefix **없음**) | `THECITY-EBONY_QUEEN_APPLE_ENCOUNTER` (BaseLib 자동 prefix `THECITY-` 부착) | 깔끔. BaseLib `ICustomModel` 자동 prefix 가 모드 격리 + 사일런트 충돌 방지. |
| A2: `TheCityEbonyQueenAppleEncounter` (Custom*Model + 클래스명 prefix `TheCity` 함께) | `THECITY-THE_CITY_EBONY_QUEEN_APPLE_ENCOUNTER` | 이중 prefix, 가독성 떨어짐. 의미상 중복. |
| A3: `EncounterModel` 직접 상속 (BaseLib 미사용) + 클래스명 prefix `TheCity` 강제 | `THE_CITY_EBONY_QUEEN_APPLE_ENCOUNTER` | BaseLib 자동 등록·Harmony 인프라 포기. ModelDb 직접 등록 필요. 비추천. |

**근거:**
- M1-1 (web-researcher): BaseLib 가 `CustomEncounterModel` / `CustomEventModel` / `CustomMonsterModel` 트리오 제공, `ICustomModel` marker 의 자동 prefix 메커니즘 (`PrefixIdPatch`).
- M1-7 (sts-game-analyst): 바닐라 EventModel/AncientEventModel 76개 슬러그 전수 — `ABNORMALITY*` / `THE_CITY_*` / `THECITY-*` 모두 충돌 0건.
- 본 모드 루트 네임스페이스: `TheCity` (확인됨, `src/ModStart.cs:8`).
- BaseLib `TypePrefix.GetPrefix()` 알고리즘: 네임스페이스 첫 단어 (`.` 앞) 대문자 + `"-"`. → `TheCity.*` → `"THECITY-"`.

### (b) v1 BGM 전략

| 옵션 | 평가 |
|---|---|
| **권장 ★** B1: `CustomBgm => ""` (빈 문자열, 게임 기본 BGM 유지) | v1 자산 미준비. v2 에서 `event:/music/...` FMOD 경로 + `.ogg` 자산 추가 시 단순 override. BaseLib `FmodAudio` 인프라가 이미 있어 v2 작업 한 줄. |
| B2: v1 부터 커스텀 BGM 추가 | `.ogg` 자산 + FMOD 이벤트 경로 + 자산 디자인 작업 별개. v1 PoC 핵심 (전투 진입·종료 흐름) 검증과 무관한 작업 분량. |

**근거:**
- M1-1 (web-researcher): BaseLib `Utils/FmodAudio` 정적 클래스가 30+ FMOD 메서드 제공 (`PlayEvent` / `PlayFile` / `PreloadMusic` / `LoadBank` / `RegisterFileReplacement` 등). `CustomBgm` override 만으로 게임 측 자동 처리.
- 위키 HellEncounter 예제: `public override string CustomBgm => "event:/music/act3_boss_queen";` 한 줄.
- verification.md §9 의 "BaseLib audio API 없음" 결론은 **오류 정정** — 실제 풍부함.

### (c) 보상 UI 호출 위치 — **사용자 결재 완료 (2026-04-25): C1-v1 채택**

**v1 권고 (결재 확정):**
```csharp
// AbnormalityEvent.cs 의 선택지 콜백
EnterCombatWithoutExitingEvent<EbonyQueenAppleEncounter>(
    Array.Empty<Reward>(),           // ← v1 은 빈 배열
    shouldResumeAfterCombat: true);
```
- `EncounterModel.ShouldGiveRewards => true` (default 유지, 명시 override 불필요).
- `extraRewards: Array.Empty<Reward>()` (또는 메서드 오버로드로 인자 생략 가능 시 생략).
- 결과: **표준 보상만** 표시 = Gold + 카드 3장 + 옵션 Potion (M1-6 결합 매트릭스의 `true × 빈 배열` 행).

**v1 → v2 마이그레이션 비용 = 0 (인자만 채우면 됨):**
```csharp
// v2 EGO 카드 도입 시 — 같은 호출의 첫 인자만 변경
EnterCombatWithoutExitingEvent<EbonyQueenAppleEncounter>(
    new[] { new CardReward(egoOptions, 3, base.Owner) },   // ← 빈 배열 → EGO CardReward
    shouldResumeAfterCombat: true);
```
- 메커니즘 A (extraRewards 패턴) 자체는 v1/v2 동일. 코드 구조 변경 0.
- v1 단계에서 `egoOptions` 정의·EGO 카드 풀·`EgoCardPool` 클래스 모두 작성 안 함 (v2 별도 plan).

**옵션 비교 (참고):**
| 옵션 | 평가 |
|---|---|
| **권장 ★ (결재 확정)** C1-v1: `ShouldGiveRewards=true` + `extraRewards: Array.Empty<Reward>()` (표준 보상만) | v1 PoC 핵심 (이벤트→전투→결과) 검증에 집중. 인자만 채우면 v2 EGO 도입 가능 — 코드 구조 변경 0. |
| C1-v2: `ShouldGiveRewards=true` + `extraRewards: [new CardReward(egoOptions, 3, player)]` | v2 EGO 카드 도입 시 활성화. 메커니즘 A 동일 구조. |
| C2: `ShouldGiveRewards=false` + Resume 에서 `RewardsCmd.OfferCustom` 직접 호출 (BattlewornDummy 패턴) | 표준 보상 풀 차단. 사용자 요구 ("기본 보상 UI 로 처리") 와 불일치. 채택 안 함. |
| C3: `TryModifyRewards` override (PowerModel 또는 RelicModel 측) | 환상체별 차등 보상 시 유연. v1 단순 PoC 에 과잉. v2 보류. |

⚠ **함정 (반드시 회피, v1/v2 공통):**
| ShouldGiveRewards | extraRewards | 결과 |
|---|---|---|
| **true** | **빈 배열** | **표준 보상만 (← v1 정답)** |
| true | non-empty | 표준 + extraRewards 합성 (← v2 정답) |
| false | 빈 배열 | 빈 보상 화면 |
| false | non-empty | ⚠ **extraRewards 침묵 무시** (`RewardsSet.EmptyForRoom` 분기에 합성 코드 없음) |

> **EGO 카드 시스템은 별도 plan (미작성)**. v2 진입 시점에 `doc/plan/ego-card-system.md` 같은 별도 plan 작성 후 그 plan 의 결재를 거쳐 EGO 카드 풀·CardCreationOptions 정의·CardReward 슬롯 통합 진행. 본 m1.md 의 (c) 결재는 v1 표준 보상까지만 다룸.

**근거:**
- M1-6 / M1-8 (sts-game-analyst): `RewardsCmd.OfferForRoomEnd` 분기 (`RewardsCmd.cs:11-25`), `RewardsSet.WithRewardsFromRoom` ExtraRewards 합성 (`RewardsSet.cs:64-67`), `EnterCombatWithoutExitingEvent` 의 `combatRoom.AddExtraReward` 호출 흐름 (`EventModel.cs:458-491`).
- BattlewornDummy 가 C2 패턴 쓴 이유: Setting 별 보상 유형 완전 분리 (Potion / 카드 업그레이드 즉시 / Relic 즉시) → 표준 RoomType=Monster 보상과 어느 setting 도 부합 안 함. **환상체전과 무관**.
- 사용자 결재 (2026-04-25): C1 채택, v1 단계는 EGO 미포함 (별도 plan), v1↔v2 코드 구조 변경 0.

### (d) v1 시각 자산 — 배경 / 보스 분리

> 사용자 자산 폴더 `assets/sprites/Abnormalities/Ebony Queen's Apple/` 에 `Background.png` (1000×600) + `Boss.png` (512×507) 이미 존재. 보강 #12 (web-researcher) 결과로 두 자산을 **분리 결재**:

#### (d-1) 배경 자산

| 옵션 | 평가 |
|---|---|
| **권장 ★** Bg-NONE: v1 배경 미포함 (`CustomEncounterBackground` override 안 함) | BaseLib `_customBackgroundAssets == null` → BaseLib override `HasCustomBackground => false` → 게임이 `parentAct.GenerateBackgroundAssets(rng)` 자동 폴백 → 부모 act 기본 배경 사용. **자산 0, 안전, CTD 위험 없음**. (배경은 `MonsterModel` 과 달리 BaseLib 의 `HasCustomBackground` override 가 자동 폴백을 보장.) |
| Bg-tscn: v1 부터 배경 자산 포함 | **단일 PNG 헬퍼 없음** (보강 #12 확인). `CustomBackgroundAssets` 가 디렉토리 + `_bg_<group>_*` / `_fg_*` 강제 + `*_background.tscn` 강제. 사용자 `Background.png` 그대로는 통과 안 함. 자산 정리 (`bg_default_1.png` 리네임 + 더미 `fg_empty_1.png` 1×1 투명 + 빈 `_background.tscn`) 후 사용 가능. v2 작업량 < 30분 (자산 정리 + override 한 줄). |

**v1 권장 = Bg-NONE** — 자산 정리 비용 회피 + act 기본 배경으로 PoC 충분. 배경은 `MonsterModel` 과 달리 자동 폴백이 안전 — Mon-NONE 위험과는 다름.

#### (d-2) 보스 시각화 — **v1 부터 자산 + 코드 둘 다 필수**

| 옵션 | 평가 |
|---|---|
| **권장 ★** Mon-PNG: 단일 PNG 직접 사용 — `CustomMonsterModel.CreateCustomVisuals()` override + `NodeFactory<NCreatureVisuals>.CreateFromResource(pngPath)` 한 줄 | BaseLib 가 공식 지원. 위키 명시: "A single PNG image is enough to create creature visuals." 사용자 `Boss.png` 그대로 v1 부터 사용 가능. 코드 4줄, .tscn 작성 0개. v1 PoC 정답. |
| Mon-tscn: `.tscn` 기반 visuals (Spine 또는 다중 노드) | 애니메이션·다부위 표현 가능. v2 다부위 환상체 도입 시 적합. v1 비용 큼. |
| ⚠ Mon-NONE: 자산·코드 둘 다 미포함 (`CreateCustomVisuals` null + `Boss.png` 미동봉) | `CreateCustomVisuals` null → 게임 본체 `VisualsPath` 폴백 → 기본 경로 (`res://scenes/creature_visuals/{slug}.tscn`) `.tscn` 부재 → ResourceLoader 실패 → **CTD 위험 큼**. 절대 권장 안 함. (배경 [§(d-1)] 의 `HasCustomBackground` 자동 false 폴백과 다른 점 — `MonsterModel` 측엔 자동 폴백 없음.) |

**v1 권장 = Mon-PNG**. 본체 (Head) 만 정지 이미지로 표시. 부위 (LeftArm/RightArm/Root) 는 v1 별도 PNG 자산 추가 + 동일 한 줄 패턴 — **부위 자산 부재 시 같은 CTD 위험 적용**, 자산 4장 모두 빌드 시점에 존재해야 함.

#### v1 보스 시각화 코드 (확정, 2026-04-27 경로 정정 반영)

```csharp
// AbnormalityMonster.cs (또는 EbonyQueenAppleHead 등 본체 클래스)
public override NCreatureVisuals? CreateCustomVisuals()
{
    return NodeFactory<NCreatureVisuals>.CreateFromResource(
        "res://assets/sprites/abnormalities/ebony_queen_apple/boss.png");
        // ⚠ 빌드 시 폴더명 공백 제거 + lowercase + underscore 권장 (현 폴더 "Ebony Queen's Apple" → "ebony_queen_apple")
        // ⚠ 2026-04-27 정정: 본 모드 export_presets.cfg 에 res_prefix 키 없음 → 모드 자산이 res:// 루트에 직접 합쳐짐.
        //    이전 초안의 "res://TheCity/assets/..." 는 ModTemplate 컨벤션 답습으로 본 모드와 불일치. §2.3 ⚠ 블록 (4) 참조.
}
```

**참고 사항 (mod-implementer 가이드):**
- 결과: `NCreatureVisuals` 루트 + `Bounds` 자식 (이미지 사이즈 +10%) + `Sprite2D` 자식 (PNG 텍스처, centered anchor 보정).
- **애니메이션 0** (Idle/Hit/Attack/Cast/Dead 모두 동일 정지 이미지). v1 PoC 충분, v2 에서 다부위/애니메이션 도입 시 별도 작업.
- **확인 필요 (런타임 검증)**: 단일 sprite 보스의 게임 측 기본 animator 호환성. `SetupCustomAnimationStates` override 필요 시 BaseLib `SetupAnimationState(controller, idleName: "default")` 헬퍼로 idle-only animator 명시 구성.
- **파일 경로 공백 주의**: 현 폴더 `Ebony Queen's Apple` 에 공백 + 어퍼케이스 + 어포스트로피 — Godot res:// 는 허용하지만 모드 빌드 시 GUID 매핑·임포트 캐시 이슈 가능. 빌드 전 `ebony_queen_apple` 으로 리네임 권장.

**근거:**
- M1-3 (sts-game-analyst): `BackgroundAssets` 생성자 (`MegaCrit.Sts2.Core.Rooms/BackgroundAssets.cs:26-65`) 의 `_bg_` / `_fg_` 강제, `layers/` 하위 디렉토리 금지, `_bg_<group>_<variant>` 그룹 키 파싱.
- M1-3 (sts-game-analyst): `NCombatBackground.AddLayer` (`NCombatBackground.cs:31-65`) 가 `Layer_{i:D2}` (zero-pad 2자리) + `Foreground` 자식 노드 강제.
- M1-3 보강 #11 (web-researcher): BaseLib 의 Harmony 패치 3개 본체 + `HasCustomBackground => _customBackgroundAssets != null` 자동 폴백 → CTD 위험 없음.
- **M1-3 보강 #12 (web-researcher)**: 단일 PNG 배경 헬퍼 부재 (`CustomBackgroundAssets` 가 디렉토리 + 파일명 패턴 강제). **단일 PNG 보스 시각화 헬퍼 존재** (`NodeFactory<NCreatureVisuals>.CreateFromResource`, BaseLib 공식 위키 명시).
- 게임 본체 `CreateBackgroundAssetsForCustom` 의 하드코딩 경로 (`res://scenes/backgrounds/{slug}/`) 는 **모드가 사용하지 않는 코드 경로** (`HasCustomBackground` 분기에서 자동 회피).
- 본 모드 `export_presets.cfg` 에 `res_prefix` 키 없음. 모드 .pck 자산은 `res://` 트리에 직접 합쳐짐.

> **(d) v2 작업 분량 (확정값, 보강 #11 + #12 반영):**
>
> **배경 자산 추가** — 자산 정리: `Background.png` → `bg_default_1.png` 리네임 + 더미 `fg_empty_1.png` (1×1 투명 PNG) 1장 + 빈 `_background.tscn` 1개. 코드: `CustomEncounterBackground` override 한 줄. 합계 < 30분.
>
> **다부위 / 애니메이션 보스** — 부위별 PNG 다수 또는 Spine `.tscn` + `SetupCustomAnimationStates` override. 작업량 자산 디자인에 좌우.
>
> **v1 PoC** — D1-bg + D1-monster 채택 시: 배경 자산 0 / 보스 PNG 직접 참조 (코드 4줄, 자산은 이미 있음). 자산 디렉토리 리네임 (공백 제거) 만 1회.

**근거:**
- M1-3 (sts-game-analyst): `BackgroundAssets` 생성자 (`MegaCrit.Sts2.Core.Rooms/BackgroundAssets.cs:26-65`) 의 `_bg_` / `_fg_` 강제, `layers/` 하위 디렉토리 금지, `_bg_<group>_<variant>` 그룹 키 파싱.
- M1-3 (sts-game-analyst): `NCombatBackground.AddLayer` (`NCombatBackground.cs:31-65`) 가 `Layer_{i:D2}` (zero-pad 2자리) + `Foreground` 자식 노드 강제.
- M1-3 보강 (web-researcher #11): BaseLib 의 Harmony 패치 3개 본체 추출 + 호출 흐름 분석. **핵심**: BaseLib 가 `HasCustomBackground` 자체를 override 했음 — `protected override bool HasCustomBackground => _customBackgroundAssets != null;`. 따라서 모드가 `CustomEncounterBackground` override 안 하거나 null 반환 → `_customBackgroundAssets == null` → `HasCustomBackground == false` → 게임은 `CreateBackgroundAssetsForCustom` **호출 안 함** + `parentAct.GenerateBackgroundAssets(rng)` 폴백. **CTD 위험 없음**.
- 게임 본체 `CreateBackgroundAssetsForCustom` 의 하드코딩 경로 (`res://scenes/backgrounds/{slug}/`) 는 **모드가 사용하지 않는 코드 경로** (`HasCustomBackground` 분기에서 자동 회피).
- `<modname>` 토큰 합성 규칙은 BaseLib 외 게임 코드에 있고 정적 미확인이지만, **fallback 경로 자체가 D1 채택 시 안 쓰임** → 미확인 영향 없음.
- 본 모드 `export_presets.cfg` 에 `res_prefix` 키 없음. 모드 .pck 자산은 `res://` 트리에 직접 합쳐짐. v2 자산 추가 시 본 모드의 기존 컨벤션 (`res://assets/...`) 유지 또는 ModTemplate 컨벤션 (`res://TheCity/...`) 채택은 **자산 폴더 구조 결정과 함께 별도 결재** 필요. M-A 의 보스 PNG 는 `res://assets/sprites/abnormalities/...` 경로 사용 (본 모드 기존 컨벤션).

**`CustomEncounterBackground` override 시 권장 진입점 (v2 자산 추가 시):**
```csharp
// 본 모드 기존 컨벤션 (res://assets/...) 유지 시:
public override BackgroundAssets? CustomEncounterBackground(ActModel parentAct, Rng rng)
    => new CustomBackgroundAssets(
        layersPath: "res://assets/scenes/backgrounds/ebony_queen_apple/layers",
        bgScenePath: "res://assets/scenes/backgrounds/ebony_queen_apple/background.tscn",
        rng: rng);
// (참고) ModTemplate 컨벤션 따른다면 layersPath/bgScenePath 모두 "res://TheCity/scenes/..." 로 시작. 본 모드 기존 자산이 res://assets/ 루트에 있으므로 통일성 위해 res://assets/ 권장.
```
- BaseLib `Utils/CustomBackgroundAssets.cs` 가 layersPath 디렉토리를 자체 스캔 + `_bg_<key>_*` / `*_fg_*` 파싱 (게임 본체와 동일 컨벤션).
- 모드가 절대 res:// 경로 명시 → 게임 본체 토큰 합성 의존 회피.
- **모드측 Harmony 패치 작성 불필요** — BaseLib 가 모든 가로채기 자동 처리.

> **(d) v2 작업 분량 (BaseLib 보강 #11 확정값):**
>
> 코드 측 (mod-implementer): `AbnormalityEncounter` 에 5-7개 짧은 virtual override 추가 (`CustomScenePath`, `CustomEncounterBackground`, `Slots`, `GenerateMonsters`, `AllPossibleMonsters`, 보스 시 아이콘 2개) — 1-3 시간 분량. **모드측 Harmony 패치 0개**.
>
> 자산 측 (artist 영역, 본 팀 외): `_fg_*.tscn` 1+개 + `_bg_<group>_*.tscn` 그룹당 1+개 + 메인 `*_background.tscn` (1920x1080 Control + Layer_NN/Foreground 자식 노드 + NCombatBackground 스크립트 참조). 보스 (`RoomType.Boss`) 시 `CustomRunHistoryIconPath` / `CustomRunHistoryIconOutlinePath` 둘 다 필수. 디자인·튜닝 시간이 코드 시간보다 훨씬 큼.
>
> v1 PoC 는 D1 채택 시 자산·코드 작업 0.

---

## 1. 정적 조사 결과 통합 (M1-1 ~ M1-8)

### 1.1 M1-1: BaseLib `Custom*Model` 트리오

**결론**: `Alchyr.Sts2.BaseLib` v3.x 가 `CustomEncounterModel` / `CustomEventModel` / `CustomMonsterModel` 트리오 제공. 모두 `ICustomModel` marker → 자동 등록 + 자동 prefix.

**부모 클래스 매핑 (Plan v1.1 §4 정정):**
```csharp
// Plan v1.1 §4
public abstract class AbnormalityEvent     : EventModel        // ← 변경
public abstract class AbnormalityEncounter : EncounterModel    // ← 변경
public abstract class AbnormalityMonster   : MonsterModel      // ← 변경

// M1-1 권장
public abstract class AbnormalityEvent     : CustomEventModel
public abstract class AbnormalityEncounter : CustomEncounterModel
public abstract class AbnormalityMonster   : CustomMonsterModel
```

**`CustomEncounterModel` 주요 멤버 (web-researcher 인용):**
```csharp
public abstract class CustomEncounterModel : EncounterModel, ICustomModel
{
    public override RoomType RoomType { get; }
    protected CustomEncounterModel(RoomType roomType, bool autoAdd = true)
    {
        // RoomType 검증 (Monster/Elite/Boss 외엔 경고 로그)
        RoomType = roomType;
        if (autoAdd) CustomContentDictionary.AddEncounter(this);   // ← 자동 등록
    }
    public abstract bool IsValidForAct(ActModel act);              // ← v1 PoC `=> true` 한 줄
    public virtual string? CustomScenePath => null;                // ← 옵셔널 .tscn 레이아웃
    public override bool HasScene => (CustomScenePath != null && ResourceLoader.Exists(CustomScenePath))
                                     || ResourceLoader.Exists(ScenePath);
    public virtual BackgroundAssets? CustomEncounterBackground(ActModel parentAct, Rng rng) => null;
    public virtual string? CustomRunHistoryIconPath => null;       // ← Boss 만 필수, Monster 무관
    public virtual string? CustomRunHistoryIconOutlinePath => null;
}
```

**자동 prefix 메커니즘:**
- BaseLib `Patches/Content/PrefixIdPatch.cs` 가 `ModelDb.GetEntry` Postfix.
- `ICustomModel` 구현 클래스 → `type.GetPrefix() + originalEntry`.
- `TypePrefix.GetPrefix()` = 네임스페이스 첫 단어 대문자 + `"-"`.
- 본 모드 = `TheCity.*` → `"THECITY-"`.

**출처:**
- https://github.com/Alchyr/BaseLib-StS2/blob/master/Abstracts/CustomEncounterModel.cs
- https://github.com/Alchyr/BaseLib-StS2/blob/master/Patches/Content/PrefixIdPatch.cs
- https://github.com/Alchyr/BaseLib-StS2/blob/master/Extensions/TypePrefix.cs
- https://alchyr.github.io/BaseLib-Wiki/docs/models/custom-encounter.html (HellEncounter 예제)

### 1.2 M1-2: Slugify 알고리즘 + 컨텍스트별 표기

**결론**: `StringHelper.Slugify` = PascalCase → SCREAMING_SNAKE_CASE. 게임 본체 4개 EventModel Loc 키 하드코딩으로 직접 검증.

**알고리즘:**
```csharp
// MegaCrit.Sts2.Core.Helpers/StringHelper.cs
public static string Slugify(string txt)
{
    string text = CamelCaseRegex().Replace(txt.Trim(), "$1_$2");        // CamelCase 분리
    string input = WhitespaceRegex().Replace(text.ToUpperInvariant(), "_");  // 공백 → _, 대문자
    return SpecialCharRegex().Replace(input, "");                        // 특수문자 제거
}
[GeneratedRegex("([A-Za-z0-9]|\\G(?!^))([A-Z])")]   // CamelCaseRegex
[GeneratedRegex("\\s+")]                              // WhitespaceRegex
[GeneratedRegex("[^A-Z0-9_]")]                        // SpecialCharRegex
```

**검증된 4건 (게임 본체 하드코딩 Loc 키):**
| 클래스 | Id.Entry | 출처 |
|---|---|---|
| `BattlewornDummy` | `BATTLEWORN_DUMMY` | BattlewornDummy.cs:46 |
| `BrainLeech` | `BRAIN_LEECH` | BrainLeech.cs:46 |
| `SelfHelpBook` | `SELF_HELP_BOOK` | SelfHelpBook.cs:21 |
| `WarHistorianRepy` | `WAR_HISTORIAN_REPY` | WarHistorianRepy.cs:27 |

**컨텍스트별 표기 (구현 시 헷갈리지 말 것):**
| 컨텍스트 | 표기 | 예 |
|---|---|---|
| `Id.Entry` 자체 | SCREAMING_SNAKE_CASE | `EBONY_QUEEN_APPLE_ENCOUNTER` |
| Loc 키 | `Id.Entry` 그대로 | `"EBONY_QUEEN_APPLE_ENCOUNTER.title"` |
| 폴더 경로 | `Id.Entry.ToLowerInvariant()` | `res://scenes/backgrounds/ebony_queen_apple_encounter/` |
| BaseLib 결합 | prefix + entry | `Id.Entry = "THECITY-EBONY_QUEEN_APPLE_ENCOUNTER"`, 폴더 = `thecity-ebony_queen_apple_encounter` |

**작명 규약:**
- PascalCase 작성 시 인접 대문자 (`XMLParser` → `X_M_L_PARSER`) 회피. `XmlParser` 권장.
- 클래스명 끝에 `Model` 붙이지 않기 (`SlugifyCategory` 의 `_MODEL` 접미사 제거 동작 인지).

### 1.3 M1-3: `res://scenes/backgrounds/` 자산 파이프라인

**결론**: 게임 코드가 폴더 구조 + 파일명 규약 + 메인 씬 노드 트리 모두 강제. v1 미포함 (D1-A) 권장. v2 추가 시 코드 5-7개 override + 자산 1세트 (1-3 시간 분량 + 자산 디자인 별개).

**경로 (`Id.Entry.ToLowerInvariant()` 기준):**
```
res://scenes/backgrounds/{title}/
├── {title}_background.tscn    ← 메인 씬 (NCombatBackground : Control 스크립트)
│                                  자식 노드 강제: Layer_00, Layer_01, ..., Layer_NN, Foreground
│                                  zero-pad 2자리 (D2 포맷)
└── layers/                     ← 서브디렉토리 안에 디렉토리 금지
    ├── {title}_bg_<group>_<variant>.tscn   ← 그룹별 RNG 1개 선택, 최소 1개 그룹 필수
    └── {title}_fg_<name>.tscn               ← 옵션, 전체 중 1개 선택 또는 0
```

**파일명 규약:**
- `_bg_` 또는 `_fg_` 둘 중 하나 포함 필수. 없으면 `BackgroundAssets` 생성자 즉시 `InvalidOperationException`.
- `_bg_` 다음 첫 `_` 까지가 그룹 키. 예: `*_bg_sky_01.tscn` → 그룹 `sky`.

**모드 PCK 마운트 + BaseLib Harmony 흐름 (보강 #11 결과 반영):**
- 본 모드 `export_presets.cfg` 에 `res_prefix` 키 없음 → Godot 4.5 PCK 빌드 표준 = 루트 마운트.
- ModTemplate 컨벤션은 모드 자산을 자기 모드명 폴더 (`res://TheCity/...`) 안에 두는 것이지만, **본 모드는 기존 자산이 `res://assets/...` 루트에 있어 그 컨벤션 채택 안 함**. 자산 경로는 `res://assets/...` 그대로. (2026-04-27 정정: 의뢰서 §2.3 ⚠ 블록 (4) 참조.)
- BaseLib `Abstracts/CustomEncounterModel.cs` 의 nested static class 3개가 `[HarmonyPrefix]` 자동 부착:
  - `ScenePathPatch` (`EncounterModel.ScenePath` getter): `CustomScenePath` 가 null 이면 본체 fallback.
  - `GetCustomBackgroundAssets` (`EncounterModel.GetBackgroundAssets`): 모드의 `CustomEncounterBackground` 결과를 `_customBackgroundAssets` 에 캐시 (사이드 효과만, 본체도 실행).
  - `ScenePatch` (`EncounterModel.CreateBackgroundAssetsForCustom`): 캐시값 반환, null 이면 본체 fallback.
- 모드용 절대 경로 진입점 = BaseLib `Utils/CustomBackgroundAssets.cs`:
  ```csharp
  new CustomBackgroundAssets(layersPath, bgScenePath, rng)
  ```
  layersPath 디렉토리를 자체 스캔, `_bg_<key>_*` / `*_fg_*` 파싱 (게임 본체와 동일 컨벤션).

**모드 측 안전성 (보강 #11 정정):**
- BaseLib 가 `HasCustomBackground` 를 override 함: `protected override bool HasCustomBackground => _customBackgroundAssets != null;`.
- 모드가 `CustomEncounterBackground` override 안 함 또는 null 반환 → `_customBackgroundAssets == null` → `HasCustomBackground == false` 자동 → 게임이 `CreateBackgroundAssetsForCustom` **호출 안 함**, `parentAct.GenerateBackgroundAssets(rng)` 폴백 → 부모 act 기본 배경 사용.
- 게임 본체 `CreateBackgroundAssetsForCustom` 의 하드코딩 경로 (`res://scenes/backgrounds/{slug}/`) 는 모드가 사용하지 않는 코드 경로 — fallback 토큰 합성 미확인은 영향 없음.
- 결재 (d) D1 (자산 미포함) 안전 확정.

**출처:**
- BackgroundAssets.cs:26-65 — DirAccess.Open + `_bg_`/`_fg_` 파싱
- NCombatBackground.cs:31-65 — `Create` + `AddLayer` (zero-pad 2자리 자식 노드 검색)
- EncounterModel.cs:224-243 — `CreateBackground` / `GetBackgroundAssets` / `CreateBackgroundAssetsForCustom`
- BaseLib `Abstracts/CustomEncounterModel.cs` 끝부분 — Harmony Prefix 3개 nested class
- BaseLib `Utils/CustomBackgroundAssets.cs` — 모드용 절대 경로 BackgroundAssets 진입점

### 1.4 M1-4: `Hook.ShouldStopCombatFromEnding`

**결론**: 두 시그니처 분리. 모드 override 는 **무인자** `bool ShouldStopCombatFromEnding()` (AbstractModel virtual). v1 다부위는 MinionPower 만으로 충분 — 본 훅은 v2 페이즈 전환에서만 등장.

**시그니처:**
- 정적 진입점 (`Hook.cs:1857`): `public static bool ShouldStopCombatFromEnding(CombatState combatState)` — 게임이 호출, 모드는 호출 안 함.
- 모델 virtual (`AbstractModel.cs:974`): `public virtual bool ShouldStopCombatFromEnding()` — 무인자, 기본 `false`.

⚠ **함정**: `get_hook_signature` 결과 `bool ShouldStopCombatFromEnding(CombatState)` 를 모델에 그대로 override 하면 컴파일 에러.

**호출 시점 (`CombatManager.cs:107-127`):**
```csharp
public bool IsEnding {
    get {
        if (!IsInProgress) return false;
        if (_pendingLoss != null) return true;
        if (_state.Enemies.Any(e => e.IsAlive && e.IsPrimaryEnemy))
            return false;                                   // ← Primary 살아있으면 종료 차단
        if (Hook.ShouldStopCombatFromEnding(_state))
            return false;                                   // ← Hook 이 차단
        return true;
    }
}
```

**v1 다부위 권장 패턴:**
- 부위 (머리/팔×2/뿌리) 에 **MinionPower** 적용 → `OwnerIsSecondaryEnemy=true` → `IsPrimaryEnemy=false` → IsEnding 의 첫 분기에서 카운트 제외.
- 본체만 죽으면 부위 살아있어도 자동 종료.
- ShouldStopCombatFromEnding override 불필요 (v2 페이즈 전환 시 등장).

**AutoSlay 리스크 (v1 무관, 메모용):**
- `EventRoomHandler.cs:150` 에서 ShouldStopCombatFromEnding=true 파워 강제 제거. autoslay 테스트 시 인지.

### 1.5 M1-5: `MinionPower` 적용 API

**결론**: `PowerCmd.Apply<MinionPower>(target, 1m, applier, null)` 한 줄. `CombatCmd.ApplyPower` 는 게임에 존재하지 않음.

**시그니처 (`PowerCmd.cs:18`):**
```csharp
public static async Task<T?> Apply<T>(
    Creature target, decimal amount, Creature? applier, CardModel? cardSource, bool silent = false
) where T : PowerModel
```

**바닐라 사용처 8건:**
| 파일·라인 | 패턴 |
|---|---|
| TorchHeadAmalgam.cs:43 | 자기 자신 `(base.Creature, 1m, base.Creature, null)` |
| GasBomb.cs:55 | 자기 자신 |
| KinFollower.cs:103 | 자기 자신 |
| Fabricator.cs:105 | 소환된 Creature `(spawned, 1m, base.Creature, null)` |
| Ovicopter.cs:81 | 동일 |
| MockAttackAndSummonMinionMonster.cs:32 | 동일 |
| IllusionPower.cs:65 | applier null `(base.Owner, 1m, null, null)` |

**MinionPower 정의 (`MinionPower.cs:7-19`):**
```csharp
public sealed class MinionPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override bool ShouldPlayVfx => false;
    public override bool OwnerIsSecondaryEnemy => true;          // ← 핵심
    public override bool ShouldPowerBeRemovedAfterOwnerDeath() => false;
    public override bool ShouldOwnerDeathTriggerFatal() => false;
}
```

**`AbnormalityMonster.cs` 본체 (Plan v1.1 §4.3 ⚠ 블록 그대로 OK):**
```csharp
public abstract class AbnormalityMonster : CustomMonsterModel
{
    public abstract string AbnormalityId { get; }
    /// <summary>true면 부위. 본체는 false (기본).</summary>
    protected virtual bool IsSecondaryPart => false;

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        if (IsSecondaryPart)
            await PowerCmd.Apply<MinionPower>(base.Creature, 1m, base.Creature, null);
    }
}
```

**부수:**
- `PowerCmd.Apply` 가 `CombatManager.Instance.IsEnding` 시 즉시 null 반환 — 전투 종료 직전 호출 안전.
- 같은 creature 두 번 호출 OK (Single, 결과 동일). 신경 쓰이면 `HasPower<MinionPower>()` 사전 체크.

### 1.6 M1-6: `EnterCombatWithoutExitingEvent` reward 빈 배열

**결론**: 빈 배열 = 추가 보상 0. 보상 화면 표시는 **`ShouldGiveRewards`** 가 결정. 결합 매트릭스 적용 (결재 (c) 함정 표 참고).

**메서드 본체 (`EventModel.cs:458-491`):**
```csharp
protected void EnterCombatWithoutExitingEvent(EncounterModel mutableEncounter, IReadOnlyList<Reward> extraRewards, bool shouldResumeAfterCombat)
{
    if (!IsShared) throw new InvalidOperationException(...);
    if (shouldResumeAfterCombat && LayoutType == EventLayoutType.Combat)
        throw new InvalidOperationException(...);
    EnteringEventCombat?.Invoke();
    if (!LocalContext.IsMe(Owner)) return;
    Node = null;
    CombatState combatState = LayoutType != EventLayoutType.Combat
        ? new CombatState(mutableEncounter, ...)
        : _combatStateForCombatLayout;
    CombatRoom combatRoom = new CombatRoom(combatState)
    {
        ShouldCreateCombat = (LayoutType != EventLayoutType.Combat),
        ShouldResumeParentEventAfterCombat = shouldResumeAfterCombat,
        ParentEventId = base.Id     // ← 호출 EventModel ID 자동 세팅
    };
    foreach (Reward extraReward in extraRewards)
        combatRoom.AddExtraReward(extraReward.Player, extraReward);    // ← extraRewards 가 ExtraRewards 딕셔너리에 추가
    TaskHelper.RunSafely(RunManager.Instance.EnterRoomWithoutExitingCurrentRoom(combatRoom, LayoutType != EventLayoutType.Combat));
}
```

**전제조건 (Plan v1.1 ⚠ 블록 재확인):**
- `IsShared == false` 면 throw → 환상체 EventModel 은 **반드시 `public override bool IsShared => true;`** 필요.
- `shouldResumeAfterCombat && LayoutType == Combat` 조합도 throw — 환상체 EventModel 은 LayoutType 기본값 (Combat 아님) 사용.

**ShouldGiveRewards × extraRewards 결합 (재확인, 결재 (c) 함정 표):**
| ShouldGiveRewards | extraRewards | 결과 |
|---|---|---|
| true | 빈 배열 | 표준 보상만 |
| true | non-empty | 표준 + extraRewards 합성 |
| false | 빈 배열 | 빈 보상 화면 (`_allowEmptyRewards=true`) |
| false | non-empty | ⚠ extraRewards 무시 |

### 1.7 M1-7: 이벤트 ID prefix 충돌

**결론**: `Abnormality*` / `ABNORMALITY_*` 검색 0건. 모드 prefix 자유롭게 사용 가능. BaseLib 자동 prefix `THECITY-` 와 결합 시 충돌 위험 사실상 0.

**바닐라 EventModel/AncientEventModel 76개 슬러그 전수 (`THE_*` 시작 4건만 발췌):**
- `THE_ARCHITECT`, `THE_FUTURE_OF_POTIONS`, `THE_LANTERN_KEY`, `THE_LEGENDS_WERE_TRUE` — 모두 `THE_CITY_*` 와 다른 단어.
- `THECITY-*` 시작은 0건 (BaseLib prefix 사용 시 자연 격리).

**충돌 검출 메커니즘:**
- `AbstractModel` 생성자 (`AbstractModel.cs:48`): 같은 **Type** 두 번 등록 시 `DuplicateModelException`.
- `ModelDb.Init` (`ModelDb.cs:18`): 같은 ID 다른 Type 이면 **사일런트 덮어쓰기** (예외 없음).
- 모드 클래스명이 바닐라 슬러그와 우연히 같으면 사일런트 충돌. BaseLib `THECITY-` prefix 가 자연 회피.

**Init 순서:** 바닐라 먼저 → `ReflectionHelper.GetSubtypesInMods<AbstractModel>` 결과 뒤. 모드가 같은 슬러그면 바닐라 덮어씀 — 회피 필수.

### 1.8 M1-8: ShouldGiveRewards + 보상 풀 끼워넣기

**결론**: 가능. **권장 A (extraRewards)** 가 단순·안전. 권장 B (TryModifyRewards) 는 v2 차등 보상에 적합.

**메커니즘 5종 (분석 결과):**
| ID | 메커니즘 | 평가 |
|---|---|---|
| **A ★** | `combatRoom.ExtraRewards` (EnterCombatWithoutExitingEvent 의 extraRewards 인자) | 표준 보상 + 추가 슬롯 합성. v1 PoC 정답. |
| B | `Hook.ModifyRewards` 의 `TryModifyRewards` override | 환상체별 차등 보상 (조건부). v2 적합. |
| C | `Hook.TryModifyCardRewardOptions` | 카드 후보 자체 교체 (새 슬롯 추가 X). 부분 사용. |
| D | 커스텀 `CardReward` 직접 생성 | 하드코딩 카드 리스트. 자동화 부족. |
| E | `RewardsCmd.OfferCustom` (BattlewornDummy 패턴) | 표준 보상 완전 대체. 환상체전 부적합 (사용자 요구 불일치). |

**권장 A 코드 한 줄:**
```csharp
// AbnormalityEvent.cs 의 선택지 콜백
EnterCombatWithoutExitingEvent<EbonyQueenAppleEncounter>(
    new[] { new CardReward(egoOptions, 3, base.Owner) },
    shouldResumeAfterCombat: true);
```

`egoOptions` 정의 (참고):
```csharp
var egoOptions = new CardCreationOptions(
    new[] { ModelDb.CardPool<EgoCardPool>() },
    CardCreationSource.Encounter,
    CardRarityOddsType.Uniform);
```

**리스크 (v1 권장 시 무관, 메모):**
- 메커니즘 B 는 `EncounterModel.ShouldReceiveCombatHooks=false` 라 hook 활성 여부 정적 미확인. 안전 우회 = extraRewards 사용.
- 멀티플레이어: `combatRoom.AddExtraReward(player, reward)` 가 player 인자라 호스트만 받을 가능성. 런타임 검증 필요.
- `CardReward.RewardsSetIndex => 5` 고정. EGO 를 다른 위치에 띄우려면 서브클래스로 index 변경.

---

## 2. Plan v1.1 차분 정정 가이드

> 본문은 그대로 두고 구현 시 본 §2 차분 만 적용한다.

### 2.1 Plan §2.2 (BattlewornDummy 패턴)

**원안 → 정정:**
- ⚠ `combatRoom.CombatState.AreAllPlayersDead` → 게임에 없음. encounter 커스텀 플래그 (예: `IsVictory` / `RanOutOfTime`) + `SaveCustomState`/`LoadCustomState` 왕복 필수.
- ⚠ `SetEventFinished("string")` → `SetEventFinished(LocString)`. `L10NLookup(...)` 으로 감싸기.
- 추가: 환상체 EventModel 은 **반드시** `public override bool IsShared => true;` (`EnterCombatWithoutExitingEvent` 의 throw 회피).

### 2.2 Plan §3 (폴더·에셋 구조)

**원안 → 정정:**
- ⚠ 배경 경로: `res://images/backgrounds/encounters/{slug}/bg.png` → `res://scenes/backgrounds/{Id.Entry.ToLowerInvariant()}/{title}_background.tscn` + `layers/_bg_<group>_<variant>.tscn` (다수) + `layers/_fg_*.tscn` (옵션).
- ⚠ `.png` 단일 → `.tscn` 메인 + `.tscn` 레이어 (Godot scene, NCombatBackground 스크립트 + Layer_NN/Foreground 자식 노드).
- BaseLib `THECITY-` 자동 prefix 결합: 폴더 경로 `res://scenes/backgrounds/thecity-ebony_queen_apple_encounter/...` (대시 포함, lowercase).

> v1 PoC 는 D1 (배경 미포함) 권장 — 본 §2.2 정정은 v2 자산 추가 시 적용.

### 2.3 Plan §4 (부모 클래스)

**원안 → 정정:**
- 부모 변경: `EventModel` → `CustomEventModel`, `EncounterModel` → `CustomEncounterModel`, `MonsterModel` → `CustomMonsterModel`.
- 자동 등록 (BaseLib autoAdd) → `ModelDb.Inject` 직접 호출 불필요.
- `AbnormalityEncounter` 생성자: `: base(RoomType.Monster)` 추가.
- `IsValidForAct(ActModel act)` 추상 → v1 `=> true` 한 줄.
- 승패 판정 (Plan v1.1 §4.1 ⚠ 블록 그대로 OK):
  ```csharp
  public override async Task Resume(AbstractRoom room)
  {
      var combatRoom = (CombatRoom)room;
      var encounter = (AbnormalityEncounter)combatRoom.Encounter;
      if (encounter.IsVictory) await OnVictory(combatRoom);
      else                     await OnDefeat(combatRoom);
  }
  ```
  `IsVictory` 플래그 + `SaveCustomState`/`LoadCustomState` 왕복 필수.

> ⚠ **2026-04-27 M-A 구현 발견 — 정정 4건**
>
> **(1) `ModelDb.GetByType(Type)` 는 게임에 존재하지 않음.** Plan v1.1 §4.4 의 `ModelDb.GetByType(type) as EventModel` 는 빌드 실패.
> - 게임의 public API 는 모두 generic 진입점만 제공: `ModelDb.Event<T>()`, `ModelDb.Encounter<T>()`, `ModelDb.Monster<T>()`, `ModelDb.Card<T>()`, `ModelDb.Affliction<T>()`, `ModelDb.Enchantment<T>()`.
> - 비-generic 진입점 `ModelDb.Get(Type)` / `ModelDb.Get<T>()` 는 모두 **private** (csproj `Publicizer` 비활성화 상태에서는 접근 불가).
> - **정확한 패턴 — Type 동적 매개변수로 ModelDb 조회 시:**
>   ```csharp
>   // ModelDb.GetId(Type) 는 public static — Type → ModelId 변환.
>   // ModelDb.GetByIdOrNull<T>(ModelId) 는 public static generic — ModelId → AbstractModel 서브타입.
>   var modelId = ModelDb.GetId(type);
>   var event = ModelDb.GetByIdOrNull<EventModel>(modelId);
>   ```
> - 적용처: `AbnormalityRegistry.GetEventForAbnormality(string id)` / `AbnormalityEvent.StartAbnormalityCombat()` (헬퍼가 `EncounterType` 으로 mutable encounter 를 얻을 때) 둘 다 본 패턴 사용.
>
> **(2) `MonsterModel` 의 abstract 추상 멤버 3건 — Plan v1.1 / m1.md 본문 어디에도 명시 없음. PoC 단계에서도 stub 채워야 빌드 통과.**
> - `public abstract int MinInitialHp { get; }` — 인스턴스 생성 시 HP 결정에 사용.
> - `public abstract int MaxInitialHp { get; }` — 동일.
> - `protected abstract MonsterMoveStateMachine GenerateMoveStateMachine();` — `SetUpForCombat` 단계에서 호출, 반환된 state machine 이 인텐트·이동 결정.
> - **PoC 단계 권장 stub:**
>   ```csharp
>   public override int MinInitialHp => 60;   // placeholder, 본격 튜닝은 후속 단계
>   public override int MaxInitialHp => 70;
>   protected override MonsterMoveStateMachine GenerateMoveStateMachine()
>   {
>       // 자기참조 무한 루프 idle — 인텐트 빈 채로, 효과 없음
>       var idle = new MoveState("IDLE", static _ => Task.CompletedTask);
>       idle.FollowUpState = idle;
>       return new MonsterMoveStateMachine(new[] { idle }, idle);
>   }
>   ```
> - `MoveState` 정확한 ctor: `MoveState(string stateId, Func<IReadOnlyList<Creature>, Task> onPerform, params AbstractIntent[] intents)`. `params` 라 인텐트 미지정 시 빈 배열 자동.
> - `MonsterMoveStateMachine` ctor: `MonsterMoveStateMachine(IEnumerable<MonsterState> states, MonsterState initialState)`.
>
> **(3) 모드 측 `const Id` 명명 회피 — `AbstractModel.Id` 속성과 충돌.**
> - `public const string Id = "EbonyQueenApple";` 처럼 모델 클래스 안에 `Id` 라는 const 를 두면 부모 `AbstractModel.Id` (ModelId 인스턴스 속성) 를 숨김 → C# 컴파일러 경고 CS0108.
> - **권장 작명**: `AbnormalityKey`, `Slug`, `Identifier` 같은 이름. 본 PoC 에서는 `EbonyQueenAppleEncounter.AbnormalityKey` 사용.
>
> **(4) 모드 .pck 의 `res://` 마운트 — 실측 결과 모드 .pck 가 게임 res:// 루트에 직접 합쳐짐.**
> - 본 모드 `export_presets.cfg` 에 `res_prefix` 키 없음. ModTemplate-StS2 컨벤션의 `res://ModName/` 1단계 prefix 는 모드별 선택 사항.
> - **본 모드의 정확한 자산 경로:** `res://assets/sprites/abnormalities/ebony_queen_apple/boss.png` (모드 폴더 prefix 없음).
> - m1.md (d-2) 본문의 `"res://TheCity/assets/sprites/..."` 예시는 ModTemplate 컨벤션 답습이며 **본 모드와 불일치**. M-A 에서는 prefix 없는 경로로 빌드 통과 + DLL 복사 확인. 런타임 PNG 로드 검증은 L9 (Mon-PNG animator 호환성) 와 함께 1회 수행 필요.

### 2.4 Plan §5 (EbonyQueenApple 구현)

**원안 → 정정:**
- §5.1 `EbonyQueenAppleEvent`: `IsShared => true` 추가 명시.
- §5.1 `OnApproach` 콜백 (결재 (c) C1-v1 채택, 2026-04-25):
  ```csharp
  EnterCombatWithoutExitingEvent<EbonyQueenAppleEncounter>(
      Array.Empty<Reward>(),       // v1 = 표준 보상만 (Gold + 카드 3장)
      shouldResumeAfterCombat: true);
  ```
  v2 EGO 도입 시: 첫 인자만 `new[] { new CardReward(egoOptions, 3, base.Owner) }` 로 교체. 코드 구조 변경 0.
- §5.2 `EbonyQueenAppleEncounter`: `IsVictory` / `IsDefeated` 커스텀 플래그 + Save/Load 왕복.
- §5.2 부위 클래스 (`_LeftArm`, `_RightArm`, `_Root`): 각자 `protected override bool IsSecondaryPart => true;` (§4.3 패턴).
- §5.2 본체 (`_Head`): `IsSecondaryPart` 미설정 (default false → Primary). `MinionPower` 적용 안 함.
- §5.2 본체 (`_Head`) 시각화 (결재 (d-2) Mon-PNG, 보강 #12 추가): `CreateCustomVisuals()` override + `NodeFactory<NCreatureVisuals>.CreateFromResource("res://assets/sprites/abnormalities/ebony_queen_apple/boss.png")` 한 줄 (※ 2026-04-27 정정: 본 모드는 res:// 루트 마운트라 `res://TheCity/...` prefix 미사용 — §2.3 ⚠ 블록 (4) 참조). v1 부터 PNG 직접 참조. 부위 (`_LeftArm`/`_RightArm`/`_Root`) 도 동일 한 줄 패턴 — **각 부위 PNG 자산 4장 모두 빌드 시점에 존재 필수** (자산 부재 시 Mon-NONE 위험과 동일하게 CTD 가능).
- 폴더 경로 (배경 자산 v2 추가 시): `res://assets/scenes/backgrounds/ebony_queen_apple/...` (본 모드 기존 컨벤션, v1 무관). 2026-04-27 정정 — ModTemplate 의 `res://TheCity/...` 가 아닌 본 모드 기존 `res://assets/...` 사용.
- **자산 디렉토리 리네임 권장 (빌드 전 1회)**: 현 `assets/sprites/Abnormalities/Ebony Queen's Apple/` → `assets/sprites/abnormalities/ebony_queen_apple/` (공백 제거 + lowercase + underscore). Godot res:// 는 공백 허용하지만 GUID 매핑·임포트 캐시 안전성 + `Slugify` 결과 (M1-2) 와 일관성.

### 2.5 Plan §6 (라우팅)

**원안 그대로 OK** (verification.md 와 일관):
- `Hook.AfterMapGenerated` (hydrate) + `Hook.ModifyNextEvent` (라우팅) 2훅.
- `IRunState.CurrentMapCoord` / `CurrentActIndex` / `Map` 모두 public 확인됨.
- 결정론 해시는 기존 `AbnormalityMapInjector` 패턴 재사용.

### 2.6 Plan §7 (구현 순서)

**원안 7단계 그대로 진행 가능. 단 단계 3 (배경/BGM 적용) 은 결재 (b)/(d) 에 따라 분기:**

| 단계 | 작업 | 결재 의존 |
|---|---|---|
| 1 | `Template/` 부모 클래스 4개 (Custom*Model 트리오 + Registry) | (a) |
| 2 | 단일 부위 PoC (`Head`만), 콘솔 `fight` 호출 | — |
| 3 | 배경/BGM 적용 (`HasCustomBackground` / `CustomBgm`) | **(b) B2 + (d) D2 채택 시만 v1**, 권장은 **v2 이관** |
| 4 | 다부위 + AI Move 풀 | (a) |
| 5 | `EbonyQueenAppleEvent`, 콘솔 `event` 호출 | (c) |
| 6 | `MapIntegration/AbnormalityEventRouter` (Hook 2개) | — |
| 7 | 로컬라이제이션 키 정리 | (a) |

---

## 3. 정적 한계 → 런타임 검증 (Wave 2)

> 정적으로 답이 안 나온 7건. v1 권장 결정 채택 시 대부분 무관 — 단 `*` 표시는 v1 진입 직전 런타임 확인 권장.

| ID | 항목 | v1 권장 결정 시 영향 |
|---|---|---|
| L1 | M1-2 인접 대문자 케이스 (`XMLParser` 식) 정확한 슬러그 | **무관** — 작명 회피로 우회. |
| L2 | M1-3 바닐라 `*_background.tscn` 노드 트리 (Layer_NN N 값 등) | **D1-A 무관**, v2 자산 추가 시 PCK 추출. |
| L3 | M1-3 모드 PCK res:// 경로 마운트 동작 | **D1-A 무관**, v2 자산 추가 시 1회 검증. 모드 .pck 가 res:// 트리에 합쳐짐은 ModTemplate 컨벤션으로 확인. |
| L4 | M1-4 `Hook.ShouldStopCombatFromEnding` 컬렉션 순회 범위 (`ShouldReceiveCombatHooks=false` 영향) | **v2 페이즈 전환 도입 시 필요**, v1 무관. |
| L5 ★ | M1-7 BaseLib `ICustomModel` prefix 가 EventModel 에 적용되는지 | **권장 A1 + Custom*Model 트리오 채택 시 자동 해결**. 미해결 시 수동 prefix 강제. **v1 진입 직전 1회 검증 권장**. |
| L6 | M1-8 `EncounterModel.TryModifyRewards` 호출 여부 (ShouldReceiveCombatHooks=false 영향) | **권장 C1 (extraRewards) 채택 시 무관**. 메커니즘 B 사용 시 검증. |
| L7 | M1-8 멀티플레이어 ExtraRewards sync (호스트만 받는지) | **싱글 PoC 단계 무관**. 멀티플레이 검증 시 (별도 plan). |
| L8 | M1-3 보강: 게임 본체 `EncounterModel.ScenePath` getter 의 fallback 토큰 합성 (`<modname>-<entry>`) 규칙 | **D1-bg 채택 시 무관** — BaseLib 가 `HasCustomBackground` override 로 자동 회피. v2 (D2) 도 `CustomScenePath` 명시 사용 시 무관. fallback 경로 의존 시에만 critical (사용 안 함). |
| L9 ★ | M1-3 보강 #12: 단일 sprite 보스의 게임 측 기본 animator 호환성 — `CreateFromResource` 가 만든 `NCreatureVisuals` 에 게임이 부착하는 기본 animator 가 idle-only 와 충돌 없는지 | **Mon-PNG 채택 시 v1 진입 직전 1회 검증 권장**. 위험 시 `SetupCustomAnimationStates` override + BaseLib `SetupAnimationState(controller, idleName: "default")` 헬퍼로 idle-only animator 명시 구성. |
| L10 | M1-3 보강 #12: `BgLayers` 의 PNG 경로 string 이 게임 본체 `AddLayer` 의 `GetScene(layerPath).Instantiate<Control>()` 강제와 호환되는 메커니즘 (PNG → Control 자동 변환? 별도 분기?) | **Bg-NONE 채택 시 무관**. v2 (Bg-tscn) 자산 정리 시점에 sts-game-analyst (`get_entity_source NCombatBackground.SetLayers/AddLayer`) 로 확정 필요. v2 작업 정확한 분량 추정에 사용. |
| L11 ★ | **M-A 구현 발견**: `EncounterModel.L10NLookup(string key)` 본체가 `new LocString("encounters", key)` 로 **`"encounters"` 테이블을 하드코딩**. 본 모드의 모든 로컬라이제이션은 `thecity.json` 단일 테이블에 있고 `LocTableInjector` 가 `tables["thecity"]` 만 등록 — `L10NLookup` 호출 시 `LocManager` 가 `encounters` 테이블에서 키를 찾다 실패할 수 있음. 영향 범위: Encounter 표시 텍스트 (Title / Description), `OnDefeat` 의 `SetEventFinished(L10NLookup($"{AbnormalityId}.defeat"))` 같은 EventModel 의 `L10NLookup`(이쪽은 다른 테이블 명 사용 가능) 도 점검 필요. | **M-B 진입 전 sts-game-analyst 추가 의뢰** — `LocManager` 의 fallback 메커니즘 (테이블 없으면 다른 테이블 검색? 키 그대로 표시?) + `EventModel.L10NLookup` 의 테이블 (encounters / events / 다른?) 확정. 결과에 따라 두 방향: (a) `LocTableInjector` 가 `thecity` 외에 `encounters`/`events` 별칭으로도 같은 dict 등록, (b) 모드측이 직접 `LocString` 인스턴스를 만들어 `thecity` 테이블 명시. **본 항목 미해결 시 M-B 콘솔 `event` 호출에서 텍스트가 빈 채로 표시될 가능성**. |

→ **권장 결정 (A1/B1/C1/Bg-NONE/Mon-PNG) 채택 시 v1 진입 직전 필수 런타임 검증 = L5 + L9 + L11 (총 3건)**.

---

## 4. 결재 후 §7 진입 시 mod-implementer 가이드

> **이 세션에서는 진입 안 함**. 사용자 결재 + 후속 세션에서 mod-implementer 호출 시 본 섹션 인용.

### 4.1 진입 자격 (Gate A 체크리스트)
- [ ] 결재 (a)/(b)/(c)/(d) 모두 결재됨
- [x] BaseLib 보강 (#11) 결과 도착 → m1.md (d) 갱신 완료 (2026-04-25, 추가 보강 후속도 반영)
- [ ] L5 (Custom*Model 자동 prefix EventModel 적용) 런타임 1회 검증 완료 (또는 검증 후속 단계로 이관 명시)

### 4.2 mod-implementer 의뢰 prompt 핵심 (자기완결)
- 본 m1.md 의 §1 (정적 결과) + §2 (Plan 차분 정정) 통째로 인용.
- §3 (런타임 한계) 도 함께 — 어느 항목이 v1 무관인지 mod-implementer 가 인지하게.
- 단건 7건 회신 (sts-game-analyst inbox) 도 참조 자료로 첨부 — 호출 시점·내부 동작·코드 인용 풍부.
- 결재 결과 (a/b/c/d 사용자 선택) 명시.
- §7 단계 1~7 순차 진행, 각 단계 후 빌드·런타임 검증.

### 4.3 빌드·검증 흐름
- `dotnet build -c Release` → DLL 자동 복사 (`$(ModsPath)/TheCity/`).
- 콘솔 `fight <encounterId>` / `event <eventId>` 로 단계별 검증.
- runtime-qa Wave 2 의뢰 시점은 단계 6 (라우팅) 완료 후 (R2/R4 + L5).

---

## 5. 메타

### 5.1 본 사양서 갱신 정책
- BaseLib 보강 (#11) 결과 도착 → §1.1 / §1.3 / §3 / 결재 (d) 갱신. **(2026-04-25 완료)**
- 사용자 결재 결정 변경 → 결재 항목 표 + §2 / §4.1 갱신.
- 런타임 검증 결과 도착 → §3 한계 항목 status 변경.

### 5.2 출처 정리
- 게임 코드 (sts-game-analyst, MCP search_game_code/get_entity_source/list_hooks):
  - `MegaCrit.Sts2.Core.Models/EncounterModel.cs:30-243`
  - `MegaCrit.Sts2.Core.Models/EventModel.cs:458-491`
  - `MegaCrit.Sts2.Core.Models/AbstractModel.cs:48, 834-840, 974`
  - `MegaCrit.Sts2.Core.Helpers/StringHelper.cs (Slugify)`
  - `MegaCrit.Sts2.Core.Rooms/BackgroundAssets.cs:26-65`
  - `MegaCrit.Sts2.Core.Nodes.Rooms/NCombatBackground.cs:31-65`
  - `MegaCrit.Sts2.Core.Combat/CombatManager.cs:107-127`
  - `MegaCrit.Sts2.Core.Commands/PowerCmd.cs:18`
  - `MegaCrit.Sts2.Core.Commands/RewardsCmd.cs:11-25`
  - `MegaCrit.Sts2.Core.Rewards/RewardsSet.cs:64-67, 79, 127-167`
  - `MegaCrit.Sts2.Core.Models.Powers/MinionPower.cs:7-19`
  - `MegaCrit.Sts2.Core.Models.Monsters/TorchHeadAmalgam.cs:43`
  - `MegaCrit.Sts2.Core.Models.Events/BattlewornDummy.cs:46, 71-103`
  - `MegaCrit.Sts2.Core.Models.Encounters/BattlewornDummyEventEncounter.cs:28`
  - 외 75개 EventModel/AncientEventModel 슬러그 전수 (M1-7)
- BaseLib (web-researcher):
  - https://github.com/Alchyr/BaseLib-StS2/blob/master/Abstracts/CustomEncounterModel.cs
  - https://github.com/Alchyr/BaseLib-StS2/blob/master/Abstracts/CustomEventModel.cs
  - https://github.com/Alchyr/BaseLib-StS2/blob/master/Abstracts/CustomMonsterModel.cs
  - https://github.com/Alchyr/BaseLib-StS2/blob/master/Patches/Content/PrefixIdPatch.cs
  - https://github.com/Alchyr/BaseLib-StS2/blob/master/Extensions/TypePrefix.cs
  - https://github.com/Alchyr/BaseLib-StS2/blob/master/Utils/FmodAudio.cs
  - https://github.com/Alchyr/BaseLib-StS2/blob/master/Utils/CustomBackgroundAssets.cs (보강 #11)
  - https://github.com/Alchyr/ModTemplate-StS2 (모드 res:// 컨벤션 참고, 보강 #11)
  - https://alchyr.github.io/BaseLib-Wiki/docs/models/custom-encounter.html
  - https://github.com/Alchyr/BaseLib-StS2/blob/master/Abstracts/CustomMonsterModel.cs (CreateCustomVisuals + Harmony 패치 6개, 보강 #12)
  - https://github.com/Alchyr/BaseLib-StS2/blob/master/Utils/NodeFactories/NCreatureVisualsFactory.cs (CreateFromResource 본체, 보강 #12)
  - https://github.com/Alchyr/BaseLib-StS2/blob/master/Utils/NodeFactories/NodeFactory.cs (제네릭 베이스, 보강 #12)
  - https://alchyr.github.io/BaseLib-Wiki/docs/scenes/creature-visuals.html (단일 PNG 명시, 보강 #12)
- 모드 측 확인:
  - `TheCity.csproj:36` (`<PackageReference ... Version="*"`)
  - `src/ModStart.cs:8` (`namespace TheCity`)
  - `export_presets.cfg` (res_prefix 없음, 루트 마운트)

### 5.3 갱신 이력
- 2026-04-25 — 초안 작성 (M1-1~M1-8 + 결재 4건 + 한계 7개).
- 2026-04-25 — BaseLib 보강 (#11) 1차 반영: Harmony 패치 3개 본체 인용, `Utils/CustomBackgroundAssets.cs` 절대 경로 진입점 추가, fallback 토큰 합성 미확정 우려로 결재 (d) 의 D1 을 D1-A (안전) / D1-B (위험) 로 분리.
- 2026-04-25 — BaseLib 보강 (#11) 2차 반영 (정정): web-researcher 4섹션 보고로 BaseLib 가 `HasCustomBackground` 자체를 override (`=> _customBackgroundAssets != null`) 함을 확인. 모드가 `CustomEncounterBackground` override 안 하면 게임이 `parentAct.GenerateBackgroundAssets` 자동 폴백 → CTD 위험 없음. 결재 (d) D1-A/D1-B 분리 취소, D1 단일 권장으로 단순화. v2 코드 분량 = 5-7개 짧은 override + **모드측 Harmony 패치 0개** 확정.
- 2026-04-25 — **사용자 결재 (c) 채택**: C1-v1 (`ShouldGiveRewards=true` + `extraRewards: Array.Empty<Reward>()`). v1 = 표준 보상만 (Gold + 카드 3장). EGO 카드 시스템은 별도 plan (미작성), v2 진입 시점에 인자만 채워 마이그레이션 — 코드 구조 변경 0. (a)/(b)/(d) 는 결재 답 대기 중.
- 2026-04-25 — **보강 #12 1차 반영**: 사용자 자산 (`Background.png` + `Boss.png`) 으로 v1 진입 가능성 조사. 결과: 배경은 단일 PNG 헬퍼 부재, 보스 시각화는 BaseLib 공식 헬퍼 존재 (`NodeFactory<NCreatureVisuals>.CreateFromResource`). 결재 (d) 를 (d-1) 배경 / (d-2) 보스 분리.
- 2026-04-25 — **보강 #12 2차 반영 (정밀화)**: 두 번째 회신으로 핵심 정보 추가 — `MonsterModel` 측엔 `EncounterModel.HasCustomBackground` 같은 자동 폴백 override 가 없음. 따라서 보스 자산·코드 둘 다 미포함 (Mon-NONE) 시 게임 본체 `VisualsPath` 폴백 → ResourceLoader 실패 → CTD 위험. 옵션 ID 를 D1-bg/D1-monster 식 → Bg-NONE/Bg-tscn / Mon-PNG/Mon-tscn/Mon-NONE 식으로 정정해 자산 부재 위험 명시. §3 한계 표에 L10 (`BgLayers` PNG → Control 변환 메커니즘) 신설 — v2 자산 정리 시점에 sts-game-analyst 추가 확인 필요.
- 2026-04-27 — **M-A 구현 발견 4건 반영**: §2.3 부모 클래스 차분에 ⚠ 블록 추가 — (1) `ModelDb.GetByType` 게임 부재 → `ModelDb.GetId(Type)` + `ModelDb.GetByIdOrNull<T>(ModelId)` 패턴으로 정정 (Plan v1.1 §4.4 / m1.md 본문 §2.3 영향). (2) `MonsterModel` 추상 멤버 3건 (`MinInitialHp`/`MaxInitialHp` public abstract int + `GenerateMoveStateMachine` protected abstract) 명시 + PoC stub 패턴 제공 (단순 idle MoveState 자기참조 무한 루프). (3) 모드 측 `const Id` ↔ `AbstractModel.Id` 충돌 (CS0108) 회피 — 권장 명: `AbnormalityKey`/`Slug`/`Identifier`. (4) 모드 .pck `res://` 마운트 실측 — `export_presets.cfg` `res_prefix` 없어 모드 자산이 res:// 루트에 직접 합쳐짐, 본 모드 정확 경로는 `res://assets/...` (m1.md (d-2) 본문의 `res://TheCity/...` 예시는 ModTemplate 답습으로 본 모드 불일치). §3 한계 표에 L11 (LocString 라이프사이클 — `EncounterModel.L10NLookup` 의 `"encounters"` 테이블 하드코딩 vs 모드 `thecity.json` 단일 테이블) 신설 — M-B 진입 전 sts-game-analyst 추가 의뢰 필요. 권장 결정 채택 시 v1 진입 직전 필수 런타임 검증 = L5 + L9 + L11 (총 3건, 기존 2건에서 +1).

---

> ⚠ **이 사양서는 결재 + 후속 세션에서 mod-implementer 호출 시 사용. 이 세션에서는 본 m1.md 작성 + 결재 보고까지가 끝. `src/Abnormality/` 코드 작성 / `dotnet build` / `assets/` 신규 자산 추가는 본 세션에서 실행하지 않는다.**
