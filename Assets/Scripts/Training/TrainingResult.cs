using System;
using System.Collections.Generic;

[Serializable]
public class TrainingResult
{
    public string trainingId;
    public string trainingName;
    public int elapsedSteps;
    public int maxSteps;
    public int simultaneousKnockoutCount;
    public int playerLpConsumedCount;
    public int opponentLpConsumedCount;
    public List<TrainingProgressEntry> progressEntries = new List<TrainingProgressEntry>();
    public int affectionReward;
    public int totalStepAffectionReward;
    public int totalStepTrainingProficiencyReward;
    public int totalPlayerHpCostReduction;
    public int totalHeroineHpCostReduction;
    public int totalAffectionRewardModifier;
    public int totalTrainingProficiencyRewardModifier;
    public List<string> effectiveTrainingSkillIds = new List<string>();
    public List<string> effectiveTrainingSkillNames = new List<string>();
    public int trainingProficiencyReward;
    public int totalTrainingProficiency;
    public int playerSkillPointReward;
    public int heroineSkillPointReward;
    public int totalPlayerSkillPoints;
    public int totalHeroineSkillPoints;
    public int simultaneousKnockoutBonus;
    public int totalAffectionReward;
    public bool wasInterrupted;
    public bool isFinished;
    public TrainingEndReason endReason;

    public static TrainingResult Create(TrainingData training, TrainingSessionState state)
    {
        TrainingResult result = new TrainingResult();
        if (training != null)
        {
            result.trainingId = training.trainingId;
        }
        else if (state != null)
        {
            result.trainingId = state.trainingId;
        }

        result.trainingName = training != null ? training.GetDisplayName() : result.trainingId;
        if (training != null)
        {
            result.affectionReward = training.affectionReward;
            result.trainingProficiencyReward = training.trainingProficiencyReward;
            result.playerSkillPointReward = training.playerSkillPointReward;
            result.heroineSkillPointReward = training.heroineSkillPointReward;
            result.simultaneousKnockoutBonus = training.simultaneousKnockoutBonus;
        }

        if (state != null)
        {
            result.elapsedSteps = state.elapsedSteps;
            result.maxSteps = state.maxSteps;
            result.simultaneousKnockoutCount = state.simultaneousKnockoutCount;
            result.totalStepAffectionReward = state.totalStepAffectionReward;
            result.totalStepTrainingProficiencyReward =
                state.totalStepTrainingProficiencyReward;
            result.totalPlayerHpCostReduction = state.totalPlayerHpCostReduction;
            result.totalHeroineHpCostReduction = state.totalHeroineHpCostReduction;
            result.totalAffectionRewardModifier = state.totalAffectionRewardModifier;
            result.totalTrainingProficiencyRewardModifier =
                state.totalTrainingProficiencyRewardModifier;
            if (state.effectiveTrainingSkillIds != null)
            {
                result.effectiveTrainingSkillIds.AddRange(
                    state.effectiveTrainingSkillIds);
            }
            if (state.effectiveTrainingSkillNames != null)
            {
                result.effectiveTrainingSkillNames.AddRange(
                    state.effectiveTrainingSkillNames);
            }
            result.playerLpConsumedCount = state.playerLpConsumedCount;
            result.opponentLpConsumedCount = state.heroineLpConsumedCount;
            if (state.progressEntries != null)
            {
                for (int i = 0; i < state.progressEntries.Count; i++)
                {
                    TrainingProgressEntry entry = state.progressEntries[i];
                    if (entry == null)
                    {
                        continue;
                    }

                    result.progressEntries.Add(new TrainingProgressEntry
                    {
                        trainingId = entry.trainingId,
                        trainingCategoryId = entry.trainingCategoryId,
                        elapsedSteps = entry.elapsedSteps,
                        playerLpConsumedCount = entry.playerLpConsumedCount,
                        opponentLpConsumedCount = entry.opponentLpConsumedCount,
                        trainingProficiencyReward = entry.trainingProficiencyReward
                    });
                }
            }
            result.wasInterrupted = state.wasInterrupted;
            result.isFinished = state.isFinished;
            result.endReason = state.endReason;
        }

        result.totalAffectionReward = result.totalStepAffectionReward;
        if (result.isFinished && !result.wasInterrupted)
        {
            result.totalAffectionReward +=
                result.affectionReward +
                result.simultaneousKnockoutBonus * result.simultaneousKnockoutCount;
        }

        return result;
    }
}
