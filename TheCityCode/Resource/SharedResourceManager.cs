using System;
using System.Collections.Generic;

namespace TheCity.TheCityCode.Resource;

/// <summary>
/// ID 기반 다중 자원 관리자.
/// 전투 시작 시 초기화, 전투 종료 시 정리.
/// 값 변경 시 이벤트 발행 + 네트워크 동기화.
/// </summary>
public static class SharedResourceManager
{
    private static readonly Dictionary<string, int> _resources = new();

    public static bool IsActive { get; private set; }

    // ── 이벤트 ──

    /// <summary>특정 자원의 값이 변경됨. (id, oldValue, newValue)</summary>
    public static event Action<string, int, int>? ValueChanged;

    /// <summary>새 자원이 등록됨. (id, initialValue)</summary>
    public static event Action<string, int>? ResourceRegistered;

    /// <summary>전투 시작, 모든 자원 초기화 완료.</summary>
    public static event Action? Initialized;

    /// <summary>전투 종료, 모든 자원 정리 완료.</summary>
    public static event Action? CleanedUp;

    // ── 자원 정의 ──

    /// <summary>
    /// 자원 종류를 등록. Initialize() 호출 전에 등록해야 함.
    /// 중복 등록 시 무시.
    /// </summary>
    public static void Register(string id, int initialValue = 0)
    {
        if (_resources.ContainsKey(id)) return;
        _resources[id] = initialValue;
        ResourceRegistered?.Invoke(id, initialValue);
    }

    // ── 라이프사이클 ──

    /// <summary>전투 시작 시 호출. 등록된 모든 자원을 0으로 초기화.</summary>
    public static void Initialize()
    {
        foreach (var id in _resources.Keys)
        {
            _resources[id] = 0;
        }
        IsActive = true;
        Initialized?.Invoke();
    }

    /// <summary>전투 종료 시 호출.</summary>
    public static void Cleanup()
    {
        IsActive = false;
        foreach (var id in _resources.Keys)
        {
            _resources[id] = 0;
        }
        CleanedUp?.Invoke();
    }

    // ── 값 접근 ──

    /// <summary>자원 값 조회. 미등록 시 0 반환.</summary>
    public static int Get(string id)
    {
        return _resources.TryGetValue(id, out var value) ? value : 0;
    }

    /// <summary>자원 존재 여부.</summary>
    public static bool Has(string id) => _resources.ContainsKey(id);

    /// <summary>등록된 모든 자원 ID 목록.</summary>
    public static IReadOnlyCollection<string> AllIds => _resources.Keys;

    // ── 값 변경 ──

    /// <summary>자원 값을 델타만큼 변경.</summary>
    /// <param name="id">자원 ID</param>
    /// <param name="delta">변경량</param>
    /// <param name="sync">네트워크 동기화 여부 (수신 측에서는 false)</param>
    public static void Modify(string id, int delta, bool sync = true)
    {
        if (!IsActive || !_resources.ContainsKey(id)) return;

        int oldValue = _resources[id];
        _resources[id] += delta;
        ValueChanged?.Invoke(id, oldValue, _resources[id]);

        if (sync)
        {
            SharedResourceSync.SendUpdate(id, _resources[id]);
        }
    }

    /// <summary>자원 값을 절대값으로 설정.</summary>
    public static void Set(string id, int newValue, bool sync = true)
    {
        if (!IsActive || !_resources.ContainsKey(id)) return;

        int oldValue = _resources[id];
        _resources[id] = newValue;
        ValueChanged?.Invoke(id, oldValue, newValue);

        if (sync)
        {
            SharedResourceSync.SendUpdate(id, newValue);
        }
    }
}
