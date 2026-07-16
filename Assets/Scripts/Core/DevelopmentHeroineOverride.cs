using System;
using System.Collections.Generic;

public static class DevelopmentHeroineOverride
{
    public static string HeroineId { get; private set; } = string.Empty;

    public static void SetHeroineId(string heroineId)
    {
        HeroineId = (heroineId ?? string.Empty).Trim();
    }

    public static HeroineProfileData ResolveProfile(
        IEnumerable<HeroineProfileData> profiles,
        bool shouldLoadOnStart)
    {
        if (shouldLoadOnStart || string.IsNullOrWhiteSpace(HeroineId) || profiles == null)
        {
            return null;
        }

        foreach (HeroineProfileData profile in profiles)
        {
            if (profile != null &&
                string.Equals(profile.heroineId, HeroineId, StringComparison.Ordinal))
            {
                return profile;
            }
        }
        return null;
    }
}
