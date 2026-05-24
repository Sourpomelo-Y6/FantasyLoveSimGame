using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    public int saveVersion = 1;
    public int saveSlotIndex;
    public string savedAt;

    public int day;
    public TimeSlot currentTimeSlot;
    public Weekday currentWeekday;
    public Season currentSeason;
    public Weather currentWeather;

    public int affection;

    public string currentOutfitId;

    public List<OutfitPreference> outfitPreferences = new List<OutfitPreference>();
    public OutfitPromptAbilitySet playerOutfitPromptAbilities = new OutfitPromptAbilitySet();
    public OutfitPromptAbilitySet heroineOutfitPromptAbilities = new OutfitPromptAbilitySet();
    public List<string> unlockedStatusAbilityIds = new List<string>();

    public List<string> shownConversationIds = new List<string>();
    public List<string> shownGameEventIds = new List<string>();

    public ScheduleType todaySchedule;
    public ScheduleType tomorrowSchedule;
    public bool todayScheduleEventExecuted;
}
