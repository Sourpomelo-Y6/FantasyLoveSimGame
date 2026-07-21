using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using FantasyLoveSim.EditorTools;

public static class HeroineAssetImporter
{
    private const string MenuPath = "FantasyLoveSim/Import Heroine Export";
    private const string ProfileJsonRelativePath = "Data/heroine_profile_export.json";
    private const string AssetsJsonRelativePath = "Data/assets_export.json";
    private const string SpriteLayersJsonRelativePath = "Data/sprite_layers_export.json";
    private const string TrainingImagesJsonRelativePath = "Data/training_images_export.json";
    private const string TrainingDialoguesJsonRelativePath = "Data/training_dialogues_export.json";
    private const string ConversationsJsonRelativePath = "Data/conversations_export.json";
    private const string GameEventsJsonRelativePath = "Data/game_events_export.json";
    private const string ScheduledEventsJsonRelativePath = "Data/scheduled_events_export.json";
    private const string ActionReactionsJsonRelativePath = "Data/action_reactions_export.json";
    private const string EndingsJsonRelativePath = "Data/endings_export.json";
    private const string DefaultHeroineSpriteAssetId = "Heroine_Normal";
    private const string DefaultHeroineSpriteFileName = "Heroine_Normal.png";
    private const bool OverwriteExistingImages = false;

    [MenuItem(MenuPath)]
    public static void ImportHeroineExport()
    {
        string exportFolder = EditorUtility.OpenFolderPanel("Import Heroine Export", "", "");
        if (string.IsNullOrEmpty(exportFolder))
        {
            return;
        }

        ImportHeroineExport(exportFolder);
    }

    public static void ImportHeroineExport(string exportFolder)
    {
        HeroineImportReport report = new HeroineImportReport();
        string profileJsonPath = Path.Combine(exportFolder, ProfileJsonRelativePath);
        if (!File.Exists(profileJsonPath))
        {
            Debug.LogError("heroine_profile_export.json が見つかりません: " + profileJsonPath);
            return;
        }

        HeroineProfileExport profileExport;
        try
        {
            string json = File.ReadAllText(profileJsonPath);
            profileExport = JsonUtility.FromJson<HeroineProfileExport>(json);
        }
        catch (Exception ex)
        {
            Debug.LogError("heroine_profile_export.json の読み込みに失敗しました: " + ex.Message);
            return;
        }

        if (profileExport == null || string.IsNullOrWhiteSpace(profileExport.heroineId))
        {
            Debug.LogError("heroine_profile_export.json の heroineId が空です。");
            return;
        }

        string legacyAffectionWarning;
        if (TryGetLegacyAffectionScaleWarning(exportFolder, out legacyAffectionWarning))
        {
            Debug.LogError(legacyAffectionWarning);
            EditorUtility.DisplayDialog(
                "Heroine Export Import",
                legacyAffectionWarning,
                "OK");
            return;
        }

        EnsureFolder("Assets/Resources");
        EnsureFolder("Assets/Resources/Heroines");

        string assetPath = $"Assets/Resources/Heroines/{profileExport.heroineId}Profile.asset";
        HeroineProfileData profile = AssetDatabase.LoadAssetAtPath<HeroineProfileData>(assetPath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<HeroineProfileData>();
            AssetDatabase.CreateAsset(profile, assetPath);
        }

        ApplyProfile(profile, profileExport);
        ImportImages(exportFolder, profileExport.heroineId, report);
        ApplyDefaultHeroineSprite(profile, report.defaultSpritePath, report);
        ImportTrainingImages(exportFolder, profileExport.heroineId, report);
        ImportTrainingDialogues(exportFolder, profileExport.heroineId, report);
        HeroineSkillTreeAssetSync.Import(exportFolder, profileExport.heroineId);
        BattleMessageImportSummary battleMessageSummary = HeroineBattleMessageAssetSync.Import(exportFolder, profile);
        report.battleMessageAddedCount = battleMessageSummary.addedCount;
        report.battleMessageUpdatedCount = battleMessageSummary.updatedCount;
        report.battleMessageDeletedCount = battleMessageSummary.deletedCount;
        report.battleMessageSkippedCount = battleMessageSummary.skippedCount;
        ImportSpriteLayers(exportFolder, profileExport.heroineId, report);
        ImportConversations(exportFolder, profileExport.heroineId, report);
        ImportGameEvents(exportFolder, profileExport.heroineId, report);
        ImportScheduledEvents(exportFolder, profileExport.heroineId, report);
        ImportActionReactions(exportFolder, profileExport.heroineId, report);
        ImportEndings(exportFolder, profileExport.heroineId, report);

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        report.LogSummary(assetPath);
        EditorUtility.DisplayDialog(
            "Heroine Export Import",
            report.CreateDialogMessage(assetPath),
            "OK");
    }

    private static bool TryGetLegacyAffectionScaleWarning(
        string exportFolder,
        out string warning)
    {
        warning = string.Empty;
        string conversationsJsonPath = Path.Combine(exportFolder, ConversationsJsonRelativePath);
        if (!File.Exists(conversationsJsonPath))
        {
            return false;
        }

        try
        {
            if (IsLikelyLegacyAffectionScale(File.ReadAllText(conversationsJsonPath)))
            {
                warning =
                    "旧好感度尺度（上限100）の conversations_export.json を検出したため、" +
                    "インポートを中止しました。\n\n" +
                    "AssetTool側の好感度条件と増減値を新尺度へ移行してください。" +
                    "通常コンテンツの maxAffection は9999、旧条件値・増減値は10倍が基準です。";
                return true;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning(
                "好感度尺度の事前確認に失敗しました。通常のimport検証を続行します: " +
                ex.Message);
        }

        return false;
    }

    internal static bool IsLikelyLegacyAffectionScale(string conversationsJson)
    {
        ConversationsExport exported = JsonUtility.FromJson<ConversationsExport>(conversationsJson);
        if (exported == null || exported.items == null || exported.items.Length < 3)
        {
            return false;
        }

        int conditionedItemCount = 0;
        int legacyMaximumCount = 0;
        foreach (ConversationExportItem item in exported.items)
        {
            if (item == null || item.conditions == null)
            {
                continue;
            }

            conditionedItemCount++;
            if (item.conditions.maxAffection == 100)
            {
                legacyMaximumCount++;
            }
        }

        // 旧仕様では全会話の上限が100だった。全件一致するexportだけを旧尺度と判定する。
        return conditionedItemCount >= 3 && legacyMaximumCount == conditionedItemCount;
    }

    private static void ApplyProfile(HeroineProfileData profile, HeroineProfileExport profileExport)
    {
        profile.heroineId = profileExport.heroineId;
        profile.displayName = string.IsNullOrWhiteSpace(profileExport.displayName)
            ? profileExport.heroineId
            : profileExport.displayName;
        if (profileExport.firstPerson != null)
        {
            profile.heroineFirstPerson = profileExport.firstPerson;
        }
        if (profileExport.secondPerson != null)
        {
            profile.playerSecondPerson = profileExport.secondPerson;
        }
        profile.initialDialogueMessage = ResolveProfileText(
            profileExport.initialDialogueMessage,
            profile.initialDialogueMessage,
            "今日は何を話しましょうか？");
        profile.nextActionPrompt = ResolveProfileText(
            profileExport.nextActionPrompt,
            profile.nextActionPrompt,
            "次は何をしましょうか？");
        profile.morningGreeting = ResolveProfileText(
            profileExport.morningGreeting,
            profile.morningGreeting,
            "おはようございます。今日もよろしくお願いしますね。");
        profile.goodNightGreeting = ResolveProfileText(
            profileExport.goodNightGreeting,
            profile.goodNightGreeting,
            "もう夜も遅いですね。おやすみなさい。また明日。");
        profile.gameStartFallbackMessage = ResolveProfileText(
            profileExport.gameStartFallbackMessage,
            profile.gameStartFallbackMessage,
            "新しい物語が始まります。");
        profile.gameStartFollowUpMessage = ResolveProfileText(
            profileExport.gameStartFollowUpMessage,
            profile.gameStartFollowUpMessage,
            "今日は何を話しましょうか？");
        ApplyOutfitMessageOverrides(profile.outfitMessageOverrides, profileExport.outfitMessageOverrides);
        ApplyOutfitReactionMessageOverrides(
            profile.outfitReactionMessageOverrides,
            profileExport.outfitReactionMessageOverrides);
        ApplyBattleSkills(profile.battleSkills, profileExport.battleSkills);
        profile.conversationResourcePath = ResolveResourcePath(profileExport.conversationResourcePath, profile.conversationResourcePath, profileExport.heroineId, "Conversations");
        profile.gameEventResourcePath = ResolveResourcePath(profileExport.gameEventResourcePath, profile.gameEventResourcePath, profileExport.heroineId, "GameEvents");
        profile.actionResourcePath = ResolveResourcePath(profileExport.actionResourcePath, profile.actionResourcePath, profileExport.heroineId, "Actions");
        profile.scheduledEventResourcePath = ResolveResourcePath(profileExport.scheduledEventResourcePath, profile.scheduledEventResourcePath, profileExport.heroineId, "ScheduledEvents");
        profile.battleResultEventResourcePath = ResolveResourcePath(profileExport.battleResultEventResourcePath, profile.battleResultEventResourcePath, profileExport.heroineId, "BattleResultEvents");
        profile.battlePanelResultMessageResourcePath = ResolveResourcePath(profileExport.battlePanelResultMessageResourcePath, profile.battlePanelResultMessageResourcePath, profileExport.heroineId, "BattlePanelResultMessages");
        profile.endingResourcePath = ResolveResourcePath(profileExport.endingResourcePath, profile.endingResourcePath, profileExport.heroineId, "Endings");
    }

    private static string ResolveResourcePath(string exportedPath, string currentPath, string heroineId, string defaultLeaf)
    {
        if (exportedPath != null)
        {
            return exportedPath;
        }

        string defaultPath = $"Heroines/{heroineId}/{defaultLeaf}";
        return string.IsNullOrWhiteSpace(currentPath) || string.Equals(currentPath, defaultLeaf, StringComparison.Ordinal)
            ? defaultPath
            : currentPath;
    }

    private static void ApplyBattleSkills(List<HeroineBattleSkillData> target, HeroineBattleSkillExport[] source)
    {
        // null は古いJSONで項目が省略された状態。既存値を維持する。
        if (target == null || source == null)
        {
            return;
        }

        List<BattleSkillSyncItem> normalized = BattleSkillSyncService.Normalize(
            source.Select(item => item == null ? null : new BattleSkillSyncItem
            {
                SkillId = item.skillId,
                DisplayName = item.displayName,
                EffectType = item.effectType,
                Target = item.target,
                Cost = item.cost,
                Power = item.power,
                AffectedStat = item.affectedStat,
                StatusDurationTurns = item.statusDurationTurns,
                UseChancePercent = item.useChancePercent,
                Priority = item.priority,
                MaxUsesPerBattle = item.maxUsesPerBattle
            }));
        target.Clear();
        foreach (BattleSkillSyncItem item in normalized)
        {
            target.Add(new HeroineBattleSkillData
            {
                skillId = item.SkillId,
                displayName = item.DisplayName,
                effectType = ParseEnumOrDefault(item.EffectType, SkillEffectType.Damage),
                target = ParseEnumOrDefault(item.Target, HeroineSkillTarget.Enemy),
                cost = Math.Max(0, item.Cost),
                power = item.Power,
                affectedStat = ParseEnumOrDefault(item.AffectedStat, SkillBattleStat.Attack),
                statusDurationTurns = Math.Max(1, item.StatusDurationTurns),
                useChancePercent = Mathf.Clamp(item.UseChancePercent, 0, 100),
                priority = item.Priority,
                maxUsesPerBattle = item.MaxUsesPerBattle
            });
        }
    }

    internal static void ApplyProfileJsonForTests(HeroineProfileData profile, string json)
    {
        ApplyProfile(profile, JsonUtility.FromJson<HeroineProfileExport>(json));
    }

    private static string ResolveProfileText(string exportedText, string currentText, string fallback)
    {
        if (!string.IsNullOrWhiteSpace(exportedText))
        {
            return exportedText;
        }

        if (!string.IsNullOrWhiteSpace(currentText))
        {
            return currentText;
        }

        return fallback;
    }

    private static void ApplyOutfitMessageOverrides(
        List<OutfitMessageOverride> target,
        OutfitMessageOverrideExport[] source)
    {
        if (target == null || source == null)
        {
            return;
        }

        target.Clear();
        foreach (OutfitMessageOverrideExport item in source)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.outfitId))
            {
                continue;
            }

            target.Add(new OutfitMessageOverride
            {
                outfitId = item.outfitId,
                lockedMessage = item.lockedMessage ?? string.Empty,
                changedMessage = item.changedMessage ?? string.Empty,
                changedExpressionId = item.changedExpressionId ?? string.Empty
            });
        }
    }

    private static void ApplyOutfitReactionMessageOverrides(
        List<OutfitReactionMessageOverride> target,
        OutfitReactionMessageOverrideExport[] source)
    {
        if (target == null || source == null)
        {
            return;
        }

        target.Clear();
        foreach (OutfitReactionMessageOverrideExport item in source)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.message))
            {
                continue;
            }

            target.Add(new OutfitReactionMessageOverride
            {
                reactionType = ParseEnumOrDefault(item.reactionType, OutfitReactionType.Praise),
                message = item.message,
                expressionId = item.expressionId ?? string.Empty
            });
        }
    }

    private static void ApplyDefaultHeroineSprite(
        HeroineProfileData profile,
        string defaultSpritePath,
        HeroineImportReport report)
    {
        if (string.IsNullOrWhiteSpace(defaultSpritePath))
        {
            Debug.Log("代表立ち絵候補が見つからないため、defaultHeroineSprite は変更しませんでした。");
            return;
        }

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(defaultSpritePath);
        if (sprite == null)
        {
            report.Warn("代表立ち絵候補を Sprite として読み込めませんでした: " + defaultSpritePath);
            return;
        }

        profile.defaultHeroineSprite = sprite;
    }

    private static void ImportImages(
        string exportFolder,
        string heroineId,
        HeroineImportReport report)
    {
        string assetsJsonPath = Path.Combine(exportFolder, AssetsJsonRelativePath);
        if (!File.Exists(assetsJsonPath))
        {
            Debug.Log("assets_export.json が見つからないため、画像 import はスキップしました: " + assetsJsonPath);
            return;
        }

        AssetsExport assetsExport;
        try
        {
            string json = File.ReadAllText(assetsJsonPath);
            assetsExport = JsonUtility.FromJson<AssetsExport>(json);
        }
        catch (Exception ex)
        {
            Debug.LogError("assets_export.json の読み込みに失敗しました: " + ex.Message);
            return;
        }

        if (assetsExport == null || assetsExport.assets == null)
        {
            report.Warn("assets_export.json に assets がありません。");
            return;
        }

        string effectiveHeroineId = string.IsNullOrWhiteSpace(assetsExport.heroineId)
            ? heroineId
            : assetsExport.heroineId;
        HeroineAssetCatalog catalog = LoadOrCreateAssetCatalog(effectiveHeroineId);
        catalog.heroineId = effectiveHeroineId;
        if (catalog.assets == null)
        {
            catalog.assets = new List<HeroineAssetEntry>();
        }

        catalog.assets.Clear();
        HashSet<string> importedAssetIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (HeroineAssetExport asset in assetsExport.assets)
        {
            if (asset == null || !ShouldImportAsset(asset))
            {
                continue;
            }

            if (!CanImportCatalogAsset(asset, importedAssetIds, report))
            {
                continue;
            }

            string sourcePath = ResolveExportPath(exportFolder, asset.exportImagePath, asset.usage, asset.fileName);
            if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath))
            {
                report.Warn("画像ファイルが見つからないためスキップしました: " + sourcePath);
                continue;
            }

            string unityPath = ResolveUnityImagePath(effectiveHeroineId, asset);
            if (!IsValidUnityAssetPath(unityPath))
            {
                report.Warn("unityImagePath が Assets 配下ではないためスキップしました: " + unityPath);
                continue;
            }

            EnsureFolderForAssetPath(unityPath);
            if (File.Exists(unityPath) && !OverwriteExistingImages)
            {
                report.Warn("既存画像があるため上書きせずスキップしました: " + unityPath);
                AssetDatabase.ImportAsset(unityPath);
                EnsureSpriteImportSettings(unityPath);
                SetDefaultSpriteCandidateIfNeeded(report, asset, unityPath);
                AddCatalogEntry(catalog, asset, unityPath, report);
                continue;
            }

            File.Copy(sourcePath, unityPath, OverwriteExistingImages);
            AssetDatabase.ImportAsset(unityPath);
            EnsureSpriteImportSettings(unityPath);
            SetDefaultSpriteCandidateIfNeeded(report, asset, unityPath);
            AddCatalogEntry(catalog, asset, unityPath, report);
            report.copiedImageCount++;
        }

        report.catalogAssetCount = catalog.assets.Count;
        EditorUtility.SetDirty(catalog);
    }

    private static HeroineAssetCatalog LoadOrCreateAssetCatalog(string heroineId)
    {
        string heroineResourceFolderPath = $"Assets/Resources/Heroines/{heroineId}";
        EnsureFolder(heroineResourceFolderPath);

        string assetPath = $"{heroineResourceFolderPath}/HeroineAssetCatalog.asset";
        HeroineAssetCatalog catalog = AssetDatabase.LoadAssetAtPath<HeroineAssetCatalog>(assetPath);
        if (catalog != null)
        {
            return catalog;
        }

        catalog = ScriptableObject.CreateInstance<HeroineAssetCatalog>();
        AssetDatabase.CreateAsset(catalog, assetPath);
        return catalog;
    }

    private static void ImportTrainingImages(
        string exportFolder,
        string heroineId,
        HeroineImportReport report)
    {
        string jsonPath = Path.Combine(exportFolder, TrainingImagesJsonRelativePath);
        if (!File.Exists(jsonPath))
        {
            Debug.Log(
                "training_images_export.json が見つからないため、既存の訓練画像設定は変更しません: " +
                jsonPath);
            return;
        }

        TrainingImagesExport exported;
        try
        {
            exported = JsonUtility.FromJson<TrainingImagesExport>(File.ReadAllText(jsonPath));
        }
        catch (Exception ex)
        {
            report.Warn("training_images_export.json の読み込みに失敗しました: " + ex.Message);
            return;
        }

        if (exported == null)
        {
            report.Warn("training_images_export.json を読み込めませんでした。");
            return;
        }

        if (!string.IsNullOrWhiteSpace(exported.heroineId) &&
            !string.Equals(exported.heroineId, heroineId, StringComparison.Ordinal))
        {
            report.Warn(
                "training_images_export.json の heroineId が profile と一致しないためスキップしました: " +
                exported.heroineId + " / " + heroineId);
            report.trainingImageSkippedCount++;
            return;
        }

        HeroineAssetCatalog catalog = LoadOrCreateAssetCatalog(heroineId);
        Dictionary<string, HeroineAssetEntry> assetsById =
            CreateCatalogAssetEntryMap(catalog, report);
        report.trainingImageCount = assetsById.Values.Count(
            entry => entry != null &&
                string.Equals(entry.usage, "Training", StringComparison.OrdinalIgnoreCase) &&
                entry.sprite != null);

        string resourceFolder = $"Assets/Resources/Heroines/{heroineId}/TrainingImages";
        EnsureFolder(resourceFolder);
        string assetPath = resourceFolder + "/HeroineTrainingImageData.asset";
        HeroineTrainingImageData data =
            AssetDatabase.LoadAssetAtPath<HeroineTrainingImageData>(assetPath);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<HeroineTrainingImageData>();
            AssetDatabase.CreateAsset(data, assetPath);
        }

        data.heroineId = heroineId;
        ApplyTrainingImageDefaults(data, exported.defaults, assetsById, report);
        if (data.entries == null)
        {
            data.entries = new List<HeroineTrainingImageEntry>();
        }
        data.entries.Clear();

        HashSet<string> trainingIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> knownTrainingIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (TrainingData training in Resources.LoadAll<TrainingData>("Training"))
        {
            if (training != null && !string.IsNullOrWhiteSpace(training.trainingId))
            {
                knownTrainingIds.Add(training.trainingId);
            }
        }
        if (exported.items != null)
        {
            foreach (TrainingImageExportItem item in exported.items)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.trainingId))
                {
                    report.Warn("trainingId が空の訓練画像設定をスキップしました。");
                    report.trainingImageSkippedCount++;
                    continue;
                }
                if (!trainingIds.Add(item.trainingId))
                {
                    report.Warn("trainingId が重複している訓練画像設定をスキップしました: " + item.trainingId);
                    report.trainingImageSkippedCount++;
                    continue;
                }
                if (!knownTrainingIds.Contains(item.trainingId))
                {
                    report.Warn("存在しないtrainingIdの訓練画像設定をスキップしました: " + item.trainingId);
                    report.trainingImageSkippedCount++;
                    continue;
                }

                data.entries.Add(new HeroineTrainingImageEntry
                {
                    trainingId = item.trainingId,
                    selectedBeforeFirstStepSprite = ResolveTrainingSprite(
                        item.beforeFirstStepImageAssetId, assetsById, report),
                    selectedAfterFirstStepSprite = ResolveTrainingSprite(
                        item.afterFirstStepImageAssetId, assetsById, report),
                    playerLpConsumedSprite = ResolveTrainingSprite(
                        item.playerLpConsumedImageAssetId, assetsById, report),
                    heroineLpConsumedSprite = ResolveTrainingSprite(
                        item.heroineLpConsumedImageAssetId, assetsById, report),
                    simultaneousLpConsumedSprite = ResolveTrainingSprite(
                        item.simultaneousLpConsumedImageAssetId, assetsById, report)
                });
            }
        }

        report.trainingImageEntryCount = data.entries.Count;
        report.trainingImageSettingsUpdated = true;
        EditorUtility.SetDirty(data);
    }

    private static Dictionary<string, HeroineAssetEntry> CreateCatalogAssetEntryMap(
        HeroineAssetCatalog catalog,
        HeroineImportReport report)
    {
        Dictionary<string, HeroineAssetEntry> result =
            new Dictionary<string, HeroineAssetEntry>(StringComparer.Ordinal);
        if (catalog == null || catalog.assets == null)
        {
            return result;
        }

        foreach (HeroineAssetEntry entry in catalog.assets)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.assetId))
            {
                continue;
            }
            if (result.ContainsKey(entry.assetId))
            {
                report.Warn("HeroineAssetCatalog の assetId が重複しています: " + entry.assetId);
                continue;
            }
            result.Add(entry.assetId, entry);
        }
        return result;
    }

    internal static void ImportTrainingDialogues(
        string exportFolder,
        string heroineId,
        HeroineImportReport report)
    {
        string jsonPath = Path.Combine(exportFolder, TrainingDialoguesJsonRelativePath);
        if (!File.Exists(jsonPath))
        {
            Debug.Log("training_dialogues_export.json がないため、既存の訓練セリフは変更しません: " + jsonPath);
            return;
        }

        TrainingDialoguesExport exported;
        try
        {
            exported = JsonUtility.FromJson<TrainingDialoguesExport>(File.ReadAllText(jsonPath));
        }
        catch (Exception ex)
        {
            report.Warn("training_dialogues_export.json の読み込みに失敗しました: " + ex.Message);
            return;
        }
        if (exported == null)
        {
            report.Warn("training_dialogues_export.json を読み込めなかったためスキップしました。");
            return;
        }
        if (!TrainingDialogueSyncService.ValidateImportHeader(
            exported.schemaVersion,
            1,
            exported.heroineId,
            heroineId,
            report.Warn))
        {
            return;
        }

        string resourceFolder = $"Assets/Resources/Heroines/{heroineId}/TrainingDialogues";
        EnsureFolder(resourceFolder);
        string assetPath = resourceFolder + "/HeroineTrainingDialogueData.asset";
        HeroineTrainingDialogueData data = AssetDatabase.LoadAssetAtPath<HeroineTrainingDialogueData>(assetPath);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<HeroineTrainingDialogueData>();
            AssetDatabase.CreateAsset(data, assetPath);
        }
        data.heroineId = heroineId;
        data.entries = new List<HeroineTrainingDialogueEntry>();

        HashSet<string> knownTrainingIds = new HashSet<string>(
            Resources.LoadAll<TrainingData>("Training")
                .Where(training => training != null && !string.IsNullOrWhiteSpace(training.trainingId))
                .Select(training => training.trainingId),
            StringComparer.Ordinal);
        List<TrainingDialogueSyncItem> importedItems = TrainingDialogueSyncService.BuildImportItems(
            (exported.items ?? Array.Empty<TrainingDialogueExportItem>()).Select(item =>
                item == null ? null : new TrainingDialogueSyncItem
                {
                    TrainingId = item.trainingId,
                    VisualState = item.visualState,
                    Messages = (item.messages ?? Array.Empty<string>()).ToList()
                }),
            knownTrainingIds,
            report.Warn);
        foreach (TrainingDialogueSyncItem item in importedItems)
        {
            if (!Enum.TryParse(item.VisualState, out TrainingVisualState state))
            {
                continue;
            }
            data.entries.Add(new HeroineTrainingDialogueEntry
            {
                trainingId = item.TrainingId,
                visualState = state,
                messages = item.Messages
            });
        }
        report.trainingDialogueEntryCount = data.entries.Count;
        EditorUtility.SetDirty(data);
    }

    private static void ApplyTrainingImageDefaults(
        HeroineTrainingImageData data,
        TrainingImageExportDefaults defaults,
        Dictionary<string, HeroineAssetEntry> assetsById,
        HeroineImportReport report)
    {
        defaults = defaults ?? new TrainingImageExportDefaults();
        data.defaultBeforeFirstStepSprite = ResolveTrainingSprite(
            defaults.beforeFirstStepImageAssetId, assetsById, report);
        data.defaultAfterFirstStepSprite = ResolveTrainingSprite(
            defaults.afterFirstStepImageAssetId, assetsById, report);
        data.defaultPlayerLpConsumedSprite = ResolveTrainingSprite(
            defaults.playerLpConsumedImageAssetId, assetsById, report);
        data.defaultHeroineLpConsumedSprite = ResolveTrainingSprite(
            defaults.heroineLpConsumedImageAssetId, assetsById, report);
        data.defaultSimultaneousLpConsumedSprite = ResolveTrainingSprite(
            defaults.simultaneousLpConsumedImageAssetId, assetsById, report);
    }

    private static Sprite ResolveTrainingSprite(
        string assetId,
        Dictionary<string, HeroineAssetEntry> assetsById,
        HeroineImportReport report)
    {
        if (string.IsNullOrWhiteSpace(assetId))
        {
            return null;
        }
        if (!assetsById.TryGetValue(assetId, out HeroineAssetEntry entry) || entry == null)
        {
            report.Warn("訓練画像のAssetIdをカタログから解決できませんでした: " + assetId);
            report.trainingImageUnresolvedCount++;
            return null;
        }
        if (!string.Equals(entry.usage, "Training", StringComparison.OrdinalIgnoreCase))
        {
            report.Warn("訓練画像のUsageがTrainingではありません: " + assetId + " / " + entry.usage);
        }
        if (entry.sprite == null)
        {
            report.Warn("訓練画像のSpriteが未設定です: " + assetId);
            report.trainingImageUnresolvedCount++;
            return null;
        }
        return entry.sprite;
    }

    private static bool CanImportCatalogAsset(
        HeroineAssetExport asset,
        HashSet<string> importedAssetIds,
        HeroineImportReport report)
    {
        if (string.IsNullOrWhiteSpace(asset.assetId))
        {
            report.Warn("assetId が空の画像 asset をスキップしました: " + asset.fileName);
            return false;
        }

        if (!importedAssetIds.Add(asset.assetId))
        {
            report.Warn("assetId が重複している画像 asset をスキップしました: " + asset.assetId);
            return false;
        }

        return true;
    }

    private static void AddCatalogEntry(
        HeroineAssetCatalog catalog,
        HeroineAssetExport asset,
        string unityPath,
        HeroineImportReport report)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(unityPath);
        if (sprite == null)
        {
            report.Warn("HeroineAssetCatalog 用 Sprite を解決できませんでした: " + unityPath);
        }

        catalog.assets.Add(new HeroineAssetEntry
        {
            assetId = asset.assetId,
            usage = asset.usage,
            status = asset.status,
            fileName = asset.fileName,
            memo = asset.memo,
            sprite = sprite,
            unityImagePath = unityPath,
            exportPromptPath = asset.exportPromptPath
        });
    }

    private static void ImportSpriteLayers(
        string exportFolder,
        string heroineId,
        HeroineImportReport report)
    {
        string spriteLayersJsonPath = Path.Combine(exportFolder, SpriteLayersJsonRelativePath);
        if (!File.Exists(spriteLayersJsonPath))
        {
            Debug.Log("sprite_layers_export.json が見つからないため、レイヤー import はスキップしました: " + spriteLayersJsonPath);
            return;
        }

        SpriteLayersExport spriteLayersExport;
        try
        {
            string json = File.ReadAllText(spriteLayersJsonPath);
            spriteLayersExport = JsonUtility.FromJson<SpriteLayersExport>(json);
        }
        catch (Exception ex)
        {
            Debug.LogError("sprite_layers_export.json の読み込みに失敗しました: " + ex.Message);
            return;
        }

        if (spriteLayersExport == null || spriteLayersExport.layers == null)
        {
            report.Warn("sprite_layers_export.json に layers がありません。");
            return;
        }

        if (!string.IsNullOrWhiteSpace(spriteLayersExport.heroineId)
            && !string.Equals(spriteLayersExport.heroineId, heroineId, StringComparison.Ordinal))
        {
            report.Warn($"sprite_layers_export.json の heroineId が profile と一致しません: {spriteLayersExport.heroineId} / {heroineId}");
        }

        string heroineResourceFolderPath = $"Assets/Resources/Heroines/{heroineId}";
        EnsureFolder(heroineResourceFolderPath);

        string assetPath = $"{heroineResourceFolderPath}/HeroineLayeredSpriteData.asset";
        HeroineLayeredSpriteData layeredSpriteData =
            AssetDatabase.LoadAssetAtPath<HeroineLayeredSpriteData>(assetPath);
        if (layeredSpriteData == null)
        {
            layeredSpriteData = ScriptableObject.CreateInstance<HeroineLayeredSpriteData>();
            AssetDatabase.CreateAsset(layeredSpriteData, assetPath);
        }

        EnsureLayerLists(layeredSpriteData);
        layeredSpriteData.heroineId = heroineId;
        layeredSpriteData.baseBodyLayers.Clear();
        layeredSpriteData.costumeLayers.Clear();
        layeredSpriteData.expressionLayers.Clear();
        layeredSpriteData.accessoryLayers.Clear();

        HeroineAssetCatalog catalog = LoadOrCreateAssetCatalog(heroineId);
        Dictionary<string, Sprite> spritesByAssetId = CreateCatalogSpriteMap(catalog, report);
        HashSet<string> importedLayerAssetIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> importedLayerKeys = new HashSet<string>(StringComparer.Ordinal);

        foreach (SpriteLayerExport layer in spriteLayersExport.layers)
        {
            if (!CanImportSpriteLayer(layer, importedLayerAssetIds, importedLayerKeys, report))
            {
                continue;
            }

            LayerEntry entry = CreateLayerEntry(layer, spritesByAssetId, report);
            AddLayerEntry(layeredSpriteData, entry, report);
        }

        SortLayerEntries(layeredSpriteData);
        ValidateLayeredSpriteData(layeredSpriteData, report);
        report.layerCount =
            layeredSpriteData.baseBodyLayers.Count +
            layeredSpriteData.costumeLayers.Count +
            layeredSpriteData.expressionLayers.Count +
            layeredSpriteData.accessoryLayers.Count;

        EditorUtility.SetDirty(layeredSpriteData);
    }

    private static void EnsureLayerLists(HeroineLayeredSpriteData data)
    {
        if (data.baseBodyLayers == null)
        {
            data.baseBodyLayers = new List<LayerEntry>();
        }

        if (data.costumeLayers == null)
        {
            data.costumeLayers = new List<LayerEntry>();
        }

        if (data.expressionLayers == null)
        {
            data.expressionLayers = new List<LayerEntry>();
        }

        if (data.accessoryLayers == null)
        {
            data.accessoryLayers = new List<LayerEntry>();
        }
    }

    private static Dictionary<string, Sprite> CreateCatalogSpriteMap(
        HeroineAssetCatalog catalog,
        HeroineImportReport report)
    {
        Dictionary<string, Sprite> spritesByAssetId =
            new Dictionary<string, Sprite>(StringComparer.Ordinal);
        if (catalog == null || catalog.assets == null)
        {
            return spritesByAssetId;
        }

        foreach (HeroineAssetEntry asset in catalog.assets)
        {
            if (asset == null || string.IsNullOrWhiteSpace(asset.assetId))
            {
                continue;
            }

            if (spritesByAssetId.ContainsKey(asset.assetId))
            {
                report.Warn("HeroineAssetCatalog の assetId が重複しています: " + asset.assetId);
                continue;
            }

            spritesByAssetId.Add(asset.assetId, asset.sprite);
        }

        return spritesByAssetId;
    }

    private static bool CanImportSpriteLayer(
        SpriteLayerExport layer,
        HashSet<string> importedLayerAssetIds,
        HashSet<string> importedLayerKeys,
        HeroineImportReport report)
    {
        if (layer == null)
        {
            report.Warn("空の sprite layer をスキップしました。");
            return false;
        }

        if (string.IsNullOrWhiteSpace(layer.assetId))
        {
            report.Warn("assetId が空の sprite layer をスキップしました: " + layer.fileName);
            return false;
        }

        if (!importedLayerAssetIds.Add(layer.assetId))
        {
            report.Warn("assetId が重複している sprite layer をスキップしました: " + layer.assetId);
            return false;
        }

        if (!IsKnownLayerKind(layer.layerKind))
        {
            report.Warn("未知の layerKind の sprite layer をスキップしました: " + layer.layerKind);
            return false;
        }

        if (string.Equals(layer.layerKind, "Costume", StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(layer.costumeId))
        {
            report.Warn("Costume なのに costumeId が空の sprite layer をスキップしました: " + layer.assetId);
            return false;
        }

        if (string.Equals(layer.layerKind, "Expression", StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(layer.expressionId))
        {
            report.Warn("Expression なのに expressionId が空の sprite layer をスキップしました: " + layer.assetId);
            return false;
        }

        string layerKey = NormalizeLayerKind(layer.layerKind) + "|" + layer.costumeId + "|" + layer.expressionId;
        if (!importedLayerKeys.Add(layerKey))
        {
            report.Warn("同じ layerKind + costumeId + expressionId の sprite layer があります: " + layerKey);
        }

        return true;
    }

    private static bool IsKnownLayerKind(string layerKind)
    {
        string normalizedLayerKind = NormalizeLayerKind(layerKind);
        return normalizedLayerKind == "BaseBody"
            || normalizedLayerKind == "Costume"
            || normalizedLayerKind == "Expression"
            || normalizedLayerKind == "Accessory";
    }

    private static string NormalizeLayerKind(string layerKind)
    {
        if (string.Equals(layerKind, "BaseBody", StringComparison.OrdinalIgnoreCase))
        {
            return "BaseBody";
        }

        if (string.Equals(layerKind, "Costume", StringComparison.OrdinalIgnoreCase))
        {
            return "Costume";
        }

        if (string.Equals(layerKind, "Expression", StringComparison.OrdinalIgnoreCase))
        {
            return "Expression";
        }

        if (string.Equals(layerKind, "Accessory", StringComparison.OrdinalIgnoreCase))
        {
            return "Accessory";
        }

        return layerKind;
    }

    private static LayerEntry CreateLayerEntry(
        SpriteLayerExport layer,
        Dictionary<string, Sprite> spritesByAssetId,
        HeroineImportReport report)
    {
        Sprite sprite = null;
        if (!spritesByAssetId.TryGetValue(layer.assetId, out sprite) || sprite == null)
        {
            sprite = AssetDatabase.LoadAssetAtPath<Sprite>(NormalizeAssetPath(layer.unityImagePath));
            if (sprite == null)
            {
                report.Warn("sprite layer の Sprite を解決できませんでした: " + layer.assetId);
            }
        }

        return new LayerEntry
        {
            assetId = layer.assetId,
            layerKind = NormalizeLayerKind(layer.layerKind),
            costumeId = layer.costumeId,
            expressionId = layer.expressionId,
            displayName = layer.displayName,
            drawOrder = layer.drawOrder,
            sprite = sprite
        };
    }

    private static void AddLayerEntry(
        HeroineLayeredSpriteData data,
        LayerEntry entry,
        HeroineImportReport report)
    {
        switch (entry.layerKind)
        {
            case "BaseBody":
                data.baseBodyLayers.Add(entry);
                break;
            case "Costume":
                data.costumeLayers.Add(entry);
                break;
            case "Expression":
                data.expressionLayers.Add(entry);
                break;
            case "Accessory":
                data.accessoryLayers.Add(entry);
                break;
            default:
                report.Warn("未知の layerKind の LayerEntry をスキップしました: " + entry.layerKind);
                break;
        }
    }

    private static void SortLayerEntries(HeroineLayeredSpriteData data)
    {
        Comparison<LayerEntry> comparison = (left, right) => left.drawOrder.CompareTo(right.drawOrder);
        data.baseBodyLayers.Sort(comparison);
        data.costumeLayers.Sort(comparison);
        data.expressionLayers.Sort(comparison);
        data.accessoryLayers.Sort(comparison);
    }

    private static void ValidateLayeredSpriteData(
        HeroineLayeredSpriteData data,
        HeroineImportReport report)
    {
        if (data.baseBodyLayers.Count == 0)
        {
            report.Warn("HeroineLayeredSpriteData に BaseBody がありません。");
        }

        bool hasDefaultCostume = data.costumeLayers.Exists(
            layer => string.Equals(layer.costumeId, data.defaultCostumeId, StringComparison.Ordinal));
        if (!hasDefaultCostume)
        {
            report.Warn("HeroineLayeredSpriteData に Default 衣装がありません。");
        }

        bool hasNeutralExpression = data.expressionLayers.Exists(
            layer => string.Equals(layer.expressionId, data.defaultExpressionId, StringComparison.Ordinal));
        if (!hasNeutralExpression)
        {
            report.Warn("HeroineLayeredSpriteData に Neutral 表情がありません。");
        }
    }

    private static void ImportConversations(
        string exportFolder,
        string heroineId,
        HeroineImportReport report)
    {
        string conversationsJsonPath = Path.Combine(exportFolder, ConversationsJsonRelativePath);
        if (!File.Exists(conversationsJsonPath))
        {
            Debug.Log("conversations_export.json が見つからないため、会話 import はスキップしました: " + conversationsJsonPath);
            return;
        }

        ConversationsExport conversationsExport;
        try
        {
            string json = File.ReadAllText(conversationsJsonPath);
            conversationsExport = JsonUtility.FromJson<ConversationsExport>(json);
        }
        catch (Exception ex)
        {
            Debug.LogError("conversations_export.json の読み込みに失敗しました: " + ex.Message);
            return;
        }

        if (conversationsExport == null || conversationsExport.items == null)
        {
            report.Warn("conversations_export.json に items がありません。");
            return;
        }

        if (!string.IsNullOrWhiteSpace(conversationsExport.heroineId)
            && !string.Equals(conversationsExport.heroineId, heroineId, StringComparison.Ordinal))
        {
            report.Warn($"conversations_export.json の heroineId が profile と一致しません: {conversationsExport.heroineId} / {heroineId}");
        }

        string conversationFolderPath = $"Assets/Resources/Heroines/{heroineId}/Conversations";
        EnsureFolder(conversationFolderPath);
        DeleteLegacyConversationContainer(heroineId, report);

        int importedCount = 0;
        HashSet<string> importedIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (ConversationExportItem item in conversationsExport.items)
        {
            if (!CanImportConversation(item, importedIds, report))
            {
                continue;
            }

            string assetPath = $"{conversationFolderPath}/{ToSafeAssetFileName(item.id)}.asset";
            ConversationData conversation = AssetDatabase.LoadAssetAtPath<ConversationData>(assetPath);
            if (conversation == null)
            {
                conversation = ScriptableObject.CreateInstance<ConversationData>();
                AssetDatabase.CreateAsset(conversation, assetPath);
            }

            conversation.name = item.id;
            conversation.heroineId = heroineId;
            conversation.items.Clear();
            ApplyConversation(conversation, item, report);
            EditorUtility.SetDirty(conversation);
            importedIds.Add(item.id);
            importedCount++;
        }

        report.conversationCount = importedCount;
    }

    private static void DeleteLegacyConversationContainer(
        string heroineId,
        HeroineImportReport report)
    {
        string legacyAssetPath = $"Assets/Resources/Heroines/{heroineId}/Conversations.asset";
        ConversationData legacyContainer = AssetDatabase.LoadAssetAtPath<ConversationData>(legacyAssetPath);
        if (legacyContainer == null)
        {
            return;
        }

        if (legacyContainer.items == null || legacyContainer.items.Count == 0)
        {
            return;
        }

        if (AssetDatabase.DeleteAsset(legacyAssetPath))
        {
            report.Warn("旧 Conversations.asset container を削除しました: " + legacyAssetPath);
        }
        else
        {
            report.Warn("旧 Conversations.asset container の削除に失敗しました: " + legacyAssetPath);
        }
    }

    private static bool CanImportConversation(
        ConversationExportItem item,
        HashSet<string> importedIds,
        HeroineImportReport report)
    {
        if (item == null || string.IsNullOrWhiteSpace(item.id))
        {
            report.Warn("id が空の会話 item をスキップしました。");
            return false;
        }

        if (importedIds.Contains(item.id))
        {
            report.Warn("id が重複している会話 item をスキップしました: " + item.id);
            return false;
        }

        string heroineLine = GetFirstLineText(item);
        if (string.IsNullOrWhiteSpace(heroineLine))
        {
            report.Warn("lines が空の会話 item をスキップしました: " + item.id);
            return false;
        }

        return true;
    }

    private static void ApplyConversation(
        ConversationDataItem conversation,
        ConversationExportItem item,
        HeroineImportReport report)
    {
        ConversationExportConditions conditions = item.conditions ?? new ConversationExportConditions();

        conversation.conversationId = item.id;
        conversation.genre = ParseEnumOrDefault(item.category, ConversationGenre.Daily);
        conversation.type = ConversationType.Simple;
        conversation.heroineLine = GetFirstLineText(item);
        conversation.expressionId = GetFirstLineExpression(item);
        ApplyConversationLines(conversation.lines, item);
        ApplyConversationChoices(conversation, item, report);
        conversation.priority = item.priority;
        conversation.showOnce = conditions.once;
        conversation.minAffection = Math.Max(0, conditions.minAffection);
        conversation.maxAffection = conditions.maxAffection > 0 ? conditions.maxAffection : 9999;
        conversation.costumeId = conditions.costumeId ?? string.Empty;

        ApplySingleEnumCondition(
            conditions.timeOfDay,
            conversation.allowedTimeSlots,
            value => conversation.anyTimeSlot = value,
            report);
        ApplySingleEnumCondition(
            conditions.season,
            conversation.allowedSeasons,
            value => conversation.anySeason = value,
            report);
        ApplySingleEnumCondition(
            conditions.weather,
            conversation.allowedWeathers,
            value => conversation.anyWeather = value,
            report);
    }

    private static void ApplyConversation(
        ConversationData conversation,
        ConversationExportItem item,
        HeroineImportReport report)
    {
        ConversationExportConditions conditions = item.conditions ?? new ConversationExportConditions();

        conversation.conversationId = item.id;
        conversation.genre = ParseEnumOrDefault(item.category, ConversationGenre.Daily);
        conversation.type = ConversationType.Simple;
        conversation.heroineLine = GetFirstLineText(item);
        conversation.expressionId = GetFirstLineExpression(item);
        ApplyConversationLines(conversation.lines, item);
        ApplyConversationChoices(conversation, item, report);
        conversation.priority = item.priority;
        conversation.showOnce = conditions.once;
        conversation.minAffection = Math.Max(0, conditions.minAffection);
        conversation.maxAffection = conditions.maxAffection > 0 ? conditions.maxAffection : 9999;
        conversation.costumeId = conditions.costumeId ?? string.Empty;

        ApplySingleEnumCondition(
            conditions.timeOfDay,
            conversation.allowedTimeSlots,
            value => conversation.anyTimeSlot = value,
            report);
        ApplySingleEnumCondition(
            conditions.season,
            conversation.allowedSeasons,
            value => conversation.anySeason = value,
            report);
        ApplySingleEnumCondition(
            conditions.weather,
            conversation.allowedWeathers,
            value => conversation.anyWeather = value,
            report);
    }

    private static string GetFirstLineText(ConversationExportItem item)
    {
        if (item == null || item.lines == null)
        {
            return string.Empty;
        }

        foreach (ConversationExportLine line in item.lines)
        {
            if (line != null && !string.IsNullOrWhiteSpace(line.text))
            {
                return line.text;
            }
        }

        return string.Empty;
    }

    private static string GetFirstLineExpression(ConversationExportItem item)
    {
        return GetFirstLineExpression(item == null ? null : item.lines);
    }

    private static string GetFirstLineExpression(ActionReactionExportItem item)
    {
        return GetFirstLineExpression(item == null ? null : item.lines);
    }

    private static string GetFirstLineExpression(ConversationExportLine[] lines)
    {
        if (lines == null)
        {
            return string.Empty;
        }

        foreach (ConversationExportLine line in lines)
        {
            if (line != null && !string.IsNullOrWhiteSpace(line.text))
            {
                return line.expression ?? string.Empty;
            }
        }

        return string.Empty;
    }

    private static string GetFirstLineText(ActionReactionExportItem item)
    {
        return GetFirstLineText(item == null ? null : item.lines);
    }

    private static string GetFirstLineText(EndingExportItem item)
    {
        return GetFirstLineText(item == null ? null : item.lines);
    }

    private static string GetFirstLineText(ConversationExportLine[] lines)
    {
        if (lines == null)
        {
            return string.Empty;
        }

        foreach (ConversationExportLine line in lines)
        {
            if (line != null && !string.IsNullOrWhiteSpace(line.text))
            {
                return line.text;
            }
        }

        return string.Empty;
    }

    private static string JoinLineTexts(ConversationExportLine[] lines)
    {
        List<string> texts = new List<string>();
        if (lines == null)
        {
            return string.Empty;
        }

        foreach (ConversationExportLine line in lines)
        {
            if (line != null && !string.IsNullOrWhiteSpace(line.text))
            {
                texts.Add(line.text);
            }
        }

        return string.Join("\n", texts.ToArray());
    }

    private static bool UsesHeroineSpeaker(ActionReactionExportItem item)
    {
        if (item == null || item.lines == null)
        {
            return true;
        }

        foreach (ConversationExportLine line in item.lines)
        {
            if (line != null && !string.IsNullOrWhiteSpace(line.text))
            {
                return string.IsNullOrWhiteSpace(line.speaker)
                    || string.Equals(line.speaker, "Heroine", StringComparison.OrdinalIgnoreCase);
            }
        }

        return true;
    }

    private static string GetFirstImageAssetId(string[] imageAssetIds)
    {
        if (imageAssetIds == null)
        {
            return string.Empty;
        }

        foreach (string imageAssetId in imageAssetIds)
        {
            if (!string.IsNullOrWhiteSpace(imageAssetId))
            {
                return imageAssetId;
            }
        }

        return string.Empty;
    }

    private static void ApplyConversationLines(
        List<ConversationLineData> target,
        ConversationExportItem item)
    {
        target.Clear();

        if (item == null || item.lines == null)
        {
            return;
        }

        foreach (ConversationExportLine line in item.lines)
        {
            if (line == null || string.IsNullOrWhiteSpace(line.text))
            {
                continue;
            }

            ConversationLineData conversationLine = new ConversationLineData
            {
                speaker = line.speaker ?? string.Empty,
                text = line.text,
                expressionId = line.expression ?? string.Empty
            };

            target.Add(conversationLine);
        }
    }

    private static void ApplyConversationChoices(
        ConversationDataItem conversation,
        ConversationExportItem item,
        HeroineImportReport report)
    {
        conversation.choices.Clear();
        if (item == null || item.choices == null || item.choices.Length == 0)
        {
            conversation.type = ConversationType.Simple;
            return;
        }

        if (item.choices.Length > 3)
        {
            report.Warn("Unity 側 UI は選択肢 3 件までのため、4 件目以降は表示されません: " + item.id);
        }

        foreach (ConversationChoiceExport choiceExport in item.choices)
        {
            if (choiceExport == null)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(choiceExport.choiceText))
            {
                report.Warn("choiceText が空の選択肢をスキップしました: " + item.id);
                continue;
            }

            if (string.IsNullOrWhiteSpace(choiceExport.responseText))
            {
                report.Warn("responseText が空の選択肢をスキップしました: " + item.id + " / " + choiceExport.choiceText);
                continue;
            }

            conversation.choices.Add(
                new ConversationChoice
                {
                    choiceText = choiceExport.choiceText,
                    responseText = choiceExport.responseText,
                    affectionChange = choiceExport.affectionChange
                });
        }

        conversation.type = conversation.choices.Count > 0
            ? ConversationType.Choice
            : ConversationType.Simple;
    }

    private static void ApplyConversationChoices(
        ConversationData conversation,
        ConversationExportItem item,
        HeroineImportReport report)
    {
        conversation.choices.Clear();
        if (item == null || item.choices == null || item.choices.Length == 0)
        {
            conversation.type = ConversationType.Simple;
            return;
        }

        if (item.choices.Length > 3)
        {
            report.Warn("Unity 側 UI は選択肢 3 件までのため、4 件目以降は表示されません: " + item.id);
        }

        foreach (ConversationChoiceExport choiceExport in item.choices)
        {
            if (choiceExport == null)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(choiceExport.choiceText))
            {
                report.Warn("choiceText が空の選択肢をスキップしました: " + item.id);
                continue;
            }

            if (string.IsNullOrWhiteSpace(choiceExport.responseText))
            {
                report.Warn("responseText が空の選択肢をスキップしました: " + item.id + " / " + choiceExport.choiceText);
                continue;
            }

            conversation.choices.Add(
                new ConversationChoice
                {
                    choiceText = choiceExport.choiceText,
                    responseText = choiceExport.responseText,
                    affectionChange = choiceExport.affectionChange
                });
        }

        conversation.type = conversation.choices.Count > 0
            ? ConversationType.Choice
            : ConversationType.Simple;
    }

    internal static void ImportGameEvents(
        string exportFolder,
        string heroineId,
        HeroineImportReport report)
    {
        string gameEventsJsonPath = Path.Combine(exportFolder, GameEventsJsonRelativePath);
        if (!File.Exists(gameEventsJsonPath))
        {
            Debug.Log("game_events_export.json が見つからないため、ゲームイベント import はスキップしました: " + gameEventsJsonPath);
            return;
        }

        GameEventsExport gameEventsExport;
        try
        {
            string json = File.ReadAllText(gameEventsJsonPath);
            gameEventsExport = JsonUtility.FromJson<GameEventsExport>(json);
        }
        catch (Exception ex)
        {
            Debug.LogError("game_events_export.json の読み込みに失敗しました: " + ex.Message);
            return;
        }

        if (gameEventsExport == null || gameEventsExport.items == null)
        {
            report.Warn("game_events_export.json に items がありません。");
            return;
        }

        if (!string.IsNullOrWhiteSpace(gameEventsExport.heroineId)
            && !string.Equals(gameEventsExport.heroineId, heroineId, StringComparison.Ordinal))
        {
            report.Warn($"game_events_export.json の heroineId が profile と一致しません: {gameEventsExport.heroineId} / {heroineId}");
        }

        string eventFolderPath = $"Assets/Resources/Heroines/{heroineId}/GameEvents";
        EnsureFolder(eventFolderPath);

        HeroineAssetCatalog catalog = LoadOrCreateAssetCatalog(heroineId);
        Dictionary<string, Sprite> spritesByAssetId = CreateCatalogSpriteMap(catalog, report);
        HashSet<string> importedIds = new HashSet<string>(StringComparer.Ordinal);
        int importedCount = 0;

        foreach (GameEventExportItem item in gameEventsExport.items)
        {
            if (!CanImportGameEvent(item, importedIds, report))
            {
                continue;
            }

            string assetPath = $"{eventFolderPath}/{ToSafeAssetFileName(item.id)}.asset";
            GameEventData gameEvent = AssetDatabase.LoadAssetAtPath<GameEventData>(assetPath);
            if (gameEvent == null)
            {
                gameEvent = ScriptableObject.CreateInstance<GameEventData>();
                AssetDatabase.CreateAsset(gameEvent, assetPath);
            }

            ApplyGameEvent(gameEvent, item, spritesByAssetId, report);
            EditorUtility.SetDirty(gameEvent);
            importedCount++;
        }

        report.gameEventCount = importedCount;
    }

    private static bool CanImportGameEvent(
        GameEventExportItem item,
        HashSet<string> importedIds,
        HeroineImportReport report)
    {
        if (item == null || string.IsNullOrWhiteSpace(item.id))
        {
            report.Warn("id が空の game event item をスキップしました。");
            return false;
        }

        if (!importedIds.Add(item.id))
        {
            report.Warn("id が重複している game event item をスキップしました: " + item.id);
            return false;
        }

        if (item.lines == null || item.lines.Length == 0)
        {
            report.Warn("lines が空の game event item をスキップしました: " + item.id);
            return false;
        }

        return true;
    }

    private static void ApplyGameEvent(
        GameEventData gameEvent,
        GameEventExportItem item,
        Dictionary<string, Sprite> spritesByAssetId,
        HeroineImportReport report)
    {
        GameEventExportConditions conditions = item.conditions ?? new GameEventExportConditions();

        gameEvent.name = item.id;
        gameEvent.eventId = item.id;
        gameEvent.triggerType = ParseGameEventTriggerType(item.category, report);
        gameEvent.showOnce = conditions.once;
        gameEvent.isEnabled = true;
        gameEvent.sortOrder = item.priority;
        gameEvent.minDay = Math.Max(0, conditions.minDay);
        gameEvent.maxDay = Math.Max(0, conditions.maxDay);
        gameEvent.minAffection = Math.Max(0, conditions.minAffection);
        gameEvent.maxAffection = Math.Max(0, conditions.maxAffection);
        EnsureGameEventLists(gameEvent);
        RequiredSkillIdSyncService.ApplyIfSpecified(
            gameEvent.requiredSkillIds,
            conditions.requiredSkillIds);
        ApplyStringList(gameEvent.requiredShownEventIds, conditions.requiredShownEventIds);
        ApplyStringList(gameEvent.blockedShownEventIds, conditions.blockedShownEventIds);
        ApplyStringList(gameEvent.requiredOutfitIds, conditions.requiredOutfitIds);
        AddStringIfNotExists(gameEvent.requiredOutfitIds, conditions.costumeId);
        ApplyStringList(gameEvent.blockedOutfitIds, conditions.blockedOutfitIds);
        gameEvent.requiredOutfits.Clear();
        gameEvent.blockedOutfits.Clear();
        ApplySingleEnumCondition(
            conditions.weather,
            gameEvent.allowedWeathers,
            value => gameEvent.anyWeather = value,
            report);
        ApplyGameEventPages(gameEvent.pages, item, spritesByAssetId, report);
    }

    private static void EnsureGameEventLists(GameEventData gameEvent)
    {
        if (gameEvent.requiredSkillIds == null)
        {
            gameEvent.requiredSkillIds = new List<string>();
        }

        if (gameEvent.requiredShownEventIds == null)
        {
            gameEvent.requiredShownEventIds = new List<string>();
        }

        if (gameEvent.blockedShownEventIds == null)
        {
            gameEvent.blockedShownEventIds = new List<string>();
        }

        if (gameEvent.requiredOutfitIds == null)
        {
            gameEvent.requiredOutfitIds = new List<string>();
        }

        if (gameEvent.blockedOutfitIds == null)
        {
            gameEvent.blockedOutfitIds = new List<string>();
        }

        if (gameEvent.requiredOutfits == null)
        {
            gameEvent.requiredOutfits = new List<OutfitData>();
        }

        if (gameEvent.blockedOutfits == null)
        {
            gameEvent.blockedOutfits = new List<OutfitData>();
        }

        if (gameEvent.allowedWeathers == null)
        {
            gameEvent.allowedWeathers = new List<Weather>();
        }

        if (gameEvent.pages == null)
        {
            gameEvent.pages = new List<GameEventPageData>();
        }
    }

    private static GameEventTriggerType ParseGameEventTriggerType(
        string category,
        HeroineImportReport report)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            return GameEventTriggerType.Manual;
        }

        if (Enum.TryParse(category, true, out GameEventTriggerType triggerType))
        {
            return triggerType;
        }

        report.Warn("未知の game event category は Manual として import します: " + category);
        return GameEventTriggerType.Manual;
    }

    private static void ApplyGameEventPages(
        List<GameEventPageData> target,
        GameEventExportItem item,
        Dictionary<string, Sprite> spritesByAssetId,
        HeroineImportReport report)
    {
        target.Clear();

        Sprite stillSprite = ResolveFirstSprite(item.imageAssetIds, spritesByAssetId, report);
        string stillId = item.imageAssetIds != null && item.imageAssetIds.Length > 0
            ? item.imageAssetIds[0]
            : string.Empty;
        bool appliedStill = false;

        foreach (ConversationExportLine line in item.lines)
        {
            if (line == null || string.IsNullOrWhiteSpace(line.text))
            {
                continue;
            }

            GameEventPageData page = new GameEventPageData
            {
                speakerType = ParseScheduledEventSpeakerType(line.speaker),
                speakerName = "",
                message = line.text,
                expressionId = line.expression ?? string.Empty,
                stillId = appliedStill ? string.Empty : stillId,
                stillSprite = appliedStill ? null : stillSprite
            };

            target.Add(page);
            appliedStill = true;
        }
    }

    private static ScheduledEventSpeakerType ParseScheduledEventSpeakerType(string speaker)
    {
        if (string.IsNullOrWhiteSpace(speaker))
        {
            return ScheduledEventSpeakerType.Heroine;
        }

        if (IsPlayerSpeaker(speaker))
        {
            return ScheduledEventSpeakerType.Player;
        }

        if (Enum.TryParse(speaker, true, out ScheduledEventSpeakerType speakerType))
        {
            return speakerType;
        }

        return ScheduledEventSpeakerType.Heroine;
    }

    private static bool IsPlayerSpeaker(string speaker)
    {
        return string.Equals(speaker, "Player", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(speaker, "Protagonist", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(speaker, "MainCharacter", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(speaker, "Main Character", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(speaker, "PC", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(speaker, "User", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(speaker, "You", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(speaker, "主人公", StringComparison.OrdinalIgnoreCase);
    }

    private static void ImportScheduledEvents(
        string exportFolder,
        string heroineId,
        HeroineImportReport report)
    {
        string scheduledEventsJsonPath = Path.Combine(exportFolder, ScheduledEventsJsonRelativePath);
        if (!File.Exists(scheduledEventsJsonPath))
        {
            Debug.Log("scheduled_events_export.json が見つからないため、予定イベント import はスキップしました: " + scheduledEventsJsonPath);
            return;
        }

        ScheduledEventsExport scheduledEventsExport;
        try
        {
            string json = File.ReadAllText(scheduledEventsJsonPath);
            scheduledEventsExport = JsonUtility.FromJson<ScheduledEventsExport>(json);
        }
        catch (Exception ex)
        {
            Debug.LogError("scheduled_events_export.json の読み込みに失敗しました: " + ex.Message);
            return;
        }

        if (scheduledEventsExport == null || scheduledEventsExport.items == null)
        {
            report.Warn("scheduled_events_export.json に items がありません。");
            return;
        }

        if (!string.IsNullOrWhiteSpace(scheduledEventsExport.heroineId)
            && !string.Equals(scheduledEventsExport.heroineId, heroineId, StringComparison.Ordinal))
        {
            report.Warn($"scheduled_events_export.json の heroineId が profile と一致しません: {scheduledEventsExport.heroineId} / {heroineId}");
        }

        string scheduledEventFolderPath = $"Assets/Resources/Heroines/{heroineId}/ScheduledEvents";
        EnsureFolder(scheduledEventFolderPath);

        HeroineAssetCatalog catalog = LoadOrCreateAssetCatalog(heroineId);
        Dictionary<string, Sprite> spritesByAssetId = CreateCatalogSpriteMap(catalog, report);
        HashSet<ScheduleType> importedScheduleTypes = new HashSet<ScheduleType>();
        int importedCount = 0;

        foreach (ScheduledEventExportItem item in scheduledEventsExport.items)
        {
            if (!TryResolveScheduledEventType(item, report, out ScheduleType scheduleType))
            {
                continue;
            }

            if (!importedScheduleTypes.Add(scheduleType))
            {
                report.Warn("scheduleType が重複している scheduled event item をスキップしました: " + scheduleType);
                continue;
            }

            string assetPath = ResolveScheduledEventAssetPath(scheduledEventFolderPath, scheduleType, item, report);
            ScheduledEventData scheduledEvent = AssetDatabase.LoadAssetAtPath<ScheduledEventData>(assetPath);
            if (scheduledEvent == null)
            {
                scheduledEvent = ScriptableObject.CreateInstance<ScheduledEventData>();
                AssetDatabase.CreateAsset(scheduledEvent, assetPath);
            }

            ApplyScheduledEvent(scheduledEvent, item, scheduleType, spritesByAssetId, report);
            EditorUtility.SetDirty(scheduledEvent);
            importedCount++;
        }

        report.scheduledEventCount = importedCount;
    }

    private static bool TryResolveScheduledEventType(
        ScheduledEventExportItem item,
        HeroineImportReport report,
        out ScheduleType scheduleType)
    {
        scheduleType = ScheduleType.None;
        if (item == null)
        {
            report.Warn("scheduled event item が空のためスキップしました。");
            return false;
        }

        ScheduledEventExportConditions conditions = item.conditions ?? new ScheduledEventExportConditions();
        string rawScheduleType = FirstNonEmpty(conditions.scheduleType, item.scheduleType, item.category);
        if (string.IsNullOrWhiteSpace(rawScheduleType))
        {
            report.Warn("scheduleType が空の scheduled event item をスキップしました: " + item.id);
            return false;
        }

        if (!Enum.TryParse(rawScheduleType, true, out scheduleType) || scheduleType == ScheduleType.None)
        {
            report.Warn("未知の scheduleType の scheduled event item をスキップしました: " + rawScheduleType);
            return false;
        }

        return true;
    }

    private static string ResolveScheduledEventAssetPath(
        string scheduledEventFolderPath,
        ScheduleType scheduleType,
        ScheduledEventExportItem item,
        HeroineImportReport report)
    {
        ScheduledEventExportConditions conditions = item.conditions ?? new ScheduledEventExportConditions();
        string assetName = FirstNonEmpty(item.id, conditions.actionId, item.actionId, scheduleType.ToString());
        string existingPath = FindScheduledEventAssetPathByScheduleType(
            scheduledEventFolderPath,
            scheduleType,
            report);
        if (!string.IsNullOrEmpty(existingPath))
        {
            return existingPath;
        }

        return $"{scheduledEventFolderPath}/{ToSafeAssetFileName(assetName)}.asset";
    }

    private static string FindScheduledEventAssetPathByScheduleType(
        string scheduledEventFolderPath,
        ScheduleType scheduleType,
        HeroineImportReport report)
    {
        string[] guids = AssetDatabase.FindAssets("t:ScheduledEventData", new[] { scheduledEventFolderPath });
        string fallbackPath = string.Empty;
        string preferredPath = string.Empty;
        string preferredActionId = GetDefaultScheduledEventActionId(scheduleType);
        int matchCount = 0;

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            ScheduledEventData scheduledEvent = AssetDatabase.LoadAssetAtPath<ScheduledEventData>(assetPath);
            if (scheduledEvent == null || scheduledEvent.scheduleType != scheduleType)
            {
                continue;
            }

            matchCount++;
            if (string.IsNullOrEmpty(fallbackPath))
            {
                fallbackPath = assetPath;
            }

            if (!string.IsNullOrEmpty(preferredActionId)
                && scheduledEvent.actionId == preferredActionId)
            {
                preferredPath = assetPath;
            }
        }

        if (matchCount > 1)
        {
            report.Warn("ScheduledEventData の scheduleType が重複しています。import は既存 asset を優先して更新します: " + scheduleType);
        }

        return !string.IsNullOrEmpty(preferredPath) ? preferredPath : fallbackPath;
    }

    private static string GetDefaultScheduledEventActionId(ScheduleType scheduleType)
    {
        switch (scheduleType)
        {
            case ScheduleType.SoloForest:
                return "AutoWalkForest";
            case ScheduleType.SoloCave:
                return "AutoWalkCave";
            case ScheduleType.SoloLake:
                return "AutoWalkLake";
            case ScheduleType.SoloShopping:
                return "AutoWalkShopping";
            case ScheduleType.DuoForest:
                return "AutoDuoForest";
            case ScheduleType.DuoCave:
                return "AutoDuoCave";
            case ScheduleType.DuoLake:
                return "AutoDuoLake";
            case ScheduleType.DuoShopping:
                return "AutoDuoShopping";
            case ScheduleType.StayHome:
                return "AutoStayHome";
            default:
                return string.Empty;
        }
    }

    private static void ApplyScheduledEvent(
        ScheduledEventData scheduledEvent,
        ScheduledEventExportItem item,
        ScheduleType scheduleType,
        Dictionary<string, Sprite> spritesByAssetId,
        HeroineImportReport report)
    {
        ScheduledEventExportConditions conditions = item.conditions ?? new ScheduledEventExportConditions();
        string actionId = FirstNonEmpty(conditions.actionId, item.actionId, item.id, scheduleType.ToString());
        string title = string.IsNullOrWhiteSpace(item.title) ? actionId : item.title;
        string stillId = GetFirstImageAssetId(item.imageAssetIds);

        scheduledEvent.name = title;
        scheduledEvent.scheduleType = scheduleType;
        scheduledEvent.actionId = actionId;
        scheduledEvent.triggerTimeSlot = ParseEnumOrDefault(
            FirstNonEmpty(conditions.triggerTimeSlot, conditions.timeOfDay),
            TimeSlot.Noon);
        scheduledEvent.allowOutfitChangeBeforeStart = conditions.allowOutfitChangeBeforeStart;
        scheduledEvent.costumeId = conditions.costumeId ?? string.Empty;
        scheduledEvent.outfitPromptMode = ParseEnumOrDefault(
            conditions.outfitPromptMode,
            ScheduledEventOutfitPromptMode.Conditional);
        scheduledEvent.eventSpeakerType = ParseScheduledEventSpeakerType(
            FirstNonEmpty(conditions.eventSpeakerType, conditions.speakerType, GetFirstLineSpeaker(item.lines)));
        scheduledEvent.preparationMessage = ResolveScheduledPreparationMessage(item);
        scheduledEvent.eventMessage = ResolveScheduledEventMessage(item);
        scheduledEvent.stillId = stillId;
        scheduledEvent.stillSprite = ResolveFirstSprite(item.imageAssetIds, spritesByAssetId, report);
        scheduledEvent.affectionChange = conditions.affectionChange;
    }

    private static string ResolveScheduledPreparationMessage(ScheduledEventExportItem item)
    {
        if (!string.IsNullOrWhiteSpace(item.preparationMessage))
        {
            return item.preparationMessage;
        }

        if (item.lines != null && item.lines.Length >= 2)
        {
            return item.lines[0]?.text ?? string.Empty;
        }

        return string.Empty;
    }

    private static string ResolveScheduledEventMessage(ScheduledEventExportItem item)
    {
        if (!string.IsNullOrWhiteSpace(item.eventMessage))
        {
            return item.eventMessage;
        }

        if (item.lines == null || item.lines.Length == 0)
        {
            return string.Empty;
        }

        int startIndex = item.lines.Length >= 2 ? 1 : 0;
        List<string> texts = new List<string>();
        for (int i = startIndex; i < item.lines.Length; i++)
        {
            if (item.lines[i] != null && !string.IsNullOrWhiteSpace(item.lines[i].text))
            {
                texts.Add(item.lines[i].text);
            }
        }

        return string.Join("\n", texts);
    }

    private static string GetFirstLineSpeaker(ConversationExportLine[] lines)
    {
        if (lines == null)
        {
            return string.Empty;
        }

        foreach (ConversationExportLine line in lines)
        {
            if (line != null && !string.IsNullOrWhiteSpace(line.speaker))
            {
                return line.speaker;
            }
        }

        return string.Empty;
    }

    private static void ImportActionReactions(
        string exportFolder,
        string heroineId,
        HeroineImportReport report)
    {
        string actionReactionsJsonPath = Path.Combine(exportFolder, ActionReactionsJsonRelativePath);
        if (!File.Exists(actionReactionsJsonPath))
        {
            Debug.Log("action_reactions_export.json が見つからないため、行動反応 import はスキップしました: " + actionReactionsJsonPath);
            return;
        }

        ActionReactionsExport actionReactionsExport;
        try
        {
            string json = File.ReadAllText(actionReactionsJsonPath);
            actionReactionsExport = JsonUtility.FromJson<ActionReactionsExport>(json);
        }
        catch (Exception ex)
        {
            Debug.LogError("action_reactions_export.json の読み込みに失敗しました: " + ex.Message);
            return;
        }

        if (actionReactionsExport == null || actionReactionsExport.items == null)
        {
            report.Warn("action_reactions_export.json に items がありません。");
            return;
        }

        if (!string.IsNullOrWhiteSpace(actionReactionsExport.heroineId)
            && !string.Equals(actionReactionsExport.heroineId, heroineId, StringComparison.Ordinal))
        {
            report.Warn($"action_reactions_export.json の heroineId が profile と一致しません: {actionReactionsExport.heroineId} / {heroineId}");
        }

        string actionFolderPath = $"Assets/Resources/Heroines/{heroineId}/Actions";
        EnsureFolder(actionFolderPath);

        HeroineAssetCatalog catalog = LoadOrCreateAssetCatalog(heroineId);
        Dictionary<string, Sprite> spritesByAssetId = CreateCatalogSpriteMap(catalog, report);
        Dictionary<string, ActionData> actionsById = LoadActionsById(actionFolderPath, report);
        HashSet<string> resetActionIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> importedReactionIds = new HashSet<string>(StringComparer.Ordinal);
        int importedCount = 0;

        foreach (ActionReactionExportItem item in actionReactionsExport.items)
        {
            if (!CanImportActionReaction(item, importedReactionIds, report))
            {
                continue;
            }

            string actionId = item.conditions.actionId;
            ActionData action = LoadOrCreateAction(actionId, item, actionFolderPath, actionsById);
            if (resetActionIds.Add(actionId))
            {
                action.reactions.Clear();
            }

            action.reactions.Add(CreateActionReaction(item, spritesByAssetId, report));
            EditorUtility.SetDirty(action);
            importedCount++;
        }

        report.actionReactionCount = importedCount;
    }

    private static bool CanImportActionReaction(
        ActionReactionExportItem item,
        HashSet<string> importedReactionIds,
        HeroineImportReport report)
    {
        if (item == null || string.IsNullOrWhiteSpace(item.id))
        {
            report.Warn("id が空の action reaction item をスキップしました。");
            return false;
        }

        if (!importedReactionIds.Add(item.id))
        {
            report.Warn("id が重複している action reaction item をスキップしました: " + item.id);
            return false;
        }

        if (item.conditions == null || string.IsNullOrWhiteSpace(item.conditions.actionId))
        {
            report.Warn("conditions.actionId が空の action reaction item をスキップしました: " + item.id);
            return false;
        }

        if (string.IsNullOrWhiteSpace(GetFirstLineText(item)))
        {
            report.Warn("lines が空の action reaction item をスキップしました: " + item.id);
            return false;
        }

        return true;
    }

    private static Dictionary<string, ActionData> LoadActionsById(
        string actionFolderPath,
        HeroineImportReport report)
    {
        Dictionary<string, ActionData> actionsById = new Dictionary<string, ActionData>(StringComparer.Ordinal);
        string[] guids = AssetDatabase.FindAssets("t:ActionData", new[] { actionFolderPath });
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            ActionData action = AssetDatabase.LoadAssetAtPath<ActionData>(assetPath);
            if (action == null || string.IsNullOrWhiteSpace(action.actionId))
            {
                continue;
            }

            if (actionsById.ContainsKey(action.actionId))
            {
                report.Warn("ActionData の actionId が重複しています。先に見つかった asset を使います: " + action.actionId);
                continue;
            }

            actionsById.Add(action.actionId, action);
        }

        return actionsById;
    }

    private static ActionData LoadOrCreateAction(
        string actionId,
        ActionReactionExportItem item,
        string actionFolderPath,
        Dictionary<string, ActionData> actionsById)
    {
        if (actionsById.TryGetValue(actionId, out ActionData action) && action != null)
        {
            return action;
        }

        string assetPath = $"{actionFolderPath}/{ToSafeAssetFileName(actionId)}.asset";
        action = AssetDatabase.LoadAssetAtPath<ActionData>(assetPath);
        if (action == null)
        {
            action = ScriptableObject.CreateInstance<ActionData>();
            AssetDatabase.CreateAsset(action, assetPath);
        }

        action.name = actionId;
        action.actionId = actionId;
        action.displayName = string.IsNullOrWhiteSpace(item.category) ? actionId : item.category;
        action.executionType = ActionExecutionType.SimpleAction;
        action.isEnabled = true;
        action.advanceTime = true;
        actionsById[actionId] = action;
        return action;
    }

    private static ActionReactionData CreateActionReaction(
        ActionReactionExportItem item,
        Dictionary<string, Sprite> spritesByAssetId,
        HeroineImportReport report)
    {
        ActionReactionExportConditions conditions = item.conditions ?? new ActionReactionExportConditions();
        string stillId = GetFirstImageAssetId(item.imageAssetIds);

        ActionReactionData reaction = new ActionReactionData
        {
            reactionId = item.id,
            resultMessage = GetFirstLineText(item),
            useHeroineNameAsSpeaker = UsesHeroineSpeaker(item),
            expressionId = GetFirstLineExpression(item),
            stillId = stillId,
            stillSprite = ResolveFirstSprite(item.imageAssetIds, spritesByAssetId, report),
            affectionChange = conditions.affectionChange,
            advanceTime = conditions.advanceTime,
            priority = item.priority,
            showOnce = conditions.once,
            minAffection = Math.Max(0, conditions.minAffection),
            maxAffection = conditions.maxAffection > 0 ? conditions.maxAffection : 9999,
            costumeId = conditions.costumeId ?? string.Empty,
            requiredShownEventIds = CreateStringList(conditions.requiredFlagIds),
            requiredSkillIds = CreateStringList(conditions.requiredSkillIds)
        };

        ApplySingleEnumCondition(
            conditions.timeOfDay,
            reaction.allowedTimeSlots,
            value => reaction.anyTimeSlot = value,
            report);
        ApplySingleEnumCondition(
            conditions.weather,
            reaction.allowedWeathers,
            value => reaction.anyWeather = value,
            report);
        ApplySingleEnumCondition(
            conditions.season,
            reaction.allowedSeasons,
            value => reaction.anySeason = value,
            report);

        return reaction;
    }

    private static void ImportEndings(
        string exportFolder,
        string heroineId,
        HeroineImportReport report)
    {
        string endingsJsonPath = Path.Combine(exportFolder, EndingsJsonRelativePath);
        if (!File.Exists(endingsJsonPath))
        {
            Debug.Log("endings_export.json が見つからないため、エンディング import はスキップしました: " + endingsJsonPath);
            return;
        }

        EndingsExport endingsExport;
        try
        {
            string json = File.ReadAllText(endingsJsonPath);
            endingsExport = JsonUtility.FromJson<EndingsExport>(json);
        }
        catch (Exception ex)
        {
            Debug.LogError("endings_export.json の読み込みに失敗しました: " + ex.Message);
            return;
        }

        if (endingsExport == null || endingsExport.items == null)
        {
            report.Warn("endings_export.json に items がありません。");
            return;
        }

        if (!string.IsNullOrWhiteSpace(endingsExport.heroineId)
            && !string.Equals(endingsExport.heroineId, heroineId, StringComparison.Ordinal))
        {
            report.Warn($"endings_export.json の heroineId が profile と一致しません: {endingsExport.heroineId} / {heroineId}");
        }

        string endingFolderPath = $"Assets/Resources/Heroines/{heroineId}/Endings";
        EnsureFolder(endingFolderPath);

        HeroineAssetCatalog catalog = LoadOrCreateAssetCatalog(heroineId);
        Dictionary<string, Sprite> spritesByAssetId = CreateCatalogSpriteMap(catalog, report);
        HashSet<string> importedIds = new HashSet<string>(StringComparer.Ordinal);
        int importedCount = 0;

        foreach (EndingExportItem item in endingsExport.items)
        {
            if (!CanImportEnding(item, importedIds, report))
            {
                continue;
            }

            string assetPath = $"{endingFolderPath}/{ToSafeAssetFileName(item.id)}.asset";
            EndingData ending = AssetDatabase.LoadAssetAtPath<EndingData>(assetPath);
            if (ending == null)
            {
                ending = ScriptableObject.CreateInstance<EndingData>();
                AssetDatabase.CreateAsset(ending, assetPath);
            }

            ApplyEnding(ending, item, spritesByAssetId, report);
            EditorUtility.SetDirty(ending);
            importedCount++;
        }

        report.endingCount = importedCount;
    }

    private static bool CanImportEnding(
        EndingExportItem item,
        HashSet<string> importedIds,
        HeroineImportReport report)
    {
        if (item == null || string.IsNullOrWhiteSpace(item.id))
        {
            report.Warn("id が空の ending item をスキップしました。");
            return false;
        }

        if (!importedIds.Add(item.id))
        {
            report.Warn("id が重複している ending item をスキップしました: " + item.id);
            return false;
        }

        if (string.IsNullOrWhiteSpace(GetFirstLineText(item)))
        {
            report.Warn("lines が空の ending item をスキップしました: " + item.id);
            return false;
        }

        return true;
    }

    private static void ApplyEnding(
        EndingData ending,
        EndingExportItem item,
        Dictionary<string, Sprite> spritesByAssetId,
        HeroineImportReport report)
    {
        EndingExportConditions conditions = item.conditions ?? new EndingExportConditions();

        ending.name = item.id;
        ending.endingId = item.id;
        ending.displayName = string.IsNullOrWhiteSpace(item.title) ? item.id : item.title;
        ending.message = JoinLineTexts(item.lines);
        ending.stillSprite = ResolveFirstSprite(item.imageAssetIds, spritesByAssetId, report);
        ending.requiredAffection = Math.Max(0, conditions.minAffection);
        ending.costumeId = conditions.costumeId ?? string.Empty;
        ending.requiredShownEventIds = conditions.requiredShownEventIds ?? conditions.requiredFlagIds ?? new string[0];
    }

    private static Sprite ResolveFirstSprite(
        string[] imageAssetIds,
        Dictionary<string, Sprite> spritesByAssetId,
        HeroineImportReport report)
    {
        if (imageAssetIds == null || imageAssetIds.Length == 0)
        {
            return null;
        }

        string assetId = imageAssetIds[0];
        if (string.IsNullOrWhiteSpace(assetId))
        {
            return null;
        }

        if (spritesByAssetId != null
            && spritesByAssetId.TryGetValue(assetId, out Sprite sprite)
            && sprite != null)
        {
            return sprite;
        }

        report.Warn("imageAssetIds を Sprite に解決できませんでした: " + assetId);
        return null;
    }

    private static void ApplyStringList(List<string> target, string[] source)
    {
        target.Clear();
        if (source == null)
        {
            return;
        }

        foreach (string value in source)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                target.Add(value);
            }
        }
    }

    private static List<string> CreateStringList(string[] source)
    {
        List<string> values = new List<string>();
        ApplyStringList(values, source);
        return values;
    }

    private static void AddStringIfNotExists(List<string> target, string value)
    {
        if (target == null || string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (!target.Contains(value))
        {
            target.Add(value);
        }
    }

    private static string ToSafeAssetFileName(string rawName)
    {
        string fileName = string.IsNullOrWhiteSpace(rawName) ? "GameEvent" : rawName.Trim();
        foreach (char invalidChar in Path.GetInvalidFileNameChars())
        {
            fileName = fileName.Replace(invalidChar, '_');
        }

        return fileName;
    }

    private static string FirstNonEmpty(params string[] values)
    {
        if (values == null)
        {
            return string.Empty;
        }

        foreach (string value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return string.Empty;
    }

    private static void ApplySingleEnumCondition<T>(
        string rawValue,
        List<T> target,
        Action<bool> setAny,
        HeroineImportReport report)
        where T : struct
    {
        target.Clear();
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            setAny(true);
            return;
        }

        if (Enum.TryParse(rawValue, true, out T parsedValue))
        {
            target.Add(parsedValue);
            setAny(false);
            return;
        }

        report.Warn($"未知の条件値を無視しました: {typeof(T).Name} = {rawValue}");
        setAny(true);
    }

    private static T ParseEnumOrDefault<T>(string rawValue, T defaultValue)
        where T : struct
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return defaultValue;
        }

        return Enum.TryParse(rawValue, true, out T parsedValue) ? parsedValue : defaultValue;
    }

    private static void SetDefaultSpriteCandidateIfNeeded(
        HeroineImportReport report,
        HeroineAssetExport asset,
        string unityPath)
    {
        if (!string.IsNullOrWhiteSpace(report.defaultSpritePath) || !IsDefaultSpriteCandidate(asset))
        {
            return;
        }

        report.defaultSpritePath = unityPath;
    }

    private static bool IsDefaultSpriteCandidate(HeroineAssetExport asset)
    {
        if (!string.Equals(asset.usage, "Sprites", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return string.Equals(asset.assetId, DefaultHeroineSpriteAssetId, StringComparison.OrdinalIgnoreCase)
            || string.Equals(asset.fileName, DefaultHeroineSpriteFileName, StringComparison.OrdinalIgnoreCase);
    }

    private static void EnsureSpriteImportSettings(string unityPath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(unityPath) as TextureImporter;
        if (importer == null || importer.textureType == TextureImporterType.Sprite)
        {
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.SaveAndReimport();
    }

    private static bool ShouldImportAsset(HeroineAssetExport asset)
    {
        return string.IsNullOrWhiteSpace(asset.status)
            || string.Equals(asset.status, "Accepted", StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveExportPath(string exportFolder, string exportImagePath, string usage, string fileName)
    {
        if (!string.IsNullOrWhiteSpace(exportImagePath))
        {
            return Path.IsPathRooted(exportImagePath)
                ? exportImagePath
                : Path.Combine(exportFolder, exportImagePath);
        }

        if (string.IsNullOrWhiteSpace(usage) || string.IsNullOrWhiteSpace(fileName))
        {
            return string.Empty;
        }

        return Path.Combine(exportFolder, "Images", usage, fileName);
    }

    private static string ResolveUnityImagePath(string heroineId, HeroineAssetExport asset)
    {
        if (!string.IsNullOrWhiteSpace(asset.unityImagePath))
        {
            return NormalizeAssetPath(asset.unityImagePath);
        }

        if (string.IsNullOrWhiteSpace(asset.usage) || string.IsNullOrWhiteSpace(asset.fileName))
        {
            return string.Empty;
        }

        return NormalizeAssetPath($"Assets/Images/Heroines/{heroineId}/{asset.usage}/{asset.fileName}");
    }

    private static bool IsValidUnityAssetPath(string assetPath)
    {
        string normalizedPath = NormalizeAssetPath(assetPath);
        return normalizedPath.StartsWith("Assets/", StringComparison.Ordinal)
            && !normalizedPath.Contains("/../")
            && !normalizedPath.EndsWith("/..", StringComparison.Ordinal);
    }

    private static void EnsureFolderForAssetPath(string assetPath)
    {
        string folderPath = Path.GetDirectoryName(assetPath)?.Replace("\\", "/");
        if (!string.IsNullOrEmpty(folderPath))
        {
            EnsureFolder(folderPath);
        }
    }

    private static string NormalizeAssetPath(string path)
    {
        return string.IsNullOrWhiteSpace(path) ? string.Empty : path.Replace("\\", "/");
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        string parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
        string folderName = Path.GetFileName(path);
        if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(folderName))
        {
            return;
        }

        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, folderName);
    }

    [Serializable]
    private sealed class HeroineProfileExport
    {
        public string schemaVersion;
        public string heroineId;
        public string displayName;
        public string age;
        public string height;
        public string personality;
        public string speakingStyle;
        public string firstPerson;
        public string secondPerson;
        public string initialDialogueMessage;
        public string nextActionPrompt;
        public string morningGreeting;
        public string goodNightGreeting;
        public string gameStartFallbackMessage;
        public string gameStartFollowUpMessage;
        public OutfitMessageOverrideExport[] outfitMessageOverrides;
        public OutfitReactionMessageOverrideExport[] outfitReactionMessageOverrides;
        public string appearancePrompt;
        public string stillCommonPositivePrompt;
        public string actionReactionPolicy;
        public string endingPolicy;
        public string[] likes;
        public string[] dislikes;
        public HeroineBattleSkillExport[] battleSkills;
        public string conversationResourcePath;
        public string gameEventResourcePath;
        public string actionResourcePath;
        public string scheduledEventResourcePath;
        public string battleResultEventResourcePath;
        public string battlePanelResultMessageResourcePath;
        public string endingResourcePath;
    }

    [Serializable]
    private sealed class HeroineBattleSkillExport
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
    private sealed class OutfitMessageOverrideExport
    {
        public string outfitId;
        public string lockedMessage;
        public string changedMessage;
        public string changedExpressionId;
    }

    [Serializable]
    private sealed class OutfitReactionMessageOverrideExport
    {
        public string reactionType;
        public string message;
        public string expressionId;
    }

    [Serializable]
    private sealed class AssetsExport
    {
        public string schemaVersion;
        public string heroineId;
        public string unityImageRoot;
        public HeroineAssetExport[] assets;
    }

    [Serializable]
    private sealed class TrainingImagesExport
    {
        public int schemaVersion;
        public string heroineId;
        public TrainingImageExportDefaults defaults;
        public TrainingImageExportItem[] items;
    }

    [Serializable]
    private sealed class TrainingImageExportDefaults
    {
        public string beforeFirstStepImageAssetId;
        public string afterFirstStepImageAssetId;
        public string playerLpConsumedImageAssetId;
        public string heroineLpConsumedImageAssetId;
        public string simultaneousLpConsumedImageAssetId;
    }

    [Serializable]
    private sealed class TrainingImageExportItem
    {
        public string trainingId;
        public string beforeFirstStepImageAssetId;
        public string afterFirstStepImageAssetId;
        public string playerLpConsumedImageAssetId;
        public string heroineLpConsumedImageAssetId;
        public string simultaneousLpConsumedImageAssetId;
        public string memo;
    }

    [Serializable]
    private sealed class TrainingDialoguesExport
    {
        public int schemaVersion;
        public string heroineId;
        public TrainingDialogueExportItem[] items;
    }

    [Serializable]
    private sealed class TrainingDialogueExportItem
    {
        public string trainingId;
        public string visualState;
        public string[] messages;
    }

    [Serializable]
    private sealed class ConversationsExport
    {
        public string schemaVersion;
        public string heroineId;
        public ConversationExportItem[] items;
    }

    [Serializable]
    private sealed class GameEventsExport
    {
        public string schemaVersion;
        public string heroineId;
        public GameEventExportItem[] items;
    }

    [Serializable]
    private sealed class ScheduledEventsExport
    {
        public string schemaVersion;
        public string heroineId;
        public ScheduledEventExportItem[] items;
    }

    [Serializable]
    private sealed class ActionReactionsExport
    {
        public string schemaVersion;
        public string heroineId;
        public ActionReactionExportItem[] items;
    }

    [Serializable]
    private sealed class EndingsExport
    {
        public string schemaVersion;
        public string heroineId;
        public EndingExportItem[] items;
    }

    [Serializable]
    private sealed class SpriteLayersExport
    {
        public string schemaVersion;
        public string heroineId;
        public string unityImageRoot;
        public SpriteLayerExport[] layers;
    }

    [Serializable]
    private sealed class SpriteLayerExport
    {
        public string assetId;
        public string layerKind;
        public string costumeId;
        public string expressionId;
        public string displayName;
        public int drawOrder;
        public string fileName;
        public string exportImagePath;
        public string unityImagePath;
    }

    [Serializable]
    private sealed class ConversationExportItem
    {
        public string id;
        public string title;
        public string category;
        public ConversationExportConditions conditions;
        public ConversationExportLine[] lines;
        public ConversationChoiceExport[] choices;
        public string[] imageAssetIds;
        public int priority;
        public string memo;
    }

    [Serializable]
    private sealed class ConversationExportConditions
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
    private sealed class GameEventExportItem
    {
        public string id;
        public string title;
        public string category;
        public GameEventExportConditions conditions;
        public ConversationExportLine[] lines;
        public string[] imageAssetIds;
        public int priority;
        public string memo;
    }

    [Serializable]
    private sealed class GameEventExportConditions
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
        public string[] requiredShownEventIds;
        public string[] blockedShownEventIds;
        public string[] requiredOutfitIds;
        public string[] blockedOutfitIds;
        public string[] requiredSkillIds;
    }

    [Serializable]
    private sealed class ScheduledEventExportItem
    {
        public string id;
        public string title;
        public string category;
        public string scheduleType;
        public string actionId;
        public ScheduledEventExportConditions conditions;
        public string preparationMessage;
        public string eventMessage;
        public ConversationExportLine[] lines;
        public string[] imageAssetIds;
        public int priority;
        public string memo;
    }

    [Serializable]
    private sealed class ScheduledEventExportConditions
    {
        public string scheduleType;
        public string actionId;
        public string triggerTimeSlot;
        public string timeOfDay;
        public string costumeId;
        public bool allowOutfitChangeBeforeStart = true;
        public string outfitPromptMode;
        public string eventSpeakerType;
        public string speakerType;
        public int affectionChange;
    }

    [Serializable]
    private sealed class ActionReactionExportItem
    {
        public string id;
        public string title;
        public string category;
        public ActionReactionExportConditions conditions;
        public ConversationExportLine[] lines;
        public string[] imageAssetIds;
        public int priority;
        public string memo;
    }

    [Serializable]
    private sealed class ActionReactionExportConditions
    {
        public string actionId;
        public int minAffection;
        public int maxAffection;
        public string weather;
        public string season;
        public string timeOfDay;
        public string costumeId;
        public int affectionChange;
        public bool advanceTime = true;
        public bool once;
        public string[] requiredFlagIds;
        public string[] requiredSkillIds;
    }

    [Serializable]
    private sealed class EndingExportItem
    {
        public string id;
        public string title;
        public string category;
        public EndingExportConditions conditions;
        public ConversationExportLine[] lines;
        public string[] imageAssetIds;
        public int priority;
        public string memo;
    }

    [Serializable]
    private sealed class EndingExportConditions
    {
        public int minAffection;
        public string costumeId;
        public string[] requiredFlagIds;
        public string[] requiredShownEventIds;
    }

    [Serializable]
    private sealed class ConversationExportLine
    {
        public string speaker;
        public string text;
        public string expression;
    }

    [Serializable]
    private sealed class ConversationChoiceExport
    {
        public string choiceText;
        public string responseText;
        public int affectionChange;
    }

    [Serializable]
    private sealed class HeroineAssetExport
    {
        public string assetId;
        public string usage;
        public string status;
        public string fileName;
        public string memo;
        public string exportImagePath;
        public string exportPromptPath;
        public string unityImagePath;
    }

    internal sealed class HeroineImportReport
    {
        public int copiedImageCount;
        public int catalogAssetCount;
        public int layerCount;
        public int conversationCount;
        public int gameEventCount;
        public int scheduledEventCount;
        public int actionReactionCount;
        public int endingCount;
        public int trainingImageCount;
        public int trainingImageEntryCount;
        public int trainingImageUnresolvedCount;
        public int trainingImageSkippedCount;
        public int trainingDialogueEntryCount;
        public int battleMessageAddedCount;
        public int battleMessageUpdatedCount;
        public int battleMessageDeletedCount;
        public int battleMessageSkippedCount;
        public bool trainingImageSettingsUpdated;
        public string defaultSpritePath;
        public readonly List<string> warnings = new List<string>();

        public void Warn(string message)
        {
            warnings.Add(message);
            Debug.LogWarning(message);
        }

        public void LogSummary(string assetPath)
        {
            Debug.Log(
                $"Heroine export を import しました: {assetPath}, copied images: {copiedImageCount}, catalog assets: {catalogAssetCount}, training images: {trainingImageCount}, training entries: {trainingImageEntryCount}, training dialogues: {trainingDialogueEntryCount}, battle messages added/updated/deleted/skipped: {battleMessageAddedCount}/{battleMessageUpdatedCount}/{battleMessageDeletedCount}/{battleMessageSkippedCount}, training unresolved: {trainingImageUnresolvedCount}, training skipped: {trainingImageSkippedCount}, layers: {layerCount}, conversations: {conversationCount}, game events: {gameEventCount}, scheduled events: {scheduledEventCount}, action reactions: {actionReactionCount}, endings: {endingCount}, warnings: {warnings.Count}");
        }

        public string CreateDialogMessage(string assetPath)
        {
            string message =
                "Import completed.\n" +
                "Profile: " + assetPath + "\n" +
                "Copied images: " + copiedImageCount + "\n" +
                "Catalog assets: " + catalogAssetCount + "\n" +
                "Training images: " + trainingImageCount + "\n" +
                "Training settings updated: " + (trainingImageSettingsUpdated ? "Yes" : "No") + "\n" +
                "Training entries: " + trainingImageEntryCount + "\n" +
                "Training dialogues: " + trainingDialogueEntryCount + "\n" +
                "Battle messages added/updated/deleted/skipped: " + battleMessageAddedCount + "/" + battleMessageUpdatedCount + "/" + battleMessageDeletedCount + "/" + battleMessageSkippedCount + "\n" +
                "Training unresolved: " + trainingImageUnresolvedCount + "\n" +
                "Training skipped: " + trainingImageSkippedCount + "\n" +
                "Layers: " + layerCount + "\n" +
                "Conversations: " + conversationCount + "\n" +
                "Game events: " + gameEventCount + "\n" +
                "Scheduled events: " + scheduledEventCount + "\n" +
                "Action reactions: " + actionReactionCount + "\n" +
                "Endings: " + endingCount + "\n" +
                "Warnings: " + warnings.Count;

            if (warnings.Count == 0)
            {
                return message;
            }

            int previewCount = Math.Min(5, warnings.Count);
            message += "\n\nWarnings:";
            for (int i = 0; i < previewCount; i++)
            {
                message += "\n- " + warnings[i];
            }

            if (warnings.Count > previewCount)
            {
                message += "\n- ...";
            }

            return message;
        }
    }
}
