using BaseLib.Config;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using TheCity.Resource;

namespace TheCity;

[ModInitializer(nameof(ModInit))]
public static class ModStart
{
    public const string ModId = "TheCity";

    public static void ModInit()
    {
        // 설정 로드 (Harmony 패치 전에 등록하여 값이 먼저 로드됨)
        ModConfigRegistry.Register(ModId, new TheCityConfig());

        Harmony harmony = new(ModId);
        harmony.PatchAll();

        // 자원 종류 등록 (전투 시작 전에 등록해야 함)
        // SharedResourceManager.Register("faith");
        // SharedResourceManager.Register("corruption");

        GD.Print($"[{ModId}] Mod initialized.");
    }
}
