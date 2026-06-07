using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class HeroineAssetImporter
{
    private const string MenuPath = "FantasyLoveSim/Import Heroine Export";
    private const string ProfileJsonRelativePath = "Data/heroine_profile_export.json";
    private const string AssetsJsonRelativePath = "Data/assets_export.json";
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
        int copiedImageCount = ImportImages(exportFolder, profileExport.heroineId);

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Heroine export を import しました: {assetPath}, copied images: {copiedImageCount}");
    }

    private static void ApplyProfile(HeroineProfileData profile, HeroineProfileExport profileExport)
    {
        profile.heroineId = profileExport.heroineId;
        profile.displayName = string.IsNullOrWhiteSpace(profileExport.displayName)
            ? profileExport.heroineId
            : profileExport.displayName;
        profile.conversationResourcePath = $"Heroines/{profileExport.heroineId}/Conversations";
        profile.gameEventResourcePath = $"Heroines/{profileExport.heroineId}/GameEvents";
        profile.actionResourcePath = $"Heroines/{profileExport.heroineId}/Actions";
        profile.endingResourcePath = $"Heroines/{profileExport.heroineId}/Endings";
    }

    private static int ImportImages(string exportFolder, string heroineId)
    {
        string assetsJsonPath = Path.Combine(exportFolder, AssetsJsonRelativePath);
        if (!File.Exists(assetsJsonPath))
        {
            Debug.Log("assets_export.json が見つからないため、画像 import はスキップしました: " + assetsJsonPath);
            return 0;
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
            return 0;
        }

        if (assetsExport == null || assetsExport.assets == null)
        {
            Debug.LogWarning("assets_export.json に assets がありません。");
            return 0;
        }

        string effectiveHeroineId = string.IsNullOrWhiteSpace(assetsExport.heroineId)
            ? heroineId
            : assetsExport.heroineId;
        int copiedCount = 0;

        foreach (HeroineAssetExport asset in assetsExport.assets)
        {
            if (asset == null || !ShouldImportAsset(asset))
            {
                continue;
            }

            string sourcePath = ResolveExportPath(exportFolder, asset.exportImagePath, asset.usage, asset.fileName);
            if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath))
            {
                Debug.LogWarning("画像ファイルが見つからないためスキップしました: " + sourcePath);
                continue;
            }

            string unityPath = ResolveUnityImagePath(effectiveHeroineId, asset);
            if (!IsValidUnityAssetPath(unityPath))
            {
                Debug.LogWarning("unityImagePath が Assets 配下ではないためスキップしました: " + unityPath);
                continue;
            }

            EnsureFolderForAssetPath(unityPath);
            if (File.Exists(unityPath) && !OverwriteExistingImages)
            {
                Debug.LogWarning("既存画像があるため上書きせずスキップしました: " + unityPath);
                continue;
            }

            File.Copy(sourcePath, unityPath, OverwriteExistingImages);
            AssetDatabase.ImportAsset(unityPath);
            copiedCount++;
        }

        return copiedCount;
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
}
