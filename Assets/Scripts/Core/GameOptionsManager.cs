using System;
using System.IO;
using UnityEngine;

[Serializable]
public sealed class GameOptionsData
{
    public const int CurrentVersion = 3;

    public int version = CurrentVersion;
    public bool dialogueWindowClickAdvance = true;
    [Range(0f, 1f)] public float bgmVolume = 1f;
    public bool bgmMuted;
    [Range(0f, 1f)] public float seVolume = 1f;
    public bool seMuted;
    [Range(0f, 1f)] public float voiceVolume = 1f;
    public bool voiceMuted;
    public bool voiceAutoPlay = true;
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

    public static float BgmVolume => GetCurrent().bgmVolume;
    public static bool BgmMuted => GetCurrent().bgmMuted;
    public static float SeVolume => GetCurrent().seVolume;
    public static bool SeMuted => GetCurrent().seMuted;
    public static float VoiceVolume => GetCurrent().voiceVolume;
    public static bool VoiceMuted => GetCurrent().voiceMuted;
    public static bool VoiceAutoPlay => GetCurrent().voiceAutoPlay;

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
        return TryApplyUpdatedData(updated, out message);
    }

    public static bool SetBgmVolume(float volume, out string message)
    {
        GameOptionsData updated = Clone(GetCurrent());
        updated.bgmVolume = Mathf.Clamp01(volume);
        return TryApplyUpdatedData(updated, out message);
    }

    public static bool SetBgmMuted(bool muted, out string message)
    {
        GameOptionsData updated = Clone(GetCurrent());
        updated.bgmMuted = muted;
        return TryApplyUpdatedData(updated, out message);
    }

    public static bool SetSeVolume(float volume, out string message)
    {
        GameOptionsData updated = Clone(GetCurrent());
        updated.seVolume = Mathf.Clamp01(volume);
        return TryApplyUpdatedData(updated, out message);
    }

    public static bool SetSeMuted(bool muted, out string message)
    {
        GameOptionsData updated = Clone(GetCurrent());
        updated.seMuted = muted;
        return TryApplyUpdatedData(updated, out message);
    }

    public static bool SetVoiceVolume(float volume, out string message)
    {
        GameOptionsData updated = Clone(GetCurrent());
        updated.voiceVolume = Mathf.Clamp01(volume);
        return TryApplyUpdatedData(updated, out message);
    }

    public static bool SetVoiceMuted(bool muted, out string message)
    {
        GameOptionsData updated = Clone(GetCurrent());
        updated.voiceMuted = muted;
        return TryApplyUpdatedData(updated, out message);
    }

    public static bool SetVoiceAutoPlay(bool enabled, out string message)
    {
        GameOptionsData updated = Clone(GetCurrent());
        updated.voiceAutoPlay = enabled;
        return TryApplyUpdatedData(updated, out message);
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

            if (loaded.version == 1)
            {
                // version 1には音量項目がないため、無音の0ではなく既定音量へ移行する。
                loaded.bgmVolume = 1f;
                loaded.seVolume = 1f;
                loaded.bgmMuted = false;
                loaded.seMuted = false;
            }

            if (loaded.version < 3)
            {
                // version 2以前にはボイス項目がないため、安全な既定値を補完する。
                loaded.voiceVolume = 1f;
                loaded.voiceMuted = false;
                loaded.voiceAutoPlay = true;
            }

            loaded.version = GameOptionsData.CurrentVersion;
            Normalize(loaded);
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
            Normalize(data);
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

    private static bool TryApplyUpdatedData(GameOptionsData updated, out string message)
    {
        Normalize(updated);
        if (!TrySaveToPath(updated, FilePath, out message))
        {
            return false;
        }

        current = updated;
        AudioManager.ApplyCurrentOptionsIfAvailable();
        return true;
    }

    private static void Normalize(GameOptionsData data)
    {
        if (data == null)
        {
            return;
        }

        data.bgmVolume = Mathf.Clamp01(data.bgmVolume);
        data.seVolume = Mathf.Clamp01(data.seVolume);
        data.voiceVolume = Mathf.Clamp01(data.voiceVolume);
    }
}
