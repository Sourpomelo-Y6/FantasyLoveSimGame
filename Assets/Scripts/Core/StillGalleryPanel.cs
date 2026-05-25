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

    [Header("Labels")]
    [SerializeField] private string defaultTitle = "イベント回想";
    [SerializeField] private string emptyMessage = "解放済みのスチルはありません。";

    private bool hasWarnedMissingReferences = false;

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

        ClearList();
        HideTemplateButton();

        if (gameManager == null)
        {
            SetEmptyState(true);
            ClearPreview();
            return;
        }

        List<GameManager.StillGalleryItem> items = gameManager.GetUnlockedStillGalleryItems();
        if (items == null || items.Count == 0)
        {
            SetEmptyState(true);
            ClearPreview();
            return;
        }

        SetEmptyState(false);

        foreach (GameManager.StillGalleryItem item in items)
        {
            CreateGalleryButton(item);
        }

        ShowStill(items[0]);
    }

    private void CreateGalleryButton(GameManager.StillGalleryItem item)
    {
        if (listParent == null || itemButtonPrefab == null)
        {
            return;
        }

        Button button = Instantiate(itemButtonPrefab, listParent);
        button.gameObject.SetActive(true);

        TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            buttonText.text = item.StillId;
        }

        button.onClick.AddListener(() => ShowStill(item));
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
