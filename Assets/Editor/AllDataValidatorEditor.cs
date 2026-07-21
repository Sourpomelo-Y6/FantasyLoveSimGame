using UnityEditor;
using UnityEngine;

public static class AllDataValidatorEditor
{
    private const string MenuPath = "FantasyLoveSim/Validation/Run All Validations";

    [MenuItem(MenuPath, false, -100)]
    public static void RunAllValidations()
    {
        AllDataValidationReport report = AllDataValidator.ValidateProjectAssets(true);
        string summary = "[AllDataValidation] " + report.CreateSummary();
        if (report.IsValid) Debug.Log(summary); else Debug.LogWarning(summary);
        EditorUtility.DisplayDialog(
            "All Data Validations",
            report.CreateDialogMessage(),
            "OK");
    }
}
