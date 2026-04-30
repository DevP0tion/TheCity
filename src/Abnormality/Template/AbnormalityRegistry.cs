using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace TheCity.Abnormality.Template;

/// <summary>
/// 환상체 ID(예: "EbonyQueenApple") 와 해당 환상체의 <see cref="AbnormalityEvent"/> 타입을 매핑하는 정적 레지스트리.
///
/// 등록 시점은 <see cref="ModStart.ModInit"/> — Harmony 패치 / ModelDb 초기화와 무관하게 타입 매핑만 보관.
/// 맵 노드 → 이벤트 라우팅(M-C, <c>AbnormalityEventRouter</c>)은 본 매핑을 통해 적절한
/// <see cref="EventModel"/> 인스턴스를 <see cref="ModelDb"/> 에서 조회.
///
/// **v1 단순화 (2026-04-29, plan §6.6):** 환상체 종류 = 1개. <see cref="GetAbnormalityIdForCoord"/> 가
/// 등록된 첫 ID 를 그대로 반환. v2 다중 환상체 도입 시 좌표별 결정론 해시로 분기 — 그때
/// <c>AbnormalityMapInjector</c> 의 hash 공식을 헬퍼로 추출해 재사용.
/// </summary>
public static class AbnormalityRegistry
{
    private static readonly Dictionary<string, Type> EventByAbnormalityId = new(StringComparer.Ordinal);

    /// <summary>환상체 등록. <see cref="ModStart.ModInit"/> 에서 호출.</summary>
    public static void Register<TEvent>(string abnormalityId) where TEvent : AbnormalityEvent
    {
        if (string.IsNullOrEmpty(abnormalityId))
            throw new ArgumentException("abnormalityId must be non-empty", nameof(abnormalityId));
        EventByAbnormalityId[abnormalityId] = typeof(TEvent);
    }

    /// <summary>
    /// 환상체 ID 로 등록된 EventModel 조회. 미등록이면 null. **반환은 canonical 인스턴스** —
    /// <see cref="EventModel.ToMutable"/> 호출 절대 금지.
    /// <para>
    /// 근거 (cto-lead 보강 정정 #2, sts-game-analyst Q5): <c>EventRoom</c> 생성자
    /// (<c>EventRoom.cs:32</c>) 가 <c>eventModel.AssertCanonical()</c> 를 호출 → mutable 인스턴스를
    /// 받으면 즉시 <c>MutableModelException</c> throw. <see cref="Hook.ModifyNextEvent"/> 반환 경로는
    /// canonical 만 허용. <c>StartAbnormalityCombat</c> (Encounter 인자) 의 <c>ToMutable()</c> 은
    /// 별개 경로라 무관.
    /// </para>
    /// </summary>
    public static EventModel? GetEventForAbnormality(string abnormalityId)
    {
        if (!EventByAbnormalityId.TryGetValue(abnormalityId, out var type)) return null;
        var modelId = ModelDb.GetId(type);
        // ToMutable() 호출 금지 — EventRoom 생성자의 AssertCanonical 회피.
        return ModelDb.GetByIdOrNull<EventModel>(modelId);
    }

    /// <summary>
    /// 진입 중인 맵 노드 좌표에 해당하는 환상체 ID 결정.
    ///
    /// <b>v1 단순화 (plan §6.6.2)</b>: 환상체 1개 (EbonyQueenApple) 가정 → 등록된 첫 ID 반환.
    /// 미등록 시 null.
    /// <b>v2 확장 시</b>: 결정론 해시 (<c>AbnormalityMapInjector</c> 동일 공식) 로
    /// <c>RegisteredIds[hash % count]</c> 분기. 그때 hash 공식을 공통 헬퍼로 추출.
    /// </summary>
    public static string? GetAbnormalityIdForCoord(IRunState runState, MapCoord coord)
    {
        _ = runState;
        _ = coord;
        if (EventByAbnormalityId.Count == 0) return null;
        return EventByAbnormalityId.Keys.First();
    }

    /// <summary>등록된 환상체 ID 전체 (테스트/디버깅용).</summary>
    public static IReadOnlyCollection<string> RegisteredIds => EventByAbnormalityId.Keys;
}
