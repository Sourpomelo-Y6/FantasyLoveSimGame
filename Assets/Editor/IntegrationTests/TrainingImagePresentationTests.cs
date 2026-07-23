#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class TrainingImagePresentationTests
{
    private readonly List<Object> createdObjects = new List<Object>();

    [TearDown]
    public void TearDown()
    {
        for (int i = createdObjects.Count - 1; i >= 0; i--)
        {
            if (createdObjects[i] != null)
            {
                Object.DestroyImmediate(createdObjects[i]);
            }
        }
        createdObjects.Clear();
    }

    [Test]
    public void ResolveSprite_TrainingSpecificImageTakesPriority()
    {
        HeroineTrainingImageData data = CreateData();
        Sprite defaultSprite = CreateSprite();
        Sprite trainingSprite = CreateSprite();
        data.defaultPlayerLpConsumedSprite = defaultSprite;
        data.entries.Add(new HeroineTrainingImageEntry
        {
            trainingId = "LightPractice",
            playerLpConsumedSprite = trainingSprite
        });

        Sprite resolved = data.ResolveSprite(
            "LightPractice",
            TrainingVisualState.PlayerLpConsumed);

        Assert.That(resolved, Is.SameAs(trainingSprite));
    }

    [Test]
    public void ResolveSprite_MissingTrainingImageUsesStateDefault()
    {
        HeroineTrainingImageData data = CreateData();
        Sprite defaultSprite = CreateSprite();
        data.defaultHeroineLpConsumedSprite = defaultSprite;
        data.entries.Add(new HeroineTrainingImageEntry
        {
            trainingId = "LightPractice"
        });

        Sprite resolved = data.ResolveSprite(
            "LightPractice",
            TrainingVisualState.HeroineLpConsumed);

        Assert.That(resolved, Is.SameAs(defaultSprite));
    }

    [Test]
    public void ResolveSprite_MissingTrainingAndDefaultImagesReturnsNull()
    {
        HeroineTrainingImageData data = CreateData();

        Sprite resolved = data.ResolveSprite(
            "MissingTraining",
            TrainingVisualState.SelectedAfterFirstStep);

        Assert.That(resolved, Is.Null);
    }

    [TestCase(0, 0, TrainingVisualState.SelectedAfterFirstStep)]
    [TestCase(1, 0, TrainingVisualState.PlayerLpConsumed)]
    [TestCase(0, 1, TrainingVisualState.HeroineLpConsumed)]
    [TestCase(1, 1, TrainingVisualState.SimultaneousLpConsumed)]
    public void ResolveStepState_UsesLpConsumptionWithSimultaneousPriority(
        int playerLpConsumed,
        int heroineLpConsumed,
        TrainingVisualState expected)
    {
        TrainingStepResult result = new TrainingStepResult
        {
            playerLpConsumed = playerLpConsumed,
            heroineLpConsumed = heroineLpConsumed
        };

        Assert.That(TrainingVisualStateResolver.Resolve(result), Is.EqualTo(expected));
    }

    private HeroineTrainingImageData CreateData()
    {
        HeroineTrainingImageData data = ScriptableObject.CreateInstance<HeroineTrainingImageData>();
        createdObjects.Add(data);
        return data;
    }

    private Sprite CreateSprite()
    {
        Texture2D texture = new Texture2D(2, 2);
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.zero);
        createdObjects.Add(texture);
        createdObjects.Add(sprite);
        return sprite;
    }
}
#endif
