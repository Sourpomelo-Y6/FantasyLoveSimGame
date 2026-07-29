using UnityEngine;

[CreateAssetMenu(menuName = "LoveSim/Solo Return Reaction Data")]
public class SoloReturnReactionData : ScriptableObject
{
    public BattleResultEventType battleResultEventType =
        BattleResultEventType.SoloVictory;
    public string battleContextId;
    [TextArea(2, 5)] public string message;
    [Tooltip("Resources/Audio/Voice/<HeroineId>/ 以下の拡張子なし音声ID。")]
    public string voiceId;
    public string stillId;
    public BattleResultVisualMode visualMode = BattleResultVisualMode.Auto;
    public string expressionId;
}
