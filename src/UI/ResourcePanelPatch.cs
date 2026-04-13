using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace TheCity.UI;

/// <summary>
/// NCombatRoom에 ResourcePanel을 주입하는 Harmony 패치.
/// 유물 바 하단에 배치.
/// </summary>
[HarmonyPatch(typeof(NCombatRoom), "_Ready")]
public static class ResourcePanelPatch
{
    public static void Postfix(NCombatRoom __instance)
    {
        if (ResourcePanel.Instance != null) return;

        var panel = new ResourcePanel();
        panel.AnchorLeft = 0f;
        panel.AnchorTop = 0f;
        panel.OffsetLeft = 20;
        panel.OffsetTop = 90;
        __instance.AddChild(panel);
    }
}
