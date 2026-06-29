using System;

[Serializable]
public class BattleStatusData
{
    public int currentHp = 100;
    public int maxHp = 100;
    public int attack = 10;
    public int defense = 5;
    public int speed = 5;

    public BattleStatusData Clone()
    {
        return new BattleStatusData
        {
            currentHp = currentHp,
            maxHp = maxHp,
            attack = attack,
            defense = defense,
            speed = speed
        };
    }

    public void CopyFrom(BattleStatusData source)
    {
        if (source == null)
        {
            Clamp();
            return;
        }

        currentHp = source.currentHp;
        maxHp = source.maxHp;
        attack = source.attack;
        defense = source.defense;
        speed = source.speed;
        Clamp();
    }

    public void Clamp()
    {
        if (maxHp < 1)
        {
            maxHp = 1;
        }

        if (currentHp < 0)
        {
            currentHp = 0;
        }

        if (currentHp > maxHp)
        {
            currentHp = maxHp;
        }

        if (attack < 0)
        {
            attack = 0;
        }

        if (defense < 0)
        {
            defense = 0;
        }

        if (speed < 0)
        {
            speed = 0;
        }
    }
}
