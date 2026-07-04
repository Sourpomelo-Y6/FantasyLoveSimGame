using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class PlayerAssetImporter
{
    private const string MenuPath = "FantasyLoveSim/Import Player Export";
    private const string PlayerAssetsJsonRelativePath = "Data/player_assets_export.json";
    private const string AssetsJsonRelativePath = "Data/assets_export.json";
    private const string PlayerResourceRoot = "Assets/Resources/Player";
    private const string DefaultPlayerId = "Player";
    private const bool OverwriteExistingImages = false;

    [MenuItem(MenuPath)]
    public static void ImportPlayerExport()
    {
        string exportFolder = EditorUtility.OpenFolderPanel("Import Player Export", "", "");
        if (string.IsNullOrEmpty(exportFolder))
        {
            return;
        }

        PlayerImportReport report = ImportPlayerExport(exportFolder);
        Debug.Log(report.CreateLogMessage());
        EditorUtility.DisplayDialog("Player Export Import", report.CreateDialogMessage(), "OK");
    }

    public static PlayerImportReport ImportPlayerExport(string exportFolder)
    {
        PlayerImportReport report = new PlayerImportReport();
        if (string.IsNullOrWhiteSpace(exportFolder) || !Directory.Exists(exportFolder))
        {
            report.errors.Add("Player export folder が見つかりません: " + exportFolder);
            return report;
        }

        string assetsJsonPath = ResolveAssetsJsonPath(exportFolder);
        if (!File.Exists(assetsJsonPath))
        {
            string[] playerFolders = Directory.GetDirectories(exportFolder);
            bool foundPlayerExport = false;
            foreach (string playerFolder in playerFolders)
            {
                string childAssetsJsonPath = ResolveAssetsJsonPath(playerFolder);
                if (!File.Exists(childAssetsJsonPath))
                {
                    continue;
                }

                foundPlayerExport = true;
                report.Merge(ImportSinglePlayerExport(playerFolder));
            }

            if (!foundPlayerExport)
            {
                report.errors.Add("player_assets_export.json が見つかりません: " + assetsJsonPath);
            }

            return report;
        }

        return ImportSinglePlayerExport(exportFolder);
    }

    private static PlayerImportReport ImportSinglePlayerExport(string exportFolder)
    {
        PlayerImportReport report = new PlayerImportReport();
        string assetsJsonPath = ResolveAssetsJsonPath(exportFolder);
        if (!File.Exists(assetsJsonPath))
        {
            report.errors.Add("player_assets_export.json が見つかりません: " + assetsJsonPath);
            return report;
        }

        PlayerAssetsExport assetsExport = ReadJson<PlayerAssetsExport>(assetsJsonPath, report);
        if (assetsExport == null)
        {
            return report;
        }

        string playerId = string.IsNullOrWhiteSpace(assetsExport.playerId)
            ? DefaultPlayerId
            : assetsExport.playerId;

        PlayerAssetCatalog catalog = LoadOrCreatePlayerAssetCatalog(playerId, report);
        catalog.playerId = playerId;
        if (catalog.assets == null)
        {
            catalog.assets = new List<PlayerAssetEntry>();
        }

        catalog.assets.Clear();
        report.importedPlayerFolderCount++;

        if (assetsExport.assets == null || assetsExport.assets.Count == 0)
        {
            report.warnings.Add("player_assets_export.json の assets が空です。Accepted 画像が export されているか確認してください。");
            EditorUtility.SetDirty(catalog);
            SaveAndRefresh();
            return report;
        }

        HashSet<string> importedAssetIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (PlayerAssetExport asset in assetsExport.assets)
        {
            ImportPlayerAsset(exportFolder, asset, importedAssetIds, catalog, report);
        }

        report.catalogAssetCount = catalog.assets.Count;
        EditorUtility.SetDirty(catalog);
        SaveAndRefresh();
        return report;
    }

    private static string ResolveAssetsJsonPath(string exportFolder)
    {
        string playerAssetsJsonPath = Path.Combine(exportFolder, PlayerAssetsJsonRelativePath);
        if (File.Exists(playerAssetsJsonPath))
        {
            return playerAssetsJsonPath;
        }

        return Path.Combine(exportFolder, AssetsJsonRelativePath);
    }

    private static T ReadJson<T>(string path, PlayerImportReport report) where T : class
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

    private static PlayerAssetCatalog LoadOrCreatePlayerAssetCatalog(string playerId, PlayerImportReport report)
    {
        EnsureFolder(PlayerResourceRoot);

        string assetPath = PlayerResourceRoot + "/PlayerAssetCatalog.asset";
        PlayerAssetCatalog catalog = AssetDatabase.LoadAssetAtPath<PlayerAssetCatalog>(assetPath);
        if (catalog != null)
        {
            return catalog;
        }

        catalog = ScriptableObject.CreateInstance<PlayerAssetCatalog>();
        catalog.playerId = playerId;
        AssetDatabase.CreateAsset(catalog, assetPath);
        report.createdCatalogCount++;
        return catalog;
    }

    private static void ImportPlayerAsset(
        string exportFolder,
        PlayerAssetExport asset,
        HashSet<string> importedAssetIds,
        PlayerAssetCatalog catalog,
        PlayerImportReport report)
    {
        if (asset == null)
        {
            report.warnings.Add("null の player asset をスキップしました。");
            return;
        }

        if (!ShouldImportAsset(asset))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(asset.assetId))
        {
            report.warnings.Add("assetId が空の player asset をスキップしました: " + asset.fileName);
            return;
        }

        if (!importedAssetIds.Add(asset.assetId))
        {
            report.warnings.Add("assetId が重複している player asset をスキップしました: " + asset.assetId);
            return;
        }

        string sourcePath = ResolveExportPath(exportFolder, asset.exportImagePath, asset.fileName);
        if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath))
        {
            report.warnings.Add("プレイヤー画像ファイルが見つからないためスキップしました: " + sourcePath);
            return;
        }

        string unityPath = ResolveUnityImagePath(asset);
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

    private static bool ShouldImportAsset(PlayerAssetExport asset)
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

    private static string ResolveUnityImagePath(PlayerAssetExport asset)
    {
        if (!string.IsNullOrWhiteSpace(asset.unityImagePath))
        {
            return asset.unityImagePath.Replace("\\", "/");
        }

        string fileName = !string.IsNullOrWhiteSpace(asset.fileName)
            ? asset.fileName
            : asset.assetId + ".png";
        return "Assets/Images/Player/Battle/" + fileName;
    }

    private static void AddCatalogEntry(
        PlayerAssetCatalog catalog,
        PlayerAssetExport asset,
        string unityPath,
        PlayerImportReport report)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(unityPath);
        if (sprite == null)
        {
            report.warnings.Add("PlayerAssetCatalog 用 Sprite を解決できませんでした: " + unityPath);
        }

        catalog.assets.Add(new PlayerAssetEntry
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

    private static void SaveAndRefresh()
    {
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}

[Serializable]
public class PlayerAssetsExport
{
    public int schemaVersion;
    public string playerId;
    public string unityImageRoot;
    public List<PlayerAssetExport> assets = new List<PlayerAssetExport>();
}

[Serializable]
public class PlayerAssetExport
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

public class PlayerImportReport
{
    public int importedPlayerFolderCount;
    public int createdCatalogCount;
    public int copiedImageCount;
    public int catalogAssetCount;
    public readonly List<string> warnings = new List<string>();
    public readonly List<string> errors = new List<string>();

    public string CreateLogMessage()
    {
        return "Player Export Import: folders=" +
            importedPlayerFolderCount +
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
            "Player Folders: " + importedPlayerFolderCount +
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

    public void Merge(PlayerImportReport other)
    {
        if (other == null)
        {
            return;
        }

        importedPlayerFolderCount += other.importedPlayerFolderCount;
        createdCatalogCount += other.createdCatalogCount;
        copiedImageCount += other.copiedImageCount;
        catalogAssetCount += other.catalogAssetCount;
        warnings.AddRange(other.warnings);
        errors.AddRange(other.errors);
    }
}
