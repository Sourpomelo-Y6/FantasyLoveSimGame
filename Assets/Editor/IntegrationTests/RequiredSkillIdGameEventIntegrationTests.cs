#if UNITY_INCLUDE_TESTS
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class RequiredSkillIdGameEventIntegrationTests
{
    private const string HeroineId = "__RequiredSkillIdGameEventTest";
    private const string HeroineFolder = "Assets/Resources/Heroines/" + HeroineId;
    private const string EventFolder = HeroineFolder + "/GameEvents";
    private const string EventAssetPath = EventFolder + "/SkillGateEvent.asset";
    private string importFolder;
    private string outputFolder;
    private HeroineProfileData profile;

    [SetUp]
    public void SetUp()
    {
        AssetDatabase.DeleteAsset(HeroineFolder);
        string tempRoot = Path.Combine(Path.GetFullPath("Temp"), "RequiredSkillIdGameEventIntegrationTests");
        if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, true);
        importFolder = Path.Combine(tempRoot, "Import");
        outputFolder = Path.Combine(tempRoot, "Export");
        Directory.CreateDirectory(Path.Combine(importFolder, "Data"));
        Directory.CreateDirectory(outputFolder);
        profile = ScriptableObject.CreateInstance<HeroineProfileData>();
        profile.heroineId = HeroineId;
        profile.gameEventResourcePath = "Heroines/" + HeroineId + "/GameEvents";
    }

    [TearDown]
    public void TearDown()
    {
        if (profile != null) UnityEngine.Object.DestroyImmediate(profile);
        AssetDatabase.DeleteAsset(HeroineFolder);
        AssetDatabase.Refresh();
        string tempRoot = Path.Combine(Path.GetFullPath("Temp"), "RequiredSkillIdGameEventIntegrationTests");
        if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, true);
    }

    [Test]
    public void ImportGameEvents_DistinguishesMissingAndExplicitEmptyRequiredSkillIds()
    {
        WriteImportJson("\"requiredSkillIds\":[\" skill_b \",\"skill_a\",\"SKILL_B\",\"unknown_skill\"],");
        ImportGameEvents();
        GameEventData data = LoadEvent();
        Assert.That(data.requiredSkillIds, Is.EqualTo(new[] { "skill_b", "skill_a", "unknown_skill" }));
        Assert.That(data.minAffection, Is.EqualTo(7));

        WriteImportJson(string.Empty);
        ImportGameEvents();
        data = LoadEvent();
        Assert.That(data.requiredSkillIds, Is.EqualTo(new[] { "skill_b", "skill_a", "unknown_skill" }));

        WriteImportJson("\"requiredSkillIds\":[],");
        ImportGameEvents();
        data = LoadEvent();
        Assert.That(data.requiredSkillIds, Is.Empty);
        Assert.That(data.minAffection, Is.EqualTo(7));
    }

    [Test]
    public void ExportGameEvents_NormalizesRequiredSkillIdsInJson()
    {
        EnsureFolder("Assets/Resources");
        EnsureFolder("Assets/Resources/Heroines");
        EnsureFolder(HeroineFolder);
        EnsureFolder(EventFolder);
        GameEventData data = ScriptableObject.CreateInstance<GameEventData>();
        data.eventId = "SkillGateEvent";
        data.requiredSkillIds = new List<string> { " skill_b ", "skill_a", "SKILL_B", "unknown_skill" };
        data.pages = new List<GameEventPageData> { new GameEventPageData { message = "テスト" } };
        AssetDatabase.CreateAsset(data, EventAssetPath);
        AssetDatabase.SaveAssets();

        HeroineUnityDataExporter.HeroineUnityExportReport report =
            new HeroineUnityDataExporter.HeroineUnityExportReport();
        HeroineUnityDataExporter.ExportGameEvents(profile, outputFolder, report);
        string jsonPath = Path.Combine(outputFolder, "game_events_from_unity.json");
        GameEventsFile exported = JsonUtility.FromJson<GameEventsFile>(File.ReadAllText(jsonPath));

        Assert.That(exported.items.Count, Is.EqualTo(1));
        Assert.That(
            exported.items[0].conditions.requiredSkillIds,
            Is.EqualTo(new[] { "skill_b", "skill_a", "unknown_skill" }));
        Assert.That(report.gameEventCount, Is.EqualTo(1));
    }

    private void ImportGameEvents()
    {
        HeroineAssetImporter.ImportGameEvents(
            importFolder,
            HeroineId,
            new HeroineAssetImporter.HeroineImportReport());
        AssetDatabase.SaveAssets();
    }

    private GameEventData LoadEvent()
    {
        GameEventData data = AssetDatabase.LoadAssetAtPath<GameEventData>(EventAssetPath);
        Assert.That(data, Is.Not.Null);
        return data;
    }

    private void WriteImportJson(string requiredSkillIdsProperty)
    {
        string json = "{\"schemaVersion\":1,\"heroineId\":\"" + HeroineId +
            "\",\"items\":[{\"id\":\"SkillGateEvent\",\"title\":\"Test\",\"category\":\"Manual\"," +
            "\"conditions\":{\"minAffection\":7," + requiredSkillIdsProperty + "\"once\":true}," +
            "\"lines\":[{\"speaker\":\"Heroine\",\"text\":\"テスト\",\"expression\":\"\"}]}]}";
        File.WriteAllText(Path.Combine(importFolder, "Data", "game_events_export.json"), json);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int separator = path.LastIndexOf('/');
        AssetDatabase.CreateFolder(path.Substring(0, separator), path.Substring(separator + 1));
    }

    [Serializable]
    private sealed class GameEventsFile
    {
        public List<GameEventItem> items;
    }

    [Serializable]
    private sealed class GameEventItem
    {
        public GameEventConditions conditions;
    }

    [Serializable]
    private sealed class GameEventConditions
    {
        public List<string> requiredSkillIds;
    }
}
#endif
