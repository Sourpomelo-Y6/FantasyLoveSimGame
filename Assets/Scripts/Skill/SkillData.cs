using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "LoveSim/Skill Data")]
public class SkillData : ScriptableObject
{
    [Header("Basic")]
    public string skillId = "Skill";
    public string displayName = "スキル";
    public SkillCategory category = SkillCategory.Battle;
    public SkillEffectType effectType = SkillEffectType.Damage;
    public SkillTargetType targetType = SkillTargetType.Enemy;
    public int sortOrder = 0;
    public bool isEnabled = true;

    [Header("Description")]
    [TextArea(2, 5)]
    public string description;

    [Header("Cost and Effect")]
    public int cost = 0;
    public int power = 0;

    [Header("Unlock Condition")]
    public int requiredAffection = 0;
    public int requiredDay = 1;
    public string requiredTrainingId;
    public int requiredTrainingProficiency = 0;
    public List<string> requiredSkillIds = new List<string>();

    [Header("Availability")]
    public bool canUseInBattle = true;
    public bool canUseInTraining = false;
    public bool canUseInExploration = false;

    public string GetDisplayName()
    {
        return !string.IsNullOrEmpty(displayName) ? displayName : skillId;
    }

    public List<string> GetRequiredSkillIds()
    {
        if (requiredSkillIds == null)
        {
            return new List<string>();
        }

        return new List<string>(requiredSkillIds);
    }
}
