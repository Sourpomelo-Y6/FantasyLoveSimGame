using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public sealed class EndingDataValidationEntry
{
    public string Message { get; private set; }
    public UnityEngine.Object Context { get; private set; }

    public EndingDataValidationEntry(string message, UnityEngine.Object context)
    {
        Message = message;
        Context = context;
    }
}

public sealed class EndingDataValidationReport
{
    private readonly List<EndingDataValidationEntry> warnings =
        new List<EndingDataValidationEntry>();

    public int HeroineCount { get; internal set; }
    public int EndingCount { get; internal set; }
    public int SkippedAssetCount { get; internal set; }
    public int WarningCount => warnings.Count;
    public bool IsValid => WarningCount == 0;
    public IReadOnlyList<EndingDataValidationEntry> Warnings => warnings;

    internal void Warn(string message, UnityEngine.Object context)
    {
        warnings.Add(new EndingDataValidationEntry(message, context));
    }

    public string CreateSummary()
    {
        return
            "Ending data validation: heroines=" + HeroineCount +
            " / endings=" + EndingCount +
            " / skipped=" + SkippedAssetCount +
            " / warnings=" + WarningCount;
    }

    public void Log()
    {
        string summary = "[EndingDataValidation] " + CreateSummary();
        if (IsValid)
        {
            Debug.Log(summary);
        }
        else
        {
            Debug.LogWarning(summary);
        }

        foreach (EndingDataValidationEntry warning in warnings)
        {
            Debug.LogWarning("[EndingDataValidation] " + warning.Message, warning.Context);
        }
    }
}

public static class EndingDataValidator
{
    private const string HeroineAssetRoot = "Assets/Resources/Heroines";
    private static readonly Regex ValidIdPattern =
        new Regex("^[A-Za-z][A-Za-z0-9_]*$", RegexOptions.CultureInvariant);

    private sealed class EndingRecord
    {
        public string HeroineId;
        public EndingData Ending;
    }

    public static EndingDataValidationReport ValidateProjectAssets()
    {
        EndingDataValidationReport report = new EndingDataValidationReport();
        List<EndingRecord> records = LoadEndingRecords(report);
        ValidateRecords(
            records,
            LoadEventIdsByHeroine(),
            LoadOutfitIds(),
            LoadExpressionIdsByHeroine(),
            report);
        report.HeroineCount = records
            .Select(record => record.HeroineId)
            .Distinct(StringComparer.Ordinal)
            .Count();
        return report;
    }

    internal static EndingDataValidationReport ValidateForTests(
        string heroineId,
        IEnumerable<string> knownEventIds,
        IEnumerable<string> knownOutfitIds,
        params EndingData[] endings)
    {
        EndingDataValidationReport report = new EndingDataValidationReport();
        List<EndingRecord> records = (endings ?? new EndingData[0])
            .Where(ending => ending != null)
            .Select(ending => new EndingRecord
            {
                HeroineId = heroineId,
                Ending = ending
            })
            .ToList();

        Dictionary<string, HashSet<string>> eventIds =
            new Dictionary<string, HashSet<string>>(StringComparer.Ordinal)
            {
                { heroineId, CreateIdSet(knownEventIds) }
            };
        ValidateRecords(
            records,
            eventIds,
            CreateIdSet(knownOutfitIds),
            new Dictionary<string, HashSet<string>>(StringComparer.Ordinal),
            report);
        report.HeroineCount = records.Count > 0 ? 1 : 0;
        return report;
    }

    private static List<EndingRecord> LoadEndingRecords(EndingDataValidationReport report)
    {
        List<EndingRecord> records = new List<EndingRecord>();
        string[] guids = AssetDatabase.FindAssets(
            "t:" + nameof(EndingData),
            new[] { HeroineAssetRoot });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            EndingData ending = AssetDatabase.LoadAssetAtPath<EndingData>(path);
            string heroineId = GetHeroineId(path);
            if (ending == null || string.IsNullOrWhiteSpace(heroineId))
            {
                report.SkippedAssetCount++;
                report.Warn("エンディングアセットを読み込めないためスキップしました: " + path, ending);
                continue;
            }

            records.Add(new EndingRecord
            {
                HeroineId = heroineId,
                Ending = ending
            });
        }
        return records;
    }

    private static void ValidateRecords(
        List<EndingRecord> records,
        Dictionary<string, HashSet<string>> eventIdsByHeroine,
        HashSet<string> outfitIds,
        Dictionary<string, HashSet<string>> expressionIdsByHeroine,
        EndingDataValidationReport report)
    {
        foreach (IGrouping<string, EndingRecord> heroineGroup in
            records.GroupBy(record => record.HeroineId, StringComparer.Ordinal))
        {
            Dictionary<string, EndingData> endingsById =
                new Dictionary<string, EndingData>(StringComparer.Ordinal);
            List<EndingData> endings = heroineGroup.Select(record => record.Ending).ToList();

            HashSet<string> knownEventIds;
            eventIdsByHeroine.TryGetValue(heroineGroup.Key, out knownEventIds);
            knownEventIds = knownEventIds ?? new HashSet<string>(StringComparer.Ordinal);
            HashSet<string> expressionIds;
            expressionIdsByHeroine.TryGetValue(heroineGroup.Key, out expressionIds);
            expressionIds = expressionIds ?? new HashSet<string>(StringComparer.Ordinal);

            foreach (EndingData ending in endings)
            {
                report.EndingCount++;
                string label = "heroine=" + heroineGroup.Key + " / ending=" + ending.endingId;
                ValidateEnding(ending, label, knownEventIds, outfitIds, expressionIds, report);

                if (!string.IsNullOrWhiteSpace(ending.endingId))
                {
                    EndingData duplicate;
                    if (endingsById.TryGetValue(ending.endingId, out duplicate))
                    {
                        report.Warn(
                            "endingId が同じヒロイン内で重複しています: heroine=" +
                            heroineGroup.Key + " / endingId=" + ending.endingId,
                            ending);
                    }
                    else
                    {
                        endingsById.Add(ending.endingId, ending);
                    }
                }
            }

            if (!endings.Any(IsFallbackEnding))
            {
                report.Warn(
                    "heroine=" + heroineGroup.Key +
                    " に衣装・イベント条件なしのフォールバックエンディングがありません。",
                    endings.FirstOrDefault());
            }

            ValidateAmbiguousSelections(heroineGroup.Key, endings, report);
        }
    }

    private static void ValidateEnding(
        EndingData ending,
        string label,
        HashSet<string> knownEventIds,
        HashSet<string> outfitIds,
        HashSet<string> expressionIds,
        EndingDataValidationReport report)
    {
        if (string.IsNullOrWhiteSpace(ending.endingId))
        {
            report.Warn(label + " の endingId が空です。", ending);
        }
        else if (!ValidIdPattern.IsMatch(ending.endingId))
        {
            report.Warn(
                label + " の endingId に使用できない文字があります: " + ending.endingId,
                ending);
        }

        if (string.IsNullOrWhiteSpace(ending.displayName))
        {
            report.Warn(label + " の displayName が空です。", ending);
        }
        bool hasPages = ending.pages != null && ending.pages.Count > 0;
        if (!hasPages && string.IsNullOrWhiteSpace(ending.message))
        {
            report.Warn(label + " の message が空です。", ending);
        }
        if (ending.requiredAffection < 0 ||
            ending.requiredAffection > AffectionDataValidator.MaximumAffection)
        {
            report.Warn(
                label + " の requiredAffection が範囲外です: " + ending.requiredAffection,
                ending);
        }

        if (!string.IsNullOrWhiteSpace(ending.costumeId) &&
            !outfitIds.Contains(ending.costumeId))
        {
            report.Warn(label + " の costumeId が存在しません: " + ending.costumeId, ending);
        }

        ValidateEventIds(ending.requiredShownEventIds, knownEventIds, label, ending, report);

        if (hasPages)
        {
            for (int i = 0; i < ending.pages.Count; i++)
            {
                EndingPageData page = ending.pages[i];
                string pageLabel = label + ".pages[" + i + "]";
                if (page == null)
                {
                    report.Warn(pageLabel + " がnullです。", ending);
                    continue;
                }
                if (string.IsNullOrWhiteSpace(page.message))
                {
                    report.Warn(pageLabel + " の message が空です。", ending);
                }
                if (!string.IsNullOrWhiteSpace(page.expressionId) &&
                    expressionIds.Count > 0 &&
                    !expressionIds.Contains(page.expressionId))
                {
                    report.Warn(
                        pageLabel + " の expressionId が表情レイヤーに存在しません: " +
                        page.expressionId,
                        ending);
                }
                if (!string.IsNullOrWhiteSpace(page.stillId) && page.stillSprite == null)
                {
                    report.Warn(pageLabel + " はstillIdがありますがstillSpriteが未設定です。", ending);
                }
                if (string.IsNullOrWhiteSpace(page.stillId) && page.stillSprite != null)
                {
                    report.Warn(pageLabel + " はstillSpriteがありますがstillIdが空です。", ending);
                }
            }
        }
    }

    private static void ValidateEventIds(
        IEnumerable<string> values,
        HashSet<string> knownIds,
        string label,
        UnityEngine.Object context,
        EndingDataValidationReport report)
    {
        if (values == null) return;
        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (string value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                report.Warn(label + " の requiredShownEventIds に空のIDがあります。", context);
            }
            else
            {
                if (!seen.Add(value))
                {
                    report.Warn(label + " の requiredShownEventIds に重複IDがあります: " + value, context);
                }
                if (!knownIds.Contains(value))
                {
                    report.Warn(label + " の requiredShownEventIds に存在しないIDがあります: " + value, context);
                }
            }
        }
    }

    private static void ValidateAmbiguousSelections(
        string heroineId,
        List<EndingData> endings,
        EndingDataValidationReport report)
    {
        for (int i = 0; i < endings.Count; i++)
        {
            for (int j = i + 1; j < endings.Count; j++)
            {
                EndingData first = endings[i];
                EndingData second = endings[j];
                if (first.requiredAffection == second.requiredAffection &&
                    string.Equals(first.costumeId ?? string.Empty, second.costumeId ?? string.Empty, StringComparison.Ordinal))
                {
                    report.Warn(
                        "同じ必要好感度・衣装条件のエンディングは同時成立して選択が不定になる可能性があります: " +
                        "heroine=" + heroineId + " / " + first.endingId + " / " + second.endingId,
                        second);
                }
            }
        }
    }

    private static bool IsFallbackEnding(EndingData ending)
    {
        return ending != null &&
            string.IsNullOrWhiteSpace(ending.costumeId) &&
            (ending.requiredShownEventIds == null || ending.requiredShownEventIds.Length == 0);
    }

    private static Dictionary<string, HashSet<string>> LoadEventIdsByHeroine()
    {
        Dictionary<string, HashSet<string>> result =
            new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        string[] guids = AssetDatabase.FindAssets(
            "t:" + nameof(GameEventData),
            new[] { HeroineAssetRoot });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameEventData gameEvent = AssetDatabase.LoadAssetAtPath<GameEventData>(path);
            string heroineId = GetHeroineId(path);
            if (gameEvent == null || string.IsNullOrWhiteSpace(gameEvent.eventId) ||
                string.IsNullOrWhiteSpace(heroineId))
            {
                continue;
            }

            HashSet<string> ids;
            if (!result.TryGetValue(heroineId, out ids))
            {
                ids = new HashSet<string>(StringComparer.Ordinal);
                result.Add(heroineId, ids);
            }
            ids.Add(gameEvent.eventId);
        }
        return result;
    }

    private static HashSet<string> LoadOutfitIds()
    {
        return new HashSet<string>(
            Resources.LoadAll<OutfitData>("Outfits")
                .Where(outfit => outfit != null && !string.IsNullOrWhiteSpace(outfit.outfitId))
                .Select(outfit => outfit.outfitId),
            StringComparer.Ordinal);
    }

    private static Dictionary<string, HashSet<string>> LoadExpressionIdsByHeroine()
    {
        Dictionary<string, HashSet<string>> result =
            new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        foreach (HeroineLayeredSpriteData data in AssetDatabase.FindAssets(
            "t:" + nameof(HeroineLayeredSpriteData),
            new[] { HeroineAssetRoot })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<HeroineLayeredSpriteData>)
            .Where(data => data != null && !string.IsNullOrWhiteSpace(data.heroineId)))
        {
            result[data.heroineId] = new HashSet<string>(
                data.expressionLayers == null
                    ? new string[0]
                    : data.expressionLayers
                        .Where(layer => layer != null && !string.IsNullOrWhiteSpace(layer.expressionId))
                        .Select(layer => layer.expressionId),
                StringComparer.Ordinal);
        }
        return result;
    }

    private static HashSet<string> CreateIdSet(IEnumerable<string> values)
    {
        return new HashSet<string>(
            values == null ? new string[0] : values.Where(value => !string.IsNullOrWhiteSpace(value)),
            StringComparer.Ordinal);
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
