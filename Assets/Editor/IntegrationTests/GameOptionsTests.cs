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
        Assert.That(data.bgmVolume, Is.EqualTo(1f));
        Assert.That(data.bgmMuted, Is.False);
        Assert.That(data.seVolume, Is.EqualTo(1f));
        Assert.That(data.seMuted, Is.False);
        Assert.That(data.voiceVolume, Is.EqualTo(1f));
        Assert.That(data.voiceMuted, Is.False);
        Assert.That(data.voiceAutoPlay, Is.True);
        Assert.That(data.version, Is.EqualTo(GameOptionsData.CurrentVersion));
    }

    [Test]
    public void SaveAndLoad_RoundTripsDisabledOption()
    {
        GameOptionsData source = new GameOptionsData
        {
            dialogueWindowClickAdvance = false,
            bgmVolume = 0.25f,
            bgmMuted = true,
            seVolume = 0.75f,
            seMuted = true,
            voiceVolume = 0.5f,
            voiceMuted = true,
            voiceAutoPlay = false
        };
        string message;

        Assert.That(GameOptionsManager.TrySaveToPath(source, GetTestPath(), out message), Is.True, message);
        GameOptionsData restored = GameOptionsManager.LoadFromPath(GetTestPath(), false);

        Assert.That(restored.dialogueWindowClickAdvance, Is.False);
        Assert.That(restored.bgmVolume, Is.EqualTo(0.25f).Within(0.001f));
        Assert.That(restored.bgmMuted, Is.True);
        Assert.That(restored.seVolume, Is.EqualTo(0.75f).Within(0.001f));
        Assert.That(restored.seMuted, Is.True);
        Assert.That(restored.voiceVolume, Is.EqualTo(0.5f).Within(0.001f));
        Assert.That(restored.voiceMuted, Is.True);
        Assert.That(restored.voiceAutoPlay, Is.False);
        Assert.That(restored.version, Is.EqualTo(GameOptionsData.CurrentVersion));
    }

    [Test]
    public void LoadFromPath_BrokenJsonUsesEnabledDefault()
    {
        File.WriteAllText(GetTestPath(), "{ broken json");

        GameOptionsData data = GameOptionsManager.LoadFromPath(GetTestPath(), false);

        Assert.That(data.dialogueWindowClickAdvance, Is.True);
        Assert.That(data.bgmVolume, Is.EqualTo(1f));
        Assert.That(data.seVolume, Is.EqualTo(1f));
        Assert.That(data.voiceVolume, Is.EqualTo(1f));
        Assert.That(data.voiceAutoPlay, Is.True);
    }

    [Test]
    public void LoadFromPath_VersionOneMigratesAudioDefaults()
    {
        File.WriteAllText(
            GetTestPath(),
            "{\"version\":1,\"dialogueWindowClickAdvance\":false}");

        GameOptionsData data = GameOptionsManager.LoadFromPath(GetTestPath(), false);

        Assert.That(data.version, Is.EqualTo(GameOptionsData.CurrentVersion));
        Assert.That(data.dialogueWindowClickAdvance, Is.False);
        Assert.That(data.bgmVolume, Is.EqualTo(1f));
        Assert.That(data.bgmMuted, Is.False);
        Assert.That(data.seVolume, Is.EqualTo(1f));
        Assert.That(data.seMuted, Is.False);
        Assert.That(data.voiceVolume, Is.EqualTo(1f));
        Assert.That(data.voiceMuted, Is.False);
        Assert.That(data.voiceAutoPlay, Is.True);
    }

    [Test]
    public void LoadFromPath_VersionTwoMigratesVoiceDefaults()
    {
        File.WriteAllText(
            GetTestPath(),
            "{\"version\":2,\"bgmVolume\":0.4,\"seVolume\":0.6}");

        GameOptionsData data = GameOptionsManager.LoadFromPath(GetTestPath(), false);

        Assert.That(data.version, Is.EqualTo(GameOptionsData.CurrentVersion));
        Assert.That(data.bgmVolume, Is.EqualTo(0.4f).Within(0.001f));
        Assert.That(data.seVolume, Is.EqualTo(0.6f).Within(0.001f));
        Assert.That(data.voiceVolume, Is.EqualTo(1f));
        Assert.That(data.voiceMuted, Is.False);
        Assert.That(data.voiceAutoPlay, Is.True);
    }

    [Test]
    public void SaveAndLoad_ClampsAudioVolumes()
    {
        GameOptionsData source = new GameOptionsData
        {
            bgmVolume = -0.5f,
            seVolume = 1.5f,
            voiceVolume = 2f
        };
        string message;

        Assert.That(
            GameOptionsManager.TrySaveToPath(source, GetTestPath(), out message),
            Is.True,
            message);
        GameOptionsData restored = GameOptionsManager.LoadFromPath(GetTestPath(), false);

        Assert.That(restored.bgmVolume, Is.EqualTo(0f));
        Assert.That(restored.seVolume, Is.EqualTo(1f));
        Assert.That(restored.voiceVolume, Is.EqualTo(1f));
    }

    [TestCase("", "Line01", "Audio/Voice/Line01")]
    [TestCase("TestHeroine", "Line01", "Audio/Voice/TestHeroine/Line01")]
    [TestCase("TestHeroine", "Scenes/Intro01", "Audio/Voice/TestHeroine/Scenes/Intro01")]
    [TestCase("Ignored", "Audio/Voice/Common/System01", "Audio/Voice/Common/System01")]
    public void VoiceResourcePath_UsesStableResourcesConvention(
        string heroineId,
        string voiceId,
        string expected)
    {
        Assert.That(
            AudioManager.BuildVoiceResourcePath(heroineId, voiceId),
            Is.EqualTo(expected));
    }

    [Test]
    public void VoiceResourcePath_EmptyVoiceIdDoesNotRequestAudio()
    {
        Assert.That(
            AudioManager.BuildVoiceResourcePath("TestHeroine", " "),
            Is.Empty);
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
