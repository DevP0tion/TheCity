# 멀티플레이어 네트워킹 가이드

## INetMessage 구현 패턴

### 메시지 클래스
```csharp
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;

public sealed class MyMessage : INetMessage, IPacketSerializable
{
    public bool ShouldBroadcast => true;                    // 전체 브로드캐스트
    public NetTransferMode Mode => NetTransferMode.Reliable; // 신뢰 전송
    public LogLevel LogLevel => LogLevel.Debug;

    public string Data { get; set; } = string.Empty;
    public int Amount { get; set; }

    public void Serialize(PacketWriter writer)
    {
        writer.WriteString(Data);
        writer.WriteInt(Amount);
    }

    public void Deserialize(PacketReader reader)
    {
        Data = reader.ReadString();
        Amount = reader.ReadInt();
    }
}
```

### 핵심 주의사항
- `Mode` 사용 (`TransferMode` 아님)
- `PacketWriter`/`PacketReader` 사용 (`StreamPeerBuffer` 아님)
- 핸들러 시그니처: `(T message, ulong senderId)` — `int`가 아닌 `ulong`
- `UnregisterMessageHandler<T>`에 동일 delegate 참조 전달 필수

### 전송 모드
| 모드 | 용도 |
|------|------|
| `Reliable` | 게임 상태 동기화 (일반적 사용) |
| `Unreliable` | 고빈도 임시 데이터 |
| `ReliableOrdered` | 순서 보장 필요 시 |

### 등록/해제
```csharp
RunManager.Instance.NetService?.RegisterMessageHandler<MyMessage>(OnReceived);
RunManager.Instance.NetService?.UnregisterMessageHandler<MyMessage>(OnReceived);
```

### 복합 데이터 전송 패턴
JSON 직렬화로 복잡한 구조체 전달:
```csharp
var json = System.Text.Json.JsonSerializer.Serialize(complexData);
writer.WriteString(json);
```

## 게임 내장 커맨드 (자동 동기화)
카드 OnPlay에서 아래 커맨드 사용 시 별도 네트워크 코드 불필요:
- `DamageCmd.Attack()` — 데미지
- `CreatureCmd.GainBlock()` — 블록
- `PowerCmd.Apply<T>()` — 파워
- `CardPileCmd.Draw()` — 드로우

## GameAction 멀티플레이어
- `ToNetAction()` 구현 필요 (미구현 시 싱글만 동작)
- `RunManager.Instance.ActionQueueSynchronizer.RequestEnqueue()` 로 큐잉

## 재전송 방지 패턴
```csharp
// 로컬 변경 시
SharedResourceManager.Modify("faith", 3, sync: true);   // 동기화 O

// 네트워크 수신 시
SharedResourceManager.Set("faith", newValue, sync: false); // 동기화 X (재전송 방지)
```
