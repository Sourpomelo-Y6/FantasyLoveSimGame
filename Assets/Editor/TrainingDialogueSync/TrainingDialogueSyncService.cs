using System;
using System.Collections.Generic;
using System.Linq;

namespace FantasyLoveSim.EditorTools
{
    public sealed class TrainingDialogueSyncItem
    {
        public string TrainingId { get; set; }
        public string VisualState { get; set; }
        public List<string> Messages { get; set; } = new List<string>();
        public List<TrainingDialogueVoiceSyncItem> VoicedMessages { get; set; } =
            new List<TrainingDialogueVoiceSyncItem>();
        public bool ReplaceVoicedMessages { get; set; }
    }

    public sealed class TrainingDialogueVoiceSyncItem
    {
        public string Message { get; set; }
        public string VoiceId { get; set; }
    }

    public static class TrainingDialogueSyncService
    {
        private static readonly HashSet<string> KnownVisualStates = new HashSet<string>(StringComparer.Ordinal)
        {
            "SelectedBeforeFirstStep",
            "SelectedAfterFirstStep",
            "PlayerLpConsumed",
            "HeroineLpConsumed",
            "SimultaneousLpConsumed"
        };

        public static bool ValidateImportHeader(
            int schemaVersion,
            int supportedSchemaVersion,
            string exportedHeroineId,
            string expectedHeroineId,
            Action<string> warn)
        {
            if (schemaVersion != supportedSchemaVersion)
            {
                warn?.Invoke("未対応のtraining_dialogues schemaVersionのためスキップしました: " + schemaVersion);
                return false;
            }
            if (!string.IsNullOrWhiteSpace(exportedHeroineId) &&
                !string.Equals(exportedHeroineId, expectedHeroineId, StringComparison.Ordinal))
            {
                warn?.Invoke("training_dialogues_export.json のheroineIdが一致しないためスキップしました。");
                return false;
            }
            return true;
        }

        public static List<TrainingDialogueSyncItem> BuildImportItems(
            IEnumerable<TrainingDialogueSyncItem> sourceItems,
            ISet<string> knownTrainingIds,
            Action<string> warn)
        {
            List<TrainingDialogueSyncItem> result = new List<TrainingDialogueSyncItem>();
            HashSet<string> keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (TrainingDialogueSyncItem source in sourceItems ?? Enumerable.Empty<TrainingDialogueSyncItem>())
            {
                string trainingId = (source?.TrainingId ?? string.Empty).Trim();
                string visualState = (source?.VisualState ?? string.Empty).Trim();
                if (source == null || visualState.Length == 0)
                {
                    warn?.Invoke("visualStateが空の訓練セリフをスキップしました。");
                    continue;
                }
                if (trainingId.Length > 0 && (knownTrainingIds == null || !knownTrainingIds.Contains(trainingId)))
                {
                    warn?.Invoke("存在しないtrainingIdの訓練セリフをスキップしました: " + trainingId);
                    continue;
                }
                if (!KnownVisualStates.Contains(visualState))
                {
                    warn?.Invoke("未知のvisualStateをスキップしました: " + visualState);
                    continue;
                }

                string key = trainingId + "\n" + visualState;
                if (!keys.Add(key))
                {
                    warn?.Invoke("重複した訓練セリフをスキップしました: " + trainingId + " / " + visualState);
                    continue;
                }

                List<string> messages = NormalizeMessages(source.Messages);
                List<TrainingDialogueVoiceSyncItem> voicedMessages =
                    NormalizeVoicedMessages(source.VoicedMessages);
                if (messages.Count == 0 && voicedMessages.Count == 0)
                {
                    warn?.Invoke("セリフ候補が空の項目をスキップしました: " + trainingId + " / " + visualState);
                    continue;
                }
                result.Add(new TrainingDialogueSyncItem
                {
                    TrainingId = trainingId,
                    VisualState = visualState,
                    Messages = messages,
                    VoicedMessages = voicedMessages,
                    ReplaceVoicedMessages = source.ReplaceVoicedMessages
                });
            }
            return result;
        }

        public static List<TrainingDialogueSyncItem> BuildExportItems(
            IEnumerable<TrainingDialogueSyncItem> sourceItems,
            Action<string> warn)
        {
            List<TrainingDialogueSyncItem> result = new List<TrainingDialogueSyncItem>();
            Dictionary<string, TrainingDialogueSyncItem> itemsByKey =
                new Dictionary<string, TrainingDialogueSyncItem>(StringComparer.Ordinal);
            foreach (TrainingDialogueSyncItem source in sourceItems ?? Enumerable.Empty<TrainingDialogueSyncItem>())
            {
                if (source == null)
                {
                    continue;
                }
                string trainingId = (source.TrainingId ?? string.Empty).Trim();
                string visualState = (source.VisualState ?? string.Empty).Trim();
                string key = trainingId + "\n" + visualState;
                if (!itemsByKey.TryGetValue(key, out TrainingDialogueSyncItem target))
                {
                    target = new TrainingDialogueSyncItem
                    {
                        TrainingId = trainingId,
                        VisualState = visualState
                    };
                    itemsByKey.Add(key, target);
                    result.Add(target);
                }
                else
                {
                    warn?.Invoke("重複した訓練セリフ枠を統合しました: " + trainingId + " / " + visualState);
                }

                foreach (string message in NormalizeMessages(source.Messages))
                {
                    if (!target.Messages.Contains(message))
                    {
                        target.Messages.Add(message);
                    }
                }
                foreach (TrainingDialogueVoiceSyncItem candidate in
                    NormalizeVoicedMessages(source.VoicedMessages))
                {
                    TrainingDialogueVoiceSyncItem existing =
                        target.VoicedMessages.FirstOrDefault(value =>
                            string.Equals(value.Message, candidate.Message, StringComparison.Ordinal));
                    if (existing == null)
                    {
                        target.VoicedMessages.Add(candidate);
                    }
                    else if (!string.Equals(
                        existing.VoiceId,
                        candidate.VoiceId,
                        StringComparison.Ordinal))
                    {
                        warn?.Invoke(
                            "同じ本文に異なるvoiceIdが設定されています。先の設定を使用します: " +
                            trainingId + " / " + visualState + " / " + candidate.Message);
                    }
                }
            }

            result.RemoveAll(item =>
            {
                if (item.Messages.Count > 0 || item.VoicedMessages.Count > 0)
                {
                    return false;
                }
                warn?.Invoke("セリフ候補が空の訓練セリフ枠をスキップしました: " + item.TrainingId + " / " + item.VisualState);
                return true;
            });
            return result;
        }

        private static List<string> NormalizeMessages(IEnumerable<string> messages)
        {
            return (messages ?? Enumerable.Empty<string>())
                .Where(message => !string.IsNullOrWhiteSpace(message))
                .Select(message => message.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToList();
        }

        private static List<TrainingDialogueVoiceSyncItem> NormalizeVoicedMessages(
            IEnumerable<TrainingDialogueVoiceSyncItem> candidates)
        {
            List<TrainingDialogueVoiceSyncItem> result =
                new List<TrainingDialogueVoiceSyncItem>();
            foreach (TrainingDialogueVoiceSyncItem candidate in
                candidates ?? Enumerable.Empty<TrainingDialogueVoiceSyncItem>())
            {
                string message = (candidate?.Message ?? string.Empty).Trim();
                if (message.Length == 0 ||
                    result.Any(value =>
                        string.Equals(value.Message, message, StringComparison.Ordinal)))
                {
                    continue;
                }
                result.Add(new TrainingDialogueVoiceSyncItem
                {
                    Message = message,
                    VoiceId = (candidate.VoiceId ?? string.Empty).Trim()
                });
            }
            return result;
        }
    }
}
