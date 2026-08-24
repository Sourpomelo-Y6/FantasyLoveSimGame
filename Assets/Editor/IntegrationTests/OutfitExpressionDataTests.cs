#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

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
                lockedExpressionId = "Concerned",
                changedExpressionId = "Shy"
            }
        });

        Assert.That(manager.GetLockedExpressionId(outfit), Is.EqualTo("Concerned"));
        Assert.That(manager.GetChangedExpressionId(outfit), Is.EqualTo("Shy"));
    }

    [Test]
    public void AssetToolProfileJson_ImportsAndExportsAllOutfitMessageFields()
    {
        HeroineProfileData profile = ScriptableObject.CreateInstance<HeroineProfileData>();
        createdObjects.Add(profile);
        string json = "{\"heroineId\":\"RoundTripHeroine\",\"outfitMessageOverrides\":[" +
            "{\"outfitId\":\"Formal\",\"lockedMessage\":\"locked\",\"lockedExpressionId\":\"Concerned\"," +
            "\"changedMessage\":\"changed\",\"changedExpressionId\":\"Smile\"}]," +
            "\"outfitReactionMessageOverrides\":[" +
            "{\"reactionType\":\"Praise\",\"message\":\"praise\",\"expressionId\":\"Smile\"}," +
            "{\"reactionType\":\"Dislike\",\"message\":\"dislike\",\"expressionId\":\"Angry\"}," +
            "{\"reactionType\":\"Bored\",\"message\":\"bored\",\"expressionId\":\"Neutral\"}," +
            "{\"reactionType\":\"Change\",\"message\":\"change\",\"expressionId\":\"Shy\"}]}";

        HeroineAssetImporter.ApplyProfileJsonForTests(profile, json);

        Assert.That(profile.outfitMessageOverrides.Count, Is.EqualTo(1));
        OutfitMessageOverride outfit = profile.outfitMessageOverrides[0];
        Assert.That(outfit.outfitId, Is.EqualTo("Formal"));
        Assert.That(outfit.lockedMessage, Is.EqualTo("locked"));
        Assert.That(outfit.lockedExpressionId, Is.EqualTo("Concerned"));
        Assert.That(outfit.changedMessage, Is.EqualTo("changed"));
        Assert.That(outfit.changedExpressionId, Is.EqualTo("Smile"));
        Assert.That(profile.outfitReactionMessageOverrides.Select(item => item.reactionType),
            Is.EqualTo(new[]
            {
                OutfitReactionType.Praise,
                OutfitReactionType.Dislike,
                OutfitReactionType.Bored,
                OutfitReactionType.Change
            }));

        string outputFolder = Path.Combine(Path.GetFullPath("Temp"),
            "OutfitExpressionDataTests", System.Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outputFolder);
        try
        {
            HeroineUnityDataExporter.ExportProfile(
                profile,
                outputFolder,
                new HeroineUnityDataExporter.HeroineUnityExportReport());
            string exported = File.ReadAllText(Path.Combine(outputFolder, "heroine_profile_from_unity.json"));

            StringAssert.Contains("\"lockedExpressionId\": \"Concerned\"", exported);
            StringAssert.Contains("\"changedExpressionId\": \"Smile\"", exported);
            StringAssert.Contains("\"reactionType\": \"Praise\"", exported);
            StringAssert.Contains("\"reactionType\": \"Dislike\"", exported);
            StringAssert.Contains("\"reactionType\": \"Bored\"", exported);
            StringAssert.Contains("\"reactionType\": \"Change\"", exported);
            StringAssert.Contains("\"expressionId\": \"Shy\"", exported);
        }
        finally
        {
            if (Directory.Exists(outputFolder)) Directory.Delete(outputFolder, true);
        }
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

    [Test]
    public void LayeredSpriteData_DetectsEightLayerDataWithoutBreakingLegacyData()
    {
        HeroineLayeredSpriteData data = ScriptableObject.CreateInstance<HeroineLayeredSpriteData>();
        createdObjects.Add(data);
        data.baseBodyLayers.Add(new LayerEntry { assetId = "Legacy" });

        Assert.That(data.HasEightLayerData(), Is.False);

        data.headExpressionLayers.Add(new LayerEntry
        {
            assetId = "Head_Normal",
            layerKind = HeroineVisualLayerKinds.HeadExpression,
            expressionId = "Neutral"
        });

        Assert.That(data.HasEightLayerData(), Is.True);
    }

    [Test]
    public void LayeredSpriteView_SelectsCostumeAndCombinedHeadExpressionLayers()
    {
        GameObject root = new GameObject("EightLayerView");
        createdObjects.Add(root);
        Image costumeImage = CreateLayerImage(root.transform, "CostumeBodyImage");
        Image headImage = CreateLayerImage(root.transform, "HeadExpressionImage");
        Image effectImage = CreateLayerImage(root.transform, "EffectImage");
        HeroineLayeredSpriteView view = root.AddComponent<HeroineLayeredSpriteView>();

        HeroineLayeredSpriteData data = ScriptableObject.CreateInstance<HeroineLayeredSpriteData>();
        createdObjects.Add(data);
        data.defaultCostumeId = "Default";
        data.defaultExpressionId = "Neutral";
        Sprite defaultCostume = CreateSprite();
        Sprite summerCostume = CreateSprite();
        Sprite neutralHead = CreateSprite();
        Sprite smileHead = CreateSprite();
        Sprite effect = CreateSprite();
        data.costumeBodyLayers.Add(CreateLayer(
            "Costume_Default", HeroineVisualLayerKinds.CostumeBody, "Default", "", 30, defaultCostume));
        data.costumeBodyLayers.Add(CreateLayer(
            "Costume_Summer", HeroineVisualLayerKinds.CostumeBody, "Summer", "", 30, summerCostume));
        data.headExpressionLayers.Add(CreateLayer(
            "Head_Neutral", HeroineVisualLayerKinds.HeadExpression, "", "Neutral", 40, neutralHead));
        data.headExpressionLayers.Add(CreateLayer(
            "Head_Smile", HeroineVisualLayerKinds.HeadExpression, "", "Smile", 40, smileHead));
        data.effectLayers.Add(CreateLayer(
            "Effect_Common", HeroineVisualLayerKinds.Effect, "", "", 80, effect));

        view.SetData(data);
        bool visible = view.Refresh("Summer", "Smile");

        Assert.That(visible, Is.True);
        Assert.That(costumeImage.sprite, Is.SameAs(summerCostume));
        Assert.That(headImage.sprite, Is.SameAs(smileHead));
        Assert.That(effectImage.sprite, Is.SameAs(effect));
        Assert.That(costumeImage.transform.GetSiblingIndex(), Is.LessThan(headImage.transform.GetSiblingIndex()));
        Assert.That(headImage.transform.GetSiblingIndex(), Is.LessThan(effectImage.transform.GetSiblingIndex()));
    }

    [Test]
    public void LayeredSpriteView_UsesLegacyCostumeAndExpressionDuringEightLayerMigration()
    {
        GameObject root = new GameObject("MixedLayerView");
        createdObjects.Add(root);
        Image costumeImage = CreateLayerImage(root.transform, "CostumeBodyImage");
        Image headImage = CreateLayerImage(root.transform, "HeadExpressionImage");
        HeroineLayeredSpriteView view = root.AddComponent<HeroineLayeredSpriteView>();

        HeroineLayeredSpriteData data = ScriptableObject.CreateInstance<HeroineLayeredSpriteData>();
        createdObjects.Add(data);
        data.defaultCostumeId = "Default";
        data.defaultExpressionId = "Neutral";
        Sprite defaultCostume = CreateSprite();
        Sprite townCostume = CreateSprite();
        Sprite summerCostume = CreateSprite();
        Sprite newNeutralExpression = CreateSprite();
        Sprite legacySmileExpression = CreateSprite();
        data.costumeBodyLayers.Add(CreateLayer(
            "Costume_Default", HeroineVisualLayerKinds.CostumeBody, "Default", "", 40, defaultCostume));
        data.costumeBodyLayers.Add(CreateLayer(
            "Costume_Town", HeroineVisualLayerKinds.CostumeBody, "Town", "", 40, townCostume));
        data.headExpressionLayers.Add(CreateLayer(
            "Head_Neutral", HeroineVisualLayerKinds.HeadExpression, "", "Neutral", 50, newNeutralExpression));
        data.costumeLayers.Add(CreateLayer(
            "Costume_Summer", HeroineVisualLayerKinds.LegacyCostume, "Summer", "", 10, summerCostume));
        data.expressionLayers.Add(CreateLayer(
            "Expression_Smile", HeroineVisualLayerKinds.LegacyExpression, "", "Smile", 20, legacySmileExpression));

        view.SetData(data);
        bool visible = view.Refresh("Summer", "Smile");

        Assert.That(visible, Is.True);
        Assert.That(costumeImage.sprite, Is.SameAs(summerCostume));
        Assert.That(headImage.sprite, Is.SameAs(legacySmileExpression));
    }

    [Test]
    public void LayerImporter_MergesPresentSlotAndPreservesAbsentCostumes()
    {
        HeroineLayeredSpriteData data = ScriptableObject.CreateInstance<HeroineLayeredSpriteData>();
        createdObjects.Add(data);
        Sprite defaultSprite = CreateSprite();
        Sprite oldTownSprite = CreateSprite();
        Sprite newTownSprite = CreateSprite();
        data.costumeLayers.Add(CreateLayer(
            "Costume_Default", HeroineVisualLayerKinds.LegacyCostume, "Default", "", 10, defaultSprite));
        data.costumeLayers.Add(CreateLayer(
            "Costume_Town_Old", HeroineVisualLayerKinds.LegacyCostume, "Town", "", 10, oldTownSprite));

        HeroineAssetImporter.MergeLayerEntry(
            data,
            CreateLayer(
                "Costume_Town",
                HeroineVisualLayerKinds.CostumeBody,
                "Town",
                "",
                40,
                newTownSprite));

        Assert.That(data.costumeLayers.Count, Is.EqualTo(1));
        Assert.That(data.costumeLayers[0].assetId, Is.EqualTo("Costume_Default"));
        Assert.That(data.costumeBodyLayers.Count, Is.EqualTo(1));
        Assert.That(data.costumeBodyLayers[0].assetId, Is.EqualTo("Costume_Town"));
        Assert.That(data.costumeBodyLayers[0].sprite, Is.SameAs(newTownSprite));
    }

    private Image CreateLayerImage(Transform parent, string name)
    {
        GameObject layer = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        layer.transform.SetParent(parent, false);
        createdObjects.Add(layer);
        return layer.GetComponent<Image>();
    }

    private Sprite CreateSprite()
    {
        Texture2D texture = new Texture2D(2, 2);
        createdObjects.Add(texture);
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.one * 0.5f);
        createdObjects.Add(sprite);
        return sprite;
    }

    private static LayerEntry CreateLayer(
        string assetId,
        string layerKind,
        string costumeId,
        string expressionId,
        int drawOrder,
        Sprite sprite)
    {
        return new LayerEntry
        {
            assetId = assetId,
            layerKind = layerKind,
            costumeId = costumeId,
            expressionId = expressionId,
            drawOrder = drawOrder,
            sprite = sprite
        };
    }
}
#endif
