# Card-Sin Attribution — 브레인스토밍 리포트

**Feature:** `card-sin-attribution`
**작성:** 2026-04-19 (team-lead 통합)
**입력:** researcher / designer / architect / balancer 4개 병렬 리포트
**목표:** 모든 카드에 7대죄 속성 부여 → 플레이 시 해당 Sin 자원 +1

---

## 1. 공통 기반 (4팀 합의)

### 1.1 기술적 결론

| 항목 | 확정 사항 |
|------|----------|
| **플레이 훅** | `Hook.AfterCardPlayed(CombatState, PlayerChoiceContext, CardPlay)` |
| **훅 파일** | `MegaCrit.Sts2.Core.Hooks.Hook.cs:181` |
| **패치 방식** | Harmony Postfix (`[HarmonyPatch(typeof(Hook), nameof(Hook.AfterCardPlayed))]`) |
| **매핑 키** | `CardModel.Id` (ModelId record = Category.Entry 쌍) — 업그레이드/인챈트에 불변 |
| **저장소** | 정적 `Dictionary<ModelId, Sin>` — 코드 상수, 세이브 불필요 |
| **MP safety** | 매핑이 코드 상수 → peer 간 자동 일치. Sin 자원은 기존 `SharedResourceManager.Modify(sync:true)` 활용 |

### 1.2 공통 가드 (3가지)

```csharp
[HarmonyPatch(typeof(Hook), nameof(Hook.AfterCardPlayed))]
public static class Hook_AfterCardPlayed_Patch
{
    public static void Postfix(CardPlay cardPlay)
    {
        // ① 첫 번째 플레이만 +1 (Dupe/Clone/Whirlwind 루프 무한 증폭 방지)
        if (cardPlay.PlayIndex != 0) return;

        // ② AutoPlay(카드 효과로 공짜 플레이)는 제외 선택 가능
        // if (cardPlay.IsAutoPlay) return;

        // ③ MP: 로컬 플레이어 카드만 처리 (원격 피어 발화 시 중복 방지) — 런타임 검증 후 결정
        // if (!LocalContext.IsMe(cardPlay.Card.Owner?.Creature)) return;

        var sin = cardPlay.Card.Id.GetSin();
        sin?.Modify(+1, sync: true);
    }
}
```

### 1.3 카탈로그 규모 (balancer 조사)

| 분류 | 카드 수 |
|------|:-------:|
| 캐릭터 풀 전체 (5 × ~88) | 439 |
| 비캐릭터 (Colorless/Event/Status/Curse/Quest/Token) | 137 |
| **바닐라 총** | **576** |
| **v1 대상** (Attack + Skill + Power, playable만) | **~468** |
| v1 제외 | Status(11) / Curse(18) / Quest(3) / Unplayable 전체 |

### 1.4 공통 위험 (balancer)

| # | 리스크 | 완화 |
|---|--------|------|
| R1 | 공격 카드 편중(~33%)으로 **분노 폭주** | 매핑 분산 + Sin 소비 메커니즘 조기 설계 |
| R2 | 0코 루프(Havoc/Dupe/Cascade) **무한 증폭** | `PlayIndex == 0` 가드 (필수) |
| R3 | Status 카드(적이 준 카드) 모호성 | v1 제외, v2에서 정책 결정 |

### 1.5 7대죄 컨셉 가이드 (designer)

| Sin | 키워드 | 대표 카드 예시 |
|-----|--------|---------------|
| 분노 (Wrath) | 순수 공격, 고코스트 일격 | Strike, Bludgeon, Headbutt |
| 색욕 (Lust) | 사이클/유혹/충동적 돌진 | Anger, Reckless Charge |
| 나태 (Sloth) | 방어, 지속성, 느림 | Defend, Shrug It Off |
| 탐식 (Gluttony) | 카드 축적/획득/소모 | Hoarder, Feed, Warcry |
| 우울 (Gloom) | 자해/희생/소진 | Reaper, Offering |
| 오만 (Pride) | 파워/버프/권위 | Demon Form, Inflame |
| 질투 (Envy) | 모방/탈취/복제 | Mirror Image, 적 스킬 복사 |

---

## 2. 통합 설계안 3종

4팀의 권장이 서로 다른 축을 건드리므로, 아래 3안은 **매핑 정책 × 구현 복잡도 × 품질/시간**의 trade-off 스펙트럼을 대표합니다.

### 🔹 Option 1: MVP 규칙 자동 (빠른 가시화)

**매핑 전략:** 규칙 함수만 (designer S2 + architect A3)
**아키텍처:** `SinClassifier.Classify(CardModel) → Sin` 순수 함수 + 캐시

```
src/Resource/
├── Sin.cs                      (기존)
├── SharedResourceManager.cs    (기존)
├── CardSinRegistry.cs          (신규) — Classify(CardModel) 함수 + 캐시
└── CardPlayPatch.cs            (신규) — AfterCardPlayed Postfix
```

**규칙 로직 (예):**
- `CardType.Attack` + 기본 → 분노
- `CardType.Skill` + Block → 나태
- `CardType.Skill` + Draw 키워드 → 탐식
- `CardType.Power` → 오만
- `Exhaust` 키워드 → 우울
- `Pool.Title` (캐릭터)별 기본 색조 보정

| 평가 항목 | 값 |
|----------|---|
| 초기 개발 공수 | **S** (1-2일) |
| 매핑 커버리지 (v1) | **468/468 = 100%** (자동) |
| 테마 정확도 | **60%** (공격·방어·파워는 맞으나 엣지 20%는 어색) |
| DLC/외부모드 자동대응 | ✅ 자동 |
| 유지보수성 | **L-M** (규칙 수정 시 영향 광범, QA 필요) |
| MP Safe | ✅ |

**추천 상황:** 프로토타입/쇼케이스, 게임플레이 감각 먼저 확인하고 싶을 때.
**한계:** "Feed가 왜 분노?" 같은 어색한 매핑 정리 불가. 후속 리팩터링 비용.

---

### 🔹 Option 2: 하이브리드 (**⭐ 추천**)

**매핑 전략:** 규칙 기본값 + 수동 Override (designer S3 + architect A1/A3 혼합)
**아키텍처:** 코드 규칙 + `CardSinOverrides.cs` 정적 Dictionary

```
src/Resource/
├── CardSinRegistry.cs          (신규) — GetSin(ModelId) 진입점 + 캐시
├── CardSinRules.cs             (신규) — Classify() 규칙 함수 (Option 1과 동일)
├── CardSinOverrides.cs         (신규) — Dictionary<ModelId, Sin> 수동 테이블
└── CardPlayPatch.cs            (신규)
```

**조회 순서:**
```csharp
public static Sin? GetSin(ModelId id) {
    if (_cache.TryGetValue(id, out var s)) return s;
    var card = ModelDb.FindCard(id);
    s = CardSinOverrides.TryGet(id) ?? CardSinRules.Classify(card);
    _cache[id] = s; return s;
}
```

**초기 Override 목록:** "규칙이 분명 틀리는" 20-50장만
- Feed → 탐식 (공격인데 시체 섭취 테마)
- Corruption → 탐식 (파워인데 "Skill 공짜" = 욕심)
- Mirror Image → 질투 (복제)
- Offering → 우울 (희생)
- ...

| 평가 항목 | 값 |
|----------|---|
| 초기 개발 공수 | **M** (2-4일, Override 50장 수동 검토 포함) |
| 매핑 커버리지 (v1) | 100% (규칙 80% + override 20%) |
| 테마 정확도 | **85%** (Override로 엣지 교정) |
| DLC/외부모드 자동대응 | ✅ 규칙이 fallback |
| 유지보수성 | **M-H** (Override 추가로 점진 개선 가능) |
| MP Safe | ✅ (모든 데이터 코드 상수) |
| 추후 확장성 | 가중치 벡터(S4) 승격 경로 보유 |

**추천 상황:** 장기 유지 프로젝트, "가시화 + 테마 정확도" 양쪽 필요.
**researcher·designer·architect 3명이 이 방향을 수렴적으로 지지**.

---

### 🔹 Option 3: 큐레이션 완전 수동 (최고 품질)

**매핑 전략:** 모든 카드 1:1 수동 매핑 (designer S1)
**아키텍처:** 거대 정적 Dictionary

```
src/Resource/
├── CardSinRegistry.cs          (신규) — Dictionary<ModelId, Sin> 468 항목
└── CardPlayPatch.cs            (신규)
```

**방식:**
- `ModInit`에서 `Register("Card.Strike", Sin.Wrath); Register("Card.Feed", Sin.Gluttony); ...` 468줄
- 파일을 카테고리별로 분할 가능 (Ironclad.cs / Silent.cs / Colorless.cs ...)

| 평가 항목 | 값 |
|----------|---|
| 초기 개발 공수 | **L** (3-5일, 모든 카드 1장씩 검토) |
| 매핑 커버리지 (v1) | 100% (수동) |
| 테마 정확도 | **95%+** (장인정신) |
| DLC/외부모드 자동대응 | ❌ 빈 매핑 → Sin.None (fallback 규칙 없음) |
| 유지보수성 | **H** (신규 카드마다 한 줄씩 추가) |
| MP Safe | ✅ |
| 위험 | 누락 시 무음 no-op |

**추천 상황:** 카드 수가 확정적이고 변하지 않을 때, 테마 정확도가 최우선일 때.
**단점:** DLC/타모드 카드는 별도 기여 필요. 프로젝트가 커지면 거대 Dictionary 유지 부담.

---

## 3. 옵션 비교 요약표

| 축 | Option 1 (규칙) | Option 2 (하이브리드) ⭐ | Option 3 (수동) |
|----|:---------------:|:------------------------:|:---------------:|
| 초기 공수 | S (1-2일) | M (2-4일) | L (3-5일) |
| 테마 정확도 | 60% | 85% | 95%+ |
| DLC 자동대응 | ✅ | ✅ | ❌ |
| 유지 부담 | 중 (규칙 수정 파장) | 저 (Override 추가만) | 고 (카드마다 1줄) |
| 미래 확장성 | 낮음 | **높음 (S4 벡터로 승격)** | 낮음 |
| 첫 출시 품질 | 프로토타입급 | **출시 가능** | 장인 수준 |

---

## 4. 권장 결정

**⭐ Option 2 (하이브리드)** 를 추천합니다.

### 이유
1. **4팀 수렴**: designer가 명시적으로 S3 추천, architect의 A1+A3 fallback, researcher의 "ModelId Dictionary + fallback rule" 구조와 일치.
2. **리스크 균형**: Option 1의 "어색함"과 Option 3의 "누락 공포" 양쪽 모두 회피.
3. **점진적 품질**: v1은 규칙 80% + Override 30장으로 출시 → v1.x에서 Override를 늘려가며 정확도 상승.
4. **DLC 안전망**: 신규 카드는 규칙이 자동 커버, 수동 검토 필요한 것만 Override 추가.
5. **확장성**: 나중에 Sin 가중치 벡터(복수 Sin 카드)로 자연스럽게 승격 가능.

### 초기 Scope (v1)
- Scope: Attack + Skill + Power 중 playable 468장
- 제외: Status / Curse / Quest / Unplayable
- Override 초기 30-50장 (규칙이 분명 틀리는 경우만)
- Sin 획득: `PlayIndex == 0` 가드, `IsAutoPlay` 포함 (정책은 Plan에서 확정)

### Plan 단계에서 확정 필요한 것
- [ ] `IsAutoPlay` 카드도 Sin +1 주는지 (YES: 자원 빠른 축적 / NO: 플레이어 액션만 인정)
- [ ] 업그레이드 카드는 별도 Sin 갖는지 (보통은 동일 ModelId라 자동 동일)
- [ ] MP에서 원격 피어 플레이가 로컬 Hook 발화하는지 (런타임 검증 → LocalContext 가드 필요 여부 결정)
- [ ] Override JSON 외부 파일 vs C# 정적 테이블 (architect는 C#, designer는 JSON 제안)
- [ ] v1 스타터 override 30-50장 목록 확정

---

## 5. 다음 단계

1. **사용자 선택**: Option 1/2/3 중 확정
2. **Plan 단계 진입**: `/pdca plan card-sin-attribution`
3. Plan 문서에서:
   - 위 "확정 필요" 항목 해결
   - Success Criteria 정의 (예: 바닐라 카드 100% 매핑 커버, Override ≤ 100장, 분노 평균 축적 속도 ≤ X)
   - L1-L7 Verification 매트릭스

---

*이 리포트는 4명 teammate 병렬 리서치 + team-lead 통합의 산물. 팀은 작업 완료 후 모두 idle 상태.*
