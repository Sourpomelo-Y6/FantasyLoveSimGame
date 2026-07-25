using System;

public static class GameEventTriggerMatcher
{
    public static bool Matches(
        GameEventData gameEvent,
        GameEventTriggerType triggerType,
        string triggerContextId)
    {
        return gameEvent != null &&
            gameEvent.triggerType == triggerType &&
            string.Equals(
                gameEvent.triggerContextId,
                triggerContextId,
                StringComparison.OrdinalIgnoreCase);
    }

    public static bool RequiresContext(GameEventTriggerType triggerType)
    {
        return triggerType == GameEventTriggerType.ScheduledEventCompleted ||
            triggerType == GameEventTriggerType.ActionCompleted ||
            triggerType == GameEventTriggerType.LocationEntered ||
            triggerType == GameEventTriggerType.QuestCompleted;
    }
}
