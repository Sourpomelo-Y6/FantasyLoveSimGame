using UnityEditor;

public static class SaveDataValidatorEditor
{
    private const string MenuPath = "FantasyLoveSim/Validation/Data/Save Data";

    [MenuItem(MenuPath)]
    public static void ValidateSaveData()
    {
        SaveDataValidationReport report = SaveDataValidator.ValidatePersistentSaveFiles();
        report.Log();
        EditorUtility.DisplayDialog(
            "Save Data Validation",
            report.CreateSummary() +
            (report.IsValid
                ? "\n\nNo problems found."
                : "\n\nSee Console for details."),
            "OK");
    }
}
