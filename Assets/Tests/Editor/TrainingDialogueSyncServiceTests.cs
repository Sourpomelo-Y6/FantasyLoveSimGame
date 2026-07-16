using FantasyLoveSim.EditorTools;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace FantasyLoveSim.EditorTests
{
    public class TrainingDialogueSyncServiceTests
    {
        [Test]
        public void BuildImportItems_ConvertsFiveStatesAndMultipleMessages()
        {
            string[] states = { "SelectedBeforeFirstStep", "SelectedAfterFirstStep", "PlayerLpConsumed", "HeroineLpConsumed", "SimultaneousLpConsumed" };
            List<TrainingDialogueSyncItem> source = states.Select(state => new TrainingDialogueSyncItem
            {
                TrainingId = "CooperativeDrill",
                VisualState = state,
                Messages = new List<string> { " 候補A ", "候補B", "候補A" }
            }).ToList();

            List<TrainingDialogueSyncItem> result = TrainingDialogueSyncService.BuildImportItems(
                source, new HashSet<string> { "CooperativeDrill" }, null);

            Assert.That(result.Count, Is.EqualTo(5));
            CollectionAssert.AreEquivalent(states, result.Select(item => item.VisualState));
            Assert.That(result.All(item => item.Messages.SequenceEqual(new[] { "候補A", "候補B" })), Is.True);
        }

        [Test]
        public void BuildImportItems_SkipsUnknownTrainingStateDuplicatesAndEmptyMessages()
        {
            List<string> warnings = new List<string>();
            List<TrainingDialogueSyncItem> result = TrainingDialogueSyncService.BuildImportItems(
                new[]
                {
                    Item("UnknownTraining", "PlayerLpConsumed", "候補"),
                    Item("CooperativeDrill", "UnknownState", "候補"),
                    Item("CooperativeDrill", "PlayerLpConsumed", "候補"),
                    Item("CooperativeDrill", "PlayerLpConsumed", "重複枠"),
                    Item("CooperativeDrill", "HeroineLpConsumed", " ")
                },
                new HashSet<string> { "CooperativeDrill" }, warnings.Add);

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].Messages, Is.EqualTo(new[] { "候補" }));
            Assert.That(warnings.Count, Is.EqualTo(4));
        }

        [Test]
        public void BuildImportItems_AllowsBlankTrainingIdForHeroineFallback()
        {
            List<TrainingDialogueSyncItem> result = TrainingDialogueSyncService.BuildImportItems(
                new[] { Item("", "SimultaneousLpConsumed", "共通候補") }, new HashSet<string>(), null);

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].TrainingId, Is.Empty);
        }

        [Test]
        public void BuildExportItems_MergesDuplicateEntriesAndMessages()
        {
            List<string> warnings = new List<string>();
            List<TrainingDialogueSyncItem> result = TrainingDialogueSyncService.BuildExportItems(
                new[]
                {
                    Item(" CooperativeDrill ", "PlayerLpConsumed", " 候補A ", "候補A"),
                    Item("CooperativeDrill", "PlayerLpConsumed", "候補B", " "),
                    Item("CooperativeDrill", "HeroineLpConsumed", " ")
                }, warnings.Add);

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].TrainingId, Is.EqualTo("CooperativeDrill"));
            Assert.That(result[0].Messages, Is.EqualTo(new[] { "候補A", "候補B" }));
            Assert.That(warnings.Count, Is.EqualTo(2));
        }

        [TestCase(2, "TestHeroine")]
        [TestCase(1, "OtherHeroine")]
        public void ValidateImportHeader_RejectsUnsupportedVersionOrDifferentHeroine(
            int schemaVersion,
            string heroineId)
        {
            List<string> warnings = new List<string>();

            bool result = TrainingDialogueSyncService.ValidateImportHeader(
                schemaVersion, 1, heroineId, "TestHeroine", warnings.Add);

            Assert.That(result, Is.False);
            Assert.That(warnings.Count, Is.EqualTo(1));
        }

        [Test]
        public void ValidateImportHeader_AcceptsSupportedVersionAndMatchingHeroine()
        {
            bool result = TrainingDialogueSyncService.ValidateImportHeader(
                1, 1, "TestHeroine", "TestHeroine", null);

            Assert.That(result, Is.True);
        }

        private static TrainingDialogueSyncItem Item(string trainingId, string state, params string[] messages)
        {
            return new TrainingDialogueSyncItem { TrainingId = trainingId, VisualState = state, Messages = messages.ToList() };
        }
    }
}
