using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SchedulePanel : MonoBehaviour
{
    private const int DaysPerWeek = 7;
    private const int DaysPerMonthView = 30;

    private enum CalendarView
    {
        Week,
        Month
    }

    [Header("References")]
    [SerializeField] private ScheduleManager scheduleManager;
    [SerializeField] private GameManager gameManager;

    [Header("Weekly Calendar")]
    [Tooltip("Day 0 から Day 6 の順で7個を設定します。")]
    [SerializeField] private Button[] dayButtons = new Button[DaysPerWeek];
    [Tooltip("dayButtons と同じ順序で7個を設定します。")]
    [SerializeField] private TextMeshProUGUI[] dayButtonTexts = new TextMeshProUGUI[DaysPerWeek];
    [SerializeField] private TextMeshProUGUI weekRangeText;
    [SerializeField] private Button previousWeekButton;
    [SerializeField] private Button nextWeekButton;
    [SerializeField] private Button currentWeekButton;

    [Header("View Mode")]
    [SerializeField] private Button weeklyViewButton;
    [SerializeField] private Button monthlyViewButton;
    [SerializeField] private GameObject weeklyGridRoot;
    [SerializeField] private GameObject monthlyGridRoot;
    [Tooltip("MonthlyGridRoot の子に置いた非アクティブな ScheduleDayCell を設定します。")]
    [SerializeField] private ScheduleDayCell monthlyDayCellTemplate;

    [Header("Selected Day")]
    [SerializeField] private RectTransform selectedDayArea;
    [SerializeField] private RectTransform weeklySelectedDayAnchor;
    [SerializeField] private RectTransform monthlySelectedDayAnchor;
    [SerializeField] private TextMeshProUGUI selectedDayText;
    [SerializeField] private Button cancelButton;
    [Tooltip("既存の予定選択ボタンをすべて設定すると、過去日や実行済みの日に無効化します。")]
    [SerializeField] private Button[] scheduleChoiceButtons;

    [Header("Message UI")]
    [SerializeField] private TextMeshProUGUI messageText;

    [Header("State Colors")]
    [SerializeField] private Color emptyColor = new Color(0.85f, 0.85f, 0.85f, 1f);
    [SerializeField] private Color plannedColor = new Color(0.65f, 0.85f, 1f, 1f);
    [SerializeField] private Color executedColor = new Color(0.65f, 0.9f, 0.65f, 1f);
    [SerializeField] private Color cancelledColor = new Color(0.7f, 0.7f, 0.7f, 1f);
    [SerializeField] private Color selectedColor = new Color(1f, 0.85f, 0.35f, 1f);

    private int weekStartDay;
    private int selectedDay;
    private bool buttonsHooked;
    private CalendarView currentView = CalendarView.Week;
    private readonly List<ScheduleDayCell> monthlyDayCells = new List<ScheduleDayCell>();

    private bool HasWeeklyUi
    {
        get
        {
            if (dayButtons == null || dayButtons.Length < DaysPerWeek) return false;
            for (int i = 0; i < DaysPerWeek; i++)
            {
                if (dayButtons[i] == null) return false;
            }
            return true;
        }
    }

    private bool HasMonthlyUi
    {
        get { return monthlyGridRoot != null && monthlyDayCellTemplate != null; }
    }

    private bool HasActiveCalendarUi
    {
        get { return currentView == CalendarView.Month ? HasMonthlyUi : HasWeeklyUi; }
    }

    private void Awake()
    {
        ResolveWeeklyUiReferences();
        HookButtons();
    }

    private void OnEnable()
    {
        ResolveWeeklyUiReferences();
        HookButtons();
        ResetToCurrentWeek();
    }

    [ContextMenu("Auto Assign Calendar UI References")]
    public void ResolveWeeklyUiReferences()
    {
        EnsureWeeklyArrays();
        for (int i = 0; i < DaysPerWeek; i++)
        {
            if (dayButtons[i] == null)
            {
                dayButtons[i] = FindDescendantComponent<Button>("DayButton" + i);
            }

            if (dayButtonTexts[i] == null && dayButtons[i] != null)
            {
                dayButtonTexts[i] = dayButtons[i].GetComponentInChildren<TextMeshProUGUI>(true);
            }
        }

        if (weekRangeText == null)
        {
            weekRangeText = FindDescendantComponent<TextMeshProUGUI>("WeekRangeText");
        }
        if (previousWeekButton == null)
        {
            previousWeekButton = FindDescendantComponent<Button>("PreviousWeekButton");
        }
        if (nextWeekButton == null)
        {
            nextWeekButton = FindDescendantComponent<Button>("NextWeekButton");
        }
        if (currentWeekButton == null)
        {
            currentWeekButton = FindDescendantComponent<Button>("CurrentWeekButton");
        }
        if (selectedDayText == null)
        {
            selectedDayText = FindDescendantComponent<TextMeshProUGUI>("SelectedDayText");
        }
        if (selectedDayArea == null)
        {
            Transform area = FindDescendantTransform("SelectedDayArea");
            if (area != null) selectedDayArea = area as RectTransform;
        }
        if (weeklySelectedDayAnchor == null)
        {
            Transform anchor = FindDescendantTransform("WeeklySelectedDayAnchor");
            if (anchor != null) weeklySelectedDayAnchor = anchor as RectTransform;
        }
        if (monthlySelectedDayAnchor == null)
        {
            Transform anchor = FindDescendantTransform("MonthlySelectedDayAnchor");
            if (anchor != null) monthlySelectedDayAnchor = anchor as RectTransform;
        }
        if (cancelButton == null)
        {
            cancelButton = FindDescendantComponent<Button>("CancelButton");
        }
        if (weeklyViewButton == null)
        {
            weeklyViewButton = FindDescendantComponent<Button>("WeeklyViewButton");
        }
        if (monthlyViewButton == null)
        {
            monthlyViewButton = FindDescendantComponent<Button>("MonthlyViewButton");
        }
        if (weeklyGridRoot == null)
        {
            Transform weeklyGrid = FindDescendantTransform("WeekGrid");
            if (weeklyGrid != null) weeklyGridRoot = weeklyGrid.gameObject;
        }
        if (monthlyGridRoot == null)
        {
            Transform monthlyGrid = FindDescendantTransform("MonthGrid");
            if (monthlyGrid != null) monthlyGridRoot = monthlyGrid.gameObject;
        }
        if (monthlyDayCellTemplate == null && monthlyGridRoot != null)
        {
            ScheduleDayCell[] cells = monthlyGridRoot.GetComponentsInChildren<ScheduleDayCell>(true);
            if (cells.Length > 0) monthlyDayCellTemplate = cells[0];
        }
    }

    public void SelectNone() { SelectSchedule(ScheduleType.None); }
    public void SelectSoloForest() { SelectSchedule(ScheduleType.SoloForest); }
    public void SelectSoloCave() { SelectSchedule(ScheduleType.SoloCave); }
    public void SelectSoloLake() { SelectSchedule(ScheduleType.SoloLake); }
    public void SelectSoloShopping() { SelectSchedule(ScheduleType.SoloShopping); }
    public void SelectDuoForest() { SelectSchedule(ScheduleType.DuoForest); }
    public void SelectDuoCave() { SelectSchedule(ScheduleType.DuoCave); }
    public void SelectDuoLake() { SelectSchedule(ScheduleType.DuoLake); }
    public void SelectDuoShopping() { SelectSchedule(ScheduleType.DuoShopping); }
    public void SelectStayHome() { SelectSchedule(ScheduleType.StayHome); }

    public void ShowPreviousWeek()
    {
        if (scheduleManager == null) return;
        weekStartDay = Mathf.Max(1, weekStartDay - GetVisibleDayCount());
        selectedDay = weekStartDay;
        RefreshDisplay();
    }

    public void ShowNextWeek()
    {
        if (scheduleManager == null) return;
        int maximumStartDay = GetMaximumPeriodStartDay();
        weekStartDay = Mathf.Min(maximumStartDay, weekStartDay + GetVisibleDayCount());
        selectedDay = weekStartDay;
        RefreshDisplay();
    }

    public void ResetToCurrentWeek()
    {
        if (scheduleManager == null)
        {
            RefreshMessage("ScheduleManager が設定されていません。");
            return;
        }

        weekStartDay = scheduleManager.CurrentDayNumber;
        selectedDay = HasActiveCalendarUi
            ? scheduleManager.CurrentDayNumber
            : scheduleManager.CurrentDayNumber + 1;
        RefreshDisplay();
    }

    public void ShowWeeklyView()
    {
        SwitchView(CalendarView.Week);
    }

    public void ShowMonthlyView()
    {
        if (!HasMonthlyUi)
        {
            RefreshMessage("月間表示用のMonthGridとDayCellTemplateが設定されていません。");
            return;
        }
        SwitchView(CalendarView.Month);
    }

    public void CancelSelectedSchedule()
    {
        if (scheduleManager == null) return;
        string message;
        scheduleManager.TryCancelSchedule(selectedDay, "Player", out message);
        RefreshDisplay();
        RefreshMessage(message);
        RefreshGameUi();
    }

    private void SelectSchedule(ScheduleType scheduleType)
    {
        if (scheduleManager == null)
        {
            Debug.LogWarning("ScheduleManager が設定されていません。");
            return;
        }

        string message;
        bool succeeded = scheduleManager.TrySetScheduleForDay(
            HasActiveCalendarUi ? selectedDay : scheduleManager.CurrentDayNumber + 1,
            scheduleType,
            out message);
        if (!succeeded)
        {
            Debug.LogWarning(message);
        }

        RefreshDisplay();
        RefreshMessage(message);
        RefreshGameUi();
    }

    private void HookButtons()
    {
        if (buttonsHooked) return;

        if (dayButtons != null)
        {
            int count = Mathf.Min(DaysPerWeek, dayButtons.Length);
            for (int i = 0; i < count; i++)
            {
                int index = i;
                if (dayButtons[i] != null)
                {
                    dayButtons[i].onClick.AddListener(() => SelectDay(index));
                }
            }
        }

        if (previousWeekButton != null) previousWeekButton.onClick.AddListener(ShowPreviousWeek);
        if (nextWeekButton != null) nextWeekButton.onClick.AddListener(ShowNextWeek);
        if (currentWeekButton != null) currentWeekButton.onClick.AddListener(ResetToCurrentWeek);
        if (cancelButton != null) cancelButton.onClick.AddListener(CancelSelectedSchedule);
        if (weeklyViewButton != null) weeklyViewButton.onClick.AddListener(ShowWeeklyView);
        if (monthlyViewButton != null) monthlyViewButton.onClick.AddListener(ShowMonthlyView);
        buttonsHooked = true;
    }

    private void SelectDay(int index)
    {
        if (index < 0 || index >= DaysPerWeek) return;
        selectedDay = weekStartDay + index;
        RefreshDisplay();
    }

    private void RefreshDisplay()
    {
        if (scheduleManager == null) return;

        RefreshCalendarVisibility();
        if (currentView == CalendarView.Month)
        {
            RefreshMonthCells();
        }
        else
        {
            RefreshWeekButtons();
        }
        RefreshSelectedDayLayout();
        RefreshSelectedDay();
        RefreshNavigation();
        RefreshViewButtons();
    }

    private void RefreshCalendarVisibility()
    {
        if (weeklyGridRoot != null) weeklyGridRoot.SetActive(currentView == CalendarView.Week);
        if (monthlyGridRoot != null) monthlyGridRoot.SetActive(currentView == CalendarView.Month);
    }

    private void RefreshWeekButtons()
    {
        if (!HasWeeklyUi) return;

        for (int i = 0; i < DaysPerWeek; i++)
        {
            int day = weekStartDay + i;
            Button button = dayButtons[i];
            TextMeshProUGUI label = dayButtonTexts != null && i < dayButtonTexts.Length
                ? dayButtonTexts[i]
                : null;
            ScheduleEntry entry;
            bool hasEntry = scheduleManager.TryGetScheduleEntry(day, out entry);

            if (label != null)
            {
                string weekday = ScheduleManager.GetWeekdayDisplayName(
                    scheduleManager.GetWeekdayForDay(day));
                string scheduleName = hasEntry
                    ? ScheduleManager.GetScheduleDisplayName(entry.scheduleType)
                    : "予定なし";
                string state = hasEntry ? GetStateDisplayName(entry.state) : string.Empty;
                label.text = (day == selectedDay ? "▶ " : string.Empty) +
                    "Day " + day + "（" + weekday + "）\n" + scheduleName +
                    (string.IsNullOrEmpty(state) ? string.Empty : "\n" + state);
            }

            if (button != null && button.image != null)
            {
                Color color = day == selectedDay
                    ? selectedColor
                    : GetStateColor(hasEntry ? entry.state : (ScheduleEntryState?)null);
                ApplyButtonColor(button, color);
            }
        }

        if (weekRangeText != null)
        {
            weekRangeText.text = "Day " + weekStartDay + " ～ Day " + (weekStartDay + DaysPerWeek - 1);
        }
    }

    private void RefreshMonthCells()
    {
        if (!HasMonthlyUi) return;
        EnsureMonthlyDayCells();
        for (int i = 0; i < monthlyDayCells.Count; i++)
        {
            int day = weekStartDay + i;
            ScheduleEntry entry;
            bool hasEntry = scheduleManager.TryGetScheduleEntry(day, out entry);
            string weekday = ScheduleManager.GetWeekdayDisplayName(
                scheduleManager.GetWeekdayForDay(day));
            string scheduleName = hasEntry
                ? ScheduleManager.GetScheduleDisplayName(entry.scheduleType)
                : "予定なし";
            string state = hasEntry ? GetCompactStateMarker(entry.state) : string.Empty;
            string label = (day == selectedDay ? "▶ " : string.Empty) +
                "Day " + day + "（" + weekday + "）\n" + scheduleName +
                (string.IsNullOrEmpty(state) ? string.Empty : " " + state);
            Color color = day == selectedDay
                ? selectedColor
                : GetStateColor(hasEntry ? entry.state : (ScheduleEntryState?)null);
            monthlyDayCells[i].SetDisplay(day, label, color, true);
        }

        if (weekRangeText != null)
        {
            weekRangeText.text = "Day " + weekStartDay + " ～ Day " +
                (weekStartDay + DaysPerMonthView - 1) + "（30日表示）";
        }
    }

    private void RefreshSelectedDay()
    {
        ScheduleEntry entry;
        bool hasEntry = scheduleManager.TryGetScheduleEntry(selectedDay, out entry);
        string weekday = ScheduleManager.GetWeekdayDisplayName(
            scheduleManager.GetWeekdayForDay(selectedDay));
        string details = "Day " + selectedDay + "（" + weekday + "）\n";
        if (hasEntry)
        {
            details += "予定：" + ScheduleManager.GetScheduleDisplayName(entry.scheduleType) +
                "\n状態：" + GetStateDisplayName(entry.state);
            if (entry.state == ScheduleEntryState.Cancelled && !string.IsNullOrEmpty(entry.cancelReason))
            {
                details += "\nキャンセル理由：" + GetCancelReasonDisplayName(entry.cancelReason);
            }
        }
        else
        {
            details += "予定：なし";
        }

        if (selectedDayText != null)
        {
            selectedDayText.text = details;
        }
        else
        {
            RefreshMessage(details);
        }

        string unusedMessage;
        bool canEdit = scheduleManager.CanEditScheduleForDay(selectedDay, out unusedMessage);
        SetButtonsInteractable(scheduleChoiceButtons, canEdit);
        if (cancelButton != null)
        {
            cancelButton.interactable = scheduleManager.CanCancelScheduleForDay(selectedDay, out unusedMessage);
        }
    }

    private void RefreshSelectedDayLayout()
    {
        if (selectedDayArea == null) return;
        RectTransform source = currentView == CalendarView.Month
            ? monthlySelectedDayAnchor
            : weeklySelectedDayAnchor;
        if (source == null) return;

        selectedDayArea.anchorMin = source.anchorMin;
        selectedDayArea.anchorMax = source.anchorMax;
        selectedDayArea.pivot = source.pivot;
        selectedDayArea.anchoredPosition = source.anchoredPosition;
        selectedDayArea.sizeDelta = source.sizeDelta;
    }

    private void RefreshNavigation()
    {
        if (scheduleManager == null) return;
        if (previousWeekButton != null) previousWeekButton.interactable = weekStartDay > 1;
        if (nextWeekButton != null)
        {
            nextWeekButton.interactable = weekStartDay < GetMaximumPeriodStartDay();
        }
    }

    private int GetMaximumPeriodStartDay()
    {
        if (currentView == CalendarView.Month)
        {
            return scheduleManager.CurrentDayNumber;
        }
        return Mathf.Max(
            scheduleManager.CurrentDayNumber,
            scheduleManager.CurrentDayNumber +
                ScheduleManager.MaximumEditableDayOffset -
                (DaysPerWeek - 1));
    }

    private int GetVisibleDayCount()
    {
        return currentView == CalendarView.Month ? DaysPerMonthView : DaysPerWeek;
    }

    private void SwitchView(CalendarView view)
    {
        currentView = view;
        int maximumStartDay = GetMaximumPeriodStartDay();
        weekStartDay = Mathf.Clamp(selectedDay, 1, maximumStartDay);
        RefreshDisplay();
    }

    private void RefreshViewButtons()
    {
        if (weeklyViewButton != null) weeklyViewButton.interactable = currentView != CalendarView.Week;
        if (monthlyViewButton != null) monthlyViewButton.interactable = currentView != CalendarView.Month;
    }

    private void EnsureMonthlyDayCells()
    {
        if (!HasMonthlyUi || monthlyDayCells.Count == DaysPerMonthView) return;
        monthlyDayCells.Clear();
        monthlyDayCellTemplate.gameObject.SetActive(false);
        for (int i = 0; i < DaysPerMonthView; i++)
        {
            ScheduleDayCell cell = Instantiate(monthlyDayCellTemplate, monthlyGridRoot.transform);
            cell.name = "MonthDayCell" + i;
            cell.Initialize(SelectSpecificDay);
            cell.gameObject.SetActive(true);
            monthlyDayCells.Add(cell);
        }
    }

    private void SelectSpecificDay(int day)
    {
        selectedDay = day;
        RefreshDisplay();
    }

    private void RefreshMessage(string message)
    {
        if (messageText != null)
        {
            messageText.text = message;
        }
    }

    private void RefreshGameUi()
    {
        if (gameManager != null)
        {
            gameManager.RefreshUI();
        }
    }

    private Color GetStateColor(ScheduleEntryState? state)
    {
        if (!state.HasValue) return emptyColor;
        switch (state.Value)
        {
            case ScheduleEntryState.Planned: return plannedColor;
            case ScheduleEntryState.Executed: return executedColor;
            case ScheduleEntryState.Cancelled: return cancelledColor;
            default: return emptyColor;
        }
    }

    private static void ApplyButtonColor(Button button, Color color)
    {
        if (button == null) return;
        ColorBlock colors = button.colors;
        colors.normalColor = color;
        colors.selectedColor = color;
        colors.highlightedColor = Color.Lerp(color, Color.white, 0.15f);
        colors.pressedColor = Color.Lerp(color, Color.black, 0.15f);
        button.colors = colors;
        if (button.image != null)
        {
            button.image.color = color;
        }
    }

    private static string GetStateDisplayName(ScheduleEntryState state)
    {
        switch (state)
        {
            case ScheduleEntryState.Planned: return "設定済み";
            case ScheduleEntryState.Executed: return "実行済み";
            case ScheduleEntryState.Cancelled: return "キャンセル済み";
            default: return "不明";
        }
    }

    private static string GetCompactStateMarker(ScheduleEntryState state)
    {
        switch (state)
        {
            case ScheduleEntryState.Planned: return "○";
            case ScheduleEntryState.Executed: return "済";
            case ScheduleEntryState.Cancelled: return "×";
            default: return "?";
        }
    }

    private static string GetCancelReasonDisplayName(string reason)
    {
        return reason == "Player" ? "プレイヤー操作" : reason;
    }

    private static void SetButtonsInteractable(Button[] buttons, bool interactable)
    {
        if (buttons == null) return;
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null) buttons[i].interactable = interactable;
        }
    }

    private void EnsureWeeklyArrays()
    {
        if (dayButtons == null || dayButtons.Length != DaysPerWeek)
        {
            Button[] resizedButtons = new Button[DaysPerWeek];
            if (dayButtons != null)
            {
                for (int i = 0; i < Mathf.Min(dayButtons.Length, DaysPerWeek); i++)
                {
                    resizedButtons[i] = dayButtons[i];
                }
            }
            dayButtons = resizedButtons;
        }

        if (dayButtonTexts == null || dayButtonTexts.Length != DaysPerWeek)
        {
            TextMeshProUGUI[] resizedTexts = new TextMeshProUGUI[DaysPerWeek];
            if (dayButtonTexts != null)
            {
                for (int i = 0; i < Mathf.Min(dayButtonTexts.Length, DaysPerWeek); i++)
                {
                    resizedTexts[i] = dayButtonTexts[i];
                }
            }
            dayButtonTexts = resizedTexts;
        }
    }

    private T FindDescendantComponent<T>(string objectName) where T : Component
    {
        T[] components = GetComponentsInChildren<T>(true);
        for (int i = 0; i < components.Length; i++)
        {
            T component = components[i];
            if (component != null && component.gameObject.name == objectName)
            {
                return component;
            }
        }
        return null;
    }

    private Transform FindDescendantTransform(string objectName)
    {
        Transform[] transforms = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform child = transforms[i];
            if (child != null && child.name == objectName) return child;
        }
        return null;
    }

    public void Open()
    {
        gameObject.SetActive(true);
        ResetToCurrentWeek();
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }
}
