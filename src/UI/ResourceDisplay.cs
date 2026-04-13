using Godot;

namespace TheCity.UI;

/// <summary>
/// 개별 자원의 이름과 값을 표시하는 UI 요소.
/// ResourcePanel에 의해 생성/관리됨.
/// </summary>
public partial class ResourceDisplay : HBoxContainer
{
    private Label _nameLabel = null!;
    private Label _valueLabel = null!;
    private float _animProgress = 1f;

    private static readonly Color DefaultColor = new(0.4f, 0.8f, 1.0f);
    private static readonly Color FlashColor = new(1f, 1f, 1f);

    public string ResourceId { get; private set; } = string.Empty;

    public static ResourceDisplay Create(string id, string displayName, int initialValue)
    {
        var display = new ResourceDisplay();
        display.ResourceId = id;
        display.Name = $"Resource_{id}";
        display.Setup(displayName, initialValue);
        return display;
    }

    private void Setup(string displayName, int initialValue)
    {
        AddThemeConstantOverride("separation", 8);

        _nameLabel = new Label();
        _nameLabel.Text = displayName;
        _nameLabel.AddThemeFontSizeOverride("font_size", 13);
        _nameLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.8f, 0.9f));
        AddChild(_nameLabel);

        _valueLabel = new Label();
        _valueLabel.Text = initialValue.ToString();
        _valueLabel.AddThemeFontSizeOverride("font_size", 18);
        _valueLabel.AddThemeColorOverride("font_color", DefaultColor);
        _valueLabel.CustomMinimumSize = new Vector2(40, 0);
        AddChild(_valueLabel);
    }

    public void UpdateValue(int newValue)
    {
        _valueLabel.Text = newValue.ToString();
        _animProgress = 0f;
    }

    public override void _Process(double delta)
    {
        if (_animProgress >= 1f) return;

        _animProgress = Mathf.Min(_animProgress + (float)delta * 4f, 1f);
        _valueLabel.AddThemeColorOverride("font_color", FlashColor.Lerp(DefaultColor, _animProgress));
    }
}
