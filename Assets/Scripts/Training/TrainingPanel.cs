using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TrainingPanel : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Image heroineImage;
    [SerializeField] private Button closeButton;

    [Header("Training List")]
    [SerializeField] private Transform trainingListParent;
    [SerializeField] private Button trainingButtonPrefab;
    [SerializeField] private TextMeshProUGUI emptyText;

    [Header("Status")]
    [SerializeField] private TextMeshProUGUI trainingNameText;
    [SerializeField] private TextMeshProUGUI stepCountText;
    [SerializeField] private TextMeshProUGUI playerHpText;
    [SerializeField] private TextMeshProUGUI heroineHpText;
    [SerializeField] private TextMeshProUGUI playerLpText;
    [SerializeField] private TextMeshProUGUI heroineLpText;
    [SerializeField] private TextMeshProUGUI resultLogText;

    [Header("Controls")]
    [SerializeField] private Button advanceButton;
    [SerializeField] private Button quitButton;

    [Header("Labels")]
    [SerializeField] private string emptyMessage = "選択できる訓練がありません。";
    [SerializeField] private string noTrainingLabel = "訓練未選択";
    [SerializeField] private int maxLogLines = 6;

    private readonly List<TrainingData> trainings = new List<TrainingData>();
    private readonly List<GameObject> trainingButtons = new List<GameObject>();
    private readonly List<string> logLines = new List<string>();
    private readonly List<SkillData> activePlayerTrainingSkills = new List<SkillData>();
    private readonly List<SkillData> activeHeroineTrainingSkills = new List<SkillData>();
    private BattleStatusData playerBattleStatus;
    private BattleStatusData heroineBattleStatus;
    private TrainingData currentTraining;
    private TrainingSessionState currentState;
    private TrainingStepModifiers activeTrainingSkillModifiers =
        new TrainingStepModifiers();
    private GameManager gameManager;
    private bool hasReportedResult;

    private GameObject PanelRoot
    {
        get { return panelRoot != null ? panelRoot : gameObject; }
    }

    private void Awake()
    {
        EnsureReferences();
        HookButtons();
        HideTemplateButton();
    }

    public void Initialize(GameManager manager)
    {
        gameManager = manager;
        EnsureReferences();
        HookButtons();
        HideTemplateButton();
    }

    public void Open(
        IReadOnlyList<TrainingData> availableTrainings,
        BattleStatusData playerStatus,
        BattleStatusData heroineStatus)
    {
        EnsureReferences();
        HookButtons();

        trainings.Clear();
        if (availableTrainings != null)
        {
            for (int i = 0; i < availableTrainings.Count; i++)
            {
                if (availableTrainings[i] != null)
                {
                    trainings.Add(availableTrainings[i]);
                }
            }
        }

        playerBattleStatus = playerStatus != null ? playerStatus.Clone() : new BattleStatusData();
        heroineBattleStatus = heroineStatus != null ? heroineStatus.Clone() : new BattleStatusData();
        activePlayerTrainingSkills.Clear();
        activeHeroineTrainingSkills.Clear();
        if (gameManager != null)
        {
            activePlayerTrainingSkills.AddRange(
                gameManager.GetActivePlayerTrainingSkills());
            activeHeroineTrainingSkills.AddRange(
                gameManager.GetActiveHeroineTrainingSkills());
        }
        activeTrainingSkillModifiers = new TrainingStepModifiers();
        currentTraining = null;
        currentState = null;
        hasReportedResult = false;
        logLines.Clear();

        PanelRoot.SetActive(true);
        RefreshTrainingList();
        RefreshStatus();
    }

    public void Close()
    {
        if (currentState != null && !currentState.isFinished)
        {
            currentState.Interrupt();
        }

        NotifyTrainingResult();
        PanelRoot.SetActive(false);
        if (gameManager != null)
        {
            gameManager.OnTrainingPanelClosed();
        }
    }

    public void SetHeroineSprite(Sprite sprite)
    {
        if (heroineImage == null)
        {
            return;
        }

        heroineImage.sprite = sprite;
        heroineImage.enabled = sprite != null;
        heroineImage.preserveAspect = true;
    }

    private void SelectTraining(TrainingData training)
    {
        if (training == null)
        {
            return;
        }

        currentTraining = training;
        activeTrainingSkillModifiers = TrainingStepModifiers.Create(
            training,
            activePlayerTrainingSkills,
            activeHeroineTrainingSkills);
        if (currentState == null)
        {
            currentState = TrainingSessionState.Create(training, playerBattleStatus, heroineBattleStatus);
            hasReportedResult = false;
            logLines.Clear();
            AddLog(training.GetDisplayName() + "を開始しました。");
            AddLog(currentState.maxSteps > 0
                ? "最大ステップ: " + currentState.maxSteps
                : "最大ステップ: 制限なし");
            AddTrainingPreviewLogs(training);
        }
        else
        {
            currentState.trainingId = training.trainingId;
            AddLog(training.GetDisplayName() + "に切り替えました。");
            AddTrainingPreviewLogs(training);
        }

        RefreshStatus();
    }

    private void AddTrainingPreviewLogs(TrainingData training)
    {
        List<string> playerSkillNames = GetApplicableSkillNames(
            activePlayerTrainingSkills,
            training);
        List<string> heroineSkillNames = GetApplicableSkillNames(
            activeHeroineTrainingSkills,
            training);
        if (playerSkillNames.Count == 0 && heroineSkillNames.Count == 0)
        {
            AddLog("有効スキル: なし");
        }
        else
        {
            AddLog(
                "有効スキル: 主人公[" + FormatSkillNames(playerSkillNames) +
                "] / ヒロイン[" + FormatSkillNames(heroineSkillNames) + "]");
        }

        TrainingStepResult preview = TrainingSessionState.CalculateStepResult(
            training,
            activeTrainingSkillModifiers);
        AddLog(
            "消費予定: 主人公HP " + preview.playerHpCost +
            " / ヒロインHP " + preview.heroineHpCost);
        AddLog(
            "報酬予定: 好感度 " + preview.affectionReward +
            " / 熟練度 " + preview.trainingProficiencyReward);
    }

    private static List<string> GetApplicableSkillNames(
        List<SkillData> skills,
        TrainingData training)
    {
        List<string> names = new List<string>();
        if (skills != null)
        {
            for (int i = 0; i < skills.Count; i++)
            {
                SkillData skill = skills[i];
                if (skill != null && skill.AppliesToTraining(training))
                {
                    names.Add(skill.GetDisplayName());
                }
            }
        }

        return names;
    }

    private static string FormatSkillNames(List<string> names)
    {
        return names != null && names.Count > 0
            ? string.Join("、", names.ToArray())
            : "なし";
    }

    private void AdvanceStep()
    {
        if (currentTraining == null || currentState == null || currentState.isFinished)
        {
            return;
        }

        int previousSimultaneousCount = currentState.simultaneousKnockoutCount;
        TrainingStepResult stepResult = currentState.AdvanceStep(
            currentTraining,
            activeTrainingSkillModifiers);
        AddLog(
            "Step " + currentState.elapsedSteps +
            ": 主人公 -" + stepResult.playerHpCost +
            " / ヒロイン -" + stepResult.heroineHpCost);
        if (stepResult.HasAppliedModifier)
        {
            AddLog(BuildTrainingSkillModifierLog(stepResult));
        }
        if (stepResult.affectionReward > 0)
        {
            AddLog(
                "好感度 +" + stepResult.affectionReward +
                "（今回 +" + currentState.totalStepAffectionReward + "）");
        }
        if (stepResult.trainingProficiencyReward > 0)
        {
            AddLog(
                "熟練度 +" + stepResult.trainingProficiencyReward +
                "（今回 +" + currentState.totalStepTrainingProficiencyReward + "）");
        }

        if (currentState.simultaneousKnockoutCount > previousSimultaneousCount)
        {
            AddLog("同時に限界を迎えました。ボーナス +" + currentTraining.simultaneousKnockoutBonus);
        }

        if (currentState.isFinished)
        {
            AddLog(GetTrainingEndLog(currentState.endReason));
            NotifyTrainingResult();
        }

        RefreshStatus();
    }

    private static string BuildTrainingSkillModifierLog(TrainingStepResult stepResult)
    {
        List<string> parts = new List<string>();
        int playerReduction = stepResult.basePlayerHpCost - stepResult.playerHpCost;
        int heroineReduction = stepResult.baseHeroineHpCost - stepResult.heroineHpCost;
        int affectionDifference =
            stepResult.affectionReward - stepResult.baseAffectionReward;
        int proficiencyDifference =
            stepResult.trainingProficiencyReward -
            stepResult.baseTrainingProficiencyReward;

        if (playerReduction > 0)
        {
            parts.Add("主人公HP消費 -" + playerReduction);
        }
        if (heroineReduction > 0)
        {
            parts.Add("ヒロインHP消費 -" + heroineReduction);
        }
        if (affectionDifference != 0)
        {
            parts.Add("好感度 " + FormatSignedValue(affectionDifference));
        }
        if (proficiencyDifference != 0)
        {
            parts.Add("熟練度 " + FormatSignedValue(proficiencyDifference));
        }

        return "スキル補正: " + string.Join(" / ", parts.ToArray());
    }

    private static string FormatSignedValue(int value)
    {
        return value > 0 ? "+" + value : value.ToString();
    }

    private void InterruptTraining()
    {
        if (currentState == null || currentState.isFinished)
        {
            return;
        }

        currentState.Interrupt();
        AddLog("訓練を途中でやめました。");
        NotifyTrainingResult();
        RefreshStatus();
    }

    private void NotifyTrainingResult()
    {
        if (hasReportedResult || gameManager == null || currentState == null)
        {
            return;
        }

        hasReportedResult = true;
        PanelRoot.SetActive(false);
        gameManager.OnTrainingPanelResult(TrainingResult.Create(currentTraining, currentState));
    }

    private void RefreshTrainingList()
    {
        ClearTrainingButtons();
        HideTemplateButton();

        bool hasTrainings = trainings.Count > 0;
        if (emptyText != null)
        {
            emptyText.gameObject.SetActive(!hasTrainings);
            emptyText.text = emptyMessage;
        }

        if (!hasTrainings || trainingListParent == null || trainingButtonPrefab == null)
        {
            if (hasTrainings)
            {
                Debug.LogWarning("TrainingPanel の trainingListParent または trainingButtonPrefab が設定されていません。");
            }

            return;
        }

        for (int i = 0; i < trainings.Count; i++)
        {
            CreateTrainingButton(trainings[i]);
        }
    }

    private void CreateTrainingButton(TrainingData training)
    {
        Button button = Instantiate(trainingButtonPrefab, trainingListParent);
        button.gameObject.SetActive(true);
        trainingButtons.Add(button.gameObject);

        TMP_Text buttonText = button.GetComponentInChildren<TMP_Text>();
        if (buttonText != null)
        {
            buttonText.text = FormatTrainingNameWithProficiency(training);
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => SelectTraining(training));
    }

    private void RefreshStatus()
    {
        if (trainingNameText != null)
        {
            string trainingLabel = currentTraining != null
                ? FormatTrainingNameWithProficiency(currentTraining)
                : noTrainingLabel;
            if (stepCountText == null && currentState != null)
            {
                trainingLabel += " / " + FormatStepCount(currentState);
            }
            trainingNameText.text = trainingLabel;
        }

        if (stepCountText != null)
        {
            stepCountText.text = currentState != null
                ? FormatStepCount(currentState)
                : "Step: -";
        }

        if (playerHpText != null)
        {
            playerHpText.text = "主人公HP: " + FormatHp(currentState != null ? currentState.playerHp : 0, currentState != null ? currentState.playerMaxHp : 0);
        }

        if (heroineHpText != null)
        {
            heroineHpText.text = "ヒロインHP: " + FormatHp(currentState != null ? currentState.heroineHp : 0, currentState != null ? currentState.heroineMaxHp : 0);
        }

        if (playerLpText != null)
        {
            playerLpText.text = "主人公LP: " + (currentState != null ? currentState.playerLp : 0);
        }

        if (heroineLpText != null)
        {
            heroineLpText.text = "ヒロインLP: " + (currentState != null ? currentState.heroineLp : 0);
        }

        if (resultLogText != null)
        {
            RefreshResultLog();
        }

        bool canAdvance = currentState != null && !currentState.isFinished;
        if (advanceButton != null)
        {
            advanceButton.interactable = canAdvance;
        }

        if (quitButton != null)
        {
            quitButton.interactable = canAdvance;
        }
    }

    private void AddLog(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return;
        }

        logLines.Add(message);
        int lineLimit = Mathf.Max(1, maxLogLines);
        while (logLines.Count > lineLimit)
        {
            logLines.RemoveAt(0);
        }
    }

    private void RefreshResultLog()
    {
        if (resultLogText == null)
        {
            return;
        }

        int lineLimit = Mathf.Max(1, maxLogLines);
        resultLogText.maxVisibleLines = lineLimit;
        resultLogText.overflowMode = TextOverflowModes.Truncate;
        while (true)
        {
            resultLogText.text = logLines.Count > 0
                ? string.Join("\n", logLines.ToArray())
                : "";
            resultLogText.ForceMeshUpdate(true, true);
            int visibleLineCount = resultLogText.textInfo != null
                ? resultLogText.textInfo.lineCount
                : 0;
            if (visibleLineCount <= lineLimit || logLines.Count <= 1)
            {
                break;
            }

            logLines.RemoveAt(0);
        }
    }

    private static string FormatHp(int currentHp, int maxHp)
    {
        return currentHp + " / " + maxHp;
    }

    private static string FormatStepCount(TrainingSessionState state)
    {
        if (state == null)
        {
            return "Step: -";
        }

        return state.maxSteps > 0
            ? "Step " + state.elapsedSteps + " / " + state.maxSteps
            : "Step " + state.elapsedSteps + " / 制限なし";
    }

    private static string GetTrainingEndLog(TrainingEndReason endReason)
    {
        switch (endReason)
        {
            case TrainingEndReason.StepLimitReached:
                return "最大ステップ数に到達し、訓練を完了しました。";
            case TrainingEndReason.HpOrLpDepleted:
                return "HP・LPの終了条件により訓練を完了しました。";
            case TrainingEndReason.Interrupted:
                return "訓練を途中でやめました。";
            default:
                return "訓練を終了しました。";
        }
    }

    private string FormatTrainingNameWithProficiency(TrainingData training)
    {
        if (training == null)
        {
            return noTrainingLabel;
        }

        return training.GetDisplayName() + " 熟練度 " + GetTrainingProficiency(training);
    }

    private int GetTrainingProficiency(TrainingData training)
    {
        if (training == null || gameManager == null)
        {
            return 0;
        }

        return gameManager.GetTrainingProficiency(training.trainingId);
    }

    private void ClearTrainingButtons()
    {
        for (int i = 0; i < trainingButtons.Count; i++)
        {
            if (trainingButtons[i] != null)
            {
                Destroy(trainingButtons[i]);
            }
        }

        trainingButtons.Clear();
    }

    private void HideTemplateButton()
    {
        if (trainingButtonPrefab != null)
        {
            trainingButtonPrefab.gameObject.SetActive(false);
        }
    }

    private void EnsureReferences()
    {
        if (panelRoot == null)
        {
            panelRoot = gameObject;
        }

        if (heroineImage == null)
        {
            Transform imageTransform = FindChildRecursive(transform, "HeroineImage");
            if (imageTransform != null)
            {
                heroineImage = imageTransform.GetComponent<Image>();
            }
        }

        if (trainingListParent == null)
        {
            trainingListParent = FindChildRecursive(transform, "TrainingList");
        }

        if (trainingButtonPrefab == null)
        {
            Transform buttonTransform = FindChildRecursive(transform, "TrainingButtonPrefab");
            if (buttonTransform != null)
            {
                trainingButtonPrefab = buttonTransform.GetComponent<Button>();
            }
        }

        if (emptyText == null)
        {
            emptyText = FindText("EmptyText");
        }

        if (trainingNameText == null)
        {
            trainingNameText = FindText("TrainingNameText");
        }

        if (stepCountText == null)
        {
            stepCountText = FindText("StepCountText");
        }

        if (playerHpText == null)
        {
            playerHpText = FindText("PlayerHpText");
        }

        if (heroineHpText == null)
        {
            heroineHpText = FindText("HeroineHpText");
        }

        if (playerLpText == null)
        {
            playerLpText = FindText("PlayerLpText");
        }

        if (heroineLpText == null)
        {
            heroineLpText = FindText("HeroineLpText");
        }

        if (resultLogText == null)
        {
            resultLogText = FindText("ResultLogText");
        }

        if (advanceButton == null)
        {
            advanceButton = FindButton("AdvanceButton");
        }

        if (quitButton == null)
        {
            quitButton = FindButton("QuitButton");
        }

        if (closeButton == null)
        {
            closeButton = FindButton("CloseButton");
        }

        if (heroineImage != null)
        {
            heroineImage.preserveAspect = true;
        }
    }

    private void HookButtons()
    {
        if (advanceButton != null)
        {
            advanceButton.onClick.RemoveListener(AdvanceStep);
            advanceButton.onClick.AddListener(AdvanceStep);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(InterruptTraining);
            quitButton.onClick.AddListener(InterruptTraining);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(Close);
            closeButton.onClick.AddListener(Close);
        }
    }

    private TextMeshProUGUI FindText(string objectName)
    {
        Transform textTransform = FindChildRecursive(transform, objectName);
        return textTransform != null ? textTransform.GetComponent<TextMeshProUGUI>() : null;
    }

    private Button FindButton(string objectName)
    {
        Transform buttonTransform = FindChildRecursive(transform, objectName);
        return buttonTransform != null ? buttonTransform.GetComponent<Button>() : null;
    }

    private Transform FindChildRecursive(Transform root, string objectName)
    {
        if (root == null)
        {
            return null;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == objectName)
            {
                return child;
            }

            Transform found = FindChildRecursive(child, objectName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
