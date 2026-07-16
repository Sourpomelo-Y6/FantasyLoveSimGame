using System;
using System.Collections.Generic;

namespace FantasyLoveSim.EditorTools
{
    public sealed class BattleSkillSyncItem
    {
        public string SkillId;
        public string DisplayName;
        public string EffectType;
        public string Target;
        public int Cost;
        public int Power;
        public string AffectedStat;
        public int StatusDurationTurns;
        public int UseChancePercent;
        public int Priority;
        public int MaxUsesPerBattle;
    }

    public static class BattleSkillSyncService
    {
        public static List<BattleSkillSyncItem> Normalize(IEnumerable<BattleSkillSyncItem> source)
        {
            List<BattleSkillSyncItem> result = new List<BattleSkillSyncItem>();
            HashSet<string> ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (source == null) return result;
            foreach (BattleSkillSyncItem item in source)
            {
                string skillId = (item?.SkillId ?? string.Empty).Trim();
                if (skillId.Length == 0 || !ids.Add(skillId)) continue;
                result.Add(new BattleSkillSyncItem
                {
                    SkillId = skillId,
                    DisplayName = (item.DisplayName ?? string.Empty).Trim(),
                    EffectType = (item.EffectType ?? string.Empty).Trim(),
                    Target = (item.Target ?? string.Empty).Trim(),
                    Cost = item.Cost,
                    Power = item.Power,
                    AffectedStat = (item.AffectedStat ?? string.Empty).Trim(),
                    StatusDurationTurns = item.StatusDurationTurns,
                    UseChancePercent = item.UseChancePercent,
                    Priority = item.Priority,
                    MaxUsesPerBattle = item.MaxUsesPerBattle
                });
            }
            return result;
        }
    }
}
