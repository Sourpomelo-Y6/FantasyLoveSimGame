using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public sealed class AudioAssetValidationEntry
{
    public string Category { get; private set; }
    public string LogicalId { get; private set; }
    public string ResourcePath { get; private set; }

    public AudioAssetValidationEntry(string category, string logicalId, string resourcePath)
    {
        Category = category;
        LogicalId = logicalId;
        ResourcePath = resourcePath;
    }
}

public sealed class AudioAssetValidationReport
{
    private readonly List<AudioAssetValidationEntry> missing =
        new List<AudioAssetValidationEntry>();

    public int CheckedCount { get; internal set; }
    public int FoundCount => CheckedCount - missing.Count;
    public int MissingCount => missing.Count;
    public bool IsComplete => MissingCount == 0;
    public IReadOnlyList<AudioAssetValidationEntry> Missing => missing;

    internal void AddMissing(AudioAssetValidationEntry entry)
    {
        missing.Add(entry);
    }

    public string CreateSummary()
    {
        return "Audio asset validation: checked=" + CheckedCount +
            " / found=" + FoundCount +
            " / missing=" + MissingCount;
    }

    public void Log()
    {
        string summary = "[AudioAssetValidation] " + CreateSummary();
        if (IsComplete) Debug.Log(summary);
        else Debug.LogWarning(summary);

        foreach (AudioAssetValidationEntry entry in missing)
        {
            Debug.LogWarning(
                "[AudioAssetValidation] " + entry.Category + "「" +
                entry.LogicalId + "」が見つかりません。配置先例: Assets/Resources/" +
                entry.ResourcePath + ".ogg");
        }
    }
}

public static class AudioAssetValidator
{
    private const string AudioRoot = "Assets/Resources/Audio";

    private static readonly AudioAssetValidationEntry[] Requirements =
    {
        Bgm("Title"),
        Bgm(AudioManager.MainBgmId),
        Bgm("Ending"),
        Bgm(AudioManager.BattleBgmId),
        Bgm(AudioManager.TrainingBgmId),

        Se(UiSePlayer.ConfirmSeId),
        Se(UiSePlayer.CancelSeId),
        Se(UiSePlayer.NextSeId),
        Se("UI/Error"),
        Se("Shop/PurchaseSuccess"),
        Se("Shop/PurchaseFailed"),
        Se("Skill/AcquireSuccess"),
        Se("Skill/AcquireFailed"),
        Se("Schedule/Set"),
        Se("Schedule/Cancel"),
        Se("Training/Step"),
        Se("Training/Complete"),
        Se("Training/Cancel"),
        Se("Battle/Attack"),
        Se("Battle/Defend"),
        Se("Battle/Heal"),
        Se("Battle/Skill"),
        Se("Battle/Item"),
        Se("Battle/Victory"),
        Se("Battle/Defeat"),
        Se("Battle/Escape"),
        Se("Event/Start")
    };

    public static AudioAssetValidationReport ValidateProjectAssets()
    {
        HashSet<string> availableResourcePaths = new HashSet<string>(
            AssetDatabase.FindAssets("t:AudioClip", new[] { AudioRoot })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(ToResourcePath)
                .Where(path => !string.IsNullOrEmpty(path)),
            System.StringComparer.Ordinal);

        return Validate(Requirements, availableResourcePaths);
    }

    internal static AudioAssetValidationReport Validate(
        IEnumerable<AudioAssetValidationEntry> requirements,
        IEnumerable<string> availableResourcePaths)
    {
        AudioAssetValidationReport report = new AudioAssetValidationReport();
        HashSet<string> available = new HashSet<string>(
            availableResourcePaths ?? Enumerable.Empty<string>(),
            System.StringComparer.Ordinal);

        foreach (AudioAssetValidationEntry requirement in
            requirements ?? Enumerable.Empty<AudioAssetValidationEntry>())
        {
            if (requirement == null || string.IsNullOrEmpty(requirement.ResourcePath))
            {
                continue;
            }

            report.CheckedCount++;
            if (!available.Contains(requirement.ResourcePath))
            {
                report.AddMissing(requirement);
            }
        }

        return report;
    }

    internal static string ToResourcePath(string assetPath)
    {
        const string resourcesPrefix = "Assets/Resources/";
        if (string.IsNullOrEmpty(assetPath) ||
            !assetPath.StartsWith(resourcesPrefix, System.StringComparison.Ordinal))
        {
            return string.Empty;
        }

        string withoutExtension = Path.ChangeExtension(assetPath, null);
        return withoutExtension.Substring(resourcesPrefix.Length).Replace('\\', '/');
    }

    internal static AudioAssetValidationEntry Bgm(string id)
    {
        return new AudioAssetValidationEntry(
            "BGM",
            id,
            AudioManager.BuildBgmResourcePath(id));
    }

    internal static AudioAssetValidationEntry Se(string id)
    {
        return new AudioAssetValidationEntry(
            "SE",
            id,
            AudioManager.BuildSeResourcePath(id));
    }
}
