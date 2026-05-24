using UnityEngine;

[CreateAssetMenu(menuName = "LoveSim/Scheduled Event Data")]
public class ScheduledEventData : ScriptableObject
{
    [Header("Basic")]
    public ScheduleType scheduleType = ScheduleType.None;
    public string actionId;

    [Header("Timing")]
    public TimeSlot triggerTimeSlot = TimeSlot.Noon;
    public bool allowOutfitChangeBeforeStart = true;

    [Header("Messages")]
    public ScheduledEventSpeakerType eventSpeakerType = ScheduledEventSpeakerType.Heroine;

    [TextArea(2, 4)]
    public string preparationMessage;

    [TextArea(3, 6)]
    public string eventMessage;

    [Header("Effect")]
    public int affectionChange = 0;

    public ScheduledEventDefinition ToDefinition()
    {
        return new ScheduledEventDefinition(
            scheduleType,
            actionId,
            triggerTimeSlot,
            allowOutfitChangeBeforeStart,
            eventSpeakerType,
            preparationMessage,
            eventMessage,
            affectionChange
        );
    }
}
