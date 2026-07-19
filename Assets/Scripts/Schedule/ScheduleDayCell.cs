using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScheduleDayCell : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI label;

    private int day;
    private Action<int> onSelected;
    private bool hooked;

    private void Awake()
    {
        ResolveReferences();
        HookButton();
    }

    public void Initialize(Action<int> selectedCallback)
    {
        ResolveReferences();
        HookButton();
        onSelected = selectedCallback;
    }

    public void SetDisplay(
        int targetDay,
        string text,
        Color color,
        bool interactable)
    {
        day = targetDay;
        if (label != null) label.text = text;
        if (button != null)
        {
            button.interactable = interactable;
            ApplyButtonColor(button, color);
        }
    }

    private void ResolveReferences()
    {
        if (button == null) button = GetComponent<Button>();
        if (button == null) button = GetComponentInChildren<Button>(true);
        if (label == null) label = GetComponentInChildren<TextMeshProUGUI>(true);
    }

    private void HookButton()
    {
        if (hooked || button == null) return;
        button.onClick.AddListener(HandleClick);
        hooked = true;
    }

    private void HandleClick()
    {
        if (onSelected != null) onSelected(day);
    }

    private static void ApplyButtonColor(Button targetButton, Color color)
    {
        ColorBlock colors = targetButton.colors;
        colors.normalColor = color;
        colors.selectedColor = color;
        colors.highlightedColor = Color.Lerp(color, Color.white, 0.15f);
        colors.pressedColor = Color.Lerp(color, Color.black, 0.15f);
        targetButton.colors = colors;
        if (targetButton.image != null) targetButton.image.color = color;
    }
}
