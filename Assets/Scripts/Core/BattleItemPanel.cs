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
    [SerializeField] private Image playerTargetImage;
    [SerializeField] private Image heroineTargetImage;
    [SerializeField] private TextMeshProUGUI playerTargetNameText;
    [SerializeField] private TextMeshProUGUI heroineTargetNameText;
    [SerializeField] private TextMeshProUGUI playerTargetHpText;
    [SerializeField] private TextMeshProUGUI heroineTargetHpText;
    [SerializeField] private TextMeshProUGUI playerTargetMpText;
    [SerializeField] private TextMeshProUGUI heroineTargetMpText;
    [SerializeField] private Transform itemList;
    [SerializeField] private Button itemButtonPrefab;
    private readonly List<GameObject> rows = new List<GameObject>();
    private ShopItemData item;
    private BattleStatusData player;
    private BattleStatusData heroine;
    private bool targetHeroine;
    private Action<ShopItemData, bool> confirmed;
    private IReadOnlyList<BattleStatusEffect> playerEffects;
    private IReadOnlyList<BattleStatusEffect> heroineEffects;
    public void Open(ShopItemData value, BattleStatusData playerStatus, BattleStatusData heroineStatus, Action<ShopItemData, bool> onConfirmed)
    {
        item = value; player = playerStatus; heroine = heroineStatus; confirmed = onConfirmed; targetHeroine = heroine != null;
        if (panelRoot == null) panelRoot = gameObject;
        playerTargetButton = playerTargetButton ?? Find("PlayerTargetButton"); heroineTargetButton = heroineTargetButton ?? Find("HeroineTargetButton");
        useButton = useButton ?? Find("UseButton"); closeButton = closeButton ?? Find("CloseButton");
        playerTargetStatusText = playerTargetStatusText ?? FindText("PlayerTargetStatusText"); heroineTargetStatusText = heroineTargetStatusText ?? FindText("HeroineTargetStatusText");
        playerTargetImage = playerTargetImage ?? FindImage("PlayerTargetImage"); heroineTargetImage = heroineTargetImage ?? FindImage("HeroineTargetImage");
        playerTargetNameText = playerTargetNameText ?? FindText("PlayerTargetNameText"); heroineTargetNameText = heroineTargetNameText ?? FindText("HeroineTargetNameText");
        playerTargetHpText = playerTargetHpText ?? FindText("PlayerTargetHpText"); heroineTargetHpText = heroineTargetHpText ?? FindText("HeroineTargetHpText");
        playerTargetMpText = playerTargetMpText ?? FindText("PlayerTargetMpText"); heroineTargetMpText = heroineTargetMpText ?? FindText("HeroineTargetMpText");
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
    public void SetStatusEffects(IReadOnlyList<BattleStatusEffect> player, IReadOnlyList<BattleStatusEffect> heroine)
    {
        playerEffects = player; heroineEffects = heroine; Refresh();
    }
    public void SetCharacterImages(Sprite playerSprite, Sprite heroineSprite) { if (playerTargetImage != null) { playerTargetImage.sprite = playerSprite; playerTargetImage.enabled = playerSprite != null; } if (heroineTargetImage != null) { heroineTargetImage.sprite = heroineSprite; heroineTargetImage.enabled = heroineSprite != null; } }
    private void Refresh()
    {
        if (heroineTargetButton != null) heroineTargetButton.gameObject.SetActive(heroine != null);
        if (playerTargetStatusText != null) playerTargetStatusText.gameObject.SetActive(false);
        if (heroineTargetStatusText != null) heroineTargetStatusText.gameObject.SetActive(false);
        SetCard(playerTargetNameText, playerTargetHpText, playerTargetMpText, player, "主人公");
        SetCard(heroineTargetNameText, heroineTargetHpText, heroineTargetMpText, heroine, "ヒロイン");
    }
    private string Status(BattleStatusData s, IReadOnlyList<BattleStatusEffect> effects, bool selected) { if (s == null) return "同行者なし"; int hp = Mathf.Min(s.maxHp, s.currentHp + (selected && item != null ? item.hpRecoveryAmount : 0)); int mp = Mathf.Min(s.maxMp, s.currentMp + (selected && item != null ? item.mpRecoveryAmount : 0)); string text = "HP " + s.currentHp + "/" + s.maxHp + (selected ? " → " + hp + "/" + s.maxHp : "") + "\nMP " + s.currentMp + "/" + s.maxMp + (selected ? " → " + mp + "/" + s.maxMp : ""); if (effects != null) foreach (BattleStatusEffect e in effects) text += "\n" + e.displayName + " " + (e.appliedValue >= 0 ? "↑" : "↓") + Mathf.Abs(e.appliedValue) + " 残り" + e.remainingTargetTurns + "T"; return text; }
    private void Close() { if (panelRoot != null) panelRoot.SetActive(false); confirmed = null; }
    private Button Find(string n) { foreach (Button b in GetComponentsInChildren<Button>(true)) if (b.name == n) return b; return null; }
    private TextMeshProUGUI FindText(string n) { foreach (TextMeshProUGUI t in GetComponentsInChildren<TextMeshProUGUI>(true)) if (t.name == n) return t; return null; }
    private Image FindImage(string n) { foreach (Image i in GetComponentsInChildren<Image>(true)) if (i.name == n) return i; return null; }
    private static void SetCard(TextMeshProUGUI name, TextMeshProUGUI hp, TextMeshProUGUI mp, BattleStatusData status, string label) { if (name != null) name.text = label; if (hp != null) hp.text = status == null ? "HP -" : "HP " + status.currentHp + " / " + status.maxHp; if (mp != null) mp.text = status == null ? "MP -" : "MP " + status.currentMp + " / " + status.maxMp; }
    private Transform FindTransform(string n) { foreach (Transform t in GetComponentsInChildren<Transform>(true)) if (t.name == n) return t; return null; }
}
