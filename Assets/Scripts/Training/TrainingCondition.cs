using System;
using System.Collections.Generic;

[Serializable]
public enum TrainingConditionRank
{
    Excellent,
    Good,
    Normal,
    Poor,
    Awful
}

/// <summary>
/// その日の訓練へ適用する調子。日付から導出するため、個別のセーブ項目は不要。
/// </summary>
[Serializable]
public class TrainingCondition
{
    public int cycleDay;
    public TrainingConditionRank rank;
    public int playerHpCostModifier;
    public int heroineHpCostModifier;
    public int affectionRewardModifier;
    public int trainingProficiencyRewardModifier;

    public string DisplayName
    {
        get
        {
            switch (rank)
            {
                case TrainingConditionRank.Excellent:
                    return "絶好調";
                case TrainingConditionRank.Good:
                    return "好調";
                case TrainingConditionRank.Normal:
                    return "普通";
                case TrainingConditionRank.Poor:
                    return "不調";
                case TrainingConditionRank.Awful:
                    return "絶不調";
                default:
                    return "不明";
            }
        }
    }

    public bool IsBuff
    {
        get
        {
            return playerHpCostModifier < 0 ||
                heroineHpCostModifier < 0 ||
                affectionRewardModifier > 0 ||
                trainingProficiencyRewardModifier > 0;
        }
    }

    public bool IsDebuff
    {
        get
        {
            return playerHpCostModifier > 0 ||
                heroineHpCostModifier > 0 ||
                affectionRewardModifier < 0 ||
                trainingProficiencyRewardModifier < 0;
        }
    }

    public bool IsNeutral
    {
        get { return !IsBuff && !IsDebuff; }
    }
}

public static class TrainingConditionResolver
{
    public const int CycleLength = 30;

    // 好調側10日、普通10日、不調側10日。極端な調子は連続させない。
    private static readonly TrainingConditionRank[] Cycle =
    {
        TrainingConditionRank.Good,
        TrainingConditionRank.Normal,
        TrainingConditionRank.Poor,
        TrainingConditionRank.Normal,
        TrainingConditionRank.Good,
        TrainingConditionRank.Awful,
        TrainingConditionRank.Excellent,
        TrainingConditionRank.Good,
        TrainingConditionRank.Poor,
        TrainingConditionRank.Normal,
        TrainingConditionRank.Good,
        TrainingConditionRank.Normal,
        TrainingConditionRank.Poor,
        TrainingConditionRank.Awful,
        TrainingConditionRank.Excellent,
        TrainingConditionRank.Normal,
        TrainingConditionRank.Good,
        TrainingConditionRank.Poor,
        TrainingConditionRank.Normal,
        TrainingConditionRank.Good,
        TrainingConditionRank.Normal,
        TrainingConditionRank.Poor,
        TrainingConditionRank.Awful,
        TrainingConditionRank.Excellent,
        TrainingConditionRank.Normal,
        TrainingConditionRank.Poor,
        TrainingConditionRank.Normal,
        TrainingConditionRank.Good,
        TrainingConditionRank.Normal,
        TrainingConditionRank.Poor
    };

    public static TrainingCondition Resolve(int absoluteDay)
    {
        int safeDay = Math.Max(1, absoluteDay);
        int cycleDay = ((safeDay - 1) % CycleLength) + 1;
        TrainingConditionRank rank = Cycle[cycleDay - 1];
        TrainingCondition condition = new TrainingCondition
        {
            cycleDay = cycleDay,
            rank = rank
        };

        switch (rank)
        {
            case TrainingConditionRank.Excellent:
                condition.playerHpCostModifier = -2;
                condition.heroineHpCostModifier = -2;
                condition.affectionRewardModifier = 1;
                condition.trainingProficiencyRewardModifier = 2;
                break;
            case TrainingConditionRank.Good:
                condition.playerHpCostModifier = -1;
                condition.heroineHpCostModifier = -1;
                condition.trainingProficiencyRewardModifier = 1;
                break;
            case TrainingConditionRank.Normal:
                break;
            case TrainingConditionRank.Poor:
                condition.playerHpCostModifier = 1;
                condition.heroineHpCostModifier = 1;
                condition.trainingProficiencyRewardModifier = -1;
                break;
            case TrainingConditionRank.Awful:
                condition.playerHpCostModifier = 2;
                condition.heroineHpCostModifier = 2;
                condition.affectionRewardModifier = -1;
                condition.trainingProficiencyRewardModifier = -2;
                break;
        }

        return condition;
    }
}

public enum TrainingAvailabilityState
{
    Hidden,
    Disabled,
    Available
}

/// <summary>
/// 訓練の表示・実行可否。UIへ依存しないため、予定画面や検証でも共有できる。
/// </summary>
public sealed class TrainingAvailability
{
    public TrainingAvailabilityState state;
    public readonly List<string> reasons = new List<string>();

    public bool IsVisible
    {
        get { return state != TrainingAvailabilityState.Hidden; }
    }

    public bool CanExecute
    {
        get { return state == TrainingAvailabilityState.Available; }
    }

    public string ReasonText
    {
        get { return string.Join(" / ", reasons.ToArray()); }
    }
}

public static class TrainingAvailabilityEvaluator
{
    public static TrainingAvailability Evaluate(
        TrainingData training,
        TrainingConditionRank conditionRank,
        bool permanentlyUnlocked,
        Func<string, bool> isTrainingCompleted)
    {
        TrainingAvailability result = new TrainingAvailability
        {
            state = TrainingAvailabilityState.Available
        };
        if (training == null)
        {
            result.state = TrainingAvailabilityState.Hidden;
            result.reasons.Add("訓練データがありません");
            return result;
        }

        if (!ContainsOrUnrestricted(training.visibleConditionRanks, conditionRank))
        {
            result.state = TrainingAvailabilityState.Hidden;
            result.reasons.Add("本日の調子では出現しません");
            return result;
        }

        bool prerequisitesMet = ArePrerequisitesMet(training, isTrainingCompleted);
        if (!prerequisitesMet && training.hideUntilPrerequisitesMet)
        {
            result.state = TrainingAvailabilityState.Hidden;
            result.reasons.Add("前提訓練が未完了です");
            return result;
        }

        bool completed = IsCompleted(training.trainingId, isTrainingCompleted);
        if (training.occurrenceType == TrainingOccurrenceType.OncePerSave &&
            completed &&
            training.hideAfterCompletion)
        {
            result.state = TrainingAvailabilityState.Hidden;
            result.reasons.Add("完了済みです");
            return result;
        }

        if (!permanentlyUnlocked)
        {
            result.reasons.Add("恒久解放条件を満たしていません");
        }
        if (!prerequisitesMet)
        {
            result.reasons.Add("前提訓練が未完了です");
        }
        if (training.occurrenceType == TrainingOccurrenceType.OncePerSave && completed)
        {
            result.reasons.Add("一回限定訓練は完了済みです");
        }
        if (!ContainsOrUnrestricted(training.executableConditionRanks, conditionRank))
        {
            result.reasons.Add("本日の調子では実行できません");
        }

        if (result.reasons.Count > 0)
        {
            result.state = TrainingAvailabilityState.Disabled;
        }
        return result;
    }

    private static bool ArePrerequisitesMet(
        TrainingData training,
        Func<string, bool> isTrainingCompleted)
    {
        string[] requiredIds = training.requiredCompletedTrainingIds;
        if (requiredIds == null || requiredIds.Length == 0)
        {
            return true;
        }

        bool foundValidId = false;
        for (int i = 0; i < requiredIds.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(requiredIds[i]))
            {
                continue;
            }

            foundValidId = true;
            bool completed = IsCompleted(requiredIds[i], isTrainingCompleted);
            if (training.requireAllCompletedTrainings && !completed)
            {
                return false;
            }
            if (!training.requireAllCompletedTrainings && completed)
            {
                return true;
            }
        }

        return training.requireAllCompletedTrainings || !foundValidId;
    }

    private static bool IsCompleted(
        string trainingId,
        Func<string, bool> isTrainingCompleted)
    {
        return !string.IsNullOrWhiteSpace(trainingId) &&
            isTrainingCompleted != null &&
            isTrainingCompleted(trainingId);
    }

    private static bool ContainsOrUnrestricted(
        TrainingConditionRank[] ranks,
        TrainingConditionRank value)
    {
        if (ranks == null || ranks.Length == 0)
        {
            return true;
        }

        for (int i = 0; i < ranks.Length; i++)
        {
            if (ranks[i] == value)
            {
                return true;
            }
        }
        return false;
    }
}
