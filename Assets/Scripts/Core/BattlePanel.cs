using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattlePanel : MonoBehaviour
{
    public enum BattleResultType
    {
        None,
        Victory,
        Defeat,
        Escape
    }

    public class BattleResult
    {
        public BattleResultType resultType;
        public string resultLabel;
        public string enemyId;
        public string enemyName;
        public int rewardMoney;
        public int affectionChangeOnWin;
        public int turnCount;
        public BattleStatusData playerStatus;
        public BattleStatusData heroineStatus;
        public BattleStatusData enemyStatus;
    }

    private const string BattleSpriteIdle = "Idle";
    private const string BattleSpriteAttack = "Attack";
    private const string BattleSpriteDamage = "Damage";
    private const string BattleSpriteVictory = "Victory";
    private const string BattleSpriteDefeat = "Defeat";
    private const int DebugHealAmount = 20;

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
    [SerializeField] private Button defendButton;
    [SerializeField] private Button healButton;
    [SerializeField] private Button skillButton;
    [SerializeField] private BattleSkillPanel battleSkillPanel;
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
    private bool battleResultNotified;

    private void Awake()
    {
        EnsureReferences();
        EnsureBattleSkillPanel();
        HookButtons();
    }

    public void Initialize(GameManager manager, PlayerStatus player, HeroineStatus heroine)
    {
        gameManager = manager;
        playerStatus = player;
        heroineStatus = heroine;
        EnsureReferences();
        EnsureBattleSkillPanel();
        HookButtons();
    }

    public void OpenDebugBattle()
    {
        EnsureReferences();
        HookButtons();

        OpenBattle(ResolveDebugEnemy(), true);
    }

    public void OpenBattle(EnemyData enemy)
    {
        OpenBattle(enemy, true);
    }

    public void OpenBattle(EnemyData enemy, bool includeHeroine)
    {
        EnsureReferences();
        HookButtons();

        currentDebugEnemy = enemy;
        enemyDisplayName = currentDebugEnemy != null ? currentDebugEnemy.GetDisplayName() : "デバッグ敵";
        debugEnemyStatus = currentDebugEnemy != null ? currentDebugEnemy.CreateBattleStatus() : CreateDefaultEnemyStatus();
        debugPlayerStatus = playerStatus != null && playerStatus.BattleStatus != null
            ? playerStatus.BattleStatus.Clone()
            : CreateDefaultPlayerStatus();
        debugHeroineStatus = includeHeroine && heroineStatus != null && heroineStatus.BattleStatus != null
            ? heroineStatus.BattleStatus.Clone()
            : null;
        debugPlayerStatus.RestoreMp();
        if (debugHeroineStatus != null)
        {
            debugHeroineStatus.RestoreMp();
        }
        ApplyPlayerImage(BattleSpriteIdle);
        ApplyHeroineImage(BattleSpriteIdle);
        ApplyEnemyImage(currentDebugEnemy, BattleSpriteIdle);
        turnCount = 0;
        battleFinished = false;
        battleResultNotified = false;

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
        if (battleSkillPanel != null)
        {
            battleSkillPanel.Close();
        }

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
        ApplyPlayerImage(BattleSpriteIdle);
        ApplyHeroineImage(BattleSpriteIdle);
        ApplyEnemyImage(currentDebugEnemy, BattleSpriteIdle);

        ApplyPlayerImage(BattleSpriteAttack);
        ApplyEnemyImage(currentDebugEnemy, BattleSpriteDamage);
        int playerDamage = Damage(debugPlayerStatus, debugEnemyStatus);
        AddLog("プレイヤーの攻撃。 " + enemyDisplayName + " に " + playerDamage + " ダメージ。");

        if (IsDefeated(debugEnemyStatus))
        {
            FinishBattle("勝利", ResolveVictoryMessage());
            return;
        }

        if (debugHeroineStatus != null && !IsDefeated(debugHeroineStatus))
        {
            ApplyPlayerImage(BattleSpriteIdle);
            ApplyHeroineImage(BattleSpriteAttack);
            ApplyEnemyImage(currentDebugEnemy, BattleSpriteDamage);
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

    private void OnDefendClicked()
    {
        if (battleFinished)
        {
            return;
        }

        turnCount++;
        AddLog("--- " + turnCount + "ターン目 ---");
        ApplyPlayerImage(BattleSpriteIdle);
        ApplyHeroineImage(BattleSpriteIdle);
        ApplyEnemyImage(currentDebugEnemy, BattleSpriteIdle);

        AddLog("プレイヤーは防御した。");

        if (debugHeroineStatus != null && !IsDefeated(debugHeroineStatus))
        {
            ApplyHeroineImage(BattleSpriteAttack);
            ApplyEnemyImage(currentDebugEnemy, BattleSpriteDamage);
            int heroineDamage = Damage(debugHeroineStatus, debugEnemyStatus);
            string heroineName = heroineStatus != null ? heroineStatus.HeroineName : "ヒロイン";
            AddLog(heroineName + " の攻撃。 " + enemyDisplayName + " に " + heroineDamage + " ダメージ。");

            if (IsDefeated(debugEnemyStatus))
            {
                FinishBattle("勝利", ResolveVictoryMessage());
                return;
            }
        }

        ApplyEnemyAttack(true);
        if (IsDefeated(debugPlayerStatus))
        {
            FinishBattle("敗北", ResolveDefeatMessage());
            return;
        }

        AddHpSummaryLog();
        Refresh();
    }

    private void OnHealClicked()
    {
        if (battleFinished)
        {
            return;
        }

        turnCount++;
        AddLog("--- " + turnCount + "ターン目 ---");
        ApplyPlayerImage(BattleSpriteIdle);
        ApplyHeroineImage(BattleSpriteIdle);
        ApplyEnemyImage(currentDebugEnemy, BattleSpriteIdle);

        BattleStatusData target = ResolveHealTarget();
        string targetName = ResolveHealTargetName(target);
        int recovered = Recover(target, DebugHealAmount);
        if (recovered > 0)
        {
            AddLog(targetName + " は " + recovered + " 回復した。");
        }
        else
        {
            AddLog("回復できる対象がいません。");
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

    private void OnSkillClicked()
    {
        if (battleFinished)
        {
            return;
        }

        if (gameManager == null)
        {
            AddLog("使用できる戦闘スキルがありません。訓練でスキルを解放してください。");
            Refresh();
            return;
        }

        List<SkillData> skills = gameManager.GetUnlockedBattleSkills();
        if (skills.Count == 0)
        {
            AddLog("使用できる戦闘スキルがありません。訓練でスキルを解放してください。");
            Refresh();
            return;
        }

        EnsureBattleSkillPanel();
        if (battleSkillPanel != null)
        {
            battleSkillPanel.Open(
                skills,
                debugPlayerStatus != null ? debugPlayerStatus.currentMp : 0,
                debugPlayerStatus != null ? debugPlayerStatus.maxMp : 0,
                UseSelectedSkill);
            return;
        }

        // Keeps the existing generic selector usable until the dedicated panel is placed in the scene.
        if (!gameManager.TryOpenBattleSkillSelection(UseSelectedSkill))
        {
            AddLog("戦闘スキル選択 UI が設定されていません。");
            Refresh();
        }
    }

    private void UseSelectedSkill(SkillData skill)
    {
        if (battleFinished || skill == null)
        {
            return;
        }

        int skillCost = Mathf.Max(0, skill.cost);
        if (debugPlayerStatus == null || !debugPlayerStatus.TrySpendMp(skillCost))
        {
            AddLog(
                "MP が足りないため " +
                skill.GetDisplayName() +
                " を使えない。必要 MP: " +
                skillCost);
            Refresh();
            return;
        }

        turnCount++;
        AddLog("--- " + turnCount + "ターン目 ---");
        ApplyPlayerImage(BattleSpriteIdle);
        ApplyHeroineImage(BattleSpriteIdle);
        ApplyEnemyImage(currentDebugEnemy, BattleSpriteIdle);

        AddLog(
            "プレイヤーは " +
            skill.GetDisplayName() +
            " を使った。MP " +
            skillCost +
            " 消費。");

        bool playerDefending = false;
        bool heroineAttacks = true;
        switch (skill.effectType)
        {
            case SkillEffectType.Damage:
            {
                ApplyPlayerImage(BattleSpriteAttack);
                ApplyEnemyImage(currentDebugEnemy, BattleSpriteDamage);
                int playerDamage = DamageWithSkill(debugPlayerStatus, debugEnemyStatus, skill.power);
                AddLog(enemyDisplayName + " に " + playerDamage + " ダメージ。");
                break;
            }
            case SkillEffectType.Heal:
            {
                BattleStatusData healTarget = ResolveSkillHealTarget(skill.targetType);
                int recovered = Recover(healTarget, Mathf.Max(1, skill.power));
                AddLog(ResolveHealTargetName(healTarget) + " は " + recovered + " 回復した。");
                heroineAttacks = false;
                break;
            }
            case SkillEffectType.Guard:
            {
                playerDefending = true;
                AddLog("次の敵の攻撃を防御する。");
                heroineAttacks = false;
                break;
            }
            case SkillEffectType.Buff:
            {
                BattleStatusData buffTarget = ResolveSkillStatTarget(skill.targetType, true);
                int appliedValue = ApplyBattleStatModifier(buffTarget, skill.affectedStat, Mathf.Max(1, skill.power));
                AddLog(
                    ResolveSkillStatTargetName(buffTarget) +
                    " の " +
                    GetBattleStatDisplayName(skill.affectedStat) +
                    " が " +
                    appliedValue +
                    " 上がった。");
                heroineAttacks = false;
                break;
            }
            case SkillEffectType.Debuff:
            {
                BattleStatusData debuffTarget = ResolveSkillStatTarget(skill.targetType, false);
                int appliedValue = ApplyBattleStatModifier(debuffTarget, skill.affectedStat, -Mathf.Max(1, skill.power));
                AddLog(
                    ResolveSkillStatTargetName(debuffTarget) +
                    " の " +
                    GetBattleStatDisplayName(skill.affectedStat) +
                    " が " +
                    Mathf.Abs(appliedValue) +
                    " 下がった。");
                heroineAttacks = false;
                break;
            }
            default:
                AddLog("このスキル効果はまだ戦闘で使えません。");
                Refresh();
                return;
        }

        if (IsDefeated(debugEnemyStatus))
        {
            FinishBattle("勝利", ResolveVictoryMessage());
            return;
        }

        if (heroineAttacks && debugHeroineStatus != null && !IsDefeated(debugHeroineStatus))
        {
            ApplyPlayerImage(BattleSpriteIdle);
            ApplyHeroineImage(BattleSpriteAttack);
            ApplyEnemyImage(currentDebugEnemy, BattleSpriteDamage);
            int heroineDamage = Damage(debugHeroineStatus, debugEnemyStatus);
            string heroineName = heroineStatus != null ? heroineStatus.HeroineName : "ヒロイン";
            AddLog(heroineName + " の攻撃。 " + enemyDisplayName + " に " + heroineDamage + " ダメージ。");

            if (IsDefeated(debugEnemyStatus))
            {
                FinishBattle("勝利", ResolveVictoryMessage());
                return;
            }
        }

        ApplyEnemyAttack(playerDefending);
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

    private void ApplyEnemyAttack(bool playerDefending = false)
    {
        bool canAttackHeroine = debugHeroineStatus != null && !IsDefeated(debugHeroineStatus);
        if (canAttackHeroine && Random.Range(0, 2) == 0)
        {
            ApplyEnemyImage(currentDebugEnemy, BattleSpriteAttack);
            ApplyHeroineImage(BattleSpriteDamage);
            int heroineDamage = Damage(debugEnemyStatus, debugHeroineStatus);
            string heroineName = heroineStatus != null ? heroineStatus.HeroineName : "ヒロイン";
            AddLog(enemyDisplayName + " の攻撃。 " + heroineName + " は " + heroineDamage + " ダメージ。");
            if (IsDefeated(debugHeroineStatus))
            {
                ApplyHeroineImage(BattleSpriteDefeat);
                AddLog(heroineName + " は戦闘不能です。");
            }

            return;
        }

        ApplyEnemyImage(currentDebugEnemy, BattleSpriteAttack);
        ApplyPlayerImage(BattleSpriteDamage);
        int playerDamage = Damage(debugEnemyStatus, debugPlayerStatus, playerDefending);
        string defendMessage = playerDefending ? "（防御）" : "";
        AddLog(enemyDisplayName + " の攻撃。 プレイヤーは " + playerDamage + " ダメージ。" + defendMessage);
    }

    private void FinishBattle(string resultLabel, string message)
    {
        battleFinished = true;
        ApplyBattleResultImages(resultLabel);
        AddLog(message);
        AddLog("戦闘結果：" + resultLabel);
        AddHpSummaryLog();
        NotifyBattleResult(resultLabel);
        Refresh();
    }

    private void NotifyBattleResult(string resultLabel)
    {
        if (battleResultNotified || gameManager == null)
        {
            return;
        }

        battleResultNotified = true;
        gameManager.OnBattlePanelResult(CreateBattleResult(resultLabel));
    }

    private BattleResult CreateBattleResult(string resultLabel)
    {
        return new BattleResult
        {
            resultType = ResolveBattleResultType(resultLabel),
            resultLabel = resultLabel,
            enemyId = currentDebugEnemy != null ? currentDebugEnemy.enemyId : "",
            enemyName = enemyDisplayName,
            rewardMoney = currentDebugEnemy != null ? currentDebugEnemy.rewardMoney : 0,
            affectionChangeOnWin = currentDebugEnemy != null ? currentDebugEnemy.affectionChangeOnWin : 0,
            turnCount = turnCount,
            playerStatus = debugPlayerStatus != null ? debugPlayerStatus.Clone() : null,
            heroineStatus = debugHeroineStatus != null ? debugHeroineStatus.Clone() : null,
            enemyStatus = debugEnemyStatus != null ? debugEnemyStatus.Clone() : null
        };
    }

    private static BattleResultType ResolveBattleResultType(string resultLabel)
    {
        if (resultLabel == "勝利")
        {
            return BattleResultType.Victory;
        }

        if (resultLabel == "敗北")
        {
            return BattleResultType.Defeat;
        }

        if (resultLabel == "撤退")
        {
            return BattleResultType.Escape;
        }

        return BattleResultType.None;
    }

    private void ApplyBattleResultImages(string resultLabel)
    {
        if (resultLabel == "勝利")
        {
            ApplyPlayerImage(BattleSpriteVictory);
            ApplyHeroineImage(BattleSpriteVictory);
            ApplyEnemyImage(currentDebugEnemy, BattleSpriteDefeat);
            return;
        }

        if (resultLabel == "敗北")
        {
            ApplyPlayerImage(BattleSpriteDefeat);
            ApplyHeroineImage(BattleSpriteDefeat);
            ApplyEnemyImage(currentDebugEnemy, BattleSpriteIdle);
            return;
        }

        ApplyPlayerImage(BattleSpriteIdle);
        ApplyHeroineImage(BattleSpriteIdle);
        ApplyEnemyImage(currentDebugEnemy, BattleSpriteIdle);
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

        if (defendButton != null)
        {
            defendButton.interactable = !battleFinished;
        }

        if (healButton != null)
        {
            healButton.interactable = !battleFinished;
        }

        if (skillButton != null)
        {
            skillButton.interactable = !battleFinished;
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

        if (closeButton != null)
        {
            closeButton.gameObject.SetActive(battleFinished);
            closeButton.interactable = battleFinished;
        }
    }

    private void AddHpSummaryLog()
    {
        AddLog(
            "HP: プレイヤー " + FormatHp(debugPlayerStatus) +
            " MP " + FormatMp(debugPlayerStatus) +
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

    private static string FormatMp(BattleStatusData status)
    {
        if (status == null)
        {
            return "-";
        }

        return status.currentMp + "/" + status.maxMp;
    }

    private BattleStatusData ResolveHealTarget()
    {
        if (CanRecover(debugPlayerStatus))
        {
            return debugPlayerStatus;
        }

        if (CanRecover(debugHeroineStatus))
        {
            return debugHeroineStatus;
        }

        return debugPlayerStatus;
    }

    private BattleStatusData ResolveSkillHealTarget(SkillTargetType targetType)
    {
        if (targetType == SkillTargetType.Self)
        {
            return debugPlayerStatus;
        }

        if (targetType == SkillTargetType.Ally || targetType == SkillTargetType.AllAllies)
        {
            if (GetMissingHp(debugHeroineStatus) > GetMissingHp(debugPlayerStatus))
            {
                return debugHeroineStatus;
            }
        }

        return ResolveHealTarget();
    }

    private BattleStatusData ResolveSkillStatTarget(SkillTargetType targetType, bool isBuff)
    {
        if (!isBuff && (targetType == SkillTargetType.Enemy || targetType == SkillTargetType.AllEnemies))
        {
            return debugEnemyStatus;
        }

        if (isBuff &&
            (targetType == SkillTargetType.Ally || targetType == SkillTargetType.AllAllies) &&
            debugHeroineStatus != null &&
            !IsDefeated(debugHeroineStatus))
        {
            return debugHeroineStatus;
        }

        return debugPlayerStatus;
    }

    private string ResolveSkillStatTargetName(BattleStatusData target)
    {
        if (target == debugEnemyStatus)
        {
            return enemyDisplayName;
        }

        if (target == debugHeroineStatus)
        {
            return heroineStatus != null ? heroineStatus.HeroineName : "ヒロイン";
        }

        return "プレイヤー";
    }

    private string ResolveHealTargetName(BattleStatusData target)
    {
        if (target == debugHeroineStatus)
        {
            return heroineStatus != null ? heroineStatus.HeroineName : "ヒロイン";
        }

        return "プレイヤー";
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

    private void ApplyEnemyImage(EnemyData enemy, string state)
    {
        if (enemyImage == null)
        {
            return;
        }

        Sprite enemySprite = ResolveEnemySprite(enemy, state);
        enemyImage.sprite = enemySprite;
        enemyImage.enabled = enemySprite != null;
        enemyImage.preserveAspect = true;
    }

    private void ApplyPlayerImage(string state)
    {
        if (playerImage == null)
        {
            return;
        }

        Sprite playerSprite = ResolvePlayerSprite(state);
        playerImage.sprite = playerSprite;
        playerImage.enabled = playerSprite != null;
        playerImage.preserveAspect = true;
    }

    private void ApplyHeroineImage(string state)
    {
        if (heroineImage == null)
        {
            return;
        }

        if (debugHeroineStatus == null)
        {
            heroineImage.sprite = null;
            heroineImage.enabled = false;
            return;
        }

        Sprite heroineSprite = ResolveHeroineSprite(state);
        heroineImage.sprite = heroineSprite;
        heroineImage.enabled = heroineSprite != null;
        heroineImage.preserveAspect = true;
    }

    private static Sprite ResolvePlayerSprite(string state)
    {
        PlayerAssetCatalog catalog = Resources.Load<PlayerAssetCatalog>("Player/PlayerAssetCatalog");
        if (catalog == null || catalog.assets == null || catalog.assets.Count == 0)
        {
            return null;
        }

        return ResolvePlayerCatalogSprite(catalog, "Battle_Player_" + state, "Battle_Player_Idle");
    }

    private Sprite ResolveHeroineSprite(string state)
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

        return ResolveHeroineCatalogSprite(catalog, "Battle_Heroine_" + state, "Battle_Heroine_Idle");
    }

    private static Sprite ResolveEnemySprite(EnemyData enemy, string state)
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

        string stateAssetId = "Enemy_" + enemy.enemyId + "_" + state;
        string idleAssetId = "Enemy_" + enemy.enemyId + "_" + BattleSpriteIdle;
        for (int i = 0; i < catalog.assets.Count; i++)
        {
            EnemyAssetEntry entry = catalog.assets[i];
            if (entry != null && entry.sprite != null && entry.assetId == stateAssetId)
            {
                return entry.sprite;
            }
        }

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

    private static Sprite ResolvePlayerCatalogSprite(PlayerAssetCatalog catalog, string preferredAssetId, string fallbackAssetId)
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
            if (entry != null && entry.sprite != null && entry.assetId == fallbackAssetId)
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

    private static Sprite ResolveHeroineCatalogSprite(HeroineAssetCatalog catalog, string preferredAssetId, string fallbackAssetId)
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
            if (entry != null && entry.sprite != null && entry.assetId == fallbackAssetId)
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

        if (defendButton != null)
        {
            defendButton.onClick.RemoveListener(OnDefendClicked);
            defendButton.onClick.AddListener(OnDefendClicked);
        }

        if (healButton != null)
        {
            healButton.onClick.RemoveListener(OnHealClicked);
            healButton.onClick.AddListener(OnHealClicked);
        }

        if (skillButton != null)
        {
            skillButton.onClick.RemoveListener(OnSkillClicked);
            skillButton.onClick.AddListener(OnSkillClicked);
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

    private void EnsureBattleSkillPanel()
    {
        if (battleSkillPanel == null)
        {
            battleSkillPanel = FindObjectOfType<BattleSkillPanel>(true);
        }

        if (battleSkillPanel != null)
        {
            battleSkillPanel.Initialize();
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

    private static int Damage(
        BattleStatusData attacker,
        BattleStatusData defender,
        bool defenderGuarding = false,
        float attackMultiplier = 1f)
    {
        if (attacker == null || defender == null)
        {
            return 0;
        }

        int attack = Mathf.Max(1, Mathf.RoundToInt(attacker.attack * attackMultiplier));
        int baseDamage = Mathf.Max(1, attack - defender.defense);
        int variance = Random.Range(0, 3);
        int damage = Mathf.Max(1, baseDamage + variance);
        if (defenderGuarding)
        {
            damage = Mathf.Max(1, Mathf.CeilToInt(damage * 0.5f));
        }

        int before = defender.currentHp;
        defender.currentHp -= damage;
        defender.Clamp();
        return before - defender.currentHp;
    }

    private static int Recover(BattleStatusData target, int amount)
    {
        if (target == null || amount <= 0 || target.currentHp <= 0)
        {
            return 0;
        }

        int before = target.currentHp;
        target.currentHp += amount;
        target.Clamp();
        return target.currentHp - before;
    }

    private static int DamageWithSkill(BattleStatusData attacker, BattleStatusData defender, int power)
    {
        if (attacker == null || defender == null)
        {
            return 0;
        }

        int attack = Mathf.Max(1, attacker.attack + Mathf.Max(0, power));
        int baseDamage = Mathf.Max(1, attack - defender.defense);
        int variance = Random.Range(0, 3);
        int damage = Mathf.Max(1, baseDamage + variance);
        int before = defender.currentHp;
        defender.currentHp -= damage;
        defender.Clamp();
        return before - defender.currentHp;
    }

    private static int ApplyBattleStatModifier(BattleStatusData target, SkillBattleStat stat, int value)
    {
        if (target == null || value == 0)
        {
            return 0;
        }

        int before;
        switch (stat)
        {
            case SkillBattleStat.Defense:
                before = target.defense;
                target.defense = Mathf.Max(0, target.defense + value);
                return target.defense - before;
            case SkillBattleStat.Speed:
                before = target.speed;
                target.speed = Mathf.Max(0, target.speed + value);
                return target.speed - before;
            case SkillBattleStat.Attack:
            default:
                before = target.attack;
                target.attack = Mathf.Max(0, target.attack + value);
                return target.attack - before;
        }
    }

    private static string GetBattleStatDisplayName(SkillBattleStat stat)
    {
        switch (stat)
        {
            case SkillBattleStat.Defense:
                return "防御";
            case SkillBattleStat.Speed:
                return "素早さ";
            case SkillBattleStat.Attack:
            default:
                return "攻撃";
        }
    }

    private static int GetMissingHp(BattleStatusData status)
    {
        if (status == null)
        {
            return -1;
        }

        return Mathf.Max(0, status.maxHp - status.currentHp);
    }

    private static bool CanRecover(BattleStatusData status)
    {
        return status != null && status.currentHp > 0 && status.currentHp < status.maxHp;
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
