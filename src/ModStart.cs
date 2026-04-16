using BaseLib.Config;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using TheCity.Map;
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

        // Abnormality 기능 preflight 검증 (sentinel 충돌 / 리네임 감지)
        AbnormalityPreflight.Run();
        AbnormalityMapPointType.EnsureLoaded();

        Harmony harmony = new(ModId);
        harmony.PatchAll();

        // 7대죄 자원 등록
        SinExtensions.RegisterAll();

        GD.Print($"[{ModId}] Mod initialized.");
    }
}
