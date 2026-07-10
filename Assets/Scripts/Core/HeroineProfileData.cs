using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class OutfitMessageOverride
{
    public string outfitId;

    [TextArea(2, 4)]
    public string lockedMessage;

    [TextArea(2, 4)]
    public string changedMessage;
}

[Serializable]
public class OutfitReactionMessageOverride
{
    public OutfitReactionType reactionType;

    [TextArea(2, 4)]
    public string message;
}

[CreateAssetMenu(menuName = "LoveSim/Heroine Profile Data")]
public class HeroineProfileData : ScriptableObject
{
    [Header("Basic")]
    public string heroineId = "DefaultHeroine";
    public string displayName = "ヒロイン";
    public Sprite defaultHeroineSprite;

    [Header("Pronouns")]
    public string heroineFirstPerson = "私";
    public string playerSecondPerson = "あなた";

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

    [Header("Outfit Dialogue Overrides")]
    public List<OutfitMessageOverride> outfitMessageOverrides = new List<OutfitMessageOverride>();
    public List<OutfitReactionMessageOverride> outfitReactionMessageOverrides =
        new List<OutfitReactionMessageOverride>();

    [Header("Battle Skills")]
    public List<HeroineBattleSkillData> battleSkills = new List<HeroineBattleSkillData>();

    public List<HeroineBattleSkillData> GetBattleSkills()
    {
        return battleSkills != null
            ? new List<HeroineBattleSkillData>(battleSkills)
            : new List<HeroineBattleSkillData>();
    }

    [Header("Resource Paths")]
    public string conversationResourcePath = "Conversations";
    public string gameEventResourcePath = "GameEvents";
    public string actionResourcePath = "Actions";
    public string scheduledEventResourcePath = "ScheduledEvents";
    public string battleResultEventResourcePath = "BattleResultEvents";
    public string battlePanelResultMessageResourcePath = "BattlePanelResultMessages";
    public string endingResourcePath = "Endings";
}
