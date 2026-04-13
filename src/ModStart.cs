using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using TheCity.Resource;

namespace TheCity;

[ModInitializer(nameof(Initialize))]
public partial class ModStart : Node
{
    public const string ModId = "TheCity";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } = new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static void Initialize()
    {
        Harmony harmony = new(ModId);
        harmony.PatchAll();

        // 자원 종류 등록 (전투 시작 전에 등록해야 함)
        // SharedResourceManager.Register("faith");
        // SharedResourceManager.Register("corruption");

        Logger.Info("TheCity mod initialized.");
    }
}
