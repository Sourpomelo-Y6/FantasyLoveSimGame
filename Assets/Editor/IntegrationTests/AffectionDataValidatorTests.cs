#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public class AffectionDataValidatorTests
{
    private readonly List<ScriptableObject> createdObjects = new List<ScriptableObject>();

    [TearDown]
    public void TearDown()
    {
        foreach (ScriptableObject createdObject in createdObjects)
        {
            if (createdObject != null)
            {
                Object.DestroyImmediate(createdObject);
            }
        }
        createdObjects.Clear();
    }

    [Test]
    public void Validate_AcceptsCurrentScaleAndUnlimitedGameEventMaximum()
    {
        ConversationData conversation = Create<ConversationData>();
        conversation.minAffection = 300;
        conversation.maxAffection = 9999;
        conversation.choices.Add(new ConversationChoice { affectionChange = 10 });

        ActionData action = Create<ActionData>();
        action.minAffection = 0;
        action.maxAffection = 9999;
        action.affectionChange = 20;

        GameEventData gameEvent = Create<GameEventData>();
        gameEvent.minAffection = 100;
        gameEvent.maxAffection = 0;

        ScheduledEventData scheduledEvent = Create<ScheduledEventData>();
        scheduledEvent.affectionChange = 10;

        EndingData ending = Create<EndingData>();
        ending.requiredAffection = 1000;

        AffectionDataValidationReport report = AffectionDataValidator.Validate(
            new[] { conversation },
            new[] { action },
            new[] { gameEvent },
            new[] { scheduledEvent },
            new[] { ending });

        Assert.That(report.IsValid, Is.True);
        Assert.That(report.WarningCount, Is.Zero);
    }

    [Test]
    public void Validate_DetectsLegacyScaleAcrossSupportedAssetTypes()
    {
        ConversationData conversation = Create<ConversationData>();
        conversation.maxAffection = 100;
        conversation.choices.Add(new ConversationChoice { affectionChange = 1 });

        ActionData action = Create<ActionData>();
        action.maxAffection = 100;

        GameEventData gameEvent = Create<GameEventData>();
        gameEvent.maxAffection = 100;

        ScheduledEventData scheduledEvent = Create<ScheduledEventData>();
        scheduledEvent.affectionChange = 2;

        EndingData ending = Create<EndingData>();
        ending.requiredAffection = 80;

        AffectionDataValidationReport report = AffectionDataValidator.Validate(
            new[] { conversation },
            new[] { action },
            new[] { gameEvent },
            new[] { scheduledEvent },
            new[] { ending });

        string[] warnings = report.Warnings.Select(entry => entry.Message).ToArray();
        Assert.That(warnings.Any(message => message.Contains("maxAffection=100")), Is.True);
        Assert.That(warnings.Any(message => message.Contains("affectionChange=1")), Is.True);
        Assert.That(warnings.Any(message => message.Contains("affectionChange=2")), Is.True);
        Assert.That(warnings.Any(message => message.Contains("requiredAffection")), Is.True);
    }

    [Test]
    public void Validate_DetectsInvertedAndOutOfRangeConditions()
    {
        ConversationData conversation = Create<ConversationData>();
        conversation.minAffection = 500;
        conversation.maxAffection = 300;

        ActionData action = Create<ActionData>();
        action.minAffection = -1;
        action.maxAffection = AffectionDataValidator.MaximumAffection + 1;

        AffectionDataValidationReport report = AffectionDataValidator.Validate(
            new[] { conversation },
            new[] { action },
            null,
            null,
            null);

        string[] warnings = report.Warnings.Select(entry => entry.Message).ToArray();
        Assert.That(warnings.Any(message => message.Contains("好感度範囲が逆転")), Is.True);
        Assert.That(warnings.Count(message => message.Contains("範囲外")), Is.EqualTo(2));
    }

    private T Create<T>() where T : ScriptableObject
    {
        T createdObject = ScriptableObject.CreateInstance<T>();
        createdObject.name = typeof(T).Name + "Test";
        createdObjects.Add(createdObject);
        return createdObject;
    }
}
#endif
