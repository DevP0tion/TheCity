# Plan: card-sin-attribution

**Feature:** card-sin-attribution
**상태:** Plan 작성 (Do 일부 진행 중 — 스키마 + 생성기 + Hook 구현 완료)
**작성:** 2026-04-20
**입력:** 4팀 병렬 리서치 (researcher / designer / architect / balancer) → 부록 A 요약
**인코딩 레퍼런스:** 부록 B

## Executive Summary

| 관점 | 요약 |
|------|------|
| **Problem** | TheCity 모드의 7대죄 자원 시스템이 UI만 있고 획득 메커니즘 없음. 전투 내내 모든 Sin이 0으로 고정 — 시스템 자체가 작동하지 않음. |
| **Solution** | 모든 바닐라 카드에 Sin 속성 매핑(JSON 기반) + 카드 플레이 시 해당 Sin +1 획득 (Harmony `Hook.AfterCardPlayed` Postfix). |
| **Function UX Effect** | 플레이어가 덱 구성과 카드 선택을 통해 7대죄 자원을 전략적으로 축적. 전투 중 HUD(`SinStackPanel`)로 실시간 확인. |
| **Core Value** | 데크 빌딩 기반 자원 관리 레이어 추가 — 기존 STS2 플레이 위에 "카드 플레이 = 특정 Sin 획득" 축을 얹어 림버스 컴퍼니 스타일의 자원 게임플레이 도입. |

## Context Anchor

| 축 | 내용 |
|----|------|
| **WHY** | UI만 있고 실제 자원 획득이 안 되는 "빈 HUD" 상태 해소. 모드의 핵심 게임플레이 축 하나 완성. |
| **WHO** | TheCity 모드 사용자 (Slay the Spire 2 + 림버스 컴퍼니 팬). 싱글 + 멀티 양쪽 지원. |
| **RISK** | (1) MP 동기화 중복/desync  (2) 리플레이 루프 무한 증폭  (3) 공격 카드 편중으로 분노 폭주 |
| **SUCCESS** | 모든 바닐라 카드 매핑 커버리지 100%, 플레이 시 정확히 +1, PlayIndex==0 가드로 무한 증폭 방지, MP 양측 동일 값 |
| **SCOPE** | v1: 바닐라 577장 매핑 + Hook +1 로직. **제외**: Sin 소비 메커니즘, 특수 효과 Sin(분노 5 시 보너스 등), DLC 카드 |

## 1. 목표

### 1.1 무엇을 만드는가
1. **카드 속성 데이터** — 577장 각 바닐라 카드에 Sin 매핑 (JSON + 빌드 시 C# 생성)
2. **플레이 훅** — `Hook.AfterCardPlayed` Postfix로 +1 획득 로직
3. **런타임 레지스트리** — 소유권 이전 패턴으로 생성 데이터 → 런타임 dict 이관

### 1.2 이 Plan에서 제외되는 것 (NON-GOALS)
- ~~Sin 소비/사용 메커니즘~~ — 별도 Plan
- ~~특수 효과 (분노 10 시 공격 +2 같은 시너지)~~ — 별도 Plan
- ~~DLC/모드 카드 자동 매핑 규칙~~ — v2
- ~~색욕/질투 수동 재배치 완료~~ — 별도 작업 (초기 Sin 분포 밸런싱)

## 2. 요구사항

### 2.1 기능 요구사항 (Functional)

| ID | 요구사항 | 우선순위 |
|----|----------|:-------:|
| F1 | 모든 바닐라 카드가 Sin 속성 하나를 가진다 | P0 |
| F2 | 플레이 가능 카드 플레이 시 해당 Sin이 정확히 +1 증가한다 | P0 |
| F3 | 같은 카드가 여러 번 플레이되는 루프(Dupe/Havoc/Whirlwind)에서 첫 플레이만 +1 | P0 |
| F4 | 멀티플레이어 양측 peer에서 Sin 값이 동일하게 유지된다 | P0 |
| F5 | 상태/저주 카드 등 unplayable 카드는 자연히 +1 안 됨 (Hook 미발화) | P1 |
| F6 | JSON 편집 후 dotnet build 1회로 새 매핑 반영 | P1 |
| F7 | 미등록 카드(DLC/외부 mod)는 에러 없이 조용히 skip | P2 |

### 2.2 비기능 요구사항 (Non-Functional)

| ID | 요구사항 | 기준 |
|----|----------|------|
| N1 | 런타임 조회 비용 | O(1) Dictionary 룩업 |
| N2 | 빌드 시간 증가 | 전체 dotnet build에 +1초 이하 |
| N3 | 메모리 | CardSinMap 정적 슬롯 ModInit 후 해제 (GC 대상) |
| N4 | 멀티플레이어 안전성 | 코드 상수 매핑 + SharedResourceManager.sync=true |
| N5 | 카드 578장째 추가 시 유지비 | JSON 1개 추가 + dotnet build → O(1) 작업 |

## 3. Success Criteria (SC)

| SC | 검증 방법 | 통과 기준 |
|----|----------|----------|
| **SC1** 매핑 커버리지 | `CardSinRegistry.Count` 로그 | `= 577` (F1) |
| **SC2** 플레이 시 +1 | 전투 내 카드 플레이 후 `Sin.Get()` | 플레이 전후 차이 = 1 (F2) |
| **SC3** 리플레이 무한 증폭 방지 | Havoc 또는 Dupe 카드로 Strike 3회 복제 플레이 | 분노 +1만 (F3) |
| **SC4** MP 동일 값 | 2 peer 동시 플레이 후 양측 SharedResource 비교 | 양측 일치 (F4) |
| **SC5** Unplayable 자동 skip | Burn/Dazed 카드 덱에 추가 후 턴 진행 | +1 발생 안 함 (F5) |
| **SC6** JSON 편집 반영 | `assets/overrides/cards/Strike.json` sin 수정 → 빌드 | 새 값 반영 (F6) |
| **SC7** 미등록 카드 안전 | 커스텀 카드 ID로 플레이 시 예외 미발생 | 조용히 skip, 로그 경고 (F7) |
| **SC8** 빌드 오버헤드 | `time dotnet build` JSON 미변경 시 | pre-compile 스텝 < 1초 (N2) |
| **SC9** 메모리 해제 | `CardSinMap.IsReleased` | `== true` after ModInit (N3) |

## 4. 아키텍처 결정

### 4.1 선택된 옵션 (브레인스토밍 Option 2 하이브리드)

4-teammate 병렬 리서치 결과 수렴적 추천을 그대로 채택:

| 영역 | 결정 | 근거 |
|------|------|------|
| 매핑 전략 | **하이브리드** (규칙 기본 + 수동 override) | designer S3, 576장 커버 + DLC 자동 대응 |
| 저장 구조 | **JSON per card** (577 files) | architect A1+A2 혼합 — git-diff 친화, 카드별 독립 편집 |
| 식별자 | **`CardModel.Id.Entry`** (string) | researcher — ModelId record, 업그레이드/인챈트에 불변 |
| 플레이 훅 | **`Hook.AfterCardPlayed` Postfix** | researcher + architect, 이미 확정 |
| 빌드 파이프라인 | **MSBuild + C# 생성기 (`tools/CardSinMapGen`)** | user 선택 (Source Generator 대신 단순 스크립트) |
| 메모리 관리 | **소유권 이전 (`TakeOwnership` + null 해제)** | user 요청, CardSinMap static slot 해제 |

### 4.2 데이터 흐름 다이어그램

```
┌─────────────────────────────────────────────────┐
│ Build Time                                       │
│                                                  │
│  assets/overrides/cards/*.json (577 files)       │
│         ↓                                        │
│  tools/CardSinMapGen (MSBuild BeforeCompile)     │
│         ↓                                        │
│  Generated/CardSinMap.g.cs                       │
│     (private static Dictionary<string, Sin>)     │
└─────────────────────────────────────────────────┘
                  ↓ (컴파일)
┌─────────────────────────────────────────────────┐
│ Runtime                                          │
│                                                  │
│  ModStart.ModInit()                              │
│    └─ CardSinRegistry.LoadOnce()                 │
│         └─ CardSinMap.TakeOwnership()            │
│            → CardSinMap._data = null (GC 가능)   │
│            → CardSinRegistry._byCardId = Dict    │
│                                                  │
│  카드 플레이 → Hook.AfterCardPlayed              │
│    └─ Hook_AfterCardPlayed_Patch.Postfix         │
│         ├─ PlayIndex != 0 → skip                 │
│         ├─ !LocalContext.IsMe(owner) → skip      │
│         ├─ CardSinRegistry.GetSin(card) 조회     │
│         └─ sin.Modify(+1, sync: true)            │
│              └─ SharedResourceManager            │
│                 + MP sync (SharedResourceSync)   │
│                 + ValueChanged event → HUD 갱신  │
└─────────────────────────────────────────────────┘
```

## 5. 파일 구조 (현재 + 신규)

```
assets/overrides/cards/           ← 577 JSON 파일 (신규, commit됨)
    Abrasive.json
    ... (575 files)
    Zap.json

Generated/                        ← 빌드 산출물 (git commit)
    CardSinMap.g.cs

tools/                            ← 빌드 도구 (git commit)
    CardSinMapGen/
        CardSinMapGen.csproj
        Program.cs

src/Resource/
    CardPlayPatch.cs              ← 신규, Hook.AfterCardPlayed Postfix
    CardSinRegistry.cs            ← 신규, 런타임 레지스트리 (TakeOwnership)
    CardFields.cs                 (기존 — CityValue, 수정 없음)
    Sin.cs                        (기존, 수정 없음)
    SharedResourceManager.cs      (기존, 수정 없음)
    ...

src/ModStart.cs                   ← CardSinRegistry.LoadOnce() 호출 추가

TheCity.csproj                    ← MSBuild 타겟 (BuildCardSinMapTool + GenerateCardSinMap)
.gitignore                        ← .tmp/ 추가
export_presets.cfg                ← tools/Generated/.tmp 등 PCK 제외
```

## 6. 가드 로직 명세

```csharp
public static void Postfix(CardPlay cardPlay)
{
    // G1: 리플레이 루프 첫 플레이만 카운트
    if (cardPlay.PlayIndex != 0) return;

    // G2: MP - 로컬 플레이어 카드만 처리
    var owner = cardPlay.Card.Owner;
    if (owner != null && !LocalContext.IsMe(owner)) return;

    // G3: 미등록 카드 조용히 skip (외부 mod / 신규 카드)
    var sin = cardPlay.Card.GetSin();
    if (sin == null) return;

    // 적용
    sin.Value.Modify(+1, sync: true);
}
```

### 각 가드의 근거

| Guard | 근거 | 제거 시 영향 |
|-------|------|-------------|
| G1 | researcher 권장. `CardPlay.PlayCount > 1` 시 매번 발화 | Havoc로 Strike 3회 복제 → 분노 +3 (의도 아님) |
| G2 | MP에서 원격 피어 play가 로컬 Hook 발화하는지 미검증 | 중복 +1 → peer 간 값 불일치 가능성 |
| G3 | DLC/외부 mod 카드 대응 | NullReferenceException → ModInit 실패 위험 |

## 7. 위험 레지스터

| # | 위험 | 가능성 | 영향 | 완화 |
|---|------|:---:|:---:|------|
| R1 | `Hook.AfterCardPlayed` static 메서드가 HarmonyX patch 가능 여부 | L | H | BaseLib/HarmonyX 지원 확인 필요. 실패 시 `CardModel.OnPlayWrapper` Postfix로 대체 |
| R2 | MP에서 원격 피어 플레이가 로컬 훅 발화 여부 | M | M | G2 `LocalContext.IsMe` 가드로 선제 대응. 런타임 검증 후 필요 시 가드 유지/제거 결정 |
| R3 | `IsAutoPlay` 카드(자동 효과) 포함 시 밸런스 영향 | M | M | v1은 포함, 테스트 후 밸런스 판단 |
| R4 | 공격 카드 편중으로 분노 폭주 | H | M | 디자이너 수동 재배치 (Lust/Envy 보강) 선행 필요 |
| R5 | 업그레이드 카드가 동일 `ModelId.Entry` 가지는지 | L | L | ModelId는 base ID만 가짐 (Entry = "Bash"), 업그레이드 여부는 별도 필드 |
| R6 | 빌드 생성기가 `dotnet` 경로 미발견 | L | L | MSBuild `dotnet` 명령은 SDK 표준. CI 환경에서도 존재 |
| R7 | JSON 오타/잘못된 enum 값 | L | L | 생성기가 파싱 시 throw → 빌드 실패로 즉시 노출 |

## 8. 테스트 계획 (L1-L7)

| Level | 목적 | 방법 | SC |
|-------|------|------|:--:|
| **L0** | 정적 | `dotnet build` 성공 + CardSinMap.g.cs 생성 확인 | SC6, SC8 |
| **L1** | 부팅 | 게임 시작 후 로그 확인 — `CardSinRegistry: 577 entries loaded; CardSinMap released` | SC1, SC9 |
| **L2** | 단일 플레이 | 전투 진입 → Strike 1회 플레이 → `Sin.Wrath.Get()` 조회 | SC2 |
| **L3** | 리플레이 루프 | Havoc 또는 Dupe로 Strike 3회 복제 → 분노 값 확인 | SC3 |
| **L4** | Unplayable | Burn/Dazed 카드 덱 추가 후 턴 진행 → Sin 변화 없음 | SC5 |
| **L5** | JSON 편집 반영 | Strike.json `sin: 0` → `sin: 5` 변경 → 빌드 → L2 | SC6 |
| **L6** | 미등록 카드 | 존재하지 않는 카드 ID 플레이 시도 (커맨드) → 예외 없음 | SC7 |
| **L7** | MP | 2 peer 동시 전투 → 양측 값 비교 | SC4 |

**실행 도구**: bridge MCP (`bridge_start_run`, `bridge_play_card`, `bridge_get_full_state`, `bridge_get_exceptions`).

## 9. 구현 상태 (이미 진행된 Do)

사용자의 단계별 요구에 따라 다음이 Plan 확정 전에 선행됨:

- [x] 4-teammate 브레인스토밍 (→ 부록 A)
- [x] 577 JSON 파일 스캐폴드 (숫자 인코딩 스키마)
- [x] 인코딩 레퍼런스 (→ 부록 B)
- [x] `tools/CardSinMapGen/` C# 빌드 도구
- [x] `Generated/CardSinMap.g.cs` 자동 생성 파이프라인
- [x] `src/Resource/CardSinRegistry.cs` 런타임 레지스트리 (소유권 이전)
- [x] `src/Resource/CardPlayPatch.cs` Hook.AfterCardPlayed Postfix
- [x] `ModStart.ModInit`에 `CardSinRegistry.LoadOnce()` 호출 추가
- [x] 빌드 검증 (경고 0 / 오류 0)

## 10. 남은 작업 (Do → Check → Act)

### 10.1 Do 잔여
- [ ] (선택) 색욕/질투 보강을 위한 수동 JSON 재배치 라운드
  - Lust 후보: 사이클/유혹 Attack (Anger, Reckless Charge, Flurry Of Blows, Whirlwind)
  - Envy 후보: 복제/스틸/모방 (Mirror Image, Apparition, Discovery, Secret Weapon)

### 10.2 Check
- [ ] L0-L6 런타임 검증 (싱글 플레이)
- [ ] L7 MP 검증 (가능 시)
- [ ] gap-detector로 Plan vs 구현 비교 → matchRate ≥ 90% 확인

### 10.3 Act
- [ ] matchRate < 90% 이면 pdca-iterator 자동 개선
- [ ] (선택) 생성기에 enum 검증 강화 (정수 범위 벗어나면 빌드 실패)

## 11. Plan 단계에서 답 필요한 미해결 항목

| # | 질문 | 현재 답 / 결정 필요 |
|---|------|--------------------|
| Q1 | `IsAutoPlay` 카드도 +1? | 현재 YES (제외 로직 없음). 밸런스 테스트 후 재검토 |
| Q2 | MP에서 `LocalContext.IsMe` 가드가 실제로 작동하는지 | 런타임 검증 필요 (L7) |
| Q3 | Sin 상한 (예: 최대 99) | 없음. SharedResourceManager는 int overflow까지 허용 |
| Q4 | 다른 캐릭터 (Necrobinder/Regent) 시작 덱 Sin 편향 | 수동 재배치 시 캐릭터별 균형 확인 |
| Q5 | 업그레이드 카드 동일 Sin? | 거의 확실 YES — 같은 `ModelId.Entry` 공유. L5 테스트로 검증 |

## 12. 참고 자료

- **브레인스토밍 요약**: 부록 A
- **인코딩 레퍼런스**: 부록 B
- **관련 설계 (이전 기능)**: `doc/plan/abnormality-map-node.md` (Map 기능)
- **게임 소스**:
  - `Hook.AfterCardPlayed` — `MegaCrit.Sts2.Core.Hooks.Hook:181`
  - `CardPlay` — `MegaCrit.Sts2.Core.Entities.Cards.CardPlay`
  - `CardModel.Id` — `MegaCrit.Sts2.Core.Models.CardModel`

---

*이 Plan은 /pdca plan으로 작성되었으나, 브레인스토밍 + Do 선행 작업이 이미 진행됐으므로 Plan-Do가 일부 중첩. Check 단계로 진입 시 gap-detector가 이 문서 §3 SC 기준으로 구현을 검증.*

---

## 부록 A — 브레인스토밍 요약

> 2026-04-19 team-lead 통합. researcher / designer / architect / balancer 4개 병렬 리포트.
> 원본: `card-sin-attribution-brainstorm.md` (2026-04-21 이 문서에 흡수·삭제).

### A.1 4팀 공통 기반

| 항목 | 확정 사항 |
|------|----------|
| 플레이 훅 | `Hook.AfterCardPlayed(CombatState, PlayerChoiceContext, CardPlay)` (`Hook.cs:181`) |
| 패치 방식 | Harmony Postfix |
| 매핑 키 | `CardModel.Id.Entry` (ModelId record) — 업그레이드/인챈트에 불변 |
| 저장소 | 정적 `Dictionary<string, Sin>` (코드 상수, 세이브 불필요) |
| MP safety | 매핑은 코드 상수 → peer 자동 일치. 값 동기화는 `SharedResourceManager.Modify(sync:true)` |

### A.2 7대죄 컨셉 가이드 (designer)

| Sin | 키워드 | 대표 카드 |
|-----|--------|----------|
| 분노 (Wrath) | 순수 공격, 고코스트 일격 | Strike, Bludgeon, Headbutt |
| 색욕 (Lust) | 사이클/유혹/충동 돌진 | Anger, Reckless Charge |
| 나태 (Sloth) | 방어, 지속성, 느림 | Defend, Shrug It Off |
| 탐식 (Gluttony) | 카드 축적/획득/소모 | Hoarder, Feed, Warcry |
| 우울 (Gloom) | 자해/희생/소진 | Reaper, Offering |
| 오만 (Pride) | 파워/버프/권위 | Demon Form, Inflame |
| 질투 (Envy) | 모방/탈취/복제 | Mirror Image, 적 스킬 복사 |

### A.3 고려된 3개 옵션

| 축 | Option 1 (규칙 자동) | **Option 2 하이브리드** ⭐ | Option 3 (완전 수동) |
|----|:---:|:---:|:---:|
| 초기 공수 | S (1-2일) | M (2-4일) | L (3-5일) |
| 테마 정확도 | 60% | 85% | 95%+ |
| DLC 자동 대응 | ✅ | ✅ | ❌ |
| 유지 부담 | 중 | 저 | 고 |
| 확장성 | 낮음 | 높음 (Sin 벡터로 승격) | 낮음 |

### A.4 Option 2 선택 이유

1. **4팀 수렴**: designer S3, architect A1+A3, researcher 모두 동일 방향.
2. **리스크 균형**: Option 1 "어색함" + Option 3 "누락 공포" 양쪽 회피.
3. **점진 품질**: 규칙 80% + Override로 v1 출시 → Override 늘려 정확도 상승.
4. **DLC 안전망**: 신규 카드는 규칙 fallback, 수동 검토만 Override.
5. **확장성**: 추후 Sin 가중치 벡터(복수 Sin 카드)로 자연 승격.

> 실제 구현은 "JSON per card + 생성기" 변형으로 진화 (brainstorm 시점의 C# Override → 빌드 시 JSON 파싱). 아키텍처 결정은 §4.1 참조.

---

## 부록 B — JSON 인코딩 레퍼런스

> `assets/overrides/cards/*.json`의 모든 필드(단 `cardId` 제외)는 숫자 인코딩.
> 게임 소스의 enum ordinal을 그대로 사용 → 게임 API 직접 호환.
> 원본: `card-sin-attribution-encoding.md` (2026-04-21 이 문서에 흡수·삭제).

### B.1 Sin (`sin`) — `src/Resource/Sin.cs`

| 값 | Sin | 한국어 |
|:--:|-----|--------|
| 0 | Wrath | 분노 |
| 1 | Lust | 색욕 |
| 2 | Sloth | 나태 |
| 3 | Gluttony | 탐식 |
| 4 | Gloom | 우울 |
| 5 | Pride | 오만 |
| 6 | Envy | 질투 |

`sin` 필드가 누락/null이면 빌드 시 `FNV-1a(cardId) % 7` 결정론적 할당.

### B.2 CardType (`cardType`)

| 값 | 이름 |
|:--:|------|
| 0 | None |
| 1 | Attack |
| 2 | Skill |
| 3 | Power |
| 4 | Status |
| 5 | Curse |
| 6 | Quest |

### B.3 CardRarity (`rarity`)

| 값 | 이름 |
|:--:|------|
| 0 | None |
| 1 | Basic |
| 2 | Common |
| 3 | Uncommon |
| 4 | Rare |
| 5 | Ancient |
| 6 | Event |
| 7 | Token |
| 8 | Status |
| 9 | Curse |
| 10 | Quest |

### B.4 TargetType (`targetType`)

| 값 | 이름 |
|:--:|------|
| 0 | None |
| 1 | Self |
| 2 | AnyEnemy |
| 3 | AllEnemies |
| 4 | RandomEnemy |
| 5 | AnyPlayer |
| 6 | AnyAlly |
| 7 | AllAllies |
| 8 | TargetedNoCreature |
| 9 | Osty |

### B.5 CardKeyword (`keywords[]`)

| 값 | 이름 |
|:--:|------|
| 0 | None |
| 1 | Exhaust |
| 2 | Ethereal |
| 3 | Innate |
| 4 | Unplayable |
| 5 | Retain |
| 6 | Sly |
| 7 | Eternal |

### B.6 `energyCost`

숫자(int). `null`은 가변 X 코스트 또는 해당 없음(Status/Curse/Quest 일부).

### B.7 파일 예시

```json
// Bash (기본 공격)
{ "cardId": "Bash", "sin": 0, "cardType": 1, "rarity": 1, "energyCost": 2, "targetType": 2, "keywords": [] }

// AscendersBane (저주)
{ "cardId": "AscendersBane", "sin": 4, "cardType": 5, "rarity": 9, "energyCost": null, "targetType": 0, "keywords": [7, 2, 4] }

// Whirlwind (X 코스트)
{ "cardId": "Whirlwind", "sin": 0, "cardType": 1, "rarity": 3, "energyCost": null, "targetType": 3, "keywords": [] }
```

### B.8 편집 워크플로

1. `assets/overrides/cards/{CardName}.json` 열기
2. 위 매핑 테이블 참조하여 필드 수정 (예: `"sin": 2` = Sloth)
3. `dotnet build` → `Generated/CardSinMap.g.cs` 자동 재생성
4. 런타임 조회: `CardSinRegistry.GetSin(modelId)` (소유권 이전 후)
