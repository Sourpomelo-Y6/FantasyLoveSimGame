using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameEventPageData
{
    public ScheduledEventSpeakerType speakerType = ScheduledEventSpeakerType.Heroine;
    public string speakerName;

    [TextArea(2, 5)]
    public string message;

    public string expressionId;

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

    [Header("Conditions")]
    public int minDay = 0;
    public int maxDay = 0;
    public int minAffection = 0;
    public int maxAffection = 0;
    public List<string> requiredShownEventIds = new List<string>();
    public List<string> blockedShownEventIds = new List<string>();
    public List<string> requiredOutfitIds = new List<string>();
    public List<string> blockedOutfitIds = new List<string>();
    public List<OutfitData> requiredOutfits = new List<OutfitData>();
    public List<OutfitData> blockedOutfits = new List<OutfitData>();
    [Tooltip("イベント開始に必要な取得済み主人公スキル ID。すべて取得している場合だけ開始できます。")]
    public List<string> requiredSkillIds = new List<string>();

    [Header("Weather Condition")]
    public bool anyWeather = true;
    public List<Weather> allowedWeathers = new List<Weather>();

    [Header("Pages")]
    public List<GameEventPageData> pages = new List<GameEventPageData>();
}
