using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "LoveSim/Shop Item Data")]
public class ShopItemData : ScriptableObject
{
    [Header("Basic")]
    public string itemId;
    public string displayName;
    public int price = 100;

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
}
