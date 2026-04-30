using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;

namespace TheCity.Abnormality.Template;

/// <summary>
/// 환상체 전용 이벤트 공통 부모. <see cref="CustomEventModel"/> 상속으로 BaseLib 자동 등록 + 자동 prefix.
///
/// <b>v1 PoC 결정 (m1.md §1.1, §2.1, §2.4):</b>
/// <list type="bullet">
/// <item><see cref="IsShared"/> = true 강제 — <c>EnterCombatWithoutExitingEvent</c> 가
///       <c>IsShared==false</c> 시 <see cref="System.InvalidOperationException"/> throw 하므로 절대 끄지 말 것.</item>
/// <item>승패 분기는 <see cref="Resume"/> 에서 표준 처리 — <see cref="OnVictory"/> / <see cref="OnDefeat"/>
///       virtual hook 만 자식 클래스에서 override (선택). 기본 동작은 victory/defeat 전용 LocString 으로
///       <see cref="EventModel.SetEventFinished"/>.</item>
/// </list>
///
/// 자식 클래스(<c>EbonyQueenAppleEvent</c>) 가 <see cref="StartAbnormalityCombat"/> 헬퍼로 전투 진입.
/// </summary>
public abstract class AbnormalityEvent : CustomEventModel
{
    /// <summary>환상체 식별자(예: "EbonyQueenApple"). Encounter/Monster 와 같은 값을 사용해 그룹핑.</summary>
    public abstract string AbnormalityId { get; }

    /// <summary>본 환상체 이벤트가 진입할 인카운터 타입. <see cref="StartAbnormalityCombat"/> 의 generic 인자로 사용.</summary>
    protected abstract Type EncounterType { get; }

    public override bool IsShared => true;

    /// <summary>
    /// 모드 로컬라이제이션 테이블 사용 (default <c>"events"</c> override).
    /// <para>
    /// 근거 (cto-lead 보강 정정 #3, sts-game-analyst Q7): <c>EventModel.LocTable</c> 는
    /// <c>public virtual</c> (<c>EventModel.cs:54</c>) → override 가능. <c>L10NLookup(string)</c> 이
    /// <c>new LocString(LocTable, key)</c> (<c>EventModel.cs:357-360</c>) 로 본 프로퍼티 사용 → 자식의
    /// lookup 이 모두 모드 테이블로 이전. 본 모드는 <c>"thecity"</c> 만 등록 — 미등록 테이블 접근 시
    /// <c>LocException</c> throw. <c>EncounterModel.L10NLookup</c> 은 static+private+<c>"encounters"</c>
    /// 하드코딩이라 override 불가 — Encounter 텍스트는 별도 트랙 (옵션 A: <c>encounters.json</c> 추가).
    /// </para>
    /// </summary>
    public override string LocTable => "thecity";

    /// <summary>
    /// 환상체 전투 진입. <see cref="EventModel.EnterCombatWithoutExitingEvent"/> 의 reflection 우회 헬퍼.
    /// generic 시그니처는 protected 라 자식 클래스는 직접 호출이 가능하나, 환상체 보상 정책(v1 = 표준 보상)
    /// 을 한 곳에 모으기 위해 부모 헬퍼로 둠.
    /// </summary>
    protected void StartAbnormalityCombat()
    {
        var encounter = ModelDb.GetByIdOrNull<EncounterModel>(ModelDb.GetId(EncounterType))?.ToMutable();
        if (encounter == null)
        {
            throw new InvalidOperationException(
                $"AbnormalityEvent: encounter type {EncounterType.FullName} not registered in ModelDb.");
        }

        // v1 (m1.md 결재 (c) C1-v1): 표준 보상 풀만 사용 → extraRewards 비움.
        // shouldResumeAfterCombat=true → 전투 종료 후 본 이벤트로 복귀해 OnVictory/OnDefeat 분기.
        EnterCombatWithoutExitingEvent(encounter, Array.Empty<Reward>(), shouldResumeAfterCombat: true);
    }

    /// <summary>
    /// 자식 클래스가 표준 옵션 1개 ("APPROACH") 만 사용할 때 쓰는 편의 메서드.
    /// 다중 선택지 / 조건부 옵션이 필요해지면 자식이 <see cref="EventModel.GenerateInitialOptions"/> 를 직접 override.
    /// </summary>
    protected IReadOnlyList<EventOption> SingleApproachOption()
    {
        return new[]
        {
            new EventOption(this, OnApproachInternal, $"{Id.Entry}.options.APPROACH"),
        };
    }

    private Task OnApproachInternal()
    {
        StartAbnormalityCombat();
        return Task.CompletedTask;
    }

    /// <summary>
    /// 전투 종료 후 본 이벤트로 복귀 시 호출 (게임측 <see cref="EventModel.Resume"/>). 표준 분기:
    /// <list type="number">
    /// <item><see cref="CombatRoom.CombatState"/> 의 적 전부 사망 → <see cref="OnVictory"/> 호출 후 victory LocString 으로 finish.</item>
    /// <item>그 외 (플레이어 사망 / 시간 초과 등) → <see cref="OnDefeat"/> 호출 후 defeat LocString 으로 finish.</item>
    /// </list>
    /// 자식 클래스는 hook 만 override 하면 되고, finish LocString 키 명세 (<c>{Id.Entry}.victory</c> /
    /// <c>{Id.Entry}.defeat</c>) 는 부모가 강제 — <c>Id.Entry</c> 는 BaseLib auto-prefix 가 클래스명을
    /// SCREAMING_SNAKE_CASE 변환한 값 (예: <c>EBONY_QUEEN_APPLE_EVENT</c>). plan §5.4.3 / §5.4.4 와 일치.
    /// </summary>
    public override async Task Resume(AbstractRoom exitedRoom)
    {
        if (exitedRoom is not CombatRoom combatRoom)
        {
            return;
        }

        // v1 단순화: 적 전부 사망 시 승리. 본체/부위 분리는 v2 이관 (plan §5.4.2 γ — MonsterModel.AfterDeath).
        bool isVictory = combatRoom.CombatState.Enemies.Count > 0
            && combatRoom.CombatState.Enemies.All(e => e.IsDead);

        if (isVictory)
        {
            await OnVictory(combatRoom);
            SetEventFinished(L10NLookup($"{Id.Entry}.victory"));
        }
        else
        {
            await OnDefeat(combatRoom);
            SetEventFinished(L10NLookup($"{Id.Entry}.defeat"));
        }
    }

    /// <summary>전투 승리 시 자식 클래스 hook. v1 default = no-op (표준 보상은 게임 본체가 처리).</summary>
    protected virtual Task OnVictory(CombatRoom combatRoom) => Task.CompletedTask;

    /// <summary>전투 패배 시 자식 클래스 hook. v1 default = no-op.</summary>
    protected virtual Task OnDefeat(CombatRoom combatRoom) => Task.CompletedTask;
}
