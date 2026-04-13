using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;
using MegaCrit.Sts2.Core.Runs;

namespace TheCity.Resource;

/// <summary>
/// 특정 자원의 값 동기화용 네트워크 메시지.
/// </summary>
public sealed class SharedResourceSyncMessage : INetMessage, IPacketSerializable
{
    public bool ShouldBroadcast => true;
    public NetTransferMode Mode => NetTransferMode.Reliable;
    public LogLevel LogLevel => LogLevel.Debug;

    public string ResourceId { get; set; } = string.Empty;
    public int NewValue { get; set; }

    public void Serialize(PacketWriter writer)
    {
        writer.WriteString(ResourceId);
        writer.WriteInt(NewValue);
    }

    public void Deserialize(PacketReader reader)
    {
        ResourceId = reader.ReadString();
        NewValue = reader.ReadInt();
    }
}

/// <summary>
/// SharedResourceManager 네트워크 동기화 유틸리티.
/// </summary>
public static class SharedResourceSync
{
    private static bool _registered;

    public static void Register()
    {
        if (_registered) return;

        var netService = RunManager.Instance?.NetService;
        if (netService == null) return;

        netService.RegisterMessageHandler<SharedResourceSyncMessage>(OnReceived);
        _registered = true;
    }

    public static void Unregister()
    {
        if (!_registered) return;

        var netService = RunManager.Instance?.NetService;
        netService?.UnregisterMessageHandler<SharedResourceSyncMessage>(OnReceived);
        _registered = false;
    }

    public static void SendUpdate(string resourceId, int newValue)
    {
        var netService = RunManager.Instance?.NetService;
        if (netService == null) return;

        netService.SendMessage(new SharedResourceSyncMessage
        {
            ResourceId = resourceId,
            NewValue = newValue,
        });
    }

    private static void OnReceived(SharedResourceSyncMessage msg, ulong senderId)
    {
        SharedResourceManager.Set(msg.ResourceId, msg.NewValue, sync: false);
    }
}
