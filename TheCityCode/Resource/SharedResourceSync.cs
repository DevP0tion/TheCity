using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;
using MegaCrit.Sts2.Core.Runs;

namespace TheCity.TheCityCode.Resource;

/// <summary>
/// SharedResource 값 동기화용 네트워크 메시지.
/// 값이 변경될 때 모든 플레이어에게 브로드캐스트.
/// </summary>
public sealed class SharedResourceSyncMessage : INetMessage, IPacketSerializable
{
    public bool ShouldBroadcast => true;
    public NetTransferMode Mode => NetTransferMode.Reliable;
    public LogLevel LogLevel => LogLevel.Debug;

    public int NewValue { get; set; }

    public void Serialize(PacketWriter writer)
    {
        writer.WriteInt(NewValue);
    }

    public void Deserialize(PacketReader reader)
    {
        NewValue = reader.ReadInt();
    }
}

/// <summary>
/// SharedResource 네트워크 동기화 유틸리티.
/// </summary>
public static class SharedResourceSync
{
    private static bool _registered;

    /// <summary>메시지 핸들러 등록. 모드 초기화 시 호출.</summary>
    public static void Register()
    {
        if (_registered) return;

        var netService = RunManager.Instance?.NetService;
        if (netService == null) return;

        netService.RegisterMessageHandler<SharedResourceSyncMessage>(OnReceived);
        _registered = true;
    }

    /// <summary>메시지 핸들러 해제.</summary>
    public static void Unregister()
    {
        if (!_registered) return;

        var netService = RunManager.Instance?.NetService;
        netService?.UnregisterMessageHandler<SharedResourceSyncMessage>(OnReceived);
        _registered = false;
    }

    /// <summary>현재 값을 다른 플레이어에게 전송.</summary>
    public static void SendUpdate(int newValue)
    {
        var netService = RunManager.Instance?.NetService;
        if (netService == null) return;

        netService.SendMessage(new SharedResourceSyncMessage { NewValue = newValue });
    }

    private static void OnReceived(SharedResourceSyncMessage msg, ulong senderId)
    {
        // 수신 측: sync=false로 설정하여 재전송 방지
        SharedResource.Set(msg.NewValue, sync: false);
    }
}
