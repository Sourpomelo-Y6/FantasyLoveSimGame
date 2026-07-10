using System;
using UnityEngine;

[Serializable]
public class EnemyBattleSkillData
{
    public string skillId = "EnemySkill";
    public string displayName = "敵スキル";
    public SkillEffectType effectType = SkillEffectType.Damage;
    public EnemySkillTarget target = EnemySkillTarget.RandomOpponent;
    public int cost = 0;
    public int power = 0;
    public SkillBattleStat affectedStat = SkillBattleStat.Attack;
    [Min(1)] public int statusDurationTurns = 2;
    [Range(0, 100)] public int useChancePercent = 35;
    public int priority = 0;
    [Tooltip("0 以下なら使用回数に制限なし")]
    public int maxUsesPerBattle = 1;

    public string GetDisplayName()
    {
        return !string.IsNullOrEmpty(displayName) ? displayName : skillId;
    }
}
