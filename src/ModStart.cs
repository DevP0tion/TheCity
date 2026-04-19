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

        // LocManager는 이미 초기화 완료된 상태 → 현재 언어로 1회 수동 주입.
        // 이후 언어 변경 시엔 LocTableInjector의 SetLanguageInternal_Patch가 자동 재주입.
        LocTableInjector.InjectForCurrentLanguage();

        // 7대죄 자원 등록
        SinExtensions.RegisterAll();

        GD.Print($"[{ModId}] Mod initialized.");
    }
}
