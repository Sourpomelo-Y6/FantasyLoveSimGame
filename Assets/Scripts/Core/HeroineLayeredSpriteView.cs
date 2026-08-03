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

    [Header("Eight Layer Composition")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image backAccessoryImage;
    [SerializeField] private Image backHairImage;
    [SerializeField] private Image costumeBodyImage;
    [SerializeField] private Image headExpressionImage;
    [SerializeField] private Image frontAccessoryImage;
    [SerializeField] private Image frontArmImage;
    [SerializeField] private Image effectImage;

    public bool HasData => layeredSpriteData != null;

    private bool warnedMissingBaseBody = false;

    private void Awake()
    {
        ResolveImageReferences();
        ConfigureLayerImage(baseBodyImage);
        ConfigureLayerImage(costumeImage);
        ConfigureLayerImage(expressionImage);
        ConfigureLayerImage(accessoryImage);
        ConfigureEightLayerImages();
    }

    public void SetData(HeroineLayeredSpriteData data)
    {
        layeredSpriteData = data;
        warnedMissingBaseBody = false;
        ResolveImageReferences();
        if (layeredSpriteData != null && layeredSpriteData.HasEightLayerData())
        {
            EnsureEightLayerImages();
        }
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

        if (layeredSpriteData.HasEightLayerData())
        {
            ClearLegacyLayers();
            return RefreshEightLayers(costumeId, expressionId);
        }

        ClearEightLayers();

        LayerEntry baseBodyLayer = GetFirstValidLayer(layeredSpriteData.baseBodyLayers);
        LayerEntry costumeLayer = FindLayerByCostumeId(costumeId);
        LayerEntry expressionLayer = FindLayerByExpressionId(expressionId);
        LayerEntry accessoryLayer = FindAccessoryLayer(costumeLayer, expressionLayer);
        LayerEntry visibleBaseBodyLayer = HasVisibleLayer(costumeLayer) ? null : baseBodyLayer;

        if (!HasVisibleLayer(baseBodyLayer) &&
            !HasVisibleLayer(costumeLayer) &&
            !warnedMissingBaseBody)
        {
            Debug.LogWarning("HeroineLayeredSpriteView: BaseBody レイヤーが見つからないため表示できません。");
            warnedMissingBaseBody = true;
        }

        ApplyLayer(baseBodyImage, visibleBaseBodyLayer);
        ApplyLayer(costumeImage, costumeLayer);
        ApplyLayer(expressionImage, expressionLayer);
        ApplyLayer(accessoryImage, accessoryLayer);
        ApplyLayerSiblingOrder(
            baseBodyImage,
            visibleBaseBodyLayer,
            costumeImage,
            costumeLayer,
            expressionImage,
            expressionLayer,
            accessoryImage,
            accessoryLayer);

        return HasVisibleLayer(visibleBaseBodyLayer) ||
            HasVisibleLayer(costumeLayer) ||
            HasVisibleLayer(expressionLayer) ||
            HasVisibleLayer(accessoryLayer);
    }

    public void ClearAll()
    {
        ClearLegacyLayers();
        ClearEightLayers();
    }

    private void ClearLegacyLayers()
    {
        ClearLayer(baseBodyImage);
        ClearLayer(costumeImage);
        ClearLayer(expressionImage);
        ClearLayer(accessoryImage);
    }

    private void ClearEightLayers()
    {
        ClearLayer(backgroundImage);
        ClearLayer(backAccessoryImage);
        ClearLayer(backHairImage);
        ClearLayer(costumeBodyImage);
        ClearLayer(headExpressionImage);
        ClearLayer(frontAccessoryImage);
        ClearLayer(frontArmImage);
        ClearLayer(effectImage);
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

        if (backgroundImage == null) backgroundImage = FindChildImage("BackgroundImage");
        if (backAccessoryImage == null) backAccessoryImage = FindChildImage("BackAccessoryImage");
        if (backHairImage == null) backHairImage = FindChildImage("BackHairImage");
        if (costumeBodyImage == null) costumeBodyImage = FindChildImage("CostumeBodyImage");
        if (headExpressionImage == null) headExpressionImage = FindChildImage("HeadExpressionImage");
        if (frontAccessoryImage == null) frontAccessoryImage = FindChildImage("FrontAccessoryImage");
        if (frontArmImage == null) frontArmImage = FindChildImage("FrontArmImage");
        if (effectImage == null) effectImage = FindChildImage("EffectImage");
    }

    private void ConfigureEightLayerImages()
    {
        ConfigureLayerImage(backgroundImage);
        ConfigureLayerImage(backAccessoryImage);
        ConfigureLayerImage(backHairImage);
        ConfigureLayerImage(costumeBodyImage);
        ConfigureLayerImage(headExpressionImage);
        ConfigureLayerImage(frontAccessoryImage);
        ConfigureLayerImage(frontArmImage);
        ConfigureLayerImage(effectImage);
    }

    private void EnsureEightLayerImages()
    {
        backgroundImage = EnsureLayerImage(backgroundImage, "BackgroundImage");
        backAccessoryImage = EnsureLayerImage(backAccessoryImage, "BackAccessoryImage");
        backHairImage = EnsureLayerImage(backHairImage, "BackHairImage");
        costumeBodyImage = EnsureLayerImage(costumeBodyImage, "CostumeBodyImage");
        headExpressionImage = EnsureLayerImage(headExpressionImage, "HeadExpressionImage");
        frontAccessoryImage = EnsureLayerImage(frontAccessoryImage, "FrontAccessoryImage");
        frontArmImage = EnsureLayerImage(frontArmImage, "FrontArmImage");
        effectImage = EnsureLayerImage(effectImage, "EffectImage");
        ConfigureEightLayerImages();
    }

    private Image EnsureLayerImage(Image image, string childName)
    {
        if (image != null)
        {
            return image;
        }

        Image existing = FindChildImage(childName);
        if (existing != null)
        {
            return existing;
        }

        GameObject child = new GameObject(
            childName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        RectTransform rectTransform = child.GetComponent<RectTransform>();
        rectTransform.SetParent(transform, false);
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        Image created = child.GetComponent<Image>();
        ClearLayer(created);
        return created;
    }

    private bool RefreshEightLayers(string costumeId, string expressionId)
    {
        string resolvedCostumeId = string.IsNullOrEmpty(costumeId)
            ? GetDefaultCostumeId()
            : costumeId;
        string resolvedExpressionId = string.IsNullOrEmpty(expressionId)
            ? GetDefaultExpressionId()
            : expressionId;

        LayerEntry background = FindBestConditionalLayer(
            layeredSpriteData.backgroundLayers, resolvedCostumeId, resolvedExpressionId, false, false);
        LayerEntry backAccessory = FindBestConditionalLayer(
            layeredSpriteData.backAccessoryLayers, resolvedCostumeId, resolvedExpressionId, false, false);
        LayerEntry backHair = FindBestConditionalLayer(
            layeredSpriteData.backHairLayers, resolvedCostumeId, resolvedExpressionId, false, false);
        LayerEntry costumeBody = FindBestConditionalLayer(
            layeredSpriteData.costumeBodyLayers, resolvedCostumeId, resolvedExpressionId, true, false);
        LayerEntry headExpression = FindBestConditionalLayer(
            layeredSpriteData.headExpressionLayers, resolvedCostumeId, resolvedExpressionId, false, true);
        LayerEntry frontAccessory = FindBestConditionalLayer(
            layeredSpriteData.frontAccessoryLayers, resolvedCostumeId, resolvedExpressionId, false, false);
        LayerEntry frontArm = FindBestConditionalLayer(
            layeredSpriteData.frontArmLayers, resolvedCostumeId, resolvedExpressionId, false, false);
        LayerEntry effect = FindBestConditionalLayer(
            layeredSpriteData.effectLayers, resolvedCostumeId, resolvedExpressionId, false, false);

        // 8階層へ段階移行しているデータでは、未移行の衣装や表情だけ旧4階層から補う。
        // 一部の8階層データがあるだけで全旧レイヤーを非表示にすると、未移行衣装が消えてしまう。
        LayerEntry exactLegacyCostume = FindLayerById(
            layeredSpriteData.costumeLayers, resolvedCostumeId);
        if (HasVisibleLayer(exactLegacyCostume) &&
            (costumeBody == null || costumeBody.costumeId != resolvedCostumeId))
        {
            costumeBody = exactLegacyCostume;
        }
        if (!HasVisibleLayer(costumeBody))
        {
            costumeBody = FindLayerByCostumeId(resolvedCostumeId);
        }
        if (!HasVisibleLayer(costumeBody))
        {
            costumeBody = GetFirstValidLayer(layeredSpriteData.baseBodyLayers);
        }
        LayerEntry exactLegacyExpression = FindLayerById(
            layeredSpriteData.expressionLayers, resolvedExpressionId);
        if (HasVisibleLayer(exactLegacyExpression) &&
            (headExpression == null || headExpression.expressionId != resolvedExpressionId))
        {
            headExpression = exactLegacyExpression;
        }
        if (!HasVisibleLayer(headExpression))
        {
            headExpression = FindLayerByExpressionId(resolvedExpressionId);
        }
        if (!HasVisibleLayer(frontAccessory))
        {
            frontAccessory = FindAccessoryLayer(costumeBody, headExpression);
        }

        ApplyLayer(backgroundImage, background);
        ApplyLayer(backAccessoryImage, backAccessory);
        ApplyLayer(backHairImage, backHair);
        ApplyLayer(costumeBodyImage, costumeBody);
        ApplyLayer(headExpressionImage, headExpression);
        ApplyLayer(frontAccessoryImage, frontAccessory);
        ApplyLayer(frontArmImage, frontArm);
        ApplyLayer(effectImage, effect);

        List<LayerImagePair> pairs = new List<LayerImagePair>
        {
            new LayerImagePair(backgroundImage, background),
            new LayerImagePair(backAccessoryImage, backAccessory),
            new LayerImagePair(backHairImage, backHair),
            new LayerImagePair(costumeBodyImage, costumeBody),
            new LayerImagePair(headExpressionImage, headExpression),
            new LayerImagePair(frontAccessoryImage, frontAccessory),
            new LayerImagePair(frontArmImage, frontArm),
            new LayerImagePair(effectImage, effect)
        };
        ApplyLayerSiblingOrder(pairs);

        bool hasVisibleLayer = pairs.Exists(pair => pair.HasVisibleLayer);
        if (!hasVisibleLayer && !warnedMissingBaseBody)
        {
            Debug.LogWarning("HeroineLayeredSpriteView: 8階層に表示可能なSpriteがありません。");
            warnedMissingBaseBody = true;
        }

        return hasVisibleLayer;
    }

    private LayerEntry FindBestConditionalLayer(
        List<LayerEntry> layers,
        string costumeId,
        string expressionId,
        bool preferCostume,
        bool preferExpression)
    {
        if (layers == null)
        {
            return null;
        }

        LayerEntry best = null;
        int bestScore = int.MinValue;
        foreach (LayerEntry layer in layers)
        {
            if (!HasVisibleLayer(layer))
            {
                continue;
            }

            bool costumeMatches = string.IsNullOrEmpty(layer.costumeId) || layer.costumeId == costumeId;
            bool expressionMatches = string.IsNullOrEmpty(layer.expressionId) || layer.expressionId == expressionId;
            if (!costumeMatches || !expressionMatches)
            {
                continue;
            }

            int score = 0;
            if (!string.IsNullOrEmpty(layer.costumeId)) score += preferCostume ? 4 : 2;
            if (!string.IsNullOrEmpty(layer.expressionId)) score += preferExpression ? 4 : 2;
            if (best == null || score > bestScore ||
                (score == bestScore && layer.drawOrder < best.drawOrder))
            {
                best = layer;
                bestScore = score;
            }
        }

        if (best == null && preferCostume && costumeId != GetDefaultCostumeId())
        {
            return FindBestConditionalLayer(
                layers, GetDefaultCostumeId(), expressionId, false, preferExpression);
        }

        if (best == null && preferExpression && expressionId != GetDefaultExpressionId())
        {
            return FindBestConditionalLayer(
                layers, costumeId, GetDefaultExpressionId(), preferCostume, false);
        }

        return best;
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

        ApplyLayerSiblingOrder(pairs);
    }

    private static void ApplyLayerSiblingOrder(List<LayerImagePair> pairs)
    {
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
        public bool HasVisibleLayer => Image != null && Image.enabled && Image.sprite != null;

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
