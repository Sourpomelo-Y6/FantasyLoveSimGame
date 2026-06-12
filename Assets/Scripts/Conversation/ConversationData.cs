using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ConversationDataItem
{
    [Header("Basic")]
    public string conversationId;

    public ConversationGenre genre;

    public ConversationType type;

    [TextArea(3, 6)]
    public string heroineLine;

    public List<ConversationChoice> choices = new List<ConversationChoice>();

    [Header("Display Rule")]
    public int priority = 0;
    public bool showOnce = false;

    [Header("Affection Condition")]
    public int minAffection = 0;
    public int maxAffection = 100;

    [Header("Time Slot Condition")]
    public bool anyTimeSlot = true;
    public List<TimeSlot> allowedTimeSlots = new List<TimeSlot>();

    [Header("Season Condition")]
    public bool anySeason = true;
    public List<Season> allowedSeasons = new List<Season>();

    [Header("Weather Condition")]
    public bool anyWeather = true;
    public List<Weather> allowedWeathers = new List<Weather>();
}

[CreateAssetMenu(menuName = "LoveSim/Conversation Data")]
public class ConversationData : ScriptableObject
{
    [Header("Container")]
    public string heroineId;
    public List<ConversationDataItem> items = new List<ConversationDataItem>();

    [Header("Basic")]
    public string conversationId;

    public ConversationGenre genre;

    public ConversationType type;

    [TextArea(3, 6)]
    public string heroineLine;

    public List<ConversationChoice> choices = new List<ConversationChoice>();

    [Header("Display Rule")]
    public int priority = 0;
    public bool showOnce = false;

    [Header("Affection Condition")]
    public int minAffection = 0;
    public int maxAffection = 100;

    [Header("Time Slot Condition")]
    public bool anyTimeSlot = true;
    public List<TimeSlot> allowedTimeSlots = new List<TimeSlot>();

    [Header("Season Condition")]
    public bool anySeason = true;
    public List<Season> allowedSeasons = new List<Season>();

    [Header("Weather Condition")]
    public bool anyWeather = true;
    public List<Weather> allowedWeathers = new List<Weather>();
}
