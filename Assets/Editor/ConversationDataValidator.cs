using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public sealed class ConversationDataValidationEntry
{
    public string Message { get; private set; }
    public UnityEngine.Object Context { get; private set; }

    public ConversationDataValidationEntry(string message, UnityEngine.Object context)
    {
        Message = message;
        Context = context;
    }
}

public sealed class ConversationDataValidationReport
{
    private readonly List<ConversationDataValidationEntry> warnings =
        new List<ConversationDataValidationEntry>();

    public int HeroineCount { get; internal set; }
    public int AssetCount { get; internal set; }
    public int ConversationCount { get; internal set; }
    public int SkippedAssetCount { get; internal set; }
    public int WarningCount => warnings.Count;
    public bool IsValid => WarningCount == 0;
    public IReadOnlyList<ConversationDataValidationEntry> Warnings => warnings;

    internal void Warn(string message, UnityEngine.Object context)
    {
        warnings.Add(new ConversationDataValidationEntry(message, context));
    }

    public string CreateSummary()
    {
        return
            "Conversation data validation: heroines=" + HeroineCount +
            " / assets=" + AssetCount +
            " / conversations=" + ConversationCount +
            " / skipped=" + SkippedAssetCount +
            " / warnings=" + WarningCount;
    }

    public void Log()
    {
        string summary = "[ConversationDataValidation] " + CreateSummary();
        if (IsValid)
        {
            Debug.Log(summary);
        }
        else
        {
            Debug.LogWarning(summary);
        }

        foreach (ConversationDataValidationEntry warning in warnings)
        {
            Debug.LogWarning("[ConversationDataValidation] " + warning.Message, warning.Context);
        }
    }
}

public static class ConversationDataValidator
{
    private const string HeroineAssetRoot = "Assets/Resources/Heroines";
    private static readonly Regex ValidIdPattern =
        new Regex("^[A-Za-z][A-Za-z0-9_]*$", RegexOptions.CultureInvariant);

    private sealed class ConversationRecord
    {
        public string HeroineId;
        public string ConversationId;
        public string SourceLabel;
        public string AssetPath;
        public bool IsStandalone;
        public UnityEngine.Object Context;
        public ConversationGenre Genre;
        public ConversationType Type;
        public int Priority;
        public bool ShowOnce;
        public int MinAffection;
        public int MaxAffection;
        public string CostumeId;
        public bool AnyTimeSlot;
        public IEnumerable<TimeSlot> AllowedTimeSlots;
        public bool AnySeason;
        public IEnumerable<Season> AllowedSeasons;
        public bool AnyWeather;
        public IEnumerable<Weather> AllowedWeathers;
        public IList<ConversationChoice> Choices;
    }

    public static ConversationDataValidationReport ValidateProjectAssets()
    {
        ConversationDataValidationReport report = new ConversationDataValidationReport();
        List<ConversationRecord> records = new List<ConversationRecord>();
        string[] guids = AssetDatabase.FindAssets(
            "t:" + nameof(ConversationData),
            new[] { HeroineAssetRoot });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ConversationData asset = AssetDatabase.LoadAssetAtPath<ConversationData>(path);
            if (asset == null)
            {
                report.SkippedAssetCount++;
                report.Warn("会話アセットを読み込めないためスキップしました: " + path, null);
                continue;
            }

            report.AssetCount++;
            string heroineId = GetHeroineId(path);
            ValidateAssetOwner(asset, path, heroineId, report);
            AddRecords(asset, path, heroineId, records, report);
        }

        ValidateRecords(records, report);
        report.HeroineCount = records
            .Select(record => record.HeroineId)
            .Where(heroineId => !string.IsNullOrWhiteSpace(heroineId))
            .Distinct(StringComparer.Ordinal)
            .Count();
        report.ConversationCount = records.Count;
        return report;
    }

    internal static ConversationDataValidationReport ValidateForTests(
        string heroineId,
        params ConversationData[] conversations)
    {
        ConversationDataValidationReport report = new ConversationDataValidationReport();
        List<ConversationRecord> records = new List<ConversationRecord>();
        foreach (ConversationData conversation in conversations ?? new ConversationData[0])
        {
            if (conversation == null) continue;
            report.AssetCount++;
            AddRecords(
                conversation,
                conversation.name + ".asset",
                heroineId,
                records,
                report);
        }

        ValidateRecords(records, report);
        report.HeroineCount = records.Count > 0 ? 1 : 0;
        report.ConversationCount = records.Count;
        return report;
    }

    private static void ValidateAssetOwner(
        ConversationData asset,
        string path,
        string pathHeroineId,
        ConversationDataValidationReport report)
    {
        if (string.IsNullOrWhiteSpace(pathHeroineId))
        {
            report.Warn("ヒロインIDをパスから判定できません: " + path, asset);
            return;
        }

        if (string.IsNullOrWhiteSpace(asset.heroineId))
        {
            report.Warn(
                "ConversationData.heroineId が空です: heroine=" + pathHeroineId +
                " / asset=" + asset.name,
                asset);
        }
        else if (!string.Equals(asset.heroineId, pathHeroineId, StringComparison.Ordinal))
        {
            report.Warn(
                "ConversationData.heroineId が配置先ヒロインと一致しません: expected=" +
                pathHeroineId + " / actual=" + asset.heroineId + " / asset=" + asset.name,
                asset);
        }
    }

    private static void AddRecords(
        ConversationData asset,
        string path,
        string heroineId,
        List<ConversationRecord> records,
        ConversationDataValidationReport report)
    {
        if (asset.items != null && asset.items.Count > 0)
        {
            for (int i = 0; i < asset.items.Count; i++)
            {
                ConversationDataItem item = asset.items[i];
                if (item == null)
                {
                    report.Warn(asset.name + ".items[" + i + "] がnullです。", asset);
                    continue;
                }

                records.Add(CreateRecord(asset, item, heroineId, path, i));
            }
            return;
        }

        records.Add(CreateRecord(asset, heroineId, path));
    }

    private static ConversationRecord CreateRecord(
        ConversationData asset,
        string heroineId,
        string path)
    {
        return new ConversationRecord
        {
            HeroineId = heroineId,
            ConversationId = asset.conversationId,
            SourceLabel = "heroine=" + heroineId + " / asset=" + asset.name,
            AssetPath = path,
            IsStandalone = true,
            Context = asset,
            Genre = asset.genre,
            Type = asset.type,
            Priority = asset.priority,
            ShowOnce = asset.showOnce,
            MinAffection = asset.minAffection,
            MaxAffection = asset.maxAffection,
            CostumeId = asset.costumeId,
            AnyTimeSlot = asset.anyTimeSlot,
            AllowedTimeSlots = asset.allowedTimeSlots,
            AnySeason = asset.anySeason,
            AllowedSeasons = asset.allowedSeasons,
            AnyWeather = asset.anyWeather,
            AllowedWeathers = asset.allowedWeathers,
            Choices = asset.choices
        };
    }

    private static ConversationRecord CreateRecord(
        ConversationData asset,
        ConversationDataItem item,
        string heroineId,
        string path,
        int index)
    {
        return new ConversationRecord
        {
            HeroineId = heroineId,
            ConversationId = item.conversationId,
            SourceLabel =
                "heroine=" + heroineId + " / asset=" + asset.name +
                " / item[" + index + "]=" + item.conversationId,
            AssetPath = path,
            IsStandalone = false,
            Context = asset,
            Genre = item.genre,
            Type = item.type,
            Priority = item.priority,
            ShowOnce = item.showOnce,
            MinAffection = item.minAffection,
            MaxAffection = item.maxAffection,
            CostumeId = item.costumeId,
            AnyTimeSlot = item.anyTimeSlot,
            AllowedTimeSlots = item.allowedTimeSlots,
            AnySeason = item.anySeason,
            AllowedSeasons = item.allowedSeasons,
            AnyWeather = item.anyWeather,
            AllowedWeathers = item.allowedWeathers,
            Choices = item.choices
        };
    }

    private static void ValidateRecords(
        List<ConversationRecord> records,
        ConversationDataValidationReport report)
    {
        Dictionary<string, ConversationRecord> recordsById =
            new Dictionary<string, ConversationRecord>(StringComparer.Ordinal);
        Dictionary<string, ConversationRecord> recordsByCondition =
            new Dictionary<string, ConversationRecord>(StringComparer.Ordinal);
        Dictionary<string, HashSet<ConversationGenre>> genresByHeroine =
            new Dictionary<string, HashSet<ConversationGenre>>(StringComparer.Ordinal);
        HashSet<string> fallbackKeys = new HashSet<string>(StringComparer.Ordinal);
        Dictionary<string, ConversationRecord> genreContexts =
            new Dictionary<string, ConversationRecord>(StringComparer.Ordinal);

        foreach (ConversationRecord record in records)
        {
            ValidateRecord(record, report);
            string idKey = record.HeroineId + "|" + record.ConversationId;
            if (!string.IsNullOrWhiteSpace(record.ConversationId))
            {
                ConversationRecord existing;
                if (recordsById.TryGetValue(idKey, out existing))
                {
                    report.Warn(
                        "conversationId が同じヒロイン内で重複しています: " +
                        record.ConversationId + " / sources=" + existing.SourceLabel +
                        ", " + record.SourceLabel,
                        record.Context);
                }
                else
                {
                    recordsById.Add(idKey, record);
                }
            }

            string conditionKey = record.HeroineId + "|" + CreateConditionKey(record);
            ConversationRecord conditionMatch;
            if (recordsByCondition.TryGetValue(conditionKey, out conditionMatch))
            {
                report.Warn(
                    "同じヒロイン内に種別・条件・優先度が同一の会話があります: " +
                    conditionMatch.ConversationId + " / " + record.ConversationId,
                    record.Context);
            }
            else
            {
                recordsByCondition.Add(conditionKey, record);
            }

            HashSet<ConversationGenre> genres;
            if (!genresByHeroine.TryGetValue(record.HeroineId, out genres))
            {
                genres = new HashSet<ConversationGenre>();
                genresByHeroine.Add(record.HeroineId, genres);
            }
            genres.Add(record.Genre);

            string genreKey = record.HeroineId + "|" + record.Genre;
            if (!genreContexts.ContainsKey(genreKey))
            {
                genreContexts.Add(genreKey, record);
            }
            if (IsFallback(record))
            {
                fallbackKeys.Add(genreKey);
            }
        }

        foreach (KeyValuePair<string, HashSet<ConversationGenre>> heroineGenres in genresByHeroine)
        {
            foreach (ConversationGenre genre in heroineGenres.Value)
            {
                string genreKey = heroineGenres.Key + "|" + genre;
                if (!fallbackKeys.Contains(genreKey))
                {
                    ConversationRecord context = genreContexts[genreKey];
                    report.Warn(
                        "条件不成立時に使える会話がありません: heroine=" + heroineGenres.Key +
                        " / genre=" + genre +
                        "。showOnce=false、好感度0〜9999、その他条件なしの会話を用意してください。",
                        context.Context);
                }
            }
        }
    }

    private static void ValidateRecord(
        ConversationRecord record,
        ConversationDataValidationReport report)
    {
        if (string.IsNullOrWhiteSpace(record.ConversationId))
        {
            report.Warn(record.SourceLabel + " の conversationId が空です。", record.Context);
        }
        else if (!ValidIdPattern.IsMatch(record.ConversationId))
        {
            report.Warn(
                record.SourceLabel + " の conversationId に使用できない文字があります: " +
                record.ConversationId + "。英字で開始し、英数字とアンダースコアを使用してください。",
                record.Context);
        }

        if (record.IsStandalone &&
            !string.IsNullOrWhiteSpace(record.ConversationId) &&
            !string.Equals(
                System.IO.Path.GetFileNameWithoutExtension(record.AssetPath),
                record.ConversationId,
                StringComparison.Ordinal))
        {
            report.Warn(
                record.SourceLabel + " のファイル名と conversationId が一致しません: " +
                System.IO.Path.GetFileNameWithoutExtension(record.AssetPath) + " / " +
                record.ConversationId,
                record.Context);
        }

        if (!Enum.IsDefined(typeof(ConversationGenre), record.Genre))
        {
            report.Warn(record.SourceLabel + " の genre が不正です: " + (int)record.Genre, record.Context);
        }
        if (!Enum.IsDefined(typeof(ConversationType), record.Type))
        {
            report.Warn(record.SourceLabel + " の type が不正です: " + (int)record.Type, record.Context);
        }

        int choiceCount = record.Choices == null ? 0 : record.Choices.Count;
        if (record.Type == ConversationType.Choice && choiceCount == 0)
        {
            report.Warn(record.SourceLabel + " はChoiceですが選択肢がありません。", record.Context);
        }
        else if (record.Type == ConversationType.Simple && choiceCount > 0)
        {
            report.Warn(record.SourceLabel + " はSimpleですが選択肢が設定されています。", record.Context);
        }
    }

    private static bool IsFallback(ConversationRecord record)
    {
        return
            !record.ShowOnce &&
            record.MinAffection == 0 &&
            record.MaxAffection == AffectionDataValidator.MaximumAffection &&
            string.IsNullOrWhiteSpace(record.CostumeId) &&
            record.AnyTimeSlot &&
            record.AnySeason &&
            record.AnyWeather;
    }

    private static string CreateConditionKey(ConversationRecord record)
    {
        return string.Join(
            "|",
            record.Genre,
            record.Type,
            record.Priority,
            record.ShowOnce,
            record.MinAffection,
            record.MaxAffection,
            record.CostumeId ?? string.Empty,
            record.AnyTimeSlot,
            CreateEnumListKey(record.AllowedTimeSlots),
            record.AnySeason,
            CreateEnumListKey(record.AllowedSeasons),
            record.AnyWeather,
            CreateEnumListKey(record.AllowedWeathers));
    }

    private static string CreateEnumListKey<T>(IEnumerable<T> values)
    {
        return values == null
            ? string.Empty
            : string.Join(",", values.Select(value => Convert.ToInt32(value)).OrderBy(value => value));
    }

    private static string GetHeroineId(string assetPath)
    {
        string prefix = HeroineAssetRoot + "/";
        if (string.IsNullOrWhiteSpace(assetPath) ||
            !assetPath.StartsWith(prefix, StringComparison.Ordinal))
        {
            return string.Empty;
        }

        string relativePath = assetPath.Substring(prefix.Length);
        int separatorIndex = relativePath.IndexOf('/');
        return separatorIndex > 0 ? relativePath.Substring(0, separatorIndex) : string.Empty;
    }
}
