using System;

[Serializable]
public class TrainingResult
{
    public string trainingId;
    public string trainingName;
    public int elapsedSteps;
    public int simultaneousKnockoutCount;
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

        if (state != null)
        {
            result.elapsedSteps = state.elapsedSteps;
            result.simultaneousKnockoutCount = state.simultaneousKnockoutCount;
            result.wasInterrupted = state.wasInterrupted;
            result.isFinished = state.isFinished;
        }

        return result;
    }
}
