using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameEventPageData
{
    public ScheduledEventSpeakerType speakerType = ScheduledEventSpeakerType.Heroine;
    public string speakerName;

    [TextArea(2, 5)]
    public string message;

    public string stillId;
    public Sprite stillSprite;
}

[CreateAssetMenu(menuName = "LoveSim/Game Event Data")]
public class GameEventData : ScriptableObject
{
    [Header("Basic")]
    public string eventId;
    public GameEventTriggerType triggerType = GameEventTriggerType.Manual;
    public bool showOnce = true;
    public bool isEnabled = true;
    public int sortOrder = 0;

    [Header("Pages")]
    public List<GameEventPageData> pages = new List<GameEventPageData>();
}
