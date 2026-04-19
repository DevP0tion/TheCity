// SinStackPanel을 NCombatRoom.Ui 레이어에 주입.
// 전투 중 내내 덱(DrawPile) 위에 상시 표시되므로 카드 hover/select 추적 패치 불필요.
using HarmonyLib;
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
