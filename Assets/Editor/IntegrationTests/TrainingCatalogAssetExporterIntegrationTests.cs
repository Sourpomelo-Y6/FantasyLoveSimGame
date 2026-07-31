#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class TrainingCatalogAssetExporterIntegrationTests
{
    private const string AssetFolder = "Assets/Resources/Training";
    private string assetPath;
    private string outputFolder;
    private HeroineProfileData profile;

    [SetUp]
    public void SetUp()
    {
        string suffix = Guid.NewGuid().ToString("N");
        assetPath = AssetFolder + "/TrainingCatalogExportTest_" + suffix + ".asset";
        outputFolder = Path.Combine(Path.GetTempPath(), "FantasyLoveSimTrainingCatalogTests", suffix);
        Directory.CreateDirectory(outputFolder);

        TrainingData training = ScriptableObject.CreateInstance<TrainingData>();
        training.trainingId = "TrainingCatalogExportTest_" + suffix;
        training.displayName = "条件付き訓練";
        training.trainingCategoryId = "IntegrationTest";
        training.sortOrder = 42;
        training.unlockedByDefault = false;
        training.occurrenceType = TrainingOccurrenceType.OncePerSave;
        training.visibleConditionRanks = new[] { TrainingConditionRank.Excellent };
        training.executableConditionRanks = new[]
        {
            TrainingConditionRank.Normal,
            TrainingConditionRank.Excellent
        };
        training.requiredCompletedTrainingIds = new[] { " TrialTraining ", "TrialTraining", "" };
        training.requireAllCompletedTrainings = false;
        training.hideUntilPrerequisitesMet = true;
        training.hideAfterCompletion = true;
        AssetDatabase.CreateAsset(training, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        profile = ScriptableObject.CreateInstance<HeroineProfileData>();
        profile.heroineId = "TrainingCatalogExportHeroine";
    }

    [TearDown]
    public void TearDown()
    {
        if (profile != null) UnityEngine.Object.DestroyImmediate(profile);
        AssetDatabase.DeleteAsset(assetPath);
        if (Directory.Exists(outputFolder)) Directory.Delete(outputFolder, true);
        AssetDatabase.Refresh();
    }

    [Test]
    public void ExportTrainingCatalog_IncludesPrerequisiteOnlyTrainingAndConditions()
    {
        HeroineUnityDataExporter.HeroineUnityExportReport report =
            new HeroineUnityDataExporter.HeroineUnityExportReport();

        HeroineUnityDataExporter.ExportTrainingCatalog(profile, outputFolder, report);

        string json = File.ReadAllText(Path.Combine(outputFolder, "training_catalog_from_unity.json"));
        TrainingCatalogFile exported = JsonUtility.FromJson<TrainingCatalogFile>(json);
        TrainingCatalogItem item = exported.items.Single(entry =>
            entry.trainingId == Path.GetFileNameWithoutExtension(assetPath));
        Assert.That(item.unlockedByDefault, Is.False);
        Assert.That(item.sortOrder, Is.EqualTo(42));
        Assert.That(item.occurrenceType, Is.EqualTo("OncePerSave"));
        Assert.That(item.visibleConditionRanks, Is.EqualTo(new[] { "Excellent" }));
        Assert.That(item.executableConditionRanks,
            Is.EqualTo(new[] { "Normal", "Excellent" }));
        Assert.That(item.requiredCompletedTrainingIds,
            Is.EqualTo(new[] { "TrialTraining" }));
        Assert.That(item.requireAllCompletedTrainings, Is.False);
        Assert.That(item.hideUntilPrerequisitesMet, Is.True);
        Assert.That(item.hideAfterCompletion, Is.True);
    }

    [Serializable]
    private sealed class TrainingCatalogFile
    {
        public List<TrainingCatalogItem> items;
    }

    [Serializable]
    private sealed class TrainingCatalogItem
    {
        public string trainingId;
        public bool unlockedByDefault;
        public int sortOrder;
        public string occurrenceType;
        public string[] visibleConditionRanks;
        public string[] executableConditionRanks;
        public string[] requiredCompletedTrainingIds;
        public bool requireAllCompletedTrainings;
        public bool hideUntilPrerequisitesMet;
        public bool hideAfterCompletion;
    }
}
#endif
