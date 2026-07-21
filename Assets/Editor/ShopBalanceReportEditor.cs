using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class ShopBalanceReportEditor
{
    private const string MenuPath = "FantasyLoveSim/Validation/Shop Balance Report";
    private const int InitialPlayerMoney = 1000;

    [MenuItem(MenuPath)]
    public static void ShowReport()
    {
        ShopItemData[] items = Resources.LoadAll<ShopItemData>("ShopItems")
            .Where(item => item != null)
            .OrderBy(item => item.price)
            .ThenBy(item => item.itemId)
            .ToArray();
        StringBuilder report = new StringBuilder();
        report.AppendLine("Shop balance report (initial money: " + InitialPlayerMoney + "G)");
        report.AppendLine("Item | Price | Day | Affection | Prerequisites");

        foreach (ShopItemData item in items)
        {
            string prerequisites = item.requiredPurchasedItemIds != null &&
                item.requiredPurchasedItemIds.Count > 0
                ? string.Join(", ", item.requiredPurchasedItemIds.ToArray())
                : "-";
            report.AppendLine(
                item.itemId + " | " + item.price + "G | " + item.requiredDay +
                " | " + item.requiredAffection + " | " + prerequisites);
        }

        ShopItemData[] seasonalOutfits = items
            .Where(item => item.itemId == "SpringOutfitItem" ||
                item.itemId == "SummerOutfitItem" ||
                item.itemId == "AutumnOutfitItem" ||
                item.itemId == "WinterOutfitItem")
            .ToArray();
        int totalPrice = seasonalOutfits.Sum(item => item.price);
        report.AppendLine("Seasonal outfit total: " + totalPrice + "G");
        report.AppendLine(
            "Initial money can buy all seasonal outfits: " +
            (totalPrice <= InitialPlayerMoney ? "Yes" : "No"));

        string message = report.ToString().TrimEnd();
        Debug.Log("[ShopBalance] " + message);
        EditorUtility.DisplayDialog(
            "Shop Balance Report",
            message + "\n\nDetailed output was written to Console.",
            "OK");
    }
}
