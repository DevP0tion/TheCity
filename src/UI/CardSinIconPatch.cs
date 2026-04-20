using System.Collections.Generic;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Nodes.Cards;
using TheCity.Resource;

namespace TheCity.UI;

/// <summary>
/// NCard.Reload() Postfix — 카드 우측 상단에 Sin 속성 아이콘 표시.
///
/// 아이콘 경로: assets/sprites/icons/{Sin}.png (PCK에 포함)
/// 위치: 카드 우측 상단 (에너지 아이콘 반대편)
/// 크기: 36×36px
/// </summary>
[HarmonyPatch(typeof(NCard), "Reload")]
public static class CardSinIconPatch
{
    private const string IconNodeName = "SinIcon";
    private const float IconSize = 36f;
    private const float MarginRight = 12f;
    private const float MarginTop = 12f;

    // 텍스처 캐시 (Sin → Texture2D)
    private static readonly Dictionary<Sin, Texture2D?> _textureCache = new();
    public static void Postfix(NCard __instance)
    {
        if (__instance.Model == null) return;
        if (!__instance.IsNodeReady()) return;

        var sin = __instance.Model.GetSin();

        // Body(%CardContainer) 하위에 아이콘 노드 관리
        var body = __instance.Body;
        if (body == null) return;

        var existing = body.GetNodeOrNull<TextureRect>(IconNodeName);

        // Sin 미등록 또는 카드 면이 가려진 상태(Hidden/Locked)에서는 아이콘 숨김
        if (sin == null || __instance.Visibility != ModelVisibility.Visible)
        {
            if (existing != null) existing.Visible = false;
            return;
        }

        var texture = GetSinTexture(sin.Value);
        if (texture == null)
        {
            if (existing != null) existing.Visible = false;
            return;
        }

        TextureRect icon;
        if (existing != null)
        {
            icon = existing;
        }
        else
        {
            icon = new TextureRect
            {
                Name = IconNodeName,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                MouseFilter = Control.MouseFilterEnum.Ignore,
                ZIndex = 10,
            };
            body.AddChild(icon);
        }

        icon.Texture = texture;
        icon.Size = new Vector2(IconSize, IconSize);
        // 카드 우측 상단에 배치 (카드 크기 300×422 기준)
        icon.Position = new Vector2(
            NCard.defaultSize.X - IconSize - MarginRight,
            MarginTop
        );
        icon.Visible = true;
    }

    private static Texture2D? GetSinTexture(Sin sin)
    {
        if (_textureCache.TryGetValue(sin, out var cached))
            return cached;

        // 모드 에셋에서 로드 (PCK 내 res:// 경로)
        var path = $"res://assets/sprites/icons/{sin}.png";
        Texture2D? tex = null;

        if (ResourceLoader.Exists(path))
        {
            tex = ResourceLoader.Load<Texture2D>(path, null, ResourceLoader.CacheMode.Reuse);
        }
        else
        {
            GD.PrintErr($"[{ModStart.ModId}] Sin icon not found: {path}");
        }

        _textureCache[sin] = tex;
        return tex;
    }
}
