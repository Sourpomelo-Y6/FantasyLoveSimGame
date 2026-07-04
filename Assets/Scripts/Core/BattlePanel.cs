using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattlePanel : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private PlayerStatus playerStatus;
    [SerializeField] private HeroineStatus heroineStatus;
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TextMeshProUGUI enemyNameText;
    [SerializeField] private TextMeshProUGUI enemyHpText;
    [SerializeField] private TextMeshProUGUI playerHpText;
    [SerializeField] private TextMeshProUGUI heroineHpText;
    [SerializeField] private TextMeshProUGUI battleLogText;
    [SerializeField] private Image playerImage;
    [SerializeField] private Image heroineImage;
    [SerializeField] private Image enemyImage;
    [SerializeField] private Button attackButton;
    [SerializeField] private Button escapeButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private EnemyData debugEnemy;
    [SerializeField] private string debugEnemyResourcePath = "Enemies/ForestSlime";
    [SerializeField] private int maxLogLines = 6;

    private BattleStatusData debugPlayerStatus;
    private BattleStatusData debugHeroineStatus;
    private BattleStatusData debugEnemyStatus;
    private EnemyData currentDebugEnemy;
    private string enemyDisplayName = "敵";
    private readonly List<string> logLines = new List<string>();
    private int turnCount;
    private bool battleFinished;

    private void Awake()
    {
        EnsureReferences();
        HookButtons();
    }

    public void Initialize(GameManager manager, PlayerStatus player, HeroineStatus heroine)
    {
        gameManager = manager;
        playerStatus = player;
        heroineStatus = heroine;
        EnsureReferences();
        HookButtons();
    }

    public void OpenDebugBattle()
    {
        EnsureReferences();
        HookButtons();

        currentDebugEnemy = ResolveDebugEnemy();
        enemyDisplayName = currentDebugEnemy != null ? currentDebugEnemy.GetDisplayName() : "デバッグ敵";
        debugEnemyStatus = currentDebugEnemy != null ? currentDebugEnemy.CreateBattleStatus() : CreateDefaultEnemyStatus();
        ApplyPlayerImage();
        ApplyHeroineImage();
        ApplyEnemyImage(currentDebugEnemy);
        debugPlayerStatus = playerStatus != null && playerStatus.BattleStatus != null
            ? playerStatus.BattleStatus.Clone()
            : CreateDefaultPlayerStatus();
        debugHeroineStatus = heroineStatus != null && heroineStatus.BattleStatus != null
            ? heroineStatus.BattleStatus.Clone()
            : null;
        turnCount = 0;
        battleFinished = false;

        logLines.Clear();
        AddLog("デバッグ戦闘を開始しました。");
        AddLog(enemyDisplayName + " が現れました。");
        AddHpSummaryLog();

        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }

        Refresh();
    }

    public void Close()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }

        if (gameManager != null)
        {
            gameManager.OnBattlePanelClosed();
        }
    }

    private void OnAttackClicked()
    {
        if (battleFinished)
        {
            return;
        }

        turnCount++;
        AddLog("--- " + turnCount + "ターン目 ---");

        int playerDamage = Damage(debugPlayerStatus, debugEnemyStatus);
        AddLog("プレイヤーの攻撃。 " + enemyDisplayName + " に " + playerDamage + " ダメージ。");

        if (IsDefeated(debugEnemyStatus))
        {
            FinishBattle("勝利", ResolveVictoryMessage());
            return;
        }

        if (debugHeroineStatus != null && !IsDefeated(debugHeroineStatus))
        {
            int heroineDamage = Damage(debugHeroineStatus, debugEnemyStatus);
            string heroineName = heroineStatus != null ? heroineStatus.HeroineName : "ヒロイン";
            AddLog(heroineName + " の攻撃。 " + enemyDisplayName + " に " + heroineDamage + " ダメージ。");

            if (IsDefeated(debugEnemyStatus))
            {
                FinishBattle("勝利", ResolveVictoryMessage());
                return;
            }
        }

        ApplyEnemyAttack();
        if (IsDefeated(debugPlayerStatus))
        {
            FinishBattle("敗北", ResolveDefeatMessage());
            return;
        }

        AddHpSummaryLog();
        Refresh();
    }

    private void OnEscapeClicked()
    {
        if (!battleFinished)
        {
            FinishBattle("撤退", "撤退しました。");
        }
        else
        {
            Close();
        }
    }

    private void ApplyEnemyAttack()
    {
        bool canAttackHeroine = debugHeroineStatus != null && !IsDefeated(debugHeroineStatus);
        if (canAttackHeroine && Random.Range(0, 2) == 0)
        {
            int heroineDamage = Damage(debugEnemyStatus, debugHeroineStatus);
            string heroineName = heroineStatus != null ? heroineStatus.HeroineName : "ヒロイン";
            AddLog(enemyDisplayName + " の攻撃。 " + heroineName + " は " + heroineDamage + " ダメージ。");
            if (IsDefeated(debugHeroineStatus))
            {
                AddLog(heroineName + " は戦闘不能です。");
            }

            return;
        }

        int playerDamage = Damage(debugEnemyStatus, debugPlayerStatus);
        AddLog(enemyDisplayName + " の攻撃。 プレイヤーは " + playerDamage + " ダメージ。");
    }

    private void FinishBattle(string resultLabel, string message)
    {
        battleFinished = true;
        AddLog(message);
        AddLog("戦闘結果：" + resultLabel);
        AddHpSummaryLog();
        Refresh();
    }

    private void Refresh()
    {
        if (enemyNameText != null)
        {
            enemyNameText.text = enemyDisplayName;
        }

        SetHpText(enemyHpText, "敵HP", debugEnemyStatus);
        SetHpText(playerHpText, "プレイヤーHP", debugPlayerStatus);
        SetHpText(heroineHpText, "ヒロインHP", debugHeroineStatus);

        if (battleLogText != null)
        {
            battleLogText.text = string.Join("\n", logLines);
        }

        if (attackButton != null)
        {
            attackButton.interactable = !battleFinished;
        }

        if (escapeButton != null)
        {
            escapeButton.interactable = true;
            TextMeshProUGUI escapeButtonText = escapeButton.GetComponentInChildren<TextMeshProUGUI>();
            if (escapeButtonText != null)
            {
                escapeButtonText.text = battleFinished ? "閉じる" : "逃げる";
            }
        }
    }

    private void AddHpSummaryLog()
    {
        AddLog(
            "HP: プレイヤー " + FormatHp(debugPlayerStatus) +
            " / ヒロイン " + FormatHp(debugHeroineStatus) +
            " / " + enemyDisplayName + " " + FormatHp(debugEnemyStatus));
    }

    private static string FormatHp(BattleStatusData status)
    {
        if (status == null)
        {
            return "-";
        }

        return status.currentHp + "/" + status.maxHp;
    }

    private string ResolveVictoryMessage()
    {
        if (currentDebugEnemy != null && !string.IsNullOrEmpty(currentDebugEnemy.victoryMessage))
        {
            return currentDebugEnemy.victoryMessage;
        }

        return "勝利しました。";
    }

    private string ResolveDefeatMessage()
    {
        if (currentDebugEnemy != null && !string.IsNullOrEmpty(currentDebugEnemy.defeatMessage))
        {
            return currentDebugEnemy.defeatMessage;
        }

        return "敗北しました。";
    }

    private void AddLog(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return;
        }

        logLines.Add(message);
        int limit = Mathf.Max(1, maxLogLines);
        while (logLines.Count > limit)
        {
            logLines.RemoveAt(0);
        }
    }

    private EnemyData ResolveDebugEnemy()
    {
        if (debugEnemy != null)
        {
            return debugEnemy;
        }

        if (string.IsNullOrEmpty(debugEnemyResourcePath))
        {
            return null;
        }

        debugEnemy = Resources.Load<EnemyData>(debugEnemyResourcePath);
        return debugEnemy;
    }

    private void ApplyEnemyImage(EnemyData enemy)
    {
        if (enemyImage == null)
        {
            return;
        }

        Sprite enemySprite = ResolveEnemySprite(enemy);
        enemyImage.sprite = enemySprite;
        enemyImage.enabled = enemySprite != null;
        enemyImage.preserveAspect = true;
    }

    private void ApplyPlayerImage()
    {
        if (playerImage == null)
        {
            return;
        }

        Sprite playerSprite = ResolvePlayerSprite();
        playerImage.sprite = playerSprite;
        playerImage.enabled = playerSprite != null;
        playerImage.preserveAspect = true;
    }

    private void ApplyHeroineImage()
    {
        if (heroineImage == null)
        {
            return;
        }

        Sprite heroineSprite = ResolveHeroineSprite();
        heroineImage.sprite = heroineSprite;
        heroineImage.enabled = heroineSprite != null;
        heroineImage.preserveAspect = true;
    }

    private static Sprite ResolvePlayerSprite()
    {
        PlayerAssetCatalog catalog = Resources.Load<PlayerAssetCatalog>("Player/PlayerAssetCatalog");
        if (catalog == null || catalog.assets == null || catalog.assets.Count == 0)
        {
            return null;
        }

        return ResolvePlayerCatalogSprite(catalog, "Battle_Player_Idle");
    }

    private Sprite ResolveHeroineSprite()
    {
        HeroineAssetCatalog catalog = gameManager != null ? gameManager.CurrentHeroineAssetCatalog : null;
        if (catalog == null && gameManager != null && !string.IsNullOrEmpty(gameManager.CurrentHeroineId))
        {
            catalog = Resources.Load<HeroineAssetCatalog>(
                "Heroines/" + gameManager.CurrentHeroineId + "/HeroineAssetCatalog");
        }

        if (catalog == null || catalog.assets == null || catalog.assets.Count == 0)
        {
            return null;
        }

        return ResolveHeroineCatalogSprite(catalog, "Battle_Heroine_Idle");
    }

    private static Sprite ResolveEnemySprite(EnemyData enemy)
    {
        if (enemy == null || string.IsNullOrEmpty(enemy.enemyId))
        {
            return null;
        }

        EnemyAssetCatalog catalog = Resources.Load<EnemyAssetCatalog>(
            "Enemies/" + enemy.enemyId + "/EnemyAssetCatalog");
        if (catalog == null || catalog.assets == null || catalog.assets.Count == 0)
        {
            return null;
        }

        string idleAssetId = "Enemy_" + enemy.enemyId + "_Idle";
        for (int i = 0; i < catalog.assets.Count; i++)
        {
            EnemyAssetEntry entry = catalog.assets[i];
            if (entry != null && entry.sprite != null && entry.assetId == idleAssetId)
            {
                return entry.sprite;
            }
        }

        for (int i = 0; i < catalog.assets.Count; i++)
        {
            EnemyAssetEntry entry = catalog.assets[i];
            if (entry != null &&
                entry.sprite != null &&
                string.Equals(entry.usage, "Battle", System.StringComparison.OrdinalIgnoreCase))
            {
                return entry.sprite;
            }
        }

        for (int i = 0; i < catalog.assets.Count; i++)
        {
            EnemyAssetEntry entry = catalog.assets[i];
            if (entry != null && entry.sprite != null)
            {
                return entry.sprite;
            }
        }

        return null;
    }

    private static Sprite ResolvePlayerCatalogSprite(PlayerAssetCatalog catalog, string preferredAssetId)
    {
        for (int i = 0; i < catalog.assets.Count; i++)
        {
            PlayerAssetEntry entry = catalog.assets[i];
            if (entry != null && entry.sprite != null && entry.assetId == preferredAssetId)
            {
                return entry.sprite;
            }
        }

        for (int i = 0; i < catalog.assets.Count; i++)
        {
            PlayerAssetEntry entry = catalog.assets[i];
            if (entry != null &&
                entry.sprite != null &&
                string.Equals(entry.usage, "Battle", System.StringComparison.OrdinalIgnoreCase))
            {
                return entry.sprite;
            }
        }

        for (int i = 0; i < catalog.assets.Count; i++)
        {
            PlayerAssetEntry entry = catalog.assets[i];
            if (entry != null && entry.sprite != null)
            {
                return entry.sprite;
            }
        }

        return null;
    }

    private static Sprite ResolveHeroineCatalogSprite(HeroineAssetCatalog catalog, string preferredAssetId)
    {
        for (int i = 0; i < catalog.assets.Count; i++)
        {
            HeroineAssetEntry entry = catalog.assets[i];
            if (entry != null && entry.sprite != null && entry.assetId == preferredAssetId)
            {
                return entry.sprite;
            }
        }

        for (int i = 0; i < catalog.assets.Count; i++)
        {
            HeroineAssetEntry entry = catalog.assets[i];
            if (entry != null &&
                entry.sprite != null &&
                string.Equals(entry.usage, "Battle", System.StringComparison.OrdinalIgnoreCase))
            {
                return entry.sprite;
            }
        }

        for (int i = 0; i < catalog.assets.Count; i++)
        {
            HeroineAssetEntry entry = catalog.assets[i];
            if (entry != null && entry.sprite != null)
            {
                return entry.sprite;
            }
        }

        return null;
    }

    private void HookButtons()
    {
        if (attackButton != null)
        {
            attackButton.onClick.RemoveListener(OnAttackClicked);
            attackButton.onClick.AddListener(OnAttackClicked);
        }

        if (escapeButton != null)
        {
            escapeButton.onClick.RemoveListener(OnEscapeClicked);
            escapeButton.onClick.AddListener(OnEscapeClicked);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(Close);
            closeButton.onClick.AddListener(Close);
        }
    }

    private void EnsureReferences()
    {
        if (panelRoot == null)
        {
            panelRoot = gameObject;
        }
    }

    private static void SetHpText(TextMeshProUGUI text, string label, BattleStatusData status)
    {
        if (text == null)
        {
            return;
        }

        if (status == null)
        {
            text.text = label + ": -";
            return;
        }

        text.text = label + ": " + status.currentHp + "/" + status.maxHp;
    }

    private static int Damage(BattleStatusData attacker, BattleStatusData defender)
    {
        if (attacker == null || defender == null)
        {
            return 0;
        }

        int baseDamage = Mathf.Max(1, attacker.attack - defender.defense);
        int variance = Random.Range(0, 3);
        int damage = Mathf.Max(1, baseDamage + variance);
        int before = defender.currentHp;
        defender.currentHp -= damage;
        defender.Clamp();
        return before - defender.currentHp;
    }

    private static bool IsDefeated(BattleStatusData status)
    {
        return status == null || status.currentHp <= 0;
    }

    private static BattleStatusData CreateDefaultPlayerStatus()
    {
        return new BattleStatusData
        {
            currentHp = 100,
            maxHp = 100,
            attack = 10,
            defense = 5,
            speed = 5
        };
    }

    private static BattleStatusData CreateDefaultEnemyStatus()
    {
        return new BattleStatusData
        {
            currentHp = 24,
            maxHp = 24,
            attack = 5,
            defense = 2,
            speed = 3
        };
    }
}
