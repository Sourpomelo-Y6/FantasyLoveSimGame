using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ScheduleTemplateManager : MonoBehaviour
{
    public const int WeeklyTemplateDayCount = 7;
    public const int MonthlyTemplateDayCount = 30;
    private const int CurrentLibraryVersion = 1;

    [SerializeField] private string templateFileName = "schedule_templates.json";

    private ScheduleTemplateLibrary library;

    public string TemplateFilePath
    {
        get { return Path.Combine(Application.persistentDataPath, templateFileName); }
    }

    public List<ScheduleTemplateData> GetTemplates()
    {
        EnsureLoaded();
        List<ScheduleTemplateData> result = new List<ScheduleTemplateData>();
        for (int i = 0; i < library.templates.Count; i++)
        {
            ScheduleTemplateData template = library.templates[i];
            if (template != null) result.Add(template.Clone());
        }

        result.Sort((left, right) => string.Compare(
            left.displayName,
            right.displayName,
            StringComparison.CurrentCulture));
        return result;
    }

    public bool TrySaveTemplate(
        string displayName,
        int startDay,
        int dayCount,
        ScheduleManager scheduleManager,
        string overwriteTemplateId,
        out ScheduleTemplateData savedTemplate,
        out string message)
    {
        savedTemplate = null;
        if (scheduleManager == null)
        {
            message = "ScheduleManager が設定されていません。";
            return false;
        }

        string normalizedName = string.IsNullOrWhiteSpace(displayName) ? string.Empty : displayName.Trim();
        if (string.IsNullOrEmpty(normalizedName))
        {
            message = "テンプレート名を入力してください。";
            return false;
        }

        if (!IsSupportedDayCount(dayCount))
        {
            message = "テンプレート期間は7日または30日を指定してください。";
            return false;
        }

        if (startDay < 1)
        {
            message = "開始日はDay 1以降を指定してください。";
            return false;
        }

        EnsureLoaded();
        string previousJson = JsonUtility.ToJson(library);
        ScheduleTemplateData template = FindTemplate(overwriteTemplateId);
        if (template == null)
        {
            template = new ScheduleTemplateData
            {
                templateId = Guid.NewGuid().ToString("N")
            };
            library.templates.Add(template);
        }

        template.displayName = normalizedName;
        template.dayCount = dayCount;
        template.days = new List<ScheduleTemplateDay>();
        for (int offset = 0; offset < dayCount; offset++)
        {
            ScheduleEntry entry;
            ScheduleType scheduleType = scheduleManager.TryGetScheduleEntry(startDay + offset, out entry)
                ? entry.scheduleType
                : ScheduleType.None;
            template.days.Add(new ScheduleTemplateDay
            {
                dayOffset = offset,
                scheduleType = scheduleType
            });
        }

        NormalizeLibrary();
        if (!TryWriteLibrary(out message))
        {
            library = JsonUtility.FromJson<ScheduleTemplateLibrary>(previousJson);
            NormalizeLibrary();
            return false;
        }

        savedTemplate = template.Clone();
        message = "テンプレート「" + template.displayName + "」を保存しました。";
        return true;
    }

    public bool TryDeleteTemplate(string templateId, out string message)
    {
        EnsureLoaded();
        string previousJson = JsonUtility.ToJson(library);
        int removedCount = library.templates.RemoveAll(template =>
            template != null && string.Equals(template.templateId, templateId, StringComparison.Ordinal));
        if (removedCount == 0)
        {
            message = "削除するテンプレートが見つかりません。";
            return false;
        }

        if (!TryWriteLibrary(out message))
        {
            library = JsonUtility.FromJson<ScheduleTemplateLibrary>(previousJson);
            NormalizeLibrary();
            return false;
        }
        message = "テンプレートを削除しました。";
        return true;
    }

    public bool TryPreviewTemplateApplication(
        string templateId,
        int startDay,
        bool overwriteExisting,
        ScheduleManager scheduleManager,
        out ScheduleTemplateApplyResult result,
        out string message)
    {
        return ProcessTemplateApplication(
            templateId,
            startDay,
            overwriteExisting,
            scheduleManager,
            false,
            out result,
            out message);
    }

    public bool TryApplyTemplate(
        string templateId,
        int startDay,
        bool overwriteExisting,
        ScheduleManager scheduleManager,
        out ScheduleTemplateApplyResult result,
        out string message)
    {
        return ProcessTemplateApplication(
            templateId,
            startDay,
            overwriteExisting,
            scheduleManager,
            true,
            out result,
            out message);
    }

    private bool ProcessTemplateApplication(
        string templateId,
        int startDay,
        bool overwriteExisting,
        ScheduleManager scheduleManager,
        bool applyChanges,
        out ScheduleTemplateApplyResult result,
        out string message)
    {
        result = new ScheduleTemplateApplyResult();
        if (scheduleManager == null)
        {
            message = "ScheduleManager が設定されていません。";
            return false;
        }

        if (startDay < 1)
        {
            message = "開始日はDay 1以降を指定してください。";
            return false;
        }

        EnsureLoaded();
        ScheduleTemplateData template = FindTemplate(templateId);
        if (template == null)
        {
            message = "適用するテンプレートが見つかりません。";
            return false;
        }

        result.totalSlotCount = template.dayCount;
        Dictionary<int, ScheduleType> schedulesByOffset = CreateScheduleMap(template);
        for (int offset = 0; offset < template.dayCount; offset++)
        {
            int targetDay = startDay + offset;
            string editMessage;
            if (!scheduleManager.CanEditScheduleForDay(targetDay, out editMessage))
            {
                result.skippedCount++;
                continue;
            }

            ScheduleType scheduleType;
            if (!schedulesByOffset.TryGetValue(offset, out scheduleType))
            {
                scheduleType = ScheduleType.None;
            }

            ScheduleEntry existingEntry;
            bool hasExisting = scheduleManager.TryGetScheduleEntry(targetDay, out existingEntry);
            if (hasExisting && !overwriteExisting)
            {
                result.conflictCount++;
                continue;
            }

            if (!hasExisting && scheduleType == ScheduleType.None)
            {
                result.skippedCount++;
                continue;
            }

            if (!applyChanges)
            {
                result.appliedCount++;
                continue;
            }

            string setMessage;
            if (scheduleManager.TrySetScheduleForDay(targetDay, scheduleType, out setMessage))
            {
                result.appliedCount++;
            }
            else
            {
                result.errorCount++;
            }
        }

        message = "テンプレート「" + template.displayName + "」: " +
            (applyChanges ? result.CreateSummary() : result.CreatePreviewSummary());
        return result.errorCount == 0;
    }

    public void Reload()
    {
        library = null;
        EnsureLoaded();
    }

    private void EnsureLoaded()
    {
        if (library != null) return;

        library = new ScheduleTemplateLibrary();
        if (!File.Exists(TemplateFilePath)) return;

        try
        {
            string json = File.ReadAllText(TemplateFilePath);
            ScheduleTemplateLibrary loaded = JsonUtility.FromJson<ScheduleTemplateLibrary>(json);
            if (loaded != null) library = loaded;
            NormalizeLibrary();
        }
        catch (Exception exception)
        {
            Debug.LogWarning("スケジュールテンプレートを読み込めませんでした。新しい一覧を使用します。\n" + exception.Message);
            library = new ScheduleTemplateLibrary();
        }
    }

    private bool TryWriteLibrary(out string message)
    {
        string temporaryPath = TemplateFilePath + ".tmp";
        try
        {
            string json = JsonUtility.ToJson(library, true);
            File.WriteAllText(temporaryPath, json);
            File.Copy(temporaryPath, TemplateFilePath, true);
            File.Delete(temporaryPath);
            message = string.Empty;
            return true;
        }
        catch (Exception exception)
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
            message = "テンプレートを保存できませんでした: " + exception.Message;
            Debug.LogWarning(message);
            return false;
        }
    }

    private ScheduleTemplateData FindTemplate(string templateId)
    {
        if (string.IsNullOrEmpty(templateId)) return null;
        for (int i = 0; i < library.templates.Count; i++)
        {
            ScheduleTemplateData template = library.templates[i];
            if (template != null &&
                string.Equals(template.templateId, templateId, StringComparison.Ordinal))
            {
                return template;
            }
        }

        return null;
    }

    private static Dictionary<int, ScheduleType> CreateScheduleMap(ScheduleTemplateData template)
    {
        Dictionary<int, ScheduleType> result = new Dictionary<int, ScheduleType>();
        if (template.days == null) return result;
        for (int i = 0; i < template.days.Count; i++)
        {
            ScheduleTemplateDay day = template.days[i];
            if (day != null && day.dayOffset >= 0 && day.dayOffset < template.dayCount)
            {
                result[day.dayOffset] = day.scheduleType;
            }
        }
        return result;
    }

    private void NormalizeLibrary()
    {
        if (library == null) library = new ScheduleTemplateLibrary();
        library.version = CurrentLibraryVersion;
        if (library.templates == null) library.templates = new List<ScheduleTemplateData>();

        HashSet<string> usedIds = new HashSet<string>();
        List<ScheduleTemplateData> normalized = new List<ScheduleTemplateData>();
        for (int i = 0; i < library.templates.Count; i++)
        {
            ScheduleTemplateData template = library.templates[i];
            if (template == null ||
                string.IsNullOrWhiteSpace(template.displayName) ||
                !IsSupportedDayCount(template.dayCount))
            {
                continue;
            }

            if (string.IsNullOrEmpty(template.templateId) || usedIds.Contains(template.templateId))
            {
                template.templateId = Guid.NewGuid().ToString("N");
            }
            usedIds.Add(template.templateId);
            if (template.days == null) template.days = new List<ScheduleTemplateDay>();
            normalized.Add(template);
        }

        library.templates = normalized;
    }

    private static bool IsSupportedDayCount(int dayCount)
    {
        return dayCount == WeeklyTemplateDayCount || dayCount == MonthlyTemplateDayCount;
    }
}
