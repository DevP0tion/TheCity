# 환상체(Abnormality) 커스텀 맵 노드

## 상태: 구현 중 (M1/M2/M3)

## 개요
맵에 "환상체(Abnormality)"라는 새로운 노드 타입을 추가.
`MapPointType` enum에 직접 새 값을 주입할 수 없으므로 sentinel 정수값 200으로 캐스팅하고,
`SerializableMapPoint`의 8-bit 직렬화(0~255) 범위 안에서 안전하게 동작.
게임 코드의 switch 지점들을 Harmony Prefix로 단락 처리하여 enum 미정의 경로를 우회.

## 접근법: `(MapPointType)200` 캐스팅 + Harmony Prefix/Postfix

### 왜 `[CustomEnum]`이 아닌가
- BaseLib(Alchyr.Sts2.BaseLib) 인덱스 전체 검색 결과 `CustomEnum`, `EnumPatch`, `InjectEnum`, `ExtendEnum` 어느 것도 존재하지 않음
- BaseLib 문서상 namespace `BaseLib.Utilities.Enums`도 존재하지 않음 (실제 namespace는 `BaseLib.Utils`, `BaseLib.Abstracts` 등)
- 런타임 enum 주입 없이 sentinel 상수로 처리하는 편이 단순하고 직렬화 안전

### Sentinel 정의
```csharp
using MegaCrit.Sts2.Core.Map;
namespace TheCity.Map;

public static class AbnormalityMapPointType
{
    // SerializableMapPoint.Serialize는 writer.WriteInt((int)PointType, 8)로 직렬화 — 최대값 255
    // 현재 enum은 0~8까지 사용 (Unassigned=0 ... Ancient=8) — 200은 충분한 여유
    public const MapPointType Abnormality = (MapPointType)200;

    public static void EnsureLoaded() { _ = Abnormality; } // 심볼 참조 유지용
}
```

## 패치 지점 (검증 완료)

MCP로 실제 게임 코드를 확인하여 원안의 오류 수정:

### 1. `RunManager.RollRoomTypeFor` — MapPointType → RoomType 변환 (Prefix)
- 파일: `MegaCrit.Sts2.Core.Runs.RunManager`
- **실제 메서드**: `private RoomType RollRoomTypeFor(MapPointType pointType, IEnumerable<RoomType> blacklist)`
- default 분기: `throw new ArgumentOutOfRangeException("pointType", pointType, null)` → Prefix `return false`로 단락 안전
- 패치: Abnormality → `RoomType.Event` (일반 이벤트 풀에서 이벤트 선택됨)

### 2. `NNormalMapPoint.IconName` — 맵 아이콘 키 (Prefix)
- 파일: `MegaCrit.Sts2.Core.Nodes.Screens.Map.NNormalMapPoint`
- **실제 메서드**: `private static string IconName(MapPointType pointType)`
- default: `throw new ArgumentOutOfRangeException(pointType.ToString())` → Prefix 안전
- 패치: Abnormality → `"map_abnormality"` 문자열 반환

### 3. `NTopBarRoomIcon.GetHoverTipPrefixForRoomType` — 상단바 방 이름 (Prefix)
- 파일: `MegaCrit.sts2.Core.Nodes.TopBar.NTopBarRoomIcon`
  - **주의: namespace가 소문자 `sts2`로 되어있음** (게임측 표기 오타)
- **실제 메서드**: `private string GetHoverTipPrefixForRoomType()` (무인자, 내부 `GetCurrentMapPointType()` 호출)
- 반환값: 로컬라이제이션 **prefix** (예: `"ROOM_ANCIENT"`), 하위에서 `PREFIX.title`/`PREFIX.description` 조합
- 패치: `Traverse`로 private `GetCurrentMapPointType()` 호출 → Abnormality 시 `"ROOM_ABNORMALITY"` 반환
- **로컬라이제이션 키는 `static_hover_tips` 테이블 필요** (game 쪽 LocString 테이블) — 다음 항목 참조

### 4. `NMapPointHistoryHoverTip._Ready` — 방문 기록 호버팁 (Postfix, 원안의 Prefix 불가)
- 파일: `MegaCrit.Sts2.Core.Nodes.HoverTips.NMapPointHistoryHoverTip`
- **원안 오류**: `_Ready` 내부의 inline switch는 default가 `_ => null` (throw 아님)
- 따라서 Prefix로 단락하면 `_Ready`의 나머지 초기화(라벨 바인딩, 이벤트 연결)가 스킵되어 크래시
- **실제 패치**: Postfix로 실행 후 `_entry.MapPointType == Abnormality` 시 `_roomStats.Text`를 직접 덮어씀
- 필요 private 필드: `_entry` (MapPointHistoryEntry), `_roomStats` (RichTextLabel) — `Traverse`로 접근

### 5. `ImageHelper.GetRoomIconPath` / `GetRoomIconOutlinePath` — 아이콘 파일 경로 (Prefix)
- 파일: `MegaCrit.Sts2.Core.Helpers.ImageHelper`
- **원안 오류**: `GetRoomIconSuffix`는 MapPointType switch가 아님 (Unknown만 특수 처리, 나머지는 `roomType.ToString()` 슬러그 사용)
- **실제 패치 대상**: `public static string? GetRoomIconPath(MapPointType, RoomType, ModelId?)`, `GetRoomIconOutlinePath(...)`
- Abnormality → `"res://assets/images/map/map_abnormality.tres"` 등 반환
- `ResourceLoader.Exists()` 체크해서 없으면 fallback (그러면 vanilla 아이콘 사용)

### 6. `Hook.ModifyGeneratedMap` — 맵 생성 후 노드 주입 (Postfix, 원안의 AbstractModel 훅 대체)
- **원안**: 유물/파워에서 `ModifyGeneratedMap` override → 보이지 않는 영구 relic 등록 필요
- **실제 채택**: `Hook.ModifyGeneratedMap` 자체를 Harmony Postfix — 등록 보일러플레이트 제거
- 시그니처: `public static ActMap ModifyGeneratedMap(IRunState runState, ActMap map, int actIndex)`
- Postfix: `AbnormalityMapInjector.Inject(runState, __result, actIndex)` 호출하여 결과 맵 반환
- `Hook.ModifyGeneratedMap`만 패치 (Late는 loaded save의 재주입 방지를 위해 제외)

## 맵 주입 전략

### 결정론적 배치 (멀티플레이어 안전)
```csharp
public static ActMap Inject(IRunState runState, ActMap map, int actIndex)
{
    if (!TheCityConfig.EnableAbnormalityNodes) return map;
    if (!AbnormalityPreflight.Healthy) return map;

    // 이미 주입된 경우 멱등 (ModifyGeneratedMap이 여러 번 호출될 수 있음)
    if (map.GetAllMapPoints().Any(p => p.PointType == AbnormalityMapPointType.Abnormality))
        return map;

    var events = map.GetAllMapPoints()
                    .Where(p => p.PointType == MapPointType.Unknown && p.CanBeModified)
                    .ToList();
    if (events.Count == 0) return map;

    // 결정론적 선택: runState.Rng.Seed + actIndex 해시
    ulong seed = (ulong)runState.Rng.Seed;
    int hash = (int)(seed ^ ((ulong)actIndex * 0x9E3779B1UL));
    int idx = ((hash % events.Count) + events.Count) % events.Count;
    var point = events[idx];
    point.PointType = AbnormalityMapPointType.Abnormality;
    point.CanBeModified = false;  // 후속 Late 패스에서 변경 방지
    return map;
}
```

### 배치 정책 v1
- 각 액트당 **정확히 1개** Abnormality 노드 주입
- 교체 대상: `MapPointType.Unknown`(이벤트/랜덤 룸) 중 `CanBeModified == true`인 노드
- 결정론: `runState.Rng.Seed ^ (actIndex * 0x9E3779B1)`만 사용, 외부 RNG 금지
- 수정 불가 노드가 없으면 silent skip

### 방 진입 시 동작
- `MapPointType.Abnormality → RoomType.Event` 매핑
- `CreateRoom(Event, Abnormality, null)` → `PullNextEvent(State)` 호출 (바닐라 이벤트 풀)
- v1은 전용 이벤트 컨텐츠 없음. 시각적 구분(아이콘+호버)만으로 유지
- 추후 `doc/dynamic-event-design.md`의 동적 이벤트 시스템과 연동하여 전용 컨텐츠 추가

## 파일 구조 (구현)

```
src/Map/
├── AbnormalityMapPointType.cs     # sentinel 상수 정의
├── AbnormalityPreflight.cs        # ModInit 시 reflection 기반 안전 검사
├── AbnormalityMapInjector.cs      # 주입 로직 (config+preflight 게이트)
├── MapPointTypePatches.cs         # 6개 Harmony 패치 (switch 단락 + 주입)
└── HoverTipOverride.cs            # NMapPointHistoryHoverTip Postfix 헬퍼
```

### 기존 파일 수정
- `src/ModStart.cs`: preflight 호출 + injector 패치 등록 지점 추가
- `src/TheCityConfig.cs`: `[ConfigSection("MapSettings")] EnableAbnormalityNodes`
- `assets/localization/{eng,kor}/thecity.json`: `ROOM_ABNORMALITY` 키
- `assets/localization/{eng,kor}/settings_ui.json`: config 토글 제목

### 로컬라이제이션

**`assets/localization/eng/thecity.json`** (추가):
```json
"ROOM_ABNORMALITY.title": "Abnormality",
"ROOM_ABNORMALITY.description": "An anomalous presence. Outcome unknown."
```

**`assets/localization/kor/thecity.json`** (추가):
```json
"ROOM_ABNORMALITY.title": "환상체",
"ROOM_ABNORMALITY.description": "비정상적 존재. 결과는 알 수 없다."
```

**주의**: `NTopBarRoomIcon`은 `static_hover_tips` 테이블에서 `PREFIX.title`/`.description`을 조회. 우리 `thecity.json`의 키는 그 테이블에 없으므로, 상단바 호버팁은 라벨이 누락되거나 영문 `ROOM_ABNORMALITY.title`가 직접 표시될 가능성이 있음. 완전 해결은 v2에서 `LocManager`에 `static_hover_tips` 테이블 키 추가 필요.

### 필요 에셋
- `assets/images/map/map_abnormality.tres` — 맵 아이콘 텍스처 (M2)
- `assets/images/map/map_abnormality_outline.tres` — 아이콘 아웃라인 (M2)
- 로드 경로: `res://assets/images/map/...`
- `<None Include="assets/**" />`에 의해 자동 .pck 패킹 (TheCity.csproj:59)
- `.tres` 반영은 `dotnet publish -c Release` 필요 (`dotnet build`만으로는 .pck 재생성 안 됨)

## Preflight 안전 검사

```csharp
public static class AbnormalityPreflight
{
    public static bool Healthy { get; private set; }

    public static void Run()
    {
        var reasons = new List<string>();

        if (Enum.IsDefined(typeof(MapPointType), (int)AbnormalityMapPointType.Abnormality))
            reasons.Add("Sentinel value 200 is now defined by game — bump sentinel");

        if (AccessTools.Method(typeof(RunManager), "RollRoomTypeFor") == null)
            reasons.Add("RunManager.RollRoomTypeFor missing");
        if (AccessTools.Method(typeof(NNormalMapPoint), "IconName") == null)
            reasons.Add("NNormalMapPoint.IconName missing");
        // ... 각 패치 대상 동일 체크

        Healthy = reasons.Count == 0;
        if (!Healthy)
            GD.PushError($"[{ModStart.ModId}] Abnormality preflight failed: {string.Join("; ", reasons)}");
    }
}
```

**원칙**: preflight 실패 시 injector는 건너뛰지만 switch 패치는 그대로 설치. 기존 세이브에 sentinel 200이 있으면 여전히 정상 매핑되어 크래시 방지.

## 라이프사이클

```
ModInit:
  1. ModConfigRegistry.Register(...)
  2. AbnormalityPreflight.Run()          // 먼저 검증
  3. AbnormalityMapPointType.EnsureLoaded()
  4. Harmony.PatchAll()                  // switch 패치 설치 (preflight 실패해도 설치)
  5. SinExtensions.RegisterAll()

Run 시작:
  GenerateMap → Hook.ModifyGeneratedMap → [Postfix] AbnormalityMapInjector.Inject

Run 진행:
  EnterMapPoint → RollRoomTypeFor (Prefix로 Abnormality → Event 매핑)
  NMapScreen render → IconName (Prefix로 custom 아이콘 키)
  상단바 호버 → GetHoverTipPrefixForRoomType (Prefix로 ROOM_ABNORMALITY)
  기록 호버 → _Ready (Postfix로 _roomStats.Text 덮어쓰기)

Run 종료:
  세이브: SerializableMapPoint.Serialize → WriteInt((int)200, 8)
  로드: WriteInt ↔ ReadInt round-trip, PointType = 200
  (switch 패치가 설치돼있으면 정상 동작)
```

## 리스크 레지스터

| # | 리스크 | 가능성 | 영향 | 완화 |
|---|--------|:---:|:---:|------|
| R1 | sentinel 200이 게임 업데이트로 신규 enum 값과 충돌 | L | H | Preflight `Enum.IsDefined` 체크, 실패 시 feature 비활성 |
| R2 | 모드 미설치 MP 상대방이 enum 200 수신 → 크래시 | M | H | TheCity.json / README에 "양측 모드 필수" 명시. v2에서 peer detection |
| R3 | 모드 미설치 바닐라 클라이언트가 modded save 로드 → 크래시 | M | H | 알려진 제약으로 문서화. v2에서 BaseLib 저장 메타데이터 태깅 |
| R4 | 게임 업데이트로 `RollRoomTypeFor` 등 이름 변경 | M | H | Preflight에서 `AccessTools.Method` null 체크, 실패 시 비활성 |
| R5 | `NMapPointHistoryHoverTip` private 필드명 변경 | L | M | `Traverse.Field(...)` null 체크, 실패 시 WARN 로그 후 패스 |
| R6 | `Hook.ModifyGeneratedMap` 다중 호출로 중복 주입 | M | M | Inject에서 기존 Abnormality 존재 확인 + `CanBeModified = false` |
| R7 | `.tres` 아이콘 에셋이 .pck 누락 | M | L | `ResourceLoader.Exists` 체크, fallback 경로 반환 |
| R8 | Private 멤버에 Harmony 접근 실패 (`[Publicize]` 없음) | L | M | `AccessTools`는 visibility 무시, preflight가 reflection으로 확인 |
| R9 | `static_hover_tips` 테이블에 키가 없어 상단바 호버팁 불완전 | H | L | v1은 수용, v2에서 LocManager 확장 |

## 검증 매트릭스 (L0–L7)

| Level | 목적 | 명령 | 통과 기준 |
|-------|------|------|-----------|
| L0 | 정적 검사 | `check_dependencies`, `analyze_build_output` | 에러 0, API mismatch 0 |
| L1 | 모드 부팅 | `launch_game` → `bridge_ping` → `bridge_get_game_log` | "TheCity initialized", preflight `Healthy=true`, Harmony 예외 없음 |
| L2 | 맵 생성 | `bridge_start_run` → `bridge_get_map_state` | Abnormality 노드(200) ≥1 존재, injector 로그 `placed=1` |
| L3 | 아이콘 렌더 | `bridge_capture_screenshot` (맵 화면) | 커스텀 아이콘 표시 (M2 이후) |
| L4 | 방 진입 | `bridge_navigate_map → bridge_get_screen → bridge_get_exceptions` | Event 방 진입 성공, 예외 0 |
| L5 | 호버 툴팁 | 호버 → `bridge_get_full_state` | 제목/설명이 Abnormality/환상체로 표시 |
| L6 | 세이브 라운드트립 | `bridge_save_snapshot` → 재시작 → `bridge_restore_snapshot` → `bridge_get_map_state` | Abnormality 노드 200 유지 |
| L7 | 코옵 (가능 시) | 양측 모드 설치 | 맵 상태 동일, 예외 없음 |
| Reg | 바닐라 로드 거부 | 모드 제거 후 L6 스냅샷 로드 | 크래시 아닌 graceful 실패 (미달 시 제약으로 문서화) |

**모듈별 통과 요구**:
- M1 완료 → L1, L2(수동: `bridge_manipulate_state`), L4, L5, L6
- M2 완료 → + L3
- M3 완료 → + L2(자동), L7

## 롤백 전략

- **Soft disable**: `TheCityConfig.EnableAbnormalityNodes = false` — injector 스킵. switch 패치는 유지되어 기존 세이브 정상 동작
- **Preflight disable**: 런타임에 `AbnormalityPreflight._healthy = false` 세팅 (bridge_execute_action로 외부 주입 가능 시)
- **완전 제거**: Abnormality 노드가 포함된 run은 모드 없이 로드 불가 (문서화된 제약)

## 알려진 제약

1. MP에서 모든 peer가 TheCity 모드 필수 (한쪽만 없으면 크래시)
2. Abnormality 노드 포함 세이브는 모드 없이 로드 불가
3. 게임 업데이트로 switch 로직 변경 시 preflight가 이름 변경은 잡지만 로직 변경은 수동 재검증 필요
4. 전용 이벤트 컨텐츠 없음 — 바닐라 이벤트 풀 공유 (추후 `doc/dynamic-event-design.md`와 연동 시 해결)
5. 상단바 호버 팁의 `static_hover_tips.ROOM_ABNORMALITY.title` 누락 (v2 과제)

## 실행 순서

1. **S0** (완료): 이 문서 작성
2. **M1**: sentinel + 4 switch 패치 + 로컬라이제이션 + preflight
3. **M2**: `.tres` 아이콘 에셋 + GetRoomIconPath 패치 (v1은 placeholder `.tres` 가능)
4. **M3**: ModifyGeneratedMap Postfix + config 토글 + 배치 정책
