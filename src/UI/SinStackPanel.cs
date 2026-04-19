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
/// MonoMod/Harmony JIT 훅과 충돌하므로 커스텀 Node 서브클래스 없이 <b>정적 헬퍼</b> +
/// plain <see cref="PanelContainer"/> + 자식 <see cref="Godot.Timer"/> 시그널로 구현.
///
/// 앵커 대상: <c>NCombatRoom.Instance.Ui.DrawPile</c> (NDrawPileButton).
/// 위치: DrawPile의 GlobalPosition 기준 상단 중앙 정렬, <see cref="VerticalGap"/> 띄움.
/// </summary>
public static class SinStackPanel
{
    public static bool IsActive => _container != null && GodotObject.IsInstanceValid(_container);

    private const float VerticalGap = 12f;
    private const double TickInterval = 0.016;  // ~60 FPS

    private static PanelContainer? _container;
    private static readonly Dictionary<Sin, SinDisplay> _displays = new();
    private static readonly Dictionary<string, SinDisplay> _displaysById = new();
    private static readonly Queue<(string id, int value)> _pendingUpdates = new();
    private static readonly object _pendingLock = new();

    // ── 라이프사이클 ──

    /// <summary>
    /// NCombatRoom.Ui에 패널 주입. 전투방마다 1회 호출. 이미 붙어있으면 무시.
    /// </summary>
    public static void AttachTo(Control parent)
    {
        if (IsActive) return;

        var panel = new PanelContainer
        {
            Name = "SinStackPanel",
            TopLevel = true,                                    // 부모 레이아웃 무시, GlobalPosition 직접 제어
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };

        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.08f, 0.08f, 0.12f, 0.82f),
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            ContentMarginLeft = 8,
            ContentMarginRight = 8,
            ContentMarginTop = 6,
            ContentMarginBottom = 6,
            BorderWidthBottom = 1,
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            BorderWidthTop = 1,
            BorderColor = new Color(0.4f, 0.6f, 0.8f, 0.55f),
        };
        panel.AddThemeStyleboxOverride("panel", style);

        var box = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        box.AddThemeConstantOverride("separation", 3);
        panel.AddChild(box);

        _displays.Clear();
        _displaysById.Clear();
        foreach (Sin sin in Enum.GetValues<Sin>())
        {
            var display = new SinDisplay(sin) { MouseFilter = Control.MouseFilterEnum.Ignore };
            box.AddChild(display);
            _displays[sin] = display;
            _displaysById[sin.ToResourceId()] = display;
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

        // 초기 값 채우기 (SharedResourceManager가 이미 초기화된 상태)
        RefreshAllDisplays();

        SharedResourceManager.ValueChanged += OnValueChanged;
        SharedResourceManager.Initialized += OnResourcesInitialized;

        GD.Print($"[{ModStart.ModId}] SinStackPanel attached (above DrawPile).");
    }

    // ── 이벤트 핸들러 ──

    private static void OnValueChanged(string id, int oldValue, int newValue)
    {
        if (!_displaysById.ContainsKey(id)) return;
        lock (_pendingLock)
        {
            _pendingUpdates.Enqueue((id, newValue));
        }
    }

    private static void OnResourcesInitialized()
    {
        // 전투 시작 시 모든 값이 0으로 재설정되면 UI도 동기화
        lock (_pendingLock)
        {
            foreach (var kv in _displaysById)
                _pendingUpdates.Enqueue((kv.Key, 0));
        }
    }

    private static void OnTreeExiting()
    {
        SharedResourceManager.ValueChanged -= OnValueChanged;
        SharedResourceManager.Initialized -= OnResourcesInitialized;
        _displays.Clear();
        _displaysById.Clear();
        lock (_pendingLock) _pendingUpdates.Clear();
        _container = null;
    }

    // ── 매 프레임 업데이트 (Timer.Timeout) ──

    private static void OnTick()
    {
        if (!IsActive) return;

        // 큐 플러시 (네트워크 스레드 → 메인 스레드 마샬링)
        lock (_pendingLock)
        {
            while (_pendingUpdates.Count > 0)
            {
                var (id, value) = _pendingUpdates.Dequeue();
                if (_displaysById.TryGetValue(id, out var display))
                    ApplyValue(display, value);
            }
        }

        // DrawPile 위치 추적
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
            rect.Position.X + rect.Size.X * 0.5f - mySize.X * 0.5f,  // 덱 좌우 중앙
            rect.Position.Y - mySize.Y - VerticalGap                 // 덱 상단 위
        );
    }

    // ── 헬퍼 ──

    private static void RefreshAllDisplays()
    {
        foreach (var (sin, display) in _displays)
            ApplyValue(display, sin.Get());
    }

    private static void ApplyValue(SinDisplay display, int value)
    {
        display.Visible = true;
        display.SetValue(value);
    }
}
