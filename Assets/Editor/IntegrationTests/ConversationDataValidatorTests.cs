#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
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

    [Test]
    public void TestHeroineConversations_CoverGenresRelationshipBandsAndContexts()
    {
        const string root = "Assets/Resources/Heroines/TestHeroine/Conversations";
        ConversationData[] conversations = AssetDatabase.FindAssets("t:ConversationData", new[] { root })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<ConversationData>)
            .Where(conversation => conversation != null)
            .ToArray();

        foreach (ConversationGenre genre in System.Enum.GetValues(typeof(ConversationGenre)))
        {
            ConversationData[] genreConversations = conversations
                .Where(conversation => conversation.genre == genre)
                .ToArray();

            Assert.That(
                genreConversations.Any(conversation =>
                    !conversation.showOnce &&
                    conversation.minAffection == 0 &&
                    conversation.maxAffection == AffectionDataValidator.MaximumAffection),
                Is.True,
                genre + " に無条件フォールバックが必要です。");
            Assert.That(
                genreConversations.Any(conversation =>
                    conversation.minAffection <= 200 && conversation.maxAffection >= 599),
                Is.True,
                genre + " に親しみ段階の会話が必要です。");
            Assert.That(
                genreConversations.Any(conversation =>
                    conversation.minAffection <= 600 &&
                    conversation.maxAffection == AffectionDataValidator.MaximumAffection),
                Is.True,
                genre + " に信頼以降の会話が必要です。");
            Assert.That(
                genreConversations.Any(conversation =>
                    !conversation.anyTimeSlot ||
                    !conversation.anySeason ||
                    !conversation.anyWeather),
                Is.True,
                genre + " に時間帯・季節・天候のいずれかを使う会話が必要です。");
        }

        Assert.That(
            conversations.Any(conversation => !conversation.anyTimeSlot),
            Is.True,
            "時間帯条件を使う会話が必要です。");
        Assert.That(
            conversations.Any(conversation => !conversation.anySeason),
            Is.True,
            "季節条件を使う会話が必要です。");
        Assert.That(
            conversations.Any(conversation => !conversation.anyWeather),
            Is.True,
            "天候条件を使う会話が必要です。");
        Assert.That(
            conversations
                .Where(conversation =>
                    conversation.conversationId.StartsWith("Conv_") &&
                    (!conversation.anyTimeSlot ||
                    !conversation.anySeason ||
                    !conversation.anyWeather))
                .All(conversation => conversation.priority >= 200),
            Is.True,
            "条件付き会話は無条件会話より十分高いpriorityを設定します。");

        Assert.That(
            conversations
                .Where(conversation => conversation.conversationId.StartsWith("Conv_"))
                .All(conversation => conversation.name == conversation.conversationId),
            Is.True,
            "新命名規則の会話はファイル名とconversationIdを一致させます。");
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
