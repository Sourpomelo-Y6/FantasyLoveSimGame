using System;
using System.Collections.Generic;

public static class GameEventCompletionService
{
    public static int Complete(
        IEnumerable<GameEventData> gameEvents,
        Func<string, bool> isShown,
        Action<string> markShown)
    {
        if (gameEvents == null)
        {
            return 0;
        }

        int totalAffectionChange = 0;
        foreach (GameEventData gameEvent in gameEvents)
        {
            if (gameEvent == null)
            {
                continue;
            }

            bool hasEventId = !string.IsNullOrEmpty(gameEvent.eventId);
            if (gameEvent.showOnce &&
                hasEventId &&
                isShown != null &&
                isShown(gameEvent.eventId))
            {
                continue;
            }

            totalAffectionChange += gameEvent.affectionChange;

            if (gameEvent.showOnce && hasEventId && markShown != null)
            {
                markShown(gameEvent.eventId);
            }
        }

        return totalAffectionChange;
    }
}
