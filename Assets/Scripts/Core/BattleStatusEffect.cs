using System;

[Serializable]
public class BattleStatusEffect
{
    public string effectId;
    public string displayName;
    public SkillBattleStat affectedStat;
    public int appliedValue;
    public int remainingTargetTurns;
    public bool skipNextTargetTick;
}
