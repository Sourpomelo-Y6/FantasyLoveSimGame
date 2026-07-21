using UnityEditor;

public static class GameplayDataValidatorEditor
{
    [MenuItem("FantasyLoveSim/Validation/Training Data")]
    public static void ValidateTrainingData()
    {
        Show("Training Data Validation", GameplayDataValidator.ValidateTrainingProjectAssets());
    }

    [MenuItem("FantasyLoveSim/Validation/Enemy Data")]
    public static void ValidateEnemyData()
    {
        Show("Enemy Data Validation", GameplayDataValidator.ValidateEnemyProjectAssets());
    }

    [MenuItem("FantasyLoveSim/Validation/Shop Data")]
    public static void ValidateShopData()
    {
        Show("Shop Data Validation", GameplayDataValidator.ValidateShopProjectAssets());
    }

    private static void Show(string title, GameplayDataValidationReport report)
    {
        report.Log();
        EditorUtility.DisplayDialog(
            title,
            report.CreateSummary() +
            (report.IsValid
                ? "\n\nNo problems found."
                : "\n\nSee Console for details. Double-click a warning to select its asset."),
            "OK");
    }
}
