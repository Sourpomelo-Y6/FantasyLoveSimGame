using System;

[Serializable]
public class OutfitPromptAbilitySet
{
    public ScheduledEventOutfitPromptMode selectedMode = ScheduledEventOutfitPromptMode.Always;

    public OutfitPromptAbilitySet Clone()
    {
        return new OutfitPromptAbilitySet
        {
            selectedMode = selectedMode
        };
    }

    public void CopyFrom(OutfitPromptAbilitySet source)
    {
        if (source == null)
        {
            return;
        }

        selectedMode = source.selectedMode;
    }
}
