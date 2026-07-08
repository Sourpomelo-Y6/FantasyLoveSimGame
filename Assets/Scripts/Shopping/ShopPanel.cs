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
    [SerializeField] private Button closeButton;
    [SerializeField] private string title = "買い物";
    [SerializeField] private string emptyMessage = "購入できる商品がありません。";

    private readonly List<GameObject> itemButtons = new List<GameObject>();
    private Func<ShopItemData, bool> isPurchasedResolver;
    private Func<ShopItemData, bool> meetsConditionResolver;
    private Func<ShopItemData, bool> canAffordResolver;
    private Action<ShopItemData> itemSelected;
    private Action closed;
    private IReadOnlyList<ShopItemData> currentItems;

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
        Action<ShopItemData> onItemSelected,
        Action onClosed)
    {
        EnsureReferences();

        isPurchasedResolver = isPurchased;
        meetsConditionResolver = meetsCondition;
        canAffordResolver = canAfford;
        itemSelected = onItemSelected;
        closed = onClosed;
        currentItems = items;

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

        foreach (ShopItemData item in GetDisplayItems(items))
        {
            CreateItemButton(item);
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

        bool isPurchased = IsPurchased(item);
        bool meetsCondition = MeetsCondition(item);
        bool canAfford = CanAfford(item);
        button.interactable = !isPurchased && meetsCondition && canAfford;

        button.onClick.RemoveAllListeners();
        if (button.interactable)
        {
            button.onClick.AddListener(() => SelectItem(item));
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
        if (outfitIds.Count > 0)
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

    private void SelectItem(ShopItemData item)
    {
        itemSelected?.Invoke(item);
        RefreshItems(currentItems);
    }

    private void ClearItems()
    {
        for (int i = 0; i < itemButtons.Count; i++)
        {
            if (itemButtons[i] != null)
            {
                Destroy(itemButtons[i]);
            }
        }

        itemButtons.Clear();
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

        if (itemButtonPrefab != null)
        {
            itemButtonPrefab.gameObject.SetActive(false);
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
