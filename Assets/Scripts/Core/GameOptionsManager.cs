using System;
using System.IO;
using UnityEngine;

[Serializable]
public sealed class GameOptionsData
{
    public const int CurrentVersion = 1;

    public int version = CurrentVersion;
    public bool dialogueWindowClickAdvance = true;
}

public static class GameOptionsManager
{
    private const string FileName = "game_options.json";
    private static GameOptionsData current;

    public static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

    public static bool DialogueWindowClickAdvanceEnabled
    {
        get { return GetCurrent().dialogueWindowClickAdvance; }
    }

    public static GameOptionsData GetCurrent()
    {
        if (current == null)
        {
            current = LoadFromPath(FilePath, true);
        }

        return current;
    }

    public static bool SetDialogueWindowClickAdvance(bool enabled, out string message)
    {
        GameOptionsData updated = Clone(GetCurrent());
        updated.dialogueWindowClickAdvance = enabled;
        if (!TrySaveToPath(updated, FilePath, out message))
        {
            return false;
        }

        current = updated;
        return true;
    }

    public static void Reload()
    {
        current = null;
    }

    public static GameOptionsData LoadFromPath(string path, bool logWarning)
    {
        GameOptionsData defaults = new GameOptionsData();
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            return defaults;
        }

        try
        {
            GameOptionsData loaded = JsonUtility.FromJson<GameOptionsData>(File.ReadAllText(path));
            if (loaded == null || loaded.version < 1 || loaded.version > GameOptionsData.CurrentVersion)
            {
                if (logWarning)
                {
                    Debug.LogWarning("ゲーム設定を読み込めませんでした。既定値を使用します。");
                }
                return defaults;
            }

            return loaded;
        }
        catch (Exception exception)
        {
            if (logWarning)
            {
                Debug.LogWarning(
                    "ゲーム設定を読み込めませんでした。既定値を使用します。\n" +
                    exception.Message);
            }
            return defaults;
        }
    }

    public static bool TrySaveToPath(
        GameOptionsData data,
        string path,
        out string message)
    {
        if (data == null || string.IsNullOrEmpty(path))
        {
            message = "ゲーム設定の保存先が不正です。";
            return false;
        }

        string temporaryPath = path + ".tmp";
        try
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            data.version = GameOptionsData.CurrentVersion;
            File.WriteAllText(temporaryPath, JsonUtility.ToJson(data, true));
            File.Copy(temporaryPath, path, true);
            File.Delete(temporaryPath);
            message = string.Empty;
            return true;
        }
        catch (Exception exception)
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
            message = "ゲーム設定を保存できませんでした: " + exception.Message;
            Debug.LogWarning(message);
            return false;
        }
    }

    private static GameOptionsData Clone(GameOptionsData source)
    {
        return JsonUtility.FromJson<GameOptionsData>(JsonUtility.ToJson(source));
    }
}
