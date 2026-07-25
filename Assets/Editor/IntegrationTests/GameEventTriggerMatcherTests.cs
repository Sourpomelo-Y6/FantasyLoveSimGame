#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using UnityEngine;

public class GameEventTriggerMatcherTests
{
    [Test]
    public void Matches_UsesTriggerTypeAndCaseInsensitiveContext()
    {
        GameEventData gameEvent = ScriptableObject.CreateInstance<GameEventData>();
        try
        {
            gameEvent.triggerType = GameEventTriggerType.ScheduledEventCompleted;
            gameEvent.triggerContextId = "Forest";

            Assert.That(
                GameEventTriggerMatcher.Matches(
                    gameEvent,
                    GameEventTriggerType.ScheduledEventCompleted,
                    "forest"),
                Is.True);
            Assert.That(
                GameEventTriggerMatcher.Matches(
                    gameEvent,
                    GameEventTriggerType.ActionCompleted,
                    "Forest"),
                Is.False);
            Assert.That(
                GameEventTriggerMatcher.Matches(
                    gameEvent,
                    GameEventTriggerType.ScheduledEventCompleted,
                    "Cave"),
                Is.False);
        }
        finally
        {
            Object.DestroyImmediate(gameEvent);
        }
    }

    [Test]
    public void RequiresContext_DistinguishesAutomaticAndLegacyTriggers()
    {
        Assert.That(
            GameEventTriggerMatcher.RequiresContext(
                GameEventTriggerType.ScheduledEventCompleted),
            Is.True);
        Assert.That(
            GameEventTriggerMatcher.RequiresContext(GameEventTriggerType.Manual),
            Is.False);
        Assert.That(
            GameEventTriggerMatcher.RequiresContext(GameEventTriggerType.DayStart),
            Is.False);
    }
}
#endif
