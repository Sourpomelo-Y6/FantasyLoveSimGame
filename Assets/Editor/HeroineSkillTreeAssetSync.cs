using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// AssetTool とヒロイン固有の訓練スキル／スキルツリーを往復する。
/// 主人公共通データと共通 TrainingData は変更しない。
/// </summary>
public static class HeroineSkillTreeAssetSync
{
    public const string ImportRelativePath = "Data/heroine_skills_export.json";
    public const string ExportFileName = "heroine_skills_from_unity.json";

    public static void Import(string exportFolder, string heroineId)
    {
        string path = Path.Combine(exportFolder, ImportRelativePath);
        if (!File.Exists(path)) return;
        HeroineSkillsFile data = JsonUtility.FromJson<HeroineSkillsFile>(File.ReadAllText(path));
        if (data == null || data.schemaVersion != 1)
        {
            Debug.LogWarning("heroine_skills_export.json のschemaVersionを確認してください。");
            return;
        }
        if (!string.IsNullOrWhiteSpace(data.heroineId) &&
            !string.Equals(data.heroineId, heroineId, StringComparison.Ordinal))
        {
            Debug.LogWarning("heroine_skills_export.json のheroineIdが選択中のヒロインと一致しません。");
            return;
        }

        string skillFolder = $"Assets/Resources/Skills/Heroines/{heroineId}";
        string nodeFolder = $"Assets/Resources/SkillTreeNodes/Heroines/{heroineId}";
        EnsureFolder(skillFolder);
        EnsureFolder(nodeFolder);
        Dictionary<string, SkillData> skills = ImportSkills(data.trainingSkills, skillFolder);
        ImportNodes(data.nodes, heroineId, nodeFolder, skills);
        AssetDatabase.SaveAssets();
    }

    public static void Export(string heroineId, string outputFolder)
    {
        SkillTreeNodeData[] allNodes = Resources.LoadAll<SkillTreeNodeData>("SkillTreeNodes");
        List<SkillTreeNodeData> nodes = allNodes.Where(node => node != null &&
            node.owner == SkillTreeOwner.Heroine &&
            string.Equals(node.targetHeroineId, heroineId, StringComparison.Ordinal))
            .OrderBy(node => node.sortOrder).ThenBy(node => node.nodeId).ToList();
        List<SkillData> skills = nodes.Select(node => node.skill).Where(skill => skill != null &&
            skill.category == SkillCategory.Training && skill.canUseInTraining)
            .GroupBy(skill => skill.skillId, StringComparer.Ordinal).Select(group => group.First())
            .OrderBy(skill => skill.sortOrder).ThenBy(skill => skill.skillId).ToList();

        HeroineSkillsFile data = new HeroineSkillsFile
        {
            schemaVersion = 1,
            heroineId = heroineId,
            trainingSkills = skills.Select(ToExport).ToArray(),
            nodes = nodes.Select(ToExport).ToArray()
        };
        File.WriteAllText(Path.Combine(outputFolder, ExportFileName), JsonUtility.ToJson(data, true));
    }

    private static Dictionary<string, SkillData> ImportSkills(TrainingSkillItem[] source, string folder)
    {
        Dictionary<string, SkillData> result = new Dictionary<string, SkillData>(StringComparer.Ordinal);
        if (source == null)
        {
            foreach (SkillData skill in LoadAssetsInFolder<SkillData>(folder))
                if (skill != null && !string.IsNullOrWhiteSpace(skill.skillId)) result[skill.skillId] = skill;
            return result;
        }
        HashSet<string> importedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (TrainingSkillItem item in source)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.skillId)) continue;
            string id = item.skillId.Trim();
            string path = $"{folder}/{SafeFileName(id)}.asset";
            SkillData skill = AssetDatabase.LoadAssetAtPath<SkillData>(path);
            if (skill == null) { skill = ScriptableObject.CreateInstance<SkillData>(); AssetDatabase.CreateAsset(skill, path); }
            skill.skillId = id;
            skill.displayName = item.displayName ?? string.Empty;
            skill.description = item.description ?? string.Empty;
            skill.category = SkillCategory.Training;
            skill.effectType = SkillEffectType.Buff;
            skill.targetType = SkillTargetType.Self;
            skill.sortOrder = item.sortOrder;
            skill.isEnabled = item.isEnabled;
            skill.trainingPlayerHpCostReduction = Math.Max(0, item.playerHpCostReduction);
            skill.trainingHeroineHpCostReduction = Math.Max(0, item.heroineHpCostReduction);
            skill.trainingAffectionRewardModifier = item.affectionRewardModifier;
            skill.trainingProficiencyRewardModifier = item.proficiencyRewardModifier;
            skill.trainingApplicationScope = Parse(item.applicationScope, TrainingSkillApplicationScope.AllTrainings);
            skill.trainingApplicationTargetId = item.applicationTargetId ?? string.Empty;
            skill.canUseInBattle = false;
            skill.canUseInTraining = true;
            EditorUtility.SetDirty(skill);
            result[id] = skill;
            importedPaths.Add(path);
        }
        DeleteMissingAssets<SkillData>(folder, importedPaths);
        return result;
    }

    private static void ImportNodes(NodeItem[] source, string heroineId, string folder, Dictionary<string, SkillData> skills)
    {
        if (source == null) return;
        Dictionary<string, SkillTreeNodeData> imported = new Dictionary<string, SkillTreeNodeData>(StringComparer.Ordinal);
        HashSet<string> paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (NodeItem item in source)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.nodeId)) continue;
            string id = item.nodeId.Trim();
            string path = $"{folder}/{SafeFileName(id)}.asset";
            SkillTreeNodeData node = AssetDatabase.LoadAssetAtPath<SkillTreeNodeData>(path);
            if (node == null) { node = ScriptableObject.CreateInstance<SkillTreeNodeData>(); AssetDatabase.CreateAsset(node, path); }
            node.nodeId = id;
            node.displayName = item.displayName ?? string.Empty;
            node.owner = SkillTreeOwner.Heroine;
            node.targetHeroineId = heroineId;
            node.grantedHeroineSkillId = item.grantedHeroineSkillId ?? string.Empty;
            node.skill = !string.IsNullOrWhiteSpace(item.trainingSkillId) && skills.TryGetValue(item.trainingSkillId, out SkillData skill) ? skill : null;
            node.sortOrder = item.sortOrder;
            node.skillPointCost = Math.Max(0, item.skillPointCost);
            node.unlockedTrainingIds = CleanIds(item.unlockedTrainingIds);
            node.unlockEventHeroineId = heroineId;
            node.unlockEventId = item.unlockEventId ?? string.Empty;
            node.unlockConditions = (item.unlockConditions ?? Array.Empty<ConditionItem>()).Where(x => x != null)
                .Select(x => new SkillTreeUnlockCondition
                {
                    conditionType = Parse(x.conditionType, SkillTreeConditionType.TrainingCount),
                    scope = Parse(x.scope, SkillTreeProgressScope.Total),
                    targetId = x.targetId ?? string.Empty,
                    requiredValue = Math.Max(0, x.requiredValue)
                }).ToList();
            node.treePosition = new Vector2(item.treePositionX, item.treePositionY);
            imported[id] = node;
            paths.Add(path);
        }
        Dictionary<string, SkillTreeNodeData> allNodes = Resources.LoadAll<SkillTreeNodeData>("SkillTreeNodes")
            .Where(x => x != null && !string.IsNullOrWhiteSpace(x.nodeId))
            .GroupBy(x => x.nodeId, StringComparer.Ordinal).ToDictionary(x => x.Key, x => x.First(), StringComparer.Ordinal);
        foreach (NodeItem item in source.Where(x => x != null && imported.ContainsKey(x.nodeId?.Trim() ?? string.Empty)))
        {
            SkillTreeNodeData node = imported[item.nodeId.Trim()];
            node.prerequisiteNodes = CleanIds(item.prerequisiteNodeIds)
                .Select(id => imported.TryGetValue(id, out SkillTreeNodeData local) ? local : allNodes.TryGetValue(id, out SkillTreeNodeData found) ? found : null)
                .Where(x => x != null).ToList();
            EditorUtility.SetDirty(node);
        }
        DeleteMissingAssets<SkillTreeNodeData>(folder, paths);
    }

    private static TrainingSkillItem ToExport(SkillData skill) => new TrainingSkillItem
    {
        skillId = skill.skillId, displayName = skill.displayName, description = skill.description,
        sortOrder = skill.sortOrder, isEnabled = skill.isEnabled,
        playerHpCostReduction = skill.trainingPlayerHpCostReduction,
        heroineHpCostReduction = skill.trainingHeroineHpCostReduction,
        affectionRewardModifier = skill.trainingAffectionRewardModifier,
        proficiencyRewardModifier = skill.trainingProficiencyRewardModifier,
        applicationScope = skill.trainingApplicationScope.ToString(),
        applicationTargetId = skill.trainingApplicationTargetId
    };

    private static NodeItem ToExport(SkillTreeNodeData node) => new NodeItem
    {
        nodeId = node.nodeId, displayName = node.displayName,
        trainingSkillId = node.skill != null && node.skill.category == SkillCategory.Training ? node.skill.skillId : string.Empty,
        grantedHeroineSkillId = node.grantedHeroineSkillId, sortOrder = node.sortOrder,
        skillPointCost = node.skillPointCost,
        prerequisiteNodeIds = (node.prerequisiteNodes ?? new List<SkillTreeNodeData>()).Where(x => x != null).Select(x => x.nodeId).ToArray(),
        unlockedTrainingIds = (node.unlockedTrainingIds ?? new List<string>()).ToArray(),
        unlockEventId = node.unlockEventId,
        unlockConditions = (node.unlockConditions ?? new List<SkillTreeUnlockCondition>()).Where(x => x != null).Select(x => new ConditionItem
        { conditionType = x.conditionType.ToString(), scope = x.scope.ToString(), targetId = x.targetId, requiredValue = x.requiredValue }).ToArray(),
        treePositionX = node.treePosition.x, treePositionY = node.treePosition.y
    };

    private static List<string> CleanIds(IEnumerable<string> ids) => (ids ?? Enumerable.Empty<string>())
        .Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.Ordinal).ToList();
    private static T Parse<T>(string value, T fallback) where T : struct => Enum.TryParse(value, true, out T parsed) ? parsed : fallback;
    private static string SafeFileName(string value) => string.Concat(value.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
    private static T[] LoadAssetsInFolder<T>(string folder) where T : UnityEngine.Object => AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folder })
        .Select(guid => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid))).Where(x => x != null).ToArray();
    private static void DeleteMissingAssets<T>(string folder, HashSet<string> keep) where T : UnityEngine.Object
    {
        foreach (string guid in AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folder }))
        { string path = AssetDatabase.GUIDToAssetPath(guid); if (!keep.Contains(path)) AssetDatabase.DeleteAsset(path); }
    }
    private static void EnsureFolder(string path)
    {
        string[] parts = path.Split('/'); string current = parts[0];
        for (int i = 1; i < parts.Length; i++) { string next = current + "/" + parts[i]; if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]); current = next; }
    }

    [Serializable] private class HeroineSkillsFile { public int schemaVersion; public string heroineId; public TrainingSkillItem[] trainingSkills; public NodeItem[] nodes; }
    [Serializable] private class TrainingSkillItem { public string skillId; public string displayName; public string description; public int sortOrder; public bool isEnabled = true; public int playerHpCostReduction; public int heroineHpCostReduction; public int affectionRewardModifier; public int proficiencyRewardModifier; public string applicationScope; public string applicationTargetId; }
    [Serializable] private class NodeItem { public string nodeId; public string displayName; public string trainingSkillId; public string grantedHeroineSkillId; public int sortOrder; public int skillPointCost; public string[] prerequisiteNodeIds; public string[] unlockedTrainingIds; public string unlockEventId; public ConditionItem[] unlockConditions; public float treePositionX; public float treePositionY; }
    [Serializable] private class ConditionItem { public string conditionType; public string scope; public string targetId; public int requiredValue; }
}
