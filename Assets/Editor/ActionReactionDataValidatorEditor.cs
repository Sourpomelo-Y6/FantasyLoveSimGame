using UnityEditor;

public static class ActionReactionDataValidatorEditor
{
    private const string MenuPath = "FantasyLoveSim/Validation/Action Reaction Data";

    [MenuItem(MenuPath)]
    public static void ValidateActionReactionData()
    {
        ActionReactionValidationReport report = ActionReactionDataValidator.ValidateProjectAssets();
        report.Log();
        EditorUtility.DisplayDialog(
            "Action Reaction Data Validation",
            report.CreateSummary() +
            (report.IsValid
                ? "\n\nNo problems found."
                : "\n\nSee Console for details. Double-click a warning to select its asset."),
            "OK");
    }
}
