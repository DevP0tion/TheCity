// Design Ref: doc/plan/card-sin-attribution-brainstorm.md §3 Option 2
// 카드 플레이 시 해당 카드의 Sin 속성을 +1 획득.
using System;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Hooks;

namespace TheCity.Resource;

/// <summary>
/// <see cref="Hook.AfterCardPlayed"/> Prefix — 카드 1장 플레이 → 해당 Sin +1.
/// Prefix 사용: AfterCardPlayed는 async Task 메서드이므로 Postfix 타이밍이 불안정함.
/// </summary>
[HarmonyPatch(typeof(Hook), nameof(Hook.AfterCardPlayed))]
public static class Hook_AfterCardPlayed_Patch
{
    public static void Prefix(CombatState combatState, CardPlay cardPlay)
    {
        try
        {
            if (cardPlay.PlayIndex != 0) return;

            var owner = cardPlay.Card.Owner;
            if (owner != null && !LocalContext.IsMe(owner)) return;

            var sin = cardPlay.Card.GetSin();
            if (sin == null)
            {
                GD.Print($"[{ModStart.ModId}] CardPlay: {cardPlay.Card.Id.Entry} → Sin=null (no mapping)");
                return;
            }

            if (!SharedResourceManager.IsActive)
            {
                GD.PrintErr($"[{ModStart.ModId}] CardPlay: SharedResourceManager not active!");
                return;
            }

            sin.Value.Modify(+1, sync: true);
            GD.Print($"[{ModStart.ModId}] CardPlay: {cardPlay.Card.Id.Entry} → {sin.Value} +1 (now {sin.Value.Get()})");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[{ModStart.ModId}] CardPlay error: {ex.Message}");
        }
    }
}
