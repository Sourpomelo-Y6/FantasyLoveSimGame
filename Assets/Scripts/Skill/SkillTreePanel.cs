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

    [Header("Tree Layout")]
    [SerializeField] private bool useTreeLayout = true;
    [SerializeField] private Vector2 treeNodeSize = new Vector2(240f, 72f);
    [SerializeField] private Vector2 treePadding = new Vector2(60f, 60f);
    [SerializeField] private float connectorThickness = 6f;
    [SerializeField] private Color lockedConnectorColor = new Color32(95, 95, 95, 255);

    [Header("Node Colors")]
    [SerializeField] private Color lockedColor = new Color32(120, 120, 120, 255);
    [SerializeField] private Color insufficientPointsColor = new Color32(210, 170, 55, 255);
    [SerializeField] private Color availableColor = new Color32(80, 180, 100, 255);
    [SerializeField] private Color acquiredColor = new Color32(75, 135, 210, 255);
    [SerializeField] private Color selectedOutlineColor = Color.white;

    private readonly List<GameObject> spawnedNodeButtons = new List<GameObject>();
    private readonly List<GameObject> spawnedConnectors = new List<GameObject>();
    private readonly Dictionary<Button, SkillTreeNodeData> nodeButtonNodes =
        new Dictionary<Button, SkillTreeNodeData>();
    private readonly Dictionary<SkillTreeNodeData, RectTransform> nodeRects =
        new Dictionary<SkillTreeNodeData, RectTransform>();
    private GameManager gameManager;
    private SkillTreeOwner currentOwner = SkillTreeOwner.Player;
    private SkillTreeNodeData selectedNode;
    private string feedbackMessage = string.Empty;
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
        feedbackMessage = string.Empty;
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
        feedbackMessage = string.Empty;
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
            string ownerName = currentOwner == SkillTreeOwner.Player
                ? "主人公"
                : ResolveCurrentHeroineName();
            skillPointText.text = ownerName + " / スキルポイント：" + points;
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

        List<SkillTreeNodeData> visibleNodes = new List<SkillTreeNodeData>();
        List<SkillTreeNodeData> nodes = gameManager.GetSkillTreeNodes();
        for (int i = 0; i < nodes.Count; i++)
        {
            SkillTreeNodeData node = nodes[i];
            if (node == null ||
                node.owner != currentOwner ||
                (currentOwner == SkillTreeOwner.Heroine &&
                    !gameManager.IsSkillTreeNodeForCurrentHeroine(node)))
            {
                continue;
            }

            visibleNodes.Add(node);
        }

        if (useTreeLayout)
        {
            PrepareTreeLayout(visibleNodes);
        }

        for (int i = 0; i < visibleNodes.Count; i++)
        {
            Button button = CreateNodeButton(visibleNodes[i]);
            if (useTreeLayout)
            {
                PositionTreeNode(button, visibleNodes[i], visibleNodes);
            }
        }

        if (useTreeLayout)
        {
            CreateTreeConnectors(visibleNodes);
        }
    }

    private Button CreateNodeButton(SkillTreeNodeData node)
    {
        Button button = Instantiate(nodeButtonPrefab, nodeListParent);
        button.gameObject.SetActive(true);
        spawnedNodeButtons.Add(button.gameObject);
        nodeButtonNodes[button] = node;

        SkillTreeNodeEvaluation evaluation = gameManager.EvaluateSkillTreeNode(node);
        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null)
        {
            string activationLabel = evaluation.state == SkillTreeNodeState.Acquired &&
                IsNodeLoadoutConfigurable(node) &&
                IsNodeInLoadout(node)
                    ? GetActiveNodeLabel(node)
                    : "";
            label.text = node.GetDisplayName() + " / 必要SP " +
                Mathf.Max(0, node.skillPointCost) + " / " + GetStateLabel(evaluation.state) +
                activationLabel;
        }

        button.onClick.AddListener(() => SelectNode(node));
        ApplyNodeButtonVisual(button, evaluation, node == selectedNode);
        return button;
    }

    private void PrepareTreeLayout(List<SkillTreeNodeData> nodes)
    {
        VerticalLayoutGroup layoutGroup = nodeListParent.GetComponent<VerticalLayoutGroup>();
        if (layoutGroup != null) layoutGroup.enabled = false;

        ContentSizeFitter sizeFitter = nodeListParent.GetComponent<ContentSizeFitter>();
        if (sizeFitter != null) sizeFitter.enabled = false;

        RectTransform content = nodeListParent as RectTransform;
        if (content == null) return;

        float minX;
        float maxX;
        float minY;
        float maxY;
        GetTreeBounds(nodes, out minX, out maxX, out minY, out maxY);

        RectTransform viewport = content.parent as RectTransform;
        float minimumWidth = viewport != null ? viewport.rect.width : 0f;
        float minimumHeight = viewport != null ? viewport.rect.height : 0f;
        float width = Mathf.Max(
            minimumWidth,
            maxX - minX + treeNodeSize.x + treePadding.x * 2f);
        float height = Mathf.Max(
            minimumHeight,
            maxY - minY + treeNodeSize.y + treePadding.y * 2f);

        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(0f, 1f);
        content.pivot = new Vector2(0f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(width, height);

        ScrollRect scrollRect = content.GetComponentInParent<ScrollRect>();
        if (scrollRect != null)
        {
            scrollRect.horizontal = width > minimumWidth + 0.5f;
            scrollRect.vertical = height > minimumHeight + 0.5f;
            scrollRect.horizontalNormalizedPosition = 0f;
            scrollRect.verticalNormalizedPosition = 1f;
        }
    }

    private void PositionTreeNode(
        Button button,
        SkillTreeNodeData node,
        List<SkillTreeNodeData> visibleNodes)
    {
        RectTransform rect = button != null ? button.transform as RectTransform : null;
        if (rect == null) return;

        float minX;
        float maxX;
        float minY;
        float maxY;
        GetTreeBounds(visibleNodes, out minX, out maxX, out minY, out maxY);

        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = treeNodeSize;
        rect.anchoredPosition = new Vector2(
            node.treePosition.x - minX + treePadding.x + treeNodeSize.x * 0.5f,
            -(maxY - node.treePosition.y + treePadding.y + treeNodeSize.y * 0.5f));
        nodeRects[node] = rect;
    }

    private static void GetTreeBounds(
        List<SkillTreeNodeData> nodes,
        out float minX,
        out float maxX,
        out float minY,
        out float maxY)
    {
        minX = maxX = minY = maxY = 0f;
        if (nodes == null || nodes.Count == 0) return;

        minX = maxX = nodes[0].treePosition.x;
        minY = maxY = nodes[0].treePosition.y;
        for (int i = 1; i < nodes.Count; i++)
        {
            Vector2 position = nodes[i].treePosition;
            minX = Mathf.Min(minX, position.x);
            maxX = Mathf.Max(maxX, position.x);
            minY = Mathf.Min(minY, position.y);
            maxY = Mathf.Max(maxY, position.y);
        }
    }

    private void CreateTreeConnectors(List<SkillTreeNodeData> visibleNodes)
    {
        HashSet<SkillTreeNodeData> visibleNodeSet =
            new HashSet<SkillTreeNodeData>(visibleNodes);
        for (int i = 0; i < visibleNodes.Count; i++)
        {
            SkillTreeNodeData node = visibleNodes[i];
            if (node.prerequisiteNodes == null) continue;

            for (int j = 0; j < node.prerequisiteNodes.Count; j++)
            {
                SkillTreeNodeData prerequisite = node.prerequisiteNodes[j];
                if (prerequisite == null || !visibleNodeSet.Contains(prerequisite)) continue;
                CreateTreeConnector(prerequisite, node);
            }
        }
    }

    private void CreateTreeConnector(
        SkillTreeNodeData prerequisite,
        SkillTreeNodeData target)
    {
        RectTransform fromRect;
        RectTransform toRect;
        if (!nodeRects.TryGetValue(prerequisite, out fromRect) ||
            !nodeRects.TryGetValue(target, out toRect))
        {
            return;
        }

        Vector2 from = fromRect.anchoredPosition;
        Vector2 to = toRect.anchoredPosition;
        Vector2 direction = to - from;
        float distance = direction.magnitude;
        if (distance <= 0.01f) return;

        GameObject connector = new GameObject(
            "Connection_" + prerequisite.nodeId + "_" + target.nodeId,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        connector.transform.SetParent(nodeListParent, false);
        connector.transform.SetAsFirstSibling();
        spawnedConnectors.Add(connector);

        RectTransform connectorRect = connector.GetComponent<RectTransform>();
        connectorRect.anchorMin = new Vector2(0f, 1f);
        connectorRect.anchorMax = new Vector2(0f, 1f);
        connectorRect.pivot = new Vector2(0.5f, 0.5f);
        connectorRect.anchoredPosition = (from + to) * 0.5f;
        connectorRect.sizeDelta = new Vector2(distance, Mathf.Max(1f, connectorThickness));
        connectorRect.localRotation = Quaternion.Euler(
            0f,
            0f,
            Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);

        SkillTreeNodeEvaluation evaluation = gameManager.EvaluateSkillTreeNode(target);
        Image image = connector.GetComponent<Image>();
        image.raycastTarget = false;
        image.color = evaluation.state == SkillTreeNodeState.Locked
            ? lockedConnectorColor
            : GetNodeStateColor(evaluation.state);
    }

    private void SelectNode(SkillTreeNodeData node)
    {
        selectedNode = node;
        feedbackMessage = string.Empty;
        RefreshNodeButtonVisuals();
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
            if (acquireButton != null)
            {
                acquireButton.interactable = false;
                SetAcquireButtonLabel("取得");
            }
            return;
        }

        SkillTreeNodeEvaluation evaluation = gameManager.EvaluateSkillTreeNode(selectedNode);
        if (descriptionText != null)
        {
            string description = BuildDescription(evaluation);
            descriptionText.text = string.IsNullOrEmpty(feedbackMessage)
                ? description
                : feedbackMessage + "\n\n" + description;
        }
        if (acquireButton != null)
        {
            bool canToggleEquipment = evaluation.state == SkillTreeNodeState.Acquired &&
                IsNodeLoadoutConfigurable(selectedNode);
            acquireButton.interactable =
                evaluation.state == SkillTreeNodeState.Available || canToggleEquipment;
            if (canToggleEquipment)
            {
                SetAcquireButtonLabel(GetToggleButtonLabel(
                    selectedNode,
                    IsNodeInLoadout(selectedNode)));
            }
            else
            {
                SetAcquireButtonLabel(
                    evaluation.state == SkillTreeNodeState.Acquired ? "取得済み" : "取得");
            }
        }
    }

    private bool HasNodesForCurrentOwner()
    {
        List<SkillTreeNodeData> nodes = gameManager.GetSkillTreeNodes();
        return nodes.Exists(node =>
            node != null &&
            node.owner == currentOwner &&
            (currentOwner != SkillTreeOwner.Heroine ||
                gameManager.IsSkillTreeNodeForCurrentHeroine(node)));
    }

    private string BuildDescription(SkillTreeNodeEvaluation evaluation)
    {
        SkillTreeNodeData node = evaluation.node;
        StringBuilder builder = new StringBuilder();
        builder.AppendLine(node.GetDisplayName());
        builder.AppendLine("状態：" + GetStateLabel(evaluation.state));
        builder.AppendLine("必要スキルポイント：" + evaluation.requiredSkillPoints);
        builder.AppendLine(BuildAvailabilityMessage(evaluation));

        if (evaluation.state == SkillTreeNodeState.Acquired && IsNodeLoadoutConfigurable(node))
        {
            if (IsTrainingSkillNode(node))
            {
                int activeCount = node.owner == SkillTreeOwner.Player
                    ? gameManager.GetActivePlayerTrainingSkillIds().Count
                    : gameManager.GetActiveHeroineTrainingSkillIds().Count;
                builder.AppendLine(
                    "有効状態：" +
                    (IsNodeInLoadout(node) ? "有効" : "無効") +
                    "（有効数 " + activeCount + "）");
            }
            else if (node.owner == SkillTreeOwner.Player)
            {
                int equippedCount = gameManager.GetEquippedPlayerBattleSkillIds().Count;
                builder.AppendLine(
                    "装備状態：" +
                    (IsNodeInLoadout(node) ? "装備中" : "未装備") +
                    "（" + equippedCount + " / " + gameManager.PlayerBattleSkillSlotCount + "）");
            }
            else
            {
                int equippedCount = gameManager.GetEquippedHeroineBattleSkillIds().Count;
                builder.AppendLine(
                    "編成状態：" +
                    (IsNodeInLoadout(node) ? "編成中" : "未編成") +
                    "（" + equippedCount + " / " + gameManager.HeroineBattleSkillSlotCount + "）");
            }
        }

        if (IsTrainingSkillNode(node))
        {
            builder.AppendLine("適用対象：" + GetTrainingApplicationLabel(node.skill));
        }

        if (node.unlockedTrainingIds != null && node.unlockedTrainingIds.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("解放する訓練：");
            for (int i = 0; i < node.unlockedTrainingIds.Count; i++)
            {
                string trainingId = node.unlockedTrainingIds[i];
                if (!string.IsNullOrWhiteSpace(trainingId))
                {
                    builder.AppendLine("・" + gameManager.GetTrainingDisplayName(trainingId));
                }
            }
        }

        if (node.skill != null && !string.IsNullOrEmpty(node.skill.description))
        {
            builder.AppendLine();
            builder.AppendLine(node.skill.description);
        }
        else if (node.owner == SkillTreeOwner.Heroine &&
            !string.IsNullOrEmpty(node.grantedHeroineSkillId))
        {
            builder.AppendLine();
            builder.AppendLine(
                "習得スキル：" +
                gameManager.GetHeroineBattleSkillDisplayName(node.grantedHeroineSkillId));
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
        if (selectedNode == null)
        {
            return;
        }

        SkillTreeNodeEvaluation evaluation = gameManager.EvaluateSkillTreeNode(selectedNode);
        if (evaluation.state == SkillTreeNodeState.Acquired &&
            IsNodeLoadoutConfigurable(selectedNode))
        {
            string equipmentMessage;
            if (IsTrainingSkillNode(selectedNode))
            {
                if (selectedNode.owner == SkillTreeOwner.Player)
                {
                    gameManager.TryTogglePlayerTrainingSkill(
                        selectedNode.skill.skillId,
                        out equipmentMessage);
                }
                else
                {
                    gameManager.TryToggleHeroineTrainingSkill(
                        selectedNode.skill.skillId,
                        out equipmentMessage);
                }
            }
            else if (selectedNode.owner == SkillTreeOwner.Player)
            {
                gameManager.TryTogglePlayerBattleSkill(
                    selectedNode.skill.skillId,
                    out equipmentMessage);
            }
            else
            {
                gameManager.TryToggleHeroineBattleSkill(
                    selectedNode.grantedHeroineSkillId,
                    out equipmentMessage);
            }
            feedbackMessage = equipmentMessage;
            Refresh();
            return;
        }

        if (!gameManager.TryAcquireSkillTreeNode(selectedNode))
        {
            feedbackMessage = "取得できませんでした。条件とスキルポイントを確認してください。";
            RefreshSelectedNode();
            return;
        }

        feedbackMessage = selectedNode.GetDisplayName() + "を習得しました。";
        Refresh();
    }

    private static bool IsNodeLoadoutConfigurable(SkillTreeNodeData node)
    {
        if (node == null) return false;
        if (IsTrainingSkillNode(node)) return true;
        if (node.owner == SkillTreeOwner.Heroine)
        {
            return !string.IsNullOrEmpty(node.grantedHeroineSkillId);
        }

        return node.skill != null &&
            node.skill.isEnabled &&
            node.skill.category == SkillCategory.Battle &&
            node.skill.canUseInBattle;
    }

    private bool IsNodeInLoadout(SkillTreeNodeData node)
    {
        if (node == null || gameManager == null) return false;
        if (IsTrainingSkillNode(node))
        {
            return node.owner == SkillTreeOwner.Player
                ? gameManager.IsPlayerTrainingSkillActive(node.skill.skillId)
                : gameManager.IsHeroineTrainingSkillActive(node.skill.skillId);
        }

        return node.owner == SkillTreeOwner.Player
            ? node.skill != null && gameManager.IsPlayerBattleSkillEquipped(node.skill.skillId)
            : gameManager.IsHeroineBattleSkillEquipped(node.grantedHeroineSkillId);
    }

    private static bool IsTrainingSkillNode(SkillTreeNodeData node)
    {
        return node != null &&
            node.skill != null &&
            node.skill.isEnabled &&
            node.skill.category == SkillCategory.Training &&
            node.skill.canUseInTraining;
    }

    private static string GetActiveNodeLabel(SkillTreeNodeData node)
    {
        if (IsTrainingSkillNode(node)) return " / 有効";
        return node.owner == SkillTreeOwner.Player ? " / 装備中" : " / 編成中";
    }

    private static string GetToggleButtonLabel(SkillTreeNodeData node, bool isActive)
    {
        if (IsTrainingSkillNode(node))
        {
            return isActive ? "無効にする" : "有効にする";
        }

        if (isActive) return "外す";
        return node.owner == SkillTreeOwner.Player ? "装備する" : "編成する";
    }

    private void SetAcquireButtonLabel(string label)
    {
        if (acquireButton == null) return;
        TextMeshProUGUI buttonText = acquireButton.GetComponentInChildren<TextMeshProUGUI>(true);
        if (buttonText != null)
        {
            buttonText.text = label;
        }
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

    private string GetConditionLabel(SkillTreeUnlockCondition condition)
    {
        if (condition == null) return "不明な条件";
        string targetName = gameManager != null
            ? gameManager.GetSkillTreeConditionTargetDisplayName(condition)
            : condition.targetId;
        string target = string.IsNullOrEmpty(targetName) ? "" : "（" + targetName + "）";
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

    private string GetTrainingApplicationLabel(SkillData skill)
    {
        if (skill == null)
        {
            return "不明";
        }

        if (skill.trainingApplicationScope ==
            TrainingSkillApplicationScope.AllTrainings)
        {
            return "全訓練";
        }

        SkillTreeProgressScope progressScope =
            skill.trainingApplicationScope ==
                TrainingSkillApplicationScope.TrainingCategory
                ? SkillTreeProgressScope.TrainingCategory
                : SkillTreeProgressScope.Training;
        SkillTreeUnlockCondition target = new SkillTreeUnlockCondition
        {
            scope = progressScope,
            targetId = skill.trainingApplicationTargetId
        };
        string displayName = gameManager != null
            ? gameManager.GetSkillTreeConditionTargetDisplayName(target)
            : skill.trainingApplicationTargetId;
        string scopeLabel = progressScope == SkillTreeProgressScope.TrainingCategory
            ? "カテゴリー"
            : "訓練";
        return scopeLabel + "（" +
            (string.IsNullOrEmpty(displayName) ? "未設定" : displayName) + "）";
    }

    private void ClearSpawnedNodes()
    {
        for (int i = 0; i < spawnedConnectors.Count; i++)
        {
            if (spawnedConnectors[i] != null) Destroy(spawnedConnectors[i]);
        }
        spawnedConnectors.Clear();

        for (int i = 0; i < spawnedNodeButtons.Count; i++)
        {
            if (spawnedNodeButtons[i] != null) Destroy(spawnedNodeButtons[i]);
        }
        spawnedNodeButtons.Clear();
        nodeButtonNodes.Clear();
        nodeRects.Clear();
    }

    private string ResolveCurrentHeroineName()
    {
        HeroineProfileData profile = gameManager != null ? gameManager.CurrentHeroineProfile : null;
        return profile != null && !string.IsNullOrEmpty(profile.displayName)
            ? profile.displayName
            : "ヒロイン";
    }

    private static string BuildAvailabilityMessage(SkillTreeNodeEvaluation evaluation)
    {
        switch (evaluation.state)
        {
            case SkillTreeNodeState.Available:
                return "このノードは取得できます。";
            case SkillTreeNodeState.InsufficientPoints:
                return "取得不可：スキルポイントが " +
                    Mathf.Max(0, evaluation.requiredSkillPoints - evaluation.currentSkillPoints) +
                    " 不足しています。";
            case SkillTreeNodeState.Acquired:
                return "このノードは習得済みです。";
            default:
                return "取得不可：前提ノードまたは解放条件を満たしていません。";
        }
    }

    private void RefreshNodeButtonVisuals()
    {
        foreach (KeyValuePair<Button, SkillTreeNodeData> pair in nodeButtonNodes)
        {
            if (pair.Key == null || pair.Value == null) continue;
            ApplyNodeButtonVisual(
                pair.Key,
                gameManager.EvaluateSkillTreeNode(pair.Value),
                pair.Value == selectedNode);
        }
    }

    private void ApplyNodeButtonVisual(
        Button button,
        SkillTreeNodeEvaluation evaluation,
        bool selected)
    {
        Color stateColor = GetNodeStateColor(evaluation.state);
        ColorBlock colors = button.colors;
        colors.normalColor = stateColor;
        colors.highlightedColor = Color.Lerp(stateColor, Color.white, 0.2f);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = Color.Lerp(stateColor, Color.black, 0.15f);
        colors.disabledColor = stateColor;
        button.colors = colors;

        Outline outline = button.GetComponent<Outline>();
        if (outline == null)
        {
            outline = button.gameObject.AddComponent<Outline>();
            outline.effectDistance = new Vector2(3f, -3f);
        }
        outline.effectColor = selectedOutlineColor;
        outline.enabled = selected;
    }

    private Color GetNodeStateColor(SkillTreeNodeState state)
    {
        switch (state)
        {
            case SkillTreeNodeState.Available: return availableColor;
            case SkillTreeNodeState.InsufficientPoints: return insufficientPointsColor;
            case SkillTreeNodeState.Acquired: return acquiredColor;
            default: return lockedColor;
        }
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
