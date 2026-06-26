using System.IO;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    [SerializeField] private string saveFileName = "save.json";
    [SerializeField] private string thumbnailFileNameFormat = "save_slot_{0}_thumbnail.png";
    [SerializeField] private int saveSlotCount = 3;

    private int currentSlotIndex = 0;

    public int CurrentSlotIndex
    {
        get { return currentSlotIndex; }
    }

    public int SaveSlotCount
    {
        get { return saveSlotCount; }
    }

    private string SavePath
    {
        get
        {
            return Path.Combine(Application.persistentDataPath, saveFileName);
        }
    }

    public void SetCurrentSlotIndex(int slotIndex)
    {
        currentSlotIndex = NormalizeSlotIndex(slotIndex);
    }

    public void Save(SaveData saveData)
    {
        Save(saveData, currentSlotIndex);
    }

    public void Save(SaveData saveData, int slotIndex)
    {
        slotIndex = NormalizeSlotIndex(slotIndex);

        string json = JsonUtility.ToJson(saveData, true);
        string savePath = GetSavePath(slotIndex);

        File.WriteAllText(savePath, json);

        currentSlotIndex = slotIndex;

        Debug.Log("Saved: " + savePath);
    }

    public SaveData Load()
    {
        return Load(currentSlotIndex);
    }

    public SaveData Load(int slotIndex)
    {
        slotIndex = NormalizeSlotIndex(slotIndex);

        string savePath = GetSavePath(slotIndex);

        if (!File.Exists(savePath))
        {
            Debug.Log("Save file not found: " + savePath);
            return null;
        }

        string json = File.ReadAllText(savePath);
        SaveData saveData = JsonUtility.FromJson<SaveData>(json);

        currentSlotIndex = slotIndex;

        Debug.Log("Loaded: " + savePath);

        return saveData;
    }

    public SaveData LoadPreview(int slotIndex)
    {
        slotIndex = NormalizeSlotIndex(slotIndex);

        string savePath = GetSavePath(slotIndex);

        if (!File.Exists(savePath))
        {
            return null;
        }

        string json = File.ReadAllText(savePath);
        return JsonUtility.FromJson<SaveData>(json);
    }

    public bool HasSaveData()
    {
        return HasSaveData(currentSlotIndex);
    }

    public bool HasSaveData(int slotIndex)
    {
        slotIndex = NormalizeSlotIndex(slotIndex);

        return File.Exists(GetSavePath(slotIndex));
    }

    public void DeleteSaveData()
    {
        DeleteSaveData(currentSlotIndex);
    }

    public void DeleteSaveData(int slotIndex)
    {
        slotIndex = NormalizeSlotIndex(slotIndex);

        string savePath = GetSavePath(slotIndex);

        if (File.Exists(savePath))
        {
            File.Delete(savePath);
            Debug.Log("Deleted save file: " + savePath);
        }

        string thumbnailPath = GetThumbnailPath(slotIndex);
        if (File.Exists(thumbnailPath))
        {
            File.Delete(thumbnailPath);
            Debug.Log("Deleted save thumbnail: " + thumbnailPath);
        }
    }

    public string SaveThumbnail(Texture2D texture, int slotIndex)
    {
        if (texture == null)
        {
            return "";
        }

        slotIndex = NormalizeSlotIndex(slotIndex);
        string fileName = GetThumbnailFileName(slotIndex);
        string thumbnailPath = Path.Combine(Application.persistentDataPath, fileName);
        byte[] bytes = texture.EncodeToPNG();
        File.WriteAllBytes(thumbnailPath, bytes);
        Debug.Log("Saved thumbnail: " + thumbnailPath);
        return fileName;
    }

    public Texture2D LoadThumbnail(string thumbnailFileName)
    {
        if (string.IsNullOrWhiteSpace(thumbnailFileName))
        {
            return null;
        }

        string thumbnailPath = Path.Combine(Application.persistentDataPath, thumbnailFileName);
        if (!File.Exists(thumbnailPath))
        {
            return null;
        }

        byte[] bytes = File.ReadAllBytes(thumbnailPath);
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!texture.LoadImage(bytes))
        {
            Destroy(texture);
            return null;
        }

        return texture;
    }

    private string GetSavePath(int slotIndex)
    {
        slotIndex = NormalizeSlotIndex(slotIndex);

        if (slotIndex == 0)
        {
            return SavePath;
        }

        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(saveFileName);
        string extension = Path.GetExtension(saveFileName);
        string slotFileName = fileNameWithoutExtension + "_slot_" + slotIndex + extension;

        return Path.Combine(Application.persistentDataPath, slotFileName);
    }

    private string GetThumbnailPath(int slotIndex)
    {
        return Path.Combine(Application.persistentDataPath, GetThumbnailFileName(slotIndex));
    }

    private string GetThumbnailFileName(int slotIndex)
    {
        slotIndex = NormalizeSlotIndex(slotIndex);
        return string.Format(thumbnailFileNameFormat, slotIndex);
    }

    private bool IsValidSlotIndex(int slotIndex)
    {
        return slotIndex >= 0 && slotIndex < saveSlotCount;
    }

    private int NormalizeSlotIndex(int slotIndex)
    {
        if (IsValidSlotIndex(slotIndex))
        {
            return slotIndex;
        }

        Debug.LogWarning("Invalid save slot: " + slotIndex);
        return 0;
    }
}
