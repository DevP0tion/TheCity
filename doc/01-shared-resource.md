# 공유 자원 시스템 (SharedResourceManager)

## 상태: ✅ 구현 완료

## 개요
파티 전체가 공유하는 ID 기반 다중 정수형 자원 시스템.
전투 시작 시 0으로 초기화, 전투 종료 시 정리.
값 변경 시 INetMessage로 멀티플레이어 자동 동기화.

## 파일
- `src/Resource/SharedResourceManager.cs` — 자원 관리 (static, Dictionary<string, int>)
- `src/Resource/SharedResourceSync.cs` — INetMessage 기반 동기화
- `src/Resource/CardFields.cs` — CardModel에 커스텀 정수형 속성 부착
- `src/Resource/CombatLifecyclePatches.cs` — 전투 시작/종료 Harmony 패치

## API

### 자원 등록 (ModInit에서 호출)
```csharp
SharedResourceManager.Register("faith");
SharedResourceManager.Register("corruption");
```

### 자원 조작 (전투 중)
```csharp
SharedResourceManager.Modify("faith", 3);       // +3, 자동 멀티 동기화
SharedResourceManager.Modify("corruption", -1);  // -1
SharedResourceManager.Set("faith", 10);          // 절대값 설정
int val = SharedResourceManager.Get("faith");    // 조회
bool has = SharedResourceManager.Has("faith");   // 존재 여부
```

### 이벤트 (UI 등 외부에서 구독)
```csharp
SharedResourceManager.ValueChanged += (id, oldVal, newVal) => { };
SharedResourceManager.ResourceRegistered += (id, initial) => { };
SharedResourceManager.Initialized += () => { };    // 전투 시작
SharedResourceManager.CleanedUp += () => { };      // 전투 종료
```

### CardModel 확장 속성
```csharp
card.SetCityValue(5);
int val = card.GetCityValue();  // 미설정 시 0
// 전투 종료 시 CardFields.ClearAll() 자동 호출
```

## 네트워크 동기화
- `SharedResourceSyncMessage` : `INetMessage`, `IPacketSerializable`
- 필드: `ResourceId (string)`, `NewValue (int)`
- `NetTransferMode.Reliable`
- `Modify()`/`Set()` 호출 시 자동으로 `SendMessage()` 수행
- 수신 측은 `sync: false`로 `Set()` 호출하여 재전송 방지

## 라이프사이클
1. `CombatManager.SetUpCombat` Postfix → `SharedResourceSync.Register()` + `SharedResourceManager.Initialize()`
2. 전투 진행 → `Modify()` / `Set()` 호출 → 이벤트 발행 + 네트워크 동기화
3. `CombatManager.EndCombatInternal` Postfix → `Cleanup()` + `Unregister()` + `CardFields.ClearAll()`
