using System;
using System.Collections.Generic;

public sealed class SimpleBattleSimulationResult
{
    public bool PlayerWon;
    public int Turns;
    public int PlayerDamageTaken;
    public int HeroineDamageTaken;
    public int PlayerRemainingHp;
    public int HeroineRemainingHp;
    public int EnemyRemainingHp;
    public List<string> LogLines = new List<string>();
}

/// <summary>
/// 探索用の簡易戦闘を、副作用なしで計算する。
/// 渡されたステータスは複製して使用するため、プレビューやEditMode Testにも利用できる。
/// </summary>
public static class SimpleBattleSimulator
{
    public const int DefaultMaxTurns = 20;

    public static SimpleBattleSimulationResult Simulate(
        BattleStatusData player,
        BattleStatusData heroine,
        BattleStatusData enemy,
        int maxTurns = DefaultMaxTurns)
    {
        SimpleBattleSimulationResult result = new SimpleBattleSimulationResult();
        BattleStatusData playerStatus = CloneAndClamp(player);
        BattleStatusData heroineStatus = CloneAndClamp(heroine);
        BattleStatusData enemyStatus = CloneAndClamp(enemy);

        if (playerStatus == null || enemyStatus == null || maxTurns < 1)
        {
            SetRemainingHp(result, playerStatus, heroineStatus, enemyStatus);
            return result;
        }

        bool includeHeroine = heroineStatus != null;
        bool playerActsFirst = playerStatus.speed >= enemyStatus.speed;

        for (int turn = 1; turn <= maxTurns; turn++)
        {
            result.Turns = turn;
            if (playerActsFirst)
            {
                AttackEnemy(playerStatus, enemyStatus, "プレイヤー", turn, result);
                if (enemyStatus.currentHp <= 0) return Complete(true, result, playerStatus, heroineStatus, enemyStatus);

                AttackEnemy(heroineStatus, enemyStatus, "ヒロイン", turn, result);
                if (enemyStatus.currentHp <= 0) return Complete(true, result, playerStatus, heroineStatus, enemyStatus);

                AttackParty(enemyStatus, playerStatus, heroineStatus, includeHeroine, turn, result);
            }
            else
            {
                AttackParty(enemyStatus, playerStatus, heroineStatus, includeHeroine, turn, result);
                if (playerStatus.currentHp <= 0) return Complete(false, result, playerStatus, heroineStatus, enemyStatus);

                AttackEnemy(playerStatus, enemyStatus, "プレイヤー", turn, result);
                if (enemyStatus.currentHp <= 0) return Complete(true, result, playerStatus, heroineStatus, enemyStatus);

                AttackEnemy(heroineStatus, enemyStatus, "ヒロイン", turn, result);
                if (enemyStatus.currentHp <= 0) return Complete(true, result, playerStatus, heroineStatus, enemyStatus);
            }

            if (playerStatus.currentHp <= 0)
                return Complete(false, result, playerStatus, heroineStatus, enemyStatus);
        }

        return Complete(false, result, playerStatus, heroineStatus, enemyStatus);
    }

    public static int CalculateDamage(BattleStatusData attacker, BattleStatusData defender)
    {
        if (attacker == null) return 1;
        int defense = defender != null ? defender.defense : 0;
        return Math.Max(1, attacker.attack - defense);
    }

    private static BattleStatusData CloneAndClamp(BattleStatusData source)
    {
        if (source == null) return null;
        BattleStatusData clone = source.Clone();
        clone.Clamp();
        return clone;
    }

    private static void AttackEnemy(
        BattleStatusData attacker,
        BattleStatusData enemy,
        string attackerName,
        int turn,
        SimpleBattleSimulationResult result)
    {
        if (attacker == null || attacker.currentHp <= 0 || enemy == null || enemy.currentHp <= 0) return;
        int damage = ApplyDamage(enemy, CalculateDamage(attacker, enemy));
        result.LogLines.Add(turn + "T: " + attackerName + " -> 敵 " + damage);
    }

    private static void AttackParty(
        BattleStatusData enemy,
        BattleStatusData player,
        BattleStatusData heroine,
        bool includeHeroine,
        int turn,
        SimpleBattleSimulationResult result)
    {
        if (enemy == null || enemy.currentHp <= 0) return;
        bool canAttackHeroine = includeHeroine && heroine != null && heroine.currentHp > 0;
        if (canAttackHeroine && turn % 2 == 0)
        {
            int damage = ApplyDamage(heroine, CalculateDamage(enemy, heroine));
            result.HeroineDamageTaken += damage;
            result.LogLines.Add(turn + "T: 敵 -> ヒロイン " + damage);
            return;
        }

        int playerDamage = ApplyDamage(player, CalculateDamage(enemy, player));
        result.PlayerDamageTaken += playerDamage;
        result.LogLines.Add(turn + "T: 敵 -> プレイヤー " + playerDamage);
    }

    private static int ApplyDamage(BattleStatusData target, int damage)
    {
        if (target == null || damage <= 0) return 0;
        int before = target.currentHp;
        target.currentHp -= damage;
        target.Clamp();
        return before - target.currentHp;
    }

    private static SimpleBattleSimulationResult Complete(
        bool playerWon,
        SimpleBattleSimulationResult result,
        BattleStatusData player,
        BattleStatusData heroine,
        BattleStatusData enemy)
    {
        result.PlayerWon = playerWon;
        SetRemainingHp(result, player, heroine, enemy);
        return result;
    }

    private static void SetRemainingHp(
        SimpleBattleSimulationResult result,
        BattleStatusData player,
        BattleStatusData heroine,
        BattleStatusData enemy)
    {
        result.PlayerRemainingHp = player != null ? player.currentHp : 0;
        result.HeroineRemainingHp = heroine != null ? heroine.currentHp : 0;
        result.EnemyRemainingHp = enemy != null ? enemy.currentHp : 0;
    }
}
