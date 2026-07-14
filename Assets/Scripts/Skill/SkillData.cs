using System;
using System.Collections.Generic;
using UnityEngine;

public enum TrainingSkillApplicationScope
{
    AllTrainings,
    TrainingCategory,
    Training
}

[CreateAssetMenu(menuName = "LoveSim/Skill Data")]
public class SkillData : ScriptableObject
{
    [Header("Basic")]
    public string skillId = "Skill";
    public string displayName = "スキル";
    public SkillCategory category = SkillCategory.Battle;
    public SkillEffectType effectType = SkillEffectType.Damage;
    public SkillTargetType targetType = SkillTargetType.Enemy;
    public int sortOrder = 0;
    public bool isEnabled = true;

    [Header("Description")]
    [TextArea(2, 5)]
    public string description;

    [Header("Cost and Effect")]
    public int cost = 0;
    public int power = 0;
    public SkillBattleStat affectedStat = SkillBattleStat.Attack;
    [Min(1)] public int statusDurationTurns = 2;

    [Header("Training Effect")]
    [Min(0)] public int trainingPlayerHpCostReduction = 0;
    [Min(0)] public int trainingHeroineHpCostReduction = 0;
    public int trainingAffectionRewardModifier = 0;
    public int trainingProficiencyRewardModifier = 0;
    [Tooltip("訓練スキルを適用する範囲。")]
    public TrainingSkillApplicationScope trainingApplicationScope =
        TrainingSkillApplicationScope.AllTrainings;
    [Tooltip("カテゴリー指定なら TrainingData.trainingCategoryId、訓練指定なら trainingId。")]
    public string trainingApplicationTargetId;

    [Header("Unlock Condition")]
    public int requiredAffection = 0;
    public int requiredDay = 1;
    public string requiredTrainingId;
    public int requiredTrainingProficiency = 0;
    public List<string> requiredSkillIds = new List<string>();

    [Header("Availability")]
    public bool canUseInBattle = true;
    public bool canUseInTraining = false;
    public bool canUseInExploration = false;

    public string GetDisplayName()
    {
        return !string.IsNullOrEmpty(displayName) ? displayName : skillId;
    }

    public List<string> GetRequiredSkillIds()
    {
        if (requiredSkillIds == null)
        {
            return new List<string>();
        }

        return new List<string>(requiredSkillIds);
    }

    public bool AppliesToTraining(TrainingData training)
    {
        if (training == null)
        {
            return false;
        }

        switch (trainingApplicationScope)
        {
            case TrainingSkillApplicationScope.AllTrainings:
                return true;
            case TrainingSkillApplicationScope.TrainingCategory:
                return !string.IsNullOrEmpty(trainingApplicationTargetId) &&
                    string.Equals(
                        trainingApplicationTargetId,
                        training.trainingCategoryId,
                        StringComparison.Ordinal);
            case TrainingSkillApplicationScope.Training:
                return !string.IsNullOrEmpty(trainingApplicationTargetId) &&
                    string.Equals(
                        trainingApplicationTargetId,
                        training.trainingId,
                        StringComparison.Ordinal);
            default:
                return false;
        }
    }
}
