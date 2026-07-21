#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public class OutfitExpressionDataTests
{
    private readonly List<Object> createdObjects = new List<Object>();

    [TearDown]
    public void TearDown()
    {
        foreach (Object createdObject in createdObjects)
        {
            if (createdObject != null)
            {
                Object.DestroyImmediate(createdObject);
            }
        }
        createdObjects.Clear();
    }

    [Test]
    public void OutfitManager_UsesHeroineOverrideBeforeSharedOutfitExpression()
    {
        GameObject gameObject = new GameObject("OutfitManagerTest");
        createdObjects.Add(gameObject);
        OutfitManager manager = gameObject.AddComponent<OutfitManager>();
        OutfitData outfit = ScriptableObject.CreateInstance<OutfitData>();
        createdObjects.Add(outfit);
        outfit.outfitId = "Dress";
        outfit.changedExpressionId = "Neutral";

        manager.SetMessageOverrides(new List<OutfitMessageOverride>
        {
            new OutfitMessageOverride
            {
                outfitId = "Dress",
                changedExpressionId = "Shy"
            }
        });

        Assert.That(manager.GetChangedExpressionId(outfit), Is.EqualTo("Shy"));
    }

    [Test]
    public void OutfitPreferenceManager_ReturnsConfiguredReactionExpression()
    {
        GameObject gameObject = new GameObject("OutfitPreferenceManagerTest");
        createdObjects.Add(gameObject);
        OutfitPreferenceManager manager = gameObject.AddComponent<OutfitPreferenceManager>();
        manager.SetReactionMessageOverrides(new List<OutfitReactionMessageOverride>
        {
            new OutfitReactionMessageOverride
            {
                reactionType = OutfitReactionType.Praise,
                message = "reaction",
                expressionId = "Shy"
            }
        });

        Assert.That(
            manager.GetReactionExpressionId(OutfitReactionType.Praise),
            Is.EqualTo("Shy"));
        Assert.That(
            manager.GetReactionExpressionId(OutfitReactionType.Dislike),
            Is.Empty);
    }

    [Test]
    public void TestHeroineProfile_OutfitExpressionsReferenceRegisteredLayers()
    {
        HeroineProfileData profile = Resources.Load<HeroineProfileData>("Heroines/TestHeroineProfile");
        HeroineLayeredSpriteData layeredData =
            Resources.Load<HeroineLayeredSpriteData>("Heroines/TestHeroine/HeroineLayeredSpriteData");

        Assert.That(profile, Is.Not.Null);
        Assert.That(layeredData, Is.Not.Null);
        HashSet<string> expressionIds = new HashSet<string>(
            layeredData.expressionLayers
                .Where(layer => layer != null && !string.IsNullOrWhiteSpace(layer.expressionId))
                .Select(layer => layer.expressionId));

        Assert.That(
            profile.outfitMessageOverrides
                .Where(item => !string.IsNullOrWhiteSpace(item.changedExpressionId))
                .All(item => expressionIds.Contains(item.changedExpressionId)),
            Is.True);
        Assert.That(
            profile.outfitReactionMessageOverrides
                .Where(item => !string.IsNullOrWhiteSpace(item.expressionId))
                .All(item => expressionIds.Contains(item.expressionId)),
            Is.True);
    }
}
#endif
