using System;
using System.Collections.Generic;
using System.Linq;

public static class SaveDataNormalizer
{
    public static SaveData Normalize(SaveData data)
    {
        if (data == null)
        {
            return null;
        }

        data.savedAt = data.savedAt ?? string.Empty;
        data.heroineId = data.heroineId ?? string.Empty;
        data.heroineDisplayName = data.heroineDisplayName ?? string.Empty;
        data.thumbnailFileName = data.thumbnailFileName ?? string.Empty;
        data.currentOutfitId = data.currentOutfitId ?? string.Empty;
        data.saveSlotIndex = Math.Max(0, data.saveSlotIndex);
        data.day = Math.Max(1, data.day);
        data.affection = Clamp(data.affection, 0, 9999);
        data.playerMoney = Math.Max(0, data.playerMoney);
        data.playerSkillPoints = Math.Max(0, data.playerSkillPoints);
        data.heroineSkillPoints = Math.Max(0, data.heroineSkillPoints);
        data.currentTimeSlot = NormalizeEnum(data.currentTimeSlot, default(TimeSlot));
        data.currentWeekday = NormalizeEnum(data.currentWeekday, default(Weekday));
        data.currentSeason = NormalizeEnum(data.currentSeason, default(Season));
        data.currentWeather = NormalizeEnum(data.currentWeather, default(Weather));
        data.todaySchedule = NormalizeEnum(data.todaySchedule, ScheduleType.None);
        data.tomorrowSchedule = NormalizeEnum(data.tomorrowSchedule, ScheduleType.None);

        data.playerBattleStatus = data.playerBattleStatus ?? new BattleStatusData();
        data.heroineBattleStatus = data.heroineBattleStatus ?? new BattleStatusData
        {
            currentHp = 80,
            maxHp = 80,
            currentMp = 30,
            maxMp = 30,
            attack = 8,
            defense = 4,
            speed = 6
        };
        data.playerBattleStatus.Clamp();
        data.heroineBattleStatus.Clamp();
        data.playerOutfitPromptAbilities =
            data.playerOutfitPromptAbilities ?? new OutfitPromptAbilitySet();

        data.outfitPreferences = NormalizeOutfitPreferences(data.outfitPreferences);
        data.unlockedSkillIds = NormalizeIds(data.unlockedSkillIds);
        data.equippedPlayerBattleSkillIds = NormalizeIds(data.equippedPlayerBattleSkillIds);
        data.activePlayerTrainingSkillIds = NormalizeIds(data.activePlayerTrainingSkillIds);
        data.unlockedStillIds = NormalizeIds(data.unlockedStillIds);
        data.purchasedItemIds = NormalizeIds(data.purchasedItemIds);
        data.unlockedOutfitIds = NormalizeIds(data.unlockedOutfitIds);
        data.acquiredPlayerSkillTreeNodeIds = NormalizeIds(data.acquiredPlayerSkillTreeNodeIds);
        data.acquiredHeroineSkillTreeNodeIds = NormalizeIds(data.acquiredHeroineSkillTreeNodeIds);
        data.shownConversationIds = NormalizeIds(data.shownConversationIds);
        data.shownGameEventIds = NormalizeIds(data.shownGameEventIds);

        data.itemQuantities = NormalizeItemQuantities(data.itemQuantities);
        data.trainingProficiencies = NormalizeTrainingProficiencies(data.trainingProficiencies);
        data.skillProgressStats = NormalizeSkillProgressStats(data.skillProgressStats);
        data.scheduleEntries = NormalizeScheduleEntries(data.scheduleEntries);
        data.heroineBattleSkillLoadouts = NormalizeHeroineBattleSkillLoadouts(
            data.heroineBattleSkillLoadouts,
            data.heroineId);
        data.heroineTrainingSkillActivations = NormalizeHeroineTrainingSkillActivations(
            data.heroineTrainingSkillActivations,
            data.heroineId);

        return data;
    }

    private static List<string> NormalizeIds(IEnumerable<string> values)
    {
        List<string> result = new List<string>();
        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        if (values == null) return result;
        foreach (string value in values)
        {
            if (!string.IsNullOrWhiteSpace(value) && seen.Add(value))
            {
                result.Add(value);
            }
        }
        return result;
    }

    private static List<OutfitPreference> NormalizeOutfitPreferences(
        IEnumerable<OutfitPreference> values)
    {
        List<OutfitPreference> result = new List<OutfitPreference>();
        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        if (values == null) return result;
        foreach (OutfitPreference value in values)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.outfitId) ||
                !seen.Add(value.outfitId)) continue;
            value.wearCount = Math.Max(0, value.wearCount);
            value.praiseCount = Math.Max(0, value.praiseCount);
            value.dislikeCount = Math.Max(0, value.dislikeCount);
            value.boredCount = Math.Max(0, value.boredCount);
            result.Add(value);
        }
        return result;
    }

    private static List<ItemQuantityEntry> NormalizeItemQuantities(
        IEnumerable<ItemQuantityEntry> values)
    {
        Dictionary<string, int> quantities = new Dictionary<string, int>(StringComparer.Ordinal);
        if (values != null)
        {
            foreach (ItemQuantityEntry value in values)
            {
                if (value == null || string.IsNullOrWhiteSpace(value.itemId)) continue;
                quantities[value.itemId] = Math.Max(0, value.quantity);
            }
        }
        List<ItemQuantityEntry> result = new List<ItemQuantityEntry>();
        foreach (KeyValuePair<string, int> pair in quantities.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            result.Add(new ItemQuantityEntry { itemId = pair.Key, quantity = pair.Value });
        }
        return result;
    }

    private static List<TrainingProficiencyEntry> NormalizeTrainingProficiencies(
        IEnumerable<TrainingProficiencyEntry> values)
    {
        Dictionary<string, int> proficiencies = new Dictionary<string, int>(StringComparer.Ordinal);
        if (values != null)
        {
            foreach (TrainingProficiencyEntry value in values)
            {
                if (value == null || string.IsNullOrWhiteSpace(value.trainingId)) continue;
                proficiencies[value.trainingId] = Math.Max(0, value.proficiency);
            }
        }
        List<TrainingProficiencyEntry> result = new List<TrainingProficiencyEntry>();
        foreach (KeyValuePair<string, int> pair in proficiencies.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            result.Add(new TrainingProficiencyEntry
            {
                trainingId = pair.Key,
                proficiency = pair.Value
            });
        }
        return result;
    }

    private static SkillProgressStats NormalizeSkillProgressStats(SkillProgressStats value)
    {
        SkillProgressStats result = new SkillProgressStats();
        result.CopyFrom(value);
        return result;
    }

    private static List<ScheduleEntry> NormalizeScheduleEntries(IEnumerable<ScheduleEntry> values)
    {
        Dictionary<int, ScheduleEntry> entries = new Dictionary<int, ScheduleEntry>();
        if (values != null)
        {
            foreach (ScheduleEntry value in values)
            {
                if (value == null || value.day < 1) continue;
                value.cancelReason = value.cancelReason ?? string.Empty;
                value.scheduleType = NormalizeEnum(value.scheduleType, ScheduleType.None);
                value.state = NormalizeEnum(value.state, ScheduleEntryState.Planned);
                entries[value.day] = value;
            }
        }
        List<ScheduleEntry> result = new List<ScheduleEntry>(entries.Values);
        result.Sort((left, right) => left.day.CompareTo(right.day));
        return result;
    }

    private static List<HeroineBattleSkillLoadoutEntry> NormalizeHeroineBattleSkillLoadouts(
        IEnumerable<HeroineBattleSkillLoadoutEntry> values,
        string heroineId)
    {
        List<HeroineBattleSkillLoadoutEntry> result = new List<HeroineBattleSkillLoadoutEntry>();
        if (values == null || string.IsNullOrWhiteSpace(heroineId)) return result;
        List<string> skillIds = new List<string>();
        foreach (HeroineBattleSkillLoadoutEntry value in values)
        {
            if (value == null || !string.Equals(value.heroineId, heroineId, StringComparison.Ordinal))
            {
                continue;
            }
            if (value.equippedSkillIds != null) skillIds.AddRange(value.equippedSkillIds);
        }
        if (skillIds.Count > 0)
        {
            result.Add(new HeroineBattleSkillLoadoutEntry
            {
                heroineId = heroineId,
                equippedSkillIds = NormalizeIds(skillIds)
            });
        }
        return result;
    }

    private static List<HeroineTrainingSkillActivationEntry> NormalizeHeroineTrainingSkillActivations(
        IEnumerable<HeroineTrainingSkillActivationEntry> values,
        string heroineId)
    {
        List<HeroineTrainingSkillActivationEntry> result =
            new List<HeroineTrainingSkillActivationEntry>();
        if (values == null || string.IsNullOrWhiteSpace(heroineId)) return result;
        List<string> skillIds = new List<string>();
        foreach (HeroineTrainingSkillActivationEntry value in values)
        {
            if (value == null || !string.Equals(value.heroineId, heroineId, StringComparison.Ordinal))
            {
                continue;
            }
            if (value.activeSkillIds != null) skillIds.AddRange(value.activeSkillIds);
        }
        if (skillIds.Count > 0)
        {
            result.Add(new HeroineTrainingSkillActivationEntry
            {
                heroineId = heroineId,
                activeSkillIds = NormalizeIds(skillIds)
            });
        }
        return result;
    }

    private static int Clamp(int value, int minimum, int maximum)
    {
        return value < minimum ? minimum : value > maximum ? maximum : value;
    }

    private static T NormalizeEnum<T>(T value, T fallback) where T : struct
    {
        return Enum.IsDefined(typeof(T), value) ? value : fallback;
    }
}
