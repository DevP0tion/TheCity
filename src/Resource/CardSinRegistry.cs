using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace TheCity.Resource;

/// <summary>
/// 런타임 카드 → Sin 조회.
///
/// 우선순위:
/// 1. 정적 맵 (CardSinMap.g.cs — 빌드 시 자동 생성, 577개 바닐라 카드)
/// 2. 동적 태그 기반 (CardTag → Sin, 겹치지 않는 항목만)
///    - Shiv → Lust (단도 → 색욕)
///    - Minion → Gluttony (하수인 → 탐식)
///    - OstyAttack → Gluttony (골골이 공격 → 탐식)
/// </summary>
public static class CardSinRegistry
{
    private static Dictionary<string, Sin>? _byCardId;

    /// <summary>ModInit에서 1회 호출. CardSinMap 데이터를 흡수하고 원본을 해제.</summary>
    public static void LoadOnce()
    {
        if (_byCardId != null) return;
        if (CardSinMap.IsReleased) return;

        _byCardId = CardSinMap.TakeOwnership();
        GD.Print($"[{ModStart.ModId}] CardSinRegistry: {_byCardId.Count} entries loaded; CardSinMap released.");
    }

    /// <summary>카드의 Sin 조회. 정적 맵 → 태그 폴백 순.</summary>
    public static Sin? GetSin(this CardModel card)
    {
        // 1. 정적 맵 조회
        if (_byCardId != null)
        {
            var entry = card.Id.Entry;
            if (_byCardId.TryGetValue(entry, out var sin))
                return sin;
        }

        // 2. 태그 기반 폴백 (겹치지 않는 항목만)
        return ResolveByTag(card);
    }

    /// <summary>카드 ID(string) 기반 조회 — CardModel 없는 경로용.</summary>
    public static bool TryGetSin(string cardId, out Sin sin)
    {
        if (_byCardId != null) return _byCardId.TryGetValue(cardId, out sin);
        sin = default;
        return false;
    }

    /// <summary>CardTag → Sin 동적 해석. 겹치지 않는 항목만.</summary>
    private static Sin? ResolveByTag(CardModel card)
    {
        if (card.Tags.Contains(CardTag.Shiv))       return Sin.Lust;
        if (card.Tags.Contains(CardTag.Minion))      return Sin.Gluttony;
        if (card.Tags.Contains(CardTag.OstyAttack))  return Sin.Gluttony;
        return null;
    }

    /// <summary>등록된 전체 항목 수 (디버깅/로깅용).</summary>
    public static int Count => _byCardId?.Count ?? 0;
}
