using MegaCrit.Sts2.Core.Map;

namespace TheCity.Map;

/// <summary>
/// 환상체 노드의 sentinel MapPointType 값.
///
/// BaseLib에 런타임 enum 주입 API(<c>[CustomEnum]</c>)가 존재하지 않으므로
/// 정수 sentinel 200을 <see cref="MapPointType"/>으로 캐스팅하여 사용.
///
/// - <see cref="SerializableMapPoint"/>는 <c>WriteInt((int)PointType, 8)</c>로 직렬화 (최대 255)
/// - 현재 enum은 0..8까지 사용 (Unassigned=0 ... Ancient=8) — 200은 충분한 여유
/// - 게임 업데이트로 200이 신규 enum 값과 충돌하면 <see cref="AbnormalityPreflight"/>가 감지
/// </summary>
public static class AbnormalityMapPointType
{
    public const MapPointType Abnormality = (MapPointType)200;

    /// <summary>로컬라이제이션 키 prefix. <c>{LocPrefix}.title</c>, <c>{LocPrefix}.description</c>.</summary>
    public const string LocPrefix = "ROOM_ABNORMALITY";

    /// <summary>
    /// 아이콘 basename. 게임 atlas에 실제 basename 주입 불가 → vanilla Unknown 아이콘 키로 위장하고
    /// <see cref="MegaCrit.Sts2.Core.Nodes.Screens.Map.NNormalMapPoint"/>.UpdateIcon Postfix에서
    /// 별 텍스처로 교체.
    /// </summary>
    public const string IconBasename = "map_unknown";

    /// <summary>
    /// 상수가 실제로 참조되는 시점 확인용 (const는 정적 초기화가 없지만, 호출 자체가 심볼 유지).
    /// <see cref="ModStart.ModInit"/>에서 <see cref="HarmonyLib.Harmony.PatchAll"/> 전에 호출.
    /// </summary>
    public static void EnsureLoaded() { _ = Abnormality; }
}
