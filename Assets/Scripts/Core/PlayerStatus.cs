using UnityEngine;

public class PlayerStatus : MonoBehaviour
{
    [Header("Battle")]
    [SerializeField] private BattleStatusData battleStatus = new BattleStatusData();

    [Header("Money")]
    [SerializeField] private int money = 1000;

    public BattleStatusData BattleStatus => battleStatus;
    public int Money => money;

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
        if (battleStatus == null)
        {
            battleStatus = new BattleStatusData();
        }

        battleStatus.Clamp();
        SetMoney(money);
    }
}
