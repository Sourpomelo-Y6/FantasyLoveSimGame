using UnityEditor;
using UnityEngine;

public static class GameEventDataValidatorEditor
{
    private const string MenuPath = "FantasyLoveSim/Validation/Game Event Data";

    [MenuItem(MenuPath)]
    public static void ValidateGameEventData()
    {
        GameEventValidationReport report = GameEventDataValidator.ValidateResources();
        report.Log();
        EditorUtility.DisplayDialog(
            "Game Event Data Validation",
            report.CreateSummary() +
            (report.IsValid
                ? "\n\nNo problems found."
                : "\n\nSee Console for details."),
            "OK");
    }
}
