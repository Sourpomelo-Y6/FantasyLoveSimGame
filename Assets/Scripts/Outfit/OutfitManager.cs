using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OutfitManager : MonoBehaviour
{
    [Header("Managers")]
    [SerializeField] private HeroineStatus heroineStatus;
    [SerializeField] private TimeManager timeManager;

    [Header("Preference")]
    [SerializeField] private OutfitPreferenceManager outfitPreferenceManager;

    [Header("Schedule")]
    [SerializeField] private ScheduleManager scheduleManager;

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

        if (scheduleManager == null)
        {
            scheduleManager = FindObjectOfType<ScheduleManager>();
        }
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

        if (outfitPreferenceManager != null)
        {
            outfitPreferenceManager.RegisterWear(outfit.outfitId);
        }

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

    public bool AutoChooseOutfitForToday(out string message)
    {
        message = "";

        List<OutfitData> candidates = new List<OutfitData>();

        foreach (OutfitData outfit in outfits)
        {
            if (outfit == null)
            {
                continue;
            }

            if (!CanWearOutfit(outfit))
            {
                continue;
            }

            candidates.Add(outfit);
        }

        if (candidates.Count == 0)
        {
            message = "着られる衣装がありません。";
            return false;
        }

        OutfitData selectedOutfit = SelectOutfitByScore(candidates);

        if (selectedOutfit == null)
        {
            message = "衣装を選べませんでした。";
            return false;
        }

        bool success = TryChangeOutfit(selectedOutfit, out message);

        if (success)
        {
            message = "ヒロインは今日の服として「" + selectedOutfit.displayName + "」を選びました。";
        }

        return success;
    }

    private OutfitData SelectOutfitByScore(List<OutfitData> candidates)
    {
        if (candidates == null || candidates.Count == 0)
        {
            return null;
        }

        int totalWeight = 0;
        List<int> weights = new List<int>();

        foreach (OutfitData outfit in candidates)
        {
            int weight = CalculateOutfitWeight(outfit);

            if (weight < 1)
            {
                weight = 1;
            }

            Debug.Log("Outfit Weight: " + outfit.displayName + " = " + weight);

            weights.Add(weight);
            totalWeight += weight;
        }

        int randomValue = Random.Range(0, totalWeight);
        int current = 0;

        for (int i = 0; i < candidates.Count; i++)
        {
            current += weights[i];

            if (randomValue < current)
            {
                return candidates[i];
            }
        }

        return candidates[candidates.Count - 1];
    }

    private int CalculateOutfitWeight(OutfitData outfit)
    {
        int weight = 10;

        // プレイヤーの評価
        if (outfitPreferenceManager != null)
        {
            OutfitPreference preference = outfitPreferenceManager.FindPreference(outfit.outfitId);

            if (preference != null)
            {
                weight += preference.score;
                weight -= preference.boredCount * 3;
            }
        }

        // 同じ服ばかりを少し避ける
        if (currentOutfit != null && currentOutfit.outfitId == outfit.outfitId)
        {
            weight -= 3;
        }

        // 季節適性
        if (IsSuitableForCurrentSeason(outfit))
        {
            weight += 8;
        }
        else
        {
            weight -= 8;
        }

        // 天気適性
        if (IsSuitableForCurrentWeather(outfit))
        {
            weight += 8;
        }
        else
        {
            weight -= 8;
        }

        // 詳細な季節補正
        weight += CalculateSeasonTraitBonus(outfit);

        // 詳細な天気補正
        weight += CalculateWeatherTraitBonus(outfit);

        if (weight < 1)
        {
            weight = 1;
        }

        Debug.Log("Outfit Weight: " + outfit.displayName + " = " + weight);

        return weight;
    }

    private bool IsSuitableForCurrentSeason(OutfitData outfit)
    {
        if (outfit == null)
        {
            return false;
        }

        if (outfit.anySeason)
        {
            return true;
        }

        if (timeManager == null)
        {
            return true;
        }

        if (outfit.suitableSeasons == null)
        {
            return false;
        }

        return outfit.suitableSeasons.Contains(timeManager.CurrentSeason);
    }

    private bool IsSuitableForCurrentWeather(OutfitData outfit)
    {
        if (outfit == null)
        {
            return false;
        }

        if (outfit.anyWeather)
        {
            return true;
        }

        if (timeManager == null)
        {
            return true;
        }

        if (outfit.suitableWeathers == null)
        {
            return false;
        }

        return outfit.suitableWeathers.Contains(timeManager.CurrentWeather);
    }

    private int CalculateSeasonTraitBonus(OutfitData outfit)
    {
        if (timeManager == null || outfit == null)
        {
            return 0;
        }

        int bonus = 0;

        if (timeManager.CurrentSeason == Season.Summer)
        {
            if (outfit.isLightOutfit)
            {
                bonus += 10;
            }

            if (outfit.isWarmOutfit)
            {
                bonus -= 15;
            }
        }

        if (timeManager.CurrentSeason == Season.Winter)
        {
            if (outfit.isWarmOutfit)
            {
                bonus += 10;
            }

            if (outfit.isLightOutfit)
            {
                bonus -= 15;
            }
        }

        if (timeManager.CurrentSeason == Season.Spring)
        {
            if (outfit.isOutdoorOutfit)
            {
                bonus += 3;
            }

            if (outfit.isWarmOutfit)
            {
                bonus -= 2;
            }
        }

        if (timeManager.CurrentSeason == Season.Autumn)
        {
            if (outfit.isWarmOutfit)
            {
                bonus += 3;
            }

            if (outfit.isLightOutfit)
            {
                bonus -= 3;
            }
        }

        return bonus;
    }

    private int CalculateScheduleTraitBonus(OutfitData outfit)
    {
        if (scheduleManager == null || outfit == null)
        {
            return 0;
        }

        int bonus = 0;

        if (scheduleManager.IsTodayHomeSchedule())
        {
            if (outfit.isIndoorOutfit)
            {
                bonus += 10;
            }

            if (outfit.isWarmOutfit)
            {
                bonus += 3;
            }

            if (outfit.isOutdoorOutfit)
            {
                bonus -= 12;
            }
        }

        if (scheduleManager.IsTodayDuoSchedule())
        {
            if (outfit.isOutdoorOutfit)
            {
                bonus += 8;
            }

            if (outfit.isLightOutfit)
            {
                bonus += 2;
            }

            if (outfit.isIndoorOutfit)
            {
                bonus += 2;
            }
        }

        return bonus;
    }

    private int CalculateWeatherTraitBonus(OutfitData outfit)
    {
        if (timeManager == null || outfit == null)
        {
            return 0;
        }

        int bonus = 0;

        if (timeManager.CurrentWeather == Weather.Sunny)
        {
            if (outfit.isOutdoorOutfit)
            {
                bonus += 4;
            }
        }

        if (timeManager.CurrentWeather == Weather.Cloudy)
        {
            if (outfit.isOutdoorOutfit)
            {
                bonus += 2;
            }
        }

        if (timeManager.CurrentWeather == Weather.Rainy)
        {
            if (outfit.isRainOutfit)
            {
                bonus += 15;
            }

            if (outfit.isOutdoorOutfit && !outfit.isRainOutfit)
            {
                bonus -= 8;
            }

            if (outfit.isIndoorOutfit)
            {
                bonus += 4;
            }
        }

        if (timeManager.CurrentWeather == Weather.Storm)
        {
            if (outfit.isRainOutfit)
            {
                bonus += 10;
            }

            if (outfit.isIndoorOutfit)
            {
                bonus += 10;
            }

            if (outfit.isOutdoorOutfit && !outfit.isRainOutfit)
            {
                bonus -= 15;
            }
        }

        if (timeManager.CurrentWeather == Weather.Snow)
        {
            if (outfit.isSnowOutfit)
            {
                bonus += 15;
            }

            if (outfit.isWarmOutfit)
            {
                bonus += 8;
            }

            if (outfit.isLightOutfit)
            {
                bonus -= 20;
            }
        }

        return bonus;
    }


}
