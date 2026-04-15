using MegaCrit.Sts2.Core.Localization;

namespace TheCity;

/// <summary>
/// 모드 전역 번역 시스템.
/// 게임의 LocManager를 통해 언어별 단일 JSON 파일(thecity.json)에서 번역 조회.
///
/// 파일 위치:
///   assets/localization/eng/thecity.json
///   assets/localization/kor/thecity.json
///
/// JSON 형식:
///   { "KEY_NAME": "translated text" }
///
/// 사용:
///   Loc.Get("WRATH")           → "Wrath" 또는 "분노"
///   Loc.Format("CARD_DESC")    → DynamicVar 포맷팅 포함
///   Loc.Has("SOME_KEY")        → 키 존재 여부
/// </summary>
public static class Loc
{
    public const string Table = "thecity";

    /// <summary>번역 문자열 조회. 키가 없으면 키 자체를 반환.</summary>
    public static string Get(string key)
    {
        if (!LocString.Exists(Table, key))
            return key;

        return new LocString(Table, key).GetFormattedText();
    }

    /// <summary>키 존재 여부.</summary>
    public static bool Has(string key) => LocString.Exists(Table, key);

    /// <summary>LocString 객체 생성 (DynamicVar 등 변수 바인딩이 필요할 때).</summary>
    public static LocString Of(string key) => new(Table, key);

    /// <summary>현재 게임 언어 코드 (eng, kor 등).</summary>
    public static string Language => LocManager.Instance.Language;
}
