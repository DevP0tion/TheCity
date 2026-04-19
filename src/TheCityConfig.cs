using BaseLib.Config;

namespace TheCity;

/// <summary>
/// TheCity 모드 설정.
/// Settings → Mod Configuration에서 접근 가능.
/// static 속성으로 어디서든 TheCityConfig.PropertyName으로 접근.
/// </summary>
internal class TheCityConfig : SimpleModConfig
{
    // ── 자원 설정 ──

    /// <summary>
    /// 자원 초기값 (텍스트 입력 기반, 정수만 허용, 자릿수 제한 없음).
    /// </summary>
    [ConfigSection("ResourceSettings")]
    [ConfigTextInput("[0-9]+")]
    public static string StartingResourceValue { get; set; } = "0";

    /// <summary>StartingResourceValue를 int로 조회. 파싱 실패 시 0.</summary>
    [ConfigIgnore]
    public static int StartingResourceInt => int.TryParse(StartingResourceValue, out var v) ? v : 0;

    // ── 맵 설정 ──

    /// <summary>
    /// 환상체(Abnormality) 노드 출현 확률 (%).
    /// 각 후보 Unknown 노드마다 독립 확률로 교체 (0 = 비활성, 100 = 모든 Unknown → 환상체).
    /// 결정론적 해시로 판정하여 멀티플레이어 safe.
    /// </summary>
    [ConfigSection("MapSettings")]
    [ConfigSlider(0, 100)]
    public static int AbnormalitySpawnChance { get; set; } = 20;
}
