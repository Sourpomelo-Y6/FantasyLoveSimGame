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
