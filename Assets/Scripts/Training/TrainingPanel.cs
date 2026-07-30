using System.Collections.Generic;
using System;
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
    [SerializeField] private Toggle availableOnlyToggle;

    [Header("Training Details")]
    [SerializeField] private TextMeshProUGUI detailNameText;
    [SerializeField] private TextMeshProUGUI detailDescriptionText;
    [SerializeField] private TextMeshProUGUI detailCostText;
    [SerializeField] private TextMeshProUGUI detailRewardText;
    [SerializeField] private TextMeshProUGUI detailRequirementText;
    [SerializeField] private Button startButton;
    [SerializeField] private Color selectedTrainingOutlineColor = new Color(1f, 0.82f, 0.2f, 1f);

    [Header("Status")]
    [SerializeField] private TextMeshProUGUI trainingNameText;
    [SerializeField] private TextMeshProUGUI conditionText;
    [SerializeField] private TextMeshProUGUI stepCountText;
    [SerializeField] private TextMeshProUGUI playerHpText;
    [SerializeField] private TextMeshProUGUI heroineHpText;
    [SerializeField] private TextMeshProUGUI playerLpText;
    [SerializeField] private TextMeshProUGUI heroineLpText;
    [SerializeField] private TextMeshProUGUI resultLogText;

    [Header("Heroine Dialogue")]
    [SerializeField] private TextMeshProUGUI heroineNameText;
    [SerializeField] private TextMeshProUGUI trainingMessageText;
    [SerializeField] private Button voiceReplayButton;

    [Header("Controls")]
    [SerializeField] private Button advanceButton;
    [SerializeField] private Button quitButton;

    [Header("Labels")]
    [SerializeField] private string emptyMessage = "選択できる訓練がありません。";
    [SerializeField] private string noTrainingLabel = "訓練未選択";
    [SerializeField] private int maxLogLines = 6;

    private readonly List<TrainingData> trainings = new List<TrainingData>();
    private readonly List<GameObject> trainingButtons = new List<GameObject>();
    private readonly Dictionary<TrainingData, Outline> trainingButtonOutlines =
        new Dictionary<TrainingData, Outline>();
    private readonly List<string> logLines = new List<string>();
    private readonly List<SkillData> activePlayerTrainingSkills = new List<SkillData>();
    private readonly List<SkillData> activeHeroineTrainingSkills = new List<SkillData>();
    private BattleStatusData playerBattleStatus;
    private BattleStatusData heroineBattleStatus;
    private TrainingData currentTraining;
    private TrainingData selectedTraining;
    private TrainingSessionState currentState;
    private TrainingStepModifiers activeTrainingSkillModifiers =
        new TrainingStepModifiers();
    private TrainingCondition currentTrainingCondition;
    private HeroineTrainingImageData trainingImageData;
    private HeroineTrainingDialogueData trainingDialogueData;
    private string lastTrainingMessage = string.Empty;
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
        AudioManager.Instance.PlayBgmById(AudioManager.TrainingBgmId);

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
        currentTrainingCondition = TrainingConditionResolver.Resolve(
            gameManager != null ? gameManager.CurrentDay : 1);
        currentTraining = null;
        selectedTraining = null;
        currentState = null;
        trainingImageData = LoadTrainingImageData();
        trainingDialogueData = LoadTrainingDialogueData();
        lastTrainingMessage = string.Empty;
        AudioManager.StopVoiceIfAvailable();
        RefreshHeroineName();
        if (trainingMessageText != null)
        {
            trainingMessageText.text = string.Empty;
        }
        hasReportedResult = false;
        logLines.Clear();
        AddLog(BuildConditionSummary(currentTrainingCondition));

        PanelRoot.SetActive(true);
        RefreshTrainingList();
        SelectInitialTrainingForDetails();
        RefreshStatus();
    }

    public void Close()
    {
        RestoreMainBgm();
        AudioManager.StopVoiceIfAvailable();
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
        if (currentState != null &&
            currentState.elapsedSteps > 0 &&
            currentTraining != training)
        {
            AddLog("訓練開始後は別の訓練へ切り替えられません。");
            RefreshStatus();
            return;
        }

        currentTraining = training;
        activeTrainingSkillModifiers = TrainingStepModifiers.Create(
            training,
            activePlayerTrainingSkills,
            activeHeroineTrainingSkills);
        activeTrainingSkillModifiers.condition = currentTrainingCondition;
        if (currentState == null)
        {
            currentState = TrainingSessionState.Create(training, playerBattleStatus, heroineBattleStatus);
            hasReportedResult = false;
            logLines.Clear();
            AddLog(BuildConditionSummary(currentTrainingCondition));
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

        ApplyTrainingPresentation(currentState.elapsedSteps > 0
            ? TrainingVisualState.SelectedAfterFirstStep
            : TrainingVisualState.SelectedBeforeFirstStep);
        RefreshStatus();
        RefreshTrainingButtonSelection();
    }

    private void SelectTrainingForDetails(TrainingData training)
    {
        selectedTraining = training;
        RefreshTrainingDetails();
        RefreshTrainingButtonSelection();
        RefreshStatus();
    }

    private void StartSelectedTraining()
    {
        if (selectedTraining == null || !IsTrainingAvailable(selectedTraining))
        {
            RefreshTrainingDetails();
            return;
        }

        SelectTraining(selectedTraining);
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
        ApplyTrainingPresentation(TrainingVisualStateResolver.Resolve(stepResult));
        AddLog(
            "Step " + currentState.elapsedSteps +
            ": 主人公 -" + stepResult.playerHpCost +
            " / ヒロイン -" + stepResult.heroineHpCost);
        if (stepResult.HasAppliedSkillModifier)
        {
            AddLog(BuildTrainingSkillModifierLog(stepResult));
        }
        if (stepResult.condition != null)
        {
            AddLog(BuildTrainingConditionModifierLog(stepResult));
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
            AudioManager.Instance.PlaySeById("Training/Complete");
            AddLog(GetTrainingEndLog(currentState.endReason));
            NotifyTrainingResult();
        }
        else
        {
            AudioManager.Instance.PlaySeById("Training/Step");
        }

        RefreshStatus();
    }

    private HeroineTrainingImageData LoadTrainingImageData()
    {
        if (gameManager == null || string.IsNullOrEmpty(gameManager.CurrentHeroineId))
        {
            return null;
        }

        string resourcePath =
            "Heroines/" + gameManager.CurrentHeroineId +
            "/TrainingImages/HeroineTrainingImageData";
        HeroineTrainingImageData data = Resources.Load<HeroineTrainingImageData>(resourcePath);
        if (data != null &&
            !string.IsNullOrEmpty(data.heroineId) &&
            data.heroineId != gameManager.CurrentHeroineId)
        {
            Debug.LogWarning(
                "HeroineTrainingImageData の heroineId が現在のヒロインと一致しません: " +
                data.heroineId + " / " + gameManager.CurrentHeroineId);
            return null;
        }

        return data;
    }

    private HeroineTrainingDialogueData LoadTrainingDialogueData()
    {
        if (gameManager == null || string.IsNullOrEmpty(gameManager.CurrentHeroineId))
        {
            return null;
        }

        string resourcePath =
            "Heroines/" + gameManager.CurrentHeroineId +
            "/TrainingDialogues/HeroineTrainingDialogueData";
        HeroineTrainingDialogueData data =
            Resources.Load<HeroineTrainingDialogueData>(resourcePath);
        if (data != null &&
            !string.IsNullOrEmpty(data.heroineId) &&
            data.heroineId != gameManager.CurrentHeroineId)
        {
            Debug.LogWarning(
                "HeroineTrainingDialogueData の heroineId が現在のヒロインと一致しません: " +
                data.heroineId + " / " + gameManager.CurrentHeroineId);
            return null;
        }

        return data;
    }

    private void ApplyTrainingPresentation(TrainingVisualState state)
    {
        ApplyTrainingImage(state);
        ApplyTrainingDialogue(state);
    }

    private void ApplyTrainingImage(TrainingVisualState state)
    {
        if (heroineImage == null || trainingImageData == null || currentTraining == null)
        {
            return;
        }

        Sprite sprite = trainingImageData.ResolveSprite(currentTraining.trainingId, state);
        if (sprite == null)
        {
            // 未設定時は、現在表示中のヒロイン画像を維持する。
            return;
        }

        heroineImage.sprite = sprite;
        heroineImage.enabled = true;
        heroineImage.preserveAspect = true;
    }

    private void ApplyTrainingDialogue(TrainingVisualState state)
    {
        AudioManager.StopVoiceIfAvailable();
        RefreshVoiceReplayButton();
        if (trainingMessageText == null || trainingDialogueData == null || currentTraining == null)
        {
            return;
        }

        HeroineTrainingDialogueSelection dialogue =
            trainingDialogueData.ResolveDialogue(
            currentTraining.trainingId,
            state,
            lastTrainingMessage);
        if (!dialogue.HasMessage)
        {
            // 未設定時は現在のセリフを維持する。
            return;
        }

        lastTrainingMessage = dialogue.Message;
        trainingMessageText.text = dialogue.Message;
        if (!string.IsNullOrWhiteSpace(dialogue.VoiceId))
        {
            AudioManager.Instance.PlayVoiceById(
                gameManager != null ? gameManager.CurrentHeroineId : string.Empty,
                dialogue.VoiceId);
        }
        RefreshVoiceReplayButton();
    }

    private void RefreshHeroineName()
    {
        if (heroineNameText == null)
        {
            return;
        }

        HeroineProfileData profile = gameManager != null
            ? gameManager.CurrentHeroineProfile
            : null;
        heroineNameText.text = profile != null && !string.IsNullOrEmpty(profile.displayName)
            ? profile.displayName
            : "ヒロイン";
    }

    private void ReplayCurrentVoice()
    {
        AudioManager.Instance.ReplayCurrentVoice();
        RefreshVoiceReplayButton();
    }

    private void RefreshVoiceReplayButton()
    {
        if (voiceReplayButton == null)
        {
            return;
        }

        AudioManager audioManager = AudioManager.Instance;
        voiceReplayButton.gameObject.SetActive(audioManager.HasPreparedVoice);
        voiceReplayButton.interactable = audioManager.CanReplayCurrentVoice;
    }

    private static string BuildTrainingSkillModifierLog(TrainingStepResult stepResult)
    {
        List<string> parts = new List<string>();
        int playerReduction =
            stepResult.basePlayerHpCost - stepResult.skillAdjustedPlayerHpCost;
        int heroineReduction =
            stepResult.baseHeroineHpCost - stepResult.skillAdjustedHeroineHpCost;
        int affectionDifference =
            stepResult.skillAdjustedAffectionReward -
            stepResult.baseAffectionReward;
        int proficiencyDifference =
            stepResult.skillAdjustedTrainingProficiencyReward -
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

        return parts.Count > 0
            ? "スキル補正: " + string.Join(" / ", parts.ToArray())
            : "スキル補正: 最終値への変化なし";
    }

    private static string BuildTrainingConditionModifierLog(
        TrainingStepResult stepResult)
    {
        TrainingCondition condition = stepResult.condition;
        int playerDifference =
            stepResult.playerHpCost - stepResult.skillAdjustedPlayerHpCost;
        int heroineDifference =
            stepResult.heroineHpCost - stepResult.skillAdjustedHeroineHpCost;
        int affectionDifference =
            stepResult.affectionReward - stepResult.skillAdjustedAffectionReward;
        int proficiencyDifference =
            stepResult.trainingProficiencyReward -
            stepResult.skillAdjustedTrainingProficiencyReward;
        return "調子補正(" + condition.DisplayName + "): HP 主" +
            FormatSignedValue(playerDifference) + "/姫" +
            FormatSignedValue(heroineDifference) + " / 好感度 " +
            FormatSignedValue(affectionDifference) + " / 熟練度 " +
            FormatSignedValue(proficiencyDifference);
    }

    private static string BuildConditionSummary(TrainingCondition condition)
    {
        if (condition == null)
        {
            return "本日の調子: 未設定";
        }

        return "本日の調子: " + condition.DisplayName +
            "（30日周期 " + condition.cycleDay + "日目 / HP " +
            FormatSignedValue(condition.playerHpCostModifier) + " / 好感度 " +
            FormatSignedValue(condition.affectionRewardModifier) + " / 熟練度 " +
            FormatSignedValue(condition.trainingProficiencyRewardModifier) + "）";
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
        AudioManager.Instance.PlaySeById("Training/Cancel");
        AudioManager.StopVoiceIfAvailable();
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
        RestoreMainBgm();
        AudioManager.StopVoiceIfAvailable();
        PanelRoot.SetActive(false);
        gameManager.OnTrainingPanelResult(TrainingResult.Create(currentTraining, currentState));
    }

    private static void RestoreMainBgm()
    {
        AudioManager.Instance.PlayBgmById(AudioManager.MainBgmId);
    }

    private void RefreshTrainingList()
    {
        ClearTrainingButtons();
        HideTemplateButton();

        bool availableOnly = availableOnlyToggle != null && availableOnlyToggle.isOn;
        List<TrainingData> displayTrainings = TrainingListPresentation.CreateDisplayList(
            trainings,
            IsTrainingAvailable,
            availableOnly);
        bool hasTrainings = displayTrainings.Count > 0;
        if (emptyText != null)
        {
            emptyText.gameObject.SetActive(!hasTrainings);
            emptyText.text = availableOnly && trainings.Count > 0
                ? "現在実行できる訓練がありません。"
                : emptyMessage;
        }

        if (!hasTrainings || trainingListParent == null || trainingButtonPrefab == null)
        {
            if (hasTrainings)
            {
                Debug.LogWarning("TrainingPanel の trainingListParent または trainingButtonPrefab が設定されていません。");
            }

            return;
        }

        for (int i = 0; i < displayTrainings.Count; i++)
        {
            CreateTrainingButton(displayTrainings[i]);
        }

        if (selectedTraining != null && !displayTrainings.Contains(selectedTraining))
        {
            selectedTraining = null;
        }
        RefreshTrainingButtonSelection();
        RefreshTrainingDetails();
    }

    private void CreateTrainingButton(TrainingData training)
    {
        Button button = Instantiate(trainingButtonPrefab, trainingListParent);
        button.gameObject.SetActive(true);
        trainingButtons.Add(button.gameObject);

        TrainingAvailability availability = EvaluateTrainingAvailability(training);
        bool isAvailable = availability.CanExecute;

        TMP_Text buttonText = button.GetComponentInChildren<TMP_Text>();
        if (buttonText != null)
        {
            if (isAvailable)
            {
                buttonText.text = FormatTrainingNameWithProficiency(training);
            }
            else
            {
                buttonText.text = FormatTrainingNameWithProficiency(training) +
                    "\n[" + GetAvailabilityReason(training, availability) + "]";
            }
        }

        button.onClick.RemoveAllListeners();
        bool useDetails = startButton != null;
        button.interactable = useDetails || isAvailable;
        if (useDetails)
        {
            button.onClick.AddListener(() => SelectTrainingForDetails(training));
        }
        else if (isAvailable)
        {
            button.onClick.AddListener(() => SelectTraining(training));
        }

        Outline outline = button.GetComponent<Outline>();
        if (outline == null)
        {
            outline = button.gameObject.AddComponent<Outline>();
        }
        outline.effectColor = selectedTrainingOutlineColor;
        outline.effectDistance = new Vector2(3f, -3f);
        outline.useGraphicAlpha = true;
        outline.enabled = false;
        trainingButtonOutlines[training] = outline;
    }

    private void RefreshStatus()
    {
        if (conditionText != null)
        {
            conditionText.text = BuildConditionSummary(currentTrainingCondition);
        }

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
            playerHpText.text = "HP: " + FormatHp(currentState != null ? currentState.playerHp : 0, currentState != null ? currentState.playerMaxHp : 0);
        }

        if (heroineHpText != null)
        {
            heroineHpText.text = "HP: " + FormatHp(currentState != null ? currentState.heroineHp : 0, currentState != null ? currentState.heroineMaxHp : 0);
        }

        if (playerLpText != null)
        {
            playerLpText.text = "LP: " + (currentState != null ? currentState.playerLp : 0);
        }

        if (heroineLpText != null)
        {
            heroineLpText.text = "LP: " + (currentState != null ? currentState.heroineLp : 0);
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

        RefreshTrainingDetails();
        RefreshVoiceReplayButton();
    }

    private bool IsTrainingAvailable(TrainingData training)
    {
        return EvaluateTrainingAvailability(training).CanExecute;
    }

    private TrainingAvailability EvaluateTrainingAvailability(TrainingData training)
    {
        if (gameManager != null)
        {
            return gameManager.EvaluateTrainingAvailability(training);
        }

        return TrainingAvailabilityEvaluator.Evaluate(
            training,
            currentTrainingCondition != null
                ? currentTrainingCondition.rank
                : TrainingConditionRank.Normal,
            true,
            null);
    }

    private string GetAvailabilityReason(
        TrainingData training,
        TrainingAvailability availability)
    {
        if (availability != null && !string.IsNullOrEmpty(availability.ReasonText))
        {
            return availability.ReasonText;
        }
        if (gameManager != null && training != null)
        {
            string unlockReason = gameManager.GetTrainingUnlockRequirementLabel(training);
            if (!string.IsNullOrEmpty(unlockReason))
            {
                return "未解放：" + unlockReason;
            }
        }
        return "実行できません";
    }

    private void SelectInitialTrainingForDetails()
    {
        if (startButton == null)
        {
            return;
        }

        List<TrainingData> displayTrainings = TrainingListPresentation.CreateDisplayList(
            trainings,
            IsTrainingAvailable,
            availableOnlyToggle != null && availableOnlyToggle.isOn);
        selectedTraining = displayTrainings.Count > 0 ? displayTrainings[0] : null;
        RefreshTrainingButtonSelection();
        RefreshTrainingDetails();
    }

    private void RefreshTrainingButtonSelection()
    {
        foreach (KeyValuePair<TrainingData, Outline> pair in trainingButtonOutlines)
        {
            if (pair.Value != null)
            {
                pair.Value.enabled = pair.Key == selectedTraining;
            }
        }
    }

    private void RefreshTrainingDetails()
    {
        TrainingData training = selectedTraining;
        bool available = IsTrainingAvailable(training);
        if (detailNameText != null)
        {
            detailNameText.text = training != null
                ? FormatTrainingNameWithProficiency(training)
                : noTrainingLabel;
        }
        if (detailDescriptionText != null)
        {
            detailDescriptionText.text = training != null &&
                !string.IsNullOrWhiteSpace(training.description)
                ? training.description
                : "説明はありません。";
        }
        if (detailCostText != null)
        {
            detailCostText.text = training != null
                ? BuildTrainingCostDetails(training)
                : "消費: -";
        }
        if (detailRewardText != null)
        {
            detailRewardText.text = training != null
                ? BuildTrainingRewardDetails(training)
                : "報酬: -";
        }
        if (detailRequirementText != null)
        {
            detailRequirementText.text = training == null
                ? "訓練を選択してください。"
                : available
                    ? "実行可能"
                    : GetAvailabilityReason(
                        training,
                        EvaluateTrainingAvailability(training));
        }
        if (startButton != null)
        {
            bool canStart = training != null &&
                available &&
                (currentState == null ||
                    (!currentState.isFinished && currentTraining != training));
            startButton.interactable = canStart;
            TMP_Text label = startButton.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                label.text = currentState == null ? "この訓練を開始" : "この訓練に切替";
            }
        }
    }

    private string BuildTrainingCostDetails(TrainingData training)
    {
        TrainingStepModifiers modifiers = TrainingStepModifiers.Create(
            training,
            activePlayerTrainingSkills,
            activeHeroineTrainingSkills);
        modifiers.condition = currentTrainingCondition;
        TrainingStepResult preview = TrainingSessionState.CalculateStepResult(
            training,
            modifiers);
        int displayedMaxSteps = currentState != null
            ? currentState.maxSteps
            : training.maxSteps;
        return "1ステップ消費: 主人公HP " + preview.playerHpCost +
            " / ヒロインHP " + preview.heroineHpCost +
            "\n初期LP: 主人公 " + Mathf.Max(0, training.initialPlayerLp) +
            " / ヒロイン " + Mathf.Max(0, training.initialHeroineLp) +
            "\n最大ステップ（開始時固定）: " +
            (displayedMaxSteps > 0 ? displayedMaxSteps.ToString() : "制限なし");
    }

    private string BuildTrainingRewardDetails(TrainingData training)
    {
        TrainingStepModifiers modifiers = TrainingStepModifiers.Create(
            training,
            activePlayerTrainingSkills,
            activeHeroineTrainingSkills);
        modifiers.condition = currentTrainingCondition;
        TrainingStepResult preview = TrainingSessionState.CalculateStepResult(
            training,
            modifiers);
        return "ステップ報酬: 好感度 " + preview.affectionReward +
            " / 熟練度 " + preview.trainingProficiencyReward +
            "\n完了報酬: 好感度 " + Mathf.Max(0, training.affectionReward) +
            " / 熟練度 " + Mathf.Max(0, training.trainingProficiencyReward) +
            "\nスキルポイント: 主人公 " + Mathf.Max(0, training.playerSkillPointReward) +
            " / ヒロイン " + Mathf.Max(0, training.heroineSkillPointReward);
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
        trainingButtonOutlines.Clear();
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

        if (availableOnlyToggle == null)
        {
            Transform toggleTransform = FindChildRecursive(transform, "AvailableOnlyToggle");
            if (toggleTransform != null)
            {
                availableOnlyToggle = toggleTransform.GetComponent<Toggle>();
            }
        }

        if (detailNameText == null) detailNameText = FindText("TrainingDetailNameText");
        if (detailDescriptionText == null) detailDescriptionText = FindText("TrainingDescriptionText");
        if (detailCostText == null) detailCostText = FindText("TrainingCostText");
        if (detailRewardText == null) detailRewardText = FindText("TrainingRewardText");
        if (detailRequirementText == null) detailRequirementText = FindText("TrainingRequirementText");
        if (startButton == null) startButton = FindButton("TrainingStartButton");

        if (trainingNameText == null)
        {
            trainingNameText = FindText("TrainingNameText");
        }

        if (conditionText == null)
        {
            conditionText = FindText("ConditionText");
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

        if (heroineNameText == null)
        {
            heroineNameText = FindText("HeroineNameText");
        }

        if (trainingMessageText == null)
        {
            trainingMessageText = FindText("TrainingMessageText");
        }

        if (voiceReplayButton == null)
        {
            voiceReplayButton = FindButton("VoiceReplayButton");
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

        if (startButton != null)
        {
            startButton.onClick.RemoveListener(StartSelectedTraining);
            startButton.onClick.AddListener(StartSelectedTraining);
        }

        if (availableOnlyToggle != null)
        {
            availableOnlyToggle.onValueChanged.RemoveListener(OnAvailableOnlyChanged);
            availableOnlyToggle.onValueChanged.AddListener(OnAvailableOnlyChanged);
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

        if (voiceReplayButton != null)
        {
            voiceReplayButton.onClick.RemoveListener(ReplayCurrentVoice);
            voiceReplayButton.onClick.AddListener(ReplayCurrentVoice);
        }
    }

    private void OnAvailableOnlyChanged(bool value)
    {
        RefreshTrainingList();
        if (selectedTraining == null)
        {
            SelectInitialTrainingForDetails();
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

public static class TrainingListPresentation
{
    public static List<TrainingData> CreateDisplayList(
        IEnumerable<TrainingData> source,
        Func<TrainingData, bool> isAvailable,
        bool availableOnly)
    {
        List<TrainingData> result = new List<TrainingData>();
        if (source != null)
        {
            foreach (TrainingData training in source)
            {
                if (training == null)
                {
                    continue;
                }

                bool available = isAvailable == null || isAvailable(training);
                if (!availableOnly || available)
                {
                    result.Add(training);
                }
            }
        }

        result.Sort((left, right) =>
        {
            bool leftAvailable = isAvailable == null || isAvailable(left);
            bool rightAvailable = isAvailable == null || isAvailable(right);
            int availableComparison = rightAvailable.CompareTo(leftAvailable);
            if (availableComparison != 0)
            {
                return availableComparison;
            }

            int orderComparison = left.sortOrder.CompareTo(right.sortOrder);
            if (orderComparison != 0)
            {
                return orderComparison;
            }

            int nameComparison = string.Compare(
                left.GetDisplayName(),
                right.GetDisplayName(),
                StringComparison.Ordinal);
            if (nameComparison != 0)
            {
                return nameComparison;
            }

            return string.Compare(
                left.trainingId,
                right.trainingId,
                StringComparison.Ordinal);
        });
        return result;
    }
}
