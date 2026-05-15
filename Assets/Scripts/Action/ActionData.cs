using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "LoveSim/Action Data")]
public class ActionData : ScriptableObject
{
    [Header("Basic")]
    public string actionId;
    public string displayName;

    public ActionExecutionType executionType = ActionExecutionType.SimpleAction;

    [Header("Default Result")]
    [TextArea(3, 6)]
    public string resultMessage;

    [TextArea(2, 4)]
    public string unavailableMessage = "今はこの行動はできません。";

    public bool useHeroineNameAsSpeaker = false;

    [Header("Default Effect")]
    public int affectionChange = 0;
    public bool advanceTime = true;

    [Header("Conditional Reactions")]
    public List<ActionReactionData> reactions = new List<ActionReactionData>();

    [Header("Display")]
    public int sortOrder = 0;
    public bool isEnabled = true;

    [Header("Time Slot Condition")]
    public bool anyTimeSlot = true;
    public List<TimeSlot> allowedTimeSlots = new List<TimeSlot>();

    [Header("Weather Condition")]
    public bool anyWeather = true;
    public List<Weather> allowedWeathers = new List<Weather>();

    [Header("Season Condition")]
    public bool anySeason = true;
    public List<Season> allowedSeasons = new List<Season>();

    [Header("Affection Condition")]
    public int minAffection = 0;
    public int maxAffection = 100;
}