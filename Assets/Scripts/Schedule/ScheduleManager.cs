using UnityEngine;

public class ScheduleManager : MonoBehaviour
{
    [Header("Schedule State")]
    [SerializeField] private ScheduleType todaySchedule = ScheduleType.None;
    [SerializeField] private ScheduleType tomorrowSchedule = ScheduleType.None;

    [Header("References")]
    [SerializeField] private TimeManager timeManager;

    public ScheduleType TodaySchedule
    {
        get { return todaySchedule; }
    }

    public ScheduleType TomorrowSchedule
    {
        get { return tomorrowSchedule; }
    }

    private void OnEnable()
    {
        if (timeManager != null)
        {
            timeManager.OnDayChanged += HandleDayChanged;
        }
    }

    private void OnDisable()
    {
        if (timeManager != null)
        {
            timeManager.OnDayChanged -= HandleDayChanged;
        }
    }

    private void Update()
    {
        //デバック
        //if (Input.GetKeyDown(KeyCode.Alpha1))
        //{
        //    SetTomorrowSchedule(ScheduleType.DuoLake);
        //}

        //if (Input.GetKeyDown(KeyCode.Alpha2))
        //{
        //    AdvanceScheduleToToday();
        //}
    }

    private void HandleDayChanged()
    {
        AdvanceScheduleToToday();
    }

    public void SetTomorrowSchedule(ScheduleType scheduleType)
    {
        tomorrowSchedule = scheduleType;

        Debug.Log("明日の予定を設定しました：" + GetScheduleDisplayName(tomorrowSchedule));
    }

    public void ClearTomorrowSchedule()
    {
        tomorrowSchedule = ScheduleType.None;
    }

    public void AdvanceScheduleToToday()
    {
        todaySchedule = tomorrowSchedule;
        tomorrowSchedule = ScheduleType.None;

        Debug.Log("今日の予定：" + GetScheduleDisplayName(todaySchedule));
    }

    public void SetScheduleState(ScheduleType today, ScheduleType tomorrow)
    {
        todaySchedule = today;
        tomorrowSchedule = tomorrow;
    }

    public static string GetScheduleDisplayName(ScheduleType scheduleType)
    {
        switch (scheduleType)
        {
            case ScheduleType.None:
                return "なし";

            case ScheduleType.SoloForest:
                return "森へ";

            case ScheduleType.SoloCave:
                return "洞窟へ";

            case ScheduleType.SoloLake:
                return "湖へ";

            case ScheduleType.SoloShopping:
                return "買い物へ";

            case ScheduleType.DuoForest:
                return "二人で森へ";

            case ScheduleType.DuoCave:
                return "二人で洞窟へ";

            case ScheduleType.DuoLake:
                return "二人で湖へ";

            case ScheduleType.DuoShopping:
                return "二人で買い物へ";

            case ScheduleType.StayHome:
                return "在宅";

            default:
                return "不明";
        }
    }

    public string GetTodayScheduleDisplayName()
    {
        return GetScheduleDisplayName(todaySchedule);
    }

    public string GetTomorrowScheduleDisplayName()
    {
        return GetScheduleDisplayName(tomorrowSchedule);
    }

    public bool IsTodayOutdoorSchedule()
    {
        switch (todaySchedule)
        {
            case ScheduleType.SoloForest:
            case ScheduleType.SoloCave:
            case ScheduleType.SoloLake:
            case ScheduleType.SoloShopping:
            case ScheduleType.DuoForest:
            case ScheduleType.DuoCave:
            case ScheduleType.DuoLake:
            case ScheduleType.DuoShopping:
                return true;

            default:
                return false;
        }
    }

    public bool IsTodayDuoSchedule()
    {
        switch (todaySchedule)
        {
            case ScheduleType.DuoForest:
            case ScheduleType.DuoCave:
            case ScheduleType.DuoLake:
            case ScheduleType.DuoShopping:
                return true;

            default:
                return false;
        }
    }

    public bool IsTodayHomeSchedule()
    {
        return todaySchedule == ScheduleType.StayHome;
    }
}
