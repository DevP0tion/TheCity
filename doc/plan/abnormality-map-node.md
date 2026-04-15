# 환상체(Abnormality) 커스텀 맵 노드

## 상태: ❌ 미구현

## 개요
맵에 "환상체"라는 새로운 노드 타입을 추가.
BaseLib `[CustomEnum]`으로 `MapPointType`에 새 값을 주입하고,
게임 내 switch문을 Harmony 패치로 보강.

## 접근법: `[CustomEnum]` + Harmony Prefix 패치

### enum 주입
```csharp
using BaseLib.Utilities.Enums; // 정확한 네임스페이스 확인 필요

public static class AbnormalityMapPointType
{
    [CustomEnum]
    public static MapPointType Abnormality;
}
```

### 패치 필요 지점 (6곳)

#### RunManager — MapPointType → RoomType 변환
- 파일: `MegaCrit.Sts2.Core.Runs.RunManager`
- 위치: line 728~737 (switch expression)
- default: `throw new ArgumentOutOfRangeException`
- 패치: Prefix에서 Abnormality일 때 `RoomType.Event` 반환 + `__result` 설정 + `return false`

#### NNormalMapPoint.IconName — 맵 아이콘
- 파일: `MegaCrit.Sts2.Core.Nodes.Screens.Map.NNormalMapPoint`
- 위치: line 145~156 (switch expression)
- default: `throw new ArgumentOutOfRangeException`
- 패치: Abnormality일 때 커스텀 아이콘명 반환 (예: `"map_abnormality"`)

#### NTopBarRoomIcon — 상단바 방 이름
- 파일: `MegaCrit.sts2.Core.Nodes.TopBar.NTopBarRoomIcon`
- 위치: line 116~124 (switch expression)
- 패치: Abnormality일 때 `"ROOM_ABNORMALITY"` 로컬라이제이션 키 반환

#### NMapPointHistoryHoverTip — 방문 기록 호버팁
- 파일: `MegaCrit.Sts2.Core.Nodes.HoverTips.NMapPointHistoryHoverTip`
- 위치: line 160~168 (switch expression)
- 패치: Abnormality일 때 `"ROOM_ABNORMALITY"` 반환

#### ImageHelper.GetRoomIconSuffix — 아이콘 경로
- 파일: `MegaCrit.Sts2.Core.Helpers.ImageHelper`
- 위치: line 47~
- 패치: Abnormality 아이콘 경로 반환

#### ActMap — 맵 생성 시 배치 규칙
- 훅: `ModifyGeneratedMap` (유물/파워에서 오버라이드)
- 방법: 생성된 맵의 일부 노드를 `Abnormality`로 교체

### 맵 주입 전략
```csharp
// ModifyGeneratedMap 훅에서
// 특정 조건의 노드(예: Unknown/Event)를 Abnormality로 교체
public override ActMap ModifyGeneratedMap(IRunState runState, ActMap map, int actIndex)
{
    foreach (var point in map.AllPoints)
    {
        if (ShouldBeAbnormality(point, runState))
        {
            point.PointType = AbnormalityMapPointType.Abnormality;
        }
    }
    return map;
}
```

### 방 진입 시 동작
`MapPointType.Abnormality` → `RoomType.Event`로 매핑.
실제 방 내용은 동적 이벤트 시스템(doc/dynamic-event-design.md)과 연동하여 결정.

### 필요 에셋
- `map_abnormality.tres` — 맵 아이콘 텍스처
- `map_abnormality_outline.tres` — 아이콘 아웃라인

### 파일 구조 (구현 시)
```
src/Map/
├── AbnormalityMapPointType.cs   # [CustomEnum] 정의
├── MapPointTypePatches.cs       # 6개 Harmony Prefix 패치
└── AbnormalityMapInjector.cs    # ModifyGeneratedMap 훅 구현 (유물 또는 별도 모델)
```

### 로컬라이제이션 추가 (thecity.json)
```json
{
  "ROOM_ABNORMALITY": "Abnormality",     // eng
  "ROOM_ABNORMALITY": "환상체"            // kor
}
```

### 리스크
- `[CustomEnum]`이 `MapPointType`에 대해 동작하는지 미검증 (BaseLib 문서에 명시된 지원 타입: CardKeyword, CardPile)
- 게임 업데이트 시 switch문 위치 변경 가능 → 패치 깨짐
- 멀티플레이어에서 모든 클라이언트가 동일 모드 필요 (enum 값 불일치 시 크래시)
- 세이브 호환성: 환상체 노드가 포함된 세이브를 모드 없이 로드 시 크래시 가능

### MCP에서 추가 확인 필요
```
# [CustomEnum] 실제 구현 확인
search_game_code: "CustomEnum|CustomEnumAttribute"
get_entity_source: "CustomEnumAttribute"

# ActMap.AllPoints 접근
get_entity_source: "StandardActMap"

# ModifyGeneratedMap 훅 시그니처
search_game_code: "ModifyGeneratedMap"
```
