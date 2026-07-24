using System.Collections.Generic;

public static class ShopItemPresentation
{
    public static string GetDisplayName(ShopItemData item)
    {
        if (item == null) return "商品未選択";
        return !string.IsNullOrWhiteSpace(item.displayName)
            ? item.displayName
            : item.itemId;
    }

    public static string GetTypeLabel(ShopItemData item)
    {
        if (item == null) return string.Empty;
        if (item.isBattleConsumable) return "戦闘用消耗品";
        if (item.GetUnlockedOutfitIds().Count > 0) return "衣装";
        return "その他";
    }

    public static string GetDescription(ShopItemData item)
    {
        if (item == null) return string.Empty;
        if (!string.IsNullOrWhiteSpace(item.description)) return item.description.Trim();

        List<string> effects = new List<string>();
        if (item.hpRecoveryAmount > 0) effects.Add("HPを" + item.hpRecoveryAmount + "回復");
        if (item.mpRecoveryAmount > 0) effects.Add("MPを" + item.mpRecoveryAmount + "回復");
        List<string> outfitIds = item.GetUnlockedOutfitIds();
        if (outfitIds.Count > 0)
        {
            effects.Add("衣装を解放: " + string.Join("、", outfitIds.ToArray()));
        }

        return effects.Count > 0
            ? string.Join(" / ", effects.ToArray())
            : "説明は未設定です。";
    }

    public static string GetRequirements(ShopItemData item)
    {
        if (item == null) return string.Empty;

        List<string> requirements = new List<string>();
        if (item.requiredAffection > 0)
            requirements.Add("好感度 " + item.requiredAffection + "以上");
        if (item.requiredDay > 0)
            requirements.Add("Day " + item.requiredDay + "以降");
        List<string> requiredIds = item.GetRequiredPurchasedItemIds();
        if (requiredIds.Count > 0)
            requirements.Add("必要商品: " + string.Join("、", requiredIds.ToArray()));

        return requirements.Count > 0
            ? string.Join(" / ", requirements.ToArray())
            : "購入条件: なし";
    }
}
