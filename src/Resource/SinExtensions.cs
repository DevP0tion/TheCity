using System;
using System.Collections.Generic;

namespace TheCity.Resource;

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

    /// <summary>한국어 이름.</summary>
    public static string ToKorean(this Sin sin) => sin switch
    {
        Sin.Wrath => "분노",
        Sin.Lust => "색욕",
        Sin.Sloth => "나태",
        Sin.Gluttony => "탐식",
        Sin.Gloom => "우울",
        Sin.Pride => "오만",
        Sin.Envy => "질투",
        _ => sin.ToString(),
    };

    /// <summary>영어 이름.</summary>
    public static string ToEnglish(this Sin sin) => sin switch
    {
        Sin.Wrath => "Wrath",
        Sin.Lust => "Lust",
        Sin.Sloth => "Sloth",
        Sin.Gluttony => "Gluttony",
        Sin.Gloom => "Gloom",
        Sin.Pride => "Pride",
        Sin.Envy => "Envy",
        _ => sin.ToString(),
    };
}
