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

    [Header("Slot Buttons")]
    [SerializeField] private Button[] slotButtons;
    [SerializeField] private TextMeshProUGUI[] slotLabels;
    [SerializeField] private bool autoWireSlotButtons = true;
    [SerializeField] private int slotCount = 3;

    [Header("Labels")]
    [SerializeField] private string saveSlotLabelFormat = "Slot {0}";
    [SerializeField] private string emptySlotSuffix = " / Empty";
    [SerializeField] private string savedSlotSuffix = " / Saved";

    private SaveLoadPanelMode currentMode = SaveLoadPanelMode.Load;

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

        if (autoWireSlotButtons)
        {
            WireSlotButtons();
        }
    }

    private void OnEnable()
    {
        RefreshSlots();
    }

    public void OpenSave()
    {
        currentMode = SaveLoadPanelMode.Save;
        PanelRoot.SetActive(true);
        RefreshSlots();
    }

    public void OpenLoad()
    {
        currentMode = SaveLoadPanelMode.Load;
        PanelRoot.SetActive(true);
        RefreshSlots();
    }

    public void Close()
    {
        PanelRoot.SetActive(false);
    }

    public void SelectSlot(int slotIndex)
    {
        if (currentMode == SaveLoadPanelMode.Save)
        {
            SaveToSlot(slotIndex);
            RefreshSlots();
            return;
        }

        LoadFromSlot(slotIndex);
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
                slotLabels[i].text = CreateSlotLabel(i, hasSaveData);
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
            return;
        }

        if (titleManager != null)
        {
            titleManager.ContinueFromSelectedSlot(slotIndex);
            return;
        }

        Debug.LogWarning("SaveLoadPanel needs TitleManager or GameManager to load.");
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

    private string CreateSlotLabel(int slotIndex, bool hasSaveData)
    {
        string label = string.Format(saveSlotLabelFormat, slotIndex + 1);
        return label + (hasSaveData ? savedSlotSuffix : emptySlotSuffix);
    }
}
