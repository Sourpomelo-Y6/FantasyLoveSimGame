#if UNITY_INCLUDE_TESTS
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

public class TrainingDialogueAssetExporterIntegrationTests
{
    private const string HeroineId = "__TrainingDialogueExporterTest";
    private const string HeroineFolder = "Assets/Resources/Heroines/" + HeroineId;
    private const string AssetPath = HeroineFolder + "/TrainingDialogues/HeroineTrainingDialogueData.asset";
    private string outputFolder;
    private HeroineProfileData profile;

    [SetUp]
    public void SetUp()
    {
        AssetDatabase.DeleteAsset(HeroineFolder);
        outputFolder = Path.Combine(Path.GetFullPath("Temp"), "TrainingDialogueExporterIntegrationTests");
        if (Directory.Exists(outputFolder))
        {
            Directory.Delete(outputFolder, true);
        }
        Directory.CreateDirectory(outputFolder);
        profile = ScriptableObject.CreateInstance<HeroineProfileData>();
        profile.heroineId = HeroineId;
    }

    [TearDown]
    public void TearDown()
    {
        if (profile != null)
        {
            UnityEngine.Object.DestroyImmediate(profile);
        }
        AssetDatabase.DeleteAsset(HeroineFolder);
        AssetDatabase.Refresh();
        if (Directory.Exists(outputFolder))
        {
            Directory.Delete(outputFolder, true);
        }
    }

    [Test]
    public void ExportTrainingDialogues_WritesFiveStatesAndMergesDuplicateMessages()
    {
        string[] states = { "SelectedBeforeFirstStep", "SelectedAfterFirstStep", "PlayerLpConsumed", "HeroineLpConsumed", "SimultaneousLpConsumed" };
        HeroineTrainingDialogueData data = CreateDataAsset(HeroineId);
        foreach (string state in states)
        {
            data.entries.Add(Entry("CooperativeDrill", state, " 候補A ", "候補B", "候補A"));
        }
        data.entries.Add(Entry(" CooperativeDrill ", "PlayerLpConsumed", "候補C"));
        data.entries.Add(Entry("CooperativeDrill", "HeroineLpConsumed", " "));
        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();
        LogAssert.Expect(LogType.Warning, "重複した訓練セリフ枠を統合しました: CooperativeDrill / PlayerLpConsumed");
        LogAssert.Expect(LogType.Warning, "重複した訓練セリフ枠を統合しました: CooperativeDrill / HeroineLpConsumed");

        HeroineUnityDataExporter.HeroineUnityExportReport report = Export();
        TrainingDialogueExportFile exported = ReadExport();

        Assert.That(exported.schemaVersion, Is.EqualTo(1));
        Assert.That(exported.heroineId, Is.EqualTo(HeroineId));
        Assert.That(exported.source, Is.EqualTo("Unity"));
        Assert.That(exported.items.Count, Is.EqualTo(5));
        CollectionAssert.AreEquivalent(states, exported.items.Select(item => item.visualState));
        TrainingDialogueExportItem playerLp = exported.items.Single(item => item.visualState == "PlayerLpConsumed");
        Assert.That(playerLp.trainingId, Is.EqualTo("CooperativeDrill"));
        Assert.That(playerLp.messages, Is.EqualTo(new[] { "候補A", "候補B", "候補C" }));
        Assert.That(report.trainingDialogueEntryCount, Is.EqualTo(5));
    }

    [Test]
    public void ExportTrainingDialogues_WritesEmptyItemsWhenDataIsMissing()
    {
        LogAssert.Expect(
            LogType.Warning,
            "訓練セリフデータが見つかりません: Resources/Heroines/" + HeroineId + "/TrainingDialogues/HeroineTrainingDialogueData");

        HeroineUnityDataExporter.HeroineUnityExportReport report = Export();
        TrainingDialogueExportFile exported = ReadExport();

        Assert.That(exported.items, Is.Empty);
        Assert.That(report.trainingDialogueEntryCount, Is.Zero);
    }

    [Test]
    public void ExportTrainingDialogues_WritesEmptyItemsForDifferentHeroineData()
    {
        CreateDataAsset("OtherHeroine").entries.Add(
            Entry("CooperativeDrill", "PlayerLpConsumed", "出力しない候補"));
        AssetDatabase.SaveAssets();
        LogAssert.Expect(
            LogType.Warning,
            "訓練セリフデータのheroineIdが一致しません: OtherHeroine / " + HeroineId);

        HeroineUnityDataExporter.HeroineUnityExportReport report = Export();
        TrainingDialogueExportFile exported = ReadExport();

        Assert.That(exported.items, Is.Empty);
        Assert.That(report.trainingDialogueEntryCount, Is.Zero);
    }

    private HeroineUnityDataExporter.HeroineUnityExportReport Export()
    {
        HeroineUnityDataExporter.HeroineUnityExportReport report =
            new HeroineUnityDataExporter.HeroineUnityExportReport();
        HeroineUnityDataExporter.ExportTrainingDialogues(profile, outputFolder, report);
        return report;
    }

    private HeroineTrainingDialogueData CreateDataAsset(string dataHeroineId)
    {
        EnsureFolder("Assets/Resources");
        EnsureFolder("Assets/Resources/Heroines");
        EnsureFolder(HeroineFolder);
        EnsureFolder(HeroineFolder + "/TrainingDialogues");
        HeroineTrainingDialogueData data = ScriptableObject.CreateInstance<HeroineTrainingDialogueData>();
        data.heroineId = dataHeroineId;
        data.entries = new List<HeroineTrainingDialogueEntry>();
        AssetDatabase.CreateAsset(data, AssetPath);
        return data;
    }

    private static HeroineTrainingDialogueEntry Entry(
        string trainingId,
        string state,
        params string[] messages)
    {
        return new HeroineTrainingDialogueEntry
        {
            trainingId = trainingId,
            visualState = (TrainingVisualState)Enum.Parse(typeof(TrainingVisualState), state),
            messages = messages.ToList()
        };
    }

    private TrainingDialogueExportFile ReadExport()
    {
        string path = Path.Combine(outputFolder, "training_dialogues_from_unity.json");
        Assert.That(File.Exists(path), Is.True);
        return JsonUtility.FromJson<TrainingDialogueExportFile>(File.ReadAllText(path));
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }
        int separator = path.LastIndexOf('/');
        AssetDatabase.CreateFolder(path.Substring(0, separator), path.Substring(separator + 1));
    }

    [Serializable]
    private sealed class TrainingDialogueExportFile
    {
        public int schemaVersion;
        public string heroineId;
        public string source;
        public List<TrainingDialogueExportItem> items;
    }

    [Serializable]
    private sealed class TrainingDialogueExportItem
    {
        public string trainingId;
        public string visualState;
        public List<string> messages;
    }
}
#endif
