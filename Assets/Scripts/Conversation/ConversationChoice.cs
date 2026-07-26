using System;
using UnityEngine;


[Serializable]
public class ConversationChoice
{
    [TextArea]
    public string choiceText;

    [TextArea]
    public string responseText;
    [Tooltip("選択後の返答で再生するボイスID。空なら再生しません。")]
    public string responseVoiceId;

    public int affectionChange;
}
