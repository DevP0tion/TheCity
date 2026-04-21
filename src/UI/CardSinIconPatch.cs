using System.Collections.Generic;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Nodes.Cards;
using TheCity.Resource;

namespace TheCity.UI;

/// <summary>
/// NCard.Reload() Postfix — 카드 우측 상단에 Sin 속성 아이콘 표시.
/// 텍스처: PCK에서 res:// 경로로 로드.
/// </summary>
[HarmonyPatch(typeof(NCard), "Reload")]
public static class CardSinIconPatch
{
    private const string IconNodeName = "SinIcon";
    private const float IconSize = 54f;
    private const float RelativeMargin = -0.05f;

    private static readonly Dictionary<Sin, Texture2D?> _textureCache = new();

    public static void Postfix(NCard __instance)
    {
        if (__instance.Model == null) return;
        if (!__instance.IsNodeReady()) return;

        var sin = __instance.Model.GetSin();
        var body = __instance.Body;
        if (body == null) return;

        var existing = body.GetNodeOrNull<TextureRect>(IconNodeName);

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
            };
            body.AddChild(icon);
        }

        icon.Texture = texture;
        icon.Size = new Vector2(IconSize, IconSize);
        var cardSize = NCard.defaultSize;
        icon.Position = new Vector2(
            cardSize.X / 2f - IconSize - cardSize.X * RelativeMargin,
            -cardSize.Y / 2f + cardSize.Y * RelativeMargin
        );
        icon.Visible = true;
    }

    private static Texture2D? GetSinTexture(Sin sin)
    {
        if (_textureCache.TryGetValue(sin, out var cached))
            return cached;

        // PCK에서 res:// 경로로 로드 (Godot import 시스템이 .ctex로 리다이렉트)
        var path = $"res://assets/sprites/icons/{sin}.png";
        Texture2D? tex = null;

        if (ResourceLoader.Exists(path))
        {
            tex = ResourceLoader.Load<Texture2D>(path, null, ResourceLoader.CacheMode.Reuse);
        }

        if (tex == null)
            GD.PrintErr($"[{ModStart.ModId}] Sin icon not found: {path}");

        _textureCache[sin] = tex;
        return tex;
    }
}
