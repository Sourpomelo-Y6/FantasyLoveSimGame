#if UNITY_INCLUDE_TESTS
using NUnit.Framework;

public class TrainingConditionTests
{
    [Test]
    public void Resolve_RepeatsAfterThirtyDays()
    {
        for (int day = 1; day <= TrainingConditionResolver.CycleLength; day++)
        {
            TrainingCondition first = TrainingConditionResolver.Resolve(day);
            TrainingCondition next = TrainingConditionResolver.Resolve(
                day + TrainingConditionResolver.CycleLength);

            Assert.That(next.cycleDay, Is.EqualTo(first.cycleDay));
            Assert.That(next.rank, Is.EqualTo(first.rank));
            Assert.That(next.playerHpCostModifier,
                Is.EqualTo(first.playerHpCostModifier));
            Assert.That(next.trainingProficiencyRewardModifier,
                Is.EqualTo(first.trainingProficiencyRewardModifier));
        }
    }

    [Test]
    public void Resolve_ContainsBuffAndDebuffDaysWithoutNeutralDays()
    {
        int buffDays = 0;
        int debuffDays = 0;
        for (int day = 1; day <= TrainingConditionResolver.CycleLength; day++)
        {
            TrainingCondition condition = TrainingConditionResolver.Resolve(day);
            if (condition.IsBuff)
            {
                buffDays++;
            }
            else
            {
                debuffDays++;
            }

            Assert.That(condition.playerHpCostModifier, Is.Not.EqualTo(0));
        }

        Assert.That(buffDays, Is.EqualTo(15));
        Assert.That(debuffDays, Is.EqualTo(15));
    }

    [Test]
    public void CalculateStepResult_ComposesSkillAndConditionSeparately()
    {
        TrainingData training =
            UnityEngine.ScriptableObject.CreateInstance<TrainingData>();
        training.playerHpCostPerStep = 3;
        training.heroineHpCostPerStep = 3;
        training.affectionRewardPerStep = 1;
        training.trainingProficiencyRewardPerStep = 1;
        TrainingStepModifiers modifiers = new TrainingStepModifiers
        {
            playerHpCostReduction = 1,
            heroineHpCostReduction = 1,
            condition = TrainingConditionResolver.Resolve(5)
        };

        TrainingStepResult result =
            TrainingSessionState.CalculateStepResult(training, modifiers);

        Assert.That(result.skillAdjustedPlayerHpCost, Is.EqualTo(2));
        Assert.That(result.playerHpCost, Is.EqualTo(1));
        Assert.That(result.affectionReward, Is.EqualTo(2));
        Assert.That(result.trainingProficiencyReward, Is.EqualTo(3));
    }

    [Test]
    public void CalculateStepResult_KeepsMinimumHpCostAndNonNegativeRewards()
    {
        TrainingData training =
            UnityEngine.ScriptableObject.CreateInstance<TrainingData>();
        training.playerHpCostPerStep = 1;
        training.heroineHpCostPerStep = 1;
        training.affectionRewardPerStep = 0;
        training.trainingProficiencyRewardPerStep = 0;
        TrainingStepModifiers buff = new TrainingStepModifiers
        {
            playerHpCostReduction = 999,
            heroineHpCostReduction = 999,
            condition = TrainingConditionResolver.Resolve(5)
        };
        TrainingStepModifiers debuff = new TrainingStepModifiers
        {
            condition = TrainingConditionResolver.Resolve(4)
        };

        TrainingStepResult buffResult =
            TrainingSessionState.CalculateStepResult(training, buff);
        TrainingStepResult debuffResult =
            TrainingSessionState.CalculateStepResult(training, debuff);

        Assert.That(buffResult.playerHpCost, Is.EqualTo(1));
        Assert.That(buffResult.heroineHpCost, Is.EqualTo(1));
        Assert.That(debuffResult.affectionReward, Is.EqualTo(0));
        Assert.That(debuffResult.trainingProficiencyReward, Is.EqualTo(0));
    }
}
#endif
