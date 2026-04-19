using System.Collections.Generic;
using System.Text.Json;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;

namespace TheCity;

/// <summary>
/// 게임의 <see cref="LocManager"/>에 우리 <c>thecity</c> 테이블을 주입.
///
/// 배경: 게임의 <c>ModManager.GetModdedLocTables</c>는
/// <c>res://{mod_id}/localization/{lang}/{file}</c> 경로를 스캔하지만,
/// <b>게임이 이미 가지고 있는 기본 테이블</b>(map, static_hover_tips 등)에 대해서만 순회함.
/// 따라서 우리가 만든 새 테이블(<c>thecity</c>)은 게임이 순회 대상에 포함시키지 않아 로드되지 않음.
///
/// 해결:
/// 1) ModInit에서 <see cref="InjectForCurrentLanguage"/> 1회 수동 호출 (LocManager는 이미 초기화됨).
/// 2) <see cref="LocManager.SetLanguageInternal"/> Postfix로 이후 언어 변경 시 자동 재주입.
/// </summary>
internal static class LocTableInjector
{
    public const string TableName = "thecity";

    /// <summary>현재 LocManager 언어에 맞춰 테이블 주입 (ModInit에서 1회 호출).</summary>
    /// <remarks>
    /// ModInit은 <see cref="MegaCrit.Sts2.Core.Helpers.OneTimeInitialization.ExecuteEssential"/> 중
    /// <c>ModManager.Initialize</c>에서 호출되며, 이때는 아직 <see cref="LocManager.Initialize"/> 전이라
    /// <c>LocManager.Instance == null</c>일 수 있음. 이 경우 조용히 넘어가고
    /// <see cref="SetLanguageInternal_Patch"/>의 Postfix가 직후 LocManager 생성 시점에 주입을 수행.
    /// </remarks>
    public static void InjectForCurrentLanguage()
    {
        if (LocManager.Instance == null) return;  // Postfix가 곧 처리
        Inject(LocManager.Instance, LocManager.Instance.Language);
    }

    private static void Inject(LocManager instance, string language)
    {
        try
        {
            var dict = LoadDictForLanguage(language) ?? LoadDictForLanguage("eng");
            if (dict == null)
            {
                GD.PushError($"[{ModStart.ModId}] Loc: failed to load {TableName}.json for language '{language}' (and english fallback).");
                return;
            }

            var tablesField = AccessTools.Field(typeof(LocManager), "_tables");
            if (tablesField == null)
            {
                GD.PushError($"[{ModStart.ModId}] Loc: LocManager._tables field not found (BaseLib/game version mismatch?).");
                return;
            }

            var tables = (Dictionary<string, LocTable>)tablesField.GetValue(instance)!;
            tables[TableName] = new LocTable(TableName, dict);
            GD.Print($"[{ModStart.ModId}] Loc: injected '{TableName}' table with {dict.Count} keys for language '{language}'.");
        }
        catch (System.Exception ex)
        {
            GD.PushError($"[{ModStart.ModId}] Loc: failed to inject '{TableName}' table: {ex.Message}");
        }
    }

    private static Dictionary<string, string>? LoadDictForLanguage(string language)
    {
        var path = $"res://assets/localization/{language}/{TableName}.json";
        if (!Godot.FileAccess.FileExists(path)) return null;

        using var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
        if (file == null) return null;

        var json = file.GetAsText();
        return JsonSerializer.Deserialize<Dictionary<string, string>>(json);
    }

    /// <summary>언어 변경 시 재주입 (설정에서 언어 바꾸는 경우).</summary>
    [HarmonyPatch(typeof(LocManager), "SetLanguageInternal")]
    public static class SetLanguageInternal_Patch
    {
        public static void Postfix(LocManager __instance, string language) => Inject(__instance, language);
    }
}
