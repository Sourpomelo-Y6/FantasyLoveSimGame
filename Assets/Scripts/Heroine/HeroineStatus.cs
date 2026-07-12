using UnityEngine;

public class HeroineStatus : MonoBehaviour
{
    [Header("Basic")]
    [SerializeField] private string heroineName = "ヒロイン";

    [Header("Affection")]
    [SerializeField] private int affection = 0;
    [SerializeField] private int maxAffection = 9999;
    [SerializeField] private int endingUnlockAffection = 1000;

    [Header("Battle")]
    [SerializeField] private BattleStatusData battleStatus = new BattleStatusData
    {
        currentHp = 80,
        maxHp = 80,
        attack = 8,
        defense = 4,
        speed = 6
    };

    [Header("Outfit Prompt Ability")]
    [SerializeField] private OutfitPromptAbilitySet outfitPromptAbilities = new OutfitPromptAbilitySet();

    public string HeroineName => heroineName;
    public int Affection => affection;
    public int MaxAffection => maxAffection;
    public int EndingUnlockAffection => endingUnlockAffection;
    public BattleStatusData BattleStatus => battleStatus;
    public int CurrentHp => battleStatus != null ? battleStatus.currentHp : 0;
    public int MaxHp => battleStatus != null ? battleStatus.maxHp : 0;
    public OutfitPromptAbilitySet OutfitPromptAbilities => outfitPromptAbilities;

    private void Awake()
    {
        NormalizeBattleStatus();
    }

    public void SetHeroineName(string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            heroineName = value;
        }
    }

    public void SetOutfitPromptAbilities(OutfitPromptAbilitySet source)
    {
        outfitPromptAbilities.CopyFrom(source);
    }

    public void SetBattleStatus(BattleStatusData source)
    {
        NormalizeBattleStatus();
        battleStatus.CopyFrom(source);
    }

    public void SetCurrentHp(int value)
    {
        NormalizeBattleStatus();
        battleStatus.currentHp = value;
        battleStatus.Clamp();
    }

    public int DamageHp(int value)
    {
        if (value <= 0)
        {
            return 0;
        }

        NormalizeBattleStatus();
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

        NormalizeBattleStatus();
        int before = battleStatus.currentHp;
        battleStatus.currentHp += value;
        battleStatus.Clamp();
        return battleStatus.currentHp - before;
    }

    public void AddAffection(int value)
    {
        long nextValue = (long)affection + value;
        affection = nextValue > int.MaxValue
            ? int.MaxValue
            : nextValue < int.MinValue ? int.MinValue : (int)nextValue;
        ClampAffection();
    }

    public void SetAffection(int value)
    {
        affection = value;
        ClampAffection();
    }

    public bool CanEnding()
    {
        return affection >= endingUnlockAffection;
    }

    public string GetAffectionRankName()
    {
        if (affection >= 1000)
        {
            return "好感度MAX";
        }

        if (affection >= 800)
        {
            return "かなり親密";
        }

        if (affection >= 600)
        {
            return "気になる相手";
        }

        if (affection >= 400)
        {
            return "仲の良い友人";
        }

        if (affection >= 200)
        {
            return "友人";
        }

        return "知り合い";
    }

    private void ClampAffection()
    {
        if (maxAffection < 1)
        {
            maxAffection = 9999;
        }

        endingUnlockAffection = Mathf.Clamp(endingUnlockAffection, 0, maxAffection);
        if (affection < 0)
        {
            affection = 0;
        }

        if (affection > maxAffection)
        {
            affection = maxAffection;
        }
    }

    private void NormalizeBattleStatus()
    {
        if (battleStatus == null)
        {
            battleStatus = new BattleStatusData
            {
                currentHp = 80,
                maxHp = 80,
                attack = 8,
                defense = 4,
                speed = 6
            };
        }

        battleStatus.Clamp();
    }
}
