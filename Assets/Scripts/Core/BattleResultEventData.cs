using UnityEngine;

[CreateAssetMenu(menuName = "LoveSim/Battle Result Event Data")]
public class BattleResultEventData : ScriptableObject
{
    public BattleResultEventType battleResultEventType = BattleResultEventType.SoloVictory;
    [TextArea(2, 5)] public string message;
    public string stillId;
    public int affectionChange;
    public string[] unlockedOutfitIds;
}
