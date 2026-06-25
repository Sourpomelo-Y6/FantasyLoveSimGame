using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EndingManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button titleButton;
    [SerializeField] private TextMeshProUGUI endingText;
    [SerializeField] private Image stillImage;

    [Header("Scene")]
    [SerializeField] private string titleSceneName = "TitleScene";

    [Header("Ending Data")]
    [SerializeField] private string endingResourcePath = "Endings";
    [SerializeField] private string defaultEndingId = "GoodEnding";

    [Header("Content")]
    [TextArea]
    [SerializeField] private string endingMessage = "好感度MAXエンドです。あなたと過ごした日々を、私は忘れません。";

    private void Start()
    {
        EndingData endingData = FindSelectedEndingData();

        if (endingText != null)
        {
            endingText.text = endingData != null ? endingData.message : endingMessage;
        }

        if (stillImage != null)
        {
            Sprite stillSprite = endingData != null ? endingData.stillSprite : null;
            stillImage.sprite = stillSprite;
            stillImage.gameObject.SetActive(stillSprite != null);
            stillImage.preserveAspect = true;
        }

        if (titleButton != null)
        {
            titleButton.onClick.AddListener(ReturnToTitle);
        }
    }

    private EndingData FindSelectedEndingData()
    {
        string selectedEndingId = EndingSelectionSettings.SelectedEndingId;
        if (string.IsNullOrEmpty(selectedEndingId))
        {
            selectedEndingId = defaultEndingId;
        }

        string resourcePath = EndingSelectionSettings.EndingResourcePath;
        if (string.IsNullOrEmpty(resourcePath))
        {
            resourcePath = endingResourcePath;
        }

        EndingData[] endings = Resources.LoadAll<EndingData>(resourcePath);
        Debug.Log(
            "EndingManager selection: heroineId=" +
            EndingSelectionSettings.SelectedHeroineId +
            " / selectedEndingId=" +
            selectedEndingId +
            " / resourcePath=" +
            resourcePath +
            " / loadedCount=" +
            endings.Length);

        foreach (EndingData ending in endings)
        {
            if (ending == null || string.IsNullOrEmpty(ending.endingId))
            {
                continue;
            }

            if (ending.endingId == selectedEndingId)
            {
                Debug.Log(
                    "EndingManager selected EndingData: endingId=" +
                    ending.endingId +
                    " / displayName=" +
                    ending.displayName +
                    " / hasStillSprite=" +
                    (ending.stillSprite != null));
                return ending;
            }
        }

        Debug.LogWarning("EndingData が見つかりません: " + selectedEndingId + " / Path: " + resourcePath);
        return null;
    }

    public void ReturnToTitle()
    {
        if (string.IsNullOrEmpty(titleSceneName))
        {
            return;
        }

        EndingSelectionSettings.Clear();
        SceneManager.LoadScene(titleSceneName);
    }
}
