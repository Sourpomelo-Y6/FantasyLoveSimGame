public class ScheduledEventDefinition
{
    public ScheduleType ScheduleType { get; private set; }
    public string ActionId { get; private set; }
    public TimeSlot TriggerTimeSlot { get; private set; }
    public bool AllowOutfitChangeBeforeStart { get; private set; }
    public string PreparationMessage { get; private set; }
    public string EventMessage { get; private set; }
    public int AffectionChange { get; private set; }

    public ScheduledEventDefinition(
        ScheduleType scheduleType,
        string actionId,
        TimeSlot triggerTimeSlot,
        bool allowOutfitChangeBeforeStart,
        string preparationMessage,
        string eventMessage,
        int affectionChange)
    {
        ScheduleType = scheduleType;
        ActionId = actionId;
        TriggerTimeSlot = triggerTimeSlot;
        AllowOutfitChangeBeforeStart = allowOutfitChangeBeforeStart;
        PreparationMessage = preparationMessage;
        EventMessage = eventMessage;
        AffectionChange = affectionChange;
    }
}
