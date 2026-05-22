using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "LoveSim/Outfit Data")]
public class OutfitData : ScriptableObject
{
    [Header("Basic")]
    public string outfitId;
    public string displayName;

    [Header("Visual")]
    public Sprite heroineSprite;

    [Header("Unlock")]
    public bool isUnlockedByDefault = true;
    public int requiredAffection = 0;

    [Header("Messages")]
    [TextArea(2, 4)]
    public string lockedMessage = "ÇªÇÍÇÕÇ‹Çæè≠ÇµípÇ∏Ç©ÇµÇ¢ÇÊÇ§Ç≈Ç∑ÅB";

    [TextArea(2, 4)]
    public string changedMessage = "";

    [Header("Suitability")]
    public bool anySeason = true;
    public List<Season> suitableSeasons = new List<Season>();

    public bool anyWeather = true;
    public List<Weather> suitableWeathers = new List<Weather>();

    [Header("Outfit Trait")]
    public bool isWarmOutfit = false;
    public bool isLightOutfit = false;
    public bool isRainOutfit = false;
    public bool isSnowOutfit = false;
    public bool isIndoorOutfit = false;
    public bool isOutdoorOutfit = false;

    [Header("Display")]
    public int sortOrder = 0;
    public bool isEnabled = true;
}
