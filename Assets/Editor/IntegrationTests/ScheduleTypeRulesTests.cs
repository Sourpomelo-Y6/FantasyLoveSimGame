#if UNITY_INCLUDE_TESTS
using NUnit.Framework;

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
}
#endif
