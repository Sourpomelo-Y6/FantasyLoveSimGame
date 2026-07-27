using UnityEditor;

public static class AudioAssetValidatorEditor
{
    private const string MenuPath =
        "FantasyLoveSim/Validation/Assets/Audio Assets";

    [MenuItem(MenuPath)]
    public static void ValidateAudioAssets()
    {
        AudioAssetValidationReport report =
            AudioAssetValidator.ValidateProjectAssets();
        report.Log();

        EditorUtility.DisplayDialog(
            "Audio Asset Validation",
            report.CreateSummary() +
            (report.IsComplete
                ? "\n\nAll expected local audio assets were found."
                : "\n\nMissing audio is allowed and the game will remain silent." +
                  "\nSee Console for expected local paths."),
            "OK");
    }
}
