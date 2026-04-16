// Design Ref: Plan §SinDisplay — 개별 Sin 한 줄. 아이콘 원 + 값, 이름은 툴팁.
using Godot;
using TheCity.Resource;

namespace TheCity.UI;

/// <summary>
/// 수직 스택의 한 줄. Sin 색상 원 + 값 라벨.
/// 이름은 TooltipText로만 노출(스택 폭을 좁게 유지).
/// 값 변경 시 흰색→Sin색 펄스 애니메이션.
/// </summary>
public partial class SinDisplay : HBoxContainer
{
    private const float IconSize = 14f;
    private static readonly Color FlashColor = new(1f, 1f, 1f);

    private static readonly System.Collections.Generic.Dictionary<Sin, Color> Palette = new()
    {
        { Sin.Wrath,    new Color(0.82f, 0.18f, 0.18f) },  // 빨강
        { Sin.Lust,     new Color(0.90f, 0.49f, 0.13f) },  // 주황
        { Sin.Sloth,    new Color(0.95f, 0.77f, 0.06f) },  // 노랑
        { Sin.Gluttony, new Color(0.61f, 0.35f, 0.71f) },  // 보라
        { Sin.Gloom,    new Color(0.10f, 0.74f, 0.61f) },  // 청록
        { Sin.Pride,    new Color(0.29f, 0.47f, 0.75f) },  // 남색
        { Sin.Envy,     new Color(0.15f, 0.68f, 0.38f) },  // 녹색
    };

    public Sin Sin { get; }
    private readonly Color _baseColor;

    private Panel _icon = null!;
    private Label _valueLabel = null!;
    private float _animProgress = 1f;

    public SinDisplay(Sin sin)
    {
        Sin = sin;
        _baseColor = Palette[sin];
        Name = $"Sin_{sin}";
    }

    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 6);
        TooltipText = Sin.ToDisplayName();

        // 색상 아이콘 (원형에 가까운 둥근 사각형)
        _icon = new Panel();
        var iconStyle = new StyleBoxFlat
        {
            BgColor = _baseColor,
            CornerRadiusBottomLeft = 7,
            CornerRadiusBottomRight = 7,
            CornerRadiusTopLeft = 7,
            CornerRadiusTopRight = 7,
            BorderWidthBottom = 1,
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            BorderWidthTop = 1,
            BorderColor = new Color(0f, 0f, 0f, 0.45f),
        };
        _icon.AddThemeStyleboxOverride("panel", iconStyle);
        _icon.CustomMinimumSize = new Vector2(IconSize, IconSize);
        _icon.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        AddChild(_icon);

        _valueLabel = new Label();
        _valueLabel.AddThemeFontSizeOverride("font_size", 16);
        _valueLabel.AddThemeColorOverride("font_color", _baseColor);
        _valueLabel.CustomMinimumSize = new Vector2(26, 0);
        _valueLabel.HorizontalAlignment = HorizontalAlignment.Right;
        AddChild(_valueLabel);
    }

    /// <summary>값 갱신 + 플래시 트리거.</summary>
    public void SetValue(int value)
    {
        if (_valueLabel != null)
            _valueLabel.Text = value.ToString();
        _animProgress = 0f;
    }

    public override void _Process(double delta)
    {
        if (_animProgress >= 1f) return;

        _animProgress = Mathf.Min(_animProgress + (float)delta * 4f, 1f);
        _valueLabel.AddThemeColorOverride("font_color", FlashColor.Lerp(_baseColor, _animProgress));
    }
}
