public sealed class TrainingBalanceSimulationResult
{
    public TrainingSessionState State;
    public TrainingResult Result;
    public bool ReachedSafetyLimit;

    public int TotalProficiencyReward
    {
        get
        {
            if (Result == null) return 0;
            bool completed = Result.isFinished && !Result.wasInterrupted;
            return Result.totalStepTrainingProficiencyReward +
                (completed ? Result.trainingProficiencyReward : 0);
        }
    }
}

/// <summary>
/// 訓練を入力状態へ副作用を与えず、終了または安全上限まで進める。
/// Editorのバランス表示と回帰テストで実ゲームと同じ計算を共有する。
/// </summary>
public static class TrainingBalanceCalculator
{
    public const int DefaultSafetyStepLimit = 1000;

    public static TrainingBalanceSimulationResult Simulate(
        TrainingData training,
        BattleStatusData playerStatus,
        BattleStatusData heroineStatus,
        TrainingStepModifiers modifiers = null,
        int safetyStepLimit = DefaultSafetyStepLimit)
    {
        TrainingBalanceSimulationResult simulation = new TrainingBalanceSimulationResult();
        TrainingSessionState state = TrainingSessionState.Create(
            training,
            playerStatus,
            heroineStatus);
        TrainingStepModifiers appliedModifiers = modifiers ?? new TrainingStepModifiers();
        int safeLimit = safetyStepLimit > 0 ? safetyStepLimit : DefaultSafetyStepLimit;

        while (!state.isFinished && state.elapsedSteps < safeLimit)
        {
            state.AdvanceStep(training, appliedModifiers);
        }

        simulation.State = state;
        simulation.ReachedSafetyLimit = !state.isFinished;
        simulation.Result = TrainingResult.Create(training, state);
        return simulation;
    }
}
