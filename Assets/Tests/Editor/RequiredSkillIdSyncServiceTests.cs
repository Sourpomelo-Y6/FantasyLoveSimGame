using FantasyLoveSim.EditorTools;
using NUnit.Framework;
using System.Collections.Generic;

namespace FantasyLoveSim.EditorTests
{
    public class RequiredSkillIdSyncServiceTests
    {
        [Test]
        public void Normalize_TrimsDeduplicatesAndPreservesOrder()
        {
            List<string> result = RequiredSkillIdSyncService.Normalize(
                new[] { " skill_b ", "skill_a", "SKILL_B", "", null, "unknown_skill" });

            Assert.That(result, Is.EqualTo(new[] { "skill_b", "skill_a", "unknown_skill" }));
        }

        [Test]
        public void ApplyIfSpecified_DoesNotChangeTargetForMissingField()
        {
            List<string> target = new List<string> { "existing_skill" };

            bool applied = RequiredSkillIdSyncService.ApplyIfSpecified(target, null);

            Assert.That(applied, Is.False);
            Assert.That(target, Is.EqualTo(new[] { "existing_skill" }));
        }

        [Test]
        public void ApplyIfSpecified_ClearsTargetForExplicitEmptyArray()
        {
            List<string> target = new List<string> { "existing_skill" };

            bool applied = RequiredSkillIdSyncService.ApplyIfSpecified(target, new string[0]);

            Assert.That(applied, Is.True);
            Assert.That(target, Is.Empty);
        }
    }
}
