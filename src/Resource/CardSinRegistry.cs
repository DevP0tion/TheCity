using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace TheCity.Resource;

/// <summary>
/// 런타임 카드 → Sin 조회.
///
/// 우선순위:
/// 1. 정적 맵 (CardSinMap.g.cs — 577개 바닐라 카드)
/// 2. 동적 해석 (Keyword → Tag → Type 순)
///
/// 매핑 테이블:
///   분노(Wrath)   ← 소멸(Exhaust) / 타격(Strike)
///   색욕(Lust)    ← 단도(Shiv)
///   나태(Sloth)   ← 보존(Retain) / 수비(Defend)
///   탐식(Gluttony) ← 하수인(Minion) / 골골이(OstyAttack)
///   우울(Gloom)   ← 휘발성(Ethereal) / 상태이상(Status)
///   오만(Pride)   ← 선천성(Innate) / 파워(Power)
///   질투(Envy)    ← 교활(Sly) / 저주(Curse)
/// </summary>
public static class CardSinRegistry
{
    private static Dictionary<string, Sin>? _byCardId;

    /// <summary>ModInit에서 1회 호출.</summary>
    public static void LoadOnce()
    {
        if (_byCardId != null) return;
        if (CardSinMap.IsReleased) return;

        _byCardId = CardSinMap.TakeOwnership();
        GD.Print($"[{ModStart.ModId}] CardSinRegistry: {_byCardId.Count} entries loaded; CardSinMap released.");
    }

    /// <summary>카드의 Sin 조회. 정적 맵 → 동적 해석 순.</summary>
    public static Sin? GetSin(this CardModel card)
    {
        // 1. 정적 맵
        if (_byCardId != null && _byCardId.TryGetValue(card.Id.Entry, out var sin))
            return sin;

        // 2. 동적 해석 (Keyword → Tag → Type)
        return ResolveDynamic(card);
    }

    /// <summary>카드 ID(string) 기반 조회.</summary>
    public static bool TryGetSin(string cardId, out Sin sin)
    {
        if (_byCardId != null) return _byCardId.TryGetValue(cardId, out sin);
        sin = default;
        return false;
    }

    /// <summary>Keyword → Tag → Type 순으로 동적 Sin 해석.</summary>
    private static Sin? ResolveDynamic(CardModel card)
    {
        // Keyword (가장 구체적)
        if (card.Keywords.Contains(CardKeyword.Exhaust))  return Sin.Wrath;
        if (card.Keywords.Contains(CardKeyword.Sly))      return Sin.Envy;
        if (card.Keywords.Contains(CardKeyword.Ethereal))  return Sin.Gloom;
        if (card.Keywords.Contains(CardKeyword.Innate))    return Sin.Pride;
        if (card.Keywords.Contains(CardKeyword.Retain))    return Sin.Sloth;

        // Tag
        if (card.Tags.Contains(CardTag.Shiv))       return Sin.Lust;
        if (card.Tags.Contains(CardTag.Minion))      return Sin.Gluttony;
        if (card.Tags.Contains(CardTag.OstyAttack))  return Sin.Gluttony;
        if (card.Tags.Contains(CardTag.Strike))      return Sin.Wrath;
        if (card.Tags.Contains(CardTag.Defend))      return Sin.Sloth;

        // Type (가장 광범위)
        if (card.Type == CardType.Status) return Sin.Gloom;
        if (card.Type == CardType.Power)  return Sin.Pride;
        if (card.Type == CardType.Curse)  return Sin.Envy;

        return null;
    }

    /// <summary>등록된 정적 맵 항목 수.</summary>
    public static int Count => _byCardId?.Count ?? 0;
}
