using UnityEditor;
using UnityEngine;

public static class SkillTreeDataValidatorEditor
{
    private const string MenuPath = "FantasyLoveSim/Validate Skill Tree Data";

    [MenuItem(MenuPath)]
    public static void ValidateSkillTreeData()
    {
        SkillTreeValidationReport report = SkillTreeDataValidator.ValidateResources();
        report.Log();
        EditorUtility.DisplayDialog(
            "Skill Tree Data Validation",
            report.CreateSummary() +
            (report.IsValid
                ? "\n\nNo problems found."
                : "\n\nSee Console for details."),
            "OK");
    }
}
