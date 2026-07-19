using System;
using System.Collections.Generic;
using UnityEngine;

public class ScheduleManager : MonoBehaviour
{
    public const int ScheduleEntrySaveVersion = 19;
    public const int MaximumEditableDayOffset = 30;

    [Header("Legacy Schedule State")]
    [SerializeField] private ScheduleType todaySchedule = ScheduleType.None;
    [SerializeField] private ScheduleType tomorrowSchedule = ScheduleType.None;
    [SerializeField] private bool todayScheduleEventExecuted;

    [Header("Schedule Entries")]
    [SerializeField] private List<ScheduleEntry> scheduleEntries = new List<ScheduleEntry>();

    [Header("References")]
    [SerializeField] private TimeManager timeManager;

    private int CurrentDay
    {
        get { return timeManager != null ? Mathf.Max(1, timeManager.Day) : 1; }
    }

    public ScheduleType TodaySchedule
    {
        get { return GetPlannedScheduleType(CurrentDay); }
    }

    public ScheduleType TomorrowSchedule
    {
        get { return GetPlannedScheduleType(CurrentDay + 1); }
    }

    public bool TodayScheduleEventExecuted
    {
        get
        {
            ScheduleEntry entry = FindEntry(CurrentDay);
            return entry != null && entry.state == ScheduleEntryState.Executed;
        }
    }

    private void Awake()
    {
        if ((scheduleEntries == null || scheduleEntries.Count == 0) &&
            (todaySchedule != ScheduleType.None || tomorrowSchedule != ScheduleType.None))
        {
            LoadScheduleState(null, 0, todaySchedule, tomorrowSchedule, todayScheduleEventExecuted);
        }
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
        // デバッグ操作。通常UIは日付指定APIを使用する。
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SetTomorrowSchedule(ScheduleType.DuoLake);
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            AdvanceScheduleToToday();
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            Debug.Log("今日の予定：" + GetTodayScheduleDisplayName());
            Debug.Log("明日の予定：" + GetTomorrowScheduleDisplayName());
        }
    }

    private void HandleDayChanged()
    {
        AdvanceScheduleToToday();
    }

    public bool TryGetScheduleEntry(int day, out ScheduleEntry entry)
    {
        ScheduleEntry storedEntry = FindEntry(day);
        entry = storedEntry != null ? storedEntry.Clone() : null;
        return entry != null;
    }

    public List<ScheduleEntry> GetScheduleEntries(int startDay, int endDay)
    {
        EnsureEntryList();
        int firstDay = Mathf.Max(1, Math.Min(startDay, endDay));
        int lastDay = Mathf.Max(firstDay, Math.Max(startDay, endDay));
        List<ScheduleEntry> result = new List<ScheduleEntry>();
        for (int i = 0; i < scheduleEntries.Count; i++)
        {
            ScheduleEntry entry = scheduleEntries[i];
            if (entry != null && entry.day >= firstDay && entry.day <= lastDay)
            {
                result.Add(entry.Clone());
            }
        }

        result.Sort((a, b) => a.day.CompareTo(b.day));
        return result;
    }

    public List<ScheduleEntry> CreateScheduleEntrySaveData()
    {
        return GetScheduleEntries(1, int.MaxValue);
    }

    public bool TrySetScheduleForDay(int day, ScheduleType scheduleType, out string message)
    {
        if (!CanEditDay(day, out message))
        {
            return false;
        }

        if (scheduleType == ScheduleType.None)
        {
            RemoveEntry(day);
            SynchronizeLegacyState();
            message = "Day " + day + "の予定をなしにしました。";
            return true;
        }

        ScheduleEntry entry = FindEntry(day);
        if (entry == null)
        {
            entry = new ScheduleEntry { day = day };
            scheduleEntries.Add(entry);
        }

        entry.scheduleType = scheduleType;
        entry.state = ScheduleEntryState.Planned;
        entry.cancelReason = string.Empty;
        NormalizeEntries();
        SynchronizeLegacyState();
        message = "Day " + day + "の予定を「" + GetScheduleDisplayName(scheduleType) + "」に設定しました。";
        return true;
    }

    public bool TryCancelSchedule(int day, string reason, out string message)
    {
        if (day < CurrentDay)
        {
            message = "過去の予定はキャンセルできません。";
            return false;
        }

        ScheduleEntry entry = FindEntry(day);
        if (entry == null || entry.scheduleType == ScheduleType.None)
        {
            message = "キャンセルできる予定がありません。";
            return false;
        }

        if (entry.state == ScheduleEntryState.Executed)
        {
            message = "実行済みの予定はキャンセルできません。";
            return false;
        }

        if (entry.state == ScheduleEntryState.Cancelled)
        {
            message = "この予定はすでにキャンセルされています。";
            return false;
        }

        entry.state = ScheduleEntryState.Cancelled;
        entry.cancelReason = string.IsNullOrWhiteSpace(reason) ? "Player" : reason.Trim();
        SynchronizeLegacyState();
        message = "Day " + day + "の予定をキャンセルしました。";
        return true;
    }

    public void SetTomorrowSchedule(ScheduleType scheduleType)
    {
        string message;
        if (TrySetScheduleForDay(CurrentDay + 1, scheduleType, out message))
        {
            Debug.Log(message);
        }
        else
        {
            Debug.LogWarning(message);
        }
    }

    public void ClearTomorrowSchedule()
    {
        string message;
        TrySetScheduleForDay(CurrentDay + 1, ScheduleType.None, out message);
    }

    public void AdvanceScheduleToToday()
    {
        SynchronizeLegacyState();
        Debug.Log("今日の予定：" + GetTodayScheduleDisplayName());
    }

    public void SetScheduleState(ScheduleType today, ScheduleType tomorrow)
    {
        SetScheduleState(today, tomorrow, false);
    }

    public void SetScheduleState(ScheduleType today, ScheduleType tomorrow, bool eventExecuted)
    {
        LoadScheduleState(null, 0, today, tomorrow, eventExecuted);
    }

    public void LoadScheduleState(
        List<ScheduleEntry> entries,
        int saveVersion,
        ScheduleType legacyToday,
        ScheduleType legacyTomorrow,
        bool legacyTodayExecuted)
    {
        EnsureEntryList();
        scheduleEntries.Clear();
        if (saveVersion >= ScheduleEntrySaveVersion)
        {
            CopyEntries(entries, scheduleEntries);
        }
        else
        {
            AddLegacyEntry(CurrentDay, legacyToday, legacyTodayExecuted);
            AddLegacyEntry(CurrentDay + 1, legacyTomorrow, false);
        }

        NormalizeEntries();
        SynchronizeLegacyState();
    }

    public void MarkTodayScheduleEventExecuted()
    {
        ScheduleEntry entry = FindEntry(CurrentDay);
        if (entry == null || entry.scheduleType == ScheduleType.None)
        {
            return;
        }

        entry.state = ScheduleEntryState.Executed;
        entry.cancelReason = string.Empty;
        SynchronizeLegacyState();
    }

    public static string GetScheduleDisplayName(ScheduleType scheduleType)
    {
        switch (scheduleType)
        {
            case ScheduleType.None: return "なし";
            case ScheduleType.SoloForest: return "森へ";
            case ScheduleType.SoloCave: return "洞窟へ";
            case ScheduleType.SoloLake: return "湖へ";
            case ScheduleType.SoloShopping: return "買い物へ";
            case ScheduleType.DuoForest: return "二人で森へ";
            case ScheduleType.DuoCave: return "二人で洞窟へ";
            case ScheduleType.DuoLake: return "二人で湖へ";
            case ScheduleType.DuoShopping: return "二人で買い物へ";
            case ScheduleType.StayHome: return "在宅";
            default: return "不明";
        }
    }

    public string GetTodayScheduleDisplayName()
    {
        return GetScheduleDisplayName(TodaySchedule);
    }

    public string GetTomorrowScheduleDisplayName()
    {
        return GetScheduleDisplayName(TomorrowSchedule);
    }

    public bool IsTodayOutdoorSchedule()
    {
        switch (TodaySchedule)
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
        switch (TodaySchedule)
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
        return TodaySchedule == ScheduleType.StayHome;
    }

    private ScheduleType GetPlannedScheduleType(int day)
    {
        ScheduleEntry entry = FindEntry(day);
        return entry != null && entry.state != ScheduleEntryState.Cancelled
            ? entry.scheduleType
            : ScheduleType.None;
    }

    private bool CanEditDay(int day, out string message)
    {
        if (day < CurrentDay)
        {
            message = "過去の予定は変更できません。";
            return false;
        }

        if (day > CurrentDay + MaximumEditableDayOffset)
        {
            message = "予定を設定できるのは現在日から30日先までです。";
            return false;
        }

        ScheduleEntry current = FindEntry(day);
        if (current != null && current.state == ScheduleEntryState.Executed)
        {
            message = "実行済みの予定は変更できません。";
            return false;
        }

        message = string.Empty;
        return true;
    }

    private ScheduleEntry FindEntry(int day)
    {
        EnsureEntryList();
        for (int i = 0; i < scheduleEntries.Count; i++)
        {
            ScheduleEntry entry = scheduleEntries[i];
            if (entry != null && entry.day == day)
            {
                return entry;
            }
        }
        return null;
    }

    private void RemoveEntry(int day)
    {
        scheduleEntries.RemoveAll(entry => entry == null || entry.day == day);
    }

    private void AddLegacyEntry(int day, ScheduleType scheduleType, bool executed)
    {
        if (scheduleType == ScheduleType.None) return;
        scheduleEntries.Add(new ScheduleEntry
        {
            day = day,
            scheduleType = scheduleType,
            state = executed ? ScheduleEntryState.Executed : ScheduleEntryState.Planned,
            cancelReason = string.Empty
        });
    }

    private static void CopyEntries(List<ScheduleEntry> source, List<ScheduleEntry> destination)
    {
        if (source == null) return;
        for (int i = 0; i < source.Count; i++)
        {
            ScheduleEntry entry = source[i];
            if (entry != null && entry.day > 0 && entry.scheduleType != ScheduleType.None)
            {
                destination.Add(entry.Clone());
            }
        }
    }

    private void NormalizeEntries()
    {
        EnsureEntryList();
        Dictionary<int, ScheduleEntry> entriesByDay = new Dictionary<int, ScheduleEntry>();
        for (int i = 0; i < scheduleEntries.Count; i++)
        {
            ScheduleEntry entry = scheduleEntries[i];
            if (entry == null || entry.day <= 0 || entry.scheduleType == ScheduleType.None) continue;
            if (entry.state != ScheduleEntryState.Cancelled)
            {
                entry.cancelReason = string.Empty;
            }
            entriesByDay[entry.day] = entry;
        }

        scheduleEntries = new List<ScheduleEntry>(entriesByDay.Values);
        scheduleEntries.Sort((a, b) => a.day.CompareTo(b.day));
    }

    private void EnsureEntryList()
    {
        if (scheduleEntries == null)
        {
            scheduleEntries = new List<ScheduleEntry>();
        }
    }

    private void SynchronizeLegacyState()
    {
        todaySchedule = GetPlannedScheduleType(CurrentDay);
        tomorrowSchedule = GetPlannedScheduleType(CurrentDay + 1);
        todayScheduleEventExecuted = TodayScheduleEventExecuted;
    }
}
