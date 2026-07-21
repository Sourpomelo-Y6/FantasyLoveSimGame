#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public class EndingDataValidatorTests
{
    private readonly List<EndingData> createdObjects = new List<EndingData>();

    [TearDown]
    public void TearDown()
    {
        foreach (EndingData ending in createdObjects)
        {
            if (ending != null)
            {
                Object.DestroyImmediate(ending);
            }
        }
        createdObjects.Clear();
    }

    [Test]
    public void ValidateForTests_AcceptsFallbackAndConditionalEndings()
    {
        EndingData fallback = Create("NormalEnding", 0);
        EndingData special = Create("SpecialEnding", 1200);
        special.costumeId = "Adventure";
        special.requiredShownEventIds = new[] { "Event_Forest_01" };

        EndingDataValidationReport report = EndingDataValidator.ValidateForTests(
            "TestHeroine",
            new[] { "Event_Forest_01" },
            new[] { "Adventure" },
            fallback,
            special);

        Assert.That(report.IsValid, Is.True, report.CreateSummary());
        Assert.That(report.EndingCount, Is.EqualTo(2));
    }

    [Test]
    public void ValidateForTests_DetectsInvalidFieldsAndReferences()
    {
        EndingData fallback = Create("NormalEnding", 0);
        EndingData invalid = Create("invalid-id", 10000);
        invalid.displayName = "";
        invalid.message = "";
        invalid.costumeId = "MissingOutfit";
        invalid.requiredShownEventIds = new[] { "MissingEvent", "MissingEvent", "" };

        EndingDataValidationReport report = EndingDataValidator.ValidateForTests(
            "TestHeroine",
            new string[0],
            new string[0],
            fallback,
            invalid);

        string[] warnings = report.Warnings.Select(entry => entry.Message).ToArray();
        Assert.That(warnings.Any(message => message.Contains("使用できない文字")), Is.True);
        Assert.That(warnings.Any(message => message.Contains("displayName")), Is.True);
        Assert.That(warnings.Any(message => message.Contains("message")), Is.True);
        Assert.That(warnings.Any(message => message.Contains("requiredAffection")), Is.True);
        Assert.That(warnings.Any(message => message.Contains("costumeId")), Is.True);
        Assert.That(warnings.Any(message => message.Contains("存在しないID")), Is.True);
        Assert.That(warnings.Any(message => message.Contains("重複ID")), Is.True);
        Assert.That(warnings.Any(message => message.Contains("空のID")), Is.True);
    }

    [Test]
    public void ValidateForTests_DetectsDuplicateAndAmbiguousSelection()
    {
        EndingData first = Create("EndingA", 1000);
        EndingData second = Create("EndingA", 1000);

        EndingDataValidationReport report = EndingDataValidator.ValidateForTests(
            "TestHeroine",
            new string[0],
            new string[0],
            first,
            second);

        string[] warnings = report.Warnings.Select(entry => entry.Message).ToArray();
        Assert.That(warnings.Any(message => message.Contains("endingId") && message.Contains("重複")), Is.True);
        Assert.That(warnings.Any(message => message.Contains("選択が不定")), Is.True);
    }

    [Test]
    public void ValidateForTests_DetectsMissingFallback()
    {
        EndingData special = Create("SpecialEnding", 1200);
        special.costumeId = "Adventure";

        EndingDataValidationReport report = EndingDataValidator.ValidateForTests(
            "TestHeroine",
            new string[0],
            new[] { "Adventure" },
            special);

        Assert.That(
            report.Warnings.Any(entry => entry.Message.Contains("フォールバック")),
            Is.True);
    }

    [Test]
    public void EndingData_GetDisplayPages_UsesLegacyFieldsWhenPagesAreEmpty()
    {
        EndingData ending = Create("LegacyEnding", 1000);
        ending.message = "legacy message";

        List<EndingPageData> pages = ending.GetDisplayPages();

        Assert.That(pages, Has.Count.EqualTo(1));
        Assert.That(pages[0].speakerType, Is.EqualTo(ScheduledEventSpeakerType.System));
        Assert.That(pages[0].message, Is.EqualTo("legacy message"));
    }

    [Test]
    public void EndingData_GetDisplayPages_UsesConfiguredPageOrder()
    {
        EndingData ending = Create("PagedEnding", 1000);
        ending.pages.Add(new EndingPageData { message = "first", expressionId = "Smile" });
        ending.pages.Add(new EndingPageData { message = "second", expressionId = "Shy" });

        List<EndingPageData> pages = ending.GetDisplayPages();

        Assert.That(pages.Select(page => page.message), Is.EqualTo(new[] { "first", "second" }));
        Assert.That(pages.Select(page => page.expressionId), Is.EqualTo(new[] { "Smile", "Shy" }));
    }

    [Test]
    public void ValidateForTests_DetectsEmptyEndingPage()
    {
        EndingData ending = Create("PagedEnding", 0);
        ending.message = "";
        ending.pages.Add(new EndingPageData { message = "" });

        EndingDataValidationReport report = EndingDataValidator.ValidateForTests(
            "TestHeroine",
            new string[0],
            new string[0],
            ending);

        Assert.That(
            report.Warnings.Any(entry => entry.Message.Contains("pages[0]") && entry.Message.Contains("message")),
            Is.True);
    }

    [Test]
    public void ValidateProjectAssets_CurrentEndingAssetsHaveNoWarnings()
    {
        EndingDataValidationReport report = EndingDataValidator.ValidateProjectAssets();

        Assert.That(
            report.Warnings.Select(entry => entry.Message),
            Is.Empty,
            report.CreateSummary());
        Assert.That(report.EndingCount, Is.GreaterThan(0));
    }

    private EndingData Create(string endingId, int requiredAffection)
    {
        EndingData ending = ScriptableObject.CreateInstance<EndingData>();
        ending.name = endingId;
        ending.endingId = endingId;
        ending.displayName = endingId;
        ending.message = "ending";
        ending.requiredAffection = requiredAffection;
        ending.requiredShownEventIds = new string[0];
        createdObjects.Add(ending);
        return ending;
    }
}
#endif
