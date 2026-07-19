using System;

public enum ScheduleEntryState
{
    Planned,
    Executed,
    Cancelled
}

[Serializable]
public class ScheduleEntry
{
    public int day;
    public ScheduleType scheduleType;
    public ScheduleEntryState state = ScheduleEntryState.Planned;
    public string cancelReason;

    public ScheduleEntry Clone()
    {
        return new ScheduleEntry
        {
            day = day,
            scheduleType = scheduleType,
            state = state,
            cancelReason = cancelReason
        };
    }
}
