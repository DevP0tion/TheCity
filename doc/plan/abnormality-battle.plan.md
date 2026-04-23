# Plan: abnormality-battle

**Feature:** abnormality-battle
**상태:** Plan v1.1 (2026-04-22 verification 반영, Do 미시작)
**작성:** 2026-04-22
**관련 문서:** `abnormality-map-node.md` (맵 노드 시스템 — 선행 완료), `abnormality-battle.verification.md` (2026-04-22 plan 검증 리포트)

> ⚠ **2026-04-22 verification 반영**: plan 의 게임 API 가정 중 치명/중대 수정 사항이 아래 섹션에 ⚠ 블록으로 표시됨. 본문 원안은 맥락 유지를 위해 남겨두되, 실제 구현은 ⚠ 블록의 교정된 내용을 따를 것. 상세 근거는 `abnormality-battle.verification.md` 참조.

## Executive Summary

| 관점 | 요약 |
|------|------|
| **Problem** | 환상체 맵 노드는 구현됐으나, 진입 시 발생할 전투 시스템이 없음. 림버스 컴퍼니의 환상체전 (이벤트 → 선택지 → 전투 → 결과) 흐름을 STS2 위에 구현해야 함. |
| **Solution** | `EventModel` → `EncounterModel` → `MonsterModel` 3계층 추상 부모 클래스(`Template/`)를 만들고, 각 환상체전(`Battles/{이름}/`)이 이를 상속하여 자기완결적으로 구현. 게임이 제공하는 `HasCustomBackground` / `CustomBgm` 오버라이드 포인트를 그대로 활용. |
| **Function UX Effect** | 환상체 맵 노드 클릭 → 환상체 전용 이벤트 화면 → 선택지 (예: "접근한다" / "관찰한다") → 전용 배경/BGM의 전투 → 승리 시 환상체 EGO 카드 등 보상. |
| **Core Value** | 림버스 컴퍼니의 핵심 콘텐츠인 환상체전을 STS2의 룸 시스템 위에 자연스럽게 얹기. 환상체 1개 추가 = 폴더 1개 추가의 확장성 확보. |

## Context Anchor

| 축 | 내용 |
|----|------|
| **WHY** | 환상체 맵 노드만 있고 진입 시 발생할 콘텐츠가 없음. 모드의 핵심 콘텐츠 축 완성. |
| **WHO** | TheCity 모드 사용자. 싱글플레이 우선, 멀티플레이는 추후 검증. |
| **RISK** | (1) 맵 노드 → 이벤트 라우팅 분기점 식별 실패 (2) 다부위 환상체의 STS2 표현 한계 (3) 배경/BGM 에셋 경로 규약 위반 시 로드 실패 |
| **SUCCESS** | 환상체 맵 노드 클릭 → 이벤트 화면 정상 진입 → 선택지 클릭 → 전용 배경/BGM 적용된 전투 → 승리 시 보상 → 맵으로 복귀. 새 환상체 추가가 폴더 1개 추가로 완결. |
| **SCOPE** | v1: 흑단여왕의 사과 1개 PoC. 다부위 (머리/팔/뿌리). **제외**: 림버스식 클래시 시스템, EGO 카드 시스템, 환상체 페이즈 전환 이벤트, 멀티플레이어 검증 |

## 1. 목표

### 1.1 무엇을 만드는가

**부모 클래스 계층** (`Template/`):
- `AbnormalityEvent` — `EventModel` 상속, 환상체전 공통 이벤트 골격
- `AbnormalityEncounter` — `EncounterModel` 상속, 배경/BGM 규약 강제
- `AbnormalityMonster` — `MonsterModel` 상속, 환상체 공통 속성
- `AbnormalityRegistry` — 환상체 ↔ 맵 노드 매핑

**라우팅 계층** (`MapIntegration/`):
- 맵 노드 진입 시 환상체 전용 `EventModel` 반환하도록 `RunManager` 분기 패치

**구현 계층** (`Battles/{이름}/`):
- 환상체별 자기완결 폴더 — Event/Encounter/Monsters/Moves

### 1.2 무엇을 만들지 않는가

- 림버스식 클래시 / 코인 시스템 (STS2 카드 vs 인텐트 그대로 사용)
- EGO 카드 시스템 (별도 plan으로 분리)
- 환상체 페이즈 전환 이벤트 (HP% 도달 시 스킬체크) — v2
- 멀티플레이어 환상체전 (싱글 동작 검증 후 별도 검증)
- 환상체 자체 콜렉션 / 관찰 로그 UI

## 2. 게임 코드 조사 결과

### 2.1 전투 진입 흐름 (게임 기본)

```
맵 노드 클릭
  → RunManager.CreateRoom(roomType, mapPointType, model)
    → roomType별 분기: EventRoom / CombatRoom / ShopRoom / ...
  → EnterRoom(room)
    → AbstractRoom.Enter(runState)
      → EventRoom: NEventRoom 생성 + EventModel.GenerateInitialOptions
      → CombatRoom: NCombatRoom 생성 + CombatManager.SetUpCombat
```

### 2.2 이벤트 → 전투 패턴 (`BattlewornDummy` 레퍼런스)

`MegaCrit.Sts2.Core.Models.Events.BattlewornDummy` 가 정확한 레퍼런스. 흐름:

```csharp
// 1. EventModel에서 선택지 정의
protected override IReadOnlyList<EventOption> GenerateInitialOptions() {
    return new[] {
        new EventOption(this, OnChoose1, "loc.key.option1"),
        new EventOption(this, OnChoose2, "loc.key.option2"),
    };
}

// 2. 선택지 콜백에서 전투 진입
private Task OnChoose1() {
    var encounter = (MyEncounter)ModelDb.Encounter<MyEncounter>().ToMutable();
    encounter.SomeSetting = ...;  // 선택지에 따른 전투 변형
    EnterCombatWithoutExitingEvent(encounter, Array.Empty<Reward>(), shouldResumeAfterCombat: true);
    return Task.CompletedTask;
}

// 3. 전투 종료 후 자동 호출
public override async Task Resume(AbstractRoom room) {
    var combatRoom = (CombatRoom)room;
    var encounter = (MyEncounter)combatRoom.Encounter;

    // 전투 결과(승/패, 커스텀 상태) 조회 → 보상 처리
    SetEventFinished(L10NLookup("loc.key.victory"));
    await RewardsCmd.OfferCustom(Owner, new List<Reward> { ... });
}
```

> ⚠ **2026-04-22 verification 반영 — §2.2 치명 수정 3건**
>
> 1. **`combatRoom.CombatState.AreAllPlayersDead` 삭제.** 이 API 는 게임 전역에 존재하지 않음 (search_game_code 0건). 실제 BattlewornDummy 패턴은 Encounter 에 **커스텀 플래그** 를 두고 Resume 에서 읽음:
>    ```csharp
>    // Encounter 에 플래그 선언
>    public bool RanOutOfTime { get; set; }   // 또는 IsDefeated / IsVictory 등 의미에 맞춰
>
>    public override Dictionary<string, string> SaveCustomState() => new() {
>        ["RanOutOfTime"] = RanOutOfTime.ToString()
>    };
>    public override void LoadCustomState(Dictionary<string, string> state) {
>        RanOutOfTime = bool.Parse(state["RanOutOfTime"]);
>    }
>
>    // Event.Resume 에서
>    var won = !encounter.RanOutOfTime;   // 의미에 맞는 플래그명 사용
>    ```
>    커스텀 플래그는 **반드시 SaveCustomState / LoadCustomState 로 세이브 왕복** 되어야 전투 중 세이브/로드 시 유실되지 않음.
> 2. **`EnterCombatWithoutExitingEvent` 전제조건**: `EventModel.IsShared == false` 이면 내부에서 `InvalidOperationException` throw. 환상체전 EventModel 은 **반드시 `public override bool IsShared => true;`** 오버라이드 필요.
> 3. **`SetEventFinished` 인자 타입**: `string` 이 아니라 `LocString`. 위 코드의 `SetEventFinished(L10NLookup("..."))` 는 `L10NLookup` 반환이 `LocString` 이므로 OK. 단 plan 원안에서 string 직접 전달하는 곳은 모두 `L10NLookup(...)` 로 감싸야 함.

### 2.3 EncounterModel 오버라이드 포인트 (게임 기본 지원)

```csharp
// 배경 이미지 — true면 res://images/backgrounds/encounters/{id_lowercase}/ 자동 로드
protected virtual bool HasCustomBackground => false;

// BGM — FMOD 이벤트 경로
public virtual string CustomBgm => "";

// 앰비언트 SFX
public virtual string AmbientSfx => "";

// 몬스터 구성
protected abstract IReadOnlyList<(MonsterModel, string?)> GenerateMonsters();
```

**중요**: 배경/BGM은 Harmony 패치 불필요. `EncounterModel` 오버라이드만으로 게임이 자동 처리.

> ⚠ **2026-04-22 verification 반영 — §2.3 접근수정자 교정 + 누락 멤버 열거**
>
> - `HasCustomBackground` 는 `protected virtual bool` (public 아님). 상기 주석 OK.
> - plan §2.3 에 누락된 중요 오버라이드 포인트 (실제 구현 시 필수):
>   - `RoomType` — abstract, 반드시 override.
>   - `AllPossibleMonsters` — abstract, 등장 가능한 몬스터 전체 집합.
>   - `Slots` — `public virtual IReadOnlyList<string>`. **임의 문자열** 허용 (enum 아님). 단 `HasScene => true` + `res://scenes/encounters/{id_snake_case}.tscn` 존재 시에만 배치 순서가 반영됨. scene 없으면 기본 레이아웃.
>   - `BossNodePath` — 맵 노드 Spine 데이터 경로 규약.
>   - `ExtraAssetPaths` — 커스텀 에셋 프리로드.
>   - `Tags`, `HasScene`, `FullyCenterPlayers`, `GetCameraScaling / GetCameraOffset` — 필요 시.

### 2.4 맵 노드 → 이벤트 분기 위치

`ActModel.PullNextEvent` 가 호출하는 공식 훅 `Hook.ModifyNextEvent(IRunState, EventModel)` 에서 환상체 EventModel 로 교체. `MapPointType.Abnormality → RoomType.Event` 매핑은 이미 `src/Map/MapPointTypePatches.cs` (`RollRoomTypeFor` Prefix) 가 처리. 상세 설계는 §6 라우팅 참조.

## 3. 폴더 구조

```
src/Abnormality/
│
├── Template/                          # 부모 클래스 (모든 환상체전이 상속)
│   ├── AbnormalityEvent.cs            # abstract : EventModel
│   ├── AbnormalityEncounter.cs        # abstract : EncounterModel
│   ├── AbnormalityMonster.cs          # abstract : MonsterModel
│   ├── AbnormalityMove.cs             # abstract : 이동/공격 패턴 헬퍼
│   └── AbnormalityRegistry.cs         # 환상체 등록/조회
│
├── MapIntegration/                    # 맵 노드 ↔ 환상체전 연결
│   ├── AbnormalityEventRouter.cs      # Hook.AfterMapGenerated(hydrate) + Hook.ModifyNextEvent(라우팅)
│   └── AbnormalityNodeHover.cs        # 맵 노드 호버 시 환상체명 표시
│
└── Battles/                           # 환상체별 구현 (자기완결)
    │
    └── EbonyQueenApple/                # 흑단여왕의 사과 (PoC)
        ├── EbonyQueenAppleEvent.cs        # : AbnormalityEvent
        ├── EbonyQueenAppleEncounter.cs    # : AbnormalityEncounter
        ├── Monsters/
        │   ├── EbonyQueenApple_Head.cs    # : AbnormalityMonster (본체)
        │   ├── EbonyQueenApple_LeftArm.cs
        │   ├── EbonyQueenApple_RightArm.cs
        │   └── EbonyQueenApple_Root.cs
        └── Moves/
            ├── VainFruit.cs               # : AbnormalityMove
            ├── PaleStem.cs
            ├── Distrust.cs
            └── EntanglingRoots.cs
```

### 에셋 구조 (PCK 빌드 시 포함)

```
assets/
├── images/
│   ├── backgrounds/
│   │   └── encounters/
│   │       └── ebonyqueenappleencounter/   # EncounterModel.Id.Entry 소문자
│   │           ├── bg.png
│   │           └── bg.png.import
│   │
│   ├── monsters/
│   │   └── ebony_queen_apple/
│   │       ├── head.png
│   │       ├── left_arm.png
│   │       ├── right_arm.png
│   │       └── root.png
│   │
│   └── map/
│       └── abnormality/
│           └── ebony_queen_apple_node.png  # 맵 노드 아이콘
│
├── audio/
│   └── abnormality/
│       └── ebony_queen_apple_bgm.ogg
│
└── localization/
    ├── eng/
    │   ├── encounters.json    # 환상체 이름, 승/패 메시지
    │   ├── monsters.json      # 부위명, 인텐트 설명
    │   └── events.json        # 이벤트 선택지 텍스트
    └── kor/
        └── (동일 구조)
```

> ⚠ **2026-04-22 verification 반영 — §3 배경 경로 규약 전면 교체 + Slugify 규칙 명시**
>
> **배경 경로 (원안 오류):**
> - ~~`res://images/backgrounds/encounters/{id_lowercase}/bg.png`~~
> - **실제**: `res://scenes/backgrounds/{id_snake_case}/` 폴더 + `layers/` 서브디렉토리 구조. 단일 `bg.png` 아님.
>
> **실제 `BackgroundAssets` 로딩 체인:**
> 1. `DirAccess.Open("res://scenes/backgrounds/{id_snake_case}/layers")` — **없으면 `InvalidOperationException` throw**.
> 2. `layers/` 내부 파일명 규약:
>    - `_bg_{group}_*` 파일 — 배경 (그룹당 RNG 1개 선택)
>    - `_fg_*` 파일 — 전경 (최소 1개 필수)
>    - 같은 `_bg_group_` 접두사 파일들은 한 그룹으로 묶여 RNG 선택.
> 3. `{id_snake_case}_background.tscn` 메인 scene 파일 (Godot scene).
>
> **Slugify 규칙 (`ModelDb.GetEntry` → `StringHelper.Slugify`):**
> - CamelCase → **snake_case (밑줄 포함)**
> - 예: `EbonyQueenAppleEncounter` → `ebony_queen_apple_encounter`
> - 원안의 `ebonyqueenappleencounter` (밑줄 없음) 는 **전부 오류**. 경로/플레이스홀더 전체를 `ebony_queen_apple_encounter` 로 교체.
>
> **수정된 에셋 구조:**
> ```
> assets/
> ├── scenes/
> │   └── backgrounds/
> │       └── ebony_queen_apple_encounter/       # Slugify (snake_case)
> │           ├── ebony_queen_apple_encounter_background.tscn
> │           └── layers/
> │               ├── _bg_main_0.png             # 배경 그룹 "main"
> │               ├── _bg_main_1.png
> │               ├── _fg_tree.png               # 전경
> │               └── _fg_particles.png
> │
> ├── images/monsters/, images/map/, audio/, localization/ — 원안과 동일
> ```
>
> **클래스명 접두사 (ID 충돌 회피):** `ModelId.Entry` 에는 모드 접두사 **자동 주입 없음**. BaseLib `CustomEncounterModel` 존재 여부는 M1-1 에서 실행 검증. 충돌 회피는 **클래스명 자체로** 해결 권장 (예: `TheCityEbonyQueenAppleEncounter` → `the_city_ebony_queen_apple_encounter`).

## 4. 부모 클래스 설계

### 4.1 AbnormalityEvent

```csharp
public abstract class AbnormalityEvent : EventModel
{
    /// <summary>환상체 식별자 (예: "EbonyQueenApple")</summary>
    public abstract string AbnormalityId { get; }

    /// <summary>이 이벤트에서 시작할 인카운터 타입</summary>
    protected abstract Type EncounterType { get; }

    public override bool IsShared => true;

    /// <summary>전투 진입 헬퍼 — 하위 클래스의 선택지 콜백에서 호출</summary>
    protected void StartAbnormalityCombat(Action<EncounterModel>? configureEncounter = null)
    {
        var encounter = ModelDb.GetByType(EncounterType).ToMutable() as EncounterModel;
        configureEncounter?.Invoke(encounter);
        EnterCombatWithoutExitingEvent(encounter, Array.Empty<Reward>(), shouldResumeAfterCombat: true);
    }

    /// <summary>전투 종료 후 자동 호출 — 공통 처리 후 하위 훅 호출</summary>
    public override async Task Resume(AbstractRoom room)
    {
        var combatRoom = (CombatRoom)room;
        var won = !combatRoom.CombatState.AreAllPlayersDead;  // 정확한 API는 구현 시 확인

        if (won)
            await OnVictory(combatRoom);
        else
            await OnDefeat(combatRoom);
    }

    /// <summary>승리 시 처리 — 하위가 보상 정의</summary>
    protected abstract Task OnVictory(CombatRoom room);

    /// <summary>패배 시 처리 — 기본은 패배 메시지만 표시</summary>
    protected virtual Task OnDefeat(CombatRoom room)
    {
        SetEventFinished(L10NLookup($"{AbnormalityId}.defeat"));
        return Task.CompletedTask;
    }
}
```

> ⚠ **2026-04-22 verification 반영 — §4.1 승패 판정 재설계**
>
> 상기 `combatRoom.CombatState.AreAllPlayersDead` 는 **게임 전역에 존재하지 않는 API**. Resume 메서드는 BattlewornDummy 패턴대로 **Encounter 커스텀 플래그** 를 읽도록 변경:
>
> ```csharp
> public override async Task Resume(AbstractRoom room)
> {
>     var combatRoom = (CombatRoom)room;
>     var encounter = (AbnormalityEncounter)combatRoom.Encounter;
>
>     if (encounter.IsVictory)           // 커스텀 플래그 (Encounter 에 정의 + SaveCustomState 왕복 필수)
>         await OnVictory(combatRoom);
>     else
>         await OnDefeat(combatRoom);
> }
> ```
>
> `IsVictory` / `IsDefeated` 등 의미에 맞는 플래그를 `AbnormalityEncounter` 에 선언하고, 전투 내에서 승리 조건 충족 시점에 세팅 (예: `CombatManager.IsEnding` Postfix 또는 본체 사망 훅). 플래그는 **반드시 SaveCustomState / LoadCustomState 왕복** 필수. `AbnormalityEvent` 의 **`public override bool IsShared => true;`** 는 이미 명시돼 있으나 이유는 **`EnterCombatWithoutExitingEvent` 가 `IsShared==false` 시 `InvalidOperationException` throw** — 삭제 금지.

### 4.2 AbnormalityEncounter

```csharp
public abstract class AbnormalityEncounter : EncounterModel
{
    public override RoomType RoomType => RoomType.Monster;
    public override bool ShouldGiveRewards => false;  // 보상은 EventModel.OnVictory에서

    // 모든 환상체전은 커스텀 배경/BGM 사용 강제
    protected override bool HasCustomBackground => true;
    public override string CustomBgm => GetBgmEventPath();

    /// <summary>FMOD 이벤트 경로 — 하위가 정의</summary>
    protected abstract string GetBgmEventPath();

    /// <summary>환상체 식별자 — 에셋 경로 규약과 매칭</summary>
    public abstract string AbnormalityId { get; }
}
```

> ⚠ **2026-04-22 verification 반영 — §4.2 접근수정자 교정 + 오버라이드 포인트 추가**
>
> - `HasCustomBackground` 는 `protected virtual bool` — 상기 코드 `protected override bool HasCustomBackground => true;` OK.
> - 추가로 고려할 오버라이드 포인트 (필요 시):
>   - `public override bool HasScene => true;` + `res://scenes/encounters/{id_snake_case}.tscn` — 몬스터 배치 scene 필요 시.
>   - `public override IReadOnlyList<string> Slots => new[] { "center", "left", "right", "ground" };` — 임의 문자열 허용.
>   - `public override bool FullyCenterPlayers => false;`
>   - `public override string BossNodePath => "...";` — 보스 스파인 데이터.
>   - `public override IEnumerable<string> ExtraAssetPaths => new[] { ... };` — 커스텀 에셋 프리로드.
>   - `public override float GetCameraScaling() => 1.0f;` / `GetCameraOffset()` — 카메라 튜닝.
>   - `public override IReadOnlyList<string> Tags => new[] { "abnormality" };` — 분류/검색용.

### 4.3 AbnormalityMonster

```csharp
public abstract class AbnormalityMonster : MonsterModel
{
    /// <summary>이 부위가 본체인지 (false면 본체 사망 시 함께 처리될 수 있음)</summary>
    public virtual bool IsCore => false;

    /// <summary>환상체 식별자 (같은 환상체의 부위들끼리 그룹핑)</summary>
    public abstract string AbnormalityId { get; }

    // MonsterModel 추상 메서드는 하위가 구현:
    // - HP, 스프라이트 경로, AI 패턴 (Move 풀)
}
```

> ⚠ **2026-04-22 verification 반영 — §4.3 `IsCore` 폐기, `MinionPower` 패턴 채택**
>
> 상기 `public virtual bool IsCore => false;` 는 **게임 전역에 존재하지 않는 가정**. 실제 STS2 메커니즘:
>
> - `Creature.IsPrimaryEnemy = Side==Enemy && !IsSecondaryEnemy` (기본값: true)
> - `Creature.IsSecondaryEnemy = Powers.Any(p => p.OwnerIsSecondaryEnemy)`
> - **`MinionPower.OwnerIsSecondaryEnemy => true`** (게임 내 유일하게 이 속성을 override 하는 파워)
>
> **올바른 패턴:**
> - **"본체"** 는 아무 조치 불필요 — Primary 가 default.
> - **"부위"** 는 `AfterAddedToRoom()` 에서 자기 자신에게 `MinionPower` 적용:
>   ```csharp
>   public override Task AfterAddedToRoom()
>   {
>       PowerCmd.Apply<MinionPower>(base.Creature, 1m, base.Creature, null);
>       return base.AfterAddedToRoom();
>   }
>   ```
> - `CombatManager.IsEnding` 은 `Enemies.Any(e => e.IsAlive && e.IsPrimaryEnemy)` 로 전투 종료 판정 → **본체(Primary) 만 죽으면 자동 종료**, 부위(Secondary) 가 살아있어도 종료됨.
>
> **바닐라 레퍼런스:**
> - `TorchHeadAmalgam` — 자기 자신에게 MinionPower 적용하는 가장 단순한 패턴.
> - `DoormakerBoss` / `Door` / `Doormaker` — 런타임 추가 + MinionPower 패턴.
> - `EyeWithTeeth`, `Parafright` — 추가 레퍼런스.
>
> **`AbnormalityMonster` 재설계 권장:**
> ```csharp
> public abstract class AbnormalityMonster : MonsterModel
> {
>     public abstract string AbnormalityId { get; }
>     /// <summary>true면 부위. 본체는 false (기본). 부위는 AfterAddedToRoom 에서 MinionPower 적용.</summary>
>     protected virtual bool IsSecondaryPart => false;
>
>     public override async Task AfterAddedToRoom()
>     {
>         await base.AfterAddedToRoom();
>         if (IsSecondaryPart)
>             PowerCmd.Apply<MinionPower>(base.Creature, 1m, base.Creature, null);
>     }
> }
> ```

### 4.4 AbnormalityRegistry

```csharp
public static class AbnormalityRegistry
{
    private static readonly Dictionary<string, Type> _eventByAbnormalityId = new();

    /// <summary>환상체 등록 — ModInit에서 호출</summary>
    public static void Register<TEvent>(string abnormalityId) where TEvent : AbnormalityEvent
    {
        _eventByAbnormalityId[abnormalityId] = typeof(TEvent);
    }

    /// <summary>맵 노드의 환상체 ID로 EventModel 조회</summary>
    public static EventModel? GetEventForAbnormality(string abnormalityId)
    {
        if (!_eventByAbnormalityId.TryGetValue(abnormalityId, out var type)) return null;
        return ModelDb.GetByType(type) as EventModel;
    }
}
```

## 5. 환상체 구현 예시 (흑단여왕의 사과)

### 5.1 EbonyQueenAppleEvent

```csharp
public sealed class EbonyQueenAppleEvent : AbnormalityEvent
{
    public override string AbnormalityId => "EbonyQueenApple";
    protected override Type EncounterType => typeof(EbonyQueenAppleEncounter);

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return new[] {
            new EventOption(this, OnApproach, "EBONY_QUEEN_APPLE.options.APPROACH"),
            new EventOption(this, OnObserve, "EBONY_QUEEN_APPLE.options.OBSERVE"),
        };
    }

    private Task OnApproach()
    {
        StartAbnormalityCombat();
        return Task.CompletedTask;
    }

    private Task OnObserve()
    {
        // 선택지에 따라 전투 변형 가능 (예: 시작 시 디버프 부여)
        StartAbnormalityCombat(enc => {
            ((EbonyQueenAppleEncounter)enc).PlayerStartsWithDebuff = true;
        });
        return Task.CompletedTask;
    }

    protected override async Task OnVictory(CombatRoom room)
    {
        SetEventFinished(L10NLookup("EBONY_QUEEN_APPLE.victory"));
        // TODO: EGO 카드 보상 시스템 연동 (별도 plan)
    }
}
```

### 5.2 EbonyQueenAppleEncounter

```csharp
public sealed class EbonyQueenAppleEncounter : AbnormalityEncounter
{
    public override string AbnormalityId => "EbonyQueenApple";
    protected override string GetBgmEventPath() => "event:/BGM/Abnormality/EbonyQueen";

    public bool PlayerStartsWithDebuff { get; set; }

    public override IEnumerable<MonsterModel> AllPossibleMonsters => new MonsterModel[] {
        ModelDb.Monster<EbonyQueenApple_Head>(),
        ModelDb.Monster<EbonyQueenApple_LeftArm>(),
        ModelDb.Monster<EbonyQueenApple_RightArm>(),
        ModelDb.Monster<EbonyQueenApple_Root>(),
    };

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new[] {
            (ModelDb.Monster<EbonyQueenApple_Head>().ToMutable(), "center"),
            (ModelDb.Monster<EbonyQueenApple_LeftArm>().ToMutable(), "left"),
            (ModelDb.Monster<EbonyQueenApple_RightArm>().ToMutable(), "right"),
            (ModelDb.Monster<EbonyQueenApple_Root>().ToMutable(), "ground"),
        };
    }

    public override Dictionary<string, string> SaveCustomState() => new() {
        ["PlayerStartsWithDebuff"] = PlayerStartsWithDebuff.ToString()
    };

    public override void LoadCustomState(Dictionary<string, string> state)
    {
        PlayerStartsWithDebuff = bool.Parse(state["PlayerStartsWithDebuff"]);
    }
}
```

> ⚠ **2026-04-22 verification 반영 — §5.2 추가 요구사항**
>
> - **Slugify 규칙 적용**: `EbonyQueenAppleEncounter` → `ebony_queen_apple_encounter` (밑줄 포함). 배경 폴더는 `res://scenes/backgrounds/ebony_queen_apple_encounter/layers/`.
> - **`AllPossibleMonsters` 는 `abstract` + 반환 타입 확인 필요**: 현 코드는 `IEnumerable<MonsterModel>` 인데 실제 EncounterModel 계약 확인 후 매칭.
> - **`Slots` 문자열 규약**: `"center"/"left"/"right"/"ground"` 허용 (임의 문자열). 단 scene 파일 (`res://scenes/encounters/ebony_queen_apple_encounter.tscn`) 이 있을 때만 배치 순서 반영. scene 없으면 기본 레이아웃 — PoC 는 scene 없이도 동작.
> - **다부위 MinionPower 적용**: `EbonyQueenApple_LeftArm` / `_RightArm` / `_Root` 는 각자의 `AfterAddedToRoom()` 에서 **MinionPower 적용 필수** (본체 `_Head` 는 불필요). 위 §4.3 재설계 블록의 `IsSecondaryPart => true` 오버라이드 패턴 사용.
> - **승패 플래그**: `EbonyQueenAppleEncounter` 에 `IsVictory`/`IsDefeated` 같은 커스텀 플래그 추가 + `SaveCustomState/LoadCustomState` 왕복 필수 (plan §4.1 ⚠ 블록 참조).
> - **보상 `Array.Empty<Reward>()`**: 전투 기본 보상 UI 가 표시될지 여부는 M1-6 에서 실행 검증.

## 6. 맵 노드 라우팅

**Hook 단일 경로 확정.** 공식 `Hook.AfterMapGenerated` + `Hook.ModifyNextEvent` + public `IRunState` 인터페이스만으로 완결. 추가 Harmony 패치 불필요.

### 6.1 맵 노드 ↔ 환상체 ID 저장 (Option A)

`AbnormalityMapInjector.Inject()` (src/Map/AbnormalityMapInjector.cs:47-63 기존 결정론 해시 재사용) 확장해 **`Dictionary<(IRunState.Id, int actIndex, int col, int row), string> _nodeAssignments`** 구축. 같은 입력 → 같은 출력이므로 lookup miss 시 즉석 재계산 (lazy hydrate) 도 안전.

```csharp
// AbnormalityMapInjector.Inject() 확장
var abnormalityId = registeredIds[hash(runId, actIndex, col, row) % registeredIds.Count];
AbnormalityRegistry._nodeAssignments[(runId, actIndex, col, row)] = abnormalityId;
```

### 6.2 Hydrate — `Hook.AfterMapGenerated`

`Hook.AfterMapGenerated(IRunState runState, ActMap map, int actIndex)` 구독. **세이브 로드 / 신규 run 양쪽 모두에서 발화** (RunManager.cs:577 기준). act 당 1회 호출되므로 Dictionary 전체 재구성에 적합.

### 6.3 라우팅 — `Hook.ModifyNextEvent`

`Hook.ModifyNextEvent(IRunState runState, EventModel currentEvent)` 구독 (category=modify/general). `ActModel.PullNextEvent` (ActModel.cs:340) 내부에서 호출되며, `EnsureNextEventIsValid` 선행으로 `currentEvent` non-null 보장.

```csharp
// Hook 구독 두 줄 — 상세 API 는 BaseLib Hook 헬퍼 문서 참조
Hook.AfterMapGenerated += OnAfterMapGenerated;   // 6.2 hydrate
Hook.ModifyNextEvent   += OnModifyNextEvent;     // 6.3 라우팅

static EventModel OnModifyNextEvent(IRunState runState, EventModel currentEvent)
{
    if (runState.CurrentMapCoord is not { } coord) return currentEvent;
    var key = (runState.Id, runState.CurrentActIndex, coord.Column, coord.Row);
    if (!AbnormalityRegistry._nodeAssignments.TryGetValue(key, out var id)) return currentEvent;
    return AbnormalityRegistry.GetEventForAbnormality(id) ?? currentEvent;
}
```

**전제 검증:** `IRunState.CurrentMapCoord`/`CurrentActIndex`/`Map`/`CurrentRoom` 은 public 인터페이스에 존재 (IRunState.cs, verification.md §5.4 확인).

**잔여 리스크 (QA 단계):**
- **R2**: 세이브/로드 시 다부위 partial load 엣지 케이스에서 `Hook.AfterMapGenerated` 발화 타이밍 (lazy hydrate 로 안전망).
- **R4**: 첫 방 진입 시 `CurrentMapCoord` null window 가능성 — `AddVisitedMapCoord` 호출 순서 실행 검증.

상세 리스크 분석은 `abnormality-battle.verification.md` §5.5 참조.

## 7. 구현 순서

| 단계 | 작업 | 검증 방법 |
|------|------|-----------|
| 1 | `Template/` 부모 클래스 4개 작성 (Event/Encounter/Monster/Registry) | 컴파일 통과 |
| 2 | 흑단여왕 단일 부위 PoC (`Head`만) — Encounter 등록 + 콘솔 `fight` 호출 | 게임 내 전투 진입, 몬스터 등장 |
| 3 | 배경/BGM 에셋 추가 + `HasCustomBackground` / `CustomBgm` 적용 | 전투 화면 배경/음악 변경 확인 |
| 4 | 다부위 (`LeftArm`/`RightArm`/`Root`) 추가, AI 패턴 (`Moves/`) 작성 | 4부위 동시 등장, 인텐트 정상 동작 |
| 5 | `EbonyQueenAppleEvent` 작성 + 콘솔 `event` 호출로 이벤트 → 전투 흐름 검증 | 선택지 → 전투 → 결과 → 맵 복귀 |
| 6 | `MapIntegration/AbnormalityEventRouter` — `Hook.AfterMapGenerated`(hydrate) + `Hook.ModifyNextEvent`(라우팅) 구독 | 맵 노드 클릭 → 환상체 이벤트 진입 |
| 7 | 로컬라이제이션 키 정리 (`encounters.json` / `monsters.json` / `events.json`) | 한국어/영어 텍스트 정상 표시 |

각 단계마다 직전 단계가 완전히 동작한 뒤에만 다음으로 진행. 단계 5 완료 후 v1 PoC 완성.

## 8. 미해결 / 후속 작업

### 8.1 v1 직전에 확인해야 할 것

- `MonsterModel` 정확한 추상 메서드 시그니처 (HP / Move 풀 / 스프라이트 로드) — verification.md §3.4 체크리스트에서 해소
- `Move` / `Intent` 정의 방식 (별도 클래스인지, 데이터인지) — verification.md 에서 `MonsterMoveStateMachine` / `MoveState` / `AbstractIntent` 계층 확인
- `EncounterModel.Slots` 의미 — verification.md §2.3 에서 임의 문자열 허용 확인
- **맵 노드 ↔ 환상체 ID 저장 + 라우팅** — §6 에서 Option A + Hook 단일 경로로 확정
- FMOD 이벤트 경로 형식 — verification.md §9 에서 v1 은 `CustomBgm => ""` 로 우회, v2 에서 Godot 네이티브 경로

**M1 런타임 실행 검증 항목은 `doc/todo.md` 의 "abnormality-battle — M1 런타임 실행 검증 필요" 섹션 참조.**

### 8.2 v2 이후

- **EGO 카드 시스템**: 환상체 승리 시 보상으로 받는 특수 카드 — 별도 plan
- **페이즈 전환 이벤트**: 흑단여왕 사과의 "fruit 50% HP" 같은 특수 이벤트 — 게임의 `Hook.AfterDamageGiven` 등으로 구현 가능
- **환상체 관찰 로그 UI**: 림버스의 환상체 정보 화면
- **Mirror Dungeon 통합**: 환상체전을 거울 던전 풀에 추가
- **멀티플레이어 동기화 검증**: 다부위 환상체의 네트워크 동기화

### 8.3 알려진 위험

- **다부위 = 다중 몬스터**: STS2는 "여러 몬스터를 한 개체로 표현"하는 시스템이 없으므로, 부위별 독립 `MonsterModel`로 표현. 이 때 "한 부위만 죽어도 전투 종료"가 되지 않도록 주의 필요 — 게임의 기본 동작은 모든 적 사망 시 종료이므로 문제없을 가능성이 높지만 구현 시 검증.
- **배경 에셋 경로 규약**: `EncounterModel.Id.Entry`를 lowercase 변환한 경로로 자동 로드. 클래스명 작명이 곧 에셋 경로 결정. `EbonyQueenAppleEncounter` → `ebonyqueenappleencounter` 폴더.
- **이벤트 ID 충돌**: 바닐라 이벤트와 환상체 이벤트 ID가 충돌하지 않도록 prefix (예: `ABNORMALITY_*`) 사용 권장.

> ⚠ **2026-04-22 verification 반영 — §8.3 리스크 갱신**
>
> - **다부위 전투 종료 — 검증됨 + 보강:** `CombatManager.IsEnding` 은 `Enemies.Any(e => e.IsAlive && e.IsPrimaryEnemy)` 로 전투 종료 판정. **부위에 `MinionPower` 적용 전제라면**, 본체(Primary) 만 죽으면 부위(Secondary) 생존 여부와 무관하게 자동 종료 — 바닐라 동작으로 보장됨. v2 페이즈 전환 시 `Hook.ShouldStopCombatFromEnding` 추가 차단 훅 활용 가능 (M1-4 에서 시그니처 확인).
> - **배경 에셋 경로 규약 — 원안 오류 (위 §3 ⚠ 블록 참조):**
>   - ~~`EncounterModel.Id.Entry` → `ebonyqueenappleencounter` (밑줄 없음)~~
>   - **실제**: `StringHelper.Slugify` 는 **CamelCase → snake_case (밑줄 포함)** → `ebony_queen_apple_encounter`. 경로는 `res://scenes/backgrounds/{id_snake_case}/layers/` (`res://images/backgrounds/encounters/` 아님).
> - **이벤트 ID 충돌 — 런타임 확인 필요 (M1-7):** 바닐라에 `ABNORMALITY_*` prefix 존재 여부 실행 검증. BaseLib `CustomEncounterModel` 이 자동 prefix 를 붙이지 않으므로 **클래스명 자체** 로 충돌 회피 (예: `TheCityEbonyQueenAppleEncounter`).

## 9. 참조

- 게임 레퍼런스: `MegaCrit.Sts2.Core.Models.Events.BattlewornDummy` — 이벤트→전투→복귀 패턴의 가장 가까운 예시
- 게임 레퍼런스: `MegaCrit.Sts2.Core.Models.EncounterModel` — `HasCustomBackground` / `CustomBgm` 오버라이드 포인트
- 게임 레퍼런스: `MegaCrit.Sts2.Core.Rooms.CombatRoom` — `ParentEventId` / `ShouldResumeParentEventAfterCombat` 필드로 이벤트 복귀 메커니즘
- 게임 레퍼런스: `TorchHeadAmalgam`, `DoormakerBoss`, `CubexConstruct`, `FabricatorNormal` — 다부위/상태머신/소환 패턴 (2026-04-22 verification 추가)
- 선행 plan: `doc/plan/abnormality-map-node.md` — 맵 노드 생성/등록 (이미 구현됨)
- **검증 리포트**: `doc/plan/abnormality-battle.verification.md` — 2026-04-22 게임 API 검증 + 에셋 경로 규약 + M1 선결 체크리스트 (2026-04-22 추가)
- 림버스 wiki: https://limbuscompany.wiki.gg/wiki/Ebony_Queen%27s_Apple — PoC 환상체 스펙 참고
