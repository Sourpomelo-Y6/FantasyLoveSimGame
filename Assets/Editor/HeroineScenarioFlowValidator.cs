using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>選択中ヒロインのシナリオ導線と到達条件を一覧・検証する。</summary>
public static class HeroineScenarioFlowValidator
{
    private const string MenuPath = "FantasyLoveSim/Validation/Data/Heroine Scenario Flow";

    [MenuItem(MenuPath)]
    public static void ValidateSelectedHeroine()
    {
        HeroineProfileData profile = ResolveProfile();
        if (profile == null)
        {
            Debug.LogWarning("HeroineProfileData が選択されていないため、シナリオ導線検証を中止しました。");
            return;
        }

        HeroineScenarioFlowReport report = Validate(profile);
        report.Log();
        EditorUtility.DisplayDialog("Heroine Scenario Flow", report.CreateDialogMessage(), "OK");
    }

    public static HeroineScenarioFlowReport Validate(HeroineProfileData profile)
    {
        if (profile == null) throw new ArgumentNullException(nameof(profile));
        HeroineScenarioFlowReport report = new HeroineScenarioFlowReport(profile);

        GameEventData[] gameEvents = Load<GameEventData>(profile.gameEventResourcePath);
        ConversationData[] conversations = Load<ConversationData>(profile.conversationResourcePath);
        ActionData[] actions = Load<ActionData>(profile.actionResourcePath);
        ScheduledEventData[] schedules = Load<ScheduledEventData>(profile.scheduledEventResourcePath);
        EndingData[] endings = Load<EndingData>(profile.endingResourcePath);

        HashSet<string> eventIds = new HashSet<string>(
            gameEvents.Where(x => x != null && !string.IsNullOrWhiteSpace(x.eventId))
                .Select(x => x.eventId.Trim()), StringComparer.Ordinal);
        HashSet<string> skillIds = new HashSet<string>(
            Resources.LoadAll<SkillData>("Skills").Where(x => x != null && !string.IsNullOrWhiteSpace(x.skillId))
                .Select(x => x.skillId.Trim()), StringComparer.Ordinal);

        ValidateProfileText(profile, report);
        foreach (GameEventData item in gameEvents.Where(x => x != null)
            .OrderBy(x => TriggerOrder(x.triggerType)).ThenBy(x => x.sortOrder).ThenBy(x => x.eventId))
        {
            string id = Label(item.eventId, item.name);
            report.AddFlow("GameEvent", id,
                $"trigger={item.triggerType}/{EmptyAsAny(item.triggerContextId)}, once={item.showOnce}, " +
                $"day={Range(item.minDay, item.maxDay)}, affection={Range(item.minAffection, item.maxAffection)}");
            ValidateRange(item.minDay, item.maxDay, "day", id, report);
            ValidateRange(item.minAffection, item.maxAffection, "affection", id, report);
            ValidateReferences(item.requiredShownEventIds, eventIds, id, "requiredShownEventIds", report);
            ValidateReferences(item.blockedShownEventIds, eventIds, id, "blockedShownEventIds", report);
            ValidateReferences(item.requiredSkillIds, skillIds, id, "requiredSkillIds", report);
            WarnIntersection(item.requiredShownEventIds, item.blockedShownEventIds, id, report);
            Scan(item, "GameEvent " + id, report);
        }

        foreach (ConversationData data in conversations.Where(x => x != null).OrderBy(x => x.name))
        {
            foreach (ConversationDataItem item in EnumerateConversationItems(data).OrderBy(x => x.conversationId))
            {
                string id = Label(item.conversationId, data.name);
                report.AddFlow("Conversation", id,
                    $"genre={item.genre}, once={item.showOnce}, priority={item.priority}, affection={Range(item.minAffection, item.maxAffection)}");
                ValidateRange(item.minAffection, item.maxAffection, "affection", id, report);
            }
            Scan(data, "Conversation " + data.name, report);
        }

        foreach (ActionData action in actions.Where(x => x != null).OrderBy(x => x.sortOrder).ThenBy(x => x.actionId))
        {
            string actionId = Label(action.actionId, action.name);
            report.AddFlow("Action", actionId,
                $"enabled={action.isEnabled}, affection={Range(action.minAffection, action.maxAffection)}, reactions={action.reactions?.Count ?? 0}");
            ValidateRange(action.minAffection, action.maxAffection, "affection", actionId, report);
            foreach (ActionReactionData reaction in action.reactions ?? new List<ActionReactionData>())
            {
                if (reaction == null) continue;
                string id = actionId + "/" + Label(reaction.reactionId, "Reaction");
                ValidateRange(reaction.minAffection, reaction.maxAffection, "affection", id, report);
                ValidateReferences(reaction.requiredShownEventIds, eventIds, id, "requiredShownEventIds", report);
                ValidateReferences(reaction.requiredSkillIds, skillIds, id, "requiredSkillIds", report);
            }
            Scan(action, "Action " + actionId, report);
        }

        foreach (ScheduledEventData item in schedules.Where(x => x != null)
            .OrderBy(x => x.scheduleType).ThenBy(x => x.actionId))
        {
            report.AddFlow("Schedule", item.name,
                $"type={item.scheduleType}, action={EmptyAsAny(item.actionId)}, time={item.triggerTimeSlot}");
            Scan(item, "Schedule " + item.name, report);
        }

        foreach (EndingData item in endings.Where(x => x != null)
            .OrderBy(x => x.requiredAffection).ThenBy(x => x.endingId))
        {
            string id = Label(item.endingId, item.name);
            report.AddFlow("Ending", id,
                $"affection>={item.requiredAffection}, requiredEvents={Join(item.requiredShownEventIds)}");
            ValidateReferences(item.requiredShownEventIds, eventIds, id, "requiredShownEventIds", report);
            Scan(item, "Ending " + id, report);
        }

        if (!gameEvents.Any(x => x != null && x.isEnabled && x.triggerType == GameEventTriggerType.GameStart))
            report.Warn("ゲーム開始導線となる有効なGameStartイベントがありません。");
        if (endings.Length == 0) report.Warn("エンディングがありません。");
        return report;
    }

    private static void ValidateProfileText(HeroineProfileData profile, HeroineScenarioFlowReport report)
    {
        Scan(profile, "Profile", report);
    }

    private static void ValidateReferences(IEnumerable<string> values, HashSet<string> known, string owner,
        string field, HeroineScenarioFlowReport report)
    {
        foreach (string value in values ?? Enumerable.Empty<string>())
            if (!string.IsNullOrWhiteSpace(value) && !known.Contains(value.Trim()))
                report.Warn($"{owner}: {field} の参照先がありません: {value.Trim()}");
    }

    private static void WarnIntersection(IEnumerable<string> required, IEnumerable<string> blocked,
        string owner, HeroineScenarioFlowReport report)
    {
        HashSet<string> blockedIds = new HashSet<string>(blocked ?? Enumerable.Empty<string>(), StringComparer.Ordinal);
        foreach (string id in (required ?? Enumerable.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x)))
            if (blockedIds.Contains(id)) report.Warn($"{owner}: 同じイベントが必須条件と阻害条件の両方にあります: {id}");
    }

    private static void ValidateRange(int min, int max, string field, string owner, HeroineScenarioFlowReport report)
    {
        if (max > 0 && min > max) report.Warn($"{owner}: {field}条件が逆転しています: {min}..{max}");
    }

    private static void Scan(UnityEngine.Object asset, string owner, HeroineScenarioFlowReport report)
    {
        // TestHeroine自身では正しいID・Resource Pathなので混入扱いにしない。
        if (!report.ShouldDetectTestHeroine) return;
        SerializedObject serialized = new SerializedObject(asset);
        SerializedProperty property = serialized.GetIterator();
        bool enterChildren = true;
        while (property.NextVisible(enterChildren))
        {
            enterChildren = true;
            if (property.propertyType != SerializedPropertyType.String || property.propertyPath.StartsWith("m_", StringComparison.Ordinal))
                continue;
            string value = property.stringValue;
            if (!string.IsNullOrEmpty(value) && value.IndexOf("TestHeroine", StringComparison.OrdinalIgnoreCase) >= 0)
                report.Warn($"{owner}: TestHeroine文字列が残っています: {property.propertyPath}");
        }
    }

    private static IEnumerable<ConversationDataItem> EnumerateConversationItems(ConversationData data)
    {
        if (data.items != null && data.items.Count > 0) return data.items.Where(x => x != null);
        return new[] { new ConversationDataItem
        {
            conversationId = data.conversationId, genre = data.genre, type = data.type,
            heroineLine = data.heroineLine, lines = data.lines, choices = data.choices,
            priority = data.priority, showOnce = data.showOnce, minAffection = data.minAffection,
            maxAffection = data.maxAffection, costumeId = data.costumeId
        }};
    }

    private static T[] Load<T>(string path) where T : UnityEngine.Object =>
        Resources.LoadAll<T>(path ?? string.Empty);
    private static int TriggerOrder(GameEventTriggerType trigger) => trigger == GameEventTriggerType.GameStart ? 0 : (int)trigger + 1;
    private static string Label(string id, string fallback) => string.IsNullOrWhiteSpace(id) ? fallback : id.Trim();
    private static string EmptyAsAny(string value) => string.IsNullOrWhiteSpace(value) ? "*" : value.Trim();
    private static string Range(int min, int max) => max <= 0 ? min + "..*" : min + ".." + max;
    private static string Join(IEnumerable<string> values) => string.Join(",", values ?? Enumerable.Empty<string>());

    private static HeroineProfileData ResolveProfile()
    {
        HeroineProfileData selected = Selection.activeObject as HeroineProfileData;
        if (selected != null) return selected;
        string path = EditorUtility.OpenFilePanel("Select HeroineProfileData",
            Path.Combine(Application.dataPath, "Resources", "Heroines"), "asset");
        if (string.IsNullOrEmpty(path)) return null;
        string root = Directory.GetParent(Application.dataPath).FullName.Replace("\\", "/") + "/";
        string normalized = path.Replace("\\", "/");
        return normalized.StartsWith(root, StringComparison.Ordinal)
            ? AssetDatabase.LoadAssetAtPath<HeroineProfileData>(normalized.Substring(root.Length))
            : null;
    }
}

public sealed class HeroineScenarioFlowReport
{
    private readonly List<string> flow = new List<string>();
    private readonly List<string> warnings = new List<string>();
    public HeroineScenarioFlowReport(HeroineProfileData profile) { Profile = profile; }
    public HeroineProfileData Profile { get; }
    public bool ShouldDetectTestHeroine =>
        !string.Equals(Profile.heroineId, "TestHeroine", StringComparison.OrdinalIgnoreCase);
    public IReadOnlyList<string> Flow => flow;
    public IReadOnlyList<string> Warnings => warnings;
    public bool IsValid => warnings.Count == 0;
    public void AddFlow(string category, string id, string conditions) => flow.Add($"[{category}] {id} | {conditions}");
    public void Warn(string message) => warnings.Add(message);
    public string CreateDialogMessage() =>
        $"Heroine: {Profile.heroineId}\n導線項目: {flow.Count}\n警告: {warnings.Count}\n\n詳細はConsoleを確認してください。";
    public void Log()
    {
        string summary = $"[HeroineScenarioFlow] heroine={Profile.heroineId} / entries={flow.Count} / warnings={warnings.Count}";
        if (IsValid) Debug.Log(summary, Profile); else Debug.LogWarning(summary, Profile);
        foreach (string item in flow) Debug.Log("[HeroineScenarioFlow] " + item, Profile);
        foreach (string warning in warnings) Debug.LogWarning("[HeroineScenarioFlow] " + warning, Profile);
    }
}
