using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public sealed class ActionReactionValidationEntry
{
    public string Message { get; private set; }
    public UnityEngine.Object Context { get; private set; }

    public ActionReactionValidationEntry(string message, UnityEngine.Object context)
    {
        Message = message;
        Context = context;
    }
}

public sealed class ActionReactionValidationReport
{
    private readonly List<ActionReactionValidationEntry> warnings =
        new List<ActionReactionValidationEntry>();

    public int HeroineCount { get; internal set; }
    public int ActionCount { get; internal set; }
    public int ReactionCount { get; internal set; }
    public int SkippedAssetCount { get; internal set; }
    public int WarningCount => warnings.Count;
    public bool IsValid => WarningCount == 0;
    public IReadOnlyList<ActionReactionValidationEntry> Warnings => warnings;

    internal void Warn(string message, UnityEngine.Object context)
    {
        warnings.Add(new ActionReactionValidationEntry(message, context));
    }

    public string CreateSummary()
    {
        return
            "Action reaction validation: heroines=" + HeroineCount +
            " / actions=" + ActionCount +
            " / reactions=" + ReactionCount +
            " / skipped=" + SkippedAssetCount +
            " / warnings=" + WarningCount;
    }

    public void Log()
    {
        string summary = "[ActionReactionValidation] " + CreateSummary();
        if (IsValid)
        {
            Debug.Log(summary);
        }
        else
        {
            Debug.LogWarning(summary);
        }

        foreach (ActionReactionValidationEntry warning in warnings)
        {
            Debug.LogWarning("[ActionReactionValidation] " + warning.Message, warning.Context);
        }
    }
}

public static class ActionReactionDataValidator
{
    private const string HeroineAssetRoot = "Assets/Resources/Heroines";
    private static readonly Regex ValidIdPattern =
        new Regex("^[A-Za-z][A-Za-z0-9_]*$", RegexOptions.CultureInvariant);

    private sealed class ActionRecord
    {
        public string HeroineId;
        public string AssetPath;
        public ActionData Action;
    }

    public static ActionReactionValidationReport ValidateProjectAssets()
    {
        ActionReactionValidationReport report = new ActionReactionValidationReport();
        List<ActionRecord> records = LoadActionRecords(report);
        HashSet<string> skillIds = LoadSkillIds();
        Dictionary<string, HashSet<string>> eventIdsByHeroine = LoadEventIdsByHeroine();
        Dictionary<string, HashSet<string>> conversationIdsByHeroine = LoadConversationIdsByHeroine();
        ValidateRecords(records, skillIds, eventIdsByHeroine, conversationIdsByHeroine, report);
        report.HeroineCount = records
            .Select(record => record.HeroineId)
            .Distinct(StringComparer.Ordinal)
            .Count();
        return report;
    }

    internal static ActionReactionValidationReport ValidateForTests(
        string heroineId,
        IEnumerable<string> knownSkillIds,
        IEnumerable<string> knownEventIds,
        IEnumerable<string> knownConversationIds,
        params ActionData[] actions)
    {
        ActionReactionValidationReport report = new ActionReactionValidationReport();
        List<ActionRecord> records = new List<ActionRecord>();
        foreach (ActionData action in actions ?? new ActionData[0])
        {
            if (action == null) continue;
            records.Add(new ActionRecord
            {
                HeroineId = heroineId,
                AssetPath = action.name + ".asset",
                Action = action
            });
        }

        Dictionary<string, HashSet<string>> eventIds =
            new Dictionary<string, HashSet<string>>(StringComparer.Ordinal)
            {
                { heroineId, CreateIdSet(knownEventIds) }
            };
        Dictionary<string, HashSet<string>> conversationIds =
            new Dictionary<string, HashSet<string>>(StringComparer.Ordinal)
            {
                { heroineId, CreateIdSet(knownConversationIds) }
            };

        ValidateRecords(
            records,
            CreateIdSet(knownSkillIds),
            eventIds,
            conversationIds,
            report);
        report.HeroineCount = records.Count > 0 ? 1 : 0;
        return report;
    }

    private static List<ActionRecord> LoadActionRecords(ActionReactionValidationReport report)
    {
        List<ActionRecord> records = new List<ActionRecord>();
        string[] guids = AssetDatabase.FindAssets(
            "t:" + nameof(ActionData),
            new[] { HeroineAssetRoot });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ActionData action = AssetDatabase.LoadAssetAtPath<ActionData>(path);
            if (action == null)
            {
                report.SkippedAssetCount++;
                report.Warn("行動アセットを読み込めないためスキップしました: " + path, null);
                continue;
            }

            string heroineId = GetHeroineId(path);
            if (string.IsNullOrWhiteSpace(heroineId))
            {
                report.SkippedAssetCount++;
                report.Warn("配置先ヒロインを判定できないためスキップしました: " + path, action);
                continue;
            }

            records.Add(new ActionRecord
            {
                HeroineId = heroineId,
                AssetPath = path,
                Action = action
            });
        }
        return records;
    }

    private static void ValidateRecords(
        List<ActionRecord> records,
        HashSet<string> skillIds,
        Dictionary<string, HashSet<string>> eventIdsByHeroine,
        Dictionary<string, HashSet<string>> conversationIdsByHeroine,
        ActionReactionValidationReport report)
    {
        Dictionary<string, ActionRecord> actionsById =
            new Dictionary<string, ActionRecord>(StringComparer.Ordinal);
        Dictionary<string, ActionRecord> reactionsById =
            new Dictionary<string, ActionRecord>(StringComparer.Ordinal);

        foreach (ActionRecord record in records)
        {
            ActionData action = record.Action;
            report.ActionCount++;
            string actionLabel = CreateActionLabel(record);
            ValidateId(action.actionId, actionLabel + " の actionId", action, report);

            if (!string.IsNullOrWhiteSpace(action.actionId))
            {
                string actionKey = record.HeroineId + "|" + action.actionId;
                ActionRecord duplicateAction;
                if (actionsById.TryGetValue(actionKey, out duplicateAction))
                {
                    report.Warn(
                        "actionId が同じヒロイン内で重複しています: heroine=" +
                        record.HeroineId + " / actionId=" + action.actionId,
                        action);
                }
                else
                {
                    actionsById.Add(actionKey, record);
                }
            }

            List<ActionReactionData> reactions = action.reactions ?? new List<ActionReactionData>();
            if (reactions.Count > 0 && action.executionType != ActionExecutionType.SimpleAction)
            {
                report.Warn(
                    actionLabel + " はSimpleActionではないため、設定されたreactionsは現在の処理で使用されません。",
                    action);
            }

            Dictionary<string, ActionReactionData> conditions =
                new Dictionary<string, ActionReactionData>(StringComparer.Ordinal);
            for (int i = 0; i < reactions.Count; i++)
            {
                ActionReactionData reaction = reactions[i];
                if (reaction == null)
                {
                    report.Warn(actionLabel + ".reactions[" + i + "] がnullです。", action);
                    continue;
                }

                report.ReactionCount++;
                string reactionLabel = actionLabel + " / reaction=" + reaction.reactionId;
                ValidateReaction(
                    record.HeroineId,
                    reaction,
                    reactionLabel,
                    skillIds,
                    eventIdsByHeroine,
                    conversationIdsByHeroine,
                    action,
                    report);

                if (!string.IsNullOrWhiteSpace(reaction.reactionId))
                {
                    string reactionKey = record.HeroineId + "|" + reaction.reactionId;
                    ActionRecord duplicateReaction;
                    if (reactionsById.TryGetValue(reactionKey, out duplicateReaction))
                    {
                        report.Warn(
                            "reactionId が同じヒロイン内で重複しています: heroine=" +
                            record.HeroineId + " / reactionId=" + reaction.reactionId,
                            action);
                    }
                    else
                    {
                        reactionsById.Add(reactionKey, record);
                    }
                }

                string conditionKey = CreateConditionKey(reaction);
                ActionReactionData conditionMatch;
                if (conditions.TryGetValue(conditionKey, out conditionMatch))
                {
                    report.Warn(
                        actionLabel + " に条件・priorityが同一の反応があります: " +
                        conditionMatch.reactionId + " / " + reaction.reactionId,
                        action);
                }
                else
                {
                    conditions.Add(conditionKey, reaction);
                }
            }

            if (action.executionType == ActionExecutionType.SimpleAction &&
                string.IsNullOrWhiteSpace(action.resultMessage) &&
                !reactions.Any(IsFallbackReaction))
            {
                report.Warn(
                    actionLabel + " に無条件フォールバックがありません。" +
                    "ActionData.resultMessageまたは無条件の繰り返し可能なreactionを設定してください。",
                    action);
            }
        }
    }

    private static void ValidateReaction(
        string heroineId,
        ActionReactionData reaction,
        string label,
        HashSet<string> skillIds,
        Dictionary<string, HashSet<string>> eventIdsByHeroine,
        Dictionary<string, HashSet<string>> conversationIdsByHeroine,
        UnityEngine.Object context,
        ActionReactionValidationReport report)
    {
        ValidateId(reaction.reactionId, label + " の reactionId", context, report);
        if (reaction.showOnce && string.IsNullOrWhiteSpace(reaction.reactionId))
        {
            report.Warn(label + " はshowOnceですがreactionIdが空です。", context);
        }
        if (reaction.minAffection < 0 ||
            reaction.maxAffection > AffectionDataValidator.MaximumAffection ||
            reaction.minAffection > reaction.maxAffection)
        {
            report.Warn(
                label + " の好感度範囲が不正です: " +
                reaction.minAffection + "〜" + reaction.maxAffection,
                context);
        }

        ValidateRequiredIds(
            reaction.requiredSkillIds,
            skillIds,
            label + " / requiredSkillIds",
            context,
            report);

        HashSet<string> eventIds;
        eventIdsByHeroine.TryGetValue(heroineId, out eventIds);
        ValidateRequiredIds(
            reaction.requiredShownEventIds,
            eventIds ?? new HashSet<string>(StringComparer.Ordinal),
            label + " / requiredShownEventIds",
            context,
            report);

        HashSet<string> conversationIds;
        if (!string.IsNullOrWhiteSpace(reaction.reactionId) &&
            conversationIdsByHeroine.TryGetValue(heroineId, out conversationIds) &&
            conversationIds.Contains(reaction.reactionId))
        {
            report.Warn(
                label + " のreactionIdは通常会話IDと重複しています。表示済み管理が競合します。",
                context);
        }
    }

    private static void ValidateRequiredIds(
        IEnumerable<string> values,
        HashSet<string> knownIds,
        string label,
        UnityEngine.Object context,
        ActionReactionValidationReport report)
    {
        if (values == null) return;
        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (string value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                report.Warn(label + " に空のIDがあります。", context);
                continue;
            }
            if (!seen.Add(value))
            {
                report.Warn(label + " に重複IDがあります: " + value, context);
            }
            if (!knownIds.Contains(value))
            {
                report.Warn(label + " に存在しないIDがあります: " + value, context);
            }
        }
    }

    private static void ValidateId(
        string value,
        string label,
        UnityEngine.Object context,
        ActionReactionValidationReport report)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            report.Warn(label + " が空です。", context);
        }
        else if (!ValidIdPattern.IsMatch(value))
        {
            report.Warn(
                label + " に使用できない文字があります: " + value +
                "。英字で開始し、英数字とアンダースコアを使用してください。",
                context);
        }
    }

    private static bool IsFallbackReaction(ActionReactionData reaction)
    {
        return
            reaction != null &&
            !reaction.showOnce &&
            reaction.minAffection == 0 &&
            reaction.maxAffection == AffectionDataValidator.MaximumAffection &&
            string.IsNullOrWhiteSpace(reaction.costumeId) &&
            IsEmpty(reaction.requiredShownEventIds) &&
            IsEmpty(reaction.requiredSkillIds) &&
            reaction.anyTimeSlot &&
            reaction.anyWeather &&
            reaction.anySeason;
    }

    private static string CreateConditionKey(ActionReactionData reaction)
    {
        return string.Join(
            "|",
            reaction.priority,
            reaction.showOnce,
            reaction.minAffection,
            reaction.maxAffection,
            reaction.costumeId ?? string.Empty,
            CreateStringListKey(reaction.requiredShownEventIds),
            CreateStringListKey(reaction.requiredSkillIds),
            reaction.anyTimeSlot,
            CreateEnumListKey(reaction.allowedTimeSlots),
            reaction.anyWeather,
            CreateEnumListKey(reaction.allowedWeathers),
            reaction.anySeason,
            CreateEnumListKey(reaction.allowedSeasons));
    }

    private static bool IsEmpty<T>(ICollection<T> values)
    {
        return values == null || values.Count == 0;
    }

    private static string CreateStringListKey(IEnumerable<string> values)
    {
        return values == null
            ? string.Empty
            : string.Join(",", values.Where(value => value != null).OrderBy(value => value));
    }

    private static string CreateEnumListKey<T>(IEnumerable<T> values)
    {
        return values == null
            ? string.Empty
            : string.Join(",", values.Select(value => Convert.ToInt32(value)).OrderBy(value => value));
    }

    private static HashSet<string> LoadSkillIds()
    {
        return new HashSet<string>(
            Resources.LoadAll<SkillData>("Skills")
                .Where(skill => skill != null && !string.IsNullOrWhiteSpace(skill.skillId))
                .Select(skill => skill.skillId),
            StringComparer.Ordinal);
    }

    private static Dictionary<string, HashSet<string>> LoadEventIdsByHeroine()
    {
        Dictionary<string, HashSet<string>> result =
            new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        foreach (GameEventData gameEvent in LoadAssets<GameEventData>())
        {
            if (gameEvent == null || string.IsNullOrWhiteSpace(gameEvent.eventId)) continue;
            AddOwnedId(result, GetHeroineId(AssetDatabase.GetAssetPath(gameEvent)), gameEvent.eventId);
        }
        return result;
    }

    private static Dictionary<string, HashSet<string>> LoadConversationIdsByHeroine()
    {
        Dictionary<string, HashSet<string>> result =
            new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        foreach (ConversationData conversation in LoadAssets<ConversationData>())
        {
            if (conversation == null) continue;
            string heroineId = GetHeroineId(AssetDatabase.GetAssetPath(conversation));
            if (conversation.items != null && conversation.items.Count > 0)
            {
                foreach (ConversationDataItem item in conversation.items)
                {
                    if (item != null) AddOwnedId(result, heroineId, item.conversationId);
                }
            }
            else
            {
                AddOwnedId(result, heroineId, conversation.conversationId);
            }
        }
        return result;
    }

    private static T[] LoadAssets<T>() where T : UnityEngine.Object
    {
        return AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { HeroineAssetRoot })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<T>)
            .Where(asset => asset != null)
            .ToArray();
    }

    private static void AddOwnedId(
        Dictionary<string, HashSet<string>> idsByHeroine,
        string heroineId,
        string id)
    {
        if (string.IsNullOrWhiteSpace(heroineId) || string.IsNullOrWhiteSpace(id)) return;
        HashSet<string> ids;
        if (!idsByHeroine.TryGetValue(heroineId, out ids))
        {
            ids = new HashSet<string>(StringComparer.Ordinal);
            idsByHeroine.Add(heroineId, ids);
        }
        ids.Add(id);
    }

    private static HashSet<string> CreateIdSet(IEnumerable<string> values)
    {
        return new HashSet<string>(
            values == null ? new string[0] : values.Where(value => !string.IsNullOrWhiteSpace(value)),
            StringComparer.Ordinal);
    }

    private static string CreateActionLabel(ActionRecord record)
    {
        return "heroine=" + record.HeroineId + " / action=" + record.Action.actionId +
            " / asset=" + record.Action.name;
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
