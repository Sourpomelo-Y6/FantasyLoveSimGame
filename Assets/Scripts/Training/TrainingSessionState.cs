using System;
using System.Collections.Generic;

[Serializable]
public enum TrainingEndReason
{
    None,
    HpOrLpDepleted,
    StepLimitReached,
    Interrupted
}

[Serializable]
public class TrainingStepModifiers
{
    public int playerHpCostReduction;
    public int heroineHpCostReduction;
    public int affectionRewardModifier;
    public int trainingProficiencyRewardModifier;

    public bool HasAnyEffect
    {
        get
        {
            return playerHpCostReduction != 0 ||
                heroineHpCostReduction != 0 ||
                affectionRewardModifier != 0 ||
                trainingProficiencyRewardModifier != 0;
        }
    }

    public static TrainingStepModifiers Create(
        TrainingData training,
        IEnumerable<SkillData> playerSkills,
        IEnumerable<SkillData> heroineSkills)
    {
        TrainingStepModifiers modifiers = new TrainingStepModifiers();
        HashSet<string> appliedSkillIds =
            new HashSet<string>(StringComparer.Ordinal);
        modifiers.AddSkills(training, playerSkills, appliedSkillIds);
        modifiers.AddSkills(training, heroineSkills, appliedSkillIds);
        return modifiers;
    }

    private void AddSkills(
        TrainingData training,
        IEnumerable<SkillData> skills,
        HashSet<string> appliedSkillIds)
    {
        if (skills == null)
        {
            return;
        }

        foreach (SkillData skill in skills)
        {
            if (skill == null ||
                !skill.isEnabled ||
                skill.category != SkillCategory.Training ||
                !skill.canUseInTraining ||
                !skill.AppliesToTraining(training) ||
                string.IsNullOrEmpty(skill.skillId) ||
                !appliedSkillIds.Add(skill.skillId))
            {
                continue;
            }

            playerHpCostReduction = AddClamped(
                playerHpCostReduction,
                Math.Max(0, skill.trainingPlayerHpCostReduction));
            heroineHpCostReduction = AddClamped(
                heroineHpCostReduction,
                Math.Max(0, skill.trainingHeroineHpCostReduction));
            affectionRewardModifier = AddClamped(
                affectionRewardModifier,
                skill.trainingAffectionRewardModifier);
            trainingProficiencyRewardModifier = AddClamped(
                trainingProficiencyRewardModifier,
                skill.trainingProficiencyRewardModifier);
        }
    }

    private static int AddClamped(int currentValue, int addedValue)
    {
        long total = (long)currentValue + addedValue;
        if (total > int.MaxValue) return int.MaxValue;
        if (total < int.MinValue) return int.MinValue;
        return (int)total;
    }
}

[Serializable]
public class TrainingStepResult
{
    public bool wasApplied;
    public int basePlayerHpCost;
    public int playerHpCost;
    public int baseHeroineHpCost;
    public int heroineHpCost;
    public int baseAffectionReward;
    public int affectionReward;
    public int baseTrainingProficiencyReward;
    public int trainingProficiencyReward;

    public bool HasAppliedModifier
    {
        get
        {
            return basePlayerHpCost != playerHpCost ||
                baseHeroineHpCost != heroineHpCost ||
                baseAffectionReward != affectionReward ||
                baseTrainingProficiencyReward != trainingProficiencyReward;
        }
    }
}

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
    public int maxSteps;
    public int simultaneousKnockoutCount;
    public int totalStepAffectionReward;
    public int totalStepTrainingProficiencyReward;
    public int playerLpConsumedCount;
    public int heroineLpConsumedCount;
    public List<TrainingProgressEntry> progressEntries = new List<TrainingProgressEntry>();
    public bool wasInterrupted;
    public bool isFinished;
    public TrainingEndReason endReason;

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
        state.maxSteps = training != null ? Math.Max(0, training.maxSteps) : 0;
        state.endReason = TrainingEndReason.None;
        state.Clamp();
        return state;
    }

    public TrainingStepResult AdvanceStep(
        TrainingData training,
        TrainingStepModifiers modifiers)
    {
        TrainingStepResult stepResult = CalculateStepResult(training, modifiers);
        if (isFinished)
        {
            return stepResult;
        }

        int previousPlayerLp = playerLp;
        int previousHeroineLp = heroineLp;
        elapsedSteps++;
        playerHp -= stepResult.playerHpCost;
        heroineHp -= stepResult.heroineHpCost;
        ResolveHpAndLp();

        int playerLpConsumed = Math.Max(0, previousPlayerLp - playerLp);
        int heroineLpConsumed = Math.Max(0, previousHeroineLp - heroineLp);
        playerLpConsumedCount += playerLpConsumed;
        heroineLpConsumedCount += heroineLpConsumed;
        RecordProgress(
            training,
            playerLpConsumed,
            heroineLpConsumed,
            stepResult.trainingProficiencyReward);
        totalStepAffectionReward += stepResult.affectionReward;
        totalStepTrainingProficiencyReward += stepResult.trainingProficiencyReward;
        stepResult.wasApplied = true;

        if (!isFinished && maxSteps > 0 && elapsedSteps >= maxSteps)
        {
            isFinished = true;
            endReason = TrainingEndReason.StepLimitReached;
        }

        return stepResult;
    }

    public static TrainingStepResult CalculateStepResult(
        TrainingData training,
        TrainingStepModifiers modifiers)
    {
        TrainingStepResult result = new TrainingStepResult();
        result.basePlayerHpCost = training != null
            ? Math.Max(0, training.playerHpCostPerStep)
            : 0;
        result.baseHeroineHpCost = training != null
            ? Math.Max(0, training.heroineHpCostPerStep)
            : 0;
        result.baseAffectionReward = training != null
            ? Math.Max(0, training.affectionRewardPerStep)
            : 0;
        result.baseTrainingProficiencyReward = training != null
            ? Math.Max(0, training.trainingProficiencyRewardPerStep)
            : 0;

        int playerReduction = modifiers != null
            ? Math.Max(0, modifiers.playerHpCostReduction)
            : 0;
        int heroineReduction = modifiers != null
            ? Math.Max(0, modifiers.heroineHpCostReduction)
            : 0;
        result.playerHpCost = ApplyHpCostReduction(
            result.basePlayerHpCost,
            playerReduction);
        result.heroineHpCost = ApplyHpCostReduction(
            result.baseHeroineHpCost,
            heroineReduction);
        result.affectionReward = ApplyRewardModifier(
            result.baseAffectionReward,
            modifiers != null ? modifiers.affectionRewardModifier : 0);
        result.trainingProficiencyReward = ApplyRewardModifier(
            result.baseTrainingProficiencyReward,
            modifiers != null ? modifiers.trainingProficiencyRewardModifier : 0);
        return result;
    }

    private static int ApplyHpCostReduction(int baseCost, int reduction)
    {
        if (baseCost <= 0)
        {
            return 0;
        }

        return Math.Max(1, baseCost - Math.Min(baseCost, reduction));
    }

    private static int ApplyRewardModifier(int baseReward, int modifier)
    {
        long adjustedReward = (long)baseReward + modifier;
        if (adjustedReward <= 0) return 0;
        return adjustedReward > int.MaxValue ? int.MaxValue : (int)adjustedReward;
    }

    public void Interrupt()
    {
        wasInterrupted = true;
        isFinished = true;
        endReason = TrainingEndReason.Interrupted;
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
            endReason = TrainingEndReason.HpOrLpDepleted;
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

    private void RecordProgress(
        TrainingData training,
        int playerLpConsumed,
        int heroineLpConsumed,
        int trainingProficiencyReward)
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
        entry.trainingProficiencyReward += Math.Max(0, trainingProficiencyReward);
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
