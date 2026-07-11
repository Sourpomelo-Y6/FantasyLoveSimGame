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
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private GameObject playerSelectedFrame;
    [SerializeField] private GameObject heroineSelectedFrame;
    [SerializeField] private Transform itemList;
    [SerializeField] private Button itemButtonPrefab;
    [SerializeField] private Color selectedItemColor = new Color(1f, 0.85f, 0.35f, 1f);
    private readonly List<GameObject> rows = new List<GameObject>();
    private readonly List<Button> itemButtons = new List<Button>();
    private readonly List<ShopItemData> displayedItems = new List<ShopItemData>();
    private readonly List<Color> itemNormalColors = new List<Color>();
    private ShopItemData item;
    private BattleStatusData player;
    private BattleStatusData heroine;
    private bool targetHeroine;
    private Action<ShopItemData, bool> confirmed;
    private Func<ShopItemData, bool, bool> confirmedWithResult;
    private Func<ShopItemData, int> quantityResolver;
    public void Open(ShopItemData value, BattleStatusData playerStatus, BattleStatusData heroineStatus, Action<ShopItemData, bool> onConfirmed)
    {
        item = value; player = playerStatus; heroine = heroineStatus; confirmed = onConfirmed; confirmedWithResult = null; targetHeroine = heroine != null;
        if (panelRoot == null) panelRoot = gameObject;
        playerTargetButton = playerTargetButton ?? Find("PlayerTargetButton"); heroineTargetButton = heroineTargetButton ?? Find("HeroineTargetButton");
        useButton = useButton ?? Find("UseButton"); closeButton = closeButton ?? Find("CloseButton");
        playerTargetStatusText = playerTargetStatusText ?? FindText("PlayerTargetStatusText"); heroineTargetStatusText = heroineTargetStatusText ?? FindText("HeroineTargetStatusText");
        playerTargetImage = playerTargetImage ?? FindImage("PlayerTargetImage"); heroineTargetImage = heroineTargetImage ?? FindImage("HeroineTargetImage");
        playerTargetNameText = playerTargetNameText ?? FindText("PlayerTargetNameText"); heroineTargetNameText = heroineTargetNameText ?? FindText("HeroineTargetNameText");
        playerTargetHpText = playerTargetHpText ?? FindText("PlayerTargetHpText"); heroineTargetHpText = heroineTargetHpText ?? FindText("HeroineTargetHpText");
        playerTargetMpText = playerTargetMpText ?? FindText("PlayerTargetMpText"); heroineTargetMpText = heroineTargetMpText ?? FindText("HeroineTargetMpText");
        descriptionText = descriptionText ?? FindText("DescriptionText");
        playerSelectedFrame = playerSelectedFrame ?? FindObject("PlayerSelectedFrame");
        heroineSelectedFrame = heroineSelectedFrame ?? FindObject("HeroineSelectedFrame");
        playerTargetButton.onClick.RemoveAllListeners(); playerTargetButton.onClick.AddListener(() => { targetHeroine = false; Refresh(); });
        heroineTargetButton.onClick.RemoveAllListeners(); heroineTargetButton.onClick.AddListener(() => { targetHeroine = true; Refresh(); });
        useButton.onClick.RemoveAllListeners(); useButton.onClick.AddListener(ConfirmUse);
        closeButton.onClick.RemoveAllListeners(); closeButton.onClick.AddListener(Close);
        panelRoot.SetActive(true); Refresh();
    }
    public void Open(IReadOnlyList<ShopItemData> items, BattleStatusData playerStatus, BattleStatusData heroineStatus, Action<ShopItemData, bool> onConfirmed)
    {
        Open(items, playerStatus, heroineStatus, null, onConfirmed);
    }
    public void Open(IReadOnlyList<ShopItemData> items, BattleStatusData playerStatus, BattleStatusData heroineStatus, Func<ShopItemData, int> getQuantity, Action<ShopItemData, bool> onConfirmed)
    {
        Open(items, playerStatus, heroineStatus, getQuantity, (selectedItem, selectedHeroine) =>
        {
            onConfirmed?.Invoke(selectedItem, selectedHeroine);
            return true;
        });
    }
    public void Open(IReadOnlyList<ShopItemData> items, BattleStatusData playerStatus, BattleStatusData heroineStatus, Func<ShopItemData, int> getQuantity, Func<ShopItemData, bool, bool> onConfirmed)
    {
        Open(items != null && items.Count > 0 ? items[0] : null, playerStatus, heroineStatus, (Action<ShopItemData, bool>)null);
        confirmed = null;
        confirmedWithResult = onConfirmed;
        quantityResolver = getQuantity;
        itemList = itemList ?? FindTransform("ItemList"); itemButtonPrefab = itemButtonPrefab ?? Find("BattleItemButtonPrefab");
        foreach (GameObject row in rows) Destroy(row); rows.Clear(); itemButtons.Clear(); displayedItems.Clear(); itemNormalColors.Clear();
        if (items == null || itemList == null || itemButtonPrefab == null) return;
        foreach (ShopItemData entry in items) { ShopItemData captured = entry; Button row = Instantiate(itemButtonPrefab, itemList); row.gameObject.SetActive(true); TMP_Text label = row.GetComponentInChildren<TMP_Text>(); if (label != null) label.text = GetDisplayName(entry) + " 所持: " + GetQuantity(entry); row.onClick.RemoveAllListeners(); row.onClick.AddListener(() => { item = captured; Refresh(); }); rows.Add(row.gameObject); itemButtons.Add(row); displayedItems.Add(entry); Image background = row.GetComponent<Image>(); itemNormalColors.Add(background != null ? background.color : Color.white); }
        Refresh();
    }
    public void SetStatusEffects(IReadOnlyList<BattleStatusEffect> player, IReadOnlyList<BattleStatusEffect> heroine)
    {
        Refresh();
    }
    public void SetCharacterImages(Sprite playerSprite, Sprite heroineSprite) { if (playerTargetImage != null) { playerTargetImage.sprite = playerSprite; playerTargetImage.enabled = playerSprite != null; } if (heroineTargetImage != null) { heroineTargetImage.sprite = heroineSprite; heroineTargetImage.enabled = heroineSprite != null; } }
    private void Refresh()
    {
        if (heroineTargetButton != null) heroineTargetButton.gameObject.SetActive(heroine != null);
        if (playerTargetStatusText != null) playerTargetStatusText.gameObject.SetActive(false);
        if (heroineTargetStatusText != null) heroineTargetStatusText.gameObject.SetActive(false);
        if (playerSelectedFrame != null) playerSelectedFrame.SetActive(!targetHeroine);
        if (heroineSelectedFrame != null) heroineSelectedFrame.SetActive(targetHeroine && heroine != null);
        SetCard(playerTargetNameText, playerTargetHpText, playerTargetMpText, player, "主人公");
        SetCard(heroineTargetNameText, heroineTargetHpText, heroineTargetMpText, heroine, "ヒロイン");
        if (descriptionText != null) descriptionText.text = BuildDescription();
        if (useButton != null) useButton.interactable = CanUseSelectedItem();
        for (int i = 0; i < itemButtons.Count; i++)
        {
            Image background = itemButtons[i] != null ? itemButtons[i].GetComponent<Image>() : null;
            if (itemButtons[i] != null) itemButtons[i].interactable = GetQuantity(displayedItems[i]) > 0;
            if (background != null) background.color = displayedItems[i] == item ? selectedItemColor : itemNormalColors[i];
            TMP_Text label = itemButtons[i] != null ? itemButtons[i].GetComponentInChildren<TMP_Text>() : null;
            if (label != null) label.text = GetDisplayName(displayedItems[i]) + " 所持: " + GetQuantity(displayedItems[i]);
        }
    }
    private void ConfirmUse()
    {
        if (!CanUseSelectedItem())
        {
            Refresh();
            return;
        }

        bool succeeded = confirmedWithResult != null
            ? confirmedWithResult.Invoke(item, targetHeroine)
            : ConfirmWithAction();

        if (!succeeded)
        {
            Refresh();
            return;
        }

        SelectAvailableItemIfNeeded();
        Refresh();
    }

    private bool ConfirmWithAction()
    {
        confirmed?.Invoke(item, targetHeroine);
        return true;
    }

    private void SelectAvailableItemIfNeeded()
    {
        if (item != null && GetQuantity(item) > 0)
        {
            return;
        }

        item = null;
        foreach (ShopItemData displayedItem in displayedItems)
        {
            if (GetQuantity(displayedItem) > 0)
            {
                item = displayedItem;
                return;
            }
        }
    }

    private void Close() { if (panelRoot != null) panelRoot.SetActive(false); confirmed = null; confirmedWithResult = null; }
    private Button Find(string n) { foreach (Button b in GetComponentsInChildren<Button>(true)) if (b.name == n) return b; return null; }
    private TextMeshProUGUI FindText(string n) { foreach (TextMeshProUGUI t in GetComponentsInChildren<TextMeshProUGUI>(true)) if (t.name == n) return t; return null; }
    private Image FindImage(string n) { foreach (Image i in GetComponentsInChildren<Image>(true)) if (i.name == n) return i; return null; }
    private GameObject FindObject(string n) { foreach (Transform t in GetComponentsInChildren<Transform>(true)) if (t.name == n) return t.gameObject; return null; }
    private static void SetCard(TextMeshProUGUI name, TextMeshProUGUI hp, TextMeshProUGUI mp, BattleStatusData status, string label) { if (name != null) name.text = label; if (hp != null) hp.text = status == null ? "HP -" : "HP " + status.currentHp + " / " + status.maxHp; if (mp != null) mp.text = status == null ? "MP -" : "MP " + status.currentMp + " / " + status.maxMp; }
    private string BuildDescription()
    {
        if (item == null) return "使用するアイテムを選択してください。";
        BattleStatusData target = GetSelectedTarget();
        string text = GetDisplayName(item) + "\n";
        if (item.hpRecoveryAmount > 0) text += "HP " + item.hpRecoveryAmount + " 回復";
        if (item.mpRecoveryAmount > 0) text += (item.hpRecoveryAmount > 0 ? " / " : "") + "MP " + item.mpRecoveryAmount + " 回復";
        if (item.hpRecoveryAmount <= 0 && item.mpRecoveryAmount <= 0) text += "戦闘中に使用できます。";
        text += "\n対象: " + (targetHeroine ? "ヒロイン" : "主人公");
        if (target == null) return text + "\n対象がいません。";
        if (item.hpRecoveryAmount > 0) text += "\nHP " + target.currentHp + " / " + target.maxHp + " → " + Mathf.Min(target.maxHp, target.currentHp + item.hpRecoveryAmount) + " / " + target.maxHp;
        if (item.mpRecoveryAmount > 0) text += "\nMP " + target.currentMp + " / " + target.maxMp + " → " + Mathf.Min(target.maxMp, target.currentMp + item.mpRecoveryAmount) + " / " + target.maxMp;
        if (!CanUseSelectedItem()) text += "\n現在は使用できません。";
        return text;
    }
    private BattleStatusData GetSelectedTarget() { return targetHeroine ? heroine : player; }
    private bool CanUseSelectedItem()
    {
        BattleStatusData target = GetSelectedTarget();
        if (item == null || target == null || GetQuantity(item) <= 0)
        {
            return false;
        }

        bool canRecoverHp = item.hpRecoveryAmount > 0 && target.currentHp < target.maxHp;
        bool canRecoverMp = item.mpRecoveryAmount > 0 && target.currentMp < target.maxMp;
        return canRecoverHp || canRecoverMp;
    }
    private int GetQuantity(ShopItemData value) { return quantityResolver != null ? quantityResolver(value) : value != null ? 1 : 0; }
    private static string GetDisplayName(ShopItemData value) { if (value == null) return ""; return !string.IsNullOrEmpty(value.displayName) ? value.displayName : value.itemId; }
    private Transform FindTransform(string n) { foreach (Transform t in GetComponentsInChildren<Transform>(true)) if (t.name == n) return t; return null; }
}
