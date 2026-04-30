using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;
using TheCity.Abnormality.Template;

namespace TheCity.Abnormality.Battles.EbonyQueenApple;

/// <summary>
/// 흑단여왕의 사과 인카운터 — 단부위 PoC (M-A). 본체(<see cref="EbonyQueenAppleHead"/>) 만 등장.
///
/// 후속 단계에서 LeftArm/RightArm/Root 추가 + Slots 지정 + scene 파일 도입 가능. 현재는 기본 레이아웃만.
/// </summary>
public sealed class EbonyQueenAppleEncounter : AbnormalityEncounter
{
    /// <summary>환상체 식별자 — Event/Encounter/Monster 가 공유.</summary>
    public const string AbnormalityKey = "EbonyQueenApple";

    public override string AbnormalityId => AbnormalityKey;

    public override IEnumerable<MonsterModel> AllPossibleMonsters => new MonsterModel[]
    {
        ModelDb.Monster<EbonyQueenAppleHead>(),
    };

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() => new (MonsterModel, string?)[]
    {
        (ModelDb.Monster<EbonyQueenAppleHead>().ToMutable(), null),
    };
}
