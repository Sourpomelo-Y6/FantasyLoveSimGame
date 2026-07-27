using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public sealed class AudioAssetValidationEntry
{
    public string Category { get; private set; }
    public string LogicalId { get; private set; }
    public string ResourcePath { get; private set; }
    public string SourceLabel { get; private set; }
    public UnityEngine.Object Context { get; private set; }

    public AudioAssetValidationEntry(
        string category,
        string logicalId,
        string resourcePath,
        string sourceLabel = "",
        UnityEngine.Object context = null)
    {
        Category = category;
        LogicalId = logicalId;
        ResourcePath = resourcePath;
        SourceLabel = sourceLabel ?? string.Empty;
        Context = context;
    }
}

public sealed class AudioAssetValidationReport
{
    private readonly List<AudioAssetValidationEntry> missing =
        new List<AudioAssetValidationEntry>();
    private readonly Dictionary<string, int> checkedCounts =
        new Dictionary<string, int>();
    private readonly Dictionary<string, int> foundCounts =
        new Dictionary<string, int>();

    public int CheckedCount => checkedCounts.Values.Sum();
    public int FoundCount => CheckedCount - missing.Count;
    public int MissingCount => missing.Count;
    public bool IsComplete => MissingCount == 0;
    public IReadOnlyList<AudioAssetValidationEntry> Missing => missing;

    internal void Record(AudioAssetValidationEntry entry, bool found)
    {
        Increment(checkedCounts, entry.Category);
        if (found)
        {
            Increment(foundCounts, entry.Category);
        }
        else
        {
            missing.Add(entry);
        }
    }

    public int GetCheckedCount(string category)
    {
        int count;
        return checkedCounts.TryGetValue(category ?? string.Empty, out count)
            ? count
            : 0;
    }

    public int GetFoundCount(string category)
    {
        int count;
        return foundCounts.TryGetValue(category ?? string.Empty, out count)
            ? count
            : 0;
    }

    private static void Increment(Dictionary<string, int> counts, string category)
    {
        string key = category ?? string.Empty;
        int count;
        counts.TryGetValue(key, out count);
        counts[key] = count + 1;
    }

    public string CreateSummary()
    {
        return "Audio asset validation: checked=" + CheckedCount +
            " / found=" + FoundCount +
            " / missing=" + MissingCount +
            " / BGM=" + GetFoundCount("BGM") + "/" + GetCheckedCount("BGM") +
            " / SE=" + GetFoundCount("SE") + "/" + GetCheckedCount("SE") +
            " / VOICE=" + GetFoundCount("VOICE") + "/" + GetCheckedCount("VOICE");
    }

    public void Log()
    {
        string summary = "[AudioAssetValidation] " + CreateSummary();
        if (IsComplete) Debug.Log(summary);
        else Debug.LogWarning(summary);

        foreach (AudioAssetValidationEntry entry in missing)
        {
            string source = string.IsNullOrEmpty(entry.SourceLabel)
                ? string.Empty
                : " / 参照元: " + entry.SourceLabel;
            Debug.LogWarning(
                "[AudioAssetValidation] " + entry.Category + "「" +
                entry.LogicalId + "」が見つかりません。配置先例: Assets/Resources/" +
                entry.ResourcePath + ".ogg" + source,
                entry.Context);
        }
    }
}

public static class AudioAssetValidator
{
    private const string AudioRoot = "Assets/Resources/Audio";

    private static readonly AudioAssetValidationEntry[] Requirements =
    {
        Bgm("Title"),
        Bgm(AudioManager.MainBgmId),
        Bgm("Ending"),
        Bgm(AudioManager.BattleBgmId),
        Bgm(AudioManager.TrainingBgmId),

        Se(UiSePlayer.ConfirmSeId),
        Se(UiSePlayer.CancelSeId),
        Se(UiSePlayer.NextSeId),
        Se("UI/Error"),
        Se("Shop/PurchaseSuccess"),
        Se("Shop/PurchaseFailed"),
        Se("Skill/AcquireSuccess"),
        Se("Skill/AcquireFailed"),
        Se("Schedule/Set"),
        Se("Schedule/Cancel"),
        Se("Training/Step"),
        Se("Training/Complete"),
        Se("Training/Cancel"),
        Se("Battle/Attack"),
        Se("Battle/Defend"),
        Se("Battle/Heal"),
        Se("Battle/Skill"),
        Se("Battle/Item"),
        Se("Battle/Victory"),
        Se("Battle/Defeat"),
        Se("Battle/Escape"),
        Se("Event/Start")
    };

    public static AudioAssetValidationReport ValidateProjectAssets()
    {
        HashSet<string> availableResourcePaths = new HashSet<string>(
            AssetDatabase.FindAssets("t:AudioClip", new[] { AudioRoot })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(ToResourcePath)
                .Where(path => !string.IsNullOrEmpty(path)),
            System.StringComparer.Ordinal);

        List<AudioAssetValidationEntry> requirements =
            new List<AudioAssetValidationEntry>(Requirements);
        requirements.AddRange(CollectProjectVoiceRequirements());
        return Validate(requirements, availableResourcePaths);
    }

    internal static AudioAssetValidationReport Validate(
        IEnumerable<AudioAssetValidationEntry> requirements,
        IEnumerable<string> availableResourcePaths)
    {
        AudioAssetValidationReport report = new AudioAssetValidationReport();
        HashSet<string> available = new HashSet<string>(
            availableResourcePaths ?? Enumerable.Empty<string>(),
            System.StringComparer.Ordinal);

        foreach (AudioAssetValidationEntry requirement in
            requirements ?? Enumerable.Empty<AudioAssetValidationEntry>())
        {
            if (requirement == null || string.IsNullOrEmpty(requirement.ResourcePath))
            {
                continue;
            }

            report.Record(
                requirement,
                available.Contains(requirement.ResourcePath));
        }

        return report;
    }

    internal static string ToResourcePath(string assetPath)
    {
        const string resourcesPrefix = "Assets/Resources/";
        if (string.IsNullOrEmpty(assetPath) ||
            !assetPath.StartsWith(resourcesPrefix, System.StringComparison.Ordinal))
        {
            return string.Empty;
        }

        string withoutExtension = Path.ChangeExtension(assetPath, null);
        return withoutExtension.Substring(resourcesPrefix.Length).Replace('\\', '/');
    }

    internal static AudioAssetValidationEntry Bgm(string id)
    {
        return new AudioAssetValidationEntry(
            "BGM",
            id,
            AudioManager.BuildBgmResourcePath(id));
    }

    internal static AudioAssetValidationEntry Se(string id)
    {
        return new AudioAssetValidationEntry(
            "SE",
            id,
            AudioManager.BuildSeResourcePath(id));
    }

    internal static List<AudioAssetValidationEntry> CollectVoiceRequirements(
        IEnumerable<UnityEngine.Object> assets)
    {
        List<AudioAssetValidationEntry> result =
            new List<AudioAssetValidationEntry>();
        foreach (UnityEngine.Object asset in
            assets ?? Enumerable.Empty<UnityEngine.Object>())
        {
            CollectVoiceRequirements(asset, result);
        }

        return result;
    }

    private static List<AudioAssetValidationEntry> CollectProjectVoiceRequirements()
    {
        string[] roots = { "Assets/Resources/Heroines" };
        List<UnityEngine.Object> assets = new List<UnityEngine.Object>();
        AddAssets<HeroineProfileData>(assets, roots);
        AddAssets<ConversationData>(assets, roots);
        AddAssets<ActionData>(assets, roots);
        AddAssets<GameEventData>(assets, roots);
        AddAssets<ScheduledEventData>(assets, roots);
        AddAssets<HeroineTrainingDialogueData>(assets, roots);
        AddAssets<BattleResultEventData>(assets, roots);
        AddAssets<BattlePanelResultMessageData>(assets, roots);
        AddAssets<EndingData>(assets, roots);
        return CollectVoiceRequirements(assets);
    }

    private static void AddAssets<T>(
        List<UnityEngine.Object> destination,
        string[] roots)
        where T : UnityEngine.Object
    {
        destination.AddRange(
            AssetDatabase.FindAssets("t:" + typeof(T).Name, roots)
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<T>)
                .Where(asset => asset != null));
    }

    private static void CollectVoiceRequirements(
        UnityEngine.Object asset,
        List<AudioAssetValidationEntry> result)
    {
        if (asset == null) return;

        HeroineProfileData profile = asset as HeroineProfileData;
        if (profile != null)
        {
            string heroineId = ResolveHeroineId(asset, profile.heroineId);
            AddVoice(result, asset, heroineId, profile.initialDialogueVoiceId, "initialDialogueVoiceId");
            AddVoice(result, asset, heroineId, profile.nextActionPromptVoiceId, "nextActionPromptVoiceId");
            AddVoice(result, asset, heroineId, profile.morningGreetingVoiceId, "morningGreetingVoiceId");
            AddVoice(result, asset, heroineId, profile.goodNightGreetingVoiceId, "goodNightGreetingVoiceId");
            return;
        }

        string ownerId = ResolveHeroineId(asset, string.Empty);
        ConversationData conversation = asset as ConversationData;
        if (conversation != null)
        {
            ownerId = ResolveHeroineId(asset, conversation.heroineId);
            CollectConversationVoices(conversation, ownerId, result);
            return;
        }

        ActionData action = asset as ActionData;
        if (action != null)
        {
            if (action.reactions != null)
            {
                for (int i = 0; i < action.reactions.Count; i++)
                {
                    ActionReactionData reaction = action.reactions[i];
                    AddVoice(
                        result,
                        asset,
                        ownerId,
                        reaction != null ? reaction.voiceId : string.Empty,
                        "reactions[" + i + "].voiceId");
                }
            }
            return;
        }

        GameEventData gameEvent = asset as GameEventData;
        if (gameEvent != null)
        {
            if (gameEvent.pages != null)
            {
                for (int i = 0; i < gameEvent.pages.Count; i++)
                {
                    GameEventPageData page = gameEvent.pages[i];
                    AddVoice(
                        result,
                        asset,
                        ownerId,
                        page != null ? page.voiceId : string.Empty,
                        "pages[" + i + "].voiceId");
                }
            }
            return;
        }

        ScheduledEventData scheduledEvent = asset as ScheduledEventData;
        if (scheduledEvent != null)
        {
            AddVoice(result, asset, ownerId, scheduledEvent.preparationVoiceId, "preparationVoiceId");
            AddVoice(result, asset, ownerId, scheduledEvent.eventVoiceId, "eventVoiceId");
            return;
        }

        HeroineTrainingDialogueData training = asset as HeroineTrainingDialogueData;
        if (training != null)
        {
            ownerId = ResolveHeroineId(asset, training.heroineId);
            if (training.entries != null)
            {
                for (int entryIndex = 0; entryIndex < training.entries.Count; entryIndex++)
                {
                    HeroineTrainingDialogueEntry entry = training.entries[entryIndex];
                    if (entry == null || entry.voicedMessages == null) continue;
                    for (int voiceIndex = 0; voiceIndex < entry.voicedMessages.Count; voiceIndex++)
                    {
                        HeroineTrainingDialogueCandidate candidate =
                            entry.voicedMessages[voiceIndex];
                        AddVoice(
                            result,
                            asset,
                            ownerId,
                            candidate != null ? candidate.voiceId : string.Empty,
                            "entries[" + entryIndex + "].voicedMessages[" +
                            voiceIndex + "].voiceId");
                    }
                }
            }
            return;
        }

        BattleResultEventData battleResult = asset as BattleResultEventData;
        if (battleResult != null)
        {
            AddVoice(result, asset, ownerId, battleResult.voiceId, "voiceId");
            return;
        }

        BattlePanelResultMessageData panelResult =
            asset as BattlePanelResultMessageData;
        if (panelResult != null)
        {
            AddVoice(result, asset, ownerId, panelResult.voiceId, "voiceId");
            return;
        }

        EndingData ending = asset as EndingData;
        if (ending != null && ending.pages != null)
        {
            for (int i = 0; i < ending.pages.Count; i++)
            {
                EndingPageData page = ending.pages[i];
                AddVoice(
                    result,
                    asset,
                    ownerId,
                    page != null ? page.voiceId : string.Empty,
                    "pages[" + i + "].voiceId");
            }
        }
    }

    private static void CollectConversationVoices(
        ConversationData data,
        string heroineId,
        List<AudioAssetValidationEntry> result)
    {
        AddVoice(result, data, heroineId, data.voiceId, "voiceId");
        CollectConversationLines(data, data.lines, data.choices, heroineId, "legacy", result);
        if (data.items == null) return;

        for (int i = 0; i < data.items.Count; i++)
        {
            ConversationDataItem item = data.items[i];
            if (item == null) continue;
            AddVoice(result, data, heroineId, item.voiceId, "items[" + i + "].voiceId");
            CollectConversationLines(
                data,
                item.lines,
                item.choices,
                heroineId,
                "items[" + i + "]",
                result);
        }
    }

    private static void CollectConversationLines(
        UnityEngine.Object asset,
        List<ConversationLineData> lines,
        List<ConversationChoice> choices,
        string heroineId,
        string prefix,
        List<AudioAssetValidationEntry> result)
    {
        if (lines != null)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                ConversationLineData line = lines[i];
                AddVoice(
                    result,
                    asset,
                    heroineId,
                    line != null ? line.voiceId : string.Empty,
                    prefix + ".lines[" + i + "].voiceId");
            }
        }
        if (choices != null)
        {
            for (int i = 0; i < choices.Count; i++)
            {
                ConversationChoice choice = choices[i];
                AddVoice(
                    result,
                    asset,
                    heroineId,
                    choice != null ? choice.responseVoiceId : string.Empty,
                    prefix + ".choices[" + i + "].responseVoiceId");
            }
        }
    }

    private static void AddVoice(
        List<AudioAssetValidationEntry> result,
        UnityEngine.Object asset,
        string heroineId,
        string voiceId,
        string field)
    {
        if (string.IsNullOrWhiteSpace(voiceId)) return;

        result.Add(new AudioAssetValidationEntry(
            "VOICE",
            voiceId.Trim(),
            AudioManager.BuildVoiceResourcePath(heroineId, voiceId),
            asset.name + " / " + field,
            asset));
    }

    internal static string ResolveHeroineId(
        UnityEngine.Object asset,
        string explicitHeroineId)
    {
        if (!string.IsNullOrWhiteSpace(explicitHeroineId))
        {
            return explicitHeroineId.Trim();
        }

        const string prefix = "Assets/Resources/Heroines/";
        string path = asset != null ? AssetDatabase.GetAssetPath(asset) : string.Empty;
        if (!path.StartsWith(prefix, System.StringComparison.Ordinal))
        {
            return string.Empty;
        }

        string relativePath = path.Substring(prefix.Length);
        int slashIndex = relativePath.IndexOf('/');
        return slashIndex > 0 ? relativePath.Substring(0, slashIndex) : string.Empty;
    }
}
