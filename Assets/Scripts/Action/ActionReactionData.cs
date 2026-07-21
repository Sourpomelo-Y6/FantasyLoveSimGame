using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ActionReactionData
{
    [Header("Basic")]
    public string reactionId;

    [TextArea(3, 6)]
    public string resultMessage;

    public bool useHeroineNameAsSpeaker = false;

    [Tooltip("ヒロイン発話時に適用する表情ID。空なら現在の表情を維持します。")]
    public string expressionId;

    [Header("Still")]
    public string stillId;
    public Sprite stillSprite;

    [Header("Effect")]
    public int affectionChange = 0;
    public int playerHpChange = 0;
    public int heroineHpChange = 0;
    public bool advanceTime = true;

    [Header("Display Rule")]
    public int priority = 0;
    [Tooltip("有効時は、この reactionId の反応をセーブデータごとに一度だけ表示します。")]
    public bool showOnce = false;

    [Header("Affection Condition")]
    public int minAffection = 0;
    public int maxAffection = 9999;

    [Header("Costume Condition")]
    public string costumeId;

    [Header("Progress Condition")]
    [Tooltip("すべて表示済みである必要がある GameEventData.eventId。")]
    public List<string> requiredShownEventIds = new List<string>();
    [Tooltip("すべて取得済みである必要があるスキルID。")]
    public List<string> requiredSkillIds = new List<string>();

    [Header("Time Slot Condition")]
    public bool anyTimeSlot = true;
    public List<TimeSlot> allowedTimeSlots = new List<TimeSlot>();

    [Header("Weather Condition")]
    public bool anyWeather = true;
    public List<Weather> allowedWeathers = new List<Weather>();

    [Header("Season Condition")]
    public bool anySeason = true;
    public List<Season> allowedSeasons = new List<Season>();
}
