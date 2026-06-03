public static class EndingSelectionSettings
{
    public static string SelectedEndingId { get; set; } = "";
    public static string SelectedHeroineId { get; set; } = "";
    public static string EndingResourcePath { get; set; } = "";

    public static void Clear()
    {
        SelectedEndingId = "";
        SelectedHeroineId = "";
        EndingResourcePath = "";
    }
}
