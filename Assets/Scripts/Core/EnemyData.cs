using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "LoveSim/Enemy Data")]
public class EnemyData : ScriptableObject
{
    public string enemyId = "Enemy";
    public string displayName = "敵";
    public BattleStatusData battleStatus = new BattleStatusData();
    [Header("Battle Skills")]
    public List<EnemyBattleSkillData> battleSkills = new List<EnemyBattleSkillData>();
    public int rewardMoney = 0;
    public int affectionChangeOnWin = 0;
    [TextArea(2, 4)] public string victoryMessage = "戦闘に勝利しました。";
    [TextArea(2, 4)] public string defeatMessage = "戦闘に敗北しました。";

    public string GetDisplayName()
    {
        return !string.IsNullOrEmpty(displayName) ? displayName : enemyId;
    }

    public BattleStatusData CreateBattleStatus()
    {
        return battleStatus != null ? battleStatus.Clone() : new BattleStatusData();
    }

    public List<EnemyBattleSkillData> GetBattleSkills()
    {
        return battleSkills != null
            ? new List<EnemyBattleSkillData>(battleSkills)
            : new List<EnemyBattleSkillData>();
    }
}
