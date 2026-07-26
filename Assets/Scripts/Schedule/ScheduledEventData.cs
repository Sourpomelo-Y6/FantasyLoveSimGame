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
    public ScheduledEventOutfitPromptMode outfitPromptMode = ScheduledEventOutfitPromptMode.Conditional;

    [Header("Costume Condition")]
    public string costumeId;

    [Header("Messages")]
    public ScheduledEventSpeakerType eventSpeakerType = ScheduledEventSpeakerType.Heroine;

    [TextArea(2, 4)]
    public string preparationMessage;
    [Tooltip("準備メッセージ用のボイスID。空なら再生しません。")]
    public string preparationVoiceId;

    [TextArea(3, 6)]
    public string eventMessage;
    [Tooltip("実行結果メッセージ用のボイスID。空なら再生しません。")]
    public string eventVoiceId;

    [Header("Still")]
    public string stillId;
    public Sprite stillSprite;

    [Header("Effect")]
    public int affectionChange = 0;

    public ScheduledEventDefinition ToDefinition()
    {
        return new ScheduledEventDefinition(
            scheduleType,
            actionId,
            triggerTimeSlot,
            allowOutfitChangeBeforeStart,
            outfitPromptMode,
            eventSpeakerType,
            preparationMessage,
            eventMessage,
            affectionChange,
            costumeId,
            stillId,
            stillSprite,
            preparationVoiceId,
            eventVoiceId
        );
    }
}
