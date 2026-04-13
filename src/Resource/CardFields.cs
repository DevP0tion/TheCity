using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;

namespace TheCity.Resource;

/// <summary>
/// CardModel에 커스텀 정수형 속성을 부착.
/// 인스턴스별 Dictionary 기반 — 게임 코드 수정 없이 동작.
/// </summary>
public static class CardFields
{
    private static readonly Dictionary<CardModel, int> _cityValue = new();

    /// <summary>카드의 CityValue 조회. 미설정 시 0.</summary>
    public static int GetCityValue(this CardModel card)
    {
        return _cityValue.TryGetValue(card, out var value) ? value : 0;
    }

    /// <summary>카드의 CityValue 설정.</summary>
    public static void SetCityValue(this CardModel card, int value)
    {
        _cityValue[card] = value;
    }

    /// <summary>전투 종료 등 정리 시 호출.</summary>
    public static void ClearAll()
    {
        _cityValue.Clear();
    }
}
