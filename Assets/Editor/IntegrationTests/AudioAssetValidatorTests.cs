#if UNITY_INCLUDE_TESTS
using System.Linq;
using NUnit.Framework;

public class AudioAssetValidatorTests
{
    [Test]
    public void ValidateProjectAssets_ChecksEveryRegisteredBgmAndSe()
    {
        AudioAssetValidationReport report =
            AudioAssetValidator.ValidateProjectAssets();

        Assert.That(report.CheckedCount, Is.EqualTo(25));
        Assert.That(report.FoundCount, Is.InRange(0, report.CheckedCount));
        Assert.That(report.MissingCount, Is.EqualTo(
            report.CheckedCount - report.FoundCount));
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
