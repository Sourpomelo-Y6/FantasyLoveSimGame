#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using UnityEditor;

public class SimpleBattleSimulatorTests
{
    [Test]
    public void CalculateDamage_AlwaysDealsAtLeastOneDamage()
    {
        BattleStatusData attacker = Status(10, 2, 0, 1);
        BattleStatusData defender = Status(10, 1, 99, 1);

        Assert.That(SimpleBattleSimulator.CalculateDamage(attacker, defender), Is.EqualTo(1));
    }

    [Test]
    public void ForestSlime_IsSafeForSoloExploration()
    {
        SimpleBattleSimulationResult result = Simulate("ForestSlime", false);

        Assert.That(result.PlayerWon, Is.True);
        Assert.That(result.Turns, Is.EqualTo(3));
        Assert.That(result.PlayerDamageTaken, Is.EqualTo(2));
    }

    [Test]
    public void CaveBat_IsDangerousSoloAndDuoReducesPlayerRisk()
    {
        SimpleBattleSimulationResult solo = Simulate("CaveBat", false);
        SimpleBattleSimulationResult duo = Simulate("CaveBat", true);

        Assert.That(solo.PlayerWon, Is.False);
        Assert.That(solo.Turns, Is.EqualTo(7));
        Assert.That(solo.PlayerDamageTaken, Is.EqualTo(100));
        Assert.That(duo.PlayerWon, Is.True);
        Assert.That(duo.Turns, Is.EqualTo(5));
        Assert.That(duo.PlayerDamageTaken, Is.EqualTo(45));
        Assert.That(duo.HeroineDamageTaken, Is.EqualTo(32));
    }

    [Test]
    public void LakeSpirit_IsLowRiskAndAffectionFocused()
    {
        EnemyData enemy = LoadEnemy("LakeSpirit");
        SimpleBattleSimulationResult result = SimpleBattleSimulator.Simulate(
            PlayerStatus(), null, enemy.CreateBattleStatus());

        Assert.That(result.PlayerWon, Is.True);
        Assert.That(result.Turns, Is.EqualTo(5));
        Assert.That(result.PlayerDamageTaken, Is.EqualTo(4));
        Assert.That(enemy.rewardMoney, Is.LessThan(12));
        Assert.That(enemy.affectionChangeOnWin, Is.EqualTo(2));
    }

    [Test]
    public void Simulate_DoesNotMutateInputsAndHonorsTurnLimit()
    {
        BattleStatusData player = PlayerStatus();
        BattleStatusData enemy = Status(999, 1, 0, 1);

        SimpleBattleSimulationResult result = SimpleBattleSimulator.Simulate(player, null, enemy, 1);

        Assert.That(result.PlayerWon, Is.False);
        Assert.That(result.Turns, Is.EqualTo(1));
        Assert.That(player.currentHp, Is.EqualTo(100));
        Assert.That(enemy.currentHp, Is.EqualTo(999));
    }

    private static SimpleBattleSimulationResult Simulate(string enemyId, bool includeHeroine)
    {
        EnemyData enemy = LoadEnemy(enemyId);
        return SimpleBattleSimulator.Simulate(
            PlayerStatus(),
            includeHeroine ? HeroineStatus() : null,
            enemy.CreateBattleStatus());
    }

    private static EnemyData LoadEnemy(string enemyId)
    {
        EnemyData enemy = AssetDatabase.LoadAssetAtPath<EnemyData>(
            "Assets/Resources/Enemies/" + enemyId + ".asset");
        Assert.That(enemy, Is.Not.Null, "Enemy asset is required: " + enemyId);
        return enemy;
    }

    private static BattleStatusData PlayerStatus()
    {
        return Status(100, 10, 5, 5);
    }

    private static BattleStatusData HeroineStatus()
    {
        return Status(80, 8, 4, 6);
    }

    private static BattleStatusData Status(int hp, int attack, int defense, int speed)
    {
        return new BattleStatusData
        {
            currentHp = hp,
            maxHp = hp,
            currentMp = 30,
            maxMp = 30,
            attack = attack,
            defense = defense,
            speed = speed
        };
    }
}
#endif
