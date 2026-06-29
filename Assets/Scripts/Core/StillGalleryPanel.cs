using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StillGalleryPanel : MonoBehaviour
{
    [Header("Managers")]
    [SerializeField] private GameManager gameManager;

    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button closeButton;

    [Header("Gallery")]
    [SerializeField] private Transform listParent;
    [SerializeField] private Button itemButtonPrefab;
    [SerializeField] private Image stillImage;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI emptyText;
    [SerializeField] private int itemsPerPage = 8;
    [SerializeField] private Button previousPageButton;
    [SerializeField] private Button nextPageButton;
    [SerializeField] private TextMeshProUGUI pageText;

    [Header("Labels")]
    [SerializeField] private string defaultTitle = "イベント回想";
    [SerializeField] private string emptyMessage = "解放済みのスチルはありません。";
    [SerializeField] private string lockedStillLabel = "???";

    private bool hasWarnedMissingReferences = false;
    private readonly List<GameManager.StillGalleryItem> cachedItems = new List<GameManager.StillGalleryItem>();
    private int currentPageIndex = 0;

    private GameObject PanelRoot
    {
        get { return panelRoot != null ? panelRoot : gameObject; }
    }

    private void Awake()
    {
        EnsureUiReferences();

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Close);
        }

        if (previousPageButton != null)
        {
            previousPageButton.onClick.AddListener(ShowPreviousPage);
        }

        if (nextPageButton != null)
        {
            nextPageButton.onClick.AddListener(ShowNextPage);
        }

        if (stillImage != null)
        {
            stillImage.preserveAspect = true;
        }

        HideTemplateButton();
        Close();
    }

    private void OnEnable()
    {
        EnsureUiReferences();
    }

    public void Initialize(GameManager manager)
    {
        gameManager = manager;
        EnsureUiReferences();
    }

    public void Open()
    {
        EnsureUiReferences();
        PanelRoot.SetActive(true);
        currentPageIndex = 0;
        Refresh();
    }

    public void Close()
    {
        PanelRoot.SetActive(false);
    }

    public void Refresh()
    {
        EnsureUiReferences();

        if (!PanelRoot.activeSelf)
        {
            return;
        }

        if (titleText != null)
        {
            titleText.text = defaultTitle;
        }

        cachedItems.Clear();
        ClearList();
        HideTemplateButton();

        if (gameManager == null)
        {
            SetEmptyState(true);
            ClearPreview();
            UpdatePageControls();
            return;
        }

        List<GameManager.StillGalleryItem> items = gameManager.GetStillGalleryItems(false);
        if (items == null || items.Count == 0)
        {
            SetEmptyState(true);
            ClearPreview();
            UpdatePageControls();
            return;
        }

        cachedItems.AddRange(items);
        ClampCurrentPageIndex();
        SetEmptyState(false);
        RefreshPageItems();
    }

    private void RefreshPageItems()
    {
        ClearList();
        HideTemplateButton();

        if (cachedItems.Count == 0)
        {
            SetEmptyState(true);
            ClearPreview();
            UpdatePageControls();
            return;
        }

        SetEmptyState(false);

        int startIndex = currentPageIndex * GetItemsPerPage();
        int endIndex = Mathf.Min(startIndex + GetItemsPerPage(), cachedItems.Count);

        for (int i = startIndex; i < endIndex; i++)
        {
            CreateGalleryButton(cachedItems[i]);
        }

        ShowFirstUnlockedStillInCurrentPage();
        UpdatePageControls();
    }

    private void CreateGalleryButton(GameManager.StillGalleryItem item)
    {
        if (listParent == null || itemButtonPrefab == null)
        {
            return;
        }

        Button button = Instantiate(itemButtonPrefab, listParent);
        button.gameObject.SetActive(true);
        bool isUnlocked = gameManager != null && gameManager.IsStillUnlocked(item.StillId);
        button.interactable = isUnlocked;

        TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            buttonText.text = isUnlocked ? item.StillId : lockedStillLabel;
        }

        if (isUnlocked)
        {
            button.onClick.AddListener(() => ShowStill(item));
        }
    }

    private void ShowFirstUnlockedStillInCurrentPage()
    {
        if (gameManager == null)
        {
            ClearPreview();
            return;
        }

        int startIndex = currentPageIndex * GetItemsPerPage();
        int endIndex = Mathf.Min(startIndex + GetItemsPerPage(), cachedItems.Count);

        for (int i = startIndex; i < endIndex; i++)
        {
            GameManager.StillGalleryItem item = cachedItems[i];
            if (gameManager.IsStillUnlocked(item.StillId))
            {
                ShowStill(item);
                return;
            }
        }

        ClearPreview();
    }

    private void ShowPreviousPage()
    {
        if (currentPageIndex <= 0)
        {
            return;
        }

        currentPageIndex--;
        RefreshPageItems();
    }

    private void ShowNextPage()
    {
        int totalPages = GetTotalPages();
        if (currentPageIndex >= totalPages - 1)
        {
            return;
        }

        currentPageIndex++;
        RefreshPageItems();
    }

    private void UpdatePageControls()
    {
        int totalPages = GetTotalPages();
        bool hasMultiplePages = totalPages > 1;

        if (previousPageButton != null)
        {
            previousPageButton.interactable = hasMultiplePages && currentPageIndex > 0;
        }

        if (nextPageButton != null)
        {
            nextPageButton.interactable = hasMultiplePages && currentPageIndex < totalPages - 1;
        }

        if (pageText != null)
        {
            pageText.text = totalPages > 0
                ? (currentPageIndex + 1) + " / " + totalPages
                : "0 / 0";
        }
    }

    private int GetItemsPerPage()
    {
        return Mathf.Max(1, itemsPerPage);
    }

    private int GetTotalPages()
    {
        if (cachedItems.Count == 0)
        {
            return 0;
        }

        return Mathf.CeilToInt((float)cachedItems.Count / GetItemsPerPage());
    }

    private void ClampCurrentPageIndex()
    {
        int totalPages = GetTotalPages();
        if (totalPages <= 0)
        {
            currentPageIndex = 0;
            return;
        }

        currentPageIndex = Mathf.Clamp(currentPageIndex, 0, totalPages - 1);
    }

    private void ShowStill(GameManager.StillGalleryItem item)
    {
        if (stillImage != null)
        {
            stillImage.sprite = item.Sprite;
            stillImage.preserveAspect = true;
            stillImage.gameObject.SetActive(item.Sprite != null);
        }

        if (titleText != null)
        {
            titleText.text = item.StillId;
        }
    }

    private void ClearPreview()
    {
        if (stillImage != null)
        {
            stillImage.sprite = null;
            stillImage.gameObject.SetActive(false);
        }

        if (titleText != null)
        {
            titleText.text = defaultTitle;
        }
    }

    private void SetEmptyState(bool isEmpty)
    {
        if (emptyText != null)
        {
            emptyText.gameObject.SetActive(isEmpty);
            emptyText.text = emptyMessage;
        }
    }

    private void ClearList()
    {
        if (listParent == null)
        {
            return;
        }

        Transform templateTransform = itemButtonPrefab != null ? itemButtonPrefab.transform : null;

        for (int i = listParent.childCount - 1; i >= 0; i--)
        {
            Transform child = listParent.GetChild(i);
            if (child == templateTransform)
            {
                continue;
            }

            Destroy(child.gameObject);
        }
    }

    private void HideTemplateButton()
    {
        if (itemButtonPrefab != null && itemButtonPrefab.transform.parent == listParent)
        {
            itemButtonPrefab.gameObject.SetActive(false);
        }
    }

    private void EnsureUiReferences()
    {
        if (panelRoot == null)
        {
            panelRoot = gameObject;
        }

        if (gameManager == null)
        {
            gameManager = FindObjectOfType<GameManager>();
        }

        if (!hasWarnedMissingReferences &&
            (listParent == null ||
             itemButtonPrefab == null ||
             stillImage == null ||
             closeButton == null))
        {
            Debug.LogWarning("StillGalleryPanel の UI 参照が不足しています。Hierarchy 上に UI を配置し、Inspector で参照を割り当ててください。");
            hasWarnedMissingReferences = true;
        }
    }
}
