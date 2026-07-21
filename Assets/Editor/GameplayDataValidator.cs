using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public sealed class GameplayDataValidationEntry
{
    public string Message { get; private set; }
    public UnityEngine.Object Context { get; private set; }

    public GameplayDataValidationEntry(string message, UnityEngine.Object context)
    {
        Message = message;
        Context = context;
    }
}

public sealed class GameplayDataValidationReport
{
    private readonly List<GameplayDataValidationEntry> warnings =
        new List<GameplayDataValidationEntry>();

    public string DataType { get; private set; }
    public int AssetCount { get; internal set; }
    public int ReferenceCount { get; internal set; }
    public int WarningCount => warnings.Count;
    public bool IsValid => WarningCount == 0;
    public IReadOnlyList<GameplayDataValidationEntry> Warnings => warnings;

    public GameplayDataValidationReport(string dataType)
    {
        DataType = dataType;
    }

    internal void Warn(string message, UnityEngine.Object context)
    {
        warnings.Add(new GameplayDataValidationEntry(message, context));
    }

    public string CreateSummary()
    {
        return DataType + " validation: assets=" + AssetCount +
            " / references=" + ReferenceCount + " / warnings=" + WarningCount;
    }

    public void Log()
    {
        string summary = "[" + DataType + "Validation] " + CreateSummary();
        if (IsValid) Debug.Log(summary);
        else Debug.LogWarning(summary);

        foreach (GameplayDataValidationEntry warning in warnings)
        {
            Debug.LogWarning("[" + DataType + "Validation] " + warning.Message, warning.Context);
        }
    }
}

public static class GameplayDataValidator
{
    private static readonly Regex ValidIdPattern =
        new Regex("^[A-Za-z][A-Za-z0-9_]*$", RegexOptions.CultureInvariant);

    private static T[] LoadAll<T>(string[] roots) where T : UnityEngine.Object
    {
        return AssetDatabase.FindAssets("t:" + typeof(T).Name, roots)
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<T>)
            .Where(asset => asset != null)
            .ToArray();
    }

    public static GameplayDataValidationReport ValidateTrainingProjectAssets()
    {
        return ValidateTrainingForTests(
            LoadAll<TrainingData>(new[] { "Assets/Resources/Training" }),
            LoadAll<HeroineTrainingDialogueData>(new[] { "Assets/Resources/Heroines" }),
            LoadAll<HeroineTrainingImageData>(new[] { "Assets/Resources/Heroines" }),
            LoadAll<SkillTreeNodeData>(new[] { "Assets/Resources/SkillTreeNodes" }));
    }

    internal static GameplayDataValidationReport ValidateTrainingForTests(
        TrainingData[] trainings,
        HeroineTrainingDialogueData[] dialogueAssets,
        HeroineTrainingImageData[] imageAssets,
        SkillTreeNodeData[] skillTreeNodes)
    {
        GameplayDataValidationReport report = new GameplayDataValidationReport("TrainingData");
        trainings = trainings ?? new TrainingData[0];
        report.AssetCount = trainings.Length;
        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);

        foreach (TrainingData training in trainings.Where(value => value != null))
        {
            ValidateId(training.trainingId, "trainingId", training, report);
            if (!string.IsNullOrWhiteSpace(training.trainingId) && !ids.Add(training.trainingId))
                report.Warn("trainingId が重複しています: " + training.trainingId, training);
            RequireText(training.trainingCategoryId, "trainingCategoryId", training, report);
            RequireText(training.displayName, "displayName", training, report);
            RequireText(training.description, "description", training, report);
            WarnNegative(training.playerHpCostPerStep, "playerHpCostPerStep", training, report);
            WarnNegative(training.heroineHpCostPerStep, "heroineHpCostPerStep", training, report);
            if (training.initialPlayerLp < 1) report.Warn("initialPlayerLp は1以上にしてください。", training);
            if (training.initialHeroineLp < 1) report.Warn("initialHeroineLp は1以上にしてください。", training);
            WarnNegative(training.affectionRewardPerStep, "affectionRewardPerStep", training, report);
            WarnNegative(training.affectionReward, "affectionReward", training, report);
            WarnNegative(training.trainingProficiencyRewardPerStep, "trainingProficiencyRewardPerStep", training, report);
            WarnNegative(training.trainingProficiencyReward, "trainingProficiencyReward", training, report);
            WarnNegative(training.playerSkillPointReward, "playerSkillPointReward", training, report);
            WarnNegative(training.heroineSkillPointReward, "heroineSkillPointReward", training, report);
            WarnNegative(training.simultaneousKnockoutBonus, "simultaneousKnockoutBonus", training, report);
        }

        foreach (HeroineTrainingDialogueData asset in dialogueAssets ?? new HeroineTrainingDialogueData[0])
        {
            if (asset == null || asset.entries == null) continue;
            HashSet<string> keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (HeroineTrainingDialogueEntry entry in asset.entries.Where(value => value != null))
            {
                if (!string.IsNullOrWhiteSpace(entry.trainingId))
                    ValidateTrainingReference(entry.trainingId, "訓練セリフ", ids, asset, report);
                string key = (entry.trainingId ?? string.Empty) + "|" + entry.visualState;
                if (!keys.Add(key)) report.Warn("訓練セリフの状態が重複しています: " + key, asset);
                if (entry.messages == null || !entry.messages.Any(message => !string.IsNullOrWhiteSpace(message)))
                    report.Warn("訓練セリフ本文がありません: " + key, asset);
            }
        }

        foreach (HeroineTrainingImageData asset in imageAssets ?? new HeroineTrainingImageData[0])
        {
            if (asset == null || asset.entries == null) continue;
            HashSet<string> imageIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (HeroineTrainingImageEntry entry in asset.entries.Where(value => value != null))
            {
                ValidateTrainingReference(entry.trainingId, "訓練画像", ids, asset, report);
                if (!string.IsNullOrWhiteSpace(entry.trainingId) && !imageIds.Add(entry.trainingId))
                    report.Warn("訓練画像のtrainingIdが重複しています: " + entry.trainingId, asset);
            }
        }

        foreach (SkillTreeNodeData node in skillTreeNodes ?? new SkillTreeNodeData[0])
        {
            if (node == null || node.unlockedTrainingIds == null) continue;
            foreach (string trainingId in node.unlockedTrainingIds)
                ValidateTrainingReference(trainingId, "スキルツリー", ids, node, report);
        }

        return report;
    }

    private static void ValidateTrainingReference(
        string trainingId,
        string source,
        HashSet<string> knownIds,
        UnityEngine.Object context,
        GameplayDataValidationReport report)
    {
        report.ReferenceCount++;
        if (string.IsNullOrWhiteSpace(trainingId))
            report.Warn(source + "に空のtrainingIdがあります。", context);
        else if (!knownIds.Contains(trainingId))
            report.Warn(source + "が存在しないtrainingIdを参照しています: " + trainingId, context);
    }

    public static GameplayDataValidationReport ValidateEnemyProjectAssets()
    {
        return ValidateEnemyForTests(
            LoadAll<EnemyData>(new[] { "Assets/Resources/Enemies" }),
            new[] { "ForestSlime", "CaveBat", "LakeSpirit" });
    }

    internal static GameplayDataValidationReport ValidateEnemyForTests(
        EnemyData[] enemies,
        string[] requiredEnemyIds)
    {
        GameplayDataValidationReport report = new GameplayDataValidationReport("EnemyData");
        enemies = enemies ?? new EnemyData[0];
        report.AssetCount = enemies.Length;
        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);

        foreach (EnemyData enemy in enemies.Where(value => value != null))
        {
            ValidateId(enemy.enemyId, "enemyId", enemy, report);
            if (!string.IsNullOrWhiteSpace(enemy.enemyId) && !ids.Add(enemy.enemyId))
                report.Warn("enemyId が重複しています: " + enemy.enemyId, enemy);
            RequireText(enemy.displayName, "displayName", enemy, report);
            RequireText(enemy.victoryMessage, "victoryMessage", enemy, report);
            RequireText(enemy.defeatMessage, "defeatMessage", enemy, report);
            ValidateBattleStatus(enemy.battleStatus, enemy, report);
            WarnNegative(enemy.rewardMoney, "rewardMoney", enemy, report);
            WarnNegative(enemy.playerSkillPointReward, "playerSkillPointReward", enemy, report);
            WarnNegative(enemy.heroineSkillPointReward, "heroineSkillPointReward", enemy, report);

            HashSet<string> skillIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (EnemyBattleSkillData skill in enemy.battleSkills ?? new List<EnemyBattleSkillData>())
            {
                if (skill == null) { report.Warn("battleSkills にnullがあります。", enemy); continue; }
                ValidateId(skill.skillId, "battleSkills.skillId", enemy, report);
                if (!string.IsNullOrWhiteSpace(skill.skillId) && !skillIds.Add(skill.skillId))
                    report.Warn("敵スキルIDが重複しています: " + skill.skillId, enemy);
                RequireText(skill.displayName, "battleSkills.displayName", enemy, report);
                WarnNegative(skill.cost, "battleSkills.cost", enemy, report);
                WarnNegative(skill.power, "battleSkills.power", enemy, report);
                if (skill.statusDurationTurns < 1) report.Warn("敵スキルのstatusDurationTurnsは1以上にしてください。", enemy);
                if (skill.useChancePercent < 0 || skill.useChancePercent > 100)
                    report.Warn("敵スキルのuseChancePercentは0～100にしてください。", enemy);
            }
        }

        foreach (string requiredId in requiredEnemyIds ?? new string[0])
        {
            report.ReferenceCount++;
            if (!ids.Contains(requiredId))
                report.Warn("探索先に必要な敵データがありません: " + requiredId, null);
        }
        return report;
    }

    private static void ValidateBattleStatus(
        BattleStatusData status,
        EnemyData enemy,
        GameplayDataValidationReport report)
    {
        if (status == null) { report.Warn("battleStatus がnullです。", enemy); return; }
        if (status.maxHp < 1) report.Warn("battleStatus.maxHp は1以上にしてください。", enemy);
        if (status.currentHp < 0 || status.currentHp > status.maxHp)
            report.Warn("battleStatus.currentHp が0～maxHpの範囲外です。", enemy);
        if (status.maxMp < 0) report.Warn("battleStatus.maxMp が負数です。", enemy);
        if (status.currentMp < 0 || status.currentMp > status.maxMp)
            report.Warn("battleStatus.currentMp が0～maxMpの範囲外です。", enemy);
        WarnNegative(status.attack, "battleStatus.attack", enemy, report);
        WarnNegative(status.defense, "battleStatus.defense", enemy, report);
        WarnNegative(status.speed, "battleStatus.speed", enemy, report);
    }

    public static GameplayDataValidationReport ValidateShopProjectAssets()
    {
        OutfitData[] outfits = LoadAll<OutfitData>(new[] { "Assets/Resources/Outfits" });
        return ValidateShopForTests(
            LoadAll<ShopItemData>(new[] { "Assets/Resources/ShopItems" }),
            LoadAll<ShopCatalogData>(new[] { "Assets/Resources/ShopItems" }),
            outfits.Where(outfit => !string.IsNullOrWhiteSpace(outfit.outfitId))
                .Select(outfit => outfit.outfitId).ToArray());
    }

    internal static GameplayDataValidationReport ValidateShopForTests(
        ShopItemData[] items,
        ShopCatalogData[] catalogs,
        string[] outfitIds)
    {
        GameplayDataValidationReport report = new GameplayDataValidationReport("ShopData");
        items = items ?? new ShopItemData[0];
        catalogs = catalogs ?? new ShopCatalogData[0];
        report.AssetCount = items.Length + catalogs.Length;
        HashSet<string> knownItemIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> knownOutfitIds = new HashSet<string>(outfitIds ?? new string[0], StringComparer.Ordinal);

        foreach (ShopItemData item in items.Where(value => value != null))
        {
            ValidateId(item.itemId, "itemId", item, report);
            if (!string.IsNullOrWhiteSpace(item.itemId) && !knownItemIds.Add(item.itemId))
                report.Warn("itemId が重複しています: " + item.itemId, item);
            RequireText(item.displayName, "displayName", item, report);
            WarnNegative(item.price, "price", item, report);
            WarnNegative(item.mpRecoveryAmount, "mpRecoveryAmount", item, report);
            WarnNegative(item.hpRecoveryAmount, "hpRecoveryAmount", item, report);
            WarnNegative(item.requiredAffection, "requiredAffection", item, report);
            WarnNegative(item.requiredDay, "requiredDay", item, report);
            if (item.requiredAffection > AffectionDataValidator.MaximumAffection)
                report.Warn("requiredAffection が9999を超えています。", item);
            if (item.isBattleConsumable && item.mpRecoveryAmount == 0 && item.hpRecoveryAmount == 0)
                report.Warn("戦闘消耗品にHPまたはMP回復量を設定してください。", item);
        }

        foreach (ShopItemData item in items.Where(value => value != null))
        {
            ValidateStringReferences(item.requiredPurchasedItemIds, knownItemIds, item.itemId,
                "requiredPurchasedItemIds", item, report, true);
            ValidateStringReferences(item.unlockedOutfitIds, knownOutfitIds, null,
                "unlockedOutfitIds", item, report, false);
        }

        foreach (ShopCatalogData catalog in catalogs.Where(value => value != null))
        {
            HashSet<string> catalogIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (ShopItemData item in catalog.items ?? new List<ShopItemData>())
            {
                report.ReferenceCount++;
                if (item == null) { report.Warn("商品カタログにnull項目があります。", catalog); continue; }
                if (!knownItemIds.Contains(item.itemId))
                    report.Warn("商品カタログが検証対象外のitemIdを参照しています: " + item.itemId, catalog);
                if (!catalogIds.Add(item.itemId))
                    report.Warn("商品カタログ内でitemIdが重複しています: " + item.itemId, catalog);
            }
        }
        return report;
    }

    private static void ValidateStringReferences(
        IEnumerable<string> values,
        HashSet<string> knownIds,
        string selfId,
        string field,
        UnityEngine.Object context,
        GameplayDataValidationReport report,
        bool rejectSelf)
    {
        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (string value in values ?? new string[0])
        {
            report.ReferenceCount++;
            if (string.IsNullOrWhiteSpace(value)) report.Warn(field + " に空のIDがあります。", context);
            else if (!seen.Add(value)) report.Warn(field + " に重複IDがあります: " + value, context);
            else if (rejectSelf && string.Equals(value, selfId, StringComparison.Ordinal))
                report.Warn(field + " が自分自身を参照しています: " + value, context);
            else if (!knownIds.Contains(value)) report.Warn(field + " が存在しないIDを参照しています: " + value, context);
        }
    }

    private static void ValidateId(
        string value,
        string field,
        UnityEngine.Object context,
        GameplayDataValidationReport report)
    {
        if (string.IsNullOrWhiteSpace(value)) report.Warn(field + " が空です。", context);
        else if (!ValidIdPattern.IsMatch(value))
            report.Warn(field + " に使用できない文字があります: " + value, context);
    }

    private static void RequireText(
        string value,
        string field,
        UnityEngine.Object context,
        GameplayDataValidationReport report)
    {
        if (string.IsNullOrWhiteSpace(value)) report.Warn(field + " が空です。", context);
    }

    private static void WarnNegative(
        int value,
        string field,
        UnityEngine.Object context,
        GameplayDataValidationReport report)
    {
        if (value < 0) report.Warn(field + " が負数です: " + value, context);
    }
}
