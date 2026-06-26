using UnityEngine;

[CreateAssetMenu(menuName = "LoveSim/Ending Data")]
public class EndingData : ScriptableObject
{
    public string endingId = "GoodEnding";
    public string displayName = "Good Ending";

    [TextArea]
    public string message = "好感度MAXエンドです。あなたと過ごした日々を、私は忘れません。";

    public Sprite stillSprite;
    public int requiredAffection = 100;
    public string costumeId;
    public string[] requiredShownEventIds;
}
