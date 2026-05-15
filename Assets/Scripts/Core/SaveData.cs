using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    public int day;
    public TimeSlot currentTimeSlot;
    public Weekday currentWeekday;
    public Season currentSeason;
    public Weather currentWeather;

    public int affection;

    public string currentOutfitId;

    public List<string> shownConversationIds = new List<string>();
}
