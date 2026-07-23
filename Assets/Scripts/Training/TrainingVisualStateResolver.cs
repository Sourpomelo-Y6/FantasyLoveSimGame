public static class TrainingVisualStateResolver
{
    public static TrainingVisualState Resolve(TrainingStepResult stepResult)
    {
        bool playerConsumed = stepResult != null && stepResult.playerLpConsumed > 0;
        bool heroineConsumed = stepResult != null && stepResult.heroineLpConsumed > 0;
        if (playerConsumed && heroineConsumed)
        {
            return TrainingVisualState.SimultaneousLpConsumed;
        }

        if (playerConsumed)
        {
            return TrainingVisualState.PlayerLpConsumed;
        }

        if (heroineConsumed)
        {
            return TrainingVisualState.HeroineLpConsumed;
        }

        return TrainingVisualState.SelectedAfterFirstStep;
    }
}
