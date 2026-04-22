# Plan: abnormality-battle

**Feature:** abnormality-battle
**상태:** Plan 작성 (Do 미시작)
**작성:** 2026-04-22
**관련 문서:** `abnormality-map-node.md` (맵 노드 시스템 — 선행 완료)

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

### 2.4 맵 노드 → 이벤트 분기 위치

`RunManager.cs` 의 방 생성 분기점에서 `MapPointType` 별로 `EventModel` / `EncounterModel`을 결정. 이미 구현된 `AbnormalityMapPointType` (`abnormality-map-node.md` 참조)에 대해 전용 `EventModel` 반환하도록 Harmony 패치 추가 필요. 정확한 메서드 시그니처는 구현 시점에 확인.

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
│   ├── AbnormalityRoomRouter.cs       # MapPointType 진입 시 EventModel 반환 (Harmony)
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

## 6. 맵 노드 라우팅

`AbnormalityMapPointType` (이미 구현됨) 진입 시 호출되는 `RunManager` 메서드를 Harmony 패치하여 `AbnormalityRegistry`에서 `EventModel` 조회 후 반환.

```csharp
[HarmonyPatch(typeof(RunManager), "CreateRoom")]
public static class AbnormalityRoomRouterPatch
{
    public static bool Prefix(RoomType roomType, MapPointType mapPointType,
                              AbstractModel? model, ref AbstractRoom __result)
    {
        if (mapPointType != AbnormalityMapPointType.Value) return true;  // 게임 기본 동작

        // 노드에 저장된 환상체 ID로 이벤트 조회 (저장 메커니즘은 abnormality-map-node에서 정의)
        var abnormalityId = ResolveAbnormalityIdFromCurrentNode();
        var eventModel = AbnormalityRegistry.GetEventForAbnormality(abnormalityId);
        if (eventModel == null) return true;

        __result = new EventRoom(eventModel);
        return false;  // 게임 기본 분기 스킵
    }
}
```

**미해결**: 어떤 환상체 노드인지 노드별로 어떻게 식별할지. 노드 생성 시 `model: ModelId` 파라미터로 전달하는 게 자연스러울 가능성. 구현 시 `abnormality-map-node.md` 보강 필요.

## 7. 구현 순서

| 단계 | 작업 | 검증 방법 |
|------|------|-----------|
| 1 | `Template/` 부모 클래스 4개 작성 (Event/Encounter/Monster/Registry) | 컴파일 통과 |
| 2 | 흑단여왕 단일 부위 PoC (`Head`만) — Encounter 등록 + 콘솔 `fight` 호출 | 게임 내 전투 진입, 몬스터 등장 |
| 3 | 배경/BGM 에셋 추가 + `HasCustomBackground` / `CustomBgm` 적용 | 전투 화면 배경/음악 변경 확인 |
| 4 | 다부위 (`LeftArm`/`RightArm`/`Root`) 추가, AI 패턴 (`Moves/`) 작성 | 4부위 동시 등장, 인텐트 정상 동작 |
| 5 | `EbonyQueenAppleEvent` 작성 + 콘솔 `event` 호출로 이벤트 → 전투 흐름 검증 | 선택지 → 전투 → 결과 → 맵 복귀 |
| 6 | `MapIntegration/AbnormalityRoomRouter` Harmony 패치 | 맵 노드 클릭 → 이벤트 진입 |
| 7 | 로컬라이제이션 키 정리 (`encounters.json` / `monsters.json` / `events.json`) | 한국어/영어 텍스트 정상 표시 |

각 단계마다 직전 단계가 완전히 동작한 뒤에만 다음으로 진행. 단계 5 완료 후 v1 PoC 완성.

## 8. 미해결 / 후속 작업

### 8.1 v1 직전에 확인해야 할 것

- `MonsterModel` 정확한 추상 메서드 시그니처 (HP / Move 풀 / 스프라이트 로드)
- `Move` / `Intent` 정의 방식 (별도 클래스인지, 데이터인지)
- `EncounterModel.Slots` 의 의미 — `"left"` / `"right"` 같은 슬롯명을 직접 정의 가능한지, 아니면 게임이 정의한 슬롯만 사용 가능한지
- `RunManager.CreateRoom` 정확한 시그니처 + Harmony 패치 가능 여부
- 맵 노드에 환상체 ID를 저장하는 메커니즘 (`abnormality-map-node.md` 보강 필요)
- FMOD 이벤트 경로 형식 — 모드 BGM을 FMOD 뱅크에 추가하는 방법

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

## 9. 참조

- 게임 레퍼런스: `MegaCrit.Sts2.Core.Models.Events.BattlewornDummy` — 이벤트→전투→복귀 패턴의 가장 가까운 예시
- 게임 레퍼런스: `MegaCrit.Sts2.Core.Models.EncounterModel` — `HasCustomBackground` / `CustomBgm` 오버라이드 포인트
- 게임 레퍼런스: `MegaCrit.Sts2.Core.Rooms.CombatRoom` — `ParentEventId` / `ShouldResumeParentEventAfterCombat` 필드로 이벤트 복귀 메커니즘
- 선행 plan: `doc/plan/abnormality-map-node.md` — 맵 노드 생성/등록 (이미 구현됨)
- 림버스 wiki: https://limbuscompany.wiki.gg/wiki/Ebony_Queen%27s_Apple — PoC 환상체 스펙 참고
