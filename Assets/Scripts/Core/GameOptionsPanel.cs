using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class GameOptionsPanel : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Toggle dialogueClickAdvanceToggle;
    [SerializeField] private Button closeButton;
    [SerializeField] private TextMeshProUGUI resultText;

    private bool isRefreshing;

    private GameObject PanelRoot => panelRoot != null ? panelRoot : gameObject;

    private void Awake()
    {
        if (dialogueClickAdvanceToggle != null)
        {
            dialogueClickAdvanceToggle.onValueChanged.AddListener(OnToggleChanged);
        }
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Close);
        }
    }

    public void Open()
    {
        Refresh();
        PanelRoot.SetActive(true);
    }

    public void Close()
    {
        PanelRoot.SetActive(false);
    }

    public void Refresh()
    {
        isRefreshing = true;
        if (dialogueClickAdvanceToggle != null)
        {
            dialogueClickAdvanceToggle.isOn =
                GameOptionsManager.DialogueWindowClickAdvanceEnabled;
        }
        if (resultText != null) resultText.text = string.Empty;
        isRefreshing = false;
    }

    private void OnToggleChanged(bool enabled)
    {
        if (isRefreshing) return;

        string message;
        bool saved = GameOptionsManager.SetDialogueWindowClickAdvance(enabled, out message);
        if (!saved)
        {
            isRefreshing = true;
            dialogueClickAdvanceToggle.isOn =
                GameOptionsManager.DialogueWindowClickAdvanceEnabled;
            isRefreshing = false;
        }

        if (resultText != null)
        {
            resultText.text = saved ? "設定を保存しました。" : message;
        }
    }
}
