#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using UnityEngine;

public class DevelopmentHeroineOverrideTests
{
    private HeroineProfileData defaultProfile;
    private HeroineProfileData testProfile;
    private string originalHeroineId;

    [SetUp]
    public void SetUp()
    {
        originalHeroineId = DevelopmentHeroineOverride.HeroineId;
        defaultProfile = ScriptableObject.CreateInstance<HeroineProfileData>();
        defaultProfile.heroineId = "DefaultHeroine";
        testProfile = ScriptableObject.CreateInstance<HeroineProfileData>();
        testProfile.heroineId = "TestHeroine";
        DevelopmentHeroineOverride.SetHeroineId(string.Empty);
    }

    [TearDown]
    public void TearDown()
    {
        DevelopmentHeroineOverride.SetHeroineId(originalHeroineId);
        Object.DestroyImmediate(defaultProfile);
        Object.DestroyImmediate(testProfile);
    }

    [Test]
    public void ResolveProfile_ReturnsLocalDevelopmentProfileForNewGame()
    {
        DevelopmentHeroineOverride.SetHeroineId(" TestHeroine ");

        HeroineProfileData result = DevelopmentHeroineOverride.ResolveProfile(
            new[] { defaultProfile, testProfile },
            false);

        Assert.That(result, Is.SameAs(testProfile));
    }

    [Test]
    public void ResolveProfile_DoesNotOverrideSaveLoad()
    {
        DevelopmentHeroineOverride.SetHeroineId("TestHeroine");

        HeroineProfileData result = DevelopmentHeroineOverride.ResolveProfile(
            new[] { defaultProfile, testProfile },
            true);

        Assert.That(result, Is.Null);
    }

    [Test]
    public void ResolveProfile_FallsBackWhenConfiguredProfileDoesNotExist()
    {
        DevelopmentHeroineOverride.SetHeroineId("MissingHeroine");

        HeroineProfileData result = DevelopmentHeroineOverride.ResolveProfile(
            new[] { defaultProfile, testProfile },
            false);

        Assert.That(result, Is.Null);
    }
}
#endif
