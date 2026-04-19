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
/// </summary>
public partial class SinStackPanel : PanelContainer
{
    public static SinStackPanel? Instance { get; private set; }

    private const float VerticalGap = 12f;      // 카드 상단과 스택 하단 사이 여백

    private VBoxContainer _container = null!;
    private readonly Dictionary<Sin, SinDisplay> _displays = new();
    private readonly Dictionary<string, SinDisplay> _displaysById = new();
    private CardModel? _target;

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

        // 7대죄 고정 순서로 선생성 (enum 선언 순).
        foreach (Sin sin in Enum.GetValues(typeof(Sin)))
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

    private void OnValueChanged(string id, int oldValue, int newValue)
    {
        // 네트워크 스레드에서 호출될 가능성이 있으므로 메인 스레드로 마샬링.
        // 바인딩된 카드가 있고, 변경된 리소스가 Sin일 때만 해당 display 하나만 갱신.
        if (_target == null) return;
        if (!_displaysById.ContainsKey(id)) return;
        CallDeferred(nameof(ApplySingle), id, newValue);
    }

    private void ApplySingle(string id, int value)
    {
        if (_displaysById.TryGetValue(id, out var display))
            ApplyValue(display, value);
    }

    private void OnCleanedUp()
    {
        CallDeferred(MethodName.Unbind);
    }

    public override void _Process(double delta)
    {
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
