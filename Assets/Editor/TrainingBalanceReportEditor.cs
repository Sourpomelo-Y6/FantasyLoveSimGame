using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class TrainingBalanceReportEditor
{
    private const string MenuPath = "FantasyLoveSim/Validation/Training Balance Report";

    [MenuItem(MenuPath)]
    public static void ShowReport()
    {
        TrainingData[] trainings = Resources.LoadAll<TrainingData>("Training")
            .Where(training => training != null)
            .OrderBy(training => training.trainingId)
            .ToArray();
        StringBuilder report = new StringBuilder();
        report.AppendLine("Training balance report (Player HP 100 / Heroine HP 80 / no skills)");
        report.AppendLine("Training | Steps | End | LP(P/H) | Simultaneous | Affection | Proficiency | SP(P/H)");

        foreach (TrainingData training in trainings)
        {
            TrainingBalanceSimulationResult simulation = TrainingBalanceCalculator.Simulate(
                training,
                Status(100, 10, 5, 5),
                Status(80, 8, 4, 6));
            TrainingResult result = simulation.Result;
            report.AppendLine(
                training.trainingId + " | " + result.elapsedSteps + " | " +
                (simulation.ReachedSafetyLimit ? "SafetyLimit" : result.endReason.ToString()) +
                " | " + result.playerLpConsumedCount + "/" + result.opponentLpConsumedCount +
                " | " + result.simultaneousKnockoutCount +
                " | " + result.totalAffectionReward +
                " | " + simulation.TotalProficiencyReward +
                " | " + result.playerSkillPointReward + "/" + result.heroineSkillPointReward);
        }

        report.AppendLine("Note: SparringPractice affection includes repeated simultaneous-knockout bonuses.");
        string message = report.ToString().TrimEnd();
        Debug.Log("[TrainingBalance] " + message);
        EditorUtility.DisplayDialog(
            "Training Balance Report",
            message + "\n\nDetailed output was written to Console.",
            "OK");
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
