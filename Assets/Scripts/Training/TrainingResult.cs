using System;
using System.Collections.Generic;

[Serializable]
public class TrainingResult
{
    public string trainingId;
    public string trainingName;
    public int elapsedSteps;
    public int simultaneousKnockoutCount;
    public int playerLpConsumedCount;
    public int opponentLpConsumedCount;
    public List<TrainingProgressEntry> progressEntries = new List<TrainingProgressEntry>();
    public int affectionReward;
    public int totalStepAffectionReward;
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
            result.simultaneousKnockoutCount = state.simultaneousKnockoutCount;
            result.totalStepAffectionReward = state.totalStepAffectionReward;
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
                        opponentLpConsumedCount = entry.opponentLpConsumedCount
                    });
                }
            }
            result.wasInterrupted = state.wasInterrupted;
            result.isFinished = state.isFinished;
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
