#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class ShopItemPresentationTests
{
    private readonly List<ShopItemData> createdItems = new List<ShopItemData>();

    [TearDown]
    public void TearDown()
    {
        foreach (ShopItemData item in createdItems)
        {
            if (item != null) Object.DestroyImmediate(item);
        }
        createdItems.Clear();
    }

    [Test]
    public void GetDisplayName_UsesItemIdWhenDisplayNameIsEmpty()
    {
        ShopItemData item = CreateItem();
        item.itemId = "Potion";

        Assert.That(ShopItemPresentation.GetDisplayName(item), Is.EqualTo("Potion"));
    }

    [Test]
    public void GetTypeLabel_ClassifiesConsumableOutfitAndOtherItems()
    {
        ShopItemData consumable = CreateItem();
        consumable.isBattleConsumable = true;
        ShopItemData outfit = CreateItem();
        outfit.unlockedOutfitIds.Add("Spring");
        ShopItemData other = CreateItem();

        Assert.That(ShopItemPresentation.GetTypeLabel(consumable), Is.EqualTo("戦闘用消耗品"));
        Assert.That(ShopItemPresentation.GetTypeLabel(outfit), Is.EqualTo("衣装"));
        Assert.That(ShopItemPresentation.GetTypeLabel(other), Is.EqualTo("その他"));
    }

    [Test]
    public void GetDescription_PrefersConfiguredDescription()
    {
        ShopItemData item = CreateItem();
        item.description = "  特別な説明  ";
        item.hpRecoveryAmount = 10;

        Assert.That(ShopItemPresentation.GetDescription(item), Is.EqualTo("特別な説明"));
    }

    [Test]
    public void GetDescription_BuildsFallbackFromEffects()
    {
        ShopItemData item = CreateItem();
        item.hpRecoveryAmount = 20;
        item.mpRecoveryAmount = 5;
        item.unlockedOutfitIds.Add("Casual");

        string description = ShopItemPresentation.GetDescription(item);

        Assert.That(description, Does.Contain("HPを20回復"));
        Assert.That(description, Does.Contain("MPを5回復"));
        Assert.That(description, Does.Contain("Casual"));
    }

    [Test]
    public void GetRequirements_ListsAllConfiguredConditions()
    {
        ShopItemData item = CreateItem();
        item.requiredAffection = 500;
        item.requiredDay = 7;
        item.requiredPurchasedItemIds.Add("FirstItem");

        string requirements = ShopItemPresentation.GetRequirements(item);

        Assert.That(requirements, Does.Contain("好感度 500以上"));
        Assert.That(requirements, Does.Contain("Day 7以降"));
        Assert.That(requirements, Does.Contain("FirstItem"));
    }

    private ShopItemData CreateItem()
    {
        ShopItemData item = ScriptableObject.CreateInstance<ShopItemData>();
        createdItems.Add(item);
        return item;
    }
}
#endif
