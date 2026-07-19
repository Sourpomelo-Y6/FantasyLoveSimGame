using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class SkillTreeValidationReport
{
    private readonly List<string> warnings = new List<string>();

    public int NodeCount { get; internal set; }
    public int WarningCount => warnings.Count;
    public bool IsValid => warnings.Count == 0;
    public IReadOnlyList<string> Warnings => warnings;

    internal void Warn(string message)
    {
        warnings.Add(message);
    }

    public string CreateSummary()
    {
        return "Skill tree validation: nodes=" + NodeCount + " / warnings=" + WarningCount;
    }

    public void Log()
    {
        string summary = "[SkillTreeValidation] " + CreateSummary();
        if (IsValid)
        {
            Debug.Log(summary);
        }
        else
        {
            Debug.LogWarning(summary);
        }

        for (int i = 0; i < warnings.Count; i++)
        {
            Debug.LogWarning("[SkillTreeValidation] " + warnings[i]);
        }
    }
}

public static class SkillTreeDataValidator
{
    public static SkillTreeValidationReport ValidateResources()
    {
        SkillTreeNodeData[] nodes = Resources.LoadAll<SkillTreeNodeData>("SkillTreeNodes");
        HeroineProfileData[] profiles = Resources.LoadAll<HeroineProfileData>("Heroines");
        SkillTreeValidationReport report = Validate(
            nodes,
            Resources.LoadAll<SkillData>("Skills"),
            profiles,
            Resources.LoadAll<TrainingData>("Training"),
            Resources.LoadAll<EnemyData>("Enemies"));
        ValidateAcquisitionEvents(nodes, profiles, report);
        return report;
    }

    public static SkillTreeValidationReport Validate(
        SkillTreeNodeData[] nodes,
        SkillData[] skills,
        HeroineProfileData[] heroineProfiles,
        TrainingData[] trainings,
        EnemyData[] enemies)
    {
        SkillTreeValidationReport report = new SkillTreeValidationReport();
        nodes = nodes ?? new SkillTreeNodeData[0];
        skills = skills ?? new SkillData[0];
        heroineProfiles = heroineProfiles ?? new HeroineProfileData[0];
        trainings = trainings ?? new TrainingData[0];
        enemies = enemies ?? new EnemyData[0];

        HashSet<SkillTreeNodeData> nodeSet = new HashSet<SkillTreeNodeData>();
        Dictionary<string, SkillTreeNodeData> nodesById =
            new Dictionary<string, SkillTreeNodeData>(StringComparer.Ordinal);
        Dictionary<string, SkillData> skillsById = BuildSkillMap(skills, report);
        Dictionary<string, HeroineProfileData> heroinesById =
            BuildHeroineMap(heroineProfiles, report);
        HashSet<string> trainingIds = BuildTrainingIds(trainings, report);
        HashSet<string> defaultUnlockedTrainingIds = BuildDefaultUnlockedTrainingIds(trainings);
        HashSet<string> categoryIds = BuildTrainingCategoryIds(trainings);
        HashSet<string> enemyIds = BuildEnemyIds(enemies, report);

        for (int i = 0; i < nodes.Length; i++)
        {
            SkillTreeNodeData node = nodes[i];
            if (node == null) continue;
            report.NodeCount++;
            nodeSet.Add(node);

            if (string.IsNullOrWhiteSpace(node.nodeId))
            {
                report.Warn("nodeId が空です: asset=" + node.name);
            }
            else if (nodesById.ContainsKey(node.nodeId))
            {
                report.Warn(
                    "nodeId が重複しています: nodeId=" + node.nodeId +
                    " / assets=" + nodesById[node.nodeId].name + ", " + node.name);
            }
            else
            {
                nodesById.Add(node.nodeId, node);
            }
        }

        for (int i = 0; i < nodes.Length; i++)
        {
            SkillTreeNodeData node = nodes[i];
            if (node == null) continue;
            ValidateNode(
                node,
                nodeSet,
                skillsById,
                heroinesById,
                trainingIds,
                defaultUnlockedTrainingIds,
                categoryIds,
                enemyIds,
                report);
        }

        ValidateCycles(nodes, nodeSet, report);
        ValidateTreePositions(nodes, report);
        return report;
    }

    private static void ValidateAcquisitionEvents(
        SkillTreeNodeData[] nodes,
        HeroineProfileData[] profiles,
        SkillTreeValidationReport report)
    {
        Dictionary<string, HeroineProfileData> profilesById = new Dictionary<string, HeroineProfileData>(StringComparer.Ordinal);
        foreach (HeroineProfileData profile in profiles ?? new HeroineProfileData[0])
        {
            if (profile != null && !string.IsNullOrWhiteSpace(profile.heroineId) && !profilesById.ContainsKey(profile.heroineId))
            {
                profilesById.Add(profile.heroineId, profile);
            }
        }

        foreach (SkillTreeNodeData node in nodes ?? new SkillTreeNodeData[0])
        {
            if (node == null || string.IsNullOrWhiteSpace(node.unlockEventId))
            {
                continue;
            }

            string label = GetNodeLabel(node);
            if (string.IsNullOrWhiteSpace(node.unlockEventHeroineId))
            {
                report.Warn(label + " の取得時イベントには unlockEventHeroineId を指定してください。");
                continue;
            }

            if (!profilesById.TryGetValue(node.unlockEventHeroineId, out HeroineProfileData profile))
            {
                continue;
            }

            GameEventData[] events = Resources.LoadAll<GameEventData>(profile.gameEventResourcePath);
            GameEventData target = Array.Find(events, gameEvent =>
                gameEvent != null && string.Equals(gameEvent.eventId, node.unlockEventId, StringComparison.Ordinal));
            if (target == null)
            {
                report.Warn(
                    label + " の取得時イベントが対象ヒロインのイベントパスに存在しません: " +
                    profile.gameEventResourcePath + " / eventId=" + node.unlockEventId);
                continue;
            }

            if (target.triggerType != GameEventTriggerType.Manual)
            {
                report.Warn(label + " の取得時イベントは triggerType=Manual ではありません: " + node.unlockEventId);
            }
            if (!target.showOnce)
            {
                report.Warn(label + " の取得時イベントは showOnce=true にしてください: " + node.unlockEventId);
            }
            if (!target.isEnabled)
            {
                report.Warn(label + " の取得時イベントが無効です: " + node.unlockEventId);
            }

            HashSet<string> guaranteedSkillIds = new HashSet<string>(StringComparer.Ordinal);
            CollectGuaranteedPlayerSkillIds(node, guaranteedSkillIds, new HashSet<SkillTreeNodeData>());
            foreach (string requiredSkillId in target.requiredSkillIds ?? new List<string>())
            {
                if (!string.IsNullOrWhiteSpace(requiredSkillId) && !guaranteedSkillIds.Contains(requiredSkillId))
                {
                    report.Warn(
                        label + " の取得だけではイベント必須スキルを保証できません: eventId=" +
                        node.unlockEventId + " / skillId=" + requiredSkillId);
                }
            }
        }
    }

    private static void CollectGuaranteedPlayerSkillIds(
        SkillTreeNodeData node,
        HashSet<string> skillIds,
        HashSet<SkillTreeNodeData> visited)
    {
        if (node == null || !visited.Add(node))
        {
            return;
        }

        if (node.owner == SkillTreeOwner.Player && node.skill != null && !string.IsNullOrWhiteSpace(node.skill.skillId))
        {
            skillIds.Add(node.skill.skillId);
        }

        foreach (SkillTreeNodeData prerequisite in node.prerequisiteNodes ?? new List<SkillTreeNodeData>())
        {
            CollectGuaranteedPlayerSkillIds(prerequisite, skillIds, visited);
        }
    }

    private static void ValidateNode(
        SkillTreeNodeData node,
        HashSet<SkillTreeNodeData> nodeSet,
        Dictionary<string, SkillData> skillsById,
        Dictionary<string, HeroineProfileData> heroinesById,
        HashSet<string> trainingIds,
        HashSet<string> defaultUnlockedTrainingIds,
        HashSet<string> categoryIds,
        HashSet<string> enemyIds,
        SkillTreeValidationReport report)
    {
        string label = GetNodeLabel(node);
        if (!string.IsNullOrWhiteSpace(node.unlockEventHeroineId) &&
            !heroinesById.ContainsKey(node.unlockEventHeroineId))
        {
            report.Warn(
                label + " の unlockEventHeroineId に一致するプロフィールがありません: " +
                node.unlockEventHeroineId);
        }
        if (!string.IsNullOrWhiteSpace(node.unlockEventHeroineId) &&
            string.IsNullOrWhiteSpace(node.unlockEventId))
        {
            report.Warn(label + " に対象ヒロインはありますが unlockEventId が空です。");
        }
        if (node.skillPointCost < 0)
        {
            report.Warn(label + " の skillPointCost が負数です: " + node.skillPointCost);
        }

        if (node.owner == SkillTreeOwner.Player)
        {
            if (node.skill == null &&
                !HasTrainingUnlocks(node) &&
                !HasOutfitPromptUnlock(node))
            {
                report.Warn(label + " に主人公用 SkillData、訓練解放効果、衣装確認モード解放効果のいずれも設定されていません。");
            }
            else if (node.skill != null &&
                (string.IsNullOrWhiteSpace(node.skill.skillId) ||
                    !skillsById.ContainsKey(node.skill.skillId)))
            {
                report.Warn(
                    label + " の主人公スキルが Resources/Skills に存在しません: skillId=" +
                    node.skill.skillId);
            }
        }
        else
        {
            if (node.unlocksOutfitPromptMode)
            {
                report.Warn(label + " の衣装確認モード解放効果は主人公ノードにだけ設定できます。");
            }
            ValidateHeroineSkill(node, skillsById, heroinesById, report);
        }

        if (node.unlocksOutfitPromptMode &&
            node.unlockedOutfitPromptMode == ScheduledEventOutfitPromptMode.Always)
        {
            report.Warn(label + " の衣装確認モード解放効果に Always は指定できません。");
        }

        ValidateTrainingSkillApplication(
            node,
            trainingIds,
            categoryIds,
            report);
        ValidateTrainingUnlocks(
            node,
            trainingIds,
            defaultUnlockedTrainingIds,
            report);
        ValidatePrerequisites(node, nodeSet, report);
        ValidateConditions(node, trainingIds, categoryIds, enemyIds, report);
    }

    private static void ValidateTrainingSkillApplication(
        SkillTreeNodeData node,
        HashSet<string> trainingIds,
        HashSet<string> categoryIds,
        SkillTreeValidationReport report)
    {
        SkillData skill = node != null ? node.skill : null;
        if (skill == null ||
            skill.category != SkillCategory.Training ||
            !skill.canUseInTraining)
        {
            return;
        }

        string label = GetNodeLabel(node);
        string targetId = skill.trainingApplicationTargetId;
        switch (skill.trainingApplicationScope)
        {
            case TrainingSkillApplicationScope.AllTrainings:
                if (!string.IsNullOrWhiteSpace(targetId))
                {
                    report.Warn(
                        label + " の全訓練対象スキルに不要な適用対象 ID があります: " +
                        targetId);
                }
                break;
            case TrainingSkillApplicationScope.TrainingCategory:
                if (string.IsNullOrWhiteSpace(targetId) ||
                    !categoryIds.Contains(targetId))
                {
                    report.Warn(
                        label + " の適用対象カテゴリーが存在しません: " + targetId);
                }
                break;
            case TrainingSkillApplicationScope.Training:
                if (string.IsNullOrWhiteSpace(targetId) ||
                    !trainingIds.Contains(targetId))
                {
                    report.Warn(
                        label + " の適用対象訓練が存在しません: " + targetId);
                }
                break;
            default:
                report.Warn(label + " の訓練スキル適用範囲が不正です。");
                break;
        }
    }

    private static void ValidateHeroineSkill(
        SkillTreeNodeData node,
        Dictionary<string, SkillData> skillsById,
        Dictionary<string, HeroineProfileData> heroinesById,
        SkillTreeValidationReport report)
    {
        string label = GetNodeLabel(node);
        if (!string.IsNullOrWhiteSpace(node.targetHeroineId) &&
            !heroinesById.ContainsKey(node.targetHeroineId))
        {
            report.Warn(
                label + " の targetHeroineId に一致するプロフィールがありません: " +
                node.targetHeroineId);
            return;
        }

        if (node.skill != null)
        {
            if (string.IsNullOrWhiteSpace(node.skill.skillId) ||
                !skillsById.ContainsKey(node.skill.skillId))
            {
                report.Warn(
                    label + " のヒロイン訓練スキルが Resources/Skills に存在しません: skillId=" +
                    node.skill.skillId);
            }
            else if (node.skill.category != SkillCategory.Training ||
                !node.skill.canUseInTraining)
            {
                report.Warn(label + " のヒロイン用 SkillData は訓練スキルではありません。");
            }

            if (!string.IsNullOrWhiteSpace(node.grantedHeroineSkillId))
            {
                report.Warn(
                    label + " に訓練スキルと戦闘スキルが同時に設定されています。");
            }
            return;
        }

        if (string.IsNullOrWhiteSpace(node.grantedHeroineSkillId))
        {
            if (!HasTrainingUnlocks(node))
            {
                report.Warn(label + " の grantedHeroineSkillId または訓練解放効果が空です。");
            }
            return;
        }

        foreach (KeyValuePair<string, HeroineProfileData> pair in heroinesById)
        {
            if (!string.IsNullOrWhiteSpace(node.targetHeroineId) &&
                !string.Equals(node.targetHeroineId, pair.Key, StringComparison.Ordinal))
            {
                continue;
            }

            if (!HasHeroineSkill(pair.Value, node.grantedHeroineSkillId))
            {
                report.Warn(
                    label + " のヒロインスキルがプロフィールに存在しません: heroineId=" +
                    pair.Key + " / skillId=" + node.grantedHeroineSkillId);
            }
        }
    }

    private static bool HasHeroineSkill(HeroineProfileData profile, string skillId)
    {
        if (profile == null || profile.battleSkills == null) return false;
        for (int i = 0; i < profile.battleSkills.Count; i++)
        {
            HeroineBattleSkillData skill = profile.battleSkills[i];
            if (skill != null && string.Equals(skill.skillId, skillId, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasTrainingUnlocks(SkillTreeNodeData node)
    {
        if (node == null || node.unlockedTrainingIds == null) return false;
        for (int i = 0; i < node.unlockedTrainingIds.Count; i++)
        {
            if (!string.IsNullOrWhiteSpace(node.unlockedTrainingIds[i])) return true;
        }
        return false;
    }

    private static bool HasOutfitPromptUnlock(SkillTreeNodeData node)
    {
        return node != null &&
            node.owner == SkillTreeOwner.Player &&
            node.unlocksOutfitPromptMode &&
            node.unlockedOutfitPromptMode != ScheduledEventOutfitPromptMode.Always;
    }

    private static void ValidateTrainingUnlocks(
        SkillTreeNodeData node,
        HashSet<string> trainingIds,
        HashSet<string> defaultUnlockedTrainingIds,
        SkillTreeValidationReport report)
    {
        if (node.unlockedTrainingIds == null) return;

        string label = GetNodeLabel(node);
        HashSet<string> seenIds = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < node.unlockedTrainingIds.Count; i++)
        {
            string trainingId = (node.unlockedTrainingIds[i] ?? string.Empty).Trim();
            if (trainingId.Length == 0)
            {
                report.Warn(label + " の解放対象 trainingId が空です。");
                continue;
            }
            if (!seenIds.Add(trainingId))
            {
                report.Warn(label + " の解放対象 trainingId が重複しています: " + trainingId);
            }
            if (!trainingIds.Contains(trainingId))
            {
                report.Warn(label + " の解放対象訓練が存在しません: " + trainingId);
            }
            if (defaultUnlockedTrainingIds.Contains(trainingId))
            {
                report.Warn(label + " は初期解放済みの訓練を解放対象にしています: " + trainingId);
            }

            if (node.unlockConditions == null) continue;
            for (int conditionIndex = 0;
                conditionIndex < node.unlockConditions.Count;
                conditionIndex++)
            {
                SkillTreeUnlockCondition condition = node.unlockConditions[conditionIndex];
                if (condition != null &&
                    condition.requiredValue > 0 &&
                    condition.scope == SkillTreeProgressScope.Training &&
                    string.Equals(condition.targetId, trainingId, StringComparison.Ordinal))
                {
                    report.Warn(
                        label + " は自身が解放する訓練の実績を取得条件にしているため到達不能です: " +
                        trainingId);
                }
            }
        }
    }

    private static void ValidatePrerequisites(
        SkillTreeNodeData node,
        HashSet<SkillTreeNodeData> nodeSet,
        SkillTreeValidationReport report)
    {
        if (node.prerequisiteNodes == null) return;
        HashSet<SkillTreeNodeData> prerequisites = new HashSet<SkillTreeNodeData>();
        for (int i = 0; i < node.prerequisiteNodes.Count; i++)
        {
            SkillTreeNodeData prerequisite = node.prerequisiteNodes[i];
            string label = GetNodeLabel(node);
            if (prerequisite == null)
            {
                report.Warn(label + " の前提ノードに Missing 参照があります。");
                continue;
            }

            if (prerequisite == node)
            {
                report.Warn(label + " が自分自身を前提ノードにしています。");
            }
            if (!prerequisites.Add(prerequisite))
            {
                report.Warn(
                    label + " の前提ノードが重複しています: " + GetNodeLabel(prerequisite));
            }
            if (!nodeSet.Contains(prerequisite))
            {
                report.Warn(
                    label + " の前提ノードが Resources/SkillTreeNodes にありません: " +
                    GetNodeLabel(prerequisite));
            }
            if (prerequisite.owner != node.owner)
            {
                report.Warn(
                    label + " が異なる所有者のノードを前提にしています: " +
                    GetNodeLabel(prerequisite));
            }
            else if (node.owner == SkillTreeOwner.Heroine &&
                !AreHeroineTargetsCompatible(node, prerequisite))
            {
                report.Warn(
                    label + " が別ヒロイン専用ノードを前提にしています: " +
                    GetNodeLabel(prerequisite));
            }
        }
    }

    private static bool AreHeroineTargetsCompatible(
        SkillTreeNodeData node,
        SkillTreeNodeData prerequisite)
    {
        if (string.IsNullOrWhiteSpace(node.targetHeroineId))
        {
            return string.IsNullOrWhiteSpace(prerequisite.targetHeroineId);
        }

        return string.IsNullOrWhiteSpace(prerequisite.targetHeroineId) ||
            string.Equals(
                node.targetHeroineId,
                prerequisite.targetHeroineId,
                StringComparison.Ordinal);
    }

    private static void ValidateConditions(
        SkillTreeNodeData node,
        HashSet<string> trainingIds,
        HashSet<string> categoryIds,
        HashSet<string> enemyIds,
        SkillTreeValidationReport report)
    {
        if (node.unlockConditions == null) return;
        HashSet<string> conditionKeys = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < node.unlockConditions.Count; i++)
        {
            SkillTreeUnlockCondition condition = node.unlockConditions[i];
            if (condition == null)
            {
                report.Warn(GetNodeLabel(node) + " の解放条件に null が含まれています。");
                continue;
            }

            string key = condition.conditionType + "|" + condition.scope + "|" +
                (condition.targetId ?? "");
            if (!conditionKeys.Add(key))
            {
                report.Warn(
                    GetNodeLabel(node) + " の解放条件が重複しています: " + key);
            }
            if (condition.requiredValue < 0)
            {
                report.Warn(
                    GetNodeLabel(node) + " の解放条件 requiredValue が負数です: " + key);
            }

            ValidateConditionTarget(
                node,
                condition,
                trainingIds,
                categoryIds,
                enemyIds,
                report);
        }
    }

    private static void ValidateConditionTarget(
        SkillTreeNodeData node,
        SkillTreeUnlockCondition condition,
        HashSet<string> trainingIds,
        HashSet<string> categoryIds,
        HashSet<string> enemyIds,
        SkillTreeValidationReport report)
    {
        string label = GetNodeLabel(node);
        if (condition.conditionType == SkillTreeConditionType.TrainingProficiency)
        {
            if (condition.scope != SkillTreeProgressScope.Training)
            {
                report.Warn(label + " の熟練度条件には Training scope を使用してください。");
            }
            if (!trainingIds.Contains(condition.targetId ?? ""))
            {
                report.Warn(label + " の熟練度条件に存在しない trainingId が設定されています: " + condition.targetId);
            }
            return;
        }

        if (condition.conditionType == SkillTreeConditionType.MonsterDefeatCount)
        {
            if (condition.scope != SkillTreeProgressScope.Total &&
                condition.scope != SkillTreeProgressScope.Enemy)
            {
                report.Warn(label + " の撃破条件には Total または Enemy scope を使用してください。");
            }
            else if (condition.scope == SkillTreeProgressScope.Enemy &&
                !enemyIds.Contains(condition.targetId ?? ""))
            {
                report.Warn(label + " の撃破条件に存在しない enemyId が設定されています: " + condition.targetId);
            }
            return;
        }

        if (condition.conditionType == SkillTreeConditionType.Affection ||
            condition.conditionType == SkillTreeConditionType.Day)
        {
            if (condition.scope != SkillTreeProgressScope.Total ||
                !string.IsNullOrWhiteSpace(condition.targetId))
            {
                report.Warn(label + " の好感度・日数条件には Total scope と空の targetId を使用してください。");
            }
            return;
        }

        if (condition.scope == SkillTreeProgressScope.Total)
        {
            if (!string.IsNullOrWhiteSpace(condition.targetId))
            {
                report.Warn(label + " の Total 条件では targetId を空にしてください: " + condition.targetId);
            }
        }
        else if (condition.scope == SkillTreeProgressScope.Training)
        {
            if (!trainingIds.Contains(condition.targetId ?? ""))
            {
                report.Warn(label + " の条件に存在しない trainingId が設定されています: " + condition.targetId);
            }
        }
        else if (condition.scope == SkillTreeProgressScope.TrainingCategory)
        {
            if (!categoryIds.Contains(condition.targetId ?? ""))
            {
                report.Warn(label + " の条件に存在しない trainingCategoryId が設定されています: " + condition.targetId);
            }
        }
        else
        {
            report.Warn(label + " の訓練実績条件に Enemy scope は使用できません。");
        }
    }

    private static void ValidateCycles(
        SkillTreeNodeData[] nodes,
        HashSet<SkillTreeNodeData> nodeSet,
        SkillTreeValidationReport report)
    {
        Dictionary<SkillTreeNodeData, int> states = new Dictionary<SkillTreeNodeData, int>();
        List<SkillTreeNodeData> path = new List<SkillTreeNodeData>();
        HashSet<string> reportedCycles = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < nodes.Length; i++)
        {
            SkillTreeNodeData node = nodes[i];
            if (node != null && !states.ContainsKey(node))
            {
                VisitNode(node, nodeSet, states, path, reportedCycles, report);
            }
        }
    }

    private static void VisitNode(
        SkillTreeNodeData node,
        HashSet<SkillTreeNodeData> nodeSet,
        Dictionary<SkillTreeNodeData, int> states,
        List<SkillTreeNodeData> path,
        HashSet<string> reportedCycles,
        SkillTreeValidationReport report)
    {
        states[node] = 1;
        path.Add(node);
        if (node.prerequisiteNodes != null)
        {
            for (int i = 0; i < node.prerequisiteNodes.Count; i++)
            {
                SkillTreeNodeData prerequisite = node.prerequisiteNodes[i];
                if (prerequisite == null || !nodeSet.Contains(prerequisite)) continue;

                int state;
                if (!states.TryGetValue(prerequisite, out state))
                {
                    VisitNode(prerequisite, nodeSet, states, path, reportedCycles, report);
                }
                else if (state == 1)
                {
                    int startIndex = path.IndexOf(prerequisite);
                    List<string> cycleParts = new List<string>();
                    for (int j = startIndex; j < path.Count; j++)
                    {
                        cycleParts.Add(path[j].nodeId);
                    }
                    cycleParts.Add(prerequisite.nodeId);
                    string cycle = string.Join(" -> ", cycleParts.ToArray());
                    if (reportedCycles.Add(cycle))
                    {
                        report.Warn("前提ノードが循環しています: " + cycle);
                    }
                }
            }
        }
        path.RemoveAt(path.Count - 1);
        states[node] = 2;
    }

    private static void ValidateTreePositions(
        SkillTreeNodeData[] nodes,
        SkillTreeValidationReport report)
    {
        Dictionary<string, SkillTreeNodeData> positions =
            new Dictionary<string, SkillTreeNodeData>(StringComparer.Ordinal);
        for (int i = 0; i < nodes.Length; i++)
        {
            SkillTreeNodeData node = nodes[i];
            if (node == null) continue;
            string heroineScope = node.owner == SkillTreeOwner.Heroine
                ? node.targetHeroineId ?? ""
                : "";
            string key = node.owner + "|" + heroineScope + "|" +
                node.treePosition.x + "|" + node.treePosition.y;
            SkillTreeNodeData existing;
            if (positions.TryGetValue(key, out existing))
            {
                report.Warn(
                    "同じツリー内でノード座標が重複しています: " +
                    GetNodeLabel(existing) + " / " + GetNodeLabel(node));
            }
            else
            {
                positions.Add(key, node);
            }
        }
    }

    private static Dictionary<string, SkillData> BuildSkillMap(
        SkillData[] skills,
        SkillTreeValidationReport report)
    {
        Dictionary<string, SkillData> result =
            new Dictionary<string, SkillData>(StringComparer.Ordinal);
        for (int i = 0; i < skills.Length; i++)
        {
            SkillData skill = skills[i];
            if (skill == null || string.IsNullOrWhiteSpace(skill.skillId)) continue;
            if (result.ContainsKey(skill.skillId))
            {
                report.Warn("SkillData.skillId が重複しています: " + skill.skillId);
            }
            else
            {
                result.Add(skill.skillId, skill);
            }
        }
        return result;
    }

    private static Dictionary<string, HeroineProfileData> BuildHeroineMap(
        HeroineProfileData[] profiles,
        SkillTreeValidationReport report)
    {
        Dictionary<string, HeroineProfileData> result =
            new Dictionary<string, HeroineProfileData>(StringComparer.Ordinal);
        for (int i = 0; i < profiles.Length; i++)
        {
            HeroineProfileData profile = profiles[i];
            if (profile == null || string.IsNullOrWhiteSpace(profile.heroineId)) continue;
            if (result.ContainsKey(profile.heroineId))
            {
                report.Warn("HeroineProfileData.heroineId が重複しています: " + profile.heroineId);
            }
            else
            {
                result.Add(profile.heroineId, profile);
            }
        }
        return result;
    }

    private static HashSet<string> BuildTrainingIds(
        TrainingData[] trainings,
        SkillTreeValidationReport report)
    {
        HashSet<string> result = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < trainings.Length; i++)
        {
            TrainingData training = trainings[i];
            if (training == null || string.IsNullOrWhiteSpace(training.trainingId)) continue;
            if (!result.Add(training.trainingId))
            {
                report.Warn("TrainingData.trainingId が重複しています: " + training.trainingId);
            }
        }
        return result;
    }

    private static HashSet<string> BuildDefaultUnlockedTrainingIds(TrainingData[] trainings)
    {
        HashSet<string> result = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < trainings.Length; i++)
        {
            TrainingData training = trainings[i];
            if (training != null &&
                training.unlockedByDefault &&
                !string.IsNullOrWhiteSpace(training.trainingId))
            {
                result.Add(training.trainingId);
            }
        }
        return result;
    }

    private static HashSet<string> BuildTrainingCategoryIds(TrainingData[] trainings)
    {
        HashSet<string> result = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < trainings.Length; i++)
        {
            TrainingData training = trainings[i];
            if (training != null && !string.IsNullOrWhiteSpace(training.trainingCategoryId))
            {
                result.Add(training.trainingCategoryId);
            }
        }
        return result;
    }

    private static HashSet<string> BuildEnemyIds(
        EnemyData[] enemies,
        SkillTreeValidationReport report)
    {
        HashSet<string> result = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < enemies.Length; i++)
        {
            EnemyData enemy = enemies[i];
            if (enemy == null || string.IsNullOrWhiteSpace(enemy.enemyId)) continue;
            if (!result.Add(enemy.enemyId))
            {
                report.Warn("EnemyData.enemyId が重複しています: " + enemy.enemyId);
            }
        }
        return result;
    }

    private static string GetNodeLabel(SkillTreeNodeData node)
    {
        if (node == null) return "(null node)";
        return string.IsNullOrWhiteSpace(node.nodeId)
            ? node.name
            : node.nodeId + " (" + node.name + ")";
    }
}
