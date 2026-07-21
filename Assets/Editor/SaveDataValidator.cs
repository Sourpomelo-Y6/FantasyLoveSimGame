using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public sealed class SaveDataValidationReport
{
    private readonly List<string> warnings = new List<string>();

    public int FileCount { get; internal set; }
    public int ValidatedSaveCount { get; internal set; }
    public int SkippedFileCount { get; internal set; }
    public int WarningCount => warnings.Count;
    public bool IsValid => WarningCount == 0;
    public IReadOnlyList<string> Warnings => warnings;

    internal void Warn(string message)
    {
        warnings.Add(message);
    }

    public string CreateSummary()
    {
        return
            "Save data validation: files=" + FileCount +
            " / saves=" + ValidatedSaveCount +
            " / skipped=" + SkippedFileCount +
            " / warnings=" + WarningCount;
    }

    public void Log()
    {
        string summary = "[SaveDataValidation] " + CreateSummary();
        if (IsValid) Debug.Log(summary); else Debug.LogWarning(summary);
        foreach (string warning in warnings)
        {
            Debug.LogWarning("[SaveDataValidation] " + warning);
        }
    }
}

public static class SaveDataValidator
{
    private static readonly Regex SaveFilePattern =
        new Regex("^save(?:_slot_[0-9]+)?\\.json$", RegexOptions.IgnoreCase);

    public static SaveDataValidationReport ValidatePersistentSaveFiles()
    {
        SaveDataValidationReport report = new SaveDataValidationReport();
        string root = Application.persistentDataPath;
        if (!Directory.Exists(root))
        {
            return report;
        }

        string[] paths = Directory.GetFiles(root, "*.json", SearchOption.TopDirectoryOnly)
            .Where(path => SaveFilePattern.IsMatch(Path.GetFileName(path)))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        report.FileCount = paths.Length;
        foreach (string path in paths)
        {
            try
            {
                SaveData data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
                if (data == null)
                {
                    report.SkippedFileCount++;
                    report.Warn(Path.GetFileName(path) + " をSaveDataとして読み込めませんでした。");
                    continue;
                }
                report.ValidatedSaveCount++;
                Validate(data, Path.GetFileName(path), report);
            }
            catch (Exception exception)
            {
                report.SkippedFileCount++;
                report.Warn(Path.GetFileName(path) + " のJSONが不正です: " + exception.Message);
            }
        }
        return report;
    }

    internal static SaveDataValidationReport ValidateForTests(SaveData data)
    {
        SaveDataValidationReport report = new SaveDataValidationReport
        {
            FileCount = 1
        };
        if (data == null)
        {
            report.SkippedFileCount = 1;
            report.Warn("SaveDataがnullです。");
            return report;
        }
        report.ValidatedSaveCount = 1;
        Validate(data, "test", report);
        return report;
    }

    private static void Validate(SaveData data, string label, SaveDataValidationReport report)
    {
        if (data.saveVersion < 0 || data.saveVersion > SaveData.CurrentVersion)
        {
            report.Warn(label + " のsaveVersionが未対応です: " + data.saveVersion);
        }
        if (string.IsNullOrWhiteSpace(data.heroineId))
        {
            report.Warn(label + " のheroineIdが空です。");
        }
        if (data.day < 1) report.Warn(label + " のdayが1未満です: " + data.day);
        if (data.saveSlotIndex < 0) report.Warn(label + " のsaveSlotIndexが負数です。");
        if (data.affection < 0 || data.affection > 9999)
        {
            report.Warn(label + " のaffectionが範囲外です: " + data.affection);
        }
        if (data.playerMoney < 0) report.Warn(label + " のplayerMoneyが負数です。");
        if (data.playerSkillPoints < 0 || data.heroineSkillPoints < 0)
        {
            report.Warn(label + " のskillPointsが負数です。");
        }

        ValidateRequiredObject(data.playerBattleStatus, label + ".playerBattleStatus", report);
        ValidateRequiredObject(data.heroineBattleStatus, label + ".heroineBattleStatus", report);
        ValidateRequiredObject(data.playerOutfitPromptAbilities, label + ".playerOutfitPromptAbilities", report);
        ValidateRequiredObject(data.skillProgressStats, label + ".skillProgressStats", report);
        ValidateRequiredObject(data.outfitPreferences, label + ".outfitPreferences", report);
        ValidateRequiredObject(data.itemQuantities, label + ".itemQuantities", report);
        ValidateRequiredObject(data.trainingProficiencies, label + ".trainingProficiencies", report);

        ValidateIdList(data.unlockedSkillIds, label + ".unlockedSkillIds", report);
        ValidateIdList(data.equippedPlayerBattleSkillIds, label + ".equippedPlayerBattleSkillIds", report);
        ValidateIdList(data.activePlayerTrainingSkillIds, label + ".activePlayerTrainingSkillIds", report);
        ValidateIdList(data.unlockedStillIds, label + ".unlockedStillIds", report);
        ValidateIdList(data.purchasedItemIds, label + ".purchasedItemIds", report);
        ValidateIdList(data.unlockedOutfitIds, label + ".unlockedOutfitIds", report);
        ValidateIdList(data.acquiredPlayerSkillTreeNodeIds, label + ".acquiredPlayerSkillTreeNodeIds", report);
        ValidateIdList(data.acquiredHeroineSkillTreeNodeIds, label + ".acquiredHeroineSkillTreeNodeIds", report);
        ValidateIdList(data.shownConversationIds, label + ".shownConversationIds", report);
        ValidateIdList(data.shownGameEventIds, label + ".shownGameEventIds", report);

        ValidateHeroineEntries(data, label, report);
        ValidateScheduleEntries(data.scheduleEntries, label, report);
    }

    private static void ValidateRequiredObject(object value, string label, SaveDataValidationReport report)
    {
        if (value == null) report.Warn(label + " がnullです。");
    }

    private static void ValidateIdList(
        IEnumerable<string> values,
        string label,
        SaveDataValidationReport report)
    {
        if (values == null)
        {
            report.Warn(label + " がnullです。");
            return;
        }
        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (string value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                report.Warn(label + " に空のIDがあります。");
            }
            else if (!seen.Add(value))
            {
                report.Warn(label + " に重複IDがあります: " + value);
            }
        }
    }

    private static void ValidateHeroineEntries(
        SaveData data,
        string label,
        SaveDataValidationReport report)
    {
        if (data.heroineBattleSkillLoadouts == null)
        {
            report.Warn(label + ".heroineBattleSkillLoadouts がnullです。");
        }
        else
        {
            HashSet<string> heroineIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (HeroineBattleSkillLoadoutEntry entry in data.heroineBattleSkillLoadouts)
            {
                if (entry == null || !string.Equals(entry.heroineId, data.heroineId, StringComparison.Ordinal))
                {
                    report.Warn(label + " に選択中ヒロイン以外の戦闘スキル構成があります。");
                }
                else if (!heroineIds.Add(entry.heroineId))
                {
                    report.Warn(label + " に同じヒロインの戦闘スキル構成が重複しています。");
                }
            }
        }

        if (data.heroineTrainingSkillActivations == null)
        {
            report.Warn(label + ".heroineTrainingSkillActivations がnullです。");
        }
        else
        {
            HashSet<string> heroineIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (HeroineTrainingSkillActivationEntry entry in data.heroineTrainingSkillActivations)
            {
                if (entry == null || !string.Equals(entry.heroineId, data.heroineId, StringComparison.Ordinal))
                {
                    report.Warn(label + " に選択中ヒロイン以外の訓練スキル設定があります。");
                }
                else if (!heroineIds.Add(entry.heroineId))
                {
                    report.Warn(label + " に同じヒロインの訓練スキル設定が重複しています。");
                }
            }
        }
    }

    private static void ValidateScheduleEntries(
        IEnumerable<ScheduleEntry> values,
        string label,
        SaveDataValidationReport report)
    {
        if (values == null)
        {
            report.Warn(label + ".scheduleEntries がnullです。");
            return;
        }
        HashSet<int> days = new HashSet<int>();
        foreach (ScheduleEntry entry in values)
        {
            if (entry == null || entry.day < 1)
            {
                report.Warn(label + ".scheduleEntries に不正な日付があります。");
            }
            else if (!days.Add(entry.day))
            {
                report.Warn(label + ".scheduleEntries に同じ日付が重複しています: " + entry.day);
            }
        }
    }
}
