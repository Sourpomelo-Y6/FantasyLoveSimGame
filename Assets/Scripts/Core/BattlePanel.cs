using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattlePanel : MonoBehaviour
{
    private const int CombatantNameDisplayMaxLength = 8;
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
    private const string ManaPotionItemId = "ManaPotion";

    [SerializeField] private GameManager gameManager;
    [SerializeField] private PlayerStatus playerStatus;
    [SerializeField] private HeroineStatus heroineStatus;
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TextMeshProUGUI enemyNameText;
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private TextMeshProUGUI heroineNameText;
    [SerializeField] private TextMeshProUGUI enemyHpText;
    [SerializeField] private TextMeshProUGUI playerHpText;
    [SerializeField] private TextMeshProUGUI heroineHpText;
    [SerializeField] private TextMeshProUGUI enemyMpText;
    [SerializeField] private TextMeshProUGUI playerMpText;
    [SerializeField] private TextMeshProUGUI heroineMpText;
    [SerializeField] private TextMeshProUGUI battleLogText;
    [SerializeField] private Image playerImage;
    [SerializeField] private Image heroineImage;
    [SerializeField] private Image enemyImage;
    [SerializeField] private Button attackButton;
    [SerializeField] private Button defendButton;
    [SerializeField] private Button healButton;
    [SerializeField] private Button skillButton;
    [SerializeField] private Button itemButton;
    [SerializeField] private BattleSkillPanel battleSkillPanel;
    [SerializeField] private Button statusButton;
    [SerializeField] private Button escapeButton;
    [SerializeField] private Button closeButton;
    [Header("Status Effect UI")]
    [SerializeField] private GameObject statusEffectPanel;
    [SerializeField] private Button closeStatusEffectButton;
    [SerializeField] private Transform playerStatusEffectList;
    [SerializeField] private Transform heroineStatusEffectList;
    [SerializeField] private Transform enemyStatusEffectList;
    [SerializeField] private GameObject heroineEffectSection;
    [SerializeField] private TextMeshProUGUI heroineEffectSectionNameText;
    [SerializeField] private TextMeshProUGUI enemyEffectSectionNameText;
    [SerializeField] private GameObject statusEffectRowPrefab;
    [SerializeField] private Color buffEffectColor = new Color(0.25f, 0.55f, 0.9f, 0.85f);
    [SerializeField] private Color debuffEffectColor = new Color(0.8f, 0.3f, 0.45f, 0.85f);
    [SerializeField] private Color noEffectColor = new Color(0.35f, 0.35f, 0.35f, 0.7f);
    [SerializeField] private EnemyData debugEnemy;
    [SerializeField] private string debugEnemyResourcePath = "Enemies/ForestSlime";
    [SerializeField] private int maxLogLines = 6;

    private BattleStatusData debugPlayerStatus;
    private BattleStatusData debugHeroineStatus;
    private BattleStatusData debugEnemyStatus;
    private EnemyData currentDebugEnemy;
    private string enemyDisplayName = "敵";
    private readonly List<string> logLines = new List<string>();
    private readonly Dictionary<string, int> enemySkillUseCounts = new Dictionary<string, int>();
    private readonly Dictionary<string, int> heroineSkillUseCounts = new Dictionary<string, int>();
    private readonly List<BattleStatusEffect> playerStatusEffects = new List<BattleStatusEffect>();
    private readonly List<BattleStatusEffect> heroineStatusEffects = new List<BattleStatusEffect>();
    private readonly List<BattleStatusEffect> enemyStatusEffects = new List<BattleStatusEffect>();
    private readonly List<GameObject> spawnedStatusEffectRows = new List<GameObject>();
    private int turnCount;
    private bool battleFinished;
    private bool battleResultNotified;

    private void Awake()
    {
        EnsureReferences();
        EnsureBattleSkillPanel();
        EnsureStatusEffectUi();
        HookButtons();
    }

    public void Initialize(GameManager manager, PlayerStatus player, HeroineStatus heroine)
    {
        gameManager = manager;
        playerStatus = player;
        heroineStatus = heroine;
        EnsureReferences();
        EnsureBattleSkillPanel();
        EnsureStatusEffectUi();
        HookButtons();
    }

    public void OpenDebugBattle()
    {
        EnsureReferences();
        EnsureStatusEffectUi();
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
        debugEnemyStatus.RestoreMp();
        ApplyPlayerImage(BattleSpriteIdle);
        ApplyHeroineImage(BattleSpriteIdle);
        ApplyEnemyImage(currentDebugEnemy, BattleSpriteIdle);
        turnCount = 0;
        battleFinished = false;
        battleResultNotified = false;
        enemySkillUseCounts.Clear();
        heroineSkillUseCounts.Clear();
        playerStatusEffects.Clear();
        heroineStatusEffects.Clear();
        enemyStatusEffects.Clear();
        CloseStatusEffectPanel();

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

        CloseStatusEffectPanel();

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

        if (ApplyHeroineTurn())
        {
            FinishBattle("勝利", ResolveVictoryMessage());
            return;
        }

        TickStatusEffects(debugPlayerStatus);
        ApplyEnemyTurn();
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

        if (ApplyHeroineTurn())
        {
            FinishBattle("勝利", ResolveVictoryMessage());
            return;
        }

        TickStatusEffects(debugPlayerStatus);
        ApplyEnemyTurn(true);
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

        TickStatusEffects(debugPlayerStatus);
        ApplyEnemyTurn();
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

    private void OnItemClicked()
    {
        if (battleFinished || gameManager == null)
        {
            return;
        }

        BattleStatusData target = GetMissingMp(debugHeroineStatus) > GetMissingMp(debugPlayerStatus)
            ? debugHeroineStatus
            : debugPlayerStatus;
        if (target == null || GetMissingMp(target) <= 0)
        {
            AddLog("MP を回復する必要がありません。");
            Refresh();
            return;
        }

        if (!gameManager.TryConsumeBattleItem(ManaPotionItemId, out ShopItemData item))
        {
            AddLog("マナポーションを所持していません。");
            Refresh();
            return;
        }

        int before = target.currentMp;
        target.currentMp += Mathf.Max(1, item.mpRecoveryAmount);
        target.Clamp();
        string itemName = !string.IsNullOrEmpty(item.displayName) ? item.displayName : item.itemId;
        AddLog(ResolveBattleStatusTargetName(target) + " は " + itemName + " を使い、MP を " + (target.currentMp - before) + " 回復した。");
        ApplyEnemyTurn();
        AddHpSummaryLog();
        Refresh();
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
                ApplyStatusEffect(
                    buffTarget,
                    skill.skillId,
                    skill.GetDisplayName(),
                    skill.affectedStat,
                    Mathf.Max(1, skill.power),
                    skill.statusDurationTurns,
                    true);
                heroineAttacks = false;
                break;
            }
            case SkillEffectType.Debuff:
            {
                BattleStatusData debuffTarget = ResolveSkillStatTarget(skill.targetType, false);
                ApplyStatusEffect(
                    debuffTarget,
                    skill.skillId,
                    skill.GetDisplayName(),
                    skill.affectedStat,
                    -Mathf.Max(1, skill.power),
                    skill.statusDurationTurns,
                    false);
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

        if (heroineAttacks && ApplyHeroineTurn())
        {
            FinishBattle("勝利", ResolveVictoryMessage());
            return;
        }

        TickStatusEffects(debugPlayerStatus);
        ApplyEnemyTurn(playerDefending);
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

    private bool ApplyHeroineTurn()
    {
        if (debugHeroineStatus == null || IsDefeated(debugHeroineStatus))
        {
            return false;
        }

        HeroineBattleSkillData skill = SelectHeroineSkill();
        if (skill != null)
        {
            ExecuteHeroineSkill(skill);
        }
        else
        {
            ApplyHeroineBasicAttack();
        }

        TickStatusEffects(debugHeroineStatus);
        return IsDefeated(debugEnemyStatus);
    }

    private HeroineBattleSkillData SelectHeroineSkill()
    {
        HeroineProfileData profile = gameManager != null ? gameManager.CurrentHeroineProfile : null;
        if (profile == null || debugHeroineStatus == null)
        {
            return null;
        }

        List<HeroineBattleSkillData> candidates = new List<HeroineBattleSkillData>();
        int highestPriority = int.MinValue;
        foreach (HeroineBattleSkillData skill in profile.GetBattleSkills())
        {
            if (!CanHeroineUseSkill(skill) || Random.Range(0, 100) >= Mathf.Clamp(skill.useChancePercent, 0, 100))
            {
                continue;
            }

            if (skill.priority > highestPriority)
            {
                candidates.Clear();
                highestPriority = skill.priority;
            }

            if (skill.priority == highestPriority)
            {
                candidates.Add(skill);
            }
        }

        return candidates.Count > 0 ? candidates[Random.Range(0, candidates.Count)] : null;
    }

    private bool CanHeroineUseSkill(HeroineBattleSkillData skill)
    {
        if (skill == null || debugHeroineStatus == null || debugHeroineStatus.currentMp < Mathf.Max(0, skill.cost))
        {
            return false;
        }

        BattleStatusData target = ResolveHeroineSkillTarget(skill.target);
        if (target == null || IsDefeated(target))
        {
            return false;
        }

        if (skill.effectType == SkillEffectType.Heal && GetMissingHp(target) <= 0)
        {
            return false;
        }

        string skillKey = GetHeroineSkillKey(skill);
        return skill.maxUsesPerBattle <= 0 ||
            !heroineSkillUseCounts.TryGetValue(skillKey, out int usedCount) ||
            usedCount < skill.maxUsesPerBattle;
    }

    private void ExecuteHeroineSkill(HeroineBattleSkillData skill)
    {
        if (skill == null || debugHeroineStatus == null || !debugHeroineStatus.TrySpendMp(Mathf.Max(0, skill.cost)))
        {
            return;
        }

        string skillKey = GetHeroineSkillKey(skill);
        heroineSkillUseCounts.TryGetValue(skillKey, out int usedCount);
        heroineSkillUseCounts[skillKey] = usedCount + 1;

        string heroineName = heroineStatus != null && !string.IsNullOrEmpty(heroineStatus.HeroineName)
            ? heroineStatus.HeroineName
            : "ヒロイン";
        ApplyHeroineImage(BattleSpriteAttack);
        AddLog(
            heroineName +
            " は " +
            skill.GetDisplayName() +
            " を使った。MP " +
            Mathf.Max(0, skill.cost) +
            " 消費。");

        BattleStatusData target = ResolveHeroineSkillTarget(skill.target);
        switch (skill.effectType)
        {
            case SkillEffectType.Damage:
            {
                ApplyHeroineSkillTargetDamageImage(target);
                int damage = DamageWithSkill(debugHeroineStatus, target, skill.power);
                AddLog(ResolveBattleStatusTargetName(target) + " に " + damage + " ダメージ。");
                break;
            }
            case SkillEffectType.Heal:
            {
                int recovered = Recover(target, Mathf.Max(1, skill.power));
                AddLog(ResolveBattleStatusTargetName(target) + " は " + recovered + " 回復した。");
                break;
            }
            case SkillEffectType.Buff:
            {
                ApplyStatusEffect(
                    target,
                    skill.skillId,
                    skill.GetDisplayName(),
                    skill.affectedStat,
                    Mathf.Max(1, skill.power),
                    skill.statusDurationTurns,
                    target == debugHeroineStatus);
                break;
            }
            case SkillEffectType.Debuff:
            {
                ApplyStatusEffect(
                    target,
                    skill.skillId,
                    skill.GetDisplayName(),
                    skill.affectedStat,
                    -Mathf.Max(1, skill.power),
                    skill.statusDurationTurns,
                    target == debugHeroineStatus);
                break;
            }
            default:
                AddLog(heroineName + " のスキルは通常攻撃として処理された。");
                ApplyHeroineBasicAttack();
                break;
        }
    }

    private BattleStatusData ResolveHeroineSkillTarget(HeroineSkillTarget target)
    {
        switch (target)
        {
            case HeroineSkillTarget.Self:
                return debugHeroineStatus;
            case HeroineSkillTarget.Player:
                return debugPlayerStatus;
            case HeroineSkillTarget.LowestHpAlly:
                return GetMissingHp(debugPlayerStatus) > GetMissingHp(debugHeroineStatus)
                    ? debugPlayerStatus
                    : debugHeroineStatus;
            case HeroineSkillTarget.Enemy:
            default:
                return debugEnemyStatus;
        }
    }

    private void ApplyHeroineSkillTargetDamageImage(BattleStatusData target)
    {
        if (target == debugEnemyStatus)
        {
            ApplyEnemyImage(currentDebugEnemy, BattleSpriteDamage);
            return;
        }

        if (target == debugPlayerStatus)
        {
            ApplyPlayerImage(BattleSpriteDamage);
        }
    }

    private void ApplyHeroineBasicAttack()
    {
        ApplyPlayerImage(BattleSpriteIdle);
        ApplyHeroineImage(BattleSpriteAttack);
        ApplyEnemyImage(currentDebugEnemy, BattleSpriteDamage);
        int heroineDamage = Damage(debugHeroineStatus, debugEnemyStatus);
        string heroineName = heroineStatus != null ? heroineStatus.HeroineName : "ヒロイン";
        AddLog(heroineName + " の攻撃。 " + enemyDisplayName + " に " + heroineDamage + " ダメージ。");
    }

    private static string GetHeroineSkillKey(HeroineBattleSkillData skill)
    {
        if (skill == null)
        {
            return string.Empty;
        }

        return !string.IsNullOrEmpty(skill.skillId) ? skill.skillId : skill.GetDisplayName();
    }

    private void ApplyEnemyAttack(bool playerDefending = false)
    {
        EnemyBattleSkillData skill = SelectEnemySkill();
        if (skill != null)
        {
            ExecuteEnemySkill(skill, playerDefending);
            return;
        }

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

    private void ApplyEnemyTurn(bool playerDefending = false)
    {
        ApplyEnemyAttack(playerDefending);
        TickStatusEffects(debugEnemyStatus);

        if (!IsDefeated(debugPlayerStatus) && ShouldEnemyGainExtraAction())
        {
            AddLog(enemyDisplayName + " は素早さの差で追加行動した。");
            ApplyEnemyAttack(false);
            TickStatusEffects(debugEnemyStatus);
        }
    }

    private bool ShouldEnemyGainExtraAction()
    {
        if (debugEnemyStatus == null || debugPlayerStatus == null || IsDefeated(debugEnemyStatus))
        {
            return false;
        }

        int speedDifference = debugEnemyStatus.speed - debugPlayerStatus.speed;
        return speedDifference >= 4 && Random.Range(0, 100) < 30;
    }

    private EnemyBattleSkillData SelectEnemySkill()
    {
        if (currentDebugEnemy == null || debugEnemyStatus == null)
        {
            return null;
        }

        List<EnemyBattleSkillData> candidates = new List<EnemyBattleSkillData>();
        int highestPriority = int.MinValue;
        foreach (EnemyBattleSkillData skill in currentDebugEnemy.GetBattleSkills())
        {
            if (!CanEnemyUseSkill(skill) || Random.Range(0, 100) >= Mathf.Clamp(skill.useChancePercent, 0, 100))
            {
                continue;
            }

            if (skill.priority > highestPriority)
            {
                candidates.Clear();
                highestPriority = skill.priority;
            }

            if (skill.priority == highestPriority)
            {
                candidates.Add(skill);
            }
        }

        return candidates.Count > 0 ? candidates[Random.Range(0, candidates.Count)] : null;
    }

    private bool CanEnemyUseSkill(EnemyBattleSkillData skill)
    {
        if (skill == null || debugEnemyStatus == null || debugEnemyStatus.currentMp < Mathf.Max(0, skill.cost))
        {
            return false;
        }

        string skillKey = GetEnemySkillKey(skill);
        return skill.maxUsesPerBattle <= 0 ||
            !enemySkillUseCounts.TryGetValue(skillKey, out int usedCount) ||
            usedCount < skill.maxUsesPerBattle;
    }

    private void ExecuteEnemySkill(EnemyBattleSkillData skill, bool playerDefending)
    {
        if (skill == null || debugEnemyStatus == null || !debugEnemyStatus.TrySpendMp(Mathf.Max(0, skill.cost)))
        {
            return;
        }

        string skillKey = GetEnemySkillKey(skill);
        enemySkillUseCounts.TryGetValue(skillKey, out int usedCount);
        enemySkillUseCounts[skillKey] = usedCount + 1;

        ApplyEnemyImage(currentDebugEnemy, BattleSpriteAttack);
        AddLog(
            enemyDisplayName +
            " は " +
            skill.GetDisplayName() +
            " を使った。MP " +
            Mathf.Max(0, skill.cost) +
            " 消費。");

        switch (skill.effectType)
        {
            case SkillEffectType.Damage:
            {
                BattleStatusData target = ResolveEnemySkillTarget(skill.target);
                ApplyEnemySkillTargetDamageImage(target);
                int damage = DamageWithSkill(
                    debugEnemyStatus,
                    target,
                    skill.power,
                    target == debugPlayerStatus && playerDefending);
                AddLog(ResolveEnemySkillTargetName(target) + " に " + damage + " ダメージ。");
                break;
            }
            case SkillEffectType.Heal:
            {
                BattleStatusData target = ResolveEnemySkillTarget(skill.target);
                int recovered = Recover(target, Mathf.Max(1, skill.power));
                AddLog(ResolveEnemySkillTargetName(target) + " は " + recovered + " 回復した。");
                break;
            }
            case SkillEffectType.Buff:
            {
                BattleStatusData target = ResolveEnemySkillTarget(skill.target);
                ApplyStatusEffect(
                    target,
                    skill.skillId,
                    skill.GetDisplayName(),
                    skill.affectedStat,
                    Mathf.Max(1, skill.power),
                    skill.statusDurationTurns,
                    target == debugEnemyStatus);
                break;
            }
            case SkillEffectType.Debuff:
            {
                BattleStatusData target = ResolveEnemySkillTarget(skill.target);
                ApplyStatusEffect(
                    target,
                    skill.skillId,
                    skill.GetDisplayName(),
                    skill.affectedStat,
                    -Mathf.Max(1, skill.power),
                    skill.statusDurationTurns,
                    target == debugEnemyStatus);
                break;
            }
            default:
                AddLog(enemyDisplayName + " のスキルは通常攻撃として処理された。");
                ApplyEnemyBasicAttack(playerDefending);
                break;
        }
    }

    private BattleStatusData ResolveEnemySkillTarget(EnemySkillTarget target)
    {
        switch (target)
        {
            case EnemySkillTarget.Self:
                return debugEnemyStatus;
            case EnemySkillTarget.Heroine:
                return debugHeroineStatus != null && !IsDefeated(debugHeroineStatus)
                    ? debugHeroineStatus
                    : debugPlayerStatus;
            case EnemySkillTarget.RandomOpponent:
                if (debugHeroineStatus != null && !IsDefeated(debugHeroineStatus) && Random.Range(0, 2) == 0)
                {
                    return debugHeroineStatus;
                }

                return debugPlayerStatus;
            case EnemySkillTarget.Player:
            default:
                return debugPlayerStatus;
        }
    }

    private string ResolveEnemySkillTargetName(BattleStatusData target)
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

    private void ApplyEnemySkillTargetDamageImage(BattleStatusData target)
    {
        if (target == debugHeroineStatus)
        {
            ApplyHeroineImage(BattleSpriteDamage);
            return;
        }

        if (target == debugPlayerStatus)
        {
            ApplyPlayerImage(BattleSpriteDamage);
        }
    }

    private void ApplyEnemyBasicAttack(bool playerDefending)
    {
        ApplyEnemyImage(currentDebugEnemy, BattleSpriteAttack);
        ApplyPlayerImage(BattleSpriteDamage);
        int playerDamage = Damage(debugEnemyStatus, debugPlayerStatus, playerDefending);
        string defendMessage = playerDefending ? "（防御）" : "";
        AddLog(enemyDisplayName + " の攻撃。 プレイヤーは " + playerDamage + " ダメージ。" + defendMessage);
    }

    private static string GetEnemySkillKey(EnemyBattleSkillData skill)
    {
        if (skill == null)
        {
            return string.Empty;
        }

        return !string.IsNullOrEmpty(skill.skillId) ? skill.skillId : skill.GetDisplayName();
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

        RefreshCombatantNames();
        SetHpText(enemyHpText, "HP", debugEnemyStatus);
        SetHpText(playerHpText, "HP", debugPlayerStatus);
        SetHpText(heroineHpText, "HP", debugHeroineStatus);
        SetMpText(enemyMpText, debugEnemyStatus);
        SetMpText(playerMpText, debugPlayerStatus);
        SetMpText(heroineMpText, debugHeroineStatus);

        if (statusEffectPanel != null && statusEffectPanel.activeSelf)
        {
            RefreshStatusEffectPanel();
        }

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

        if (itemButton != null)
        {
            itemButton.onClick.RemoveListener(OnItemClicked);
            itemButton.onClick.AddListener(OnItemClicked);
        }

        if (statusButton != null)
        {
            statusButton.interactable = true;
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
            " / " + enemyDisplayName + " " + FormatHp(debugEnemyStatus) +
            " MP " + FormatMp(debugEnemyStatus));
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

        if (statusButton != null)
        {
            statusButton.onClick.RemoveListener(OpenStatusEffectPanel);
            statusButton.onClick.AddListener(OpenStatusEffectPanel);
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

        if (closeStatusEffectButton != null)
        {
            closeStatusEffectButton.onClick.RemoveListener(CloseStatusEffectPanel);
            closeStatusEffectButton.onClick.AddListener(CloseStatusEffectPanel);
        }
    }

    private void EnsureReferences()
    {
        if (panelRoot == null)
        {
            panelRoot = gameObject;
        }
    }

    private void EnsureStatusEffectUi()
    {
        if (playerNameText == null)
        {
            playerNameText = FindComponentInChildrenByName<TextMeshProUGUI>(transform, "PlayerNameText");
        }

        if (heroineNameText == null)
        {
            heroineNameText = FindComponentInChildrenByName<TextMeshProUGUI>(transform, "HeroineNameText");
        }

        if (playerMpText == null)
        {
            playerMpText = FindSceneComponentByName<TextMeshProUGUI>("PlayerMpText");
        }

        if (heroineMpText == null)
        {
            heroineMpText = FindSceneComponentByName<TextMeshProUGUI>("HeroineMpText");
        }

        if (enemyMpText == null)
        {
            enemyMpText = FindSceneComponentByName<TextMeshProUGUI>("EnemyMpText");
        }

        if (statusButton == null)
        {
            statusButton = FindComponentInChildrenByName<Button>(transform, "StatusButton");
        }

        if (statusEffectPanel == null)
        {
            statusEffectPanel = FindSceneComponentByName<Transform>("BattleStatusEffectPanel")?.gameObject;
        }

        if (statusEffectPanel == null)
        {
            return;
        }

        Transform panelTransform = statusEffectPanel.transform;
        if (closeStatusEffectButton == null)
        {
            closeStatusEffectButton = FindComponentInChildrenByName<Button>(panelTransform, "CloseButton");
        }

        if (playerStatusEffectList == null)
        {
            playerStatusEffectList = FindComponentInChildrenByName<Transform>(panelTransform, "PlayerStatusEffectList");
        }

        if (heroineStatusEffectList == null)
        {
            heroineStatusEffectList = FindComponentInChildrenByName<Transform>(panelTransform, "HeroineStatusEffectList");
        }

        if (enemyStatusEffectList == null)
        {
            enemyStatusEffectList = FindComponentInChildrenByName<Transform>(panelTransform, "EnemyStatusEffectList");
        }

        if (heroineEffectSection == null)
        {
            Transform section = FindComponentInChildrenByName<Transform>(panelTransform, "HeroineEffectSection");
            heroineEffectSection = section != null ? section.gameObject : null;
        }

        if (heroineEffectSectionNameText == null && heroineEffectSection != null)
        {
            heroineEffectSectionNameText = heroineEffectSection.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (enemyEffectSectionNameText == null && enemyStatusEffectList != null && enemyStatusEffectList.parent != null)
        {
            enemyEffectSectionNameText = enemyStatusEffectList.parent.GetComponentInChildren<TextMeshProUGUI>(true);
        }
    }

    private static T FindSceneComponentByName<T>(string objectName) where T : Component
    {
        T[] components = FindObjectsOfType<T>(true);
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i].gameObject.name == objectName)
            {
                return components[i];
            }
        }

        return null;
    }

    private static T FindComponentInChildrenByName<T>(Transform parent, string objectName) where T : Component
    {
        if (parent == null)
        {
            return null;
        }

        T[] components = parent.GetComponentsInChildren<T>(true);
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i].gameObject.name == objectName)
            {
                return components[i];
            }
        }

        return null;
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

    private static void SetMpText(TextMeshProUGUI text, BattleStatusData status)
    {
        if (text == null)
        {
            return;
        }

        text.text = status == null
            ? "MP -"
            : "MP " + status.currentMp + "/" + status.maxMp;
    }

    private void RefreshCombatantNames()
    {
        if (playerNameText != null)
        {
            playerNameText.text = TruncateCombatantName("主人公");
        }

        if (heroineNameText != null)
        {
            string heroineName = heroineStatus != null && !string.IsNullOrEmpty(heroineStatus.HeroineName)
                ? heroineStatus.HeroineName
                : "ヒロイン";
            heroineNameText.text = TruncateCombatantName(heroineName);
        }
    }

    private static string TruncateCombatantName(string name)
    {
        if (string.IsNullOrEmpty(name) || name.Length <= CombatantNameDisplayMaxLength)
        {
            return name;
        }

        return name.Substring(0, CombatantNameDisplayMaxLength - 1) + "…";
    }

    private void OpenStatusEffectPanel()
    {
        EnsureStatusEffectUi();
        if (statusEffectPanel == null)
        {
            AddLog("状態表示 UI が設定されていません。");
            Refresh();
            return;
        }

        statusEffectPanel.SetActive(true);
        RefreshStatusEffectPanel();
    }

    private void CloseStatusEffectPanel()
    {
        if (statusEffectPanel != null)
        {
            statusEffectPanel.SetActive(false);
        }
    }

    private void RefreshStatusEffectPanel()
    {
        EnsureStatusEffectUi();
        ClearStatusEffectRows();
        RefreshStatusEffectList(playerStatusEffectList, playerStatusEffects);
        RefreshStatusEffectList(heroineStatusEffectList, heroineStatusEffects);
        RefreshStatusEffectList(enemyStatusEffectList, enemyStatusEffects);

        if (heroineEffectSection != null)
        {
            heroineEffectSection.SetActive(debugHeroineStatus != null);
        }

        if (heroineEffectSectionNameText != null)
        {
            heroineEffectSectionNameText.text = heroineStatus != null && !string.IsNullOrEmpty(heroineStatus.HeroineName)
                ? heroineStatus.HeroineName
                : "ヒロイン";
        }

        if (enemyEffectSectionNameText != null)
        {
            enemyEffectSectionNameText.text = enemyDisplayName;
        }
    }

    private void RefreshStatusEffectList(Transform parent, List<BattleStatusEffect> effects)
    {
        if (parent == null)
        {
            return;
        }

        if (effects == null || effects.Count == 0)
        {
            CreateStatusEffectRow(parent, "状態変化なし", noEffectColor);
            return;
        }

        for (int i = 0; i < effects.Count; i++)
        {
            BattleStatusEffect effect = effects[i];
            if (effect == null)
            {
                continue;
            }

            bool isBuff = effect.appliedValue > 0;
            string label = GetBattleStatDisplayName(effect.affectedStat) +
                (isBuff ? " ↑" : " ↓") +
                Mathf.Abs(effect.appliedValue) +
                "　残り " + Mathf.Max(0, effect.remainingTargetTurns) + " ターン";
            CreateStatusEffectRow(parent, label, isBuff ? buffEffectColor : debuffEffectColor);
        }
    }

    private void CreateStatusEffectRow(Transform parent, string label, Color backgroundColor)
    {
        if (statusEffectRowPrefab == null || parent == null)
        {
            return;
        }

        GameObject row = Instantiate(statusEffectRowPrefab, parent);
        row.SetActive(true);
        spawnedStatusEffectRows.Add(row);

        Image background = row.GetComponent<Image>();
        if (background != null)
        {
            background.color = backgroundColor;
        }

        TextMeshProUGUI text = row.GetComponentInChildren<TextMeshProUGUI>(true);
        if (text != null)
        {
            text.text = label;
        }
    }

    private void ClearStatusEffectRows()
    {
        for (int i = 0; i < spawnedStatusEffectRows.Count; i++)
        {
            if (spawnedStatusEffectRows[i] != null)
            {
                Destroy(spawnedStatusEffectRows[i]);
            }
        }

        spawnedStatusEffectRows.Clear();
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

    private static int DamageWithSkill(
        BattleStatusData attacker,
        BattleStatusData defender,
        int power,
        bool defenderGuarding = false)
    {
        if (attacker == null || defender == null)
        {
            return 0;
        }

        int attack = Mathf.Max(1, attacker.attack + Mathf.Max(0, power));
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

    private void ApplyStatusEffect(
        BattleStatusData target,
        string effectId,
        string displayName,
        SkillBattleStat stat,
        int value,
        int durationTurns,
        bool skipNextTargetTick)
    {
        if (target == null || value == 0)
        {
            return;
        }

        int appliedValue = ApplyBattleStatModifier(target, stat, value);
        if (appliedValue == 0)
        {
            AddLog(ResolveBattleStatusTargetName(target) + " の " + GetBattleStatDisplayName(stat) + " は変化しなかった。");
            return;
        }

        List<BattleStatusEffect> effects = GetStatusEffects(target);
        effects.Add(new BattleStatusEffect
        {
            effectId = effectId,
            displayName = displayName,
            affectedStat = stat,
            appliedValue = appliedValue,
            remainingTargetTurns = Mathf.Max(1, durationTurns),
            skipNextTargetTick = skipNextTargetTick
        });

        AddLog(
            ResolveBattleStatusTargetName(target) +
            " の " +
            GetBattleStatDisplayName(stat) +
            " が " +
            Mathf.Abs(appliedValue) +
            (appliedValue > 0 ? " 上がった。" : " 下がった。") +
            "（" +
            Mathf.Max(1, durationTurns) +
            "ターン）");
    }

    private void TickStatusEffects(BattleStatusData target)
    {
        List<BattleStatusEffect> effects = GetStatusEffects(target);
        for (int i = effects.Count - 1; i >= 0; i--)
        {
            BattleStatusEffect effect = effects[i];
            if (effect.skipNextTargetTick)
            {
                effect.skipNextTargetTick = false;
                continue;
            }

            effect.remainingTargetTurns--;
            if (effect.remainingTargetTurns > 0)
            {
                continue;
            }

            ApplyBattleStatModifier(target, effect.affectedStat, -effect.appliedValue);
            AddLog(
                ResolveBattleStatusTargetName(target) +
                " の " +
                GetBattleStatDisplayName(effect.affectedStat) +
                " は元に戻った。");
            effects.RemoveAt(i);
        }
    }

    private List<BattleStatusEffect> GetStatusEffects(BattleStatusData target)
    {
        if (target == debugEnemyStatus)
        {
            return enemyStatusEffects;
        }

        if (target == debugHeroineStatus)
        {
            return heroineStatusEffects;
        }

        return playerStatusEffects;
    }

    private string ResolveBattleStatusTargetName(BattleStatusData target)
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

    private static int GetMissingMp(BattleStatusData status)
    {
        return status == null ? 0 : Mathf.Max(0, status.maxMp - status.currentMp);
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
