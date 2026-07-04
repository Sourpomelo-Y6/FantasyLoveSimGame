using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PlayerAssetEntry
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

[CreateAssetMenu(menuName = "LoveSim/Player Asset Catalog")]
public class PlayerAssetCatalog : ScriptableObject
{
    public string playerId = "Player";
    public List<PlayerAssetEntry> assets = new List<PlayerAssetEntry>();
}
