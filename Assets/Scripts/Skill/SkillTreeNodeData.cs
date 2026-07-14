using System;
using System.Collections.Generic;
using UnityEngine;

public enum SkillTreeOwner
{
    Player,
    Heroine
}

public enum SkillTreeProgressScope
{
    Total,
    Training,
    TrainingCategory,
    Enemy
}

public enum SkillTreeConditionType
{
    TrainingProficiency,
    TrainingCount,
    PlayerLpConsumedCount,
    OpponentLpConsumedCount,
    MonsterDefeatCount,
    Affection,
    Day
}

public enum SkillTreeNodeState
{
    Locked,
    Available,
    InsufficientPoints,
    Acquired
}

[Serializable]
public class SkillTreeUnlockCondition
{
    public SkillTreeConditionType conditionType;
    public SkillTreeProgressScope scope;
    public string targetId;
    [Min(0)] public int requiredValue;
}

[CreateAssetMenu(menuName = "LoveSim/Skill Tree Node")]
public class SkillTreeNodeData : ScriptableObject
{
    [Header("Basic")]
    public string nodeId = "SkillTreeNode";
    public string displayName = "スキルノード";
    public SkillTreeOwner owner = SkillTreeOwner.Player;
    public SkillData skill;
    public int sortOrder;

    [Header("Heroine Skill")]
    [Tooltip("Heroine ノードで対象とする HeroineProfileData.heroineId。空なら全ヒロイン共通。")]
    public string targetHeroineId;
    [Tooltip("取得時に使用可能にする HeroineBattleSkillData.skillId。")]
    public string grantedHeroineSkillId;

    [Header("Acquisition")]
    [Min(0)] public int skillPointCost = 1;
    public List<SkillTreeNodeData> prerequisiteNodes = new List<SkillTreeNodeData>();
    public List<SkillTreeUnlockCondition> unlockConditions = new List<SkillTreeUnlockCondition>();

    [Header("Tree Layout")]
    public Vector2 treePosition;

    public string GetDisplayName()
    {
        if (!string.IsNullOrEmpty(displayName))
        {
            return displayName;
        }

        return skill != null ? skill.GetDisplayName() : nodeId;
    }
}

public class SkillTreeConditionProgress
{
    public SkillTreeUnlockCondition condition;
    public int currentValue;
    public int requiredValue;
    public bool IsMet => currentValue >= requiredValue;
}

public class SkillTreeNodeEvaluation
{
    public SkillTreeNodeData node;
    public SkillTreeNodeState state;
    public int currentSkillPoints;
    public int requiredSkillPoints;
    public List<string> missingPrerequisiteNodeIds = new List<string>();
    public List<SkillTreeConditionProgress> conditions = new List<SkillTreeConditionProgress>();
}
