using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 主人公・ヒロインの現在状態を表示する読み取り専用パネルです。
/// スキルの取得と使用設定は SkillTreePanel が担当します。
/// </summary>
public class StatusDetailPanel : MonoBehaviour
{
    [Header("Managers")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private PlayerStatus playerStatus;
    [SerializeField] private HeroineStatus heroineStatus;

    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button progressButton;
    [SerializeField] private StatusProgressPanel progressPanel;

    [Header("Detail View")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI statusSummaryText;

    [Header("Legacy UI Cleanup")]
    [Tooltip("旧アビリティ一覧のルートです。Sceneから削除するまで非表示にします。")]
    [SerializeField] private Transform abilityListParent;
    [Tooltip("旧アビリティ取得パネルです。Sceneから削除するまで非表示にします。")]
    [SerializeField] private GameObject abilityAcquirePanel;

    [Header("Labels")]
    [SerializeField] private string playerTitle = "プレイヤー詳細ステータス";
    [SerializeField] private string heroineTitle = "ヒロイン詳細ステータス";
    [SerializeField] private string playerSummaryTitle = "プレイヤー能力";
    [SerializeField] private string heroineSummaryTitle = "ヒロイン能力";
    [SerializeField] private string unlockedLabel = "解放済み";
    [SerializeField] private string lockedLabel = "未解放";

    private StatusDetailRole currentRole = StatusDetailRole.Player;
    private bool hasWarnedMissingReferences;

    private GameObject PanelRoot => panelRoot != null ? panelRoot : gameObject;

    private void Awake()
    {
        EnsureUiReferences();
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Close);
        }

        if (progressButton != null)
        {
            progressButton.onClick.AddListener(OpenProgressPanel);
        }

        HideLegacyAbilityUi();
    }

    private void OnEnable()
    {
        EnsureUiReferences();
        HideLegacyAbilityUi();
        Refresh();
    }

    public void Initialize(GameManager manager, HeroineStatus heroine)
    {
        gameManager = manager;
        heroineStatus = heroine;
        playerStatus = manager != null ? manager.PlayerStatus : playerStatus;
        InitializeProgressPanel();
        EnsureUiReferences();
    }

    public void Initialize(GameManager manager, HeroineStatus heroine, TimeManager time)
    {
        Initialize(manager, heroine);
    }

    public void OpenPlayerDetail()
    {
        currentRole = StatusDetailRole.Player;
        PanelRoot.SetActive(true);
        Refresh();
    }

    public void OpenHeroineDetail()
    {
        currentRole = StatusDetailRole.Heroine;
        PanelRoot.SetActive(true);
        Refresh();
    }

    public void Close()
    {
        if (progressPanel != null)
        {
            progressPanel.Close();
        }

        PanelRoot.SetActive(false);
    }

    public void RefreshStatusDisplay()
    {
        Refresh();
        if (progressPanel != null)
        {
            progressPanel.RefreshDisplay();
        }
    }

    public void OpenProgressPanel()
    {
        InitializeProgressPanel();
        if (progressPanel == null)
        {
            Debug.LogWarning("StatusProgressPanel が設定されていないため、実績を表示できません。");
            return;
        }

        progressPanel.Open();
    }

    private void Refresh()
    {
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
            statusSummaryText.text = currentRole == StatusDetailRole.Player
                ? BuildPlayerStatusSummary(playerStatus != null ? playerStatus.BattleStatus : null)
                : BuildHeroineStatusSummary(heroineStatus != null ? heroineStatus.BattleStatus : null);
        }
    }

    private string BuildPlayerStatusSummary(BattleStatusData status)
    {
        bool conditionalUnlocked = gameManager != null &&
            gameManager.CanUseScheduledEventOutfitPromptMode(
                ScheduledEventOutfitPromptMode.Conditional);
        bool hiddenUnlocked = gameManager != null &&
            gameManager.CanUseScheduledEventOutfitPromptMode(
                ScheduledEventOutfitPromptMode.Hidden);
        ScheduledEventOutfitPromptMode selectedMode = gameManager != null &&
            gameManager.PlayerOutfitPromptAbilities != null
                ? gameManager.PlayerOutfitPromptAbilities.selectedMode
                : ScheduledEventOutfitPromptMode.Always;

        return playerSummaryTitle +
            "\nHP：" + GetCurrentHp(status) + "/" + GetMaxHp(status) +
            "\n攻撃：" + GetAttack(status) +
            "\n防御：" + GetDefense(status) +
            "\n素早さ：" + GetSpeed(status) +
            "\n所持金：" + (playerStatus != null ? playerStatus.Money : 0) +
            "\n購入済み：" + BuildPurchasedItemSummary() +
            "\n解放衣装：" + BuildUnlockedOutfitSummary() +
            "\n条件表示：" + (conditionalUnlocked ? unlockedLabel : lockedLabel) +
            "\n非表示：" + (hiddenUnlocked ? unlockedLabel : lockedLabel) +
            "\n現在の衣装確認設定：" +
            (gameManager != null
                ? gameManager.GetScheduledEventOutfitPromptModeLabel(selectedMode)
                : "毎回表示");
    }

    private string BuildHeroineStatusSummary(BattleStatusData status)
    {
        return heroineSummaryTitle +
            "\nHP：" + GetCurrentHp(status) + "/" + GetMaxHp(status) +
            "\n攻撃：" + GetAttack(status) +
            "\n防御：" + GetDefense(status) +
            "\n素早さ：" + GetSpeed(status);
    }

    private string BuildPurchasedItemSummary()
    {
        if (gameManager == null)
        {
            return "なし";
        }

        List<string> itemIds = gameManager.GetPurchasedItemIds();
        if (itemIds == null || itemIds.Count == 0)
        {
            return "なし";
        }

        itemIds.Sort();
        return string.Join(", ", itemIds.ToArray());
    }

    private string BuildUnlockedOutfitSummary()
    {
        if (gameManager == null)
        {
            return "なし";
        }

        List<string> outfitIds = gameManager.GetUnlockedOutfitIds();
        if (outfitIds == null || outfitIds.Count == 0)
        {
            return "なし";
        }

        outfitIds.Sort();
        return string.Join(", ", outfitIds.ToArray());
    }

    private static int GetCurrentHp(BattleStatusData status) => status != null ? status.currentHp : 0;
    private static int GetMaxHp(BattleStatusData status) => status != null ? status.maxHp : 0;
    private static int GetAttack(BattleStatusData status) => status != null ? status.attack : 0;
    private static int GetDefense(BattleStatusData status) => status != null ? status.defense : 0;
    private static int GetSpeed(BattleStatusData status) => status != null ? status.speed : 0;

    private void HideLegacyAbilityUi()
    {
        if (abilityListParent != null)
        {
            abilityListParent.gameObject.SetActive(false);
        }

        if (abilityAcquirePanel != null)
        {
            abilityAcquirePanel.SetActive(false);
        }
    }

    private void InitializeProgressPanel()
    {
        if (progressPanel == null)
        {
            progressPanel = GetComponentInChildren<StatusProgressPanel>(true);
        }

        if (progressPanel != null)
        {
            progressPanel.Initialize(gameManager);
        }
    }

    private void EnsureUiReferences()
    {
        if (playerStatus == null && gameManager != null)
        {
            playerStatus = gameManager.PlayerStatus;
        }

        if ((titleText != null && statusSummaryText != null && closeButton != null) ||
            hasWarnedMissingReferences)
        {
            return;
        }

        Debug.LogWarning(
            "StatusDetailPanel の UI 参照が不足しています。Hierarchy 上の参照を確認してください。");
        hasWarnedMissingReferences = true;
    }
}
