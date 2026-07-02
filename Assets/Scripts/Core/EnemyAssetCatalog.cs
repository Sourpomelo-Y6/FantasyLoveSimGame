using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EnemyAssetEntry
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

[CreateAssetMenu(menuName = "LoveSim/Enemy Asset Catalog")]
public class EnemyAssetCatalog : ScriptableObject
{
    public string enemyId;
    public List<EnemyAssetEntry> assets = new List<EnemyAssetEntry>();
}
