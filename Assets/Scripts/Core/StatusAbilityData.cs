using UnityEngine;

[CreateAssetMenu(menuName = "LoveSim/Status Ability Data")]
public class StatusAbilityData : ScriptableObject
{
    [Header("Basic")]
    public string abilityId;
    public StatusAbilityKind abilityKind = StatusAbilityKind.ConditionalOutfitPrompt;
    public StatusDetailRole targetRole = StatusDetailRole.Player;

    [Header("Display")]
    public string displayName;

    [TextArea(2, 5)]
    public string description;

    public int sortOrder = 0;
    public bool isEnabled = true;
}
