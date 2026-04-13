using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Rooms;

namespace TheCity.TheCityCode.Resource;

/// <summary>
/// 전투 시작/종료 시 SharedResource 초기화/정리.
/// CombatManager의 이벤트를 Harmony 패치로 후킹.
/// </summary>
[HarmonyPatch(typeof(CombatManager), nameof(CombatManager.SetUpCombat))]
public static class CombatStartPatch
{
    public static void Postfix()
    {
        SharedResource.Initialize();
        SharedResourceSync.Register();
        MainFile.Logger.Info("SharedResource initialized for combat.");
    }
}

[HarmonyPatch(typeof(CombatManager), nameof(CombatManager.EndCombatInternal))]
public static class CombatEndPatch
{
    public static void Postfix()
    {
        SharedResourceSync.Unregister();
        SharedResource.Cleanup();
        MainFile.Logger.Info("SharedResource cleaned up after combat.");
    }
}
