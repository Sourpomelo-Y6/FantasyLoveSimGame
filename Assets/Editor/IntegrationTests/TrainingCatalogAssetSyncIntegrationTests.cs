using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class TrainingCatalogAssetSyncIntegrationTests
{
    private string exportFolder;
    private string trainingAssetPath;
    private string nodeAssetPath;

    [SetUp]
    public void SetUp()
    {
        string suffix = Guid.NewGuid().ToString("N");
        trainingAssetPath = "Assets/Resources/Training/TrainingCatalogSync_" + suffix + ".asset";
        nodeAssetPath = "Assets/Resources/SkillTreeNodes/TrainingCatalogSync_" + suffix + ".asset";
        exportFolder = Path.Combine(Path.GetTempPath(), "FantasyLoveSimTrainingCatalogImport", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(exportFolder, "Data"));
    }

    [TearDown]
    public void TearDown()
    {
        AssetDatabase.DeleteAsset(trainingAssetPath);
        AssetDatabase.DeleteAsset(nodeAssetPath);
        if (Directory.Exists(exportFolder)) Directory.Delete(exportFolder, true);
        AssetDatabase.Refresh();
    }

    [Test]
    public void Import_UpdatesAvailabilityAndHeroineUnlockNode()
    {
        TrainingData training = ScriptableObject.CreateInstance<TrainingData>();
        training.trainingId = "CatalogSyncTraining";
        AssetDatabase.CreateAsset(training, trainingAssetPath);
        SkillTreeNodeData node = ScriptableObject.CreateInstance<SkillTreeNodeData>();
        node.nodeId = "CatalogSyncNode";
        node.owner = SkillTreeOwner.Heroine;
        node.targetHeroineId = "TestHeroine";
        node.unlockedTrainingIds = new List<string>();
        AssetDatabase.CreateAsset(node, nodeAssetPath);
        AssetDatabase.SaveAssets();

        File.WriteAllText(Path.Combine(exportFolder, "Data", "training_catalog_export.json"),
            "{\"schemaVersion\":1,\"heroineId\":\"TestHeroine\",\"items\":[{" +
            "\"trainingId\":\"CatalogSyncTraining\",\"unlockedByDefault\":false,\"sortOrder\":25," +
            "\"occurrenceType\":\"OncePerSave\",\"visibleConditionRanks\":[\"Excellent\"]," +
            "\"executableConditionRanks\":[\"Good\",\"Excellent\"]," +
            "\"requiredCompletedTrainingIds\":[\"Preparation\"],\"requireAllCompletedTrainings\":false," +
            "\"hideUntilPrerequisitesMet\":true,\"hideAfterCompletion\":true," +
            "\"unlockNodeIds\":[\"CatalogSyncNode\"]}]}" );
        HeroineAssetImporter.HeroineImportReport report = new HeroineAssetImporter.HeroineImportReport();

        TrainingCatalogAssetSync.Import(exportFolder, "TestHeroine", report);

        Assert.AreEqual(1, report.trainingCatalogUpdatedCount);
        Assert.AreEqual(TrainingOccurrenceType.OncePerSave, training.occurrenceType);
        CollectionAssert.AreEqual(new[] { TrainingConditionRank.Excellent }, training.visibleConditionRanks);
        CollectionAssert.AreEqual(new[] { "Preparation" }, training.requiredCompletedTrainingIds);
        Assert.IsTrue(training.hideAfterCompletion);
        CollectionAssert.Contains(node.unlockedTrainingIds, "CatalogSyncTraining");
    }
}
