using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;

namespace TheCity.TheCityCode.Resource;

/// <summary>
/// 전투 시작/종료 시 SharedResourceManager 초기화/정리.
/// </summary>
[HarmonyPatch(typeof(CombatManager), nameof(CombatManager.SetUpCombat))]
public static class CombatStartPatch
{
    public static void Postfix()
    {
        SharedResourceSync.Register();
        SharedResourceManager.Initialize();
        MainFile.Logger.Info("SharedResourceManager initialized for combat.");
    }
}

[HarmonyPatch(typeof(CombatManager), nameof(CombatManager.EndCombatInternal))]
public static class CombatEndPatch
{
    public static void Postfix()
    {
        SharedResourceManager.Cleanup();
        SharedResourceSync.Unregister();
        MainFile.Logger.Info("SharedResourceManager cleaned up after combat.");
    }
}
