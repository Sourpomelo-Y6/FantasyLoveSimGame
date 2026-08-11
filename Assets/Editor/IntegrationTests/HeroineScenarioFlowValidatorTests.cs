#if UNITY_EDITOR
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class HeroineScenarioFlowValidatorTests
{
    private const string Root = "Assets/Resources/Heroines/ScenarioFlowValidatorTest";
    private HeroineProfileData profile;

    [SetUp]
    public void SetUp()
    {
        EnsureFolder("Assets/Resources/Heroines", "ScenarioFlowValidatorTest");
        EnsureFolder(Root, "GameEvents");
        EnsureFolder(Root, "Endings");

        GameEventData start = ScriptableObject.CreateInstance<GameEventData>();
        start.eventId = "Start";
        start.triggerType = GameEventTriggerType.GameStart;
        start.pages.Add(new GameEventPageData { message = "開始" });
        AssetDatabase.CreateAsset(start, Root + "/GameEvents/Start.asset");

        GameEventData broken = ScriptableObject.CreateInstance<GameEventData>();
        broken.eventId = "Broken";
        broken.triggerType = GameEventTriggerType.Manual;
        broken.requiredShownEventIds.Add("MissingEvent");
        broken.pages.Add(new GameEventPageData { message = "TestHeroineの仮文章" });
        AssetDatabase.CreateAsset(broken, Root + "/GameEvents/Broken.asset");

        EndingData ending = ScriptableObject.CreateInstance<EndingData>();
        ending.endingId = "Normal";
        ending.message = "完了";
        AssetDatabase.CreateAsset(ending, Root + "/Endings/Normal.asset");

        profile = ScriptableObject.CreateInstance<HeroineProfileData>();
        profile.heroineId = "ScenarioFlowValidatorTest";
        profile.gameEventResourcePath = "Heroines/ScenarioFlowValidatorTest/GameEvents";
        profile.conversationResourcePath = "Heroines/ScenarioFlowValidatorTest/Conversations";
        profile.actionResourcePath = "Heroines/ScenarioFlowValidatorTest/Actions";
        profile.scheduledEventResourcePath = "Heroines/ScenarioFlowValidatorTest/ScheduledEvents";
        profile.endingResourcePath = "Heroines/ScenarioFlowValidatorTest/Endings";
        AssetDatabase.Refresh();
    }

    [TearDown]
    public void TearDown()
    {
        if (profile != null) Object.DestroyImmediate(profile);
        AssetDatabase.DeleteAsset(Root);
        AssetDatabase.Refresh();
    }

    [Test]
    public void Validate_ReportsMissingPrerequisiteAndTestHeroineText()
    {
        HeroineScenarioFlowReport report = HeroineScenarioFlowValidator.Validate(profile);

        Assert.That(report.Flow.Any(x => x.Contains("[GameEvent] Start")), Is.True);
        Assert.That(report.Flow.Any(x => x.Contains("[Ending] Normal")), Is.True);
        Assert.That(report.Warnings.Any(x => x.Contains("MissingEvent")), Is.True);
        Assert.That(report.Warnings.Any(x => x.Contains("TestHeroine")), Is.True);
    }

    private static void EnsureFolder(string parent, string name)
    {
        string path = parent + "/" + name;
        if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, name);
    }
}
#endif
