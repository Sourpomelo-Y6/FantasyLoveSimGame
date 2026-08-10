using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

internal static class TrainingCatalogAssetSync
{
    private const string RelativePath = "Data/training_catalog_export.json";

    internal static void Import(
        string exportFolder,
        string heroineId,
        HeroineAssetImporter.HeroineImportReport report)
    {
        string path = Path.Combine(exportFolder, RelativePath);
        if (!File.Exists(path))
        {
            Debug.Log("training_catalog_export.json がないため、既存の訓練条件は変更しません: " + path);
            return;
        }

        TrainingCatalogExport data;
        try
        {
            data = JsonUtility.FromJson<TrainingCatalogExport>(File.ReadAllText(path));
        }
        catch (Exception ex)
        {
            report.Warn("training_catalog_export.json の読み込みに失敗しました: " + ex.Message);
            return;
        }
        if (data == null || data.schemaVersion != 1)
        {
            report.Warn("training_catalog_export.json のschemaVersionが未対応です。");
            return;
        }
        if (!string.IsNullOrWhiteSpace(data.heroineId) &&
            !string.Equals(data.heroineId, heroineId, StringComparison.Ordinal))
        {
            report.Warn("training_catalog_export.json のheroineIdが一致しないためスキップしました: " +
                data.heroineId + " / " + heroineId);
            return;
        }

        Dictionary<string, TrainingData> trainings = Resources.LoadAll<TrainingData>("Training")
            .Where(training => training != null && !string.IsNullOrWhiteSpace(training.trainingId))
            .GroupBy(training => training.trainingId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        SkillTreeNodeData[] nodes = Resources.LoadAll<SkillTreeNodeData>("SkillTreeNodes");
        Dictionary<string, SkillTreeNodeData> editableNodes = nodes
            .Where(node => IsEditableNode(node, heroineId) && !string.IsNullOrWhiteSpace(node.nodeId))
            .GroupBy(node => node.nodeId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        HashSet<string> importedIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (TrainingCatalogItem item in data.items ?? new TrainingCatalogItem[0])
        {
            if (item == null || string.IsNullOrWhiteSpace(item.trainingId) || !importedIds.Add(item.trainingId))
            {
                report.trainingCatalogSkippedCount++;
                report.Warn("空または重複したtrainingIdの訓練条件をスキップしました。");
                continue;
            }
            TrainingData training;
            if (!trainings.TryGetValue(item.trainingId, out training))
            {
                report.trainingCatalogSkippedCount++;
                report.Warn("存在しないTrainingDataの条件をスキップしました: " + item.trainingId);
                continue;
            }
            TrainingOccurrenceType occurrence;
            if (!Enum.TryParse(item.occurrenceType, true, out occurrence))
            {
                report.trainingCatalogSkippedCount++;
                report.Warn("不正なoccurrenceTypeのためスキップしました: " + item.trainingId + " / " + item.occurrenceType);
                continue;
            }
            TrainingConditionRank[] visible;
            TrainingConditionRank[] executable;
            if (!TryParseRanks(item.visibleConditionRanks, out visible) ||
                !TryParseRanks(item.executableConditionRanks, out executable))
            {
                report.trainingCatalogSkippedCount++;
                report.Warn("不正な調子条件のためスキップしました: " + item.trainingId);
                continue;
            }

            Undo.RecordObject(training, "Import training availability conditions");
            training.unlockedByDefault = item.unlockedByDefault;
            training.sortOrder = item.sortOrder;
            training.occurrenceType = occurrence;
            training.visibleConditionRanks = visible;
            training.executableConditionRanks = executable;
            training.requiredCompletedTrainingIds = CleanIds(item.requiredCompletedTrainingIds);
            training.requireAllCompletedTrainings = item.requireAllCompletedTrainings;
            training.hideUntilPrerequisitesMet = item.hideUntilPrerequisitesMet;
            training.hideAfterCompletion = item.hideAfterCompletion;
            EditorUtility.SetDirty(training);

            SynchronizeUnlockNodes(item, editableNodes, report);
            report.trainingCatalogUpdatedCount++;
        }
    }

    private static void SynchronizeUnlockNodes(
        TrainingCatalogItem item,
        Dictionary<string, SkillTreeNodeData> editableNodes,
        HeroineAssetImporter.HeroineImportReport report)
    {
        HashSet<string> selected = new HashSet<string>(CleanIds(item.unlockNodeIds), StringComparer.Ordinal);
        foreach (string nodeId in selected.Where(id => !editableNodes.ContainsKey(id)))
            report.Warn("存在しない、または別ヒロインの解放ノードをスキップしました: " + nodeId);

        foreach (SkillTreeNodeData node in editableNodes.Values)
        {
            node.unlockedTrainingIds = node.unlockedTrainingIds ?? new List<string>();
            bool shouldContain = selected.Contains(node.nodeId);
            bool contains = node.unlockedTrainingIds.Contains(item.trainingId);
            if (shouldContain == contains) continue;
            Undo.RecordObject(node, "Import training unlock node");
            if (shouldContain) node.unlockedTrainingIds.Add(item.trainingId);
            else node.unlockedTrainingIds.RemoveAll(id => string.Equals(id, item.trainingId, StringComparison.Ordinal));
            EditorUtility.SetDirty(node);
        }
    }

    private static bool IsEditableNode(SkillTreeNodeData node, string heroineId)
    {
        if (node == null) return false;
        return node.owner != SkillTreeOwner.Heroine || string.IsNullOrWhiteSpace(node.targetHeroineId) ||
            string.Equals(node.targetHeroineId, heroineId, StringComparison.Ordinal);
    }

    private static bool TryParseRanks(string[] values, out TrainingConditionRank[] result)
    {
        List<TrainingConditionRank> ranks = new List<TrainingConditionRank>();
        foreach (string value in values ?? new string[0])
        {
            TrainingConditionRank rank;
            if (!Enum.TryParse(value, true, out rank)) { result = null; return false; }
            if (!ranks.Contains(rank)) ranks.Add(rank);
        }
        result = ranks.ToArray();
        return true;
    }

    private static string[] CleanIds(IEnumerable<string> values)
    {
        return (values ?? Enumerable.Empty<string>()).Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim()).Distinct(StringComparer.Ordinal).ToArray();
    }

    [Serializable]
    private sealed class TrainingCatalogExport
    {
        public int schemaVersion;
        public string heroineId;
        public TrainingCatalogItem[] items;
    }

    [Serializable]
    private sealed class TrainingCatalogItem
    {
        public string trainingId;
        public bool unlockedByDefault;
        public int sortOrder;
        public string occurrenceType;
        public string[] visibleConditionRanks;
        public string[] executableConditionRanks;
        public string[] requiredCompletedTrainingIds;
        public bool requireAllCompletedTrainings;
        public bool hideUntilPrerequisitesMet;
        public bool hideAfterCompletion;
        public string[] unlockNodeIds;
    }
}
