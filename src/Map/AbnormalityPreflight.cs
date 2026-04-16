using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.sts2.Core.Nodes.TopBar; // 게임측 네임스페이스 표기 (소문자 sts2)

namespace TheCity.Map;

/// <summary>
/// 환상체 기능 활성화 가능 여부를 <see cref="ModStart.ModInit"/> 시점에 검증.
///
/// - Sentinel 200이 이미 게임 enum 값이면 비활성 (충돌 방지)
/// - 패치 대상 메서드들이 reflection으로 실제 존재하는지 확인 (게임 업데이트 리네임 감지)
///
/// <see cref="Healthy"/> == false여도 switch 패치들은 설치 유지 (기존 세이브의 sentinel 값 매핑 필요).
/// <see cref="AbnormalityMapInjector"/>만 <see cref="Healthy"/> 체크로 게이트됨.
/// </summary>
public static class AbnormalityPreflight
{
    public static bool Healthy { get; private set; }
    public static IReadOnlyList<string> FailureReasons { get; private set; } = ImmutableArray<string>.Empty;

    public static void Run()
    {
        var reasons = new List<string>();

        // Sentinel 값 충돌 체크
        if (Enum.IsDefined(typeof(MapPointType), (int)AbnormalityMapPointType.Abnormality))
        {
            reasons.Add($"Sentinel {(int)AbnormalityMapPointType.Abnormality} is now a defined MapPointType value — bump sentinel");
        }

        // 패치 대상 메서드 존재 검증
        CheckMethod(reasons, typeof(RunManager), "RollRoomTypeFor");
        CheckMethod(reasons, typeof(NNormalMapPoint), "IconName");
        CheckMethod(reasons, typeof(NTopBarRoomIcon), "GetHoverTipPrefixForRoomType");
        CheckMethod(reasons, typeof(NMapPointHistoryHoverTip), "_Ready");
        CheckMethod(reasons, typeof(ImageHelper), "GetRoomIconPath");
        CheckMethod(reasons, typeof(ImageHelper), "GetRoomIconOutlinePath");
        CheckMethod(reasons, typeof(Hook), nameof(Hook.ModifyGeneratedMap));

        Healthy = reasons.Count == 0;
        FailureReasons = reasons.ToImmutableArray();

        if (!Healthy)
        {
            GD.PushError($"[{ModStart.ModId}] Abnormality preflight FAILED: {string.Join("; ", reasons)}");
        }
        else
        {
            GD.Print($"[{ModStart.ModId}] Abnormality preflight OK.");
        }
    }

    private static void CheckMethod(List<string> reasons, Type type, string name)
    {
        if (AccessTools.Method(type, name) == null)
        {
            reasons.Add($"{type.FullName}.{name} not found");
        }
    }
}
