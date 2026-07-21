#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public class ActionReactionDataValidatorTests
{
    private readonly List<ActionData> createdObjects = new List<ActionData>();

    [TearDown]
    public void TearDown()
    {
        foreach (ActionData createdObject in createdObjects)
        {
            if (createdObject != null)
            {
                Object.DestroyImmediate(createdObject);
            }
        }
        createdObjects.Clear();
    }

    [Test]
    public void ValidateForTests_AcceptsFallbackAndKnownRequirements()
    {
        ActionData action = Create("Tea");
        action.reactions.Add(CreateReaction("Reaction_Tea_Special", 100, 200));
        action.reactions[0].requiredSkillIds.Add("Consideration");
        action.reactions[0].requiredShownEventIds.Add("Manual_Consideration_01");
        action.reactions.Add(CreateReaction("Reaction_Tea_Default", 0, 0));

        ActionReactionValidationReport report = ActionReactionDataValidator.ValidateForTests(
            "TestHeroine",
            new[] { "Consideration" },
            new[] { "Manual_Consideration_01" },
            new string[0],
            action);

        Assert.That(report.IsValid, Is.True, report.CreateSummary());
        Assert.That(report.ActionCount, Is.EqualTo(1));
        Assert.That(report.ReactionCount, Is.EqualTo(2));
    }

    [Test]
    public void ValidateForTests_DetectsDuplicateIdsAndConditionCollisions()
    {
        ActionData first = Create("Tea");
        first.reactions.Add(CreateReaction("Reaction_Shared", 10, 0));
        first.reactions.Add(CreateReaction("Reaction_Other", 10, 0));
        ActionData duplicateAction = Create("Tea");
        duplicateAction.reactions.Add(CreateReaction("Reaction_Shared", 0, 0));

        ActionReactionValidationReport report = ActionReactionDataValidator.ValidateForTests(
            "TestHeroine",
            new string[0],
            new string[0],
            new string[0],
            first,
            duplicateAction);

        string[] warnings = report.Warnings.Select(entry => entry.Message).ToArray();
        Assert.That(warnings.Any(message => message.Contains("actionId") && message.Contains("重複")), Is.True);
        Assert.That(warnings.Any(message => message.Contains("reactionId") && message.Contains("重複")), Is.True);
        Assert.That(warnings.Any(message => message.Contains("条件・priorityが同一")), Is.True);
    }

    [Test]
    public void ValidateForTests_DetectsInvalidRequirementsAndShownIdCollision()
    {
        ActionData action = Create("Tea");
        ActionReactionData reaction = CreateReaction("Daily_001", 0, 0);
        reaction.showOnce = true;
        reaction.minAffection = 700;
        reaction.maxAffection = 600;
        reaction.requiredSkillIds.Add("MissingSkill");
        reaction.requiredShownEventIds.Add("MissingEvent");
        action.reactions.Add(reaction);

        ActionReactionValidationReport report = ActionReactionDataValidator.ValidateForTests(
            "TestHeroine",
            new string[0],
            new string[0],
            new[] { "Daily_001" },
            action);

        string[] warnings = report.Warnings.Select(entry => entry.Message).ToArray();
        Assert.That(warnings.Any(message => message.Contains("好感度範囲")), Is.True);
        Assert.That(warnings.Count(message => message.Contains("存在しないID")), Is.EqualTo(2));
        Assert.That(warnings.Any(message => message.Contains("通常会話IDと重複")), Is.True);
    }

    [Test]
    public void ValidateForTests_WarnsWhenDedicatedActionHasUnusedReactions()
    {
        ActionData action = Create("Talk");
        action.executionType = ActionExecutionType.OpenConversationGenres;
        action.reactions.Add(CreateReaction("Reaction_Talk_01", 0, 0));

        ActionReactionValidationReport report = ActionReactionDataValidator.ValidateForTests(
            "TestHeroine",
            new string[0],
            new string[0],
            new string[0],
            action);

        Assert.That(
            report.Warnings.Any(entry => entry.Message.Contains("SimpleActionではない")),
            Is.True);
    }

    [Test]
    public void ValidateProjectAssets_CurrentActionReactionsHaveNoWarnings()
    {
        ActionReactionValidationReport report = ActionReactionDataValidator.ValidateProjectAssets();

        Assert.That(
            report.Warnings.Select(entry => entry.Message),
            Is.Empty,
            report.CreateSummary());
        Assert.That(report.ActionCount, Is.GreaterThan(0));
        Assert.That(report.ReactionCount, Is.GreaterThan(0));
    }

    private ActionData Create(string actionId)
    {
        ActionData action = ScriptableObject.CreateInstance<ActionData>();
        action.name = actionId;
        action.actionId = actionId;
        action.executionType = ActionExecutionType.SimpleAction;
        action.resultMessage = "default";
        createdObjects.Add(action);
        return action;
    }

    private static ActionReactionData CreateReaction(string reactionId, int priority, int minAffection)
    {
        return new ActionReactionData
        {
            reactionId = reactionId,
            resultMessage = "reaction",
            priority = priority,
            minAffection = minAffection,
            maxAffection = AffectionDataValidator.MaximumAffection,
            anyTimeSlot = true,
            anyWeather = true,
            anySeason = true
        };
    }
}
#endif
