// Design Ref: Plan §SinStackPanel — 드래그 중 카드 위의 Sin 7개 수직 스택.
using System;
using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using TheCity.Resource;

namespace TheCity.UI;

/// <summary>
/// 드래그/선택 중인 카드 바로 위에 떠 있는 7대죄 수직 스택.
///
/// 구현 메모: Godot source generator(partial + InvokeGodotClassMethod)가
/// MonoMod/Harmony JIT 훅과 충돌해 ArgumentException을 유발함.
/// 이를 회피하기 위해 커스텀 Node 서브클래스 없이 <b>정적 헬퍼</b> + plain
/// <see cref="PanelContainer"/>/<see cref="Godot.Timer"/> 조립으로 구현.
/// - Godot 가상 메서드 override 없음 → InvokeGodotClassMethod 생성 없음
/// - <c>Timer.Timeout</c> 시그널로 매 프레임 업데이트 (16ms 주기)
/// - <c>TreeExiting</c> 시그널로 cleanup
/// </summary>
public static class SinStackPanel
{
    public static bool IsActive => _container != null && GodotObject.IsInstanceValid(_container);

    private const float VerticalGap = 12f;      // 카드 상단과 스택 하단 사이 여백
    private const double TickInterval = 0.016;  // ~60 FPS

    private static PanelContainer? _container;
    private static readonly Dictionary<Sin, SinDisplay> _displays = new();
    private static readonly Dictionary<string, SinDisplay> _displaysById = new();
    private static readonly Queue<(string id, int value)> _pendingUpdates = new();
    private static readonly object _pendingLock = new();
    private static CardModel? _target;
    private static bool _pendingUnbind;

    // ── 라이프사이클 ──

    /// <summary>
    /// NCombatRoom.Ui에 패널 주입. 전투방마다 1회 호출.
    /// 이미 붙어있으면 무시(idempotent).
    /// </summary>
    public static void AttachTo(Control parent)
    {
        if (IsActive) return;

        var panel = new PanelContainer
        {
            Name = "SinStackPanel",
            Visible = false,
            TopLevel = true,                                    // 부모 레이아웃 무시, GlobalPosition 직접 제어
            MouseFilter = Control.MouseFilterEnum.Ignore,       // 카드 입력 가로채지 않음
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

        SharedResourceManager.ValueChanged += OnValueChanged;
        SharedResourceManager.CleanedUp += OnCleanedUp;

        GD.Print($"[{ModStart.ModId}] SinStackPanel attached.");
    }

    /// <summary>드래그 시작: 대상 카드 바인딩 + 표시.</summary>
    public static void Bind(CardModel model)
    {
        if (!IsActive) return;
        _target = model;
        RefreshValues();
        _container!.Visible = true;
    }

    /// <summary>드래그 종료: 숨김.</summary>
    public static void Unbind()
    {
        _target = null;
        if (_container != null && GodotObject.IsInstanceValid(_container))
            _container.Visible = false;
    }

    // ── 이벤트 핸들러 ──

    private static void OnValueChanged(string id, int oldValue, int newValue)
    {
        if (_target == null) return;
        if (!_displaysById.ContainsKey(id)) return;
        lock (_pendingLock)
        {
            _pendingUpdates.Enqueue((id, newValue));
        }
    }

    private static void OnCleanedUp()
    {
        _pendingUnbind = true;
    }

    private static void OnTreeExiting()
    {
        SharedResourceManager.ValueChanged -= OnValueChanged;
        SharedResourceManager.CleanedUp -= OnCleanedUp;
        _displays.Clear();
        _displaysById.Clear();
        lock (_pendingLock) _pendingUpdates.Clear();
        _target = null;
        _container = null;
    }

    // ── 매 프레임 업데이트 (Timer.Timeout) ──

    private static void OnTick()
    {
        if (!IsActive) return;

        if (_pendingUnbind)
        {
            _pendingUnbind = false;
            Unbind();
            return;
        }

        lock (_pendingLock)
        {
            while (_pendingUpdates.Count > 0)
            {
                var (id, value) = _pendingUpdates.Dequeue();
                if (_displaysById.TryGetValue(id, out var display))
                    ApplyValue(display, value);
            }
        }

        if (_target == null) return;

        var cardNode = NCombatRoom.Instance?.Ui?.Hand?.GetCardHolder(_target)?.CardNode;
        if (cardNode == null || !GodotObject.IsInstanceValid(cardNode))
        {
            Unbind();
            return;
        }

        var rect = cardNode.GetGlobalRect();
        var mySize = _container!.Size;
        _container.GlobalPosition = new Vector2(
            rect.Position.X + rect.Size.X * 0.5f - mySize.X * 0.5f,
            rect.Position.Y - mySize.Y - VerticalGap
        );
    }

    // ── 헬퍼 ──

    private static void RefreshValues()
    {
        foreach (var (sin, display) in _displays)
        {
            ApplyValue(display, sin.Get());
        }
    }

    private static void ApplyValue(SinDisplay display, int value)
    {
        display.Visible = true;
        display.SetValue(value);
    }
}
