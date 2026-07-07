using UnityEngine;

[CreateAssetMenu(menuName = "LoveSim/Training Data")]
public class TrainingData : ScriptableObject
{
    [Header("Basic")]
    public string trainingId = "Training";
    public string displayName = "訓練";

    [TextArea(2, 5)]
    public string description;

    [Header("Step Cost")]
    public int playerHpCostPerStep = 10;
    public int heroineHpCostPerStep = 10;

    [Header("Initial Session")]
    public int initialPlayerLp = 1;
    public int initialHeroineLp = 1;

    [Header("Rewards")]
    public int affectionReward = 0;
    public int trainingProficiencyReward = 1;
    public int simultaneousKnockoutBonus = 1;

    public string GetDisplayName()
    {
        return !string.IsNullOrEmpty(displayName) ? displayName : trainingId;
    }
}
