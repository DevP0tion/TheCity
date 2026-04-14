# STS2 API 참조 (세션 내 확인된 사항)

## 게임 훅 시스템
`Hook.cs`에 정의. `AbstractModel`을 상속한 카드/유물/파워에서 오버라이드.

### Before 훅 (사전)
BeforeCombatStart, BeforeCardPlayed, BeforeTurnEnd, BeforeHandDraw,
BeforeDamageReceived, BeforeBlockGained, BeforePowerAmountChanged,
BeforeCardRemoved, BeforeRoomEntered, BeforeRewardsOffered, BeforePotionUsed,
BeforeFlush, BeforePlayPhaseStart, BeforeCardAutoPlayed, BeforeDeath

### After 훅 (사후)
AfterCombatEnd, AfterCombatVictory, AfterCardPlayed, AfterCardDrawn,
AfterCardDiscarded, AfterCardExhausted, AfterCardRetained,
AfterDamageReceived, AfterDamageGiven, AfterBlockGained, AfterBlockBroken,
AfterPowerAmountChanged, AfterTurnEnd, AfterEnergyReset, AfterHandEmptied,
AfterShuffle, AfterRoomEntered, AfterRewardTaken, AfterItemPurchased,
AfterPotionUsed, AfterRestSiteHeal, AfterRestSiteSmith, AfterGoldGained,
AfterDeath, AfterCreatureAddedToCombat, AfterOrbChanneled, AfterOrbEvoked

### Modify 훅 (값 변환)
ModifyDamage, ModifyBlock, ModifyHandDraw, ModifyMaxEnergy,
ModifyEnergyCostInCombat, ModifyCardRewardOptions, ModifyMerchantPrice 등

### Should 훅 (불리언 게이트)
ShouldDie, ShouldDraw, ShouldPlay, ShouldFlush 등

## CombatManager
- `CombatManager.SetUpCombat(CombatState state)` — 전투 시작
- `CombatManager.EndCombatInternal()` — 전투 종료 (async Task)
- `CombatManager.Instance.CombatSetUp` — event Action<CombatState>
- `CombatManager.Instance.CombatEnded` — event Action<CombatRoom>
- `CombatManager.Instance.CombatWon` — event Action<CombatRoom>

## NCombatRoom
- `NCombatRoom.Instance` — 현재 전투 방 (static)
- `NCombatRoom.Ui` — NCombatUi (HUD)
- `NCombatRoom.CombatVfxContainer` — VFX 컨테이너
- `_Ready()`에 Harmony Postfix로 UI 주입 가능
- 주입 대상: NCombatRoom, NMapRoom, NShopRoom, NRestSiteRoom, NEventRoom

## RunManager
- `RunManager.Instance` — 현재 런
- `RunManager.Instance.NetService` — INetGameService (null이면 싱글)
- `RunManager.Instance.GetPlayer(ulong ownerId)` — 플레이어 조회

## LocalContext
- `LocalContext.IsMe(Creature)` — 로컬 플레이어의 크리처인지
- `GetLocalPlayerId` 존재 (정확한 시그니처 MCP로 확인 필요)

## BaseLib
- `SimpleModConfig` — 모드 설정 UI 자동 생성 (namespace: `BaseLib.Config`)
- `ModConfigRegistry.Register(modId, config)` — 설정 등록
- `CustomEventModel` — 커스텀 이벤트 (IsShared, CalculateVars, GenerateInitialOptions)
- `CustomCardModel`, `CustomRelicModel`, `CustomPowerModel` 등

## 카드 작성 패턴
```csharp
[Pool(typeof(IroncladCardPool))]
public sealed class MyCard : CardModel
{
    public override CardType Type => CardType.Attack;
    public override CardRarity Rarity => CardRarity.Common;
    public override TargetType TargetType => TargetType.AnyEnemy;
    public override CardEnergyCost EnergyCost => 2;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(12m, ValueProp.Move),
    };

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this).Targeting(play.Target).Execute(ctx);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4m);
    }
}
```

## DynamicVar 종류
| 클래스 | DynamicVars 접근 | 로컬라이제이션 |
|--------|-----------------|-------------|
| `DamageVar(Xm, ValueProp.Move)` | `.Damage` | `{Damage}` |
| `BlockVar(Xm, ValueProp.Move)` | `.Block` | `{Block}` |
| `MagicVar(Xm)` | `.MagicNumber` | `{MagicNumber}` |
| `PowerVar<T>(Xm)` | `.PowerName` | `{PowerName}` |

## 호스트 판별 (⚠ MCP로 확인 필요)
`strings sts2.dll` 결과:
- `GetLocalPlayerId`, `get_LocalPlayerId`
- `AddLocalHostPlayer`, `AddLocalHostPlayerInternal`
- `IsMultiplayer`

MCP에서 확인할 쿼리:
```
search_game_code: "AddLocalHostPlayer|GetLocalPlayerId|IsHost"
get_entity_source: "LocalContext"
```
