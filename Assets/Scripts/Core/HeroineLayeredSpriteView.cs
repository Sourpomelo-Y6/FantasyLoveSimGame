using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HeroineLayeredSpriteView : MonoBehaviour
{
    private const string DefaultCostumeFallbackId = "Default";
    private const string DefaultExpressionFallbackId = "Neutral";

    [SerializeField] private HeroineLayeredSpriteData layeredSpriteData;
    [SerializeField] private Image baseBodyImage;
    [SerializeField] private Image costumeImage;
    [SerializeField] private Image expressionImage;
    [SerializeField] private Image accessoryImage;

    public bool HasData => layeredSpriteData != null;

    private bool warnedMissingBaseBody = false;

    private void Awake()
    {
        ResolveImageReferences();
        ConfigureLayerImage(baseBodyImage);
        ConfigureLayerImage(costumeImage);
        ConfigureLayerImage(expressionImage);
        ConfigureLayerImage(accessoryImage);
    }

    public void SetData(HeroineLayeredSpriteData data)
    {
        layeredSpriteData = data;
        warnedMissingBaseBody = false;
        ResolveImageReferences();
        Refresh(
            GetDefaultCostumeId(),
            GetDefaultExpressionId());
    }

    public bool Refresh(string costumeId, string expressionId)
    {
        ResolveImageReferences();

        if (layeredSpriteData == null)
        {
            ClearAll();
            return false;
        }

        LayerEntry baseBodyLayer = GetFirstValidLayer(layeredSpriteData.baseBodyLayers);
        LayerEntry costumeLayer = FindLayerByCostumeId(costumeId);
        LayerEntry expressionLayer = FindLayerByExpressionId(expressionId);
        LayerEntry accessoryLayer = FindAccessoryLayer(costumeLayer, expressionLayer);

        if (!HasVisibleLayer(baseBodyLayer) && !warnedMissingBaseBody)
        {
            Debug.LogWarning("HeroineLayeredSpriteView: BaseBody レイヤーが見つからないため表示できません。");
            warnedMissingBaseBody = true;
        }

        ApplyLayer(baseBodyImage, baseBodyLayer);
        ApplyLayer(costumeImage, costumeLayer);
        ApplyLayer(expressionImage, expressionLayer);
        ApplyLayer(accessoryImage, accessoryLayer);
        ApplyLayerSiblingOrder(
            baseBodyImage,
            baseBodyLayer,
            costumeImage,
            costumeLayer,
            expressionImage,
            expressionLayer,
            accessoryImage,
            accessoryLayer);

        return HasVisibleLayer(baseBodyLayer) ||
            HasVisibleLayer(costumeLayer) ||
            HasVisibleLayer(expressionLayer) ||
            HasVisibleLayer(accessoryLayer);
    }

    public void ClearAll()
    {
        ClearLayer(baseBodyImage);
        ClearLayer(costumeImage);
        ClearLayer(expressionImage);
        ClearLayer(accessoryImage);
    }

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }

    private void ResolveImageReferences()
    {
        if (baseBodyImage == null)
        {
            baseBodyImage = FindChildImage("BaseBodyImage");
        }

        if (costumeImage == null)
        {
            costumeImage = FindChildImage("CostumeImage");
        }

        if (expressionImage == null)
        {
            expressionImage = FindChildImage("ExpressionImage");
        }

        if (accessoryImage == null)
        {
            accessoryImage = FindChildImage("AccessoryImage");
        }
    }

    private Image FindChildImage(string childName)
    {
        Transform child = transform.Find(childName);
        if (child == null)
        {
            return null;
        }

        return child.GetComponent<Image>();
    }

    private void ConfigureLayerImage(Image image)
    {
        if (image == null)
        {
            return;
        }

        image.raycastTarget = false;
        image.preserveAspect = false;
    }

    private LayerEntry FindLayerByCostumeId(string costumeId)
    {
        LayerEntry layer = FindLayerById(layeredSpriteData.costumeLayers, costumeId);
        if (layer != null)
        {
            return layer;
        }

        return FindLayerById(layeredSpriteData.costumeLayers, GetDefaultCostumeId());
    }

    private LayerEntry FindLayerByExpressionId(string expressionId)
    {
        LayerEntry layer = FindLayerById(layeredSpriteData.expressionLayers, expressionId);
        if (layer != null)
        {
            return layer;
        }

        return FindLayerById(layeredSpriteData.expressionLayers, GetDefaultExpressionId());
    }

    private LayerEntry FindAccessoryLayer(LayerEntry costumeLayer, LayerEntry expressionLayer)
    {
        if (layeredSpriteData.accessoryLayers == null)
        {
            return null;
        }

        foreach (LayerEntry layer in layeredSpriteData.accessoryLayers)
        {
            if (!HasVisibleLayer(layer))
            {
                continue;
            }

            bool costumeMatches = string.IsNullOrEmpty(layer.costumeId) ||
                (costumeLayer != null && layer.costumeId == costumeLayer.costumeId);
            bool expressionMatches = string.IsNullOrEmpty(layer.expressionId) ||
                (expressionLayer != null && layer.expressionId == expressionLayer.expressionId);

            if (costumeMatches && expressionMatches)
            {
                return layer;
            }
        }

        return null;
    }

    private static LayerEntry FindLayerById(List<LayerEntry> layers, string id)
    {
        if (layers == null || string.IsNullOrEmpty(id))
        {
            return null;
        }

        foreach (LayerEntry layer in layers)
        {
            if (layer == null)
            {
                continue;
            }

            if (layer.costumeId == id || layer.expressionId == id || layer.assetId == id)
            {
                return layer;
            }
        }

        return null;
    }

    private static LayerEntry GetFirstValidLayer(List<LayerEntry> layers)
    {
        if (layers == null)
        {
            return null;
        }

        foreach (LayerEntry layer in layers)
        {
            if (HasVisibleLayer(layer))
            {
                return layer;
            }
        }

        return null;
    }

    private void ApplyLayer(Image image, LayerEntry layer)
    {
        if (image == null)
        {
            return;
        }

        if (!HasVisibleLayer(layer))
        {
            ClearLayer(image);
            return;
        }

        image.sprite = layer.sprite;
        image.color = Color.white;
        image.enabled = true;
    }

    private static void ClearLayer(Image image)
    {
        if (image == null)
        {
            return;
        }

        image.sprite = null;
        image.color = new Color(1f, 1f, 1f, 0f);
        image.enabled = false;
    }

    private static void ApplyLayerSiblingOrder(
        Image baseBody,
        LayerEntry baseBodyLayer,
        Image costume,
        LayerEntry costumeLayer,
        Image expression,
        LayerEntry expressionLayer,
        Image accessory,
        LayerEntry accessoryLayer)
    {
        List<LayerImagePair> pairs = new List<LayerImagePair>
        {
            new LayerImagePair(baseBody, baseBodyLayer),
            new LayerImagePair(costume, costumeLayer),
            new LayerImagePair(expression, expressionLayer),
            new LayerImagePair(accessory, accessoryLayer)
        };

        pairs.Sort((a, b) => a.DrawOrder.CompareTo(b.DrawOrder));

        int siblingIndex = 0;
        foreach (LayerImagePair pair in pairs)
        {
            if (pair.Image == null || pair.Image.transform.parent == null)
            {
                continue;
            }

            pair.Image.transform.SetSiblingIndex(siblingIndex);
            siblingIndex++;
        }
    }

    private struct LayerImagePair
    {
        public readonly Image Image;
        public readonly int DrawOrder;

        public LayerImagePair(Image image, LayerEntry layer)
        {
            Image = image;
            DrawOrder = layer != null ? layer.drawOrder : int.MaxValue;
        }
    }

    private string GetDefaultCostumeId()
    {
        if (layeredSpriteData != null &&
            !string.IsNullOrEmpty(layeredSpriteData.defaultCostumeId))
        {
            return layeredSpriteData.defaultCostumeId;
        }

        return DefaultCostumeFallbackId;
    }

    private string GetDefaultExpressionId()
    {
        if (layeredSpriteData != null &&
            !string.IsNullOrEmpty(layeredSpriteData.defaultExpressionId))
        {
            return layeredSpriteData.defaultExpressionId;
        }

        return DefaultExpressionFallbackId;
    }

    private static bool HasVisibleLayer(LayerEntry layer)
    {
        return layer != null && layer.sprite != null;
    }
}
