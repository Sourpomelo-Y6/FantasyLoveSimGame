using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class HeroineTrainingDialogueEntry
{
    public string trainingId;
    public TrainingVisualState visualState;

    [TextArea(2, 4)]
    public List<string> messages = new List<string>();
}

[CreateAssetMenu(menuName = "LoveSim/Heroine Training Dialogue Data")]
public class HeroineTrainingDialogueData : ScriptableObject
{
    public string heroineId;

    [Tooltip("trainingId が空のエントリは、その状態のヒロイン共通セリフとして扱います。")]
    public List<HeroineTrainingDialogueEntry> entries =
        new List<HeroineTrainingDialogueEntry>();

    public string ResolveMessage(
        string trainingId,
        TrainingVisualState state,
        string previousMessage)
    {
        List<string> candidates = FindMessages(trainingId, state);
        if (candidates.Count == 0)
        {
            candidates = FindMessages(string.Empty, state);
        }

        return SelectMessage(candidates, previousMessage);
    }

    private List<string> FindMessages(string trainingId, TrainingVisualState state)
    {
        List<string> messages = new List<string>();
        if (entries == null)
        {
            return messages;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            HeroineTrainingDialogueEntry entry = entries[i];
            if (entry == null ||
                entry.visualState != state ||
                !string.Equals(entry.trainingId ?? string.Empty, trainingId ?? string.Empty, StringComparison.Ordinal) ||
                entry.messages == null)
            {
                continue;
            }

            for (int messageIndex = 0; messageIndex < entry.messages.Count; messageIndex++)
            {
                string message = entry.messages[messageIndex];
                if (!string.IsNullOrWhiteSpace(message))
                {
                    messages.Add(message.Trim());
                }
            }
        }

        return messages;
    }

    private static string SelectMessage(List<string> candidates, string previousMessage)
    {
        if (candidates == null || candidates.Count == 0)
        {
            return string.Empty;
        }

        if (candidates.Count == 1)
        {
            return candidates[0];
        }

        List<string> selectable = candidates.FindAll(
            message => !string.Equals(message, previousMessage, StringComparison.Ordinal));
        List<string> source = selectable.Count > 0 ? selectable : candidates;
        return source[UnityEngine.Random.Range(0, source.Count)];
    }
}
