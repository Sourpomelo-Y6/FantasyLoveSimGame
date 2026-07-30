#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class TrainingListPresentationTests
{
    private readonly List<TrainingData> created = new List<TrainingData>();

    [TearDown]
    public void TearDown()
    {
        foreach (TrainingData training in created)
        {
            Object.DestroyImmediate(training);
        }
        created.Clear();
    }

    [Test]
    public void CreateDisplayList_SortsAvailableBeforeLockedAndUsesStableFields()
    {
        TrainingData locked = Create("Locked", "あ", 0, false);
        TrainingData later = Create("Later", "う", 20, true);
        TrainingData first = Create("First", "い", 10, true);

        List<TrainingData> result = TrainingListPresentation.CreateDisplayList(
            new[] { locked, later, first },
            training => training.unlockedByDefault,
            false);

        Assert.That(result, Is.EqualTo(new[] { first, later, locked }));
    }

    [Test]
    public void CreateDisplayList_AvailableOnlyExcludesLockedTraining()
    {
        TrainingData available = Create("Available", "実行可能", 0, true);
        TrainingData locked = Create("Locked", "未解放", 0, false);

        List<TrainingData> result = TrainingListPresentation.CreateDisplayList(
            new[] { locked, available },
            training => training.unlockedByDefault,
            true);

        Assert.That(result, Is.EqualTo(new[] { available }));
    }

    [Test]
    public void Availability_EmptyConditions_PreservesLegacyAvailability()
    {
        TrainingData training = Create("Legacy", "Legacy", 0, true);

        TrainingAvailability result = TrainingAvailabilityEvaluator.Evaluate(
            training,
            TrainingConditionRank.Awful,
            true,
            id => false);

        Assert.AreEqual(TrainingAvailabilityState.Available, result.state);
    }

    [Test]
    public void Availability_ExcellentOnly_IsHiddenOnNormalDay()
    {
        TrainingData training = Create("Special", "Special", 0, true);
        training.visibleConditionRanks =
            new[] { TrainingConditionRank.Excellent };

        TrainingAvailability result = TrainingAvailabilityEvaluator.Evaluate(
            training,
            TrainingConditionRank.Normal,
            true,
            id => false);

        Assert.AreEqual(TrainingAvailabilityState.Hidden, result.state);
    }

    [Test]
    public void Availability_DisallowedExecutionRank_ReturnsReason()
    {
        TrainingData training = Create("Hard", "Hard", 0, true);
        training.executableConditionRanks = new[]
        {
            TrainingConditionRank.Excellent,
            TrainingConditionRank.Good,
            TrainingConditionRank.Normal
        };

        TrainingAvailability result = TrainingAvailabilityEvaluator.Evaluate(
            training,
            TrainingConditionRank.Poor,
            true,
            id => false);

        Assert.AreEqual(TrainingAvailabilityState.Disabled, result.state);
        StringAssert.Contains("調子", result.ReasonText);
    }

    [Test]
    public void Availability_OncePerSave_DisablesAfterCompletion()
    {
        TrainingData training = Create("Trial", "Trial", 0, true);
        training.occurrenceType = TrainingOccurrenceType.OncePerSave;

        TrainingAvailability result = TrainingAvailabilityEvaluator.Evaluate(
            training,
            TrainingConditionRank.Normal,
            true,
            id => id == "Trial");

        Assert.AreEqual(TrainingAvailabilityState.Disabled, result.state);
        StringAssert.Contains("完了済み", result.ReasonText);
    }

    [Test]
    public void Availability_PrerequisiteChain_UnlocksAfterCompletion()
    {
        TrainingData training = Create("Advanced", "Advanced", 0, true);
        training.requiredCompletedTrainingIds = new[] { "Trial" };
        training.hideUntilPrerequisitesMet = true;

        TrainingAvailability before = TrainingAvailabilityEvaluator.Evaluate(
            training,
            TrainingConditionRank.Normal,
            true,
            id => false);
        TrainingAvailability after = TrainingAvailabilityEvaluator.Evaluate(
            training,
            TrainingConditionRank.Normal,
            true,
            id => id == "Trial");

        Assert.AreEqual(TrainingAvailabilityState.Hidden, before.state);
        Assert.AreEqual(TrainingAvailabilityState.Available, after.state);
    }

    [Test]
    public void SkillProgressStats_Clone_PreservesCompletionRecordsIndependently()
    {
        SkillProgressStats source = new SkillProgressStats();
        source.trainingCompletionRecords.Add(new TrainingCompletionRecord
        {
            trainingId = "Trial",
            completionCount = 1,
            firstCompletedDay = 3,
            lastCompletedDay = 3
        });

        SkillProgressStats copy = source.Clone();
        source.trainingCompletionRecords[0].completionCount = 2;

        Assert.AreEqual(1, copy.trainingCompletionRecords.Count);
        Assert.AreEqual("Trial", copy.trainingCompletionRecords[0].trainingId);
        Assert.AreEqual(1, copy.trainingCompletionRecords[0].completionCount);
        Assert.AreEqual(3, copy.trainingCompletionRecords[0].firstCompletedDay);
    }

    private TrainingData Create(string id, string displayName, int sortOrder, bool unlocked)
    {
        TrainingData training = ScriptableObject.CreateInstance<TrainingData>();
        training.trainingId = id;
        training.displayName = displayName;
        training.sortOrder = sortOrder;
        training.unlockedByDefault = unlocked;
        created.Add(training);
        return training;
    }
}
#endif
