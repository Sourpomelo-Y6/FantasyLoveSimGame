using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class EnemyDataImporter
{
    private const string MenuPath = "FantasyLoveSim/Import Enemy Export";
    private const string ProfileJsonRelativePath = "Data/enemy_profile_export.json";
    private const string AssetsJsonRelativePath = "Data/enemy_assets_export.json";
    private const string EnemyResourceRoot = "Assets/Resources/Enemies";
    private const bool OverwriteExistingImages = false;

    [MenuItem(MenuPath)]
    public static void ImportEnemyExport()
    {
        string exportFolder = EditorUtility.OpenFolderPanel("Import Enemy Export", "", "");
        if (string.IsNullOrEmpty(exportFolder))
        {
            return;
        }

        EnemyImportReport report = ImportEnemyExport(exportFolder);
        Debug.Log(report.CreateLogMessage());
        EditorUtility.DisplayDialog("Enemy Export Import", report.CreateDialogMessage(), "OK");
    }

    public static EnemyImportReport ImportEnemyExport(string exportFolder)
    {
        EnemyImportReport report = new EnemyImportReport();
        if (string.IsNullOrWhiteSpace(exportFolder) || !Directory.Exists(exportFolder))
        {
            report.errors.Add("Enemy export folder が見つかりません: " + exportFolder);
            return report;
        }

        string profileJsonPath = Path.Combine(exportFolder, ProfileJsonRelativePath);
        if (!File.Exists(profileJsonPath))
        {
            string[] enemyFolders = Directory.GetDirectories(exportFolder);
            bool foundEnemyExport = false;
            foreach (string enemyFolder in enemyFolders)
            {
                string childProfileJsonPath = Path.Combine(enemyFolder, ProfileJsonRelativePath);
                if (!File.Exists(childProfileJsonPath))
                {
                    continue;
                }

                foundEnemyExport = true;
                report.Merge(ImportSingleEnemyExport(enemyFolder));
            }

            if (!foundEnemyExport)
            {
                report.errors.Add("enemy_profile_export.json が見つかりません: " + profileJsonPath);
            }

            return report;
        }

        return ImportSingleEnemyExport(exportFolder);
    }

    private static EnemyImportReport ImportSingleEnemyExport(string exportFolder)
    {
        EnemyImportReport report = new EnemyImportReport();
        string profileJsonPath = Path.Combine(exportFolder, ProfileJsonRelativePath);
        if (!File.Exists(profileJsonPath))
        {
            report.errors.Add("enemy_profile_export.json が見つかりません: " + profileJsonPath);
            return report;
        }

        EnemyProfileExport profile = ReadJson<EnemyProfileExport>(profileJsonPath, report);
        if (profile == null)
        {
            return report;
        }

        if (string.IsNullOrWhiteSpace(profile.enemyId))
        {
            report.errors.Add("enemy_profile_export.json の enemyId が空です。");
            return report;
        }

        report.importedEnemyFolderCount++;
        EnsureFolder(EnemyResourceRoot);
        EnemyData enemy = LoadOrCreateEnemyData(profile.enemyId, report);
        ApplyEnemyProfile(enemy, profile);
        EditorUtility.SetDirty(enemy);

        ImportEnemyAssets(exportFolder, profile.enemyId, report);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return report;
    }

    private static T ReadJson<T>(string path, EnemyImportReport report) where T : class
    {
        try
        {
            return JsonUtility.FromJson<T>(File.ReadAllText(path));
        }
        catch (Exception exception)
        {
            report.errors.Add(Path.GetFileName(path) + " の読み込みに失敗しました: " + exception.Message);
            return null;
        }
    }

    private static EnemyData LoadOrCreateEnemyData(string enemyId, EnemyImportReport report)
    {
        string assetPath = EnemyResourceRoot + "/" + SanitizeAssetName(enemyId) + ".asset";
        EnemyData enemy = AssetDatabase.LoadAssetAtPath<EnemyData>(assetPath);
        if (enemy != null)
        {
            report.updatedEnemyDataCount++;
            return enemy;
        }

        enemy = ScriptableObject.CreateInstance<EnemyData>();
        enemy.battleStatus = new BattleStatusData();
        AssetDatabase.CreateAsset(enemy, assetPath);
        report.createdEnemyDataCount++;
        return enemy;
    }

    private static void ApplyEnemyProfile(EnemyData enemy, EnemyProfileExport profile)
    {
        enemy.enemyId = profile.enemyId;
        enemy.displayName = !string.IsNullOrWhiteSpace(profile.displayName)
            ? profile.displayName
            : profile.enemyId;

        if (enemy.battleStatus == null)
        {
            enemy.battleStatus = new BattleStatusData();
        }

        enemy.battleStatus.Clamp();
        if (enemy.battleStatus.currentHp <= 0)
        {
            enemy.battleStatus.currentHp = enemy.battleStatus.maxHp;
        }
    }

    private static void ImportEnemyAssets(
        string exportFolder,
        string profileEnemyId,
        EnemyImportReport report)
    {
        string assetsJsonPath = Path.Combine(exportFolder, AssetsJsonRelativePath);
        if (!File.Exists(assetsJsonPath))
        {
            report.warnings.Add("enemy_assets_export.json が見つからないため、画像 import はスキップしました: " + assetsJsonPath);
            return;
        }

        EnemyAssetsExport assetsExport = ReadJson<EnemyAssetsExport>(assetsJsonPath, report);
        if (assetsExport == null)
        {
            return;
        }

        string enemyId = string.IsNullOrWhiteSpace(assetsExport.enemyId)
            ? profileEnemyId
            : assetsExport.enemyId;
        if (!string.Equals(enemyId, profileEnemyId, StringComparison.Ordinal))
        {
            report.warnings.Add(
                "enemy_profile_export.json と enemy_assets_export.json の enemyId が一致しません: " +
                profileEnemyId +
                " / " +
                enemyId);
        }

        EnemyAssetCatalog catalog = LoadOrCreateEnemyAssetCatalog(profileEnemyId, report);
        catalog.enemyId = profileEnemyId;
        if (catalog.assets == null)
        {
            catalog.assets = new List<EnemyAssetEntry>();
        }

        catalog.assets.Clear();

        if (assetsExport.assets == null || assetsExport.assets.Count == 0)
        {
            report.warnings.Add("enemy_assets_export.json の assets が空です。Accepted 画像が export されているか確認してください。");
            EditorUtility.SetDirty(catalog);
            return;
        }

        HashSet<string> importedAssetIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (EnemyAssetExport asset in assetsExport.assets)
        {
            ImportEnemyAsset(exportFolder, profileEnemyId, asset, importedAssetIds, catalog, report);
        }

        report.catalogAssetCount = catalog.assets.Count;
        EditorUtility.SetDirty(catalog);
    }

    private static EnemyAssetCatalog LoadOrCreateEnemyAssetCatalog(string enemyId, EnemyImportReport report)
    {
        string enemyResourceFolderPath = EnemyResourceRoot + "/" + SanitizeAssetName(enemyId);
        EnsureFolder(enemyResourceFolderPath);

        string assetPath = enemyResourceFolderPath + "/EnemyAssetCatalog.asset";
        EnemyAssetCatalog catalog = AssetDatabase.LoadAssetAtPath<EnemyAssetCatalog>(assetPath);
        if (catalog != null)
        {
            return catalog;
        }

        catalog = ScriptableObject.CreateInstance<EnemyAssetCatalog>();
        AssetDatabase.CreateAsset(catalog, assetPath);
        report.createdCatalogCount++;
        return catalog;
    }

    private static void ImportEnemyAsset(
        string exportFolder,
        string enemyId,
        EnemyAssetExport asset,
        HashSet<string> importedAssetIds,
        EnemyAssetCatalog catalog,
        EnemyImportReport report)
    {
        if (asset == null)
        {
            report.warnings.Add("null の enemy asset をスキップしました。");
            return;
        }

        if (!ShouldImportAsset(asset))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(asset.assetId))
        {
            report.warnings.Add("assetId が空の enemy asset をスキップしました: " + asset.fileName);
            return;
        }

        if (!importedAssetIds.Add(asset.assetId))
        {
            report.warnings.Add("assetId が重複している enemy asset をスキップしました: " + asset.assetId);
            return;
        }

        string sourcePath = ResolveExportPath(exportFolder, asset.exportImagePath, asset.fileName);
        if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath))
        {
            report.warnings.Add("敵画像ファイルが見つからないためスキップしました: " + sourcePath);
            return;
        }

        string unityPath = ResolveUnityImagePath(enemyId, asset);
        if (!IsValidUnityAssetPath(unityPath))
        {
            report.warnings.Add("unityImagePath が Assets 配下ではないためスキップしました: " + unityPath);
            return;
        }

        EnsureFolderForAssetPath(unityPath);
        if (File.Exists(unityPath) && !OverwriteExistingImages)
        {
            report.warnings.Add("既存画像があるため上書きせず参照だけ更新しました: " + unityPath);
        }
        else
        {
            File.Copy(sourcePath, unityPath, OverwriteExistingImages);
            report.copiedImageCount++;
        }

        AssetDatabase.ImportAsset(unityPath);
        EnsureSpriteImportSettings(unityPath);
        AddCatalogEntry(catalog, asset, unityPath, report);
    }

    private static bool ShouldImportAsset(EnemyAssetExport asset)
    {
        return string.IsNullOrWhiteSpace(asset.status) ||
            string.Equals(asset.status, "Accepted", StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveExportPath(string exportFolder, string exportImagePath, string fileName)
    {
        if (!string.IsNullOrWhiteSpace(exportImagePath))
        {
            return Path.Combine(exportFolder, exportImagePath).Replace("\\", "/");
        }

        if (!string.IsNullOrWhiteSpace(fileName))
        {
            return Path.Combine(exportFolder, "Images", "Battle", fileName).Replace("\\", "/");
        }

        return string.Empty;
    }

    private static string ResolveUnityImagePath(string enemyId, EnemyAssetExport asset)
    {
        if (!string.IsNullOrWhiteSpace(asset.unityImagePath))
        {
            return asset.unityImagePath.Replace("\\", "/");
        }

        string fileName = !string.IsNullOrWhiteSpace(asset.fileName)
            ? asset.fileName
            : asset.assetId + ".png";
        return "Assets/Images/Enemies/" + enemyId + "/Battle/" + fileName;
    }

    private static void AddCatalogEntry(
        EnemyAssetCatalog catalog,
        EnemyAssetExport asset,
        string unityPath,
        EnemyImportReport report)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(unityPath);
        if (sprite == null)
        {
            report.warnings.Add("EnemyAssetCatalog 用 Sprite を解決できませんでした: " + unityPath);
        }

        catalog.assets.Add(new EnemyAssetEntry
        {
            assetId = asset.assetId,
            usage = asset.usage,
            status = asset.status,
            fileName = asset.fileName,
            memo = asset.memo,
            sprite = sprite,
            unityImagePath = unityPath,
            exportPromptPath = asset.exportPromptPath
        });
    }

    private static void EnsureSpriteImportSettings(string unityPath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(unityPath) as TextureImporter;
        if (importer == null)
        {
            return;
        }

        bool changed = false;
        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            changed = true;
        }

        if (importer.spriteImportMode != SpriteImportMode.Single)
        {
            importer.spriteImportMode = SpriteImportMode.Single;
            changed = true;
        }

        if (changed)
        {
            importer.SaveAndReimport();
        }
    }

    private static bool IsValidUnityAssetPath(string assetPath)
    {
        return !string.IsNullOrWhiteSpace(assetPath) &&
            assetPath.Replace("\\", "/").StartsWith("Assets/", StringComparison.Ordinal);
    }

    private static void EnsureFolderForAssetPath(string assetPath)
    {
        string folderPath = Path.GetDirectoryName(assetPath);
        if (!string.IsNullOrWhiteSpace(folderPath))
        {
            EnsureFolder(folderPath.Replace("\\", "/"));
        }
    }

    private static void EnsureFolder(string assetFolder)
    {
        string normalized = assetFolder.Replace("\\", "/").Trim('/');
        if (AssetDatabase.IsValidFolder(normalized))
        {
            return;
        }

        string[] parts = normalized.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }

    private static string SanitizeAssetName(string value)
    {
        string fileName = value.Trim();
        char[] invalidChars = Path.GetInvalidFileNameChars();
        for (int i = 0; i < invalidChars.Length; i++)
        {
            fileName = fileName.Replace(invalidChars[i], '_');
        }

        return string.IsNullOrWhiteSpace(fileName) ? "Enemy" : fileName;
    }
}

[Serializable]
public class EnemyProfileExport
{
    public int schemaVersion;
    public string enemyId;
    public string displayName;
    public string enemyType;
    public string memo;
}

[Serializable]
public class EnemyAssetsExport
{
    public int schemaVersion;
    public string enemyId;
    public string unityImageRoot;
    public List<EnemyAssetExport> assets = new List<EnemyAssetExport>();
}

[Serializable]
public class EnemyAssetExport
{
    public string assetId;
    public string usage;
    public string status;
    public string fileName;
    public string memo;
    public string exportImagePath;
    public string exportPromptPath;
    public string unityImagePath;
}

public class EnemyImportReport
{
    public int importedEnemyFolderCount;
    public int createdEnemyDataCount;
    public int updatedEnemyDataCount;
    public int createdCatalogCount;
    public int copiedImageCount;
    public int catalogAssetCount;
    public readonly List<string> warnings = new List<string>();
    public readonly List<string> errors = new List<string>();

    public string CreateLogMessage()
    {
        return "Enemy Export Import: folders=" +
            importedEnemyFolderCount +
            " enemyCreated=" +
            createdEnemyDataCount +
            " enemyUpdated=" +
            updatedEnemyDataCount +
            " catalogAssets=" +
            catalogAssetCount +
            " copiedImages=" +
            copiedImageCount +
            " warnings=" +
            warnings.Count +
            " errors=" +
            errors.Count;
    }

    public string CreateDialogMessage()
    {
        string message =
            "Enemy Folders: " + importedEnemyFolderCount +
            "\nEnemy Created: " + createdEnemyDataCount +
            "\nEnemy Updated: " + updatedEnemyDataCount +
            "\nCatalog Assets: " + catalogAssetCount +
            "\nCopied Images: " + copiedImageCount;
        if (warnings.Count > 0)
        {
            message += "\nWarnings: " + warnings.Count;
        }

        if (errors.Count > 0)
        {
            message += "\nErrors: " + errors.Count + "\n" + string.Join("\n", errors);
        }

        return message;
    }

    public void Merge(EnemyImportReport other)
    {
        if (other == null)
        {
            return;
        }

        importedEnemyFolderCount += other.importedEnemyFolderCount;
        createdEnemyDataCount += other.createdEnemyDataCount;
        updatedEnemyDataCount += other.updatedEnemyDataCount;
        createdCatalogCount += other.createdCatalogCount;
        copiedImageCount += other.copiedImageCount;
        catalogAssetCount += other.catalogAssetCount;
        warnings.AddRange(other.warnings);
        errors.AddRange(other.errors);
    }
}
