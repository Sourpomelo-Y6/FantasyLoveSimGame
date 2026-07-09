using System;

[Serializable]
public class BattleStatusData
{
    public int currentHp = 100;
    public int maxHp = 100;
    public int currentMp = 30;
    public int maxMp = 30;
    public int attack = 10;
    public int defense = 5;
    public int speed = 5;

    public BattleStatusData Clone()
    {
        return new BattleStatusData
        {
            currentHp = currentHp,
            maxHp = maxHp,
            currentMp = currentMp,
            maxMp = maxMp,
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
        currentMp = source.currentMp;
        maxMp = source.maxMp;
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

        // Older save data did not contain MP fields. Treat missing values as a full default MP pool.
        if (maxMp <= 0)
        {
            maxMp = 30;
            if (currentMp <= 0)
            {
                currentMp = maxMp;
            }
        }

        if (currentMp < 0)
        {
            currentMp = 0;
        }

        if (currentMp > maxMp)
        {
            currentMp = maxMp;
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

    public bool TrySpendMp(int value)
    {
        if (value < 0 || currentMp < value)
        {
            return false;
        }

        currentMp -= value;
        return true;
    }

    public void RestoreMp()
    {
        Clamp();
        currentMp = maxMp;
    }
}
