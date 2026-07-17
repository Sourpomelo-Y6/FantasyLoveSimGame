using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using FantasyLoveSim.EditorTools;

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
        ExportProfile(profile, outputFolder, report);
        ExportActions(profile, outputFolder, report);
        ExportConversations(profile, outputFolder, report);
        ExportGameEvents(profile, outputFolder, report);
        ExportScheduledEvents(profile, outputFolder, report);
        ExportEndings(profile, outputFolder, report);
        ExportTrainingCatalog(profile, outputFolder, report);
        ExportTrainingDialogues(profile, outputFolder, report);
        HeroineSkillTreeAssetSync.Export(profile.heroineId, outputFolder);
        HeroineBattleMessageAssetSync.Export(profile, outputFolder);
        WriteReport(profile, outputFolder, report);

        Debug.Log(
            "Heroine Unity data を export しました: " +
            outputFolder +
            " / actions: " +
            report.actionCount +
            " / conversations: " +
            report.conversationCount +
            " / game events: " +
            report.gameEventCount +
            " / scheduled events: " +
            report.scheduledEventCount +
            " / endings: " +
            report.endingCount +
            " / training dialogues: " +
            report.trainingDialogueEntryCount +
            " / trainings: " +
            report.trainingCatalogCount +
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

    internal static void ExportProfile(
        HeroineProfileData profile,
        string outputFolder,
        HeroineUnityExportReport report)
    {
        HeroineProfileFromUnityExport export = new HeroineProfileFromUnityExport
        {
            schemaVersion = SchemaVersion,
            heroineId = profile.heroineId,
            source = "Unity",
            displayName = profile.displayName,
            heroineFirstPerson = profile.heroineFirstPerson,
            playerSecondPerson = profile.playerSecondPerson,
            initialDialogueMessage = profile.initialDialogueMessage,
            nextActionPrompt = profile.nextActionPrompt,
            morningGreeting = profile.morningGreeting,
            goodNightGreeting = profile.goodNightGreeting,
            gameStartFallbackMessage = profile.gameStartFallbackMessage,
            gameStartFollowUpMessage = profile.gameStartFollowUpMessage,
            outfitMessageOverrides = CreateOutfitMessageOverrides(profile.outfitMessageOverrides),
            outfitReactionMessageOverrides =
                CreateOutfitReactionMessageOverrides(profile.outfitReactionMessageOverrides),
            battleSkills = CreateHeroineBattleSkills(profile.battleSkills),
            conversationResourcePath = profile.conversationResourcePath,
            gameEventResourcePath = profile.gameEventResourcePath,
            actionResourcePath = profile.actionResourcePath,
            scheduledEventResourcePath = profile.scheduledEventResourcePath,
            battleResultEventResourcePath = profile.battleResultEventResourcePath,
            battlePanelResultMessageResourcePath = profile.battlePanelResultMessageResourcePath,
            endingResourcePath = profile.endingResourcePath
        };

        WriteJson(Path.Combine(outputFolder, "heroine_profile_from_unity.json"), export);
        report.profileExported = true;
    }

    private static List<HeroineBattleSkillFromUnity> CreateHeroineBattleSkills(List<HeroineBattleSkillData> source)
    {
        return BattleSkillSyncService.Normalize((source ?? new List<HeroineBattleSkillData>())
            .Select(item => item == null ? null : new BattleSkillSyncItem
            {
                SkillId = item.skillId,
                DisplayName = item.displayName,
                EffectType = item.effectType.ToString(),
                Target = item.target.ToString(),
                Cost = item.cost,
                Power = item.power,
                AffectedStat = item.affectedStat.ToString(),
                StatusDurationTurns = item.statusDurationTurns,
                UseChancePercent = item.useChancePercent,
                Priority = item.priority,
                MaxUsesPerBattle = item.maxUsesPerBattle
            }))
            .Select(item => new HeroineBattleSkillFromUnity
            {
                skillId = item.SkillId,
                displayName = item.DisplayName,
                effectType = item.EffectType,
                target = item.Target,
                cost = item.Cost,
                power = item.Power,
                affectedStat = item.AffectedStat,
                statusDurationTurns = item.StatusDurationTurns,
                useChancePercent = item.UseChancePercent,
                priority = item.Priority,
                maxUsesPerBattle = item.MaxUsesPerBattle
            }).ToList();
    }

    internal static void ExportTrainingDialogues(
        HeroineProfileData profile,
        string outputFolder,
        HeroineUnityExportReport report)
    {
        string heroineId = string.IsNullOrWhiteSpace(profile.heroineId)
            ? profile.name
            : profile.heroineId;
        string resourcePath =
            "Heroines/" + heroineId + "/TrainingDialogues/HeroineTrainingDialogueData";
        HeroineTrainingDialogueData data = Resources.Load<HeroineTrainingDialogueData>(resourcePath);
        TrainingDialoguesFromUnityExport export = new TrainingDialoguesFromUnityExport
        {
            schemaVersion = SchemaVersion,
            heroineId = heroineId,
            source = "Unity",
            items = new List<TrainingDialogueFromUnityItem>()
        };

        if (data == null)
        {
            report.Warn("訓練セリフデータが見つかりません: Resources/" + resourcePath);
        }
        else if (!string.IsNullOrWhiteSpace(data.heroineId) &&
            !string.Equals(data.heroineId, heroineId, StringComparison.Ordinal))
        {
            report.Warn(
                "訓練セリフデータのheroineIdが一致しません: " +
                data.heroineId + " / " + heroineId);
        }
        else if (data.entries != null)
        {
            export.items = TrainingDialogueSyncService.BuildExportItems(
                    data.entries.Select(entry => entry == null ? null : new TrainingDialogueSyncItem
                    {
                        TrainingId = entry.trainingId,
                        VisualState = entry.visualState.ToString(),
                        Messages = entry.messages
                    }),
                    report.Warn)
                .Select(item => new TrainingDialogueFromUnityItem
                {
                    trainingId = item.TrainingId,
                    visualState = item.VisualState,
                    messages = item.Messages
                })
                .ToList();
        }

        report.trainingDialogueEntryCount = export.items.Count;
        WriteJson(Path.Combine(outputFolder, "training_dialogues_from_unity.json"), export);
    }

    private static void ExportTrainingCatalog(
        HeroineProfileData profile,
        string outputFolder,
        HeroineUnityExportReport report)
    {
        string heroineId = string.IsNullOrWhiteSpace(profile.heroineId)
            ? profile.name
            : profile.heroineId;
        TrainingData[] trainings = Resources.LoadAll<TrainingData>("Training");
        SkillTreeNodeData[] nodes = Resources.LoadAll<SkillTreeNodeData>("SkillTreeNodes");
        Array.Sort(trainings, (left, right) => string.Compare(
            left != null ? left.trainingId : string.Empty,
            right != null ? right.trainingId : string.Empty,
            StringComparison.Ordinal));

        TrainingCatalogFromUnityExport export = new TrainingCatalogFromUnityExport
        {
            schemaVersion = SchemaVersion,
            heroineId = heroineId,
            source = "Unity",
            items = new List<TrainingCatalogFromUnityItem>()
        };
        HashSet<string> seenIds = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < trainings.Length; i++)
        {
            TrainingData training = trainings[i];
            if (training == null || string.IsNullOrWhiteSpace(training.trainingId))
            {
                continue;
            }
            if (!seenIds.Add(training.trainingId))
            {
                report.Warn("訓練カタログのtrainingIdが重複しています: " + training.trainingId);
                continue;
            }

            List<string> unlockNodeIds = new List<string>();
            List<string> unlockNodeNames = new List<string>();
            for (int nodeIndex = 0; nodeIndex < nodes.Length; nodeIndex++)
            {
                SkillTreeNodeData node = nodes[nodeIndex];
                if (node == null ||
                    (node.owner == SkillTreeOwner.Heroine &&
                        !string.IsNullOrWhiteSpace(node.targetHeroineId) &&
                        !string.Equals(node.targetHeroineId, heroineId, StringComparison.Ordinal)) ||
                    node.unlockedTrainingIds == null ||
                    !node.unlockedTrainingIds.Contains(training.trainingId))
                {
                    continue;
                }

                if (!unlockNodeIds.Contains(node.nodeId))
                {
                    unlockNodeIds.Add(node.nodeId);
                    unlockNodeNames.Add(node.GetDisplayName());
                }
            }

            if (!training.unlockedByDefault && unlockNodeIds.Count == 0)
            {
                continue;
            }

            export.items.Add(new TrainingCatalogFromUnityItem
            {
                trainingId = training.trainingId,
                displayName = training.GetDisplayName(),
                trainingCategoryId = training.trainingCategoryId,
                unlockedByDefault = training.unlockedByDefault,
                unlockNodeIds = unlockNodeIds,
                unlockNodeNames = unlockNodeNames
            });
        }

        report.trainingCatalogCount = export.items.Count;
        WriteJson(Path.Combine(outputFolder, "training_catalog_from_unity.json"), export);
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

    internal static void ExportGameEvents(
        HeroineProfileData profile,
        string outputFolder,
        HeroineUnityExportReport report)
    {
        string gameEventResourcePath = string.IsNullOrWhiteSpace(profile.gameEventResourcePath)
            ? "GameEvents"
            : profile.gameEventResourcePath;
        GameEventData[] gameEvents = Resources.LoadAll<GameEventData>(gameEventResourcePath);
        Array.Sort(gameEvents, (left, right) => left.sortOrder.CompareTo(right.sortOrder));

        GameEventsFromUnityExport export = new GameEventsFromUnityExport
        {
            schemaVersion = SchemaVersion,
            heroineId = profile.heroineId,
            source = "Unity",
            gameEventResourcePath = gameEventResourcePath,
            items = new List<GameEventFromUnityItem>()
        };

        HashSet<string> eventIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (GameEventData gameEvent in gameEvents)
        {
            if (gameEvent == null)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(gameEvent.eventId))
            {
                report.Warn("eventId が空の GameEventData があります: " + gameEvent.name);
            }
            else if (!eventIds.Add(gameEvent.eventId))
            {
                report.Warn("eventId が重複しています: " + gameEvent.eventId);
            }

            export.items.Add(CreateGameEventItem(gameEvent, report));
        }

        report.gameEventCount = export.items.Count;
        WriteJson(Path.Combine(outputFolder, "game_events_from_unity.json"), export);
    }

    private static void ExportScheduledEvents(
        HeroineProfileData profile,
        string outputFolder,
        HeroineUnityExportReport report)
    {
        string scheduledEventResourcePath = string.IsNullOrWhiteSpace(profile.scheduledEventResourcePath)
            ? "ScheduledEvents"
            : profile.scheduledEventResourcePath;
        ScheduledEventData[] scheduledEvents = Resources.LoadAll<ScheduledEventData>(scheduledEventResourcePath);
        Array.Sort(scheduledEvents, CompareScheduledEventData);

        ScheduledEventsFromUnityExport export = new ScheduledEventsFromUnityExport
        {
            schemaVersion = SchemaVersion,
            heroineId = profile.heroineId,
            source = "Unity",
            scheduledEventResourcePath = scheduledEventResourcePath,
            items = new List<ScheduledEventFromUnityItem>()
        };

        HashSet<ScheduleType> scheduleTypes = new HashSet<ScheduleType>();
        foreach (ScheduledEventData scheduledEvent in scheduledEvents)
        {
            if (scheduledEvent == null)
            {
                continue;
            }

            if (scheduledEvent.scheduleType == ScheduleType.None)
            {
                report.Warn("scheduleType が None の ScheduledEventData があります: " + scheduledEvent.name);
            }
            else if (!scheduleTypes.Add(scheduledEvent.scheduleType))
            {
                report.Warn("scheduleType が重複しています: " + scheduledEvent.scheduleType);
            }

            export.items.Add(CreateScheduledEventItem(scheduledEvent, report));
        }

        report.scheduledEventCount = export.items.Count;
        WriteJson(Path.Combine(outputFolder, "scheduled_events_from_unity.json"), export);
    }

    private static void ExportEndings(
        HeroineProfileData profile,
        string outputFolder,
        HeroineUnityExportReport report)
    {
        string endingResourcePath = string.IsNullOrWhiteSpace(profile.endingResourcePath)
            ? "Endings"
            : profile.endingResourcePath;
        EndingData[] endings = Resources.LoadAll<EndingData>(endingResourcePath);
        Array.Sort(endings, CompareEndingData);

        EndingsFromUnityExport export = new EndingsFromUnityExport
        {
            schemaVersion = SchemaVersion,
            heroineId = profile.heroineId,
            source = "Unity",
            endingResourcePath = endingResourcePath,
            items = new List<EndingFromUnityItem>()
        };

        HashSet<string> endingIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (EndingData ending in endings)
        {
            if (ending == null)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(ending.endingId))
            {
                report.Warn("endingId が空の EndingData があります: " + ending.name);
            }
            else if (!endingIds.Add(ending.endingId))
            {
                report.Warn("endingId が重複しています: " + ending.endingId);
            }

            export.items.Add(CreateEndingItem(ending, report));
        }

        report.endingCount = export.items.Count;
        WriteJson(Path.Combine(outputFolder, "endings_from_unity.json"), export);
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

    private static GameEventFromUnityItem CreateGameEventItem(
        GameEventData gameEvent,
        HeroineUnityExportReport report)
    {
        return new GameEventFromUnityItem
        {
            id = gameEvent.eventId,
            title = gameEvent.eventId,
            category = gameEvent.triggerType.ToString(),
            conditions = CreateGameEventConditions(gameEvent),
            lines = CreateGameEventLines(gameEvent.pages, report),
            imageAssetIds = CreateGameEventImageAssetIds(gameEvent, report),
            priority = gameEvent.sortOrder,
            memo = "Unity側から逆export",
            sourceMetadata = CreateGameEventSourceMetadata(gameEvent)
        };
    }

    private static EndingFromUnityItem CreateEndingItem(
        EndingData ending,
        HeroineUnityExportReport report)
    {
        return new EndingFromUnityItem
        {
            id = ending.endingId,
            title = ending.displayName,
            category = CreateEndingCategory(ending),
            conditions = new EndingFromUnityConditions
            {
                minAffection = ending.requiredAffection,
                costumeId = ending.costumeId ?? string.Empty,
                requiredFlagIds = CreateStringArrayList(ending.requiredShownEventIds)
            },
            lines = CreateEndingLines(ending.message),
            imageAssetIds = CreateEndingImageAssetIds(ending, report),
            priority = ending.requiredAffection,
            requiredAffection = ending.requiredAffection,
            requiredShownEventIds = CreateStringArrayList(ending.requiredShownEventIds),
            memo = "Unity側から逆export",
            sourceMetadata = new EndingSourceMetadata
            {
                sourceAssetName = ending.name,
                hasStillSprite = ending.stillSprite != null
            }
        };
    }

    private static ScheduledEventFromUnityItem CreateScheduledEventItem(
        ScheduledEventData scheduledEvent,
        HeroineUnityExportReport report)
    {
        string id = string.IsNullOrWhiteSpace(scheduledEvent.actionId)
            ? scheduledEvent.name
            : scheduledEvent.actionId;

        return new ScheduledEventFromUnityItem
        {
            id = id,
            title = scheduledEvent.name,
            category = scheduledEvent.scheduleType.ToString(),
            scheduleType = scheduledEvent.scheduleType.ToString(),
            actionId = scheduledEvent.actionId ?? string.Empty,
            conditions = new ScheduledEventFromUnityConditions
            {
                scheduleType = scheduledEvent.scheduleType.ToString(),
                actionId = scheduledEvent.actionId ?? string.Empty,
                triggerTimeSlot = scheduledEvent.triggerTimeSlot.ToString(),
                timeOfDay = scheduledEvent.triggerTimeSlot.ToString(),
                costumeId = scheduledEvent.costumeId ?? string.Empty,
                allowOutfitChangeBeforeStart = scheduledEvent.allowOutfitChangeBeforeStart,
                outfitPromptMode = scheduledEvent.outfitPromptMode.ToString(),
                eventSpeakerType = scheduledEvent.eventSpeakerType.ToString(),
                speakerType = scheduledEvent.eventSpeakerType.ToString(),
                affectionChange = scheduledEvent.affectionChange
            },
            preparationMessage = scheduledEvent.preparationMessage ?? string.Empty,
            eventMessage = scheduledEvent.eventMessage ?? string.Empty,
            lines = CreateScheduledEventLines(scheduledEvent),
            imageAssetIds = CreateImageAssetIds(scheduledEvent.stillId, scheduledEvent.stillSprite, report),
            priority = 0,
            memo = "Unity側から逆export"
        };
    }

    private static List<FromUnityLine> CreateScheduledEventLines(ScheduledEventData scheduledEvent)
    {
        List<FromUnityLine> lines = new List<FromUnityLine>();
        if (!string.IsNullOrWhiteSpace(scheduledEvent.preparationMessage))
        {
            lines.Add(
                new FromUnityLine
                {
                    speaker = "Schedule",
                    text = scheduledEvent.preparationMessage,
                    expression = string.Empty
                });
        }

        if (!string.IsNullOrWhiteSpace(scheduledEvent.eventMessage))
        {
            lines.Add(
                new FromUnityLine
                {
                    speaker = scheduledEvent.eventSpeakerType.ToString(),
                    text = scheduledEvent.eventMessage,
                    expression = string.Empty
                });
        }

        return lines;
    }

    private static string CreateEndingCategory(EndingData ending)
    {
        string source = ((ending.endingId ?? string.Empty) + " " + (ending.displayName ?? string.Empty)).Trim();
        if (source.IndexOf("Good", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return "Good";
        }

        if (source.IndexOf("Normal", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return "Normal";
        }

        if (source.IndexOf("Bad", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return "Bad";
        }

        return "Ending";
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

    private static List<FromUnityLine> CreateGameEventLines(
        List<GameEventPageData> pages,
        HeroineUnityExportReport report)
    {
        List<FromUnityLine> lines = new List<FromUnityLine>();
        if (pages == null)
        {
            return lines;
        }

        foreach (GameEventPageData page in pages)
        {
            if (page == null)
            {
                continue;
            }

            lines.Add(
                new FromUnityLine
                {
                    speaker = page.speakerType.ToString(),
                    text = page.message ?? string.Empty,
                    expression = page.expressionId ?? string.Empty
                });

            if (string.IsNullOrWhiteSpace(page.stillId) && page.stillSprite != null)
            {
                report.Warn("GameEvent page に Sprite 参照はありますが stillId が空です: " + page.stillSprite.name);
            }
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
            seasons = CreateEnumList(action.anySeason, action.allowedSeasons),
            costumeId = string.Empty
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
            seasons = CreateEnumList(reaction.anySeason, reaction.allowedSeasons),
            costumeId = reaction.costumeId ?? string.Empty
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
            timeOfDay = CreateSingleEnumValue(conversation.anyTimeSlot, conversation.allowedTimeSlots),
            costumeId = conversation.costumeId ?? string.Empty
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
            timeOfDay = CreateSingleEnumValue(item.anyTimeSlot, item.allowedTimeSlots),
            costumeId = item.costumeId ?? string.Empty
        };
    }

    private static GameEventFromUnityConditions CreateGameEventConditions(GameEventData gameEvent)
    {
        return new GameEventFromUnityConditions
        {
            once = gameEvent.showOnce,
            locationId = string.Empty,
            minDay = gameEvent.minDay,
            maxDay = gameEvent.maxDay,
            minAffection = gameEvent.minAffection,
            maxAffection = gameEvent.maxAffection,
            weather = CreateSingleEnumValue(gameEvent.anyWeather, gameEvent.allowedWeathers),
            season = string.Empty,
            timeOfDay = string.Empty,
            costumeId = CreateFirstOutfitId(gameEvent.requiredOutfitIds, gameEvent.requiredOutfits),
            requiredShownEventIds = CreateStringList(gameEvent.requiredShownEventIds),
            blockedShownEventIds = CreateStringList(gameEvent.blockedShownEventIds),
            requiredOutfitIds = CreateOutfitIdList(gameEvent.requiredOutfitIds, gameEvent.requiredOutfits),
            blockedOutfitIds = CreateOutfitIdList(gameEvent.blockedOutfitIds, gameEvent.blockedOutfits),
            requiredSkillIds = RequiredSkillIdSyncService.Normalize(gameEvent.requiredSkillIds)
        };
    }

    private static List<string> CreateGameEventImageAssetIds(
        GameEventData gameEvent,
        HeroineUnityExportReport report)
    {
        List<string> imageAssetIds = new List<string>();
        HashSet<string> addedIds = new HashSet<string>(StringComparer.Ordinal);
        if (gameEvent.pages == null)
        {
            return imageAssetIds;
        }

        foreach (GameEventPageData page in gameEvent.pages)
        {
            if (page == null)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(page.stillId))
            {
                if (addedIds.Add(page.stillId))
                {
                    imageAssetIds.Add(page.stillId);
                }
            }
            else if (page.stillSprite != null)
            {
                report.Warn("GameEvent page に Sprite 参照はありますが stillId が空です: " + page.stillSprite.name);
            }
        }

        return imageAssetIds;
    }

    private static List<FromUnityLine> CreateEndingLines(string message)
    {
        List<FromUnityLine> lines = new List<FromUnityLine>();
        if (string.IsNullOrWhiteSpace(message))
        {
            return lines;
        }

        string[] splitLines = message.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        foreach (string line in splitLines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            lines.Add(
                new FromUnityLine
                {
                    speaker = "System",
                    text = line,
                    expression = string.Empty
                });
        }

        return lines;
    }

    private static List<string> CreateEndingImageAssetIds(
        EndingData ending,
        HeroineUnityExportReport report)
    {
        List<string> imageAssetIds = new List<string>();
        if (ending.stillSprite == null)
        {
            return imageAssetIds;
        }

        if (string.IsNullOrWhiteSpace(ending.stillSprite.name))
        {
            report.Warn("EndingData に Sprite 参照はありますが Sprite 名が空です: " + ending.name);
            return imageAssetIds;
        }

        imageAssetIds.Add(ending.stillSprite.name);
        return imageAssetIds;
    }

    private static GameEventSourceMetadata CreateGameEventSourceMetadata(GameEventData gameEvent)
    {
        return new GameEventSourceMetadata
        {
            sourceAssetName = gameEvent.name,
            isEnabled = gameEvent.isEnabled,
            pages = CreateGameEventPageMetadata(gameEvent.pages)
        };
    }

    private static List<GameEventPageSourceMetadata> CreateGameEventPageMetadata(
        List<GameEventPageData> pages)
    {
        List<GameEventPageSourceMetadata> metadata = new List<GameEventPageSourceMetadata>();
        if (pages == null)
        {
            return metadata;
        }

        for (int i = 0; i < pages.Count; i++)
        {
            GameEventPageData page = pages[i];
            if (page == null)
            {
                continue;
            }

            metadata.Add(
                new GameEventPageSourceMetadata
                {
                    index = i,
                    speakerName = page.speakerName ?? string.Empty,
                    stillId = page.stillId ?? string.Empty,
                    hasStillSprite = page.stillSprite != null
                });
        }

        return metadata;
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

    private static List<string> CreateStringList(List<string> values)
    {
        List<string> result = new List<string>();
        if (values == null)
        {
            return result;
        }

        foreach (string value in values)
        {
            if (!string.IsNullOrWhiteSpace(value) && !result.Contains(value))
            {
                result.Add(value);
            }
        }

        return result;
    }

    private static List<string> CreateStringArrayList(string[] values)
    {
        List<string> result = new List<string>();
        if (values == null)
        {
            return result;
        }

        foreach (string value in values)
        {
            if (!string.IsNullOrWhiteSpace(value) && !result.Contains(value))
            {
                result.Add(value);
            }
        }

        return result;
    }

    private static List<OutfitMessageOverrideFromUnity> CreateOutfitMessageOverrides(
        List<OutfitMessageOverride> overrides)
    {
        List<OutfitMessageOverrideFromUnity> result = new List<OutfitMessageOverrideFromUnity>();
        if (overrides == null)
        {
            return result;
        }

        foreach (OutfitMessageOverride item in overrides)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.outfitId))
            {
                continue;
            }

            result.Add(new OutfitMessageOverrideFromUnity
            {
                outfitId = item.outfitId,
                lockedMessage = item.lockedMessage ?? string.Empty,
                changedMessage = item.changedMessage ?? string.Empty
            });
        }

        return result;
    }

    private static List<OutfitReactionMessageOverrideFromUnity> CreateOutfitReactionMessageOverrides(
        List<OutfitReactionMessageOverride> overrides)
    {
        List<OutfitReactionMessageOverrideFromUnity> result =
            new List<OutfitReactionMessageOverrideFromUnity>();
        if (overrides == null)
        {
            return result;
        }

        foreach (OutfitReactionMessageOverride item in overrides)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.message))
            {
                continue;
            }

            result.Add(new OutfitReactionMessageOverrideFromUnity
            {
                reactionType = item.reactionType.ToString(),
                message = item.message
            });
        }

        return result;
    }

    private static List<string> CreateOutfitIdList(
        List<string> outfitIds,
        List<OutfitData> outfits)
    {
        List<string> result = CreateStringList(outfitIds);
        if (outfits == null)
        {
            return result;
        }

        foreach (OutfitData outfit in outfits)
        {
            if (outfit == null || string.IsNullOrWhiteSpace(outfit.outfitId))
            {
                continue;
            }

            if (!result.Contains(outfit.outfitId))
            {
                result.Add(outfit.outfitId);
            }
        }

        return result;
    }

    private static string CreateFirstOutfitId(
        List<string> outfitIds,
        List<OutfitData> outfits)
    {
        List<string> ids = CreateOutfitIdList(outfitIds, outfits);
        return ids.Count > 0 ? ids[0] : string.Empty;
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

    private static int CompareEndingData(EndingData left, EndingData right)
    {
        int affectionComparison = right.requiredAffection.CompareTo(left.requiredAffection);
        if (affectionComparison != 0)
        {
            return affectionComparison;
        }

        return string.Compare(left.endingId, right.endingId, StringComparison.Ordinal);
    }

    private static int CompareScheduledEventData(ScheduledEventData left, ScheduledEventData right)
    {
        int scheduleComparison = left.scheduleType.CompareTo(right.scheduleType);
        if (scheduleComparison != 0)
        {
            return scheduleComparison;
        }

        return string.Compare(left.actionId, right.actionId, StringComparison.Ordinal);
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
            profileExported = report.profileExported,
            actionCount = report.actionCount,
            conversationCount = report.conversationCount,
            gameEventCount = report.gameEventCount,
            scheduledEventCount = report.scheduledEventCount,
            endingCount = report.endingCount,
            trainingDialogueEntryCount = report.trainingDialogueEntryCount,
            trainingCatalogCount = report.trainingCatalogCount,
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
    private sealed class HeroineProfileFromUnityExport
    {
        public int schemaVersion;
        public string heroineId;
        public string source;
        public string displayName;
        public string heroineFirstPerson;
        public string playerSecondPerson;
        public string initialDialogueMessage;
        public string nextActionPrompt;
        public string morningGreeting;
        public string goodNightGreeting;
        public string gameStartFallbackMessage;
        public string gameStartFollowUpMessage;
        public List<OutfitMessageOverrideFromUnity> outfitMessageOverrides;
        public List<OutfitReactionMessageOverrideFromUnity> outfitReactionMessageOverrides;
        public List<HeroineBattleSkillFromUnity> battleSkills;
        public string conversationResourcePath;
        public string gameEventResourcePath;
        public string actionResourcePath;
        public string scheduledEventResourcePath;
        public string battleResultEventResourcePath;
        public string battlePanelResultMessageResourcePath;
        public string endingResourcePath;
    }

    [Serializable]
    private sealed class HeroineBattleSkillFromUnity
    {
        public string skillId;
        public string displayName;
        public string effectType;
        public string target;
        public int cost;
        public int power;
        public string affectedStat;
        public int statusDurationTurns;
        public int useChancePercent;
        public int priority;
        public int maxUsesPerBattle;
    }

    [Serializable]
    private sealed class TrainingDialoguesFromUnityExport
    {
        public int schemaVersion;
        public string heroineId;
        public string source;
        public List<TrainingDialogueFromUnityItem> items;
    }

    [Serializable]
    private sealed class TrainingCatalogFromUnityExport
    {
        public int schemaVersion;
        public string heroineId;
        public string source;
        public List<TrainingCatalogFromUnityItem> items;
    }

    [Serializable]
    private sealed class TrainingCatalogFromUnityItem
    {
        public string trainingId;
        public string displayName;
        public string trainingCategoryId;
        public bool unlockedByDefault;
        public List<string> unlockNodeIds;
        public List<string> unlockNodeNames;
    }

    [Serializable]
    private sealed class TrainingDialogueFromUnityItem
    {
        public string trainingId;
        public string visualState;
        public List<string> messages;
    }

    [Serializable]
    private sealed class OutfitMessageOverrideFromUnity
    {
        public string outfitId;
        public string lockedMessage;
        public string changedMessage;
    }

    [Serializable]
    private sealed class OutfitReactionMessageOverrideFromUnity
    {
        public string reactionType;
        public string message;
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
        public string costumeId;
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
    private sealed class GameEventsFromUnityExport
    {
        public int schemaVersion;
        public string heroineId;
        public string source;
        public string gameEventResourcePath;
        public List<GameEventFromUnityItem> items;
    }

    [Serializable]
    private sealed class EndingsFromUnityExport
    {
        public int schemaVersion;
        public string heroineId;
        public string source;
        public string endingResourcePath;
        public List<EndingFromUnityItem> items;
    }

    [Serializable]
    private sealed class ScheduledEventsFromUnityExport
    {
        public int schemaVersion;
        public string heroineId;
        public string source;
        public string scheduledEventResourcePath;
        public List<ScheduledEventFromUnityItem> items;
    }

    [Serializable]
    private sealed class GameEventFromUnityItem
    {
        public string id;
        public string title;
        public string category;
        public GameEventFromUnityConditions conditions;
        public List<FromUnityLine> lines;
        public List<string> imageAssetIds;
        public int priority;
        public string memo;
        public GameEventSourceMetadata sourceMetadata;
    }

    [Serializable]
    private sealed class EndingFromUnityItem
    {
        public string id;
        public string title;
        public string category;
        public EndingFromUnityConditions conditions;
        public List<FromUnityLine> lines;
        public List<string> imageAssetIds;
        public int priority;
        public int requiredAffection;
        public List<string> requiredShownEventIds;
        public string memo;
        public EndingSourceMetadata sourceMetadata;
    }

    [Serializable]
    private sealed class ScheduledEventFromUnityItem
    {
        public string id;
        public string title;
        public string category;
        public string scheduleType;
        public string actionId;
        public ScheduledEventFromUnityConditions conditions;
        public string preparationMessage;
        public string eventMessage;
        public List<FromUnityLine> lines;
        public List<string> imageAssetIds;
        public int priority;
        public string memo;
    }

    [Serializable]
    private sealed class ScheduledEventFromUnityConditions
    {
        public string scheduleType;
        public string actionId;
        public string triggerTimeSlot;
        public string timeOfDay;
        public string costumeId;
        public bool allowOutfitChangeBeforeStart;
        public string outfitPromptMode;
        public string eventSpeakerType;
        public string speakerType;
        public int affectionChange;
    }

    [Serializable]
    private sealed class EndingFromUnityConditions
    {
        public int minAffection;
        public string costumeId;
        public List<string> requiredFlagIds;
    }

    [Serializable]
    private sealed class GameEventFromUnityConditions
    {
        public bool once;
        public string locationId;
        public int minDay;
        public int maxDay;
        public int minAffection;
        public int maxAffection;
        public string weather;
        public string season;
        public string timeOfDay;
        public string costumeId;
        public List<string> requiredShownEventIds;
        public List<string> blockedShownEventIds;
        public List<string> requiredOutfitIds;
        public List<string> blockedOutfitIds;
        public List<string> requiredSkillIds;
    }

    [Serializable]
    private sealed class GameEventSourceMetadata
    {
        public string sourceAssetName;
        public bool isEnabled;
        public List<GameEventPageSourceMetadata> pages;
    }

    [Serializable]
    private sealed class EndingSourceMetadata
    {
        public string sourceAssetName;
        public bool hasStillSprite;
    }

    [Serializable]
    private sealed class GameEventPageSourceMetadata
    {
        public int index;
        public string speakerName;
        public string stillId;
        public bool hasStillSprite;
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
        public string costumeId;
    }

    [Serializable]
    private sealed class UnityExportReportJson
    {
        public int schemaVersion;
        public string heroineId;
        public string source;
        public bool profileExported;
        public int actionCount;
        public int conversationCount;
        public int gameEventCount;
        public int scheduledEventCount;
        public int endingCount;
        public int trainingDialogueEntryCount;
        public int trainingCatalogCount;
        public List<string> warnings;
    }

    internal sealed class HeroineUnityExportReport
    {
        public int actionCount;
        public int conversationCount;
        public int gameEventCount;
        public int scheduledEventCount;
        public int endingCount;
        public int trainingDialogueEntryCount;
        public int trainingCatalogCount;
        public bool profileExported;
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
                "Game events: " + gameEventCount + "\n" +
                "Scheduled events: " + scheduledEventCount + "\n" +
                "Endings: " + endingCount + "\n" +
                "Training dialogue entries: " + trainingDialogueEntryCount + "\n" +
                "Trainings: " + trainingCatalogCount + "\n" +
                "Warnings: " + warnings.Count;
        }
    }
}
