#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public class GameEventDataValidatorTests
{
    private readonly List<GameEventData> createdEvents = new List<GameEventData>();

    [TearDown]
    public void TearDown()
    {
        foreach (GameEventData gameEvent in createdEvents)
        {
            if (gameEvent != null)
            {
                Object.DestroyImmediate(gameEvent);
            }
        }

        createdEvents.Clear();
    }

    [Test]
    public void Validate_AcceptsConfiguredEventAndZeroCompletionEffect()
    {
        GameEventData gameEvent = CreateEvent();
        gameEvent.eventId = "Intro";
        gameEvent.showOnce = true;
        gameEvent.affectionChange = 0;
        gameEvent.pages.Add(new GameEventPageData { message = "導入本文" });

        GameEventValidationReport report =
            GameEventDataValidator.Validate(new[] { gameEvent }, null);

        Assert.That(report.IsValid, Is.True);
    }

    [Test]
    public void Validate_DetectsMissingIdBodyAndOutOfRangeCompletionEffect()
    {
        GameEventData gameEvent = CreateEvent();
        gameEvent.name = "BrokenEvent";
        gameEvent.showOnce = true;
        gameEvent.affectionChange = 10000;

        GameEventValidationReport report =
            GameEventDataValidator.Validate(new[] { gameEvent }, null);

        Assert.That(report.Warnings.Any(message => message.Contains("eventId が空")), Is.True);
        Assert.That(report.Warnings.Any(message => message.Contains("本文")), Is.True);
        Assert.That(report.Warnings.Any(message => message.Contains("affectionChange")), Is.True);
    }

    [Test]
    public void Validate_ContextTriggerRequiresTargetId()
    {
        GameEventData gameEvent = CreateEvent();
        gameEvent.eventId = "ForestEvent";
        gameEvent.triggerType = GameEventTriggerType.ScheduledEventCompleted;
        gameEvent.pages.Add(new GameEventPageData { message = "本文" });

        GameEventValidationReport report =
            GameEventDataValidator.Validate(new[] { gameEvent }, null);

        Assert.That(
            report.Warnings.Any(message => message.Contains("発火対象ID")),
            Is.True);
    }

    private GameEventData CreateEvent()
    {
        GameEventData gameEvent = ScriptableObject.CreateInstance<GameEventData>();
        createdEvents.Add(gameEvent);
        return gameEvent;
    }
}
#endif
