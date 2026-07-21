#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using UnityEditor;

public class TrainingBalanceCalculatorTests
{
    [TestCase("LightPractice", 20, TrainingEndReason.HpOrLpDepleted, 1, 1, 0, 30, 21, 1)]
    [TestCase("SparringPractice", 14, TrainingEndReason.HpOrLpDepleted, 1, 1, 2, 74, 16, 2)]
    [TestCase("EnduranceTraining", 18, TrainingEndReason.HpOrLpDepleted, 1, 2, 0, 28, 21, 2)]
    [TestCase("CooperativeDrill", 20, TrainingEndReason.StepLimitReached, 2, 2, 0, 32, 23, 2)]
    public void CurrentTrainingBalance_MatchesRegressionBaseline(
        string trainingId,
        int steps,
        TrainingEndReason endReason,
        int playerLpConsumed,
        int heroineLpConsumed,
        int simultaneousKnockouts,
        int affection,
        int proficiency,
        int skillPoints)
    {
        TrainingData training = LoadTraining(trainingId);
        TrainingBalanceSimulationResult simulation = TrainingBalanceCalculator.Simulate(
            training, Status(100), Status(80));

        Assert.That(simulation.ReachedSafetyLimit, Is.False);
        Assert.That(simulation.Result.elapsedSteps, Is.EqualTo(steps));
        Assert.That(simulation.Result.endReason, Is.EqualTo(endReason));
        Assert.That(simulation.Result.playerLpConsumedCount, Is.EqualTo(playerLpConsumed));
        Assert.That(simulation.Result.opponentLpConsumedCount, Is.EqualTo(heroineLpConsumed));
        Assert.That(simulation.Result.simultaneousKnockoutCount, Is.EqualTo(simultaneousKnockouts));
        Assert.That(simulation.Result.totalAffectionReward, Is.EqualTo(affection));
        Assert.That(simulation.TotalProficiencyReward, Is.EqualTo(proficiency));
        Assert.That(simulation.Result.playerSkillPointReward, Is.EqualTo(skillPoints));
        Assert.That(simulation.Result.heroineSkillPointReward, Is.EqualTo(skillPoints));
    }

    [Test]
    public void HpCostReduction_LeavesMinimumOneAndDoesNotMutateInputStatus()
    {
        TrainingData training = LoadTraining("LightPractice");
        BattleStatusData player = Status(100);
        BattleStatusData heroine = Status(80);
        TrainingStepModifiers modifiers = new TrainingStepModifiers
        {
            playerHpCostReduction = 999,
            heroineHpCostReduction = 999
        };

        TrainingStepResult step = TrainingSessionState.CalculateStepResult(training, modifiers);
        TrainingBalanceCalculator.Simulate(training, player, heroine, modifiers, 1);

        Assert.That(step.playerHpCost, Is.EqualTo(1));
        Assert.That(step.heroineHpCost, Is.EqualTo(1));
        Assert.That(player.currentHp, Is.EqualTo(100));
        Assert.That(heroine.currentHp, Is.EqualTo(80));
    }

    private static TrainingData LoadTraining(string trainingId)
    {
        TrainingData training = AssetDatabase.LoadAssetAtPath<TrainingData>(
            "Assets/Resources/Training/" + trainingId + ".asset");
        Assert.That(training, Is.Not.Null, "Training asset is required: " + trainingId);
        return training;
    }

    private static BattleStatusData Status(int hp)
    {
        return new BattleStatusData
        {
            currentHp = hp,
            maxHp = hp,
            currentMp = 30,
            maxMp = 30,
            attack = 10,
            defense = 5,
            speed = 5
        };
    }
}
#endif
