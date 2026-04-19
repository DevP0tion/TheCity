// Design Ref: doc/plan/card-sin-attribution-brainstorm.md §3 Option 2
// 카드 플레이 시 해당 카드의 Sin 속성을 +1 획득.
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Hooks;

namespace TheCity.Resource;

/// <summary>
/// <see cref="Hook.AfterCardPlayed"/> Postfix — 카드 1장 플레이 → 해당 Sin +1.
///
/// 가드:
/// 1. <see cref="CardPlay.PlayIndex"/> != 0 일 경우 skip — Dupe/Havoc/Whirlwind 등
///    멀티 플레이 루프 반복에서 같은 카드가 여러 번 훅을 발화하는 것을 방지.
/// 2. <see cref="CardPlay.IsAutoPlay"/>: 현재는 +1 주는 쪽으로 둠 (설계 결정).
///    추후 밸런스 조정 시 여기서 skip 토글.
/// 3. MP: 로컬 플레이어 카드만 +1 — 원격 피어 플레이가 로컬 훅을 발화하는지
///    미검증이라, <see cref="LocalContext.IsMe"/> 가드로 중복 방지.
///
/// 조회: <see cref="CardSinRegistry.GetSin"/> 확장메서드.
/// 미등록 카드는 null 반환 → 조용히 skip (로그 없음, 오류 없음).
///
/// Sin.Modify(+1, sync: true):
/// - SharedResourceManager 에 증가 적용
/// - 멀티플레이어 환경에서 SharedResourceSync 메시지로 peer 동기화
/// </summary>
[HarmonyPatch(typeof(Hook), nameof(Hook.AfterCardPlayed))]
public static class Hook_AfterCardPlayed_Patch
{
    public static void Postfix(CardPlay cardPlay)
    {
        // Guard 1: 리플레이/멀티플레이 루프 첫 번째만 카운트
        if (cardPlay.PlayIndex != 0) return;

        // Guard 2: MP — 원격 피어 카드 플레이가 로컬 훅을 발화하는지 미검증 상태.
        // 로컬 플레이어 카드만 처리하여 양측 peer에서 중복 +1 을 방지.
        // (싱글플레이어일 경우 owner 는 로컬 자신이므로 항상 통과)
        var owner = cardPlay.Card.Owner;
        if (owner != null && !LocalContext.IsMe(owner)) return;

        var sin = cardPlay.Card.GetSin();
        if (sin == null) return;  // 미등록 카드 (매핑 누락 또는 외부 mod 신규 카드)

        sin.Value.Modify(+1, sync: true);
    }
}
