using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed class AffectionDataValidationEntry
{
    public string Message { get; private set; }
    public UnityEngine.Object Context { get; private set; }

    public AffectionDataValidationEntry(string message, UnityEngine.Object context)
    {
        Message = message;
        Context = context;
    }
}

public sealed class AffectionDataValidationReport
{
    private readonly List<AffectionDataValidationEntry> warnings =
        new List<AffectionDataValidationEntry>();

    public int HeroineCount { get; internal set; }
    public int ConversationCount { get; internal set; }
    public int ActionCount { get; internal set; }
    public int GameEventCount { get; internal set; }
    public int ScheduledEventCount { get; internal set; }
    public int EndingCount { get; internal set; }
    public int SkippedAssetCount { get; internal set; }
    public int WarningCount => warnings.Count;
    public bool IsValid => WarningCount == 0;
    public IReadOnlyList<AffectionDataValidationEntry> Warnings => warnings;

    internal void Warn(string message, UnityEngine.Object context)
    {
        warnings.Add(new AffectionDataValidationEntry(message, context));
    }

    public string CreateSummary()
    {
        return
            "Affection data validation: heroines=" + HeroineCount +
            " / conversations=" + ConversationCount +
            " / actions=" + ActionCount +
            " / game events=" + GameEventCount +
            " / scheduled events=" + ScheduledEventCount +
            " / endings=" + EndingCount +
            " / skipped=" + SkippedAssetCount +
            " / warnings=" + WarningCount;
    }

    public void Log()
    {
        string summary = "[AffectionDataValidation] " + CreateSummary();
        if (IsValid)
        {
            Debug.Log(summary);
        }
        else
        {
            Debug.LogWarning(summary);
        }

        foreach (AffectionDataValidationEntry warning in warnings)
        {
            Debug.LogWarning("[AffectionDataValidation] " + warning.Message, warning.Context);
        }
    }
}

public static class AffectionDataValidator
{
    internal const int MaximumAffection = 9999;
    private const int LegacyMaximumAffection = 100;
    private const int SuspiciousSmallChangeMaximum = 5;
    private const string HeroineAssetRoot = "Assets/Resources/Heroines";

    public static AffectionDataValidationReport ValidateProjectAssets()
    {
        AffectionDataValidationReport report = new AffectionDataValidationReport();
        HashSet<string> heroineIds = new HashSet<string>(StringComparer.Ordinal);

        ValidateAssets<ConversationData>(
            report,
            heroineIds,
            asset =>
            {
                report.ConversationCount++;
                ValidateConversation(asset, GetAssetLabel(asset), report);
            });
        ValidateAssets<ActionData>(
            report,
            heroineIds,
            asset =>
            {
                report.ActionCount++;
                ValidateAction(asset, GetAssetLabel(asset), report);
            });
        ValidateAssets<GameEventData>(
            report,
            heroineIds,
            asset =>
            {
                report.GameEventCount++;
                ValidateGameEvent(asset, GetAssetLabel(asset), report);
            });
        ValidateAssets<ScheduledEventData>(
            report,
            heroineIds,
            asset =>
            {
                report.ScheduledEventCount++;
                ValidateScheduledEvent(asset, GetAssetLabel(asset), report);
            });
        ValidateAssets<EndingData>(
            report,
            heroineIds,
            asset =>
            {
                report.EndingCount++;
                ValidateEnding(asset, GetAssetLabel(asset), report);
            });

        report.HeroineCount = heroineIds.Count;
        return report;
    }

    public static AffectionDataValidationReport Validate(
        ConversationData[] conversations,
        ActionData[] actions,
        GameEventData[] gameEvents,
        ScheduledEventData[] scheduledEvents,
        EndingData[] endings)
    {
        AffectionDataValidationReport report = new AffectionDataValidationReport();

        foreach (ConversationData conversation in conversations ?? new ConversationData[0])
        {
            if (conversation == null) continue;
            report.ConversationCount++;
            ValidateConversation(conversation, conversation.name, report);
        }
        foreach (ActionData action in actions ?? new ActionData[0])
        {
            if (action == null) continue;
            report.ActionCount++;
            ValidateAction(action, action.name, report);
        }
        foreach (GameEventData gameEvent in gameEvents ?? new GameEventData[0])
        {
            if (gameEvent == null) continue;
            report.GameEventCount++;
            ValidateGameEvent(gameEvent, gameEvent.name, report);
        }
        foreach (ScheduledEventData scheduledEvent in scheduledEvents ?? new ScheduledEventData[0])
        {
            if (scheduledEvent == null) continue;
            report.ScheduledEventCount++;
            ValidateScheduledEvent(scheduledEvent, scheduledEvent.name, report);
        }
        foreach (EndingData ending in endings ?? new EndingData[0])
        {
            if (ending == null) continue;
            report.EndingCount++;
            ValidateEnding(ending, ending.name, report);
        }

        return report;
    }

    private static void ValidateAssets<T>(
        AffectionDataValidationReport report,
        HashSet<string> heroineIds,
        Action<T> validate)
        where T : UnityEngine.Object
    {
        string[] guids = AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { HeroineAssetRoot });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                report.SkippedAssetCount++;
                report.Warn("アセットを読み込めないためスキップしました: " + path, null);
                continue;
            }

            string heroineId = GetHeroineId(path);
            if (!string.IsNullOrWhiteSpace(heroineId))
            {
                heroineIds.Add(heroineId);
            }

            try
            {
                validate(asset);
            }
            catch (Exception ex)
            {
                report.SkippedAssetCount++;
                report.Warn(
                    "検証中に例外が発生したためスキップしました: " + path + " / " + ex.Message,
                    asset);
            }
        }
    }

    private static void ValidateConversation(
        ConversationData conversation,
        string label,
        AffectionDataValidationReport report)
    {
        ValidateRange(
            label,
            conversation.minAffection,
            conversation.maxAffection,
            false,
            conversation,
            report);
        ValidateChoices(label, conversation.choices, conversation, report);

        if (conversation.items == null) return;
        foreach (ConversationDataItem item in conversation.items)
        {
            if (item == null) continue;
            string itemLabel = label + " / item=" + item.conversationId;
            ValidateRange(itemLabel, item.minAffection, item.maxAffection, false, conversation, report);
            ValidateChoices(itemLabel, item.choices, conversation, report);
        }
    }

    private static void ValidateChoices(
        string label,
        List<ConversationChoice> choices,
        UnityEngine.Object context,
        AffectionDataValidationReport report)
    {
        if (choices == null) return;
        for (int i = 0; i < choices.Count; i++)
        {
            ConversationChoice choice = choices[i];
            if (choice == null) continue;
            ValidateChange(label + " / choice[" + i + "]", choice.affectionChange, context, report);
        }
    }

    private static void ValidateAction(
        ActionData action,
        string label,
        AffectionDataValidationReport report)
    {
        ValidateRange(label, action.minAffection, action.maxAffection, false, action, report);
        ValidateChange(label + " / default effect", action.affectionChange, action, report);
        if (action.reactions == null) return;

        for (int i = 0; i < action.reactions.Count; i++)
        {
            ActionReactionData reaction = action.reactions[i];
            if (reaction == null) continue;
            string reactionLabel = label + " / reaction=" + reaction.reactionId;
            ValidateRange(
                reactionLabel,
                reaction.minAffection,
                reaction.maxAffection,
                false,
                action,
                report);
            ValidateChange(reactionLabel, reaction.affectionChange, action, report);
        }
    }

    private static void ValidateGameEvent(
        GameEventData gameEvent,
        string label,
        AffectionDataValidationReport report)
    {
        ValidateRange(label, gameEvent.minAffection, gameEvent.maxAffection, true, gameEvent, report);
    }

    private static void ValidateScheduledEvent(
        ScheduledEventData scheduledEvent,
        string label,
        AffectionDataValidationReport report)
    {
        ValidateChange(label, scheduledEvent.affectionChange, scheduledEvent, report);
    }

    private static void ValidateEnding(
        EndingData ending,
        string label,
        AffectionDataValidationReport report)
    {
        int value = ending.requiredAffection;
        if (value < 0 || value > MaximumAffection)
        {
            report.Warn(label + " の requiredAffection が範囲外です: " + value, ending);
        }
        else if (value > 0 && value <= LegacyMaximumAffection)
        {
            report.Warn(
                label + " の requiredAffection は旧尺度の可能性があります: " + value,
                ending);
        }
    }

    private static void ValidateRange(
        string label,
        int minimum,
        int maximum,
        bool zeroMaximumMeansUnlimited,
        UnityEngine.Object context,
        AffectionDataValidationReport report)
    {
        if (minimum < 0 || minimum > MaximumAffection)
        {
            report.Warn(label + " の minAffection が範囲外です: " + minimum, context);
        }

        if (maximum < 0 || maximum > MaximumAffection)
        {
            report.Warn(label + " の maxAffection が範囲外です: " + maximum, context);
            return;
        }

        if (maximum == 0 && zeroMaximumMeansUnlimited)
        {
            return;
        }

        if (maximum == 0)
        {
            report.Warn(label + " の maxAffection が0です。通常は9999を指定してください。", context);
            return;
        }

        if (minimum > maximum)
        {
            report.Warn(
                label + " の好感度範囲が逆転しています: " + minimum + " > " + maximum,
                context);
        }

        if (maximum == LegacyMaximumAffection)
        {
            report.Warn(
                label + " の maxAffection=100 は旧尺度の可能性があります。通常上限は9999です。",
                context);
        }
    }

    private static void ValidateChange(
        string label,
        int value,
        UnityEngine.Object context,
        AffectionDataValidationReport report)
    {
        if (value < -MaximumAffection || value > MaximumAffection)
        {
            report.Warn(label + " の affectionChange が範囲外です: " + value, context);
            return;
        }

        int absoluteValue = Math.Abs(value);
        if (absoluteValue > 0 && absoluteValue <= SuspiciousSmallChangeMaximum)
        {
            report.Warn(
                label + " の affectionChange=" + value +
                " は旧尺度の可能性があります。通常行動・会話では10倍移行を確認してください。",
                context);
        }
    }

    private static string GetAssetLabel(UnityEngine.Object asset)
    {
        string path = AssetDatabase.GetAssetPath(asset);
        string heroineId = GetHeroineId(path);
        return string.IsNullOrWhiteSpace(heroineId)
            ? path
            : "heroine=" + heroineId + " / asset=" + asset.name;
    }

    private static string GetHeroineId(string assetPath)
    {
        string prefix = HeroineAssetRoot + "/";
        if (string.IsNullOrWhiteSpace(assetPath) || !assetPath.StartsWith(prefix, StringComparison.Ordinal))
        {
            return string.Empty;
        }

        string relativePath = assetPath.Substring(prefix.Length);
        int separatorIndex = relativePath.IndexOf('/');
        return separatorIndex > 0 ? relativePath.Substring(0, separatorIndex) : string.Empty;
    }
}
