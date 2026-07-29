using System;

[Serializable]
public enum TrainingConditionRank
{
    Excellent,
    Good,
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
}

public static class TrainingConditionResolver
{
    public const int CycleLength = 30;

    // 好調15日、不調15日。極端な調子は連続しない配置にする。
    private static readonly TrainingConditionRank[] Cycle =
    {
        TrainingConditionRank.Good,
        TrainingConditionRank.Poor,
        TrainingConditionRank.Good,
        TrainingConditionRank.Awful,
        TrainingConditionRank.Excellent,
        TrainingConditionRank.Poor,
        TrainingConditionRank.Good,
        TrainingConditionRank.Poor,
        TrainingConditionRank.Good,
        TrainingConditionRank.Awful,
        TrainingConditionRank.Excellent,
        TrainingConditionRank.Poor,
        TrainingConditionRank.Good,
        TrainingConditionRank.Poor,
        TrainingConditionRank.Good,
        TrainingConditionRank.Awful,
        TrainingConditionRank.Excellent,
        TrainingConditionRank.Poor,
        TrainingConditionRank.Good,
        TrainingConditionRank.Poor,
        TrainingConditionRank.Good,
        TrainingConditionRank.Awful,
        TrainingConditionRank.Excellent,
        TrainingConditionRank.Poor,
        TrainingConditionRank.Good,
        TrainingConditionRank.Poor,
        TrainingConditionRank.Good,
        TrainingConditionRank.Poor,
        TrainingConditionRank.Good,
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
