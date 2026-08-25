#if UNITY_INCLUDE_TESTS
using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class HeroineSkillTreeAssetSyncIntegrationTests
{
    private const string HeroineId = "__HeroineSkillTreeSyncTest";
    private const string SkillFolder = "Assets/Resources/Skills/Heroines/" + HeroineId;
    private const string NodeFolder = "Assets/Resources/SkillTreeNodes/Heroines/" + HeroineId;
    private string exchangeFolder;

    [SetUp]
    public void SetUp()
    {
        AssetDatabase.DeleteAsset(SkillFolder);
        AssetDatabase.DeleteAsset(NodeFolder);
        exchangeFolder = Path.Combine(Path.GetFullPath("Temp"), "HeroineSkillTreeAssetSyncIntegrationTests");
        if (Directory.Exists(exchangeFolder)) Directory.Delete(exchangeFolder, true);
        Directory.CreateDirectory(Path.Combine(exchangeFolder, "Data"));
    }

    [TearDown]
    public void TearDown()
    {
        AssetDatabase.DeleteAsset(SkillFolder);
        AssetDatabase.DeleteAsset(NodeFolder);
        AssetDatabase.Refresh();
        if (Directory.Exists(exchangeFolder)) Directory.Delete(exchangeFolder, true);
    }

    [Test]
    public void ImportThenExport_RoundTripsAllSkillTreeFieldsAndMultipleConditions()
    {
        File.WriteAllText(
            Path.Combine(exchangeFolder, HeroineSkillTreeAssetSync.ImportRelativePath),
            BuildImportJson());

        HeroineSkillTreeAssetSync.Import(exchangeFolder, HeroineId);
        HeroineSkillTreeAssetSync.Export(HeroineId, exchangeFolder);

        SkillsFile result = JsonUtility.FromJson<SkillsFile>(File.ReadAllText(
            Path.Combine(exchangeFolder, HeroineSkillTreeAssetSync.ExportFileName)));

        Assert.That(result.heroineId, Is.EqualTo(HeroineId));
        Assert.That(result.trainingSkills.Length, Is.EqualTo(2));
        TrainingSkillItem focused = result.trainingSkills.Single(x => x.skillId.EndsWith("_Focused"));
        Assert.That(focused.displayName, Is.EqualTo("集中訓練"));
        Assert.That(focused.description, Is.EqualTo("全フィールド往復"));
        Assert.That(focused.sortOrder, Is.EqualTo(7));
        Assert.That(focused.isEnabled, Is.False);
        Assert.That(focused.playerHpCostReduction, Is.EqualTo(2));
        Assert.That(focused.heroineHpCostReduction, Is.EqualTo(3));
        Assert.That(focused.affectionRewardModifier, Is.EqualTo(-4));
        Assert.That(focused.proficiencyRewardModifier, Is.EqualTo(5));
        Assert.That(focused.applicationScope, Is.EqualTo("TrainingCategory"));
        Assert.That(focused.applicationTargetId, Is.EqualTo("CategoryA"));

        NodeItem root = result.nodes.Single(x => x.nodeId.EndsWith("_Root"));
        NodeItem advanced = result.nodes.Single(x => x.nodeId.EndsWith("_Advanced"));
        Assert.That(root.prerequisiteNodeIds, Is.Empty);
        Assert.That(root.unlockEventId, Is.Empty);
        Assert.That(advanced.trainingSkillId, Does.EndWith("_Focused"));
        Assert.That(advanced.grantedHeroineSkillId, Is.EqualTo("HeroineBattleSkillA"));
        Assert.That(advanced.sortOrder, Is.EqualTo(9));
        Assert.That(advanced.skillPointCost, Is.EqualTo(4));
        Assert.That(advanced.prerequisiteNodeIds, Is.EqualTo(new[] { HeroineId + "_Root" }));
        Assert.That(advanced.unlockedTrainingIds, Is.EqualTo(new[] { "TrainingA", "TrainingB" }));
        Assert.That(advanced.unlockEventId, Is.EqualTo("UnlockEventA"));
        Assert.That(advanced.unlockConditions.Length, Is.EqualTo(3));
        Assert.That(advanced.unlockConditions.Select(x => x.conditionType),
            Is.EqualTo(new[] { "TrainingCount", "TrainingProficiency", "Day" }));
        Assert.That(advanced.unlockConditions[0].scope, Is.EqualTo("TrainingCategory"));
        Assert.That(advanced.unlockConditions[0].targetId, Is.EqualTo("CategoryA"));
        Assert.That(advanced.unlockConditions[0].requiredValue, Is.EqualTo(12));
        Assert.That(advanced.unlockConditions[2].targetId, Is.Empty);
        Assert.That(advanced.treePositionX, Is.EqualTo(120.5f));
        Assert.That(advanced.treePositionY, Is.EqualTo(-36.25f));
    }

    private static string BuildImportJson()
    {
        return "{\"schemaVersion\":1,\"heroineId\":\"" + HeroineId + "\"," +
            "\"trainingSkills\":[" +
            "{\"skillId\":\"" + HeroineId + "_Focused\",\"displayName\":\"集中訓練\",\"description\":\"全フィールド往復\",\"sortOrder\":7,\"isEnabled\":false,\"playerHpCostReduction\":2,\"heroineHpCostReduction\":3,\"affectionRewardModifier\":-4,\"proficiencyRewardModifier\":5,\"applicationScope\":\"TrainingCategory\",\"applicationTargetId\":\"CategoryA\"}," +
            "{\"skillId\":\"" + HeroineId + "_All\",\"displayName\":\"全訓練\",\"description\":\"\",\"isEnabled\":true,\"applicationScope\":\"AllTrainings\",\"applicationTargetId\":\"\"}]," +
            "\"nodes\":[" +
            "{\"nodeId\":\"" + HeroineId + "_Root\",\"displayName\":\"ルート\",\"trainingSkillId\":\"" + HeroineId + "_All\",\"prerequisiteNodeIds\":[],\"unlockedTrainingIds\":[],\"unlockEventId\":\"\",\"unlockConditions\":[],\"treePositionX\":0,\"treePositionY\":0}," +
            "{\"nodeId\":\"" + HeroineId + "_Advanced\",\"displayName\":\"上級\",\"trainingSkillId\":\"" + HeroineId + "_Focused\",\"grantedHeroineSkillId\":\"HeroineBattleSkillA\",\"sortOrder\":9,\"skillPointCost\":4,\"prerequisiteNodeIds\":[\"" + HeroineId + "_Root\"],\"unlockedTrainingIds\":[\"TrainingA\",\"TrainingB\"],\"unlockEventId\":\"UnlockEventA\",\"unlockConditions\":[" +
            "{\"conditionType\":\"TrainingCount\",\"scope\":\"TrainingCategory\",\"targetId\":\"CategoryA\",\"requiredValue\":12}," +
            "{\"conditionType\":\"TrainingProficiency\",\"scope\":\"Training\",\"targetId\":\"TrainingA\",\"requiredValue\":8}," +
            "{\"conditionType\":\"Day\",\"scope\":\"Total\",\"targetId\":\"\",\"requiredValue\":20}],\"treePositionX\":120.5,\"treePositionY\":-36.25}]}";
    }

    [Serializable] private class SkillsFile { public string heroineId; public TrainingSkillItem[] trainingSkills; public NodeItem[] nodes; }
    [Serializable] private class TrainingSkillItem { public string skillId; public string displayName; public string description; public int sortOrder; public bool isEnabled; public int playerHpCostReduction; public int heroineHpCostReduction; public int affectionRewardModifier; public int proficiencyRewardModifier; public string applicationScope; public string applicationTargetId; }
    [Serializable] private class NodeItem { public string nodeId; public string trainingSkillId; public string grantedHeroineSkillId; public int sortOrder; public int skillPointCost; public string[] prerequisiteNodeIds; public string[] unlockedTrainingIds; public string unlockEventId; public ConditionItem[] unlockConditions; public float treePositionX; public float treePositionY; }
    [Serializable] private class ConditionItem { public string conditionType; public string scope; public string targetId; public int requiredValue; }
}
#endif
