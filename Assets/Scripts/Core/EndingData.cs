using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EndingPageData
{
    public ScheduledEventSpeakerType speakerType = ScheduledEventSpeakerType.Heroine;
    public string speakerName;

    [TextArea(3, 6)]
    public string message;

    [Tooltip("ヒロイン立ち絵の表情ID。空なら現在の表情を維持します。")]
    public string expressionId;

    [Tooltip("回想・識別用のスチルID。画像が未完成なら空で構いません。")]
    public string stillId;
    public Sprite stillSprite;
}

[CreateAssetMenu(menuName = "LoveSim/Ending Data")]
public class EndingData : ScriptableObject
{
    public string endingId = "GoodEnding";
    public string displayName = "Good Ending";

    [TextArea]
    public string message = "好感度MAXエンドです。あなたと過ごした日々を、私は忘れません。";

    public Sprite stillSprite;

    [Header("Pages")]
    [Tooltip("1件以上あればこちらを使用します。空の場合は旧message/stillSpriteを1ページとして表示します。")]
    public List<EndingPageData> pages = new List<EndingPageData>();

    public int requiredAffection = 1000;
    public string costumeId;
    public string[] requiredShownEventIds;

    public List<EndingPageData> GetDisplayPages()
    {
        if (pages != null && pages.Count > 0)
        {
            return new List<EndingPageData>(pages);
        }

        return new List<EndingPageData>
        {
            new EndingPageData
            {
                speakerType = ScheduledEventSpeakerType.System,
                message = message ?? string.Empty,
                stillSprite = stillSprite
            }
        };
    }
}
