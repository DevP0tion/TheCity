// Design Ref: 전투 중 화면 좌측 하단 덱 아이콘 위에 7대죄 자원 HUD.
using System;
using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using TheCity.Resource;

namespace TheCity.UI;

/// <summary>
/// 전투 중 내내 덱(DrawPile) 아이콘 위에 상시 표시되는 7대죄 자원 HUD.
///
/// 구현 메모: Godot source generator(partial + InvokeGodotClassMethod)가
/// MonoMod/Harmony JIT 훅과 충돌하므로 <b>커스텀 Node 서브클래스를 전혀 사용하지 않음</b>.
/// SinDisplay도 제거하고 plain Panel/Label/HBoxContainer 조합으로 직접 조립.
/// </summary>
public static class SinStackPanel
{
    public static bool IsActive => _container != null && GodotObject.IsInstanceValid(_container);

    private const float VerticalGap = 12f;
    private const float IconSize = 14f;
    private const int NameFontSize = 14;
    private const int ValueFontSize = 16;
    private const double TickInterval = 0.016;  // ~60 FPS

    private static readonly Color PanelBg = new(0.08f, 0.08f, 0.12f, 0.82f);
    private static readonly Color PanelBorder = new(0.4f, 0.6f, 0.8f, 0.55f);
    private static readonly Color DimValueColor = new(0.55f, 0.55f, 0.6f);
    private static readonly Color DimNameColor = new(0.65f, 0.65f, 0.7f);

    private static readonly Dictionary<Sin, Color> Palette = new()
    {
        { Sin.Wrath,    new Color(0.90f, 0.28f, 0.28f) },  // 빨강
        { Sin.Lust,     new Color(0.95f, 0.55f, 0.18f) },  // 주황
        { Sin.Sloth,    new Color(0.98f, 0.83f, 0.15f) },  // 노랑
        { Sin.Gluttony, new Color(0.22f, 0.78f, 0.45f) },  // 녹색
        { Sin.Gloom,    new Color(0.15f, 0.82f, 0.70f) },  // 청록
        { Sin.Pride,    new Color(0.38f, 0.58f, 0.88f) },  // 남색
        { Sin.Envy,     new Color(0.70f, 0.42f, 0.82f) },  // 보라
    };

    private sealed class Row
    {
        public required Panel Icon;
        public required Label NameLabel;
        public required Label ValueLabel;
        public required Color Color;
    }

    private static PanelContainer? _container;
    private static readonly Dictionary<Sin, Row> _rows = new();
    private static readonly Dictionary<string, Row> _rowsById = new();
    private static readonly Queue<(string id, int value)> _pendingUpdates = new();
    private static readonly object _pendingLock = new();

    // ── 라이프사이클 ──

    public static void AttachTo(Control parent)
    {
        if (IsActive) return;

        var panel = new PanelContainer
        {
            Name = "SinStackPanel",
            TopLevel = true,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = PanelBg,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            ContentMarginLeft = 10,
            ContentMarginRight = 10,
            ContentMarginTop = 8,
            ContentMarginBottom = 8,
            BorderWidthBottom = 1,
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            BorderWidthTop = 1,
            BorderColor = PanelBorder,
        });

        var box = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        box.AddThemeConstantOverride("separation", 4);
        panel.AddChild(box);

        _rows.Clear();
        _rowsById.Clear();
        foreach (Sin sin in Enum.GetValues<Sin>())
        {
            var row = BuildRow(sin);
            box.AddChild(BuildRowContainer(row));
            _rows[sin] = row;
            _rowsById[sin.ToResourceId()] = row;
        }

        var timer = new Godot.Timer
        {
            ProcessCallback = Godot.Timer.TimerProcessCallback.Idle,
            WaitTime = TickInterval,
            Autostart = true,
            OneShot = false,
        };
        timer.Timeout += OnTick;
        panel.AddChild(timer);

        panel.TreeExiting += OnTreeExiting;

        parent.AddChild(panel);
        _container = panel;

        RefreshAllRows();

        SharedResourceManager.ValueChanged += OnValueChanged;
        SharedResourceManager.Initialized += OnResourcesInitialized;

        GD.Print($"[{ModStart.ModId}] SinStackPanel attached: {_rows.Count} rows (above DrawPile).");
    }

    // ── UI 조립 헬퍼 ──

    private static Row BuildRow(Sin sin)
    {
        var color = Palette[sin];

        var icon = new Panel
        {
            CustomMinimumSize = new Vector2(IconSize, IconSize),
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        icon.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = color,
            CornerRadiusBottomLeft = 7,
            CornerRadiusBottomRight = 7,
            CornerRadiusTopLeft = 7,
            CornerRadiusTopRight = 7,
            BorderWidthBottom = 1,
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            BorderWidthTop = 1,
            BorderColor = new Color(0f, 0f, 0f, 0.45f),
        });

        var nameLabel = new Label
        {
            Text = sin.ToDisplayName(),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        nameLabel.AddThemeFontSizeOverride("font_size", NameFontSize);
        nameLabel.AddThemeColorOverride("font_color", DimNameColor);

        var valueLabel = new Label
        {
            Text = "0",
            CustomMinimumSize = new Vector2(30, 0),
            HorizontalAlignment = HorizontalAlignment.Right,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        valueLabel.AddThemeFontSizeOverride("font_size", ValueFontSize);
        valueLabel.AddThemeColorOverride("font_color", DimValueColor);

        return new Row { Icon = icon, NameLabel = nameLabel, ValueLabel = valueLabel, Color = color };
    }

    private static HBoxContainer BuildRowContainer(Row row)
    {
        var hbox = new HBoxContainer
        {
            Name = row.NameLabel.Text,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        hbox.AddThemeConstantOverride("separation", 8);
        hbox.AddChild(row.Icon);
        hbox.AddChild(row.NameLabel);
        hbox.AddChild(row.ValueLabel);
        return hbox;
    }

    // ── 이벤트 핸들러 ──

    private static void OnValueChanged(string id, int oldValue, int newValue)
    {
        if (!_rowsById.ContainsKey(id)) return;
        lock (_pendingLock)
        {
            _pendingUpdates.Enqueue((id, newValue));
        }
    }

    private static void OnResourcesInitialized()
    {
        lock (_pendingLock)
        {
            foreach (var kv in _rowsById)
                _pendingUpdates.Enqueue((kv.Key, 0));
        }
    }

    private static void OnTreeExiting()
    {
        SharedResourceManager.ValueChanged -= OnValueChanged;
        SharedResourceManager.Initialized -= OnResourcesInitialized;
        _rows.Clear();
        _rowsById.Clear();
        lock (_pendingLock) _pendingUpdates.Clear();
        _container = null;
    }

    // ── 매 프레임 (Timer.Timeout) ──

    private static void OnTick()
    {
        if (!IsActive) return;

        lock (_pendingLock)
        {
            while (_pendingUpdates.Count > 0)
            {
                var (id, value) = _pendingUpdates.Dequeue();
                if (_rowsById.TryGetValue(id, out var row))
                    ApplyRowValue(row, value);
            }
        }

        var drawPile = NCombatRoom.Instance?.Ui?.DrawPile as Control;
        if (drawPile == null || !GodotObject.IsInstanceValid(drawPile))
        {
            _container!.Visible = false;
            return;
        }

        _container!.Visible = true;
        var rect = drawPile.GetGlobalRect();
        var mySize = _container.Size;
        _container.GlobalPosition = new Vector2(
            rect.Position.X + rect.Size.X * 0.5f - mySize.X * 0.5f,
            rect.Position.Y - mySize.Y - VerticalGap
        );
    }

    // ── 헬퍼 ──

    private static void RefreshAllRows()
    {
        foreach (var (sin, row) in _rows)
            ApplyRowValue(row, sin.Get());
    }

    private static void ApplyRowValue(Row row, int value)
    {
        row.ValueLabel.Text = value.ToString();
        if (value <= 0)
        {
            row.ValueLabel.AddThemeColorOverride("font_color", DimValueColor);
            row.NameLabel.AddThemeColorOverride("font_color", DimNameColor);
            row.Icon.Modulate = new Color(1f, 1f, 1f, 0.45f);
        }
        else
        {
            row.ValueLabel.AddThemeColorOverride("font_color", row.Color);
            row.NameLabel.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f, 0.95f));
            row.Icon.Modulate = Colors.White;
        }
    }
}
