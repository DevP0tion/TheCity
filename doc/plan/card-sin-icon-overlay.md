# 카드 Sin 속성 아이콘 오버레이

## 상태: ❌ 미구현

## 목표
카드 렌더링 시 카드 테두리 **우측 상단**에 해당 카드의 Sin 속성 아이콘을 표시.

## 구현 접근법

### 1단계: 게임 코드 조사 (MCP 필요)
```
# 카드 디스플레이 노드 클래스 찾기
search_game_code: "class NCard|class NHandCard|class NCardView|class NCardFront"

# 카드 씬 구조 (Godot scene tree)
search_game_assets: "card*.tscn"

# 카드 _Ready / 초기화 흐름
get_entity_source: "NCardFront" 또는 발견된 클래스명
```

### 2단계: Harmony 패치 설계
카드 디스플레이 노드의 `_Ready()` Postfix에서 TextureRect 자식 노드 추가.

```csharp
// 예상 구조 (실제 클래스명은 MCP 조사 후 확정)
[HarmonyPatch(typeof(NCardDisplay), "_Ready")]
public static class CardSinIconPatch
{
    public static void Postfix(NCardDisplay __instance)
    {
        var card = __instance.CardModel; // 실제 프로퍼티명 확인 필요
        var sin = card.GetSin();
        if (sin == null) return;

        var icon = new TextureRect();
        icon.Name = "SinIcon";
        icon.Texture = SinIconLoader.Get(sin.Value);
        icon.Size = new Vector2(32, 32);

        // 우측 상단 배치
        icon.AnchorLeft = 1f;
        icon.AnchorTop = 0f;
        icon.OffsetLeft = -40;
        icon.OffsetTop = 8;
        icon.OffsetRight = -8;
        icon.OffsetBottom = 40;

        __instance.AddChild(icon);
    }
}
```

### 3단계: 아이콘 로딩
Sin enum → Texture2D 매핑. `assets/sprites/icons/` 에 이미 7개 아이콘 존재.

```csharp
public static class SinIconLoader
{
    private static readonly Dictionary<Sin, Texture2D> _cache = new();

    public static Texture2D Get(Sin sin)
    {
        if (_cache.TryGetValue(sin, out var tex)) return tex;
        // 경로: assets/sprites/icons/{Sin}.png
        var path = $"res://TheCity/assets/sprites/icons/{sin}.png";
        tex = ResourceLoader.Load<Texture2D>(path);
        _cache[sin] = tex;
        return tex;
    }
}
```

## 조사 필요 항목

### 필수
- [ ] 카드 디스플레이 노드 클래스명 (NCardFront? NCardView? NHandCard?)
- [ ] CardModel 참조 프로퍼티명 (card? CardModel? Model?)
- [ ] 카드 씬(.tscn) 노드 트리 구조 (사이즈, 앵커 파악)
- [ ] 카드 상태별 표시 여부 (손패, 보상 화면, 덱 보기, 상점)

### 선택
- [ ] 카드 타입별 테두리 색상과의 조화
- [ ] 아이콘 크기 최적값 (해상도별)
- [ ] 마우스 호버 시 Sin 이름 툴팁

## 아이콘 에셋 (이미 존재)
```
assets/sprites/icons/
├── Wrath.png     (분노)
├── Lust.png      (색욕)
├── Sloth.png     (나태)
├── Gluttony.png  (탐식)
├── Gloom.png     (우울)
├── Pride.png     (오만)
└── Envy.png      (질투)
```

## 파일 구조 (구현 시)
```
src/UI/
├── CardSinIconPatch.cs    # Harmony 패치 — 카드에 아이콘 주입
└── SinIconLoader.cs       # Sin → Texture2D 캐시 로더
```
