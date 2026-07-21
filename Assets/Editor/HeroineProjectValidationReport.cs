using System.Collections.Generic;
using UnityEngine;

public sealed class HeroineProjectValidationReport
{
    private readonly List<HeroineDataValidator.ValidationReport> reports =
        new List<HeroineDataValidator.ValidationReport>();

    public int ProfileCount => reports.Count;
    public int AssetCount { get; private set; }
    public int WarningCount { get; private set; }
    public bool IsValid => WarningCount == 0;
    public IReadOnlyList<HeroineDataValidator.ValidationReport> Reports => reports;

    internal void Add(HeroineDataValidator.ValidationReport report)
    {
        reports.Add(report);
        AssetCount += report.AssetCount;
        WarningCount += report.WarningCount;
    }

    public string CreateSummary()
    {
        return "Heroine data validation: profiles=" + ProfileCount +
            " / assets=" + AssetCount + " / warnings=" + WarningCount;
    }

    public void Log()
    {
        string summary = "[HeroineDataValidation] " + CreateSummary();
        if (IsValid) Debug.Log(summary); else Debug.LogWarning(summary);
        foreach (HeroineDataValidator.ValidationReport report in reports)
        {
            report.Log();
        }
    }
}
