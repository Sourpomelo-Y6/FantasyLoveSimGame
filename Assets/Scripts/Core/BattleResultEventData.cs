using UnityEngine;

public enum BattleResultVisualMode
{
    Auto,
    StillOnly,
    StillWithPortrait,
    PortraitOnly
}

[CreateAssetMenu(menuName = "LoveSim/Battle Result Event Data")]
public class BattleResultEventData : ScriptableObject
{
    public BattleResultEventType battleResultEventType = BattleResultEventType.SoloVictory;
    public string battleContextId;
    public ScheduledEventSpeakerType speakerType = ScheduledEventSpeakerType.Heroine;
    public string speakerName;
    [TextArea(2, 5)] public string message;
    [Tooltip("Resources/Audio/Voice/<HeroineId>/ 以下の拡張子なし音声ID。")]
    public string voiceId;
    public string stillId;
    public BattleResultVisualMode visualMode = BattleResultVisualMode.Auto;
    public string expressionId;
    public int affectionChange;
    public string[] unlockedOutfitIds;
}
