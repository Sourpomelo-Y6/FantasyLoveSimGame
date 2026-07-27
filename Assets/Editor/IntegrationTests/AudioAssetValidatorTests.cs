#if UNITY_INCLUDE_TESTS
using System.Linq;
using NUnit.Framework;

public class AudioAssetValidatorTests
{
    [Test]
    public void ValidateProjectAssets_ChecksRegisteredAudioAndDataVoiceReferences()
    {
        AudioAssetValidationReport report =
            AudioAssetValidator.ValidateProjectAssets();

        Assert.That(report.GetCheckedCount("BGM"), Is.EqualTo(5));
        Assert.That(report.GetCheckedCount("SE"), Is.EqualTo(22));
        Assert.That(report.GetCheckedCount("VOICE"), Is.GreaterThan(0));
        Assert.That(report.CheckedCount, Is.GreaterThanOrEqualTo(27));
        Assert.That(report.FoundCount, Is.InRange(0, report.CheckedCount));
        Assert.That(report.MissingCount, Is.EqualTo(
            report.CheckedCount - report.FoundCount));
    }

    [Test]
    public void CollectVoiceRequirements_UsesHeroineAndSourceField()
    {
        ConversationData data =
            UnityEngine.ScriptableObject.CreateInstance<ConversationData>();
        try
        {
            data.name = "ConversationTest";
            data.heroineId = "TestHeroine";
            data.voiceId = "Conversation/Legacy";
            data.lines.Add(new ConversationLineData
            {
                voiceId = "Conversation/Line01"
            });
            data.choices.Add(new ConversationChoice
            {
                responseVoiceId = " "
            });

            System.Collections.Generic.List<AudioAssetValidationEntry> entries =
                AudioAssetValidator.CollectVoiceRequirements(
                    new UnityEngine.Object[] { data });

            Assert.That(entries.Count, Is.EqualTo(2));
            Assert.That(
                entries.Select(entry => entry.ResourcePath),
                Is.EquivalentTo(new[]
                {
                    "Audio/Voice/TestHeroine/Conversation/Legacy",
                    "Audio/Voice/TestHeroine/Conversation/Line01"
                }));
            Assert.That(entries.All(entry => entry.Category == "VOICE"), Is.True);
            Assert.That(entries.All(entry => entry.Context == data), Is.True);
            Assert.That(entries.All(entry => entry.SourceLabel.Contains("ConversationTest")), Is.True);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(data);
        }
    }

    [Test]
    public void Validate_AllowsMultipleReferencesToSameVoiceClip()
    {
        AudioAssetValidationEntry[] requirements =
        {
            new AudioAssetValidationEntry(
                "VOICE",
                "Common/Line01",
                "Audio/Voice/TestHeroine/Common/Line01",
                "Source A"),
            new AudioAssetValidationEntry(
                "VOICE",
                "Common/Line01",
                "Audio/Voice/TestHeroine/Common/Line01",
                "Source B")
        };

        AudioAssetValidationReport report = AudioAssetValidator.Validate(
            requirements,
            new[] { "Audio/Voice/TestHeroine/Common/Line01" });

        Assert.That(report.GetCheckedCount("VOICE"), Is.EqualTo(2));
        Assert.That(report.GetFoundCount("VOICE"), Is.EqualTo(2));
        Assert.That(report.Missing, Is.Empty);
    }

    [Test]
    public void Validate_CountsFoundAndMissingRequirements()
    {
        AudioAssetValidationEntry[] requirements =
        {
            AudioAssetValidator.Bgm("Main"),
            AudioAssetValidator.Se("UI/Confirm"),
            AudioAssetValidator.Se("UI/Cancel")
        };

        AudioAssetValidationReport report = AudioAssetValidator.Validate(
            requirements,
            new[] { "Audio/Bgm/Main", "Audio/SE/UI/Confirm" });

        Assert.That(report.CheckedCount, Is.EqualTo(3));
        Assert.That(report.FoundCount, Is.EqualTo(2));
        Assert.That(report.MissingCount, Is.EqualTo(1));
        Assert.That(report.IsComplete, Is.False);
        Assert.That(report.Missing.Single().LogicalId, Is.EqualTo("UI/Cancel"));
    }

    [Test]
    public void Validate_AllowsCompleteLocalSetup()
    {
        AudioAssetValidationEntry requirement =
            AudioAssetValidator.Bgm("Training");

        AudioAssetValidationReport report = AudioAssetValidator.Validate(
            new[] { requirement },
            new[] { "Audio/Bgm/Training" });

        Assert.That(report.IsComplete, Is.True);
        Assert.That(report.FoundCount, Is.EqualTo(1));
        Assert.That(report.Missing, Is.Empty);
    }

    [TestCase(
        "Assets/Resources/Audio/Bgm/Main.ogg",
        "Audio/Bgm/Main")]
    [TestCase(
        "Assets/Resources/Audio/SE/UI/Confirm.wav",
        "Audio/SE/UI/Confirm")]
    [TestCase("Assets/Audio/Main.ogg", "")]
    public void ToResourcePath_RemovesResourcesPrefixAndExtension(
        string assetPath,
        string expected)
    {
        Assert.That(AudioAssetValidator.ToResourcePath(assetPath), Is.EqualTo(expected));
    }
}
#endif
