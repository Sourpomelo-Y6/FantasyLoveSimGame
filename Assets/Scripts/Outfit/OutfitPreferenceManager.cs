using System.Collections.Generic;
using UnityEngine;

public class OutfitPreferenceManager : MonoBehaviour
{
    private readonly List<OutfitPreference> preferences = new List<OutfitPreference>();

    public IReadOnlyList<OutfitPreference> Preferences => preferences;

    public OutfitPreference GetOrCreatePreference(string outfitId)
    {
        if (string.IsNullOrEmpty(outfitId))
        {
            return null;
        }

        foreach (OutfitPreference preference in preferences)
        {
            if (preference.outfitId == outfitId)
            {
                return preference;
            }
        }

        OutfitPreference newPreference = new OutfitPreference(outfitId);
        preferences.Add(newPreference);
        return newPreference;
    }

    public void RegisterWear(string outfitId)
    {
        OutfitPreference preference = GetOrCreatePreference(outfitId);

        if (preference == null)
        {
            return;
        }

        preference.wearCount++;
    }

    public string ApplyReaction(OutfitData outfit, OutfitReactionType reactionType, HeroineStatus heroineStatus)
    {
        if (outfit == null)
        {
            return "まだ衣装を着ていません。";
        }

        OutfitPreference preference = GetOrCreatePreference(outfit.outfitId);

        if (preference == null)
        {
            return "衣装評価を記録できませんでした。";
        }

        switch (reactionType)
        {
            case OutfitReactionType.Praise:
                preference.score += 2;
                preference.praiseCount++;

                if (heroineStatus != null)
                {
                    heroineStatus.AddAffection(1);
                }

                return "「本当ですか？ そう言ってもらえると嬉しいです」\nヒロインは少し照れながら笑っています。";

            case OutfitReactionType.Dislike:
                preference.score -= 2;
                preference.dislikeCount++;

                if (heroineStatus != null)
                {
                    heroineStatus.AddAffection(-1);
                }

                return "「そうですか……次は別の服にしてみます」\nヒロインは少し残念そうです。";

            case OutfitReactionType.Bored:
                preference.score -= 1;
                preference.boredCount++;

                return "「確かに、最近こればかりでしたね。次は違う服にしてみます」";

            case OutfitReactionType.Change:
                return "別の衣装を選んでください。";

            default:
                return "";
        }
    }

    public int GetScore(string outfitId)
    {
        OutfitPreference preference = GetOrCreatePreference(outfitId);

        if (preference == null)
        {
            return 0;
        }

        return preference.score;
    }

    public void SetPreferences(List<OutfitPreference> loadedPreferences)
    {
        preferences.Clear();

        if (loadedPreferences == null)
        {
            return;
        }

        foreach (OutfitPreference preference in loadedPreferences)
        {
            if (preference == null)
            {
                continue;
            }

            if (string.IsNullOrEmpty(preference.outfitId))
            {
                continue;
            }

            preferences.Add(preference);
        }
    }

    public List<OutfitPreference> CreateSaveData()
    {
        List<OutfitPreference> saveList = new List<OutfitPreference>();

        foreach (OutfitPreference preference in preferences)
        {
            if (preference == null)
            {
                continue;
            }

            OutfitPreference copy = new OutfitPreference(preference.outfitId);
            copy.score = preference.score;
            copy.wearCount = preference.wearCount;
            copy.praiseCount = preference.praiseCount;
            copy.dislikeCount = preference.dislikeCount;
            copy.boredCount = preference.boredCount;

            saveList.Add(copy);
        }

        return saveList;
    }

    public OutfitPreference FindPreference(string outfitId)
    {
        if (string.IsNullOrEmpty(outfitId))
        {
            return null;
        }

        foreach (OutfitPreference preference in preferences)
        {
            if (preference.outfitId == outfitId)
            {
                return preference;
            }
        }

        return null;
    }
}