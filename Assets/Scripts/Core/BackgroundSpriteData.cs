using UnityEngine;

[System.Serializable]
public class BackgroundSpriteEntry
{
    public TimeSlot timeSlot = TimeSlot.Morning;
    public Weather weather = Weather.Sunny;
    public Sprite sprite;
}

[CreateAssetMenu(menuName = "LoveSim/Background Sprite Data")]
public class BackgroundSpriteData : ScriptableObject
{
    public BackgroundSpriteEntry[] entries;

    public Sprite FindSprite(TimeSlot timeSlot, Weather weather)
    {
        if (entries == null)
        {
            return null;
        }

        foreach (BackgroundSpriteEntry entry in entries)
        {
            if (entry == null)
            {
                continue;
            }

            if (entry.timeSlot == timeSlot &&
                entry.weather == weather &&
                entry.sprite != null)
            {
                return entry.sprite;
            }
        }

        return null;
    }
}
