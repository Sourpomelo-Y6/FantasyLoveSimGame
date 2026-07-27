using UnityEngine;

[CreateAssetMenu(menuName = "LoveSim/Battle Panel Result Message Data")]
public class BattlePanelResultMessageData : ScriptableObject
{
    public BattlePanelResultMessageType resultType = BattlePanelResultMessageType.Default;
    [TextArea(1, 3)] public string message;
    [Tooltip("戦闘ログの先頭ページで再生する、拡張子なしの音声ID。")]
    public string voiceId;
}
