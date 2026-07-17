using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>AssetTool とヒロイン別戦闘結果文章を安全に往復する。</summary>
public static class HeroineBattleMessageAssetSync
{
    private const string ResultImportPath = "Data/battle_result_events_export.json";
    private const string PanelImportPath = "Data/battle_panel_result_messages_export.json";
    private const string ResultExportName = "battle_result_events_from_unity.json";
    private const string PanelExportName = "battle_panel_result_messages_from_unity.json";

    public static BattleMessageImportSummary Import(string exportFolder, HeroineProfileData profile)
    {
        BattleMessageImportSummary summary = new BattleMessageImportSummary();
        if (profile == null) return summary;
        ImportResultEvents(Path.Combine(exportFolder, ResultImportPath), profile, summary);
        ImportPanelMessages(Path.Combine(exportFolder, PanelImportPath), profile, summary);
        AssetDatabase.SaveAssets();
        return summary;
    }

    public static void Export(HeroineProfileData profile, string outputFolder)
    {
        if (profile == null) return;
        BattleResultEventData[] events = Resources.LoadAll<BattleResultEventData>(profile.battleResultEventResourcePath);
        BattlePanelResultMessageData[] messages = Resources.LoadAll<BattlePanelResultMessageData>(profile.battlePanelResultMessageResourcePath);
        ResultEventsFile resultFile = new ResultEventsFile
        {
            schemaVersion = 1,
            heroineId = profile.heroineId,
            items = events.Where(x => x != null).OrderBy(x => x.battleResultEventType).ThenBy(x => x.battleContextId)
                .Select(x => new ResultEventItem
                {
                    eventId = GetAssetId(x, CreateEventId(x)),
                    resultType = x.battleResultEventType.ToString(),
                    battleContextId = x.battleContextId,
                    speakerType = x.speakerType.ToString(),
                    speakerName = x.speakerName,
                    message = x.message,
                    stillId = x.stillId,
                    visualMode = x.visualMode.ToString(),
                    expressionId = x.expressionId,
                    affectionChange = x.affectionChange,
                    unlockedOutfitIds = x.unlockedOutfitIds ?? Array.Empty<string>()
                }).ToArray()
        };
        PanelMessagesFile panelFile = new PanelMessagesFile
        {
            schemaVersion = 1,
            heroineId = profile.heroineId,
            items = messages.Where(x => x != null).OrderBy(x => x.resultType)
                .Select(x => new PanelMessageItem
                {
                    messageId = GetAssetId(x, x.resultType.ToString()),
                    resultType = x.resultType.ToString(),
                    message = x.message
                }).ToArray()
        };
        File.WriteAllText(Path.Combine(outputFolder, ResultExportName), JsonUtility.ToJson(resultFile, true));
        File.WriteAllText(Path.Combine(outputFolder, PanelExportName), JsonUtility.ToJson(panelFile, true));
    }

    private static void ImportResultEvents(string jsonPath, HeroineProfileData profile, BattleMessageImportSummary summary)
    {
        if (!File.Exists(jsonPath)) return;
        ResultEventsFile data = JsonUtility.FromJson<ResultEventsFile>(File.ReadAllText(jsonPath));
        if (!CanImport(data?.schemaVersion ?? 0, data?.heroineId, profile.heroineId, Path.GetFileName(jsonPath))) { summary.skippedCount++; return; }
        if (data.items == null) return;
        string folder = ToHeroineAssetFolder(profile.battleResultEventResourcePath, profile.heroineId);
        if (folder == null) return;
        EnsureFolder(folder);
        HashSet<string> keep = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (ResultEventItem item in data.items)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.eventId)) { summary.skippedCount++; continue; }
            string path = $"{folder}/{SafeFileName(item.eventId.Trim())}.asset";
            BattleResultEventData asset = AssetDatabase.LoadAssetAtPath<BattleResultEventData>(path);
            if (asset == null) { asset = ScriptableObject.CreateInstance<BattleResultEventData>(); AssetDatabase.CreateAsset(asset, path); summary.addedCount++; }
            else summary.updatedCount++;
            asset.battleResultEventType = Parse(item.resultType, BattleResultEventType.SoloVictory);
            asset.battleContextId = item.battleContextId ?? string.Empty;
            asset.speakerType = Parse(item.speakerType, ScheduledEventSpeakerType.Heroine);
            asset.speakerName = item.speakerName ?? string.Empty;
            asset.message = item.message ?? string.Empty;
            asset.stillId = item.stillId ?? string.Empty;
            asset.visualMode = Parse(item.visualMode, BattleResultVisualMode.Auto);
            asset.expressionId = item.expressionId ?? string.Empty;
            asset.affectionChange = item.affectionChange;
            asset.unlockedOutfitIds = CleanIds(item.unlockedOutfitIds).ToArray();
            EditorUtility.SetDirty(asset);
            keep.Add(path);
        }
        summary.deletedCount += DeleteMissing<BattleResultEventData>(folder, keep);
    }

    private static void ImportPanelMessages(string jsonPath, HeroineProfileData profile, BattleMessageImportSummary summary)
    {
        if (!File.Exists(jsonPath)) return;
        PanelMessagesFile data = JsonUtility.FromJson<PanelMessagesFile>(File.ReadAllText(jsonPath));
        if (!CanImport(data?.schemaVersion ?? 0, data?.heroineId, profile.heroineId, Path.GetFileName(jsonPath))) { summary.skippedCount++; return; }
        if (data.items == null) return;
        string folder = ToHeroineAssetFolder(profile.battlePanelResultMessageResourcePath, profile.heroineId);
        if (folder == null) return;
        EnsureFolder(folder);
        HashSet<string> keep = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (PanelMessageItem item in data.items)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.messageId)) { summary.skippedCount++; continue; }
            string path = $"{folder}/{SafeFileName(item.messageId.Trim())}.asset";
            BattlePanelResultMessageData asset = AssetDatabase.LoadAssetAtPath<BattlePanelResultMessageData>(path);
            if (asset == null) { asset = ScriptableObject.CreateInstance<BattlePanelResultMessageData>(); AssetDatabase.CreateAsset(asset, path); summary.addedCount++; }
            else summary.updatedCount++;
            asset.resultType = Parse(item.resultType, BattlePanelResultMessageType.Default);
            asset.message = item.message ?? string.Empty;
            EditorUtility.SetDirty(asset);
            keep.Add(path);
        }
        summary.deletedCount += DeleteMissing<BattlePanelResultMessageData>(folder, keep);
    }

    private static bool CanImport(int version, string jsonHeroineId, string expectedHeroineId, string fileName)
    {
        if (version != 1) { Debug.LogWarning(fileName + " のschemaVersionを確認してください。"); return false; }
        if (!string.IsNullOrWhiteSpace(jsonHeroineId) && !string.Equals(jsonHeroineId, expectedHeroineId, StringComparison.Ordinal))
        { Debug.LogWarning(fileName + " のheroineIdが対象ヒロインと一致しません。"); return false; }
        return true;
    }

    private static string CreateEventId(BattleResultEventData data)
    {
        string id = data.battleResultEventType.ToString();
        if (!string.IsNullOrWhiteSpace(data.battleContextId)) id += "_" + data.battleContextId.Trim();
        return id;
    }
    private static string GetAssetId(UnityEngine.Object asset, string fallback)
    {
        string path = AssetDatabase.GetAssetPath(asset);
        return string.IsNullOrWhiteSpace(path) ? fallback : Path.GetFileNameWithoutExtension(path);
    }
    private static string ToHeroineAssetFolder(string resourcePath, string heroineId)
    {
        string normalized = (resourcePath ?? string.Empty).Trim('/');
        string requiredPrefix = "Heroines/" + heroineId + "/";
        if (!normalized.StartsWith(requiredPrefix, StringComparison.Ordinal))
        {
            Debug.LogWarning("戦闘メッセージのImport先がヒロイン別Resources pathではないためスキップします: " + normalized);
            return null;
        }
        return "Assets/Resources/" + normalized;
    }
    private static T Parse<T>(string value, T fallback) where T : struct => Enum.TryParse(value, true, out T parsed) ? parsed : fallback;
    private static List<string> CleanIds(IEnumerable<string> ids) => (ids ?? Enumerable.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.Ordinal).ToList();
    private static string SafeFileName(string value) => string.Concat(value.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
    private static int DeleteMissing<T>(string folder, HashSet<string> keep) where T : UnityEngine.Object
    {
        int count = 0;
        foreach (string guid in AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folder }))
        { string path = AssetDatabase.GUIDToAssetPath(guid); if (!keep.Contains(path) && AssetDatabase.DeleteAsset(path)) count++; }
        return count;
    }
    private static void EnsureFolder(string path)
    {
        string[] parts = path.Split('/'); string current = parts[0];
        for (int i = 1; i < parts.Length; i++) { string next = current + "/" + parts[i]; if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]); current = next; }
    }

    [Serializable] private class ResultEventsFile { public int schemaVersion; public string heroineId; public ResultEventItem[] items; }
    [Serializable] private class ResultEventItem { public string eventId; public string resultType; public string battleContextId; public string speakerType; public string speakerName; public string message; public string stillId; public string visualMode; public string expressionId; public int affectionChange; public string[] unlockedOutfitIds; }
    [Serializable] private class PanelMessagesFile { public int schemaVersion; public string heroineId; public PanelMessageItem[] items; }
    [Serializable] private class PanelMessageItem { public string messageId; public string resultType; public string message; }
}

public sealed class BattleMessageImportSummary
{
    public int addedCount;
    public int updatedCount;
    public int deletedCount;
    public int skippedCount;
}
