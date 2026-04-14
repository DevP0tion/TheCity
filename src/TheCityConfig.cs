using BaseLib.Config;

namespace TheCity;

/// <summary>
/// TheCity 모드 설정.
/// Settings → Mod Configuration에서 접근 가능.
/// static 속성으로 어디서든 TheCityConfig.PropertyName으로 접근.
/// </summary>
internal class TheCityConfig : SimpleModConfig
{
    // ── 일반 설정 ──

    [ConfigSection("General")]
    public static bool EnableResourceUI { get; set; } = true;

    // ── 숫자 입력 (슬라이더) ──

    [ConfigSection("ResourceSettings")]

    [SliderRange(0, 100)]
    public static int MaxResourceValue { get; set; } = 50;

    // ── 숫자 입력 (직접 타이핑) ──

    /// <summary>
    /// 텍스트 입력 기반 숫자 설정.
    /// 1~9999 범위의 정수만 허용.
    /// </summary>
    [ConfigTextInput("[0-9]{1,4}", MaxLength = 4)]
    public static string StartingResourceValue { get; set; } = "0";

    /// <summary>StartingResourceValue를 int로 조회. 파싱 실패 시 0.</summary>
    [ConfigIgnore]
    public static int StartingResourceInt => int.TryParse(StartingResourceValue, out var v) ? v : 0;
}
