using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatusDetailPanel : MonoBehaviour
{
    [Header("Managers")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private HeroineStatus heroineStatus;

    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button closeButton;

    [Header("Detail View")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI statusSummaryText;
    [SerializeField] private Transform abilityListParent;
    [SerializeField] private Button abilityButtonPrefab;

    [Header("Acquire View")]
    [SerializeField] private GameObject abilityAcquirePanel;
    [SerializeField] private TextMeshProUGUI abilityAcquireTitleText;
    [SerializeField] private TextMeshProUGUI abilityAcquireDescriptionText;
    [SerializeField] private Button abilityAcquireButton;
    [SerializeField] private Button abilityAcquireBackButton;

    [Header("Ability Lists")]
    [SerializeField] private StatusAbilityKind[] playerAbilityKinds =
    {
        StatusAbilityKind.ConditionalOutfitPrompt,
        StatusAbilityKind.HiddenOutfitPrompt
    };

    [SerializeField] private StatusAbilityKind[] heroineAbilityKinds =
    {
        StatusAbilityKind.ConditionalOutfitPrompt,
        StatusAbilityKind.HiddenOutfitPrompt
    };

    [Header("Labels")]
    [SerializeField] private string playerTitle = "プレイヤー詳細ステータス";
    [SerializeField] private string heroineTitle = "ヒロイン詳細ステータス";
    [SerializeField] private string playerSummaryFormat = "プレイヤー能力\n衣装確認モード：{0}\nHidden解放：{1}";
    [SerializeField] private string heroineSummaryFormat = "ヒロイン能力\n衣装確認モード：{0}\nHidden解放：{1}";
    [SerializeField] private string conditionalAbilityName = "衣装確認モード: 条件表示";
    [SerializeField] private string hiddenAbilityName = "衣装確認モード: 非表示";
    [SerializeField] private string conditionalAbilityDescription = "衣装が予定に対して問題ない場合は、出発前の確認を省略できるようにします。";
    [SerializeField] private string hiddenAbilityDescription = "衣装確認そのものを省略し、予定開始時にそのまま進めるようにします。";
    [SerializeField] private string unlockedLabel = "解放済み";
    [SerializeField] private string lockedLabel = "未解放";
    [SerializeField] private string acquireButtonLabel = "解放する";
    [SerializeField] private string acquiredMessage = "解放しました。";

    private StatusDetailRole currentRole = StatusDetailRole.Player;
    private StatusAbilityKind selectedAbilityKind = StatusAbilityKind.ConditionalOutfitPrompt;
    private bool hasWarnedMissingReferences = false;

    private GameObject PanelRoot
    {
        get { return panelRoot != null ? panelRoot : gameObject; }
    }

    private void Awake()
    {
        EnsureUiReferences();

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Close);
        }

        if (abilityAcquireButton != null)
        {
            abilityAcquireButton.onClick.AddListener(UnlockSelectedAbility);
        }

        if (abilityAcquireBackButton != null)
        {
            abilityAcquireBackButton.onClick.AddListener(ShowDetailView);
        }

        HideAllViews();
    }

    private void OnEnable()
    {
        EnsureUiReferences();
        Refresh();
    }

    public void Initialize(GameManager manager, HeroineStatus heroine)
    {
        gameManager = manager;
        heroineStatus = heroine;
        EnsureUiReferences();
    }

    public void OpenPlayerDetail()
    {
        EnsureUiReferences();
        currentRole = StatusDetailRole.Player;
        PanelRoot.SetActive(true);
        ShowDetailView();
    }

    public void OpenHeroineDetail()
    {
        EnsureUiReferences();
        currentRole = StatusDetailRole.Heroine;
        PanelRoot.SetActive(true);
        ShowDetailView();
    }

    public void Close()
    {
        PanelRoot.SetActive(false);
    }

    public void ShowAbilityAcquirePanelForConditional()
    {
        EnsureUiReferences();
        ShowAbilityAcquireView(StatusAbilityKind.ConditionalOutfitPrompt);
    }

    public void ShowAbilityAcquirePanelForHidden()
    {
        EnsureUiReferences();
        ShowAbilityAcquireView(StatusAbilityKind.HiddenOutfitPrompt);
    }

    private void ShowDetailView()
    {
        EnsureUiReferences();

        if (abilityAcquirePanel != null)
        {
            abilityAcquirePanel.SetActive(false);
        }

        Refresh();
    }

    private void ShowAbilityAcquireView(StatusAbilityKind abilityKind)
    {
        EnsureUiReferences();
        selectedAbilityKind = abilityKind;

        if (abilityAcquirePanel != null)
        {
            abilityAcquirePanel.SetActive(true);
        }

        if (abilityAcquireTitleText != null)
        {
            abilityAcquireTitleText.text = GetAbilityName(abilityKind) + " の解放";
        }

        if (abilityAcquireDescriptionText != null)
        {
            abilityAcquireDescriptionText.text = GetAbilityDescription(abilityKind);
        }

        if (abilityAcquireButton != null)
        {
            abilityAcquireButton.interactable = !IsAbilityUnlocked(abilityKind);

            TextMeshProUGUI buttonLabel = abilityAcquireButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonLabel != null)
            {
                buttonLabel.text = IsAbilityUnlocked(abilityKind) ? unlockedLabel : acquireButtonLabel;
            }
        }
    }

    private void Refresh()
    {
        EnsureUiReferences();

        if (!PanelRoot.activeSelf)
        {
            return;
        }

        if (titleText != null)
        {
            titleText.text = currentRole == StatusDetailRole.Player ? playerTitle : heroineTitle;
        }

        if (statusSummaryText != null)
        {
            statusSummaryText.text = BuildStatusSummary();
        }

        RefreshAbilityList();
    }

    private void RefreshAbilityList()
    {
        if (abilityListParent == null || abilityButtonPrefab == null)
        {
            return;
        }

        ClearAbilityList();

        StatusAbilityKind[] abilityKinds = GetCurrentAbilityKinds();
        if (abilityKinds == null)
        {
            return;
        }

        foreach (StatusAbilityKind abilityKind in abilityKinds)
        {
            CreateAbilityButton(abilityKind);
        }
    }

    private void CreateAbilityButton(StatusAbilityKind abilityKind)
    {
        Button button = Instantiate(abilityButtonPrefab, abilityListParent);
        button.gameObject.SetActive(true);

        TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            buttonText.text = BuildAbilityButtonLabel(abilityKind);
        }

        button.onClick.AddListener(() => ShowAbilityAcquireView(abilityKind));
    }

    private void ClearAbilityList()
    {
        for (int i = abilityListParent.childCount - 1; i >= 0; i--)
        {
            Destroy(abilityListParent.GetChild(i).gameObject);
        }
    }

    private StatusAbilityKind[] GetCurrentAbilityKinds()
    {
        return currentRole == StatusDetailRole.Player
            ? playerAbilityKinds
            : heroineAbilityKinds;
    }

    private string BuildStatusSummary()
    {
        OutfitPromptAbilitySet abilities = GetCurrentAbilities();
        if (abilities == null)
        {
            return "能力情報が設定されていません。";
        }

        string conditionalLabel = abilities.canUseConditionalMode ? unlockedLabel : lockedLabel;
        string hiddenLabel = abilities.canUseHiddenMode ? unlockedLabel : lockedLabel;

        string format = currentRole == StatusDetailRole.Player
            ? playerSummaryFormat
            : heroineSummaryFormat;

        return string.Format(format, conditionalLabel, hiddenLabel);
    }

    private string BuildAbilityButtonLabel(StatusAbilityKind abilityKind)
    {
        return GetAbilityName(abilityKind) + " / " + GetAbilityStateText(abilityKind);
    }

    private string GetAbilityStateText(StatusAbilityKind abilityKind)
    {
        return IsAbilityUnlocked(abilityKind) ? unlockedLabel : lockedLabel;
    }

    private string GetAbilityName(StatusAbilityKind abilityKind)
    {
        switch (abilityKind)
        {
            case StatusAbilityKind.HiddenOutfitPrompt:
                return hiddenAbilityName;
            default:
                return conditionalAbilityName;
        }
    }

    private string GetAbilityDescription(StatusAbilityKind abilityKind)
    {
        switch (abilityKind)
        {
            case StatusAbilityKind.HiddenOutfitPrompt:
                return hiddenAbilityDescription;
            default:
                return conditionalAbilityDescription;
        }
    }

    private bool IsAbilityUnlocked(StatusAbilityKind abilityKind)
    {
        OutfitPromptAbilitySet abilities = GetCurrentAbilities();
        if (abilities == null)
        {
            return false;
        }

        switch (abilityKind)
        {
            case StatusAbilityKind.HiddenOutfitPrompt:
                return abilities.canUseHiddenMode;
            default:
                return abilities.canUseConditionalMode;
        }
    }

    private void UnlockSelectedAbility()
    {
        OutfitPromptAbilitySet abilities = GetCurrentAbilities();
        if (abilities == null)
        {
            return;
        }

        switch (selectedAbilityKind)
        {
            case StatusAbilityKind.HiddenOutfitPrompt:
                abilities.canUseHiddenMode = true;
                break;

            default:
                abilities.canUseConditionalMode = true;
                break;
        }

        Refresh();

        if (abilityAcquireDescriptionText != null)
        {
            abilityAcquireDescriptionText.text = GetAbilityDescription(selectedAbilityKind) + "\n" + acquiredMessage;
        }

        if (abilityAcquireButton != null)
        {
            abilityAcquireButton.interactable = false;
            TextMeshProUGUI buttonLabel = abilityAcquireButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonLabel != null)
            {
                buttonLabel.text = unlockedLabel;
            }
        }
    }

    private OutfitPromptAbilitySet GetCurrentAbilities()
    {
        if (currentRole == StatusDetailRole.Player)
        {
            return gameManager != null ? gameManager.PlayerOutfitPromptAbilities : null;
        }

        return heroineStatus != null ? heroineStatus.OutfitPromptAbilities : null;
    }

    private void HideAllViews()
    {
        if (abilityAcquirePanel != null)
        {
            abilityAcquirePanel.SetActive(false);
        }
    }

    private void EnsureUiReferences()
    {
        if (HasRequiredReferences() || hasWarnedMissingReferences)
        {
            return;
        }

        Debug.LogWarning("StatusDetailPanel の UI 参照が不足しています。Hierarchy 上に UI を配置し、Inspector で参照を割り当ててください。");
        hasWarnedMissingReferences = true;
    }

    private bool HasRequiredReferences()
    {
        return titleText != null &&
            statusSummaryText != null &&
            abilityListParent != null &&
            abilityButtonPrefab != null &&
            abilityAcquirePanel != null &&
            abilityAcquireTitleText != null &&
            abilityAcquireDescriptionText != null &&
            abilityAcquireButton != null &&
            abilityAcquireBackButton != null;
    }
}
