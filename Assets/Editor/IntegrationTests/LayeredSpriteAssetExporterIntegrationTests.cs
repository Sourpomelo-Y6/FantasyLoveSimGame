using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

public class LayeredSpriteAssetExporterIntegrationTests
{
    private string outputFolder;

    [SetUp]
    public void SetUp()
    {
        outputFolder = Path.Combine(
            Path.GetTempPath(),
            "FantasyLoveSimLayeredSpriteExportTests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outputFolder);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(outputFolder))
        {
            Directory.Delete(outputFolder, true);
        }
    }

    [Test]
    public void ExportLayeredSprites_ExportsLegacyLayersForToolMigration()
    {
        HeroineProfileData profile = Resources.Load<HeroineProfileData>("Heroines/TestHeroineProfile");
        Assert.That(profile, Is.Not.Null);

        HeroineUnityDataExporter.ExportLayeredSprites(
            profile,
            outputFolder,
            new HeroineUnityDataExporter.HeroineUnityExportReport());

        string json = File.ReadAllText(Path.Combine(
            outputFolder,
            "heroine_layered_sprites_from_unity.json"));
        StringAssert.Contains("\"heroineId\": \"TestHeroine\"", json);
        StringAssert.Contains("\"assetId\": \"Costume_Default\"", json);
        StringAssert.Contains("\"layerKind\": \"Costume\"", json);
        StringAssert.Contains("\"assetId\": \"Expression_Neutral\"", json);
    }
}
