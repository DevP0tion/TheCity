// Design Ref: Plan §SinStackPanel — 드래그 중 카드 위의 Sin 7개 수직 스택.
// Plan SC: R1(드래그 카드 바인딩), R2(값 0 숨김), R4(수직 정렬), R5(ValueChanged 구독).
using System;
using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using TheCity.Resource;

namespace TheCity.UI;

/// <summary>
/// 드래그/선택 중인 카드 바로 위에 떠 있는 7대죄 수직 스택.
/// HoveredModelTracker.OnLocalCardSelected에서 Bind, OnLocalCardDeselected에서 Unbind.
/// _Process에서 카드 GlobalPosition을 추적하여 자신의 위치를 갱신.
/// 값이 0인 Sin도 회색 "0"으로 표시 (Sin 획득 메커니즘 도입 전 가시성 확보).
///
/// 구현 메모: Godot의 CallDeferred(params Variant[]) 경로는 MonoMod/Harmony의 JIT 훅과
/// 충돌해 "Value does not fall within the expected range" 예외를 유발함.
/// 대신 lock + Queue 기반으로 네트워크 스레드 → 메인 스레드 마샬링을 수행.
/// </summary>
public partial class SinStackPanel : PanelContainer
{
    public static SinStackPanel? Instance { get; private set; }

    private const float VerticalGap = 12f;      // 카드 상단과 스택 하단 사이 여백

    private VBoxContainer _container = null!;
    private readonly Dictionary<Sin, SinDisplay> _displays = new();
    private readonly Dictionary<string, SinDisplay> _displaysById = new();
    private readonly Queue<(string id, int value)> _pendingUpdates = new();
    private readonly object _pendingLock = new();
    private CardModel? _target;
    private bool _pendingUnbind;

    public override void _Ready()
    {
        Instance = this;
        Name = "SinStackPanel";
        Visible = false;
        TopLevel = true;                         // 부모 레이아웃 무시하고 GlobalPosition 직접 제어
        MouseFilter = MouseFilterEnum.Ignore;    // 카드 입력 가로채지 않음

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
        AddThemeStyleboxOverride("panel", style);

        _container = new VBoxContainer();
        _container.AddThemeConstantOverride("separation", 3);
        _container.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(_container);

        // 7대죄 고정 순서로 선생성 (enum 선언 순). generic 형식은 dynamic-code 요구 없음.
        foreach (Sin sin in Enum.GetValues<Sin>())
        {
            var display = new SinDisplay(sin);
            display.MouseFilter = MouseFilterEnum.Ignore;
            _container.AddChild(display);
            _displays[sin] = display;
            _displaysById[sin.ToResourceId()] = display;
        }

        SharedResourceManager.ValueChanged += OnValueChanged;
        SharedResourceManager.CleanedUp += OnCleanedUp;
    }

    public override void _ExitTree()
    {
        SharedResourceManager.ValueChanged -= OnValueChanged;
        SharedResourceManager.CleanedUp -= OnCleanedUp;
        _displays.Clear();
        _displaysById.Clear();
        lock (_pendingLock) _pendingUpdates.Clear();
        _target = null;
        if (Instance == this) Instance = null;
    }

    /// <summary>드래그 시작: 대상 카드 바인딩 + 표시.</summary>
    public void Bind(CardModel model)
    {
        _target = model;
        RefreshValues();
        Visible = true;
    }

    /// <summary>드래그 종료: 숨김.</summary>
    public void Unbind()
    {
        _target = null;
        Visible = false;
    }

    private void RefreshValues()
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

    // ── 이벤트 핸들러 (네트워크 스레드에서 호출될 수 있음) ──
    // CallDeferred는 MonoMod/Harmony JIT 훅과 충돌 → lock+Queue로 대체.

    private void OnValueChanged(string id, int oldValue, int newValue)
    {
        if (_target == null) return;
        if (!_displaysById.ContainsKey(id)) return;
        lock (_pendingLock)
        {
            _pendingUpdates.Enqueue((id, newValue));
        }
    }

    private void OnCleanedUp()
    {
        _pendingUnbind = true;
    }

    public override void _Process(double delta)
    {
        // 1) 이벤트 큐 플러시 (메인 스레드).
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

        // 2) 카드 위치 추적.
        if (_target == null) return;

        var cardNode = NCombatRoom.Instance?.Ui?.Hand?.GetCardHolder(_target)?.CardNode;
        if (cardNode == null || !GodotObject.IsInstanceValid(cardNode))
        {
            // 카드가 pile을 떠났다(exhaust/discard/플레이 완료) → 자동 해제.
            Unbind();
            return;
        }

        var rect = cardNode.GetGlobalRect();
        var mySize = Size;
        GlobalPosition = new Vector2(
            rect.Position.X + rect.Size.X * 0.5f - mySize.X * 0.5f,
            rect.Position.Y - mySize.Y - VerticalGap
        );
    }
}
