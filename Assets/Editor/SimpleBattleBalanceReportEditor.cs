using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class SimpleBattleBalanceReportEditor
{
    private const string MenuPath = "FantasyLoveSim/Validation/Battle Balance Report";

    [MenuItem(MenuPath)]
    public static void ShowReport()
    {
        EnemyData[] enemies = Resources.LoadAll<EnemyData>("Enemies")
            .Where(enemy => enemy != null)
            .OrderBy(enemy => enemy.enemyId)
            .ToArray();
        StringBuilder report = new StringBuilder();
        report.AppendLine("Simple battle balance report");
        report.AppendLine("Enemy | Solo | Duo");

        foreach (EnemyData enemy in enemies)
        {
            SimpleBattleSimulationResult solo = SimpleBattleSimulator.Simulate(
                CreatePlayerStatus(), null, enemy.CreateBattleStatus());
            SimpleBattleSimulationResult duo = SimpleBattleSimulator.Simulate(
                CreatePlayerStatus(), CreateHeroineStatus(), enemy.CreateBattleStatus());
            report.AppendLine(
                enemy.enemyId + " | " + Format(solo) + " | " + Format(duo));
        }

        string message = report.ToString().TrimEnd();
        Debug.Log("[BattleBalance] " + message);
        EditorUtility.DisplayDialog(
            "Battle Balance Report",
            message + "\n\nDetailed output was written to Console.",
            "OK");
    }

    private static string Format(SimpleBattleSimulationResult result)
    {
        return (result.PlayerWon ? "Win" : "Lose") +
            " " + result.Turns + "T" +
            " P-" + result.PlayerDamageTaken +
            " H-" + result.HeroineDamageTaken;
    }

    private static BattleStatusData CreatePlayerStatus()
    {
        return new BattleStatusData
        {
            currentHp = 100,
            maxHp = 100,
            currentMp = 30,
            maxMp = 30,
            attack = 10,
            defense = 5,
            speed = 5
        };
    }

    private static BattleStatusData CreateHeroineStatus()
    {
        return new BattleStatusData
        {
            currentHp = 80,
            maxHp = 80,
            currentMp = 30,
            maxMp = 30,
            attack = 8,
            defense = 4,
            speed = 6
        };
    }
}
