using TMPro;
using UnityEngine;

public class SchedulePanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ScheduleManager scheduleManager;
    [SerializeField] private GameManager gameManager;

    [Header("Message UI")]
    [SerializeField] private TextMeshProUGUI messageText;

    private void OnEnable()
    {
        RefreshMessage();
    }

    public void SelectNone()
    {
        SelectSchedule(ScheduleType.None);
    }

    public void SelectSoloForest()
    {
        SelectSchedule(ScheduleType.SoloForest);
    }

    public void SelectSoloCave()
    {
        SelectSchedule(ScheduleType.SoloCave);
    }

    public void SelectSoloLake()
    {
        SelectSchedule(ScheduleType.SoloLake);
    }

    public void SelectSoloShopping()
    {
        SelectSchedule(ScheduleType.SoloShopping);
    }

    public void SelectDuoForest()
    {
        SelectSchedule(ScheduleType.DuoForest);
    }

    public void SelectDuoCave()
    {
        SelectSchedule(ScheduleType.DuoCave);
    }

    public void SelectDuoLake()
    {
        SelectSchedule(ScheduleType.DuoLake);
    }

    public void SelectDuoShopping()
    {
        SelectSchedule(ScheduleType.DuoShopping);
    }

    public void SelectStayHome()
    {
        SelectSchedule(ScheduleType.StayHome);
    }

    private void SelectSchedule(ScheduleType scheduleType)
    {
        if (scheduleManager == null)
        {
            Debug.LogWarning("ScheduleManager Ç™ê›íËÇ≥ÇÍÇƒÇ¢Ç‹ÇπÇÒÅB");
            return;
        }

        scheduleManager.SetTomorrowSchedule(scheduleType);

        RefreshMessage();

        if (gameManager != null)
        {
            gameManager.RefreshUI();
        }
    }

    private void RefreshMessage()
    {
        if (messageText == null || scheduleManager == null)
        {
            return;
        }

        messageText.text =
            "ñæì˙ÇÃó\íËÅF" + scheduleManager.GetTomorrowScheduleDisplayName();
    }

    public void Open()
    {
        gameObject.SetActive(true);
        RefreshMessage();
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }
}