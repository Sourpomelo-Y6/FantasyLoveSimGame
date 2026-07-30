#if UNITY_INCLUDE_TESTS
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class EndingVisualSettingsImportTests
{
    private const string HeroineId = "EndingVisualImportTest";
    private const string AssetFolder = "Assets/Resources/Heroines/" + HeroineId;

    [TearDown]
    public void TearDown()
    {
        AssetDatabase.DeleteAsset(AssetFolder);
        AssetDatabase.Refresh();
    }

    [Test]
    public void ImportEndings_PreservesVisualModeAndStillContinuation()
    {
        string importFolder = Path.Combine(
            Path.GetTempPath(),
            "FantasyLoveSimEndingImport",
            System.Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(importFolder, "Data"));
        try
        {
            File.WriteAllText(
                Path.Combine(importFolder, "Data", "endings_export.json"),
                "{\"heroineId\":\"" + HeroineId + "\",\"items\":[{" +
                "\"id\":\"GoodEnding\",\"title\":\"Good Ending\"," +
                "\"visualMode\":\"StillOnly\",\"keepStillAcrossPages\":true," +
                "\"conditions\":{\"minAffection\":1000}," +
                "\"lines\":[{\"speaker\":\"Heroine\",\"text\":\"Test\"}]}]}");

            HeroineAssetImporter.HeroineImportReport report =
                new HeroineAssetImporter.HeroineImportReport();
            HeroineAssetImporter.ImportEndings(importFolder, HeroineId, report);
            AssetDatabase.SaveAssets();

            EndingData ending = AssetDatabase.LoadAssetAtPath<EndingData>(
                AssetFolder + "/Endings/GoodEnding.asset");

            Assert.That(ending, Is.Not.Null);
            Assert.That(ending.visualMode, Is.EqualTo(EndingVisualMode.StillOnly));
            Assert.That(ending.keepStillAcrossPages, Is.True);
        }
        finally
        {
            if (Directory.Exists(importFolder))
            {
                Directory.Delete(importFolder, true);
            }
        }
    }
}
#endif
