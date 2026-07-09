using System;

[Serializable]
public class OutfitPromptAbilitySet
{
    public bool canUseConditionalMode = true;
    public bool canUseHiddenMode = false;
    public ScheduledEventOutfitPromptMode selectedMode = ScheduledEventOutfitPromptMode.Always;

    public OutfitPromptAbilitySet Clone()
    {
        return new OutfitPromptAbilitySet
        {
            canUseConditionalMode = canUseConditionalMode,
            canUseHiddenMode = canUseHiddenMode,
            selectedMode = selectedMode
        };
    }

    public void CopyFrom(OutfitPromptAbilitySet source)
    {
        if (source == null)
        {
            return;
        }

        canUseConditionalMode = source.canUseConditionalMode;
        canUseHiddenMode = source.canUseHiddenMode;
        selectedMode = source.selectedMode;
    }
}
