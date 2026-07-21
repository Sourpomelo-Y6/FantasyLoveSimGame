using UnityEditor;

public static class AffectionDataValidatorEditor
{
    private const string MenuPath = "FantasyLoveSim/Validation/Affection Data";

    [MenuItem(MenuPath)]
    public static void ValidateAffectionData()
    {
        AffectionDataValidationReport report = AffectionDataValidator.ValidateProjectAssets();
        report.Log();
        EditorUtility.DisplayDialog(
            "Affection Data Validation",
            report.CreateSummary() +
            (report.IsValid
                ? "\n\nNo problems found."
                : "\n\nSee Console for details. Double-click a warning to select its asset."),
            "OK");
    }
}
