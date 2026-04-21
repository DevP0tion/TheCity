using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Nodes.Cards;
using TheCity.Resource;

namespace TheCity.UI;

/// <summary>
/// NCard.Reload() Postfix — 카드 우측 상단에 Sin 속성 아이콘 표시.
///
/// 텍스처 로드: PCK 없이 파일 시스템에서 직접 로드 (Image.LoadFromFile).
/// 아이콘 경로: {모드 디렉토리}/assets/sprites/icons/{Sin}.png
/// </summary>
[HarmonyPatch(typeof(NCard), "Reload")]
public static class CardSinIconPatch
{
    private const string IconNodeName = "SinIcon";
    private const float IconSize = 36f;
    private const float MarginRight = 12f;
    private const float MarginTop = 12f;

    private static readonly Dictionary<Sin, Texture2D?> _textureCache = new();
    private static string? _modDir;

    public static void Postfix(NCard __instance)
    {
        if (__instance.Model == null) return;
        if (!__instance.IsNodeReady()) return;

        var sin = __instance.Model.GetSin();
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

        var path = GetIconPath(sin);
        Texture2D? tex = null;

        if (path != null && File.Exists(path))
        {
            var image = Image.LoadFromFile(path);
            if (image != null)
            {
                image.Resize((int)IconSize, (int)IconSize, Image.Interpolation.Lanczos);
                tex = ImageTexture.CreateFromImage(image);
            }
        }

        if (tex == null)
            GD.PrintErr($"[{ModStart.ModId}] Sin icon not found: {path ?? sin.ToString()}");

        _textureCache[sin] = tex;
        return tex;
    }

    private static string? GetIconPath(Sin sin)
    {
        _modDir ??= ResolveModDir();
        if (_modDir == null) return null;
        return Path.Combine(_modDir, "assets", "sprites", "icons", $"{sin}.png");
    }

    /// <summary>모드 설치 디렉토리 탐색.</summary>
    private static string? ResolveModDir()
    {
        // 1. DLL 위치 기반
        try
        {
            var dllPath = Assembly.GetExecutingAssembly().Location;
            if (!string.IsNullOrEmpty(dllPath))
            {
                var dir = Path.GetDirectoryName(dllPath);
                if (dir != null && Directory.Exists(Path.Combine(dir, "assets")))
                {
                    GD.Print($"[{ModStart.ModId}] Mod dir resolved from DLL: {dir}");
                    return dir;
                }
            }
        }
        catch { /* ignore */ }

        // TODO: ModManager 폴백은 정확한 API 확인 후 복원 (sts-game-analyst에 의뢰)

        GD.PrintErr($"[{ModStart.ModId}] Could not resolve mod directory!");
        return null;
    }
}
