using System;
using TMPro;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BattleItemPanel : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button playerTargetButton;
    [SerializeField] private Button heroineTargetButton;
    [SerializeField] private Button useButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private TextMeshProUGUI playerTargetStatusText;
    [SerializeField] private TextMeshProUGUI heroineTargetStatusText;
    [SerializeField] private Transform itemList;
    [SerializeField] private Button itemButtonPrefab;
    private readonly List<GameObject> rows = new List<GameObject>();
    private ShopItemData item;
    private BattleStatusData player;
    private BattleStatusData heroine;
    private bool targetHeroine;
    private Action<ShopItemData, bool> confirmed;
    public void Open(ShopItemData value, BattleStatusData playerStatus, BattleStatusData heroineStatus, Action<ShopItemData, bool> onConfirmed)
    {
        item = value; player = playerStatus; heroine = heroineStatus; confirmed = onConfirmed; targetHeroine = heroine != null;
        if (panelRoot == null) panelRoot = gameObject;
        playerTargetButton = playerTargetButton ?? Find("PlayerTargetButton"); heroineTargetButton = heroineTargetButton ?? Find("HeroineTargetButton");
        useButton = useButton ?? Find("UseButton"); closeButton = closeButton ?? Find("CloseButton");
        playerTargetStatusText = playerTargetStatusText ?? FindText("PlayerTargetStatusText"); heroineTargetStatusText = heroineTargetStatusText ?? FindText("HeroineTargetStatusText");
        playerTargetButton.onClick.RemoveAllListeners(); playerTargetButton.onClick.AddListener(() => { targetHeroine = false; Refresh(); });
        heroineTargetButton.onClick.RemoveAllListeners(); heroineTargetButton.onClick.AddListener(() => { targetHeroine = true; Refresh(); });
        useButton.onClick.RemoveAllListeners(); useButton.onClick.AddListener(() => { confirmed?.Invoke(item, targetHeroine); Close(); });
        closeButton.onClick.RemoveAllListeners(); closeButton.onClick.AddListener(Close);
        panelRoot.SetActive(true); Refresh();
    }
    public void Open(IReadOnlyList<ShopItemData> items, BattleStatusData playerStatus, BattleStatusData heroineStatus, Action<ShopItemData, bool> onConfirmed)
    {
        Open(items != null && items.Count > 0 ? items[0] : null, playerStatus, heroineStatus, onConfirmed);
        itemList = itemList ?? FindTransform("ItemList"); itemButtonPrefab = itemButtonPrefab ?? Find("BattleItemButtonPrefab");
        foreach (GameObject row in rows) Destroy(row); rows.Clear();
        if (items == null || itemList == null || itemButtonPrefab == null) return;
        foreach (ShopItemData entry in items) { ShopItemData captured = entry; Button row = Instantiate(itemButtonPrefab, itemList); row.gameObject.SetActive(true); row.GetComponentInChildren<TMP_Text>().text = entry.displayName + " 所持: " + ""; row.onClick.RemoveAllListeners(); row.onClick.AddListener(() => { item = captured; Refresh(); }); rows.Add(row.gameObject); }
    }
    private void Refresh()
    {
        if (heroineTargetButton != null) heroineTargetButton.gameObject.SetActive(heroine != null);
        if (playerTargetStatusText != null) playerTargetStatusText.text = Status(player);
        if (heroineTargetStatusText != null) heroineTargetStatusText.text = Status(heroine);
    }
    private string Status(BattleStatusData s) { if (s == null) return "同行者なし"; int hp = Mathf.Min(s.maxHp, s.currentHp + (item != null ? item.hpRecoveryAmount : 0)); int mp = Mathf.Min(s.maxMp, s.currentMp + (item != null ? item.mpRecoveryAmount : 0)); return "HP " + s.currentHp + "/" + s.maxHp + " → " + hp + "/" + s.maxHp + "\nMP " + s.currentMp + "/" + s.maxMp + " → " + mp + "/" + s.maxMp; }
    private void Close() { if (panelRoot != null) panelRoot.SetActive(false); confirmed = null; }
    private Button Find(string n) { foreach (Button b in GetComponentsInChildren<Button>(true)) if (b.name == n) return b; return null; }
    private TextMeshProUGUI FindText(string n) { foreach (TextMeshProUGUI t in GetComponentsInChildren<TextMeshProUGUI>(true)) if (t.name == n) return t; return null; }
    private Transform FindTransform(string n) { foreach (Transform t in GetComponentsInChildren<Transform>(true)) if (t.name == n) return t; return null; }
}
