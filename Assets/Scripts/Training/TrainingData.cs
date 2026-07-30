using UnityEngine;

public enum TrainingOccurrenceType
{
    Repeatable,
    OncePerSave
}

[CreateAssetMenu(menuName = "LoveSim/Training Data")]
public class TrainingData : ScriptableObject
{
    [Header("Basic")]
    public string trainingId = "Training";
    public string trainingCategoryId = "General";
    public string displayName = "訓練";
    [Tooltip("訓練一覧内の表示順。小さい値を先に表示します。")]
    public int sortOrder;
    [Tooltip("スキルツリーノードを取得しなくても最初から選択できる訓練。")]
    public bool unlockedByDefault = true;

    [TextArea(2, 5)]
    public string description;

    [Header("Availability")]
    [Tooltip("空の場合は、すべての調子で一覧に表示します。")]
    public TrainingConditionRank[] visibleConditionRanks =
        new TrainingConditionRank[0];
    [Tooltip("空の場合は、すべての調子で実行できます。")]
    public TrainingConditionRank[] executableConditionRanks =
        new TrainingConditionRank[0];
    public TrainingOccurrenceType occurrenceType = TrainingOccurrenceType.Repeatable;
    [Tooltip("成功完了している必要がある訓練ID。")]
    public string[] requiredCompletedTrainingIds = new string[0];
    [Tooltip("オンならすべて、オフならいずれか1つの前提完了を要求します。")]
    public bool requireAllCompletedTrainings = true;
    [Tooltip("前提未達の間、一覧から隠します。")]
    public bool hideUntilPrerequisitesMet = true;
    [Tooltip("一回限定の成功完了後、一覧から隠します。")]
    public bool hideAfterCompletion;

    [Header("Step Cost")]
    public int playerHpCostPerStep = 10;
    public int heroineHpCostPerStep = 10;

    [Header("Initial Session")]
    public int initialPlayerLp = 1;
    public int initialHeroineLp = 1;
    [Tooltip("訓練セッションの最大ステップ数。0 以下は無制限。")]
    public int maxSteps = 0;

    [Header("Rewards")]
    public int affectionRewardPerStep = 1;
    public int affectionReward = 0;
    public int trainingProficiencyRewardPerStep = 1;
    public int trainingProficiencyReward = 1;
    public int playerSkillPointReward = 1;
    public int heroineSkillPointReward = 1;
    public int simultaneousKnockoutBonus = 1;

    public string GetDisplayName()
    {
        return !string.IsNullOrEmpty(displayName) ? displayName : trainingId;
    }
}
