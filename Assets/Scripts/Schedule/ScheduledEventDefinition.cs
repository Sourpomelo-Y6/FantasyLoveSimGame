using UnityEngine;

public class ScheduledEventDefinition
{
    public ScheduleType ScheduleType { get; private set; }
    public string ActionId { get; private set; }
    public TimeSlot TriggerTimeSlot { get; private set; }
    public bool AllowOutfitChangeBeforeStart { get; private set; }
    public ScheduledEventOutfitPromptMode OutfitPromptMode { get; private set; }
    public ScheduledEventSpeakerType EventSpeakerType { get; private set; }
    public string PreparationMessage { get; private set; }
    public string EventMessage { get; private set; }
    public int AffectionChange { get; private set; }
    public string CostumeId { get; private set; }
    public string StillId { get; private set; }
    public Sprite StillSprite { get; private set; }

    public ScheduledEventDefinition(
        ScheduleType scheduleType,
        string actionId,
        TimeSlot triggerTimeSlot,
        bool allowOutfitChangeBeforeStart,
        ScheduledEventOutfitPromptMode outfitPromptMode,
        ScheduledEventSpeakerType eventSpeakerType,
        string preparationMessage,
        string eventMessage,
        int affectionChange,
        string costumeId = "",
        string stillId = "",
        Sprite stillSprite = null)
    {
        ScheduleType = scheduleType;
        ActionId = actionId;
        TriggerTimeSlot = triggerTimeSlot;
        AllowOutfitChangeBeforeStart = allowOutfitChangeBeforeStart;
        OutfitPromptMode = outfitPromptMode;
        EventSpeakerType = eventSpeakerType;
        PreparationMessage = preparationMessage;
        EventMessage = eventMessage;
        AffectionChange = affectionChange;
        CostumeId = costumeId;
        StillId = stillId;
        StillSprite = stillSprite;
    }
}
