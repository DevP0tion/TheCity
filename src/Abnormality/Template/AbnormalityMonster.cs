using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models.Powers;

namespace TheCity.Abnormality.Template;

/// <summary>
/// 환상체 부위 공통 부모. <see cref="CustomMonsterModel"/> 상속으로 BaseLib 자동 등록 + 자동 prefix 적용.
/// 같은 환상체의 부위들은 <see cref="AbnormalityId"/> 로 그룹핑.
///
/// <b>본체 / 부위 구분 (m1.md §1.5):</b>
/// <list type="bullet">
/// <item>본체(Primary): <see cref="IsSecondaryPart"/> = false (default). MinionPower 미적용.</item>
/// <item>부위(Secondary): <see cref="IsSecondaryPart"/> = true override. <see cref="AfterAddedToRoom"/> 에서
///       자기 자신에게 <see cref="MinionPower"/> 적용 → <c>OwnerIsSecondaryEnemy=true</c> →
///       <see cref="MegaCrit.Sts2.Core.Combat.CombatManager.IsEnding"/> 의 본체 카운트에서 제외.</item>
/// </list>
/// 본체만 죽으면 부위 생존 여부와 무관하게 전투 자동 종료(바닐라 동작).
///
/// 단부위 PoC(M-A) 단계에서는 본 클래스의 <see cref="AfterAddedToRoom"/> 분기가 호출돼도 영향 없음
/// — 머리 단일 출현, IsSecondaryPart=false. 부위 분리는 후속 단계에서 도입.
/// </summary>
public abstract class AbnormalityMonster : CustomMonsterModel
{
    /// <summary>환상체 식별자(예: "EbonyQueenApple"). 같은 환상체의 부위들끼리 일치해야 함.</summary>
    public abstract string AbnormalityId { get; }

    /// <summary>true 면 부위(Secondary), false 면 본체(Primary). 기본 false.</summary>
    protected virtual bool IsSecondaryPart => false;

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        if (IsSecondaryPart)
            await PowerCmd.Apply<MinionPower>(base.Creature, 1m, base.Creature, null);
    }
}
