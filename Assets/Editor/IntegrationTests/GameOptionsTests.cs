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

    [TestCase(false, false, false)]
    [TestCase(false, true, false)]
    [TestCase(true, true, false)]
    [TestCase(true, false, true)]
    public void VoiceReplayPolicy_RequiresPreparedUnmutedVoice(
        bool hasPreparedVoice,
        bool voiceMuted,
        bool expected)
    {
        Assert.That(
            AudioManager.CanReplayVoice(hasPreparedVoice, voiceMuted),
            Is.EqualTo(expected));
    }

    [Test]
    public void ScheduledEventDefinition_CopiesVoiceIds()
    {
        ScheduledEventData data =
            UnityEngine.ScriptableObject.CreateInstance<ScheduledEventData>();
        try
        {
            data.preparationVoiceId = "SchedulePrepare01";
            data.eventVoiceId = "ScheduleResult01";

            ScheduledEventDefinition definition = data.ToDefinition();

            Assert.That(definition.PreparationVoiceId, Is.EqualTo("SchedulePrepare01"));
            Assert.That(definition.EventVoiceId, Is.EqualTo("ScheduleResult01"));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(data);
        }
    }

    [Test]
    public void AdditionalVoiceMetadata_DefaultsToEmpty()
    {
        Assert.That(new ActionReactionData().voiceId, Is.Null.Or.Empty);
        Assert.That(new ConversationChoice().responseVoiceId, Is.Null.Or.Empty);

        HeroineProfileData profile =
            UnityEngine.ScriptableObject.CreateInstance<HeroineProfileData>();
        try
        {
            Assert.That(profile.initialDialogueVoiceId, Is.Null.Or.Empty);
            Assert.That(profile.nextActionPromptVoiceId, Is.Null.Or.Empty);
            Assert.That(profile.morningGreetingVoiceId, Is.Null.Or.Empty);
            Assert.That(profile.goodNightGreetingVoiceId, Is.Null.Or.Empty);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(profile);
        }
    }

    [Test]
    public void TrainingDialogue_PrefersVoicedCandidateAndReturnsVoiceId()
    {
        HeroineTrainingDialogueData data =
            UnityEngine.ScriptableObject.CreateInstance<HeroineTrainingDialogueData>();
        try
        {
            data.entries.Add(new HeroineTrainingDialogueEntry
            {
                trainingId = "Training01",
                visualState = TrainingVisualState.SelectedBeforeFirstStep,
                messages = new System.Collections.Generic.List<string> { "従来のセリフ" },
                voicedMessages =
                    new System.Collections.Generic.List<HeroineTrainingDialogueCandidate>
                    {
                        new HeroineTrainingDialogueCandidate
                        {
                            message = "音声付きセリフ",
                            voiceId = "Training/Line01"
                        }
                    }
            });

            HeroineTrainingDialogueSelection selection = data.ResolveDialogue(
                "Training01",
                TrainingVisualState.SelectedBeforeFirstStep,
                string.Empty);

            Assert.That(selection.Message, Is.EqualTo("音声付きセリフ"));
            Assert.That(selection.VoiceId, Is.EqualTo("Training/Line01"));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(data);
        }
    }

    [Test]
    public void TrainingDialogue_LegacyMessagesRemainSupported()
    {
        HeroineTrainingDialogueData data =
            UnityEngine.ScriptableObject.CreateInstance<HeroineTrainingDialogueData>();
        try
        {
            data.entries.Add(new HeroineTrainingDialogueEntry
            {
                trainingId = "Training01",
                visualState = TrainingVisualState.SelectedAfterFirstStep,
                messages = new System.Collections.Generic.List<string> { "従来のセリフ" }
            });

            HeroineTrainingDialogueSelection selection = data.ResolveDialogue(
                "Training01",
                TrainingVisualState.SelectedAfterFirstStep,
                string.Empty);

            Assert.That(selection.Message, Is.EqualTo("従来のセリフ"));
            Assert.That(selection.VoiceId, Is.Empty);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(data);
        }
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

    [TestCase("", "")]
    [TestCase("UI/Confirm", "Audio/SE/UI/Confirm")]
    [TestCase(" /Battle/Victory/ ", "Audio/SE/Battle/Victory")]
    [TestCase("Audio/SE/Shop/PurchaseSuccess", "Audio/SE/Shop/PurchaseSuccess")]
    public void BuildSeResourcePath_NormalizesLogicalId(string seId, string expected)
    {
        Assert.That(AudioManager.BuildSeResourcePath(seId), Is.EqualTo(expected));
    }

    [TestCase("", false, false)]
    [TestCase("UI/Confirm", true, false)]
    [TestCase("UI/Confirm", false, true)]
    public void CanPlaySe_RequiresIdAndUnmutedOption(
        string seId,
        bool muted,
        bool expected)
    {
        Assert.That(AudioManager.CanPlaySe(seId, muted), Is.EqualTo(expected));
    }

    [TestCase("CloseButton", UiSePlayer.CancelSeId)]
    [TestCase("NextPageButton", UiSePlayer.NextSeId)]
    [TestCase("StatusButton", UiSePlayer.ConfirmSeId)]
    [TestCase("AttackButton", "")]
    [TestCase("PurchaseButton", "")]
    [TestCase("ScheduleButton", "")]
    public void ResolveDefaultSeId_SeparatesGenericAndResultSensitiveButtons(
        string buttonName,
        string expected)
    {
        Assert.That(UiSePlayer.ResolveDefaultSeId(buttonName), Is.EqualTo(expected));
    }

    private string GetTestPath()
    {
        return System.IO.Path.Combine(testFolder, "game_options.json");
    }
}
#endif
