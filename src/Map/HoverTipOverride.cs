using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Runs.History;

namespace TheCity.Map;

/// <summary>
/// <see cref="NMapPointHistoryHoverTip"/>의 Postfix 헬퍼.
///
/// 원본 <c>_Ready</c>는 내부에 inline switch가 있고 default → null이라서 Prefix로는 단락할 수 없음
/// (다른 초기화 로직까지 스킵되어 크래시). Postfix로 정상 실행 후,
/// <c>_entry.MapPointType == Abnormality</c>인 경우 <c>_roomStats.Text</c>를 Abnormality 라벨로 덮어씀.
/// </summary>
internal static class HoverTipOverride
{
    public static void ApplyIfAbnormality(NMapPointHistoryHoverTip instance)
    {
        var entry = Traverse.Create(instance).Field("_entry").GetValue<MapPointHistoryEntry>();
        if (entry == null || entry.MapPointType != AbnormalityMapPointType.Abnormality) return;

        var roomStats = Traverse.Create(instance).Field("_roomStats").GetValue<RichTextLabel>();
        if (roomStats == null) return;

        // NMapPointHistoryHoverTip은 static_hover_tips 테이블을 참조하므로
        // 우리 thecity 테이블의 Loc.Get을 사용해 라벨만 덮어씀.
        roomStats.Text = Loc.Get($"{AbnormalityMapPointType.LocPrefix}.title");
    }
}
