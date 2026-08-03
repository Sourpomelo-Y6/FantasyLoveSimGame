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

public static class HeroineVisualLayerKinds
{
    public const string Background = "Background";
    public const string BackAccessory = "BackAccessory";
    public const string BackHair = "BackHair";
    public const string CostumeBody = "CostumeBody";
    public const string HeadExpression = "HeadExpression";
    public const string FrontAccessory = "FrontAccessory";
    public const string FrontArm = "FrontArm";
    public const string Effect = "Effect";

    // 旧4階層。既存アセットと旧Exportの読み込み互換用に残す。
    public const string LegacyBaseBody = "BaseBody";
    public const string LegacyCostume = "Costume";
    public const string LegacyExpression = "Expression";
    public const string LegacyAccessory = "Accessory";
}

[CreateAssetMenu(menuName = "LoveSim/Heroine Layered Sprite Data")]
public class HeroineLayeredSpriteData : ScriptableObject
{
    public string heroineId;
    public string defaultCostumeId = "Default";
    public string defaultExpressionId = "Neutral";

    [Header("Eight Layer Composition")]
    public List<LayerEntry> backgroundLayers = new List<LayerEntry>();
    public List<LayerEntry> backAccessoryLayers = new List<LayerEntry>();
    public List<LayerEntry> backHairLayers = new List<LayerEntry>();
    public List<LayerEntry> costumeBodyLayers = new List<LayerEntry>();
    public List<LayerEntry> headExpressionLayers = new List<LayerEntry>();
    public List<LayerEntry> frontAccessoryLayers = new List<LayerEntry>();
    public List<LayerEntry> frontArmLayers = new List<LayerEntry>();
    public List<LayerEntry> effectLayers = new List<LayerEntry>();

    [Header("Legacy Four Layer Composition")]
    [Tooltip("旧BaseBody。既存データ互換用です。新規データではBackHair/CostumeBody等を使用します。")]
    public List<LayerEntry> baseBodyLayers = new List<LayerEntry>();
    public List<LayerEntry> costumeLayers = new List<LayerEntry>();
    public List<LayerEntry> expressionLayers = new List<LayerEntry>();
    public List<LayerEntry> accessoryLayers = new List<LayerEntry>();

    public bool HasEightLayerData()
    {
        return HasEntries(backgroundLayers) ||
            HasEntries(backAccessoryLayers) ||
            HasEntries(backHairLayers) ||
            HasEntries(costumeBodyLayers) ||
            HasEntries(headExpressionLayers) ||
            HasEntries(frontAccessoryLayers) ||
            HasEntries(frontArmLayers) ||
            HasEntries(effectLayers);
    }

    private static bool HasEntries(List<LayerEntry> entries)
    {
        return entries != null && entries.Count > 0;
    }
}
