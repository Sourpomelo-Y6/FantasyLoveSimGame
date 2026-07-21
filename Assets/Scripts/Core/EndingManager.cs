using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EndingManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button titleButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private TextMeshProUGUI speakerNameText;
    [SerializeField] private TextMeshProUGUI endingText;
    [SerializeField] private Image stillImage;
    [SerializeField] private HeroineLayeredSpriteView layeredSpriteView;

    [Header("Scene")]
    [SerializeField] private string titleSceneName = "TitleScene";

    [Header("Ending Data")]
    [SerializeField] private string endingResourcePath = "Endings";
    [SerializeField] private string defaultEndingId = "GoodEnding";

    [Header("Content")]
    [TextArea]
    [SerializeField] private string endingMessage = "好感度MAXエンドです。あなたと過ごした日々を、私は忘れません。";

    private EndingData currentEnding;
    private List<EndingPageData> currentPages = new List<EndingPageData>();
    private int currentPageIndex;
    private HeroineProfileData heroineProfile;
    private HeroineLayeredSpriteData layeredSpriteData;

    private void Start()
    {
        currentEnding = FindSelectedEndingData();
        heroineProfile = FindSelectedHeroineProfile();
        ConfigureLayeredSpriteView();
        currentPages = currentEnding != null
            ? currentEnding.GetDisplayPages()
            : CreateFallbackPages();
        currentPageIndex = 0;

        if (titleButton != null)
        {
            titleButton.onClick.AddListener(ReturnToTitle);
        }
        if (nextButton != null)
        {
            nextButton.onClick.AddListener(ShowNextPage);
        }

        ShowCurrentPage();
    }

    public void ShowNextPage()
    {
        if (currentPageIndex + 1 >= currentPages.Count)
        {
            return;
        }

        currentPageIndex++;
        ShowCurrentPage();
    }

    private void ShowCurrentPage()
    {
        EndingPageData page = currentPages.Count > 0
            ? currentPages[Mathf.Clamp(currentPageIndex, 0, currentPages.Count - 1)]
            : null;

        if (endingText != null)
        {
            endingText.text = page != null ? page.message ?? string.Empty : endingMessage;
        }
        if (speakerNameText != null)
        {
            string speakerName = page != null ? ResolveSpeakerName(page) : string.Empty;
            speakerNameText.text = speakerName;
            speakerNameText.gameObject.SetActive(!string.IsNullOrEmpty(speakerName));
        }
        if (stillImage != null)
        {
            Sprite sprite = page != null ? page.stillSprite : null;
            stillImage.sprite = sprite;
            stillImage.gameObject.SetActive(sprite != null);
            stillImage.preserveAspect = true;
        }
        if (page != null && !string.IsNullOrWhiteSpace(page.expressionId) &&
            layeredSpriteView != null && layeredSpriteData != null)
        {
            layeredSpriteView.Refresh(
                currentEnding != null ? currentEnding.costumeId : string.Empty,
                page.expressionId);
        }

        bool hasNextPage = currentPageIndex + 1 < currentPages.Count;
        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(hasNextPage);
        }
        if (titleButton != null)
        {
            titleButton.gameObject.SetActive(nextButton == null || !hasNextPage);
        }
    }

    private string ResolveSpeakerName(EndingPageData page)
    {
        if (!string.IsNullOrWhiteSpace(page.speakerName))
        {
            return page.speakerName;
        }

        switch (page.speakerType)
        {
            case ScheduledEventSpeakerType.Heroine:
                return heroineProfile != null ? heroineProfile.displayName : "ヒロイン";
            case ScheduledEventSpeakerType.Player:
                return "主人公";
            case ScheduledEventSpeakerType.Schedule:
                return "予定";
            case ScheduledEventSpeakerType.Outfit:
                return "衣装";
            default:
                return string.Empty;
        }
    }

    private List<EndingPageData> CreateFallbackPages()
    {
        return new List<EndingPageData>
        {
            new EndingPageData
            {
                speakerType = ScheduledEventSpeakerType.System,
                message = endingMessage
            }
        };
    }

    private HeroineProfileData FindSelectedHeroineProfile()
    {
        string heroineId = EndingSelectionSettings.SelectedHeroineId;
        if (string.IsNullOrWhiteSpace(heroineId))
        {
            return null;
        }

        foreach (HeroineProfileData profile in Resources.LoadAll<HeroineProfileData>("Heroines"))
        {
            if (profile != null && profile.heroineId == heroineId)
            {
                return profile;
            }
        }
        return null;
    }

    private void ConfigureLayeredSpriteView()
    {
        if (layeredSpriteView == null || heroineProfile == null)
        {
            return;
        }

        layeredSpriteData = Resources.Load<HeroineLayeredSpriteData>(
            "Heroines/" + heroineProfile.heroineId + "/HeroineLayeredSpriteData");
        if (layeredSpriteData == null)
        {
            layeredSpriteView.SetVisible(false);
            return;
        }

        layeredSpriteView.SetData(layeredSpriteData);
        layeredSpriteView.SetVisible(true);
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
