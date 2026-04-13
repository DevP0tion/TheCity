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
        Harmony harmony = new(ModId);
        harmony.PatchAll();

        // 자원 종류 등록 (전투 시작 전에 등록해야 함)
        // SharedResourceManager.Register("faith");
        // SharedResourceManager.Register("corruption");

        GD.Print($"[{ModId}] Mod initialized.");
    }
}
