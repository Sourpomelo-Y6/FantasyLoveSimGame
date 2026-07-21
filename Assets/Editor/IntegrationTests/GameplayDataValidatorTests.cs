#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class GameplayDataValidatorTests
{
    private readonly List<ScriptableObject> createdObjects = new List<ScriptableObject>();

    [TearDown]
    public void TearDown()
    {
        foreach (ScriptableObject createdObject in createdObjects)
        {
            if (createdObject != null) Object.DestroyImmediate(createdObject);
        }
        createdObjects.Clear();
    }

    [Test]
    public void ValidateProjectAssets_CurrentGameplayDataHasNoWarnings()
    {
        GameplayDataValidationReport training = GameplayDataValidator.ValidateTrainingProjectAssets();
        GameplayDataValidationReport enemy = GameplayDataValidator.ValidateEnemyProjectAssets();
        GameplayDataValidationReport shop = GameplayDataValidator.ValidateShopProjectAssets();

        Assert.That(training.Warnings.Select(value => value.Message), Is.Empty, training.CreateSummary());
        Assert.That(enemy.Warnings.Select(value => value.Message), Is.Empty, enemy.CreateSummary());
        Assert.That(shop.Warnings.Select(value => value.Message), Is.Empty, shop.CreateSummary());
        Assert.That(training.AssetCount, Is.GreaterThan(0));
        Assert.That(enemy.AssetCount, Is.GreaterThan(0));
        Assert.That(shop.AssetCount, Is.GreaterThan(0));
    }

    [Test]
    public void ValidateTrainingForTests_DetectsInvalidValuesAndReferences()
    {
        TrainingData training = Create<TrainingData>();
        training.trainingId = "invalid-id";
        training.trainingCategoryId = "";
        training.displayName = "";
        training.description = "";
        training.playerHpCostPerStep = -1;
        training.initialPlayerLp = 0;

        HeroineTrainingDialogueData dialogue = Create<HeroineTrainingDialogueData>();
        dialogue.entries.Add(new HeroineTrainingDialogueEntry
        {
            trainingId = "MissingTraining",
            visualState = TrainingVisualState.SelectedBeforeFirstStep,
            messages = new List<string>()
        });

        GameplayDataValidationReport report = GameplayDataValidator.ValidateTrainingForTests(
            new[] { training }, new[] { dialogue }, null, null);

        string[] warnings = report.Warnings.Select(value => value.Message).ToArray();
        Assert.That(warnings.Any(value => value.Contains("使用できない文字")), Is.True);
        Assert.That(warnings.Any(value => value.Contains("trainingCategoryId")), Is.True);
        Assert.That(warnings.Any(value => value.Contains("負数")), Is.True);
        Assert.That(warnings.Any(value => value.Contains("initialPlayerLp")), Is.True);
        Assert.That(warnings.Any(value => value.Contains("存在しないtrainingId")), Is.True);
        Assert.That(warnings.Any(value => value.Contains("本文がありません")), Is.True);
    }

    [Test]
    public void ValidateEnemyForTests_DetectsInvalidStatusSkillsAndMissingExplorationEnemy()
    {
        EnemyData enemy = Create<EnemyData>();
        enemy.enemyId = "EnemyA";
        enemy.displayName = "";
        enemy.battleStatus.maxHp = 0;
        enemy.battleStatus.currentHp = 10;
        enemy.rewardMoney = -1;
        enemy.battleSkills.Add(new EnemyBattleSkillData
        {
            skillId = "invalid-id",
            displayName = "",
            cost = -1,
            statusDurationTurns = 0,
            useChancePercent = 101
        });

        GameplayDataValidationReport report = GameplayDataValidator.ValidateEnemyForTests(
            new[] { enemy }, new[] { "ForestSlime" });

        string[] warnings = report.Warnings.Select(value => value.Message).ToArray();
        Assert.That(warnings.Any(value => value.Contains("displayName")), Is.True);
        Assert.That(warnings.Any(value => value.Contains("maxHp")), Is.True);
        Assert.That(warnings.Any(value => value.Contains("rewardMoney")), Is.True);
        Assert.That(warnings.Any(value => value.Contains("敵スキル") || value.Contains("battleSkills")), Is.True);
        Assert.That(warnings.Any(value => value.Contains("探索先に必要")), Is.True);
    }

    [Test]
    public void ValidateShopForTests_DetectsInvalidItemReferencesAndCatalogDuplicates()
    {
        ShopItemData item = Create<ShopItemData>();
        item.itemId = "Potion";
        item.displayName = "";
        item.price = -1;
        item.isBattleConsumable = true;
        item.requiredPurchasedItemIds.Add("Potion");
        item.unlockedOutfitIds.Add("MissingOutfit");

        ShopCatalogData catalog = Create<ShopCatalogData>();
        catalog.items.Add(item);
        catalog.items.Add(item);

        GameplayDataValidationReport report = GameplayDataValidator.ValidateShopForTests(
            new[] { item }, new[] { catalog }, new[] { "Normal" });

        string[] warnings = report.Warnings.Select(value => value.Message).ToArray();
        Assert.That(warnings.Any(value => value.Contains("displayName")), Is.True);
        Assert.That(warnings.Any(value => value.Contains("price")), Is.True);
        Assert.That(warnings.Any(value => value.Contains("回復量")), Is.True);
        Assert.That(warnings.Any(value => value.Contains("自分自身")), Is.True);
        Assert.That(warnings.Any(value => value.Contains("存在しないID")), Is.True);
        Assert.That(warnings.Any(value => value.Contains("カタログ内") && value.Contains("重複")), Is.True);
    }

    [Test]
    public void SeasonalOutfitPrices_ExceedInitialBudgetButKeepAnEntryPurchaseAffordable()
    {
        ShopItemData[] outfits = SeasonalOutfits();

        Assert.That(outfits, Has.Length.EqualTo(4));
        Assert.That(outfits.Sum(item => item.price), Is.EqualTo(1550));
        Assert.That(outfits.Sum(item => item.price), Is.GreaterThan(1000));
        Assert.That(LoadShopItem("SpringOutfitItem").price, Is.LessThanOrEqualTo(1000));
    }

    [Test]
    public void SeasonalOutfitRequirements_CreateExpectedProgression()
    {
        ShopItemData spring = LoadShopItem("SpringOutfitItem");
        ShopItemData summer = LoadShopItem("SummerOutfitItem");
        ShopItemData autumn = LoadShopItem("AutumnOutfitItem");
        ShopItemData winter = LoadShopItem("WinterOutfitItem");
        ShopCatalogData catalog = AssetDatabase.LoadAssetAtPath<ShopCatalogData>(
            "Assets/Resources/ShopItems/DuoShoppingCatalog.asset");

        Assert.That(spring.requiredDay, Is.Zero);
        Assert.That(spring.requiredPurchasedItemIds, Is.Empty);
        Assert.That(summer.requiredDay, Is.EqualTo(7));
        Assert.That(autumn.requiredAffection, Is.EqualTo(200));
        Assert.That(autumn.requiredPurchasedItemIds, Is.EqualTo(new[] { "SpringOutfitItem" }));
        Assert.That(winter.requiredDay, Is.EqualTo(14));
        Assert.That(winter.requiredPurchasedItemIds, Is.EqualTo(new[] { "AutumnOutfitItem" }));
        Assert.That(catalog, Is.Not.Null);
        Assert.That(
            catalog.items.Where(item => item != null).Select(item => item.itemId),
            Does.Not.Contain("ShoppingTestItem_01"));
    }

    private static ShopItemData[] SeasonalOutfits()
    {
        return new[]
        {
            LoadShopItem("SpringOutfitItem"),
            LoadShopItem("SummerOutfitItem"),
            LoadShopItem("AutumnOutfitItem"),
            LoadShopItem("WinterOutfitItem")
        };
    }

    private static ShopItemData LoadShopItem(string itemId)
    {
        ShopItemData item = AssetDatabase.LoadAssetAtPath<ShopItemData>(
            "Assets/Resources/ShopItems/" + itemId + ".asset");
        Assert.That(item, Is.Not.Null, "Shop item asset is required: " + itemId);
        return item;
    }

    private T Create<T>() where T : ScriptableObject
    {
        T value = ScriptableObject.CreateInstance<T>();
        createdObjects.Add(value);
        return value;
    }
}
#endif
