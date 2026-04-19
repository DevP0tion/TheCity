// Design Ref: Plan §Harmony 패치 요약 — 3개 Postfix.
// 1) NCombatRoom._Ready: 스택 주입 (Ui 레이어)
// 2) HoveredModelTracker.OnLocalCardSelected: Bind
// 3) HoveredModelTracker.OnLocalCardDeselected: Unbind
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game.PeerInput;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace TheCity.UI;

[HarmonyPatch(typeof(NCombatRoom), "_Ready")]
public static class SinStackInjectPatch
{
    public static void Postfix(NCombatRoom __instance)
    {
        if (SinStackPanel.IsActive) return;
        if (__instance.Ui == null) return;
        SinStackPanel.AttachTo(__instance.Ui);
    }
}

[HarmonyPatch(typeof(HoveredModelTracker), nameof(HoveredModelTracker.OnLocalCardSelected))]
public static class SinStackSelectPatch
{
    public static void Postfix(CardModel cardModel)
    {
        SinStackPanel.Bind(cardModel);
    }
}

[HarmonyPatch(typeof(HoveredModelTracker), nameof(HoveredModelTracker.OnLocalCardDeselected))]
public static class SinStackDeselectPatch
{
    public static void Postfix()
    {
        SinStackPanel.Unbind();
    }
}
