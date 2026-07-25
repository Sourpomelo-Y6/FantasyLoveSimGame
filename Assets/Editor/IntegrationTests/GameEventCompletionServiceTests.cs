#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class GameEventCompletionServiceTests
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
    public void Complete_AppliesEffectsAndMarksShowOnceEvents()
    {
        HashSet<string> shownIds = new HashSet<string>();
        GameEventData first = CreateEvent("first", true, 10);
        GameEventData repeatable = CreateEvent("repeatable", false, -3);

        int affectionChange = GameEventCompletionService.Complete(
            new[] { first, repeatable },
            shownIds.Contains,
            eventId => shownIds.Add(eventId));

        Assert.That(affectionChange, Is.EqualTo(7));
        Assert.That(shownIds, Is.EquivalentTo(new[] { "first" }));
    }

    [Test]
    public void Complete_DoesNotApplyShowOnceEventTwice()
    {
        HashSet<string> shownIds = new HashSet<string>();
        GameEventData gameEvent = CreateEvent("once", true, 10);

        int firstChange = GameEventCompletionService.Complete(
            new[] { gameEvent },
            shownIds.Contains,
            eventId => shownIds.Add(eventId));
        int secondChange = GameEventCompletionService.Complete(
            new[] { gameEvent },
            shownIds.Contains,
            eventId => shownIds.Add(eventId));

        Assert.That(firstChange, Is.EqualTo(10));
        Assert.That(secondChange, Is.Zero);
        Assert.That(shownIds.Count, Is.EqualTo(1));
    }

    private GameEventData CreateEvent(string eventId, bool showOnce, int affectionChange)
    {
        GameEventData gameEvent = ScriptableObject.CreateInstance<GameEventData>();
        gameEvent.eventId = eventId;
        gameEvent.showOnce = showOnce;
        gameEvent.affectionChange = affectionChange;
        createdEvents.Add(gameEvent);
        return gameEvent;
    }
}
#endif
