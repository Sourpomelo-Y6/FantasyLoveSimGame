using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class GameManager : MonoBehaviour
{
    private const int MaxTrainingProficiency = 999999;
    private const int DefaultPlayerBattleSkillSlotCount = 4;
    private const int DefaultHeroineBattleSkillSlotCount = 3;
    private const string SkillTreeNodeResourcePath = "SkillTreeNodes";
    private const string CommonScheduledEventResourcePath = "ScheduledEvents";
    private const string BattleResultEventResourcePath = "BattleResultEvents";
    private const string BattlePanelResultMessageResourcePath = "BattlePanelResultMessages";

    private enum ConversationFlowState
    {
        Idle,
        ShowingQuestion,
        WaitingChoice,
        ShowingResponse,
        ShowingSimple,
        ShowingActionResult,
        ShowingBattleIntro,
        ShowingGoodNight
    }

    private enum DialogueSpeakerType
    {
        Heroine,
        Player,
        System,
        Schedule,
        Outfit,
        BattleLog
    }

    private struct DialogueMessage
    {
        public readonly DialogueSpeakerType SpeakerType;
        public readonly string SpeakerName;
        public readonly string Message;
        public readonly string StillId;
        public readonly Sprite StillSprite;
        public readonly string ExpressionId;
        public readonly BattleResultVisualMode? BattleResultVisualMode;

        public DialogueMessage(DialogueSpeakerType speakerType, string speakerName, string message)
            : this(speakerType, speakerName, message, null)
        {
        }

        public DialogueMessage(
            DialogueSpeakerType speakerType,
            string speakerName,
            string message,
            Sprite stillSprite)
            : this(speakerType, speakerName, message, "", stillSprite)
        {
        }

        public DialogueMessage(
            DialogueSpeakerType speakerType,
            string speakerName,
            string message,
            string stillId,
            Sprite stillSprite)
            : this(speakerType, speakerName, message, stillId, stillSprite, "")
        {
        }

        public DialogueMessage(
            DialogueSpeakerType speakerType,
            string speakerName,
            string message,
            string stillId,
            Sprite stillSprite,
            string expressionId,
            BattleResultVisualMode? battleResultVisualMode = null)
        {
            SpeakerType = speakerType;
            SpeakerName = speakerName;
            Message = message;
            StillId = stillId;
            StillSprite = stillSprite;
            ExpressionId = expressionId;
            BattleResultVisualMode = battleResultVisualMode;
        }
    }

    private struct SimpleBattleResult
    {
        public bool PlayerWon;
        public bool PlayerEscaped;
        public int Turns;
        public int PlayerDamageTaken;
        public int HeroineDamageTaken;
        public int RewardMoney;
        public int AffectionChange;
        public int PlayerSkillPointReward;
        public int HeroineSkillPointReward;
        public int TotalPlayerSkillPoints;
        public int TotalHeroineSkillPoints;
        public string Message;
        public List<string> LogLines;
    }

    public struct StillGalleryItem
    {
        public readonly string StillId;
        public readonly Sprite Sprite;

        public StillGalleryItem(string stillId, Sprite sprite)
        {
            StillId = stillId;
            Sprite = sprite;
        }
    }

    [Header("Managers")]
    [SerializeField] private TimeManager timeManager;
    [SerializeField] private PlayerStatus playerStatus;
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
    [SerializeField] private TMP_Text mainPlayerHpText;
    [SerializeField] private TMP_Text mainHeroineHpText;
    [SerializeField] private TMP_Text mainPlayerMoneyText;

    [Header("Schedule UI")]
    [SerializeField] private TextMeshProUGUI todayScheduleText;
    [SerializeField] private TextMeshProUGUI tomorrowScheduleText;
    [SerializeField] private GameObject schedulePanel;

    [Header("Dialogue")]
    [SerializeField] private TMP_Text speakerNameText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private Color heroineSpeakerColor = Color.white;
    [SerializeField] private Color heroineDialogueColor = Color.white;
    [SerializeField] private Color playerSpeakerColor = new Color32(255, 230, 160, 255);
    [SerializeField] private Color playerDialogueColor = Color.white;
    [SerializeField] private Color systemSpeakerColor = new Color32(210, 210, 210, 255);
    [SerializeField] private Color systemDialogueColor = new Color32(210, 210, 210, 255);
    [SerializeField] private Color scheduleSpeakerColor = new Color32(120, 180, 255, 255);
    [SerializeField] private Color scheduleDialogueColor = Color.white;
    [SerializeField] private Color outfitSpeakerColor = new Color32(130, 220, 160, 255);
    [SerializeField] private Color outfitDialogueColor = Color.white;
    [SerializeField] private Color battleLogSpeakerColor = new Color32(255, 170, 120, 255);
    [SerializeField] private Color battleLogDialogueColor = new Color32(235, 235, 235, 255);

    [Header("Fade")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 1.5f;

    [Header("Action Buttons")]
    [SerializeField] private GameObject actionButtonArea;
    [SerializeField] private GameObject actionButtonAreaColumnLeft;
    [SerializeField] private GameObject actionButtonAreaColumnCenter;
    [SerializeField] private GameObject actionButtonAreaColumnRight;

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
    [SerializeField] private DialogueClickAdvanceArea dialogueClickAdvanceArea;
    [SerializeField] private bool enableDialogueWindowClickAdvance = true;

    [Header("Heroine Profile")]
    [SerializeField] private HeroineProfileData heroineProfile;
    [SerializeField] private string defaultHeroineProfileResourcePath = "Heroines/DefaultHeroineProfile";

    [Header("Ending")]
    [SerializeField] private Button endingButton;
    [SerializeField] private string endingSceneName = "EndingScene";
    [SerializeField] private string defaultEndingId = "GoodEnding";

    [Header("Action Data")]
    [SerializeField] private string actionResourcePath = "Actions";

    private List<ActionData> actions = new List<ActionData>();

    [Header("Game Event Data")]
    [SerializeField] private string gameEventResourcePath = "GameEvents";

    private List<GameEventData> gameEvents = new List<GameEventData>();

    [Header("Scheduled Event Data")]
    [SerializeField] private string scheduledEventResourcePath = "ScheduledEvents";

    [Header("Outfit Prompt Ability")]
    [SerializeField] private OutfitPromptAbilitySet playerOutfitPromptAbilities = new OutfitPromptAbilitySet();

    private List<ScheduledEventData> scheduledEvents = new List<ScheduledEventData>();

    [Header("Conversation Data")]
    [SerializeField] private string conversationResourcePath = "Conversations";

    private List<ConversationData> conversations = new List<ConversationData>();

    private ConversationFlowState flowState = ConversationFlowState.Idle;

    private ConversationData currentConversation;

    private bool pendingAdvanceTime = false;
    private bool pendingGoodNight = false;
    private bool isFading = false;
    private List<DialogueMessage> dayStartMessages = new List<DialogueMessage>();
    private ScheduledEventDefinition pendingScheduledEvent;
    private bool startPendingScheduledEventAfterOutfitMessage = false;
    private bool returnToScheduledEventPromptAfterOutfitMessage = false;
    private string currentHeroineId = "";
    private HeroineAssetCatalog heroineAssetCatalog;
    private Image dialogueSequenceStillImageTarget;
    private Sprite dialogueSequencePreviousStillSprite;
    private bool dialogueSequenceHasStillSpriteOverride = false;
    private bool dialogueSequencePreviousStillPreserveAspect = false;
    private bool dialogueSequenceHasStillPreserveAspectOverride = false;
    private bool dialogueSequencePreviousStillImageActive = false;
    private bool dialogueSequenceHasStillImageActiveOverride = false;
    private bool dialogueSequenceIsActive = false;
    private bool dialogueSequenceHidHeroineImage = false;
    private bool dialogueSequenceShowedPortraitBackdrop = false;
    private bool dialogueSequenceHidSaveLoadButtons = false;
    private bool dialogueSequenceHasBackgroundZoomOverride = false;
    private Vector3 dialogueSequencePreviousBackgroundZoomScale;
    private Vector2 dialogueSequencePreviousBackgroundZoomPosition;
    private Sprite blankStillSprite;
    private Texture2D pendingSaveThumbnail;

    private const int SaveThumbnailWidth = 320;
    private const int SaveThumbnailHeight = 180;

    private const string SystemSpeakerName = "SYSTEM";
    private const string PlayerSpeakerName = "主人公";
    private const string ScheduleSpeakerName = "予定";
    private const string OutfitSpeakerName = "衣装";
    private const string BattleLogSpeakerName = "戦闘ログ";

    public OutfitPromptAbilitySet PlayerOutfitPromptAbilities => playerOutfitPromptAbilities;
    public PlayerStatus PlayerStatus => playerStatus;
    public string CurrentHeroineId => currentHeroineId;
    public HeroineProfileData CurrentHeroineProfile => heroineProfile;
    public HeroineAssetCatalog CurrentHeroineAssetCatalog => heroineAssetCatalog;

    private readonly HashSet<string> shownConversationIds = new HashSet<string>();
    private readonly HashSet<string> shownGameEventIds = new HashSet<string>();
    private readonly HashSet<string> unlockedStatusAbilityIds = new HashSet<string>();
    private readonly HashSet<string> unlockedSkillIds = new HashSet<string>();
    private readonly List<string> equippedPlayerBattleSkillIds = new List<string>();
    private readonly Dictionary<string, List<string>> heroineBattleSkillLoadouts =
        new Dictionary<string, List<string>>(StringComparer.Ordinal);
    private readonly HashSet<string> activePlayerTrainingSkillIds =
        new HashSet<string>(StringComparer.Ordinal);
    private readonly Dictionary<string, HashSet<string>> activeHeroineTrainingSkillIds =
        new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
    private readonly HashSet<string> acquiredPlayerSkillTreeNodeIds = new HashSet<string>();
    private readonly HashSet<string> acquiredHeroineSkillTreeNodeIds = new HashSet<string>();
    private readonly HashSet<string> unlockedStillIds = new HashSet<string>();
    private readonly HashSet<string> purchasedItemIds = new HashSet<string>();
    private readonly Dictionary<string, int> itemQuantities = new Dictionary<string, int>();
    private readonly HashSet<string> unlockedOutfitIds = new HashSet<string>();
    private readonly Dictionary<string, int> trainingProficiencies = new Dictionary<string, int>();
    private readonly SkillProgressStats skillProgressStats = new SkillProgressStats();
    private int playerSkillPoints;
    private int heroineSkillPoints;
    private readonly Queue<DialogueMessage> queuedDialogueMessages = new Queue<DialogueMessage>();
    private readonly List<DialogueMessage> pendingScheduledEventFollowUpMessages = new List<DialogueMessage>();
    private readonly List<MessageLogPanel.MessageLogEntry> messageLogEntries =
        new List<MessageLogPanel.MessageLogEntry>();

    [Header("Save / Load Buttons")]
    [SerializeField] private Button saveButton;
    [SerializeField] private Button loadButton;
    [SerializeField] private Color saveLoadDisabledColor = new Color32(120, 120, 120, 180);
    [SerializeField] private Color saveLoadDisabledTextColor = new Color32(180, 180, 180, 255);
    private Color saveButtonTextColor = Color.white;
    private Color loadButtonTextColor = Color.white;
    private bool saveButtonTextColorCached;
    private bool loadButtonTextColorCached;


    [Header("Background")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private BackgroundSpriteData backgroundSpriteData;
    [SerializeField] private Sprite dayBackgroundSprite;
    [SerializeField] private Sprite nightBackgroundSprite;
    [SerializeField] private BackgroundZoom backgroundZoom;

    [Header("Event Still")]
    [SerializeField] private Image eventStillImage;

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

    [Header("Status Detail")]
    [SerializeField] private StatusDetailPanel statusDetailPanel;

    [Header("Still Gallery")]
    [SerializeField] private StillGalleryPanel stillGalleryPanel;

    [Header("Message Log")]
    [SerializeField] private MessageLogPanel messageLogPanel;
    [SerializeField] private int messageLogLimit = 20;

    [Header("Battle UI")]
    [SerializeField] private BattlePanel battlePanel;
    [FormerlySerializedAs("useBattlePanelForDuoForestExploration")]
    [SerializeField] private bool useBattlePanelForExploration = true;
    private BattlePanel.BattleResult lastBattlePanelResult;
    private SimpleBattleResult lastBattlePanelSimpleResult;
    private bool hasLastBattlePanelSimpleResult;
    private ScheduledEventDefinition pendingBattlePanelScheduledEvent;
    private bool waitingForBattlePanelScheduledEvent;
    public BattlePanel.BattleResult LastBattlePanelResult => lastBattlePanelResult;

    [Header("Training UI")]
    [SerializeField] private TrainingPanel trainingPanel;
    [SerializeField] private string trainingResourcePath = "Training";
    private TrainingData[] trainingDataList;

    [Header("Skill UI")]
    [SerializeField] private SkillPanel skillPanel;
    [SerializeField] private SkillTreePanel skillTreePanel;
    [SerializeField] private string skillResourcePath = "Skills";
    private SkillData[] skillDataList;
    private SkillTreeNodeData[] skillTreeNodeDataList;

    [Header("Battle Result Events")]
    [SerializeField] private string battleResultEventResourcePath = BattleResultEventResourcePath;
    [SerializeField] private string battlePanelResultMessageResourcePath = BattlePanelResultMessageResourcePath;
    [SerializeField] private BattleResultEventData[] battleResultEvents;
    private BattleResultEventData[] commonBattleResultEvents;
    private bool battleResultEventsLoadedFromResources = false;
    private BattlePanelResultMessageData[] battlePanelResultMessages;
    private BattlePanelResultMessageData[] commonBattlePanelResultMessages;

    [Header("Game Event Debug")]
    [SerializeField] private string debugManualGameEventId = "";
    [SerializeField] private KeyCode debugManualGameEventKey = KeyCode.F7;

    [Header("Money Debug")]
    [SerializeField] private KeyCode debugAddMoneyKey = KeyCode.F8;
    [SerializeField] private KeyCode debugSpendMoneyKey = KeyCode.F9;
    [SerializeField] private int debugMoneyAmount = 100;

    [Header("Shopping Test")]
    [SerializeField] private ShopCatalogData duoShoppingShopCatalog;
    [SerializeField] private ShopItemData duoShoppingShopItem;
    [SerializeField] private ShopPanel shopPanel;
    [SerializeField] private int duoShoppingTestCost = 100;
    [SerializeField] private string duoShoppingTestItemId = "ShoppingTestItem_01";
    [SerializeField] private string duoShoppingTestItemName = "買い物テスト商品";
    [SerializeField] private List<string> duoShoppingUnlockedOutfitIds =
        new List<string> { "Spring", "Summer", "Autumn", "Winter" };
    private readonly List<string> pendingDuoShoppingResultMessages = new List<string>();


    private void Update()
    {
        RefreshSaveLoadButtonState();

        if (Input.GetKeyDown(debugManualGameEventKey))
        {
            if (TryStartManualGameEvent(debugManualGameEventId))
            {
                return;
            }

            if (!string.IsNullOrEmpty(debugManualGameEventId))
            {
                ShowSystemMessage("手動イベントを開始できませんでした。");
            }
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            OpenSchedulePanel();
        }

        if (Input.GetKeyDown(debugAddMoneyKey))
        {
            AddPlayerMoney(debugMoneyAmount);
        }

        if (Input.GetKeyDown(debugSpendMoneyKey))
        {
            TrySpendPlayerMoney(debugMoneyAmount);
        }
    }

    private void UpdateBackgroundByTime()
    {
        if (backgroundImage == null)
        {
            return;
        }

        Sprite weatherBackground = null;

        if (backgroundSpriteData != null)
        {
            weatherBackground = backgroundSpriteData.FindSprite(
                timeManager.CurrentTimeSlot,
                timeManager.CurrentWeather);
        }

        if (weatherBackground != null)
        {
            backgroundImage.sprite = weatherBackground;
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

    private void ShowDialogue(string speakerName, string message)
    {
        ShowDialogue(GetSpeakerTypeForName(speakerName), speakerName, message);
    }

    private void ShowDialogue(DialogueSpeakerType speakerType, string speakerName, string message)
    {
        ShowDialogue(speakerType, speakerName, message, null);
    }

    private void ShowDialogue(
        DialogueSpeakerType speakerType,
        string speakerName,
        string message,
        Sprite stillSprite)
    {
        ShowDialogue(speakerType, speakerName, message, "", stillSprite);
    }

    private void ShowDialogue(
        DialogueSpeakerType speakerType,
        string speakerName,
        string message,
        string stillId,
        Sprite stillSprite)
    {
        ResetDialogueSequenceState();
        queuedDialogueMessages.Clear();
        dialogueSequenceIsActive = false;
        SetDialogueText(speakerType, speakerName, message, stillId, stillSprite);
    }

    private void SetDialogueText(
        DialogueSpeakerType speakerType,
        string speakerName,
        string message,
        string stillId,
        Sprite stillSprite)
    {
        SetDialogueText(speakerType, speakerName, message, stillId, stillSprite, "");
    }

    private void SetDialogueText(
        DialogueSpeakerType speakerType,
        string speakerName,
        string message,
        string stillId,
        Sprite stillSprite,
        string expressionId,
        BattleResultVisualMode? battleResultVisualMode = null)
    {
        if (!string.IsNullOrEmpty(expressionId))
        {
            ApplyHeroineExpression(expressionId);
        }

        bool useStill = stillSprite != null &&
            (!battleResultVisualMode.HasValue ||
                battleResultVisualMode.Value != BattleResultVisualMode.PortraitOnly);
        if (useStill)
        {
            UnlockStill(stillId);

            Image stillImage = GetDialogueStillImage();
            if (stillImage != null)
            {
                if (!dialogueSequenceHasStillSpriteOverride)
                {
                    dialogueSequenceStillImageTarget = stillImage;
                    dialogueSequencePreviousStillSprite = stillImage.sprite;
                    dialogueSequenceHasStillSpriteOverride = true;
                }

                if (!dialogueSequenceHasStillPreserveAspectOverride)
                {
                    dialogueSequencePreviousStillPreserveAspect = stillImage.preserveAspect;
                    dialogueSequenceHasStillPreserveAspectOverride = true;
                }

                if (!dialogueSequenceHasStillImageActiveOverride)
                {
                    dialogueSequencePreviousStillImageActive = stillImage.gameObject.activeSelf;
                    dialogueSequenceHasStillImageActiveOverride = true;
                }

                stillImage.gameObject.SetActive(true);
                stillImage.sprite = stillSprite;
                stillImage.preserveAspect = true;
            }

            bool hidePortrait = !battleResultVisualMode.HasValue ||
                battleResultVisualMode.Value == BattleResultVisualMode.Auto ||
                battleResultVisualMode.Value == BattleResultVisualMode.StillOnly;
            if (hidePortrait && !dialogueSequenceHidHeroineImage &&
                outfitManager != null &&
                outfitManager.IsHeroineImageVisible())
            {
                outfitManager.SetHeroineImageVisible(false);
                dialogueSequenceHidHeroineImage = true;
            }

            if (backgroundZoom != null)
            {
                if (!dialogueSequenceHasBackgroundZoomOverride)
                {
                    backgroundZoom.CaptureState(
                        out dialogueSequencePreviousBackgroundZoomScale,
                        out dialogueSequencePreviousBackgroundZoomPosition);
                    dialogueSequenceHasBackgroundZoomOverride = true;
                }

                backgroundZoom.ResetZoom();
            }
        }

        if (battleResultVisualMode.HasValue && outfitManager != null)
        {
            bool showPortraitBackdrop = !useStill ||
                battleResultVisualMode.Value == BattleResultVisualMode.StillWithPortrait ||
                battleResultVisualMode.Value == BattleResultVisualMode.PortraitOnly;
            outfitManager.SetDialoguePortraitBackdropVisible(showPortraitBackdrop);
            dialogueSequenceShowedPortraitBackdrop |= showPortraitBackdrop;
        }

        if (speakerNameText != null)
        {
            speakerNameText.text = speakerName;
            speakerNameText.color = GetSpeakerNameColor(speakerType);
        }

        if (dialogueText != null)
        {
            dialogueText.text = speakerNameText == null && !string.IsNullOrEmpty(speakerName)
                ? speakerName + "\n" + message
                : message;
            dialogueText.color = GetDialogueColor(speakerType);
        }

        AddMessageLogEntry(speakerType, speakerName, message);
    }

    private void AddMessageLogEntry(DialogueSpeakerType speakerType, string speakerName, string message)
    {
        if (string.IsNullOrEmpty(speakerName) && string.IsNullOrEmpty(message))
        {
            return;
        }

        messageLogEntries.Add(
            new MessageLogPanel.MessageLogEntry(
                speakerName,
                message,
                GetSpeakerNameColor(speakerType),
                GetDialogueColor(speakerType)));

        int limit = Mathf.Max(1, messageLogLimit);
        while (messageLogEntries.Count > limit)
        {
            messageLogEntries.RemoveAt(0);
        }
    }

    public IReadOnlyList<MessageLogPanel.MessageLogEntry> GetMessageLogEntries()
    {
        return messageLogEntries;
    }

    private void ResetDialogueSequenceState()
    {
        if (dialogueSequenceHasStillSpriteOverride && dialogueSequenceStillImageTarget != null)
        {
            dialogueSequenceStillImageTarget.sprite = dialogueSequencePreviousStillSprite;
        }

        dialogueSequencePreviousStillSprite = null;
        dialogueSequenceHasStillSpriteOverride = false;

        if (dialogueSequenceHasStillPreserveAspectOverride && dialogueSequenceStillImageTarget != null)
        {
            dialogueSequenceStillImageTarget.preserveAspect = dialogueSequencePreviousStillPreserveAspect;
        }

        dialogueSequencePreviousStillPreserveAspect = false;
        dialogueSequenceHasStillPreserveAspectOverride = false;

        if (dialogueSequenceHasStillImageActiveOverride && dialogueSequenceStillImageTarget != null)
        {
            dialogueSequenceStillImageTarget.gameObject.SetActive(dialogueSequencePreviousStillImageActive);
        }

        dialogueSequencePreviousStillImageActive = false;
        dialogueSequenceHasStillImageActiveOverride = false;
        dialogueSequenceStillImageTarget = null;

        if (dialogueSequenceHidHeroineImage && outfitManager != null)
        {
            outfitManager.SetHeroineImageVisible(true);
        }

        dialogueSequenceHidHeroineImage = false;

        if (dialogueSequenceShowedPortraitBackdrop && outfitManager != null)
        {
            outfitManager.SetDialoguePortraitBackdropVisible(false);
        }

        dialogueSequenceShowedPortraitBackdrop = false;

        if (dialogueSequenceHidSaveLoadButtons)
        {
            SetSaveLoadButtonsVisible(true);
        }

        dialogueSequenceHidSaveLoadButtons = false;

        if (dialogueSequenceHasBackgroundZoomOverride && backgroundZoom != null)
        {
            backgroundZoom.RestoreState(
                dialogueSequencePreviousBackgroundZoomScale,
                dialogueSequencePreviousBackgroundZoomPosition);
        }

        dialogueSequenceHasBackgroundZoomOverride = false;
    }

    private void SetSaveLoadButtonsVisible(bool visible)
    {
        if (saveButton != null)
        {
            saveButton.gameObject.SetActive(visible);
        }

        if (loadButton != null)
        {
            loadButton.gameObject.SetActive(visible);
        }

        RefreshSaveLoadButtonState();
    }

    private void RefreshSaveLoadButtonState()
    {
        bool canOpen = CanOpenSaveLoadPanel();
        ApplySaveLoadButtonState(saveButton, canOpen);
        ApplySaveLoadButtonState(loadButton, canOpen);
    }

    private void ApplySaveLoadButtonState(Button button, bool canOpen)
    {
        if (button == null)
        {
            return;
        }

        ColorBlock colors = button.colors;
        colors.disabledColor = saveLoadDisabledColor;
        button.colors = colors;
        button.interactable = canOpen;
        ApplySaveLoadButtonTextState(button, canOpen);
    }

    private void ApplySaveLoadButtonTextState(Button button, bool canOpen)
    {
        TMP_Text buttonText = button.GetComponentInChildren<TMP_Text>();
        if (buttonText == null)
        {
            return;
        }

        if (button == saveButton)
        {
            if (!saveButtonTextColorCached)
            {
                saveButtonTextColor = buttonText.color;
                saveButtonTextColorCached = true;
            }

            buttonText.color = canOpen ? saveButtonTextColor : saveLoadDisabledTextColor;
            return;
        }

        if (button == loadButton)
        {
            if (!loadButtonTextColorCached)
            {
                loadButtonTextColor = buttonText.color;
                loadButtonTextColorCached = true;
            }

            buttonText.color = canOpen ? loadButtonTextColor : saveLoadDisabledTextColor;
            return;
        }
    }

    public bool CanOpenSaveLoadPanel()
    {
        if (isFading)
        {
            return false;
        }

        if (flowState != ConversationFlowState.Idle)
        {
            return false;
        }

        if (dialogueSequenceIsActive || queuedDialogueMessages.Count > 0)
        {
            return false;
        }

        return actionButtonArea == null || actionButtonArea.activeInHierarchy;
    }

    private Image GetDialogueStillImage()
    {
        if (eventStillImage != null)
        {
            return eventStillImage;
        }

        return backgroundImage;
    }

    private DialogueSpeakerType GetSpeakerTypeForName(string speakerName)
    {
        if (speakerName == SystemSpeakerName)
        {
            return DialogueSpeakerType.System;
        }

        if (speakerName == PlayerSpeakerName)
        {
            return DialogueSpeakerType.Player;
        }

        if (speakerName == ScheduleSpeakerName)
        {
            return DialogueSpeakerType.Schedule;
        }

        if (speakerName == OutfitSpeakerName)
        {
            return DialogueSpeakerType.Outfit;
        }

        if (speakerName == BattleLogSpeakerName)
        {
            return DialogueSpeakerType.BattleLog;
        }

        return DialogueSpeakerType.Heroine;
    }

    private Color GetSpeakerNameColor(DialogueSpeakerType speakerType)
    {
        switch (speakerType)
        {
            case DialogueSpeakerType.Player:
                return playerSpeakerColor;
            case DialogueSpeakerType.System:
                return systemSpeakerColor;
            case DialogueSpeakerType.Schedule:
                return scheduleSpeakerColor;
            case DialogueSpeakerType.Outfit:
                return outfitSpeakerColor;
            case DialogueSpeakerType.BattleLog:
                return battleLogSpeakerColor;
            default:
                return heroineSpeakerColor;
        }
    }

    private Color GetDialogueColor(DialogueSpeakerType speakerType)
    {
        switch (speakerType)
        {
            case DialogueSpeakerType.Player:
                return playerDialogueColor;
            case DialogueSpeakerType.System:
                return systemDialogueColor;
            case DialogueSpeakerType.Schedule:
                return scheduleDialogueColor;
            case DialogueSpeakerType.Outfit:
                return outfitDialogueColor;
            case DialogueSpeakerType.BattleLog:
                return battleLogDialogueColor;
            default:
                return heroineDialogueColor;
        }
    }

    private void ApplyHeroineExpression(string expressionId)
    {
        if (outfitManager == null)
        {
            return;
        }

        outfitManager.SetHeroineExpression(expressionId);
    }

    private void ShowDialogueSequence(List<DialogueMessage> messages)
    {
        ResetDialogueSequenceState();
        queuedDialogueMessages.Clear();
        dialogueSequenceIsActive = true;

        if (messages == null || messages.Count == 0)
        {
            dialogueSequenceIsActive = false;
            nextButton.gameObject.SetActive(false);
            return;
        }

        SetDialogueText(
            messages[0].SpeakerType,
            messages[0].SpeakerName,
            messages[0].Message,
            messages[0].StillId,
            messages[0].StillSprite,
            messages[0].ExpressionId,
            messages[0].BattleResultVisualMode);

        for (int i = 1; i < messages.Count; i++)
        {
            queuedDialogueMessages.Enqueue(messages[i]);
        }

        nextButton.gameObject.SetActive(true);
    }

    private bool TryShowNextQueuedDialogue()
    {
        if (queuedDialogueMessages.Count == 0)
        {
            return false;
        }

        DialogueMessage message = queuedDialogueMessages.Dequeue();
        SetDialogueText(
            message.SpeakerType,
            message.SpeakerName,
            message.Message,
            message.StillId,
            message.StillSprite,
            message.ExpressionId,
            message.BattleResultVisualMode);

        if (queuedDialogueMessages.Count == 0 && flowState == ConversationFlowState.Idle)
        {
            ResetDialogueSequenceState();
            dialogueSequenceIsActive = false;
            actionButtonArea.SetActive(true);
            nextButton.gameObject.SetActive(false);
        }

        return true;
    }

    private void ShowConversationDialogue(ConversationData conversation)
    {
        ResetDialogueSequenceState();
        queuedDialogueMessages.Clear();
        dialogueSequenceIsActive = false;

        List<DialogueMessage> messages = BuildConversationDialogueMessages(conversation);
        if (messages.Count == 0)
        {
            ShowHeroineDialogue("");
            return;
        }

        if (messages.Count == 1)
        {
            DialogueMessage message = messages[0];
            SetDialogueText(
                message.SpeakerType,
                message.SpeakerName,
                message.Message,
                message.StillId,
                message.StillSprite,
                message.ExpressionId);
            return;
        }

        ShowDialogueSequence(messages);
    }

    private List<DialogueMessage> BuildConversationDialogueMessages(ConversationData conversation)
    {
        List<DialogueMessage> messages = new List<DialogueMessage>();
        if (conversation == null)
        {
            return messages;
        }

        if (conversation.lines != null && conversation.lines.Count > 0)
        {
            foreach (ConversationLineData line in conversation.lines)
            {
                if (line == null || string.IsNullOrWhiteSpace(line.text))
                {
                    continue;
                }

                DialogueSpeakerType speakerType = GetConversationLineSpeakerType(line.speaker);
                string speakerName = GetConversationLineSpeakerName(speakerType, line.speaker);
                messages.Add(
                    new DialogueMessage(
                        speakerType,
                        speakerName,
                        line.text,
                        "",
                        null,
                        line.expressionId));
            }

            return messages;
        }

        if (!string.IsNullOrWhiteSpace(conversation.heroineLine))
        {
            messages.Add(
                new DialogueMessage(
                    DialogueSpeakerType.Heroine,
                    heroineStatus.HeroineName,
                    conversation.heroineLine,
                    "",
                    null,
                    conversation.expressionId));
        }

        return messages;
    }

    private DialogueSpeakerType GetConversationLineSpeakerType(string speaker)
    {
        if (string.IsNullOrWhiteSpace(speaker))
        {
            return DialogueSpeakerType.Heroine;
        }

        if (string.Equals(speaker, "System", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(speaker, SystemSpeakerName, StringComparison.OrdinalIgnoreCase))
        {
            return DialogueSpeakerType.System;
        }

        if (IsPlayerSpeaker(speaker))
        {
            return DialogueSpeakerType.Player;
        }

        return DialogueSpeakerType.Heroine;
    }

    private string GetConversationLineSpeakerName(DialogueSpeakerType speakerType, string speaker)
    {
        if (speakerType == DialogueSpeakerType.System)
        {
            return SystemSpeakerName;
        }

        if (speakerType == DialogueSpeakerType.Player)
        {
            return PlayerSpeakerName;
        }

        return heroineStatus.HeroineName;
    }

    private static bool IsPlayerSpeaker(string speaker)
    {
        return string.Equals(speaker, "Player", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(speaker, "Protagonist", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(speaker, "MainCharacter", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(speaker, "Main Character", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(speaker, "PC", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(speaker, "User", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(speaker, "You", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(speaker, PlayerSpeakerName, StringComparison.OrdinalIgnoreCase);
    }

    private void ShowHeroineDialogue(string message)
    {
        ShowHeroineDialogue(message, "");
    }

    private void ShowHeroineDialogue(string message, string expressionId)
    {
        ResetDialogueSequenceState();
        queuedDialogueMessages.Clear();
        dialogueSequenceIsActive = false;
        SetDialogueText(
            DialogueSpeakerType.Heroine,
            heroineStatus.HeroineName,
            message,
            "",
            null,
            expressionId);
    }

    private void ShowSystemDialogue(string message)
    {
        ShowDialogue(DialogueSpeakerType.System, SystemSpeakerName, message);
    }

    private void ShowScheduleDialogue(string message)
    {
        ShowDialogue(DialogueSpeakerType.Schedule, ScheduleSpeakerName, message);
    }

    private void ShowOutfitDialogue(string message)
    {
        ShowDialogue(DialogueSpeakerType.Outfit, OutfitSpeakerName, message);
    }

    private string GetInitialDialogueMessage()
    {
        return GetHeroineProfileDialogue(
            heroineProfile != null ? heroineProfile.initialDialogueMessage : null,
            "今日は何を話しましょうか？");
    }

    private string GetNextActionPrompt()
    {
        return GetHeroineProfileDialogue(
            heroineProfile != null ? heroineProfile.nextActionPrompt : null,
            "次は何をしましょうか？");
    }

    private string GetMorningGreeting()
    {
        return GetHeroineProfileDialogue(
            heroineProfile != null ? heroineProfile.morningGreeting : null,
            "おはようございます。今日もよろしくお願いしますね。");
    }

    private string GetGoodNightGreeting()
    {
        return GetHeroineProfileDialogue(
            heroineProfile != null ? heroineProfile.goodNightGreeting : null,
            "もう夜も遅いですね。おやすみなさい。また明日。");
    }

    private string GetGameStartFallbackMessage()
    {
        return GetHeroineProfileDialogue(
            heroineProfile != null ? heroineProfile.gameStartFallbackMessage : null,
            "新しい物語が始まります。");
    }

    private string GetGameStartFollowUpMessage()
    {
        return GetHeroineProfileDialogue(
            heroineProfile != null ? heroineProfile.gameStartFollowUpMessage : null,
            "今日は何を話しましょうか？");
    }

    private static string GetHeroineProfileDialogue(string profileMessage, string fallback)
    {
        return string.IsNullOrWhiteSpace(profileMessage) ? fallback : profileMessage;
    }

    private void ApplyHeroineProfileSettings()
    {
        HeroineProfileData profile = ResolveHeroineProfile();
        ApplyHeroineProfileSettings(profile);
    }

    private void ApplyHeroineProfileSettings(HeroineProfileData profile)
    {
        if (profile == null)
        {
            return;
        }

        heroineProfile = profile;
        currentHeroineId = profile.heroineId ?? "";

        if (heroineStatus != null)
        {
            heroineStatus.SetHeroineName(profile.displayName);
        }

        if (outfitManager != null)
        {
            outfitManager.SetMessageOverrides(profile.outfitMessageOverrides);
            outfitManager.SetDefaultHeroineSprite(profile.defaultHeroineSprite);
            outfitManager.SetLayeredSpriteData(ResolveHeroineLayeredSpriteData(profile));
        }

        if (outfitPreferenceManager != null)
        {
            outfitPreferenceManager.SetReactionMessageOverrides(profile.outfitReactionMessageOverrides);
        }

        heroineAssetCatalog = ResolveHeroineAssetCatalog(profile);

        conversationResourcePath = GetProfileResourcePath(
            profile.conversationResourcePath,
            conversationResourcePath);
        gameEventResourcePath = GetProfileResourcePath(
            profile.gameEventResourcePath,
            gameEventResourcePath);
        actionResourcePath = GetProfileResourcePath(
            profile.actionResourcePath,
            actionResourcePath);
        scheduledEventResourcePath = GetProfileResourcePath(
            profile.scheduledEventResourcePath,
            scheduledEventResourcePath);
        battleResultEventResourcePath = GetProfileResourcePath(
            profile.battleResultEventResourcePath,
            battleResultEventResourcePath);
        battlePanelResultMessageResourcePath = GetProfileResourcePath(
            profile.battlePanelResultMessageResourcePath,
            battlePanelResultMessageResourcePath);

        if (battleResultEventsLoadedFromResources)
        {
            battleResultEvents = null;
            battleResultEventsLoadedFromResources = false;
        }

        commonBattleResultEvents = null;
        battlePanelResultMessages = null;
        commonBattlePanelResultMessages = null;

        Debug.Log(
            "Applied HeroineProfile resource paths: heroineId=" +
            currentHeroineId +
            " / conversations=" +
            conversationResourcePath +
            " / gameEvents=" +
            gameEventResourcePath +
            " / actions=" +
            actionResourcePath +
            " / scheduledEvents=" +
            scheduledEventResourcePath +
            " / battleResultEvents=" +
            battleResultEventResourcePath +
            " / battlePanelResultMessages=" +
            battlePanelResultMessageResourcePath);
    }

    private HeroineProfileData ResolveHeroineProfile()
    {
        HeroineProfileData developmentProfile = DevelopmentHeroineOverride.ResolveProfile(
            Resources.LoadAll<HeroineProfileData>("Heroines"),
            GameStartSettings.ShouldLoadOnStart);
        if (developmentProfile != null)
        {
            Debug.Log("Development heroine override applied: " + developmentProfile.heroineId);
            return developmentProfile;
        }

        HeroineProfileData selectedProfile = ResolveSelectedHeroineProfile();
        if (selectedProfile != null)
        {
            return selectedProfile;
        }

        if (heroineProfile != null)
        {
            return heroineProfile;
        }

        if (string.IsNullOrEmpty(defaultHeroineProfileResourcePath))
        {
            return null;
        }

        HeroineProfileData profile =
            Resources.Load<HeroineProfileData>(defaultHeroineProfileResourcePath);
        if (profile == null)
        {
            Debug.LogWarning("HeroineProfileData が見つかりません: " + defaultHeroineProfileResourcePath);
        }

        return profile;
    }

    private HeroineProfileData ResolveSelectedHeroineProfile()
    {
        if (!GameStartSettings.ShouldPlayGameStartEvent ||
            GameStartSettings.ShouldLoadOnStart ||
            string.IsNullOrWhiteSpace(GameStartSettings.SelectedHeroineId))
        {
            return null;
        }

        HeroineProfileData[] profiles = Resources.LoadAll<HeroineProfileData>("Heroines");
        foreach (HeroineProfileData profile in profiles)
        {
            if (profile == null)
            {
                continue;
            }

            if (string.Equals(
                profile.heroineId,
                GameStartSettings.SelectedHeroineId,
                StringComparison.Ordinal))
            {
                return profile;
            }
        }

        Debug.LogWarning(
            "選択された HeroineProfileData が見つかりません: " +
            GameStartSettings.SelectedHeroineId);
        return null;
    }

    private bool TryApplyHeroineProfileById(string heroineId)
    {
        if (string.IsNullOrWhiteSpace(heroineId))
        {
            return false;
        }

        HeroineProfileData profile = FindHeroineProfileById(heroineId);
        if (profile == null)
        {
            Debug.LogWarning("セーブデータの HeroineProfileData が見つかりません: " + heroineId);
            return false;
        }

        ApplyHeroineProfileSettings(profile);
        ReloadHeroineRuntimeData();
        return true;
    }

    private HeroineProfileData FindHeroineProfileById(string heroineId)
    {
        if (string.IsNullOrWhiteSpace(heroineId))
        {
            return null;
        }

        HeroineProfileData[] profiles = Resources.LoadAll<HeroineProfileData>("Heroines");
        foreach (HeroineProfileData profile in profiles)
        {
            if (profile == null)
            {
                continue;
            }

            if (string.Equals(profile.heroineId, heroineId, StringComparison.Ordinal))
            {
                return profile;
            }
        }

        return null;
    }

    private void ReloadHeroineRuntimeData()
    {
        LoadConversationsFromResources();
        LoadActionsFromResources();
        LoadGameEventsFromResources();
        LoadScheduledEventsFromResources();
        CreateGenreButtons();
        CreateActionButtons();
        CreateOutfitButtons();
    }

    private string GetProfileResourcePath(string profilePath, string fallbackPath)
    {
        if (string.IsNullOrEmpty(profilePath))
        {
            return fallbackPath;
        }

        return profilePath;
    }

    private HeroineLayeredSpriteData ResolveHeroineLayeredSpriteData(HeroineProfileData profile)
    {
        if (profile == null || string.IsNullOrEmpty(profile.heroineId))
        {
            return null;
        }

        string resourcePath = "Heroines/" + profile.heroineId + "/HeroineLayeredSpriteData";
        return Resources.Load<HeroineLayeredSpriteData>(resourcePath);
    }

    private HeroineAssetCatalog ResolveHeroineAssetCatalog(HeroineProfileData profile)
    {
        if (profile == null || string.IsNullOrEmpty(profile.heroineId))
        {
            return null;
        }

        string resourcePath = "Heroines/" + profile.heroineId + "/HeroineAssetCatalog";
        HeroineAssetCatalog catalog = Resources.Load<HeroineAssetCatalog>(resourcePath);
        if (catalog == null)
        {
            Debug.LogWarning("HeroineAssetCatalog が見つかりません: " + resourcePath);
        }

        return catalog;
    }

    private void Start()
    {
        EnsureCoreStatusReferences();
        ApplyHeroineProfileSettings();
        ValidateSkillTreeDataOnStartup();

        LoadConversationsFromResources();
        LoadActionsFromResources();
        LoadGameEventsFromResources();
        LoadScheduledEventsFromResources();
        SynchronizeUnlockedSkillsWithSkillTree();
        NormalizeEquippedPlayerBattleSkills(true);
        NormalizeAllHeroineBattleSkillLoadouts(true);
        NormalizeActivePlayerTrainingSkills(true);
        NormalizeAllActiveHeroineTrainingSkills(true);

        CreateGenreButtons();
        CreateActionButtons();
        CreateOutfitButtons();

        SetFadeAlpha(0f);

        praiseOutfitButton.onClick.AddListener(() => OnClickOutfitReaction(OutfitReactionType.Praise));
        dislikeOutfitButton.onClick.AddListener(() => OnClickOutfitReaction(OutfitReactionType.Dislike));
        boredOutfitButton.onClick.AddListener(() => OnClickOutfitReaction(OutfitReactionType.Bored));
        changeOutfitButton.onClick.AddListener(() => OnClickOutfitReaction(OutfitReactionType.Change));

        nextButton.onClick.AddListener(OnClickNext);
        if (dialogueClickAdvanceArea != null)
        {
            dialogueClickAdvanceArea.Initialize(this);
        }

        endingButton.onClick.AddListener(OnClickEnding);

        actionButtonArea.SetActive(true);
        genreButtonArea.SetActive(false);
        choiceButtonArea.SetActive(false);
        outfitPanel.SetActive(false);
        outfitReactionPanel.SetActive(false);
        nextButton.gameObject.SetActive(false);
        endingButton.gameObject.SetActive(false);
        SetSaveLoadButtonsVisible(false);
        if (eventStillImage != null)
        {
            eventStillImage.gameObject.SetActive(false);
        }

        //saveButton.onClick.AddListener(SaveGame);
        //loadButton.onClick.AddListener(LoadGame);

        timeManager.OnDayChanged += OnDayChanged;

        outfitManager.WearDefaultOutfit();

        OnTalkStart();
        //background.localScale = new Vector3(1.3f, 1.3f, 1f);
        //background.anchoredPosition = new Vector2(-200f, 0f);

        RefreshUI();

        if (GameStartSettings.ShouldLoadOnStart)
        {
            GameStartSettings.ShouldLoadOnStart = false;
            GameStartSettings.ShouldPlayGameStartEvent = false;
            LoadGameFromSlotInternal(GameStartSettings.SelectedSaveSlotIndex);
            RefreshUI();
            SetSaveLoadButtonsVisible(true);
            EnsureStatusDetailPanel();
            return;
        }

        if (GameStartSettings.ShouldPlayGameStartEvent)
        {
            GameStartSettings.ShouldPlayGameStartEvent = false;
            StartGameStartSequence();
            EnsureStatusDetailPanel();
            return;
        }

        ShowHeroineDialogue(GetInitialDialogueMessage());
        RefreshUI();
        SetSaveLoadButtonsVisible(true);
        EnsureStatusDetailPanel();
    }

    private void EnsureCoreStatusReferences()
    {
        if (playerStatus == null)
        {
            playerStatus = FindObjectOfType<PlayerStatus>();
        }

        if (playerStatus == null)
        {
            playerStatus = gameObject.AddComponent<PlayerStatus>();
        }
    }

    private void LoadConversationsFromResources()
    {
        ConversationData[] loadedConversations =
            Resources.LoadAll<ConversationData>(conversationResourcePath);

        conversations = new List<ConversationData>();
        Dictionary<string, string> loadedConversationSources = new Dictionary<string, string>();
        foreach (ConversationData loadedConversation in loadedConversations)
        {
            AddLoadedConversationData(loadedConversation, loadedConversationSources);
        }

        Debug.Log(
            "Loaded Conversations: " +
            conversations.Count +
            " / ResourcePath: " +
            conversationResourcePath +
            " / RawAssets: " +
            loadedConversations.Length);

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

    private void AddLoadedConversationData(
        ConversationData loadedConversation,
        Dictionary<string, string> loadedConversationSources)
    {
        if (loadedConversation == null)
        {
            return;
        }

        string sourceName = string.IsNullOrEmpty(loadedConversation.name)
            ? loadedConversation.ToString()
            : loadedConversation.name;

        if (loadedConversation.items != null && loadedConversation.items.Count > 0)
        {
            foreach (ConversationDataItem item in loadedConversation.items)
            {
                ConversationData expandedConversation = CreateConversationFromItem(
                    loadedConversation,
                    item);
                if (expandedConversation != null)
                {
                    AddConversationIfNotDuplicate(
                        expandedConversation,
                        loadedConversationSources,
                        sourceName + ".items/" + item.conversationId);
                }
            }

            return;
        }

        AddConversationIfNotDuplicate(
            loadedConversation,
            loadedConversationSources,
            sourceName);
    }

    private void AddConversationIfNotDuplicate(
        ConversationData conversation,
        Dictionary<string, string> loadedConversationSources,
        string sourceName)
    {
        if (conversation == null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(conversation.conversationId)
            && loadedConversationSources.TryGetValue(conversation.conversationId, out string existingSource))
        {
            Debug.LogWarning(
                "Conversation Id が重複しているためスキップしました: " +
                conversation.conversationId +
                " / Existing: " +
                existingSource +
                " / Skipped: " +
                sourceName +
                " / ResourcePath: " +
                conversationResourcePath);
            return;
        }

        if (!string.IsNullOrEmpty(conversation.conversationId))
        {
            loadedConversationSources[conversation.conversationId] = sourceName;
        }

        conversations.Add(conversation);
    }

    private ConversationData CreateConversationFromItem(
        ConversationData sourceAsset,
        ConversationDataItem item)
    {
        if (item == null)
        {
            return null;
        }

        ConversationData conversation = ScriptableObject.CreateInstance<ConversationData>();
        conversation.name = string.IsNullOrEmpty(item.conversationId)
            ? sourceAsset.name
            : sourceAsset.name + "/" + item.conversationId;
        conversation.conversationId = item.conversationId;
        conversation.genre = item.genre;
        conversation.type = item.type;
        conversation.heroineLine = item.heroineLine;
        conversation.expressionId = item.expressionId;
        conversation.lines = item.lines == null
            ? new List<ConversationLineData>()
            : new List<ConversationLineData>(item.lines);
        conversation.choices = item.choices == null
            ? new List<ConversationChoice>()
            : new List<ConversationChoice>(item.choices);
        conversation.priority = item.priority;
        conversation.showOnce = item.showOnce;
        conversation.minAffection = item.minAffection;
        conversation.maxAffection = item.maxAffection;
        conversation.costumeId = item.costumeId;
        conversation.anyTimeSlot = item.anyTimeSlot;
        conversation.allowedTimeSlots = item.allowedTimeSlots == null
            ? new List<TimeSlot>()
            : new List<TimeSlot>(item.allowedTimeSlots);
        conversation.anySeason = item.anySeason;
        conversation.allowedSeasons = item.allowedSeasons == null
            ? new List<Season>()
            : new List<Season>(item.allowedSeasons);
        conversation.anyWeather = item.anyWeather;
        conversation.allowedWeathers = item.allowedWeathers == null
            ? new List<Weather>()
            : new List<Weather>(item.allowedWeathers);
        return conversation;
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
            ShowSystemDialogue("現在の条件に合う会話データがありません。");

            flowState = ConversationFlowState.ShowingSimple;
            nextButton.gameObject.SetActive(true);
            return;
        }

        currentConversation = SelectConversationByPriority(candidates);

        RegisterShownConversation(currentConversation);

        ShowConversationDialogue(currentConversation);

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

        if (!IsCostumeConditionMatch(conversation.costumeId))
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

        int highestScore = GetConversationSelectionScore(candidates[0]);

        for (int i = 1; i < candidates.Count; i++)
        {
            int candidateScore = GetConversationSelectionScore(candidates[i]);

            if (candidateScore > highestScore)
            {
                highestScore = candidateScore;
            }
        }

        List<ConversationData> highestCandidates = candidates.FindAll(
            conversation => GetConversationSelectionScore(conversation) == highestScore
        );

        return highestCandidates[UnityEngine.Random.Range(0, highestCandidates.Count)];
    }

    private int GetConversationSelectionScore(ConversationData conversation)
    {
        if (conversation == null)
        {
            return int.MinValue;
        }

        int score = conversation.priority;

        if (scheduleManager == null)
        {
            return score;
        }

        if (scheduleManager.IsTodayHomeSchedule())
        {
            switch (conversation.genre)
            {
                case ConversationGenre.Daily:
                    score += 8;
                    break;
                case ConversationGenre.Food:
                    score += 4;
                    break;
                case ConversationGenre.Love:
                    score += 3;
                    break;
                case ConversationGenre.Adventure:
                    score -= 6;
                    break;
            }
        }

        if (scheduleManager.IsTodayDuoSchedule())
        {
            switch (conversation.genre)
            {
                case ConversationGenre.Love:
                    score += 10;
                    break;
                case ConversationGenre.Food:
                    score += 4;
                    break;
                case ConversationGenre.Daily:
                    score += 2;
                    break;
                case ConversationGenre.Adventure:
                    score += 1;
                    break;
            }
        }

        return score;
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

        if (TryShowNextQueuedDialogue())
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

        if (flowState == ConversationFlowState.ShowingBattleIntro)
        {
            StartPendingBattlePanelBattle();
            return;
        }

        if (flowState == ConversationFlowState.ShowingGoodNight)
        {
            StartCoroutine(FadeToBlackAndNextMorning());
            return;
        }

        if (dialogueSequenceIsActive && flowState == ConversationFlowState.Idle)
        {
            ResetDialogueSequenceState();
            dialogueSequenceIsActive = false;
            actionButtonArea.SetActive(true);
            nextButton.gameObject.SetActive(false);
        }
    }

    public void OnDialogueWindowClicked()
    {
        if (!enableDialogueWindowClickAdvance)
        {
            return;
        }

        if (nextButton == null ||
            !nextButton.gameObject.activeInHierarchy ||
            !nextButton.interactable)
        {
            return;
        }

        OnClickNext();
    }

    private void FinishActionResult()
    {
        if (pendingGoodNight)
        {
            pendingGoodNight = false;

            ShowHeroineDialogue(GetGoodNightGreeting());

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

        if (startPendingScheduledEventAfterOutfitMessage)
        {
            startPendingScheduledEventAfterOutfitMessage = false;
            StartScheduledEvent(pendingScheduledEvent);
            return;
        }

        if (returnToScheduledEventPromptAfterOutfitMessage)
        {
            returnToScheduledEventPromptAfterOutfitMessage = false;
            ShowScheduledEventOutfitPrompt(pendingScheduledEvent);
            return;
        }

        if (TryStartScheduledEvent())
        {
            return;
        }

        flowState = ConversationFlowState.Idle;

        nextButton.gameObject.SetActive(false);
        choiceButtonArea.SetActive(false);
        genreButtonArea.SetActive(false);
        outfitPanel.SetActive(false);
        outfitReactionPanel.SetActive(false);
        actionButtonArea.SetActive(true);

        ShowHeroineDialogue(GetNextActionPrompt());
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
            ShowSystemDialogue("選択肢タイプですが、Choices が設定されていません。");

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

        ShowHeroineDialogue(choice.responseText);

        heroineStatus.AddAffection(choice.affectionChange);

        RefreshUI();

        flowState = ConversationFlowState.ShowingResponse;
        nextButton.gameObject.SetActive(true);
    }

    private void FinishSimpleConversation()
    {
        heroineStatus.AddAffection(20);

        if (IsNightBeforeAdvance())
        {
            RefreshUI();
            ShowGoodNightBeforeNextDay();
            return;
        }

        timeManager.AdvanceTime();
        RefreshUI();

        if (TryStartScheduledEvent())
        {
            return;
        }

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

        if (TryStartScheduledEvent())
        {
            return;
        }

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

        ShowHeroineDialogue(GetNextActionPrompt());
    }

    private void OnClickEnding()
    {
        if (string.IsNullOrEmpty(endingSceneName))
        {
            ShowSystemDialogue("エンディングシーン名が設定されていません。");
            return;
        }

        EndingSelectionSettings.SelectedEndingId = ResolveEndingIdForCurrentState();
        EndingSelectionSettings.SelectedHeroineId = heroineProfile != null ? heroineProfile.heroineId : "";
        EndingSelectionSettings.EndingResourcePath =
            heroineProfile != null ? heroineProfile.endingResourcePath : "";
        SceneManager.LoadScene(endingSceneName);
    }

    private string ResolveEndingIdForCurrentState()
    {
        string resourcePath = heroineProfile != null ? heroineProfile.endingResourcePath : "";
        if (string.IsNullOrEmpty(resourcePath))
        {
            return defaultEndingId;
        }

        EndingData[] endings = Resources.LoadAll<EndingData>(resourcePath);
        EndingData selected = null;
        foreach (EndingData ending in endings)
        {
            if (!CanSelectEnding(ending))
            {
                continue;
            }

            if (selected == null || ending.requiredAffection > selected.requiredAffection)
            {
                selected = ending;
            }
        }

        if (selected == null || string.IsNullOrEmpty(selected.endingId))
        {
            return defaultEndingId;
        }

        Debug.Log(
            "Selected EndingData: endingId=" +
            selected.endingId +
            " / requiredAffection=" +
            selected.requiredAffection +
            " / costumeId=" +
            selected.costumeId);
        return selected.endingId;
    }

    private bool CanSelectEnding(EndingData ending)
    {
        if (ending == null || string.IsNullOrEmpty(ending.endingId))
        {
            return false;
        }

        if (heroineStatus != null && heroineStatus.Affection < ending.requiredAffection)
        {
            return false;
        }

        if (!IsCostumeConditionMatch(ending.costumeId))
        {
            return false;
        }

        if (ending.requiredShownEventIds != null)
        {
            foreach (string eventId in ending.requiredShownEventIds)
            {
                if (string.IsNullOrEmpty(eventId))
                {
                    continue;
                }

                if (!IsGameEventShown(eventId))
                {
                    return false;
                }
            }
        }

        return true;
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
        UpdateMainStatusUI();

        endingButton.gameObject.SetActive(heroineStatus.CanEnding());

        UpdateScheduleStatusUI();

        UpdateBackgroundByTime();
    }

    public void SaveGame()
    {
        SaveGameToSlot(GameStartSettings.SelectedSaveSlotIndex);
    }

    public void CaptureSaveThumbnailPreview()
    {
        ClearPendingSaveThumbnail();

        Texture2D screenshot = ScreenCapture.CaptureScreenshotAsTexture();
        if (screenshot == null)
        {
            return;
        }

        pendingSaveThumbnail = CreateScaledTexture(
            screenshot,
            SaveThumbnailWidth,
            SaveThumbnailHeight);
        Destroy(screenshot);
    }

    public void ClearSaveThumbnailPreview()
    {
        ClearPendingSaveThumbnail();
    }

    public void SaveGameToSlot(int slotIndex)
    {
        if (!CanOpenSaveLoadPanel())
        {
            Debug.LogWarning("会話やイベントの表示中はセーブできません。");
            return;
        }

        SelectSaveSlot(slotIndex);

        SaveData saveData = new SaveData();

        saveData.saveSlotIndex = GameStartSettings.SelectedSaveSlotIndex;
        saveData.savedAt = DateTime.Now.ToString("o");
        saveData.heroineId = currentHeroineId;
        saveData.heroineDisplayName = heroineStatus != null ? heroineStatus.HeroineName : "";
        saveData.thumbnailFileName = SavePendingThumbnail(GameStartSettings.SelectedSaveSlotIndex);

        saveData.day = timeManager.Day;
        saveData.currentTimeSlot = timeManager.CurrentTimeSlot;
        saveData.currentWeekday = timeManager.CurrentWeekday;
        saveData.currentSeason = timeManager.CurrentSeason;
        saveData.currentWeather = timeManager.CurrentWeather;

        saveData.affection = heroineStatus.Affection;
        if (playerStatus != null)
        {
            saveData.playerBattleStatus = playerStatus.BattleStatus.Clone();
            saveData.playerMoney = playerStatus.Money;
        }

        if (heroineStatus != null)
        {
            saveData.heroineBattleStatus = heroineStatus.BattleStatus.Clone();
        }

        if (outfitManager.CurrentOutfit != null)
        {
            saveData.currentOutfitId = outfitManager.CurrentOutfit.outfitId;
        }
        else
        {
            saveData.currentOutfitId = "";
        }

        saveData.outfitPreferences = outfitPreferenceManager.CreateSaveData();
        saveData.playerOutfitPromptAbilities = playerOutfitPromptAbilities.Clone();
        saveData.heroineOutfitPromptAbilities =
            heroineStatus != null
                ? heroineStatus.OutfitPromptAbilities.Clone()
                : new OutfitPromptAbilitySet();
        saveData.unlockedStatusAbilityIds = new List<string>(unlockedStatusAbilityIds);
        saveData.unlockedSkillIds = new List<string>(unlockedSkillIds);
        NormalizeEquippedPlayerBattleSkills(false);
        saveData.equippedPlayerBattleSkillIds =
            new List<string>(equippedPlayerBattleSkillIds);
        saveData.heroineBattleSkillLoadouts = CreateHeroineBattleSkillLoadoutSaveData();
        NormalizeActivePlayerTrainingSkills(false);
        saveData.activePlayerTrainingSkillIds =
            CreateSortedSkillIdList(activePlayerTrainingSkillIds);
        saveData.heroineTrainingSkillActivations =
            CreateHeroineTrainingSkillActivationSaveData();
        saveData.unlockedStillIds = new List<string>(unlockedStillIds);
        saveData.purchasedItemIds = new List<string>(purchasedItemIds);
        saveData.itemQuantities = CreateItemQuantitySaveData();
        saveData.unlockedOutfitIds = new List<string>(unlockedOutfitIds);
        saveData.trainingProficiencies = CreateTrainingProficiencySaveData();
        saveData.skillProgressStats = skillProgressStats.Clone();
        saveData.playerSkillPoints = Mathf.Max(0, playerSkillPoints);
        saveData.heroineSkillPoints = Mathf.Max(0, heroineSkillPoints);
        saveData.acquiredPlayerSkillTreeNodeIds =
            new List<string>(acquiredPlayerSkillTreeNodeIds);
        saveData.acquiredHeroineSkillTreeNodeIds =
            new List<string>(acquiredHeroineSkillTreeNodeIds);

        saveData.shownConversationIds = new List<string>(shownConversationIds);
        saveData.shownGameEventIds = new List<string>(shownGameEventIds);

        saveData.todaySchedule = scheduleManager.TodaySchedule;
        saveData.tomorrowSchedule = scheduleManager.TomorrowSchedule;
        saveData.todayScheduleEventExecuted = scheduleManager.TodayScheduleEventExecuted;

        saveManager.Save(saveData, GameStartSettings.SelectedSaveSlotIndex);

        ShowSystemDialogue("セーブしました。");
    }

    private string SavePendingThumbnail(int slotIndex)
    {
        if (saveManager == null || pendingSaveThumbnail == null)
        {
            return "";
        }

        string fileName = saveManager.SaveThumbnail(pendingSaveThumbnail, slotIndex);
        return fileName;
    }

    private void ClearPendingSaveThumbnail()
    {
        if (pendingSaveThumbnail != null)
        {
            Destroy(pendingSaveThumbnail);
            pendingSaveThumbnail = null;
        }
    }

    private static Texture2D CreateScaledTexture(Texture2D source, int width, int height)
    {
        RenderTexture previous = RenderTexture.active;
        RenderTexture renderTexture = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(source, renderTexture);
        RenderTexture.active = renderTexture;

        Texture2D scaled = new Texture2D(width, height, TextureFormat.RGBA32, false);
        scaled.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        scaled.Apply();

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(renderTexture);
        return scaled;
    }

    public void LoadGame()
    {
        LoadGameFromSlot(GameStartSettings.SelectedSaveSlotIndex);
    }

    public void LoadGameFromSlot(int slotIndex)
    {
        if (!CanOpenSaveLoadPanel())
        {
            Debug.LogWarning("会話やイベントの表示中はロードできません。");
            return;
        }

        LoadGameFromSlotInternal(slotIndex);
    }

    private void LoadGameFromSlotInternal(int slotIndex)
    {
        SelectSaveSlot(slotIndex);

        SaveData saveData = saveManager.Load(GameStartSettings.SelectedSaveSlotIndex);

        if (saveData == null)
        {
            ShowSystemDialogue("セーブデータがありません。");
            return;
        }

        TryApplyHeroineProfileById(saveData.heroineId);

        timeManager.SetTimeState(
            saveData.day,
            saveData.currentTimeSlot,
            saveData.currentWeekday,
            saveData.currentSeason,
            saveData.currentWeather
        );

        heroineStatus.SetAffection(saveData.affection);
        if (saveData.saveVersion >= 4 && playerStatus != null)
        {
            playerStatus.SetBattleStatus(saveData.playerBattleStatus);
            playerStatus.SetMoney(saveData.playerMoney);
        }

        if (saveData.saveVersion >= 4 && heroineStatus != null)
        {
            heroineStatus.SetBattleStatus(saveData.heroineBattleStatus);
        }

        playerOutfitPromptAbilities.CopyFrom(saveData.playerOutfitPromptAbilities);
        if (heroineStatus != null)
        {
            heroineStatus.SetOutfitPromptAbilities(saveData.heroineOutfitPromptAbilities);
        }
        unlockedStatusAbilityIds.Clear();
        if (saveData.unlockedStatusAbilityIds != null)
        {
            foreach (string abilityId in saveData.unlockedStatusAbilityIds)
            {
                if (!string.IsNullOrEmpty(abilityId))
                {
                    unlockedStatusAbilityIds.Add(abilityId);
                }
            }
        }
        unlockedSkillIds.Clear();
        if (saveData.unlockedSkillIds != null)
        {
            foreach (string skillId in saveData.unlockedSkillIds)
            {
                if (!string.IsNullOrEmpty(skillId))
                {
                    unlockedSkillIds.Add(skillId);
                }
            }
        }
        unlockedStillIds.Clear();
        if (saveData.unlockedStillIds != null)
        {
            foreach (string stillId in saveData.unlockedStillIds)
            {
                if (!string.IsNullOrEmpty(stillId))
                {
                    unlockedStillIds.Add(stillId);
                }
            }
        }
        purchasedItemIds.Clear();
        if (saveData.purchasedItemIds != null)
        {
            foreach (string itemId in saveData.purchasedItemIds)
            {
                if (!string.IsNullOrEmpty(itemId))
                {
                    purchasedItemIds.Add(itemId);
                }
            }
        }
        LoadItemQuantities(saveData.itemQuantities);
        unlockedOutfitIds.Clear();
        if (saveData.unlockedOutfitIds != null)
        {
            foreach (string outfitId in saveData.unlockedOutfitIds)
            {
                if (!string.IsNullOrEmpty(outfitId))
                {
                    unlockedOutfitIds.Add(outfitId);
                }
            }
        }
        ApplyPurchasedItemOutfitUnlocks();
        ApplyUnlockedOutfitsToManager();
        LoadTrainingProficiencies(saveData.trainingProficiencies);
        skillProgressStats.CopyFrom(saveData.skillProgressStats);
        playerSkillPoints = Mathf.Max(0, saveData.playerSkillPoints);
        heroineSkillPoints = Mathf.Max(0, saveData.heroineSkillPoints);
        LoadSkillTreeNodeIds(
            acquiredPlayerSkillTreeNodeIds,
            saveData.acquiredPlayerSkillTreeNodeIds);
        LoadSkillTreeNodeIds(
            acquiredHeroineSkillTreeNodeIds,
            saveData.acquiredHeroineSkillTreeNodeIds);
        equippedPlayerBattleSkillIds.Clear();
        if (saveData.equippedPlayerBattleSkillIds != null)
        {
            for (int i = 0; i < saveData.equippedPlayerBattleSkillIds.Count; i++)
            {
                string skillId = saveData.equippedPlayerBattleSkillIds[i];
                if (!string.IsNullOrEmpty(skillId))
                {
                    equippedPlayerBattleSkillIds.Add(skillId);
                }
            }
        }
        SynchronizeUnlockedSkillsWithSkillTree();
        NormalizeEquippedPlayerBattleSkills(saveData.saveVersion < 14);
        LoadHeroineBattleSkillLoadouts(saveData.heroineBattleSkillLoadouts);
        NormalizeAllHeroineBattleSkillLoadouts(saveData.saveVersion < 15);
        LoadActivePlayerTrainingSkills(saveData.activePlayerTrainingSkillIds);
        NormalizeActivePlayerTrainingSkills(saveData.saveVersion < 16);
        LoadActiveHeroineTrainingSkills(saveData.heroineTrainingSkillActivations);
        NormalizeAllActiveHeroineTrainingSkills(saveData.saveVersion < 16);

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

        shownGameEventIds.Clear();
        if (saveData.shownGameEventIds != null)
        {
            foreach (string eventId in saveData.shownGameEventIds)
            {
                if (!string.IsNullOrEmpty(eventId))
                {
                    shownGameEventIds.Add(eventId);
                }
            }
        }

        scheduleManager.SetScheduleState(
            saveData.todaySchedule,
            saveData.tomorrowSchedule,
            saveData.todayScheduleEventExecuted
        );

        choiceButtonArea.SetActive(false);
        nextButton.gameObject.SetActive(false);
        genreButtonArea.SetActive(false);
        outfitPanel.SetActive(false);
        outfitReactionPanel.SetActive(false);
        actionButtonArea.SetActive(true);

        currentConversation = null;
        flowState = ConversationFlowState.Idle;

        ShowSystemDialogue("ロードしました。");

        RefreshUI();
        TryStartScheduledEvent();
    }

    public void SelectSaveSlot(int slotIndex)
    {
        if (saveManager == null)
        {
            return;
        }

        saveManager.SetCurrentSlotIndex(slotIndex);
        GameStartSettings.SelectedSaveSlotIndex = saveManager.CurrentSlotIndex;
    }

    public bool HasSaveDataInSlot(int slotIndex)
    {
        return saveManager != null && saveManager.HasSaveData(slotIndex);
    }

    public bool IsStatusAbilityUnlocked(string abilityId)
    {
        return !string.IsNullOrEmpty(abilityId) && unlockedStatusAbilityIds.Contains(abilityId);
    }

    public void UnlockStatusAbility(string abilityId)
    {
        if (string.IsNullOrEmpty(abilityId))
        {
            return;
        }

        unlockedStatusAbilityIds.Add(abilityId);
    }

    public bool IsSkillUnlocked(string skillId)
    {
        return !string.IsNullOrEmpty(skillId) && unlockedSkillIds.Contains(skillId);
    }

    private void UnlockSkill(string skillId)
    {
        if (string.IsNullOrEmpty(skillId))
        {
            return;
        }

        unlockedSkillIds.Add(skillId);
    }

    public List<string> GetUnlockedSkillIds()
    {
        return new List<string>(unlockedSkillIds);
    }

    public int PlayerBattleSkillSlotCount => DefaultPlayerBattleSkillSlotCount;

    public List<string> GetEquippedPlayerBattleSkillIds()
    {
        NormalizeEquippedPlayerBattleSkills(false);
        return new List<string>(equippedPlayerBattleSkillIds);
    }

    public bool IsPlayerBattleSkillEquipped(string skillId)
    {
        return !string.IsNullOrEmpty(skillId) &&
            equippedPlayerBattleSkillIds.Contains(skillId);
    }

    public bool TryEquipPlayerBattleSkill(string skillId, out string message)
    {
        NormalizeEquippedPlayerBattleSkills(false);
        SkillData skill = FindSkillData(skillId);
        if (!IsEquippablePlayerBattleSkill(skill))
        {
            message = "習得済みの戦闘スキルだけを装備できます。";
            return false;
        }

        if (equippedPlayerBattleSkillIds.Contains(skillId))
        {
            message = skill.GetDisplayName() + "はすでに装備中です。";
            return true;
        }

        if (equippedPlayerBattleSkillIds.Count >= DefaultPlayerBattleSkillSlotCount)
        {
            message = "装備枠がいっぱいです。先に別のスキルを外してください。";
            return false;
        }

        equippedPlayerBattleSkillIds.Add(skillId);
        message = skill.GetDisplayName() + "を装備しました。";
        return true;
    }

    public bool TryUnequipPlayerBattleSkill(string skillId, out string message)
    {
        NormalizeEquippedPlayerBattleSkills(false);
        SkillData skill = FindSkillData(skillId);
        if (string.IsNullOrEmpty(skillId) || !equippedPlayerBattleSkillIds.Remove(skillId))
        {
            message = "このスキルは装備されていません。";
            return false;
        }

        message = (skill != null ? skill.GetDisplayName() : skillId) + "を外しました。";
        return true;
    }

    public bool TryTogglePlayerBattleSkill(string skillId, out string message)
    {
        return IsPlayerBattleSkillEquipped(skillId)
            ? TryUnequipPlayerBattleSkill(skillId, out message)
            : TryEquipPlayerBattleSkill(skillId, out message);
    }

    public List<string> GetActivePlayerTrainingSkillIds()
    {
        NormalizeActivePlayerTrainingSkills(false);
        return CreateSortedSkillIdList(activePlayerTrainingSkillIds);
    }

    public List<SkillData> GetActivePlayerTrainingSkills()
    {
        NormalizeActivePlayerTrainingSkills(false);
        return CreateTrainingSkillDataList(activePlayerTrainingSkillIds);
    }

    public bool IsPlayerTrainingSkillActive(string skillId)
    {
        return !string.IsNullOrEmpty(skillId) &&
            activePlayerTrainingSkillIds.Contains(skillId);
    }

    public bool TryTogglePlayerTrainingSkill(string skillId, out string message)
    {
        NormalizeActivePlayerTrainingSkills(false);
        SkillData skill = FindSkillData(skillId);
        if (!IsActivatablePlayerTrainingSkill(skill))
        {
            message = "習得済みの訓練スキルだけを有効にできます。";
            return false;
        }

        if (activePlayerTrainingSkillIds.Remove(skillId))
        {
            message = skill.GetDisplayName() + "を無効にしました。";
        }
        else
        {
            activePlayerTrainingSkillIds.Add(skillId);
            message = skill.GetDisplayName() + "を有効にしました。";
        }

        return true;
    }

    public List<string> GetActiveHeroineTrainingSkillIds()
    {
        return GetActiveHeroineTrainingSkillIds(currentHeroineId);
    }

    public List<string> GetActiveHeroineTrainingSkillIds(string heroineId)
    {
        if (string.IsNullOrEmpty(heroineId))
        {
            return new List<string>();
        }

        NormalizeActiveHeroineTrainingSkills(heroineId, false);
        return CreateSortedSkillIdList(GetOrCreateActiveHeroineTrainingSkillIds(heroineId));
    }

    public List<SkillData> GetActiveHeroineTrainingSkills()
    {
        return GetActiveHeroineTrainingSkills(currentHeroineId);
    }

    public List<SkillData> GetActiveHeroineTrainingSkills(string heroineId)
    {
        if (string.IsNullOrEmpty(heroineId))
        {
            return new List<SkillData>();
        }

        NormalizeActiveHeroineTrainingSkills(heroineId, false);
        return CreateTrainingSkillDataList(
            GetOrCreateActiveHeroineTrainingSkillIds(heroineId));
    }

    public bool IsHeroineTrainingSkillActive(string skillId)
    {
        return IsHeroineTrainingSkillActive(currentHeroineId, skillId);
    }

    public bool IsHeroineTrainingSkillActive(string heroineId, string skillId)
    {
        if (string.IsNullOrEmpty(heroineId) || string.IsNullOrEmpty(skillId))
        {
            return false;
        }

        NormalizeActiveHeroineTrainingSkills(heroineId, false);
        return GetOrCreateActiveHeroineTrainingSkillIds(heroineId).Contains(skillId);
    }

    public bool TryToggleHeroineTrainingSkill(string skillId, out string message)
    {
        string heroineId = currentHeroineId;
        if (string.IsNullOrEmpty(heroineId))
        {
            message = "ヒロインが選択されていません。";
            return false;
        }

        NormalizeActiveHeroineTrainingSkills(heroineId, false);
        SkillData skill = FindSkillData(skillId);
        if (!IsActivatableHeroineTrainingSkill(heroineId, skill))
        {
            message = "現在のヒロインが習得済みの訓練スキルだけを有効にできます。";
            return false;
        }

        HashSet<string> activeIds = GetOrCreateActiveHeroineTrainingSkillIds(heroineId);
        if (activeIds.Remove(skillId))
        {
            message = skill.GetDisplayName() + "を無効にしました。";
        }
        else
        {
            activeIds.Add(skillId);
            message = skill.GetDisplayName() + "を有効にしました。";
        }

        return true;
    }

    public List<SkillData> GetSkills()
    {
        return new List<SkillData>(GetSkillDataList());
    }

    public List<SkillTreeNodeData> GetSkillTreeNodes()
    {
        List<SkillTreeNodeData> nodes = new List<SkillTreeNodeData>(GetSkillTreeNodeDataList());
        nodes.RemoveAll(node => node == null || string.IsNullOrEmpty(node.nodeId));
        nodes.Sort((a, b) =>
        {
            int ownerComparison = a.owner.CompareTo(b.owner);
            return ownerComparison != 0
                ? ownerComparison
                : a.sortOrder.CompareTo(b.sortOrder);
        });
        return nodes;
    }

    public List<string> GetAcquiredSkillTreeNodeIds(SkillTreeOwner owner)
    {
        List<string> nodeIds = new List<string>(GetSkillTreeNodeSet(owner));
        nodeIds.Sort(StringComparer.Ordinal);
        return nodeIds;
    }

    public bool IsSkillTreeNodeAcquired(string nodeId, SkillTreeOwner owner)
    {
        return !string.IsNullOrEmpty(nodeId) && GetSkillTreeNodeSet(owner).Contains(nodeId);
    }

    public bool ShouldShowTraining(TrainingData training)
    {
        return training != null &&
            (training.unlockedByDefault || GetTrainingUnlockNodes(training.trainingId).Count > 0);
    }

    public bool IsTrainingUnlocked(TrainingData training)
    {
        if (training == null)
        {
            return false;
        }
        if (training.unlockedByDefault)
        {
            return true;
        }

        List<SkillTreeNodeData> unlockNodes = GetTrainingUnlockNodes(training.trainingId);
        for (int i = 0; i < unlockNodes.Count; i++)
        {
            SkillTreeNodeData node = unlockNodes[i];
            if (IsSkillTreeNodeAcquired(node.nodeId, node.owner))
            {
                return true;
            }
        }

        return false;
    }

    public string GetTrainingUnlockRequirementLabel(TrainingData training)
    {
        if (training == null || training.unlockedByDefault)
        {
            return string.Empty;
        }

        List<SkillTreeNodeData> nodes = GetTrainingUnlockNodes(training.trainingId);
        if (nodes.Count == 0)
        {
            return "解放手段なし";
        }

        List<string> names = new List<string>();
        for (int i = 0; i < nodes.Count; i++)
        {
            string name = nodes[i].GetDisplayName();
            if (!names.Contains(name))
            {
                names.Add(name);
            }
        }
        return string.Join(" / ", names.ToArray());
    }

    public string GetTrainingDisplayName(string trainingId)
    {
        TrainingData[] trainings = GetTrainingDataList();
        for (int i = 0; i < trainings.Length; i++)
        {
            TrainingData training = trainings[i];
            if (training != null && string.Equals(
                training.trainingId,
                trainingId,
                StringComparison.Ordinal))
            {
                return training.GetDisplayName();
            }
        }

        return string.IsNullOrEmpty(trainingId) ? "訓練" : trainingId;
    }

    private List<SkillTreeNodeData> GetTrainingUnlockNodes(string trainingId)
    {
        List<SkillTreeNodeData> result = new List<SkillTreeNodeData>();
        if (string.IsNullOrEmpty(trainingId))
        {
            return result;
        }

        SkillTreeNodeData[] nodes = GetSkillTreeNodeDataList();
        for (int i = 0; i < nodes.Length; i++)
        {
            SkillTreeNodeData node = nodes[i];
            if (node == null ||
                (node.owner == SkillTreeOwner.Heroine && !IsSkillTreeNodeForCurrentHeroine(node)) ||
                node.unlockedTrainingIds == null)
            {
                continue;
            }

            for (int trainingIndex = 0;
                trainingIndex < node.unlockedTrainingIds.Count;
                trainingIndex++)
            {
                if (string.Equals(
                    node.unlockedTrainingIds[trainingIndex],
                    trainingId,
                    StringComparison.Ordinal))
                {
                    result.Add(node);
                    break;
                }
            }
        }

        result.Sort((left, right) => left.sortOrder.CompareTo(right.sortOrder));
        return result;
    }

    public bool IsSkillTreeNodeForCurrentHeroine(SkillTreeNodeData node)
    {
        if (node == null || node.owner != SkillTreeOwner.Heroine)
        {
            return node != null;
        }

        return string.IsNullOrEmpty(node.targetHeroineId) ||
            string.Equals(node.targetHeroineId, currentHeroineId, StringComparison.Ordinal);
    }

    public bool IsHeroineBattleSkillUnlocked(string skillId)
    {
        return IsHeroineBattleSkillUnlockedForHeroine(currentHeroineId, skillId);
    }

    private bool IsHeroineBattleSkillUnlockedForHeroine(string heroineId, string skillId)
    {
        if (string.IsNullOrEmpty(heroineId) || string.IsNullOrEmpty(skillId))
        {
            return false;
        }

        SkillTreeNodeData[] nodes = GetSkillTreeNodeDataList();
        for (int i = 0; i < nodes.Length; i++)
        {
            SkillTreeNodeData node = nodes[i];
            if (node != null &&
                node.owner == SkillTreeOwner.Heroine &&
                (string.IsNullOrEmpty(node.targetHeroineId) ||
                    string.Equals(node.targetHeroineId, heroineId, StringComparison.Ordinal)) &&
                string.Equals(node.grantedHeroineSkillId, skillId, StringComparison.Ordinal) &&
                IsSkillTreeNodeAcquired(node.nodeId, SkillTreeOwner.Heroine))
            {
                return true;
            }
        }

        return false;
    }

    public int HeroineBattleSkillSlotCount => DefaultHeroineBattleSkillSlotCount;

    public List<string> GetEquippedHeroineBattleSkillIds()
    {
        return GetEquippedHeroineBattleSkillIds(currentHeroineId);
    }

    public List<string> GetEquippedHeroineBattleSkillIds(string heroineId)
    {
        if (string.IsNullOrEmpty(heroineId))
        {
            return new List<string>();
        }

        NormalizeHeroineBattleSkillLoadout(heroineId, false);
        return new List<string>(GetOrCreateHeroineBattleSkillLoadout(heroineId));
    }

    public bool IsHeroineBattleSkillEquipped(string skillId)
    {
        return IsHeroineBattleSkillEquipped(currentHeroineId, skillId);
    }

    public bool IsHeroineBattleSkillEquipped(string heroineId, string skillId)
    {
        if (string.IsNullOrEmpty(heroineId) || string.IsNullOrEmpty(skillId))
        {
            return false;
        }

        NormalizeHeroineBattleSkillLoadout(heroineId, false);
        return GetOrCreateHeroineBattleSkillLoadout(heroineId).Contains(skillId);
    }

    public bool TryEquipHeroineBattleSkill(string skillId, out string message)
    {
        string heroineId = currentHeroineId;
        if (string.IsNullOrEmpty(heroineId))
        {
            message = "ヒロインが選択されていません。";
            return false;
        }

        NormalizeHeroineBattleSkillLoadout(heroineId, false);
        HeroineBattleSkillData skill = FindHeroineBattleSkillData(heroineId, skillId);
        if (!IsEquippableHeroineBattleSkill(heroineId, skill))
        {
            message = "現在のヒロインが習得済みの戦闘スキルだけを編成できます。";
            return false;
        }

        List<string> loadout = GetOrCreateHeroineBattleSkillLoadout(heroineId);
        if (loadout.Contains(skillId))
        {
            message = skill.GetDisplayName() + "はすでに編成中です。";
            return true;
        }

        if (loadout.Count >= DefaultHeroineBattleSkillSlotCount)
        {
            message = "ヒロインの編成枠がいっぱいです。先に別のスキルを外してください。";
            return false;
        }

        loadout.Add(skillId);
        message = skill.GetDisplayName() + "を編成しました。";
        return true;
    }

    public bool TryUnequipHeroineBattleSkill(string skillId, out string message)
    {
        string heroineId = currentHeroineId;
        if (string.IsNullOrEmpty(heroineId))
        {
            message = "ヒロインが選択されていません。";
            return false;
        }

        NormalizeHeroineBattleSkillLoadout(heroineId, false);
        HeroineBattleSkillData skill = FindHeroineBattleSkillData(heroineId, skillId);
        List<string> loadout = GetOrCreateHeroineBattleSkillLoadout(heroineId);
        if (string.IsNullOrEmpty(skillId) || !loadout.Remove(skillId))
        {
            message = "このヒロインスキルは編成されていません。";
            return false;
        }

        message = (skill != null ? skill.GetDisplayName() : skillId) + "を編成から外しました。";
        return true;
    }

    public bool TryToggleHeroineBattleSkill(string skillId, out string message)
    {
        return IsHeroineBattleSkillEquipped(skillId)
            ? TryUnequipHeroineBattleSkill(skillId, out message)
            : TryEquipHeroineBattleSkill(skillId, out message);
    }

    private void TryAutoEquipHeroineBattleSkill(string heroineId, string skillId)
    {
        if (string.IsNullOrEmpty(heroineId) || string.IsNullOrEmpty(skillId))
        {
            return;
        }

        NormalizeHeroineBattleSkillLoadout(heroineId, false);
        HeroineBattleSkillData skill = FindHeroineBattleSkillData(heroineId, skillId);
        List<string> loadout = GetOrCreateHeroineBattleSkillLoadout(heroineId);
        if (IsEquippableHeroineBattleSkill(heroineId, skill) &&
            loadout.Count < DefaultHeroineBattleSkillSlotCount &&
            !loadout.Contains(skillId))
        {
            loadout.Add(skillId);
        }
    }

    private bool IsEquippableHeroineBattleSkill(
        string heroineId,
        HeroineBattleSkillData skill)
    {
        return skill != null &&
            IsHeroineBattleSkillUnlockedForHeroine(heroineId, skill.skillId);
    }

    private HeroineBattleSkillData FindHeroineBattleSkillData(
        string heroineId,
        string skillId)
    {
        if (string.IsNullOrEmpty(heroineId) || string.IsNullOrEmpty(skillId))
        {
            return null;
        }

        HeroineProfileData profile = FindHeroineProfileById(heroineId);
        if (profile == null)
        {
            return null;
        }

        List<HeroineBattleSkillData> skills = profile.GetBattleSkills();
        for (int i = 0; i < skills.Count; i++)
        {
            HeroineBattleSkillData skill = skills[i];
            if (skill != null && string.Equals(skill.skillId, skillId, StringComparison.Ordinal))
            {
                return skill;
            }
        }

        return null;
    }

    private List<string> GetOrCreateHeroineBattleSkillLoadout(string heroineId)
    {
        List<string> loadout;
        if (!heroineBattleSkillLoadouts.TryGetValue(heroineId, out loadout))
        {
            loadout = new List<string>();
            heroineBattleSkillLoadouts[heroineId] = loadout;
        }

        return loadout;
    }

    private void NormalizeHeroineBattleSkillLoadout(string heroineId, bool fillEmptySlots)
    {
        if (string.IsNullOrEmpty(heroineId) || FindHeroineProfileById(heroineId) == null)
        {
            return;
        }

        List<string> loadout = GetOrCreateHeroineBattleSkillLoadout(heroineId);
        HashSet<string> seenIds = new HashSet<string>(StringComparer.Ordinal);
        List<string> normalizedIds = new List<string>();
        for (int i = 0;
            i < loadout.Count && normalizedIds.Count < DefaultHeroineBattleSkillSlotCount;
            i++)
        {
            string skillId = loadout[i];
            HeroineBattleSkillData skill = FindHeroineBattleSkillData(heroineId, skillId);
            if (IsEquippableHeroineBattleSkill(heroineId, skill) && seenIds.Add(skillId))
            {
                normalizedIds.Add(skillId);
            }
        }
        loadout.Clear();
        loadout.AddRange(normalizedIds);

        if (!fillEmptySlots)
        {
            return;
        }

        HeroineProfileData profile = FindHeroineProfileById(heroineId);
        List<HeroineBattleSkillData> skills = profile.GetBattleSkills();
        for (int i = 0;
            i < skills.Count && loadout.Count < DefaultHeroineBattleSkillSlotCount;
            i++)
        {
            HeroineBattleSkillData skill = skills[i];
            if (IsEquippableHeroineBattleSkill(heroineId, skill) && seenIds.Add(skill.skillId))
            {
                loadout.Add(skill.skillId);
            }
        }
    }

    private void NormalizeAllHeroineBattleSkillLoadouts(bool fillEmptySlots)
    {
        HeroineProfileData[] profiles = Resources.LoadAll<HeroineProfileData>("Heroines");
        HashSet<string> validHeroineIds = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < profiles.Length; i++)
        {
            HeroineProfileData profile = profiles[i];
            if (profile == null || string.IsNullOrEmpty(profile.heroineId)) continue;
            validHeroineIds.Add(profile.heroineId);
            NormalizeHeroineBattleSkillLoadout(profile.heroineId, fillEmptySlots);
        }

        List<string> savedHeroineIds = new List<string>(heroineBattleSkillLoadouts.Keys);
        for (int i = 0; i < savedHeroineIds.Count; i++)
        {
            if (!validHeroineIds.Contains(savedHeroineIds[i]))
            {
                heroineBattleSkillLoadouts.Remove(savedHeroineIds[i]);
            }
        }
    }

    private List<HeroineBattleSkillLoadoutEntry> CreateHeroineBattleSkillLoadoutSaveData()
    {
        NormalizeAllHeroineBattleSkillLoadouts(false);
        List<string> heroineIds = new List<string>(heroineBattleSkillLoadouts.Keys);
        heroineIds.Sort(StringComparer.Ordinal);
        List<HeroineBattleSkillLoadoutEntry> entries =
            new List<HeroineBattleSkillLoadoutEntry>();
        for (int i = 0; i < heroineIds.Count; i++)
        {
            string heroineId = heroineIds[i];
            entries.Add(new HeroineBattleSkillLoadoutEntry
            {
                heroineId = heroineId,
                equippedSkillIds = new List<string>(heroineBattleSkillLoadouts[heroineId])
            });
        }

        return entries;
    }

    private void LoadHeroineBattleSkillLoadouts(List<HeroineBattleSkillLoadoutEntry> entries)
    {
        heroineBattleSkillLoadouts.Clear();
        if (entries == null)
        {
            return;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            HeroineBattleSkillLoadoutEntry entry = entries[i];
            if (entry == null || string.IsNullOrEmpty(entry.heroineId)) continue;
            List<string> loadout = GetOrCreateHeroineBattleSkillLoadout(entry.heroineId);
            if (entry.equippedSkillIds != null)
            {
                loadout.AddRange(entry.equippedSkillIds);
            }
        }
    }

    public string GetHeroineBattleSkillDisplayName(string skillId)
    {
        if (heroineProfile != null)
        {
            List<HeroineBattleSkillData> skills = heroineProfile.GetBattleSkills();
            for (int i = 0; i < skills.Count; i++)
            {
                HeroineBattleSkillData skill = skills[i];
                if (skill != null &&
                    string.Equals(skill.skillId, skillId, StringComparison.Ordinal))
                {
                    return skill.GetDisplayName();
                }
            }
        }

        return string.IsNullOrEmpty(skillId) ? "ヒロインスキル" : skillId;
    }

    public string GetSkillTreeConditionTargetDisplayName(SkillTreeUnlockCondition condition)
    {
        if (condition == null || string.IsNullOrEmpty(condition.targetId))
        {
            return string.Empty;
        }

        if (condition.scope == SkillTreeProgressScope.Training)
        {
            TrainingData[] trainings = GetTrainingDataList();
            for (int i = 0; i < trainings.Length; i++)
            {
                TrainingData training = trainings[i];
                if (training != null &&
                    string.Equals(training.trainingId, condition.targetId, StringComparison.Ordinal))
                {
                    return training.GetDisplayName();
                }
            }
        }

        if (condition.scope == SkillTreeProgressScope.TrainingCategory)
        {
            switch (condition.targetId)
            {
                case "Fundamentals": return "基礎";
                case "Combat": return "実戦";
                case "Endurance": return "持久";
            }
        }

        if (condition.scope == SkillTreeProgressScope.Enemy)
        {
            EnemyData[] enemies = Resources.LoadAll<EnemyData>("Enemies");
            for (int i = 0; i < enemies.Length; i++)
            {
                EnemyData enemy = enemies[i];
                if (enemy != null &&
                    string.Equals(enemy.enemyId, condition.targetId, StringComparison.Ordinal))
                {
                    return enemy.GetDisplayName();
                }
            }
        }

        return condition.targetId;
    }

    private SkillTreeNodeData[] GetSkillTreeNodeDataList()
    {
        if (skillTreeNodeDataList == null)
        {
            skillTreeNodeDataList = Resources.LoadAll<SkillTreeNodeData>(SkillTreeNodeResourcePath);
        }

        return skillTreeNodeDataList;
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    private void ValidateSkillTreeDataOnStartup()
    {
        SkillTreeDataValidator.ValidateResources().Log();
        GameEventDataValidator.ValidateResources().Log();
    }

    public SkillTreeNodeEvaluation EvaluateSkillTreeNode(SkillTreeNodeData node)
    {
        SkillTreeNodeEvaluation evaluation = new SkillTreeNodeEvaluation
        {
            node = node,
            state = SkillTreeNodeState.Locked
        };
        if (node == null || string.IsNullOrEmpty(node.nodeId))
        {
            return evaluation;
        }

        if (node.owner == SkillTreeOwner.Heroine &&
            (!IsSkillTreeNodeForCurrentHeroine(node) ||
                (string.IsNullOrEmpty(node.grantedHeroineSkillId) &&
                    !IsTrainingSkillData(node.skill) &&
                    !HasTrainingUnlocks(node))))
        {
            return evaluation;
        }

        evaluation.currentSkillPoints = node.owner == SkillTreeOwner.Player
            ? playerSkillPoints
            : heroineSkillPoints;
        evaluation.requiredSkillPoints = Mathf.Max(0, node.skillPointCost);
        if (IsSkillTreeNodeAcquired(node.nodeId, node.owner))
        {
            evaluation.state = SkillTreeNodeState.Acquired;
            return evaluation;
        }

        if (node.prerequisiteNodes != null)
        {
            for (int i = 0; i < node.prerequisiteNodes.Count; i++)
            {
                SkillTreeNodeData prerequisite = node.prerequisiteNodes[i];
                if (prerequisite != null &&
                    !IsSkillTreeNodeAcquired(prerequisite.nodeId, prerequisite.owner))
                {
                    evaluation.missingPrerequisiteNodeIds.Add(prerequisite.nodeId);
                }
            }
        }

        bool conditionsMet = true;
        if (node.unlockConditions != null)
        {
            for (int i = 0; i < node.unlockConditions.Count; i++)
            {
                SkillTreeUnlockCondition condition = node.unlockConditions[i];
                if (condition == null)
                {
                    continue;
                }

                SkillTreeConditionProgress progress = new SkillTreeConditionProgress
                {
                    condition = condition,
                    currentValue = GetSkillTreeConditionValue(condition),
                    requiredValue = Mathf.Max(0, condition.requiredValue)
                };
                evaluation.conditions.Add(progress);
                if (!progress.IsMet)
                {
                    conditionsMet = false;
                }
            }
        }

        if (evaluation.missingPrerequisiteNodeIds.Count > 0 || !conditionsMet)
        {
            evaluation.state = SkillTreeNodeState.Locked;
        }
        else if (evaluation.currentSkillPoints < evaluation.requiredSkillPoints)
        {
            evaluation.state = SkillTreeNodeState.InsufficientPoints;
        }
        else
        {
            evaluation.state = SkillTreeNodeState.Available;
        }

        return evaluation;
    }

    public bool TryAcquireSkillTreeNode(SkillTreeNodeData node)
    {
        SkillTreeNodeEvaluation evaluation = EvaluateSkillTreeNode(node);
        if (evaluation.state != SkillTreeNodeState.Available)
        {
            return false;
        }

        bool spent = node.owner == SkillTreeOwner.Player
            ? TrySpendPlayerSkillPoints(evaluation.requiredSkillPoints)
            : TrySpendHeroineSkillPoints(evaluation.requiredSkillPoints);
        if (!spent && evaluation.requiredSkillPoints > 0)
        {
            return false;
        }

        GetSkillTreeNodeSet(node.owner).Add(node.nodeId);
        if (node.owner == SkillTreeOwner.Player && node.skill != null)
        {
            UnlockSkill(node.skill.skillId);
            TryAutoEquipPlayerBattleSkill(node.skill);
            TryAutoActivatePlayerTrainingSkill(node.skill);
        }
        else if (node.owner == SkillTreeOwner.Heroine)
        {
            string heroineId = string.IsNullOrEmpty(node.targetHeroineId)
                ? currentHeroineId
                : node.targetHeroineId;
            if (IsTrainingSkillData(node.skill))
            {
                TryAutoActivateHeroineTrainingSkill(heroineId, node.skill);
            }
            else if (!string.IsNullOrEmpty(node.grantedHeroineSkillId))
            {
                TryAutoEquipHeroineBattleSkill(heroineId, node.grantedHeroineSkillId);
            }
        }

        RefreshStatusDetailPanel();
        Debug.Log(
            "[SkillTree] Acquired node=" + node.nodeId +
            " / owner=" + node.owner +
            " / cost=" + evaluation.requiredSkillPoints);
        return true;
    }

    private static bool HasTrainingUnlocks(SkillTreeNodeData node)
    {
        if (node == null || node.unlockedTrainingIds == null)
        {
            return false;
        }

        for (int i = 0; i < node.unlockedTrainingIds.Count; i++)
        {
            if (!string.IsNullOrWhiteSpace(node.unlockedTrainingIds[i]))
            {
                return true;
            }
        }

        return false;
    }

    private int GetSkillTreeConditionValue(SkillTreeUnlockCondition condition)
    {
        switch (condition.conditionType)
        {
            case SkillTreeConditionType.TrainingProficiency:
                return GetTrainingProficiency(condition.targetId);
            case SkillTreeConditionType.TrainingCount:
                return GetTrainingProgressValue(condition, 0);
            case SkillTreeConditionType.PlayerLpConsumedCount:
                return GetTrainingProgressValue(condition, 1);
            case SkillTreeConditionType.OpponentLpConsumedCount:
                return GetTrainingProgressValue(condition, 2);
            case SkillTreeConditionType.MonsterDefeatCount:
                return GetMonsterDefeatProgressValue(condition);
            case SkillTreeConditionType.Affection:
                return heroineStatus != null ? heroineStatus.Affection : 0;
            case SkillTreeConditionType.Day:
                return timeManager != null ? timeManager.Day : 0;
            default:
                return 0;
        }
    }

    private int GetTrainingProgressValue(SkillTreeUnlockCondition condition, int valueKind)
    {
        if (condition.scope == SkillTreeProgressScope.Total)
        {
            return valueKind == 0
                ? skillProgressStats.totalTrainingCount
                : valueKind == 1
                    ? skillProgressStats.playerLpConsumedCount
                    : skillProgressStats.opponentLpConsumedCount;
        }

        if (condition.scope == SkillTreeProgressScope.Training)
        {
            for (int i = 0; i < skillProgressStats.trainingStats.Count; i++)
            {
                TrainingProgressStatEntry entry = skillProgressStats.trainingStats[i];
                if (entry != null && string.Equals(
                    entry.trainingId,
                    condition.targetId,
                    StringComparison.Ordinal))
                {
                    return valueKind == 0
                        ? entry.trainingCount
                        : valueKind == 1
                            ? entry.playerLpConsumedCount
                            : entry.opponentLpConsumedCount;
                }
            }
        }

        if (condition.scope == SkillTreeProgressScope.TrainingCategory)
        {
            for (int i = 0; i < skillProgressStats.trainingCategoryStats.Count; i++)
            {
                TrainingCategoryProgressStatEntry entry =
                    skillProgressStats.trainingCategoryStats[i];
                if (entry != null && string.Equals(
                    entry.trainingCategoryId,
                    condition.targetId,
                    StringComparison.Ordinal))
                {
                    return valueKind == 0
                        ? entry.trainingCount
                        : valueKind == 1
                            ? entry.playerLpConsumedCount
                            : entry.opponentLpConsumedCount;
                }
            }
        }

        return 0;
    }

    private int GetMonsterDefeatProgressValue(SkillTreeUnlockCondition condition)
    {
        if (condition.scope != SkillTreeProgressScope.Enemy)
        {
            return skillProgressStats.totalMonsterDefeatCount;
        }

        for (int i = 0; i < skillProgressStats.enemyDefeatStats.Count; i++)
        {
            EnemyDefeatStatEntry entry = skillProgressStats.enemyDefeatStats[i];
            if (entry != null && string.Equals(
                entry.enemyId,
                condition.targetId,
                StringComparison.Ordinal))
            {
                return entry.defeatCount;
            }
        }

        return 0;
    }

    private HashSet<string> GetSkillTreeNodeSet(SkillTreeOwner owner)
    {
        return owner == SkillTreeOwner.Player
            ? acquiredPlayerSkillTreeNodeIds
            : acquiredHeroineSkillTreeNodeIds;
    }

    private static void LoadSkillTreeNodeIds(HashSet<string> target, List<string> source)
    {
        target.Clear();
        if (source == null)
        {
            return;
        }

        for (int i = 0; i < source.Count; i++)
        {
            if (!string.IsNullOrEmpty(source[i]))
            {
                target.Add(source[i]);
            }
        }
    }

    public List<SkillData> GetUnlockedBattleSkills()
    {
        List<SkillData> skills = new List<SkillData>();
        foreach (SkillData skill in GetSkillDataList())
        {
            if (skill != null &&
                skill.isEnabled &&
                skill.category == SkillCategory.Battle &&
                skill.canUseInBattle &&
                IsSkillUnlocked(skill.skillId))
            {
                skills.Add(skill);
            }
        }

        return skills;
    }

    public List<SkillData> GetEquippedPlayerBattleSkills()
    {
        NormalizeEquippedPlayerBattleSkills(false);
        List<SkillData> result = new List<SkillData>();
        for (int i = 0; i < equippedPlayerBattleSkillIds.Count; i++)
        {
            SkillData skill = FindSkillData(equippedPlayerBattleSkillIds[i]);
            if (IsEquippablePlayerBattleSkill(skill))
            {
                result.Add(skill);
            }
        }

        return result;
    }

    private void NormalizeEquippedPlayerBattleSkills(bool fillEmptySlots)
    {
        HashSet<string> seenIds = new HashSet<string>(StringComparer.Ordinal);
        List<string> normalizedIds = new List<string>();
        for (int i = 0;
            i < equippedPlayerBattleSkillIds.Count &&
                normalizedIds.Count < DefaultPlayerBattleSkillSlotCount;
            i++)
        {
            string skillId = equippedPlayerBattleSkillIds[i];
            SkillData skill = FindSkillData(skillId);
            if (IsEquippablePlayerBattleSkill(skill) && seenIds.Add(skillId))
            {
                normalizedIds.Add(skillId);
            }
        }
        equippedPlayerBattleSkillIds.Clear();
        equippedPlayerBattleSkillIds.AddRange(normalizedIds);

        if (!fillEmptySlots)
        {
            return;
        }

        List<SkillData> unlockedBattleSkills = GetUnlockedBattleSkills();
        for (int i = 0;
            i < unlockedBattleSkills.Count &&
                equippedPlayerBattleSkillIds.Count < DefaultPlayerBattleSkillSlotCount;
            i++)
        {
            SkillData skill = unlockedBattleSkills[i];
            if (skill != null && seenIds.Add(skill.skillId))
            {
                equippedPlayerBattleSkillIds.Add(skill.skillId);
            }
        }
    }

    private bool IsEquippablePlayerBattleSkill(SkillData skill)
    {
        return skill != null &&
            skill.isEnabled &&
            skill.category == SkillCategory.Battle &&
            skill.canUseInBattle &&
            IsSkillUnlocked(skill.skillId);
    }

    private void TryAutoEquipPlayerBattleSkill(SkillData skill)
    {
        NormalizeEquippedPlayerBattleSkills(false);
        if (IsEquippablePlayerBattleSkill(skill) &&
            equippedPlayerBattleSkillIds.Count < DefaultPlayerBattleSkillSlotCount &&
            !equippedPlayerBattleSkillIds.Contains(skill.skillId))
        {
            equippedPlayerBattleSkillIds.Add(skill.skillId);
        }
    }

    private static bool IsTrainingSkillData(SkillData skill)
    {
        return skill != null &&
            skill.isEnabled &&
            skill.category == SkillCategory.Training &&
            skill.canUseInTraining;
    }

    private bool IsActivatablePlayerTrainingSkill(SkillData skill)
    {
        return IsTrainingSkillData(skill) && IsSkillUnlocked(skill.skillId);
    }

    private bool IsActivatableHeroineTrainingSkill(string heroineId, SkillData skill)
    {
        if (string.IsNullOrEmpty(heroineId) || !IsTrainingSkillData(skill))
        {
            return false;
        }

        SkillTreeNodeData[] nodes = GetSkillTreeNodeDataList();
        for (int i = 0; i < nodes.Length; i++)
        {
            SkillTreeNodeData node = nodes[i];
            if (node != null &&
                node.owner == SkillTreeOwner.Heroine &&
                node.skill != null &&
                string.Equals(node.skill.skillId, skill.skillId, StringComparison.Ordinal) &&
                (string.IsNullOrEmpty(node.targetHeroineId) ||
                    string.Equals(node.targetHeroineId, heroineId, StringComparison.Ordinal)) &&
                IsSkillTreeNodeAcquired(node.nodeId, SkillTreeOwner.Heroine))
            {
                return true;
            }
        }

        return false;
    }

    private void NormalizeActivePlayerTrainingSkills(bool activateAllAcquired)
    {
        List<string> savedIds = new List<string>(activePlayerTrainingSkillIds);
        activePlayerTrainingSkillIds.Clear();
        for (int i = 0; i < savedIds.Count; i++)
        {
            SkillData skill = FindSkillData(savedIds[i]);
            if (IsActivatablePlayerTrainingSkill(skill))
            {
                activePlayerTrainingSkillIds.Add(skill.skillId);
            }
        }

        if (!activateAllAcquired)
        {
            return;
        }

        SkillData[] skills = GetSkillDataList();
        for (int i = 0; i < skills.Length; i++)
        {
            SkillData skill = skills[i];
            if (IsActivatablePlayerTrainingSkill(skill))
            {
                activePlayerTrainingSkillIds.Add(skill.skillId);
            }
        }
    }

    private HashSet<string> GetOrCreateActiveHeroineTrainingSkillIds(string heroineId)
    {
        HashSet<string> activeIds;
        if (!activeHeroineTrainingSkillIds.TryGetValue(heroineId, out activeIds))
        {
            activeIds = new HashSet<string>(StringComparer.Ordinal);
            activeHeroineTrainingSkillIds[heroineId] = activeIds;
        }

        return activeIds;
    }

    private void NormalizeActiveHeroineTrainingSkills(
        string heroineId,
        bool activateAllAcquired)
    {
        if (string.IsNullOrEmpty(heroineId) || FindHeroineProfileById(heroineId) == null)
        {
            return;
        }

        HashSet<string> activeIds = GetOrCreateActiveHeroineTrainingSkillIds(heroineId);
        List<string> savedIds = new List<string>(activeIds);
        activeIds.Clear();
        for (int i = 0; i < savedIds.Count; i++)
        {
            SkillData skill = FindSkillData(savedIds[i]);
            if (IsActivatableHeroineTrainingSkill(heroineId, skill))
            {
                activeIds.Add(skill.skillId);
            }
        }

        if (!activateAllAcquired)
        {
            return;
        }

        SkillTreeNodeData[] nodes = GetSkillTreeNodeDataList();
        for (int i = 0; i < nodes.Length; i++)
        {
            SkillTreeNodeData node = nodes[i];
            if (node == null ||
                node.owner != SkillTreeOwner.Heroine ||
                node.skill == null ||
                (!string.IsNullOrEmpty(node.targetHeroineId) &&
                    !string.Equals(node.targetHeroineId, heroineId, StringComparison.Ordinal)))
            {
                continue;
            }

            if (IsSkillTreeNodeAcquired(node.nodeId, SkillTreeOwner.Heroine) &&
                IsTrainingSkillData(node.skill))
            {
                activeIds.Add(node.skill.skillId);
            }
        }
    }

    private void NormalizeAllActiveHeroineTrainingSkills(bool activateAllAcquired)
    {
        HeroineProfileData[] profiles = Resources.LoadAll<HeroineProfileData>("Heroines");
        HashSet<string> validHeroineIds = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < profiles.Length; i++)
        {
            HeroineProfileData profile = profiles[i];
            if (profile == null || string.IsNullOrEmpty(profile.heroineId)) continue;
            validHeroineIds.Add(profile.heroineId);
            NormalizeActiveHeroineTrainingSkills(profile.heroineId, activateAllAcquired);
        }

        List<string> savedHeroineIds =
            new List<string>(activeHeroineTrainingSkillIds.Keys);
        for (int i = 0; i < savedHeroineIds.Count; i++)
        {
            if (!validHeroineIds.Contains(savedHeroineIds[i]))
            {
                activeHeroineTrainingSkillIds.Remove(savedHeroineIds[i]);
            }
        }
    }

    private void TryAutoActivatePlayerTrainingSkill(SkillData skill)
    {
        if (IsActivatablePlayerTrainingSkill(skill))
        {
            activePlayerTrainingSkillIds.Add(skill.skillId);
        }
    }

    private void TryAutoActivateHeroineTrainingSkill(string heroineId, SkillData skill)
    {
        if (IsActivatableHeroineTrainingSkill(heroineId, skill))
        {
            GetOrCreateActiveHeroineTrainingSkillIds(heroineId).Add(skill.skillId);
        }
    }

    private void LoadActivePlayerTrainingSkills(List<string> skillIds)
    {
        activePlayerTrainingSkillIds.Clear();
        if (skillIds == null) return;
        for (int i = 0; i < skillIds.Count; i++)
        {
            if (!string.IsNullOrEmpty(skillIds[i]))
            {
                activePlayerTrainingSkillIds.Add(skillIds[i]);
            }
        }
    }

    private void LoadActiveHeroineTrainingSkills(
        List<HeroineTrainingSkillActivationEntry> entries)
    {
        activeHeroineTrainingSkillIds.Clear();
        if (entries == null) return;
        for (int i = 0; i < entries.Count; i++)
        {
            HeroineTrainingSkillActivationEntry entry = entries[i];
            if (entry == null || string.IsNullOrEmpty(entry.heroineId)) continue;
            HashSet<string> activeIds =
                GetOrCreateActiveHeroineTrainingSkillIds(entry.heroineId);
            if (entry.activeSkillIds == null) continue;
            for (int j = 0; j < entry.activeSkillIds.Count; j++)
            {
                if (!string.IsNullOrEmpty(entry.activeSkillIds[j]))
                {
                    activeIds.Add(entry.activeSkillIds[j]);
                }
            }
        }
    }

    private List<HeroineTrainingSkillActivationEntry>
        CreateHeroineTrainingSkillActivationSaveData()
    {
        NormalizeAllActiveHeroineTrainingSkills(false);
        List<string> heroineIds = new List<string>(activeHeroineTrainingSkillIds.Keys);
        heroineIds.Sort(StringComparer.Ordinal);
        List<HeroineTrainingSkillActivationEntry> entries =
            new List<HeroineTrainingSkillActivationEntry>();
        for (int i = 0; i < heroineIds.Count; i++)
        {
            string heroineId = heroineIds[i];
            entries.Add(new HeroineTrainingSkillActivationEntry
            {
                heroineId = heroineId,
                activeSkillIds = CreateSortedSkillIdList(
                    activeHeroineTrainingSkillIds[heroineId])
            });
        }

        return entries;
    }

    private static List<string> CreateSortedSkillIdList(IEnumerable<string> skillIds)
    {
        List<string> result = new List<string>();
        if (skillIds != null)
        {
            foreach (string skillId in skillIds)
            {
                if (!string.IsNullOrEmpty(skillId))
                {
                    result.Add(skillId);
                }
            }
        }
        result.Sort(StringComparer.Ordinal);
        return result;
    }

    private List<SkillData> CreateTrainingSkillDataList(IEnumerable<string> skillIds)
    {
        List<string> sortedIds = CreateSortedSkillIdList(skillIds);
        List<SkillData> result = new List<SkillData>();
        for (int i = 0; i < sortedIds.Count; i++)
        {
            SkillData skill = FindSkillData(sortedIds[i]);
            if (IsTrainingSkillData(skill))
            {
                result.Add(skill);
            }
        }

        return result;
    }

    private SkillData FindSkillData(string skillId)
    {
        if (string.IsNullOrEmpty(skillId))
        {
            return null;
        }

        SkillData[] skills = GetSkillDataList();
        for (int i = 0; i < skills.Length; i++)
        {
            SkillData skill = skills[i];
            if (skill != null && string.Equals(skill.skillId, skillId, StringComparison.Ordinal))
            {
                return skill;
            }
        }

        return null;
    }

    public bool MeetsSkillUnlockConditions(SkillData skill)
    {
        if (skill == null || !skill.isEnabled)
        {
            return false;
        }

        if (heroineStatus != null && heroineStatus.Affection < skill.requiredAffection)
        {
            return false;
        }

        if (timeManager != null && timeManager.Day < skill.requiredDay)
        {
            return false;
        }

        if (!string.IsNullOrEmpty(skill.requiredTrainingId) &&
            GetTrainingProficiency(skill.requiredTrainingId) < skill.requiredTrainingProficiency)
        {
            return false;
        }

        foreach (string requiredSkillId in skill.GetRequiredSkillIds())
        {
            if (!string.IsNullOrEmpty(requiredSkillId) && !IsSkillUnlocked(requiredSkillId))
            {
                return false;
            }
        }

        return true;
    }

    public string GetSkillUnlockConditionText(SkillData skill)
    {
        if (skill == null)
        {
            return "スキルデータがありません。";
        }

        List<string> conditions = new List<string>();
        if (skill.requiredAffection > 0)
        {
            conditions.Add("必要好感度: " + skill.requiredAffection);
        }

        if (skill.requiredDay > 1)
        {
            conditions.Add("必要日数: Day " + skill.requiredDay);
        }

        if (!string.IsNullOrEmpty(skill.requiredTrainingId))
        {
            conditions.Add(
                "必要訓練熟練度: " +
                skill.requiredTrainingId +
                " " +
                skill.requiredTrainingProficiency);
        }

        List<string> requiredSkillIds = skill.GetRequiredSkillIds();
        if (requiredSkillIds.Count > 0)
        {
            conditions.Add("必要スキル: " + string.Join(", ", requiredSkillIds.ToArray()));
        }

        return conditions.Count > 0 ? string.Join("\n", conditions.ToArray()) : "解放条件なし";
    }

    public void OpenSkillPanel()
    {
        EnsureSkillTreePanel();
        if (skillTreePanel != null)
        {
            skillTreePanel.Open(this);
            return;
        }

        EnsureSkillPanel();
        if (skillPanel == null)
        {
            ShowSystemMessage("スキル画面が設定されていないため、開けません。");
            return;
        }

        skillPanel.OpenSkillList(this);
    }

    public bool TryOpenBattleSkillSelection(Action<SkillData> onSelected)
    {
        if (GetEquippedPlayerBattleSkills().Count == 0)
        {
            return false;
        }

        EnsureSkillPanel();
        if (skillPanel == null)
        {
            return false;
        }

        skillPanel.OpenBattleSkillSelection(this, onSelected);
        return true;
    }

    public bool IsPurchasedItem(string itemId)
    {
        return !string.IsNullOrEmpty(itemId) && purchasedItemIds.Contains(itemId);
    }

    public List<string> GetPurchasedItemIds()
    {
        return new List<string>(purchasedItemIds);
    }

    public int GetItemQuantity(string itemId)
    {
        return !string.IsNullOrEmpty(itemId) && itemQuantities.TryGetValue(itemId, out int quantity) ? quantity : 0;
    }

    public bool TryConsumeBattleItem(string itemId, out ShopItemData item)
    {
        item = Resources.Load<ShopItemData>("ShopItems/" + itemId);
        if (item == null || !item.isBattleConsumable || GetItemQuantity(itemId) <= 0)
        {
            return false;
        }

        itemQuantities[itemId]--;
        return true;
    }

    public List<ShopItemData> GetBattleItems()
    {
        List<ShopItemData> items = new List<ShopItemData>();
        foreach (ShopItemData item in Resources.LoadAll<ShopItemData>("ShopItems"))
            if (item != null && item.isBattleConsumable && GetItemQuantity(item.itemId) > 0) items.Add(item);
        return items;
    }

    public List<string> GetUnlockedOutfitIds()
    {
        return new List<string>(unlockedOutfitIds);
    }

    public int GetTrainingProficiency(string trainingId)
    {
        if (string.IsNullOrEmpty(trainingId))
        {
            return 0;
        }

        return trainingProficiencies.TryGetValue(trainingId, out int value)
            ? value
            : 0;
    }

    public SkillProgressStats GetSkillProgressStats()
    {
        return skillProgressStats.Clone();
    }

    public int PlayerSkillPoints => playerSkillPoints;
    public int HeroineSkillPoints => heroineSkillPoints;

    public bool TrySpendPlayerSkillPoints(int amount)
    {
        bool spent = TrySpendSkillPoints(ref playerSkillPoints, amount);
        if (spent)
        {
            RefreshStatusDetailPanel();
        }
        return spent;
    }

    public bool TrySpendHeroineSkillPoints(int amount)
    {
        bool spent = TrySpendSkillPoints(ref heroineSkillPoints, amount);
        if (spent)
        {
            RefreshStatusDetailPanel();
        }
        return spent;
    }

    private static bool TrySpendSkillPoints(ref int currentPoints, int amount)
    {
        if (amount <= 0 || currentPoints < amount)
        {
            return false;
        }

        currentPoints -= amount;
        return true;
    }

    private static int AddSkillPoints(int currentPoints, int amount)
    {
        if (amount <= 0)
        {
            return Mathf.Max(0, currentPoints);
        }

        long total = (long)Mathf.Max(0, currentPoints) + amount;
        return total > int.MaxValue ? int.MaxValue : (int)total;
    }

    private void RecordTrainingProgress(TrainingResult result)
    {
        if (result == null || result.elapsedSteps <= 0)
        {
            return;
        }

        skillProgressStats.totalTrainingCount++;
        skillProgressStats.playerLpConsumedCount += Math.Max(0, result.playerLpConsumedCount);
        skillProgressStats.opponentLpConsumedCount += Math.Max(0, result.opponentLpConsumedCount);

        HashSet<string> countedTrainingIds = new HashSet<string>();
        Dictionary<string, TrainingCategoryProgressStatEntry> categoryChanges =
            new Dictionary<string, TrainingCategoryProgressStatEntry>();
        if (result.progressEntries != null)
        {
            for (int i = 0; i < result.progressEntries.Count; i++)
            {
                TrainingProgressEntry progress = result.progressEntries[i];
                if (progress == null || progress.elapsedSteps <= 0)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(progress.trainingId))
                {
                    TrainingProgressStatEntry trainingStat =
                        GetOrCreateTrainingProgressStat(progress.trainingId);
                    if (countedTrainingIds.Add(progress.trainingId))
                    {
                        trainingStat.trainingCount++;
                    }
                    trainingStat.playerLpConsumedCount += Math.Max(0, progress.playerLpConsumedCount);
                    trainingStat.opponentLpConsumedCount += Math.Max(0, progress.opponentLpConsumedCount);
                }

                if (!string.IsNullOrEmpty(progress.trainingCategoryId))
                {
                    if (!categoryChanges.TryGetValue(
                        progress.trainingCategoryId,
                        out TrainingCategoryProgressStatEntry categoryChange))
                    {
                        categoryChange = new TrainingCategoryProgressStatEntry
                        {
                            trainingCategoryId = progress.trainingCategoryId,
                            trainingCount = 1
                        };
                        categoryChanges.Add(progress.trainingCategoryId, categoryChange);
                    }

                    categoryChange.playerLpConsumedCount +=
                        Math.Max(0, progress.playerLpConsumedCount);
                    categoryChange.opponentLpConsumedCount +=
                        Math.Max(0, progress.opponentLpConsumedCount);
                }
            }
        }

        foreach (KeyValuePair<string, TrainingCategoryProgressStatEntry> pair in categoryChanges)
        {
            TrainingCategoryProgressStatEntry categoryStat =
                GetOrCreateTrainingCategoryProgressStat(pair.Key);
            categoryStat.trainingCount += pair.Value.trainingCount;
            categoryStat.playerLpConsumedCount += pair.Value.playerLpConsumedCount;
            categoryStat.opponentLpConsumedCount += pair.Value.opponentLpConsumedCount;
        }

        Debug.Log(
            "[SkillProgress] Training=" + skillProgressStats.totalTrainingCount +
            " / PlayerLp=" + skillProgressStats.playerLpConsumedCount +
            " / OpponentLp=" + skillProgressStats.opponentLpConsumedCount);
    }

    private TrainingProgressStatEntry GetOrCreateTrainingProgressStat(string trainingId)
    {
        for (int i = 0; i < skillProgressStats.trainingStats.Count; i++)
        {
            TrainingProgressStatEntry entry = skillProgressStats.trainingStats[i];
            if (entry != null && string.Equals(entry.trainingId, trainingId, StringComparison.Ordinal))
            {
                return entry;
            }
        }

        TrainingProgressStatEntry created = new TrainingProgressStatEntry { trainingId = trainingId };
        skillProgressStats.trainingStats.Add(created);
        return created;
    }

    private TrainingCategoryProgressStatEntry GetOrCreateTrainingCategoryProgressStat(
        string trainingCategoryId)
    {
        for (int i = 0; i < skillProgressStats.trainingCategoryStats.Count; i++)
        {
            TrainingCategoryProgressStatEntry entry = skillProgressStats.trainingCategoryStats[i];
            if (entry != null && string.Equals(
                entry.trainingCategoryId,
                trainingCategoryId,
                StringComparison.Ordinal))
            {
                return entry;
            }
        }

        TrainingCategoryProgressStatEntry created = new TrainingCategoryProgressStatEntry
        {
            trainingCategoryId = trainingCategoryId
        };
        skillProgressStats.trainingCategoryStats.Add(created);
        return created;
    }

    private void RecordMonsterDefeat(string enemyId)
    {
        skillProgressStats.totalMonsterDefeatCount++;
        if (!string.IsNullOrEmpty(enemyId))
        {
            EnemyDefeatStatEntry enemyStat = null;
            for (int i = 0; i < skillProgressStats.enemyDefeatStats.Count; i++)
            {
                EnemyDefeatStatEntry candidate = skillProgressStats.enemyDefeatStats[i];
                if (candidate != null &&
                    string.Equals(candidate.enemyId, enemyId, StringComparison.Ordinal))
                {
                    enemyStat = candidate;
                    break;
                }
            }

            if (enemyStat == null)
            {
                enemyStat = new EnemyDefeatStatEntry { enemyId = enemyId };
                skillProgressStats.enemyDefeatStats.Add(enemyStat);
            }
            enemyStat.defeatCount++;
        }

        Debug.Log(
            "[SkillProgress] MonsterDefeats=" + skillProgressStats.totalMonsterDefeatCount +
            " / EnemyId=" + enemyId);
    }

    private int AddTrainingProficiency(string trainingId, int value)
    {
        if (string.IsNullOrEmpty(trainingId) || value == 0)
        {
            return GetTrainingProficiency(trainingId);
        }

        int currentValue = GetTrainingProficiency(trainingId);
        long total = (long)currentValue + value;
        int nextValue = (int)Math.Max(0L, Math.Min(MaxTrainingProficiency, total));
        trainingProficiencies[trainingId] = nextValue;
        return nextValue;
    }

    private List<TrainingProficiencyEntry> CreateTrainingProficiencySaveData()
    {
        List<TrainingProficiencyEntry> entries = new List<TrainingProficiencyEntry>();
        foreach (KeyValuePair<string, int> pair in trainingProficiencies)
        {
            if (string.IsNullOrEmpty(pair.Key))
            {
                continue;
            }

            entries.Add(new TrainingProficiencyEntry
            {
                trainingId = pair.Key,
                proficiency = Mathf.Clamp(pair.Value, 0, MaxTrainingProficiency)
            });
        }

        entries.Sort((a, b) => string.Compare(a.trainingId, b.trainingId, StringComparison.Ordinal));
        return entries;
    }

    private void LoadTrainingProficiencies(List<TrainingProficiencyEntry> entries)
    {
        trainingProficiencies.Clear();
        if (entries == null)
        {
            return;
        }

        foreach (TrainingProficiencyEntry entry in entries)
        {
            if (entry == null || string.IsNullOrEmpty(entry.trainingId))
            {
                continue;
            }

            trainingProficiencies[entry.trainingId] =
                Mathf.Clamp(entry.proficiency, 0, MaxTrainingProficiency);
        }
    }

    private void RegisterPurchasedItem(string itemId)
    {
        if (!string.IsNullOrEmpty(itemId))
        {
            purchasedItemIds.Add(itemId);
        }
    }

    private List<ItemQuantityEntry> CreateItemQuantitySaveData()
    {
        List<ItemQuantityEntry> entries = new List<ItemQuantityEntry>();
        foreach (KeyValuePair<string, int> pair in itemQuantities)
        {
            if (!string.IsNullOrEmpty(pair.Key) && pair.Value > 0)
            {
                entries.Add(new ItemQuantityEntry { itemId = pair.Key, quantity = pair.Value });
            }
        }

        entries.Sort((a, b) => string.Compare(a.itemId, b.itemId, StringComparison.Ordinal));
        return entries;
    }

    private void LoadItemQuantities(List<ItemQuantityEntry> entries)
    {
        itemQuantities.Clear();
        if (entries == null)
        {
            return;
        }

        foreach (ItemQuantityEntry entry in entries)
        {
            if (entry != null && !string.IsNullOrEmpty(entry.itemId) && entry.quantity > 0)
            {
                itemQuantities[entry.itemId] = entry.quantity;
            }
        }
    }

    private void RegisterUnlockedOutfit(string outfitId)
    {
        if (string.IsNullOrEmpty(outfitId))
        {
            return;
        }

        unlockedOutfitIds.Add(outfitId);
        ApplyUnlockedOutfitsToManager();
    }

    private void RegisterUnlockedOutfits(IEnumerable<string> outfitIds)
    {
        if (outfitIds == null)
        {
            return;
        }

        foreach (string outfitId in outfitIds)
        {
            if (!string.IsNullOrEmpty(outfitId))
            {
                unlockedOutfitIds.Add(outfitId);
            }
        }

        ApplyUnlockedOutfitsToManager();
    }

    private void ApplyPurchasedItemOutfitUnlocks()
    {
        string duoShoppingItemId = GetDuoShoppingItemId();
        if (IsPurchasedItem(duoShoppingItemId))
        {
            RegisterUnlockedOutfits(GetDuoShoppingUnlockedOutfitIds());
        }
    }

    private void ApplyUnlockedOutfitsToManager()
    {
        if (outfitManager != null)
        {
            outfitManager.SetUnlockedOutfitIds(unlockedOutfitIds);
        }
    }

    public bool IsGameEventShown(string eventId)
    {
        return !string.IsNullOrEmpty(eventId) && shownGameEventIds.Contains(eventId);
    }

    public void MarkGameEventShown(string eventId)
    {
        if (string.IsNullOrEmpty(eventId))
        {
            return;
        }

        shownGameEventIds.Add(eventId);
    }

    public bool IsStillUnlocked(string stillId)
    {
        return !string.IsNullOrEmpty(stillId) && unlockedStillIds.Contains(stillId);
    }

    public void UnlockStill(string stillId)
    {
        if (string.IsNullOrEmpty(stillId))
        {
            return;
        }

        unlockedStillIds.Add(stillId);
    }

    public List<StillGalleryItem> GetUnlockedStillGalleryItems()
    {
        return GetStillGalleryItems(true);
    }

    public List<StillGalleryItem> GetStillGalleryItems(bool unlockedOnly)
    {
        List<StillGalleryItem> items = new List<StillGalleryItem>();
        HashSet<string> addedStillIds = new HashSet<string>();

        if (actions == null || actions.Count == 0)
        {
            LoadActionsFromResources();
        }

        foreach (ActionData action in actions)
        {
            if (action == null)
            {
                continue;
            }

            AddStillGalleryItem(items, addedStillIds, action.stillId, action.stillSprite, unlockedOnly);

            if (action.reactions == null)
            {
                continue;
            }

            foreach (ActionReactionData reaction in action.reactions)
            {
                if (reaction == null)
                {
                    continue;
                }

                AddStillGalleryItem(items, addedStillIds, reaction.stillId, reaction.stillSprite, unlockedOnly);
            }
        }

        if (gameEvents == null || gameEvents.Count == 0)
        {
            LoadGameEventsFromResources();
        }

        foreach (GameEventData gameEvent in gameEvents)
        {
            if (gameEvent == null || gameEvent.pages == null)
            {
                continue;
            }

            foreach (GameEventPageData page in gameEvent.pages)
            {
                if (page != null)
                {
                    AddStillGalleryItem(items, addedStillIds, page.stillId, page.stillSprite, unlockedOnly);
                }
            }
        }

        if (scheduledEvents == null || scheduledEvents.Count == 0)
        {
            LoadScheduledEventsFromResources();
        }

        foreach (ScheduledEventData scheduledEvent in scheduledEvents)
        {
            if (scheduledEvent == null)
            {
                continue;
            }

            AddStillGalleryItem(
                items,
                addedStillIds,
                scheduledEvent.stillId,
                scheduledEvent.stillSprite,
                unlockedOnly
            );
        }

        return items;
    }

    private void AddStillGalleryItem(
        List<StillGalleryItem> items,
        HashSet<string> addedStillIds,
        string stillId,
        Sprite stillSprite,
        bool unlockedOnly)
    {
        if (items == null ||
            addedStillIds == null ||
            string.IsNullOrEmpty(stillId) ||
            stillSprite == null)
        {
            return;
        }

        if (addedStillIds.Contains(stillId))
        {
            return;
        }

        if (unlockedOnly && !IsStillUnlocked(stillId))
        {
            return;
        }

        addedStillIds.Add(stillId);
        items.Add(new StillGalleryItem(stillId, stillSprite));
    }

    private void ExecuteSimpleAction(
        string speakerName,
        string message,
        int affectionChange,
        int playerHpChange,
        int heroineHpChange,
        bool advanceTime,
        string actionId,
        string stillId = "",
        Sprite stillSprite = null)
    {
        actionButtonArea.SetActive(false);
        genreButtonArea.SetActive(false);
        choiceButtonArea.SetActive(false);
        outfitPanel.SetActive(false);
        outfitReactionPanel.SetActive(false);

        int appliedPlayerHpChange = ApplyPlayerHpChange(playerHpChange);
        int appliedHeroineHpChange = ApplyHeroineHpChange(heroineHpChange);
        string resultMessage = AppendActionHpChangeMessage(message, appliedPlayerHpChange, appliedHeroineHpChange);

        ShowDialogue(GetSpeakerTypeForName(speakerName), speakerName, resultMessage, stillId, stillSprite);

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

        ShowSystemDialogue(message);

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

    private void LoadScheduledEventsFromResources()
    {
        ScheduledEventData[] heroineScheduledEvents =
            Resources.LoadAll<ScheduledEventData>(scheduledEventResourcePath);

        scheduledEvents = new List<ScheduledEventData>(heroineScheduledEvents);
        Dictionary<ScheduledEventData, string> scheduledEventSources =
            new Dictionary<ScheduledEventData, string>();
        HashSet<ScheduleType> heroineScheduleTypes = new HashSet<ScheduleType>();
        foreach (ScheduledEventData scheduledEvent in heroineScheduledEvents)
        {
            if (scheduledEvent == null)
            {
                continue;
            }

            scheduledEventSources[scheduledEvent] = "Heroine";
            if (scheduledEvent.scheduleType != ScheduleType.None)
            {
                heroineScheduleTypes.Add(scheduledEvent.scheduleType);
            }
        }

        int fallbackScheduledEventCount = 0;
        if (scheduledEventResourcePath != CommonScheduledEventResourcePath)
        {
            ScheduledEventData[] commonScheduledEvents =
                Resources.LoadAll<ScheduledEventData>(CommonScheduledEventResourcePath);
            foreach (ScheduledEventData scheduledEvent in commonScheduledEvents)
            {
                if (scheduledEvent == null)
                {
                    continue;
                }

                if (heroineScheduleTypes.Contains(scheduledEvent.scheduleType))
                {
                    continue;
                }

                scheduledEvents.Add(scheduledEvent);
                scheduledEventSources[scheduledEvent] = "CommonFallback";
                fallbackScheduledEventCount++;
            }
        }

        Debug.Log(
            "Loaded Scheduled Events: " +
            scheduledEvents.Count +
            " / HeroinePath: " +
            scheduledEventResourcePath +
            " / HeroineCount: " +
            heroineScheduledEvents.Length +
            " / FallbackPath: " +
            CommonScheduledEventResourcePath +
            " / FallbackAdded: " +
            fallbackScheduledEventCount);

        foreach (ScheduledEventData scheduledEvent in scheduledEvents)
        {
            string source = scheduledEventSources.TryGetValue(scheduledEvent, out string value)
                ? value
                : "Unknown";
            Debug.Log(
                "Scheduled Event: " +
                scheduledEvent.name +
                " / Source: " +
                source +
                " / Schedule: " +
                scheduledEvent.scheduleType +
                " / ActionId: " +
                scheduledEvent.actionId +
                " / Trigger: " +
                scheduledEvent.triggerTimeSlot +
                " / CostumeId: " +
                scheduledEvent.costumeId +
                " / StillId: " +
                scheduledEvent.stillId
            );
        }
    }

    private void CreateActionButtons()
    {
        Transform[] actionButtonParents = GetActionButtonParents();
        if (actionButtonParents.Length == 0)
        {
            Debug.LogError("Action Button Parent / Columns が設定されていません。");
            return;
        }

        if (actionButtonPrefab == null)
        {
            Debug.LogError("Action Button Prefab が設定されていません。");
            return;
        }

        ClearActionButtons();

        List<ActionData> enabledActions = new List<ActionData>();
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

            enabledActions.Add(action);
        }

        if (enabledActions.Count == 0)
        {
            return;
        }

        int buttonsPerColumn = Mathf.CeilToInt(enabledActions.Count / (float)actionButtonParents.Length);
        if (buttonsPerColumn < 1)
        {
            buttonsPerColumn = 1;
        }

        for (int i = 0; i < enabledActions.Count; i++)
        {
            ActionData action = enabledActions[i];
            int columnIndex = GetActionButtonColumnIndex(action, i, buttonsPerColumn, actionButtonParents.Length);
            Transform parent = actionButtonParents[columnIndex];

            if (parent == null)
            {
                continue;
            }

            Button button = Instantiate(actionButtonPrefab, parent);

            TMP_Text buttonText = button.GetComponentInChildren<TMP_Text>();
            if (buttonText != null)
            {
                buttonText.text = action.displayName;
            }

            ActionData capturedAction = action;
            button.onClick.AddListener(() => ExecuteAction(capturedAction));
        }
    }

    private void LoadGameEventsFromResources()
    {
        GameEventData[] loadedGameEvents =
            Resources.LoadAll<GameEventData>(gameEventResourcePath);

        gameEvents = new List<GameEventData>(loadedGameEvents);
        gameEvents.Sort((a, b) => a.sortOrder.CompareTo(b.sortOrder));

        Debug.Log("Loaded Game Events: " + gameEvents.Count);

        foreach (GameEventData gameEvent in gameEvents)
        {
            Debug.Log(
                "Game Event: " +
                gameEvent.name +
                " / Id: " +
                gameEvent.eventId +
                " / Trigger: " +
                gameEvent.triggerType +
                " / Sort: " +
                gameEvent.sortOrder +
                " / Pages: " +
                (gameEvent.pages != null ? gameEvent.pages.Count : 0)
            );
        }
    }

    private void StartGameStartSequence()
    {
        List<DialogueMessage> startMessages = BuildGameStartMessages();
        StartGameEventSequence(startMessages, true);
    }

    private List<DialogueMessage> BuildGameStartMessages()
    {
        List<DialogueMessage> messages = new List<DialogueMessage>();
        AppendGameEventMessages(messages, GameEventTriggerType.GameStart);

        if (messages.Count > 0)
        {
            return messages;
        }

        Sprite stillSprite = GetDefaultGameStartStillSprite();

        messages.Add(
            new DialogueMessage(
                DialogueSpeakerType.Heroine,
                heroineStatus != null ? heroineStatus.HeroineName : "",
                GetGameStartFallbackMessage(),
                "GameStartIntro_01",
                stillSprite
            )
        );
        messages.Add(
            new DialogueMessage(
                DialogueSpeakerType.Heroine,
                heroineStatus != null ? heroineStatus.HeroineName : "",
                GetGameStartFollowUpMessage()
            )
        );

        return messages;
    }

    private void AppendGameEventMessages(List<DialogueMessage> messages, GameEventTriggerType triggerType)
    {
        if (messages == null)
        {
            return;
        }

        foreach (GameEventData gameEvent in GetGameEventsForTrigger(triggerType))
        {
            if (gameEvent == null || gameEvent.pages == null || gameEvent.pages.Count == 0)
            {
                continue;
            }

            messages.AddRange(BuildGameEventMessages(gameEvent));

            if (gameEvent.showOnce && !string.IsNullOrEmpty(gameEvent.eventId))
            {
                MarkGameEventShown(gameEvent.eventId);
            }
        }
    }

    public bool TryStartManualGameEvent(string eventId)
    {
        if (string.IsNullOrEmpty(eventId))
        {
            return false;
        }

        GameEventData gameEvent = FindGameEventById(eventId);

        if (gameEvent == null || gameEvent.triggerType != GameEventTriggerType.Manual)
        {
            return false;
        }

        if (!CanStartGameEvent(gameEvent))
        {
            return false;
        }

        List<DialogueMessage> messages = BuildGameEventMessages(gameEvent);

        if (messages.Count == 0)
        {
            return false;
        }

        StartGameEventSequence(messages, true);

        if (gameEvent.showOnce && !string.IsNullOrEmpty(gameEvent.eventId))
        {
            MarkGameEventShown(gameEvent.eventId);
        }

        return true;
    }

    private void StartGameEventSequence(List<DialogueMessage> messages, bool hideSaveLoadButtons)
    {
        actionButtonArea.SetActive(false);
        genreButtonArea.SetActive(false);
        choiceButtonArea.SetActive(false);
        outfitPanel.SetActive(false);
        outfitReactionPanel.SetActive(false);
        nextButton.gameObject.SetActive(false);

        currentConversation = null;
        pendingAdvanceTime = false;
        pendingGoodNight = false;
        flowState = ConversationFlowState.Idle;

        ShowDialogueSequence(messages);

        if (hideSaveLoadButtons)
        {
            SetSaveLoadButtonsVisible(false);
            dialogueSequenceHidSaveLoadButtons = true;
        }
    }

    private List<GameEventData> GetGameEventsForTrigger(GameEventTriggerType triggerType)
    {
        List<GameEventData> result = new List<GameEventData>();

        if (gameEvents == null)
        {
            return result;
        }

        foreach (GameEventData gameEvent in gameEvents)
        {
            if (gameEvent == null || gameEvent.triggerType != triggerType)
            {
                continue;
            }

            if (!CanStartGameEvent(gameEvent))
            {
                continue;
            }

            result.Add(gameEvent);
        }

        return result;
    }

    private bool CanStartGameEvent(GameEventData gameEvent)
    {
        if (gameEvent == null || !gameEvent.isEnabled)
        {
            return false;
        }

        if (gameEvent.showOnce && !string.IsNullOrEmpty(gameEvent.eventId) && IsGameEventShown(gameEvent.eventId))
        {
            return false;
        }

        if (timeManager != null)
        {
            int currentDay = timeManager.Day;

            if (gameEvent.minDay > 1 && currentDay < gameEvent.minDay)
            {
                return false;
            }

            if (gameEvent.maxDay > 0 && currentDay > gameEvent.maxDay)
            {
                return false;
            }
        }

        if (heroineStatus != null)
        {
            int currentAffection = heroineStatus.Affection;

            if (gameEvent.minAffection > 0 && currentAffection < gameEvent.minAffection)
            {
                return false;
            }

            if (gameEvent.maxAffection > 0 && currentAffection > gameEvent.maxAffection)
            {
                return false;
            }
        }

        if (!HasRequiredOutfit(gameEvent.requiredOutfitIds, gameEvent.requiredOutfits))
        {
            return false;
        }

        if (HasBlockedOutfit(gameEvent.blockedOutfitIds, gameEvent.blockedOutfits))
        {
            return false;
        }

        if (!HasRequiredGameEventSkills(gameEvent.requiredSkillIds))
        {
            return false;
        }

        if (!IsGameEventWeatherAvailable(gameEvent))
        {
            return false;
        }

        if (!HasRequiredShownGameEvents(gameEvent.requiredShownEventIds))
        {
            return false;
        }

        if (HasBlockedShownGameEvent(gameEvent.blockedShownEventIds))
        {
            return false;
        }

        return true;
    }

    private bool HasRequiredGameEventSkills(List<string> requiredSkillIds)
    {
        if (requiredSkillIds == null || requiredSkillIds.Count == 0)
        {
            return true;
        }

        for (int i = 0; i < requiredSkillIds.Count; i++)
        {
            string skillId = requiredSkillIds[i];
            if (string.IsNullOrWhiteSpace(skillId) || !IsSkillUnlocked(skillId))
            {
                return false;
            }
        }

        return true;
    }

    private bool IsGameEventWeatherAvailable(GameEventData gameEvent)
    {
        if (gameEvent == null || gameEvent.anyWeather)
        {
            return true;
        }

        if (gameEvent.allowedWeathers == null || gameEvent.allowedWeathers.Count == 0)
        {
            return false;
        }

        if (timeManager == null)
        {
            return false;
        }

        return gameEvent.allowedWeathers.Contains(timeManager.CurrentWeather);
    }

    private bool HasRequiredOutfit(List<string> outfitIds, List<OutfitData> outfits)
    {
        if (!HasOutfitCondition(outfitIds, outfits))
        {
            return true;
        }

        string currentOutfitId = GetCurrentOutfitId();
        if (string.IsNullOrEmpty(currentOutfitId))
        {
            return false;
        }

        return OutfitConditionContains(outfitIds, outfits, currentOutfitId);
    }

    private bool HasBlockedOutfit(List<string> outfitIds, List<OutfitData> outfits)
    {
        if (!HasOutfitCondition(outfitIds, outfits))
        {
            return false;
        }

        string currentOutfitId = GetCurrentOutfitId();
        if (string.IsNullOrEmpty(currentOutfitId))
        {
            return false;
        }

        return OutfitConditionContains(outfitIds, outfits, currentOutfitId);
    }

    private bool HasOutfitCondition(List<string> outfitIds, List<OutfitData> outfits)
    {
        if (outfitIds != null)
        {
            foreach (string outfitId in outfitIds)
            {
                if (!string.IsNullOrEmpty(outfitId))
                {
                    return true;
                }
            }
        }

        if (outfits != null)
        {
            foreach (OutfitData outfit in outfits)
            {
                if (outfit != null && !string.IsNullOrEmpty(outfit.outfitId))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool OutfitConditionContains(List<string> outfitIds, List<OutfitData> outfits, string currentOutfitId)
    {
        if (string.IsNullOrEmpty(currentOutfitId))
        {
            return false;
        }

        if (outfitIds != null && outfitIds.Contains(currentOutfitId))
        {
            return true;
        }

        if (outfits != null)
        {
            foreach (OutfitData outfit in outfits)
            {
                if (outfit != null && outfit.outfitId == currentOutfitId)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private string GetCurrentOutfitId()
    {
        if (outfitManager == null || outfitManager.CurrentOutfit == null)
        {
            return "";
        }

        return outfitManager.CurrentOutfit.outfitId;
    }

    private bool IsCostumeConditionMatch(string costumeId)
    {
        if (string.IsNullOrWhiteSpace(costumeId))
        {
            return true;
        }

        string currentOutfitId = GetCurrentOutfitId();
        return !string.IsNullOrEmpty(currentOutfitId) &&
            string.Equals(currentOutfitId, costumeId, StringComparison.Ordinal);
    }

    private bool HasRequiredShownGameEvents(List<string> eventIds)
    {
        if (eventIds == null || eventIds.Count == 0)
        {
            return true;
        }

        foreach (string eventId in eventIds)
        {
            if (string.IsNullOrEmpty(eventId))
            {
                continue;
            }

            if (!IsGameEventShown(eventId))
            {
                return false;
            }
        }

        return true;
    }

    private bool HasBlockedShownGameEvent(List<string> eventIds)
    {
        if (eventIds == null || eventIds.Count == 0)
        {
            return false;
        }

        foreach (string eventId in eventIds)
        {
            if (string.IsNullOrEmpty(eventId))
            {
                continue;
            }

            if (IsGameEventShown(eventId))
            {
                return true;
            }
        }

        return false;
    }

    private GameEventData FindGameEventById(string eventId)
    {
        if (string.IsNullOrEmpty(eventId) || gameEvents == null)
        {
            return null;
        }

        foreach (GameEventData gameEvent in gameEvents)
        {
            if (gameEvent == null)
            {
                continue;
            }

            if (gameEvent.eventId == eventId)
            {
                return gameEvent;
            }
        }

        return null;
    }

    private List<DialogueMessage> BuildGameEventMessages(GameEventData gameEvent)
    {
        List<DialogueMessage> messages = new List<DialogueMessage>();

        if (gameEvent == null || gameEvent.pages == null)
        {
            return messages;
        }

        Sprite defaultStillSprite = GetDefaultGameStartStillSprite();

        for (int i = 0; i < gameEvent.pages.Count; i++)
        {
            GameEventPageData page = gameEvent.pages[i];
            if (page == null)
            {
                continue;
            }

            string speakerName = page.speakerName;
            if (string.IsNullOrEmpty(speakerName))
            {
                speakerName = GetGameEventDefaultSpeakerName(page.speakerType);
            }

            Sprite stillSprite = page.stillSprite;
            if (stillSprite == null && i == 0)
            {
                stillSprite = defaultStillSprite;
            }

            messages.Add(
                new DialogueMessage(
                    GetDialogueSpeakerType(page.speakerType),
                    speakerName,
                    page.message,
                    page.stillId,
                    stillSprite,
                    page.expressionId
                )
            );
        }

        return messages;
    }

    private Sprite GetDefaultGameStartStillSprite()
    {
        if (blankStillSprite == null)
        {
            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, new Color(0f, 0f, 0f, 0f));
            texture.Apply();
            texture.hideFlags = HideFlags.HideAndDontSave;

            blankStillSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                1f
            );
            blankStillSprite.hideFlags = HideFlags.HideAndDontSave;
        }

        return blankStillSprite;
    }

    private DialogueSpeakerType GetDialogueSpeakerType(ScheduledEventSpeakerType speakerType)
    {
        switch (speakerType)
        {
            case ScheduledEventSpeakerType.System:
                return DialogueSpeakerType.System;
            case ScheduledEventSpeakerType.Schedule:
                return DialogueSpeakerType.Schedule;
            case ScheduledEventSpeakerType.Outfit:
                return DialogueSpeakerType.Outfit;
            case ScheduledEventSpeakerType.Player:
                return DialogueSpeakerType.Player;
            default:
                return DialogueSpeakerType.Heroine;
        }
    }

    private string GetGameEventDefaultSpeakerName(ScheduledEventSpeakerType speakerType)
    {
        switch (speakerType)
        {
            case ScheduledEventSpeakerType.System:
                return SystemSpeakerName;
            case ScheduledEventSpeakerType.Schedule:
                return ScheduleSpeakerName;
            case ScheduledEventSpeakerType.Outfit:
                return OutfitSpeakerName;
            case ScheduledEventSpeakerType.Player:
                return PlayerSpeakerName;
            default:
                return heroineStatus != null ? heroineStatus.HeroineName : "";
        }
    }

    private int GetActionButtonColumnIndex(
        ActionData action,
        int actionIndex,
        int buttonsPerColumn,
        int columnCount)
    {
        if (columnCount <= 0)
        {
            return 0;
        }

        if (columnCount == 1 || action == null || action.displayColumn == ActionButtonColumn.Auto)
        {
            return Mathf.Min(actionIndex / buttonsPerColumn, columnCount - 1);
        }

        int requestedColumnIndex;
        switch (action.displayColumn)
        {
            case ActionButtonColumn.Left:
                requestedColumnIndex = 0;
                break;
            case ActionButtonColumn.Center:
                requestedColumnIndex = 1;
                break;
            case ActionButtonColumn.Right:
                requestedColumnIndex = 2;
                break;
            default:
                requestedColumnIndex = Mathf.Min(actionIndex / buttonsPerColumn, columnCount - 1);
                break;
        }

        if (requestedColumnIndex >= columnCount)
        {
            return Mathf.Min(actionIndex / buttonsPerColumn, columnCount - 1);
        }

        return requestedColumnIndex;
    }

    private void ClearActionButtons()
    {
        Transform[] actionButtonParents = GetActionButtonParents();
        if (actionButtonParents.Length == 0)
        {
            return;
        }

        for (int i = 0; i < actionButtonParents.Length; i++)
        {
            Transform parent = actionButtonParents[i];
            if (parent == null)
            {
                continue;
            }

            for (int childIndex = parent.childCount - 1; childIndex >= 0; childIndex--)
            {
                Destroy(parent.GetChild(childIndex).gameObject);
            }
        }
    }

    private Transform[] GetActionButtonParents()
    {
        List<Transform> parents = new List<Transform>();

        if (actionButtonAreaColumnLeft != null)
        {
            parents.Add(actionButtonAreaColumnLeft.transform);
        }

        if (actionButtonAreaColumnCenter != null)
        {
            parents.Add(actionButtonAreaColumnCenter.transform);
        }

        if (actionButtonAreaColumnRight != null)
        {
            parents.Add(actionButtonAreaColumnRight.transform);
        }

        if (parents.Count > 0)
        {
            return parents.ToArray();
        }

        if (actionButtonParent != null)
        {
            return new[] { actionButtonParent };
        }

        return Array.Empty<Transform>();
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

        if (action.executionType == ActionExecutionType.OpenStatusDetailPanel)
        {
            OpenStatusDetailPanel();
            return;
        }

        if (action.executionType == ActionExecutionType.OpenStillGalleryPanel)
        {
            OpenStillGalleryPanel();
            return;
        }

        if (action.executionType == ActionExecutionType.OpenMessageLogPanel)
        {
            OpenMessageLogPanel();
            return;
        }

        if (action.executionType == ActionExecutionType.OpenDebugBattlePanel)
        {
            OpenDebugBattlePanel();
            return;
        }

        if (action.executionType == ActionExecutionType.OpenTrainingPanel)
        {
            OpenTrainingPanel();
            return;
        }

        if (action.executionType == ActionExecutionType.OpenSkillPanel)
        {
            OpenSkillPanel();
            return;
        }
    }

    private void OpenOutfitPanel(ActionData action)
    {
        actionButtonArea.SetActive(false);
        genreButtonArea.SetActive(false);
        choiceButtonArea.SetActive(false);
        CreateOutfitButtons();
        outfitPanel.SetActive(true);
        outfitReactionPanel.SetActive(false);

        if (string.IsNullOrEmpty(action.resultMessage))
        {
            ShowOutfitDialogue("着替える衣装を選んでください。");
        }
        else
        {
            ShowOutfitDialogue(action.resultMessage);
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

        if (IsTodayHomeActionRestricted(action))
        {
            return false;
        }

        return true;
    }

    private bool IsTodayHomeActionRestricted(ActionData action)
    {
        if (scheduleManager == null || action == null)
        {
            return false;
        }

        if (!scheduleManager.IsTodayHomeSchedule())
        {
            return false;
        }

        return action.actionId == "Walk";
    }

    private void OpenConversationGenres(ActionData action)
    {
        actionButtonArea.SetActive(false);
        genreButtonArea.SetActive(true);
        choiceButtonArea.SetActive(false);
        outfitPanel.SetActive(false);
        outfitReactionPanel.SetActive(false);

        if (string.IsNullOrEmpty(action.resultMessage))
        {
            ShowSystemDialogue("話したい話題を選んでください。");
        }
        else
        {
            ShowSystemDialogue(action.resultMessage);
        }

        flowState = ConversationFlowState.Idle;
    }

    private void ExecuteActionData(ActionData action)
    {
        ActionReactionData reaction = SelectActionReaction(action);

        if (reaction != null)
        {
            if (reaction.showOnce && !string.IsNullOrEmpty(reaction.reactionId))
            {
                // 会話IDと同じ表示履歴を利用し、既存セーブ形式のまま一度限りを永続化する。
                shownConversationIds.Add(reaction.reactionId);
            }

            string reactionSpeakerName = reaction.useHeroineNameAsSpeaker
                ? heroineStatus.HeroineName
                : SystemSpeakerName;
            string reactionMessage = string.IsNullOrEmpty(reaction.resultMessage)
                ? action.resultMessage
                : reaction.resultMessage;

            ExecuteSimpleAction(
                reactionSpeakerName,
                reactionMessage,
                reaction.affectionChange,
                reaction.playerHpChange,
                reaction.heroineHpChange,
                reaction.advanceTime,
                action.actionId,
                string.IsNullOrEmpty(reaction.stillId) ? action.stillId : reaction.stillId,
                reaction.stillSprite != null ? reaction.stillSprite : action.stillSprite
            );

            return;
        }

        string defaultSpeakerName = action.useHeroineNameAsSpeaker
            ? heroineStatus.HeroineName
            : SystemSpeakerName;

        ExecuteSimpleAction(
            defaultSpeakerName,
            action.resultMessage,
            action.affectionChange,
            action.playerHpChange,
            action.heroineHpChange,
            action.advanceTime,
            action.actionId,
            action.stillId,
            action.stillSprite
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

        if (reaction.showOnce &&
            !string.IsNullOrEmpty(reaction.reactionId) &&
            shownConversationIds.Contains(reaction.reactionId))
        {
            return false;
        }

        if (!AreRequiredIdsSatisfied(reaction.requiredShownEventIds, shownGameEventIds.Contains) ||
            !AreRequiredIdsSatisfied(reaction.requiredSkillIds, IsSkillUnlocked))
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

        if (!IsCostumeConditionMatch(reaction.costumeId))
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
            if (!outfitManager.IsOutfitVisibleInDressUp(outfit))
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

        ShowHeroineDialogue(message);

        if (pendingScheduledEvent != null)
        {
            if (success)
            {
                startPendingScheduledEventAfterOutfitMessage = true;
                returnToScheduledEventPromptAfterOutfitMessage = false;
            }
            else
            {
                startPendingScheduledEventAfterOutfitMessage = false;
                returnToScheduledEventPromptAfterOutfitMessage = true;
            }
        }

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

        if (outfitManager.CurrentOutfit == null)
        {
            ShowOutfitDialogue("まだ衣装を着ていません。");
        }
        else if (string.IsNullOrEmpty(action.resultMessage))
        {
            ShowOutfitDialogue("今の衣装について、どう反応しますか？");
        }
        else
        {
            ShowOutfitDialogue(action.resultMessage + "\n現在の衣装：" + outfitManager.CurrentOutfit.displayName);
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

        message = ApplyScheduleOutfitReactionBonus(reactionType, message);

        actionButtonArea.SetActive(false);
        genreButtonArea.SetActive(false);
        choiceButtonArea.SetActive(false);
        outfitPanel.SetActive(false);
        outfitReactionPanel.SetActive(false);

        ShowHeroineDialogue(message);

        RefreshUI();

        flowState = ConversationFlowState.ShowingActionResult;
        nextButton.gameObject.SetActive(true);
    }

    private string ApplyScheduleOutfitReactionBonus(OutfitReactionType reactionType, string message)
    {
        if (scheduleManager == null || !scheduleManager.IsTodayDuoSchedule())
        {
            return message;
        }

        if (reactionType == OutfitReactionType.Praise)
        {
            heroineStatus.AddAffection(10);
            return message + "\nDuo schedule bonus: praise feels stronger today.";
        }

        if (reactionType == OutfitReactionType.Dislike)
        {
            heroineStatus.AddAffection(-10);
            return message + "\nDuo schedule penalty: the reaction lands harder today.";
        }

        if (reactionType == OutfitReactionType.Bored)
        {
            heroineStatus.AddAffection(-10);
            return message + "\nDuo schedule penalty: boredom is less welcome today.";
        }

        return message;
    }

    private void OpenOutfitPanelForChange()
    {
        actionButtonArea.SetActive(false);
        genreButtonArea.SetActive(false);
        choiceButtonArea.SetActive(false);
        outfitReactionPanel.SetActive(false);
        CreateOutfitButtons();
        outfitPanel.SetActive(true);
        nextButton.gameObject.SetActive(false);

        ShowOutfitDialogue("変更する衣装を選んでください。");

        flowState = ConversationFlowState.Idle;
    }

    private void OnDayChanged()
    {
        dayStartMessages = BuildDayStartMessages();
    }

    private List<DialogueMessage> BuildDayStartMessages()
    {
        List<DialogueMessage> messages = new List<DialogueMessage>();
        string outfitMessage = AutoChooseOutfitOnNewDay();

        if (!string.IsNullOrEmpty(outfitMessage))
        {
            messages.Add(new DialogueMessage(DialogueSpeakerType.Outfit, OutfitSpeakerName, outfitMessage));
        }

        string scheduleMessage = CreateScheduledEventPreparationMessage();

        if (!string.IsNullOrEmpty(scheduleMessage))
        {
            messages.Add(new DialogueMessage(DialogueSpeakerType.Schedule, ScheduleSpeakerName, scheduleMessage));
        }

        AppendGameEventMessages(messages, GameEventTriggerType.DayStart);

        return messages;
    }

    private string AutoChooseOutfitOnNewDay()
    {
        if (outfitManager == null)
        {
            return "";
        }

        string message;
        bool success = outfitManager.AutoChooseOutfitForToday(out message);

        if (!success)
        {
            return "";
        }

        RefreshUI();
        return message;
    }

    private string CreateScheduledEventPreparationMessage()
    {
        if (scheduleManager == null)
        {
            return "";
        }

        if (scheduleManager.TodayScheduleEventExecuted)
        {
            return "";
        }

        ScheduledEventDefinition scheduledEvent = GetScheduledEventDefinition(scheduleManager.TodaySchedule);

        if (scheduledEvent == null)
        {
            return "";
        }

        if (ShouldCancelScheduledEventForWeather(scheduledEvent))
        {
            scheduleManager.MarkTodayScheduleEventExecuted();
            return CreateScheduledEventWeatherCancelMessage(scheduledEvent);
        }

        return scheduledEvent.PreparationMessage;
    }

    private bool TryStartScheduledEvent()
    {
        if (scheduleManager == null)
        {
            return false;
        }

        if (scheduleManager.TodayScheduleEventExecuted)
        {
            return false;
        }

        ScheduledEventDefinition scheduledEvent = GetScheduledEventDefinition(scheduleManager.TodaySchedule);

        if (scheduledEvent == null)
        {
            return false;
        }

        if (timeManager.CurrentTimeSlot != scheduledEvent.TriggerTimeSlot)
        {
            return false;
        }

        if (ShouldCancelScheduledEventForWeather(scheduledEvent))
        {
            CancelScheduledEventForWeather(scheduledEvent);
            return true;
        }

        if (ShouldShowScheduledEventOutfitPrompt(scheduledEvent))
        {
            ShowScheduledEventOutfitPrompt(scheduledEvent);
            return true;
        }

        StartScheduledEvent(scheduledEvent);
        return true;
    }

    private static bool AreRequiredIdsSatisfied(List<string> requiredIds, Predicate<string> isSatisfied)
    {
        if (requiredIds == null || requiredIds.Count == 0)
        {
            return true;
        }

        foreach (string requiredId in requiredIds)
        {
            if (!string.IsNullOrEmpty(requiredId) && !isSatisfied(requiredId))
            {
                return false;
            }
        }

        return true;
    }

    private bool ShouldCancelScheduledEventForWeather(ScheduledEventDefinition scheduledEvent)
    {
        if (scheduledEvent == null || timeManager == null)
        {
            return false;
        }

        return timeManager.CurrentWeather == Weather.Storm &&
               IsOutdoorSchedule(scheduledEvent.ScheduleType);
    }

    private bool IsOutdoorSchedule(ScheduleType scheduleType)
    {
        switch (scheduleType)
        {
            case ScheduleType.SoloForest:
            case ScheduleType.SoloCave:
            case ScheduleType.SoloLake:
            case ScheduleType.SoloShopping:
            case ScheduleType.DuoForest:
            case ScheduleType.DuoCave:
            case ScheduleType.DuoLake:
            case ScheduleType.DuoShopping:
                return true;

            default:
                return false;
        }
    }

    private void CancelScheduledEventForWeather(ScheduledEventDefinition scheduledEvent)
    {
        scheduleManager.MarkTodayScheduleEventExecuted();
        pendingScheduledEvent = null;
        startPendingScheduledEventAfterOutfitMessage = false;
        returnToScheduledEventPromptAfterOutfitMessage = false;
        currentConversation = null;
        pendingAdvanceTime = false;
        pendingGoodNight = false;
        pendingDuoShoppingResultMessages.Clear();

        actionButtonArea.SetActive(false);
        genreButtonArea.SetActive(false);
        choiceButtonArea.SetActive(false);
        outfitPanel.SetActive(false);
        outfitReactionPanel.SetActive(false);

        ShowScheduleDialogue(CreateScheduledEventWeatherCancelMessage(scheduledEvent));

        flowState = ConversationFlowState.ShowingActionResult;
        nextButton.gameObject.SetActive(true);

        RefreshUI();
    }

    private string CreateScheduledEventWeatherCancelMessage(ScheduledEventDefinition scheduledEvent)
    {
        if (scheduledEvent == null)
        {
            return "嵐のため、今日の予定はキャンセルになりました。";
        }

        return "嵐のため、" +
               ScheduleManager.GetScheduleDisplayName(scheduledEvent.ScheduleType) +
               "の予定はキャンセルになりました。";
    }

    private bool ShouldShowScheduledEventOutfitPrompt(ScheduledEventDefinition scheduledEvent)
    {
        if (scheduledEvent == null || !scheduledEvent.AllowOutfitChangeBeforeStart)
        {
            return false;
        }

        if (outfitManager == null)
        {
            return true;
        }

        ScheduledEventOutfitPromptMode effectiveMode =
            GetEffectiveScheduledEventOutfitPromptMode(scheduledEvent.OutfitPromptMode);

        switch (effectiveMode)
        {
            case ScheduledEventOutfitPromptMode.Always:
                return true;

            case ScheduledEventOutfitPromptMode.Hidden:
                return false;

            case ScheduledEventOutfitPromptMode.Conditional:
                return !outfitManager.IsCurrentOutfitSuitableForSchedule(scheduledEvent.ScheduleType);

            default:
                return false;
        }
    }

    private ScheduledEventOutfitPromptMode GetEffectiveScheduledEventOutfitPromptMode(
        ScheduledEventOutfitPromptMode outfitPromptMode)
    {
        ScheduledEventOutfitPromptMode selectedMode = playerOutfitPromptAbilities != null
            ? playerOutfitPromptAbilities.selectedMode
            : ScheduledEventOutfitPromptMode.Always;
        if (CanUseScheduledEventOutfitPromptMode(selectedMode))
        {
            return selectedMode;
        }

        return ScheduledEventOutfitPromptMode.Always;
    }

    private bool CanUseScheduledEventOutfitPromptMode(ScheduledEventOutfitPromptMode outfitPromptMode)
    {
        if (outfitPromptMode == ScheduledEventOutfitPromptMode.Always)
        {
            return true;
        }

        if (outfitPromptMode == ScheduledEventOutfitPromptMode.Conditional)
        {
            return playerOutfitPromptAbilities != null &&
                   playerOutfitPromptAbilities.canUseConditionalMode;
        }

        if (outfitPromptMode == ScheduledEventOutfitPromptMode.Hidden)
        {
            return playerOutfitPromptAbilities != null &&
                   playerOutfitPromptAbilities.canUseHiddenMode;
        }

        return false;
    }

    private void ShowScheduledEventOutfitPrompt(ScheduledEventDefinition scheduledEvent)
    {
        if (scheduledEvent == null)
        {
            return;
        }

        pendingScheduledEvent = scheduledEvent;
        startPendingScheduledEventAfterOutfitMessage = false;
        returnToScheduledEventPromptAfterOutfitMessage = false;
        currentConversation = null;
        pendingAdvanceTime = false;
        pendingGoodNight = false;

        actionButtonArea.SetActive(false);
        genreButtonArea.SetActive(false);
        choiceButtonArea.SetActive(true);
        outfitPanel.SetActive(false);
        outfitReactionPanel.SetActive(false);

        ShowScheduleDialogue(
            "そろそろ" +
            ScheduleManager.GetScheduleDisplayName(scheduledEvent.ScheduleType) +
            "の時間です。衣装を確認しますか？"
        );

        SetupScheduledEventPromptButton(choiceButton1, "このまま出発", () => StartScheduledEvent(scheduledEvent));
        SetupScheduledEventPromptButton(choiceButton2, "着替える", OpenOutfitPanelForScheduledEvent);
        choiceButton3.gameObject.SetActive(false);

        RefreshUI();
        flowState = ConversationFlowState.Idle;
        nextButton.gameObject.SetActive(false);
    }

    private void SetupScheduledEventPromptButton(Button button, string label, UnityEngine.Events.UnityAction action)
    {
        button.gameObject.SetActive(true);

        TMP_Text buttonText = button.GetComponentInChildren<TMP_Text>();
        if (buttonText != null)
        {
            buttonText.text = label;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    private void OpenOutfitPanelForScheduledEvent()
    {
        actionButtonArea.SetActive(false);
        genreButtonArea.SetActive(false);
        choiceButtonArea.SetActive(false);
        outfitReactionPanel.SetActive(false);
        CreateOutfitButtons();
        outfitPanel.SetActive(true);
        nextButton.gameObject.SetActive(false);

        ShowOutfitDialogue("出発前に衣装を選んでください。");

        flowState = ConversationFlowState.Idle;
    }

    private void StartScheduledEvent(ScheduledEventDefinition scheduledEvent)
    {
        if (scheduledEvent == null)
        {
            return;
        }

        if (ShouldCancelScheduledEventForWeather(scheduledEvent))
        {
            CancelScheduledEventForWeather(scheduledEvent);
            return;
        }

        if (IsShoppingSchedule(scheduledEvent.ScheduleType) && TryOpenDuoShoppingShopPanel(scheduledEvent))
        {
            return;
        }

        if (TryOpenBattlePanelForScheduledExploration(scheduledEvent))
        {
            return;
        }

        CompleteScheduledEvent(scheduledEvent, null);
    }

    private bool TryOpenBattlePanelForScheduledExploration(ScheduledEventDefinition scheduledEvent)
    {
        if (scheduledEvent == null ||
            !ShouldUseBattlePanelForExploration(scheduledEvent.ScheduleType))
        {
            return false;
        }

        EnemyData enemy = ResolveExplorationEnemy(scheduledEvent.ScheduleType);
        if (enemy == null)
        {
            return false;
        }

        EnsureBattlePanel();
        if (battlePanel == null)
        {
            Debug.LogWarning("BattlePanel が設定されていないため、探索予定は既存の自動戦闘で処理します。");
            return false;
        }

        pendingBattlePanelScheduledEvent = scheduledEvent;
        waitingForBattlePanelScheduledEvent = true;
        hasLastBattlePanelSimpleResult = false;
        lastBattlePanelResult = null;

        actionButtonArea.SetActive(false);
        genreButtonArea.SetActive(false);
        choiceButtonArea.SetActive(false);
        outfitPanel.SetActive(false);
        outfitReactionPanel.SetActive(false);
        nextButton.gameObject.SetActive(false);

        ShowBattlePanelIntro(scheduledEvent, enemy);
        return true;
    }

    private bool ShouldUseBattlePanelForExploration(ScheduleType scheduleType)
    {
        if (!useBattlePanelForExploration)
        {
            return false;
        }

        switch (scheduleType)
        {
            case ScheduleType.SoloForest:
            case ScheduleType.SoloCave:
            case ScheduleType.SoloLake:
            case ScheduleType.DuoForest:
            case ScheduleType.DuoCave:
            case ScheduleType.DuoLake:
                return true;

            default:
                return false;
        }
    }

    private void ShowBattlePanelIntro(ScheduledEventDefinition scheduledEvent, EnemyData enemy)
    {
        string stillId;
        Sprite stillSprite;
        ResolveScheduledEventStill(scheduledEvent, out stillId, out stillSprite);

        string message = BuildBattlePanelIntroMessage(
            scheduledEvent != null ? scheduledEvent.ScheduleType : ScheduleType.None,
            enemy);

        ShowDialogue(DialogueSpeakerType.Schedule, ScheduleSpeakerName, message, stillId, stillSprite);

        flowState = ConversationFlowState.ShowingBattleIntro;
        nextButton.gameObject.SetActive(true);
        RefreshUI();
    }

    private static string BuildBattlePanelIntroMessage(ScheduleType scheduleType, EnemyData enemy)
    {
        string enemyName = enemy != null ? enemy.GetDisplayName() : "何か";
        switch (scheduleType)
        {
            case ScheduleType.DuoForest:
                return "二人で森を歩いていると、木々の奥から気配が近づいてきます。\n" +
                    enemyName + " が現れそうです。";

            case ScheduleType.SoloForest:
                return "森を歩いていると、木々の奥から気配が近づいてきます。\n" +
                    enemyName + " が現れそうです。";

            case ScheduleType.DuoCave:
                return "二人で洞窟を進んでいると、暗がりの奥で何かが動きました。\n" +
                    enemyName + " が現れそうです。";

            case ScheduleType.SoloCave:
                return "洞窟を進んでいると、暗がりの奥で何かが動きました。\n" +
                    enemyName + " が現れそうです。";

            case ScheduleType.DuoLake:
                return "二人で湖のほとりを歩いていると、水面の向こうに気配を感じます。\n" +
                    enemyName + " が現れそうです。";

            case ScheduleType.SoloLake:
                return "湖のほとりを歩いていると、水面の向こうに気配を感じます。\n" +
                    enemyName + " が現れそうです。";

            default:
                return "探索中に気配を感じました。\n" +
                    enemyName + " が現れそうです。";
        }
    }

    private void StartPendingBattlePanelBattle()
    {
        if (!waitingForBattlePanelScheduledEvent)
        {
            flowState = ConversationFlowState.Idle;
            nextButton.gameObject.SetActive(false);
            RefreshUI();
            return;
        }

        if (pendingBattlePanelScheduledEvent == null)
        {
            waitingForBattlePanelScheduledEvent = false;
            flowState = ConversationFlowState.Idle;
            nextButton.gameObject.SetActive(false);
            RefreshUI();
            return;
        }

        EnemyData enemy = ResolveExplorationEnemy(pendingBattlePanelScheduledEvent.ScheduleType);
        if (enemy == null)
        {
            CompletePendingBattlePanelScheduledEvent();
            return;
        }

        ResetDialogueSequenceState();
        flowState = ConversationFlowState.Idle;
        nextButton.gameObject.SetActive(false);
        battlePanel.OpenBattle(enemy, IsDuoExplorationSchedule(pendingBattlePanelScheduledEvent.ScheduleType));
    }

    private void CompleteScheduledEvent(
        ScheduledEventDefinition scheduledEvent,
        ShopItemData selectedShopItem,
        bool hasExternalBattleResult = false,
        SimpleBattleResult externalBattleResult = default(SimpleBattleResult))
    {
        pendingScheduledEventFollowUpMessages.Clear();
        string eventMessage = ResolveScheduledEventMessage(
            scheduledEvent,
            selectedShopItem,
            hasExternalBattleResult,
            externalBattleResult);
        CompleteScheduledEventWithResolvedMessage(scheduledEvent, eventMessage);
    }

    private string ResolveScheduledEventMessage(
        ScheduledEventDefinition scheduledEvent,
        ShopItemData selectedShopItem,
        bool hasExternalBattleResult = false,
        SimpleBattleResult externalBattleResult = default(SimpleBattleResult))
    {
        if (scheduledEvent == null)
        {
            return "";
        }

        if (IsShoppingSchedule(scheduledEvent.ScheduleType))
        {
            return ApplyDuoShoppingTestPurchase(scheduledEvent.EventMessage, selectedShopItem);
        }

        if (IsExplorationSchedule(scheduledEvent.ScheduleType))
        {
            return ApplySimpleExplorationResult(
                scheduledEvent.EventMessage,
                scheduledEvent.ScheduleType,
                hasExternalBattleResult,
                externalBattleResult);
        }

        return scheduledEvent.EventMessage;
    }

    private static bool IsShoppingSchedule(ScheduleType scheduleType)
    {
        return scheduleType == ScheduleType.SoloShopping || scheduleType == ScheduleType.DuoShopping;
    }

    private static bool IsExplorationSchedule(ScheduleType scheduleType)
    {
        return scheduleType == ScheduleType.SoloForest ||
            scheduleType == ScheduleType.SoloCave ||
            scheduleType == ScheduleType.SoloLake ||
            scheduleType == ScheduleType.DuoForest ||
            scheduleType == ScheduleType.DuoCave ||
            scheduleType == ScheduleType.DuoLake;
    }

    private string ApplySimpleExplorationResult(
        string baseMessage,
        ScheduleType scheduleType,
        bool hasExternalBattleResult = false,
        SimpleBattleResult externalBattleResult = default(SimpleBattleResult))
    {
        EnsureCoreStatusReferences();

        if (playerStatus == null)
        {
            return AppendLine(baseMessage, "探索結果を確認できませんでした。プレイヤーステータスが設定されていません。");
        }

        string locationName;
        int rewardMoney;
        int playerHpChange;
        int heroineHpChange;
        switch (scheduleType)
        {
            case ScheduleType.SoloForest:
                locationName = "森";
                rewardMoney = 20;
                playerHpChange = 0;
                heroineHpChange = 0;
                break;

            case ScheduleType.DuoForest:
                locationName = "二人で森";
                rewardMoney = 25;
                playerHpChange = 0;
                heroineHpChange = 0;
                break;

            case ScheduleType.SoloCave:
                locationName = "洞窟";
                rewardMoney = 50;
                playerHpChange = -10;
                heroineHpChange = 0;
                break;

            case ScheduleType.DuoCave:
                locationName = "二人で洞窟";
                rewardMoney = 60;
                playerHpChange = -8;
                heroineHpChange = -6;
                break;

            case ScheduleType.SoloLake:
                locationName = "湖";
                rewardMoney = 10;
                playerHpChange = 10;
                heroineHpChange = 0;
                break;

            case ScheduleType.DuoLake:
                locationName = "二人で湖";
                rewardMoney = 15;
                playerHpChange = 10;
                heroineHpChange = 8;
                break;

            default:
                locationName = "探索";
                rewardMoney = 0;
                playerHpChange = 0;
                heroineHpChange = 0;
                break;
        }

        if (hasExternalBattleResult)
        {
            playerHpChange = 0;
            heroineHpChange = 0;
        }

        int appliedPlayerHpChange = ApplyPlayerHpChange(playerHpChange);
        int appliedHeroineHpChange = ApplyHeroineHpChange(heroineHpChange);
        EnemyData enemy = ResolveExplorationEnemy(scheduleType);
        SimpleBattleResult battleResult = new SimpleBattleResult();
        bool hasBattleResult = false;
        if (hasExternalBattleResult)
        {
            battleResult = externalBattleResult;
            hasBattleResult = true;
        }
        else if (enemy != null)
        {
            battleResult = ResolveSimpleBattle(enemy, IsDuoExplorationSchedule(scheduleType));
            hasBattleResult = true;
        }

        bool canReceiveExplorationReward = rewardMoney > 0 && (!hasBattleResult || battleResult.PlayerWon);
        if (canReceiveExplorationReward)
        {
            playerStatus.AddMoney(rewardMoney);
        }

        RefreshStatusDetailPanel();

        string resultMessage = locationName + "の探索を終えました。";
        if (enemy != null)
        {
            resultMessage += "\n" + enemy.GetDisplayName() + " に遭遇しました。";
        }

        if (canReceiveExplorationReward)
        {
            resultMessage += "\n探索報酬として " + rewardMoney + " を入手しました。現在の所持金：" + playerStatus.Money;
        }
        else if (hasBattleResult && battleResult.PlayerEscaped)
        {
            resultMessage += "\n戦闘から撤退しました。探索報酬はありません。";
            resultMessage += "\n予定は消費されました。";
        }
        else if (rewardMoney > 0 && hasBattleResult && !battleResult.PlayerWon)
        {
            resultMessage += "\n敗北したため探索報酬は入手できませんでした。";
        }

        if (appliedPlayerHpChange != 0)
        {
            resultMessage += "\nプレイヤーHP " + FormatSignedValue(appliedPlayerHpChange) +
                "（現在 " + playerStatus.CurrentHp + "/" + playerStatus.MaxHp + "）";
        }

        if (appliedHeroineHpChange != 0 && heroineStatus != null)
        {
            resultMessage += "\n" + heroineStatus.HeroineName + " HP " + FormatSignedValue(appliedHeroineHpChange) +
                "（現在 " + heroineStatus.CurrentHp + "/" + heroineStatus.MaxHp + "）";
        }

        if (hasBattleResult)
        {
            AddBattleLogFollowUpMessages(battleResult);
            AddBattleResultEventFollowUpMessage(scheduleType, battleResult);
        }

        return AppendLine(baseMessage, resultMessage);
    }

    private void EnqueueScheduledEventFollowUpMessages()
    {
        if (pendingScheduledEventFollowUpMessages.Count == 0)
        {
            return;
        }

        foreach (DialogueMessage message in pendingScheduledEventFollowUpMessages)
        {
            queuedDialogueMessages.Enqueue(message);
        }

        pendingScheduledEventFollowUpMessages.Clear();
    }

    private void AddBattleResultEventFollowUpMessage(ScheduleType scheduleType, SimpleBattleResult result)
    {
        BattleResultEventType eventType = ResolveBattleResultEventType(scheduleType, result);
        string battleContextId = ResolveBattleContextId(scheduleType);
        BattleResultEventData eventData = ResolveBattleResultEventData(eventType, battleContextId);
        if (eventData != null && !string.IsNullOrEmpty(eventData.message))
        {
            DialogueSpeakerType speakerType = GetDialogueSpeakerType(eventData.speakerType);
            string speakerName = string.IsNullOrWhiteSpace(eventData.speakerName)
                ? GetGameEventDefaultSpeakerName(eventData.speakerType)
                : eventData.speakerName;
            Sprite stillSprite = null;
            if (!string.IsNullOrWhiteSpace(eventData.stillId))
            {
                TryResolveHeroineCatalogSprite(eventData.stillId, out stillSprite);
            }
            pendingScheduledEventFollowUpMessages.Add(new DialogueMessage(
                speakerType,
                speakerName,
                FormatMessageVariables(eventData.message),
                eventData.stillId ?? "",
                stillSprite,
                eventData.expressionId ?? "",
                eventData.visualMode));
            return;
        }

        string message = BuildBattleResultEventMessage(scheduleType, result);
        if (string.IsNullOrEmpty(message))
        {
            return;
        }

        pendingScheduledEventFollowUpMessages.Add(
            new DialogueMessage(DialogueSpeakerType.Schedule, ScheduleSpeakerName, message));
    }

    private string BuildBattleResultEventMessage(ScheduleType scheduleType, SimpleBattleResult result)
    {
        BattleResultEventType eventType = ResolveBattleResultEventType(scheduleType, result);
        string battleContextId = ResolveBattleContextId(scheduleType);
        string dataMessage = ResolveBattleResultEventDataMessage(eventType, battleContextId);
        if (!string.IsNullOrEmpty(dataMessage))
        {
            return dataMessage;
        }

        string heroineName = ResolveHeroineDisplayName();
        switch (eventType)
        {
            case BattleResultEventType.DuoVictory:
                return "戦闘後イベント：勝利\n" +
                    heroineName + "と協力して探索を続けられました。\n" +
                    "勝利結果は今後の好感度イベントや探索分岐へ接続できます。";

            case BattleResultEventType.SoloVictory:
                return "戦闘後イベント：勝利\n" +
                    "敵を退け、探索を続けられました。\n" +
                    "勝利結果は今後の探索分岐へ接続できます。";

            case BattleResultEventType.DuoDefeat:
                return "戦闘後イベント：敗北\n" +
                    heroineName + "と体勢を立て直すため撤退しました。\n" +
                    "敗北結果は今後の専用イベント分岐へ接続できます。";

            case BattleResultEventType.DuoEscape:
                return "戦闘後イベント：撤退\n" +
                    heroineName + "と体勢を立て直して帰還しました。\n" +
                    "予定は消費されました。";

            case BattleResultEventType.SoloEscape:
                return "戦闘後イベント：撤退\n" +
                    "体勢を立て直すため帰還しました。\n" +
                    "予定は消費されました。";

            default:
                return "戦闘後イベント：敗北\n" +
                    "探索を切り上げて撤退しました。\n" +
                    "敗北結果は今後の専用イベント分岐へ接続できます。";
        }
    }

    private static BattleResultEventType ResolveBattleResultEventType(
        ScheduleType scheduleType,
        SimpleBattleResult result)
    {
        bool isDuoExploration = IsDuoExplorationSchedule(scheduleType);
        if (result.PlayerWon)
        {
            return isDuoExploration ? BattleResultEventType.DuoVictory : BattleResultEventType.SoloVictory;
        }

        if (result.PlayerEscaped)
        {
            return isDuoExploration ? BattleResultEventType.DuoEscape : BattleResultEventType.SoloEscape;
        }

        return isDuoExploration ? BattleResultEventType.DuoDefeat : BattleResultEventType.SoloDefeat;
    }

    private static string ResolveBattleContextId(ScheduleType scheduleType)
    {
        switch (scheduleType)
        {
            case ScheduleType.SoloForest:
            case ScheduleType.DuoForest:
                return "Forest";

            case ScheduleType.SoloCave:
            case ScheduleType.DuoCave:
                return "Cave";

            case ScheduleType.SoloLake:
            case ScheduleType.DuoLake:
                return "Lake";

            default:
                return "";
        }
    }

    private string ResolveBattleResultEventDataMessage(
        BattleResultEventType eventType,
        string battleContextId)
    {
        BattleResultEventData data = ResolveBattleResultEventData(eventType, battleContextId);
        return data != null ? FormatMessageVariables(data.message) : "";
    }

    private BattleResultEventData ResolveBattleResultEventData(
        BattleResultEventType eventType,
        string battleContextId)
    {
        BattleResultEventData primary = ResolveBattleResultEventDataFromSource(
            GetPrimaryBattleResultEvents(),
            eventType,
            battleContextId);
        if (primary != null)
        {
            return primary;
        }

        if (string.Equals(battleResultEventResourcePath, BattleResultEventResourcePath, StringComparison.Ordinal))
        {
            return null;
        }

        return ResolveBattleResultEventDataFromSource(
            GetCommonBattleResultEvents(),
            eventType,
            battleContextId);
    }

    private BattleResultEventData[] GetPrimaryBattleResultEvents()
    {
        if (battleResultEvents != null && battleResultEvents.Length > 0)
        {
            return battleResultEvents;
        }

        battleResultEvents = Resources.LoadAll<BattleResultEventData>(battleResultEventResourcePath);
        battleResultEventsLoadedFromResources = true;
        return battleResultEvents;
    }

    private BattleResultEventData[] GetCommonBattleResultEvents()
    {
        if (commonBattleResultEvents == null)
        {
            commonBattleResultEvents =
                Resources.LoadAll<BattleResultEventData>(BattleResultEventResourcePath);
        }

        return commonBattleResultEvents;
    }

    private string ResolveBattleResultEventDataMessageFromSource(
        BattleResultEventData[] sourceEvents,
        BattleResultEventType eventType,
        string battleContextId)
    {
        BattleResultEventData data = ResolveBattleResultEventDataFromSource(sourceEvents, eventType, battleContextId);
        return data != null ? FormatMessageVariables(data.message) : "";
    }

    private static BattleResultEventData ResolveBattleResultEventDataFromSource(
        BattleResultEventData[] sourceEvents,
        BattleResultEventType eventType,
        string battleContextId)
    {
        if (sourceEvents == null || sourceEvents.Length == 0)
        {
            return null;
        }

        BattleResultEventData fallback = null;
        for (int i = 0; i < sourceEvents.Length; i++)
        {
            BattleResultEventData eventData = sourceEvents[i];
            if (eventData == null ||
                eventData.battleResultEventType != eventType ||
                string.IsNullOrEmpty(eventData.message))
            {
                continue;
            }

            if (!string.IsNullOrEmpty(eventData.battleContextId))
            {
                if (string.Equals(eventData.battleContextId, battleContextId, StringComparison.OrdinalIgnoreCase))
                {
                    return eventData;
                }

                continue;
            }

            if (fallback == null)
            {
                fallback = eventData;
            }
        }

        return fallback;
    }

    private string FormatMessageVariables(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return "";
        }

        return message
            .Replace("{heroineName}", ResolveHeroineDisplayName())
            .Replace("{heroineFirstPerson}", ResolveHeroineFirstPerson())
            .Replace("{playerSecondPerson}", ResolvePlayerSecondPerson())
            .Replace("{playerName}", PlayerSpeakerName);
    }

    private string ResolveHeroineDisplayName()
    {
        if (heroineStatus != null && !string.IsNullOrEmpty(heroineStatus.HeroineName))
        {
            return heroineStatus.HeroineName;
        }

        if (heroineProfile != null && !string.IsNullOrEmpty(heroineProfile.displayName))
        {
            return heroineProfile.displayName;
        }

        return "ヒロイン";
    }

    private string ResolveHeroineFirstPerson()
    {
        return heroineProfile != null && !string.IsNullOrEmpty(heroineProfile.heroineFirstPerson)
            ? heroineProfile.heroineFirstPerson
            : "私";
    }

    private string ResolvePlayerSecondPerson()
    {
        return heroineProfile != null && !string.IsNullOrEmpty(heroineProfile.playerSecondPerson)
            ? heroineProfile.playerSecondPerson
            : "あなた";
    }

    private void AddBattleLogFollowUpMessages(SimpleBattleResult result)
    {
        List<string> lines = SplitMessageLines(result.Message);
        if (result.LogLines != null && result.LogLines.Count > 0)
        {
            lines.Add("行動ログ:");
            lines.AddRange(result.LogLines);
        }

        if (lines.Count == 0)
        {
            return;
        }

        AddPagedFollowUpMessages(
            DialogueSpeakerType.BattleLog,
            BattleLogSpeakerName,
            "戦闘ログ",
            lines,
            3);
    }

    private void AddPagedFollowUpMessages(
        DialogueSpeakerType speakerType,
        string speakerName,
        string title,
        IList<string> lines,
        int contentLinesPerPage)
    {
        if (lines == null || lines.Count == 0 || contentLinesPerPage <= 0)
        {
            return;
        }

        int pageCount = (lines.Count + contentLinesPerPage - 1) / contentLinesPerPage;
        for (int pageIndex = 0; pageIndex < pageCount; pageIndex++)
        {
            int start = pageIndex * contentLinesPerPage;
            int end = Math.Min(start + contentLinesPerPage, lines.Count);
            string message = title + " " + (pageIndex + 1) + "/" + pageCount;
            for (int i = start; i < end; i++)
            {
                message += "\n" + lines[i];
            }

            pendingScheduledEventFollowUpMessages.Add(
                new DialogueMessage(speakerType, speakerName, message));
        }
    }

    private static List<string> SplitMessageLines(string message)
    {
        List<string> lines = new List<string>();
        if (string.IsNullOrEmpty(message))
        {
            return lines;
        }

        string[] splitLines = message.Split(new[] { '\n' }, StringSplitOptions.None);
        for (int i = 0; i < splitLines.Length; i++)
        {
            if (!string.IsNullOrEmpty(splitLines[i]))
            {
                lines.Add(splitLines[i]);
            }
        }

        return lines;
    }

    private static bool IsDuoExplorationSchedule(ScheduleType scheduleType)
    {
        return scheduleType == ScheduleType.DuoForest ||
            scheduleType == ScheduleType.DuoCave ||
            scheduleType == ScheduleType.DuoLake;
    }

    private EnemyData ResolveExplorationEnemy(ScheduleType scheduleType)
    {
        string resourcePath = GetExplorationEnemyResourcePath(scheduleType);
        if (string.IsNullOrEmpty(resourcePath))
        {
            return null;
        }

        EnemyData enemy = Resources.Load<EnemyData>(resourcePath);
        if (enemy == null)
        {
            Debug.LogWarning("探索敵データが見つかりません: " + resourcePath);
        }

        return enemy;
    }

    private static string GetExplorationEnemyResourcePath(ScheduleType scheduleType)
    {
        switch (scheduleType)
        {
            case ScheduleType.SoloForest:
            case ScheduleType.DuoForest:
                return "Enemies/ForestSlime";

            case ScheduleType.SoloCave:
            case ScheduleType.DuoCave:
                return "Enemies/CaveBat";

            case ScheduleType.SoloLake:
            case ScheduleType.DuoLake:
                return "Enemies/LakeSpirit";

            default:
                return "";
        }
    }

    private SimpleBattleResult ResolveSimpleBattle(EnemyData enemy, bool includeHeroine)
    {
        SimpleBattleResult result = new SimpleBattleResult();
        result.LogLines = new List<string>();
        if (enemy == null || playerStatus == null)
        {
            result.Message = "戦闘処理を確認できませんでした。";
            return result;
        }

        BattleStatusData enemyStatus = enemy.CreateBattleStatus();
        BattleStatusData playerBattleStatus = playerStatus.BattleStatus;
        BattleStatusData heroineBattleStatus =
            includeHeroine && heroineStatus != null ? heroineStatus.BattleStatus : null;
        bool playerActsFirst = playerBattleStatus == null ||
            playerBattleStatus.speed >= GetSpeed(enemyStatus);
        const int maxTurns = 20;

        for (int turn = 1; turn <= maxTurns; turn++)
        {
            result.Turns = turn;

            if (playerActsFirst)
            {
                int playerDamage = AttackEnemy(playerBattleStatus, enemyStatus);
                AddBattleLogLine(ref result, turn + "T: プレイヤー -> 敵 " + playerDamage);
                if (enemyStatus.currentHp <= 0)
                {
                    ApplySimpleBattleVictory(enemy, ref result);
                    return result;
                }

                int heroineDamage = AttackEnemy(heroineBattleStatus, enemyStatus);
                if (heroineDamage > 0)
                {
                    AddBattleLogLine(ref result, turn + "T: ヒロイン -> 敵 " + heroineDamage);
                }

                if (enemyStatus.currentHp <= 0)
                {
                    ApplySimpleBattleVictory(enemy, ref result);
                    return result;
                }

                AttackParty(enemyStatus, includeHeroine, turn, ref result);
            }
            else
            {
                AttackParty(enemyStatus, includeHeroine, turn, ref result);
                if (playerStatus.CurrentHp <= 0)
                {
                    ApplySimpleBattleDefeat(enemy, ref result);
                    return result;
                }

                int playerDamage = AttackEnemy(playerBattleStatus, enemyStatus);
                AddBattleLogLine(ref result, turn + "T: プレイヤー -> 敵 " + playerDamage);
                if (enemyStatus.currentHp <= 0)
                {
                    ApplySimpleBattleVictory(enemy, ref result);
                    return result;
                }

                int heroineDamage = AttackEnemy(heroineBattleStatus, enemyStatus);
                if (heroineDamage > 0)
                {
                    AddBattleLogLine(ref result, turn + "T: ヒロイン -> 敵 " + heroineDamage);
                }

                if (enemyStatus.currentHp <= 0)
                {
                    ApplySimpleBattleVictory(enemy, ref result);
                    return result;
                }
            }

            if (playerStatus.CurrentHp <= 0)
            {
                ApplySimpleBattleDefeat(enemy, ref result);
                return result;
            }
        }

        ApplySimpleBattleDefeat(enemy, ref result);
        return result;
    }

    private static int AttackEnemy(BattleStatusData attacker, BattleStatusData enemyStatus)
    {
        if (attacker == null || enemyStatus == null || attacker.currentHp <= 0 || enemyStatus.currentHp <= 0)
        {
            return 0;
        }

        int damage = CalculateBattleDamage(attacker, enemyStatus);
        enemyStatus.currentHp -= damage;
        enemyStatus.Clamp();
        return damage;
    }

    private void AttackParty(
        BattleStatusData enemyStatus,
        bool includeHeroine,
        int turn,
        ref SimpleBattleResult result)
    {
        if (enemyStatus == null || enemyStatus.currentHp <= 0)
        {
            return;
        }

        bool canAttackHeroine = includeHeroine && heroineStatus != null && heroineStatus.CurrentHp > 0;
        if (canAttackHeroine && turn % 2 == 0)
        {
            int heroineDamage = heroineStatus.DamageHp(CalculateBattleDamage(enemyStatus, heroineStatus.BattleStatus));
            result.HeroineDamageTaken += heroineDamage;
            AddBattleLogLine(ref result, turn + "T: 敵 -> ヒロイン " + heroineDamage);
            return;
        }

        int playerDamage = playerStatus.DamageHp(CalculateBattleDamage(enemyStatus, playerStatus.BattleStatus));
        result.PlayerDamageTaken += playerDamage;
        AddBattleLogLine(ref result, turn + "T: 敵 -> プレイヤー " + playerDamage);
    }

    private void ApplySimpleBattleVictory(EnemyData enemy, ref SimpleBattleResult result)
    {
        result.PlayerWon = true;
        result.RewardMoney = enemy != null ? Math.Max(0, enemy.rewardMoney) : 0;
        result.AffectionChange = enemy != null ? enemy.affectionChangeOnWin : 0;
        ApplyBattleSkillPointRewards(
            enemy != null ? enemy.playerSkillPointReward : 0,
            enemy != null ? enemy.heroineSkillPointReward : 0,
            ref result);
        RecordMonsterDefeat(enemy != null ? enemy.enemyId : string.Empty);

        if (result.RewardMoney > 0 && playerStatus != null)
        {
            playerStatus.AddMoney(result.RewardMoney);
        }

        if (result.AffectionChange != 0 && heroineStatus != null)
        {
            heroineStatus.AddAffection(result.AffectionChange);
        }

        string message = enemy != null && !string.IsNullOrEmpty(enemy.victoryMessage)
            ? enemy.victoryMessage
            : "戦闘に勝利しました。";
        result.Message =
            message +
            BuildBattleEnemyMessage(enemy) +
            "\n戦闘結果：勝利（" + result.Turns + "ターン）" +
            "\nプレイヤー被ダメージ：" + result.PlayerDamageTaken +
            BuildPlayerHpMessage() +
            BuildHeroineDamageMessage(result.HeroineDamageTaken) +
            BuildHeroineHpMessage() +
            BuildBattleRewardMessage(result);
    }

    private void ApplySimpleBattleDefeat(EnemyData enemy, ref SimpleBattleResult result)
    {
        result.PlayerWon = false;
        if (playerStatus != null && playerStatus.CurrentHp <= 0)
        {
            playerStatus.SetCurrentHp(1);
        }

        if (heroineStatus != null && heroineStatus.CurrentHp <= 0)
        {
            heroineStatus.SetCurrentHp(1);
        }

        string message = enemy != null && !string.IsNullOrEmpty(enemy.defeatMessage)
            ? enemy.defeatMessage
            : "戦闘に敗北しました。";
        result.Message =
            message +
            BuildBattleEnemyMessage(enemy) +
            "\n戦闘結果：敗北（" + result.Turns + "ターン）" +
            "\nプレイヤー被ダメージ：" + result.PlayerDamageTaken +
            BuildPlayerHpMessage() +
            BuildHeroineDamageMessage(result.HeroineDamageTaken) +
            BuildHeroineHpMessage() +
            "\n戦闘報酬なし" +
            "\nHP 1 で撤退しました。" +
            "\n予定は消費済みです。";
    }

    private static int CalculateBattleDamage(BattleStatusData attacker, BattleStatusData defender)
    {
        if (attacker == null)
        {
            return 1;
        }

        int defense = defender != null ? defender.defense : 0;
        return Math.Max(1, attacker.attack - defense);
    }

    private static int GetSpeed(BattleStatusData status)
    {
        return status != null ? status.speed : 0;
    }

    private static string BuildHeroineDamageMessage(int heroineDamageTaken)
    {
        if (heroineDamageTaken <= 0)
        {
            return "";
        }

        return "\nヒロイン被ダメージ：" + heroineDamageTaken;
    }

    private static string BuildBattleEnemyMessage(EnemyData enemy)
    {
        if (enemy == null)
        {
            return "";
        }

        return "\n敵：" + enemy.GetDisplayName();
    }

    private string BuildPlayerHpMessage()
    {
        if (playerStatus == null)
        {
            return "";
        }

        return "\nプレイヤー現在 HP：" + playerStatus.CurrentHp + "/" + playerStatus.MaxHp;
    }

    private string BuildHeroineHpMessage()
    {
        if (heroineStatus == null)
        {
            return "";
        }

        return "\n" + heroineStatus.HeroineName + " 現在 HP：" +
            heroineStatus.CurrentHp + "/" + heroineStatus.MaxHp;
    }

    private static string BuildBattleRewardMessage(SimpleBattleResult result)
    {
        string message = "";
        if (result.RewardMoney > 0)
        {
            message += "\n戦闘報酬：" + result.RewardMoney;
        }

        if (result.AffectionChange != 0)
        {
            message += "\n勝利時好感度：" + FormatSignedValue(result.AffectionChange);
        }

        message += BuildBattleSkillPointRewardMessage(result);

        return message;
    }

    private static string BuildBattleSkillPointRewardMessage(SimpleBattleResult result)
    {
        string message = "";
        if (result.PlayerSkillPointReward > 0)
        {
            message += "\n主人公スキルポイント +" +
                result.PlayerSkillPointReward +
                "（現在 " + result.TotalPlayerSkillPoints + "）";
        }

        if (result.HeroineSkillPointReward > 0)
        {
            message += "\nヒロインスキルポイント +" +
                result.HeroineSkillPointReward +
                "（現在 " + result.TotalHeroineSkillPoints + "）";
        }

        return message;
    }

    private static void AddBattleLogLine(ref SimpleBattleResult result, string line)
    {
        if (string.IsNullOrEmpty(line))
        {
            return;
        }

        if (result.LogLines == null)
        {
            result.LogLines = new List<string>();
        }

        result.LogLines.Add(line);
    }

    private int ApplyPlayerHpChange(int value)
    {
        if (playerStatus == null || value == 0)
        {
            return 0;
        }

        if (value < 0)
        {
            return -playerStatus.DamageHp(-value);
        }

        return playerStatus.RecoverHp(value);
    }

    private int ApplyHeroineHpChange(int value)
    {
        if (heroineStatus == null || value == 0)
        {
            return 0;
        }

        if (value < 0)
        {
            return -heroineStatus.DamageHp(-value);
        }

        return heroineStatus.RecoverHp(value);
    }

    private static string AppendActionHpChangeMessage(
        string baseMessage,
        int appliedPlayerHpChange,
        int appliedHeroineHpChange)
    {
        string message = baseMessage;

        if (appliedPlayerHpChange != 0)
        {
            message = AppendLine(message, "プレイヤーHP " + FormatSignedValue(appliedPlayerHpChange));
        }

        if (appliedHeroineHpChange != 0)
        {
            message = AppendLine(message, "ヒロインHP " + FormatSignedValue(appliedHeroineHpChange));
        }

        return message;
    }

    private static string FormatSignedValue(int value)
    {
        if (value > 0)
        {
            return "+" + value;
        }

        return value.ToString();
    }

    private bool TryOpenDuoShoppingShopPanel(ScheduledEventDefinition scheduledEvent)
    {
        List<ShopItemData> shopItems = GetDuoShoppingShopItems();
        if (shopItems.Count == 0)
        {
            return false;
        }

        EnsureShopPanel();
        if (shopPanel == null)
        {
            ShowScheduleDialogue("ShopPanel が設定されていないため、買い物を開始できません。");
            flowState = ConversationFlowState.Idle;
            RefreshUI();
            return true;
        }

        pendingScheduledEvent = scheduledEvent;
        startPendingScheduledEventAfterOutfitMessage = false;
        returnToScheduledEventPromptAfterOutfitMessage = false;
        currentConversation = null;
        pendingAdvanceTime = false;
        pendingGoodNight = false;

        actionButtonArea.SetActive(false);
        genreButtonArea.SetActive(false);
        choiceButtonArea.SetActive(false);
        outfitPanel.SetActive(false);
        outfitReactionPanel.SetActive(false);
        nextButton.gameObject.SetActive(false);

        ShowScheduleDialogue("購入する商品を選んでください。");
        shopPanel.Open(
            shopItems,
            IsShopItemPurchased,
            MeetsShopItemPurchaseConditions,
            CanAffordShopItem,
            OnSelectDuoShoppingShopItem,
            OnCloseDuoShoppingShopPanel);

        flowState = ConversationFlowState.Idle;
        RefreshUI();
        return true;
    }

    private void OnSelectDuoShoppingShopItem(ShopItemData item)
    {
        ScheduledEventDefinition scheduledEvent = pendingScheduledEvent;
        if (scheduledEvent == null)
        {
            return;
        }

        string purchaseMessage = ApplyDuoShoppingTestPurchase("", item);
        if (!string.IsNullOrWhiteSpace(purchaseMessage))
        {
            pendingDuoShoppingResultMessages.Add(purchaseMessage);
            ShowScheduleDialogue(purchaseMessage + "\nほかの商品を選ぶか、閉じるボタンで買い物を終えてください。");
        }

        RefreshUI();
    }

    private void OnCloseDuoShoppingShopPanel()
    {
        ScheduledEventDefinition scheduledEvent = pendingScheduledEvent;
        if (scheduledEvent != null && pendingDuoShoppingResultMessages.Count > 0)
        {
            string resultMessage = scheduledEvent.EventMessage;
            foreach (string message in pendingDuoShoppingResultMessages)
            {
                resultMessage = AppendLine(resultMessage, message);
            }

            pendingDuoShoppingResultMessages.Clear();
            pendingScheduledEventFollowUpMessages.Clear();
            CompleteScheduledEventWithResolvedMessage(scheduledEvent, resultMessage);
            return;
        }

        pendingDuoShoppingResultMessages.Clear();
        pendingScheduledEvent = null;
        actionButtonArea.SetActive(true);
        genreButtonArea.SetActive(false);
        choiceButtonArea.SetActive(false);
        nextButton.gameObject.SetActive(false);
        ShowScheduleDialogue("買い物をやめました。");
        RefreshUI();
    }

    private void CompleteScheduledEventWithResolvedMessage(
        ScheduledEventDefinition scheduledEvent,
        string eventMessage)
    {
        scheduleManager.MarkTodayScheduleEventExecuted();
        heroineStatus.AddAffection(scheduledEvent.AffectionChange);
        pendingScheduledEvent = null;
        startPendingScheduledEventAfterOutfitMessage = false;
        returnToScheduledEventPromptAfterOutfitMessage = false;
        currentConversation = null;
        pendingAdvanceTime = false;
        pendingGoodNight = false;

        actionButtonArea.SetActive(false);
        genreButtonArea.SetActive(false);
        choiceButtonArea.SetActive(false);
        outfitPanel.SetActive(false);
        outfitReactionPanel.SetActive(false);

        ShowScheduledEventDialogue(scheduledEvent, eventMessage);
        EnqueueScheduledEventFollowUpMessages();

        flowState = ConversationFlowState.ShowingActionResult;
        nextButton.gameObject.SetActive(true);

        RefreshUI();
    }

    private bool IsShopItemPurchased(ShopItemData item)
    {
        if (item != null && item.isBattleConsumable)
        {
            return false;
        }

        return item != null && IsPurchasedItem(item.itemId);
    }

    private bool CanAffordShopItem(ShopItemData item)
    {
        EnsureCoreStatusReferences();
        return playerStatus != null && item != null && playerStatus.Money >= item.price;
    }

    private bool MeetsShopItemPurchaseConditions(ShopItemData item)
    {
        if (item == null)
        {
            return false;
        }

        EnsureCoreStatusReferences();

        if (item.requiredAffection > 0 && (heroineStatus == null || heroineStatus.Affection < item.requiredAffection))
        {
            return false;
        }

        if (item.requiredDay > 0 && (timeManager == null || timeManager.Day < item.requiredDay))
        {
            return false;
        }

        List<string> requiredItemIds = item.GetRequiredPurchasedItemIds();
        for (int i = 0; i < requiredItemIds.Count; i++)
        {
            string requiredItemId = requiredItemIds[i];
            if (!string.IsNullOrEmpty(requiredItemId) && !IsPurchasedItem(requiredItemId))
            {
                return false;
            }
        }

        return true;
    }

    private string ApplyDuoShoppingTestPurchase(string baseMessage, ShopItemData selectedShopItem)
    {
        EnsureCoreStatusReferences();

        if (playerStatus == null)
        {
            return AppendLine(baseMessage, "買い物処理を確認できませんでした。プレイヤーステータスが設定されていません。");
        }

        string itemId = GetShopItemId(selectedShopItem);
        string itemName = GetShopItemName(selectedShopItem);
        int itemPrice = GetShopItemPrice(selectedShopItem);

        if (string.IsNullOrEmpty(itemId))
        {
            return AppendLine(baseMessage, "買い物テスト商品 ID が設定されていません。");
        }

        if (selectedShopItem != null && !MeetsShopItemPurchaseConditions(selectedShopItem))
        {
            return AppendLine(baseMessage, itemName + " はまだ購入条件を満たしていません。");
        }

        if (selectedShopItem != null && selectedShopItem.isBattleConsumable)
        {
            if (!playerStatus.TrySpendMoney(itemPrice))
            {
                return AppendLine(baseMessage, "所持金が足りません。現在の所持金：" + playerStatus.Money);
            }

            itemQuantities.TryGetValue(itemId, out int quantity);
            itemQuantities[itemId] = quantity + 1;
            return AppendLine(baseMessage, itemName + " を購入しました。所持数：" + itemQuantities[itemId] + " / 所持金：" + playerStatus.Money);
        }

        if (IsPurchasedItem(itemId))
        {
            List<string> unlockedOutfitIdsForPurchasedItem = GetShopItemUnlockedOutfitIds(selectedShopItem);
            bool alreadyUnlocked = AreOutfitsUnlocked(unlockedOutfitIdsForPurchasedItem);
            RegisterUnlockedOutfits(unlockedOutfitIdsForPurchasedItem);
            RefreshStatusDetailPanel();

            string purchasedMessage =
                itemName + " は購入済みです。現在の所持金：" + playerStatus.Money;
            if (!alreadyUnlocked)
            {
                string purchasedItemUnlockMessage = BuildUnlockedOutfitMessage(unlockedOutfitIdsForPurchasedItem);
                if (!string.IsNullOrEmpty(purchasedItemUnlockMessage))
                {
                    purchasedMessage += "\n購入済み商品の衣装解放を反映しました。\n" + purchasedItemUnlockMessage;
                }
            }

            return AppendLine(baseMessage, purchasedMessage);
        }

        if (itemPrice <= 0)
        {
            return AppendLine(baseMessage, "買い物テスト費用が 0 以下のため、所持金は変化しませんでした。");
        }

        if (!playerStatus.TrySpendMoney(itemPrice))
        {
            return AppendLine(
                baseMessage,
                "買い物をしようとしましたが、所持金が足りませんでした。現在の所持金：" + playerStatus.Money);
        }

        RegisterPurchasedItem(itemId);
        List<string> unlockedOutfitIdsForPurchase = GetShopItemUnlockedOutfitIds(selectedShopItem);
        RegisterUnlockedOutfits(unlockedOutfitIdsForPurchase);
        RefreshStatusDetailPanel();
        string resultMessage =
            itemName + " を " + itemPrice + " で購入しました。現在の所持金：" + playerStatus.Money;

        string unlockedOutfitMessage = BuildUnlockedOutfitMessage(unlockedOutfitIdsForPurchase);
        if (!string.IsNullOrEmpty(unlockedOutfitMessage))
        {
            resultMessage += "\n" + unlockedOutfitMessage;
        }

        return AppendLine(baseMessage, resultMessage);
    }

    private static string BuildUnlockedOutfitMessage(List<string> outfitIds)
    {
        if (outfitIds == null || outfitIds.Count == 0)
        {
            return "";
        }

        List<string> validOutfitIds = new List<string>();
        foreach (string outfitId in outfitIds)
        {
            if (!string.IsNullOrEmpty(outfitId))
            {
                validOutfitIds.Add(outfitId);
            }
        }

        if (validOutfitIds.Count == 0)
        {
            return "";
        }

        return "衣装 `" + string.Join(", ", validOutfitIds.ToArray()) + "` が解放されました。";
    }

    private bool AreOutfitsUnlocked(List<string> outfitIds)
    {
        if (outfitIds == null || outfitIds.Count == 0)
        {
            return true;
        }

        foreach (string outfitId in outfitIds)
        {
            if (!string.IsNullOrEmpty(outfitId) && !unlockedOutfitIds.Contains(outfitId))
            {
                return false;
            }
        }

        return true;
    }

    private string GetDuoShoppingItemId()
    {
        return GetShopItemId(GetDuoShoppingShopItem());
    }

    private string GetDuoShoppingItemName()
    {
        return GetShopItemName(GetDuoShoppingShopItem());
    }

    private int GetDuoShoppingItemPrice()
    {
        return GetShopItemPrice(GetDuoShoppingShopItem());
    }

    private List<string> GetDuoShoppingUnlockedOutfitIds()
    {
        return GetShopItemUnlockedOutfitIds(GetDuoShoppingShopItem());
    }

    private ShopItemData GetDuoShoppingShopItem()
    {
        if (duoShoppingShopCatalog != null)
        {
            ShopItemData catalogItem = duoShoppingShopCatalog.GetFirstItem();
            if (catalogItem != null)
            {
                return catalogItem;
            }
        }

        return duoShoppingShopItem;
    }

    private List<ShopItemData> GetDuoShoppingShopItems()
    {
        if (duoShoppingShopCatalog != null)
        {
            List<ShopItemData> catalogItems = duoShoppingShopCatalog.GetAvailableItems();
            if (catalogItems.Count > 0)
            {
                return catalogItems;
            }
        }

        List<ShopItemData> shopItems = new List<ShopItemData>();
        if (duoShoppingShopItem != null)
        {
            shopItems.Add(duoShoppingShopItem);
        }

        return shopItems;
    }

    private string GetShopItemId(ShopItemData shopItem)
    {
        if (shopItem != null && !string.IsNullOrEmpty(shopItem.itemId))
        {
            return shopItem.itemId;
        }

        return duoShoppingTestItemId;
    }

    private string GetShopItemName(ShopItemData shopItem)
    {
        if (shopItem != null && !string.IsNullOrEmpty(shopItem.displayName))
        {
            return shopItem.displayName;
        }

        return duoShoppingTestItemName;
    }

    private int GetShopItemPrice(ShopItemData shopItem)
    {
        if (shopItem != null)
        {
            return shopItem.price;
        }

        return duoShoppingTestCost;
    }

    private List<string> GetShopItemUnlockedOutfitIds(ShopItemData shopItem)
    {
        if (shopItem != null)
        {
            return shopItem.GetUnlockedOutfitIds();
        }

        if (duoShoppingUnlockedOutfitIds != null && duoShoppingUnlockedOutfitIds.Count > 0)
        {
            return duoShoppingUnlockedOutfitIds;
        }

        return new List<string> { "Spring", "Summer", "Autumn", "Winter" };
    }

    private static string AppendLine(string baseMessage, string appendedMessage)
    {
        if (string.IsNullOrEmpty(baseMessage))
        {
            return appendedMessage;
        }

        if (string.IsNullOrEmpty(appendedMessage))
        {
            return baseMessage;
        }

        return baseMessage + "\n" + appendedMessage;
    }

    private void ShowScheduledEventDialogue(ScheduledEventDefinition scheduledEvent, string eventMessage)
    {
        string stillId;
        Sprite stillSprite;
        ResolveScheduledEventStill(scheduledEvent, out stillId, out stillSprite);

        switch (scheduledEvent.EventSpeakerType)
        {
            case ScheduledEventSpeakerType.System:
                ShowDialogue(DialogueSpeakerType.System, SystemSpeakerName, eventMessage, stillId, stillSprite);
                return;

            case ScheduledEventSpeakerType.Schedule:
                ShowDialogue(DialogueSpeakerType.Schedule, ScheduleSpeakerName, eventMessage, stillId, stillSprite);
                return;

            case ScheduledEventSpeakerType.Outfit:
                ShowDialogue(DialogueSpeakerType.Outfit, OutfitSpeakerName, eventMessage, stillId, stillSprite);
                return;

            case ScheduledEventSpeakerType.Player:
                ShowDialogue(DialogueSpeakerType.Player, PlayerSpeakerName, eventMessage, stillId, stillSprite);
                return;

            default:
                ShowDialogue(DialogueSpeakerType.Heroine, heroineStatus.HeroineName, eventMessage, stillId, stillSprite);
                return;
        }
    }

    private void ResolveScheduledEventStill(
        ScheduledEventDefinition scheduledEvent,
        out string stillId,
        out Sprite stillSprite)
    {
        stillId = scheduledEvent != null ? scheduledEvent.StillId : string.Empty;
        stillSprite = scheduledEvent != null ? scheduledEvent.StillSprite : null;
        if (scheduledEvent == null)
        {
            return;
        }

        List<string> candidates = GetScheduledEventStillIdCandidates(scheduledEvent);
        foreach (string candidate in candidates)
        {
            if (TryResolveHeroineCatalogSprite(candidate, out Sprite catalogSprite))
            {
                stillId = candidate;
                stillSprite = catalogSprite;
                return;
            }
        }

        if (heroineAssetCatalog != null && currentHeroineId != "DefaultHeroine")
        {
            stillId = string.Empty;
            stillSprite = null;
        }
    }

    private List<string> GetScheduledEventStillIdCandidates(ScheduledEventDefinition scheduledEvent)
    {
        List<string> candidates = new List<string>();
        AddStillCandidate(candidates, scheduledEvent.StillId);

        switch (scheduledEvent.ScheduleType)
        {
            case ScheduleType.SoloForest:
            case ScheduleType.DuoForest:
                AddStillCandidate(candidates, "WithForest_01");
                AddStillCandidate(candidates, "WithForest");
                break;

            case ScheduleType.SoloCave:
            case ScheduleType.DuoCave:
                AddStillCandidate(candidates, "WithCave_01");
                AddStillCandidate(candidates, "WithCave");
                break;

            case ScheduleType.SoloLake:
            case ScheduleType.DuoLake:
                AddStillCandidate(candidates, "WithLake_01");
                AddStillCandidate(candidates, "WithLake");
                break;

            case ScheduleType.SoloShopping:
            case ScheduleType.DuoShopping:
                AddStillCandidate(candidates, "WithShopping_01");
                AddStillCandidate(candidates, "WithTown_01");
                break;
        }

        return candidates;
    }

    private static void AddStillCandidate(List<string> candidates, string stillId)
    {
        if (string.IsNullOrEmpty(stillId) || candidates.Contains(stillId))
        {
            return;
        }

        candidates.Add(stillId);
    }

    private bool TryResolveHeroineCatalogSprite(string assetId, out Sprite sprite)
    {
        sprite = null;
        if (heroineAssetCatalog == null || heroineAssetCatalog.assets == null || string.IsNullOrEmpty(assetId))
        {
            return false;
        }

        foreach (HeroineAssetEntry asset in heroineAssetCatalog.assets)
        {
            if (asset == null || asset.sprite == null)
            {
                continue;
            }

            if (asset.assetId == assetId)
            {
                sprite = asset.sprite;
                return true;
            }
        }

        return false;
    }

    private ScheduledEventDefinition GetScheduledEventDefinition(ScheduleType scheduleType)
    {
        ScheduledEventDefinition dataDefinition = GetScheduledEventDefinitionFromData(scheduleType);

        if (dataDefinition != null)
        {
            return dataDefinition;
        }

        return GetDefaultScheduledEventDefinition(scheduleType);
    }

    private ScheduledEventDefinition GetScheduledEventDefinitionFromData(ScheduleType scheduleType)
    {
        if (scheduledEvents == null)
        {
            return null;
        }

        ScheduledEventData fallback = null;
        ScheduledEventData preferred = null;
        ScheduledEventData costumeFallback = null;
        ScheduledEventData costumePreferred = null;
        string preferredActionId = GetDefaultScheduledEventActionId(scheduleType);
        int matchCount = 0;

        foreach (ScheduledEventData scheduledEvent in scheduledEvents)
        {
            if (scheduledEvent == null)
            {
                continue;
            }

            if (scheduledEvent.scheduleType == scheduleType)
            {
                matchCount++;

                bool hasCostumeCondition = !string.IsNullOrWhiteSpace(scheduledEvent.costumeId);
                if (hasCostumeCondition && !IsCostumeConditionMatch(scheduledEvent.costumeId))
                {
                    continue;
                }

                if (fallback == null)
                {
                    fallback = scheduledEvent;
                }

                if (!string.IsNullOrEmpty(preferredActionId)
                    && scheduledEvent.actionId == preferredActionId)
                {
                    preferred = scheduledEvent;
                }

                if (hasCostumeCondition)
                {
                    if (costumeFallback == null)
                    {
                        costumeFallback = scheduledEvent;
                    }

                    if (!string.IsNullOrEmpty(preferredActionId)
                        && scheduledEvent.actionId == preferredActionId)
                    {
                        costumePreferred = scheduledEvent;
                    }
                }
            }
        }

        if (matchCount > 1)
        {
            Debug.LogWarning("ScheduledEventData の scheduleType が重複しています: " + scheduleType);
        }

        ScheduledEventData selected = costumePreferred != null
            ? costumePreferred
            : costumeFallback != null
                ? costumeFallback
                : preferred != null
                    ? preferred
                    : fallback;
        return selected != null ? selected.ToDefinition() : null;
    }

    private string GetDefaultScheduledEventActionId(ScheduleType scheduleType)
    {
        switch (scheduleType)
        {
            case ScheduleType.SoloForest:
                return "AutoWalkForest";
            case ScheduleType.SoloCave:
                return "AutoWalkCave";
            case ScheduleType.SoloLake:
                return "AutoWalkLake";
            case ScheduleType.SoloShopping:
                return "AutoWalkShopping";
            case ScheduleType.DuoForest:
                return "AutoDuoForest";
            case ScheduleType.DuoCave:
                return "AutoDuoCave";
            case ScheduleType.DuoLake:
                return "AutoDuoLake";
            case ScheduleType.DuoShopping:
                return "AutoDuoShopping";
            case ScheduleType.StayHome:
                return "AutoStayHome";
            default:
                return string.Empty;
        }
    }

    private ScheduledEventDefinition GetDefaultScheduledEventDefinition(ScheduleType scheduleType)
    {
        switch (scheduleType)
        {
            case ScheduleType.SoloForest:
                return new ScheduledEventDefinition(
                    ScheduleType.SoloForest,
                    "AutoWalkForest",
                    TimeSlot.Noon,
                    true,
                    ScheduledEventOutfitPromptMode.Conditional,
                    ScheduledEventSpeakerType.Heroine,
                    "今日は昼に森へ出かける予定です。出発までに服装を整えられます。",
                    "森の中をゆっくり歩きました。木漏れ日の下で、少し気持ちが軽くなります。",
                    1
                );

            case ScheduleType.SoloCave:
                return new ScheduledEventDefinition(
                    ScheduleType.SoloCave,
                    "AutoWalkCave",
                    TimeSlot.Noon,
                    true,
                    ScheduledEventOutfitPromptMode.Conditional,
                    ScheduledEventSpeakerType.Heroine,
                    "今日は昼に洞窟へ向かう予定です。動きやすい服にしておくとよさそうです。",
                    "洞窟の入口まで足を運びました。ひんやりした空気に、少し冒険の気配を感じます。",
                    1
                );

            case ScheduleType.SoloLake:
                return new ScheduledEventDefinition(
                    ScheduleType.SoloLake,
                    "AutoWalkLake",
                    TimeSlot.Noon,
                    true,
                    ScheduledEventOutfitPromptMode.Conditional,
                    ScheduledEventSpeakerType.Heroine,
                    "今日は昼に湖へ行く予定です。水辺に合う服を選ぶ余裕があります。",
                    "湖畔で静かな時間を過ごしました。水面を眺めていると、心が落ち着きます。",
                    1
                );

            case ScheduleType.SoloShopping:
                return new ScheduledEventDefinition(
                    ScheduleType.SoloShopping,
                    "AutoWalkShopping",
                    TimeSlot.Noon,
                    true,
                    ScheduledEventOutfitPromptMode.Conditional,
                    ScheduledEventSpeakerType.Heroine,
                    "今日は昼に買い物へ行く予定です。街に出る服を選んでおけます。",
                    "街で買い物をしました。店先を見て回るだけでも、少し気分が華やぎます。",
                    1
                );

            case ScheduleType.DuoForest:
                return new ScheduledEventDefinition(
                    ScheduleType.DuoForest,
                    "AutoDuoForest",
                    TimeSlot.Noon,
                    true,
                    ScheduledEventOutfitPromptMode.Conditional,
                    ScheduledEventSpeakerType.Heroine,
                    "今日は昼に二人で森へ行く予定です。出発前に衣装を見直せます。",
                    "二人で森を歩きました。並んで歩く時間が、いつもより少し近く感じられます。",
                    3
                );

            case ScheduleType.DuoCave:
                return new ScheduledEventDefinition(
                    ScheduleType.DuoCave,
                    "AutoDuoCave",
                    TimeSlot.Noon,
                    true,
                    ScheduledEventOutfitPromptMode.Conditional,
                    ScheduledEventSpeakerType.Heroine,
                    "今日は昼に二人で洞窟へ行く予定です。動きやすい服装がよさそうです。",
                    "二人で洞窟へ向かいました。暗がりで声を掛け合うたび、距離が少し縮まります。",
                    3
                );

            case ScheduleType.DuoLake:
                return new ScheduledEventDefinition(
                    ScheduleType.DuoLake,
                    "AutoDuoLake",
                    TimeSlot.Noon,
                    true,
                    ScheduledEventOutfitPromptMode.Conditional,
                    ScheduledEventSpeakerType.Heroine,
                    "今日は昼に二人で湖へ行く予定です。水辺に合う服を選ぶ時間があります。",
                    "二人で湖を眺めました。穏やかな水音の中で、会話も自然と柔らかくなります。",
                    3
                );

            case ScheduleType.DuoShopping:
                return new ScheduledEventDefinition(
                    ScheduleType.DuoShopping,
                    "AutoDuoShopping",
                    TimeSlot.Noon,
                    true,
                    ScheduledEventOutfitPromptMode.Conditional,
                    ScheduledEventSpeakerType.Heroine,
                    "今日は昼に二人で買い物へ行く予定です。街歩きの服を選んでおけます。",
                    "二人で買い物に出かけました。選んだものを見せ合う時間が、楽しい思い出になります。",
                    3
                );

            case ScheduleType.StayHome:
                return new ScheduledEventDefinition(
                    ScheduleType.StayHome,
                    "AutoStayHome",
                    TimeSlot.Noon,
                    true,
                    ScheduledEventOutfitPromptMode.Conditional,
                    ScheduledEventSpeakerType.Heroine,
                    "今日は家で過ごす予定です。くつろげる服に着替えてもよさそうです。",
                    "家でゆっくり過ごしました。落ち着いた時間の中で、自然に会話が続きます。",
                    2
                );

            default:
                return null;
        }
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

        if (dayStartMessages == null || dayStartMessages.Count == 0)
        {
            ShowHeroineDialogue(GetMorningGreeting());
        }
        else
        {
            List<DialogueMessage> morningMessages = new List<DialogueMessage>
            {
                new DialogueMessage(
                    DialogueSpeakerType.Heroine,
                    heroineStatus.HeroineName,
                    GetMorningGreeting()
                )
            };

            morningMessages.AddRange(dayStartMessages);
            ShowDialogueSequence(morningMessages);
            dayStartMessages = new List<DialogueMessage>();
        }

        flowState = ConversationFlowState.Idle;

        bool hasQueuedDayStartMessages = queuedDialogueMessages.Count > 0;

        actionButtonArea.SetActive(!hasQueuedDayStartMessages);
        genreButtonArea.SetActive(false);
        choiceButtonArea.SetActive(false);
        outfitPanel.SetActive(false);
        outfitReactionPanel.SetActive(false);
        nextButton.gameObject.SetActive(hasQueuedDayStartMessages);

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
        ShowHeroineDialogue(GetGoodNightGreeting());

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

    public void OpenStatusDetailPanel()
    {
        EnsureStatusDetailPanel();

        if (statusDetailPanel == null)
        {
            Debug.LogWarning("StatusDetailPanel が設定されていません。");
            return;
        }

        statusDetailPanel.OpenPlayerDetail();
    }

    public void AddPlayerMoney(int amount)
    {
        EnsureCoreStatusReferences();

        if (amount <= 0)
        {
            ShowSystemMessage("増やす所持金は 1 以上にしてください。");
            return;
        }

        if (playerStatus == null)
        {
            ShowSystemMessage("プレイヤーステータスが設定されていません。");
            return;
        }

        playerStatus.AddMoney(amount);
        RefreshStatusDetailPanel();
        ShowSystemMessage("所持金が " + amount + " 増えました。現在の所持金：" + playerStatus.Money);
    }

    public bool TrySpendPlayerMoney(int amount)
    {
        EnsureCoreStatusReferences();

        if (amount <= 0)
        {
            ShowSystemMessage("使う所持金は 1 以上にしてください。");
            return false;
        }

        if (playerStatus == null)
        {
            ShowSystemMessage("プレイヤーステータスが設定されていません。");
            return false;
        }

        if (!playerStatus.TrySpendMoney(amount))
        {
            ShowSystemMessage("所持金が足りません。現在の所持金：" + playerStatus.Money);
            return false;
        }

        RefreshStatusDetailPanel();
        ShowSystemMessage("所持金を " + amount + " 使いました。現在の所持金：" + playerStatus.Money);
        return true;
    }

    private void RefreshStatusDetailPanel()
    {
        UpdateMainStatusUI();
        EnsureStatusDetailPanel();

        if (statusDetailPanel != null)
        {
            statusDetailPanel.RefreshStatusDisplay();
        }
    }

    private void UpdateMainStatusUI()
    {
        if (mainPlayerHpText != null)
        {
            mainPlayerHpText.text = "HP: " + FormatStatusHp(
                playerStatus != null ? playerStatus.CurrentHp : 0,
                playerStatus != null ? playerStatus.MaxHp : 0);
        }

        if (mainHeroineHpText != null)
        {
            mainHeroineHpText.text = "HP: " + FormatStatusHp(
                heroineStatus != null ? heroineStatus.CurrentHp : 0,
                heroineStatus != null ? heroineStatus.MaxHp : 0);
        }

        if (mainPlayerMoneyText != null)
        {
            mainPlayerMoneyText.text = "所持金: " + FormatMoney(
                playerStatus != null ? playerStatus.Money : 0);
        }
    }

    private static string FormatStatusHp(int currentHp, int maxHp)
    {
        return currentHp + " / " + maxHp;
    }

    private static string FormatMoney(int money)
    {
        return money + " G";
    }

    public void OpenStillGalleryPanel()
    {
        EnsureStillGalleryPanel();

        if (stillGalleryPanel == null)
        {
            Debug.LogWarning("StillGalleryPanel が設定されていません。");
            return;
        }

        stillGalleryPanel.Open();
    }

    public void OpenMessageLogPanel()
    {
        EnsureMessageLogPanel();

        if (messageLogPanel == null)
        {
            Debug.LogWarning("MessageLogPanel が設定されていません。");
            return;
        }

        messageLogPanel.Open(messageLogEntries);
    }

    public void OpenDebugBattlePanel()
    {
        EnsureBattlePanel();

        if (battlePanel == null)
        {
            Debug.LogWarning("BattlePanel が設定されていません。Canvas 配下に手動で配置し、GameManager に参照を割り当ててください。");
            return;
        }

        actionButtonArea.SetActive(false);
        genreButtonArea.SetActive(false);
        choiceButtonArea.SetActive(false);
        outfitPanel.SetActive(false);
        outfitReactionPanel.SetActive(false);
        nextButton.gameObject.SetActive(false);

        battlePanel.OpenDebugBattle();
    }

    public void OpenTrainingPanel()
    {
        EnsureTrainingPanel();

        if (trainingPanel == null)
        {
            ShowSystemMessage("TrainingPanel が設定されていないため、訓練を開始できません。");
            return;
        }

        EnsureCoreStatusReferences();
        TrainingData[] allTrainings = GetTrainingDataList();
        List<TrainingData> trainings = new List<TrainingData>();
        for (int i = 0; i < allTrainings.Length; i++)
        {
            if (ShouldShowTraining(allTrainings[i]))
            {
                trainings.Add(allTrainings[i]);
            }
        }
        if (trainings.Count == 0)
        {
            ShowSystemMessage("選択できる訓練がありません。");
            return;
        }

        actionButtonArea.SetActive(false);
        genreButtonArea.SetActive(false);
        choiceButtonArea.SetActive(false);
        outfitPanel.SetActive(false);
        outfitReactionPanel.SetActive(false);
        nextButton.gameObject.SetActive(false);

        trainingPanel.SetHeroineSprite(heroineProfile != null ? heroineProfile.defaultHeroineSprite : null);
        trainingPanel.Open(
            trainings,
            playerStatus != null ? playerStatus.BattleStatus : null,
            heroineStatus != null ? heroineStatus.BattleStatus : null);
    }

    public void OnTrainingPanelClosed()
    {
        if (flowState != ConversationFlowState.Idle)
        {
            return;
        }

        actionButtonArea.SetActive(true);
        genreButtonArea.SetActive(false);
        choiceButtonArea.SetActive(false);
        outfitPanel.SetActive(false);
        outfitReactionPanel.SetActive(false);
        nextButton.gameObject.SetActive(false);
        RefreshUI();
    }

    public void OnTrainingPanelResult(TrainingResult result)
    {
        if (result == null)
        {
            return;
        }

        RecordTrainingProgress(result);
        ApplyTrainingResultRewards(result);

        Debug.Log(
            "Training Result: " +
            result.trainingName +
            " / Id: " +
            result.trainingId +
            " / Steps: " +
            result.elapsedSteps +
            " / MaxSteps: " +
            result.maxSteps +
            " / EndReason: " +
            result.endReason +
            " / SimultaneousDown: " +
            result.simultaneousKnockoutCount +
            " / Interrupted: " +
            result.wasInterrupted +
            " / Finished: " +
            result.isFinished +
            " / AffectionReward: " +
            result.totalAffectionReward +
            " / TrainingProficiencyReward: " +
            result.trainingProficiencyReward);
    }

    private void ApplyTrainingResultRewards(TrainingResult result)
    {
        if (result == null)
        {
            return;
        }

        if (result.totalAffectionReward != 0 && heroineStatus != null)
        {
            heroineStatus.AddAffection(result.totalAffectionReward);
            RefreshStatusDetailPanel();
        }

        ApplyTrainingProficiencyRewards(result);

        if (ShouldApplyTrainingRewards(result))
        {
            playerSkillPoints = AddSkillPoints(playerSkillPoints, result.playerSkillPointReward);
            heroineSkillPoints = AddSkillPoints(heroineSkillPoints, result.heroineSkillPointReward);
            result.totalPlayerSkillPoints = playerSkillPoints;
            result.totalHeroineSkillPoints = heroineSkillPoints;
        }

        string resultMessage = BuildTrainingResultMessage(result);

        if (ShouldAdvanceTimeAfterTraining(result))
        {
            AdvanceTimeAfterTrainingResult();
        }

        RefreshUI();
        ShowSystemMessage(resultMessage);
    }

    private bool ShouldAdvanceTimeAfterTraining(TrainingResult result)
    {
        return result != null && result.elapsedSteps > 0;
    }

    private void ApplyTrainingProficiencyRewards(TrainingResult result)
    {
        if (result == null)
        {
            return;
        }

        if (result.progressEntries != null)
        {
            for (int i = 0; i < result.progressEntries.Count; i++)
            {
                TrainingProgressEntry progress = result.progressEntries[i];
                if (progress == null ||
                    string.IsNullOrEmpty(progress.trainingId) ||
                    progress.trainingProficiencyReward <= 0)
                {
                    continue;
                }

                AddTrainingProficiency(
                    progress.trainingId,
                    progress.trainingProficiencyReward);
            }
        }

        if (ShouldApplyTrainingRewards(result) && result.trainingProficiencyReward > 0)
        {
            AddTrainingProficiency(result.trainingId, result.trainingProficiencyReward);
        }

        result.totalTrainingProficiency = GetTrainingProficiency(result.trainingId);
    }

    private void AdvanceTimeAfterTrainingResult()
    {
        pendingAdvanceTime = true;
        pendingGoodNight = ShouldShowGoodNightBeforeAdvance("Training");

        if (!pendingGoodNight)
        {
            timeManager.AdvanceTime();
            pendingAdvanceTime = false;
            RefreshUI();
        }
    }

    private static string BuildTrainingResultMessage(TrainingResult result)
    {
        if (result == null)
        {
            return "";
        }

        bool completed = result.isFinished && !result.wasInterrupted;
        string trainingName = string.IsNullOrEmpty(result.trainingName)
            ? "訓練"
            : AbbreviateTrainingResultText(result.trainingName, 12);
        string stepLimit = result.maxSteps > 0
            ? result.maxSteps.ToString()
            : "∞";
        List<string> lines = new List<string>
        {
            trainingName + ": " + GetTrainingEndReasonLabel(result.endReason) +
                " / " + result.elapsedSteps + "/" + stepLimit + "回" +
                " / 同時限界" + result.simultaneousKnockoutCount + "回"
        };

        int totalProficiencyReward = result.totalStepTrainingProficiencyReward +
            (completed ? result.trainingProficiencyReward : 0);
        lines.Add(
            "報酬: 好感度" + FormatSignedValue(result.totalAffectionReward) +
            " / 熟練度" + FormatSignedValue(totalProficiencyReward) +
            "（現在" + result.totalTrainingProficiency + "）");

        if (!completed)
        {
            lines.Add("完了報酬: なし");
        }
        else if (result.playerSkillPointReward > 0 || result.heroineSkillPointReward > 0)
        {
            lines.Add(
                "SP: 主" + FormatSignedValue(result.playerSkillPointReward) +
                "（" + result.totalPlayerSkillPoints + "）" +
                " / 姫" + FormatSignedValue(result.heroineSkillPointReward) +
                "（" + result.totalHeroineSkillPoints + "）");
        }

        AppendTrainingSkillEffectSummary(lines, result);
        return string.Join("\n", lines.ToArray());
    }

    private static void AppendTrainingSkillEffectSummary(
        List<string> lines,
        TrainingResult result)
    {
        if (lines == null || result == null)
        {
            return;
        }

        bool hasSkillNames = result.effectiveTrainingSkillNames != null &&
            result.effectiveTrainingSkillNames.Count > 0;
        bool hasEffects = result.totalPlayerHpCostReduction != 0 ||
            result.totalHeroineHpCostReduction != 0 ||
            result.totalAffectionRewardModifier != 0 ||
            result.totalTrainingProficiencyRewardModifier != 0;
        if (!hasEffects && !hasSkillNames)
        {
            return;
        }

        lines.Add(
            "スキル効果: HP 主-" + result.totalPlayerHpCostReduction +
            "/姫-" + result.totalHeroineHpCostReduction +
            " / 好" + FormatSignedValue(result.totalAffectionRewardModifier) +
            "/熟" + FormatSignedValue(result.totalTrainingProficiencyRewardModifier));
        if (hasSkillNames)
        {
            lines.Add("発動: " + BuildTrainingSkillNameSummary(
                result.effectiveTrainingSkillNames,
                28));
        }
    }

    private static string BuildTrainingSkillNameSummary(
        List<string> skillNames,
        int maxLength)
    {
        if (skillNames == null || skillNames.Count == 0)
        {
            return "なし";
        }

        string allNames = string.Join(" / ", skillNames.ToArray());
        if (allNames.Length <= maxLength)
        {
            return allNames;
        }

        string suffix = skillNames.Count > 1
            ? " ほか" + (skillNames.Count - 1) + "件"
            : "";
        int nameLength = Mathf.Max(1, maxLength - suffix.Length);
        return AbbreviateTrainingResultText(skillNames[0], nameLength) + suffix;
    }

    private static string AbbreviateTrainingResultText(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
        {
            return value;
        }

        return value.Substring(0, Mathf.Max(1, maxLength - 1)) + "…";
    }

    private static string GetTrainingEndReasonLabel(TrainingEndReason endReason)
    {
        switch (endReason)
        {
            case TrainingEndReason.HpOrLpDepleted:
                return "HP・LP終了";
            case TrainingEndReason.StepLimitReached:
                return "最大ステップ到達";
            case TrainingEndReason.Interrupted:
                return "途中終了";
            default:
                return "未確定";
        }
    }

    private static bool ShouldApplyTrainingRewards(TrainingResult result)
    {
        return result != null && result.isFinished && !result.wasInterrupted;
    }

    public void OnBattlePanelClosed()
    {
        if (waitingForBattlePanelScheduledEvent)
        {
            CompletePendingBattlePanelScheduledEvent();
            return;
        }

        if (flowState != ConversationFlowState.Idle)
        {
            return;
        }

        actionButtonArea.SetActive(true);
        genreButtonArea.SetActive(false);
        choiceButtonArea.SetActive(false);
        outfitPanel.SetActive(false);
        outfitReactionPanel.SetActive(false);
        nextButton.gameObject.SetActive(false);
        RefreshUI();
    }

    private void CompletePendingBattlePanelScheduledEvent()
    {
        ScheduledEventDefinition scheduledEvent = pendingBattlePanelScheduledEvent;
        pendingBattlePanelScheduledEvent = null;
        waitingForBattlePanelScheduledEvent = false;

        if (scheduledEvent == null)
        {
            RefreshUI();
            return;
        }

        if (!hasLastBattlePanelSimpleResult)
        {
            Debug.LogWarning("BattlePanel の予定戦闘結果がないため、予定処理を完了できませんでした。");
            actionButtonArea.SetActive(true);
            genreButtonArea.SetActive(false);
            choiceButtonArea.SetActive(false);
            outfitPanel.SetActive(false);
            outfitReactionPanel.SetActive(false);
            nextButton.gameObject.SetActive(false);
            RefreshUI();
            return;
        }

        CompleteScheduledEvent(scheduledEvent, null, true, lastBattlePanelSimpleResult);
    }

    public void OnBattlePanelResult(BattlePanel.BattleResult result)
    {
        lastBattlePanelResult = result;
        hasLastBattlePanelSimpleResult = false;
        if (result == null)
        {
            return;
        }

        bool isScheduledBattleResult = waitingForBattlePanelScheduledEvent && pendingBattlePanelScheduledEvent != null;
        bool isDuoExploration = isScheduledBattleResult &&
            IsDuoExplorationSchedule(pendingBattlePanelScheduledEvent.ScheduleType);
        if (isScheduledBattleResult && result.resultType == BattlePanel.BattleResultType.Victory)
        {
            RecordMonsterDefeat(result.enemyId);
        }
        List<string> recoveryMessages = ApplyBattlePanelResultStatus(result);
        string resultMessage = ResolveBattlePanelResultMessage(result);
        lastBattlePanelSimpleResult = ConvertBattlePanelResultToSimpleBattleResult(
            result,
            playerStatus != null ? playerStatus.BattleStatus : null,
            result.heroineStatus != null && heroineStatus != null ? heroineStatus.BattleStatus : null,
            recoveryMessages,
            resultMessage,
            isDuoExploration,
            isScheduledBattleResult);
        hasLastBattlePanelSimpleResult = true;

        Debug.Log(
            "[BattlePanel] Result received. resultType=" + result.resultType +
            ", resultLabel=" + result.resultLabel +
            ", enemyId=" + result.enemyId +
            ", enemyName=" + result.enemyName +
            ", turnCount=" + result.turnCount);

        AddMessageLogEntry(
            DialogueSpeakerType.BattleLog,
            BattleLogSpeakerName,
            BuildBattlePanelResultSummaryLogMessage(result));
    }

    private List<string> ApplyBattlePanelResultStatus(BattlePanel.BattleResult result)
    {
        List<string> recoveryMessages = new List<string>();
        if (result == null)
        {
            return recoveryMessages;
        }

        if (playerStatus != null && result.playerStatus != null)
        {
            playerStatus.SetBattleStatus(result.playerStatus);
            if (playerStatus.CurrentHp <= 0)
            {
                playerStatus.SetCurrentHp(1);
                recoveryMessages.Add("プレイヤーは倒れたが、なんとか帰還した。HP 1 で復帰。");
            }
        }

        if (heroineStatus != null && result.heroineStatus != null)
        {
            heroineStatus.SetBattleStatus(result.heroineStatus);
            if (heroineStatus.CurrentHp <= 0)
            {
                heroineStatus.SetCurrentHp(1);
                string heroineName = string.IsNullOrEmpty(heroineStatus.HeroineName) ? "ヒロイン" : heroineStatus.HeroineName;
                recoveryMessages.Add(heroineName + "は倒れたが、なんとか帰還した。HP 1 で復帰。");
            }
        }

        RefreshStatusDetailPanel();
        return recoveryMessages;
    }

    private static string BuildBattlePanelResultLogMessage(
        BattlePanel.BattleResult result,
        BattleStatusData playerBattleStatus,
        BattleStatusData heroineBattleStatus,
        List<string> recoveryMessages,
        string resultMessage,
        int rewardMoney,
        int affectionChange,
        bool includeOutcomeRewardLines)
    {
        if (result == null)
        {
            return "";
        }

        string enemyName = string.IsNullOrEmpty(result.enemyName) ? "敵" : result.enemyName;
        resultMessage = string.IsNullOrEmpty(resultMessage)
            ? GetDefaultBattlePanelResultMessage(result.resultType)
            : resultMessage;

        string message = resultMessage +
            "\n敵: " + enemyName +
            "\nターン数: " + result.turnCount +
            "\nプレイヤーHP: " + FormatBattlePanelHp(playerBattleStatus) +
            "\nヒロインHP: " + FormatBattlePanelHp(heroineBattleStatus);
        if (recoveryMessages != null)
        {
            for (int i = 0; i < recoveryMessages.Count; i++)
            {
                if (!string.IsNullOrEmpty(recoveryMessages[i]))
                {
                    message += "\n" + recoveryMessages[i];
                }
            }
        }

        if (includeOutcomeRewardLines)
        {
            message += BuildBattlePanelOutcomeRewardMessage(result, rewardMoney, affectionChange);
        }

        return message;
    }

    private SimpleBattleResult ConvertBattlePanelResultToSimpleBattleResult(
        BattlePanel.BattleResult result,
        BattleStatusData playerBattleStatus,
        BattleStatusData heroineBattleStatus,
        List<string> recoveryMessages,
        string resultMessage,
        bool isDuoExploration,
        bool applyOutcomeRewards)
    {
        SimpleBattleResult simpleResult = new SimpleBattleResult
        {
            PlayerWon = result != null && result.resultType == BattlePanel.BattleResultType.Victory,
            PlayerEscaped = result != null && result.resultType == BattlePanel.BattleResultType.Escape,
            Turns = result != null ? result.turnCount : 0,
            PlayerDamageTaken = EstimateDamageTaken(playerBattleStatus),
            HeroineDamageTaken = EstimateDamageTaken(heroineBattleStatus),
            RewardMoney = 0,
            AffectionChange = 0,
            PlayerSkillPointReward = 0,
            HeroineSkillPointReward = 0,
            TotalPlayerSkillPoints = playerSkillPoints,
            TotalHeroineSkillPoints = heroineSkillPoints,
            LogLines = new List<string>()
        };
        ApplyBattlePanelOutcomeRewards(ref simpleResult, result, applyOutcomeRewards, isDuoExploration);
        simpleResult.Message = BuildSimpleBattleResultMessage(
            result,
            playerBattleStatus,
            heroineBattleStatus,
            recoveryMessages,
            resultMessage,
            simpleResult.RewardMoney,
            simpleResult.AffectionChange,
            applyOutcomeRewards);
        simpleResult.Message += BuildBattleSkillPointRewardMessage(simpleResult);

        AddBattleLogLine(ref simpleResult, "結果: " + ResolveBattlePanelResultLabel(result));
        AddBattleLogLine(ref simpleResult, "敵: " + ResolveBattlePanelEnemyName(result));
        AddBattleLogLine(ref simpleResult, "ターン数: " + simpleResult.Turns);
        AddBattleLogLine(ref simpleResult, "プレイヤーHP: " + FormatBattlePanelHp(playerBattleStatus));
        AddBattleLogLine(ref simpleResult, "ヒロインHP: " + FormatBattlePanelHp(heroineBattleStatus));
        AddBattleLogLine(ref simpleResult, "敵HP: " + FormatBattlePanelHp(result != null ? result.enemyStatus : null));
        if (recoveryMessages != null)
        {
            for (int i = 0; i < recoveryMessages.Count; i++)
            {
                AddBattleLogLine(ref simpleResult, recoveryMessages[i]);
            }
        }
        if (applyOutcomeRewards)
        {
            AddBattleLogLine(ref simpleResult, BuildBattlePanelOutcomeRewardLogLine(result, simpleResult));
        }

        return simpleResult;
    }

    private void ApplyBattlePanelOutcomeRewards(
        ref SimpleBattleResult result,
        BattlePanel.BattleResult battleResult,
        bool applyOutcomeRewards,
        bool isDuoExploration)
    {
        if (!applyOutcomeRewards || !result.PlayerWon || battleResult == null)
        {
            result.RewardMoney = 0;
            result.AffectionChange = 0;
            result.PlayerSkillPointReward = 0;
            result.HeroineSkillPointReward = 0;
            result.TotalPlayerSkillPoints = playerSkillPoints;
            result.TotalHeroineSkillPoints = heroineSkillPoints;
            return;
        }

        result.RewardMoney = Math.Max(0, battleResult.rewardMoney);
        result.AffectionChange = isDuoExploration ? battleResult.affectionChangeOnWin : 0;
        ApplyBattleSkillPointRewards(
            battleResult.playerSkillPointReward,
            battleResult.heroineSkillPointReward,
            ref result);

        if (result.RewardMoney > 0 && playerStatus != null)
        {
            playerStatus.AddMoney(result.RewardMoney);
        }

        if (result.AffectionChange != 0 && heroineStatus != null)
        {
            heroineStatus.AddAffection(result.AffectionChange);
        }

        RefreshStatusDetailPanel();
    }

    private void ApplyBattleSkillPointRewards(
        int playerReward,
        int heroineReward,
        ref SimpleBattleResult result)
    {
        int previousPlayerPoints = playerSkillPoints;
        int previousHeroinePoints = heroineSkillPoints;
        playerSkillPoints = AddSkillPoints(playerSkillPoints, Math.Max(0, playerReward));
        heroineSkillPoints = AddSkillPoints(heroineSkillPoints, Math.Max(0, heroineReward));
        result.PlayerSkillPointReward = playerSkillPoints - previousPlayerPoints;
        result.HeroineSkillPointReward = heroineSkillPoints - previousHeroinePoints;
        result.TotalPlayerSkillPoints = playerSkillPoints;
        result.TotalHeroineSkillPoints = heroineSkillPoints;
    }

    private static string BuildSimpleBattleResultMessage(
        BattlePanel.BattleResult result,
        BattleStatusData playerBattleStatus,
        BattleStatusData heroineBattleStatus,
        List<string> recoveryMessages,
        string resultMessage,
        int rewardMoney,
        int affectionChange,
        bool includeOutcomeRewardLines)
    {
        if (result == null)
        {
            return "戦闘結果を取得できませんでした。";
        }

        return BuildBattlePanelResultLogMessage(
            result,
            playerBattleStatus,
            heroineBattleStatus,
            recoveryMessages,
            resultMessage,
            rewardMoney,
            affectionChange,
            includeOutcomeRewardLines);
    }

    private string ResolveBattlePanelResultMessage(BattlePanel.BattleResult result)
    {
        BattlePanel.BattleResultType resultType = result != null
            ? result.resultType
            : BattlePanel.BattleResultType.None;
        BattlePanelResultMessageType messageType = ConvertBattlePanelResultMessageType(resultType);
        string message = ResolveBattlePanelResultMessageFromSource(
            GetBattlePanelResultMessages(),
            messageType);
        if (!string.IsNullOrEmpty(message))
        {
            return message;
        }

        if (!string.Equals(
            battlePanelResultMessageResourcePath,
            BattlePanelResultMessageResourcePath,
            StringComparison.Ordinal))
        {
            message = ResolveBattlePanelResultMessageFromSource(
                GetCommonBattlePanelResultMessages(),
                messageType);
            if (!string.IsNullOrEmpty(message))
            {
                return message;
            }
        }

        return GetDefaultBattlePanelResultMessage(resultType);
    }

    private string ResolveBattlePanelResultMessageFromSource(
        BattlePanelResultMessageData[] messages,
        BattlePanelResultMessageType messageType)
    {
        if (messages == null)
        {
            return "";
        }

        for (int i = 0; i < messages.Length; i++)
        {
            BattlePanelResultMessageData messageData = messages[i];
            if (messageData != null &&
                messageData.resultType == messageType &&
                !string.IsNullOrEmpty(messageData.message))
            {
                return FormatMessageVariables(messageData.message);
            }
        }

        return "";
    }

    private BattlePanelResultMessageData[] GetBattlePanelResultMessages()
    {
        if (battlePanelResultMessages == null)
        {
            battlePanelResultMessages =
                Resources.LoadAll<BattlePanelResultMessageData>(battlePanelResultMessageResourcePath);
        }

        return battlePanelResultMessages;
    }

    private BattlePanelResultMessageData[] GetCommonBattlePanelResultMessages()
    {
        if (commonBattlePanelResultMessages == null)
        {
            commonBattlePanelResultMessages =
                Resources.LoadAll<BattlePanelResultMessageData>(BattlePanelResultMessageResourcePath);
        }

        return commonBattlePanelResultMessages;
    }

    private static BattlePanelResultMessageType ConvertBattlePanelResultMessageType(
        BattlePanel.BattleResultType resultType)
    {
        switch (resultType)
        {
            case BattlePanel.BattleResultType.Victory:
                return BattlePanelResultMessageType.Victory;
            case BattlePanel.BattleResultType.Defeat:
                return BattlePanelResultMessageType.Defeat;
            case BattlePanel.BattleResultType.Escape:
                return BattlePanelResultMessageType.Escape;
            default:
                return BattlePanelResultMessageType.Default;
        }
    }

    private static string GetDefaultBattlePanelResultMessage(BattlePanel.BattleResultType resultType)
    {
        switch (resultType)
        {
            case BattlePanel.BattleResultType.Victory:
                return "戦闘に勝利しました。";
            case BattlePanel.BattleResultType.Defeat:
                return "戦闘に敗北しました。";
            case BattlePanel.BattleResultType.Escape:
                return "戦闘から撤退しました。";
            default:
                return "戦闘が終了しました。";
        }
    }

    private static string BuildBattlePanelOutcomeRewardMessage(
        BattlePanel.BattleResult result,
        int rewardMoney,
        int affectionChange)
    {
        if (result == null)
        {
            return "";
        }

        if (result.resultType != BattlePanel.BattleResultType.Victory)
        {
            return result.resultType == BattlePanel.BattleResultType.Escape
                ? "\n撤退したため戦闘報酬なし"
                : "\n戦闘報酬なし";
        }

        string message = "";
        if (rewardMoney > 0)
        {
            message += "\n戦闘報酬：" + rewardMoney;
        }

        if (affectionChange != 0)
        {
            message += "\n勝利時好感度：" + FormatSignedValue(affectionChange);
        }

        return message;
    }

    private static string BuildBattlePanelOutcomeRewardLogLine(
        BattlePanel.BattleResult result,
        SimpleBattleResult simpleResult)
    {
        if (result == null || result.resultType != BattlePanel.BattleResultType.Victory)
        {
            return result != null && result.resultType == BattlePanel.BattleResultType.Escape
                ? "撤退したため戦闘報酬なし"
                : "戦闘報酬なし";
        }

        string line = "";
        if (simpleResult.RewardMoney > 0)
        {
            line = "戦闘報酬: " + simpleResult.RewardMoney;
        }
        if (simpleResult.AffectionChange != 0)
        {
            line = AppendRewardLogPart(
                line,
                "勝利時好感度: " + FormatSignedValue(simpleResult.AffectionChange));
        }

        if (simpleResult.PlayerSkillPointReward > 0)
        {
            line = AppendRewardLogPart(
                line,
                "主人公SP: +" + simpleResult.PlayerSkillPointReward);
        }

        if (simpleResult.HeroineSkillPointReward > 0)
        {
            line = AppendRewardLogPart(
                line,
                "ヒロインSP: +" + simpleResult.HeroineSkillPointReward);
        }

        return string.IsNullOrEmpty(line) ? "戦闘報酬なし" : line;
    }

    private static string AppendRewardLogPart(string current, string part)
    {
        return string.IsNullOrEmpty(current) ? part : current + " / " + part;
    }

    private static string ResolveBattlePanelResultLabel(BattlePanel.BattleResult result)
    {
        if (result == null || string.IsNullOrEmpty(result.resultLabel))
        {
            return "不明";
        }

        return result.resultLabel;
    }

    private static string ResolveBattlePanelEnemyName(BattlePanel.BattleResult result)
    {
        if (result == null || string.IsNullOrEmpty(result.enemyName))
        {
            return "敵";
        }

        return result.enemyName;
    }

    private static string BuildBattlePanelResultSummaryLogMessage(BattlePanel.BattleResult result)
    {
        return "戦闘結果: " + ResolveBattlePanelResultLabel(result) +
            " / 敵: " + ResolveBattlePanelEnemyName(result) +
            " / " + (result != null ? result.turnCount : 0) + "ターン";
    }

    private static string FormatBattlePanelHp(BattleStatusData status)
    {
        if (status == null)
        {
            return "-";
        }

        return status.currentHp + "/" + status.maxHp;
    }

    private static int EstimateDamageTaken(BattleStatusData status)
    {
        if (status == null)
        {
            return 0;
        }

        return Mathf.Max(0, status.maxHp - status.currentHp);
    }

    private void EnsureStatusDetailPanel()
    {
        if (statusDetailPanel == null)
        {
            statusDetailPanel = FindObjectOfType<StatusDetailPanel>();
        }

        if (statusDetailPanel == null)
        {
            Debug.LogWarning("StatusDetailPanel がシーンに配置されていません。Canvas 配下に手動で配置し、GameManager に参照を割り当ててください。");
            return;
        }

        statusDetailPanel.Initialize(this, heroineStatus, timeManager);
    }

    private void EnsureStillGalleryPanel()
    {
        if (stillGalleryPanel == null)
        {
            stillGalleryPanel = FindObjectOfType<StillGalleryPanel>();
        }

        if (stillGalleryPanel == null)
        {
            Debug.LogWarning("StillGalleryPanel がシーンに配置されていません。Canvas 配下に手動で配置し、GameManager に参照を割り当ててください。");
            return;
        }

        stillGalleryPanel.Initialize(this);
    }

    private void EnsureMessageLogPanel()
    {
        if (messageLogPanel == null)
        {
            messageLogPanel = FindObjectOfType<MessageLogPanel>();
        }

        if (messageLogPanel == null)
        {
            GameObject panelObject = GameObject.Find("MessageLogPanel");
            if (panelObject != null)
            {
                messageLogPanel = panelObject.AddComponent<MessageLogPanel>();
            }
        }

        if (messageLogPanel == null)
        {
            Debug.LogWarning("MessageLogPanel がシーンに配置されていません。Canvas 配下に手動で配置してください。");
            return;
        }

        messageLogPanel.Initialize(this);
    }

    private void EnsureBattlePanel()
    {
        if (battlePanel == null)
        {
            battlePanel = FindObjectOfType<BattlePanel>();
        }

        if (battlePanel == null)
        {
            Debug.LogWarning("BattlePanel がシーンに配置されていません。Canvas 配下に手動で配置してください。");
            return;
        }

        EnsureCoreStatusReferences();
        battlePanel.Initialize(this, playerStatus, heroineStatus);
    }

    private void EnsureTrainingPanel()
    {
        if (trainingPanel == null)
        {
            trainingPanel = FindObjectOfType<TrainingPanel>(true);
        }

        if (trainingPanel == null)
        {
            Debug.LogWarning("TrainingPanel がシーンに配置されていません。Canvas 配下に手動で配置してください。");
            return;
        }

        trainingPanel.Initialize(this);
    }

    private TrainingData[] GetTrainingDataList()
    {
        if (trainingDataList == null)
        {
            trainingDataList = Resources.LoadAll<TrainingData>(trainingResourcePath);
            Array.Sort(trainingDataList, (a, b) => string.Compare(
                a != null ? a.trainingId : "",
                b != null ? b.trainingId : "",
                StringComparison.Ordinal));

            Debug.Log(
                "Loaded TrainingData: " +
                trainingDataList.Length +
                " / ResourcePath: " +
                trainingResourcePath);
        }

        return trainingDataList;
    }

    private SkillData[] GetSkillDataList()
    {
        if (skillDataList == null)
        {
            skillDataList = Resources.LoadAll<SkillData>(skillResourcePath);
            Array.Sort(skillDataList, (a, b) =>
            {
                int sortOrderCompare = (a != null ? a.sortOrder : 0).CompareTo(b != null ? b.sortOrder : 0);
                if (sortOrderCompare != 0)
                {
                    return sortOrderCompare;
                }

                return string.Compare(
                    a != null ? a.skillId : "",
                    b != null ? b.skillId : "",
                    StringComparison.Ordinal);
            });
        }

        return skillDataList;
    }

    private void SynchronizeUnlockedSkillsWithSkillTree()
    {
        unlockedSkillIds.Clear();
        List<SkillTreeNodeData> nodes = GetSkillTreeNodes();
        for (int i = 0; i < nodes.Count; i++)
        {
            SkillTreeNodeData node = nodes[i];
            if (node == null ||
                node.owner != SkillTreeOwner.Player ||
                node.skill == null ||
                !IsSkillTreeNodeAcquired(node.nodeId, SkillTreeOwner.Player))
            {
                continue;
            }

            UnlockSkill(node.skill.skillId);
        }
    }

    private void EnsureSkillPanel()
    {
        if (skillPanel == null)
        {
            skillPanel = FindObjectOfType<SkillPanel>(true);
        }

        if (skillPanel == null)
        {
            Debug.LogWarning("SkillPanel がシーンに配置されていません。Canvas 配下に手動で配置してください。");
            return;
        }

        skillPanel.Initialize(this);
    }

    private void EnsureSkillTreePanel()
    {
        if (skillTreePanel == null)
        {
            skillTreePanel = FindObjectOfType<SkillTreePanel>(true);
        }

        if (skillTreePanel != null)
        {
            skillTreePanel.Initialize(this);
        }
    }

    private void EnsureShopPanel()
    {
        if (shopPanel == null)
        {
            Debug.LogWarning("ShopPanel が設定されていません。Canvas 配下に手動で配置し、GameManager に参照を割り当ててください。");
            return;
        }

        shopPanel.Initialize(this);
    }

    public void OnShopPanelClosedByPanel()
    {
        if (pendingScheduledEvent == null)
        {
            return;
        }

        OnCloseDuoShoppingShopPanel();
    }


}
