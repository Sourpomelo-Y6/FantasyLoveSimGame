using UnityEngine;

[CreateAssetMenu(menuName = "LoveSim/Heroine Profile Data")]
public class HeroineProfileData : ScriptableObject
{
    [Header("Basic")]
    public string heroineId = "DefaultHeroine";
    public string displayName = "ヒロイン";
    public Sprite defaultHeroineSprite;

    [Header("Resource Paths")]
    public string conversationResourcePath = "Conversations";
    public string gameEventResourcePath = "GameEvents";
    public string actionResourcePath = "Actions";
    public string endingResourcePath = "Endings";
}
