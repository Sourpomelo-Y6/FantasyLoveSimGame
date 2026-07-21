#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public class ConversationDataValidatorTests
{
    private readonly List<ConversationData> createdObjects = new List<ConversationData>();

    [TearDown]
    public void TearDown()
    {
        foreach (ConversationData createdObject in createdObjects)
        {
            if (createdObject != null)
            {
                Object.DestroyImmediate(createdObject);
            }
        }
        createdObjects.Clear();
    }

    [Test]
    public void ValidateForTests_AcceptsUniqueConversationWithFallback()
    {
        ConversationData conversation = Create("Daily_001");

        ConversationDataValidationReport report =
            ConversationDataValidator.ValidateForTests("TestHeroine", conversation);

        Assert.That(report.IsValid, Is.True);
        Assert.That(report.ConversationCount, Is.EqualTo(1));
    }

    [Test]
    public void ValidateForTests_DetectsDuplicateIdsAndConditionCollisions()
    {
        ConversationData first = Create("Daily_001");
        ConversationData duplicateId = Create("Daily_001");
        duplicateId.name = "DailyDuplicate";
        ConversationData sameCondition = Create("Daily_002");

        ConversationDataValidationReport report = ConversationDataValidator.ValidateForTests(
            "TestHeroine",
            first,
            duplicateId,
            sameCondition);

        string[] warnings = report.Warnings.Select(entry => entry.Message).ToArray();
        Assert.That(warnings.Any(message => message.Contains("conversationId") && message.Contains("重複")), Is.True);
        Assert.That(warnings.Any(message => message.Contains("条件・優先度が同一")), Is.True);
    }

    [Test]
    public void ValidateForTests_DetectsMissingFallbackAndInvalidClassification()
    {
        ConversationData conversation = Create("invalid-id");
        conversation.showOnce = true;
        conversation.type = ConversationType.Choice;
        conversation.choices.Clear();

        ConversationDataValidationReport report =
            ConversationDataValidator.ValidateForTests("TestHeroine", conversation);

        string[] warnings = report.Warnings.Select(entry => entry.Message).ToArray();
        Assert.That(warnings.Any(message => message.Contains("使用できない文字")), Is.True);
        Assert.That(warnings.Any(message => message.Contains("Choiceですが選択肢がありません")), Is.True);
        Assert.That(warnings.Any(message => message.Contains("条件不成立時に使える会話がありません")), Is.True);
    }

    [Test]
    public void ValidateForTests_DoesNotTreatDifferentConversationTypesAsCollision()
    {
        ConversationData simple = Create("Daily_001");
        ConversationData choice = Create("Daily_002");
        choice.type = ConversationType.Choice;
        choice.choices.Add(new ConversationChoice { choiceText = "選択", responseText = "返答" });

        ConversationDataValidationReport report =
            ConversationDataValidator.ValidateForTests("TestHeroine", simple, choice);

        Assert.That(
            report.Warnings.Any(entry => entry.Message.Contains("条件・優先度が同一")),
            Is.False);
    }

    [Test]
    public void ValidateProjectAssets_CurrentConversationAssetsHaveNoWarnings()
    {
        ConversationDataValidationReport report = ConversationDataValidator.ValidateProjectAssets();

        Assert.That(
            report.Warnings.Select(entry => entry.Message),
            Is.Empty,
            report.CreateSummary());
        Assert.That(report.ConversationCount, Is.GreaterThan(0));
    }

    private ConversationData Create(string conversationId)
    {
        ConversationData conversation = ScriptableObject.CreateInstance<ConversationData>();
        conversation.name = conversationId;
        conversation.heroineId = "TestHeroine";
        conversation.conversationId = conversationId;
        conversation.genre = ConversationGenre.Daily;
        conversation.type = ConversationType.Simple;
        conversation.showOnce = false;
        conversation.minAffection = 0;
        conversation.maxAffection = AffectionDataValidator.MaximumAffection;
        conversation.anyTimeSlot = true;
        conversation.anySeason = true;
        conversation.anyWeather = true;
        createdObjects.Add(conversation);
        return conversation;
    }
}
#endif
