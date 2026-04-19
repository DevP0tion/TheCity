# Card-Sin Attribution — Numeric Encoding Reference

`assets/overrides/cards/*.json` 파일의 모든 필드(단 `cardId` 제외)는 숫자로 인코딩됨.
게임 소스의 enum ordinal을 그대로 사용 → 게임 API와 직접 호환.

## Sin (`sin` 필드)

`src/Resource/Sin.cs` 정의 순서.

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

## CardType (`cardType` 필드)

`MegaCrit.Sts2.Core.Entities.Cards.CardType`

| 값 | 이름 |
|:--:|------|
| 0 | None |
| 1 | Attack |
| 2 | Skill |
| 3 | Power |
| 4 | Status |
| 5 | Curse |
| 6 | Quest |

## CardRarity (`rarity` 필드)

`MegaCrit.Sts2.Core.Entities.Cards.CardRarity`

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

## TargetType (`targetType` 필드)

`MegaCrit.Sts2.Core.Entities.Cards.TargetType`

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

## CardKeyword (`keywords` 배열 요소)

`MegaCrit.Sts2.Core.Entities.Cards.CardKeyword`

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

## energyCost (`energyCost` 필드)

숫자(int). `null`은 가변 X 코스트 또는 해당 없음(Status/Curse/Quest 일부).

---

## 완전한 파일 예시

### Bash (기본 공격)
```json
{
  "cardId": "Bash",
  "sin": 0,
  "cardType": 1,
  "rarity": 1,
  "energyCost": 2,
  "targetType": 2,
  "keywords": []
}
```

### AscendersBane (저주)
```json
{
  "cardId": "AscendersBane",
  "sin": 4,
  "cardType": 5,
  "rarity": 9,
  "energyCost": null,
  "targetType": 0,
  "keywords": [7, 2, 4]
}
```

### Whirlwind (X 코스트)
```json
{
  "cardId": "Whirlwind",
  "sin": 0,
  "cardType": 1,
  "rarity": 3,
  "energyCost": null,
  "targetType": 3,
  "keywords": []
}
```

---

## 빌드 파이프라인

```
assets/overrides/cards/*.json      (입력 — 577+ 파일)
      ↓
tools/CardSinMapGen (C# 콘솔)       (MSBuild BeforeCompile)
      ↓
Generated/CardSinMap.g.cs          (정적 Dictionary<string, Sin>)
      ↓
TheCity.dll                        (.g.cs 컴파일 포함)
```

- 증분 빌드: JSON 미변경 시 생성기 skip (MSBuild Inputs/Outputs)
- 재현성: `Generated/*.g.cs`는 `.gitignore`, 입력 JSON + 생성기 코드가 소스

## 편집 워크플로

1. `assets/overrides/cards/{CardName}.json` 열기
2. 매핑 테이블 참조하여 `sin` 값 수정 (예: `"sin": 2` = Sloth)
3. `dotnet build` 또는 IDE Build → `CardSinMap.g.cs` 자동 재생성
4. 런타임에 `CardSinMap.ById["Bash"]` 로 조회
