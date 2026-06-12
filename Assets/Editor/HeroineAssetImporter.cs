using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class HeroineAssetImporter
{
    private const string MenuPath = "FantasyLoveSim/Import Heroine Export";
    private const string ProfileJsonRelativePath = "Data/heroine_profile_export.json";
    private const string AssetsJsonRelativePath = "Data/assets_export.json";
    private const string ConversationsJsonRelativePath = "Data/conversations_export.json";
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
        ImportConversations(exportFolder, profileExport.heroineId, report);

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        report.LogSummary(assetPath);
        EditorUtility.DisplayDialog(
            "Heroine Export Import",
            report.CreateDialogMessage(assetPath),
            "OK");
    }

    private static void ApplyProfile(HeroineProfileData profile, HeroineProfileExport profileExport)
    {
        profile.heroineId = profileExport.heroineId;
        profile.displayName = string.IsNullOrWhiteSpace(profileExport.displayName)
            ? profileExport.heroineId
            : profileExport.displayName;
        profile.conversationResourcePath = $"Heroines/{profileExport.heroineId}";
        profile.gameEventResourcePath = $"Heroines/{profileExport.heroineId}/GameEvents";
        profile.actionResourcePath = $"Heroines/{profileExport.heroineId}/Actions";
        profile.endingResourcePath = $"Heroines/{profileExport.heroineId}/Endings";
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

        string heroineResourceFolderPath = $"Assets/Resources/Heroines/{heroineId}";
        EnsureFolder(heroineResourceFolderPath);

        string assetPath = $"{heroineResourceFolderPath}/Conversations.asset";
        ConversationData conversationContainer = AssetDatabase.LoadAssetAtPath<ConversationData>(assetPath);
        if (conversationContainer == null)
        {
            conversationContainer = ScriptableObject.CreateInstance<ConversationData>();
            AssetDatabase.CreateAsset(conversationContainer, assetPath);
        }

        int importedCount = 0;
        HashSet<string> importedIds = new HashSet<string>(StringComparer.Ordinal);
        conversationContainer.heroineId = heroineId;
        conversationContainer.items.Clear();
        foreach (ConversationExportItem item in conversationsExport.items)
        {
            if (!CanImportConversation(item, importedIds, report))
            {
                continue;
            }

            ConversationDataItem conversation = new ConversationDataItem();
            ApplyConversation(conversation, item, report);
            conversationContainer.items.Add(conversation);
            importedIds.Add(item.id);
            importedCount++;
        }

        report.conversationCount = importedCount;
        EditorUtility.SetDirty(conversationContainer);
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
        conversation.choices.Clear();
        conversation.priority = item.priority;
        conversation.showOnce = conditions.once;
        conversation.minAffection = Math.Max(0, conditions.minAffection);
        conversation.maxAffection = conditions.maxAffection > 0 ? conditions.maxAffection : 100;

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
        public string appearancePrompt;
        public string stillCommonPositivePrompt;
        public string actionReactionPolicy;
        public string endingPolicy;
        public string[] likes;
        public string[] dislikes;
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
    private sealed class ConversationsExport
    {
        public string schemaVersion;
        public string heroineId;
        public ConversationExportItem[] items;
    }

    [Serializable]
    private sealed class ConversationExportItem
    {
        public string id;
        public string title;
        public string category;
        public ConversationExportConditions conditions;
        public ConversationExportLine[] lines;
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
    }

    [Serializable]
    private sealed class ConversationExportLine
    {
        public string speaker;
        public string text;
        public string expression;
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

    private sealed class HeroineImportReport
    {
        public int copiedImageCount;
        public int catalogAssetCount;
        public int conversationCount;
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
                $"Heroine export を import しました: {assetPath}, copied images: {copiedImageCount}, catalog assets: {catalogAssetCount}, conversations: {conversationCount}, warnings: {warnings.Count}");
        }

        public string CreateDialogMessage(string assetPath)
        {
            string message =
                "Import completed.\n" +
                "Profile: " + assetPath + "\n" +
                "Copied images: " + copiedImageCount + "\n" +
                "Catalog assets: " + catalogAssetCount + "\n" +
                "Conversations: " + conversationCount + "\n" +
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
