using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Rooms;

namespace TheCity.Abnormality.Template;

/// <summary>
/// 환상체 전용 인카운터 공통 부모. <see cref="CustomEncounterModel"/> 상속으로 BaseLib 자동 등록 + 자동 prefix.
///
/// <b>공통 결정 (m1.md §1.1, §2.3):</b>
/// <list type="bullet">
/// <item><c>RoomType = Monster</c> — 일반 전투 룸 처리. (Boss 환상체는 별도 plan 에서 RoomType 변경 검토.)</item>
/// <item><see cref="ShouldGiveRewards"/> = true (default 유지) — 보상은 표준 보상 풀(Gold + 카드 3장) 사용.</item>
/// <item><see cref="IsValidForAct"/> = true (단순 PoC). 후속 단계에서 act 별 분리 시 override.</item>
/// <item>승패 플래그 + <c>SaveCustomState</c>/<c>LoadCustomState</c> 왕복은 후속 단계(M-B)에서
///       <see cref="AbnormalityEvent.Resume"/> 와 함께 도입. 본 부모에는 미정의.</item>
/// </list>
///
/// 단부위 PoC(M-A) 단계에서는 본 클래스의 자식이 <c>GenerateMonsters</c> / <c>AllPossibleMonsters</c> 만
/// 정의하면 부모의 RoomType=Monster + IsValidForAct=true 가 그대로 적용돼 콘솔 <c>fight</c> 호출이 가능해진다.
/// </summary>
public abstract class AbnormalityEncounter : CustomEncounterModel
{
    /// <summary>환상체 식별자(예: "EbonyQueenApple"). 이벤트/몬스터와 같은 값을 사용해 그룹핑.</summary>
    public abstract string AbnormalityId { get; }

    protected AbnormalityEncounter() : base(RoomType.Monster) { }

    /// <summary>모든 act 에서 등장 허용 (PoC). act 별 분리 시 override.</summary>
    public override bool IsValidForAct(MegaCrit.Sts2.Core.Models.ActModel act) => true;
}
