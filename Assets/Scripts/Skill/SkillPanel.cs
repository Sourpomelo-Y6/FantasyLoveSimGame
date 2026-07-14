using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillPanel : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button closeButton;

    [Header("List")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Transform skillListParent;
    [SerializeField] private Button skillButtonPrefab;
    [SerializeField] private string skillListTitle = "スキル一覧";
    [SerializeField] private string battleSelectionTitle = "使用するスキルを選択";
    [SerializeField] private string unlockedLabel = "解放済み";
    [SerializeField] private string lockedLabel = "未解放";

    private GameManager gameManager;
    private Action<SkillData> onBattleSkillSelected;
    private bool battleSelectionMode;
    private bool hasWarnedMissingReferences;
    private readonly List<GameObject> spawnedSkillButtons = new List<GameObject>();

    private GameObject PanelRoot
    {
        get { return panelRoot != null ? panelRoot : gameObject; }
    }

    private void Awake()
    {
        EnsureUiReferences();
        HideSkillButtonTemplate();

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Close);
        }
    }

    public void Initialize(GameManager manager)
    {
        gameManager = manager;
        EnsureUiReferences();
    }

    public void OpenSkillList(GameManager manager)
    {
        Initialize(manager);
        battleSelectionMode = false;
        onBattleSkillSelected = null;
        PanelRoot.SetActive(true);
        Refresh();
    }

    public void OpenBattleSkillSelection(GameManager manager, Action<SkillData> onSelected)
    {
        Initialize(manager);
        battleSelectionMode = true;
        onBattleSkillSelected = onSelected;
        PanelRoot.SetActive(true);
        Refresh();
    }

    public void Close()
    {
        onBattleSkillSelected = null;
        battleSelectionMode = false;
        PanelRoot.SetActive(false);
    }

    private void Refresh()
    {
        if (gameManager == null || !PanelRoot.activeSelf)
        {
            return;
        }

        if (titleText != null)
        {
            titleText.text = battleSelectionMode ? battleSelectionTitle : skillListTitle;
        }

        if (descriptionText != null)
        {
            descriptionText.text = battleSelectionMode
                ? "解放済みの戦闘用スキルを選択してください。"
                : "スキルポイントを使い、スキルツリーで習得したスキルを確認できます。";
        }

        RefreshSkillList();
    }

    private void RefreshSkillList()
    {
        if (skillListParent == null || skillButtonPrefab == null)
        {
            return;
        }

        ClearSkillList();
        HideSkillButtonTemplate();
        List<SkillData> skills = battleSelectionMode
            ? gameManager.GetUnlockedBattleSkills()
            : gameManager.GetSkills();

        foreach (SkillData skill in skills)
        {
            if (skill == null || !skill.isEnabled)
            {
                continue;
            }

            CreateSkillButton(skill);
        }
    }

    private void CreateSkillButton(SkillData skill)
    {
        Button button = Instantiate(skillButtonPrefab, skillListParent);
        button.gameObject.SetActive(true);
        spawnedSkillButtons.Add(button.gameObject);

        bool unlocked = gameManager.IsSkillUnlocked(skill.skillId);
        TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            buttonText.text = BuildSkillButtonLabel(skill, unlocked);
        }

        if (battleSelectionMode)
        {
            button.interactable = unlocked && skill.category == SkillCategory.Battle && skill.canUseInBattle;
            button.onClick.AddListener(() => SelectBattleSkill(skill));
            return;
        }

        button.interactable = true;
        button.onClick.AddListener(() => ShowSkillDescription(skill));
    }

    private string BuildSkillButtonLabel(SkillData skill, bool unlocked)
    {
        return skill.GetDisplayName() +
            " / MP " +
            Mathf.Max(0, skill.cost) +
            " / " +
            (unlocked ? unlockedLabel : lockedLabel);
    }

    private void ShowSkillDescription(SkillData skill)
    {
        if (descriptionText == null || skill == null)
        {
            return;
        }

        bool unlocked = gameManager.IsSkillUnlocked(skill.skillId);
        string state = unlocked ? unlockedLabel : lockedLabel;
        descriptionText.text =
            skill.GetDisplayName() + " / " + state + "\n" +
            skill.description + "\n\n" +
            "消費 MP: " + Mathf.Max(0, skill.cost) + "\n" +
            gameManager.GetSkillUnlockConditionText(skill);
    }

    private void SelectBattleSkill(SkillData skill)
    {
        if (skill == null || !gameManager.IsSkillUnlocked(skill.skillId))
        {
            return;
        }

        Action<SkillData> callback = onBattleSkillSelected;
        Close();
        callback?.Invoke(skill);
    }

    private void ClearSkillList()
    {
        for (int i = 0; i < spawnedSkillButtons.Count; i++)
        {
            if (spawnedSkillButtons[i] != null)
            {
                Destroy(spawnedSkillButtons[i]);
            }
        }

        spawnedSkillButtons.Clear();
    }

    private void HideSkillButtonTemplate()
    {
        if (skillButtonPrefab != null &&
            skillListParent != null &&
            skillButtonPrefab.transform.IsChildOf(skillListParent))
        {
            skillButtonPrefab.gameObject.SetActive(false);
        }
    }

    private void EnsureUiReferences()
    {
        if (HasRequiredReferences() || hasWarnedMissingReferences)
        {
            return;
        }

        Debug.LogWarning("SkillPanel の UI 参照が不足しています。Hierarchy 上に UI を配置し、Inspector で参照を割り当ててください。");
        hasWarnedMissingReferences = true;
    }

    private bool HasRequiredReferences()
    {
        return skillListParent != null && skillButtonPrefab != null;
    }
}
