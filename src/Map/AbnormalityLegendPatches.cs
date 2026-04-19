using System.Linq;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.addons.mega_text;

namespace TheCity.Map;

/// <summary>
/// 맵 범례(MapLegend)에 환상체 항목을 추가하는 Harmony 패치 모음.
///
/// 전략:
/// 1. <see cref="NMapLegendItem.SetMapPointType"/>와 <see cref="NMapLegendItem.SetLocalizedFields"/>의 switch를
///    Prefix로 확장하여 "AbnormalityLegendItem"이라는 새 이름을 허용 (원본 default는 throw).
/// 2. <see cref="NMapScreen._Ready"/> Postfix에서 기존 UnknownLegendItem을 <c>Duplicate()</c>로 복제하고
///    이름을 바꾼 뒤 <c>_legendItems</c>에 <c>AddChild</c>.
/// 3. AddChild 후 _Ready 타이밍 이슈 방지를 위해 SetLocalizedFields / SetMapPointType를 명시적으로 재호출.
/// 4. 아이콘을 별 텍스처로 교체.
/// 5. Focus neighbor 체인을 재계산해 컨트롤러 네비게이션 유지.
/// </summary>
internal static class AbnormalityLegendPatches
{
    private const string LegendItemName = "AbnormalityLegendItem";
    private const string UnknownTemplateName = "UnknownLegendItem";
    private const string LegendLocKey = "LEGEND_ABNORMALITY";
    private const string FallbackTitle = "Abnormality";

    // ── NMapLegendItem.SetMapPointType Prefix ──
    [HarmonyPatch(typeof(NMapLegendItem), "SetMapPointType")]
    public static class NMapLegendItem_SetMapPointType_Patch
    {
        public static bool Prefix(NMapLegendItem __instance, string name)
        {
            if (name != LegendItemName) return true;
            try
            {
                Traverse.Create(__instance).Field("_pointType").SetValue(AbnormalityMapPointType.Abnormality);
            }
            catch (System.Exception ex)
            {
                GD.PushError($"[{ModStart.ModId}] Legend: SetMapPointType Prefix threw: {ex.Message}");
            }
            return false; // 원본 실행 금지 (원본은 Abnormality 이름을 모르고 throw함)
        }
    }

    // ── NMapLegendItem.SetLocalizedFields Prefix ──
    [HarmonyPatch(typeof(NMapLegendItem), "SetLocalizedFields")]
    public static class NMapLegendItem_SetLocalizedFields_Patch
    {
        public static bool Prefix(NMapLegendItem __instance, string name)
        {
            if (name != LegendItemName) return true;

            // 원본 switch가 "AbnormalityLegendItem"을 모르므로 원본 실행 시 throw 발생 → 라벨 미설정.
            // try-catch로 어떤 예외가 나도 return false를 보장.
            try
            {
                var label = __instance.GetNodeOrNull<MegaLabel>("MegaLabel");
                if (label != null)
                {
                    string title = FallbackTitle;
                    if (Loc.Has($"{LegendLocKey}.title"))
                    {
                        var translated = Loc.Get($"{LegendLocKey}.title");
                        if (!string.IsNullOrWhiteSpace(translated) && translated != $"{LegendLocKey}.title")
                        {
                            title = translated;
                        }
                    }
                    label.SetTextAutoSize(title);
                }

                // HoverTip: thecity 테이블 키가 있든 없든 LocString 생성 시 예외 가능 → try 안에서.
                var hoverTip = new HoverTip(
                    Loc.Of($"{LegendLocKey}.hoverTip.title"),
                    Loc.Of($"{LegendLocKey}.hoverTip.description"));
                Traverse.Create(__instance).Field("_hoverTip").SetValue(hoverTip);
            }
            catch (System.Exception ex)
            {
                GD.PushError($"[{ModStart.ModId}] Legend: SetLocalizedFields Prefix threw: {ex.Message}");
                // fallback 최소 처리: 라벨만이라도 강제 설정
                try
                {
                    var label = __instance.GetNodeOrNull<MegaLabel>("MegaLabel");
                    label?.SetTextAutoSize(FallbackTitle);
                }
                catch { /* swallow */ }
            }

            return false;
        }
    }

    // ── NMapScreen._Ready Postfix: 범례에 Abnormality 항목 추가 ──
    [HarmonyPatch(typeof(NMapScreen), "_Ready")]
    public static class NMapScreen_Ready_Patch
    {
        public static void Postfix(NMapScreen __instance)
        {
            if (!AbnormalityPreflight.Healthy) return;

            var legendItems = Traverse.Create(__instance).Field("_legendItems").GetValue<Control>();
            if (legendItems == null) return;

            // 템플릿: UnknownLegendItem을 복제 (아이콘 TextureRect 구조 포함). 중복 추가 방지 겸임.
            NMapLegendItem? template = null;
            foreach (var child in legendItems.GetChildren())
            {
                if (child is not NMapLegendItem item) continue;
                if (item.Name == LegendItemName) return;  // 이미 추가됨
                if (item.Name == UnknownTemplateName) template = item;
            }
            if (template == null) return;

            var clone = template.Duplicate();
            if (clone is not NMapLegendItem newItem)
            {
                clone?.QueueFree();
                return;
            }
            newItem.Name = LegendItemName;
            legendItems.AddChild(newItem);  // AddChild 시 _Ready 실행 (패치된 SetLocalizedFields/SetMapPointType 호출됨)

            // 방어적 재호출: _Ready가 타이밍/중간 상태로 잘못된 이름으로 호출됐을 가능성 대비.
            // 우리 Prefix는 LegendItemName에만 매칭되므로 멱등.
            InvokePrivateVoid(newItem, "SetLocalizedFields", LegendItemName);
            InvokePrivateVoid(newItem, "SetMapPointType", LegendItemName);

            var icon = Traverse.Create(newItem).Field("_icon").GetValue<TextureRect>();
            if (icon != null) icon.Texture = StarTextureFactory.Star;

            // Focus neighbor 체인 재계산 (새 항목이 맨 끝에 추가됨). Right는 수평 이동 없음.
            var list = legendItems.GetChildren().OfType<NMapLegendItem>().ToList();
            for (int i = 0; i < list.Count; i++)
            {
                list[i].FocusNeighborTop = (i > 0) ? list[i - 1].GetPath() : list[i].GetPath();
                list[i].FocusNeighborBottom = (i < list.Count - 1) ? list[i + 1].GetPath() : list[i].GetPath();
                list[i].FocusNeighborRight = list[i].GetPath();
            }

            GD.Print($"[{ModStart.ModId}] Legend: Abnormality item added (total {list.Count}).");
        }

        private static void InvokePrivateVoid(object target, string methodName, object arg)
        {
            var method = AccessTools.Method(target.GetType(), methodName);
            method?.Invoke(target, new[] { arg });
        }
    }
}
