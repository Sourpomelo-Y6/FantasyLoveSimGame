using UnityEngine;

[CreateAssetMenu(menuName = "LoveSim/Heroine Profile Data")]
public class HeroineProfileData : ScriptableObject
{
    [Header("Basic")]
    public string heroineId = "DefaultHeroine";
    public string displayName = "ヒロイン";
    public Sprite defaultHeroineSprite;

    [Header("Common Dialogue")]
    [TextArea(2, 4)]
    public string initialDialogueMessage = "今日は何を話しましょうか？";

    [TextArea(2, 4)]
    public string nextActionPrompt = "次は何をしましょうか？";

    [TextArea(2, 4)]
    public string morningGreeting = "おはようございます。今日もよろしくお願いしますね。";

    [TextArea(2, 4)]
    public string goodNightGreeting = "もう夜も遅いですね。おやすみなさい。また明日。";

    [TextArea(2, 4)]
    public string gameStartFallbackMessage = "新しい物語が始まります。";

    [TextArea(2, 4)]
    public string gameStartFollowUpMessage = "今日は何を話しましょうか？";

    [Header("Resource Paths")]
    public string conversationResourcePath = "Conversations";
    public string gameEventResourcePath = "GameEvents";
    public string actionResourcePath = "Actions";
    public string scheduledEventResourcePath = "ScheduledEvents";
    public string endingResourcePath = "Endings";
}
