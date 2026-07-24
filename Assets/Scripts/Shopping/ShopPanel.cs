using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopPanel : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI emptyText;
    [SerializeField] private Transform listParent;
    [SerializeField] private Button itemButtonPrefab;
    [Header("Item Details")]
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI itemTypeText;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI requirementText;
    [SerializeField] private TextMeshProUGUI purchaseResultText;
    [SerializeField] private Button purchaseButton;
    [SerializeField] private Color selectedButtonColor = new Color(1f, 0.85f, 0.35f, 1f);

    [Header("Controls")]
    [SerializeField] private Button closeButton;
    [SerializeField] private string title = "買い物";
    [SerializeField] private string emptyMessage = "購入できる商品がありません。";

    private readonly List<GameObject> itemButtons = new List<GameObject>();
    private Func<ShopItemData, bool> isPurchasedResolver;
    private Func<ShopItemData, bool> meetsConditionResolver;
    private Func<ShopItemData, bool> canAffordResolver;
    private Func<int> moneyResolver;
    private Func<ShopItemData, string> itemPurchased;
    private Action closed;
    private IReadOnlyList<ShopItemData> currentItems;
    private ShopItemData selectedItem;
    private readonly Dictionary<Button, ColorBlock> originalButtonColors =
        new Dictionary<Button, ColorBlock>();
    private readonly Dictionary<Button, ShopItemData> itemsByButton =
        new Dictionary<Button, ShopItemData>();

    private void Awake()
    {
        EnsureReferences();
    }

    public void Initialize(GameManager manager)
    {
        gameManager = manager;
        EnsureReferences();
    }

    public void Open(
        IReadOnlyList<ShopItemData> items,
        Func<ShopItemData, bool> isPurchased,
        Func<ShopItemData, bool> meetsCondition,
        Func<ShopItemData, bool> canAfford,
        Func<int> getMoney,
        Func<ShopItemData, string> onItemPurchased,
        Action onClosed)
    {
        EnsureReferences();

        isPurchasedResolver = isPurchased;
        meetsConditionResolver = meetsCondition;
        canAffordResolver = canAfford;
        moneyResolver = getMoney;
        itemPurchased = onItemPurchased;
        closed = onClosed;
        currentItems = items;
        selectedItem = null;

        if (titleText != null)
        {
            titleText.text = title;
        }

        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
            panelRoot.transform.SetAsLastSibling();
            if (!panelRoot.activeInHierarchy)
            {
                Debug.LogWarning("ShopPanel.panelRoot は有効ですが、親オブジェクトが非アクティブのため表示されていません。");
            }
        }
        else
        {
            Debug.LogWarning("ShopPanel.panelRoot が設定されていません。");
        }

        RefreshItems(items);
    }

    public void Close()
    {
        CloseWithoutNotify();
        Action closeAction = closed;
        closed = null;
        if (closeAction != null)
        {
            closeAction.Invoke();
        }
        else if (gameManager != null)
        {
            gameManager.OnShopPanelClosedByPanel();
        }
    }

    private void CloseWithoutNotify()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    private void RefreshItems(IReadOnlyList<ShopItemData> items)
    {
        ClearItems();

        bool hasItems = items != null && items.Count > 0;
        if (emptyText != null)
        {
            emptyText.gameObject.SetActive(!hasItems);
            emptyText.text = emptyMessage;
        }

        if (!hasItems || listParent == null || itemButtonPrefab == null)
        {
            if (hasItems)
            {
                Debug.LogWarning("ShopPanel の listParent または itemButtonPrefab が設定されていません。");
            }

            return;
        }

        List<ShopItemData> displayItems = GetDisplayItems(items);
        foreach (ShopItemData item in displayItems)
        {
            CreateItemButton(item);
        }

        if (purchaseButton != null && displayItems.Count > 0)
        {
            SelectItem(displayItems[0]);
        }
        else
        {
            RefreshDetails();
        }
    }

    private List<ShopItemData> GetDisplayItems(IReadOnlyList<ShopItemData> items)
    {
        List<ShopItemData> displayItems = new List<ShopItemData>();
        List<ShopItemData> purchasedItems = new List<ShopItemData>();

        if (items == null)
        {
            return displayItems;
        }

        foreach (ShopItemData item in items)
        {
            if (item == null)
            {
                continue;
            }

            if (IsPurchased(item))
            {
                purchasedItems.Add(item);
            }
            else
            {
                displayItems.Add(item);
            }
        }

        displayItems.AddRange(purchasedItems);
        return displayItems;
    }

    private void CreateItemButton(ShopItemData item)
    {
        Button button = Instantiate(itemButtonPrefab, listParent);
        button.gameObject.SetActive(true);
        itemButtons.Add(button.gameObject);

        TMP_Text buttonText = button.GetComponentInChildren<TMP_Text>();
        if (buttonText != null)
        {
            buttonText.text = BuildItemLabel(item);
        }

        bool canPurchase = CanPurchase(item);
        button.interactable = purchaseButton != null || canPurchase;
        originalButtonColors[button] = button.colors;
        itemsByButton[button] = item;

        button.onClick.RemoveAllListeners();
        if (button.interactable)
        {
            if (purchaseButton != null)
                button.onClick.AddListener(() => SelectItem(item));
            else
                button.onClick.AddListener(() => PurchaseItem(item));
        }
    }

    private string BuildItemLabel(ShopItemData item)
    {
        string itemName = !string.IsNullOrEmpty(item.displayName) ? item.displayName : item.itemId;
        string label = itemName + " / " + item.price + "G";

        if (IsPurchased(item))
        {
            label += " / 購入済み";
        }
        else if (!MeetsCondition(item))
        {
            label += " / 条件未達";
        }
        else if (!CanAfford(item))
        {
            label += " / 所持金不足";
        }

        List<string> outfitIds = item != null ? item.GetUnlockedOutfitIds() : new List<string>();
        if (purchaseButton == null && outfitIds.Count > 0)
        {
            label += " / 解放: " + string.Join(", ", outfitIds.ToArray());
        }

        return label;
    }

    private bool IsPurchased(ShopItemData item)
    {
        return isPurchasedResolver != null && isPurchasedResolver(item);
    }

    private bool MeetsCondition(ShopItemData item)
    {
        return meetsConditionResolver == null || meetsConditionResolver(item);
    }

    private bool CanAfford(ShopItemData item)
    {
        return canAffordResolver == null || canAffordResolver(item);
    }

    public void SelectItem(ShopItemData item)
    {
        selectedItem = item;
        if (purchaseResultText != null) purchaseResultText.text = string.Empty;
        RefreshDetails();
        RefreshSelectionColors();
    }

    public void PurchaseSelectedItem()
    {
        PurchaseItem(selectedItem);
    }

    private void PurchaseItem(ShopItemData item)
    {
        if (!CanPurchase(item))
        {
            if (purchaseResultText != null)
                purchaseResultText.text = GetPurchaseStateMessage(item);
            RefreshDetails();
            return;
        }

        string result = itemPurchased != null ? itemPurchased(item) : string.Empty;
        RefreshItems(currentItems);
        selectedItem = item;
        RefreshDetails();
        RefreshSelectionColors();
        if (purchaseResultText != null) purchaseResultText.text = result;
    }

    private bool CanPurchase(ShopItemData item)
    {
        return item != null && !IsPurchased(item) && MeetsCondition(item) && CanAfford(item);
    }

    private string GetPurchaseStateMessage(ShopItemData item)
    {
        if (item == null) return "商品を選択してください。";
        if (IsPurchased(item)) return "購入済みです。";
        if (!MeetsCondition(item)) return "購入条件を満たしていません。";
        if (!CanAfford(item)) return "所持金が不足しています。";
        return "購入できます。";
    }

    private void RefreshDetails()
    {
        if (moneyText != null)
            moneyText.text = "所持金: " + (moneyResolver != null ? moneyResolver() : 0) + "G";
        SetText(itemNameText, ShopItemPresentation.GetDisplayName(selectedItem));
        SetText(itemTypeText, ShopItemPresentation.GetTypeLabel(selectedItem));
        SetText(priceText, selectedItem != null ? "価格: " + selectedItem.price + "G" : string.Empty);
        SetText(descriptionText, ShopItemPresentation.GetDescription(selectedItem));
        SetText(requirementText, selectedItem != null
            ? ShopItemPresentation.GetRequirements(selectedItem) + "\n" + GetPurchaseStateMessage(selectedItem)
            : string.Empty);
        if (purchaseButton != null) purchaseButton.interactable = CanPurchase(selectedItem);
    }

    private void RefreshSelectionColors()
    {
        foreach (KeyValuePair<Button, ShopItemData> pair in itemsByButton)
        {
            Button button = pair.Key;
            if (button == null) continue;
            ColorBlock colors = originalButtonColors[button];
            if (pair.Value == selectedItem)
            {
                // クリック後は Highlighted / Selected 状態になるため、
                // normalColor だけでなく操作中に使われる色も統一する。
                colors.normalColor = selectedButtonColor;
                colors.highlightedColor = selectedButtonColor;
                colors.pressedColor = selectedButtonColor;
                colors.selectedColor = selectedButtonColor;
            }

            button.colors = colors;
        }
    }

    private static void SetText(TextMeshProUGUI target, string value)
    {
        if (target != null) target.text = value ?? string.Empty;
    }

    private void ClearItems()
    {
        for (int i = 0; i < itemButtons.Count; i++)
        {
            if (itemButtons[i] != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(itemButtons[i]);
                }
                else
                {
                    DestroyImmediate(itemButtons[i]);
                }
            }
        }

        itemButtons.Clear();
        originalButtonColors.Clear();
        itemsByButton.Clear();
    }

    private void EnsureReferences()
    {
        if (panelRoot == null)
        {
            panelRoot = gameObject;
        }

        if (titleText == null)
        {
            titleText = FindText("TitleText");
        }

        if (emptyText == null)
        {
            emptyText = FindText("EmptyText");
        }

        if (listParent == null)
        {
            Transform listTransform = FindChildRecursive(transform, "ShopItemList");
            if (listTransform != null)
            {
                listParent = listTransform;
            }
        }

        if (itemButtonPrefab == null)
        {
            Transform itemButtonTransform = FindChildRecursive(transform, "ShopItemButtonPrefab");
            if (itemButtonTransform != null)
            {
                itemButtonPrefab = itemButtonTransform.GetComponent<Button>();
            }
        }

        if (closeButton == null)
        {
            Transform closeTransform = FindChildRecursive(transform, "CloseButton");
            if (closeTransform != null)
            {
                closeButton = closeTransform.GetComponent<Button>();
            }
        }

        if (moneyText == null) moneyText = FindText("MoneyText");
        if (itemNameText == null) itemNameText = FindText("ItemNameText");
        if (itemTypeText == null) itemTypeText = FindText("ItemTypeText");
        if (priceText == null) priceText = FindText("PriceText");
        if (descriptionText == null) descriptionText = FindText("DescriptionText");
        if (requirementText == null) requirementText = FindText("RequirementText");
        if (purchaseResultText == null) purchaseResultText = FindText("PurchaseResultText");
        if (purchaseButton == null)
        {
            Transform purchaseTransform = FindChildRecursive(transform, "PurchaseButton");
            if (purchaseTransform != null) purchaseButton = purchaseTransform.GetComponent<Button>();
        }

        if (itemButtonPrefab != null)
        {
            itemButtonPrefab.gameObject.SetActive(false);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(Close);
            closeButton.onClick.AddListener(Close);
        }

        if (purchaseButton != null)
        {
            purchaseButton.onClick.RemoveListener(PurchaseSelectedItem);
            purchaseButton.onClick.AddListener(PurchaseSelectedItem);
        }
    }

    private TextMeshProUGUI FindText(string objectName)
    {
        Transform textTransform = FindChildRecursive(transform, objectName);
        return textTransform != null ? textTransform.GetComponent<TextMeshProUGUI>() : null;
    }

    private Transform FindChildRecursive(Transform root, string objectName)
    {
        if (root == null)
        {
            return null;
        }

        if (root.name == objectName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform result = FindChildRecursive(root.GetChild(i), objectName);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }
}
