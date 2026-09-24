using System;
using UnityEngine;


[Serializable]
public class Crop : ICloneable<Crop>
{
    [SerializeField]
    private string cropID;
    [SerializeField]
    private string name;
    [SerializeField]
    private Sprite icon;
    [SerializeField]
    private int stars;
    [SerializeField]
    private int baseSellPrice;

    public string CropID => cropID;
    public string Name => name;
    public Sprite Icon => icon;
    public int Stars => stars;
    public int BaseSellPrice => baseSellPrice;

    public Crop(Crop original)
    {
        if (original != null)
        {
            this.cropID = original.cropID;
            this.name = original.name;
            this.icon = original.icon;
            this.stars = original.stars;
            this.baseSellPrice = original.baseSellPrice;
        }
    }

    public Crop Clone()
    {
        return new Crop(this);
    }

    public void SetStars(int stars)
    {
        this.stars = Mathf.Max(0, stars);
    }

    public void SetBaseSellPrice(int baseSellPrice)
    {
        this.baseSellPrice = Mathf.Max(0, baseSellPrice);
    }

    public int GetSellPrice()
    {
        int bonusPercent = GetStarBonusPercent(stars);
        return Mathf.RoundToInt(baseSellPrice * (1f + bonusPercent / 100f));
    }

    private static int GetStarBonusPercent(int stars)
    {
        switch (stars)
        {
            case 1: return 0;
            case 2: return 25;
            case 3: return 50;
            case 4: return 75;
            case 5: return 100;
            default: return 0;
        }
    }
}