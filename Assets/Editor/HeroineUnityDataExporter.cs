using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class HeroineUnityDataExporter
{
    private const string MenuPath = "FantasyLoveSim/Export Heroine Unity Data";
    private const int SchemaVersion = 1;

    [MenuItem(MenuPath)]
    public static void ExportHeroineUnityData()
    {
        HeroineProfileData profile = ResolveProfile();
        if (profile == null)
        {
            Debug.LogWarning("HeroineProfileData が選択されていないため、Unity data export を中止しました。");
            return;
        }

        string heroineId = string.IsNullOrWhiteSpace(profile.heroineId)
            ? profile.name
            : profile.heroineId;
        string defaultOutputRoot = Path.Combine(
            Directory.GetParent(Application.dataPath).FullName,
            "UnityImport",
            "FromUnity");
        Directory.CreateDirectory(defaultOutputRoot);
        string outputFolder = EditorUtility.SaveFolderPanel(
            "Export Heroine Unity Data",
            defaultOutputRoot,
            heroineId);

        if (string.IsNullOrEmpty(outputFolder))
        {
            return;
        }

        ExportHeroineUnityData(profile, outputFolder);
    }

    public static void ExportHeroineUnityData(HeroineProfileData profile, string outputFolder)
    {
        if (profile == null)
        {
            Debug.LogError("HeroineProfileData が null です。");
            return;
        }

        if (string.IsNullOrWhiteSpace(outputFolder))
        {
            Debug.LogError("出力先フォルダが空です。");
            return;
        }

        Directory.CreateDirectory(outputFolder);

        HeroineUnityExportReport report = new HeroineUnityExportReport();
        ExportActions(profile, outputFolder, report);
        ExportConversations(profile, outputFolder, report);
        WriteReport(profile, outputFolder, report);

        Debug.Log(
            "Heroine Unity data を export しました: " +
            outputFolder +
            " / actions: " +
            report.actionCount +
            " / conversations: " +
            report.conversationCount +
            " / warnings: " +
            report.warnings.Count);
        EditorUtility.DisplayDialog(
            "Heroine Unity Data Export",
            report.CreateDialogMessage(outputFolder),
            "OK");
    }

    private static HeroineProfileData ResolveProfile()
    {
        HeroineProfileData selectedProfile = Selection.activeObject as HeroineProfileData;
        if (selectedProfile != null)
        {
            return selectedProfile;
        }

        string assetPath = EditorUtility.OpenFilePanel(
            "Select HeroineProfileData",
            Path.Combine(Application.dataPath, "Resources", "Heroines"),
            "asset");
        if (string.IsNullOrEmpty(assetPath))
        {
            return null;
        }

        string projectRoot = Directory.GetParent(Application.dataPath).FullName.Replace("\\", "/");
        string normalizedPath = assetPath.Replace("\\", "/");
        if (!normalizedPath.StartsWith(projectRoot + "/", StringComparison.Ordinal))
        {
            Debug.LogWarning("Unity project 配下の asset を選択してください: " + assetPath);
            return null;
        }

        string relativeAssetPath = normalizedPath.Substring(projectRoot.Length + 1);
        return AssetDatabase.LoadAssetAtPath<HeroineProfileData>(relativeAssetPath);
    }

    private static void ExportActions(
        HeroineProfileData profile,
        string outputFolder,
        HeroineUnityExportReport report)
    {
        string actionResourcePath = string.IsNullOrWhiteSpace(profile.actionResourcePath)
            ? "Actions"
            : profile.actionResourcePath;
        ActionData[] actions = Resources.LoadAll<ActionData>(actionResourcePath);
        Array.Sort(actions, (left, right) => left.sortOrder.CompareTo(right.sortOrder));

        ActionsFromUnityExport export = new ActionsFromUnityExport
        {
            schemaVersion = SchemaVersion,
            heroineId = profile.heroineId,
            source = "Unity",
            actionResourcePath = actionResourcePath,
            items = new List<ActionFromUnityItem>()
        };

        HashSet<string> actionIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (ActionData action in actions)
        {
            if (action == null)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(action.actionId))
            {
                report.Warn("actionId が空の ActionData があります: " + action.name);
            }
            else if (!actionIds.Add(action.actionId))
            {
                report.Warn("actionId が重複しています: " + action.actionId);
            }

            export.items.Add(CreateActionItem(action, report));
        }

        report.actionCount = export.items.Count;
        WriteJson(Path.Combine(outputFolder, "actions_from_unity.json"), export);
    }

    private static void ExportConversations(
        HeroineProfileData profile,
        string outputFolder,
        HeroineUnityExportReport report)
    {
        string conversationResourcePath = string.IsNullOrWhiteSpace(profile.conversationResourcePath)
            ? "Conversations"
            : profile.conversationResourcePath;
        ConversationData[] conversationAssets = Resources.LoadAll<ConversationData>(conversationResourcePath);

        ConversationsFromUnityExport export = new ConversationsFromUnityExport
        {
            schemaVersion = SchemaVersion,
            heroineId = profile.heroineId,
            source = "Unity",
            conversationResourcePath = conversationResourcePath,
            items = new List<ConversationFromUnityItem>()
        };

        HashSet<string> conversationIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (ConversationData conversationAsset in conversationAssets)
        {
            if (conversationAsset == null)
            {
                continue;
            }

            if (conversationAsset.items != null && conversationAsset.items.Count > 0)
            {
                foreach (ConversationDataItem item in conversationAsset.items)
                {
                    AddConversationItem(
                        export.items,
                        CreateConversationItem(item, conversationAsset.name, report),
                        conversationIds,
                        report);
                }

                continue;
            }

            AddConversationItem(
                export.items,
                CreateConversationItem(conversationAsset, report),
                conversationIds,
                report);
        }

        export.items.Sort(CompareConversationItems);
        report.conversationCount = export.items.Count;
        WriteJson(Path.Combine(outputFolder, "conversations_from_unity.json"), export);
    }

    private static ActionFromUnityItem CreateActionItem(
        ActionData action,
        HeroineUnityExportReport report)
    {
        ActionFromUnityItem item = new ActionFromUnityItem
        {
            id = action.actionId,
            displayName = action.displayName,
            category = action.executionType.ToString(),
            executionType = action.executionType.ToString(),
            displayColumn = action.displayColumn.ToString(),
            sortOrder = action.sortOrder,
            isEnabled = action.isEnabled,
            unavailableMessage = action.unavailableMessage,
            useHeroineNameAsSpeaker = action.useHeroineNameAsSpeaker,
            resultLines = new List<FromUnityLine>
            {
                CreateLine(action.useHeroineNameAsSpeaker, action.resultMessage)
            },
            imageAssetIds = CreateImageAssetIds(action.stillId, action.stillSprite, report),
            affectionChange = action.affectionChange,
            advanceTime = action.advanceTime,
            conditions = CreateActionConditions(action),
            reactions = CreateActionReactions(action, report),
            memo = "Unity側から逆export"
        };

        return item;
    }

    private static ConversationFromUnityItem CreateConversationItem(
        ConversationData conversation,
        HeroineUnityExportReport report)
    {
        return new ConversationFromUnityItem
        {
            id = conversation.conversationId,
            title = conversation.conversationId,
            category = conversation.genre.ToString(),
            type = conversation.type.ToString(),
            conditions = CreateConversationConditions(conversation),
            lines = CreateConversationLines(
                conversation.lines,
                conversation.heroineLine,
                conversation.expressionId),
            imageAssetIds = new List<string>(),
            priority = conversation.priority,
            memo = "Unity側から逆export",
            sourceMetadata = CreateConversationSourceMetadata(
                conversation.name,
                conversation.showOnce,
                conversation.choices,
                report)
        };
    }

    private static ConversationFromUnityItem CreateConversationItem(
        ConversationDataItem item,
        string sourceAssetName,
        HeroineUnityExportReport report)
    {
        return new ConversationFromUnityItem
        {
            id = item.conversationId,
            title = item.conversationId,
            category = item.genre.ToString(),
            type = item.type.ToString(),
            conditions = CreateConversationConditions(item),
            lines = CreateConversationLines(
                item.lines,
                item.heroineLine,
                item.expressionId),
            imageAssetIds = new List<string>(),
            priority = item.priority,
            memo = "Unity側から逆export",
            sourceMetadata = CreateConversationSourceMetadata(
                sourceAssetName,
                item.showOnce,
                item.choices,
                report)
        };
    }

    private static void AddConversationItem(
        List<ConversationFromUnityItem> items,
        ConversationFromUnityItem item,
        HashSet<string> conversationIds,
        HeroineUnityExportReport report)
    {
        if (item == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(item.id))
        {
            report.Warn("conversationId が空の ConversationData があります。");
        }
        else if (!conversationIds.Add(item.id))
        {
            report.Warn("conversationId が重複しています: " + item.id);
        }

        items.Add(item);
    }

    private static List<ActionReactionFromUnityItem> CreateActionReactions(
        ActionData action,
        HeroineUnityExportReport report)
    {
        List<ActionReactionFromUnityItem> reactions = new List<ActionReactionFromUnityItem>();
        if (action.reactions == null)
        {
            return reactions;
        }

        foreach (ActionReactionData reaction in action.reactions)
        {
            if (reaction == null)
            {
                continue;
            }

            reactions.Add(
                new ActionReactionFromUnityItem
                {
                    id = reaction.reactionId,
                    resultLines = new List<FromUnityLine>
                    {
                        CreateLine(reaction.useHeroineNameAsSpeaker, reaction.resultMessage)
                    },
                    imageAssetIds = CreateImageAssetIds(reaction.stillId, reaction.stillSprite, report),
                    affectionChange = reaction.affectionChange,
                    advanceTime = reaction.advanceTime,
                    priority = reaction.priority,
                    conditions = CreateReactionConditions(reaction)
                });
        }

        return reactions;
    }

    private static FromUnityLine CreateLine(bool useHeroineNameAsSpeaker, string text)
    {
        return new FromUnityLine
        {
            speaker = useHeroineNameAsSpeaker ? "Heroine" : "System",
            text = text ?? string.Empty,
            expression = string.Empty
        };
    }

    private static List<FromUnityLine> CreateConversationLines(
        List<ConversationLineData> sourceLines,
        string heroineLine,
        string expressionId)
    {
        List<FromUnityLine> lines = new List<FromUnityLine>();
        if (sourceLines != null)
        {
            foreach (ConversationLineData line in sourceLines)
            {
                if (line == null)
                {
                    continue;
                }

                lines.Add(
                    new FromUnityLine
                    {
                        speaker = string.IsNullOrWhiteSpace(line.speaker) ? "Heroine" : line.speaker,
                        text = line.text ?? string.Empty,
                        expression = line.expressionId ?? string.Empty
                    });
            }
        }

        if (lines.Count == 0 && !string.IsNullOrWhiteSpace(heroineLine))
        {
            lines.Add(
                new FromUnityLine
                {
                    speaker = "Heroine",
                    text = heroineLine,
                    expression = expressionId ?? string.Empty
                });
        }

        return lines;
    }

    private static List<string> CreateImageAssetIds(
        string stillId,
        Sprite stillSprite,
        HeroineUnityExportReport report)
    {
        List<string> imageAssetIds = new List<string>();
        if (!string.IsNullOrWhiteSpace(stillId))
        {
            imageAssetIds.Add(stillId);
            return imageAssetIds;
        }

        if (stillSprite != null)
        {
            report.Warn("Sprite 参照はありますが stillId が空のため imageAssetIds に戻せません: " + stillSprite.name);
        }

        return imageAssetIds;
    }

    private static FromUnityConditions CreateActionConditions(ActionData action)
    {
        return new FromUnityConditions
        {
            minAffection = action.minAffection,
            maxAffection = action.maxAffection,
            timeSlots = CreateEnumList(action.anyTimeSlot, action.allowedTimeSlots),
            weathers = CreateEnumList(action.anyWeather, action.allowedWeathers),
            seasons = CreateEnumList(action.anySeason, action.allowedSeasons)
        };
    }

    private static FromUnityConditions CreateReactionConditions(ActionReactionData reaction)
    {
        return new FromUnityConditions
        {
            minAffection = reaction.minAffection,
            maxAffection = reaction.maxAffection,
            timeSlots = CreateEnumList(reaction.anyTimeSlot, reaction.allowedTimeSlots),
            weathers = CreateEnumList(reaction.anyWeather, reaction.allowedWeathers),
            seasons = CreateEnumList(reaction.anySeason, reaction.allowedSeasons)
        };
    }

    private static ConversationFromUnityConditions CreateConversationConditions(ConversationData conversation)
    {
        return new ConversationFromUnityConditions
        {
            once = conversation.showOnce,
            locationId = string.Empty,
            minAffection = conversation.minAffection,
            maxAffection = conversation.maxAffection,
            weather = CreateSingleEnumValue(conversation.anyWeather, conversation.allowedWeathers),
            season = CreateSingleEnumValue(conversation.anySeason, conversation.allowedSeasons),
            timeOfDay = CreateSingleEnumValue(conversation.anyTimeSlot, conversation.allowedTimeSlots)
        };
    }

    private static ConversationFromUnityConditions CreateConversationConditions(ConversationDataItem item)
    {
        return new ConversationFromUnityConditions
        {
            once = item.showOnce,
            locationId = string.Empty,
            minAffection = item.minAffection,
            maxAffection = item.maxAffection,
            weather = CreateSingleEnumValue(item.anyWeather, item.allowedWeathers),
            season = CreateSingleEnumValue(item.anySeason, item.allowedSeasons),
            timeOfDay = CreateSingleEnumValue(item.anyTimeSlot, item.allowedTimeSlots)
        };
    }

    private static ConversationSourceMetadata CreateConversationSourceMetadata(
        string sourceAssetName,
        bool showOnce,
        List<ConversationChoice> choices,
        HeroineUnityExportReport report)
    {
        ConversationSourceMetadata metadata = new ConversationSourceMetadata
        {
            sourceAssetName = sourceAssetName,
            showOnce = showOnce,
            choices = CreateConversationChoices(choices)
        };

        if (metadata.choices.Count > 0)
        {
            report.Warn("選択肢つき会話を sourceMetadata.choices に出力しました: " + sourceAssetName);
        }

        return metadata;
    }

    private static List<ConversationChoiceFromUnityItem> CreateConversationChoices(
        List<ConversationChoice> sourceChoices)
    {
        List<ConversationChoiceFromUnityItem> choices = new List<ConversationChoiceFromUnityItem>();
        if (sourceChoices == null)
        {
            return choices;
        }

        foreach (ConversationChoice choice in sourceChoices)
        {
            if (choice == null)
            {
                continue;
            }

            choices.Add(
                new ConversationChoiceFromUnityItem
                {
                    choiceText = choice.choiceText ?? string.Empty,
                    responseText = choice.responseText ?? string.Empty,
                    affectionChange = choice.affectionChange
                });
        }

        return choices;
    }

    private static string CreateSingleEnumValue<T>(bool any, List<T> values)
    {
        if (any || values == null || values.Count == 0)
        {
            return string.Empty;
        }

        if (values.Count > 1)
        {
            return string.Join(",", CreateEnumList(false, values).ToArray());
        }

        return values[0].ToString();
    }

    private static int CompareConversationItems(
        ConversationFromUnityItem left,
        ConversationFromUnityItem right)
    {
        int priorityComparison = right.priority.CompareTo(left.priority);
        if (priorityComparison != 0)
        {
            return priorityComparison;
        }

        return string.Compare(left.id, right.id, StringComparison.Ordinal);
    }

    private static List<string> CreateEnumList<T>(bool any, List<T> values)
    {
        List<string> result = new List<string>();
        if (any || values == null)
        {
            return result;
        }

        foreach (T value in values)
        {
            result.Add(value.ToString());
        }

        return result;
    }

    private static void WriteReport(
        HeroineProfileData profile,
        string outputFolder,
        HeroineUnityExportReport report)
    {
        UnityExportReportJson reportJson = new UnityExportReportJson
        {
            schemaVersion = SchemaVersion,
            heroineId = profile.heroineId,
            source = "Unity",
            actionCount = report.actionCount,
            conversationCount = report.conversationCount,
            warnings = report.warnings
        };

        WriteJson(Path.Combine(outputFolder, "export_report.json"), reportJson);
    }

    private static void WriteJson<T>(string path, T value)
    {
        string json = JsonUtility.ToJson(value, true);
        File.WriteAllText(path, json);
    }

    [Serializable]
    private sealed class ActionsFromUnityExport
    {
        public int schemaVersion;
        public string heroineId;
        public string source;
        public string actionResourcePath;
        public List<ActionFromUnityItem> items;
    }

    [Serializable]
    private sealed class ActionFromUnityItem
    {
        public string id;
        public string displayName;
        public string category;
        public string executionType;
        public string displayColumn;
        public int sortOrder;
        public bool isEnabled;
        public string unavailableMessage;
        public bool useHeroineNameAsSpeaker;
        public List<FromUnityLine> resultLines;
        public List<string> imageAssetIds;
        public int affectionChange;
        public bool advanceTime;
        public FromUnityConditions conditions;
        public List<ActionReactionFromUnityItem> reactions;
        public string memo;
    }

    [Serializable]
    private sealed class ConversationsFromUnityExport
    {
        public int schemaVersion;
        public string heroineId;
        public string source;
        public string conversationResourcePath;
        public List<ConversationFromUnityItem> items;
    }

    [Serializable]
    private sealed class ConversationFromUnityItem
    {
        public string id;
        public string title;
        public string category;
        public string type;
        public ConversationFromUnityConditions conditions;
        public List<FromUnityLine> lines;
        public List<string> imageAssetIds;
        public int priority;
        public string memo;
        public ConversationSourceMetadata sourceMetadata;
    }

    [Serializable]
    private sealed class ConversationFromUnityConditions
    {
        public bool once;
        public string locationId;
        public int minAffection;
        public int maxAffection;
        public string weather;
        public string season;
        public string timeOfDay;
    }

    [Serializable]
    private sealed class ConversationSourceMetadata
    {
        public string sourceAssetName;
        public bool showOnce;
        public List<ConversationChoiceFromUnityItem> choices;
    }

    [Serializable]
    private sealed class ConversationChoiceFromUnityItem
    {
        public string choiceText;
        public string responseText;
        public int affectionChange;
    }

    [Serializable]
    private sealed class ActionReactionFromUnityItem
    {
        public string id;
        public List<FromUnityLine> resultLines;
        public List<string> imageAssetIds;
        public int affectionChange;
        public bool advanceTime;
        public int priority;
        public FromUnityConditions conditions;
    }

    [Serializable]
    private sealed class FromUnityLine
    {
        public string speaker;
        public string text;
        public string expression;
    }

    [Serializable]
    private sealed class FromUnityConditions
    {
        public int minAffection;
        public int maxAffection;
        public List<string> timeSlots;
        public List<string> weathers;
        public List<string> seasons;
    }

    [Serializable]
    private sealed class UnityExportReportJson
    {
        public int schemaVersion;
        public string heroineId;
        public string source;
        public int actionCount;
        public int conversationCount;
        public List<string> warnings;
    }

    private sealed class HeroineUnityExportReport
    {
        public int actionCount;
        public int conversationCount;
        public readonly List<string> warnings = new List<string>();

        public void Warn(string message)
        {
            warnings.Add(message);
            Debug.LogWarning(message);
        }

        public string CreateDialogMessage(string outputFolder)
        {
            return
                "Export completed.\n" +
                "Output: " + outputFolder + "\n" +
                "Actions: " + actionCount + "\n" +
                "Conversations: " + conversationCount + "\n" +
                "Warnings: " + warnings.Count;
        }
    }
}
