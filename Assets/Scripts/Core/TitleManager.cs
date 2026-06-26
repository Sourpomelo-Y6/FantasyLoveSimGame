using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class TitleManager : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button quitButton;

    [Header("Save")]
    [SerializeField] private SaveManager saveManager;

    [Header("Scene")]
    [SerializeField] private string mainSceneName = "MainScene";

    [Header("Heroine Select")]
    [SerializeField] private Button heroineSelectButton;
    [SerializeField] private GameObject heroineSelectPanel;
    [SerializeField] private Button previousHeroineButton;
    [SerializeField] private Button nextHeroineButton;
    [SerializeField] private Button confirmHeroineButton;
    [SerializeField] private Button closeHeroineSelectButton;
    [SerializeField] private Image heroinePreviewImage;
    [SerializeField] private TextMeshProUGUI heroineNameText;
    [SerializeField] private TextMeshProUGUI heroineInfoText;
    [SerializeField] private string defaultHeroineId = "DefaultHeroine";
    [SerializeField] private string noHeroineMessage = "選択できるキャラクターがありません。";

    private HeroineProfileData[] heroineProfiles = new HeroineProfileData[0];
    private int previewHeroineIndex = -1;
    private int selectedHeroineIndex = -1;

    private void Start()
    {
        newGameButton.onClick.AddListener(OnClickNewGame);
        //continueButton.onClick.AddListener(OnClickContinue);
        quitButton.onClick.AddListener(OnClickQuit);

        LoadHeroineProfiles();
        WireHeroineSelectButtons();
        CloseHeroineSelection();

        RefreshContinueButton();
    }

    private void RefreshContinueButton()
    {
        if (saveManager == null)
        {
            continueButton.interactable = false;
            return;
        }

        continueButton.interactable = saveManager.HasSaveData();
    }

    private void OnClickNewGame()
    {
        ApplySelectedHeroineToGameStartSettings();
        GameStartSettings.ShouldLoadOnStart = false;
        GameStartSettings.ShouldPlayGameStartEvent = true;
        SceneManager.LoadScene(mainSceneName);
    }

    private void OnClickContinue()
    {
        ContinueFromSelectedSlot();
    }

    public void ContinueFromSelectedSlot()
    {
        GameStartSettings.SelectedHeroineId = "";
        GameStartSettings.ShouldLoadOnStart = true;
        GameStartSettings.ShouldPlayGameStartEvent = false;
        SceneManager.LoadScene(mainSceneName);
    }

    public void ContinueFromSelectedSlot(int slotIndex)
    {
        SelectSaveSlot(slotIndex);

        if (!HasSaveDataInSlot(GameStartSettings.SelectedSaveSlotIndex))
        {
            RefreshContinueButton();
            return;
        }

        ContinueFromSelectedSlot();
    }

    public void SelectSaveSlot(int slotIndex)
    {
        if (saveManager == null)
        {
            return;
        }

        saveManager.SetCurrentSlotIndex(slotIndex);
        GameStartSettings.SelectedSaveSlotIndex = saveManager.CurrentSlotIndex;
        RefreshContinueButton();
    }

    public bool HasSaveDataInSlot(int slotIndex)
    {
        return saveManager != null && saveManager.HasSaveData(slotIndex);
    }

    public int GetSaveSlotCount()
    {
        return saveManager != null ? saveManager.SaveSlotCount : 0;
    }

    public void OpenHeroineSelection()
    {
        if (heroineProfiles == null || heroineProfiles.Length == 0)
        {
            LoadHeroineProfiles();
        }

        if (heroineSelectPanel != null)
        {
            heroineSelectPanel.SetActive(true);
        }

        if (previewHeroineIndex < 0 && heroineProfiles.Length > 0)
        {
            previewHeroineIndex = selectedHeroineIndex >= 0 ? selectedHeroineIndex : 0;
        }

        RefreshHeroinePreview();
    }

    public void CloseHeroineSelection()
    {
        if (heroineSelectPanel != null)
        {
            heroineSelectPanel.SetActive(false);
        }
    }

    public void SelectPreviousHeroine()
    {
        if (heroineProfiles == null || heroineProfiles.Length == 0)
        {
            return;
        }

        previewHeroineIndex--;
        if (previewHeroineIndex < 0)
        {
            previewHeroineIndex = heroineProfiles.Length - 1;
        }

        RefreshHeroinePreview();
    }

    public void SelectNextHeroine()
    {
        if (heroineProfiles == null || heroineProfiles.Length == 0)
        {
            return;
        }

        previewHeroineIndex++;
        if (previewHeroineIndex >= heroineProfiles.Length)
        {
            previewHeroineIndex = 0;
        }

        RefreshHeroinePreview();
    }

    public void SelectHeroineByIndex(int index)
    {
        if (heroineProfiles == null ||
            index < 0 ||
            index >= heroineProfiles.Length)
        {
            return;
        }

        previewHeroineIndex = index;
        RefreshHeroinePreview();
    }

    public void ConfirmHeroineSelection()
    {
        if (heroineProfiles == null ||
            previewHeroineIndex < 0 ||
            previewHeroineIndex >= heroineProfiles.Length)
        {
            return;
        }

        selectedHeroineIndex = previewHeroineIndex;
        ApplySelectedHeroineToGameStartSettings();
        RefreshHeroinePreview();
        CloseHeroineSelection();
    }

    private void LoadHeroineProfiles()
    {
        heroineProfiles = Resources.LoadAll<HeroineProfileData>("Heroines");
        System.Array.Sort(
            heroineProfiles,
            (left, right) => string.Compare(GetHeroineSortKey(left), GetHeroineSortKey(right), System.StringComparison.Ordinal));

        selectedHeroineIndex = FindHeroineIndex(GameStartSettings.SelectedHeroineId);
        if (selectedHeroineIndex < 0)
        {
            selectedHeroineIndex = FindHeroineIndex(defaultHeroineId);
        }

        if (selectedHeroineIndex < 0 && heroineProfiles.Length > 0)
        {
            selectedHeroineIndex = 0;
        }

        previewHeroineIndex = selectedHeroineIndex;
        ApplySelectedHeroineToGameStartSettings();
        RefreshHeroinePreview();
    }

    private void WireHeroineSelectButtons()
    {
        if (heroineSelectButton != null)
        {
            heroineSelectButton.onClick.AddListener(OpenHeroineSelection);
        }

        if (previousHeroineButton != null)
        {
            previousHeroineButton.onClick.AddListener(SelectPreviousHeroine);
        }

        if (nextHeroineButton != null)
        {
            nextHeroineButton.onClick.AddListener(SelectNextHeroine);
        }

        if (confirmHeroineButton != null)
        {
            confirmHeroineButton.onClick.AddListener(ConfirmHeroineSelection);
        }

        if (closeHeroineSelectButton != null)
        {
            closeHeroineSelectButton.onClick.AddListener(CloseHeroineSelection);
        }
    }

    private void RefreshHeroinePreview()
    {
        HeroineProfileData profile = GetPreviewHeroineProfile();
        if (profile == null)
        {
            if (heroineNameText != null)
            {
                heroineNameText.text = noHeroineMessage;
            }

            if (heroineInfoText != null)
            {
                heroineInfoText.text = "";
            }

            if (heroinePreviewImage != null)
            {
                heroinePreviewImage.sprite = null;
                heroinePreviewImage.gameObject.SetActive(false);
            }

            SetHeroineNavigationInteractable(false);
            return;
        }

        if (heroineNameText != null)
        {
            heroineNameText.text = string.IsNullOrWhiteSpace(profile.displayName)
                ? profile.heroineId
                : profile.displayName;
        }

        if (heroineInfoText != null)
        {
            heroineInfoText.text =
                "ID: " +
                profile.heroineId +
                "\nActions: " +
                profile.actionResourcePath +
                "\nConversations: " +
                profile.conversationResourcePath;
        }

        if (heroinePreviewImage != null)
        {
            heroinePreviewImage.sprite = profile.defaultHeroineSprite;
            heroinePreviewImage.preserveAspect = true;
            heroinePreviewImage.gameObject.SetActive(profile.defaultHeroineSprite != null);
        }

        SetHeroineNavigationInteractable(heroineProfiles.Length > 1);
        if (confirmHeroineButton != null)
        {
            confirmHeroineButton.interactable = true;
        }
    }

    private void SetHeroineNavigationInteractable(bool interactable)
    {
        if (previousHeroineButton != null)
        {
            previousHeroineButton.interactable = interactable;
        }

        if (nextHeroineButton != null)
        {
            nextHeroineButton.interactable = interactable;
        }

        if (confirmHeroineButton != null)
        {
            confirmHeroineButton.interactable = heroineProfiles != null && heroineProfiles.Length > 0;
        }
    }

    private HeroineProfileData GetPreviewHeroineProfile()
    {
        if (heroineProfiles == null ||
            previewHeroineIndex < 0 ||
            previewHeroineIndex >= heroineProfiles.Length)
        {
            return null;
        }

        return heroineProfiles[previewHeroineIndex];
    }

    private void ApplySelectedHeroineToGameStartSettings()
    {
        if (heroineProfiles == null ||
            selectedHeroineIndex < 0 ||
            selectedHeroineIndex >= heroineProfiles.Length)
        {
            GameStartSettings.SelectedHeroineId = "";
            return;
        }

        GameStartSettings.SelectedHeroineId = heroineProfiles[selectedHeroineIndex].heroineId;
    }

    private int FindHeroineIndex(string heroineId)
    {
        if (heroineProfiles == null || string.IsNullOrWhiteSpace(heroineId))
        {
            return -1;
        }

        for (int i = 0; i < heroineProfiles.Length; i++)
        {
            HeroineProfileData profile = heroineProfiles[i];
            if (profile != null &&
                string.Equals(profile.heroineId, heroineId, System.StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }

    private static string GetHeroineSortKey(HeroineProfileData profile)
    {
        if (profile == null)
        {
            return "";
        }

        if (!string.IsNullOrWhiteSpace(profile.displayName))
        {
            return profile.displayName;
        }

        return profile.heroineId;
    }

    private void OnClickQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
