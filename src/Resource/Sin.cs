namespace TheCity.Resource;

/// <summary>
/// 림버스 컴퍼니 7대죄 속성.
/// </summary>
public enum Sin
{
    Wrath,
    Lust,
    Sloth,
    Gluttony,
    Gloom,
    Pride,
    Envy,
}

public static class SinExtensions
{
    private static readonly Dictionary<Sin, string> ResourceIds = new()
    {
        { Sin.Wrath, "sin_wrath" },
        { Sin.Lust, "sin_lust" },
        { Sin.Sloth, "sin_sloth" },
        { Sin.Gluttony, "sin_gluttony" },
        { Sin.Gloom, "sin_gloom" },
        { Sin.Pride, "sin_pride" },
        { Sin.Envy, "sin_envy" },
    };

    /// <summary>SharedResourceManager에서 사용하는 ID.</summary>
    public static string ToResourceId(this Sin sin) => ResourceIds[sin];

    /// <summary>현재 값 조회.</summary>
    public static int Get(this Sin sin) => SharedResourceManager.Get(sin.ToResourceId());

    /// <summary>값 변경. sync: 멀티 동기화 여부.</summary>
    public static void Modify(this Sin sin, int delta, bool sync = true)
        => SharedResourceManager.Modify(sin.ToResourceId(), delta, sync);

    /// <summary>절대값 설정.</summary>
    public static void Set(this Sin sin, int value, bool sync = true)
        => SharedResourceManager.Set(sin.ToResourceId(), value, sync);

    /// <summary>7종 전부 SharedResourceManager에 등록.</summary>
    public static void RegisterAll()
    {
        foreach (Sin sin in Enum.GetValues(typeof(Sin)))
        {
            SharedResourceManager.Register(sin.ToResourceId());
        }
    }

    /// <summary>로컬라이제이션 키.</summary>
    public static string ToLocKey(this Sin sin) => $"SIN_{sin.ToString().ToUpperInvariant()}";

    /// <summary>현재 게임 언어로 번역된 이름.</summary>
    public static string ToDisplayName(this Sin sin) => Loc.Get(sin.ToLocKey());
}