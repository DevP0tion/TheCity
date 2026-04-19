using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Models;

namespace TheCity.Resource;

/// <summary>
/// 런타임 카드 → Sin 조회. <see cref="CardSinMap"/>(빌드 시 자동 생성)에서 데이터를
/// 소유권 이전(<see cref="CardSinMap.TakeOwnership"/>) 방식으로 받아옴.
///
/// 이전 후 CardSinMap의 정적 필드는 null이 되고, 이 레지스트리만이 Dictionary를 참조.
/// 캐시/인덱스 같은 추가 구조는 이 파일에서 관리 (CardSinMap.g.cs는 순수 데이터 vehicle).
/// </summary>
public static class CardSinRegistry
{
    private static Dictionary<string, Sin>? _byCardId;

    /// <summary>ModInit에서 1회 호출. CardSinMap 데이터를 흡수하고 원본을 해제.</summary>
    public static void LoadOnce()
    {
        if (_byCardId != null) return;  // 이미 로드됨
        if (CardSinMap.IsReleased) return;  // 이미 다른 경로로 해제됨

        _byCardId = CardSinMap.TakeOwnership();
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
