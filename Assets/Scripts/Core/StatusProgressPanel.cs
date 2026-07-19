using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatusProgressPanel : MonoBehaviour
{
    private enum ProgressView
    {
        Summary,
        Category,
        Training,
        Enemy
    }

    [Header("Manager")]
    [SerializeField] private GameManager gameManager;

    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button closeButton;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI contentText;

    [Header("View Buttons")]
    [SerializeField] private Button summaryButton;
    [SerializeField] private Button categoryButton;
    [SerializeField] private Button trainingButton;
    [SerializeField] private Button enemyButton;

    [Header("Labels")]
    [SerializeField] private string titleLabel = "訓練・戦闘実績";
    [SerializeField] private string emptyLabel = "記録はまだありません。";

    private ProgressView currentView = ProgressView.Summary;
    private bool buttonsHooked;

    private GameObject PanelRoot
    {
        get { return panelRoot != null ? panelRoot : gameObject; }
    }

    private void Awake()
    {
        HookButtons();
    }

    private void OnEnable()
    {
        HookButtons();
        RefreshDisplay();
    }

    public void Initialize(GameManager manager)
    {
        gameManager = manager;
        HookButtons();
    }

    public void Open()
    {
        currentView = ProgressView.Summary;
        PanelRoot.SetActive(true);
        RefreshDisplay();
    }

    public void Close()
    {
        PanelRoot.SetActive(false);
    }

    public void RefreshDisplay()
    {
        if (!PanelRoot.activeSelf)
        {
            return;
        }

        if (contentText == null)
        {
            return;
        }

        SkillProgressStats stats = gameManager != null
            ? gameManager.GetSkillProgressStats()
            : new SkillProgressStats();
        int displayedCount;
        string viewLabel;
        switch (currentView)
        {
            case ProgressView.Category:
                viewLabel = "カテゴリー別";
                contentText.text = BuildCategoryText(stats, out displayedCount);
                break;
            case ProgressView.Training:
                viewLabel = "訓練別";
                contentText.text = BuildTrainingText(stats, out displayedCount);
                break;
            case ProgressView.Enemy:
                viewLabel = "敵別";
                contentText.text = BuildEnemyText(stats, out displayedCount);
                break;
            default:
                viewLabel = "全体";
                contentText.text = BuildSummaryText(stats);
                displayedCount = 1;
                break;
        }

        if (titleText != null)
        {
            titleText.text = titleLabel + " / " + viewLabel +
                (currentView == ProgressView.Summary ? "" : "（" + displayedCount + "件）");
        }

        RefreshContentLayout();
        RefreshButtonStates();
    }

    private void RefreshContentLayout()
    {
        RectTransform textRect = contentText != null ? contentText.rectTransform : null;
        RectTransform contentRect = textRect != null ? textRect.parent as RectTransform : null;
        if (textRect == null || contentRect == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();
        contentText.ForceMeshUpdate();
        float preferredHeight = Mathf.Max(1f, Mathf.Ceil(contentText.preferredHeight));
        RectTransform viewportRect = contentRect.parent as RectTransform;
        float viewportHeight = viewportRect != null ? viewportRect.rect.height : 0f;
        float contentHeight = Mathf.Max(preferredHeight, viewportHeight);

        contentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight);
        textRect.anchorMin = new Vector2(0f, 1f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.pivot = new Vector2(0f, 1f);
        textRect.anchoredPosition = Vector2.zero;
        textRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, preferredHeight);

        ScrollRect scrollRect = contentRect.GetComponentInParent<ScrollRect>();
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 1f;
        }
    }

    private void HookButtons()
    {
        if (buttonsHooked)
        {
            return;
        }

        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (summaryButton != null) summaryButton.onClick.AddListener(ShowSummary);
        if (categoryButton != null) categoryButton.onClick.AddListener(ShowCategories);
        if (trainingButton != null) trainingButton.onClick.AddListener(ShowTrainings);
        if (enemyButton != null) enemyButton.onClick.AddListener(ShowEnemies);
        buttonsHooked = true;
    }

    private void ShowSummary()
    {
        currentView = ProgressView.Summary;
        RefreshDisplay();
    }

    private void ShowCategories()
    {
        currentView = ProgressView.Category;
        RefreshDisplay();
    }

    private void ShowTrainings()
    {
        currentView = ProgressView.Training;
        RefreshDisplay();
    }

    private void ShowEnemies()
    {
        currentView = ProgressView.Enemy;
        RefreshDisplay();
    }

    private void RefreshButtonStates()
    {
        if (summaryButton != null) summaryButton.interactable = currentView != ProgressView.Summary;
        if (categoryButton != null) categoryButton.interactable = currentView != ProgressView.Category;
        if (trainingButton != null) trainingButton.interactable = currentView != ProgressView.Training;
        if (enemyButton != null) enemyButton.interactable = currentView != ProgressView.Enemy;
    }

    private string BuildSummaryText(SkillProgressStats stats)
    {
        return "【訓練実績】" +
            "\n累計訓練回数：" + Math.Max(0, stats.totalTrainingCount) +
                BuildTotalConditionMarker(SkillTreeConditionType.TrainingCount) +
            "\n主人公 LP 消費回数：" + Math.Max(0, stats.playerLpConsumedCount) +
                BuildTotalConditionMarker(SkillTreeConditionType.PlayerLpConsumedCount) +
            "\n相手 LP 消費回数：" + Math.Max(0, stats.opponentLpConsumedCount) +
                BuildTotalConditionMarker(SkillTreeConditionType.OpponentLpConsumedCount) +
            "\n\n【戦闘実績】" +
            "\nモンスター撃破数：" + Math.Max(0, stats.totalMonsterDefeatCount) +
                BuildTotalConditionMarker(SkillTreeConditionType.MonsterDefeatCount) +
            "\n\n【スキルポイント】" +
            "\n主人公：" + (gameManager != null ? gameManager.PlayerSkillPoints : 0) +
            "\nヒロイン：" + (gameManager != null ? gameManager.HeroineSkillPoints : 0);
    }

    private string BuildCategoryText(SkillProgressStats stats, out int displayedCount)
    {
        displayedCount = 0;
        List<TrainingCategoryProgressStatEntry> entries = stats.trainingCategoryStats != null
            ? new List<TrainingCategoryProgressStatEntry>(stats.trainingCategoryStats)
            : new List<TrainingCategoryProgressStatEntry>();
        if (entries == null || entries.Count == 0)
        {
            return emptyLabel;
        }

        entries.Sort((a, b) => string.Compare(
            ResolveCategoryName(a != null ? a.trainingCategoryId : string.Empty),
            ResolveCategoryName(b != null ? b.trainingCategoryId : string.Empty),
            StringComparison.Ordinal));
        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < entries.Count; i++)
        {
            TrainingCategoryProgressStatEntry entry = entries[i];
            if (entry == null || string.IsNullOrEmpty(entry.trainingCategoryId)) continue;
            if (!HasTrainingProgress(
                entry.trainingCount,
                entry.playerLpConsumedCount,
                entry.opponentLpConsumedCount)) continue;
            AppendProgressBlock(
                builder,
                ResolveCategoryName(entry.trainingCategoryId),
                entry.trainingCount,
                entry.playerLpConsumedCount,
                entry.opponentLpConsumedCount,
                IsUnlockConditionTarget(
                    SkillTreeProgressScope.TrainingCategory,
                    entry.trainingCategoryId));
            displayedCount++;
        }
        return builder.Length > 0 ? builder.ToString() : emptyLabel;
    }

    private string BuildTrainingText(SkillProgressStats stats, out int displayedCount)
    {
        displayedCount = 0;
        List<TrainingProgressStatEntry> entries = stats.trainingStats != null
            ? new List<TrainingProgressStatEntry>(stats.trainingStats)
            : new List<TrainingProgressStatEntry>();
        if (entries == null || entries.Count == 0)
        {
            return emptyLabel;
        }

        Dictionary<string, string> names = LoadTrainingNames();
        entries.Sort((a, b) => string.Compare(
            ResolveName(names, a != null ? a.trainingId : string.Empty),
            ResolveName(names, b != null ? b.trainingId : string.Empty),
            StringComparison.Ordinal));
        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < entries.Count; i++)
        {
            TrainingProgressStatEntry entry = entries[i];
            if (entry == null || string.IsNullOrEmpty(entry.trainingId)) continue;
            int proficiency = gameManager != null
                ? gameManager.GetTrainingProficiency(entry.trainingId)
                : 0;
            if (!HasTrainingProgress(
                entry.trainingCount,
                entry.playerLpConsumedCount,
                entry.opponentLpConsumedCount) && proficiency <= 0) continue;
            AppendProgressBlock(
                builder,
                ResolveName(names, entry.trainingId),
                entry.trainingCount,
                entry.playerLpConsumedCount,
                entry.opponentLpConsumedCount,
                IsUnlockConditionTarget(
                    SkillTreeProgressScope.Training,
                    entry.trainingId),
                proficiency);
            displayedCount++;
        }
        return builder.Length > 0 ? builder.ToString() : emptyLabel;
    }

    private string BuildEnemyText(SkillProgressStats stats, out int displayedCount)
    {
        displayedCount = 0;
        List<EnemyDefeatStatEntry> entries = stats.enemyDefeatStats != null
            ? new List<EnemyDefeatStatEntry>(stats.enemyDefeatStats)
            : new List<EnemyDefeatStatEntry>();
        if (entries == null || entries.Count == 0)
        {
            return emptyLabel;
        }

        Dictionary<string, string> names = LoadEnemyNames();
        entries.Sort((a, b) => string.Compare(
            ResolveName(names, a != null ? a.enemyId : string.Empty),
            ResolveName(names, b != null ? b.enemyId : string.Empty),
            StringComparison.Ordinal));
        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < entries.Count; i++)
        {
            EnemyDefeatStatEntry entry = entries[i];
            if (entry == null || string.IsNullOrEmpty(entry.enemyId)) continue;
            if (entry.defeatCount <= 0) continue;
            if (builder.Length > 0) builder.Append('\n');
            builder.Append("【");
            builder.Append(ResolveName(names, entry.enemyId));
            builder.Append("】");
            builder.Append("\n撃破数：");
            builder.Append(Math.Max(0, entry.defeatCount));
            if (IsUnlockConditionTarget(SkillTreeProgressScope.Enemy, entry.enemyId))
            {
                builder.Append("\nスキル解放条件：あり");
            }
            displayedCount++;
        }
        return builder.Length > 0 ? builder.ToString() : emptyLabel;
    }

    private static void AppendProgressBlock(
        StringBuilder builder,
        string name,
        int trainingCount,
        int playerLpConsumedCount,
        int opponentLpConsumedCount,
        bool isUnlockConditionTarget,
        int proficiency = -1)
    {
        if (builder.Length > 0) builder.Append('\n');
        builder.Append("【");
        builder.Append(name);
        builder.Append("】");
        if (proficiency >= 0)
        {
            builder.Append("\n熟練度：");
            builder.Append(Math.Max(0, proficiency));
        }
        builder.Append("\n訓練回数：");
        builder.Append(Math.Max(0, trainingCount));
        builder.Append("\n主人公 LP 消費回数：");
        builder.Append(Math.Max(0, playerLpConsumedCount));
        builder.Append("\n相手 LP 消費回数：");
        builder.Append(Math.Max(0, opponentLpConsumedCount));
        if (isUnlockConditionTarget)
        {
            builder.Append("\nスキル解放条件：あり");
        }
    }

    private static bool HasTrainingProgress(
        int trainingCount,
        int playerLpConsumedCount,
        int opponentLpConsumedCount)
    {
        return trainingCount > 0 ||
            playerLpConsumedCount > 0 ||
            opponentLpConsumedCount > 0;
    }

    private string BuildTotalConditionMarker(SkillTreeConditionType conditionType)
    {
        return HasUnlockCondition(SkillTreeProgressScope.Total, conditionType, string.Empty)
            ? "（スキル解放条件）"
            : string.Empty;
    }

    private bool IsUnlockConditionTarget(SkillTreeProgressScope scope, string targetId)
    {
        return HasUnlockCondition(scope, null, targetId);
    }

    private bool HasUnlockCondition(
        SkillTreeProgressScope scope,
        SkillTreeConditionType? conditionType,
        string targetId)
    {
        if (gameManager == null)
        {
            return false;
        }

        List<SkillTreeNodeData> nodes = gameManager.GetSkillTreeNodes();
        for (int nodeIndex = 0; nodeIndex < nodes.Count; nodeIndex++)
        {
            SkillTreeNodeData node = nodes[nodeIndex];
            if (node == null ||
                (node.owner == SkillTreeOwner.Heroine &&
                    !gameManager.IsSkillTreeNodeForCurrentHeroine(node)) ||
                node.unlockConditions == null) continue;
            for (int conditionIndex = 0;
                conditionIndex < node.unlockConditions.Count;
                conditionIndex++)
            {
                SkillTreeUnlockCondition condition = node.unlockConditions[conditionIndex];
                if (condition == null || condition.scope != scope) continue;
                if (conditionType.HasValue && condition.conditionType != conditionType.Value) continue;
                if (scope != SkillTreeProgressScope.Total &&
                    !string.Equals(condition.targetId, targetId, StringComparison.Ordinal)) continue;
                return true;
            }
        }

        return false;
    }

    private static Dictionary<string, string> LoadTrainingNames()
    {
        Dictionary<string, string> names = new Dictionary<string, string>();
        TrainingData[] trainings = Resources.LoadAll<TrainingData>("Training");
        for (int i = 0; i < trainings.Length; i++)
        {
            TrainingData training = trainings[i];
            if (training != null && !string.IsNullOrEmpty(training.trainingId))
            {
                names[training.trainingId] = training.GetDisplayName();
            }
        }
        return names;
    }

    private static Dictionary<string, string> LoadEnemyNames()
    {
        Dictionary<string, string> names = new Dictionary<string, string>();
        EnemyData[] enemies = Resources.LoadAll<EnemyData>("Enemies");
        for (int i = 0; i < enemies.Length; i++)
        {
            EnemyData enemy = enemies[i];
            if (enemy != null && !string.IsNullOrEmpty(enemy.enemyId))
            {
                names[enemy.enemyId] = enemy.GetDisplayName();
            }
        }
        return names;
    }

    private static string ResolveName(Dictionary<string, string> names, string id)
    {
        return names != null && !string.IsNullOrEmpty(id) && names.TryGetValue(id, out string name)
            ? name
            : id;
    }

    private static string ResolveCategoryName(string categoryId)
    {
        switch (categoryId)
        {
            case "Fundamentals":
                return "基礎";
            case "Combat":
                return "実戦";
            case "Endurance":
                return "持久";
            case "Coordination":
                return "連携";
            case "General":
                return "一般";
            default:
                return categoryId;
        }
    }
}
