using UnityEditor;

public static class ConversationDataValidatorEditor
{
    private const string MenuPath = "FantasyLoveSim/Validation/Conversation Data";

    [MenuItem(MenuPath)]
    public static void ValidateConversationData()
    {
        ConversationDataValidationReport report = ConversationDataValidator.ValidateProjectAssets();
        report.Log();
        EditorUtility.DisplayDialog(
            "Conversation Data Validation",
            report.CreateSummary() +
            (report.IsValid
                ? "\n\nNo problems found."
                : "\n\nSee Console for details. Double-click a warning to select its asset."),
            "OK");
    }
}
