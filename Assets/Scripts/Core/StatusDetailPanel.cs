using TMPro;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StatusDetailPanel : MonoBehaviour
{
    [Header("Managers")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private PlayerStatus playerStatus;
    [SerializeField] private HeroineStatus heroineStatus;
    [SerializeField] private TimeManager timeManager;

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
    [SerializeField] private string statusAbilityResourcePath = "StatusAbilities";
    [SerializeField] private StatusAbilityData[] playerAbilities;
    [SerializeField] private StatusAbilityData[] heroineAbilities;

    [Header("Labels")]
    [SerializeField] private string playerTitle = "プレイヤー詳細ステータス";
    [SerializeField] private string heroineTitle = "ヒロイン詳細ステータス";
    [SerializeField] private string playerSummaryTitle = "プレイヤー能力";
    [SerializeField] private string heroineSummaryTitle = "ヒロイン能力";
    [SerializeField] private string conditionalAbilityName = "衣装確認モード: 条件表示";
    [SerializeField] private string hiddenAbilityName = "衣装確認モード: 非表示";
    [SerializeField] private string conditionalAbilityDescription = "衣装が予定に対して問題ない場合は、出発前の確認を省略できるようにします。";
    [SerializeField] private string hiddenAbilityDescription = "衣装確認そのものを省略し、予定開始時にそのまま進めるようにします。";
    [SerializeField] private string unlockedLabel = "解放済み";
    [SerializeField] private string lockedLabel = "未解放";
    [SerializeField] private string acquireButtonLabel = "解放する";
    [SerializeField] private string acquiredMessage = "解放しました。";

    private StatusDetailRole currentRole = StatusDetailRole.Player;
    private StatusAbilityData selectedAbility;
    private StatusAbilityKind selectedAbilityKind = StatusAbilityKind.ConditionalOutfitPrompt;
    private bool hasWarnedMissingReferences = false;
    private List<StatusAbilityData> loadedAbilities = new List<StatusAbilityData>();

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
        playerStatus = manager != null ? manager.PlayerStatus : playerStatus;
        EnsureUiReferences();
    }

    public void Initialize(GameManager manager, HeroineStatus heroine, TimeManager time)
    {
        gameManager = manager;
        heroineStatus = heroine;
        timeManager = time;
        playerStatus = manager != null ? manager.PlayerStatus : playerStatus;
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

    public void RefreshStatusDisplay()
    {
        Refresh();
    }

    public void ShowAbilityAcquirePanelForConditional()
    {
        EnsureUiReferences();
        ShowAbilityAcquireView(StatusAbilityKind.ConditionalOutfitPrompt, null);
    }

    public void ShowAbilityAcquirePanelForHidden()
    {
        EnsureUiReferences();
        ShowAbilityAcquireView(StatusAbilityKind.HiddenOutfitPrompt, null);
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

    private void ShowAbilityAcquireView(StatusAbilityKind abilityKind, StatusAbilityData ability)
    {
        EnsureUiReferences();
        selectedAbility = ability;
        selectedAbilityKind = abilityKind;

        if (abilityAcquirePanel != null)
        {
            abilityAcquirePanel.SetActive(true);
        }

        if (abilityAcquireTitleText != null)
        {
            abilityAcquireTitleText.text = GetAbilityName(abilityKind, ability) + " の解放";
        }

        if (abilityAcquireDescriptionText != null)
        {
            abilityAcquireDescriptionText.text = BuildAbilityAcquireDescription(abilityKind, ability);
        }

        if (abilityAcquireButton != null)
        {
            bool canUnlock = CanUnlockAbility(abilityKind, ability);
            abilityAcquireButton.interactable = !IsAbilityUnlocked(abilityKind, ability) && canUnlock;

            TextMeshProUGUI buttonLabel = abilityAcquireButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonLabel != null)
            {
                buttonLabel.text = IsAbilityUnlocked(abilityKind, ability) ? unlockedLabel : acquireButtonLabel;
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

        StatusAbilityData[] abilities = GetCurrentAbilitiesForList();
        if (abilities == null)
        {
            return;
        }

        foreach (StatusAbilityData ability in abilities)
        {
            if (ability == null || !ability.isEnabled || ability.targetRole != currentRole)
            {
                continue;
            }

            CreateAbilityButton(ability);
        }
    }

    private void CreateAbilityButton(StatusAbilityData ability)
    {
        Button button = Instantiate(abilityButtonPrefab, abilityListParent);
        button.gameObject.SetActive(true);

        TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            buttonText.text = BuildAbilityButtonLabel(ability);
        }

        button.onClick.AddListener(() => ShowAbilityAcquireView(ability.abilityKind, ability));
    }

    private void ClearAbilityList()
    {
        for (int i = abilityListParent.childCount - 1; i >= 0; i--)
        {
            Destroy(abilityListParent.GetChild(i).gameObject);
        }
    }

    private StatusAbilityData[] GetCurrentAbilitiesForList()
    {
        StatusAbilityData[] configuredAbilities = currentRole == StatusDetailRole.Player
            ? playerAbilities
            : heroineAbilities;

        if (configuredAbilities != null && configuredAbilities.Length > 0)
        {
            return configuredAbilities;
        }

        LoadStatusAbilitiesFromResources();

        List<StatusAbilityData> roleAbilities = new List<StatusAbilityData>();
        foreach (StatusAbilityData ability in loadedAbilities)
        {
            if (ability != null && ability.targetRole == currentRole)
            {
                roleAbilities.Add(ability);
            }
        }

        return roleAbilities.ToArray();
    }

    private void LoadStatusAbilitiesFromResources()
    {
        if (loadedAbilities.Count > 0)
        {
            return;
        }

        StatusAbilityData[] abilities = Resources.LoadAll<StatusAbilityData>(statusAbilityResourcePath);
        loadedAbilities = new List<StatusAbilityData>(abilities);
        loadedAbilities.Sort((a, b) => a.sortOrder.CompareTo(b.sortOrder));
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

        if (currentRole == StatusDetailRole.Player)
        {
            BattleStatusData playerBattleStatus = playerStatus != null ? playerStatus.BattleStatus : null;
            return BuildPlayerStatusSummary(playerBattleStatus, conditionalLabel, hiddenLabel);
        }

        BattleStatusData heroineBattleStatus = heroineStatus != null ? heroineStatus.BattleStatus : null;
        return BuildHeroineStatusSummary(heroineBattleStatus, conditionalLabel, hiddenLabel);
    }

    private string BuildPlayerStatusSummary(
        BattleStatusData status,
        string conditionalLabel,
        string hiddenLabel)
    {
        return playerSummaryTitle +
            "\nHP：" + GetCurrentHp(status) + "/" + GetMaxHp(status) +
            "\n攻撃：" + GetAttack(status) +
            "\n防御：" + GetDefense(status) +
            "\n素早さ：" + GetSpeed(status) +
            "\n所持金：" + (playerStatus != null ? playerStatus.Money : 0) +
            "\n衣装確認モード：" + conditionalLabel +
            "\nHidden解放：" + hiddenLabel;
    }

    private string BuildHeroineStatusSummary(
        BattleStatusData status,
        string conditionalLabel,
        string hiddenLabel)
    {
        return heroineSummaryTitle +
            "\nHP：" + GetCurrentHp(status) + "/" + GetMaxHp(status) +
            "\n攻撃：" + GetAttack(status) +
            "\n防御：" + GetDefense(status) +
            "\n素早さ：" + GetSpeed(status) +
            "\n衣装確認モード：" + conditionalLabel +
            "\nHidden解放：" + hiddenLabel;
    }

    private static int GetCurrentHp(BattleStatusData status)
    {
        return status != null ? status.currentHp : 0;
    }

    private static int GetMaxHp(BattleStatusData status)
    {
        return status != null ? status.maxHp : 0;
    }

    private static int GetAttack(BattleStatusData status)
    {
        return status != null ? status.attack : 0;
    }

    private static int GetDefense(BattleStatusData status)
    {
        return status != null ? status.defense : 0;
    }

    private static int GetSpeed(BattleStatusData status)
    {
        return status != null ? status.speed : 0;
    }

    private string BuildAbilityButtonLabel(StatusAbilityData ability)
    {
        return GetAbilityName(ability.abilityKind, ability) + " / " + GetAbilityStateText(ability);
    }

    private string GetAbilityStateText(StatusAbilityData ability)
    {
        return IsAbilityUnlocked(ability.abilityKind, ability) ? unlockedLabel : lockedLabel;
    }

    private string GetAbilityName(StatusAbilityKind abilityKind, StatusAbilityData ability)
    {
        if (ability != null && !string.IsNullOrEmpty(ability.displayName))
        {
            return ability.displayName;
        }

        switch (abilityKind)
        {
            case StatusAbilityKind.HiddenOutfitPrompt:
                return hiddenAbilityName;
            default:
                return conditionalAbilityName;
        }
    }

    private string GetAbilityDescription(StatusAbilityKind abilityKind, StatusAbilityData ability)
    {
        if (ability != null && !string.IsNullOrEmpty(ability.description))
        {
            return ability.description;
        }

        switch (abilityKind)
        {
            case StatusAbilityKind.HiddenOutfitPrompt:
                return hiddenAbilityDescription;
            default:
                return conditionalAbilityDescription;
        }
    }

    private string BuildAbilityAcquireDescription(StatusAbilityKind abilityKind, StatusAbilityData ability)
    {
        string description = GetAbilityDescription(abilityKind, ability);
        string missingCondition = BuildMissingUnlockConditionText(ability);

        if (string.IsNullOrEmpty(missingCondition))
        {
            return description;
        }

        return description + "\n" + missingCondition;
    }

    private bool IsAbilityUnlocked(StatusAbilityKind abilityKind, StatusAbilityData ability)
    {
        OutfitPromptAbilitySet abilities = GetCurrentAbilities();
        if (abilities == null)
        {
            return false;
        }

        switch (GetAbilityEffectType(abilityKind, ability))
        {
            case StatusAbilityEffectType.OutfitPromptConditional:
                return abilities.canUseConditionalMode;
            case StatusAbilityEffectType.OutfitPromptHidden:
                return abilities.canUseHiddenMode;
            case StatusAbilityEffectType.None:
            default:
                return gameManager != null && gameManager.IsStatusAbilityUnlocked(GetStatusAbilitySaveKey(ability));
        }
    }

    private void UnlockSelectedAbility()
    {
        OutfitPromptAbilitySet abilities = GetCurrentAbilities();
        if (abilities == null)
        {
            return;
        }

        if (!CanUnlockAbility(selectedAbilityKind, selectedAbility))
        {
            if (abilityAcquireDescriptionText != null)
            {
                abilityAcquireDescriptionText.text = BuildAbilityAcquireDescription(selectedAbilityKind, selectedAbility);
            }

            return;
        }

        switch (GetAbilityEffectType(selectedAbilityKind, selectedAbility))
        {
            case StatusAbilityEffectType.OutfitPromptConditional:
                abilities.canUseConditionalMode = true;
                break;
            case StatusAbilityEffectType.OutfitPromptHidden:
                abilities.canUseHiddenMode = true;
                break;
            case StatusAbilityEffectType.None:
            default:
                if (gameManager != null)
                {
                    gameManager.UnlockStatusAbility(GetStatusAbilitySaveKey(selectedAbility));
                }
                break;
        }

        Refresh();

        if (abilityAcquireDescriptionText != null)
        {
            abilityAcquireDescriptionText.text = GetAbilityDescription(selectedAbilityKind, selectedAbility) + "\n" + acquiredMessage;
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

    private StatusAbilityEffectType GetAbilityEffectType(StatusAbilityKind abilityKind, StatusAbilityData ability)
    {
        if (ability != null && ability.effectType != StatusAbilityEffectType.UseAbilityKind)
        {
            return ability.effectType;
        }

        switch (abilityKind)
        {
            case StatusAbilityKind.ConditionalOutfitPrompt:
                return StatusAbilityEffectType.OutfitPromptConditional;
            case StatusAbilityKind.HiddenOutfitPrompt:
                return StatusAbilityEffectType.OutfitPromptHidden;
            default:
                return StatusAbilityEffectType.None;
        }
    }

    private bool CanUnlockAbility(StatusAbilityKind abilityKind, StatusAbilityData ability)
    {
        if (IsAbilityUnlocked(abilityKind, ability))
        {
            return false;
        }

        return MeetsUnlockCondition(ability);
    }

    private string GetStatusAbilitySaveKey(StatusAbilityData ability)
    {
        if (ability != null && !string.IsNullOrEmpty(ability.abilityId))
        {
            return ability.targetRole + ":" + ability.abilityId;
        }

        return currentRole + ":" + selectedAbilityKind;
    }

    private bool MeetsUnlockCondition(StatusAbilityData ability)
    {
        if (ability == null)
        {
            return true;
        }

        if (heroineStatus != null && heroineStatus.Affection < ability.requiredAffection)
        {
            return false;
        }

        if (timeManager != null && timeManager.Day < ability.requiredDay)
        {
            return false;
        }

        return true;
    }

    private string BuildMissingUnlockConditionText(StatusAbilityData ability)
    {
        if (ability == null)
        {
            return "";
        }

        string message = "";

        if (heroineStatus != null && heroineStatus.Affection < ability.requiredAffection)
        {
            message += "必要好感度: " + ability.requiredAffection;
        }

        if (timeManager != null && timeManager.Day < ability.requiredDay)
        {
            if (!string.IsNullOrEmpty(message))
            {
                message += "\n";
            }

            message += "必要日数: Day " + ability.requiredDay;
        }

        if (string.IsNullOrEmpty(message))
        {
            return "";
        }

        return "解放条件未達\n" + message;
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
        if (playerStatus == null && gameManager != null)
        {
            playerStatus = gameManager.PlayerStatus;
        }

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
