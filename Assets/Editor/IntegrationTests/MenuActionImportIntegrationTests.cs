#if UNITY_INCLUDE_TESTS
using System;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class MenuActionImportIntegrationTests
{
    private const string HeroineId = "MenuActionImportTest";
    private const string AssetFolder = "Assets/Resources/Heroines/" + HeroineId;

    [TearDown]
    public void TearDown()
    {
        AssetDatabase.DeleteAsset(AssetFolder);
        AssetDatabase.Refresh();
    }

    [Test]
    public void ImportMenuActions_CreatesNavigationActionsAndPreservesExistingReactions()
    {
        string importFolder = Path.Combine(
            Path.GetTempPath(),
            "FantasyLoveSimMenuActionImport",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(importFolder, "Data"));

        try
        {
            File.WriteAllText(
                Path.Combine(importFolder, "Data", "actions_export.json"),
                "{\"schemaVersion\":1,\"heroineId\":\"" + HeroineId + "\",\"items\":[" +
                "{\"actionId\":\"Talk\",\"displayName\":\"会話\",\"displayColumn\":1,\"sortOrder\":10," +
                "\"executionType\":\"OpenConversationGenres\",\"isEnabled\":true}," +
                "{\"actionId\":\"StatusDetail\",\"displayName\":\"状態\",\"displayColumn\":3,\"sortOrder\":12," +
                "\"executionType\":\"OpenStatusDetailPanel\",\"isEnabled\":true}]}" );

            string actionsFolder = AssetFolder + "/Actions";
            EnsureAssetFolder(actionsFolder);
            ActionData existing = ScriptableObject.CreateInstance<ActionData>();
            existing.actionId = "Talk";
            existing.reactions.Add(new ActionReactionData { reactionId = "KeepReaction" });
            AssetDatabase.CreateAsset(existing, actionsFolder + "/Talk.asset");

            HeroineAssetImporter.HeroineImportReport report =
                new HeroineAssetImporter.HeroineImportReport();
            HeroineAssetImporter.ImportMenuActions(importFolder, HeroineId, report);
            AssetDatabase.SaveAssets();

            ActionData talk = AssetDatabase.LoadAssetAtPath<ActionData>(actionsFolder + "/Talk.asset");
            ActionData status = AssetDatabase.LoadAssetAtPath<ActionData>(actionsFolder + "/StatusDetailAction.asset");

            Assert.That(report.menuActionCount, Is.EqualTo(2));
            Assert.That(talk.executionType, Is.EqualTo(ActionExecutionType.OpenConversationGenres));
            Assert.That(talk.displayName, Is.EqualTo("会話"));
            Assert.That(talk.sortOrder, Is.EqualTo(10));
            Assert.That(talk.reactions.Count, Is.EqualTo(1));
            Assert.That(talk.reactions[0].reactionId, Is.EqualTo("KeepReaction"));
            Assert.That(status, Is.Not.Null);
            Assert.That(status.executionType, Is.EqualTo(ActionExecutionType.OpenStatusDetailPanel));
            Assert.That(status.displayColumn, Is.EqualTo(ActionButtonColumn.Right));
            Assert.That(status.advanceTime, Is.False);
        }
        finally
        {
            if (Directory.Exists(importFolder))
            {
                Directory.Delete(importFolder, true);
            }
        }
    }

    private static void EnsureAssetFolder(string folderPath)
    {
        string current = "Assets";
        string[] parts = folderPath.Substring("Assets/".Length).Split('/');
        foreach (string part in parts)
        {
            string next = current + "/" + part;
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, part);
            }

            current = next;
        }
    }
}
#endif
