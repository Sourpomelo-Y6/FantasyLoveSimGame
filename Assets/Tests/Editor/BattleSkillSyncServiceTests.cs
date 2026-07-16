using FantasyLoveSim.EditorTools;
using NUnit.Framework;
using System.Linq;

namespace FantasyLoveSim.EditorTests
{
    public class BattleSkillSyncServiceTests
    {
        [Test]
        public void Normalize_TrimsIdsSkipsEmptyAndDeduplicatesInOrder()
        {
            var result = BattleSkillSyncService.Normalize(new[]
            {
                Item(" skill_b ", "FutureEffect", 7),
                Item("skill_a", "Heal", 2),
                Item("SKILL_B", "Damage", 9),
                Item(" ", "Damage", 1)
            });

            Assert.That(result.Select(item => item.SkillId), Is.EqualTo(new[] { "skill_b", "skill_a" }));
            Assert.That(result[0].EffectType, Is.EqualTo("FutureEffect"));
            Assert.That(result[0].Cost, Is.EqualTo(7));
        }

        [Test]
        public void Normalize_PreservesEveryBattleField()
        {
            BattleSkillSyncItem source = Item("skill", "Buff", 4);
            source.DisplayName = " Skill Name ";
            source.Target = "Player";
            source.Power = -3;
            source.AffectedStat = "Defense";
            source.StatusDurationTurns = 5;
            source.UseChancePercent = 85;
            source.Priority = -2;
            source.MaxUsesPerBattle = 0;

            BattleSkillSyncItem result = BattleSkillSyncService.Normalize(new[] { source }).Single();

            Assert.That(result.DisplayName, Is.EqualTo("Skill Name"));
            Assert.That(result.Target, Is.EqualTo("Player"));
            Assert.That(result.Power, Is.EqualTo(-3));
            Assert.That(result.AffectedStat, Is.EqualTo("Defense"));
            Assert.That(result.StatusDurationTurns, Is.EqualTo(5));
            Assert.That(result.UseChancePercent, Is.EqualTo(85));
            Assert.That(result.Priority, Is.EqualTo(-2));
            Assert.That(result.MaxUsesPerBattle, Is.Zero);
        }

        private static BattleSkillSyncItem Item(string id, string effect, int cost)
        {
            return new BattleSkillSyncItem { SkillId = id, EffectType = effect, Cost = cost };
        }
    }
}
