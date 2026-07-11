using System;
using TMPro;
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
    private void Refresh()
    {
        if (heroineTargetButton != null) heroineTargetButton.gameObject.SetActive(heroine != null);
        if (playerTargetStatusText != null) playerTargetStatusText.text = Status(player);
        if (heroineTargetStatusText != null) heroineTargetStatusText.text = Status(heroine);
    }
    private string Status(BattleStatusData s) { if (s == null) return "同行者なし"; int after = Mathf.Min(s.maxMp, s.currentMp + (item != null ? item.mpRecoveryAmount : 0)); return "MP " + s.currentMp + "/" + s.maxMp + " → " + after + "/" + s.maxMp; }
    private void Close() { if (panelRoot != null) panelRoot.SetActive(false); confirmed = null; }
    private Button Find(string n) { foreach (Button b in GetComponentsInChildren<Button>(true)) if (b.name == n) return b; return null; }
    private TextMeshProUGUI FindText(string n) { foreach (TextMeshProUGUI t in GetComponentsInChildren<TextMeshProUGUI>(true)) if (t.name == n) return t; return null; }
}
