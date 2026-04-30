using HarmonyLib;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using TheCity.Abnormality.Template;
using TheCity.Map;

namespace TheCity.Abnormality.MapIntegration;

/// <summary>
/// 환상체 맵 노드 진입 시 등록된 환상체 <see cref="EventModel"/> 로 라우팅. <see cref="Hook.ModifyNextEvent"/> Postfix.
///
/// <b>설계 근거 (plan §6.6, 2026-04-29 분석가 정정):</b>
/// <list type="bullet">
/// <item><c>AbstractModel.ModifyNextEvent</c> virtual override 시그니처는 <see cref="EventModel"/> 만 받음
///       → <see cref="IRunState"/> 접근 불가. <c>Hook.X += handler</c> 패턴 사용 불가.</item>
/// <item>유일 깨끗한 경로: <see cref="Hook.ModifyNextEvent"/> static 메서드 자체를 Harmony Postfix
///       (<see cref="MapPointTypePatches.Hook_ModifyGeneratedMap_Patch"/> 와 동일 패턴).</item>
/// </list>
///
/// <b>v1 단순화</b>: 환상체 1개 (EbonyQueenApple) 가정. <see cref="AbnormalityRegistry.GetAbnormalityIdForCoord"/>
/// 가 등록된 첫 ID 반환. v2 다중 환상체 도입 시 좌표별 결정론 해시 분기는 Registry 안에서 캡슐화 — 본 라우터는
/// 코드 변경 없음.
///
/// <b>Preflight</b>: <see cref="AbnormalityPreflight.Healthy"/> == false 면 단락 (게임 업데이트로
/// <see cref="Hook.ModifyNextEvent"/> 시그니처 변경 시점 등). switch 패치는 유지하되 라우팅만 비활성화.
/// </summary>
[HarmonyPatch(typeof(Hook), nameof(Hook.ModifyNextEvent))]
internal static class AbnormalityEventRouter
{
    [HarmonyPostfix]
    public static void Postfix(IRunState runState, ref EventModel __result)
    {
        // 게임 업데이트 / sentinel 충돌 시 라우팅만 비활성화 — 기존 패치들은 유지.
        if (!AbnormalityPreflight.Healthy) return;

        // 디버그/console 경로 등 진입 좌표 미확정 시 패스.
        // CurrentMapPoint 가 non-null 이면 PointType 직접 검사 (map.GetPoint 추측 호출 불필요).
        var currentPoint = runState.CurrentMapPoint;
        if (currentPoint == null) return;

        // 현재 맵 노드가 환상체 노드가 아니면 원본 currentEvent 유지.
        if (currentPoint.PointType != AbnormalityMapPointType.Abnormality) return;

        // 환상체 ID 결정 (v1 = 첫 ID, v2 = 결정론 해시).
        var abnormalityId = AbnormalityRegistry.GetAbnormalityIdForCoord(runState, currentPoint.coord);
        if (abnormalityId == null) return;

        // 등록된 환상체 EventModel 로 교체. miss 시 __result 그대로 유지.
        var newEvent = AbnormalityRegistry.GetEventForAbnormality(abnormalityId);
        if (newEvent != null)
            __result = newEvent;
    }
}
