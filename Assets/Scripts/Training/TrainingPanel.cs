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
    private BattleStatusData playerBattleStatus;
    private BattleStatusData heroineBattleStatus;
    private TrainingData currentTraining;
    private TrainingSessionState currentState;
    private GameManager gameManager;

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
        currentTraining = null;
        currentState = null;
        logLines.Clear();

        PanelRoot.SetActive(true);
        RefreshTrainingList();
        RefreshStatus();
    }

    public void Close()
    {
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
        if (currentState == null)
        {
            currentState = TrainingSessionState.Create(training, playerBattleStatus, heroineBattleStatus);
            logLines.Clear();
            AddLog(training.GetDisplayName() + "を開始しました。");
        }
        else
        {
            currentState.trainingId = training.trainingId;
            AddLog(training.GetDisplayName() + "に切り替えました。");
        }

        RefreshStatus();
    }

    private void AdvanceStep()
    {
        if (currentTraining == null || currentState == null || currentState.isFinished)
        {
            return;
        }

        int previousSimultaneousCount = currentState.simultaneousKnockoutCount;
        currentState.AdvanceStep(currentTraining);
        AddLog(
            "Step " + currentState.elapsedSteps +
            ": 主人公 -" + currentTraining.playerHpCostPerStep +
            " / ヒロイン -" + currentTraining.heroineHpCostPerStep);

        if (currentState.simultaneousKnockoutCount > previousSimultaneousCount)
        {
            AddLog("同時に限界を迎えました。ボーナス +" + currentTraining.simultaneousKnockoutBonus);
        }

        if (currentState.isFinished)
        {
            AddLog("訓練を終了しました。");
        }

        RefreshStatus();
    }

    private void InterruptTraining()
    {
        if (currentState == null || currentState.isFinished)
        {
            return;
        }

        currentState.Interrupt();
        AddLog("訓練を途中でやめました。");
        RefreshStatus();
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
            buttonText.text = training.GetDisplayName();
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => SelectTraining(training));
    }

    private void RefreshStatus()
    {
        if (trainingNameText != null)
        {
            trainingNameText.text = currentTraining != null ? currentTraining.GetDisplayName() : noTrainingLabel;
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
            resultLogText.text = logLines.Count > 0 ? string.Join("\n", logLines.ToArray()) : "";
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
        while (logLines.Count > maxLogLines)
        {
            logLines.RemoveAt(0);
        }
    }

    private static string FormatHp(int currentHp, int maxHp)
    {
        return currentHp + " / " + maxHp;
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
