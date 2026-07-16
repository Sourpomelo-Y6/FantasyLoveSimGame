using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public sealed class DevelopmentHeroineOverrideWindow : EditorWindow
{
    private const string MenuPath = "Tools/FantasyLoveSim/Development Heroine Override";
    private const string EnabledKey = "FantasyLoveSim.DevelopmentHeroineOverride.Enabled";
    private const string HeroineIdKey = "FantasyLoveSim.DevelopmentHeroineOverride.HeroineId";
    private HeroineProfileData selectedProfile;
    private bool enabledOverride;

    static DevelopmentHeroineOverrideWindow()
    {
        ApplyEditorPrefs();
    }

    [MenuItem(MenuPath)]
    private static void Open()
    {
        GetWindow<DevelopmentHeroineOverrideWindow>("Development Heroine");
    }

    private void OnEnable()
    {
        enabledOverride = EditorPrefs.GetBool(EnabledKey, false);
        selectedProfile = FindProfile(EditorPrefs.GetString(HeroineIdKey, string.Empty));
    }

    private void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "Editor上の新規ゲームだけで使用するローカル設定です。SceneやProfile Assetは変更せず、Gitにも保存されません。ロード時はセーブデータを優先します。",
            MessageType.Info);

        EditorGUI.BeginChangeCheck();
        enabledOverride = EditorGUILayout.Toggle("Enable Override", enabledOverride);
        selectedProfile = (HeroineProfileData)EditorGUILayout.ObjectField(
            "Development Heroine",
            selectedProfile,
            typeof(HeroineProfileData),
            false);
        if (EditorGUI.EndChangeCheck())
        {
            SaveAndApply();
        }

        using (new EditorGUI.DisabledScope(selectedProfile != null &&
            string.Equals(selectedProfile.heroineId, "TestHeroine", StringComparison.Ordinal)))
        {
            if (GUILayout.Button("Use TestHeroine"))
            {
                selectedProfile = FindProfile("TestHeroine");
                enabledOverride = selectedProfile != null;
                SaveAndApply();
            }
        }

        if (GUILayout.Button("Disable And Clear"))
        {
            enabledOverride = false;
            selectedProfile = null;
            SaveAndApply();
        }

        string activeId = DevelopmentHeroineOverride.HeroineId;
        EditorGUILayout.Space();
        EditorGUILayout.LabelField(
            "Active Override",
            string.IsNullOrEmpty(activeId) ? "None (production settings)" : activeId);
        if (enabledOverride && selectedProfile == null)
        {
            EditorGUILayout.HelpBox(
                "指定したHeroineProfileDataが見つからないため、本番設定へフォールバックします。",
                MessageType.Warning);
        }
    }

    private void SaveAndApply()
    {
        EditorPrefs.SetBool(EnabledKey, enabledOverride);
        EditorPrefs.SetString(
            HeroineIdKey,
            selectedProfile != null ? selectedProfile.heroineId ?? string.Empty : string.Empty);
        ApplyEditorPrefs();
    }

    private static void ApplyEditorPrefs()
    {
        bool enabled = EditorPrefs.GetBool(EnabledKey, false);
        string heroineId = enabled
            ? EditorPrefs.GetString(HeroineIdKey, string.Empty)
            : string.Empty;
        DevelopmentHeroineOverride.SetHeroineId(heroineId);
    }

    private static HeroineProfileData FindProfile(string heroineId)
    {
        if (string.IsNullOrWhiteSpace(heroineId))
        {
            return null;
        }
        return Resources.LoadAll<HeroineProfileData>("Heroines")
            .FirstOrDefault(profile => profile != null &&
                string.Equals(profile.heroineId, heroineId, StringComparison.Ordinal));
    }
}
