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

    [Header("Affection Condition")]
    public int minAffection = 0;
    public int maxAffection = 9999;

    [Header("Costume Condition")]
    public string costumeId;

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
