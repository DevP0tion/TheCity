using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils.NodeFactories;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using TheCity.Abnormality.Template;

namespace TheCity.Abnormality.Battles.EbonyQueenApple;

/// <summary>
/// 흑단여왕의 사과 — 본체(머리). 단부위 PoC (M-A) 의 유일한 부위.
///
/// 본 PoC 단계에서는 본체만 등장 → <see cref="AbnormalityMonster.IsSecondaryPart"/> 는 default(false) 유지.
/// 후속 단계에서 LeftArm/RightArm/Root 추가 시 그쪽 클래스들이 IsSecondaryPart=true 로 override.
///
/// 시각화는 <see cref="CreateCustomVisuals"/> 에서 단일 PNG (<c>boss.png</c>) 직접 참조 — m1.md (d-2) Mon-PNG.
/// HP 와 Move 풀은 PoC 용 placeholder — 빌드 통과 + 콘솔 <c>fight</c> 로 등장 확인 가능한 최소값.
/// 본격 AI 패턴(VainFruit / PaleStem 등) 은 후속 단계에서 도입.
/// </summary>
public sealed class EbonyQueenAppleHead : AbnormalityMonster
{
    public override string AbnormalityId => EbonyQueenAppleEncounter.AbnormalityKey;

    // Placeholder HP — 본격 튜닝은 후속 단계.
    public override int MinInitialHp => 60;
    public override int MaxInitialHp => 70;

    /// <summary>
    /// 단일 PNG 이미지로 본체 시각화 생성. BaseLib <c>NCreatureVisualsFactory</c> 가
    /// PNG 경로 → <c>Texture2D</c> 로드 → <c>NCreatureVisuals</c>(Bounds + Sprite2D) 생성.
    /// </summary>
    public override NCreatureVisuals? CreateCustomVisuals()
    {
        return NodeFactory<NCreatureVisuals>.CreateFromResource(
            "res://assets/sprites/abnormalities/ebony_queen_apple/boss.png");
    }

    /// <summary>
    /// PoC idle move — 효과 없는 wait state 가 자기 자신을 FollowUp 으로 가리키는 무한 루프.
    /// 인텐트 배열 빈 채로 — 의도(intent) 표시 없음. 본격 AI 패턴은 후속 단계.
    /// </summary>
    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var idle = new MoveState("IDLE", IdleAction);
        idle.FollowUpState = idle;
        return new MonsterMoveStateMachine(new[] { idle }, idle);
    }

    private static Task IdleAction(IReadOnlyList<Creature> targets) => Task.CompletedTask;
}
