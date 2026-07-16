#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

public class TrainingDialogueAssetImporterIntegrationTests
{
    private const string HeroineId = "__TrainingDialogueImporterTest";
    private const string HeroineFolder = "Assets/Resources/Heroines/" + HeroineId;
    private const string AssetPath = HeroineFolder + "/TrainingDialogues/HeroineTrainingDialogueData.asset";
    private string exportFolder;

    [SetUp]
    public void SetUp()
    {
        AssetDatabase.DeleteAsset(HeroineFolder);
        exportFolder = Path.Combine(Path.GetFullPath("Temp"), "TrainingDialogueImporterIntegrationTests");
        if (Directory.Exists(exportFolder))
        {
            Directory.Delete(exportFolder, true);
        }
        Directory.CreateDirectory(Path.Combine(exportFolder, "Data"));
    }

    [TearDown]
    public void TearDown()
    {
        AssetDatabase.DeleteAsset(HeroineFolder);
        AssetDatabase.Refresh();
        if (Directory.Exists(exportFolder))
        {
            Directory.Delete(exportFolder, true);
        }
    }

    [Test]
    public void ImportTrainingDialogues_CreatesAndPersistsAssetWithMultipleMessages()
    {
        WriteJson("{\"schemaVersion\":1,\"heroineId\":\"" + HeroineId +
            "\",\"items\":[{\"trainingId\":\"\",\"visualState\":\"PlayerLpConsumed\",\"messages\":[\" 候補A \",\"候補B\",\"候補A\"]}]}");

        HeroineAssetImporter.HeroineImportReport report = Import();
        AssetDatabase.SaveAssets();
        HeroineTrainingDialogueData data = AssetDatabase.LoadAssetAtPath<HeroineTrainingDialogueData>(AssetPath);

        Assert.That(data, Is.Not.Null);
        Assert.That(data.heroineId, Is.EqualTo(HeroineId));
        Assert.That(data.entries.Count, Is.EqualTo(1));
        Assert.That(data.entries[0].visualState, Is.EqualTo(TrainingVisualState.PlayerLpConsumed));
        Assert.That(data.entries[0].messages, Is.EqualTo(new[] { "候補A", "候補B" }));
        Assert.That(report.trainingDialogueEntryCount, Is.EqualTo(1));
    }

    [Test]
    public void ImportTrainingDialogues_LeavesExistingAssetUnchangedWhenJsonIsMissing()
    {
        HeroineTrainingDialogueData existing = CreateExistingAsset("維持する候補");

        HeroineAssetImporter.ImportTrainingDialogues(
            exportFolder, HeroineId, new HeroineAssetImporter.HeroineImportReport());
        AssetDatabase.SaveAssets();
        HeroineTrainingDialogueData reloaded = AssetDatabase.LoadAssetAtPath<HeroineTrainingDialogueData>(AssetPath);

        Assert.That(reloaded.entries.Count, Is.EqualTo(1));
        Assert.That(reloaded.entries[0].messages, Is.EqualTo(new[] { "維持する候補" }));
        Assert.That(reloaded, Is.SameAs(existing));
    }

    [Test]
    public void ImportTrainingDialogues_LeavesExistingAssetUnchangedForDifferentHeroine()
    {
        CreateExistingAsset("維持する候補");
        WriteJson("{\"schemaVersion\":1,\"heroineId\":\"OtherHeroine\",\"items\":[]}");
        LogAssert.Expect(LogType.Warning, "training_dialogues_export.json のheroineIdが一致しないためスキップしました。");

        Import();

        AssertExistingMessage("維持する候補");
    }

    [Test]
    public void ImportTrainingDialogues_LeavesExistingAssetUnchangedForUnsupportedSchema()
    {
        CreateExistingAsset("維持する候補");
        WriteJson("{\"schemaVersion\":2,\"heroineId\":\"" + HeroineId + "\",\"items\":[]}");
        LogAssert.Expect(LogType.Warning, "未対応のtraining_dialogues schemaVersionのためスキップしました: 2");

        Import();

        AssertExistingMessage("維持する候補");
    }

    [Test]
    public void ImportTrainingDialogues_SkipsUnknownTrainingAndVisualState()
    {
        WriteJson("{\"schemaVersion\":1,\"heroineId\":\"" + HeroineId +
            "\",\"items\":[" +
            "{\"trainingId\":\"MissingTraining\",\"visualState\":\"PlayerLpConsumed\",\"messages\":[\"候補\"]}," +
            "{\"trainingId\":\"\",\"visualState\":\"UnknownState\",\"messages\":[\"候補\"]}]}");
        LogAssert.Expect(LogType.Warning, "存在しないtrainingIdの訓練セリフをスキップしました: MissingTraining");
        LogAssert.Expect(LogType.Warning, "未知のvisualStateをスキップしました: UnknownState");

        HeroineAssetImporter.HeroineImportReport report = Import();
        HeroineTrainingDialogueData data = AssetDatabase.LoadAssetAtPath<HeroineTrainingDialogueData>(AssetPath);

        Assert.That(data, Is.Not.Null);
        Assert.That(data.entries, Is.Empty);
        Assert.That(report.trainingDialogueEntryCount, Is.Zero);
    }

    private HeroineAssetImporter.HeroineImportReport Import()
    {
        HeroineAssetImporter.HeroineImportReport report = new HeroineAssetImporter.HeroineImportReport();
        HeroineAssetImporter.ImportTrainingDialogues(exportFolder, HeroineId, report);
        return report;
    }

    private static HeroineTrainingDialogueData CreateExistingAsset(string message)
    {
        EnsureFolder("Assets/Resources");
        EnsureFolder("Assets/Resources/Heroines");
        EnsureFolder(HeroineFolder);
        EnsureFolder(HeroineFolder + "/TrainingDialogues");
        HeroineTrainingDialogueData data = ScriptableObject.CreateInstance<HeroineTrainingDialogueData>();
        data.heroineId = HeroineId;
        data.entries = new List<HeroineTrainingDialogueEntry>
        {
            new HeroineTrainingDialogueEntry
            {
                trainingId = string.Empty,
                visualState = TrainingVisualState.SelectedBeforeFirstStep,
                messages = new List<string> { message }
            }
        };
        AssetDatabase.CreateAsset(data, AssetPath);
        AssetDatabase.SaveAssets();
        return data;
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

    private static void AssertExistingMessage(string expected)
    {
        HeroineTrainingDialogueData data = AssetDatabase.LoadAssetAtPath<HeroineTrainingDialogueData>(AssetPath);
        Assert.That(data.entries.Count, Is.EqualTo(1));
        Assert.That(data.entries[0].messages, Is.EqualTo(new[] { expected }));
    }

    private void WriteJson(string json)
    {
        File.WriteAllText(Path.Combine(exportFolder, "Data", "training_dialogues_export.json"), json);
    }
}
#endif
