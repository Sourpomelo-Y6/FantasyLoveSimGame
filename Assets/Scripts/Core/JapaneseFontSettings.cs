using TMPro;
using UnityEngine;

/// <summary>
/// ローカル環境で使用する日本語 TMP Font Asset を保持します。
/// Git 上のアセットは未設定のまま維持してください。
/// </summary>
[CreateAssetMenu(
    fileName = "JapaneseFontSettings",
    menuName = "LoveSim/TextMeshPro/Japanese Font Settings")]
public sealed class JapaneseFontSettings : ScriptableObject
{
    public const string ResourcePath = "JapaneseFontSettings";

    [Tooltip("日本語表示に使用する TextMeshPro Font Asset。未設定時は既存フォントを変更しません。")]
    public TMP_FontAsset defaultFontAsset;

    public static JapaneseFontSettings Load()
    {
        return Resources.Load<JapaneseFontSettings>(ResourcePath);
    }

    public static bool TryGetFontAsset(out TMP_FontAsset fontAsset)
    {
        JapaneseFontSettings settings = Load();
        fontAsset = settings != null ? settings.defaultFontAsset : null;
        return fontAsset != null;
    }
}
