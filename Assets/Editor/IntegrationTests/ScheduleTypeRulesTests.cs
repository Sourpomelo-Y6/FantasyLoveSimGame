#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using UnityEditor;

public class ScheduleTypeRulesTests
{
    [TestCase(ScheduleType.SoloForest)]
    [TestCase(ScheduleType.SoloCave)]
    [TestCase(ScheduleType.SoloLake)]
    [TestCase(ScheduleType.SoloShopping)]
    public void IsSoloSchedule_ReturnsTrueForPlayerOnlyOutings(
        ScheduleType scheduleType)
    {
        Assert.That(ScheduleManager.IsSoloSchedule(scheduleType), Is.True);
    }

    [TestCase(ScheduleType.DuoForest)]
    [TestCase(ScheduleType.DuoCave)]
    [TestCase(ScheduleType.DuoLake)]
    [TestCase(ScheduleType.DuoShopping)]
    [TestCase(ScheduleType.StayHome)]
    [TestCase(ScheduleType.None)]
    public void IsSoloSchedule_ReturnsFalseWhenHeroineMayParticipate(
        ScheduleType scheduleType)
    {
        Assert.That(ScheduleManager.IsSoloSchedule(scheduleType), Is.False);
    }

    [TestCase("SoloVictory")]
    [TestCase("SoloDefeat")]
    public void CommonSoloBattleResult_UsesScheduleSpeaker(string assetName)
    {
        BattleResultEventData data =
            AssetDatabase.LoadAssetAtPath<BattleResultEventData>(
                "Assets/Resources/BattleResultEvents/" + assetName + ".asset");

        Assert.That(data, Is.Not.Null);
        Assert.That(data.speakerType,
            Is.EqualTo(ScheduledEventSpeakerType.Schedule));
    }
}
#endif
