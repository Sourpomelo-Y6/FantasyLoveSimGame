using System;
using UnityEngine;


[Serializable]
public class ConversationChoice
{
    [TextArea]
    public string choiceText;

    [TextArea]
    public string responseText;

    public int affectionChange;
}