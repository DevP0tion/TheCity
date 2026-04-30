# abnormality-battle (통합 사양서)

**Feature:** abnormality-battle
**상태:** M1 정적 조사 완료 / M-A 빌드 통과 / **사용자 결재 4건 채택 완료 ((a)A1 / (b)B1 / (c)C1-v1 / (d-1)Bg-USE / (d-2)Mon-PNG, 2026-04-29) / M-B + M-C 통합 구현 진행 중**
**최종 갱신:** 2026-04-29
**관련 문서:**
- `abnormality-map-node.md` — 선행 맵 노드 시스템 (구현 완료, §6 라우팅이 이를 확장)
- `_archive/abnormality-battle/` — 통합 전 plan v1.1 / verification / m1 (역사적 참고용)

> 본 문서는 **단일 진실원천 (single source of truth)**. 구현·결재·검증 모두 본 문서를 따른다. 통합 전 3 파일 (plan v1.1 / verification.md / m1.md) 의 결정 흐름은 §7 갱신 이력 + `_archive/` 보존.

---

## Executive Summary

| 관점 | 요약 |
|---|---|
| **Problem** | 환상체 맵 노드는 구현됐으나 (`abnormality-map-node.md`), 진입 시 발생할 전투 시스템이 없음. 림버스 컴퍼니의 환상체전 (이벤트 → 선택지 → 전투 → 결과) 흐름을 STS2 위에 얹어야 함. |
| **Solution** | BaseLib `Custom*Model` 트리오 (`CustomEventModel`/`CustomEncounterModel`/`CustomMonsterModel`) 상속 + `Hook.AfterMapGenerated` (hydrate) + `Hook.ModifyNextEvent` (라우팅) + 단일 PNG 보스 시각화 (`NodeFactory<NCreatureVisuals>.CreateFromResource`). 부모 클래스 3계층 + 환상체별 자기완결 폴더 구조. |
| **Outcome (현재)** | M-A (Template 4 + EbonyQueenAppleHead 단부위 PoC + 자산 리네임) 빌드 Success. M-B (EbonyQueenAppleEvent + 콘솔 흐름) / M-C (라우팅) 보류. |
| **Scope (v1)** | 흑단여왕의 사과 1체 단부위 PoC. 다부위 / EGO 카드 보상 / 페이즈 전환 / 멀티플레이어 검증은 v2 이상. |

### 핵심 결정 (사용자 결재 진행 상태)

| 결재 | 권장안 | 상태 |
|---|---|---|
| (a) 클래스명 prefix | A1: `EbonyQueenAppleEncounter` (BaseLib 자동 `THECITY-` prefix) | **2026-04-29 채택 완료** |
| (b) v1 BGM | B1: `CustomBgm => ""` (자산 미준비) | **2026-04-29 채택 완료** |
| (c) 보상 UI | C1-v1: `ShouldGiveRewards=true` + `extraRewards: Array.Empty<Reward>()` (표준 보상만) | **2026-04-25 채택 완료** |
| (d-1) 배경 자산 | ★ Bg-USE: `CustomEncounterBackground` override 신규 추가, `background.png` 사용 (사용자 변경) | **2026-04-29 채택 완료 (권장안 Bg-NONE 변경)** |
| (d-2) 보스 시각화 | Mon-PNG: `NodeFactory<NCreatureVisuals>.CreateFromResource(boss.png)` 한 줄 | **2026-04-29 채택 완료** |

---

## 1. Plan (정밀 사양)

### 1.1 목표

**부모 클래스 계층** (`src/Abnormality/Template/`):
- `AbnormalityEvent : CustomEventModel` — 환상체전 공통 이벤트 골격
- `AbnormalityEncounter : CustomEncounterModel` — 배경/BGM/RoomType 규약 + 승패 플래그
- `AbnormalityMonster : CustomMonsterModel` — 환상체 공통 + MinionPower 패턴
- `AbnormalityRegistry` — 환상체 ID ↔ EventModel 타입 매핑

**라우팅 계층** (`src/Abnormality/MapIntegration/`):
- `AbnormalityEventRouter` — `Hook.AfterMapGenerated` (hydrate) + `Hook.ModifyNextEvent` (라우팅)

**구현 계층** (`src/Abnormality/Battles/{이름}/`):
- 환상체별 자기완결 폴더 — Event / Encounter / Monsters / Moves

### 1.2 Non-Goals (v1 제외)

- 림버스식 클래시 / 코인 시스템 (STS2 카드 vs 인텐트 그대로 사용)
- EGO 카드 시스템 (별도 plan, 미작성)
- 환상체 페이즈 전환 이벤트
- 멀티플레이어 환상체전 검증
- 환상체 자체 콜렉션 / 관찰 로그 UI
- 다부위 (LeftArm/RightArm/Root) — v2

### 1.3 폴더 구조

```
src/Abnormality/
│
├── Template/                          # 부모 클래스
│   ├── AbnormalityEvent.cs            # : CustomEventModel
│   ├── AbnormalityEncounter.cs        # : CustomEncounterModel
│   ├── AbnormalityMonster.cs          # : CustomMonsterModel
│   └── AbnormalityRegistry.cs         # 환상체 ID ↔ Type 매핑
│
├── MapIntegration/                    # 라우팅 (M-C)
│   └── AbnormalityEventRouter.cs      # Hook 2개 구독
│
└── Battles/                           # 환상체별 (자기완결)
    │
    └── EbonyQueenApple/                # 흑단여왕의 사과 (PoC)
        ├── EbonyQueenAppleEvent.cs        # : AbnormalityEvent (M-B)
        ├── EbonyQueenAppleEncounter.cs    # : AbnormalityEncounter
        ├── EbonyQueenAppleHead.cs         # : AbnormalityMonster (단부위 PoC)
        └── (v2: Monsters/, Moves/)
```

### 1.4 자산 구조 (M-A 빌드 시 적용 완료)

```
assets/sprites/abnormalities/ebony_queen_apple/    # 본 모드는 res:// 루트 마운트 (export_presets.cfg 에 res_prefix 없음)
├── boss.png + boss.png.import                     # 본체 시각화 (Mon-PNG)
└── background.png + background.png.import         # v2 배경 (현재 코드에서 참조 안 함)
```

> ⚠ `res://TheCity/...` 같은 모드 폴더 prefix 는 본 모드 컨벤션 아님. 본 모드는 `assets/` 그대로 res:// 루트에 합쳐짐 — 따라서 코드 경로는 `res://assets/sprites/...`. ModTemplate-StS2 의 `res://ModTemplate/...` 컨벤션은 본 모드 미채택.

### 1.5 부모 클래스 설계 (M-A 작성 완료)

#### `AbnormalityEvent : CustomEventModel`
```csharp
public abstract class AbnormalityEvent : CustomEventModel
{
    public abstract string AbnormalityId { get; }
    protected abstract Type EncounterType { get; }
    public override bool IsShared => true;   // EnterCombatWithoutExitingEvent throw 회피 필수

    protected void StartAbnormalityCombat()
    {
        // ⚠ ModelDb.GetByType 은 게임에 없음. ModelDb.GetId(Type) + GetByIdOrNull<T>(ModelId) 사용.
        var modelId = ModelDb.GetId(EncounterType);
        var encounter = ModelDb.GetByIdOrNull<EncounterModel>(modelId)?.ToMutable()
            ?? throw new InvalidOperationException($"[{ModStart.ModId}] Encounter not found: {EncounterType.Name}");
        EnterCombatWithoutExitingEvent(encounter, Array.Empty<Reward>(), shouldResumeAfterCombat: true);
    }

    public override async Task Resume(AbstractRoom room)
    {
        var combatRoom = (CombatRoom)room;
        var encounter = (AbnormalityEncounter)combatRoom.Encounter;
        if (encounter.IsVictory) await OnVictory(combatRoom);
        else                     await OnDefeat(combatRoom);
    }

    protected abstract Task OnVictory(CombatRoom room);
    protected virtual Task OnDefeat(CombatRoom room) {
        SetEventFinished(L10NLookup($"{AbnormalityId}.defeat"));
        return Task.CompletedTask;
    }
}
```

#### `AbnormalityEncounter : CustomEncounterModel`
```csharp
public abstract class AbnormalityEncounter : CustomEncounterModel
{
    protected AbnormalityEncounter() : base(RoomType.Monster) { }   // BaseLib autoAdd 자동 등록

    public abstract string AbnormalityId { get; }
    public override bool ShouldGiveRewards => true;   // 결재 (c) C1-v1
    public override string CustomBgm => "";           // 결재 (b) B1
    public override bool IsValidForAct(ActModel act) => true;   // v1 모든 act 허용

    public bool IsVictory { get; set; }
    public override Dictionary<string, string> SaveCustomState() => new() {
        ["IsVictory"] = IsVictory.ToString()
    };
    public override void LoadCustomState(Dictionary<string, string> state) {
        IsVictory = bool.Parse(state.GetValueOrDefault("IsVictory", "false"));
    }
}
```

> `IsVictory` 세팅 시점은 M-B 검증 시 결정 (본체 사망 hook + 플레이어 승리 판정). 후보: `CombatManager.IsEnding` Postfix 또는 `Hook.AfterCombatFinished` 류.

#### `AbnormalityMonster : CustomMonsterModel`
```csharp
public abstract class AbnormalityMonster : CustomMonsterModel
{
    public abstract string AbnormalityId { get; }
    protected virtual bool IsSecondaryPart => false;   // true 면 부위, false 면 본체

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        if (IsSecondaryPart)
            await PowerCmd.Apply<MinionPower>(base.Creature, 1m, base.Creature, null);
    }
}
```

#### `AbnormalityRegistry`
```csharp
public static class AbnormalityRegistry
{
    private static readonly Dictionary<string, Type> EventByAbnormalityId = new(StringComparer.Ordinal);

    public static void Register<TEvent>(string abnormalityId) where TEvent : AbnormalityEvent
    {
        if (string.IsNullOrEmpty(abnormalityId))
            throw new ArgumentException("abnormalityId must be non-empty", nameof(abnormalityId));
        EventByAbnormalityId[abnormalityId] = typeof(TEvent);
    }

    public static EventModel? GetEventForAbnormality(string abnormalityId)
    {
        if (!EventByAbnormalityId.TryGetValue(abnormalityId, out var type)) return null;
        var modelId = ModelDb.GetId(type);
        return ModelDb.GetByIdOrNull<EventModel>(modelId);
    }

    public static IReadOnlyCollection<string> RegisteredIds => EventByAbnormalityId.Keys;
}
```

### 1.6 EbonyQueenApple 구현 (단부위 PoC)

#### `EbonyQueenAppleEncounter`
```csharp
public sealed class EbonyQueenAppleEncounter : AbnormalityEncounter
{
    public const string AbnormalityKey = "EbonyQueenApple";   // ⚠ AbstractModel.Id 와 충돌 회피용 명: AbnormalityKey
    public override string AbnormalityId => AbnormalityKey;

    public override IEnumerable<MonsterModel> AllPossibleMonsters
        => new[] { ModelDb.Monster<EbonyQueenAppleHead>() };

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
        => new[] { ((MonsterModel)ModelDb.Monster<EbonyQueenAppleHead>().ToMutable(), (string?)"center") };
}
```

#### `EbonyQueenAppleHead` (Mon-PNG, 본체)
```csharp
public sealed class EbonyQueenAppleHead : AbnormalityMonster
{
    public override string AbnormalityId => EbonyQueenAppleEncounter.AbnormalityKey;
    public override int MinInitialHp => 60;   // PoC placeholder
    public override int MaxInitialHp => 70;

    public override NCreatureVisuals? CreateCustomVisuals()
        => NodeFactory<NCreatureVisuals>.CreateFromResource(
            "res://assets/sprites/abnormalities/ebony_queen_apple/boss.png");

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var idle = new MoveState("IDLE", IdleAction);
        idle.FollowUpState = idle;   // 자기참조 무한 루프
        return new MonsterMoveStateMachine(new[] { idle }, idle);
    }

    private static Task IdleAction(IReadOnlyList<Creature> targets) => Task.CompletedTask;
}
```

### 1.7 EbonyQueenAppleEvent (M-B 산물 — 미작성)

```csharp
public sealed class EbonyQueenAppleEvent : AbnormalityEvent
{
    public override string AbnormalityId => EbonyQueenAppleEncounter.AbnormalityKey;
    protected override Type EncounterType => typeof(EbonyQueenAppleEncounter);

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
        => new[] {
            new EventOption(this, OnApproach, $"{AbnormalityId}.options.APPROACH"),
        };

    private Task OnApproach()
    {
        StartAbnormalityCombat();   // 헬퍼가 ModelDb 조회 + EnterCombatWithoutExitingEvent
        return Task.CompletedTask;
    }

    protected override async Task OnVictory(CombatRoom room)
    {
        SetEventFinished(L10NLookup($"{AbnormalityId}.victory"));
        // EGO 카드 보상은 별도 plan (v2) — 현재는 표준 보상만 (Gold + 카드 3장).
    }
}
```

`ModInit` 등록:
```csharp
// ModStart.ModInit (M-B 추가)
AbnormalityRegistry.Register<EbonyQueenAppleEvent>(EbonyQueenAppleEncounter.AbnormalityKey);
```

---

## 2. 게임 코드 조사 결과

### 2.1 BattlewornDummy 패턴 (이벤트 → 전투 → 복귀 레퍼런스)

`MegaCrit.Sts2.Core.Models.Events.BattlewornDummy` + `BattlewornDummyEventEncounter` 가 정확한 레퍼런스.

```csharp
// 1. EventModel 의 선택지 정의
protected override IReadOnlyList<EventOption> GenerateInitialOptions() {
    return new[] {
        new EventOption(this, OnChoose1, "loc.key.option1"),
    };
}

// 2. 선택지 콜백에서 전투 진입 (extraRewards 인자가 표준 보상 + 합성됨, 결재 (c))
private Task OnChoose1() {
    var encounter = (MyEncounter)ModelDb.Encounter<MyEncounter>().ToMutable();
    EnterCombatWithoutExitingEvent(encounter, Array.Empty<Reward>(), shouldResumeAfterCombat: true);
    return Task.CompletedTask;
}

// 3. 전투 종료 후 자동 호출
public override async Task Resume(AbstractRoom room) {
    var combatRoom = (CombatRoom)room;
    var encounter = (MyEncounter)combatRoom.Encounter;
    if (encounter.IsVictory)              // 커스텀 플래그 + Save/Load 왕복 필수
        SetEventFinished(L10NLookup("loc.key.victory"));
    else
        SetEventFinished(L10NLookup("loc.key.defeat"));
}
```

**핵심 제약:**
- `EnterCombatWithoutExitingEvent` 는 `IsShared == false` 시 `InvalidOperationException` throw → 환상체 EventModel 은 **반드시 `IsShared => true` 필수**.
- `combatRoom.CombatState.AreAllPlayersDead` 는 게임에 없음 (전체 0건). 승패 판정은 **encounter 커스텀 플래그 (`IsVictory` 등) + `SaveCustomState`/`LoadCustomState` 왕복**.
- `SetEventFinished` 인자 타입은 `LocString` — `L10NLookup(...)` 결과 사용.
- `shouldResumeAfterCombat && LayoutType == EventLayoutType.Combat` 조합도 throw — 환상체 EventModel 은 LayoutType 기본값 사용.

### 2.2 BaseLib `Custom*Model` 트리오 (보강 #11)

`Alchyr.Sts2.BaseLib` v3.x 가 `CustomEncounterModel` / `CustomEventModel` / `CustomMonsterModel` 트리오 제공. 모두 `ICustomModel` marker → **자동 등록** + **자동 prefix**.

#### CustomEncounterModel
```csharp
// BaseLib v3.x: Abstracts/CustomEncounterModel.cs
public abstract class CustomEncounterModel : EncounterModel, ICustomModel
{
    public override RoomType RoomType { get; }
    protected CustomEncounterModel(RoomType roomType, bool autoAdd = true)
    {
        RoomType = roomType;
        if (autoAdd) CustomContentDictionary.AddEncounter(this);   // 자동 등록
    }
    public abstract bool IsValidForAct(ActModel act);
    public virtual string? CustomScenePath => null;
    public override bool HasScene => (CustomScenePath != null && ResourceLoader.Exists(CustomScenePath))
                                     || ResourceLoader.Exists(ScenePath);
    public virtual BackgroundAssets? CustomEncounterBackground(ActModel parentAct, Rng rng) => null;
    protected override bool HasCustomBackground => _customBackgroundAssets != null;   // 자동 fallback
    public virtual string? CustomRunHistoryIconPath => null;
    public virtual string? CustomRunHistoryIconOutlinePath => null;
}
```

**내부 Harmony Prefix 3개** (BaseLib 자동 부착):
- `ScenePathPatch` (게임 측 `EncounterModel.ScenePath` getter): `CustomScenePath` 가 null 이면 본체 fallback.
- `GetCustomBackgroundAssets` (게임 측 `EncounterModel.GetBackgroundAssets`, void Prefix): 모드의 `CustomEncounterBackground` 호출 → `_customBackgroundAssets` 캐시.
- `ScenePatch` (게임 측 `EncounterModel.CreateBackgroundAssetsForCustom`): 캐시값 반환, null 이면 본체 fallback.

**핵심**: 모드가 `CustomEncounterBackground` override 안 하면 `_customBackgroundAssets == null` → BaseLib override 의 `HasCustomBackground == false` → 게임이 `parentAct.GenerateBackgroundAssets(rng)` 자동 폴백 → **부모 act 기본 배경 사용, CTD 없음**.

#### 자동 prefix 메커니즘
- `BaseLib/Patches/Content/PrefixIdPatch.cs` 가 `ModelDb.GetEntry` Postfix.
- `ICustomModel` 구현 클래스 → `type.GetPrefix() + originalEntry`.
- `TypePrefix.GetPrefix()` = 네임스페이스 첫 단어 (`.` 앞) 대문자 + `"-"`.
- 본 모드 = `TheCity.*` → `"THECITY-"`.
- 명시 우회: `[CustomID("explicit_id")]` 어트리뷰트.

### 2.3 Slugify (`StringHelper.Slugify`)

PascalCase → SCREAMING_SNAKE_CASE. 게임 본체 4개 EventModel 의 하드코딩 Loc 키로 검증:

| 클래스 | Id.Entry | 출처 |
|---|---|---|
| `BattlewornDummy` | `BATTLEWORN_DUMMY` | BattlewornDummy.cs:46 |
| `BrainLeech` | `BRAIN_LEECH` | BrainLeech.cs:46 |
| `SelfHelpBook` | `SELF_HELP_BOOK` | SelfHelpBook.cs:21 |
| `WarHistorianRepy` | `WAR_HISTORIAN_REPY` | WarHistorianRepy.cs:27 |

**알고리즘 (`StringHelper.Slugify`):**
```csharp
public static string Slugify(string txt)
{
    string text = CamelCaseRegex().Replace(txt.Trim(), "$1_$2");
    string input = WhitespaceRegex().Replace(text.ToUpperInvariant(), "_");
    return SpecialCharRegex().Replace(input, "");
}
[GeneratedRegex("([A-Za-z0-9]|\\G(?!^))([A-Z])")]   // CamelCaseRegex
[GeneratedRegex("\\s+")]                              // WhitespaceRegex
[GeneratedRegex("[^A-Z0-9_]")]                        // SpecialCharRegex
```

**컨텍스트별 표기 (구현 시 주의):**
| 컨텍스트 | 표기 | 예 |
|---|---|---|
| `Id.Entry` 자체 | SCREAMING_SNAKE_CASE | `EBONY_QUEEN_APPLE_ENCOUNTER` |
| Loc 키 | `Id.Entry` 그대로 | `"EBONY_QUEEN_APPLE_ENCOUNTER.title"` |
| 폴더 경로 | `Id.Entry.ToLowerInvariant()` | `res://scenes/backgrounds/ebony_queen_apple_encounter/` |
| BaseLib prefix 결합 | prefix + entry | `"THECITY-EBONY_QUEEN_APPLE_ENCOUNTER"`, 폴더 `thecity-ebony_queen_apple_encounter` |

**작명 규약:**
- PascalCase 작성 시 인접 대문자 (`XMLParser` → `X_M_L_PARSER`) 회피 → `XmlParser` 권장.
- 클래스명 끝에 `Model` 붙이지 않기 (`SlugifyCategory` 의 `_MODEL` 접미사 제거 동작).
- 모드 const `Id` 사용 시 `AbstractModel.Id` 와 CS0108 충돌 → `AbnormalityKey` / `Slug` / `Identifier` 같은 다른 이름 권장.

### 2.4 배경 자산 파이프라인 (보강 #11/#12)

게임 본체 `BackgroundAssets` (`MegaCrit.Sts2.Core.Rooms/BackgroundAssets.cs:26-65`):
- 경로: `res://scenes/backgrounds/{title}/` (where `title = Id.Entry.ToLowerInvariant()`)
- 메인: `{title}_background.tscn` — 루트는 `NCombatBackground : Control`, 자식 노드 `Layer_00, Layer_01, ..., Layer_NN, Foreground` 강제 (zero-pad 2자리).
- 레이어: `layers/` 서브디렉토리 + `_bg_<group>_<variant>.tscn` (그룹별 RNG 1개) + `_fg_*.tscn` (옵션, 1개 선택 또는 0).
- 파일명에 `_bg_` 또는 `_fg_` 둘 중 하나 없으면 즉시 `InvalidOperationException`. layers/ 하위 디렉토리도 throw.

**BaseLib 단일 PNG 헬퍼 부재** — `CustomBackgroundAssets(layersPath, bgScenePath, rng)` 가 디렉토리 + 파일명 패턴 강제. 단일 PNG 만으론 통과 안 함. v2 에서 자산 정리 (`bg_default_1.png` 리네임 + 더미 `fg_empty_1.png` + `_background.tscn`) 후 사용 가능, 작업량 < 30분.

**v1 권장 = Bg-NONE** — `CustomEncounterBackground` override 안 함 → BaseLib 자동 폴백 → 부모 act 기본 배경 사용.

### 2.5 단일 PNG 보스 시각화 (보강 #12)

BaseLib 가 공식 단일 PNG 헬퍼 제공:

```csharp
// AbnormalityMonster 또는 자식
public override NCreatureVisuals? CreateCustomVisuals()
    => NodeFactory<NCreatureVisuals>.CreateFromResource("res://assets/.../boss.png");
```

`BaseLib/Utils/NodeFactories/NCreatureVisualsFactory.cs` 의 `CreateBareFromResource(Texture2D)`:
1. 이미지 사이즈 + 10% 인플레이션으로 bounds 계산.
2. `NCreatureVisuals` 루트 (Control) 생성.
3. `Bounds` 자식 (Control) 추가.
4. `Visuals` 자식에 `Sprite2D` 추가 — 텍스처 = 입력 PNG.

**위키 명시**: "A single PNG image is enough to create creature visuals."

**한계**: 단일 Sprite2D 라 애니메이션 0 (Idle/Hit/Attack/Cast/Dead 모두 동일 정지 이미지). v1 PoC 충분, v2 다부위·애니메이션 도입 시 별도 작업.

**위험 (Mon-NONE = 절대 금지)**: 모드가 `CreateCustomVisuals` null 반환 + 자산 부재 → 게임 본체 `VisualsPath` 폴백 (`res://scenes/creature_visuals/{slug}.tscn`) → ResourceLoader 실패 → **CTD**. 배경 (`HasCustomBackground` 자동 폴백) 과 다름 — 보스는 자동 폴백 없음.

### 2.6 MinionPower 적용 API (다부위 패턴, v2 활용)

`MegaCrit.Sts2.Core.Commands.PowerCmd.Apply<T>(target, amount, applier, cardSource)`:

```csharp
await PowerCmd.Apply<MinionPower>(base.Creature, 1m, base.Creature, null);
```

**바닐라 8건 사용처:** TorchHeadAmalgam.cs:43 / GasBomb.cs:55 / KinFollower.cs:103 / Fabricator.cs:105 / Ovicopter.cs:81 / MockAttackAndSummonMinionMonster.cs:32 / IllusionPower.cs:65.

`CombatCmd.ApplyPower` 는 게임에 **존재하지 않음**.

**`MinionPower` 정의:**
```csharp
public sealed class MinionPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override bool ShouldPlayVfx => false;
    public override bool OwnerIsSecondaryEnemy => true;   // 핵심 — IsPrimaryEnemy 카운팅 제외
    public override bool ShouldPowerBeRemovedAfterOwnerDeath() => false;
    public override bool ShouldOwnerDeathTriggerFatal() => false;
}
```

**다부위 종료 동작**: `CombatManager.IsEnding` 의 `Enemies.Any(e => e.IsAlive && e.IsPrimaryEnemy)` 체크 → 부위 (Secondary) 는 카운트 제외 → **본체 (Primary) 만 죽으면 부위 살아있어도 자동 종료**.

### 2.7 보상 흐름 (`EnterCombatWithoutExitingEvent` + `ShouldGiveRewards`)

`EnterCombatWithoutExitingEvent(EncounterModel, IReadOnlyList<Reward> extraRewards, bool shouldResumeAfterCombat)`:
- `extraRewards` 는 `combatRoom._extraRewards` 딕셔너리에 추가.
- 빈 배열 (`Array.Empty<Reward>()`) = 추가 보상 0.

**보상 화면 흐름** (`RewardsCmd.OfferForRoomEnd` → `RewardsSet`):
- `ShouldGiveRewards == true`: `WithRewardsFromRoom` → 표준 보상 (Gold + 카드 3장 + 옵션 Potion) + `combatRoom.ExtraRewards` 합성.
- `ShouldGiveRewards == false`: `EmptyForRoom` → 빈 보상 화면 (`_allowEmptyRewards=true`).

**결합 매트릭스 (구현 시 함정 회피):**

| ShouldGiveRewards | extraRewards | 결과 |
|---|---|---|
| **true** | **빈 배열** | **표준 보상만 (← v1 정답, 결재 (c) C1-v1)** |
| true | non-empty | 표준 + extraRewards 합성 (← v2 EGO 카드 도입 시) |
| false | 빈 배열 | 빈 보상 화면 |
| false | non-empty | ⚠ **extraRewards 침묵 무시** (`RewardsSet.EmptyForRoom` 분기에 합성 코드 없음) |

⚠ **함정**: `ShouldGiveRewards=false` + non-empty `extraRewards` 조합은 침묵 무시. BattlewornDummy 가 이 조합 안 쓰는 이유 — Setting 별 보상 유형 분리 (Potion / 카드 업그레이드 / Relic 즉시) 라 OfferCustom 직접 호출.

### 2.8 BaseLib 오디오 (`Utils/FmodAudio`)

BaseLib 가 풍부한 FMOD 래퍼 제공 (30+ 메서드):
- 이벤트 재생: `PlayEvent(eventPath)` / `PlayEventByGuid(guid)` / `CreateEventInstance(eventPath)`
- 커스텀 파일: `PlayFile(absolutePath, volume, pitch)` / `PreloadFile` / `CreateSoundInstance`
- 음악: `PreloadMusic(absolutePath)` / `PlayMusic(absolutePath, volume, pitch)`
- FMOD 뱅크: `LoadBank(bankPath)` / `UnloadBank(bankPath)`
- 사운드 교체: `RegisterReplacement` / `RegisterFileReplacement` / `RegisterEventReplacement`

`CustomEncounterModel.CustomBgm` override 한 줄로 게임 측 자동 처리:
```csharp
public override string CustomBgm => "event:/music/act3_boss_queen";
```

**v1 권장 = B1**: `CustomBgm => ""` (자산 미준비, v2 이관).

### 2.9 이벤트 ID prefix 충돌

바닐라 EventModel/AncientEventModel **76개 슬러그 전수 검증** — `Abnormality*` / `ABNORMALITY_*` 매치 0건. BaseLib 자동 prefix `THECITY-` 적용 시 자연 격리.

**충돌 검출:**
- `AbstractModel` 생성자: 같은 **Type** 두 번 등록 시 `DuplicateModelException`.
- `ModelDb.Init`: 같은 ID 다른 Type 이면 **사일런트 덮어쓰기** (예외 없음). BaseLib `ICustomModel` prefix 가 자연 회피.

### 2.10 ModelDb API (M-A 발견 정정)

`ModelDb.GetByType(Type)` 는 **게임에 존재하지 않음**. 정확한 Type → AbstractModel 변환:
```csharp
var modelId = ModelDb.GetId(Type);                          // public static
var model = ModelDb.GetByIdOrNull<EventModel>(modelId);     // public static generic
```

`ModelDb.Event<T>` / `Encounter<T>` / `Monster<T>` 같은 generic accessor 도 사용 가능 — 컴파일 시점에 타입 알면 이쪽이 더 단순.

### 2.11 MonsterModel 추상 멤버 (M-A 발견)

`MonsterModel` 추상 멤버 (m1.md / plan v1.1 본문에 명시 부재였음):
- `public abstract int MinInitialHp { get; }`
- `public abstract int MaxInitialHp { get; }`
- `protected abstract MonsterMoveStateMachine GenerateMoveStateMachine();`

**PoC stub 패턴 (단순 idle 무한 루프):**
```csharp
protected override MonsterMoveStateMachine GenerateMoveStateMachine()
{
    var idle = new MoveState("IDLE", IdleAction);
    idle.FollowUpState = idle;   // 자기참조
    return new MonsterMoveStateMachine(new[] { idle }, idle);
}
private static Task IdleAction(IReadOnlyList<Creature> targets) => Task.CompletedTask;
```

본격 AI 패턴 (인텐트 + 데미지) 은 v2.

---

## 3. 사용자 결재

> 환상체전 v1 PoC 진입 자격 결재. M-A 빌드는 권장안 모두 채택 가정으로 진행 — 사용자 정합 confirm 받으면 M-B/M-C 진입 가능.

### 3.1 결재 항목 (5건)

#### (a) 클래스명 prefix 규약

| 옵션 | 결과 `Id.Entry` | 평가 |
|---|---|---|
| **권장 ★** A1: `EbonyQueenAppleEncounter` (BaseLib `Custom*Model` 트리오, 클래스명 prefix **없음**) | `THECITY-EBONY_QUEEN_APPLE_ENCOUNTER` (BaseLib 자동 prefix `THECITY-` 부착) | 깔끔. BaseLib `ICustomModel` 자동 prefix 가 모드 격리 + 사일런트 충돌 방지. |
| A2: `TheCityEbonyQueenAppleEncounter` (이중 prefix) | `THECITY-THE_CITY_EBONY_QUEEN_APPLE_ENCOUNTER` | 가독성 떨어짐. 의미상 중복. |
| A3: `EncounterModel` 직접 상속 + 클래스명 `TheCity` 강제 | `THE_CITY_EBONY_QUEEN_APPLE_ENCOUNTER` | BaseLib 자동 등록·Harmony 인프라 포기. 비추천. |

**근거**: §2.2 BaseLib 자동 prefix + §2.9 76 슬러그 충돌 검증.

#### (b) v1 BGM

| 옵션 | 평가 |
|---|---|
| **권장 ★** B1: `CustomBgm => ""` (게임 기본 BGM) | v1 자산 미준비. v2 에서 `event:/music/...` 한 줄. |
| B2: v1 부터 커스텀 BGM | `.ogg` 자산 + FMOD 이벤트 + 자산 디자인 별개. PoC 핵심과 무관. |

**근거**: §2.8 BaseLib `FmodAudio` 인프라 충분, v1 자산 작업 회피.

#### (c) 보상 UI 호출 위치 — **2026-04-25 채택**

| 옵션 | 평가 |
|---|---|
| **★ 채택** C1-v1: `ShouldGiveRewards=true` + `extraRewards: Array.Empty<Reward>()` (표준 보상만) | v1 PoC 핵심 검증에 집중. 인자만 채우면 v2 EGO 도입 가능 — 코드 구조 변경 0. |
| C1-v2: `ShouldGiveRewards=true` + `extraRewards: [new CardReward(egoOptions, 3, player)]` | v2 EGO 카드 도입 시 활성화. 메커니즘 동일 구조. |
| C2: `ShouldGiveRewards=false` + Resume `RewardsCmd.OfferCustom` (BattlewornDummy 패턴) | 표준 보상 풀 차단. 사용자 요구 ("기본 보상 UI 로 처리") 와 불일치. 채택 안 함. |
| C3: `TryModifyRewards` override (PowerModel/RelicModel) | 환상체별 차등 보상. v2 보류. |

**v1 → v2 마이그레이션 비용 = 0 (인자만 채우면 됨):**
```csharp
// v1 (채택)
EnterCombatWithoutExitingEvent<EbonyQueenAppleEncounter>(
    Array.Empty<Reward>(), shouldResumeAfterCombat: true);

// v2 EGO 도입 시 — 첫 인자만 교체
EnterCombatWithoutExitingEvent<EbonyQueenAppleEncounter>(
    new[] { new CardReward(egoOptions, 3, base.Owner) }, shouldResumeAfterCombat: true);
```

> EGO 카드 시스템은 별도 plan (미작성). v2 진입 시점에 `doc/plan/ego-card-system.md` 작성 후 그 plan 의 결재를 거쳐 EGO 카드 풀·CardCreationOptions 정의·CardReward 슬롯 통합. 본 결재는 v1 표준 보상까지만.

**함정 (반드시 회피, v1/v2 공통):**

| ShouldGiveRewards | extraRewards | 결과 |
|---|---|---|
| **true** | **빈 배열** | **표준 보상만 (← v1 정답)** |
| true | non-empty | 표준 + extraRewards 합성 (← v2 정답) |
| false | 빈 배열 | 빈 보상 화면 |
| false | non-empty | ⚠ **extraRewards 침묵 무시** (`RewardsSet.EmptyForRoom` 분기에 합성 코드 없음) |

#### (d-1) v1 배경 자산

| 옵션 | 평가 |
|---|---|
| **권장 ★** Bg-NONE: `CustomEncounterBackground` override 안 함 | BaseLib `_customBackgroundAssets == null` → BaseLib override `HasCustomBackground => false` → 게임이 `parentAct.GenerateBackgroundAssets(rng)` 자동 폴백 → 부모 act 기본 배경. **자산 0, 안전, CTD 위험 없음**. |
| Bg-tscn: v1 부터 자산 포함 | 단일 PNG 헬퍼 없음. `CustomBackgroundAssets` 가 디렉토리 + 파일명 패턴 강제. 사용자 `Background.png` 자산 정리 (리네임 + 더미 fg + 빈 .tscn) 후 사용 가능. v2 작업량 < 30분. |

**근거**: §2.4 BaseLib `HasCustomBackground` 자동 폴백 메커니즘.

#### (d-2) v1 보스 시각화 — **자산 + 코드 둘 다 필수**

| 옵션 | 평가 |
|---|---|
| **권장 ★** Mon-PNG: 단일 PNG 직접 사용 — `NodeFactory<NCreatureVisuals>.CreateFromResource(pngPath)` 한 줄 | BaseLib 공식 지원. 위키 명시: "A single PNG image is enough to create creature visuals." 사용자 `Boss.png` 그대로 v1 부터 사용. 코드 4줄, .tscn 작성 0개. |
| Mon-tscn: `.tscn` 기반 visuals (Spine 또는 다중 노드) | 애니메이션·다부위 표현 가능. v2 다부위 환상체 도입 시 적합. |
| ⚠ Mon-NONE: 자산·코드 둘 다 미포함 | `CreateCustomVisuals` null → 게임 본체 `VisualsPath` 폴백 → 자산 부재 → ResourceLoader 실패 → **CTD 위험 큼**. 절대 권장 안 함. (배경의 자동 폴백과 다름.) |

**근거**: §2.5 단일 PNG 보스 시각화 헬퍼 + Mon-NONE CTD 위험.

### 3.2 권장 패키지 (M-A 빌드 시 채택)

| 결재 | 권장안 | 비고 |
|---|---|---|
| (a) | A1 | BaseLib 자동 `THECITY-` prefix |
| (b) | B1 | `CustomBgm => ""` |
| (c) | C1-v1 | **2026-04-25 사용자 채택 완료** |
| (d-1) | Bg-NONE | 부모 act 기본 배경 |
| (d-2) | Mon-PNG | `Boss.png` 직접 참조 |

→ **M-A 빌드는 위 패키지로 통과**. 사용자 (a)/(b)/(d-1)/(d-2) 정합 confirm 받으면 M-B 진입 가능. 다른 결재 (예: A2 클래스명 prefix 도입) 답하면 부모 클래스 일부 수정 필요.

---

## 4. 정적 한계 → 런타임 검증 (Wave 2)

> 정적으로 답이 안 나온 11건. ★ 표시는 v1 진입 직전 권장 검증 항목.

| ID | 항목 | v1 권장 결정 시 영향 |
|---|---|---|
| L1 | §2.3 인접 대문자 케이스 (`XMLParser` 식) 정확한 슬러그 | **무관** — 작명 회피로 우회 (§2.3 작명 규약). |
| L2 | §2.4 바닐라 `*_background.tscn` 노드 트리 (Layer_NN N 값 등) | **Bg-NONE 무관**, v2 자산 추가 시 PCK 추출. |
| L3 | §2.4 모드 PCK res:// 경로 마운트 동작 | **Bg-NONE 무관**. 본 모드 `export_presets.cfg` 에 `res_prefix` 없어 루트 마운트 — Mon-PNG 검증 시 함께 (§L9). |
| L4 | §2.6 `Hook.ShouldStopCombatFromEnding` 컬렉션 순회 범위 (`ShouldReceiveCombatHooks=false` 영향) | **v2 페이즈 전환 도입 시 필요**, v1 무관. |
| L5 ★ | §2.2 BaseLib `ICustomModel` 자동 prefix 가 EventModel 에 적용되는지 | **A1 + Custom*Model 트리오 채택 시 자동 해결**. 미해결 시 수동 prefix 강제. **v1 진입 직전 1회 검증 권장**. |
| L6 | §2.7 `EncounterModel.TryModifyRewards` 호출 여부 (`ShouldReceiveCombatHooks=false` 영향) | **C1 (extraRewards) 채택 시 무관**. 메커니즘 B 사용 시 검증. |
| L7 | §2.7 멀티플레이어 ExtraRewards sync (호스트만 받는지) | **싱글 PoC 단계 무관**. 멀티플레이 검증 시 별도 plan. |
| L8 | §2.4 보강: 게임 본체 `EncounterModel.ScenePath` getter 의 fallback 토큰 합성 (`<modname>-<entry>`) 규칙 | **Bg-NONE 채택 시 무관** — BaseLib `HasCustomBackground` override 가 자동 회피. fallback 의존 시에만 critical (사용 안 함). |
| L9 ★ | §2.5 보강: 단일 sprite 보스의 게임 측 기본 animator 호환성 — `CreateFromResource` 가 만든 `NCreatureVisuals` 에 게임이 부착하는 기본 animator 가 idle-only 와 충돌 없는지 | **Mon-PNG 채택 시 v1 진입 직전 1회 검증 권장**. 위험 시 `SetupCustomAnimationStates` override + BaseLib `SetupAnimationState(controller, idleName: "default")` 헬퍼로 idle-only animator 명시 구성. |
| L10 | §2.4 보강: `BgLayers` 의 PNG 경로 string 이 게임 본체 `AddLayer` 의 `GetScene(layerPath).Instantiate<Control>()` 강제와 호환되는 메커니즘 | **Bg-NONE 채택 시 무관**. v2 (Bg-tscn) 자산 정리 시점에 sts-game-analyst 추가 확인 필요. |
| L11 ★ | M-A 발견: `EncounterModel.L10NLookup(string key)` 본체가 `new LocString("encounters", key)` 로 **`"encounters"` 테이블을 하드코딩**. 본 모드는 `thecity.json` 단일 테이블 + `LocTableInjector` 가 `tables["thecity"]` 만 등록 — 키 lookup 실패 가능. 영향: Encounter Title/Description, EventModel `L10NLookup` (다른 테이블 가능성) | **M-B 진입 전 sts-game-analyst 추가 의뢰** — `LocManager` 의 fallback 메커니즘 + `EventModel.L10NLookup` 의 테이블명 확정. 결과 따라 (a) `LocTableInjector` 가 `thecity` 외 `encounters`/`events` 별칭 등록 vs (b) 모드측 `LocString("thecity", key)` 명시. **본 항목 미해결 시 M-B 콘솔 `event` 호출에서 텍스트 빈 표시 가능**. |

→ **권장 결정 (A1/B1/C1/Bg-NONE/Mon-PNG) 채택 시 v1 진입 직전 필수 런타임 검증 = L5 + L9 + L11 (3건)**.

---

## 6. 라우팅 (M-C 단계)

> M-A/M-B 완료 후 진입. 맵 노드 → 환상체 이벤트 단일 경로. **2026-04-29 정정 (§6.5 참조)**: 분석가 두 명 보고 결과 §6.1~§6.4 의 sample code/접근 방식이 게임 API 와 일부 불일치 발견 → §6.6 (M-C 구현 직전 사양, v1) 으로 대체. §6.1~§6.4 는 역사적 맥락 보존 위해 유지.

### 6.1 단일 경로 — `Hook.AfterMapGenerated` (hydrate) + `Hook.ModifyNextEvent` (라우팅) — *원안 (정정 전)*

> ⚠ **2026-04-29 정정**: 본 섹션의 sample code (`Hook.X += handler` 패턴) 가 게임 API 와 호환 안 됨. `AbstractModel.ModifyNextEvent` virtual override 시그니처가 `EventModel` 만 받고 `IRunState` 부재 → 좌표 추출 불가. 정정된 패턴은 §6.6 참조.

게임 API 추정 없이 공식 훅 2개로 완결. `RunManager.CreateRoom` Prefix / `IRunState.ExtraFields` 등 추가 메커니즘 **불필요**.

#### Hydrate — `Hook.AfterMapGenerated(IRunState, ActMap, int actIndex)`
- 카테고리: 기본 (general).
- 호출 시점: act 진입 / 세이브 로드 양쪽 모두 발화 (`RunManager.cs:577`).
- 모드는 ActMap 순회 → 모든 Abnormality 노드에 대해 `AbnormalityRegistry._nodeAssignments[(IRunState.Id, actIndex, col, row)] = abnormalityId` 동기화.
- 결정론 해시 (`AbnormalityMapInjector.Inject` 와 동일 공식) 로 같은 입력 → 같은 출력 보장.

#### Routing — `Hook.ModifyNextEvent(IRunState, EventModel currentEvent)`
- 카테고리: `modify/general`.
- 호출 시점: `ActModel.PullNextEvent` 내부 (`ActModel.cs:340`).
- `currentEvent` 는 `_rooms.NextEvent` (`EnsureNextEventIsValid` 선행으로 비-null 보장).

```csharp
// AbnormalityEventRouter.cs (M-C 원안 sample — § 6.6 으로 대체됨)
Hook.AfterMapGenerated += OnAfterMapGenerated;   // hydrate
Hook.ModifyNextEvent   += OnModifyNextEvent;     // routing

static EventModel OnModifyNextEvent(IRunState runState, EventModel currentEvent)
{
    if (runState.CurrentMapCoord is not { } coord) return currentEvent;
    var key = (runState.Id, runState.CurrentActIndex, coord.Column, coord.Row);
    if (!AbnormalityRegistry._nodeAssignments.TryGetValue(key, out var id)) return currentEvent;
    return AbnormalityRegistry.GetEventForAbnormality(id) ?? currentEvent;
}
```

### 6.2 Option A — Dictionary + 결정론 해시 — *원안 (정정 전)*

> ⚠ **2026-04-29 정정**: v1 환상체 종류 = 1개 (EbonyQueenApple) 라 `hash % 1 == 0` 결정. 결정론 해시 + Dictionary 둘 다 v1 PoC 단계에서 **불필요**. v2 다중 환상체 도입 시점에 활성화. 정정된 단순화 경로는 §6.6 참조.

**왜 Option A 인가**: mod-code-analyst 옵션 비교 결과 — `SerializableMapPoint` 직렬화 확장 (B), Godot SetMeta (C), 이벤트 풀 RNG (D) 모두 단점 있음. Option A 만 패치 0개 + publicizer 불필요.

**구현 세부:**
1. `AbnormalityMapInjector.Inject(...)` 가 노드 교체 시점에 이미 결정론 해시 (`hash(seed, actIndex, col, row)`) 계산 중 — 같은 시점에 `registeredIds[hash % count]` 으로 환상체 ID 골라 Dictionary 저장.
2. 이벤트 진입 시점 (`Hook.ModifyNextEvent`) 에 `runState.CurrentMapCoord` / `runState.CurrentActIndex` 로 lookup.
3. **세이브/로드 왕복**: Dictionary 자체는 저장 안 함. **lazy hydrate** — lookup miss 시 즉석 해시 계산 (같은 입력 → 같은 출력 보장).
4. **Hydrate 주 경로**: `Hook.AfterMapGenerated` (위 6.1).

### 6.3 게임 API 검증 (verification §5.4 흡수 + 2026-04-29 보강)

| API | 결론 | 출처 |
|---|---|---|
| `IRunState.CurrentMapCoord` | **존재 확인** (`MapCoord?` public, hook 호출 시점 non-null 보장 — `AddVisitedMapCoord` 가 `EnterMapPointInternal` 이전 호출) | IRunState.cs / RunManager 호출 순서 |
| `IRunState.CurrentActIndex` | `int { get; set; }` public | IRunState.cs |
| `IRunState.Rng` | `RunRngSet` public, `Seed` (uint, run 전역 결정론) 포함 | IRunState.cs / RunRngSet.cs:57 |
| `MapCoord` 멤버 | `public int col, row` (struct, IEquatable, IPacketSerializable) — `Column`/`Row` 가 아니라 **`col`/`row`** | MapCoord 정의 |
| `Hook.ModifyNextEvent(IRunState, EventModel)` | 존재, 호출 지점 1곳 (`ActModel.PullNextEvent` 내부) — Postfix 패치 단일 진입점 | Hook.cs:1365 / ActModel.cs:340 |
| `Hook.AfterMapGenerated(IRunState, ActMap, int)` | 존재, 호출 지점 2곳 (`RunManager.cs:571, 577` — act 진입 + 세이브 로드 양쪽) | Hook.cs:461 |
| `Hook.ModifyGeneratedMap` | 본 모드 이미 사용 중 (`MapPointTypePatches.cs`), Postfix Harmony 패턴 | 자체 코드 |
| `EventModel.Owner` 시점 | `BeginEvent` 호출 전이라 `PullNextEvent` 시점에 **null** — `EventModel` 인자만 보고 RunState 도달 불가, Hook.ModifyNextEvent Postfix 가 유일 경로 | sts-game-analyst 결론 |

### 6.4 잔여 리스크 (M-C 단계 검증)

- **R1**: `Hook.ModifyNextEvent` 는 Event 방에서만 발화. 환상체가 실제 `RoomType.Event` 로 진입함을 `src/Map/MapPointTypePatches.cs` (`RollRoomTypeFor` Prefix) 가 이미 보장 — 재확인 불요.
- **R2**: 세이브 로드 시 `SavedActMap` 으로 복원된 맵이 `Hook.AfterMapGenerated` 에 전달되는 타이밍 — 다부위 partial load 엣지 케이스는 런타임 QA.
- **R3**: 멀티플레이 peer 간 `Hook.ModifyNextEvent` 결정론성 — 해시 시드가 모든 peer 에서 동일해야 함. `AbnormalityMapInjector` 가 이미 결정론 해시 사용으로 자연 보장.
- **R4**: 첫 방 진입 시 `CurrentMapCoord` null window — sts-game-analyst 결론: `AddVisitedMapCoord` 가 `EnterMapPointInternal` 이전 호출이라 `Hook.ModifyNextEvent` 시점에 **non-null 보장**. `EnterRoomDebug` 디버그 경로만 stale 가능 (실제 플레이 영향 없음). null 체크는 방어적으로 유지 권장.
- **R5 (신규, 2026-04-29)**: `MapPointType.Ancient` 는 `CreateRoom` 분기에서 `PullAncient()` 로 빠져 `Hook.ModifyNextEvent` 미발화. 본 모드 환상체는 `MapPointType.Abnormality(=200)` (Ancient 와 다른 sentinel) → 일반 Event 분기 → `PullNextEvent` → hook 발화. **무관**.
- **R6 (신규, 2026-04-29)**: `Hook.ModifyNextEvent` 가 반환한 `EventModel` 인스턴스가 캐노니컬일 가능성 — `EventRoom` 생성자가 `ToMutable()` 자동 처리하는지 미확인. **mitigation**: 라우터에서 미리 `ModelDb.GetByIdOrNull<EventModel>(...).ToMutable()` 형태로 mutable instance 반환 (이미 `AbnormalityRegistry.GetEventForAbnormality` 가 `ModelDb.GetByIdOrNull<EventModel>` 호출). v1 PoC 진입 후 콘솔 검증 필요.

### 6.5 갱신 이력 (§6 내부)

| 일자 | 갱신 | 산출물 |
|---|---|---|
| 2026-04-22 | §6 초안 | Plan v1.1 본문 |
| 2026-04-22 | Verification 흡수 | `Hook.AfterMapGenerated` + `Hook.ModifyNextEvent` 단일 경로 확정 |
| 2026-04-29 | M-C 진입 직전 분석가 정정 | sts-game-analyst (Hook 시그니처/Postfix 패턴/IRunState non-null) + mod-code-analyst (단일 누락점/종속 4건/v1=1개 단순화) → §6.6 신설 |
| 2026-04-30 | §6.6.1 sample 실 구현 동기화 | 실 구현 시 `IRunState.CurrentMapPoint` 가 진입 노드 직접 반환 확인 → LINQ 우회 제거 / `AbnormalityPreflight.Healthy` 게이트 추가 / Postfix 시그니처에서 `currentEvent` 인자 제거 / XML 문서 확장. plan ↔ 실 코드 정합성 회복. |

### 6.6 M-C 구현 직전 사양 (v1, 2026-04-29 확정)

**§6.1~§6.4 와의 차이 (요약):**
| 항목 | §6.1 원안 | §6.6 v1 정정 |
|---|---|---|
| Hook 진입 방식 | `Hook.X += handler` 이벤트 등록 | **Harmony Postfix on `Hook.ModifyNextEvent` static method** |
| Hook 갯수 | 2개 (`AfterMapGenerated` hydrate + `ModifyNextEvent` routing) | **1개 (`ModifyNextEvent` 만)** — hydrate 불필요 |
| Dictionary | `_nodeAssignments[(runId, actIndex, col, row)] = abnormalityId` | **불필요** (v1 환상체 1개라 hash % 1 = 0) |
| 결정론 해시 함수 | `AbnormalityMapInjector` 와 동일 공식 재사용 | **v1 작성 안 함** (v2 도입 시 같은 공식 추출 헬퍼로 분리) |
| Registry 신규 메서드 | `_nodeAssignments` dict 직접 노출 | `GetAbnormalityIdForCoord(IRunState, MapCoord)` 캡슐화 메서드 (v1 = `RegisteredIds.First()` 반환) |

**근거**:
1. `AbstractModel.ModifyNextEvent` virtual override 시그니처는 `EventModel` 만 받음 (AbstractModel.cs:759). hook listener 등록 경로로는 `IRunState` 접근 불가 → §6.1 의 `Hook.X += handler` 패턴 사용 불가.
2. `EventModel.Owner` 는 `BeginEvent` 호출 전이라 `PullNextEvent` 시점에 null → `currentEvent` 인자에서 RunState 도달 경로 없음.
3. **유일 깨끗한 경로**: `Hook.ModifyNextEvent` static 메서드 자체를 Harmony Postfix 패치 → `IRunState runState, EventModel currentEvent, ref EventModel __result` 인자에서 직접 사용 (게임 호출 1곳뿐 = `ActModel.PullNextEvent`).
4. 본 모드 기존 컨벤션과 일치 — `MapPointTypePatches.cs:192` 의 `Hook.ModifyGeneratedMap` Postfix 패턴 동일.

#### 6.6.1 코드 사양 — `AbnormalityEventRouter`

> **2026-04-30 갱신** (실 구현과 동기화): 본 sample 은 처음에 `runState.CurrentMapCoord` + `map.GetAllMapPoints().FirstOrDefault(p => p.coord.col == ... && p.coord.row == ...)` LINQ 우회 (`ActMap.GetPoint(MapCoord)` 시그니처 미확인 회피용) 였으나, M-C 실 구현 시 `IRunState.CurrentMapPoint` 가 현재 진입 노드를 직접 반환함이 확인돼 LINQ 검색 + `runState.Map` 접근 모두 불필요하게 됨 (빌드 통과로 API 실재 검증). 추가로 ① `AbnormalityPreflight.Healthy` 게이트를 첫 줄에 삽입 (게임 업데이트로 `Hook.ModifyNextEvent` 시그니처 변경 시 라우팅만 단락, 다른 patch 는 유지) ② Postfix 메서드는 `currentEvent` 인자 미사용으로 시그니처 단축 (`IRunState runState, ref EventModel __result` 만) ③ XML 문서를 설계 근거 + Preflight 동작까지 확장. `using System.Linq` / `using Godot` / `using MegaCrit.Sts2.Core.Map` 모두 제거.

```csharp
// src/Abnormality/MapIntegration/AbnormalityEventRouter.cs
using HarmonyLib;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using TheCity.Abnormality.Template;
using TheCity.Map;

namespace TheCity.Abnormality.MapIntegration;

/// <summary>
/// 환상체 맵 노드 진입 시 등록된 환상체 <see cref="EventModel"/> 로 라우팅. <see cref="Hook.ModifyNextEvent"/> Postfix.
///
/// <b>설계 근거 (plan §6.6, 2026-04-29 분석가 정정):</b>
/// <list type="bullet">
/// <item><c>AbstractModel.ModifyNextEvent</c> virtual override 시그니처는 <see cref="EventModel"/> 만 받음
///       → <see cref="IRunState"/> 접근 불가. <c>Hook.X += handler</c> 패턴 사용 불가.</item>
/// <item>유일 깨끗한 경로: <see cref="Hook.ModifyNextEvent"/> static 메서드 자체를 Harmony Postfix
///       (<see cref="MapPointTypePatches.Hook_ModifyGeneratedMap_Patch"/> 와 동일 패턴).</item>
/// </list>
///
/// <b>v1 단순화</b>: 환상체 1개 (EbonyQueenApple) 가정. <see cref="AbnormalityRegistry.GetAbnormalityIdForCoord"/>
/// 가 등록된 첫 ID 반환. v2 다중 환상체 도입 시 좌표별 결정론 해시 분기는 Registry 안에서 캡슐화 — 본 라우터는
/// 코드 변경 없음.
///
/// <b>Preflight</b>: <see cref="AbnormalityPreflight.Healthy"/> == false 면 단락 (게임 업데이트로
/// <see cref="Hook.ModifyNextEvent"/> 시그니처 변경 시점 등). switch 패치는 유지하되 라우팅만 비활성화.
/// </summary>
[HarmonyPatch(typeof(Hook), nameof(Hook.ModifyNextEvent))]
internal static class AbnormalityEventRouter
{
    [HarmonyPostfix]
    public static void Postfix(IRunState runState, ref EventModel __result)
    {
        // 게임 업데이트 / sentinel 충돌 시 라우팅만 비활성화 — 기존 패치들은 유지.
        if (!AbnormalityPreflight.Healthy) return;

        // 디버그/console 경로 등 진입 좌표 미확정 시 패스.
        // CurrentMapPoint 가 non-null 이면 PointType 직접 검사 (map.GetPoint 추측 호출 불필요).
        var currentPoint = runState.CurrentMapPoint;
        if (currentPoint == null) return;

        // 현재 맵 노드가 환상체 노드가 아니면 원본 currentEvent 유지.
        if (currentPoint.PointType != AbnormalityMapPointType.Abnormality) return;

        // 환상체 ID 결정 (v1 = 첫 ID, v2 = 결정론 해시).
        var abnormalityId = AbnormalityRegistry.GetAbnormalityIdForCoord(runState, currentPoint.coord);
        if (abnormalityId == null) return;

        // 등록된 환상체 EventModel 로 교체. miss 시 __result 그대로 유지.
        var newEvent = AbnormalityRegistry.GetEventForAbnormality(abnormalityId);
        if (newEvent != null)
            __result = newEvent;
    }
}
```

**캡슐화 의도**: `GetAbnormalityIdForCoord` 신규 추가로 v1 → v2 전환 시 라우터 코드 변경 0. v2 구현 변경은 Registry 안에서 (결정론 해시 + RegisteredIds[hash % count]).

#### 6.6.2 `AbnormalityRegistry` 확장 사양

```csharp
// src/Abnormality/Template/AbnormalityRegistry.cs (확장)

/// <summary>
/// 진입 중인 맵 노드 좌표에 해당하는 환상체 ID 결정.
///
/// **v1 단순화**: 환상체 1개 (EbonyQueenApple) 가정 → 등록된 첫 ID 반환. 미등록 시 null.
/// **v2 확장 시**: 결정론 해시 (<c>AbnormalityMapInjector</c> 동일 공식) 로 RegisteredIds[hash % count] 분기.
/// </summary>
public static string? GetAbnormalityIdForCoord(IRunState runState, MapCoord coord)
{
    // v1: 환상체 1개 가정. 다중 환상체는 v2.
    if (EventByAbnormalityId.Count == 0) return null;
    return EventByAbnormalityId.Keys.First();
    // v2: return RegisteredIds[DeterministicHash(runState.Rng.Seed, runState.CurrentActIndex, coord) % count];
}
```

**불변성**: `EventByAbnormalityId` private 유지 (외부 누설 없음). v2 도입 시 `MapCoord` / `IRunState` 의존성을 Registry 안으로 캡슐화.

#### 6.6.3 `EbonyQueenAppleEvent` (M-B 산물, 라우팅이 가리킬 EventModel)

§5.4.1 의 `EbonyQueenAppleEvent.cs` 신규 작성과 함께 진행. M-B + M-C 동일 PR. `EbonyQueenAppleEvent` 미존재 시 라우터 hook 은 무동작 (Registry 가 비어 있어 lookup miss → 원본 currentEvent 반환).

#### 6.6.4 `ModStart.ModInit` 추가 (위치 + 순서)

```csharp
// src/ModStart.cs ModInit 안에 추가 — harmony.PatchAll() 이후 LocTableInjector 부근
AbnormalityRegistry.Register<EbonyQueenAppleEvent>(EbonyQueenAppleEncounter.AbnormalityKey);
```

`AbnormalityEventRouter` 자체는 `[HarmonyPatch]` 어트리뷰트 → `harmony.PatchAll()` 호출 시 자동 등록. 별도 `Init()` 호출 불필요.

#### 6.6.5 `AbnormalityPreflight` 확장

기존 §3 의 메서드 검증에 추가:
```csharp
CheckMethod(reasons, typeof(Hook), nameof(Hook.ModifyNextEvent));
```

게임 업데이트로 hook 메서드 시그니처 변경 시 preflight 단계에서 감지 → injector + router 비활성화 (기존 패턴).

#### 6.6.6 빌드 시 충돌 가능성 (mod-code-analyst Q3 결과)

| 충돌 후보 | 평가 | 결론 |
|---|---|---|
| 본 모드 기존 `Hook.ModifyGeneratedMap` Postfix (`MapPointTypePatches.cs:192`) | 다른 hook static 메서드 | **무충돌** |
| 본 모드 기존 `Hook.AfterCardPlayed` 구독 (`CardPlayPatch.cs:17`) | 다른 hook | **무충돌** |
| `AbnormalityMapInjector.Inject` 의 결정론 해시 공식 | v1 사용 안 함 (v2 도입 시 헬퍼 추출 필요) | **v1 무충돌** |
| `AbnormalityRegistry` 의 기존 `GetEventForAbnormality(string)` | 신규 `GetAbnormalityIdForCoord` 와 책임 분리 | **무충돌** |
| ModInit 순서 | `harmony.PatchAll()` 이후 `LocTableInjector.InjectForCurrentLanguage()` 부근 1줄 추가 | **순서 위반 위험 0** |

#### 6.6.7 검증 표 (L1~L6)

| Level | 목적 | 명령 | 통과 기준 |
|---|---|---|---|
| L1 | 게임 부팅 + 모드 로드 + 예외 0 | `launch_game` → `bridge_get_game_log` → `bridge_get_exceptions` | "TheCity initialized" 로그 + Harmony 예외 0 + Preflight `Healthy=true` (`Hook.ModifyNextEvent` 메서드 검증 추가됨) |
| L2 | 새 런 시작 → 환상체 노드 존재 | `bridge_start_run` → `bridge_get_map_state` | Abnormality(200) 노드 ≥1개 (기본 chance=20%) |
| L3 | (선택) 맵 화면 시각 | `bridge_capture_screenshot` | 별 아이콘 정상 |
| L4 | **★ 환상체 노드 진입 → EbonyQueenAppleEvent 발생** | `bridge_navigate_map → 환상체 노드 → bridge_get_screen` | 발생 EventModel 의 `Id.Entry == "EBONY_QUEEN_APPLE_EVENT"` (또는 BaseLib prefix `THECITY-EBONY_QUEEN_APPLE_EVENT`). 일반 이벤트 누수 검증: 환상체 노드 N=5 회 진입 시 모두 환상체 이벤트. **부수 검증 (mod-code-analyst Q7 잔여)**: BaseLib `CustomEncounterModel.ShouldGiveRewards` default 가 `true` 인지 (결재 (c) C1-v1 정합 — 표준 보상 표시되는지) + `CustomBgm` default 가 `""` 인지 (결재 (b) B1 정합 — 부모 act BGM 재생되는지). default 가 false/null 이면 명시 override 추가 필요. |
| L5 | (선택) 호버 텍스트 | 호버 → `bridge_get_full_state` | L11 영향 가능성 — 텍스트 표시 못 해도 라우팅 자체는 통과 |
| L6 | 저장·복원 왕복 | `bridge_save_snapshot` → 재시작 → `bridge_restore_snapshot` → 환상체 노드 진입 | 라우팅 결과 일치 (lazy hydrate 결정론 / v1 = 1개라 자명) |

L4 가 본 작업의 핵심 통과 기준. L1~L3 + L6 는 회귀 검증 (선행 맵 노드 시스템 영향 없음 확인).

#### 6.6.8 v2 이관 항목

- 다중 환상체 도입 시 `GetAbnormalityIdForCoord` 결정론 해시 활성화 + `AbnormalityMapInjector` 의 hash 공식 추출. mod-code-analyst Q2-a 결과: 현재 해시 공식은 `AbnormalityMapInjector.Inject` 본문 안 인라인 + `unchecked` 블록. 추출 권장 시그니처: `internal static ulong ComputeMix(ulong seed, int actIndex, int col, int row)`. 같은 mix 를 `mix % 100` (확률 판정) 와 `mix % registeredIds.Count` (인덱스 선택) 두 번 modulo 하면 통계적 무시 가능하나 v2 환상체 N개 + spawnChance 가 100 의 약수면 패턴 노출 가능성 — 64-bit 분산 충분.
- `Hook.AfterMapGenerated` 에서 ActMap 순회로 lazy hydrate 미스 방지 — v1 에서 단일 환상체라 lookup miss 자체가 없음, v2 RngSet 변경 시점 등 가드.
- 멀티플레이 peer 결정론 검증 (R3) — host-only / client-only / 양쪽 호출 여부 sts-game-analyst 추가 의뢰.
- **L11 별칭 등록 시 CRITICAL 가드 (mod-code-analyst Q4-c)**: `LocTableInjector` 가 `tables["encounters"] = ...` 별칭 등록 시 게임 기존 `encounters` 테이블 덮어쓰기 = 게임 모든 인카운터 텍스트 손상. 적용 시 **`if (!tables.ContainsKey("encounters")) { ... }` 가드 필수**. 권장 우회: 별도 `assets/localization/{lang}/encounters.json` 파일 (게임 측 모드 디렉토리 머지 메커니즘) 가 가드 회피 + 키 충돌 없음.
- **CLAUDE.md "Namespace rules" 갱신 (mod-code-analyst Q6-a)**: 현재 CLAUDE.md 는 `TheCity.Abnormality.*` 미명시. 1줄 추가 권장: `- `TheCity.Abnormality` — 환상체 시스템 (Template / Battles / MapIntegration)`. 본 작업의 산출물 PR 에 포함 가능.

---

## 5. 구현 가이드

### 5.1 마일스톤 (Plan §7 + 본 세션 진행 흡수)

| 단계 | 작업 | 검증 방법 | 상태 |
|---|---|---|---|
| 1 | `Template/` 부모 클래스 4개 (Event/Encounter/Monster/Registry) | 컴파일 통과 | ✅ M-A 완료 |
| 2 | EbonyQueenApple 단부위 PoC (`Head` 만) | 빌드 + DLL 복사 | ✅ M-A 완료 |
| 3 | 배경/BGM 적용 (`HasCustomBackground` / `CustomBgm`) | 전투 화면 배경/음악 | **(b) B1 v2 이관 / (d-1) Bg-USE 채택 (2026-04-29) — `EbonyQueenAppleEncounter.CustomEncounterBackground` override 신규 추가, `background.png` 사용** |
| 4 | 다부위 (`LeftArm`/`RightArm`/`Root`) + AI Move 풀 | 4부위 동시 등장 | v2 이관 |
| 5 | `EbonyQueenAppleEvent` + 콘솔 `event` 호출 흐름 | 선택지 → 전투 → 결과 → 맵 복귀 | **M-B 진입 결재 대기** |
| 6 | `MapIntegration/AbnormalityEventRouter` (Harmony Postfix on `Hook.ModifyNextEvent`) | 맵 노드 클릭 → 환상체 이벤트 진입 | **M-C 분석 완료 (§6.6)**, 구현 대기 |
| 7 | 로컬라이제이션 키 정리 (`encounters.json` / `monsters.json` / `events.json`) | 한국어/영어 텍스트 정상 | **L11 결과 + M-B 와 함께 결정** |

### 5.2 M-A 완료 결과 (2026-04-27)

#### 작성 파일 (6개, 213 줄)
| 경로 | 줄수 | 역할 |
|---|---|---|
| `src/Abnormality/Template/AbnormalityRegistry.cs` | 37 | Type ↔ ID 매핑 |
| `src/Abnormality/Template/AbnormalityEvent.cs` | 24 | `: CustomEventModel`, `IsShared => true` |
| `src/Abnormality/Template/AbnormalityEncounter.cs` | 31 | `: CustomEncounterModel`, `RoomType.Monster`, `IsValidForAct => true` |
| `src/Abnormality/Template/AbnormalityMonster.cs` | 40 | `: CustomMonsterModel`, MinionPower 패턴 |
| `src/Abnormality/Battles/EbonyQueenApple/EbonyQueenAppleEncounter.cs` | 28 | `AbnormalityKey="EbonyQueenApple"`, Head 1마리 |
| `src/Abnormality/Battles/EbonyQueenApple/EbonyQueenAppleHead.cs` | 53 | HP 60-70, idle MoveState, `CreateCustomVisuals` PNG |

#### 자산 리네임
- `assets/sprites/Abnormalities/Ebony Queen's Apple/` → `assets/sprites/abnormalities/ebony_queen_apple/`
- `Background.png` → `background.png` (lowercase + 공백 제거)
- `Boss.png` → `boss.png`
- `.import` 메타 파일도 함께 이동, `source_file`/`path` 갱신.

#### 빌드
- `dotnet build -c Release` ✅ Success (1.66s, 경고 1개 = 기존 ModAnalyzer 컴파일러 mismatch).
- DLL 자동 복사 (`mods/TheCity/TheCity.dll`).

#### 임시 처리 / TODO
- `AbnormalityEncounter.IsVictory` 세팅 시점 — M-B 검증 시 결정 (본체 사망 hook + 플레이어 승리 판정).
- `EbonyQueenAppleHead.GenerateMoveStateMachine` — 단순 idle 무한 루프 (인텐트 0). 본격 AI 패턴은 v2.
- `AbnormalityRegistry.Register<EbonyQueenAppleEvent>` — `ModStart.ModInit` 등록은 M-B 산물 (현재 미추가).

### 5.3 M-B 진입 자격 (Gate)

진입 전 체크리스트:
- [ ] **결재 (a)/(b)/(d-1)/(d-2) 사용자 confirm** — M-A 빌드는 권장안 채택 가정. (c) 는 2026-04-25 채택 완료.
- [ ] **L11 (LocString 라이프사이클) sts-game-analyst 의뢰** — `LocManager.GetText` / `LocString.Resolve` 본체 + `EncounterModel.L10NLookup` `"encounters"` 테이블 vs `EventModel.L10NLookup` 테이블명 + fallback 메커니즘.
- [ ] **L5 (BaseLib `ICustomModel` prefix EventModel 적용 여부) 런타임 1회 검증** — Custom*Model 트리오 채택 시 자동 해결 가설 확인. 미해결 시 수동 prefix 강제.
- [ ] **L9 (단일 sprite 보스 animator 호환성) 런타임 1회 검증** — 콘솔 `fight THECITY-EBONY_QUEEN_APPLE_ENCOUNTER` 로 Head 가 정지 이미지로 정상 등장하는지.

L5 + L9 는 콘솔 `fight` 한 번이면 두 항목 동시 검증 가능 (runtime-qa 1회 의뢰).

### 5.4 M-B 작업 범위

#### 5.4.1 작성할 파일

**`src/Abnormality/Battles/EbonyQueenApple/EbonyQueenAppleEvent.cs` (신규)**
```csharp
namespace TheCity.Abnormality.Battles.EbonyQueenApple;

public sealed class EbonyQueenAppleEvent : AbnormalityEvent
{
    public override string AbnormalityId => EbonyQueenAppleEncounter.AbnormalityKey;
    protected override Type EncounterType => typeof(EbonyQueenAppleEncounter);

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
        => new[] {
            new EventOption(this, OnApproach, $"{AbnormalityId}.options.APPROACH"),
        };

    private Task OnApproach()
    {
        StartAbnormalityCombat();   // 부모 헬퍼 — ModelDb.GetId + GetByIdOrNull + EnterCombatWithoutExitingEvent
        return Task.CompletedTask;
    }

    protected override async Task OnVictory(CombatRoom room)
    {
        SetEventFinished(L10NLookup($"{AbnormalityId}.victory"));
        // EGO 카드 보상은 별도 plan (v2). v1 = 표준 보상 (Gold + 카드 3장).
    }
}
```

**`src/ModStart.cs` 편집**
```csharp
// ModInit 안에 추가 (Harmony.PatchAll 후)
AbnormalityRegistry.Register<EbonyQueenAppleEvent>(EbonyQueenAppleEncounter.AbnormalityKey);
```

#### 5.4.2 `IsVictory` 세팅 시점 결정

후보:
- **(α) `CombatManager.IsEnding` Postfix** — IsEnding 평가 시점에 `Enemies.Any(e => e.IsAlive && e.IsPrimaryEnemy) == false` 면 본체 사망 = 플레이어 승리 → `encounter.IsVictory = true`.
- **(β) `Hook.AfterCombatFinished`** (존재 시) — 전투 종료 직후 호출. 더 명확한 시점.
- **(γ) `MonsterModel.AfterDeath`** override (Head 클래스) — 본체 사망 시 자기 encounter 의 `IsVictory = true`.

→ M-B 단계에서 sts-game-analyst 또는 mod-implementer 가 후보 검증 후 결정. 가장 단순한 (γ) 권장 (본체만 결정자, 부위 무관).

#### 5.4.3 로컬라이제이션 키 (L11 결과 반영)

L11 결과 따라:
- (a) **`LocTableInjector` 별칭 등록**: `tables["encounters"]` / `tables["events"]` 도 같은 dict 가리키게 → `EncounterModel.L10NLookup` 의 `"encounters"` 하드코딩이 자연 해소.
- (b) **모드측 `LocString("thecity", key)` 명시**: 부모 `AbnormalityEvent` / `AbnormalityEncounter` 의 모든 L10N 호출을 모드 자체 헬퍼로 래핑.

키 형식 (BaseLib prefix 포함):
- 환상체전 키: `EBONY_QUEEN_APPLE.options.APPROACH` / `.victory` / `.defeat`
- BaseLib `THECITY-` prefix 와 결합 시 실제 lookup 키는 `THECITY-EBONY_QUEEN_APPLE.options.APPROACH` 가 될 가능성 — L11 검증 시 함께 확인.

#### 5.4.4 콘솔 흐름 검증

```
콘솔 → event THECITY-EBONY_QUEEN_APPLE
  → EbonyQueenAppleEvent 진입 (선택지 1개)
  → "APPROACH" 선택 → StartAbnormalityCombat
  → EbonyQueenAppleEncounter 전투 진입 (Head 1마리, 정지 이미지)
  → 본체 사망 → IsVictory=true (5.4.2 결과 따라)
  → Resume → OnVictory → SetEventFinished + 표준 보상 (Gold + 카드 3장)
  → 맵 복귀
```

각 단계에서 텍스트 빈 표시 / CTD / 보상 미표시 등 발생 시 L11 / L5 / L9 결과 재확인.

### 5.5 M-C 작업 범위

§6 라우팅 그대로:
1. `src/Abnormality/MapIntegration/AbnormalityEventRouter.cs` 신규 — Hook 2개 구독.
2. `src/Map/AbnormalityMapInjector.cs` 확장 — 결정론 해시로 환상체 ID 저장 (`AbnormalityRegistry._nodeAssignments` Dictionary).
3. 게임 진입 → 환상체 맵 노드 클릭 → 환상체 이벤트 → 전투 → 보스 보임 → 맵 복귀 흐름 검증.

R2 (세이브/로드 다부위 partial load) / R4 (첫 방 진입 `CurrentMapCoord` null window) 는 M-C 검증 항목.

### 5.6 v2 이상 (별도 plan 또는 후속 마일스톤)

- **EGO 카드 시스템** (`doc/plan/ego-card-system.md`, 미작성): 환상체 승리 시 보상으로 받는 특수 카드. (c) C1-v2 활성화.
- **다부위** (`LeftArm`/`RightArm`/`Root`): 각자 PNG 자산 + `IsSecondaryPart => true` + MinionPower 자동.
- **페이즈 전환**: 본체 HP% 도달 시 특수 이벤트. `Hook.ShouldStopCombatFromEnding` 또는 `Hook.AfterDamageGiven` 활용.
- **배경/BGM 자산**: Bg-tscn (자산 정리 + override) + B2 (FMOD 이벤트 경로 + .ogg 자산).
- **환상체 관찰 로그 UI**: 림버스의 환상체 정보 화면.
- **Mirror Dungeon 통합**: 환상체전을 거울 던전 풀에.
- **멀티플레이어 검증**: 다부위 sync + ExtraRewards player 인자 동작 + Hook.ModifyNextEvent 결정론성 (R3).

### 5.7 바닐라 레퍼런스 클래스

| 패턴 | 클래스 |
|---|---|
| Event → Combat → Resume | `MegaCrit.Sts2.Core.Models.Events.BattlewornDummy` + `BattlewornDummyEventEncounter` |
| 다부위 (자기 자신에 MinionPower) | `TorchHeadAmalgam.cs:43` |
| 다부위 (런타임 추가) | `DoormakerBoss` + `Door` + `Doormaker` |
| 단순 상태머신 (FollowUpState 체인) | `CubexConstruct` |
| 소환 미니언 (Fabricator + Guardbot) | `FabricatorNormal` encounter |

---

## 7. 갱신 이력

본 통합 문서의 결정 흐름. turn-by-turn 정밀 이력은 `_archive/abnormality-battle/` 의 plan v1.1 / verification / m1.md / 본 세션 메시지 로그 참조.

| 일자 | 마일스톤 | 산출물 |
|---|---|---|
| 2026-04-22 | Plan v1.1 작성 | `plan.md` 본문 + 검증 4팀 의뢰 |
| 2026-04-22 | Verification 리포트 | `verification.md` — plan 가정 8건 라벨링 (치명 3 / 중대 5) + Option A 라우팅 확정 + M1 선결 항목 7개 |
| 2026-04-22 | Plan v1.1 ⚠ 차분 흡수 | `plan.md` 에 ⚠ blockquote 152줄 추가 (verification 결론 본문 위에 표시) |
| 2026-04-25 | M1 정적 조사 8건 (M1-1~M1-8) | sts-game-analyst (M1-2/3/4/5/6/7/8) + web-researcher (M1-1) — Slugify / 배경 파이프라인 / Hook 시그니처 / MinionPower API / 보상 풀 결합 매트릭스 / prefix 충돌 0 / BaseLib `Custom*Model` 트리오 발견 |
| 2026-04-25 | m1.md 작성 | M1 정적 결과 통합 + 결재 4건 + Plan 차분 정정 가이드 + 한계 7건 + mod-implementer 가이드 |
| 2026-04-25 | 보강 #11 (web-researcher) | BaseLib `CustomEncounterModel` 내부 Harmony Prefix 3개 본체 + `Utils/CustomBackgroundAssets.cs` 진입점 + ModTemplate 컨벤션 → m1.md 1차/2차 정정 (D1-A/D1-B 분리 → 통합) |
| 2026-04-25 | 결재 (c) C1-v1 채택 | 사용자 결재: `ShouldGiveRewards=true` + `extraRewards: Array.Empty<Reward>()`. v1 = 표준 보상만. EGO 카드는 v2 별도 plan |
| 2026-04-25 | 보강 #12 (web-researcher) | 단일 PNG 보스 시각화 가능 (`NodeFactory<NCreatureVisuals>.CreateFromResource`), 단일 PNG 배경 헬퍼 부재. `MonsterModel` 자동 폴백 부재 → Mon-NONE 위험 명시. (d) 분리 → (d-1) 배경 / (d-2) 보스 |
| 2026-04-27 | M-A 빌드 | mod-implementer 구현: Template 부모 4 + EbonyQueenApple 2 + 자산 리네임 + `dotnet build` ✅ Success. 코드 213줄 |
| 2026-04-27 | m1.md 정정 #16 (mod-implementer) | M-A 발견 6건 반영 — `ModelDb.GetByType` 부재 → `GetId+GetByIdOrNull` / `MonsterModel` 추상 멤버 명시 / `AbstractModel.Id` 충돌 → `AbnormalityKey` / `res://TheCity/` → `res://assets/` / idle MoveState stub / L11 (LocString 라이프사이클) 신설 |
| 2026-04-27 | 옵션 4 통합 (본 문서) | plan + verification + m1 → `abnormality-battle.md` 단일 통합. ⚠ blockquote 차분 형식 폐지. archive 정책 (가) — 3 파일 `_archive/abnormality-battle/` 로 이동 |
| 2026-04-29 | M-C 분석가 정정 (S1a/S1b) | sts-game-analyst Q1~Q3 + mod-code-analyst Q1~Q6 — Hook.ModifyNextEvent 진입은 `Hook.X += handler` 가 아닌 **Harmony Postfix on static method** 가 유일 깨끗한 경로 (`AbstractModel.ModifyNextEvent` virtual 시그니처에 IRunState 부재) / `Hook.AfterMapGenerated` hydrate 불필요 / v1 = 환상체 1개라 `_nodeAssignments` Dictionary + 결정론 해시 둘 다 v1 미사용 → §6.6 (M-C 구현 직전 사양) 신설. §6.1~§6.4 는 ⚠ 정정 marker 부착 후 역사적 보존 |

### 7.1 결정의 진화 (핵심 인사이트)

- **부모 클래스 상속**: 게임 측 `EncounterModel` 직접 상속 (Plan v1.1) → BaseLib `CustomEncounterModel` 트리오 (보강 #11). BaseLib `ICustomModel` 자동 prefix `THECITY-` 가 클래스명 prefix 강제를 대체.
- **승패 판정**: `combatRoom.CombatState.AreAllPlayersDead` (Plan v1.1, 게임에 없음) → encounter 커스텀 플래그 `IsVictory` + `SaveCustomState`/`LoadCustomState` (verification §1.2 → m1.md §2.3).
- **배경 fallback**: "fallback 토큰 합성 미확정 → CTD 위험" (m1.md 1차) → "BaseLib `HasCustomBackground` 자동 폴백 → CTD 없음" (보강 #11 2차 정정). D1-A/D1-B 분리 → Bg-NONE 단일.
- **단일 PNG 보스**: BaseLib 공식 헬퍼 `NodeFactory<NCreatureVisuals>.CreateFromResource` 발견 (보강 #12) → v1 부터 `Boss.png` 직접 참조 가능. (d) 단일 → (d-1)/(d-2) 분리.
- **보상 UI**: BattlewornDummy 패턴 (`OfferCustom`) 모방 (Plan v1.1) → C1-v1 (표준 보상만, v2 마이그레이션 비용 0). 함정 매트릭스 명시.
- **라우팅**: `RunManager.CreateRoom` Prefix (Plan v1.1) → `Hook.ModifyNextEvent` + `Hook.AfterMapGenerated` 단일 경로 (verification §5.4 검증 후) → **`Hook.ModifyNextEvent` 단독 Harmony Postfix + Dictionary 없음** (2026-04-29 분석가 정정, §6.6). hydrate 와 결정론 해시 둘 다 v2 다중 환상체 도입 시점으로 이관.

### 7.2 정정된 가정 (작업자 인지 필수)

| 가정 | 상태 | 정정 위치 |
|---|---|---|
| `combatRoom.CombatState.AreAllPlayersDead` | 부재 (전체 0건) | §2.1, §1.5 |
| `ModelDb.GetByType(Type)` | 부재 | §2.10 (`ModelDb.GetId + GetByIdOrNull`) |
| `CombatCmd.ApplyPower(...)` | 부재 | §2.6 (`PowerCmd.Apply<T>`) |
| `MonsterModel.IsCore` | 게임에 없음 | §1.5 (`IsSecondaryPart` + MinionPower 패턴) |
| `res://images/backgrounds/encounters/{slug}/bg.png` | 잘못된 경로 | §2.4 (`res://scenes/backgrounds/{slug}/layers/_bg_<group>_*.tscn`) |
| `Slugify` 결과가 lowercase | 실은 SCREAMING_SNAKE_CASE | §2.3 (4건 게임 본체 검증) |
| BaseLib audio API "없음" | 정정 — `Utils/FmodAudio` 30+ 메서드 | §2.8 |

### 7.3 후속 작업

- **M-B + M-C 통합 구현** (사용자 결재 후) — `EbonyQueenAppleEvent` 작성 + `AbnormalityEventRouter` Postfix + `AbnormalityRegistry.Register` 호출 추가 + `AbnormalityRegistry.GetAbnormalityIdForCoord` 신규 메서드. §5.4 (M-B) + §6.6 (M-C) 결합.
- **L5 + L9 런타임 1회 검증** (콘솔 `fight` 1회로 두 항목 동시).
- **L4 환상체 노드 진입 검증** (§6.6.7) — 환상체 노드 N=5 회 진입 시 모두 환상체 이벤트, 일반 이벤트 누수 0건.
- **L11 의뢰 / 추가** (sts-game-analyst Q4 별도 task, 라우팅 자체와 독립) — `EncounterModel.L10NLookup` 의 `"encounters"` 테이블 하드코딩 / `EventModel.L10NLookup` 테이블명 / `LocManager` fallback 동작.
- **v2 별도 plan** 들 — `ego-card-system.md`, 다부위 확장, 페이즈 전환 등.

---

## 부록 A. 출처 정리

### 게임 코드 (sts-game-analyst, MCP 활용)
- `MegaCrit.Sts2.Core.Models/EncounterModel.cs:30-243` — RoomType / Slots / HasCustomBackground / GetBackgroundAssets / CreateBackgroundAssetsForCustom / Resume
- `MegaCrit.Sts2.Core.Models/EventModel.cs:458-491` — EnterCombatWithoutExitingEvent / IsShared / Resume
- `MegaCrit.Sts2.Core.Models/AbstractModel.cs:48, 834-840, 974` — 생성자 (DuplicateModelException) / TryModifyRewards / ShouldStopCombatFromEnding (무인자 virtual)
- `MegaCrit.Sts2.Core.Helpers/StringHelper.cs` — Slugify 알고리즘
- `MegaCrit.Sts2.Core.Rooms/BackgroundAssets.cs:26-65` — DirAccess.Open + `_bg_`/`_fg_` 파싱
- `MegaCrit.Sts2.Core.Nodes.Rooms/NCombatBackground.cs:31-65` — Create + AddLayer (zero-pad 2자리)
- `MegaCrit.Sts2.Core.Combat/CombatManager.cs:107-127` — IsEnding 분기
- `MegaCrit.Sts2.Core.Commands/PowerCmd.cs:18` — Apply<T>(target, amount, applier, cardSource)
- `MegaCrit.Sts2.Core.Commands/RewardsCmd.cs:11-25` — OfferForRoomEnd 분기
- `MegaCrit.Sts2.Core.Rewards/RewardsSet.cs:64-67, 79, 127-167` — WithRewardsFromRoom / EmptyForRoom / GenerateRewardsFor
- `MegaCrit.Sts2.Core.Models.Powers/MinionPower.cs:7-19` — OwnerIsSecondaryEnemy
- `MegaCrit.Sts2.Core.Models.Monsters/TorchHeadAmalgam.cs:43` — MinionPower 적용 패턴
- `MegaCrit.Sts2.Core.Models.Events/BattlewornDummy.cs:46, 71-103` — Resume 패턴
- `MegaCrit.Sts2.Core.Models.Encounters/BattlewornDummyEventEncounter.cs:28` — ShouldGiveRewards = false
- 외 75개 EventModel/AncientEventModel 슬러그 전수 (§2.9)

### BaseLib (web-researcher, GitHub `Alchyr/BaseLib-StS2`)
- https://github.com/Alchyr/BaseLib-StS2/blob/master/Abstracts/CustomEncounterModel.cs (Harmony Prefix 3개 본체)
- https://github.com/Alchyr/BaseLib-StS2/blob/master/Abstracts/CustomEventModel.cs
- https://github.com/Alchyr/BaseLib-StS2/blob/master/Abstracts/CustomMonsterModel.cs (CreateCustomVisuals + Harmony 패치 6개)
- https://github.com/Alchyr/BaseLib-StS2/blob/master/Patches/Content/PrefixIdPatch.cs
- https://github.com/Alchyr/BaseLib-StS2/blob/master/Extensions/TypePrefix.cs
- https://github.com/Alchyr/BaseLib-StS2/blob/master/Utils/FmodAudio.cs
- https://github.com/Alchyr/BaseLib-StS2/blob/master/Utils/CustomBackgroundAssets.cs
- https://github.com/Alchyr/BaseLib-StS2/blob/master/Utils/NodeFactories/NCreatureVisualsFactory.cs (CreateBareFromResource 본체)
- https://github.com/Alchyr/BaseLib-StS2/blob/master/Utils/NodeFactories/NodeFactory.cs
- https://github.com/Alchyr/ModTemplate-StS2 (모드 res:// 컨벤션 참고)
- https://alchyr.github.io/BaseLib-Wiki/docs/models/custom-encounter.html
- https://alchyr.github.io/BaseLib-Wiki/docs/scenes/creature-visuals.html (단일 PNG 명시)

### 모드 측 확인
- `TheCity.csproj:36` — `<PackageReference Include="Alchyr.Sts2.BaseLib" Version="*" />`
- `src/ModStart.cs:8` — `namespace TheCity;`
- `export_presets.cfg` — `res_prefix` 키 없음 (루트 마운트)
