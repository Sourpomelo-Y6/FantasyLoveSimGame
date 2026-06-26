using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class HeroineDataValidator
{
    private const string MenuPath = "FantasyLoveSim/Validate Heroine Data";

    [MenuItem(MenuPath)]
    public static void ValidateSelectedHeroineData()
    {
        HeroineProfileData profile = ResolveProfile();
        if (profile == null)
        {
            Debug.LogWarning("HeroineProfileData が選択されていないため、ヒロインデータ検証を中止しました。");
            return;
        }

        ValidationReport report = new ValidationReport(profile);
        ValidateProfile(profile, report);
        ValidateActions(profile, report);
        ValidateConversations(profile, report);
        ValidateGameEvents(profile, report);
        ValidateScheduledEvents(profile, report);
        ValidateEndings(profile, report);
        ValidateLayeredSpriteData(profile, report);
        ValidateAssetCatalog(profile, report);

        report.Log();
        EditorUtility.DisplayDialog("Heroine Data Validation", report.CreateDialogMessage(), "OK");
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

    private static void ValidateProfile(HeroineProfileData profile, ValidationReport report)
    {
        string heroineId = report.HeroineId;
        string profileAssetPath = AssetDatabase.GetAssetPath(profile);
        if (string.IsNullOrWhiteSpace(profile.heroineId))
        {
            report.Warn("HeroineProfileData.heroineId が空です: " + profileAssetPath);
        }

        if (string.IsNullOrWhiteSpace(profile.displayName))
        {
            report.Warn("HeroineProfileData.displayName が空です: " + profileAssetPath);
        }

        ValidateSpriteOwner(profile.defaultHeroineSprite, heroineId, "Profile.defaultHeroineSprite", report);
        ValidateResourcePath("conversationResourcePath", profile.conversationResourcePath, heroineId, report);
        ValidateResourcePath("gameEventResourcePath", profile.gameEventResourcePath, heroineId, report);
        ValidateResourcePath("actionResourcePath", profile.actionResourcePath, heroineId, report);
        ValidateResourcePath("scheduledEventResourcePath", profile.scheduledEventResourcePath, heroineId, report);
        ValidateResourcePath("endingResourcePath", profile.endingResourcePath, heroineId, report);
        ValidateOutfitMessageOverrides(profile, report);
        ValidateOutfitReactionMessageOverrides(profile, report);
    }

    private static void ValidateActions(HeroineProfileData profile, ValidationReport report)
    {
        ActionData[] actions = Resources.LoadAll<ActionData>(Fallback(profile.actionResourcePath, "Actions"));
        report.actionCount = actions.Length;

        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (ActionData action in actions)
        {
            if (action == null)
            {
                continue;
            }

            ValidateResourceAssetOwner(action, report);
            ValidateRequiredId(action.actionId, "ActionData.actionId", action, report);
            ValidateDuplicateId(ids, action.actionId, "ActionData.actionId", action, report);
            ValidateSpriteOwner(action.stillSprite, report.HeroineId, "ActionData.stillSprite: " + action.name, report);

            if (action.reactions == null)
            {
                continue;
            }

            HashSet<string> reactionIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (ActionReactionData reaction in action.reactions)
            {
                if (reaction == null)
                {
                    continue;
                }

                ValidateDuplicateId(
                    reactionIds,
                    reaction.reactionId,
                    "ActionReactionData.reactionId in " + action.name,
                    action,
                    report);
                ValidateSpriteOwner(
                    reaction.stillSprite,
                    report.HeroineId,
                    "ActionReactionData.stillSprite: " + action.name + "/" + reaction.reactionId,
                    report);
            }
        }
    }

    private static void ValidateConversations(HeroineProfileData profile, ValidationReport report)
    {
        ConversationData[] conversations =
            Resources.LoadAll<ConversationData>(Fallback(profile.conversationResourcePath, "Conversations"));
        report.conversationCount = conversations.Length;

        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (ConversationData conversation in conversations)
        {
            if (conversation == null)
            {
                continue;
            }

            ValidateResourceAssetOwner(conversation, report);
            if (!string.IsNullOrWhiteSpace(conversation.heroineId) &&
                !string.Equals(conversation.heroineId, report.HeroineId, StringComparison.Ordinal))
            {
                report.Warn(
                    "ConversationData.heroineId が選択中ヒロインと一致しません: " +
                    conversation.name +
                    " / heroineId=" +
                    conversation.heroineId);
            }

            if (conversation.items != null && conversation.items.Count > 0)
            {
                foreach (ConversationDataItem item in conversation.items)
                {
                    if (item == null)
                    {
                        continue;
                    }

                    ValidateRequiredId(item.conversationId, "ConversationDataItem.conversationId", conversation, report);
                    ValidateDuplicateId(ids, item.conversationId, "ConversationDataItem.conversationId", conversation, report);
                }
            }
            else
            {
                ValidateRequiredId(conversation.conversationId, "ConversationData.conversationId", conversation, report);
                ValidateDuplicateId(ids, conversation.conversationId, "ConversationData.conversationId", conversation, report);
            }
        }
    }

    private static void ValidateGameEvents(HeroineProfileData profile, ValidationReport report)
    {
        GameEventData[] gameEvents =
            Resources.LoadAll<GameEventData>(Fallback(profile.gameEventResourcePath, "GameEvents"));
        report.gameEventCount = gameEvents.Length;

        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (GameEventData gameEvent in gameEvents)
        {
            if (gameEvent == null)
            {
                continue;
            }

            ValidateResourceAssetOwner(gameEvent, report);
            ValidateRequiredId(gameEvent.eventId, "GameEventData.eventId", gameEvent, report);
            ValidateDuplicateId(ids, gameEvent.eventId, "GameEventData.eventId", gameEvent, report);
            if (gameEvent.pages == null)
            {
                continue;
            }

            for (int i = 0; i < gameEvent.pages.Count; i++)
            {
                GameEventPageData page = gameEvent.pages[i];
                if (page == null)
                {
                    continue;
                }

                ValidateSpriteOwner(
                    page.stillSprite,
                    report.HeroineId,
                    "GameEventData.pages[" + i + "].stillSprite: " + gameEvent.name,
                    report);
            }
        }
    }

    private static void ValidateScheduledEvents(HeroineProfileData profile, ValidationReport report)
    {
        ScheduledEventData[] scheduledEvents =
            Resources.LoadAll<ScheduledEventData>(Fallback(profile.scheduledEventResourcePath, "ScheduledEvents"));
        report.scheduledEventCount = scheduledEvents.Length;

        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (ScheduledEventData scheduledEvent in scheduledEvents)
        {
            if (scheduledEvent == null)
            {
                continue;
            }

            ValidateResourceAssetOwner(scheduledEvent, report);
            ValidateDuplicateId(ids, scheduledEvent.name, "ScheduledEventData asset name", scheduledEvent, report);
            ValidateSpriteOwner(
                scheduledEvent.stillSprite,
                report.HeroineId,
                "ScheduledEventData.stillSprite: " + scheduledEvent.name,
                report);
        }
    }

    private static void ValidateEndings(HeroineProfileData profile, ValidationReport report)
    {
        EndingData[] endings = Resources.LoadAll<EndingData>(Fallback(profile.endingResourcePath, "Endings"));
        report.endingCount = endings.Length;

        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (EndingData ending in endings)
        {
            if (ending == null)
            {
                continue;
            }

            ValidateResourceAssetOwner(ending, report);
            ValidateRequiredId(ending.endingId, "EndingData.endingId", ending, report);
            ValidateDuplicateId(ids, ending.endingId, "EndingData.endingId", ending, report);
            ValidateSpriteOwner(ending.stillSprite, report.HeroineId, "EndingData.stillSprite: " + ending.name, report);
        }
    }

    private static void ValidateLayeredSpriteData(HeroineProfileData profile, ValidationReport report)
    {
        string resourcePath = "Heroines/" + report.HeroineId + "/HeroineLayeredSpriteData";
        HeroineLayeredSpriteData data = Resources.Load<HeroineLayeredSpriteData>(resourcePath);
        if (data == null)
        {
            report.Info("HeroineLayeredSpriteData は見つかりませんでした: " + resourcePath);
            return;
        }

        ValidateResourceAssetOwner(data, report);
        if (!string.IsNullOrWhiteSpace(data.heroineId) &&
            !string.Equals(data.heroineId, report.HeroineId, StringComparison.Ordinal))
        {
            report.Warn(
                "HeroineLayeredSpriteData.heroineId が選択中ヒロインと一致しません: " +
                data.name +
                " / heroineId=" +
                data.heroineId);
        }

        ValidateLayerEntries(data.baseBodyLayers, report.HeroineId, "baseBodyLayers", report);
        ValidateLayerEntries(data.costumeLayers, report.HeroineId, "costumeLayers", report);
        ValidateLayerEntries(data.expressionLayers, report.HeroineId, "expressionLayers", report);
        ValidateLayerEntries(data.accessoryLayers, report.HeroineId, "accessoryLayers", report);
    }

    private static void ValidateAssetCatalog(HeroineProfileData profile, ValidationReport report)
    {
        string resourcePath = "Heroines/" + report.HeroineId + "/HeroineAssetCatalog";
        HeroineAssetCatalog catalog = Resources.Load<HeroineAssetCatalog>(resourcePath);
        if (catalog == null)
        {
            report.Info("HeroineAssetCatalog は見つかりませんでした: " + resourcePath);
            return;
        }

        ValidateResourceAssetOwner(catalog, report);
        if (!string.IsNullOrWhiteSpace(catalog.heroineId) &&
            !string.Equals(catalog.heroineId, report.HeroineId, StringComparison.Ordinal))
        {
            report.Warn(
                "HeroineAssetCatalog.heroineId が選択中ヒロインと一致しません: " +
                catalog.name +
                " / heroineId=" +
                catalog.heroineId);
        }

        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        if (catalog.assets == null)
        {
            return;
        }

        foreach (HeroineAssetEntry entry in catalog.assets)
        {
            if (entry == null)
            {
                continue;
            }

            ValidateDuplicateId(ids, entry.assetId, "HeroineAssetCatalog.assetId", catalog, report);
            ValidateSpriteOwner(
                entry.sprite,
                report.HeroineId,
                "HeroineAssetCatalog.sprite: " + entry.assetId,
                report);
        }
    }

    private static void ValidateOutfitMessageOverrides(HeroineProfileData profile, ValidationReport report)
    {
        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        if (profile.outfitMessageOverrides == null)
        {
            return;
        }

        foreach (OutfitMessageOverride messageOverride in profile.outfitMessageOverrides)
        {
            if (messageOverride == null)
            {
                continue;
            }

            ValidateRequiredId(messageOverride.outfitId, "OutfitMessageOverride.outfitId", profile, report);
            ValidateDuplicateId(ids, messageOverride.outfitId, "OutfitMessageOverride.outfitId", profile, report);
        }
    }

    private static void ValidateOutfitReactionMessageOverrides(HeroineProfileData profile, ValidationReport report)
    {
        HashSet<OutfitReactionType> reactionTypes = new HashSet<OutfitReactionType>();
        if (profile.outfitReactionMessageOverrides == null)
        {
            return;
        }

        foreach (OutfitReactionMessageOverride messageOverride in profile.outfitReactionMessageOverrides)
        {
            if (messageOverride == null)
            {
                continue;
            }

            if (!reactionTypes.Add(messageOverride.reactionType))
            {
                report.Warn(
                    "OutfitReactionMessageOverride.reactionType が重複しています: " +
                    messageOverride.reactionType +
                    " / " +
                    AssetDatabase.GetAssetPath(profile));
            }
        }
    }

    private static void ValidateLayerEntries(
        List<LayerEntry> entries,
        string heroineId,
        string listName,
        ValidationReport report)
    {
        if (entries == null)
        {
            return;
        }

        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (LayerEntry entry in entries)
        {
            if (entry == null)
            {
                continue;
            }

            ValidateDuplicateRawId(ids, entry.assetId, "HeroineLayeredSpriteData." + listName, report);
            ValidateSpriteOwner(entry.sprite, heroineId, "HeroineLayeredSpriteData." + listName + ": " + entry.assetId, report);
        }
    }

    private static void ValidateResourcePath(
        string label,
        string resourcePath,
        string heroineId,
        ValidationReport report)
    {
        if (string.IsNullOrWhiteSpace(resourcePath))
        {
            report.Warn(label + " が空です。");
            return;
        }

        if (string.Equals(heroineId, "DefaultHeroine", StringComparison.Ordinal))
        {
            return;
        }

        if (resourcePath.IndexOf("DefaultHeroine", StringComparison.Ordinal) >= 0)
        {
            report.Warn(label + " が DefaultHeroine を参照しています: " + resourcePath);
        }

        string expectedPrefix = "Heroines/" + heroineId + "/";
        if (!resourcePath.StartsWith(expectedPrefix, StringComparison.Ordinal))
        {
            report.Warn(label + " がヒロイン別 ResourcePath ではない可能性があります: " + resourcePath);
        }
    }

    private static void ValidateResourceAssetOwner(UnityEngine.Object asset, ValidationReport report)
    {
        string assetPath = AssetDatabase.GetAssetPath(asset);
        if (string.IsNullOrEmpty(assetPath))
        {
            return;
        }

        string heroineId = report.HeroineId;
        if (string.Equals(heroineId, "DefaultHeroine", StringComparison.Ordinal))
        {
            return;
        }

        string expected = "/Resources/Heroines/" + heroineId + "/";
        string normalizedPath = "/" + assetPath.Replace("\\", "/");
        if (!normalizedPath.Contains(expected))
        {
            report.Warn("選択中ヒロイン以外の Resources asset を参照している可能性があります: " + assetPath);
        }
    }

    private static void ValidateSpriteOwner(
        Sprite sprite,
        string heroineId,
        string context,
        ValidationReport report)
    {
        if (sprite == null)
        {
            return;
        }

        string assetPath = AssetDatabase.GetAssetPath(sprite);
        if (string.IsNullOrEmpty(assetPath))
        {
            return;
        }

        string normalizedPath = assetPath.Replace("\\", "/");
        string heroineImagesRoot = "Assets/Images/Heroines/";
        if (!normalizedPath.StartsWith(heroineImagesRoot, StringComparison.Ordinal))
        {
            return;
        }

        string expected = heroineImagesRoot + heroineId + "/";
        if (!normalizedPath.StartsWith(expected, StringComparison.Ordinal))
        {
            report.Warn(context + " が別ヒロインの画像を参照しています: " + normalizedPath);
        }
    }

    private static void ValidateRequiredId(
        string id,
        string label,
        UnityEngine.Object owner,
        ValidationReport report)
    {
        if (!string.IsNullOrWhiteSpace(id))
        {
            return;
        }

        report.Warn(label + " が空です: " + AssetDatabase.GetAssetPath(owner));
    }

    private static void ValidateDuplicateId(
        HashSet<string> ids,
        string id,
        string label,
        UnityEngine.Object owner,
        ValidationReport report)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return;
        }

        if (!ids.Add(id))
        {
            report.Warn(label + " が重複しています: " + id + " / " + AssetDatabase.GetAssetPath(owner));
        }
    }

    private static void ValidateDuplicateRawId(
        HashSet<string> ids,
        string id,
        string label,
        ValidationReport report)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return;
        }

        if (!ids.Add(id))
        {
            report.Warn(label + " の assetId が重複しています: " + id);
        }
    }

    private static string Fallback(string value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }

    private sealed class ValidationReport
    {
        private readonly List<string> infos = new List<string>();
        private readonly List<string> warnings = new List<string>();

        public ValidationReport(HeroineProfileData profile)
        {
            Profile = profile;
            HeroineId = string.IsNullOrWhiteSpace(profile.heroineId) ? profile.name : profile.heroineId;
        }

        public HeroineProfileData Profile { get; }
        public string HeroineId { get; }
        public int actionCount;
        public int conversationCount;
        public int gameEventCount;
        public int scheduledEventCount;
        public int endingCount;

        public void Info(string message)
        {
            infos.Add(message);
        }

        public void Warn(string message)
        {
            warnings.Add(message);
        }

        public void Log()
        {
            string summary =
                "Heroine data validation: heroineId=" +
                HeroineId +
                " / actions=" +
                actionCount +
                " / conversations=" +
                conversationCount +
                " / gameEvents=" +
                gameEventCount +
                " / scheduledEvents=" +
                scheduledEventCount +
                " / endings=" +
                endingCount +
                " / warnings=" +
                warnings.Count;

            if (warnings.Count == 0)
            {
                Debug.Log(summary);
            }
            else
            {
                Debug.LogWarning(summary);
            }

            foreach (string info in infos)
            {
                Debug.Log("[HeroineDataValidator] " + info);
            }

            foreach (string warning in warnings)
            {
                Debug.LogWarning("[HeroineDataValidator] " + warning);
            }
        }

        public string CreateDialogMessage()
        {
            return
                "HeroineId: " +
                HeroineId +
                "\nActions: " +
                actionCount +
                "\nConversations: " +
                conversationCount +
                "\nGameEvents: " +
                gameEventCount +
                "\nScheduledEvents: " +
                scheduledEventCount +
                "\nEndings: " +
                endingCount +
                "\nWarnings: " +
                warnings.Count;
        }
    }
}
