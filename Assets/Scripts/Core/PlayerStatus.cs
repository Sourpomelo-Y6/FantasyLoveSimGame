using UnityEngine;

public class PlayerStatus : MonoBehaviour
{
    [Header("Battle")]
    [SerializeField] private BattleStatusData battleStatus = new BattleStatusData();

    [Header("Money")]
    [SerializeField] private int money = 1000;

    public BattleStatusData BattleStatus => battleStatus;
    public int Money => money;
    public int CurrentHp => battleStatus != null ? battleStatus.currentHp : 0;
    public int MaxHp => battleStatus != null ? battleStatus.maxHp : 0;

    private void Awake()
    {
        Normalize();
    }

    public void SetBattleStatus(BattleStatusData source)
    {
        if (battleStatus == null)
        {
            battleStatus = new BattleStatusData();
        }

        battleStatus.CopyFrom(source);
    }

    public void SetCurrentHp(int value)
    {
        EnsureBattleStatus();
        battleStatus.currentHp = value;
        battleStatus.Clamp();
    }

    public int DamageHp(int value)
    {
        if (value <= 0)
        {
            return 0;
        }

        EnsureBattleStatus();
        int before = battleStatus.currentHp;
        battleStatus.currentHp -= value;
        battleStatus.Clamp();
        return before - battleStatus.currentHp;
    }

    public int RecoverHp(int value)
    {
        if (value <= 0)
        {
            return 0;
        }

        EnsureBattleStatus();
        int before = battleStatus.currentHp;
        battleStatus.currentHp += value;
        battleStatus.Clamp();
        return battleStatus.currentHp - before;
    }

    public void SetMoney(int value)
    {
        money = value;
        if (money < 0)
        {
            money = 0;
        }
    }

    public void AddMoney(int value)
    {
        SetMoney(money + value);
    }

    public bool TrySpendMoney(int value)
    {
        if (value < 0)
        {
            return false;
        }

        if (money < value)
        {
            return false;
        }

        money -= value;
        return true;
    }

    private void Normalize()
    {
        EnsureBattleStatus();
        battleStatus.Clamp();
        SetMoney(money);
    }

    private void EnsureBattleStatus()
    {
        if (battleStatus == null)
        {
            battleStatus = new BattleStatusData();
        }
    }
}
