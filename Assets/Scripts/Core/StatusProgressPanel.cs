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

        if (titleText != null)
        {
            titleText.text = titleLabel;
        }

        if (contentText == null)
        {
            return;
        }

        SkillProgressStats stats = gameManager != null
            ? gameManager.GetSkillProgressStats()
            : new SkillProgressStats();
        switch (currentView)
        {
            case ProgressView.Category:
                contentText.text = BuildCategoryText(stats);
                break;
            case ProgressView.Training:
                contentText.text = BuildTrainingText(stats);
                break;
            case ProgressView.Enemy:
                contentText.text = BuildEnemyText(stats);
                break;
            default:
                contentText.text = BuildSummaryText(stats);
                break;
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

        contentText.ForceMeshUpdate();
        float preferredHeight = Mathf.Max(1f, contentText.preferredHeight);
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
        return "累計訓練回数：" + Math.Max(0, stats.totalTrainingCount) +
            "\n主人公 LP 消費回数：" + Math.Max(0, stats.playerLpConsumedCount) +
            "\n相手 LP 消費回数：" + Math.Max(0, stats.opponentLpConsumedCount) +
            "\nモンスター撃破数：" + Math.Max(0, stats.totalMonsterDefeatCount) +
            "\n主人公スキルポイント：" +
                (gameManager != null ? gameManager.PlayerSkillPoints : 0) +
            "\nヒロインスキルポイント：" +
                (gameManager != null ? gameManager.HeroineSkillPoints : 0);
    }

    private string BuildCategoryText(SkillProgressStats stats)
    {
        List<TrainingCategoryProgressStatEntry> entries = stats.trainingCategoryStats;
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
            AppendProgressBlock(
                builder,
                ResolveCategoryName(entry.trainingCategoryId),
                entry.trainingCount,
                entry.playerLpConsumedCount,
                entry.opponentLpConsumedCount);
        }
        return builder.Length > 0 ? builder.ToString() : emptyLabel;
    }

    private string BuildTrainingText(SkillProgressStats stats)
    {
        List<TrainingProgressStatEntry> entries = stats.trainingStats;
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
            AppendProgressBlock(
                builder,
                ResolveName(names, entry.trainingId),
                entry.trainingCount,
                entry.playerLpConsumedCount,
                entry.opponentLpConsumedCount);
        }
        return builder.Length > 0 ? builder.ToString() : emptyLabel;
    }

    private string BuildEnemyText(SkillProgressStats stats)
    {
        List<EnemyDefeatStatEntry> entries = stats.enemyDefeatStats;
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
            if (builder.Length > 0) builder.Append('\n');
            builder.Append(ResolveName(names, entry.enemyId));
            builder.Append("\n撃破数：");
            builder.Append(Math.Max(0, entry.defeatCount));
        }
        return builder.Length > 0 ? builder.ToString() : emptyLabel;
    }

    private static void AppendProgressBlock(
        StringBuilder builder,
        string name,
        int trainingCount,
        int playerLpConsumedCount,
        int opponentLpConsumedCount)
    {
        if (builder.Length > 0) builder.Append('\n');
        builder.Append(name);
        builder.Append("\n訓練回数：");
        builder.Append(Math.Max(0, trainingCount));
        builder.Append(" / 主人公 LP：");
        builder.Append(Math.Max(0, playerLpConsumedCount));
        builder.Append(" / 相手 LP：");
        builder.Append(Math.Max(0, opponentLpConsumedCount));
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
            default:
                return categoryId;
        }
    }
}
