using System.IO;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    [SerializeField] private string saveFileName = "save.json";

    private string SavePath
    {
        get
        {
            return Path.Combine(Application.persistentDataPath, saveFileName);
        }
    }

    public void Save(SaveData saveData)
    {
        string json = JsonUtility.ToJson(saveData, true);
        File.WriteAllText(SavePath, json);

        Debug.Log("Saved: " + SavePath);
    }

    public SaveData Load()
    {
        if (!File.Exists(SavePath))
        {
            Debug.Log("Save file not found: " + SavePath);
            return null;
        }

        string json = File.ReadAllText(SavePath);
        SaveData saveData = JsonUtility.FromJson<SaveData>(json);

        Debug.Log("Loaded: " + SavePath);

        return saveData;
    }

    public bool HasSaveData()
    {
        return File.Exists(SavePath);
    }

    public void DeleteSaveData()
    {
        if (File.Exists(SavePath))
        {
            File.Delete(SavePath);
            Debug.Log("Deleted save file: " + SavePath);
        }
    }
}
