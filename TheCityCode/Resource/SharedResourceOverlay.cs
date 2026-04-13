using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace TheCity.TheCityCode.Resource;

/// <summary>
/// 전투 중 SharedResource 값을 표시하는 UI 오버레이.
/// 유물 바 하단에 위치.
/// </summary>
public partial class SharedResourceOverlay : Control
{
    public static SharedResourceOverlay? Instance { get; private set; }

    private Label _valueLabel = null!;
    private Label _nameLabel = null!;
    private Panel _background = null!;

    private int _displayedValue;
    private float _animProgress = 1f;

    public override void _Ready()
    {
        Instance = this;
        Name = "SharedResourceOverlay";

        // 유물 바 하단 위치 (좌상단 기준)
        AnchorLeft = 0f;
        AnchorTop = 0f;
        OffsetLeft = 20;
        OffsetTop = 90; // 유물 바 아래
        OffsetRight = 160;
        OffsetBottom = 140;

        // 배경 패널
        _background = new Panel();
        var stylebox = new StyleBoxFlat
        {
            BgColor = new Color(0.1f, 0.1f, 0.15f, 0.85f),
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            ContentMarginLeft = 8,
            ContentMarginRight = 8,
            ContentMarginTop = 4,
            ContentMarginBottom = 4,
            BorderWidthBottom = 1,
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            BorderWidthTop = 1,
            BorderColor = new Color(0.4f, 0.6f, 0.8f, 0.6f),
        };
        _background.AddThemeStyleboxOverride("panel", stylebox);
        _background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_background);

        // 자원 이름
        _nameLabel = new Label();
        _nameLabel.Text = "The City"; // TODO: 로컬라이제이션
        _nameLabel.AddThemeFontSizeOverride("font_size", 12);
        _nameLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.8f, 0.9f));
        _nameLabel.Position = new Vector2(10, 4);
        AddChild(_nameLabel);

        // 자원 값
        _valueLabel = new Label();
        _valueLabel.AddThemeFontSizeOverride("font_size", 22);
        _valueLabel.AddThemeColorOverride("font_color", new Color(0.4f, 0.8f, 1.0f));
        _valueLabel.Position = new Vector2(10, 20);
        AddChild(_valueLabel);

        _displayedValue = SharedResource.Value;
        UpdateDisplay();

        SharedResource.ValueChanged += OnValueChanged;
    }

    public override void _Process(double delta)
    {
        if (_animProgress < 1f)
        {
            _animProgress = Mathf.Min(_animProgress + (float)delta * 4f, 1f);

            // 값 변경 시 색상 펄스 효과
            var pulseColor = new Color(1f, 1f, 1f).Lerp(new Color(0.4f, 0.8f, 1.0f), _animProgress);
            _valueLabel.AddThemeColorOverride("font_color", pulseColor);

            var scale = 1f + (1f - _animProgress) * 0.15f;
            _valueLabel.Scale = new Vector2(scale, scale);
        }

        // 전투 비활성 시 숨김
        Visible = SharedResource.IsActive;
    }

    public override void _ExitTree()
    {
        SharedResource.ValueChanged -= OnValueChanged;
        if (Instance == this) Instance = null;
    }

    private void OnValueChanged(int oldValue, int newValue)
    {
        _displayedValue = newValue;
        _animProgress = 0f;
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        _valueLabel.Text = _displayedValue.ToString();
    }
}

/// <summary>
/// NCombatRoom에 SharedResourceOverlay를 주입하는 Harmony 패치.
/// </summary>
[HarmonyPatch(typeof(NCombatRoom), "_Ready")]
public static class SharedResourceOverlayPatch
{
    public static void Postfix(NCombatRoom __instance)
    {
        if (SharedResourceOverlay.Instance != null) return;
        __instance.AddChild(new SharedResourceOverlay());
    }
}
