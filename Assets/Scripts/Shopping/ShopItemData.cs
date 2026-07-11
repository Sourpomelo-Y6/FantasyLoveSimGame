using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "LoveSim/Shop Item Data")]
public class ShopItemData : ScriptableObject
{
    [Header("Basic")]
    public string itemId;
    public string displayName;
    public int price = 100;

    [Header("Battle Consumable")]
    public bool isBattleConsumable;
    [Min(0)] public int mpRecoveryAmount;
    [Min(0)] public int hpRecoveryAmount;

    [Header("Purchase Conditions")]
    public int requiredAffection;
    public int requiredDay;
    public List<string> requiredPurchasedItemIds = new List<string>();

    [Header("Unlocks")]
    public List<string> unlockedOutfitIds = new List<string>();

    public List<string> GetUnlockedOutfitIds()
    {
        if (unlockedOutfitIds == null)
        {
            return new List<string>();
        }

        return new List<string>(unlockedOutfitIds);
    }

    public List<string> GetRequiredPurchasedItemIds()
    {
        if (requiredPurchasedItemIds == null)
        {
            return new List<string>();
        }

        return new List<string>(requiredPurchasedItemIds);
    }
}
