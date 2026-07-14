using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    public int saveVersion = 16;
    public int saveSlotIndex;
    public string savedAt;
    public string heroineId;
    public string heroineDisplayName;
    public string thumbnailFileName;

    public int day;
    public TimeSlot currentTimeSlot;
    public Weekday currentWeekday;
    public Season currentSeason;
    public Weather currentWeather;

    public int affection;
    public BattleStatusData playerBattleStatus = new BattleStatusData();
    public int playerMoney = 1000;
    public BattleStatusData heroineBattleStatus = new BattleStatusData
    {
        currentHp = 80,
        maxHp = 80,
        attack = 8,
        defense = 4,
        speed = 6
    };

    public string currentOutfitId;

    public List<OutfitPreference> outfitPreferences = new List<OutfitPreference>();
    public OutfitPromptAbilitySet playerOutfitPromptAbilities = new OutfitPromptAbilitySet();
    public OutfitPromptAbilitySet heroineOutfitPromptAbilities = new OutfitPromptAbilitySet();
    public List<string> unlockedStatusAbilityIds = new List<string>();
    public List<string> unlockedSkillIds = new List<string>();
    public List<string> equippedPlayerBattleSkillIds = new List<string>();
    public List<HeroineBattleSkillLoadoutEntry> heroineBattleSkillLoadouts =
        new List<HeroineBattleSkillLoadoutEntry>();
    public List<string> activePlayerTrainingSkillIds = new List<string>();
    public List<HeroineTrainingSkillActivationEntry> heroineTrainingSkillActivations =
        new List<HeroineTrainingSkillActivationEntry>();
    public List<string> unlockedStillIds = new List<string>();
    public List<string> purchasedItemIds = new List<string>();
    public List<ItemQuantityEntry> itemQuantities = new List<ItemQuantityEntry>();
    public List<string> unlockedOutfitIds = new List<string>();
    public List<TrainingProficiencyEntry> trainingProficiencies = new List<TrainingProficiencyEntry>();
    public SkillProgressStats skillProgressStats = new SkillProgressStats();
    public int playerSkillPoints;
    public int heroineSkillPoints;
    public List<string> acquiredPlayerSkillTreeNodeIds = new List<string>();
    public List<string> acquiredHeroineSkillTreeNodeIds = new List<string>();

    public List<string> shownConversationIds = new List<string>();
    public List<string> shownGameEventIds = new List<string>();

    public ScheduleType todaySchedule;
    public ScheduleType tomorrowSchedule;
    public bool todayScheduleEventExecuted;
}

[Serializable]
public class ItemQuantityEntry
{
    public string itemId;
    public int quantity;
}

[Serializable]
public class TrainingProficiencyEntry
{
    public string trainingId;
    public int proficiency;
}

[Serializable]
public class HeroineBattleSkillLoadoutEntry
{
    public string heroineId;
    public List<string> equippedSkillIds = new List<string>();
}

[Serializable]
public class HeroineTrainingSkillActivationEntry
{
    public string heroineId;
    public List<string> activeSkillIds = new List<string>();
}

[Serializable]
public class SkillProgressStats
{
    public int totalTrainingCount;
    public int playerLpConsumedCount;
    public int opponentLpConsumedCount;
    public int totalMonsterDefeatCount;
    public List<TrainingProgressStatEntry> trainingStats = new List<TrainingProgressStatEntry>();
    public List<TrainingCategoryProgressStatEntry> trainingCategoryStats =
        new List<TrainingCategoryProgressStatEntry>();
    public List<EnemyDefeatStatEntry> enemyDefeatStats = new List<EnemyDefeatStatEntry>();

    public SkillProgressStats Clone()
    {
        SkillProgressStats copy = new SkillProgressStats
        {
            totalTrainingCount = Math.Max(0, totalTrainingCount),
            playerLpConsumedCount = Math.Max(0, playerLpConsumedCount),
            opponentLpConsumedCount = Math.Max(0, opponentLpConsumedCount),
            totalMonsterDefeatCount = Math.Max(0, totalMonsterDefeatCount)
        };

        CopyTrainingStats(trainingStats, copy.trainingStats);
        CopyCategoryStats(trainingCategoryStats, copy.trainingCategoryStats);
        CopyEnemyStats(enemyDefeatStats, copy.enemyDefeatStats);
        return copy;
    }

    public void CopyFrom(SkillProgressStats source)
    {
        totalTrainingCount = source != null ? Math.Max(0, source.totalTrainingCount) : 0;
        playerLpConsumedCount = source != null ? Math.Max(0, source.playerLpConsumedCount) : 0;
        opponentLpConsumedCount = source != null ? Math.Max(0, source.opponentLpConsumedCount) : 0;
        totalMonsterDefeatCount = source != null ? Math.Max(0, source.totalMonsterDefeatCount) : 0;
        trainingStats.Clear();
        trainingCategoryStats.Clear();
        enemyDefeatStats.Clear();
        if (source == null)
        {
            return;
        }

        CopyTrainingStats(source.trainingStats, trainingStats);
        CopyCategoryStats(source.trainingCategoryStats, trainingCategoryStats);
        CopyEnemyStats(source.enemyDefeatStats, enemyDefeatStats);
    }

    private static void CopyTrainingStats(
        List<TrainingProgressStatEntry> source,
        List<TrainingProgressStatEntry> destination)
    {
        if (source == null) return;
        for (int i = 0; i < source.Count; i++)
        {
            TrainingProgressStatEntry entry = source[i];
            if (entry == null || string.IsNullOrEmpty(entry.trainingId)) continue;
            destination.Add(new TrainingProgressStatEntry
            {
                trainingId = entry.trainingId,
                trainingCount = Math.Max(0, entry.trainingCount),
                playerLpConsumedCount = Math.Max(0, entry.playerLpConsumedCount),
                opponentLpConsumedCount = Math.Max(0, entry.opponentLpConsumedCount)
            });
        }
    }

    private static void CopyCategoryStats(
        List<TrainingCategoryProgressStatEntry> source,
        List<TrainingCategoryProgressStatEntry> destination)
    {
        if (source == null) return;
        for (int i = 0; i < source.Count; i++)
        {
            TrainingCategoryProgressStatEntry entry = source[i];
            if (entry == null || string.IsNullOrEmpty(entry.trainingCategoryId)) continue;
            destination.Add(new TrainingCategoryProgressStatEntry
            {
                trainingCategoryId = entry.trainingCategoryId,
                trainingCount = Math.Max(0, entry.trainingCount),
                playerLpConsumedCount = Math.Max(0, entry.playerLpConsumedCount),
                opponentLpConsumedCount = Math.Max(0, entry.opponentLpConsumedCount)
            });
        }
    }

    private static void CopyEnemyStats(
        List<EnemyDefeatStatEntry> source,
        List<EnemyDefeatStatEntry> destination)
    {
        if (source == null) return;
        for (int i = 0; i < source.Count; i++)
        {
            EnemyDefeatStatEntry entry = source[i];
            if (entry == null || string.IsNullOrEmpty(entry.enemyId)) continue;
            destination.Add(new EnemyDefeatStatEntry
            {
                enemyId = entry.enemyId,
                defeatCount = Math.Max(0, entry.defeatCount)
            });
        }
    }
}

[Serializable]
public class TrainingProgressStatEntry
{
    public string trainingId;
    public int trainingCount;
    public int playerLpConsumedCount;
    public int opponentLpConsumedCount;
}

[Serializable]
public class TrainingCategoryProgressStatEntry
{
    public string trainingCategoryId;
    public int trainingCount;
    public int playerLpConsumedCount;
    public int opponentLpConsumedCount;
}

[Serializable]
public class EnemyDefeatStatEntry
{
    public string enemyId;
    public int defeatCount;
}
