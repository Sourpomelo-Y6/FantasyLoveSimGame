#if UNITY_INCLUDE_TESTS
using NUnit.Framework;

public class HeroineAffectionScaleImportTests
{
    [Test]
    public void LegacyScale_IsDetectedWhenEveryConversationUsesOldMaximum()
    {
        const string json =
            "{\"items\":[" +
            "{\"conditions\":{\"minAffection\":0,\"maxAffection\":100}}," +
            "{\"conditions\":{\"minAffection\":30,\"maxAffection\":100}}," +
            "{\"conditions\":{\"minAffection\":80,\"maxAffection\":100}}]}";

        Assert.That(HeroineAssetImporter.IsLikelyLegacyAffectionScale(json), Is.True);
    }

    [Test]
    public void CurrentScale_IsNotDetectedAsLegacy()
    {
        const string json =
            "{\"items\":[" +
            "{\"conditions\":{\"minAffection\":0,\"maxAffection\":9999}}," +
            "{\"conditions\":{\"minAffection\":300,\"maxAffection\":9999}}," +
            "{\"conditions\":{\"minAffection\":800,\"maxAffection\":9999}}]}";

        Assert.That(HeroineAssetImporter.IsLikelyLegacyAffectionScale(json), Is.False);
    }

    [Test]
    public void MixedExplicitRanges_AreNotRejectedAsLegacy()
    {
        const string json =
            "{\"items\":[" +
            "{\"conditions\":{\"minAffection\":0,\"maxAffection\":100}}," +
            "{\"conditions\":{\"minAffection\":101,\"maxAffection\":500}}," +
            "{\"conditions\":{\"minAffection\":501,\"maxAffection\":9999}}]}";

        Assert.That(HeroineAssetImporter.IsLikelyLegacyAffectionScale(json), Is.False);
    }
}
#endif
