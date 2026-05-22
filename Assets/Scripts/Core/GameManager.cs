using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class GameManager : MonoBehaviour
{
    private enum ConversationFlowState
    {
        Idle,
        ShowingQuestion,
        WaitingChoice,
        ShowingResponse,
        ShowingSimple,
        ShowingActionResult,
        ShowingGoodNight
    }

    [Header("Managers")]
    [SerializeField] private TimeManager timeManager;
    [SerializeField] private HeroineStatus heroineStatus;
    [SerializeField] private SaveManager saveManager;
    [SerializeField] private OutfitManager outfitManager;
    [SerializeField] private OutfitPreferenceManager outfitPreferenceManager;
    [SerializeField] private ScheduleManager scheduleManager;

    [Header("Status")]
    [SerializeField] private TMP_Text dayText;
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private TMP_Text weekdayText;
    [SerializeField] private TMP_Text seasonText;
    [SerializeField] private TMP_Text weatherText;
    [SerializeField] private TMP_Text affectionText;
    [SerializeField] private TMP_Text affectionRankText;

    [Header("Schedule UI")]
    [SerializeField] private TextMeshProUGUI todayScheduleText;
    [SerializeField] private TextMeshProUGUI tomorrowScheduleText;
    [SerializeField] private GameObject schedulePanel;

    [Header("Dialogue")]
    [SerializeField] private TMP_Text speakerNameText;
    [SerializeField] private TMP_Text dialogueText;

    [Header("Fade")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 1.5f;

    [Header("Action Buttons")]
    [SerializeField] private GameObject actionButtonArea;
    [SerializeField] private Transform actionButtonParent;
    [SerializeField] private Button actionButtonPrefab;

    [Header("Genre Buttons")]
    [SerializeField] private GameObject genreButtonArea;
    [SerializeField] private Transform genreButtonParent;
    [SerializeField] private Button genreButtonPrefab;

    [Header("Choice Buttons")]
    [SerializeField] private GameObject choiceButtonArea;
    [SerializeField] private Button choiceButton1;
    [SerializeField] private Button choiceButton2;
    [SerializeField] private Button choiceButton3;

    [Header("Control Buttons")]
    [SerializeField] private Button nextButton;

    [Header("Ending")]
    [SerializeField] private Button endingButton;

    [Header("Action Data")]
    [SerializeField] private string actionResourcePath = "Actions";

    private List<ActionData> actions = new List<ActionData>();

    [Header("Conversation Data")]
    [SerializeField] private string conversationResourcePath = "Conversations";

    private List<ConversationData> conversations = new List<ConversationData>();

    private ConversationFlowState flowState = ConversationFlowState.Idle;

    private ConversationData currentConversation;

    private bool pendingAdvanceTime = false;
    private bool pendingGoodNight = false;
    private bool isFading = false;

    private readonly HashSet<string> shownConversationIds = new HashSet<string>();

    [Header("Save / Load Buttons")]
    [SerializeField] private Button saveButton;
    [SerializeField] private Button loadButton;


    [Header("Background")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Sprite dayBackgroundSprite;
    [SerializeField] private Sprite nightBackgroundSprite;
    [SerializeField] private BackgroundZoom backgroundZoom;

    [Header("Outfit UI")]
    [SerializeField] private GameObject outfitPanel;
    [SerializeField] private Transform outfitButtonParent;
    [SerializeField] private Button outfitButtonPrefab;

    [Header("Outfit Reaction UI")]
    [SerializeField] private GameObject outfitReactionPanel;
    [SerializeField] private Button praiseOutfitButton;
    [SerializeField] private Button dislikeOutfitButton;
    [SerializeField] private Button boredOutfitButton;
    [SerializeField] private Button changeOutfitButton;


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            OpenSchedulePanel();
        }
    }

    private void UpdateBackgroundByTime()
    {
        if (backgroundImage == null)
        {
            return;
        }

        if (timeManager.CurrentTimeSlot == TimeSlot.Night ||
            timeManager.CurrentTimeSlot == TimeSlot.LateNight)
        {
            if (nightBackgroundSprite != null)
            {
                backgroundImage.sprite = nightBackgroundSprite;
            }

            return;
        }

        if (dayBackgroundSprite != null)
        {
            backgroundImage.sprite = dayBackgroundSprite;
        }
        OnTalkStart();
    }

    public void OnTalkStart()
    {
        backgroundZoom.ZoomRight();
    }

    public void OnLookAtWindow()
    {
        backgroundZoom.ZoomRight();
    }

    public void OnBackToNormal()
    {
        backgroundZoom.ResetZoom();
    }

    private void Start()
    {
        LoadConversationsFromResources();
        LoadActionsFromResources();

        CreateGenreButtons();
        CreateActionButtons();
        CreateOutfitButtons();

        SetFadeAlpha(0f);

        praiseOutfitButton.onClick.AddListener(() => OnClickOutfitReaction(OutfitReactionType.Praise));
        dislikeOutfitButton.onClick.AddListener(() => OnClickOutfitReaction(OutfitReactionType.Dislike));
        boredOutfitButton.onClick.AddListener(() => OnClickOutfitReaction(OutfitReactionType.Bored));
        changeOutfitButton.onClick.AddListener(() => OnClickOutfitReaction(OutfitReactionType.Change));

        nextButton.onClick.AddListener(OnClickNext);
        endingButton.onClick.AddListener(OnClickEnding);

        actionButtonArea.SetActive(true);
        genreButtonArea.SetActive(false);
        choiceButtonArea.SetActive(false);
        outfitPanel.SetActive(false);
        outfitReactionPanel.SetActive(false);
        nextButton.gameObject.SetActive(false);
        endingButton.gameObject.SetActive(false);

        saveButton.onClick.AddListener(SaveGame);
        loadButton.onClick.AddListener(LoadGame);

        timeManager.OnDayChanged += OnDayChanged;

        outfitManager.WearDefaultOutfit();
        speakerNameText.text = heroineStatus.HeroineName;
        dialogueText.text = "今日は何を話しましょうか？";
        
        OnTalkStart();
        //background.localScale = new Vector3(1.3f, 1.3f, 1f);
        //background.anchoredPosition = new Vector2(-200f, 0f);

        RefreshUI();

        if (GameStartSettings.ShouldLoadOnStart)
        {
            GameStartSettings.ShouldLoadOnStart = false;
            LoadGame();
        }

        RefreshUI();
    }

    private void LoadConversationsFromResources()
    {
        ConversationData[] loadedConversations =
            Resources.LoadAll<ConversationData>(conversationResourcePath);

        conversations = new List<ConversationData>(loadedConversations);

        Debug.Log("Loaded Conversations: " + conversations.Count);

        foreach (ConversationData conversation in conversations)
        {
            Debug.Log(
                "Conversation: " +
                conversation.name +
                " / Id: " +
                conversation.conversationId +
                " / Genre: " +
                conversation.genre +
                " / Type: " +
                conversation.type
            );
        }
    }

    private void CreateGenreButtons()
    {
        if (genreButtonParent == null)
        {
            Debug.LogError("Genre Button Parent が設定されていません。");
            return;
        }

        if (genreButtonPrefab == null)
        {
            Debug.LogError("Genre Button Prefab が設定されていません。");
            return;
        }

        ClearGenreButtons();

        ConversationGenre[] genres =
            (ConversationGenre[])Enum.GetValues(typeof(ConversationGenre));

        foreach (ConversationGenre genre in genres)
        {
            Button button = Instantiate(genreButtonPrefab, genreButtonParent);

            TMP_Text buttonText = button.GetComponentInChildren<TMP_Text>();
            if (buttonText != null)
            {
                buttonText.text = GetGenreDisplayName(genre);
            }

            ConversationGenre capturedGenre = genre;
            button.onClick.AddListener(() => StartTalk(capturedGenre));
        }
    }

    private void ClearGenreButtons()
    {
        for (int i = genreButtonParent.childCount - 1; i >= 0; i--)
        {
            Destroy(genreButtonParent.GetChild(i).gameObject);
        }
    }

    private string GetGenreDisplayName(ConversationGenre genre)
    {
        switch (genre)
        {
            case ConversationGenre.Daily:
                return "日常";

            case ConversationGenre.Food:
                return "食べ物";

            case ConversationGenre.Adventure:
                return "冒険";

            case ConversationGenre.Love:
                return "恋愛";

            default:
                return genre.ToString();
        }
    }

    private void StartTalk(ConversationGenre genre)
    {
        actionButtonArea.SetActive(false);
        genreButtonArea.SetActive(false);
        choiceButtonArea.SetActive(false);
        outfitPanel.SetActive(false);
        outfitReactionPanel.SetActive(false);
        nextButton.gameObject.SetActive(false);

        List<ConversationData> candidates = conversations.FindAll(conversation =>
            conversation.genre == genre &&
            IsConversationAvailable(conversation) &&
            CanShowConversation(conversation)
        );

        if (candidates.Count == 0)
        {
            speakerNameText.text = "システム";
            dialogueText.text = "現在の条件に合う会話データがありません。";

            flowState = ConversationFlowState.ShowingSimple;
            nextButton.gameObject.SetActive(true);
            return;
        }

        currentConversation = SelectConversationByPriority(candidates);

        RegisterShownConversation(currentConversation);

        speakerNameText.text = heroineStatus.HeroineName;
        dialogueText.text = currentConversation.heroineLine;

        if (currentConversation.type == ConversationType.Simple)
        {
            flowState = ConversationFlowState.ShowingSimple;
        }
        else
        {
            flowState = ConversationFlowState.ShowingQuestion;
        }

        nextButton.gameObject.SetActive(true);
    }

    private bool IsConversationAvailable(ConversationData conversation)
    {
        if (conversation == null)
        {
            return false;
        }

        if (heroineStatus.Affection < conversation.minAffection)
        {
            return false;
        }

        if (heroineStatus.Affection > conversation.maxAffection)
        {
            return false;
        }

        if (!conversation.anyTimeSlot)
        {
            if (conversation.allowedTimeSlots == null ||
                !conversation.allowedTimeSlots.Contains(timeManager.CurrentTimeSlot))
            {
                return false;
            }
        }

        if (!conversation.anySeason)
        {
            if (conversation.allowedSeasons == null ||
                !conversation.allowedSeasons.Contains(timeManager.CurrentSeason))
            {
                return false;
            }
        }

        if (!conversation.anyWeather)
        {
            if (conversation.allowedWeathers == null ||
                !conversation.allowedWeathers.Contains(timeManager.CurrentWeather))
            {
                return false;
            }
        }

        return true;
    }

    private bool CanShowConversation(ConversationData conversation)
    {
        if (conversation == null)
        {
            return false;
        }

        if (!conversation.showOnce)
        {
            return true;
        }

        if (string.IsNullOrEmpty(conversation.conversationId))
        {
            Debug.LogWarning("Show Once が true ですが、Conversation Id が空です: " + conversation.name);
            return true;
        }

        return !shownConversationIds.Contains(conversation.conversationId);
    }

    private ConversationData SelectConversationByPriority(List<ConversationData> candidates)
    {
        if (candidates == null || candidates.Count == 0)
        {
            return null;
        }

        int highestPriority = candidates[0].priority;

        for (int i = 1; i < candidates.Count; i++)
        {
            if (candidates[i].priority > highestPriority)
            {
                highestPriority = candidates[i].priority;
            }
        }

        List<ConversationData> highestCandidates = candidates.FindAll(
            conversation => conversation.priority == highestPriority
        );

        return highestCandidates[UnityEngine.Random.Range(0, highestCandidates.Count)];
    }

    private void RegisterShownConversation(ConversationData conversation)
    {
        if (conversation == null)
        {
            return;
        }

        if (!conversation.showOnce)
        {
            return;
        }

        if (string.IsNullOrEmpty(conversation.conversationId))
        {
            Debug.LogWarning("Conversation Id が空なので、表示済みに記録できません: " + conversation.name);
            return;
        }

        shownConversationIds.Add(conversation.conversationId);
    }

    private void OnClickNext()
    {
        if (isFading)
        {
            return;
        }

        if (flowState == ConversationFlowState.ShowingQuestion)
        {
            ShowChoices();
            return;
        }

        if (flowState == ConversationFlowState.ShowingSimple)
        {
            FinishSimpleConversation();
            return;
        }

        if (flowState == ConversationFlowState.ShowingResponse)
        {
            FinishChoiceConversation();
            return;
        }

        if (flowState == ConversationFlowState.ShowingActionResult)
        {
            FinishActionResult();
            return;
        }

        if (flowState == ConversationFlowState.ShowingGoodNight)
        {
            StartCoroutine(FadeToBlackAndNextMorning());
            return;
        }
    }

    private void FinishActionResult()
    {
        if (pendingGoodNight)
        {
            pendingGoodNight = false;

            speakerNameText.text = heroineStatus.HeroineName;
            dialogueText.text = "もう夜も遅いですね。おやすみなさい。また明日。";

            flowState = ConversationFlowState.ShowingGoodNight;

            actionButtonArea.SetActive(false);
            genreButtonArea.SetActive(false);
            choiceButtonArea.SetActive(false);
            outfitPanel.SetActive(false);
            outfitReactionPanel.SetActive(false);

            nextButton.gameObject.SetActive(true);
            return;
        }

        pendingAdvanceTime = false;

        flowState = ConversationFlowState.Idle;

        nextButton.gameObject.SetActive(false);
        choiceButtonArea.SetActive(false);
        genreButtonArea.SetActive(false);
        outfitPanel.SetActive(false);
        outfitReactionPanel.SetActive(false);
        actionButtonArea.SetActive(true);

        speakerNameText.text = heroineStatus.HeroineName;
        dialogueText.text = "次は何をしましょうか？";
    }

    private void ShowChoices()
    {
        if (currentConversation == null)
        {
            FinishConversationWithoutTimeAdvance();
            return;
        }

        if (currentConversation.choices == null || currentConversation.choices.Count == 0)
        {
            speakerNameText.text = "システム";
            dialogueText.text = "選択肢タイプですが、Choices が設定されていません。";

            flowState = ConversationFlowState.ShowingSimple;
            nextButton.gameObject.SetActive(true);
            return;
        }

        nextButton.gameObject.SetActive(false);
        choiceButtonArea.SetActive(true);

        SetupChoiceButton(choiceButton1, 0);
        SetupChoiceButton(choiceButton2, 1);
        SetupChoiceButton(choiceButton3, 2);

        flowState = ConversationFlowState.WaitingChoice;
    }

    private void SetupChoiceButton(Button button, int choiceIndex)
    {
        if (choiceIndex >= currentConversation.choices.Count)
        {
            button.gameObject.SetActive(false);
            return;
        }

        button.gameObject.SetActive(true);

        ConversationChoice choice = currentConversation.choices[choiceIndex];

        TMP_Text buttonText = button.GetComponentInChildren<TMP_Text>();
        if (buttonText != null)
        {
            buttonText.text = choice.choiceText;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => SelectChoice(choice));
    }

    private void SelectChoice(ConversationChoice choice)
    {
        choiceButtonArea.SetActive(false);

        speakerNameText.text = heroineStatus.HeroineName;
        dialogueText.text = choice.responseText;

        heroineStatus.AddAffection(choice.affectionChange);

        RefreshUI();

        flowState = ConversationFlowState.ShowingResponse;
        nextButton.gameObject.SetActive(true);
    }

    private void FinishSimpleConversation()
    {
        heroineStatus.AddAffection(2);

        if (IsNightBeforeAdvance())
        {
            RefreshUI();
            ShowGoodNightBeforeNextDay();
            return;
        }

        timeManager.AdvanceTime();
        RefreshUI();

        FinishConversationWithoutTimeAdvance();
    }

    private void FinishChoiceConversation()
    {
        if (IsNightBeforeAdvance())
        {
            RefreshUI();
            ShowGoodNightBeforeNextDay();
            return;
        }

        timeManager.AdvanceTime();
        RefreshUI();

        FinishConversationWithoutTimeAdvance();
    }

    private void FinishConversationWithoutTimeAdvance()
    {
        currentConversation = null;

        flowState = ConversationFlowState.Idle;

        choiceButtonArea.SetActive(false);
        nextButton.gameObject.SetActive(false);
        genreButtonArea.SetActive(false);
        outfitPanel.SetActive(false);
        outfitReactionPanel.SetActive(false);
        actionButtonArea.SetActive(true);

        speakerNameText.text = heroineStatus.HeroineName;
        dialogueText.text = "次は何をしましょうか？";
    }

    private void OnClickEnding()
    {
        actionButtonArea.SetActive(false);
        genreButtonArea.SetActive(false);
        choiceButtonArea.SetActive(false);
        outfitPanel.SetActive(false);
        outfitReactionPanel.SetActive(false);
        nextButton.gameObject.SetActive(false);

        currentConversation = null;
        flowState = ConversationFlowState.Idle;

        speakerNameText.text = heroineStatus.HeroineName;
        dialogueText.text = "好感度MAXエンドです。あなたと過ごした日々を、私は忘れません。";
    }

    public void RefreshUI()
    {
        dayText.text = "Day " + timeManager.Day;
        timeText.text = timeManager.GetTimeSlotDisplayName();
        weekdayText.text = timeManager.GetWeekdayDisplayName();
        seasonText.text = timeManager.GetSeasonDisplayName();
        weatherText.text = timeManager.GetWeatherDisplayName();

        affectionText.text = "好感度：" + heroineStatus.Affection;
        affectionRankText.text = heroineStatus.GetAffectionRankName();

        endingButton.gameObject.SetActive(heroineStatus.CanEnding());

        UpdateScheduleStatusUI();

        UpdateBackgroundByTime();
    }

    public void SaveGame()
    {
        SaveData saveData = new SaveData();

        saveData.day = timeManager.Day;
        saveData.currentTimeSlot = timeManager.CurrentTimeSlot;
        saveData.currentWeekday = timeManager.CurrentWeekday;
        saveData.currentSeason = timeManager.CurrentSeason;
        saveData.currentWeather = timeManager.CurrentWeather;

        saveData.affection = heroineStatus.Affection;

        if (outfitManager.CurrentOutfit != null)
        {
            saveData.currentOutfitId = outfitManager.CurrentOutfit.outfitId;
        }
        else
        {
            saveData.currentOutfitId = "";
        }

        saveData.outfitPreferences = outfitPreferenceManager.CreateSaveData();

        saveData.shownConversationIds = new List<string>(shownConversationIds);

        saveData.todaySchedule = scheduleManager.TodaySchedule;
        saveData.tomorrowSchedule = scheduleManager.TomorrowSchedule;

        saveManager.Save(saveData);

        speakerNameText.text = "システム";
        dialogueText.text = "セーブしました。";
    }

    public void LoadGame()
    {
        SaveData saveData = saveManager.Load();

        if (saveData == null)
        {
            speakerNameText.text = "システム";
            dialogueText.text = "セーブデータがありません。";
            return;
        }

        timeManager.SetTimeState(
            saveData.day,
            saveData.currentTimeSlot,
            saveData.currentWeekday,
            saveData.currentSeason,
            saveData.currentWeather
        );

        heroineStatus.SetAffection(saveData.affection);

        outfitPreferenceManager.SetPreferences(saveData.outfitPreferences);

        if (!string.IsNullOrEmpty(saveData.currentOutfitId))
        {
            string outfitMessage;
            outfitManager.TryChangeOutfitById(saveData.currentOutfitId, out outfitMessage);
        }
        else
        {
            outfitManager.WearDefaultOutfit();
        }

        shownConversationIds.Clear();

        if (saveData.shownConversationIds != null)
        {
            foreach (string conversationId in saveData.shownConversationIds)
            {
                shownConversationIds.Add(conversationId);
            }
        }

        scheduleManager.SetScheduleState(saveData.todaySchedule, saveData.tomorrowSchedule);

        choiceButtonArea.SetActive(false);
        nextButton.gameObject.SetActive(false);
        genreButtonArea.SetActive(false);
        outfitPanel.SetActive(false);
        outfitReactionPanel.SetActive(false);
        actionButtonArea.SetActive(true);

        currentConversation = null;
        flowState = ConversationFlowState.Idle;

        speakerNameText.text = "システム";
        dialogueText.text = "ロードしました。";

        RefreshUI();
    }

    private void ExecuteSimpleAction(
        string speakerName,
        string message,
        int affectionChange,
        bool advanceTime,
        string actionId)
    {
        actionButtonArea.SetActive(false);
        genreButtonArea.SetActive(false);
        choiceButtonArea.SetActive(false);
        outfitPanel.SetActive(false);
        outfitReactionPanel.SetActive(false);

        speakerNameText.text = speakerName;
        dialogueText.text = message;

        heroineStatus.AddAffection(affectionChange);

        pendingAdvanceTime = advanceTime;
        pendingGoodNight = advanceTime && ShouldShowGoodNightBeforeAdvance(actionId);

        if (advanceTime && !pendingGoodNight)
        {
            timeManager.AdvanceTime();
            pendingAdvanceTime = false;
        }

        RefreshUI();

        flowState = ConversationFlowState.ShowingActionResult;
        nextButton.gameObject.SetActive(true);
    }

    private void ShowSystemMessage(string message)
    {
        actionButtonArea.SetActive(false);
        genreButtonArea.SetActive(false);
        choiceButtonArea.SetActive(false);
        outfitPanel.SetActive(false);
        outfitReactionPanel.SetActive(false);

        speakerNameText.text = "システム";
        dialogueText.text = message;

        flowState = ConversationFlowState.ShowingActionResult;
        nextButton.gameObject.SetActive(true);
    }

    private void LoadActionsFromResources()
    {
        ActionData[] loadedActions =
            Resources.LoadAll<ActionData>(actionResourcePath);

        actions = new List<ActionData>(loadedActions);

        actions.Sort((a, b) => a.sortOrder.CompareTo(b.sortOrder));

        Debug.Log("Loaded Actions: " + actions.Count);

        foreach (ActionData action in actions)
        {
            Debug.Log(
                "Action: " +
                action.name +
                " / Id: " +
                action.actionId +
                " / Display: " +
                action.displayName +
                " / Type: " +
                action.executionType
            );
        }
    }

    private void CreateActionButtons()
    {
        if (actionButtonParent == null)
        {
            Debug.LogError("Action Button Parent が設定されていません。");
            return;
        }

        if (actionButtonPrefab == null)
        {
            Debug.LogError("Action Button Prefab が設定されていません。");
            return;
        }

        ClearActionButtons();

        foreach (ActionData action in actions)
        {
            if (action == null)
            {
                continue;
            }

            if (!action.isEnabled)
            {
                continue;
            }

            Button button = Instantiate(actionButtonPrefab, actionButtonParent);

            TMP_Text buttonText = button.GetComponentInChildren<TMP_Text>();
            if (buttonText != null)
            {
                buttonText.text = action.displayName;
            }

            ActionData capturedAction = action;
            button.onClick.AddListener(() => ExecuteAction(capturedAction));
        }
    }

    private void ClearActionButtons()
    {
        for (int i = actionButtonParent.childCount - 1; i >= 0; i--)
        {
            Destroy(actionButtonParent.GetChild(i).gameObject);
        }
    }

    private void ExecuteAction(ActionData action)
    {
        if (action == null)
        {
            return;
        }

        if (!CanExecuteAction(action))
        {
            ShowSystemMessage(action.unavailableMessage);
            return;
        }

        if (action.executionType == ActionExecutionType.OpenConversationGenres)
        {
            OpenConversationGenres(action);
            return;
        }

        if (action.executionType == ActionExecutionType.OpenOutfitPanel)
        {
            OpenOutfitPanel(action);
            return;
        }

        if (action.executionType == ActionExecutionType.OpenOutfitReactionPanel)
        {
            OpenOutfitReactionPanel(action);
            return;
        }

        if (action.executionType == ActionExecutionType.SimpleAction)
        {
            ExecuteActionData(action);
            return;
        }

        if (action.executionType == ActionExecutionType.OpenSchedulePanel)
        {
            OpenSchedulePanel();
            return;
        }
    }

    private void OpenOutfitPanel(ActionData action)
    {
        actionButtonArea.SetActive(false);
        genreButtonArea.SetActive(false);
        choiceButtonArea.SetActive(false);
        outfitPanel.SetActive(true);
        outfitReactionPanel.SetActive(false);

        speakerNameText.text = "システム";

        if (string.IsNullOrEmpty(action.resultMessage))
        {
            dialogueText.text = "着替える衣装を選んでください。";
        }
        else
        {
            dialogueText.text = action.resultMessage;
        }

        flowState = ConversationFlowState.Idle;
    }

    private bool CanExecuteAction(ActionData action)
    {
        if (action == null)
        {
            return false;
        }

        if (!action.isEnabled)
        {
            return false;
        }

        if (heroineStatus.Affection < action.minAffection)
        {
            return false;
        }

        if (heroineStatus.Affection > action.maxAffection)
        {
            return false;
        }

        if (!action.anyTimeSlot)
        {
            if (action.allowedTimeSlots == null ||
                !action.allowedTimeSlots.Contains(timeManager.CurrentTimeSlot))
            {
                return false;
            }
        }

        if (!action.anyWeather)
        {
            if (action.allowedWeathers == null ||
                !action.allowedWeathers.Contains(timeManager.CurrentWeather))
            {
                return false;
            }
        }

        if (!action.anySeason)
        {
            if (action.allowedSeasons == null ||
                !action.allowedSeasons.Contains(timeManager.CurrentSeason))
            {
                return false;
            }
        }

        return true;
    }

    private void OpenConversationGenres(ActionData action)
    {
        actionButtonArea.SetActive(false);
        genreButtonArea.SetActive(true);
        choiceButtonArea.SetActive(false);
        outfitPanel.SetActive(false);
        outfitReactionPanel.SetActive(false);

        speakerNameText.text = "システム";

        if (string.IsNullOrEmpty(action.resultMessage))
        {
            dialogueText.text = "話したい話題を選んでください。";
        }
        else
        {
            dialogueText.text = action.resultMessage;
        }

        flowState = ConversationFlowState.Idle;
    }

    private void ExecuteActionData(ActionData action)
    {
        ActionReactionData reaction = SelectActionReaction(action);

        if (reaction != null)
        {
            string reactionSpeakerName = reaction.useHeroineNameAsSpeaker
                ? heroineStatus.HeroineName
                : "システム";

            ExecuteSimpleAction(
                reactionSpeakerName,
                action.resultMessage,
                action.affectionChange,
                action.advanceTime,
                action.actionId
            );

            return;
        }

        string defaultSpeakerName = action.useHeroineNameAsSpeaker
            ? heroineStatus.HeroineName
            : "システム";

        ExecuteSimpleAction(
            defaultSpeakerName,
            action.resultMessage,
            action.affectionChange,
            action.advanceTime,
            action.actionId
        );
    }

    private ActionReactionData SelectActionReaction(ActionData action)
    {
        if (action == null || action.reactions == null || action.reactions.Count == 0)
        {
            return null;
        }

        List<ActionReactionData> candidates = new List<ActionReactionData>();

        foreach (ActionReactionData reaction in action.reactions)
        {
            if (IsActionReactionAvailable(reaction))
            {
                candidates.Add(reaction);
            }
        }

        if (candidates.Count == 0)
        {
            return null;
        }

        int highestPriority = candidates[0].priority;

        for (int i = 1; i < candidates.Count; i++)
        {
            if (candidates[i].priority > highestPriority)
            {
                highestPriority = candidates[i].priority;
            }
        }

        List<ActionReactionData> highestCandidates = candidates.FindAll(
            reaction => reaction.priority == highestPriority
        );

        return highestCandidates[UnityEngine.Random.Range(0, highestCandidates.Count)];
    }

    private bool IsActionReactionAvailable(ActionReactionData reaction)
    {
        if (reaction == null)
        {
            return false;
        }

        if (heroineStatus.Affection < reaction.minAffection)
        {
            return false;
        }

        if (heroineStatus.Affection > reaction.maxAffection)
        {
            return false;
        }

        if (!reaction.anyTimeSlot)
        {
            if (reaction.allowedTimeSlots == null ||
                !reaction.allowedTimeSlots.Contains(timeManager.CurrentTimeSlot))
            {
                return false;
            }
        }

        if (!reaction.anyWeather)
        {
            if (reaction.allowedWeathers == null ||
                !reaction.allowedWeathers.Contains(timeManager.CurrentWeather))
            {
                return false;
            }
        }

        if (!reaction.anySeason)
        {
            if (reaction.allowedSeasons == null ||
                !reaction.allowedSeasons.Contains(timeManager.CurrentSeason))
            {
                return false;
            }
        }

        return true;
    }

    private void CreateOutfitButtons()
    {
        if (outfitManager == null)
        {
            Debug.LogError("Outfit Manager が設定されていません。");
            return;
        }

        if (outfitButtonParent == null)
        {
            Debug.LogError("Outfit Button Parent が設定されていません。");
            return;
        }

        if (outfitButtonPrefab == null)
        {
            Debug.LogError("Outfit Button Prefab が設定されていません。");
            return;
        }

        ClearOutfitButtons();

        foreach (OutfitData outfit in outfitManager.Outfits)
        {
            if (outfit == null)
            {
                continue;
            }

            if (!outfit.isEnabled)
            {
                continue;
            }

            Button button = Instantiate(outfitButtonPrefab, outfitButtonParent);

            TMP_Text buttonText = button.GetComponentInChildren<TMP_Text>();
            if (buttonText != null)
            {
                buttonText.text = outfit.displayName;
            }

            OutfitData capturedOutfit = outfit;
            button.onClick.AddListener(() => OnClickOutfitButton(capturedOutfit));
        }
    }

    private void ClearOutfitButtons()
    {
        for (int i = outfitButtonParent.childCount - 1; i >= 0; i--)
        {
            Destroy(outfitButtonParent.GetChild(i).gameObject);
        }
    }

    private void OnClickOutfitButton(OutfitData outfit)
    {
        string message;
        bool success = outfitManager.TryChangeOutfit(outfit, out message);

        actionButtonArea.SetActive(false);
        genreButtonArea.SetActive(false);
        choiceButtonArea.SetActive(false);
        outfitPanel.SetActive(false);

        speakerNameText.text = heroineStatus.HeroineName;
        dialogueText.text = message;

        flowState = ConversationFlowState.ShowingActionResult;
        nextButton.gameObject.SetActive(true);

        RefreshUI();
    }

    private void OpenOutfitReactionPanel(ActionData action)
    {
        actionButtonArea.SetActive(false);
        genreButtonArea.SetActive(false);
        choiceButtonArea.SetActive(false);
        outfitPanel.SetActive(false);
        outfitReactionPanel.SetActive(true);
        nextButton.gameObject.SetActive(false);

        speakerNameText.text = "システム";

        if (outfitManager.CurrentOutfit == null)
        {
            dialogueText.text = "まだ衣装を着ていません。";
        }
        else if (string.IsNullOrEmpty(action.resultMessage))
        {
            dialogueText.text = "今の衣装について、どう反応しますか？";
        }
        else
        {
            dialogueText.text = action.resultMessage + "\n現在の衣装：" + outfitManager.CurrentOutfit.displayName;
        }

        flowState = ConversationFlowState.Idle;
    }

    private void OnClickOutfitReaction(OutfitReactionType reactionType)
    {
        if (reactionType == OutfitReactionType.Change)
        {
            OpenOutfitPanelForChange();
            return;
        }

        OutfitData currentOutfit = outfitManager.CurrentOutfit;

        string message = outfitPreferenceManager.ApplyReaction(
            currentOutfit,
            reactionType,
            heroineStatus
        );

        actionButtonArea.SetActive(false);
        genreButtonArea.SetActive(false);
        choiceButtonArea.SetActive(false);
        outfitPanel.SetActive(false);
        outfitReactionPanel.SetActive(false);

        speakerNameText.text = heroineStatus.HeroineName;
        dialogueText.text = message;

        RefreshUI();

        flowState = ConversationFlowState.ShowingActionResult;
        nextButton.gameObject.SetActive(true);
    }

    private void OpenOutfitPanelForChange()
    {
        actionButtonArea.SetActive(false);
        genreButtonArea.SetActive(false);
        choiceButtonArea.SetActive(false);
        outfitReactionPanel.SetActive(false);
        outfitPanel.SetActive(true);
        nextButton.gameObject.SetActive(false);

        speakerNameText.text = "システム";
        dialogueText.text = "変更する衣装を選んでください。";

        flowState = ConversationFlowState.Idle;
    }

    private void OnDayChanged()
    {
        AutoChooseOutfitOnNewDay();
    }

    private void AutoChooseOutfitOnNewDay()
    {
        if (outfitManager == null)
        {
            return;
        }

        string message;
        bool success = outfitManager.AutoChooseOutfitForToday(out message);

        if (!success)
        {
            return;
        }

        speakerNameText.text = heroineStatus.HeroineName;
        dialogueText.text = message;

        RefreshUI();
    }

    private void OnDestroy()
    {
        if (timeManager != null)
        {
            timeManager.OnDayChanged -= OnDayChanged;
        }
    }

    private void SetFadeAlpha(float alpha)
    {
        if (fadeImage == null)
        {
            return;
        }

        Color color = fadeImage.color;
        color.a = alpha;
        fadeImage.color = color;
    }

    private IEnumerator FadeToBlackAndNextMorning()
    {
        isFading = true;

        actionButtonArea.SetActive(false);
        genreButtonArea.SetActive(false);
        choiceButtonArea.SetActive(false);
        outfitPanel.SetActive(false);
        outfitReactionPanel.SetActive(false);
        nextButton.gameObject.SetActive(false);

        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Clamp01(timer / fadeDuration);
            SetFadeAlpha(alpha);
            yield return null;
        }

        SetFadeAlpha(1f);

        // ここで夜 → 翌朝へ進む
        timeManager.AdvanceTime();
        RefreshUI();

        yield return new WaitForSeconds(0.4f);

        timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float alpha = 1f - Mathf.Clamp01(timer / fadeDuration);
            SetFadeAlpha(alpha);
            yield return null;
        }

        SetFadeAlpha(0f);

        speakerNameText.text = heroineStatus.HeroineName;
        dialogueText.text = "おはようございます。今日もよろしくお願いしますね。";

        flowState = ConversationFlowState.Idle;

        actionButtonArea.SetActive(true);
        genreButtonArea.SetActive(false);
        choiceButtonArea.SetActive(false);
        outfitPanel.SetActive(false);
        outfitReactionPanel.SetActive(false);
        nextButton.gameObject.SetActive(false);

        isFading = false;
    }

    private bool ShouldShowGoodNightBeforeAdvance(string actionId)
    {
        if (timeManager.CurrentTimeSlot != TimeSlot.Night)
        {
            return false;
        }

        // 夜に「休む」を選んだ場合は、すでに休む行動そのものなので、おやすみ会話を省略する
        //if (actionId == "Rest")
        //{
        //    return false;
        //}

        return true;
    }

    private bool IsNightBeforeAdvance()
    {
        return timeManager.CurrentTimeSlot == TimeSlot.Night ||
               timeManager.CurrentTimeSlot == TimeSlot.LateNight;
    }

    private void ShowGoodNightBeforeNextDay()
    {
        speakerNameText.text = heroineStatus.HeroineName;
        dialogueText.text = "もう夜も遅いですね。おやすみなさい。また明日。";

        flowState = ConversationFlowState.ShowingGoodNight;

        actionButtonArea.SetActive(false);
        genreButtonArea.SetActive(false);
        choiceButtonArea.SetActive(false);
        outfitPanel.SetActive(false);
        outfitReactionPanel.SetActive(false);

        nextButton.gameObject.SetActive(true);
    }

    private void UpdateScheduleStatusUI()
    {
        if (scheduleManager == null)
        {
            return;
        }

        if (todayScheduleText != null)
        {
            todayScheduleText.text =
                "今日の予定：" + scheduleManager.GetTodayScheduleDisplayName();
        }

        if (tomorrowScheduleText != null)
        {
            tomorrowScheduleText.text =
                "明日の予定：" + scheduleManager.GetTomorrowScheduleDisplayName();
        }
    }

    public void OpenSchedulePanel()
    {
        if (schedulePanel == null)
        {
            Debug.LogWarning("SchedulePanel が設定されていません。");
            return;
        }

        schedulePanel.SetActive(true);
    }


}