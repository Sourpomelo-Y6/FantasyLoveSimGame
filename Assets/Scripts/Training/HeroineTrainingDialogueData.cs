using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class HeroineTrainingDialogueCandidate
{
    [TextArea(2, 4)]
    public string message;

    [Tooltip("Resources/Audio/Voice/<HeroineId>/ 以下の拡張子なし音声ID。空なら再生しません。")]
    public string voiceId;
}

[Serializable]
public class HeroineTrainingDialogueEntry
{
    public string trainingId;
    public TrainingVisualState visualState;

    [TextArea(2, 4)]
    public List<string> messages = new List<string>();

    [Tooltip("音声付き候補。1件以上あれば、従来の messages より優先して使用します。")]
    public List<HeroineTrainingDialogueCandidate> voicedMessages =
        new List<HeroineTrainingDialogueCandidate>();
}

public struct HeroineTrainingDialogueSelection
{
    public string Message { get; private set; }
    public string VoiceId { get; private set; }

    public bool HasMessage
    {
        get { return !string.IsNullOrEmpty(Message); }
    }

    public HeroineTrainingDialogueSelection(string message, string voiceId)
    {
        Message = message ?? string.Empty;
        VoiceId = voiceId ?? string.Empty;
    }
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
        return ResolveDialogue(trainingId, state, previousMessage).Message;
    }

    public HeroineTrainingDialogueSelection ResolveDialogue(
        string trainingId,
        TrainingVisualState state,
        string previousMessage)
    {
        List<HeroineTrainingDialogueSelection> candidates =
            FindVoicedMessages(trainingId, state);
        if (candidates.Count == 0)
        {
            candidates = FindVoicedMessages(string.Empty, state);
        }
        if (candidates.Count == 0)
        {
            candidates = FindMessages(trainingId, state);
        }
        if (candidates.Count == 0)
        {
            candidates = FindMessages(string.Empty, state);
        }

        return SelectDialogue(candidates, previousMessage);
    }

    private List<HeroineTrainingDialogueSelection> FindVoicedMessages(
        string trainingId,
        TrainingVisualState state)
    {
        List<HeroineTrainingDialogueSelection> candidates =
            new List<HeroineTrainingDialogueSelection>();
        if (entries == null)
        {
            return candidates;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            HeroineTrainingDialogueEntry entry = entries[i];
            if (entry == null ||
                entry.visualState != state ||
                !string.Equals(entry.trainingId ?? string.Empty, trainingId ?? string.Empty, StringComparison.Ordinal) ||
                entry.voicedMessages == null)
            {
                continue;
            }

            for (int candidateIndex = 0;
                candidateIndex < entry.voicedMessages.Count;
                candidateIndex++)
            {
                HeroineTrainingDialogueCandidate candidate =
                    entry.voicedMessages[candidateIndex];
                if (candidate != null && !string.IsNullOrWhiteSpace(candidate.message))
                {
                    candidates.Add(new HeroineTrainingDialogueSelection(
                        candidate.message.Trim(),
                        (candidate.voiceId ?? string.Empty).Trim()));
                }
            }
        }

        return candidates;
    }

    private List<HeroineTrainingDialogueSelection> FindMessages(
        string trainingId,
        TrainingVisualState state)
    {
        List<HeroineTrainingDialogueSelection> messages =
            new List<HeroineTrainingDialogueSelection>();
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
                    messages.Add(new HeroineTrainingDialogueSelection(
                        message.Trim(),
                        string.Empty));
                }
            }
        }

        return messages;
    }

    private static HeroineTrainingDialogueSelection SelectDialogue(
        List<HeroineTrainingDialogueSelection> candidates,
        string previousMessage)
    {
        if (candidates == null || candidates.Count == 0)
        {
            return new HeroineTrainingDialogueSelection();
        }

        if (candidates.Count == 1)
        {
            return candidates[0];
        }

        List<HeroineTrainingDialogueSelection> selectable = candidates.FindAll(
            candidate => !string.Equals(
                candidate.Message,
                previousMessage,
                StringComparison.Ordinal));
        List<HeroineTrainingDialogueSelection> source =
            selectable.Count > 0 ? selectable : candidates;
        return source[UnityEngine.Random.Range(0, source.Count)];
    }
}
