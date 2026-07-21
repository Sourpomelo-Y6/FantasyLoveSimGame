using UnityEditor;

public static class EndingDataValidatorEditor
{
    private const string MenuPath = "FantasyLoveSim/Validation/Data/Ending Data";

    [MenuItem(MenuPath)]
    public static void ValidateEndingData()
    {
        EndingDataValidationReport report = EndingDataValidator.ValidateProjectAssets();
        report.Log();
        EditorUtility.DisplayDialog(
            "Ending Data Validation",
            report.CreateSummary() +
            (report.IsValid
                ? "\n\nNo problems found."
                : "\n\nSee Console for details. Double-click a warning to select its asset."),
            "OK");
    }
}
