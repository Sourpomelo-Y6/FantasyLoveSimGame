using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillTreePanel : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button closeButton;

    [Header("Owner")]
    [SerializeField] private Button playerButton;
    [SerializeField] private Button heroineButton;
    [SerializeField] private TextMeshProUGUI skillPointText;

    [Header("Nodes")]
    [SerializeField] private Transform nodeListParent;
    [SerializeField] private Button nodeButtonPrefab;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Button acquireButton;

    private readonly List<GameObject> spawnedNodeButtons = new List<GameObject>();
    private GameManager gameManager;
    private SkillTreeOwner currentOwner = SkillTreeOwner.Player;
    private SkillTreeNodeData selectedNode;
    private bool buttonsHooked;

    private GameObject PanelRoot => panelRoot != null ? panelRoot : gameObject;

    private void Awake()
    {
        ResolveUiReferences();
        HookButtons();
        HideNodeButtonTemplate();
    }

    public void Initialize(GameManager manager)
    {
        gameManager = manager;
        ResolveUiReferences();
        HookButtons();
        HideNodeButtonTemplate();
    }

    public void Open(GameManager manager)
    {
        Initialize(manager);
        currentOwner = SkillTreeOwner.Player;
        selectedNode = null;
        PanelRoot.SetActive(true);
        Refresh();
    }

    public void Close()
    {
        PanelRoot.SetActive(false);
    }

    private void ShowPlayerNodes()
    {
        ChangeOwner(SkillTreeOwner.Player);
    }

    private void ShowHeroineNodes()
    {
        ChangeOwner(SkillTreeOwner.Heroine);
    }

    private void ChangeOwner(SkillTreeOwner owner)
    {
        if (currentOwner == owner)
        {
            return;
        }

        currentOwner = owner;
        selectedNode = null;
        Refresh();
    }

    private void Refresh()
    {
        if (gameManager == null)
        {
            return;
        }

        int points = currentOwner == SkillTreeOwner.Player
            ? gameManager.PlayerSkillPoints
            : gameManager.HeroineSkillPoints;
        if (skillPointText != null)
        {
            skillPointText.text = "スキルポイント：" + points;
        }

        if (playerButton != null) playerButton.interactable = currentOwner != SkillTreeOwner.Player;
        if (heroineButton != null) heroineButton.interactable = currentOwner != SkillTreeOwner.Heroine;

        RefreshNodeList();
        RefreshSelectedNode();
    }

    private void RefreshNodeList()
    {
        ClearSpawnedNodes();
        HideNodeButtonTemplate();
        if (nodeListParent == null || nodeButtonPrefab == null)
        {
            return;
        }

        List<SkillTreeNodeData> nodes = gameManager.GetSkillTreeNodes();
        for (int i = 0; i < nodes.Count; i++)
        {
            SkillTreeNodeData node = nodes[i];
            if (node == null || node.owner != currentOwner)
            {
                continue;
            }

            CreateNodeButton(node);
        }
    }

    private void CreateNodeButton(SkillTreeNodeData node)
    {
        Button button = Instantiate(nodeButtonPrefab, nodeListParent);
        button.gameObject.SetActive(true);
        spawnedNodeButtons.Add(button.gameObject);

        SkillTreeNodeEvaluation evaluation = gameManager.EvaluateSkillTreeNode(node);
        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null)
        {
            label.text = node.GetDisplayName() + " / 必要SP " +
                Mathf.Max(0, node.skillPointCost) + " / " + GetStateLabel(evaluation.state);
        }

        button.onClick.AddListener(() => SelectNode(node));
    }

    private void SelectNode(SkillTreeNodeData node)
    {
        selectedNode = node;
        RefreshSelectedNode();
    }

    private void RefreshSelectedNode()
    {
        if (selectedNode == null)
        {
            if (descriptionText != null)
            {
                descriptionText.text = HasNodesForCurrentOwner()
                    ? "習得するノードを選択してください。"
                    : "このキャラクターのスキルツリーノードはまだありません。";
            }
            if (acquireButton != null) acquireButton.interactable = false;
            return;
        }

        SkillTreeNodeEvaluation evaluation = gameManager.EvaluateSkillTreeNode(selectedNode);
        if (descriptionText != null)
        {
            descriptionText.text = BuildDescription(evaluation);
        }
        if (acquireButton != null)
        {
            acquireButton.interactable = evaluation.state == SkillTreeNodeState.Available;
        }
    }

    private bool HasNodesForCurrentOwner()
    {
        List<SkillTreeNodeData> nodes = gameManager.GetSkillTreeNodes();
        return nodes.Exists(node => node != null && node.owner == currentOwner);
    }

    private string BuildDescription(SkillTreeNodeEvaluation evaluation)
    {
        SkillTreeNodeData node = evaluation.node;
        StringBuilder builder = new StringBuilder();
        builder.AppendLine(node.GetDisplayName());
        builder.AppendLine("状態：" + GetStateLabel(evaluation.state));
        builder.AppendLine("必要スキルポイント：" + evaluation.requiredSkillPoints);

        if (node.skill != null && !string.IsNullOrEmpty(node.skill.description))
        {
            builder.AppendLine();
            builder.AppendLine(node.skill.description);
        }

        if (node.prerequisiteNodes != null && node.prerequisiteNodes.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("前提ノード：");
            for (int i = 0; i < node.prerequisiteNodes.Count; i++)
            {
                SkillTreeNodeData prerequisite = node.prerequisiteNodes[i];
                if (prerequisite == null) continue;
                bool acquired = gameManager.IsSkillTreeNodeAcquired(prerequisite.nodeId, prerequisite.owner);
                builder.AppendLine("・" + prerequisite.GetDisplayName() + (acquired ? "（習得済み）" : "（未習得）"));
            }
        }

        if (evaluation.conditions.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("解放条件：");
            for (int i = 0; i < evaluation.conditions.Count; i++)
            {
                SkillTreeConditionProgress progress = evaluation.conditions[i];
                builder.AppendLine("・" + GetConditionLabel(progress.condition) + "：" +
                    progress.currentValue + " / " + progress.requiredValue);
            }
        }

        return builder.ToString().TrimEnd();
    }

    private void AcquireSelectedNode()
    {
        if (selectedNode == null || !gameManager.TryAcquireSkillTreeNode(selectedNode))
        {
            return;
        }

        Refresh();
    }

    private static string GetStateLabel(SkillTreeNodeState state)
    {
        switch (state)
        {
            case SkillTreeNodeState.Available: return "習得可能";
            case SkillTreeNodeState.InsufficientPoints: return "SP不足";
            case SkillTreeNodeState.Acquired: return "習得済み";
            default: return "未解放";
        }
    }

    private static string GetConditionLabel(SkillTreeUnlockCondition condition)
    {
        if (condition == null) return "不明な条件";
        string target = string.IsNullOrEmpty(condition.targetId) ? "" : "（" + condition.targetId + "）";
        switch (condition.conditionType)
        {
            case SkillTreeConditionType.TrainingProficiency: return "訓練熟練度" + target;
            case SkillTreeConditionType.TrainingCount: return "訓練回数" + target;
            case SkillTreeConditionType.PlayerLpConsumedCount: return "主人公LP減少回数" + target;
            case SkillTreeConditionType.OpponentLpConsumedCount: return "相手LP減少回数" + target;
            case SkillTreeConditionType.MonsterDefeatCount: return "モンスター撃破数" + target;
            case SkillTreeConditionType.Affection: return "好感度";
            case SkillTreeConditionType.Day: return "経過日数";
            default: return condition.conditionType.ToString();
        }
    }

    private void ClearSpawnedNodes()
    {
        for (int i = 0; i < spawnedNodeButtons.Count; i++)
        {
            if (spawnedNodeButtons[i] != null) Destroy(spawnedNodeButtons[i]);
        }
        spawnedNodeButtons.Clear();
    }

    private void HideNodeButtonTemplate()
    {
        if (nodeButtonPrefab != null && nodeListParent != null &&
            nodeButtonPrefab.transform.IsChildOf(nodeListParent))
        {
            nodeButtonPrefab.gameObject.SetActive(false);
        }
    }

    private void HookButtons()
    {
        if (buttonsHooked) return;
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (playerButton != null) playerButton.onClick.AddListener(ShowPlayerNodes);
        if (heroineButton != null) heroineButton.onClick.AddListener(ShowHeroineNodes);
        if (acquireButton != null) acquireButton.onClick.AddListener(AcquireSelectedNode);
        buttonsHooked = true;
    }

    private void ResolveUiReferences()
    {
        if (panelRoot == null) panelRoot = gameObject;
        if (playerButton == null) playerButton = FindComponent<Button>("PlayerButton");
        if (heroineButton == null) heroineButton = FindComponent<Button>("HeroineButton");
        if (skillPointText == null) skillPointText = FindComponent<TextMeshProUGUI>("SkillPointText");
        if (descriptionText == null) descriptionText = FindComponent<TextMeshProUGUI>("DescriptionText");
        if (acquireButton == null) acquireButton = FindComponent<Button>("AcquireButton");
        if (closeButton == null) closeButton = FindComponent<Button>("CloseButton");

        Transform scrollView = FindChild(transform, "NodeListScrollView");
        ScrollRect scrollRect = scrollView != null ? scrollView.GetComponent<ScrollRect>() : null;
        if (nodeListParent == null && scrollRect != null) nodeListParent = scrollRect.content;
        if (nodeButtonPrefab == null && nodeListParent != null)
        {
            nodeButtonPrefab = nodeListParent.GetComponentInChildren<Button>(true);
        }
    }

    private T FindComponent<T>(string objectName) where T : Component
    {
        Transform child = FindChild(transform, objectName);
        return child != null ? child.GetComponent<T>() : null;
    }

    private static Transform FindChild(Transform parent, string objectName)
    {
        if (parent == null) return null;
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == objectName) return child;
            Transform result = FindChild(child, objectName);
            if (result != null) return result;
        }
        return null;
    }
}
