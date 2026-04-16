using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Runs;

namespace TheCity.Map;

/// <summary>
/// 생성된 <see cref="ActMap"/>에 Abnormality 노드를 주입.
///
/// 각 후보 노드(Unknown + CanBeModified)마다 독립 확률(<see cref="TheCityConfig.AbnormalitySpawnChance"/>)로 교체.
/// 확률 판정은 결정론적 해시 기반 — 멀티플레이어에서 peer 간 동일 결과 보장.
/// <see cref="Hook.ModifyGeneratedMap"/> Postfix에서 호출됨.
/// </summary>
public static class AbnormalityMapInjector
{
    private static bool _firstCallLogged;

    /// <summary>
    /// 각 후보에 대해 독립 확률로 Abnormality 적용. 이미 주입된 맵이면 멱등(idempotent)으로 스킵.
    /// </summary>
    public static ActMap Inject(IRunState runState, ActMap map, int actIndex)
    {
        if (!AbnormalityPreflight.Healthy) return map;

        LogFirstCall();

        int chance = TheCityConfig.AbnormalitySpawnChance;
        if (chance <= 0) return map;

        // 멱등: 이미 주입된 맵이면 스킵
        if (map.GetAllMapPoints().Any(p => p.PointType == AbnormalityMapPointType.Abnormality))
        {
            return map;
        }

        // 후보: Unknown(=?) 노드 중 수정 가능
        var candidates = map.GetAllMapPoints()
            .Where(p => p.PointType == MapPointType.Unknown && p.CanBeModified)
            .ToList();

        if (candidates.Count == 0)
        {
            GD.Print($"[{ModStart.ModId}] AbnormalityInjector: no modifiable Unknown points in act {actIndex}, skipped.");
            return map;
        }

        // 각 후보마다 독립 결정론적 확률 판정
        ulong seed = (ulong)runState.Rng.Seed;
        int placed = 0;
        foreach (var point in candidates)
        {
            // hash(seed, actIndex, coord) % 100 < chance
            unchecked
            {
                ulong mix = seed;
                mix ^= (ulong)actIndex * 0x9E3779B1UL;
                mix ^= (ulong)point.coord.col * 0x27D4EB2DUL;
                mix ^= (ulong)point.coord.row * 0x165667B1UL;
                // 추가 믹싱 (xorshift 1회)
                mix ^= mix >> 33;
                mix *= 0xff51afd7ed558ccdUL;
                mix ^= mix >> 33;

                int roll = (int)(mix % 100UL);
                if (roll < chance)
                {
                    point.PointType = AbnormalityMapPointType.Abnormality;
                    point.CanBeModified = false;  // 후속 Late 패스에서 변경 방지
                    placed++;
                }
            }
        }

        GD.Print($"[{ModStart.ModId}] AbnormalityInjector: act {actIndex}, chance={chance}%, candidates={candidates.Count}, placed={placed}.");
        return map;
    }

    private static void LogFirstCall()
    {
        if (_firstCallLogged) return;
        _firstCallLogged = true;
        GD.Print($"[{ModStart.ModId}] AbnormalityInjector: first invocation (Hook.ModifyGeneratedMap patch active).");
    }
}
