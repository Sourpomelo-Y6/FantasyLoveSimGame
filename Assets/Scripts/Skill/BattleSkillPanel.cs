using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleSkillPanel : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button closeButton;

    [Header("Status")]
    [SerializeField] private TextMeshProUGUI mpText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Button useButton;

    [Header("Skill List")]
    [SerializeField] private Transform skillListParent;
    [SerializeField] private Button skillButtonPrefab;
    [SerializeField] private string noSelectionMessage = "使用するスキルを選択してください。";
    [SerializeField] private string insufficientMpLabel = "MP不足";

    private readonly List<GameObject> spawnedSkillButtons = new List<GameObject>();
    private readonly List<SkillData> availableSkills = new List<SkillData>();
    private Action<SkillData> onSkillConfirmed;
    private SkillData selectedSkill;
    private int currentMp;
    private int maxMp;
    private bool hasWarnedMissingReferences;

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

        if (useButton != null)
        {
            useButton.onClick.AddListener(ConfirmSelectedSkill);
        }
    }

    public void Initialize()
    {
        EnsureUiReferences();
    }

    public void Open(IReadOnlyList<SkillData> skills, int playerCurrentMp, int playerMaxMp, Action<SkillData> onConfirmed)
    {
        Initialize();
        availableSkills.Clear();
        if (skills != null)
        {
            for (int i = 0; i < skills.Count; i++)
            {
                SkillData skill = skills[i];
                if (skill != null && skill.isEnabled && skill.canUseInBattle)
                {
                    availableSkills.Add(skill);
                }
            }
        }

        currentMp = Mathf.Max(0, playerCurrentMp);
        maxMp = Mathf.Max(0, playerMaxMp);
        onSkillConfirmed = onConfirmed;
        selectedSkill = null;
        PanelRoot.SetActive(true);
        Refresh();
    }

    public void Close()
    {
        selectedSkill = null;
        onSkillConfirmed = null;
        PanelRoot.SetActive(false);
    }

    private void Refresh()
    {
        if (!PanelRoot.activeSelf)
        {
            return;
        }

        if (mpText != null)
        {
            mpText.text = "MP " + currentMp + "/" + maxMp;
        }

        RefreshSkillList();
        RefreshSelection();
    }

    private void RefreshSkillList()
    {
        if (skillListParent == null || skillButtonPrefab == null)
        {
            return;
        }

        ClearSkillList();
        HideSkillButtonTemplate();

        foreach (SkillData skill in availableSkills)
        {
            Button button = Instantiate(skillButtonPrefab, skillListParent);
            button.gameObject.SetActive(true);
            spawnedSkillButtons.Add(button.gameObject);

            bool canUse = CanUseSkill(skill);
            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.text = BuildSkillButtonLabel(skill, canUse);
            }

            button.interactable = canUse;
            button.onClick.AddListener(() => SelectSkill(skill));
        }
    }

    private void SelectSkill(SkillData skill)
    {
        if (!CanUseSkill(skill))
        {
            return;
        }

        selectedSkill = skill;
        RefreshSelection();
    }

    private void RefreshSelection()
    {
        if (descriptionText != null)
        {
            descriptionText.text = selectedSkill == null
                ? noSelectionMessage
                : BuildSkillDescription(selectedSkill);
        }

        if (useButton != null)
        {
            useButton.interactable = CanUseSkill(selectedSkill);
        }
    }

    private void ConfirmSelectedSkill()
    {
        if (!CanUseSkill(selectedSkill))
        {
            return;
        }

        SkillData skill = selectedSkill;
        Action<SkillData> callback = onSkillConfirmed;
        Close();
        callback?.Invoke(skill);
    }

    private bool CanUseSkill(SkillData skill)
    {
        return skill != null && Mathf.Max(0, skill.cost) <= currentMp;
    }

    private string BuildSkillButtonLabel(SkillData skill, bool canUse)
    {
        string label = skill.GetDisplayName() + " / MP " + Mathf.Max(0, skill.cost);
        return canUse ? label : label + " / " + insufficientMpLabel;
    }

    private static string BuildSkillDescription(SkillData skill)
    {
        return skill.GetDisplayName() +
            "\n消費 MP: " + Mathf.Max(0, skill.cost) +
            "\n" + skill.description;
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

        Debug.LogWarning("BattleSkillPanel の UI 参照が不足しています。Canvas 配下に UI を配置し、Inspector で参照を割り当ててください。");
        hasWarnedMissingReferences = true;
    }

    private bool HasRequiredReferences()
    {
        return skillListParent != null && skillButtonPrefab != null && useButton != null;
    }
}
