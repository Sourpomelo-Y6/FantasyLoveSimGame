using System;
using System.Collections.Generic;

[Serializable]
public class TrainingSessionState
{
    public string trainingId;
    public int playerHp;
    public int playerMaxHp;
    public int heroineHp;
    public int heroineMaxHp;
    public int playerLp;
    public int heroineLp;
    public int elapsedSteps;
    public int simultaneousKnockoutCount;
    public int totalStepAffectionReward;
    public int totalStepTrainingProficiencyReward;
    public int playerLpConsumedCount;
    public int heroineLpConsumedCount;
    public List<TrainingProgressEntry> progressEntries = new List<TrainingProgressEntry>();
    public bool wasInterrupted;
    public bool isFinished;

    public static TrainingSessionState Create(
        TrainingData training,
        BattleStatusData playerStatus,
        BattleStatusData heroineStatus)
    {
        TrainingSessionState state = new TrainingSessionState();
        state.trainingId = training != null ? training.trainingId : string.Empty;
        state.playerMaxHp = playerStatus != null ? playerStatus.maxHp : 1;
        state.heroineMaxHp = heroineStatus != null ? heroineStatus.maxHp : 1;
        state.playerHp = playerStatus != null ? playerStatus.currentHp : state.playerMaxHp;
        state.heroineHp = heroineStatus != null ? heroineStatus.currentHp : state.heroineMaxHp;
        state.playerLp = training != null ? training.initialPlayerLp : 0;
        state.heroineLp = training != null ? training.initialHeroineLp : 0;
        state.Clamp();
        return state;
    }

    public void AdvanceStep(TrainingData training)
    {
        if (isFinished)
        {
            return;
        }

        int previousPlayerLp = playerLp;
        int previousHeroineLp = heroineLp;
        elapsedSteps++;
        playerHp -= training != null ? training.playerHpCostPerStep : 0;
        heroineHp -= training != null ? training.heroineHpCostPerStep : 0;
        ResolveHpAndLp();

        int playerLpConsumed = Math.Max(0, previousPlayerLp - playerLp);
        int heroineLpConsumed = Math.Max(0, previousHeroineLp - heroineLp);
        playerLpConsumedCount += playerLpConsumed;
        heroineLpConsumedCount += heroineLpConsumed;
        RecordProgress(training, playerLpConsumed, heroineLpConsumed);
        if (training != null)
        {
            totalStepAffectionReward += Math.Max(0, training.affectionRewardPerStep);
            totalStepTrainingProficiencyReward +=
                Math.Max(0, training.trainingProficiencyRewardPerStep);
        }
    }

    public void Interrupt()
    {
        wasInterrupted = true;
        isFinished = true;
    }

    private void ResolveHpAndLp()
    {
        bool playerDown = playerHp <= 0;
        bool heroineDown = heroineHp <= 0;

        if (playerDown && heroineDown)
        {
            simultaneousKnockoutCount++;
        }

        if (playerDown)
        {
            RecoverWithLp(ref playerHp, playerMaxHp, ref playerLp);
        }

        if (heroineDown)
        {
            RecoverWithLp(ref heroineHp, heroineMaxHp, ref heroineLp);
        }

        if ((playerHp <= 0 && playerLp <= 0) || (heroineHp <= 0 && heroineLp <= 0))
        {
            isFinished = true;
        }

        Clamp();
    }

    private static void RecoverWithLp(ref int hp, int maxHp, ref int lp)
    {
        if (hp > 0)
        {
            return;
        }

        if (lp > 0)
        {
            lp--;
            hp = maxHp;
        }
    }

    private void RecordProgress(TrainingData training, int playerLpConsumed, int heroineLpConsumed)
    {
        string currentTrainingId = training != null ? training.trainingId : trainingId;
        string categoryId = training != null ? training.trainingCategoryId : string.Empty;
        TrainingProgressEntry entry = null;
        for (int i = 0; i < progressEntries.Count; i++)
        {
            TrainingProgressEntry candidate = progressEntries[i];
            if (candidate != null &&
                string.Equals(candidate.trainingId, currentTrainingId, StringComparison.Ordinal))
            {
                entry = candidate;
                break;
            }
        }

        if (entry == null)
        {
            entry = new TrainingProgressEntry
            {
                trainingId = currentTrainingId,
                trainingCategoryId = categoryId
            };
            progressEntries.Add(entry);
        }

        entry.elapsedSteps++;
        entry.playerLpConsumedCount += playerLpConsumed;
        entry.opponentLpConsumedCount += heroineLpConsumed;
        entry.trainingProficiencyReward += training != null
            ? Math.Max(0, training.trainingProficiencyRewardPerStep)
            : 0;
    }

    private void Clamp()
    {
        if (playerMaxHp < 1)
        {
            playerMaxHp = 1;
        }

        if (heroineMaxHp < 1)
        {
            heroineMaxHp = 1;
        }

        if (playerHp > playerMaxHp)
        {
            playerHp = playerMaxHp;
        }

        if (heroineHp > heroineMaxHp)
        {
            heroineHp = heroineMaxHp;
        }

        if (playerLp < 0)
        {
            playerLp = 0;
        }

        if (heroineLp < 0)
        {
            heroineLp = 0;
        }
    }
}

[Serializable]
public class TrainingProgressEntry
{
    public string trainingId;
    public string trainingCategoryId;
    public int elapsedSteps;
    public int playerLpConsumedCount;
    public int opponentLpConsumedCount;
    public int trainingProficiencyReward;
}
