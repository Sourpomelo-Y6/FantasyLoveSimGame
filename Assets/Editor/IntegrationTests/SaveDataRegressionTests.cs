#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public class SaveDataRegressionTests
{
    [Test]
    public void JsonRoundTrip_PreservesRecentGameplayState()
    {
        SaveData source = CreateValidSaveData();
        source.affection = 1234;
        source.playerBattleStatus.currentHp = 77;
        source.playerBattleStatus.currentMp = 12;
        source.heroineBattleStatus.currentHp = 55;
        source.heroineBattleStatus.currentMp = 8;
        source.playerSkillPoints = 4;
        source.heroineSkillPoints = 7;
        source.shownConversationIds.Add("Reaction_Tea_Consideration_01");
        source.shownGameEventIds.Add("Manual_Consideration_01");
        source.unlockedStillIds.Add("GameStartIntro_01");
        source.purchasedItemIds.Add("SpringOutfitItem");
        source.acquiredPlayerSkillTreeNodeIds.Add("Player_Consideration");
        source.scheduleEntries.Add(new ScheduleEntry
        {
            day = 7,
            scheduleType = ScheduleType.DuoForest,
            state = ScheduleEntryState.Planned
        });
        source.skillProgressStats.totalTrainingCount = 9;
        source.skillProgressStats.totalMonsterDefeatCount = 3;
        source.skillProgressStats.enemyDefeatStats.Add(new EnemyDefeatStatEntry
        {
            enemyId = "Slime",
            defeatCount = 3
        });

        SaveData restored = SaveDataNormalizer.Normalize(
            JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(source)));

        Assert.That(restored.heroineId, Is.EqualTo("TestHeroine"));
        Assert.That(restored.affection, Is.EqualTo(1234));
        Assert.That(restored.playerBattleStatus.currentHp, Is.EqualTo(77));
        Assert.That(restored.playerBattleStatus.currentMp, Is.EqualTo(12));
        Assert.That(restored.heroineBattleStatus.currentHp, Is.EqualTo(55));
        Assert.That(restored.heroineBattleStatus.currentMp, Is.EqualTo(8));
        Assert.That(restored.playerSkillPoints, Is.EqualTo(4));
        Assert.That(restored.heroineSkillPoints, Is.EqualTo(7));
        Assert.That(restored.shownConversationIds, Contains.Item("Reaction_Tea_Consideration_01"));
        Assert.That(restored.shownGameEventIds, Contains.Item("Manual_Consideration_01"));
        Assert.That(restored.unlockedStillIds, Contains.Item("GameStartIntro_01"));
        Assert.That(restored.purchasedItemIds, Contains.Item("SpringOutfitItem"));
        Assert.That(restored.acquiredPlayerSkillTreeNodeIds, Contains.Item("Player_Consideration"));
        Assert.That(restored.scheduleEntries.Single().day, Is.EqualTo(7));
        Assert.That(restored.skillProgressStats.totalTrainingCount, Is.EqualTo(9));
        Assert.That(restored.skillProgressStats.enemyDefeatStats.Single().defeatCount, Is.EqualTo(3));
    }

    [Test]
    public void Normalize_RepairsNullCollectionsAndInvalidNumbers()
    {
        SaveData data = CreateValidSaveData();
        data.day = 0;
        data.affection = 12000;
        data.playerMoney = -1;
        data.playerSkillPoints = -2;
        data.heroineSkillPoints = -3;
        data.playerBattleStatus = null;
        data.heroineBattleStatus = null;
        data.playerOutfitPromptAbilities = null;
        data.unlockedSkillIds = null;
        data.itemQuantities = null;
        data.shownConversationIds = null;
        data.shownGameEventIds = null;
        data.scheduleEntries = null;
        data.skillProgressStats = null;

        SaveDataNormalizer.Normalize(data);

        Assert.That(data.day, Is.EqualTo(1));
        Assert.That(data.affection, Is.EqualTo(9999));
        Assert.That(data.playerMoney, Is.Zero);
        Assert.That(data.playerSkillPoints, Is.Zero);
        Assert.That(data.heroineSkillPoints, Is.Zero);
        Assert.That(data.playerBattleStatus, Is.Not.Null);
        Assert.That(data.heroineBattleStatus, Is.Not.Null);
        Assert.That(data.playerOutfitPromptAbilities, Is.Not.Null);
        Assert.That(data.unlockedSkillIds, Is.Empty);
        Assert.That(data.itemQuantities, Is.Empty);
        Assert.That(data.shownConversationIds, Is.Empty);
        Assert.That(data.shownGameEventIds, Is.Empty);
        Assert.That(data.scheduleEntries, Is.Empty);
        Assert.That(data.skillProgressStats, Is.Not.Null);
    }

    [Test]
    public void JsonRoundTrip_PreservesMultipleItemQuantities()
    {
        SaveData source = CreateValidSaveData();
        source.itemQuantities.Add(new ItemQuantityEntry
        {
            itemId = "HealPotion",
            quantity = 3
        });
        source.itemQuantities.Add(new ItemQuantityEntry
        {
            itemId = "ManaPotion",
            quantity = 7
        });

        SaveData restored = SaveDataNormalizer.Normalize(
            JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(source)));

        Assert.That(GetItemQuantity(restored, "HealPotion"), Is.EqualTo(3));
        Assert.That(GetItemQuantity(restored, "ManaPotion"), Is.EqualTo(7));
    }

    [Test]
    public void Normalize_ItemQuantitiesRepairsInvalidAndDuplicateEntries()
    {
        SaveData data = CreateValidSaveData();
        data.itemQuantities = new List<ItemQuantityEntry>
        {
            null,
            new ItemQuantityEntry { itemId = "", quantity = 9 },
            new ItemQuantityEntry { itemId = "ManaPotion", quantity = -2 },
            new ItemQuantityEntry { itemId = "HealPotion", quantity = 2 },
            new ItemQuantityEntry { itemId = "HealPotion", quantity = 5 },
            new ItemQuantityEntry { itemId = "ZeroQuantityItem", quantity = 0 }
        };

        SaveDataNormalizer.Normalize(data);

        Assert.That(
            data.itemQuantities.Select(entry => entry.itemId),
            Is.EqualTo(new[] { "HealPotion", "ManaPotion", "ZeroQuantityItem" }));
        Assert.That(GetItemQuantity(data, "HealPotion"), Is.EqualTo(5));
        Assert.That(GetItemQuantity(data, "ManaPotion"), Is.Zero);
        Assert.That(GetItemQuantity(data, "ZeroQuantityItem"), Is.Zero);
    }

    [Test]
    public void JsonRoundTrip_PreservesQuantityAfterBattleItemUse()
    {
        SaveData source = CreateValidSaveData();
        ItemQuantityEntry healPotion = new ItemQuantityEntry
        {
            itemId = "HealPotion",
            quantity = 2
        };
        source.itemQuantities.Add(healPotion);

        healPotion.quantity--;
        SaveData restored = SaveDataNormalizer.Normalize(
            JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(source)));

        Assert.That(GetItemQuantity(restored, "HealPotion"), Is.EqualTo(1));
    }

    [Test]
    public void Normalize_RemovesDuplicateIdsAndOtherHeroineSettings()
    {
        SaveData data = CreateValidSaveData();
        data.unlockedSkillIds.AddRange(new[] { "SkillA", "SkillA", "" });
        data.heroineBattleSkillLoadouts.Add(new HeroineBattleSkillLoadoutEntry
        {
            heroineId = "TestHeroine",
            equippedSkillIds = new List<string> { "SkillA", "SkillA" }
        });
        data.heroineBattleSkillLoadouts.Add(new HeroineBattleSkillLoadoutEntry
        {
            heroineId = "DefaultHeroine",
            equippedSkillIds = new List<string> { "OtherSkill" }
        });
        data.heroineTrainingSkillActivations.Add(new HeroineTrainingSkillActivationEntry
        {
            heroineId = "DefaultHeroine",
            activeSkillIds = new List<string> { "OtherTrainingSkill" }
        });

        SaveDataNormalizer.Normalize(data);

        Assert.That(data.unlockedSkillIds, Is.EqualTo(new[] { "SkillA" }));
        Assert.That(data.heroineBattleSkillLoadouts, Has.Count.EqualTo(1));
        Assert.That(data.heroineBattleSkillLoadouts[0].heroineId, Is.EqualTo("TestHeroine"));
        Assert.That(data.heroineBattleSkillLoadouts[0].equippedSkillIds, Is.EqualTo(new[] { "SkillA" }));
        Assert.That(data.heroineTrainingSkillActivations, Is.Empty);
    }

    [Test]
    public void Validator_AcceptsNormalizedSaveData()
    {
        SaveData data = SaveDataNormalizer.Normalize(CreateValidSaveData());

        SaveDataValidationReport report = SaveDataValidator.ValidateForTests(data);

        Assert.That(report.IsValid, Is.True, report.CreateSummary());
    }

    [Test]
    public void Validator_DetectsNullDuplicateAndMixedHeroineData()
    {
        SaveData data = CreateValidSaveData();
        data.unlockedSkillIds = new List<string> { "SkillA", "SkillA", "" };
        data.scheduleEntries = new List<ScheduleEntry>
        {
            new ScheduleEntry { day = 2 },
            new ScheduleEntry { day = 2 }
        };
        data.heroineBattleSkillLoadouts.Add(new HeroineBattleSkillLoadoutEntry
        {
            heroineId = "DefaultHeroine"
        });

        SaveDataValidationReport report = SaveDataValidator.ValidateForTests(data);
        string[] warnings = report.Warnings.ToArray();

        Assert.That(warnings.Any(message => message.Contains("重複ID")), Is.True);
        Assert.That(warnings.Any(message => message.Contains("空のID")), Is.True);
        Assert.That(warnings.Any(message => message.Contains("同じ日付")), Is.True);
        Assert.That(warnings.Any(message => message.Contains("選択中ヒロイン以外")), Is.True);
    }

    private static SaveData CreateValidSaveData()
    {
        return new SaveData
        {
            saveVersion = SaveData.CurrentVersion,
            heroineId = "TestHeroine",
            heroineDisplayName = "テストヒロイン",
            day = 1,
            affection = 0,
            playerMoney = 1000
        };
    }

    private static int GetItemQuantity(SaveData data, string itemId)
    {
        ItemQuantityEntry entry = data.itemQuantities.SingleOrDefault(
            value => value != null && value.itemId == itemId);
        return entry != null ? entry.quantity : 0;
    }
}
#endif
