#if UNITY_INCLUDE_TESTS
using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class VoiceIdHeroineDataSyncIntegrationTests
{
    private const string HeroineId = "__VoiceIdSyncTest";
    private const string HeroineFolder = "Assets/Resources/Heroines/" + HeroineId;
    private string importFolder;
    private string outputFolder;
    private HeroineProfileData profile;

    [SetUp]
    public void SetUp()
    {
        AssetDatabase.DeleteAsset(HeroineFolder);

        string tempRoot = Path.Combine(Path.GetFullPath("Temp"), "VoiceIdHeroineDataSyncIntegrationTests");
        if (Directory.Exists(tempRoot))
        {
            Directory.Delete(tempRoot, true);
        }

        importFolder = Path.Combine(tempRoot, "Import");
        outputFolder = Path.Combine(tempRoot, "Export");
        Directory.CreateDirectory(Path.Combine(importFolder, "Data"));
        Directory.CreateDirectory(outputFolder);

        profile = ScriptableObject.CreateInstance<HeroineProfileData>();
        profile.heroineId = HeroineId;
        profile.conversationResourcePath = "Heroines/" + HeroineId + "/Conversations";
        profile.actionResourcePath = "Heroines/" + HeroineId + "/Actions";
        profile.scheduledEventResourcePath = "Heroines/" + HeroineId + "/ScheduledEvents";
        profile.endingResourcePath = "Heroines/" + HeroineId + "/Endings";
    }

    [TearDown]
    public void TearDown()
    {
        if (profile != null)
        {
            UnityEngine.Object.DestroyImmediate(profile);
        }

        AssetDatabase.DeleteAsset(HeroineFolder);
        AssetDatabase.Refresh();

        string tempRoot = Path.Combine(Path.GetFullPath("Temp"), "VoiceIdHeroineDataSyncIntegrationTests");
        if (Directory.Exists(tempRoot))
        {
            Directory.Delete(tempRoot, true);
        }
    }

    [Test]
    public void ImportAndExport_PreservesVoiceIdsAcrossHeroineContent()
    {
        WriteImportFiles(includeVoiceIds: true);
        ImportAll();

        ConversationData conversation = LoadAsset<ConversationData>(
            HeroineFolder + "/Conversations/VoiceConversation.asset");
        Assert.That(conversation.lines, Has.Count.EqualTo(1));
        Assert.That(conversation.lines[0].voiceId, Is.EqualTo("Conversation/Line01"));

        ScheduledEventData scheduledEvent = LoadAsset<ScheduledEventData>(
            HeroineFolder + "/ScheduledEvents/VoiceSchedule.asset");
        Assert.That(scheduledEvent.preparationVoiceId, Is.EqualTo("Schedule/Prepare01"));
        Assert.That(scheduledEvent.eventVoiceId, Is.EqualTo("Schedule/Result01"));

        ActionData action = LoadAsset<ActionData>(HeroineFolder + "/Actions/Tea.asset");
        Assert.That(action.reactions, Has.Count.EqualTo(1));
        Assert.That(action.reactions[0].voiceId, Is.EqualTo("Action/TeaReaction01"));

        EndingData ending = LoadAsset<EndingData>(HeroineFolder + "/Endings/VoiceEnding.asset");
        Assert.That(ending.pages, Has.Count.EqualTo(1));
        Assert.That(ending.pages[0].voiceId, Is.EqualTo("Ending/Page01"));

        ExportAll();

        Assert.That(
            ReadJson<ConversationExport>("conversations_from_unity.json").items[0].lines[0].voiceId,
            Is.EqualTo("Conversation/Line01"));
        ScheduledEventItem exportedSchedule =
            ReadJson<ScheduledEventExport>("scheduled_events_from_unity.json").items[0];
        Assert.That(exportedSchedule.lines[0].voiceId, Is.EqualTo("Schedule/Prepare01"));
        Assert.That(exportedSchedule.lines[1].voiceId, Is.EqualTo("Schedule/Result01"));
        Assert.That(
            ReadJson<ActionExport>("actions_from_unity.json").items[0].reactions[0].resultLines[0].voiceId,
            Is.EqualTo("Action/TeaReaction01"));
        Assert.That(
            ReadJson<EndingExport>("endings_from_unity.json").items[0].lines[0].voiceId,
            Is.EqualTo("Ending/Page01"));
    }

    [Test]
    public void Import_OldJsonWithoutVoiceIds_UsesEmptyValues()
    {
        WriteImportFiles(includeVoiceIds: false);
        ImportAll();

        Assert.That(
            LoadAsset<ConversationData>(
                HeroineFolder + "/Conversations/VoiceConversation.asset").lines[0].voiceId,
            Is.Empty);
        ScheduledEventData scheduledEvent = LoadAsset<ScheduledEventData>(
            HeroineFolder + "/ScheduledEvents/VoiceSchedule.asset");
        Assert.That(scheduledEvent.preparationVoiceId, Is.Empty);
        Assert.That(scheduledEvent.eventVoiceId, Is.Empty);
        Assert.That(
            LoadAsset<ActionData>(HeroineFolder + "/Actions/Tea.asset").reactions[0].voiceId,
            Is.Empty);
        Assert.That(
            LoadAsset<EndingData>(
                HeroineFolder + "/Endings/VoiceEnding.asset").pages[0].voiceId,
            Is.Empty);
    }

    private void ImportAll()
    {
        HeroineAssetImporter.HeroineImportReport report =
            new HeroineAssetImporter.HeroineImportReport();
        HeroineAssetImporter.ImportConversations(importFolder, HeroineId, report);
        AssetDatabase.SaveAssets();
        HeroineAssetImporter.ImportScheduledEvents(importFolder, HeroineId, report);
        AssetDatabase.SaveAssets();
        HeroineAssetImporter.ImportActionReactions(importFolder, HeroineId, report);
        AssetDatabase.SaveAssets();
        HeroineAssetImporter.ImportEndings(importFolder, HeroineId, report);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private void ExportAll()
    {
        HeroineUnityDataExporter.HeroineUnityExportReport report =
            new HeroineUnityDataExporter.HeroineUnityExportReport();
        HeroineUnityDataExporter.ExportConversations(profile, outputFolder, report);
        HeroineUnityDataExporter.ExportScheduledEvents(profile, outputFolder, report);
        HeroineUnityDataExporter.ExportActions(profile, outputFolder, report);
        HeroineUnityDataExporter.ExportEndings(profile, outputFolder, report);
    }

    private void WriteImportFiles(bool includeVoiceIds)
    {
        string conversationVoice = VoiceProperty("Conversation/Line01", includeVoiceIds);
        string prepareVoice = VoiceProperty("Schedule/Prepare01", includeVoiceIds);
        string resultVoice = VoiceProperty("Schedule/Result01", includeVoiceIds);
        string actionVoice = VoiceProperty("Action/TeaReaction01", includeVoiceIds);
        string endingVoice = VoiceProperty("Ending/Page01", includeVoiceIds);

        WriteData(
            "conversations_export.json",
            "{\"schemaVersion\":1,\"heroineId\":\"" + HeroineId +
            "\",\"items\":[{\"id\":\"VoiceConversation\",\"category\":\"Daily\"," +
            "\"conditions\":{\"maxAffection\":9999},\"lines\":[{\"speaker\":\"Heroine\"," +
            "\"text\":\"会話テスト\",\"expression\":\"Smile\"" + conversationVoice + "}]}]}");
        WriteData(
            "scheduled_events_export.json",
            "{\"schemaVersion\":1,\"heroineId\":\"" + HeroineId +
            "\",\"items\":[{\"id\":\"VoiceSchedule\",\"category\":\"StayHome\"," +
            "\"scheduleType\":\"StayHome\",\"conditions\":{\"scheduleType\":\"StayHome\"}," +
            "\"lines\":[{\"speaker\":\"Schedule\",\"text\":\"準備\"" + prepareVoice + "}," +
            "{\"speaker\":\"Heroine\",\"text\":\"結果\"" + resultVoice + "}]}]}");
        WriteData(
            "action_reactions_export.json",
            "{\"schemaVersion\":1,\"heroineId\":\"" + HeroineId +
            "\",\"items\":[{\"id\":\"VoiceTeaReaction\",\"category\":\"Tea\"," +
            "\"conditions\":{\"actionId\":\"Tea\",\"maxAffection\":9999}," +
            "\"lines\":[{\"speaker\":\"Heroine\",\"text\":\"行動反応\"" + actionVoice + "}]}]}");
        WriteData(
            "endings_export.json",
            "{\"schemaVersion\":1,\"heroineId\":\"" + HeroineId +
            "\",\"items\":[{\"id\":\"VoiceEnding\",\"title\":\"Voice Ending\"," +
            "\"conditions\":{\"minAffection\":0},\"lines\":[{\"speaker\":\"Heroine\"," +
            "\"text\":\"エンディング\"" + endingVoice + "}]}]}");
    }

    private static string VoiceProperty(string voiceId, bool includeVoiceIds)
    {
        return includeVoiceIds ? ",\"voiceId\":\"" + voiceId + "\"" : string.Empty;
    }

    private void WriteData(string fileName, string json)
    {
        File.WriteAllText(Path.Combine(importFolder, "Data", fileName), json);
    }

    private T ReadJson<T>(string fileName)
    {
        return JsonUtility.FromJson<T>(File.ReadAllText(Path.Combine(outputFolder, fileName)));
    }

    private static T LoadAsset<T>(string assetPath) where T : UnityEngine.Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
        Assert.That(asset, Is.Not.Null, assetPath);
        return asset;
    }

    [Serializable]
    private sealed class ConversationExport
    {
        public List<ConversationItem> items;
    }

    [Serializable]
    private sealed class ConversationItem
    {
        public List<Line> lines;
    }

    [Serializable]
    private sealed class ScheduledEventExport
    {
        public List<ScheduledEventItem> items;
    }

    [Serializable]
    private sealed class ScheduledEventItem
    {
        public List<Line> lines;
    }

    [Serializable]
    private sealed class ActionExport
    {
        public List<ActionItem> items;
    }

    [Serializable]
    private sealed class ActionItem
    {
        public List<ActionReactionItem> reactions;
    }

    [Serializable]
    private sealed class ActionReactionItem
    {
        public List<Line> resultLines;
    }

    [Serializable]
    private sealed class EndingExport
    {
        public List<EndingItem> items;
    }

    [Serializable]
    private sealed class EndingItem
    {
        public List<Line> lines;
    }

    [Serializable]
    private sealed class Line
    {
        public string voiceId;
    }
}
#endif
