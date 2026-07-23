#if UNITY_INCLUDE_TESTS
using System;
using System.IO;
using NUnit.Framework;

public class GameOptionsTests
{
    private string testFolder;

    [SetUp]
    public void SetUp()
    {
        testFolder = Path.Combine(
            Path.GetFullPath("Temp"),
            "GameOptionsTests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testFolder);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(testFolder)) Directory.Delete(testFolder, true);
    }

    [Test]
    public void LoadFromPath_MissingFileUsesEnabledDefault()
    {
        GameOptionsData data = GameOptionsManager.LoadFromPath(GetTestPath(), false);

        Assert.That(data.dialogueWindowClickAdvance, Is.True);
        Assert.That(data.version, Is.EqualTo(GameOptionsData.CurrentVersion));
    }

    [Test]
    public void SaveAndLoad_RoundTripsDisabledOption()
    {
        GameOptionsData source = new GameOptionsData
        {
            dialogueWindowClickAdvance = false
        };
        string message;

        Assert.That(GameOptionsManager.TrySaveToPath(source, GetTestPath(), out message), Is.True, message);
        GameOptionsData restored = GameOptionsManager.LoadFromPath(GetTestPath(), false);

        Assert.That(restored.dialogueWindowClickAdvance, Is.False);
        Assert.That(restored.version, Is.EqualTo(GameOptionsData.CurrentVersion));
    }

    [Test]
    public void LoadFromPath_BrokenJsonUsesEnabledDefault()
    {
        File.WriteAllText(GetTestPath(), "{ broken json");

        GameOptionsData data = GameOptionsManager.LoadFromPath(GetTestPath(), false);

        Assert.That(data.dialogueWindowClickAdvance, Is.True);
    }

    [TestCase(false, true, true, true, false)]
    [TestCase(true, false, true, true, false)]
    [TestCase(true, true, false, true, false)]
    [TestCase(true, true, true, false, false)]
    [TestCase(true, true, true, true, true)]
    public void ClickPolicy_RequiresAllConditions(
        bool componentEnabled,
        bool optionEnabled,
        bool nextVisible,
        bool nextInteractable,
        bool expected)
    {
        Assert.That(
            DialogueClickAdvancePolicy.CanAdvance(
                componentEnabled,
                optionEnabled,
                nextVisible,
                nextInteractable),
            Is.EqualTo(expected));
    }

    private string GetTestPath()
    {
        return System.IO.Path.Combine(testFolder, "game_options.json");
    }
}
#endif
