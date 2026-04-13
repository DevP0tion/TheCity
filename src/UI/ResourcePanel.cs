using System.Collections.Generic;
using Godot;
using TheCity.Resource;

namespace TheCity.UI;

/// <summary>
/// SharedResourceManager의 이벤트를 구독하여
/// 자원 UI를 자동으로 생성/업데이트/정리하는 패널.
/// NCombatRoom에 주입되어 전투 중에만 표시.
/// </summary>
public partial class ResourcePanel : PanelContainer
{
    public static ResourcePanel? Instance { get; private set; }

    private VBoxContainer _container = null!;
    private readonly Dictionary<string, ResourceDisplay> _displays = new();

    /// <summary>
    /// 자원 ID → 표시 이름 매핑.
    /// Register 전에 설정하면 해당 이름으로 표시됨.
    /// 미설정 시 ID를 그대로 표시.
    /// </summary>
    public static Dictionary<string, string> DisplayNames { get; } = new();

    public override void _Ready()
    {
        Instance = this;
        Name = "ResourcePanel";
        Visible = false;

        // 스타일
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.1f, 0.1f, 0.15f, 0.85f),
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            ContentMarginLeft = 10,
            ContentMarginRight = 10,
            ContentMarginTop = 6,
            ContentMarginBottom = 6,
            BorderWidthBottom = 1,
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            BorderWidthTop = 1,
            BorderColor = new Color(0.4f, 0.6f, 0.8f, 0.6f),
        };
        AddThemeStyleboxOverride("panel", style);

        _container = new VBoxContainer();
        _container.AddThemeConstantOverride("separation", 4);
        AddChild(_container);

        Subscribe();
        BuildExistingResources();
    }

    public override void _ExitTree()
    {
        Unsubscribe();
        _displays.Clear();
        if (Instance == this) Instance = null;
    }

    private void Subscribe()
    {
        SharedResourceManager.ValueChanged += OnValueChanged;
        SharedResourceManager.ResourceRegistered += OnResourceRegistered;
        SharedResourceManager.Initialized += OnInitialized;
        SharedResourceManager.CleanedUp += OnCleanedUp;
    }

    private void Unsubscribe()
    {
        SharedResourceManager.ValueChanged -= OnValueChanged;
        SharedResourceManager.ResourceRegistered -= OnResourceRegistered;
        SharedResourceManager.Initialized -= OnInitialized;
        SharedResourceManager.CleanedUp -= OnCleanedUp;
    }

    private void BuildExistingResources()
    {
        foreach (var id in SharedResourceManager.AllIds)
        {
            AddDisplay(id, SharedResourceManager.Get(id));
        }
    }

    private void AddDisplay(string id, int value)
    {
        if (_displays.ContainsKey(id)) return;

        var displayName = DisplayNames.TryGetValue(id, out var name) ? name : id;
        var display = ResourceDisplay.Create(id, displayName, value);
        _container.AddChild(display);
        _displays[id] = display;
    }

    // ── 이벤트 핸들러 ──

    private void OnValueChanged(string id, int oldValue, int newValue)
    {
        if (_displays.TryGetValue(id, out var display))
        {
            display.UpdateValue(newValue);
        }
    }

    private void OnResourceRegistered(string id, int initialValue)
    {
        AddDisplay(id, initialValue);
    }

    private void OnInitialized()
    {
        Visible = _displays.Count > 0;
        foreach (var (id, display) in _displays)
        {
            display.UpdateValue(0);
        }
    }

    private void OnCleanedUp()
    {
        Visible = false;
    }
}
