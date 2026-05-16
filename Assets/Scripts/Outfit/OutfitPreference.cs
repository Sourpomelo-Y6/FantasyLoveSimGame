using System;

[Serializable]
public class OutfitPreference
{
    public string outfitId;

    public int score;
    public int wearCount;
    public int praiseCount;
    public int dislikeCount;
    public int boredCount;

    public OutfitPreference(string outfitId)
    {
        this.outfitId = outfitId;
        score = 0;
        wearCount = 0;
        praiseCount = 0;
        dislikeCount = 0;
        boredCount = 0;
    }
}