using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using TheCity;

namespace TheCity.Resource;

[HarmonyPatch(typeof(CombatManager), nameof(CombatManager.SetUpCombat))]
public static class CombatStartPatch
{
    public static void Postfix()
    {
        SharedResourceSync.Register();
        SharedResourceManager.Initialize();
        GD.Print($"[{ModStart.ModId}] SharedResourceManager initialized.");
    }
}

[HarmonyPatch(typeof(CombatManager), nameof(CombatManager.EndCombatInternal))]
public static class CombatEndPatch
{
    public static void Postfix()
    {
        SharedResourceManager.Cleanup();
        SharedResourceSync.Unregister();
        CardFields.ClearAll();
        GD.Print($"[{ModStart.ModId}] SharedResourceManager cleaned up.");
    }
}
