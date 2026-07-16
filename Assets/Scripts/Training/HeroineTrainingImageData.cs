using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public enum TrainingVisualState
{
    SelectedBeforeFirstStep,
    SelectedAfterFirstStep,
    PlayerLpConsumed,
    HeroineLpConsumed,
    SimultaneousLpConsumed
}

[Serializable]
public class HeroineTrainingImageEntry
{
    public string trainingId;
    public Sprite selectedBeforeFirstStepSprite;
    public Sprite selectedAfterFirstStepSprite;
    public Sprite playerLpConsumedSprite;
    public Sprite heroineLpConsumedSprite;
    public Sprite simultaneousLpConsumedSprite;

    public Sprite GetSprite(TrainingVisualState state)
    {
        switch (state)
        {
            case TrainingVisualState.SelectedBeforeFirstStep:
                return selectedBeforeFirstStepSprite;
            case TrainingVisualState.SelectedAfterFirstStep:
                return selectedAfterFirstStepSprite;
            case TrainingVisualState.PlayerLpConsumed:
                return playerLpConsumedSprite;
            case TrainingVisualState.HeroineLpConsumed:
                return heroineLpConsumedSprite;
            case TrainingVisualState.SimultaneousLpConsumed:
                return simultaneousLpConsumedSprite;
            default:
                return null;
        }
    }
}

[CreateAssetMenu(menuName = "LoveSim/Heroine Training Image Data")]
public class HeroineTrainingImageData : ScriptableObject
{
    public string heroineId;

    [Header("Default Images")]
    public Sprite defaultBeforeFirstStepSprite;
    public Sprite defaultAfterFirstStepSprite;
    public Sprite defaultPlayerLpConsumedSprite;
    public Sprite defaultHeroineLpConsumedSprite;
    public Sprite defaultSimultaneousLpConsumedSprite;

    [Header("Training-specific Images")]
    public List<HeroineTrainingImageEntry> entries = new List<HeroineTrainingImageEntry>();

    public Sprite ResolveSprite(string trainingId, TrainingVisualState state)
    {
        HeroineTrainingImageEntry entry = FindEntry(trainingId);
        Sprite trainingSprite = entry != null ? entry.GetSprite(state) : null;
        return trainingSprite != null ? trainingSprite : GetDefaultSprite(state);
    }

    private HeroineTrainingImageEntry FindEntry(string trainingId)
    {
        if (string.IsNullOrEmpty(trainingId) || entries == null)
        {
            return null;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            HeroineTrainingImageEntry entry = entries[i];
            if (entry != null &&
                string.Equals(entry.trainingId, trainingId, StringComparison.Ordinal))
            {
                return entry;
            }
        }

        return null;
    }

    private Sprite GetDefaultSprite(TrainingVisualState state)
    {
        switch (state)
        {
            case TrainingVisualState.SelectedBeforeFirstStep:
                return defaultBeforeFirstStepSprite;
            case TrainingVisualState.SelectedAfterFirstStep:
                return defaultAfterFirstStepSprite;
            case TrainingVisualState.PlayerLpConsumed:
                return defaultPlayerLpConsumedSprite;
            case TrainingVisualState.HeroineLpConsumed:
                return defaultHeroineLpConsumedSprite;
            case TrainingVisualState.SimultaneousLpConsumed:
                return defaultSimultaneousLpConsumedSprite;
            default:
                return null;
        }
    }
}
