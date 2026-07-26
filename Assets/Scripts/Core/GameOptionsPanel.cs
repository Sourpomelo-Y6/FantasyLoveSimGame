using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class GameOptionsPanel : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Toggle dialogueClickAdvanceToggle;
    [Header("Audio")]
    [SerializeField] private Slider bgmVolumeSlider;
    [SerializeField] private Toggle bgmMuteToggle;
    [SerializeField] private Slider seVolumeSlider;
    [SerializeField] private Toggle seMuteToggle;
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
        if (bgmVolumeSlider != null)
        {
            bgmVolumeSlider.onValueChanged.AddListener(OnBgmVolumeChanged);
        }
        if (bgmMuteToggle != null)
        {
            bgmMuteToggle.onValueChanged.AddListener(OnBgmMuteChanged);
        }
        if (seVolumeSlider != null)
        {
            seVolumeSlider.onValueChanged.AddListener(OnSeVolumeChanged);
        }
        if (seMuteToggle != null)
        {
            seMuteToggle.onValueChanged.AddListener(OnSeMuteChanged);
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
        if (bgmVolumeSlider != null)
        {
            bgmVolumeSlider.SetValueWithoutNotify(GameOptionsManager.BgmVolume);
        }
        if (bgmMuteToggle != null)
        {
            bgmMuteToggle.SetIsOnWithoutNotify(GameOptionsManager.BgmMuted);
        }
        if (seVolumeSlider != null)
        {
            seVolumeSlider.SetValueWithoutNotify(GameOptionsManager.SeVolume);
        }
        if (seMuteToggle != null)
        {
            seMuteToggle.SetIsOnWithoutNotify(GameOptionsManager.SeMuted);
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

    private void OnBgmVolumeChanged(float volume)
    {
        SaveAudioOption(
            (out string message) => GameOptionsManager.SetBgmVolume(volume, out message));
    }

    private void OnBgmMuteChanged(bool muted)
    {
        SaveAudioOption(
            (out string message) => GameOptionsManager.SetBgmMuted(muted, out message));
    }

    private void OnSeVolumeChanged(float volume)
    {
        SaveAudioOption(
            (out string message) => GameOptionsManager.SetSeVolume(volume, out message));
    }

    private void OnSeMuteChanged(bool muted)
    {
        SaveAudioOption(
            (out string message) => GameOptionsManager.SetSeMuted(muted, out message));
    }

    private void SaveAudioOption(OptionSaver save)
    {
        if (isRefreshing)
        {
            return;
        }

        string message;
        bool saved = save(out message);
        if (!saved)
        {
            Refresh();
        }

        if (resultText != null)
        {
            resultText.text = saved ? "設定を保存しました。" : message;
        }
    }

    private delegate bool OptionSaver(out string message);
}
