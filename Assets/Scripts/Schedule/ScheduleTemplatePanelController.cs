using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScheduleTemplatePanelController : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private ScheduleTemplateManager templateManager;
    [SerializeField] private ScheduleManager scheduleManager;
    [SerializeField] private SchedulePanel schedulePanel;

    [Header("Edit UI")]
    [SerializeField] private TMP_InputField templateNameInput;
    [SerializeField] private TMP_Dropdown periodDropdown;
    [SerializeField] private Button saveNewButton;
    [SerializeField] private Button overwriteButton;

    [Header("List UI")]
    [SerializeField] private Button templateRowTemplate;
    [SerializeField] private TMP_Text selectedTemplateText;

    [Header("Apply UI")]
    [SerializeField] private TMP_Text applyStartDayText;
    [SerializeField] private Toggle overwriteExistingToggle;
    [SerializeField] private Button applyButton;
    [SerializeField] private Button deleteButton;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private Button closeButton;

    [Header("Confirmation UI")]
    [SerializeField] private GameObject confirmationPanel;
    [SerializeField] private TMP_Text confirmationMessageText;
    [SerializeField] private Button confirmationConfirmButton;
    [SerializeField] private Button confirmationCancelButton;

    private readonly List<Button> generatedRows = new List<Button>();
    private string selectedTemplateId;
    private int selectedStartDay = 1;
    private bool buttonsHooked;
    private Action pendingConfirmation;

    private void Awake()
    {
        ResolveReferences();
        HookButtons();
        HideConfirmation();
    }

    public void Initialize(SchedulePanel ownerPanel, ScheduleManager ownerScheduleManager)
    {
        schedulePanel = ownerPanel;
        scheduleManager = ownerScheduleManager;
        ResolveReferences();
        HookButtons();
    }

    public void Open(int startDay)
    {
        selectedStartDay = Mathf.Max(1, startDay);
        ResolveReferences();
        HookButtons();
        if (templateManager != null) templateManager.Reload();
        selectedTemplateId = string.Empty;
        gameObject.SetActive(true);
        HideConfirmation();
        RefreshTemplateList();
        RefreshSelection();
        SetResult(string.Empty);
    }

    public void Close()
    {
        pendingConfirmation = null;
        HideConfirmation();
        gameObject.SetActive(false);
    }

    private void SaveNewTemplate()
    {
        SaveTemplate(string.Empty);
    }

    private void RequestOverwriteTemplate()
    {
        ScheduleTemplateData selected = GetSelectedTemplate();
        if (selected == null) return;

        ShowConfirmation(
            "テンプレート「" + selected.displayName + "」を、現在の入力内容で上書きします。",
            () => SaveTemplate(selected.templateId));
    }

    private void SaveTemplate(string overwriteTemplateId)
    {
        if (!HasRequiredDataReferences()) return;

        ScheduleTemplateData savedTemplate;
        string message;
        bool succeeded = templateManager.TrySaveTemplate(
            templateNameInput != null ? templateNameInput.text : string.Empty,
            selectedStartDay,
            GetSelectedDayCount(),
            scheduleManager,
            overwriteTemplateId,
            out savedTemplate,
            out message);
        SetResult(message);
        if (!succeeded) return;

        selectedTemplateId = savedTemplate.templateId;
        RefreshTemplateList();
        RefreshSelection();
    }

    private void RequestApplyTemplate()
    {
        ScheduleTemplateData selected = GetSelectedTemplate();
        if (selected == null || !HasRequiredDataReferences()) return;

        bool overwriteExisting = overwriteExistingToggle != null && overwriteExistingToggle.isOn;
        ScheduleTemplateApplyResult preview;
        string previewMessage;
        if (!templateManager.TryPreviewTemplateApplication(
            selected.templateId,
            selectedStartDay,
            overwriteExisting,
            scheduleManager,
            out preview,
            out previewMessage))
        {
            SetResult(previewMessage);
            return;
        }

        string confirmation = "テンプレート「" + selected.displayName + "」をDay " +
            selectedStartDay + "から適用します。\n" + preview.CreatePreviewSummary() +
            "\n既存予定を上書き：" + (overwriteExisting ? "する" : "しない");
        if (overwriteExisting)
        {
            confirmation += "\nテンプレートの空欄により既存予定が削除される場合があります。";
        }

        ShowConfirmation(confirmation, () => ApplyTemplate(selected.templateId, overwriteExisting));
    }

    private void ApplyTemplate(string templateId, bool overwriteExisting)
    {
        ScheduleTemplateApplyResult result;
        string message;
        templateManager.TryApplyTemplate(
            templateId,
            selectedStartDay,
            overwriteExisting,
            scheduleManager,
            out result,
            out message);
        SetResult(message);
        if (schedulePanel != null) schedulePanel.RefreshAfterTemplateChange();
    }

    private void RequestDeleteTemplate()
    {
        ScheduleTemplateData selected = GetSelectedTemplate();
        if (selected == null) return;

        ShowConfirmation(
            "テンプレート「" + selected.displayName + "」を削除します。",
            () => DeleteTemplate(selected.templateId));
    }

    private void DeleteTemplate(string templateId)
    {
        string message = "ScheduleTemplateManager が設定されていません。";
        bool succeeded = templateManager != null && templateManager.TryDeleteTemplate(templateId, out message);
        SetResult(message);
        if (!succeeded) return;

        selectedTemplateId = string.Empty;
        RefreshTemplateList();
        RefreshSelection();
    }

    private void RefreshTemplateList()
    {
        ClearGeneratedRows();
        if (templateManager == null || templateRowTemplate == null) return;

        templateRowTemplate.gameObject.SetActive(false);
        List<ScheduleTemplateData> templates = templateManager.GetTemplates();
        for (int i = 0; i < templates.Count; i++)
        {
            ScheduleTemplateData template = templates[i];
            Button row = Instantiate(templateRowTemplate, templateRowTemplate.transform.parent);
            row.name = "TemplateRow_" + template.templateId;
            TMP_Text label = row.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.text = template.displayName + "（" + template.dayCount + "日）";
            }

            string templateId = template.templateId;
            row.onClick.RemoveAllListeners();
            row.onClick.AddListener(() => SelectTemplate(templateId));
            row.gameObject.SetActive(true);
            generatedRows.Add(row);
        }
    }

    private void SelectTemplate(string templateId)
    {
        selectedTemplateId = templateId;
        ScheduleTemplateData selected = GetSelectedTemplate();
        if (selected != null)
        {
            if (templateNameInput != null) templateNameInput.text = selected.displayName;
            if (periodDropdown != null)
            {
                periodDropdown.value = selected.dayCount == ScheduleTemplateManager.MonthlyTemplateDayCount ? 1 : 0;
            }
        }
        RefreshSelection();
    }

    private void RefreshSelection()
    {
        ScheduleTemplateData selected = GetSelectedTemplate();
        bool hasSelection = selected != null;
        if (selectedTemplateText != null)
        {
            selectedTemplateText.text = hasSelection
                ? "選択中：" + selected.displayName + "（" + selected.dayCount + "日）"
                : "テンプレートが選択されていません";
        }
        if (applyStartDayText != null)
        {
            applyStartDayText.text = "適用開始日：Day " + selectedStartDay;
        }
        if (overwriteButton != null) overwriteButton.interactable = hasSelection;
        if (applyButton != null) applyButton.interactable = hasSelection;
        if (deleteButton != null) deleteButton.interactable = hasSelection;
    }

    private ScheduleTemplateData GetSelectedTemplate()
    {
        if (templateManager == null || string.IsNullOrEmpty(selectedTemplateId)) return null;
        List<ScheduleTemplateData> templates = templateManager.GetTemplates();
        for (int i = 0; i < templates.Count; i++)
        {
            if (string.Equals(templates[i].templateId, selectedTemplateId, StringComparison.Ordinal))
            {
                return templates[i];
            }
        }
        return null;
    }

    private int GetSelectedDayCount()
    {
        return periodDropdown != null && periodDropdown.value == 1
            ? ScheduleTemplateManager.MonthlyTemplateDayCount
            : ScheduleTemplateManager.WeeklyTemplateDayCount;
    }

    private bool HasRequiredDataReferences()
    {
        if (templateManager != null && scheduleManager != null) return true;
        SetResult("ScheduleTemplateManager または ScheduleManager が設定されていません。");
        return false;
    }

    private void ShowConfirmation(string message, Action confirmedAction)
    {
        if (confirmationPanel == null || confirmationConfirmButton == null)
        {
            confirmedAction();
            return;
        }

        pendingConfirmation = confirmedAction;
        if (confirmationMessageText != null) confirmationMessageText.text = message;
        confirmationPanel.SetActive(true);
    }

    private void ConfirmPendingAction()
    {
        Action action = pendingConfirmation;
        pendingConfirmation = null;
        HideConfirmation();
        if (action != null) action();
    }

    private void HideConfirmation()
    {
        pendingConfirmation = null;
        if (confirmationPanel != null) confirmationPanel.SetActive(false);
    }

    private void SetResult(string message)
    {
        if (resultText != null) resultText.text = message ?? string.Empty;
    }

    private void ClearGeneratedRows()
    {
        for (int i = 0; i < generatedRows.Count; i++)
        {
            if (generatedRows[i] != null) Destroy(generatedRows[i].gameObject);
        }
        generatedRows.Clear();
    }

    private void HookButtons()
    {
        if (buttonsHooked) return;
        if (saveNewButton != null) saveNewButton.onClick.AddListener(SaveNewTemplate);
        if (overwriteButton != null) overwriteButton.onClick.AddListener(RequestOverwriteTemplate);
        if (applyButton != null) applyButton.onClick.AddListener(RequestApplyTemplate);
        if (deleteButton != null) deleteButton.onClick.AddListener(RequestDeleteTemplate);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (confirmationConfirmButton != null) confirmationConfirmButton.onClick.AddListener(ConfirmPendingAction);
        if (confirmationCancelButton != null) confirmationCancelButton.onClick.AddListener(HideConfirmation);
        buttonsHooked = true;
    }

    private void ResolveReferences()
    {
        if (templateManager == null) templateManager = GetComponent<ScheduleTemplateManager>();
        if (templateNameInput == null) templateNameInput = FindComponent<TMP_InputField>("TemplateNameInput");
        if (periodDropdown == null) periodDropdown = FindComponent<TMP_Dropdown>("PeriodDropdown");
        if (saveNewButton == null) saveNewButton = FindComponent<Button>("SaveNewButton");
        if (overwriteButton == null) overwriteButton = FindComponent<Button>("OverwriteButton");
        if (templateRowTemplate == null) templateRowTemplate = FindComponent<Button>("TemplateRowTemplate");
        if (selectedTemplateText == null) selectedTemplateText = FindComponent<TMP_Text>("SelectedTemplateText");
        if (applyStartDayText == null) applyStartDayText = FindComponent<TMP_Text>("ApplyStartDayText");
        if (overwriteExistingToggle == null) overwriteExistingToggle = FindComponent<Toggle>("OverwriteExistingToggle");
        if (applyButton == null) applyButton = FindComponent<Button>("ApplyButton");
        if (deleteButton == null) deleteButton = FindComponent<Button>("DeleteButton");
        if (resultText == null) resultText = FindComponent<TMP_Text>("ResultText");
        if (closeButton == null) closeButton = FindComponent<Button>("CloseButton");

        if (confirmationPanel == null)
        {
            Transform confirmation = FindTransform("ConfirmationPanel");
            if (confirmation != null) confirmationPanel = confirmation.gameObject;
        }
        if (confirmationPanel != null)
        {
            if (confirmationMessageText == null) confirmationMessageText = FindComponentInRoot<TMP_Text>(confirmationPanel, "MessageText");
            if (confirmationConfirmButton == null) confirmationConfirmButton = FindComponentInRoot<Button>(confirmationPanel, "ConfirmButton");
            if (confirmationCancelButton == null) confirmationCancelButton = FindComponentInRoot<Button>(confirmationPanel, "CancelButton");
        }
    }

    private T FindComponent<T>(string objectName) where T : Component
    {
        return FindComponentInRoot<T>(gameObject, objectName);
    }

    private static T FindComponentInRoot<T>(GameObject root, string objectName) where T : Component
    {
        if (root == null) return null;
        T[] components = root.GetComponentsInChildren<T>(true);
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] != null && components[i].gameObject.name == objectName) return components[i];
        }
        return null;
    }

    private Transform FindTransform(string objectName)
    {
        Transform[] transforms = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i] != null && transforms[i].name == objectName) return transforms[i];
        }
        return null;
    }
}
