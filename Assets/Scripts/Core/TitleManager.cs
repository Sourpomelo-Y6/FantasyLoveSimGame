using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleManager : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button quitButton;

    [Header("Save")]
    [SerializeField] private SaveManager saveManager;

    [Header("Scene")]
    [SerializeField] private string mainSceneName = "MainScene";

    private void Start()
    {
        newGameButton.onClick.AddListener(OnClickNewGame);
        continueButton.onClick.AddListener(OnClickContinue);
        quitButton.onClick.AddListener(OnClickQuit);

        RefreshContinueButton();
    }

    private void RefreshContinueButton()
    {
        if (saveManager == null)
        {
            continueButton.interactable = false;
            return;
        }

        continueButton.interactable = saveManager.HasSaveData();
    }

    private void OnClickNewGame()
    {
        GameStartSettings.ShouldLoadOnStart = false;
        SceneManager.LoadScene(mainSceneName);
    }

    private void OnClickContinue()
    {
        ContinueFromSelectedSlot();
    }

    public void ContinueFromSelectedSlot()
    {
        GameStartSettings.ShouldLoadOnStart = true;
        SceneManager.LoadScene(mainSceneName);
    }

    public void ContinueFromSelectedSlot(int slotIndex)
    {
        SelectSaveSlot(slotIndex);

        if (!HasSaveDataInSlot(GameStartSettings.SelectedSaveSlotIndex))
        {
            RefreshContinueButton();
            return;
        }

        ContinueFromSelectedSlot();
    }

    public void SelectSaveSlot(int slotIndex)
    {
        if (saveManager == null)
        {
            return;
        }

        saveManager.SetCurrentSlotIndex(slotIndex);
        GameStartSettings.SelectedSaveSlotIndex = saveManager.CurrentSlotIndex;
        RefreshContinueButton();
    }

    public bool HasSaveDataInSlot(int slotIndex)
    {
        return saveManager != null && saveManager.HasSaveData(slotIndex);
    }

    public int GetSaveSlotCount()
    {
        return saveManager != null ? saveManager.SaveSlotCount : 0;
    }

    private void OnClickQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
