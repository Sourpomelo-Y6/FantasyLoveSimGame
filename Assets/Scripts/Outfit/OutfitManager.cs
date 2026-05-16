using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OutfitManager : MonoBehaviour
{
    [Header("Managers")]
    [SerializeField] private HeroineStatus heroineStatus;

    [Header("View")]
    [SerializeField] private Image heroineImage;

    [Header("Outfit Data")]
    [SerializeField] private string outfitResourcePath = "Outfits";

    private List<OutfitData> outfits = new List<OutfitData>();

    private OutfitData currentOutfit;

    public OutfitData CurrentOutfit => currentOutfit;
    public IReadOnlyList<OutfitData> Outfits => outfits;

    private void Awake()
    {
        LoadOutfitsFromResources();
    }

    private void LoadOutfitsFromResources()
    {
        OutfitData[] loadedOutfits =
            Resources.LoadAll<OutfitData>(outfitResourcePath);

        outfits = new List<OutfitData>(loadedOutfits);

        outfits.Sort((a, b) => a.sortOrder.CompareTo(b.sortOrder));

        Debug.Log("Loaded Outfits: " + outfits.Count);

        foreach (OutfitData outfit in outfits)
        {
            Debug.Log(
                "Outfit: " +
                outfit.name +
                " / Id: " +
                outfit.outfitId +
                " / Display: " +
                outfit.displayName
            );
        }
    }

    public bool CanWearOutfit(OutfitData outfit)
    {
        if (outfit == null)
        {
            return false;
        }

        if (!outfit.isEnabled)
        {
            return false;
        }

        if (!outfit.isUnlockedByDefault)
        {
            if (heroineStatus.Affection < outfit.requiredAffection)
            {
                return false;
            }
        }

        if (heroineStatus.Affection < outfit.requiredAffection)
        {
            return false;
        }

        return true;
    }

    public bool TryChangeOutfit(OutfitData outfit, out string message)
    {
        message = "";

        if (outfit == null)
        {
            message = "衣装データがありません。";
            return false;
        }

        if (!outfit.isEnabled)
        {
            message = "この衣装は現在使用できません。";
            return false;
        }

        if (!CanWearOutfit(outfit))
        {
            if (!string.IsNullOrEmpty(outfit.lockedMessage))
            {
                message = outfit.lockedMessage;
            }
            else
            {
                message = "「" + outfit.displayName + "」は、まだ少し恥ずかしいようです。";
            }

            return false;
        }

        currentOutfit = outfit;

        if (heroineImage != null && outfit.heroineSprite != null)
        {
            heroineImage.sprite = outfit.heroineSprite;
            heroineImage.color = Color.white;
        }

        if (!string.IsNullOrEmpty(outfit.changedMessage))
        {
            message = outfit.changedMessage;
        }
        else
        {
            message = "ヒロインは「" + outfit.displayName + "」に着替えました。";
        }

        return true;
    }

    public void WearDefaultOutfit()
    {
        if (outfits == null || outfits.Count == 0)
        {
            return;
        }

        foreach (OutfitData outfit in outfits)
        {
            if (outfit != null && outfit.isEnabled && CanWearOutfit(outfit))
            {
                string message;
                TryChangeOutfit(outfit, out message);
                return;
            }
        }
    }

    public OutfitData FindOutfitById(string outfitId)
    {
        if (string.IsNullOrEmpty(outfitId))
        {
            return null;
        }

        foreach (OutfitData outfit in outfits)
        {
            if (outfit != null && outfit.outfitId == outfitId)
            {
                return outfit;
            }
        }

        return null;
    }

    public bool TryChangeOutfitById(string outfitId, out string message)
    {
        OutfitData outfit = FindOutfitById(outfitId);

        if (outfit == null)
        {
            message = "指定された衣装が見つかりません。";
            return false;
        }

        return TryChangeOutfit(outfit, out message);
    }
}
