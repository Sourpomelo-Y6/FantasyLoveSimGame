using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EndingManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button titleButton;
    [SerializeField] private TextMeshProUGUI endingText;

    [Header("Scene")]
    [SerializeField] private string titleSceneName = "TitleScene";

    [Header("Content")]
    [TextArea]
    [SerializeField] private string endingMessage = "好感度MAXエンドです。あなたと過ごした日々を、私は忘れません。";

    private void Start()
    {
        if (endingText != null)
        {
            endingText.text = endingMessage;
        }

        if (titleButton != null)
        {
            titleButton.onClick.AddListener(ReturnToTitle);
        }
    }

    public void ReturnToTitle()
    {
        if (string.IsNullOrEmpty(titleSceneName))
        {
            return;
        }

        SceneManager.LoadScene(titleSceneName);
    }
}
