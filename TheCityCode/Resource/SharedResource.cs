using System;

namespace TheCity.TheCityCode.Resource;

/// <summary>
/// 파티 전체가 공유하는 정수형 전투 자원.
/// 전투 시작 시 0으로 초기화, 전투 종료 시 정리.
/// 값 변경 시 네트워크 동기화 자동 수행.
/// </summary>
public static class SharedResource
{
    public static int Value { get; private set; }
    public static bool IsActive { get; private set; }

    /// <summary>값이 변경될 때 발생. (oldValue, newValue)</summary>
    public static event Action<int, int>? ValueChanged;

    /// <summary>전투 시작 시 호출. 값을 0으로 초기화.</summary>
    public static void Initialize()
    {
        Value = 0;
        IsActive = true;
        ValueChanged?.Invoke(0, 0);
    }

    /// <summary>전투 종료 시 호출.</summary>
    public static void Cleanup()
    {
        IsActive = false;
        Value = 0;
    }

    /// <summary>
    /// 자원 값을 변경하고 네트워크 동기화를 수행.
    /// </summary>
    /// <param name="delta">변경량 (양수: 증가, 음수: 감소)</param>
    /// <param name="sync">네트워크 동기화 여부 (수신 측에서는 false)</param>
    public static void Modify(int delta, bool sync = true)
    {
        if (!IsActive) return;

        int oldValue = Value;
        Value += delta;
        ValueChanged?.Invoke(oldValue, Value);

        if (sync)
        {
            SharedResourceSync.SendUpdate(Value);
        }
    }

    /// <summary>
    /// 자원 값을 절대값으로 설정하고 네트워크 동기화를 수행.
    /// </summary>
    public static void Set(int newValue, bool sync = true)
    {
        if (!IsActive) return;

        int oldValue = Value;
        Value = newValue;
        ValueChanged?.Invoke(oldValue, Value);

        if (sync)
        {
            SharedResourceSync.SendUpdate(Value);
        }
    }
}
