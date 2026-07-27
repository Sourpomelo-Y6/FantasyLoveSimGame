using System;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class BattleMessageAssetSyncIntegrationTests
{
    private const string HeroineId = "BattleMessageSyncTestHeroine";
    private const string ResourceRoot = "Heroines/" + HeroineId;
    private const string AssetRoot = "Assets/Resources/" + ResourceRoot;
    private string exchangeFolder;
    private HeroineProfileData profile;

    [SetUp]
    public void SetUp()
    {
        exchangeFolder = Path.Combine(Path.GetTempPath(), "FantasyLoveSimBattleMessageTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(exchangeFolder, "Data"));
        profile = ScriptableObject.CreateInstance<HeroineProfileData>();
        profile.heroineId = HeroineId;
        profile.battleResultEventResourcePath = ResourceRoot + "/BattleResultEvents";
        profile.battlePanelResultMessageResourcePath = ResourceRoot + "/BattlePanelResultMessages";
    }

    [TearDown]
    public void TearDown()
    {
        if (profile != null) UnityEngine.Object.DestroyImmediate(profile);
        AssetDatabase.DeleteAsset(AssetRoot);
        if (Directory.Exists(exchangeFolder)) Directory.Delete(exchangeFolder, true);
        AssetDatabase.Refresh();
    }

    [Test]
    public void ImportAndExport_RoundTripsBothMessageTypes()
    {
        File.WriteAllText(Path.Combine(exchangeFolder, "Data", "battle_result_events_export.json"),
            "{\"schemaVersion\":1,\"heroineId\":\"" + HeroineId + "\",\"items\":[{" +
            "\"eventId\":\"DuoVictory_Forest\",\"resultType\":\"DuoVictory\",\"battleContextId\":\"Forest\"," +
            "\"speakerType\":\"Heroine\",\"speakerName\":\"テストヒロイン\",\"visualMode\":\"StillWithPortrait\",\"expressionId\":\"Smile\"," +
            "\"message\":\"二人で勝てましたね\",\"voiceId\":\"Battle/DuoVictoryForest01\",\"stillId\":\"VictoryStill\",\"affectionChange\":3," +
            "\"unlockedOutfitIds\":[\"Formal\",\"Casual\"]}]}" );
        File.WriteAllText(Path.Combine(exchangeFolder, "Data", "battle_panel_result_messages_export.json"),
            "{\"schemaVersion\":1,\"heroineId\":\"" + HeroineId + "\",\"items\":[{" +
            "\"messageId\":\"Victory\",\"resultType\":\"Victory\",\"message\":\"勝利しました\",\"voiceId\":\"Battle/Victory01\"}]}" );

        HeroineBattleMessageAssetSync.Import(exchangeFolder, profile);

        BattleResultEventData result = Resources.Load<BattleResultEventData>(profile.battleResultEventResourcePath + "/DuoVictory_Forest");
        BattlePanelResultMessageData panel = Resources.Load<BattlePanelResultMessageData>(profile.battlePanelResultMessageResourcePath + "/Victory");
        Assert.That(result, Is.Not.Null);
        Assert.That(result.battleResultEventType, Is.EqualTo(BattleResultEventType.DuoVictory));
        Assert.That(result.speakerName, Is.EqualTo("テストヒロイン"));
        Assert.That(result.expressionId, Is.EqualTo("Smile"));
        Assert.That(result.visualMode, Is.EqualTo(BattleResultVisualMode.StillWithPortrait));
        Assert.That(result.voiceId, Is.EqualTo("Battle/DuoVictoryForest01"));
        Assert.That(result.unlockedOutfitIds, Is.EqualTo(new[] { "Formal", "Casual" }));
        Assert.That(panel.message, Is.EqualTo("勝利しました"));
        Assert.That(panel.voiceId, Is.EqualTo("Battle/Victory01"));

        // Voice IDを持たない旧JSONでは、既存の音声設定を消さない。
        File.WriteAllText(Path.Combine(exchangeFolder, "Data", "battle_result_events_export.json"),
            "{\"schemaVersion\":1,\"heroineId\":\"" + HeroineId + "\",\"items\":[{" +
            "\"eventId\":\"DuoVictory_Forest\",\"resultType\":\"DuoVictory\",\"battleContextId\":\"Forest\"," +
            "\"message\":\"更新本文\"}]}");
        File.WriteAllText(Path.Combine(exchangeFolder, "Data", "battle_panel_result_messages_export.json"),
            "{\"schemaVersion\":1,\"heroineId\":\"" + HeroineId + "\",\"items\":[{" +
            "\"messageId\":\"Victory\",\"resultType\":\"Victory\",\"message\":\"更新結果\"}]}");
        HeroineBattleMessageAssetSync.Import(exchangeFolder, profile);
        Assert.That(result.voiceId, Is.EqualTo("Battle/DuoVictoryForest01"));
        Assert.That(panel.voiceId, Is.EqualTo("Battle/Victory01"));

        string output = Path.Combine(exchangeFolder, "FromUnity");
        Directory.CreateDirectory(output);
        HeroineBattleMessageAssetSync.Export(profile, output);
        string resultJson = File.ReadAllText(Path.Combine(output, "battle_result_events_from_unity.json"));
        string panelJson = File.ReadAllText(Path.Combine(output, "battle_panel_result_messages_from_unity.json"));
        StringAssert.Contains("DuoVictory_Forest", resultJson);
        StringAssert.Contains("更新本文", resultJson);
        StringAssert.Contains("Battle/DuoVictoryForest01", resultJson);
        StringAssert.Contains("Battle/Victory01", panelJson);
    }
}
