# 동적 이벤트 시스템

## 상태: ❌ 미구현 — 설계만 완료

## 개요
맵 노드의 이벤트(? 방) 내용을 런타임에 동적으로 생성하는 시스템.
방장(호스트)이 데이터를 생성 → 네트워크로 전파 → 클라이언트는 수신한 데이터로 이벤트 표시.

## 설계 구조

### 파일 (구현 필요)
```
src/Event/
├── EventData.cs            # 이벤트 데이터 모델 (직렬화 가능)
├── EventDataRegistry.cs    # 동기화된 데이터 보관소 (Dictionary)
├── EventDataSync.cs        # INetMessage로 호스트→클라이언트 전파
├── EventDataGenerator.cs   # IEventDataGenerator 인터페이스 + 구현
├── EventManager.cs         # 오케스트레이터
└── HostUtil.cs             # 방장 판별 유틸리티
```

### 데이터 흐름
```
[이벤트 방 진입]
       │
       ▼
  HostUtil.IsHost?
       │
  ┌────┴────┐
  │ Host    │ Client
  ▼         ▼
Generate    대기
  │
  ▼
BroadcastFromHost ──→ EventDataSyncMessage ──→ 수신
  │                                             │
  ▼                                             ▼
EventDataRegistry.Set()              EventDataRegistry.Set()
  │                                             │
  └──────────────┬──────────────────────────────┘
                 ▼
      CustomEventModel이 Registry에서 데이터 읽어 표시
      (모든 클라이언트가 동일 데이터 참조)
```

### EventData 모델
```csharp
public class EventData
{
    public string EventId { get; set; }
    public string TitleKey { get; set; }
    public string DescriptionKey { get; set; }
    public List<EventChoiceData> Choices { get; set; }
    public Dictionary<string, int> IntParams { get; set; }
    public Dictionary<string, string> StringParams { get; set; }
}

public class EventChoiceData
{
    public string ChoiceKey { get; set; }
    public string EffectType { get; set; }
    public Dictionary<string, int> Params { get; set; }
}
```

### 네트워크 메시지
- `EventDataSyncMessage` : `INetMessage`, `IPacketSerializable`
- EventData를 `System.Text.Json`으로 직렬화하여 string으로 전달
- `NetTransferMode.Reliable`

### 호스트 판별 (⚠ 구현 시 확인 필요)
- 싱글플레이어: `RunManager.Instance.NetService == null` → 항상 호스트
- 멀티플레이어: 정확한 API를 MCP `search_game_code`로 확인 필요
  - 후보: `LocalContext.GetLocalPlayerId()`, `RunManager` 관련 프로퍼티
  - `strings sts2.dll` 결과: `GetLocalPlayerId`, `get_LocalPlayerId`, `AddLocalHostPlayer` 존재
  - MCP 도구: `search_game_code` pattern `"IsHost|AddLocalHostPlayer|GetLocalPlayerId"` 로 조회

### BaseLib CustomEventModel 사용법
```csharp
public class DynamicCityEvent() : CustomEventModel()
{
    public override bool IsShared => true;  // 멀티: 전원 동일 이벤트

    // Registry에서 데이터를 읽어 동적 구성
    public override void CalculateVars()
    {
        var data = EventDataRegistry.Get("dynamic_city");
        // data.IntParams로 수치 설정
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        var data = EventDataRegistry.Get("dynamic_city");
        // data.Choices로 선택지 구성
    }

    public override bool IsAllowed(...)
    {
        // EventDataRegistry에 데이터가 있을 때만 출현
        return EventDataRegistry.Get("dynamic_city") != null;
    }
}
```

### 멀티플레이어 안전 규칙
- ✅ 안전: SharedResourceManager (동기화됨), 게임 내장 Rng (시드 기반), 런 상태
- ❌ 위험: System.Random, 로컬 시간/파일, 비동기화 상태
- `IsShared => true` 필수
