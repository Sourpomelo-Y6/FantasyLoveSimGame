using System;
using System.Collections.Generic;

[Serializable]
public class ScheduleTemplateDay
{
    public int dayOffset;
    public ScheduleType scheduleType;

    public ScheduleTemplateDay Clone()
    {
        return new ScheduleTemplateDay
        {
            dayOffset = dayOffset,
            scheduleType = scheduleType
        };
    }
}

[Serializable]
public class ScheduleTemplateData
{
    public string templateId;
    public string displayName;
    public int dayCount;
    public List<ScheduleTemplateDay> days = new List<ScheduleTemplateDay>();

    public ScheduleTemplateData Clone()
    {
        ScheduleTemplateData clone = new ScheduleTemplateData
        {
            templateId = templateId,
            displayName = displayName,
            dayCount = dayCount
        };

        if (days != null)
        {
            for (int i = 0; i < days.Count; i++)
            {
                if (days[i] != null) clone.days.Add(days[i].Clone());
            }
        }

        return clone;
    }
}

[Serializable]
public class ScheduleTemplateLibrary
{
    public int version = 1;
    public List<ScheduleTemplateData> templates = new List<ScheduleTemplateData>();
}

public class ScheduleTemplateApplyResult
{
    public int totalSlotCount;
    public int appliedCount;
    public int skippedCount;
    public int conflictCount;
    public int errorCount;

    public string CreateSummary()
    {
        return "適用 " + appliedCount + "件 / 競合 " + conflictCount +
            "件 / スキップ " + skippedCount + "件 / エラー " + errorCount + "件";
    }

    public string CreatePreviewSummary()
    {
        return "適用予定 " + appliedCount + "件 / 競合 " + conflictCount +
            "件 / スキップ予定 " + skippedCount + "件";
    }
}
