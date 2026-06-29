using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "LoveSim/Shop Catalog Data")]
public class ShopCatalogData : ScriptableObject
{
    [Header("Items")]
    public List<ShopItemData> items = new List<ShopItemData>();

    public ShopItemData GetFirstItem()
    {
        if (items == null)
        {
            return null;
        }

        foreach (ShopItemData item in items)
        {
            if (item != null)
            {
                return item;
            }
        }

        return null;
    }

    public List<ShopItemData> GetAvailableItems()
    {
        List<ShopItemData> availableItems = new List<ShopItemData>();
        if (items == null)
        {
            return availableItems;
        }

        foreach (ShopItemData item in items)
        {
            if (item != null)
            {
                availableItems.Add(item);
            }
        }

        return availableItems;
    }
}
