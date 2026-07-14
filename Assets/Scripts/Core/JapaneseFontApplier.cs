using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// JapaneseFontSettings に設定されたフォントを子階層またはロード済みSceneへ適用します。
/// 設定がない場合は現在のフォントを変更しません。
/// </summary>
[DefaultExecutionOrder(-10000)]
public sealed class JapaneseFontApplier : MonoBehaviour
{
    [SerializeField] private bool includeInactive = true;
    [SerializeField] private bool applyToAllLoadedScenes;
    [SerializeField] private bool applyOnEnable = true;

    private bool sceneCallbackRegistered;

    private void OnEnable()
    {
        if (applyToAllLoadedScenes)
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            sceneCallbackRegistered = true;
        }

        if (applyOnEnable)
        {
            ApplyConfiguredFont();
        }
    }

    private void OnDisable()
    {
        if (sceneCallbackRegistered)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            sceneCallbackRegistered = false;
        }
    }

    public int ApplyConfiguredFont()
    {
        TMP_FontAsset fontAsset;
        if (!JapaneseFontSettings.TryGetFontAsset(out fontAsset))
        {
            return 0;
        }

        return applyToAllLoadedScenes
            ? ApplyToLoadedScenes(fontAsset)
            : ApplyToHierarchy(gameObject, fontAsset, includeInactive);
    }

    public static int ApplyToHierarchy(
        GameObject root,
        TMP_FontAsset fontAsset,
        bool includeInactiveObjects = true)
    {
        if (root == null || fontAsset == null)
        {
            return 0;
        }

        TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(includeInactiveObjects);
        return ApplyToTexts(texts, fontAsset);
    }

    public static int ApplyToLoadedScenes(TMP_FontAsset fontAsset)
    {
        if (fontAsset == null)
        {
            return 0;
        }

        int changedCount = 0;
        for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
        {
            Scene scene = SceneManager.GetSceneAt(sceneIndex);
            if (!scene.isLoaded)
            {
                continue;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                changedCount += ApplyToHierarchy(roots[rootIndex], fontAsset, true);
            }
        }

        return changedCount;
    }

    private static int ApplyToTexts(TMP_Text[] texts, TMP_FontAsset fontAsset)
    {
        if (texts == null || fontAsset == null)
        {
            return 0;
        }

        int changedCount = 0;
        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];
            if (text == null || text.font == fontAsset)
            {
                continue;
            }

            text.font = fontAsset;
            changedCount++;
        }

        return changedCount;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TMP_FontAsset fontAsset;
        if (!JapaneseFontSettings.TryGetFontAsset(out fontAsset))
        {
            return;
        }

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            ApplyToHierarchy(roots[i], fontAsset, true);
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateRuntimeApplier()
    {
        TMP_FontAsset fontAsset;
        if (!JapaneseFontSettings.TryGetFontAsset(out fontAsset))
        {
            return;
        }

        GameObject applierObject = new GameObject("JapaneseFontApplier");
        applierObject.SetActive(false);
        JapaneseFontApplier applier = applierObject.AddComponent<JapaneseFontApplier>();
        applier.applyToAllLoadedScenes = true;
        applier.includeInactive = true;
        Object.DontDestroyOnLoad(applierObject);
        applierObject.SetActive(true);
    }
}
