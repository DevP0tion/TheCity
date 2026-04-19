using System.Collections.Generic;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.sts2.Core.Nodes.TopBar;

namespace TheCity.Map;

/// <summary>
/// Abnormality sentinel을 처리하는 Harmony 패치 모음.
///
/// - switch 패치는 preflight 상태와 무관하게 항상 활성 (기존 세이브의 sentinel 값 매핑 필요)
/// - injector(Hook.ModifyGeneratedMap Postfix)만 preflight + config로 게이트됨
/// </summary>
internal static class MapPointTypePatches
{
    // 커스텀 아이콘 경로 (v1: .tres 미생성 → UpdateIcon Postfix의 별 텍스처가 실제 표시)
    private const string AbnormalityIconPath = "res://assets/images/map/map_abnormality.tres";
    private const string AbnormalityOutlinePath = "res://assets/images/map/map_abnormality_outline.tres";

    // 에셋 존재 여부 캐시 (ResourceLoader.Exists 반복 호출 방지).
    // 에셋 존재 여부는 런타임 중 변하지 않음.
    private static readonly Dictionary<string, bool> AssetExistsCache = new();

    private static bool AssetExists(string path)
    {
        if (AssetExistsCache.TryGetValue(path, out var cached)) return cached;
        bool exists = ResourceLoader.Exists(path);
        AssetExistsCache[path] = exists;
        if (!exists) GD.Print($"[{ModStart.ModId}] Abnormality asset missing at {path}, using fallback.");
        return exists;
    }


    // ── 1. RunManager.RollRoomTypeFor (Prefix) ──
    //     private RoomType RollRoomTypeFor(MapPointType pointType, IEnumerable<RoomType> blacklist)
    //     default: throw ArgumentOutOfRangeException → Prefix + return false로 단락 안전

    [HarmonyPatch(typeof(RunManager), "RollRoomTypeFor")]
    public static class RunManager_RollRoomTypeFor_Patch
    {
        public static bool Prefix(MapPointType pointType, ref RoomType __result)
        {
            if (pointType != AbnormalityMapPointType.Abnormality) return true;
            __result = RoomType.Event;
            return false;
        }
    }

    // ── 2. NNormalMapPoint.IconName (Prefix) ──
    //     private static string IconName(MapPointType pointType)
    //     default: throw ArgumentOutOfRangeException → Prefix 안전

    [HarmonyPatch(typeof(NNormalMapPoint), "IconName")]
    public static class NNormalMapPoint_IconName_Patch
    {
        // NNormalMapPoint.IconPath는 "atlases/ui_atlas.sprites/map/icons/{filename}.tres"로 확장.
        // 게임 .pck에 존재하는 basename만 반환 가능 (로드 실패 방지) → Unknown basename 반환 후
        // UpdateIcon Postfix에서 실제 텍스처를 별 이미지로 교체.
        public static bool Prefix(MapPointType pointType, ref string __result)
        {
            if (pointType != AbnormalityMapPointType.Abnormality) return true;
            __result = AbnormalityMapPointType.IconBasename;
            return false;
        }
    }

    // ── 2b. NNormalMapPoint.UpdateIcon (Postfix) — 실제 텍스처를 별 아이콘으로 교체 ──

    [HarmonyPatch(typeof(NNormalMapPoint), "UpdateIcon")]
    public static class NNormalMapPoint_UpdateIcon_Patch
    {
        public static void Postfix(NNormalMapPoint __instance)
        {
            if (__instance.Point?.PointType != AbnormalityMapPointType.Abnormality) return;

            var t = Traverse.Create(__instance);
            var icon = t.Field("_icon").GetValue<TextureRect>();
            var outline = t.Field("_outline").GetValue<TextureRect>();

            if (icon != null)
            {
                icon.Texture = StarTextureFactory.Star;
            }
            if (outline != null)
            {
                // 외곽선 텍스처는 제거 (별 이미지에 외곽선이 이미 포함되어 있음)
                outline.Texture = null;
            }
        }
    }

    // ── 3. NTopBarRoomIcon.GetHoverTipPrefixForRoomType (Prefix) ──
    //     private string GetHoverTipPrefixForRoomType()
    //     무인자 — 내부에서 private GetCurrentMapPointType() 호출
    //     반환값은 loc prefix 문자열 (예: "ROOM_ANCIENT")

    [HarmonyPatch(typeof(NTopBarRoomIcon), "GetHoverTipPrefixForRoomType")]
    public static class NTopBarRoomIcon_GetHoverTipPrefix_Patch
    {
        public static bool Prefix(NTopBarRoomIcon __instance, ref string __result)
        {
            var mpt = Traverse.Create(__instance)
                              .Method("GetCurrentMapPointType")
                              .GetValue<MapPointType>();
            if (mpt != AbnormalityMapPointType.Abnormality) return true;
            __result = AbnormalityMapPointType.LocPrefix;
            return false;
        }
    }

    // ── 4. NMapPointHistoryHoverTip._Ready (Postfix) ──
    //     default → null (throw 아님) 이라 Prefix로 단락 불가.
    //     원본 실행 후 _entry.MapPointType == Abnormality 시 _roomStats 텍스트 덮어씀.

    [HarmonyPatch(typeof(NMapPointHistoryHoverTip), "_Ready")]
    public static class NMapPointHistoryHoverTip_Ready_Patch
    {
        public static void Postfix(NMapPointHistoryHoverTip __instance)
        {
            HoverTipOverride.ApplyIfAbnormality(__instance);
        }
    }

    // ── 5. ImageHelper.GetRoomIconPath (Prefix) — M2 ──
    //     에셋 누락 시 바닐라 이벤트 아이콘으로 fallback (원본 로직에 위임)

    [HarmonyPatch(typeof(ImageHelper), nameof(ImageHelper.GetRoomIconPath))]
    public static class ImageHelper_GetRoomIconPath_Patch
    {
        public static bool Prefix(MapPointType mapPointType, ref string? __result)
        {
            if (mapPointType != AbnormalityMapPointType.Abnormality) return true;
            if (!AssetExists(AbnormalityIconPath)) return true;
            __result = AbnormalityIconPath;
            return false;
        }
    }

    // ── 6. ImageHelper.GetRoomIconOutlinePath (Prefix) — M2 ──

    [HarmonyPatch(typeof(ImageHelper), nameof(ImageHelper.GetRoomIconOutlinePath))]
    public static class ImageHelper_GetRoomIconOutlinePath_Patch
    {
        public static bool Prefix(MapPointType mapPointType, ref string? __result)
        {
            if (mapPointType != AbnormalityMapPointType.Abnormality) return true;
            if (!AssetExists(AbnormalityOutlinePath)) return true;
            __result = AbnormalityOutlinePath;
            return false;
        }
    }

    // ── 7. Hook.ModifyGeneratedMap (Postfix) — M3 injector ──
    //     모든 AbstractModel 훅 리스너 순회 후, 마지막에 우리 injector 적용.
    //     Hook.ModifyGeneratedMapLate는 패치하지 않음 (loaded save 재주입 방지).

    [HarmonyPatch(typeof(Hook), nameof(Hook.ModifyGeneratedMap))]
    public static class Hook_ModifyGeneratedMap_Patch
    {
        public static void Postfix(IRunState runState, ref ActMap __result, int actIndex)
        {
            __result = AbnormalityMapInjector.Inject(runState, __result, actIndex);
        }
    }
}
