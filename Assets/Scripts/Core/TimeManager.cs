using UnityEngine;

public class TimeManager : MonoBehaviour
{
    [Header("Date")]
    [SerializeField] private int day = 1;
    [SerializeField] private TimeSlot currentTimeSlot = TimeSlot.Morning;
    [SerializeField] private Weekday currentWeekday = Weekday.Monday;

    [Header("Season / Weather")]
    [SerializeField] private Season currentSeason = Season.Spring;
    [SerializeField] private Weather currentWeather = Weather.Sunny;

    [Header("Season Settings")]
    [SerializeField] private int daysPerSeason = 30;

    public int Day => day;
    public TimeSlot CurrentTimeSlot => currentTimeSlot;
    public Weekday CurrentWeekday => currentWeekday;
    public Season CurrentSeason => currentSeason;
    public Weather CurrentWeather => currentWeather;

    public void AdvanceTime()
    {
        if (currentTimeSlot == TimeSlot.Morning)
        {
            currentTimeSlot = TimeSlot.Noon;
            return;
        }

        if (currentTimeSlot == TimeSlot.Noon)
        {
            currentTimeSlot = TimeSlot.Night;
            return;
        }

        if (currentTimeSlot == TimeSlot.Night)
        {
            AdvanceDay();
            return;
        }

        if (currentTimeSlot == TimeSlot.LateNight)
        {
            AdvanceDay();
            return;
        }
    }

    private void AdvanceDay()
    {
        day++;
        currentTimeSlot = TimeSlot.Morning;

        AdvanceWeekday();
        UpdateSeason();
        RandomizeWeather();
    }

    private void AdvanceWeekday()
    {
        int next = (int)currentWeekday + 1;

        if (next > (int)Weekday.Saturday)
        {
            next = (int)Weekday.Sunday;
        }

        currentWeekday = (Weekday)next;
    }

    private void UpdateSeason()
    {
        int seasonIndex = ((day - 1) / daysPerSeason) % 4;
        currentSeason = (Season)seasonIndex;
    }

    private void RandomizeWeather()
    {
        // 季節ごとに少しだけ天気の出方を変える
        int randomValue = Random.Range(0, 100);

        if (currentSeason == Season.Spring)
        {
            if (randomValue < 55)
            {
                currentWeather = Weather.Sunny;
            }
            else if (randomValue < 80)
            {
                currentWeather = Weather.Cloudy;
            }
            else if (randomValue < 95)
            {
                currentWeather = Weather.Rainy;
            }
            else
            {
                currentWeather = Weather.Storm;
            }

            return;
        }

        if (currentSeason == Season.Summer)
        {
            if (randomValue < 60)
            {
                currentWeather = Weather.Sunny;
            }
            else if (randomValue < 75)
            {
                currentWeather = Weather.Cloudy;
            }
            else if (randomValue < 90)
            {
                currentWeather = Weather.Rainy;
            }
            else
            {
                currentWeather = Weather.Storm;
            }

            return;
        }

        if (currentSeason == Season.Autumn)
        {
            if (randomValue < 45)
            {
                currentWeather = Weather.Sunny;
            }
            else if (randomValue < 75)
            {
                currentWeather = Weather.Cloudy;
            }
            else if (randomValue < 95)
            {
                currentWeather = Weather.Rainy;
            }
            else
            {
                currentWeather = Weather.Storm;
            }

            return;
        }

        if (currentSeason == Season.Winter)
        {
            if (randomValue < 35)
            {
                currentWeather = Weather.Sunny;
            }
            else if (randomValue < 60)
            {
                currentWeather = Weather.Cloudy;
            }
            else if (randomValue < 75)
            {
                currentWeather = Weather.Rainy;
            }
            else if (randomValue < 95)
            {
                currentWeather = Weather.Snow;
            }
            else
            {
                currentWeather = Weather.Storm;
            }

            return;
        }
    }

    public string GetTimeSlotDisplayName()
    {
        switch (currentTimeSlot)
        {
            case TimeSlot.Morning:
                return "朝";

            case TimeSlot.Noon:
                return "昼";

            case TimeSlot.Night:
                return "夜";

            case TimeSlot.LateNight:
                return "深夜";

            default:
                return "";
        }
    }

    public string GetWeekdayDisplayName()
    {
        switch (currentWeekday)
        {
            case Weekday.Sunday:
                return "日曜日";

            case Weekday.Monday:
                return "月曜日";

            case Weekday.Tuesday:
                return "火曜日";

            case Weekday.Wednesday:
                return "水曜日";

            case Weekday.Thursday:
                return "木曜日";

            case Weekday.Friday:
                return "金曜日";

            case Weekday.Saturday:
                return "土曜日";

            default:
                return "";
        }
    }

    public string GetSeasonDisplayName()
    {
        switch (currentSeason)
        {
            case Season.Spring:
                return "春";

            case Season.Summer:
                return "夏";

            case Season.Autumn:
                return "秋";

            case Season.Winter:
                return "冬";

            default:
                return "";
        }
    }

    public string GetWeatherDisplayName()
    {
        switch (currentWeather)
        {
            case Weather.Sunny:
                return "晴れ";

            case Weather.Cloudy:
                return "曇り";

            case Weather.Rainy:
                return "雨";

            case Weather.Storm:
                return "嵐";

            case Weather.Snow:
                return "雪";

            default:
                return "";
        }
    }

    public void SetTimeState(
        int newDay,
        TimeSlot newTimeSlot,
        Weekday newWeekday,
        Season newSeason,
        Weather newWeather)
    {
        day = newDay;
        currentTimeSlot = newTimeSlot;
        currentWeekday = newWeekday;
        currentSeason = newSeason;
        currentWeather = newWeather;
    }
}