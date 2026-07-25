#if UNITY_INCLUDE_TESTS
using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class ShopPanelTests
{
    private readonly List<Object> createdObjects = new List<Object>();
    private GameObject root;
    private ShopPanel panel;
    private Transform listParent;
    private Button itemButtonPrefab;
    private Button purchaseButton;
    private Button allCategoryButton;
    private Button outfitCategoryButton;
    private Button consumableCategoryButton;
    private Button otherCategoryButton;
    private TextMeshProUGUI moneyText;
    private TextMeshProUGUI itemNameText;
    private TextMeshProUGUI ownedQuantityText;
    private TextMeshProUGUI requirementText;
    private Color selectedColor;

    [SetUp]
    public void SetUp()
    {
        root = Track(new GameObject("ShopPanelTest"));
        panel = root.AddComponent<ShopPanel>();

        listParent = Track(new GameObject("ShopItemList")).transform;
        listParent.SetParent(root.transform);
        itemButtonPrefab = CreateButton("ShopItemButtonPrefab", root.transform);
        itemButtonPrefab.gameObject.SetActive(false);
        purchaseButton = CreateButton("PurchaseButton", root.transform);
        allCategoryButton = CreateButton("AllCategoryButton", root.transform);
        outfitCategoryButton = CreateButton("OutfitCategoryButton", root.transform);
        consumableCategoryButton = CreateButton("ConsumableCategoryButton", root.transform);
        otherCategoryButton = CreateButton("OtherCategoryButton", root.transform);
        moneyText = CreateText("MoneyText", root.transform);
        itemNameText = CreateText("ItemNameText", root.transform);
        ownedQuantityText = CreateText("OwnedQuantityText", root.transform);
        requirementText = CreateText("RequirementText", root.transform);
        selectedColor = new Color(0.95f, 0.65f, 0.2f, 1f);

        SetField("panelRoot", root);
        SetField("listParent", listParent);
        SetField("itemButtonPrefab", itemButtonPrefab);
        SetField("purchaseButton", purchaseButton);
        SetField("allCategoryButton", allCategoryButton);
        SetField("outfitCategoryButton", outfitCategoryButton);
        SetField("consumableCategoryButton", consumableCategoryButton);
        SetField("otherCategoryButton", otherCategoryButton);
        SetField("moneyText", moneyText);
        SetField("itemNameText", itemNameText);
        SetField("ownedQuantityText", ownedQuantityText);
        SetField("requirementText", requirementText);
        SetField("selectedButtonColor", selectedColor);
    }

    [TearDown]
    public void TearDown()
    {
        for (int i = createdObjects.Count - 1; i >= 0; i--)
        {
            if (createdObjects[i] != null)
            {
                Object.DestroyImmediate(createdObjects[i]);
            }
        }

        createdObjects.Clear();
    }

    [Test]
    public void Open_SelectsFirstItemAndDisplaysItsDetails()
    {
        ShopItemData first = CreateItem("Potion", "回復薬", 100);
        ShopItemData second = CreateItem("Ether", "魔力薬", 150);

        Open(new[] { first, second });

        Assert.That(itemNameText.text, Is.EqualTo("回復薬"));
        Assert.That(GetGeneratedButtons()[0].colors.normalColor, Is.EqualTo(selectedColor));
    }

    [Test]
    public void SelectingItem_UpdatesDetailsAndAllInteractiveStateColors()
    {
        ShopItemData first = CreateItem("Potion", "回復薬", 100);
        ShopItemData second = CreateItem("Ether", "魔力薬", 150);
        Open(new[] { first, second });
        List<Button> buttons = GetGeneratedButtons();
        ColorBlock secondOriginalColors = buttons[1].colors;

        buttons[1].onClick.Invoke();

        Assert.That(itemNameText.text, Is.EqualTo("魔力薬"));
        AssertSelectedColors(buttons[1].colors);
        Assert.That(buttons[0].colors.normalColor, Is.Not.EqualTo(selectedColor));

        buttons[0].onClick.Invoke();

        Assert.That(buttons[1].colors.normalColor, Is.EqualTo(secondOriginalColors.normalColor));
        Assert.That(buttons[1].colors.highlightedColor, Is.EqualTo(secondOriginalColors.highlightedColor));
        Assert.That(buttons[1].colors.selectedColor, Is.EqualTo(secondOriginalColors.selectedColor));
    }

    [TestCase(true, true, true, false, "購入済みです。")]
    [TestCase(false, false, true, false, "購入条件を満たしていません。")]
    [TestCase(false, true, false, false, "所持金が不足しています。")]
    [TestCase(false, true, true, true, "購入できます。")]
    public void Open_UpdatesPurchaseAvailability(
        bool purchased,
        bool meetsCondition,
        bool canAfford,
        bool expectedInteractable,
        string expectedMessage)
    {
        ShopItemData item = CreateItem("Potion", "回復薬", 100);

        Open(
            new[] { item },
            _ => purchased,
            _ => meetsCondition,
            _ => canAfford);

        Assert.That(purchaseButton.interactable, Is.EqualTo(expectedInteractable));
        Assert.That(requirementText.text, Does.Contain(expectedMessage));
    }

    [Test]
    public void PurchaseSelectedItem_RefreshesMoneyListAndDetails()
    {
        ShopItemData item = CreateItem("Potion", "回復薬", 100);
        item.isBattleConsumable = true;
        int money = 300;
        int quantity = 2;
        int purchaseCount = 0;
        Open(
            new[] { item },
            _ => false,
            _ => true,
            _ => money >= item.price,
            () => money,
            _ => quantity,
            purchasedItem =>
            {
                purchaseCount++;
                money -= purchasedItem.price;
                quantity++;
                return "購入しました。";
            });

        purchaseButton.onClick.Invoke();

        Assert.That(purchaseCount, Is.EqualTo(1));
        Assert.That(moneyText.text, Is.EqualTo("所持金: 200G"));
        Assert.That(itemNameText.text, Is.EqualTo("回復薬"));
        Assert.That(ownedQuantityText.text, Is.EqualTo("所持数: 3"));
        Assert.That(GetButtonText(GetGeneratedButtons()[0]), Does.Contain("所持: 3"));
        Assert.That(GetGeneratedButtons(), Has.Count.EqualTo(1));
    }

    [Test]
    public void SelectingItem_DisplaysConsumableQuantityAndOutfitOwnership()
    {
        ShopItemData consumable = CreateItem("Potion", "回復薬", 100);
        consumable.isBattleConsumable = true;
        ShopItemData outfit = CreateItem("SpringOutfit", "春服", 200);

        Open(
            new[] { consumable, outfit },
            item => item == outfit,
            getQuantity: item => item == consumable ? 4 : 0);

        Assert.That(ownedQuantityText.text, Is.EqualTo("所持数: 4"));
        Assert.That(GetButtonText(GetGeneratedButtons()[0]), Does.Contain("所持: 4"));

        GetGeneratedButtons()[1].onClick.Invoke();

        Assert.That(ownedQuantityText.text, Is.EqualTo("所持状態: 購入済み"));
    }

    [Test]
    public void CategoryButtons_FilterItemsAndSelectFirstMatchingItem()
    {
        ShopItemData consumable = CreateItem("Potion", "回復薬", 100);
        consumable.isBattleConsumable = true;
        ShopItemData outfit = CreateItem("SpringOutfit", "春服", 200);
        outfit.unlockedOutfitIds.Add("Spring");
        ShopItemData other = CreateItem("KeyItem", "鍵", 50);
        Open(new[] { consumable, outfit, other });

        outfitCategoryButton.onClick.Invoke();

        Assert.That(GetGeneratedButtons(), Has.Count.EqualTo(1));
        Assert.That(itemNameText.text, Is.EqualTo("春服"));
        Assert.That(outfitCategoryButton.colors.normalColor, Is.EqualTo(selectedColor));
        Assert.That(allCategoryButton.colors.normalColor, Is.Not.EqualTo(selectedColor));

        consumableCategoryButton.onClick.Invoke();

        Assert.That(GetGeneratedButtons(), Has.Count.EqualTo(1));
        Assert.That(itemNameText.text, Is.EqualTo("回復薬"));

        otherCategoryButton.onClick.Invoke();

        Assert.That(GetGeneratedButtons(), Has.Count.EqualTo(1));
        Assert.That(itemNameText.text, Is.EqualTo("鍵"));

        allCategoryButton.onClick.Invoke();

        Assert.That(GetGeneratedButtons(), Has.Count.EqualTo(3));
    }

    [Test]
    public void PurchaseSelectedItem_PreservesCurrentCategory()
    {
        ShopItemData consumable = CreateItem("Potion", "回復薬", 100);
        consumable.isBattleConsumable = true;
        ShopItemData outfit = CreateItem("SpringOutfit", "春服", 200);
        outfit.unlockedOutfitIds.Add("Spring");
        int quantity = 0;
        Open(
            new[] { consumable, outfit },
            getQuantity: _ => quantity,
            onPurchased: _ =>
            {
                quantity++;
                return "購入しました。";
            });
        consumableCategoryButton.onClick.Invoke();

        purchaseButton.onClick.Invoke();

        Assert.That(GetGeneratedButtons(), Has.Count.EqualTo(1));
        Assert.That(itemNameText.text, Is.EqualTo("回復薬"));
        Assert.That(ownedQuantityText.text, Is.EqualTo("所持数: 1"));
        Assert.That(consumableCategoryButton.colors.normalColor, Is.EqualTo(selectedColor));
    }

    [Test]
    public void CappedConsumable_BecomesPurchasableAfterQuantityDrops()
    {
        ShopItemData consumable = CreateItem("Potion", "回復薬", 100);
        consumable.isBattleConsumable = true;
        consumable.maxOwnedQuantity = 9;
        int quantity = 9;
        Open(
            new[] { consumable },
            getQuantity: _ => quantity,
            onPurchased: _ =>
            {
                quantity++;
                return "購入しました。";
            });

        Assert.That(purchaseButton.interactable, Is.False);
        Assert.That(ownedQuantityText.text, Is.EqualTo("所持数: 9 / 9"));
        Assert.That(requirementText.text, Does.Contain("所持上限に達しています。"));
        Assert.That(GetButtonText(GetGeneratedButtons()[0]), Does.Contain("所持: 9 / 9 / 上限"));

        quantity = 8;
        Open(
            new[] { consumable },
            getQuantity: _ => quantity,
            onPurchased: _ =>
            {
                quantity++;
                return "購入しました。";
            });

        Assert.That(purchaseButton.interactable, Is.True);
        purchaseButton.onClick.Invoke();
        Assert.That(quantity, Is.EqualTo(9));
        Assert.That(purchaseButton.interactable, Is.False);
        Assert.That(ownedQuantityText.text, Is.EqualTo("所持数: 9 / 9"));
    }

    [Test]
    public void Open_WithoutPurchaseButton_PreservesImmediatePurchaseBehavior()
    {
        ShopItemData item = CreateItem("Potion", "回復薬", 100);
        int purchaseCount = 0;
        purchaseButton.name = "UnusedPurchaseButton";
        SetField("purchaseButton", null);

        panel.Open(
            new[] { item },
            _ => false,
            _ => true,
            _ => true,
            () => 300,
            _ => 0,
            _ =>
            {
                purchaseCount++;
                return "購入しました。";
            },
            null);

        GetGeneratedButtons()[0].onClick.Invoke();

        Assert.That(purchaseCount, Is.EqualTo(1));
    }

    private void Open(
        IReadOnlyList<ShopItemData> items,
        Func<ShopItemData, bool> isPurchased = null,
        Func<ShopItemData, bool> meetsCondition = null,
        Func<ShopItemData, bool> canAfford = null,
        Func<int> getMoney = null,
        Func<ShopItemData, int> getQuantity = null,
        Func<ShopItemData, string> onPurchased = null)
    {
        panel.Open(
            items,
            isPurchased ?? (_ => false),
            meetsCondition ?? (_ => true),
            canAfford ?? (_ => true),
            getMoney ?? (() => 300),
            getQuantity ?? (_ => 0),
            onPurchased ?? (_ => "購入しました。"),
            null);
    }

    private static string GetButtonText(Button button)
    {
        TMP_Text text = button != null ? button.GetComponentInChildren<TMP_Text>() : null;
        return text != null ? text.text : string.Empty;
    }

    private List<Button> GetGeneratedButtons()
    {
        List<Button> buttons = new List<Button>();
        for (int i = 0; i < listParent.childCount; i++)
        {
            Button button = listParent.GetChild(i).GetComponent<Button>();
            if (button != null)
            {
                buttons.Add(button);
            }
        }

        return buttons;
    }

    private void AssertSelectedColors(ColorBlock colors)
    {
        Assert.That(colors.normalColor, Is.EqualTo(selectedColor));
        Assert.That(colors.highlightedColor, Is.EqualTo(selectedColor));
        Assert.That(colors.pressedColor, Is.EqualTo(selectedColor));
        Assert.That(colors.selectedColor, Is.EqualTo(selectedColor));
    }

    private ShopItemData CreateItem(string itemId, string displayName, int price)
    {
        ShopItemData item = Track(ScriptableObject.CreateInstance<ShopItemData>());
        item.itemId = itemId;
        item.displayName = displayName;
        item.price = price;
        return item;
    }

    private Button CreateButton(string objectName, Transform parent)
    {
        GameObject buttonObject = Track(new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button)));
        buttonObject.transform.SetParent(parent);
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = buttonObject.GetComponent<Image>();
        CreateText("Text", buttonObject.transform);
        return button;
    }

    private TextMeshProUGUI CreateText(string objectName, Transform parent)
    {
        GameObject textObject = Track(new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI)));
        textObject.transform.SetParent(parent);
        return textObject.GetComponent<TextMeshProUGUI>();
    }

    private void SetField(string fieldName, object value)
    {
        FieldInfo field = typeof(ShopPanel).GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, fieldName);
        field.SetValue(panel, value);
    }

    private T Track<T>(T createdObject) where T : Object
    {
        createdObjects.Add(createdObject);
        return createdObject;
    }
}
#endif
