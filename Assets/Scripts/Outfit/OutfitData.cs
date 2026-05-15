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

    [Header("Suitability")]
    public bool anySeason = true;
    public List<Season> suitableSeasons = new List<Season>();

    public bool anyWeather = true;
    public List<Weather> suitableWeathers = new List<Weather>();

    [Header("Display")]
    public int sortOrder = 0;
    public bool isEnabled = true;
}
