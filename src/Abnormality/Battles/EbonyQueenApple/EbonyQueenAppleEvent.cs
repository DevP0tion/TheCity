using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Events;
using TheCity.Abnormality.Template;

namespace TheCity.Abnormality.Battles.EbonyQueenApple;

/// <summary>
/// 흑단여왕의 사과 이벤트 — 단일 선택지 ("APPROACH") 로 전투 진입 (M-B).
///
/// <see cref="AbnormalityRegistry"/> 에 <see cref="ModStart.ModInit"/> 에서 등록되어
/// <c>AbnormalityEventRouter</c> (M-C) 가 환상체 맵 노드 진입 시 본 이벤트로 라우팅.
///
/// v1 PoC: 옵션 1개만 — 텍스트 표시 결과는 L11 결과 (LocString 라이프사이클) 영향 가능.
/// 보상 정책 = 표준 보상 풀 (Gold + 카드 3장) — 결재 (c) C1-v1.
/// </summary>
public sealed class EbonyQueenAppleEvent : AbnormalityEvent
{
    public override string AbnormalityId => EbonyQueenAppleEncounter.AbnormalityKey;

    protected override Type EncounterType => typeof(EbonyQueenAppleEncounter);

    /// <summary>단일 선택지 — 부모 헬퍼 사용.</summary>
    protected override IReadOnlyList<EventOption> GenerateInitialOptions() => SingleApproachOption();
}
