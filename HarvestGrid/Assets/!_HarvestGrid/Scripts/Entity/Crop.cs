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

    public string CropID => cropID;
    public string Name => name;
    public Sprite Icon => icon;
    public int Stars => stars;

    public Crop(Crop original)
    {
        if (original != null)
        {
            this.cropID = original.cropID;
            this.name = original.name;
            this.icon = original.icon;
            this.stars = original.stars;
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
}