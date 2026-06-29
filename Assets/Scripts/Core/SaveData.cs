using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    public int saveVersion = 4;
    public int saveSlotIndex;
    public string savedAt;
    public string heroineId;
    public string heroineDisplayName;
    public string thumbnailFileName;

    public int day;
    public TimeSlot currentTimeSlot;
    public Weekday currentWeekday;
    public Season currentSeason;
    public Weather currentWeather;

    public int affection;
    public BattleStatusData playerBattleStatus = new BattleStatusData();
    public int playerMoney = 1000;
    public BattleStatusData heroineBattleStatus = new BattleStatusData
    {
        currentHp = 80,
        maxHp = 80,
        attack = 8,
        defense = 4,
        speed = 6
    };

    public string currentOutfitId;

    public List<OutfitPreference> outfitPreferences = new List<OutfitPreference>();
    public OutfitPromptAbilitySet playerOutfitPromptAbilities = new OutfitPromptAbilitySet();
    public OutfitPromptAbilitySet heroineOutfitPromptAbilities = new OutfitPromptAbilitySet();
    public List<string> unlockedStatusAbilityIds = new List<string>();
    public List<string> unlockedStillIds = new List<string>();

    public List<string> shownConversationIds = new List<string>();
    public List<string> shownGameEventIds = new List<string>();

    public ScheduleType todaySchedule;
    public ScheduleType tomorrowSchedule;
    public bool todayScheduleEventExecuted;
}
