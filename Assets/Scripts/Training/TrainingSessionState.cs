using System;

[Serializable]
public class TrainingSessionState
{
    public string trainingId;
    public int playerHp;
    public int playerMaxHp;
    public int heroineHp;
    public int heroineMaxHp;
    public int playerLp;
    public int heroineLp;
    public int elapsedSteps;
    public int simultaneousKnockoutCount;
    public bool wasInterrupted;
    public bool isFinished;

    public static TrainingSessionState Create(
        TrainingData training,
        BattleStatusData playerStatus,
        BattleStatusData heroineStatus)
    {
        TrainingSessionState state = new TrainingSessionState();
        state.trainingId = training != null ? training.trainingId : string.Empty;
        state.playerMaxHp = playerStatus != null ? playerStatus.maxHp : 1;
        state.heroineMaxHp = heroineStatus != null ? heroineStatus.maxHp : 1;
        state.playerHp = playerStatus != null ? playerStatus.currentHp : state.playerMaxHp;
        state.heroineHp = heroineStatus != null ? heroineStatus.currentHp : state.heroineMaxHp;
        state.playerLp = training != null ? training.initialPlayerLp : 0;
        state.heroineLp = training != null ? training.initialHeroineLp : 0;
        state.Clamp();
        return state;
    }

    public void AdvanceStep(TrainingData training)
    {
        if (isFinished)
        {
            return;
        }

        elapsedSteps++;
        playerHp -= training != null ? training.playerHpCostPerStep : 0;
        heroineHp -= training != null ? training.heroineHpCostPerStep : 0;
        ResolveHpAndLp();
    }

    public void Interrupt()
    {
        wasInterrupted = true;
        isFinished = true;
    }

    private void ResolveHpAndLp()
    {
        bool playerDown = playerHp <= 0;
        bool heroineDown = heroineHp <= 0;

        if (playerDown && heroineDown)
        {
            simultaneousKnockoutCount++;
        }

        if (playerDown)
        {
            RecoverWithLp(ref playerHp, playerMaxHp, ref playerLp);
        }

        if (heroineDown)
        {
            RecoverWithLp(ref heroineHp, heroineMaxHp, ref heroineLp);
        }

        if ((playerHp <= 0 && playerLp <= 0) || (heroineHp <= 0 && heroineLp <= 0))
        {
            isFinished = true;
        }

        Clamp();
    }

    private static void RecoverWithLp(ref int hp, int maxHp, ref int lp)
    {
        if (hp > 0)
        {
            return;
        }

        if (lp > 0)
        {
            lp--;
            hp = maxHp;
        }
    }

    private void Clamp()
    {
        if (playerMaxHp < 1)
        {
            playerMaxHp = 1;
        }

        if (heroineMaxHp < 1)
        {
            heroineMaxHp = 1;
        }

        if (playerHp > playerMaxHp)
        {
            playerHp = playerMaxHp;
        }

        if (heroineHp > heroineMaxHp)
        {
            heroineHp = heroineMaxHp;
        }

        if (playerLp < 0)
        {
            playerLp = 0;
        }

        if (heroineLp < 0)
        {
            heroineLp = 0;
        }
    }
}
