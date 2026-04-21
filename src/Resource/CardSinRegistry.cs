using System.Collections.Generic;
using System.Text.RegularExpressions;
using Godot;
using MegaCrit.Sts2.Core.Models;

namespace TheCity.Resource;

/// <summary>
/// 런타임 카드 → Sin 조회. <see cref="CardSinMap"/>(빌드 시 자동 생성)에서 데이터를
/// 소유권 이전(<see cref="CardSinMap.TakeOwnership"/>) 방식으로 받아옴.
///
/// JSON의 PascalCase ID를 게임의 UPPER_SNAKE_CASE(ModelId.Entry)로 변환하여 저장.
/// </summary>
public static class CardSinRegistry
{
    private static Dictionary<string, Sin>? _byCardId;

    // StringHelper.Slugify 복제: PascalCase → UPPER_SNAKE_CASE
    private static readonly Regex CamelCaseRx = new(@"([A-Za-z0-9]|\G(?!^))([A-Z])", RegexOptions.Compiled);
    private static readonly Regex SpecialCharRx = new(@"[^A-Z0-9_]", RegexOptions.Compiled);

    private static string Slugify(string txt)
    {
        var s = CamelCaseRx.Replace(txt.Trim(), "$1_$2");
        return SpecialCharRx.Replace(s.ToUpperInvariant().Replace(" ", "_"), "");
    }

    /// <summary>ModInit에서 1회 호출. CardSinMap 데이터를 흡수하고 원본을 해제.</summary>
    public static void LoadOnce()
    {
        if (_byCardId != null) return;
        if (CardSinMap.IsReleased) return;

        var raw = CardSinMap.TakeOwnership();
        // PascalCase → UPPER_SNAKE_CASE 변환
        _byCardId = new Dictionary<string, Sin>(raw.Count);
        foreach (var kvp in raw)
        {
            _byCardId[Slugify(kvp.Key)] = kvp.Value;
        }
        GD.Print($"[{ModStart.ModId}] CardSinRegistry: {_byCardId.Count} entries loaded; CardSinMap released.");
    }

    /// <summary>카드의 Sin 조회. 미등록 카드는 null.</summary>
    public static Sin? GetSin(this CardModel card)
    {
        if (_byCardId == null) return null;
        var entry = card.Id.Entry;
        return _byCardId.TryGetValue(entry, out var sin) ? sin : (Sin?)null;
    }

    /// <summary>카드 ID(string) 기반 조회 — CardModel 없는 경로용.</summary>
    public static bool TryGetSin(string cardId, out Sin sin)
    {
        if (_byCardId != null) return _byCardId.TryGetValue(cardId, out sin);
        sin = default;
        return false;
    }

    /// <summary>등록된 전체 항목 수 (디버깅/로깅용).</summary>
    public static int Count => _byCardId?.Count ?? 0;
}
