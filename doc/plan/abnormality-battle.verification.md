# abnormality-battle plan 검증 리포트

**대상 문서:** `doc/plan/abnormality-battle.plan.md` (2026-04-22 작성)
**검증 일자:** 2026-04-22
**검증 담당:** cto-lead (오케스트레이션) · sts-game-analyst (게임코드) · mod-code-analyst (모드 코드) · web-researcher (외부 사례)
**plan 문서 수정 여부:** 수정하지 않음 (이 리포트는 별도 분석 노트)

## Executive Summary

plan 의 **개념적 구조 (3계층 상속, 이벤트→전투→복귀 패턴, 다부위 표현)** 는 STS2 게임코드와 호환됨. 하지만 **구체 API 시그니처·경로·클래스명 레벨에서 8건의 치명/중대 수정이 필요**. **맵 노드 ↔ 환상체 ID 저장 메커니즘 (plan §8.1 미해결 1건)** 은 Option A 설계로 확정. 게임코드 전제 (`IRunState` 속성, `Hook.ModifyNextEvent`, `Hook.AfterMapGenerated`) 는 sts-game-analyst 최종 재검증 완료 (2026-04-22 부록 A 갱신).

**판정 요약:**
| 범주 | 검증됨 | 수정필요 | 미결 |
|---|:---:|:---:|:---:|
| plan §2 게임코드 조사 | 3 | **3 (치명)** | 0 |
| plan §3 폴더·에셋 | 1 | **2 (중대)** | 0 |
| plan §4 부모 클래스 설계 | 2 | **3 (중대)** | 0 |
| plan §5 구현 예시 | 0 | **2 (중대)** | 0 |
| plan §6 라우팅 | **3** | **1 (중대)** | **0 (Q4 해소)** |
| plan §8.3 다부위 종료 | 1 | 1 (보강) | 0 |

## 1. plan 가정별 라벨링

### §2.1 전투 진입 흐름
**검증됨.** `RunManager.CreateRoom(RoomType, MapPointType, AbstractModel?)` 존재 (private). roomType switch 분기. Event 분기는 `PullNextEvent` 또는 `PullAncient` 호출.

### §2.2 BattlewornDummy 패턴 — **치명적 3건 수정필요**

| plan 라인 | plan 가정 | 실제 | 수정 |
|---|---|---|---|
| 69-76 | `GenerateInitialOptions()` 호출 O | O | 없음 |
| 82 | `EnterCombatWithoutExitingEvent(encounter, Array.Empty<Reward>(), shouldResumeAfterCombat: true)` | protected 맞음. **그러나 EventModel 이 `IsShared=false` 면 `InvalidOperationException` throw 발생**. 환상체전 EventModel 은 **반드시 `public override bool IsShared => true;` 필수** | §2.2 코드 스니펫 상단에 `public override bool IsShared => true;` 추가 |
| 88-94 | `Resume(AbstractRoom room)` override, base 호출 없음 | 일치 (base 는 `Task.CompletedTask`) | 없음 |
| **90** | **`var won = !combatRoom.CombatState.AreAllPlayersDead;`** | **`AreAllPlayersDead` API 존재하지 않음. 전체 게임코드 0건 매치.** BattlewornDummy 실제 패턴은 encounter 에 **커스텀 플래그 `RanOutOfTime`** 저장 → `Resume()` 에서 읽음 | **치명 수정**: 삭제 후 encounter 커스텀 플래그 패턴 (+SaveCustomState/LoadCustomState) 으로 재작성 |
| 93-94 | `SetEventFinished("string")` | `SetEventFinished(LocString)` — string 직접 전달 시 컴파일 에러 | `L10NLookup(...)` 결과 전달하도록 수정 |

**추가 제약 (plan 미언급)**: `EnterCombatWithoutExitingEvent` 는 `shouldResumeAfterCombat && LayoutType == EventLayoutType.Combat` 조합이면 또 다른 throw. 환상체 이벤트 레이아웃 결정 시 주의.

### §2.3 EncounterModel 오버라이드 포인트 — **경미 수정 + 보강**

| 멤버 | plan 가정 | 실제 | 수정 |
|---|---|---|---|
| `HasCustomBackground` | `virtual bool` | **`protected virtual bool`** | 접근수정자 명시 |
| `CustomBgm` | `public virtual string => ""` | 일치 | 없음 |
| `AmbientSfx` | `public virtual string => ""` | 일치 | 없음 |
| `GenerateMonsters` | `protected abstract IReadOnlyList<(MonsterModel, string?)>` | 일치 | 없음 |

**plan §2.3 에 누락된 중요 멤버** (구현자에게 필요):
- `RoomType` (abstract, 반드시 override)
- `AllPossibleMonsters` (abstract)
- `Slots` (`public virtual IReadOnlyList<string>` — 임의 문자열 허용, enum 아님)
- `BossNodePath` (맵 노드 Spine 데이터 경로 규약, plan 미언급)
- `ExtraAssetPaths` (커스텀 에셋 프리로드)
- `Tags`, `HasScene`, `FullyCenterPlayers`, `GetCameraScaling/Offset`

### §2.4 맵 노드 → 이벤트 분기 위치
**검증됨 (부분).** `RunManager.CreateRoom` 은 roomType switch 지만 MapPointType 은 Event.Ancient 분기에서만 참조. 환상체 라우팅은 §6 에서 별도 수정 (아래 §6 항 참조).

## 2. §3 폴더/에셋 구조 — **중대 2건 수정필요**

### 2.1 배경 경로 규약 — **전면 오류**

| plan | 실제 |
|---|---|
| `res://images/backgrounds/encounters/ebonyqueenappleencounter/` | **`res://scenes/backgrounds/ebony_queen_apple_encounter/`** |
| `bg.png` 단일 파일 | **`{title}_background.tscn` (Godot scene) + `layers/` 서브디렉토리 필수** |

**실제 로딩 체인** (`BackgroundAssets` 생성자):
1. `DirAccess.Open("res://scenes/backgrounds/{id_lower}/layers")` — **없으면 `InvalidOperationException` throw**
2. `layers/` 내부에 `_fg_*` (전경 1개 이상) + `_bg_*` (배경 1개 이상) 파일 필수
3. `_bg_` 뒤 첫 `_` 까지 문자열로 그룹 키 결정 → 같은 그룹 내 RNG 선택
4. `_background.tscn` 메인 scene 도 같은 폴더에

### 2.2 `Id.Entry` 슬러그화 규칙 — **예시 오류**

`ModelDb.GetEntry(Type)` 는 `StringHelper.Slugify(type.Name)` 호출 → CamelCase → `UPPER_SNAKE_CASE`:
- `EbonyQueenAppleEncounter` → `"EBONY_QUEEN_APPLE_ENCOUNTER"` → lower → **`"ebony_queen_apple_encounter"`** (밑줄 포함)
- plan §3 의 `ebonyqueenappleencounter` (밑줄 없음) 는 전부 **`ebony_queen_apple_encounter`** 로 교체

`ModelId.Entry` 에는 **모드 접두사 자동 주입 없음**. BaseLib 의 `CustomEncounterModel` 존재 여부/prefix 주입은 런타임 QA 항목. 충돌 회피는 **클래스명 자체로** 해결 권장 (예: `TheCityEbonyQueenAppleEncounter`).

## 3. §4 부모 클래스 설계 — **중대 3건 수정필요**

### 3.1 §4.1 AbnormalityEvent — 치명 수정
- `public override bool IsShared => true;` **반드시 추가** (plan 엔 있으나 이유 설명 필요 — `EnterCombatWithoutExitingEvent` 가 `IsShared==false` 시 throw).
- `Resume` 내부의 `combatRoom.CombatState.AreAllPlayersDead` **삭제** → encounter 커스텀 플래그 (`IsVictory` / `IsLoss` 등) 패턴으로 재작성.
- `SetEventFinished` 인자 타입 `LocString` 로 교정.

### 3.2 §4.2 AbnormalityEncounter — 접근수정자 교정
- `HasCustomBackground` 는 `protected virtual`. plan §4.2 의 `protected override bool HasCustomBackground => true;` 라인은 이미 `protected` 이므로 OK — 단 §2.3 의 선언을 교정하는 게 목적.
- `GetBgmEventPath()` 추상 메서드 설계는 BGM 전략 결정 후 재검토 (§5 참조).

### 3.3 §4.3 AbnormalityMonster — **`IsCore` 가정 폐기**
- plan 의 `public virtual bool IsCore => false;` 은 **게임 전역에 존재하지 않는 가정**. 게임의 실제 메커니즘:
  - `Creature.IsPrimaryEnemy = Side==Enemy && !IsSecondaryEnemy`
  - `Creature.IsSecondaryEnemy = Powers.Any(p => p.OwnerIsSecondaryEnemy)`
  - **`MinionPower.OwnerIsSecondaryEnemy => true`** (게임 내 유일하게 override 하는 파워)
- **올바른 패턴**: "본체" 는 아무 조치 불필요 (Primary 가 default). "부위" 는 `AfterAddedToRoom()` 에서 `PowerCmd.Apply<MinionPower>(base.Creature, 1m, base.Creature, null)` 호출.
- 바닐라 레퍼런스: **TorchHeadAmalgam.cs:43** (자기 자신에게 MinionPower 적용하는 가장 단순한 패턴), DoormakerBoss/Door/Doormaker (런타임 추가 + MinionPower 패턴).

### 3.4 §4.3 MonsterModel 추상 멤버 (plan 이 비워둔 부분) — **구현자 체크리스트**

**필수 (abstract):**
- `MinInitialHp` / `MaxInitialHp` (int)
- `GenerateMoveStateMachine()` → `MonsterMoveStateMachine`

**주요 virtual (흔히 override):**
- `Title` (LocString, 기본 `L10NMonsterLookup(Id.Entry + ".name")`)
- `VisualsPath` (기본 `res://scenes/creature_visuals/{id_lower}.tscn`)
- `AttackSfx` / `CastSfx` / `DeathSfx` (FMOD: `event:/sfx/enemy/enemy_attacks/{lower}/{lower}_attack` 등)
- `HurtSfx`, `TakeDamageSfxType`, `BestiaryAttackAnimId`
- `IsHealthBarVisible` (다부위 본체만 표시 시)
- `AfterAddedToRoom()` (초기 Power/블록 적용, MinionPower 부착)

**Move 시스템 (plan 에 구조 설명 필요):**
```
MonsterMoveStateMachine
   └ Dictionary<string, MonsterState> States
   └ MonsterState _initialState

MoveState : MonsterState
   └ Intents: IReadOnlyList<AbstractIntent>   // UI 예고
   └ FollowUpState / FollowUpStateId          // 다음 수 체인
   └ _onPerform: Func<IReadOnlyList<Creature>, Task>  // 실제 로직

AbstractIntent (추상)
   └ SingleAttackIntent(damage)
   └ MultiAttackIntent(damage, hitCount)
   └ BuffIntent / DefendIntent / DebuffIntent(...) / SummonIntent / EscapeIntent
```

바닐라 레퍼런스 (단순 상태머신): `CubexConstruct.GenerateMoveStateMachine()` — ChargeUp → Blast1 → Blast2 → Expel 루프, FollowUpState 체인.

### 3.5 §4.4 AbnormalityRegistry
**검증됨.** 설계 자체는 합리적. 단 `ModelDb.GetByType(type) as EventModel` 패턴은 게임코드에서 확인되지만, plan §6 의 `GetEventForAbnormality(abnormalityId)` 호출 주체가 `Hook.ModifyNextEvent` 여야 할지 `CreateRoom` Prefix 여야 할지는 §6 에서 재결정.

## 4. §5 구현 예시 (EbonyQueenApple) — **중대 2건 수정필요**

### 4.1 §5.1 EbonyQueenAppleEvent
- `OnApproach` / `OnObserve` 선택지 콜백 구조는 타당.
- `OnVictory` 내부 `SetEventFinished(L10NLookup("EBONY_QUEEN_APPLE.victory"))` — `L10NLookup` 반환 타입 확인 필요. `EventModel.L10NLookup` 은 `LocString` 반환이므로 OK.
- **승리 판정 로직 부재**: `Resume` 을 부모 `AbnormalityEvent` 에 두고 승/패 분기를 거기서 처리하는 설계는 좋으나 §4.1 의 `combatRoom.CombatState.AreAllPlayersDead` 가 동작 안 함. **EbonyQueenAppleEncounter 에 커스텀 `IsDefeated` 플래그 + `SaveCustomState/LoadCustomState` 필수**.

### 4.2 §5.2 EbonyQueenAppleEncounter
- `Slots` 가 자유 문자열임이 검증됐으므로 `"center"/"left"/"right"/"ground"` 자체는 **허용**. 단 게임 내 관용은 `"first"/"second"` 또는 `"segment1"/"segment2"` 같은 순서/역할명이 더 일반적. 시각적 배치는 **별도 `.tscn` scene 이 있을 때만** Slots 순서가 반영됨 (`HasScene => true` + `res://scenes/encounters/{lower}.tscn`). scene 없으면 기본 레이아웃.
- `AllPossibleMonsters` + `GenerateMonsters` 분리 구조는 일치.
- **추가 필요**: "본체" 인 `EbonyQueenApple_Head` 는 아무 것도 안 해도 Primary. "부위" 인 `_LeftArm/_RightArm/_Root` 는 **각자의 `AfterAddedToRoom` 에서 MinionPower 적용 필수**. plan §5.2 에 이 코드 누락.

## 5. §6 라우팅 계층 — **중대 재설계 + 2건 확인 불가**

### 5.1 기존 abnormality-map-node 구현과의 관계
현재 `src/Map/MapPointTypePatches.cs` 는 `RunManager.RollRoomTypeFor` 를 Prefix 패치해 `MapPointType.Abnormality → RoomType.Event` 를 매핑. 즉 **모든 Abnormality 노드는 현재 바닐라 이벤트 풀에서 랜덤 이벤트를 뽑는 상태**. plan §6 은 이걸 "환상체 전용 EventModel" 로 바꿔야 함.

### 5.2 plan §6 의 `CreateRoom` Prefix 가정 — 대안이 더 낫다

**검증됨:** `RunManager.CreateRoom(RoomType, MapPointType, AbstractModel?)` 은 private 이지만 Harmony `[HarmonyPatch(typeof(RunManager), "CreateRoom", new[] { typeof(RoomType), typeof(MapPointType), typeof(AbstractModel) })]` + `ref AbstractRoom __result` Prefix + `return false` 패턴으로 대체 가능.

**더 나은 대안 (채택): `Hook.ModifyNextEvent(IRunState, EventModel)`** — 공식 훅. `ActModel.PullNextEvent` 내부에서 호출 (ActModel.cs:340):
```csharp
EventModel eventModel = Hook.ModifyNextEvent(runState, _rooms.NextEvent);
```
- 카테고리 `modify/general`. Event 방에서만 발화 — Encounter 는 별도 경로 (`ActModel.PullNextEncounter`).
- `runState.CurrentMapCoord` / `runState.CurrentActIndex` 로 모드측 Dictionary 조회 → 매칭 시 환상체 EventModel 반환, miss 시 `currentEvent` 그대로 반환.

### 5.3 "어느 환상체인가" 식별 — **Option A (Dictionary + 결정론 해시) 확정**

mod-code-analyst 의 설계 옵션 분석:
| 옵션 | 평가 | 채택 |
|---|---|:---:|
| A. 모드측 `Dictionary<(actIndex, col, row), string>` + 결정론 해시 | 패치 0개, publicizer 불필요, 결정론 해시 재사용 | **확정** |
| B. `SerializableMapPoint` 직렬화 확장 | 바닐라 로드 시 pipeline 파괴 — R3 치명 악화 | 기각 |
| C. Godot 노드 SetMeta | UI 노드 간접화, 결국 hydrate 필요 — A 대비 이득 없음 | 기각 |
| D. 이벤트 풀 RNG 선택 | 맵에서 "어느 환상체인지" 프리뷰 불가능 — UX 봉쇄 | 기각 |

**Option A 구현 세부:**
1. `AbnormalityMapInjector.Inject(...)` 가 노드 교체 시점에 이미 결정론 해시 (`hash(seed, actIndex, col, row)`) 를 계산하고 있으므로, **같은 해시 공식** 으로 `registeredIds[hash % count]` 을 골라 `AbnormalityRegistry._nodeAssignments[(actIndex, coord)] = id` 저장.
2. 이벤트 진입 시점 (`Hook.ModifyNextEvent` Postfix) 에 `runState.CurrentMapCoord`/`runState.CurrentActIndex` 로 lookup.
3. 세이브/로드 저장·복원 왕복: Dictionary 자체는 저장 안 함. **lazy hydrate** — lookup miss 시 즉석 해시 계산 (같은 입력 → 같은 출력 보장).
4. **Hydrate 주 경로** (neat form): `Hook.AfterMapGenerated(IRunState, ActMap, int actIndex)` 구독 — 세이브 로드 / 신규 run 양쪽 모두 발화 확정 (RunManager.cs:571, 577). act 당 1회 호출.

### 5.4 게임 API 검증 결과 (2026-04-22 최종) — **Option A 완전 지원**

sts-game-analyst 최종 재검증 (Q4a/b/c) 결과:

| 질문 | 결론 | 출처 |
|---|---|---|
| **Q4a.** `IRunState` 의 load 완료 hook | **해소** — `Hook.AfterRunLoaded` 전역은 없지만, `Hook.AfterMapGenerated` 가 **세이브 로드 경로에서도 발화** (RunManager.cs:577). hydrate 주 지점으로 사용. 차선: `Hook.BeforeRoomEntered` (MapRoom 자동 제외). | RunManager.cs:571, 577, 918 |
| **Q4b.** `RunManager.PullNextEvent` 필터링 훅 | **해소** — `Hook.ModifyNextEvent(IRunState runState, EventModel currentEvent)` 존재. `ActModel.PullNextEvent` 내부에서 호출. `currentEvent` 은 `_rooms.NextEvent` (`EnsureNextEventIsValid` 선행 실행으로 비-null 보장). | ActModel.cs:340 |
| **Q4c.** CreateRoom 시점 현재 맵 노드 좌표 접근 | **해소** — `IRunState.CurrentMapCoord` (`MapCoord?`) / `CurrentMapPoint` (`MapPoint?`) / `CurrentActIndex` / `Map` (ActMap) / `CurrentRoom` 모두 public 인터페이스에 존재. `Hook.ModifyNextEvent` 파라미터로 `IRunState` 바로 받으므로 Traverse/publicizer 불필요. | IRunState.cs |

### 5.5 확정 구현 전략 — **단일 경로**

**Option A + `Hook.AfterMapGenerated` (hydrate) + `Hook.ModifyNextEvent` (routing)** — 게임 API 추정 없이 공식 훅만으로 완결:

1. **맵 주입 단계** (`AbnormalityMapInjector.Inject` 확장): 현재 `point.PointType = Abnormality` 만 하는 자리에, **같은 시점에 결정론 해시로 환상체 ID 를 계산해 `AbnormalityRegistry._nodeAssignments[(actIndex, coord)] = id` 에 저장**. 해시 공식은 이미 있음 (Inject.cs line 47-63).
2. **Hydrate 단계** (`Hook.AfterMapGenerated` 구독): act 진입 / 세이브 로드 시 `ActMap` 순회해 모든 Abnormality 노드에 대해 Dictionary 동기화. lazy hydrate 도 병행 (lookup miss 시 즉석 계산) — 양쪽 경로로 결정론성 보증.
3. **라우팅 단계** (`Hook.ModifyNextEvent` 구독): `runState.CurrentMapCoord` + `runState.CurrentActIndex` → Dictionary lookup → 매칭 시 `AbnormalityRegistry.GetEventForAbnormality(id)` 결과 반환, miss 시 `currentEvent` 그대로 반환.
4. **`RunManager.CreateRoom` Prefix 는 불필요** — plan §6 의 원안 경로는 폐기. Hook 기반이 훨씬 안전.

**잔여 리스크 (Implementer 확인 항목):**
- **R1**: `Hook.ModifyNextEvent` 는 **Event 방에서만** 발화. 환상체가 실제 `RoomType.Event` 로 진입함을 `src/Map/MapPointTypePatches.cs` (`RollRoomTypeFor` Prefix) 가 이미 보장 — 재확인 불요.
- **R2**: 세이브 로드 시 `SavedActMap` 으로 복원된 맵이 `Hook.AfterMapGenerated` 에 전달되는 타이밍 — RunManager.cs:577 기준 확정이나, 다부위 로드 경로 (partial load) 엣지 케이스는 런타임 QA 필요.
- **R3**: 멀티플레이 peer 간 `Hook.ModifyNextEvent` 결정론성 — 해시 시드가 모든 peer 에서 동일해야 함. `AbnormalityMapInjector` 가 이미 결정론 해시를 쓰고 있으므로 자연 보장.
- **R4**: 첫 방 진입 시 `CurrentMapCoord` null window — `AddVisitedMapCoord` 호출 순서 런타임 확인 필요.

plan §6 는 **Hook 기반 단일 경로** 로 재작성. `CreateRoom` Prefix 는 제거.

## 6. §7 구현 순서 — **영향 없음**
단계 1~7 구조는 유지. 단 각 단계 내용은 위 수정사항 반영 필요.

## 7. §8.1 미해결 항목 판정

| plan 미해결 | 검증 결과 |
|---|---|
| MonsterModel 정확한 추상 메서드 시그니처 | **해결** (§3.4 체크리스트) |
| Move / Intent 정의 방식 | **해결** (별도 클래스 계층, MonsterMoveStateMachine / MoveState / AbstractIntent) |
| `EncounterModel.Slots` 의미 | **해결** — 임의 문자열, scene 존재 시 배치 순서, 없으면 기본 레이아웃 |
| `RunManager.CreateRoom` 정확한 시그니처 + Harmony 가능성 | **해결** (§5.2) |
| 맵 노드에 환상체 ID 저장 메커니즘 | **해결** — Option A (Dictionary + `Hook.AfterMapGenerated` hydrate + `Hook.ModifyNextEvent` 라우팅). §5.4 확정 |
| FMOD 이벤트 경로 형식 — 모드 BGM | **해결** (§8 아래) |

## 8. §8.3 "다부위 = 다중 몬스터" 리스크 — **원칙 맞음 + 구체 패턴 명시 필요**

판정: **ACCEPT + ENHANCE.**
- "한 부위만 죽어도 전투 종료 X" 는 `CombatManager.IsEnding` 의 `Enemies.Any(e => e.IsAlive && e.IsPrimaryEnemy)` 로 **기본 보장**.
- 단 부위에 **`MinionPower` 적용 필수** — plan §8.3 에 이 구체 메커니즘 명시 추가.
- `Hook.ShouldStopCombatFromEnding` 추가 차단 훅도 존재 (페이즈 전환 등 v2 구현 시 활용 가능).

## 9. FMOD/BGM (Q6) — **우회 가능, v1 생략 권장**

| 경로 | 평가 |
|---|---|
| A. FMOD 뱅크 주입 (`Engine.GetSingleton("FmodServer").Call("load_bank", ...)`) | 기술적 가능, 오버엔지니어링, v1 제외 |
| **B. Godot 네이티브** (`AudioStreamMp3.LoadFromFile` + `AudioStreamPlayer` 커스텀) | 가장 가볍고 Godot 4.4+ API, **v2 에서 채택** |
| C. Harmony 인터셉트 (`EncounterModel.CustomBgm` getter 재작성) | B 의 상위 — 필요 시 M 단계에서 |

**v1 권장**: `CustomBgm => ""` 빈 문자열 반환 → 게임 기본 BGM 유지. BGM 커스터마이즈는 v2 스코프.

선행사례: **Spire Radio (Nexus #131)** 가 mp3 + BaseLib 조합으로 동작 중 (소스 비공개이지만 mp3 지원 = Godot 네이티브 경로 추정). `elliotttate/sts2-fmod-tools` 가 FMOD 뱅크 주입 패턴 레퍼런스.

BaseLib 자체에는 **audio API 없음** (Wiki 및 폴더 구조 검증).

## 10. 구현 1단계 (M1) 진입 전 선결 항목

**반드시 해결 후 Implementer 호출:**

1. **plan §2.2 치명 수정** — `AreAllPlayersDead` 제거, encounter 커스텀 플래그 패턴 채택, `IsShared => true` 필수 명시, `SetEventFinished(LocString)` 타입 교정.
2. **plan §3 경로 규약 수정** — `res://scenes/backgrounds/{id_lower}/` + `layers/` 서브디렉토리 + `_bg_`/`_fg_` 파일 규약. `id_lower` 는 밑줄 포함 (`ebony_queen_apple_encounter`).
3. **plan §4.3 / §8.3 다부위 패턴 명시** — `IsCore` 폐기, `MinionPower` 적용 패턴 채택. TorchHeadAmalgam 레퍼런스.
4. **plan §4.1 / §5 승패 판정 메커니즘 통일** — encounter 에 `IsDefeated`/`IsVictory` 커스텀 플래그 + SaveCustomState/LoadCustomState 추가 설계.
5. **plan §6 라우팅 재작성** — Option A (Dictionary + 결정론 해시) 확정. `Hook.AfterMapGenerated` (hydrate) + `Hook.ModifyNextEvent` (라우팅) 단일 경로로 명시. `CreateRoom` Prefix 제거. 게임 API 전제는 sts-game-analyst 최종 재검증 (2026-04-22 부록 A) 으로 모두 검증됨.
6. **BGM 전략 확정** — v1 은 `CustomBgm => ""` 로 기본 BGM. 커스텀 BGM 은 v2.
7. **BaseLib `CustomEncounterModel` 존재 여부 런타임 확인** — 존재하면 prefix 규약 확인, 없으면 `EncounterModel` 직접 상속 + 수동 `ModelDb.Inject` 후 Entry 는 slugified class name.

**Implementer 단계 런타임 검증 항목:**
- MinionPower 적용 후 전투 UI (target reticle, end-of-combat screen) 정상 여부.
- `Hook.ShouldStopCombatFromEnding` 가 secondary 만 남은 상태에서도 호출되는지.
- `.tscn` 배경 scene 구조 샘플 확보 (바닐라 encounter 에셋 `res://scenes/backgrounds/` 열어서).
- `Hook.AfterMapGenerated` 세이브 로드 경로 실제 발화 (R2) — 다부위 로드 엣지 케이스 포함.
- 첫 방 진입 시 `CurrentMapCoord` null window (R4) — `Hook.ModifyNextEvent` 호출 시점 기준.

## 11. 검증 수단 / 도구 한계

**확인 성공:**
- sts-game-analyst: MCP `search_game_code` / `get_entity_source` 활용 — BattlewornDummy, EncounterModel, MonsterModel, CombatManager, BackgroundAssets, MinionPower 등 직접 소스 확인.
- mod-code-analyst: `src/Map/` 전체 파일 정독 + plan 대조 → 설계 옵션 비교.
- web-researcher: Alchyr BaseLib Wiki, elliotttate/sts2-fmod-tools, Nexus Spire Radio 등 5개 출처 교차 검증.

**확인 실패 → 이후 해소 (2026-04-22 재검증):**
- ~~`RunState.ExtraFields` 실재 여부 (Q4a)~~ → 존재하지만 모드 필드 추가 불가 (세이브 스키마 박힘). hydrate 는 `Hook.AfterMapGenerated` 로 대체.
- ~~`runState.CurrentMapCoord` 실재 여부 (Q4c)~~ → **존재 확인** (`IRunState.CurrentMapCoord: MapCoord?` public). 추가로 `CurrentMapPoint`/`CurrentActIndex`/`Map`/`CurrentRoom` 등 전부 public.
- ~~`Hook.ModifyNextEvent` 의 currentEvent null 가능성 (Q4b)~~ → `EnsureNextEventIsValid` 선행 실행으로 non-null 보장.

**최종 검증 경로**: sts-game-analyst 가 `IRunState.cs` / `RunManager.cs` / `ActModel.cs` 원문 직접 참조로 해소. 부록 A (plan 부록이 될 수도 있으나 여기선 §5.4 표에 요약).

## 12. 관련 파일 경로

- plan: `F:\workspace\godot\TheCity\doc\plan\abnormality-battle.plan.md`
- 선행 plan: `F:\workspace\godot\TheCity\doc\plan\abnormality-map-node.md`
- 현 맵 노드 구현: `F:\workspace\godot\TheCity\src\Map\AbnormalityMapInjector.cs` (특히 `Inject()` line 47-63 결정론 해시)
- 현 맵 노드 구현: `F:\workspace\godot\TheCity\src\Map\MapPointTypePatches.cs` (`RollRoomTypeFor` Prefix — plan §6 수정 시 연동 필요)
- 모드 초기화: `F:\workspace\godot\TheCity\src\ModStart.cs`
- 환상체 디렉토리 (현 비어있음): `F:\workspace\godot\TheCity\src\Abnormality\` (.gitkeep 뿐)

## 13. 바닐라 레퍼런스 클래스 (구현 시 참고)

| 패턴 | 바닐라 클래스 |
|---|---|
| Event → Combat → Resume | `MegaCrit.Sts2.Core.Models.Events.BattlewornDummy` + `BattlewornDummyEventEncounter` |
| 다부위 (자기 자신에 MinionPower) | `TorchHeadAmalgam` |
| 다부위 (런타임 추가) | `DoormakerBoss` + `Door` + `Doormaker` |
| 단순 상태머신 (FollowUpState 체인) | `CubexConstruct` |
| 소환 미니언 (Fabricator + Guardbot) | `FabricatorNormal` encounter |
