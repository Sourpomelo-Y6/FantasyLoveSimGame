using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LayerEntry
{
    public string assetId;
    public string layerKind;
    public string costumeId;
    public string expressionId;
    public string displayName;
    public int drawOrder;
    public Sprite sprite;
}

[CreateAssetMenu(menuName = "LoveSim/Heroine Layered Sprite Data")]
public class HeroineLayeredSpriteData : ScriptableObject
{
    public string heroineId;
    public string defaultCostumeId = "Default";
    public string defaultExpressionId = "Neutral";
    public List<LayerEntry> baseBodyLayers = new List<LayerEntry>();
    public List<LayerEntry> costumeLayers = new List<LayerEntry>();
    public List<LayerEntry> expressionLayers = new List<LayerEntry>();
    public List<LayerEntry> accessoryLayers = new List<LayerEntry>();
}
