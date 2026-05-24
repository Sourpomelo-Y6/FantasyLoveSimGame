using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatusDetailPanel : MonoBehaviour
{
    [Header("Managers")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private HeroineStatus heroineStatus;

    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button closeButton;

    [Header("Detail View")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI statusSummaryText;
    [SerializeField] private Transform abilityListParent;
    [SerializeField] private Button abilityButtonPrefab;

    [Header("Acquire View")]
    [SerializeField] private GameObject abilityAcquirePanel;
    [SerializeField] private TextMeshProUGUI abilityAcquireTitleText;
    [SerializeField] private TextMeshProUGUI abilityAcquireDescriptionText;
    [SerializeField] private Button abilityAcquireButton;
    [SerializeField] private Button abilityAcquireBackButton;

    [Header("Labels")]
    [SerializeField] private string playerTitle = "プレイヤー詳細ステータス";
    [SerializeField] private string heroineTitle = "ヒロイン詳細ステータス";
    [SerializeField] private string playerSummaryFormat = "プレイヤー能力\n衣装確認モード：{0}\nHidden解放：{1}";
    [SerializeField] private string heroineSummaryFormat = "ヒロイン能力\n衣装確認モード：{0}\nHidden解放：{1}";
    [SerializeField] private string conditionalAbilityName = "衣装確認モード: 条件表示";
    [SerializeField] private string hiddenAbilityName = "衣装確認モード: 非表示";
    [SerializeField] private string conditionalAbilityDescription = "衣装が予定に対して問題ない場合は、出発前の確認を省略できるようにします。";
    [SerializeField] private string hiddenAbilityDescription = "衣装確認そのものを省略し、予定開始時にそのまま進めるようにします。";
    [SerializeField] private string unlockedLabel = "解放済み";
    [SerializeField] private string lockedLabel = "未解放";
    [SerializeField] private string acquireButtonLabel = "解放する";
    [SerializeField] private string acquiredMessage = "解放しました。";

    private StatusDetailRole currentRole = StatusDetailRole.Player;
    private StatusAbilityKind selectedAbilityKind = StatusAbilityKind.ConditionalOutfitPrompt;
    private bool runtimeUiBuilt = false;

    private GameObject PanelRoot
    {
        get { return panelRoot != null ? panelRoot : gameObject; }
    }

    private void Awake()
    {
        EnsureRuntimeUi();

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Close);
        }

        if (abilityAcquireButton != null)
        {
            abilityAcquireButton.onClick.AddListener(UnlockSelectedAbility);
        }

        if (abilityAcquireBackButton != null)
        {
            abilityAcquireBackButton.onClick.AddListener(ShowDetailView);
        }

        HideAllViews();
    }

    private void OnEnable()
    {
        EnsureRuntimeUi();
        Refresh();
    }

    public void Initialize(GameManager manager, HeroineStatus heroine)
    {
        gameManager = manager;
        heroineStatus = heroine;
        EnsureRuntimeUi();
    }

    public void OpenPlayerDetail()
    {
        EnsureRuntimeUi();
        currentRole = StatusDetailRole.Player;
        PanelRoot.SetActive(true);
        ShowDetailView();
    }

    public void OpenHeroineDetail()
    {
        EnsureRuntimeUi();
        currentRole = StatusDetailRole.Heroine;
        PanelRoot.SetActive(true);
        ShowDetailView();
    }

    public void Close()
    {
        PanelRoot.SetActive(false);
    }

    public void ShowAbilityAcquirePanelForConditional()
    {
        EnsureRuntimeUi();
        ShowAbilityAcquireView(StatusAbilityKind.ConditionalOutfitPrompt);
    }

    public void ShowAbilityAcquirePanelForHidden()
    {
        EnsureRuntimeUi();
        ShowAbilityAcquireView(StatusAbilityKind.HiddenOutfitPrompt);
    }

    private void ShowDetailView()
    {
        EnsureRuntimeUi();

        if (abilityAcquirePanel != null)
        {
            abilityAcquirePanel.SetActive(false);
        }

        Refresh();
    }

    private void ShowAbilityAcquireView(StatusAbilityKind abilityKind)
    {
        EnsureRuntimeUi();
        selectedAbilityKind = abilityKind;

        if (abilityAcquirePanel != null)
        {
            abilityAcquirePanel.SetActive(true);
        }

        if (abilityAcquireTitleText != null)
        {
            abilityAcquireTitleText.text = GetAbilityName(abilityKind) + " の解放";
        }

        if (abilityAcquireDescriptionText != null)
        {
            abilityAcquireDescriptionText.text = GetAbilityDescription(abilityKind);
        }

        if (abilityAcquireButton != null)
        {
            abilityAcquireButton.interactable = !IsAbilityUnlocked(abilityKind);

            TextMeshProUGUI buttonLabel = abilityAcquireButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonLabel != null)
            {
                buttonLabel.text = IsAbilityUnlocked(abilityKind) ? unlockedLabel : acquireButtonLabel;
            }
        }
    }

    private void Refresh()
    {
        EnsureRuntimeUi();

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
            statusSummaryText.text = BuildStatusSummary();
        }

        RefreshAbilityList();
    }

    private void RefreshAbilityList()
    {
        if (abilityListParent == null || abilityButtonPrefab == null)
        {
            return;
        }

        ClearAbilityList();

        CreateAbilityButton(StatusAbilityKind.ConditionalOutfitPrompt);
        CreateAbilityButton(StatusAbilityKind.HiddenOutfitPrompt);
    }

    private void CreateAbilityButton(StatusAbilityKind abilityKind)
    {
        Button button = Instantiate(abilityButtonPrefab, abilityListParent);
        button.gameObject.SetActive(true);

        TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            buttonText.text = BuildAbilityButtonLabel(abilityKind);
        }

        button.onClick.AddListener(() => ShowAbilityAcquireView(abilityKind));
    }

    private void ClearAbilityList()
    {
        for (int i = abilityListParent.childCount - 1; i >= 0; i--)
        {
            Destroy(abilityListParent.GetChild(i).gameObject);
        }
    }

    private string BuildStatusSummary()
    {
        OutfitPromptAbilitySet abilities = GetCurrentAbilities();
        if (abilities == null)
        {
            return "能力情報が設定されていません。";
        }

        string conditionalLabel = abilities.canUseConditionalMode ? unlockedLabel : lockedLabel;
        string hiddenLabel = abilities.canUseHiddenMode ? unlockedLabel : lockedLabel;

        string format = currentRole == StatusDetailRole.Player
            ? playerSummaryFormat
            : heroineSummaryFormat;

        return string.Format(format, conditionalLabel, hiddenLabel);
    }

    private string BuildAbilityButtonLabel(StatusAbilityKind abilityKind)
    {
        return GetAbilityName(abilityKind) + " / " + GetAbilityStateText(abilityKind);
    }

    private string GetAbilityStateText(StatusAbilityKind abilityKind)
    {
        return IsAbilityUnlocked(abilityKind) ? unlockedLabel : lockedLabel;
    }

    private string GetAbilityName(StatusAbilityKind abilityKind)
    {
        switch (abilityKind)
        {
            case StatusAbilityKind.HiddenOutfitPrompt:
                return hiddenAbilityName;
            default:
                return conditionalAbilityName;
        }
    }

    private string GetAbilityDescription(StatusAbilityKind abilityKind)
    {
        switch (abilityKind)
        {
            case StatusAbilityKind.HiddenOutfitPrompt:
                return hiddenAbilityDescription;
            default:
                return conditionalAbilityDescription;
        }
    }

    private bool IsAbilityUnlocked(StatusAbilityKind abilityKind)
    {
        OutfitPromptAbilitySet abilities = GetCurrentAbilities();
        if (abilities == null)
        {
            return false;
        }

        switch (abilityKind)
        {
            case StatusAbilityKind.HiddenOutfitPrompt:
                return abilities.canUseHiddenMode;
            default:
                return abilities.canUseConditionalMode;
        }
    }

    private void UnlockSelectedAbility()
    {
        OutfitPromptAbilitySet abilities = GetCurrentAbilities();
        if (abilities == null)
        {
            return;
        }

        switch (selectedAbilityKind)
        {
            case StatusAbilityKind.HiddenOutfitPrompt:
                abilities.canUseHiddenMode = true;
                break;

            default:
                abilities.canUseConditionalMode = true;
                break;
        }

        Refresh();

        if (abilityAcquireDescriptionText != null)
        {
            abilityAcquireDescriptionText.text = GetAbilityDescription(selectedAbilityKind) + "\n" + acquiredMessage;
        }

        if (abilityAcquireButton != null)
        {
            abilityAcquireButton.interactable = false;
            TextMeshProUGUI buttonLabel = abilityAcquireButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonLabel != null)
            {
                buttonLabel.text = unlockedLabel;
            }
        }
    }

    private OutfitPromptAbilitySet GetCurrentAbilities()
    {
        if (currentRole == StatusDetailRole.Player)
        {
            return gameManager != null ? gameManager.PlayerOutfitPromptAbilities : null;
        }

        return heroineStatus != null ? heroineStatus.OutfitPromptAbilities : null;
    }

    private void HideAllViews()
    {
        if (abilityAcquirePanel != null)
        {
            abilityAcquirePanel.SetActive(false);
        }
    }

    private void EnsureRuntimeUi()
    {
        if (runtimeUiBuilt)
        {
            return;
        }

        if (titleText != null &&
            statusSummaryText != null &&
            abilityListParent != null &&
            abilityButtonPrefab != null &&
            abilityAcquirePanel != null)
        {
            runtimeUiBuilt = true;
            return;
        }

        BuildRuntimeUi();
        runtimeUiBuilt = true;
    }

    private void BuildRuntimeUi()
    {
        RectTransform rootRect = PanelRoot.GetComponent<RectTransform>();
        if (rootRect == null)
        {
            rootRect = PanelRoot.AddComponent<RectTransform>();
        }

        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        Image rootImage = PanelRoot.GetComponent<Image>();
        if (rootImage == null)
        {
            rootImage = PanelRoot.AddComponent<Image>();
        }

        rootImage.color = new Color(0f, 0f, 0f, 0.72f);

        GameObject contentObject = new GameObject("Content", typeof(RectTransform), typeof(Image));
        contentObject.transform.SetParent(PanelRoot.transform, false);
        RectTransform contentRect = contentObject.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.5f, 0.5f);
        contentRect.anchorMax = new Vector2(0.5f, 0.5f);
        contentRect.sizeDelta = new Vector2(920f, 620f);
        contentRect.anchoredPosition = Vector2.zero;

        Image contentImage = contentObject.GetComponent<Image>();
        contentImage.color = new Color(0.1f, 0.1f, 0.12f, 0.95f);

        TMP_FontAsset fontAsset = ResolveFontAsset();

        GameObject titleObject = CreateTextObject("Title", contentObject.transform, fontAsset, 30, TextAlignmentOptions.Left, new Vector2(30f, 565f), new Vector2(500f, 40f));
        titleText = titleObject.GetComponent<TextMeshProUGUI>();

        GameObject closeObject = CreateButtonObject("CloseButton", contentObject.transform, fontAsset, "閉じる", new Vector2(790f, 560f), new Vector2(100f, 36f));
        closeButton = closeObject.GetComponent<Button>();
        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(Close);

        GameObject summaryObject = CreateTextObject("Summary", contentObject.transform, fontAsset, 24, TextAlignmentOptions.TopLeft, new Vector2(30f, 470f), new Vector2(350f, 110f));
        statusSummaryText = summaryObject.GetComponent<TextMeshProUGUI>();

        GameObject playerTab = CreateButtonObject("PlayerTab", contentObject.transform, fontAsset, "プレイヤー", new Vector2(30f, 420f), new Vector2(150f, 40f));
        playerTab.GetComponent<Button>().onClick.RemoveAllListeners();
        playerTab.GetComponent<Button>().onClick.AddListener(OpenPlayerDetail);

        GameObject heroineTab = CreateButtonObject("HeroineTab", contentObject.transform, fontAsset, "ヒロイン", new Vector2(190f, 420f), new Vector2(150f, 40f));
        heroineTab.GetComponent<Button>().onClick.RemoveAllListeners();
        heroineTab.GetComponent<Button>().onClick.AddListener(OpenHeroineDetail);

        GameObject abilityLabelObject = CreateTextObject("AbilityLabel", contentObject.transform, fontAsset, 24, TextAlignmentOptions.Left, new Vector2(30f, 360f), new Vector2(280f, 32f));
        abilityLabelObject.GetComponent<TextMeshProUGUI>().text = "能力一覧";

        GameObject listObject = new GameObject("AbilityList", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        listObject.transform.SetParent(contentObject.transform, false);
        RectTransform listRect = listObject.GetComponent<RectTransform>();
        listRect.anchorMin = new Vector2(0f, 0f);
        listRect.anchorMax = new Vector2(0f, 0f);
        listRect.pivot = new Vector2(0f, 1f);
        listRect.anchoredPosition = new Vector2(30f, 330f);
        listRect.sizeDelta = new Vector2(360f, 240f);

        VerticalLayoutGroup layoutGroup = listObject.GetComponent<VerticalLayoutGroup>();
        layoutGroup.spacing = 10f;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.childControlHeight = true;
        layoutGroup.childControlWidth = true;

        ContentSizeFitter fitter = listObject.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        abilityListParent = listObject.transform;
        abilityButtonPrefab = CreateAbilityButtonTemplate(fontAsset);

        abilityAcquirePanel = CreateAcquirePanel(contentObject.transform, fontAsset);
        abilityAcquirePanel.SetActive(false);

        RefreshAbilityList();
        ShowDetailView();
    }

    private TMP_FontAsset ResolveFontAsset()
    {
        if (titleText != null && titleText.font != null)
        {
            return titleText.font;
        }

        if (statusSummaryText != null && statusSummaryText.font != null)
        {
            return statusSummaryText.font;
        }

        TextMeshProUGUI[] texts = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>();
        foreach (TextMeshProUGUI text in texts)
        {
            if (text != null && text.font != null)
            {
                return text.font;
            }
        }

        TMP_FontAsset[] fontAssets = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        foreach (TMP_FontAsset fontAsset in fontAssets)
        {
            if (fontAsset != null)
            {
                return fontAsset;
            }
        }

        return null;
    }

    private GameObject CreateTextObject(
        string name,
        Transform parent,
        TMP_FontAsset fontAsset,
        int fontSize,
        TextAlignmentOptions alignment,
        Vector2 anchoredPosition,
        Vector2 sizeDelta)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(0f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = sizeDelta;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = fontAsset;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = alignment;
        text.enableWordWrapping = true;

        return textObject;
    }

    private GameObject CreateButtonObject(
        string name,
        Transform parent,
        TMP_FontAsset fontAsset,
        string label,
        Vector2 anchoredPosition,
        Vector2 sizeDelta)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(0f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = sizeDelta;

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.9f);

        GameObject labelObject = CreateTextObject("Text", buttonObject.transform, fontAsset, 20, TextAlignmentOptions.Center, new Vector2(0f, 0f), sizeDelta);
        labelObject.GetComponent<TextMeshProUGUI>().text = label;
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;

        LayoutElement layoutElement = buttonObject.GetComponent<LayoutElement>();
        layoutElement.preferredWidth = sizeDelta.x;
        layoutElement.preferredHeight = sizeDelta.y;

        return buttonObject;
    }

    private Button CreateAbilityButtonTemplate(TMP_FontAsset fontAsset)
    {
        GameObject buttonObject = CreateButtonObject(
            "AbilityButtonTemplate",
            PanelRoot.transform,
            fontAsset,
            "能力",
            Vector2.zero,
            new Vector2(300f, 44f)
        );

        buttonObject.SetActive(false);
        return buttonObject.GetComponent<Button>();
    }

    private GameObject CreateAcquirePanel(Transform parent, TMP_FontAsset fontAsset)
    {
        GameObject panelObject = new GameObject("AbilityAcquirePanel", typeof(RectTransform), typeof(Image));
        panelObject.transform.SetParent(parent, false);

        RectTransform rectTransform = panelObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = new Vector2(420f, 360f);

        Image image = panelObject.GetComponent<Image>();
        image.color = new Color(0.08f, 0.08f, 0.1f, 0.98f);

        abilityAcquireTitleText = CreateTextObject("AcquireTitle", panelObject.transform, fontAsset, 28, TextAlignmentOptions.Left, new Vector2(24f, 312f), new Vector2(360f, 40f)).GetComponent<TextMeshProUGUI>();
        abilityAcquireDescriptionText = CreateTextObject("AcquireDescription", panelObject.transform, fontAsset, 22, TextAlignmentOptions.TopLeft, new Vector2(24f, 250f), new Vector2(360f, 150f)).GetComponent<TextMeshProUGUI>();

        abilityAcquireButton = CreateButtonObject("AcquireButton", panelObject.transform, fontAsset, acquireButtonLabel, new Vector2(24f, 58f), new Vector2(150f, 44f)).GetComponent<Button>();
        abilityAcquireButton.onClick.RemoveAllListeners();
        abilityAcquireButton.onClick.AddListener(UnlockSelectedAbility);

        abilityAcquireBackButton = CreateButtonObject("AcquireBackButton", panelObject.transform, fontAsset, "戻る", new Vector2(190f, 58f), new Vector2(120f, 44f)).GetComponent<Button>();
        abilityAcquireBackButton.onClick.RemoveAllListeners();
        abilityAcquireBackButton.onClick.AddListener(ShowDetailView);

        return panelObject;
    }
}
