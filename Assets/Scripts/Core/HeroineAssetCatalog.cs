using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class HeroineAssetEntry
{
    public string assetId;
    public string usage;
    public string status;
    public string fileName;
    public string memo;
    public Sprite sprite;
    public string unityImagePath;
    public string exportPromptPath;
}

[CreateAssetMenu(menuName = "LoveSim/Heroine Asset Catalog")]
public class HeroineAssetCatalog : ScriptableObject
{
    public string heroineId;
    public List<HeroineAssetEntry> assets = new List<HeroineAssetEntry>();
}
