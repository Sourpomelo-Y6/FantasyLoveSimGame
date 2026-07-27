using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Scene内の一般的なButtonへ、文字列IDによるUI SEを自動接続する。
/// 成否を伴うゲーム操作は個別処理から専用SEを再生する。
/// </summary>
public static class UiSePlayer
{
    public const string ConfirmSeId = "UI/Confirm";
    public const string CancelSeId = "UI/Cancel";
    public const string NextSeId = "UI/Next";

    public static int InstallSceneButtons()
    {
        int installedCount = 0;
        Button[] buttons = Resources.FindObjectsOfTypeAll<Button>();
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null ||
                !button.gameObject.scene.IsValid() ||
                button.hideFlags != HideFlags.None ||
                button.GetComponent<UiButtonSe>() != null)
            {
                continue;
            }

            string seId = ResolveDefaultSeId(button.gameObject.name);
            if (string.IsNullOrEmpty(seId))
            {
                continue;
            }

            UiButtonSe player = button.gameObject.AddComponent<UiButtonSe>();
            player.Initialize(seId);
            installedCount++;
        }

        return installedCount;
    }

    public static void SuppressAutomaticSe(Button button)
    {
        if (button == null || button.GetComponent<UiButtonSe>() != null)
        {
            return;
        }

        button.gameObject.AddComponent<UiButtonSe>().Initialize(string.Empty);
    }

    public static string ResolveDefaultSeId(string buttonName)
    {
        string name = buttonName ?? string.Empty;
        if (ContainsAny(name, "Attack", "Defend", "Skill", "Item", "Use",
            "Purchase", "Buy", "Acquire", "Training", "Advance", "Schedule"))
        {
            return string.Empty;
        }
        if (ContainsAny(name, "Close", "Cancel", "Back", "Quit", "Return"))
        {
            return CancelSeId;
        }
        if (ContainsAny(name, "Next", "Page"))
        {
            return NextSeId;
        }

        return ConfirmSeId;
    }

    private static bool ContainsAny(string source, params string[] values)
    {
        for (int i = 0; i < values.Length; i++)
        {
            if (source.IndexOf(values[i], StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }
}

[DisallowMultipleComponent]
public sealed class UiButtonSe : MonoBehaviour
{
    [SerializeField] private string seId;
    private Button button;
    private bool hooked;

    private void Awake()
    {
        Hook();
    }

    public void Initialize(string value)
    {
        seId = value ?? string.Empty;
        Hook();
    }

    private void Hook()
    {
        if (hooked)
        {
            return;
        }

        button = GetComponent<Button>();
        if (button == null)
        {
            return;
        }

        button.onClick.AddListener(Play);
        hooked = true;
    }

    private void Play()
    {
        if (button != null && button.interactable)
        {
            AudioManager.Instance.PlaySeById(seId);
        }
    }
}
