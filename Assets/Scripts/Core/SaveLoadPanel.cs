using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SaveLoadPanel : MonoBehaviour
{
    [Header("Scene Managers")]
    [SerializeField] private TitleManager titleManager;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private SaveManager saveManager;

    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button closeButton;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TextMeshProUGUI titleText;

    [SerializeField] private Color saveBackgroundColor = Color.blue;
    [SerializeField] private Color loadBackgroundColor = new Color(1f, 0.39f, 0.2f, 0.78f);
    [SerializeField] private string saveTitle = "セーブ";
    [SerializeField] private string loadTitle = "ロード";

    [Header("Slot Buttons")]
    [SerializeField] private Button[] slotButtons;
    [SerializeField] private TextMeshProUGUI[] slotLabels;
    [SerializeField] private bool autoWireSlotButtons = true;
    [SerializeField] private int slotCount = 3;

    [Header("Labels")]
    [SerializeField] private string saveSlotLabelFormat = "Slot {0}";
    [SerializeField] private string emptySlotSuffix = " / Empty";
    [SerializeField] private string savedSlotSuffix = " / Saved";
    [SerializeField] private string savedSlotDetailFormat = " / Day {0} / Affection {1}";

    [Header("Confirmation")]
    [SerializeField] private GameObject confirmPanel;
    [SerializeField] private TextMeshProUGUI confirmMessageText;
    [SerializeField] private Button confirmOkButton;
    [SerializeField] private Button confirmCancelButton;
    [SerializeField] private string overwriteConfirmMessageFormat = "Slot {0} を上書きしますか？";
    [SerializeField] private string loadConfirmMessageFormat = "Slot {0} をロードしますか？";

    private SaveLoadPanelMode currentMode = SaveLoadPanelMode.Load;
    private SaveLoadPanelMode pendingMode = SaveLoadPanelMode.Load;
    private int pendingSlotIndex = -1;

    private GameObject PanelRoot
    {
        get { return panelRoot != null ? panelRoot : gameObject; }
    }

    private void Awake()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Close);
        }

        if (confirmOkButton != null)
        {
            confirmOkButton.onClick.AddListener(ConfirmPendingAction);
        }

        if (confirmCancelButton != null)
        {
            confirmCancelButton.onClick.AddListener(CancelPendingAction);
        }

        if (autoWireSlotButtons)
        {
            WireSlotButtons();
        }

        HideConfirmPanel();
        PanelRoot.SetActive(false);
    }

    private void OnEnable()
    {
        RefreshSlots();
    }

    public void OpenSave()
    {
        currentMode = SaveLoadPanelMode.Save;
        ClearPendingAction();
        ApplyModeVisuals();
        PanelRoot.SetActive(true);
        RefreshSlots();
    }

    public void OpenLoad()
    {
        currentMode = SaveLoadPanelMode.Load;
        ClearPendingAction();
        ApplyModeVisuals();
        PanelRoot.SetActive(true);
        RefreshSlots();
    }

    private void ApplyModeVisuals()
    {
        if (backgroundImage != null)
        {
            backgroundImage.color = currentMode == SaveLoadPanelMode.Save
                ? saveBackgroundColor
                : loadBackgroundColor;
        }

        if (titleText != null)
        {
            titleText.text = currentMode == SaveLoadPanelMode.Save
                ? saveTitle
                : loadTitle;
        }
    }

    public void Close()
    {
        ClearPendingAction();
        PanelRoot.SetActive(false);
    }

    public void SelectSlot(int slotIndex)
    {
        if (currentMode == SaveLoadPanelMode.Save)
        {
            RequestSaveToSlot(slotIndex);
            return;
        }

        RequestLoadFromSlot(slotIndex);
    }

    public void ConfirmPendingAction()
    {
        if (pendingSlotIndex < 0)
        {
            HideConfirmPanel();
            return;
        }

        int slotIndex = pendingSlotIndex;
        SaveLoadPanelMode mode = pendingMode;

        ClearPendingAction();

        if (mode == SaveLoadPanelMode.Save)
        {
            SaveToSlot(slotIndex);
            RefreshSlots();
            return;
        }

        LoadFromSlot(slotIndex);
    }

    public void CancelPendingAction()
    {
        ClearPendingAction();
    }

    public void RefreshSlots()
    {
        int count = GetSlotCount();

        if (slotButtons == null)
        {
            return;
        }

        for (int i = 0; i < slotButtons.Length; i++)
        {
            bool isValidSlot = i < count;
            bool hasSaveData = isValidSlot && HasSaveData(i);

            if (slotButtons[i] != null)
            {
                slotButtons[i].interactable = isValidSlot &&
                    (currentMode == SaveLoadPanelMode.Save || hasSaveData);
            }

            if (slotLabels != null && i < slotLabels.Length && slotLabels[i] != null)
            {
                SaveData saveData = hasSaveData ? GetSavePreview(i) : null;
                slotLabels[i].text = CreateSlotLabel(i, hasSaveData, saveData);
            }
        }
    }

    private void WireSlotButtons()
    {
        if (slotButtons == null)
        {
            return;
        }

        for (int i = 0; i < slotButtons.Length; i++)
        {
            if (slotButtons[i] == null)
            {
                continue;
            }

            int slotIndex = i;
            slotButtons[i].onClick.AddListener(() => SelectSlot(slotIndex));
        }
    }

    private void RequestSaveToSlot(int slotIndex)
    {
        if (HasSaveData(slotIndex))
        {
            ShowConfirmPanel(SaveLoadPanelMode.Save, slotIndex);
            return;
        }

        SaveToSlot(slotIndex);
        RefreshSlots();
    }

    private void RequestLoadFromSlot(int slotIndex)
    {
        if (!HasSaveData(slotIndex))
        {
            RefreshSlots();
            return;
        }

        ShowConfirmPanel(SaveLoadPanelMode.Load, slotIndex);
    }

    private void SaveToSlot(int slotIndex)
    {
        if (gameManager == null)
        {
            Debug.LogWarning("SaveLoadPanel needs GameManager to save.");
            return;
        }

        gameManager.SaveGameToSlot(slotIndex);
    }

    private void LoadFromSlot(int slotIndex)
    {
        if (!HasSaveData(slotIndex))
        {
            RefreshSlots();
            return;
        }

        if (gameManager != null)
        {
            gameManager.LoadGameFromSlot(slotIndex);
            Close();
            return;
        }

        if (titleManager != null)
        {
            titleManager.ContinueFromSelectedSlot(slotIndex);
            return;
        }

        Debug.LogWarning("SaveLoadPanel needs TitleManager or GameManager to load.");
    }

    private void ShowConfirmPanel(SaveLoadPanelMode mode, int slotIndex)
    {
        if (confirmPanel == null)
        {
            if (mode == SaveLoadPanelMode.Save)
            {
                SaveToSlot(slotIndex);
                RefreshSlots();
                return;
            }

            LoadFromSlot(slotIndex);
            return;
        }

        pendingMode = mode;
        pendingSlotIndex = slotIndex;

        if (confirmMessageText != null)
        {
            string messageFormat = mode == SaveLoadPanelMode.Save
                ? overwriteConfirmMessageFormat
                : loadConfirmMessageFormat;
            confirmMessageText.text = string.Format(messageFormat, slotIndex + 1);
        }

        confirmPanel.SetActive(true);
    }

    private void HideConfirmPanel()
    {
        if (confirmPanel != null)
        {
            confirmPanel.SetActive(false);
        }
    }

    private void ClearPendingAction()
    {
        pendingSlotIndex = -1;
        HideConfirmPanel();
    }

    private bool HasSaveData(int slotIndex)
    {
        if (saveManager != null)
        {
            return saveManager.HasSaveData(slotIndex);
        }

        if (gameManager != null)
        {
            return gameManager.HasSaveDataInSlot(slotIndex);
        }

        if (titleManager != null)
        {
            return titleManager.HasSaveDataInSlot(slotIndex);
        }

        return false;
    }

    private SaveData GetSavePreview(int slotIndex)
    {
        if (saveManager != null)
        {
            return saveManager.LoadPreview(slotIndex);
        }

        return null;
    }

    private int GetSlotCount()
    {
        if (saveManager != null)
        {
            return saveManager.SaveSlotCount;
        }

        if (titleManager != null)
        {
            return titleManager.GetSaveSlotCount();
        }

        return Mathf.Max(0, slotCount);
    }

    private string CreateSlotLabel(int slotIndex, bool hasSaveData, SaveData saveData)
    {
        string label = string.Format(saveSlotLabelFormat, slotIndex + 1);

        if (!hasSaveData)
        {
            return label + emptySlotSuffix;
        }

        if (saveData == null)
        {
            return label + savedSlotSuffix;
        }

        return label + string.Format(savedSlotDetailFormat, saveData.day, saveData.affection);
    }
}
