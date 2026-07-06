using UnityEngine;

[CreateAssetMenu(menuName = "LoveSim/Battle Panel Result Message Data")]
public class BattlePanelResultMessageData : ScriptableObject
{
    public BattlePanelResultMessageType resultType = BattlePanelResultMessageType.Default;
    [TextArea(1, 3)] public string message;
}
