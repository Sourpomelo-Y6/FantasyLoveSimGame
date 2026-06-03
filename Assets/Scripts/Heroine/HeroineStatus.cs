using UnityEngine;

public class HeroineStatus : MonoBehaviour
{
    [Header("Basic")]
    [SerializeField] private string heroineName = "ヒロイン";

    [Header("Affection")]
    [SerializeField] private int affection = 0;
    [SerializeField] private int maxAffection = 100;

    [Header("Outfit Prompt Ability")]
    [SerializeField] private OutfitPromptAbilitySet outfitPromptAbilities = new OutfitPromptAbilitySet();

    public string HeroineName => heroineName;
    public int Affection => affection;
    public int MaxAffection => maxAffection;
    public OutfitPromptAbilitySet OutfitPromptAbilities => outfitPromptAbilities;

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

    public void AddAffection(int value)
    {
        affection += value;
        ClampAffection();
    }

    public void SetAffection(int value)
    {
        affection = value;
        ClampAffection();
    }

    public bool CanEnding()
    {
        return affection >= maxAffection;
    }

    public string GetAffectionRankName()
    {
        if (affection >= 100)
        {
            return "好感度MAX";
        }

        if (affection >= 80)
        {
            return "かなり親密";
        }

        if (affection >= 60)
        {
            return "気になる相手";
        }

        if (affection >= 40)
        {
            return "仲の良い友人";
        }

        if (affection >= 20)
        {
            return "友人";
        }

        return "知り合い";
    }

    private void ClampAffection()
    {
        if (affection < 0)
        {
            affection = 0;
        }

        if (affection > maxAffection)
        {
            affection = maxAffection;
        }
    }
}
