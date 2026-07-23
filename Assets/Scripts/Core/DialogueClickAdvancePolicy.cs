public static class DialogueClickAdvancePolicy
{
    public static bool CanAdvance(
        bool componentEnabled,
        bool optionEnabled,
        bool nextButtonVisible,
        bool nextButtonInteractable)
    {
        return componentEnabled &&
            optionEnabled &&
            nextButtonVisible &&
            nextButtonInteractable;
    }
}
