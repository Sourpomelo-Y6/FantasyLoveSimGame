using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class HeroineAssetImporter
{
    private const string MenuPath = "FantasyLoveSim/Import Heroine Export";
    private const string ProfileJsonRelativePath = "Data/heroine_profile_export.json";

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

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("HeroineProfileData を import しました: " + assetPath);
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
}
