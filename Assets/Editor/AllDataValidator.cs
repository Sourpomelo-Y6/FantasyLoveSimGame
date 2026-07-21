using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class AllDataValidationItem
{
    public string Name { get; private set; }
    public int AssetCount { get; private set; }
    public int WarningCount { get; private set; }
    public bool IsValid => WarningCount == 0;

    public AllDataValidationItem(string name, int assetCount, int warningCount)
    {
        Name = name;
        AssetCount = assetCount;
        WarningCount = warningCount;
    }
}

public sealed class AllDataValidationReport
{
    private readonly List<AllDataValidationItem> items = new List<AllDataValidationItem>();

    public IReadOnlyList<AllDataValidationItem> Items => items;
    public int ValidatorCount => items.Count;
    public int AssetCount => items.Sum(item => item.AssetCount);
    public int WarningCount => items.Sum(item => item.WarningCount);
    public bool IsValid => WarningCount == 0;

    internal void Add(AllDataValidationItem item)
    {
        items.Add(item);
    }

    public string CreateSummary()
    {
        return "All data validation: validators=" + ValidatorCount +
            " / assets=" + AssetCount + " / warnings=" + WarningCount;
    }

    public string CreateDialogMessage()
    {
        List<string> lines = new List<string> { CreateSummary(), string.Empty };
        foreach (AllDataValidationItem item in items)
        {
            lines.Add(
                (item.IsValid ? "OK" : "WARNING") + "  " + item.Name +
                "  assets=" + item.AssetCount + "  warnings=" + item.WarningCount);
        }

        lines.Add(string.Empty);
        lines.Add(IsValid ? "No problems found." : "See Console for warning details.");
        return string.Join("\n", lines);
    }
}

public static class AllDataValidator
{
    public static AllDataValidationReport ValidateProjectAssets(bool logDetails)
    {
        AllDataValidationReport result = new AllDataValidationReport();

        Run(result, "Heroine Data", () =>
        {
            HeroineProjectValidationReport report = HeroineDataValidator.ValidateProjectAssets();
            if (logDetails) report.Log();
            return new AllDataValidationItem("Heroine Data", report.AssetCount, report.WarningCount);
        });
        Run(result, "Conversation Data", () =>
        {
            ConversationDataValidationReport report = ConversationDataValidator.ValidateProjectAssets();
            if (logDetails) report.Log();
            return new AllDataValidationItem("Conversation Data", report.AssetCount, report.WarningCount);
        });
        Run(result, "Action Reaction Data", () =>
        {
            ActionReactionValidationReport report = ActionReactionDataValidator.ValidateProjectAssets();
            if (logDetails) report.Log();
            return new AllDataValidationItem("Action Reaction Data", report.ActionCount, report.WarningCount);
        });
        Run(result, "Game Event Data", () =>
        {
            GameEventValidationReport report = GameEventDataValidator.ValidateResources();
            if (logDetails) report.Log();
            return new AllDataValidationItem("Game Event Data", report.EventCount, report.WarningCount);
        });
        Run(result, "Ending Data", () =>
        {
            EndingDataValidationReport report = EndingDataValidator.ValidateProjectAssets();
            if (logDetails) report.Log();
            return new AllDataValidationItem("Ending Data", report.EndingCount, report.WarningCount);
        });
        Run(result, "Skill Tree Data", () =>
        {
            SkillTreeValidationReport report = SkillTreeDataValidator.ValidateResources();
            if (logDetails) report.Log();
            return new AllDataValidationItem("Skill Tree Data", report.NodeCount, report.WarningCount);
        });
        Run(result, "Save Data", () =>
        {
            SaveDataValidationReport report = SaveDataValidator.ValidatePersistentSaveFiles();
            if (logDetails) report.Log();
            return new AllDataValidationItem("Save Data", report.FileCount, report.WarningCount);
        });
        RunGameplay(
            result,
            "Training Data",
            GameplayDataValidator.ValidateTrainingProjectAssets,
            logDetails);
        RunGameplay(
            result,
            "Enemy Data",
            GameplayDataValidator.ValidateEnemyProjectAssets,
            logDetails);
        RunGameplay(
            result,
            "Shop Data",
            GameplayDataValidator.ValidateShopProjectAssets,
            logDetails);
        return result;
    }

    private static void RunGameplay(
        AllDataValidationReport result,
        string name,
        Func<GameplayDataValidationReport> validation,
        bool logDetails)
    {
        Run(result, name, () =>
        {
            GameplayDataValidationReport report = validation();
            if (logDetails) report.Log();
            return new AllDataValidationItem(name, report.AssetCount, report.WarningCount);
        });
    }

    private static void Run(
        AllDataValidationReport result,
        string name,
        Func<AllDataValidationItem> validation)
    {
        try
        {
            result.Add(validation());
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            result.Add(new AllDataValidationItem(name, 0, 1));
        }
    }
}
