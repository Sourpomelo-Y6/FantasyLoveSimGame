using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class JapaneseFontSetupWindow : EditorWindow
{
    private const string MenuPath = "Tools/TextMeshPro/Japanese Font Setup";
    private const string SettingsAssetPath = "Assets/Resources/JapaneseFontSettings.asset";
    private const string LocalFontFolder = "Assets/Fonts/Local";

    [SerializeField] private Font sourceFont;
    [SerializeField] private TMP_FontAsset selectedFontAsset;

    private JapaneseFontSettings settings;
    private Vector2 scrollPosition;

    [MenuItem(MenuPath)]
    public static void OpenWindow()
    {
        GetWindow<JapaneseFontSetupWindow>("Japanese Font Setup");
    }

    private void OnEnable()
    {
        LoadSettingsReference();
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        EditorGUILayout.LabelField("TextMeshPro 日本語フォント設定", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        DrawSourceFontSection();
        EditorGUILayout.Space();
        DrawFontAssetSection();
        EditorGUILayout.Space();
        DrawSettingsSection();
        EditorGUILayout.Space();
        DrawApplySection();
        EditorGUILayout.Space();
        DrawTmpSettingsSection();
        EditorGUILayout.Space();
        DrawGitWarning();

        EditorGUILayout.EndScrollView();
    }

    private void DrawSourceFontSection()
    {
        EditorGUILayout.LabelField("1. 元フォントファイル", EditorStyles.boldLabel);
        sourceFont = (Font)EditorGUILayout.ObjectField(
            "Source Font (.ttf / .otf)",
            sourceFont,
            typeof(Font),
            false);

        string validationMessage;
        MessageType validationType;
        if (!ValidateSourceFont(sourceFont, out validationMessage, out validationType))
        {
            EditorGUILayout.HelpBox(validationMessage, validationType);
        }
        else if (!string.IsNullOrEmpty(validationMessage))
        {
            EditorGUILayout.HelpBox(validationMessage, validationType);
        }

        EditorGUILayout.HelpBox(
            "このツールはTMPの内部APIを使った自動生成を行いません。次のいずれかでFont Assetを作成してください。\n\n" +
            "Window > TextMeshPro > Font Asset Creator\n\n" +
            "または Projectウィンドウでフォントを右クリック\n" +
            "Create > TextMeshPro > Font Asset",
            MessageType.Info);
    }

    private void DrawFontAssetSection()
    {
        EditorGUILayout.LabelField("2. TMP Font Asset", EditorStyles.boldLabel);
        selectedFontAsset = (TMP_FontAsset)EditorGUILayout.ObjectField(
            "Default Font Asset",
            selectedFontAsset,
            typeof(TMP_FontAsset),
            false);

        if (selectedFontAsset != null)
        {
            string assetPath = AssetDatabase.GetAssetPath(selectedFontAsset);
            if (!IsAssetPath(assetPath))
            {
                EditorGUILayout.HelpBox(
                    "TMP Font AssetはAssetsフォルダ内のアセットを指定してください。",
                    MessageType.Error);
            }
            else
            {
                EditorGUILayout.LabelField("Asset Path", assetPath);
            }
        }
        else
        {
            EditorGUILayout.HelpBox(
                "Font Assetが未設定の間、Runtime適用と一括適用は既存フォントを変更しません。",
                MessageType.Warning);
        }
    }

    private void DrawSettingsSection()
    {
        EditorGUILayout.LabelField("3. 設定アセット", EditorStyles.boldLabel);
        settings = (JapaneseFontSettings)EditorGUILayout.ObjectField(
            "Japanese Font Settings",
            settings,
            typeof(JapaneseFontSettings),
            false);
        EditorGUILayout.LabelField("固定保存先", SettingsAssetPath);

        EditorGUILayout.BeginHorizontal();
        if (settings == null)
        {
            if (GUILayout.Button("設定アセットを作成"))
            {
                CreateSettingsAsset();
            }
        }
        else
        {
            if (GUILayout.Button("設定アセットを選択"))
            {
                Selection.activeObject = settings;
                EditorGUIUtility.PingObject(settings);
            }
        }

        using (new EditorGUI.DisabledScope(settings == null || selectedFontAsset == null))
        {
            if (GUILayout.Button("選択フォントを設定へ保存"))
            {
                SaveSelectedFontToSettings();
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.HelpBox(
            "JapaneseFontSettings.assetは未設定状態をGit管理します。ローカルFont Assetを保存すると" +
            "この設定アセットにもローカルGUIDが記録されます。割り当て後の差分はGitへコミットしないでください。",
            MessageType.Warning);
    }

    private void DrawApplySection()
    {
        EditorGUILayout.LabelField("4. 既存TMP Textへの一括適用", EditorStyles.boldLabel);
        using (new EditorGUI.DisabledScope(!HasValidSelectedFont()))
        {
            if (GUILayout.Button("現在開いているSceneだけへ適用"))
            {
                ApplyToOpenScenes();
            }

            if (GUILayout.Button("プロジェクト内の全Sceneへ適用して保存"))
            {
                ApplyToAllScenes();
            }

            if (GUILayout.Button("プロジェクト内の全Prefabへ適用して保存"))
            {
                ApplyToAllPrefabs();
            }
        }

        EditorGUILayout.HelpBox(
            "Scene・Prefabへの一括適用はローカル確認用です。非Git管理のFont Assetを直接保存するため、" +
            "変更したScene・Prefabをコミットすると別環境でGUID参照切れになります。\n\n" +
            "安全な通常運用では、JapaneseFontSettingsから起動時にロード済みSceneへ適用する" +
            "JapaneseFontApplierを使用します。設定がない場合は何も変更しません。",
            MessageType.Warning);
    }

    private void DrawTmpSettingsSection()
    {
        EditorGUILayout.LabelField("5. 新規TMP Textへの適用", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "TextMeshPro 3.0.6のDefault Font Assetには公開setterがないため、このツールからの自動変更は行いません。\n\n" +
            "Edit > Project Settings > TextMesh Pro > Default Font Asset\n\n" +
            "で設定してください。新しく作成するTextMeshProにはDefault Font Assetが使われます。" +
            "既存コンポーネントには上の一括適用またはJapaneseFontApplierが必要です。",
            MessageType.Info);

        if (GUILayout.Button("TMP Settings.assetを選択"))
        {
            TMP_Settings tmpSettings = TMP_Settings.instance;
            if (tmpSettings != null)
            {
                Selection.activeObject = tmpSettings;
                EditorGUIUtility.PingObject(tmpSettings);
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "Japanese Font Setup",
                    "TMP Settings.assetを読み込めませんでした。TextMeshPro Essential Resourcesを確認してください。",
                    "OK");
            }
        }
    }

    private static void DrawGitWarning()
    {
        EditorGUILayout.LabelField("Git運用", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "推奨ローカル保存先: " + LocalFontFolder + "\n" +
            "*.ttf、*.otf、生成したTMP Font Asset、フォントアトラスはコミットしません。\n" +
            "Editorツール、Runtimeコード、未設定のJapaneseFontSettings.assetと各.metaはコミットします。",
            MessageType.Info);
    }

    private void LoadSettingsReference()
    {
        settings = AssetDatabase.LoadAssetAtPath<JapaneseFontSettings>(SettingsAssetPath);
        if (settings != null && settings.defaultFontAsset != null)
        {
            selectedFontAsset = settings.defaultFontAsset;
        }
    }

    private void CreateSettingsAsset()
    {
        JapaneseFontSettings existing =
            AssetDatabase.LoadAssetAtPath<JapaneseFontSettings>(SettingsAssetPath);
        if (existing != null)
        {
            settings = existing;
            return;
        }

        EnsureFolder("Assets/Resources");
        settings = CreateInstance<JapaneseFontSettings>();
        AssetDatabase.CreateAsset(settings, SettingsAssetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeObject = settings;
        EditorGUIUtility.PingObject(settings);
    }

    private void SaveSelectedFontToSettings()
    {
        if (settings == null || !HasValidSelectedFont())
        {
            return;
        }

        bool confirmed = EditorUtility.DisplayDialog(
            "Save Japanese Font Setting",
            "ローカルFont AssetへのGUID参照をJapaneseFontSettings.assetへ保存します。\n\n" +
            "割り当て後のJapaneseFontSettings.assetはGitへコミットしないでください。続行しますか？",
            "保存",
            "キャンセル");
        if (!confirmed)
        {
            return;
        }

        Undo.RecordObject(settings, "Set Japanese TMP Font Asset");
        settings.defaultFontAsset = selectedFontAsset;
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        ShowNotification(new GUIContent("JapaneseFontSettingsへ保存しました"));
    }

    private void ApplyToOpenScenes()
    {
        if (!ConfirmDirectApplication(
            "現在開いているSceneへ適用",
            "Prefabインスタンス上のTMP Textにも変更を記録するため、Prefab Overrideが多数作成される可能性があります。\n\n" +
            "SceneはDirty状態にしますが自動保存しません。確認後に保存またはUndoしてください。"))
        {
            return;
        }

        JapaneseFontApplicationReport report = new JapaneseFontApplicationReport();
        try
        {
            int sceneCount = SceneManager.sceneCount;
            for (int i = 0; i < sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                EditorUtility.DisplayProgressBar(
                    "Japanese Font Setup",
                    "開いているSceneを処理中: " + scene.name,
                    sceneCount > 0 ? (float)i / sceneCount : 1f);
                report.examinedSceneCount++;
                try
                {
                    int changed = ApplyFontToScene(scene, selectedFontAsset, true, true);
                    report.changedTextCount += changed;
                    if (changed > 0)
                    {
                        report.changedSceneCount++;
                        EditorSceneManager.MarkSceneDirty(scene);
                    }
                }
                catch (Exception exception)
                {
                    report.errorOrSkippedAssetCount++;
                    Debug.LogError("Sceneのフォント適用に失敗しました: " + scene.path + "\n" + exception);
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        ShowReport(report, "開いているSceneへの適用が完了しました。Sceneは未保存です。");
    }

    private void ApplyToAllScenes()
    {
        if (!ConfirmDirectApplication(
            "全Sceneへ適用",
            "プロジェクト内の全Sceneを順番に開いて保存します。PrefabインスタンスにはOverrideが作成される可能性があります。\n\n" +
            "現在開いているScene構成は処理後に復元します。"))
        {
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" });
        SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();
        JapaneseFontApplicationReport report = new JapaneseFontApplicationReport();
        try
        {
            for (int i = 0; i < sceneGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(sceneGuids[i]);
                EditorUtility.DisplayProgressBar(
                    "Japanese Font Setup",
                    "Sceneを処理中: " + path,
                    sceneGuids.Length > 0 ? (float)i / sceneGuids.Length : 1f);
                report.examinedSceneCount++;
                if (!IsAssetPath(path))
                {
                    report.errorOrSkippedAssetCount++;
                    continue;
                }

                try
                {
                    Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    int changed = ApplyFontToScene(scene, selectedFontAsset, true, true);
                    report.changedTextCount += changed;
                    if (changed > 0)
                    {
                        report.changedSceneCount++;
                        EditorSceneManager.MarkSceneDirty(scene);
                        if (!EditorSceneManager.SaveScene(scene))
                        {
                            report.errorOrSkippedAssetCount++;
                            Debug.LogError("Sceneを保存できませんでした: " + path);
                        }
                    }
                }
                catch (Exception exception)
                {
                    report.errorOrSkippedAssetCount++;
                    Debug.LogError("Sceneのフォント適用に失敗しました: " + path + "\n" + exception);
                }
            }
        }
        finally
        {
            try
            {
                EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
            }
            catch (Exception exception)
            {
                report.errorOrSkippedAssetCount++;
                Debug.LogError("元のScene構成を復元できませんでした。\n" + exception);
            }

            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.SaveAssets();
        ShowReport(report, "全Sceneへの適用が完了しました。");
    }

    private void ApplyToAllPrefabs()
    {
        if (!ConfirmDirectApplication(
            "全Prefabへ適用",
            "プロジェクト内の全Prefabを読み込み、TMP Textを変更して保存します。\n\n" +
            "Prefab Variantも対象です。各Prefabのエラーは記録して次へ進みます。"))
        {
            return;
        }

        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
        JapaneseFontApplicationReport report = new JapaneseFontApplicationReport();
        try
        {
            for (int i = 0; i < prefabGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
                EditorUtility.DisplayProgressBar(
                    "Japanese Font Setup",
                    "Prefabを処理中: " + path,
                    prefabGuids.Length > 0 ? (float)i / prefabGuids.Length : 1f);
                report.examinedPrefabCount++;
                if (!IsAssetPath(path))
                {
                    report.errorOrSkippedAssetCount++;
                    continue;
                }

                GameObject prefabRoot = null;
                try
                {
                    prefabRoot = PrefabUtility.LoadPrefabContents(path);
                    int changed = ApplyFontToRoot(prefabRoot, selectedFontAsset, false, false);
                    report.changedTextCount += changed;
                    if (changed > 0)
                    {
                        EditorUtility.SetDirty(prefabRoot);
                        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
                        if (savedPrefab == null)
                        {
                            report.errorOrSkippedAssetCount++;
                            Debug.LogError("Prefabを保存できませんでした: " + path);
                        }
                        else
                        {
                            report.changedPrefabCount++;
                        }
                    }
                }
                catch (Exception exception)
                {
                    report.errorOrSkippedAssetCount++;
                    Debug.LogError("Prefabのフォント適用に失敗しました: " + path + "\n" + exception);
                }
                finally
                {
                    if (prefabRoot != null)
                    {
                        try
                        {
                            PrefabUtility.UnloadPrefabContents(prefabRoot);
                        }
                        catch (Exception exception)
                        {
                            report.errorOrSkippedAssetCount++;
                            Debug.LogError("Prefabのアンロードに失敗しました: " + path + "\n" + exception);
                        }
                    }
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.SaveAssets();
        ShowReport(report, "全Prefabへの適用が完了しました。");
    }

    private static int ApplyFontToScene(
        Scene scene,
        TMP_FontAsset fontAsset,
        bool recordUndo,
        bool recordPrefabOverrides)
    {
        if (!scene.IsValid() || !scene.isLoaded || fontAsset == null)
        {
            return 0;
        }

        int changedCount = 0;
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            changedCount += ApplyFontToRoot(
                roots[i],
                fontAsset,
                recordUndo,
                recordPrefabOverrides);
        }

        return changedCount;
    }

    private static int ApplyFontToRoot(
        GameObject root,
        TMP_FontAsset fontAsset,
        bool recordUndo,
        bool recordPrefabOverrides)
    {
        if (root == null || fontAsset == null)
        {
            return 0;
        }

        TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
        int changedCount = 0;
        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];
            if (text == null || text.font == fontAsset)
            {
                continue;
            }

            if (recordUndo)
            {
                Undo.RecordObject(text, "Apply Japanese TMP Font");
            }

            text.font = fontAsset;
            EditorUtility.SetDirty(text);
            if (recordPrefabOverrides && PrefabUtility.IsPartOfPrefabInstance(text))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(text);
            }

            changedCount++;
        }

        return changedCount;
    }

    private bool ConfirmDirectApplication(string title, string details)
    {
        if (!HasValidSelectedFont())
        {
            EditorUtility.DisplayDialog(
                "Japanese Font Setup",
                "Assetsフォルダ内のTMP Font Assetを指定してください。",
                "OK");
            return false;
        }

        return EditorUtility.DisplayDialog(
            title,
            details + "\n\n" +
            "重要: 保存されるFont AssetのGUIDはローカル環境だけで有効です。" +
            "変更したScene・PrefabをGitへコミットしないでください。",
            "適用する",
            "キャンセル");
    }

    private bool HasValidSelectedFont()
    {
        return selectedFontAsset != null &&
            IsAssetPath(AssetDatabase.GetAssetPath(selectedFontAsset));
    }

    private static bool ValidateSourceFont(
        Font font,
        out string message,
        out MessageType messageType)
    {
        message = "";
        messageType = MessageType.None;
        if (font == null)
        {
            return true;
        }

        string path = AssetDatabase.GetAssetPath(font);
        if (!IsAssetPath(path))
        {
            message = "元フォントはAssetsフォルダ内へ追加してから指定してください。";
            messageType = MessageType.Error;
            return false;
        }

        string extension = Path.GetExtension(path).ToLowerInvariant();
        if (extension != ".ttf" && extension != ".otf")
        {
            message = ".ttfまたは.otfのフォントを指定してください。";
            messageType = MessageType.Error;
            return false;
        }

        if (!path.StartsWith(LocalFontFolder + "/", StringComparison.Ordinal))
        {
            message = "Git除外済みの " + LocalFontFolder + " への配置を推奨します。";
            messageType = MessageType.Warning;
        }

        return true;
    }

    private static bool IsAssetPath(string path)
    {
        return !string.IsNullOrEmpty(path) &&
            (path == "Assets" || path.StartsWith("Assets/", StringComparison.Ordinal));
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string parent = Path.GetDirectoryName(folderPath).Replace('\\', '/');
        string name = Path.GetFileName(folderPath);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }

    private static void ShowReport(JapaneseFontApplicationReport report, string heading)
    {
        string message = heading + "\n\n" + report.CreateSummary();
        Debug.Log(message);
        EditorUtility.DisplayDialog("Japanese Font Setup", message, "OK");
    }
}

internal sealed class JapaneseFontApplicationReport
{
    public int examinedSceneCount;
    public int changedSceneCount;
    public int changedTextCount;
    public int examinedPrefabCount;
    public int changedPrefabCount;
    public int errorOrSkippedAssetCount;

    public string CreateSummary()
    {
        return "調査したScene数: " + examinedSceneCount +
            "\n変更したScene数: " + changedSceneCount +
            "\n変更したTMP Text数: " + changedTextCount +
            "\n調査したPrefab数: " + examinedPrefabCount +
            "\n変更したPrefab数: " + changedPrefabCount +
            "\nエラーまたはスキップしたアセット数: " + errorOrSkippedAssetCount;
    }
}

[InitializeOnLoad]
internal static class JapaneseFontSetupWarning
{
    private const string SettingsAssetPath = "Assets/Resources/JapaneseFontSettings.asset";
    private const string WarningSessionKey = "FantasyLoveSim.JapaneseFontSetup.WarningShown";

    static JapaneseFontSetupWarning()
    {
        EditorApplication.delayCall += WarnIfFontIsMissing;
    }

    private static void WarnIfFontIsMissing()
    {
        if (SessionState.GetBool(WarningSessionKey, false))
        {
            return;
        }

        JapaneseFontSettings settings =
            AssetDatabase.LoadAssetAtPath<JapaneseFontSettings>(SettingsAssetPath);
        if (settings == null || settings.defaultFontAsset != null)
        {
            return;
        }

        SessionState.SetBool(WarningSessionKey, true);
        Debug.LogWarning(
            "Japanese TextMeshPro font is not configured.\n" +
            "Open Tools > TextMeshPro > Japanese Font Setup and assign a TMP Font Asset.");
    }
}
